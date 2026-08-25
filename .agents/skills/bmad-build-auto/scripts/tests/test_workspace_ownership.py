#!/usr/bin/env python3
"""Hermetic workspace-ownership fixtures for the Build Auto workflow."""

from __future__ import annotations

import base64
import csv
import hashlib
import json
import os
import shutil
import stat
import subprocess
import tempfile
from contextlib import contextmanager
from dataclasses import dataclass
from pathlib import Path
from typing import Callable, Iterable, Iterator, Optional

try:
    import fcntl
except ImportError:  # pragma: no cover - the CI fixture runs on Linux
    fcntl = None


PROJECT_ROOT = Path(__file__).resolve().parents[5]
SKILL_ROOTS = [
    PROJECT_ROOT / ".agent" / "skills" / "bmad-build-auto",
    PROJECT_ROOT / ".agents" / "skills" / "bmad-build-auto",
    PROJECT_ROOT / ".claude" / "skills" / "bmad-build-auto",
]
SYNCED_RELATIVES = [
    Path("workflow.md"),
    Path("step-01-clarify-and-route.md"),
    Path("step-03-implement.md"),
    Path("step-04-review.md"),
    Path("scripts/tests/test_workspace_ownership.py"),
]
MANIFEST_PATHS = {
    "bmm/ship/bmad-build-auto/workflow.md": Path("workflow.md"),
    "bmm/ship/bmad-build-auto/step-01-clarify-and-route.md": Path(
        "step-01-clarify-and-route.md"
    ),
    "bmm/ship/bmad-build-auto/step-03-implement.md": Path("step-03-implement.md"),
    "bmm/ship/bmad-build-auto/step-04-review.md": Path("step-04-review.md"),
    "bmm/ship/bmad-build-auto/scripts/tests/test_workspace_ownership.py": Path(
        "scripts/tests/test_workspace_ownership.py"
    ),
}
ROUTING_ENVIRONMENT = {
    "GIT_DIR",
    "GIT_WORK_TREE",
    "GIT_INDEX_FILE",
    "GIT_COMMON_DIR",
    "GIT_OBJECT_DIRECTORY",
    "GIT_ALTERNATE_OBJECT_DIRECTORIES",
    "GIT_PREFIX",
    "GIT_CEILING_DIRECTORIES",
}


class SnapshotError(RuntimeError):
    """The workspace cannot be represented safely."""


class WorkspaceDrift(RuntimeError):
    """An ownership gate detected workspace drift."""


@dataclass(frozen=True)
class PathState:
    """No-follow identity for one raw worktree path."""

    raw_path: bytes
    kind: str
    mode: int
    digest: bytes
    content: bytes


@dataclass(frozen=True)
class Snapshot:
    """Exact identities needed by the fixture's ownership gate."""

    head: bytes
    status: bytes
    index: bytes
    tracked_patch: bytes
    tracked_paths: tuple[PathState, ...]
    untracked: tuple[PathState, ...]
    submodules: tuple[tuple[bytes, bytes, str, bytes], ...]
    controls: tuple[PathState, ...]


@dataclass(frozen=True)
class OwnedDelta:
    """A retained pre/post transition limited to reported raw paths."""

    before: Snapshot
    after: Snapshot
    reported_paths: tuple[bytes, ...]
    patch: bytes
    preimages: tuple[PathState, ...]
    postimages: tuple[PathState, ...]


@dataclass
class ExclusiveLease:
    """A continuously held OS lock bound to a worktree and token."""

    worktree: Path
    git_dir: Path
    token: str
    descriptor: int
    active: bool = True


def git_environment(extra: Optional[dict[str, str]] = None) -> dict[str, str]:
    """Remove caller-controlled repository routing before each Git command."""
    environment = {
        key: value
        for key, value in os.environ.items()
        if key not in ROUTING_ENVIRONMENT
    }
    environment["GIT_OPTIONAL_LOCKS"] = "0"
    if extra:
        environment.update(extra)
    return environment


def run(
    repo: Path,
    *arguments: str,
    input_bytes: Optional[bytes] = None,
    check: bool = True,
    env: Optional[dict[str, str]] = None,
) -> subprocess.CompletedProcess[bytes]:
    """Run Git without shell interpolation or inherited repository routing."""
    result = subprocess.run(
        ["git", "-C", str(repo), *arguments],
        input=input_bytes,
        capture_output=True,
        env=git_environment(env),
        timeout=20,
        check=False,
    )
    if check and result.returncode != 0:
        raise AssertionError(
            f"git {' '.join(arguments)} failed ({result.returncode}): "
            f"{result.stderr.decode('utf-8', 'backslashreplace')}"
        )
    return result


def initialize_repo(root: Path) -> Path:
    """Create a clean repository with several tracked path classes."""
    repo = root / "repo"
    repo.mkdir()
    run(repo, "init", "-q")
    run(repo, "config", "user.name", "Fixture")
    run(repo, "config", "user.email", "fixture@example.invalid")
    (repo / "owned.txt").write_text(
        "alpha\nbeta\ngamma\ndelta\nepsilon\nzeta\neta\ntheta\niota\n",
        encoding="utf-8",
    )
    (repo / "binary.bin").write_bytes(b"\x00base\xff\n")
    (repo / "unrelated.txt").write_text("tracked original\n", encoding="utf-8")
    (repo / "pre-staged.txt").write_text("index original\n", encoding="utf-8")
    (repo / "rename-me.txt").write_text("rename source\n", encoding="utf-8")
    run(repo, "add", "--", ".")
    run(repo, "commit", "-qm", "test: initialize fixture")
    return repo


def git_dir(repo: Path) -> Path:
    """Resolve the actual Git directory."""
    return Path(run(repo, "rev-parse", "--absolute-git-dir").stdout.strip().decode())


def raw_join(repo: Path, raw_path: bytes) -> bytes:
    """Join a repository path without decoding the raw filename."""
    return os.path.join(os.fsencode(repo), raw_path)


def stable_regular_read(raw_path: bytes) -> tuple[os.stat_result, bytes]:
    """Read a regular file without following symlinks and reject races."""
    before = os.lstat(raw_path)
    flags = os.O_RDONLY | getattr(os, "O_NOFOLLOW", 0)
    descriptor = os.open(raw_path, flags)
    try:
        opened_before = os.fstat(descriptor)
        chunks: list[bytes] = []
        while True:
            chunk = os.read(descriptor, 65536)
            if not chunk:
                break
            chunks.append(chunk)
        opened_after = os.fstat(descriptor)
    finally:
        os.close(descriptor)
    after = os.lstat(raw_path)
    identities = [
        (item.st_dev, item.st_ino, item.st_mode, item.st_size, item.st_mtime_ns)
        for item in (before, opened_before, opened_after, after)
    ]
    if any(identity != identities[0] for identity in identities[1:]):
        raise SnapshotError(f"raced path read: {raw_path!r}")
    return after, b"".join(chunks)


def path_state(repo: Path, raw_path: bytes) -> PathState:
    """Capture stable raw type/mode/content identity without following links."""
    absolute = raw_join(repo, raw_path)
    try:
        before = os.lstat(absolute)
    except FileNotFoundError:
        return PathState(raw_path, "missing", 0, hashlib.sha256(b"").digest(), b"")
    mode = stat.S_IMODE(before.st_mode)
    if stat.S_ISREG(before.st_mode):
        metadata, content = stable_regular_read(absolute)
        mode = stat.S_IMODE(metadata.st_mode)
        kind = "file"
    elif stat.S_ISLNK(before.st_mode):
        content = os.readlink(absolute)
        if isinstance(content, str):
            content = os.fsencode(content)
        after = os.lstat(absolute)
        if (before.st_dev, before.st_ino, before.st_mode, before.st_mtime_ns) != (
            after.st_dev,
            after.st_ino,
            after.st_mode,
            after.st_mtime_ns,
        ):
            raise SnapshotError(f"raced symlink read: {raw_path!r}")
        kind = "symlink"
    elif stat.S_ISDIR(before.st_mode):
        content = b""
        kind = "directory"
    else:
        raise SnapshotError(f"unsupported path type: {raw_path!r}")
    return PathState(raw_path, kind, mode, hashlib.sha256(content).digest(), content)


def index_path(repo: Path) -> Path:
    """Resolve the active index path."""
    raw = run(repo, "rev-parse", "--git-path", "index").stdout.strip()
    path = Path(os.fsdecode(raw))
    return path if path.is_absolute() else repo / path


def index_identity(repo: Path) -> bytes:
    """Hash the active index and only its actually referenced shared index."""
    paths = [index_path(repo)]
    shared = run(repo, "rev-parse", "--shared-index-path", check=False)
    if shared.returncode == 0 and shared.stdout.strip():
        shared_path = Path(os.fsdecode(shared.stdout.strip()))
        paths.append(shared_path if shared_path.is_absolute() else repo / shared_path)
    digest = hashlib.sha256()
    for path in paths:
        _, content = stable_regular_read(os.fsencode(path))
        digest.update(os.fsencode(str(path.resolve(strict=False))))
        digest.update(b"\0" + content + b"\0")
    digest.update(run(repo, "ls-files", "--stage", "-v", "-z").stdout)
    return digest.digest()


def tracked_names(repo: Path) -> tuple[bytes, ...]:
    """Return raw tracked path names, including skip-worktree entries."""
    return tuple(
        sorted(item for item in run(repo, "ls-files", "-z").stdout.split(b"\0") if item)
    )


def untracked_names(repo: Path) -> tuple[bytes, ...]:
    """Return every Git-visible untracked raw path name."""
    return tuple(
        sorted(
            item
            for item in run(
                repo, "ls-files", "--others", "--exclude-standard", "-z"
            ).stdout.split(b"\0")
            if item
        )
    )


def tracked_diff(repo: Path) -> bytes:
    """Return raw records and binary/full-index worktree hunks."""
    arguments = (
        "--binary",
        "--full-index",
        "--no-ext-diff",
        "--no-textconv",
        "--no-renames",
    )
    return run(repo, "diff", *arguments, "--raw", "-z").stdout + run(
        repo, "diff", *arguments
    ).stdout


def gitlinks(repo: Path) -> list[tuple[bytes, bytes]]:
    """Return tracked gitlink names and index object ids."""
    result: list[tuple[bytes, bytes]] = []
    for record in run(repo, "ls-files", "--stage", "-z").stdout.split(b"\0"):
        if not record:
            continue
        metadata, raw_path = record.split(b"\t", 1)
        mode, object_id, _ = metadata.split(b" ", 2)
        if mode == b"160000":
            result.append((raw_path, object_id))
    return result


def has_own_git_marker(repo: Path, raw_path: bytes) -> bool:
    """Check initialization without allowing Git to walk to a superproject."""
    marker = os.path.join(raw_join(repo, raw_path), b".git")
    try:
        metadata = os.lstat(marker)
    except FileNotFoundError:
        return False
    return stat.S_ISREG(metadata.st_mode) or stat.S_ISDIR(metadata.st_mode)


def capture_submodules(
    repo: Path, seen_git_dirs: frozenset[Path]
) -> tuple[tuple[bytes, bytes, str, bytes], ...]:
    """Capture initialized nested gitlinks and fail closed on cycles."""
    identities: list[tuple[bytes, bytes, str, bytes]] = []
    for raw_path, object_id in gitlinks(repo):
        if not has_own_git_marker(repo, raw_path):
            identities.append((raw_path, object_id, "uninitialized", path_state(repo, raw_path).digest))
            continue
        child = repo / os.fsdecode(raw_path)
        child_git_dir = git_dir(child)
        if child_git_dir in seen_git_dirs:
            raise SnapshotError(f"nested gitlink cycle: {raw_path!r}")
        child_snapshot = capture_once(child, (), seen_git_dirs | {child_git_dir})
        payload = repr(child_snapshot).encode("utf-8", "backslashreplace")
        identities.append((raw_path, object_id, "initialized", hashlib.sha256(payload).digest()))
    return tuple(identities)


def capture_once(
    repo: Path,
    controls: Iterable[bytes] = (),
    seen_git_dirs: Optional[frozenset[Path]] = None,
) -> Snapshot:
    """Capture one complete observation."""
    current_git_dir = git_dir(repo)
    seen = seen_git_dirs or frozenset({current_git_dir})
    return Snapshot(
        head=run(repo, "rev-parse", "HEAD").stdout.strip(),
        status=run(
            repo,
            "status",
            "--porcelain=v2",
            "-z",
            "--untracked-files=all",
            "--ignore-submodules=none",
        ).stdout,
        index=index_identity(repo),
        tracked_patch=tracked_diff(repo),
        tracked_paths=tuple(path_state(repo, item) for item in tracked_names(repo)),
        untracked=tuple(path_state(repo, item) for item in untracked_names(repo)),
        submodules=capture_submodules(repo, seen),
        controls=tuple(path_state(repo, item) for item in sorted(set(controls))),
    )


def capture(repo: Path, controls: Iterable[bytes] = (), attempts: int = 3) -> Snapshot:
    """Require two consecutive byte-identical complete observations."""
    last_error: Optional[Exception] = None
    for _ in range(attempts):
        try:
            first = capture_once(repo, controls)
            second = capture_once(repo, controls)
        except (OSError, SnapshotError) as error:
            last_error = error
            continue
        if first == second:
            return second
        last_error = SnapshotError("consecutive observations differed")
    raise SnapshotError(f"incomplete checkpoint: {last_error}")


def drift_classes(expected: Snapshot, current: Snapshot) -> list[str]:
    """Return differing classes in mutation-safety priority order."""
    differences: list[str] = []
    if expected.head != current.head:
        differences.append("HEAD")
    if expected.index != current.index:
        differences.append("index")
    if expected.submodules != current.submodules:
        differences.append("submodule/gitlink")
    if expected.controls != current.controls:
        differences.append("spec/control path")
    if expected.status != current.status:
        differences.append("status/path set")
    if expected.untracked != current.untracked:
        differences.append("untracked inventory/type/content")
    if (
        expected.tracked_patch != current.tracked_patch
        or expected.tracked_paths != current.tracked_paths
    ):
        differences.append("tracked worktree/hunks")
    return differences


def gate(repo: Path, expected: Snapshot, controls: Iterable[bytes] = ()) -> None:
    """Fail closed when any checkpoint class differs."""
    differences = drift_classes(expected, capture(repo, controls))
    if differences:
        raise WorkspaceDrift(", ".join(differences))


def state_map(snapshot: Snapshot) -> dict[bytes, PathState]:
    """Flatten tracked and untracked identities by raw path."""
    return {item.raw_path: item for item in (*snapshot.tracked_paths, *snapshot.untracked)}


def missing_state(raw_path: bytes) -> PathState:
    """Return the canonical absent-path identity."""
    return PathState(raw_path, "missing", 0, hashlib.sha256(b"").digest(), b"")


def changed_paths(before: Snapshot, after: Snapshot) -> set[bytes]:
    """Find exact worktree and gitlink path transitions."""
    before_map = state_map(before)
    after_map = state_map(after)
    changed = {
        raw_path
        for raw_path in before_map.keys() | after_map.keys()
        if before_map.get(raw_path) != after_map.get(raw_path)
    }
    before_links = {item[0]: item for item in before.submodules}
    after_links = {item[0]: item for item in after.submodules}
    changed.update(
        raw_path
        for raw_path in before_links.keys() | after_links.keys()
        if before_links.get(raw_path) != after_links.get(raw_path)
    )
    return changed


@contextmanager
def copied_index(repo: Path, seed_from_head: bool = False) -> Iterator[dict[str, str]]:
    """Create a private index beside the real one so split-index references resolve."""
    descriptor, raw_name = tempfile.mkstemp(prefix="bmad-owned-index-", dir=git_dir(repo))
    os.close(descriptor)
    path = Path(raw_name)
    try:
        if seed_from_head:
            path.unlink()
        else:
            shutil.copyfile(index_path(repo), path)
        environment = {"GIT_INDEX_FILE": str(path)}
        if seed_from_head:
            run(repo, "read-tree", "HEAD", env=environment)
        yield environment
    finally:
        path.unlink(missing_ok=True)


def build_owned_patch(
    repo: Path, before: Snapshot, reported_paths: Iterable[bytes]
) -> bytes:
    """Build a binary patch from retained preimages for reported paths only."""
    paths = tuple(sorted(set(reported_paths)))
    before_map = state_map(before)
    with copied_index(repo) as environment:
        for raw_path in paths:
            preimage = before_map.get(raw_path, missing_state(raw_path))
            decoded_path = os.fsdecode(raw_path)
            if preimage.kind == "missing":
                run(repo, "update-index", "--force-remove", "--", decoded_path, env=environment)
                if path_state(repo, raw_path).kind != "missing":
                    run(repo, "add", "-N", "--", decoded_path, env=environment)
                continue
            if preimage.kind == "file":
                git_mode = "100755" if preimage.mode & stat.S_IXUSR else "100644"
            elif preimage.kind == "symlink":
                git_mode = "120000"
            else:
                raise SnapshotError(
                    f"owned patch cannot represent preimage type: {preimage.kind}"
                )
            object_id = run(repo, "hash-object", "-w", "--stdin", input_bytes=preimage.content).stdout.strip()
            run(
                repo,
                "update-index",
                "--add",
                "--cacheinfo",
                git_mode,
                object_id.decode("ascii"),
                decoded_path,
                env=environment,
            )
        return run(
            repo,
            "diff",
            "--binary",
            "--full-index",
            "--no-ext-diff",
            "--no-textconv",
            "--no-renames",
            "--",
            *(os.fsdecode(item) for item in paths),
            env=environment,
        ).stdout


def capture_owned_delta(
    repo: Path,
    before: Snapshot,
    reported_paths: Iterable[bytes],
    controls: Iterable[bytes] = (),
) -> OwnedDelta:
    """Accept only the exact pre-handoff to post-handoff reported transition."""
    after = capture(repo, controls)
    if before.head != after.head:
        raise WorkspaceDrift("HEAD")
    if before.index != after.index:
        raise WorkspaceDrift("index")
    if before.controls != after.controls:
        raise WorkspaceDrift("spec/control path")
    reported = tuple(sorted(set(reported_paths)))
    actual = changed_paths(before, after)
    if actual != set(reported):
        raise WorkspaceDrift(
            f"reported path set: expected {reported!r}, observed {sorted(actual)!r}"
        )
    before_map = state_map(before)
    after_map = state_map(after)
    preimages = tuple(
        before_map.get(item, missing_state(item))
        for item in reported
    )
    postimages = tuple(
        after_map.get(item, missing_state(item))
        for item in reported
    )
    return OwnedDelta(
        before=before,
        after=after,
        reported_paths=reported,
        patch=build_owned_patch(repo, before, reported),
        preimages=preimages,
        postimages=postimages,
    )


def entry_decision(status: str, live_checkpoint: Optional[Snapshot]) -> str:
    """Model resume routing without recapturing dirty state."""
    if status in {"in-progress", "in-review"} and live_checkpoint is None:
        return "blocked-missing-live-checkpoint"
    if status == "done":
        return "fresh-followup"
    return "continue"


def worktree_identity(snapshot: Snapshot) -> tuple[object, ...]:
    """Return fields that staging must not mutate."""
    return (
        tuple(sorted(state_map(snapshot).items())),
        snapshot.submodules,
        snapshot.controls,
    )


def index_entries(repo: Path) -> dict[bytes, bytes]:
    """Map raw paths to their exact staged entry record."""
    entries: dict[bytes, bytes] = {}
    for record in run(repo, "ls-files", "--stage", "-z").stdout.split(b"\0"):
        if record:
            metadata, raw_path = record.split(b"\t", 1)
            entries[raw_path] = metadata
    return entries


def stage_exact(
    repo: Path, expected: Snapshot, delta: OwnedDelta, controls: Iterable[bytes] = ()
) -> dict[bytes, bytes]:
    """Stage exactly owned hunks while preserving other index/worktree state."""
    gate(repo, expected, controls)
    before_snapshot = capture(repo, controls)
    before_entries = index_entries(repo)
    run(repo, "apply", "--cached", "--check", "--binary", input_bytes=delta.patch)
    run(repo, "apply", "--cached", "--binary", input_bytes=delta.patch)
    after_snapshot = capture(repo, controls)
    assert worktree_identity(after_snapshot) == worktree_identity(before_snapshot)
    after_entries = index_entries(repo)
    for raw_path, entry in before_entries.items():
        if raw_path not in delta.reported_paths:
            assert after_entries.get(raw_path) == entry
    staged_owned_paths = {
        item
        for item in run(
            repo,
            "diff",
            "--cached",
            "--name-only",
            "-z",
            "--no-renames",
            "--",
            *(os.fsdecode(item) for item in delta.reported_paths),
        ).stdout.split(b"\0")
        if item
    }
    assert staged_owned_paths == set(delta.reported_paths)
    return before_entries


def commit_exact(repo: Path, delta: OwnedDelta, message: str) -> bytes:
    """Commit through a private index so unrelated staged entries cannot leak."""
    parent = run(repo, "rev-parse", "HEAD").stdout.strip()
    with copied_index(repo, seed_from_head=True) as environment:
        run(
            repo,
            "apply",
            "--cached",
            "--check",
            "--binary",
            input_bytes=delta.patch,
            env=environment,
        )
        run(repo, "apply", "--cached", "--binary", input_bytes=delta.patch, env=environment)
        tree = run(repo, "write-tree", env=environment).stdout.strip()
    commit = run(
        repo,
        "commit-tree",
        tree.decode(),
        "-p",
        parent.decode(),
        input_bytes=(message + "\n").encode(),
    ).stdout.strip()
    run(repo, "update-ref", "HEAD", commit.decode(), parent.decode())
    committed = set(
        item
        for item in run(
            repo, "diff-tree", "--no-commit-id", "--name-only", "-r", "-z", commit.decode()
        ).stdout.split(b"\0")
        if item
    )
    assert committed == set(delta.reported_paths)
    return commit


def reverse_exact(
    repo: Path, expected: Snapshot, patch: bytes, controls: Iterable[bytes] = ()
) -> None:
    """Gate, preflight, and reverse one complete owned patch."""
    gate(repo, expected, controls)
    before_snapshot = capture(repo, controls)
    before_index = index_identity(repo)
    check = run(
        repo,
        "apply",
        "--reverse",
        "--check",
        "--binary",
        input_bytes=patch,
        check=False,
    )
    if check.returncode != 0:
        assert capture(repo, controls) == before_snapshot
        assert index_identity(repo) == before_index
        raise WorkspaceDrift("reverse preflight")
    run(repo, "apply", "--reverse", "--binary", input_bytes=patch)


def authorized_replace(
    repo: Path,
    expected: Snapshot,
    delta: OwnedDelta,
    raw_path: bytes,
    old: bytes,
    new: bytes,
) -> Snapshot:
    """Apply one repair only when the full captured owned postimage remains exact."""
    gate(repo, expected)
    current = path_state(repo, raw_path)
    postimages = {item.raw_path: item for item in delta.postimages}
    if current != postimages[raw_path] or current.content.count(old) != 1:
        raise WorkspaceDrift("stale repair preimage")
    absolute = raw_join(repo, raw_path)
    replacement = current.content.replace(old, new, 1)
    descriptor = os.open(absolute, os.O_WRONLY | os.O_TRUNC | getattr(os, "O_NOFOLLOW", 0))
    try:
        os.write(descriptor, replacement)
    finally:
        os.close(descriptor)
    return capture(repo)


def serialize_state(state: PathState) -> dict[str, object]:
    """Encode raw names safely for persisted historical evidence."""
    return {
        "path_b64": base64.b64encode(state.raw_path).decode("ascii"),
        "kind": state.kind,
        "mode": state.mode,
        "sha256": state.digest.hex(),
    }


def persist_evidence(root: Path, delta: OwnedDelta) -> dict[str, str]:
    """Persist exact patch and path/type metadata with external hashes."""
    root.mkdir(parents=True, exist_ok=True)
    patch_path = root / "owned.patch"
    metadata_path = root / "owned.json"
    patch_path.write_bytes(delta.patch)
    metadata = {
        "baseline_revision": delta.before.head.decode("ascii"),
        "patch_sha256": hashlib.sha256(delta.patch).hexdigest(),
        "paths": [
            {"preimage": serialize_state(before), "postimage": serialize_state(after)}
            for before, after in zip(delta.preimages, delta.postimages)
        ],
    }
    metadata_path.write_text(json.dumps(metadata, sort_keys=True), encoding="utf-8")
    return {
        "patch_path": str(patch_path),
        "patch_hash": hashlib.sha256(patch_path.read_bytes()).hexdigest(),
        "metadata_path": str(metadata_path),
        "metadata_hash": hashlib.sha256(metadata_path.read_bytes()).hexdigest(),
    }


def load_evidence(record: dict[str, str]) -> tuple[bytes, dict[str, object]]:
    """Verify and reload exact historical evidence."""
    patch_path = Path(record["patch_path"])
    metadata_path = Path(record["metadata_path"])
    if not patch_path.is_file() or not metadata_path.is_file():
        raise WorkspaceDrift("missing historical evidence")
    patch = patch_path.read_bytes()
    metadata_bytes = metadata_path.read_bytes()
    if hashlib.sha256(patch).hexdigest() != record["patch_hash"]:
        raise WorkspaceDrift("tampered historical patch")
    if hashlib.sha256(metadata_bytes).hexdigest() != record["metadata_hash"]:
        raise WorkspaceDrift("tampered historical metadata")
    metadata = json.loads(metadata_bytes)
    if metadata["patch_sha256"] != hashlib.sha256(patch).hexdigest():
        raise WorkspaceDrift("historical patch digest mismatch")
    return patch, metadata


def acquire_lease(worktree: Path, token: str) -> Optional[ExclusiveLease]:
    """Acquire a real non-blocking OS lock for one worktree."""
    if fcntl is None:
        return None
    lock_path = git_dir(worktree) / "bmad-build-auto.lease"
    descriptor = os.open(lock_path, os.O_RDWR | os.O_CREAT, 0o600)
    try:
        fcntl.flock(descriptor, fcntl.LOCK_EX | fcntl.LOCK_NB)
    except BlockingIOError:
        os.close(descriptor)
        return None
    os.ftruncate(descriptor, 0)
    os.write(descriptor, token.encode("utf-8"))
    os.fsync(descriptor)
    return ExclusiveLease(worktree.resolve(), git_dir(worktree), token, descriptor)


def release_lease(lease: ExclusiveLease) -> None:
    """Release a held lease and mark it unusable."""
    if lease.active:
        if fcntl is not None:
            fcntl.flock(lease.descriptor, fcntl.LOCK_UN)
        os.close(lease.descriptor)
        lease.active = False


def prove_exclusive(repo: Path, lease: Optional[ExclusiveLease], token: str) -> bool:
    """Require live lock, matching token, worktree, and Git-directory identity."""
    if lease is None or not lease.active or lease.token != token:
        return False
    try:
        os.fstat(lease.descriptor)
    except OSError:
        return False
    return repo.resolve() == lease.worktree and git_dir(repo) == lease.git_dir


def create_submodule(parent_root: Path, repo: Path) -> Path:
    """Add a local initialized gitlink for nested-state drift coverage."""
    source = parent_root / "submodule-source"
    source.mkdir()
    run(source, "init", "-q")
    run(source, "config", "user.name", "Fixture")
    run(source, "config", "user.email", "fixture@example.invalid")
    (source / "child.txt").write_text("one\n", encoding="utf-8")
    run(source, "add", "--", "child.txt")
    run(source, "commit", "-qm", "test: initialize submodule")
    run(
        repo,
        "-c",
        "protocol.file.allow=always",
        "submodule",
        "add",
        "-q",
        str(source),
        "modules/child",
        env={"GIT_ALLOW_PROTOCOL": "file"},
    )
    run(repo, "commit", "-qam", "test: add submodule")
    child = repo / "modules" / "child"
    run(child, "config", "user.name", "Fixture")
    run(child, "config", "user.email", "fixture@example.invalid")
    return child


def assert_order(text: str, *needles: str) -> None:
    """Assert instruction fragments occur in the required order."""
    offsets = [text.index(needle) for needle in needles]
    assert offsets == sorted(offsets), f"instruction order violated: {needles}"


def test_clean_owned_run_and_reported_path_boundary() -> None:
    """Retained pre/post state excludes unrelated worktree and index content."""
    with tempfile.TemporaryDirectory() as temporary:
        repo = initialize_repo(Path(temporary))
        owned_lines = (repo / "owned.txt").read_text(encoding="utf-8").splitlines()
        owned_lines[0] = "same-path user preimage"
        (repo / "owned.txt").write_text("\n".join(owned_lines) + "\n", encoding="utf-8")
        (repo / "unrelated.txt").write_text("tracked user change\n", encoding="utf-8")
        (repo / "loose.keep").write_text("untracked user file\n", encoding="utf-8")
        (repo / "pre-staged.txt").write_text("staged user change\n", encoding="utf-8")
        run(repo, "add", "--", "pre-staged.txt")
        before = capture(repo)
        before_worktree = worktree_identity(before)
        before_index = index_entries(repo)

        owned_lines[7] = "owned-late-hunk"
        (repo / "owned.txt").write_text("\n".join(owned_lines) + "\n", encoding="utf-8")
        (repo / "binary.bin").write_bytes(b"\x00owned\xfe\n")
        (repo / "created.txt").write_text("created\n", encoding="utf-8")
        reported = (b"owned.txt", b"binary.bin", b"created.txt")
        delta = capture_owned_delta(repo, before, reported)
        assert delta.patch
        assert b"unrelated.txt" not in delta.patch
        assert b"loose.keep" not in delta.patch
        assert b"same-path user preimage" not in delta.patch
        expected = delta.after
        stage_exact(repo, expected, delta)
        after_stage = capture(repo)
        assert worktree_identity(after_stage) == worktree_identity(delta.after)
        for raw_path, entry in before_index.items():
            if raw_path not in reported:
                assert index_entries(repo).get(raw_path) == entry
        commit = commit_exact(repo, delta, "test: exact owned commit")
        committed = run(repo, "show", "--pretty=", "--name-only", "-z", commit.decode()).stdout
        assert b"pre-staged.txt" not in committed
        assert (repo / "unrelated.txt").read_text(encoding="utf-8") == "tracked user change\n"
        assert (repo / "loose.keep").read_text(encoding="utf-8") == "untracked user file\n"
        assert (repo / "owned.txt").read_text(encoding="utf-8").startswith("same-path user preimage\n")
        owned_worktree_diff = run(repo, "diff", "--", "owned.txt").stdout
        assert b"same-path user preimage" in owned_worktree_diff
        assert b"owned-late-hunk" not in owned_worktree_diff
        assert b"pre-staged.txt" in run(repo, "diff", "--cached", "--name-only", "-z").stdout
        assert before_worktree != worktree_identity(delta.after)

    with tempfile.TemporaryDirectory() as temporary:
        repo = initialize_repo(Path(temporary))
        before = capture(repo)
        (repo / "owned.txt").write_text("owned\n", encoding="utf-8")
        (repo / "unreported.txt").write_text("external\n", encoding="utf-8")
        try:
            capture_owned_delta(repo, before, (b"owned.txt",))
        except WorkspaceDrift as error:
            assert "reported path set" in str(error)
        else:
            raise AssertionError("unreported extra path was accepted")


def test_drift_decision_table_and_control_identity() -> None:
    """HEAD/index/path/hunk/untracked/control drift always blocks without writes."""

    def mutate_head(repo: Path) -> None:
        (repo / "external.txt").write_text("external\n", encoding="utf-8")
        run(repo, "add", "--", "external.txt")
        run(repo, "commit", "-qm", "test: external head drift")

    def mutate_index(repo: Path) -> None:
        (repo / "pre-staged.txt").write_text("external stage\n", encoding="utf-8")
        run(repo, "add", "--", "pre-staged.txt")

    def mutate_hunk(repo: Path) -> None:
        (repo / "owned.txt").write_text("alpha\nexternal\ngamma\ndelta\nepsilon\nzeta\neta\ntheta\niota\n", encoding="utf-8")

    def mutate_path(repo: Path) -> None:
        (repo / "extra.txt").write_text("extra\n", encoding="utf-8")

    cases: tuple[tuple[str, Callable[[Path], None], str], ...] = (
        ("HEAD", mutate_head, "HEAD"),
        ("index", mutate_index, "index"),
        ("hunk", mutate_hunk, "tracked worktree/hunks"),
        ("path", mutate_path, "status/path set"),
    )
    for name, mutator, expected_class in cases:
        with tempfile.TemporaryDirectory() as temporary:
            repo = initialize_repo(Path(temporary))
            if name == "hunk":
                (repo / "owned.txt").write_text("alpha\nowned\ngamma\ndelta\nepsilon\nzeta\neta\ntheta\niota\n", encoding="utf-8")
            expected = capture(repo)
            mutator(repo)
            drifted = capture(repo)
            try:
                gate(repo, expected)
            except WorkspaceDrift as error:
                assert expected_class in str(error), (name, error)
            else:
                raise AssertionError(f"{name} drift was accepted")
            assert capture(repo) == drifted

    with tempfile.TemporaryDirectory() as temporary:
        repo = initialize_repo(Path(temporary))
        (repo / ".gitignore").write_text("control.state\n", encoding="utf-8")
        run(repo, "add", "--", ".gitignore")
        run(repo, "commit", "-qm", "test: ignore control")
        control = b"control.state"
        (repo / "control.state").write_bytes(b"one")
        expected = capture(repo, (control,))
        (repo / "control.state").write_bytes(b"two")
        assert "spec/control path" in drift_classes(expected, capture(repo, (control,)))
        (repo / "control.state").unlink()
        os.symlink("elsewhere", repo / "control.state")
        assert "spec/control path" in drift_classes(expected, capture(repo, (control,)))


def test_untracked_submodule_unusual_rename_and_mode_transitions() -> None:
    """Raw names, types, modes, renames, and gitlink state remain observable."""
    with tempfile.TemporaryDirectory() as temporary:
        root = Path(temporary)
        repo = initialize_repo(root)
        child = create_submodule(root, repo)
        expected = capture(repo)
        (child / "child.txt").write_text("two\n", encoding="utf-8")
        run(child, "add", "--", "child.txt")
        run(child, "commit", "-qm", "test: advance submodule")
        assert "submodule/gitlink" in drift_classes(expected, capture(repo))

    with tempfile.TemporaryDirectory() as temporary:
        repo = initialize_repo(Path(temporary))
        object_id = run(repo, "rev-parse", "HEAD").stdout.strip().decode()
        run(repo, "update-index", "--add", "--cacheinfo", f"160000,{object_id},modules/missing")
        run(repo, "commit", "-qm", "test: add uninitialized gitlink")
        snapshot = capture(repo)
        record = next(item for item in snapshot.submodules if item[0] == b"modules/missing")
        assert record[2] == "uninitialized"

    if os.name != "nt":
        with tempfile.TemporaryDirectory() as temporary:
            repo = initialize_repo(Path(temporary))
            before = capture(repo)
            newline_name = b"renamed\nfile.txt"
            non_utf8_name = b"raw-\xff.txt"
            os.rename(raw_join(repo, b"rename-me.txt"), raw_join(repo, newline_name))
            descriptor = os.open(raw_join(repo, non_utf8_name), os.O_WRONLY | os.O_CREAT, 0o600)
            try:
                os.write(descriptor, b"raw name\n")
            finally:
                os.close(descriptor)
            os.chmod(repo / "owned.txt", 0o755)
            reported = (b"rename-me.txt", newline_name, non_utf8_name, b"owned.txt")
            delta = capture_owned_delta(repo, before, reported)
            assert set(delta.reported_paths) == set(reported)
            assert newline_name in state_map(delta.after)
            assert non_utf8_name in state_map(delta.after)
            owned_after = state_map(delta.after)[b"owned.txt"]
            assert owned_after.mode & stat.S_IXUSR


def test_incomplete_checkpoint_rejects_unsupported_type() -> None:
    """A path type that cannot be represented fails closed."""
    if os.name == "nt":
        return
    with tempfile.TemporaryDirectory() as temporary:
        repo = initialize_repo(Path(temporary))
        os.mkfifo(repo / "unsupported.pipe")
        try:
            path_state(repo, b"unsupported.pipe")
        except SnapshotError as error:
            assert "unsupported path type" in str(error)
        else:
            raise AssertionError("unsupported path type was silently omitted")


def test_authorized_repair_and_stale_preimage() -> None:
    """Repairs preserve unrelated bytes and stale preimages cannot write or refresh."""
    with tempfile.TemporaryDirectory() as temporary:
        repo = initialize_repo(Path(temporary))
        before = capture(repo)
        (repo / "owned.txt").write_text(
            "user-prefix\nbeta\ngamma\ndelta\nepsilon\nzeta\neta\ntheta\nowned-tail\n",
            encoding="utf-8",
        )
        delta = capture_owned_delta(repo, before, (b"owned.txt",))
        refreshed = authorized_replace(
            repo, delta.after, delta, b"owned.txt", b"owned-tail", b"repaired-tail"
        )
        content = (repo / "owned.txt").read_bytes()
        assert content.startswith(b"user-prefix\n")
        gate(repo, refreshed)

        (repo / "owned.txt").write_bytes(content.replace(b"user-prefix", b"external-prefix"))
        current_checkpoint = capture(repo)
        before_attempt = (repo / "owned.txt").read_bytes()
        try:
            authorized_replace(
                repo,
                current_checkpoint,
                delta,
                b"owned.txt",
                b"owned-tail",
                b"should-not-write",
            )
        except WorkspaceDrift as error:
            assert "stale repair preimage" in str(error)
        else:
            raise AssertionError("stale repair preimage was accepted")
        assert (repo / "owned.txt").read_bytes() == before_attempt
        assert capture(repo) == current_checkpoint


def test_shared_reversal_and_failed_preflight_are_atomic() -> None:
    """Exact reversal preserves controls; overlap failure changes no bytes or index."""
    with tempfile.TemporaryDirectory() as temporary:
        repo = initialize_repo(Path(temporary))
        before = capture(repo)
        lines = (repo / "owned.txt").read_text(encoding="utf-8").splitlines()
        lines[1] = "owned-first"
        lines[7] = "owned-second"
        (repo / "owned.txt").write_text("\n".join(lines) + "\n", encoding="utf-8")
        (repo / "created.txt").write_text("owned create\n", encoding="utf-8")
        delta = capture_owned_delta(repo, before, (b"owned.txt", b"created.txt"))
        (repo / "review.patch").write_bytes(delta.patch)
        expected = capture(repo, (b"review.patch",))
        reverse_exact(repo, expected, delta.patch, (b"review.patch",))
        assert state_map(capture(repo))[b"owned.txt"] == state_map(before)[b"owned.txt"]
        assert not (repo / "created.txt").exists()
        assert (repo / "review.patch").read_bytes() == delta.patch

    with tempfile.TemporaryDirectory() as temporary:
        repo = initialize_repo(Path(temporary))
        before = capture(repo)
        lines = (repo / "owned.txt").read_text(encoding="utf-8").splitlines()
        lines[1] = "owned-first"
        lines[7] = "owned-second"
        (repo / "owned.txt").write_text("\n".join(lines) + "\n", encoding="utf-8")
        delta = capture_owned_delta(repo, before, (b"owned.txt",))
        overlapped = lines.copy()
        overlapped[1] = "external-overlap"
        (repo / "owned.txt").write_text("\n".join(overlapped) + "\n", encoding="utf-8")
        current_checkpoint = capture(repo)
        before_bytes = (repo / "owned.txt").read_bytes()
        before_index = index_identity(repo)
        try:
            reverse_exact(repo, current_checkpoint, delta.patch)
        except WorkspaceDrift as error:
            assert "reverse preflight" in str(error)
        else:
            raise AssertionError("overlapping reverse preflight was accepted")
        assert (repo / "owned.txt").read_bytes() == before_bytes
        assert index_identity(repo) == before_index
        assert capture(repo) == current_checkpoint


def test_done_followup_uses_persisted_exact_evidence() -> None:
    """Later commits cannot contaminate verified historical owned evidence."""
    with tempfile.TemporaryDirectory() as temporary:
        root = Path(temporary)
        repo = initialize_repo(root)
        before = capture(repo)
        (repo / "owned.txt").write_text("completed owned change\n", encoding="utf-8")
        delta = capture_owned_delta(repo, before, (b"owned.txt",))
        record = persist_evidence(root / "evidence", delta)
        stage_exact(repo, delta.after, delta)
        commit_exact(repo, delta, "test: completed owned run")
        (repo / "later.txt").write_text("later unrelated\n", encoding="utf-8")
        run(repo, "add", "--", "later.txt")
        run(repo, "commit", "-qm", "test: later unrelated commit")

        fresh = capture(repo)
        assert entry_decision("done", None) == "fresh-followup"
        patch, metadata = load_evidence(record)
        assert patch == delta.patch
        assert b"later.txt" not in patch
        assert metadata["baseline_revision"] == before.head.decode("ascii")
        gate(repo, fresh)

        missing = dict(record)
        missing["patch_path"] = str(root / "missing.patch")
        try:
            load_evidence(missing)
        except WorkspaceDrift as error:
            assert "missing historical evidence" in str(error)
        else:
            raise AssertionError("missing evidence was accepted")

        Path(record["metadata_path"]).write_text("{}", encoding="utf-8")
        try:
            load_evidence(record)
        except WorkspaceDrift as error:
            assert "tampered historical metadata" in str(error)
        else:
            raise AssertionError("tampered evidence was accepted")


def test_interrupted_resume_uses_workflow_state_helper() -> None:
    """An interrupted active spec cannot recapture dirty state as owned."""
    with tempfile.TemporaryDirectory() as temporary:
        repo = initialize_repo(Path(temporary))
        before = capture(repo)
        assert entry_decision("in-progress", None) == "blocked-missing-live-checkpoint"
        assert entry_decision("in-review", None) == "blocked-missing-live-checkpoint"
        assert entry_decision("in-progress", before) == "continue"
        assert capture(repo) == before


def test_exclusive_lease_requires_real_continuous_lock() -> None:
    """Wrong, lost, mismatched, and competing leases never prove exclusivity."""
    if fcntl is None:
        return
    with tempfile.TemporaryDirectory() as temporary:
        root = Path(temporary)
        repo = initialize_repo(root)
        worktree = root / "isolated"
        run(repo, "branch", "fixture-isolated")
        run(repo, "worktree", "add", "-q", str(worktree), "fixture-isolated")
        token = "fixture-private-token"
        lease = acquire_lease(worktree, token)
        assert lease is not None
        assert prove_exclusive(worktree, lease, token)
        baseline = capture(worktree)
        (worktree / "owned.txt").write_text("isolated owned change\n", encoding="utf-8")
        delta = capture_owned_delta(worktree, baseline, (b"owned.txt",))
        reverse_exact(worktree, delta.after, delta.patch)
        assert state_map(capture(worktree))[b"owned.txt"] == state_map(baseline)[b"owned.txt"]
        assert not prove_exclusive(worktree, lease, "wrong-token")
        assert not prove_exclusive(repo, lease, token)
        assert acquire_lease(worktree, "competing-holder") is None
        release_lease(lease)
        assert not prove_exclusive(worktree, lease, token)
        replacement = acquire_lease(worktree, "replacement-holder")
        assert replacement is not None
        release_lease(replacement)


def test_instruction_order_forbidden_operations_and_ci_policy() -> None:
    """Normative instructions encode every ownership gate and safe route."""
    workflow = (SKILL_ROOTS[1] / "workflow.md").read_text(encoding="utf-8")
    step_01 = (SKILL_ROOTS[1] / "step-01-clarify-and-route.md").read_text(encoding="utf-8")
    step_03 = (SKILL_ROOTS[1] / "step-03-implement.md").read_text(encoding="utf-8")
    step_04 = (SKILL_ROOTS[1] / "step-04-review.md").read_text(encoding="utf-8")
    assert "stable double capture" in workflow
    assert "including skip-worktree and filtered paths" in workflow
    assert "git rev-parse --shared-index-path" in workflow
    assert "recursively initialized nested-gitlink state" in workflow
    assert "including ignored paths" in workflow
    assert "done` → **EARLY EXIT**" in step_01
    assert "set `review_loop_iteration` to `0`" not in step_01
    assert "control-owned creation with an absent preimage" in step_03
    assert_order(step_03, "Immediately before invoking", "Invoke the subagent")
    assert "recursively captured nested-submodule" in step_03
    assert "no_vcs_review = true" in step_04
    assert "verified persisted `historical_owned_delta`" in step_04
    assert "reset `review_loop_iteration` to `0` as a declared control mutation" in step_04
    assert "increment it by 1 as a declared control mutation" in step_04
    assert step_04.count("append the one pending triage entry exactly once") == 1
    assert "binary/full-index patch plus metadata" in step_04
    assert_order(
        step_04,
        "Run an ownership gate immediately before staging",
        "Run another ownership gate immediately before commit",
    )
    assert "revert code changes" not in step_04.lower()
    for forbidden in ("git add -A", "git reset --hard", "git checkout --", "git restore", "git clean"):
        assert forbidden not in step_03
        assert forbidden not in step_04
    assert "whole-path staging and broad Git mutation commands are forbidden" in workflow
    assert "deferred-work ledger, bundle intent, and generated `_bmad/render/**`" in workflow
    assert "no-mutation rule above overrides normal HALT write-back" in workflow

    ci = (PROJECT_ROOT / ".github/workflows/ci.yml").read_text(encoding="utf-8")
    exact_step = (
        "      - name: Validate Build Auto workspace ownership\n"
        "        env:\n"
        "          PYTHONDONTWRITEBYTECODE: '1'\n"
        "        run: python3 .agents/skills/bmad-build-auto/scripts/tests/test_workspace_ownership.py"
    )
    assert ci.count(exact_step) == 1
    policy = (PROJECT_ROOT / "tests/tools/run-ci-workflow-gates.ps1").read_text(encoding="utf-8")
    assert "test_workspace_ownership.py" in policy


def test_all_entry_points_and_manifest_rows_are_exact_and_unique() -> None:
    """All installed copies and manifest hashes are exact and unique."""
    for relative in SYNCED_RELATIVES:
        copies = [root / relative for root in SKILL_ROOTS]
        assert all(path.is_file() for path in copies), copies
        expected = copies[0].read_bytes()
        assert all(path.read_bytes() == expected for path in copies), copies

    with open(PROJECT_ROOT / "_bmad/_config/files-manifest.csv", encoding="utf-8", newline="") as stream:
        rows = list(csv.DictReader(stream))
    for manifest_path, relative in MANIFEST_PATHS.items():
        matches = [row for row in rows if row["path"] == manifest_path]
        assert len(matches) == 1, (manifest_path, matches)
        expected_hash = hashlib.sha256((SKILL_ROOTS[1] / relative).read_bytes()).hexdigest()
        assert matches[0]["hash"] == expected_hash, manifest_path


if __name__ == "__main__":
    tests: Iterable[Callable[[], None]] = (
        test_clean_owned_run_and_reported_path_boundary,
        test_drift_decision_table_and_control_identity,
        test_untracked_submodule_unusual_rename_and_mode_transitions,
        test_incomplete_checkpoint_rejects_unsupported_type,
        test_authorized_repair_and_stale_preimage,
        test_shared_reversal_and_failed_preflight_are_atomic,
        test_done_followup_uses_persisted_exact_evidence,
        test_interrupted_resume_uses_workflow_state_helper,
        test_exclusive_lease_requires_real_continuous_lock,
        test_instruction_order_forbidden_operations_and_ci_policy,
        test_all_entry_points_and_manifest_rows_are_exact_and_unique,
    )
    failures = 0
    for test in tests:
        try:
            test()
        except Exception as error:  # noqa: BLE001 - dependency-free self-runner
            failures += 1
            print(f"FAIL {test.__name__}: {error}")
        else:
            print(f"PASS {test.__name__}")
    if failures:
        raise SystemExit(1)
    print(f"workspace-ownership: PASSED — {len(tuple(tests))} fixture groups")
