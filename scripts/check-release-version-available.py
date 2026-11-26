#!/usr/bin/env python3
"""Require the planned version to be absent for all Projects release packages."""

from __future__ import annotations

import argparse
import json
import sys
import urllib.error
import urllib.request

from _nuget import VERSION_PATTERN, load_manifest, reject_duplicate_json_keys

PACKAGE_BASE_ADDRESS = "https://api.nuget.org/v3-flatcontainer"
MAX_VERSION_INDEX_SIZE = 1024 * 1024


def published_versions(package_id: str) -> set[str]:
    url = f"{PACKAGE_BASE_ADDRESS}/{package_id.lower()}/index.json"
    request = urllib.request.Request(url, headers={"Accept": "application/json", "Cache-Control": "no-cache"})
    try:
        with urllib.request.urlopen(request, timeout=20) as response:
            if response.status != 200 or response.geturl() != url:
                raise ValueError(f"Unexpected package index response for {package_id}")
            contents = response.read(MAX_VERSION_INDEX_SIZE + 1)
    except urllib.error.HTTPError as exc:
        with exc:
            if exc.code == 404 and exc.geturl() == url:
                return set()
            raise
    if len(contents) > MAX_VERSION_INDEX_SIZE:
        raise ValueError(f"Package index is too large for {package_id}")
    document = json.loads(contents, object_pairs_hook=reject_duplicate_json_keys)
    versions = document.get("versions") if isinstance(document, dict) else None
    if not isinstance(versions, list) or not versions:
        raise ValueError(f"Invalid package version index for {package_id}")
    if any(not isinstance(version, str) or not VERSION_PATTERN.fullmatch(version) for version in versions):
        raise ValueError(f"Invalid package version entry for {package_id}")
    return {version.lower() for version in versions}


def require_available(version: str) -> int:
    if not VERSION_PATTERN.fullmatch(version):
        raise ValueError("Expected a normalized NuGet version")
    packages = load_manifest().packages
    occupied = [
        package.package_id for package in packages
        if version.lower() in published_versions(package.package_id)
    ]
    if occupied:
        raise ValueError(f"Version {version} is already occupied for: {', '.join(occupied)}")
    return len(packages)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("version")
    args = parser.parse_args()
    try:
        count = require_available(args.version)
    except (ValueError, OSError) as exc:
        print(f"Refusing release: {exc}", file=sys.stderr)
        return 1
    print(f"Version {args.version} is absent for all {count} release packages.", file=sys.stderr)
    return 0


if __name__ == "__main__":
    sys.exit(main())
