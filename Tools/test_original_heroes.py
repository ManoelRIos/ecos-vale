import json
from pathlib import Path
import unittest

from PIL import Image

import generate_original_heroes as heroes


class OriginalHeroSheetTests(unittest.TestCase):
    def test_every_catalog_frame_is_drawn_and_feet_rest_on_the_pivot(self):
        catalog = json.loads((heroes.SHEETS / 'catalog.json').read_text())
        for name in catalog['characters']:
            sheet = Image.open(heroes.SHEETS / (name + '.png')).convert('RGBA')
            self.assertEqual(sheet.size, (catalog['textureWidth'], catalog['textureHeight']))
            for clip in catalog['clips']:
                if clip['name'] == 'Shadow':
                    continue
                for index in clip['frames']:
                    f = catalog['frames'][index]
                    top = catalog['textureHeight'] - (f['y'] + f['height'])
                    cell = sheet.crop((f['x'], top, f['x'] + f['width'], top + f['height']))
                    box = cell.getbbox()
                    self.assertIsNotNone(box, (name, clip['name'], index))
                    # Nothing is drawn below the shared foot line (4 px above the frame bottom).
                    self.assertLessEqual(box[3], f['height'] - 3, (name, clip['name'], index))

    def test_costumes_are_distinct(self):
        coats = {c['coat'] for c in heroes.COSTUMES.values()}
        self.assertEqual(len(coats), len(heroes.COSTUMES))

    def test_drawing_is_deterministic(self):
        a = heroes.hero('pilot', 'Walk', 'Right', 'OneHanded', 2, 6, 32)
        b = heroes.hero('pilot', 'Walk', 'Right', 'OneHanded', 2, 6, 32)
        self.assertEqual(a.tobytes(), b.tobytes())


if __name__ == '__main__':
    unittest.main()
