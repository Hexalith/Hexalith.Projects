#!/usr/bin/env python3
# /// script
# requires-python = ">=3.9"
# dependencies = []
# ///
"""Merge module help entries into shared _bmad/module-help.csv.

Reads a source CSV with module help entries and merges them into a target CSV.
Uses an anti-zombie pattern: all existing rows matching the source module value
are removed before appending fresh rows.

Legacy cleanup: when --legacy-dir and --module-code are provided, deletes old
per-module module-help.csv files from {legacy-dir}/{module-code}/ and
{legacy-dir}/core/. Only the current module and core are touched.

Exit codes: 0=success, 1=validation error, 2=runtime error
"""

import argparse
import csv
import json
import os
import re
import shutil
import stat
import sys
import tempfile
from io import StringIO
from pathlib import Path

HEADER = [
    "module",
    "skill",
    "display-name",
    "menu-code",
    "description",
    "action",
    "args",
    "phase",
    "after",
    "before",
    "required",
    "output-location",
    "outputs",
]

_SAFE_MODULE_CODE = re.compile(r"^[A-Za-z0-9][A-Za-z0-9._-]*$")
_WINDOWS_RESERVED_BASENAMES = {
    "con",
    "prn",
    "aux",
    "nul",
    *(f"com{number}" for number in range(1, 10)),
    *(f"lpt{number}" for number in range(1, 10)),
}
_RESERVED_MODULE_CODES = {"core"}


class ValidationError(ValueError):
    """An invalid invocation or input document."""


def parse_args():
    parser = argparse.ArgumentParser(
        description="Merge module help entries into shared _bmad/module-help.csv with anti-zombie pattern."
    )
    parser.add_argument("--target", required=True, help="Path to the target _bmad/module-help.csv file")
    parser.add_argument("--source", required=True, help="Path to the source module-help.csv with entries to merge")
    parser.add_argument("--legacy-dir", help="Path to _bmad/ directory to check for legacy per-module CSV files.")
    parser.add_argument("--module-code", help="Module code (required with --legacy-dir for scoping cleanup).")
    parser.add_argument("--verbose", action="store_true", help="Print detailed progress to stderr")
    return parser.parse_args()


def read_csv_rows(path: str) -> tuple[list[str], list[list[str]]]:
    """Read a CSV file, returning its header and data rows."""
    file_path = Path(path)
    if not file_path.exists():
        return [], []
    with open(file_path, "r", encoding="utf-8", newline="") as stream:
        rows = list(csv.reader(StringIO(stream.read())))
    if not rows:
        return [], []
    return rows[0], rows[1:]


def extract_module_codes(rows: list[list[str]]) -> set[str]:
    """Extract the distinct values in the CSV module column."""
    return {row[0].strip() for row in rows if row and row[0].strip()}


def filter_rows(rows: list[list[str]], module_code: str) -> list[list[str]]:
    """Remove all rows matching one module-column value."""
    return [row for row in rows if not row or row[0].strip() != module_code]


def render_csv(header: list[str], rows: list[list[str]]) -> str:
    """Render a complete prospective CSV document in memory."""
    stream = StringIO(newline="")
    writer = csv.writer(stream)
    writer.writerow(header)
    writer.writerows(rows)
    content = stream.getvalue()
    if list(csv.reader(StringIO(content))) != [header, *rows]:
        raise ValidationError("Prospective module-help.csv failed CSV validation")
    return content


def cleanup_legacy_csvs(legacy_candidates: list[Path], verbose: bool = False) -> list:
    """Delete legacy per-module CSV files for this module and core only."""
    deleted = []
    for legacy_path in legacy_candidates:
        if legacy_path.exists():
            if verbose:
                print(f"Deleting legacy CSV: {legacy_path}", file=sys.stderr)
            legacy_path.unlink()
            deleted.append(str(legacy_path))
    return deleted


def reject_unresolved_paths(named_paths: list[tuple[str, str]]) -> None:
    """Reject filesystem path arguments containing an unresolved project token."""
    for name, value in named_paths:
        if value and "{project-root}" in value:
            raise ValidationError(
                f"Unresolved '{{project-root}}' token in {name} path: {value!r}. "
                "Resolve '{project-root}' to the actual project root before running "
                "this script — it is a filesystem path, not a config value."
            )


def validate_module_code(module_code: str) -> None:
    """Require a non-reserved, portable, single-component module code."""
    if (
        not module_code
        or not _SAFE_MODULE_CODE.fullmatch(module_code)
        or module_code.endswith(".")
        or module_code.casefold() in _RESERVED_MODULE_CODES
        or module_code.split(".", 1)[0].casefold() in _WINDOWS_RESERVED_BASENAMES
    ):
        raise ValidationError(
            "--module-code must be a safe, non-reserved single path component"
        )


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


def validate_source_identity(rows: list[list[str]], module_code: str) -> None:
    """Bind cleanup scope to an explicit setup-skill marker in the source CSV."""
    expected_skills = {f"{module_code}-setup", f"bmad-{module_code}-setup"}
    source_skills = {row[1].strip() for row in rows if len(row) > 1 and row[1].strip()}
    if source_skills.isdisjoint(expected_skills):
        raise ValidationError(
            f"--module-code {module_code!r} does not identify the source module"
        )


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
        expected = legacy_root / subdir / "module-help.csv"
        if resolved_path(expected) != expected:
            raise ValidationError(
                f"Legacy cleanup path must be the direct child {expected}; symlink redirection is not allowed"
            )
        candidates.append(expected)
    return candidates


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
            raise RuntimeError("CSV staging artifact cleanup failed") from error
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
            raise RuntimeError("CSV rollback artifact cleanup failed") from error
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


def publish_csv(target: Path, content: str) -> None:
    """Atomically replace one CSV target and roll it back if replacement fails."""
    staged = None
    rollback = None
    snapshot = capture_file_snapshot(target)
    attempted = False
    failure = None
    recovery_artifact = None
    try:
        staged = stage_text(
            target,
            content,
            snapshot["mode"] if snapshot["existed"] else default_create_mode(),
        )
        if snapshot["existed"]:
            rollback = create_rollback(target)
        attempted = True
        os.replace(staged, target)
        staged = None
    except BaseException as error:
        failure = error
        if attempted:
            restored, recovery_artifact = restore_target(target, snapshot, rollback)
            if restored:
                recovery_artifact = None
    cleanup_errors = remove_artifacts(
        [staged] + ([] if recovery_artifact == rollback else [rollback])
    )
    if failure is not None:
        if recovery_artifact is not None:
            raise RuntimeError(
                f"CSV publication rollback could not restore original state; "
                f"manual recovery artifact: {recovery_artifact}"
            ) from failure
        if cleanup_errors:
            raise RuntimeError("CSV publication artifact cleanup failed") from failure
        raise failure
    if cleanup_errors:
        raise RuntimeError("CSV publication artifact cleanup failed") from cleanup_errors[0]


def run(args) -> dict:
    """Validate, compute, publish, then clean legacy state."""
    reject_unresolved_paths(
        [("--target", args.target), ("--source", args.source), ("--legacy-dir", args.legacy_dir)]
    )
    if args.legacy_dir and not args.module_code:
        raise ValidationError("--module-code is required when --legacy-dir is provided")
    if args.module_code:
        validate_module_code(args.module_code)

    requested_target = Path(args.target)
    target = effective_output_path(requested_target)
    source = Path(args.source)
    reject_alias("--target", target, "--source", source)

    legacy_candidates = []
    if args.legacy_dir:
        legacy_candidates = validate_legacy_candidates(args.legacy_dir, args.module_code)
        for candidate in legacy_candidates:
            reject_alias("--target", target, "legacy cleanup target", candidate)
            reject_alias("--source", source, "legacy cleanup target", candidate)

    source_header, source_rows = read_csv_rows(args.source)
    if not source_rows:
        raise ValidationError(f"No data rows found in source {args.source}")
    source_codes = extract_module_codes(source_rows)
    if not source_codes:
        raise ValidationError("Could not determine module code from source rows")
    if args.legacy_dir:
        if len(source_codes) != 1:
            raise ValidationError(
                "Cleanup source must contain exactly one nonempty module-column value"
            )
        validate_source_identity(source_rows, args.module_code)

    if args.verbose:
        print(f"Source module codes: {source_codes}", file=sys.stderr)
        print(f"Source rows: {len(source_rows)}", file=sys.stderr)

    target_header, target_rows = read_csv_rows(str(target))
    target_existed = target.exists()
    if args.verbose:
        print(f"Target exists: {target_existed}", file=sys.stderr)
        if target_existed:
            print(f"Existing target rows: {len(target_rows)}", file=sys.stderr)

    header = target_header if target_header else (source_header if source_header else HEADER)
    filtered_rows = target_rows
    removed_count = 0
    for code in source_codes:
        before_count = len(filtered_rows)
        filtered_rows = filter_rows(filtered_rows, code)
        removed_count += before_count - len(filtered_rows)
    merged_rows = filtered_rows + source_rows
    prospective_csv = render_csv(header, merged_rows)

    if args.verbose:
        print(f"Publishing {len(merged_rows)} data rows to {target}", file=sys.stderr)
    publish_csv(target, prospective_csv)

    legacy_deleted = []
    if args.legacy_dir:
        legacy_deleted = cleanup_legacy_csvs(legacy_candidates, args.verbose)

    return {
        "status": "success",
        "target_path": str(requested_target.resolve()),
        "target_existed": target_existed,
        "module_codes": sorted(source_codes),
        "rows_removed": removed_count,
        "rows_added": len(source_rows),
        "total_rows": len(merged_rows),
        "legacy_csvs_deleted": legacy_deleted,
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
