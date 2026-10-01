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

- Selected: EventStore `3.110.0`, tag `v3.110.0`, revision
  `27279fe6431925a6ea046c3f89af61487185c7de`; Builds revision
  `21ce044ab465ccb2adab58b3d66e394ffbecf3c2`.
- Rollback: EventStore `3.70.1`, tag `v3.70.1`, revision
  `f13f9925fdca53efa2ab8c90d396ab106f91bb9c`; Builds revision
  `7af20f8bafbfe561df6f7705913a0800603090b5`.

## Acceptance Boundary

The fixed acceptance record is present and valid, so P1R is accepted. It
contains `accept` decisions for the EventStore Owner, Builds Owner, Solution
Architect, and Test Architect. Each role names Jérôme Piquot as approver, the
exact record path, and the record's `selected` tuple, at
`2026-10-01T06:17:01Z`. The Solution Architect decision explicitly rebinds
the EventStore Stack to this tagged package coordinate with the owner packet's
recorded limitations.

The [2026-09-22 acceptance](../../implementation-artifacts/evidence/6-1-p1r-acceptance-20260922-3.106.0.json)
is retained as historical evidence; it is outside the fixed acceptance gate.
The stale G-6 packet is not accepted by these decisions. Current prerequisite
usability stays false, and already-done P1R/P0 Stage 1/DW-35/DW-68 statuses
remain done for the newly accepted tuple.

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
