"""Require every submodule revision bound by a G-6 packet to equal its root gitlink.

The G-6 validator only requires a bound revision to be an ancestor of the checked-out
submodule commit. This check makes the binding exact: each packet repository other than the
root (path ".") must be a root gitlink whose recorded commit is the packet revision. It runs
in the G-6 CI jobs, so a routine submodule bump fails only those jobs until the packet is
recaptured, while the workflow, build, test and project gates keep running.

A packet that is not a JSON object, a repository binding that is not an object, a missing git
executable and an unreadable root index are each reported as their own failure, never as a
missing gitlink.
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from pathlib import Path


class GitUnavailableError(Exception):
    """Raised when git cannot run or cannot read the root index."""


def root_gitlink(workspace: Path, path: str) -> str | None:
    """Return the commit the root index records for a gitlink path, or None when it is not one."""
    try:
        completed = subprocess.run(
            ["git", "-C", str(workspace), "ls-files", "--stage", "--", path],
            capture_output=True,
            text=True,
            check=False,
        )
    except OSError as error:
        raise GitUnavailableError(f"git is not available: {error}") from error
    if completed.returncode != 0:
        raise GitUnavailableError(
            f"git cannot read the root index of {workspace}: {completed.stderr.strip() or f'exit {completed.returncode}'}")
    match = re.fullmatch(r"160000 ([0-9a-f]{40}) 0\t" + re.escape(path), completed.stdout.strip())
    return match.group(1) if match else None


def packet_gitlink_drift(workspace: Path, packet_path: Path) -> tuple[list[str], int]:
    """Return every binding that is not its exact root gitlink, and the number of submodule bindings."""
    packet = json.loads(packet_path.read_text(encoding="utf-8"))
    if not isinstance(packet, dict):
        return ["the packet is not a JSON object"], 0
    repositories = packet.get("repositories")
    if not isinstance(repositories, list):
        return ["the packet has no repository bindings"], 0
    malformed = [f"repository binding {index} is not an object"
                 for index, item in enumerate(repositories) if not isinstance(item, dict)]
    if malformed:
        return malformed, 0
    bindings = [item for item in repositories if item.get("path") != "."]
    if not bindings:
        return ["the packet binds no submodule repositories"], 0
    drift = []
    for binding in bindings:
        name, path, revision = binding.get("name"), binding.get("path"), binding.get("revision")
        if not isinstance(path, str) or not path:
            drift.append(f"{name} has no repository path")
            continue
        gitlink = root_gitlink(workspace, path)
        if gitlink is None:
            drift.append(f"{name} is bound at '{path}', which is not a root gitlink")
        elif gitlink != revision:
            drift.append(f"{name} is bound at '{revision}' but the root gitlink is '{gitlink}'")
    return drift, len(bindings)


def main(arguments: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--workspace", type=Path, default=Path("."))
    parser.add_argument("--packet", type=Path, required=True)
    options = parser.parse_args(arguments)
    workspace = options.workspace.resolve()
    packet_path = options.packet if options.packet.is_absolute() else workspace / options.packet
    try:
        drift, bound = packet_gitlink_drift(workspace, packet_path)
    except (OSError, json.JSONDecodeError) as error:
        print(f"G6-PACKET-GITLINK-DRIFT: unable to read {options.packet}: {error}", file=sys.stderr)
        return 1
    except GitUnavailableError as error:
        print(f"G6-PACKET-GITLINK-GIT-UNAVAILABLE: {error}", file=sys.stderr)
        return 1
    if drift:
        for item in drift:
            print(f"G6-PACKET-GITLINK-DRIFT: {item}; recapture the G-6 packet at the committed gitlinks", file=sys.stderr)
        return 1
    print(f"G6-PACKET-GITLINKS-EXACT: {bound} submodule revisions equal their root gitlinks")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
