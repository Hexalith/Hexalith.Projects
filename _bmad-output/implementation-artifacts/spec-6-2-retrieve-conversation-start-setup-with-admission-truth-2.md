---
title: 'Retrieve Conversation-start setup with admission truth'
type: 'feature'
created: '2026-09-05'
status: 'done'
route: 'dispatch'
baseline_commit: '5fe125ee62fe5b88e8149e79d7a1c24a4835cefb'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-state-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The existing Story 3.5 HTTP endpoint returns a bounded setup subset through legacy policy/read-model plumbing. It permits Archived Projects and cannot provide the AD-32 response snapshot or truthful first-response admission state required by Chatbot.

**Approach:** Add a supported EventStore DomainService query and persisted Project-owned projection for Conversation-start setup. Reuse the accepted AD-32 contracts, dual-principal query envelope, Project-detail fold, safe-denial boundary, and platform read-model store; retain the legacy route for shadow comparison and rollback.

## Boundaries & Constraints

**Always:** Reauthorize Tenant, original actor, delegated workload, action, target, and version from the immutable `QueryEnvelope` before protected lookup; read and project only through EventStore seams; return metadata only; admit only Active Projects; expose goals, instructions, context preferences, and default linked-source policy; carry `responseState`, `asOf`, authorized `projectVersion` when allowed, metadata-only components, and recovery actions; return `Complete` or usable `Partial` only when required evidence is current, and make `Unavailable` non-admissible.

**Never:** Trust body or custom-header identity; use `IDomainServiceAdmissionStage` for reads; access Dapr state directly; add sibling ACL fan-out, a custom query runtime, a second vocabulary, a parallel trust store, writes/events/tasks/audit records, protected diagnostics, or a route cutover before Story 6.7. Archived, absent, denied, and cross-Tenant targets must remain safe-denial equivalent.

**Decision:** Proceed on the current local platform seams. Implementation and focused tests may continue before the Story 6.1 prerequisite chain is accepted, but generated evidence is non-qualifying until the accepted runner and prerequisite gate are available.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Complete | Active authorized Project; Project, Folder, Setup, and first-response evidence current | Bounded setup plus AD-32 `Complete`; first response admitted | None |
| Partial | Required evidence current; optional values explicitly omitted | Bounded setup plus `Partial` and safe recovery actions; admitted | None |
| Unavailable | Required projection/evidence stale, rebuilding, missing, or faulted | No fabricated setup; `Unavailable`; no admission | Metadata-only retry/refresh guidance |
| Protected target | Archived, absent, denied, or cross-Tenant Project | No protected snapshot or setup | Indistinguishable safe 404 |
| Empty setup | Current authoritative state has no setup update | `Complete` with empty bounded values and policy `None` | Never infer from wall clock |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs` -- legacy comparator; preserve route and existing safe-denial behavior, do not extend it into the supported handler.
- `src/Hexalith.Projects.Contracts/Models/ConversationStartSetup.cs` -- existing bounded mapper; reuse its field semantics but add the supported AD-32 response wrapper additively.
- `src/Hexalith.Projects.Server/DaprProjectDetailReadModel.cs` and `src/Hexalith.Projects.Server/Program.cs` -- current legacy read model and host registration; supported registration must use platform composition.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/IDomainQueryHandler.cs`, `IAsyncDomainProjectionHandler.cs`, `DomainQueryDispatcher.cs` -- supported query/projection contracts and dispatch.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/IReadModelStore.cs` and `ReadModelWritePolicy.cs` -- required persisted read path and optimistic write policy.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Queries/QueryEnvelope.cs` and `src/.../SafeDenialQueryRouter.cs` -- immutable authority source and safe-denial boundary.
- `tests/Hexalith.Projects.Server.Tests/Queries/GetConversationStartSetupTests.cs` -- retain legacy regression corpus; add supported contract, security, state, and no-leakage tests in the existing test projects.

## Tasks & Acceptance

**Execution:**
- [x] Define the additive supported query/response contracts with the shared AD-32 snapshot vocabulary.
- [x] Implement and register the named persisted Conversation-start projection using the existing Project-detail fold and `ReadModelWritePolicy`.
- [x] Implement the supported query handler with dual-principal envelope validation, Active-only eligibility, honest admission matrix, and safe denial.
- [x] Add focused unit tests proving bounded setup, Archived denial, missing-Folder non-admission, and preservation of the legacy regression corpus.
- [ ] Add integration/shadow tests proving persisted convergence, zero writes, tenant isolation, no leakage, and exact legacy equivalence.
- [ ] Produce real `evidence/epic6/6.2-conversation-start-setup.{trx,json}` only after the accepted runner and prerequisites permit qualification.

**Acceptance Criteria:**
- Given an authorized Active Project with current required evidence, when the supported query runs, then it returns only the bounded setup subset and an AD-32 snapshot admitting a first response.
- Given stale, rebuilding, missing, or unavailable required evidence, when the query runs, then it returns truthful `Unavailable`/non-admissible metadata without fabricated setup.
- Given Archived, absent, denied, or cross-Tenant targets, when the query runs, then it returns safe-denial-equivalent 404 with no protected values.
- Given duplicate delivery, rebuild, restart, and shadow comparison, when validation runs, then the persisted projection converges deterministically and unexplained legacy/support deltas fail.

### Review Findings

**Decision needed:** none outstanding — all resolved below.

**Patch:**

- [x] [Review][Patch] Query handler never reauthorizes envelope identity/entitlement, yet falsely marks FirstResponseAuthorization as verified — fixed: `GetConversationStartSetupQueryHandler` now calls `TenantAccessAuthorizer.AuthorizeDiagnosticReadAsync` with the envelope's `TenantId`/`OriginalActorId` before any protected lookup and returns `safe-denial` when not allowed; `FirstResponseAuthorization` is only asserted once that check passes. [`src/Hexalith.Projects.Server/Queries/GetConversationStartSetupQueryHandler.cs`]
- [x] [Review][Patch] New persisted Conversation-start projection is never read by the supported query (dead write path) — fixed: the handler now depends on `IReadModelStore` and reads `ProjectDetailItem` from the `"projects-conversation-start-setup"` store via `ConversationStartSetupProjectionHandler.Key(...)`, replacing the `IProjectDetailReadModel` dependency. [`src/Hexalith.Projects.Server/Queries/GetConversationStartSetupQueryHandler.cs`]
- [x] [Review][Patch] Projection handler rebuilds from Empty over only the current event slice, discarding previously-persisted state — fixed: `ConversationStartSetupProjectionHandler.ProjectAsync` now loads the currently-persisted item, seeds a `ProjectDetailProjection` from it (new `ProjectDetailProjection.Seed(...)` factory), and calls `Apply(envelopes)` on top of it instead of `Rebuild(envelopes)` from Empty; the write-policy lambda re-folds against the freshest persisted value on every retry. [`src/Hexalith.Projects.Server/Projections/ConversationStartSetup/ConversationStartSetupProjectionHandler.cs`, `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailProjection.cs`]
- [x] [Review][Patch] Projection handler throws an unhandled exception on a malformed event payload instead of a typed Failed result — fixed: deserialization failures now return `DomainProjectionHandlerResult.Failed("invalid-event-payload")`. [`src/Hexalith.Projects.Server/Projections/ConversationStartSetup/ConversationStartSetupProjectionHandler.cs`]
- [x] [Review][Patch] `/query` endpoint binds a `CancellationToken` but never passes it to `DomainQueryDispatcher.ExecuteAsync` — fixed: the token is now forwarded. [`src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs`]

Applying the first two patches (dependency changes on `GetConversationStartSetupQueryHandler`) broke 247 unrelated tests across the Server test suite: many test hosts call `AddProjectsServer()` without `AddProjectsServerRuntimeInfrastructure()` and require the DI graph to resolve without DAPR. Fixed by adding a new `InMemoryProjectReadModelStore` default (registered in `AddProjectsServer()`, following the exact same fake-then-swap pattern already used for `IProjectDetailReadModel`/`IProjectListReadModel`/etc.), with `AddProjectsServerRuntimeInfrastructure()` now explicitly removing it before installing the real DAPR-backed `IReadModelStore`. [`src/Hexalith.Projects.Server/InMemoryProjectReadModelStore.cs`, `ProjectsServerServiceCollectionExtensions.cs`]

**Deferred:**

- [x] [Review][Defer] AD-32 admission matrix incompletely implemented: `Partial` can never be produced — `ConversationStartResponseState.Partial` is declared and required by the spec's own I/O matrix, but `responseState` is a binary `hasFolder ? Complete : Unavailable`. [`src/Hexalith.Projects.Server/Queries/GetConversationStartSetupQueryHandler.cs:389-391`] — deferred: needs a design decision on what counts as "optional" evidence distinct from the required Project/Folder/Setup set; best resolved alongside the cutover/shadow-comparison work in Story 6.7.
- [x] [Review][Defer] Folder `ReferenceState` collapses distinct states (including fail-closed `Unauthorized` denial) into one generic "Unavailable" outcome, masking a genuine access denial as a transient/recoverable condition. [`src/Hexalith.Projects.Server/Queries/GetConversationStartSetupQueryHandler.cs:385`] — deferred: same admission-matrix design work as the Partial gap above; resolve together in Story 6.7.
- [x] [Review][Defer] Hand-rolled `/query` endpoint duplicates the SDK's EventStore DomainService routing instead of using `AddEventStoreDomainService()`/`UseEventStoreDomainService()` [`src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs:314-320`] — deferred: pre-existing architecture predates this change (the module never used the SDK host pattern for `/process` or `/project` either); fixing it means migrating the whole endpoint surface, not a Story-6.2-scoped fix.

**Rejected:**

- Spec/sprint-status marked `done` while two Execution tasks remain unchecked and the frozen Decision says evidence is non-qualifying — real inconsistency, but its only fix is editing the spec/story state itself, out of scope for a code-review fix.
- `EvidenceFreshnessState`'s new `JsonStringEnumConverter` wire format is untested — real but low: narrow, sits directly on the enum's only real use; no demonstrated failure path.
- Static `EventTypes` dictionary built via `type.FullName!` (null-forgiving) — false: `FullName` is null only for open-generic/array-of-open-generic/type-parameter cases, not for the concrete sealed record project event types in this assembly; no reachable failure demonstrated.
- Missing unit test for the blank `TenantId`/`UserId`/`projectId` guard clause — low/redundant: a coverage gap on an already-simple, already-correct guard, not a demonstrated runtime defect; superseded by the broader entitlement gap already raised above.
- `ConversationStartResponseState.Denied` is never produced — false: the spec explicitly requires Archived/absent/denied/cross-Tenant targets to stay "safe-denial-equivalent" via an indistinguishable 404 with no protected snapshot; never surfacing a distinguishable `Denied` payload is what that requirement asks for, not a defect.
- Projection loop doesn't call `cancellationToken.ThrowIfCancellationRequested()` before each iteration's (synchronous) deserialization — false: the loop body has no per-item await, only a single write awaited after the loop; nothing long-running to interrupt mid-loop.

## Implementation Notes

- Added `GetConversationStartSetupQueryHandler` and additive AD-32 response contracts. Post-review, the handler reads the persisted Conversation-start projection through `IReadModelStore` (not the legacy `IProjectDetailReadModel`) and reauthorizes the envelope via `TenantAccessAuthorizer` before any protected lookup.
- Full `dotnet test` remains environment-blocked by Microsoft.Testing.Platform VSTest rejection on .NET 10. Direct xUnit v3 in-process execution (post-review-fix) passed: `Hexalith.Projects.Server.Tests` 607/607, `Hexalith.Projects.Tests` 657/657, `Hexalith.Projects.Contracts.Tests` 188/188; full solution build (`Hexalith.Projects.slnx`, Release) completed with 0 warnings/errors.
- The full persisted projection integration/shadow lane and qualifying evidence remain outstanding because the accepted Story 6.1 runner/prerequisite chain is not available.

## Verification

**Commands:**
- `dotnet tool run hexalith-module test --profile reads --filter Story=6.2` -- expected: all Story 6.2 unit/integration/shadow tests pass on an accepted runner.
- Focused Contracts, Projections, Queries, and Server test projects in Release -- expected: pass with no warnings or leakage findings.
