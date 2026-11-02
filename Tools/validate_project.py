#!/usr/bin/env python3
"""Check project structure, metadata, local GUID references and pinned manifests.
C# syntax parsing runs when tree-sitter + tree-sitter-c-sharp are installed.
This does NOT compile Unity code, import assets or validate shader rendering.
"""
import json
import re
import sys
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
from check_editor_version import project_version, version_error
errors = []
def check(condition, message):
    if not condition:
        errors.append(message)
pinned_editor = project_version()
check(version_error(pinned_editor, pinned_editor) is None, 'Unsafe or unsupported pinned Editor: ' + pinned_editor)
assets = ROOT / 'Assets'
files = [p for p in assets.rglob('*') if p.is_file() and p.suffix != '.meta']
guids = {}
for path in assets.rglob('*'):
    if path.suffix == '.meta':
        continue
    meta = Path(str(path) + '.meta')
    check(meta.exists(), f'Missing meta: {path.relative_to(ROOT)}')
    if meta.exists():
        match = re.search(r'^guid: ([a-f0-9]{32})$', meta.read_text(), re.M)
        check(match is not None, f'Invalid meta: {meta}')
        if match:
            g = match[1]
            check(g not in guids, f'Duplicate GUID: {g}')
            guids[g] = path
external = {'0000000000000000e000000000000000'}
# Official URP shader GUIDs inspected from the pinned package's upstream source.
external.update(json.loads((ROOT / 'Tools/external_guids.json').read_text()))
for path in files:
    if path.suffix in {'.unity', '.prefab', '.asset', '.mat'}:
        for g in re.findall(r'guid: ([a-f0-9]{32})', path.read_text()):
            check(g in guids or g in external, f'Unknown GUID {g} in {path}')
for path in files:
    if path.suffix in {'.unity', '.prefab'}:
        text = path.read_text()
        anchors = re.findall(r'^--- !u!\d+ &(\d+)$', text, re.M)
        check(len(anchors) == len(set(anchors)), f'Duplicate object IDs in {path}')
        for file_id in re.findall(r'\{fileID: (\d+)\}', text):
            check(file_id == '0' or file_id in anchors, f'Dangling local object {file_id} in {path}')
for path in assets.rglob('*.asmdef'):
    json.loads(path.read_text())
manifest = json.loads((ROOT / 'Packages/manifest.json').read_text())['dependencies']
# NGO 2.0.0's NetworkRigidbodyBase compiles when either physics module is
# enabled, but references types from both modules (confirmed by Editor CS1069).
if manifest.get('com.unity.netcode.gameobjects') == '2.0.0':
    for module in ('com.unity.modules.physics', 'com.unity.modules.physics2d'):
        check(manifest.get(module) == '1.0.0', f'Netcode 2.0.0 requires built-in module: {module}')
for package, version in manifest.items():
    check(re.fullmatch(r'\d+\.\d+\.\d+', version) is not None, f'Unpinned dependency: {package}')
for path in (ROOT / 'Documentation/PackageManifests').glob('*.json'):
    evidence = json.loads(path.read_text())
    check(manifest.get(evidence['name']) == evidence['version'], f'Manifest mismatch: {path}')
scene = assets / '_Game/Scenes/Valley.unity'
check(scene.exists(), 'No entry scene')
check('activeInputHandler: 1' in (ROOT / 'ProjectSettings/ProjectSettings.asset').read_text(), 'Input System disabled')
check(len(list((assets/'_Game/Biomes').glob('Biome?.asset'))) == 6, 'Six biome configs required')
check(len(list((assets/'_Game/Prefabs').glob('*.prefab'))) >= 2, 'Missing authored prefabs')
cs_files = list(assets.rglob('*.cs'))
try:
    import tree_sitter_c_sharp
    from tree_sitter import Language, Parser
    parser = Parser(Language(tree_sitter_c_sharp.language()))
    for path in cs_files:
        tree = parser.parse(path.read_bytes())
        check(not tree.root_node.has_error, f'C# parse error: {path}')
    print(f'C# syntax parsed: {len(cs_files)} files')
except ImportError:
    print('C# syntax parser NOT RUN (install Tools/requirements-validation.txt)')
print(f'Assets checked: {len(files)}; GUIDs checked: {len(guids)}')
if errors:
    print('\n'.join(errors), file=sys.stderr)
    sys.exit(1)
print('Project structure, local references and dependency pins: PASS')
