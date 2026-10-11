---
title: '6.1-P1R Prepare and qualify a new published EventStore successor'
type: 'feature'
created: '2026-10-11'
status: 'ready-for-dev'
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

**Route decision (2026-10-11):** The user chose Green CI. Repair the three Contracts failures and require a new exact-source green CI run. If the Builds validator fix needs it, updating EventStore's nested Builds pointer is authorized. Publication still needs a separate exact-source owner decision.

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

## Code Map

EventStore paths below are relative to `references/Hexalith.EventStore/`.

- `.github/workflows/release.yml:32-234` — reuse exact-main CI/Commitlint and protected publication gates. Current source has three Contracts failures in CI 38133533659; Commitlint 38133533676 passed.
- `tests/Hexalith.EventStore.Contracts.Tests/Packaging/`, `_bmad-output/implementation-artifacts/sprint-status.yaml:138`, and workspace `references/Hexalith.Builds/Tools/validate-consumer-package-authority.ps1` — three CI failures; do not initialize nested Builds.
- `src/Hexalith.EventStore.Server/Commands/IdempotencyExecutionContextProtector.cs`, `Actors/AggregateActor.cs`, `tools/p1r-published-consumers/domain/FixtureEvolutionManifest.cs` — repairs absent from published 3.120.0.
- `tools/p1r_published_executor.py:35-42,892-1000,1315-1492`, `tools/p1r_published_qualification.py:313-515,1150-1337`, `tools/p1r_support_boundary.json` — 3.119.0 bindings, case receipts and sealed policy.

## Tasks & Acceptance

**Execution:**
- [ ] Workspace `_bmad-output/implementation-artifacts/6-1-p1r-published-successor-decision-packet-2026-10-11.md` — record the Green CI route and leave exact-source release authorization and role acceptance pending.
- [ ] `tests/Hexalith.EventStore.Contracts.Tests/Packaging/ContractsPackageDependencyTests.cs` and workspace `references/Hexalith.Builds/Tools/validate-consumer-package-authority.ps1` — reproduce and fix the null-expression failure without weakening authority; update EventStore's nested Builds gitlink only if required.
- [ ] `tests/Hexalith.EventStore.Contracts.Tests/Packaging/DeployedRuntimeParityClosureTests.cs`, `tests/Hexalith.EventStore.Contracts.Tests/Packaging/CorrectedDeployedRuntimeParityClosureTests.cs`, and `_bmad-output/implementation-artifacts/sprint-status.yaml` — resolve both guarded Story 3.15 tracker assertions with truthful caveat and subject evidence; prove exact-source green CI.
- [ ] `tools/p1r_published_executor.py`, `tools/p1r_published_qualification.py`, and new `tools/p1r_successor_support_boundary.json` — prepare identity-bound successor measurement and a separate case table without guessing a version or altering 3.119.0 evidence.
- [ ] `tools/tests/test_p1r_published_executor.py` and `tools/tests/test_p1r_published_qualification.py` — reject substitution, candidate failures, invented refusals and missing assertions.
- [ ] `.github/workflows/release.yml` — after green CI and a reviewable candidate, obtain exact owner authorization and dispatch existing workflow; capture tag, archive hashes, package-build Builds identity and container, or retain failure.
- [ ] Workspace `_bmad-output/implementation-artifacts/evidence/6-1-p1r-next-published-run/` — run authorized archives on PostgreSQL/Dapr 1.18.2; retain case receipts, pre-effect refusals, persisted restore/restart, hashes, Tenant isolation, cleanup and independent validation. Leave pins, usability and Story 6.1 blocked.

**Acceptance Criteria:**
- Given 3.120.0 lacks the repair, when successor selection runs, then it is refused without qualification.
- Given authorized published archives, when qualification runs, then candidate-specific effects/refusals, identities, assertions and cleanup are independently verifiable.
- Given a failed required case, missing owner decision or unproved production admission, when readiness is assessed, then usability stays false and pins/status do not advance.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `python3 -m unittest discover -s tools/tests -p 'test_p1r_published_*.py'` in EventStore — pass.
- Run Contracts tests individually in Release package mode; require green exact-source CI.
- Run isolated package execution and independent `prepare`/`validate`; inspect receipts and gate values.
- `git diff --check` in changed repositories — pass.
