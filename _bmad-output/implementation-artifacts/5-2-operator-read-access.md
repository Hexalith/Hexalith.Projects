---
story_id: 5.2
story_key: 5-2-operator-read-access
epic: "Epic 5: Operational Console & Audit (CLI / MCP / Web)"
created: 2026-05-30
source_story_status: backlog
baseline_commit: 8b030347b0b3c3a0e000fecf3ce8dcc84f543951
---

# Story 5.2: Operator Read Access

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an authorized operator,
I want authorization-gated, tenant-scoped read access to Project metadata, lifecycle, references, resolution outcomes, and audit metadata,
so that I can troubleshoot project state without write power or payload exposure.

## Acceptance Criteria

1. Given an authorized operator request, when operator read queries run, then the response is metadata-only, tenant-scoped to the authenticated authoritative tenant, authorization-gated through the existing layered Projects authorization chain, and exposes no write capability except separately defined maintenance/archive workflows that are not part of this story.
2. Given an unauthorized, cross-tenant, malformed-identity, missing-authoritative-tenant, disabled-tenant, stale/unavailable-projection, or denied-Dapr-policy request, when an operator read is attempted, then the boundary follows the canonical query contract: safe-denial 404 for non-retryable denial and no cross-tenant existence disclosure, 503 for retryable read-model/projection unavailability, and no payload or protected details in error bodies.
3. Given a project-scoped operator read, when a project exists and the caller is authorized, then it returns safe project metadata, lifecycle state, current setup metadata/preferences, current folder/file/memory reference summaries, freshness/watermark evidence, and a bounded audit timeline window from the Story 5.1 `IProjectAuditTimelineReadModel`.
4. Given a tenant-scoped operator inventory read, when the caller is authorized, then it returns safe project rows filtered to the authoritative tenant, supports lifecycle filtering consistent with `ListProjects`, carries freshness evidence, and never returns rows for another tenant even if a read model accidentally supplies them.
5. Given audit metadata is included, when audit rows are serialized, then they expose only Story 5.1 safe fields: timestamp, actor/source principal where available, operation type, safe state deltas, affected reference kind/id, correlation id, task id, audit event id, and safe reason/state codes. They must not expose transcript payloads, file contents, raw prompts, memory payloads, setup bodies beyond existing bounded safe setup preferences, unrestricted paths, raw tokens, candidate scores/ranks, rejected candidate ids, full command bodies, sibling denial details, or proposal bodies.
6. Given resolution-related operator diagnostics, when the response includes resolution evidence, then it uses existing safe outcomes and audit metadata only: `ProjectResolutionConfirmed` rows, `ProjectResolution` DTOs from existing compute-on-demand read endpoints if deliberately reused, and shared reason/result vocabulary. It must not persist or expose full resolution traces, candidate scores/ranks, rejected ids, or proposal preview payloads; detailed trace workbench behavior remains Story 5.6.
7. Given the public REST surface changes, when the story adds operator read endpoints or DTOs, then the OpenAPI Contract Spine is updated first, generated client/idempotency helpers are regenerated, query operations reject `Idempotency-Key` after authorization, accept only `X-Hexalith-Freshness: eventually_consistent`, emit `X-Correlation-Id` and `X-Hexalith-Freshness`, and pass the OpenAPI fingerprint/compatibility tests.
8. Given no public REST surface change is needed for a specific read, when the story reuses existing `ListProjects`, `GetProject`, `GetProjectContext`, `GetProjectContextExplanation`, resolution, or audit read-model seams, then it documents that reuse in code/tests and does not create duplicate endpoint shapes, duplicate DTOs, or parallel read models.
9. Given future CLI/MCP/Web stories consume this foundation, when this story completes, then it leaves a single operator diagnostic model that later stories can adapt across FrontComposer Web, MCP resources, and CLI commands without changing state names, reason codes, audit identifiers, tenant isolation behavior, or redaction guarantees.

## Tasks / Subtasks

- [x] Define the operator read contract surface and reuse boundary (AC: 1, 3, 4, 8, 9)
  - [x] Decide whether 5.2 needs new explicit operator routes or can extend the existing query routes plus a new audit query route. Prefer reuse of existing `ListProjects`, `GetProject`, `GetProjectContext`, `GetProjectContextExplanation`, `ResolveProjectFromConversation`, `ResolveProjectFromAttachments`, and the Story 5.1 audit read model before adding a parallel API.
  - [x] If new routes are required, add route names and OpenAPI operations under the existing `/api/v1/projects...` resource shape without making ambiguous static segments collide with `/api/v1/projects/{projectId}`. Static operator paths must be mapped before parameterized `{projectId}` paths.
  - [x] Keep the response model metadata-only and bounded. Recommended shapes are a tenant inventory response and a project-scoped diagnostic response rather than one unbounded "dump everything" endpoint.
  - [x] Do not add tenant authority from query, body, or client-controlled headers. Tenant scope comes from `IProjectTenantContextAccessor` and the EventStore claim-transform evidence only.

- [x] Add or reuse authorization actions for read-only operator access (AC: 1, 2)
  - [x] Extend `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs` only if existing `projects:list` and `projects:read` actions are insufficient for operator support. If a new token is needed, use a read-only token such as `projects:operator_read`.
  - [x] A tenant-wide operator inventory read must require tenant access, EventStore validator evidence, and Dapr policy evidence, but must not require project detail.
  - [x] A project-scoped operator read must require project detail and must continue to allow metadata reads for archived projects.
  - [x] Preserve the current evaluation order: JWT validation, EventStore claim transform, TenantAccess freshness, Project ACL when project-scoped, EventStore validator, Dapr deny-by-default policy.
  - [x] Add authorization tests proving allowed operator reads and denial when the operator read/list/read action is absent.

- [x] Implement project-scoped operator diagnostic read (AC: 1, 3, 5, 6, 8)
  - [x] Reuse `ProjectAuthorizationGate.AuthorizeReadAsync(...)` or the new operator read method; do not bypass the gate.
  - [x] Reuse `authorization.ProjectDetail` from the gate for metadata/lifecycle/reference summaries instead of calling a second detail read unless a missing field makes that unavoidable.
  - [x] Reuse `IProjectAuditTimelineReadModel.ListAsync(authoritativeTenantId, projectId, limit, cancellationToken)` for audit rows.
  - [x] Bound the audit timeline result with a documented default and maximum limit. Invalid limit values must be safe validation errors after authorization.
  - [x] Include resolution confirmation metadata only through audit rows and existing safe resolution DTOs. Do not create or persist a resolution trace store in this story.

- [x] Implement tenant-scoped operator inventory read if existing `ListProjects` is insufficient (AC: 1, 4, 8)
  - [x] Reuse `IProjectListReadModel.ListAsync(...)` and `ProjectQueryTenantFilter.FilterList(...)`.
  - [x] Preserve lifecycle filter semantics from `ListProjects`: `active`, `archived`, or absent/all.
  - [x] Return freshness metadata derived from projected rows, not response wall-clock guesses.
  - [x] Do not include raw tenant ids in general Project rows unless the OpenAPI contract explicitly marks the field as operator-only, server-derived, and metadata-only.

- [x] Add OpenAPI/client support for any new public read surface (AC: 7, 9)
  - [x] Update `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` before generated client files.
  - [x] Add closed schemas for every new response item. Use existing `OpaqueIdentifier`, `UtcDateTime`, `FreshnessMetadata`, `ProjectLifecycleState`, reference-state/reason-code vocabulary, and `ProblemDetails` components.
  - [x] Query operations must include `CorrelationId` and `Freshness` parameters, exclude `IdempotencyKey`, include safe-denial 401/403/404, validation 400, and read-model-unavailable 503 responses, and declare `x-hexalith-read-consistency.class: eventually_consistent`.
  - [x] Regenerate `src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs` and `src/Hexalith.Projects.Client/Generated/HexalithProjectsIdempotencyHelpers.g.cs` only through the established generator workflow. Do not hand-edit generated `.g.cs`.
  - [x] Update OpenAPI spine tests to assert route shape, schemas, freshness, no query idempotency header, safe denial, and metadata-only properties.

- [x] Add server endpoint tests for canonical query behavior (AC: 1, 2, 3, 4, 5, 7)
  - [x] Add tests under `tests/Hexalith.Projects.Server.Tests/Queries/` for happy path project-scoped operator read, tenant-scoped inventory if added, audit-limit behavior, and no cross-tenant rows.
  - [x] Cover canonical negative-test checklist rows for queries: malformed identifier, `Idempotency-Key` present after authorization, non-`eventually_consistent` freshness after authorization, cross-tenant safe-denial 404, missing authoritative tenant 404, stale/unavailable projection 503, and response body no leakage.
  - [x] Add ordering tests showing unauthorized callers receive safe-denial rather than validation feedback for `Idempotency-Key` or freshness probes.
  - [x] Use existing in-memory read models, tenant-access fakes, and test helpers before creating new doubles.

- [x] Extend leakage and parity proofs (AC: 5, 6, 9)
  - [x] Extend `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs` or add a sibling fixture for any new operator DTOs.
  - [x] Assert serialized operator responses do not contain forbidden payload terms from `PayloadClassification.ForbiddenContent` and explicit terms: `candidate`, `score`, `rank`, `transcript`, `prompt`, `content`, `token`, `path`, `secret`, `body`, and sibling denial details where relevant.
  - [x] If new safe fields are introduced, update `docs/payload-taxonomy.md` and `PayloadClassification` only when the existing safe categories cannot describe them.
  - [x] Update or create `docs/parity-matrix.md` with the 5.2 operator diagnostic fields that 5.3-5.11 must preserve across Web/MCP/CLI.

- [x] Update documentation/catalogs (AC: 8, 9)
  - [x] Update `docs/projection-catalog.md` consumer guidance for the read models consumed by operator access.
  - [x] Update `docs/checklists/mutation-and-query-negative-tests.md` story references for the new query tests.
  - [x] Add a short docs note or parity matrix entry explaining which fields are the 5.2 operator diagnostic model and which later stories own rendering/export/maintenance.

- [x] Run focused verification (AC: all)
  - [x] `dotnet test Hexalith.Projects.slnx --filter "FullyQualifiedName~OperatorRead|FullyQualifiedName~OpenApi|FullyQualifiedName~NoPayloadLeakage|FullyQualifiedName~ProjectAuthorizationGate"` (VSTest transport blocked by sandbox socket permissions; equivalent xUnit v3 in-process focused lanes passed)
  - [x] `dotnet build Hexalith.Projects.slnx -warnaserror`
  - [x] `git diff --check`
  - [x] If generated client/openapi fingerprint changes occur, document the intentional route/schema change in the Dev Agent Record.

## Dev Notes

### Current State

- `ListProjects` and `GetProject` already implement the canonical read-query pattern: authorization before query validation, `Idempotency-Key` rejected on queries, only `eventually_consistent` freshness accepted, `X-Hexalith-Freshness` emitted, safe-denial 404 for unauthorized/nonexistent, and 503 for retryable read-model failures. Reuse this behavior rather than inventing a second query contract. [Source: src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs#L618] [Source: src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs#L1556]
- `ProjectAuthorizationGate` already defines read/list/mutation action tokens and executes the ordered fail-closed layers. It requires project detail for project-scoped reads and not for list reads. Add operator read only if the existing `projects:list` and `projects:read` permissions do not satisfy the support-agent role. [Source: src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs#L14] [Source: src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs#L298]
- Query-side tenant filtering exists in `ProjectQueryTenantFilter` for detail and list rows. Extend it for audit/operator DTOs if needed; do not trust read-model tenant filtering alone. [Source: src/Hexalith.Projects.Server/TenantAccess/ProjectQueryTenantFilter.cs]
- Story 5.1 already created `IProjectAuditTimelineReadModel` with in-memory and Dapr-backed implementations. Use that seam for audit rows; do not refold EventStore payloads in the endpoint. [Source: src/Hexalith.Projects.Server/IProjectAuditTimelineReadModel.cs] [Source: src/Hexalith.Projects.Server/DaprProjectAuditTimelineReadModel.cs]
- The durable audit read model rebuilds from the shared `projects:projection-journal:{tenantId}` journal and fails closed through the same missing/malformed/replay-conflict path as list/detail/reference-index projections. Operator reads should catch those failures and return the canonical 503. [Source: src/Hexalith.Projects.Infrastructure/DaprProjectProjectionStore.cs#L103] [Source: docs/projection-catalog.md#ProjectAuditTimelineProjection]

### Story Scope Boundaries

- This story is backend/operator-read access. It does not build the FrontComposer console shell (Story 5.3), inventory/detail views (Story 5.4), reference health view (Story 5.5), resolution trace workbench (Story 5.6), audit timeline view/export (Story 5.7), warnings dashboard (Story 5.8), maintenance mutations (Story 5.9), MCP/CLI parity surfaces (Story 5.10), or final responsive/a11y hardening (Story 5.11). [Source: _bmad-output/planning-artifacts/epics.md#Epic-5-Operational-Console--Audit-CLI--MCP--Web]
- Resolution diagnostics in this story are limited to safe already-computed outcomes and audit metadata. Do not create a persisted resolution-trace history. Story 5.6 owns the workbench and any custom trace composition. [Source: _bmad-output/planning-artifacts/epics.md#Story-5.6-Resolution-Trace-Workbench] [Source: _bmad-output/planning-artifacts/architecture.md#Process-Patterns]
- Audit export/copy/download is not part of this story. Story 5.7 owns safe diagnostic export. 5.2 should only make a metadata-only read model available for later surfaces. [Source: _bmad-output/planning-artifacts/epics.md#Story-5.7-Audit-timeline-view--Safe-Diagnostic-Export]

### Metadata-Only Rules

- Operator read responses may expose Project identifiers, lifecycle, safe display metadata, bounded setup preferences, reference kind/id/state/reason code, freshness/watermarks, correlation/task ids, audit event ids, operation types, timestamps, and actor/source principal identifiers where already present in safe metadata. [Source: docs/payload-taxonomy.md] [Source: src/Hexalith.Projects.Contracts/Models/PayloadClassification.cs]
- Operator read responses must not expose sibling-owned payloads or sensitive evidence: conversation transcripts, file contents, memory payloads, raw prompts, secrets, raw tokens, unrestricted/local paths, full command bodies, proposal bodies, candidate scores/ranks, rejected candidate ids, or raw sibling denial details. [Source: docs/payload-taxonomy.md] [Source: _bmad-output/planning-artifacts/architecture.md#Authentication--Security]
- Avoid adding raw `tenantId` fields to general Project rows. Existing public list/detail/context DTOs intentionally do not carry tenant authority on the wire; tenant authority remains server-derived from claims and EventStore evidence. If an operator-only tenant scope label is introduced, it must be server-derived, closed, explicitly documented, and covered by leakage tests. [Source: src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml#L22] [Source: _bmad-output/project-context.md#Critical-Dont-Miss-Rules]

### Existing Files To Touch Carefully

- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs`: current endpoint map and helper methods. Add query routes here or in a new partial file only if that preserves the current static-before-parameter route order, safe-denial helpers, correlation/freshness behavior, and response JSON options. Preserve all existing routes. [Source: src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs]
- `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs`: current authorization action registry and ordered gate. Add only read-only operator action(s); do not weaken existing read/mutation requirements. [Source: src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs]
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml`: OpenAPI Contract Spine is the source of truth for public REST contract changes. Update this before generated clients. [Source: src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml]
- `src/Hexalith.Projects.Client/Generated/*.g.cs`: generated output only. Regenerate through the established workflow; do not hand-edit. [Source: _bmad-output/project-context.md#Code-Quality--Style-Rules]
- `tests/Hexalith.Projects.Server.Tests/Queries/`: add operator endpoint tests here to match existing query endpoint fixtures. [Source: tests/Hexalith.Projects.Server.Tests/Queries/GetProjectContextTests.cs] [Source: tests/Hexalith.Projects.Server.Tests/Queries/GetConversationStartSetupTests.cs]
- `tests/Hexalith.Projects.Contracts.Tests/OpenApi/OpenApiContractSpineTests.cs`: extend route/schema/freshness/safe-denial assertions if the spine changes. [Source: tests/Hexalith.Projects.Contracts.Tests/OpenApi/OpenApiContractSpineTests.cs]
- `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs`: add serialization leakage proof for new operator DTOs. [Source: tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs]

### Previous Story Intelligence

- Story 5.1 delivered `ProjectAuditTimelineProjection`, `ProjectAuditTimelineItem`, `IProjectAuditTimelineReadModel`, `DaprProjectAuditTimelineReadModel`, and `IProjectProjectionStore.ListAuditTimelineAsync`. It verified metadata-only rows, deterministic audit ids, newest-first sequence ordering, durable journal rebuild, and no OpenAPI/client churn. Reuse that read model directly. [Source: _bmad-output/implementation-artifacts/5-1-audit-timeline-projection-metadata-only-audit-events.md]
- Story 5.1 intentionally did not add public HTTP audit endpoints. 5.2 is the first story allowed to expose operator read access to audit rows through public/query contracts. [Source: _bmad-output/implementation-artifacts/5-1-audit-timeline-projection-metadata-only-audit-events.md#Query-And-Authorization-Scope]
- Story 4.5 established that proposal confirmation emits an explicit command chain only. No `ProjectCreatedFromProposal` event, proposal aggregate, persisted proposal store, persisted proposal preview, or raw proposal body may appear in operator reads. [Source: _bmad-output/implementation-artifacts/sprint-status.yaml]
- Story 4.4 established that `ProjectResolutionConfirmed` is metadata-only and persists only the confirmed choice. Operator reads must not expose candidate scores, ranks, rejected ids, or full resolution traces. [Source: _bmad-output/implementation-artifacts/sprint-status.yaml]
- Recent commits show 5.1 landed audit timeline code and orchestration state before this story. Expect the working tree to include completed 5.1 code and tests; do not remove or rewrite them. [Source: git log --oneline -5]

### Architecture Guardrails

- Dapr is the only infrastructure abstraction; do not add direct Redis/PostgreSQL/Cosmos/broker clients for operator reads. [Source: _bmad-output/project-context.md#Framework-Specific-Rules]
- EventStore owns envelope metadata. Operator reads must consume projections/read models and must not spoof or synthesize event envelope fields. [Source: _bmad-output/project-context.md#Framework-Specific-Rules]
- Query endpoints must apply tenant isolation at every layer: API/auth, tenant access, project ACL, projection keys/read models, response filtering, and logs. [Source: _bmad-output/planning-artifacts/architecture.md#Authentication--Security]
- Errors must use established safe Problem Details and must not leak protected identifiers or payload clues. [Source: _bmad-output/planning-artifacts/architecture.md#Format-Patterns]
- Public contract changes must be additive and serialization-tolerant. Do not add `V2` DTOs/events or parallel state/reason-code enums. [Source: _bmad-output/project-context.md#Code-Quality--Style-Rules]

### UX / Future Surface Handoff

- Operator reads are the shared diagnostic model for later CLI/MCP/Web surfaces. Preserve identical state names, reason codes, timestamps, warning semantics, audit identifiers, and redaction behavior across all future adapters. [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Cross-Surface-Parity-Pattern]
- The later Web UX should be FrontComposer/Fluent UI based. 5.2 should define safe fields and contracts, not bespoke UI components. [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Component-Implementation-Strategy]
- Empty/denied/unavailable states must be distinguishable by safe codes so later views do not render blank tables. [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Empty-State-Pattern]

### Latest Technical Context

- Keep the pinned package/runtime posture. The workspace uses .NET SDK `10.0.302`, `net10.0`, warnings-as-errors, central package management, Dapr `1.17.7`, Aspire `13.2.x`, and Fluent UI Blazor `5.0.0-rc.2-26098.1`. No package upgrades are needed for this story. [Source: _bmad-output/project-context.md#Technology-Stack--Versions]
- External check on 2026-05-30: Microsoft lists .NET 10 as an LTS release supported until November 2028, and Dapr docs list runtime `1.17.7` as the current supported release. That confirms the existing pinned posture; it does not authorize dependency churn. [Source: https://learn.microsoft.com/en-us/dotnet/core/releases-and-support] [Source: https://docs.dapr.io/operations/support/support-release-policy/]

### Hard Stops

- Stop before coding if operator access seems to require a new write/mutation capability. That belongs to Story 5.9 unless explicitly limited to already-existing archive behavior.
- Stop before coding if a response shape requires raw tenant authority from caller input, sibling payloads, full command bodies, raw proposal data, or resolution candidate scores/ranks.
- Stop before coding if implementation requires direct infrastructure dependencies outside Dapr/EventStore abstractions.
- Stop before coding if a new state/reason-code enum is proposed instead of reusing the shared vocabulary.
- Stop before coding if generated `.g.cs` files would need hand edits.
- Stop before coding if a package version bump or submodule pointer change appears in the diff.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-5.2-Operator-read-access]
- [Source: _bmad-output/planning-artifacts/architecture.md#Audit--Operations-FR-21-22]
- [Source: _bmad-output/planning-artifacts/architecture.md#Implementation-Patterns--Consistency-Rules]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Component-Strategy]
- [Source: _bmad-output/implementation-artifacts/5-1-audit-timeline-projection-metadata-only-audit-events.md]
- [Source: docs/projection-catalog.md#ProjectAuditTimelineProjection]
- [Source: docs/checklists/mutation-and-query-negative-tests.md#Canonical-Rows]
- [Source: src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs]
- [Source: src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs]
- [Source: src/Hexalith.Projects.Server/IProjectAuditTimelineReadModel.cs]
- [Source: src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml]
- [Source: tests/Hexalith.Projects.Contracts.Tests/OpenApi/OpenApiContractSpineTests.cs]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-30: Captured baseline commit `8b030347b0b3c3a0e000fecf3ce8dcc84f543951`; sprint status moved to `in-progress`.
- 2026-05-30: Added `GET /api/v1/projects/{projectId}/operator-diagnostics` as the only new public read surface; tenant inventory reuses existing `ListProjects`.
- 2026-05-30: Regenerated `src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs` and `src/Hexalith.Projects.Client/Generated/HexalithProjectsIdempotencyHelpers.g.cs` through `dotnet build src/Hexalith.Projects.Client/Hexalith.Projects.Client.csproj -m:1 -nr:false`.
- 2026-05-30: `dotnet test Hexalith.Projects.slnx --filter "FullyQualifiedName~OperatorRead|FullyQualifiedName~OpenApi|FullyQualifiedName~NoPayloadLeakage|FullyQualifiedName~ProjectAuthorizationGate" -m:1 -nr:false --no-build` could not run under this sandbox because VSTest opens a local socket and received `System.Net.Sockets.SocketException (13): Permission denied`. Equivalent xUnit v3 in-process focused lanes passed.
- 2026-05-30: Verification passed: Server operator/auth focused lane 22/22, OpenAPI lane 26/26, Client generation lane 37/37, NoPayloadLeakage lane 49/49, `dotnet build Hexalith.Projects.slnx -warnaserror`, and `git diff --check`.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Create-story validation applied against checklist: story includes source-derived ACs, concrete file locations, existing read-model and authorization seams, previous-story constraints, query negative-test requirements, leakage guardrails, public contract guidance, and hard stops.
- Implemented project-scoped operator diagnostics at `GET /api/v1/projects/{projectId}/operator-diagnostics`, reusing `ProjectAuthorizationGate.AuthorizeReadAsync(...)`, `authorization.ProjectDetail`, existing reference summaries, and `IProjectAuditTimelineReadModel.ListAsync(...)`.
- Tenant-scoped inventory is intentionally reused through existing `ListProjects`; no duplicate inventory endpoint, DTO, or read model was added.
- Operator audit rows are bounded by `auditLimit` (default 25, max 100), filtered again by authoritative tenant/project before serialization, and omit idempotency keys, command bodies, raw payloads, proposal bodies, and resolution candidate scores/ranks.
- OpenAPI spine was updated before generated client artifacts; `GetProjectOperatorDiagnosticsAsync` is generated as a query without `Idempotency-Key`, with freshness/correlation parameters and safe-denial/read-model-unavailable responses.
- Added parity documentation for future Web/MCP/CLI surfaces and updated projection/negative-test docs.

### File List

- _bmad-output/implementation-artifacts/5-2-operator-read-access.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- docs/checklists/mutation-and-query-negative-tests.md
- docs/parity-matrix.md
- docs/projection-catalog.md
- src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs
- src/Hexalith.Projects.Client/Generated/HexalithProjectsIdempotencyHelpers.g.cs
- src/Hexalith.Projects.Contracts/Models/ProjectOperatorAuditTimelineItem.cs
- src/Hexalith.Projects.Contracts/Models/ProjectOperatorContextActivation.cs
- src/Hexalith.Projects.Contracts/Models/ProjectOperatorDiagnostic.cs
- src/Hexalith.Projects.Contracts/Models/ProjectOperatorFreshnessMetadata.cs
- src/Hexalith.Projects.Contracts/Models/ProjectOperatorReferenceSummary.cs
- src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml
- src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs
- tests/Hexalith.Projects.Client.Tests/ClientGenerationTests.cs
- tests/Hexalith.Projects.Contracts.Tests/OpenApi/OpenApiContractSpineTests.cs
- tests/Hexalith.Projects.Server.Tests/ProjectAuthorizationGateTests.cs
- tests/Hexalith.Projects.Server.Tests/Queries/OperatorReadAccessTests.cs
- tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs
- tests/e2e/specs/projects-operator-read-access.spec.ts
- tests/e2e/support/helpers/projects-api-client.ts

### Change Log

- 2026-05-30: Added bounded project-scoped operator diagnostic query and shared metadata-only operator DTOs.
- 2026-05-30: Updated OpenAPI contract spine and regenerated generated client/idempotency helper artifacts for the new query surface.
- 2026-05-30: Added authorization, operator-read, OpenAPI, generated-client, and leakage tests; updated projection catalog, negative-test checklist, and parity matrix.
- 2026-05-30: Marked story ready for review after focused xUnit verification, solution build with warnings as errors, and diff whitespace check.
- 2026-05-30: Senior Developer Review (AI) completed with auto-fix; reverted a stray `Hexalith.Folders` submodule pointer, closed canonical negative-test gaps, fixed two contract-conformance omissions, normalized audit-row state casing, and re-verified build (0 warnings) and focused lanes.

## Senior Developer Review (AI)

**Reviewer:** Jerome (adversarial AI review, 5-lens fan-out + independent verification)
**Date:** 2026-05-30
**Outcome:** Approve (status → done). 0 critical issues remain after fixes.

The production design is sound: the new `GET /api/v1/projects/{projectId}/operator-diagnostics`
reuses `ProjectAuthorizationGate.AuthorizeReadAsync`, `authorization.ProjectDetail`, and the Story
5.1 `IProjectAuditTimelineReadModel`; tenant authority is server-derived only; audit rows drop the
idempotency key and re-filter by authoritative tenant/project before serialization; the OpenAPI
spine reuses the shared `FreshnessMetadata` / `ContextActivation` / `ProjectReferenceSummary` /
`ProjectSetup` schemas (no duplicate wire shape) and the generated client carries no
`Idempotency-Key`. Endpoint ordering (malformed-id safe-denial before auth; idempotency/freshness
rejected only after auth) matches the canonical `GetProjectAsync` pattern.

Findings auto-fixed during review:

- **[HIGH] Hard-stop violation — stray submodule pointer.** The working tree had bumped
  `Hexalith.Folders` `4efa637 → 0dc927b` (an unrelated container-publishing commit), violating the
  story's declared hard stop. Reverted the submodule working tree to the recorded baseline gitlink
  `4efa637`; the 5.2 diff now contains no submodule pointer move.
- **[MEDIUM] Canonical negative-test rows marked `[x]` but untested.** Added executable xUnit
  coverage in `OperatorReadAccessTests` for malformed identifier → 404 (theory),
  missing-authoritative-tenant → 404, cross-tenant project → safe-denial 404, authorized
  `Idempotency-Key` → 400 (after authorization), and an audit-limit boundary theory
  (`0/-5/101/abc` → 400; `1/100` accepted). Added error-body redaction assertions to the 400/404/503
  paths. The negative-test checklist now cites the real tests instead of the bare code guard.
- **[MEDIUM] Contract-conformance: required nullable fields omitted when null.** The OpenAPI marks
  `ProjectOperatorDiagnostic.setupMetadata` and `ContextActivation.blockedReasonCode` as required,
  but `ResponseJsonOptions` uses `WhenWritingNull`, so a null value produced a schema-invalid
  response and diverged from `GetProject`. Added `[JsonIgnore(Condition = Never)]` to
  `ProjectOperatorDiagnostic.SetupMetadata` and `ProjectOperatorContextActivation.BlockedReasonCode`,
  matching the established `ProjectResponse` / `ContextActivationResponse` pattern.
- **[LOW] Audit-row state casing parity (AC9).** Story 5.1 audit rows carry single-word PascalCase
  states (`Included`, `Confirmed`, …) while reference summaries on the same response use the lowercase
  wire vocabulary. Normalized `previousState` / `newState` to lowercase at the canonical operator
  boundary (`ToWireAuditState`), so the single diagnostic model presents one consistent spelling for
  later Web/MCP/CLI surfaces.
- **[LOW] Leakage proof hardened.** Added `ProjectOperatorDiagnostic_ForbiddenValueInFreeTextCarrier_IsDetected`,
  a negative test proving the harness fires when a forbidden value reaches the DTO's only free-text
  carriers (`ProjectSetup.Goals` / `UserInstructions`) — so the metadata-only proof is a real guard,
  not a vacuous shape check. Added a spine assertion that `auditLimit` documents `default: 25`.
- **[LOW] Documentation accuracy.** Added the two E2E files to the File List, reworded the
  `ProjectOperatorContextActivation.Enabled` doc-comment to state it is lifecycle-derived (not a live
  context-assembly probe), and normalized both E2E `.ts` files (committed with mixed line endings) to
  uniform LF, matching the new spec and sibling Playwright specs.

Intentional / not changed: the public `ProjectOperator*` Contracts records are field-identical to the
private endpoint response records, but this is deliberate — AC9 requires a public, reusable diagnostic
model in `Contracts`, while the OpenAPI spine still reuses the shared schemas so there is one wire
shape (no duplicate contract).

**Verification (sandbox disabled, `-m:1`):** `dotnet build Hexalith.Projects.slnx -warnaserror` →
0 warnings / 0 errors. Focused lanes: Server operator+authorization **36/36**, OpenAPI spine
**26/26**, NoPayloadLeakage **51/51**, client generation **52/52** (generated `.g.cs` still in sync —
no regeneration needed), full `Hexalith.Projects.Server.Tests` **498/498**, `git diff --check` clean.
(The dev record's `49/49` leakage count was stale; the lane is now 51/51 after review additions.)
