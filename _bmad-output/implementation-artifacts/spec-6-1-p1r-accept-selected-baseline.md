---
title: '6.1-P1R Accept the selected EventStore and Builds baseline'
type: 'chore'
created: '2026-09-22'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/spec-6-1-p1r-minimal-acceptance-gate.md'
  - '{project-root}/_bmad-output/specs/spec-6-1-p1r-revalidation-owner-acceptance/qualification-contract.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The minimal P1R guard is implemented and verified, but the selected EventStore/Builds baseline remains open because the fixed acceptance record has no explicit decisions from the four accountable roles.

**Approach:** If and only if every role explicitly accepts the exact selected tuple, create the fixed JSON record and atomically close the five permitted representations of P1R. Generate no new qualification evidence and leave every downstream gate unchanged.

**Decisions:** On 2026-09-22, Jérôme Piquot stated, “I Jérôme Piquot have all roles and accept 1, 2 , 3 , 4.” This explicitly accepts the exact selected tuple as EventStore Owner, Builds Owner, Solution Architect, and Test Architect. Each decision is bound to `_bmad-output/implementation-artifacts/6-1-p1r-acceptance.json` and `#/selected`.

## Boundaries & Constraints

**Always:** Bind acceptance to selected EventStore `3.106.0` / `v3.106.0` / `76051c70cbf868c40edc00ca0344fa5bd8879b69` and Builds `ad52f350a2f0bc47849179ae17b4594dafff5363`; retain rollback EventStore `3.70.1` / `v3.70.1` / `f13f9925fdca53efa2ab8c90d396ab106f91bb9c` and Builds `7af20f8bafbfe561df6f7705913a0800603090b5`. Record a canonical UTC timestamp and a named, explicit decision for each required role.

**Never:** Do not infer a role decision from this build request, fabricate an approver, create a partial/placeholder record, add qualification artifacts, change product/runtime code, or close P0 stages 2–7, P2, P3, P4, Story 6.1, readiness, or dependent Epic 7/8 work.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Four accepts | Each role explicitly accepts with a named approver | Fixed JSON plus all five permitted closure sites validate atomically | Any mismatch blocks the transition |
| Missing/rejected role | At least one decision absent or not `accept` | No record or status change | Report the blocking role |
| Coordinate mismatch | Decision targets another tuple | No record or status change | Fail closed with the mismatched coordinate |
| Partial closure | Record or any closure site differs | Guard rejects the candidate | Restore one coherent open or accepted state |

</frozen-after-approval>

## Code Map

- `tools/planning/validate_production_authority.py` -- exact schema, tuple, role, timestamp, atomic-closure, and downstream non-closure authority; do not modify.
- `tests/tools/test_production_authority_guard.py` -- 18-case guard suite; do not modify.
- `_bmad-output/implementation-artifacts/6-1-p1r-acceptance.json` -- fixed new acceptance record.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- close only action `6.1-P1R` and P0 `stage_1_p1r_baseline`.
- `_bmad-output/implementation-artifacts/6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md` -- close only `p1r_stage_1.status`.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- close only DW-35 and DW-68.

## Tasks & Acceptance

**Execution:**
- [x] Record all four explicit human decisions verbatim in this spec; stop without repository mutation if any role does not accept.
- [ ] Create `6-1-p1r-acceptance.json` with the exact schema, tuples, timestamp, fixed record path, `#/selected` references, and named approvers.
- [ ] Update the five permitted status sites as one candidate transition and leave all downstream state unchanged.
- [ ] Run the focused guard suite and active-index validation; keep P1R open if either fails.

**Acceptance Criteria:**
- Given four explicit accepts, when the transition is applied, then the fixed record and all five closure sites pass the guard as one accepted state.
- Given the accepted P1R state, when the index is validated, then P0 stages 2–7, P2/P3/P4, Story 6.1, readiness, and Epics 7/8 remain open or blocked.
- Given any absent/rejected decision or invalid record field, when validation runs, then P1R remains open without partial acceptance.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests/tools/test_production_authority_guard.py -v` -- expected: 18 tests pass.
- `python3 tools/planning/validate_production_authority.py --validate-index` -- expected: accepted index passes while downstream gates remain open.
- `git diff --check` -- expected: no whitespace errors.
