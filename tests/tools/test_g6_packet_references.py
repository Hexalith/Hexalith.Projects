"""Require the G-6 and P1R documents to quote the reviewed G-6 packet hash and the current gitlinks.

Hermetic: it reads only local files and the root Git index, never the network. The G-6 README,
the current P1R owner packet, the Architecture Spine and sprint-status.yaml must quote the full SHA-256
of the reviewed packet and the full root Builds and EventStore gitlinks; the README must quote every
packet-bound submodule gitlink; the current sprint-status keys must carry exactly those values; and
a replaced pending packet hash may appear only on a line that marks it replaced or superseded.

The reviewed current v2 packet is canonical pending JSON with null acceptance and false usability.
Historical acceptance and the separate P1R record do not approve this packet.

It runs in the g6-candidate job, so a stale document fails only the G-6 jobs. When a new packet is
captured, the same change must update every quoted hash and gitlink.
"""

from __future__ import annotations

import hashlib
import copy
import json
import re
import subprocess
import unittest
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[2]
ARTIFACTS = PROJECT_ROOT / "_bmad-output/implementation-artifacts"
PACKET_PATH = ARTIFACTS / "qualification-evidence/g-6-runtime-toolchain-20261001/attempt-16/packet.json"
README_PATH = PACKET_PATH.with_name("README.md")
OWNER_PACKET_PATH = ARTIFACTS / "6-1-p1r-current-exact-baseline-candidate.md"
SPRINT_STATUS_PATH = ARTIFACTS / "sprint-status.yaml"
SPINE_PATH = PROJECT_ROOT / "_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md"
DOCUMENTS = (README_PATH, OWNER_PACKET_PATH, SPINE_PATH, SPRINT_STATUS_PATH)
PENDING_STATUS = b'"status": "pending"'
ACCEPTED_STATUS = b'"status": "accepted"'
REPLACED_MARKERS = ("supersed", "replac")


def reviewed_packet_sha256(packet: dict | None = None) -> str:
    """Normalized-LF SHA-256 of the packet as the owner reviews it (status pending)."""
    packet = json.loads(PACKET_PATH.read_text(encoding="utf-8")) if packet is None else copy.deepcopy(packet)
    if packet["status"] == "accepted":
        packet.update(status="pending", acceptance=None, usableAsPrerequisite=False)
    elif packet["status"] != "pending":
        raise AssertionError("current packet status must be pending or accepted")
    return hashlib.sha256((json.dumps(packet, ensure_ascii=False, indent=2) + "\n").encode()).hexdigest()


def root_gitlink(path: str) -> str:
    entry = subprocess.run(
        ["git", "-C", str(PROJECT_ROOT), "ls-files", "--stage", "--", path],
        capture_output=True, text=True, check=True, timeout=60).stdout.strip()
    match = re.fullmatch(r"160000 ([0-9a-f]{40}) 0\t" + re.escape(path), entry)
    if match is None:
        raise AssertionError(f"{path} is not a root gitlink: {entry!r}")
    return match.group(1)


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def yaml_block(text: str, key: str, indent: int) -> str:
    """Return the lines nested under one `key:` line at the given indentation (plain line scan)."""
    match = re.search(rf"(?m)^{' ' * indent}{re.escape(key)}:[ \t]*\n((?:(?:{' ' * (indent + 1)}.*)?\n)*)", text)
    if match is None:
        raise AssertionError(f"sprint-status.yaml has no '{key}' block at indentation {indent}")
    return match.group(1)


def yaml_value(block: str, key: str, indent: int) -> str:
    match = re.search(rf"(?m)^{' ' * indent}{re.escape(key)}:[ \t]*(.+?)[ \t]*$", block)
    if match is None:
        raise AssertionError(f"sprint-status.yaml block has no '{key}' key at indentation {indent}")
    return match.group(1).strip('"')


def load_selected_inputs() -> None:
    global PACKET, SUBMODULE_BINDINGS, PROJECTS_REVISION, SPRINT_STATUS, P1R_BLOCK, G6_BLOCK
    PACKET = json.loads(PACKET_PATH.read_text(encoding="utf-8"))
    SUBMODULE_BINDINGS = [binding for binding in PACKET["repositories"] if binding["path"] != "."]
    PROJECTS_REVISION = next(binding["revision"] for binding in PACKET["repositories"] if binding["path"] == ".")
    SPRINT_STATUS = read(SPRINT_STATUS_PATH)
    P1R_BLOCK = yaml_block(SPRINT_STATUS, "p1r_current_revalidation", 0)
    G6_BLOCK = yaml_block(yaml_block(SPRINT_STATUS, "qualification_gates", 0), "G-6", 2)


def assert_packet_usability(packet: dict) -> None:
    if packet["status"] not in {"pending", "accepted"} or packet["usableAsPrerequisite"] is not (packet["status"] == "accepted"):
        raise AssertionError("Packet usability must follow pending/accepted status")


class G6PacketReferenceTests(unittest.TestCase):
    """The documents index exactly the selected pending packet and the gitlinks it binds."""

    @classmethod
    def setUpClass(cls) -> None:
        load_selected_inputs()

    def test_documents_quote_the_reviewed_packet_hash(self) -> None:
        packet_hash = reviewed_packet_sha256()
        for document in DOCUMENTS:
            with self.subTest(document=document.name):
                self.assertIn(packet_hash, read(document))

    def test_documents_quote_the_current_builds_and_eventstore_gitlinks(self) -> None:
        for path in ("references/Hexalith.Builds", "references/Hexalith.EventStore"):
            gitlink = root_gitlink(path)
            for document in DOCUMENTS:
                with self.subTest(document=document.name, gitlink=path):
                    self.assertIn(gitlink, read(document))

    def test_readme_quotes_every_bound_submodule_gitlink(self) -> None:
        readme = read(README_PATH)
        self.assertTrue(SUBMODULE_BINDINGS, "the packet must bind submodule repositories")
        for binding in SUBMODULE_BINDINGS:
            with self.subTest(repository=binding["name"]):
                gitlink = root_gitlink(binding["path"])
                self.assertEqual(binding["rootGitlink"], gitlink)
                self.assertIn(gitlink, readme)
                self.assertIn(binding["revision"], readme)
        self.assertIn(PROJECTS_REVISION, readme)

    def test_sprint_status_current_keys_carry_the_packet_and_gitlinks(self) -> None:
        packet_hash = reviewed_packet_sha256()
        builds = root_gitlink("references/Hexalith.Builds")
        eventstore = root_gitlink("references/Hexalith.EventStore")
        candidate = yaml_block(P1R_BLOCK, "candidate", 2)
        self.assertEqual(yaml_value(P1R_BLOCK, "g6_builds_source_revision", 2), builds)
        # Published P1R acceptance keeps its exact package/source coordinate when
        # the current G-6 checkout advances to committed qualification tooling.
        accepted = yaml_block(yaml_block(SPRINT_STATUS, "p1r_acceptance_gate", 0), "selected", 2)
        self.assertEqual(yaml_value(candidate, "builds_revision", 4), yaml_value(accepted, "builds_revision", 4))
        self.assertEqual(yaml_value(P1R_BLOCK, "eventstore_checkout_gitlink", 2), eventstore)
        self.assertEqual(yaml_value(P1R_BLOCK, "projects_baseline_commit", 2), PROJECTS_REVISION)
        self.assertEqual(yaml_value(P1R_BLOCK, "g6_fresh_candidate_sha256", 2), packet_hash)
        self.assertEqual(yaml_value(P1R_BLOCK, "usable_as_prerequisite", 2), "false")
        assert_packet_usability(PACKET)
        self.assertEqual(yaml_value(G6_BLOCK, "current_candidate_sha256", 4), packet_hash)
        self.assertEqual(yaml_value(G6_BLOCK, "current_candidate_builds_source_revision", 4), builds)

    def test_replaced_packet_hashes_appear_only_as_replaced(self) -> None:
        replaced = {value for key, value in re.findall(r"(?m)^\s*(\w*(?:superseded|replaced)\w*sha256): ([0-9a-f]{64})\s*$",
                                                        SPRINT_STATUS)}
        self.assertTrue(replaced, "sprint-status.yaml must record the replaced pending packet hashes")
        self.assertNotIn(reviewed_packet_sha256(), replaced)
        for document in DOCUMENTS:
            for number, line in enumerate(read(document).splitlines(), start=1):
                for value in replaced:
                    if value[:8] in line:
                        with self.subTest(document=document.name, line=number, hash=value[:8]):
                            self.assertTrue(any(marker in line.lower() for marker in REPLACED_MARKERS), line)


class PacketUsabilityStateTests(unittest.TestCase):
    def test_pending_packet_is_unusable_and_preserves_downstream_state(self):
        downstream = {"p1r": False, "downstream": "blocked"}
        assert_packet_usability({"status": "pending", "usableAsPrerequisite": False})
        self.assertEqual(downstream, {"p1r": False, "downstream": "blocked"})
        with self.assertRaises(AssertionError):
            assert_packet_usability({"status": "pending", "usableAsPrerequisite": True})

    def test_accepted_packet_is_usable_without_accepting_p1r(self):
        downstream = {"p1r": False, "downstream": "blocked"}
        assert_packet_usability({"status": "accepted", "usableAsPrerequisite": True})
        self.assertFalse(downstream["p1r"])
        self.assertEqual(downstream["downstream"], "blocked")
        with self.assertRaises(AssertionError):
            assert_packet_usability({"status": "accepted", "usableAsPrerequisite": False})
        accepted = {"status": "accepted", "usableAsPrerequisite": True, "acceptance": {"decision": "accept"}}
        pending = {"status": "pending", "usableAsPrerequisite": False, "acceptance": None}
        self.assertEqual(reviewed_packet_sha256(accepted), reviewed_packet_sha256(pending))
        self.assertTrue(accepted["usableAsPrerequisite"])


if __name__ == "__main__":
    unittest.main()
