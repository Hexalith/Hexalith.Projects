#!/usr/bin/env python3
"""Bind retained official observations to exact local source and signed package bytes."""
import base64
import hashlib
import json
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET
import zipfile

receipt = Path(__file__).resolve().parent
root = receipt.parents[4]
builds = root / "references/Hexalith.Builds"
platform = root / "references/Hexalith.Platform"
expected = "2574bf2f3da5367d8801f0d7661d66155e5d90b0"
archive = Path("/tmp/story64-published-tuple-20261028/hexalith.builds.module.cli.4.38.2.nupkg")
load = lambda name: json.loads((receipt / name).read_bytes())
observations = []


def git(owner, *args):
    argv = ["git", *args]
    completed = subprocess.run(argv, cwd=owner, check=True, capture_output=True, text=True)
    observations.append({"argv": argv, "cwd": str(owner), "exit_code": completed.returncode, "stdout": completed.stdout})
    return completed.stdout.strip()


def source_catalog(revision):
    data = git(builds, "show", revision + ":Props/Directory.Packages.props")
    properties = {element.tag: element.text for element in ET.fromstring(data).iter() if element.tag.startswith("Hexalith")}
    return properties


release = load("builds-4382-release.stdout")
tag = load("builds-4382-tag.stdout")
inventory_bytes = (receipt / "g4-tool-package-inventory.json").read_bytes()
inventory = json.loads(inventory_bytes)
asset = next(asset for asset in release["assets"] if asset["name"] == "g4-tool-package-inventory.json")
assert "sha256:" + hashlib.sha256(inventory_bytes).hexdigest() == asset["digest"]
assert len(inventory_bytes) == asset["size"]
assert release["tag_name"] == "v4.38.2" and not release["draft"] and not release["prerelease"]
assert tag["object"] == {"sha": expected, "type": "commit", "url": "https://api.github.com/repos/Hexalith/Hexalith.Builds/git/commits/" + expected}
assert git(builds, "rev-parse", "HEAD") == expected
assert git(root, "rev-parse", "HEAD:references/Hexalith.Builds") == expected
assert git(root, "ls-files", "--stage", "references/Hexalith.Builds").split()[1] == expected
assert git(builds, "status", "--porcelain=v1", "--untracked-files=all") == ""
assert inventory["version"] == "4.38.2" and inventory["configuration"] == "Release"
for field in ("packageBuild", "sourceValidation", "controls"):
    assert inventory["qualification"][field]["result"] == "passed"
assert inventory["qualification"]["packageBuild"]["mode"] == "official"
assert inventory["qualification"]["sourceValidation"]["mode"] == "executed"
assert inventory["qualification"]["controls"]["mode"] == "executed"
assert inventory["qualification"]["sourceTree"]["clean"] is True
assert inventory["qualification"]["sourceTree"]["revision"] == expected
assert inventory["qualification"]["releaseEligible"] is True
assert inventory["qualification"]["ineligibilityReasons"] == []
for name, run_id in (("CI", 40660642386), ("Release", 40664505738)):
    run = next(run for run in load("builds-4382-runs.stdout")["workflow_runs"] if run["id"] == run_id)
    assert run["name"] == name and run["head_sha"] == expected and run["status"] == "completed" and run["conclusion"] == "success"
payload = archive.read_bytes()
feed = load("builds-4382-feed-catalog.stdout")
assert feed["repository"]["commit"] == expected
assert feed["version"] == "4.38.2" and feed["packageHashAlgorithm"] == "SHA512"
assert base64.b64encode(hashlib.sha512(payload).digest()).decode() == feed["packageHash"]
assert len(payload) == feed["packageSize"]
with zipfile.ZipFile(archive) as package:
    nuspec = package.read("Hexalith.Builds.Module.Cli.nuspec")
    document = ET.fromstring(nuspec)
    namespace = {"n": document.tag.split("}")[0].strip("{")}
    metadata = document.find("n:metadata", namespace)
    assert metadata.find("n:version", namespace).text == "4.38.2"
    assert metadata.find("n:repository", namespace).attrib["commit"] == expected
    assert ".signature.p7s" in package.namelist()
    tooling_hash = hashlib.sha256(package.read("tools/net10.0/any/Hexalith.Builds.Tooling.dll")).hexdigest()
(receipt / "builds-4382.nuspec.xml").write_bytes(nuspec)
assert load("builds-4382-signature.command.json")["exit_code"] == 0
catalog = load("builds-4382-actual-embedded-catalog.stdout")
properties = source_catalog(expected)
assert properties["HexalithConversationsVersion"] == "1.3.1"
assert properties["HexalithEventStoreVersion"] == catalog["eventStoreVersion"] == "3.128.4"
assert properties["HexalithAspireAppHostSdkVersion"] == catalog["aspireAppHostSdkVersion"] == "13.6.4"
assert git(builds, "diff", "--name-only", "16ff70a2321ad576faaac3fe53088c2f1bdacae3", expected, "--", "src/libraries/Hexalith.Builds.Tooling", "src/libraries/Hexalith.Builds.Module.Cli", "test/Hexalith.Builds.Module.Tests/Workspace") == ""
platform_tags = {}
for tag_name in ("v1.0.0", "v1.0.1"):
    commit = git(platform, "rev-parse", tag_name + "^{commit}")
    builds_pin = git(platform, "rev-parse", commit + ":references/Hexalith.Builds")
    platform_tags[tag_name] = {"commit": commit, "builds_pin": builds_pin, "catalog_properties": source_catalog(builds_pin)}
assert platform_tags["v1.0.1"]["catalog_properties"]["HexalithConversationsVersion"] == "1.3.0"
remote_refs = (receipt / "platform-remote-tags.stdout").read_text().splitlines()
assert {line.split()[1] for line in remote_refs} == {"refs/tags/v1.0.0", "refs/tags/v1.0.0^{}", "refs/tags/v1.0.1", "refs/tags/v1.0.1^{}"}
for name, value in platform_tags.items():
    assert any(line == value["commit"] + "\trefs/tags/" + name + "^{}" for line in remote_refs)
assert load("platform-releases.stdout") == []
current_platform = git(platform, "rev-parse", "HEAD")
current_pin = git(platform, "rev-parse", "HEAD:references/Hexalith.Builds")
report = {
    "result": "published Builds candidate verified; compatible released Platform identity absent in observed official refs",
    "source": expected,
    "version": "4.38.2",
    "signed_archive": {"path": str(archive), "sha256": hashlib.sha256(payload).hexdigest(), "sha512": feed["packageHash"], "size": len(payload), "signature_command_exit": 0, "revocation_mode": "default inherited mode; no offline override supplied"},
    "official_inventory": {"sha256": hashlib.sha256(inventory_bytes).hexdigest(), "size": len(inventory_bytes), "asset_id": asset["id"], "unsigned_module_package": next(p for p in inventory["packages"] if p["id"] == "Hexalith.Builds.Module.Cli")},
    "tooling_assembly_sha256": tooling_hash,
    "embedded_catalog": catalog,
    "source_catalog_properties": properties,
    "platform_tags": platform_tags,
    "current_platform": {"commit": current_platform, "builds_pin": current_pin},
    "unchanged_between_4381_and_4382": ["src/libraries/Hexalith.Builds.Tooling", "src/libraries/Hexalith.Builds.Module.Cli", "test/Hexalith.Builds.Module.Tests/Workspace"],
    "limitations": ["Official unsigned inventory ZIP hash differs from the NuGet repository-signed ZIP; no unsigned ZIP reconstruction claimed.", "No Platform release qualification or runtime integration acceptance is inferred.", "Standalone SHA512 endpoint returned HTTP failure; the official NuGet catalog binds actual signed archive SHA512 and size."],
    "local_git_observations": observations,
}
(receipt / "published-candidate-identity.json").write_text(json.dumps(report, indent=2) + "\n")
print(json.dumps({"version": report["version"], "source": expected, "signed_sha256": report["signed_archive"]["sha256"], "size": len(payload), "result": report["result"]}, indent=2))
