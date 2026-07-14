---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
assessmentStatus: NOT_READY
completedAt: 2026-07-14
inputDocuments:
  prd:
    - _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md
  architecture:
    - _bmad-output/planning-artifacts/architecture.md
  epics:
    - _bmad-output/planning-artifacts/epics.md
  ux:
    - _bmad-output/planning-artifacts/ux-design-specification.md
    - _bmad-output/planning-artifacts/ux-design-directions.html
---

# Implementation Readiness Assessment Report

**Date:** 2026-07-14
**Project:** projects

## Document Inventory

### PRD

- Primary: `prds/prd-Hexalith.Projects-2026-05-24/prd.md` (23,936 bytes; modified 2026-05-29)
- The containing versioned bundle also includes decision logs, validation reports, a review rubric, and handoff notes. It is not a sharded PRD.

### Architecture

- Primary: `architecture.md` (51,169 bytes; modified 2026-07-14)
- No sharded architecture document found.

### Epics and Stories

- Primary: `epics.md` (102,645 bytes; modified 2026-07-14)
- No sharded epics document found.

### UX Design

- Primary: `ux-design-specification.md` (53,895 bytes; modified 2026-05-29)
- Supplementary: `ux-design-directions.html` (25,390 bytes; modified 2026-05-29)
- No sharded UX document found.

### Discovery Issues

- No whole-versus-sharded duplicates were found.
- No required document category is missing.
- The PRD's versioned bundle location is unambiguous.

## PRD Analysis

### Functional Requirements

#### FR-1: Create Project

Chatbot can create a Project with tenant context and Project name as the required inputs. Project name is the only required user-supplied field. Description, initial setup, initial references to Conversations, File References, and Memories are optional. If no Project Folder is supplied, Chatbot can request creation of a Project Folder with the same name. Realizes UJ-2 and UJ-3.

Testable consequences:

- Creating a Project records durable Project metadata and sets Project Lifecycle State to `Active`.
- Creating a Project requires only a Project name from the user.
- Creating a Project can create or attach a Project Folder with the same name as the Project when no folder is supplied.
- Creating a Project does not duplicate conversation transcripts, file contents, or memory payloads.
- Creating a Project fails closed when tenant context is missing or unauthorized.

#### FR-2: Open Project

Chatbot can open a Project and receive the Project metadata, lifecycle state, setup, and authorized references needed to initialize a conversation. Realizes UJ-1.

Testable consequences:

- Opening a Project returns only references visible to the requesting tenant/user context.
- Archived or unavailable Projects are clearly identified and cannot silently become active conversation context.

#### FR-3: Update Project Setup

Chatbot can update Project Setup needed for conversation continuity, including project instructions, context preferences, and configuration metadata. Realizes UJ-1.

Testable consequences:

- Setup updates are durable and available to later conversations.
- Setup can include project goals, user-facing instructions, preferred context sources, source inclusion/exclusion preferences, conversation-start defaults, and metadata needed to resume the Project.
- v1 Project Setup describes conversation behavior and context policy, not model-provider internals.
- Setup updates preserve additive, serialization-tolerant contract behavior.
- Setup updates do not allow raw secrets, unrestricted file paths, or payload data that belongs to another bounded context.

#### FR-4: Archive Project

Chatbot or authorized operators can archive a Project so it remains discoverable for history but is no longer selected as active context by default.

Testable consequences:

- v1 Project Lifecycle State is limited to `Active` and `Archived`.
- Archived Projects are excluded from automatic Project Resolution unless explicitly requested.
- Existing references remain auditable after archival.

#### FR-5: List Projects

Chatbot can list Active and Archived Projects visible to the requesting tenant/user context.

Testable consequences:

- List results are tenant-scoped and authorization-filtered.
- List results include enough metadata for Chatbot to present Project choices without loading full Project Context.
- List results can filter by Project Lifecycle State.

#### FR-6: Link Conversation

Chatbot can link an existing Conversation to a Project. Realizes UJ-1 and UJ-3.

Testable consequences:

- The link records stable Conversation identity and relevant metadata.
- A Conversation can be linked to only one Project in v1.
- Linking a Conversation that already belongs to another Project requires an explicit move operation rather than creating a second membership.
- The link does not copy transcript content into Projects.
- The link fails if tenant authorization for the Conversation cannot be established.

#### FR-7: Move Conversation Between Projects

Chatbot can move a Conversation from one Project to another when the user explicitly confirms the move.

Testable consequences:

- Moving a Conversation removes the prior Project membership before creating the new one.
- Moving a Conversation is auditable as metadata.
- Moving a Conversation fails closed when authorization to either Project or the Conversation cannot be established.

#### FR-8: Set Project Folder

Chatbot can set the single authorized Project Folder for a Project. Realizes UJ-2.

Testable consequences:

- A Project has exactly one Project Folder in v1.
- Setting a Project Folder records stable Folder identity and relevant metadata.
- Setting a new Project Folder replaces the previous Project Folder only through an explicit update.
- The Project Folder reference does not store file contents or unrestricted filesystem paths in Projects.
- Folder authorization remains delegated to `Hexalith.Folders`.

#### FR-9: Link File Reference

Chatbot can link authorized File References to a Project when a file should be part of Project Context without changing the Project Folder.

Testable consequences:

- File References are optional and do not replace the Project Folder.
- A File Reference records stable File identity and relevant metadata.
- File authorization remains delegated to `Hexalith.Folders`.

#### FR-10: Link Memory

Chatbot can link authorized Memory references to a Project. Realizes UJ-1 and UJ-3.

Testable consequences:

- The link records stable Memory identity and relevant metadata.
- The link does not store Memory payloads in Projects.
- Memory authorization remains delegated to `Hexalith.Memories`.

#### FR-11: Unlink Context Reference

Chatbot can remove a Conversation, File Reference, or Memory reference from a Project without deleting the underlying resource. The Project Folder can be replaced but not removed unless the Project is archived. v1 Projects require a Project Folder.

Testable consequences:

- Unlinking removes the association from Project Context.
- Unlinking does not delete the underlying Conversation, File Reference, or Memory.
- Unlinking is auditable as metadata.

#### FR-12: Resolve Project From Conversation

Chatbot can ask Projects to resolve Candidate Projects for a Conversation that has no explicit Project. Realizes UJ-3.

Testable consequences:

- Resolution returns a Resolution Result of `NoMatch`, `SingleCandidate`, or `MultipleCandidates`.
- Candidate Projects include one or more Resolution Reason Codes.
- Resolution does not access unauthorized Conversations, Project Folders, File References, Memories, or Projects.
- Resolution excludes archived Projects unless explicitly requested.

#### FR-13: Resolve Project From Attachments

Chatbot can ask Projects to resolve Candidate Projects from attached Project Folder or File References. Realizes UJ-2.

Testable consequences:

- Matching considers existing Project Folder references and File References.
- Matching identifies `ProjectFolderMatched` or `FileReferenceMatched` Resolution Reason Codes when applicable.
- Matching fails closed when Project Folder or File Reference authorization is missing or stale.
- Matching never treats raw file contents as Project-owned data.

#### FR-14: Confirm Ambiguous Project

When Project Resolution returns multiple plausible Candidate Projects, Chatbot can present the candidates and record the user's confirmed choice.

Testable consequences:

- Projects does not silently attach a Conversation when the Resolution Result is `MultipleCandidates`.
- User confirmation creates or updates the Project-to-Conversation association.
- Rejected candidates are not linked.

#### FR-15: Propose New Project

When Project Resolution cannot find a suitable Project, Chatbot can propose creating a new Project using the current Conversation, attachments, and setup metadata.

Testable consequences:

- The proposal includes a suggested Project name and initial setup metadata when available.
- No Project is created from inference until authorized user action confirms creation.
- The created Project links the initiating Conversation and authorized attachments.

#### FR-16: Get Project Context

Chatbot can request the Project Context for a Project and receive the setup plus authorized references to Conversations, the Project Folder, File References, and Memories needed for conversation initialization. Realizes UJ-1 and UJ-4.

Testable consequences:

- Project Context is tenant-scoped and authorization-filtered.
- Project Context contains references and metadata, not full payloads owned by other bounded contexts.
- Project Context indicates which referenced resources were excluded because of authorization, lifecycle, or availability.

#### FR-17: Explain Context Selection

Chatbot can display or log metadata explaining why a Conversation, Project Folder, File Reference, or Memory reference was included or excluded from Project Context. Realizes UJ-4.

Testable consequences:

- Explanation metadata does not include secrets, file contents, transcript payloads, prompts, or memory payloads.
- Explanation supports troubleshooting incorrect context selection.

#### FR-18: Refresh Project Context

Chatbot can request a refreshed Project Context after links, setup, or resource availability changes.

Testable consequences:

- Refresh reflects current Project links and lifecycle state.
- Refresh preserves tenant authorization checks.
- Stale or unavailable references are surfaced rather than silently ignored.

#### FR-19: Validate Project Setup

Projects validates Project Setup before accepting create or update operations.

Testable consequences:

- Setup validation rejects raw secrets, unrestricted local paths, unsupported reference types, and payload data that belongs to Conversations, Folders, or Memories.
- Setup validation requires only a Project name as user-supplied creation input.
- Setup validation defaults Project Lifecycle State to `Active`.
- Setup validation permits the Project Folder to be supplied explicitly or created with the same name as the Project.
- Setup validation allows durable conversation guidance such as project goals, preferred tone, domain instructions, and context-source preferences.
- Validation failures return structured errors that identify the rejected setup field without echoing sensitive values.

#### FR-20: Retrieve Conversation-Start Setup

Chatbot can retrieve the subset of Project Setup needed to start or resume a Conversation.

Testable consequences:

- Conversation-start setup includes project goals, instructions, context preferences, and default linked-source policy.
- Conversation-start setup excludes internal audit metadata and unavailable or unauthorized references.
- Conversation-start setup is stable enough for Chatbot to use without re-querying every bounded context before the first response.

#### FR-21: Record Project Audit Events

Projects records metadata-only audit events for Project lifecycle and context-reference changes.

Testable consequences:

- Audit events are recorded for Project creation, setup updates, archival, Conversation linking, Conversation moves, Project Folder changes, File Reference linking/unlinking, Memory linking/unlinking, Project Resolution confirmation, and new Project creation from a proposal.
- Audit events include tenant, Project identity, operation type, timestamp, actor identity where available, and affected reference identifiers.
- Audit events do not include transcript payloads, file contents, raw prompts, secrets, or Memory payloads.

#### FR-22: Support Operator Read Access

Authorized operators can inspect Project metadata, lifecycle state, references, resolution outcomes, and audit metadata for troubleshooting.

Testable consequences:

- Operator read access is authorization-gated and tenant-scoped.
- Operator read access exposes metadata only.
- Operator read access does not provide write capabilities beyond archive and troubleshooting workflows explicitly exposed by Chatbot or generated/admin surfaces.

**Total FRs: 22**

### Non-Functional Requirements

#### NFR-1: Security and Privacy

Projects must enforce tenant isolation across reads, writes, links, resolution, and context assembly. Logs and diagnostics must remain metadata-only.

#### NFR-2: Reliability

Project Context retrieval should fail closed when authorization, lifecycle, or referenced-resource availability cannot be verified.

#### NFR-3: Observability

Project Resolution and Project Context assembly must emit structured metadata sufficient to troubleshoot incorrect matches without exposing payloads.

#### NFR-4: Performance

Project listing, Project opening, Project Resolution, and Project Context retrieval should target p95 under 500 ms when dependent bounded-context metadata is available. This is an internal service target, not a formal external SLA.

#### NFR-5: Compatibility

Public contracts should be additive and serialization-tolerant unless a breaking change is explicitly approved.

**Total NFRs: 5**

### Additional Requirements

- The product is a tenant-aware durable AI workspace boundary for `Hexalith.Chatbot`, not a generic project-management system or public standalone product in v1.
- Projects owns Project metadata, setup, lifecycle, associations, resolution, context assembly, and metadata-only audit evidence; Conversations, Folders, and Memories remain systems of record for their payloads and authorization boundaries.
- In v1, a Conversation belongs to exactly one Project, and moving it requires an explicit, user-confirmed operation.
- Every v1 Project requires exactly one canonical Project Folder. The folder may be supplied or requested with the same name during Project creation; it can be replaced explicitly and can only be absent after archival.
- File and Memory references are optional and remain stable identifiers plus relevant metadata only.
- Project Resolution has exactly three outcomes: `NoMatch`, `SingleCandidate`, and `MultipleCandidates`.
- Resolution reason codes are `ConversationLinked`, `ProjectFolderMatched`, `FileReferenceMatched`, `MemoryMatched`, and `MetadataMatched`.
- Lifecycle states are limited to `Active` and `Archived`.
- Ambiguous resolution requires confirmation, and inferred creation never occurs without authorized user confirmation.
- Projects must not store full transcripts, file contents, raw prompts, secrets, Memory payloads, or unrestricted filesystem paths.
- Projects must not bypass Dapr, Hexalith.EventStore, tenant isolation, or the authorization boundaries of dependent contexts.
- Cross-tenant sharing, full-text/semantic file retrieval, memory synthesis, transcript storage/summarization, standalone end-user UI, and generic project-management workflows are outside MVP scope.
- Context-selection explanations and operator surfaces are metadata-only and tenant-scoped.
- The success counter-metrics forbid optimizing automatic attachment rate or context volume at the expense of correctness, confirmation, relevance, security, or prompt quality.
- The PRD declares no open questions and records accepted decisions for internal-only v1 scope, setup boundaries, mandatory Project Folder behavior, confirmation before inferred creation, and the non-SLA nature of the latency target.

### PRD Completeness Assessment

The PRD is functionally strong: it has stable identifiers, explicit bounded-context ownership, testable consequences for all 22 FRs, clear MVP boundaries, four user journeys, success metrics, and no unresolved product questions. The core safety posture—tenant isolation, fail-closed authorization, confirmation for ambiguity, and metadata-only handling—is repeated consistently.

The NFR section is less implementation-ready than the functional section. Only performance has a quantitative target, and even that excludes the behavior expected when dependency metadata is slow or unavailable. The PRD does not define availability or recovery objectives, throughput or concurrency expectations, data-volume limits, audit retention, encryption requirements, authentication/authorization mechanisms, accessibility requirements, consistency/freshness tolerances, dependency timeouts, or explicit scalability targets. These omissions do not invalidate functional scope, but they require architecture or epic-level closure before implementation can be considered fully ready.

## Epic Coverage Validation

### Coverage Matrix

| FR | PRD requirement | Epic and story coverage | Status |
| --- | --- | --- | --- |
| FR-1 | Create a tenant-scoped Project from the Project name, optionally with setup/references and an auto-created same-name Project Folder. | Epic 1, Story 1.4 creates the Project; Epic 2, Story 2.4 completes the deferred auto-folder behavior. | Covered |
| FR-2 | Open a Project and return its authorized metadata, lifecycle, setup, and references. | Epic 1, Story 1.7 (`GetProject` over `ProjectDetailProjection`). | Covered |
| FR-3 | Update durable, safe, serialization-tolerant Project Setup. | Epic 1, Story 1.8 (`UpdateProjectSetup`). | Covered |
| FR-4 | Archive a Project while retaining discoverability and auditability and excluding it from automatic resolution. | Epic 1, Story 1.8 (`ArchiveProject`). | Covered |
| FR-5 | List authorized Active and Archived Projects with lifecycle filtering and summary metadata. | Epic 1, Story 1.7 (`ListProjects` over `ProjectListProjection`). | Covered |
| FR-6 | Link an authorized Conversation to exactly one Project without copying transcript content. | Epic 2, Story 2.1 provides the read ACL; Stories 2.2–2.3 provide Conversations-owned reassignment and the Projects link path. | Covered; upstream dependency |
| FR-7 | Move a Conversation between Projects after explicit confirmation, atomically and audibly. | Epic 2, Stories 2.2–2.3 provide the Conversations-owned reassignment and confirmed move flow. | Covered; upstream dependency |
| FR-8 | Set or explicitly replace the single authorized Project Folder. | Epic 2, Story 2.4 (`SetProjectFolder` plus Folders ACL). | Covered |
| FR-9 | Link optional authorized File References without replacing the Project Folder. | Epic 2, Story 2.5 (`LinkFileReference`). | Covered |
| FR-10 | Link authorized Memory references without storing Memory payloads. | Epic 2, Story 2.6 settles the Case-versus-MemoryUnit model; Story 2.7 implements `LinkMemory`. | Covered; decision dependency |
| FR-11 | Unlink Conversation, File, or Memory associations without deleting resources; replace but do not remove an active Project's Folder. | Epic 2, Stories 2.3, 2.4, 2.5, and 2.7 cover each reference type and the Folder rule. | Covered |
| FR-12 | Resolve candidate Projects from an unassigned Conversation with typed outcomes/reason codes and authorization filtering. | Epic 4, Story 4.1 supplies the engine; Story 4.2 supplies the Conversation flow. | Covered |
| FR-13 | Resolve candidate Projects from attached Folder/File references and fail closed on stale or missing authorization. | Epic 4, Story 4.3. | Covered |
| FR-14 | Present multiple candidates and persist only the user's confirmed Project choice. | Epic 4, Story 4.4 (`ConfirmProjectResolution`). | Covered |
| FR-15 | Propose, but do not inferentially create, a new Project; on confirmation link the initiating authorized context. | Epic 4, Story 4.5, consuming Epic 1 Story 1.4 and Epic 2 Story 2.3. | Covered |
| FR-16 | Assemble tenant-scoped Project Context from setup and authorized references, with exclusions surfaced. | Epic 3, Story 3.1 defines the allowlist policy; Story 3.2 implements `GetProjectContext`. | Covered |
| FR-17 | Explain metadata-only inclusion and exclusion decisions for context references. | Epic 3, Story 3.3 (`ExplainContextSelection`). | Covered |
| FR-18 | Refresh Project Context while preserving authorization and surfacing stale/unavailable references. | Epic 3, Story 3.4 (`RefreshProjectContext`). | Covered |
| FR-19 | Validate Project Setup on create/update and return safe, field-specific structured errors. | Epic 1, Stories 1.4 and 1.8. | Covered |
| FR-20 | Return the safe Project Setup subset needed to begin or resume a Conversation. | Epic 3, Story 3.5 over `ConversationStartSetupProjection`. | Covered |
| FR-21 | Record tenant-scoped, metadata-only audit events for all specified lifecycle/reference/resolution operations. | Epic 5, Story 5.1 (`ProjectAuditTimelineProjection`). | Covered |
| FR-22 | Provide authorized, tenant-scoped, metadata-only operator read access. | Epic 5, Story 5.2. | Covered |

### Missing Requirements

No PRD functional requirement is missing from the epics and stories document. No epic-only FR identifiers were found that lack a corresponding PRD requirement.

Coverage does rely on three explicitly planned prerequisites rather than hiding them:

- FR-1 auto-folder creation is completed in Epic 2 Story 2.4 and depends on the Folders create operation.
- FR-6 and FR-7 depend on the Conversations-owned reassignment capability in Epic 2 Story 2.2.
- FR-10 depends on the Memories linkage-model decision in Epic 2 Story 2.6.

These are sequencing and readiness risks, not traceability gaps.

### Coverage Statistics

- Total PRD FRs: 22
- FRs covered in epics: 22
- Missing PRD FRs: 0
- Epic-only FR identifiers: 0
- Coverage: 100%

## UX Alignment Assessment

### UX Document Status

Found and complete:

- Primary UX specification: `ux-design-specification.md` (status `complete`, 14 workflow steps).
- Supplementary visual exploration: `ux-design-directions.html` (six operational directions).

The chosen UX direction is a FrontComposer-composed Metadata Control Plane, supplemented by a Resolution Trace Workbench and Audit-First Maintenance. Its direct users are administrators, operators, developers, and MCP-assisted support workflows; the end-user Chatbot conversation experience is explicitly outside this module's direct UX scope.

### Alignment Strengths

- The UX correctly preserves the PRD's product boundary: Projects is a metadata control plane, not a generic project-management or content-browsing product.
- Metadata-only handling, tenant scope, safe denial, fail-closed states, explicit ambiguity, auditability, and reference-don't-own semantics are consistent across PRD, UX, and architecture.
- The UX operational views map to defined architectural read models: project list/detail, reference index, audit timeline, context/resolution evidence, warnings, and conversation-start setup.
- FrontComposer/Fluent composition, shared state vocabulary, Fluxor lifecycle, CLI/MCP/Web parity, structured output, accessibility, and test automation are all explicitly supported by the architecture.
- Responsive behavior and WCAG 2.2 AA are backed by FrontComposer/Fluent primitives plus bUnit, Playwright, and axe-core validation in the architecture and epics.
- The PRD's ambiguous-resolution and confirmation rules are reinforced consistently: no silent attachment and no inferred Project creation without authorized confirmation.

### Alignment Issues

#### High: Restore and re-evaluate behavior lacks a product/domain contract

The UX defines `restore`, `relink`, `unlink`, and `reevaluate` as mutating maintenance actions. The PRD defines archive and reference link/unlink behavior but does not define restoring an archived Project or the semantics of a state-changing re-evaluation. The architecture command/event catalog includes `ArchiveProject`/`ProjectArchived`, but no `RestoreProject`/`ProjectRestored` transition or re-evaluation command/event.

Before implementation, either remove these actions from MVP or add explicit PRD requirements, lifecycle invariants, authorization rules, commands/events, rejection outcomes, idempotency behavior, and audit consequences.

#### High: Historical resolution-trace UX conflicts with compute-on-demand architecture

The UX lets operators start from a “resolution case ID,” browse resolution cases, inspect traces later, and reconstruct evaluated inputs/candidates. The architecture explicitly makes resolution compute-on-demand, persists only `ProjectResolutionConfirmed`, and defers persisted trace history and `ProjectResolutionTraceProjection`.

Choose one contract before implementation:

- Make the workbench transient/recomputed, remove historical case navigation/IDs, and specify which safe inputs can reproduce a trace; or
- Add a metadata-only persisted trace model with tenant scope, retention, deletion, redaction, replay semantics, and authorization.

#### High: Canonical vocabulary drifts inside the supplementary prototype

The normative vocabulary is `ConversationLinked`, `ProjectFolderMatched`, `FileReferenceMatched`, `MemoryMatched`, and `MetadataMatched`, with the shared inclusion/lifecycle states. The HTML prototype also uses ungoverned alternatives such as `conversationReferenceMatched`, `folderReferenceMatched`, `FolderMatched`, `stale_reference`, `ambiguous_resolution`, `ReferenceFreshnessExpired`, and audit/event-looking names such as `ProjectFolderRelinked`, `ProjectOpened`, and `ArchiveDryRun` that are absent from the architecture catalog.

Mark all prototype values non-normative or update the prototype to the canonical contract. Where audit operation codes intentionally differ from domain events, define and map that vocabulary explicitly.

#### High: Current FrontComposer and hosting boundaries are not reflected

The architecture places UI-specific contracts under `Hexalith.Projects.Contracts/Ui` and plans Projects-owned `ServiceDefaults`, `Aspire`, and `AppHost` projects. Current workspace rules require domain modules to remain domain-centric, use the platform host/runtime, and keep Blazor/Fluent rendering contracts aligned with the current FrontComposer `Contracts`/`Contracts.UI` split.

The UX can still be delivered, but the architecture must be updated so the operational console composes through the platform/FrontComposer host without reintroducing module-owned hosting, telemetry, Dapr, or UI scaffolding. The contract dependency graph must also preserve the UI-clean FrontComposer kernel.

#### Medium: Primary Chatbot confirmation UX is an external dependency without a companion specification

PRD journeys UJ-2 and UJ-3 require Chatbot to present candidates, request confirmation, and propose creation. The Projects UX correctly declares the end-user Chatbot experience out of scope, but no selected artifact defines the consumer-owned presentation contract, accessible interaction, cancellation behavior, or error recovery.

Record this as a Chatbot-owned UX prerequisite or provide a minimal integration interaction contract so FR-14 and FR-15 are verifiable end to end.

#### Medium: Dense operational views lack UX-specific performance and volume budgets

The architecture carries the PRD's p95 under 500 ms target for list/open/resolution/context, but the UX adds high-volume grids, trace comparisons, audit timelines, exports, dashboards, and cross-surface views. Pagination, virtualization, maximum trace/reference/audit sizes, export limits, and interaction/render budgets are not specified. Pattern A cross-context reads are already identified as a performance risk.

#### Medium: Safe Diagnostic Export needs a normative schema

The UX requires Web download/copy, CLI output, and MCP resources for safe export. Architecture supplies the metadata-only invariant and leakage harness but no explicit export schema, size limit, deterministic ordering, versioning, authorization, or retention/no-retention rule. Define these before the three surfaces implement separate interpretations.

### Warnings

- UX documentation exists and is substantial; missing-UX is not a readiness problem.
- The UX specification is older than the current FrontComposer contract-boundary changes. Revalidate its component and package assumptions against the current platform before implementation.
- Accessibility coverage is strong on paper, but primary Chatbot confirmation/proposal accessibility remains outside the selected UX artifact.
- The supplementary HTML is a design-direction artifact, not production implementation guidance; its raw HTML/CSS and sample contract values must not be copied into the FrontComposer/Fluent implementation.

## Epic Quality Review

### Review Scope

Reviewed five epics and 38 stories against user-value focus, epic independence, story sizing, dependency direction, BDD acceptance criteria, starter/scaffold requirements, and FR traceability.

### Epic Compliance Summary

| Epic | User value | Independent of future epics | Story sizing | No forward dependencies | Acceptance criteria | FR traceability |
| --- | --- | --- | --- | --- | --- | --- |
| Epic 1: Project Workspace Foundation | Mixed: delivers CRUD, but six of nine stories are platform enablers | **Fail**: creates active Projects without the PRD-mandated Folder and defers completion to Epic 2 | Mixed; Stories 1.2 and 1.9 combine several independently verifiable concerns | **Fail**: Story 1.4's unauthorized-tenant AC relies on Story 1.6 | Generally testable, with invariant conflict | Complete |
| Epic 2: Context References | Pass: users gain linked context | **Fail unless external prerequisites are already released** | Mixed; Stories 2.2 and 2.6 are upstream/spike work, while 2.4 combines set and auto-create | No later-epic dependency, but unscheduled external gates remain | Mixed; Story 2.4 permits incomplete behavior | Complete |
| Epic 3: Project Context Assembly | Pass | Pass; consumes Epics 1–2 only | Pass overall | Pass | Strong, except one ambiguous fail-closed outcome | Complete |
| Epic 4: Project Resolution | Pass | Pass; consumes Epics 1–2 only | Pass overall | Pass | Happy paths are clear; multi-context recovery is incomplete | Complete |
| Epic 5: Operational Console & Audit | Pass at epic level | Pass with respect to later epics | **Fail** for Story 5.9; trailing hardening/verification stories are oversized | **Fail**: Story 5.9 requires the MCP surface introduced in Story 5.10 | Mixed; trace/export/dashboard/live-test criteria have gaps | Complete for PRD FRs, but adds unapproved scope |

No database/table-upfront violation was found. EventStore streams and projections are introduced with their first functional slices. The selected hybrid starter is addressed in Story 1.1, and greenfield build/CI setup appears early, but that starter no longer matches current domain-module boundaries.

### Critical Violations

#### C1. Epic 1 is not independently valid against the mandatory Project Folder invariant

Story 1.4 explicitly accepts creation without a Project Folder and defers auto-folder behavior to Epic 2 Story 2.4. The PRD states that v1 Projects require a Project Folder, while Epic 1 claims to deliver a standalone, active, complete workspace CRUD slice. This leaves an `Active` Project in a state the PRD says is invalid and makes Epic 1 depend on Epic 2 for correctness.

**Remediation:** Move folder supply/provisioning into the create-project vertical slice, or introduce and approve a non-Active provisioning state with explicit failure/recovery semantics. Do not claim Epic 1 standalone until the invariant holds.

#### C2. Story 1.4 has a forward dependency on Story 1.6

Story 1.4 requires missing/unauthorized tenant context to fail closed through the command flow, while Story 1.6 later introduces the local TenantAccessProjection and layered authorization chain that establishes that evidence. Story 1.4 cannot meet its own security AC independently in the declared order.

**Remediation:** Move Story 1.6 before the create tracer bullet, or include the minimum complete tenant-access/authz vertical in Story 1.4 and leave only hardening/expansion for a later story.

#### C3. Story 2.4 is gated on unscheduled external behavior and permits false completion

The story promises “Set & auto-create Project Folder” but depends on Folders `CreateFolder` exposure outside this plan. Its AC allows the operation to “degrade gracefully” by queuing/flagging creation while still treating the story as satisfied. That is not the promised outcome and leaves FR-1/FR-8/FR-11 only partially implemented.

**Remediation:** Make the exact Folders version/endpoint a verified entry criterion, schedule the upstream story before 2.4, and split `SetProjectFolder` from `AutoCreateProjectFolder`. A blocked prerequisite must block the auto-create story rather than redefine success.

#### C4. Story 5.9 is epic-sized and forward-dependent

Story 5.9 introduces four maintenance operations, an eight-state preview/dry-run panel, a five-state asynchronous command lifecycle, validation, authorization, audit evidence, and MCP parity. It depends on the MCP surface in later Story 5.10 and on restore/re-evaluate domain contracts that do not exist in the PRD or architecture.

**Remediation:** First approve the missing product/domain requirements. Then split by operation and shared lifecycle foundation; build shared contracts before Web/MCP/CLI adapters. No story may require a later story's surface to pass.

### Major Issues

#### M1. Epic 1 is dominated by technical milestones

The epic does eventually deliver user value, but Stories 1.1, 1.2, 1.3, 1.5, 1.6, and 1.9 are platform/scaffold/governance milestones. Story 1.2 alone combines identifiers, shared vocabulary, rejection taxonomy, payload classification, and canonical identity derivation.

**Remediation:** Rename the epic around the user outcome (“Create and manage tenant-isolated Projects”) and attach each enabler to the smallest vertical slice that proves it. Split Story 1.2 into independently testable contract/safety foundations if it cannot be completed and reviewed as one small story.

#### M2. Story 1.1 and Story 1.9 use an obsolete module scaffold

They require Projects-owned `ServiceDefaults`, `Aspire`, and `AppHost` projects and a broad filtered `dotnet test` lane. Current workspace rules require domain-centric modules to consume platform hosting and use project-appropriate test lanes. The acceptance criteria therefore encode a now-invalid delivery structure.

**Remediation:** Rebase the starter story on the current EventStore domain-service SDK and platform AppHost, remove duplicated hosting/runtime projects, and align validation commands with current repository rules.

#### M3. Story 2.2 prerequisite status is contradictory

The document labels AR-G1/PR-1 “resolved” while retaining Story 2.2 as an unimplemented upstream capability and making Story 2.3 depend on it. The plan does not identify a released version, commit, contract test, or completion state.

**Remediation:** If released, replace Story 2.2 with a verified dependency baseline and consumer-contract evidence. If not released, mark Epic 2 blocked and schedule the upstream work explicitly before Projects implementation.

#### M4. Story 2.6 is a stale decision spike

The story asks whether Memories links target a Case or MemoryUnit, but the current architecture says the ADR resolved the target as a Memories Case. Leaving the spike in the implementation sequence creates churn and ambiguity.

**Remediation:** Remove the spike and make the approved ADR plus compatible dependency version an entry condition for Story 2.7, or reopen the architecture decision explicitly if it is no longer accepted.

#### M5. Cross-context workflows lack failure and recovery acceptance criteria

Stories 2.3, 4.4, and 4.5 coordinate Conversations assignment, Project events, Project creation, and attachment linking. Their ACs cover success and authorization denial but not partial completion, retry with the same idempotency key, expected-source conflict, compensation, or recovery after a downstream failure.

**Remediation:** Define orchestration ownership and add a failure matrix proving deterministic recovery, no duplicate membership, no duplicate Project, and auditable outcomes for each boundary failure.

#### M6. Story 5.1 cannot yet source every promised audit operation

Conversation link/move ownership remains in Conversations, while the Project audit projection is described as deriving from EventStore envelope metadata plus Project events. The story promises Project audit entries for link/move without defining a subscription, ACL audit receipt, or Projects event that supplies them.

**Remediation:** Define the cross-context audit-evidence contract and idempotent ingestion path, or narrow FR-21 ownership and document where operators retrieve the Conversations-owned evidence.

#### M7. Story 5.6 has no implementable resolution-trace data source

The story assumes a resolution case and evaluated-input/candidate trace, while Epic 4 persists no trace and the architecture defers trace history.

**Remediation:** Resolve the transient-versus-persisted trace decision before scheduling this story and add an explicit query contract, authorization model, data limits, and test fixtures.

#### M8. Story 5.7 lacks a Safe Diagnostic Export contract

The AC promises Web, CLI, and MCP export without a canonical schema, size limits, field ordering, version, or deterministic leakage test corpus.

**Remediation:** Add a contract-first export story or make the schema and conformance tests explicit prerequisites.

#### M9. Story 5.8 has vague, non-measurable dashboard criteria

“Aggregated health/status tiles” does not specify which metrics, calculation rules, freshness, time window, pagination, empty state, or degraded state constitutes success.

**Remediation:** Define each metric and source projection, tenant aggregation rules, freshness behavior, and exact expected outcomes.

#### M10. Accessibility, tenant isolation, and leakage are deferred to trailing hardening

Story 5.11 performs the main accessibility/security pass only after Stories 5.3–5.10. Story 5.12 then defers live topology evidence again. Safety and accessibility cannot be bolted on after each surface is considered complete.

**Remediation:** Put baseline WCAG, keyboard, tenant-isolation, and NoPayloadLeakage ACs on every UI/MCP/CLI story. Retain Story 5.11 only for cross-surface conformance, not first compliance. Run live evidence incrementally.

#### M11. Story 5.12 can pass with no live product evidence

Its AC allows live tests to skip whenever routes, deterministic fixtures, authentication, or product prerequisites are unavailable, provided a blocker is recorded. That is useful diagnostic behavior but not an acceptance gate for a story promising executable live verification.

**Remediation:** Separate hermetic fixture-contract tests from the live release gate. The live story is complete only when required routes/fixtures exist and the defined critical cases execute; otherwise it remains blocked.

### Minor Concerns

- Epic titles such as “Project Workspace Foundation,” “Context References,” and “Project Context Assembly” emphasize system structure more than user outcomes. Rename them around what Chatbot/operators can accomplish.
- The repeated `Standalone: Yes` label is misleading for Epic 5, which explicitly requires all earlier epics. The relevant property is “no dependency on future epics.”
- Story 3.2 allows either reference exclusion or whole-request denial for the same unverifiable-evidence condition. Point to explicit decision-matrix cells so each input has one expected result.
- Several acceptance criteria contain long chains of `And` clauses spanning multiple independently testable behaviors. Split them into named scenarios to make failure ownership clear.
- The plan does not state dependency readiness gates as versions/commits/contract-test results, making “resolved” external work difficult to verify.

### Recommended Restructuring Order

1. Reconcile the architecture and starter with current domain-service/FrontComposer boundaries.
2. Close or version-pin all upstream prerequisites before scheduling dependent stories.
3. Rebuild Epic 1 as a valid vertical slice that includes tenant authorization and the mandatory Folder invariant.
4. Split folder set versus auto-create, remove the resolved Memories spike, and add cross-context recovery ACs.
5. Approve or remove restore, re-evaluate, historical trace, and export scope before Epic 5.
6. Split Story 5.9 and move security/accessibility/leakage criteria into every surface story.
7. Make live evidence a real completion gate with provisioned routes and deterministic fixtures.

## Summary and Recommendations

### Overall Readiness Status

**NOT READY**

The planning set has excellent functional traceability—22 of 22 PRD FRs are mapped—but traceability alone does not make it implementable. The current stories violate a mandatory domain invariant, contain forward and external dependencies that can masquerade as completed work, introduce product behavior absent from the PRD/architecture, and target hosting/FrontComposer boundaries that no longer match current workspace rules.

Implementation should not start from the current Epic 1 sequence. Correct the artifacts first, then rerun readiness validation.

### Critical Issues Requiring Immediate Action

1. **Restore the mandatory Folder invariant.** An `Active` Project cannot be allowed to exist without its required Project Folder while Epic 1 claims standalone completeness.
2. **Remove forward dependency in Story 1.4.** Tenant authorization must precede or be part of the create-project tracer bullet, not arrive in Story 1.6.
3. **Close the Folders prerequisite before auto-create work.** Story 2.4 must not pass by “degrading gracefully” without delivering its promised outcome.
4. **Reconcile architecture with current platform rules.** Remove module-owned AppHost/Aspire/ServiceDefaults/runtime duplication and align UI contracts with the current FrontComposer kernel/Contracts.UI boundary.
5. **Resolve unapproved Epic 5 scope.** Approve or remove restore, re-evaluate, historical resolution cases/traces, and safe-export behavior before stories implement them.
6. **Split and reorder Story 5.9.** Define domain contracts first, remove its dependency on later Story 5.10, and divide operations into independently completable slices.

### Recommended Next Steps

1. Run a planning correction pass over the PRD, architecture, UX, and epics together. Record explicit decisions for restore/re-evaluate, transient versus persisted traces, and consumer-owned Chatbot confirmation UX.
2. Update the architecture and starter tree to the current EventStore domain-service SDK, platform-host composition, FrontComposer contract split, and current validation/test-lane rules.
3. Verify every upstream dependency with an exact released version or commit and consumer-contract evidence: Conversations reassignment/list, Folders create, and the approved Memories Case linkage.
4. Rebuild Epic 1 around one valid vertical outcome: authorized Project creation with the Folder invariant satisfied, followed by open/list/update/archive slices.
5. Split Epic 2's set-folder and auto-create behavior, remove the resolved Memories spike, and add deterministic cross-context retry/compensation criteria to Stories 2.3, 4.4, and 4.5.
6. Define the audit-evidence source for Conversations-owned link/move operations and the canonical schemas for trace, export, dashboard metrics, and shared reason/audit codes.
7. Restructure Epic 5 so each surface story includes baseline WCAG, tenant-isolation, metadata-leakage, and live-evidence criteria; retain final stories only for cross-surface conformance.
8. Complete the missing NFR decisions: availability/recovery, concurrency and volume limits, consistency/freshness, dependency timeouts, encryption, audit retention, accessibility ownership, and operational UX performance budgets.
9. Rerun implementation-readiness validation after the revised artifacts and prerequisite evidence are committed.

### Final Note

This assessment records 27 issue entries across UX alignment and epic quality—4 critical violations, 11 major issues, 5 minor concerns, and 7 UX alignment gaps—with some overlap where the same defect crosses artifacts. It also identifies a separate PRD NFR-completeness gap.

The product intent is coherent, the core safety posture is strong, and FR coverage is complete. The blocker is planning consistency and executable sequencing, not lack of vision. Address the critical issues before Phase 4 implementation.

**Assessment date:** 2026-07-14  
**Assessor:** Codex, implementation-readiness and requirements-traceability review
