"""The current runner must reject an impossible tuple before starting Docker."""

from __future__ import annotations

import importlib.util
import json
import sys
import tempfile
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


if __name__ == "__main__":
    unittest.main()
