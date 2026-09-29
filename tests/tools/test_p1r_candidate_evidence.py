"""Replay the pending 6.1-P1R EventStore 3.109.0 candidate's release-bypass and checkout-drift claims."""

from __future__ import annotations

import json
import os
import subprocess
import unittest
import urllib.request
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[2]
EVENTSTORE_ROOT = PROJECT_ROOT / "references" / "Hexalith.EventStore"
RECORD_PATH = EVENTSTORE_ROOT / "_bmad-output/implementation-artifacts/evidence/6-1-p1r-3109/public-packages.json"
SPRINT_STATUS_PATH = PROJECT_ROOT / "_bmad-output/implementation-artifacts/sprint-status.yaml"
OWNER_PACKET_PATH = PROJECT_ROOT / "_bmad-output/implementation-artifacts/6-1-p1r-3109-exact-baseline-candidate.md"
RUNS_API = "https://api.github.com/repos/Hexalith/Hexalith.EventStore/actions/runs/"
OPEN_DISPOSITION = "open-eventstore-owner-disposition"
RECORD = json.loads(RECORD_PATH.read_text(encoding="utf-8"))


def github_run(path: str) -> dict:
    """Read one public Actions run resource; GITHUB_TOKEN only raises the rate limit."""
    request = urllib.request.Request(RUNS_API + path, headers={"Accept": "application/vnd.github+json"})
    token = os.environ.get("GITHUB_TOKEN")
    if token:
        request.add_header("Authorization", f"Bearer {token}")
    with urllib.request.urlopen(request, timeout=60) as response:
        return json.load(response)


def eventstore_git(*arguments: str) -> str:
    completed = subprocess.run(
        ["git", "-C", str(EVENTSTORE_ROOT), *arguments], capture_output=True, text=True, check=True, timeout=60)
    return completed.stdout.strip()


class ReleaseValidationBypassTests(unittest.TestCase):
    """The release bypass stays an open EventStore Owner disposition and is never reported as green CI."""

    def test_release_run_used_commitlint_source_proof_instead_of_ci(self) -> None:
        release = RECORD["release_workflow_run"]
        bypass = release["validation_bypass"]
        run = github_run(str(release["id"]))
        self.assertEqual(run["head_sha"], RECORD["tag_commit"])
        self.assertEqual(run["conclusion"], "success")
        self.assertEqual(run["path"], ".github/workflows/release.yml")
        proof = github_run(str(bypass["source_proof_run_id"]))
        self.assertEqual(proof["head_sha"], RECORD["tag_commit"])
        self.assertEqual(proof["path"], ".github/workflows/commitlint.yml")
        self.assertEqual(proof["conclusion"], bypass["source_proof_run_conclusion"])
        self.assertFalse(bypass["green_ci_proof"])
        self.assertEqual(bypass["disposition"], OPEN_DISPOSITION)

    def test_tag_ci_failed_only_in_contracts(self) -> None:
        tag_ci = RECORD["tag_ci_run"]
        run = github_run(str(tag_ci["id"]))
        self.assertEqual(run["head_sha"], RECORD["tag_commit"])
        self.assertEqual(run["path"], ".github/workflows/ci.yml")
        self.assertEqual(run["conclusion"], "failure")
        jobs = github_run(f"{tag_ci['id']}/jobs?per_page=100")["jobs"]
        failed = {job["name"] for job in jobs if job["conclusion"] == "failure"}
        self.assertEqual(failed, {tag_ci["failed_job"]["name"]})
        self.assertEqual(tag_ci["contracts_test_summary"]["failed"], len(tag_ci["failed_tests"]))
        self.assertTrue(tag_ci["disposition"].startswith(OPEN_DISPOSITION))

    def test_projects_records_keep_the_bypass_open(self) -> None:
        sprint_status = SPRINT_STATUS_PATH.read_text(encoding="utf-8")
        self.assertIn(f'eventstore_release_validation: "BYPASS_VALIDATION; Commitlint run {RECORD["release_workflow_run"]["validation_bypass"]["source_proof_run_id"]} was the source proof, not CI"', sprint_status)
        self.assertIn(f'actions/runs/{RECORD["tag_ci_run"]["id"]} failed: 3 Contracts OQ8 Story 4.15 governance tests"', sprint_status)
        self.assertIn("BYPASS_VALIDATION", OWNER_PACKET_PATH.read_text(encoding="utf-8"))


class CheckoutDriftTests(unittest.TestCase):
    """The later EventStore checkout's drift from the tag is recorded exactly and marked untested."""

    observation = RECORD["newer_checkout_observation"]

    def test_commit_and_source_path_counts_match_git(self) -> None:
        checkout = self.observation["sha"]
        self.assertEqual(int(eventstore_git("rev-list", "--count", f"{RECORD['tag']}..{checkout}")),
                         self.observation["tag_ahead_commit_count"])
        statuses = [line.split("\t", 1)[0] for line in eventstore_git(
            "diff", "--name-status", "--no-renames", RECORD["tag"], checkout, "--", "src").splitlines()]
        self.assertEqual(len(statuses), self.observation["changed_source_paths"])
        self.assertEqual(statuses.count("A"), self.observation["added_source_paths"])
        self.assertEqual(statuses.count("M"), self.observation["modified_source_paths"])
        self.assertEqual(statuses.count("D"), self.observation["deleted_source_paths"])

    def test_storage_record_blobs_match_git(self) -> None:
        checkout = self.observation["sha"]
        for path in self.observation["storage_record_blobs_identical_v3_70_1_to_head"]:
            self.assertEqual(eventstore_git("rev-parse", f"v3.70.1:{path}"), eventstore_git("rev-parse", f"{checkout}:{path}"))
        changed = self.observation["storage_record_changed_after_tag"]
        for revision in ("v3.70.1", RECORD["tag"]):
            self.assertEqual(eventstore_git("rev-parse", f"{revision}:{changed['path']}"), changed["blob_v3_70_1_to_v3_109_0"])
        self.assertEqual(eventstore_git("rev-parse", f"{checkout}:{changed['path']}"), changed["blob_head"])

    def test_checkout_is_marked_breaking_for_implementers(self) -> None:
        self.assertTrue(self.observation["source_breaking_for_implementers"])
        self.assertTrue(self.observation["interface_changes_after_tag"])


if __name__ == "__main__":
    unittest.main()
