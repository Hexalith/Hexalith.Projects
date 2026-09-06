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

When --skills-dir is provided, the script verifies that every skill found
in the legacy directories exists at the installed location before removing
anything. Directories without skills (like _config/) are removed directly.

Exit codes: 0=success (including nothing to remove), 1=validation error, 2=runtime error
"""

import argparse
import json
import shutil
import sys
import tempfile
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
        help="Path to .claude/skills/ — enables safety verification that skills "
        "are installed before removing legacy copies",
    )
    parser.add_argument(
        "--verbose",
        action="store_true",
        help="Print detailed progress to stderr",
    )
    return parser.parse_args()


def find_skill_dirs(base_path: str) -> list:
    """Find directories that contain a SKILL.md file.

    Walks the directory tree and returns the leaf directory name for each
    directory containing a SKILL.md. These are considered skill directories.

    Returns:
        List of skill directory names (e.g. ['bmad-agent-builder', 'bmad-builder-setup'])
    """
    skills = []
    root = Path(base_path)
    if not root.exists():
        return skills
    for skill_md in root.rglob("SKILL.md"):
        skills.append(skill_md.parent.name)
    return sorted(set(skills))


def verify_skills_installed(
    bmad_dir: str, dirs_to_check: list, skills_dir: str, verbose: bool = False
) -> list:
    """Verify that skills in legacy directories exist at the installed location.

    Scans each directory in dirs_to_check for skill folders (containing SKILL.md),
    then checks that a matching directory exists under skills_dir. Directories
    that contain no skills (like _config/) are silently skipped.

    Returns:
        List of verified skill names.

    Raises SystemExit(1) if any skills are missing from skills_dir.
    """
    all_verified = []
    missing = []

    for dirname in dirs_to_check:
        legacy_path = Path(bmad_dir) / dirname
        if not legacy_path.exists():
            continue

        skill_names = find_skill_dirs(str(legacy_path))
        if not skill_names:
            if verbose:
                print(
                    f"No skills found in {dirname}/ — skipping verification",
                    file=sys.stderr,
                )
            continue

        for skill_name in skill_names:
            installed_path = Path(skills_dir) / skill_name
            if installed_path.is_dir():
                all_verified.append(skill_name)
                if verbose:
                    print(
                        f"Verified: {skill_name} exists at {installed_path}",
                        file=sys.stderr,
                    )
            else:
                missing.append(skill_name)
                if verbose:
                    print(
                        f"MISSING: {skill_name} not found at {installed_path}",
                        file=sys.stderr,
                    )

    if missing:
        error_result = {
            "status": "error",
            "error": "Skills not found at installed location",
            "missing_skills": missing,
            "skills_dir": str(Path(skills_dir).resolve()),
        }
        print(json.dumps(error_result, indent=2))
        sys.exit(1)

    return sorted(set(all_verified))


def count_files(path: Path) -> int:
    """Count all files recursively in a directory."""
    count = 0
    for item in path.rglob("*"):
        if item.is_file():
            count += 1
    return count


def print_verbose(verbose: bool, message: str) -> None:
    """Write a best-effort diagnostic that cannot affect cleanup state."""
    if not verbose:
        return
    try:
        print(message, file=sys.stderr)
    except Exception:
        pass


def cleanup_directories(
    bmad_dir: str, dirs_to_remove: list, verbose: bool = False
) -> tuple:
    """Remove specified directories under bmad_dir as one logical batch.

    Returns:
        (removed, not_found, total_files_removed) tuple
    """
    not_found = []
    removable = []
    total_files = 0
    bmad_root = Path(bmad_dir).resolve()

    for dirname in dirs_to_remove:
        try:
            target = bmad_root / dirname
            if not target.exists():
                not_found.append(dirname)
                print_verbose(verbose, f"Not found (skipping): {target}")
                continue

            if not target.is_dir():
                print_verbose(verbose, f"Not a directory (skipping): {target}")
                not_found.append(dirname)
                continue

            file_count = count_files(target)
        except (OSError, RuntimeError) as e:
            error_result = {
                "status": "error",
                "error": f"Failed to preflight {target}: {e}",
                "phase": "preflight",
                "directories_removed": [],
                "directories_failed": dirname,
                "directories_restored": [],
                "recovery_directory": None,
                "recovery_targets": [],
            }
            print(json.dumps(error_result, indent=2))
            sys.exit(2)

        removable.append((dirname, target, file_count))
        total_files += file_count

    if not removable:
        return [], not_found, 0

    try:
        transaction_dir = Path(
            tempfile.mkdtemp(prefix=".legacy-cleanup-", dir=bmad_root)
        )
    except (OSError, RuntimeError) as e:
        failed_dirname, failed_target, _ = removable[0]
        error_result = {
            "status": "error",
            "error": f"Failed to create cleanup staging directory: {e}",
            "phase": "staging",
            "directories_removed": [],
            "directories_failed": failed_dirname,
            "failed_target": str(failed_target),
            "directories_restored": [],
            "recovery_directory": None,
            "recovery_targets": [],
        }
        print(json.dumps(error_result, indent=2))
        sys.exit(2)

    staged = []
    for index, (dirname, target, file_count) in enumerate(removable):
        staged_path = transaction_dir / f"{index:04d}"
        print_verbose(
            verbose,
            f"Staging {target} ({file_count} files) at {staged_path}",
        )

        try:
            target.rename(staged_path)
        except (OSError, RuntimeError) as e:
            restored = []
            rollback_errors = []
            recovery_targets = []

            for (
                staged_dirname,
                original_path,
                prior_staged_path,
                _,
            ) in reversed(staged):
                try:
                    prior_staged_path.rename(original_path)
                    restored.append(staged_dirname)
                    print_verbose(
                        verbose,
                        f"Restored {original_path} from {prior_staged_path}",
                    )
                except (OSError, RuntimeError) as rollback_error:
                    rollback_errors.append(
                        {
                            "directory": staged_dirname,
                            "error": str(rollback_error),
                        }
                    )
                    recovery_targets.append(
                        {
                            "directory": staged_dirname,
                            "original_path": str(original_path),
                            "staged_path": str(prior_staged_path),
                        }
                    )

            staging_cleanup_error = None
            recovery_directory = None
            if not recovery_targets:
                try:
                    transaction_dir.rmdir()
                except (OSError, RuntimeError) as cleanup_error:
                    staging_cleanup_error = str(cleanup_error)
                    recovery_directory = str(transaction_dir)
            else:
                recovery_directory = str(transaction_dir)
            error_result = {
                "status": "error",
                "error": f"Failed to stage {target}: {e}",
                "phase": "staging",
                "directories_removed": [],
                "directories_failed": dirname,
                "failed_target": str(target),
                "directories_restored": restored,
                "rollback_complete": not recovery_targets,
                "rollback_errors": rollback_errors,
                "recovery_directory": recovery_directory,
                "recovery_targets": recovery_targets,
            }
            if staging_cleanup_error is not None:
                error_result["staging_cleanup_error"] = staging_cleanup_error
            print(json.dumps(error_result, indent=2))
            sys.exit(2)

        staged.append((dirname, target, staged_path, file_count))

    removed = [dirname for dirname, _, _, _ in staged]
    print_verbose(verbose, f"Finalizing removal from {transaction_dir}")

    try:
        shutil.rmtree(transaction_dir)
    except (OSError, RuntimeError) as e:
        recovery_targets = []
        recovery_probe_errors = []
        for dirname, original_path, staged_path, _ in staged:
            mapping = {
                "directory": dirname,
                "original_path": str(original_path),
                "staged_path": str(staged_path),
            }
            try:
                staged_path_exists = staged_path.exists()
            except (OSError, RuntimeError) as probe_error:
                staged_path_exists = True
                recovery_probe_errors.append(
                    {
                        "directory": dirname,
                        "staged_path": str(staged_path),
                        "error": str(probe_error),
                    }
                )
            if staged_path_exists:
                recovery_targets.append(mapping)

        error_result = {
            "status": "error",
            "error": f"Failed to finalize cleanup at {transaction_dir}: {e}",
            "phase": "final-cleanup",
            "directories_removed": removed,
            "directories_failed": None,
            "directories_restored": [],
            "recovery_directory": str(transaction_dir),
            "recovery_targets": recovery_targets,
            "recovery_probe_errors": recovery_probe_errors,
        }
        print(json.dumps(error_result, indent=2))
        sys.exit(2)

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

    bmad_dir = args.bmad_dir
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

    if args.verbose:
        print(f"Directories to remove: {dirs_to_remove}", file=sys.stderr)

    # Safety check: verify skills are installed before removing
    verified_skills = None
    if args.skills_dir:
        if args.verbose:
            print(
                f"Verifying skills installed at {args.skills_dir}",
                file=sys.stderr,
            )
        verified_skills = verify_skills_installed(
            bmad_dir, dirs_to_remove, args.skills_dir, args.verbose
        )

    # Remove directories
    removed, not_found, total_files = cleanup_directories(
        bmad_dir, dirs_to_remove, args.verbose
    )

    # Build result
    result = {
        "status": "success",
        "bmad_dir": str(Path(bmad_dir).resolve()),
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
