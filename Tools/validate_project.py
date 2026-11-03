#!/usr/bin/env python3
"""Validate the roguelike's source layout, references and pinned dependencies.

This does not import Unity packages, compile Unity components or validate rendering.
"""
import json
import re
import sys
from pathlib import Path
from check_editor_version import project_version, version_error

ROOT = Path(__file__).resolve().parents[1]
errors = []


def check(condition, message):
    if not condition:
        errors.append(message)


pinned_editor = project_version()
check(version_error(pinned_editor, pinned_editor) is None,
      'Unsafe or unsupported pinned Editor: ' + pinned_editor)
assets = ROOT / 'Assets'
files = [p for p in assets.rglob('*') if p.is_file() and p.suffix != '.meta']
guids = {}
for path in assets.rglob('*'):
    if path.suffix == '.meta':
        if 'folderAsset: yes' not in path.read_text():
            check(Path(str(path)[:-5]).exists(), f'Orphaned metadata: {path.relative_to(ROOT)}')
        continue
    meta = Path(str(path) + '.meta')
    check(meta.exists(), f'Missing meta: {path.relative_to(ROOT)}')
    if meta.exists():
        match = re.search(r'^guid: ([a-f0-9]{32})$', meta.read_text(), re.M)
        check(match is not None, f'Invalid meta: {meta}')
        if match:
            guid = match[1]
            check(guid not in guids, f'Duplicate GUID: {guid}')
            guids[guid] = path

# Resolve references against installed package metadata when the Editor has generated
# its render pipeline assets. Package caches remain outside the project asset counts.
external = {'0000000000000000e000000000000000'}
for package_root in (ROOT / 'Packages', ROOT / 'Library/PackageCache'):
    for meta in package_root.rglob('*.meta'):
        match = re.search(r'^guid: ([a-f0-9]{32})$', meta.read_text(errors='replace'), re.M)
        if match:
            external.add(match[1])
for path in files:
    if path.suffix in {'.unity', '.prefab', '.asset', '.mat'}:
        for guid in re.findall(r'guid: ([a-f0-9]{32})', path.read_text()):
            check(guid in guids or guid in external, f'Unknown GUID {guid} in {path}')
    if path.suffix in {'.unity', '.prefab'}:
        contents = path.read_text()
        anchors = re.findall(r'^--- !u!\d+ &(\d+)$', contents, re.M)
        check(len(anchors) == len(set(anchors)), f'Duplicate object IDs in {path}')
        for file_id in re.findall(r'\{fileID: (\d+)\}', contents):
            check(file_id == '0' or file_id in anchors, f'Dangling local object {file_id} in {path}')

manifest = json.loads((ROOT / 'Packages/manifest.json').read_text())['dependencies']
required_packages = {
    'com.unity.render-pipelines.universal': '17.0.4',
    'com.unity.inputsystem': '1.11.2',
    'com.unity.test-framework': '1.4.5',
    'com.unity.modules.audio': '1.0.0',
    'com.unity.modules.imgui': '1.0.0',
    'com.unity.modules.jsonserialize': '1.0.0',
}
check(manifest == required_packages, 'Package manifest must contain only the pinned roguelike requirements')
for package, version in manifest.items():
    check(re.fullmatch(r'\d+\.\d+\.\d+', version) is not None, f'Unpinned dependency: {package}')
for path in (ROOT / 'Documentation/PackageManifests').glob('*.json'):
    evidence = json.loads(path.read_text())
    check(manifest.get(evidence['name']) == evidence['version'], f'Manifest mismatch: {path}')

assemblies = {}
for path in assets.rglob('*.asmdef'):
    definition = json.loads(path.read_text())
    name = definition['name']
    check(name not in assemblies, f'Duplicate assembly: {name}')
    assemblies[name] = definition
check(set(assemblies) == {'Haunt.Runtime', 'Haunt.Editor', 'Haunt.Tests'},
      'Only the roguelike runtime, Editor and test assemblies should remain')
package_assemblies = {
    'Unity.InputSystem', 'Unity.RenderPipelines.Universal.Runtime',
    'Unity.RenderPipelines.Core.Runtime', 'UnityEngine.TestRunner', 'UnityEditor.TestRunner',
}
for name, definition in assemblies.items():
    for reference in definition.get('references', []):
        check(reference in assemblies or reference in package_assemblies,
              f'Unknown assembly reference {reference} in {name}')

scene = assets / '_Game/Scenes/Haunt.unity'
check(scene.exists(), 'Missing roguelike entry scene')
check(set((assets / '_Game/Scenes').glob('*.unity')) == {scene}, 'Only the roguelike scene should remain')
build = (ROOT / 'ProjectSettings/EditorBuildSettings.asset').read_text()
check(re.findall(r'^    path: (.+)$', build, re.M) == ['Assets/_Game/Scenes/Haunt.unity'],
      'The build must contain only the roguelike entry scene')
check(re.findall(r'^  - enabled: (\d+)$', build, re.M) == ['1'], 'The roguelike build scene must be enabled')
check('activeInputHandler: 1' in (ROOT / 'ProjectSettings/ProjectSettings.asset').read_text(),
      'Input System disabled')
for relative in ('Haunt/Runtime/HauntGame.cs', 'Haunt/Runtime/HauntRendering.cs',
                 'Haunt/Editor/HauntSetup.cs', 'Resources/HauntSprites.mat'):
    check((assets / '_Game' / relative).exists(), f'Missing roguelike resource: {relative}')
for name in ('Art', 'Audio', 'Biomes', 'Core', 'Editor', 'Environment', 'Gameplay', 'Inventory',
             'Multiplayer', 'Player', 'Prefabs', 'Quests', 'ScriptableObjects', 'Tests', 'UI', 'World'):
    check(not (assets / '_Game' / name).exists(), f'Unexpected obsolete asset folder: {name}')

cs_files = list(assets.rglob('*.cs'))
try:
    import tree_sitter_c_sharp
    from tree_sitter import Language, Parser
    parser = Parser(Language(tree_sitter_c_sharp.language()))
    for path in cs_files:
        check(not parser.parse(path.read_bytes()).root_node.has_error, f'C# parse error: {path}')
    print(f'C# syntax parsed: {len(cs_files)} files')
except ImportError:
    print('C# syntax parser NOT RUN (install Tools/requirements-validation.txt)')
print(f'Assets checked: {len(files)}; GUIDs checked: {len(guids)}')
if errors:
    print('\n'.join(errors), file=sys.stderr)
    sys.exit(1)
print('Roguelike structure, references and dependency pins: PASS')
