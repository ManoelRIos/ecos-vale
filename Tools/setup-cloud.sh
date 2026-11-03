#!/usr/bin/env bash
set -euo pipefail
cd /workspace/ecos-vale
mkdir -p /workspace/.tools
sdk_dir=/workspace/.tools/dotnet
if [[ ! -x "$sdk_dir/dotnet" ]]; then
  installer=$(mktemp /tmp/ecos-dotnet-install.XXXXXX)
  trap 'rm -f "$installer"' EXIT
  curl --fail --silent --show-error --location https://dot.net/v1/dotnet-install.sh -o "$installer"
  bash "$installer" --version 8.0.427 --install-dir "$sdk_dir" --no-path
fi
python3 -m pip install --disable-pip-version-check --target /workspace/.tools/python --upgrade -r Tools/requirements-validation.txt
python3 -m unittest discover -s Tools -p "test_*.py"
PYTHONPATH="/workspace/.tools/python${PYTHONPATH:+:$PYTHONPATH}" python3 Tools/validate_project.py
DOTNET_CLI_HOME=/workspace/.tools/dotnet-home DOTNET_CLI_TELEMETRY_OPTOUT=1 "$sdk_dir/dotnet" run --project Tools/HauntChecks.csproj
printf '%s\n' 'Core C# and static validation complete. Unity import, Play Mode and Windows build remain separate required checks.'
