#!/usr/bin/env python3
# /// script
# requires-python = ">=3.9"
# dependencies = ["pyyaml"]
# ///
"""Merge module configuration into shared config files failure-atomically.

Exit codes: 0=success, 1=validation error, 2=runtime error
"""

import argparse
import json
import os
import re
import shutil
import stat
import sys
import tempfile
from io import StringIO
from pathlib import Path

try:
    import yaml
except ImportError:
    print("Error: pyyaml is required (PEP 723 dependency)", file=sys.stderr)
    sys.exit(2)


_CORE_KEYS = frozenset(
    {"user_name", "communication_language", "document_output_language", "output_folder"}
)
_CORE_USER_KEYS = ("user_name", "communication_language")
_SAFE_MODULE_CODE = re.compile(r"^[A-Za-z0-9][A-Za-z0-9._-]*$")
_WINDOWS_RESERVED_BASENAMES = {
    "con",
    "prn",
    "aux",
    "nul",
    *(f"com{number}" for number in range(1, 10)),
    *(f"lpt{number}" for number in range(1, 10)),
}
_RESERVED_MODULE_CODES = {"core", *(key.casefold() for key in _CORE_KEYS)}


class ValidationError(ValueError):
    """An invalid invocation or configuration document."""


def parse_args():
    parser = argparse.ArgumentParser(
        description="Merge module config into shared _bmad/config.yaml with anti-zombie pattern."
    )
    parser.add_argument("--config-path", required=True, help="Path to the target _bmad/config.yaml file")
    parser.add_argument("--module-yaml", required=True, help="Path to the module.yaml definition file")
    parser.add_argument("--answers", required=True, help="Path to JSON file with collected answers")
    parser.add_argument("--user-config-path", required=True, help="Path to the target _bmad/config.user.yaml file")
    parser.add_argument(
        "--legacy-dir",
        help="Path to _bmad/ directory to check for legacy per-module config files. "
        "Matching values are used as fallback defaults, then legacy files are deleted.",
    )
    parser.add_argument("--verbose", action="store_true", help="Print detailed progress to stderr")
    return parser.parse_args()


def load_yaml_file(path: str, name: str, required: bool = False) -> dict:
    """Load one YAML mapping, returning an empty mapping for an absent optional file."""
    file_path = Path(path)
    if not file_path.exists():
        if required:
            raise ValidationError(f"Could not load {name} from {path}")
        return {}
    try:
        with open(file_path, "r", encoding="utf-8") as stream:
            content = yaml.safe_load(stream)
    except yaml.YAMLError as error:
        raise ValidationError(f"Could not parse {name} from {path}: {error}") from error
    if content is None:
        return {}
    if not isinstance(content, dict):
        raise ValidationError(f"{name} must contain a YAML mapping")
    return content


def load_json_file(path: str) -> dict:
    """Load the answers JSON mapping."""
    file_path = Path(path)
    if not file_path.exists():
        raise ValidationError(f"Could not load answers from {path}")
    try:
        with open(file_path, "r", encoding="utf-8") as stream:
            content = json.load(stream)
    except json.JSONDecodeError as error:
        raise ValidationError(f"Could not parse answers from {path}: {error}") from error
    if not isinstance(content, dict):
        raise ValidationError("answers must contain a JSON object")
    for section in ("core", "module"):
        if section in content and not isinstance(content[section], dict):
            raise ValidationError(f"answers.{section} must contain a JSON object")
    return content


def load_legacy_values(
    legacy_candidates: list[Path], module_yaml: dict, verbose: bool = False
) -> tuple[dict, dict, list]:
    """Read mapping-shaped legacy core and module configuration values."""
    legacy_core = {}
    legacy_module = {}
    files_found = []

    module_path, core_path = legacy_candidates
    if core_path.exists():
        core_data = load_yaml_file(str(core_path), "legacy core config")
        files_found.append(str(core_path))
        for key, value in core_data.items():
            if key in _CORE_KEYS:
                legacy_core[key] = value
        if verbose:
            print(f"Legacy core config: {list(legacy_core.keys())}", file=sys.stderr)

    if module_path.exists():
        module_data = load_yaml_file(str(module_path), "legacy module config")
        files_found.append(str(module_path))
        for key, value in module_data.items():
            if key in _CORE_KEYS:
                if key not in legacy_core:
                    legacy_core[key] = value
            elif key in module_yaml and isinstance(module_yaml[key], dict):
                legacy_module[key] = value
        if verbose:
            print(f"Legacy module config: {list(legacy_module.keys())}", file=sys.stderr)

    return legacy_core, legacy_module, files_found


def apply_legacy_defaults(answers: dict, legacy_core: dict, legacy_module: dict) -> dict:
    """Apply legacy values as fallbacks while retaining explicit answer precedence."""
    merged = dict(answers)
    if legacy_core:
        filled_core = dict(legacy_core)
        filled_core.update(merged.get("core", {}))
        merged["core"] = filled_core
    if legacy_module:
        filled_module = dict(legacy_module)
        filled_module.update(merged.get("module", {}))
        merged["module"] = filled_module
    return merged


def cleanup_legacy_configs(legacy_candidates: list[Path], verbose: bool = False) -> list:
    """Delete legacy config files after both shared targets commit."""
    deleted = []
    for legacy_path in legacy_candidates:
        if legacy_path.exists():
            if verbose:
                print(f"Deleting legacy config: {legacy_path}", file=sys.stderr)
            legacy_path.unlink()
            deleted.append(str(legacy_path))
    return deleted


def extract_module_metadata(module_yaml: dict) -> dict:
    """Extract non-variable metadata fields from module.yaml."""
    metadata = {}
    for key in ("name", "description"):
        if key in module_yaml:
            metadata[key] = module_yaml[key]
    metadata["version"] = module_yaml.get("module_version")
    if "default_selected" in module_yaml:
        metadata["default_selected"] = module_yaml["default_selected"]
    return metadata


def apply_result_templates(module_yaml: dict, module_answers: dict, verbose: bool = False) -> dict:
    """Apply result templates from module.yaml to raw answer values."""
    transformed = {}
    for key, value in module_answers.items():
        variable_definition = module_yaml.get(key)
        if (
            isinstance(variable_definition, dict)
            and "result" in variable_definition
            and "{project-root}" not in str(value)
        ):
            template = variable_definition["result"]
            if not isinstance(template, str):
                raise ValidationError(f"module.yaml result template for {key!r} must be a string")
            transformed[key] = template.replace("{value}", str(value))
            if verbose:
                print(
                    f"Applied result template for '{key}': {value} → {transformed[key]}",
                    file=sys.stderr,
                )
        else:
            transformed[key] = value
    return transformed


def merge_config(existing_config: dict, module_yaml: dict, answers: dict, verbose: bool = False) -> dict:
    """Merge answers into the shared config using anti-zombie semantics."""
    config = dict(existing_config)
    module_code = module_yaml["code"]

    if "core" in config and isinstance(config["core"], dict):
        if verbose:
            print("Migrating legacy 'core' section to root", file=sys.stderr)
        config.update(config.pop("core"))

    for key in _CORE_USER_KEYS:
        if key in config:
            if verbose:
                print(
                    f"Removing user-only key '{key}' from config (belongs in config.user.yaml)",
                    file=sys.stderr,
                )
            del config[key]

    core_answers = answers.get("core")
    if core_answers:
        shared_core = {key: value for key, value in core_answers.items() if key not in _CORE_USER_KEYS}
        if shared_core:
            if verbose:
                print(f"Writing core config at root: {list(shared_core.keys())}", file=sys.stderr)
            config.update(shared_core)

    if module_code in config:
        if verbose:
            print(f"Removing existing '{module_code}' section (anti-zombie)", file=sys.stderr)
        del config[module_code]

    module_section = extract_module_metadata(module_yaml)
    module_section.update(
        apply_result_templates(module_yaml, answers.get("module", {}), verbose)
    )
    if verbose:
        print(
            f"Writing '{module_code}' section with keys: {list(module_section.keys())}",
            file=sys.stderr,
        )
    config[module_code] = module_section
    return config


def extract_user_settings(module_yaml: dict, answers: dict) -> dict:
    """Collect core and module settings that belong in config.user.yaml."""
    user_settings = {}
    core_answers = answers.get("core", {})
    for key in _CORE_USER_KEYS:
        if key in core_answers:
            user_settings[key] = core_answers[key]
    module_answers = answers.get("module", {})
    for variable_name, variable_definition in module_yaml.items():
        if isinstance(variable_definition, dict) and variable_definition.get("user_setting") is True:
            if variable_name in module_answers:
                user_settings[variable_name] = module_answers[variable_name]
    return user_settings


def serialize_yaml(config: dict, name: str) -> str:
    """Serialize and re-validate a complete prospective YAML mapping."""
    stream = StringIO()
    try:
        yaml.dump(
            config,
            stream,
            default_flow_style=False,
            allow_unicode=True,
            sort_keys=False,
        )
        content = stream.getvalue()
        reparsed = yaml.safe_load(content)
    except (TypeError, yaml.YAMLError) as error:
        raise ValidationError(f"Could not serialize prospective {name}: {error}") from error
    if not isinstance(reparsed, dict):
        raise ValidationError(f"Prospective {name} must contain a YAML mapping")
    return content


def reject_unresolved_paths(named_paths: list[tuple[str, str]]) -> None:
    """Reject filesystem path arguments containing an unresolved project token."""
    for name, value in named_paths:
        if value and "{project-root}" in value:
            raise ValidationError(
                f"Unresolved '{{project-root}}' token in {name} path: {value!r}. "
                "Resolve '{project-root}' to the actual project root before running "
                "this script — it is a filesystem path, not a config value."
            )


def validate_module_code(module_code) -> None:
    """Require a non-reserved, portable, single-component module code."""
    if (
        not isinstance(module_code, str)
        or not _SAFE_MODULE_CODE.fullmatch(module_code)
        or module_code.endswith(".")
        or module_code.casefold() in _RESERVED_MODULE_CODES
        or module_code.split(".", 1)[0].casefold() in _WINDOWS_RESERVED_BASENAMES
    ):
        raise ValidationError("module.yaml code must be a safe, non-reserved single path component")


def resolved_path(path: Path) -> Path:
    """Resolve a path without requiring the final filesystem object to exist."""
    try:
        return path.resolve(strict=False)
    except (OSError, RuntimeError) as error:
        raise ValidationError(f"Could not resolve filesystem path {path}") from error


def resolved_casefold(path: Path) -> str:
    """Return a normalized spelling used to reject case-only path aliases."""
    return str(resolved_path(path)).casefold()


def paths_alias(first: Path, second: Path) -> bool:
    """Detect lexical, symlink, hard-link, and case-insensitive aliases."""
    if resolved_casefold(first) == resolved_casefold(second):
        return True
    try:
        return first.exists() and second.exists() and os.path.samefile(first, second)
    except OSError as error:
        raise ValidationError(
            f"Could not establish path distinctness for {first} and {second}"
        ) from error


def reject_alias(first_name: str, first: Path, second_name: str, second: Path) -> None:
    """Reject two arguments that can address the same filesystem object."""
    if paths_alias(first, second):
        raise ValidationError(f"{first_name} must not alias {second_name}")


def is_nested_path(first: Path, second: Path) -> bool:
    """Detect an ancestor/descendant relationship, including case-only spellings."""
    first_parts = tuple(part.casefold() for part in resolved_path(first).parts)
    second_parts = tuple(part.casefold() for part in resolved_path(second).parts)
    shortest = min(len(first_parts), len(second_parts))
    return first_parts[:shortest] == second_parts[:shortest] and len(first_parts) != len(second_parts)


def effective_output_path(path: Path) -> Path:
    """Follow an output symlink so atomic replacement preserves its directory entry."""
    try:
        return resolved_path(path) if path.is_symlink() else path
    except OSError as error:
        raise ValidationError(f"Could not inspect output path {path}") from error


def validate_legacy_candidates(legacy_dir: str, module_code: str) -> list[Path]:
    """Resolve and confine legacy files to their expected non-symlinked children."""
    legacy_root = resolved_path(Path(legacy_dir))
    candidates = []
    for subdir in (module_code, "core"):
        expected = legacy_root / subdir / "config.yaml"
        if resolved_path(expected) != expected:
            raise ValidationError(
                f"Legacy cleanup path must be the direct child {expected}; symlink redirection is not allowed"
            )
        candidates.append(expected)
    return candidates


def validate_path_relationships(args) -> tuple[Path, Path, Path, Path, Path, Path]:
    """Validate output/output and output/input relationships before any input read."""
    requested_config_path = Path(args.config_path)
    requested_user_config_path = Path(args.user_config_path)
    config_path = effective_output_path(requested_config_path)
    user_config_path = effective_output_path(requested_user_config_path)
    module_yaml_path = Path(args.module_yaml)
    answers_path = Path(args.answers)

    reject_alias("--config-path", config_path, "--user-config-path", user_config_path)
    if is_nested_path(config_path, user_config_path):
        raise ValidationError("configuration output paths must not be nested")
    for output_name, output_path in (
        ("--config-path", config_path),
        ("--user-config-path", user_config_path),
    ):
        reject_alias(output_name, output_path, "--module-yaml", module_yaml_path)
        reject_alias(output_name, output_path, "--answers", answers_path)
    return (
        requested_config_path,
        requested_user_config_path,
        config_path,
        user_config_path,
        module_yaml_path,
        answers_path,
    )


def default_create_mode() -> int:
    """Return the mode a normal open(..., 'w') file creation would receive."""
    process_umask = os.umask(0)
    os.umask(process_umask)
    return 0o666 & ~process_umask


def remove_artifacts(paths: list[Path]) -> list:
    """Exhaust repeated removal attempts for every disposable artifact."""
    pending = [path for path in paths if path is not None]
    errors = []
    for _ in range(3):
        if not pending:
            break
        failed = []
        errors = []
        for path in pending:
            try:
                path.unlink(missing_ok=True)
            except BaseException as error:
                failed.append(path)
                errors.append(error)
        pending = failed
    return errors


def capture_file_snapshot(target: Path) -> dict:
    """Capture the exact state needed to verify publication rollback."""
    if not target.exists():
        return {"existed": False, "content": None, "mode": None}
    return {
        "existed": True,
        "content": target.read_bytes(),
        "mode": stat.S_IMODE(target.stat().st_mode),
    }


def snapshot_matches(target: Path, snapshot: dict) -> bool:
    """Check target existence, bytes, and mode against a captured snapshot."""
    if not snapshot["existed"]:
        return not target.exists()
    return (
        target.exists()
        and target.read_bytes() == snapshot["content"]
        and stat.S_IMODE(target.stat().st_mode) == snapshot["mode"]
    )


def stage_text(target: Path, content: str, mode: int) -> Path:
    """Write and fsync content to a same-directory staging file."""
    target.parent.mkdir(parents=True, exist_ok=True)
    descriptor, temporary_name = tempfile.mkstemp(
        prefix=f".{target.name}.stage-", suffix=".tmp", dir=str(target.parent)
    )
    temporary_path = Path(temporary_name)
    try:
        with os.fdopen(descriptor, "w", encoding="utf-8", newline="") as stream:
            stream.write(content)
            stream.flush()
            os.fsync(stream.fileno())
        os.chmod(temporary_path, mode)
        return temporary_path
    except BaseException as error:
        try:
            os.close(descriptor)
        except OSError:
            pass
        if remove_artifacts([temporary_path]):
            raise RuntimeError("Configuration staging artifact cleanup failed") from error
        raise


def create_rollback(target: Path) -> Path:
    """Copy an existing target to a same-directory rollback artifact."""
    descriptor, rollback_name = tempfile.mkstemp(
        prefix=f".{target.name}.rollback-", suffix=".tmp", dir=str(target.parent)
    )
    os.close(descriptor)
    rollback = Path(rollback_name)
    try:
        shutil.copy2(target, rollback)
        return rollback
    except BaseException as error:
        if remove_artifacts([rollback]):
            raise RuntimeError("Configuration rollback artifact cleanup failed") from error
        raise


def restore_target(target: Path, snapshot: dict, rollback: Path) -> tuple[bool, Path]:
    """Retry restoration three times and verify exact original target state."""
    for _ in range(3):
        try:
            if snapshot["existed"]:
                if rollback is not None and rollback.exists():
                    os.replace(rollback, target)
            else:
                target.unlink(missing_ok=True)
        except BaseException:
            pass
        try:
            if snapshot_matches(target, snapshot):
                return True, None
        except BaseException:
            pass
    recovery_path = rollback if rollback is not None and rollback.exists() else target
    return False, recovery_path


def publish_config_pair(documents: list[tuple[Path, str]]) -> None:
    """Stage all changed documents, publish them, and roll back the pair on failure."""
    entries = []
    failure = None
    recovery_artifacts = []
    try:
        for target, content in documents:
            snapshot = capture_file_snapshot(target)
            if snapshot["existed"] and snapshot["content"] == content.encode("utf-8"):
                continue
            entries.append(
                {
                    "target": target,
                    "staged": stage_text(
                        target,
                        content,
                        snapshot["mode"] if snapshot["existed"] else default_create_mode(),
                    ),
                    "snapshot": snapshot,
                    "rollback": None,
                    "attempted": False,
                    "retain_rollback": False,
                }
            )
        for entry in entries:
            if entry["snapshot"]["existed"]:
                entry["rollback"] = create_rollback(entry["target"])
        for entry in entries:
            entry["attempted"] = True
            os.replace(entry["staged"], entry["target"])
            entry["staged"] = None
    except BaseException as error:
        failure = error
        for entry in reversed(entries):
            if not entry["attempted"]:
                continue
            restored, recovery_path = restore_target(
                entry["target"], entry["snapshot"], entry["rollback"]
            )
            if not restored:
                entry["retain_rollback"] = recovery_path == entry["rollback"]
                recovery_artifacts.append(recovery_path)

    cleanup_errors = remove_artifacts(
        [entry["staged"] for entry in entries]
        + [entry["rollback"] for entry in entries if not entry["retain_rollback"]]
    )
    if failure is not None:
        if recovery_artifacts:
            locations = ", ".join(str(path) for path in recovery_artifacts)
            raise RuntimeError(
                f"Configuration publication rollback could not restore original state; "
                f"manual recovery artifact: {locations}"
            ) from failure
        if cleanup_errors:
            raise RuntimeError("Configuration publication artifact cleanup failed") from failure
        raise failure
    if cleanup_errors:
        raise RuntimeError("Configuration publication artifact cleanup failed") from cleanup_errors[0]


def run(args) -> dict:
    """Validate all prospective state, publish the pair, then clean legacy files."""
    reject_unresolved_paths(
        [
            ("--config-path", args.config_path),
            ("--user-config-path", args.user_config_path),
            ("--module-yaml", args.module_yaml),
            ("--answers", args.answers),
            ("--legacy-dir", args.legacy_dir),
        ]
    )
    (
        requested_config_path,
        requested_user_config_path,
        config_path,
        user_config_path,
        module_yaml_path,
        answers_path,
    ) = validate_path_relationships(args)

    module_yaml = load_yaml_file(args.module_yaml, "module.yaml", required=True)
    module_code = module_yaml.get("code")
    validate_module_code(module_code)

    legacy_candidates = []
    if args.legacy_dir:
        legacy_candidates = validate_legacy_candidates(args.legacy_dir, module_code)
        reject_alias("legacy module config", legacy_candidates[0], "legacy core config", legacy_candidates[1])
        for candidate in legacy_candidates:
            for output_name, output_path in (
                ("--config-path", config_path),
                ("--user-config-path", user_config_path),
            ):
                reject_alias(output_name, output_path, "legacy cleanup target", candidate)
            reject_alias("--module-yaml", module_yaml_path, "legacy cleanup target", candidate)
            reject_alias("--answers", answers_path, "legacy cleanup target", candidate)

    answers = load_json_file(args.answers)
    existing_config = load_yaml_file(str(config_path), "config.yaml")
    existing_user_config = load_yaml_file(str(user_config_path), "config.user.yaml")

    if args.verbose:
        print(f"Config file exists: {config_path.exists()}", file=sys.stderr)
        if config_path.exists():
            print(f"Existing sections: {list(existing_config.keys())}", file=sys.stderr)

    legacy_files_found = []
    if args.legacy_dir:
        legacy_core, legacy_module, legacy_files_found = load_legacy_values(
            legacy_candidates, module_yaml, args.verbose
        )
        if legacy_core or legacy_module:
            answers = apply_legacy_defaults(answers, legacy_core, legacy_module)
            if args.verbose:
                print("Applied legacy values as fallback defaults", file=sys.stderr)

    updated_config = merge_config(existing_config, module_yaml, answers, args.verbose)
    user_settings = extract_user_settings(module_yaml, answers)
    updated_user_config = dict(existing_user_config)
    updated_user_config.update(user_settings)

    prospective_config = serialize_yaml(updated_config, "config.yaml")
    prospective_user_config = serialize_yaml(updated_user_config, "config.user.yaml")
    documents = [(config_path, prospective_config)]
    if user_settings:
        documents.append((user_config_path, prospective_user_config))

    if args.verbose:
        print(f"Publishing config to {config_path}", file=sys.stderr)
        if user_settings:
            print(f"Publishing user config to {user_config_path}", file=sys.stderr)
    publish_config_pair(documents)

    legacy_deleted = []
    if args.legacy_dir:
        legacy_deleted = cleanup_legacy_configs(legacy_candidates, args.verbose)

    return {
        "status": "success",
        "config_path": str(requested_config_path.resolve()),
        "user_config_path": str(requested_user_config_path.resolve()),
        "module_code": module_code,
        "core_updated": bool(answers.get("core")),
        "module_keys": list(updated_config.get(module_code, {}).keys()),
        "user_keys": list(user_settings.keys()),
        "legacy_configs_found": legacy_files_found,
        "legacy_configs_deleted": legacy_deleted,
    }


def main():
    try:
        print(json.dumps(run(parse_args()), indent=2))
    except ValidationError as error:
        print(f"Error: {error}", file=sys.stderr)
        sys.exit(1)
    except (KeyboardInterrupt, OSError, RuntimeError) as error:
        print(f"Error: {error}", file=sys.stderr)
        sys.exit(2)


if __name__ == "__main__":
    main()
