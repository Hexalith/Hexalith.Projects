# Story 1.7: Open & List Projects

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As **Hexalith.Chatbot**,
I want **to open a Project and list the Projects visible to my tenant/user context**,
so that **I can present project choices and load the metadata needed to initialize a conversation** _(realizes UJ-1; FR-2, FR-5)_.

This story completes the Epic 1 read side over the projections established in Stories 1.4 and 1.5 and the authorization chain hardened in Story 1.6. Story 1.4 shipped only a minimal `GetProject` confirmation read after `CreateProject`; Story 1.7 grows that query into the Open Project read surface and adds `ListProjects` over `ProjectListProjection`.

**Scope discipline:** this story owns read/query contracts and server behavior for Open Project and List Projects only. Do not implement `UpdateProjectSetup`, `ArchiveProject`, new lifecycle events, reference link/unlink commands, ProjectContext assembly, resolution, FrontComposer UI, CLI, MCP, Aspire topology, production Dapr projection storage, or sibling ACL calls. If the Open Project response includes reference fields, they must be metadata-only and populated only from Projects-owned read models available in this story; until Epic 2 reference events/ACLs exist, reference collections are empty with explicit safe trust/freshness metadata rather than fabricated sibling data.

## Acceptance Criteria

1. **Open Project returns authorized metadata/setup/reference summary with freshness/trust state (FR-2, AR-8, AR-16).**
   **Given** an authorized request for a specific Project
   **When** `GetProject` is queried over `ProjectDetailProjection`
   **Then** it returns Project metadata, lifecycle state, setup metadata, visible reference summaries, and freshness/trust state
   **And** the response is metadata-only: no conversation transcript, file contents, memory payload, raw prompt, secret, token, full command body, unrestricted path, or raw sibling denial detail
   **And** `X-Hexalith-Freshness` is returned and the body carries deterministic freshness metadata derived from projection state, not wall-clock guessing.

2. **Archived/unavailable Projects are identifiable and never silently become active context (FR-2, FR-4, NFR-3).**
   **Given** an authorized read of an `Archived` or unavailable Project
   **When** the Project is opened
   **Then** the response clearly exposes lifecycle/availability state and marks conversation-context activation as blocked
   **And** archived/unavailable Projects are not treated as active conversation context by default
   **And** missing or unverifiable projection data fails closed with the existing safe-denial/read-model-unavailable envelope, not a generic `500`.

3. **List Projects is tenant-scoped, authorization-filtered, lifecycle-filterable, and metadata-only (FR-5, NFR-1, NFR-5).**
   **Given** an authorized list request
   **When** `ListProjects` is queried over `ProjectListProjection`
   **Then** results are filtered to the authoritative tenant and requesting principal, can filter by lifecycle state (`Active`, `Archived`, or all), and include enough metadata to present project choices without loading full Project Context
   **And** list rows include safe fields only: `projectId`, display name, lifecycle state, created/updated timestamps where available, and freshness/trust metadata
   **And** query responses do not accept `Idempotency-Key`.

4. **Unauthorized/cross-tenant open/list cannot leak existence or rows (FS-8, SM-3, AR-19).**
   **Given** an unauthorized, missing-tenant, disabled-tenant, stale-projection, non-member, gateway-denied, cross-tenant, or malformed-evidence request
   **When** Open Project or List Projects is attempted
   **Then** the request fails closed or filters rows through `ProjectQueryTenantFilter` before response construction
   **And** another tenant's projects are never returned, never counted, and never named
   **And** cross-tenant existing-project and missing-project responses remain externally indistinguishable.

5. **Contract spine, generated client, and tests prove the query shape (AR-15, FS-7).**
   **Given** the OpenAPI spine is the source of truth
   **When** Story 1.7 adds `ListProjects` and expands `GetProject`
   **Then** `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` defines the query contracts, metadata-only examples, safe-denial responses, freshness behavior, lifecycle filter semantics, and query idempotency rejection
   **And** `Client/Generated/*.g.cs` and idempotency helpers are regenerated from the spine and never hand-edited
   **And** OpenAPI contract tests, client generation tests, filtered tests, fingerprint gate, and FrontComposer inspect gate all pass.

6. **No compiler setting weakened; boundaries preserved; gates green (project-context.md).**
   All touched projects keep `net10.0`, nullable, implicit usings, and warnings-as-errors; no `NoWarn`, `#pragma`, nullable-disable, or broad suppressions are added to force green. Domain projections remain pure and infrastructure-free. Server owns HTTP/query authorization and safe ProblemDetails mapping. Client/CLI/MCP do not reference domain events or Dapr. No new package version is added unless unavoidable; any version goes through central package management. No sibling submodule pointer changes.

## Tasks / Subtasks

- [x] **Task 1 - Expand the OpenAPI Contract Spine for query reads** (AC: 1, 2, 3, 5)
  - [x] Keep `GET /api/v1/projects/{projectId}` as `operationId: GetProject`, but expand the `Project` response from the Story 1.4 minimal confirmation body into the Open Project body: metadata, `setupMetadata`, lifecycle, `contextActivation`/availability signal, reference summary collection, and freshness/trust metadata.
  - [x] Add `GET /api/v1/projects` as `operationId: ListProjects` with optional lifecycle filter (`active`, `archived`, all/absent), `X-Correlation-Id`, and `X-Hexalith-Freshness`.
  - [x] Ensure both query operations have no `Idempotency-Key` parameter and the server rejects query requests that include one after the appropriate authorization step.
  - [x] Define closed, metadata-only schemas for `ProjectListResponse`, `ProjectListItem`, `ProjectReferenceSummary` (empty until Epic 2 can populate), `ContextActivation`, and freshness/trust metadata. Do not add client-controlled `tenantId` fields.
  - [x] Keep examples synthetic and payload-free; no real tenant/user identifiers, host paths, tokens, file names/contents, transcript text, memory content, prompts, or sibling denial details.

- [x] **Task 2 - Regenerate client and update contract tests** (AC: 5, 6)
  - [x] Regenerate `src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs` and `HexalithProjectsIdempotencyHelpers.g.cs` from the spine. Do not hand-edit `.g.cs`.
  - [x] Update `tests/Hexalith.Projects.Contracts.Tests/OpenApi/OpenApiContractSpineTests.cs` to assert `ListProjects`, expanded `GetProject`, lifecycle filter values, freshness headers/body, no query idempotency key, safe-denial responses, and payload-free examples.
  - [x] Update client-generation tests so `ListProjectsAsync` exists and no idempotency helper is exposed for queries.

- [x] **Task 3 - Add/extend read models for list and open** (AC: 1, 2, 3, 6)
  - [x] Add `IProjectListReadModel` and an in-memory implementation parallel to `IProjectDetailReadModel` / `InMemoryProjectDetailReadModel`. It should fold `ProjectProjectionEnvelope` into `ProjectListProjection` and expose `ListAsync(authoritativeTenantId, lifecycleFilter, ct)`.
  - [x] Update `ProjectListItem` if needed so rows carry enough safe metadata for project choice presentation, including an `UpdatedAt` value derived from event data. For the current `ProjectCreated`-only event set, `UpdatedAt == CreatedAt`.
  - [x] Reuse `ProjectDetailItem.SetupMetadata` in the Open Project response; do not invent a raw setup body before Story 1.8 defines durable setup changes.
  - [x] Keep projection folds pure, deterministic, tenant-guarded, rebuildable, and throw-on-unknown-event. If any projection shape changes, extend the Story 1.5 rebuild/replay conformance tests rather than bypassing them.
  - [x] Update `docs/projection-catalog.md` to include `ProjectListProjection` and `ProjectDetailProjection` entries with owner, source events, key, tenant scoping, rebuild behavior, freshness semantics, and leakage boundary.

- [x] **Task 4 - Update server authorization and endpoint behavior** (AC: 1, 2, 3, 4, 6)
  - [x] Add list authorization (`AuthorizeListAsync` or equivalent) in `ProjectAuthorizationGate` using the existing ordered layers: JWT -> claim transform -> TenantAccessProjection freshness -> query-side filter -> EventStore validator -> Dapr deny-by-default evidence.
  - [x] Split metadata read from active-context activation: an authorized archived Project may be returned as metadata, but the response must mark context activation as blocked. Do not weaken write authorization or resolution active-project rules.
  - [x] Extend `ProjectQueryTenantFilter` to filter `ProjectListItem` collections as well as detail items. Filtering must happen before response construction.
  - [x] Update `ProjectsDomainServiceEndpoints` to map `GET /api/v1/projects` and expanded `GET /api/v1/projects/{projectId}`. Authorize before returning validation feedback for protected project-specific requests, preserving the Story 1.6 fix that invalid freshness must not become an oracle for unauthorized callers.
  - [x] Preserve the redacted safe-denial envelope: authorization denials at the HTTP edge collapse to `tenant_access_denied` / `resource_unavailable` with `details.visibility = redacted`.
  - [x] Register any new read model in `ProjectsServerServiceCollectionExtensions` with the same in-memory/test-friendly pattern as the current detail read model.

- [x] **Task 5 - Tier-1 projection/query-filter tests** (AC: 3, 4, 6)
  - [x] Add pure tests for lifecycle filtering over `ProjectListProjection` / `ProjectListItem` fixtures. Use fixed `DateTimeOffset` values only.
  - [x] Extend tenant-isolation tests so a tenant A project never appears in tenant B list results, counts, or filters.
  - [x] Extend `ProjectQueryTenantFilter` tests for detail and list paths, including empty authoritative tenant, null/empty item sets, and mixed-tenant rows.
  - [x] If projection item shape changes, extend `ProjectionRebuildConformance` coverage so rebuild == incremental, duplicate delivery idempotency, and out-of-order convergence still hold.

- [x] **Task 6 - Tier-2 server tests for Open/List** (AC: 1, 2, 3, 4)
  - [x] Add `GetProject_OpenProject_ReturnsSetupLifecycleReferencesAndFreshness` over a seeded `ProjectDetailProjection`.
  - [x] Add archived Project coverage: authorized archived detail returns metadata with context activation blocked; it is not silently treated as active.
  - [x] Add `ListProjects_Authorized_ReturnsOnlyTenantScopedFilteredRows` with mixed tenant seed data and lifecycle filters.
  - [x] Add query idempotency rejection tests for both `GetProject` and `ListProjects`, ensuring unauthorized project-specific reads still get safe denial before validation oracle behavior.
  - [x] Preserve/extend `GetProject_CrossTenantExistingProjectAndMissingProject_AreIndistinguishable`.
  - [x] Extend the reusable `ProjectTenantIsolationConformance` surfaces to include the list endpoint and list filter.
  - [x] Extend `NoPayloadLeakageAssertions` coverage over Open Project response, List Projects response, safe denial, and read-model-unavailable responses.

- [x] **Task 7 - Run required verification and record results** (AC: 5, 6)
  - [x] `dotnet build Hexalith.Projects.slnx`
  - [x] `tests/tools/run-filtered-tests.ps1`
  - [x] `tests/tools/run-openapi-fingerprint-gate.ps1`
  - [x] `tests/tools/run-frontcomposer-inspect-gate.ps1` (expected skip-clean unless `[Projection]` / `[Command]` contracts are added)
  - [x] Confirm `git status` shows no sibling submodule pointer changes and no unrelated file churn.

## Dev Notes

### Current On-Disk State to Build From

- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` already defines `POST /api/v1/projects` and a minimal `GET /api/v1/projects/{projectId}`. Its own comment says `ListProjects` and the full Open Project surface are deferred to Story 1.7. Grow this file; never hand-edit generated client files.
- `src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs` is pure, deterministic, tenant-guarded, rebuildable, and keyed by `ProjectIdentity`. It currently applies only `ProjectCreated` and documents itself as an "active project list"; Story 1.7 should make the list semantics lifecycle-aware without introducing new lifecycle events.
- `src/Hexalith.Projects/Projections/ProjectList/ProjectListItem.cs` currently carries `TenantId`, `ProjectId`, `Name`, `Lifecycle`, `Sequence`, and `CreatedAt`. If list rows need `UpdatedAt`, derive it from event data and update rebuild tests.
- `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailProjection.cs` and `ProjectDetailItem.cs` already carry `Description`, `SetupMetadata`, lifecycle, timestamps, and sequence. The existing server response omits `SetupMetadata`; Story 1.7 should surface safe setup metadata in the Open Project contract.
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` currently maps POST create and minimal GET project. It validates `X-Hexalith-Freshness` only after authorization for GET; preserve that anti-oracle ordering.
- `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs` currently denies `AuthorizeReadAsync` when `detail.Lifecycle != ProjectLifecycle.Active`. Story 1.7 needs a deliberate split: metadata reads can return authorized archived Projects, but context activation must be blocked. Do not weaken mutation authorization.
- `src/Hexalith.Projects.Server/TenantAccess/ProjectQueryTenantFilter.cs` filters `ProjectDetailItem` and detail collections only. Extend it for list rows and keep all filtering before response construction.
- `src/Hexalith.Projects.Server/IProjectDetailReadModel.cs` and `InMemoryProjectDetailReadModel.cs` are the current in-memory detail read model. Add a list read model rather than overloading detail reads as a list source.
- `docs/projection-catalog.md` currently documents only `TenantAccessProjection`; add Project list/detail projection catalog entries as part of this story.

### Previous Story Intelligence

- **Story 1.6 is security-sensitive carry-forward.** It added `TenantAccessProjection`, `ProjectAuthorizationGate`, `ProjectQueryTenantFilter`, claim-transform evidence, Dapr deny-by-default evidence, and negative-path tests. Do not bypass or reorder the authorization chain.
- Story 1.6 review fixed two high-risk issues that Story 1.7 must preserve: HTTP safe-denial must not expose internal authz reason codes, and GET validation must not run before authorization in a way that gives unauthorized callers an oracle.
- Story 1.6 maps external authorization denial to the redacted `tenant_access_denied` / `resource_unavailable` envelope. Keep internal diagnostic reason codes internal; only metadata-safe fields may surface.
- Story 1.5 added `ProjectListProjection.Rebuild(...)` and `ProjectDetailProjection.Rebuild(...)` as one-line delegates to `Empty.Apply(...)` plus reusable `ProjectionRebuildConformance`. If the list/detail item shape changes, update the conformance assertions; do not duplicate fold logic.
- Story 1.4 established the minimal create -> `ProjectCreated` -> list/detail projection -> `GetProject` vertical slice, `NoPayloadLeakageAssertions`, schema-evolution corpus, and safe-denial patterns. Story 1.7 extends that slice; it must not reimplement the aggregate or mutate read models as authority.

### Architecture and Scope Guardrails

- EventStore remains the sole write authority; read models are projections and are never authoritative for writes.
- Project data uses the user-facing tenant as the EventStore envelope tenant, never the reserved `system` tenant.
- Tenant authority comes from authenticated claims plus EventStore claim-transform only. Payload, query, and headers are comparison evidence, never authority.
- Query-side filtering is required even after API/JWT authorization. Command-side RBAC is not enough.
- Open Project is not ProjectContext assembly. Do not call Conversations, Folders, or Memories clients from aggregate/projection logic, and do not assemble sibling metadata before Epic 2/Epic 3 ACL work exists.
- References returned by this story must be safe summaries only. Empty is correct when no Projects-owned reference projection exists; fabricated placeholder IDs or sibling payloads are not.
- Archived Project behavior must be explicit: discoverable/readable to authorized callers, excluded from active context by default, and never silently activated.
- Idempotency keys are for mutations only. Queries must not accept or propagate `Idempotency-Key`.
- Use shared `ProjectLifecycle`, `ReferenceState`, and reason-code vocabulary. Do not create parallel enums or magic strings.

### Library / Framework Requirements

- Use the repository-pinned stack: .NET SDK `10.0.300`, `net10.0`, nullable, implicit usings, warnings-as-errors, central package management.
- OpenAPI/NSwag are already pinned (`NSwag.MSBuild 14.7.1`, `YamlDotNet 17.1.0` through central package management). Regenerate from the spine; no manual `.g.cs` edits.
- Tests use xUnit v3 + Shouldly. Keep Tier-1 tests pure: no Dapr, Aspire, network, containers, browser, or wall-clock waits.
- No external latest-version change is required for this story; local version pins and generated-contract gates are authoritative. Do not upgrade Fluent UI, Dapr, Aspire, Roslyn, Fluxor, xUnit, NSwag, or the SDK.

### Expected File / Structure Changes

```text
src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml       # MODIFY - GetProject expansion + ListProjects
src/Hexalith.Projects.Client/Generated/*.g.cs                           # REGENERATE ONLY - never hand-edit
src/Hexalith.Projects/Projections/ProjectList/ProjectListItem.cs         # MODIFY if UpdatedAt/list metadata needed
src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs   # MODIFY docs/fold only if required; keep pure
src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailItem.cs     # MODIFY only if Open response needs projected fields
src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs           # MODIFY - expanded GetProject + new ListProjects
src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs    # MODIFY - list auth + archived metadata-read split
src/Hexalith.Projects.Server/TenantAccess/ProjectQueryTenantFilter.cs     # MODIFY - list-row filtering
src/Hexalith.Projects.Server/IProjectListReadModel.cs                    # NEW
src/Hexalith.Projects.Server/InMemoryProjectListReadModel.cs             # NEW
src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs # MODIFY - read-model registration
docs/projection-catalog.md                                               # MODIFY - ProjectList/ProjectDetail entries
tests/Hexalith.Projects.Contracts.Tests/OpenApi/*                        # MODIFY
tests/Hexalith.Projects.Client.Tests/*                                   # MODIFY if generated client assertions need update
tests/Hexalith.Projects.Tests/*                                          # MODIFY/ADD Tier-1 projection/filter/replay tests
tests/Hexalith.Projects.Server.Tests/*                                   # MODIFY/ADD Open/List endpoint tests
```

### Testing Requirements

- **Tier-1:** projection list/detail behavior, lifecycle filtering, tenant filtering, `NoPayloadLeakage`, rebuild/replay conformance if item shape changes, deterministic timestamps only.
- **Tier-2:** Open Project and List Projects endpoint authorization, safe denial, lifecycle filter validation, archived context-blocking, freshness header/body, query idempotency rejection, and cross-tenant indistinguishability.
- **Contract/client:** OpenAPI shape, generated client shape, no query idempotency helper, metadata-only examples, fingerprint gate.
- **Security negatives:** missing tenant, `system` tenant, unknown tenant, disabled tenant, non-member, stale projection, malformed evidence, Dapr policy denial, cross-tenant detail read, mixed-tenant list rows, invalid freshness after authorization.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.7: Open & List Projects] - story statement and BDD acceptance criteria for `GetProject` and `ListProjects`.
- [Source: _bmad-output/planning-artifacts/epics.md#Requirements Inventory] - FR-2 Open Project, FR-5 List Projects, NFR-1 tenant isolation, NFR-2 metadata-only, NFR-3 fail-closed, NFR-5 p95 target, NFR-7 idempotency.
- [Source: _bmad-output/planning-artifacts/epics.md#Additional Requirements] - AR-8 projection requirements, AR-15 OpenAPI spine, AR-16 command/query errors and safe denial, AR-19 authorization chain, AR-23 testing requirements.
- [Source: _bmad-output/planning-artifacts/architecture.md#Format Patterns] - RFC 9457 ProblemDetails, safe-denial 404, query freshness/trust state, idempotency rejected on queries.
- [Source: _bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping] - Workspace management maps to `ProjectListProjection`, `ProjectDetailProjection`, `Queries/ListProjects`, and `GetProject`.
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md#FR-2: Open Project] - Open Project returns authorized metadata/lifecycle/setup/references and archived Projects cannot silently become active context.
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md#FR-5: List Projects] - List Projects tenant-scoped, authorization-filtered, lifecycle-filterable, enough metadata for choices.
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Component Strategy] - Project list/detail operational views, diagnostic header, metadata-only status/reason-code patterns, accessible non-color-only states.
- [Source: _bmad-output/implementation-artifacts/1-4-create-project-end-to-end-tracer-bullet.md] - minimal GetProject and current list/detail projection slice to extend.
- [Source: _bmad-output/implementation-artifacts/1-5-projection-rebuild-replay-idempotency.md] - rebuild/idempotency conformance and no-fold-duplication guardrails.
- [Source: _bmad-output/implementation-artifacts/1-6-tenant-access-layered-fail-closed-authorization.md] - layered authz, query-side filtering, safe-denial review fixes, and negative-path matrix to preserve.
- [Source: _bmad-output/project-context.md] - pinned stack, Dapr-only infra, EventStore envelope ownership, metadata-only logging, tenant isolation at every layer, central package management, no recursive submodules.
- [Source: src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml] - existing spine with minimal `GetProject` and explicit `ListProjects` deferral.
- [Source: src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs] - current pure, deterministic, tenant-guarded list projection.
- [Source: src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailProjection.cs] - current pure, deterministic, tenant-guarded detail projection.
- [Source: src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs] - current minimal GET behavior and safe ProblemDetails helpers.
- [Source: src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs] - existing authorization chain and active-only read gate to split carefully.
- [Source: src/Hexalith.Projects.Server/TenantAccess/ProjectQueryTenantFilter.cs] - existing query-side tenant filter to extend for list rows.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Red OpenAPI contract run: `dotnet test tests\Hexalith.Projects.Contracts.Tests\Hexalith.Projects.Contracts.Tests.csproj --filter FullyQualifiedName~OpenApiContractSpineTests --no-restore` failed for missing `LifecycleFilter`, `ListProjects`, `ProjectListResponse`, and expanded Open Project fields.
- Red list read-model run: `dotnet test tests\Hexalith.Projects.Server.Tests\Hexalith.Projects.Server.Tests.csproj --filter FullyQualifiedName~InMemoryProjectListReadModelTests --no-restore` failed for missing `InMemoryProjectListReadModel` and `ProjectListItem.UpdatedAt`.
- Red server endpoint run: targeted `CreateProjectEndpointTests` failed for missing list route, archived read denial, and missing query idempotency rejection.
- Verification: `dotnet build Hexalith.Projects.slnx` passed with 0 warnings / 0 errors.
- Verification: `tests\tools\run-filtered-tests.ps1` passed (Contracts 112, Client 17, Projects 99, Server 51).
- Verification: `tests\tools\run-openapi-fingerprint-gate.ps1` passed.
- Verification: `tests\tools\run-frontcomposer-inspect-gate.ps1` skipped clean (no `[Projection]` / `[Command]` contracts).
- Verification: `dotnet test Hexalith.Projects.slnx --no-restore` passed (Contracts 112, Client 17, Projects 99, Integration 1, Server 51).
- Review verification: `dotnet test tests\Hexalith.Projects.Server.Tests\Hexalith.Projects.Server.Tests.csproj --filter FullyQualifiedName~ProjectAuthorizationGateTests --no-restore` passed (8/8).
- Review verification: `dotnet build Hexalith.Projects.slnx` passed with 0 warnings / 0 errors.
- Review verification: `tests\tools\run-filtered-tests.ps1` passed (Contracts 112, Client 17, Projects 99, Server 54).
- Review verification: `tests\tools\run-openapi-fingerprint-gate.ps1` passed.
- Review verification: `tests\tools\run-frontcomposer-inspect-gate.ps1` skipped clean (no `[Projection]` / `[Command]` contracts).
- Review verification: `dotnet test Hexalith.Projects.slnx --no-restore` passed (Contracts 112, Client 17, Projects 99, Integration 1, Server 54).
- Review verification: `git diff --check` passed (no whitespace errors; Git emitted autocrlf warnings only).

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Expanded the OpenAPI spine with `ListProjects`, the Open Project response shape, metadata-only reference/context activation schemas, lifecycle filter semantics, and query idempotency rejection.
- Regenerated the NSwag client and idempotency helper artifacts from the spine; query response types expose no idempotency helpers.
- Added `IProjectListReadModel` and `InMemoryProjectListReadModel`, extended `ProjectListItem` with `UpdatedAt`, and added lifecycle-aware list projection filtering.
- Updated server authorization to allow authorized archived metadata reads while blocking context activation, added list authorization, and preserved authorization-before-validation for protected reads.
- Added Open/List endpoint behavior, deterministic freshness/trust metadata from projection state, safe-denial/read-model-unavailable handling, and query `Idempotency-Key` rejection.
- Added/extended contract, client-generation, projection, query-filter, tenant-isolation, no-payload-leakage, and server endpoint tests.
- Review auto-fixed 2 MEDIUM issues: added gate-level tests proving `projects:list` permission enforcement and archived metadata-read authorization, and normalized the regenerated client whitespace so `git diff --check` is clean.

### File List

- `_bmad-output/implementation-artifacts/1-7-open-list-projects.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/story-automator/orchestration-1-20260525-125327.md`
- `docs/projection-catalog.md`
- `src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs`
- `src/Hexalith.Projects.Client/Generated/HexalithProjectsIdempotencyHelpers.g.cs`
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml`
- `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs`
- `src/Hexalith.Projects.Server/IProjectListReadModel.cs`
- `src/Hexalith.Projects.Server/InMemoryProjectListReadModel.cs`
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs`
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs`
- `src/Hexalith.Projects.Server/TenantAccess/ProjectQueryTenantFilter.cs`
- `src/Hexalith.Projects/Projections/ProjectList/ProjectListItem.cs`
- `src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs`
- `tests/Hexalith.Projects.Client.Tests/ClientGenerationTests.cs`
- `tests/Hexalith.Projects.Contracts.Tests/OpenApi/OpenApiContractSpineTests.cs`
- `tests/Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.cs`
- `tests/Hexalith.Projects.Server.Tests/InMemoryProjectListReadModelTests.cs`
- `tests/Hexalith.Projects.Server.Tests/ProjectAuthorizationGateTests.cs`
- `tests/Hexalith.Projects.Server.Tests/ProjectQueryTenantFilterTests.cs`
- `tests/Hexalith.Projects.Tests/Projections/ProjectProjectionTests.cs`

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex  
Date: 2026-05-25

Outcome: Approved after auto-fixes. No CRITICAL issues remain.

Findings:

- MEDIUM - Gate-level proof for the new list authorization surface was incomplete. Endpoint tests exercised the happy path, but `ProjectAuthorizationGate` had no direct assertion that `projects:list` is the required permission or that missing list permission short-circuits at claim-transform. Fixed in `tests/Hexalith.Projects.Server.Tests/ProjectAuthorizationGateTests.cs`.
- MEDIUM - `git diff --check` found trailing whitespace in the regenerated NSwag client at `src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs:191`. Fixed by normalizing generated-client whitespace after regeneration.

AC validation:

- AC1/AC2: Open Project returns metadata/setup/context activation/reference/freshness fields; archived reads return metadata with activation blocked.
- AC3/AC4: List Projects is tenant-scoped, lifecycle-filterable, authorization-gated, and filtered before response construction; unauthorized/cross-tenant reads remain non-inferential.
- AC5: OpenAPI spine and generated client include `ListProjects`, expanded `GetProject`, freshness/trust fields, and no query idempotency helper.
- AC6: No compiler settings or package versions were weakened; no submodule pointer changes detected.

### Change Log

- 2026-05-25: Implemented Story 1.7 Open/List Projects read surface and moved status to review.
- 2026-05-25: Story-automator review cycle 1 auto-fixed 2 MEDIUM issues, verified gates, and moved status to done.
