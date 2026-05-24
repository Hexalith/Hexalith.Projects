---
title: "PRD: Hexalith.Projects"
status: final
created: 2026-05-24
updated: 2026-05-24
---

# PRD: Hexalith.Projects

## 0. Document Purpose

This PRD defines the v1 product requirements for `Hexalith.Projects`, a Hexalith module that gives `Hexalith.Chatbot` a durable AI project workspace. It is written for product, architecture, UX, and implementation workflows. Requirements are grouped by feature, with stable functional requirement IDs and testable consequences.

## 1. Vision

`Hexalith.Projects` enables `Hexalith.Chatbot` to hold rich, continuous conversations with users by grounding each conversation in the correct project context. A project connects the conversations, files, folders, memories, setup, and operational metadata needed for an AI work session to continue safely across time.

The module is not a generic project management system. Its purpose is to provide a tenant-aware AI workspace boundary: Chatbot can determine which project a conversation belongs to, retrieve the context needed for the next interaction, and propose project creation when no suitable project exists.

v1 is an internal platform and near-term implementation spec for Hexalith.Chatbot, not a public standalone product launch.

## 2. Target User

### 2.1 Primary Persona

The primary user is a Hexalith.Chatbot user who works across multiple conversations, files, folders, and memories and expects the assistant to remember the correct project context without re-explaining it each session.

### 2.2 Jobs To Be Done

- Resume AI work without manually rebuilding the project context.
- Keep project-related conversations, files, folders, and memories connected.
- Start a new conversation and have Chatbot infer or request the correct project context.
- Avoid leaking context across unrelated projects, tenants, or users.
- Let downstream agents and tools rely on a durable project boundary instead of transient chat UI state.

### 2.3 Non-Users (v1)

- Users looking for a generic task board, scheduling system, or project management suite.
- Users expecting Projects to store full conversation transcripts, file contents, or memory payloads directly.
- External customers consuming Projects as an independent end-user product.

### 2.4 Key User Journeys

- **UJ-1. The Hexalith.Chatbot user resumes an existing project conversation.** The user opens Hexalith.Chatbot and selects an existing project. Chatbot requests the project context from Projects, receives linked conversations, folders, files, memories, and setup metadata, then starts the conversation with the correct project boundary. The user sees continuity without manually attaching prior context.

- **UJ-2. The Hexalith.Chatbot user starts with files before choosing a project.** The user opens Chatbot, attaches files or selects a folder, and begins asking questions. Projects evaluates the attached folder/file references and conversation metadata, identifies likely matching projects, and either attaches the conversation to the strongest match or asks the user to confirm. If no suitable project exists, Chatbot proposes creating one.

- **UJ-3. The Hexalith.Chatbot user starts a conversation with no explicit project.** The user starts chatting without choosing a project. Chatbot asks Projects to resolve project membership from the conversation, attachments, memories, and available project metadata. If confidence is high, Chatbot uses the likely project after confirmation; if confidence is ambiguous, it presents candidates; if no match exists, it proposes a new project.

- **UJ-4. The Hexalith.Chatbot user protects unrelated work from context leakage.** The user works on multiple projects with overlapping terminology or files. Projects only returns context from authorized, linked, or confirmed project references. Chatbot does not include unrelated conversations, files, folders, or memories in the active prompt context.

## 3. Glossary

- **Project** — A durable AI workspace boundary managed by Hexalith.Projects. A Project contains project metadata, setup, lifecycle state, and references to related bounded-context resources.
- **Project Context** — The complete set of project-scoped information Chatbot may use to conduct a conversation, including Project metadata, setup, linked Conversation references, Project Folder reference, optional File References, Memory references, and context-selection policy.
- **Conversation** — A conversation owned by `Hexalith.Conversations`. Projects references Conversation identifiers and metadata; it does not store full transcript content. In v1, a Conversation belongs to exactly one Project.
- **Project Folder** — The single canonical folder owned by `Hexalith.Folders` and referenced by a Project in v1. Projects references the Project Folder identifier and metadata; it does not store file contents or bypass folder authorization.
- **File Reference** — A file resource owned by `Hexalith.Folders` and optionally referenced by Project Context. Projects references File identifiers and metadata; it does not store file contents.
- **Memory** — A durable memory resource owned by `Hexalith.Memories`. Projects references Memory identifiers and metadata; it does not own memory payload storage.
- **Project Setup** — Durable settings and instructions needed to initialize or resume a Project in Chatbot, including goals, user-facing instructions, context preferences, allowed source references, and conversation-start defaults.
- **Project Resolution** — The process of identifying which Project should apply to a Conversation when no explicit Project has been selected.
- **Candidate Project** — A Project returned by Project Resolution as a possible match for the current Conversation, attachments, or metadata.
- **Resolution Result** — The outcome of Project Resolution. v1 supports `NoMatch`, `SingleCandidate`, and `MultipleCandidates`.
- **Resolution Reason Code** — Metadata explaining why a Candidate Project was returned. v1 supports `ConversationLinked`, `ProjectFolderMatched`, `FileReferenceMatched`, `MemoryMatched`, and `MetadataMatched`.
- **Project Lifecycle State** — The availability state of a Project. v1 supports only `Active` and `Archived`.
- **Tenant** — The Hexalith isolation boundary that scopes access to Projects, Conversations, Project Folders, File References, and Memories.

## 4. Features

### 4.1 Project Workspace Management

**Description:** Projects provides the durable workspace record that Chatbot uses to create, open, update, archive, and list AI projects. A Project captures metadata and setup required for continuity, while related bounded contexts remain the systems of record for their own data.

#### FR-1: Create Project

Chatbot can create a Project with tenant context and Project name as the required inputs. Project name is the only required user-supplied field. Description, initial setup, initial references to Conversations, File References, and Memories are optional. If no Project Folder is supplied, Chatbot can request creation of a Project Folder with the same name. Realizes UJ-2 and UJ-3.

**Consequences (testable):**
- Creating a Project records durable Project metadata and sets Project Lifecycle State to `Active`.
- Creating a Project requires only a Project name from the user.
- Creating a Project can create or attach a Project Folder with the same name as the Project when no folder is supplied.
- Creating a Project does not duplicate conversation transcripts, file contents, or memory payloads.
- Creating a Project fails closed when tenant context is missing or unauthorized.

#### FR-2: Open Project

Chatbot can open a Project and receive the Project metadata, lifecycle state, setup, and authorized references needed to initialize a conversation. Realizes UJ-1.

**Consequences (testable):**
- Opening a Project returns only references visible to the requesting tenant/user context.
- Archived or unavailable Projects are clearly identified and cannot silently become active conversation context.

#### FR-3: Update Project Setup

Chatbot can update Project Setup needed for conversation continuity, including project instructions, context preferences, and configuration metadata. Realizes UJ-1.

**Consequences (testable):**
- Setup updates are durable and available to later conversations.
- Setup can include project goals, user-facing instructions, preferred context sources, source inclusion/exclusion preferences, conversation-start defaults, and metadata needed to resume the Project.
- v1 Project Setup describes conversation behavior and context policy, not model-provider internals.
- Setup updates preserve additive, serialization-tolerant contract behavior.
- Setup updates do not allow raw secrets, unrestricted file paths, or payload data that belongs to another bounded context.

#### FR-4: Archive Project

Chatbot or authorized operators can archive a Project so it remains discoverable for history but is no longer selected as active context by default.

**Consequences (testable):**
- v1 Project Lifecycle State is limited to `Active` and `Archived`.
- Archived Projects are excluded from automatic Project Resolution unless explicitly requested.
- Existing references remain auditable after archival.

#### FR-5: List Projects

Chatbot can list Active and Archived Projects visible to the requesting tenant/user context.

**Consequences (testable):**
- List results are tenant-scoped and authorization-filtered.
- List results include enough metadata for Chatbot to present Project choices without loading full Project Context.
- List results can filter by Project Lifecycle State.

### 4.2 Context References

**Description:** Projects coordinates references to Conversations, the Project Folder, optional File References, and Memories. It owns the association between a Project and these resources, but not the underlying resources or their payloads.

#### FR-6: Link Conversation

Chatbot can link an existing Conversation to a Project. Realizes UJ-1 and UJ-3.

**Consequences (testable):**
- The link records stable Conversation identity and relevant metadata.
- A Conversation can be linked to only one Project in v1.
- Linking a Conversation that already belongs to another Project requires an explicit move operation rather than creating a second membership.
- The link does not copy transcript content into Projects.
- The link fails if tenant authorization for the Conversation cannot be established.

#### FR-7: Move Conversation Between Projects

Chatbot can move a Conversation from one Project to another when the user explicitly confirms the move.

**Consequences (testable):**
- Moving a Conversation removes the prior Project membership before creating the new one.
- Moving a Conversation is auditable as metadata.
- Moving a Conversation fails closed when authorization to either Project or the Conversation cannot be established.

#### FR-8: Set Project Folder

Chatbot can set the single authorized Project Folder for a Project. Realizes UJ-2.

**Consequences (testable):**
- A Project has exactly one Project Folder in v1.
- Setting a Project Folder records stable Folder identity and relevant metadata.
- Setting a new Project Folder replaces the previous Project Folder only through an explicit update.
- The Project Folder reference does not store file contents or unrestricted filesystem paths in Projects.
- Folder authorization remains delegated to `Hexalith.Folders`.

#### FR-9: Link File Reference

Chatbot can link authorized File References to a Project when a file should be part of Project Context without changing the Project Folder.

**Consequences (testable):**
- File References are optional and do not replace the Project Folder.
- A File Reference records stable File identity and relevant metadata.
- File authorization remains delegated to `Hexalith.Folders`.

#### FR-10: Link Memory

Chatbot can link authorized Memory references to a Project. Realizes UJ-1 and UJ-3.

**Consequences (testable):**
- The link records stable Memory identity and relevant metadata.
- The link does not store Memory payloads in Projects.
- Memory authorization remains delegated to `Hexalith.Memories`.

#### FR-11: Unlink Context Reference

Chatbot can remove a Conversation, File Reference, or Memory reference from a Project without deleting the underlying resource. The Project Folder can be replaced but not removed unless the Project is archived. v1 Projects require a Project Folder.

**Consequences (testable):**
- Unlinking removes the association from Project Context.
- Unlinking does not delete the underlying Conversation, File Reference, or Memory.
- Unlinking is auditable as metadata.

### 4.3 Project Resolution

**Description:** When Chatbot has a Conversation without an explicit Project, Projects helps determine the likely Project from conversation metadata, attached Project Folder or File References, linked Memories, and existing Project metadata. Best practice is to prefer explicit user confirmation over silent attachment when confidence is ambiguous.

#### FR-12: Resolve Project From Conversation

Chatbot can ask Projects to resolve Candidate Projects for a Conversation that has no explicit Project. Realizes UJ-3.

**Consequences (testable):**
- Resolution returns a Resolution Result of `NoMatch`, `SingleCandidate`, or `MultipleCandidates`.
- Candidate Projects include one or more Resolution Reason Codes.
- Resolution does not access unauthorized Conversations, Project Folders, File References, Memories, or Projects.
- Resolution excludes archived Projects unless explicitly requested.

#### FR-13: Resolve Project From Attachments

Chatbot can ask Projects to resolve Candidate Projects from attached Project Folder or File References. Realizes UJ-2.

**Consequences (testable):**
- Matching considers existing Project Folder references and File References.
- Matching identifies `ProjectFolderMatched` or `FileReferenceMatched` Resolution Reason Codes when applicable.
- Matching fails closed when Project Folder or File Reference authorization is missing or stale.
- Matching never treats raw file contents as Project-owned data.

#### FR-14: Confirm Ambiguous Project

When Project Resolution returns multiple plausible Candidate Projects, Chatbot can present the candidates and record the user's confirmed choice.

**Consequences (testable):**
- Projects does not silently attach a Conversation when the Resolution Result is `MultipleCandidates`.
- User confirmation creates or updates the Project-to-Conversation association.
- Rejected candidates are not linked.

#### FR-15: Propose New Project

When Project Resolution cannot find a suitable Project, Chatbot can propose creating a new Project using the current Conversation, attachments, and setup metadata.

**Consequences (testable):**
- The proposal includes a suggested Project name and initial setup metadata when available.
- No Project is created from inference until authorized user action confirms creation.
- The created Project links the initiating Conversation and authorized attachments.

### 4.4 Project Context Assembly

**Description:** Projects provides the Project Context that Chatbot needs for rich conversation. Context assembly must be scoped, explainable, and conservative so Chatbot gets useful continuity without accidental cross-project contamination.

#### FR-16: Get Project Context

Chatbot can request the Project Context for a Project and receive the setup plus authorized references to Conversations, the Project Folder, File References, and Memories needed for conversation initialization. Realizes UJ-1 and UJ-4.

**Consequences (testable):**
- Project Context is tenant-scoped and authorization-filtered.
- Project Context contains references and metadata, not full payloads owned by other bounded contexts.
- Project Context indicates which referenced resources were excluded because of authorization, lifecycle, or availability.

#### FR-17: Explain Context Selection

Chatbot can display or log metadata explaining why a Conversation, Project Folder, File Reference, or Memory reference was included or excluded from Project Context. Realizes UJ-4.

**Consequences (testable):**
- Explanation metadata does not include secrets, file contents, transcript payloads, prompts, or memory payloads.
- Explanation supports troubleshooting incorrect context selection.

#### FR-18: Refresh Project Context

Chatbot can request a refreshed Project Context after links, setup, or resource availability changes.

**Consequences (testable):**
- Refresh reflects current Project links and lifecycle state.
- Refresh preserves tenant authorization checks.
- Stale or unavailable references are surfaced rather than silently ignored.

### 4.5 Project Setup Quality

**Description:** Project Setup must be useful enough for Chatbot to resume work without making Projects responsible for prompt construction, model orchestration, or payload storage.

#### FR-19: Validate Project Setup

Projects validates Project Setup before accepting create or update operations.

**Consequences (testable):**
- Setup validation rejects raw secrets, unrestricted local paths, unsupported reference types, and payload data that belongs to Conversations, Folders, or Memories.
- Setup validation requires only a Project name as user-supplied creation input.
- Setup validation defaults Project Lifecycle State to `Active`.
- Setup validation permits the Project Folder to be supplied explicitly or created with the same name as the Project.
- Setup validation allows durable conversation guidance such as project goals, preferred tone, domain instructions, and context-source preferences.
- Validation failures return structured errors that identify the rejected setup field without echoing sensitive values.

#### FR-20: Retrieve Conversation-Start Setup

Chatbot can retrieve the subset of Project Setup needed to start or resume a Conversation.

**Consequences (testable):**
- Conversation-start setup includes project goals, instructions, context preferences, and default linked-source policy.
- Conversation-start setup excludes internal audit metadata and unavailable or unauthorized references.
- Conversation-start setup is stable enough for Chatbot to use without re-querying every bounded context before the first response.

### 4.6 Audit and Operations

**Description:** Projects must provide enough audit and operational surface for an internal platform module without becoming a standalone admin product.

#### FR-21: Record Project Audit Events

Projects records metadata-only audit events for Project lifecycle and context-reference changes.

**Consequences (testable):**
- Audit events are recorded for Project creation, setup updates, archival, Conversation linking, Conversation moves, Project Folder changes, File Reference linking/unlinking, Memory linking/unlinking, Project Resolution confirmation, and new Project creation from a proposal.
- Audit events include tenant, Project identity, operation type, timestamp, actor identity where available, and affected reference identifiers.
- Audit events do not include transcript payloads, file contents, raw prompts, secrets, or Memory payloads.

#### FR-22: Support Operator Read Access

Authorized operators can inspect Project metadata, lifecycle state, references, resolution outcomes, and audit metadata for troubleshooting.

**Consequences (testable):**
- Operator read access is authorization-gated and tenant-scoped.
- Operator read access exposes metadata only.
- Operator read access does not provide write capabilities beyond archive and troubleshooting workflows explicitly exposed by Chatbot or generated/admin surfaces.

## 5. Non-Goals (Explicit)

- Projects will not replace `Hexalith.Conversations` as the conversation system of record.
- Projects will not replace `Hexalith.Folders` as the Project Folder/File Reference system of record or authorization boundary.
- Projects will not replace `Hexalith.Memories` as the memory system of record.
- Projects will not store full conversation transcripts, file contents, raw prompts, secrets, memory payloads, or unrestricted filesystem paths.
- Projects will not provide generic project management features such as tasks, milestones, kanban boards, schedules, or resource planning in v1.
- Projects will not bypass Dapr, Hexalith.EventStore, or tenant isolation patterns.

## 6. MVP Scope

### 6.1 In Scope

- Project identity, metadata, setup, lifecycle, and tenant scoping.
- References from Projects to Conversations, one Project Folder, optional File References, and Memories.
- Project Resolution from Conversation metadata and attached Project Folder or File References.
- Candidate Project confirmation and new Project proposal flows.
- Project listing and Project Context retrieval for Chatbot.
- Project Setup validation and conversation-start setup retrieval.
- Metadata-only audit events and operator read access.
- Metadata-only diagnostics for context inclusion/exclusion.
- Archive behavior for Projects.

### 6.2 Out of Scope for MVP

- Full-text indexing or semantic retrieval over file contents.
- Memory payload storage or synthesis.
- Transcript storage or summarization inside Projects.
- Standalone end-user UI outside Chatbot or generated/admin surfaces.
- Generic project management workflows.
- Cross-tenant project sharing.

## 7. Cross-Cutting NFRs

- **Security and privacy:** Projects must enforce tenant isolation across reads, writes, links, resolution, and context assembly. Logs and diagnostics must remain metadata-only.
- **Reliability:** Project Context retrieval should fail closed when authorization, lifecycle, or referenced-resource availability cannot be verified.
- **Observability:** Project Resolution and Project Context assembly must emit structured metadata sufficient to troubleshoot incorrect matches without exposing payloads.
- **Performance:** Project listing, Project opening, Project Resolution, and Project Context retrieval should target p95 under 500 ms when dependent bounded-context metadata is available. This is an internal service target, not a formal external SLA.
- **Compatibility:** Public contracts should be additive and serialization-tolerant unless a breaking change is explicitly approved.

## 8. Success Metrics

**Primary**

- **SM-1:** Project Context availability — Chatbot can retrieve usable Project Context for active Projects in normal operation. Validates FR-2, FR-16, FR-18.
- **SM-2:** Resolution usefulness — conversations without explicit Projects produce either a correct Candidate Project or a useful new Project proposal. Validates FR-12, FR-13, FR-14, FR-15.
- **SM-3:** Context isolation — Project Context never includes unauthorized or unrelated tenant/project references. Validates FR-6, FR-8, FR-9, FR-10, FR-16, FR-17.
- **SM-4:** Interactive metadata latency — Project list, open, resolution, and context retrieval meet the p95 target in normal internal operation. Validates FR-5, FR-12, FR-16, FR-18.

**Counter-metrics**

- **SM-C1:** Do not optimize for automatic attachment rate at the expense of correctness; ambiguous Project Resolution should ask for confirmation.
- **SM-C2:** Do not optimize context volume at the expense of relevance, security, or prompt quality.

## 9. Open Questions

- None.

## 10. Accepted Planning Decisions

- §1 Vision — v1 is an internal platform and near-term implementation spec for Hexalith.Chatbot, not a public standalone product launch.
- FR-3 — v1 Project Setup describes conversation behavior and context policy, not model-provider internals.
- FR-11 — v1 Projects require a Project Folder, but it can be created automatically from the Project name when the user does not supply one.
- FR-15 — v1 requires confirmation before creating a Project from inference.
- §7 Cross-Cutting NFRs — v1 latency target is an internal service target, not a formal external SLA.
