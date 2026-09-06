#!/usr/bin/env python3
# /// script
# requires-python = ">=3.10"
# ///
"""Hermetic tests for failure-atomic legacy cleanup."""

import csv
import hashlib
import json
import subprocess
import sys
import tempfile
from pathlib import Path

ENTRY_ROOT = Path(__file__).resolve().parents[4]
PROJECT_ROOT = Path(__file__).resolve().parents[5]
LOCAL_SCRIPTS = [
    ENTRY_ROOT / "skills" / "bmad-bmb-setup" / "scripts" / "cleanup-legacy.py",
    ENTRY_ROOT
    / "skills"
    / "bmad-module-builder"
    / "assets"
    / "setup-skill-template"
    / "scripts"
    / "cleanup-legacy.py",
]
ENTRY_POINTS = [PROJECT_ROOT / name for name in (".agent", ".agents", ".claude")]
ALL_SCRIPTS = [
    path
    for entry_point in ENTRY_POINTS
    for path in (
        entry_point
        / "skills"
        / "bmad-bmb-setup"
        / "scripts"
        / "cleanup-legacy.py",
        entry_point
        / "skills"
        / "bmad-module-builder"
        / "assets"
        / "setup-skill-template"
        / "scripts"
        / "cleanup-legacy.py",
    )
]
ALL_TESTS = [
    entry_point
    / "skills"
    / "bmad-module-builder"
    / "scripts"
    / "tests"
    / "test-cleanup-legacy.py"
    for entry_point in ENTRY_POINTS
]
MANIFEST = PROJECT_ROOT / "_bmad" / "_config" / "files-manifest.csv"
CI_WORKFLOW = PROJECT_ROOT / ".github" / "workflows" / "ci.yml"

FAULT_RUNNER = r"""
import runpy
import shutil
import sys
from pathlib import Path

separator = sys.argv.index("--")
script, mode, failed_stage, failed_restore = sys.argv[1:separator]
cli_args = sys.argv[separator + 1:]
bmad_root = Path(cli_args[cli_args.index("--bmad-dir") + 1]).resolve()
original_rename = Path.rename
original_rmtree = shutil.rmtree
original_exists = Path.exists
original_stderr = sys.stderr

def injected_rename(source, target):
    source = Path(source)
    target = Path(target)
    if source.parent == bmad_root and source.name == failed_stage:
        raise OSError("injected staging failure")
    if (
        mode == "rollback"
        and source.parent.parent == bmad_root
        and source.parent.name.startswith(".legacy-cleanup-")
        and target.parent == bmad_root
        and target.name == failed_restore
    ):
        raise OSError("injected rollback failure")
    return original_rename(source, target)

def injected_rmtree(path, *args, **kwargs):
    path = Path(path)
    if mode == "partial-final" and path.parent == bmad_root and path.name.startswith(".legacy-cleanup-"):
        original_rmtree(path / "0000")
        raise OSError("injected partial final cleanup failure")
    if mode in ("final", "probe") and path.parent == bmad_root and path.name.startswith(".legacy-cleanup-"):
        raise OSError("injected final cleanup failure")
    return original_rmtree(path, *args, **kwargs)

def injected_exists(path):
    path = Path(path)
    if (
        mode == "probe"
        and path.parent.name.startswith(".legacy-cleanup-")
        and path.name == "0000"
    ):
        raise OSError("injected recovery probe failure")
    return original_exists(path)

class FailingStderr:
    def __init__(self):
        self.failed = False

    def write(self, text):
        fail_staging = mode == "verbose-staging" and "Staging " in text and f"{bmad_root / 'core'} (" in text
        fail_rollback = mode == "verbose-rollback" and "Restored " in text and f"{bmad_root / 'core'} from" in text
        if not self.failed and (fail_staging or fail_rollback):
            self.failed = True
            raise OSError("injected verbose stderr failure")
        return original_stderr.write(text)

    def flush(self):
        return original_stderr.flush()

Path.rename = injected_rename
shutil.rmtree = injected_rmtree
Path.exists = injected_exists
if mode.startswith("verbose-"):
    sys.stderr = FailingStderr()
sys.argv = [script, *cli_args]
runpy.run_path(script, run_name="__main__")
"""


def write_target(bmad_root: Path, name: str, sentinel: bytes, extra_files: int = 0) -> Path:
    """Create a cleanup target with a byte-level sentinel."""
    target = bmad_root / name
    target.mkdir()
    (target / "sentinel.bin").write_bytes(sentinel)
    for index in range(extra_files):
        nested = target / "nested"
        nested.mkdir(exist_ok=True)
        (nested / f"extra-{index}.bin").write_bytes(sentinel + bytes([index]))
    return target


def run_cleanup(
    script: Path,
    bmad_root: Path,
    also_remove: list[str] | None = None,
    mode: str = "none",
    failed_stage: str = "",
    failed_restore: str = "",
    verbose: bool = False,
) -> tuple[int, dict]:
    """Run a cleanup copy with optional injected filesystem failures."""
    cli_args = [
        "--bmad-dir",
        str(bmad_root),
        "--module-code",
        "first",
    ]
    for dirname in also_remove or []:
        cli_args.extend(["--also-remove", dirname])
    if verbose:
        cli_args.append("--verbose")

    if mode == "none":
        command = [sys.executable, str(script), *cli_args]
    else:
        command = [
            sys.executable,
            "-c",
            FAULT_RUNNER,
            str(script),
            mode,
            failed_stage,
            failed_restore,
            "--",
            *cli_args,
        ]

    result = subprocess.run(command, capture_output=True, text=True, timeout=10)
    try:
        data = json.loads(result.stdout)
    except json.JSONDecodeError as error:
        raise AssertionError(
            f"{script} returned non-JSON stdout: {result.stdout!r}; stderr={result.stderr!r}"
        ) from error
    return result.returncode, data


def transaction_directories(bmad_root: Path) -> list[Path]:
    """Return cleanup transaction directories still present under _bmad."""
    return sorted(bmad_root.glob(".legacy-cleanup-*"))


def test_successful_batch_preserves_order_and_file_count():
    """Stage and delete every existing directory while preserving result semantics."""
    for script in LOCAL_SCRIPTS:
        with tempfile.TemporaryDirectory() as tmp:
            bmad_root = Path(tmp) / "_bmad"
            bmad_root.mkdir()
            write_target(bmad_root, "first", b"first")
            write_target(bmad_root, "core", b"core", extra_files=1)
            write_target(bmad_root, "third", b"third")
            (bmad_root / "plain-file").write_bytes(b"keep")

            code, data = run_cleanup(
                script,
                bmad_root,
                also_remove=["third", "missing", "plain-file"],
            )

            assert code == 0, data
            assert data["status"] == "success"
            assert data["directories_removed"] == ["first", "core", "third"]
            assert data["directories_not_found"] == ["missing", "plain-file"]
            assert data["files_removed_count"] == 4
            assert (bmad_root / "plain-file").read_bytes() == b"keep"
            assert not transaction_directories(bmad_root)


def test_staging_failure_restores_every_target_byte_for_byte():
    """A failure on target N restores all previously staged targets."""
    sentinels = {"first": b"\x00first\xff", "core": b"\x00core\xfe", "third": b"\x00third\xfd"}
    for script in LOCAL_SCRIPTS:
        with tempfile.TemporaryDirectory() as tmp:
            bmad_root = Path(tmp) / "_bmad"
            bmad_root.mkdir()
            for name, sentinel in sentinels.items():
                write_target(bmad_root, name, sentinel)

            code, data = run_cleanup(
                script,
                bmad_root,
                also_remove=["third"],
                mode="staging",
                failed_stage="third",
            )

            assert code == 2, data
            assert data["phase"] == "staging"
            assert data["directories_failed"] == "third"
            assert data["directories_removed"] == []
            assert data["directories_restored"] == ["core", "first"]
            assert data["rollback_complete"] is True
            assert data["recovery_directory"] is None
            assert data["recovery_targets"] == []
            for name, sentinel in sentinels.items():
                assert (bmad_root / name / "sentinel.bin").read_bytes() == sentinel
            assert not transaction_directories(bmad_root)


def test_incomplete_rollback_retains_exact_recovery_mapping():
    """Rollback continues after an error and reports each unrestored target."""
    sentinels = {"first": b"first-data", "core": b"core-data", "third": b"third-data"}
    for script in LOCAL_SCRIPTS:
        with tempfile.TemporaryDirectory() as tmp:
            bmad_root = Path(tmp) / "_bmad"
            bmad_root.mkdir()
            for name, sentinel in sentinels.items():
                write_target(bmad_root, name, sentinel)

            code, data = run_cleanup(
                script,
                bmad_root,
                also_remove=["third"],
                mode="rollback",
                failed_stage="third",
                failed_restore="core",
            )

            assert code == 2, data
            assert data["phase"] == "staging"
            assert data["directories_restored"] == ["first"]
            assert data["rollback_complete"] is False
            assert [item["directory"] for item in data["rollback_errors"]] == ["core"]
            assert len(data["recovery_targets"]) == 1
            mapping = data["recovery_targets"][0]
            assert mapping["directory"] == "core"
            assert Path(mapping["original_path"]) == bmad_root / "core"
            assert (Path(mapping["staged_path"]) / "sentinel.bin").read_bytes() == sentinels["core"]
            assert Path(data["recovery_directory"]) == Path(mapping["staged_path"]).parent
            assert (bmad_root / "first" / "sentinel.bin").read_bytes() == sentinels["first"]
            assert not (bmad_root / "core").exists()
            assert (bmad_root / "third" / "sentinel.bin").read_bytes() == sentinels["third"]


def test_final_cleanup_failure_retains_recoverable_staged_contents():
    """A post-commit delete failure leaves originals absent and reports recovery data."""
    sentinels = {"first": b"first-final", "core": b"core-final", "third": b"third-final"}
    for script in LOCAL_SCRIPTS:
        with tempfile.TemporaryDirectory() as tmp:
            bmad_root = Path(tmp) / "_bmad"
            bmad_root.mkdir()
            for name, sentinel in sentinels.items():
                write_target(bmad_root, name, sentinel)

            code, data = run_cleanup(
                script,
                bmad_root,
                also_remove=["third"],
                mode="final",
            )

            assert code == 2, data
            assert data["phase"] == "final-cleanup"
            assert data["directories_removed"] == ["first", "core", "third"]
            assert all(not (bmad_root / name).exists() for name in sentinels)
            assert Path(data["recovery_directory"]).is_dir()
            assert [item["directory"] for item in data["recovery_targets"]] == [
                "first",
                "core",
                "third",
            ]
            for mapping in data["recovery_targets"]:
                assert (Path(mapping["staged_path"]) / "sentinel.bin").read_bytes() == sentinels[
                    mapping["directory"]
                ]


def test_partial_final_cleanup_reports_only_surviving_targets():
    """Recovery mappings exclude a target physically removed before finalization fails."""
    sentinels = {"first": b"first-partial", "core": b"core-partial", "third": b"third-partial"}
    for script in LOCAL_SCRIPTS:
        with tempfile.TemporaryDirectory() as tmp:
            bmad_root = Path(tmp) / "_bmad"
            bmad_root.mkdir()
            for name, sentinel in sentinels.items():
                write_target(bmad_root, name, sentinel)

            code, data = run_cleanup(
                script,
                bmad_root,
                also_remove=["third"],
                mode="partial-final",
            )

            assert code == 2, data
            assert data["phase"] == "final-cleanup"
            assert [item["directory"] for item in data["recovery_targets"]] == [
                "core",
                "third",
            ]
            assert data["recovery_probe_errors"] == []
            for mapping in data["recovery_targets"]:
                assert (Path(mapping["staged_path"]) / "sentinel.bin").read_bytes() == sentinels[
                    mapping["directory"]
                ]


def test_recovery_probe_failure_preserves_original_error_json():
    """An unreadable staged-path probe is reported conservatively without hiding recovery."""
    for script in LOCAL_SCRIPTS:
        with tempfile.TemporaryDirectory() as tmp:
            bmad_root = Path(tmp) / "_bmad"
            bmad_root.mkdir()
            write_target(bmad_root, "first", b"first-probe")
            write_target(bmad_root, "core", b"core-probe")

            code, data = run_cleanup(script, bmad_root, mode="probe")

            assert code == 2, data
            assert data["phase"] == "final-cleanup"
            assert [item["directory"] for item in data["recovery_targets"]] == [
                "first",
                "core",
            ]
            assert len(data["recovery_probe_errors"]) == 1
            assert data["recovery_probe_errors"][0]["directory"] == "first"
            assert "injected recovery probe failure" in data["recovery_probe_errors"][0]["error"]


def test_verbose_diagnostic_failures_do_not_change_transaction_state():
    """Staging and rollback continue when best-effort stderr diagnostics fail."""
    for script in LOCAL_SCRIPTS:
        with tempfile.TemporaryDirectory() as tmp:
            bmad_root = Path(tmp) / "_bmad"
            bmad_root.mkdir()
            write_target(bmad_root, "first", b"first-verbose")
            write_target(bmad_root, "core", b"core-verbose")

            code, data = run_cleanup(
                script,
                bmad_root,
                mode="verbose-staging",
                verbose=True,
            )

            assert code == 0, data
            assert data["directories_removed"] == ["first", "core"]
            assert not transaction_directories(bmad_root)

        with tempfile.TemporaryDirectory() as tmp:
            bmad_root = Path(tmp) / "_bmad"
            bmad_root.mkdir()
            write_target(bmad_root, "first", b"first-rollback-verbose")
            write_target(bmad_root, "core", b"core-rollback-verbose")
            write_target(bmad_root, "third", b"third-rollback-verbose")

            code, data = run_cleanup(
                script,
                bmad_root,
                also_remove=["third"],
                mode="verbose-rollback",
                failed_stage="third",
                verbose=True,
            )

            assert code == 2, data
            assert data["rollback_complete"] is True
            assert data["recovery_targets"] == []
            assert all((bmad_root / name / "sentinel.bin").is_file() for name in ("first", "core", "third"))
            assert not transaction_directories(bmad_root)


def test_empty_batch_creates_no_transaction_state():
    """Missing and non-directory entries retain the idempotent success contract."""
    for script in LOCAL_SCRIPTS:
        with tempfile.TemporaryDirectory() as tmp:
            bmad_root = Path(tmp) / "_bmad"
            bmad_root.mkdir()
            (bmad_root / "first").write_bytes(b"not-a-directory")

            code, data = run_cleanup(script, bmad_root, also_remove=["missing"])

            assert code == 0, data
            assert data["directories_removed"] == []
            assert data["directories_not_found"] == ["first", "core", "missing"]
            assert data["files_removed_count"] == 0
            assert (bmad_root / "first").read_bytes() == b"not-a-directory"
            assert not transaction_directories(bmad_root)


def test_agent_entry_points_are_byte_identical():
    """All installed/template scripts and peer tests remain synchronized."""
    script_bytes = [path.read_bytes() for path in ALL_SCRIPTS]
    test_bytes = [path.read_bytes() for path in ALL_TESTS]
    assert all(content == script_bytes[0] for content in script_bytes[1:])
    assert all(content == test_bytes[0] for content in test_bytes[1:])


def test_manifest_hashes_match_canonical_files():
    """The installed-file manifest records every changed canonical file hash."""
    expected_paths = {
        "bmb/bmad-bmb-setup/scripts/cleanup-legacy.py": LOCAL_SCRIPTS[0],
        "bmb/bmad-module-builder/assets/setup-skill-template/scripts/cleanup-legacy.py": LOCAL_SCRIPTS[1],
        "bmb/bmad-module-builder/scripts/tests/test-cleanup-legacy.py": PROJECT_ROOT
        / ".agents"
        / "skills"
        / "bmad-module-builder"
        / "scripts"
        / "tests"
        / "test-cleanup-legacy.py",
    }
    with MANIFEST.open(newline="") as manifest_file:
        rows = {row["path"]: row for row in csv.DictReader(manifest_file)}

    for manifest_path, source_path in expected_paths.items():
        assert manifest_path in rows
        assert rows[manifest_path]["hash"] == hashlib.sha256(source_path.read_bytes()).hexdigest()


def test_ci_runs_canonical_suite_as_blocking_no_bytecode_gate():
    """The normal workflow policy job blocks on the canonical cleanup suite."""
    workflow = CI_WORKFLOW.read_text(encoding="utf-8").replace("\r\n", "\n")
    expected_step = """      - name: Validate BMAD legacy cleanup atomicity
        env:
          PYTHONDONTWRITEBYTECODE: '1'
        run: python3 .agents/skills/bmad-module-builder/scripts/tests/test-cleanup-legacy.py"""
    assert workflow.count(expected_step) == 1
    workflow_gates = workflow.split("  ci:\n", maxsplit=1)[0]
    assert expected_step in workflow_gates
    assert workflow_gates.index(expected_step) < workflow_gates.index("      - name: Validate CI/CD invariants")


if __name__ == "__main__":
    tests = [
        test_successful_batch_preserves_order_and_file_count,
        test_staging_failure_restores_every_target_byte_for_byte,
        test_incomplete_rollback_retains_exact_recovery_mapping,
        test_final_cleanup_failure_retains_recoverable_staged_contents,
        test_partial_final_cleanup_reports_only_surviving_targets,
        test_recovery_probe_failure_preserves_original_error_json,
        test_verbose_diagnostic_failures_do_not_change_transaction_state,
        test_empty_batch_creates_no_transaction_state,
        test_agent_entry_points_are_byte_identical,
        test_manifest_hashes_match_canonical_files,
        test_ci_runs_canonical_suite_as_blocking_no_bytecode_gate,
    ]
    passed = 0
    failed = 0
    for test in tests:
        try:
            test()
            print(f"  PASS: {test.__name__}")
            passed += 1
        except AssertionError as error:
            print(f"  FAIL: {test.__name__}: {error}")
            failed += 1
        except Exception as error:
            print(f"  ERROR: {test.__name__}: {error}")
            failed += 1
    print(f"\n{passed} passed, {failed} failed")
    sys.exit(1 if failed else 0)
