#!/usr/bin/env python3
"""Pack the exact Conversations release inventory in NuGet package mode."""

from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

from release_contract import ROOT, load_manifest, validate_semver


def prepare_output_directory(output_directory: Path) -> Path:
    """Resolve a repository-local package directory before removing stale archives."""
    resolved = output_directory.resolve()
    if resolved == ROOT or ROOT not in resolved.parents:
        raise ValueError(f"package output must be a child of the repository root: {output_directory}")
    resolved.mkdir(parents=True, exist_ok=True)
    return resolved


def main() -> int:
    parser = argparse.ArgumentParser(description="Pack Hexalith.Conversations release packages.")
    parser.add_argument("output_directory", type=Path)
    parser.add_argument("version")
    args = parser.parse_args()

    packages = load_manifest()
    version = validate_semver(args.version)
    output_directory = prepare_output_directory(args.output_directory)
    for pattern in ("*.nupkg", "*.snupkg"):
        for package_path in output_directory.glob(pattern):
            package_path.unlink()

    for package in packages:
        subprocess.run(
            [
                "dotnet",
                "pack",
                package.project,
                "--no-restore",
                "--configuration",
                "Release",
                "--output",
                str(output_directory),
                f"-p:Version={version}",
                "/m:1",
                "/nr:false",
            ],
            check=True,
        )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, ValueError) as exc:
        print(f"Package packing failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
    except subprocess.CalledProcessError as exc:
        print(f"Package packing failed with exit code {exc.returncode}.", file=sys.stderr)
        raise SystemExit(exc.returncode)
