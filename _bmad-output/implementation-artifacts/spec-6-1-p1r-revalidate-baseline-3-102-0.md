---
title: '6.1-P1R Align EventStore and Builds Baseline to 3.102.0'
type: 'bugfix'
created: '2026-09-05'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '9f5c81976cc546a03275caf84d77db710745f0f4'
context:
  - '{project-root}/_bmad-output/specs/spec-6-1-p1r-revalidation-owner-acceptance/qualification-contract.md'
  - '{project-root}/references/Hexalith.Builds/DEVELOPMENT.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** EventStore source/packages and the Builds catalog have moved to `v3.102.0`, superseding the stopped `3.97.0` attempt in `spec-6-1-p1r-revalidation-owner-acceptance.md` (never passed its first gate). The Builds runner/schema/fixture/test corpus still hardcodes `3.90.0`, and P1R remains `open` with a stale 2026-08-03 `observed_drift` snapshot in `sprint-status.yaml`.

**Approach:** Select the immutable `v3.102.0` tag commit `4ae9cee1e9abe050402fd1405a9abd54892ba13f` as both source-mode and package-mode revision (source/package equivalent, skipping the 18 untagged post-tag commits), align one immutable Builds candidate to `3.102.0`, prove a separate executable `3.70.1` rollback, and append a superseding section to the canonical P1R record with four pending owner decisions. Supersedes `spec-6-1-p1r-revalidate-platform-baseline-2.md` and `spec-6-1-p1r-revalidation-owner-acceptance.md` (both historical only).

## Boundaries & Constraints

**Always:** Follow `qualification-contract.md` exactly — clean isolated worktrees per revision, serialized lanes with unique NuGet/CLI-home/temp/output dirs, retained first results (UTC, commands, exits, hashes), `.slnx`-only restore/build, commitlint-validated narrowly-scoped Builds commits, append-only record/audit edits.

**Never:** Push, publish, release, move gitlinks, init nested submodules, mark P1R or any downstream gate (P0/P2/P3/P4/Story 6.1) done/accepted, infer owner acceptance, edit the Architecture Spine or `sprint-status.yaml` status fields (append evidence pointers only), select a coordinate other than `v3.102.0`/`3.102.0` without recorded owner rationale, or skip classifying the four post-tag `Actors/*` files.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Candidate | `v3.102.0` source+package tag, clean worktrees | All source, package, 14-ID remote-restore, seven-API, audit, runner, packaged-tool controls pass | Append blocker row; keep P1R `open` |
| Post-tag drift | HEAD 18 commits past tag, only `Actors/*` touched | Record as unselected observation, explicit diff classification | Named-path change requires owner direction |
| Stale pins | `3.90.0` (runner), `3.88.0`, `3.70.1` | Active-runner negatives reject with only `HXM016` | Unexpected diagnostic fails qualification |
| Rollback | Atomic Builds `3.70.1` revision | `3.70.1` passes; `3.102.0` rejects with `HXM016` | Missing reciprocal proof fails qualification |
| Evidence failure | Missing artifact, stall, cancel, nonzero exit | No pass/packet; record `INCONCLUSIVE`/`FAIL` with root cause | Retain evidence; never silently retry into a false pass |

</frozen-after-approval>

## Code Map

- `qualification-contract.md` -- authoritative phases/schema/matrices/owner table; do not restate here.
- `references/Hexalith.EventStore` @ `v3.102.0` = `4ae9cee1e9abe050402fd1405a9abd54892ba13f` -- selected revision; `tools/release-packages.json` there has the 14-ID inventory.
- `references/Hexalith.Builds/Props/Directory.Packages.props:8` -- catalog reads `3.102.0`; re-verify at qualifying revision.
- `.../Manifest/SupportedPlatformPins.cs:14` -- `EventStoreVersion = "3.90.0"`, stale, align to `3.102.0`.
- `references/Hexalith.Builds/test/fixtures/{module,evidence}/**`, `test/Hexalith.Builds.Module.Tests/*.cs` -- runner corpus hardcoding `3.90.0`; keep `superseded-platform-pin.json` (`3.88.0`) and `tampered-platform-pin.json` (`3.70.1`) as intentional negatives.
- `references/Hexalith.Builds/Tools/package-version-audit.json` -- `hexalith-eventstore` family, `retained`, already `3.102.0`-catalog-ancestor; append only.
- `references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p1r-eventstore-source-architecture-runner-revalidation-record.md` -- append-only; latest section is the stopped `3.97.0` attempt (failed `es-001-coordinates` on a shell-quoting bug, not a coordinate defect).
- `_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md:368` -- still bound to `3.70.1`/`f13f9925fdca53efa2ab8c90d396ab106f91bb9c`; read-only.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (6.1-P1R) -- `status: open`, stale drift snapshot; append evidence pointer only.

## Tasks & Acceptance

**Execution:**
- [ ] `Hexalith.EventStore` clean worktree @ `4ae9cee1e9abe050402fd1405a9abd54892ba13f` -- coordinate/source comparison, 14-package manifest+remote-restore, seven-API comparison, source-mode + package-mode lanes -- fills the coordinate record.
- [ ] `Hexalith.Builds` clean worktree -- align catalog/runner/schema/fixture/evidence/hashes to `3.102.0`, preserve `3.88.0`/`3.70.1` negatives, pass full audit, one immutable candidate commit -- fills `builds_qualifying_revision`.
- [ ] `Hexalith.Builds` separate clean worktree -- validated rollback commit binding everything to `3.70.1`, rejecting `3.102.0` -- fills `rollback_builds_revision`.
- [ ] Run contract phases 0-5 serially; stop at first non-passing/incomplete gate.
- [ ] Append dated superseding section (10 required subsections, four `pending` owner rows) to the canonical record; Architecture and `sprint-status.yaml` status untouched.

**Acceptance Criteria:**
- Given the `v3.102.0` candidate and `3.70.1` rollback revisions, when every contract lane runs from clean serialized worktrees, then every row is `PASS`, hashes resolve, and reciprocal `HXM016` behavior is exact.
- Given complete evidence, when the record is appended, then it names the exact tuple/revisions, leaves all four owner decisions `pending`, and no downstream status changes.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

The stopped `3.97.0` attempt failed its first gate on a shell-quoting bug, not a coordinate defect — expand every contract command literally and retain the exact expanded string to avoid repeating that failure class.

## Verification

**Commands:**
- Contract's EventStore source-mode/package-mode lanes -- expected: all four test projects pass both modes; 14-ID remote restore resolves cleanly, no local feed/fallback/floating version.
- Contract's Builds serialized lane -- expected: restore/build/Module.Tests/Evidence.Tests/catalog script/audit script/G-4 gate all exit `0`, zero stale mismatches, no `-SkipSourceValidation`.
- Contract's reciprocal rollback matrix -- expected: candidate rejects `3.88.0`/`3.70.1`; rollback passes `3.70.1`, rejects `3.102.0`; exact `HXM016`.
- `git status --porcelain=v1` before/after each worktree -- expected: clean, no dirty marker.
