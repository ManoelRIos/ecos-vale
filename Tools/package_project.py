#!/usr/bin/env python3
"""Build a deterministic Unity source ZIP with the short root EcosDaMeiaNoite/.

Files come from the Git index, but their current working contents are packaged.
No Unity import, compilation or Play Mode validation is performed by this tool.
"""
import argparse
import json
import os
import re
import stat
import subprocess
import tempfile
import zipfile
from pathlib import Path, PurePosixPath

ARCHIVE_ROOT = 'EcosDaMeiaNoite'
EDITOR_VERSION = '6000.0.85f1'
FIXED_TIMESTAMP = (1980, 1, 1, 0, 0, 0)
EXCLUDED_DIRECTORIES = {
    '.git', '.vs', '__pycache__', 'library', 'temp', 'obj', 'build', 'builds',
    'logs', 'usersettings', 'node_modules',
}
REQUIRED_FILES = {
    'Packages/manifest.json', 'ProjectSettings/ProjectVersion.txt',
    'ProjectSettings/EditorBuildSettings.asset',
    'ABRIR_ECOS_DA_MEIA_NOITE.cmd',
    'Assets/_Game/Scenes/Haunt.unity', 'Assets/_Game/Scenes/Haunt.unity.meta',
    'Assets/_Game/Haunt/Runtime/HauntGame.cs', 'Assets/_Game/Haunt/Runtime/HauntGame.cs.meta',
    'Assets/_Game/Haunt/Runtime/HauntGame.UI.cs', 'Assets/_Game/Haunt/Runtime/HauntGame.UI.cs.meta',
    'Assets/_Game/Haunt/Runtime/HauntGame.Visuals.cs', 'Assets/_Game/Haunt/Runtime/HauntGame.Visuals.cs.meta',
    'Assets/_Game/Haunt/Runtime/HauntRendering.cs', 'Assets/_Game/Haunt/Runtime/HauntRendering.cs.meta',
    'Assets/_Game/Haunt/Runtime/HauntPixelText.cs', 'Assets/_Game/Haunt/Runtime/HauntPixelText.cs.meta',
    'Assets/_Game/Haunt/Runtime/HauntGame.Weapons.cs', 'Assets/_Game/Haunt/Runtime/HauntGame.Weapons.cs.meta',
    'Assets/_Game/Haunt/Runtime/HauntGame.Audio.cs', 'Assets/_Game/Haunt/Runtime/HauntGame.Audio.cs.meta',
    'Assets/_Game/Haunt/Runtime/HauntGame.Input.cs', 'Assets/_Game/Haunt/Runtime/HauntGame.Input.cs.meta',
    'Assets/_Game/Haunt/Runtime/HauntGame.Environment.cs', 'Assets/_Game/Haunt/Runtime/HauntGame.Environment.cs.meta',
    'Assets/_Game/Haunt/Runtime/HauntGame.Feel.cs', 'Assets/_Game/Haunt/Runtime/HauntGame.Feel.cs.meta',
    'Assets/_Game/Haunt/Runtime/HauntGame.Patterns.cs', 'Assets/_Game/Haunt/Runtime/HauntGame.Patterns.cs.meta',
    'Assets/_Game/Haunt/Runtime/HauntGame.Lighting.cs', 'Assets/_Game/Haunt/Runtime/HauntGame.Lighting.cs.meta',
    'Assets/_Game/Haunt/Runtime/HauntFlickerLight.cs', 'Assets/_Game/Haunt/Runtime/HauntFlickerLight.cs.meta',
    'Assets/_Game/Haunt/Art/HauntPixelArt.cs', 'Assets/_Game/Haunt/Art/HauntPixelArt.cs.meta',
    'Assets/_Game/Haunt/Core/DungeonFloor.cs', 'Assets/_Game/Haunt/Core/DungeonFloor.cs.meta',
    'Assets/_Game/Haunt/Core/RunState.cs', 'Assets/_Game/Haunt/Core/RunState.cs.meta',
    'Assets/_Game/Haunt/Core/CombatGeometry.cs', 'Assets/_Game/Haunt/Core/CombatGeometry.cs.meta',
    'Assets/_Game/Haunt/Core/BulletPatternGenerator.cs', 'Assets/_Game/Haunt/Core/BulletPatternGenerator.cs.meta',
    'Assets/_Game/Haunt/Core/DodgeRules.cs', 'Assets/_Game/Haunt/Core/DodgeRules.cs.meta',
    'Assets/_Game/Haunt/Editor/HauntProject.cs', 'Assets/_Game/Haunt/Editor/HauntProject.cs.meta',
    'Assets/_Game/Haunt/Editor/HauntSetup.cs', 'Assets/_Game/Haunt/Editor/HauntSetup.cs.meta',
    'Assets/_Game/Haunt/Haunt.Runtime.asmdef', 'Assets/_Game/Haunt/Haunt.Runtime.asmdef.meta',
    'Assets/_Game/Haunt/Editor/Haunt.Editor.asmdef', 'Assets/_Game/Haunt/Editor/Haunt.Editor.asmdef.meta',
    'Assets/_Game/Haunt/Art/HauntSprites.shader', 'Assets/_Game/Haunt/Art/HauntSprites.shader.meta',
    'Assets/_Game/Resources/HauntSprites.mat', 'Assets/_Game/Resources/HauntSprites.mat.meta',
}


class PackagingError(ValueError):
    """A source or destination would produce an unsafe/incomplete archive."""


def _below_root(path, root):
    try:
        path.relative_to(root)
    except ValueError as exc:
        raise PackagingError('Path escapes the project root.') from exc


def _reject_symlinks(path, root):
    _below_root(path, root)
    cursor = root
    for part in path.relative_to(root).parts:
        cursor = cursor / part
        if cursor.is_symlink():
            raise PackagingError('Symlinks are not allowed in packaging paths.')
    _below_root(path.resolve(strict=False), root)


def _excluded(name):
    parts = PurePosixPath(name).parts
    lowered = tuple(part.casefold() for part in parts)
    return (
        lowered[0] == 'distribution'
        or any(part in EXCLUDED_DIRECTORIES for part in lowered[:-1])
        or any(part.startswith('library_backup') for part in lowered[:-1])
        or (len(lowered) >= 3 and lowered[:2] == ('tools', 'bin'))
    )


def source_files(root, output):
    result = subprocess.run(
        ['git', '-C', str(root), 'ls-files', '--cached', '-z'],
        check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
    )
    names = [os.fsdecode(name) for name in result.stdout.split(b'\0') if name]
    if len(names) != len(set(names)):
        raise PackagingError('The Git index contains duplicate/unmerged paths.')
    files = []
    for name in sorted(names):
        relative = PurePosixPath(name)
        if relative.is_absolute() or '..' in relative.parts or '\\' in name:
            raise PackagingError('Invalid source path in the Git index.')
        if not relative.parts:
            raise PackagingError('Empty source path in the Git index.')
        if _excluded(name):
            continue
        path = root.joinpath(*relative.parts)
        _reject_symlinks(path, root)
        if path == output:
            continue
        if not path.is_file():
            raise PackagingError(f'Tracked source is missing or not a file: {name}')
        files.append((name, path))
    included = {name for name, _ in files}
    if not REQUIRED_FILES.issubset(included):
        raise PackagingError('Required Unity project files must be tracked in Git.')
    return files



def folder_entries(files, root):
    """Recover empty Unity folders from tracked folder metadata, as after a clone."""
    directories = set()
    for name, metadata in files:
        if not name.endswith('.meta'):
            continue
        text = metadata.read_text(encoding='utf-8-sig')
        if not re.search(r'^folderAsset:\s*yes\s*$', text, re.M):
            continue
        directory = name[:-len('.meta')]
        if not directory:
            raise PackagingError('Folder metadata has no directory name.')
        path = root.joinpath(*PurePosixPath(directory).parts)
        _reject_symlinks(path, root)
        if path.exists() and not path.is_dir():
            raise PackagingError(f'Folder metadata points to a non-directory: {directory}')
        directories.add(directory)
    return sorted(directories)


def _validate_project(root):
    try:
        dependencies = json.loads((root / 'Packages/manifest.json').read_text())['dependencies']
    except (OSError, ValueError, KeyError, TypeError) as exc:
        raise PackagingError('Packages/manifest.json is invalid.') from exc
    for package, version in {
        'com.unity.render-pipelines.universal': '17.0.4',
        'com.unity.inputsystem': '1.11.2',
        'com.unity.test-framework': '1.4.5',
    }.items():
        if dependencies.get(package) != version:
            raise PackagingError(f'The delivery must declare {package} {version}.')
    build = (root / 'ProjectSettings/EditorBuildSettings.asset').read_text()
    scenes = re.findall(r'^    path: (.+)$', build, re.M)
    if scenes != ['Assets/_Game/Scenes/Haunt.unity']:
        raise PackagingError('The delivery must contain only the Haunt build scene.')
    version = (root / 'ProjectSettings/ProjectVersion.txt').read_text()
    if not re.search(r'^m_EditorVersion: ' + re.escape(EDITOR_VERSION) + r'$', version, re.M):
        raise PackagingError(f'The delivery must pin Unity {EDITOR_VERSION}.')


def _entry(name, directory=False):
    entry = zipfile.ZipInfo(name, FIXED_TIMESTAMP)
    entry.create_system = 3
    entry.compress_type = zipfile.ZIP_DEFLATED
    mode = (stat.S_IFDIR | 0o755) if directory else (stat.S_IFREG | 0o644)
    entry.external_attr = mode << 16
    if directory:
        entry.external_attr |= 0x10
    return entry


def create_archive(project_root, destination=None):
    requested_root = Path(project_root).absolute()
    if requested_root.is_symlink():
        raise PackagingError('The project root must not be a symlink.')
    root = requested_root.resolve(strict=True)
    requested_output = Path(destination) if destination is not None else Path('Distribution/EcosDaMeiaNoite.zip')
    output = requested_output if requested_output.is_absolute() else root / requested_output
    _reject_symlinks(output, root)
    output = output.resolve(strict=False)
    files = source_files(root, output)
    directories = folder_entries(files, root)
    _validate_project(root)
    output.parent.mkdir(parents=True, exist_ok=True)
    temporary = None
    try:
        with tempfile.NamedTemporaryFile(dir=output.parent, suffix='.zip.tmp', delete=False) as handle:
            temporary = Path(handle.name)
        with zipfile.ZipFile(temporary, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
            archive.writestr(_entry(ARCHIVE_ROOT + '/', directory=True), b'')
            for directory in directories:
                archive.writestr(_entry(ARCHIVE_ROOT + '/' + directory + '/', directory=True), b'')
            for name, path in files:
                archive.writestr(_entry(ARCHIVE_ROOT + '/' + name), path.read_bytes(), compresslevel=9)
        with zipfile.ZipFile(temporary) as archive:
            if archive.testzip() is not None:
                raise PackagingError('Archive integrity check failed.')
        os.replace(temporary, output)
        temporary = None
    finally:
        if temporary is not None:
            temporary.unlink(missing_ok=True)
    return output


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project-root', type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument('--output', type=Path, default=Path('Distribution/EcosDaMeiaNoite.zip'))
    args = parser.parse_args()
    try:
        output = create_archive(args.project_root, args.output)
    except (PackagingError, OSError, subprocess.CalledProcessError) as exc:
        parser.exit(1, f'Packaging failed: {exc}\n')
    print(f'Source ZIP created: {output} ({output.stat().st_size} bytes; root {ARCHIVE_ROOT}/)')
    print('Unity compilation and Play Mode are not validated by packaging.')


if __name__ == '__main__':
    main()
