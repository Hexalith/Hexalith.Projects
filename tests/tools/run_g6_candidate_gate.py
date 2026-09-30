"""Run every G-6 check of the g6-candidate CI job, choosing the validator mode from the packet status.

1. The Builds G-6 validator checks the bound baseline and packet in --candidate mode while the
   packet status is pending, and in accepted-only mode once it is accepted. Owner acceptance
   therefore needs only the packet status flip and a named decision record; no bound file (this
   script, ci.yml, the workflow gate, the validator or the baseline) changes.
2. Every submodule revision the packet binds must equal its root gitlink
   (check_g6_packet_gitlinks.py).
3. Every Hexalith.Builds workflow or action that ci.yml executes must be pinned to one SHA, and that
   SHA must equal the root Builds gitlink whose catalog, baseline and validator the packet binds.

All three checks run and report, and the gate exits 1 when any of them fails. It runs only in the
g6-candidate job, which no other job needs, so G-6 drift (a bound file, a submodule gitlink or the
Builds execution SHA) fails the G-6 jobs while the workflow, build, test and project gates run.
"""

from __future__ import annotations

import argparse
import importlib.util
import json
import re
import subprocess
import sys
from pathlib import Path


VALIDATOR = Path("references/Hexalith.Builds/Tools/validate-runtime-toolchain-evidence.py")
WORKFLOW = Path(".github/workflows/ci.yml")
BUILDS_PATH = "references/Hexalith.Builds"
BUILDS_REFERENCE = re.compile(r"uses:\s*Hexalith/Hexalith\.Builds/[^\s@]+@(?P<sha>[^\s#]+)")
STATUS_MODES = {"pending": ["--candidate"], "accepted": []}
GITLINK_CHECK_SPEC = importlib.util.spec_from_file_location(
    "check_g6_packet_gitlinks", Path(__file__).resolve().with_name("check_g6_packet_gitlinks.py"))
assert GITLINK_CHECK_SPEC is not None and GITLINK_CHECK_SPEC.loader is not None
GITLINK_CHECK = importlib.util.module_from_spec(GITLINK_CHECK_SPEC)
GITLINK_CHECK_SPEC.loader.exec_module(GITLINK_CHECK)


def packet_status(packet_path: Path) -> str:
    """Return the packet status that selects the validator mode; raise ValueError when there is none."""
    packet = json.loads(packet_path.read_text(encoding="utf-8"))
    if not isinstance(packet, dict):
        raise ValueError("the packet is not a JSON object")
    status = packet.get("status")
    if status not in STATUS_MODES:
        raise ValueError(f"the packet status must be pending or accepted, not {status!r}")
    return status


def run_validator(workspace: Path, baseline: Path, packet: Path) -> list[str]:
    """Run the validator in the mode the packet status selects and return its failures."""
    try:
        status = packet_status(packet)
    except (OSError, ValueError) as error:
        return [f"G6-GATE-VALIDATOR: unable to select the validator mode from {packet}: {error}"]
    mode = "candidate" if status == "pending" else "accepted-only"
    command = [sys.executable, str(workspace / VALIDATOR), "--workspace", str(workspace),
               "--baseline", str(baseline), "--packet", str(packet), *STATUS_MODES[status]]
    completed = subprocess.run(command, capture_output=True, text=True, check=False)
    output = (completed.stdout + completed.stderr).strip()
    print(f"G6-GATE-VALIDATOR: packet status {status}; {mode} mode exited {completed.returncode}: {output}")
    if completed.returncode != 0:
        return [f"G6-GATE-VALIDATOR: the {mode} validator exited {completed.returncode}: {output}"]
    return []


def check_gitlinks(workspace: Path, packet: Path) -> list[str]:
    """Require every packet submodule revision to equal its root gitlink."""
    try:
        drift, bound = GITLINK_CHECK.packet_gitlink_drift(workspace, packet)
    except (OSError, json.JSONDecodeError, GITLINK_CHECK.GitUnavailableError) as error:
        return [f"G6-PACKET-GITLINK-DRIFT: unable to compare {packet} with the root gitlinks: {error}"]
    if drift:
        return [f"G6-PACKET-GITLINK-DRIFT: {item}; recapture the G-6 packet at the committed gitlinks" for item in drift]
    print(f"G6-PACKET-GITLINKS-EXACT: {bound} submodule revisions equal their root gitlinks")
    return []


def check_builds_execution_sha(workspace: Path, workflow: Path) -> list[str]:
    """Require the one executed Hexalith.Builds SHA in the workflow to equal the root Builds gitlink."""
    try:
        text = workflow.read_text(encoding="utf-8")
    except OSError as error:
        return [f"G6-BUILDS-EXECUTION-SHA: unable to read {workflow}: {error}"]
    active = "\n".join(line for line in text.splitlines() if not line.lstrip().startswith("#"))
    executed = sorted({match["sha"] for match in BUILDS_REFERENCE.finditer(active)})
    if len(executed) != 1:
        return [f"G6-BUILDS-EXECUTION-SHA: {workflow} must execute Hexalith.Builds at exactly one SHA; found {executed}"]
    try:
        gitlink = GITLINK_CHECK.root_gitlink(workspace, BUILDS_PATH)
    except GITLINK_CHECK.GitUnavailableError as error:
        return [f"G6-BUILDS-EXECUTION-SHA: unable to read the root Builds gitlink: {error}"]
    if gitlink is None:
        return [f"G6-BUILDS-EXECUTION-SHA: {BUILDS_PATH} is not a root gitlink"]
    if executed[0] != gitlink:
        return [f"G6-BUILDS-EXECUTION-SHA: CI executes Hexalith.Builds '{executed[0]}' but the root Hexalith.Builds gitlink is '{gitlink}'"]
    print(f"G6-BUILDS-EXECUTION-SHA-EXACT: CI executes Hexalith.Builds at the root gitlink {gitlink}")
    return []


def main(arguments: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--workspace", type=Path, default=Path("."))
    parser.add_argument("--baseline", type=Path, required=True)
    parser.add_argument("--packet", type=Path, required=True)
    parser.add_argument("--workflow", type=Path, default=WORKFLOW)
    options = parser.parse_args(arguments)
    workspace = options.workspace.resolve()

    def resolve(path: Path) -> Path:
        return path if path.is_absolute() else workspace / path

    failures = [
        *run_validator(workspace, resolve(options.baseline), resolve(options.packet)),
        *check_gitlinks(workspace, resolve(options.packet)),
        *check_builds_execution_sha(workspace, resolve(options.workflow)),
    ]
    for failure in failures:
        print(failure, file=sys.stderr)
    if failures:
        print(f"G6-GATE-FAILED: {len(failures)} G-6 check(s) failed", file=sys.stderr)
        return 1
    print("G6-GATE-PASSED: validator, packet gitlinks and Builds execution SHA")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
