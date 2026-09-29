"""Replay the pending 6.1-P1R EventStore 3.109.0 candidate's release-bypass and checkout-drift claims.

Every technical claim in the EventStore owner record is compared with an independent source: the
public GitHub Actions run, job and workflow-run resources, or EventStore Git history at the root
gitlink. Only the containment policy (the bypass and tag-CI failure stay open EventStore Owner
disposition items) is asserted against fixed constants.
"""

from __future__ import annotations

import json
import os
import re
import subprocess
import unittest
import urllib.request
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[2]
EVENTSTORE_ROOT = PROJECT_ROOT / "references" / "Hexalith.EventStore"
RECORD_PATH = EVENTSTORE_ROOT / "_bmad-output/implementation-artifacts/evidence/6-1-p1r-3109/public-packages.json"
SPRINT_STATUS_PATH = PROJECT_ROOT / "_bmad-output/implementation-artifacts/sprint-status.yaml"
OWNER_PACKET_PATH = PROJECT_ROOT / "_bmad-output/implementation-artifacts/6-1-p1r-3109-exact-baseline-candidate.md"
REPOSITORY_API = "https://api.github.com/repos/Hexalith/Hexalith.EventStore/"
OPEN_DISPOSITION = "open-eventstore-owner-disposition"
PASSING_JOB_CONCLUSIONS = {"success", "skipped"}
RECORD = json.loads(RECORD_PATH.read_text(encoding="utf-8"))


def github_api(path: str) -> dict:
    """Read one public EventStore API resource; GITHUB_TOKEN only raises the rate limit."""
    request = urllib.request.Request(REPOSITORY_API + path, headers={"Accept": "application/vnd.github+json"})
    token = os.environ.get("GITHUB_TOKEN")
    if token:
        request.add_header("Authorization", f"Bearer {token}")
    with urllib.request.urlopen(request, timeout=60) as response:
        return json.load(response)


def github_run(path: str) -> dict:
    return github_api(f"actions/runs/{path}")


def eventstore_git(*arguments: str) -> str:
    completed = subprocess.run(
        ["git", "-C", str(EVENTSTORE_ROOT), *arguments], capture_output=True, text=True, check=True, timeout=60)
    return completed.stdout.strip()


def contains_word(text: str, word: str) -> bool:
    return re.search(rf"(?<![A-Za-z0-9_]){re.escape(word)}(?![A-Za-z0-9_])", text) is not None


class ReleaseValidationBypassTests(unittest.TestCase):
    """The release bypass stays an open EventStore Owner disposition and is never reported as green CI."""

    def test_release_run_used_commitlint_source_proof_instead_of_ci(self) -> None:
        release = RECORD["release_workflow_run"]
        bypass = release["validation_bypass"]
        run = github_run(str(release["id"]))
        self.assertEqual(run["head_sha"], RECORD["tag_commit"])
        self.assertEqual(run["event"], "workflow_dispatch")
        self.assertEqual(run["conclusion"], "success")
        self.assertEqual(run["path"], ".github/workflows/release.yml")
        proof = github_run(str(bypass["source_proof_run_id"]))
        self.assertEqual(proof["head_sha"], RECORD["tag_commit"])
        self.assertEqual(proof["event"], "push")
        self.assertEqual(proof["path"], ".github/workflows/commitlint.yml")
        self.assertEqual(proof["conclusion"], "success")

    def test_no_successful_push_ci_exists_for_the_tag(self) -> None:
        tag_ci = RECORD["tag_ci_run"]
        page = github_api(f"actions/workflows/ci.yml/runs?head_sha={RECORD['tag_commit']}&event=push&per_page=100")
        runs = page["workflow_runs"]
        self.assertEqual(page["total_count"], len(runs))
        self.assertIn(tag_ci["id"], {run["id"] for run in runs})
        self.assertNotIn("success", {run["conclusion"] for run in runs})
        self.assertFalse(RECORD["release_workflow_run"]["validation_bypass"]["green_ci_proof"])

    def test_tag_ci_failed_only_in_contracts(self) -> None:
        tag_ci = RECORD["tag_ci_run"]
        run = github_run(str(tag_ci["id"]))
        self.assertEqual(run["head_sha"], RECORD["tag_commit"])
        self.assertEqual(run["path"], ".github/workflows/ci.yml")
        self.assertEqual(run["event"], "push")
        self.assertEqual(run["conclusion"], "failure")
        page = github_run(f"{tag_ci['id']}/jobs?per_page=100")
        jobs = page["jobs"]
        self.assertEqual(page["total_count"], len(jobs))
        conclusions = {job["name"]: job["conclusion"] for job in jobs}
        self.assertEqual(len(conclusions), len(jobs), "job names must be unique")
        # Any conclusion other than success or skipped (failure, cancelled, timed_out, action_required,
        # neutral, stale or still running) counts as a failed job.
        failed = {name for name, conclusion in conclusions.items() if conclusion not in PASSING_JOB_CONCLUSIONS}
        self.assertEqual(failed, {tag_ci["failed_job"]["name"]})
        failed_job = next(job for job in jobs if job["name"] == tag_ci["failed_job"]["name"])
        self.assertEqual(failed_job["id"], tag_ci["failed_job"]["id"])
        self.assertEqual(failed_job["conclusion"], "failure")
        self.assertEqual({name for name, conclusion in conclusions.items() if conclusion == "success"},
                         set(tag_ci["passed_jobs"]))
        self.assertEqual({name for name, conclusion in conclusions.items() if conclusion == "skipped"},
                         set(tag_ci["skipped_jobs"]))

    def test_projects_records_keep_the_bypass_open(self) -> None:
        # Containment policy: until an EventStore Owner decision is recorded, both items stay open.
        self.assertEqual(RECORD["release_workflow_run"]["validation_bypass"]["disposition"], OPEN_DISPOSITION)
        self.assertTrue(RECORD["tag_ci_run"]["disposition"].startswith(OPEN_DISPOSITION))
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
        identical = self.observation["storage_record_blobs_identical_v3_70_1_to_head"]
        self.assertTrue(identical, "the storage-record list must not be empty")
        for path in identical:
            self.assertEqual(eventstore_git("rev-parse", f"v3.70.1:{path}"), eventstore_git("rev-parse", f"{checkout}:{path}"))
        changed = self.observation["storage_record_changed_after_tag"]
        for revision in ("v3.70.1", RECORD["tag"]):
            self.assertEqual(eventstore_git("rev-parse", f"{revision}:{changed['path']}"), changed["blob_v3_70_1_to_v3_109_0"])
        self.assertEqual(eventstore_git("rev-parse", f"{checkout}:{changed['path']}"), changed["blob_head"])
        added = re.search(r"\bparameter (\w+)", changed["change"])
        self.assertIsNotNone(added, changed["change"])
        self.assertFalse(contains_word(eventstore_git("show", f"{RECORD['tag']}:{changed['path']}"), added.group(1)))
        self.assertTrue(contains_word(eventstore_git("show", f"{checkout}:{changed['path']}"), added.group(1)))

    def test_interface_changes_after_tag_match_git(self) -> None:
        # Each recorded interface gains members or a required parameter type after the tag, so every
        # implementer of that interface must change: the checkout is source-breaking for implementers.
        checkout = self.observation["sha"]
        changes = self.observation["interface_changes_after_tag"]
        self.assertTrue(changes, "the interface-change list must not be empty")
        item_pattern = re.compile(r"^(?P<interface>I[A-Z]\w*)(?:\.\w+)? (?P<change>adds|gains) (?P<rest>.+)$")
        for item in changes:
            match = item_pattern.match(item)
            self.assertIsNotNone(match, item)
            if match["change"] == "adds":
                introduced = re.findall(r"\b[A-Z]\w*Async\b", match["rest"])
            else:
                introduced = re.findall(r"\b([A-Z]\w*)\[\]", match["rest"])
            self.assertTrue(introduced, item)
            declarations = eventstore_git("grep", "-l", "-E", rf"interface {match['interface']}\b", checkout, "--", "src").splitlines()
            self.assertEqual(len(declarations), 1, item)
            path = declarations[0].split(":", 1)[1]
            tag_source = eventstore_git("show", f"{RECORD['tag']}:{path}")
            checkout_source = eventstore_git("show", f"{checkout}:{path}")
            for identifier in introduced:
                self.assertFalse(contains_word(tag_source, identifier), f"{item}: {identifier} already at the tag")
                self.assertTrue(contains_word(checkout_source, identifier), f"{item}: {identifier} absent at the checkout")
        self.assertTrue(self.observation["source_breaking_for_implementers"])


if __name__ == "__main__":
    unittest.main()
