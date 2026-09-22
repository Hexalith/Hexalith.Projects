---
title: '6.1-P1R Accept the selected EventStore and Builds baseline'
type: 'chore'
created: '2026-09-22'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '3f12e4c329a8b46a8e69397635ba290388923c75'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/spec-6-1-p1r-minimal-acceptance-gate.md'
  - '{project-root}/_bmad-output/specs/spec-6-1-p1r-revalidation-owner-acceptance/qualification-contract.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The minimal P1R guard is implemented and verified, but the selected EventStore/Builds baseline remains open because the fixed acceptance record has no explicit decisions from the four accountable roles.

**Approach:** If and only if every role explicitly accepts the exact selected tuple, create the fixed JSON record and atomically close the five permitted representations of P1R. Generate no new qualification evidence and leave every downstream gate unchanged.

**Decisions:** On 2026-09-22, Jérôme Piquot stated, “I Jérôme Piquot have all roles and accept 1, 2 , 3 , 4.” This explicitly accepts the exact selected tuple as EventStore Owner, Builds Owner, Solution Architect, and Test Architect. Each decision is bound to `_bmad-output/implementation-artifacts/6-1-p1r-acceptance.json` and `#/selected`.

After the first fail-closed implementation attempt exposed state-dependent test fixtures, Jérôme Piquot explicitly authorized amending this frozen boundary. The focused guard tests may be changed only to seed their open-state fixtures independently of the live accepted files; the production guard, test expectations, scenario coverage, and acceptance boundary remain unchanged.

## Boundaries & Constraints

**Always:** Bind acceptance to selected EventStore `3.106.0` / `v3.106.0` / `76051c70cbf868c40edc00ca0344fa5bd8879b69` and Builds `ad52f350a2f0bc47849179ae17b4594dafff5363`; retain rollback EventStore `3.70.1` / `v3.70.1` / `f13f9925fdca53efa2ab8c90d396ab106f91bb9c` and Builds `7af20f8bafbfe561df6f7705913a0800603090b5`. Record a canonical UTC timestamp and a named, explicit decision for each required role. Keep the production guard unchanged and make the focused suite's open-state fixture independent of the live repository state.

**Never:** Do not infer a role decision from this build request, fabricate an approver, create a partial/placeholder record, add qualification artifacts, change the production guard, alter test expectations or scenario coverage, change product/runtime code, or close P0 stages 2–7, P2, P3, P4, Story 6.1, readiness, or dependent Epic 7/8 work.

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
- `tests/tools/test_production_authority_guard.py` -- 18-case guard suite; change only fixture seeding so open and accepted scenarios do not depend on the live repository state.
- `_bmad-output/implementation-artifacts/6-1-p1r-acceptance.json` -- fixed new acceptance record.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- close only action `6.1-P1R` and P0 `stage_1_p1r_baseline`.
- `_bmad-output/implementation-artifacts/6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md` -- close only `p1r_stage_1.status`.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- close only DW-35 and DW-68.

## Tasks & Acceptance

**Execution:**
- [x] Record all four explicit human decisions verbatim in this spec; stop without repository mutation if any role does not accept.
- [x] Make the focused suite seed its canonical open fixtures independently of the live repository state without changing expectations or coverage.
- [x] Create `6-1-p1r-acceptance.json` with the exact schema, tuples, timestamp, fixed record path, `#/selected` references, and named approvers.
- [x] Update the five permitted status sites as one candidate transition and leave all downstream state unchanged.
- [x] Run the focused guard suite and active-index validation; keep P1R open if either fails.

**Acceptance Criteria:**
- Given four explicit accepts, when the transition is applied, then the fixed record and all five closure sites pass the guard as one accepted state.
- Given the live repository is accepted, when the focused suite runs, then all 18 existing tests still pass while exercising both open and accepted fixture states.
- Given the accepted P1R state, when the index is validated, then P0 stages 2–7, P2/P3/P4, Story 6.1, readiness, and Epics 7/8 remain open or blocked.
- Given any absent/rejected decision or invalid record field, when validation runs, then P1R remains open without partial acceptance.

## Implementation Notes

- The focused suite now normalizes only its canonical open-state fixture inputs
  when loading live files, so accepted and open scenarios remain independent of
  the repository's accepted state. Test expectations and scenario coverage are
  unchanged.
- Created the fixed record with canonical timestamp `2026-09-22T17:11:38Z`
  and Jérôme Piquot as the named approver for all four explicitly accepted roles.
- Closed the P1R action, both P0 Stage 1 representations, DW-35, and DW-68.
  The production-authority guard and every downstream gate remain unchanged.

## Spec Change Log

- 2026-09-22: Attempted the accepted P1R transition, then restored the fixed
  record-absent open state because both mandated validations cannot pass on the
  same working tree without modifying prohibited test infrastructure.
- 2026-09-22: Applied the explicitly authorized fixture-seeding amendment and
  accepted the selected P1R baseline after both mandated validations passed.

## Review Triage Log

- `false` — blind-hunter: The P0 sentence at line 167 is a conditional rule
  (remain open until the record validates), not a claim that the condition is
  still unmet; its Stage 1 frontmatter now records the accepted state.
- `medium` — blind-hunter: The active companion P1R specification still says
  the record is absent and the gate is open, which now misdirects readers of
  the current acceptance contract.
- `low` — blind-hunter: DW-35 and DW-68 are marked done but retain only their
  original reasons and no resolution field, unlike resolved ledger entries;
  add a dated resolution tied to the fixed record.
- `medium` — blind-hunter: The new fixture normalizer searches all text after a
  marker, so a missing target field can cause it to select an unrelated later
  status; bound the lookup to the marked YAML or Markdown section.
- `false` — blind-hunter: The authorization requires independence from the
  live P1R acceptance state, not from every unrelated live planning field;
  consuming the current file shape while normalizing the five P1R fields meets
  that requirement and preserves integration coverage.
- `false` — blind-hunter: The cited P0 prose is conditional and the production
  guard deliberately treats only the named frontmatter field as the atomic
  representation, so the accepted tests do not bless contradictory current
  P1R state.
- `medium` — edge-case-hunter: The normalizer does not constrain a status
  lookup to the marker's section, allowing a later record's status to be
  rewritten if the intended field disappears.
- `low` — edge-case-hunter: Substring matching accepts prefixes such as
  `done-extra`; exact line matching is needed so malformed fixture state fails
  loudly instead of being partially normalized.
- `false` — edge-case-hunter: The reviewed claim is acceptance-state
  independence, demonstrated by the suite passing against the accepted live
  files; it does not promise static fixtures isolated from all repository
  evolution.
- `low` — verification-gap: A coherent reversal of the record plus all five
  closure sites remains green because the minimal guard intentionally supports
  both open and accepted states. Such a coordinated six-file reversal is an
  explicit policy change, not an incidental regression, and pinning the reusable
  suite to accepted-only state would alter the frozen scenario contract.
- `medium` — verification-gap other: The qualification-evidence README still
  says P1R is open and no record exists, contradicting the accepted live state.

## Verification

**Commands:**
- `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests/tools/test_production_authority_guard.py -v` -- expected: 18 tests pass.
- `python3 tools/planning/validate_production_authority.py --validate-index` -- expected: accepted index passes while downstream gates remain open.
- `git diff --check` -- expected: no whitespace errors.

**Observed:**
- The focused suite passed all 18 tests against the accepted live repository
  while continuing to exercise both open and accepted fixture states.
- Active-index validation passed with production-authority epics `[6, 7, 8]`.
- `git diff --check` reported no whitespace errors.
