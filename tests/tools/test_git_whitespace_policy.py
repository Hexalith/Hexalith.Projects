"""Tests for the repository's tracked Git whitespace policy."""

from __future__ import annotations

import os
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[2]
ATTRIBUTES_PATH = PROJECT_ROOT / ".gitattributes"


class GitWhitespacePolicyTests(unittest.TestCase):
    """Exercise the checked-in policy through Git's command-line boundary."""

    def setUp(self) -> None:
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary_directory.cleanup)
        self.repository = Path(self.temporary_directory.name)
        # Keep developer config, attributes, templates, and repository overrides
        # from influencing the plain Git commands used by this gate.
        self.environment = {
            key: value
            for key, value in os.environ.items()
            if not key.upper().startswith("GIT_")
        }
        self.environment.update(
            {
                "GIT_CONFIG_NOSYSTEM": "1",
                "GIT_CONFIG_GLOBAL": os.devnull,
                "GIT_ATTR_NOSYSTEM": "1",
                "XDG_CONFIG_HOME": str(self.repository / "xdg-config"),
                "LC_ALL": "C",
                "LANGUAGE": "C",
            }
        )
        template = self.repository / "empty-template"
        template.mkdir()
        self.run_git("init", "--quiet", f"--template={template}")
        shutil.copyfile(ATTRIBUTES_PATH, self.repository / ".gitattributes")
        (self.repository / "ordinary.txt").write_bytes(b"before\r\n")
        self.run_git("add", ".gitattributes", "ordinary.txt")

    def run_git(
        self,
        *arguments: str,
        check: bool = True,
    ) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            ["git", *arguments],
            cwd=self.repository,
            env=self.environment,
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

    def test_plain_diff_check_preserves_other_default_whitespace_checks(self) -> None:
        cases = (
            ("trailing tab", b"after\t\r\n", "trailing whitespace"),
            ("blank line at eof", b"after\r\n\r\n", "new blank line at EOF"),
            ("space before tab", b" \tafter\r\n", "space before tab in indent"),
            ("extra carriage return", b"after\r\r\n", "trailing whitespace"),
        )
        for name, content, diagnostic in cases:
            with self.subTest(name=name):
                (self.repository / "ordinary.txt").write_bytes(content)

                completed = self.run_git("diff", "--check", check=False)

                output = completed.stdout + completed.stderr
                self.assertNotEqual(0, completed.returncode, output)
                self.assertIn(diagnostic, output)

    def test_policy_does_not_prescribe_eol_for_lf_exceptions(self) -> None:
        paths = (
            "ordinary.txt",
            "tests/e2e/run-live-apphost.sh",
            "scripts/example.bash",
            "scripts/example.zsh",
            "Dockerfile",
            ".github/workflows/ci.yml",
            "configuration/example.yaml",
        )

        completed = self.run_git("check-attr", "whitespace", "eol", "text", "--", *paths)

        attributes: dict[tuple[str, str], str] = {}
        for line in completed.stdout.splitlines():
            path, attribute, value = line.split(": ", 2)
            attributes[(path, attribute)] = value

        for path in paths:
            with self.subTest(path=path):
                self.assertEqual("cr-at-eol", attributes[(path, "whitespace")])
                self.assertEqual("unspecified", attributes[(path, "eol")])
                self.assertEqual("unspecified", attributes[(path, "text")])


class GitWhitespacePolicyIsolationTests(unittest.TestCase):
    """Prove inherited Git settings cannot change the gate's outcomes."""

    def test_gate_ignores_inherited_git_configuration_and_attributes(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            attributes = root / "attributes"
            attributes.write_text("* eol=lf text\n", encoding="utf-8")
            config = root / "gitconfig"
            config.write_text(
                "[core]\n"
                "    autocrlf = true\n"
                f"    attributesFile = {attributes.as_posix()}\n",
                encoding="utf-8",
            )
            xdg_config = root / "xdg-config"
            (xdg_config / "git").mkdir(parents=True)
            shutil.copyfile(attributes, xdg_config / "git" / "attributes")
            template = root / "template"
            template.mkdir()
            shutil.copyfile(config, template / "config")
            environment = dict(os.environ)
            environment.update(
                {
                    "GIT_CONFIG_GLOBAL": str(config),
                    "GIT_CONFIG_SYSTEM": str(config),
                    "GIT_CONFIG_COUNT": "1",
                    "GIT_CONFIG_KEY_0": "core.attributesFile",
                    "GIT_CONFIG_VALUE_0": str(attributes),
                    "GIT_DIR": str(root / "missing.git"),
                    "GIT_WORK_TREE": str(root / "missing-worktree"),
                    "GIT_INDEX_FILE": str(root / "external-index"),
                    "GIT_TEMPLATE_DIR": str(template),
                    "XDG_CONFIG_HOME": str(xdg_config),
                    "PYTHONDONTWRITEBYTECODE": "1",
                }
            )

            completed = subprocess.run(
                [
                    sys.executable,
                    str(Path(__file__).resolve()),
                    "GitWhitespacePolicyTests",
                    "-v",
                ],
                cwd=PROJECT_ROOT,
                env=environment,
                capture_output=True,
                text=True,
                check=False,
            )

            self.assertEqual(0, completed.returncode, completed.stdout + completed.stderr)
            self.assertFalse((root / "external-index").exists())


if __name__ == "__main__":
    unittest.main()
