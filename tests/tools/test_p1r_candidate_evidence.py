"""Replay the selected 3.110.0 P1R evidence and preserve the historical 3.109.0 claims.

Replayed against public GitHub Actions resources: the release run's event, head, workflow and
conclusion and its Commitlint source-proof run; the absence of any successful push CI run for the
tag; and the job conclusions of the recorded tag-CI attempt. Replayed against EventStore Git: the
recorded historical checkout descends from its tag, the tag-to-checkout commit and `src` path
counts, the storage-record blobs, `RetainedFloor` and each recorded interface member. Historical
open dispositions stay in the record and historical Projects index text. The selected archive
record is bound to the fixed accepted tuple, independent push CI for its tagged commit,
Commitlint publication proof, tagged manifest, and consumer fixture bytes. G-6 checks checkout drift;
the historical source observation does not describe today's root gitlink.

Not replayed here: the 14 package hashes and signatures (`verify_public_packages.py`), the
`BYPASS_VALIDATION` job environment and the failing test names (job logs need authentication),
and the release-time Builds revision (release artifact).

Archive hashes and signatures are replayed by the selected record's verify_public_packages.py.
Update the selected replay when a new exact tuple is accepted; preserve historical records.
"""

from __future__ import annotations

import http.client
import hashlib
import json
import os
import re
import subprocess
import time
import unittest
import urllib.error
import urllib.request
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[2]
EVENTSTORE_ROOT = PROJECT_ROOT / "references" / "Hexalith.EventStore"
RECORD_PATH = EVENTSTORE_ROOT / "_bmad-output/implementation-artifacts/evidence/6-1-p1r-3109/public-packages.json"
SELECTED_RECORD_PATH = EVENTSTORE_ROOT / "_bmad-output/implementation-artifacts/evidence/6-1-p1r-3110/public-packages.json"
ACCEPTANCE_PATH = PROJECT_ROOT / "_bmad-output/implementation-artifacts/6-1-p1r-acceptance.json"
CURRENT_OWNER_PACKET_PATH = PROJECT_ROOT / "_bmad-output/implementation-artifacts/6-1-p1r-current-exact-baseline-candidate.md"
SPRINT_STATUS_PATH = PROJECT_ROOT / "_bmad-output/implementation-artifacts/sprint-status.yaml"
OWNER_PACKET_PATH = PROJECT_ROOT / "_bmad-output/implementation-artifacts/6-1-p1r-3109-exact-baseline-candidate.md"
REPOSITORY_API = "https://api.github.com/repos/Hexalith/Hexalith.EventStore/"
OPEN_DISPOSITION = "open-eventstore-owner-disposition"
PASSING_JOB_CONCLUSIONS = {"success", "skipped"}
# The record captured the tag CI run's first attempt (its failed job belongs to attempt 1); a later
# rerun adds attempts, so jobs are read from this attempt rather than from the latest one.
TAG_CI_RECORDED_ATTEMPT = 1
# Transient GitHub responses (rate limits and server errors) are retried a bounded number of times,
# as are network errors, read timeouts and truncated or unparsable response bodies.
RETRIED_HTTP_STATUSES = {403, 429}
RETRIED_READ_ERRORS = (urllib.error.URLError, TimeoutError, http.client.IncompleteRead, json.JSONDecodeError)
API_ATTEMPTS = 4
# `git merge-base --is-ancestor` exits 1 for "not an ancestor" and 128 when an object is missing.
NOT_AN_ANCESTOR = 1
MISSING_HISTORY = 128
RECORD = json.loads(RECORD_PATH.read_text(encoding="utf-8"))
SELECTED_RECORD = json.loads(SELECTED_RECORD_PATH.read_text(encoding="utf-8"))


def retry_delay(attempt: int, error: urllib.error.HTTPError | None) -> float:
    """Honor a short Retry-After header, otherwise back off 2, 4, 8 seconds."""
    retry_after = error.headers.get("Retry-After") if error is not None and error.headers is not None else None
    if retry_after is not None and retry_after.isdigit() and int(retry_after) <= 60:
        return float(retry_after)
    return float(2 ** (attempt + 1))


def github_api(path: str, attempts: int = API_ATTEMPTS) -> dict:
    """Read one public EventStore API resource; GITHUB_TOKEN only raises the rate limit.

    Network errors, read timeouts, truncated or unparsable bodies, HTTP 5xx and rate-limit 403/429
    responses are retried; any other HTTP error, or the last failed attempt, is raised so the replay
    fails loudly instead of skipping.
    """
    for attempt in range(attempts):
        request = urllib.request.Request(REPOSITORY_API + path, headers={"Accept": "application/vnd.github+json"})
        token = os.environ.get("GITHUB_TOKEN")
        if token:
            request.add_header("Authorization", f"Bearer {token}")
        try:
            with urllib.request.urlopen(request, timeout=60) as response:
                return json.load(response)
        except urllib.error.HTTPError as error:
            if attempt == attempts - 1 or not (error.code >= 500 or error.code in RETRIED_HTTP_STATUSES):
                raise
            time.sleep(retry_delay(attempt, error))
        except RETRIED_READ_ERRORS:
            if attempt == attempts - 1:
                raise
            time.sleep(retry_delay(attempt, None))
    raise AssertionError("unreachable")


def github_run(path: str) -> dict:
    return github_api(f"actions/runs/{path}")


def github_run_search(path: str, attempts: int = 3) -> dict:
    """Filtered run searches intermittently return an empty page; retry before trusting emptiness."""
    for attempt in range(attempts):
        page = github_api(path)
        if page["workflow_runs"] or attempt == attempts - 1:
            return page
        time.sleep(2 * (attempt + 1))
    raise AssertionError("unreachable")


def eventstore_git(*arguments: str) -> str:
    completed = subprocess.run(
        ["git", "-C", str(EVENTSTORE_ROOT), *arguments], capture_output=True, text=True, check=True, timeout=60)
    return completed.stdout.strip()


def eventstore_git_status(*arguments: str) -> int:
    return subprocess.run(
        ["git", "-C", str(EVENTSTORE_ROOT), *arguments], capture_output=True, text=True, check=False, timeout=60).returncode


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
        page = github_run_search(f"actions/workflows/ci.yml/runs?head_sha={RECORD['tag_commit']}&event=push&per_page=100")
        runs = page["workflow_runs"]
        self.assertEqual(page["total_count"], len(runs))
        self.assertIn(tag_ci["id"], {run["id"] for run in runs})
        self.assertNotIn("success", {run["conclusion"] for run in runs})
        self.assertFalse(RECORD["release_workflow_run"]["validation_bypass"]["green_ci_proof"])

    def test_tag_ci_failed_only_in_contracts(self) -> None:
        tag_ci = RECORD["tag_ci_run"]
        attempt = f"{tag_ci['id']}/attempts/{TAG_CI_RECORDED_ATTEMPT}"
        run = github_run(attempt)
        self.assertEqual(run["run_attempt"], TAG_CI_RECORDED_ATTEMPT)
        self.assertEqual(run["head_sha"], RECORD["tag_commit"])
        self.assertEqual(run["path"], ".github/workflows/ci.yml")
        self.assertEqual(run["event"], "push")
        self.assertEqual(run["conclusion"], "failure")
        page = github_run(f"{attempt}/jobs?per_page=100")
        jobs = page["jobs"]
        self.assertEqual(page["total_count"], len(jobs))
        self.assertEqual({job["run_attempt"] for job in jobs}, {TAG_CI_RECORDED_ATTEMPT})
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

    def test_historical_checkout_descends_from_the_recorded_tag(self) -> None:
        # The 3.109.0 packet is superseded by the accepted 3.110.0 published tuple. Its source
        # measurements still bind this recorded checkout; current checkout closure belongs to G-6.
        checkout = self.observation["sha"]
        tag = RECORD["tag_commit"]
        status = eventstore_git_status("merge-base", "--is-ancestor", tag, checkout)
        self.assertNotEqual(status, MISSING_HISTORY,
                            f"EventStore history is missing: the recorded checkout {checkout} or tag {tag} "
                            "is not in the clone; fetch the full EventStore history")
        self.assertNotEqual(status, NOT_AN_ANCESTOR,
                            f"recorded tag {tag} is not an ancestor of historical checkout {checkout}")
        self.assertEqual(status, 0, f"git merge-base --is-ancestor {tag} {checkout} exited {status}")

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


class SelectedPublishedEvidenceTests(unittest.TestCase):
    """The current published replay binds the accepted tuple without granting checkout usability."""

    def test_archive_record_binds_the_fixed_selected_tuple(self) -> None:
        acceptance = json.loads(ACCEPTANCE_PATH.read_text(encoding="utf-8"))
        selected = acceptance["selected"]
        self.assertEqual(SELECTED_RECORD["version"], selected["eventstore_version"])
        self.assertEqual(SELECTED_RECORD["tag"], selected["eventstore_tag"])
        self.assertEqual(SELECTED_RECORD["tag_commit"], selected["eventstore_revision"])
        self.assertEqual(set(acceptance["decisions"]),
                         {"EventStore Owner", "Builds Owner", "Solution Architect", "Test Architect"})
        for role, decision in acceptance["decisions"].items():
            with self.subTest(role=role):
                self.assertEqual(decision["decision"], "accept")
                self.assertTrue(decision["approver"].strip())
                self.assertEqual(decision["selected"], "#/selected")
        self.assertEqual(SELECTED_RECORD["release_validation_bypass"]["status"], "already-accepted")
        self.assertIn(selected["builds_revision"], CURRENT_OWNER_PACKET_PATH.read_text(encoding="utf-8"))
        self.assertIn(SELECTED_RECORD_PATH.parent.name, SPRINT_STATUS_PATH.read_text(encoding="utf-8"))
        self.assertIn("usable_as_prerequisite remains false", SELECTED_RECORD["open_limitations"])

    def test_selected_release_uses_commitlint_proof_and_separate_push_ci(self) -> None:
        bypass = SELECTED_RECORD["release_validation_bypass"]
        for run_id, workflow, event in (
            (bypass["release_run_id"], ".github/workflows/release.yml", "workflow_dispatch"),
            (bypass["source_proof_run_id"], ".github/workflows/commitlint.yml", "push"),
        ):
            with self.subTest(run_id=run_id):
                run = github_run(f"{run_id}/attempts/1")
                self.assertEqual(run["head_sha"], SELECTED_RECORD["tag_commit"])
                self.assertEqual(run["run_attempt"], 1)
                self.assertEqual(run["event"], event)
                self.assertEqual(run["path"], workflow)
                self.assertEqual(run["conclusion"], "success")
        self.assertEqual(bypass["source_proof_workflow"], "commitlint.yml")
        self.assertNotEqual(bypass["source_proof_run_id"], bypass["independent_tag_ci_run_id"])

    def test_independent_push_ci_for_tagged_commit_passed_with_only_aspire_and_performance_skipped(self) -> None:
        bypass = SELECTED_RECORD["release_validation_bypass"]
        attempt = f"{bypass['independent_tag_ci_run_id']}/attempts/1"
        run = github_run(attempt)
        self.assertEqual(run["head_sha"], SELECTED_RECORD["tag_commit"])
        self.assertEqual(run["run_attempt"], 1)
        self.assertEqual(run["path"], ".github/workflows/ci.yml")
        self.assertEqual(run["event"], "push")
        self.assertEqual(bypass["independent_tag_ci_conclusion"], "success")
        self.assertEqual(run["conclusion"], bypass["independent_tag_ci_conclusion"])
        page = github_run(f"{attempt}/jobs?per_page=100")
        jobs = page["jobs"]
        self.assertEqual(page["total_count"], len(jobs))
        self.assertEqual({job["run_attempt"] for job in jobs}, {1})
        conclusions = {job["name"]: job["conclusion"] for job in jobs}
        self.assertEqual(len(conclusions), len(jobs), "job names must be unique")
        self.assertEqual({name for name, conclusion in conclusions.items() if conclusion == "success"},
                         {"ci / contracts", "ci / tenants-source-mode", "ci / semantic-release-governance", "ci / build-and-test"})
        self.assertEqual({name for name, conclusion in conclusions.items() if conclusion == "skipped"},
                         {"ci / aspire-tests", "ci / performance-tests"})
        self.assertTrue(all(conclusion in PASSING_JOB_CONCLUSIONS for conclusion in conclusions.values()))

    def test_tagged_manifest_and_recorded_checkout_match_archive_inventory(self) -> None:
        self.assertEqual(eventstore_git("rev-parse", f"{SELECTED_RECORD['tag']}^{{commit}}"), SELECTED_RECORD["tag_commit"])
        path = "tools/release-packages.json"
        source = subprocess.run(["git", "-C", str(EVENTSTORE_ROOT), "show", f"{SELECTED_RECORD['tag']}:{path}"],
                                capture_output=True, check=True, timeout=60).stdout
        self.assertEqual(hashlib.sha256(source).hexdigest(), SELECTED_RECORD["release_manifest_sha256"])
        manifest = json.loads(source)
        packages = SELECTED_RECORD["packages"]
        self.assertEqual(len(packages), 14)
        self.assertEqual(manifest["packages"], [{"id": row["id"], "project": row["project"]} for row in packages])
        observation = SELECTED_RECORD["release_manifest_current_checkout"]
        self.assertEqual(observation["sha256"], SELECTED_RECORD["release_manifest_sha256"])
        self.assertIs(observation["unchanged_since_tag"], True)
        self.assertEqual(observation["exit_code"], 0)
        checkout = observation["checkout"]
        self.assertEqual(eventstore_git_status("diff", "--quiet", SELECTED_RECORD["tag"], checkout, "--", path), 0)
        for row in packages:
            with self.subTest(package=row["id"]):
                self.assertEqual(row["nuget_version"], SELECTED_RECORD["version"])
                self.assertEqual(row["nuget_repository"]["commit"], SELECTED_RECORD["tag_commit"])
                self.assertEqual(row["github_repository"], row["nuget_repository"])

    def test_selected_consumer_fixture_bytes_match_the_archive_replay(self) -> None:
        consumer = SELECTED_RECORD["independent_consumer"]
        self.assertEqual(set(consumer["fixture_sha256"]), {"Consumer.csproj", "PublishedApiSmoke.cs", "NuGet.Config"})
        for name, expected in consumer["fixture_sha256"].items():
            with self.subTest(file=name):
                self.assertEqual(hashlib.sha256((SELECTED_RECORD_PATH.parent / "consumer" / name).read_bytes()).hexdigest(), expected)


if __name__ == "__main__":
    unittest.main()
