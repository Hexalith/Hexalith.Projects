---
title: '6.1-P1R Prepare and qualify a new published EventStore successor'
type: 'feature'
created: '2026-10-11'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/spec-6-1-p1r-remediation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/6-1-p1r-published-successor-decision-packet-2026-10-11.md'
---

<frozen-after-approval reason="human-owned release and rollback scope">

## Intent

**Problem:** The sealed 3.119.0 run fails candidate stale-fence and logical-alias checks. The latest published 3.120.0 family predates their current source/fixture repairs, so there is no suitable published successor to qualify. Current P1R remains unusable.

**Approach:** Prepare an exact current-source release decision, publish only through the existing owner-controlled workflow after authorization, then qualify the resulting immutable family on PostgreSQL/Dapr 1.18.2 with a new candidate-specific support table. Keep technical qualification, role acceptance, and pin transitions separate.

**Decision (2026-10-11):** The user selected the recommended published-successor path and `rollback=null`. Retain Projects AD-17 drain, mutation freeze, and forward recovery. This is planning direction, not a release or four-role acceptance decision.

## Boundaries & Constraints

**Always:** Preserve sealed failures, original event hashes, two-Tenant isolation, one writer, retained-floor append/restart, dual-principal authority, positive watermarks, and actual package/Builds/runtime identities. Prove incapable old-route refusal before protected access or effect. Recheck exact source and CI before release.

**Never:** Reseal 3.119.0; treat source tests or 3.120.0 as repaired package proof; reuse its 163-case table without new measurement; hide opaque denials or unexecuted alias registration; guess a version; bypass validation without explicit owner disposition; change pins, Stack, P1R usability, or Story 6.1 status from publication alone.

## I/O & Edge-Case Matrix

| Input / state | Required observation | Failure treatment |
| --- | --- | --- |
| Release source and CI | Exact live main; green CI or explicit Commitlint exception; 14 packages and container in scope | Changed source or missing proof blocks release |
| Replaced/forged fence at sequence 12 | Specific denial before effect; both Tenants unchanged | Opaque error or false completion fails |
| Stored old alias and version 987 | Actual package reader hydrates known alias; unknown version rejects; old bytes persist | Missing registration/read or applied unknown fails |
| rollback=null | One writer, drain and mutation freeze, forward recovery | Backup losing later writes cannot count as rollback |

</frozen-after-approval>

## Open Questions

- Which exact publication path does the EventStore/Builds owner authorize? **Repair the three Contracts CI failures and require green exact-source CI** (preferred; changes the candidate SHA, then rechecks it) / **use the release workflow's `bypass-validation=true` exception at current `25f063e9fee2da1f4e00acb512083c2ca223d12a`** (publishes all 14 packages and the container with known Contracts failures, using exact-source Commitlint proof) / **hold or select another source** (no release or package run). The protected production job and publication-enable gate still apply.

## Code Map

Paths below are relative to `references/Hexalith.EventStore/`.

- `.github/workflows/release.yml` — exact live-main, CI/Commitlint gate, protected publication of 14 packages plus container; no new release mechanism.
- `tests/Hexalith.EventStore.Contracts.Tests/Packaging/{ContractsPackageDependencyTests,DeployedRuntimeParityClosureTests,CorrectedDeployedRuntimeParityClosureTests}.cs` and `_bmad-output/implementation-artifacts/sprint-status.yaml` — three current CI failures; inspect and repair only if green-CI route is chosen.
- `src/Hexalith.EventStore.Server/Commands/IdempotencyExecutionContextProtector.cs`, `Actors/AggregateActor.cs`, `tools/p1r-published-consumers/domain/FixtureEvolutionManifest.cs` — post-3.120.0 repairs requiring real package proof.
- `tools/p1r_published_executor.py`, `tools/p1r_published_qualification.py`, `tools/p1r_support_boundary.json` — 3.119.0-bound execution and policy; derive a new candidate table without masking history.
- `_bmad-output/implementation-artifacts/evidence/6-1-p1r-31190-published-run/README.md` — sealed nonqualifying baseline; read only.

## Tasks & Acceptance

**Execution:**
- [ ] `tests/Hexalith.EventStore.Contracts.Tests/Packaging/` and `_bmad-output/implementation-artifacts/sprint-status.yaml` — if green CI is chosen, fix only the reproduced blockers and verify exact-source CI; otherwise preserve failures as an explicit exception limitation.
- [ ] `.github/workflows/release.yml` — after exact owner authorization, use its existing dispatch; capture resulting tag, actual archive hashes and package-build Builds revision, or retain the failed release attempt.
- [ ] `tools/p1r_published_executor.py`, `tools/p1r_published_qualification.py`, and `_bmad-output/implementation-artifacts/evidence/6-1-p1r-next-published-run/` — run the exact published family, candidate checks, persisted restore/replay, old-route admission, cleanup, new case inventory and independent validation.
- [ ] `tools/tests/test_p1r_published_executor.py` and `test_p1r_published_qualification.py` — reject package substitution, candidate failures, invented refusals and missing assertions.

**Acceptance Criteria:**
- Given no package containing the repair, when successor selection runs, then 3.120.0 is refused and no qualification is claimed.
- Given an authorized exact release and its published archives, when qualification runs, then every required case records actual package identity, persisted effect/refusal, assertions and cleanup.
- Given a failed case, missing owner decision or unproved production admission, when P1R is assessed, then usability remains false and pins/status do not advance.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Focused P1R Python tests and affected individual .NET test projects — pass or retain exact blockers.
- Exact-source CI or documented Commitlint exception, then isolated package `run` and independent `prepare`/`validate` on unique paths — truthful gate values.
- `git diff --check` in changed repositories — pass.
