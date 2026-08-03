#!/usr/bin/env python3
"""Fail-closed scheduling guard for the Hexalith.Projects production backlog."""

from __future__ import annotations

import argparse
import hashlib
import re
import sys
from pathlib import Path


EXPECTED_PRODUCTION_EPICS = (6, 7, 8)
HISTORICAL_EPICS = (1, 2, 3, 4, 5)
EXPECTED_HISTORICAL_STATUS_SHA256 = (
    "ce120516809be4a3814565f67c7a1e648650b3b9f0547ed9a4135a78eba0bbaf"
)
PROJECT_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_SPRINT_STATUS = (
    PROJECT_ROOT / "_bmad-output" / "implementation-artifacts" / "sprint-status.yaml"
)


class GuardViolation(ValueError):
    """Raised when a scheduling request or index violates production authority."""


def parse_production_authority_epics(sprint_status: str) -> tuple[int, ...]:
    """Read the single authoritative root scheduling field without a YAML dependency."""

    matches = re.findall(
        r"^production_authority_epics:\s*\[([^\]]*)\]\s*$",
        sprint_status,
        flags=re.MULTILINE,
    )
    if len(matches) != 1:
        raise GuardViolation(
            "sprint-status.yaml must contain exactly one root production_authority_epics field"
        )

    try:
        epics = tuple(int(value.strip()) for value in matches[0].split(","))
    except ValueError as error:
        raise GuardViolation("production_authority_epics must contain integers") from error

    if epics != EXPECTED_PRODUCTION_EPICS:
        raise GuardViolation(
            "production_authority_epics must remain exactly [6, 7, 8]; "
            f"found {list(epics)}"
        )

    return epics


def parse_historical_status_sha256(sprint_status: str) -> str:
    """Read and pin the immutable historical development-status digest."""

    matches = re.findall(
        r"^historical_development_status_sha256:\s*([0-9a-f]{64})\s*$",
        sprint_status,
        flags=re.MULTILINE,
    )
    if len(matches) != 1:
        raise GuardViolation(
            "sprint-status.yaml must contain exactly one root "
            "historical_development_status_sha256 field"
        )
    if matches[0] != EXPECTED_HISTORICAL_STATUS_SHA256:
        raise GuardViolation(
            "historical_development_status_sha256 does not match the approved immutable inventory"
        )
    return matches[0]


def parse_development_status(sprint_status: str) -> list[tuple[str, str]]:
    """Extract ordered development-status entries from the repository-owned YAML shape."""

    lines = sprint_status.splitlines()
    if lines.count("development_status:") != 1:
        raise GuardViolation(
            "sprint-status.yaml must contain exactly one root development_status mapping"
        )
    start = lines.index("development_status:") + 1

    entries: list[tuple[str, str]] = []
    seen_keys: set[str] = set()
    entry_pattern = re.compile(r"^  ([A-Za-z0-9][^:]*):\s*([A-Za-z][A-Za-z-]*)\s*$")
    root_key_pattern = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*:\s*")

    for line in lines[start:]:
        if root_key_pattern.match(line):
            break
        if not line.strip() or line.lstrip().startswith("#"):
            continue
        match = entry_pattern.match(line)
        if not match:
            raise GuardViolation(
                "unrecognized development_status child; only canonical unquoted "
                f"key/status lines are accepted: {line!r}"
            )
        key, status = match.group(1), match.group(2)
        if key in seen_keys:
            raise GuardViolation(f"duplicate development_status key: {key}")
        seen_keys.add(key)
        entries.append((key, status))

    if not entries:
        raise GuardViolation("development_status contains no schedulable entries")

    return entries


def historical_status_sha256(entries: list[tuple[str, str]]) -> str:
    """Hash the complete ordered Epic 1-5 tracking inventory and statuses."""

    historical_entries = [
        (key, status)
        for key, status in entries
        if tracking_key_epic(key) in HISTORICAL_EPICS
    ]
    payload = "".join(f"{key}:{status}\n" for key, status in historical_entries)
    return hashlib.sha256(payload.encode("utf-8")).hexdigest()


def tracking_key_epic(key: str) -> int | None:
    """Return the epic number for epic, story, or retrospective tracking keys."""

    epic_match = re.fullmatch(r"epic-(\d+)(?:-retrospective)?", key)
    if epic_match:
        return int(epic_match.group(1))

    story_match = re.fullmatch(
        r"(\d+)-(\d+)-[a-z0-9]+(?:-[a-z0-9]+)*",
        key,
    )
    if story_match:
        return int(story_match.group(1))

    return None


def requested_story_parts(story_id: str) -> tuple[int, int]:
    """Parse a story identifier accepted by BMad entry points."""

    numeric_match = re.fullmatch(
        r"[ \t]*(?:story[ \t]+)?(\d+)\.(\d+)[ \t]*",
        story_id,
        flags=re.IGNORECASE,
    )
    tracking_match = re.fullmatch(
        r"[ \t]*(\d+)-(\d+)-[a-z0-9]+(?:-[a-z0-9]+)*[ \t]*",
        story_id,
        flags=re.IGNORECASE,
    )
    match = numeric_match or tracking_match
    if match is None:
        raise GuardViolation(
            f"invalid story identifier {story_id!r}; expected forms such as 6.1 or 6-1-title"
        )
    return int(match.group(1)), int(match.group(2))


def assert_story_allowed(story_id: str) -> tuple[int, int]:
    """Reject any story outside the approved production-authority epics."""

    epic, story = requested_story_parts(story_id)
    if epic in HISTORICAL_EPICS:
        raise GuardViolation(
            f"Story {epic}.{story} belongs to immutable implementation history; "
            "Epics 1-5 cannot be created, reopened, or scheduled as current production work"
        )
    if epic not in EXPECTED_PRODUCTION_EPICS:
        raise GuardViolation(
            f"Story {epic}.{story} is outside production authority {list(EXPECTED_PRODUCTION_EPICS)}"
        )
    return epic, story


def validate_index_snapshot(
    sprint_status: str,
) -> tuple[tuple[int, ...], list[tuple[str, str]]]:
    """Validate one in-memory scheduling snapshot without a check/use race."""

    authority = parse_production_authority_epics(sprint_status)
    recorded_historical_digest = parse_historical_status_sha256(sprint_status)
    entries = parse_development_status(sprint_status)
    epic_statuses: dict[int, str] = {}

    for key, status in entries:
        epic = tracking_key_epic(key)
        if epic is None:
            raise GuardViolation(f"unrecognized development_status key: {key}")
        if epic not in HISTORICAL_EPICS + EXPECTED_PRODUCTION_EPICS:
            raise GuardViolation(f"development_status key {key} is outside Epics 1-8")
        if epic in HISTORICAL_EPICS and status != "done":
            raise GuardViolation(
                f"historical tracking key {key} must remain done; found {status}"
            )
        if key == f"epic-{epic}":
            epic_statuses[epic] = status

    missing_epics = [
        epic
        for epic in HISTORICAL_EPICS + EXPECTED_PRODUCTION_EPICS
        if epic not in epic_statuses
    ]
    if missing_epics:
        raise GuardViolation(f"development_status is missing epic keys {missing_epics}")

    actual_historical_digest = historical_status_sha256(entries)
    if actual_historical_digest != recorded_historical_digest:
        raise GuardViolation(
            "Epic 1-5 development_status keys or statuses differ from the approved immutable "
            f"inventory: {actual_historical_digest}"
        )

    return authority, entries


def validate_index(sprint_status_path: Path = DEFAULT_SPRINT_STATUS) -> tuple[int, ...]:
    """Validate the authority field and immutable historical tracking states."""

    try:
        sprint_status = sprint_status_path.read_text(encoding="utf-8")
    except OSError as error:
        raise GuardViolation(f"cannot read {sprint_status_path}: {error}") from error

    authority, _ = validate_index_snapshot(sprint_status)
    return authority


def validate_story_request(
    story_id: str,
    sprint_status_path: Path = DEFAULT_SPRINT_STATUS,
) -> tuple[int, int]:
    """Validate both the index and a requested production story."""

    try:
        sprint_status = sprint_status_path.read_text(encoding="utf-8")
    except OSError as error:
        raise GuardViolation(f"cannot read {sprint_status_path}: {error}") from error

    _, entries = validate_index_snapshot(sprint_status)
    epic, story = assert_story_allowed(story_id)
    prefix = f"{epic}-{story}-"
    matches = [key for key, _ in entries if key.startswith(prefix)]
    if len(matches) != 1:
        raise GuardViolation(
            f"Story {epic}.{story} must resolve to exactly one development_status key; "
            f"found {matches}"
        )
    return epic, story


def build_parser() -> argparse.ArgumentParser:
    """Build the command-line parser."""

    parser = argparse.ArgumentParser(description=__doc__)
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument(
        "--validate-index",
        action="store_true",
        help="validate sprint reconciliation authority and immutable historical states",
    )
    group.add_argument(
        "--story-id",
        help="validate a story creation, reopening, or scheduling request",
    )
    parser.add_argument(
        "--sprint-status",
        type=Path,
        default=DEFAULT_SPRINT_STATUS,
        help="candidate sprint-status path to validate before atomic replacement",
    )
    return parser


def main() -> int:
    """Run the requested fail-closed validation."""

    arguments = build_parser().parse_args()
    try:
        if arguments.validate_index:
            authority = validate_index(arguments.sprint_status)
            print(f"PASS: production-authority index is {list(authority)}")
        else:
            epic, story = validate_story_request(
                arguments.story_id,
                arguments.sprint_status,
            )
            print(f"PASS: Story {epic}.{story} is within production authority")
    except GuardViolation as error:
        print(f"BLOCKED: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
