#!/usr/bin/env python3
"""Check the pinned/installed Editor against the supported LTS line and known CVE.
This checks version policy; it does not launch Unity or certify general security.
"""
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SECURITY_FIXED_FROM = (6000, 0, 58, 2)  # CVE-2025-59489, Unity 6.0 LTS.


def parse_version(value):
    match = re.fullmatch(r'(\d+)\.(\d+)\.(\d+)f(\d+)', value.strip())
    return tuple(map(int, match.groups())) if match else None


def project_version():
    text = (ROOT / 'ProjectSettings/ProjectVersion.txt').read_text()
    match = re.search(r'^m_EditorVersion: (\S+)$', text, re.M)
    if not match:
        raise ValueError('ProjectVersion.txt não contém m_EditorVersion.')
    return match[1]


def version_error(value, minimum):
    actual, required = parse_version(value), parse_version(minimum)
    if actual is None or required is None:
        return 'Versão inválida: use uma versão estável no formato 6000.0.NNfN.'
    if actual[:2] != (6000, 0):
        return 'Este projeto é preparado para Unity 6.0 LTS; não use Unity 2022/2023 ou outra linha.'
    if actual < SECURITY_FIXED_FROM:
        return 'Editor afetado pela CVE-2025-59489: na linha 6.0, a correção começa em 6000.0.58f2.'
    if actual < required:
        return f'Editor mais antigo que o patch fixado no projeto ({minimum}).'
    return None


def main():
    minimum = project_version()
    value = sys.argv[1].strip() if len(sys.argv) > 1 else minimum
    error = version_error(value, minimum)
    if error:
        print(error, file=sys.stderr)
        return 1
    print(f'Editor version policy: PASS ({value}; project {minimum})')
    return 0


if __name__ == '__main__':
    sys.exit(main())
