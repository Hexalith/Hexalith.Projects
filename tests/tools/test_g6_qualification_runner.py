"""Prove partial Docker launch failures retain only exact owned container identities."""

from __future__ import annotations

import importlib.util
import json
import types
import subprocess
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch


ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("g6_runner_controls", ROOT / "tools/qualification/run_g6_qualification.py")
RUNNER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(RUNNER)
CONTAINER_ID = "a" * 64
SHARED = {"containers": [{"id": "b" * 64, "name": "dapr_redis"}], "binaries": []}


class QualificationContainerCleanupTests(unittest.TestCase):
    """Launch failure cleanup must preserve the shared-resource snapshot."""

    def check_failed_launch(self, writes_identity: bool) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            scratch = Path(temporary) / "scratch"
            scratch.mkdir()
            with patch.object(RUNNER, "shared_snapshot", return_value=SHARED):
                run = RUNNER.Qualification(Path(temporary), scratch, {})
            commands = []

            def docker(command, **kwargs):
                commands.append(command)
                if command[:2] == ["docker", "run"]:
                    if writes_identity:
                        Path(command[command.index("--cidfile") + 1]).write_text(CONTAINER_ID + "\n")
                    return subprocess.CompletedProcess(command, 125, "", "port publication failed")
                if command[:3] == ["docker", "rm", "-f"]:
                    self.assertEqual(command, ["docker", "rm", "-f", CONTAINER_ID])
                    return subprocess.CompletedProcess(command, 0, "", "")
                self.assertEqual(command, ["docker", "inspect", CONTAINER_ID])
                return subprocess.CompletedProcess(command, 1, "", "container absent")

            with patch.object(RUNNER.subprocess, "run", side_effect=docker), patch.object(
                RUNNER, "shared_snapshot", return_value=SHARED
            ):
                with self.assertRaisesRegex(RuntimeError, "exit 125"):
                    run.start_container("g6-oq8-owned-scheduler", "daprio/dapr:1.18.2", [], 50006, 15006)
                receipt = run.cleanup()

            expected_ids = [CONTAINER_ID] if writes_identity else []
            self.assertEqual([item["id"] for item in receipt["ownedContainers"]], expected_ids)
            self.assertEqual(receipt["removedContainerIds"], expected_ids)
            self.assertEqual(receipt["before"], receipt["after"])
            self.assertTrue(receipt["scratchRemoved"])
            self.assertEqual(len(commands), 3 if writes_identity else 1)

    def test_failed_launch_with_cid_removes_only_that_exact_container(self) -> None:
        self.check_failed_launch(writes_identity=True)

    def test_failed_launch_without_cid_does_not_target_any_container(self) -> None:
        self.check_failed_launch(writes_identity=False)


class QualificationEvidenceRetentionTests(unittest.TestCase):
    def check_process_cleanup(self, disappears: bool, fixture_agrees: bool = True, unverifiable: bool = False):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            scratch = root / "scratch"
            scratch.mkdir()
            with patch.object(RUNNER, "shared_snapshot", return_value=SHARED):
                run = RUNNER.Qualification(root, scratch, {})
            owned_group = 987654
            run.process_groups = [owned_group]
            RUNNER.write(scratch / "fixture-cleanup.json", {"processesStopped": fixture_agrees, "runtimeScratchRemoved": True, "postgresContainerId": CONTAINER_ID})
            signals = []
            probes = 0
            def killpg(group, signal):
                nonlocal probes
                self.assertEqual(group, owned_group)
                signals.append((group, signal))
                if unverifiable:
                    raise PermissionError("group cannot be verified")
                if signal == 0:
                    probes += 1
                    if disappears and probes >= 3:
                        raise ProcessLookupError("owned group disappeared")
            with patch.object(RUNNER.os, "killpg", side_effect=killpg), patch.object(RUNNER.time, "monotonic", side_effect=[0, 1, 3]), patch.object(RUNNER.time, "sleep"), patch.object(RUNNER, "shared_snapshot", return_value=SHARED), patch.object(RUNNER.subprocess, "run", return_value=subprocess.CompletedProcess([], 1)):
                receipt = run.cleanup()
            self.assertIs(receipt["ownedProcessesStopped"], disappears and fixture_agrees and not unverifiable)
            self.assertEqual(receipt["ownedProcessGroups"], [owned_group])
            self.assertEqual(receipt["removedContainerIds"], [CONTAINER_ID])
            self.assertEqual(sum(signal == RUNNER.signal.SIGKILL for _, signal in signals), 0 if unverifiable else 1)

    def test_process_group_disappearance_after_kill_is_proven(self):
        self.check_process_cleanup(disappears=True)

    def test_still_present_process_group_keeps_cleanup_false(self):
        self.check_process_cleanup(disappears=False)

    def test_unverifiable_process_group_keeps_cleanup_false(self):
        self.check_process_cleanup(disappears=False, unverifiable=True)

    def test_fixture_disagreement_keeps_cleanup_false(self):
        self.check_process_cleanup(disappears=True, fixture_agrees=False)

    def test_cleanup_failure_is_retained_even_when_commands_pass(self):
        clean = {"ownedProcessesStopped": True, "scratchRemoved": True, "fixtureScratchRemoved": True, "before": SHARED, "after": SHARED, "ownedContainers": [{"id": CONTAINER_ID}], "removedContainerIds": [CONTAINER_ID]}
        errors = []
        self.assertTrue(RUNNER.retain_cleanup_failure(clean, errors))
        self.assertFalse(errors)
        for field in ("ownedProcessesStopped", "scratchRemoved", "fixtureScratchRemoved"):
            errors = []
            self.assertFalse(RUNNER.retain_cleanup_failure(dict(clean, **{field: False}), errors))
            self.assertTrue(errors)
            self.assertFalse(not errors and all(command["exitCode"] == 0 for command in [{"exitCode": 0}]))

    def test_source_drift_retains_original_snapshot_and_failure(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source = root / "runtime.json"
            source.write_text('{"mode":"original"}')
            baseline = {"consumerInventory": {"roots": ["."]}}
            def manifest(*args):
                return [{"path": "runtime.json", "sha256": RUNNER.API.sha256(source)}]
            repositories = [{"path": ".", "revision": "a" * 40, "rootGitlink": None}]
            with patch.object(RUNNER.CURRENT, "source_manifest", side_effect=manifest), patch.object(RUNNER, "repositories", return_value=repositories):
                snapshot = RUNNER.source_snapshot(baseline)
                retained = root / "source-state.json"
                RUNNER.write(retained, snapshot)
                source.write_text('{"mode":"changed"}')
                errors = []
                self.assertFalse(RUNNER.check_source_snapshot(snapshot, baseline, errors))
                self.assertTrue(errors)
                self.assertEqual(json.loads(retained.read_text()), snapshot)
                self.assertNotEqual(snapshot["files"], manifest())

    def test_failed_support_keeps_actual_counts_and_safe_identities(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            methods = {"Fixture.Support.One": 1, "Fixture.Support.Two": 1}
            ctrf = root / "raw.json"
            RUNNER.write(ctrf, {"results": {"summary": {"tests": 2, "passed": 0, "failed": 1, "skipped": 1}, "tests": [
                {"name": "Fixture.Support.One(protected-argument-marker)", "status": "failed", "message": "protected-output-marker"},
                {"name": "Fixture.Support.Two", "status": "skipped", "output": "protected-output-marker"}]}})
            oq8 = types.SimpleNamespace(EXPECTED_SUPPORT_METHOD_CASES=methods, SUPPORT_CURRENT_COMMAND="dotnet /Release/support.dll", expected_support_classifications=lambda: {})
            destination = root / "safe.json"
            RUNNER.retain_support_results(ctrf, destination, oq8)
            document = json.loads(destination.read_text())
            self.assertEqual(document["summary"], {"tests": 2, "passed": 0, "failed": 1, "skipped": 1})
            self.assertEqual(document["methods"][0]["failedCases"], 1)
            self.assertEqual(document["methods"][1]["skippedCases"], 1)
            self.assertEqual(document["selectors"], list(methods))
            self.assertNotIn("protected-", destination.read_text())

    def test_failed_fresh_build_never_executes_fixture_dll(self):
        run = types.SimpleNamespace(run=lambda purpose, command, cwd: commands.append(command) or 1)
        commands = []
        self.assertEqual(RUNNER.run_fixture_controls(run, 1, Path("stale.dll")), 1)
        self.assertEqual(commands[0][:2], ["python3", "-c"])
        self.assertNotIn("stale.dll", " ".join(commands[0]))

    def test_actual_assets_retain_transitive_families_and_exact_preview_exclusions(self):
        baseline = RUNNER.API.read_json(RUNNER.BUILDS / "Tools/runtime-toolchain-baseline-2026-10-01.json")
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            project = root / "src/AppHost.csproj"
            project.parent.mkdir()
            project.write_text('<Project Sdk="Aspire.AppHost.Sdk/13.6.0"><ItemGroup><PackageReference Include="CommunityToolkit.Aspire.Hosting.Dapr" /></ItemGroup></Project>')
            assets = project.parent / "obj/project.assets.json"
            assets.parent.mkdir()
            libraries = {identity: {"type": "package"} for identity in ["Aspire.Hosting.AppHost/13.6.0", "CommunityToolkit.Aspire.Hosting.Dapr/13.6.0-beta.910", "Dapr.Client/1.18.10", "Dapr.Actors/1.18.10", "Aspire.Hosting.Keycloak/13.6.0-preview.1.26479.8"]}
            RUNNER.write(assets, {"libraries": libraries})
            with patch.object(RUNNER, "ROOT", root):
                rows = RUNNER.resolved(["src/AppHost.csproj"], baseline, root)
                packages = {item["id"]: item for item in rows[0]["packages"]}
                self.assertIn("Dapr.Actors", packages)
                self.assertFalse(packages["Aspire.Hosting.Keycloak"]["qualified"])
                for original, replacement in [("Aspire.Hosting.Keycloak/13.6.0-preview.1.26479.8", "Aspire.Hosting.Keycloak/13.5.4-preview.1.26464.4"), ("Dapr.Actors/1.18.10", "Dapr.Actors/1.18.9")]:
                    RUNNER.write(assets, {"libraries": {(replacement if key == original else key): value for key, value in libraries.items()}})
                    with self.assertRaisesRegex(RUNNER.API.ValidationError, "Resolved package drift"):
                        RUNNER.resolved(["src/AppHost.csproj"], baseline, root)
                for omitted in ["Aspire.Hosting.AppHost/13.6.0", "CommunityToolkit.Aspire.Hosting.Dapr/13.6.0-beta.910"]:
                    RUNNER.write(assets, {"libraries": {key: value for key, value in libraries.items() if key != omitted}})
                    with self.assertRaisesRegex(RUNNER.API.ValidationError, "Required resolved dependencies"):
                        RUNNER.resolved(["src/AppHost.csproj"], baseline, root)

    def test_actual_mcp_assets_require_transitive_dapr_client(self):
        baseline = RUNNER.API.read_json(RUNNER.BUILDS / "Tools/runtime-toolchain-baseline-2026-10-01.json")
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            relative = "references/Hexalith.McpCli/src/Hexalith.McpCli/Hexalith.McpCli.csproj"
            project = root / relative
            project.parent.mkdir(parents=True)
            project.write_text('<Project />')
            assets = project.parent / "obj/project.assets.json"
            RUNNER.write(assets, {"libraries": {"Dapr.Actors/1.18.10": {"type": "package"}}})
            with patch.object(RUNNER, "ROOT", root):
                with self.assertRaisesRegex(RUNNER.API.ValidationError, "Required resolved dependencies"):
                    RUNNER.resolved([relative], baseline, root)
                RUNNER.write(assets, {"libraries": {identity: {"type": "package"} for identity in ["Dapr.Actors/1.18.10", "Dapr.Client/1.18.10"]}})
                self.assertEqual(len(RUNNER.resolved([relative], baseline, root)[0]["packages"]), 2)


if __name__ == "__main__":
    unittest.main()
