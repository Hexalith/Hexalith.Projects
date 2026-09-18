#!/usr/bin/env python3
"""Hermetic workspace-ownership fixtures for Build Auto.

Dependency-free: temporary Git repositories, instruction-copy inspection,
and installer-manifest integrity. Run with PYTHONDONTWRITEBYTECODE=1.
"""

from __future__ import annotations

import base64
import csv
import fcntl
import hashlib
import json
import os
import re
import stat
import subprocess
import sys
import tempfile
from dataclasses import dataclass, field
from pathlib import Path
from typing import Callable, Iterable, Optional


PROJECT_ROOT = Path(__file__).resolve().parents[5]
ENTRY_POINTS = [PROJECT_ROOT / name for name in (".agent", ".agents", ".claude")]
SKILL_RELATIVE = Path("skills") / "bmad-build-auto"
WORKFLOW_NAME = "workflow.md"
STEP01_NAME = "step-01-clarify-and-route.md"
STEP03_NAME = "step-03-implement.md"
STEP04_NAME = "step-04-review.md"
TEST_RELATIVE = Path("scripts") / "tests" / "test_workspace_ownership.py"
MANIFEST = PROJECT_ROOT / "_bmad" / "_config" / "files-manifest.csv"
CI_WORKFLOW = PROJECT_ROOT / ".github" / "workflows" / "ci.yml"
MANIFEST_PATHS = {
    "bmm/ship/bmad-build-auto/workflow.md": SKILL_RELATIVE / WORKFLOW_NAME,
    "bmm/ship/bmad-build-auto/step-01-clarify-and-route.md": SKILL_RELATIVE / STEP01_NAME,
    "bmm/ship/bmad-build-auto/step-03-implement.md": SKILL_RELATIVE / STEP03_NAME,
    "bmm/ship/bmad-build-auto/step-04-review.md": SKILL_RELATIVE / STEP04_NAME,
    "bmm/ship/bmad-build-auto/scripts/tests/test_workspace_ownership.py": (
        SKILL_RELATIVE / TEST_RELATIVE
    ),
}
STEP03_HEADINGS = (
    "### Ownership session",
    "### Capture expected workspace",
    "### Capture baseline_revision",
    "### Mark in-progress",
    "### Implement",
    "### Post-handoff ownership check",
    "### Capture owned_delta",
    "### Verify",
)
STEP04_HEADINGS = (
    "### Ownership gate",
    "### Mark in-review",
    "### Stage the owned diff",
    "### Review",
    "### Classify",
    "### Restore or repair",
    "### Finalize",
)
FORBIDDEN_IMPERATIVE = (
    "git add -A",
    "git add --all",
    "git reset --hard",
    "git clean",
    "revert code changes",
)
GIT_ROUTING_VARS = (
    "GIT_DIR",
    "GIT_WORK_TREE",
    "GIT_INDEX_FILE",
    "GIT_OBJECT_DIRECTORY",
    "GIT_ALTERNATE_OBJECT_DIRECTORIES",
    "GIT_COMMON_DIR",
    "GIT_PREFIX",
    "GIT_CEILING_DIRECTORIES",
    "GIT_DISCOVERY_ACROSS_FILESYSTEM",
    "GIT_NAMESPACE",
)
O_NOFOLLOW = getattr(os, "O_NOFOLLOW", 0)
MATRIX_SCENARIOS = (
    "clean_owned_run",
    "head_or_index_drift",
    "worktree_or_path_set_drift",
    "authorized_repair",
    "shared_repair_reversal",
    "exclusive_isolated_reversal",
    "interrupted_resume",
    "dirty_follow_up_entry",
    "orchestrator_re_drive_handoff",
    "done_follow_up_review",
)


class OwnershipHalt(Exception):
    """Workflow-equivalent ownership halt."""

    def __init__(self, differing_class: str):
        self.differing_class = differing_class
        super().__init__(f"workspace ownership drift: {differing_class}")


@dataclass(frozen=True)
class PathRecord:
    kind: str
    mode: int
    digest: str


@dataclass(frozen=True)
class Checkpoint:
    head: str
    index_digest: str
    split_index_ref: Optional[str]
    split_index_digest: Optional[str]
    porcelain_v2: bytes
    paths: tuple[tuple[bytes, PathRecord], ...]
    gitlinks: tuple[tuple[bytes, tuple], ...]
    control: tuple[tuple[bytes, PathRecord], ...]
    tracked: tuple[bytes, ...]

    def path_map(self) -> dict[bytes, PathRecord]:
        return dict(self.paths)

    def control_map(self) -> dict[bytes, PathRecord]:
        return dict(self.control)

    def gitlink_map(self) -> dict[bytes, tuple]:
        return dict(self.gitlinks)


@dataclass
class OwnedDelta:
    patch: bytes
    patch_digest: str
    implementation_paths: tuple[bytes, ...]
    preimages: dict[bytes, bytes]
    postimages: dict[bytes, bytes]
    types: dict[bytes, str]
    modes: dict[bytes, tuple[int, int]]
    control_owned: dict[bytes, bytes]
    rename_pairs: tuple[tuple[bytes, bytes], ...] = ()


@dataclass
class Session:
    baseline_revision: str
    expected: Optional[Checkpoint]
    owned: Optional[OwnedDelta]
    live: bool
    control_names: tuple[bytes, ...]
    no_vcs: bool = False
    followup: bool = False


@dataclass
class WorktreeLease:
    worktree: Path
    fd: Optional[int] = field(default=None, repr=False)

    @property
    def path(self) -> Path:
        resolved = self.worktree.resolve()
        return git_dir(resolved) / "build-auto-exclusive.lock"

    def acquire(self) -> "WorktreeLease":
        lock = self.path
        named = self.worktree.resolve()
        if named not in lock.resolve().parents and lock.parent != git_dir(named):
            raise OwnershipHalt("exclusive isolated ownership unproven")
        lock.parent.mkdir(parents=True, exist_ok=True)
        self.fd = os.open(lock, os.O_CREAT | os.O_RDWR, 0o600)
        try:
            fcntl.flock(self.fd, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except (BlockingIOError, OSError) as error:
            os.close(self.fd)
            self.fd = None
            raise OwnershipHalt("exclusive isolated ownership unproven") from error
        return self

    def held(self) -> bool:
        return self.fd is not None

    def names(self, repo: Path) -> bool:
        try:
            return (
                self.held()
                and self.worktree.resolve() == repo.resolve()
                and self.path.parent == git_dir(repo)
            )
        except (OSError, RuntimeError):
            return False

    def release(self) -> None:
        if self.fd is None:
            return
        fcntl.flock(self.fd, fcntl.LOCK_UN)
        os.close(self.fd)
        self.fd = None


def hermetic_env(repo: Path, extra: Optional[dict[str, str]] = None) -> dict[str, str]:
    """Strip repository-routing Git variables and pin fixture identity."""
    env = {key: value for key, value in os.environ.items() if not key.startswith("GIT_")}
    env.update(
        {
            "GIT_AUTHOR_NAME": "ownership-fixture",
            "GIT_AUTHOR_EMAIL": "ownership@fixture.test",
            "GIT_COMMITTER_NAME": "ownership-fixture",
            "GIT_COMMITTER_EMAIL": "ownership@fixture.test",
            "GIT_CONFIG_NOSYSTEM": "1",
            "GIT_ATTR_NOSYSTEM": "1",
            "GIT_CONFIG_GLOBAL": os.devnull,
            "GIT_OPTIONAL_LOCKS": "0",
            "HOME": str(repo.parent),
            "XDG_CONFIG_HOME": str(repo.parent),
            "LC_ALL": "C",
            "LANG": "C",
        }
    )
    if extra:
        env.update(extra)
    return env


def run_git(
    repo: Path,
    *args: str,
    extra_env: Optional[dict[str, str]] = None,
    check: bool = True,
    stdin: Optional[bytes] = None,
) -> subprocess.CompletedProcess[bytes]:
    result = subprocess.run(
        ["git", "-C", str(repo), *args],
        env=hermetic_env(repo, extra_env),
        input=stdin,
        capture_output=True,
    )
    if check and result.returncode != 0:
        raise RuntimeError(
            f"git {' '.join(args)} failed: {result.stderr.decode('utf-8', 'replace')}"
        )
    return result


def init_repo(root: Path) -> Path:
    root.mkdir(parents=True, exist_ok=True)
    subprocess.run(
        ["git", "init", str(root)],
        env=hermetic_env(root),
        check=True,
        capture_output=True,
    )
    run_git(root, "config", "user.name", "ownership-fixture")
    run_git(root, "config", "user.email", "ownership@fixture.test")
    run_git(root, "config", "core.autocrlf", "false")
    run_git(root, "config", "core.ignorecase", "false")
    run_git(root, "config", "core.safecrlf", "false")
    return root


def git_dir(repo: Path) -> Path:
    raw = run_git(repo, "rev-parse", "--absolute-git-dir").stdout.strip().decode("ascii")
    return Path(raw)


def git_path(repo: Path, name: str) -> Path:
    raw = run_git(repo, "rev-parse", "--git-path", name).stdout.strip().decode("utf-8")
    path = Path(raw)
    if path.is_absolute():
        return path
    return (repo / path).resolve()


def join_raw(repo: Path, raw: bytes) -> Path:
    return Path(os.fsdecode(os.fsencode(repo) + b"/" + raw))


def write_raw(repo: Path, raw: bytes, data: bytes, mode: int = 0o644) -> None:
    path = join_raw(repo, raw)
    path.parent.mkdir(parents=True, exist_ok=True)
    flags = os.O_CREAT | os.O_WRONLY | os.O_TRUNC | O_NOFOLLOW
    fd = os.open(path, flags, mode)
    try:
        os.write(fd, data)
    finally:
        os.close(fd)
    os.chmod(path, mode)


def read_raw(repo: Path, raw: bytes) -> bytes:
    path = join_raw(repo, raw)
    fd = os.open(path, os.O_RDONLY | O_NOFOLLOW)
    try:
        return os.read(fd, os.fstat(fd).st_size + 1)
    finally:
        os.close(fd)


def _kind_from_stat(status: os.stat_result) -> str:
    if stat.S_ISLNK(status.st_mode):
        return "symlink"
    if stat.S_ISREG(status.st_mode):
        return "file"
    if stat.S_ISDIR(status.st_mode):
        return "directory"
    return "unsupported"


def _same_stat(first: os.stat_result, second: os.stat_result) -> bool:
    return (
        first.st_ino,
        first.st_dev,
        first.st_mode,
        first.st_size,
        first.st_mtime_ns,
        first.st_nlink,
    ) == (
        second.st_ino,
        second.st_dev,
        second.st_mode,
        second.st_size,
        second.st_mtime_ns,
        second.st_nlink,
    )


def stable_nofollow_identity(path: Path) -> PathRecord:
    first = os.lstat(path)
    kind = _kind_from_stat(first)
    if kind == "unsupported":
        raise OwnershipHalt("unsupported path type")
    try:
        if kind == "symlink":
            target = os.readlink(path)
            payload = os.fsencode(target) if isinstance(target, str) else target
        elif kind == "file":
            fd = os.open(path, os.O_RDONLY | O_NOFOLLOW)
            try:
                payload = os.read(fd, first.st_size + 1)
            finally:
                os.close(fd)
        else:
            payload = b""
    except OSError as error:
        raise OwnershipHalt("raced path observation") from error
    second = os.lstat(path)
    if not _same_stat(first, second) or (kind == "file" and len(payload) != first.st_size):
        raise OwnershipHalt("raced path observation")
    return PathRecord(kind=kind, mode=first.st_mode, digest=hashlib.sha256(payload).hexdigest())


def walk_raw(repo: Path) -> dict[bytes, PathRecord]:
    records: dict[bytes, PathRecord] = {}

    def walk(current: Path, rel: bytes) -> None:
        with os.scandir(current) as entries:
            for entry in entries:
                name = os.fsencode(entry.name)
                if name == b".git":
                    continue
                child_rel = name if rel == b"" else rel + b"/" + name
                child = Path(entry.path)
                records[child_rel] = stable_nofollow_identity(child)
                if entry.is_dir(follow_symlinks=False) and not entry.is_symlink():
                    walk(child, child_rel)

    walk(repo, b"")
    return records


def _advance_index_entry(data: bytes, pos: int, limit: int) -> tuple[bytes, int, int]:
    if pos + 62 > limit:
        raise OwnershipHalt("incomplete ownership snapshot")
    mode = int.from_bytes(data[pos + 24 : pos + 28], "big")
    flags = int.from_bytes(data[pos + 60 : pos + 62], "big")
    extended = bool(flags & 0x4000)
    header = 62 + (2 if extended else 0)
    if pos + header > limit:
        raise OwnershipHalt("incomplete ownership snapshot")
    name_len = flags & 0xFFF
    if name_len == 0xFFF:
        try:
            nul = data.index(b"\0", pos + header, limit)
        except ValueError as error:
            raise OwnershipHalt("incomplete ownership snapshot") from error
        name_len = nul - (pos + header)
    end = pos + header + name_len
    if end >= limit or end + 1 > len(data):
        raise OwnershipHalt("incomplete ownership snapshot")
    name = data[pos + header : end]
    raw_len = header + name_len + 1
    pad = (8 - (raw_len % 8)) % 8
    nxt = pos + raw_len + pad
    if nxt > limit:
        raise OwnershipHalt("incomplete ownership snapshot")
    return name, mode, nxt


def parse_index_entries(data: bytes) -> list[tuple[bytes, int]]:
    if data[:4] != b"DIRC" or len(data) < 32:
        raise OwnershipHalt("incomplete ownership snapshot")
    count = int.from_bytes(data[8:12], "big")
    pos = 12
    limit = len(data) - 20
    entries: list[tuple[bytes, int]] = []
    for _ in range(count):
        name, mode, pos = _advance_index_entry(data, pos, limit)
        entries.append((name, mode))
    return entries


def parse_index_extensions(data: bytes) -> list[tuple[bytes, bytes]]:
    if data[:4] != b"DIRC" or len(data) < 32:
        raise OwnershipHalt("incomplete ownership snapshot")
    count = int.from_bytes(data[8:12], "big")
    pos = 12
    limit = len(data) - 20
    for _ in range(count):
        _name, _mode, pos = _advance_index_entry(data, pos, limit)
    checksum_start = limit
    extensions: list[tuple[bytes, bytes]] = []
    while pos + 8 <= checksum_start:
        signature = data[pos : pos + 4]
        size = int.from_bytes(data[pos + 4 : pos + 8], "big")
        if size > checksum_start - (pos + 8):
            raise OwnershipHalt("incomplete ownership snapshot")
        payload = data[pos + 8 : pos + 8 + size]
        extensions.append((signature, payload))
        pos += 8 + size
    if pos > checksum_start:
        raise OwnershipHalt("incomplete ownership snapshot")
    return extensions


def referenced_split_index(index_bytes: bytes, directory: Path) -> tuple[Optional[str], Optional[bytes]]:
    for signature, payload in parse_index_extensions(index_bytes):
        if signature == b"link" and len(payload) >= 20:
            digest = payload[:20].hex()
            path = directory / f"sharedindex.{digest}"
            if not path.is_file():
                raise OwnershipHalt("incomplete ownership snapshot")
            return digest, path.read_bytes()
    return None, None


def observe_gitlinks(repo: Path, entries: Iterable[tuple[bytes, int]]) -> dict[bytes, tuple]:
    observed: dict[bytes, tuple] = {}
    for name, mode in entries:
        if mode != 0o160000:
            continue
        dest = join_raw(repo, name)
        try:
            dest_stat = os.lstat(dest)
        except OSError:
            observed[name] = ("uninitialized", None, None, (), ())
            continue
        if not stat.S_ISDIR(dest_stat.st_mode):
            observed[name] = ("uninitialized", None, None, (), ())
            continue
        marker = dest / ".git"
        if not os.path.lexists(marker):
            observed[name] = ("uninitialized", None, None, (), ())
            continue
        nested_head = run_git(dest, "rev-parse", "HEAD").stdout.strip().decode("ascii")
        nested_index = git_path(dest, "index")
        nested_digest = (
            hashlib.sha256(nested_index.read_bytes()).hexdigest() if nested_index.is_file() else None
        )
        nested_entries = (
            parse_index_entries(nested_index.read_bytes()) if nested_index.is_file() else []
        )
        nested_links = observe_gitlinks(dest, nested_entries)
        nested_paths = tuple(sorted(walk_raw(dest).items()))
        observed[name] = (
            "initialized",
            nested_head,
            nested_digest,
            tuple(sorted(nested_links.items())),
            nested_paths,
        )
    return observed


def _observe(repo: Path, control_names: Iterable[bytes]) -> Checkpoint:
    head = run_git(repo, "rev-parse", "HEAD").stdout.strip().decode("ascii")
    index_path = git_path(repo, "index")
    if not index_path.is_file():
        raise OwnershipHalt("incomplete ownership snapshot")
    index_bytes = index_path.read_bytes()
    split_ref, split_bytes = referenced_split_index(index_bytes, git_dir(repo))
    porcelain = run_git(
        repo,
        "-c",
        "status.relativePaths=false",
        "status",
        "--porcelain=v2",
        "-z",
        "--untracked-files=all",
        "--ignored=matching",
        "--no-renames",
    ).stdout
    entries = parse_index_entries(index_bytes)
    paths = walk_raw(repo)
    control: dict[bytes, PathRecord] = {}
    for name in control_names:
        path = join_raw(repo, name)
        if path.exists() or path.is_symlink():
            control[name] = stable_nofollow_identity(path)
    gitlinks = observe_gitlinks(repo, entries)
    return Checkpoint(
        head=head,
        index_digest=hashlib.sha256(index_bytes).hexdigest(),
        split_index_ref=split_ref,
        split_index_digest=hashlib.sha256(split_bytes).hexdigest() if split_bytes is not None else None,
        porcelain_v2=porcelain,
        paths=tuple(sorted(paths.items())),
        gitlinks=tuple(sorted(gitlinks.items())),
        control=tuple(sorted(control.items())),
        tracked=tuple(sorted(name for name, _mode in entries)),
    )


def capture_checkpoint(
    repo: Path,
    control_names: Iterable[bytes] = (),
    inject: Optional[Callable[[], None]] = None,
) -> Checkpoint:
    first = _observe(repo, control_names)
    if inject is not None:
        inject()
    second = _observe(repo, control_names)
    if first != second:
        raise OwnershipHalt("raced path observation")
    return first


def differing_class(
    expected: Checkpoint,
    actual: Checkpoint,
    owned: Optional[OwnedDelta] = None,
    ignore_owned_paths: bool = False,
) -> Optional[str]:
    owned_paths = set(owned.implementation_paths) if owned else set()
    if expected.head != actual.head:
        return "head"
    expected_paths = expected.path_map()
    actual_paths = actual.path_map()
    expected_names = set(expected_paths)
    actual_names = set(actual_paths)
    if ignore_owned_paths:
        expected_names -= owned_paths
        actual_names -= owned_paths
    if expected_names != actual_names:
        return "path-set"
    if (
        expected.index_digest != actual.index_digest
        or expected.split_index_ref != actual.split_index_ref
        or expected.split_index_digest != actual.split_index_digest
    ):
        return "index"
    if expected.gitlinks != actual.gitlinks:
        return "gitlink"
    if expected.control != actual.control:
        return "control-path"
    tracked = set(expected.tracked)
    for name in expected_names:
        if expected_paths[name] == actual_paths[name]:
            continue
        if name in owned_paths:
            return "owned-hunk"
        if name not in tracked and expected_paths[name].kind != "directory":
            return "untracked"
        return "worktree"
    return None


def revalidate(
    session: Session,
    repo: Path,
    ignore_owned_paths: bool = False,
) -> Checkpoint:
    if session.no_vcs or session.expected is None:
        raise OwnershipHalt("incomplete ownership snapshot")
    actual = capture_checkpoint(repo, session.control_names)
    klass = differing_class(
        session.expected,
        actual,
        owned=session.owned,
        ignore_owned_paths=ignore_owned_paths,
    )
    if klass:
        raise OwnershipHalt(klass)
    return actual


def refresh_expected(session: Session, repo: Path) -> None:
    session.expected = capture_checkpoint(repo, session.control_names)


def enter_workflow(
    status: str,
    *,
    session: Optional[Session],
    dirty_classes: Iterable[str] = (),
    orchestrator_bookkeeping: bool = False,
    vcs: bool = True,
    followup: bool = False,
) -> str:
    dirty = tuple(dirty_classes)
    if not vcs:
        return "no-vcs"
    if followup or status == "done":
        if dirty or orchestrator_bookkeeping:
            raise OwnershipHalt("dirty follow-up entry")
        return "fresh-followup"
    if status in {"in-progress", "in-review"}:
        if session is None or not session.live:
            raise OwnershipHalt("missing live ownership session")
        return "resume"
    if orchestrator_bookkeeping:
        raise OwnershipHalt("orchestrator re-drive handoff")
    if dirty:
        raise OwnershipHalt("dirty first-pass entry")
    return "first-pass"


def file_bytes(repo: Path, raw: bytes) -> bytes:
    path = join_raw(repo, raw)
    if path.is_symlink():
        target = os.readlink(path)
        return os.fsencode(target) if isinstance(target, str) else target
    return read_raw(repo, raw)


def make_path_patch(rel: bytes, pre: Optional[bytes], post: Optional[bytes], pre_mode: int, post_mode: int) -> bytes:
    with tempfile.TemporaryDirectory() as temporary:
        root = Path(temporary)
        before = root / "before"
        after = root / "after"
        args = ["git", "diff", "--no-index", "--binary", "--full-index", "--"]
        if pre is None:
            after.write_bytes(post or b"")
            os.chmod(after, post_mode & 0o777)
            args.extend(["/dev/null", str(after)])
        elif post is None:
            before.write_bytes(pre)
            os.chmod(before, pre_mode & 0o777)
            args.extend([str(before), "/dev/null"])
        else:
            before.write_bytes(pre)
            after.write_bytes(post)
            os.chmod(before, pre_mode & 0o777)
            os.chmod(after, post_mode & 0o777)
            args.extend([str(before), str(after)])
        result = subprocess.run(args, env=hermetic_env(root), capture_output=True)
        if result.returncode not in {0, 1}:
            raise RuntimeError(result.stderr.decode("utf-8", "replace"))
        text = result.stdout.decode("utf-8", "surrogateescape")
        name = rel.decode("utf-8", "surrogateescape")
        text = re.sub(r"^diff --git .+$", f"diff --git a/{name} b/{name}", text, count=1, flags=re.M)
        if pre is None:
            text = re.sub(r"^--- .+", "--- /dev/null", text, count=1, flags=re.M)
            text = re.sub(r"^\+\+\+ .+", f"+++ b/{name}", text, count=1, flags=re.M)
        elif post is None:
            text = re.sub(r"^--- .+", f"--- a/{name}", text, count=1, flags=re.M)
            text = re.sub(r"^\+\+\+ .+", "+++ /dev/null", text, count=1, flags=re.M)
        else:
            text = re.sub(r"^--- .+", f"--- a/{name}", text, count=1, flags=re.M)
            text = re.sub(r"^\+\+\+ .+", f"+++ b/{name}", text, count=1, flags=re.M)
        return text.encode("utf-8", "surrogateescape")


def build_owned_delta(
    repo: Path,
    allowlist: Iterable[bytes],
    preimages: dict[bytes, bytes],
    control_owned: dict[bytes, bytes],
    rename_pairs: Iterable[tuple[bytes, bytes]] = (),
) -> OwnedDelta:
    postimages: dict[bytes, bytes] = {}
    types: dict[bytes, str] = {}
    modes: dict[bytes, tuple[int, int]] = {}
    patches: list[bytes] = []
    names = tuple(allowlist)
    index_modes = dict(parse_index_entries(git_path(repo, "index").read_bytes()))
    for name in names:
        path = join_raw(repo, name)
        exists = path.exists() or path.is_symlink()
        pre = preimages.get(name)
        post = file_bytes(repo, name) if exists else None
        if exists:
            record = stable_nofollow_identity(path)
            types[name] = record.kind
            post_mode = record.mode
        else:
            types[name] = "delete"
            post_mode = 0
        pre_mode = index_modes.get(name, 0o100644 if pre is not None else 0)
        modes[name] = (pre_mode, post_mode)
        if post is not None:
            postimages[name] = post
        content_same = pre == post and pre is not None
        mode_same = exists and pre is not None and (pre_mode & 0o777) == (post_mode & 0o777)
        if content_same and mode_same:
            continue
        patches.append(make_path_patch(name, pre, post, pre_mode & 0o777, post_mode & 0o777))
    patch = b"".join(patches)
    return OwnedDelta(
        patch=patch,
        patch_digest=hashlib.sha256(patch).hexdigest(),
        implementation_paths=names,
        preimages=dict(preimages),
        postimages=postimages,
        types=types,
        modes=modes,
        control_owned=dict(control_owned),
        rename_pairs=tuple(rename_pairs),
    )


def persist_owned_delta(spec_path: Path, delta: OwnedDelta) -> Path:
    sidecar = Path(str(spec_path) + ".owned-delta")
    payload = {
        "patch_digest": delta.patch_digest,
        "implementation_paths": [
            name.decode("utf-8", "surrogateescape") for name in delta.implementation_paths
        ],
        "types": {name.decode("utf-8", "surrogateescape"): kind for name, kind in delta.types.items()},
        "patch_b64": base64.b64encode(delta.patch).decode("ascii"),
        "preimages_b64": {
            name.decode("utf-8", "surrogateescape"): base64.b64encode(data).decode("ascii")
            for name, data in delta.preimages.items()
        },
        "postimages_b64": {
            name.decode("utf-8", "surrogateescape"): base64.b64encode(data).decode("ascii")
            for name, data in delta.postimages.items()
        },
        "control_owned_b64": {
            name.decode("utf-8", "surrogateescape"): base64.b64encode(data).decode("ascii")
            for name, data in delta.control_owned.items()
        },
        "modes": {
            name.decode("utf-8", "surrogateescape"): [pre_mode, post_mode]
            for name, (pre_mode, post_mode) in delta.modes.items()
        },
        "rename_pairs": [
            [
                source.decode("utf-8", "surrogateescape"),
                dest.decode("utf-8", "surrogateescape"),
            ]
            for source, dest in delta.rename_pairs
        ],
    }
    sidecar.write_bytes(json.dumps(payload, sort_keys=True, separators=(",", ":")).encode("utf-8"))
    return sidecar


def load_owned_delta(spec_path: Path) -> OwnedDelta:
    sidecar = Path(str(spec_path) + ".owned-delta")
    if not sidecar.is_file():
        raise OwnershipHalt("missing owned_delta evidence")
    try:
        payload = json.loads(sidecar.read_bytes().decode("utf-8"))
        decode = lambda mapping: {
            key.encode("utf-8", "surrogateescape"): base64.b64decode(value)
            for key, value in mapping.items()
        }
        patch = base64.b64decode(payload["patch_b64"])
        digest = hashlib.sha256(patch).hexdigest()
        if digest != payload.get("patch_digest"):
            raise OwnershipHalt("owned_delta digest mismatch")
        if "modes" not in payload or "rename_pairs" not in payload:
            raise OwnershipHalt("incomplete owned_delta evidence")
        return OwnedDelta(
            patch=patch,
            patch_digest=payload["patch_digest"],
            implementation_paths=tuple(
                name.encode("utf-8", "surrogateescape") for name in payload["implementation_paths"]
            ),
            preimages=decode(payload["preimages_b64"]),
            postimages=decode(payload["postimages_b64"]),
            types={
                name.encode("utf-8", "surrogateescape"): kind for name, kind in payload["types"].items()
            },
            modes={
                name.encode("utf-8", "surrogateescape"): (int(pre_mode), int(post_mode))
                for name, (pre_mode, post_mode) in payload["modes"].items()
            },
            control_owned=decode(payload["control_owned_b64"]),
            rename_pairs=tuple(
                (
                    source.encode("utf-8", "surrogateescape"),
                    dest.encode("utf-8", "surrogateescape"),
                )
                for source, dest in payload["rename_pairs"]
            ),
        )
    except OwnershipHalt:
        raise
    except (ValueError, KeyError, TypeError, UnicodeError) as error:
        raise OwnershipHalt("incomplete owned_delta evidence") from error


def snapshot_bytes(repo: Path) -> dict[str, bytes]:
    payload = {
        b"__head__": run_git(repo, "rev-parse", "HEAD").stdout,
        b"__index__": git_path(repo, "index").read_bytes(),
    }
    for name, _record in walk_raw(repo).items():
        path = join_raw(repo, name)
        if path.is_dir() and not path.is_symlink():
            continue
        payload[name] = file_bytes(repo, name) if (path.exists() or path.is_symlink()) else b""
        payload[b"__mode__:" + name] = str(os.lstat(path).st_mode).encode("ascii")
    return payload


def apply_reverse_check(repo: Path, patch: bytes) -> int:
    return run_git(repo, "apply", "--check", "--reverse", check=False, stdin=patch).returncode


def restore_snapshot(repo: Path, before: dict[bytes, bytes]) -> None:
    git_path(repo, "index").write_bytes(before[b"__index__"])
    current = walk_raw(repo)
    before_names = {name for name in before if not name.startswith(b"__")}
    for name in current:
        path = join_raw(repo, name)
        if name in before_names or (path.is_dir() and not path.is_symlink()):
            continue
        if path.is_symlink() or path.is_file():
            path.unlink()
    for name in before_names:
        write_raw(repo, name, before[name], int(before[b"__mode__:" + name]) & 0o777)


def reverse_owned(
    session: Session,
    repo: Path,
    *,
    isolated: bool = False,
    lease: Optional[WorktreeLease] = None,
) -> None:
    if session.owned is None:
        raise OwnershipHalt("incomplete ownership snapshot")
    revalidate(session, repo, ignore_owned_paths=True)
    if isolated:
        if lease is None or not lease.names(repo):
            raise OwnershipHalt("exclusive isolated ownership unproven")
        before = snapshot_bytes(repo)
        try:
            run_git(repo, "read-tree", session.baseline_revision)
            run_git(repo, "checkout-index", "-a", "-f")
            for name, preimage in session.owned.preimages.items():
                write_raw(repo, name, preimage)
            for name in session.owned.implementation_paths:
                if name not in session.owned.preimages and join_raw(repo, name).is_file():
                    join_raw(repo, name).unlink()
        except Exception as error:
            restore_snapshot(repo, before)
            if isinstance(error, OwnershipHalt):
                raise
            raise OwnershipHalt("exclusive isolated restore failed") from error
        refresh_expected(session, repo)
        return
    before = snapshot_bytes(repo)
    if apply_reverse_check(repo, session.owned.patch) != 0:
        raise OwnershipHalt("reverse preflight")
    applied = run_git(repo, "apply", "--reverse", check=False, stdin=session.owned.patch)
    if applied.returncode != 0:
        restore_snapshot(repo, before)
        raise OwnershipHalt("reverse preflight")
    for name, preimage in session.owned.preimages.items():
        if file_bytes(repo, name) != preimage:
            restore_snapshot(repo, before)
            raise OwnershipHalt("reverse preflight")
    refresh_expected(session, repo)


def apply_repair(session: Session, repo: Path, name: bytes, new_bytes: bytes) -> None:
    if session.owned is None:
        raise OwnershipHalt("incomplete ownership snapshot")
    revalidate(session, repo, ignore_owned_paths=True)
    if name not in session.owned.postimages:
        raise OwnershipHalt("stale repair preimage")
    current = file_bytes(repo, name)
    expected_current = session.owned.postimages[name]
    if current != expected_current:
        raise OwnershipHalt("stale repair preimage")
    write_raw(repo, name, new_bytes)
    session.owned.postimages[name] = new_bytes
    session.owned = build_owned_delta(
        repo,
        session.owned.implementation_paths,
        session.owned.preimages,
        session.owned.control_owned,
        session.owned.rename_pairs,
    )
    persist_owned_delta(join_raw(repo, session.control_names[0]), session.owned)
    refresh_expected(session, repo)


def increment_review_loop(session: Session, repo: Path, spec_rel: bytes) -> None:
    revalidate(session, repo)
    path = join_raw(repo, spec_rel)
    text = path.read_text(encoding="utf-8")
    match = re.search(r"review_loop_iteration:\s*(\d+)", text)
    current = int(match.group(1)) if match else 0
    updated = (
        re.sub(r"review_loop_iteration:\s*\d+", f"review_loop_iteration: {current + 1}", text, count=1)
        if match
        else text.replace("---\n", f"---\nreview_loop_iteration: {current + 1}\n", 1)
    )
    if updated == text:
        raise OwnershipHalt("incomplete ownership snapshot")
    path.write_text(updated, encoding="utf-8")
    refresh_expected(session, repo)


def append_triage_log(spec_text: str, entry: str, *, outcome_known: bool, already_appended: bool) -> str:
    if not outcome_known:
        raise OwnershipHalt("triage log before branch outcome")
    if already_appended:
        raise OwnershipHalt("duplicate triage log append")
    return spec_text + entry


def git_cacheinfo_mode(kind: str, st_mode: int) -> str:
    if kind == "symlink":
        return "120000"
    if kind == "delete":
        raise OwnershipHalt("incomplete ownership snapshot")
    if st_mode & 0o111:
        return "100755"
    return "100644"


def stage_blob(
    repo: Path,
    extra: dict[str, str],
    name: bytes,
    content: bytes,
    kind: str,
    st_mode: int,
) -> None:
    blob = run_git(repo, "hash-object", "-w", "--stdin", stdin=content).stdout.strip()
    decoded = name.decode("utf-8", "surrogateescape")
    run_git(
        repo,
        "update-index",
        "--add",
        "--cacheinfo",
        f"{git_cacheinfo_mode(kind, st_mode)},{blob.decode('ascii')},{decoded}",
        extra_env=extra,
    )


def sidecar_name(spec_rel: bytes) -> bytes:
    return spec_rel + b".owned-delta"


def sync_shared_index_from_head(repo: Path, names: Iterable[bytes]) -> None:
    for name in names:
        decoded = name.decode("utf-8", "surrogateescape")
        listed = run_git(repo, "ls-tree", "-z", "HEAD", "--", decoded)
        if not listed.stdout:
            run_git(repo, "update-index", "--force-remove", decoded, check=False)
            continue
        record = listed.stdout.split(b"\0", 1)[0]
        meta, _path = record.split(b"\t", 1)
        mode, _kind, sha = meta.split()
        run_git(
            repo,
            "update-index",
            "--add",
            "--cacheinfo",
            f"{mode.decode('ascii')},{sha.decode('ascii')},{decoded}",
        )


def commit_owned(session: Session, repo: Path, spec_rel: bytes) -> str:
    if session.owned is None:
        raise OwnershipHalt("incomplete ownership snapshot")
    revalidate(session, repo)
    real_index = git_path(repo, "index")
    preserved_index = real_index.read_bytes()
    sidecar_rel = sidecar_name(spec_rel)
    persist_owned_delta(join_raw(repo, spec_rel), session.owned)
    sidecar_bytes = join_raw(repo, sidecar_rel).read_bytes()
    with tempfile.TemporaryDirectory() as temporary:
        private = Path(temporary) / "index"
        extra = {"GIT_INDEX_FILE": str(private)}
        run_git(repo, "read-tree", "HEAD", extra_env=extra)
        if session.owned.patch:
            applied = run_git(
                repo,
                "apply",
                "--cached",
                check=False,
                stdin=session.owned.patch,
                extra_env=extra,
            )
            if applied.returncode != 0:
                for name, postimage in session.owned.postimages.items():
                    kind = session.owned.types.get(name, "file")
                    post_mode = session.owned.modes.get(name, (0, 0o100644))[1]
                    stage_blob(repo, extra, name, postimage, kind, post_mode)
                for name in session.owned.implementation_paths:
                    if session.owned.types.get(name) == "delete":
                        decoded = name.decode("utf-8", "surrogateescape")
                        run_git(repo, "update-index", "--force-remove", decoded, extra_env=extra)
        for name, content in session.owned.control_owned.items():
            kind = session.owned.types.get(name, "file")
            post_mode = session.owned.modes.get(name, (0, 0o100644))[1] or 0o100644
            if kind == "delete":
                continue
            stage_blob(repo, extra, name, content, kind, post_mode)
        stage_blob(repo, extra, sidecar_rel, sidecar_bytes, "file", 0o100644)
        tree = run_git(repo, "write-tree", extra_env=extra).stdout.strip().decode("ascii")
        commit = run_git(
            repo,
            "commit-tree",
            tree,
            "-p",
            "HEAD",
            "-m",
            "owned implementation",
            extra_env=extra,
        ).stdout.strip().decode("ascii")
    run_git(repo, "update-ref", "HEAD", commit)
    real_index.write_bytes(preserved_index)
    owned_names = set(session.owned.implementation_paths) | set(session.owned.control_owned)
    owned_names.add(sidecar_rel)
    sync_shared_index_from_head(repo, owned_names)
    refresh_expected(session, repo)
    return commit


def workspace_is_clean(repo: Path) -> bool:
    porcelain = run_git(
        repo,
        "status",
        "--porcelain=v2",
        "-z",
        "--untracked-files=all",
        "--no-renames",
    ).stdout
    return porcelain == b""


def first_pass_session(repo: Path, spec_rel: bytes, extra_control: Iterable[bytes] = ()) -> Session:
    if not workspace_is_clean(repo):
        raise OwnershipHalt("dirty first-pass entry")
    control = (spec_rel, *tuple(extra_control))
    expected = capture_checkpoint(repo, control)
    head = expected.head
    return Session(
        baseline_revision=head,
        expected=expected,
        owned=None,
        live=True,
        control_names=control,
    )


def seed_repo(root: Path) -> tuple[Path, bytes]:
    repo = init_repo(root / "repo")
    (repo / "src").mkdir()
    (repo / "src" / "app.txt").write_bytes(b"base line\nkeep me\n")
    (repo / "src" / "other.txt").write_bytes(b"unrelated\n")
    spec = b"spec.md"
    write_raw(repo, spec, b"---\nstatus: ready-for-dev\nreview_loop_iteration: 2\n---\nplanned body\n")
    run_git(repo, "add", "src/app.txt", "src/other.txt", "spec.md")
    run_git(repo, "commit", "-m", "seed")
    return repo, spec


def implement_owned(repo: Path, name: bytes, new_bytes: bytes) -> None:
    write_raw(repo, name, new_bytes)


def instruction_texts() -> dict[str, list[str]]:
    texts: dict[str, list[str]] = {WORKFLOW_NAME: [], STEP01_NAME: [], STEP03_NAME: [], STEP04_NAME: []}
    for entry in ENTRY_POINTS:
        skill = entry / SKILL_RELATIVE
        for name in texts:
            texts[name].append((skill / name).read_text(encoding="utf-8"))
    return texts


def assert_identical_copies(relative: Path) -> bytes:
    copies = [entry / SKILL_RELATIVE / relative for entry in ENTRY_POINTS]
    missing = [str(path) for path in copies if not path.is_file()]
    assert not missing, f"missing synchronized copy: {missing}"
    payload = copies[0].read_bytes()
    assert all(path.read_bytes() == payload for path in copies), (
        f"copies drifted: {[str(path) for path in copies]}"
    )
    return payload


def headings_in_order(text: str, headings: Iterable[str]) -> None:
    positions = [text.index(heading) for heading in headings]
    assert positions == sorted(positions), headings


def line_allows_forbidden_mention(line: str) -> bool:
    lowered = line.lower()
    return any(token in lowered for token in ("never", "do not", "forbidden", "without"))


def test_matrix_clean_owned_run() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        repo, spec = seed_repo(Path(temporary))
        session = first_pass_session(repo, spec)
        preimage = file_bytes(repo, b"src/app.txt")
        implement_owned(repo, b"src/app.txt", b"base line\nowned change\nkeep me\n")
        session.owned = build_owned_delta(
            repo,
            [b"src/app.txt"],
            {b"src/app.txt": preimage},
            {spec: join_raw(repo, spec).read_bytes()},
        )
        persist_owned_delta(join_raw(repo, spec), session.owned)
        refresh_expected(session, repo)
        revalidate(session, repo)
        write_raw(repo, spec, b"---\nstatus: done\nreview_loop_iteration: 2\n---\nplanned body\n")
        session.owned.control_owned[spec] = join_raw(repo, spec).read_bytes()
        refresh_expected(session, repo)
        commit_owned(session, repo, spec)
        assert b"owned change" in run_git(repo, "show", "HEAD:src/app.txt").stdout
        assert b"status: done" in run_git(repo, "show", "HEAD:spec.md").stdout
        assert run_git(repo, "cat-file", "-e", "HEAD:spec.md.owned-delta", check=False).returncode == 0
        assert session.baseline_revision != session.expected.head


def test_matrix_head_or_index_drift() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        repo, spec = seed_repo(Path(temporary))
        session = first_pass_session(repo, spec)
        run_git(repo, "commit", "--allow-empty", "-m", "external")
        try:
            revalidate(session, repo)
            raise AssertionError("head drift must halt")
        except OwnershipHalt as error:
            assert error.differing_class == "head"
        session = first_pass_session(repo, spec)
        run_git(repo, "update-index", "--chmod=+x", "src/other.txt")
        try:
            revalidate(session, repo)
            raise AssertionError("index drift must halt")
        except OwnershipHalt as error:
            assert error.differing_class == "index"
        assert run_git(repo, "rev-parse", "HEAD").stdout.strip().decode("ascii") == session.expected.head


def test_matrix_worktree_or_path_set_drift() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        repo, spec = seed_repo(Path(temporary))
        session = first_pass_session(repo, spec)
        write_raw(repo, b"src/other.txt", b"unrelated\nmutated\n")
        try:
            revalidate(session, repo)
            raise AssertionError("worktree drift must halt")
        except OwnershipHalt as error:
            assert error.differing_class == "worktree"
        write_raw(repo, b"src/other.txt", b"unrelated\n")
        session = first_pass_session(repo, spec)
        write_raw(repo, b"src/new.txt", b"added\n")
        try:
            revalidate(session, repo)
            raise AssertionError("path-set drift must halt")
        except OwnershipHalt as error:
            assert error.differing_class == "path-set"
        write_raw(repo, b"src/other.txt", b"unrelated\n")
        join_raw(repo, b"src/new.txt").unlink()
        session = first_pass_session(repo, spec)
        preimage = file_bytes(repo, b"src/app.txt")
        implement_owned(repo, b"src/app.txt", b"base line\nowned\nkeep me\n")
        session.owned = build_owned_delta(
            repo, [b"src/app.txt"], {b"src/app.txt": preimage}, {spec: join_raw(repo, spec).read_bytes()}
        )
        refresh_expected(session, repo)
        implement_owned(repo, b"src/app.txt", b"base line\nowned\nextra hunk\nkeep me\n")
        try:
            revalidate(session, repo)
            raise AssertionError("owned-hunk drift must halt")
        except OwnershipHalt as error:
            assert error.differing_class == "owned-hunk"


def test_matrix_authorized_repair() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        repo, spec = seed_repo(Path(temporary))
        session = first_pass_session(repo, spec)
        preimage = file_bytes(repo, b"src/app.txt")
        implement_owned(repo, b"src/app.txt", b"base line\nowned\nkeep me\n")
        session.owned = build_owned_delta(
            repo, [b"src/app.txt"], {b"src/app.txt": preimage}, {spec: join_raw(repo, spec).read_bytes()}
        )
        refresh_expected(session, repo)
        before_digest = session.owned.patch_digest
        apply_repair(session, repo, b"src/app.txt", b"base line\nrepaired\nkeep me\n")
        assert file_bytes(repo, b"src/app.txt") == b"base line\nrepaired\nkeep me\n"
        loaded = load_owned_delta(join_raw(repo, spec))
        assert loaded.patch_digest != before_digest
        assert loaded.patch_digest == session.owned.patch_digest
        assert b"repaired" in loaded.patch
        revalidate(session, repo)


def test_matrix_shared_repair_reversal() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        repo, spec = seed_repo(Path(temporary))
        session = first_pass_session(repo, spec)
        preimage = file_bytes(repo, b"src/app.txt")
        control_before = join_raw(repo, spec).read_bytes()
        unrelated_before = file_bytes(repo, b"src/other.txt")
        implement_owned(repo, b"src/app.txt", b"base line\nowned\nkeep me\n")
        session.owned = build_owned_delta(
            repo, [b"src/app.txt"], {b"src/app.txt": preimage}, {spec: control_before}
        )
        refresh_expected(session, repo)
        reverse_owned(session, repo)
        assert file_bytes(repo, b"src/app.txt") == preimage
        assert join_raw(repo, spec).read_bytes() == control_before
        assert file_bytes(repo, b"src/other.txt") == unrelated_before
        os.chmod(join_raw(repo, b"src/app.txt"), 0o755)
        session.owned = build_owned_delta(
            repo, [b"src/app.txt"], {b"src/app.txt": preimage}, {spec: control_before}
        )
        refresh_expected(session, repo)
        pre_mode = session.owned.modes[b"src/app.txt"][0]
        reverse_owned(session, repo)
        assert (os.lstat(join_raw(repo, b"src/app.txt")).st_mode & 0o777) == (pre_mode & 0o777)
        assert file_bytes(repo, b"src/app.txt") == preimage


def test_matrix_exclusive_isolated_reversal() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        root = Path(temporary)
        repo, spec = seed_repo(root)
        evidence = root / "evidence-outside"
        evidence.write_bytes(b"keep-outside\n")
        session = first_pass_session(repo, spec)
        preimage = file_bytes(repo, b"src/app.txt")
        other_preimage = file_bytes(repo, b"src/other.txt")
        implement_owned(repo, b"src/app.txt", b"base line\nowned\nkeep me\n")
        write_raw(repo, b"src/other.txt", b"unrelated\ndirty-unowned\n")
        session.owned = build_owned_delta(
            repo, [b"src/app.txt"], {b"src/app.txt": preimage}, {spec: join_raw(repo, spec).read_bytes()}
        )
        refresh_expected(session, repo)
        try:
            reverse_owned(session, repo, isolated=True)
            raise AssertionError("list-only exclusivity must halt")
        except OwnershipHalt as error:
            assert error.differing_class == "exclusive isolated ownership unproven"
        assert file_bytes(repo, b"src/app.txt") == b"base line\nowned\nkeep me\n"
        assert file_bytes(repo, b"src/other.txt") == b"unrelated\ndirty-unowned\n"
        lease = WorktreeLease(repo).acquire()
        try:
            reverse_owned(session, repo, isolated=True, lease=lease)
        finally:
            lease.release()
        assert file_bytes(repo, b"src/app.txt") == preimage
        assert file_bytes(repo, b"src/other.txt") == other_preimage
        assert evidence.read_bytes() == b"keep-outside\n"
        dropped = WorktreeLease(repo)
        dropped.acquire()
        dropped.release()
        implement_owned(repo, b"src/app.txt", b"base line\nowned-again\nkeep me\n")
        write_raw(repo, b"src/other.txt", b"unrelated\ndirty-unowned\n")
        session.owned = build_owned_delta(
            repo, [b"src/app.txt"], {b"src/app.txt": preimage}, {spec: join_raw(repo, spec).read_bytes()}
        )
        refresh_expected(session, repo)
        try:
            reverse_owned(session, repo, isolated=True, lease=dropped)
            raise AssertionError("dropped lease must halt")
        except OwnershipHalt as error:
            assert error.differing_class == "exclusive isolated ownership unproven"


def test_matrix_interrupted_resume() -> None:
    try:
        enter_workflow("in-progress", session=None)
        raise AssertionError("resume without live session must halt")
    except OwnershipHalt as error:
        assert error.differing_class == "missing live ownership session"
    try:
        enter_workflow("in-review", session=Session("x", None, None, False, ()))
        raise AssertionError("non-live session must halt")
    except OwnershipHalt as error:
        assert error.differing_class == "missing live ownership session"


def test_matrix_dirty_follow_up_entry() -> None:
    try:
        enter_workflow("done", session=None, followup=True, dirty_classes=("worktree",))
        raise AssertionError("dirty follow-up must halt")
    except OwnershipHalt as error:
        assert error.differing_class == "dirty follow-up entry"
    try:
        enter_workflow("done", session=None, followup=True, orchestrator_bookkeeping=True)
        raise AssertionError("control bookkeeping follow-up must halt")
    except OwnershipHalt as error:
        assert error.differing_class == "dirty follow-up entry"


def test_matrix_orchestrator_re_drive_handoff() -> None:
    try:
        enter_workflow("ready-for-dev", session=None, orchestrator_bookkeeping=True)
        raise AssertionError("re-drive bookkeeping must halt")
    except OwnershipHalt as error:
        assert error.differing_class == "orchestrator re-drive handoff"
    try:
        enter_workflow("ready-for-dev", session=None, dirty_classes=("worktree",))
        raise AssertionError("dirty first-pass must halt")
    except OwnershipHalt as error:
        assert error.differing_class == "dirty first-pass entry"


def test_matrix_done_follow_up_review() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        repo, spec = seed_repo(Path(temporary))
        session = first_pass_session(repo, spec)
        historical = session.baseline_revision
        preimage = file_bytes(repo, b"src/app.txt")
        implement_owned(repo, b"src/app.txt", b"base line\nowned\nkeep me\n")
        session.owned = build_owned_delta(
            repo, [b"src/app.txt"], {b"src/app.txt": preimage}, {spec: join_raw(repo, spec).read_bytes()}
        )
        persist_owned_delta(join_raw(repo, spec), session.owned)
        refresh_expected(session, repo)
        write_raw(repo, spec, b"---\nstatus: done\nreview_loop_iteration: 2\n---\nplanned body\n")
        session.owned.control_owned[spec] = join_raw(repo, spec).read_bytes()
        refresh_expected(session, repo)
        commit_owned(session, repo, spec)
        assert run_git(repo, "status", "--porcelain=v2", "-z", "--untracked-files=all").stdout == b""
        write_raw(repo, b"src/other.txt", b"unrelated\nlater commit\n")
        run_git(repo, "add", "src/other.txt")
        run_git(repo, "commit", "-m", "unowned later")
        assert enter_workflow("done", session=None, followup=True) == "fresh-followup"
        followup_text = join_raw(repo, spec).read_bytes().replace(
            b"review_loop_iteration: 2", b"review_loop_iteration: 0"
        )
        write_raw(repo, spec, followup_text)
        assert b"review_loop_iteration: 0" in join_raw(repo, spec).read_bytes()
        fresh = capture_checkpoint(repo, (spec,))
        loaded = load_owned_delta(join_raw(repo, spec))
        assert loaded.patch_digest == session.owned.patch_digest
        assert loaded.modes
        assert loaded.rename_pairs == session.owned.rename_pairs
        assert b"later commit" not in loaded.patch
        assert historical == session.baseline_revision
        assert fresh.head != historical


def test_instruction_ordering() -> None:
    texts = instruction_texts()
    for copy in texts[STEP03_NAME]:
        headings_in_order(copy, STEP03_HEADINGS)
        assert copy.index("### Ownership session") < copy.index("### Mark in-progress")
        assert copy.index("### Post-handoff ownership check") < copy.index("### Capture owned_delta")
    for copy in texts[STEP04_NAME]:
        headings_in_order(copy, STEP04_HEADINGS)
        assert copy.index("### Ownership gate") < copy.index("### Mark in-review")
        assert copy.index("### Ownership gate") < copy.index("### Stage the owned diff")
        assert copy.index("### Restore or repair") < copy.index("### Finalize")
        assert "exactly one" in copy and "branch outcome" in copy
    for copy in texts[WORKFLOW_NAME]:
        assert "expected_workspace" in copy
        assert "owned_delta" in copy
        assert "live_ownership_session" in copy
        assert "Ownership-drift no-mutation path" in copy


def test_forbidden_broad_operations() -> None:
    texts = instruction_texts()
    for name in (WORKFLOW_NAME, STEP03_NAME, STEP04_NAME):
        for copy in texts[name]:
            for line in copy.splitlines():
                if any(token in line for token in FORBIDDEN_IMPERATIVE):
                    assert line_allows_forbidden_mention(line), line
            assert "Never" in copy or "Do not" in copy
    for copy in texts[STEP01_NAME]:
        assert "set `review_loop_iteration` to `0`" not in copy
    for copy in texts[STEP04_NAME]:
        assert "reset `review_loop_iteration` to `0`" in copy
        assert "declared_mutation" in copy


def test_control_owned_spec_included_in_finalization() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        repo, spec = seed_repo(Path(temporary))
        session = first_pass_session(repo, spec)
        planned = join_raw(repo, spec).read_bytes()
        preimage = file_bytes(repo, b"src/app.txt")
        implement_owned(repo, b"src/app.txt", b"base line\nowned\nkeep me\n")
        session.owned = build_owned_delta(
            repo, [b"src/app.txt"], {b"src/app.txt": preimage}, {spec: planned}
        )
        refresh_expected(session, repo)
        commit_owned(session, repo, spec)
        assert planned == run_git(repo, "show", "HEAD:spec.md").stdout


def test_persisted_hashed_patch_not_file_list() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        repo, spec = seed_repo(Path(temporary))
        session = first_pass_session(repo, spec)
        preimage = file_bytes(repo, b"src/app.txt")
        implement_owned(repo, b"src/app.txt", b"base line\nowned\nkeep me\n")
        session.owned = build_owned_delta(
            repo, [b"src/app.txt"], {b"src/app.txt": preimage}, {spec: join_raw(repo, spec).read_bytes()}
        )
        sidecar = persist_owned_delta(join_raw(repo, spec), session.owned)
        payload = json.loads(sidecar.read_bytes().decode("utf-8"))
        assert payload["patch_digest"] == session.owned.patch_digest
        assert "file_list" not in payload
        assert "modes" in payload and "rename_pairs" in payload
        loaded = load_owned_delta(join_raw(repo, spec))
        assert loaded.modes == session.owned.modes
        assert loaded.rename_pairs == session.owned.rename_pairs
        payload["patch_digest"] = "0" * 64
        sidecar.write_bytes(json.dumps(payload, sort_keys=True, separators=(",", ":")).encode("utf-8"))
        try:
            load_owned_delta(join_raw(repo, spec))
            raise AssertionError("tampered digest must halt")
        except OwnershipHalt as error:
            assert error.differing_class == "owned_delta digest mismatch"
        payload.pop("modes")
        payload["patch_digest"] = session.owned.patch_digest
        sidecar.write_bytes(json.dumps(payload, sort_keys=True, separators=(",", ":")).encode("utf-8"))
        try:
            load_owned_delta(join_raw(repo, spec))
            raise AssertionError("mode-stripped sidecar must halt")
        except OwnershipHalt as error:
            assert error.differing_class == "incomplete owned_delta evidence"
        sidecar.write_bytes(b"{not-json")
        try:
            load_owned_delta(join_raw(repo, spec))
            raise AssertionError("non-JSON sidecar must halt")
        except OwnershipHalt as error:
            assert error.differing_class == "incomplete owned_delta evidence"
        sidecar.write_bytes(b'{"patch_b64":')
        try:
            load_owned_delta(join_raw(repo, spec))
            raise AssertionError("truncated sidecar must halt")
        except OwnershipHalt as error:
            assert error.differing_class == "incomplete owned_delta evidence"
        sidecar.write_bytes(b"{}")
        try:
            load_owned_delta(join_raw(repo, spec))
            raise AssertionError("missing-key sidecar must halt")
        except OwnershipHalt as error:
            assert error.differing_class == "incomplete owned_delta evidence"
        sidecar.unlink()
        try:
            load_owned_delta(join_raw(repo, spec))
            raise AssertionError("missing sidecar must halt")
        except OwnershipHalt as error:
            assert error.differing_class == "missing owned_delta evidence"


def test_bounded_stable_double_capture() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        repo, spec = seed_repo(Path(temporary))

        def mutate() -> None:
            write_raw(repo, b"src/other.txt", b"unrelated\nraced\n")

        try:
            capture_checkpoint(repo, (spec,), inject=mutate)
            raise AssertionError("raced double capture must halt")
        except OwnershipHalt as error:
            assert error.differing_class == "raced path observation"


def test_skip_worktree_cannot_hide_bytes() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        repo, spec = seed_repo(Path(temporary))
        run_git(repo, "update-index", "--skip-worktree", "src/other.txt")
        session = first_pass_session(repo, spec)
        write_raw(repo, b"src/other.txt", b"hidden\n")
        try:
            revalidate(session, repo)
            raise AssertionError("skip-worktree bytes must be visible")
        except OwnershipHalt as error:
            assert error.differing_class == "worktree"


def test_ignored_control_path_and_executable_drift() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        repo, spec = seed_repo(Path(temporary))
        write_raw(repo, b".gitignore", b"ledger.md\n")
        run_git(repo, "add", ".gitignore")
        run_git(repo, "commit", "-m", "ignore ledger")
        write_raw(repo, b"ledger.md", b"ledger-one\n")
        session = first_pass_session(repo, spec, extra_control=(b"ledger.md",))
        write_raw(repo, b"ledger.md", b"ledger-two\n")
        try:
            revalidate(session, repo)
            raise AssertionError("ignored control bytes must drift")
        except OwnershipHalt as error:
            assert error.differing_class == "control-path"
        write_raw(repo, b"ledger.md", b"ledger-one\n")
        session = first_pass_session(repo, spec, extra_control=(b"ledger.md",))
        os.chmod(join_raw(repo, b"ledger.md"), 0o755)
        try:
            revalidate(session, repo)
            raise AssertionError("executable control mode must drift")
        except OwnershipHalt as error:
            assert error.differing_class == "control-path"


def test_preexisting_same_file_hunk_preserved() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        repo, spec = seed_repo(Path(temporary))
        write_raw(repo, b"src/app.txt", b"base line\npre-existing\nkeep me\n")
        expected = capture_checkpoint(repo, (spec,))
        session = Session(expected.head, expected, None, True, (spec,))
        preimage = file_bytes(repo, b"src/app.txt")
        implement_owned(repo, b"src/app.txt", b"base line\npre-existing\nowned\nkeep me\n")
        session.owned = build_owned_delta(
            repo, [b"src/app.txt"], {b"src/app.txt": preimage}, {spec: join_raw(repo, spec).read_bytes()}
        )
        assert b"pre-existing" in session.owned.preimages[b"src/app.txt"]
        assert session.owned.preimages[b"src/app.txt"] != b"base line\nkeep me\n"
        reverse_owned(session, repo)
        assert file_bytes(repo, b"src/app.txt") == b"base line\npre-existing\nkeep me\n"


def test_private_index_commit_preserves_unrelated() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        repo, spec = seed_repo(Path(temporary))
        write_raw(repo, b"src/other.txt", b"unrelated\nstaged\n")
        run_git(repo, "add", "src/other.txt")
        staged = run_git(repo, "diff", "--cached", "--", "src/other.txt").stdout
        worktree_extra = b"worktree-only\n"
        write_raw(repo, b"loose.txt", worktree_extra)
        try:
            first_pass_session(repo, spec)
            raise AssertionError("dirty first-pass must halt")
        except OwnershipHalt as error:
            assert error.differing_class == "dirty first-pass entry"
        expected = capture_checkpoint(repo, (spec,))
        session = Session(expected.head, expected, None, True, (spec,))
        preimage = file_bytes(repo, b"src/app.txt")
        implement_owned(repo, b"src/app.txt", b"base line\nowned\nkeep me\n")
        session.owned = build_owned_delta(
            repo, [b"src/app.txt"], {b"src/app.txt": preimage}, {spec: join_raw(repo, spec).read_bytes()}
        )
        refresh_expected(session, repo)
        commit_owned(session, repo, spec)
        assert staged == run_git(repo, "diff", "--cached", "--", "src/other.txt").stdout
        assert file_bytes(repo, b"loose.txt") == worktree_extra
        assert b"owned" in run_git(repo, "show", "HEAD:src/app.txt").stdout
        assert b"staged" not in run_git(repo, "show", "HEAD:src/other.txt").stdout


def test_stale_repair_preimage_is_a_no_write() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        repo, spec = seed_repo(Path(temporary))
        session = first_pass_session(repo, spec)
        preimage = file_bytes(repo, b"src/app.txt")
        implement_owned(repo, b"src/app.txt", b"base line\nowned\nkeep me\n")
        session.owned = build_owned_delta(
            repo, [b"src/app.txt"], {b"src/app.txt": preimage}, {spec: join_raw(repo, spec).read_bytes()}
        )
        refresh_expected(session, repo)
        expected = session.expected
        implement_owned(repo, b"src/app.txt", b"base line\nstale\nkeep me\n")
        before = file_bytes(repo, b"src/app.txt")
        try:
            apply_repair(session, repo, b"src/app.txt", b"base line\nrepaired\nkeep me\n")
            raise AssertionError("stale preimage must halt")
        except OwnershipHalt as error:
            assert error.differing_class == "stale repair preimage"
        assert file_bytes(repo, b"src/app.txt") == before
        assert session.expected == expected


def test_reverse_preflight_failure_is_atomic() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        repo, spec = seed_repo(Path(temporary))
        session = first_pass_session(repo, spec)
        preimage = file_bytes(repo, b"src/app.txt")
        implement_owned(repo, b"src/app.txt", b"base line\nowned\nkeep me\n")
        session.owned = build_owned_delta(
            repo, [b"src/app.txt"], {b"src/app.txt": preimage}, {spec: join_raw(repo, spec).read_bytes()}
        )
        refresh_expected(session, repo)
        implement_owned(repo, b"src/app.txt", b"base line\nowned\noverlap\nkeep me\n")
        before = snapshot_bytes(repo)
        try:
            reverse_owned(session, repo)
            raise AssertionError("overlap must fail reverse preflight")
        except OwnershipHalt as error:
            assert error.differing_class == "reverse preflight"
        assert snapshot_bytes(repo) == before


def test_uninitialized_and_nested_gitlinks() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        root = Path(temporary)
        nested_src = init_repo(root / "nested-src")
        write_raw(nested_src, b"n.txt", b"nested\n")
        run_git(nested_src, "add", "n.txt")
        run_git(nested_src, "commit", "-m", "nested")
        child_src = init_repo(root / "child-src")
        write_raw(child_src, b"c.txt", b"child\n")
        run_git(child_src, "add", "c.txt")
        run_git(child_src, "commit", "-m", "child")
        run_git(
            child_src,
            "-c",
            "protocol.file.allow=always",
            "submodule",
            "add",
            str(nested_src),
            "nested",
        )
        run_git(child_src, "commit", "-m", "child nested")
        repo, spec = seed_repo(root)
        run_git(
            repo,
            "-c",
            "protocol.file.allow=always",
            "submodule",
            "add",
            str(child_src),
            "child",
        )
        run_git(repo, "commit", "-m", "add child")
        session = first_pass_session(repo, spec)
        gitlinks = session.expected.gitlink_map()
        assert gitlinks[b"child"][0] == "initialized"
        nested_state = dict(gitlinks[b"child"][3])
        assert nested_state[b"nested"][0] == "uninitialized"
        run_git(
            repo / "child",
            "-c",
            "protocol.file.allow=always",
            "submodule",
            "update",
            "--init",
            "nested",
        )
        initialized = capture_checkpoint(repo, (spec,))
        nested_state = dict(initialized.gitlink_map()[b"child"][3])
        assert nested_state[b"nested"][0] == "initialized"
        assert len(initialized.gitlink_map()[b"child"]) == 5
        nested_paths = dict(initialized.gitlink_map()[b"child"][4])
        assert b"c.txt" in nested_paths
        write_raw(repo / "child" / "nested", b"dirty.txt", b"unstaged nested\n")
        drifted = capture_checkpoint(repo, (spec,))
        assert drifted.gitlink_map() != initialized.gitlink_map()
        assert b"nested/dirty.txt" in dict(drifted.gitlink_map()[b"child"][4])
        session = Session(initialized.head, initialized, None, True, (spec,))
        try:
            revalidate(session, repo)
            raise AssertionError("nested worktree file must be ownership drift")
        except OwnershipHalt as error:
            assert error.differing_class in {"gitlink", "path-set", "worktree", "untracked"}
        write_raw(repo / "child" / "nested", b"dirty.txt", b"")
        join_raw(repo / "child" / "nested", b"dirty.txt").unlink()
        bare = init_repo(root / "bare-src")
        write_raw(bare, b"b.txt", b"bare\n")
        run_git(bare, "add", "b.txt")
        run_git(bare, "commit", "-m", "bare")
        sha = run_git(bare, "rev-parse", "HEAD").stdout.strip().decode("ascii")
        run_git(repo, "update-index", "--add", "--cacheinfo", f"160000,{sha},plug")
        checkpoint = capture_checkpoint(repo, (spec,))
        assert checkpoint.gitlink_map()[b"plug"][0] == "uninitialized"


def test_split_index_uses_referenced_shared_index_only() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        repo, spec = seed_repo(Path(temporary))
        run_git(repo, "config", "core.splitIndex", "true")
        run_git(repo, "update-index", "--split-index")
        session = first_pass_session(repo, spec)
        assert session.expected.split_index_ref is not None
        leftover = git_dir(repo) / ("sharedindex." + "00" * 20)
        leftover.write_bytes(b"not-referenced")
        revalidate(session, repo)
        leftover.write_bytes(b"changed-unreferenced")
        revalidate(session, repo)


def test_review_loop_increment_is_declared_mutation() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        repo, spec = seed_repo(Path(temporary))
        session = first_pass_session(repo, spec)
        increment_review_loop(session, repo, spec)
        assert "review_loop_iteration: 3" in join_raw(repo, spec).read_text(encoding="utf-8")
        write_raw(repo, spec, b"no frontmatter to mutate\n")
        refresh_expected(session, repo)
        try:
            increment_review_loop(session, repo, spec)
            raise AssertionError("no-op increment must halt")
        except OwnershipHalt as error:
            assert error.differing_class == "incomplete ownership snapshot"
        write_raw(repo, b"src/other.txt", b"unrelated\ndirty\n")
        try:
            increment_review_loop(session, repo, spec)
            raise AssertionError("dirty increment must halt")
        except OwnershipHalt as error:
            assert error.differing_class == "worktree"


def test_triage_log_deferred_until_outcome() -> None:
    try:
        append_triage_log("spec\n", "\n### log\n", outcome_known=False, already_appended=False)
        raise AssertionError("early triage write must halt")
    except OwnershipHalt as error:
        assert error.differing_class == "triage log before branch outcome"
    once = append_triage_log("spec\n", "\n### log\n", outcome_known=True, already_appended=False)
    try:
        append_triage_log(once, "\n### log\n", outcome_known=True, already_appended=True)
        raise AssertionError("second append must halt")
    except OwnershipHalt as error:
        assert error.differing_class == "duplicate triage log append"


def test_newline_non_utf8_rename_mode_unsupported() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        repo, spec = seed_repo(Path(temporary))
        weird = b"weird\nname.txt"
        latin = b"caf\xe9.txt"
        write_raw(repo, weird, b"newline\n")
        write_raw(repo, latin, b"latin\n")
        run_git(repo, "add", "-A")
        run_git(repo, "commit", "-m", "weird names")
        session = first_pass_session(repo, spec)
        run_git(repo, "mv", "src/other.txt", "src/renamed.txt")
        try:
            revalidate(session, repo)
            raise AssertionError("rename must be path-set drift")
        except OwnershipHalt as error:
            assert error.differing_class == "path-set"
        run_git(repo, "mv", "src/renamed.txt", "src/other.txt")
        session = first_pass_session(repo, spec)
        os.chmod(join_raw(repo, b"src/app.txt"), 0o755)
        try:
            revalidate(session, repo)
            raise AssertionError("mode change must drift")
        except OwnershipHalt as error:
            assert error.differing_class == "worktree"
        os.mkfifo(join_raw(repo, b"pipe"))
        try:
            capture_checkpoint(repo, (spec,))
            raise AssertionError("fifo must be unsupported")
        except OwnershipHalt as error:
            assert error.differing_class == "unsupported path type"


def test_no_version_control_review_and_finalization() -> None:
    assert enter_workflow("ready-for-dev", session=None, vcs=False) == "no-vcs"
    session = Session("NO_VCS", None, None, True, (), no_vcs=True)
    assert session.no_vcs
    assert session.baseline_revision == "NO_VCS"


def test_sanitized_git_environment() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        root = Path(temporary)
        decoy = init_repo(root / "decoy")
        write_raw(decoy, b"decoy.txt", b"decoy\n")
        run_git(decoy, "add", "decoy.txt")
        run_git(decoy, "commit", "-m", "decoy")
        repo, spec = seed_repo(root)
        os.environ["GIT_DIR"] = str(git_dir(decoy))
        os.environ["GIT_WORK_TREE"] = str(decoy)
        try:
            checkpoint = capture_checkpoint(repo, (spec,))
            assert checkpoint.head == run_git(repo, "rev-parse", "HEAD").stdout.strip().decode("ascii")
            assert b"decoy.txt" not in dict(checkpoint.paths)
        finally:
            os.environ.pop("GIT_DIR", None)
            os.environ.pop("GIT_WORK_TREE", None)


def test_raced_nofollow_observation_rejected() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        repo, spec = seed_repo(Path(temporary))
        target = join_raw(repo, b"src/app.txt")
        real_lstat = os.lstat
        calls = {"count": 0}

        def flaky(path, *args, **kwargs):
            result = real_lstat(path, *args, **kwargs)
            if Path(path) == target:
                calls["count"] += 1
                if calls["count"] == 2:
                    mutated = os.stat_result(
                        (
                            result.st_mode ^ stat.S_IXUSR,
                            result.st_ino,
                            result.st_dev,
                            result.st_nlink,
                            result.st_uid,
                            result.st_gid,
                            result.st_size,
                            result.st_atime,
                            result.st_mtime,
                            result.st_ctime,
                        )
                    )
                    return mutated
            return result

        original = os.lstat
        os.lstat = flaky  # type: ignore[assignment]
        try:
            try:
                stable_nofollow_identity(target)
                raise AssertionError("raced nofollow must halt")
            except OwnershipHalt as error:
                assert error.differing_class == "raced path observation"
        finally:
            os.lstat = original


def test_all_entry_points_and_manifest_rows_are_exact_and_unique() -> None:
    for relative in (
        Path(WORKFLOW_NAME),
        Path(STEP01_NAME),
        Path(STEP03_NAME),
        Path(STEP04_NAME),
        TEST_RELATIVE,
    ):
        assert_identical_copies(relative)
    with MANIFEST.open(encoding="utf-8", newline="") as stream:
        rows = list(csv.DictReader(stream))
    selected = [row for row in rows if row["path"] in MANIFEST_PATHS]
    assert len(selected) == len(MANIFEST_PATHS)
    assert {row["path"] for row in selected} == set(MANIFEST_PATHS)
    seen: set[str] = set()
    for row in selected:
        assert row["path"] not in seen
        seen.add(row["path"])
        tracked = PROJECT_ROOT / ".agents" / MANIFEST_PATHS[row["path"]]
        digest = hashlib.sha256(tracked.read_bytes()).hexdigest()
        assert row["hash"] == digest, f"manifest hash drift for {row['path']}"
    fixture_rows = [
        row
        for row in rows
        if row["path"] == "bmm/ship/bmad-build-auto/scripts/tests/test_workspace_ownership.py"
    ]
    assert len(fixture_rows) == 1


def test_isolated_restore_failure_leaves_index_and_worktree() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        repo, spec = seed_repo(Path(temporary))
        session = first_pass_session(repo, spec)
        preimage = file_bytes(repo, b"src/app.txt")
        implement_owned(repo, b"src/app.txt", b"base line\nowned\nkeep me\n")
        session.owned = build_owned_delta(
            repo, [b"src/app.txt"], {b"src/app.txt": preimage}, {spec: join_raw(repo, spec).read_bytes()}
        )
        refresh_expected(session, repo)
        before = snapshot_bytes(repo)
        original = globals()["run_git"]

        def flaky(target, *args, extra_env=None, check=True, stdin=None):
            if args and args[0] == "checkout-index":
                raise RuntimeError("injected checkout failure")
            return original(target, *args, extra_env=extra_env, check=check, stdin=stdin)

        globals()["run_git"] = flaky
        lease = WorktreeLease(repo).acquire()
        try:
            try:
                reverse_owned(session, repo, isolated=True, lease=lease)
                raise AssertionError("failed isolated restore must halt")
            except OwnershipHalt as error:
                assert error.differing_class == "exclusive isolated restore failed"
            assert snapshot_bytes(repo) == before
        finally:
            lease.release()
            globals()["run_git"] = original


def test_mode_only_owned_change_is_in_patch() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        repo, spec = seed_repo(Path(temporary))
        session = first_pass_session(repo, spec)
        preimage = file_bytes(repo, b"src/app.txt")
        os.chmod(join_raw(repo, b"src/app.txt"), 0o755)
        session.owned = build_owned_delta(
            repo, [b"src/app.txt"], {b"src/app.txt": preimage}, {spec: join_raw(repo, spec).read_bytes()}
        )
        assert session.owned.patch
        assert (session.owned.modes[b"src/app.txt"][1] & 0o777) == 0o755
        refresh_expected(session, repo)
        commit_owned(session, repo, spec)
        assert run_git(repo, "ls-tree", "HEAD", "--", "src/app.txt").stdout.startswith(b"100755")


def test_deleted_owned_path_removed_from_shared_index() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        repo, spec = seed_repo(Path(temporary))
        session = first_pass_session(repo, spec)
        preimage = file_bytes(repo, b"src/app.txt")
        join_raw(repo, b"src/app.txt").unlink()
        session.owned = build_owned_delta(
            repo, [b"src/app.txt"], {b"src/app.txt": preimage}, {spec: join_raw(repo, spec).read_bytes()}
        )
        assert session.owned.types[b"src/app.txt"] == "delete"
        refresh_expected(session, repo)
        commit_owned(session, repo, spec)
        assert run_git(repo, "ls-files", "--", "src/app.txt").stdout == b""
        assert b"src/app.txt" not in run_git(repo, "ls-tree", "-r", "--name-only", "HEAD").stdout


def test_parser_and_repair_failures_are_ownership_halt() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        repo, spec = seed_repo(Path(temporary))
        try:
            parse_index_entries(b"DIRC" + b"\x00" * 4 + b"\x00\x00\x00\x01" + b"\x00" * 20)
            raise AssertionError("truncated index must halt")
        except OwnershipHalt as error:
            assert error.differing_class == "incomplete ownership snapshot"
        try:
            parse_index_extensions(
                b"DIRC" + b"\x00" * 8 + b"XXXX" + (2**32 - 1).to_bytes(4, "big") + b"\x00" * 20
            )
            raise AssertionError("unbounded extension must halt")
        except OwnershipHalt as error:
            assert error.differing_class == "incomplete ownership snapshot"
        session = first_pass_session(repo, spec)
        preimage = file_bytes(repo, b"src/app.txt")
        implement_owned(repo, b"src/app.txt", b"base line\nowned\nkeep me\n")
        session.owned = build_owned_delta(
            repo, [b"src/app.txt"], {b"src/app.txt": preimage}, {spec: join_raw(repo, spec).read_bytes()}
        )
        refresh_expected(session, repo)
        try:
            apply_repair(session, repo, b"missing.txt", b"nope\n")
            raise AssertionError("missing repair target must halt")
        except OwnershipHalt as error:
            assert error.differing_class == "stale repair preimage"
            assert error.__context__ is None or not isinstance(error.__context__, KeyError)
        assert file_bytes(repo, b"src/app.txt") == b"base line\nowned\nkeep me\n"


def test_symlink_gitlink_is_uninitialized() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        repo, spec = seed_repo(Path(temporary))
        os.symlink(".", join_raw(repo, b"plug"))
        sha = run_git(repo, "rev-parse", "HEAD").stdout.strip().decode("ascii")
        observed = observe_gitlinks(repo, [(b"plug", 0o160000)])
        assert observed[b"plug"] == ("uninitialized", None, None, (), ())
        run_git(repo, "update-index", "--add", "--cacheinfo", f"160000,{sha},plug")
        observed = observe_gitlinks(repo, parse_index_entries(git_path(repo, "index").read_bytes()))
        assert observed[b"plug"] == ("uninitialized", None, None, (), ())


def test_lease_blockingio_is_unproven() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        repo, spec = seed_repo(Path(temporary))
        first_pass_session(repo, spec)
        real = fcntl.flock

        def blocked(fd: int, operation: int) -> None:
            if operation & fcntl.LOCK_NB:
                raise BlockingIOError()
            return real(fd, operation)

        fcntl.flock = blocked  # type: ignore[assignment]
        try:
            try:
                WorktreeLease(repo).acquire()
                raise AssertionError("blocking lock must halt")
            except OwnershipHalt as error:
                assert error.differing_class == "exclusive isolated ownership unproven"
        finally:
            fcntl.flock = real


def test_ci_runs_canonical_suite_as_blocking_no_bytecode_gate() -> None:
    workflow = CI_WORKFLOW.read_text(encoding="utf-8").replace("\r\n", "\n")
    expected_step = (
        "      - name: Validate Build Auto workspace ownership\n"
        "        env:\n"
        "          PYTHONDONTWRITEBYTECODE: '1'\n"
        "        run: python3 .agents/skills/bmad-build-auto/scripts/tests/test_workspace_ownership.py"
    )
    assert workflow.count(expected_step) == 1
    workflow_gates = workflow.split("  ci:\n", maxsplit=1)[0]
    assert expected_step in workflow_gates
    assert workflow_gates.index(expected_step) < workflow_gates.index(
        "      - name: Validate CI/CD invariants"
    )


def test_matrix_table_is_complete() -> None:
    names = {
        item.__name__
        for item in TESTS
        if item.__name__.startswith("test_matrix_") and item is not test_matrix_table_is_complete
    }
    expected = {f"test_matrix_{name}" for name in MATRIX_SCENARIOS}
    assert names == expected, (sorted(names), sorted(expected))


TESTS = [
    test_matrix_clean_owned_run,
    test_matrix_head_or_index_drift,
    test_matrix_worktree_or_path_set_drift,
    test_matrix_authorized_repair,
    test_matrix_shared_repair_reversal,
    test_matrix_exclusive_isolated_reversal,
    test_matrix_interrupted_resume,
    test_matrix_dirty_follow_up_entry,
    test_matrix_orchestrator_re_drive_handoff,
    test_matrix_done_follow_up_review,
    test_matrix_table_is_complete,
    test_instruction_ordering,
    test_forbidden_broad_operations,
    test_control_owned_spec_included_in_finalization,
    test_persisted_hashed_patch_not_file_list,
    test_bounded_stable_double_capture,
    test_skip_worktree_cannot_hide_bytes,
    test_ignored_control_path_and_executable_drift,
    test_preexisting_same_file_hunk_preserved,
    test_private_index_commit_preserves_unrelated,
    test_stale_repair_preimage_is_a_no_write,
    test_reverse_preflight_failure_is_atomic,
    test_uninitialized_and_nested_gitlinks,
    test_split_index_uses_referenced_shared_index_only,
    test_review_loop_increment_is_declared_mutation,
    test_triage_log_deferred_until_outcome,
    test_newline_non_utf8_rename_mode_unsupported,
    test_no_version_control_review_and_finalization,
    test_sanitized_git_environment,
    test_raced_nofollow_observation_rejected,
    test_isolated_restore_failure_leaves_index_and_worktree,
    test_mode_only_owned_change_is_in_patch,
    test_deleted_owned_path_removed_from_shared_index,
    test_parser_and_repair_failures_are_ownership_halt,
    test_symlink_gitlink_is_uninitialized,
    test_lease_blockingio_is_unproven,
    test_all_entry_points_and_manifest_rows_are_exact_and_unique,
    test_ci_runs_canonical_suite_as_blocking_no_bytecode_gate,
]


if __name__ == "__main__":
    passed = 0
    failed = 0
    for test in TESTS:
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
