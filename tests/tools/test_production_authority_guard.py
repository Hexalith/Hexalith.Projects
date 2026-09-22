"""Focused tests for the production-authority and minimal P1R gate."""

from __future__ import annotations

import importlib.util
import json
import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock


PROJECT_ROOT = Path(__file__).resolve().parents[2]
GUARD_PATH = PROJECT_ROOT / "tools" / "planning" / "validate_production_authority.py"
SPEC = importlib.util.spec_from_file_location("validate_production_authority", GUARD_PATH)
if SPEC is None or SPEC.loader is None:
    raise RuntimeError(f"Cannot load scheduling guard from {GUARD_PATH}")
GUARD = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(GUARD)


class ProductionAuthorityGuardTests(unittest.TestCase):
    """Cover the open gate, acceptance record, and scheduling authority."""

    @classmethod
    def setUpClass(cls) -> None:
        cls.sprint = GUARD.DEFAULT_SPRINT_STATUS.read_text(encoding="utf-8")
        cls.deferred = GUARD.DEFAULT_DEFERRED_WORK.read_text(encoding="utf-8")
        cls.p0 = GUARD.DEFAULT_P0_ARTIFACT.read_text(encoding="utf-8")

    def replace_once(self, text: str, old: str, new: str) -> str:
        self.assertEqual(1, text.count(old), old)
        return text.replace(old, new, 1)

    def replace_after(self, text: str, marker: str, old: str, new: str) -> str:
        self.assertEqual(1, text.count(marker), marker)
        before, separator, after = text.partition(marker)
        self.assertIn(old, after)
        return before + separator + after.replace(old, new, 1)

    def accepted_sprint(self) -> str:
        result = self.replace_after(
            self.sprint,
            '    id: "6.1-P1R"',
            "    status: open",
            "    status: done",
        )
        return self.replace_once(
            result,
            "      stage_1_p1r_baseline: open",
            "      stage_1_p1r_baseline: done",
        )

    def accepted_deferred(self) -> str:
        result = self.deferred
        for entry_id in (35, 68):
            result = self.replace_after(
                result,
                f"### DW-{entry_id}:",
                "status: open",
                "status: done",
            )
        return result

    def accepted_p0(self) -> str:
        return self.replace_once(
            self.p0,
            "p1r_stage_1:\n  status: open",
            "p1r_stage_1:\n  status: done",
        )

    def acceptance_record(self) -> dict[str, object]:
        return {
            "schema": GUARD.ACCEPTANCE_SCHEMA,
            "selected": dict(GUARD.SELECTED_TUPLE),
            "rollback": dict(GUARD.ROLLBACK_TUPLE),
            "timestamp_utc": "2026-09-22T12:00:00Z",
            "decisions": {
                role: {
                    "decision": "accept",
                    "approver": "Jérôme Piquot",
                    "record": GUARD.ACCEPTANCE_RECORD_RELATIVE_PATH.as_posix(),
                    "selected": GUARD.SELECTED_REFERENCE,
                }
                for role in GUARD.ACCEPTED_ROLES
            },
        }

    def validate_boundary(
        self,
        *,
        sprint: str | bytes | None = None,
        deferred: str | bytes | None = None,
        p0: str | bytes | None = None,
        record: dict[str, object] | bytes | None = None,
        symlink_record: bool = False,
    ) -> tuple[int, ...]:
        with tempfile.TemporaryDirectory() as directory:
            workspace = Path(directory)
            sprint_path = workspace / "sprint-status.yaml"
            deferred_path = workspace / "deferred-work.md"
            p0_path = workspace / "p0.md"

            for path, value in (
                (sprint_path, self.sprint if sprint is None else sprint),
                (deferred_path, self.deferred if deferred is None else deferred),
                (p0_path, self.p0 if p0 is None else p0),
            ):
                if isinstance(value, bytes):
                    path.write_bytes(value)
                else:
                    path.write_text(value, encoding="utf-8")
            if record is not None:
                record_path = workspace / GUARD.ACCEPTANCE_RECORD_RELATIVE_PATH
                record_path.parent.mkdir(parents=True)
                payload = record if isinstance(record, bytes) else json.dumps(record).encode()
                if symlink_record:
                    target_path = record_path.with_name("acceptance-target.json")
                    target_path.write_bytes(payload)
                    record_path.symlink_to(target_path.name)
                else:
                    record_path.write_bytes(payload)
            return GUARD.validate_index(
                sprint_path,
                deferred_path,
                p0_path,
                workspace_root=workspace,
            )

    def test_open_gate_passes_without_acceptance_record(self) -> None:
        self.assertEqual(GUARD.EXPECTED_PRODUCTION_EPICS, self.validate_boundary())
        with self.assertRaisesRegex(GUARD.GuardViolation, "6.1-P1R"):
            self.validate_boundary(sprint=self.accepted_sprint())

    def test_valid_acceptance_closes_only_the_four_authorized_items(self) -> None:
        self.assertEqual(
            GUARD.EXPECTED_PRODUCTION_EPICS,
            self.validate_boundary(
                sprint=self.accepted_sprint(),
                deferred=self.accepted_deferred(),
                p0=self.accepted_p0(),
                record=self.acceptance_record(),
            ),
        )

    def test_missing_or_rejected_role_fails_closed_and_names_role(self) -> None:
        for role, mutation in (
            ("Builds Owner", "missing"),
            ("Solution Architect", "reject"),
        ):
            with self.subTest(role=role):
                record = self.acceptance_record()
                decisions = record["decisions"]
                assert isinstance(decisions, dict)
                if mutation == "missing":
                    del decisions[role]
                else:
                    decisions[role]["decision"] = "reject"
                with self.assertRaisesRegex(GUARD.GuardViolation, role):
                    self.validate_boundary(
                        sprint=self.accepted_sprint(),
                        deferred=self.accepted_deferred(),
                        p0=self.accepted_p0(),
                        record=record,
                    )

    def test_selected_and_rollback_coordinate_mismatches_fail_closed(self) -> None:
        for tuple_name, coordinate in (
            ("selected", "builds_revision"),
            ("rollback", "eventstore_revision"),
        ):
            with self.subTest(tuple_name=tuple_name, coordinate=coordinate):
                record = self.acceptance_record()
                coordinates = record[tuple_name]
                assert isinstance(coordinates, dict)
                coordinates[coordinate] = "0" * 40
                with self.assertRaisesRegex(
                    GUARD.GuardViolation, f"{tuple_name} coordinate mismatch"
                ):
                    self.validate_boundary(
                        sprint=self.accepted_sprint(),
                        deferred=self.accepted_deferred(),
                        p0=self.accepted_p0(),
                        record=record,
                    )

    def test_decision_metadata_and_timestamp_fail_closed(self) -> None:
        def mutate_decision(field: str, value: str) -> dict[str, object]:
            record = self.acceptance_record()
            decisions = record["decisions"]
            assert isinstance(decisions, dict)
            decisions["EventStore Owner"][field] = value
            return record

        cases = (
            ("rejected", mutate_decision("decision", "reject"), "EventStore Owner"),
            ("blank approver", mutate_decision("approver", " "), "EventStore Owner"),
            ("wrong record", mutate_decision("record", "wrong.json"), "EventStore Owner"),
            ("wrong selected", mutate_decision("selected", "#/rollback"), "EventStore Owner"),
            (
                "noncanonical timestamp",
                {**self.acceptance_record(), "timestamp_utc": "2026-09-22T12:00:00+00:00"},
                "canonical UTC",
            ),
        )
        for name, record, error in cases:
            with self.subTest(name=name):
                with self.assertRaisesRegex(GUARD.GuardViolation, error):
                    self.validate_boundary(
                        sprint=self.accepted_sprint(),
                        deferred=self.accepted_deferred(),
                        p0=self.accepted_p0(),
                        record=record,
                    )

    def test_each_required_closure_site_transitions_atomically(self) -> None:
        accepted_sprint = self.accepted_sprint()
        accepted_deferred = self.accepted_deferred()
        accepted_p0 = self.accepted_p0()
        cases = (
            (
                "P1R action",
                self.replace_after(
                    accepted_sprint,
                    '    id: "6.1-P1R"',
                    "    status: done",
                    "    status: open",
                ),
                accepted_deferred,
                accepted_p0,
                "6.1-P1R",
            ),
            (
                "sprint P0 Stage 1",
                self.replace_once(
                    accepted_sprint,
                    "      stage_1_p1r_baseline: done",
                    "      stage_1_p1r_baseline: open",
                ),
                accepted_deferred,
                accepted_p0,
                "Stage 1",
            ),
            (
                "P0 artifact Stage 1",
                accepted_sprint,
                accepted_deferred,
                self.replace_once(
                    accepted_p0,
                    "p1r_stage_1:\n  status: done",
                    "p1r_stage_1:\n  status: open",
                ),
                "Stage 1",
            ),
            (
                "DW-35",
                accepted_sprint,
                self.replace_after(
                    accepted_deferred, "### DW-35:", "status: done", "status: open"
                ),
                accepted_p0,
                "DW-35",
            ),
            (
                "DW-68",
                accepted_sprint,
                self.replace_after(
                    accepted_deferred, "### DW-68:", "status: done", "status: open"
                ),
                accepted_p0,
                "DW-68",
            ),
        )
        for name, sprint, deferred, p0, error in cases:
            with self.subTest(name=name):
                with self.assertRaisesRegex(GUARD.GuardViolation, error):
                    self.validate_boundary(
                        sprint=sprint,
                        deferred=deferred,
                        p0=p0,
                        record=self.acceptance_record(),
                    )

    def test_unauthorized_closure_fails_closed(self) -> None:
        sprint = self.replace_once(
            self.accepted_sprint(),
            f"  {GUARD.STORY_6_1_KEY}: blocked",
            f"  {GUARD.STORY_6_1_KEY}: ready-for-dev",
        )
        with self.assertRaisesRegex(GUARD.GuardViolation, "open boundary"):
            self.validate_boundary(
                sprint=sprint,
                deferred=self.accepted_deferred(),
                p0=self.accepted_p0(),
                record=self.acceptance_record(),
            )

    def test_each_downstream_category_remains_closed(self) -> None:
        accepted = self.accepted_sprint()
        cases = (
            (
                "P0 action",
                self.replace_after(
                    accepted, '    id: "6.1-P0"', "    status: open", "    status: done"
                ),
                "6.1-P0",
            ),
            (
                "P0 remaining stages",
                self.replace_once(
                    accepted,
                    "      stages_2_through_7: open",
                    "      stages_2_through_7: done",
                ),
                "stages 2 through 7",
            ),
            (
                "P2",
                self.replace_after(
                    accepted, '    id: "6.1-P2"', "    status: open", "    status: done"
                ),
                "6.1-P2",
            ),
            (
                "P3",
                self.replace_after(
                    accepted, '    id: "6.1-P3"', "    status: open", "    status: done"
                ),
                "6.1-P3",
            ),
            (
                "P4",
                self.replace_after(
                    accepted, '    id: "6.1-P4"', "    status: open", "    status: done"
                ),
                "6.1-P4",
            ),
            (
                "readiness",
                self.replace_once(
                    accepted, "  current_result: NOT_READY", "  current_result: READY"
                ),
                "NOT_READY",
            ),
            (
                "Epic 7",
                self.replace_once(accepted, "  epic-7: backlog", "  epic-7: in-progress"),
                "open boundary",
            ),
            (
                "Epic 8",
                self.replace_once(accepted, "  epic-8: backlog", "  epic-8: in-progress"),
                "open boundary",
            ),
        )
        for name, sprint, error in cases:
            with self.subTest(name=name):
                with self.assertRaisesRegex(GUARD.GuardViolation, error):
                    self.validate_boundary(
                        sprint=sprint,
                        deferred=self.accepted_deferred(),
                        p0=self.accepted_p0(),
                        record=self.acceptance_record(),
                    )

    def test_p0_capability_and_blocker_fields_cannot_advance(self) -> None:
        cases = (
            self.replace_once(
                self.p0,
                "  manifest_contract: implemented-unaccepted",
                "  manifest_contract: accepted",
            ),
            self.replace_once(
                self.p0,
                "blocked_by: [supported-composition, publication, consumer-pin, persisted-qualification, acceptance-record, owner-acceptance]",
                "blocked_by: [publication, consumer-pin, persisted-qualification, acceptance-record, owner-acceptance]",
            ),
        )
        for candidate in cases:
            with self.subTest(candidate=candidate[:100]):
                with self.assertRaisesRegex(GUARD.GuardViolation, "P0"):
                    self.validate_boundary(p0=candidate)

    def test_acceptance_record_symlink_fails_closed(self) -> None:
        with self.assertRaisesRegex(GUARD.GuardViolation, "symlink"):
            self.validate_boundary(
                sprint=self.accepted_sprint(),
                deferred=self.accepted_deferred(),
                p0=self.accepted_p0(),
                record=self.acceptance_record(),
                symlink_record=True,
            )

    def test_malformed_json_fails_closed(self) -> None:
        with self.assertRaisesRegex(GUARD.GuardViolation, "malformed acceptance record"):
            self.validate_boundary(
                sprint=self.accepted_sprint(),
                deferred=self.accepted_deferred(),
                p0=self.accepted_p0(),
                record=b'{"schema":',
            )

    def test_invalid_utf8_fails_closed(self) -> None:
        with self.assertRaisesRegex(GUARD.GuardViolation, "cannot read sprint status"):
            self.validate_boundary(sprint=b"\xff")

    def test_current_production_epics_are_allowed(self) -> None:
        for epic, story in ((6, 1), (7, 15), (8, 11)):
            with self.subTest(epic=epic):
                self.assertEqual((epic, story), GUARD.validate_story_request(f"{epic}.{story}"))

    def test_story_mode_uses_all_supplied_paths_and_reads_sprint_once(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            workspace = Path(directory)
            sprint_path = workspace / "candidate-sprint.yaml"
            deferred_path = workspace / "candidate-deferred.md"
            p0_path = workspace / "candidate-p0.md"
            sprint_path.write_text(self.sprint, encoding="utf-8")
            deferred_path.write_text(self.deferred, encoding="utf-8")
            p0_path.write_text(self.p0, encoding="utf-8")

            with mock.patch.object(
                GUARD, "_read_text", wraps=GUARD._read_text
            ) as read_text:
                self.assertEqual(
                    (6, 1),
                    GUARD.validate_story_request(
                        "6.1",
                        sprint_path,
                        deferred_path,
                        p0_path,
                        workspace_root=workspace,
                    ),
                )

            read_paths = [call.args[0] for call in read_text.call_args_list]
            self.assertEqual(1, read_paths.count(sprint_path))
            self.assertEqual({sprint_path, deferred_path, p0_path}, set(read_paths))

    def test_historical_and_unknown_stories_are_rejected(self) -> None:
        for story_id in ("1.1", "5.12", "9.1", "6.1.2", "epic-6"):
            with self.subTest(story_id=story_id):
                with self.assertRaises(GUARD.GuardViolation):
                    GUARD.assert_story_allowed(story_id)
        with self.assertRaisesRegex(GUARD.GuardViolation, "exactly one"):
            GUARD.validate_story_request("6.999")

    def test_scheduling_authority_and_history_fail_closed(self) -> None:
        mutations = (
            self.replace_once(
                self.sprint,
                "production_authority_epics: [6, 7, 8]",
                "production_authority_epics: [5, 6, 7, 8]",
            ),
            self.replace_once(
                self.sprint,
                "  1-1-module-scaffold-build-ci-wiring: done",
                "  1-1-module-scaffold-build-ci-wiring: backlog",
            ),
            self.sprint + "\nproduction_authority_epics: [6, 7, 8]\n",
        )
        for candidate in mutations:
            with self.subTest(candidate=candidate[-80:]):
                with self.assertRaises(GUARD.GuardViolation):
                    self.validate_boundary(sprint=candidate)

    def test_command_line_exit_codes_enforce_the_boundary(self) -> None:
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

    def test_workflows_and_ci_consume_only_the_minimal_guard(self) -> None:
        project_context = (
            PROJECT_ROOT / "_bmad-output" / "project-context.md"
        ).read_text(encoding="utf-8")
        self.assertIn("validate_production_authority.py --story-id <epic.story>", project_context)
        self.assertIn("--validate-index --sprint-status <candidate>", project_context)

        ci = (PROJECT_ROOT / ".github" / "workflows" / "ci.yml").read_text(
            encoding="utf-8"
        )
        self.assertIn("pyyaml==6.0.3", ci)
        self.assertIn("tests/tools/test_production_authority_guard.py", ci)
        self.assertNotIn("test_p1r_" + "postacceptance_guard.py", ci)


if __name__ == "__main__":
    unittest.main()
