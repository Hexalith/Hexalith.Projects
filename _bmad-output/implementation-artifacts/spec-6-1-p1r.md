---
title: '6.1-P1R Qualify and Accept the Current EventStore Baseline'
type: 'bugfix'
created: '2026-09-20'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'c3b8c4a1cf24fe88ca4e124d0adebecb8bd88d07'
context:
  - '{project-root}/_bmad-output/specs/spec-6-1-p1r-revalidation-owner-acceptance/qualification-contract.md'
  - '{project-root}/references/Hexalith.Builds/DEVELOPMENT.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** 6.1-P1R is open: the explicit EventStore selection is `v3.103.0`, while Builds catalogs `3.106.0` but its runner corpus remains on `3.102.0`. No current candidate passed qualification and four-owner acceptance.

**Approach:** Select one published coordinate, align an immutable Builds candidate, run contract phases 1–5 with retained evidence and executable `3.70.1` rollback, then apply the chosen acceptance boundary.

**Decisions:** Supersede the recorded `v3.103.0` selection with published EventStore `v3.106.0` / `76051c70cbf868c40edc00ca0344fa5bd8879b69`, keeping untagged HEAD unselected. Complete full P1R closure after every gate passes. Jérôme Piquot is the authorized named approver for the EventStore Owner, Builds Owner, Solution Architect, and Test Architect roles; each final role decision is recorded only against the completed finite record.

## Boundaries & Constraints

**Always:** Use isolated clean worktrees/caches; materialize read-only dependency worktrees at exact EventStore gitlink SHAs; retain first results, commands, times, exits, logs, artifacts, and hashes; preserve `3.88.0`/`3.70.1` negatives; create commitlint-valid local candidate and two-commit rollback revisions; append to records/evidence only.

**Never:** Initialize nested submodules, move gitlinks, edit EventStore production, weaken gates, relabel untagged HEAD as package source, rewrite history, push/publish/release, infer acceptance, or close downstream gates. Architecture/P1R change only after four-owner acceptance of one record.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Candidate | Selected tag/package and aligned Builds | All required phase rows pass | Retain first non-pass; leave P1R open |
| Materialization | Exact dependency bytes/Git objects | Governance tests run without nested initialization/live mutation | Missing/wrong SHA is non-qualifying |
| Rollback | `v3.70.1` plus two Builds commits | Accept rollback; reject candidate with `HXM016` | Any failure blocks acceptance |
| Acceptance | Complete finite record | Four named roles decide on identical evidence | Pending/reject leaves state unchanged |

</frozen-after-approval>

## Code Map

- `_bmad-output/specs/spec-6-1-p1r-revalidation-owner-acceptance/qualification-contract.md` -- exact phases, commands, record schema, owner scopes, propagation.
- `_bmad-output/implementation-artifacts/deferred-work.md` (`DW-35`, `DW-68`) -- existing coordinate/materialization/rollback decisions.
- `references/Hexalith.Builds/{Props/Directory.Packages.props,Tools/package-version-audit.json}` -- current catalog/audit.
- `references/Hexalith.Builds/{src/libraries/Hexalith.Builds.Tooling/Manifest,schemas,test,Tools}` -- runner pins, fixtures, hashes, controls, tests.
- `references/Hexalith.EventStore/{Directory.Packages.props,Directory.Build.props,tools/release-packages.json,tests}` -- read-only import, dependency, 14-package, and governance surfaces.
- `references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p1r-eventstore-source-architecture-runner-revalidation-record.md` -- append-only record.
- `_bmad-output/implementation-artifacts/{qualification-evidence,sprint-status.yaml}` -- evidence and gated propagation.

## Tasks & Acceptance

**Execution:**
- [ ] `references/Hexalith.Builds/` -- align runner/schema/tests/fixtures/evidence/controls, recompute hashes, pass audit, and commit the candidate.
- [ ] `references/Hexalith.EventStore/` -- create disposable exact-revision lanes, materialize dependencies, and run coordinate, source/package, seven-API, governance, and 14-package remote-restore gates.
- [ ] `references/Hexalith.Builds/` -- run candidate/G-4 lanes; create and prove the two-commit `3.70.1` rollback.
- [ ] `qualification-evidence/` and canonical record -- retain a manifest-verified bundle and append the finite record.
- [ ] Architecture, `sprint-status.yaml`, and DW-35/DW-68 -- propagate only the acceptance state authorized by the chosen completion boundary; preserve every downstream blocker.

**Acceptance Criteria:**
- Given the selected candidate and rollback, when contract phases 1–5 run from clean isolated worktrees, then every required row is `PASS`, all evidence hashes resolve, exact-pin negatives are deterministic, and no nested submodule or live checkout is changed.
- Given the finite record, when the completion boundary is applied, then technical-only work remains explicitly pending, or four named acceptances rebind Architecture and close only P1R against identical revisions.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

EventStore HEAD is `v3.106.0-29-gffb6901a` with production changes. Exact dependency materialization may shadow sibling Builds; record the catalog each lane resolves.

## Verification

**Commands:**
- Execute every command row in the qualification contract's EventStore, Builds, remote-package, and reciprocal-rollback matrices -- expected: retained `PASS` rows only.
- `sha256sum -c artifact-manifest.sha256` in the new evidence bundle -- expected: every listed artifact matches.
- `git diff --check` and repository status captures before/after every owned checkout and lane -- expected: no whitespace defects, hidden drift, gitlink movement, or initialized nested submodule.
