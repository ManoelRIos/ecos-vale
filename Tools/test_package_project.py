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
            'com.unity.modules.physics': '1.0.0',
            'com.unity.modules.physics2d': '1.0.0',
        }}))
        self.write('ProjectSettings/ProjectVersion.txt', 'm_EditorVersion: 6000.0.85f1\n')
        self.write('Assets/_Game/Scenes/Valley.unity', 'entry scene\n')
        self.write('Assets/_Game/Scenes/Valley.unity.meta', 'fileFormatVersion: 2\nguid: abc\n')
        self.write('Tools/CoreChecks.csproj', '<Project />\n')
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
        self.write('Assets/_Game/Scenes/Valley.unity', 'working scene update\n')
        self.write('not-tracked.txt', 'must not ship\n')
        output = create_archive(self.root)
        self.assertEqual(self.root / 'Distribution/EcosDoVale.zip', output)
        with zipfile.ZipFile(output) as archive:
            names = archive.namelist()
            self.assertTrue(all(name.startswith('EcosDoVale/') for name in names))
            self.assertIn('EcosDoVale/Tools/CoreChecks.csproj', names)
            self.assertIn('EcosDoVale/Tools/package_project.py', names)
            self.assertEqual(b'working scene update\n', archive.read('EcosDoVale/Assets/_Game/Scenes/Valley.unity'))
            self.assertEqual(b'fileFormatVersion: 2\nguid: abc\n', archive.read('EcosDoVale/Assets/_Game/Scenes/Valley.unity.meta'))
            self.assertNotIn('EcosDoVale/not-tracked.txt', names)
            self.assertTrue(all(entry.date_time == FIXED_TIMESTAMP for entry in archive.infolist()))
            self.assertIsNone(archive.testzip())

    def test_folder_metadata_preserves_empty_directory_missing_after_git_clone(self):
        metadata = 'fileFormatVersion: 2\nfolderAsset: yes\nguid: empty-folder\n'
        self.write('Assets/_Game/ScriptableObjects.meta', metadata)
        self.git('add', '--all')
        empty = self.root / 'Assets/_Game/ScriptableObjects'
        self.assertFalse(empty.exists())

        output = create_archive(self.root)
        without_directory = output.read_bytes()
        with zipfile.ZipFile(output) as archive:
            directory = archive.getinfo('EcosDoVale/Assets/_Game/ScriptableObjects/')
            self.assertTrue(directory.is_dir())
            self.assertEqual(FIXED_TIMESTAMP, directory.date_time)
            self.assertEqual(metadata.encode(), archive.read('EcosDoVale/Assets/_Game/ScriptableObjects.meta'))
        self.assertFalse(empty.exists(), 'Packaging must not create source folders')

        # A working checkout and a clean clone must produce the same delivery.
        empty.mkdir()
        self.assertEqual(without_directory, create_archive(self.root).read_bytes())

    def test_missing_physics2d_rejects_delivery_and_preserves_existing_output(self):
        output = create_archive(self.root)
        previous = output.read_bytes()
        self.write('Packages/manifest.json', '{"dependencies":{"com.unity.modules.physics":"1.0.0"}}')
        with self.assertRaisesRegex(PackagingError, 'Physics2D'):
            create_archive(self.root)
        self.assertEqual(previous, output.read_bytes())

    def test_wrong_editor_rejects_delivery(self):
        self.write('ProjectSettings/ProjectVersion.txt', 'm_EditorVersion: 6000.0.40f1\n')
        with self.assertRaisesRegex(PackagingError, '6000.0.85f1'):
            create_archive(self.root)

    def test_excludes_distribution_and_tracked_caches_without_recursive_zip(self):
        excluded = ['Distribution/EcosDoVale.zip', 'Distribution/old.zip', 'Library/cache.bin',
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
                self.assertNotIn('EcosDoVale/' + name, names)
            self.assertFalse(any('.git/' in name for name in names))

    def test_archive_bytes_are_deterministic_and_ignore_mtimes(self):
        first = create_archive(self.root).read_bytes()
        os.utime(self.root / 'Assets/_Game/Scenes/Valley.unity', (1000000000, 1000000000))
        second = create_archive(self.root).read_bytes()
        self.assertEqual(first, second)

    def test_required_project_files_must_be_tracked(self):
        self.git('rm', '--cached', 'Packages/manifest.json')
        with self.assertRaisesRegex(PackagingError, 'must be tracked'):
            create_archive(self.root)

    def test_missing_tracked_file_rejects_delivery(self):
        (self.root / 'Tools/CoreChecks.csproj').unlink()
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
