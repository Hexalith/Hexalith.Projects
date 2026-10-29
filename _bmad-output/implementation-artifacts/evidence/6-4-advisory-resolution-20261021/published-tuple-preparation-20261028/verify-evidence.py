#!/usr/bin/env python3
"""Read-only comparison of the immutable entry state, prepared inputs and results."""
import hashlib
import gzip
import json
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET

receipt = Path(__file__).resolve().parent
root = receipt.parents[4]
load = lambda path: json.loads(path.read_bytes())
sha = lambda path: hashlib.sha256(path.read_bytes()).hexdigest()
initial_bytes = gzip.decompress((receipt / "initial-state.json.gz").read_bytes())
compression = load(receipt / "initial-state.compression.json")
assert len(initial_bytes) == compression["uncompressed_bytes"]
assert hashlib.sha256(initial_bytes).hexdigest() == compression["uncompressed_sha256"]
initial = json.loads(initial_bytes)
prepared = load(receipt / "prepared-source-hashes.json")
allowed = set(prepared) | {"_bmad-output/implementation-artifacts/spec-6-4-resolve-projects-with-transient-current-explanations.md", "_bmad-output/implementation-artifacts/sprint-status.yaml"}
changed = []
count = 0
for owner, baseline in initial["repositories"].items():
    directory = root / owner
    def git(*args):
        return subprocess.check_output(["git", *args], cwd=directory).decode()
    assert git("rev-parse", "HEAD").strip() == baseline["head"], owner
    assert git("ls-files", "--stage") == baseline["index"], owner
    for name, expected in baseline["files"].items():
        path = directory / name
        relative = path.relative_to(root).as_posix()
        actual = sha(path)
        count += 1
        if actual != expected:
            assert relative in allowed, relative
            changed.append(relative)
    modules = directory / ".gitmodules"
    if owner != "." and modules.exists():
        paths = subprocess.run(["git", "config", "--file", str(modules), "--get-regexp", r"^submodule\..*\.path$"], cwd=directory, capture_output=True, text=True, check=False)
        assert paths.returncode in (0, 1)
        for line in paths.stdout.splitlines():
            nested = directory / line.split(maxsplit=1)[1]
            assert not (nested / ".git").exists(), str(nested)
assert set(changed) == allowed, changed
for name, expected in prepared.items():
    assert sha(root / name) == expected, name
prior = load(receipt / "parent-prior-untracked-evidence.json")
for name, expected in prior.items():
    assert sha(root / name) == expected, name
applied = load(receipt.parent / "root-enrollment-applied-20261028/results.json")
for name in ("module/hexalith.module-manifest.v2.json", "module/README.md"):
    assert sha(root / name) == applied["files"][name]
story = root / "_bmad-output/implementation-artifacts/spec-6-4-resolve-projects-with-transient-current-explanations.md"
assert story.read_bytes().startswith((receipt / "spec.active-entry.md.txt").read_bytes())
assert (receipt / "spec.active-entry.md.txt").read_bytes().startswith((receipt / "spec.before.md.txt").read_bytes())
tracking_name = "_bmad-output/implementation-artifacts/sprint-status.yaml"
tracking = (root / tracking_name).read_bytes()
old_comment = b"# Current source/store suites and Conversations 1.3.1 package consumer pass; public Platform/Builds package qualification remains open"
new_comment = b"# Published Builds 4.38.2 verified and local tuple prepared; exact Platform recording, integration qualification, publication and consumer adoption remain open"
assert tracking.count(new_comment) == 1
assert hashlib.sha256(tracking.replace(new_comment, old_comment)).hexdigest() == initial["repositories"]["."]["files"][tracking_name]
frozen = story.read_bytes().split(b"<frozen-after-approval", 1)[1].split(b"</frozen-after-approval>", 1)[0]
assert b"status: 'in-progress'" in story.read_bytes().split(b"<frozen-after-approval", 1)[0]
for name, expected in (("Platform", "4.35.2"), ("Conversations", "4.38.1")):
    directory = root / ("references/Hexalith." + name)
    before = json.loads(subprocess.check_output(["git", "show", "HEAD:.config/dotnet-tools.json"], cwd=directory))
    assert before["tools"]["hexalith.builds.module.cli"]["version"] == expected
    before["tools"]["hexalith.builds.module.cli"]["version"] = "4.38.2"
    assert before == load(directory / ".config/dotnet-tools.json")
assert load(root / "references/Hexalith.Platform/hexalith-platform.json") == {"schemaVersion": 1, "toolVersionRange": "[4.38.2, 5.0.0-0)"}
for name in ("public-package-accepted-4381", "public-package-prepared-4382"):
    command = load(receipt / (name + ".command.json"))
    result = load(receipt / (name + ".stdout"))
    assert command["exit_code"] == 2
    assert [item["ruleId"] for item in result["diagnostics"]] == ["HXW011", "HXW011", "HXW009"]
    missing, dirty, mismatch = [item["message"] for item in result["diagnostics"]]
    assert "fcee54288c75feaadd673e0484da1e5b5d742cc8" in missing and "no local" in missing
    assert "has changes" in dirty
    assert all(value in mismatch for value in ("81ab049208678bd6616828824729c0ddac02f7d1", "2574bf2f3da5367d8801f0d7661d66155e5d90b0"))
    assert not Path(command["argv"][command["argv"].index("--workspace") + 1]).exists()
for name in ("platform-tool-restore", "platform-tool-version", "platform-tool-help", "consumer-tool-restore", "consumer-tool-version", "consumer-tool-help", "consumer-package-release-build", "package-tooling-tests", "platform-narrow-owner-build", "platform-gitlink-patch-check", "published-candidate-qualified-audit"):
    assert load(receipt / (name + ".command.json"))["exit_code"] == 0, name
for name in ("platform-tool-version", "consumer-tool-version"):
    assert (receipt / (name + ".stdout")).read_text().strip() == "4.38.2+2574bf2f3da5367d8801f0d7661d66155e5d90b0"
build = (receipt / "consumer-package-release-build.stdout").read_text()
assert "0 Warning(s)" in build and "0 Error(s)" in build
assets = load(receipt / "consumer-project.assets.json")["libraries"]
for package in ("Hexalith.Conversations.Client/1.3.1", "Hexalith.Conversations.Contracts/1.3.1"):
    assert assets[package]["type"] == "package"
assert not any(value["type"] == "project" and not name.startswith("Hexalith.Projects") for name, value in assets.items())
assert "Ran 19 tests" in (receipt / "package-tooling-tests.stderr").read_text()
assert "\nOK\n" in (receipt / "package-tooling-tests.stderr").read_text()
inventory = ET.parse(receipt / "platform-existing-inventory.xml").getroot().find("assembly")
owner = ET.parse(receipt / "platform-existing-owner-pin.xml").getroot().find("assembly")
assert inventory.attrib["total"] == inventory.attrib["passed"] == "1" and inventory.attrib["failed"] == "0"
assert owner.attrib["total"] == owner.attrib["failed"] == "1" and owner.attrib["passed"] == "0"
failure = "".join(owner.itertext())
assert "v4.38.2^{commit}" in failure and "unknown revision" in failure and "128" in failure
assert load(receipt / "platform-composition-debug-build.command.json")["exit_code"] == 1
assert "3 Error(s)" in (receipt / "platform-composition-debug-build.stdout").read_text()
for name in ("planning-guard", "story-authority", "index-authority", "root-whitespace", "platform-whitespace", "conversations-whitespace"):
    assert load(receipt / (name + ".command.json"))["exit_code"] == 0, name
assert "Ran 37 tests" in (receipt / "planning-guard.stderr").read_text()
if (receipt / "SHA256SUMS.json").exists():
    for name, expected in load(receipt / "SHA256SUMS.json").items():
        assert sha(receipt / name) == expected, name
print(json.dumps({"result": "passed", "original_owner_heads_indices": len(initial["repositories"]), "original_tracked_files_compared": count, "authorized_changed_paths": changed, "immutable_prior_untracked_evidence_files": len(prior), "projects_module_preserved": True, "complete_original_story_prefix_preserved": True, "frozen_inner_sha256": hashlib.sha256(frozen).hexdigest(), "nested_initializations": 0, "new_passing_tests_before_planning": 20, "retained_owner_pin_failures": 1, "public_package_guard_instances": ["HXW011", "HXW011", "HXW009"]}, indent=2))
