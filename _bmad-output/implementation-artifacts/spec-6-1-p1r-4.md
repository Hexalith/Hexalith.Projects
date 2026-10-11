---
title: '6.1-P1R Prepare and qualify a new published EventStore successor'
type: 'feature'
created: '2026-10-11'
status: 'in-progress'
baseline_commit: '25bd0b29ff0d768d8a712908184937d10db6bc9d'
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

- `.github/workflows/release.yml:32-234` — reuse exact-main CI/Commitlint and protected publication gates. The Contracts failures were repaired; exact-source CI for EventStore `3099cb27` passed.
- `tests/Hexalith.EventStore.Contracts.Tests/Packaging/`, `_bmad-output/implementation-artifacts/sprint-status.yaml:138`, and workspace `references/Hexalith.Builds/Tools/validate-consumer-package-authority.ps1` — three CI failures; do not initialize nested Builds.
- `src/Hexalith.EventStore.Server/Commands/IdempotencyExecutionContextProtector.cs`, `Actors/AggregateActor.cs`, `tools/p1r-published-consumers/domain/FixtureEvolutionManifest.cs` — repairs absent from published 3.120.0.
- `tools/p1r_published_executor.py:35-42,892-1000,1315-1492`, `tools/p1r_published_qualification.py:313-515,1150-1337`, `tools/p1r_support_boundary.json` — 3.119.0 bindings, case receipts and sealed policy.

## Tasks & Acceptance

**Execution:**
- [x] Workspace `_bmad-output/implementation-artifacts/6-1-p1r-published-successor-decision-packet-2026-10-11.md` — record the Green CI route and leave exact-source release authorization and role acceptance pending.
- [x] `tests/Hexalith.EventStore.Contracts.Tests/Packaging/ContractsPackageDependencyTests.cs` and workspace `references/Hexalith.Builds/Tools/validate-consumer-package-authority.ps1` — reproduce and fix the null-expression failure without weakening authority; update EventStore's nested Builds gitlink only if required.
- [x] `tests/Hexalith.EventStore.Contracts.Tests/Packaging/DeployedRuntimeParityClosureTests.cs`, `tests/Hexalith.EventStore.Contracts.Tests/Packaging/CorrectedDeployedRuntimeParityClosureTests.cs`, and `_bmad-output/implementation-artifacts/sprint-status.yaml` — resolve both guarded Story 3.15 tracker assertions with truthful caveat and subject evidence; Contracts CI passed on EventStore `39962529`.
- [x] EventStore CI — exact-source push CI `38146720449` passed for live-main release source `3099cb27`, including Contracts and the tracked-content secret gate.
- [x] EventStore Integration Tests — exact-source live-sidecar workflow `38146720145` passed after the two obsolete unqualified snapshot expectations were corrected.
- [x] `tools/p1r_published_executor.py`, `tools/p1r_published_qualification.py`, and new `tools/p1r_successor_support_boundary.json` — prepare identity-bound successor measurement and a separate case table without guessing a version or altering 3.119.0 evidence.
- [x] `tools/tests/test_p1r_published_executor.py` and `tools/tests/test_p1r_published_qualification.py` — reject substitution, candidate failures, invented refusals and missing assertions.
- [ ] `.github/workflows/release.yml` — after green CI and a reviewable candidate, obtain exact owner authorization and dispatch existing workflow; capture tag, archive hashes, package-build Builds identity and container, or retain failure.
- [ ] Workspace `_bmad-output/implementation-artifacts/evidence/6-1-p1r-next-published-run/` — run authorized archives on PostgreSQL/Dapr 1.18.2; retain case receipts, pre-effect refusals, persisted restore/restart, hashes, Tenant isolation, cleanup and independent validation. Leave pins, usability and Story 6.1 blocked.

**Acceptance Criteria:**
- Given 3.120.0 lacks the repair, when successor selection runs, then it is refused without qualification.
- Given authorized published archives, when qualification runs, then candidate-specific effects/refusals, identities, assertions and cleanup are independently verifiable.
- Given a failed required case, missing owner decision or unproved production admission, when readiness is assessed, then usability stays false and pins/status do not advance.

## Implementation Notes

- Local Builds validator now reports empty XML explicitly. The EventStore Contracts authority test excludes only the 300 exact, hash-bound historical 3.119.0 probe files; current consumer graph authority remains checked. The guarded Story 3.15 tracker block is restored.
- The successor executor checks selected tag, live source, tag Builds identity, and signed archive bytes before protected runtime access. The qualification tooling requires a separate candidate-specific measured support boundary and Solution Architect disposition. The new table file remains deliberately unpopulated until authorized published archives produce receipts. The 3.119.0 table and sealed evidence were not changed.
- EventStore `3099cb27` retains the repaired Builds gitlink `608c164c3637240fed2dd92dbfdaaf581ba9846d` and adds live-sidecar fail-closed snapshot/replay assertions. The nested Builds worktree remains uninitialized. The user authorized pushing preparation fixes after the preceding CI run. Exact-source CI `38146720449`, Integration Tests `38146720145`, and P1R Preparation `38146720151` passed as push runs on `main` for `3099cb27`. Jérôme Piquot, Owner, authorized the exact-source `release.yml` dispatch with `bypass-validation=false` and the full 14-package/container scope on 2026-10-11; no successor version is selected yet. Publication and PostgreSQL/Dapr package execution remain pending. Story 6.1, pins, Stack, and P1R usability remain blocked.

## Spec Change Log

- 2026-10-11: Started implementation from workspace baseline `25bd0b29ff0d768d8a712908184937d10db6bc9d`. Completed local repair, successor tooling, and negative-test tasks. Retained release and operational qualification tasks open pending exact-source CI, owner authorization, and published archives.
- 2026-10-11: Observed pushed EventStore `ef6e688672e1b09a78e78237e4dbfb607d3635a5` with repaired Builds pointer and Projects `f58e7391637e4afcbbb864e50b80c95f5924bb39`. Exact-source CI result and release authorization remain pending.
- 2026-10-11: EventStore `39962529` passed Contracts CI and P1R Qualification Preparation 35/35, but CI build-and-test failed only the tracked-content secret gate. Pushed `e26173a4` with verified local gate repair; its exact-source CI and owner release decision remain pending.
- 2026-10-11: Exact-source CI for `e26173a4` passed. A separate Integration Tests workflow remains red on two snapshot tests that also failed at `b57310fb` and `d35ab1`; release decision remains pending their disposition.
- 2026-10-11: Story 6.2's qualified folded-snapshot rule explained both live-sidecar failures. Updated the two tests to prove fail-closed no-snapshot behavior and full-prefix replay; pushed EventStore `3099cb27`. Exact-source CI, Integration Tests, and P1R Preparation passed. Release and candidate-specific qualification still require owner decisions.

- 2026-10-11: Jérôme Piquot, Owner, authorized the exact-source `3099cb27` Green CI release dispatch with `bypass-validation=false`, full 14-package/container scope, and `rollback=null`; publication outcome and candidate-specific qualification remain pending.

## Review Triage Log

## Verification

**Local results (2026-10-11):**
- `dotnet build tests/Hexalith.EventStore.Contracts.Tests/Hexalith.EventStore.Contracts.Tests.csproj -c Release -m:1 --no-restore -p:UseHexalithProjectReferences=false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 -v:q` in EventStore — exit 0, zero warnings/errors.
- Built Contracts test assembly with the three focused `-method` patterns for consumer authority and the two Story 3.15 guards — exit 0, four invocations, zero failures.
- `python3 -m unittest discover -s tools/tests -p 'test_p1r_published_*.py'` in EventStore — 118 tests, pass.
- `git diff --check` in workspace, EventStore, and Builds — pass.
- Full Contracts assembly run (`timeout 600s dotnet tests/Hexalith.EventStore.Contracts.Tests/bin/Release/net10.0/Hexalith.EventStore.Contracts.Tests.dll`) reported a five-second process-test timeout, then was interrupted after no further output (exit 130); the later fixture checkout failure was caused by that interruption. This is not a full-suite pass.
- Isolated rerun (`timeout 120s dotnet tests/Hexalith.EventStore.Contracts.Tests/bin/Release/net10.0/Hexalith.EventStore.Contracts.Tests.dll -method '*ProcessContractDistinguishesOwnedExternalAndExitedProcesses'`) passed 1/1 in 1.716 seconds.
- On EventStore `d35ab1f8bb2a527d38e901ac0b9faf69e1a7519a` with the repaired Builds pointer staged: Contracts Release build passed with zero warnings/errors; the consumer-authority test passed 1/1 in 170.967 seconds; the current-subject guard passed 2/2; the lifecycle guard passed 1/1; successor Python tests passed 118/118.
- On EventStore `ef6e6886`, CI [38142379376](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/38142379376) failed only build-and-test's tracked-content secret gate; Contracts passed. P1R Preparation [38142379130](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/38142379130) failed one bounded external-SIGINT observation test.
- On EventStore `39962529`, CI [38143468044](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/38143468044) passed Contracts but failed only the tracked-content secret gate; P1R Preparation [38143467893](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/38143467893) passed 35/35. The timing correction is in this revision.
- On pushed EventStore `e26173a459073ad601322f7c5645d1bde951680d`, the Server.Tests Release build passed with zero warnings/errors; historical sealed-source guards passed 4/4; P1R executor tests passed 57/57; and the full tracked-content secret test passed 1/1 in 4m 16s. Exact-source [CI 38145298825](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/38145298825) and [P1R Preparation 38145298673](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/38145298673) passed. [Integration Tests 38145298656](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/38145298656) failed only `TombstoningLifecycleTests.Lifecycle_TerminateAfterSnapshotInterval_RehydratesAsTerminated` (`LastSnapshotSequence=0`, expected at least 14) and `EventPersistenceIntegrationTests.ProcessCommandAsync_ExceedsSnapshotInterval_CreatesSnapshot` (Redis snapshot state absent). The same two failed on earlier runs for `b57310fb` and `d35ab1`; the last observed green integration run is `25f063e9`.
- On pushed EventStore `3099cb27de12586080f32a767deaaa47aa66634c`, the revised live-sidecar tombstoning and persistence tests each passed 1/1 locally, as did the existing P1R persisted snapshot restore/restart test and two qualified folded-snapshot source tests. LiveSidecar and Server.Tests Release builds passed with zero warnings/errors. Positive qualified automatic snapshot creation through a real live sidecar is still an integration coverage gap; the candidate-specific published-package snapshot/restore obligation stays open. Exact-source [CI 38146720449](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/38146720449), [Integration Tests 38146720145](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/38146720145), and [P1R Preparation 38146720151](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/38146720151) passed as push runs on live `main` at `3099cb27`.

**Matrix audit:** The local tests cover 3.120.0 refusal, successor identity substitution, protected old-route plan, missing assertions, and `rollback=null` selection. The candidate archive/source identity row and candidate stale-fence, logical-alias, persisted restore/restart, and PostgreSQL/Dapr runtime observations cannot pass until a published successor exists. No case receipt or role acceptance is claimed from source tests.

**Commands:**
- `python3 -m unittest discover -s tools/tests -p 'test_p1r_published_*.py'` in EventStore — pass.
- Run Contracts tests individually in Release package mode; require green exact-source CI.
- Run isolated package execution and independent `prepare`/`validate`; inspect receipts and gate values.
- `git diff --check` in changed repositories — pass.
