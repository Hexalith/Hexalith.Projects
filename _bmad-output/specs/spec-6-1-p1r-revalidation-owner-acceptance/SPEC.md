---
id: SPEC-6-1-p1r-revalidation-owner-acceptance
status: active
companions:
  - qualification-contract.md
sources:
  - ../../implementation-artifacts/spec-6-1-p1r-minimal-acceptance-gate.md
---

# 6.1-P1R Minimal Owner Acceptance

## Purpose

P1R selects one EventStore/Builds baseline and one rollback baseline. Acceptance is
represented only by the optional fixed JSON record documented in
`qualification-contract.md`. Repository history, workspaces, logs, packages,
command ledgers, and evidence bundles are not part of this gate.

## Selected and Rollback Coordinates

- Selected: EventStore `3.106.0`, tag `v3.106.0`, revision
  `76051c70cbf868c40edc00ca0344fa5bd8879b69`; Builds revision
  `ad52f350a2f0bc47849179ae17b4594dafff5363`.
- Rollback: EventStore `3.70.1`, tag `v3.70.1`, revision
  `f13f9925fdca53efa2ab8c90d396ab106f91bb9c`; Builds revision
  `7af20f8bafbfe561df6f7705913a0800603090b5`.

## Acceptance Boundary

The record is absent, so P1R remains open. A valid record must contain
`accept` decisions for the EventStore Owner, Builds Owner, Solution Architect,
and Test Architect. Each role names an approver, the exact record path, and the
record's `selected` tuple.

A valid record permits only these transitions:

- P1R: `open` to `done`.
- P0 Stage 1: `open` to `done`.
- DW-35 and DW-68: `open` to `done`.

P0 stages 2–7, P2, P3, P4, Story 6.1, implementation readiness, and dependent
Epic 7/8 work remain open or blocked.

## Validation

Run:

```bash
python3 tools/planning/validate_production_authority.py --validate-index
```

The guard fails closed for malformed JSON, unknown or missing fields, a coordinate
mismatch, a missing/rejected role, a missing approver, an incorrect record path or
selected-tuple reference, and any closure outside the permitted boundary.
