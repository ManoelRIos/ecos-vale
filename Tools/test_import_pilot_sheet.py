import hashlib
from io import BytesIO
import json
from pathlib import Path
import unittest

from PIL import Image
from import_pilot_sheet import build_outputs, crop_original, checked_rect, ROOT, RESOURCE_ROOT, DEFAULT_SOURCE, DEFAULT_CATALOG, CELL_SIZE, COLUMNS, FOOT


class PilotSheetImportTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.outputs, cls.catalog = build_outputs(DEFAULT_SOURCE, DEFAULT_CATALOG)
        cls.definitions = json.loads(DEFAULT_CATALOG.read_text())
        cls.source = Image.open(DEFAULT_SOURCE).convert('RGB')
        cls.atlas = Image.open(BytesIO(cls.outputs[RESOURCE_ROOT / 'pilot.png'])).convert('RGBA')

    def test_original_png_is_byte_identical_to_the_uploaded_sheet(self):
        self.assertEqual('cd640095da82003f8f44902950382692518393bfa57e61bc9bba1f3abec8f469', hashlib.sha256(DEFAULT_SOURCE.read_bytes()).hexdigest())
        self.assertEqual(self.catalog['sourceSha256'], self.definitions['sourceSha256'])

    def test_every_original_foreground_pixel_survives_and_is_inside_its_runtime_sprite(self):
        records = self.catalog['frames']
        total = 0
        for clip in self.definitions['clips']:
            output_clip = next(c for c in self.catalog['clips'] if all(c[k] == clip[k] for k in ('name', 'facing', 'pose', 'alternate')))
            colors = {tuple(c) for c in clip.get('backgroundColors', self.definitions['backgroundColors'])}
            for definition, index in zip(clip['frames'], output_clip['frames']):
                x, y, w, h = definition['rect']
                px, py = definition['pivot']
                dx, dy = round(FOOT[0] - px), round(FOOT[1] - py)
                cell_x, cell_y = (index % COLUMNS) * CELL_SIZE, (index // COLUMNS) * CELL_SIZE
                record = records[index]
                left, top = record['x'], self.catalog['textureHeight'] - record['y'] - record['height']
                opaque = 0
                for sy in range(h):
                    for sx in range(w):
                        color = self.source.getpixel((x + sx, y + sy))
                        if color in colors:
                            continue
                        tx, ty = cell_x + dx + sx, cell_y + dy + sy
                        self.assertEqual((*color, 255), self.atlas.getpixel((tx, ty)), (clip['name'], index, sx, sy))
                        self.assertTrue(left <= tx < left + record['width'] and top <= ty < top + record['height'], (clip['name'], index, 'clipped sprite'))
                        opaque += 1
                cell = self.atlas.crop((cell_x, cell_y, cell_x + CELL_SIZE, cell_y + CELL_SIZE))
                self.assertEqual(opaque, sum(a > 0 for a in cell.getchannel('A').get_flattened_data()), (clip['name'], index, 'extra pixels'))
                total += 1
        self.assertEqual(len(records), total)

    def test_variants_share_all_frame_rectangles_without_replacing_the_original(self):
        raw = self.outputs[RESOURCE_ROOT / 'pilot.png']
        for name in ('medium', 'reaper', 'alchemist'):
            variant = self.outputs[RESOURCE_ROOT / (name + '.png')]
            self.assertNotEqual(raw, variant)
            with Image.open(BytesIO(variant)) as image:
                self.assertEqual(self.atlas.size, image.size)
        self.assertEqual(16, self.catalog['pixelsPerUnit'])
        self.assertNotIn('equipment', self.catalog)

    def test_shadow_color_is_preserved_but_panel_background_is_transparent(self):
        shadow = next(c for c in self.catalog['clips'] if c['name'] == 'Shadow')
        record = self.catalog['frames'][shadow['frames'][0]]
        left = record['x']; top = self.catalog['textureHeight'] - record['y'] - record['height']
        pixels = self.atlas.crop((left, top, left + record['width'], top + record['height'])).get_flattened_data()
        self.assertIn((169, 223, 216, 255), list(pixels))
        for clip in self.definitions['clips']:
            if clip['name'] == 'SpitOut':
                self.assertIn([169, 223, 216], clip['backgroundColors'])
                self.assertGreaterEqual(clip['frames'][0]['rect'][1], 758 if not clip['alternate'] else 635)

    def test_bad_rectangles_and_visible_clipping_are_rejected(self):
        with self.assertRaises(ValueError):
            checked_rect([-1, 0, 10, 10], 20, 20)
        with self.assertRaises(ValueError):
            checked_rect([0, 0, 21, 20], 20, 20)
        image = Image.new('RGB', (48, 48), (255, 0, 0))
        with self.assertRaises(ValueError):
            crop_original(image, {'rect': [0, 0, 48, 48], 'pivot': [48, 48]}, {(0, 0, 0)})

    def test_outputs_match_checked_in_assets_and_source_is_unchanged(self):
        for path, data in self.outputs.items():
            self.assertEqual(data, path.read_bytes(), str(path.relative_to(ROOT)))
        self.assertEqual(self.catalog['sourceSha256'], hashlib.sha256(DEFAULT_SOURCE.read_bytes()).hexdigest())


if __name__ == '__main__':
    unittest.main()
