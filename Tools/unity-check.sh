#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
: "${UNITY_EDITOR_PATH:?Set UNITY_EDITOR_PATH to a licensed Unity 6000.0.85f1 executable}"
if [[ ! -x "$UNITY_EDITOR_PATH" ]]; then
  printf '%s\n' 'UNITY_EDITOR_PATH is not executable.' >&2
  exit 1
fi
editor_version=$("$UNITY_EDITOR_PATH" -version)
python3 Tools/check_editor_version.py "$editor_version"
mode=${1:-import}
case "$mode" in
  import) "$UNITY_EDITOR_PATH" -batchmode -nographics -projectPath "$PWD" -executeMethod Ecos.Editor.ProjectSetup.CreateDefaults -quit -logFile /tmp/ecos-import.log ;;
  tests) "$UNITY_EDITOR_PATH" -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform EditMode -testResults /tmp/ecos-tests.xml -logFile /tmp/ecos-tests.log ;;
  build) "$UNITY_EDITOR_PATH" -batchmode -nographics -projectPath "$PWD" -executeMethod Ecos.Editor.ProjectSetup.BuildWindows -quit -logFile /tmp/ecos-build.log ;;
  *) printf '%s\n' 'Usage: Tools/unity-check.sh [import|tests|build]' >&2; exit 2 ;;
esac
