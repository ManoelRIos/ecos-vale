import unittest
from check_editor_version import parse_version, version_error

class EditorVersionTests(unittest.TestCase):
    def test_original_vulnerable_editor_is_rejected(self):
        self.assertIn('CVE-2025-59489', version_error('6000.0.40f1', '6000.0.85f1'))

    def test_first_patch_revision_is_still_vulnerable(self):
        self.assertIn('CVE-2025-59489', version_error('6000.0.58f1', '6000.0.58f2'))

    def test_security_fix_boundary_is_accepted(self):
        self.assertIsNone(version_error('6000.0.58f2', '6000.0.58f2'))

    def test_safe_but_older_than_project_is_rejected_separately(self):
        self.assertIn('mais antigo', version_error('6000.0.70f1', '6000.0.85f1'))

    def test_target_and_newer_patch_are_accepted(self):
        self.assertIsNone(version_error('6000.0.85f1', '6000.0.85f1'))
        self.assertIsNone(version_error('6000.0.86f1', '6000.0.85f1'))

    def test_other_editor_lines_are_rejected(self):
        for value in ['2022.3.67f2', '6000.2.6f2', '6000.3.0f1']:
            with self.subTest(version=value):
                self.assertIn('6.0 LTS', version_error(value, '6000.0.85f1'))

    def test_prerelease_and_malformed_versions_are_rejected(self):
        for value in ['', 'garbage', '6000.0.85', '6000.0.85b1']:
            self.assertIsNotNone(version_error(value, '6000.0.85f1'))

    def test_parser_compares_patch_numbers_numerically(self):
        self.assertGreater(parse_version('6000.0.100f1'), parse_version('6000.0.85f1'))

if __name__ == '__main__':
    unittest.main()
