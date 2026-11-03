"""Static contracts for Windows launchers; these do not execute cmd.exe or Unity."""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


class LauncherContractTests(unittest.TestCase):
    def setUp(self):
        self.launcher = (ROOT / 'ABRIR_ECOS_DA_MEIA_NOITE.cmd').read_text()

    def test_launch_uses_selected_project_and_queued_roguelike_entry(self):
        self.assertIn('cd /d "%~dp0"', self.launcher)
        launches = [line for line in self.launcher.splitlines() if line.startswith('start ')]
        self.assertEqual(1, len(launches))
        self.assertIn('-projectPath "%CD%"', launches[0])
        self.assertIn('-executeMethod Ecos.Haunt.Editor.HauntProject.OpenFromCommandLine', launches[0])
        self.assertIn('-logFile "%CD%\\Logs\\MeiaNoite-Editor.log"', launches[0])
        self.assertNotIn('-quit', launches[0])

    def test_partial_project_is_rejected_before_launch(self):
        for relative in ['Assets/_Game/Scenes/Haunt.unity',
                         'Assets/_Game/Haunt/Runtime/HauntGame.cs',
                         'Assets/_Game/Haunt/Editor/HauntProject.cs']:
            self.assertTrue((ROOT / relative).is_file(), relative)
            guard = 'if not exist "' + relative.replace('/', '\\') + '" goto MISSING_ROGUELIKE'
            self.assertIn(guard, self.launcher)
            self.assertLess(self.launcher.index(guard), self.launcher.index('start '))

    def test_check_mode_validates_project_without_launching_editor(self):
        check = 'if /I "%~1"=="/check" exit /b 0'
        self.assertIn(check, self.launcher)
        self.assertLess(self.launcher.index('goto WRONG_VERSION'), self.launcher.index(check))
        self.assertLess(self.launcher.index(check), self.launcher.index('goto MISSING_EDITOR'))
        self.assertLess(self.launcher.index(check), self.launcher.index('start '))

    def test_editor_pin_and_path_guard_match_project(self):
        version = re.search(r'^m_EditorVersion: (.+)$',
                            (ROOT / 'ProjectSettings/ProjectVersion.txt').read_text(), re.M).group(1)
        self.assertIn('/C:"m_EditorVersion: ' + version + '"', self.launcher)
        self.assertIn('Editor\\' + version + '\\Editor\\Unity.exe', self.launcher)
        self.assertIn('goto PATH_TOO_LONG', self.launcher)

    def test_failure_routes_exist_and_return_failure(self):
        labels = set(re.findall(r'^:([A-Z_0-9]+)$', self.launcher, re.M))
        targets = set(re.findall(r'\bgoto ([A-Z_0-9]+)', self.launcher))
        self.assertTrue(targets <= labels, targets - labels)
        self.assertIn(':FAILED\necho.', self.launcher)
        self.assertTrue(self.launcher.rstrip().endswith('exit /b 1'))


if __name__ == '__main__':
    unittest.main()
