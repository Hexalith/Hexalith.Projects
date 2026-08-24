---
title: '6.1-P1R Revalidate EventStore and Builds Baseline'
type: 'bugfix'
created: '2026-08-24'
status: 'in-progress'
review_loop_iteration: 0
baseline_commit: 'be91dd091136d8d26fa6132b3c465113e09ddc3d'
context:
  - '{project-root}/_bmad-output/specs/spec-6-1-p1r-revalidation-owner-acceptance/SPEC.md'
  - '{project-root}/_bmad-output/specs/spec-6-1-p1r-revalidation-owner-acceptance/qualification-contract.md'
  - '{project-root}/references/Hexalith.Builds/DEVELOPMENT.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** EventStore source/packages, the Builds catalog/runner, and Architecture no longer share one authority coordinate. The prior `3.90.0` candidate is historical only.

**Approach:** Record HEAD `da52e2c85ecc5909fa8ce2547e626f3968c056ef` as unselected, qualify equivalent source/package tuple `v3.97.0` / `3.97.0` / `94591f3539ce30372db58e5fdd3ba017ea8c07b8`, align one immutable Builds candidate, and prove a separate executable `3.70.1` rollback. Stop with four pending human decisions.

## Boundaries & Constraints

**Always:** Use clean exact worktrees and isolated caches; retain first results, commands, UTC timestamps, exits, logs, artifacts, and hashes; preserve history byte-for-byte; commitlint-validate narrowly scoped Builds commits; append only to the canonical record; stop at the first non-passing or incomplete gate.

**Ask First:** Selecting any tuple other than the `v3.97.0` equivalent source/package candidate; accepting a source/package divergence; changing EventStore source, package publication, Architecture, planning/status, dependency gitlinks, or owner decisions.

**Never:** Push, publish, release, move gitlinks, initialize nested submodules, infer acceptance, implement Story 6.1/P0/G-1, or close P1R or any downstream gate.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Candidate | `v3.97.0` source/packages and catalog | All source, package, API, audit, runner, and packaged controls pass | Append blocker; keep P1R open |
| Current HEAD | Five post-tag commits, no `src/` diff | Record as unselected observation, never relabel as package source | Any production/API drift requires owner direction |
| Active stale pins | `3.88.0`, `3.70.1` | Each rejects with only `HXM016` | Unexpected diagnostic fails qualification |
| Rollback | Atomic Builds `3.70.1` revision | `3.70.1` passes and `3.97.0` rejects with `HXM016` | Missing reciprocal proof fails qualification |
| Evidence failure | Missing artifact, stall, cancellation, nonzero exit | No pass or packet | Retain evidence; report owner action |

</frozen-after-approval>

## Code Map

- `.../qualification-contract.md` -- phase order, clean protocol, matrices, ledger schema, and acceptance boundary.
- `references/Hexalith.EventStore/tools/release-packages.json` and `.github/workflows/{ci,release}.yml` -- 14-package inventory and lane patterns.
- `references/Hexalith.Builds/Props/Directory.Packages.props` and `Tools/package-version-audit.json` -- current `3.97.0` catalog/audit.
- `references/Hexalith.Builds/src/libraries/Hexalith.Builds.Tooling/Manifest/SupportedPlatformPins.cs`, schema, tests, and `test/fixtures/{module,evidence}/` -- stale `3.90.0` runner corpus.
- `references/Hexalith.Builds/Tools/{G4PackageQualification.functions,test-g4-tool-package-contracts}.ps1` -- official packaged controls; never bypass source validation.
- `references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p1r-eventstore-source-architecture-runner-revalidation-record.md` -- append-only record; historical sections are immutable.
- `_bmad-output/implementation-artifacts/qualification-evidence/` -- new dated sibling bundle; Architecture/status remain read-only.

## Tasks & Acceptance

**Execution:**
- [ ] Recapture remote/tag/package coordinates, all 14 official consumptions, source diff, and seven API blobs; isolate each EventStore revision with its recorded dependency gitlinks.
- [ ] Align active Builds runner/schema/test/fixture/evidence/hash assertions to `3.97.0`, preserve exact negatives/all-`F`, validate the full audit, and create an immutable candidate commit.
- [ ] Create a separate validated rollback commit binding catalog, audit, runner, schema, fixtures, evidence, and hashes to `3.70.1`, rejecting `3.97.0`.
- [ ] Run phases 0–5 serially: mandated EventStore tests, 14-package remote restore, complete Builds gates, official G-4 controls, and reciprocal rollback.
- [ ] Retain a complete dated evidence bundle and append a superseding section plus four pending decision rows to the canonical record; do not edit Architecture or sprint status.

**Acceptance Criteria:**
- Given exact candidate/rollback revisions, when every lane runs, then every row is `PASS`, evidence hashes resolve, and reciprocal pin behavior is exact.
- Given complete evidence, when handoff is prepared, then one packet names the tuple/revisions/evidence and leaves all four named decisions pending.

## Spec Change Log

## Design Notes

Use the package tag for both EventStore modes: all 14 packages bind to it, Builds catalogs it, and HEAD has no `src/` changes. Record HEAD separately.

## Verification

**Commands:**
- EventStore contract lanes -- expected: four projects pass in both modes and all 14 exact packages restore remotely.
- Builds restore/build/tests/catalog/audit and official packaged gate -- expected: exit `0`, zero mismatches, retained evidence, no bypass.
- Reciprocal rollback matrix at `3.70.1` -- expected: positive rollback plus exact `3.97.0` `HXM016` rejection.
- `git diff --check`, artifact-manifest hash verification, and clean pre/post worktree captures -- expected: no drift, missing evidence, or dirty marker.
