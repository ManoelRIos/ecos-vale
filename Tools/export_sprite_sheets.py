#!/usr/bin/env python3
"""Export transparent, deterministic PNG sheets from production C# pixel art.

The small export-only .NET project supplies pixel storage APIs, not a Unity
renderer. This tool validates generated cells and writes PNG/JSON/Unity metadata;
it does not certify Unity import, rendering, animation playback or a build.
No Python imaging package is required.
"""
import argparse
import base64
import hashlib
import json
import os
import re
from pathlib import Path, PurePosixPath
import shutil
import struct
import subprocess
import tempfile
import uuid
import zlib

ROOT = Path(__file__).resolve().parents[1]
RESOURCE_ROOT = ROOT / 'Assets/_Game/Resources/ReferenceSheets'
PROJECT = ROOT / 'Tools/SpriteSheetExport/SpriteSheetExport.csproj'
NAMESPACE = uuid.UUID('34f93ee1-6198-55cf-b46f-944119d8174a')
PNG_SIGNATURE = b'\x89PNG\r\n\x1a\n'


def png_bytes(width, height, rgba):
    """Write top-left RGBA pixels with no resampling, palette loss or timestamps."""
    if width <= 0 or height <= 0 or len(rgba) != width * height * 4:
        raise ValueError('RGBA buffer dimensions disagree')

    def chunk(kind, contents):
        return (struct.pack('>I', len(contents)) + kind + contents
                + struct.pack('>I', zlib.crc32(kind + contents) & 0xffffffff))

    scanlines = b''.join(b'\x00' + rgba[y * width * 4:(y + 1) * width * 4]
                         for y in range(height))
    header = struct.pack('>IIBBBBB', width, height, 8, 6, 0, 0, 0)
    return (PNG_SIGNATURE + chunk(b'IHDR', header)
            + chunk(b'IDAT', zlib.compress(scanlines, 9)) + chunk(b'IEND', b''))


def guid_for(path):
    relative = path.relative_to(ROOT).as_posix()
    return uuid.uuid5(NAMESPACE, relative).hex


def folder_meta(path):
    return f'fileFormatVersion: 2\nguid: {guid_for(path)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n'.encode()


def texture_meta(path):
    # Default texture + Sprite.Create rects at runtime avoids ambiguous auto-slicing.
    # Point, lossless RGBA and NPOT preservation keep the authored high-resolution cell boundaries exact.
    return f'''fileFormatVersion: 2
guid: {guid_for(path)}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 0
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 100
  spriteMode: 0
  spriteExtrude: 0
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 32
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 0
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 100
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee7208000000000000000
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
'''.encode()


def json_meta(path):
    return f'fileFormatVersion: 2\nguid: {guid_for(path)}\nTextScriptImporter:\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n'.encode()


def dotnet_binary():
    requested = os.environ.get('DOTNET_PATH')
    candidates = [requested, shutil.which('dotnet'), '/workspace/.tools/dotnet/dotnet']
    for value in candidates:
        if value and Path(value).is_file():
            return value
    raise RuntimeError('Install .NET SDK 8 or set DOTNET_PATH to its dotnet executable')


def export_exchange():
    with tempfile.TemporaryDirectory(prefix='ecos-sprite-export-') as temporary:
        path = Path(temporary) / 'pixels.json'
        environment = os.environ.copy()
        if 'DOTNET_CLI_HOME' not in environment and Path('/workspace/.tools/dotnet-home').is_dir():
            environment['DOTNET_CLI_HOME'] = '/workspace/.tools/dotnet-home'
        subprocess.run([dotnet_binary(), 'run', '--project', str(PROJECT), '--', str(path)],
                       cwd=ROOT, env=environment, check=True)
        return json.loads(path.read_text(encoding='utf-8'))



def preview_bytes(exchange):
    """Compose original exported pixels and in-game glyphs, without resampling."""
    width, height = 1920, 3600
    background = (19, 21, 28, 255)
    pixels = bytearray(bytes(background) * (width * height))
    glyph_source = (ROOT / 'Assets/_Game/Haunt/Runtime/HauntPixelText.cs').read_text()
    glyphs = dict(re.findall(r"\{ '([A-Z0-9 .:/+\-])', \"([01/]+)\" \}", glyph_source))

    def rect(x, y, w, h, color):
        rgba = bytes((*color[:3], color[3] if len(color) > 3 else 255))
        for py in range(max(0, y), min(height, y + h)):
            start, end = max(0, x), min(width, x + w)
            if end > start:
                at = (py * width + start) * 4
                pixels[at:at + (end - start) * 4] = rgba * (end - start)

    def text(value, x, y, scale=2, color=(212, 218, 209)):
        for character in value.upper():
            pattern = glyphs.get(character)
            if pattern:
                for row, line in enumerate(pattern.split('/')):
                    for col, bit in enumerate(line):
                        if bit == '1':
                            rect(x + col * scale, y + row * scale, scale, scale, color)
            x += 6 * scale

    sheets = {sheet['resource']: sheet for sheet in exchange['sheets']}
    buffers = {key: base64.b64decode(sheet['rgba']) for key, sheet in sheets.items()}

    def pose(resource, state, frame, x, y, scale, facing='Right'):
        sheet = sheets[resource]
        cell = next(cell for cell in sheet['cells']
                    if cell['state'] == state and cell['frame'] == frame and cell['facing'] == facing)
        sx, sy, size, _ = cell['rect']
        raw = buffers[resource]
        for py in range(size):
            for px in range(size):
                at = ((sy + py) * sheet['width'] + sx + px) * 4
                r, g, b, a = raw[at:at + 4]
                if a == 0:
                    continue
                if a == 255:
                    rect(x + px * scale, y + py * scale, scale, scale, (r, g, b))
                else:
                    color = tuple((value * a + background[i] * (255 - a)) // 255
                                  for i, value in enumerate((r, g, b)))
                    rect(x + px * scale, y + py * scale, scale, scale, color)

    gold = (232, 206, 95)
    green = (157, 194, 179)
    text('ECOS DA MEIA-NOITE / FOLHAS DE ANIMACAO DETALHADAS', 24, 20, 4, gold)
    text('PIXELS AUTORADOS EM C# / PNGS TRANSPARENTES / SEM RENDERIZACAO UNITY', 24, 62, 3)
    text('JOGADORES / 48X48 / 32 PIXELS POR UNIDADE', 24, 108, 3, gold)
    heroes = [('player', 'CACADOR', ['Walk', 'Attack', 'Dodge']),
              ('medium', 'MEDIUM', ['Walk', 'Cast', 'Dodge']),
              ('reaper', 'CEIFADOR', ['Walk', 'Attack', 'Dodge']),
              ('alchemist', 'ALQUIMISTA', ['Walk', 'Cast', 'Reload'])]
    for col, (actor, label, states) in enumerate(heroes):
        x = 24 + col * 474
        text(label, x, 150, 3, green)
        for row, state in enumerate(states):
            top = 190 + row * 130
            text(state, x, top, 2)
            for frame in range(4):
                pose('actors/' + actor, state, frame, x + frame * 112, top + 18, 2)
    text('MOBS / 48X48 / EMERGENCIA - ATAQUE - MORTE', 24, 600, 3, gold)
    mobs = [('scarecrow', 'ESPANTALHO', 'Spawn'), ('gargoyle', 'GARGULA', 'Petrified'),
            ('stalker-shadow', 'SOMBRA', 'Spawn'), ('fire-bat', 'MORCEGO DE FOGO', 'Walk'),
            ('skeleton', 'ESQUELETO', 'Walk'), ('slime', 'SLIME', 'Walk')]
    for index, (actor, label, first) in enumerate(mobs):
        x, y = 24 + index % 3 * 632, 645 + index // 3 * 350
        text(label, x, y, 3, green)
        for row, state in enumerate([first, 'Attack', 'Death']):
            top = y + 30 + row * 100
            text(state, x, top + 40, 2)
            for frame in range(4):
                pose('actors/' + actor, state, frame, x + 120 + frame * 112, top, 2)
    text('BOSSES / 96X96 / REPOUSO - PREPARO - ATAQUE - FURIA - MORTE', 24, 1400, 3, gold)
    states = ['Idle', 'Telegraph', 'Attack', 'Enrage', 'Death']
    for col, label in enumerate(states):
        text(label, 360 + col * 280, 1440, 2, green)
    bosses = [('iron-executioner', 'CARRASCO DE FERRO'), ('boss', 'MESTRE ABOBORA'),
              ('widow', 'VIUVA DO VEU'), ('bishop', 'BISPO DAS CINZAS'), ('matriarch', 'MATRIARCA'),
              ('bell', 'SINO DO ABISMO'), ('bog-oracle', 'ORACULO DO BREJO')]
    for row, (actor, label) in enumerate(bosses):
        top = 1480 + row * 210
        text(label, 24, top + 84, 2)
        for col, state in enumerate(states):
            pose('bosses/' + actor, state, 2 if state != 'Death' else 3, 360 + col * 280, top, 2)
    text('OBJETOS / 32X32 / QUATRO ESTAGIOS E QUATRO QUADROS POR ESTAGIO', 24, 2990, 3, gold)
    for row, (actor, label) in enumerate([('barrel-explosive', 'BARRIL EXPLOSIVO'),
                                        ('barrel-acid', 'BARRIL DE ACIDO'), ('decrepit-chest', 'BAU DECREPITO')]):
        top = 3040 + row * 140
        text(label, 24, top + 40, 2, green)
        for stage in range(4):
            for frame in range(4):
                pose('props/' + actor, 'Stage' + str(stage), frame,
                     300 + stage * 392 + frame * 96, top, 3)
    text('24 PNGS / 1700 CELULAS / POINT / 32 PIXELS POR UNIDADE / ESCALA NO MUNDO PRESERVADA', 24, 3550, 3)
    return png_bytes(width, height, pixels)


def equipment_preview_bytes(exchange):
    """Show full authored item pixels; these preview buffers are not runtime data."""
    width, height = 1920, 1750
    background = (19, 21, 28, 255)
    pixels = bytearray(bytes(background) * (width * height))
    glyph_source = (ROOT / 'Assets/_Game/Haunt/Runtime/HauntPixelText.cs').read_text()
    glyphs = dict(re.findall(r"\{ '([A-Z0-9 .:/+\-])', \"([01/]+)\" \}", glyph_source))

    def rect(x, y, w, h, color):
        rgba = bytes((*color[:3], color[3] if len(color) > 3 else 255))
        for py in range(max(0, y), min(height, y + h)):
            start, end = max(0, x), min(width, x + w)
            if end > start:
                at = (py * width + start) * 4
                pixels[at:at + (end - start) * 4] = rgba * (end - start)

    def text(value, x, y, scale=2, color=(212, 218, 209)):
        for character in value.upper():
            pattern = glyphs.get(character)
            if pattern:
                for row, line in enumerate(pattern.split('/')):
                    for col, bit in enumerate(line):
                        if bit == '1':
                            rect(x + col * scale, y + row * scale, scale, scale, color)
            x += 6 * scale

    labels = {'weapon': 'REVOLVER', 'scattergun': 'BACAMARTE', 'repeater': 'REPETIDORA',
              'cannon': 'CANHAO DAS ALMAS', 'stapler': 'GRAMPEADOR', 'banana-gun': 'BANANA',
              'spinning-top': 'PIAO DO CAOS', 'hunter-carbine': 'CARABINA DA AURORA',
              'veil-wand': 'VARINHA DO VEU', 'bone-caster': 'CEIFADOR DE OSSOS',
              'arc-coil': 'BOBINA VOLTAICA', 'mirror-disc': 'DISCO ESPELHO',
              'ray-lantern': 'LANTERNA DE RAIOS', 'bone-flute': 'FLAUTA DE OSSOS',
              'umbrella-gun': 'GUARDA-CHUVA', 'hive-launcher': 'COLMEIA DE BOLSO',
              'boxing-glove': 'LUVA DE BOXE', 'water-urn': 'URNA', 'ammo-cache': 'MUNICAO',
              'giant-rounds': 'BALAS GIGANTES', 'character-relic': 'RELIQUIA DE ALMA',
              'quick-boots': 'BOTAS'}
    text('ECOS DA MEIA-NOITE / ARMAS E ITENS DETALHADOS', 24, 22, 4, (232, 206, 95))
    text('PIXELS REAIS DO GERADOR / 32 PPU / TAMANHO NO MUNDO PRESERVADO / SEM RENDERIZACAO UNITY',
         24, 66, 3)
    images = exchange.get('equipment', [])
    if len(images) != 22 or len({image['id'] for image in images}) != 22:
        raise ValueError('Expected 17 detailed weapons and five item previews')
    for index, image in enumerate(images):
        x, y = 24 + index % 4 * 474, 124 + index // 4 * 260
        w, h = image['width'], image['height']
        if image['pixelsPerUnit'] != 32 or w <= 0 or h <= 0:
            raise ValueError('Invalid detailed item preview dimensions or PPU')
        raw = base64.b64decode(image['rgba'], validate=True)
        if len(raw) != w * h * 4:
            raise ValueError('Invalid detailed item RGBA buffer')
        scale = max(1, min(3, 430 // w, 190 // h))
        px, py = x + (430 - w * scale) // 2, y + 36 + (190 - h * scale) // 2
        text(labels.get(image['id'], image['id']), x, y, 2, (157, 194, 179))
        text(str(w) + 'X' + str(h), x, y + 236, 2)
        for sy in range(h):
            for sx in range(w):
                at = (sy * w + sx) * 4
                r, g, b, a = raw[at:at + 4]
                if not a:
                    continue
                color = tuple((value * a + background[i] * (255 - a)) // 255
                              for i, value in enumerate((r, g, b)))
                rect(px + sx * scale, py + sy * scale, scale, scale, color)
    text('17 ARMAS / 5 ITENS / DETALHES AUTORADOS NO NOVO GRID / FILTRO POINT', 24, 1702, 3)
    return png_bytes(width, height, pixels)


def build_outputs(exchange):
    if exchange['version'] != 2 or exchange['framesPerState'] != 4:
        raise ValueError('Unsupported sheet layout protocol')
    if len(exchange['sheets']) != 24 or exchange['cellCount'] != 1700:
        raise ValueError('Expected 24 sheets and 1700 cells')
    outputs = {}
    for folder in [RESOURCE_ROOT, RESOURCE_ROOT / 'actors', RESOURCE_ROOT / 'bosses', RESOURCE_ROOT / 'props']:
        outputs[Path(str(folder) + '.meta')] = folder_meta(folder)
    manifest = {key: value for key, value in exchange.items() if key not in ('sheets', 'equipment')}
    manifest['source'] = 'Production HauntPixelArt C#; export-only pixel APIs; no Unity rendering'
    manifest['filterMode'] = 'Point'
    manifest['mipmaps'] = False
    manifest['compression'] = 'None'
    manifest['sheets'] = []
    names = set()
    cell_names = set()
    for original in exchange['sheets']:
        sheet = dict(original)
        name = PurePosixPath(sheet['resource'])
        if name.is_absolute() or '..' in name.parts or len(name.parts) != 2:
            raise ValueError('Unsafe or unexpected sheet resource path')
        if str(name) in names:
            raise ValueError('Duplicate sheet resource path')
        names.add(str(name))
        width, height, cell = sheet['width'], sheet['height'], sheet['cellSize']
        expected_cell = {'actor': 48, 'boss': 96, 'prop': 32}.get(sheet['category'])
        if cell != expected_cell or sheet['pixelsPerUnit'] != 32:
            raise ValueError('Unexpected authored resolution or world scale')
        if width != 4 * cell or height != sheet['rows'] * cell:
            raise ValueError('Invalid sheet grid dimensions')
        rgba = base64.b64decode(sheet.pop('rgba'), validate=True)
        encoded = png_bytes(width, height, rgba)
        path = RESOURCE_ROOT.joinpath(*name.parts).with_suffix('.png')
        outputs[path] = encoded
        outputs[Path(str(path) + '.meta')] = texture_meta(path)
        if len(sheet['cells']) != sheet['rows'] * 4:
            raise ValueError('Incomplete sheet cell manifest')
        expected_rects = {(x * cell, y * cell, cell, cell) for y in range(sheet['rows']) for x in range(4)}
        actual_rects = {tuple(frame['rect']) for frame in sheet['cells']}
        if expected_rects != actual_rects:
            raise ValueError('Overlapping or missing sheet cells')
        for frame in sheet['cells']:
            if frame['name'] in cell_names:
                raise ValueError('Duplicate global cell name')
            cell_names.add(frame['name'])
            x, y, w, h = frame['rect']
            if frame['unityRect'] != [x, height - y - h, w, h]:
                raise ValueError('Top-left and Unity bottom-left rects disagree')
            expected_pivot = [0.5, 2 / 24] if sheet['category'] == 'actor' else [0.5, 0.5]
            if any(abs(a - b) > .00001 for a, b in zip(frame['pivot'], expected_pivot)):
                raise ValueError('Unexpected animation pivot')
        sheet['file'] = name.as_posix() + '.png'
        sheet['sha256'] = hashlib.sha256(encoded).hexdigest()
        manifest['sheets'].append(sheet)
    path = RESOURCE_ROOT / 'manifest.json'
    outputs[path] = (json.dumps(manifest, ensure_ascii=False, indent=2) + '\n').encode('utf-8')
    outputs[Path(str(path) + '.meta')] = json_meta(path)
    outputs[ROOT / 'Documentation/REFERENCE_SPRITESHEETS.png'] = preview_bytes(exchange)
    outputs[ROOT / 'Documentation/EQUIPMENT_DETAIL.png'] = equipment_preview_bytes(exchange)
    return outputs


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true', help='Compare regenerated assets without modifying them')
    args = parser.parse_args()
    try:
        outputs = build_outputs(export_exchange())
        changed = []
        for path, data in outputs.items():
            if not path.is_file() or path.read_bytes() != data:
                changed.append(path.relative_to(ROOT).as_posix())
                if not args.check:
                    path.parent.mkdir(parents=True, exist_ok=True)
                    path.write_bytes(data)
        if args.check and changed:
            for path in changed:
                print('Outdated/missing: ' + path)
            raise RuntimeError('Spritesheets differ from the production C# pixel generator; run without --check')
        verb = 'Verified' if args.check else 'Wrote'
        print(f'{verb} 24 transparent sheets, 1700 cell records and stable Unity metadata ({len(changed)} changed files).')
        print('No Unity import, rendering, animation playback or build validation was performed.')
    except (OSError, ValueError, RuntimeError, subprocess.CalledProcessError) as exc:
        parser.exit(1, f'Sprite export failed: {exc}\n')


if __name__ == '__main__':
    main()
