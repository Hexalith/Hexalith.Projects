---
title: 'Complete 6.1-P2 Query Security and Projection Watermark Capability'
type: 'feature'
created: '2026-07-19'
status: 'in-progress'
review_loop_iteration: 0
baseline_commit: 'f9a563c96d7a6da7fe9507592437483996686cd3'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
  - '{project-root}/references/Hexalith.EventStore/_bmad-output/project-context.md'
  - '{project-root}/references/Hexalith.EventStore/_bmad-output/implementation-artifacts/spec-6-1-p2-dual-principal-query-envelope-safe-denial.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** EventStore already ships the reviewed dual-principal query fields and opt-in safe-denial boundary, but root work package 6.1-P2 remains incomplete: delegated queries carry no delegation identifier, projection wire events discard persisted `GlobalPosition`, cursor scopes have no supported watermark binding, and no finite P2 evidence/pin/rollback record exists.

**Approach:** Preserve the completed query/safe-denial design, add an optional immutable delegation identifier, and expose committed event positions through an additive projection contract plus a canonical cursor-scope binding. Prove legacy compatibility, persisted replay/restart behavior, and denial isolation, then produce the owner handoff needed for a published pin and P2 acceptance.

## Boundaries & Constraints

**Always:** Keep all public changes additive and serialization-tolerant; append query-envelope data members; derive `DelegationId` only from authenticated RFC 8693 `act.sub`; treat absent/malformed delegation evidence as unknown and let protected consumers fail closed. A projection watermark is the highest positive persisted `GlobalPosition` in the event slice durably applied to that read model; gaps remain valid and the value never asserts contiguous global consumption. Bind cursors with invariant decimal formatting and reject non-positive/unknown watermarks when authoritative binding is required.

**Ask First:** Creating a release, changing EventStore/Builds/root pins, or selecting the rollback triple requires explicit repository-owner approval after local evidence is green. Any different delegation claim mapping, watermark meaning, or alteration to existing non-opted-in denial behavior also requires approval.

**Never:** Do not use `IGlobalPositionActor.GetCurrentAsync()` as projection truth, infer a watermark from time/local sequence/ETag, expose DAPR internals, renumber persisted positions, rewrite history, weaken safe denial, absorb P0/P3/P4, or claim P2 done without G-4 persisted evidence and exact accepted pins.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Delegated query | Valid `sub`, workload claims, and `act.sub` | Immutable envelope preserves actor, workload, delegation ID, scopes, audience, Tenant, and correlation separately | Missing/malformed `act.sub` stays unknown; protected consumer denies |
| Projection delivery | Persisted events with positive, gapped global positions | Projection receives each exact position and persists the highest successfully applied value with its read model | Zero is legacy/unknown; negative values are rejected |
| Cursor replay | Cursor bound to caller/filter and persisted watermark | Decode succeeds only for the identical scope and watermark | Changed/unknown watermark fails closed as wrong scope |
| Denied or absent read | Opted-in forbidden, cross-Tenant, or either not-found form | Same canonical external `404` shape; no existence/content disclosure | Non-opted routes remain unchanged |
| Restart/rebuild | Persisted read model and event history replayed after process restart | Rebuilt state and watermark equal the pre-restart result | Allocation gaps are preserved, never filled or reused |

</frozen-after-approval>

## Code Map

- `references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Queries/QueryEnvelope.cs` and query routing chain -- append and carry `DelegationId` without changing legacy callers.
- `references/Hexalith.EventStore/src/Hexalith.EventStore/Authorization/DualPrincipalClaimsHelper.cs` -- parse bounded `act.sub` evidence at the authenticated gateway boundary.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Projections/ProjectionEventDto.cs` -- expose persisted `GlobalPosition` additively.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Projections/ProjectionEventWireBuilder.cs` -- forward the stored position instead of synthesizing it.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Queries/QueryCursorScope.cs` -- add canonical positive-watermark scope binding.
- `references/Hexalith.EventStore/tests/` -- contract, routing, cursor, safe-denial, persistence, replay, and restart evidence.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Queries/QueryEnvelope.cs`, `...Server/Pipeline/Queries/SubmitQuery.cs`, `.../Authorization/DualPrincipalIdentity.cs`, `DualPrincipalClaimsHelper.cs`, `.../Controllers/QueriesController.cs`, `...Server/Queries/QueryRouter.cs`, and `.../Queries/HandlerAwareQueryRouter.cs` -- add optional `DelegationId` end to end while preserving legacy defaults.
- [x] `references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Projections/ProjectionEventDto.cs`, `...Server/Projections/ProjectionEventWireBuilder.cs`, and `...Client/Queries/QueryCursorScope.cs` -- forward exact persisted positions and provide a named watermark binding with validation and invariant formatting.
- [ ] `references/Hexalith.EventStore/tests/Hexalith.EventStore.Contracts.Tests/`, `...Client.Tests/`, `...QueryRouting.Tests/`, `...Server.Tests/`, and `...IntegrationTests/` -- cover the matrix, including old payloads, gapped positions, cursor tamper/staleness, cross-Tenant equivalence, rebuild, and actor/process restart.
- [ ] `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/6-1-p2-query-security-projection-capability-acceptance-record.md` -- record exact public signatures, source/package candidate, commands/results, G-4 artifacts, residual risks, approvals, and the owner-approved rollback procedure; update `_bmad-output/implementation-artifacts/sprint-status.yaml` only after every gate is accepted.

**Acceptance Criteria:**
- Given legacy query/projection payloads, when current code deserializes and routes them, then behavior remains unchanged with null delegation and unknown watermark defaults.
- Given committed events and a successful durable projection write, when a cursor is issued, then its scope binds the exact persisted read-model watermark without consulting allocator state.
- Given replay, duplicate delivery, gaps, rebuild, or restart, when the same persisted history is projected, then state and watermark converge deterministically.
- Given the published candidate and G-4 fixture, when identity, denial, persistence, and restart lanes run, then machine evidence names the exact revision/package and passes cross-Tenant negative controls.
- Given owner acceptance and a rollback trigger, when the documented rollback is applied, then prior data remains readable, watermark-bound cursors fail closed, and P2/P3/P4/Story 6.1 return to blocked.

## Spec Change Log

- 2026-07-19: Implemented and locally verified the additive EventStore capability surface. Preserved the released 3.77.2 constructor/deconstruction entry points and frozen v1 delivery fingerprints. The test task remains open because an actual process-restart/G-4 lane is unavailable; publication, accepted pins, rollback selection, the owner record, and root P2 acceptance also remain open.

## Design Notes

`GlobalPositionActor` allocates before aggregate commit, so its current value can be ahead of durable events. Only positions carried by committed event envelopes and included in the same successful read-model write are admissible projection watermarks. The platform should expose this fact, not promise gap-free global completion.

## Verification

**Commands:**
- `dotnet build Hexalith.EventStore.slnx --configuration Release` -- expected: clean warnings-as-errors build.
- `dotnet test tests/Hexalith.EventStore.Contracts.Tests/Hexalith.EventStore.Contracts.Tests.csproj` -- expected: additive query/projection serialization compatibility passes.
- `dotnet test tests/Hexalith.EventStore.Client.Tests/Hexalith.EventStore.Client.Tests.csproj` -- expected: canonical watermark scope and wrong-scope cases pass.
- `dotnet test tests/Hexalith.EventStore.QueryRouting.Tests/Hexalith.EventStore.QueryRouting.Tests.csproj` -- expected: both handler and actor router identity paths pass.
- `dotnet test tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj` -- expected: claim, safe-denial, wire, persistence, replay, and restart cases pass.
- `dotnet tool run hexalith-module test --profile reads --filter Story=6.1-P2` -- expected: G-4 emits passing persisted/restart/cross-Tenant evidence for the exact candidate pin.

**Local results (2026-07-19):**
- EventStore working-tree base during final verification: `5ba794b0459bce64afd419f45d71b1d52b303c00`; the capability remains an uncommitted local change. The local `origin/main` tracking ref later advanced to non-overlapping commit `442447599da624c4a0d16f24070ca2cf09c752b9`; no synchronization was performed.
- `dotnet build Hexalith.EventStore.slnx --configuration Release --no-restore -m:1 /nr:false -p:UseSharedCompilation=false -p:NuGetAudit=false` -- passed with 0 warnings and 0 errors.
- Client tests -- 679 passed; QueryRouting tests -- 7 passed; Server tests -- 2,755 passed and 25 skipped, with 0 failures.
- Watermark rebuild integration filter -- 2 passed, proving a same-write read-model/watermark value, gapped and mixed legacy positions, duplicate delivery, provider reconstruction, and explicit full-replay rebuild convergence.
- Contracts tests -- 742 passed and 3 unrelated repository-governance tests failed because prohibited nested submodules are uninitialized/stale; the focused projection contract lane passed. No nested submodule was initialized or updated.
- `dotnet-inspect` confirmed that the prior `ProjectionEventDto`, `QueryEnvelope`, and `SubmitQuery` constructor/deconstruction signatures remain present beside the additive members.
- `git diff --check` -- passed.

**Open gates:**
- The Builds-owned 6.1-P0/G-4 runner remains `in-progress` and unavailable from this workspace, so an honest OS-process restart, persisted cross-Tenant fixture, and exact-candidate evidence cannot run yet. The required command exited 1 with `Cannot find a tool in the manifest file that has a command named 'hexalith-module'.`
- No release, package/source pin update, rollback selection, acceptance record, or P2 sprint-status transition has been authorized or performed.
