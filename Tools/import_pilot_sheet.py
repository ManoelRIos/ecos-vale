#!/usr/bin/env python3
"""Import the user-supplied Pilot sheet without resampling its character pixels.

The source PNG is never modified. Explicit source rectangles and foot anchors
come from Tools/Pilot/source-clips.json; only catalogued background colors become
transparent. Three costume variants share every source pose and clip index.
This is an asset conversion tool, not a Unity renderer or gameplay test.
"""
import argparse
from collections import deque
import hashlib
import json
import math
from pathlib import Path
import re

from PIL import Image, ImageDraw

from export_sprite_sheets import folder_meta, json_meta, png_bytes, texture_meta

ROOT = Path(__file__).resolve().parents[1]
RESOURCE_ROOT = ROOT / 'Assets/_Game/Resources/CharacterSheets'
DEFAULT_SOURCE = ROOT / 'Assets/_Game/ArtSources/PilotOriginal.png'
DEFAULT_CATALOG = ROOT / 'Tools/Pilot/source-clips.json'
CELL_SIZE = 48
COLUMNS = 32
FOOT = (24, 40)
CHARACTERS = ('pilot', 'medium', 'reaper', 'alchemist')
FACINGS = {'Front', 'FrontRight', 'Right', 'BackRight', 'Back'}
POSES = {'Empty', 'OneHanded', 'TwoHanded'}
SKIN = {(247, 206, 119), (249, 198, 105)}
HAIR = {(78, 4, 0), (114, 17, 0), (137, 64, 0), (157, 108, 32),
        (66, 9, 15), (45, 0, 5), (62, 0, 0), (63, 0, 0), (79, 4, 0),
        (128, 44, 0), (128, 43, 0), (32, 2, 0), (225, 75, 49)}
CLOTH = {(151, 140, 96), (129, 126, 82), (93, 214, 77), (33, 121, 22)}
TROUSERS = {(57, 79, 120), (22, 45, 75)}
THEMES = {
    'medium': {'hair': ((48, 26, 67), (91, 48, 117), (142, 79, 169)),
               'cloth': ((91, 54, 120), (58, 34, 78)), 'trousers': ((67, 49, 88), (32, 26, 48)),
               'skin': (247, 206, 119), 'accent': (187, 119, 225)},
    'reaper': {'hair': ((67, 17, 29), (121, 32, 41), (188, 58, 55)),
               'cloth': ((146, 43, 45), (87, 27, 35)), 'trousers': ((74, 36, 46), (31, 23, 32)),
               'skin': (225, 217, 177), 'accent': (221, 184, 113)},
    'alchemist': {'hair': ((23, 32, 37), (45, 60, 53), (83, 98, 62)),
                  'cloth': ((76, 64, 98), (39, 41, 56)), 'trousers': ((48, 63, 64), (22, 36, 38)),
                  'skin': (221, 208, 146), 'accent': (123, 218, 97)},
}


def rgba_bytes(image):
    return image.convert('RGBA').tobytes()


def checked_rect(value, width, height):
    if not isinstance(value, list) or len(value) != 4 or any(type(v) is not int for v in value):
        raise ValueError('Each source rect must contain four integers')
    x, y, w, h = value
    if min(x, y) < 0 or min(w, h) <= 0 or x + w > width or y + h > height:
        raise ValueError('Source rectangle escapes its PNG: ' + str(value))
    return x, y, w, h


def checked_backgrounds(value):
    if not isinstance(value, list) or not value or len(value) > 8:
        raise ValueError('A clip needs one to eight explicit background colors')
    colors = set()
    for color in value:
        if not isinstance(color, list) or len(color) != 3 or any(type(v) is not int or not 0 <= v <= 255 for v in color):
            raise ValueError('Background colors must be RGB triplets')
        colors.add(tuple(color))
    return colors


def load_catalog(source_path, catalog_path):
    raw = source_path.read_bytes()
    if len(raw) > 32 * 1024 * 1024:
        raise ValueError('Source PNG exceeds the importer size limit')
    with Image.open(source_path) as opened:
        if opened.format != 'PNG' or opened.width * opened.height > 4_000_000:
            raise ValueError('Use the original PNG within the supported image size')
        source = opened.convert('RGB')
    catalog = json.loads(catalog_path.read_text(encoding='utf-8'))
    if catalog.get('version') != 1 or catalog.get('sourceSize') != list(source.size):
        raise ValueError('Source image dimensions do not match the crop catalog')
    digest = hashlib.sha256(raw).hexdigest()
    if catalog.get('sourceSha256', digest) != digest:
        raise ValueError('Source PNG differs from the original catalogued file')
    if not isinstance(catalog.get('clips'), list) or not catalog['clips']:
        raise ValueError('The source catalog needs explicit animation clips')
    return source, catalog, digest


def crop_original(source, definition, background_colors):
    x, y, w, h = checked_rect(definition['rect'], source.width, source.height)
    pivot = definition.get('pivot', [w // 2, h])
    if (not isinstance(pivot, list) or len(pivot) != 2
            or any(type(v) not in (int, float) or not math.isfinite(v) for v in pivot)
            or not 0 <= pivot[0] <= w or not 0 <= pivot[1] <= h):
        raise ValueError('A frame pivot must be a finite foot anchor inside its source rectangle')
    offset = (round(FOOT[0] - pivot[0]), round(FOOT[1] - pivot[1]))
    cropped = source.crop((x, y, x + w, y + h))
    values = list(cropped.get_flattened_data())
    rgba = bytearray(w * h * 4)
    for i, color in enumerate(values):
        if color not in background_colors:
            rgba[i * 4:i * 4 + 4] = bytes((*color, 255))
    foreground = Image.frombytes('RGBA', (w, h), bytes(rgba))
    bounds = foreground.getchannel('A').getbbox()
    if bounds is None:
        raise ValueError('Empty source frame: ' + str(definition['rect']))
    target = (offset[0] + bounds[0], offset[1] + bounds[1])
    if min(target) < 0 or offset[0] + bounds[2] > CELL_SIZE or offset[1] + bounds[3] > CELL_SIZE:
        raise ValueError('Visible source pixels escape the native cell: ' + str(definition))
    cell = Image.new('RGBA', (CELL_SIZE, CELL_SIZE))
    cell.paste(foreground.crop(bounds), target)
    # Exact bytes, not a perceptual comparison: no palette substitution, resize,
    # sharpening or drawing is permitted on the original Pilot.
    restored = cell.crop((offset[0], offset[1], offset[0] + w, offset[1] + h))
    if restored.tobytes() != foreground.tobytes():
        raise ValueError('Pilot source pixels changed during placement')
    return cell, offset, hashlib.sha256(foreground.tobytes()).hexdigest()


def components(points):
    remaining = set(points)
    result = []
    while remaining:
        seed = min(remaining, key=lambda p: (p[1], p[0]))
        remaining.remove(seed)
        queue, group = deque([seed]), {seed}
        while queue:
            x, y = queue.popleft()
            for adjacent in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                if adjacent in remaining:
                    remaining.remove(adjacent)
                    group.add(adjacent)
                    queue.append(adjacent)
        result.append(group)
    return result


def head_parts(cell):
    px = cell.load()
    bbox = cell.getchannel('A').getbbox()
    if bbox is None:
        return set(), set()
    top, bottom = bbox[1], bbox[3]
    skin = {(x, y) for y in range(top, bottom) for x in range(bbox[0], bbox[2])
            if px[x, y][3] and px[x, y][:3] in SKIN}
    face_groups = [group for group in components(skin) if len(group) >= 6]
    face = max(face_groups, key=len) if face_groups else set()
    hair = {(x, y) for y in range(top, min(bottom, top + max(9, int((bottom - top) * .7))))
            for x in range(bbox[0], bbox[2]) if px[x, y][3] and px[x, y][:3] in HAIR}
    hair_groups = components(hair)
    if not hair_groups:
        return face, set()
    # The large connected mass is the hair. Tiny brown eye/brow components
    # inside the face must never become the costume's head anchor.
    selected = max(hair_groups, key=len)
    return face, selected


def variant_cell(original, character, clip, index, count):
    if character == 'pilot':
        return original.copy()
    theme = THEMES[character]
    face, hair = head_parts(original)
    out = original.copy()
    old, px = original.load(), out.load()
    ghost = 'ghost' in clip['name'].lower()
    for y in range(CELL_SIZE):
        for x in range(CELL_SIZE):
            r, g, b, a = old[x, y]
            if not a:
                continue
            color = (r, g, b)
            changed = None
            if color in CLOTH:
                changed = theme['cloth'][0 if color in {(151, 140, 96), (93, 214, 77)} else 1]
            elif color in TROUSERS:
                changed = theme['trousers'][0 if color == (57, 79, 120) else 1]
            elif (x, y) in hair:
                luminance = (r * 3 + g * 5 + b) / 9
                changed = theme['hair'][0 if luminance < 35 else 1 if luminance < 77 else 2]
            elif color in SKIN:
                changed = theme['skin']
            elif ghost and max(color) - min(color) < 6 and max(color) > 25:
                value = max(color) / 255
                accent = theme['accent']
                changed = tuple(round(c * (.3 + .7 * value)) for c in accent)
            if changed is not None:
                px[x, y] = (*changed, a)

    # Costume accents are attached to the source head, never drawn at a fixed
    # screen location. Tumbling/fragmented poses retain their exact body geometry.
    name = re.sub('[^a-z]', '', clip['name'].lower())
    unstable = any(token in name for token in ('dodge', 'roll', 'fall', 'death', 'tumble', 'spit', 'mimic'))
    if not hair or unstable or ghost:
        return out
    left, right = min(x for x, _ in hair), max(x for x, _ in hair)
    top = min(y for _, y in hair)
    cx = (left + right) // 2
    draw = ImageDraw.Draw(out)
    dark, mid, light = theme['hair']
    if character == 'medium':
        draw.line((left - 1, top + 1, cx, top - 2, right + 1, top + 1), fill=(*dark, 255))
        draw.line((left, top + 1, cx, top - 1), fill=(*light, 255))
        draw.point((right, top), fill=(*theme['accent'], 255))
    elif character == 'reaper':
        draw.line((left - 1, top + 2, left, top, cx, top - 2, right, top, right + 1, top + 2), fill=(*dark, 255))
        draw.line((left, top + 1, cx, top - 1, right, top + 1), fill=(*light, 255))
        if face and clip['facing'] not in {'Back', 'BackRight'}:
            bottom = max(y for _, y in face)
            center = round(sum(x for x, _ in face) / len(face))
            for tx in (center - 1, center + 1):
                if 0 <= tx < CELL_SIZE and 0 <= bottom < CELL_SIZE and old[tx, bottom][3]:
                    draw.point((tx, bottom), fill=(106, 84, 65, 255))
    else:
        draw.polygon(((left - 1, top), (cx - 2, top - 5), (right + 1, top)), fill=(*dark, 255))
        draw.line((cx - 2, top - 4, cx, top - 1), fill=(*mid, 255))
        draw.line((left - 2, top + 1, right + 2, top + 1), fill=(*light, 255))
        draw.point((cx + 1, top), fill=(220, 184, 84, 255))
    return out


def sprite_rect(cells):
    left, top, right, bottom = 8, 12, 40, 44
    for cell in cells:
        bounds = cell.getchannel('A').getbbox()
        if bounds:
            left, top = min(left, bounds[0]), min(top, bounds[1])
            right, bottom = max(right, bounds[2]), max(bottom, bounds[3])
    if not (0 <= left < FOOT[0] < right <= CELL_SIZE and 0 <= top < FOOT[1] <= bottom <= CELL_SIZE):
        raise ValueError('Frame bounds cannot retain a shared foot anchor')
    return left, top, right, bottom


def draw_preview(sheets, catalog, frame_cells):
    width, height = 1920, 1900
    out = Image.new('RGBA', (width, height), (19, 22, 30, 255))
    draw = ImageDraw.Draw(out)
    glyph_source = (ROOT / 'Assets/_Game/Haunt/Runtime/HauntPixelText.cs').read_text()
    glyphs = dict(re.findall(r"\{ '([A-Z0-9 .:/+\-])', \"([01/]+)\" \}", glyph_source))

    def text(value, x, y, scale=2, color=(206, 218, 208)):
        for char in value.upper():
            for row, line in enumerate(glyphs.get(char, '').split('/')):
                for col, bit in enumerate(line):
                    if bit == '1':
                        draw.rectangle((x + col * scale, y + row * scale,
                                        x + (col + 1) * scale - 1, y + (row + 1) * scale - 1), fill=(*color, 255))
            x += 6 * scale

    text('ECOS DA MEIA-NOITE / MODELO COMPLETO DOS PERSONAGENS', 24, 20, 4, (237, 208, 99))
    text('PILOT: PIXELS ORIGINAIS / VARIANTES: MESMAS POSES E DIRECOES / SEM RENDERIZACAO UNITY', 24, 64, 2)
    regular = [clip for clip in catalog['clips'] if not clip['alternate']]
    names = list(dict.fromkeys(clip['name'] for clip in regular))
    preferred = ['idle', 'walk', 'dodge', 'draw', 'cast', 'flying', 'tableflip', 'kick',
                 'chestopen', 'obtainitem', 'fall', 'death', 'revive']
    normalized = lambda value: re.sub('[^a-z]', '', value.lower())
    ordered = []
    for key in preferred:
        candidate = next((name for name in names if normalized(name) == key or key in normalized(name)), None)
        if candidate and candidate not in ordered:
            ordered.append(candidate)
    ordered.extend(name for name in names if name not in ordered)
    ordered = ordered[:12]
    for col, character in enumerate(CHARACTERS):
        x = 24 + col * 474
        text({'pilot': 'PILOT ORIGINAL', 'medium': 'MEDIUM', 'reaper': 'CEIFADOR', 'alchemist': 'ALQUIMISTA'}[character], x, 112, 3)
        for row, name in enumerate(ordered):
            choices = [clip for clip in regular if clip['name'] == name]
            pose_order = {'OneHanded': 0, 'Empty': 1, 'TwoHanded': 2}
            face_order = {'Front': 0, 'FrontRight': 1, 'Right': 2, 'BackRight': 3, 'Back': 4}
            chosen = min(choices, key=lambda clip: (pose_order[clip['pose']], face_order[clip['facing']]))
            y = 168 + row * 138
            text(name[:22] + ' / ' + str(len(chosen['frames'])) + ' FRAMES', x, y, 2)
            indices = chosen['frames']
            samples = [indices[round(i * (len(indices) - 1) / 3)] for i in range(4)]
            for frame, index in enumerate(samples):
                image = frame_cells[index][col].crop((8, 8, 40, 44))
                scaled = image.resize((96, 108), Image.Resampling.NEAREST)
                out.alpha_composite(scaled, (x + frame * 108, y + 22))
    text('FOLHAS: 48PX POR CELULA / SPRITES NATIVOS SEM AMPLIACAO / 16 PIXELS POR UNIDADE', 24, height - 35, 2)
    return png_bytes(width, height, out.tobytes())


def build_outputs(source_path, catalog_path):
    source, source_catalog, source_digest = load_catalog(source_path, catalog_path)
    definitions, clips, frame_cells, records, keys, names = [], [], [], [], {}, set()
    default_backgrounds = source_catalog.get('backgroundColors', [[144, 187, 187]])
    for source_clip in source_catalog['clips']:
        name = source_clip.get('name')
        facing, pose = source_clip.get('facing', 'Front'), source_clip.get('pose', 'Empty')
        alternate = source_clip.get('alternate', False)
        if not isinstance(name, str) or not re.fullmatch('[A-Za-z][A-Za-z0-9_-]{0,63}', name):
            raise ValueError('Clip names must be simple animation identifiers')
        if facing not in FACINGS or pose not in POSES or type(alternate) is not bool:
            raise ValueError('Invalid clip direction, weapon pose or costume')
        identity = (name, facing, pose, alternate)
        if identity in names:
            raise ValueError('Duplicate source clip: ' + str(identity))
        names.add(identity)
        fps = source_clip.get('fps', 8)
        if not isinstance(fps, (int, float)) or not math.isfinite(fps) or not 0 < fps <= 60:
            raise ValueError('Clip FPS must be finite and within 0-60')
        background_colors = checked_backgrounds(source_clip.get('backgroundColors', default_backgrounds))
        frames = source_clip.get('frames')
        if not isinstance(frames, list) or not frames:
            raise ValueError('A clip needs source frames: ' + name)
        clip = {'name': name, 'facing': facing, 'pose': pose, 'alternate': alternate,
                'fps': fps, 'loop': bool(source_clip.get('loop', True)), 'frames': []}
        for order, definition in enumerate(frames):
            rect = checked_rect(definition['rect'], source.width, source.height)
            pivot = definition.get('pivot', [rect[2] // 2, rect[3]])
            frame_backgrounds = checked_backgrounds(definition.get('backgroundColors', [list(c) for c in sorted(background_colors)]))
            key = (rect, tuple(pivot), tuple(sorted(frame_backgrounds)), name, facing, pose, alternate, order)
            if key not in keys:
                original, offset, original_hash = crop_original(source, definition, frame_backgrounds)
                cells = [variant_cell(original, character, clip, order, len(frames)) for character in CHARACTERS]
                if cells[0].tobytes() != original.tobytes():
                    raise ValueError('Original Pilot was altered by costume generation')
                index = len(frame_cells)
                keys[key] = index
                frame_cells.append(cells)
                bounds = sprite_rect(cells)
                records.append({'sourceRect': list(rect), 'sourcePivot': list(pivot),
                                'sourceRgbaSha256': original_hash, 'bounds': bounds})
            clip['frames'].append(keys[key])
        clips.append(clip)
    if len(frame_cells) > 4096:
        raise ValueError('Source catalogue exceeds the supported number of frames')
    rows = math.ceil(len(frame_cells) / COLUMNS)
    width, height = COLUMNS * CELL_SIZE, rows * CELL_SIZE
    sheets = [Image.new('RGBA', (width, height)) for _ in CHARACTERS]
    manifest_frames = []
    for index, cells in enumerate(frame_cells):
        col, row = index % COLUMNS, index // COLUMNS
        for sheet, cell in zip(sheets, cells):
            sheet.paste(cell, (col * CELL_SIZE, row * CELL_SIZE))
        record = records[index]
        left, top, right, bottom = record.pop('bounds')
        w, h = right - left, bottom - top
        manifest_frames.append({'x': col * CELL_SIZE + left, 'y': height - row * CELL_SIZE - bottom,
                                'width': w, 'height': h, 'pivotX': (FOOT[0] - left) / w,
                                'pivotY': (bottom - FOOT[1]) / h, **record})
    manifest = {'version': 1, 'sourceSha256': source_digest, 'sourceSize': list(source.size),
                'cellSize': CELL_SIZE, 'pixelsPerUnit': 16, 'textureWidth': width, 'textureHeight': height,
                'characters': list(CHARACTERS), 'frames': manifest_frames, 'clips': clips}
    outputs = {}
    outputs[Path(str(RESOURCE_ROOT) + '.meta')] = folder_meta(RESOURCE_ROOT)
    for character, sheet in zip(CHARACTERS, sheets):
        path = RESOURCE_ROOT / (character + '.png')
        outputs[path] = png_bytes(width, height, sheet.tobytes())
        outputs[Path(str(path) + '.meta')] = texture_meta(path).replace(b'spritePixelsToUnits: 32', b'spritePixelsToUnits: 16')
    path = RESOURCE_ROOT / 'catalog.json'
    outputs[path] = (json.dumps(manifest, ensure_ascii=False, indent=2) + '\n').encode('utf-8')
    outputs[Path(str(path) + '.meta')] = json_meta(path)
    outputs[ROOT / 'Documentation/CHARACTER_MODEL.png'] = draw_preview(sheets, manifest, frame_cells)
    return outputs, manifest


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, default=DEFAULT_SOURCE)
    parser.add_argument('--catalog', type=Path, default=DEFAULT_CATALOG)
    parser.add_argument('--check', action='store_true', help='Compare generated outputs without writing')
    args = parser.parse_args()
    try:
        outputs, manifest = build_outputs(args.source, args.catalog)
        changed = []
        for path, data in outputs.items():
            if not path.is_file() or path.read_bytes() != data:
                changed.append(path.relative_to(ROOT).as_posix())
                if not args.check:
                    path.parent.mkdir(parents=True, exist_ok=True)
                    path.write_bytes(data)
        if args.check and changed:
            raise ValueError('Outdated/missing outputs: ' + ', '.join(changed))
        verb = 'Verified' if args.check else 'Imported'
        print(f'{verb} {len(manifest["frames"])} native source frames in {len(manifest["clips"])} clips for four characters.')
        print(f'Pilot RGB/placement verified exactly; source SHA256 {manifest["sourceSha256"]}.')
        print('Source file was not changed. No Unity import, rendering or gameplay validation was performed.')
    except (OSError, ValueError, KeyError, TypeError) as exc:
        parser.exit(1, f'Character import failed: {exc}\n')


if __name__ == '__main__':
    main()
