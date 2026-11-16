#!/usr/bin/env python3
"""Validate existing Release packages and every isolated real API consumer."""
from __future__ import annotations

import argparse
from pathlib import Path
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET

from _consumer_packages import main as check_consumers
from _nuget import ROOT, VERSION_PATTERN, normalize_internal_dependency_ranges, validate_packages
from _pack_release_packages import main as pack_packages


def validate_project_boundaries() -> None:
    contracts = ET.parse(ROOT / 'src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj')
    if any('Hexalith.FrontComposer.' in item.get(attribute, '')
           for item in contracts.iter() for attribute in ('Include', 'Update')):
        raise ValueError('Projects.Contracts must not reference Hexalith.FrontComposer.*')
    ui = ET.parse(ROOT / 'src/Hexalith.Projects.UI.Contracts/Hexalith.Projects.UI.Contracts.csproj')
    references = [(group, item) for group in ui.getroot().findall('ItemGroup')
                  for item in group.findall('PackageReference')
                  if item.get('Include') == 'Hexalith.FrontComposer.SourceTools']
    if len(references) != 1:
        raise ValueError('Exactly one private package-mode FrontComposer analyzer is required')
    group, item = references[0]
    if (group.get('Condition') != "'$(UseHexalithProjectReferences)' != 'true'"
            or item.get('PrivateAssets', item.findtext('PrivateAssets')) != 'all'
            or 'analyzers' not in [value.strip() for value in item.get('IncludeAssets', item.findtext('IncludeAssets', '')).split(';')]):
        raise ValueError('FrontComposer SourceTools must remain private, package-mode-only and analyzer-enabled')


def verify(directory: Path, version: str, skip_pack: bool) -> None:
    if skip_pack:
        if not directory.is_dir():
            raise ValueError('Prepared package directory is missing')
        normalize_internal_dependency_ranges(directory, version)
    else:
        pack_packages(argparse.Namespace(output_directory=directory, version=version))
    validate_packages(directory, version)
    check_consumers(argparse.Namespace(package_directory=directory))


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--version', default='91.92.93-ci.1')
    parser.add_argument('--package-directory', type=Path)
    parser.add_argument('--skip-pack', action='store_true')
    arguments = parser.parse_args()
    if not VERSION_PATTERN.fullmatch(arguments.version):
        raise ValueError('An exact candidate version is required')
    validate_project_boundaries()
    if arguments.package_directory is None:
        with tempfile.TemporaryDirectory(prefix='projects-package-') as private:
            verify(Path(private), arguments.version, arguments.skip_pack)
    else:
        verify(arguments.package_directory.resolve(), arguments.version, arguments.skip_pack)
    print('Package dependency gate passed: inventory, metadata, provenance, payload and five real API consumers.')
    return 0


if __name__ == '__main__':
    try:
        sys.exit(main())
    except (OSError, subprocess.CalledProcessError, ValueError) as error:
        print(f'Package dependency gate failed: {error}', file=sys.stderr)
        sys.exit(1)
