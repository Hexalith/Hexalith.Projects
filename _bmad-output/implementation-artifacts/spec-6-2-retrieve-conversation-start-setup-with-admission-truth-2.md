---
title: 'Retrieve Conversation-start setup with admission truth'
type: 'feature'
created: '2026-09-05'
status: 'in-progress'
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
- [ ] Define the additive supported query/response contracts with the shared AD-32 snapshot vocabulary.
- [ ] Implement and register the named persisted Conversation-start projection using the existing Project-detail fold and `ReadModelWritePolicy`.
- [ ] Implement the supported query handler with dual-principal reauthorization, Active-only eligibility, honest admission matrix, and safe denial.
- [ ] Add unit/integration/shadow tests proving persisted convergence, zero writes, tenant isolation, no leakage, Archived divergence, and `Complete|Partial` admission.
- [ ] Produce real `evidence/epic6/6.2-conversation-start-setup.{trx,json}` only after the accepted runner and prerequisites permit qualification.

**Acceptance Criteria:**
- Given an authorized Active Project with current required evidence, when the supported query runs, then it returns only the bounded setup subset and an AD-32 snapshot admitting a first response.
- Given stale, rebuilding, missing, or unavailable required evidence, when the query runs, then it returns truthful `Unavailable`/non-admissible metadata without fabricated setup.
- Given Archived, absent, denied, or cross-Tenant targets, when the query runs, then it returns safe-denial-equivalent 404 with no protected values.
- Given duplicate delivery, rebuild, restart, and shadow comparison, when validation runs, then the persisted projection converges deterministically and unexplained legacy/support deltas fail.

## Implementation Notes

## Verification

**Commands:**
- `dotnet tool run hexalith-module test --profile reads --filter Story=6.2` -- expected: all Story 6.2 unit/integration/shadow tests pass on an accepted runner.
- Focused Contracts, Projections, Queries, and Server test projects in Release -- expected: pass with no warnings or leakage findings.
