"""The current runner must reject an impossible tuple before starting Docker."""

from __future__ import annotations

import importlib.util
import json
import sys
import tempfile
import types
import unittest
from pathlib import Path
from unittest.mock import patch


ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "tools/qualification/run_g6_current.py"
SPEC = importlib.util.spec_from_file_location("g6_current_runner", SCRIPT)
assert SPEC is not None and SPEC.loader is not None
sys.path.insert(0, str(SCRIPT.parent))
RUNNER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(RUNNER)


class CurrentRunnerPreflightTests(unittest.TestCase):
    def test_default_policy_matches_ci_and_explicit_override_is_preserved(self) -> None:
        selected_policy = ROOT / "references/Hexalith.Builds/Tools/g6-current-policy-2026-10-20.json"
        (ROOT / ".g6-current-evidence").mkdir(exist_ok=True)
        with tempfile.TemporaryDirectory(dir=ROOT / ".g6-current-evidence") as temporary:
            root = Path(temporary)
            custom_policy = root / "custom-policy.json"
            custom_policy.write_text("{}", encoding="utf-8")
            for name, policy_arguments, expected in (
                ("default", [], selected_policy),
                ("explicit", ["--policy", str(custom_policy)], custom_policy),
            ):
                with self.subTest(name=name), \
                     patch.object(RUNNER, "audit", side_effect=RuntimeError("controlled audit stop")) as audit, \
                     patch.object(RUNNER.historical, "Qualification") as qualification:
                    with self.assertRaisesRegex(RuntimeError, "controlled audit stop"):
                        RUNNER.main(["--output", str(root / name), *policy_arguments])
                    self.assertEqual(expected.resolve(), audit.call_args.args[1])
                    qualification.assert_not_called()

    def test_mismatched_tuple_retains_not_verified_without_starting_resources(self) -> None:
        (ROOT / ".g6-current-evidence").mkdir(exist_ok=True)
        with tempfile.TemporaryDirectory(dir=ROOT / ".g6-current-evidence") as temporary:
            root = Path(temporary)
            policy = root / "policy.json"
            policy.write_text(json.dumps({"tuple": {}, "approval": {"decision": "pending"}}), encoding="utf-8")
            result_dir = root / "result"
            audit = {"issues": ["controlled tuple changed: communityToolkitAspireDapr preview != beta"],
                     "source": {"rootSha": "a" * 40, "gitlinks": []}}
            with patch.object(RUNNER, "audit", return_value=audit), \
                 patch.object(RUNNER.CURRENT_API, "builds_execution_sha", return_value="b" * 40), \
                 patch.object(RUNNER.historical, "Qualification", side_effect=AssertionError("Docker setup started")):
                result = RUNNER.main(["--policy", str(policy), "--output", str(result_dir)])
            self.assertEqual(1, result)
            receipt = json.loads((result_dir / "result.json").read_text(encoding="utf-8"))
            self.assertEqual("not verified", receipt["qualification"]["status"])
            self.assertEqual([], receipt["qualification"]["commands"])
            self.assertEqual("ci", receipt["qualification"]["environment"]["executionScope"])
            self.assertEqual("b" * 40, receipt["qualification"]["environment"]["buildsExecutionSha"])

    def test_failed_install_retains_normalized_build_controls_without_caller_secrets(self) -> None:
        (ROOT / ".g6-current-evidence").mkdir(exist_ok=True)
        with tempfile.TemporaryDirectory(dir=ROOT / ".g6-current-evidence") as temporary:
            root = Path(temporary)
            policy = root / "policy.json"
            policy.write_text(json.dumps({"tuple": {"daprRuntime": "1.18.2"}, "approval": {"decision": "approved"}}))
            audit = {"issues": [], "tupleApproved": True, "source": {}, "materialInputs": {"fingerprint": "a" * 64}}
            output = root / "result"
            with patch.dict(RUNNER.os.environ, {"ci": "true", "idebuild": "true", "VSCODE_PID": "123", "CALLER_SECRET": "protected-marker"}), \
                 patch.object(RUNNER, "audit", return_value=audit), \
                 patch.object(RUNNER.CURRENT_API, "builds_execution_sha", return_value="b" * 40), \
                 patch.object(RUNNER.tempfile, "mkdtemp", return_value=str(root / "scratch")), \
                 patch.object(RUNNER.historical, "shared_snapshot", return_value={}), \
                 patch.object(RUNNER.historical.Qualification, "cleanup", return_value={}), \
                 patch.object(RUNNER.historical, "install", side_effect=RuntimeError("install failed")) as install:
                self.assertEqual(1, RUNNER.main(["--policy", str(policy), "--output", str(output)]))
            environment = install.call_args.kwargs["environment"]
            self.assertEqual("true", environment["GITHUB_ACTIONS"])
            self.assertFalse(any(name.upper() in {"CI", "IDEBUILD"} or name.upper().startswith("VSCODE_") for name in environment))
            result_text = (output / "result.json").read_text()
            receipt = json.loads(result_text)
            self.assertEqual("failed", receipt["qualification"]["status"])
            self.assertTrue(all(value is False for value in receipt["qualification"]["cleanup"].values()))
            self.assertEqual("true", receipt["qualification"]["environment"]["buildControls"]["GITHUB_ACTIONS"])
            self.assertIsNone(receipt["qualification"]["environment"]["buildControls"]["CI"])
            self.assertNotIn("CALLER_SECRET", result_text)
            self.assertNotIn("protected-marker", result_text)

    def test_pending_selected_tuple_can_reach_live_runner_despite_consumer_pin_drift(self) -> None:
        (ROOT / ".g6-current-evidence").mkdir(exist_ok=True)
        with tempfile.TemporaryDirectory(dir=ROOT / ".g6-current-evidence") as temporary:
            root = Path(temporary)
            policy = root / "policy.json"
            tuple_values = {"communityToolkitAspireDapr": "preview"}
            policy.write_text(json.dumps({"tuple": tuple_values, "approval": {"decision": "pending"}}), encoding="utf-8")
            audit = {"effectiveTuple": tuple_values,
                     "issues": ["Platform file host pin differs from effective tuple: beta"]}
            with patch.object(RUNNER, "audit", return_value=audit), \
                 patch.object(RUNNER.CURRENT_API, "builds_execution_sha", return_value="b" * 40), \
                 patch.object(RUNNER.tempfile, "mkdtemp", return_value=str(root / "scratch")), \
                 patch.object(RUNNER.historical, "Qualification", side_effect=RuntimeError("live setup reached")):
                with self.assertRaisesRegex(RuntimeError, "live setup reached"):
                    RUNNER.main(["--policy", str(policy), "--output", str(root / "result")])


class CurrentRunnerImageTests(unittest.TestCase):
    IMAGE = "daprio/dapr@sha256:1146cf75f6a900f098489f995bcb2145bf9b040f87ab70eb0de2db21000d01bb"

    def control_plane_launches(self, runner=RUNNER):
        """Reach both real launch call sites without downloading tools or starting Docker."""
        (ROOT / ".g6-current-evidence").mkdir(exist_ok=True)
        with tempfile.TemporaryDirectory(dir=ROOT / ".g6-current-evidence") as temporary:
            root = Path(temporary)
            scratch = root / "scratch"
            (scratch / "runtime").mkdir(parents=True)
            (scratch / "runtime/daprd").touch()
            policy = root / "policy.json"
            policy.write_text(json.dumps({"tuple": {"daprRuntime": "1.18.2"},
                                          "approval": {"decision": "approved"}}))
            audit = {"issues": [], "tupleApproved": True, "source": {},
                     "materialInputs": {"fingerprint": "a" * 64}}
            with patch.object(runner, "audit", return_value=audit), \
                 patch.object(runner.CURRENT_API, "builds_execution_sha", return_value="b" * 40), \
                 patch.object(runner.tempfile, "mkdtemp", return_value=str(scratch)), \
                 patch.object(runner.historical, "install"), \
                 patch.object(runner.historical, "Qualification") as qualification:
                run = qualification.return_value
                run.env = {"PATH": "/controlled-tools"}
                run.commands = []
                run.run.return_value = 1
                run.sanitize.side_effect = str
                run.cleanup.return_value = {}
                run.build_controls.return_value = {}
                run.start_container.side_effect = [("placement-id", 15005), ("scheduler-id", 15006),
                                                   RuntimeError("controlled stop after control-plane launches")]
                self.assertEqual(1, runner.main(["--policy", str(policy), "--output", str(root / "output")]))
                self.assertEqual(3, run.start_container.call_count)
                calls = [call.args for call in run.start_container.call_args_list[:2]]
            self.assertEqual(["./placement", "./scheduler"], [call[2][0] for call in calls])
            self.assertEqual([50005, 50006], [call[3] for call in calls])
            self.assertTrue(calls[0][0].endswith("-placement"))
            self.assertTrue(calls[1][0].endswith("-scheduler"))
            return [call[1] for call in calls]

    def test_both_control_plane_launches_use_the_approved_manifest(self) -> None:
        self.assertEqual([self.IMAGE, self.IMAGE], self.control_plane_launches())

    def test_mutable_tag_substitution_is_detected_at_the_launch_call_sites(self) -> None:
        source = SCRIPT.read_text(encoding="utf-8")
        self.assertEqual(1, source.count(self.IMAGE))
        mutated = types.ModuleType("g6_mutable_image_negative_control")
        exec(compile(source.replace(self.IMAGE, "daprio/dapr:1.18.2"), str(SCRIPT), "exec"), mutated.__dict__)
        images = self.control_plane_launches(mutated)
        self.assertEqual(["daprio/dapr:1.18.2"] * 2, images)
        self.assertNotEqual([self.IMAGE] * 2, images)


class SupportSelectorInventoryTests(unittest.TestCase):
    def test_empty_workflow_uses_the_retained_oracle_and_rejects_drift(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            eventstore = Path(temporary)
            workflow = eventstore / ".github/workflows/integration.yml"
            workflow.parent.mkdir(parents=True)
            workflow.write_text("name: Integration Tests\n", encoding="utf-8")
            oracle = eventstore / "tests/Hexalith.EventStore.Server.LiveSidecar.Tests/Actors/IdempotencyAdmissionOq8PostgresqlTests.cs"
            oracle.parent.mkdir(parents=True)
            names = [f"Hexalith.EventStore.Server.Tests.Sample.Method{index:02d}" for index in range(21)]
            body = ",\n".join(f'                "{name}"' for name in names)
            oracle.write_text(
                "deterministicSupportOracles = new[]\n            {\n" + body + "\n            },\n",
                encoding="utf-8")
            self.assertEqual(names, RUNNER.support_selectors(eventstore))
            oracle.write_text(oracle.read_text(encoding="utf-8").replace(names[-1], names[0]), encoding="utf-8")
            drifted = RUNNER.support_selectors(eventstore)
            self.assertNotEqual(21, len(set(drifted)))


if __name__ == "__main__":
    unittest.main()
