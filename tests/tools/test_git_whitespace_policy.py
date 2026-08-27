"""Tests for the repository's tracked Git whitespace policy."""

from __future__ import annotations

import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[2]
ATTRIBUTES_PATH = PROJECT_ROOT / ".gitattributes"


class GitWhitespacePolicyTests(unittest.TestCase):
    """Exercise the checked-in policy through Git's command-line boundary."""

    def setUp(self) -> None:
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.repository = Path(self.temporary_directory.name)
        self.run_git("init", "--quiet")
        shutil.copyfile(ATTRIBUTES_PATH, self.repository / ".gitattributes")
        (self.repository / "ordinary.txt").write_bytes(b"before\r\n")
        self.run_git("add", ".gitattributes", "ordinary.txt")
        self.run_git(
            "-c",
            "user.name=Whitespace Policy Test",
            "-c",
            "user.email=whitespace-policy@example.invalid",
            "commit",
            "--quiet",
            "-m",
            "baseline",
        )

    def tearDown(self) -> None:
        self.temporary_directory.cleanup()

    def run_git(
        self,
        *arguments: str,
        check: bool = True,
    ) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            ["git", *arguments],
            cwd=self.repository,
            capture_output=True,
            text=True,
            check=check,
        )

    def test_plain_diff_check_accepts_required_crlf(self) -> None:
        (self.repository / "ordinary.txt").write_bytes(b"after\r\n")

        completed = self.run_git("diff", "--check", check=False)

        self.assertEqual(0, completed.returncode, completed.stdout + completed.stderr)
        self.assertEqual("", completed.stdout)
        self.assertEqual("", completed.stderr)

    def test_plain_diff_check_rejects_trailing_blank_before_crlf(self) -> None:
        (self.repository / "ordinary.txt").write_bytes(b"after \r\n")

        completed = self.run_git("diff", "--check", check=False)

        output = completed.stdout + completed.stderr
        self.assertNotEqual(0, completed.returncode, output)
        self.assertIn("trailing whitespace", output)

    def test_policy_does_not_prescribe_eol_for_lf_exceptions(self) -> None:
        paths = (
            "tests/e2e/run-live-apphost.sh",
            "Dockerfile",
            ".github/workflows/ci.yml",
            "configuration/example.yaml",
        )

        completed = self.run_git("check-attr", "whitespace", "eol", "--", *paths)

        attributes: dict[tuple[str, str], str] = {}
        for line in completed.stdout.splitlines():
            path, attribute, value = line.split(": ", 2)
            attributes[(path, attribute)] = value

        for path in paths:
            with self.subTest(path=path):
                self.assertEqual("cr-at-eol", attributes[(path, "whitespace")])
                self.assertEqual("unspecified", attributes[(path, "eol")])


if __name__ == "__main__":
    unittest.main()
