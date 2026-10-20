---
title: 'Validate the current 6.1-P0 handoff boundary'
type: 'bugfix'
created: '2026-10-20'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Fix `tools/planning/validate_production_authority.py` to validate the recorded
6.1-P0 implemented-but-unaccepted handoff. Accept the versioned capability value
`historical-4.29.1-v1-no-current-v2-adoption` and reconcile the expected capabilities
and blockers with the existing handoff. Preserve duplicate-field rejection, strict
acceptance requirements, and blocked downstream stories. Add focused positive and
negative regression coverage and run both `--story-id 6.4` and `--validate-index`.
Do not change acceptance, sprint status, or historical evidence.

</frozen-after-approval>

## Implementation Notes

- This is validator maintenance, not implementation or scheduling of an epic story;
  no sprint synchronization applies. No intent gaps or irreversible actions exist.
- Limit implementation changes to the validator and its existing unittest suite.
  Permit dots in capability values while retaining full-line parsing and unique
  keys. Match the handoff's exact capability map and ordered blocker list.
- Both requested CLI commands initially exit 1 with `capability_status must
  contain only unique named fields` because the value grammar rejects `4.29.1`.
- Retain existing P1R schema, fixed record path, selected and rollback tuples,
  four-role decisions, atomic closure checks, and downstream scheduling locks.
- Verify with the focused unittest suite, both requested CLI commands, and
  `git diff --check`. Leave changes uncommitted as required by repository guidance.
- Implemented the exact current capability map and seven ordered blockers; added
  dotted-value parsing without changing full-line matching or duplicate checks.
- Added positive handoff coverage and negative controls for capability advancement
  and regression, changed tool versions, missing/extra capability keys, duplicate
  fields, trailing content, removed/reordered blockers, and Story 6.4 dispatch.
  Capability and blocker mutation controls run before and after P1R acceptance.
- Final verification: `python3 -m unittest
  tests/tools/test_production_authority_guard.py -v` passes all 27 tests;
  `python3 tools/planning/validate_production_authority.py --story-id 6.4` and
  `python3 tools/planning/validate_production_authority.py --validate-index` pass.
  Protected acceptance, sprint, handoff, and historical evidence paths have no diff.

## Review Triage Log

- Low, patched: mutation controls initially covered only the open P1R fixture.
  Repeated capability and blocker mutations with valid P1R acceptance; all fail
  at their specific P0 boundary checks.
- Low, patched: added a syntactically valid `4.29.2` tool-version mutation; the
  exact expected capability boundary rejects it.
- Low, patched: added missing and unknown unique capability-key mutations; the
  exact capability inventory rejects both. No review findings remain deferred.
