"""Read-only checks for the recorded-owner checkpoint and its preservation claims."""

import gzip
import hashlib
import json
import re
import subprocess
from pathlib import Path
from xml.etree import ElementTree

HERE = Path(__file__).resolve().parent
ROOT = next(parent for parent in HERE.parents if (parent / "Hexalith.Projects.slnx").exists())
SPEC = "_bmad-output/implementation-artifacts/spec-6-4-resolve-projects-with-transient-current-explanations.md"
TRACKING = "_bmad-output/implementation-artifacts/sprint-status.yaml"


def git(directory, *arguments):
    """Return output only after a successful read-only Git command."""
    return subprocess.check_output(["git", "-C", str(directory), *arguments]).strip()


def digest(data):
    """Compute a local integrity digest without altering its input."""
    return hashlib.sha256(data).hexdigest()


initial = json.loads((HERE / "initial-state.json").read_text())
for relative, expected in initial["repositories"].items():
    repository = ROOT / relative
    assert not expected["status"], (relative, "entry checkout was not clean")
    assert git(repository, "rev-parse", "HEAD").decode() == expected["head"], relative
    index = Path(git(repository, "rev-parse", "--path-format=absolute", "--git-path", "index").decode())
    assert digest(index.read_bytes()) == expected["index_sha256"], relative
    assert digest(git(repository, "ls-files", "-s")) == expected["tracked_entries_sha256"], relative
    changes = git(repository, "diff", "HEAD", "--name-only").decode().splitlines()
    assert set(changes) <= ({SPEC, TRACKING} if relative == "." else set()), (relative, changes)
    if relative != ".":
        assert not git(repository, "status", "--porcelain=v1"), relative
        assert not list((repository / "references").glob("*/.git")), relative

original = gzip.decompress((HERE / "original-spec.gz").read_bytes())
assert digest(original) == initial["spec_sha256"]
current = (ROOT / SPEC).read_bytes()
assert current.startswith(original)
frozen = re.compile(rb"<frozen-after-approval\b.*?</frozen-after-approval>", re.DOTALL)
assert frozen.search(current).group() == frozen.search(original).group()
original_tracking = gzip.decompress((HERE / "original-tracking.gz").read_bytes())
assert digest(original_tracking) == initial["tracking_sha256"]
tracking = (ROOT / TRACKING).read_bytes()
story_line = re.compile(rb"(?m)^  6-4-resolve-projects-with-transient-current-explanations: in-progress[^\r\n]*")
assert story_line.search(tracking)
assert story_line.sub(b"STORY64", tracking) == story_line.sub(b"STORY64", original_tracking)

commands = json.loads((HERE / "commands.json").read_text())
by_name = {record["name"]: record for record in commands}
assert len(by_name) == len(commands)
expected_failures = {
    "public-package-current": 2,
    "public-source-current": 2,
    "pinned-builds-object-before": 128,
    "public-package-object-available": 2,
    "platform-composition-debug-build": 1,
    "platform-existing-owner-pin-test": 1,
}
for record in commands:
    assert record["exit_code"] == expected_failures.get(record["name"], 0), record["name"]
    assert record["environment"] == {}
    assert (HERE / record["stdout"]).is_file()
    assert (HERE / record["stderr"]).is_file()
for name in expected_failures:
    assert name in by_name
assert (HERE / "builds-refs-before-object-fetch.txt").read_bytes() == (HERE / "builds-refs-after-object-fetch.txt").read_bytes()
assert "--no-write-fetch-head" in by_name["pinned-builds-object-fetch"]["argv"]
package = json.loads((HERE / "public-package-object-available.stdout").read_text())
assert [item["ruleId"] for item in package["diagnostics"]] == ["HXW011", "HXW009"]
source = json.loads((HERE / "public-source-object-available.stdout").read_text())
assert source["status"] == "prepared"
for name, count in [("package-tooling-tests", 19), ("planning-guard", 37)]:
    text = (HERE / (name + ".stderr")).read_text()
    assert re.search(rf"Ran {count} tests in", text) and text.rstrip().endswith("OK"), name
for label, expected_result in [("inventory", "Pass"), ("owner-pin", "Fail")]:
    tests = ElementTree.parse(HERE / ("platform-existing-" + label + ".xml")).getroot().findall(".//test")
    assert len(tests) == 1 and tests[0].attrib["result"] == expected_result
pin_failure = (HERE / "platform-existing-owner-pin-test.stdout").read_text()
assert "v4.38.2^{commit}" in pin_failure and "unknown revision or path" in pin_failure
build = (HERE / "platform-composition-debug-build.stdout").read_text()
errors = set(re.findall(r"/([^/]+\.cs)\((\d+,\d+)\): error (CA\d+):", build))
assert errors == {
    ("AgentsAdmissionCurrentBoundaryTests.cs", "93,62", "CA2007"),
    ("AgentsHostStartupBoundaryTests.cs", "36,16", "CA1062"),
    ("PortableDevelopmentCompositionTests.cs", "56,16", "CA1062"),
    ("AcceptedLaunchBoundaryTests.cs", "27,16", "CA1062"),
}
assets = (HERE / "consumer-project.assets.json").read_bytes()
assert digest(assets) == json.loads((HERE / "consumer-assets-audit.json").read_text())["assets_sha256"]
libraries = json.loads(assets)["libraries"]
for name in ["Hexalith.Conversations.Client/1.3.1", "Hexalith.Conversations.Contracts/1.3.1"]:
    assert libraries[name]["type"] == "package"
assert not [name for name, item in libraries.items() if item["type"] == "project" and not name.startswith("Hexalith.Projects")]
handoff = json.loads((HERE / "current-publication-adoption-handoff.json").read_text())
for relative, expected in handoff["file_sha256"].items():
    assert digest((ROOT / relative).read_bytes()) == expected, relative
inventory = HERE / "SHA256SUMS.json"
if inventory.exists():
    for relative, expected in json.loads(inventory.read_text()).items():
        assert digest((HERE / relative).read_bytes()) == expected, relative
print(json.dumps({"status": "passed", "preserved_repositories": len(initial["repositories"]), "commands": len(commands), "tests_passed": 57, "tests_failed": 1, "platform_analyzer_errors": 4, "package_rules": ["HXW011", "HXW009"], "source_preparation": "passed", "production_source_changed": False}, indent=2))
