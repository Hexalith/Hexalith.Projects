---
stepsCompleted:
  - 1
  - 2
  - 3
  - 4
  - 5
  - 6
  - 7
  - 8
lastStep: 8
status: 'superseded'
supersededAt: '2026-07-16'
supersededBy: '_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md'
historicalEvidence: true
completedAt: '2026-05-24'
inputDocuments:
  - _bmad-output/planning-artifacts/briefs/brief-Hexalith.Projects-2026-05-24/brief.md
  - _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md
  - _bmad-output/planning-artifacts/ux-design-specification.md
  - _bmad-output/planning-artifacts/implementation-readiness-report-2026-05-24.md
  - _bmad-output/planning-artifacts/research/domain-eventstore-persistence-for-hexalith-projects-module-data-research-2026-05-24.md
  - _bmad-output/planning-artifacts/research/technical-hexalith-folders-integration-research-2026-05-24.md
  - _bmad-output/planning-artifacts/research/technical-hexalith-projects-referencing-conversations-research-2026-05-24.md
  - _bmad-output/planning-artifacts/research/technical-hexalith-tenants-in-hexalith-projects-tenant-management-isolation-research-2026-05-24.md
  - _bmad-output/planning-artifacts/research/technical-hexalith-memories-rag-research-2026-05-24.md
  - _bmad-output/planning-artifacts/research/technical-frontcomposer-hexalith-projects-web-ux-research-2026-05-24.md
  - _bmad-output/project-context.md
workflowType: 'architecture'
project_name: 'Hexalith.Projects'
user_name: 'Jerome'
date: '2026-05-24'
---

# Architecture Decision Document

> **Superseded historical evidence.** This May 2026 design is not normative. The sole current architecture is [the July 2026 Architecture Spine](architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md).

_This document builds collaboratively through step-by-step discovery. Sections are appended as we work through each architectural decision together._

## Project Context Analysis

### Requirements Overview

**Functional Requirements:**
22 FRs across 6 feature areas (no epics document yet):
- **Project Workspace Management (FR-1–FR-5):** create/open/update-setup/archive/list.
  Architecturally → a single `ProjectAggregate` (write authority) plus tenant-scoped
  list/detail projections. Name is the only required create input; folder may be
  auto-created from the project name.
- **Context References (FR-6–FR-11):** link/move conversation (single-project membership,
  explicit move), set the single v1 Project Folder, link/unlink file & memory references.
  Architecturally → reference-by-ID only; underlying resources stay owned by sibling
  contexts; unlink never deletes the resource; folder is replaceable but required while active.
- **Project Resolution (FR-12–FR-15):** resolve candidates from conversation/attachments,
  confirm ambiguous matches, propose new project. Architecturally → resolution returns
  NoMatch/SingleCandidate/MultipleCandidates with reason codes; never silently attaches;
  inference never creates a project without confirmation; archived excluded by default.
- **Project Context Assembly (FR-16–FR-18):** get/explain/refresh ProjectContext.
  Architecturally → `ProjectContext` is an assembled, authorization-filtered read result
  (NOT a persisted aggregate); surfaces excluded/unauthorized/stale/unavailable references
  with safe reason codes.
- **Project Setup Quality (FR-19–FR-20):** validate setup; serve conversation-start setup.
  Architecturally → setup is durable conversation-behavior/context policy, additive and
  serialization-tolerant; validation rejects secrets/unrestricted paths/foreign payloads.
- **Audit & Operations (FR-21–FR-22):** metadata-only audit events; authorization-gated
  operator read access. Architecturally → audit derives from EventStore envelope metadata +
  Project events; operator surface is read/metadata-only (plus archive/troubleshooting).

**Non-Functional Requirements:**
- **Security/privacy:** tenant isolation across reads/writes/links/resolution/assembly;
  Project data uses the *user-facing* tenant as the EventStore envelope tenant (vs Tenants'
  `system`). Logs/diagnostics metadata-only.
- **Reliability:** fail closed when authorization/lifecycle/resource availability is
  unverifiable.
- **Observability:** structured metadata (reason codes, correlation IDs, freshness) sufficient
  to troubleshoot resolution/assembly without exposing payloads.
- **Performance:** p95 < 500 ms internal target for list/open/resolve/context → favors
  precomputed tenant-scoped projections over request-time fan-out.
- **Compatibility:** additive, serialization-tolerant public contracts; no `V2` event types.

### Scale & Complexity
- Primary domain: distributed .NET backend domain service (Hexalith.EventStore + Dapr +
  Aspire) with generated CLI / MCP / FrontComposer-Web operational surfaces.
- Complexity level: **High** (event-sourced CQRS, multi-tenant isolation, four cross-context
  integrations, payload-safety/AI-governance sensitivity, three generated surfaces).
- Estimated architectural components: one `ProjectAggregate`; ~6 projections (list, detail,
  reference index, resolution trace, audit timeline, conversation-start setup); a local
  TenantAccess projection; four Anti-Corruption Layers (Conversations, Folders, Memories,
  Tenants); host (API/`/process`/projection dispatch/auth); CLI; MCP; FrontComposer Web.

### Technical Constraints & Dependencies
- **Greenfield**: no `Hexalith.Projects` module code exists; scaffold from the
  `Hexalith.Tenants` module shape (Contracts/Server/host/Client/Aspire/ServiceDefaults/Testing).
- **EventStore is the only write authority**; persist-then-publish; pure `Handle`/`Apply`;
  rejections are events, not exceptions; EventStore owns envelope metadata.
- **Dapr is the only infrastructure abstraction** — no direct Redis/Postgres/Cosmos/broker in
  contracts/client/domain; pub/sub is at-least-once → idempotent handlers.
- **Reference-don't-own** boundary to Conversations/Folders/Memories via ACLs. Conversations
  owns Conversation-to-Project assignment; Projects calls the Conversations reassignment API through
  ACLs (`ConfirmResolutionAssignmentAsync` / `ReassignConversationProjectAsync`) rather than storing
  local conversation membership. Conversation list/read gaps were closed by the Epic 2/4 ACL paths.
  Folders contract is ahead of its wired REST server (CreateFolder/ACL not yet external) — plan
  against the typed client.
- **FrontComposer** generates Web+MCP+CLI from `[Projection]`/`[Command]` contracts; Fluent UI
  v5 RC is pinned/high-risk. Pinned set (do not casually bump): Fluent UI, Dapr, Aspire,
  Roslyn, xUnit generation, .NET SDK.
- **Identity:** canonical `{tenant}:projects:{projectId}`; derive all keys/topics/groups/logs
  from it.

### Cross-Cutting Concerns Identified
- Tenant isolation (canonical identity + query-side authorization filtering).
- Metadata-only redaction / payload-safety (events, logs, audit, diagnostics, all surfaces).
- Fail-closed authorization & projection freshness/watermarks/ETags.
- Idempotency under at-least-once Dapr delivery (commands + projection handlers).
- Additive, serialization-tolerant schema evolution (no `V2`).
- Cross-surface semantic parity (states, reason codes, audit IDs) across CLI/MCP/Web.
- Observability/audit (correlation/causation, structured metadata only).

## Starter Template Evaluation

### Primary Technology Domain

.NET 10 event-sourced domain-service module (Hexalith.EventStore + Dapr + Aspire) exposing
three operational surfaces (CLI / MCP / FrontComposer Web) over one shared model. No public
starter template applies; the "starter" is the established Hexalith module scaffold convention.

### Version Posture

All package versions are centrally pinned (`Directory.Packages.props`) and governed by the
"do not casually upgrade" rule. Volatile dependencies were web-verified in the 2026-05-24
research: Fluent UI Blazor 5.0.0-rc.2 (RC1 2026-02-18), MCP C# SDK v1.0 (2026-03-05),
.NET SDK 10.0.302, Dapr 1.17.x, Roslyn 4.12.0, Fluxor 6.9.0, Aspire 13.4.6, xUnit v3.
This step selects a scaffold pattern, not versions.

### Starter Options Considered

- **Mirror Hexalith.Tenants (8 projects):** canonical tenant-isolation + local-projection
  reference, lean and pure-domain — but lacks Cli/Mcp/UI/Workers that Projects requires.
- **Mirror Hexalith.Folders (12 projects):** matches Projects' three-surface footprint
  (adds Cli, Mcp, UI, Workers) plus OpenAPI Contract Spine, idempotency hasher,
  RFC 9457 + safe-denial, layered authorization — but its bespoke Blazor UI should be
  replaced by FrontComposer for Projects, and its contract is currently ahead of its server.

### Selected Starter: Hybrid Hexalith module scaffold (Folders shape + Tenants domain patterns + FrontComposer UI)

**Rationale for Selection:**
Projects is a "one model, three surfaces" module, so it needs Folders' fuller project set from
the start. Its highest-risk requirements (tenant isolation, fail-closed projections) are best
modeled on Tenants. Its Web UX is mandated to be FrontComposer-generated, replacing a bespoke
UI project. Folders' cross-cutting patterns (idempotency, RFC 9457, safe-denial, layered authz,
OpenAPI contract spine) are directly reusable.

**Initialization Approach (no CLI generator exists):**

```text
# Mirror the sibling-module structure into the new Projects module:
src/Hexalith.Projects.Contracts      # commands, events, queries, DTOs, identifiers, UI enums
src/Hexalith.Projects.Client         # consumer-facing typed client (+ Chatbot integration)
src/Hexalith.Projects                # domain core: Aggregates/, Projections/, Queries/,
                                      #   Authorization/, ProjectsModule.cs, DI extensions
src/Hexalith.Projects.Server         # API, /process, projection dispatch, auth, ProblemDetails
src/Hexalith.Projects.ServiceDefaults
src/Hexalith.Projects.Aspire
src/Hexalith.Projects.AppHost
src/Hexalith.Projects.Workers        # projection workers / Tenants-event subscription host
src/Hexalith.Projects.Cli            # `projects` CLI (FrontComposer-aligned evidence)
src/Hexalith.Projects.Mcp            # MCP resources/tools (FrontComposer-generated descriptors)
src/Hexalith.Projects.Testing        # reusable fakes/builders
tests/...                            # xUnit v3 Tier-1/2/3
# Drop in shared build config from Hexalith.Builds/Samples:
Module.Directory.Build.props, Module.Directory.Packages.props
# Initialize ONLY root-level submodules (no --recursive):
#   Hexalith.EventStore, Hexalith.Tenants, Hexalith.Conversations, Hexalith.Folders,
#   Hexalith.Memories, Hexalith.FrontComposer, Hexalith.Commons, Hexalith.AI.Tools
```

**Architectural Decisions Provided by Starter:**

- **Language & Runtime:** C#/.NET 10 (`net10.0`; Contracts/SourceTools-facing types
  `netstandard2.0`-safe where they feed FrontComposer), nullable + implicit usings +
  warnings-as-errors, central package management.
- **Persistence/Write:** Hexalith.EventStore command pipeline; `ProjectAggregate`;
  persist-then-publish; pure Handle/Apply; rejection events.
- **Read:** EventStore projections (CachingProjectionActor/ETag/notifiers); tenant-scoped.
- **Infrastructure:** Dapr only (state/pub-sub/actors/service-invocation); Aspire AppHost topology.
- **UI generation:** FrontComposer `[Projection]`/`[Command]` contracts → Web + MCP + CLI;
  Fluent UI Blazor v5 RC (pinned).
- **Testing:** xUnit v3 + Shouldly + NSubstitute; EventStore/Tenants Testing fakes;
  Verify.XunitV3 for FrontComposer snapshots; bUnit; Playwright E2E (Node ≥24).
- **Cross-cutting (from Folders):** OpenAPI contract spine, idempotency hasher,
  RFC 9457 + Hexalith problem extensions, safe-denial (404), layered authorization chain.

**Note:** Project initialization using this scaffold should be the first implementation story.
Open structural question to resolve in decisions: new root-level submodule vs. in-umbrella module.

## Core Architectural Decisions

### Decision Priority Analysis

**Critical Decisions (Block Implementation):**
- Write authority = single `ProjectAggregate` on Hexalith.EventStore; `ProjectContext` is an
  assembled read result, not a persisted aggregate. *(inherited + confirmed)*
- Tenant identity: Project streams use the user-facing tenant as the EventStore envelope
  tenant (`{tenant}:projects:{projectId}`); Tenants management stays under `system`.
- Conversation reference model: **Pattern A** — query Conversations by `ProjectId` via an ACL,
  no local conversation storage.
- Aggregate design: **single `ProjectAggregate` with bounded references** (one Folder ref;
  bounded File/Memory ref sets; conversation membership derived, not stored).
- Project Resolution: **compute-on-demand**; persist only the confirmed choice
  (`ProjectResolutionConfirmed`).
- API/contract: **OpenAPI Contract Spine** (Folders-style) as single source of truth.

**Important Decisions (Shape Architecture):**
- Reads via tenant-scoped EventStore projections (CachingProjectionActor/ETag/notifiers).
- Local `TenantAccessProjection` fed by Tenants events (durable store from the start).
- Four Anti-Corruption Layers (Conversations, Folders, Memories, Tenants).
- Auth: JWT/OIDC + EventStore claim-transform + layered tenant authz + query-side filtering.
- Cross-cutting from Folders: idempotency hasher, RFC 9457 + Hexalith problem extensions,
  safe-denial (404), layered authorization chain.

**Deferred Decisions (Post-MVP):**
- Pattern B local `ProjectConversationsView` projection — add only when profiling proves
  Pattern A insufficient (hot-path listing/joins/offline resilience).
- Persisted resolution-trace history beyond confirmation — revisit if the trace workbench
  needs replayable full history.
- Splitting reference-specific aggregates — revisit only if v1 reference cardinality grows large.

### Data Architecture

- **Write store:** Hexalith.EventStore event streams; `{tenant}:projects:{projectId}`.
  Persist-then-publish; pure `Handle`/`Apply`; rejection events implement `IRejectionEvent`.
- **Aggregate:** one `ProjectAggregate` owning lifecycle, setup, the single Project Folder ref,
  and bounded File/Memory reference sets. Conversation membership is NOT stored (derived via
  Pattern A). Idempotency fingerprints/dedupe state held as needed.
- **Events (metadata-only):** `ProjectCreated`, `ProjectSetupUpdated`, `ProjectArchived`,
  `ProjectFolderSet`, `FileReferenceLinked/Unlinked`, `MemoryLinked/Unlinked`,
  `ProjectResolutionConfirmed` (+ rejection events). No transcripts/file contents/memory
  payloads/prompts/secrets/paths/envelope fields.
  *(Conversation link/move events exist only if a project-side invariant ever requires them;
  by default conversation membership lives in Conversations.)*
- **Read store:** tenant-scoped projections, rebuildable from events, exposing
  freshness/watermark/ETag state: `ProjectListProjection`, `ProjectDetailProjection`,
  `ProjectReferenceIndexProjection`, `ProjectAuditTimelineProjection`,
  `ConversationStartSetupProjection`, `TenantAccessProjection` (from Tenants events).
- **`ProjectContext`:** assembled at query time by composing Project state + projection data +
  ACL-fetched, authorization-filtered sibling metadata. Allowlist inclusion; fail-closed.
- **Validation:** boundary validation on contracts (FluentValidation only where `Handle` is
  insufficient); setup validation rejects secrets/unrestricted paths/foreign payloads.
- **Schema evolution:** additive, serialization-tolerant `System.Text.Json`; no `V2` events;
  backward-compatible deserialization for every event ever produced.

### Authentication & Security

- **AuthN:** JWT bearer / OIDC (Keycloak realm `hexalith`); trusted user from `sub`.
- **AuthZ (layered, fail-closed):** API/JWT → EventStore claim-transform → `TenantAccessProjection`
  (exists/active/member/role) → project-level authz (belongs-to-tenant, accessible, active) →
  referenced-resource authorization via owning context → query-side result filtering.
- **Tenant isolation:** canonical identity for streams/state keys/projection keys/topics/
  SignalR groups/logs; cross-tenant access impossible by construction; adversarial negative tests.
- **Safe-denial:** unauthorized vs nonexistent indistinguishable at the boundary (404) to
  prevent cross-tenant existence leakage.
- **Payload safety:** metadata-only everywhere (events, logs, audit, diagnostics, all surfaces);
  enforced by `NoPayloadLeakage`-style negative tests.
- **Dapr:** access-control allowlists for internal endpoints (`/process`, projection dispatch,
  subscriptions); mTLS where deployed; dev access-control policy never shipped to prod.

### API & Communication Patterns

- **Contract:** OpenAPI 3.1 Contract Spine = single source of truth → NSwag-generated typed
  client; idempotency hasher (field-scoped equivalence; `Idempotency-Key` required on
  mutations, rejected on queries); `X-Correlation-Id` + task id threading.
- **Writes:** command-async — submit through EventStore command pipeline; mutations return
  202 `AcceptedCommand`; confirm via read/lifecycle queries (no read-after-write assumption).
- **Reads:** REST query endpoints translating to EventStore `SubmitQuery`/projection queries;
  responses carry freshness/trust state.
- **Errors:** RFC 9457 Problem Details + Hexalith extensions (category/code/correlationId/
  retryable/clientAction); never echo sensitive values.
- **Cross-context:** ACLs call sibling read APIs/clients over HTTP (or in-process where hosted),
  via Dapr service invocation; Tenants events consumed via Dapr pub/sub (idempotent handlers).
- **SignalR:** projection-change nudges only → trigger tenant-scoped re-query; never source-of-truth.

### Frontend Architecture (operational surfaces)

- **Generation:** FrontComposer `[Projection]`/`[Command]` contracts emit Web (Razor+Fluxor) +
  MCP descriptors + CLI evidence from one model — surface parity is a build output.
- **Shared vocabulary:** lifecycle/reference-state enums with `[ProjectionBadge]` (included,
  excluded, unauthorized, unavailable, stale, archived, ambiguous, tenant_mismatch, conflict,
  invalidReference) → identical across Web badges / MCP fields / CLI columns.
- **ProjectionRole mapping:** list (default DataGrid), detail (`DetailRecord`), reference
  inventory (`DetailRecord`), audit (`Timeline`), warnings (`ActionQueue`), dashboard
  (`Dashboard`/`StatusOverview`); resolution trace is the lone Level-3/4 customization candidate.
- **State/render:** Fluxor; Blazor Auto (prerender-safe; `[PersistentState]`); Fluent UI v5 RC
  (pinned). Five-state command lifecycle (Idle→Submitting→Acknowledged→Syncing→Confirmed/Rejected).
- **Accessibility:** WCAG 2.2 AA; status never color-only; verified with axe-core/Playwright.

### Infrastructure & Deployment

- **Topology:** Aspire AppHost (REST + SignalR + `MapMcp`); shared EventStore security resource
  via `AddHexalithEventStoreSecurity`; Dapr sidecars (state, pub/sub, actors, service invocation,
  resiliency, access control); local Dapr components backed by the configured Redis endpoint,
  defaulting to the Dapr-initialized Redis instance.
- **Workers:** `Hexalith.Projects.Workers` host for projection processing + Tenants-event
  subscription; durable projection + dedup store in production (not in-memory).
- **Observability:** OpenTelemetry via ServiceDefaults + Dapr; structured logs with tenant/
  project/correlation/reason metadata only.
- **Resiliency:** Dapr retry/timeout/circuit-breaker at service-invocation boundaries (not in
  domain handlers); dead-letter topics + replay/rebuild runbooks.
- **Repo/build:** module built in-place in this repo (`Hexalith.Projects`), siblings as
  root-level submodule dependencies (no `--recursive`); `Hexalith.Projects.slnx`; central
  package management; shared `Module.Directory.*.props` from `Hexalith.Builds/Samples`.

### Decision Impact Analysis

**Implementation Sequence (derived):**
1. Scaffold module structure + build wiring + root-level submodule deps.
2. Contracts: identifiers, commands, events, rejection events, query DTOs, UI state enums;
   OpenAPI spine.
3. `ProjectAggregate` core (create/update-setup/archive + folder/file/memory refs) with pure
   Tier-1 tests + replay tests.
4. Projections (list/detail/reference-index/audit/conversation-start-setup) with freshness +
   rebuild tests.
5. `TenantAccessProjection` via Tenants client/events (durable store interface).
6. ACLs (Conversations Pattern A incl. `ListConversationsAsync` gap; Folders; Memories; Tenants).
7. `ProjectContext` assembly (allowlist, fail-closed) + resolution compute + confirmation command.
8. Server host (API/`/process`/projection dispatch/auth/ProblemDetails) + Workers.
9. FrontComposer surfaces (Web/MCP/CLI) from shared contracts.
10. Aspire/Dapr topology, access-control, resiliency, dead-letter; operational proof
    (replay, rebuild, leakage, cross-tenant isolation, idempotency).

**Cross-Component Dependencies:**
- Pattern A makes the Conversations ACL + the client `ListConversationsAsync` gap a hard
  prerequisite for any conversation-aware view/resolution.
- `ProjectContext` depends on all four ACLs + `TenantAccessProjection` freshness.
- FrontComposer surface parity depends on the shared state/reason-code enums being defined first.
- Resolution compute depends on the reference index + sibling ACLs but persists nothing until
  `ProjectResolutionConfirmed`.
- OpenAPI spine drives the generated client + idempotency hasher + parity oracle — contract
  changes require fingerprint/compatibility updates.

## Implementation Patterns & Consistency Rules

> Baseline: the umbrella `_bmad-output/project-context.md` (96 rules) and `.editorconfig`
> govern language, file, namespace, and commit conventions. The rules below are the
> **Projects-specific** patterns layered on top — the divergence points agents must not
> decide independently.

### Critical Conflict Points Identified
14 Projects-specific areas where agents could otherwise choose incompatibly: identifiers,
command/event catalog, the shared state/reason-code vocabulary, ACL placement, projection
naming/keys, ProjectContext assembly, idempotency, error/denial format, metadata-only logging,
OpenAPI spine workflow, FrontComposer generation, tenant scoping, async command semantics,
and test tiers.

### Naming Patterns

**Identifiers / value objects:**
- `ProjectId` is a Projects-owned `sealed record` VO (opaque string, eager boundary validation,
  custom `System.Text.Json` converter) — mirrors sibling identifier style.
- For sibling references, **reuse the owning context's Contracts identifier** (e.g.
  `Hexalith.Conversations.Contracts...ConversationId`, Folders `OpaqueIdentifier`-shaped
  `FolderId`/`FileId`, Memories ids) rather than minting parallel Projects VOs — avoids ID drift.
- Opaque-id shape only: never a path, token, credential, or tenant authority.

**Commands (imperative, no `Command` suffix):** `CreateProject`, `UpdateProjectSetup`,
`ArchiveProject`, `SetProjectFolder`, `LinkFileReference`, `UnlinkFileReference`, `LinkMemory`,
`UnlinkMemory`, `ConfirmProjectResolution`.

**Events (past tense, no `Event` suffix):** `ProjectCreated`, `ProjectSetupUpdated`,
`ProjectArchived`, `ProjectFolderSet`, `FileReferenceLinked`, `FileReferenceUnlinked`,
`MemoryLinked`, `MemoryUnlinked`, `ProjectResolutionConfirmed`.

**Rejection events (implement `IRejectionEvent`):** `ProjectCreationRejected`,
`ProjectSetupUpdateRejected`, `ProjectArchiveRejected`, `ProjectReferenceLinkRejected`,
`ProjectReferenceUnlinkRejected`, `ProjectResolutionConfirmationRejected`.
*(One rejection never mixes success + rejection payloads in a `DomainResult`.)*

**Projections:** `Project{Concern}Projection` (`ProjectListProjection`,
`ProjectDetailProjection`, `ProjectReferenceIndexProjection`, `ProjectAuditTimelineProjection`,
`ConversationStartSetupProjection`, `TenantAccessProjection`).

**ACL interfaces:** `IProject{Context}Directory` (`IProjectConversationDirectory`,
`IProjectFolderDirectory`, `IProjectMemoryDirectory`, `ITenantAccess`/`*Authorizer`) in
`Hexalith.Projects.Server`.

**API:** plural REST resources (`/api/v1/projects/{projectId}/...`); `{projectId}` route
params; headers `Idempotency-Key`, `X-Correlation-Id`, `X-Hexalith-Task-Id`,
`X-Hexalith-Freshness`; JSON camelCase on the wire.

### The Shared State & Reason-Code Vocabulary (single source of truth)
- **One** set of enums in `Hexalith.Projects.Contracts` (UI namespace), `[ProjectionBadge]`-
  annotated, used **everywhere**: aggregate rejection reasons, query result states, ACL
  translation, audit, CLI/MCP/Web. Agents MUST NOT invent parallel enums or string literals.
- Lifecycle: `Active`, `Archived`. Reference/inclusion states: `included`, `excluded`,
  `unauthorized`, `unavailable`, `stale`, `archived`, `ambiguous`, `tenant_mismatch`,
  `conflict`, `invalidReference`. Resolution results: `NoMatch`, `SingleCandidate`,
  `MultipleCandidates`. Reason codes: `ConversationLinked`, `ProjectFolderMatched`,
  `FileReferenceMatched`, `MemoryMatched`, `MetadataMatched`.

### Structure Patterns
- Module layout fixed in step 3 (Contracts/Client/core/Server/ServiceDefaults/Aspire/
  AppHost/Workers/Cli/Mcp/Testing + `tests/`).
- Domain core folders mirror Folders: `Aggregates/Project/`, `Projections/`, `Queries/`,
  `Authorization/`, plus `ProjectsModule.cs` + `*ServiceCollectionExtensions.cs`.
- Tests live in the module's `tests/` (never co-located, never ad hoc); reuse
  EventStore/Tenants `Testing` fakes/builders before inventing doubles.
- Generated artifacts (`Generated/*.g.cs`, FrontComposer `*.g.cs`) are never hand-edited;
  change the OpenAPI spine / generator and regenerate.

### Format Patterns
- **Errors:** RFC 9457 Problem Details + Hexalith extensions (`category`, `code`, `message`,
  `correlationId`, `retryable`, `clientAction`, `details.visibility`). Generated client throws
  the typed API exception. **Safe-denial:** unauthorized and nonexistent both surface as 404.
- **Mutations:** 202 `AcceptedCommand` (command-async); confirm via read/lifecycle query — no
  read-after-write assumption. Queries carry freshness/trust state.
- **Idempotency:** stable `Idempotency-Key` per logical attempt (reused on retry); field-scoped
  equivalence via the shared hasher; required on mutations, **rejected** on queries.
- **Dates:** ISO-8601 (`DateTimeOffset`). **Schema evolution:** additive/tolerant; no `V2`.

### Communication Patterns
- **Events:** EventStore owns envelope metadata; domain returns payloads only; persist-then-publish.
- **Cross-context:** only the ACL layer references `Hexalith.{Sibling}.*` types — never
  aggregate/projection/domain logic. ACL = Adapter + Translator (sibling DTO → Projects view
  model) + fail-closed Facade; maps sibling denials to Projects-safe problems.
- **Tenants events:** consumed via Dapr pub/sub into `TenantAccessProjection`; handlers
  **idempotent** (dedupe by message id) and tolerant of out-of-order/at-least-once delivery.
- **SignalR:** nudge-only → re-query with tenant/cache context; never source-of-truth.
- **State (Web):** generated Fluxor features/reducers; explicit subscribe/dispose; no ad-hoc
  component state for operational data.

### Process Patterns
- **Tenant scoping:** tenant authority from authenticated claims + EventStore claim-transform
  **only** — never from payload/header/query. Project data uses the user-facing tenant as the
  EventStore envelope tenant.
- **Fail-closed:** missing/unknown/disabled/stale/rebuilding/unavailable/forbidden/redacted
  evidence denies trust-bearing operations and ProjectContext inclusion.
- **ProjectContext assembly:** allowlist inclusion — a reference is included only after tenant,
  project, lifecycle, authorization, and freshness checks pass; every reference carries an
  inclusion state + reason code; exclusions are surfaced, never silent. (AR-9: the per-evidence-state ×
  operation fail-closed verdicts are tabulated in [`docs/context-assembly-decision-matrix.md`](../../docs/context-assembly-decision-matrix.md);
  Story 3.1 owns the initial cell semantics, Stories 3.2–3.5 extend additively.)
- **Negative-test discipline:** every mutation and query endpoint exercises the canonical rows in
  [`docs/checklists/mutation-and-query-negative-tests.md`](../../docs/checklists/mutation-and-query-negative-tests.md)
  (malformed identifier / route-body identity / idempotency-key / freshness / cross-tenant /
  idempotency-conflict / projection-unavailable). Story 3.2 promoted the per-story pattern to a
  repo-level checklist per Epic 2 retro Action Item 7.
- **Resolution:** never silently attach on `MultipleCandidates`; inference never creates a
  persistent trace, and the scoring/confidence-band heuristic is tabulated in
  [`docs/resolution-scoring-heuristic.md`](../../docs/resolution-scoring-heuristic.md)
  (AR-10: compute-on-demand; persist only `ProjectResolutionConfirmed`). Resolution never
  creates a project without explicit confirmation; archived projects are excluded unless
  explicitly requested.
- **Logging:** structured metadata only — tenant/project/reference ids, reason codes,
  correlation/causation, freshness, status. Never payloads, transcripts, file contents, memory
  payloads, prompts, secrets, raw tokens, command bodies, or unrestricted paths.

### Enforcement Guidelines

**All AI agents MUST:**
- Use the shared state/reason-code enums; never introduce parallel vocabularies or magic strings.
- Keep aggregate `Handle`/`Apply` pure (Tier-1, no Dapr/Aspire/network/browser/containers);
  put cross-context calls in ACLs at the application/projection layer.
- Treat every Project event field as metadata-only and additive; no `V2` events.
- Derive all keys/topics/groups/log scopes from canonical `{tenant}:projects:{projectId}`.
- Route writes only through the EventStore command pipeline; never write read models as authority.
- Edit the OpenAPI spine / generator, never the generated client or `.g.cs`.

**Pattern Enforcement:**
- Tier-1 tests assert pure handlers, replay compatibility, and the state/reason-code vocabulary.
- Mandatory negative-path tests: cross-tenant isolation, `NoPayloadLeakage` (events/logs/DTOs/
  audit), idempotency (duplicate command + duplicate projection delivery), projection rebuild.
- `frontcomposer inspect --fail-on-warning` + OpenAPI fingerprint/compatibility checks in CI.
- New events/projections require an event-catalog entry (purpose, fields, sensitivity class,
  consumers) before merge.

### Pattern Examples

**Good:**
- `ProjectArchived(ProjectId, ArchivedReasonCode, ...)` — past-tense, metadata-only, reason
  code from the shared enum.
- `IProjectConversationDirectory.ListForProjectAsync(...)` translates `ConversationSummaryV1` →
  a Projects view model and maps `ProjectionTrustState` to the shared freshness signal.
- A query result row: `{ referenceId, ownerContext, state: "stale", reasonCode, lastCheckedAt }`.

**Anti-patterns (forbidden):**
- Storing an unbounded `ConversationId[]` in `ProjectAggregate` (Pattern A forbids it).
- Calling `IConversationClient`/Folders client from aggregate or projection code.
- A `ProjectContext` that includes a reference whose authorization/freshness is unverified.
- Inventing a `ProjectStatus` enum that duplicates the shared lifecycle/reference vocabulary.
- Logging a setup body, folder path, or correlation token; emitting a generic 500 for a
  fail-closed denial instead of a safe reason code.
- Hand-editing `HexalithProjectsClient.g.cs` or a FrontComposer `.g.cs`.

## Project Structure & Boundaries

### Complete Project Directory Structure

```text
Hexalith.Projects/                      # THIS repo = the Projects module repo (umbrella-self)
├── Hexalith.Projects.slnx              # module solution (.slnx)
├── global.json                         # pins SDK 10.0.302, rollForward latestPatch
├── Directory.Build.props               # HexalithEventStore/Tenants/... root-detection + guards
├── Directory.Packages.props            # central package versions (no inline versions)
├── nuget.config
├── README.md  CLAUDE.md  AGENTS.md  LICENSE
├── .editorconfig                       # CRLF, 4-space, UTF-8, final newline (inherited)
├── _bmad-output/                       # planning artifacts (this workflow's output)
├── docs/                               # ADRs, event catalog, projection catalog, runbooks
│   ├── adr/                            # ownership, discovery-pattern, identifier-boundary, ...
│   ├── event-catalog.md                # per-event: fields, sensitivity class, consumers
│   ├── projection-catalog.md           # per-projection: key, source events, freshness, rebuild
│   └── parity-matrix.md                # CLI/MCP/Web reason-code + audit-id parity
│   # --- sibling modules as ROOT-LEVEL submodule dependencies (no --recursive) ---
├── Hexalith.EventStore/   Hexalith.Tenants/        Hexalith.Conversations/
├── Hexalith.Folders/      Hexalith.Memories/       Hexalith.FrontComposer/
├── Hexalith.Commons/      Hexalith.AI.Tools/       Hexalith.Builds/
├── src/
│   ├── Hexalith.Projects.Contracts/            # low-dependency; net10.0;netstandard2.0
│   │   ├── ProjectsContractMetadata.cs
│   │   ├── Identifiers/                         # ProjectId (+ JSON converter)
│   │   ├── Commands/                            # CreateProject, UpdateProjectSetup, ...
│   │   ├── Events/                              # ProjectCreated, ProjectFolderSet, ... (+ rejections)
│   │   ├── Queries/                             # ListProjects, GetProject, GetProjectContext,
│   │   │                                        #   ResolveProject, GetConversationStartSetup, ...
│   │   ├── Models/                              # ProjectSetup, ReferenceDescriptor, ProjectContext DTO
│   │   ├── Ui/                                  # SHARED state/reason-code enums ([ProjectionBadge])
│   │   │                                        #   + [Projection]/[Command] FrontComposer contracts
│   │   └── openapi/hexalith.projects.v1.yaml    # OpenAPI 3.1 Contract Spine (source of truth)
│   ├── Hexalith.Projects.Client/               # NSwag-generated typed client + consumer DI
│   │   ├── Generated/HexalithProjectsClient.g.cs   # never hand-edited
│   │   ├── Generation/  Idempotency/  Compat/  nswag.json
│   │   ├── ProjectsClientServiceCollectionExtensions.cs  # AddProjectsClient(...)
│   │   ├── ProjectsClientOptions.cs  ProjectsAuthHandler (bearer DelegatingHandler)
│   │   └── ProjectsClientModule.cs
│   ├── Hexalith.Projects/                       # DOMAIN CORE (pure, Tier-1)
│   │   ├── Aggregates/Project/                  # ProjectAggregate, ProjectState, StateApply,
│   │   │                                        #   command handlers, validators, reason codes,
│   │   │                                        #   idempotency lookup, FolderRef/FileRef/MemoryRef
│   │   ├── Projections/                         # ProjectList/Detail/ReferenceIndex/AuditTimeline/
│   │   │                                        #   ConversationStartSetup projection handlers (pure)
│   │   ├── Resolution/                          # candidate-matching logic (compute-on-demand, pure)
│   │   ├── Context/                             # ProjectContext assembly policy (allowlist, pure)
│   │   ├── Authorization/                       # tenant/project authz decisions + denial reasons
│   │   ├── Queries/                             # query handlers (tenant-scoped, fail-closed)
│   │   ├── ProjectsModule.cs
│   │   └── ProjectsServiceCollectionExtensions.cs
│   ├── Hexalith.Projects.Server/               # host: API, /process, projection dispatch, auth
│   │   ├── Authentication/  Authorization/
│   │   ├── ProjectsDomainServiceEndpoints.cs   # /api/v1 REST (query + mutation + /process)
│   │   ├── ProjectsDomainServiceRequestHandler.cs  ProjectsDomainProcessor.cs
│   │   ├── ProjectAuthorizationDenialMapper.cs  ProjectCommandRejected.cs
│   │   ├── Acl/                                 # IProjectConversationDirectory (+ ListConversations),
│   │   │                                        #   IProjectFolderDirectory, IProjectMemoryDirectory
│   │   ├── TenantAccess/                        # ITenantAccess guard + ProjectsTenantEventHandler
│   │   ├── ProjectsServerModule.cs  ProjectsServerServiceCollectionExtensions.cs  Program.cs
│   ├── Hexalith.Projects.Workers/              # projection workers + Tenants-event subscription
│   │   ├── Tenants/                            # tenant projection subscription handlers
│   │   ├── ProjectsWorkersModule.cs  Program.cs
│   ├── Hexalith.Projects.Mcp/                  # MCP resources (read) + tools (mutate); generated descriptors
│   ├── Hexalith.Projects.Cli/                  # `projects` CLI (describe/inspect/trace/validate/dry-run/...)
│   ├── Hexalith.Projects.UI/                   # FrontComposer Web host (Shell composition, Blazor Auto)
│   ├── Hexalith.Projects.ServiceDefaults/      # OTel, health, service discovery
│   ├── Hexalith.Projects.Aspire/              # Aspire module wiring
│   ├── Hexalith.Projects.AppHost/             # local topology (eventstore, tenants, projects, workers,
│   │                                           #   projects-ui, shared security, Dapr components, Redis)
│   └── Hexalith.Projects.Testing/             # reusable fakes/builders (ProjectBuilder, fake ACLs)
├── tests/
│   ├── Hexalith.Projects.Contracts.Tests/      # serialization/additive-tolerance, naming
│   ├── Hexalith.Projects.Tests/                # Tier-1: aggregate Handle/Apply, projections,
│   │                                           #   resolution, context-assembly, ACL translators
│   ├── Hexalith.Projects.Server.Tests/         # Tier-2: API/query, tenant-event processing,
│   │                                           #   query-side authz, ProblemDetails, freshness
│   ├── Hexalith.Projects.Integration.Tests/    # Tier-3: Dapr/Aspire, /process, pub/sub, restart,
│   │                                           #   dead-letter, projection rebuild
│   └── e2e/                                     # Playwright (Node >=24) + axe-core a11y
└── samples/                                    # sample host / quickstart if needed
```

### Architectural Boundaries

**API boundaries:**
- External (Chatbot/operator): `/api/v1/projects/...` REST from the OpenAPI spine — queries
  (`SubmitQuery`) and command-async mutations (202 `AcceptedCommand`).
- Internal: EventStore `/process` (aggregate actor callback) + projection dispatch — Dapr
  access-control-scoped; not part of the public surface.
- MCP: read-only resources vs mutating tools, generated from the same contracts.
- CLI: scriptable evidence over the same query/command contracts, redaction-safe.

**Component boundaries:**
- `Contracts` is low-dependency (no Dapr/HTTP/EventStore-server); may depend at most on sibling
  `*.Contracts` for reused identifiers.
- Domain core (`Hexalith.Projects`) is pure: aggregate/projection/resolution/context-policy with
  no Dapr/network/ACL calls. ACLs and infrastructure live in `Server`/`Workers`.
- `Server`/`Workers` depend inward on the core; only `Server/Acl/*` references sibling clients.
- `Client`, `Cli`, `Mcp` never reference domain event types or Dapr directly.

**Data boundaries:**
- Write: EventStore streams `{tenant}:projects:{projectId}` (only authority).
- Read: tenant-scoped projections (rebuildable, freshness-bearing). `TenantAccessProjection`
  derived from Tenants events; never authored as truth.
- Sibling payloads (transcripts, file contents, memory bodies) never cross into Projects.

### Requirements to Structure Mapping

**Feature/FR mapping:**
- **Workspace Mgmt (FR-1–5):** `Aggregates/Project/` (`CreateProject`/`UpdateProjectSetup`/
  `ArchiveProject`), `Projections/ProjectList*`,`ProjectDetail*`, `Queries/ListProjects`,`GetProject`.
- **Context References (FR-6–11):** aggregate `ProjectFolderSet`/`FileReference*`/`Memory*`
  events; `Projections/ProjectReferenceIndex*`; `Server/Acl/` (Folders, Memories);
  conversation link/move stays in Conversations + `IProjectConversationDirectory` (Pattern A).
- **Resolution (FR-12–15):** `Resolution/` compute; `ConfirmProjectResolution` command/event;
  reads reference index + ACLs; persists only confirmation.
- **Context Assembly (FR-16–18):** `Context/` assembly policy + `Queries/GetProjectContext`,
  `ExplainContextSelection`, `RefreshProjectContext`.
- **Setup Quality (FR-19–20):** setup validator in `Aggregates/Project/`;
  `Projections/ConversationStartSetup*` + `Queries/GetConversationStartSetup`.
- **Audit & Ops (FR-21–22):** `Projections/ProjectAuditTimeline*`; operator read queries
  (metadata-only, authz-gated).

**Cross-cutting concerns:**
- Tenant access → `Server/TenantAccess/` + `Workers/Tenants/` + `Client.Projections`
  (`TenantAccessProjection`) + `Authorization/`.
- Shared state/reason-code vocabulary → `Contracts/Ui/`.
- OpenAPI spine → `Contracts/openapi/`; generated client → `Client/Generated/` + `Idempotency/`.
- FrontComposer surfaces → annotated contracts in `Contracts/Ui/` → emitted into `UI`/`Mcp`/`Cli`.

### Integration Points

**Internal communication:** EventStore command/query pipeline; in-process query handlers; Dapr
actors for aggregate/projection; SignalR nudges → re-query.

**External integrations:** Conversations (read API / typed client — Pattern A), Folders (typed
client, command-async), Memories (REST/typed client, async ingest, eventual consistency),
Tenants (Dapr pub/sub events + client), Chatbot (consumes Projects client/API).

**Data flow:** command → `ProjectAggregate.Handle` → events persisted → published → projections
updated → SignalR nudge → query re-read; ProjectContext assembled at query time from projections
+ ACL-fetched, authorization-filtered sibling metadata (fail-closed allowlist).

### File Organization Patterns
- **Config:** root `Directory.Build.props`/`Directory.Packages.props`/`global.json`/`nuget.config`;
  shared `Module.Directory.*.props` from `Hexalith.Builds/Samples`.
- **Source:** boundary-strict projects (Contracts/Client/core/Server/Workers/Mcp/Cli/UI/
  ServiceDefaults/Aspire/AppHost/Testing).
- **Tests:** `tests/` mirrors source by tier (Contracts/Tier-1/Server-Tier-2/Integration-Tier-3/e2e).
- **Generated:** `Client/Generated/*.g.cs` + FrontComposer `obj/.../generated/*.g.cs` — read-only.

### Development Workflow Integration
- **Dev:** `aspire start --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj`
  boots eventstore, tenants, projects, workers, projects-ui, shared EventStore security, and Dapr
  sidecars/components backed by the configured local Redis endpoint. Use `aspire wait` and
  `aspire describe --format Json` with the same explicit AppHost path to discover live resource
  endpoints; stop it with targeted `aspire stop --apphost ...`, never a guessed port or blanket stop.
- **Build:** `dotnet build Hexalith.Projects.slnx`; init root-level submodules only.
- **Deploy:** Dapr-sidecar services per host; durable projection/dedup stores in prod;
  access-control deny-by-default + mTLS; CI gates (`frontcomposer inspect --fail-on-warning`,
  OpenAPI fingerprint, filtered `dotnet test`).

## Architecture Validation Results

### Coherence Validation ✅

**Decision Compatibility:** All choices are mutually consistent and align with the umbrella
platform. EventStore (write) + projections (read) + Dapr + Aspire is the proven sibling stack;
versions are centrally pinned and cross-module compatible. Pattern A (reference-don't-own) is
consistent with the single-aggregate/bounded-references decision and with compute-on-demand
resolution (no duplicated conversation state to keep consistent). OpenAPI-spine + FrontComposer
both treat a typed contract as the source of truth — no conflict. No contradictory decisions found.

**Pattern Consistency:** Naming (commands/events/rejections/projections/ACLs), the single shared
state/reason-code vocabulary, metadata-only logging, fail-closed assembly, and idempotency rules
all support the decisions. Cross-surface parity is structurally enforced by shared enums +
FrontComposer generation + OpenAPI fingerprint.

**Structure Alignment:** The project tree (Contracts/Client/core/Server/Workers/Mcp/Cli/UI/
ServiceDefaults/Aspire/AppHost/Testing) supports the boundaries: pure domain core, ACLs isolated
in Server, generated surfaces from contracts. Dependency direction is machine-checkable.

### Requirements Coverage Validation

**Functional Requirements (22/22 mapped):**
- **FR-1–5 (Workspace):** ✅ `ProjectAggregate` + List/Detail projections + queries.
- **FR-8–11 (Folder/File/Memory refs):** ✅ aggregate events + reference index + Folders/Memories ACLs.
- **FR-6, FR-7, FR-12–15 (Conversation assignment + resolution/proposal):** ✅ conversation
  assignment remains owned by Conversations through the reassignment ACL; Projects resolution is
  compute-on-demand, `ConfirmProjectResolution` persists only the confirmed choice, and NoMatch
  proposal confirmation creates the Project then links the initiating conversation/authorized
  attachments through explicit commands.
- **FR-16–18 (Context assembly):** ✅ `Context/` allowlist assembly + get/explain/refresh.
- **FR-19–20 (Setup quality):** ✅ setup validator + conversation-start-setup projection.
- **FR-21–22 (Audit/Ops):** ✅ audit-timeline projection + metadata-only operator queries.

**Non-Functional Requirements:**
- **Security/privacy:** ✅ tenant isolation (canonical identity + query-side filtering),
  metadata-only everywhere, safe-denial.
- **Reliability:** ✅ fail-closed assembly + freshness/trust states.
- **Observability:** ✅ OTel + structured reason-code/correlation metadata.
- **Performance (p95<500ms):** ✅ addressed via precomputed projections — ⚠️ watch: Pattern A
  adds a cross-context read for conversation lists, and resolution compute touches multiple ACLs;
  mitigate with paging/short-TTL caching, and keep Pattern B (local projection) as the documented
  escalation if profiling shows the read is too slow.
- **Compatibility:** ✅ additive/serialization-tolerant; no `V2`.

### Implementation Readiness Validation ✅

- **Decision completeness:** all critical decisions documented; versions pinned/verified.
- **Structure completeness:** complete tree, boundaries, dependency direction, FR→location map.
- **Pattern completeness:** naming, format, communication, process, and enforcement patterns
  defined with examples and anti-patterns; conflict points enumerated.

### Gap Analysis Results

**Critical Gaps (block implementation):** None internal to Projects.

**Important Gaps (cross-module dependencies — track as upstream stories + ADRs):**
1. **Folders `CreateFolder` external endpoint** — FR-1 auto-folder-create depends on a Folders
   operation that is contract-defined but **not yet mapped to external REST** (processes internally
   via `/process`). Either land the Folders server story or, in-topology, invoke via the typed
   client/Dapr; confirm availability before the create-project-with-folder story.

**Nice-to-Have Gaps:**
- Concrete `ProjectSetup` field schema + validation rules (refine in contracts/story design).
- Resolution scoring/confidence-band definition (compute heuristics) — design in the resolution story.
- Memories linkage: confirm whether a Project Memory link maps to a Memories `Case` or individual
  `MemoryUnit` references (Memories model is Tenant→Case→MemoryUnit). **Resolved by Story 2.6
  (PR-4 / AR-G4) — a Project Memory link targets a Memories `Case`; see
  `docs/adr/memories-link-target.md`.**

### Validation Issues Addressed
The conversation link/move dependency was resolved without violating Pattern A: Projects still does
not own conversation membership, and confirmation/proposal flows call the Conversations assignment
ACL before the Projects EventStore command. The remaining external dependency is Folders
CreateFolder exposure for automatic folder creation; the implemented proposal flow links only
caller-supplied, preflight-authorized folder/file references.

### Architecture Completeness Checklist

**Requirements Analysis**
- [x] Project context thoroughly analyzed
- [x] Scale and complexity assessed
- [x] Technical constraints identified
- [x] Cross-cutting concerns mapped

**Architectural Decisions**
- [x] Critical decisions documented with versions
- [x] Technology stack fully specified
- [x] Integration patterns defined
- [x] Performance considerations addressed

**Implementation Patterns**
- [x] Naming conventions established
- [x] Structure patterns defined
- [x] Communication patterns specified
- [x] Process patterns documented

**Project Structure**
- [x] Complete directory structure defined
- [x] Component boundaries established
- [x] Integration points mapped
- [x] Requirements to structure mapping complete

### Architecture Readiness Assessment

**Overall Status:** READY WITH MINOR GAPS — all 16 checklist items satisfied and no Projects-internal
critical gaps; the open items are bounded cross-module dependencies (Conversations assignment/list,
Folders CreateFolder) with clear resolution paths and tracked as upstream stories/ADRs.

**Confidence Level:** High — the design is strongly grounded in six research reports and the proven
sibling-module patterns; the gaps are known, bounded, and external.

**Key Strengths:**
- Clean ownership model (reference-don't-own) + single aggregate keeps invariants tight and Tier-1 testable.
- Payload-safety/tenant-isolation enforced structurally, not by convention.
- One contract → three surfaces eliminates cross-surface drift by construction.
- Compute-on-demand resolution avoids persisting sensitive inference data.

**Areas for Future Enhancement:**
- Pattern B local conversation projection (perf/offline) if profiling requires it.
- Persisted resolution-trace history if the trace workbench needs replayable depth.
- Reference-aggregate split if v1 reference cardinality grows large.

### Implementation Handoff

**AI Agent Guidelines:**
- Follow all architectural decisions and consistency rules exactly as documented.
- Keep aggregate/projection logic pure; isolate cross-context calls in ACLs.
- Treat events/logs/DTOs/audit as metadata-only; derive identity from `{tenant}:projects:{projectId}`.
- Edit the OpenAPI spine/generators, never generated `.g.cs`.

**First Implementation Priority:**
Scaffold the module (mirror Folders' shape; siblings as root-level submodules), then define the
Contracts + shared state/reason-code enums + OpenAPI spine — before any aggregate/projection code.
Resolve Gap 1 (Conversations assignment) via ADR + upstream story before scheduling FR-6/FR-7.
