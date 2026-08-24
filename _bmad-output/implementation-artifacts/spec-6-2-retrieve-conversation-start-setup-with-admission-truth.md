---
title: 'Retrieve Conversation-start setup with admission truth'
type: 'feature'
created: '2026-08-24'
status: 'awaiting-operator'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-state-instructions.md'
warnings: [multiple-goals, oversized]
operator_actions:
  - 'Accept and record the exact-current 6.1-P0, 6.1-P1, 6.1-P1R, 6.1-P2, 6.1-P3, and 6.1-P4 prerequisite chain, including G-4 runner evidence, G-5 identity evidence, immutable pins, rollback, and every required owner decision.'
  - 'Land Story 6.1 shared AD-32 contracts, persisted Project detail read models, dual-principal read authorization, supported DomainService query composition, and safe-denial gateway wiring, then publish their exact reusable type and route names.'
  - 'Resolve and record the Story 6.2 scope disposition for the Hexalith.Projects.UI.Contracts split and the canonical null-setup admission semantics if the accepted Story 6.1 contracts do not settle them.'
  - 'Provide an independently green readiness rerun and an executable reads profile with a module manifest capable of producing real persisted Story 6.2 evidence.'
deferred: []
---

<intent-contract>

## Intent

**Problem:** The existing Story 3.5 endpoint returns a bounded Conversation-start setup through legacy HTTP/read-model plumbing, permits Archived Projects, and exposes only legacy lifecycle/freshness fields. It cannot prove the AD-32 Project, Folder, Setup, authorization, version, and recovery truth required to admit a Chatbot first response.

**Approach:** Add an additive supported `IDomainQueryHandler` path over EventStore-managed persisted read models, reusing the accepted Story 6.1 AD-32 and dual-principal seams. Preserve the legacy route for shadow comparison, and complete the still-binding UI descriptor assembly split without widening either public package or runtime scope.

## Boundaries & Constraints

**Always:** Derive Tenant, original actor, authenticated workload, delegation, scopes, audience, action, target, and version authority from the immutable `QueryEnvelope`; reauthorize before protected validation or lookup; use `IAsyncDomainProjectionHandler`, `IReadModelStore`, and `ReadModelWritePolicy`; return metadata only; keep the operation read-only; treat only Active Projects as eligible; admit a first response only for `Complete|Partial`; and preserve exact safe-404 equivalence for Archived, absent, denied, and cross-Tenant targets.

**Block If:** The accepted Story 6.1 contracts/read models/authorization seam are unavailable; gateway safe-denial remains unregistered; handler-computed responses cannot carry the accepted freshness proof; required component/reason/recovery vocabulary is unsettled; null setup has no approved current-empty versus unavailable meaning; or the UI.Contracts scope has no authoritative disposition. These conditions require operator resolution and therefore leave this story `awaiting-operator`, never `blocked`.

**Never:** Write `sprint-status.yaml`; trust payload or custom headers for identity; use `IDomainServiceAdmissionStage` for reads; add direct Dapr state access, a custom query runtime, a second AD-32 vocabulary, live sibling ACL fan-out, or a parallel trust store; edit generated files by hand; expose Tenant/actor authority, audit metadata, transcripts, prompts, paths, file or memory content, tokens, secrets, or raw owner failures; switch or retire the legacy route before Story 6.7; or write events, tasks, maintenance audit, resolution choices, or sibling state.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Eligible complete | Active authorized Project; Project, exactly one Folder, Setup/defaults, and first-response authorization evidence are current | Bounded goals, instructions, source preferences, linked-source policy, and one AD-32 `Complete` snapshot bound to `asOf` and authorized `projectVersion`; first response admitted | No error expected |
| Eligible partial | All required evidence is current and every optional omission is explicit | Same bounded subset with `Partial`, component omissions, and applicable recovery actions; first response admitted | No protected diagnostic detail |
| Required evidence non-current | Project, Folder, Setup, or authorization evidence is stale, rebuilding, missing, or unavailable | `Unavailable`; no fabricated setup and no first-response admission | Metadata-only retry/refresh guidance |
| Protected target | Archived, absent, denied, cross-Tenant, or unverifiable target | No protected snapshot | Observationally identical safe `404` |
| Null setup | Persisted authoritative state has no explicit setup update | Apply the operator-approved current-empty or unavailable rule consistently in projection, handler, shadow normalization, and tests | Never infer from wall clock or legacy behavior alone |
| Fault or replay | Duplicate projection dispatch, rebuild, restart, store fault, or owner-evidence fault | Deterministic state convergence or honest `Unavailable`; no write outside the read model projection | Preserve cancellation; expose no raw exception |

</intent-contract>

## Code Map

- `src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs:63` -- legacy HTTP orchestrator; preserve as the shadow comparator input. It authorizes before validation but passes empty reference evidence and cannot establish admission truth.
- `src/Hexalith.Projects.Contracts/Models/ConversationStartSetup.cs:59` -- reusable bounded subset and `FromContext` mapper; lacks AD-32 state, version, components, and recovery actions.
- `src/Hexalith.Projects/Projections/ConversationStartSetup/ConversationStartSetupProjector.cs:34` -- pure legacy subset mapper to reuse, not a persisted supported projection.
- `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailProjection.cs:41` -- deterministic existing fold for setup, Folder, lifecycle, and aggregate sequence; extract/reuse rather than reinterpret events.
- `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailItem.cs:32` -- legacy evidence fields that the accepted supported detail read model must persist with freshness/provenance.
- `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs:350` -- HTTP-bound legacy authorization chain; preserve it but do not inject it into a domain query handler.
- `src/Hexalith.Projects.Contracts/Queries/` -- target for the versioned supported query and response wrapper, reusing Story 6.1 shared AD-32 types.
- `src/Hexalith.Projects/Queries/Handlers/` -- target for the supported Conversation-start handler and Projects-owned dual-principal policy composition.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/IDomainQueryHandler.cs:18` -- supported query seam and discovery contract.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/IAsyncDomainProjectionHandler.cs:8` -- supported incremental projection seam.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/IReadModelStore.cs:23` -- required persisted read store; direct Dapr access is forbidden.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/ReadModelWritePolicy.cs:53` -- optimistic incremental/rebuild write policy.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Queries/QueryEnvelope.cs:158` -- immutable dual-principal authority source.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Queries/SafeDenialQueryRouter.cs:46` -- platform-owned opt-in safe-denial boundary that must be accepted and wired before completion.
- `src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj:20` and `src/Hexalith.Projects.Contracts/Ui/` -- current packable Contracts-to-UI dependency leak; move descriptors under the approved non-packable `Hexalith.Projects.UI.Contracts` boundary if that scope remains assigned here.
- `tests/Hexalith.Projects.Server.Tests/Queries/GetConversationStartSetupTests.cs:57` -- legacy security/leakage/no-sibling regression corpus; Archived and null-setup expectations require explicit supported-path disposition.
- `_bmad-output/test-artifacts/test-design-epic-6.md:176` -- canonical E6.2-U01/A01/A02/A03 scenarios; line 214 adds shadow equivalence.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.Projects.Contracts/Queries/GetConversationStartSetupQuery.cs` and accepted shared response files -- define an additive singleton query and bounded response wrapper; carry Project ID only as a target and reuse the exact Story 6.1 AD-32 snapshot/component/recovery contracts.
- `src/Hexalith.Projects/Projections/ConversationStartSetup/ConversationStartSetupProjectionHandler.cs` -- implement the named persisted projection with `IAsyncDomainProjectionHandler`, the existing Project-detail fold, and `ReadModelWritePolicy`; persist Tenant-scoped eligibility, exactly-one-Folder evidence, Setup/defaults, lifecycle, aggregate version, freshness/provenance, and last-verified evidence without a custom journal.
- `src/Hexalith.Projects/Queries/Handlers/GetConversationStartSetupQueryHandler.cs` -- reauthorize the full `QueryEnvelope`, read only through `IReadModelStore`, reject non-Active targets through the safe-denial sentinel, apply the approved admission-state matrix, and return `QueryResult` with truthful metadata.
- `src/Hexalith.Projects/Hexalith.Projects.csproj`, `src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj`, and `src/Hexalith.Projects.Server/Program.cs` -- consume only the accepted central `Hexalith.EventStore.DomainService` composition and Story 6.1 host seam; keep legacy routes mapped and avoid duplicate public endpoints.
- `src/Hexalith.Projects.Testing/Reads/ConversationStartShadowComparator.cs` -- compare eligible legacy and supported results using only an owner-approved finite normalization for AD-32 additions; reject every unexplained delta and emit no protected values.
- `src/Hexalith.Projects.UI.Contracts/Hexalith.Projects.UI.Contracts.csproj`, `src/Hexalith.Projects.Contracts/Ui/`, `src/Hexalith.Projects.UI/Program.cs`, and `Hexalith.Projects.slnx` -- if operator disposition retains this scope, move FrontComposer descriptors/domain discovery into the non-packable UI contract assembly and remove UI/runtime dependencies from the packable Contracts package without changing descriptor semantics.
- `tests/Hexalith.Projects.Contracts.Tests/`, `tests/Hexalith.Projects.Tests/Queries/`, `tests/Hexalith.Projects.Tests/Projections/`, and `tests/Hexalith.Projects.Server.Tests/` -- cover the complete matrix, dual-principal reauthorization, persisted convergence, zero writes, leakage, and exact safe-denial equivalence; retain legacy regressions separately.
- `tests/Hexalith.Projects.Integration.Tests/` and `evidence/epic6/` -- execute the real gateway-to-`/query`-to-handler-to-store chain, restart/fault/shadow scenarios, and persisted before/after assertions; generate actual `6.2-conversation-start-setup.trx` and `.json` rather than hand-authored claims.

**Acceptance Criteria:**
- Given an authenticated delegated Chatbot and an Active eligible Project with current required evidence, when the supported query executes, then it returns only the bounded setup plus one version-bound AD-32 `Complete|Partial` snapshot and admits the first response only for those states.
- Given required Project, Folder, Setup, or authorization evidence is non-current, when the supported query executes, then it returns honest `Unavailable`, blocks first-response use, supplies only applicable recovery actions, and fabricates no setup or timestamp.
- Given an Archived, absent, denied, or cross-Tenant target, when the query traverses the authenticated gateway and domain handler, then every caller-visible status/header/body/timing category and metadata-only diagnostic is indistinguishable safe `404`.
- Given projection replay, duplicate delivery, rebuild, restart, or a store fault, when the supported read model is inspected, then persisted end state is deterministic or truthfully unavailable and no Project event, command, task, maintenance audit, resolution, sibling mutation, or direct repair occurred.
- Given legacy and supported paths coexist, when the canonical shadow corpus runs, then every eligible legacy field is equivalent after the finite approved AD-32 normalization, every other delta fails, and default routing remains legacy until Story 6.7.
- Given the UI contract split remains assigned to Story 6.2, when package/consumer tests run, then the packable Contracts package has no FrontComposer Shell/SourceTools, ASP.NET, Fluxor, or Fluent UI dependency, `Hexalith.Projects.UI.Contracts` is non-packable, and UI discovery/navigation/vocabulary behavior remains equivalent.
- Given the approved G-4 runner and exact repository pins, when the Story 6.2 reads profile runs, then focused/unit/contract/security/persisted/shadow tests pass and the TRX/JSON evidence binds actual commands, revisions, state hashes, results, and zero-write proof without secrets or denied identifiers.

## Spec Change Log


## Review Triage Log


## Design Notes

The supported response should wrap or compose the existing bounded `ConversationStartSetup` values with the accepted shared AD-32 snapshot rather than mutate the legacy OpenAPI DTO in place. This preserves Story 3.5 client compatibility and gives Story 6.7 a finite shadow/cutover boundary. `IDomainServiceAdmissionStage` is deliberately excluded: it gates command pre-commit processing and does not express read/first-response admission.

## Verification

**Commands:**
- `dotnet restore Hexalith.Projects.slnx` -- expected: restore succeeds with central versions and no inline dependency changes.
- `dotnet build Hexalith.Projects.slnx --configuration Debug` -- expected: zero warnings and errors.
- `dotnet test tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj --configuration Debug` -- expected: supported and legacy contracts pass.
- `dotnet test tests/Hexalith.Projects.Tests/Hexalith.Projects.Tests.csproj --configuration Debug` -- expected: projection, handler, admission-state, replay, leakage, and zero-write cases pass.
- `dotnet test tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Debug` -- expected: safe-denial, gateway composition, legacy regression, and shadow cases pass.
- `dotnet test tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj --configuration Debug` -- expected: real persisted boundary, restart, fault, and two-instance scenarios pass or report an exact environment blocker.
- `dotnet tool run hexalith-module test --profile reads --filter Story=6.2` -- expected: produces real `evidence/epic6/6.2-conversation-start-setup.trx` and `.json` with passing persisted and zero-write evidence.
- `pwsh tests/tools/run-package-dependency-gate.ps1` -- expected: Contracts package is UI-clean and the UI.Contracts assembly is non-packable when that scope is retained.
- `git diff --check` -- expected: no whitespace errors.

## Auto Run Result

Status: awaiting-operator
Agent-completable work: Production-authority validation, repository/context inspection, three independent subagent investigations, exact dependency-gap analysis, code/test mapping, and the implementation-ready post-gate plan are complete. No source code was changed because the accepted shared Story 6.1 seams and human-owned entry evidence are not present.

