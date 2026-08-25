#!/usr/bin/env python3
# /// script
# requires-python = ">=3.9"
# dependencies = []
# ///
"""Remove legacy module directories from _bmad/ after config migration.

After merge-config.py and merge-help-csv.py have migrated config data and
deleted individual legacy files, this script removes the now-redundant
directory trees. These directories contain skill files that are already
installed at .claude/skills/ (or equivalent) — only the config files at
_bmad/ root need to persist.

Every requested target is validated as a direct child of the resolved _bmad
root before target contents are inspected. Skill-bearing directories are
removed only when every skill has a distinct external replacement containing
a regular SKILL.md. Directories without skills (like _config/) are removed
directly once all validation succeeds.

Exit codes: 0=success (including nothing to remove), 1=validation error, 2=runtime error
"""

import argparse
import json
import shutil
import sys
from pathlib import Path


def parse_args():
    parser = argparse.ArgumentParser(
        description="Remove legacy module directories from _bmad/ after config migration."
    )
    parser.add_argument(
        "--bmad-dir",
        required=True,
        help="Path to the _bmad/ directory",
    )
    parser.add_argument(
        "--module-code",
        required=True,
        help="Module code being cleaned up (e.g. 'bmb')",
    )
    parser.add_argument(
        "--also-remove",
        action="append",
        default=[],
        help="Additional directory names under _bmad/ to remove (repeatable)",
    )
    parser.add_argument(
        "--skills-dir",
        help="Path to .claude/skills/ — required when a cleanup target contains "
        "legacy skills",
    )
    parser.add_argument(
        "--verbose",
        action="store_true",
        help="Print detailed progress to stderr",
    )
    return parser.parse_args()


def validation_error(error: str, **details) -> None:
    """Emit a validation error as JSON and stop before cleanup begins."""
    result = {"status": "error", "error": error}
    result.update(details)
    print(json.dumps(result, indent=2))
    sys.exit(1)


def runtime_error(error: str, **details) -> None:
    """Emit a runtime error as JSON using the documented exit code."""
    result = {"status": "error", "error": error}
    result.update(details)
    print(json.dumps(result, indent=2))
    sys.exit(2)


def resolve_cleanup_targets(bmad_dir: str, directory_names: list) -> tuple:
    """Resolve and validate all cleanup targets before inspecting any target.

    Each requested value must be one relative path component whose resolved
    target has the resolved _bmad root as its parent.

    Returns:
        (resolved_bmad_root, [(requested_name, resolved_target), ...]) tuple

    Raises SystemExit(1) if any requested value is unsafe, or SystemExit(2)
    if a filesystem error occurs while inspecting a target.
    """
    try:
        bmad_root = Path(bmad_dir).resolve()
    except (OSError, RuntimeError) as error:
        validation_error(
            "The _bmad root could not be resolved",
            bmad_dir=bmad_dir,
            reason=str(error),
        )
    targets = []
    rejected = []

    for directory_name in directory_names:
        requested = Path(directory_name)
        requested_entry = bmad_root / requested
        reason = None
        resolved_target = None

        if not directory_name or requested.is_absolute() or len(requested.parts) != 1:
            reason = "target must be one relative directory name"
        else:
            try:
                target_is_symlink = requested_entry.is_symlink()
            except (OSError, RuntimeError) as error:
                runtime_error(
                    "Failed to inspect cleanup target entry",
                    directory=directory_name,
                    path=str(requested_entry),
                    reason=str(error),
                )
            if target_is_symlink:
                reason = "cleanup target entry must not be a symlink"

        if reason is None:
            try:
                resolved_target = requested_entry.resolve()
            except (OSError, RuntimeError) as error:
                reason = f"target could not be resolved: {error}"
            else:
                if resolved_target.parent != bmad_root:
                    reason = "resolved target is not a direct child of the _bmad root"

        if reason:
            rejected.append(
                {
                    "directory": directory_name,
                    "resolved_path": (
                        str(resolved_target) if resolved_target is not None else None
                    ),
                    "reason": reason,
                }
            )
        else:
            targets.append((directory_name, resolved_target))

    target_names = {}
    for directory_name, resolved_target in targets:
        target_names.setdefault(resolved_target, []).append(directory_name)
    collisions = {
        resolved_target: names
        for resolved_target, names in target_names.items()
        if len(names) > 1
    }
    if collisions:
        targets = [
            (directory_name, resolved_target)
            for directory_name, resolved_target in targets
            if resolved_target not in collisions
        ]
        for resolved_target, names in collisions.items():
            for directory_name in names:
                rejected.append(
                    {
                        "directory": directory_name,
                        "resolved_path": str(resolved_target),
                        "reason": (
                            "multiple requested names resolve to the same cleanup target: "
                            + ", ".join(names)
                        ),
                    }
                )

    if rejected:
        validation_error(
            "Cleanup targets must resolve to direct children of the _bmad root",
            bmad_dir=str(bmad_root),
            rejected_targets=[item["directory"] for item in rejected],
            target_errors=rejected,
        )

    return bmad_root, targets


def find_skill_dirs(base_path: Path) -> list:
    """Find directories that contain a SKILL.md file.

    Walks the directory tree and returns each directory containing a SKILL.md.
    These are considered legacy skill directories.

    Returns:
        List of resolved skill directory paths.
    """
    skills = []
    try:
        if not base_path.is_dir():
            return skills
        for skill_md in base_path.rglob("SKILL.md"):
            if skill_md.is_file():
                skills.append(skill_md.parent.resolve())
    except (OSError, RuntimeError) as error:
        runtime_error(
            "Failed to scan cleanup target for legacy skills",
            path=str(base_path),
            reason=str(error),
        )
    return sorted(set(skills), key=str)


def protect_live_migration_data(targets: list) -> None:
    """Reject cleanup when any target still contains live migration files."""
    protected = []
    for directory_name, target in targets:
        try:
            if not target.is_dir():
                continue
            live_files = []
            for marker_name in ("config.yaml", "module-help.csv"):
                marker = target / marker_name
                if marker.exists() or marker.is_symlink():
                    live_files.append(marker_name)
        except (OSError, RuntimeError) as error:
            runtime_error(
                "Failed to inspect cleanup target for live migration data",
                directory=directory_name,
                path=str(target),
                reason=str(error),
            )
        if live_files:
            protected.append(
                {
                    "directory": directory_name,
                    "path": str(target),
                    "live_migration_files": live_files,
                }
            )

    if protected:
        validation_error(
            "Cleanup targets still contain live migration data",
            protected_directories=protected,
        )


def verify_skills_installed(targets: list, skills_dir, verbose: bool = False) -> list:
    """Verify distinct, external replacement copies for every legacy skill.

    Scans each validated target for skill folders, then checks that a matching
    directory exists under skills_dir, resolves outside every cleanup target,
    differs from the legacy skill directory, and contains a regular SKILL.md.
    Directories that contain no skills (like _config/) are silently skipped.

    Returns:
        List of verified skill names.

    Raises SystemExit(1) if skills_dir cannot be resolved, or if any skill
    replacement is missing or unsafe, or SystemExit(2) if a filesystem error
    occurs while inspecting a target or replacement.
    """
    all_verified = []
    missing = []
    unsafe = []
    cleanup_paths = [target for _, target in targets]
    if skills_dir:
        try:
            resolved_skills_dir = Path(skills_dir).resolve()
        except (OSError, RuntimeError) as error:
            validation_error(
                "The skills directory could not be resolved",
                skills_dir=skills_dir,
                reason=str(error),
            )
    else:
        resolved_skills_dir = None

    for directory_name, legacy_path in targets:
        try:
            legacy_is_dir = legacy_path.is_dir()
        except (OSError, RuntimeError) as error:
            runtime_error(
                "Failed to inspect cleanup target for legacy skills",
                directory=directory_name,
                path=str(legacy_path),
                reason=str(error),
            )
        if not legacy_is_dir:
            continue

        legacy_skill_dirs = find_skill_dirs(legacy_path)
        if not legacy_skill_dirs:
            if verbose:
                print(
                    f"No skills found in {directory_name}/ — skipping verification",
                    file=sys.stderr,
                )
            continue

        for legacy_skill_dir in legacy_skill_dirs:
            skill_name = legacy_skill_dir.name
            if resolved_skills_dir is None:
                missing.append(
                    {
                        "skill": skill_name,
                        "legacy_path": str(legacy_skill_dir),
                        "reason": "--skills-dir is required for skill-bearing targets",
                    }
                )
                if verbose:
                    print(
                        f"MISSING: {skill_name} — --skills-dir is required for skill-bearing targets",
                        file=sys.stderr,
                    )
                continue

            unresolved_installed_path = resolved_skills_dir / skill_name
            try:
                installed_path = unresolved_installed_path.resolve()
            except (OSError, RuntimeError) as error:
                unsafe.append(
                    {
                        "skill": skill_name,
                        "legacy_path": str(legacy_skill_dir),
                        "replacement_path": str(unresolved_installed_path),
                        "reason": f"replacement could not be resolved: {error}",
                    }
                )
                if verbose:
                    print(
                        f"UNSAFE: {skill_name} — replacement could not be resolved: {error}",
                        file=sys.stderr,
                    )
                continue
            installed_skill_md = installed_path / "SKILL.md"
            unsafe_reason = None

            if installed_path == legacy_skill_dir:
                unsafe_reason = "replacement aliases the legacy skill directory"
            elif any(
                installed_path == cleanup_path or cleanup_path in installed_path.parents
                for cleanup_path in cleanup_paths
            ):
                unsafe_reason = "replacement is inside a cleanup target"
            else:
                try:
                    installed_is_dir = installed_path.is_dir()
                    skill_md_is_file = installed_skill_md.is_file()
                    skill_md_is_symlink = installed_skill_md.is_symlink()
                except (OSError, RuntimeError) as error:
                    runtime_error(
                        "Failed to inspect a legacy skill replacement",
                        skill=skill_name,
                        path=str(installed_path),
                        reason=str(error),
                    )

            if unsafe_reason:
                unsafe.append(
                    {
                        "skill": skill_name,
                        "legacy_path": str(legacy_skill_dir),
                        "replacement_path": str(installed_path),
                        "reason": unsafe_reason,
                    }
                )
                if verbose:
                    print(
                        f"UNSAFE: {skill_name} — {unsafe_reason} at {installed_path}",
                        file=sys.stderr,
                    )
            elif not installed_is_dir:
                missing.append(
                    {
                        "skill": skill_name,
                        "legacy_path": str(legacy_skill_dir),
                        "replacement_path": str(installed_path),
                        "reason": "replacement directory is missing",
                    }
                )
                if verbose:
                    print(
                        f"MISSING: {skill_name} not found at {installed_path}",
                        file=sys.stderr,
                    )
                continue
            elif not skill_md_is_file or skill_md_is_symlink:
                missing.append(
                    {
                        "skill": skill_name,
                        "legacy_path": str(legacy_skill_dir),
                        "replacement_path": str(installed_path),
                        "reason": "replacement does not contain a regular SKILL.md",
                    }
                )
                if verbose:
                    print(
                        f"MISSING: {skill_name} at {installed_path} does not contain a regular SKILL.md",
                        file=sys.stderr,
                    )
                continue

            else:
                all_verified.append(skill_name)
                if verbose:
                    print(
                        f"Verified: {skill_name} has a safe replacement at {installed_path}",
                        file=sys.stderr,
                    )

    if missing or unsafe:
        validation_error(
            "Legacy skills do not have proven safe replacements",
            missing_skills=missing,
            unsafe_skills=unsafe,
            skills_dir=str(resolved_skills_dir) if resolved_skills_dir else None,
        )

    return sorted(set(all_verified))


def count_files(path: Path) -> int:
    """Count all files recursively in a directory."""
    count = 0
    try:
        for item in path.rglob("*"):
            if item.is_file():
                count += 1
    except (OSError, RuntimeError) as error:
        runtime_error(
            "Failed to count files in cleanup target",
            path=str(path),
            reason=str(error),
        )
    return count


def cleanup_directories(targets: list, verbose: bool = False) -> tuple:
    """Remove validated directories under bmad_dir.

    Returns:
        (removed, not_found, total_files_removed) tuple
    """
    removed = []
    not_found = []
    total_files = 0

    for dirname, target in targets:
        try:
            if not target.exists():
                not_found.append(dirname)
                if verbose:
                    print(f"Not found (skipping): {target}", file=sys.stderr)
                continue

            if not target.is_dir():
                if verbose:
                    print(f"Not a directory (skipping): {target}", file=sys.stderr)
                not_found.append(dirname)
                continue

            file_count = count_files(target)
            if verbose:
                print(
                    f"Removing {target} ({file_count} files)",
                    file=sys.stderr,
                )

            shutil.rmtree(target)
        except (OSError, RuntimeError) as error:
            runtime_error(
                "Failed to inspect or remove cleanup target",
                path=str(target),
                reason=str(error),
                directories_removed=removed,
                directories_failed=dirname,
            )

        removed.append(dirname)
        total_files += file_count

    return removed, not_found, total_files


def reject_unresolved_paths(named_paths: list[tuple[str, str]]) -> None:
    """Exit with a clear error if any path argument still contains the literal
    ``{project-root}`` token. That token is meaningful only inside config
    values; filesystem path arguments must be resolved by the caller. Failing
    loudly here prevents silently operating on a junk ``{project-root}/`` directory.
    """
    for name, value in named_paths:
        if value and "{project-root}" in value:
            print(
                json.dumps(
                    {
                        "status": "error",
                        "error": (
                            f"Unresolved '{{project-root}}' token in {name} path: {value!r}. "
                            "Resolve '{project-root}' to the actual project root before running "
                            "this script — it is a filesystem path, not a config value."
                        ),
                    },
                    indent=2,
                )
            )
            sys.exit(1)


def main():
    args = parse_args()

    reject_unresolved_paths(
        [("--bmad-dir", args.bmad_dir), ("--skills-dir", args.skills_dir)]
    )

    module_code = args.module_code

    # Build the list of directories to remove
    dirs_to_remove = [module_code, "core"] + args.also_remove
    # Deduplicate while preserving order
    seen = set()
    unique_dirs = []
    for d in dirs_to_remove:
        if d not in seen:
            seen.add(d)
            unique_dirs.append(d)
    dirs_to_remove = unique_dirs

    bmad_root, cleanup_targets = resolve_cleanup_targets(
        args.bmad_dir, dirs_to_remove
    )

    if args.verbose:
        print(f"Directories to remove: {dirs_to_remove}", file=sys.stderr)

    # Batch safety checks must all pass before any directory is removed.
    protect_live_migration_data(cleanup_targets)
    if args.skills_dir and args.verbose:
        print(
            f"Verifying skills installed at {args.skills_dir}",
            file=sys.stderr,
        )
    verified_skills = verify_skills_installed(
        cleanup_targets, args.skills_dir, args.verbose
    )

    if args.skills_dir:
        if args.verbose:
            print("All skill replacements verified", file=sys.stderr)

    # Remove directories
    removed, not_found, total_files = cleanup_directories(
        cleanup_targets, args.verbose
    )

    # Build result
    result = {
        "status": "success",
        "bmad_dir": str(bmad_root),
        "directories_removed": removed,
        "directories_not_found": not_found,
        "files_removed_count": total_files,
    }

    if args.skills_dir:
        result["safety_checks"] = {
            "skills_verified": True,
            "skills_dir": str(Path(args.skills_dir).resolve()),
            "verified_skills": verified_skills,
        }
    else:
        result["safety_checks"] = None

    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
