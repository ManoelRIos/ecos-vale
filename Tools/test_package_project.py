import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest
import zipfile

from package_project import create_archive, PackagingError, FIXED_TIMESTAMP


class PackageProjectTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix='ecos-package-tests-')
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name) / 'project'
        self.root.mkdir()
        self.git('init', '--quiet')
        self.write('Packages/manifest.json', json.dumps({'dependencies': {
            'com.unity.render-pipelines.universal': '17.0.4',
            'com.unity.inputsystem': '1.11.2',
            'com.unity.test-framework': '1.4.5',
            'com.unity.modules.physics': '1.0.0',
        }}))
        self.write('ProjectSettings/ProjectVersion.txt', 'm_EditorVersion: 6000.0.85f1\n')
        self.write('Assets/_Game/Scenes/Haunt.unity', 'entry scene\n')
        self.write('Assets/_Game/Scenes/Haunt.unity.meta', 'fileFormatVersion: 2\nguid: abc\n')
        self.write('Assets/_Game/Scenes/HauntFps.unity', 'first person entry scene\n')
        self.write('Assets/_Game/Scenes/HauntFps.unity.meta', 'fileFormatVersion: 2\nguid: fps-fixture\n')
        self.write('Assets/_Game/Scenes/HauntLowPoly.unity', 'low poly entry scene\n  lowPolyMode: 1\n')
        self.write('Assets/_Game/Scenes/HauntLowPoly.unity.meta', 'fileFormatVersion: 2\nguid: lowpoly-fixture\n')
        self.write('ProjectSettings/EditorBuildSettings.asset',
                   '  - enabled: 1\n    path: Assets/_Game/Scenes/HauntLowPoly.unity\n'
                   '  - enabled: 1\n    path: Assets/_Game/Scenes/HauntFps.unity\n'
                   '  - enabled: 1\n    path: Assets/_Game/Scenes/Haunt.unity\n')
        self.write('ABRIR_ECOS_DA_MEIA_NOITE.cmd', '@echo off\n')
        for relative in ['Assets/_Game/Haunt/Runtime/HauntGame.cs',
                         'Assets/_Game/Haunt/Runtime/HauntGame.UI.cs',
                         'Assets/_Game/Haunt/Runtime/HauntGame.Visuals.cs',
                         'Assets/_Game/Haunt/Runtime/HauntRendering.cs',
                         'Assets/_Game/Haunt/Runtime/HauntPixelText.cs',
                         'Assets/_Game/Haunt/Runtime/HauntGame.Weapons.cs',
                         'Assets/_Game/Haunt/Runtime/HauntGame.Ammunition.cs',
                         'Assets/_Game/Haunt/Runtime/HauntGame.SpriteAnimation.cs',
                         'Assets/_Game/Haunt/Runtime/HauntGame.Audio.cs',
                         'Assets/_Game/Haunt/Runtime/HauntGame.Input.cs',
                         'Assets/_Game/Haunt/Runtime/HauntGame.Environment.cs',
                         'Assets/_Game/Haunt/Runtime/HauntGame.Rooms.cs',
                         'Assets/_Game/Haunt/Runtime/HauntGame.Bestiary.cs',
                         'Assets/_Game/Haunt/Runtime/HauntGame.BossPresentation.cs',
                         'Assets/_Game/Haunt/Runtime/HauntGame.Conversations.cs',
                         'Assets/_Game/Haunt/Runtime/HauntGame.Feel.cs',
                         'Assets/_Game/Haunt/Runtime/HauntGame.Patterns.cs',
                         'Assets/_Game/Haunt/Runtime/HauntGame.Lighting.cs',
                         'Assets/_Game/Haunt/Runtime/HauntFlickerLight.cs',
                         'Assets/_Game/Haunt/Art/HauntPixelArt.cs',
                         'Assets/_Game/Haunt/Art/HauntPixelArt.Actors.cs',
                         'Assets/_Game/Haunt/Art/HauntPixelArt.Equipment.cs',
                         'Assets/_Game/Haunt/Art/HauntPixelArt.Characters.cs',
                         'Assets/_Game/Haunt/Art/HauntPixelArt.ReferenceBosses.cs',
                         'Assets/_Game/Haunt/Art/HauntPixelArt.SpriteSheets.cs',
                         'Assets/_Game/Haunt/Core/DungeonFloor.cs',
                         'Assets/_Game/Haunt/Core/RunState.cs',
                         'Assets/_Game/Haunt/Core/WeaponAmmo.cs',
                         'Assets/_Game/Haunt/Core/CombatGeometry.cs',
                         'Assets/_Game/Haunt/Core/BulletPatternGenerator.cs',
                         'Assets/_Game/Haunt/Core/DodgeRules.cs',
                         'Assets/_Game/Haunt/Core/RoomLayout.cs',
                         'Assets/_Game/Haunt/Core/FpsPrototypeRules.cs',
                         'Assets/_Game/Haunt/Editor/HauntProject.cs',
                         'Assets/_Game/Haunt/Editor/HauntFpsProject.cs',
                         'Assets/_Game/Haunt/Editor/HauntSetup.cs',
                         'Assets/_Game/Haunt/Haunt.Runtime.asmdef',
                         'Assets/_Game/Haunt/Editor/Haunt.Editor.asmdef',
                         'Assets/_Game/Haunt/Art/HauntSprites.shader',
                         'Assets/_Game/Resources/HauntSprites.mat']:
            self.write(relative, 'required source\n')
            self.write(relative + '.meta', 'fileFormatVersion: 2\nguid: fixture\n')
        self.write('Tools/HauntChecks.csproj', '<Project />\n')
        self.write('Tools/package_project.py', '# tracked packaging helper\n')
        for name in ('actors/player', 'actors/player-body', 'actors/medium', 'actors/medium-body',
                     'actors/reaper', 'actors/reaper-body', 'actors/alchemist', 'actors/alchemist-body',
                     'actors/scarecrow', 'actors/gargoyle', 'actors/stalker-shadow', 'actors/fire-bat',
                     'actors/skeleton', 'actors/slime', 'bosses/boss', 'bosses/widow', 'bosses/bishop',
                     'bosses/matriarch', 'bosses/bell', 'bosses/iron-executioner', 'bosses/bog-oracle',
                     'props/barrel-explosive', 'props/barrel-acid', 'props/decrepit-chest'):
            self.write('Assets/_Game/Resources/ReferenceSheets/' + name + '.png', 'sheet fixture\n')
            self.write('Assets/_Game/Resources/ReferenceSheets/' + name + '.png.meta', 'texture fixture\n')
        self.write('Assets/_Game/Resources/ReferenceSheets/manifest.json', '{}\n')
        self.write('Assets/_Game/Resources/ReferenceSheets/manifest.json.meta', 'manifest metadata\n')
        for name in ('pilot', 'medium', 'reaper', 'alchemist'):
            self.write('Assets/_Game/Resources/CharacterSheets/' + name + '.png', 'native sheet fixture\n')
            self.write('Assets/_Game/Resources/CharacterSheets/' + name + '.png.meta', 'native metadata\n')
        for name in ('Assets/_Game/Resources/CharacterSheets/catalog.json',
                     'Assets/_Game/Resources/CharacterSheets/catalog.json.meta',
                     'Assets/_Game/ArtSources/PilotOriginal.png', 'Assets/_Game/ArtSources/PilotOriginal.png.meta',
                     'Tools/Pilot/source-clips.json', 'Tools/import_pilot_sheet.py', 'Tools/requirements-sprite-import.txt'):
            self.write(name, 'native source fixture\n')
        for name in ('Tools/export_sprite_sheets.py', 'Tools/SpriteSheetExport/Program.cs',
                     'Tools/SpriteSheetExport/UnityPixelStubs.cs', 'Tools/SpriteSheetExport/SpriteSheetExport.csproj'):
            self.write(name, 'exporter fixture\n')
        for name in ('HauntFpsGame.cs', 'HauntFpsGame.UI.cs', 'HauntFpsGame.Campaign.cs', 'HauntFpsGame.LowPoly.cs', 'HauntFpsGame.LowPolyUI.cs',
                     'HauntFpsPlayer.cs', 'HauntFpsPerspective.cs', 'HauntFpsHybridArt.cs', 'HauntFpsHeroBody.cs',
                     'HauntFpsWorld.cs', 'HauntFpsCampaign.cs', 'HauntFpsEnemy.cs',
                     'HauntFpsEnemy.Models.cs', 'HauntFpsBoss.cs', 'HauntFpsBarrel.cs',
                     'HauntFpsAudio.cs', 'HauntFpsLit.shader'):
            self.write('Assets/_Game/Haunt/Fps/' + name, 'fps source fixture\n')
            self.write('Assets/_Game/Haunt/Fps/' + name + '.meta', 'fps metadata fixture\n')
        self.write('Assets/_Game/Resources/FpsPalette.mat', 'fps material fixture\n')
        self.write('Assets/_Game/Resources/FpsPalette.mat.meta', 'fps material metadata\n')
        for name in ('HauntLowPolyCamera.cs', 'HauntLowPolyPrimitives.cs', 'HauntLowPolyDoors.cs',
                     'HauntLowPolyArsenal.cs', 'HauntLowPolyToon.shader'):
            self.write('Assets/_Game/Haunt/LowPoly/' + name, 'low poly source fixture\n')
            self.write('Assets/_Game/Haunt/LowPoly/' + name + '.meta', 'low poly metadata fixture\n')
        for name in ('Assets/_Game/Haunt/Editor/HauntLowPolyProject.cs', 'Assets/_Game/Resources/LowPolyPalette.mat'):
            self.write(name, 'low poly resource fixture\n')
            self.write(name + '.meta', 'low poly resource metadata fixture\n')
        self.git('add', '--all')

    def git(self, *args):
        return subprocess.run(['git', '-C', str(self.root), *args], check=True,
                              stdout=subprocess.PIPE, stderr=subprocess.PIPE)

    def write(self, relative, contents):
        path = self.root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(contents)
        return path

    def test_short_root_preserves_project_metadata_and_uses_working_contents(self):
        self.write('Assets/_Game/Scenes/Haunt.unity', 'working scene update\n')
        self.write('not-tracked.txt', 'must not ship\n')
        output = create_archive(self.root)
        self.assertEqual(self.root / 'Distribution/EcosDaMeiaNoite.zip', output)
        with zipfile.ZipFile(output) as archive:
            names = archive.namelist()
            self.assertTrue(all(name.startswith('EcosDaMeiaNoite/') for name in names))
            self.assertIn('EcosDaMeiaNoite/Tools/HauntChecks.csproj', names)
            self.assertIn('EcosDaMeiaNoite/Tools/package_project.py', names)
            self.assertIn('EcosDaMeiaNoite/Assets/_Game/Scenes/HauntFps.unity', names)
            self.assertIn('EcosDaMeiaNoite/Assets/_Game/Scenes/HauntLowPoly.unity', names)
            self.assertIn('EcosDaMeiaNoite/Assets/_Game/Haunt/Fps/HauntFpsGame.cs', names)
            self.assertEqual(b'working scene update\n', archive.read('EcosDaMeiaNoite/Assets/_Game/Scenes/Haunt.unity'))
            self.assertEqual(b'fileFormatVersion: 2\nguid: abc\n', archive.read('EcosDaMeiaNoite/Assets/_Game/Scenes/Haunt.unity.meta'))
            self.assertNotIn('EcosDaMeiaNoite/not-tracked.txt', names)
            self.assertTrue(all(entry.date_time == FIXED_TIMESTAMP for entry in archive.infolist()))
            self.assertIsNone(archive.testzip())

    def test_folder_metadata_preserves_empty_directory_missing_after_git_clone(self):
        metadata = 'fileFormatVersion: 2\nfolderAsset: yes\nguid: empty-folder\n'
        self.write('Assets/_Game/Haunt/Prefabs.meta', metadata)
        self.git('add', '--all')
        empty = self.root / 'Assets/_Game/Haunt/Prefabs'
        self.assertFalse(empty.exists())

        output = create_archive(self.root)
        without_directory = output.read_bytes()
        with zipfile.ZipFile(output) as archive:
            directory = archive.getinfo('EcosDaMeiaNoite/Assets/_Game/Haunt/Prefabs/')
            self.assertTrue(directory.is_dir())
            self.assertEqual(FIXED_TIMESTAMP, directory.date_time)
            self.assertEqual(metadata.encode(), archive.read('EcosDaMeiaNoite/Assets/_Game/Haunt/Prefabs.meta'))
        self.assertFalse(empty.exists(), 'Packaging must not create source folders')

        # A working checkout and a clean clone must produce the same delivery.
        empty.mkdir()
        self.assertEqual(without_directory, create_archive(self.root).read_bytes())

    def test_missing_input_system_rejects_delivery_and_preserves_existing_output(self):
        output = create_archive(self.root)
        previous = output.read_bytes()
        self.write('Packages/manifest.json', json.dumps({'dependencies': {'com.unity.render-pipelines.universal': '17.0.4'}}))
        with self.assertRaisesRegex(PackagingError, 'inputsystem'):
            create_archive(self.root)
        self.assertEqual(previous, output.read_bytes())

    def test_wrong_editor_rejects_delivery(self):
        self.write('ProjectSettings/ProjectVersion.txt', 'm_EditorVersion: 6000.0.40f1\n')
        with self.assertRaisesRegex(PackagingError, '6000.0.85f1'):
            create_archive(self.root)

    def test_excludes_distribution_and_tracked_caches_without_recursive_zip(self):
        excluded = ['Distribution/EcosDaMeiaNoite.zip', 'Distribution/old.zip', 'Library/cache.bin',
                    'Library_backup/cache.bin',
                    'Temp/generated.txt', 'Builds/game.exe', 'Logs/Editor.log',
                    'Tools/bin/Debug/tool.dll', 'Tools/obj/cache.json', 'Tools/__pycache__/x.pyc']
        for name in excluded:
            self.write(name, 'generated content')
        self.git('add', '--all')
        output = create_archive(self.root)
        with zipfile.ZipFile(output) as archive:
            names = archive.namelist()
            for name in excluded:
                self.assertNotIn('EcosDaMeiaNoite/' + name, names)
            self.assertFalse(any('.git/' in name for name in names))

    def test_archive_bytes_are_deterministic_and_ignore_mtimes(self):
        first = create_archive(self.root).read_bytes()
        os.utime(self.root / 'Assets/_Game/Scenes/Haunt.unity', (1000000000, 1000000000))
        second = create_archive(self.root).read_bytes()
        self.assertEqual(first, second)

    def test_required_project_files_must_be_tracked(self):
        self.git('rm', '--cached', 'Packages/manifest.json')
        with self.assertRaisesRegex(PackagingError, 'must be tracked'):
            create_archive(self.root)

    def test_new_game_bootstrap_and_material_must_be_tracked(self):
        self.git('rm', '--cached', 'Assets/_Game/Haunt/Runtime/HauntGame.cs')
        with self.assertRaisesRegex(PackagingError, 'must be tracked'):
            create_archive(self.root)

    def test_missing_hybrid_components_or_metadata_preserve_the_previous_delivery(self):
        output = create_archive(self.root)
        previous = output.read_bytes()
        for component in ('HauntFpsPerspective.cs', 'HauntFpsHybridArt.cs', 'HauntFpsHeroBody.cs', 'HauntFpsCampaign.cs',
                          'HauntFpsGame.Campaign.cs', 'HauntFpsBoss.cs', 'HauntFpsEnemy.Models.cs'):
            for suffix in ('', '.meta'):
                name = 'Assets/_Game/Haunt/Fps/' + component + suffix
                with self.subTest(missing=name):
                    self.git('rm', '--cached', name)
                    with self.assertRaisesRegex(PackagingError, 'must be tracked'):
                        create_archive(self.root)
                    self.assertEqual(previous, output.read_bytes())
                    self.git('add', name)

    def test_extra_build_scene_rejects_delivery(self):
        self.write('ProjectSettings/EditorBuildSettings.asset',
                   '  - enabled: 1\n    path: Assets/_Game/Scenes/HauntLowPoly.unity\n'
                   '  - enabled: 1\n    path: Assets/_Game/Scenes/HauntFps.unity\n'
                   '  - enabled: 1\n    path: Assets/_Game/Scenes/Haunt.unity\n'
                   '  - enabled: 0\n    path: Assets/OtherScene.unity\n')
        with self.assertRaisesRegex(PackagingError, 'low-poly entry followed'):
            create_archive(self.root)

    def test_lowpoly_sources_material_and_metadata_cannot_be_omitted(self):
        output = create_archive(self.root)
        previous = output.read_bytes()
        for name in ('Assets/_Game/Haunt/LowPoly/HauntLowPolyCamera.cs',
                     'Assets/_Game/Haunt/LowPoly/HauntLowPolyPrimitives.cs',
                     'Assets/_Game/Haunt/LowPoly/HauntLowPolyDoors.cs',
                     'Assets/_Game/Haunt/LowPoly/HauntLowPolyArsenal.cs',
                     'Assets/_Game/Haunt/LowPoly/HauntLowPolyToon.shader',
                     'Assets/_Game/Haunt/Fps/HauntFpsGame.LowPoly.cs',
                     'Assets/_Game/Haunt/Editor/HauntLowPolyProject.cs',
                     'Assets/_Game/Resources/LowPolyPalette.mat',
                     'Assets/_Game/Scenes/HauntLowPoly.unity'):
            for suffix in ('', '.meta'):
                with self.subTest(missing=name + suffix):
                    self.git('rm', '--cached', name + suffix)
                    with self.assertRaisesRegex(PackagingError, 'must be tracked'):
                        create_archive(self.root)
                    self.assertEqual(previous, output.read_bytes())
                    self.git('add', name + suffix)

    def test_lowpoly_must_remain_the_enabled_entry_and_preserve_both_earlier_scenes(self):
        output = create_archive(self.root)
        previous = output.read_bytes()
        for paths, enabled, error in [
            (['HauntFps', 'HauntLowPoly', 'Haunt'], [1, 1, 1], 'low-poly entry followed'),
            (['HauntLowPoly'], [1], 'low-poly entry followed'),
            (['HauntLowPoly', 'HauntFps', 'Haunt'], [0, 1, 1], 'All three game scenes'),
            (['HauntLowPoly', 'HauntFps', 'Haunt'], [1, 0, 1], 'All three game scenes'),
            (['HauntLowPoly', 'HauntFps', 'Haunt'], [1, 1, 0], 'All three game scenes'),
        ]:
            with self.subTest(paths=paths, enabled=enabled):
                self.write('ProjectSettings/EditorBuildSettings.asset', ''.join(
                    f'  - enabled: {flag}\n    path: Assets/_Game/Scenes/{name}.unity\n'
                    for name, flag in zip(paths, enabled)))
                with self.assertRaisesRegex(PackagingError, error):
                    create_archive(self.root)
                self.assertEqual(previous, output.read_bytes())

    def test_lowpoly_entry_cannot_silently_start_the_legacy_profile(self):
        output = create_archive(self.root)
        previous = output.read_bytes()
        for contents in ('low poly entry scene\n', 'low poly entry scene\n  lowPolyMode: 0\n'):
            with self.subTest(contents=contents):
                self.write('Assets/_Game/Scenes/HauntLowPoly.unity', contents)
                with self.assertRaisesRegex(PackagingError, 'lowPolyMode profile'):
                    create_archive(self.root)
                self.assertEqual(previous, output.read_bytes())

    def test_missing_tracked_file_rejects_delivery(self):
        (self.root / 'Tools/HauntChecks.csproj').unlink()
        with self.assertRaisesRegex(PackagingError, 'missing or not a file'):
            create_archive(self.root)

    def test_symlink_sources_and_escaping_output_are_rejected(self):
        outside = Path(self.temporary.name) / 'outside.txt'
        outside.write_text('not part of the project')
        link = self.root / 'linked.txt'
        try:
            link.symlink_to(outside)
        except OSError:
            self.skipTest('Symlinks unavailable on this test platform')
        self.git('add', '--all')
        with self.assertRaisesRegex(PackagingError, 'Symlinks'):
            create_archive(self.root)
        self.git('rm', '--cached', 'linked.txt')
        link.unlink()
        with self.assertRaisesRegex(PackagingError, 'escapes'):
            create_archive(self.root, Path(self.temporary.name) / 'outside.zip')


if __name__ == '__main__':
    unittest.main()
