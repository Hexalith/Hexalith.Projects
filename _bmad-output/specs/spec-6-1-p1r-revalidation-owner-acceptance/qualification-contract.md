# 6.1-P1R Minimal Qualification Contract

## Record Location and Schema

The sole optional acceptance record is:

`_bmad-output/implementation-artifacts/6-1-p1r-acceptance.json`

Its schema identifier is `hexalith.projects.p1r-acceptance.v1`. The record must
contain exactly these root keys:

- `schema`
- `selected`
- `rollback`
- `timestamp_utc`
- `decisions`

No placeholder record is created while P1R is open.

## Coordinate Tuples

Both `selected` and `rollback` contain exactly
`eventstore_version`, `eventstore_tag`, `eventstore_revision`, and
`builds_revision`.

The selected tuple is EventStore `3.110.0` / `v3.110.0` /
`27279fe6431925a6ea046c3f89af61487185c7de` plus Builds
`21ce044ab465ccb2adab58b3d66e394ffbecf3c2`.

The rollback tuple is EventStore `3.70.1` / `v3.70.1` /
`f13f9925fdca53efa2ab8c90d396ab106f91bb9c` plus Builds
`7af20f8bafbfe561df6f7705913a0800603090b5`.

The sprint index carries the same tuples. Any mismatch fails acceptance.

Jérôme Piquot accepted this exact selected tuple in all four roles at
`2026-10-01T06:17:01Z`. This is P1R acceptance only: stale G-6 evidence keeps
current prerequisite usability false and does not advance downstream work.

## Timestamp and Decisions

`timestamp_utc` uses canonical UTC seconds:
`YYYY-MM-DDTHH:MM:SSZ`.

`decisions` contains exactly these keys:

- `EventStore Owner`
- `Builds Owner`
- `Solution Architect`
- `Test Architect`

Each decision contains exactly:

- `decision`: the literal `accept`
- `approver`: a non-empty name
- `record`: the exact fixed record path
- `selected`: the JSON Pointer `#/selected`

Missing, rejected, duplicated, or malformed decisions fail closed and identify
the invalid role.

## Canonical Non-Persisted Example

This example documents the exact shape only; it is not an acceptance record and
must not be persisted as a placeholder:

```json
{
  "schema": "hexalith.projects.p1r-acceptance.v1",
  "selected": {
    "eventstore_version": "3.110.0",
    "eventstore_tag": "v3.110.0",
    "eventstore_revision": "27279fe6431925a6ea046c3f89af61487185c7de",
    "builds_revision": "21ce044ab465ccb2adab58b3d66e394ffbecf3c2"
  },
  "rollback": {
    "eventstore_version": "3.70.1",
    "eventstore_tag": "v3.70.1",
    "eventstore_revision": "f13f9925fdca53efa2ab8c90d396ab106f91bb9c",
    "builds_revision": "7af20f8bafbfe561df6f7705913a0800603090b5"
  },
  "timestamp_utc": "2026-10-01T06:17:01Z",
  "decisions": {
    "EventStore Owner": {
      "decision": "accept",
      "approver": "EventStore Owner Name",
      "record": "_bmad-output/implementation-artifacts/6-1-p1r-acceptance.json",
      "selected": "#/selected"
    },
    "Builds Owner": {
      "decision": "accept",
      "approver": "Builds Owner Name",
      "record": "_bmad-output/implementation-artifacts/6-1-p1r-acceptance.json",
      "selected": "#/selected"
    },
    "Solution Architect": {
      "decision": "accept",
      "approver": "Solution Architect Name",
      "record": "_bmad-output/implementation-artifacts/6-1-p1r-acceptance.json",
      "selected": "#/selected"
    },
    "Test Architect": {
      "decision": "accept",
      "approver": "Test Architect Name",
      "record": "_bmad-output/implementation-artifacts/6-1-p1r-acceptance.json",
      "selected": "#/selected"
    }
  }
}
```

## State Transition

Record absent means the open state is required. Record present means the record
must validate and P1R, P0 Stage 1, DW-35, and DW-68 must close atomically. No
other status may advance. The guard reads sprint YAML through PyYAML, the record
through the Python standard library, and Markdown only through explicit
frontmatter or named Markdown fields.
