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
        }}))
        self.write('ProjectSettings/ProjectVersion.txt', 'm_EditorVersion: 6000.0.85f1\n')
        self.write('Assets/_Game/Scenes/Haunt.unity', 'entry scene\n')
        self.write('Assets/_Game/Scenes/Haunt.unity.meta', 'fileFormatVersion: 2\nguid: abc\n')
        self.write('ProjectSettings/EditorBuildSettings.asset',
                   '  - enabled: 1\n    path: Assets/_Game/Scenes/Haunt.unity\n')
        self.write('ABRIR_ECOS_DA_MEIA_NOITE.cmd', '@echo off\n')
        for relative in ['Assets/_Game/Haunt/Runtime/HauntGame.cs',
                         'Assets/_Game/Haunt/Runtime/HauntGame.UI.cs',
                         'Assets/_Game/Haunt/Runtime/HauntGame.Visuals.cs',
                         'Assets/_Game/Haunt/Runtime/HauntRendering.cs',
                         'Assets/_Game/Haunt/Runtime/HauntPixelText.cs',
                         'Assets/_Game/Haunt/Runtime/HauntGame.Weapons.cs',
                         'Assets/_Game/Haunt/Runtime/HauntGame.Audio.cs',
                         'Assets/_Game/Haunt/Art/HauntPixelArt.cs',
                         'Assets/_Game/Haunt/Core/DungeonFloor.cs',
                         'Assets/_Game/Haunt/Core/RunState.cs',
                         'Assets/_Game/Haunt/Core/CombatGeometry.cs',
                         'Assets/_Game/Haunt/Editor/HauntProject.cs',
                         'Assets/_Game/Haunt/Editor/HauntSetup.cs',
                         'Assets/_Game/Haunt/Haunt.Runtime.asmdef',
                         'Assets/_Game/Haunt/Editor/Haunt.Editor.asmdef',
                         'Assets/_Game/Haunt/Art/HauntSprites.shader',
                         'Assets/_Game/Resources/HauntSprites.mat']:
            self.write(relative, 'required source\n')
            self.write(relative + '.meta', 'fileFormatVersion: 2\nguid: fixture\n')
        self.write('Tools/HauntChecks.csproj', '<Project />\n')
        self.write('Tools/package_project.py', '# tracked packaging helper\n')
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

    def test_extra_build_scene_rejects_delivery(self):
        self.write('ProjectSettings/EditorBuildSettings.asset',
                   '  - enabled: 1\n    path: Assets/_Game/Scenes/Haunt.unity\n'
                   '  - enabled: 0\n    path: Assets/OtherScene.unity\n')
        with self.assertRaisesRegex(PackagingError, 'only the Haunt'):
            create_archive(self.root)

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
