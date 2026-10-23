#!/usr/bin/env python3
# /// script
# requires-python = ">=3.10"
# ///
"""Hermetic tests for legacy cleanup path safety and quarantine failures."""

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


def run_safety_cleanup(
    script: Path,
    bmad_dir: Path,
    module_code: str = "bmb",
    also_remove: tuple = (),
    skills_dir: Path = None,
) -> tuple[int, dict]:
    """Run a cleanup copy and return its exit code and JSON result."""
    command = [
        sys.executable,
        str(script),
        "--bmad-dir",
        str(bmad_dir),
        "--module-code",
        module_code,
    ]
    for directory_name in also_remove:
        command.extend(["--also-remove", str(directory_name)])
    if skills_dir is not None:
        command.extend(["--skills-dir", str(skills_dir)])

    result = subprocess.run(command, capture_output=True, text=True, timeout=10)
    try:
        data = json.loads(result.stdout)
    except json.JSONDecodeError as error:
        raise AssertionError(
            f"{script} did not return JSON: stdout={result.stdout!r}, "
            f"stderr={result.stderr!r}"
        ) from error
    return result.returncode, data



def run_safety_runtime_failure(
    script: Path, target: Path, mode: str, extra: str = None
) -> tuple[int, dict]:
    """Inject one filesystem failure and return the subprocess JSON result."""
    harness = r'''
import importlib.util
import sys

spec = importlib.util.spec_from_file_location("cleanup_legacy", sys.argv[1])
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
target = module.Path(sys.argv[2])
mode = sys.argv[3]
extra = sys.argv[4] if len(sys.argv) > 4 else None

def fail(*args, **kwargs):
    raise OSError("simulated filesystem failure")

if mode == "live-marker":
    module.Path.stat = fail
    module.protect_live_migration_data([("bmb", target)])
elif mode == "skill-scan":
    module.os.scandir = fail
    module.find_skill_dirs(target)
elif mode == "resolve-target":
    module.Path.lstat = fail
    module.resolve_cleanup_targets(str(target.parent), [target.name])
elif mode == "verify-legacy-scan":
    module.Path.stat = fail
    module.verify_skills_installed([("bmb", target)], extra)
elif mode == "verify-replacement":
    skills_dir = module.Path(extra)
    real_stat = module.Path.stat

    def selective_fail(self, *args, **kwargs):
        if self == skills_dir or skills_dir in self.parents:
            raise OSError("simulated filesystem failure")
        return real_stat(self, *args, **kwargs)

    module.Path.stat = selective_fail
    module.verify_skills_installed([("bmb", target)], extra)
'''
    command = [sys.executable, "-c", harness, str(script), str(target), mode]
    if extra is not None:
        command.append(extra)
    result = subprocess.run(
        command,
        capture_output=True,
        text=True,
        timeout=10,
    )
    try:
        data = json.loads(result.stdout)
    except json.JSONDecodeError as error:
        raise AssertionError(
            f"{script} runtime failure did not return JSON: "
            f"stdout={result.stdout!r}, stderr={result.stderr!r}"
        ) from error
    return result.returncode, data



def make_skill(parent: Path, skill_name: str = "demo-skill") -> Path:
    """Create and return a regular skill directory."""
    skill_dir = parent / skill_name
    skill_dir.mkdir(parents=True)
    (skill_dir / "SKILL.md").write_text("# Demo skill\n", encoding="utf-8")
    return skill_dir



def assert_preserved(*paths: Path) -> None:
    """Assert that all safety sentinels still exist."""
    missing = [str(path) for path in paths if not path.exists()]
    assert not missing, f"cleanup changed protected paths: {missing}"



def test_verified_duplicate_removal():
    """Verified external skill replacements allow transactional cleanup."""
    for script in LOCAL_SCRIPTS:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            bmad_dir = root / "_bmad"
            legacy_skill = make_skill(bmad_dir / "bmb" / "skills")
            core_sentinel = bmad_dir / "core" / "retired.txt"
            core_sentinel.parent.mkdir(parents=True)
            core_sentinel.write_text("retired", encoding="utf-8")
            extra_sentinel = bmad_dir / "_config" / "retired.txt"
            extra_sentinel.parent.mkdir(parents=True)
            extra_sentinel.write_text("retired", encoding="utf-8")
            skills_dir = root / ".claude" / "skills"
            replacement = make_skill(skills_dir, legacy_skill.name)

            code, data = run_safety_cleanup(
                script,
                bmad_dir,
                also_remove=("_config",),
                skills_dir=skills_dir,
            )

            assert code == 0, data
            assert data["status"] == "success"
            assert data["directories_removed"] == ["bmb", "core", "_config"]
            assert data["safety_checks"]["verified_skills"] == [legacy_skill.name]
            assert_preserved(replacement, replacement / "SKILL.md")
            assert not (bmad_dir / "bmb").exists()
            assert not (bmad_dir / "core").exists()
            assert not (bmad_dir / "_config").exists()



def test_escaping_targets_fail_before_any_cleanup():
    """Every unsafe path form and target collision preserves the whole batch."""
    cases = (
        "absolute-module",
        "absolute-also",
        "traversal-module",
        "traversal-also",
        "nested-module",
        "dotdot-module",
        "dotdot-also",
        "external-symlink",
        "internal-symlink",
        "collision",
    )
    for script in LOCAL_SCRIPTS:
        for escape_kind in cases:
            with tempfile.TemporaryDirectory() as temp_dir:
                root = Path(temp_dir)
                bmad_dir = root / "_bmad"
                safe_sentinel = bmad_dir / "safe" / "sentinel.txt"
                safe_sentinel.parent.mkdir(parents=True)
                safe_sentinel.write_text("safe", encoding="utf-8")
                core_sentinel = bmad_dir / "core" / "sentinel.txt"
                core_sentinel.parent.mkdir(parents=True)
                core_sentinel.write_text("core", encoding="utf-8")
                outside_sentinel = root / "outside" / "sentinel.txt"
                outside_sentinel.parent.mkdir(parents=True)
                outside_sentinel.write_text("outside", encoding="utf-8")
                sibling_sentinel = bmad_dir / "sibling" / "sentinel.txt"
                sibling_sentinel.parent.mkdir(parents=True)
                sibling_sentinel.write_text("sibling", encoding="utf-8")

                if escape_kind == "absolute-module":
                    module_code = str(outside_sentinel.parent)
                    also_remove = ("safe",)
                    rejected = module_code
                elif escape_kind == "absolute-also":
                    module_code = "safe"
                    also_remove = (str(outside_sentinel.parent),)
                    rejected = str(outside_sentinel.parent)
                elif escape_kind == "traversal-module":
                    module_code = "../outside"
                    also_remove = ("safe",)
                    rejected = module_code
                elif escape_kind == "traversal-also":
                    module_code = "safe"
                    also_remove = ("../outside",)
                    rejected = "../outside"
                elif escape_kind == "nested-module":
                    module_code = "nested/target"
                    also_remove = ("safe",)
                    rejected = module_code
                elif escape_kind == "dotdot-module":
                    module_code = ".."
                    also_remove = ("safe",)
                    rejected = ".."
                elif escape_kind == "dotdot-also":
                    module_code = "safe"
                    also_remove = ("..",)
                    rejected = ".."
                elif escape_kind == "external-symlink":
                    escape_link = bmad_dir / "escape"
                    escape_link.symlink_to(
                        outside_sentinel.parent, target_is_directory=True
                    )
                    module_code = "safe"
                    also_remove = ("escape",)
                    rejected = "escape"
                elif escape_kind == "internal-symlink":
                    escape_link = bmad_dir / "escape"
                    escape_link.symlink_to(
                        sibling_sentinel.parent, target_is_directory=True
                    )
                    module_code = "safe"
                    also_remove = ("escape",)
                    rejected = "escape"
                else:
                    module_code = "safe"
                    also_remove = ("safe/",)
                    rejected = "safe"

                code, data = run_safety_cleanup(
                    script,
                    bmad_dir,
                    module_code=module_code,
                    also_remove=also_remove,
                )

                assert code == 1, data
                assert data["status"] == "error"
                assert rejected in data["rejected_targets"]
                assert_preserved(
                    safe_sentinel,
                    core_sentinel,
                    outside_sentinel,
                    sibling_sentinel,
                )



def test_live_migration_data_protects_every_candidate():
    """Either live marker blocks deletion of the full target batch."""
    for script in LOCAL_SCRIPTS:
        for protected_name in ("bmb", "core", "extra"):
            for marker_name in ("config.yaml", "module-help.csv"):
                with tempfile.TemporaryDirectory() as temp_dir:
                    root = Path(temp_dir)
                    bmad_dir = root / "_bmad"
                    bmb_sentinel = bmad_dir / "bmb" / "sentinel.txt"
                    bmb_sentinel.parent.mkdir(parents=True)
                    bmb_sentinel.write_text("bmb", encoding="utf-8")
                    core_sentinel = bmad_dir / "core" / "sentinel.txt"
                    core_sentinel.parent.mkdir(parents=True)
                    core_sentinel.write_text("core", encoding="utf-8")
                    extra_sentinel = bmad_dir / "extra" / "sentinel.txt"
                    extra_sentinel.parent.mkdir(parents=True)
                    extra_sentinel.write_text("extra", encoding="utf-8")
                    live_marker = bmad_dir / protected_name / marker_name
                    live_marker.write_text("live", encoding="utf-8")

                    code, data = run_safety_cleanup(
                        script, bmad_dir, also_remove=("extra",)
                    )

                    assert code == 1, data
                    assert data["status"] == "error"
                    protected = data["protected_directories"]
                    evidence = next(
                        item
                        for item in protected
                        if item["directory"] == protected_name
                    )
                    assert marker_name in evidence["live_migration_files"]
                    assert_preserved(
                        live_marker, bmb_sentinel, core_sentinel, extra_sentinel
                    )



def test_late_skill_failure_protects_earlier_candidates():
    """A missing skill in core or also-remove blocks earlier removable targets."""
    for script in LOCAL_SCRIPTS:
        for failing_name in ("core", "extra"):
            with tempfile.TemporaryDirectory() as temp_dir:
                root = Path(temp_dir)
                bmad_dir = root / "_bmad"
                bmb_sentinel = bmad_dir / "bmb" / "sentinel.txt"
                bmb_sentinel.parent.mkdir(parents=True)
                bmb_sentinel.write_text("bmb", encoding="utf-8")
                core_sentinel = bmad_dir / "core" / "sentinel.txt"
                core_sentinel.parent.mkdir(parents=True)
                core_sentinel.write_text("core", encoding="utf-8")
                extra_sentinel = bmad_dir / "extra" / "sentinel.txt"
                extra_sentinel.parent.mkdir(parents=True)
                extra_sentinel.write_text("extra", encoding="utf-8")
                legacy_skill = make_skill(bmad_dir / failing_name / "legacy")
                skills_dir = root / ".claude" / "skills"
                skills_dir.mkdir(parents=True)

                code, data = run_safety_cleanup(
                    script,
                    bmad_dir,
                    also_remove=("extra",),
                    skills_dir=skills_dir,
                )

                assert code == 1, data
                assert any(
                    item["skill"] == legacy_skill.name
                    for item in data["missing_skills"]
                )
                assert_preserved(
                    bmb_sentinel, core_sentinel, extra_sentinel, legacy_skill
                )



def test_one_missing_replacement_blocks_multiple_legacy_skills():
    """One later missing replacement blocks a target with an earlier valid skill."""
    for script in LOCAL_SCRIPTS:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            bmad_dir = root / "_bmad"
            valid_legacy = make_skill(bmad_dir / "bmb" / "legacy", "alpha-skill")
            missing_legacy = make_skill(bmad_dir / "bmb" / "legacy", "zeta-skill")
            core_sentinel = bmad_dir / "core" / "sentinel.txt"
            core_sentinel.parent.mkdir(parents=True)
            core_sentinel.write_text("core", encoding="utf-8")
            skills_dir = root / ".claude" / "skills"
            valid_replacement = make_skill(skills_dir, valid_legacy.name)

            code, data = run_safety_cleanup(script, bmad_dir, skills_dir=skills_dir)

            assert code == 1, data
            assert any(
                item["skill"] == missing_legacy.name
                for item in data["missing_skills"]
            )
            assert_preserved(
                valid_legacy,
                missing_legacy,
                valid_replacement,
                core_sentinel,
            )



def test_replacement_inside_different_cleanup_target_is_rejected():
    """A replacement under core cannot justify deleting a bmb legacy skill."""
    for script in LOCAL_SCRIPTS:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            bmad_dir = root / "_bmad"
            legacy_skill = make_skill(bmad_dir / "bmb" / "legacy")
            skills_dir = bmad_dir / "core" / "replacements"
            replacement = make_skill(skills_dir, legacy_skill.name)

            code, data = run_safety_cleanup(script, bmad_dir, skills_dir=skills_dir)

            assert code == 1, data
            assert data["unsafe_skills"]
            assert_preserved(legacy_skill, replacement)



def test_filesystem_failures_use_runtime_json_contract():
    """Live-marker and skill scan failures return JSON with exit code 2."""
    for script in LOCAL_SCRIPTS:
        for mode in ("live-marker", "skill-scan"):
            with tempfile.TemporaryDirectory() as temp_dir:
                target = Path(temp_dir) / "bmb"
                target.mkdir()
                (target / "sentinel.txt").write_text("safe", encoding="utf-8")

                code, data = run_safety_runtime_failure(script, target, mode)

                assert code == 2, (mode, data)
                assert data["status"] == "error"
                assert "simulated filesystem failure" in data["reason"]
                assert_preserved(target, target / "sentinel.txt")
                assert not transaction_directories(target.parent)



def test_target_and_skill_inspection_failures_use_runtime_json_contract():
    """resolve_cleanup_targets and verify_skills_installed also honor the exit-2 contract."""
    for script in LOCAL_SCRIPTS:
        for mode in ("resolve-target", "verify-legacy-scan", "verify-replacement"):
            with tempfile.TemporaryDirectory() as temp_dir:
                root = Path(temp_dir)
                bmad_dir = root / "_bmad"
                target = bmad_dir / "bmb"
                skills_dir = root / ".claude" / "skills"
                skills_dir.mkdir(parents=True)

                if mode == "resolve-target":
                    target.mkdir(parents=True)
                else:
                    make_skill(target, "demo-skill")

                code, data = run_safety_runtime_failure(script, target, mode, str(skills_dir))

                assert code == 2, (mode, data)
                assert data["status"] == "error"
                assert "simulated filesystem failure" in data["reason"]



def test_underlying_filesystem_errors_cannot_hide_protected_data():
    """Real CLI calls propagate low-level scan/stat faults before any staging."""
    harness = r'''
import importlib.util
import os
import sys

script, bmad_dir, fault_path, mode = sys.argv[1:]
spec = importlib.util.spec_from_file_location("cleanup_low_level_fault", script)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
triggered = False

def inject(real, *args, **kwargs):
    global triggered
    if args and not isinstance(args[0], int) and os.fspath(args[0]) == fault_path:
        # Keep metadata faults active across both old predicate probes;
        # a scan fault is one-shot so a subsequent deletion could succeed.
        if mode != "scan" or not triggered:
            triggered = True
            raise PermissionError("simulated underlying filesystem failure")
    return real(*args, **kwargs)

real_stat, real_lstat, real_scandir = os.stat, os.lstat, os.scandir
if mode == "scan":
    os.scandir = lambda *a, **kw: inject(real_scandir, *a, **kw)
elif mode == "marker":
    os.stat = lambda *a, **kw: inject(real_stat, *a, **kw)
    os.lstat = lambda *a, **kw: inject(real_lstat, *a, **kw)
else:
    os.lstat = lambda *a, **kw: inject(real_lstat, *a, **kw)
sys.argv = [script, "--bmad-dir", bmad_dir, "--module-code", "bmb"]
try:
    module.main()
finally:
    if not triggered:
        raise AssertionError("low-level fault was not exercised")
'''
    for script in LOCAL_SCRIPTS:
        for mode in ("scan", "marker", "target-link"):
            with tempfile.TemporaryDirectory() as temporary:
                bmad_dir = Path(temporary) / "_bmad"
                bmad_dir.mkdir()
                core = write_target(bmad_dir, "core", b"core must survive")
                if mode == "target-link":
                    target = write_target(bmad_dir, "sibling", b"sibling must survive")
                    fault_path = bmad_dir / "bmb"
                    fault_path.symlink_to(target, target_is_directory=True)
                else:
                    target = write_target(bmad_dir, "bmb", b"target must survive")
                    if mode == "scan":
                        fault_path = target / "secret"
                        make_skill(fault_path, "uninstalled-skill")
                    else:
                        fault_path = target / "config.yaml"
                        fault_path.write_text("live migration data", encoding="utf-8")
                before = {
                    path.relative_to(bmad_dir): path.read_bytes()
                    for path in bmad_dir.rglob("*") if path.is_file()
                }
                result = subprocess.run(
                    [sys.executable, "-c", harness, str(script), str(bmad_dir), str(fault_path), mode],
                    capture_output=True, text=True, timeout=10,
                )
                assert result.returncode == 2, (script, mode, result.stdout, result.stderr)
                assert not result.stderr, result.stderr
                data = json.loads(result.stdout)
                assert data["status"] == "error"
                assert "simulated underlying filesystem failure" in data["reason"]
                assert target.is_dir() and core.is_dir()
                for relative, contents in before.items():
                    assert (bmad_dir / relative).read_bytes() == contents
                if mode == "target-link":
                    assert fault_path.is_symlink()
                assert not transaction_directories(bmad_dir)


def test_missing_or_unsafe_replacement_protects_every_candidate():
    """Unproven, false, internal, and aliased replacements all fail closed."""
    variants = (
        "no-skills-dir",
        "missing-directory",
        "missing-skill-md",
        "symlink-skill-md",
        "symlink-loop",
        "inside-cleanup-target",
        "alias-to-legacy",
    )
    for script in LOCAL_SCRIPTS:
        for variant in variants:
            with tempfile.TemporaryDirectory() as temp_dir:
                root = Path(temp_dir)
                bmad_dir = root / "_bmad"
                legacy_skill = make_skill(bmad_dir / "bmb" / "legacy")
                core_sentinel = bmad_dir / "core" / "sentinel.txt"
                core_sentinel.parent.mkdir(parents=True)
                core_sentinel.write_text("core", encoding="utf-8")
                skills_dir = root / ".claude" / "skills"

                if variant == "no-skills-dir":
                    selected_skills_dir = None
                elif variant == "missing-directory":
                    skills_dir.mkdir(parents=True)
                    selected_skills_dir = skills_dir
                elif variant == "missing-skill-md":
                    (skills_dir / legacy_skill.name).mkdir(parents=True)
                    selected_skills_dir = skills_dir
                elif variant == "symlink-skill-md":
                    replacement = skills_dir / legacy_skill.name
                    replacement.mkdir(parents=True)
                    real_file = root / "shared-SKILL.md"
                    real_file.write_text("# Shared\n", encoding="utf-8")
                    (replacement / "SKILL.md").symlink_to(real_file)
                    selected_skills_dir = skills_dir
                elif variant == "symlink-loop":
                    skills_dir.mkdir(parents=True)
                    replacement = skills_dir / legacy_skill.name
                    replacement.symlink_to(replacement, target_is_directory=True)
                    selected_skills_dir = skills_dir
                elif variant == "inside-cleanup-target":
                    selected_skills_dir = bmad_dir / "bmb" / "replacements"
                    make_skill(selected_skills_dir, legacy_skill.name)
                else:
                    skills_dir.mkdir(parents=True)
                    (skills_dir / legacy_skill.name).symlink_to(
                        legacy_skill, target_is_directory=True
                    )
                    selected_skills_dir = skills_dir

                code, data = run_safety_cleanup(
                    script, bmad_dir, skills_dir=selected_skills_dir
                )

                assert code == 1, (variant, data)
                assert data["status"] == "error"
                assert data["missing_skills"] or data["unsafe_skills"]
                assert_preserved(legacy_skill, legacy_skill / "SKILL.md", core_sentinel)



def test_safe_non_skill_and_idempotent_cleanup():
    """Valid non-skill targets are removed and missing/non-directories remain benign."""
    for script in LOCAL_SCRIPTS:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            bmad_dir = root / "_bmad"
            for directory_name in ("bmb", "core", "extra"):
                sentinel = bmad_dir / directory_name / "retired.txt"
                sentinel.parent.mkdir(parents=True)
                sentinel.write_text("retired", encoding="utf-8")
            non_directory = bmad_dir / "plain-file"
            non_directory.write_text("keep", encoding="utf-8")

            code, data = run_safety_cleanup(
                script,
                bmad_dir,
                also_remove=("extra", "plain-file", "missing", "extra"),
            )

            assert code == 0, data
            assert data["directories_removed"] == ["bmb", "core", "extra"]
            assert data["directories_not_found"] == ["plain-file", "missing"]
            assert_preserved(non_directory)

            code, data = run_safety_cleanup(
                script, bmad_dir, also_remove=("extra", "plain-file", "missing")
            )
            assert code == 0, data
            assert data["directories_removed"] == []
            assert data["directories_not_found"] == [
                "bmb",
                "core",
                "extra",
                "plain-file",
                "missing",
            ]
            assert_preserved(non_directory)



def test_unresolved_project_root_guard_is_preserved():
    """Filesystem path arguments still reject a literal project-root token."""
    for script in LOCAL_SCRIPTS:
        code, data = run_safety_cleanup(script, Path("{project-root}") / "_bmad")
        assert code == 1, data
        assert "Unresolved '{project-root}' token" in data["error"]

        with tempfile.TemporaryDirectory() as temp_dir:
            bmad_dir = Path(temp_dir) / "_bmad"
            sentinel = bmad_dir / "bmb" / "sentinel.txt"
            sentinel.parent.mkdir(parents=True)
            sentinel.write_text("safe", encoding="utf-8")
            code, data = run_safety_cleanup(
                script,
                bmad_dir,
                skills_dir=Path("{project-root}") / ".claude" / "skills",
            )
            assert code == 1, data
            assert "Unresolved '{project-root}' token" in data["error"]
            assert_preserved(sentinel)



if __name__ == "__main__":
    tests = [
        test_verified_duplicate_removal,
        test_escaping_targets_fail_before_any_cleanup,
        test_live_migration_data_protects_every_candidate,
        test_late_skill_failure_protects_earlier_candidates,
        test_one_missing_replacement_blocks_multiple_legacy_skills,
        test_replacement_inside_different_cleanup_target_is_rejected,
        test_filesystem_failures_use_runtime_json_contract,
        test_target_and_skill_inspection_failures_use_runtime_json_contract,
        test_underlying_filesystem_errors_cannot_hide_protected_data,
        test_missing_or_unsafe_replacement_protects_every_candidate,
        test_safe_non_skill_and_idempotent_cleanup,
        test_unresolved_project_root_guard_is_preserved,
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
