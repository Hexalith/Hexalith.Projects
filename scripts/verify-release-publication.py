#!/usr/bin/env python3
"""Verify the exact GitHub and NuGet outputs of a manual Projects release."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import subprocess
import sys
import urllib.request
import zipfile
from datetime import datetime
from pathlib import Path

from _nuget import (
    MAX_TOTAL_SIZE,
    SOURCE_SHA_PATTERN,
    VERSION_PATTERN,
    load_manifest,
    read_committed_module_manifest,
    reject_duplicate_json_keys,
    validate_packages,
)

REPOSITORY = "Hexalith/Hexalith.Projects"
NUGET_PACKAGES = "https://api.nuget.org/v3-flatcontainer"
SIGNATURE = ".signature.p7s"


def github(endpoint: str) -> dict:
    output = subprocess.check_output(["gh", "api", f"repos/{REPOSITORY}/{endpoint}"], timeout=60)
    result = json.loads(output, object_pairs_hook=reject_duplicate_json_keys)
    if not isinstance(result, dict):
        raise ValueError(f"Invalid GitHub response at {endpoint}")
    return result


def verify_github(run_id: int, tag: str, source: str, expected_assets: set[str]) -> dict:
    run = github(f"actions/runs/{run_id}")
    identity = {
        "id": run_id,
        "head_sha": source,
        "head_branch": "main",
        "path": ".github/workflows/release.yml",
        "event": "workflow_dispatch",
        "status": "completed",
        "conclusion": "success",
    }
    if any(run.get(key) != value for key, value in identity.items()):
        raise ValueError("Workflow run does not identify a successful release of the expected source")

    attempt = run.get("run_attempt")
    if type(attempt) is not int or attempt < 1:
        raise ValueError("Release workflow run has no valid attempt")
    jobs = github(f"actions/runs/{run_id}/attempts/{attempt}/jobs")
    publish_steps = [
        step for job in jobs["jobs"]
        if job.get("name") == "Release packages" and job.get("conclusion") == "success"
        for step in job["steps"]
        if step.get("name") == "Semantic Release" and step.get("conclusion") == "success"
    ]
    if len(publish_steps) != 1:
        raise ValueError("Release attempt did not successfully execute the full publication step")
    step = publish_steps[0]

    ref = github(f"git/ref/tags/{tag}")
    tag_object = ref["object"]
    for _ in range(5):
        if tag_object["type"] != "tag":
            break
        tag_sha = tag_object["sha"]
        if not SOURCE_SHA_PATTERN.fullmatch(tag_sha):
            raise ValueError(f"Invalid annotated tag object for {tag}")
        tag_object = github(f"git/tags/{tag_sha}")["object"]
    if (ref.get("ref"), tag_object.get("type"), tag_object.get("sha")) != (
        f"refs/tags/{tag}", "commit", source,
    ):
        raise ValueError(f"GitHub tag {tag} does not resolve to {source}")
    release = github(f"releases/tags/{tag}")
    if release.get("tag_name") != tag or release.get("draft") is not False:
        raise ValueError(f"GitHub release {tag} is absent or still a draft")
    names = [asset["name"] for asset in release["assets"]]
    if len(names) != len(expected_assets) or set(names) != expected_assets:
        raise ValueError(f"GitHub release {tag} has an unexpected asset inventory")
    for asset in release["assets"]:
        if (asset.get("state") != "uploaded"
            or type(asset.get("size")) is not int or not 0 < asset["size"] <= MAX_TOTAL_SIZE
            or not re.fullmatch(r"sha256:[0-9a-f]{64}", asset.get("digest") or "")):
            raise ValueError(f"GitHub release asset metadata is incomplete: {asset['name']}")
    started, published, completed = (
        datetime.fromisoformat(timestamp)
        for timestamp in (step["started_at"], release["published_at"], step["completed_at"])
    )
    if any(timestamp.utcoffset() is None for timestamp in (started, published, completed)) or not started <= published <= completed:
        raise ValueError("GitHub release was published outside the selected workflow attempt")
    return {"workflowRun": run, "publicationStep": step, "tag": ref, "release": release}


def archive_payload(path: Path) -> dict[str, str]:
    with zipfile.ZipFile(path) as archive:
        return {
            entry.filename: hashlib.sha256(archive.read(entry)).hexdigest()
            for entry in archive.infolist()
            if entry.filename != SIGNATURE
        }


def download_package(url: str, output: Path) -> None:
    with urllib.request.urlopen(url, timeout=30) as response:
        payload = response.read(MAX_TOTAL_SIZE + 1)
    if not payload or len(payload) > MAX_TOTAL_SIZE:
        raise ValueError(f"Invalid package download length at {url}")
    output.write_bytes(payload)


def verify_publication(version: str, source: str, run_id: int, evidence: Path) -> None:
    if not VERSION_PATTERN.fullmatch(version) or not SOURCE_SHA_PATTERN.fullmatch(source) or run_id <= 0:
        raise ValueError("Expected a normalized release version, exact source SHA, and positive run ID")
    tag = f"v{version}"
    package_ids = [package.package_id for package in load_manifest().packages]
    names = {f"{package_id}.{version}.nupkg" for package_id in package_ids}
    receipt = verify_github(run_id, tag, source, names)
    module_manifest = read_committed_module_manifest(source)

    github_packages = evidence / "github"
    nuget_packages = evidence / "nuget"
    evidence.mkdir(parents=True, exist_ok=False)
    github_packages.mkdir()
    nuget_packages.mkdir()
    subprocess.run(
        ["gh", "release", "download", tag, "--repo", REPOSITORY,
         "--pattern", "*.nupkg", "--dir", str(github_packages)],
        check=True, timeout=120,
    )
    if {path.name for path in github_packages.iterdir()} != names:
        raise ValueError("Downloaded GitHub packages differ from the release inventory")
    github_hashes = {}
    for asset in receipt["release"]["assets"]:
        name = asset["name"]
        data = (github_packages / name).read_bytes()
        digest = hashlib.sha256(data).hexdigest()
        if len(data) != asset["size"] or f"sha256:{digest}" != asset["digest"]:
            raise ValueError(f"Downloaded GitHub asset differs from the published asset metadata: {name}")
        github_hashes[name] = digest
    validate_packages(github_packages, version, source, module_manifest=module_manifest)

    for package_id in package_ids:
        package_name = f"{package_id}.{version}.nupkg"
        url = f"{NUGET_PACKAGES}/{package_id.lower()}/{version.lower()}/{package_name.lower()}"
        download_package(url, nuget_packages / package_name)
    validate_packages(nuget_packages, version, source, module_manifest=module_manifest)

    verified = {}
    for name in sorted(names):
        github_package, nuget_package = github_packages / name, nuget_packages / name
        if archive_payload(github_package) != archive_payload(nuget_package):
            raise ValueError(f"GitHub and NuGet payloads differ for {name}")
        result = subprocess.run(
            ["dotnet", "nuget", "verify", "--all", str(nuget_package), "--verbosity", "normal"],
            check=True, capture_output=True, text=True, timeout=120,
            env={**os.environ, "DOTNET_CLI_UI_LANGUAGE": "en-US"},
        )
        if ("Signature type: Repository" not in result.stdout
            or "https://api.nuget.org/v3/index.json" not in result.stdout):
            raise ValueError(f"Expected a verified NuGet.org repository signature for {name}")
        verified[name] = {
            "githubSha256": github_hashes[name],
            "nugetSha256": hashlib.sha256(nuget_package.read_bytes()).hexdigest(),
            "signatureVerification": result.stdout,
        }
    receipt["packages"] = verified
    (evidence / "publication-verified.json").write_text(
        json.dumps(receipt, indent=2, sort_keys=True) + "\n", encoding="utf-8",
    )


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--version", required=True)
    parser.add_argument("--source", required=True)
    parser.add_argument("--run-id", required=True, type=int)
    parser.add_argument("--evidence", required=True, type=Path, help="A new directory for the receipt and packages")
    args = parser.parse_args()
    try:
        verify_publication(args.version, args.source, args.run_id, args.evidence)
    except (KeyError, TypeError, ValueError, OSError, subprocess.SubprocessError, zipfile.BadZipFile) as exc:
        print(f"Release verification failed: {exc}", file=sys.stderr)
        return 1
    print(f"Verified exact GitHub and NuGet release {args.version}: {args.evidence}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
