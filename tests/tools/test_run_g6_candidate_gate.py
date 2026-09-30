"""Hermetic tests for the status-aware G-6 candidate gate.

Each test builds a throwaway repository with two recorded gitlinks, a stub G-6 validator that
records its arguments, a packet and a workflow. They never read the real packet, validator or
gitlinks, so they run in workflow-gates without failing on a routine submodule bump.
"""

from __future__ import annotations

import contextlib
import importlib.util
import io
import json
import subprocess
import tempfile
import unittest
from pathlib import Path


SCRIPT = Path(__file__).resolve().with_name("run_g6_candidate_gate.py")
SPEC = importlib.util.spec_from_file_location("run_g6_candidate_gate", SCRIPT)
assert SPEC is not None and SPEC.loader is not None
GATE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(GATE)
BUILDS_REVISION = "1" * 40
EVENTSTORE_REVISION = "2" * 40
STUB_VALIDATOR = """import json, sys
from pathlib import Path
here = Path(__file__).resolve().parent
(here / "calls.jsonl").open("a", encoding="utf-8").write(json.dumps(sys.argv[1:]) + "\\n")
exit_code = int((here / "exit-code.txt").read_text(encoding="utf-8"))
print("G6-EVIDENCE-CANDIDATE-VALID" if "--candidate" in sys.argv else "G6-EVIDENCE-VALID") if exit_code == 0 else print("G6-EVIDENCE-INVALID: stub", file=sys.stderr)
raise SystemExit(exit_code)
"""


class StatusAwareGateTests(unittest.TestCase):
    """Exercise mode selection and the gitlink and Builds execution-SHA checks."""

    def setUp(self) -> None:
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.workspace = Path(self.temporary_directory.name)
        self.run_git("init", "--quiet")
        for path, revision in (
            ("references/Hexalith.Builds", BUILDS_REVISION),
            ("references/Hexalith.EventStore", EVENTSTORE_REVISION),
        ):
            self.run_git("update-index", "--add", "--cacheinfo", f"160000,{revision},{path}")
        self.tools = self.workspace / "references/Hexalith.Builds/Tools"
        self.tools.mkdir(parents=True)
        (self.tools / "validate-runtime-toolchain-evidence.py").write_text(STUB_VALIDATOR, encoding="utf-8")
        self.set_validator_exit(0)
        (self.tools / "baseline.json").write_text("{}\n", encoding="utf-8")
        self.write_workflow(BUILDS_REVISION, BUILDS_REVISION)
        self.write_packet("pending")

    def tearDown(self) -> None:
        self.temporary_directory.cleanup()

    def run_git(self, *arguments: str) -> None:
        subprocess.run(["git", "-C", str(self.workspace), *arguments], capture_output=True, text=True, check=True)

    def set_validator_exit(self, exit_code: int) -> None:
        (self.tools / "exit-code.txt").write_text(str(exit_code), encoding="utf-8")

    def write_workflow(self, workflow_sha: str, action_sha: str, extra: str = "") -> None:
        workflow = self.workspace / ".github/workflows/ci.yml"
        workflow.parent.mkdir(parents=True, exist_ok=True)
        workflow.write_text(
            "jobs:\n"
            "  ci:\n"
            f"    uses: Hexalith/Hexalith.Builds/.github/workflows/domain-ci.yml@{workflow_sha}\n"
            "  g6-candidate:\n"
            "    steps:\n"
            "      - name: Initialize root-declared submodules\n"
            f"        uses: Hexalith/Hexalith.Builds/Github/initialize-build@{action_sha} # pinned\n"
            "      - name: Initialize Dapr\n"
            "        uses: ./references/Hexalith.Builds/Github/dapr-init\n"
            f"{extra}",
            encoding="utf-8")

    def write_packet(self, status: object, eventstore: str = EVENTSTORE_REVISION, document: object | None = None) -> None:
        packet = document if document is not None else {
            "status": status,
            "repositories": [
                {"name": "Hexalith.Projects", "path": ".", "revision": "0" * 40},
                {"name": "Hexalith.Builds", "path": "references/Hexalith.Builds", "revision": BUILDS_REVISION},
                {"name": "Hexalith.EventStore", "path": "references/Hexalith.EventStore", "revision": eventstore},
            ],
        }
        (self.workspace / "packet.json").write_text(json.dumps(packet), encoding="utf-8")

    def run_gate(self) -> tuple[int, str, str]:
        stdout, stderr = io.StringIO(), io.StringIO()
        with contextlib.redirect_stdout(stdout), contextlib.redirect_stderr(stderr):
            exit_code = GATE.main([
                "--workspace", str(self.workspace),
                "--baseline", "references/Hexalith.Builds/Tools/baseline.json",
                "--packet", "packet.json",
            ])
        return exit_code, stdout.getvalue(), stderr.getvalue()

    def validator_calls(self) -> list[list[str]]:
        calls = self.tools / "calls.jsonl"
        if not calls.exists():
            return []
        return [json.loads(line) for line in calls.read_text(encoding="utf-8").splitlines()]

    def test_pending_packet_runs_the_candidate_validator(self) -> None:
        exit_code, stdout, stderr = self.run_gate()

        self.assertEqual(0, exit_code, stderr)
        calls = self.validator_calls()
        self.assertEqual(1, len(calls))
        self.assertEqual("--candidate", calls[0][-1])
        self.assertIn("packet status pending; candidate mode exited 0: G6-EVIDENCE-CANDIDATE-VALID", stdout)
        self.assertIn("G6-PACKET-GITLINKS-EXACT: 2 submodule revisions equal their root gitlinks", stdout)
        self.assertIn(f"G6-BUILDS-EXECUTION-SHA-EXACT: CI executes Hexalith.Builds at the root gitlink {BUILDS_REVISION}", stdout)
        self.assertIn("G6-GATE-PASSED", stdout)

    def test_accepted_packet_runs_the_accepted_only_validator(self) -> None:
        self.write_packet("accepted")

        exit_code, stdout, stderr = self.run_gate()

        self.assertEqual(0, exit_code, stderr)
        calls = self.validator_calls()
        self.assertEqual(1, len(calls))
        self.assertNotIn("--candidate", calls[0])
        self.assertIn("packet status accepted; accepted-only mode exited 0: G6-EVIDENCE-VALID", stdout)

    def test_failing_validator_fails_the_gate_and_the_other_checks_still_report(self) -> None:
        self.set_validator_exit(1)

        exit_code, stdout, stderr = self.run_gate()

        self.assertEqual(1, exit_code)
        self.assertIn("G6-GATE-VALIDATOR: the candidate validator exited 1: G6-EVIDENCE-INVALID: stub", stderr)
        self.assertIn("G6-PACKET-GITLINKS-EXACT", stdout)
        self.assertIn("G6-BUILDS-EXECUTION-SHA-EXACT", stdout)
        self.assertIn("G6-GATE-FAILED: 1 G-6 check(s) failed", stderr)

    def test_unknown_status_fails_without_running_the_validator(self) -> None:
        for status in ("rejected", None, "Pending"):
            with self.subTest(status=status):
                self.write_packet(status)

                exit_code, _, stderr = self.run_gate()

                self.assertEqual(1, exit_code)
                self.assertIn("the packet status must be pending or accepted", stderr)
        self.assertEqual([], self.validator_calls())

    def test_packet_that_is_not_an_object_fails(self) -> None:
        self.write_packet(None, document=["pending"])

        exit_code, _, stderr = self.run_gate()

        self.assertEqual(1, exit_code)
        self.assertIn("the packet is not a JSON object", stderr)
        self.assertEqual([], self.validator_calls())

    def test_submodule_gitlink_drift_fails(self) -> None:
        self.write_packet("pending", eventstore="3" * 40)

        exit_code, _, stderr = self.run_gate()

        self.assertEqual(1, exit_code)
        self.assertIn(f"Hexalith.EventStore is bound at '{'3' * 40}' but the root gitlink is '{EVENTSTORE_REVISION}'", stderr)

    def test_builds_execution_sha_that_differs_from_the_gitlink_fails(self) -> None:
        self.write_workflow("4" * 40, "4" * 40)

        exit_code, _, stderr = self.run_gate()

        self.assertEqual(1, exit_code)
        self.assertIn(f"CI executes Hexalith.Builds '{'4' * 40}' but the root Hexalith.Builds gitlink is '{BUILDS_REVISION}'", stderr)

    def test_two_builds_execution_shas_fail(self) -> None:
        self.write_workflow(BUILDS_REVISION, "5" * 40)

        exit_code, _, stderr = self.run_gate()

        self.assertEqual(1, exit_code)
        self.assertIn("must execute Hexalith.Builds at exactly one SHA", stderr)

    def test_commented_builds_reference_is_ignored(self) -> None:
        self.write_workflow(BUILDS_REVISION, BUILDS_REVISION,
                            "      # uses: Hexalith/Hexalith.Builds/Github/initialize-build@" + "6" * 40 + "\n")

        exit_code, _, stderr = self.run_gate()

        self.assertEqual(0, exit_code, stderr)

    def test_workflow_without_builds_references_fails(self) -> None:
        (self.workspace / ".github/workflows/ci.yml").write_text("jobs: {}\n", encoding="utf-8")

        exit_code, _, stderr = self.run_gate()

        self.assertEqual(1, exit_code)
        self.assertIn("must execute Hexalith.Builds at exactly one SHA; found []", stderr)


if __name__ == "__main__":
    unittest.main()
