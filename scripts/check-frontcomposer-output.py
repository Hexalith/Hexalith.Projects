#!/usr/bin/env python3
"""Inspect generator output retained by the shared Release/package build."""
from pathlib import Path
import re
import subprocess
import sys

from _nuget import ROOT


def main() -> int:
    contracts = ROOT / 'src/Hexalith.Projects.UI.Contracts'
    if not any(re.search(r'\[\s*(Projection|Command)\b', path.read_text()) for path in contracts.rglob('*.cs')):
        raise ValueError('No annotated FrontComposer contracts exist')
    generated = contracts / 'obj/Release'
    if not any('generated/HexalithFrontComposer/' in path.as_posix() for path in generated.rglob('*.g.cs')):
        raise ValueError('The shared Release build must retain FrontComposer generator output')
    subprocess.run(['dotnet', 'tool', 'run', 'frontcomposer', '--', 'inspect', '--project',
                    str(contracts / 'Hexalith.Projects.UI.Contracts.csproj'), '--configuration', 'Release',
                    '--fail-on-warning'], check=True)
    print('FrontComposer retained Release output inspection passed.')
    return 0


if __name__ == '__main__':
    try:
        sys.exit(main())
    except (OSError, subprocess.CalledProcessError, ValueError) as error:
        print(f'FrontComposer inspection failed: {error}', file=sys.stderr)
        sys.exit(1)
