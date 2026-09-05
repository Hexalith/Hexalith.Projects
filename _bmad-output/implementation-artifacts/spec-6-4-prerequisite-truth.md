---
title: 'Story 6.4 prerequisite truth'
type: 'chore'
created: '2026-09-05'
status: 'done'
baseline_revision: 'f54de1411d9a610e60a8620c8110fafd8ef36aaa'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '_bmad-output/implementation-artifacts/epic-6-context.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** The blocked Story 6.4 implementation artifact omits mandatory prerequisite `6.1-P1R` from its prerequisite metadata and non-implementation entry condition. It also describes EventStore `3.86.0` as the current central pin even though the approved current observation is an unaccepted `3.102.0` catalog paired with a stale `3.90.0` runner, while `3.70.1` remains the accepted rollback baseline.

**Approach:** Correct only Story 6.4's governance and version-observation prose so future agents see the exact prerequisite chain and current candidate/runner split. Preserve the story's blocked state and all implementation scope.

## Boundaries & Constraints

**Always:** Add `6.1-P1R` explicitly to both `blocked_by` and the named non-implementation entry condition; describe `3.102.0` as current but unaccepted catalog state, `3.90.0` as stale runner state, and `3.70.1` as the accepted rollback baseline; keep both machine-readable and rendered Story 6.4 status values `blocked`.

**Never:** Do not edit `_bmad-output/implementation-artifacts/deferred-work.md`, the bundle intent, planning artifacts, package/source pins, submodules, story tasks beyond the affected governance statements, or any source/test code. Do not imply that P1R or the `3.102.0` candidate is accepted, and do not unblock implementation.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Missing prerequisite | Story `blocked_by` and entry condition omit `6.1-P1R` | Both locations explicitly include `6.1-P1R` in dependency order | Preserve `status: blocked` and `Status: blocked` |
| Stale package observation | Story calls `3.86.0` the current EventStore pin | Both stale observations identify unaccepted catalog `3.102.0`, stale runner `3.90.0`, and accepted rollback `3.70.1` | Do not collapse candidate, runner, and rollback into one accepted baseline |
| Historical ledger wording | Ledger entry mentions older `3.88.0`/`3.97.0` decisions | Follow the bundle's current Intent section and verified checkout state | Leave the ledger verbatim and unedited |

</intent-contract>

## Code Map

- `.bmad-loop/runs/20260905-175129-b379/bundles/story-6-4-prerequisite-truth/intent.md` -- authoritative bundle intent and verbatim DW-30/DW-31 history; read-only.
- `_bmad-output/implementation-artifacts/6-4-resolve-projects-with-transient-current-explanations.md:7` -- machine-readable blocked status that must remain unchanged; line 8 is the `blocked_by` list requiring `6.1-P1R`.
- `_bmad-output/implementation-artifacts/6-4-resolve-projects-with-transient-current-explanations.md:46` -- non-implementation entry condition requiring an explicit P1R prerequisite; lines 50 and 136 contain the two stale `3.86.0` observations.
- `_bmad-output/implementation-artifacts/epic-6-context.md` -- current cross-story dependency statement requiring accepted P0, historical P1, current P1R, P2, P3, and P4 for later read consumers.
- `_bmad-output/implementation-artifacts/spec-6-1-p1r-revalidate-baseline-3-102-0.md:43` -- current revalidation observation: EventStore candidate `3.102.0`, stale runner `3.90.0`, accepted rollback `3.70.1`; P1R remains blocked.
- `references/Hexalith.Builds/Props/Directory.Packages.props:8` and `references/Hexalith.Builds/src/libraries/Hexalith.Builds.Tooling/Manifest/SupportedPlatformPins.cs:14` -- read-only checkout evidence for the `3.102.0` catalog / `3.90.0` runner split.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- orchestrator-owned resolution ledger; must not be edited.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad-output/implementation-artifacts/6-4-resolve-projects-with-transient-current-explanations.md` -- add P1R to `blocked_by` and the non-implementation entry condition, and replace both `3.86.0` observations with the exact unaccepted catalog/stale runner/accepted rollback distinction while preserving blocked status.

**Acceptance Criteria:**
- Given the Story 6.4 artifact, when its prerequisite metadata and entry condition are inspected, then both explicitly require accepted `6.1-P1R` and the existing surrounding gates remain intact.
- Given the two former `3.86.0` observations, when the artifact is inspected after editing, then each reflects the current unaccepted `3.102.0` catalog versus stale `3.90.0` runner state and retains `3.70.1` as the accepted rollback baseline.
- Given the governance-only scope, when the final diff is reviewed, then Story 6.4 remains blocked and neither the deferred-work ledger nor package pins, planning artifacts, source code, tests, or submodules changed.

## Spec Change Log

## Review Triage Log

### 2026-09-05 — Review pass
- verdicts: 8 findings — high 0, medium 1, low 2, false 5, maybe-false 0
- findings:
  - `[false]` `[reject]` Add `G-4` to `blocked_by` — `6.1-P0` is the supported G-4 runner/evidence prerequisite, while `entry_gate` and the entry-condition prose already name the G-4 capability explicitly; no dependency is lost.
  - `[false]` `[reject]` Explicitly repeat Solution Architect conformance, Story 6.1 readiness, and the independent readiness rerun in the entry-condition sentence — the unchanged `entry_gate` requires the exact-current Epic 6 shared baseline, whose loaded context defines those gates, and the story separately retains its conformance/readiness constraints.
  - `[medium]` `[patch]` Historical P1 was grammatically subjected to exact-current revision acceptance — split the sentence so it now states that historical `6.1-P1` evidence is already accepted and applies exact-current acceptance only to P0, P1R, and P2 through P4.
  - `[false]` `[reject]` Calling `3.70.1` the accepted rollback baseline conceals incomplete current rollback qualification — that label is verbatim bundle intent, while the artifact keeps P1R and Story 6.4 blocked and does not claim current executable rollback proof passed.
  - `[false]` `[reject]` Add observation revisions and gitlinks beside the new version prose — the story already directs the accepted Epic 6 gate evidence to record exact gitlinks, avoiding another drifting source of truth, and its retained `baseline_commit` remains correctly scoped to story creation.
  - `[low]` `[reject]` The story-only verification command does not independently prove the Builds catalog/runner values — the checkout sources were directly verified during this run, and the proposed change would only edit this build's spec rather than the delivered Story 6.4 artifact.
  - `[low]` `[reject]` The broad `rg` verification command can succeed without enforcing counts or absence — separate exact count, negative, and source assertions ran successfully; changing the recorded command would only edit this build's spec.
  - `[false]` `[reject]` The protected-path command does not prove the complete allowlist — the full baseline-to-working-tree diff contains exactly the Story 6.4 artifact and this workflow spec, and direct allowlist plus ledger/planning/source/test/submodule assertions passed.

## Verification

**Commands:**
- `rg -n 'status: blocked|Status: blocked|blocked_by|6\.1-P1R|3\.102\.0|3\.90\.0|3\.70\.1|3\.86\.0' _bmad-output/implementation-artifacts/6-4-resolve-projects-with-transient-current-explanations.md` -- expected: both blocked markers remain, P1R appears in both required locations, the three current truth versions appear, and `3.86.0` does not.
- `git diff --check` -- expected: no whitespace errors.
- `git diff --name-only -- _bmad-output/implementation-artifacts/deferred-work.md references src tests` -- expected: no output.

## Auto Run Result

Status: done

### Summary

Corrected Story 6.4's prerequisite and EventStore observation truth without changing its implementation scope or blocked disposition. Historical `6.1-P1` remains distinguished from the exact-current P0/P1R/P2-P4 acceptance chain.

### Files Changed

- `_bmad-output/implementation-artifacts/6-4-resolve-projects-with-transient-current-explanations.md` -- added explicit P1R prerequisites and replaced both stale `3.86.0` claims with the unaccepted `3.102.0` catalog, stale `3.90.0` runner, and accepted `3.70.1` rollback baseline.
- `_bmad-output/implementation-artifacts/spec-6-4-prerequisite-truth.md` -- recorded the build-auto intent, investigation, acceptance checks, review triage, and completion result.

### Review Findings

- Patches applied: 1 medium, 0 high, 0 low. The entry condition now separates already-accepted historical P1 evidence from exact-current acceptance of P0, P1R, and P2-P4.
- Items deferred: 0.
- Rejected: adding `G-4` to `blocked_by` because P0 owns that runner and G-4 is already explicit in the gate; repeating all shared-baseline gates because the loaded Epic 6 baseline and retained story constraints already define them; weakening the bundle-mandated accepted `3.70.1` label because no current proof is claimed; adding duplicate observation revisions because exact gitlinks belong in accepted gate evidence; adding source checks to this spec because sources were directly verified and spec-only review fixes are rejected; rewriting the broad `rg` because exact count/negative assertions separately passed; expanding the protected-path command because the full diff and exact allowlist already proved only two expected files changed.
- Follow-up review recommendation: `false` — patched entries were high 0, medium 1, low 0, below the first-pass threshold.

### Verification

- Confirmed exactly one machine-readable and one rendered blocked marker, explicit P1R in `blocked_by` and the entry condition, both corrected observation statements, and no remaining `3.86.0` in Story 6.4.
- Confirmed the live checkout sources report catalog `3.102.0` and runner `3.90.0`.
- `git diff --check` and the full baseline diff whitespace check passed.
- Exact changed-path allowlisting and protected ledger/planning/source/test/submodule checks passed; the deferred-work ledger is unchanged.
- Matrix audit passed for prerequisite correction, version truth, blocked-state preservation, and ledger immutability. No source test suite was run because the delivered change is documentation-only.

### Residual Risks

Story 6.4 intentionally remains blocked until its existing prerequisite and owner-acceptance gates are satisfied. No implementation readiness or P1R acceptance is implied by this correction.
