"""Hermetic tests for the G-6 packet-revision-equals-root-gitlink check.

These tests build throwaway repositories and never read the real packet or gitlinks, so they can
run in workflow-gates without failing on a routine submodule bump.
"""

from __future__ import annotations

import contextlib
import importlib.util
import io
import json
import subprocess
import tempfile
import unittest
from pathlib import Path


SCRIPT = Path(__file__).resolve().with_name("check_g6_packet_gitlinks.py")
SPEC = importlib.util.spec_from_file_location("check_g6_packet_gitlinks", SCRIPT)
assert SPEC is not None and SPEC.loader is not None
CHECK = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(CHECK)
BUILDS_REVISION = "1" * 40
EVENTSTORE_REVISION = "2" * 40


class PacketGitlinkCheckTests(unittest.TestCase):
    """Exercise the check against a root index with two recorded gitlinks."""

    def setUp(self) -> None:
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.workspace = Path(self.temporary_directory.name)
        self.run_git("init", "--quiet")
        for path, revision in (
            ("references/Hexalith.Builds", BUILDS_REVISION),
            ("references/Hexalith.EventStore", EVENTSTORE_REVISION),
        ):
            self.run_git("update-index", "--add", "--cacheinfo", f"160000,{revision},{path}")
        (self.workspace / "notes.txt").write_text("regular file\n", encoding="utf-8")
        self.run_git("add", "notes.txt")
        self.packet_path = self.workspace / "packet.json"

    def tearDown(self) -> None:
        self.temporary_directory.cleanup()

    def run_git(self, *arguments: str) -> None:
        subprocess.run(["git", "-C", str(self.workspace), *arguments], capture_output=True, text=True, check=True)

    def run_check(self, repositories: list[dict[str, str]]) -> tuple[int, str, str]:
        self.packet_path.write_text(json.dumps({"repositories": repositories}), encoding="utf-8")
        stdout, stderr = io.StringIO(), io.StringIO()
        with contextlib.redirect_stdout(stdout), contextlib.redirect_stderr(stderr):
            exit_code = CHECK.main(["--workspace", str(self.workspace), "--packet", "packet.json"])
        return exit_code, stdout.getvalue(), stderr.getvalue()

    @staticmethod
    def bindings(builds: str = BUILDS_REVISION, eventstore: str = EVENTSTORE_REVISION) -> list[dict[str, str]]:
        return [
            {"name": "Hexalith.Projects", "path": ".", "revision": "0" * 40},
            {"name": "Hexalith.Builds", "path": "references/Hexalith.Builds", "revision": builds},
            {"name": "Hexalith.EventStore", "path": "references/Hexalith.EventStore", "revision": eventstore},
        ]

    def test_exact_gitlinks_pass(self) -> None:
        exit_code, stdout, stderr = self.run_check(self.bindings())

        self.assertEqual(0, exit_code, stderr)
        self.assertIn("G6-PACKET-GITLINKS-EXACT: 2 submodule revisions equal their root gitlinks", stdout)

    def test_submodule_bump_after_capture_fails(self) -> None:
        exit_code, _, stderr = self.run_check(self.bindings(eventstore="3" * 40))

        self.assertEqual(1, exit_code)
        self.assertIn(f"Hexalith.EventStore is bound at '{'3' * 40}' but the root gitlink is '{EVENTSTORE_REVISION}'", stderr)
        self.assertNotIn("Hexalith.Builds", stderr)

    def test_binding_that_is_not_a_gitlink_fails(self) -> None:
        repositories = self.bindings() + [{"name": "Notes", "path": "notes.txt", "revision": "4" * 40}]

        exit_code, _, stderr = self.run_check(repositories)

        self.assertEqual(1, exit_code)
        self.assertIn("Notes is bound at 'notes.txt', which is not a root gitlink", stderr)

    def test_missing_gitlink_path_fails(self) -> None:
        repositories = self.bindings() + [{"name": "Tenants", "path": "references/Hexalith.Tenants", "revision": "5" * 40}]

        exit_code, _, stderr = self.run_check(repositories)

        self.assertEqual(1, exit_code)
        self.assertIn("Tenants is bound at 'references/Hexalith.Tenants', which is not a root gitlink", stderr)

    def test_packet_without_submodule_bindings_fails(self) -> None:
        exit_code, _, stderr = self.run_check(self.bindings()[:1])

        self.assertEqual(1, exit_code)
        self.assertIn("the packet binds no submodule repositories", stderr)


if __name__ == "__main__":
    unittest.main()
