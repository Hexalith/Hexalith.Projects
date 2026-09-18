#!/usr/bin/env python3
"""Validate Projects package candidates against the authoritative manifest."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

from _nuget import normalize_internal_dependency_ranges, validate_packages


def parse_arguments() -> argparse.Namespace:
    """Parse package validation inputs."""

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("package_directory", type=Path)
    parser.add_argument("--expected-version")
    parser.add_argument("--expected-source-sha")
    parser.add_argument(
        "--normalize-internal-ranges",
        action="store_true",
        help="Normalize trusted dotnet-pack output before strict validation.",
    )
    return parser.parse_args()


def main() -> int:
    """Report every independently verified candidate."""

    arguments = parse_arguments()
    if arguments.normalize_internal_ranges:
        if arguments.expected_version is None:
            raise ValueError("--normalize-internal-ranges requires --expected-version")
        normalize_internal_dependency_ranges(arguments.package_directory, arguments.expected_version)
    packages, version, source_sha = validate_packages(
        arguments.package_directory,
        arguments.expected_version,
        arguments.expected_source_sha,
    )
    for package in packages:
        print(f"Verified {package.package_id} {package.version} from {package.source_sha}")
    print(f"Validated {len(packages)} packages at version {version} from {source_sha}.")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, ValueError) as error:
        print(f"Package validation failed: {error}", file=sys.stderr)
        sys.exit(1)
