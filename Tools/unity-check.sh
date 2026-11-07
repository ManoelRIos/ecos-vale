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
  import) "$UNITY_EDITOR_PATH" -batchmode -nographics -projectPath "$PWD" -executeMethod Ecos.Haunt.Editor.HauntSetup.CreateDefaults -quit -logFile /tmp/haunt-import.log ;;
  smoke) "$UNITY_EDITOR_PATH" -batchmode -nographics -projectPath "$PWD" -executeMethod Ecos.Haunt.Editor.HauntProject.SmokeCheck -quit -logFile /tmp/haunt-smoke.log ;;
  fps-smoke) "$UNITY_EDITOR_PATH" -batchmode -nographics -projectPath "$PWD" -executeMethod Ecos.Haunt.Editor.HauntFpsProject.SmokeCheck -quit -logFile /tmp/haunt-fps-smoke.log ;;
  lowpoly-smoke) "$UNITY_EDITOR_PATH" -batchmode -nographics -projectPath "$PWD" -executeMethod Ecos.Haunt.Editor.HauntLowPolyProject.SmokeCheck -quit -logFile /tmp/haunt-lowpoly-smoke.log ;;
  tests) "$UNITY_EDITOR_PATH" -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform EditMode -testResults /tmp/haunt-tests.xml -logFile /tmp/haunt-tests.log ;;
  build) "$UNITY_EDITOR_PATH" -batchmode -nographics -projectPath "$PWD" -executeMethod Ecos.Haunt.Editor.HauntSetup.BuildWindows -quit -logFile /tmp/haunt-build.log ;;
  *) printf '%s\n' 'Usage: Tools/unity-check.sh [import|smoke|fps-smoke|lowpoly-smoke|tests|build]' >&2; exit 2 ;;
esac
