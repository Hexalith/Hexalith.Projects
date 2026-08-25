#!/usr/bin/env python3
# /// script
# requires-python = ">=3.9"
# dependencies = ["pyyaml"]
# ///
"""Failure-atomic regression tests for the BMAD configuration merge CLIs."""

import argparse
import contextlib
import csv
import hashlib
import importlib.util
import io
import json
import os
import stat
import subprocess
import sys
import tempfile
from pathlib import Path
from typing import Optional

import yaml


PROJECT_ROOT = Path(__file__).resolve().parents[5]
ENTRY_POINT_ROOTS = [
    PROJECT_ROOT / ".agent" / "skills",
    PROJECT_ROOT / ".agents" / "skills",
    PROJECT_ROOT / ".claude" / "skills",
]
SCRIPT_RELATIVES = [
    Path("bmad-bmb-setup/scripts"),
    Path("bmad-module-builder/assets/setup-skill-template/scripts"),
    Path("bmad-module-builder/assets/standalone-module-template"),
]
CONFIG_SCRIPTS = [
    root / relative / "merge-config.py"
    for root in ENTRY_POINT_ROOTS
    for relative in SCRIPT_RELATIVES
]
HELP_SCRIPTS = [
    root / relative / "merge-help-csv.py"
    for root in ENTRY_POINT_ROOTS
    for relative in SCRIPT_RELATIVES
]
TEST_COPIES = [
    root / "bmad-module-builder/scripts/tests/test-merge-atomicity.py"
    for root in ENTRY_POINT_ROOTS
]
BMB_HELP_SOURCE = PROJECT_ROOT / ".agents" / "skills" / "bmad-bmb-setup" / "assets" / "module-help.csv"
CONFIG_SUCCESS_KEYS = {
    "status",
    "config_path",
    "user_config_path",
    "module_code",
    "core_updated",
    "module_keys",
    "user_keys",
    "legacy_configs_found",
    "legacy_configs_deleted",
}
HELP_SUCCESS_KEYS = {
    "status",
    "target_path",
    "target_existed",
    "module_codes",
    "rows_removed",
    "rows_added",
    "total_rows",
    "legacy_csvs_deleted",
}
CSV_HEADER = (
    "module,skill,display-name,menu-code,description,action,args,phase,after,before,"
    "required,output-location,outputs\n"
)


def run_cli(script: Path, arguments: list[str]) -> tuple[int, str, str]:
    """Run one tracked CLI copy and return its process result."""
    result = subprocess.run(
        [sys.executable, str(script), *arguments],
        capture_output=True,
        text=True,
        timeout=15,
    )
    return result.returncode, result.stdout, result.stderr


def config_arguments(paths: dict[str, Path]) -> list[str]:
    """Build the standard config CLI argument list."""
    arguments = [
        "--config-path",
        str(paths["config"]),
        "--user-config-path",
        str(paths["user"]),
        "--module-yaml",
        str(paths["module"]),
        "--answers",
        str(paths["answers"]),
    ]
    if "legacy" in paths:
        arguments.extend(["--legacy-dir", str(paths["legacy"])])
    return arguments


def help_arguments(
    paths: dict[str, Path], include_module: bool = True, module_code: str = "demo"
) -> list[str]:
    """Build the standard help CLI argument list."""
    arguments = ["--target", str(paths["target"]), "--source", str(paths["source"])]
    if "legacy" in paths:
        arguments.extend(["--legacy-dir", str(paths["legacy"])])
    if include_module:
        arguments.extend(["--module-code", module_code])
    return arguments


def prepare_config(root: Path, existing: bool = True, legacy: bool = True) -> dict[str, Path]:
    """Create valid module, answers, targets, and optional legacy inputs."""
    paths = {
        "config": root / "shared" / "config.yaml",
        "user": root / "shared" / "config.user.yaml",
        "module": root / "inputs" / "module.yaml",
        "answers": root / "inputs" / "answers.json",
    }
    paths["module"].parent.mkdir(parents=True)
    paths["module"].write_text(
        "code: demo\n"
        "name: Demo Module\n"
        "description: Demo description\n"
        "module_version: 2.0.0\n"
        "choice:\n"
        "  user_setting: true\n"
        "legacy_value:\n"
        "  prompt: Legacy value\n",
        encoding="utf-8",
    )
    paths["answers"].write_text(
        json.dumps(
            {
                "core": {
                    "user_name": "Jerome",
                    "communication_language": "English",
                    "document_output_language": "English",
                },
                "module": {"choice": "answer-value"},
            }
        ),
        encoding="utf-8",
    )
    if existing:
        paths["config"].parent.mkdir(parents=True)
        paths["config"].write_text(
            "untouched:\n  value: keep\ndemo:\n  stale: remove\nuser_name: old\n",
            encoding="utf-8",
        )
        paths["user"].write_text("retained: keep\nchoice: stale\n", encoding="utf-8")
        paths["config"].chmod(0o640)
        paths["user"].chmod(0o600)
    if legacy:
        paths["legacy"] = root / "legacy"
        module_legacy = paths["legacy"] / "demo" / "config.yaml"
        core_legacy = paths["legacy"] / "core" / "config.yaml"
        module_legacy.parent.mkdir(parents=True)
        core_legacy.parent.mkdir(parents=True)
        module_legacy.write_text(
            "legacy_value: from-legacy\nchoice: ignored-legacy\n",
            encoding="utf-8",
        )
        core_legacy.write_text("output_folder: legacy-output\n", encoding="utf-8")
    return paths


def prepare_help(root: Path, existing: bool = True, legacy: bool = True) -> dict[str, Path]:
    """Create valid source, target, and optional legacy help CSV files."""
    paths = {"target": root / "shared" / "module-help.csv", "source": root / "inputs" / "module-help.csv"}
    paths["source"].parent.mkdir(parents=True)
    paths["source"].write_text(
        CSV_HEADER
        + "Demo Module,demo-setup,Setup Demo,SD,Setup,configure,,anytime,,,false,out,config\n",
        encoding="utf-8",
    )
    if existing:
        paths["target"].parent.mkdir(parents=True)
        paths["target"].write_text(
            CSV_HEADER
            + "Demo Module,stale,Stale,S,Stale,run,,anytime,,,false,out,old\n"
            + "Other Module,other,Other,O,Other,run,,anytime,,,false,out,other\n",
            encoding="utf-8",
        )
        paths["target"].chmod(0o604)
    if legacy:
        paths["legacy"] = root / "legacy"
        for subdir in ("demo", "core"):
            legacy_file = paths["legacy"] / subdir / "module-help.csv"
            legacy_file.parent.mkdir(parents=True)
            legacy_file.write_text("legacy\n", encoding="utf-8")
    return paths


def snapshot(path: Path) -> tuple[bool, bytes, Optional[int]]:
    """Capture one target's existence, bytes, and permission mode."""
    if not path.exists():
        return False, b"", None
    return True, path.read_bytes(), stat.S_IMODE(path.stat().st_mode)


def assert_snapshot(path: Path, expected: tuple[bool, bytes, Optional[int]]) -> None:
    """Assert one target retains its captured existence, bytes, and mode."""
    actual = snapshot(path)
    assert actual == expected, f"state changed for {path}: expected={expected}, actual={actual}"


def legacy_files(paths: dict[str, Path], filename: str) -> list[Path]:
    """Return the module and core legacy paths for one fixture."""
    return [paths["legacy"] / subdir / filename for subdir in ("demo", "core")]


def assert_no_artifacts(root: Path) -> None:
    """Assert every staging and rollback artifact was removed."""
    artifacts = [
        path
        for path in root.rglob("*")
        if ".stage-" in path.name or ".rollback-" in path.name
    ]
    assert not artifacts, f"disposable merge artifacts remain: {artifacts}"


def load_module(script: Path):
    """Load one hyphenated script path as a uniquely named module."""
    module_name = f"merge_atomicity_{hashlib.sha256(str(script).encode()).hexdigest()}"
    specification = importlib.util.spec_from_file_location(module_name, script)
    module = importlib.util.module_from_spec(specification)
    specification.loader.exec_module(module)
    return module


def invoke_main(module, arguments: list[str]) -> tuple[int, str, str]:
    """Invoke an imported CLI main and retain its exit-code contract."""
    original_argv = sys.argv
    stdout = io.StringIO()
    stderr = io.StringIO()
    sys.argv = [str(module.__file__), *arguments]
    try:
        with contextlib.redirect_stdout(stdout), contextlib.redirect_stderr(stderr):
            try:
                module.main()
                code = 0
            except SystemExit as error:
                code = int(error.code)
    finally:
        sys.argv = original_argv
    return code, stdout.getvalue(), stderr.getvalue()


def expected_create_mode() -> int:
    """Return the process's normal regular-file create mode."""
    process_umask = os.umask(0)
    os.umask(process_umask)
    return 0o666 & ~process_umask


def test_valid_existing_merges_preserve_semantics_modes_and_cleanup():
    """Every copy preserves anti-zombie/fallback behavior, modes, and JSON summaries."""
    for script in CONFIG_SCRIPTS:
        with tempfile.TemporaryDirectory() as temporary:
            paths = prepare_config(Path(temporary))
            code, stdout, stderr = run_cli(script, config_arguments(paths))
            assert code == 0, (script, stdout, stderr)
            data = json.loads(stdout)
            assert set(data) == CONFIG_SUCCESS_KEYS
            config = yaml.safe_load(paths["config"].read_text(encoding="utf-8"))
            user = yaml.safe_load(paths["user"].read_text(encoding="utf-8"))
            assert config["untouched"] == {"value": "keep"}
            assert config["demo"]["choice"] == "answer-value"
            assert config["demo"]["legacy_value"] == "from-legacy"
            assert config["output_folder"] == "legacy-output"
            assert "user_name" not in config
            assert user == {
                "retained": "keep",
                "choice": "answer-value",
                "user_name": "Jerome",
                "communication_language": "English",
            }
            assert stat.S_IMODE(paths["config"].stat().st_mode) == 0o640
            assert stat.S_IMODE(paths["user"].stat().st_mode) == 0o600
            assert all(not path.exists() for path in legacy_files(paths, "config.yaml"))
            assert_no_artifacts(Path(temporary))

    for script in HELP_SCRIPTS:
        with tempfile.TemporaryDirectory() as temporary:
            paths = prepare_help(Path(temporary))
            code, stdout, stderr = run_cli(script, help_arguments(paths))
            assert code == 0, (script, stdout, stderr)
            data = json.loads(stdout)
            assert set(data) == HELP_SUCCESS_KEYS
            rows = list(csv.reader(paths["target"].read_text(encoding="utf-8").splitlines()))
            assert [row[0] for row in rows[1:]] == ["Other Module", "Demo Module"]
            assert rows[-1][1] == "demo-setup"
            assert stat.S_IMODE(paths["target"].stat().st_mode) == 0o604
            assert all(not path.exists() for path in legacy_files(paths, "module-help.csv"))
            assert_no_artifacts(Path(temporary))


def test_new_targets_use_normal_create_mode_for_every_copy():
    """Fresh atomic replacements retain normal process create-mode semantics."""
    mode = expected_create_mode()
    for script in CONFIG_SCRIPTS:
        with tempfile.TemporaryDirectory() as temporary:
            paths = prepare_config(Path(temporary), existing=False, legacy=False)
            code, stdout, stderr = run_cli(script, config_arguments(paths))
            assert code == 0, (script, stdout, stderr)
            assert stat.S_IMODE(paths["config"].stat().st_mode) == mode
            assert stat.S_IMODE(paths["user"].stat().st_mode) == mode
            assert_no_artifacts(Path(temporary))
    for script in HELP_SCRIPTS:
        with tempfile.TemporaryDirectory() as temporary:
            paths = prepare_help(Path(temporary), existing=False, legacy=False)
            code, stdout, stderr = run_cli(script, help_arguments(paths))
            assert code == 0, (script, stdout, stderr)
            assert stat.S_IMODE(paths["target"].stat().st_mode) == mode
            assert_no_artifacts(Path(temporary))


def test_empty_existing_yaml_documents_remain_valid_for_every_copy():
    """Empty existing config documents retain their historical empty-mapping meaning."""
    for script in CONFIG_SCRIPTS:
        for target_name in ("config", "user"):
            with tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary)
                paths = prepare_config(root, legacy=False)
                paths[target_name].write_text("", encoding="utf-8")
                code, stdout, stderr = run_cli(script, config_arguments(paths))
                assert code == 0, (script, target_name, stdout, stderr)
                assert isinstance(
                    yaml.safe_load(paths[target_name].read_text(encoding="utf-8")), dict
                )
                assert_no_artifacts(root)


def test_invalid_cleanup_arguments_and_documents_preserve_all_state():
    """Late-failure regressions now stop with exit 1 before publication or cleanup."""
    for script in HELP_SCRIPTS:
        with tempfile.TemporaryDirectory() as temporary:
            paths = prepare_help(Path(temporary))
            target_before = snapshot(paths["target"])
            legacy_before = [snapshot(path) for path in legacy_files(paths, "module-help.csv")]
            code, _, _ = run_cli(script, help_arguments(paths, include_module=False))
            assert code == 1
            assert_snapshot(paths["target"], target_before)
            for path, expected in zip(legacy_files(paths, "module-help.csv"), legacy_before):
                assert_snapshot(path, expected)
            assert_no_artifacts(Path(temporary))

    malformed_values = ("[]\n", "not: [valid\n")
    for script in CONFIG_SCRIPTS:
        for target_name in ("config", "user"):
            for malformed in malformed_values:
                with tempfile.TemporaryDirectory() as temporary:
                    paths = prepare_config(Path(temporary))
                    paths[target_name].write_text(malformed, encoding="utf-8")
                    pair_before = [snapshot(paths[name]) for name in ("config", "user")]
                    legacy_before = [snapshot(path) for path in legacy_files(paths, "config.yaml")]
                    code, _, _ = run_cli(script, config_arguments(paths))
                    assert code == 1
                    for name, expected in zip(("config", "user"), pair_before):
                        assert_snapshot(paths[name], expected)
                    for path, expected in zip(legacy_files(paths, "config.yaml"), legacy_before):
                        assert_snapshot(path, expected)
                    assert_no_artifacts(Path(temporary))


def test_non_mapping_module_answers_and_legacy_inputs_are_rejected():
    """Every YAML/JSON input must be a mapping before either target is published."""
    for script in CONFIG_SCRIPTS:
        for variant in ("module", "answers", "legacy"):
            with tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary)
                paths = prepare_config(root)
                if variant == "module":
                    paths["module"].write_text("[]\n", encoding="utf-8")
                elif variant == "answers":
                    paths["answers"].write_text("[]\n", encoding="utf-8")
                else:
                    (paths["legacy"] / "core" / "config.yaml").write_text(
                        "[]\n", encoding="utf-8"
                    )
                pair_before = [snapshot(paths[name]) for name in ("config", "user")]
                legacy_before = [snapshot(path) for path in legacy_files(paths, "config.yaml")]
                code, _, _ = run_cli(script, config_arguments(paths))
                assert code == 1, (script, variant)
                for name, expected in zip(("config", "user"), pair_before):
                    assert_snapshot(paths[name], expected)
                for path, expected in zip(legacy_files(paths, "config.yaml"), legacy_before):
                    assert_snapshot(path, expected)
                assert_no_artifacts(root)


def test_unsafe_and_mismatched_module_codes_fail_before_mutation():
    """Traversal, reserved, and source-mismatched cleanup scopes are rejected."""
    for script in HELP_SCRIPTS:
        for module_code in (
            "../escape",
            "nested/name",
            "core",
            "con",
            "con.txt",
            "PRN.yaml",
            "COM1.foo",
            "demo.",
            "wrong",
        ):
            with tempfile.TemporaryDirectory() as temporary:
                paths = prepare_help(Path(temporary))
                before = snapshot(paths["target"])
                code, _, _ = run_cli(script, help_arguments(paths, module_code=module_code))
                assert code == 1, (script, module_code)
                assert_snapshot(paths["target"], before)
                assert all(path.exists() for path in legacy_files(paths, "module-help.csv"))
    for script in CONFIG_SCRIPTS:
        for module_code in (
            "../escape",
            "nested/name",
            "core",
            "con",
            "con.txt",
            "PRN.yaml",
            "COM1.foo",
            "user_name",
            "communication_language",
            "document_output_language",
            "output_folder",
            "demo.",
        ):
            with tempfile.TemporaryDirectory() as temporary:
                paths = prepare_config(Path(temporary))
                paths["module"].write_text(f"code: {module_code!r}\n", encoding="utf-8")
                before = [snapshot(paths[name]) for name in ("config", "user")]
                code, _, _ = run_cli(script, config_arguments(paths))
                assert code == 1, (script, module_code)
                for name, expected in zip(("config", "user"), before):
                    assert_snapshot(paths[name], expected)


def test_aliases_case_collisions_and_nested_outputs_are_rejected():
    """Lexical, hard-link, case-only, nested, input, and cleanup aliases fail closed."""
    for script in CONFIG_SCRIPTS:
        variants = ("hard-link", "case", "nested", "output-input", "output-cleanup", "input-cleanup")
        for variant in variants:
            with tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary)
                paths = prepare_config(root)
                if variant == "hard-link":
                    paths["user"].unlink()
                    os.link(paths["config"], paths["user"])
                elif variant == "case":
                    paths["user"] = paths["config"].with_name("CONFIG.YAML")
                elif variant == "nested":
                    paths["user"] = paths["config"] / "config.user.yaml"
                elif variant == "output-input":
                    paths["config"] = paths["module"]
                elif variant == "output-cleanup":
                    paths["config"] = paths["legacy"] / "demo" / "config.yaml"
                else:
                    paths["answers"] = paths["legacy"] / "demo" / "config.yaml"
                watched = {path: snapshot(path) for path in set(paths.values()) if path.is_file()}
                code, _, _ = run_cli(script, config_arguments(paths))
                assert code == 1, (script, variant)
                for path, expected in watched.items():
                    assert_snapshot(path, expected)
                assert_no_artifacts(root)

    for script in HELP_SCRIPTS:
        for variant in ("hard-link", "case", "target-cleanup", "source-cleanup"):
            with tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary)
                paths = prepare_help(root)
                if variant == "hard-link":
                    paths["target"].unlink()
                    os.link(paths["source"], paths["target"])
                elif variant == "case":
                    paths["target"] = paths["source"].with_name("MODULE-HELP.CSV")
                elif variant == "target-cleanup":
                    paths["target"] = paths["legacy"] / "demo" / "module-help.csv"
                else:
                    paths["source"] = paths["legacy"] / "demo" / "module-help.csv"
                watched = {path: snapshot(path) for path in set(paths.values()) if path.is_file()}
                code, _, _ = run_cli(script, help_arguments(paths))
                assert code == 1, (script, variant)
                for path, expected in watched.items():
                    assert_snapshot(path, expected)
                assert_no_artifacts(root)


def test_samefile_errors_fail_closed_before_mutation_for_every_copy():
    """An OS error during hard-link detection is a validation failure, not distinctness."""
    for script, prepare, arguments, watched_names, legacy_name in (
        *[
            (item, prepare_config, config_arguments, ("config", "user"), "config.yaml")
            for item in CONFIG_SCRIPTS
        ],
        *[
            (item, prepare_help, help_arguments, ("target",), "module-help.csv")
            for item in HELP_SCRIPTS
        ],
    ):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            paths = prepare(root)
            before = {name: snapshot(paths[name]) for name in watched_names}
            cleanup_before = [snapshot(path) for path in legacy_files(paths, legacy_name)]
            module = load_module(script)
            real_samefile = module.os.path.samefile

            def fail_samefile(*_args, **_kwargs):
                raise OSError("injected samefile failure")

            module.os.path.samefile = fail_samefile
            try:
                code, _, _ = invoke_main(module, arguments(paths))
            finally:
                module.os.path.samefile = real_samefile
            assert code == 1, script
            for name, expected in before.items():
                assert_snapshot(paths[name], expected)
            for path, expected in zip(legacy_files(paths, legacy_name), cleanup_before):
                assert_snapshot(path, expected)
            assert_no_artifacts(root)


def test_output_symlinks_preserve_directory_entries_on_success_and_failure():
    """Existing and dangling output links publish through their referents without replacement."""
    for script in CONFIG_SCRIPTS:
        for referent_existed in (True, False):
            with tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary)
                paths = prepare_config(root, legacy=False)
                referent = root / "referents" / "config.yaml"
                referent.parent.mkdir(parents=True)
                if referent_existed:
                    os.replace(paths["config"], referent)
                else:
                    paths["config"].unlink()
                link_value = os.path.relpath(referent, paths["config"].parent)
                paths["config"].symlink_to(link_value)
                code, stdout, stderr = run_cli(script, config_arguments(paths))
                assert code == 0, (script, referent_existed, stdout, stderr)
                assert paths["config"].is_symlink()
                assert os.readlink(paths["config"]) == link_value
                assert yaml.safe_load(referent.read_text(encoding="utf-8"))["demo"][
                    "choice"
                ] == "answer-value"
                assert_no_artifacts(root)

        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            paths = prepare_config(root, legacy=False)
            referent = root / "referents" / "config.yaml"
            referent.parent.mkdir(parents=True)
            os.replace(paths["config"], referent)
            link_value = os.path.relpath(referent, paths["config"].parent)
            paths["config"].symlink_to(link_value)
            referent_before = snapshot(referent)
            user_before = snapshot(paths["user"])
            module = load_module(script)
            real_replace = module.os.replace
            injected = False

            def fail_user_publication(source, destination):
                nonlocal injected
                if (
                    not injected
                    and ".stage-" in Path(source).name
                    and Path(destination) == paths["user"]
                ):
                    injected = True
                    raise OSError("injected publication failure")
                return real_replace(source, destination)

            module.os.replace = fail_user_publication
            try:
                code, _, _ = invoke_main(module, config_arguments(paths))
            finally:
                module.os.replace = real_replace
            assert code == 2, script
            assert paths["config"].is_symlink()
            assert os.readlink(paths["config"]) == link_value
            assert_snapshot(referent, referent_before)
            assert_snapshot(paths["user"], user_before)
            assert_no_artifacts(root)

    for script in HELP_SCRIPTS:
        for referent_existed in (True, False):
            with tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary)
                paths = prepare_help(root, legacy=False)
                referent = root / "referents" / "module-help.csv"
                referent.parent.mkdir(parents=True)
                if referent_existed:
                    os.replace(paths["target"], referent)
                else:
                    paths["target"].unlink()
                link_value = os.path.relpath(referent, paths["target"].parent)
                paths["target"].symlink_to(link_value)
                code, stdout, stderr = run_cli(script, help_arguments(paths))
                assert code == 0, (script, referent_existed, stdout, stderr)
                assert paths["target"].is_symlink()
                assert os.readlink(paths["target"]) == link_value
                assert "demo-setup" in referent.read_text(encoding="utf-8")
                assert_no_artifacts(root)

        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            paths = prepare_help(root, legacy=False)
            referent = root / "referents" / "module-help.csv"
            referent.parent.mkdir(parents=True)
            os.replace(paths["target"], referent)
            link_value = os.path.relpath(referent, paths["target"].parent)
            paths["target"].symlink_to(link_value)
            referent_before = snapshot(referent)
            module = load_module(script)
            real_replace = module.os.replace
            injected = False

            def fail_help_publication(source, destination):
                nonlocal injected
                if (
                    not injected
                    and ".stage-" in Path(source).name
                    and Path(destination) == referent
                ):
                    injected = True
                    raise OSError("injected publication failure")
                return real_replace(source, destination)

            module.os.replace = fail_help_publication
            try:
                code, _, _ = invoke_main(module, help_arguments(paths))
            finally:
                module.os.replace = real_replace
            assert code == 2, script
            assert paths["target"].is_symlink()
            assert os.readlink(paths["target"]) == link_value
            assert_snapshot(referent, referent_before)
            assert_no_artifacts(root)


def test_symlinked_legacy_components_cannot_read_or_delete_outside_sentinels():
    """Redirected module/core cleanup directories fail before reads or publication."""
    for script, prepare, arguments, filename, watched_names in (
        *[
            (item, prepare_config, config_arguments, "config.yaml", ("config", "user"))
            for item in CONFIG_SCRIPTS
        ],
        *[
            (item, prepare_help, help_arguments, "module-help.csv", ("target",))
            for item in HELP_SCRIPTS
        ],
    ):
        for subdir in ("demo", "core"):
            with tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary)
                paths = prepare(root)
                redirected = paths["legacy"] / subdir
                (redirected / filename).unlink()
                redirected.rmdir()
                outside = root / "outside" / subdir
                outside.mkdir(parents=True)
                sentinel = outside / filename
                sentinel.write_text("outside sentinel must survive\n", encoding="utf-8")
                sentinel_before = snapshot(sentinel)
                link_value = os.path.relpath(outside, redirected.parent)
                redirected.symlink_to(link_value, target_is_directory=True)
                before = {name: snapshot(paths[name]) for name in watched_names}

                code, _, _ = run_cli(script, arguments(paths))

                assert code == 1, (script, subdir)
                assert redirected.is_symlink()
                assert os.readlink(redirected) == link_value
                assert_snapshot(sentinel, sentinel_before)
                for name, expected in before.items():
                    assert_snapshot(paths[name], expected)
                assert_no_artifacts(root)


def test_cleanup_help_source_requires_one_module_and_accepts_installed_bmb_asset():
    """Cleanup rejects mixed source modules and accepts the bmad-bmb-setup marker path."""
    for script in HELP_SCRIPTS:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            paths = prepare_help(root)
            paths["source"].write_text(
                paths["source"].read_text(encoding="utf-8")
                + "Other Module,other,Other,O,Other,run,,anytime,,,false,out,other\n",
                encoding="utf-8",
            )
            target_before = snapshot(paths["target"])
            cleanup_before = [snapshot(path) for path in legacy_files(paths, "module-help.csv")]
            code, _, _ = run_cli(script, help_arguments(paths))
            assert code == 1, script
            assert_snapshot(paths["target"], target_before)
            for path, expected in zip(legacy_files(paths, "module-help.csv"), cleanup_before):
                assert_snapshot(path, expected)
            assert_no_artifacts(root)

        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            paths = prepare_help(root, legacy=False)
            paths["source"] = BMB_HELP_SOURCE
            paths["legacy"] = root / "legacy"
            cleanup_files = []
            for subdir in ("bmb", "core"):
                cleanup_file = paths["legacy"] / subdir / "module-help.csv"
                cleanup_file.parent.mkdir(parents=True)
                cleanup_file.write_text("legacy\n", encoding="utf-8")
                cleanup_files.append(cleanup_file)
            code, stdout, stderr = run_cli(
                script, help_arguments(paths, module_code="bmb")
            )
            assert code == 0, (script, stdout, stderr)
            assert "bmad-bmb-setup" in paths["target"].read_text(encoding="utf-8")
            assert all(not path.exists() for path in cleanup_files)
            assert_no_artifacts(root)


def test_injected_config_staging_publication_and_interruptions_roll_back_every_copy():
    """Every config copy unwinds staging, replacement failure, and interruption cleanly."""
    for script in CONFIG_SCRIPTS:
        for failure_mode in (
            "stage",
            "backup",
            "replace-first",
            "replace-second",
            "interrupt",
        ):
            with tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary)
                paths = prepare_config(root)
                pair_before = [snapshot(paths[name]) for name in ("config", "user")]
                legacy_before = [snapshot(path) for path in legacy_files(paths, "config.yaml")]
                module = load_module(script)

                if failure_mode == "stage":
                    real_mkstemp = module.tempfile.mkstemp
                    calls = 0

                    def fail_second_stage(*args, **kwargs):
                        nonlocal calls
                        calls += 1
                        if calls == 2:
                            raise OSError("injected staging failure")
                        return real_mkstemp(*args, **kwargs)

                    module.tempfile.mkstemp = fail_second_stage
                    restore = lambda: setattr(module.tempfile, "mkstemp", real_mkstemp)
                elif failure_mode == "backup":
                    real_copy2 = module.shutil.copy2

                    def fail_backup(*args, **kwargs):
                        raise OSError("injected rollback preparation failure")

                    module.shutil.copy2 = fail_backup
                    restore = lambda: setattr(module.shutil, "copy2", real_copy2)
                else:
                    real_replace = module.os.replace
                    injected = False
                    failure_target = (
                        paths["config"] if failure_mode == "replace-first" else paths["user"]
                    )

                    def fail_user_replace(source, destination):
                        nonlocal injected
                        if not injected and Path(destination) == failure_target:
                            injected = True
                            if failure_mode == "interrupt":
                                raise KeyboardInterrupt()
                            raise OSError("injected publication failure")
                        return real_replace(source, destination)

                    module.os.replace = fail_user_replace
                    restore = lambda: setattr(module.os, "replace", real_replace)

                try:
                    code, _, _ = invoke_main(module, config_arguments(paths))
                finally:
                    restore()
                assert code == 2, (script, failure_mode)
                for name, expected in zip(("config", "user"), pair_before):
                    assert_snapshot(paths[name], expected)
                for path, expected in zip(legacy_files(paths, "config.yaml"), legacy_before):
                    assert_snapshot(path, expected)
                assert_no_artifacts(root)


def test_injected_csv_staging_publication_and_interruptions_roll_back_every_copy():
    """Every help copy preserves its target and cleanup inputs on injected failures."""
    for script in HELP_SCRIPTS:
        for failure_mode in ("stage", "backup", "replace", "interrupt"):
            with tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary)
                paths = prepare_help(root)
                target_before = snapshot(paths["target"])
                legacy_before = [snapshot(path) for path in legacy_files(paths, "module-help.csv")]
                module = load_module(script)

                if failure_mode == "stage":
                    real_mkstemp = module.tempfile.mkstemp

                    def fail_stage(*args, **kwargs):
                        raise OSError("injected staging failure")

                    module.tempfile.mkstemp = fail_stage
                    restore = lambda: setattr(module.tempfile, "mkstemp", real_mkstemp)
                elif failure_mode == "backup":
                    real_copy2 = module.shutil.copy2

                    def fail_backup(*args, **kwargs):
                        raise OSError("injected rollback preparation failure")

                    module.shutil.copy2 = fail_backup
                    restore = lambda: setattr(module.shutil, "copy2", real_copy2)
                else:
                    real_replace = module.os.replace
                    injected = False

                    def fail_target_replace(source, destination):
                        nonlocal injected
                        if not injected and Path(destination) == paths["target"]:
                            injected = True
                            if failure_mode == "interrupt":
                                raise KeyboardInterrupt()
                            raise OSError("injected publication failure")
                        return real_replace(source, destination)

                    module.os.replace = fail_target_replace
                    restore = lambda: setattr(module.os, "replace", real_replace)

                try:
                    code, _, _ = invoke_main(module, help_arguments(paths))
                finally:
                    restore()
                assert code == 2, (script, failure_mode)
                assert_snapshot(paths["target"], target_before)
                for path, expected in zip(legacy_files(paths, "module-help.csv"), legacy_before):
                    assert_snapshot(path, expected)
                assert_no_artifacts(root)


def test_injected_publication_failures_restore_absent_targets_for_every_copy():
    """Rollback removes outputs that did not exist before a failed publication."""
    for script in CONFIG_SCRIPTS:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            paths = prepare_config(root, existing=False)
            module = load_module(script)
            real_replace = module.os.replace
            injected = False

            def fail_user_replace(source, destination):
                nonlocal injected
                if not injected and Path(destination) == paths["user"]:
                    injected = True
                    raise OSError("injected publication failure")
                return real_replace(source, destination)

            module.os.replace = fail_user_replace
            try:
                code, _, _ = invoke_main(module, config_arguments(paths))
            finally:
                module.os.replace = real_replace
            assert code == 2, script
            assert not paths["config"].exists()
            assert not paths["user"].exists()
            assert all(path.exists() for path in legacy_files(paths, "config.yaml"))
            assert_no_artifacts(root)

    for script in HELP_SCRIPTS:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            paths = prepare_help(root, existing=False)
            module = load_module(script)
            real_replace = module.os.replace
            injected = False

            def fail_target_replace(source, destination):
                nonlocal injected
                if not injected and Path(destination) == paths["target"]:
                    injected = True
                    raise OSError("injected publication failure")
                return real_replace(source, destination)

            module.os.replace = fail_target_replace
            try:
                code, _, _ = invoke_main(module, help_arguments(paths))
            finally:
                module.os.replace = real_replace
            assert code == 2, script
            assert not paths["target"].exists()
            assert all(path.exists() for path in legacy_files(paths, "module-help.csv"))
            assert_no_artifacts(root)


def test_one_shot_rollback_failures_are_retried_and_verified_for_every_copy():
    """Transient rollback replacement/interruption and absent-target removal recover fully."""
    for script in CONFIG_SCRIPTS:
        for rollback_failure in (OSError, KeyboardInterrupt):
            with tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary)
                paths = prepare_config(root)
                pair_before = [snapshot(paths[name]) for name in ("config", "user")]
                legacy_before = [snapshot(path) for path in legacy_files(paths, "config.yaml")]
                module = load_module(script)
                real_replace = module.os.replace
                forward_injected = False
                rollback_injected = False

                def fail_forward_and_first_rollback(source, destination):
                    nonlocal forward_injected, rollback_injected
                    source = Path(source)
                    destination = Path(destination)
                    if (
                        not forward_injected
                        and ".stage-" in source.name
                        and destination == paths["user"]
                    ):
                        forward_injected = True
                        raise OSError("injected forward publication failure")
                    if not rollback_injected and ".rollback-" in source.name:
                        rollback_injected = True
                        raise rollback_failure("injected transient rollback failure")
                    return real_replace(source, destination)

                module.os.replace = fail_forward_and_first_rollback
                try:
                    code, _, _ = invoke_main(module, config_arguments(paths))
                finally:
                    module.os.replace = real_replace
                assert code == 2, (script, rollback_failure)
                assert forward_injected and rollback_injected, (script, rollback_failure)
                for name, expected in zip(("config", "user"), pair_before):
                    assert_snapshot(paths[name], expected)
                for path, expected in zip(legacy_files(paths, "config.yaml"), legacy_before):
                    assert_snapshot(path, expected)
                assert_no_artifacts(root)

        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            paths = prepare_config(root, existing=False)
            module = load_module(script)
            real_replace = module.os.replace
            real_unlink = module.Path.unlink
            forward_injected = False
            rollback_injected = False

            def replace_then_fail(source, destination):
                nonlocal forward_injected
                result = real_replace(source, destination)
                if (
                    not forward_injected
                    and ".stage-" in Path(source).name
                    and Path(destination) == paths["user"]
                ):
                    forward_injected = True
                    raise OSError("injected post-publication failure")
                return result

            def fail_first_absent_restore(path, *args, **kwargs):
                nonlocal rollback_injected
                if not rollback_injected and path in (paths["config"], paths["user"]):
                    rollback_injected = True
                    raise OSError("injected transient absent-target rollback failure")
                return real_unlink(path, *args, **kwargs)

            module.os.replace = replace_then_fail
            module.Path.unlink = fail_first_absent_restore
            try:
                code, _, _ = invoke_main(module, config_arguments(paths))
            finally:
                module.os.replace = real_replace
                module.Path.unlink = real_unlink
            assert code == 2, script
            assert forward_injected and rollback_injected, script
            assert not paths["config"].exists()
            assert not paths["user"].exists()
            assert all(path.exists() for path in legacy_files(paths, "config.yaml"))
            assert_no_artifacts(root)

    for script in HELP_SCRIPTS:
        for rollback_failure in (OSError, KeyboardInterrupt):
            with tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary)
                paths = prepare_help(root)
                target_before = snapshot(paths["target"])
                legacy_before = [snapshot(path) for path in legacy_files(paths, "module-help.csv")]
                module = load_module(script)
                real_replace = module.os.replace
                forward_injected = False
                rollback_injected = False

                def fail_forward_and_first_rollback(source, destination):
                    nonlocal forward_injected, rollback_injected
                    source = Path(source)
                    if not forward_injected and ".stage-" in source.name:
                        forward_injected = True
                        raise OSError("injected forward publication failure")
                    if not rollback_injected and ".rollback-" in source.name:
                        rollback_injected = True
                        raise rollback_failure("injected transient rollback failure")
                    return real_replace(source, destination)

                module.os.replace = fail_forward_and_first_rollback
                try:
                    code, _, _ = invoke_main(module, help_arguments(paths))
                finally:
                    module.os.replace = real_replace
                assert code == 2, (script, rollback_failure)
                assert forward_injected and rollback_injected, (script, rollback_failure)
                assert_snapshot(paths["target"], target_before)
                for path, expected in zip(legacy_files(paths, "module-help.csv"), legacy_before):
                    assert_snapshot(path, expected)
                assert_no_artifacts(root)

        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            paths = prepare_help(root, existing=False)
            module = load_module(script)
            real_replace = module.os.replace
            real_unlink = module.Path.unlink
            forward_injected = False
            rollback_injected = False

            def replace_then_fail(source, destination):
                nonlocal forward_injected
                result = real_replace(source, destination)
                if not forward_injected and ".stage-" in Path(source).name:
                    forward_injected = True
                    raise OSError("injected post-publication failure")
                return result

            def fail_first_absent_restore(path, *args, **kwargs):
                nonlocal rollback_injected
                if not rollback_injected and path == paths["target"]:
                    rollback_injected = True
                    raise OSError("injected transient absent-target rollback failure")
                return real_unlink(path, *args, **kwargs)

            module.os.replace = replace_then_fail
            module.Path.unlink = fail_first_absent_restore
            try:
                code, _, _ = invoke_main(module, help_arguments(paths))
            finally:
                module.os.replace = real_replace
                module.Path.unlink = real_unlink
            assert code == 2, script
            assert forward_injected and rollback_injected, script
            assert not paths["target"].exists()
            assert all(path.exists() for path in legacy_files(paths, "module-help.csv"))
            assert_no_artifacts(root)


def test_transient_artifact_cleanup_failures_are_retried_for_every_copy():
    """A transient unlink failure cannot strand a staged publication artifact."""
    for script, prepare, arguments, target_name in (
        *[
            (item, prepare_config, config_arguments, "user")
            for item in CONFIG_SCRIPTS
        ],
        *[
            (item, prepare_help, help_arguments, "target")
            for item in HELP_SCRIPTS
        ],
    ):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            paths = prepare(root)
            module = load_module(script)
            real_replace = module.os.replace
            real_unlink = module.Path.unlink
            replacement_injected = False
            cleanup_injected = False

            def fail_replacement(source, destination):
                nonlocal replacement_injected
                if not replacement_injected and Path(destination) == paths[target_name]:
                    replacement_injected = True
                    raise OSError("injected publication failure")
                return real_replace(source, destination)

            def fail_first_stage_cleanup(path, *args, **kwargs):
                nonlocal cleanup_injected
                if not cleanup_injected and ".stage-" in path.name:
                    cleanup_injected = True
                    raise OSError("injected transient cleanup failure")
                return real_unlink(path, *args, **kwargs)

            module.os.replace = fail_replacement
            module.Path.unlink = fail_first_stage_cleanup
            try:
                code, _, _ = invoke_main(module, arguments(paths))
            finally:
                module.os.replace = real_replace
                module.Path.unlink = real_unlink
            assert code == 2, script
            assert cleanup_injected, script
            assert_no_artifacts(root)


def test_all_entry_points_and_manifest_rows_are_exact_and_unique():
    """All scripts/tests stay byte-identical and the manifest hashes every logical asset once."""
    synchronized_groups = [CONFIG_SCRIPTS, HELP_SCRIPTS, TEST_COPIES]
    for group in synchronized_groups:
        missing = [str(path) for path in group if not path.is_file()]
        assert not missing, f"tracked atomicity copy missing: {missing}"
        expected = group[0].read_bytes()
        assert all(path.read_bytes() == expected for path in group), (
            f"tracked atomicity copies have drifted: {[str(path) for path in group]}"
        )

    manifest_path = PROJECT_ROOT / "_bmad" / "_config" / "files-manifest.csv"
    with open(manifest_path, "r", encoding="utf-8", newline="") as stream:
        rows = list(csv.DictReader(stream))
    expected_paths = {
        "bmb/bmad-bmb-setup/scripts/merge-config.py",
        "bmb/bmad-module-builder/assets/setup-skill-template/scripts/merge-config.py",
        "bmb/bmad-module-builder/assets/standalone-module-template/merge-config.py",
        "bmb/bmad-bmb-setup/scripts/merge-help-csv.py",
        "bmb/bmad-module-builder/assets/setup-skill-template/scripts/merge-help-csv.py",
        "bmb/bmad-module-builder/assets/standalone-module-template/merge-help-csv.py",
        "bmb/bmad-module-builder/scripts/tests/test-merge-atomicity.py",
    }
    selected = [row for row in rows if row["path"] in expected_paths]
    assert len(selected) == len(expected_paths)
    assert {row["path"] for row in selected} == expected_paths
    for row in selected:
        tracked = PROJECT_ROOT / ".agents" / "skills" / row["path"].removeprefix("bmb/")
        digest = hashlib.sha256(tracked.read_bytes()).hexdigest()
        assert row["hash"] == digest, f"manifest hash drift for {row['path']}"


if __name__ == "__main__":
    tests = [
        test_valid_existing_merges_preserve_semantics_modes_and_cleanup,
        test_new_targets_use_normal_create_mode_for_every_copy,
        test_empty_existing_yaml_documents_remain_valid_for_every_copy,
        test_invalid_cleanup_arguments_and_documents_preserve_all_state,
        test_non_mapping_module_answers_and_legacy_inputs_are_rejected,
        test_unsafe_and_mismatched_module_codes_fail_before_mutation,
        test_aliases_case_collisions_and_nested_outputs_are_rejected,
        test_samefile_errors_fail_closed_before_mutation_for_every_copy,
        test_output_symlinks_preserve_directory_entries_on_success_and_failure,
        test_symlinked_legacy_components_cannot_read_or_delete_outside_sentinels,
        test_cleanup_help_source_requires_one_module_and_accepts_installed_bmb_asset,
        test_injected_config_staging_publication_and_interruptions_roll_back_every_copy,
        test_injected_csv_staging_publication_and_interruptions_roll_back_every_copy,
        test_injected_publication_failures_restore_absent_targets_for_every_copy,
        test_one_shot_rollback_failures_are_retried_and_verified_for_every_copy,
        test_transient_artifact_cleanup_failures_are_retried_for_every_copy,
        test_all_entry_points_and_manifest_rows_are_exact_and_unique,
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
