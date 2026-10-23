---
title: 'Complete five deferred planning consistency items'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_revision: '97a8a86cc74c79e65e9096031c6288b78df802d9'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five deferred planning consistency obligations remain unreconciled: a duplicate DW-56 decision, superseded Story 6.1 baseline notes, a historical CI replay claim, a P0 missing-approval claim superseded by its approved continuation, and optional readiness metadata that can contradict the strict acceptance gate.

**Approach:** Complete these five root-owned items on main and push the verified commit as requested. Preserve dated history while adding precise current dispositions and references. Remove only the duplicate DW-56 decision. Make the scheduling guard reject non-false optional P1R/preserved-pending G-6 usability flags and contradictory accepted P1R candidate, timestamp or role metadata. Keep optional metadata optional and permit pending future P1R candidates to differ from the fixed accepted tuple. Exercise the real guard using temporary candidate workspaces, with independent negative controls and pending-future positive controls. Record all five resolutions, distinguishing prior fixes from new changes. Preserve fixed acceptance records, sprint status, package pins, submodule pointers and production qualification decisions.

</frozen-after-approval>

## Implementation Notes

- This is a small reversible root tooling/documentation slice with no unresolved intent gaps or new external API. The user explicitly requested five items and a main push; the one-shot route applies.
- Code map: tools/planning/validate_production_authority.py validates the atomic acceptance/index snapshot; its existing tests/tools/test_production_authority_guard.py supplies open/accepted candidate workspaces. Add optional consistency checks after strict acceptance validation.
- deferred-work.md contains all five obligations. Story 6.1 -2 is already marked superseded and no longer names 3.108.1. The current owner packet already has a tail clarification covering the selected 3.110.0 CI replay. Clarify these at their stale reading points and close them as reconciled prior fixes.
- The P0 handoff's approved October 20 continuation supersedes the older missing-approval observation. Preserve both dated observations, identify the later preparation boundary, and retain all adoption/qualification blockers.
- Baseline active index validation passed. The existing production-authority fixture suite is the focused validation lane; CI already runs it without submodule initialization.

- Baseline fixture suite: 28/29 passed; the pre-existing failure expected the obsolete `<candidate>` command placeholder. Updated that assertion to the existing complete candidate-workspace command without changing project guidance.
- Ran the new negative controls against the original guard loaded from HEAD: 36 expected failures, zero fixture errors. The final implementation rejects each contradictory state.
- The approved P0 preparation and selected CI replay already exist. Clarifications preserve dated historical statements and explicitly distinguish the routine selected-only CI lane from manually executable historical checks.

## Verification

- `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests/tools/test_production_authority_guard.py tests/tools/test_git_whitespace_policy.py -q` — 39/39 passed, zero failures/skips (5.407 seconds).
- `python3 tools/planning/validate_production_authority.py --validate-index` — passed, production authority remains `[6, 7, 8]`.
- `pwsh -NoProfile -File tests/tools/run-ci-workflow-gates.ps1` — passed across all five workflow files.
- `git diff --check` — passed. Existing LF/CRLF conventions are preserved; the new spec uses CRLF.

## Review Triage Log

- BH1 — medium, patched: absent, empty and unsupported status strings skipped consistency validation. Present P1R summaries now explicitly declare accepted/accepted-* or pending/pending-*; temporary-workspace controls reject missing, empty, done, approved, mixed-case, trailing-dash and numeric statuses.
- BH2 — medium, patched: pending successor status skipped historical acceptance summaries. Any present accepted timestamp or role decisions now require and match the fixed historical record, while pending candidate coordinates remain independent. Both record-present and record-absent negative controls cover the distinction.
- BH3 — medium, patched: a pending candidate could have an invalid container or coordinate type. Present candidates now require exactly the four coordinate keys and nonempty string values. List/null/empty/extra/missing-key and per-coordinate invalid-value controls fail closed; a distinct valid future tuple remains allowed.
- BH4 — medium, patched: the synthetic open fixture retained accepted summaries without a record. Open fixtures now omit accepted timestamp and role decisions; accepted fixtures restore them. Unauthorized open-summary claims have separate failing controls.
- BH5 — medium, patched: mismatch controls omitted the plain accepted status form. Candidate, timestamp and role mutations now run under both accepted and the repository's accepted-* status with a valid fixed record.

- Final focused verification after review fixes: 42/42 tests passed, zero failures/skips (9.642 seconds); active index validation and git diff --check passed. All five reported findings are patched; no review finding is deferred.

- The independent reviewer rechecked all five corrections and confirmed no unresolved findings; 37/37 guard tests and active-index validation passed independently. Commit message validation passed with commitlint 21.1.0 and the repository configuration.
