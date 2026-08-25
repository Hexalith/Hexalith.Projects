"""Subprocess tests for the portable, filename-safe BMAD Loop hook relay."""

from __future__ import annotations

import json
import os
import shutil
import stat
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[2]
RELAY_PATH = PROJECT_ROOT / ".bmad-loop" / "bmad_loop_hook.py"
HOOKS_PATH = PROJECT_ROOT / ".codex" / "hooks.json"
CI_PATH = PROJECT_ROOT / ".github" / "workflows" / "ci.yml"


class BmadLoopHookTests(unittest.TestCase):
    """Exercise the installed relay only through its command-line boundary."""

    def run_relay(
        self,
        *,
        run_dir: Path | str | None,
        task_id: str | None,
        event_name: str = "Stop",
        payload: object | str | None = None,
        events_dir: Path | str | None = None,
        include_events_dir: bool = False,
        cwd: Path = PROJECT_ROOT,
        relay_path: Path = RELAY_PATH,
    ) -> subprocess.CompletedProcess[str]:
        environment = {"PYTHONDONTWRITEBYTECODE": "1"}
        if run_dir is not None:
            environment["BMAD_LOOP_RUN_DIR"] = str(run_dir)
        if task_id is not None:
            environment["BMAD_LOOP_TASK_ID"] = task_id
        if include_events_dir:
            environment["BMAD_LOOP_EVENTS_DIR"] = "" if events_dir is None else str(events_dir)
        if isinstance(payload, str):
            input_text = payload
        else:
            input_text = json.dumps({} if payload is None else payload)
        return subprocess.run(
            [sys.executable, str(relay_path), event_name],
            cwd=cwd,
            env=environment,
            input=input_text,
            capture_output=True,
            text=True,
            check=False,
        )

    def read_single_event(self, events_dir: Path) -> tuple[Path, dict[str, object]]:
        event_files = list(events_dir.glob("*.json"))
        self.assertEqual(1, len(event_files), event_files)
        return event_files[0], json.loads(event_files[0].read_text(encoding="utf-8"))

    def test_missing_or_empty_loop_attribution_is_a_silent_no_op(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for name, run_dir, task_id in (
                ("missing run", None, "task-1"),
                ("empty run", "", "task-1"),
                ("missing task", root / "missing-task-run", None),
                ("empty task", root / "empty-task-run", ""),
            ):
                with self.subTest(name=name):
                    events_dir = root / name.replace(" ", "-") / "events"
                    completed = self.run_relay(
                        run_dir=run_dir,
                        task_id=task_id,
                        events_dir=events_dir,
                        include_events_dir=True,
                    )
                    self.assertEqual(0, completed.returncode, completed.stderr)
                    self.assertEqual("", completed.stdout)
                    self.assertEqual("", completed.stderr)
                    self.assertFalse(events_dir.exists())

    def test_explicit_events_directory_is_preferred_and_empty_or_absent_falls_back(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for name, include_events_dir, configured_events_dir in (
                ("explicit", True, root / "explicit-events"),
                ("empty", True, None),
                ("absent", False, None),
            ):
                with self.subTest(name=name):
                    run_dir = root / f"run-{name}"
                    expected_events_dir = (
                        configured_events_dir
                        if configured_events_dir is not None
                        else run_dir / "events"
                    )
                    completed = self.run_relay(
                        run_dir=run_dir,
                        task_id=f"task-{name}",
                        payload={"session_id": f"session-{name}"},
                        events_dir=configured_events_dir,
                        include_events_dir=include_events_dir,
                    )
                    self.assertEqual(0, completed.returncode, completed.stderr)
                    _, event = self.read_single_event(expected_events_dir)
                    self.assertEqual(f"task-{name}", event["task_id"])
                    if configured_events_dir is not None:
                        self.assertFalse((run_dir / "events").exists())

    def test_filesystem_refusal_is_a_silent_no_op(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            refused_events_dir = root / "not-a-directory"
            refused_events_dir.write_text("occupied", encoding="utf-8")
            completed = self.run_relay(
                run_dir=root / "run",
                task_id="task-refused",
                events_dir=refused_events_dir,
                include_events_dir=True,
            )
            self.assertEqual(0, completed.returncode, completed.stderr)
            self.assertEqual("occupied", refused_events_dir.read_text(encoding="utf-8"))
            self.assertEqual([], list(root.rglob("*.json")))

    def test_payload_key_variants_are_emitted_in_the_canonical_shape(self) -> None:
        cases = (
            (
                "snake-case",
                {
                    "session_id": "session-snake",
                    "transcript_path": "transcript-snake",
                    "cwd": "cwd-snake",
                },
                ("session-snake", "transcript-snake", "cwd-snake"),
            ),
            (
                "conversation-id",
                {
                    "conversation_id": "session-conversation",
                    "transcript_path": "transcript-conversation",
                    "cwd": "cwd-conversation",
                },
                ("session-conversation", "transcript-conversation", "cwd-conversation"),
            ),
            (
                "camel-case",
                {
                    "sessionId": "session-camel",
                    "transcriptPath": "transcript-camel",
                    "cwd": "cwd-camel",
                },
                ("session-camel", "transcript-camel", "cwd-camel"),
            ),
            (
                "workspace-paths",
                {
                    "conversationId": "session-workspace",
                    "transcriptPath": "transcript-workspace",
                    "workspacePaths": ["cwd-workspace", "ignored"],
                },
                ("session-workspace", "transcript-workspace", "cwd-workspace"),
            ),
        )
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for name, payload, expected in cases:
                with self.subTest(name=name):
                    events_dir = root / name
                    completed = self.run_relay(
                        run_dir=root / f"run-{name}",
                        task_id=f"task-{name}",
                        payload=payload,
                        events_dir=events_dir,
                        include_events_dir=True,
                    )
                    self.assertEqual(0, completed.returncode, completed.stderr)
                    _, event = self.read_single_event(events_dir)
                    self.assertEqual(expected[0], event["session_id"])
                    self.assertEqual(expected[1], event["transcript_path"])
                    self.assertEqual(expected[2], event["cwd"])

    def test_malformed_and_non_object_payloads_emit_null_optional_fields(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for name, payload in (("malformed", "{"), ("array", ["not", "an", "object"])):
                with self.subTest(name=name):
                    events_dir = root / name
                    completed = self.run_relay(
                        run_dir=root / f"run-{name}",
                        task_id=f"task-{name}",
                        payload=payload,
                        events_dir=events_dir,
                        include_events_dir=True,
                    )
                    self.assertEqual(0, completed.returncode, completed.stderr)
                    _, event = self.read_single_event(events_dir)
                    self.assertIsNone(event["session_id"])
                    self.assertIsNone(event["transcript_path"])
                    self.assertIsNone(event["cwd"])

    def test_safe_task_and_event_values_remain_unchanged(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            events_dir = root / "events"
            task_id = "Task.1_safe-value"
            event_name = "SessionStart-1_safe.event"
            completed = self.run_relay(
                run_dir=root / "run",
                task_id=task_id,
                event_name=event_name,
                events_dir=events_dir,
                include_events_dir=True,
            )
            self.assertEqual(0, completed.returncode, completed.stderr)
            event_path, event = self.read_single_event(events_dir)
            self.assertTrue(event_path.name.endswith(f"-{task_id}-{event_name}.json"))
            self.assertEqual(task_id, event["task_id"])
            self.assertEqual(event_name, event["event"])

    def test_unsafe_task_and_event_values_publish_nothing(self) -> None:
        invalid_task_ids = (
            ".",
            "..",
            "../escape",
            "task/escape",
            "task\\escape",
            "task\ncontrol",
            "-leading",
            ".leading",
            "trailing.",
            "white space",
            "tâsk",
            "t" * 161,
        )
        invalid_event_names = (
            "",
            ".",
            "..",
            "../Stop",
            "Session/Stop",
            "Session\\Stop",
            "Stop\tControl",
            "_leading",
            ".leading",
            "trailing.",
            "white space",
            "Stöp",
            "S" * 65,
        )
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for index, task_id in enumerate(invalid_task_ids):
                with self.subTest(kind="task", value=task_id):
                    case_root = root / f"task-{index}"
                    events_dir = case_root / "run" / "events"
                    completed = self.run_relay(
                        run_dir=case_root / "run",
                        task_id=task_id,
                    )
                    self.assertEqual(0, completed.returncode, completed.stderr)
                    self.assertFalse(events_dir.exists())
                    self.assertEqual([], list(case_root.rglob("*.json")))

            for index, event_name in enumerate(invalid_event_names):
                with self.subTest(kind="event", value=event_name):
                    case_root = root / f"event-{index}"
                    events_dir = case_root / "run" / "events"
                    completed = self.run_relay(
                        run_dir=case_root / "run",
                        task_id="task-safe",
                        event_name=event_name,
                    )
                    self.assertEqual(0, completed.returncode, completed.stderr)
                    self.assertFalse(events_dir.exists())
                    self.assertEqual([], list(case_root.rglob("*.json")))

    def test_successful_publication_is_atomic_complete_and_restricted(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            events_dir = root / "events"
            completed = self.run_relay(
                run_dir=root / "run",
                task_id="task-atomic",
                payload={"session_id": "session-atomic"},
                events_dir=events_dir,
                include_events_dir=True,
            )
            self.assertEqual(0, completed.returncode, completed.stderr)
            event_path, event = self.read_single_event(events_dir)
            self.assertEqual("session-atomic", event["session_id"])
            self.assertEqual([], list(events_dir.glob("*.tmp")))
            if os.name == "posix":
                self.assertEqual(0o600, stat.S_IMODE(event_path.stat().st_mode))

    def test_maximum_component_lengths_publish_without_temporary_residue(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            events_dir = root / "events"
            task_id = "t" * 160
            event_name = "E" * 64
            completed = self.run_relay(
                run_dir=root / "run",
                task_id=task_id,
                event_name=event_name,
                events_dir=events_dir,
                include_events_dir=True,
            )
            self.assertEqual(0, completed.returncode, completed.stderr)
            event_path, event = self.read_single_event(events_dir)
            self.assertLessEqual(len(event_path.name + ".tmp"), 255)
            self.assertEqual(task_id, event["task_id"])
            self.assertEqual(event_name, event["event"])
            self.assertEqual([], list(events_dir.glob("*.tmp")))

    def test_relocated_hook_commands_resolve_the_copied_checkout_from_nested_cwd(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / "relocated checkout"
            relay = root / ".bmad-loop" / "bmad_loop_hook.py"
            hooks_path = root / ".codex" / "hooks.json"
            relay.parent.mkdir(parents=True)
            hooks_path.parent.mkdir(parents=True)
            shutil.copy2(RELAY_PATH, relay)
            shutil.copy2(HOOKS_PATH, hooks_path)
            hooks = json.loads(hooks_path.read_text(encoding="utf-8"))
            nested_cwd = root / "session" / "nested"
            nested_cwd.mkdir(parents=True)
            initialized = subprocess.run(
                ["git", "init", "--quiet", str(root)],
                env={"PATH": os.environ.get("PATH", "")},
                capture_output=True,
                text=True,
                check=False,
            )
            self.assertEqual(0, initialized.returncode, initialized.stderr)

            for event_name in ("Stop", "SessionStart"):
                with self.subTest(event=event_name):
                    commands = [
                        hook["command"]
                        for group in hooks["hooks"][event_name]
                        for hook in group["hooks"]
                        if "bmad_loop_hook.py" in hook.get("command", "")
                    ]
                    self.assertEqual(1, len(commands), commands)
                    self.assertNotIn(str(PROJECT_ROOT), commands[0])
                    events_dir = root / f"events-{event_name}"
                    completed = subprocess.run(
                        commands[0],
                        cwd=nested_cwd,
                        env={
                            "PATH": os.environ.get("PATH", ""),
                            "PYTHONDONTWRITEBYTECODE": "1",
                            "BMAD_LOOP_RUN_DIR": str(root / "run"),
                            "BMAD_LOOP_EVENTS_DIR": str(events_dir),
                            "BMAD_LOOP_TASK_ID": f"task-{event_name}",
                        },
                        input=json.dumps({"session_id": f"session-{event_name}"}),
                        capture_output=True,
                        text=True,
                        shell=True,
                        check=False,
                    )
                    self.assertEqual(0, completed.returncode, completed.stderr)
                    _, event = self.read_single_event(events_dir)
                    self.assertEqual(event_name, event["event"])
                    self.assertEqual(f"task-{event_name}", event["task_id"])

    def test_ci_runs_this_dependency_free_suite_as_a_blocking_workflow_gate(self) -> None:
        workflow = CI_PATH.read_text(encoding="utf-8")
        command = "python3 -m unittest tests/tools/test_bmad_loop_hook.py -v"
        self.assertIn(command, workflow)
        self.assertLess(workflow.index(command), workflow.index("Validate CI/CD invariants"))
        workflow_gates = workflow.split("  ci:\n", maxsplit=1)[0]
        self.assertNotIn("continue-on-error", workflow_gates)
        self.assertIn("PYTHONDONTWRITEBYTECODE: '1'", workflow_gates)


if __name__ == "__main__":
    unittest.main()
