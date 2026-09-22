#!/usr/bin/env python3
"""Fail-closed scheduling and 6.1-P1R acceptance guard."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import sys
from datetime import datetime
from pathlib import Path
from typing import Any

import yaml


EXPECTED_PRODUCTION_EPICS = (6, 7, 8)
HISTORICAL_EPICS = (1, 2, 3, 4, 5)
EXPECTED_HISTORICAL_STATUS_SHA256 = (
    "ce120516809be4a3814565f67c7a1e648650b3b9f0547ed9a4135a78eba0bbaf"
)
STORY_6_1_KEY = "6-1-list-and-open-projects-through-supported-authenticated-paths"
ACCEPTANCE_SCHEMA = "hexalith.projects.p1r-acceptance.v1"
ACCEPTANCE_RECORD_RELATIVE_PATH = Path(
    "_bmad-output/implementation-artifacts/6-1-p1r-acceptance.json"
)
SELECTED_TUPLE = {
    "eventstore_version": "3.106.0",
    "eventstore_tag": "v3.106.0",
    "eventstore_revision": "76051c70cbf868c40edc00ca0344fa5bd8879b69",
    "builds_revision": "ad52f350a2f0bc47849179ae17b4594dafff5363",
}
ROLLBACK_TUPLE = {
    "eventstore_version": "3.70.1",
    "eventstore_tag": "v3.70.1",
    "eventstore_revision": "f13f9925fdca53efa2ab8c90d396ab106f91bb9c",
    "builds_revision": "7af20f8bafbfe561df6f7705913a0800603090b5",
}
P0_CAPABILITY_STATUS = {
    "manifest_contract": "implemented-unaccepted",
    "evidence_validator": "implemented-unaccepted",
    "persisted_runner": "not-available",
    "published_tools": "published-stale-pin-not-supported",
    "supported_composition": "not-implemented",
    "published_consumer_pin": "absent",
    "persisted_qualification": "not-run",
    "acceptance_record": "absent",
    "owner_acceptance": "absent",
}
P0_BLOCKED_BY = (
    "supported-composition",
    "publication",
    "consumer-pin",
    "persisted-qualification",
    "acceptance-record",
    "owner-acceptance",
)
ACCEPTED_ROLES = (
    "EventStore Owner",
    "Builds Owner",
    "Solution Architect",
    "Test Architect",
)
SELECTED_REFERENCE = "#/selected"

EPIC6_DEVELOPMENT_STATUSES = {
    "epic-6": "backlog",
    STORY_6_1_KEY: "blocked",
    "6-2-retrieve-conversation-start-setup-with-admission-truth": "done",
    "6-3-retrieve-assembled-project-context-through-supported-read-models": "done",
    "6-4-resolve-projects-with-transient-current-explanations": "blocked",
    "6-5-inspect-projects-through-an-authenticated-frontcomposer-read-surface": "backlog",
    "6-6-inspect-projects-through-an-authenticated-cli-read-surface": "backlog",
    "6-7-cut-over-supported-reads-while-preserving-compatibility-and-rollback": "backlog",
    "6-8-split-hexalith-projects-ui-contracts-from-contracts": "backlog",
    "epic-6-retrospective": "optional",
}
TRANSITIVE_EPIC_STATUSES = {
    "epic-7": "backlog",
    "7-1-activate-a-project-with-exactly-one-authorized-folder": "blocked",
    "7-2-update-project-setup-idempotently": "backlog",
    "7-3-link-an-unassigned-conversation": "backlog",
    "7-4-move-a-conversation-between-projects": "backlog",
    "7-5-unlink-a-conversation": "backlog",
    "7-6-replace-a-project-folder": "backlog",
    "7-7-link-an-authorized-file-reference": "backlog",
    "7-8-unlink-a-file-reference": "backlog",
    "7-9-link-an-authorized-memory": "backlog",
    "7-10-unlink-a-memory": "backlog",
    "7-11-confirm-an-ambiguous-project-choice": "backlog",
    "7-12-confirm-a-proposed-new-project": "backlog",
    "7-13-archive-an-active-project": "backlog",
    "7-14-restore-an-archived-project": "backlog",
    "7-15-reconcile-legacy-and-interrupted-workflows": "backlog",
    "7-16-select-an-association-target": "backlog",
    "epic-7-retrospective": "optional",
    "epic-8": "backlog",
    "8-1-inspect-task-audit-and-reconciliation-truth": "backlog",
    "8-2-create-a-bounded-safe-diagnostic-export": "backlog",
    "8-3-integrate-conformant-project-maintenance-in-the-web-console": "backlog",
    "8-4-operate-projects-through-a-deterministic-cli-contract": "backlog",
    "8-5-operate-projects-through-agent-safe-mcp-contracts": "backlog",
    "8-6-observe-truthful-dependency-and-projection-health": "backlog",
    "8-7-consume-reproducible-packages-and-supply-chain-evidence": "backlog",
    "8-8-integrate-authenticated-isolation-privacy-parity-and-accessibility-evidence": "blocked",
    "8-9-meet-bounded-performance-and-back-pressure-objectives": "backlog",
    "8-10-prove-cross-workflow-resilience": "backlog",
    "8-11-record-the-terminal-production-release-decision": "blocked",
    "epic-8-retrospective": "optional",
}
ACTION_STATUSES = {
    "6.1-P0": (6, "open"),
    "6.1-P1": (6, "done"),
    "6.1-P1R": (6, None),
    "6.1-P2": (6, "open"),
    "6.1-P3": (6, "open"),
    "6.1-P4": (6, "open"),
    "7.1-P1": (7, "open"),
    "7.1-P2": (7, "open"),
    "8.3-P1": (8, "open"),
    "8.3-P2": (8, "open"),
    "8.3-P3": (8, "open"),
    "8.8-P1": (8, "open"),
    "8.8-P2": (8, "open"),
    "8.8-P3": (8, "blocked-external"),
    "8.11-P1": (8, "open"),
    "8.11-P2": (8, "open"),
    "8.11-P3": (8, "open"),
}

PROJECT_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_SPRINT_STATUS = (
    PROJECT_ROOT / "_bmad-output" / "implementation-artifacts" / "sprint-status.yaml"
)
DEFAULT_DEFERRED_WORK = (
    PROJECT_ROOT / "_bmad-output" / "implementation-artifacts" / "deferred-work.md"
)
DEFAULT_P0_ARTIFACT = PROJECT_ROOT / (
    "_bmad-output/implementation-artifacts/"
    "6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md"
)


class GuardViolation(ValueError):
    """Raised when a scheduling request or acceptance state fails closed."""


class UniqueKeyLoader(yaml.SafeLoader):
    """PyYAML safe loader that rejects duplicate mapping keys."""


def _construct_unique_mapping(
    loader: UniqueKeyLoader,
    node: yaml.MappingNode,
    deep: bool = False,
) -> dict[Any, Any]:
    mapping: dict[Any, Any] = {}
    for key_node, value_node in node.value:
        key = loader.construct_object(key_node, deep=deep)
        try:
            duplicate = key in mapping
        except TypeError as error:
            raise GuardViolation("YAML mapping keys must be scalar") from error
        if duplicate:
            raise GuardViolation(f"duplicate YAML key: {key!r}")
        mapping[key] = loader.construct_object(value_node, deep=deep)
    return mapping


UniqueKeyLoader.add_constructor(
    yaml.resolver.BaseResolver.DEFAULT_MAPPING_TAG,
    _construct_unique_mapping,
)


def _load_yaml_text(text: str, source: str) -> dict[str, Any]:
    try:
        loaded = yaml.load(text, Loader=UniqueKeyLoader)
    except (yaml.YAMLError, GuardViolation) as error:
        raise GuardViolation(f"invalid YAML in {source}: {error}") from error
    if not isinstance(loaded, dict):
        raise GuardViolation(f"{source} must contain a YAML mapping")
    return loaded


def _read_text(path: Path, label: str) -> str:
    try:
        return path.read_text(encoding="utf-8")
    except (OSError, UnicodeError) as error:
        raise GuardViolation(f"cannot read {label} {path}: {error}") from error


def _mapping(value: Any, context: str) -> dict[str, Any]:
    if not isinstance(value, dict) or not all(
        isinstance(key, str) for key in value
    ):
        raise GuardViolation(f"{context} must be a mapping with string keys")
    return value


def _exact_keys(value: dict[str, Any], keys: tuple[str, ...], context: str) -> None:
    if set(value) != set(keys):
        raise GuardViolation(
            f"{context} keys must be exactly {list(keys)}; found {list(value)}"
        )


def tracking_key_epic(key: str) -> int | None:
    """Return the epic number for an epic, story, or retrospective key."""

    epic_match = re.fullmatch(r"epic-(\d+)(?:-retrospective)?", key)
    if epic_match:
        return int(epic_match.group(1))
    story_match = re.fullmatch(r"(\d+)-(\d+)-[a-z0-9]+(?:-[a-z0-9]+)*", key)
    return int(story_match.group(1)) if story_match else None


def historical_status_sha256(entries: list[tuple[str, str]]) -> str:
    """Hash the complete ordered Epic 1-5 tracking inventory and statuses."""

    payload = "".join(
        f"{key}:{status}\n"
        for key, status in entries
        if tracking_key_epic(key) in HISTORICAL_EPICS
    )
    return hashlib.sha256(payload.encode("utf-8")).hexdigest()


def parse_development_status(sprint: dict[str, Any]) -> list[tuple[str, str]]:
    """Return the ordered development-status mapping after shape validation."""

    development = _mapping(sprint.get("development_status"), "development_status")
    entries: list[tuple[str, str]] = []
    for key, status in development.items():
        if not isinstance(status, str):
            raise GuardViolation(f"development_status.{key} must be a string")
        epic = tracking_key_epic(key)
        if epic is None or epic not in HISTORICAL_EPICS + EXPECTED_PRODUCTION_EPICS:
            raise GuardViolation(f"unrecognized development_status key: {key}")
        entries.append((key, status))
    return entries


def _validate_scheduling(sprint: dict[str, Any]) -> list[tuple[str, str]]:
    if sprint.get("production_authority_epics") != list(EXPECTED_PRODUCTION_EPICS):
        raise GuardViolation("production_authority_epics must remain exactly [6, 7, 8]")
    if (
        sprint.get("historical_development_status_sha256")
        != EXPECTED_HISTORICAL_STATUS_SHA256
    ):
        raise GuardViolation("historical development-status digest is not approved")

    entries = parse_development_status(sprint)
    if any(
        status != "done"
        for key, status in entries
        if tracking_key_epic(key) in HISTORICAL_EPICS
    ):
        raise GuardViolation("all historical Epic 1-5 statuses must remain done")
    if historical_status_sha256(entries) != EXPECTED_HISTORICAL_STATUS_SHA256:
        raise GuardViolation("Epic 1-5 inventory differs from immutable history")

    production = {
        key: status
        for key, status in entries
        if tracking_key_epic(key) in EXPECTED_PRODUCTION_EPICS
    }
    expected = {**EPIC6_DEVELOPMENT_STATUSES, **TRANSITIVE_EPIC_STATUSES}
    if production != expected:
        raise GuardViolation("Epic 6-8 development statuses exceed the open boundary")

    readiness = _mapping(sprint.get("readiness_provenance"), "readiness_provenance")
    if readiness.get("current_result") != "NOT_READY":
        raise GuardViolation("implementation readiness must remain NOT_READY")
    return entries


def _action_map(sprint: dict[str, Any]) -> dict[str, dict[str, Any]]:
    actions = sprint.get("action_items")
    if not isinstance(actions, list):
        raise GuardViolation("action_items must be a list")
    protected: dict[str, dict[str, Any]] = {}
    for action in actions:
        if not isinstance(action, dict):
            raise GuardViolation("each action item must be a mapping")
        epic = action.get("epic")
        item_id = action.get("id")
        if not isinstance(epic, int) or epic < 6:
            continue
        if not isinstance(item_id, str):
            raise GuardViolation(f"Epic {epic} action is missing a string id")
        if item_id in protected:
            raise GuardViolation(f"duplicate action item id {item_id}")
        protected[item_id] = action
    if tuple(protected) != tuple(ACTION_STATUSES):
        raise GuardViolation("Epic 6-8 action inventory must remain exact")
    return protected


def _validate_gate_coordinates(sprint: dict[str, Any]) -> None:
    gate = _mapping(sprint.get("p1r_acceptance_gate"), "p1r_acceptance_gate")
    _exact_keys(gate, ("record_path", "selected", "rollback"), "p1r_acceptance_gate")
    if gate["record_path"] != ACCEPTANCE_RECORD_RELATIVE_PATH.as_posix():
        raise GuardViolation("P1R acceptance record path differs from the fixed path")
    selected = _mapping(gate["selected"], "p1r_acceptance_gate.selected")
    rollback = _mapping(gate["rollback"], "p1r_acceptance_gate.rollback")
    if selected != SELECTED_TUPLE:
        raise GuardViolation("sprint-selected tuple differs from the approved coordinates")
    if rollback != ROLLBACK_TUPLE:
        raise GuardViolation("sprint rollback tuple differs from the approved coordinates")


def _unique_json_object(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    result: dict[str, Any] = {}
    for key, value in pairs:
        if key in result:
            raise GuardViolation(f"duplicate JSON key: {key}")
        result[key] = value
    return result


def _load_acceptance_record(path: Path) -> dict[str, Any] | None:
    if not os.path.lexists(path):
        return None
    if path.is_symlink():
        raise GuardViolation("acceptance record path must not be a symlink")
    if not path.is_file():
        raise GuardViolation("acceptance record path is not a regular file")
    try:
        with path.open(encoding="utf-8") as stream:
            value = json.load(stream, object_pairs_hook=_unique_json_object)
    except (OSError, UnicodeError, json.JSONDecodeError, GuardViolation) as error:
        raise GuardViolation(f"malformed acceptance record: {error}") from error
    if not isinstance(value, dict):
        raise GuardViolation("malformed acceptance record: root must be an object")
    return value


def _validate_acceptance_record(record: dict[str, Any]) -> None:
    _exact_keys(
        record,
        ("schema", "selected", "rollback", "timestamp_utc", "decisions"),
        "acceptance record",
    )
    if record["schema"] != ACCEPTANCE_SCHEMA:
        raise GuardViolation("acceptance record schema is unsupported")
    selected = _mapping(record["selected"], "acceptance record selected")
    rollback = _mapping(record["rollback"], "acceptance record rollback")
    _exact_keys(selected, tuple(SELECTED_TUPLE), "acceptance record selected")
    _exact_keys(rollback, tuple(ROLLBACK_TUPLE), "acceptance record rollback")
    if selected != SELECTED_TUPLE:
        raise GuardViolation("acceptance record selected coordinate mismatch")
    if rollback != ROLLBACK_TUPLE:
        raise GuardViolation("acceptance record rollback coordinate mismatch")

    timestamp = record["timestamp_utc"]
    if not isinstance(timestamp, str):
        raise GuardViolation("acceptance record timestamp_utc must be a string")
    try:
        parsed = datetime.strptime(timestamp, "%Y-%m-%dT%H:%M:%SZ")
    except ValueError as error:
        raise GuardViolation("acceptance record timestamp_utc is not canonical UTC") from error
    if parsed.strftime("%Y-%m-%dT%H:%M:%SZ") != timestamp:
        raise GuardViolation("acceptance record timestamp_utc is not canonical UTC")

    decisions = _mapping(record["decisions"], "acceptance record decisions")
    _exact_keys(decisions, ACCEPTED_ROLES, "acceptance record decisions")
    for role in ACCEPTED_ROLES:
        decision = _mapping(decisions[role], f"decision for {role}")
        _exact_keys(
            decision,
            ("decision", "approver", "record", "selected"),
            f"decision for {role}",
        )
        if decision["decision"] != "accept":
            raise GuardViolation(f"invalid decision for {role}: expected accept")
        if not isinstance(decision["approver"], str) or not decision[
            "approver"
        ].strip():
            raise GuardViolation(f"invalid approver for {role}")
        if decision["record"] != ACCEPTANCE_RECORD_RELATIVE_PATH.as_posix():
            raise GuardViolation(f"invalid record path for {role}")
        if decision["selected"] != SELECTED_REFERENCE:
            raise GuardViolation(f"invalid selected tuple reference for {role}")


def _frontmatter_lines(text: str, source: str) -> list[str]:
    """Return the delimited frontmatter lines without parsing Markdown as YAML."""

    lines = text.splitlines()
    if not lines or lines[0].strip() != "---":
        raise GuardViolation(f"{source} is missing YAML frontmatter")
    try:
        end = next(index for index, line in enumerate(lines[1:], 1) if line.strip() == "---")
    except StopIteration as error:
        raise GuardViolation(f"{source} has unterminated YAML frontmatter") from error
    return lines[1:end]


def _named_markdown_field(
    lines: list[str], field: str, context: str, *, indent: int = 0
) -> str:
    prefix = f"{' ' * indent}{field}:"
    matches = [line[len(prefix) :].strip() for line in lines if line.startswith(prefix)]
    if len(matches) != 1 or not matches[0]:
        raise GuardViolation(f"{context} must contain exactly one non-empty {field} field")
    return matches[0]


def _markdown_section(lines: list[str], section: str, source: str) -> list[str]:
    starts = [index for index, line in enumerate(lines) if line == f"{section}:"]
    if len(starts) != 1:
        raise GuardViolation(f"{source} must contain exactly one {section} section")
    start = starts[0]
    end = next(
        (
            index
            for index in range(start + 1, len(lines))
            if lines[index] and not lines[index].startswith(" ")
        ),
        len(lines),
    )
    return lines[start + 1 : end]


def _markdown_mapping(lines: list[str], context: str) -> dict[str, str]:
    result: dict[str, str] = {}
    for line in lines:
        match = re.fullmatch(r"  ([a-z][a-z0-9_]*): ([A-Za-z0-9][A-Za-z0-9-]*)", line)
        if match is None or match.group(1) in result:
            raise GuardViolation(f"{context} must contain only unique named fields")
        result[match.group(1)] = match.group(2)
    return result


def _markdown_inline_list(value: str, context: str) -> tuple[str, ...]:
    match = re.fullmatch(r"\[([^\[\]]+)\]", value)
    if match is None:
        raise GuardViolation(f"{context} must be an inline list")
    items = tuple(item.strip() for item in match.group(1).split(","))
    if not all(items) or len(items) != len(set(items)):
        raise GuardViolation(f"{context} must contain unique non-empty items")
    return items


def _p0_fields(markdown: str, source: str) -> dict[str, Any]:
    """Extract only the explicitly guarded P0 fields from Markdown frontmatter."""

    lines = _frontmatter_lines(markdown, source)
    stage_lines = _markdown_section(lines, "p1r_stage_1", source)
    capability_lines = _markdown_section(lines, "capability_status", source)
    return {
        "source_action_status": _named_markdown_field(
            lines, "source_action_status", source
        ),
        "status": _named_markdown_field(lines, "status", source),
        "owner_execution_status": _named_markdown_field(
            lines, "owner_execution_status", source
        ),
        "capability_status": _markdown_mapping(
            capability_lines, f"{source} capability_status"
        ),
        "blocked_by": _markdown_inline_list(
            _named_markdown_field(lines, "blocked_by", source),
            f"{source} blocked_by",
        ),
        "p1r_stage_1": {
            "status": _named_markdown_field(
                stage_lines, "status", f"{source} p1r_stage_1", indent=2
            ),
            "acceptance_record": _named_markdown_field(
                stage_lines,
                "acceptance_record",
                f"{source} p1r_stage_1",
                indent=2,
            ),
        },
    }


def _deferred_status(markdown: str, entry_id: int) -> str:
    """Read a DW status as Markdown fields without treating Markdown as YAML."""

    lines = markdown.splitlines()
    heading = re.compile(rf"^### DW-{entry_id}:")
    starts = [index for index, line in enumerate(lines) if heading.match(line)]
    if len(starts) != 1:
        raise GuardViolation(f"deferred-work must contain exactly one DW-{entry_id}")
    start = starts[0]
    end = next(
        (index for index in range(start + 1, len(lines)) if lines[index].startswith("### DW-")),
        len(lines),
    )
    matches = [
        match.group(1)
        for line in lines[start + 1 : end]
        if (match := re.fullmatch(r"status:\s*([A-Za-z][A-Za-z-]*)\s*", line))
    ]
    if len(matches) != 1:
        raise GuardViolation(f"DW-{entry_id} must contain exactly one status field")
    return matches[0]


def _validate_closure_boundary(
    sprint: dict[str, Any],
    deferred_work: str,
    p0_frontmatter: dict[str, Any],
    accepted: bool,
) -> None:
    expected_gate_status = "done" if accepted else "open"
    actions = _action_map(sprint)
    for item_id, (expected_epic, configured_status) in ACTION_STATUSES.items():
        action = actions[item_id]
        expected_status = expected_gate_status if item_id == "6.1-P1R" else configured_status
        if action.get("epic") != expected_epic or action.get("status") != expected_status:
            raise GuardViolation(
                f"action item {item_id} must remain Epic {expected_epic} / {expected_status}"
            )

    p0_stages = _mapping(actions["6.1-P0"].get("stage_status"), "6.1-P0.stage_status")
    _exact_keys(
        p0_stages,
        ("stage_1_p1r_baseline", "stages_2_through_7"),
        "6.1-P0.stage_status",
    )
    if p0_stages["stage_1_p1r_baseline"] != expected_gate_status:
        raise GuardViolation("6.1-P0 Stage 1 must transition atomically with P1R")
    if p0_stages["stages_2_through_7"] != "open":
        raise GuardViolation("6.1-P0 stages 2 through 7 must remain open")

    if p0_frontmatter.get("source_action_status") != "open":
        raise GuardViolation("P0 source action must remain open")
    if p0_frontmatter.get("status") != "handed-off":
        raise GuardViolation("P0 artifact must remain handed-off")
    if p0_frontmatter.get("owner_execution_status") != "in-progress":
        raise GuardViolation("P0 owner execution must remain in-progress")
    if p0_frontmatter.get("capability_status") != P0_CAPABILITY_STATUS:
        raise GuardViolation("P0 capability_status must remain at the current boundary")
    if p0_frontmatter.get("blocked_by") != P0_BLOCKED_BY:
        raise GuardViolation("P0 blocked_by must remain at the current boundary")
    p0_stage = _mapping(p0_frontmatter.get("p1r_stage_1"), "P0 p1r_stage_1")
    if p0_stage.get("status") != expected_gate_status:
        raise GuardViolation("P0 artifact Stage 1 must transition atomically with P1R")
    if p0_stage.get("acceptance_record") != ACCEPTANCE_RECORD_RELATIVE_PATH.as_posix():
        raise GuardViolation("P0 artifact must name the fixed P1R acceptance record")

    for entry_id in (35, 68):
        actual = _deferred_status(deferred_work, entry_id)
        if actual != expected_gate_status:
            raise GuardViolation(f"DW-{entry_id} must be {expected_gate_status}; found {actual}")


def _validated_index_entries(
    sprint_status_path: Path = DEFAULT_SPRINT_STATUS,
    deferred_work_path: Path = DEFAULT_DEFERRED_WORK,
    p0_artifact_path: Path = DEFAULT_P0_ARTIFACT,
    *,
    workspace_root: Path = PROJECT_ROOT,
) -> list[tuple[str, str]]:
    """Validate one parsed scheduling and acceptance snapshot."""

    sprint_text = _read_text(sprint_status_path, "sprint status")
    sprint = _load_yaml_text(sprint_text, str(sprint_status_path))
    deferred = _read_text(deferred_work_path, "deferred-work ledger")
    p0_text = _read_text(p0_artifact_path, "P0 artifact")
    p0_frontmatter = _p0_fields(p0_text, str(p0_artifact_path))

    entries = _validate_scheduling(sprint)
    _validate_gate_coordinates(sprint)
    record_path = workspace_root / ACCEPTANCE_RECORD_RELATIVE_PATH
    record = _load_acceptance_record(record_path)
    if record is not None:
        _validate_acceptance_record(record)
    _validate_closure_boundary(sprint, deferred, p0_frontmatter, record is not None)
    return entries


def validate_index(
    sprint_status_path: Path = DEFAULT_SPRINT_STATUS,
    deferred_work_path: Path = DEFAULT_DEFERRED_WORK,
    p0_artifact_path: Path = DEFAULT_P0_ARTIFACT,
    *,
    workspace_root: Path = PROJECT_ROOT,
) -> tuple[int, ...]:
    """Validate scheduling plus the optional fixed-path acceptance record."""

    _validated_index_entries(
        sprint_status_path,
        deferred_work_path,
        p0_artifact_path,
        workspace_root=workspace_root,
    )
    return EXPECTED_PRODUCTION_EPICS


def requested_story_parts(story_id: str) -> tuple[int, int]:
    """Parse a story identifier accepted by BMad entry points."""

    numeric = re.fullmatch(
        r"[ \t]*(?:story[ \t]+)?(\d+)\.(\d+)[ \t]*",
        story_id,
        flags=re.IGNORECASE,
    )
    tracking = re.fullmatch(
        r"[ \t]*(\d+)-(\d+)-[a-z0-9]+(?:-[a-z0-9]+)*[ \t]*",
        story_id,
        flags=re.IGNORECASE,
    )
    match = numeric or tracking
    if match is None:
        raise GuardViolation(f"invalid story identifier {story_id!r}")
    return int(match.group(1)), int(match.group(2))


def assert_story_allowed(story_id: str) -> tuple[int, int]:
    """Reject stories outside the approved production-authority epics."""

    epic, story = requested_story_parts(story_id)
    if epic in HISTORICAL_EPICS:
        raise GuardViolation(
            f"Story {epic}.{story} belongs to immutable implementation history"
        )
    if epic not in EXPECTED_PRODUCTION_EPICS:
        raise GuardViolation(
            f"Story {epic}.{story} is outside production authority {list(EXPECTED_PRODUCTION_EPICS)}"
        )
    return epic, story


def validate_story_request(
    story_id: str,
    sprint_status_path: Path = DEFAULT_SPRINT_STATUS,
    deferred_work_path: Path = DEFAULT_DEFERRED_WORK,
    p0_artifact_path: Path = DEFAULT_P0_ARTIFACT,
    *,
    workspace_root: Path = PROJECT_ROOT,
) -> tuple[int, int]:
    """Validate both the current index and a requested production story."""

    entries = _validated_index_entries(
        sprint_status_path,
        deferred_work_path,
        p0_artifact_path,
        workspace_root=workspace_root,
    )
    epic, story = assert_story_allowed(story_id)
    matches = [key for key, _ in entries if key.startswith(f"{epic}-{story}-")]
    if len(matches) != 1:
        raise GuardViolation(
            f"Story {epic}.{story} must resolve to exactly one development_status key"
        )
    return epic, story


def build_parser() -> argparse.ArgumentParser:
    """Build the command-line parser."""

    parser = argparse.ArgumentParser(description=__doc__)
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument("--validate-index", action="store_true")
    group.add_argument("--story-id")
    parser.add_argument("--sprint-status", type=Path, default=DEFAULT_SPRINT_STATUS)
    parser.add_argument("--deferred-work", type=Path, default=DEFAULT_DEFERRED_WORK)
    parser.add_argument("--p0-artifact", type=Path, default=DEFAULT_P0_ARTIFACT)
    parser.add_argument("--workspace-root", type=Path, default=PROJECT_ROOT)
    return parser


def main() -> int:
    """Run the requested fail-closed validation."""

    arguments = build_parser().parse_args()
    try:
        if arguments.validate_index:
            authority = validate_index(
                arguments.sprint_status,
                arguments.deferred_work,
                arguments.p0_artifact,
                workspace_root=arguments.workspace_root,
            )
            print(f"PASS: production-authority index is {list(authority)}")
        else:
            epic, story = validate_story_request(
                arguments.story_id,
                arguments.sprint_status,
                arguments.deferred_work,
                arguments.p0_artifact,
                workspace_root=arguments.workspace_root,
            )
            print(f"PASS: Story {epic}.{story} is within production authority")
    except GuardViolation as error:
        print(f"BLOCKED: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
