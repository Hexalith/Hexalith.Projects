# Story 2.1: Conversation Reference Read ACL (Projects → Conversations)

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As the **Hexalith.Projects module** (acting on behalf of Hexalith.Chatbot),
I want a **tenant-scoped, fail-closed read path that lists the conversations belonging to a Project by querying `Hexalith.Conversations` by `ProjectId` behind a Projects-owned Anti-Corruption Layer (ACL)**,
so that **Project Context (FR-16) and project views can present linked conversations with freshness/trust signals — without copying transcript content or coupling Projects to EventStore/Conversations internals**.

This is the **Pattern A (query-by-back-reference) read MVP** from the technical research. It is **read-only discovery/hydration**; writing the durable link (FR-6 Link Conversation) and moving conversations (FR-7) are separate stories.

## Acceptance Criteria

1. **ACL surface.** `Hexalith.Projects.Server` exposes `IProjectConversationDirectory` with `ListForProjectAsync(ProjectId projectId, TenantId tenantId, CallerPrincipalId caller, PageRequest page, CancellationToken ct)` returning a **Projects-shaped** result (e.g. `ProjectConversationsPage`) — **not** Conversations' `ConversationListResult`.
2. **Discovery via the existing filter.** The adapter queries Conversations using `ConversationListFilterV1(ProjectId: …)` through the verified server path (`ListConversationsQuery` → `ConversationQueryHandler.ListAsync`), reached either by **(A1)** a new typed-client method `IConversationClient.ListConversationsAsync(ListConversationsQuery, …)` that forwards to the server handler, or **(A2)** an HTTP call to `ConversationReadApi`. Pick one; document which in Dev Agent Record.
3. **Translation.** Each `ConversationSummaryV1` is translated into a Projects view item carrying at minimum `ConversationId`, lifecycle/status, an optional display label, and a **Projects-owned freshness/trust signal** derived from `ProjectionTrustState` + `ProjectionFreshnessV1`. When project hydration is present, surface `ProjectReferenceHydrationV1.SafeLabel`/`SafeStatus` (never raw upstream data).
4. **Reference identity = a value, never an object.** The reference is a typed `ConversationId` (reuse `Hexalith.Conversations.Contracts.Identifiers.ConversationId`). No `ConversationState`, projection object, or transcript/message content crosses the ACL boundary.
5. **Tenant isolation (fail-closed).** A request scoped to tenant A never returns tenant B's conversations; cross-tenant / `Forbidden` / unauthorized → empty or hidden result, never a leak and never an exception that reveals existence. Authorization is decided from the caller principal + tenant scope; a JWT tenant claim alone is **not** trusted.
6. **Degraded reads are surfaced, not hidden.** `Stale`/`Rebuilding`/`Unavailable`/`Redacted`/`Forbidden`/`MixedGeneration` map to an explicit Projects trust signal; the project view must be able to show "may be incomplete/rebuilding" rather than presenting degraded data as `Current`.
7. **No leakage of Conversations types.** Outside the ACL (Projects domain logic, projections, UI/contracts), only Projects-owned types appear. `Hexalith.Conversations.*` types are confined to `Projects.Server` (the ACL).
8. **Read-only.** No Conversations command is dispatched; Conversations state is not mutated by this story.
9. **Tier 1 tests (pure, fast).** Cover: (a) translator maps every `ProjectionTrustState` value and echoes `ProjectId`/`ConversationId`; (b) empty result and paging continuation; (c) tenant-isolation negative paths (tenant-A request never yields tenant-B rows; forbidden → hidden/empty). No Dapr, network, containers, or browser.

## Tasks / Subtasks

- [x] **Task 1 — Identifier & result boundary** (AC: 1, 4, 7)
  - [x] In `Projects.Contracts`, reference `Hexalith.Conversations.Contracts.Identifiers.ConversationId` (preferred) or define a thin `ConversationReference` value object wrapping the opaque string with eager validation (mirror `ProjectId.cs`).
  - [x] Define the Projects-shaped read DTOs: `ProjectConversationItem` (ConversationId, lifecycle/status, optional label, trust signal) and `ProjectConversationsPage` (items + page metadata + aggregate trust signal). Keep `Contracts` low-dependency (no Dapr/HTTP/EventStore-server).
- [x] **Task 2 — Close the typed-client gap** (AC: 2)
  - [x] Choose A1 or A2. For **A1**: add `ListConversationsAsync(ListConversationsQuery, CancellationToken)` to `IConversationClient`/`ConversationClient` forwarding to the existing `ConversationQueryHandler.ListAsync`. Confirm the real `ListConversationsQuery`/`ConversationPage` constructor shapes in `Hexalith.Conversations` source before coding (see Dev Notes). For **A2**: implement a typed read-API client against `ConversationReadApi`.
  - [x] If A1 touches `Hexalith.Conversations`, treat that as a scoped, additive change in that submodule (own commit, own tests there); do not mix submodule pointer churn into Projects work.
- [x] **Task 3 — ACL adapter + translator** (AC: 1, 3, 5, 6, 7, 8)
  - [x] Implement `IProjectConversationDirectory` + `ConversationsProjectConversationDirectory` in `Projects.Server`. Build `ConversationListFilterV1(ProjectId: …)`, call the client/read-API, translate `ConversationSummaryV1 → ProjectConversationItem`, and map `ProjectionTrustState`/`ConversationListResult` freshness → the Projects trust signal.
  - [x] Pass tenant + caller principal through; on `Hidden`/`Unavailable`/`Forbidden` return a Projects-safe empty/degraded page (fail closed). Map Conversations failures to Projects-safe errors; never rethrow raw upstream details.
- [x] **Task 4 — Expose for Project Context / project view** (AC: 1, 3, 6)
  - [x] Wire `IProjectConversationDirectory` into the Get-Project-Context conversation-reference read (FR-16) and/or the project conversation list, surfacing the trust signal. (If the FR-16 service does not exist yet, expose the directory via DI and add a thin query entry point; do not build the full context-assembly service here.)
- [x] **Task 5 — Tier 1 tests** (AC: 9)
  - [x] Translator tests (all trust states, empty, paging), and tenant-isolation negative-path tests using fakes. Place under `tests/Hexalith.Projects.Server.Tests` (and `Projects.Contracts.Tests` for the VO/DTOs).

## Dev Notes

### ⚠️ Prerequisite / dependency (read first)
`Hexalith.Projects` **has no code yet** — no submodule, no `.csproj`, no solution (confirmed: `Glob Hexalith.Projects/**/*.csproj` → none). This story assumes a **foundation slice exists** providing: the Projects solution + `Projects.Contracts`/`Projects.Server`/`Projects.ServiceDefaults`/`Projects.Testing` projects, DI/host wiring, and a **tenant-access service** (the equivalent of Conversations' `IConversationTenantAccessService`, which decides access from the local Tenants projection, not the JWT). If that foundation is not in place, **stop and create/confirm it first** (Epic 1). Do not silently bypass tenant access to make this story compile.

### Verified Conversations facts (from repo — safe to rely on)
- **`ProjectId` already rides on conversations and is immutable.** Set only on `ConversationCreated`; copied to `ConversationState.ProjectId`; **no re-parent event exists** (`ConversationMetadataUpdated` changes only Label/BusinessReference/Attributes). So the conversation↔project link is owned by Conversations and never changes after creation. _[Source: `Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Events/ConversationCreated.cs`; `.../State/ConversationState.cs`]_
- **Discovery already works server-side.** `ConversationListFilterV1` has a `ProjectId? ProjectId` exact-match filter (param index per `ConversationListFilterV1.cs:36`); `ConversationQueryHandler.ListAsync` applies it (`Matches(summary.ProjectId, filter.ProjectId)` at `:303`) and tags matches with `ConversationSearchMatchSource.ProjectReference` (`:351`). It is tenant-scoped (poison-guards cross-tenant rows at `:216`), refuses mixed-generation rows (`:223`), and aggregates **worst-case** freshness (`:434`). _[Source: `.../Server/Queries/ConversationQueryHandler.cs`]_
- **`ConversationSummaryV1`** returns `ConversationId`, `ProjectId`, `ProjectHydration` (`ProjectReferenceHydrationV1`), `Freshness` (`ProjectionFreshnessV1`), `LifecycleState`, optional `Label`. _[Source: `.../Contracts/Queries/ConversationSummaryV1.cs`]_
- **`ProjectReferenceHydrationV1`** exposes `ProjectId`, `HydrationState` (`ProjectionTrustState`), `Resolved`, `SafeLabel`, `SafeToken`, `SafeStatus`. _[Source: `.../Contracts/Queries/ProjectReferenceHydrationV1.cs`]_
- **🚩 The gap:** `IConversationClient` exposes **only** `CreateConversationAsync`, `AppendMessageAsync`, `GetConversationAsync` — **no list/search**. That is why Task 2 exists. _[Source: `.../Client/IConversationClient.cs`]_
- **Reference hydration boundary already exists:** `IConversationReferenceHydrationDirectory.HydrateProjectsAsync(...)` is the fail-closed pattern Conversations uses for upstream display; the Projects ACL is the mirror image on the consumer side. _[Source: `.../Server/Hydration/IConversationReferenceHydrationDirectory.cs`]_

### Architecture compliance (must follow)
- **Anti-Corruption Layer.** All Conversations access goes through the Projects-owned ACL; **never** call `IConversationClient`/the read API from Projects aggregate or projection logic. Mirror Conversations wrapping Parties behind `IParticipantDirectory` ("never call Parties from aggregate logic"). _[Source: `Hexalith.Conversations/_bmad-output/project-context.md`; research doc §Architectural Patterns]_
- **Reference, don't own.** Do **not** store an unbounded `ConversationId` list in any `Project` aggregate; the link lives on the read side (this ACL). _[Source: research doc §Integration Patterns; PRD §4.2]_
- **Dependency direction.** `Projects.Contracts` → at most `Conversations.Contracts`; `Projects.Server` → `Conversations.Client`/`Conversations.Contracts` (inward only). No Dapr/HTTP/EventStore-server in Contracts.
- **Tenant isolation at every layer**, decided by the local Tenants projection; cross-tenant access impossible by construction and tested adversarially. **Fail closed** on missing/stale/ambiguous/disabled/unauthorized. _[Source: root + Conversations `project-context.md`]_
- **Schema evolution** stays additive/serialization-tolerant; treat `ProjectId == null` on a summary as "not project-scoped". No `V2` types.

### Library / framework requirements
- **.NET 10** (`net10.0`), nullable + implicit usings + warnings-as-errors. **Central Package Management** via `Directory.Packages.props` — no inline versions.
- Depend on `Hexalith.Conversations.Client` + `Hexalith.Conversations.Contracts`. Reuse existing `TenantId`/`PartyId`/`ConversationId`/`ProjectId` identifier types rather than redefining.
- Do **not** upgrade or pin new versions of Fluent UI, Dapr, Aspire, Roslyn, xUnit generation, or the SDK.

### File / structure requirements
- `Projects.Contracts`: `Queries/ProjectConversationItem.cs`, `Queries/ProjectConversationsPage.cs`, (optional) `Identifiers/ConversationReference.cs`.
- `Projects.Server`: `Conversations/IProjectConversationDirectory.cs`, `Conversations/ConversationsProjectConversationDirectory.cs`, DI registration in the Projects service-collection extension.
- Tests: `tests/Hexalith.Projects.Server.Tests/Conversations/*`, `tests/Hexalith.Projects.Contracts.Tests/*`.
- File-scoped namespaces under `Hexalith.Projects.*`; 4-space indent, CRLF, UTF-8, final newline; private fields `_camelCase`; interfaces `I`-prefixed; async methods `Async`-suffixed; prefer `sealed`.

### Testing requirements
- **xUnit v3** (`3.2.2`), **Shouldly**, **NSubstitute** — match sibling modules. Use `Hexalith.EventStore.Testing` / `Hexalith.Tenants.Testing` fakes and reuse Conversations/Tenants authorization test patterns; **do not** invent new auth fakes or mock inside aggregate logic.
- Keep these **Tier 1**: pure translator + access-decision tests, no Dapr/Aspire/network/containers.
- Negative-path coverage is mandatory for tenant isolation (the security property), not optional happy-path only.
- Structured logging only — never log conversation content, payloads, secrets, tokens, or full command/query bodies.

### Project Structure Notes
- The Projects module is **greenfield inside a brownfield ecosystem**; follow the Hexalith package shape (`Contracts`/`Client`/`Server`/`Projections`/`Aspire`/`AppHost`/`ServiceDefaults`/`Testing`). **Variance/risk:** the module skeleton may not exist yet — see Prerequisite above; if absent this story is blocked on the Epic 1 foundation slice.
- Submodule discipline: keep changes scoped to Projects unless Task 2 chooses A1 (a `Hexalith.Conversations` client addition), which must be an additive, separately-tested change in that submodule. Never run recursive submodule init.

### References
- [Source: _bmad-output/planning-artifacts/research/technical-hexalith-projects-referencing-conversations-research-2026-05-24.md#Integration-Patterns-Analysis] — Pattern A, the client gap, ownership decision.
- [Source: _bmad-output/planning-artifacts/research/technical-hexalith-projects-referencing-conversations-research-2026-05-24.md#Architectural-Patterns-and-Design] — ACL placement, dependency direction, ADRs.
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md#FR-6] — Link Conversation (write side, separate story).
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md#FR-16] — Get Project Context (this story backs its conversation references).
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md#7-Cross-Cutting-NFRs] — fail-closed, tenant isolation, p95 < 500 ms, additive contracts.
- [Source: Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Queries/ConversationListFilterV1.cs] · [ConversationSummaryV1.cs] · [ProjectReferenceHydrationV1.cs]
- [Source: Hexalith.Conversations/src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs] · [Client/IConversationClient.cs] · [Server/Hydration/IConversationReferenceHydrationDirectory.cs]
- [Source: Hexalith.Conversations/_bmad-output/project-context.md] · [_bmad-output/project-context.md] — boundary/tenant/fail-closed rules.

## Dev Agent Record

### Agent Model Used

Codex (GPT-5)

### Debug Log References

- Red phase: `dotnet test tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj --no-restore` failed with CS0234 because `Hexalith.Projects.Contracts.Queries` and the story DTOs did not exist yet.
- Green phase: targeted Conversations client, Projects contracts, and Projects server tests were run and fixed until passing.
- Regression: `dotnet test Hexalith.Projects.slnx --no-restore` passed.
- Conversations submodule validation: `Hexalith.Conversations.Client.Tests` and `Hexalith.Conversations.Contracts.Tests` passed after the additive A1 client change. `dotnet test Hexalith.Conversations/Hexalith.Conversations.slnx --no-restore` was attempted; it still fails before story code because `Hexalith.Conversations/src/Hexalith.Conversations/Hexalith.Conversations.csproj` references `..\..\Hexalith.EventStore\...`, which is not present under the submodule path in this umbrella checkout.

### Completion Notes List

- Implemented the Projects-owned conversation reference boundary with `ProjectConversationItem`, `ProjectConversationsPage`, page metadata, `PageRequest`, `CallerPrincipalId`, and `ProjectConversationTrustSignal`; the reference identity reuses `Hexalith.Conversations.Contracts.Identifiers.ConversationId`.
- Chose **A1**: added `IConversationClient.ListConversationsAsync(ListConversationsQuery, CancellationToken)` and `ConversationClient` support for the existing read-list route that reaches `ConversationReadApi` and `ConversationQueryHandler.ListAsync`. Confirmed shapes: `ListConversationsQuery(SchemaVersion, TenantId, string CallerPrincipalId, string CorrelationId, ConversationListFilterV1? Filter, ConversationPageRequest? Page)`; `ConversationPageRequest(int PageSize = 25, string? ContinuationCursor = null)`; result paging is `ConversationPageMetadata(int ReturnedCount, string? ContinuationCursor = null)`.
- Implemented `IProjectConversationDirectory`, `ConversationsProjectConversationDirectory`, and translator logic that builds `ConversationListFilterV1(ProjectId: ...)`, passes tenant and caller through, closes the Projects page fail-closed on tenant/project mismatches, maps all `ProjectionTrustState` values plus `MixedGeneration` to a Projects signal, and surfaces only safe hydration label/status.
- Added a fail-closed `UnavailableProjectConversationDirectory` for hosts/tests that have not configured the Conversations client, and runtime DI defaults to `http://conversations` when no client registration exists.
- Added a thin project conversation list endpoint at `GET /api/v1/projects/{projectId}/conversations`; FR-16 full context assembly remains out of scope for this story.

### File List

- `_bmad-output/implementation-artifacts/2-1-conversation-reference-read-acl.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj`
- `src/Hexalith.Projects.Contracts/Identifiers/CallerPrincipalId.cs`
- `src/Hexalith.Projects.Contracts/Queries/PageRequest.cs`
- `src/Hexalith.Projects.Contracts/Queries/ProjectConversationItem.cs`
- `src/Hexalith.Projects.Contracts/Queries/ProjectConversationPageMetadata.cs`
- `src/Hexalith.Projects.Contracts/Queries/ProjectConversationTrustSignal.cs`
- `src/Hexalith.Projects.Contracts/Queries/ProjectConversationsPage.cs`
- `src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj`
- `src/Hexalith.Projects.Server/Conversations/ConversationsProjectConversationDirectory.cs`
- `src/Hexalith.Projects.Server/Conversations/IProjectConversationDirectory.cs`
- `src/Hexalith.Projects.Server/Conversations/ProjectConversationTranslator.cs`
- `src/Hexalith.Projects.Server/Conversations/UnavailableProjectConversationDirectory.cs`
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs`
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs`
- `tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj`
- `tests/Hexalith.Projects.Contracts.Tests/Queries/ProjectConversationsContractTests.cs`
- `tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj`
- `tests/Hexalith.Projects.Server.Tests/Conversations/ConversationsProjectConversationDirectoryTests.cs`
- `tests/Hexalith.Projects.Server.Tests/Conversations/ProjectConversationTranslatorTests.cs`
- `tests/Hexalith.Projects.Server.Tests/ServiceDefaultsEndpointTests.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Client/ConversationClient.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Client/IConversationClient.cs`
- `Hexalith.Conversations/tests/Hexalith.Conversations.Client.Tests/ConversationClientTest.cs`
- `Hexalith.Conversations/tests/Hexalith.Conversations.Contracts.Tests/Documentation/IntegrationGuideWorkflowExampleTest.cs`
- `Hexalith.Conversations/tests/Hexalith.Conversations.Contracts.Tests/Events/ConversationPublicationContractTest.cs`

### Change Log

- 2026-05-26: Implemented Pattern A conversation-reference read ACL, added A1 Conversations typed-client list support, exposed a thin project conversations read endpoint, and added Tier 1 boundary/translator/fail-closed tests.
- 2026-05-26: Review cycle 1 auto-fixed fail-closed scope poisoning and ACL DI lifetime, updated story/test artifacts, and marked story done after all scoped checks passed.

## Senior Developer Review (AI)

Reviewer: Codex (GPT-5) on 2026-05-26

Outcome: Approved after auto-fix. Story status set to `done`; sprint status synced. No CRITICAL issues remain.

### Findings After Auto-Fix

- CRITICAL: 0 remaining.
- HIGH: 0 remaining. Fixed 1 HIGH: any upstream tenant/project scope escape now closes the entire Projects page as empty `Unavailable` instead of returning partial rows.
- MEDIUM: 0 remaining. Fixed 2 MEDIUM: `IProjectConversationDirectory` no longer captures a typed `IConversationClient` in a singleton, and the story File List now includes review-touched/test-summary artifacts.
- LOW: 0 remaining.

### Review Evidence

- Acceptance criteria cross-check: AC1-AC9 verified against the Projects ACL contracts/server implementation, Conversations typed-client addition, and Tier 1 tests.
- Task audit: all `[x]` tasks have implementation evidence in the listed source/tests.
- Boundary check: Conversations contracts are confined to the ACL/allowed `ConversationId` reference boundary; no Conversations state/projections/transcript content crosses into Projects DTOs.
- Security check: fail-closed tenant/project mismatch, forbidden/unauthorized/upstream failure, and no-command-dispatch behavior verified through fakes.
- Git/story check: unrelated working-tree changes under `.agents/`, `.codex/`, `.gitignore`, and story-automator orchestration artifacts were observed and left untouched as outside story 2.1 scope.
- MCP documentation check: Microsoft Learn `IHttpClientFactory` typed-client guidance reviewed for typed-client DI lifetime behavior.

### Verification

- `dotnet test tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --no-restore` passed: 87/87.
- `dotnet test tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj --no-restore` passed: 117/117.
- `dotnet test Hexalith.Conversations/tests/Hexalith.Conversations.Client.Tests/Hexalith.Conversations.Client.Tests.csproj --no-restore` passed: 24/24.
- `dotnet test Hexalith.Conversations/tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --no-restore` passed: 580/580.
- `dotnet test Hexalith.Projects.slnx --no-restore` passed: 366/366.
- `git diff --check` passed for story-scoped Projects and Conversations paths with Git CRLF conversion warnings only.
