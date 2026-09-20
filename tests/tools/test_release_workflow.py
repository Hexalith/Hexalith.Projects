#!/usr/bin/env python3
"""Execute the release workflow's fail-closed shell guards hermetically."""

from __future__ import annotations

import os
import subprocess
import tempfile
import unittest
from pathlib import Path


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
WORKFLOW_PATH = REPOSITORY_ROOT / ".github" / "workflows" / "release.yml"
VALID_SHA = "a" * 40
OTHER_SHA = "b" * 40


def extract_named_step_run(step_name: str) -> str:
    """Return one named workflow step's literal run script."""

    lines = WORKFLOW_PATH.read_text(encoding="utf-8").splitlines()
    marker = f"      - name: {step_name}"
    matches = [index for index, line in enumerate(lines) if line == marker]
    if len(matches) != 1:
        raise AssertionError(f"Expected exactly one {step_name!r} step, found {len(matches)}")

    start = matches[0]
    end = next(
        (index for index in range(start + 1, len(lines)) if lines[index].startswith("      - ")),
        len(lines),
    )
    run_markers = [index for index in range(start + 1, end) if lines[index] == "        run: |"]
    if len(run_markers) != 1:
        raise AssertionError(f"Expected one literal run block in {step_name!r}")

    script_lines: list[str] = []
    for line in lines[run_markers[0] + 1 : end]:
        if line and not line.startswith("          "):
            raise AssertionError(f"Unexpected indentation in {step_name!r}: {line!r}")
        script_lines.append(line[10:] if line else "")
    return "\n".join(script_lines) + "\n"


def write_executable(directory: Path, name: str, source: str) -> None:
    """Create one executable command stub."""

    path = directory / name
    path.write_text(source, encoding="utf-8")
    path.chmod(0o755)


class ReleaseWorkflowShellTests(unittest.TestCase):
    """Exercise the extracted freeze and late source-revalidation scripts."""

    @classmethod
    def setUpClass(cls) -> None:
        cls.freeze_script = extract_named_step_run("Resolve release publication freeze")
        cls.source_script = extract_named_step_run("Revalidate current source before NuGet login")

    def run_freeze(self, value: str | None) -> tuple[subprocess.CompletedProcess[str], str]:
        with tempfile.TemporaryDirectory(prefix="projects-release-freeze-") as temporary:
            directory = Path(temporary)
            output_path = directory / "github-output"
            environment = os.environ.copy()
            environment.pop("HEXALITH_RELEASE_PUBLISH_ENABLED", None)
            environment.update(
                {
                    "GITHUB_OUTPUT": str(output_path),
                    "PATH": f"{directory}{os.pathsep}{environment['PATH']}",
                }
            )
            if value is not None:
                environment["HEXALITH_RELEASE_PUBLISH_ENABLED"] = value
            result = subprocess.run(
                ["bash", "-c", self.freeze_script],
                cwd=directory,
                env=environment,
                capture_output=True,
                text=True,
                check=False,
            )
            output = output_path.read_text(encoding="utf-8") if output_path.exists() else ""
            return result, output

    def run_source(
        self,
        *,
        publish_enabled: str,
        dispatch_sha: str,
        checked_out_sha: str,
        live_sha: str,
        stubs_must_not_run: bool = False,
    ) -> subprocess.CompletedProcess[str]:
        with tempfile.TemporaryDirectory(prefix="projects-release-source-") as temporary:
            directory = Path(temporary)
            if stubs_must_not_run:
                write_executable(directory, "git", "#!/usr/bin/env bash\nexit 97\n")
                write_executable(directory, "gh", "#!/usr/bin/env bash\nexit 98\n")
            else:
                write_executable(
                    directory,
                    "git",
                    f"#!/usr/bin/env bash\nprintf '%s\\n' '{checked_out_sha}'\n",
                )
                write_executable(
                    directory,
                    "gh",
                    f"#!/usr/bin/env bash\nprintf '%s\\n' '{live_sha}'\n",
                )
            environment = os.environ.copy()
            environment.update(
                {
                    "DISPATCH_SHA": dispatch_sha,
                    "GH_TOKEN": "fixture-token",
                    "PATH": f"{directory}{os.pathsep}{environment['PATH']}",
                    "PUBLISH_ENABLED": publish_enabled,
                    "REPOSITORY": "Hexalith/Hexalith.Projects",
                    "SOURCE_BRANCH": "main",
                }
            )
            return subprocess.run(
                ["bash", "-c", self.source_script],
                cwd=directory,
                env=environment,
                capture_output=True,
                text=True,
                check=False,
            )

    def test_repository_variable_requires_exact_true(self) -> None:
        cases = (
            ("absent", None, "false"),
            ("empty", "", "false"),
            ("mixed-case", "True", "false"),
            ("whitespace", "true ", "false"),
            ("false", "false", "false"),
            ("exact-true", "true", "true"),
        )
        for label, value, expected in cases:
            with self.subTest(label=label):
                result, output = self.run_freeze(value)
                self.assertEqual(0, result.returncode, result.stderr)
                self.assertEqual(f"publish-enabled={expected}\n", output)

    def test_source_revalidation_rejects_malformed_dispatch_sha(self) -> None:
        result = self.run_source(
            publish_enabled="true",
            dispatch_sha="not-a-sha",
            checked_out_sha=VALID_SHA,
            live_sha=VALID_SHA,
        )
        self.assertNotEqual(0, result.returncode)
        self.assertIn("exact lowercase commit SHA", result.stderr)

    def test_source_revalidation_rejects_dispatch_checkout_mismatch(self) -> None:
        result = self.run_source(
            publish_enabled="true",
            dispatch_sha=VALID_SHA,
            checked_out_sha=OTHER_SHA,
            live_sha=VALID_SHA,
        )
        self.assertNotEqual(0, result.returncode)
        self.assertIn("does not match the dispatched commit SHA", result.stderr)

    def test_source_revalidation_rejects_live_main_mismatch(self) -> None:
        result = self.run_source(
            publish_enabled="true",
            dispatch_sha=VALID_SHA,
            checked_out_sha=VALID_SHA,
            live_sha=OTHER_SHA,
        )
        self.assertNotEqual(0, result.returncode)
        self.assertIn("became stale during setup", result.stderr)

    def test_source_revalidation_accepts_exact_live_main(self) -> None:
        result = self.run_source(
            publish_enabled="true",
            dispatch_sha=VALID_SHA,
            checked_out_sha=VALID_SHA,
            live_sha=VALID_SHA,
        )
        self.assertEqual(0, result.returncode, result.stderr)

    def test_source_revalidation_skips_explicitly_frozen_release(self) -> None:
        result = self.run_source(
            publish_enabled="false",
            dispatch_sha="invalid-is-safe-when-frozen",
            checked_out_sha="",
            live_sha="",
            stubs_must_not_run=True,
        )
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn("Source revalidation skipped", result.stdout)


if __name__ == "__main__":
    unittest.main()
