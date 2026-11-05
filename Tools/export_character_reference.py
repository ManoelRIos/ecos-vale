#!/usr/bin/env python3
"""Export the current 2D character pixels into one labeled PNG reference board.

This is an asset export, not a redraw or a 3D render. Existing PNG pixels are
used for native heroes and reference sheets; remaining sprites come directly
from the production C# pixel generator. Only integer nearest-neighbor zoom is
applied to the illustrations. Requires Pillow and the .NET 8 SDK.
"""
import argparse
import base64
import hashlib
import json
import os
from pathlib import Path
import subprocess
import tempfile

from PIL import Image, ImageDraw, ImageFont
from export_sprite_sheets import dotnet_binary

ROOT = Path(__file__).resolve().parents[1]
RESOURCES = ROOT / 'Assets/_Game/Resources'
OUTPUT = ROOT / 'Documentation/TODOS_OS_PERSONAGENS.png'
PLAYERS = [('player', 'CAÇADOR / PILOTO', 'pilot', 'hunter-carbine'),
           ('medium', 'MÉDIUM', 'medium', 'veil-wand'),
           ('reaper', 'CEIFADOR', 'reaper', 'bone-caster'),
           ('alchemist', 'ALQUIMISTA', 'alchemist', 'arc-coil')]
MOBS = [('skeleton', 'ESQUELETO'), ('lantern-wisp', 'LANTERNA ERRANTE'),
        ('pumpkin', 'ABÓBORA'), ('ghost', 'FANTASMA'), ('cultist', 'CULTISTA'),
        ('bat', 'MORCEGO'), ('larva', 'LARVA'), ('sentinel', 'SENTINELA'),
        ('acolyte', 'ACÓLITO'), ('slime', 'SLIME'), ('spider', 'ARANHA'),
        ('mushroom-brute', 'COGUMELO BRUTO'), ('clockwork-imp', 'DIABRETE DE CORDA'),
        ('scarecrow', 'ESPANTALHO AMALDIÇOADO'), ('gargoyle', 'GÁRGULA DE PEDRA'),
        ('stalker-shadow', 'SOMBRA PERSEGUIDORA'), ('fire-bat', 'MORCEGO DE FOGO')]
BOSSES = [('boss', 'REI DAS ABÓBORAS'), ('widow', 'VIÚVA DO VÉU'),
          ('bishop', 'BISPO DAS CINZAS'), ('matriarch', 'MATRIARCA DA PESTE'),
          ('bell', 'SINO DO ABISMO'), ('iron-executioner', 'CARRASCO DE FERRO'),
          ('bog-oracle', 'ORÁCULO DO BREJO')]
EXTRAS = [('merchant', 'COLETOR DE ALMAS'), ('miniboss', 'MINICHEFE')]
SHEET_ACTORS = {'skeleton', 'slime', 'scarecrow', 'gargoyle', 'stalker-shadow', 'fire-bat'}
WIDTH, MARGIN, GAP, CARD_W, CARD_H = 3200, 48, 24, 758, 360
BG, CARD, LABEL, MUTED = '#141923', '#4a525f', '#edf1f6', '#b7c4d5'


def font(size, bold=False):
    candidates = ([Path('/usr/share/fonts/truetype/open-sans/OpenSans-Bold.ttf')] if bold else
                  [Path('/usr/share/fonts/truetype/open-sans/OpenSans-Regular.ttf')])
    candidates += [Path('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf')]
    for path in candidates:
        if path.is_file():
            return ImageFont.truetype(str(path), size)
    raise RuntimeError('Install Open Sans or DejaVu Sans for the reference labels.')


def csharp_records():
    project = ROOT / 'Tools/CharacterReferenceExport/CharacterReferenceExport.csproj'
    with tempfile.TemporaryDirectory(prefix='ecos-character-reference-') as tmp:
        exchange = Path(tmp) / 'pixels.json'
        environment = os.environ.copy()
        if 'DOTNET_CLI_HOME' not in environment and Path('/workspace/.tools/dotnet-home').is_dir():
            environment['DOTNET_CLI_HOME'] = '/workspace/.tools/dotnet-home'
        subprocess.run([dotnet_binary(), 'run', '--project', str(project), '--', str(exchange)],
                       cwd=ROOT, env=environment, check=True)
        payload = json.loads(exchange.read_text())
    records = {item['id']: item for item in payload['records']}
    if len(records) != 30:
        raise ValueError('Expected 26 non-player entities and four weapon companions.')
    return records


def rgba_pose(pose):
    image = Image.frombytes('RGBA', (pose['width'], pose['height']), base64.b64decode(pose['rgbaBase64topLeft']))
    if image.getchannel('A').getbbox() is None:
        raise ValueError('A reference pose is empty.')
    return image


def sheet_pose(key, pose, index):
    generated = rgba_pose(pose)
    if key in SHEET_ACTORS:
        size = 48
        row = [0, 3, 6][index]
        frame = [0, 1, 2][index]
        path = RESOURCES / 'ReferenceSheets/actors' / (key + '.png')
    elif key in dict(BOSSES):
        size = 96
        row = [0, 2, 3][index]
        frame = [0, 2, 1][index]
        path = RESOURCES / 'ReferenceSheets/bosses' / (key + '.png')
    else:
        return generated, 'C# HauntPixelArt.Frame: ' + key
    with Image.open(path) as source:
        actual = source.convert('RGBA').crop((frame * size, row * size, (frame + 1) * size, (row + 1) * size))
    if actual.tobytes() != generated.tobytes():
        raise ValueError('Generated pixels differ from the committed sheet: ' + key)
    return actual, str(path.relative_to(ROOT))


def hero_poses(sheet_name):
    catalog_path = RESOURCES / 'CharacterSheets/catalog.json'
    catalog = json.loads(catalog_path.read_text())
    source_path = RESOURCES / 'CharacterSheets' / (sheet_name + '.png')
    result = []
    with Image.open(source_path) as atlas:
        atlas = atlas.convert('RGBA')
        for direction, label in [('Front', 'FRENTE'), ('Right', 'LADO'), ('Back', 'COSTAS')]:
            clip = next(item for item in catalog['clips'] if item['name'] == 'Idle'
                        and item['pose'] == 'Empty' and item['facing'] == direction and not item['alternate'])
            record = catalog['frames'][clip['frames'][0]]
            x, y, w, h = (record[name] for name in ('x', 'y', 'width', 'height'))
            top = atlas.height - y - h
            image = atlas.crop((x, top, x + w, top + h))
            result.append((label, image, str(source_path.relative_to(ROOT)), 16))
    return result


def centered(draw, text, rect, face, color):
    left, top, right, bottom = rect
    bounds = draw.textbbox((0, 0), text, font=face)
    x = left + (right - left - (bounds[2] - bounds[0])) / 2 - bounds[0]
    y = top + (bottom - top - (bounds[3] - bounds[1])) / 2 - bounds[1]
    draw.text((round(x), round(y)), text, font=face, fill=color)


def render_card(canvas, draw, card, x, y, accent):
    draw.rounded_rectangle((x, y, x + CARD_W, y + CARD_H), radius=12, fill=CARD, outline='#717b89', width=2)
    draw.rounded_rectangle((x, y, x + CARD_W, y + 57), radius=12, fill='#202733')
    draw.rectangle((x, y + 45, x + CARD_W, y + 57), fill='#202733')
    draw.rectangle((x + 18, y + 17, x + 24, y + 40), fill=accent)
    draw.text((x + 38, y + 12), card['code'] + '  ' + card['name'], font=font(24, True), fill=LABEL)
    count = len(card['poses'])
    slot = (CARD_W - 32) // count
    geometry = []
    for i, (label, source, origin, ppu) in enumerate(card['poses']):
        bbox = source.getchannel('A').getbbox()
        if bbox is None:
            raise ValueError('Empty illustration: ' + card['key'])
        sprite = source.crop(bbox)
        factor = min((slot - 20) // sprite.width, 225 // sprite.height)
        factor = max(1, factor)
        enlarged = sprite.resize((sprite.width * factor, sprite.height * factor), Image.Resampling.NEAREST)
        px = x + 16 + i * slot + (slot - enlarged.width) // 2
        py = y + 75 + 225 - enlarged.height
        canvas.alpha_composite(enlarged, (px, py))
        centered(draw, label, (x + 16 + i * slot, y + 311, x + 16 + (i + 1) * slot, y + 340), font(18, True), LABEL)
        geometry.append({'label': label, 'source': origin, 'sourceSize': list(source.size), 'sourcePixelsPerUnit': ppu,
                         'alphaCrop': list(bbox), 'zoom': factor, 'pngRect': [px, py, enlarged.width, enlarged.height],
                         'sourceRgbaSha256': hashlib.sha256(source.tobytes()).hexdigest()})
    return geometry


def build(output):
    records = csharp_records()
    groups = []
    players = []
    for index, (key, name, sheet, weapon) in enumerate(PLAYERS):
        companion = records[weapon]['poses'][0]
        poses = hero_poses(sheet) + [('ARMA', rgba_pose(companion), 'C# HauntPixelArt.Get: ' + weapon, companion['ppu'])]
        players.append({'code': 'P%02d' % (index + 1), 'key': key, 'name': name, 'poses': poses})
    groups.append(('JOGÁVEIS', '4 almas / vistas reais e equipamento separado', '#65d7cb', players))
    for prefix, name, subtitle, accent, entities in [
            ('M', 'MOBS', '17 criaturas / poses reais do jogo', '#f29c80', MOBS),
            ('B', 'CHEFES', '7 guardiões / repouso, ataque e fúria', '#ebc36f', BOSSES),
            ('X', 'NPC E MINICHEFE', '2 entidades adicionais presentes no roguelike 2D original', '#b69ee8', EXTRAS)]:
        cards = []
        for index, (key, title) in enumerate(entities):
            poses = []
            for pose_index, pose in enumerate(records[key]['poses']):
                image, origin = sheet_pose(key, pose, pose_index)
                poses.append((pose['label'], image, origin, pose['ppu']))
            cards.append({'code': prefix + '%02d' % (index + 1), 'key': key, 'name': title, 'poses': poses})
        groups.append((name, subtitle, accent, cards))
    height = 185 + sum(88 + ((len(cards) + 3) // 4) * (CARD_H + GAP) for _, _, _, cards in groups) + 125
    canvas = Image.new('RGBA', (WIDTH, height), BG)
    draw = ImageDraw.Draw(canvas)
    draw.text((MARGIN, 26), 'ECOS DA MEIA-NOITE', font=font(51, True), fill=LABEL)
    draw.text((MARGIN, 95), 'REFERÊNCIA DOS 30 PERSONAGENS PARA ADAPTAÇÃO 3D', font=font(29, True), fill='#89ddd7')
    draw.text((MARGIN, 142), 'Sprites atuais do projeto. Um cartão representa uma entidade; vistas e poses não são personagens adicionais.', font=font(23), fill=MUTED)
    manifest = {'version': 1, 'image': output.name, 'size': [WIDTH, height], 'entityCount': 30,
                'groups': {'players': 4, 'mobs': 17, 'bosses': 7, 'original2dExtras': 2}, 'cards': []}
    y = 185
    for title, subtitle, accent, cards in groups:
        draw.rectangle((MARGIN, y + 13, MARGIN + 8, y + 60), fill=accent)
        draw.text((MARGIN + 25, y + 4), title, font=font(32, True), fill=accent)
        draw.text((MARGIN + 25, y + 48), subtitle, font=font(19), fill=MUTED)
        y += 88
        for index, card in enumerate(cards):
            x = MARGIN + (index % 4) * (CARD_W + GAP)
            top = y + (index // 4) * (CARD_H + GAP)
            poses = render_card(canvas, draw, card, x, top, accent)
            manifest['cards'].append({'id': card['code'], 'key': card['key'], 'name': card['name'], 'poses': poses})
        y += ((len(cards) + 3) // 4) * (CARD_H + GAP)
    draw.text((MARGIN, y + 18), 'Preservar paleta, silhueta, proporções e acessórios. As poses de ação e fúria servem de referência para animação e VFX.', font=font(23), fill=LABEL)
    draw.text((MARGIN, y + 56), 'Ampliação individual em pixels inteiros. O tamanho entre cartões não representa a escala dos personagens no jogo.', font=font(22), fill=MUTED)
    assert len(manifest['cards']) == len({item['key'] for item in manifest['cards']}) == 30
    output.parent.mkdir(parents=True, exist_ok=True)
    canvas.convert('RGB').save(output, format='PNG', optimize=True)
    manifest['pngSha256'] = hashlib.sha256(output.read_bytes()).hexdigest()
    output.with_suffix('.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n')
    print(f'Exported {output}: {WIDTH}x{height}, 30 entities, 94 source poses; pixel-preserving integer zoom.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, default=OUTPUT)
    args = parser.parse_args()
    build(args.output.resolve())


if __name__ == '__main__':
    main()
