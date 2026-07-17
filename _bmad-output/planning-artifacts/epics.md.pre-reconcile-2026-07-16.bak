---
stepsCompleted:
  - 1
  - 2
  - 3
  - 4
status: complete
inputDocuments:
  - _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/ux-design-specification.md
  - _bmad-output/planning-artifacts/research/domain-eventstore-persistence-for-hexalith-projects-module-data-research-2026-05-24.md
  - _bmad-output/planning-artifacts/research/technical-hexalith-folders-integration-research-2026-05-24.md
  - _bmad-output/planning-artifacts/research/technical-hexalith-projects-referencing-conversations-research-2026-05-24.md
  - _bmad-output/planning-artifacts/research/technical-hexalith-tenants-in-hexalith-projects-tenant-management-isolation-research-2026-05-24.md
  - _bmad-output/planning-artifacts/research/technical-hexalith-memories-rag-research-2026-05-24.md
  - _bmad-output/planning-artifacts/research/technical-frontcomposer-hexalith-projects-web-ux-research-2026-05-24.md
  - _bmad-output/implementation-artifacts/2-1-conversation-reference-read-acl.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-14-implementation-readiness-correction.md
---

# Hexalith.Projects - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for Hexalith.Projects, decomposing the requirements from the PRD, the UX Design Specification, and the Architecture Decision Document into implementable stories.

Hexalith.Projects is a **greenfield, tenant-aware AI workspace boundary module** built on Hexalith.EventStore + Dapr + Aspire. It gives Hexalith.Chatbot a durable project context (metadata, setup, lifecycle, and references to Conversations/Folders/Files/Memories) and exposes one operational model through three generated surfaces (Web via FrontComposer, MCP, CLI). It is a **metadata control plane** — it references but never owns sibling-context payloads.

## Requirements Inventory

### Functional Requirements

_Source: PRD §4 (FR-1–FR-22). Each FR has testable consequences in the PRD; abbreviated here._

**Project Workspace Management**

- **FR-1: Create Project** — Create a Project with tenant context + Project name (only required user input). Description, initial setup, and initial references optional. If no Project Folder supplied, can request creation of a Project Folder with the same name. Sets lifecycle `Active`. Fails closed when tenant context is missing/unauthorized. No payload duplication. (UJ-2, UJ-3)
- **FR-2: Open Project** — Open a Project and receive metadata, lifecycle state, setup, and authorized references needed to initialize a conversation. Returns only references visible to the requesting tenant/user. Archived/unavailable Projects are clearly identified and cannot silently become active context. (UJ-1)
- **FR-3: Update Project Setup** — Update durable Project Setup (project goals, instructions, context preferences, source inclusion/exclusion, conversation-start defaults). Updates are durable, additive, serialization-tolerant; reject raw secrets, unrestricted paths, foreign-context payloads. v1 setup describes conversation behavior/context policy, not model-provider internals. (UJ-1)
- **FR-4: Archive Project** — Archive a Project so it stays discoverable for history but is excluded from automatic resolution unless explicitly requested. v1 lifecycle limited to `Active`/`Archived`. Existing references remain auditable after archival.
- **FR-5: List Projects** — List Active/Archived Projects visible to the requesting tenant/user. Results are tenant-scoped, authorization-filtered, can filter by lifecycle state, and carry enough metadata to present choices without loading full Project Context.

**Context References**

- **FR-6: Link Conversation** — Link an existing Conversation to a Project (single-project membership in v1). Records stable Conversation identity + metadata, no transcript copy. Linking a Conversation already in another Project requires an explicit move. Fails if Conversation tenant authorization cannot be established. (UJ-1, UJ-3) Resolved through AR-G1 Conversations-owned reassignment ACLs.
- **FR-7: Move Conversation Between Projects** — Move a Conversation between Projects on explicit user confirmation. Removes prior membership before creating the new one; auditable; fails closed when authorization to either Project or the Conversation cannot be established. Resolved through AR-G1 Conversations-owned reassignment ACLs.
- **FR-8: Set Project Folder** — Set the single authorized Project Folder for a Project (exactly one in v1). Records stable Folder identity + metadata; replacement only via explicit update; no file contents/paths stored; folder authorization delegated to Hexalith.Folders. (UJ-2)
- **FR-9: Link File Reference** — Link authorized File References (optional, do not replace the Project Folder). Records stable File identity + metadata; authorization delegated to Hexalith.Folders.
- **FR-10: Link Memory** — Link authorized Memory references. Records stable Memory identity + metadata, no payload copy; authorization delegated to Hexalith.Memories. (UJ-1, UJ-3)
- **FR-11: Unlink Context Reference** — Remove a Conversation/File/Memory reference without deleting the underlying resource. Project Folder is replaceable but not removable unless the Project is archived (v1 Projects require a Folder). Unlinking is auditable.

**Project Resolution**

- **FR-12: Resolve Project From Conversation** — Resolve Candidate Projects for a Conversation with no explicit Project. Returns `NoMatch`/`SingleCandidate`/`MultipleCandidates` with reason code(s); excludes archived unless explicitly requested; never accesses unauthorized resources. (UJ-3)
- **FR-13: Resolve Project From Attachments** — Resolve Candidate Projects from attached Project Folder / File References. Identifies `ProjectFolderMatched`/`FileReferenceMatched`; fails closed when authorization is missing/stale; never treats raw file contents as Project data. (UJ-2)
- **FR-14: Confirm Ambiguous Project** — On `MultipleCandidates`, present candidates and record the user's confirmed choice. Never silently attaches; confirmation creates/updates the Project-to-Conversation association; rejected candidates are not linked.
- **FR-15: Propose New Project** — When no suitable Project is found, propose creating one from the current Conversation, attachments, and setup metadata. Includes a suggested name + initial setup; no Project is created from inference until authorized confirmation; the created Project links the initiating Conversation and authorized attachments through AR-G1 Conversations-owned reassignment ACLs.

**Project Context Assembly**

- **FR-16: Get Project Context** — Return Project Setup plus authorized references (Conversations, Project Folder, File References, Memories) for conversation initialization. Tenant-scoped, authorization-filtered, references+metadata only; indicates which referenced resources were excluded (authorization/lifecycle/availability). (UJ-1, UJ-4)
- **FR-17: Explain Context Selection** — Display/log metadata explaining why each reference was included/excluded. No secrets/file contents/transcripts/prompts/memory payloads in explanation metadata; supports troubleshooting incorrect context selection. (UJ-4)
- **FR-18: Refresh Project Context** — Re-assemble Project Context after links/setup/availability changes. Reflects current links + lifecycle, preserves tenant authorization, surfaces stale/unavailable references rather than silently ignoring them.

**Project Setup Quality**

- **FR-19: Validate Project Setup** — Validate setup before create/update. Reject raw secrets, unrestricted local paths, unsupported reference types, and foreign-context payloads. Require only a Project name on create; default lifecycle `Active`; permit Folder supplied or auto-created with the Project name; allow durable conversation guidance. Validation failures return structured errors identifying the rejected field without echoing sensitive values.
- **FR-20: Retrieve Conversation-Start Setup** — Return the subset of setup needed to start/resume a conversation (goals, instructions, context preferences, default linked-source policy). Excludes internal audit metadata and unavailable/unauthorized references; stable enough to use without re-querying every bounded context first.

**Audit and Operations**

- **FR-21: Record Project Audit Events** — Record metadata-only audit events for creation, setup updates, archival, conversation link/move, Folder changes, File/Memory link/unlink, resolution confirmation, and new-Project-from-proposal. Include tenant, Project identity, operation type, timestamp, actor identity where available, and affected reference IDs. Never include payloads/secrets/prompts.
- **FR-22: Support Operator Read Access** — Authorized operators can inspect Project metadata, lifecycle, references, resolution outcomes, and audit metadata. Authorization-gated, tenant-scoped, metadata-only, no write capabilities beyond archive/troubleshooting workflows explicitly exposed.

### NonFunctional Requirements

_Source: PRD §7 (Cross-Cutting NFRs) + §8 (Success Metrics) + Architecture cross-cutting concerns._

- **NFR-1 — Security & tenant isolation:** Enforce tenant isolation across reads, writes, links, resolution, and context assembly, at every layer (API/auth, aggregate identity, state keys, projection keys, pub/sub topics, queries, SignalR, logs). Cross-tenant access impossible by construction; verified by adversarial negative tests. (Validates SM-3)
- **NFR-2 — Privacy / metadata-only (`NoPayloadLeakage`):** Events, logs, audit, diagnostics, query DTOs, and all surfaces (CLI/MCP/Web) carry metadata only — never transcripts, file contents, memory payloads, raw prompts, secrets, raw tokens, full command bodies, or unrestricted paths. Enforced by negative tests scanning every output.
- **NFR-3 — Reliability / fail-closed:** Project Context retrieval and trust-bearing operations fail closed when authorization, lifecycle, or referenced-resource availability/freshness cannot be verified (missing/unknown/disabled/stale/rebuilding/unavailable/forbidden/redacted evidence denies inclusion).
- **NFR-4 — Observability:** Resolution and context assembly emit structured metadata (reason codes, correlation/causation IDs, freshness, status) sufficient to troubleshoot incorrect matches without exposing payloads; OpenTelemetry via ServiceDefaults + Dapr.
- **NFR-5 — Performance:** p95 < 500 ms for Project list, open, resolution, and context retrieval when dependent bounded-context metadata is available (internal target, not external SLA). Favors precomputed tenant-scoped projections over request-time fan-out. (Validates SM-4)
- **NFR-6 — Compatibility / schema evolution:** Public contracts and event payloads are additive and serialization-tolerant; no `V2` event types; backward-compatible deserialization for every event ever produced.
- **NFR-7 — Idempotency under at-least-once delivery:** Commands and projection/event handlers tolerate duplicate and out-of-order Dapr pub/sub delivery (dedupe by message id; `Idempotency-Key` required on mutations, rejected on queries; field-scoped equivalence hashing).
- **NFR-8 — Cross-surface semantic parity:** CLI, MCP, and Web expose equivalent operational facts — identical lifecycle/reference states, reason codes, timestamps, warnings, and audit identifiers — even when formatting differs.
- **NFR-9 — Resolution correctness over automation (counter-metric SM-C1/SM-C2):** Ambiguous resolution asks for confirmation rather than auto-attaching; context optimizes for relevance/security/prompt quality over volume.

### Additional Requirements

_Source: Architecture Decision Document (decisions, structure, implementation sequence, gap analysis) + research reports. These are the technical/infrastructure requirements that shape epics and stories._

**Foundation & build (Architecture "First Implementation Priority")**

- **AR-1 (Starter / scaffold — Epic 1 Story 1):** No `Hexalith.Projects` module code exists. Scaffold the module from the **Hybrid Hexalith module scaffold** (Folders project-set shape + Tenants domain/tenant-isolation patterns + FrontComposer-generated UI): `Contracts`, `Client`, core `Hexalith.Projects`, `Server`, `Workers`, `Mcp`, `Cli`, `UI`, `ServiceDefaults`, `Aspire`, `AppHost`, `Testing`, plus `tests/` (Contracts/Tier-1/Server-Tier-2/Integration-Tier-3/e2e). The module is built **in-place in this umbrella repo**; siblings are root-level submodule dependencies (no `--recursive`). `Hexalith.Projects.slnx`, `global.json` (SDK 10.0.302), central package management, shared `Module.Directory.*.props` from Hexalith.Builds/Samples.
- **AR-2 (Contracts-before-code):** Define identifiers, commands, events, rejection events, query DTOs, and the shared state/reason-code enums + OpenAPI spine **before** any aggregate/projection code.

**Write model (EventStore)**

- **AR-3:** EventStore is the **sole write authority**. One `ProjectAggregate` per Project with bounded references (one Folder ref; bounded File/Memory ref sets; conversation membership derived, not stored). Pure `Handle`/`Apply`; persist-then-publish; rejection events implement `IRejectionEvent`; one `DomainResult` never mixes success + rejection payloads.
- **AR-4:** Canonical identity `{tenant}:projects:{projectId}`; derive all actor IDs, state keys, projection keys, topics, SignalR groups, and log scopes from it. **Project data uses the user-facing tenant as the EventStore envelope tenant** (vs Tenants' `system`). EventStore owns envelope metadata; domain returns payloads only.
- **AR-5 (Commands):** `CreateProject`, `UpdateProjectSetup`, `ArchiveProject`, `SetProjectFolder`, `LinkFileReference`, `UnlinkFileReference`, `LinkMemory`, `UnlinkMemory`, `ConfirmProjectResolution` (+ conversation link/move commands only if a project-side invariant requires them — default: membership lives in Conversations).
- **AR-6 (Events):** `ProjectCreated`, `ProjectSetupUpdated`, `ProjectArchived`, `ProjectFolderSet`, `FileReferenceLinked`, `FileReferenceUnlinked`, `MemoryLinked`, `MemoryUnlinked`, `ProjectResolutionConfirmed` (+ rejection events: `ProjectCreationRejected`, `ProjectSetupUpdateRejected`, `ProjectArchiveRejected`, `ProjectReferenceLinkRejected`, `ProjectReferenceUnlinkRejected`, `ProjectResolutionConfirmationRejected`). Metadata-only; each new event requires an **event-catalog entry** (purpose, fields, sensitivity class, consumers) before merge.
- **AR-7 (Identifiers):** `ProjectId` is a Projects-owned `sealed record` VO (opaque, eager validation, custom JSON converter). For sibling references **reuse the owning context's Contracts identifier** (`ConversationId`, Folders `FolderId`/`FileId`, Memories ids) to avoid ID drift.

**Read model (projections)**

- **AR-8 (Projections):** Tenant-scoped, rebuildable, freshness/watermark/ETag-bearing: `ProjectListProjection`, `ProjectDetailProjection`, `ProjectReferenceIndexProjection`, `ProjectAuditTimelineProjection`, `ConversationStartSetupProjection`, and a local `TenantAccessProjection`. (Optional `ProjectResolutionTraceProjection` for the trace workbench; persisted-trace history is deferred.) Each projection defines owner, source events, key, tenant scoping, rebuild behavior, freshness semantics, and query authorization. Use `CachingProjectionActor`/ETag actors/notifiers before inventing custom routing.
- **AR-9 (ProjectContext assembly):** `ProjectContext` is an **assembled read result, not a persisted aggregate** — composed at query time from Project state + projections + ACL-fetched, authorization-filtered sibling metadata, with **allowlist inclusion** (a reference is included only after tenant, project, lifecycle, authorization, and freshness checks pass; exclusions surfaced with state + reason code).
- **AR-10 (Resolution):** Compute-on-demand; persist only the confirmed choice (`ProjectResolutionConfirmed`). Define resolution scoring/confidence-band heuristics in the resolution story.

**Cross-context integration (ACLs — "reference, don't own")**

- **AR-11 (ACLs):** Four Projects-owned Anti-Corruption Layers in `Projects.Server` (Adapter + Translator + fail-closed Facade): `IProjectConversationDirectory`, `IProjectFolderDirectory`, `IProjectMemoryDirectory`, `ITenantAccess`. Only the ACL layer references `Hexalith.{Sibling}.*` types — never aggregate/projection logic.
- **AR-12 (Conversations — Pattern A):** Discover conversations by querying Conversations by `ProjectId` behind the ACL; no local conversation storage; surface `ProjectionTrustState` freshness. *(Pattern B local projection deferred to profiling.)*
- **AR-13 (Tenants):** Consume Tenants events via Dapr pub/sub into `TenantAccessProjection`; handlers idempotent (dedupe by message id), tolerant of out-of-order/at-least-once delivery; **durable projection + dedup store in production** (not in-memory); fail closed until projection is usable after restart.
- **AR-14 (Folders/Memories clients):** Folders & Memories accessed command-async via typed clients (202 → poll lifecycle/freshness; no read-after-write). Memories model is Tenant → Case → MemoryUnit. Map sibling denials to Projects-safe problems; never rethrow raw upstream detail.

**API & surfaces**

- **AR-15 (OpenAPI Contract Spine):** OpenAPI 3.1 spine (`Contracts/openapi/hexalith.projects.v1.yaml`) is the single source of truth → NSwag-generated typed client (`Client/Generated/*.g.cs`, never hand-edited). Headers: `Idempotency-Key`, `X-Correlation-Id`, `X-Hexalith-Task-Id`, `X-Hexalith-Freshness`; JSON camelCase; ISO-8601 `DateTimeOffset`.
- **AR-16 (Command-async + errors):** Mutations return `202 AcceptedCommand`; reads carry freshness/trust state; no read-after-write assumption. Errors use RFC 9457 Problem Details + Hexalith extensions (`category`, `code`, `correlationId`, `retryable`, `clientAction`, `details.visibility`). **Safe-denial:** unauthorized vs nonexistent both surface as 404 to prevent cross-tenant existence leakage.
- **AR-17 (FrontComposer generation):** Web (Razor+Fluxor) + MCP descriptors + CLI evidence are generated from annotated `[Projection]`/`[Command]` contracts — surface parity is a build output. `frontcomposer inspect --fail-on-warning` + OpenAPI fingerprint/compatibility checks are CI gates. Never hand-edit `.g.cs`.
- **AR-18 (Shared vocabulary — single source of truth):** One set of `[ProjectionBadge]`-annotated enums in `Contracts/Ui` used everywhere (aggregate rejection reasons, query result states, ACL translation, audit, CLI/MCP/Web). No parallel enums or magic strings. Lifecycle `Active`/`Archived`; reference/inclusion states `included`/`excluded`/`unauthorized`/`unavailable`/`stale`/`archived`/`ambiguous`/`tenant_mismatch`/`conflict`/`invalidReference`; resolution results `NoMatch`/`SingleCandidate`/`MultipleCandidates`; reason codes `ConversationLinked`/`ProjectFolderMatched`/`FileReferenceMatched`/`MemoryMatched`/`MetadataMatched`.

**Authorization & infrastructure**

- **AR-19 (Layered authorization, fail-closed):** API/JWT (OIDC, Keycloak realm `hexalith`) → EventStore claim-transform → `TenantAccessProjection` (exists/active/member/role) → project-level authz (belongs-to-tenant, accessible, active) → referenced-resource authz via owning context → query-side result filtering. Object-level authorization on every endpoint accepting an identifier (OWASP API #1).
- **AR-20 (Dapr-only infrastructure):** Dapr is the only infrastructure abstraction (state/pub-sub/actors/service-invocation). No direct Redis/Postgres/Cosmos/broker in contracts/client/domain. Dapr access-control allowlists for internal endpoints (`/process`, projection dispatch, subscriptions); mTLS where deployed; dev access-control policy never shipped to prod; resiliency (retry/timeout/circuit-breaker) at service-invocation boundaries, not in domain handlers.
- **AR-21 (Workers + operations):** `Hexalith.Projects.Workers` host for projection processing + Tenants-event subscription; dead-letter topics + replay/rebuild runbooks; durable projection/dedup stores in production.
- **AR-22 (Aspire topology):** Aspire AppHost owns local topology (eventstore, tenants, projects, workers, projects-ui, Keycloak, Dapr sidecars/components backed by the configured local Redis endpoint); REST + SignalR (`nudge-only` → re-query) + `MapMcp`.
- **AR-23 (Testing tiers):** xUnit v3 + Shouldly + NSubstitute; reuse EventStore/Tenants Testing fakes/builders; `Verify.XunitV3` for FrontComposer snapshots; bUnit for components; Playwright (Node ≥24) + axe-core for E2E. Tier-1 (pure: aggregate Handle/Apply, projections, resolution, context-assembly, ACL translators, generator parse/transform/emit) has no Dapr/Aspire/network/browser/containers. Mandatory negative-path tests: cross-tenant isolation, `NoPayloadLeakage`, idempotency (duplicate command + duplicate projection delivery), projection rebuild/replay.

**Cross-module upstream dependency gaps (track as ADRs + upstream stories)**

- **AR-G1 (resolved by Epic 2/4 implementation):** Conversation assignment/move remains owned by **`Hexalith.Conversations`**. Projects calls the Conversations reassignment API through ACLs and does not store local conversation membership. Story 4.4/4.5 confirmation paths run the assignment boundary before the Projects EventStore command and use idempotent recovery for already-target / expected-source / unassigned cases.
- **AR-G2 (prerequisite for conversation-aware view/resolution — this is the existing Story 2.1):** `IConversationClient` lacks list/search; add `ListConversationsAsync` (forwarding to the server handler) **or** call `ConversationReadApi` over HTTP. The server-side `ConversationListFilterV1.ProjectId` + `ConversationQueryHandler.ListAsync` discovery path already exists and is tenant-scoped/fail-closed.
- **AR-G3 (FR-1 auto-folder-create):** Folders' `CreateFolder` is contract-defined but **not yet mapped to external REST** (processes via `/process`). Either land the Folders server story or invoke in-topology via the typed client/Dapr; confirm availability before the create-project-with-folder story. (Note: `AddFoldersClient` DI helper recently drafted in the Folders submodule, pending commit.)
- **AR-G4 (nice-to-have):** Confirm whether a Project Memory link maps to a Memories `Case` or individual `MemoryUnit` references (Memories model is Tenant → Case → MemoryUnit). Resolve in the Memory-link story. Note Memories core write methods are `[Experimental("HXL001")]` and ingestion is async/eventually-consistent.

### UX Design Requirements

_Source: UX Design Specification + FrontComposer web-UX research. The UX scope is **administrative/operational** (CLI + MCP + FrontComposer Web over one diagnostic model) — not an end-user project-management product. Chosen direction: **Metadata Control Plane** (base) + **Resolution Trace Workbench** + **Audit-First Maintenance**._

**Design foundation**

- **UX-DR1 — Inherited design system:** Web UX composed through Hexalith.FrontComposer on the established Fluent UI Blazor v5 RC (pinned) component approach; no bespoke Projects UI framework or custom visual language. Customization limited to labels, field grouping, columns, filters, badges, warning/action panels.
- **UX-DR2 — Semantic color system:** Inherit Hexalith/FrontComposer/Fluent UI palette; semantic operational colors (neutral surfaces, primary/action, success=healthy, warning=stale/ambiguous/archived/attention, error=denied/invalid/conflict/failed/unavailable, info=explanatory, muted=excluded/unavailable). **Status is never color-only** — always paired with text label, icon, accessible name, or reason code.
- **UX-DR3 — Typography & density:** FrontComposer/Fluent UI type scale; professional/calm/precise; moderate-to-high density; **monospace** for identifiers, reason codes, tenant IDs, correlation IDs, audit event IDs, and command examples; no hero/marketing display type.
- **UX-DR4 — Dense operational layout:** Resource list/detail layouts, filter bars + command bars near the data they affect, inspector panels, timeline layouts, warning panels near the affected resource, action panels separated from read-only diagnostic sections. No decorative cards or landing-page sections.

**Shared operational vocabulary (also AR-18 — UX-binding requirement)**

- **UX-DR5 — Status & reason-code pattern:** Every state has a stable code + display label + accessible name + severity mapping, declared once as `[ProjectionBadge]`-annotated shared enums and rendered identically as Web badges, MCP resource fields, and CLI columns. Covers lifecycle (`active`, `archived`), reference/inclusion states (`included`, `excluded`, `unauthorized`, `unavailable`, `stale`, `archived`, `ambiguous`, `tenant_mismatch`, `conflict`, `invalidReference`), resolution results, and reason codes.

**FrontComposer Web views (one `[Projection]`/`[Command]` contract → Web + MCP + CLI)**

- **UX-DR6 — Project inventory/list view:** Default FrontComposer DataGrid; tenant scope, lifecycle state, warnings, updated timestamp, filters (tenant, lifecycle, reason code, warning, reference type, timestamp). (FrontComposer Level 1)
- **UX-DR7 — Project detail inspector view:** `DetailRecord`; metadata, lifecycle state, setup metadata, safe identifiers, field groups, tabs/sections for metadata/references/resolution/audit/actions. (Level 1–2)
- **UX-DR8 — Reference inventory & health view:** `DetailRecord` sub-grid; linked Conversations, Project Folder, File References, Memories with inclusion + health states + reason code + last-checked timestamp + safe actions. (Level 1)
- **UX-DR9 — Resolution trace view:** Evaluated inputs, candidate projects, reason-code badges, inclusion/exclusion evidence, final outcome (`Resolved`/`NoMatch`/`MultipleCandidates`/`Excluded`/`FailedClosed`), side-by-side candidate comparison. The one bespoke-layout view — **FrontComposer Level 3 slot / Level 4 view** candidate (attempt Level 2 template first).
- **UX-DR10 — Audit timeline view:** `Timeline`; timestamp, actor/source surface, operation, previous→new state, affected reference IDs, correlation ID, audit event ID; understandable as a list for screen readers. (Level 1)
- **UX-DR11 — Warnings / maintenance queue view:** `ActionQueue` with `WhenState` filter (e.g. `Stale,Conflict,InvalidReference`); pending items needing intervention. (Level 1)
- **UX-DR12 — Operational dashboard / status overview:** `Dashboard`/`StatusOverview`; aggregated cross-project health/status tiles. (Level 1–2)

**Custom components (all 6 from the UX spec — Projects-specific compositions of Fluent UI primitives)**

- **UX-DR13 — Project Diagnostic Header:** Tenant scope, project identity, lifecycle badge, warning count, last-updated timestamp, mode indicator (`read-only`/`dry-run`/`maintenance`); copyable IDs; badges have text labels + accessible names.
- **UX-DR14 — Reference Health Matrix:** Reference type, reference ID, bounded-context owner, inclusion state, health state, reason code, last-checked timestamp, available safe actions; explicit grid headers; status not color-only.
- **UX-DR15 — Resolution Trace:** Input summary, candidate list, reason-code badges, inclusion/exclusion evidence, outcome panel, safe next actions; trace order + candidate comparisons screen-reader-readable.
- **UX-DR16 — Audit Timeline:** Timestamp, actor/source surface, operation, previous/new state, affected references, correlation ID, audit event ID; copyable timestamps/IDs.
- **UX-DR17 — Maintenance Action Panel:** Action name, tenant scope, target identifiers, current state, proposed state, warnings, dry-run result, expected audit event, confirmation control; states `Preview`/`DryRunRequired`/`DryRunPassed`/`DryRunBlocked`/`ConfirmationRequired`/`Executing`/`Succeeded`/`Failed`; clear labels + focus handling for risky actions.
- **UX-DR18 — Safe Diagnostic Export:** Safe JSON/structured metadata preview, included fields, explicit payload-exclusion guarantee, copy/export action; keyboard-copyable + screen-reader accessible; available via Web copy/download, CLI structured output, MCP resource.

**CLI surface**

- **UX-DR19 — CLI command structure:** Scriptable, stable command grouping mirroring the model — read-only `list`/`describe`/`inspect`/`trace-resolution`(`trace`)/`validate-references`(`validate`)/`audit`; preview `dry-run`/`preview`; mutating `archive`/`restore`/`relink`/`unlink`/`reevaluate` (explicit target + confirmation semantics). Machine-readable JSON output, stable exit codes, redaction-safe, no reliance on color for meaning.

**MCP surface**

- **UX-DR20 — MCP resources vs tools:** Read-only **resources** (project metadata, references, resolution traces, audit events) clearly separated from mutating **tools** (archive/restore/relink/unlink/reevaluate). Structured safe metadata fields (`projectId`, `tenantId`, state, references, reasonCodes, warnings, audit IDs) **plus** a short safe explanation — never explanation-only. Tenant-aware; mutating tools require explicit action + target IDs + tenant scope + confirmation contract + validation/dry-run before execution; reject unknown tools with suggestions.

**Interaction & feedback patterns**

- **UX-DR21 — Five-state command lifecycle:** `Idle → Submitting → Acknowledged (202) → Syncing (nudge) → Confirmed/Rejected`, surfaced in Web (and shared with MCP). Rejections are domain outcomes carrying `ProblemDetailsPayload`, not exception paths. Components tolerate the nudge→re-query eventual-consistency window.
- **UX-DR22 — Feedback patterns:** Distinct success / warning / error / fail-closed / loading feedback. Success includes operation result + tenant scope + identifiers + timestamp + audit event ID. Errors use safe reason codes (`tenant_mismatch`, `unauthorized`, `unavailable`, `conflict`, `invalidReference`, `validationFailed`); never echo secrets/prompts/transcripts/file contents/memory payloads. Loading distinguishes metadata retrieval, reference validation, trace loading, dry-run, and maintenance execution.
- **UX-DR23 — Empty-state pattern:** Distinguish true absence from denied/unavailable (no projects found / no references linked / no audit events / data unavailable / access denied / filter returned no results); never collapse to a blank table.
- **UX-DR24 — Form patterns:** Diagnostic forms accept safe identifiers (tenant/project/conversation/folder/file/memory/resolution-case/audit-event/correlation IDs, lifecycle state, reason code, timestamp range). Maintenance forms show current state, proposed state, affected identifiers, tenant scope, warnings, dry-run result, and expected audit event before execution; field-specific safe validation before state change. CLI args and MCP tool schemas use the same field names/validation as Web forms.
- **UX-DR25 — Confirmation & audit-evidence pattern:** Mutating actions require confirmation showing tenant scope, target IDs, current→proposed state, warnings, expected audit event, and whether a dry-run passed; every state-changing action ends with metadata-only audit evidence (action, actor/source, tenant, project ID, affected reference IDs, timestamp, correlation ID, result, audit event ID).

**Responsive & accessibility**

- **UX-DR26 — Responsive strategy:** Desktop-optimized and responsive. Desktop primary (inventory+detail, multi-column reference health/trace, side-by-side candidate comparison). Tablet collapses navigation/columns, stacks inspectors, favors read-only + light maintenance. Mobile supports urgent inspection only (project identity, tenant scope, lifecycle, warnings, top reason codes; state-changing actions only when confirmation content stays fully visible). Breakpoints 320–767 / 768–1023 / 1024+ / 1440+. Critical metadata, warnings, reason codes, and action consequences remain visible at every viewport; long identifiers never truncated without accessible full-value access.
- **UX-DR27 — Accessibility (WCAG 2.2 AA):** Keyboard access for grids/filters/command bars/tabs/dialogs/action panels; visible focus; semantic headings/landmarks; status text labels + accessible names (not color-only); sufficient contrast (incl. badges/warning/error/disabled/focus); screen-reader-readable tables/timelines; modal focus trapping/restoration; reduced-motion-safe; no hover-only critical actions; safe error messages. Verified with axe-core/Playwright. CLI: no color reliance, structured JSON mode, stable exit codes, safe reason codes. MCP: structured fields + short explanations, stable schemas for agent reasoning.
- **UX-DR28 — Stable test identifiers:** Preserve deterministic component keys / `data-testid` (role/label-based selectors) for automation across generated FrontComposer views.

### FR Coverage Map

- **FR-1 Create Project:** Epic 1 — `CreateProject` on `ProjectAggregate`; optional auto-folder.
- **FR-2 Open Project:** Epic 1 — `GetProject` query over `ProjectDetailProjection`.
- **FR-3 Update Project Setup:** Epic 1 — `UpdateProjectSetup`; durable, additive setup.
- **FR-4 Archive Project:** Epic 1 — `ArchiveProject`; lifecycle `Active`/`Archived`.
- **FR-5 List Projects:** Epic 1 — `ListProjects` over `ProjectListProjection`, filterable by lifecycle.
- **FR-6 Link Conversation:** Epic 2 — Conversation read ACL (Story 2.1) + write-side link through AR-G1 Conversations-owned reassignment.
- **FR-7 Move Conversation:** Epic 2 — explicit move through AR-G1 Conversations-owned reassignment; auditable and fail-closed.
- **FR-8 Set Project Folder:** Epic 2 — `SetProjectFolder` (single Folder ref) + Folders ACL.
- **FR-9 Link File Reference:** Epic 2 — `LinkFileReference` + Folders ACL.
- **FR-10 Link Memory:** Epic 2 — `LinkMemory` + Memories ACL.
- **FR-11 Unlink Context Reference:** Epic 2 — `UnlinkFileReference`/`UnlinkMemory`/conversation unlink; Folder replace-only.
- **FR-12 Resolve From Conversation:** Epic 4 — compute-on-demand resolution from conversation metadata.
- **FR-13 Resolve From Attachments:** Epic 4 — resolution from Folder/File references via reference index + ACLs.
- **FR-14 Confirm Ambiguous Project:** Epic 4 — `ConfirmProjectResolution` on `MultipleCandidates`.
- **FR-15 Propose New Project:** Epic 4 — proposal flow; create-on-confirm; links initiating conversation (AR-G1).
- **FR-16 Get Project Context:** Epic 3 — `GetProjectContext` allowlist assembly (consumes Epic 2 ACLs).
- **FR-17 Explain Context Selection:** Epic 3 — `ExplainContextSelection` inclusion/exclusion metadata.
- **FR-18 Refresh Project Context:** Epic 3 — `RefreshProjectContext`; surfaces stale/unavailable refs.
- **FR-19 Validate Project Setup:** Epic 1 — setup validator in `ProjectAggregate` (rejects secrets/paths/foreign payloads).
- **FR-20 Retrieve Conversation-Start Setup:** Epic 3 — `GetConversationStartSetup` over `ConversationStartSetupProjection`.
- **FR-21 Record Project Audit Events:** Epic 5 — `ProjectAuditTimelineProjection`; metadata-only audit.
- **FR-22 Support Operator Read Access:** Epic 5 — authorization-gated, tenant-scoped operator read queries.

_All 22 FRs mapped. NFR-1–9 are cross-cutting and verified per epic (foundation-heavy in Epic 1). Additional Requirements (AR-*) and UX Design Requirements (UX-DR*) are distributed as noted per epic below; the upstream gap items AR-G1–G4 are tracked as ADRs + prerequisite stories within their consuming epics._

## Epic List

### Epic 1: Project Workspace Foundation

Stand up a deployable, tenant-isolated, authenticated `Hexalith.Projects` service in which Chatbot can create, open, list, update, and archive projects with validated setup. This epic establishes everything later epics build on — the module scaffold (Folders project-set shape + Tenants tenant-isolation patterns + FrontComposer-generated UI wiring), the shared `[ProjectionBadge]` state/reason-code vocabulary, the OpenAPI 3.1 Contract Spine + generated typed client, the single `ProjectAggregate` write model with pure `Handle`/`Apply` and rejection events, the list/detail projections, the `TenantAccessProjection` with layered fail-closed authorization, and the Aspire/Dapr/Workers operational skeleton with CI gates (`frontcomposer inspect --fail-on-warning`, OpenAPI fingerprint, filtered `dotnet test`). After this epic a project is a real, tenant-scoped, durable workspace record.

**FRs covered:** FR-1, FR-2, FR-3, FR-4, FR-5, FR-19
**Key ARs:** AR-1–AR-7, AR-8 (list/detail), AR-13, AR-15, AR-16, AR-18, AR-19, AR-20, AR-21, AR-22, AR-23 · **UX:** UX-DR5 (shared vocabulary) · **NFRs:** NFR-1, NFR-2, NFR-3, NFR-5, NFR-6, NFR-7
**Standalone:** Yes — delivers the walking skeleton + complete workspace CRUD. Requires no later epic to function.
**Notes:** Story 1.1 is the scaffold/starter slice (greenfield module from sibling shape; root-level submodules only). Contracts + shared enums + OpenAPI spine precede any aggregate/projection code. Conversation membership is **not** stored in the aggregate (Pattern A — deferred to Epic 2).

### Epic 2: Context References

Make a project a true workspace boundary by connecting it to its conversations, its single Project Folder, optional file references, and memories — every reference held by-ID, tenant-scoped, and fail-closed behind the four Projects-owned Anti-Corruption Layers (Conversations, Folders, Memories, Tenants). Delivers the `ProjectReferenceIndexProjection` and the folder/file/memory aggregate write events, plus safe discovery/listing of a project's conversations.

**FRs covered:** FR-6, FR-7, FR-8, FR-9, FR-10, FR-11
**Key ARs:** AR-9 (reference index), AR-11, AR-12, AR-14 · **Dependencies/Gaps:** AR-G1 (resolved by Conversations-owned reassignment ACLs), AR-G2 (Story 2.1 `ListConversationsAsync`), AR-G3 (Folders `CreateFolder` REST for FR-1 auto-folder), AR-G4 (Memories Case-vs-Unit) · **NFRs:** NFR-1, NFR-2, NFR-3
**Standalone:** Yes — builds on Epic 1; does not require Epics 3–5.
**Notes:** **Story 2.1 = the existing `2-1-conversation-reference-read-acl.md`** (Conversation Reference Read ACL, Pattern A, read-only discovery — reconcile, don't duplicate). FR-6/FR-7 write-side (durable link + move) consumes the AR-G1 Conversations reassignment capability through ACLs. Folder/File/Memory links are `ProjectAggregate` writes; conversation membership stays Conversations-owned.

### Epic 3: Project Context Assembly

Give Chatbot the assembled context it needs to start or resume a conversation: a tenant-scoped, authorization-filtered `ProjectContext` (setup + included references, with explicit exclusion reasons), an explanation of why each reference was included or excluded, a refresh path that surfaces stale/unavailable references rather than hiding them, and the conversation-start setup subset. Assembly is allowlist-based and fails closed; everything is metadata-only.

**FRs covered:** FR-16, FR-17, FR-18, FR-20
**Key ARs:** AR-9 (ProjectContext assembly policy), AR-8 (`ConversationStartSetupProjection`) · **NFRs:** NFR-2, NFR-3, NFR-5, NFR-9
**Standalone:** Yes — consumes Epic 2's ACLs/reference index; independent of Epic 4 and Epic 5.
**Notes:** `ProjectContext` is an assembled read result, never a persisted aggregate. Inclusion requires tenant + project + lifecycle + authorization + freshness checks to all pass; exclusions carry a state + reason code from the shared vocabulary.

### Epic 4: Project Resolution

When a conversation arrives without an explicit project, help Chatbot find the right one: resolve candidate projects from conversation metadata (FR-12) and from attached Folder/File references (FR-13), returning `NoMatch`/`SingleCandidate`/`MultipleCandidates` with reason codes; let the user confirm an ambiguous match (FR-14); and propose a new project when none fits (FR-15). Resolution is compute-on-demand and never silently attaches; inference never creates a project without explicit confirmation, and archived projects are excluded unless explicitly requested.

**FRs covered:** FR-12, FR-13, FR-14, FR-15
**Key ARs:** AR-10 (compute-on-demand + `ProjectResolutionConfirmed`), AR-9 (reads reference index), AR-G1 (resolved Conversations-owned assignment for FR-15 "link initiating conversation") · **NFRs:** NFR-2, NFR-3, NFR-9
**Standalone:** Yes — builds on Epics 1–2; independent of Epic 3 and Epic 5.
**Notes:** Only the confirmed choice is persisted (`ProjectResolutionConfirmed`); resolution traces are computed, not stored (persisted-trace history is deferred). Define scoring/confidence-band heuristics in the resolution stories.

### Epic 5: Operational Console & Audit (CLI / MCP / Web)

Deliver the administrative/operational product the UX spec defines: a FrontComposer-generated **Metadata Control Plane** console plus parity MCP and CLI surfaces over one diagnostic model. Administrators, operators, and MCP-assisted agents can inspect projects, reference health, resolution traces (the **Resolution Trace Workbench**), and metadata-only audit history (FR-21), with authorization-gated operator read access (FR-22), and can perform safe, **audit-first** maintenance actions (archive/restore/relink/unlink/reevaluate) with dry-run/preview, confirmation, and metadata-only audit evidence. This epic owns the three generated surfaces, the seven views, the six custom components, cross-surface parity, and WCAG 2.2 AA accessibility hardening.

**FRs covered:** FR-21, FR-22
**Key ARs:** AR-17 (FrontComposer generation), AR-8 (`ProjectAuditTimelineProjection`) · **UX:** UX-DR1–UX-DR4, UX-DR6–UX-DR28 (7 views, 6 custom components, CLI, MCP, interaction/feedback/confirmation patterns, responsive, accessibility, test IDs) · **NFRs:** NFR-2, NFR-4, NFR-8
**Standalone:** Yes — builds on all prior epics (surfaces the projections/commands they define) and requires none of them to be re-opened beyond additive `[Projection]`/`[Command]` annotations.
**Notes (surface-delivery decision):** Epics 1–4 stay backend/contract-focused; Level-1 generated views come "for free" from their contracts but are not the deliverable. Epic 5 delivers the **composed** console (Shell/nav), the bespoke resolution-trace view (FrontComposer Level 3/4), the maintenance action flows with the five-state command lifecycle, the audit timeline, safe diagnostic export, and the cross-surface parity + a11y/E2E (axe-core/Playwright) hardening. Maintenance commands `restore`/`relink`/`unlink`/`reevaluate` are introduced here (`archive` already exists from Epic 1).

## Cross-Cutting Foundational Slices (Epic 1)

_Captured from the Step-2 multi-agent review (Winston/John/Murat/Amelia). These become explicit Epic 1 stories so the load-bearing invariants are defined **once** and CI-enforced, not reinvented per epic. They do not change the 5-epic grouping._

- **FS-1 — Payload-classification taxonomy & allowlist:** Enumerate reference-only fields (IDs, ETags, tenant, kind, timestamps, reason codes) vs forbidden sibling-owned content (folder names, file contents, memory bodies, conversation text, prompts, secrets, tokens, paths). This is the security boundary for ProjectContext assembly (Epic 3) and the basis for every `NoPayloadLeakage` test. **First content story of Epic 1.** (Satisfies NFR-2; AR-9; AR-18.)
- **FS-2 — `NoPayloadLeakage` CI harness:** Reusable serialization-boundary / Verify-based guard + CI gate that forces every new event/DTO/log/audit record/surface through a leakage assertion. Built in Epic 1, **extended every epic** (notably Epic 5 rendering surfaces).
- **FS-3 — Canonical identity-derivation helper:** `{tenant}:projects:{projectId}` → actor IDs, state keys, projection keys, topics, SignalR groups, log scopes, with Tier-1 conformance tests, **before** any projection consumes keys. (AR-4; NFR-1.)
- **FS-4 — Shared rejection-event & reason-code vocabulary:** Single `[ProjectionBadge]` enum set + `IRejectionEvent` taxonomy defined once (AR-18); rejection paths tested as **ACs on each command story**, not a trailing "add rejections" story.
- **FS-5 — Schema-evolution regression corpus:** Frozen serialized-event golden files round-tripped in CI to enforce additive / serialization-tolerant / no-`V2` deserialization for every event ever produced. (NFR-6; AR-6.)
- **FS-6 — Projection rebuild/replay + idempotency:** Deterministic rebuild (same events → same state) proven on the trivial Epic-1 event set and extended per epic; duplicate-**command** dedup and duplicate-**projection-delivery** idempotent `Apply` are **separate stories** (they fail independently). (NFR-7; AR-23.)
- **FS-7 — FrontComposer regeneration/staleness CI gate:** Fail CI when generated `.g.cs` drifts from `[Projection]`/`[Command]` contracts — added in Epic 1 even though the generators land in Epic 5, so Epic 5 doesn't inherit silent drift. (AR-17.)
- **FS-8 — Cross-tenant isolation conformance (UJ-4):** Explicit, demoable "Project A context never leaks into Project B / tenant B" negative-test suite — the named success metric SM-3 made into an owned acceptance test. Established in Epic 1, **re-run against each new surface** (Epics 3, 5). (NFR-1.)

> **Epic 1 decomposition (Step 3 guidance):** to avoid one un-shippable slab, Epic 1 will slice into ~3 ordered story groups — **(1a)** tracer-bullet vertical (scaffold + Contracts/OpenAPI spine + shared enums + `ProjectAggregate.Handle(CreateProject)`/`Apply(ProjectCreated)` + one rejection event + list/detail projection + CI green — ships `CreateProject` end-to-end), **(1b)** `TenantAccessProjection` + layered fail-closed authz + remaining lifecycle (FR-2/3/4/5), **(1c)** Aspire/Dapr/Workers topology + remaining projections. Use **partial classes per concern** (`ProjectAggregate.References.cs`, `ProjectAggregate.Resolution.cs`) from 1a so Epic 2 and Epic 4 don't churn the same file.

## Upstream Prerequisite & Sequencing Stories (cross-module gaps)

_How AR-G1–G4 are sequenced so no story is blocked mid-flight. Each upstream change is a separate submodule-first PR (never mixed with Projects code or submodule-pointer churn)._

- **PR-1 — AR-G1 (resolved):** Additive "reassign / move conversation project" capability in **`Hexalith.Conversations`** is consumed through Projects ACLs. Projects preserves Pattern A ownership and never persists local conversation membership.
- **PR-2 — AR-G2 (prerequisite for Story 2.1):** Add `IConversationClient.ListConversationsAsync` (forwarding to the existing `ConversationQueryHandler.ListAsync`) **or** consume `ConversationReadApi` — as a separate Conversations-submodule PR + version bump — then consume it in Story 2.1's ACL. Optionally write a consumer-driven contract test first so the ACL translator (Tier-1 pure) can progress against a verified double.
- **PR-3 — AR-G3 (FR-1 auto-folder split):** `CreateProject` (Epic 1, Story 1a) ships **without** auto-folder. Auto-folder-create becomes a separate story gated on Folders' `CreateFolder` being exposed as external REST (or invoked in-topology via the typed client/Dapr).
- **PR-4 — AR-G4 (Memories deferred behind decision spike):** A decision spike resolves whether a Project Memory link maps to a Memories `Case` or individual `MemoryUnit` references **before** any Memories write-side story. Memories writes are `[Experimental]`/async/eventually-consistent → Memories reference write-side sits at the **back of Epic 2** (or a follow-up); Epic 3's allowlist treats an absent/unavailable Memories reference as a **fail-closed-clean** state.

---

## Epic 1: Project Workspace Foundation

Stand up a deployable, tenant-isolated, authenticated `Hexalith.Projects` service where Chatbot can create, open, list, update, and archive projects with validated setup — and establish the load-bearing invariants (shared vocabulary, payload taxonomy, identity derivation, fail-closed authorization, leakage/rebuild/schema-evolution harnesses) every later epic depends on.

**FRs covered:** FR-1, FR-2, FR-3, FR-4, FR-5, FR-19

### Story 1.1: Module scaffold & build/CI wiring

As a **Projects platform engineer**,
I want **the `Hexalith.Projects` module scaffolded from the established Hexalith sibling shape with build, package, and CI wiring in place**,
So that **every later story has a compiling, test-gated module to build into without re-deciding structure**.

**Acceptance Criteria:**

**Given** a greenfield module (no `Hexalith.Projects` code exists)
**When** the module is scaffolded
**Then** the project set exists per the architecture tree — `Contracts`, `Client`, core `Hexalith.Projects`, `Server`, `Workers`, `Mcp`, `Cli`, `UI`, `ServiceDefaults`, `Aspire`, `AppHost`, `Testing` — plus `tests/` (`Contracts.Tests`, `Tests` Tier-1, `Server.Tests` Tier-2, `Integration.Tests` Tier-3, `e2e`)
**And** `Hexalith.Projects.slnx`, `global.json` (SDK `10.0.302`, `rollForward: latestPatch`), `Directory.Build.props`, `Directory.Packages.props` (central package management, no inline versions), and shared `Module.Directory.*.props` from Hexalith.Builds/Samples are present.

**Given** the umbrella repo with sibling submodule dependencies
**When** dependencies are initialized
**Then** only root-level submodules are initialized (no `--recursive`)
**And** sibling references resolve via the root-detection `HexalithProjectsRoot`/sibling-root pattern.

**Given** the scaffolded module
**When** CI runs
**Then** `dotnet build Hexalith.Projects.slnx` and the filtered `dotnet test` lane pass green
**And** placeholder CI gates for `frontcomposer inspect --fail-on-warning` and the OpenAPI fingerprint check are wired (no-op-clean until their inputs exist in 1.3).

**Given** `net10.0`, nullable, implicit usings, and warnings-as-errors
**When** the module builds
**Then** no compiler setting is weakened to make the build pass.

### Story 1.2: Shared vocabulary, identifiers & payload taxonomy

As a **Projects platform engineer**,
I want **the single shared state/reason-code vocabulary, the `ProjectId` identifier, the rejection-event taxonomy, the payload-classification allowlist, and the canonical identity-derivation helper defined once**,
So that **every aggregate, projection, ACL, query, audit record, and generated surface uses the same semantics and no story reinvents them (preventing drift and leakage)**.

**Acceptance Criteria:**

**Given** the need for opaque, validated identity
**When** `ProjectId` is defined in `Contracts/Identifiers`
**Then** it is a `sealed record` value object with eager boundary validation (throws on empty/invalid) and a custom `System.Text.Json` converter
**And** sibling references reuse the owning context's Contracts identifier types (e.g. `ConversationId`, Folders `FolderId`/`FileId`, Memories ids) rather than minting parallel VOs.

**Given** the requirement for one shared vocabulary (AR-18, UX-DR5)
**When** the UI enums are defined in `Contracts/Ui`
**Then** a single `[ProjectionBadge]`-annotated enum set exists covering lifecycle (`Active`, `Archived`), reference/inclusion states (`included`, `excluded`, `unauthorized`, `unavailable`, `stale`, `archived`, `ambiguous`, `tenant_mismatch`, `conflict`, `invalidReference`), resolution results (`NoMatch`, `SingleCandidate`, `MultipleCandidates`), and reason codes (`ConversationLinked`, `ProjectFolderMatched`, `FileReferenceMatched`, `MemoryMatched`, `MetadataMatched`)
**And** each state has a stable code, display label, accessible name, and severity mapping
**And** an `IRejectionEvent` taxonomy is defined for the project rejection events.

**Given** the metadata-only / payload-safety requirement (FS-1, NFR-2)
**When** the payload-classification allowlist is authored (`docs/` + a machine-usable form)
**Then** it enumerates reference-only/safe fields vs forbidden sibling-owned content (conversation text, file contents, memory bodies, folder names where sensitive, prompts, secrets, raw tokens, command bodies, unrestricted paths)
**And** it is referenced as the source of truth for the FS-2 leakage harness.

**Given** canonical identity `{tenant}:projects:{projectId}` (AR-4, FS-3)
**When** the identity-derivation helper is implemented
**Then** actor IDs, state keys, projection keys, pub/sub topics, SignalR groups, and log scopes are all derived from it
**And** Tier-1 conformance tests assert every derived key/topic/group/scope is produced only from the canonical identity (no payload/header/query-derived identity).

### Story 1.3: OpenAPI Contract Spine + generated typed client

As a **Chatbot integration engineer**,
I want **the OpenAPI 3.1 Contract Spine to be the single source of truth driving a generated typed client, with idempotency, correlation, freshness headers, and RFC 9457 errors**,
So that **consumers call a stable, version-checked contract and mutations/queries behave predictably (command-async, safe-denial)**.

**Acceptance Criteria:**

**Given** the contract-spine-as-source-of-truth rule (AR-15)
**When** `Contracts/openapi/hexalith.projects.v1.yaml` is authored and the client generated
**Then** the NSwag-generated client lives under `Client/Generated/*.g.cs` and is never hand-edited
**And** an OpenAPI fingerprint/compatibility check runs in CI and fails on incompatible contract drift.

**Given** the command-async API shape (AR-16)
**When** a mutation endpoint is invoked
**Then** it returns `202 AcceptedCommand` and the contract documents no read-after-write guarantee
**And** query responses carry freshness/trust state
**And** `Idempotency-Key` is required on mutations and rejected on queries, with field-scoped equivalence via the shared hasher; `X-Correlation-Id`, `X-Hexalith-Task-Id`, and `X-Hexalith-Freshness` are threaded.

**Given** the error and safe-denial requirements (AR-16, NFR-1)
**When** an error or denial occurs
**Then** responses use RFC 9457 Problem Details + Hexalith extensions (`category`, `code`, `correlationId`, `retryable`, `clientAction`, `details.visibility`)
**And** unauthorized and nonexistent both surface as `404` (safe-denial) so cross-tenant existence cannot be inferred
**And** no error echoes secrets, payloads, or sensitive values.

### Story 1.4: Create Project (end-to-end tracer bullet)

As **Hexalith.Chatbot**,
I want **to create a Project with tenant context and a Project name and have it durably recorded as `Active`**,
So that **a conversation can be anchored to a real, tenant-scoped workspace record** _(realizes UJ-2, UJ-3; FR-1, FR-19)_.

**Acceptance Criteria:**

**Given** a valid tenant context and a Project name (the only required user input)
**When** `CreateProject` is submitted through the EventStore command pipeline
**Then** `ProjectAggregate.Handle` emits `ProjectCreated` (pure, persist-then-publish), `Apply` sets lifecycle `Active`, and `ProjectListProjection`/`ProjectDetailProjection` reflect it after projection update
**And** the response is `202 AcceptedCommand` confirmable via `GetProject`
**And** no conversation transcript, file content, or memory payload is duplicated into the Project.

**Given** missing/unauthorized tenant context
**When** `CreateProject` is submitted
**Then** the command fails closed and `ProjectCreationRejected` (an `IRejectionEvent`) is emitted with a shared reason code — not an exception path — and one `DomainResult` never mixes success and rejection payloads.

**Given** setup validation rules (FR-19)
**When** `CreateProject` carries setup
**Then** raw secrets, unrestricted local paths, unsupported reference types, and foreign-context payloads are rejected with a structured error that names the rejected field without echoing its value
**And** when no Project Folder is supplied the create succeeds without one (auto-folder is deferred to PR-3, not required here).

**Given** the metadata-only invariant (FS-2) and schema tolerance (FS-5)
**When** `ProjectCreated`/`ProjectCreationRejected` are serialized
**Then** the `NoPayloadLeakage` harness asserts no forbidden field appears in the event, log, or DTO
**And** a frozen serialized sample of each event is added to the schema-evolution corpus and round-trips in CI.

### Story 1.5: Projection rebuild/replay & idempotency

As a **Projects platform engineer**,
I want **deterministic projection rebuild and idempotent handling of duplicate command and projection-event delivery proven on the trivial event set**,
So that **at-least-once Dapr delivery and projection rebuilds never corrupt read models or aggregate state, and the property is cheap to extend per epic** _(FS-6, NFR-7)_.

**Acceptance Criteria:**

**Given** a stream of `ProjectCreated` events
**When** `ProjectListProjection`/`ProjectDetailProjection` are rebuilt from events
**Then** the rebuilt state is identical to the incrementally-applied state (same events → same state) and rebuild is a tested, repeatable operation.

**Given** at-least-once command delivery
**When** the same `CreateProject` (same `Idempotency-Key`/logical attempt) is delivered twice
**Then** the aggregate dedupes and the second delivery produces no second `ProjectCreated` (idempotent), with field-scoped equivalence; a same-key/different-payload attempt yields an idempotency conflict.

**Given** at-least-once projection-event delivery
**When** the same `ProjectCreated` is dispatched to a projection twice (or out of order)
**Then** `Apply` is idempotent — the projection reflects the event exactly once and tolerates duplicate/out-of-order delivery
**And** these are covered as **separate** Tier-1 tests from the command-dedup tests.

### Story 1.6: Tenant access & layered fail-closed authorization

As a **security-conscious platform operator**,
I want **tenant membership/status projected locally from Tenants events and a layered, fail-closed authorization chain enforced on every Projects operation**,
So that **cross-tenant access is impossible by construction and unverifiable evidence denies access** _(realizes UJ-4; NFR-1, NFR-3; AR-13, AR-19; FS-8)_.

**Acceptance Criteria:**

**Given** Tenants publishes lifecycle/membership events
**When** `Hexalith.Projects.Workers` consumes them via Dapr pub/sub into `TenantAccessProjection`
**Then** handlers are idempotent (dedupe by message id), tolerate out-of-order/at-least-once delivery, and write through a durable-store interface (in-memory only for tests)
**And** after restart the projection is treated as not-yet-usable and fails closed until rebuilt/usable (no silent allow).

**Given** an authenticated request (JWT/OIDC, Keycloak realm `hexalith`)
**When** authorization is evaluated
**Then** the layered chain runs in order: API/JWT → EventStore claim-transform → `TenantAccessProjection` (exists/active/member/role) → project-level authz (belongs-to-tenant, accessible, active) → query-side result filtering
**And** tenant authority is taken only from authenticated claims + claim-transform — never from payload/header/query
**And** Project data uses the user-facing tenant as the EventStore envelope tenant (not `system`).

**Given** missing/unknown/disabled/stale/unauthorized evidence
**When** a trust-bearing operation is attempted
**Then** it fails closed with a safe-denial `404` and a structured reason code (no existence leakage).

**Given** the cross-tenant isolation conformance suite (FS-8, SM-3)
**When** a request scoped to tenant A targets tenant B's project/data
**Then** it never returns tenant B's data — the negative-test suite proves "Project A / tenant B context never leaks" and is structured to be re-run against every new surface.

### Story 1.7: Open & List Projects

As **Hexalith.Chatbot**,
I want **to open a Project and list the Projects visible to my tenant/user context**,
So that **I can present project choices and load the metadata needed to initialize a conversation** _(realizes UJ-1; FR-2, FR-5)_.

**Acceptance Criteria:**

**Given** an authorized request for a specific Project
**When** `GetProject` is queried over `ProjectDetailProjection`
**Then** it returns Project metadata, lifecycle state, setup, and the references visible to the requesting context, carrying freshness/trust state
**And** an Archived or unavailable Project is clearly identified and cannot silently become active conversation context.

**Given** an authorized list request
**When** `ListProjects` is queried over `ProjectListProjection`
**Then** results are tenant-scoped and authorization-filtered, can filter by lifecycle state (`Active`/`Archived`), and include enough metadata to present choices without loading full Project Context.

**Given** an unauthorized or cross-tenant request
**When** open/list is attempted
**Then** results are filtered/hidden via query-side filtering and safe-denial — never leaking another tenant's projects.

### Story 1.8: Update Project Setup & Archive Project

As **Hexalith.Chatbot or an authorized operator**,
I want **to update a Project's durable setup and to archive a Project**,
So that **conversation continuity stays current and finished projects remain auditable without being auto-selected as context** _(FR-3, FR-4, FR-19)_.

**Acceptance Criteria:**

**Given** an authorized setup update
**When** `UpdateProjectSetup` is submitted
**Then** `ProjectSetupUpdated` is emitted; setup may include project goals, user-facing instructions, preferred/excluded context sources, and conversation-start defaults; updates are durable and additive/serialization-tolerant
**And** update-time validation (FR-19) rejects raw secrets, unrestricted paths, unsupported reference types, and foreign-context payloads with a field-naming structured error
**And** v1 setup describes conversation behavior/context policy only — not model-provider internals.

**Given** an authorized archive request
**When** `ArchiveProject` is submitted
**Then** `ProjectArchived` is emitted, lifecycle becomes `Archived` (v1 limited to `Active`/`Archived`), the Project remains discoverable for history, and existing references remain auditable
**And** archived Projects are excluded from automatic Project Resolution unless explicitly requested.

**Given** an invalid or unauthorized update/archive
**When** the command is submitted
**Then** it fails closed with the appropriate rejection event (`ProjectSetupUpdateRejected`/`ProjectArchiveRejected`) and a shared reason code.

### Story 1.9: Aspire/Dapr/Workers topology & operational skeleton

As a **Projects platform engineer**,
I want **the local Aspire topology, Dapr components, Workers host, and service defaults wired with resiliency, dead-letter, and observability**,
So that **the module runs end-to-end locally and is operationally sound for later epics** _(AR-20, AR-21, AR-22; NFR-4 baseline)_.

**Acceptance Criteria:**

**Given** the AppHost
**When** `dotnet run --project src/Hexalith.Projects.AppHost` is executed
**Then** it boots eventstore, tenants, projects, workers, Keycloak, and Dapr sidecars/components as a coherent local topology
**And** the Dapr `statestore` and `pubsub` components use the configured local Redis backing endpoint, defaulting to the Dapr-initialized Redis instance rather than creating a second Redis server
**And** `Hexalith.Projects.Workers` hosts projection processing + the Tenants-event subscription.

**Given** Dapr as the only infrastructure abstraction (AR-20)
**When** internal endpoints (`/process`, projection dispatch, subscriptions) are exposed
**Then** Dapr access-control allowlists scope them; resiliency (retry/timeout/circuit-breaker) is applied at service-invocation boundaries (not in domain handlers); dead-letter topics + a replay/rebuild runbook exist
**And** the dev access-control policy is clearly dev-only and never shipped to production.

**Given** ServiceDefaults
**When** the services run
**Then** OpenTelemetry traces/metrics/logs flow with tenant/project/correlation/reason metadata only (no payloads), and health endpoints distinguish API/Dapr/state-store/pub-sub/projection-freshness readiness.

---

## Epic 2: Context References

Make a Project a true workspace boundary by connecting it — by stable ID, tenant-scoped, fail-closed — to its conversations, its single Project Folder, optional file references, and memories, behind Projects-owned Anti-Corruption Layers. Underlying resources stay owned by their contexts; unlinking never deletes them.

**FRs covered:** FR-6, FR-7, FR-8, FR-9, FR-10, FR-11

> Reconciliation note: **Story 2.1 corresponds to the already-drafted `_bmad-output/implementation-artifacts/2-1-conversation-reference-read-acl.md`** (status `ready-for-dev`). The acceptance criteria below are the epic-level summary of that file; the implementation-artifact file remains the detailed dev spec. Reconcile, do not duplicate.

### Story 2.1: Conversation Reference Read ACL (Projects → Conversations)

As the **Hexalith.Projects module (on behalf of Hexalith.Chatbot)**,
I want **a tenant-scoped, fail-closed read path that lists a Project's conversations by querying `Hexalith.Conversations` by `ProjectId` behind a Projects-owned ACL**,
So that **Project Context and project views can present linked conversations with freshness/trust signals — without copying transcripts or coupling Projects to Conversations internals** _(Pattern A; backs FR-16; read side of FR-6)_.

**Acceptance Criteria:**

**Given** the need for a Projects-shaped read surface
**When** `IProjectConversationDirectory.ListForProjectAsync(projectId, tenantId, caller, page, ct)` is implemented in `Projects.Server`
**Then** it returns a Projects-shaped `ProjectConversationsPage` (not Conversations' `ConversationListResult`), and `Hexalith.Conversations.*` types appear only inside the ACL — never in Projects domain/projection/UI/contract code.

**Given** the AR-G2 client gap (`IConversationClient` has no list method)
**When** discovery is wired
**Then** it queries Conversations via `ConversationListFilterV1(ProjectId: …)` through the verified server path, reached by either (A1) a new additive `IConversationClient.ListConversationsAsync` in the Conversations submodule (own commit/tests, no submodule-pointer churn mixed into Projects code) or (A2) a `ConversationReadApi` HTTP call — the choice recorded in the Dev Agent Record.

**Given** each returned `ConversationSummaryV1`
**When** it is translated
**Then** it becomes a Projects view item carrying at minimum `ConversationId`, lifecycle/status, optional label, and a Projects-owned freshness/trust signal derived from `ProjectionTrustState`/`ProjectionFreshnessV1`; the reference is a typed `ConversationId` value (reused from Conversations.Contracts), never a `ConversationState`/transcript.

**Given** tenant isolation (fail-closed)
**When** a request scoped to tenant A is made
**Then** it never returns tenant B's conversations; cross-tenant/`Forbidden`/unauthorized yields an empty/hidden page (never a leak, never an existence-revealing exception); authorization is decided from caller principal + tenant scope, not a JWT tenant claim alone.

**Given** degraded reads (`Stale`/`Rebuilding`/`Unavailable`/`Redacted`/`Forbidden`/`MixedGeneration`)
**When** results are returned
**Then** they map to an explicit Projects trust signal so the view can show "may be incomplete/rebuilding" rather than presenting degraded data as current; no Conversations command is dispatched (read-only).

**Given** Tier-1 purity
**When** tests run
**Then** they cover the translator across every `ProjectionTrustState`, empty/paging continuation, and tenant-isolation negative paths, with no Dapr/network/containers/browser.

### Story 2.2: Conversation project (re)assignment — upstream capability *(enabler / PR-1 / AR-G1)*

As a **Projects platform engineer**,
I want **`Hexalith.Conversations` to support setting or changing a conversation's `ProjectId` after creation via an additive command + event**,
So that **the Projects write-side can link and move conversations without violating Conversations' ownership of the link** _(prerequisite for Story 2.3 and Epic 4 FR-15)_.

**Acceptance Criteria:**

**Given** the conversation↔project link is currently immutable (set only at `ConversationCreated`, no re-parent event)
**When** the capability is added in the `Hexalith.Conversations` submodule
**Then** a new additive command (e.g. `ReassignConversationProject`) and past-tense event (e.g. `ConversationProjectChanged`) exist, the aggregate `Handle`/`Apply` update `ConversationState.ProjectId`, and the change is additive/serialization-tolerant (no `V2`, backward-compatible deserialization preserved).

**Given** submodule discipline
**When** the change lands
**Then** it is a self-contained, separately-tested commit/PR in `Hexalith.Conversations` (own tests there), an ADR records the ownership decision, and no Projects code or submodule-pointer update is mixed into it.

**Given** tenant isolation and authorization
**When** a reassignment is processed
**Then** it is tenant-scoped and fails closed when authorization to the conversation cannot be established, emitting a rejection rather than leaking existence.

### Story 2.3: Link & Move Conversation (write-side)

As **Hexalith.Chatbot**,
I want **to link an existing conversation to a Project, move it between Projects on explicit confirmation, and unlink it**,
So that **a project's conversation membership reflects the user's intent while preserving single-project membership** _(FR-6, FR-7, FR-11 conversation; depends on Story 2.2)_.

**Acceptance Criteria:**

**Given** the upstream reassignment capability (2.2) exists
**When** Chatbot links an existing conversation to a Project (FR-6)
**Then** the link records stable conversation identity + metadata (no transcript copy), enforces single-project membership in v1, and fails closed if conversation tenant authorization cannot be established.

**Given** a conversation already belonging to another Project
**When** a move is requested (FR-7)
**Then** it requires explicit user confirmation, removes the prior membership before creating the new one, is auditable as metadata, and fails closed when authorization to either Project or the conversation cannot be established (no second concurrent membership is ever created).

**Given** an authorized unlink (FR-11)
**When** a conversation reference is removed
**Then** the association is removed from Project Context, the underlying conversation is not deleted, and the unlink is auditable.

### Story 2.4: Set & auto-create Project Folder

As **Hexalith.Chatbot**,
I want **to set the single authorized Project Folder for a Project, and to have one auto-created with the Project name when none is supplied at creation**,
So that **a Project always has exactly one folder boundary without forcing the user to pre-create it** _(FR-8; completes the deferred FR-1 auto-folder; realizes UJ-2)_.

**Acceptance Criteria:**

**Given** an authorized request
**When** `SetProjectFolder` is submitted
**Then** `ProjectFolderSet` records stable Folder identity + metadata (no file contents/unrestricted paths), the Project has exactly one Project Folder, replacing it happens only via an explicit update, and folder authorization remains delegated to `Hexalith.Folders` via the Folders ACL; the `ProjectReferenceIndexProjection` is established/updated.

**Given** the deferred auto-folder slice (PR-3, gated on AR-G3 Folders `CreateFolder` external REST)
**When** a Project is created without a supplied folder
**Then** Projects requests creation of a Project Folder with the same name via the Folders ACL (command-async: 202 → confirm via lifecycle/freshness, no read-after-write assumption)
**And** if the Folders `CreateFolder` operation is not yet externally available, the story degrades gracefully (folder creation queued/flagged) without blocking project creation, and this gating is recorded.

**Given** the v1 "Projects require a Folder while active" rule (FR-11)
**When** removal of the Folder is attempted on an active Project
**Then** it is rejected (the Folder is replaceable but not removable unless the Project is archived).

**Given** a failure or denial from Folders
**When** the ACL handles it
**Then** it maps to a Projects-safe problem with a shared reason code — never rethrowing raw upstream detail.

### Story 2.5: Link/Unlink File Reference

As **Hexalith.Chatbot**,
I want **to link authorized file references to a Project and unlink them without changing the Project Folder**,
So that **specific files can be part of Project Context as optional references** _(FR-9, FR-11 file)_.

**Acceptance Criteria:**

**Given** an authorized file reference
**When** `LinkFileReference` is submitted
**Then** `FileReferenceLinked` records stable File identity + metadata, file references are optional and do not replace the Project Folder, authorization remains delegated to `Hexalith.Folders`, and the reference index is extended.

**Given** an authorized unlink (FR-11)
**When** `UnlinkFileReference` is submitted
**Then** `FileReferenceUnlinked` removes the association from Project Context, does not delete the underlying file, and is auditable.

**Given** an unauthorized/stale file reference
**When** link/unlink is attempted
**Then** it fails closed via the Folders ACL with a Projects-safe reason code, and raw file contents are never treated as Project-owned data.

### Story 2.6: Memories linkage decision spike *(enabler / PR-4 / AR-G4)*

As a **Projects platform engineer**,
I want **a decision on whether a Project Memory link maps to a Memories `Case` or to individual `MemoryUnit` references**,
So that **the Memory ACL and its tests are built against a settled model rather than churning** _(prerequisite for Story 2.7)_.

**Acceptance Criteria:**

**Given** the Memories model is Tenant → Case → MemoryUnit and writes are `[Experimental]`/async/eventually-consistent
**When** the spike concludes
**Then** an ADR records whether a Project Memory link targets a `Case` or `MemoryUnit`(s), the rationale, and the eventual-consistency handling strategy (deterministic convergence assertions, not sleeps).

**Given** the decision
**When** it is recorded
**Then** it defines the Memory reference identifier shape, the ACL contract surface, and how Epic 3's allowlist treats an absent/unavailable Memories reference as a fail-closed-clean state.

### Story 2.7: Link/Unlink Memory

As **Hexalith.Chatbot**,
I want **to link authorized memory references to a Project and unlink them**,
So that **relevant memories are part of Project Context without Projects storing memory payloads** _(FR-10, FR-11 memory; depends on Story 2.6; realizes UJ-1, UJ-3)_.

**Acceptance Criteria:**

**Given** the settled linkage model (2.6) and an authorized memory reference
**When** `LinkMemory` is submitted
**Then** `MemoryLinked` records stable Memory identity + metadata (no payload copy), authorization remains delegated to `Hexalith.Memories` via the Memories ACL, and the reference index is extended.

**Given** an authorized unlink (FR-11)
**When** `UnlinkMemory` is submitted
**Then** `MemoryUnlinked` removes the association from Project Context, does not delete the underlying memory, and is auditable.

**Given** the experimental/async/eventually-consistent Memories surface
**When** the ACL calls Memories
**Then** it is `[Experimental]`-aware (façaded so signature churn is contained), treats ingestion as eventually consistent (no read-after-write), and maps Memories failures/denials to Projects-safe reason codes without leaking payloads.

---

## Epic 3: Project Context Assembly

Give Chatbot the assembled context it needs to start or resume a conversation: a tenant-scoped, authorization-filtered `ProjectContext` (setup + included references with explicit exclusion reasons), an explanation of inclusion/exclusion, a refresh path that surfaces degraded references, and the conversation-start setup subset. Assembly is allowlist-based and fails closed; everything is metadata-only.

**FRs covered:** FR-16, FR-17, FR-18, FR-20

### Story 3.1: Context-assembly policy & allowlist

As a **Projects platform engineer**,
I want **a pure, allowlist-based `ProjectContext` assembly policy that includes a reference only after tenant, project, lifecycle, authorization, and freshness checks all pass**,
So that **context assembly is a tested security boundary that can never silently include an unverified reference** _(AR-9; the core of NFR-1/NFR-2/NFR-3; realizes UJ-4)_.

**Acceptance Criteria:**

**Given** Project state + projection data + ACL-fetched sibling metadata
**When** the assembly policy composes a `ProjectContext`
**Then** it is computed as an assembled read result (never a persisted aggregate), and a reference is included **only** when tenant + project + lifecycle + authorization + freshness all pass (allowlist inclusion).

**Given** a reference that fails any check
**When** assembly runs
**Then** the reference is excluded and carries an explicit inclusion state + reason code from the shared vocabulary (`excluded`/`unauthorized`/`unavailable`/`stale`/`archived`/`ambiguous`/`tenant_mismatch`/`conflict`/`invalidReference`) — exclusion is surfaced, never silent — and an absent/unavailable Memories reference is treated as a fail-closed-clean exclusion.

**Given** the metadata-only invariant (FS-2)
**When** an assembled `ProjectContext` is produced
**Then** it contains references + metadata only (no transcripts/file contents/memory payloads/prompts/secrets), verified by the `NoPayloadLeakage` harness extended over the assembly DTOs.

**Given** Tier-1 purity
**When** policy tests run
**Then** the include/exclude decision matrix is covered with pure tests (no Dapr/network), including the case where a non-allowlisted reference type is presented (→ excluded, reason-coded, logged).

### Story 3.2: Get Project Context

As **Hexalith.Chatbot**,
I want **to request the assembled Project Context for a Project**,
So that **I receive the setup plus authorized references needed to initialize a conversation with the correct boundary** _(FR-16; realizes UJ-1, UJ-4)_.

**Acceptance Criteria:**

**Given** an authorized request for a Project's context
**When** `GetProjectContext` runs the assembly policy (3.1) over the conversation read ACL (2.1), Folders/Memories ACLs, and the reference index
**Then** it returns Project Setup plus authorized references to conversations, the Project Folder, file references, and memories — tenant-scoped, authorization-filtered, references+metadata only
**And** it indicates which referenced resources were excluded and why (authorization/lifecycle/availability).

**Given** the fail-closed requirement (NFR-3)
**When** authorization, lifecycle, or referenced-resource availability cannot be verified
**Then** the operation fails closed (the reference is excluded with a safe reason code, or the request is safely denied) rather than including unverified context.

**Given** the fail-closed negative-evidence matrix (Murat)
**When** tests run
**Then** the matrix of (evidence-state: missing/stale/unauthorized/unavailable) × (operation: Get) is explicitly covered, and the `NoPayloadLeakage` assertion runs over the returned DTO.

### Story 3.3: Explain Context Selection

As **an administrator / Hexalith.Chatbot**,
I want **metadata explaining why each conversation, folder, file, or memory reference was included or excluded from Project Context**,
So that **incorrect context selection can be diagnosed without exposing protected data** _(FR-17; realizes UJ-4)_.

**Acceptance Criteria:**

**Given** an assembled (or refused) Project Context
**When** the explanation is requested
**Then** it returns, per reference, the inclusion/exclusion state + reason code + safe evidence (e.g. `lastCheckedAt`, owner context) sufficient to troubleshoot.

**Given** the payload-safety rule
**When** the explanation is produced
**Then** it contains no secrets, file contents, transcript payloads, prompts, or memory payloads (verified by the leakage harness).

### Story 3.4: Refresh Project Context

As **Hexalith.Chatbot**,
I want **to request a refreshed Project Context after links, setup, or resource availability change**,
So that **the context I use reflects the current state rather than stale assumptions** _(FR-18)_.

**Acceptance Criteria:**

**Given** links/setup/availability have changed
**When** `RefreshProjectContext` runs
**Then** the result reflects current Project links and lifecycle state and preserves all tenant authorization checks.

**Given** a reference that is now stale or unavailable
**When** refresh runs
**Then** that reference is surfaced with its degraded state + reason code rather than being silently ignored
**And** the (evidence-state × Refresh) slice of the fail-closed matrix is covered.

### Story 3.5: Retrieve Conversation-Start Setup

As **Hexalith.Chatbot**,
I want **to retrieve the subset of Project Setup needed to start or resume a conversation**,
So that **I can begin the first response without re-querying every bounded context** _(FR-20)_.

**Acceptance Criteria:**

**Given** a Project with durable setup
**When** `GetConversationStartSetup` is queried over the `ConversationStartSetupProjection`
**Then** it returns project goals, instructions, context preferences, and the default linked-source policy
**And** it excludes internal audit metadata and unavailable/unauthorized references
**And** the result is stable enough to use without re-querying every bounded context before the first response.

**Given** tenant/authorization scope
**When** the query runs
**Then** results are tenant-scoped and authorization-filtered, failing closed on unverifiable evidence.

---

## Epic 4: Project Resolution

When a conversation arrives without an explicit Project, help Chatbot find the right one — resolve candidates from conversation metadata and from attachments, confirm an ambiguous match, or propose a new Project — with correctness over automation: never silently attaching, never creating from inference without confirmation, archived excluded unless explicitly requested.

**FRs covered:** FR-12, FR-13, FR-14, FR-15

### Story 4.1: Resolution engine (compute-on-demand)

As a **Projects platform engineer**,
I want **a pure, compute-on-demand resolution engine that scores candidate Projects and returns a typed outcome with reason codes, persisting nothing**,
So that **resolution is deterministic, testable, and never stores sensitive inference data** _(AR-10; NFR-9)_.

**Acceptance Criteria:**

**Given** inputs (conversation metadata and/or attached references) and the reference index + ACL-fetched metadata
**When** the engine computes
**Then** it returns a `Resolution Result` of `NoMatch`, `SingleCandidate`, or `MultipleCandidates`, and each candidate carries one or more reason codes from the shared vocabulary (`ConversationLinked`, `ProjectFolderMatched`, `FileReferenceMatched`, `MemoryMatched`, `MetadataMatched`).

**Given** the compute-on-demand rule
**When** resolution runs
**Then** nothing is persisted (no resolution trace is written; only a later confirmation persists state), archived Projects are excluded unless explicitly requested, and a documented scoring/confidence-band heuristic determines candidate ranking and the single-vs-multiple threshold.

**Given** Tier-1 purity
**When** engine tests run
**Then** they cover no-match, single, multiple, archived-exclusion, and unauthorized-resource-exclusion cases with pure tests (no Dapr/network).

### Story 4.2: Resolve Project From Conversation

As **Hexalith.Chatbot**,
I want **to ask Projects to resolve candidate Projects for a conversation that has no explicit Project**,
So that **I can infer or request the correct project context from conversation metadata** _(FR-12; realizes UJ-3)_.

**Acceptance Criteria:**

**Given** a conversation with no explicit Project
**When** `ResolveProjectFromConversation` runs the engine (4.1) over conversation metadata
**Then** it returns `NoMatch`/`SingleCandidate`/`MultipleCandidates` with reason code(s), and excludes archived Projects unless explicitly requested.

**Given** authorization scope
**When** resolution runs
**Then** it does not access unauthorized conversations, folders, files, memories, or projects, and fails closed on unverifiable tenant evidence.

### Story 4.3: Resolve Project From Attachments

As **Hexalith.Chatbot**,
I want **to ask Projects to resolve candidate Projects from an attached Project Folder or file references**,
So that **a user who starts with files before choosing a project is matched to the right workspace** _(FR-13; realizes UJ-2)_.

**Acceptance Criteria:**

**Given** attached Project Folder / File references
**When** `ResolveProjectFromAttachments` runs the engine over existing folder/file references
**Then** matching considers existing Project Folder and File References and tags `ProjectFolderMatched`/`FileReferenceMatched` reason codes where applicable.

**Given** missing or stale Folder/File authorization
**When** matching runs
**Then** it fails closed, and raw file contents are never treated as Project-owned data.

### Story 4.4: Confirm Ambiguous Project

As **Hexalith.Chatbot**,
I want **to present multiple candidate Projects and record the user's confirmed choice**,
So that **an ambiguous match is resolved by the user rather than guessed** _(FR-14; NFR-9)_.

**Acceptance Criteria:**

**Given** a `MultipleCandidates` resolution result
**When** candidates are presented
**Then** Projects does **not** silently attach the conversation.

**Given** the user confirms a candidate
**When** `ConfirmProjectResolution` is submitted
**Then** `ProjectResolutionConfirmed` is emitted and the Project-to-Conversation association is created/updated (via the Story 2.2/2.3 link path), while rejected candidates are not linked
**And** an invalid/unauthorized confirmation fails closed with `ProjectResolutionConfirmationRejected`.

### Story 4.5: Propose New Project

As **Hexalith.Chatbot**,
I want **to propose creating a new Project from the current conversation, attachments, and setup metadata when no suitable Project exists**,
So that **a user with no matching project can start one without losing the current context** _(FR-15; realizes UJ-2, UJ-3; depends on Story 2.2)_.

**Acceptance Criteria:**

**Given** a `NoMatch` resolution result
**When** a proposal is generated
**Then** it includes a suggested Project name and initial setup metadata when available, and **no Project is created from inference until an authorized user action confirms creation**.

**Given** the user confirms the proposal
**When** the new Project is created
**Then** it is created via `CreateProject` (Epic 1) and links the initiating conversation (via the Story 2.2/2.3 reassignment path) and the authorized attachments
**And** the flow fails closed if authorization for the conversation or attachments cannot be established.

---

## Epic 5: Operational Console & Audit (CLI / MCP / Web)

Deliver the administrative/operational product: a FrontComposer-generated Metadata Control Plane console plus parity MCP and CLI surfaces over one diagnostic model. Administrators, operators, and MCP-assisted agents can inspect projects, reference health, resolution traces, and metadata-only audit history, and perform safe, audit-first maintenance — across three surfaces that expose equivalent facts.

**FRs covered:** FR-21, FR-22

### Story 5.1: Audit timeline projection & metadata-only audit events

As an **authorized operator / support agent**,
I want **every Project lifecycle and context-reference change recorded as a metadata-only audit event in a queryable timeline**,
So that **I can reconstruct what happened to a Project without accessing any payload data** _(FR-21)_.

**Acceptance Criteria:**

**Given** any Project operation
**When** it occurs (creation, setup update, archival, conversation link/move, Project Folder change, file/memory link/unlink, resolution confirmation, new Project created from a proposal)
**Then** the `ProjectAuditTimelineProjection` records an audit event including tenant, Project identity, operation type, timestamp, actor identity where available, and affected reference identifiers.

**And** a new Project created from a proposal is derived from the explicit `CreateProject` + assignment/folder/file command chain and composite metadata; Story 4.5 intentionally emits no `ProjectCreatedFromProposal` event and stores no proposal aggregate.

**Given** the metadata-only rule
**When** audit events are recorded/serialized
**Then** they contain no transcript payloads, file contents, raw prompts, secrets, or memory payloads (verified by the `NoPayloadLeakage` harness), and audit derives from EventStore envelope metadata + Project events.

**Given** tenant scope
**When** the audit timeline is queried
**Then** it is tenant-scoped and authorization-filtered.

### Story 5.2: Operator read access

As an **authorized operator**,
I want **authorization-gated, tenant-scoped read access to Project metadata, lifecycle, references, resolution outcomes, and audit metadata**,
So that **I can troubleshoot project state without write power or payload exposure** _(FR-22)_.

**Acceptance Criteria:**

**Given** an authorized operator request
**When** operator read queries run
**Then** they expose metadata only, are authorization-gated and tenant-scoped, and provide no write capability beyond archive/troubleshooting workflows explicitly exposed.

**Given** an unauthorized or cross-tenant operator request
**When** a read is attempted
**Then** it is safely denied (404) and reveals no cross-tenant existence.

### Story 5.3: FrontComposer console shell & shared rendering

As an **administrator**,
I want **a FrontComposer-composed operational console shell with shared state-badge rendering and consistent empty/feedback patterns**,
So that **every view shares one navigation, vocabulary, and interaction model** _(UX-DR1–4, UX-DR13, UX-DR22, UX-DR23)_.

**Acceptance Criteria:**

**Given** the Projects domain contracts
**When** the Shell composes the console
**Then** generated `RegisterDomain` wiring builds nav groups, and the Project Diagnostic Header (UX-DR13) shows tenant scope, project identity, lifecycle badge, warning count, last-updated timestamp, and mode indicator (`read-only`/`dry-run`/`maintenance`) with copyable IDs.

**Given** the shared `[ProjectionBadge]` vocabulary
**When** any state/reason code renders
**Then** it appears with a text label + accessible name (never color-only), identical to its MCP field / CLI column meaning.

**Given** absent vs denied vs unavailable data (UX-DR23)
**When** a view has no rows
**Then** the empty state distinguishes "no projects/references/audit", "data unavailable", "access denied", and "filter returned no results" — never a blank table.

**Given** an operation outcome (UX-DR22)
**When** feedback is shown
**Then** success/warning/error/fail-closed/loading are distinct, errors use safe reason codes, and no secret/payload is echoed.

### Story 5.4: Project inventory & detail views

As an **administrator**,
I want **a filterable project inventory and a project detail inspector**,
So that **I can move from overview to a single project's metadata, lifecycle, setup, and references** _(UX-DR6, UX-DR7)_.

**Acceptance Criteria:**

**Given** the `ProjectListProjection`
**When** the inventory view renders (default DataGrid)
**Then** it shows tenant scope, lifecycle state, warnings, updated timestamp, and supports filters (tenant, lifecycle, reason code, warning, reference type, timestamp), generated at FrontComposer Level 1.

**Given** the `ProjectDetailProjection`
**When** the detail inspector renders (`DetailRecord`)
**Then** it shows metadata, lifecycle, setup metadata, safe identifiers, and field groups with tabs/sections for metadata/references/resolution/audit/actions.

**Given** tenant isolation
**When** views load
**Then** only the requesting tenant's data is visible (query-side filtered).

### Story 5.5: Reference inventory & health view

As an **administrator**,
I want **a reference health matrix showing each linked conversation, folder, file, and memory with its inclusion/health state and reason code**,
So that **I can diagnose why a reference is stale, unauthorized, unavailable, or invalid** _(UX-DR8, UX-DR14)_.

**Acceptance Criteria:**

**Given** a Project's references
**When** the Reference Health Matrix renders
**Then** each row shows reference type, reference ID, bounded-context owner, inclusion state, health state, reason code, last-checked timestamp, and available safe actions — with explicit grid headers and non-color-only status.

**Given** the metadata-only rule
**When** reference health is shown
**Then** no sibling payload is rendered (only safe metadata + reason codes).

### Story 5.6: Resolution Trace Workbench

As an **administrator**,
I want **a resolution trace view that shows evaluated inputs, candidate projects, reason codes, inclusion/exclusion evidence, and the final outcome**,
So that **I can diagnose why resolution selected, rejected, or could not decide between candidates** _(UX-DR9, UX-DR15)_.

**Acceptance Criteria:**

**Given** a resolution case
**When** the Resolution Trace renders
**Then** it shows input summary, candidate list, reason-code badges, inclusion/exclusion evidence, and an outcome panel (`Resolved`/`NoMatch`/`MultipleCandidates`/`Excluded`/`FailedClosed`), with side-by-side candidate comparison.

**Given** the layout exceeds the generated DataGrid/detail body
**When** it is implemented
**Then** the lowest sufficient FrontComposer gradient is used (attempt Level 2 template; escalate to Level 3 slot / Level 4 view only as needed), preserving lifecycle wrapper, authorization, telemetry, and the accessibility contract; the override is registered with its contract version.

**Given** screen-reader users
**When** the trace renders
**Then** trace order and candidate comparisons are semantically readable (headings/labels), not color-only.

### Story 5.7: Audit timeline view & Safe Diagnostic Export

As an **administrator / support agent**,
I want **an audit timeline view and a safe metadata export**,
So that **I can review state-change history and hand off diagnostics without leaking payloads** _(UX-DR10, UX-DR16, UX-DR18)_.

**Acceptance Criteria:**

**Given** the audit projection (5.1)
**When** the Audit Timeline renders (`Timeline`)
**Then** each entry shows timestamp, actor/source surface, operation, previous→new state, affected references, correlation ID, and audit event ID, remains understandable as a list for screen readers, with copyable timestamps/IDs.

**Given** a diagnostic context
**When** Safe Diagnostic Export is invoked
**Then** it produces safe structured metadata with an explicit payload-exclusion guarantee, keyboard-copyable and screen-reader accessible, available via Web copy/download, CLI structured output, and MCP resource.

### Story 5.8: Warnings queue & operational dashboard

As an **administrator**,
I want **a warnings/maintenance queue and a cross-project operational dashboard**,
So that **I can triage projects needing intervention and see overall health** _(UX-DR11, UX-DR12)_.

**Acceptance Criteria:**

**Given** projects/references in attention-needed states
**When** the warnings queue renders (`ActionQueue` with `WhenState` e.g. `Stale,Conflict,InvalidReference`)
**Then** it lists pending items needing intervention with reason codes, kept in sync with the shared enum (unknown member names fail the build, not silently no-match).

**Given** cross-project metrics
**When** the dashboard renders (`Dashboard`/`StatusOverview`)
**Then** it shows aggregated health/status tiles, tenant-scoped.

### Story 5.9: Audit-first maintenance actions

As an **authorized operator**,
I want **to restore, relink, unlink, and re-evaluate via maintenance actions that preview impact and produce audit evidence**,
So that **state-changing operations are explicit, scoped, confirmed, and auditable** _(UX-DR17, UX-DR21, UX-DR24, UX-DR25; archive already exists from Epic 1)_.

**Acceptance Criteria:**

**Given** a maintenance action (restore/relink/unlink/reevaluate)
**When** it is initiated via the Maintenance Action Panel
**Then** the panel shows action name, tenant scope, target identifiers, current state, proposed state, warnings, dry-run result, expected audit event, and a confirmation control, progressing through panel states `Preview`→`DryRunRequired`→`DryRunPassed`/`DryRunBlocked`→`ConfirmationRequired`→`Executing`→`Succeeded`/`Failed`.

**Given** the command lifecycle (UX-DR21)
**When** the action submits
**Then** it follows `Idle→Submitting→Acknowledged(202)→Syncing→Confirmed/Rejected`, a rejection returns `ProblemDetailsPayload` as a domain outcome (not an exception), and the same lifecycle vocabulary appears on the MCP surface.

**Given** form validation (UX-DR24) and audit evidence (UX-DR25)
**When** the action executes
**Then** validation returns field-specific safe errors before any state change, and on success a metadata-only audit event is produced (action, actor/source, tenant, project ID, affected reference IDs, timestamp, correlation ID, result, audit event ID).

**Given** lifecycle/tenant rules
**When** an action is not allowed
**Then** it is denied with a safe reason code and produces no state change.

### Story 5.10: MCP & CLI parity surfaces

As an **MCP-assisted agent / CLI operator**,
I want **MCP resources/tools and CLI commands that expose the same safe operational model as the Web console**,
So that **diagnostics and maintenance are scriptable and agent-safe with no extra capability or payload exposure** _(UX-DR19, UX-DR20)_.

**Acceptance Criteria:**

**Given** the same `[Projection]`/`[Command]` contracts
**When** MCP descriptors are generated
**Then** read-only **resources** (project metadata, references, resolution traces, audit events) are separated from mutating **tools** (archive/restore/relink/unlink/reevaluate); resources return structured safe fields (`projectId`, `tenantId`, state, references, reasonCodes, warnings, audit IDs) **plus** a short safe explanation (never explanation-only); tools require explicit action + target IDs + tenant scope + confirmation contract + validation/dry-run; unknown tools are rejected with suggestions; everything is tenant-aware.

**Given** the CLI
**When** commands run
**Then** the grouping mirrors the model — read-only `list`/`describe`/`inspect`/`trace`/`validate`/`audit`, preview `dry-run`/`preview`, mutating `archive`/`restore`/`relink`/`unlink`/`reevaluate` — with machine-readable JSON output, stable semantic exit codes, redaction-safe output, and no reliance on color for meaning.

### Story 5.11: Cross-surface parity, responsive design & accessibility hardening

As a **quality owner**,
I want **verified cross-surface parity, responsive behavior, WCAG 2.2 AA accessibility, and a new-surface tenant-isolation/leakage pass**,
So that **the three surfaces tell the same truth, work across viewports, are accessible, and never leak across the newly-added rendering paths** _(UX-DR26, UX-DR27, UX-DR28; NFR-8; NFR-2/NFR-1 over Epic-5 surfaces)_.

**Acceptance Criteria:**

**Given** the same project/resolution case (NFR-8, UX-DR28)
**When** rendered on Web, MCP, and CLI
**Then** all three expose equivalent state names, reason codes, timestamps, warnings, and audit identifiers (asserted by shared enums + schema-fingerprint tests), and stable component keys/`data-testid` exist for automation.

**Given** responsive targets (UX-DR26)
**When** views render at mobile/tablet/desktop/wide (320–767/768–1023/1024+/1440+)
**Then** desktop is full-featured, tablet collapses columns/nav, mobile prioritizes identity/tenant/lifecycle/warnings/top reason codes; critical metadata, warnings, reason codes, and action consequences remain visible at every viewport, and long identifiers are never truncated without accessible full-value access.

**Given** WCAG 2.2 AA (UX-DR27)
**When** accessibility is verified with axe-core/Playwright
**Then** keyboard access, visible focus, semantic headings/landmarks, non-color-only status, sufficient contrast, screen-reader tables/timelines, dialog focus trapping, reduced-motion safety, and no hover-only critical actions all pass; CLI avoids color reliance with JSON mode; MCP returns structured fields + short explanations with stable schemas.

**Given** the new Web/MCP/CLI rendering surfaces
**When** the Epic-5 security pass runs
**Then** cross-tenant negative tests prove no surface renders another tenant's data, and the `NoPayloadLeakage` harness is extended over every new surface/DTO/evidence artifact (no sibling payload rendered "for debugging").

### Story 5.12: Live AppHost operational-console verification

As a **platform test engineer**,
I want **the AppHost browser endpoint provisioned and the deferred operational-console Playwright cases executable through an explicit live lane**,
So that **Epic 5 accessibility, responsive, keyboard, security, and cross-surface behavior has recorded live-topology evidence or a reproducible route blocker**.

**Acceptance Criteria:**

**Given** the umbrella workspace with root-declared sibling repositories
**When** the Projects AppHost starts
**Then** it uses root-declared submodules only, aligns its AppHost SDK with the shared Aspire hosting version, and never requires nested submodule initialization.

**Given** a running Projects AppHost
**When** the operator waits for `projects-ui` and inspects the resource graph through Aspire
**Then** the `projects-ui`, `projects`, and `security` endpoints are discovered dynamically without guessed ports
**And** the topology is stopped through Aspire after verification.

**Given** the Playwright no-AppHost lane
**When** `E2E_LIVE_APPHOST` is not enabled
**Then** selector/accessibility fixture-contract tests remain runnable
**And** live tests skip before authentication or seeded-project fixtures resolve.

**Given** valid discovered UI/API/security endpoints and local real-Keycloak credentials
**When** `E2E_LIVE_APPHOST=1` runs the Chromium lane
**Then** the 13 focused Epic 5 live cases execute first
**And** every applicable remaining AppHost-backed case across the 13 product specifications executes with recorded pass/fail/skip totals.

**Given** a route, deterministic fixture, or product prerequisite remains unavailable
**When** verification closes
**Then** every retained skip has a concrete reason, no unexplained permanent `test.fixme` remains, and the exact metadata-only blocker is recorded without tokens, credentials, payloads, or private paths.

---

## Approved Corrective Epic Addendum (2026-07-14)

This addendum materializes the corrective epic and story inventory approved by Jerome in
`sprint-change-proposal-2026-07-14-implementation-readiness-correction.md`. Completed Epics 1–5
remain implementation history. The required corrective sequence is Epic 6, then Epic 7, then
Epic 8, followed by release handoff. Production release, consequential autonomous MCP operations,
and proposal-confirmation enablement remain blocked until Story 8.9 passes.

The approved proposal is the scope authority for these corrective stories. Dedicated story files
must refine acceptance criteria without weakening its entry, verification, compatibility,
security, durability, or release gates.

## Epic 6: Supported Platform Boundary and Secure Identity

Migrate Hexalith.Projects onto supported EventStore DomainService and platform-owned hosting seams,
restore clean contract and presentation boundaries, enforce real caller and service identity, and
execute a compatibility-controlled cutover that preserves existing event history.

**Completion gate:** Closes ARCH-001, ARCH-002, SEC-001, CLIENT-001, ID-001, and API-001.

### Story 6.1: Pin platform capabilities and migration baseline

Inventory and pin the supported platform seams, versions, routes, public APIs, state keys, events,
cursors, consumers, and migration constraints before implementation changes begin.

### Story 6.2: Restore Contracts, presentation, identity, and API boundaries

Keep stable domain and wire contracts infrastructure-free, move presentation descriptors to their
approved adapter boundary, align identifiers with platform ULIDs while retaining legacy reads, and
mechanically align canonical command/query contracts with OpenAPI and generated consumers.

### Story 6.3: Enforce secure platform admission and authorization evidence

Require complete production JWT and service-identity configuration, derive tenant and actor
authority server-side, and prevent development authorization stubs from resolving outside explicit
development or test hosts.

### Story 6.4: Migrate read models and queries to DomainService

Move projections and queries to the supported asynchronous projection handlers, read-model stores,
query handlers, cursor scopes, and deterministic replay comparison provided by DomainService.

### Story 6.5: Migrate command hosting and platform topology

Move command hosting, persistence, publication, subscriptions, health, telemetry, Dapr components,
and distributed topology to their approved EventStore and platform owners.

### Story 6.6: Authenticate FrontComposer UI and CLI consumers

Use platform credential providers and authenticated runtime composition for FrontComposer Web and
CLI consumers so no client supplies authoritative tenant or actor identity.

### Story 6.7: Execute compatibility cutover and retire legacy runtime

Cut reads and commands to the supported platform paths, preserve routing rollback until all gates
pass, and retire Projects-owned legacy runtime and topology plumbing without rewriting event
history or unsafe dual writes.

---

## Epic 7: Durable Cross-Context and Agent-Safe Workflows

Replace unsafe multi-step and confirmation behavior with durable, restart-safe workflows and
server-bound task evidence while preserving bounded-context ownership and metadata-only audit.

**Completion gate:** Closes REL-001 and AGENT-001 and provides restart-safe evidence for FR-1,
FR-6 through FR-8, FR-14, FR-15, FR-21, and FR-23.

### Story 7.1: Provide shared durable workflow, task, and confirmation seams

Establish the shared platform workflow, task, and confirmation capabilities needed by
Projects-specific durable transitions.

### Story 7.2: Bind server-issued previews and confirmations

Issue expiring, single-use confirmation artifacts bound to tenant, actor, action, targets, request
hash, state or version, and preview, invalidating them when state changes.

### Story 7.3: Enforce the mandatory Folder through durable Project creation

Make Project creation a durable task that verifies or creates exactly one authorized Folder before
the Project can become Active and reconciles partial or lost-response outcomes.

### Story 7.4: Make Conversation assignment and moves durable and auditable

Coordinate Conversations-owned assignment and move operations through a durable Projects workflow
and emit an idempotent, metadata-only Projects audit receipt after confirmed completion.

### Story 7.5: Make proposed-Project confirmation durable and recoverable

Bind proposed-Project confirmation to server-issued evidence and make confirmation, cancellation,
retry, stale-state recovery, and task status durable and restart-safe.

### Story 7.6: Migrate archive, restore, relink, and unlink to bound tasks

Execute consequential maintenance operations through previewed, confirmed, durable tasks; keep
reevaluation read-only as Refresh diagnostics.

### Story 7.7: Reconcile legacy pending-Folder and in-flight records

Detect and reconcile folderless, pending-Folder, and in-flight legacy records through compensating
workflows without rewriting committed event history.

---

## Epic 8: Production Conformance and Release Evidence

Turn the corrective architecture and workflow work into executable production evidence across
persisted boundaries, authentication, tenant isolation, resilience, accessibility, performance,
packaging, deployment, and stakeholder acceptance.

**Completion gate:** Closes TEST-001 and all required P2 compliance and correctness findings, or
records an authorized disposition. Release handoff can proceed only after Story 8.9.

### Story 8.1: Establish real persisted-boundary fixtures and CI

Replace fake or omitted boundary evidence with repeatable persisted-state fixtures and blocking CI
lanes covering the real supported platform path.

### Story 8.2: Implement truthful health, telemetry, and generated logging

Ensure health, readiness, telemetry, and generated structured logging report real dependency and
projection state with bounded, metadata-only signals.

### Story 8.3: Conform MCP and CLI machine contracts

Align MCP and CLI schemas, failure semantics, freshness vocabulary, partial-failure behavior, and
machine-readable outputs with the canonical domain and wire contracts.

### Story 8.4: Rebuild operator UI conformance

Conform the platform-hosted operator UI to approved FrontComposer and Fluent boundaries,
accessibility behavior, safe failure states, and cross-surface semantics.

### Story 8.5: Align build, packaging, supply chain, and source structure

Correct build, packaging, dependency, source-layout, signing, and supply-chain evidence so release
artifacts match the supported ownership and public-surface model.

### Story 8.6: Activate authenticated critical E2E and tenant-isolation gates

Make authenticated live topology, cross-tenant isolation, critical accessibility, and leakage
cases blocking, with no unexplained permanent skips or failed cases accepted as evidence.

### Story 8.7: Prove restart, retry, concurrency, and reconciliation

Demonstrate durable workflow and projection correctness across restart, retry, duplicate delivery,
concurrency, partial failure, lost response, and reconciliation scenarios.

### Story 8.8: Bound read models and verify performance objectives

Define supported cardinality bounds and verify read-model, query, export, and operational-surface
performance at small, median, and maximum supported scales.

### Story 8.9: Record deployment and stakeholder acceptance

Record the deployed version and environment, health and smoke evidence, rollback reference,
residual-risk dispositions, and explicit dated acceptance from Jerome and John before release
handoff.
