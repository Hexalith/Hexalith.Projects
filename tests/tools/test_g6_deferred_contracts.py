"""Keep five superseded G-6 obligations covered by the real v2 contracts."""

from __future__ import annotations

import copy
import importlib.util
import json
import subprocess
import tempfile
import unittest
from contextlib import contextmanager
from pathlib import Path
from unittest.mock import patch


ROOT = Path(__file__).resolve().parents[2]
TOOLS = ROOT / "references/Hexalith.Builds/Tools"
SPEC = importlib.util.spec_from_file_location("g6_deferred_fixture", TOOLS / "test_runtime_toolchain_v2.py")
assert SPEC is not None and SPEC.loader is not None
FIXTURE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(FIXTURE)
CURRENT = FIXTURE.CURRENT
API = FIXTURE.API

# Test-owned expectations must not shrink when the validator's classifications do.
BOUND_ARTIFACTS = (
    "limitations", "source-state", "apphost-outcomes", "resolved-packages", "consumer-audit", "cleanup",
)
LEGACY_COMMANDS = (
    "managed restart smoke credential preflight",
    "broad package-exception inventory outside G-6 affected set",
)
RESTART = "real PostgreSQL two-sidecar stop/restart qualifier"
PACKAGE_VERSIONS = {
    "Dapr.Actors": "1.18.10",
    "Dapr.Actors.AspNetCore": "1.18.10",
    "Dapr.AspNetCore": "1.18.10",
    "Aspire.Hosting": "13.6.0",
    "Aspire.Hosting.AppHost": "13.6.0",
    "Hexalith.EventStore.Contracts": "3.110.0",
    "Hexalith.EventStore.Client": "3.110.0",
}


@contextmanager
def restored_files(workspace: Path, packet_path: Path, packet: dict):
    """Restore exact fixture bytes even when a subtest fails unexpectedly."""
    paths = {packet_path, *(workspace / item["path"] for item in packet["artifacts"])}
    originals = {path: path.read_bytes() for path in paths}
    try:
        yield
    finally:
        for path, data in originals.items():
            path.write_bytes(data)


def edit_artifact(workspace: Path, packet: dict, kind: str, edit) -> None:
    binding = next(item for item in packet["artifacts"] if item["kind"] == kind)
    path = workspace / binding["path"]
    document = API.read_json(path)
    edit(document)
    FIXTURE.write(path, document)
    binding["sha256"] = API.sha256(path)


class DeferredG6ContractTests(unittest.TestCase):
    def test_packet_obligations_reach_real_candidate_and_accepted_validation(self) -> None:
        real_validate = CURRENT.validate_packet
        observed = set()

        def observe(api, workspace, packet_path, baseline_path, baseline, candidate):
            # Existing fixture negatives must still reach its own real rejection checks.
            result = real_validate(api, workspace, packet_path, baseline_path, baseline, candidate)
            if candidate in observed:
                return result
            observed.add(candidate)
            packet = API.read_json(packet_path)

            def check(label, mutate, expected=None):
                # Capture unexpected errors here so the outer fixture cannot count them
                # as successful existing negative controls.
                with self.subTest(mode="candidate" if candidate else "accepted", case=label):
                    with restored_files(workspace, packet_path, packet):
                        changed = copy.deepcopy(packet)
                        mutate(changed)
                        FIXTURE.write(packet_path, changed)
                        if expected is None:
                            real_validate(api, workspace, packet_path, baseline_path, baseline, candidate)
                        else:
                            with self.assertRaisesRegex(api.ValidationError, expected):
                                real_validate(api, workspace, packet_path, baseline_path, baseline, candidate)

            if candidate:
                self.check_candidate_obligations(workspace, check)
            else:
                self.check_named_decisions(check)
            return result

        with patch.object(CURRENT, "validate_packet", side_effect=observe):
            self.assertGreater(FIXTURE.run_controls(), 0)
        self.assertEqual({True, False}, observed, "both real positive fixture modes must run")

    def check_candidate_obligations(self, workspace, check) -> None:
        for kind in BOUND_ARTIFACTS:
            def stale_bytes(packet, kind=kind):
                binding = next(item for item in packet["artifacts"] if item["kind"] == kind)
                path = workspace / binding["path"]
                path.write_bytes(path.read_bytes() + b"\n")

            check(f"{kind}: changed bytes", stale_bytes, "Current artifact hash mismatch")
            check(f"{kind}: omitted binding", lambda packet, kind=kind: packet.update(
                artifacts=[item for item in packet["artifacts"] if item["kind"] != kind]),
                "Current artifact set is incomplete")

        def changed_pin_audit(packet):
            binding = next(item for item in packet["artifacts"] if item["kind"] == "baseline-governance")
            path = workspace / binding["path"]
            document = API.read_json(path)
            document["pinAudit"]["appHostProjects"][0]["version"] = "9.9.9"
            FIXTURE.write(path, document)

        check("baseline pin audit: changed bytes", changed_pin_audit, "Current baseline hash mismatch")

        for purpose in LEGACY_COMMANDS:
            def append_command(packet, exit_code=0, purpose=purpose):
                def append(document):
                    command = dict(document["commands"][0], purpose=purpose,
                                   command="python3 fixture_probe.py", exitCode=exit_code,
                                   outcome="passed" if exit_code == 0 else "failed")
                    document["commands"].append(command)
                edit_artifact(workspace, packet, "command-record", append)

            check(f"{purpose}: success is permitted", append_command)
            check(f"{purpose}: failure contradicts claimed success", lambda packet, append=append_command: append(packet, 1),
                  "Technical validity contradicts actual command outcomes")

            def consistent_failure(packet, append=append_command):
                append(packet, 1)
                packet["technicalValidity"] = False

            check(f"{purpose}: consistent failed result cannot qualify", consistent_failure,
                  "Current technical qualification failed")

        check("restart qualifier remains required", lambda packet: edit_artifact(
            workspace, packet, "command-record", lambda document: document.update(
                commands=[item for item in document["commands"] if item["purpose"] != RESTART])),
            "Required qualification command is missing")

        def failed_restart(packet):
            edit_artifact(workspace, packet, "command-record", lambda document: next(
                item for item in document["commands"] if item["purpose"] == RESTART).update(exitCode=1))
            edit_artifact(workspace, packet, "attempts", lambda document: document["attempts"][-1].update(exitCode=1))

        check("failed restart contradicts claimed success", failed_restart,
              "Technical validity contradicts actual command outcomes")

        def consistent_restart_failure(packet):
            failed_restart(packet)
            packet["technicalValidity"] = False

        check("consistent failed restart cannot qualify", consistent_restart_failure,
              "Current technical qualification failed")

    def check_named_decisions(self, check) -> None:
        cases = (
            ("bare accepted status", lambda packet: packet.update(acceptance=None), "Named acceptance"),
            ("unknown approver", lambda packet: packet["acceptance"].update(approvedBy="Unknown Owner"),
             "Named packet decision is unapproved"),
            ("wrong decision", lambda packet: packet["acceptance"].update(decision="reject"),
             "Named packet decision is unapproved"),
            ("decision before capture", lambda packet: packet["acceptance"].update(approvedAtUtc="2026-10-01T11:59:59Z"),
             "Named decision does not bind this reviewed packet"),
            ("wrong reviewed hash", lambda packet: packet["acceptance"].update(reviewedPacketSha256="0" * 64),
             "Named decision does not bind this reviewed packet"),
            ("accepted but unusable", lambda packet: packet.update(usableAsPrerequisite=False),
             "Accepted packet usability contradicts acceptance"),
            ("missing owner role", lambda packet: packet["approval"]["ownerRoles"].pop(), "Run approval mismatch"),
            ("unknown owner role", lambda packet: packet["approval"].update(ownerRoles=["Unknown Owner"]),
             "Run approval mismatch"),
        )
        for label, mutate, expected in cases:
            check(label, mutate, expected)

    def test_real_inventory_checks_each_test_owned_package_family(self) -> None:
        baseline = {
            "tuple": {"daprDotnetPackages": "1.18.10", "aspireSdk": "13.6.0"},
            "qualification": {"eventStorePackageVersion": "3.110.0"},
            "consumerInventory": {"roots": [".", "references/Hexalith.Builds"],
                                  "unqualifiedExclusions": [], "fileBasedAppHosts": []},
        }
        with tempfile.TemporaryDirectory(prefix="g6-deferred-packages-") as temporary:
            workspace = Path(temporary)
            builds = workspace / "references/Hexalith.Builds"
            catalog_relative = "references/Hexalith.Builds/Props/Directory.Packages.props"
            catalog = workspace / catalog_relative
            catalog.parent.mkdir(parents=True)
            (workspace / ".gitmodules").write_text(
                '[submodule "Builds"]\n path = references/Hexalith.Builds\n'
                ' url = https://example.invalid/Hexalith.Builds.git\n', encoding="utf-8")
            project = workspace / "Consumer.csproj"
            project.write_text('<Project><ItemGroup>' + ''.join(
                f'<PackageReference Include="{package}" />' for package in PACKAGE_VERSIONS)
                + '</ItemGroup></Project>', encoding="utf-8")
            subprocess.run(["git", "init", "--quiet", str(workspace)], check=True, capture_output=True)
            subprocess.run(["git", "-C", str(workspace), "add", "Consumer.csproj", ".gitmodules"],
                           check=True, capture_output=True)
            subprocess.run(["git", "init", "--quiet", str(builds)], check=True, capture_output=True)

            def write_catalog(versions, eventstore_version="3.110.0"):
                catalog.write_text('<Project><PropertyGroup><HexalithEventStoreVersion>'
                    + eventstore_version + '</HexalithEventStoreVersion></PropertyGroup><ItemGroup>'
                    + ''.join(f'<PackageVersion Include="{package}" Version="{version}" />'
                        for package, version in versions.items()) + '</ItemGroup></Project>', encoding="utf-8")

            catalog_versions = dict(PACKAGE_VERSIONS, **{"Dapr.Client": "1.18.10"})
            aliased_versions = {
                package: "$(HexalithEventStoreVersion)" if package.startswith("Hexalith.EventStore") else version
                for package, version in catalog_versions.items()
            }
            write_catalog(aliased_versions)
            subprocess.run(["git", "-C", str(builds), "add", "Props/Directory.Packages.props"],
                           check=True, capture_output=True)
            expected = [{"path": path, "pins": [
                {"package": package, "version": version, "qualified": True}
                for package, version in sorted(versions.items())]}
                for path, versions in (("Consumer.csproj", PACKAGE_VERSIONS), (catalog_relative, catalog_versions))]
            self.assertEqual(expected, CURRENT.inventory(API, workspace, baseline))
            for package in PACKAGE_VERSIONS:
                with self.subTest(package=package):
                    versions = dict(aliased_versions, **{package: "9.9.9"})
                    write_catalog(versions)
                    with self.assertRaisesRegex(API.ValidationError, "Consumer pin drift:.*" + package.replace(".", r"\.")):
                        CURRENT.inventory(API, workspace, baseline)
                    write_catalog(aliased_versions)
                    self.assertEqual(expected, CURRENT.inventory(API, workspace, baseline))
            with self.subTest(package="unconsumed catalog Dapr.Client"):
                write_catalog(dict(aliased_versions, **{"Dapr.Client": "9.9.9"}))
                with self.assertRaisesRegex(API.ValidationError,
                    r"Consumer pin drift: references/Hexalith\.Builds/Props/Directory\.Packages\.props::Dapr\.Client"):
                    CURRENT.inventory(API, workspace, baseline)
                write_catalog(aliased_versions)
                self.assertEqual(expected, CURRENT.inventory(API, workspace, baseline))
            with self.subTest(package="HexalithEventStoreVersion property"):
                write_catalog(aliased_versions, eventstore_version="9.9.9")
                with self.assertRaisesRegex(API.ValidationError, r"Consumer pin drift:.*Hexalith\.EventStore"):
                    CURRENT.inventory(API, workspace, baseline)
                write_catalog(aliased_versions)
                self.assertEqual(expected, CURRENT.inventory(API, workspace, baseline))


if __name__ == "__main__":
    unittest.main()
