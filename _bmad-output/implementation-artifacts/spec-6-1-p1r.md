---
title: '6.1-P1R Qualify and Accept the Current EventStore Baseline'
type: 'bugfix'
created: '2026-09-20'
status: 'done'
disposition: 'superseded'
superseded_by: 'spec-6-1-p1r-minimal-acceptance-gate.md'
route: 'dispatch'
review_loop_iteration: 5
baseline_commit: 'c3b8c4a1cf24fe88ca4e124d0adebecb8bd88d07'
context:
  - '{project-root}/_bmad-output/specs/spec-6-1-p1r-revalidation-owner-acceptance/qualification-contract.md'
---

# 6.1-P1R Current State

The former multi-run qualification flow is retired. P1R now uses the minimal
acceptance boundary defined by
`spec-6-1-p1r-minimal-acceptance-gate.md`.

## Coordinates

- Selected EventStore: `3.106.0` / `v3.106.0` /
  `76051c70cbf868c40edc00ca0344fa5bd8879b69`.
- Selected Builds:
  `ad52f350a2f0bc47849179ae17b4594dafff5363`.
- Rollback EventStore: `3.70.1` / `v3.70.1` /
  `f13f9925fdca53efa2ab8c90d396ab106f91bb9c`.
- Rollback Builds:
  `7af20f8bafbfe561df6f7705913a0800603090b5`.

## Acceptance

The optional fixed record is
`_bmad-output/implementation-artifacts/6-1-p1r-acceptance.json`, using schema
`hexalith.projects.p1r-acceptance.v1`.

No valid record exists, so P1R, P0 Stage 1, DW-35, and DW-68 remain open. P0
stages 2–7, P2, P3, P4, Story 6.1, readiness, and dependent Epic 7/8 work remain
open or blocked.

A future record must contain the exact selected and rollback tuples, a canonical
UTC timestamp, and explicit `accept` decisions from the EventStore Owner,
Builds Owner, Solution Architect, and Test Architect. Each decision names its
approver, the exact record path, and `#/selected`.

## Verification

```bash
PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests/tools/test_production_authority_guard.py -v
python3 tools/planning/validate_production_authority.py --validate-index
```
