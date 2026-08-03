"""Deterministic positive and negative tests for the planning scheduling guard."""

from __future__ import annotations

import importlib.util
import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[2]
GUARD_PATH = PROJECT_ROOT / "tools" / "planning" / "validate_production_authority.py"
SPEC = importlib.util.spec_from_file_location("validate_production_authority", GUARD_PATH)
if SPEC is None or SPEC.loader is None:
    raise RuntimeError(f"Cannot load scheduling guard from {GUARD_PATH}")
GUARD = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(GUARD)


class ProductionAuthorityGuardTests(unittest.TestCase):
    """Prove current production epics pass and historical epics fail closed."""

    @classmethod
    def setUpClass(cls) -> None:
        cls.sprint_status = GUARD.DEFAULT_SPRINT_STATUS.read_text(encoding="utf-8")

    def assert_index_rejected(self, sprint_status: str) -> None:
        with tempfile.TemporaryDirectory() as directory:
            candidate = Path(directory) / "sprint-status.yaml"
            candidate.write_text(sprint_status, encoding="utf-8")
            with self.assertRaises(GUARD.GuardViolation):
                GUARD.validate_index(candidate)

    def test_current_production_epics_are_allowed(self) -> None:
        for epic, story in ((6, 1), (7, 15), (8, 11)):
            with self.subTest(epic=epic):
                self.assertEqual(
                    (epic, story),
                    GUARD.validate_story_request(f"{epic}.{story}"),
                )

    def test_historical_epics_are_rejected(self) -> None:
        for epic in range(1, 6):
            with self.subTest(epic=epic):
                with self.assertRaisesRegex(
                    GUARD.GuardViolation,
                    "immutable implementation history",
                ):
                    GUARD.validate_story_request(f"{epic}.1")

    def test_workspace_sprint_index_preserves_guard_and_history(self) -> None:
        self.assertEqual(
            GUARD.EXPECTED_PRODUCTION_EPICS,
            GUARD.validate_index(),
        )

    def test_invalid_index_variants_fail_closed(self) -> None:
        mutations = {
            "wrong authority": self.sprint_status.replace(
                "production_authority_epics: [6, 7, 8]",
                "production_authority_epics: [5, 6, 7, 8]",
                1,
            ),
            "missing authority": self.sprint_status.replace(
                "production_authority_epics: [6, 7, 8]\n",
                "",
                1,
            ),
            "duplicate authority": self.sprint_status
            + "\nproduction_authority_epics: [6, 7, 8]\n",
            "wrong historical digest": self.sprint_status.replace(
                GUARD.EXPECTED_HISTORICAL_STATUS_SHA256,
                "0" * 64,
                1,
            ),
            "historical reopened": self.sprint_status.replace(
                "  1-1-module-scaffold-build-ci-wiring: done",
                "  1-1-module-scaffold-build-ci-wiring: backlog",
                1,
            ),
            "historical quoted status": self.sprint_status.replace(
                "  1-1-module-scaffold-build-ci-wiring: done",
                '  1-1-module-scaffold-build-ci-wiring: "backlog"',
                1,
            ),
            "historical inline comment": self.sprint_status.replace(
                "  1-1-module-scaffold-build-ci-wiring: done",
                "  1-1-module-scaffold-build-ci-wiring: backlog # hidden reopen",
                1,
            ),
            "historical key removed": self.sprint_status.replace(
                "  1-1-module-scaffold-build-ci-wiring: done\n",
                "",
                1,
            ),
            "duplicate development mapping": self.sprint_status
            + "\ndevelopment_status:\n  epic-1: done\n",
            "duplicate story key": self.sprint_status.replace(
                "  6-1-list-and-open-projects-through-supported-authenticated-paths: blocked",
                "  6-1-list-and-open-projects-through-supported-authenticated-paths: blocked\n"
                "  6-1-list-and-open-projects-through-supported-authenticated-paths: backlog",
                1,
            ),
            "malformed tracking key": self.sprint_status.replace(
                "  6-1-list-and-open-projects-through-supported-authenticated-paths: blocked",
                "  6-1-list-and-open-projects-through-supported-authenticated-paths malformed: blocked",
                1,
            ),
            "missing production epic": self.sprint_status.replace(
                "  epic-8: backlog\n",
                "",
                1,
            ),
        }

        for name, candidate in mutations.items():
            with self.subTest(name=name):
                self.assert_index_rejected(candidate)

    def test_malformed_and_unknown_story_ids_fail_closed(self) -> None:
        for story_id in ("6.1.2", "6.1/garbage", "6.1\n7.1", "epic-6", "6"):
            with self.subTest(story_id=story_id):
                with self.assertRaises(GUARD.GuardViolation):
                    GUARD.assert_story_allowed(story_id)

        with self.assertRaisesRegex(GUARD.GuardViolation, "exactly one"):
            GUARD.validate_story_request("6.999")

    def test_command_line_exit_codes_enforce_the_hard_stop(self) -> None:
        environment = {**os.environ, "PYTHONDONTWRITEBYTECODE": "1"}
        for arguments, expected in (
            (("--validate-index",), 0),
            (("--story-id", "6.1"), 0),
            (("--story-id", "1.1"), 1),
        ):
            with self.subTest(arguments=arguments):
                completed = subprocess.run(
                    [sys.executable, str(GUARD_PATH), *arguments],
                    cwd=PROJECT_ROOT,
                    env=environment,
                    capture_output=True,
                    text=True,
                    check=False,
                )
                self.assertEqual(expected, completed.returncode, completed.stderr)

        with tempfile.TemporaryDirectory() as directory:
            candidate = Path(directory) / "sprint-status.yaml"
            candidate.write_text(
                self.sprint_status.replace(
                    "production_authority_epics: [6, 7, 8]",
                    "production_authority_epics: [1, 2, 3]",
                    1,
                ),
                encoding="utf-8",
            )
            completed = subprocess.run(
                [
                    sys.executable,
                    str(GUARD_PATH),
                    "--validate-index",
                    "--sprint-status",
                    str(candidate),
                ],
                cwd=PROJECT_ROOT,
                env=environment,
                capture_output=True,
                text=True,
                check=False,
            )
            self.assertEqual(1, completed.returncode, completed.stderr)

    def test_bmad_workflows_and_ci_consume_the_guard(self) -> None:
        persistent_fact = '"file:{project-root}/**/project-context.md"'
        for path in (
            PROJECT_ROOT / ".agents" / "skills" / "bmad-create-story" / "customize.toml",
            PROJECT_ROOT / ".agents" / "skills" / "bmad-sprint-planning" / "customize.toml",
        ):
            self.assertIn(persistent_fact, path.read_text(encoding="utf-8"), str(path))

        project_context = (
            PROJECT_ROOT / "_bmad-output" / "project-context.md"
        ).read_text(encoding="utf-8")
        self.assertIn(
            "validate_production_authority.py --story-id <epic.story>",
            project_context,
        )
        self.assertIn(
            "--validate-index --sprint-status <candidate>",
            project_context,
        )
        self.assertIn("replace the active index atomically", project_context)

        ci_workflow = (PROJECT_ROOT / ".github" / "workflows" / "ci.yml").read_text(
            encoding="utf-8"
        )
        self.assertIn(
            "python3 -m unittest tests/tools/test_production_authority_guard.py -v",
            ci_workflow,
        )


if __name__ == "__main__":
    unittest.main()
