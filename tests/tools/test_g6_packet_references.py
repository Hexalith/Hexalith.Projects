"""Check historical G-6 records against the evidence from their own captures.

Current qualification is governed by g6_current.py and the selected current policy.
The archive keeps its own packet/README bindings; fixtures exercise rejection paths
without consulting current gitlinks, sprint status or owner decisions.
"""

from __future__ import annotations

import copy
import hashlib
import json
import re
import subprocess
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
PACKET_PATH = ROOT / "_bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain-20261001/attempt-16/packet.json"


def reviewed_packet_sha256(packet: dict) -> str:
    """Hash the canonical pending form, preserving the supplied acceptance record."""
    pending = copy.deepcopy(packet)
    if pending["status"] == "accepted":
        pending.update(status="pending", acceptance=None, usableAsPrerequisite=False)
    elif pending["status"] != "pending":
        raise AssertionError("archived packet status must be pending or accepted")
    return hashlib.sha256((json.dumps(pending, ensure_ascii=False, indent=2) + "\n").encode()).hexdigest()


def assert_archived_references(packet: dict, readme: str, packet_bytes: bytes | None = None) -> None:
    """Require the archive's full reviewed hash and every full repository binding."""
    reviewed = reviewed_packet_sha256(packet)
    if packet["status"] == "accepted":
        acceptance = packet.get("acceptance")
        if (packet.get("usableAsPrerequisite") is not True or not isinstance(acceptance, dict)
                or acceptance.get("decision") != "accept" or acceptance.get("reviewedPacketSha256") != reviewed):
            raise AssertionError("Accepted archive must carry consistent acceptance and usability")
        raw = packet_bytes if packet_bytes is not None else (json.dumps(packet, ensure_ascii=False, indent=2) + "\n").encode()
        accepted_hash = hashlib.sha256(raw.replace(b"\r\n", b"\n")).hexdigest()
        if f"normalized-LF file SHA-256 `{accepted_hash}`" not in readme:
            raise AssertionError("Archive README omits the accepted packet file hash")
    elif packet.get("acceptance") is not None or packet.get("usableAsPrerequisite") is not False:
        raise AssertionError("Pending archive cannot claim acceptance or usability")
    expected = [reviewed]
    for binding in packet["repositories"]:
        expected.append(binding["revision"])
        if binding["path"] != ".":
            expected.append(binding["rootGitlink"])
        name = re.escape(binding["name"])
        rows = re.findall(r"(?m)^\| " + name + r" \|[^\n]+$", readme)
        link = "none" if binding["path"] == "." else "`" + binding["rootGitlink"] + "`"
        row = f"| {binding['name']} | `{binding['revision']}` | {link} | false |"
        if rows != [row]:
            raise AssertionError(f"Archive repository row differs: {binding['name']}")
    for value in expected:
        if not re.fullmatch(r"[0-9a-f]{40}|[0-9a-f]{64}", value):
            raise AssertionError(f"Invalid full archive identifier: {value!r}")
        if not re.search(r"(?<![0-9a-fA-F])" + value + r"(?![0-9a-fA-F])", readme):
            raise AssertionError(f"Archive README omits full binding {value}")


class ArchivedPacketReferenceTests(unittest.TestCase):
    def setUp(self) -> None:
        self.packet = {
            "status": "pending", "acceptance": None, "usableAsPrerequisite": False,
            "repositories": [
                {"name": "Hexalith.Projects", "path": ".", "revision": "1" * 40},
                {"name": "Hexalith.Builds", "path": "references/Hexalith.Builds", "revision": "2" * 40, "rootGitlink": "3" * 40},
            ],
        }
        self.readme = (reviewed_packet_sha256(self.packet) + "\n"
                       + f"| Hexalith.Projects | `{'1' * 40}` | none | false |\n"
                       + f"| Hexalith.Builds | `{'2' * 40}` | `{'3' * 40}` | false |")

    def test_archived_packet_readme_keeps_its_own_bindings(self) -> None:
        raw = PACKET_PATH.read_bytes()
        packet = json.loads(raw)
        assert_archived_references(packet, PACKET_PATH.with_name("README.md").read_text(encoding="utf-8"), raw)

    def test_fixture_accepts_exact_full_bindings(self) -> None:
        assert_archived_references(self.packet, self.readme)

    def test_missing_truncated_or_embedded_identifiers_are_rejected(self) -> None:
        for value in (reviewed_packet_sha256(self.packet), "1" * 40, "2" * 40, "3" * 40):
            for replacement in ("", value[:8], "a" + value, value + "F"):
                with self.subTest(value=value, replacement=replacement), self.assertRaises(AssertionError):
                    assert_archived_references(self.packet, self.readme.replace(value, replacement))

    def test_changed_packet_requires_a_new_reviewed_hash(self) -> None:
        self.packet["newEvidence"] = "changed"
        with self.assertRaises(AssertionError):
            assert_archived_references(self.packet, self.readme)

    def test_acceptance_normalizes_to_reviewed_pending_bytes_without_mutation(self) -> None:
        accepted = copy.deepcopy(self.packet)
        accepted.update(status="accepted", acceptance={"decision": "accept", "reviewedPacketSha256": reviewed_packet_sha256(self.packet)},
                        usableAsPrerequisite=True)
        before = copy.deepcopy(accepted)
        self.assertEqual(reviewed_packet_sha256(self.packet), reviewed_packet_sha256(accepted))
        raw = (json.dumps(accepted, ensure_ascii=False, indent=2) + "\n").encode()
        readme = self.readme + f"\nnormalized-LF file SHA-256 `{hashlib.sha256(raw).hexdigest()}`"
        assert_archived_references(accepted, readme, raw)
        self.assertEqual(before, accepted)

    def test_repository_bindings_cannot_be_swapped_between_rows(self) -> None:
        swapped = self.readme.replace("1" * 40, "x" * 40).replace("2" * 40, "1" * 40).replace("x" * 40, "2" * 40)
        with self.assertRaisesRegex(AssertionError, "repository row differs"):
            assert_archived_references(self.packet, swapped)

    def test_acceptance_metadata_and_accepted_bytes_cannot_drift(self) -> None:
        raw = PACKET_PATH.read_bytes()
        original = json.loads(raw)
        readme = PACKET_PATH.with_name("README.md").read_text()
        for field, value, diagnostic in (("decision", "reject", "consistent acceptance"),
                                         ("reviewedPacketSha256", "a" * 64, "consistent acceptance"),
                                         ("extraMetadata", "changed", "accepted packet file hash")):
            with self.subTest(field=field):
                changed = copy.deepcopy(original)
                changed["acceptance"][field] = value
                with self.assertRaisesRegex(AssertionError, diagnostic):
                    assert_archived_references(changed, readme)
        with self.assertRaisesRegex(AssertionError, "consistent acceptance"):
            assert_archived_references(dict(original, usableAsPrerequisite=False), readme)
        with self.assertRaisesRegex(AssertionError, "accepted packet file hash"):
            assert_archived_references(original, readme, raw + b"\n")
        with self.assertRaisesRegex(AssertionError, "Pending archive"):
            assert_archived_references(dict(self.packet, usableAsPrerequisite=True), self.readme)

    def test_unknown_status_is_rejected(self) -> None:
        for status in ("rejected", None, "Pending"):
            with self.subTest(status=status), self.assertRaises(AssertionError):
                reviewed_packet_sha256(dict(self.packet, status=status))


class SeptemberOq8ProvenanceTests(unittest.TestCase):
    def test_checkout_origin_matches_the_committed_capture(self) -> None:
        capture = ROOT / "_bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain-20260929"
        sidecar = capture / "eventstore/test-binary-provenance.json"
        provenance = json.loads(sidecar.read_bytes())
        records = {}
        for key, path in (("packet", "../packet.json"), ("commandRecord", "../commands.json"),
                          ("observations", "observations.json")):
            self.assertEqual(path, provenance[key]["path"])
            repo_path = (sidecar.parent / path).resolve().relative_to(ROOT).as_posix()
            # Check the archived bytes independently of checkout line-ending filters.
            raw = subprocess.check_output(["git", "cat-file", "blob", f"HEAD:{repo_path}"], cwd=ROOT)
            self.assertEqual(hashlib.sha256(raw).hexdigest(), provenance[key]["sha256"], path)
            records[key] = json.loads(raw)

        packet = records["packet"]
        source = provenance["testBinarySourceRevision"]
        self.assertEqual("https://github.com/Hexalith/Hexalith.EventStore", provenance["sourceRepository"])
        self.assertEqual([source], [row["revision"] for row in packet["repositories"]
                                    if row["name"] == "Hexalith.EventStore"])
        self.assertEqual(packet["capturedUtc"], provenance["captureUtc"])
        artifacts = {row["kind"]: row["sha256"] for row in packet["artifacts"]}
        self.assertEqual(artifacts["command-record"], provenance["commandRecord"]["sha256"])
        self.assertEqual(artifacts["eventstore-observations"], provenance["observations"]["sha256"])
        scope = provenance["scope"]
        entry = scope["shippedReleaseEntryAssemblies"]
        self.assertEqual("checkout-build", entry["origin"])
        self.assertIs(entry["value"], True)
        self.assertIs(records["observations"]["executionConfiguration"]["shippedReleaseEntryAssemblies"],
                      entry["value"])
        outcomes = {row["purpose"]: row for row in records["commandRecord"]["commands"]}
        qualifier = outcomes["real PostgreSQL two-sidecar stop/restart qualifier"]
        self.assertEqual(0, qualifier["exitCode"])
        self.assertIn(f"built from EventStore checkout source {source}", qualifier["outcome"])
        support = outcomes["21 exact deterministic support selectors"]
        self.assertEqual(0, support["exitCode"])
        self.assertIn("built from the same EventStore checkout source", support["outcome"])
        release = outcomes["Projects restore and release build"]
        packages = scope["projectsReleaseBuild"]
        self.assertEqual(0, release["exitCode"])
        self.assertEqual("published-packages", packages["origin"])
        self.assertIn(f"consuming published EventStore {packages['eventStoreVersion']},", release["outcome"])


if __name__ == "__main__":
    unittest.main()
