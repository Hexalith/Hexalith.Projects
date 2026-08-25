#!/usr/bin/env python3
"""Destructive-path safety tests for cleanup-legacy.py.

Every cleanup run is isolated in a temporary directory. The same scenarios
exercise the installed setup skill and the setup-skill template, and the final
guards ensure all tracked agent entry points remain byte-identical.
"""

import json
import subprocess
import sys
import tempfile
from pathlib import Path

SKILLS_DIR = Path(__file__).resolve().parents[3]
PROJECT_ROOT = Path(__file__).resolve().parents[5]
SCRIPTS = [
    SKILLS_DIR / "bmad-bmb-setup" / "scripts" / "cleanup-legacy.py",
    SKILLS_DIR
    / "bmad-module-builder"
    / "assets"
    / "setup-skill-template"
    / "scripts"
    / "cleanup-legacy.py",
]
ENTRY_POINT_ROOTS = [
    PROJECT_ROOT / ".agent" / "skills",
    PROJECT_ROOT / ".agents" / "skills",
    PROJECT_ROOT / ".claude" / "skills",
]


def run_cleanup(
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


def run_runtime_failure(
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
    module.Path.is_dir = fail
    module.protect_live_migration_data([("bmb", target)])
elif mode == "skill-scan":
    module.Path.rglob = fail
    module.find_skill_dirs(target)
elif mode == "count":
    module.Path.rglob = fail
    module.count_files(target)
elif mode == "resolve-target":
    module.Path.is_symlink = fail
    module.resolve_cleanup_targets(str(target.parent), [target.name])
elif mode == "verify-legacy-scan":
    module.Path.is_dir = fail
    module.verify_skills_installed([("bmb", target)], extra)
elif mode == "verify-replacement":
    skills_dir = module.Path(extra)
    real_is_dir = module.Path.is_dir

    def selective_fail(self):
        if self == skills_dir or skills_dir in self.parents:
            raise OSError("simulated filesystem failure")
        return real_is_dir(self)

    module.Path.is_dir = selective_fail
    module.verify_skills_installed([("bmb", target)], extra)
else:
    module.shutil.rmtree = fail
    module.cleanup_directories([("bmb", target)])
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
    for script in SCRIPTS:
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

            code, data = run_cleanup(
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
    for script in SCRIPTS:
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

                code, data = run_cleanup(
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
    for script in SCRIPTS:
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

                    code, data = run_cleanup(
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
    for script in SCRIPTS:
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

                code, data = run_cleanup(
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
    for script in SCRIPTS:
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

            code, data = run_cleanup(script, bmad_dir, skills_dir=skills_dir)

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
    for script in SCRIPTS:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            bmad_dir = root / "_bmad"
            legacy_skill = make_skill(bmad_dir / "bmb" / "legacy")
            skills_dir = bmad_dir / "core" / "replacements"
            replacement = make_skill(skills_dir, legacy_skill.name)

            code, data = run_cleanup(script, bmad_dir, skills_dir=skills_dir)

            assert code == 1, data
            assert data["unsafe_skills"]
            assert_preserved(legacy_skill, replacement)


def test_filesystem_failures_use_runtime_json_contract():
    """Scan, count, stat, and deletion failures return JSON with exit code 2."""
    for script in SCRIPTS:
        for mode in ("live-marker", "skill-scan", "count", "delete"):
            with tempfile.TemporaryDirectory() as temp_dir:
                target = Path(temp_dir) / "bmb"
                target.mkdir()
                (target / "sentinel.txt").write_text("safe", encoding="utf-8")

                code, data = run_runtime_failure(script, target, mode)

                assert code == 2, (mode, data)
                assert data["status"] == "error"
                assert "simulated filesystem failure" in data["reason"]
                assert_preserved(target, target / "sentinel.txt")


def test_target_and_skill_inspection_failures_use_runtime_json_contract():
    """resolve_cleanup_targets and verify_skills_installed also honor the exit-2 contract."""
    for script in SCRIPTS:
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

                code, data = run_runtime_failure(script, target, mode, str(skills_dir))

                assert code == 2, (mode, data)
                assert data["status"] == "error"
                assert "simulated filesystem failure" in data["reason"]


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
    for script in SCRIPTS:
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

                code, data = run_cleanup(
                    script, bmad_dir, skills_dir=selected_skills_dir
                )

                assert code == 1, (variant, data)
                assert data["status"] == "error"
                assert data["missing_skills"] or data["unsafe_skills"]
                assert_preserved(legacy_skill, legacy_skill / "SKILL.md", core_sentinel)


def test_safe_non_skill_and_idempotent_cleanup():
    """Valid non-skill targets are removed and missing/non-directories remain benign."""
    for script in SCRIPTS:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            bmad_dir = root / "_bmad"
            for directory_name in ("bmb", "core", "extra"):
                sentinel = bmad_dir / directory_name / "retired.txt"
                sentinel.parent.mkdir(parents=True)
                sentinel.write_text("retired", encoding="utf-8")
            non_directory = bmad_dir / "plain-file"
            non_directory.write_text("keep", encoding="utf-8")

            code, data = run_cleanup(
                script,
                bmad_dir,
                also_remove=("extra", "plain-file", "missing", "extra"),
            )

            assert code == 0, data
            assert data["directories_removed"] == ["bmb", "core", "extra"]
            assert data["directories_not_found"] == ["plain-file", "missing"]
            assert_preserved(non_directory)

            code, data = run_cleanup(
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
    for script in SCRIPTS:
        code, data = run_cleanup(script, Path("{project-root}") / "_bmad")
        assert code == 1, data
        assert "Unresolved '{project-root}' token" in data["error"]

        with tempfile.TemporaryDirectory() as temp_dir:
            bmad_dir = Path(temp_dir) / "_bmad"
            sentinel = bmad_dir / "bmb" / "sentinel.txt"
            sentinel.parent.mkdir(parents=True)
            sentinel.write_text("safe", encoding="utf-8")
            code, data = run_cleanup(
                script,
                bmad_dir,
                skills_dir=Path("{project-root}") / ".claude" / "skills",
            )
            assert code == 1, data
            assert "Unresolved '{project-root}' token" in data["error"]
            assert_preserved(sentinel)


def test_all_tracked_copies_are_identical():
    """Installed, template, and test copies cannot drift across entry points."""
    installed_docs = [
        root / "bmad-bmb-setup" / "SKILL.md" for root in ENTRY_POINT_ROOTS
    ]
    template_docs = [
        root
        / "bmad-module-builder"
        / "assets"
        / "setup-skill-template"
        / "SKILL.md"
        for root in ENTRY_POINT_ROOTS
    ]
    installed_scripts = [
        root / "bmad-bmb-setup" / "scripts" / "cleanup-legacy.py"
        for root in ENTRY_POINT_ROOTS
    ]
    template_scripts = [
        root
        / "bmad-module-builder"
        / "assets"
        / "setup-skill-template"
        / "scripts"
        / "cleanup-legacy.py"
        for root in ENTRY_POINT_ROOTS
    ]
    test_copies = [
        root
        / "bmad-module-builder"
        / "scripts"
        / "tests"
        / "test-cleanup-legacy.py"
        for root in ENTRY_POINT_ROOTS
    ]
    copies = installed_scripts + template_scripts
    synchronized_groups = [installed_docs, template_docs, copies, test_copies]
    missing = [
        str(path)
        for group in synchronized_groups
        for path in group
        if not path.is_file()
    ]
    assert not missing, f"tracked cleanup copy missing: {missing}"
    for group in synchronized_groups:
        body = group[0].read_bytes()
        assert all(path.read_bytes() == body for path in group), (
            f"tracked cleanup copies have drifted: {[str(path) for path in group]}"
        )


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
        test_missing_or_unsafe_replacement_protects_every_candidate,
        test_safe_non_skill_and_idempotent_cleanup,
        test_unresolved_project_root_guard_is_preserved,
        test_all_tracked_copies_are_identical,
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
