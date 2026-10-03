"""Hermetic change selection controls for the current G-6 CI gate."""

from __future__ import annotations

import importlib.util
import json
import os
import re
import subprocess
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch


SCRIPT = Path(__file__).resolve().with_name("run_g6_ci_gate.py")
SPEC = importlib.util.spec_from_file_location("g6_ci_gate", SCRIPT)
assert SPEC is not None and SPEC.loader is not None
GATE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(GATE)
BASE = "a" * 40
POLICY = {"materialFiles": ["global.json", "references/Hexalith.Builds/Props/Directory.Packages.props"],
          "materialPrefixes": ["src/", "references/Hexalith.EventStore/src/"]}


class G6ChangeSelectionTests(unittest.TestCase):
    def select(self, changed: str, event: str = "push", base: str = BASE) -> tuple[bool, str]:
        def fake_git(*arguments):
            if arguments[0] == "cat-file":
                return subprocess.CompletedProcess(arguments, 0, "", "")
            self.assertEqual("diff", arguments[0])
            return subprocess.CompletedProcess(arguments, 0, changed, "")
        with patch.object(GATE, "git", side_effect=fake_git):
            return GATE.selection(event, base, POLICY)

    def test_unrelated_root_gitlink_does_not_require_live_proof(self) -> None:
        self.assertEqual((False, "not required by this change"),
                         self.select("references/Hexalith.AI.Tools\n"))

    def test_runtime_owner_gitlink_requires_live_proof(self) -> None:
        required, reason = self.select("references/Hexalith.EventStore\n")
        self.assertTrue(required)
        self.assertIn("runtime-owning submodule", reason)

    def test_root_material_requires_live_proof(self) -> None:
        self.assertTrue(self.select("src/Hexalith.Projects.AppHost/Program.cs\n")[0])
        self.assertTrue(self.select("global.json\n")[0])

    def test_unrelated_root_document_is_not_required(self) -> None:
        self.assertEqual((False, "not required by this change"), self.select("docs/guide.md\n"))

    def test_schedule_or_uncertain_base_is_fail_closed(self) -> None:
        self.assertTrue(GATE.selection("schedule", "", POLICY)[0])
        self.assertTrue(GATE.selection("push", "0" * 40, POLICY)[0])

    def test_docs_only_change_reports_not_required_even_with_unapproved_audit(self) -> None:
        (GATE.ROOT / ".g6-current-evidence").mkdir(exist_ok=True)
        with tempfile.TemporaryDirectory(dir=GATE.ROOT / ".g6-current-evidence") as temporary:
            root = Path(temporary)
            policy = root / "policy.json"
            policy.write_text(json.dumps(POLICY), encoding="utf-8")
            output = root / "result"
            audit = {"source": {"rootSha": "b" * 40, "gitlinks": []},
                     "materialInputs": {"fingerprint": "c" * 64},
                     "tupleApproved": False, "issues": ["owner approval is pending"]}
            calls = []

            def fake_run(command, **_kwargs):
                calls.append(command)
                Path(command[command.index("--out") + 1]).write_text(json.dumps(audit), encoding="utf-8")
                return subprocess.CompletedProcess(command, 0)

            with patch.object(GATE, "builds_execution_pins", return_value=["d" * 40]), \
                 patch.object(GATE, "selection", return_value=(False, "not required by this change")), \
                 patch.object(GATE.subprocess, "run", side_effect=fake_run):
                result = GATE.main(["--event", "push", "--base-sha", BASE,
                                    "--policy", str(policy), "--output", str(output)])
            self.assertEqual(0, result)
            self.assertEqual(1, len(calls), "the live runner must not start")
            selected = json.loads((output / "selection.json").read_text(encoding="utf-8"))
            self.assertEqual("not required by this change", selected["status"])
            self.assertFalse(selected["tupleApproved"])
            self.assertEqual(["owner approval is pending"], selected["issues"])


class G6WorkflowPostgresqlTests(unittest.TestCase):
    IMAGE = "postgres@sha256:a02db8cac496f15b094798a38254f14d6e00741f709360e5e00bb6668ea31636"

    def check_pull(self, workflow_name: str, job_name: str, before_step: str, exit_code: int) -> None:
        workflow = (GATE.ROOT / ".github/workflows" / workflow_name).read_text()
        job = re.search(r"(?ms)^  " + job_name + r":\n.*?(?=^  [A-Za-z0-9_-]+:\n|\Z)", workflow).group()
        pulls = re.findall(r"(?ms)^      - name: Pull pinned G-6 PostgreSQL fixture image\n.*?(?=^      - |\Z)", job)
        self.assertEqual(1, len(pulls))
        expected = "      - name: Pull pinned G-6 PostgreSQL fixture image\n        run: docker pull " + self.IMAGE
        self.assertEqual(expected, pulls[0].strip("\n"))
        self.assertLess(job.index(pulls[0]), job.index("      - name: " + before_step))
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            log = directory / "docker.log"
            docker = directory / "docker"
            docker.write_text('#!/usr/bin/env bash\nprintf "%s\n" "$*" > "$DOCKER_LOG"\nexit "$DOCKER_EXIT"\n')
            docker.chmod(0o755)
            environment = dict(os.environ, PATH=str(directory) + os.pathsep + os.environ["PATH"],
                               DOCKER_LOG=str(log), DOCKER_EXIT=str(exit_code))
            result = subprocess.run(["bash", "-c", "docker pull " + self.IMAGE], env=environment,
                                    capture_output=True, text=True, check=False)
            self.assertEqual(exit_code, result.returncode)
            self.assertEqual("pull " + self.IMAGE, log.read_text().strip())

    def test_ci_and_release_pull_exact_digest_and_propagate_pull_failure(self) -> None:
        for workflow, job, before in (("ci.yml", "g6-current", "Audit and qualify current G-6 when required"),
                                      ("release.yml", "release", "Run fresh exact-source G-6 qualifier")):
            for exit_code in (0, 19):
                with self.subTest(workflow=workflow, exit_code=exit_code):
                    self.check_pull(workflow, job, before, exit_code)


if __name__ == "__main__":
    unittest.main()
