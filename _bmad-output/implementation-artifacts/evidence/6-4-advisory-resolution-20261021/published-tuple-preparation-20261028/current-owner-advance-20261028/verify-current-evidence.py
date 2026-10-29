#!/usr/bin/env python3
"""Read-only verification of this continuation; historical identities stay fixed."""
import gzip
import hashlib
import json
from pathlib import Path
import subprocess


receipt = Path(__file__).resolve().parent
root = receipt.parents[5]


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def git(repo, *args):
    return subprocess.check_output(["git", *args], cwd=repo).decode().strip()


raw = gzip.decompress((receipt / "initial-state.json.gz").read_bytes())
identity = json.loads((receipt / "initial-state-identity.json").read_text())
assert len(raw) == identity["uncompressed_size"]
assert hashlib.sha256(raw).hexdigest() == identity["uncompressed_sha256"]
initial = json.loads(raw)
for name, expected in initial["repositories"].items():
    repo = root if name == "." else root / name
    assert git(repo, "rev-parse", "HEAD") == expected["head"], name
    index = Path(git(repo, "rev-parse", "--git-path", "index"))
    if not index.is_absolute():
        index = repo / index
    assert sha(index) == expected["index_sha256"], name
    assert git(repo, "branch", "--show-current") == expected["branch"], name
    if name != ".":
        assert not list((repo / "references").glob("*/.git")), name

allowed = {
    "_bmad-output/implementation-artifacts/spec-6-4-resolve-projects-with-transient-current-explanations.md",
    "_bmad-output/implementation-artifacts/sprint-status.yaml",
}
for path, expected in initial["tracked_files"].items():
    if path not in allowed:
        assert sha(root / path) == expected, path
for path, expected in initial["prior_evidence"].items():
    assert sha(root / path) == expected, path

story = root / next(path for path in allowed if path.endswith(".md"))
original = (receipt / "original-story.md.txt").read_bytes()
assert story.read_bytes().startswith(original), "Complete original story prefix changed"
frozen_end = original.index(b"</frozen-after-approval>") + len(b"</frozen-after-approval>")
assert story.read_bytes()[:frozen_end] == original[:frozen_end]
old_sprint = (receipt / "original-sprint-status.yaml.txt").read_bytes().splitlines(keepends=True)
new_sprint = (root / "_bmad-output/implementation-artifacts/sprint-status.yaml").read_bytes().splitlines(keepends=True)
assert len(old_sprint) == len(new_sprint)
sprint_changes = []
for line, (before, after) in enumerate(zip(old_sprint, new_sprint), 1):
    if before != after:
        assert b"6-4-resolve-projects-with-transient-current-explanations: in-progress #" in before
        assert b"6-4-resolve-projects-with-transient-current-explanations: in-progress #" in after
        sprint_changes.append(line)
assert len(sprint_changes) == 1

for path, expected in json.loads((receipt / "SHA256SUMS.json").read_text()).items():
    assert sha(receipt / path) == expected, path

commands = {path.name.removesuffix(".command.json"): json.loads(path.read_text())
            for path in receipt.glob("*.command.json")}
assert commands["public-package-current"]["exit_code"] == 2
package = json.loads((receipt / "public-package-current.stdout").read_text())
assert package["status"] == "failed"
assert [item["ruleId"] for item in package["diagnostics"]] == ["HXW008", "HXW011", "HXW011", "HXW009"]
messages = " ".join(item["message"] for item in package["diagnostics"])
for expected in ("1d748b70d5f728f237a7d86048e8f4636f770601", "fcee54288c75feaadd673e0484da1e5b5d742cc8",
                 "e909ffd9fe9a6097e36d932010b983d029396968", "2574bf2f3da5367d8801f0d7661d66155e5d90b0",
                 "81ab049208678bd6616828824729c0ddac02f7d1"):
    assert expected in messages, expected
assert commands["public-source-current"]["exit_code"] == 0
assert json.loads((receipt / "public-source-current.stdout").read_text())["status"] == "prepared"
assert commands["consumer-package-release-build"]["exit_code"] == 0
build = (receipt / "consumer-package-release-build.stdout").read_text()
assert "0 Warning(s)" in build and "0 Error(s)" in build
assets = json.loads((receipt / "consumer-project.assets.json").read_text())
assert assets["libraries"]["Hexalith.Conversations.Client/1.3.1"]["type"] == "package"
assert assets["libraries"]["Hexalith.Conversations.Contracts/1.3.1"]["type"] == "package"
assert all(value["type"] != "project" or key.startswith("Hexalith.Projects")
           for key, value in assets["libraries"].items())
assert sha(root / "src/Hexalith.Projects.Server/obj/project.assets.json") == sha(receipt / "consumer-project.assets.json")
for name in ("platform-tool-restore", "platform-tool-version", "platform-tool-help",
             "consumer-tool-restore", "consumer-tool-version", "consumer-tool-help",
             "package-tooling-tests", "planning-guard", "story-authority", "index-authority",
             "root-whitespace", "platform-whitespace", "conversations-whitespace"):
    assert commands[name]["exit_code"] == 0, name
assert "Ran 19 tests" in (receipt / "package-tooling-tests.stderr").read_text()
assert "Ran 37 tests" in (receipt / "planning-guard.stderr").read_text()
assert commands["platform-composition-debug-build"]["exit_code"] == 1
platform_build = (receipt / "platform-composition-debug-build.stdout").read_text()
for expected in ("error CA2007", "error CA1062", "AgentsAdmissionCurrentBoundaryTests.cs",
                 "AgentsHostStartupBoundaryTests.cs", "AcceptedLaunchBoundaryTests.cs",
                 "PortableDevelopmentCompositionTests.cs", "4 Error(s)"):
    assert expected in platform_build, expected
delta = json.loads((receipt / "current-identities-and-source-delta.json").read_text())
assert delta["historical_patch_check"]["exit_code"] == 1
assert "patch does not apply" in delta["historical_patch_check"]["stderr"]
assert delta["platform_preparation_base_changes"]["stdout"] == ""
assert delta["builds_tooling_delta"]["stdout"] == ""
assert "5.11.1" in delta["builds_catalog_delta"]["stdout"]
handoff = json.loads((receipt / "current-publication-adoption-handoff.json").read_text())
assert handoff["future_platform_commit"] is None
assert handoff["current_platform_head"] == initial["repositories"]["references/Hexalith.Platform"]["head"]
assert handoff["current_builds_head"] == initial["repositories"]["references/Hexalith.Builds"]["head"]
print(json.dumps({
    "status": "passed",
    "preserved_head_index_pairs": len(initial["repositories"]),
    "preserved_tracked_files_except_two_authorized_story_tracking_paths": len(initial["tracked_files"]) - 2,
    "preserved_prior_evidence_files": len(initial["prior_evidence"]),
    "complete_original_story_prefix_preserved": True,
    "sprint_comment_only_changed_lines": sprint_changes,
    "current_passing_tests": 56,
    "package_preparation": "blocked: HXW008, HXW011, HXW011, HXW009",
    "platform_broad_build": "blocked: existing CA2007/CA1062 errors",
    "historical_full_suites": "not repeated or promoted to current changed-input acceptance",
}, indent=2))
