---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
readinessStatus: NOT_READY
completedAt: 2026-07-14
assessor: Codex
filesIncluded:
  prd: prds/prd-Hexalith.Projects-2026-05-24/prd.md
  architecture: architecture.md
  epics: epics.md
  ux: ux-design-specification.md
supplementalFiles:
  - ux-design-directions.html
---

# Implementation Readiness Assessment Report

**Date:** 2026-07-14
**Project:** projects

## Document Inventory

### PRD

- Whole document: `prds/prd-Hexalith.Projects-2026-05-24/prd.md` (23,936 bytes; modified 2026-05-29 07:59)
- Sharded documents: None

### Architecture

- Whole document: `architecture.md` (51,169 bytes; modified 2026-07-14 15:27)
- Sharded documents: None

### Epics and Stories

- Whole document: `epics.md` (110,516 bytes; modified 2026-07-14 22:33)
- Sharded documents: None

### UX Design

- Whole document: `ux-design-specification.md` (53,895 bytes; modified 2026-05-29 07:59)
- Supplemental artifact: `ux-design-directions.html` (25,390 bytes; modified 2026-05-29 07:59)
- Sharded documents: None

### Discovery Issues

- Duplicate whole and sharded formats: None
- Missing required document categories: None
- Selected assessment inputs: PRD, architecture, epics, and UX Markdown documents listed above

## PRD Analysis

### Functional Requirements

#### FR1: Create Project

Chatbot can create a Project with tenant context and Project name as the required inputs. Project name is the only required user-supplied field. Description, initial setup, initial references to Conversations, File References, and Memories are optional. If no Project Folder is supplied, Chatbot can request creation of a Project Folder with the same name. Realizes UJ-2 and UJ-3.

Testable consequences:

- Creating a Project records durable Project metadata and sets Project Lifecycle State to `Active`.
- Creating a Project requires only a Project name from the user.
- Creating a Project can create or attach a Project Folder with the same name as the Project when no folder is supplied.
- Creating a Project does not duplicate conversation transcripts, file contents, or memory payloads.
- Creating a Project fails closed when tenant context is missing or unauthorized.

#### FR2: Open Project

Chatbot can open a Project and receive the Project metadata, lifecycle state, setup, and authorized references needed to initialize a conversation. Realizes UJ-1.

Testable consequences:

- Opening a Project returns only references visible to the requesting tenant/user context.
- Archived or unavailable Projects are clearly identified and cannot silently become active conversation context.

#### FR3: Update Project Setup

Chatbot can update Project Setup needed for conversation continuity, including project instructions, context preferences, and configuration metadata. Realizes UJ-1.

Testable consequences:

- Setup updates are durable and available to later conversations.
- Setup can include project goals, user-facing instructions, preferred context sources, source inclusion/exclusion preferences, conversation-start defaults, and metadata needed to resume the Project.
- v1 Project Setup describes conversation behavior and context policy, not model-provider internals.
- Setup updates preserve additive, serialization-tolerant contract behavior.
- Setup updates do not allow raw secrets, unrestricted file paths, or payload data that belongs to another bounded context.

#### FR4: Archive Project

Chatbot or authorized operators can archive a Project so it remains discoverable for history but is no longer selected as active context by default.

Testable consequences:

- v1 Project Lifecycle State is limited to `Active` and `Archived`.
- Archived Projects are excluded from automatic Project Resolution unless explicitly requested.
- Existing references remain auditable after archival.

#### FR5: List Projects

Chatbot can list Active and Archived Projects visible to the requesting tenant/user context.

Testable consequences:

- List results are tenant-scoped and authorization-filtered.
- List results include enough metadata for Chatbot to present Project choices without loading full Project Context.
- List results can filter by Project Lifecycle State.

#### FR6: Link Conversation

Chatbot can link an existing Conversation to a Project. Realizes UJ-1 and UJ-3.

Testable consequences:

- The link records stable Conversation identity and relevant metadata.
- A Conversation can be linked to only one Project in v1.
- Linking a Conversation that already belongs to another Project requires an explicit move operation rather than creating a second membership.
- The link does not copy transcript content into Projects.
- The link fails if tenant authorization for the Conversation cannot be established.

#### FR7: Move Conversation Between Projects

Chatbot can move a Conversation from one Project to another when the user explicitly confirms the move.

Testable consequences:

- Moving a Conversation removes the prior Project membership before creating the new one.
- Moving a Conversation is auditable as metadata.
- Moving a Conversation fails closed when authorization to either Project or the Conversation cannot be established.

#### FR8: Set Project Folder

Chatbot can set the single authorized Project Folder for a Project. Realizes UJ-2.

Testable consequences:

- A Project has exactly one Project Folder in v1.
- Setting a Project Folder records stable Folder identity and relevant metadata.
- Setting a new Project Folder replaces the previous Project Folder only through an explicit update.
- The Project Folder reference does not store file contents or unrestricted filesystem paths in Projects.
- Folder authorization remains delegated to `Hexalith.Folders`.

#### FR9: Link File Reference

Chatbot can link authorized File References to a Project when a file should be part of Project Context without changing the Project Folder.

Testable consequences:

- File References are optional and do not replace the Project Folder.
- A File Reference records stable File identity and relevant metadata.
- File authorization remains delegated to `Hexalith.Folders`.

#### FR10: Link Memory

Chatbot can link authorized Memory references to a Project. Realizes UJ-1 and UJ-3.

Testable consequences:

- The link records stable Memory identity and relevant metadata.
- The link does not store Memory payloads in Projects.
- Memory authorization remains delegated to `Hexalith.Memories`.

#### FR11: Unlink Context Reference

Chatbot can remove a Conversation, File Reference, or Memory reference from a Project without deleting the underlying resource. The Project Folder can be replaced but not removed unless the Project is archived. v1 Projects require a Project Folder.

Testable consequences:

- Unlinking removes the association from Project Context.
- Unlinking does not delete the underlying Conversation, File Reference, or Memory.
- Unlinking is auditable as metadata.

#### FR12: Resolve Project From Conversation

Chatbot can ask Projects to resolve Candidate Projects for a Conversation that has no explicit Project. Realizes UJ-3.

Testable consequences:

- Resolution returns a Resolution Result of `NoMatch`, `SingleCandidate`, or `MultipleCandidates`.
- Candidate Projects include one or more Resolution Reason Codes.
- Resolution does not access unauthorized Conversations, Project Folders, File References, Memories, or Projects.
- Resolution excludes archived Projects unless explicitly requested.

#### FR13: Resolve Project From Attachments

Chatbot can ask Projects to resolve Candidate Projects from attached Project Folder or File References. Realizes UJ-2.

Testable consequences:

- Matching considers existing Project Folder references and File References.
- Matching identifies `ProjectFolderMatched` or `FileReferenceMatched` Resolution Reason Codes when applicable.
- Matching fails closed when Project Folder or File Reference authorization is missing or stale.
- Matching never treats raw file contents as Project-owned data.

#### FR14: Confirm Ambiguous Project

When Project Resolution returns multiple plausible Candidate Projects, Chatbot can present the candidates and record the user's confirmed choice.

Testable consequences:

- Projects does not silently attach a Conversation when the Resolution Result is `MultipleCandidates`.
- User confirmation creates or updates the Project-to-Conversation association.
- Rejected candidates are not linked.

#### FR15: Propose New Project

When Project Resolution cannot find a suitable Project, Chatbot can propose creating a new Project using the current Conversation, attachments, and setup metadata.

Testable consequences:

- The proposal includes a suggested Project name and initial setup metadata when available.
- No Project is created from inference until authorized user action confirms creation.
- The created Project links the initiating Conversation and authorized attachments.

#### FR16: Get Project Context

Chatbot can request the Project Context for a Project and receive the setup plus authorized references to Conversations, the Project Folder, File References, and Memories needed for conversation initialization. Realizes UJ-1 and UJ-4.

Testable consequences:

- Project Context is tenant-scoped and authorization-filtered.
- Project Context contains references and metadata, not full payloads owned by other bounded contexts.
- Project Context indicates which referenced resources were excluded because of authorization, lifecycle, or availability.

#### FR17: Explain Context Selection

Chatbot can display or log metadata explaining why a Conversation, Project Folder, File Reference, or Memory reference was included or excluded from Project Context. Realizes UJ-4.

Testable consequences:

- Explanation metadata does not include secrets, file contents, transcript payloads, prompts, or memory payloads.
- Explanation supports troubleshooting incorrect context selection.

#### FR18: Refresh Project Context

Chatbot can request a refreshed Project Context after links, setup, or resource availability changes.

Testable consequences:

- Refresh reflects current Project links and lifecycle state.
- Refresh preserves tenant authorization checks.
- Stale or unavailable references are surfaced rather than silently ignored.

#### FR19: Validate Project Setup

Projects validates Project Setup before accepting create or update operations.

Testable consequences:

- Setup validation rejects raw secrets, unrestricted local paths, unsupported reference types, and payload data that belongs to Conversations, Folders, or Memories.
- Setup validation requires only a Project name as user-supplied creation input.
- Setup validation defaults Project Lifecycle State to `Active`.
- Setup validation permits the Project Folder to be supplied explicitly or created with the same name as the Project.
- Setup validation allows durable conversation guidance such as project goals, preferred tone, domain instructions, and context-source preferences.
- Validation failures return structured errors that identify the rejected setup field without echoing sensitive values.

#### FR20: Retrieve Conversation-Start Setup

Chatbot can retrieve the subset of Project Setup needed to start or resume a Conversation.

Testable consequences:

- Conversation-start setup includes project goals, instructions, context preferences, and default linked-source policy.
- Conversation-start setup excludes internal audit metadata and unavailable or unauthorized references.
- Conversation-start setup is stable enough for Chatbot to use without re-querying every bounded context before the first response.

#### FR21: Record Project Audit Events

Projects records metadata-only audit events for Project lifecycle and context-reference changes.

Testable consequences:

- Audit events are recorded for Project creation, setup updates, archival, Conversation linking, Conversation moves, Project Folder changes, File Reference linking/unlinking, Memory linking/unlinking, Project Resolution confirmation, and new Project creation from a proposal.
- Audit events include tenant, Project identity, operation type, timestamp, actor identity where available, and affected reference identifiers.
- Audit events do not include transcript payloads, file contents, raw prompts, secrets, or Memory payloads.

#### FR22: Support Operator Read Access

Authorized operators can inspect Project metadata, lifecycle state, references, resolution outcomes, and audit metadata for troubleshooting.

Testable consequences:

- Operator read access is authorization-gated and tenant-scoped.
- Operator read access exposes metadata only.
- Operator read access does not provide write capabilities beyond archive and troubleshooting workflows explicitly exposed by Chatbot or generated/admin surfaces.

**Total FRs: 22**

### Non-Functional Requirements

#### NFR1: Security and Privacy

Projects must enforce tenant isolation across reads, writes, links, resolution, and context assembly. Logs and diagnostics must remain metadata-only.

#### NFR2: Reliability

Project Context retrieval should fail closed when authorization, lifecycle, or referenced-resource availability cannot be verified.

#### NFR3: Observability

Project Resolution and Project Context assembly must emit structured metadata sufficient to troubleshoot incorrect matches without exposing payloads.

#### NFR4: Performance

Project listing, Project opening, Project Resolution, and Project Context retrieval should target p95 under 500 ms when dependent bounded-context metadata is available. This is an internal service target, not a formal external SLA.

#### NFR5: Compatibility

Public contracts should be additive and serialization-tolerant unless a breaking change is explicitly approved.

**Total NFRs: 5**

### Additional Requirements

#### Product and Scope Constraints

- v1 is an internal platform and near-term implementation specification for `Hexalith.Chatbot`, not a public standalone product launch or a generic project-management system.
- The module must support the four defined user journeys: resume an existing project conversation, start with files before choosing a project, start without an explicit project, and protect unrelated work from context leakage.
- MVP scope includes project identity, metadata, setup, lifecycle, tenant scoping, bounded-context references, project resolution, candidate confirmation, new-project proposals, project listing/context retrieval, setup validation, conversation-start setup, metadata-only audit/diagnostics, operator reads, and archive behavior.
- MVP excludes full-text or semantic retrieval over file contents, memory payload storage or synthesis, transcript storage or summarization, standalone end-user UI outside Chatbot or generated/admin surfaces, generic project-management workflows, and cross-tenant project sharing.

#### Bounded-Context and Integration Constraints

- `Hexalith.Conversations` remains the conversation system of record; Projects stores stable Conversation identifiers and metadata, never full transcripts.
- `Hexalith.Folders` remains the Project Folder and File Reference system of record and authorization boundary; Projects stores stable references and never file contents or unrestricted filesystem paths.
- `Hexalith.Memories` remains the memory system of record; Projects stores stable Memory references and never Memory payloads.
- Projects must not bypass Dapr, `Hexalith.EventStore`, or established tenant-isolation patterns.
- A Conversation belongs to exactly one Project in v1; changing membership requires an explicit, confirmed move.
- Every v1 Project requires exactly one Project Folder. It may be provided or created automatically with the Project name; it may be explicitly replaced and may only be absent after archival.
- Authorization for Conversations, folders/files, and memories remains delegated to their owning bounded contexts and must fail closed when missing, stale, or unverifiable.

#### Data, Security, and Operational Constraints

- Projects must never store full conversation transcripts, file contents, raw prompts, secrets, memory payloads, or unrestricted filesystem paths.
- Context assembly must include only authorized, linked, or confirmed references and must surface exclusions caused by authorization, lifecycle, staleness, or availability.
- Resolution has exactly three v1 results (`NoMatch`, `SingleCandidate`, and `MultipleCandidates`) and five reason codes (`ConversationLinked`, `ProjectFolderMatched`, `FileReferenceMatched`, `MemoryMatched`, and `MetadataMatched`).
- Ambiguous resolution must obtain user confirmation; inference alone must never create a Project.
- Lifecycle is limited to `Active` and `Archived`; archived Projects remain historically discoverable and auditable but are excluded from automatic resolution by default.
- Audit and diagnostic outputs are metadata-only and must not reveal payloads or sensitive values.
- Structured validation errors identify the rejected setup field without echoing its sensitive value.
- Public contracts and setup updates must evolve additively and remain serialization-tolerant unless an explicitly approved breaking change applies.

#### Success and Counter-Metric Constraints

- Active Projects must yield usable Project Context under normal operation.
- Resolution must yield either a correct candidate or a useful new-project proposal.
- Project Context must never contain unauthorized or unrelated tenant/project references.
- Interactive list, open, resolution, and context-retrieval operations target p95 under 500 ms in normal internal operation.
- Automatic attachment rate must not be optimized at the expense of correctness; ambiguity requires confirmation.
- Context volume must not be optimized at the expense of relevance, security, or prompt quality.

### PRD Completeness Assessment

The PRD is structurally strong for functional scope: it is final, defines stable IDs for 22 functional requirements, attaches testable consequences to each, names bounded-context ownership, states explicit non-goals and MVP boundaries, defines user journeys and success metrics, records accepted planning decisions, and reports no open questions. The functional requirements are generally clear enough for epic-level traceability.

The NFR section is materially thinner than the functional section. Only performance has a numeric target. Security/privacy, reliability, observability, and compatibility are directionally clear but lack measurable acceptance thresholds or operational proof criteria. The PRD does not explicitly define availability/SLO targets, recovery objectives, data and audit retention, concurrency or scale envelopes, rate limits/back-pressure, authentication/role policy, encryption/key-management requirements, compliance classifications, accessibility requirements for generated/admin surfaces, or how p95 is measured when bounded-context dependencies are degraded. These omissions do not erase the stated requirements, but they must be supplied by architecture, UX, epics, or explicit follow-up decisions before implementation readiness can be considered complete.

## Epic Coverage Validation

### Epic FR Coverage Extracted

The epics document contains an explicit FR Coverage Map and states that all 22 PRD FRs are mapped. The primary allocation is:

- Epic 1 — FR1, FR2, FR3, FR4, FR5, FR19
- Epic 2 — FR6, FR7, FR8, FR9, FR10, FR11
- Epic 3 — FR16, FR17, FR18, FR20
- Epic 4 — FR12, FR13, FR14, FR15
- Epic 5 — FR21, FR22
- Corrective Epics 6–8 — migration, durability, conformance, and release-evidence work that reinforces selected existing FRs; the addendum does not replace the original FR allocation

**Total PRD FRs claimed in epics: 22**

### Coverage Matrix

| FR Number | PRD Requirement | Epic and Story Coverage | Status |
| --- | --- | --- | --- |
| FR1 | Create Project | Epic 1, Story 1.4; folder completion in Epic 2, Story 2.4; durable mandatory-folder correction in Epic 7, Story 7.3 | ✓ Covered |
| FR2 | Open Project | Epic 1, Story 1.7; supported read-model/query migration reinforced by Epic 6, Stories 6.4 and 6.7 | ✓ Covered |
| FR3 | Update Project Setup | Epic 1, Story 1.8 | ✓ Covered |
| FR4 | Archive Project | Epic 1, Story 1.8; operational action in Epic 5, Story 5.9; durable-task migration in Epic 7, Story 7.6 | ✓ Covered |
| FR5 | List Projects | Epic 1, Story 1.7 | ✓ Covered |
| FR6 | Link Conversation | Epic 2, Stories 2.1–2.3; durable assignment correction in Epic 7, Story 7.4 | ✓ Covered |
| FR7 | Move Conversation Between Projects | Epic 2, Stories 2.2–2.3; durable move correction in Epic 7, Story 7.4 | ✓ Covered |
| FR8 | Set Project Folder | Epic 2, Story 2.4; durable creation/folder enforcement in Epic 7, Story 7.3 | ✓ Covered |
| FR9 | Link File Reference | Epic 2, Story 2.5 | ✓ Covered |
| FR10 | Link Memory | Epic 2, Stories 2.6–2.7 | ✓ Covered |
| FR11 | Unlink Context Reference | Epic 2, Stories 2.3, 2.4, 2.5, and 2.7; maintenance flow in Epic 5, Story 5.9; durable-task migration in Epic 7, Story 7.6 | ✓ Covered |
| FR12 | Resolve Project From Conversation | Epic 4, Stories 4.1–4.2 | ✓ Covered |
| FR13 | Resolve Project From Attachments | Epic 4, Stories 4.1 and 4.3 | ✓ Covered |
| FR14 | Confirm Ambiguous Project | Epic 4, Story 4.4; bound confirmation evidence in Epic 7, Stories 7.2 and 7.5 | ✓ Covered |
| FR15 | Propose New Project | Epic 4, Story 4.5; durable proposal confirmation in Epic 7, Story 7.5 | ✓ Covered |
| FR16 | Get Project Context | Epic 3, Stories 3.1–3.2 | ✓ Covered |
| FR17 | Explain Context Selection | Epic 3, Story 3.3 | ✓ Covered |
| FR18 | Refresh Project Context | Epic 3, Story 3.4; reevaluation remains a read/refresh diagnostic in Epic 7, Story 7.6 | ✓ Covered |
| FR19 | Validate Project Setup | Epic 1, Stories 1.4 and 1.8 | ✓ Covered |
| FR20 | Retrieve Conversation-Start Setup | Epic 3, Story 3.5 | ✓ Covered |
| FR21 | Record Project Audit Events | Epic 5, Stories 5.1 and 5.9; durable workflow audit receipts reinforced by Epic 7, Stories 7.4–7.6 | ✓ Covered |
| FR22 | Support Operator Read Access | Epic 5, Story 5.2, with operator surfaces delivered by Stories 5.3–5.11 | ✓ Covered |

### Missing Requirements

No PRD functional requirement is absent from the epics document's explicit coverage map or story inventory.

### Epic-Only Requirement References

- **FR23 — undefined/orphan reference:** The corrective Epic 7 completion gate says it provides restart-safe evidence for `FR-23`, but the PRD, the epics Requirements Inventory, and the FR Coverage Map define only FR1–FR22. No FR23 requirement text or owning story is identified.
  - Impact: Traceability tools and reviewers cannot determine whether this is a missing approved product requirement, a numbering error, or an additional requirement that should have an AR/NFR identifier.
  - Recommendation: Either add an approved FR23 to the PRD and map it explicitly through the requirements inventory, coverage map, and stories, or replace `FR-23` in the addendum with the intended existing FR/AR/NFR reference.

### Coverage Statistics

- Total PRD FRs: 22
- PRD FRs covered in epics: 22
- PRD FRs missing from epics: 0
- Epic-only undefined FR references: 1 (`FR23`)
- PRD FR coverage percentage: 100%

This result validates explicit requirement allocation only. It does not yet validate story quality, acceptance-criteria sufficiency, internal consistency, or implementation readiness; those are assessed in later workflow steps.

## UX Alignment Assessment

### UX Document Status

**Found and complete:** `ux-design-specification.md` is marked complete through step 14. The supplemental `ux-design-directions.html` records the explored visual directions. The chosen direction is a FrontComposer-based **Metadata Control Plane**, augmented by a **Resolution Trace Workbench** and **Audit-First Maintenance**.

The UX scope is intentionally administrative and operational. The direct users are administrators, operators, developers, and MCP-assisted support workflows across Web, CLI, and MCP. The primary end-user conversation experience remains owned by `Hexalith.Chatbot` and is explicitly outside this module's direct UX scope.

### Alignment Strengths

| Area | UX ↔ PRD Alignment | UX ↔ Architecture Alignment | Result |
| --- | --- | --- | --- |
| Product boundary | UX treats Projects as a metadata control plane rather than a project-management product, matching the PRD vision and non-goals | Architecture enforces reference-don't-own boundaries and metadata-only contracts | Aligned |
| Tenant and payload safety | UX requires tenant-visible scope, fail-closed states, safe reason codes, and no transcripts/files/prompts/memory payloads | Architecture provides layered authorization, safe-denial, canonical tenant identity, allowlist assembly, and `NoPayloadLeakage` tests | Aligned |
| Operational surfaces | UX requires one operational model across Web, CLI, and MCP | Architecture uses shared contracts/vocabulary and generated surfaces to enforce parity | Aligned conceptually |
| Web technology | UX requires FrontComposer plus Fluent UI Blazor and minimal custom composition | Architecture specifies generated FrontComposer views, Fluxor, Blazor, and Fluent UI | Aligned conceptually |
| Core views | UX defines project inventory/detail, reference health, resolution trace, audit timeline, warnings queue, dashboard, maintenance, and safe export | Architecture maps list/detail/reference/audit/warnings/dashboard projection roles and identifies resolution trace as the one Level 3/4 customization candidate | Mostly aligned |
| Eventual consistency | UX distinguishes submit, acknowledge, syncing, confirmation, rejection, freshness, and degraded states | Architecture defines command-async `202`, freshness-bearing reads, SignalR nudge-only semantics, and re-query confirmation | Aligned |
| Accessibility | UX targets WCAG 2.2 AA, keyboard access, focus management, semantic tables/timelines, reduced motion, and non-color-only state | Architecture specifies WCAG 2.2 AA with axe-core/Playwright and FrontComposer/Fluent primitives | Aligned |
| Responsive design | UX defines mobile/tablet/desktop/wide behavior and visibility constraints | Architecture provides responsive FrontComposer/Fluent composition and Epic 5 verification hooks | Supported at planning level |
| Auditability | UX requires preview/confirmation and metadata-only evidence after mutations | Architecture derives audit from EventStore metadata/events and provides an audit projection | Partially aligned; durable confirmation gaps remain |

### Alignment Issues

#### UX-A1 — Primary Chatbot journeys have no linked UX/handoff specification (High)

The PRD's primary persona and UJ-2/UJ-3 require Chatbot to present candidates, request ambiguous-project confirmation, propose a new Project, and confirm creation. The selected UX document explicitly excludes the Chatbot end-user experience and specifies only operational/admin confirmation flows. No linked Chatbot UX contract defines presentation, cancellation, retry, stale-candidate handling, accessibility, or the handoff between Chatbot and Projects for FR14 and FR15.

**Required action:** Provide or reference a Chatbot-owned UX/integration specification for candidate presentation and proposal confirmation, or explicitly constrain readiness to backend/operational surfaces and record the downstream Chatbot dependency.

#### UX-A2 — Resolution Trace planning language conflicts with the documented compute-on-demand model (High)

UX supports returning later to reconstruct behavior, initiating from a resolution case ID, reviewing evaluated inputs/candidate evidence, and exporting a trace. The architecture makes resolution compute-on-demand, persists only `ProjectResolutionConfirmed`, and explicitly defers persisted resolution-trace history. Project knowledge now makes the intended source explicit: `docs/resolution-scoring-heuristic.md` and `docs/parity-matrix.md` define the Workbench as a current recomputation over `ResolveProjectFromConversation` or `ResolveProjectFromAttachments`, with transient candidate score/rank and no persisted trace history. The selected UX and epic story still use “resolution case”/reconstruction language that implies a durable historical record not present in that model.

**Required action:** Ratify the documented current-only model in the architecture, UX, and active story. Remove resolution-case identifiers and historical/replay claims, or separately approve and architect durable trace history with retention, redaction, and authorization.

#### UX-A3 — Maintenance scope and permissions exceed the PRD's explicit operator-write scope (High)

UX exposes operator `restore`, `relink`, `unlink`, and maintenance workflows in addition to archive and read access. The PRD explicitly defines operator read access and says it does not provide write capabilities beyond archive and troubleshooting workflows explicitly exposed by Chatbot or generated/admin surfaces. It does not define operator roles or authorization policy for restore/relink/unlink, and `restore` is not a numbered FR.

**Required action:** Add an approved role/operation matrix and clarify whether these actions realize existing FR4/FR6/FR7/FR11 or are additional requirements. Any new capability must be added to PRD traceability rather than introduced only through UX/epics.

#### UX-A4 — Preview/confirmation safety is not fully supported by the main architecture (High)

UX requires dry-run/preview, confirmation bound to tenant/actor/action/targets/current state, clear expiry/staleness behavior, and auditable completion. The main architecture describes five-state UI lifecycle and command-async writes but does not define tamper-resistant, single-use, expiring server-issued confirmation evidence or restart-safe multi-step tasks. Corrective Epic 7 adds these concepts, but the selected architecture has not incorporated them as canonical decisions.

**Required action:** Update the architecture with the approved durable workflow/task/confirmation design, including cancellation, retry, stale-state invalidation, expiry, idempotency, and audit semantics.

#### UX-A5 — The architecture does not ratify the existing cross-surface parity vocabulary (Medium)

UX uses additional states/codes such as `Resolved`, `Excluded`, `FailedClosed`, `validationFailed`, `partialReferenceAvailability`, dry-run and maintenance-panel states, and command lifecycle values. The architecture's explicit single-source vocabulary enumerates only lifecycle, reference/inclusion states, three resolution results, and five resolution reason codes. Project knowledge includes a detailed `docs/parity-matrix.md` with maintenance states, CLI exit categories, MCP resources, safe failure categories, and field mappings, but that contract is not incorporated into the canonical architecture and remains coupled to the stale presentation boundary.

**Required action:** Ratify and update the existing parity matrix as a canonical architecture companion, fill any remaining validation/partial-failure gaps, and project it onto the supported `Contracts`/presentation boundary with stable wire codes, display/accessibility metadata, CLI failure mapping, and MCP schema mapping.

#### UX-A6 — UI responsiveness has no measurable performance design (Medium)

The PRD targets p95 under 500 ms for list/open/resolution/context retrieval. UX specifies dense tables, high-volume testing, multi-source trace/health views, filters, and responsive layouts but no measurable render/data-volume budgets. Architecture relies on precomputed projections yet flags Pattern A conversation reads and multi-ACL resolution as performance risks; it does not define paging, supported cardinality, timeout, partial-result, or client-render budgets for the UX surfaces.

**Required action:** Define supported cardinalities and measurable server/query/render budgets for inventory, reference health, trace, audit/export, and mobile/desktop views, including behavior when dependencies exceed their budgets.

#### UX-A7 — Architecture's FrontComposer/platform boundary is stale (High)

The architecture places `[Projection]`/`[Command]` presentation descriptors and `[ProjectionBadge]` UI concerns in the low-dependency `Contracts` kernel and includes Projects-owned `AppHost`, `Aspire`, and `ServiceDefaults` projects. Current platform context requires UI-clean Contracts with presentation rendering contracts in the approved `Contracts.UI`/adapter boundary, and domain modules must consume platform-owned hosting rather than reimplement topology/service defaults. Corrective Epic 6 acknowledges this migration, but the architecture's structure and handoff sections still prescribe the obsolete boundary.

**Required action:** Revise the architecture and project tree to the current supported EventStore DomainService and FrontComposer contract boundary before implementing or correcting the UX surface.

#### UX-A8 — The safe diagnostic export contract is detailed outside the architecture (Medium)

UX requires Web copy/download, CLI structured output, and MCP resources with an explicit payload-exclusion guarantee. The architecture states metadata-only behavior but does not ratify an export contract. Project knowledge already defines `projects.safe-diagnostic-export.v1`, field groups, exclusions, bounded audit rows, selectors, and MCP/CLI handoff in `docs/parity-matrix.md`; however, size/cardinality policy beyond the audit limit, authorization/audit treatment of export, supported partial-failure behavior, and the corrected presentation ownership are not canonical architectural decisions.

**Required action:** Incorporate the existing versioned export contract into the architecture, close its remaining bounds/authorization/audit/partial-failure decisions, and retain parity tests across all three surfaces.

### Warnings

- The UX document is complete but dated 2026-05-24; the architecture and corrective epics changed materially on 2026-07-14. UX should be revalidated against the corrective platform boundary and durable workflow model.
- The architecture's version posture references an older Fluent UI RC and pre-boundary FrontComposer assumptions. Current pinned platform versions and `Contracts`/`Contracts.UI` ownership must be treated as authoritative.
- Responsive and accessibility intent is strong, but readiness requires blocking authenticated live tests with deterministic tenant/project fixtures; the epics defer this evidence to corrective Epic 8.
- The UX's direct user roles remain descriptive rather than enforceable. Authentication, role claims, tenant membership, and per-action permissions need a canonical authorization matrix shared by Web, CLI, and MCP.

## Epic Quality Review

### Best-Practices Compliance Summary

| Epic | User Value | Independence / No Forward Dependency | Story Sizing | Acceptance Criteria | FR Traceability | Assessment |
| --- | --- | --- | --- | --- | --- | --- |
| Epic 1 — Project Workspace Foundation | Partial: delivers create/open/list/update/archive, but mixes that value with scaffold, contracts, auth, topology, CI, and observability | **Fail:** active Project creation defers mandatory Folder behavior to Epic 2 and durable enforcement to Epic 7; Story 1.6 references Workers delivered in Story 1.9 | **Fail:** several stories combine multiple independently testable systems | Mostly detailed BDD, with material contradictions and deferred completeness | Strong | Major restructuring required |
| Epic 2 — Context References | Strong workspace-reference value | **Fail:** depends on upstream Conversations/Folders capabilities; safe multi-step move/link behavior is repaired only in Epic 7 | Mixed; Stories 2.3 and 2.4 are too broad | Detailed BDD, but recovery/partial-failure behavior is incomplete | Strong | Not independently completable as written |
| Epic 3 — Project Context Assembly | Strong Chatbot outcome | Passes dependency direction: consumes Epics 1–2 only | Generally reasonable | Mostly testable BDD; performance and some denial paths are under-specified | Strong | Closest to compliant |
| Epic 4 — Project Resolution | Strong Chatbot outcome | **Fail in safe completion:** Stories 4.4–4.5 rely on non-durable multi-context confirmation until corrective Epic 7 | Generally reasonable | Happy paths are clear; restart, retry, stale-confirmation, and partial-failure criteria are missing | Strong | Functionally sliced but operationally incomplete |
| Epic 5 — Operational Console & Audit | Strong operator value | **Fail:** Resolution Trace relies on an undefined resolution case/history; safe maintenance and blocking evidence are repaired by Epics 7–8 | **Fail:** Stories 5.9–5.11 are epic-sized bundles | Detailed but over-broad; live evidence story permits unresolved skips | Strong for FR21–FR22; introduces additional unapproved actions | Major restructuring required |
| Epic 6 — Supported Platform Boundary and Secure Identity | **Fail:** framed as a technical migration milestone rather than a consumable user outcome | Sequential after Epics 1–5, but story dependencies and cutover entry/exit gates are not specified | **Fail:** several one-line stories bundle multiple subsystems | **Fail:** none of Stories 6.1–6.7 has user-story structure or acceptance criteria | Finding-based rather than FR-based | Not implementation-ready |
| Epic 7 — Durable Cross-Context and Agent-Safe Workflows | Partial: restart-safe user and agent operations are valuable, but much of the epic is platform plumbing | Order is plausible after Epic 6, but prerequisites are only named, not specified | Mixed; shared seam and migration stories are broad | **Fail:** none of Stories 7.1–7.7 has user-story structure or acceptance criteria | References existing FRs plus undefined FR23 | Not implementation-ready |
| Epic 8 — Production Conformance and Release Evidence | **Fail:** a validation/release technical milestone, not a standalone user capability | Follows Epics 6–7, but puts scale/performance bounds after implementation and leaves evidence dependencies implicit | **Fail:** each line represents a multi-lane program of work | **Fail:** none of Stories 8.1–8.9 has user-story structure or acceptance criteria | Finding/gate-based, not explicit FR/NFR mapping | Not implementation-ready |

**Database/entity timing:** No traditional database-table-upfront violation was found. Event streams and projections are generally introduced near their first use. This check passes, subject to the broader platform-boundary correction.

**Starter-template check:** The architecture specifies a Hybrid Hexalith scaffold, and Epic 1 Story 1.1 is correctly positioned as the initial greenfield setup/build/CI story. This check passes, although the scaffold itself is now stale relative to the supported platform boundary.

### 🔴 Critical Violations

#### EQ-C1 — Twenty-three corrective stories are placeholders, not executable stories

Stories 6.1–6.7, 7.1–7.7, and 8.1–8.9 contain only a title and one sentence. They have no actor/persona, no “I want / so that” value statement, no Given/When/Then acceptance criteria, no negative paths, no dependency declaration, no test tier/evidence, and no measurable completion rule. The addendum explicitly says future dedicated story files “must refine” them, confirming that the current epics document is not implementation-ready.

**Remediation:** Create each dedicated story specification before implementation scheduling. Every story must define entry prerequisites, user/operational outcome, bounded scope, BDD acceptance criteria, failure/retry/security cases, compatibility constraints, verification commands/evidence, and exact dependency order.

#### EQ-C2 — Completed epics depend on later corrective epics to become safe and complete

The document calls Epics 1–5 completed implementation history, yet:

- Story 1.4 permits an Active Project without a Folder, repaired by Epic 7 Story 7.3.
- Stories 2.3, 4.4, and 4.5 perform multi-context assignment/confirmation without durable restart-safe coordination, repaired by Epic 7 Stories 7.2, 7.4, and 7.5.
- Story 5.9's consequential actions lack bound durable tasks, repaired by Epic 7 Story 7.6.
- Authenticated isolation, persisted-boundary, accessibility, resilience, and release evidence remain deferred to Epic 8.

This is a direct failure of epic independence: earlier epics cannot be considered safely functional without later epics.

**Remediation:** Rebaseline the active plan around end-to-end corrective value slices. Do not represent Epics 1–5 as releasable/completed capabilities; mark them as implementation history with known failed gates. Each corrective slice should finish one observable operation across supported platform hosting, durable workflow, auth, persistence, and blocking evidence before moving on.

#### EQ-C3 — Mandatory-Folder behavior is contradictory and forward-dependent

PRD FR1 and FR11 establish that active v1 Projects require exactly one Folder, supplied or created. Story 1.4 says creation succeeds without one. Story 2.4 allows folder creation to be merely “queued/flagged” if Folders support is unavailable. Epic 7 Story 7.3 later changes creation into a durable task that must verify/create the Folder before Active.

**Remediation:** Make the Epic 7 invariant canonical now. Define explicit states and outcomes for pending Folder creation, authorization denial, timeout, lost response, retry, cancellation, reconciliation, and activation. Remove acceptance criteria that permit an Active folderless Project.

#### EQ-C4 — Epics 6 and 8 are technical milestones, not user-value epics

“Supported Platform Boundary and Secure Identity” groups contract migration, hosting, projections, topology, auth, consumer credentials, and cutover. “Production Conformance and Release Evidence” groups CI fixtures, health/logging, UI/MCP/CLI conformance, packaging, E2E, resilience, performance, deployment, and acceptance. Neither epic yields a coherent user capability on its own; they are architecture/release work programs.

**Remediation:** Re-slice by observable outcomes, for example: “Authorized Chatbot users can create and open a Project on supported DomainService,” “Operators can perform a bound, restart-safe conversation move,” and “Operators can inspect the same safe facts across authenticated Web/CLI/MCP.” Attach the required migration and evidence work to the value slice that first needs it.

#### EQ-C5 — Resolution Trace story input contradicts the documented current-only source

Story 5.6 begins “Given a resolution case,” while the architecture persists only confirmation and explicitly defers trace history. Project knowledge defines the actual source as current compute-on-demand resolution queries with no persisted case/history. The story's prerequisite and UX language therefore point to an undefined durable case even though the implemented diagnostic model is transient.

**Remediation:** Rewrite Story 5.6 around the documented current recomputation inputs and transient output contract, removing historical-case claims. If historical trace becomes approved scope, add a preceding value story with safe schema, ownership, persistence/retention, authorization, and rebuild behavior.

### 🟠 Major Issues

#### EQ-M1 — Multiple stories are epic-sized bundles

Examples include:

- Story 1.2: identifier, shared vocabulary, rejection taxonomy, payload taxonomy, identity derivation, documentation, and conformance tests.
- Story 1.3: OpenAPI spine, generated client, headers, idempotency, async semantics, freshness, Problem Details, and safe-denial.
- Story 1.6: tenant projection, durable storage, restart behavior, complete layered authorization, safe-denial, and cross-tenant suite.
- Story 1.9: AppHost, Dapr components, Workers, Redis, access control, resiliency, dead-letter/runbook, telemetry, and health.
- Story 2.3: link, move, unlink, two-context authorization, atomicity, and audit.
- Story 5.9: restore, relink, unlink, reevaluate, preview, dry-run, confirmation, command lifecycle, and audit.
- Story 5.10: all MCP and CLI read/write surfaces together.
- Story 5.11: cross-surface parity, four responsive ranges, WCAG, Playwright, tenant isolation, and leakage.
- Stories 6.2, 6.5, 6.7, 8.3–8.8: each names several independently releasable and independently failing systems.

**Remediation:** Split along independently verifiable behavior. Keep shared primitives only as small enablers immediately followed by—and accepted through—the first user-value slice.

#### EQ-M2 — Upstream prerequisites are not resolved as entry gates

Story 2.1 requires an additive Conversations list client or direct HTTP alternative; Story 2.2 requires a separate Conversations PR; Story 2.4 depends on Folders CreateFolder exposure; Story 2.6 is a Memories decision spike. The plan names these dependencies but does not consistently require verified versions/contracts before the consuming story starts.

**Remediation:** Add explicit entry criteria with repository/version/API proof for each upstream dependency. Keep upstream PRs as separately owned prerequisite stories and prevent consuming stories from entering `ready-for-dev` until the pinned contract and consumer test pass.

#### EQ-M3 — Multi-context workflows omit failure and recovery acceptance criteria

Stories 2.3, 4.4, 4.5, and 5.9 do not specify behavior for crash between steps, lost response, duplicate confirmation, stale source/target version, partial attachment success, cancellation, retry after unknown outcome, compensation, or audit receipt failure. Later corrective titles acknowledge these omissions but provide no criteria.

**Remediation:** For every durable workflow, add a state/transition table and BDD cases for success, denial, timeout, restart at each checkpoint, duplicate delivery, concurrency conflict, stale confirmation, cancellation, compensation, reconciliation, and idempotent completion.

#### EQ-M4 — NFRs are declared but not owned by measurable story acceptance criteria

The p95 under 500 ms target appears in requirements and epic labels but no original story owns a reproducible performance gate. Supported cardinalities are deferred to Story 8.8, after implementation. Availability, recovery, retention, scale, and blocking evidence are similarly not assigned measurable thresholds. Story 8.8 itself has no acceptance criteria.

**Remediation:** Define scale/cardinality and NFR measurement conditions before implementation, assign each NFR to specific stories, and provide deterministic pass/fail thresholds and environments.

#### EQ-M5 — Maintenance actions and FR23 break traceability discipline

Epic 5 introduces restore/relink/unlink/reevaluate as operator actions without explicit PRD approval or a role/operation matrix. Epic 7 cites undefined `FR-23`. Although all PRD FR1–FR22 are mapped, these added capabilities are not traceably governed.

**Remediation:** Update the PRD and FR map for approved new capabilities, or classify them as implementation mechanics of existing FRs with explicit rationale. Remove or define FR23.

#### EQ-M6 — Active backlog and implementation history are mixed in one “complete” document

The frontmatter says `status: complete`, Epics 1–5 contain detailed stories and are called implementation history, while Epics 6–8 are an active corrective addendum of placeholders. Readers cannot determine which acceptance criteria are current, superseded, failed, or merely historical.

**Remediation:** Mark superseded stories and criteria explicitly, publish an active corrective backlog with authoritative sequencing/status, and link historical epics rather than mixing them with the implementation-ready plan.

#### EQ-M7 — Story 5.12 and the release gates allow evidence gaps instead of requiring success

Story 5.12 allows retained skips when routes/fixtures/prerequisites remain unavailable. That is useful diagnostic reporting but not an acceptance gate for Epic 5. Corrective Epic 8 later requires authenticated blocking evidence, showing the earlier story was not complete.

**Remediation:** Separate “diagnose test-environment blockers” from “feature acceptance.” Release-critical cases must have deterministic fixtures and pass; authorized waivers require owner, rationale, expiry, and residual-risk disposition.

#### EQ-M8 — Canonical architecture changes are not reflected in the detailed stories

Stories 1.1, 1.2, 1.9, and 5.3 still implement Projects-owned AppHost/Aspire/ServiceDefaults and presentation concerns in the Contracts kernel. Epic 6 later migrates away from those boundaries. The plan therefore encodes known rework rather than one current implementation path.

**Remediation:** Update the architecture first, then rewrite active stories against supported DomainService, platform hosting, and FrontComposer `Contracts`/`Contracts.UI` ownership. Historical stories should not remain prescriptive.

### 🟡 Minor Concerns

- `FR-1` and `FR1` styles are mixed across the document; normalize IDs for machine traceability.
- “Standalone: Yes” is misleading for Epics 2–5 because each consumes earlier epics. Replace it with “No dependency on later epics” or an explicit prerequisite list.
- Story 5.1 contains an orphan acceptance paragraph beginning with `And` rather than a complete Given/When/Then scenario.
- Story 3.3's explanation query lacks explicit unauthorized-project, unknown-reference, and stale-evidence scenarios, even though adjacent context stories include a failure matrix.
- Story 5.12 refers to “13 focused cases” and “13 product specifications” without stable identifiers or an owned inventory; numeric counts will become stale.
- Technical enablers/spikes (Stories 2.2 and 2.6) are legitimate prerequisites but should have timebox, decision owner, exit artifact, and consuming-story unblock criteria.

### Dependency Findings

| Dependency | Direction | Quality Result |
| --- | --- | --- |
| Epic 2 consumes Epic 1 | Backward | Valid in principle |
| Epic 3 consumes Epics 1–2 | Backward | Valid |
| Epic 4 consumes Epics 1–2 | Backward | Valid in feature design |
| Epic 5 consumes Epics 1–4 | Backward | Valid in principle |
| Epic 1 mandatory Folder completed in Epic 2 / repaired in Epic 7 | Forward | **Critical violation** |
| Epics 2 and 4 durable assignment/confirmation repaired in Epic 7 | Forward | **Critical violation** |
| Epic 5 maintenance safety repaired in Epic 7 | Forward | **Critical violation** |
| Epic 5 production evidence deferred to Epic 8 | Forward | **Critical violation for completion claims** |
| Story 1.6 relies on Workers topology formalized in Story 1.9 | Within-epic forward reference | Major issue |
| Story 5.6 relies on undefined/deferred resolution trace history | Missing/forward dependency | **Critical violation** |
| Story 2.1 / 2.2 depend on Conversations upstream contracts | External prerequisite | Must be entry-gated |
| Story 2.4 depends on Folders CreateFolder capability | External prerequisite | Must be entry-gated |

### Quality Review Conclusion

The epic plan has strong functional traceability and several well-written BDD stories in Epics 1–5, especially Epic 3. It fails implementation-readiness standards because completed value slices rely on future corrective work, several stories are too broad, a key UX trace source is undefined, and all 23 corrective stories lack executable acceptance criteria. Epics 6–8 must be re-sliced and fully specified before Phase 4 corrective implementation can start.

## Summary and Recommendations

### Overall Readiness Status

# NOT READY

Hexalith.Projects is **not ready to begin Phase 4 corrective implementation or release handoff from the current planning set**.

The artifacts have important strengths: the PRD is final and functionally clear, all 22 PRD FRs have explicit epic coverage, the metadata-only and tenant-isolation principles are consistent, and Epics 1–5 contain several strong BDD stories. Those strengths do not overcome the active-plan defects. The canonical architecture is stale relative to the supported platform boundary; completed capabilities rely on later corrective epics; the primary Chatbot confirmation experience and Resolution Trace source are unresolved; and all 23 active corrective stories are placeholders without executable acceptance criteria.

The 100% FR allocation score is therefore a traceability result, not an implementation-readiness result.

### Critical Issues Requiring Immediate Action

1. **Replace the stale architecture baseline.** Incorporate the supported EventStore DomainService model, platform-owned hosting/topology, current FrontComposer `Contracts`/`Contracts.UI` boundary, secure identity/admission, and durable workflow/task/confirmation design into the canonical architecture and project tree.
2. **Turn all 23 corrective placeholders into implementation-ready stories.** Stories 6.1–8.9 need personas/outcomes, bounded scope, prerequisites, BDD criteria, negative/recovery cases, verification evidence, and exact dependency order.
3. **Rebaseline the backlog around end-to-end user/operational value.** Separate historical Epics 1–5 from the active plan and stop treating them as safely complete while Epics 7–8 still repair their invariants and evidence.
4. **Make the mandatory-Folder invariant unambiguous.** An Active Project must never be folderless. Specify the durable creation state machine and reconciliation behavior before implementing the correction.
5. **Specify durable cross-context operations.** Conversation assignment/move, proposal confirmation, archive/restore/relink/unlink, and audit receipts require restart-safe, idempotent, bound workflows with complete partial-failure and stale-confirmation behavior.
6. **Ratify the Resolution Trace source of truth.** Promote the documented current recomputation/no-history model into UX, architecture, and the active story, removing historical-case language unless durable trace history is separately approved.
7. **Close scope and traceability gaps.** Define or remove FR23; approve and map restore/relink/unlink/reevaluate; provide the Chatbot-owned FR14/FR15 UX handoff; and define the role/operation matrix across Web, CLI, and MCP.
8. **Define measurable readiness gates before implementation.** Establish supported cardinalities, p95 measurement conditions, recovery/availability/retention objectives, authenticated tenant-isolation fixtures, accessibility gates, and deterministic pass/fail evidence with no unexplained release-critical skips.

### Recommended Next Steps

1. Update `architecture.md` as the single current architecture; explicitly mark superseded hosting, contracts, identity, and workflow decisions.
2. Update the PRD or an approved requirements addendum to resolve FR23, maintenance scope, operator permissions, and the Chatbot confirmation handoff.
3. Ratify the existing compute-on-demand Resolution Trace and `projects.safe-diagnostic-export.v1` contracts in the canonical architecture and align UX/story wording.
4. Promote and update the existing cross-surface parity matrix to cover the supported contract/presentation boundary, domain states, command lifecycle, maintenance states, failures, CLI exit semantics, MCP schemas, labels, accessibility names, and severity.
5. Replace the corrective addendum with a clean active backlog sliced by observable value, with historical Epics 1–5 linked as evidence rather than mixed into the implementation plan.
6. Create and validate each corrective story file in dependency order; upstream Conversations/Folders/platform capabilities must be verified entry gates, not mid-story choices.
7. Add state-transition and failure matrices for Project creation, conversation assignment/move, proposal confirmation, and maintenance tasks, including restart, retry, duplicates, concurrency, cancellation, compensation, and reconciliation.
8. Add NFR ownership and blocking verification to the stories, then rerun implementation readiness before any corrective story is accepted into development.

### Assessment Accounting

- Requirements and traceability issues: 2
- UX and architecture alignment issues: 8
- Epic/story quality issues: 19 (5 critical, 8 major, 6 minor)
- **Total documented issues requiring attention: 29 across 3 categories**
- Additional UX warnings: 4
- PRD functional coverage: 22 of 22 (100%)
- Corrective placeholder stories requiring full specification: 23

### Final Note

This assessment is intentionally direct: proceeding from the current artifacts would convert known planning gaps into implementation churn and unsafe completion claims. Address the critical issues, fully specify the corrective stories, and rerun this readiness assessment before Phase 4 work is authorized.

**Assessment date:** 2026-07-14  
**Assessor:** Codex — Product Management and Requirements Traceability Review
