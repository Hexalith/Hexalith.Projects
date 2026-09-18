#!/usr/bin/env python3
"""Create the exact five Projects release candidates from package-only inputs."""

from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

from _nuget import (
    ROOT,
    VERSION_PATTERN,
    current_source_sha,
    load_manifest,
    normalize_internal_dependency_ranges,
    validate_manifest_projects,
    validate_packages,
)


def parse_arguments() -> argparse.Namespace:
    """Parse the stable command-line contract used by the shared workflow."""

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("output_directory", type=Path)
    parser.add_argument("version")
    return parser.parse_args()


def prepare_output_directory(path: Path) -> Path:
    """Prepare only the explicitly selected candidate directory."""

    output = path.resolve()
    if output == ROOT or output == Path(output.anchor):
        raise ValueError(f"Refusing unsafe package output directory: {output}")
    output.mkdir(parents=True, exist_ok=True)
    for pattern in ("*.nupkg", "*.snupkg"):
        for candidate in output.glob(pattern):
            if not candidate.is_file():
                raise ValueError(f"Refusing non-file package candidate: {candidate}")
            candidate.unlink()
    return output


def main() -> int:
    """Pack, normalize, and validate the authoritative manifest inventory."""

    arguments = parse_arguments()
    if not VERSION_PATTERN.fullmatch(arguments.version):
        raise ValueError(f"Invalid package version: {arguments.version!r}")

    manifest = load_manifest()
    validate_manifest_projects(manifest)
    output = prepare_output_directory(arguments.output_directory)
    source_sha = current_source_sha()

    for package in manifest.packages:
        command = [
            "dotnet",
            "pack",
            str(package.project_path),
            "--no-build",
            "--configuration",
            "Release",
            "--output",
            str(output),
            f"-p:HexalithProjectsPackageVersion={arguments.version}",
            "-p:UseHexalithProjectReferences=false",
            f"-p:RepositoryCommit={source_sha}",
            "/m:1",
            "/nr:false",
        ]
        print(f"Packing {package.package_id} from {package.project}", flush=True)
        subprocess.run(command, cwd=ROOT, check=True)

    normalize_internal_dependency_ranges(output, arguments.version)
    validate_packages(output, arguments.version, source_sha)
    print(
        f"Packed and validated {len(manifest.packages)} release candidates "
        f"at version {arguments.version} from {source_sha}."
    )
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, subprocess.CalledProcessError, ValueError) as error:
        print(f"Package creation failed: {error}", file=sys.stderr)
        sys.exit(1)
