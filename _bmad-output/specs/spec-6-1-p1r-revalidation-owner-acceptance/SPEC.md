---
id: SPEC-6-1-p1r-revalidation-owner-acceptance
companions:
  - qualification-contract.md
  - ../../../references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p1r-eventstore-source-architecture-runner-revalidation-record.md
  - ../../planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md
sources:
  - ../../planning-artifacts/sprint-change-proposal-2026-08-03.md
---

> **Canonical contract.** This SPEC and the files in `companions:` are the complete, preservation-validated contract for what to build, test, and validate. Source documents listed in frontmatter are for traceability only — consult them only if you need narrative rationale or prose color this contract intentionally omits.

# 6.1-P1R Revalidation and Owner Acceptance

## Why

Post-P1 dependency drift split EventStore source, published packages, the Builds catalog, runner source, published G-4 tools, and the Architecture Spine across incompatible authority coordinates. P1R must qualify and obtain four-owner acceptance for one truthful source/package/runner/architecture baseline before P0 or later Story 6.1 gates may consume it.

## Capabilities

- **CAP-1**
  - **intent:** Owners can select explicit immutable EventStore source-mode and package-mode coordinates without conflating their identities.
  - **success:** One finite record names both revisions and either accepts their intentional divergence or names a new immutable release from the selected source before runner alignment.
- **CAP-2**
  - **intent:** Builds can align the selected EventStore package pin across its catalog, full audit, runner, schema, positive fixtures, serialized evidence, and coupled hashes at one immutable revision.
  - **success:** Every active value agrees, the full catalog audit passes with no concealed mismatch, and the exact qualifying Builds revision is recorded.
- **CAP-3**
  - **intent:** Qualifiers can prove the seven Story 6.1 APIs and affected source/package behavior at the selected coordinates.
  - **success:** Independent clean source-mode and package-mode lanes pass, and retained evidence records the focused blob and behavioral comparisons.
- **CAP-4**
  - **intent:** Qualifiers can establish that the aligned Builds runner and packaged-command contract enforce the selected baseline and fail-closed evidence rules.
  - **success:** In isolated serialized execution, Module, Evidence, catalog, audit, package build/restore, positive, unrelated-negative, and evidence-hash controls produce their required retained results.
- **CAP-5**
  - **intent:** Qualifiers can prove reciprocal candidate and rollback exact-pin behavior.
  - **success:** The active runner accepts only the selected pin while rejecting `3.88.0` and `3.70.1`, and a clean rollback worktree accepts `3.70.1` while rejecting the selected candidate.
- **CAP-6**
  - **intent:** Reviewers can decide P1R from one finite evidence and acceptance record.
  - **success:** The record binds exact coordinates, commands, UTC timestamps, exits, logs, artifact hashes, and dispositions; no required row is pending, inconclusive, non-qualifying, or a placeholder; all four owner roles accept that same record.
- **CAP-7**
  - **intent:** Planning maintainers can propagate an accepted P1R baseline without widening scope.
  - **success:** Architecture records the separate source/package/Builds coordinates and executable `3.70.1` rollback, only P1R closes, and P0 plus every later Story 6.1 gate remains blocked by its own contract.

## Constraints

- All proposal and workspace coordinates are dated observations, not acceptance. Execution starts by recapturing exact clean source, package, and Builds coordinates and obtaining the selection required by `qualification-contract.md`.
- Source checkout identity and package-source identity remain separate unless both resolve to the same immutable revision; no checkout may be relabeled as package source.
- EventStore `3.88.0` is a superseded unaccepted candidate and cannot become an accepted or rollback baseline. EventStore `3.70.1` remains the last accepted rollback baseline until a later accepted record explicitly changes it.
- The original `3.88.0` commands and results remain immutable historical candidate evidence. Revalidation appends a dated supersession section or creates a dated sibling record.
- Every acceptance lane runs from clean exact revisions, serialized where outputs can contend, and retains UTC start/end times, commands, exits, logs, and hashes. Cancellation, exit `143`, stalls, partial runs, and exploratory passes never qualify.
- The package-version audit covers the complete catalog and passes at the accepted Builds revision; an EventStore-only pass cannot conceal unrelated mismatches.
- The active and rollback worktrees preserve reciprocal exact-pin negatives, unrelated fixtures keep their single-purpose diagnostics, and deliberate evidence-hash mismatch controls remain fail-closed.
- Architecture remains bound to `3.70.1` until all required lanes pass and the EventStore Owner, Builds Owner, Solution Architect, and Test Architect accept the same record.
- Historical P1 remains complete. P1R acceptance does not select G-1 Durable Task or Confirmation Artifact capability and cannot substitute for P0 publication, consumer manifests, persisted qualification, or P0 owner acceptance.
- This contract does not itself authorize commits, pushes, releases, dependency updates, source changes, submodule moves, or completion transitions; each mutation still requires an implementation request and owning-repository authority.

## Non-goals

- Change the PRD, UX, MVP, epic/story inventory, sequencing, or priority.
- Deliver P0, publish or pin its accepted G-4 tools, create its Projects consumer manifests, or run its persisted multi-module qualification.
- Select G-1, close P0/P2/P3/P4 or Story 6.1, or clear any transitive Epic 7/8 gate.
- Treat a focused API comparison, package listing, published stale tool, or passing runner corpus bound to an old pin as acceptance.

## Success signal

- A clean finite record proves one exact source/package/Builds/Architecture tuple and executable `3.70.1` rollback, all mandatory lanes pass with retained evidence, all four owner roles accept the same coordinates, Architecture is rebound, and planning closes only 6.1-P1R while every downstream gate remains blocked.

## Open Questions

- Which exact EventStore source revision, package version/tag/source revision, and qualifying Builds revision do the owners select; if source and package differ, must a new release be published before alignment?
- Which named approver will sign each of the EventStore Owner, Builds Owner, Solution Architect, and Test Architect rows?
