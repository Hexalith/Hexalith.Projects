---
stepsCompleted:
  - 1
  - 2
  - 3
  - 4
status: reconciled-2026-07-16
reconciledAgainst:
  - 'final PRD FR-1..FR-24 / NFR-1..NFR-11'
  - 'ARCHITECTURE-SPINE.md AD-1..AD-34'
productionAuthority: 'Epics 6-8 (33 stories: 7/15/11)'
awaitingGate: 'independent implementation-readiness rerun == READY, before sprint-status reconciliation and 6.x/7.x/8.x story-file creation'
releaseBlockedUntil: 'Story 8.11 passes with dated Jerome + John acceptance'
inputDocuments:
  - _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md
  - _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/addendum.md
  - _bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md
  - _bmad-output/planning-artifacts/ux-design-specification.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-16.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-16-implementation-readiness-rerun.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15.md
  - _bmad-output/planning-artifacts/implementation-readiness-report-2026-07-16.md
  - _bmad-output/planning-artifacts/research/domain-eventstore-persistence-for-hexalith-projects-module-data-research-2026-05-24.md
  - _bmad-output/planning-artifacts/research/technical-hexalith-folders-integration-research-2026-05-24.md
  - _bmad-output/planning-artifacts/research/technical-hexalith-projects-referencing-conversations-research-2026-05-24.md
  - _bmad-output/planning-artifacts/research/technical-hexalith-tenants-in-hexalith-projects-tenant-management-isolation-research-2026-05-24.md
  - _bmad-output/planning-artifacts/research/technical-hexalith-memories-rag-research-2026-05-24.md
  - _bmad-output/planning-artifacts/research/technical-frontcomposer-hexalith-projects-web-ux-research-2026-05-24.md
  - _bmad-output/implementation-artifacts/2-1-conversation-reference-read-acl.md
supersededInputs:
  - '_bmad-output/planning-artifacts/architecture.md (superseded 2026-07-16 by ARCHITECTURE-SPINE.md)'
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-14-implementation-readiness-correction.md (23-placeholder addendum, replaced by the 33-story rebaseline)'
backup: '_bmad-output/planning-artifacts/epics.md.pre-reconcile-2026-07-16.bak'
---

# Hexalith.Projects - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for Hexalith.Projects, decomposing the requirements from the PRD, the UX Design Specification, and the Architecture Decision Document into implementable stories.

Hexalith.Projects is a **tenant-aware AI workspace boundary module** built as a domain-centric **EventStore DomainService** with platform-owned runtime and presentation adapters (per `architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md`, AD-1…34). It gives Hexalith.Chatbot a durable project context (metadata, setup, lifecycle, and references to Conversations/Folders/Files/Memories) and exposes one operational model through three surfaces (Web via FrontComposer, MCP, CLI). It is a **metadata control plane** — it references but never owns sibling-context payloads. Following the 2026-07-15/16 rebaseline the module is treated as **brownfield**: Epics 1–5 are completed implementation history, and the corrective Epics 6–8 migrate onto the supported platform via shadow-read-first, single-writer, compatibility-controlled cutover with **no event-history rewrite**.

## Requirements Inventory

### Functional Requirements

_Source: final PRD §6–§8 + addendum (FR-1–FR-24). Each FR has testable consequences in the PRD; abbreviated here._

> **Production authority is Epics 6–8.** Epics 1–5 are completed implementation history and internal evidence, **not** current release authority. Every FR, NFR, P1/P2 finding, external gate, and release case maps to an AC-bearing story in Epics 6–8 (see the Corrective Production Plan) and an identically keyed AD-30 evidence row (`implementation-readiness-traceability-matrix.yaml`). Corrective development, story-file creation, and sprint reconciliation remain frozen until an independent implementation-readiness rerun returns exactly `READY`; production release stays blocked until Story 8.11 passes and Jerome + John record dated terminal acceptance.

**Project Workspace Management**

- **FR-1: Create Project** — Create a Project with tenant context + Project name (only required user input). Description, initial setup, and initial references optional. If no Project Folder supplied, can request creation of a Project Folder with the same name. Sets lifecycle `Active`. Fails closed when tenant context is missing/unauthorized. No payload duplication. (UJ-2, UJ-3)
- **FR-2: Open Project** — Open a Project and receive metadata, lifecycle state, setup, and authorized references needed to initialize a conversation. Returns only references visible to the requesting tenant/user. Archived/unavailable Projects are clearly identified and cannot silently become active context. (UJ-1)
- **FR-3: Update Project Setup** — Update durable Project Setup (project goals, instructions, context preferences, source inclusion/exclusion, conversation-start defaults). Updates are durable, additive, serialization-tolerant; reject raw secrets, unrestricted paths, foreign-context payloads. v1 setup describes conversation behavior/context policy, not model-provider internals. (UJ-1)
- **FR-4: Archive Project** — Archive an Active Project through server **Preview**, single-use **Confirmation Artifact**, and an idempotent **Durable Task** (AD-4/AD-5/AD-13). It stays discoverable for history but is excluded from automatic resolution unless explicitly requested. v1 lifecycle limited to `Active`/`Archived`. Existing references remain auditable after archival. **Restore is the separate FR-23.** _Production owner: Story 7.13._
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

- **FR-19: Validate Project Setup & Classify Metadata** — Validate setup and creation admission before accepting durable work. Reject raw secrets, unrestricted local paths, unsupported reference types, and foreign-context payloads. Require only a Project name on create (the sole v1 name-only compatibility shape); canonical requests also carry a system-supplied **Metadata Classification** — exactly `public_metadata`|`tenant_sensitive`|`credential_sensitive`|`secret` at `projectMetadata.metadataClass`, assigned by authenticated integration policy and never inferred from user text (AD-31/E-9). **Authorization precedes parsing**; one shared `SensitiveMetadataTierValidator` serves both direct create and proposal confirmation; malformed/unknown classification returns `400 ValidationFailure` with `details.rejectedField = projectMetadata.metadataClass` and no echoed value. _Production owner: Story 7.1 (canonical contract cutover supported by Story 6.7)._
- **FR-20: Retrieve Conversation-Start Setup** — Return the subset of setup needed to start/resume a conversation (goals, instructions, context preferences, default linked-source policy). Excludes internal audit metadata and unavailable/unauthorized references; stable enough to use without re-querying every bounded context first.

**Audit and Operations**

- **FR-21: Record Project Audit Events** — Record metadata-only audit events for creation, setup updates, archival, conversation link/move, Folder changes, File/Memory link/unlink, resolution confirmation, and new-Project-from-proposal. Include tenant, Project identity, operation type, timestamp, actor identity where available, and affected reference IDs. Never include payloads/secrets/prompts.
- **FR-22: Support Operator Read Access** — Tenant Operators and Tenant Project Administrators can inspect authorized Project metadata, lifecycle state, references, Durable Task status, confirmed resolution outcomes, and audit metadata. Authorization-gated, tenant-scoped, **metadata-only read**. Read permission alone grants **neither** Safe Diagnostic Export (**now FR-24**) **nor** any mutation. _Production owners: Stories 6.5/6.6 (authenticated read surfaces), 8.1/8.3/8.4/8.5 (operational surfaces)._
- **FR-23: Restore Archived Project** — Restore an Archived Project through Preview, single-use confirmation, and an idempotent Durable Task (the restore counterpart to FR-4; realizes UJ-5). Preview verifies Tenant, actor, authority, current Project version, and **exactly one authorized Project Folder**; if the prior Folder is invalid/missing, Preview requires an authorized replacement or same-name Folder creation before confirmation. The Project stays **Archived until Folder evidence and read-model-confirmed restore completion succeed**; if Folder creation succeeds but activation cannot commit, the task enters `NeedsAttention` and Projects never auto-deletes a Folders-owned resource. Stale/replay/cancel/duplicate/concurrency/lost-response cannot expose an invalid Active Project; outcomes are audited metadata-only. _Production owner: Story 7.14._
- **FR-24: Create Safe Diagnostic Export** — A separately authorized Tenant Operator or Tenant Project Administrator creates a bounded `projects.safe-diagnostic-export.v1` export through Web, CLI, or MCP (**Chatbot cannot**). Export permission is distinct from FR-22 read. The complete encoded export (incl. envelope + truncation metadata) is **≤ 1 MiB, ≤ 500 reference rows, ≤ 100 audit rows**; reference ordering is stable/deterministic, audit rows newest-first with stable tie-breaking; truncation reports included/omitted counts and safe reasons without excluded detail; **no continuation cursor, no retention**; two concurrent exports per Tenant; every attempt and outcome audited metadata-only; upstream unavailability represented safely without raw errors or fabricated completeness. _Production owner: Story 8.2._

### NonFunctional Requirements

_Source: final PRD §7 (Cross-Cutting NFRs) + addendum + §8 (Success Metrics). **NFR-1…11 is the canonical set** and replaces the prior 9-NFR inventory; the earlier concepts (tenant isolation, metadata-only, fail-closed, parity, resolution-over-automation) are preserved but folded into this numbering. Each NFR-1…11 envelope is a binding MVP acceptance criterion; no approved v1 NFR is deferrable from production release. Primary owning story noted per NFR._

- **NFR-1 — Security & privacy:** Every operation is Tenant-/actor-/action-/target-/version-scoped; trust-bearing mutations fail closed on stale or unknown authorization; logs, telemetry, and errors are metadata-only (no transcripts, file contents, memory payloads, prompts, secrets, tokens, full command bodies, or unrestricted paths). Cross-tenant access impossible by construction; verified by adversarial negative tests. _Owner: Story 8.8 (evidence); enforced across all stories._
- **NFR-2 — Encryption & key management:** Authenticated encryption in transit; platform-managed encryption at rest; Projects owns no private keys; KMS rotation/revocation evidence is release-blocking. _Owner: Story 8.11 (supported by 8.6, 8.7)._
- **NFR-3 — Availability & recovery:** 99.9% monthly availability (ex-maintenance); RTO 15 min after process/node failure; accepted Durable Tasks resume or reach truthful `NeedsAttention` within 5 min. _Owner: Story 8.10 (supported by 8.6)._
- **NFR-4 — Durability & idempotency:** RPO 0 for committed events in the primary region; Active Projects are **never folderless**; equivalent retries return the same task, changed requests conflict; no silent drop or duplicate. _Owner: Story 8.10 (durable-workflow correctness across Epic 7)._
- **NFR-5 — Performance & scale:** 10,000 Projects/Tenant, 5,000 references/Project (ex-Folder), 100,000 audit records/Project; metadata reads p95 < 500 ms (at 1,000 Projects / 500 refs) and < 1 s at max scale; task admission p95 < 500 ms warm. _Owner: Story 8.9._
- **NFR-6 — Pagination & export bounds:** Cursor pages default 50, cap 200; Safe Diagnostic Export obeys the FR-24 size/row caps and the per-Tenant limit of two concurrent exports. _Owner: Story 8.9 (paging) + 8.2 (export bounds)._
- **NFR-7 — Back-pressure & dependency control:** Per Tenant 100 reads/s (burst 200), 20 mutation admissions/s (burst 40), 1,000 nonterminal tasks, 2 concurrent exports; interactive timeout 2 s, durable-step 10 s; idempotent retry ≤ 3 in 30 s; overload returns structured retry guidance. _Owner: Story 8.9._
- **NFR-8 — Retention & transient data:** Terminal result + idempotency record retained ≥ 30 days (or result lifetime); Preview/Confirmation Artifacts expire at 15 min; audit metadata retained ≥ 365 days; Resolution Traces and exports are not persisted. _Owner: Stories 8.1 & 8.2 (with Epic 7 task/confirmation stories)._
- **NFR-9 — Accessibility:** Chatbot and operator journeys conform to WCAG 2.2 AA (keyboard, focus, AT announcement, no color/timing reliance, 200% zoom, 320 CSS px reflow); automated + authenticated manual keyboard/screen-reader evidence required. _Owner: Story 8.8 (incl. cross-repository Chatbot companion evidence, AD-34/SM-5)._
- **NFR-10 — Compatibility:** Contracts stay additive/serialization-tolerant unless a breaking change is approved; historical v1 data and unversioned name-only creation remain readable/accepted; retirement requires a major version, migration notice, usage evidence, compatibility tests, and rollback evidence; no event-history rewrite. _Owner: Story 6.7 (read cutover) + 7.15 (legacy reconciliation) + 8.7._
- **NFR-11 — Release evidence:** Authenticated persisted-boundary, cross-Tenant, restart/concurrency, duplicate-delivery, lost-response, accessibility, privacy, performance, deployment, smoke, rollback, and stakeholder-acceptance evidence must pass; a failed critical case or unexplained critical skip blocks release; unavailable environments record "not verified," never `passed`. _Owner: Story 8.11 (terminal gate, AD-30); every Epic 8 story contributes rows._

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

_The current production-authority owner is the AC-bearing story in **Epics 6–8**. The "Historical impl." column is completed Epic 1–5 implementation history/evidence, **not** release authority. Each row also maps to an identically keyed `hexalith.readiness-evidence.v1` matrix row and applicable Architecture Decisions (AD-1…AD-34)._

#### Functional Requirements

| FR | Capability | Production owner (Epics 6–8) | Historical impl. | Key ADs |
| --- | --- | --- | --- | --- |
| FR-1 | Create Project (Folder-first, idempotent) | **7.1** | 1.4 | AD-3, AD-5, AD-8, AD-12, AD-22 |
| FR-2 | Open Project | **6.1** | 1.7 | AD-3, AD-14, AD-19, AD-32 |
| FR-3 | Update Project Setup | **7.2** | 1.8 | AD-5, AD-15, AD-16 |
| FR-4 | Archive Project | **7.13** | 1.8 | AD-4, AD-5, AD-13 |
| FR-5 | List Projects | **6.1** | 1.7 | AD-14, AD-19, AD-20, AD-27 |
| FR-6 | Link Conversation | **7.3** | 2.3 | AD-10, AD-12 |
| FR-7 | Move Conversation | **7.4** | 2.3 | AD-5, AD-10, AD-12, AD-13 |
| FR-8 | Set / Replace Project Folder | **7.6** (initial via 7.1) | 2.4 | AD-3, AD-11, AD-12 |
| FR-9 | Link File Reference | **7.7** | 2.5 | AD-11, AD-15 |
| FR-10 | Link Memory | **7.9** | 2.7 | AD-11, AD-15 |
| FR-11 | Unlink Context Reference | **7.5** / **7.8** / **7.10** | 2.3/2.5/2.7 | AD-5, AD-11, AD-13 |
| FR-12 | Resolve From Conversation | **6.4** | 4.2 | AD-7, AD-10, AD-14, AD-32 |
| FR-13 | Resolve From Attachments | **6.4** | 4.3 | AD-7, AD-11, AD-14, AD-32 |
| FR-14 | Confirm Ambiguous Project | **7.11** (candidates read 6.4) | 4.4 | AD-5, AD-13, AD-32 |
| FR-15 | Propose New Project | **7.12** | 4.5 | AD-5, AD-8, AD-13, AD-31 |
| FR-16 | Get Project Context | **6.3** | 3.2 | AD-11, AD-14, AD-32 |
| FR-17 | Explain Context Selection | **6.3** / **6.4** | 3.3 | AD-7, AD-14 |
| FR-18 | Refresh Project Context (read-only) | **6.3** | 3.4 | AD-7, AD-14, AD-32 |
| FR-19 | Validate Setup & Metadata Classification | **7.1** (contract cutover 6.7) | 1.4/1.8 | AD-31, AD-16 |
| FR-20 | Retrieve Conversation-Start Setup | **6.2** | 3.5 | AD-14, AD-32 |
| FR-21 | Record Project Audit Events | **8.1** | 5.1 | AD-21, AD-26, AD-30 |
| FR-22 | Support Operator Read Access | **6.5** / **6.6** + 8.1/8.3/8.4/8.5 | 5.2 | AD-2, AD-19, AD-20, AD-29 |
| FR-23 | Restore Archived Project | **7.14** | — (new) | AD-3, AD-13, AD-23 |
| FR-24 | Create Safe Diagnostic Export | **8.2** | — (new) | AD-7, AD-19, AD-21, AD-26 |

#### Non-Functional Requirements

| NFR | Concern | Primary owner | Supporting | Key ADs |
| --- | --- | --- | --- | --- |
| NFR-1 | Security & privacy | 8.8 | all stories | AD-11, AD-13, AD-20 |
| NFR-2 | Encryption & KMS | 8.11 | 8.6, 8.7 | AD-28 |
| NFR-3 | Availability & recovery | 8.10 | 8.6 | AD-4, AD-9, AD-28 |
| NFR-4 | Durability & idempotency | 8.10 | Epic 7 | AD-4, AD-12 |
| NFR-5 | Performance & scale | 8.9 | 6.7 | AD-14, AD-15, AD-27 |
| NFR-6 | Pagination & export bounds | 8.9 | 8.2 | AD-19, AD-21, AD-27 |
| NFR-7 | Back-pressure & dependency control | 8.9 | — | AD-27 |
| NFR-8 | Retention & transient data | 8.1, 8.2 | Epic 7 | AD-4, AD-5, AD-21, AD-26 |
| NFR-9 | Accessibility (incl. Chatbot companion) | 8.8 | 6.5, 8.3 | AD-34, AD-29 |
| NFR-10 | Compatibility | 6.7 | 7.15, 8.7 | AD-6, AD-16, AD-17, AD-22 |
| NFR-11 | Release evidence | 8.11 | all Epic 8 | AD-25, AD-28, AD-30 |

_All 24 FRs and 11 NFRs have an AC-bearing production owner in Epics 6–8. The nine P1 and seven P2 audit findings and every critical release-evidence category map to the same stories via the AD-30 evidence matrix (one row per stable ID). Historical Additional Requirements (AR-\*) and UX-DR\* remain distributed across Epics 1–5 as documented; upstream gaps AR-G1–G4 are now subsumed by the pinned sibling-owner entry gates (G-2) in the Epic 6/7 entry gates._

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

### Epics 6–8: Corrective Production Authority

The 2026-07-15/16 rebaseline makes **Epics 6–8 the sole future schedulable plan and
production-release authority**. Epics 1–5 above are completed implementation history — their
**"Standalone: Yes"** labels mean feature-complete in isolation as evidence, **not**
production-release authority (production-correct FR-1, FR-4/FR-23, FR-14/FR-15, FR-19, and FR-21
behavior completes in Epic 7, and all release evidence in Epic 8).

- **Epic 6 — Authorized Project Reads on the Supported Platform** (7 stories, 6.1–6.7): authorization-filtered list/open/context/resolution reads over supported DomainService read models + authenticated Web/CLI read surfaces + shadow-read cutover.
- **Epic 7 — Durable Project Decisions and Cross-Context Recovery** (15 stories, 7.1–7.15): every consequential write as a durable, restart-safe, confirmation-bound workflow — incl. Folder-first create (7.1), archive (7.13), restore/FR-23 (7.14), and legacy reconciliation (7.15).
- **Epic 8 — Safe Operations and Release Confidence** (11 stories, 8.1–8.11): operator surfaces, Safe Diagnostic Export/FR-24 (8.2), health/telemetry, packaging/supply chain, authenticated parity/isolation/accessibility incl. the Chatbot companion (8.8), performance/back-pressure, cross-workflow resilience, and the terminal deployment/acceptance gate (8.11).

Full AC-bearing detail is in the **Corrective Production Plan (Epics 6–8)** section below. Corrective
development, `6.x/7.x/8.x` story-file creation, and sprint reconciliation remain frozen until an
independent readiness rerun returns exactly `READY`.

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

> **Superseded for production (2026-07-16).** This tracer-bullet criteria must **not** authorize caller-visible or Active **folderless** creation. Production create authority is **Story 7.1** (Folder-first, idempotent, metadata-classified; no folderless-Active window). Retained as implementation history only.

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

> **Superseded for production (2026-07-16).** This must **not** degrade a Folder-creation failure into an Active **folderless** Project. Production Folder authority is **Story 7.1** (initial Folder-first create) and **Story 7.6** (replace); legacy pending-Folder / folderless records are reconciled by **Story 7.15**. Retained as implementation history only.

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

> **Superseded for production (2026-07-16).** Execution counts do **not** establish acceptance when critical cases fail — executed-but-failing evidence is **not** acceptance (NFR-11/AD-30). Authenticated critical E2E is owned by **Story 8.8** and terminal release acceptance by **Story 8.11**. Retained as implementation history only.

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

## Corrective Production Plan (Epics 6–8) — rebaselined 2026-07-16

This section is the **sole future schedulable plan** and the production-authority backlog for
Hexalith.Projects. It **replaces the 23-placeholder corrective addendum (2026-07-14) atomically**
with the approved **33-story outcome inventory** — 7 in Epic 6, 15 in Epic 7, 11 in Epic 8 — per
`sprint-change-proposal-2026-07-16.md` and
`sprint-change-proposal-2026-07-16-implementation-readiness-rerun.md`, reconciled to the final PRD
(FR-1…24 / NFR-1…11) and `architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md`
(AD-1…34). The prior placeholder inventory is retained only as findings history in
`epics.md.pre-reconcile-2026-07-16.bak`.

**Corrective order is Epic 6 → Epic 7 → Epic 8.** Epics 1–5 remain completed implementation history
and evidence, **not** release authority.

**Containment (binding).** Authoring this planning layer is the authorized step (this is route step 4,
`bmad-create-epics-and-stories`, of the approved SCP); it does **not** lift the freeze. No corrective
implementation, no `6.x/7.x/8.x` story-file creation, and no `sprint-status.yaml` reconciliation occur
until an **independent implementation-readiness rerun returns exactly `READY`**. Production release,
consequential autonomous MCP mutation, and proposed-Project confirmation remain blocked until
**Story 8.11** passes with dated terminal acceptance from **Jerome and John**. No
failed/skipped/blocked/unavailable critical evidence may be represented as passing; no event history
is rewritten; no unsafe dual writer is introduced; no sibling repository is changed without separate
repository-local authorization and validation.

**Story completion contract (SCP-07-16-rerun §4.5).** Every story below carries: a named beneficiary +
observable outcome; FR/NFR/AD/UX-journey/finding/release traceability; repository authority, named
owner, pinned prerequisite revision, and entry gate; prior-only dependencies; positive +
denial/cross-Tenant + stale/unavailable + duplicate/replay/restart/concurrency/lost-response +
compatibility/rollback acceptance criteria where applicable; a deterministic environment/fixture; an
exact verification command; an expected pass/fail evidence artifact + release disposition; an
estimate; and a completion boundary. No unresolved `TBD`, placeholder, missing external approval,
unexplained critical skip, or false `passed` state is admissible. Roles: **Product Owner** authors,
**Solution Architect** verifies AD/G-gate conformance, **Test Architect** owns evidence rows/fixtures,
**Chatbot Presentation Owner** owns the NFR-9/SM-5 companion evidence, **Jerome + John** own release.
Estimates are relative (S/M/L/XL) and are not schedule commitments until readiness passes.

**External entry gates (G-1…G-6)** are prerequisites, not delivered value: **G-1** platform Durable
Task/Confirmation engine · **G-2** sibling owner contracts (expected-version, idempotency,
receipt/status query, batch-read, compensation) · **G-3** FrontComposer adapters (reconcile package
4.0.0 vs checked-out 4.0.1 + prove descriptor/schema/credential/MCP parity) · **G-4** platform
composition runner + evidence tool · **G-5** identity/KMS/secrets/telemetry bindings · **G-6**
runtime/toolchain alignment (Dapr runtime↔SDK tuple, Fluent UI RC, CommunityToolkit preview,
NSubstitute RC, Fluxor 6.9 governance).

**Canonical evidence.** The AD-30 artifact is
`implementation-readiness-traceability-matrix.yaml` (schema `hexalith.readiness-evidence.v1`) with a
matching Markdown view, authored/reconciled alongside these stories and gated by
`dotnet tool run hexalith-evidence validate …` — a **target** gate; rows truthfully record the
external blocker and may not be marked `passed` until the Builds/platform owner supplies the
G-4 capability. Required row coverage: FR-1…24, NFR-1…11, all nine P1 and seven P2 audit findings,
and every critical NFR-11/AD-30 release category.

## Epic 6: Authorized Project Reads on the Supported Platform

Deliver authorization-filtered list/open/context/resolution **reads** over named incremental
EventStore DomainService read models and the rebuildable Reference Trust Index, exposed through
authenticated FrontComposer and CLI read surfaces, and cut supported reads over shadow-read-first
with reversible routing. This is the read/query side of the vertical slice; no consequential writes
(those are Epic 7). It closes the read-side of ARCH-001/ARCH-002/API-001/CLIENT-001/ID-001 and
establishes the identity, contract, and platform boundary every later epic depends on.

**Epic 6 entry gate (prerequisite, not delivered value).** Every read story is blocked until its
applicable external capabilities have an owner-approved repository-local revision, reproducible
evidence, and rollback pin. The gate is an explicit dependency ledger rather than implementation
work assigned to the first value story. The following applicability rules preserve every G-1…G-6
obligation without making unrelated later-surface gates prerequisites for Story 6.1:

- Story 6.1 and later consumers of the shared read baseline require accepted work packages 6.1-P0
  through 6.1-P4 below.
- Stories consuming Conversations, Folders, Memories, Parties, or Tenants additionally require the
  applicable G-2 sibling-owner read contracts.
- Story 6.5 additionally requires the approved G-3 FrontComposer contract; Story 6.6 requires its
  approved CLI adapter contract.
- G-6 applies before any affected build, runner, or evidence lane is claimed passing.

**Story 6.1 prerequisite work packages.** These are enablement packages, not user-value stories and
not Story 6.1 implementation subtasks. The approved routing and complete acceptance contracts are in
`sprint-change-proposal-2026-07-17.md`.

| ID | Repository authority | Required outcome | Accountable owners | Initial state |
|---|---|---|---|---|
| 6.1-P0 | Builds/platform tooling | Supported G-4 persisted runner and machine-checkable evidence tool | Builds Owner + Platform Owner + Test Architect | open; target uncommitted |
| 6.1-P1 | EventStore + Builds + architecture record | One owner-approved source/package/architecture/runner baseline and finite normalization record | EventStore Owner + Builds Owner + Solution Architect | open; target uncommitted |
| 6.1-P2 | EventStore/platform | Supported dual-principal query envelope, indistinguishable safe denial, and authoritative global-position watermark | EventStore Owner + Identity/Security Owner + Solution Architect | open; blocked by P1 |
| 6.1-P3 | Identity/security platform | Approved mandatory fail-closed production identity/authentication contract and fixtures | Identity/Security Owner + Projects Owner + Solution Architect | open; blocked by P2 |
| 6.1-P4 | Hexalith.Projects planning/evidence | Owner-approved 6.1 gate record linking P0-P3 pins, commands, evidence, normalization, and rollback | Product Owner + Solution Architect + Test Architect + P0-P3 owners | open; blocked by P0-P3 |

_All Epic 6 stories share: **Repository authority** Hexalith.Projects (`Contracts`, read models,
query handlers) with platform-generated read adapters; **owner** Product Owner (author) + Solution
Architect (AD/G conformance) + Test Architect (evidence); **prior-only dependencies** — each story's
applicable accepted entry-gate ledger items, never Epic 7/8; **compatibility/rollback** — legacy read route retained and reversible
until Story 6.7 cutover, no event-history rewrite; **fixture** — deterministic authorized-Tenant
persisted-boundary fixture on the G-4 runner; **release disposition** — read evidence contributes to
NFR-11 but no story here is a release gate._

### Story 6.1: List and open Projects through supported authenticated paths

As a **Tenant Operator or delegated Chatbot service caller**,
I want **to list visible Projects and open one Project's authorized metadata, lifecycle, setup summary, and reference summary through the supported DomainService read models**,
So that **operators and Chatbot get current, authorization-filtered Project truth to initialize a Conversation (FR-2, FR-5) with no legacy runtime**.

- **Traceability:** FR-2, FR-5; NFR-1, NFR-5, NFR-10; AD-3, AD-14, AD-19, AD-20, AD-32, AD-33; UJ-1; findings ARCH-001/API-001 (read side); evidence rows `fr-2`, `fr-5`.
- **Implementation state:** blocked by 6.1-P0, 6.1-P1, 6.1-P2, 6.1-P3, and 6.1-P4. It returns to `ready-for-dev` only after P4 is accepted and the Story 6.1 specification passes the complete ready-for-development standard.

**Acceptance Criteria:**

**Given** an authenticated caller with valid Tenant + actor authority and current read models, **When** `ListProjects`/`GetProject` runs via `IDomainQueryHandler` with an opaque `QueryCursorScope`, **Then** results are Tenant-scoped and authorization-filtered, carry the AD-32 snapshot (`responseState`, `asOf`, `projectVersion`, `components`, `recoveryActions`), page at default 50 / cap 200, and never select a resolution candidate.

**Given** a denied, cross-Tenant, or nonexistent Project, **When** the read runs, **Then** it fails closed and denial is **indistinguishable from nonexistence** (safe `404`), leaking no existence or protected metadata.

**Given** stale/rebuilding/unavailable read models, **When** the read runs, **Then** it returns `Partial`/`Unavailable` with honest component evidence rather than fabricated completeness, and `Unavailable` blocks context use.

**Given** the shadow-read comparison harness, **When** the same query replays against legacy and supported read models, **Then** output, keys, watermarks, cursors, and Tenant isolation are deterministically equivalent before read routing switches.

- **Verification:** `dotnet tool run hexalith-module test --profile reads --filter Story=6.1`. **Evidence:** `evidence/epic6/6.1-authorized-reads.{trx,json}` + shadow-read equivalence report. **Estimate:** M. **Completion boundary:** authorized list/open served from supported read models with equivalence evidence; no write behavior.

### Story 6.2: Retrieve Conversation-start setup with admission truth

As a **delegated Chatbot service caller**,
I want **to retrieve the bounded Conversation-start subset of Project Setup for an Active Project**,
So that **Chatbot can start or resume a Conversation from durable setup truth (FR-20) without re-querying every bounded context**.

- **Traceability:** FR-20; NFR-1, NFR-5; AD-3, AD-14, AD-19, AD-32; UJ-1; evidence row `fr-20`.

**Acceptance Criteria:**

**Given** an authorized Active Project, **When** `GetConversationStartSetup` runs, **Then** it returns only the start subset (goals, instructions, context preferences, default linked-source policy), excludes internal audit metadata and unauthorized/unavailable references, and carries the AD-32 snapshot.

**Given** an Archived or unauthorized Project, **When** the read runs, **Then** it fails closed (safe `404`) and returns no setup.

**Given** stale/unavailable setup projection, **When** the read runs, **Then** `Partial`/`Unavailable` is returned honestly and `Unavailable` blocks first-response admission.

- **Verification:** `dotnet tool run hexalith-module test --profile reads --filter Story=6.2`. **Evidence:** `evidence/epic6/6.2-conversation-start-setup.{trx,json}`. **Estimate:** S. **Completion boundary:** start-setup subset served with admission-state truth; no mutation.

### Story 6.3: Retrieve assembled Project Context through supported read models

As a **delegated Chatbot service caller**,
I want **to retrieve the allowlist-assembled Project Context (setup + included references with exclusion reasons), a read-only refresh, and a per-reference inclusion/exclusion explanation**,
So that **Chatbot grounds a Conversation in current, authorized, metadata-only context (FR-16, FR-17, FR-18)**.

- **Traceability:** FR-16, FR-17, FR-18; NFR-1, NFR-5, NFR-8; AD-7, AD-11, AD-14, AD-32; UJ-1, UJ-4; evidence rows `fr-16`, `fr-17`, `fr-18`.

**Acceptance Criteria:**

**Given** an authorized Active Project and the Reference Trust Index, **When** `GetProjectContext`/`RefreshProjectContext` runs, **Then** a reference is included only after Tenant + project + lifecycle + authorization + freshness all pass; exclusions carry a shared-vocabulary state + reason code; the result is metadata-only and carries the AD-32 snapshot; refresh is a **read-only recompute** that never mutates.

**Given** `ExplainContextSelection`, **When** it runs, **Then** it returns current transient inclusion/exclusion evidence with no secrets/payloads and no persisted trace identity.

**Given** an unauthorized/denied reference or stale index, **When** assembly runs, **Then** the reference is excluded fail-closed-clean with a reason code, never silently dropped, and index staleness surfaces as `Partial`.

- **Verification:** `dotnet tool run hexalith-module test --profile reads --filter Story=6.3`. **Evidence:** `evidence/epic6/6.3-project-context.{trx,json}` + `NoPayloadLeakage` scan. **Estimate:** L. **Completion boundary:** assembled context + refresh + explanation served read-only with allowlist + leakage evidence.

### Story 6.4: Resolve Projects with transient current explanations

As a **delegated Chatbot service caller**,
I want **to resolve Candidate Projects from a Conversation's metadata and from attached Folder/File references, with a request-scoped, current-only explanation**,
So that **Chatbot can identify the right Project (FR-12, FR-13) without persisted inference history and without silently attaching**.

- **Traceability:** FR-12, FR-13, FR-17 (transient trace); NFR-1, NFR-8; AD-7, AD-10, AD-11, AD-14, AD-32; UJ-3; evidence rows `fr-12`, `fr-13`.

**Acceptance Criteria:**

**Given** a Conversation with no explicit Project, **When** `ResolveProjectFromConversation`/`ResolveProjectFromAttachments` runs, **Then** it returns `NoMatch`/`SingleCandidate`/`MultipleCandidates` with reason codes, excludes Archived unless explicitly requested, never accesses unauthorized resources, and returns a request-scoped Resolution Trace that is **not persisted** (AD-7).

**Given** missing/stale authorization on a candidate or attachment, **When** resolution runs, **Then** it fails closed on that candidate and never treats raw file content as Project data.

**Given** a `MultipleCandidates`/`SingleCandidate` outcome, **When** the response returns, **Then** it **selects nothing** — confirmation is the durable Epic 7 step (7.11/7.12).

- **Verification:** `dotnet tool run hexalith-module test --profile reads --filter Story=6.4`. **Evidence:** `evidence/epic6/6.4-resolution-reads.{trx,json}` (asserts no persisted trace). **Estimate:** L. **Completion boundary:** compute-on-demand resolution + transient explanation; no persistence, no attachment.

### Story 6.5: Inspect Projects through an authenticated FrontComposer read surface

As a **Tenant Operator or Tenant Project Administrator**,
I want **to inspect Project inventory, detail, reference health, resolution traces, and audit timeline through an authenticated FrontComposer Web read surface**,
So that **operators get authorized, metadata-only Project truth (FR-22) with WCAG 2.2 AA behavior and no client-supplied identity**.

- **Traceability:** FR-22; NFR-1, NFR-9; AD-2, AD-19, AD-20, AD-29, AD-33, AD-34; UX-DR6–UX-DR16, UX-DR27; evidence row `fr-22-web`.

**Acceptance Criteria:**

**Given** a platform-authenticated operator session (credentials from the platform provider, never client-supplied), **When** the Web read surface renders inventory/detail/health/trace/audit, **Then** every view is Tenant-scoped, authorization-filtered, metadata-only, shows AD-32 response/recovery fields, and enforces role-specific action visibility (reads only here).

**Given** a denied or cross-Tenant target, **When** a view loads, **Then** it fails closed with a safe empty/denied state indistinguishable from absence and renders no sibling payload "for debugging."

**Given** keyboard/screen-reader/200%-zoom/320px navigation, **When** operator read journeys run, **Then** automated axe + authenticated manual evidence records WCAG 2.2 AA conformance (blocking on unresolved critical/serious).

- **Verification:** `dotnet tool run hexalith-module test --profile web-reads --filter Story=6.5` (+ axe/Playwright). **Evidence:** `evidence/epic6/6.5-web-read-surface.{trx,json}` + a11y report. **Estimate:** L. **Completion boundary:** authenticated read-only operator Web surface with a11y evidence; maintenance actions are Epic 8.

### Story 6.6: Inspect Projects through an authenticated CLI read surface

As a **Tenant Operator or automation caller**,
I want **scriptable, authenticated read-only CLI commands (`list`/`describe`/`inspect`/`trace`/`validate`/`audit`) with deterministic machine output**,
So that **operators and pipelines get authorized, metadata-only Project truth (FR-22) with stable exit codes and no color-dependent meaning**.

- **Traceability:** FR-22; NFR-1, NFR-8; AD-2, AD-19, AD-20, AD-29, AD-33; UX-DR19; evidence row `fr-22-cli`.

**Acceptance Criteria:**

**Given** an authenticated CLI invocation with platform-provided identity, **When** a read command runs, **Then** output is deterministic machine-readable JSON, Tenant-scoped, authorization-filtered, metadata-only, with stable exit codes and no reliance on color.

**Given** a denied/nonexistent target or stale read model, **When** a read command runs, **Then** it fails closed with a safe reason code and a non-zero stable exit code, indistinguishable denial/absence, no payload echo.

**Given** identical facts, **When** compared to the Web surface (6.5), **Then** lifecycle/reference states, reason codes, timestamps, warnings, and audit identifiers are semantically equivalent (parity).

- **Verification:** `dotnet tool run hexalith-module test --profile cli-reads --filter Story=6.6`. **Evidence:** `evidence/epic6/6.6-cli-read-surface.{trx,json}`. **Estimate:** M. **Completion boundary:** authenticated read-only CLI with parity + stable contract; mutating CLI is Epic 8.

### Story 6.7: Cut over supported reads while preserving compatibility and rollback

As a **Solution Architect / platform operator**,
I want **to switch read routing from legacy to supported read models after a deterministic equivalence gate, keeping routing reversible and canonical contracts aligned**,
So that **supported reads become authoritative without event-history rewrite, and the FR-19 canonical contract lands for Epic 7 (NFR-10)**.

- **Traceability:** FR-19 (contract cutover support), FR-2/5/16/20 (routing); NFR-10; **AD-1 (DomainService runtime boundary — legacy runtime read plumbing retired here)**, AD-6, AD-16, AD-17, AD-18, AD-22, AD-24, AD-25, AD-31 (contract shape); findings ARCH-001/CLIENT-001/ID-001/API-001; evidence row `nfr-10-reads`.

**Acceptance Criteria:**

**Given** the shadow-read equivalence gate across all Epic 6 reads, **When** it passes (output/keys/watermarks/cursors/Tenant isolation equivalent) and the ULID identity + OpenAPI/generated-consumer contracts are mechanically aligned, **When** cutover runs, **Then** read routing switches to supported models and remains **reversible** with legacy retained until Epic 8 release acceptance.

**Given** legacy identifiers and historical events (incl. `ProjectFolderCreationPending`), **When** reads run post-cutover, **Then** they remain readable (foreign IDs opaque, no GUID-parse) and no history is rewritten and no unsafe dual writer is introduced.

**Given** an equivalence-gate failure or post-cutover regression, **When** detected, **Then** routing rolls back deterministically and the blocker is recorded honestly.

- **Verification:** `dotnet tool run hexalith-module test --profile read-cutover --filter Story=6.7` + `hexalith-evidence validate` row `nfr-10-reads`. **Evidence:** `evidence/epic6/6.7-read-cutover.{json}` + rollback drill log. **Estimate:** L. **Completion boundary:** supported reads authoritative and reversible; legacy runtime read plumbing retired only after gates pass; command cutover is Epic 7.

---

## Epic 7: Durable Project Decisions and Cross-Context Recovery

Deliver every consequential Project **write** as a Projects-owned, versioned **workflow definition**
running on platform **Durable Tasks** — create, setup, association linking/unlinking, Folder
replacement, ambiguous/proposed confirmation, archive, restore, and legacy reconciliation. Durable
task state (not acknowledgements or notifications) is the only completion truth. It closes REL-001
and AGENT-001 and provides restart-safe evidence for FR-1, FR-3, FR-4, FR-6–FR-11, FR-14, FR-15,
FR-19, FR-21, and FR-23.

**Epic 7 entry gate (prerequisite, not delivered value).** Requires **G-1** (approved, pinned
platform Durable Task engine + Confirmation Artifact record: task IDs, admission, leases,
checkpoints, receipts, retries, cancellation, recovery, retention — AD-4/AD-9/AD-13) and **G-2**
(pinned sibling owner contracts for Conversations/Folders/Memories with expected-version,
idempotency key, receipt/status query, batch-read, and compensation — AD-12). Preserves immutable
event history and single-writer cutover (Epic 6 read cutover complete; command cutover lands here).

**Shared durable-workflow invariants (apply to every Epic 7 story; each story adds its specifics).**

1. **Task truth (AD-4/AD-9):** transitions follow `Pending → Running`, with `WaitingForDependency`
   and `NeedsAttention` recoverable and `Succeeded`/`Rejected`/`Failed`/`Cancelled` terminal and
   immutable; one transition authority; cancel only before the irreversible checkpoint; `202` and
   SignalR are non-completion signals.
2. **Confirmation-required admission (AD-5/AD-13):** archive, restore, Conversation move, Folder
   replace, unlink, ambiguous-resolution confirm, and proposed-creation confirm require a **15-minute,
   opaque, single-use Confirmation Artifact** bound to Tenant/actor/action/targets/request-hash/
   Preview/current-versions; validation + single-use + task-admission are atomic; any
   replay/alteration/stale/expiry/mismatch/changed-request fails closed with `409` + `RenewPreview`.
   Additive links, initial Folder, Setup, and direct create are **task-only** (no confirmation).
3. **Idempotency (AD-5):** scope `(Tenant, actor, operation, key)`; equivalent retry returns the same
   task; changed requests conflict; retained ≥ 30 days / result lifetime.
4. **Forward-recovery saga (AD-12):** deterministic idempotency key + expected owner version; persist
   the owner receipt **before** advancing; query authoritative owner status before retrying an
   unknown response; compensate with an explicit idempotent owner command or enter `NeedsAttention`;
   Projects never auto-deletes an owner (Folders/Conversations/Memories) resource.
5. **Audit (AD-26) & privacy:** confirmed admissions, mutations, confirmation use/rejection,
   reconciliation, and terminal outcomes emit a **metadata-only** audit receipt; never a payload.
6. **Compatibility (AD-22):** additive, serialization-tolerant event evolution; `ProjectCreated`
   gains an optional Folder binding (mandatory on new writes, nullable on replay); new writes stop
   emitting `ProjectFolderCreationPending` but keep its deserializer/apply; no `V2`, no history
   rewrite, no unsafe dual writer.

_Shared attributes for all Epic 7 stories: **repository authority** Hexalith.Projects (workflow
definitions + aggregate + Contracts) on platform-owned Durable Tasks; **owner** Product Owner +
Solution Architect (AD/G) + Test Architect (evidence); **prior-only deps** — Epic 7 entry gate + the
named Epic 6 reads; **fixture** — deterministic persisted-boundary fixture with restart/two-instance
harness on the G-4 runner; **verification** — `dotnet tool run hexalith-module test --profile durable --filter Story=<id>`;
**evidence** — `evidence/epic7/<id>-*.{trx,json}` incl. restart/duplicate/lost-response proof;
**release disposition** — durability evidence contributes to NFR-4/NFR-11; no Epic 7 story is itself
the terminal release gate._

### Story 7.1: Create a Project with exactly one authorized Folder

As a **delegated Chatbot service caller or Tenant Project Administrator**,
I want **to create a Project as a Folder-first idempotent Durable Task that classifies metadata, reserves the ProjectId, verifies or creates exactly one authorized Folder, then commits creation with the Folder binding**,
So that **a Project becomes caller-visible/Active only after one authorized Folder is bound and read-model-confirmed (FR-1, FR-19), with no observable folderless-Active interval**.

- **Traceability:** FR-1, FR-19 (canonical Metadata Classification / AD-31 / E-9); NFR-1, NFR-4; AD-3, AD-8, AD-12, AD-18, AD-22, AD-31; UJ-2, UJ-3; findings REL-001, E-9; evidence rows `fr-1`, `fr-19`.

**Acceptance Criteria** (plus shared invariants 1–6):

**Given** an authorized create request with a Project name and system-supplied `projectMetadata.metadataClass` in `{public_metadata,tenant_sensitive,credential_sensitive,secret}`, **When** admission runs, **Then** authorization precedes parsing, the shared `SensitiveMetadataTierValidator` validates classification, the task reserves a hidden ProjectId and provisions/validates exactly one authorized Folder keyed to the ProjectId, then submits one creation commit already carrying the Folder binding; the Project is Active only after read-model confirmation.

**Given** a malformed/unknown `metadataClass` (or raw secret / unrestricted path / foreign payload), **When** admission runs, **Then** it returns `400 ValidationFailure` with `details.rejectedField = projectMetadata.metadataClass`, echoes no value, and submits **no** creation command.

**Given** the legacy unversioned name-only request (the sole v1 compatibility shape), **When** submitted, **Then** it is accepted and classified per policy without breaking historical readers.

**Given** Folder creation succeeds but the creation commit cannot complete (crash/lost response), **When** the task recovers, **Then** it enters `NeedsAttention`, never auto-deletes the Folder, and an equivalent retry converges to the same task/outcome.

- **Estimate:** XL. **Completion boundary:** Folder-first idempotent create with metadata classification and no folderless-Active window; supersedes Story 1.4's create criteria.

### Story 7.2: Update Project Setup idempotently

As a **delegated Chatbot service caller or Tenant Project Administrator**,
I want **to update durable Project Setup through an idempotent Durable Task**,
So that **Conversation-continuity setup evolves additively and safely (FR-3) with equivalent-retry safety**.

- **Traceability:** FR-3, FR-19 (validation reuse); NFR-1, NFR-4, NFR-10; AD-5, AD-15, AD-16, AD-31; UJ-1; evidence row `fr-3`.

**Acceptance Criteria** (plus shared invariants 1, 3–6):

**Given** an authorized setup update (task-only, no confirmation), **When** submitted, **Then** the update is durable, additive, serialization-tolerant, re-uses the shared validator (rejecting secrets/paths/foreign payloads/invalid classification), and equivalent retries return the same task while changed requests conflict.

**Given** a denied/cross-Tenant/stale-version request, **When** submitted, **Then** it fails closed with no partial durable effect.

- **Estimate:** M. **Completion boundary:** idempotent additive setup update with validation parity; supersedes Story 1.8 update criteria.

### Story 7.3: Link an unassigned Conversation

As a **delegated Chatbot service caller**,
I want **to link an unassigned Conversation to a Project through a durable task that records intent while Conversations remains system of record**,
So that **a Conversation gains exactly one Project membership (FR-6) with a rebuildable reverse index and no local membership storage**.

- **Traceability:** FR-6; NFR-1, NFR-4; AD-10, AD-12, AD-14; UJ-1, UJ-3; evidence row `fr-6`.

**Acceptance Criteria** (plus shared invariants 1, 3–6):

**Given** an authorized unassigned Conversation (task-only link), **When** the durable workflow runs, **Then** it calls the Conversations owner with an idempotency key + expected version, persists the owner receipt before advancing, updates the Tenant-scoped **reverse index** (aggregate stores no membership), and emits a metadata-only receipt.

**Given** a Conversation already in another Project, **When** link is attempted, **Then** it is rejected and an explicit **move** (7.4) is required.

**Given** an unknown owner response or restart, **When** the task recovers, **Then** it queries owner status before retrying and converges without duplicate membership.

- **Estimate:** M. **Completion boundary:** durable single-membership link via Conversations owner + reverse index.

### Story 7.4: Move a Conversation between Projects

As a **Tenant Project Administrator or delegated caller**,
I want **to move a Conversation between Projects through Preview + single-use confirmation + durable saga**,
So that **membership changes are consequential, auditable, and recoverable (FR-7) with prior membership removed before the new one is created**.

- **Traceability:** FR-7; NFR-1, NFR-4; AD-5, AD-10, AD-12, AD-13; UJ-3; evidence row `fr-7`.

**Acceptance Criteria** (plus shared invariants 1–6; move is **confirmation-required**):

**Given** a valid bound Confirmation Artifact and authority to both Projects and the Conversation, **When** the move task runs, **Then** the saga removes prior membership before creating the new one via the Conversations owner (idempotency key + expected version + persisted receipts), and emits a metadata-only receipt.

**Given** authority to either Project or the Conversation cannot be established, or the artifact is stale/replayed, **When** the move is attempted, **Then** it fails closed (`409` + `RenewPreview` for stale confirmation; safe denial otherwise) with no partial membership.

**Given** a mid-saga crash after removal but before re-creation, **When** recovered, **Then** it compensates or reaches `NeedsAttention` — never leaving the Conversation orphaned or double-membered.

- **Estimate:** L. **Completion boundary:** durable confirmed move with saga recovery.

### Story 7.5: Unlink a Conversation

As a **Tenant Project Administrator or delegated caller**,
I want **to unlink a Conversation from a Project through Preview + confirmation + durable task**,
So that **the association is removed without deleting the Conversation (FR-11) and remains auditable**.

- **Traceability:** FR-11 (Conversation); NFR-1, NFR-4; AD-5, AD-10, AD-12, AD-13; evidence row `fr-11-conversation`.

**Acceptance Criteria** (plus shared invariants 1–6; confirmation-required):

**Given** a valid confirmation and authority, **When** unlink runs, **Then** the reverse index membership is removed via the owner, the underlying Conversation is not deleted, and a metadata-only receipt is emitted.

**Given** a stale/replayed confirmation or denied authority, **When** unlink is attempted, **Then** it fails closed with no durable effect.

- **Estimate:** M. **Completion boundary:** durable confirmed Conversation unlink; resource preserved.

### Story 7.6: Replace a Project Folder

As a **Tenant Project Administrator**,
I want **to replace a Project's single authorized Folder through Preview + confirmation + durable task**,
So that **the exactly-one-Folder invariant holds during replacement (FR-8) and the Folder is replaceable but never removed from an Active Project**.

- **Traceability:** FR-8, FR-11 (Folder is replace-only, not removable); NFR-1, NFR-4; AD-3, AD-11, AD-12, AD-13; UJ-2; evidence row `fr-8`.

**Acceptance Criteria** (plus shared invariants 1–6; confirmation-required):

**Given** a valid confirmation, authority, and a new authorized Folder, **When** replace runs, **Then** the new Folder is verified before binding, `ProjectFolderSet` records the new stable Folder identity, the Project retains exactly one Folder throughout, and folder authorization stays delegated to Folders.

**Given** an attempt to **remove** the Folder from an Active Project, **When** submitted, **Then** it is rejected (replace-only while Active).

**Given** new-Folder verification fails or the confirmation is stale, **When** replace is attempted, **Then** it fails closed, retains the prior Folder, and never auto-deletes an owner resource.

- **Estimate:** L. **Completion boundary:** durable confirmed Folder replacement preserving the single-Folder invariant.

### Story 7.7: Link an authorized File Reference

As a **delegated Chatbot service caller or Tenant Project Administrator**,
I want **to link an authorized File Reference to a Project through an idempotent durable task**,
So that **a File is referenced by stable identity/metadata (FR-9) without changing the Project Folder or copying content**.

- **Traceability:** FR-9; NFR-1, NFR-4; AD-11, AD-12, AD-15; evidence row `fr-9`.

**Acceptance Criteria** (plus shared invariants 1, 3–6; task-only additive link):

**Given** an authorized File Reference (auth delegated to Folders), **When** link runs, **Then** it records stable File identity + metadata (no contents/paths), does not alter the Project Folder, stays within the 5,000-reference cap, and is idempotent under retry.

**Given** a denied/unauthorized/stale File, **When** link is attempted, **Then** it fails closed with a Projects-safe reason code (never rethrowing raw upstream detail).

- **Estimate:** M. **Completion boundary:** idempotent authorized File Reference link.

### Story 7.8: Unlink a File Reference

As a **Tenant Project Administrator or delegated caller**,
I want **to unlink a File Reference through Preview + confirmation + durable task**,
So that **the reference is removed without deleting the File (FR-11) and remains auditable**.

- **Traceability:** FR-11 (File); NFR-1, NFR-4; AD-5, AD-11, AD-12, AD-13; evidence row `fr-11-file`.

**Acceptance Criteria** (plus shared invariants 1–6; confirmation-required):

**Given** a valid confirmation and authority, **When** unlink runs, **Then** the File Reference is removed, the underlying File is not deleted, and a metadata-only receipt is emitted.

**Given** a stale/replayed confirmation or denial, **When** unlink is attempted, **Then** it fails closed with no durable effect.

- **Estimate:** S. **Completion boundary:** durable confirmed File unlink; resource preserved.

### Story 7.9: Link an authorized Memory

As a **delegated Chatbot service caller**,
I want **to link an authorized Memory to a Project through an idempotent durable task (identity/metadata only)**,
So that **a Memory is referenced (FR-10) with authorization delegated to Hexalith.Memories and no payload copy**.

- **Traceability:** FR-10; NFR-1, NFR-4; AD-11, AD-12, AD-15; UJ-1, UJ-3; evidence row `fr-10`.

**Acceptance Criteria** (plus shared invariants 1, 3–6; task-only additive link):

**Given** an authorized Memory reference (Memories owns existence/payload/lifecycle/authorization; Case-vs-Unit granularity resolved by the pinned G-2 Memories contract), **When** link runs, **Then** it records stable Memory identity + metadata only, is idempotent, and tolerates Memories' async/eventually-consistent, `[Experimental]` ingestion.

**Given** an unavailable/denied/unauthorized Memory, **When** link is attempted, **Then** it fails closed-clean with a safe reason code and never copies payload.

- **Estimate:** M. **Completion boundary:** idempotent metadata-only Memory link honoring the pinned Memories contract.

### Story 7.10: Unlink a Memory

As a **Tenant Project Administrator or delegated caller**,
I want **to unlink a Memory through Preview + confirmation + durable task**,
So that **the reference is removed without deleting the Memory (FR-11) and remains auditable**.

- **Traceability:** FR-11 (Memory); NFR-1, NFR-4; AD-5, AD-11, AD-12, AD-13; evidence row `fr-11-memory`.

**Acceptance Criteria** (plus shared invariants 1–6; confirmation-required):

**Given** a valid confirmation and authority, **When** unlink runs, **Then** the Memory reference is removed, the underlying Memory is not deleted, and a metadata-only receipt is emitted.

**Given** a stale/replayed confirmation or denial, **When** unlink is attempted, **Then** it fails closed with no durable effect.

- **Estimate:** S. **Completion boundary:** durable confirmed Memory unlink; resource preserved.

### Story 7.11: Confirm an ambiguous Project choice

As a **Project User (via Chatbot presentation)**,
I want **to confirm one accessible candidate from an ambiguous resolution through a bound Confirmation Artifact and durable task**,
So that **the confirmed Project-to-Conversation association is recorded (FR-14) with no preselection and no silent attachment**.

- **Traceability:** FR-14; NFR-1, NFR-4; AD-5, AD-13, AD-32; UJ-3; UX candidate-comparison journey (AD-34); evidence row `fr-14`.

**Acceptance Criteria** (plus shared invariants 1–6; confirmation-required):

**Given** a `MultipleCandidates` outcome from 6.4 and accessible candidates with no preselection, **When** the user confirms one via its bound artifact, **Then** the durable task records the choice (linking/associating via the Conversations owner), and rejected candidates are not linked.

**Given** a stale/expired/replayed artifact or a candidate that became unauthorized, **When** confirm is attempted, **Then** it fails closed (`409` + `RenewPreview` for stale) with no association.

**Given** lost response after confirmation, **When** recovered, **Then** the equivalent retry converges to the single recorded association.

- **Estimate:** L. **Completion boundary:** durable confirmed ambiguous-resolution choice; MCP cannot self-confirm (AD-29).

### Story 7.12: Confirm a proposed new Project

As a **Project User (via Chatbot presentation)**,
I want **to confirm creation of a proposed Project bound to a server Preview through a durable Folder-first task**,
So that **a Project is created from inference only after authorized confirmation (FR-15), linking the initiating Conversation and authorized attachments**.

- **Traceability:** FR-15, FR-1/FR-19 (reuses Folder-first create + classification); NFR-1, NFR-4; AD-5, AD-8, AD-13, AD-31; UJ-3; evidence row `fr-15`.

**Acceptance Criteria** (plus shared invariants 1–6; confirmation-required):

**Given** a proposed Project with suggested name + initial setup and a bound Preview, **When** the user confirms, **Then** creation runs the **same Folder-first idempotent path as 7.1** (metadata classified, exactly-one-Folder), then links the initiating Conversation/attachments via the owner; no Project exists before confirmation.

**Given** a stale/replayed Preview or denied authority, **When** confirm is attempted, **Then** it fails closed with no Project and no Folder side effect.

**Given** Folder created but activation cannot commit, **When** recovered, **Then** `NeedsAttention` with no auto-deletion, converging on retry.

- **Estimate:** L. **Completion boundary:** durable confirmed proposed-creation; consequential autonomous MCP confirmation stays disabled until gates pass.

### Story 7.13: Archive an Active Project

As a **Project User, Tenant Operator, or Tenant Project Administrator**,
I want **to archive an Active Project through Preview + single-use confirmation + idempotent durable task**,
So that **archival is consequential, recoverable, and auditable (FR-4) while references remain auditable**.

- **Traceability:** FR-4; NFR-1, NFR-4; AD-4, AD-5, AD-13; UJ-5 (archive side); evidence row `fr-4`.

**Acceptance Criteria** (plus shared invariants 1–6; confirmation-required):

**Given** a valid confirmation and authority over an Active Project, **When** archive runs, **Then** lifecycle becomes `Archived` after read-model confirmation, the Project is excluded from automatic resolution unless explicitly requested, and references remain auditable.

**Given** a stale/replayed confirmation or denied authority, **When** archive is attempted, **Then** it fails closed with no lifecycle change.

**Given** duplicate delivery / lost response, **When** retried, **Then** the equivalent retry converges to the single archived outcome.

- **Estimate:** M. **Completion boundary:** durable confirmed archive; **restore is 7.14**. Supersedes Story 1.8 archive criteria.

### Story 7.14: Restore an Archived Project

As a **Project User, Tenant Operator, or Tenant Project Administrator**,
I want **to restore an Archived Project through Preview + confirmation + idempotent durable task that establishes Folder validity before activation**,
So that **restore is the safe counterpart to archive (FR-23, UJ-5) with no invalid Active Project exposed**.

- **Traceability:** FR-23; NFR-1, NFR-4; AD-3, AD-4, AD-5, AD-8, AD-13, AD-23; UJ-5; evidence row `fr-23`. **Primary owner of FR-23 per SCP §4.6.**

**Acceptance Criteria** (plus shared invariants 1–6; confirmation-required):

**Given** a valid confirmation, **When** Preview runs, **Then** it verifies Tenant, actor, authority, current Project version, and **exactly one authorized Folder**; if the prior Folder is invalid/missing it requires an authorized replacement or same-name Folder creation before confirmation.

**Given** restore proceeds, **When** the task runs, **Then** Folder validity is established while still `Archived`; a replacement emits `ProjectFolderSet` **before** `ProjectRestored` in one commit; the Project becomes Active only after read-model confirmation.

**Given** Folder creation succeeds but activation cannot commit, **When** recovered, **Then** the task enters `NeedsAttention`, never auto-deletes the Folders-owned resource, and stale/replay/cancel/duplicate/concurrency/lost-response cannot expose an invalid Active Project.

- **Estimate:** L. **Completion boundary:** durable confirmed restore with Folder-before-activation ordering and full recovery evidence.

### Story 7.15: Reconcile legacy and interrupted workflows

As a **Solution Architect / platform operator**,
I want **compensating durable tasks that detect and reconcile folderless, pending-Folder, and in-flight legacy records without rewriting committed history**,
So that **legacy partial records reach a safe terminal disposition before their value slice cuts over (NFR-4, NFR-10)**.

- **Traceability:** NFR-4, NFR-10; FR-1/FR-4/FR-23 (reconciliation evidence); AD-12, AD-17, AD-22; findings REL-001; evidence row `nfr-4-reconcile`.

**Acceptance Criteria** (plus shared invariants 1, 3–6):

**Given** the AD-17 inventory of legacy records (Active-folderless, `ProjectFolderCreationPending`, in-flight), **When** the compensating task runs per record, **Then** it uses durable receipts, status recovery, and a terminal disposition; historical folderless state is excluded from Active reads and routed to compensation; no owner resource is auto-deleted.

**Given** committed history, **When** reconciliation runs, **Then** no event is rewritten and no unsafe dual writer is introduced; new writes stop emitting `ProjectFolderCreationPending` while its deserializer/apply are retained.

**Given** a record that cannot be safely reconciled, **When** processed, **Then** it enters `NeedsAttention` with an honest recorded blocker rather than a false success.

- **Estimate:** L. **Completion boundary:** every legacy/interrupted record reaches a safe terminal or `NeedsAttention` disposition with evidence; single-writer command cutover complete.

---

## Epic 8: Safe Operations and Release Confidence

Turn the corrective read and durable-workflow work into **executable production evidence** across
operator surfaces, Safe Diagnostic Export, health/telemetry, packaging/supply chain, authenticated
parity/isolation/accessibility, performance/back-pressure, cross-workflow resilience, and terminal
deployment/rollback/stakeholder acceptance. The whole epic is fail-closed behind the AD-30
machine-checkable evidence gate. It closes TEST-001 and every required P1/P2 finding (or records an
authorized disposition), and maps every critical NFR-11 release category to an owning story.

**Epic 8 entry gate (prerequisite, not delivered value).** Supplies **deterministic
persisted-boundary fixtures** (authorized Tenant, seeded Projects/references/audit, required UI/static
assets) and **blocking CI** lanes over the real supported platform path (G-4 runner + `hexalith-evidence`),
with G-5 identity/KMS/secret bindings and G-6 toolchain alignment pinned. No failed/skipped/blocked/
unavailable critical case may be represented as passing.

_Shared attributes for all Epic 8 stories: **repository authority** platform adapters + Hexalith.Projects
evidence (Chatbot companion evidence is separately owned — see 8.8); **owner** Test Architect (evidence
rows/fixtures) + Product Owner + Solution Architect (AD/G conformance); **prior-only deps** — Epics 6–7
value slices + the Epic 8 entry gate; **fixture** — the entry-gate persisted-boundary fixtures;
**verification** — `dotnet tool run hexalith-module test --profile <lane> --filter Story=<id>` +
`dotnet tool run hexalith-evidence validate …`; **evidence** — `evidence/epic8/<id>-*.{trx,json}` +
the matching `hexalith.readiness-evidence.v1` rows; **compatibility/rollback** — legacy routing
retained and reversible until 8.11 acceptance. Every story's completion requires honest pass/fail;
an unavailable environment records "not verified," never `passed`._

### Story 8.1: Inspect task, audit, and reconciliation truth

As a **Tenant Operator or Tenant Project Administrator**,
I want **to inspect Durable Task status, metadata-only audit timeline, and reconciliation outcomes through authorized read surfaces**,
So that **operators have truthful operational visibility (FR-21) with correct retention and no payload leakage (NFR-8)**.

- **Traceability:** FR-21, FR-22 (task/audit read); NFR-1, NFR-8; AD-21, AD-26, AD-30; UX-DR10, UX-DR16; findings (audit) ; evidence rows `fr-21`, `nfr-8`.

**Acceptance Criteria:**

**Given** an authorized operator, **When** the audit timeline / task-status / reconciliation views load, **Then** they show metadata-only records (admission, terminal outcome, confirmation use/rejection, auth denial, confirmed mutations, reconciliation, receipt IDs), Tenant-scoped, with audit retained ≥ 365 days and task/idempotency records ≥ 30 days / result lifetime; Resolution Traces and exports are absent (not persisted).

**Given** a denied/cross-Tenant target, **When** a view loads, **Then** it fails closed (safe absence) with no leakage.

**Given** intermediate states/polls/retries, **When** rendered, **Then** they are telemetry-only and separated from durable audit truth (AD-26).

- **Estimate:** M. **Completion boundary:** truthful task/audit/reconciliation reads with retention + separation evidence.

### Story 8.2: Create a bounded Safe Diagnostic Export

As a **separately authorized Tenant Operator or Tenant Project Administrator**,
I want **to create a synchronous, bounded, non-retained `projects.safe-diagnostic-export.v1` export via Web, CLI, or MCP**,
So that **support gets bounded metadata truth (FR-24) without unbounded troubleshooting access and Chatbot cannot export**.

- **Traceability:** FR-24; NFR-1, NFR-6, NFR-8; AD-7, AD-19, AD-21, AD-26, AD-27; UX-DR18; evidence row `fr-24`. **Primary owner of FR-24 per SCP §4.6.**

**Acceptance Criteria:**

**Given** a caller with the **separate** export permission (distinct from FR-22 read; Chatbot rejected), **When** export runs, **Then** it produces one synchronous snapshot ≤ 1 MiB encoded, ≤ 500 reference rows, ≤ 100 audit rows, with stable/deterministic reference ordering and newest-first audit rows, truncation metadata reporting included/omitted counts + safe reasons, **no continuation cursor, no retained bytes/tasks**, under a two-lease per-Tenant gate; every attempt and outcome is audited metadata-only.

**Given** upstream component unavailability, **When** export runs, **Then** unavailable components are marked safely without raw errors or fabricated completeness.

**Given** a third concurrent export for the Tenant or a lost/duplicate request, **When** attempted, **Then** it is throttled with structured retry guidance and does not double-produce.

- **Estimate:** L. **Completion boundary:** bounded, non-retained, separately authorized export with equivalent Web/CLI/MCP semantics.

### Story 8.3: Operate Projects through a conformant Web console

As a **Tenant Operator or Tenant Project Administrator**,
I want **an authenticated FrontComposer/Fluent V5 operator Web console exposing audit-first maintenance actions (archive/restore/relink/unlink/reevaluate) with Preview/confirmation and response/recovery fields**,
So that **operators run safe maintenance (FR-22 + Epic 7 actions) with conformant, accessible, safe-failure UX**.

- **Traceability:** FR-22, FR-4/FR-23/FR-11 (operator-initiated via Epic 7 durable tasks); NFR-1, NFR-9; AD-2, AD-19, AD-29, AD-32, AD-33, AD-34; UX-DR7, UX-DR11–UX-DR13, UX-DR17, UX-DR21–UX-DR25; evidence row `fr-22-web-ops`.

**Acceptance Criteria:**

**Given** an authenticated operator with role-specific authority, **When** the console renders maintenance actions, **Then** each mutating action shows Preview (current→proposed state, targets, warnings, expected audit event), requires the bound Confirmation Artifact, maps every phase to Durable Task/read-model truth, exposes `responseState`/`asOf`/`projectVersion`/Recovery Action Codes, and treats reevaluate as read-only Refresh.

**Given** a denied action or stale confirmation, **When** attempted, **Then** the UI fails closed with a safe reason code (`409`/`RenewPreview` for stale) and no client-supplied authority.

**Given** Fluent V5 governance (G-6) and the required `FluentAccordion` section pattern, **When** the console composes sibling sections, **Then** it uses Fluent 2 tokens (not the non-normative HTML prototype).

- **Estimate:** L. **Completion boundary:** conformant authenticated operator Web console driving Epic 7 durable tasks with safe-failure UX.

### Story 8.4: Operate Projects through a deterministic CLI contract

As a **Tenant Operator or automation pipeline**,
I want **an authenticated CLI with deterministic machine output, stable exit codes, and confirmation semantics for mutating commands**,
So that **scripted operations get a stable, redaction-safe contract with parity to Web/MCP (FR-22, NFR-8)**.

- **Traceability:** FR-22; NFR-1, NFR-8; AD-2, AD-19, AD-29, AD-33; UX-DR19, UX-DR24; evidence row `cli-contract`.

**Acceptance Criteria:**

**Given** a mutating CLI command (`archive`/`restore`/`relink`/`unlink`/`reevaluate`), **When** invoked, **Then** it requires explicit target IDs + tenant scope + a confirmation/dry-run contract, returns deterministic JSON with stable exit codes, and reevaluate is read-only.

**Given** partial failure / freshness / denial, **When** a command runs, **Then** failure semantics, freshness vocabulary, and partial-failure behavior match the canonical contracts with safe reason codes and no payload echo.

**Given** identical facts, **When** compared to Web (8.3) and MCP (8.5), **Then** semantics are equivalent (parity) without authority expansion.

- **Estimate:** M. **Completion boundary:** deterministic authenticated CLI contract with confirmation semantics + parity.

### Story 8.5: Operate Projects through agent-safe MCP contracts

As an **MCP-assisted agent operating under dual-principal authority**,
I want **agent-safe MCP resources (reads) and tools (mutations) that cannot expand authority or self-confirm end-user decisions**,
So that **agents operate Projects safely (FR-22) with consequential mutation contained until gates pass (AD-29)**.

- **Traceability:** FR-22; NFR-1, NFR-8; AD-2, AD-19, AD-20, AD-29, AD-33; UX-DR20; findings AGENT-001 (surface); evidence row `mcp-contract`.

**Acceptance Criteria:**

**Given** MCP read **resources** vs mutating **tools** are separated, **When** a tool is invoked, **Then** it requires explicit action + target IDs + tenant scope + a confirmation contract, carries structured safe metadata **plus** a short safe explanation (never explanation-only), and cannot bypass Preview/Confirmation/admission or expand permissions.

**Given** an end-user resolution/proposal confirmation, **When** MCP attempts it, **Then** it is **refused** (only the end user confirms; AD-29), and consequential autonomous MCP mutation remains **disabled** until readiness/release gates pass — availability/containment state shown explicitly.

**Given** an unknown tool, **When** called, **Then** it is rejected with suggestions.

- **Estimate:** M. **Completion boundary:** agent-safe MCP contracts with explicit containment; autonomous mutation stays disabled.

### Story 8.6: Observe truthful dependency and projection health

As a **platform operator**,
I want **health, readiness, telemetry, and generated structured logging that report real dependency and projection state with bounded metadata-only signals**,
So that **availability/recovery (NFR-3) is observable without fabricated health or payload leakage**.

- **Traceability:** NFR-1, NFR-3, NFR-2 (config fail-fast support); AD-20, AD-26, AD-28; findings (health) ; evidence row `nfr-3`.

**Acceptance Criteria:**

**Given** real dependency/projection state (including stale/rebuilding/unavailable), **When** health/readiness/telemetry report, **Then** signals reflect true state (no always-green), are bounded and metadata-only, and structured logs are source-generated (never payloads/names/tokens).

**Given** incomplete identity/audience/signing/key configuration (AD-20/AD-28), **When** the host starts, **Then** it **fails fast** rather than admitting work with partial authority.

**Given** a dependency outage, **When** observed, **Then** readiness degrades truthfully and supports the RTO/recovery evidence in 8.10.

- **Estimate:** M. **Completion boundary:** truthful bounded health/telemetry/logging with fail-fast admission.

### Story 8.7: Consume reproducible packages and supply-chain evidence

As a **downstream consumer / release engineer**,
I want **the target package graph, versions, signing, and supply-chain evidence to match the supported ownership and public-surface model**,
So that **release artifacts are reproducible and correctly bounded (NFR-10, NFR-11) with the AD-24 inventory enforced**.

- **Traceability:** NFR-10, NFR-11, NFR-2 (signing/provenance support); AD-16, AD-24, AD-25; findings CLIENT-001/supply-chain; evidence row `supply-chain`.

**Acceptance Criteria:**

**Given** the AD-24 target inventory (packable `Contracts`/`Projects`/`Testing`; non-packable `UI.Contracts`; two-line `Server` host; conditional generated `Client`), **When** the release manifest is validated, **Then** the exact inventory is enforced, `Projects.Client.Generation`/`.Shared` are retired only after the generator reproduces output + compatibility fingerprints, and central package versions (incl. Builds-owned NSwag 14.7.1 / Fluxor 6.9.0) are used with production fail-fast.

**Given** Release-mode CI (package references) vs local Debug (project references), **When** built, **Then** artifacts are reproducible, signed, and provenance-evidenced; each test project runs individually.

**Given** a boundary violation (e.g. `Contracts` depending on Shell/Fluxor/Fluent/ASP.NET/Dapr/Aspire), **When** CI runs, **Then** it fails closed.

- **Estimate:** M. **Completion boundary:** reproducible, correctly bounded package graph + supply-chain evidence matching AD-24.

### Story 8.8: Verify authenticated parity, isolation, and accessibility

As a **Test Architect (with the Chatbot Presentation Owner for companion evidence)**,
I want **blocking authenticated live E2E for cross-surface parity, cross-Tenant isolation, privacy leakage, and WCAG 2.2 AA accessibility across operator and Chatbot companion journeys**,
So that **security/isolation/privacy (NFR-1) and accessibility (NFR-9) have authenticated evidence and the 19/56 live failures are superseded**.

- **Traceability:** NFR-1, NFR-9, NFR-11; AD-19, AD-20, AD-29, AD-33, AD-34; UX-DR27; SM-5; findings SEC-001/leakage/E-6; evidence rows `nfr-1`, `nfr-9`, `e2e-live`. **Owns Chatbot NFR-9/SM-5/AD-34 companion evidence per SCP §4.6.**

**Acceptance Criteria:**

**Given** authenticated live topology with real Keycloak/OIDC, **When** the critical E2E lane runs, **Then** cross-surface parity, cross-Tenant isolation, and `NoPayloadLeakage` cases are **blocking** with no unexplained permanent skips and no failed case accepted as evidence (superseding the 19-passed/56-failed run).

**Given** operator journeys, **When** accessibility runs, **Then** automated axe + authenticated manual keyboard/focus/live-region/screen-reader/200%-zoom/320px-reflow evidence records WCAG 2.2 AA at small/median/max shapes; any unresolved critical/serious violation blocks release.

**Given** the **cross-repository Chatbot companion** (candidate/proposal/confirm/cancel/recovery/task/first-response-admission journeys), **When** its evidence is produced, **Then** it is owned by a **separately approved Chatbot owner + pinned revision** with authenticated verification artifacts — without granting Projects authority to mutate Chatbot; absence of this evidence blocks Projects release.

- **Estimate:** XL. **Completion boundary:** blocking authenticated parity/isolation/privacy/a11y evidence incl. Chatbot companion; no false pass.

### Story 8.9: Meet bounded performance and back-pressure objectives

As a **Test Architect / platform operator**,
I want **verified performance at supported scale and enforced per-Tenant back-pressure, quotas, timeouts, and pagination bounds**,
So that **scale/performance (NFR-5), pagination/export bounds (NFR-6), and back-pressure (NFR-7) are proven and overload is safe**.

- **Traceability:** NFR-5, NFR-6, NFR-7; AD-14, AD-15, AD-21, AD-27; evidence rows `nfr-5`, `nfr-6`, `nfr-7`.

**Acceptance Criteria:**

**Given** supported scale (10,000 Projects/Tenant, 5,000 refs/Project, 100,000 audit records), **When** the performance lane runs, **Then** metadata reads meet p95 < 500 ms (at 1,000 Projects / 500 refs) and < 1 s at max, and task admission p95 < 500 ms warm, at small/median/max shapes.

**Given** cursor paging, **When** exercised, **Then** default 50 / cap 200 holds with opaque scoped cursors; Safe Diagnostic Export bounds (8.2) are enforced.

**Given** per-Tenant limits (100 reads/s burst 200, 20 mutations/s burst 40, 1,000 nonterminal tasks, 2 export leases, 2 s interactive / 10 s durable timeouts, ≤ 3 retries in 30 s), **When** exceeded, **Then** platform admission rejects **before** partial durable work with structured retry guidance; domain handlers carry no retry/quota logic.

- **Estimate:** L. **Completion boundary:** authenticated performance + back-pressure evidence at supported scale.

### Story 8.10: Prove cross-workflow resilience

As a **Test Architect / platform operator**,
I want **durable workflow and projection correctness proven across restart, retry, duplicate delivery, concurrency, partial failure, lost response, and reconciliation**,
So that **availability/recovery (NFR-3) and durability/idempotency (NFR-4) hold under adversarial runtime conditions**.

- **Traceability:** NFR-3, NFR-4; FR-1/FR-4/FR-7/FR-14/FR-15/FR-23 (durable-path resilience); AD-4, AD-9, AD-12, AD-17, AD-28; findings REL-001/TEST-001; evidence rows `nfr-3-resilience`, `nfr-4`.

**Acceptance Criteria:**

**Given** an accepted Durable Task, **When** the worker restarts or a second instance runs, **Then** it resumes or reaches truthful `NeedsAttention` within 5 min (RTO 15 min), with fenced ownership and no duplicate durable effect (RPO 0 for committed events).

**Given** duplicate delivery / lost response / concurrency, **When** retried, **Then** equivalent retries converge to the original durable truth and changed requests conflict; no silent drop or duplicate.

**Given** a mid-saga partial failure, **When** recovered, **Then** compensation or `NeedsAttention` applies and reconciliation (7.15) reaches a terminal disposition.

- **Estimate:** L. **Completion boundary:** adversarial resilience evidence for the durable-workflow platform path.

### Story 8.11: Complete deployment, rollback evidence, and stakeholder acceptance

As a **Release Owner (Jerome and John)**,
I want **the deployed version/environment, encryption/KMS evidence, health/smoke evidence, rollback drill, residual-risk dispositions, and dated terminal acceptance recorded through the AD-30 evidence gate**,
So that **production release is authorized only on complete, honest evidence (NFR-2, NFR-11) and never by recording a blocker (this is the terminal gate)**.

- **Traceability:** NFR-2, NFR-11; all FR/NFR release rows; AD-25, AD-28, AD-30; findings (release/E-8); evidence rows `nfr-2`, `nfr-11`, `release-acceptance`. **Terminal release gate per AD-30.**

**Acceptance Criteria:**

**Given** the canonical `implementation-readiness-traceability-matrix.yaml`, **When** `hexalith-evidence validate` runs, **Then** it rejects duplicate/missing keys, placeholders, incomplete ownership/version/command/artifact fields, failed critical evidence, unexplained critical skips, and `passed` for unavailable environments; all FR-1…24 / NFR-1…11 / P1×9 / P2×7 / critical release rows are present and honest.

**Given** deployment, **When** performed, **Then** deployed version/environment, authenticated encryption in transit + platform-managed encryption at rest with KMS rotation/revocation evidence, health/smoke evidence, and a **rollback drill reference** are recorded.

**Given** all critical evidence passes, **When** release is proposed, **Then** **Jerome and John record dated residual-risk dispositions and terminal acceptance**; production, consequential autonomous MCP mutation, and proposed-Project confirmation are enabled **only** after this passes.

**Given** any unavailable environment or unresolved critical case, **When** 8.11 is evaluated, **Then** it **cannot complete** by recording a blocker — completion requires real passing evidence.

- **Estimate:** L. **Completion boundary:** dated terminal release acceptance on complete honest AD-30 evidence; supersedes the prior Story 8.9 gate. Supersedes Story 5.12's "executed = accepted" interpretation.
