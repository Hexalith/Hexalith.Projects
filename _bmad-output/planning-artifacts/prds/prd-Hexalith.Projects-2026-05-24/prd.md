---
title: "PRD: Hexalith.Projects"
status: final
created: 2026-05-24
updated: 2026-09-08
---

# PRD: Hexalith.Projects

## 0. Document Purpose

This PRD defines the v1 product requirements for `Hexalith.Projects`, the Hexalith module that gives `Hexalith.Chatbot` a durable, tenant-aware AI project workspace. It is the product baseline for UX, architecture, epics, implementation, and release acceptance. Requirements are grouped by feature with stable functional requirement IDs; cross-cutting quality obligations have stable NFR IDs. Technical mechanisms, migration detail, and proposal-specific implementation evidence live in [addendum.md](./addendum.md). Tagged assumptions and open specification items live in §9. `status: final` means the product baseline is direction-complete with stable IDs; the 2026-09-08 rules are binding on downstream work pending the re-baselining that §2.5 requires.

## 1. Vision

`Hexalith.Projects` enables `Hexalith.Chatbot` to sustain rich conversations by grounding each interaction in the correct Project Context. A Project connects the Conversations, Project Folder, File References, Memories, setup, and operational metadata needed for AI work to continue safely across time.

The module is not a generic project-management system. It provides a durable workspace boundary: Chatbot can determine which Project applies, retrieve authorized context, ask for confirmation when intent is ambiguous, and propose a Project when none fits. Consequential changes are durable, recoverable, and confirmed by server truth rather than inferred from acknowledgement or notification.

v1 is an internal platform and near-term implementation baseline for Hexalith.Chatbot, not a public standalone product launch.

## 2. Product Contract and Release Scope

### 2.1 Accepted Planning Decisions

- v1 is an internal Hexalith.Chatbot platform module, not a standalone or generic project-management product.
- Project name is the only required user-authored creation field; canonical requests also carry system-supplied Metadata Classification.
- Project lifecycle remains exactly `Active`/`Archived`; Durable Task statuses are separate.
- A Conversation is associated with at most one Project at any time and never more than one in v1, trading simultaneous cross-Project context for an unambiguous workspace boundary; every membership change after the first confirmed membership is an auditable move (FR-7), and unlink never becomes a silent path to a second membership. Revisit only if an explicit multi-Project Conversation product need and safe conflict model are approved.
- No Project is caller-visible or Active before exactly one authorized Project Folder is bound and the read model confirms completion. A Folder is the Project Folder of at most one Active Project, so the Folder is an exclusive authorization anchor rather than only a cardinality rule. The single Folder trades multi-root workspaces for a stable authorization anchor; revisit only if one Folder cannot represent an approved workspace use case.
- v1 actor authorization for a Project is Folder-derived and split by `Hexalith.Folders` permission level. Folder read authorization on the Project Folder grants read authority only: open, list, resolution, context, explanation, refresh, Conversation-start setup, and own-task inspection (FR-2, FR-5, FR-12, FR-13, FR-16, FR-17, FR-18, FR-20). Folder manage authorization on the Project Folder grants Project User mutation authority: setup, archive, restore, link, move, unlink, and initial Folder set (FR-1 supplied Folder, FR-3, FR-4, FR-6, FR-7, FR-8, FR-9, FR-10, FR-11, FR-23). A Project User Folder replacement (FR-8) requires manage on both Folders. Project creation (FR-1, FR-15) requires no prior Project authority; a supplied Folder requires manage on it, and a Folder created for a Project User names that actor as its sole manager with no Tenant-default read. Linking, moving, or confirming a Conversation, File, or Memory additionally requires the actor's current read authorization on that resource in its owning system. FR-14 confirmation requires the authority of the association it confirms. Projects issues no Project-level grants of its own and its workload identity holds no Folder authorization; grants and revocations happen in Folders, are recomputed at every authorization decision, and are auditable only in Folders. Project User authority is evaluated only on Chatbot sessions; Tenant Operator and Tenant Project Administrator authority is Tenant-role based, applies on Web, CLI, and MCP, and is never combined with Project User authority in one session. Accepted trade-off: a read-only Project User may open, resume already-linked Conversations, and inspect, but cannot attach new Conversations, Files, or Memories or confirm a candidate, so that Folder manage remains the single mutation gate. A Tenant Project Administrator re-anchoring (FR-8) is rejected when the Administrator is in the target Folder's reader set, and may reduce a permitted set only when the current Folder is invalid or missing; an Administrator-initiated Folder creation names as sole manager the Project User the Administrator designates in the Preview, never the Administrator. See §9 A-3.
- Project creation from inference and other consequential actions follow the approved Preview/Confirmation boundary. An association is actor-selected only when the request carries Selection Evidence (§4, FR-25) minted by the actor's own pick inside the Projects-owned selection component on Chatbot or Web; a caller-asserted selection flag, an open response, or a list response is not evidence, and on CLI and MCP every association is an Inferred Association whose confirmation stays disabled until A-7 holds (CLI) and the consequential-MCP gate also passes (MCP). Every other association is an Inferred Association and requires confirmation. A `SingleCandidate` whose reasons include `ConversationLinked` reflects an existing confirmed membership and is not an association to bind; every other `SingleCandidate` is an Inferred Association.
- Role and surface choice never expand authority; service callers act only with delegated actor authority. Every authorization decision binds to the original actor and action; workload identity is transport, never a role. Confirmation Artifacts and Selection Evidence are bound to the original actor's authenticated interactive session and surface and are consumed or minted only by the actor's own gesture inside the Projects-owned interactive confirmation and selection components (FR-25, §9 A-6); no confirm or select API is callable by an adapter on the actor's behalf. A dual-principal adapter (Chatbot, Web, CLI) that carries that session presents the Preview and hosts the component but cannot originate or self-confirm, and a Service/Workflow Caller, meaning any caller without the actor's interactive session, never receives or consumes them. Consequential MCP confirmation and MCP Selection Evidence stay disabled until §9 A-7 holds and the addendum's consequential-MCP gate (Story 8.11 terminal acceptance) passes; autonomous MCP confirmation stays out of scope (§2.4).
- Every reference, link, move, Folder binding, restore, resolution input, context component, audit record, and export row belongs to the same Tenant as the Project, and so does the acting session. Authorization on both ends is necessary and not sufficient; cross-Tenant references fail closed.
- FR-22 remains operator read access plus bounded task control (reconciliation of an already-admitted task); FR-24 owns Safe Diagnostic Export; mutations remain in their action-specific FRs.
- Resolution is current recomputation, not persisted inference history; only confirmed outcomes enter audit. This trades retrospective inference replay for current-authorization safety and lower diagnostic retention; revisit only through a separately approved history/retention requirement.
- Projects stores metadata and references while Conversations, Folders, and Memories remain their systems of record.
- Audit, diagnostics, errors, telemetry, and exports are metadata-only and Tenant-scoped. "Metadata-only" means Safe Metadata as defined in §4; Descriptive Metadata (Project name, Project Setup bodies, Conversation titles, File names and paths, Memory titles, Preview bodies) is served only to authorized Project Users and to operators holding separate descriptive-metadata inspection authorization, and never enters audit, traces, telemetry, measurement, or Safe Diagnostic Export.
- The approved performance, availability, durability, retention, back-pressure, security, accessibility, compatibility, and release-evidence envelopes in NFR-1 through NFR-11 are binding MVP acceptance criteria.
- Historical unversioned name-only creation compatibility trades a smaller v1 contract surface for non-breaking adoption; retirement remains gated by an approved major version, usage evidence, migration notice, compatibility tests, and rollback evidence.
- Core user value may be sequenced before release-blocking safety/operations, but no approved v1 FR or NFR is deferrable from production release; §2.3 records the release-cut rule.
- Resolution success includes usefulness to the Project User, not merely production of candidates or proposals.

### 2.2 In Scope

- Project identity, setup, `Active`/`Archived` lifecycle, tenant scoping, and exactly-one-Folder invariant.
- Durable, idempotent, recoverable creation, archive, restore, and cross-context association workflows.
- Server Preview and Confirmation Artifacts for destructive, inferred, or consequential user-intent changes.
- Project Resolution, accessible candidate/proposal confirmation, transient current Resolution Traces, and read-only refresh.
- Project Context retrieval for Chatbot and Conversation-start setup.
- Role-scoped metadata-only operator access, audit, reconciliation status, and bounded Safe Diagnostic Export.
- v1 compatibility for historical unversioned name-only creation requests.
- Authenticated release evidence for persistence, isolation, recovery, accessibility, performance, deployment, smoke, and rollback.

### 2.3 Release Classification and Cut Rule

This classification controls sequencing and release decisions without changing requirement priority or IDs.

| Class | Requirements | Sequencing and release decision |
| --- | --- | --- |
| **Core user value** | FR-1 through FR-20, FR-23, and FR-25 | These requirements deliver the durable workspace, lifecycle, references, resolution, context, and setup outcomes. They may be implemented and verified in coherent value slices after implementation readiness returns `READY`, but a core-only build is internal evidence, not an authorized production release. |
| **Release-blocking safety and operations** | FR-21, FR-22, FR-24, and NFR-1 through NFR-11 | These requirements make core value supportable and safe in production: metadata-only audit and operator truth, bounded diagnostics instead of ad-hoc payload access, tenant/action isolation, recovery, performance, accessibility, compatibility, and release evidence. Production release remains blocked until this class passes alongside the core class. |
| **Deferrable or out-of-scope release cuts** | No approved v1 FR or NFR. Only the capabilities in §2.4 are deferrable. | Removing an approved FR/NFR is a product-scope change requiring explicit approval, replacement safety/operability treatment where applicable, and updated acceptance evidence. Temporary disabling of a gated surface does not count as delivering that requirement. |

The accepted v1 trade-off is deliberate: sequencing may expose a core-first implementation path, but there is no smaller safe production release. In particular, FR-22 and FR-24 remain release-blocking because authorized operator truth and bounded Safe Diagnostic Export are the supported alternatives to payload-level or unaudited troubleshooting access. FR-22 is per-Project and per-task interactive inspection of Safe Metadata plus bounded reconciliation of an admitted task; FR-24 is a bounded, shareable, audited snapshot of the same Safe Metadata, not a wider disclosure. NFR-11 prevents incomplete evidence from being represented as release acceptance.

### 2.4 Product Boundaries and v1 Exclusions

v1 does not serve external customers consuming Projects as an independent product or users seeking the excluded capabilities below.

**Enduring product boundaries**

- Projects does not replace Conversations, Folders, or Memories as their systems of record or authorization boundaries.
- Projects does not store full transcripts, file contents, raw prompts, secrets, Memory payloads, unrestricted paths, or raw upstream problems.
- Projects does not provide generic task management; Durable Tasks are internal truth for Project operations, not user-managed work items.
- Projects does not persist candidate-score history or Resolution Traces, nor does it later reconstruct transient inference detail.
- Projects does not expose a standalone end-user UI outside Chatbot and generated/operational surfaces.
- Projects does not bypass Dapr, Hexalith.EventStore, tenant isolation, or action-level authorization.
- Legacy Active folderless Projects created before v1 may exist in Tenant data. They are quarantined: excluded from list, resolution, context, and Conversation-start until a Tenant Project Administrator binds exactly one authorized Project Folder through FR-8 Preview and confirmation; the quarantine inventory is readable under FR-22 (addendum §5, §9 A-4). The "never folderless" invariant applies to every Project admitted through v1 workflows and to every Active Project a caller can see.

**Out of scope for v1**

- Content indexing or semantic retrieval over file contents.
- Memory payload storage or synthesis.
- Transcript storage or summarization inside Projects.
- Historical inference/candidate-score storage or persisted diagnostic exports.
- Generic project-management workflows.
- Autonomous MCP confirmation or blanket service-identity mutations; consequential MCP confirmation by a human actor is gated (addendum readiness gates, §9 A-7), not excluded.
- Cross-Tenant Project sharing, customer-managed encryption keys, and cross-region disaster-recovery guarantees.

### 2.5 Planning Status

Product direction is decided: no open product-direction question blocks UX, architecture, or epic decomposition, and FR/NFR IDs are stable. Specification-level holes surfaced by validation are tagged in §9 with owners and revisit conditions; downstream artifacts must source those rules from §9 rather than invent them.

Planning and implementation status is not a green light. The controlling implementation disposition is addendum E-17 (`NOT READY`, 2026-08-02): production-authority implementation stays frozen until the Story 6.1 prerequisite chain, signed same-baseline architecture conformance, P4 clean-checkout acceptance, story-spec readiness, and an independent `READY` rerun pass; release additionally requires Story 8.11 terminal evidence. Live `release-smoke` evidence last recorded 19 passed and 56 failed. Later proposals through 2026-09-02 (addendum E-19 onward; E-30 has no recorded approver) corrected the UX, epic, and readiness artifacts toward this PRD without changing FR/NFR IDs. The 2026-09-08 revision of this PRD changed FR bodies without changing IDs; a sprint-change proposal that carries its rules into UX, architecture, and Epic 6–8 stories, a re-baselined conformance checklist, and a new independent readiness rerun are required before any Epic 6–8 story is scheduled. That proposal carries the landing map for each rule. Repository-local dependency/version gates and implementation mechanisms are tracked in the addendum and must be verified before affected stories start.

## 3. Target Users and Journeys

### 3.1 Primary User

The primary user works in Hexalith.Chatbot across multiple Conversations and resources and expects the assistant to resume the correct Project without requiring context to be rebuilt each session.

### 3.2 Runtime Roles and Operations

| Role and surface | Purpose | Authority |
| --- | --- | --- |
| **Project User (Chatbot)** | Works with authorized Projects and confirms user-intent decisions. | Read authority from Folder read (§2.1) over their permitted Projects and their own tasks; creation; with Folder manage: setup, actor-selected or confirmed inferred additive link and initial Folder set, archive, restore, move, unlink, and Folder replacement (manage on both Folders, no reader loss); confirmation of a resolution or proposed creation, which is Chatbot-only; no Safe Diagnostic Export. |
| **Tenant Operator (Web/CLI/MCP)** | Inspects operational metadata and performs authorized lifecycle operations. | Safe Metadata read; Descriptive Metadata only with separate, audited inspection authorization; archive/restore Preview and confirmation on Web, on CLI once §9 A-7 holds, and on MCP once §9 A-7 holds and the addendum's consequential-MCP gate passes; restore may only rebind the prior Folder; identifies Projects by opaque identifier unless holding inspection authorization; no move, Folder replacement, unlink, additive link, or resolution/proposal confirmation; Safe Diagnostic Export only with separate authorization. |
| **Tenant Project Administrator (Web/CLI/MCP)** | Has Tenant Operator capabilities plus authorized administrative association operations. | Tenant Operator authority plus administrative associations, which are exactly Conversation move (FR-7), Folder replacement and quarantined-legacy Folder binding (FR-8, the only path that may reduce a permitted set, disclosed as a coarse class and audited), and unlink (FR-11); bounded reconciliation (FR-22); no additive link, no initial Folder set, no resolution/proposal confirmation; Safe Diagnostic Export only with separate authorization. |
| **Service/Workflow Caller** | Acts for a real actor through an authorized workflow. | A caller without the original actor's interactive session. Delegated scope only; every decision is evaluated against the original actor, never the workload role; may perform reads, FR-3 setup updates, and FR-1 creation with no Folder supplied (Projects then performs same-name creation naming the original actor as sole manager, which is how the historical name-only path is served), all within the actor's authority; never receives or consumes a Confirmation Artifact or Selection Evidence and therefore never links, confirms, or binds an existing Folder; never gains autonomous confirmation or blanket mutation authority. |

### 3.3 Jobs To Be Done

- Resume AI work without manually rebuilding Project Context.
- Keep Project-related Conversations, the Project Folder, File References, and Memories connected.
- Start a Conversation and safely resolve or create the correct Project.
- Understand and recover consequential work after expiry, dependency failure, timeout, or lost response.
- Prevent context leakage across Projects, actors, and Tenants.
- Give downstream agents and operators durable server truth instead of transient UI state.

### 3.4 Key User Journeys

- **UJ-1. Priya resumes an existing Project.** Priya opens Chatbot and selects an authorized Project. Chatbot receives a read-model-confirmed Active Project and its current authorized Project Context. Priya sees continuity without manually reattaching prior work; stale or unavailable references are disclosed safely rather than silently omitted.

- **UJ-2. Jules creates a Project from attached work.** Jules attaches files before choosing a Project. When no suitable Project exists, Chatbot presents a proposal without creating anything silently. Jules confirms a server-issued preview. A Durable Task verifies or creates the Project Folder, binds authorized references, and exposes the Project as Active only after read-model confirmation. Jules can recover the task after a lost response.

- **UJ-3. Sam resolves an ambiguous Conversation.** Sam starts a Conversation that matches several Projects. Chatbot presents accessible, unselected candidates with current reason metadata. Sam confirms one candidate using a single-use Confirmation Artifact. A Durable Task records the association; expired or stale confirmation returns Sam to a fresh preview.

- **UJ-4. Alex protects unrelated work.** Alex works on Projects with overlapping terminology. Project Resolution and Project Context use only current authorized references. Unconfirmed candidates, foreign payloads, transient traces, and unrelated Tenant data never enter the active context.

- **UJ-5. Morgan restores an archived Project.** Morgan, a Tenant Project Administrator, previews the current archived Project. Restore verifies actor authority, Project version, and exactly one authorized Project Folder. If Folder evidence is missing, Morgan chooses a replacement, which the Preview confirms covers the Project's current permitted set, or confirms same-name creation naming a designated Project User as manager. The Project becomes Active only after the Durable Task and read model confirm completion.

## 4. Glossary

- **Project** — A durable AI workspace boundary managed by Hexalith.Projects. It contains metadata, Project Setup, lifecycle state, and authorized references to resources owned by other bounded contexts.
- **Project Context** — The authorized Project metadata, setup, reference metadata, and inclusion/exclusion evidence Chatbot may use for a Conversation. It contains references, not foreign payloads.
- **Context Response State** — The observable usability state of a Project list, resolution, context, or Conversation-start response: `Complete`, `Partial`, `Unavailable`, or `Denied`. It is response metadata, not Project Lifecycle State or Task Status.
- **Evidence Freshness State** — The current verification state of an authorized response component: `Current`, `Stale`, `Rebuilding`, or `Unavailable`.
- **Recovery Action Code** — A safe next action returned with a non-complete response or workflow outcome: `None`, `Retry`, `RefreshContext`, `RequestPreview`, `RenewPreview`, `PollTask`, `ResolveNeedsAttention`, `SelectAlternative`, or `ContactAdministrator`. `RequestPreview` is returned when an Inferred Association or other consequential action was attempted without a Confirmation Artifact; `RenewPreview` when a Confirmation Artifact is expired or stale.
- **Conversation** — A conversation owned by `Hexalith.Conversations`. Projects references its stable identity and metadata. In v1, a Conversation is associated with at most one Project at any time; a Conversation whose membership was unlinked keeps a prior-membership record, retained for the Conversation's lifetime, so that any later link is treated as a move. The record is an opaque receipt: it discloses the prior Project only to an actor permitted for that Project and otherwise appears as the safe code `PriorMembershipRecorded`.
- **Context Reference** — A Project's association to one Conversation, File Reference, or Memory. The Project Folder is not a Context Reference; it is the Project's exclusive authorization anchor.
- **Project Folder** — The single canonical Folder owned by `Hexalith.Folders` and referenced by every Active Project. A Folder is the Project Folder of at most one Active Project.
- **File Reference** — An authorized file identity and metadata owned by `Hexalith.Folders` and optionally linked to a Project.
- **Memory** — A durable resource owned by `Hexalith.Memories`; Projects stores only its authorized identity and metadata.
- **Project Setup** — Durable goals, user-facing instructions, context preferences, source inclusion/exclusion policy over the Project's existing Context References (including which references are required), and Conversation-start defaults used to initialize or resume a Project. Setup filters Context References; it never introduces one.
- **Project Lifecycle State** — The availability state of a Project. v1 supports exactly `Active` and `Archived`.
- **Durable Task** — Recoverable server truth for consequential or cross-context work. Its state is separate from Project Lifecycle State.
- **Task Status** — One of `Pending`, `Running`, `WaitingForDependency`, `NeedsAttention`, `Succeeded`, `Rejected`, `Failed`, or `Cancelled`. The last four are terminal; `NeedsAttention` is recoverable and nonterminal.
- **Preview** — Current server-derived metadata describing a consequential action before confirmation. Its Preview body is the human-readable part (Project name, Setup excerpts, titles) and is Descriptive Metadata; the rest is Safe Metadata.
- **Project User, Tenant Operator, Tenant Project Administrator, Service/Workflow Caller** — The runtime roles defined in §3.2. A **Tenant-role actor** is a Tenant Operator or Tenant Project Administrator. A Project's **permitted set** is the actors holding Folder read on its Project Folder; **Folder read** and **Folder manage** are the two `Hexalith.Folders` authorization levels §2.1 derives authority from.
- **Confirmation Artifact** — An expiring, single-use, tamper-evident authorization bound to Tenant, actor, the actor's interactive session, action, targets, normalized request, Preview, and the current resource and authorization evidence. It is invalidated when any of those changes and is never issued to or consumed by a Service/Workflow Caller.
- **Selection Evidence** — A single-use, short-lived proof, produced by FR-25, that the actor personally picked a target for a specific subject on an interactive surface. An association without it is an Inferred Association (§9 A-6).
- **Inferred Association** — Any Project association whose request carries no Selection Evidence. Inferred Associations always require Preview and confirmation and are otherwise rejected with `RequestPreview`. An **actor-selected** association is one whose request carries valid Selection Evidence; a caller-asserted flag is not evidence.
- **Idempotency Key** — A caller-supplied request identity scoped to Tenant (of the acting session), actor, and operation. Equivalent reuse returns the original Durable Task; materially different reuse, including different action targets, returns a conflict that discloses nothing about the original task.
- **Project Resolution** — Current recomputation that identifies which Project should apply when no explicit Project is selected.
- **Candidate Project** — An authorized Project returned by Project Resolution as a possible match.
- **Resolution Result** — `NoMatch`, `SingleCandidate`, or `MultipleCandidates`. `SingleCandidate` is a proposal, not a decision: unless its reasons include `ConversationLinked` (an existing confirmed membership, nothing to bind), binding it is an Inferred Association.
- **Resolution Reason Code** — Current metadata explaining a Candidate Project, including `ConversationLinked`, `ProjectFolderMatched`, `FileReferenceMatched`, `MemoryMatched`, and `MetadataMatched`.
- **Resolution Trace** — Request-scoped, nonpersistent diagnostic evidence for the current Project Resolution or Project Context selection computation. It carries Safe Metadata only.
- **Read-Model-Confirmed Completion** — Completion established by the authoritative read model after durable mutation, not by request acknowledgement or notification.
- **Safe Diagnostic Export** — A bounded, non-retained, Safe Metadata operational export produced from an already-authorized diagnostic view.
- **Safe Metadata** — The allow-list that every audit event, Resolution Trace, telemetry record, error, measurement record, pre-activation task status, and Safe Diagnostic Export may carry: Tenant, actor, Project, task, and reference identifiers; lifecycle state; resource versions; timestamps; Task Status; safe reason, freshness, outcome, and Recovery Action Codes; counts; and stable upstream receipt identifiers. Identifiers in Safe channels are opaque, reason codes are enumerated and never embed names, and actor identifiers in Safe Diagnostic Export are pseudonymous and Tenant-salted so one person yields unrelated identifiers across Tenants.
- **Descriptive Metadata** — Human-readable Project content that is authorized data, not Safe Metadata: Project name, Project Setup bodies, Conversation titles, Folder names, File names and paths, Memory titles, and Preview bodies. Reference-level titles and names are served only when the actor's current read authorization on that reference in its owning system is verified at response time; otherwise the reference appears as an opaque identifier with a safe code. It is served to authorized Project Users and, with separate descriptive-metadata inspection authorization (a per-Tenant role permission whose every use is audited per Project under FR-21), to Tenant-role actors; it never enters Safe Metadata channels, and Tenant-role actors obtain it by no other path.
- **Metadata Classification** — A system-supplied classification required on canonical creation requests; it is not a user-authored creation field.
- **Tenant** — The Hexalith isolation boundary for Projects and every referenced resource.

## 5. Observable Context and Recovery Contract

Project open, list, resolution, context, Conversation-start, and proposal-recovery responses share the following logical fields. Exact wire names, casing, serialization, and transport mappings belong in API/architecture contracts; every supported surface must preserve these semantics.

- `responseState`: one Context Response State.
- `asOf`: the server timestamp of the authorization and evidence computation.
- `projectVersion`: the authorized current Project version when a Project may safely be disclosed.
- `resolutionResult`: `NoMatch`, `SingleCandidate`, or `MultipleCandidates` when resolution ran.
- `components`: metadata-only entries containing component kind, inclusion status (`Included` or `Excluded`), Evidence Freshness State, safe reason code, and last-verified timestamp when known.
- `recoveryActions`: zero or more Recovery Action Codes with only the applicable metadata: task identity/status, Preview expiry, and bounded retry-after guidance.

The response states have binding consequences:

- Required evidence per purpose: for open, context, and Conversation-start, the required components are the Project record, its Project Folder, its Project Setup, and the actor's current authorization evidence; for list and resolution, the response-level state is `Complete` when the enumeration itself is authorized and current, and each Project entry or candidate carries its own state, requiring the Project record, its Project Folder, and authorization evidence, with an entry whose Folder is not `Current` reported `Unavailable`; for proposal recovery, the task record and authorization evidence. Conversation, File Reference, and Memory components are optional unless Project Setup marks a reference as required, in which case it joins the required set.
- `Complete` means every required component is `Current` and every optional component is `Current` or `Excluded` with reason code `ExcludedBySetupPolicy`; the response is usable for its requested purpose.
- `Partial` means every required component is `Current`, while one or more optional references are excluded, stale, rebuilding, or unavailable. The response may be used only with every omission represented in `components`.
- `Unavailable` means a required component is missing, stale, rebuilding, or unavailable. The response cannot initialize or resume a Conversation and returns `Retry`, `RefreshContext`, `SelectAlternative`, or `ContactAdministrator` as applicable.
- `Denied` means current actor authorization failed. Unauthorized, cross-Tenant, and nonexistent targets produce one indistinguishable `Denied` outcome for every read, Preview, single-target resolution input, and task-status request; for multi-input resolution (FR-13) such an input is indistinguishable from an authorized input with no match so that one bad attachment does not deny the request; it discloses no protected Project/component metadata and becomes eligible for another outcome only through a newly authorized request.

Refresh and recovery never rewrite an earlier response silently. A refresh recomputes authorization and evidence and returns a new `asOf`, `projectVersion`, state, and component set. `Partial` or `Unavailable` becomes `Complete` only after fresh recomputation proves all required evidence current. Expired or stale confirmation returns `RenewPreview` and admits no task; a lost admission response returns `PollTask` or an equivalent Idempotency Key retry that resolves to the original task before any single-use artifact is re-validated. Dependency delay returns `WaitingForDependency`; human-recoverable work returns `NeedsAttention` plus `ResolveNeedsAttention`; terminal Task Status values remain immutable.

## 6. Functional Requirements

### 6.1 Project Workspace Management

**Description:** Projects provides the durable workspace record and recoverable workflows used to create, open, update, archive, restore, and list AI Projects. Project name remains the only required user-authored creation input.

#### FR-1: Create Project

Chatbot can admit Project creation as an idempotent Durable Task. A Project becomes caller-visible and `Active` only after exactly one authorized Project Folder is verified and bound. Realizes UJ-2.

**Consequences (testable):**

- The only required user-authored field is Project name; canonical requests also carry a valid system-supplied Metadata Classification.
- Any Project User can create; a supplied Project Folder requires the actor's manage authorization, is verified, same-Tenant, and not already the Project Folder of another Active Project. When none is supplied, Projects requests create-only same-name Folder creation from `Hexalith.Folders` naming the original actor as sole manager, on every request shape including the historical name-only one; a name collision fails closed and never binds a pre-existing Folder. Binding an existing Folder is actor-selected (FR-25) or an inferred binding confirmed under FR-8/FR-15; it is never implicit.
- A reserved or orphaned Folder left by a failed creation stays in `NeedsAttention` and never becomes an implicit binding target (§9 A-2).
- A Folder created by Projects for a Project User names that actor as sole manager with no Tenant-default read (an Administrator-initiated creation carries no creator grant, FR-8); Projects' workload identity holds no Folder authorization (§2.1).
- Admission returns a pollable Durable Task rather than an immediately Active Project.
- Dependency denial, timeout, cancellation, duplicate delivery, lost response, or reconciliation never exposes an Active folderless Project.
- Equivalent Idempotency Key retries return the original task. A materially different request using the same scoped key returns an idempotency conflict.
- Terminal success exposes the Project identity only after Read-Model-Confirmed Completion.
- Historical unversioned name-only creation requests remain supported throughout v1; retirement requires an explicitly approved major version.
- Creation never duplicates transcripts, file contents, prompts, secrets, or Memory payloads.

#### FR-2: Open Project

Chatbot can open an authorized Project and receive the metadata, lifecycle state, Project Setup, and references needed to initialize a Conversation. Realizes UJ-1.

**Consequences (testable):**

- Opening returns only data visible to the requesting Tenant and actor.
- Opening follows the Context Response State, Evidence Freshness State, and Recovery Action Code semantics in §5.
- Pre-activation creation tasks are not exposed through Project open APIs.
- Archived or unavailable Projects are identified and cannot silently become active Conversation context.

#### FR-3: Update Project Setup

A Project User with Folder manage authority can update Project Setup used for Conversation continuity through Chatbot.

**Consequences (testable):**

- Updates are admitted as an idempotent Durable Task without Preview, are durable, and are observable from the authoritative read model.
- Setup may include goals, user-facing instructions, context preferences, source inclusion/exclusion policy including required-reference marking, and Conversation-start defaults.
- Setup source policy may name only existing Context References of the Project; any other reference identifier is rejected as unsupported (FR-19). Setup filters Context References and never introduces one.
- Setup describes Conversation behavior and context policy, not model-provider internals.
- Updates remain additive and serialization-tolerant and reject secrets, unrestricted paths, and foreign payloads.
- A Setup change produces a new `projectVersion`. It never widens an already-admitted Conversation-start or context snapshot; a changed inclusion/exclusion policy applies only at the next Conversation start or FR-18 refresh, which reports policy-driven inclusion changes as components with a safe reason code.

#### FR-4: Archive Project

An authorized Project User, Tenant Operator, or Tenant Project Administrator can archive an Active Project through server Preview, single-use confirmation, and an idempotent Durable Task. FR-23 defines the corresponding restore operation.

**Consequences (testable):**

- Project Lifecycle State remains limited to `Active` and `Archived`.
- Confirmation is invalidated when actor authority or Project version changes.
- Archived Projects are excluded from Project Resolution unless explicitly requested.
- Completion is not reported until the read model confirms `Archived`.
- Existing references remain auditable after archival.

#### FR-5: List Projects

Authorized callers can list visible Active and Archived Projects.

**Consequences (testable):**

- Results are Tenant-scoped, authorization-filtered, and filterable by Project Lifecycle State.
- Each result contains authorized Project identity, lifecycle state, current Project version, Project Folder availability, and the §5 response/freshness/recovery metadata needed for selection without loading full Project Context; Project name is included for Project Users and, for Tenant-role callers, only under descriptive-metadata inspection authorization (FR-22). Chatbot list responses are Folder-derived only; Tenant-role entries appear only on Web, CLI, and MCP.
- Pre-activation tasks never appear as Projects.
- Pagination follows NFR-6; cursors remain scoped to the authenticated query.

### 6.2 Context References and Project Folder

**Description:** Projects owns associations to Conversations, the Project Folder, File References, and Memories. The referenced bounded contexts remain authoritative for payloads and authorization. Every referenced resource belongs to the Project's Tenant; a reference whose Tenant differs fails closed regardless of actor authorization on both ends. Cross-context work produces durable receipts and remains recoverable after retries, duplicate delivery, concurrency, or lost responses.

#### FR-6: Link Conversation

An authorized Project User can link an existing Conversation to a Project. Realizes UJ-1 and UJ-3.

**Consequences (testable):**

- A Conversation is associated with at most one Project at any time and never more than one; the Conversation and the Project belong to the same Tenant, and the actor holds current read authorization on the Conversation in `Hexalith.Conversations` and manage on the Project Folder.
- An actor-selected additive link (valid Selection Evidence, §4) uses an idempotent Durable Task without a second confirmation; an Inferred Association, including a `SingleCandidate` result whose reasons do not include `ConversationLinked`, requires Preview and confirmation and is otherwise rejected with `RequestPreview`.
- Linking a Conversation that currently has, or previously had, a confirmed Project membership is classified as a move governed by FR-7, never an additive link; unlink followed by link cannot bypass FR-7. That an actor authorized on the Conversation can learn it once had a membership is accepted; which Project is disclosed only per FR-7.
- Membership mutation is atomic and fails closed: concurrent links, a link racing a move, duplicate delivery, or a lost response cannot leave two memberships valid; a lost response converges to one receipt.
- Authorization failure prevents any protected resource access or durable effect.
- The link stores stable identity and metadata, never transcript content.

#### FR-7: Move Conversation Between Projects

An authorized Project User or Tenant Project Administrator can move a Conversation through Preview, single-use confirmation, and an idempotent Durable Task.

**Consequences (testable):**

- Preview binds both Projects (or the prior-membership record as an opaque receipt after an unlink), the Conversation, actor, and current resource versions; all belong to one Tenant.
- Authority: a Project User needs manage on the target Project Folder, read on the Conversation, and, while the prior membership is still bound, read on the prior Project; after a confirmed unlink, target and Conversation only. A Tenant Project Administrator acts under Tenant-role authority that Conversations recognizes (§9 A-3). The prior Project is disclosed in Preview only to an actor permitted for it and otherwise as `PriorMembershipRecorded`.
- Completion yields exactly one Project membership and a durable cross-context receipt.
- Failure, duplicate delivery, or lost response cannot leave two memberships silently valid.
- The move is audited using metadata only and fails closed when either Project or the Conversation cannot be authorized.

#### FR-8: Set Project Folder

A Project User sets the single Project Folder within FR-1 creation or FR-23 restore with manage on that Folder; a Project User with manage authority on both Folders or a Tenant Project Administrator can replace it through Preview and confirmation. A Tenant Project Administrator can bind the Project Folder of a quarantined legacy folderless Project through the same Preview and confirmation. Realizes UJ-2.

**Consequences (testable):**

- Every Active Project has exactly one authorized Project Folder, and a Folder is the Project Folder of at most one Active Project. Set, replacement, and restore fail closed when the target Folder is already another Active Project's Folder or belongs to another Tenant.
- Initial actor-selected binding is idempotent; inferred binding requires confirmation.
- Replacement binds old and new Folder evidence to the Confirmation Artifact and completes only after the authoritative read model confirms the replacement.
- Because permitted Projects are Folder-derived, replacement and legacy binding change who is permitted. A Project User replacement fails closed unless the target Folder's readers cover the current permitted set; the Preview shows the delta as counts, which the actor can already read in Folders. A Tenant Project Administrator replacement or legacy binding is rejected when the Administrator is in the target Folder's reader set; it may reduce the permitted set only when the current Folder is invalid or missing, otherwise the target's readers must cover the current permitted set. Its Preview discloses only whether coverage holds (`NoLoss` or `Loss`), every such Preview and completion is audited under FR-21 with that class (never counts), and the residual one-bit disclosure per audited Preview is accepted. A same-name Folder created on an Administrator's behalf names as sole manager the Project User the Administrator designates in the Preview, never the Administrator. The Confirmation Artifact binds both Folders' reader-set evidence so a change between Preview and confirmation invalidates it (§9 A-3).
- Projects stores Folder identity and metadata, never file contents or unrestricted paths.
- `Hexalith.Folders` remains the authorization and system-of-record boundary.

#### FR-9: Link File Reference

An authorized Project User can link a File Reference without changing the Project Folder.

**Consequences (testable):**

- File References are optional and do not replace the Project Folder.
- Actor-selected additive linking is idempotent; inferred linking requires confirmation.
- The actor holds current read authorization on the File in `Hexalith.Folders` and manage on the Project Folder. Linking a File outside the bound Project Folder requires confirmation whose Preview identifies the foreign Folder and, only when the actor is permitted for it, any Active Project bound to that Folder (otherwise a safe code); the File and the Project belong to one Tenant.
- Re-homing a File to another Project is intentionally two audited actions (FR-11 unlink, then link); the link audit event carries the prior-link receipt when one exists.
- Linking uses an idempotent Durable Task with Read-Model-Confirmed Completion; a lost response recovers through `PollTask` or an equivalent Idempotency Key retry, and stale or unavailable authorization evidence fails closed.
- Projects stores stable File identity and metadata only; authorization remains delegated to `Hexalith.Folders` and is re-checked on every context assembly.

#### FR-10: Link Memory

An authorized Project User can link a Memory. Realizes UJ-1 and UJ-3.

**Consequences (testable):**

- Actor-selected additive linking is idempotent; inferred linking requires confirmation; the Memory and the Project belong to one Tenant, and the actor holds current read authorization on the Memory in `Hexalith.Memories` and manage on the Project Folder.
- Linking uses an idempotent Durable Task with Read-Model-Confirmed Completion; a lost response recovers through `PollTask` or an equivalent Idempotency Key retry, and stale or unavailable authorization evidence fails closed.
- Projects stores stable Memory identity and metadata only.
- Authorization remains delegated to `Hexalith.Memories` and is re-checked on every context assembly.
- Re-homing a Memory to another Project is intentionally two audited actions (FR-11 unlink, then link); the link audit event carries the prior-link receipt when one exists.

#### FR-11: Unlink Context Reference

An authorized Project User or Tenant Project Administrator can unlink a Conversation, File Reference, or Memory through Preview, confirmation, and an idempotent Durable Task. The Project Folder can be replaced but not removed from an Active Project.

**Consequences (testable):**

- Unlinking removes only the association and never deletes the underlying resource.
- Preview identifies the affected reference and current Project version.
- Completion is durable, audited using metadata only, and confirmed by the read model.
- The operation fails closed on stale authorization or resource evidence.
- Unlinking a Conversation leaves it detached with its prior-membership record retained for the Conversation's lifetime; zero membership arises only from a confirmed unlink or a transient `NeedsAttention` in which the prior membership stays bound, never silently. Re-linking a detached Conversation is an FR-7 move that does not require authorization on the prior Project.

#### FR-25: Select Association Target

A Project User can pick the target of an additive association (a Project for a Conversation, File, or Memory link, or a Folder for FR-1/FR-23 binding) inside the Projects-owned selection component, producing Selection Evidence. Realizes UJ-1 and UJ-2.

**Consequences (testable):**

- The component is hosted only on adapters the platform registers as interactive and that carry Project User sessions (Chatbot in v1; Web or MCP only when a Project User surface is registered there and, for MCP, the consequential-MCP gates pass); the enumeration it renders is Projects-served and limited to Projects on whose Folder the actor holds manage, or, for Folder targets, Folders on which `Hexalith.Folders` confirms manage at pick time.
- Selection Evidence is minted by the actor's pick, never by an API call a caller makes on the actor's behalf, and is bound to Tenant, actor, interactive session, action, the subject reference, the target, and current versions.
- It is single-use, expires within the NFR-8 bound, and is never attached to open, list, resolution, or proposal responses.
- Consumption is recorded against the admitted Durable Task; an equivalent Idempotency Key retry resolves to that task before evidence is re-validated, so a lost response never demands a new pick. The same order applies to Confirmation Artifacts (§5).
- A link or binding request whose evidence is missing, expired, consumed, mismatched on subject or target, or minted on another surface is rejected with `RequestPreview`.

### 6.3 Project Resolution

**Description:** Projects recomputes Candidate Projects from current authorized metadata. Resolution favors explicit intent over silent attachment and does not retain candidate-score history.

#### FR-12: Resolve Project From Conversation

Chatbot can request Candidate Projects for a Conversation with no explicit Project. Realizes UJ-3.

**Consequences (testable):**

- The result is `NoMatch`, `SingleCandidate`, or `MultipleCandidates` with current Resolution Reason Codes.
- Only Active, read-model-confirmed Projects are considered by default. A Conversation with an existing confirmed membership short-circuits to `SingleCandidate` with `ConversationLinked`; it is never a resolution episode.
- Pre-activation tasks and unauthorized or stale resources cannot become candidates. Because binding a candidate requires manage (FR-14, FR-25), a Project on whose Folder the actor holds only read is returned with the safe reason code `ReadOnlyCandidate`, is excluded from FR-14's accept action, and does not count in SM-7.
- The response follows the §5 contract; `Unavailable` and `Denied` never return a selected candidate, and no result is a selection: a `SingleCandidate` whose reasons do not include `ConversationLinked` is an Inferred Association that Chatbot may bind only through FR-14 confirmation.
- Authorization filtering never manufactures certainty: when unauthorized or stale siblings were hidden, the response still follows the Inferred Association rule rather than presenting an exclusive match.
- An unauthorized, cross-Tenant, or nonexistent Conversation input yields the single `Denied` outcome defined in §5, with no Project or component fields; `NoMatch` is returned only for an authorized Conversation.

#### FR-13: Resolve Project From Attachments

Chatbot can resolve Candidate Projects from an attached Project Folder or File References. Realizes UJ-2.

**Consequences (testable):**

- Matching uses current authorized Folder/File identity and metadata, not file contents.
- Applicable candidates include `ProjectFolderMatched` or `FileReferenceMatched` reason codes.
- Missing, stale, or unavailable authorization evidence fails closed. Foreign-Tenant, unauthorized, or unknown attachments are never resolution inputs, and at the request level they produce the same outcome as an authorized attachment with no match.
- A sole match from attachments is an Inferred Association and follows FR-14.

#### FR-14: Confirm Ambiguous Project

When resolution returns candidates that are not an existing confirmed membership, Chatbot presents an accessible, unselected comparison and records the Project User's choice through a Confirmation Artifact and Durable Task. This applies to `MultipleCandidates` and to any `SingleCandidate` that is an Inferred Association. Realizes UJ-3.

**Consequences (testable):**

- No candidate is silently or visually preselected; a sole candidate is presented with its reason metadata and explicit accept and decline actions.
- The artifact is bound to Tenant, actor, action, Conversation, candidates, normalized request, Preview, and current versions; it expires after 15 minutes and is single-use.
- The artifact is bound to the Project User's authenticated interactive Chatbot session and is consumable only by that actor on that session; Chatbot transports it as a dual-principal adapter and cannot originate or self-confirm; a Service/Workflow Caller never receives or consumes it (§9 A-7).
- Stale, expired, replayed, or tampered confirmation is rejected safely and requires a fresh Preview.
- Confirmation requires the authority of the association it confirms: manage on the chosen Project's Folder and read on the Conversation. When the Conversation already has a confirmed membership, the confirmed choice is an FR-7 move and the artifact carries FR-7's bindings.
- Only Read-Model-Confirmed Completion creates or updates the Conversation association and audit history.
- Chatbot supports states for confirmation, cancellation, retry, expiry or staleness, lost-response recovery, and task status.

#### FR-15: Propose New Project

When no suitable Project exists, Chatbot can present a proposed Project and admit creation only after the Project User confirms a bound Preview. Realizes UJ-2.

**Consequences (testable):**

- The proposal includes a suggested Project name whenever attachment or Conversation metadata supplies one, may suggest setup metadata, and creates nothing before confirmation.
- The Confirmation Artifact binds the initiating Conversation, authorized attachments, Folder plan, normalized request, and current evidence.
- Confirmed creation follows FR-1: no prior Project authority is needed, a Folder in the plan requires the actor's manage authorization, and a created Folder names the actor as sole manager. No Project is exposed before Folder binding and read-model confirmation.
- Non-success outcomes follow the §5 recovery contract; cancellation returns `Cancelled`, terminal failure returns `Failed`, and expired or stale evidence creates no task.

### 6.4 Project Context Assembly

**Description:** Projects supplies scoped and explainable Project Context without accidental cross-Project contamination.

#### FR-16: Get Project Context

Chatbot can request Project Context for an Active Project on behalf of a named Conversation. Realizes UJ-1 and UJ-4.

**Consequences (testable):**

- The request names the Conversation; Projects serves context only for the Project that Conversation is a member of, or, when it has no membership, only for a Project the actor explicitly opened in that session, so Chatbot cannot inject another Project's context into a membered Conversation.
- Context is Tenant-scoped, actor-authorized, and available only for a read-model-confirmed Active Project with exactly one authorized Project Folder; every included reference is same-Tenant and its authorization is re-checked at assembly time.
- It contains Project Setup and reference metadata, not payloads owned by other bounded contexts; included references come only from the Project's Context Reference set, filtered by Setup policy.
- It follows the §5 contract, representing every excluded, stale, rebuilding, or unavailable reference as a metadata-only component; `Denied` discloses no protected detail.

#### FR-17: Explain Context Selection

Authorized callers can obtain current metadata explaining why a reference was included or excluded. Realizes UJ-4.

**Consequences (testable):**

- Explanations are current Resolution Traces, not reconstructed history.
- Traces carry Safe Metadata only: no secrets, payloads, prompts, unrestricted paths, raw upstream problems, unconfirmed-candidate detail, Project Setup bodies, or titles.
- Traces are request-scoped and not persisted; only confirmed outcomes enter audit history.

#### FR-18: Refresh Project Context

Chatbot can request a read-only refresh after links, setup, authorization, or resource availability changes.

**Consequences (testable):**

- Refresh recomputes from current authorized Project, Conversation, Folder, File Reference, Memory, and version metadata.
- Refresh itself never mutates Project or reference state and creates no maintenance audit event.
- The refreshed response follows §5, including new snapshot metadata, component evidence, recovery actions, and the binding transition rules for `Partial`, `Unavailable`, and `Complete`.

### 6.5 Project Setup Quality

**Description:** Project Setup is useful for Conversation continuity without making Projects responsible for prompt construction, model orchestration, or payload storage.

#### FR-19: Validate Project Setup

Projects validates setup and creation admission before accepting durable work.

**Consequences (testable):**

- Project name remains the only required user-authored creation field.
- Canonical creation requests require valid system-supplied Metadata Classification; invalid classification is rejected before command submission.
- Validation permits a supplied authorized Project Folder or same-name Folder creation, but never defaults a caller-visible Project to Active before Folder completion.
- Validation permits a supplied Project Folder only when it is same-Tenant and not another Active Project's Folder; same-name creation is create-only and fails closed on collision.
- Validation rejects secrets, unrestricted paths, unsupported references (including any Setup source reference that is not an existing Context Reference of the Project), and foreign payloads. Rejectable secret classes are credential-shaped tokens (provider API-key prefixes, bearer tokens), PEM key blocks, connection strings with embedded credentials, and known secret-reference formats; residual Setup text is Descriptive Metadata and never enters Safe Metadata channels.
- Validation rejects Unicode line separators and other control or invisible characters in identifiers and envelope fields (addendum §7.3).
- Failures identify safe field/reason codes without echoing sensitive values.

#### FR-20: Retrieve Conversation-Start Setup

Chatbot can retrieve the subset of Project Setup needed to start or resume a Conversation.

**Consequences (testable):**

- The result includes goals, user-facing instructions, context preferences, and default source policy.
- It excludes internal audit metadata and unavailable or unauthorized references.
- The request names the Conversation and follows the FR-16 membership rule. It is bound to one authorized `projectVersion` and `asOf` snapshot and follows §5. Chatbot may admit the first response only for `Complete` or `Partial`; `Unavailable` or `Denied` blocks first-response admission and returns the applicable Recovery Action Codes without re-querying every bounded context.

### 6.6 Audit and Operations

**Description:** Projects exposes metadata-only operational truth while keeping authority action-specific. Web, CLI, MCP, or Chatbot surface choice never expands permission.

#### FR-21: Record Project Audit Events

Projects records metadata-only audit events for consequential task admission and outcome, confirmed Project mutations, security-relevant confirmation outcomes, reconciliation, and Safe Diagnostic Export.

**Consequences (testable):**

- Audit covers task admission and terminal outcome; confirmation use and cancellation; rejection of stale, replayed, or tampered confirmations; authorization denial; creation, archive, restore, move, Folder replacement, link, unlink, confirmed resolution, and confirmed proposed creation; bounded reconciliation; Folder re-anchoring with its permitted-set delta as counts for Project User replacements and as coverage class for Administrator re-anchoring (FR-8); Setup updates with the admitting credential kind (interactive or delegated); each descriptive-metadata inspection by a Tenant-role actor (actor, Project, timestamp, field class); and Safe Diagnostic Export creation. Audit also records stable upstream receipt identifiers.
- Equivalent idempotent retries do not create duplicate audit events.
- Intermediate task states, polls, retries, dependency latency, notifications, unused expiry, and read-only Resolution Traces remain operational telemetry rather than durable audit.
- Audit contains Safe Metadata only: Tenant, actor, Project/action identity, timestamp, safe reason/outcome codes, and affected reference identifiers; never payloads, secrets, or Descriptive Metadata.

#### FR-22: Support Operator Read Access and Task Reconciliation

Tenant Operators and Tenant Project Administrators can inspect authorized Project metadata, lifecycle state, references, Durable Task status, confirmed resolution outcomes, and audit metadata.

**Consequences (testable):**

- Access is Tenant-scoped, action-authorized, and Safe Metadata only across Web, CLI, and MCP; Descriptive Metadata requires separate descriptive-metadata inspection authorization, is never included by default, and every such read is audited (§9 A-5). The same rule governs Project name in FR-5 results and Preview bodies in FR-4/FR-23 for Tenant-role callers.
- Inspection is per Project and per task. Reference and audit reads are scoped to one Project and paged under NFR-6; v1 offers no cross-Project audit or reference enumeration.
- Project Users may inspect only their own permitted task status through Chatbot.
- Pre-activation tasks remain separate from Project list/open APIs. Their status, for any role, carries only task identity, Task Status, safe reason and Recovery Action Codes, timestamps, and expiry; reserved Project identity, names, Folder/File/Memory identifiers, Setup, and Conversation identifiers are withheld until Read-Model-Confirmed Completion.
- Reconciliation by a Tenant Project Administrator is a task-control action on an existing Durable Task: it needs no Confirmation Artifact and creates no new task, and it is audited. It acts only on an already-admitted task against its originally bound Tenant, actor intent, targets, and Preview, and it re-evaluates the original actor's current authorization at commit, failing closed to `Rejected`. It cannot add or change targets, confirm a resolution or proposal, complete membership without the original bindings, or admit new intent; new intent requires a new Preview by an authorized actor.
- The quarantined-legacy inventory (§2.4) is readable here by Tenant Project Administrators as Safe Metadata plus, as a narrowly scoped and audited exception to A-5, the Project name and any pre-v1 Folder or creation receipt of each quarantined Project, never Setup or titles, with one FR-21 inspection event per Project returned (role-gated until the A-5 permission exists, then gated on it); binding a Folder to a quarantined Project is an FR-8 operation, not reconciliation.
- Read permission alone grants neither Safe Diagnostic Export nor a mutation.

#### FR-23: Restore Archived Project

An authorized Project User, Tenant Operator, or Tenant Project Administrator can restore an Archived Project through Preview, confirmation, and an idempotent Durable Task. Only an actor with FR-8 replacement authority may restore with a replacement or newly created Folder; when the prior Folder is missing or unreadable its permitted set is undefined, so such a restore is a Tenant Project Administrator operation under FR-8's rules. A Tenant Operator may only rebind the prior Folder. This is the restore counterpart to FR-4 and realizes UJ-5.

**Consequences (testable):**

- Preview verifies Tenant, actor, authority, current Project version, and exactly one authorized Project Folder.
- If the prior Folder is invalid or missing, Preview requires a replacement chosen by the actor (for a Project User, Selection Evidence under FR-25; for a Tenant Project Administrator, an inferred binding confirmed under FR-8) or create-only same-name Folder creation before confirmation under FR-8 authority and its permitted-set disclosure; the target is same-Tenant and not another Active Project's Folder, and an orphaned Folder is never the implicit target.
- The Project remains Archived until Folder evidence and read-model-confirmed restore completion succeed.
- If Folder creation succeeds but activation cannot commit, the task enters `NeedsAttention`; Projects never automatically deletes a Folders-owned resource.
- Stale/unavailable evidence, replay, cancellation, duplicate delivery, concurrency, and lost response cannot expose an invalid Active Project.
- Completion and reconciliation outcomes are audited using metadata only.

#### FR-24: Create Safe Diagnostic Export

A separately authorized Tenant Operator or Tenant Project Administrator can create a bounded Safe Diagnostic Export through Web, CLI, or MCP.

**Consequences (testable):**

- Export permission is distinct from FR-22 read permission; Chatbot cannot create exports.
- Export creation is a bounded synchronous read, not a Durable Task, and needs no Confirmation Artifact. A repeated or lost request yields a new current snapshot and a new audited attempt; Projects makes no exactly-once claim and returns no retained bytes.
- The export schema carries Safe Metadata only and names its fields explicitly; it never includes Descriptive Metadata.
- Every attempt and outcome is audited using metadata only.
- The complete encoded export, including envelope and truncation metadata, is at most 1 MiB and contains at most 500 reference rows and 100 audit rows.
- Reference ordering is stable and deterministic; audit rows are newest-first with stable tie-breaking.
- Truncation reports included/omitted counts and safe reasons without excluded detail; exports have no continuation cursor.
- Upstream unavailability is represented safely without raw errors or fabricated completeness.
- Projects never retains generated exports.

## 7. Cross-Cutting Non-Functional Requirements

### Security, Privacy, Reliability, and Recovery

- **NFR-1 — Security and privacy:** Every read, write, task, confirmation, audit event, and export is Tenant-, actor-, action-, target-, and current-version-scoped. Every referenced resource and the acting session are same-Tenant as the Project; authorization on both ends never substitutes for that predicate. Authorization is recomputed at every decision from current Folders and Tenant-role evidence. Authorization decisions bind to the original actor, never to workload identity. Trust-bearing mutations fail closed when authorization evidence is stale, unknown, rebuilding, or unavailable. Logs, telemetry, errors, measurement, and evidence carry Safe Metadata only.
- **NFR-2 — Encryption and key management:** Production traffic uses platform-approved authenticated encryption in transit. Durable Project, task, idempotency, and audit data uses platform-managed encryption at rest. Projects owns no private keys; approved platform KMS/secret-provider rotation and revocation evidence is release-blocking.
- **NFR-3 — Availability and recovery:** Authenticated metadata APIs and task admission target 99.9% monthly availability excluding planned maintenance. With required dependencies healthy, service RTO after process/node failure is 15 minutes, and accepted tasks resume or reach truthful `NeedsAttention` within 5 minutes.
- **NFR-4 — Durability and idempotency:** A Project event acknowledged as committed has RPO 0 within the configured primary-region durability domain. No Project admitted through v1 workflows is ever Active and folderless, and no caller-visible Active Project is folderless; legacy folderless Projects stay quarantined per §2.4 until reconciled. Equivalent retries return the same task; changed requests conflict. Accepted tasks are never silently dropped or duplicated.

### Scale and Back-pressure

- **NFR-5 — Performance and scale:** v1 supports 10,000 Projects per Tenant, 5,000 Context References per Project excluding its Folder, and 100,000 retained audit records per Project. Metadata reads target p95 under 500 ms at a data shape of 1,000 Projects and 500 references, and p95 under 1 second at the supported maximum. Durable-task admission targets p95 under 500 ms under authenticated warm steady-state with required dependencies available.
- **NFR-6 — Pagination and export bounds:** Cursor pages default to 50 and cap at 200. Safe Diagnostic Export obeys FR-24's per-export global size/row bounds and a per-Tenant limit of two concurrent exports.
- **NFR-7 — Back-pressure and dependency control:** Per Tenant, v1 supports 100 metadata reads/second with burst 200, 20 mutation admissions/second with burst 40, 1,000 nonterminal tasks, and 2 concurrent Safe Diagnostic Exports. Interactive dependency timeout defaults to 2 seconds and durable-step timeout to 10 seconds. Idempotent calls retry at most three times within 30 seconds before truthful waiting or intervention status. Overload returns structured retry guidance.

### Retention, Accessibility, Compatibility, and Release Evidence

- **NFR-8 — Retention and transient data:** Active tasks remain pollable until terminal. A terminal result and its scoped idempotency record remain available for at least 30 days or for the result's lifetime, whichever is longer. Preview/Confirmation Artifacts expire after 15 minutes; Selection Evidence expires after 5 minutes. Audit metadata is retained at least 365 days and never less than applicable retained event-history obligations. Resolution Traces and generated exports are not persisted.
- **NFR-9 — Accessibility:** Chatbot candidate, confirmation, cancellation, recovery, and task journeys, plus operator read, mutation, and export journeys, conform to WCAG 2.2 AA. They are keyboard operable, visibly focused, announced to assistive technology, do not rely on color or timing alone, and are usable at 200% zoom and a width of 320 CSS pixels. Verification combines automated evidence with authenticated manual keyboard and screen-reader evidence.
- **NFR-10 — Compatibility:** Contracts are additive and serialization-tolerant unless a breaking change is explicitly approved. Historical v1 data and unversioned name-only creation remain readable/accepted throughout v1. Retirement requires a major version, migration notice, usage evidence, compatibility tests, and rollback evidence; event history is not rewritten.
- **NFR-11 — Release evidence:** Authenticated persisted-boundary, cross-Tenant, restart/concurrency, duplicate-delivery, lost-response, accessibility, privacy, performance, deployment, smoke, rollback, and stakeholder-acceptance evidence must pass. A failed critical case or unexplained critical skip blocks release; unavailable environments remain “not verified,” never “passed.”

## 8. Success Metrics

**Outcome measurement contract**

- User-outcome metrics use rolling 30-day production windows. Release acceptance must first prove the metadata-only measurement path with deterministic authenticated fixtures; production reporting begins when the capability is enabled.
- An **eligible resumption** is an authorized Conversation-start request for an existing Conversation already associated with an Active Project and with at least one prior read-model-confirmed Project Context. Synthetic/operational traffic and a user's explicit request for a different or new Project before context retrieval are excluded. Degraded, unavailable, abandoned, and timed-out eligible resumptions remain in the denominator.
- A **continuity success** is an eligible resumption that returns `Complete` or `Partial`, reaches Chatbot first-response admission, and has no context-correction outcome before the next accepted user turn. A **context correction** is a Project switch, reattachment of a reference that was already linked at resumption start, or a Project Setup change explicitly classified as repair of missing prior context.
- A **resolution episode** begins with an authorized Conversation-start or resolve request that names no explicit Project and whose Resolution Result is not a `SingleCandidate` with `ConversationLinked` (those resumptions belong to SM-8). It ends at the first of: an accepted Candidate Project or accepted new-Project proposal, an explicit request for a different or new Project, 15 minutes elapsed, or Conversation abandonment. Repeated resolve calls in one Conversation inside that window count as one episode. **Accepted** means Read-Model-Confirmed Completion of an FR-14 confirmation or FR-15 creation. An **operator metadata repair** is an FR-22 reconciliation or an FR-7, FR-8, or FR-11 administrative association on the episode's Conversation or Project within the window.
- The data source combines Projects' metadata-only response and admission facts with Chatbot's metadata-only companion outcomes (§9 A-1). Measurement records may contain response, freshness, reason, and action codes; a Task or Resolution outcome; timestamps; an ephemeral correlation token; and a correction category. They contain no Conversation text, Project name, prompt, path, foreign payload, or secret. The architecture and test strategy define the exact event transport and aggregation.

**Primary**

- **SM-7 — Resolution usefulness:** In each rolling 30-day window, at least 90% of resolution episodes (defined in the §8 Outcome measurement contract) reach an accepted Candidate Project or accepted new-Project proposal within 15 minutes without an operator metadata repair or a context correction before the next accepted user turn. Synthetic/operational and unauthorized/invalid requests are excluded; expired, abandoned, degraded, and unavailable eligible episodes remain in the denominator. The source is the metadata-only Resolution/Chatbot outcome feed in the §8 Outcome measurement contract. Validates FR-12 through FR-15.
- **SM-8 — Continuity without reconstruction:** In each rolling 30-day window, at least 90% of eligible resumptions are continuity successes as defined above. Report numerator, denominator, excluded-count-by-safe-reason, `Partial` count, `Unavailable` count, and context-correction count; a window with no eligible resumptions is reported as insufficient volume, not 100%. Validates UJ-1, FR-2, FR-16, FR-18, FR-20, NFR-1, and NFR-3.

**Release-acceptance checks**

These are pass/fail gates under NFR-11 (SM-2 and SM-3 validate the thesis; SM-1, SM-4, SM-5, and SM-6 restate NFR bounds); they keep their IDs but are not outcome metrics.

- **SM-2 — Recoverable Project decisions:** In release acceptance, 100% of creation, archive, restore, confirmation, and cross-context tasks under restart, duplicate delivery, concurrency, and lost response reach the correct terminal state or truthful `NeedsAttention`; recovered tasks meet the 5-minute target. Validates FR-1, FR-4, FR-7, FR-8, FR-11, FR-14, FR-15, and FR-23.
- **SM-3 — Context and authority isolation:** Zero unauthorized or cross-Tenant disclosures through Project Context, tasks, Confirmation Artifacts, audit, operator access, or Safe Diagnostic Export. Validates FR-12 through FR-18 and FR-21 through FR-24.
- **SM-1 — Safe context availability:** At least 99.9% monthly availability for authenticated Project metadata/context admission, and context is usable only for read-model-confirmed Active Projects with exactly one authorized Project Folder. Validates FR-1, FR-2, FR-16, NFR-3, and NFR-4.
- **SM-4 — Interactive metadata latency:** List, open, resolution, context, and task admission meet NFR-5 at declared median and maximum data shapes. Validates FR-2, FR-5, FR-12, FR-16, and NFR-5.
- **SM-5 — Accessible completion:** All in-scope Chatbot and operator journeys pass automated checks and authenticated manual keyboard/screen-reader review with no unresolved critical or serious accessibility violation. Validates FR-14, FR-15, FR-22 through FR-24, and NFR-9.
- **SM-6 — Release evidence integrity:** Zero failed critical cases and zero unexplained critical skips are represented as passing evidence. Validates NFR-11.

**Counter-metrics**

- **SM-C1:** Do not optimize automatic attachment rate at the expense of explicit intent, correctness, or replay safety; the share of Inferred Associations bound without FR-14 confirmation is zero in every window.
- **SM-C2:** Do not optimize context or export volume at the expense of relevance, authorization, privacy, or bounded response behavior; unauthorized or cross-Tenant disclosures through any channel stay at zero (SM-3).
- **SM-C3:** Do not optimize acknowledgement latency by presenting unconfirmed mutation or notification as completion; completions reported before Read-Model-Confirmed Completion stay at zero in release acceptance.
- **SM-C4:** Context corrections must remain at or below 5% of eligible resumptions in each rolling 30-day window; unknown, abandoned, degraded, or unavailable outcomes are reported separately and never reclassified as successes to meet SM-8.

## 9. Assumptions and Open Items

Each item is a specification dependency or hole surfaced by validation. None changes product direction; each has an owner and a revisit condition. Downstream artifacts cite these IDs rather than inventing a rule.

- **A-1 `[ASSUMPTION]` Chatbot measurement companion.** SM-7, SM-8, and SM-C4 assume `Hexalith.Chatbot` emits metadata-only companion outcomes (first-response admission, context-correction category, accepted candidate or proposal) on the versioned contract in addendum §6. Owner: Chatbot owner with John. Revisit: if Chatbot cannot commit to the companion feed before Projects release, SM-7/SM-8 report "not measurable" and cannot be represented as passing.
- **A-2 `[ASSUMPTION]` Folders create-only same-name creation.** FR-1, FR-19, and FR-23 assume `Hexalith.Folders` offers a create-only Folder creation that fails on name collision within the Tenant and reports the collision as a safe code. Owner: Folders owner with Winston. Revisit: if Folders cannot guarantee create-only semantics, Projects must pre-check and fail closed on any existing same-name Folder, and UJ-2/UJ-5 confirmation must disclose that limit.
- **A-3 `[ASSUMPTION]` Folder-derived Project authorization.** §2.1 fixes the v1 permitted-Projects model as Folder-derived read authorization with no Project-level grants. It depends on `Hexalith.Folders` exposing distinct read and manage levels per Folder, a creation option that names a given actor as sole manager (or no manager) with no Tenant-default read, a current-authorization query, a reader-set coverage and delta query over two Folders with a versioned reader-set digest, and on `Hexalith.Conversations` and `Hexalith.Folders` recognizing Tenant Operator and Tenant Project Administrator authority for the move, replacement, and legacy-binding checks Projects delegates. Owner: Jerome with John; Folders and Conversations owners for the capabilities. Revisit: there is no degraded fallback; if any capability is missing, the affected FRs are not releasable under NFR-11. If an approved use case needs Project membership independent of Folder access, add explicit grant/revoke FRs under a new ID rather than widening FR-2 or FR-5.
- **A-4 `[ASSUMPTION]` Legacy folderless inventory.** §2.4, FR-8, FR-22, and NFR-4 assume the count of legacy Active folderless Projects is small enough to reconcile before the first production Tenant cutover (addendum §5). Owner: Product Owner with Amelia. Revisit: if inventory shows a volume that cannot be reconciled before cutover, quarantine remains, the FR-22 inventory read and FR-8 legacy binding stay the only path, and the migration story must add bulk-binding tooling under those FRs.
- **A-5 `[ASSUMPTION]` Descriptive-metadata inspection authorization.** §2.1, §3.2, and FR-22 introduce a separate descriptive-metadata inspection authorization for operators. Owner: Jerome with Winston. Until the identity contract pins it, the default is that Tenant-role actors receive Safe Metadata only, including in FR-5 names and FR-4/FR-23 Preview bodies. Revisit: if the platform identity contract cannot express that permission distinctly from FR-22 read and FR-24 export, that default becomes the v1 rule and Descriptive Metadata stays Project-User-only.
- **A-6 `[ASSUMPTION]` Selection Evidence issuer.** FR-25 makes Projects the issuer of Selection Evidence through a Projects-owned interactive component hosted via FrontComposer on Chatbot and Web, over its own permitted-Projects enumeration and, for Folder targets, a `Hexalith.Folders` authorization check at pick time. The components authenticate their own requests with a Projects-issued credential bound to the actor's session and unavailable to the host adapter, so the host cannot replay a pick or a confirmation; a host that drives the component's rendered UI is an accepted residual risk shared with every interactive surface. Owner: Winston with the Folders owner. Revisit: until the FR-25 component exists on a surface, every additive link and initial Folder set on that surface is an Inferred Association requiring confirmation; if Folders cannot answer the pick-time check, Folder targets stay confirmation-required.
- **A-7 `[ASSUMPTION]` Interactive-session claim.** §2.1, §4, and FR-14 assume the platform identity contract exposes, on every call, whether the credential carries the original actor's interactive session or a delegated-service/non-interactive claim, so Confirmation Artifacts and Selection Evidence can bind to the session and fail closed otherwise. Owner: Identity/Security owner with Winston. Revisit: until that claim exists, confirmation is accepted only from the Chatbot and Web adapters that the platform composition registers as interactive, and CLI/MCP confirmation stays disabled.
