---
story_id: 6.1
story_key: 6-1-list-and-open-projects-through-supported-authenticated-paths
epic: "Epic 6: Authorized Project Reads on the Supported Platform"
created: 2026-07-17
source_story_status: backlog
status: blocked
blocked_by: [6.1-P0, 6.1-P1, 6.1-P2, 6.1-P3, 6.1-P4]
baseline_commit: 90e481c
traceability:
  requirements: [fr-2, fr-5]
  nfrs: [nfr-1, nfr-5, nfr-10]
  architecture: [AD-3, AD-14, AD-19, AD-20, AD-32, AD-33]
  journey: UJ-1
  findings: [ARCH-001, API-001]
repository_authority: "Hexalith.Projects contracts, read models, and query handlers with platform-generated read adapters"
owners: [Product Owner, Solution Architect, Test Architect]
entry_gate: "6.1-P4 must be accepted and the Story 6.1 spec must pass ready-for-development before implementation begins"
release_disposition: "Not a production-release gate; Story 8.11 owns terminal deployment, rollback, and stakeholder acceptance"
estimate: M
---

# Story 6.1: List and Open Projects Through Supported Authenticated Paths

Status: blocked

<!-- The 2026-07-17 implementation-readiness report authorizes story planning, not Story 6.1 implementation. The approved 2026-07-17 Sprint Change Proposal externalizes platform enablement as 6.1-P0 through 6.1-P4. -->

## Story

As a Tenant Operator or delegated Chatbot service caller,
I want to list visible Projects and open one Project's authorized metadata, lifecycle, setup summary, and reference summary through the supported DomainService read models,
so that operators and Chatbot get current, authorization-filtered Project truth to initialize a Conversation (FR-2, FR-5) with no legacy runtime.

## Acceptance Criteria

1. **Supported authorized list/open path.** Given an authenticated caller with valid Tenant and actor authority and current read models, when `ListProjects` or `GetProject` executes through an `IDomainQueryHandler`, then the result is Tenant-scoped and authorization-filtered; carries the AD-32 snapshot fields `responseState`, `asOf`, `projectVersion`, `components`, and `recoveryActions`; and never computes, persists, or selects a resolution candidate. `ListProjects` uses an opaque platform cursor bound with `QueryCursorScope`, defaults to 50 rows, caps requests at 200 rows, filters by lifecycle, returns visible `Active` and `Archived` Projects, and has deterministic gap-free ordering. `GetProject` is a singleton read and does not accept paging.
2. **Indistinguishable safe denial.** Given a well-formed opaque Project identity that is denied, cross-Tenant, or nonexistent, when the read executes, then the boundary fails closed with a safe `404`; status, headers, body shape, timing category, logs, and telemetry do not disclose whether the Project or Tenant exists or why access failed. Unauthorized rows are absent from list results, while an unauthorized list scope fails safely. Structurally invalid query/filter/page/cursor input follows AD-19 `400` metadata-only validation semantics after authorization where protected validation could disclose information; it is not conflated with a well-formed absent identity.
3. **Honest response state.** Given stale, rebuilding, unavailable, or incomplete read-model evidence, when an already-authorized read executes, then it returns an honest `Partial` or `Unavailable` snapshot with component-level freshness, inclusion, safe reason, and last-verified evidence rather than inferred completeness. `Partial` is usable only when current Project, Folder, Setup, and authorization evidence are present and every optional omission is declared. `Unavailable` blocks Conversation context use. An authorized Archived Project may be inspected, but its lifecycle always blocks context initialization even when its metadata snapshot is `Complete`. If authorization itself cannot be established, no protected snapshot is returned.
4. **Shadow equivalence before cutover.** Given the shadow-read comparison harness and the deterministic authorized-Tenant persisted-boundary fixture, when the same list/open query runs against the legacy and supported read paths, then canonical output, read-model keys, watermarks, cursor behavior, ordering, and Tenant isolation are deterministically equivalent. The entry gate must freeze a finite field-level normalization list with owner/date and revision; only those exact rules may normalize legacy-to-AD-32 differences, and every other delta fails. The default/public read route is not switched by this story; Story 6.7 owns reversible routing cutover.
5. **FR-2/FR-5 response completeness.** Given an authorized list/open response, then it contains only the explicit metadata allowlist in Dev Notes. List rows expose authorized Project identity, name, lifecycle, current Project version, Project Folder availability, and enough AD-32 freshness/recovery evidence to choose a Project without loading full Project Context. Open exposes the approved Project metadata, lifecycle, typed setup summary, and Projects-owned reference summary. Pre-activation creation tasks never appear as Projects; full context, unvalidated setup source text, source payloads, and sibling-owned content are excluded.
6. **Read-only and additive compatibility.** Given any success, denial, retry, restart, rebuild, or fault scenario, then the operation performs no Project/domain write, event append, task creation, maintenance audit, resolution persistence, sibling mutation, or direct read-model repair. Platform-owned metadata-only durable security audit remains required for authorization denial under FR-21/AD-26 and must not disclose protected identity/resource detail. Existing event history remains readable, no event is rewritten, public contract evolution is additive, and the legacy read path remains available for comparison and later rollback until Story 6.7/8.11 retires it.
7. **Persisted-boundary verification.** Given Story 6.1 is complete, when the supported reads profile runs on the approved G-4 composition runner, then unit, adapter, persisted-state, restart, fault, authorization, leakage, and shadow-equivalence scenarios pass; `evidence/epic6/6.1-authorized-reads.trx`, `evidence/epic6/6.1-authorized-reads.json`, and a machine-readable shadow-equivalence report are produced from actual results rather than hand-authored claims.

## Tasks / Subtasks

**Non-implementation entry condition.** Story 6.1 does not own platform enablement. Do not mark any
task below in progress until 6.1-P0 through 6.1-P3 have owner-approved repository-local revisions and
evidence, 6.1-P4 accepts their exact pins/normalization/rollback record, and the Story 6.1 spec passes
ready-for-development. The implementing agent cannot satisfy, waive, or self-approve these blockers.

- [ ] Define additive supported read contracts and the AD-32 vocabulary (AC: 1, 3, 5, 6)
  - [ ] Add one public C# type per file under `src/Hexalith.Projects.Contracts/Queries/` for the list/open query contracts, list page and row, open result, response snapshot, component evidence, recovery action, and stable response/component/reason states. Prefer existing shared vocabulary where it already has the exact canonical meaning; do not create parallel Web/CLI-only enums.
  - [ ] Model `ListProjectsQuery` with lifecycle filter, cursor, and page size. Apply query-specific 50/200 policy; do not change the existing general `PageRequest` 25/100 defaults used by conversation queries.
  - [ ] Model `GetProjectQuery` as a singleton Project read. Bind Tenant and actor only from the authenticated platform envelope/context; Project ID may be route-bound but must be reauthorized before protected validation or lookup evidence is exposed.
  - [ ] Keep the wire shape metadata-only and presentation-ready: use only the exact allowlisted fields in Dev Notes, stable structured states/reasons, deterministic component/reference ordering, opaque copy-safe identifiers, explicit Folder availability, and no blank/implicit trust state.
  - [ ] Exclude `resolutionResult` from this story's normal output because no resolution runs. If the shared AD-32 envelope requires the optional member, leave it absent/null and prove no candidate was computed or selected.
  - [ ] Preserve `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml`, generated clients, and the existing public route DTOs in this story. The supported DomainService seam is added beside the legacy public API for authenticated platform-gateway and persisted-evidence use; never hand-edit generated `.g.cs` files or map a duplicate `/api/v1/projects` route. Stop for architecture disposition if the approved platform seam makes a public-contract change unavoidable.

- [ ] Implement supported incremental Project list/detail read models (AC: 1, 3, 5, 6)
  - [ ] Add named `IAsyncDomainProjectionHandler` implementations under `src/Hexalith.Projects/Projections/ProjectList/` and `ProjectDetail/`, using the approved `IReadModelStore`/batch/rebuild seam and `ReadModelWritePolicy`. Reuse or extract the deterministic fold logic in `ProjectListProjection` and `ProjectDetailProjection`; do not duplicate domain event interpretation.
  - [ ] Persist the approved tenant-scoped list index and project-scoped detail keys from the entry-gate inventory. Store authoritative sequence/watermark, last verified time supplied by persisted evidence, lifecycle, Folder availability, setup summary, and safe reference summary needed to derive AD-32 state.
  - [ ] Handle the complete current Projects event catalog deterministically through one explicit event adapter classification: relevant known events apply; explicitly catalogued additive events proven irrelevant to list/open are deterministic no-ops with compatibility tests; corrupt, undecodable, or unknown potentially relevant events fail the projection/rebuild honestly and yield `Unavailable`. Neither blanket skipping nor blanket rejection of harmless additive events satisfies NFR-10.
  - [ ] Prove full replay, incremental apply, duplicate dispatch, optimistic concurrency, retry/restart, and rebuild produce the same end state. Persisted-state assertions are mandatory; handler return values alone are insufficient.
  - [ ] Treat historical Active-but-folderless or otherwise incomplete records honestly: they may remain inspectable/listable as safe recovery evidence but are `Unavailable` for context use. `ProjectCreated` alone must not imply a usable Project Folder.
  - [ ] Do not add a Projects-owned Dapr event journal, direct Dapr state calls, full-history-per-read replay, or local replacements for EventStore projection/query/cursor behavior. Keep `DaprProjectProjectionStore` unchanged as legacy shadow input until the later cutover/retirement stories.

- [ ] Implement authenticated `IDomainQueryHandler` list/open handlers (AC: 1, 2, 3, 5, 6)
  - [ ] Add `ListProjectsQueryHandler` and `GetProjectQueryHandler` under `src/Hexalith.Projects/Queries/Handlers/`, following the approved EventStore/Tenants handler conventions while preserving Projects' stronger safe-denial policy.
  - [ ] Add `ProjectReadAuthorizationService` (or use the exact architecture-approved platform equivalent) so handlers reauthorize the immutable G-5 Tenant, original actor, authenticated workload, delegation, scopes, audience, action, target, and version. Preserve authorization-before-protected-validation and defense-in-depth Tenant filtering; never trust tenant/actor values from payload, query, `X-Hexalith-Tenant-Id`, or substitute headers.
  - [ ] Read only through `IReadModelStore`. Map current required evidence to `Complete`; current required plus explicitly omitted optional evidence to `Partial`; missing/stale/rebuilding required evidence to `Unavailable`; and denied/absent to one indistinguishable adapter failure that the supported REST boundary maps to safe `404`.
  - [ ] For `ListProjects`, filter authorization and lifecycle before pagination; sort by the approved stable key; build cursor scope from Tenant, original actor, workload/delegation identity, action/query type, lifecycle filter, page-size policy/version, and any other result-shaping input; then use `IQueryCursorCodec`. Reject tampering, wrong query/filter/identity scope, version/expiry/key-rotation failure, separators/injection, and oversized values without leaking data.
  - [ ] Return at most 200 rows, default 50. Ensure no gaps or duplicates across page boundaries and no cross-caller replay. `GetProject` must not interpret a cursor.
  - [ ] Register projection/query handlers through `ProjectsServiceCollectionExtensions` using the platform interfaces. Add only centrally versioned EventStore project/package references required by the approved gate to `Hexalith.Projects.csproj`; do not inline versions or clone platform code.
  - [ ] Register and map the supported DomainService host seam in `Hexalith.Projects.Server/Program.cs` and, only if required, `Hexalith.Projects.Server.csproj`, side-by-side with the intact legacy server. The supported platform route must not collide with or replace the existing public `/api/v1/projects` routes before Story 6.7.
  - [ ] Prove the callable supported chain through the approved authenticated platform-generated adapter and EventStore gateway into the SDK host `POST /query` endpoint and then the matching `IDomainQueryHandler`. Handler-direct tests are necessary but cannot satisfy persisted-boundary completion. Pin the external adapter route/contract in the entry-gate inventory; do not expose the internal `/query` endpoint as an unauthenticated public API.

- [ ] Preserve the legacy boundary and add a canonical shadow comparator (AC: 2, 4, 6)
  - [ ] Inventory and preserve current behavior in `ProjectsDomainServiceEndpoints`, `ProjectAuthorizationGate`, `IProjectListReadModel`, `IProjectDetailReadModel`, `DaprProjectListReadModel`, and `DaprProjectDetailReadModel`. The current route has useful authorization-first, safe-404, lifecycle-filter, metadata-only, and Tenant-filter defenses; keep them as regressions.
  - [ ] Do not extend the legacy `DaprProjectProjectionStore` as the supported implementation. Do not delete it or change default/public routing in Story 6.1.
  - [ ] Add a test/evidence shadow comparator that executes identical immutable query inputs against the legacy adapter and supported handlers, canonicalizes only documented legacy-to-AD-32 differences, and compares output fields, authorization filtering, ordering, keys, watermarks, page boundaries/cursors, empty/archived/degraded cases, and Tenant isolation.
  - [ ] Put reusable comparer/fixture types in `src/Hexalith.Projects.Testing/Reads/` and add only the approved EventStore testing dependency to `Hexalith.Projects.Testing.csproj`. Keep production query assemblies free of comparison-only code.
  - [ ] Emit the report at `evidence/epic6/6.1-shadow-read-equivalence.json` and include baseline revisions, fixture ID, query/filter/cursor scope hash, compared fields, allowed normalization rules, unexplained deltas, and pass/fail. Any unexplained delta blocks completion and later routing cutover.
  - [ ] Ensure shadow execution is side-effect-free and does not log protected values. It must not become an application-level feature flag or a second query runtime; Story 6.7 owns supported-route activation and rollback controls.

- [ ] Add focused and persisted-boundary verification (AC: all)
  - [ ] Unit cases: `E6.1-U01` page default/cap and complete response, `E6.1-U02` no gaps/duplicates, `E6.1-U03` cursor tamper/scope rejection, and `E6.1-U04` unknown version/expiry rejection.
  - [ ] Adapter/persisted cases: `E6.1-A01` authorized persisted list, `E6.1-A02` exact metadata-only open, `E6.1-A03` observationally identical missing/denied/cross-Tenant safe `404`, `E6.1-A04` honest `Partial`, `E6.1-A05` honest `Unavailable`, `E6.1-A06` shadow equivalence, and `E6.1-A07` restart/retry stability.
  - [ ] Add an actor x workload x Tenant matrix, including missing/invalid delegation, audience, scope, disabled Tenant, stale Tenant-access evidence, structurally invalid input (`400`), well-formed absent/denied Project identity (indistinguishable `404`), and cross-Tenant identity (`404`). Prove authorization precedes protected validation and that caller-controlled tenant headers do not establish authority.
  - [ ] Add lifecycle/freshness matrices for Active/current-Folder, Active/folderless historical, Archived, empty, stale, rebuilding, and unavailable projections; deterministic ordering at page boundaries; filter-bound cursor replay; and required-versus-optional component omissions.
  - [ ] Add reflection/serialization/log-capture leakage tests over every new public type and failure path. Forbid transcripts, prompts, file content/path, folder paths, memory payloads, setup source text beyond approved summary, sibling denial detail, raw upstream problems, command bodies, idempotency keys, secrets, and tokens.
  - [ ] Assert zero EventStore appends, Project/domain writes, tasks, maintenance audits, sibling calls, candidate computation, and resolution persistence for every read scenario. Separately prove authorization denials emit only the required platform-owned, metadata-only, deduplicated security audit without becoming an existence oracle.
  - [ ] Exercise production mappers, registration, projections, `IReadModelStore`, cursor codec, and generated/supported adapters. Endpoint tests with in-memory/allowing stubs remain regressions but are not sufficient proof.
  - [ ] Run the repository build and focused test projects with warnings as errors, then run `dotnet tool run hexalith-module test --profile reads --filter Story=6.1` on G-4 and retain actual `.trx`, JSON summary, and shadow report. Final NFR-5 10k/p95 proof belongs to Story 8.9; this story proves bounded paging and records measurements without claiming the release performance gate.
  - [ ] Run `git diff --check`, OpenAPI/generated-adapter fingerprint checks if contracts changed, and the architecture conformance checks applicable to Story 6.1.

## Dev Notes

### Authority, Readiness, and Scope

- The current story definition is the corrective production plan in `epics.md`. The old `bmad-dev-auto-result-6-1-pin-platform-capabilities-and-migration-baseline.md` describes the superseded Story 6.1; use it only as historical evidence. The current `epic-6-context.md` and approved Sprint Change Proposal route capability pinning through 6.1-P0 through 6.1-P4.
- The 2026-07-17 readiness report returns `READY` and authorizes Story 6.x file creation. It supersedes the 2026-07-16 `NOT_READY` decision and the still-unexecuted conformance checklist for that purpose. It does not mark the Epic 6 implementation pins, G-4 runner, G-5 identity, or draft test-design approvals complete.
- The canonical traceability matrix records FR-2 and FR-5 as `blocked-external` and binds their implementation readiness to 6.1-P0 through 6.1-P4. Planning-layer `READY` is not implementation approval.
- Story 6.1 depends on accepted 6.1-P0 through 6.1-P4 only. Story 6.2 owns Conversation-start setup, 6.3 full Project Context/refresh/explanation, 6.4 nonpersistent resolution, 6.5 authenticated Web, 6.6 authenticated CLI, and 6.7 reversible read-routing cutover. Epic 7 durable tasks and Epic 8 release proof are not forward dependencies.
- `blocked` is non-startable. The story returns to `ready-for-dev` only after P4 acceptance and a passing specification readiness rerun.

### Story Completion Contract

- Repository authority: Hexalith.Projects contracts, read models, and query handlers, consumed through platform-generated read adapters.
- Named accountable roles: Product Owner, Solution Architect, and Test Architect own P4; Builds, Platform, EventStore, and Identity/Security owners retain authority for P0-P3. The implementing agent cannot self-approve any prerequisite.
- Findings/trace: FR-2, FR-5; NFR-1, NFR-5, NFR-10; AD-3, AD-14, AD-19, AD-20, AD-32, AD-33; UJ-1; `ARCH-001` and `API-001` read-side closure; evidence rows `fr-2` and `fr-5`.
- Dependency: accepted 6.1-P0 through 6.1-P4. Compatibility: legacy routing remains intact and reversible, no event-history rewrite, and no public cutover in this story.
- Fixture/evidence: deterministic authorized-Tenant persisted-boundary fixture on G-4, required reads command, `.trx`/JSON evidence, and exact shadow report.
- Estimate: M. Release disposition: Story 6.1 is not a production-release gate; Story 8.11 plus dated Jerome/John acceptance remains terminal authority.

### Canonical Response Semantics

- AD-32 fields are `responseState`, `asOf`, `projectVersion`, optional `resolutionResult`, `components`, and `recoveryActions`. Canonical states are `Complete`, `Partial`, `Unavailable`, and `Denied`; protected `Denied` detail stays internal and collapses to the safe-404 boundary.
- Components use inclusion exactly `Included|Excluded`, freshness exactly `Current|Stale|Rebuilding|Unavailable`, a metadata-only safe reason, and last-verified time when known. Recovery codes are exactly `None|Retry|RefreshContext|RequestPreview|RenewPreview|PollTask|ResolveNeedsAttention|SelectAlternative|ContactAdministrator`; Story 6.1 emits only the applicable read recovery codes and their bounded metadata.
- `asOf` is the timestamp of this authorization-and-evidence computation, not a fabricated projection timestamp. `projectVersion` is the authorized aggregate sequence when disclosable; keep it distinct from store ETags, projection dispatch positions, and the legacy global watermark.
- `Partial` is not a synonym for stale. It is usable only when Project, Folder, Setup, and authorization evidence are current and every optional omission has component evidence. `Unavailable` blocks initialize/resume. Recovery/refresh never silently rewrites an earlier response.
- List rows include visible Active and Archived Projects, lifecycle filtering, current version, Folder availability, and selection-grade snapshot evidence. Pre-activation durable-task records never appear. Archived metadata can be complete for inspection while `lifecycleState = Archived` independently blocks Conversation context use.
- Setup summary means the existing metadata-only Project setup fields authorized for list/open. It is not Story 6.2's bounded Conversation-start payload. Reference summary means opaque reference kind/id/display metadata, availability/trust/freshness, and safe reason only; it is not Story 6.3 full context or sibling-owned content.
- Stable structured state/reason/component values are required so later Web/CLI surfaces can be accessible and surface-invariant. Story 6.1 does not implement UI, CLI, MCP, Playwright, or accessibility acceptance.

### Metadata Allowlist

- List page: `items`, protected `cursor`, and `hasMore`. Each row may contain only `projectId`, `name`, `lifecycleState`, authorized aggregate-sequence `projectVersion`, Project Folder availability, and its AD-32 `responseState`, `asOf`, `components`, and `recoveryActions`. Do not echo Tenant or actor identity.
- Open Project metadata: `projectId`, `name`, validated metadata-only `description`, `lifecycleState`, `createdAt`, `updatedAt`, authorized aggregate-sequence `projectVersion`, and the AD-32 snapshot. Do not add audit, task, Conversation, resolution, or sibling payload fields.
- Typed setup summary: only the already validated/bounded `ProjectSetup` contract (`Goals`, `UserInstructions`, `PreferredSourceKinds`, `ExcludedSourceKinds`, `ConversationStartDefaults`) plus the existing opaque `SetupMetadata` reference when authorized. These values remain subject to payload/leakage validation; never expose an unvalidated source document, provider/model prompt, transcript, secret, token, or sibling content. Story 6.2 owns the separately bounded Conversation-start response and admission truth.
- Reference summary: only `referenceKind`; the Projects-stored opaque `FolderId`, `FileReferenceId`, or `MemoryReferenceId` applicable to that kind; the Projects-owned bounded `DisplayName` intent already in the event/read model; `ReferenceState`; stable safe `ReasonCode`; and event-carried `ObservedAt`/AD-32 freshness evidence. Do not fetch or expose sibling-owned labels, workspace/path/URI/content/hash/classification/provider data, MemoryUnit fields, raw authorization detail, or raw upstream problems.
- Recovery actions carry only the exact AD-32 code and applicable bounded retry guidance. Story 6.1 creates no task/preview; therefore it does not invent task identity/status or Preview-expiry data.

### Current Implementation: Preserve, Replace, and Do Not Conflate

- `ProjectsDomainServiceEndpoints` currently hand-maps `GET /api/v1/projects` and `GET /api/v1/projects/{projectId}`. List has no cursor/paging and both use private legacy DTOs without the AD-32 snapshot. Preserve the route and regression behavior during this story; do not map a second handler to the same route.
- `ProjectAuthorizationGate` already applies authenticated claim transformation, Tenant-access freshness, Project visibility, EventStore validator, Dapr policy, safe denial, and defense-in-depth filtering. Preserve its legacy behavior. The supported `IDomainQueryHandler` cannot depend on `HttpContext` or Server-owned read-model interfaces; give it an approved domain/platform authorization adapter rather than calling the legacy endpoint gate from the domain layer.
- `ProjectListProjection` and `ProjectDetailProjection` are deterministic pure event folds with Tenant guards. Reuse their semantics. They currently feed Projects-specific in-memory/Dapr adapters and do not themselves satisfy `IAsyncDomainProjectionHandler`, `IReadModelStore`, persisted incremental/rebuild, AD-32, or cursor requirements.
- Do not attach store ETags/watermarks/freshness to the legacy `ProjectListItem` or `ProjectDetailItem` merely to make the new handler compile. Keep supported persistence metadata in dedicated read-model envelopes while preserving the existing pure-fold contracts and tests.
- `DaprProjectProjectionStore` maintains a Projects-owned Dapr projection journal and rebuilds projections from that journal on reads. It is the legacy comparator, not the supported target. No new code may copy or extend that persistence pattern.
- Current list items omit explicit Folder availability and use `Sequence` as freshness/version evidence; current detail contains setup and reference metadata. Define the supported persisted shapes additively and derive an authorized version/watermark from actual persisted projection evidence, never wall-clock defaults.
- Existing API and test behavior for lifecycle validation, idempotency-header rejection on reads, tenant filtering, safe-404 equality, archived metadata inspection, metadata-only errors, and authorization-before-protected-validation must remain regression-covered.

### Platform API Guidance

- At the architecture pin, `IAsyncDomainProjectionHandler` exposes `Domain`, `ProjectionType`, and `ProjectAsync(ProjectionRequest, dispatchId, CancellationToken)`; `IDomainQueryHandler` exposes `Domain`, `QueryType`, and `ExecuteAsync(QueryEnvelope, CancellationToken)`.
- `IReadModelStore` supplies `GetAsync<T>`, `SaveAsync`, and ETag-aware `TrySaveAsync`; use the platform batch/rebuild seam where the approved version provides it and `ReadModelWritePolicy` for incremental/rebuild behavior.
- Build cursor scope with `QueryCursorScope.Create().Add(...).Build()`. Encode/decode only through `IQueryCursorCodec`; bind the query type and every result-shaping/authorization dimension. Do not expose storage positions or create a local signing scheme.
- `references/Hexalith.Tenants` provides useful `ListTenantsQueryHandler`/`GetTenantQueryHandler` patterns for platform store, cursor, and pagination use. Do not inherit its response/authorization decisions where they conflict with Projects' AD-19 safe-404 and AD-20 dual-principal requirements.
- Source-mode references are for local development and API inspection. Package-mode compatibility must also pass using the centrally approved version; source-only success is insufficient.

### Library and Framework Constraints

- Keep `global.json` at .NET SDK `10.0.302`, `net10.0`, and repository C# settings. .NET 10 is LTS; this story has no SDK/runtime upgrade scope.
- Central package management is mandatory. Current relevant Builds pins include NSwag `14.7.1`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, and NSubstitute `6.0.0`. Do not inline versions, downgrade warnings, disable nullable analysis, or add analyzer suppressions to make the change pass.
- If public System.Text.Json DTOs change, add explicit serialization/golden coverage, including duplicate/case-conflicting property behavior under .NET 10. Keep additions backward-compatible.
- One public C# type per file; file/type names match. Keep domain read logic in `Hexalith.Projects`, transport composition in Server/generated adapters, and shared wire contracts in Contracts.

### Recent Git Intelligence

- Baseline `90e481c` updates the Builds, EventStore, and Memories submodule pointers; this is why entry-gate verification must use exact revisions rather than version prose copied from older artifacts.
- The immediately preceding commits are planning/test-design rebaseline work (`01bfdd3`, `5e32ece`, `ec21c7e`, `aa20ff1`), not a supported Projects read implementation. Do not infer that the new runner, identity seam, or routing cutover already exists from those document changes.
- Relevant earlier code commit `0810708` extended Tenant-access projection freshness and AppHost setup. Preserve those authorization/freshness regressions, but do not mistake the current Tenant-access/in-memory/Dapr path for the AD-14 supported read model.
- Work from the current root baseline and keep root-declared submodule pointers unchanged unless a separately approved gate change explicitly owns them. Do not initialize nested submodules.

### Recommended File Map

- New contracts: `src/Hexalith.Projects.Contracts/Queries/ListProjectsQuery.cs`, `GetProjectQuery.cs`, and one-file-per-type AD-32 list/open response models in the same folder.
- New handlers/policy: `src/Hexalith.Projects/Queries/Handlers/ListProjectsQueryHandler.cs`, `GetProjectQueryHandler.cs`, plus domain-owned authorization/cursor helpers only where the approved platform seam does not already provide them.
- New platform projections/read shapes: `src/Hexalith.Projects/Projections/ProjectList/` and `ProjectDetail/`; reuse current pure folds and add supported handler/rebuild/persisted model types without turning legacy adapters into the target.
- Update registration/dependencies: `src/Hexalith.Projects/ProjectsServiceCollectionExtensions.cs`, `src/Hexalith.Projects/Hexalith.Projects.csproj`, `src/Hexalith.Projects.Server/Program.cs`, and only if required by the approved host seam `src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj`.
- Preserve the existing OpenAPI spine, generated client, and legacy `/api/v1/projects` route DTOs in this story. If the approved platform generator unexpectedly requires a public-contract change, stop for architecture disposition rather than hand-editing generated output or creating a duplicate route.
- Reusable shadow fixtures/comparer: `src/Hexalith.Projects.Testing/Reads/` and `src/Hexalith.Projects.Testing/Hexalith.Projects.Testing.csproj`.
- Unit tests: `tests/Hexalith.Projects.Tests/Queries/` and `tests/Hexalith.Projects.Tests/Projections/`.
- Adapter/shadow tests: `tests/Hexalith.Projects.Server.Tests/` and the approved platform-runner-backed integration/evidence project. Do not treat `DaprProjectionStoreTests` with a fake backend as persisted-boundary proof.
- No feature work in `src/Hexalith.Projects.UI`, `src/Hexalith.Projects.Cli`, or their acceptance suites. Those surfaces belong to Stories 6.5 and 6.6.

### Update-File Guidance

| File | Current state | Change in Story 6.1 | Preserve |
| --- | --- | --- | --- |
| `src/Hexalith.Projects/Hexalith.Projects.csproj` | Packable domain library references Contracts and Microsoft Extensions only. | Add only the gate-approved central EventStore DomainService/Client dependencies needed by supported projection/query handlers. | Packability, domain boundary, central versioning, warnings-as-errors. |
| `src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj` | Bespoke Web host references domain, Contracts, Infrastructure, ServiceDefaults, sibling clients, and EventStore Client/Contracts source projects. | Add the approved DomainService host SDK reference only if not already transitively supplied. | All legacy runtime/sibling references and public API behavior until 6.7. |
| `src/Hexalith.Projects.Server/Program.cs` | Registers legacy Server/runtime, conditionally enables JWT, and maps default plus Projects endpoints. | Add the two-line supported DomainService host composition against the Projects assembly and make required production dual-principal configuration fail closed. | Existing legacy mappings; do not replace `/api/v1/projects`, AppHost ownership, or ServiceDefaults. |
| `src/Hexalith.Projects/ProjectsServiceCollectionExtensions.cs` | Registers Tenant-access, context policy, and resolution services. | Register only Projects-owned read authorization/snapshot helpers not auto-discovered by DomainService. | Existing domain registrations; no platform hosting/runtime clone. |
| `src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs` | Deterministic tenant-guarded batch fold with stable ProjectId ordering. | Extract/expose an idempotent incremental reducer usable from a dedicated persisted read-model envelope. | Existing rebuild API, ordering, Tenant checks, and legacy tests. |
| `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailProjection.cs` | Deterministic detail fold over metadata/setup/references/lifecycle. | Add a safe persisted-state incremental application seam with aggregate-sequence protection. | Existing rebuild semantics and `ProjectDetailItem`; store metadata stays in the new envelope. |
| `src/Hexalith.Projects.Testing/Hexalith.Projects.Testing.csproj` | Packable reusable helpers reference domain/Contracts and logging. | Add only the approved EventStore testing dependency for persisted fixtures and shadow comparison. | Packable reusable boundary; no host topology. |
| `tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj` | Legacy Dapr/Aspire integration lane references AppHost, Aspire, Infrastructure, Server, Testing, and Workers. | Add the approved DomainService integration fixture dependency only when required for the G-4 consumer lane. | Existing legacy integration coverage; no retirement or topology rewrite in 6.1. |

### Verification and Evidence Contract

- Required command: `dotnet tool run hexalith-module test --profile reads --filter Story=6.1`.
- Required evidence: `evidence/epic6/6.1-authorized-reads.trx`, `evidence/epic6/6.1-authorized-reads.json`, and the shadow-equivalence report.
- Required proof includes actual persisted end state, duplicate dispatch, full replay/rebuild, restart/retry, real fault/unavailability behavior, cursor scope/tamper, dual-principal Tenant isolation, exact metadata-only safe denial, and zero writes.
- The known Story 5.12 live evidence remains red in this checkout (focused live 13/13 failures and full live 56/75 failures, primarily Tenant-access fixture and UI asset issues). Do not misattribute pre-existing failures, but do not use them as an excuse to replace the G-4 persisted proof with mocks or hand-authored evidence.

### Hard Stops

- Stop unless 6.1-P4 accepts the exact EventStore/Builds/G-5/G-4 revisions and evidence from P0-P3; escalate with exact revision/API evidence.
- Stop if original actor and authenticated workload identity cannot both be proven and reauthorized at query time.
- Stop if the gateway can emit an observably different `403`/tenant rejection before the Projects handler can enforce the safe-404 contract.
- Stop if exact legacy/supported watermark comparison is required but the approved projection event contract still omits global position and no normalization has been approved.
- Stop if production can start or admit supported reads without the required dual-principal authentication configuration.
- Stop if a denied/absent/cross-Tenant response differs observably or if degraded output can disclose a Project before authorization is established.
- Stop if implementation requires a custom Dapr journal, direct state repair, full replay on each read, a home-grown cursor/signature, a custom query runtime/switch, or platform API reimplementation.
- Stop if supported routing must become the public default, legacy read plumbing must be removed, or rollback controls must be changed; that is Story 6.7.
- Stop if any read writes Project/domain state, events, tasks, maintenance audit, invokes sibling mutation, selects/persists resolution, rewrites history, or exposes payload-bearing data. Do not block the platform-owned metadata-only durable security audit required for an authorization denial.
- Stop if evidence must be fabricated because G-4 tooling, a persisted boundary, or an external dependency is unavailable. Record the blocker; do not synthesize a passing artifact.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-6.1-List-and-open-Projects-through-supported-authenticated-paths]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic-6-Authorized-Project-Reads-on-the-Supported-Platform]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md#5-Observable-Context-and-Recovery-Contract]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md#Functional-Requirements]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md]
- [Source: _bmad-output/planning-artifacts/implementation-readiness-report-2026-07-17.md]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-17.md]
- [Source: _bmad-output/planning-artifacts/implementation-readiness-traceability-matrix.yaml]
- [Source: _bmad-output/planning-artifacts/epics-architecture-conformance-checklist-2026-07-16.md]
- [Source: _bmad-output/test-artifacts/test-design-epic-6.md]
- [Source: _bmad-output/project-context.md]
- [Source: references/Hexalith.AI.Tools/hexalith-llm-instructions.md]
- [Source: references/Hexalith.AI.Tools/hexalith-state-instructions.md]
- [Source: references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/IAsyncDomainProjectionHandler.cs]
- [Source: references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/IDomainQueryHandler.cs]
- [Source: references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/IReadModelStore.cs]
- [Source: references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/ReadModelWritePolicy.cs]
- [Source: references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Queries/IQueryCursorCodec.cs]
- [Source: references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Queries/QueryCursorScope.cs]
- [Source: references/Hexalith.Tenants/src/Hexalith.Tenants/Queries/Handlers/ListTenantsQueryHandler.cs]
- [Source: references/Hexalith.Tenants/src/Hexalith.Tenants/Queries/Handlers/GetTenantQueryHandler.cs]
- [Source: src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs]
- [Source: src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs]
- [Source: src/Hexalith.Projects.Infrastructure/DaprProjectProjectionStore.cs]
- [Source: src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs]
- [Source: src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailProjection.cs]
- [Source: tests/Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.cs]
- [Source: tests/Hexalith.Projects.Tests/Replay/ProjectionRebuildDeterminismTests.cs]
- [Official .NET 10 compatibility guidance: https://learn.microsoft.com/dotnet/core/compatibility/10]
- [Official .NET release and support policy: https://learn.microsoft.com/dotnet/core/releases-and-support]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Story creation only; no implementation build or test was run.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Source discovery covered the corrective epics, canonical PRD, canonical architecture spine, readiness/reconciliation artifacts, Epic 6 test design, all project-context facts, current code/tests, EventStore supported APIs, current package/submodule revisions, recent Git history, and official .NET 10 lifecycle/compatibility guidance.
- Story creation records the 2026-07-17 readiness supersession while preserving the unresolved Epic 6 package, identity, composition-runner, and planning-reconciliation gates as explicit implementation hard stops.
- Independent create-story checklist review was applied: corrected AD-19 invalid-input semantics and FR-21/AD-26 denial audit handling; fixed the authoritative Epic/source references; enumerated the metadata allowlist and update-file guidance; constrained shadow normalization; and required proof through the authenticated gateway-to-`/query` chain.

### File List

- `_bmad-output/implementation-artifacts/6-1-list-and-open-projects-through-supported-authenticated-paths.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Change Log

| Date | Change |
| --- | --- |
| 2026-07-17 | Created Story 6.1 comprehensive implementation context and set status to ready-for-dev. |
| 2026-07-17 | Approved course correction set the story to blocked, externalized platform enablement as 6.1-P0 through 6.1-P4, and removed cross-repository gate resolution from Story 6.1 tasks. |
