"""Verify the SHA-256 manifests in the committed Projects qualification archive."""

import hashlib
import os
import re
import subprocess
import sys
from pathlib import PurePosixPath


ARCHIVE = "_bmad-output/implementation-artifacts/qualification-evidence"
MANIFEST = "artifact-manifest.sha256"
ENTRY = re.compile(r"(\\?)([0-9a-fA-F]{64}) [ *](.+)")
FILENAME_ESCAPES = {"n": "\n", "r": "\r", "\\": "\\"}
REGULAR_MODES = {b"100644", b"100755"}


def committed_entries(revision: str) -> dict[PurePosixPath, tuple[bytes, bytes]]:
    tree = subprocess.check_output(
        ["git", "ls-tree", "-rz", "--full-tree", revision, "--", ARCHIVE]
    )
    entries = {}
    for record in tree.split(b"\0"):
        if record:
            metadata, path = record.split(b"\t", 1)
            mode, _, oid = metadata.split()
            entries[PurePosixPath(os.fsdecode(path))] = (mode, oid)
    return entries


def blob_digest(oid: bytes) -> str:
    command = ["git", "cat-file", "blob", oid.decode("ascii")]
    with subprocess.Popen(command, stdout=subprocess.PIPE) as process:
        digest = hashlib.sha256()
        for chunk in iter(lambda: process.stdout.read(1024 * 1024), b""):
            digest.update(chunk)
        if process.wait():
            raise subprocess.CalledProcessError(process.returncode, command)
    return digest.hexdigest()


def verify_manifest(manifest: PurePosixPath, entries: dict[PurePosixPath, tuple[bytes, bytes]]) -> bool:
    mode, oid = entries[manifest]
    if mode not in REGULAR_MODES:
        print(f"{manifest}: manifest must be a tracked regular file", file=sys.stderr)
        return False
    content = subprocess.check_output(["git", "cat-file", "blob", oid.decode("ascii")])
    lines = os.fsdecode(content).removesuffix("\n").split("\n") if content else []
    if not lines:
        print(f"{manifest}: empty manifest", file=sys.stderr)
        return False

    seen = set()
    verified = 0
    for number, line in enumerate(lines, 1):
        location = f"{manifest}:{number}"
        match = ENTRY.fullmatch(line.removesuffix("\r"))
        if not match:
            print(f"{location}: expected a sha256sum entry", file=sys.stderr)
            continue
        escaped, expected, filename = match.groups()
        if escaped:
            try:
                filename = re.sub(r"\\(.|$)", lambda m: FILENAME_ESCAPES[m[1]], filename)
            except KeyError:
                print(f"{location}: invalid sha256sum filename escape", file=sys.stderr)
                continue
        path = PurePosixPath(filename)
        if path.is_absolute() or ".." in path.parts or not path.parts or filename.endswith(("/", "/.")):
            print(f"{location}: invalid bundle-relative path {filename!r}", file=sys.stderr)
            continue
        if path in seen:
            print(f"{location}: duplicate entry {filename!r}", file=sys.stderr)
            continue
        seen.add(path)
        artifact = entries.get(manifest.parent / path)
        if artifact is None or artifact[0] not in REGULAR_MODES:
            print(f"{location}: no tracked regular file for {filename!r}", file=sys.stderr)
            continue
        actual = blob_digest(artifact[1])
        if actual != expected.lower():
            print(f"{location}: {filename!r}: expected {expected}, got {actual}", file=sys.stderr)
            continue
        verified += 1
    print(f"{manifest}: {verified}/{len(lines)} entries verified")
    return verified == len(lines)


def main() -> int:
    try:
        revision = subprocess.check_output(["git", "rev-parse", "--verify", "HEAD^{commit}"], text=True).strip()
        entries = committed_entries(revision)
        manifests = sorted(path for path in entries if path.name == MANIFEST)
        if not manifests:
            print(f"{revision}: no {MANIFEST} under {ARCHIVE}", file=sys.stderr)
            return 1
        print(f"Verifying {len(manifests)} manifests at {revision}")
        results = [verify_manifest(manifest, entries) for manifest in manifests]
        return 0 if all(results) else 1
    except (OSError, subprocess.CalledProcessError) as error:
        print(f"Could not read committed qualification evidence: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
