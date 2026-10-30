"""Verify this local checkpoint without changing its evidence or Git state."""

import gzip
import hashlib
import json
import re
import subprocess
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = next(parent for parent in HERE.parents if (parent / "Hexalith.Projects.slnx").is_file())
SPEC = "_bmad-output/implementation-artifacts/spec-6-4-resolve-projects-with-transient-current-explanations.md"
TRACKING = "_bmad-output/implementation-artifacts/sprint-status.yaml"
OWNER = "references/Hexalith.Conversations"


def git(repository, *arguments):
    """Run only read-only Git queries."""
    return subprocess.check_output(["git", "-C", str(repository), *arguments]).strip()


def digest(data):
    """Return the SHA-256 of exact retained bytes."""
    return hashlib.sha256(data).hexdigest()


initial = json.loads((HERE / "initial-state.json").read_text())
for relative, before in initial["repositories"].items():
    repository = ROOT / relative
    assert git(repository, "rev-parse", "HEAD").decode() == before["head"], relative
    index = Path(git(repository, "rev-parse", "--path-format=absolute", "--git-path", "index").decode())
    assert digest(index.read_bytes()) == before["index_sha256"], relative
    assert digest(git(repository, "show-ref")) == before["refs_sha256"], relative
    assert digest(git(repository, "ls-files", "-s")) == before["tracked_entries_sha256"], relative
    actual = set(git(repository, "diff", "HEAD", "--name-only", "--ignore-submodules=all").decode().splitlines())
    allowed = {SPEC, TRACKING} if relative == "." else {".config/dotnet-tools.json", "module/README.md"} if relative == OWNER else set()
    assert actual == allowed, (relative, actual, allowed)
    if relative != ".":
        assert not git(repository, "ls-files", "--others", "--exclude-standard"), relative
        nested = sorted(str(path.relative_to(repository)) for path in (repository / "references").glob("*/.git"))
        assert nested == before["nested_initialized"], relative

original = gzip.decompress((HERE / "original-spec.gz").read_bytes())
assert digest(original) == initial["spec_sha256"]
current = (ROOT / SPEC).read_bytes()
assert current.startswith(original)
frozen = re.compile(rb"<frozen-after-approval\b.*?</frozen-after-approval>", re.DOTALL)
assert frozen.search(current).group() == frozen.search(original).group()
tracking = gzip.decompress((HERE / "original-tracking.gz").read_bytes())
assert digest(tracking) == initial["tracking_sha256"]
story = re.compile(rb"(?m)^  6-4-resolve-projects-with-transient-current-explanations: in-progress[^\r\n]*")
assert story.search((ROOT / TRACKING).read_bytes())
assert story.sub(b"STORY64", tracking) == story.sub(b"STORY64", (ROOT / TRACKING).read_bytes())
tools = gzip.decompress((HERE / "original-conversations-tools.gz").read_bytes())
assert (ROOT / OWNER / ".config/dotnet-tools.json").read_bytes() == tools.replace(b'"version": "4.38.2"', b'"version": "4.38.3"')
old_readme = gzip.decompress((HERE / "original-conversations-module-readme.gz").read_bytes())
new_readme = (ROOT / OWNER / "module/README.md").read_bytes()
title, old_body = old_readme.split(b"\n\n", 1)
assert new_readme.startswith(title + b"\n\n") and new_readme.endswith(old_body)

commands = json.loads((HERE / "commands.json").read_text())
names = {item["name"] for item in commands}
assert len(names) == len(commands)
failures = {"public-package-conversations-tool", "public-package-platform-tool", "prepared-public-package"}
assert failures <= names
for record in commands:
    assert record["exit_code"] == (2 if record["name"] in failures else 0), record["name"]
    assert (HERE / record["stdout"]).is_file() and (HERE / record["stderr"]).is_file()
for name in ["prepared-public-package", "public-package-platform-tool"]:
    result = json.loads((HERE / (name + ".stdout")).read_text())
    assert result["status"] == "failed"
    assert [item["ruleId"] for item in result["diagnostics"]] == ["HXW011", "HXW009"]
    text = json.dumps(result)
    for identity in ["9f43cefe11bd246ce0de1cd0740261b557c0c7a0", "d1931b1e803144048d927fff16cb8119afde541e", "e909ffd9fe9a6097e36d932010b983d029396968"]:
        assert identity in text
for name in ["public-source", "prepared-public-source"]:
    assert json.loads((HERE / (name + ".stdout")).read_text())["status"] == "prepared"
for name, count in [("package-tooling-tests", 19), ("planning-guard", 37)]:
    text = (HERE / (name + ".stderr")).read_text()
    assert re.search(rf"Ran {count} tests in", text) and text.rstrip().endswith("OK"), name
build = (HERE / "consumer-package-release-build.stdout").read_text()
assert "Build succeeded." in build and "0 Warning(s)" in build and "0 Error(s)" in build
assets = gzip.decompress((HERE / "consumer-project.assets.json.gz").read_bytes())
assert digest(assets) == json.loads((HERE / "consumer-assets-audit.json").read_text())["assets_sha256"]
libraries = json.loads(assets)["libraries"]
for name in ["Hexalith.Conversations.Client/1.3.1", "Hexalith.Conversations.Contracts/1.3.1"]:
    assert libraries[name]["type"] == "package"
assert not [name for name, item in libraries.items() if item["type"] == "project" and not name.startswith("Hexalith.Projects")]
assert "<clear />" in (HERE / "offline.nuget.config.txt").read_text()
identity = json.loads((HERE / "installed-tool-identity.json").read_text())
assert digest((ROOT / identity["archive"]).read_bytes()) == identity["archive_sha256"]
assert digest(Path(identity["installed_tooling_path"]).read_bytes()) == identity["installed_tooling_sha256"]
delta = json.loads((HERE / "historical-full-suite-input-delta.json").read_text())
assert not delta["root_src_tests_docs_changes"] and not delta["conversations_src_tests_changes"]
inventory = HERE / "SHA256SUMS.json"
if inventory.exists():
    for relative, expected in json.loads(inventory.read_text()).items():
        assert digest((HERE / relative).read_bytes()) == expected, relative
print(json.dumps({"status": "passed", "repositories_preserved": len(initial["repositories"]), "commands": len(commands), "tests_passed": 56, "package_rules": ["HXW011", "HXW009"], "source_implementation_changed": False}, indent=2))
