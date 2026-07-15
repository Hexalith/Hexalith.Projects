---
title: "PRD: Hexalith.Projects"
status: final
created: 2026-05-24
updated: 2026-07-15
---

# PRD: Hexalith.Projects

## 0. Document Purpose

This PRD defines the v1 product requirements for `Hexalith.Projects`, the Hexalith module that gives `Hexalith.Chatbot` a durable, tenant-aware AI project workspace. It is the product baseline for UX, architecture, epics, implementation, and release acceptance. Requirements are grouped by feature with stable functional requirement IDs; cross-cutting quality obligations have stable NFR IDs. Technical mechanisms, migration detail, and proposal-specific implementation evidence live in [addendum.md](./addendum.md).

## 1. Vision

`Hexalith.Projects` enables `Hexalith.Chatbot` to sustain rich conversations by grounding each interaction in the correct Project Context. A Project connects the Conversations, Project Folder, File References, Memories, setup, and operational metadata needed for AI work to continue safely across time.

The module is not a generic project-management system. It provides a durable workspace boundary: Chatbot can determine which Project applies, retrieve authorized context, ask for confirmation when intent is ambiguous, and propose a Project when none fits. Consequential changes are durable, recoverable, and confirmed by server truth rather than inferred from acknowledgement or notification.

v1 is an internal platform and near-term implementation baseline for Hexalith.Chatbot, not a public standalone product launch.

## 2. Product Contract and Release Scope

### 2.1 Accepted Planning Decisions

- v1 is an internal Hexalith.Chatbot platform module, not a standalone or generic project-management product.
- Project name is the only required user-authored creation field; canonical requests also carry system-supplied Metadata Classification.
- Project lifecycle remains exactly `Active`/`Archived`; Durable Task statuses are separate.
- A Conversation belongs to exactly one Project in v1, trading simultaneous cross-Project context for an unambiguous workspace boundary; revisit only if an explicit multi-Project Conversation product need and safe conflict model are approved.
- No Project is caller-visible or Active before exactly one authorized Project Folder is bound and the read model confirms completion. The single Folder trades multi-root workspaces for a stable authorization anchor; revisit only if one Folder cannot represent an approved workspace use case.
- Project creation from inference and other consequential actions follow the approved Preview/Confirmation boundary.
- Role and surface choice never expand authority; service callers act only with delegated actor authority.
- FR-22 remains operator read access; FR-24 owns Safe Diagnostic Export; mutations remain in their action-specific FRs.
- Resolution is current recomputation, not persisted inference history; only confirmed outcomes enter audit. This trades retrospective inference replay for current-authorization safety and lower diagnostic retention; revisit only through a separately approved history/retention requirement.
- Projects stores metadata and references while Conversations, Folders, and Memories remain their systems of record.
- Audit, diagnostics, errors, telemetry, and exports are metadata-only and Tenant-scoped.
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
| **Core user value** | FR-1 through FR-20 and FR-23 | These requirements deliver the durable workspace, lifecycle, references, resolution, context, and setup outcomes. They may be implemented and verified in coherent value slices after implementation readiness returns `READY`, but a core-only build is internal evidence, not an authorized production release. |
| **Release-blocking safety and operations** | FR-21, FR-22, FR-24, and NFR-1 through NFR-11 | These requirements make core value supportable and safe in production: metadata-only audit and operator truth, bounded diagnostics instead of ad-hoc payload access, tenant/action isolation, recovery, performance, accessibility, compatibility, and release evidence. Production release remains blocked until this class passes alongside the core class. |
| **Deferrable or out-of-scope release cuts** | No approved v1 FR or NFR. Only the capabilities in §2.4 are deferrable. | Removing an approved FR/NFR is a product-scope change requiring explicit approval, replacement safety/operability treatment where applicable, and updated acceptance evidence. Temporary disabling of a gated surface does not count as delivering that requirement. |

The accepted v1 trade-off is deliberate: sequencing may expose a core-first implementation path, but there is no smaller safe production release. In particular, FR-22 and FR-24 remain release-blocking because authorized operator truth and bounded Safe Diagnostic Export are the supported alternatives to unbounded troubleshooting access; NFR-11 prevents incomplete evidence from being represented as release acceptance.

### 2.4 Product Boundaries and v1 Exclusions

v1 does not serve external customers consuming Projects as an independent product or users seeking the excluded capabilities below.

**Enduring product boundaries**

- Projects does not replace Conversations, Folders, or Memories as their systems of record or authorization boundaries.
- Projects does not store full transcripts, file contents, raw prompts, secrets, Memory payloads, unrestricted paths, or raw upstream problems.
- Projects does not provide generic task management; Durable Tasks are internal truth for Project operations, not user-managed work items.
- Projects does not persist candidate-score history or Resolution Traces, nor does it later reconstruct transient inference detail.
- Projects does not expose a standalone end-user UI outside Chatbot and generated/operational surfaces.
- Projects does not bypass Dapr, Hexalith.EventStore, tenant isolation, or action-level authorization.

**Out of scope for v1**

- Content indexing or semantic retrieval over file contents.
- Memory payload storage or synthesis.
- Transcript storage or summarization inside Projects.
- Historical inference/candidate-score storage or persisted diagnostic exports.
- Generic project-management workflows.
- Autonomous MCP confirmation or blanket service-identity mutations.
- Cross-Tenant Project sharing, customer-managed encryption keys, and cross-region disaster-recovery guarantees.

### 2.5 Planning Status

No phase-blocking product questions remain for UX, architecture, or epic decomposition. Repository-local dependency/version gates and implementation mechanisms are tracked in the addendum and must be verified before affected stories start.

## 3. Target Users and Journeys

### 3.1 Primary User

The primary user works in Hexalith.Chatbot across multiple Conversations and resources and expects the assistant to resume the correct Project without requiring context to be rebuilt each session.

### 3.2 Runtime Roles and Operations

| Role and surface | Purpose | Authority |
| --- | --- | --- |
| **Project User (Chatbot)** | Works with authorized Projects and confirms user-intent decisions. | Read access to their own permitted Projects and tasks; permitted Preview, archive, restore, relink, and unlink operations; confirmation of a resolution or proposed creation; no Safe Diagnostic Export. |
| **Tenant Operator (Web/CLI/MCP)** | Inspects operational metadata and performs authorized lifecycle operations. | Metadata-only read; archive/restore Preview and confirmation; no relink/unlink or resolution/proposal confirmation; Safe Diagnostic Export only with separate authorization. |
| **Tenant Project Administrator (Web/CLI/MCP)** | Has Tenant Operator capabilities plus authorized administrative association operations. | Metadata-only read; all permitted administrative Preview; archive/restore; relink/unlink; no resolution/proposal confirmation; Safe Diagnostic Export only with separate authorization. |
| **Service/Workflow Caller** | Acts for a real actor through an authorized workflow. | Delegated scope only; follows the actor's authority and never gains autonomous confirmation or blanket mutation authority. |

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

- **UJ-5. Morgan restores an archived Project.** Morgan, acting with restore authority, previews the current archived Project. Restore verifies actor authority, Project version, and exactly one authorized Project Folder. If Folder evidence is missing, Morgan selects a replacement or confirms same-name creation. The Project becomes Active only after the Durable Task and read model confirm completion.

## 4. Glossary

- **Project** — A durable AI workspace boundary managed by Hexalith.Projects. It contains metadata, Project Setup, lifecycle state, and authorized references to resources owned by other bounded contexts.
- **Project Context** — The authorized Project metadata, setup, reference metadata, and inclusion/exclusion evidence Chatbot may use for a Conversation. It contains references, not foreign payloads.
- **Context Response State** — The observable usability state of a Project list, resolution, context, or Conversation-start response: `Complete`, `Partial`, `Unavailable`, or `Denied`. It is response metadata, not Project Lifecycle State or Task Status.
- **Evidence Freshness State** — The current verification state of an authorized response component: `Current`, `Stale`, `Rebuilding`, or `Unavailable`.
- **Recovery Action Code** — A safe next action returned with a non-complete response or workflow outcome: `None`, `Retry`, `RefreshContext`, `RequestPreview`, `RenewPreview`, `PollTask`, `ResolveNeedsAttention`, `SelectAlternative`, or `ContactAdministrator`.
- **Conversation** — A conversation owned by `Hexalith.Conversations`. Projects references its stable identity and metadata. In v1, a Conversation belongs to exactly one Project.
- **Project Folder** — The single canonical Folder owned by `Hexalith.Folders` and referenced by every Active Project.
- **File Reference** — An authorized file identity and metadata owned by `Hexalith.Folders` and optionally linked to a Project.
- **Memory** — A durable resource owned by `Hexalith.Memories`; Projects stores only its authorized identity and metadata.
- **Project Setup** — Durable goals, user-facing instructions, context preferences, allowed source references, and Conversation-start defaults used to initialize or resume a Project.
- **Project Lifecycle State** — The availability state of a Project. v1 supports exactly `Active` and `Archived`.
- **Durable Task** — Recoverable server truth for consequential or cross-context work. Its state is separate from Project Lifecycle State.
- **Task Status** — One of `Pending`, `Running`, `WaitingForDependency`, `NeedsAttention`, `Succeeded`, `Rejected`, `Failed`, or `Cancelled`. The last four are terminal; `NeedsAttention` is recoverable and nonterminal.
- **Preview** — Current server-derived metadata describing a consequential action before confirmation.
- **Confirmation Artifact** — An expiring, single-use, tamper-evident authorization bound to Tenant, actor, action, targets, normalized request, Preview, and current resource version.
- **Idempotency Key** — A caller-supplied request identity scoped to Tenant, actor, and operation. Equivalent reuse returns the original Durable Task; materially different reuse conflicts.
- **Project Resolution** — Current recomputation that identifies which Project should apply when no explicit Project is selected.
- **Candidate Project** — An authorized Project returned by Project Resolution as a possible match.
- **Resolution Result** — `NoMatch`, `SingleCandidate`, or `MultipleCandidates`.
- **Resolution Reason Code** — Current metadata explaining a Candidate Project, including `ConversationLinked`, `ProjectFolderMatched`, `FileReferenceMatched`, `MemoryMatched`, and `MetadataMatched`.
- **Resolution Trace** — Request-scoped, nonpersistent diagnostic evidence for the current Project Resolution computation.
- **Read-Model-Confirmed Completion** — Completion established by the authoritative read model after durable mutation, not by request acknowledgement or notification.
- **Safe Diagnostic Export** — A bounded, non-retained, metadata-only operational export produced from an already-authorized diagnostic view.
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

- `Complete` means the authorized Project, Project Folder, Project Setup, and other required evidence are `Current`; the response is usable for its requested purpose.
- `Partial` means the Project, Project Folder, Project Setup, and authorization evidence required for first-response admission are `Current`, while one or more optional references are excluded, stale, rebuilding, or unavailable. The response may be used only with every omission represented in `components`.
- `Unavailable` means a required component is missing, stale, rebuilding, or unavailable. The response cannot initialize or resume a Conversation and returns `Retry`, `RefreshContext`, `SelectAlternative`, or `ContactAdministrator` as applicable.
- `Denied` means current actor authorization failed. It discloses no protected Project/component metadata and becomes eligible for another outcome only through a newly authorized request.

Refresh and recovery never rewrite an earlier response silently. A refresh recomputes authorization and evidence and returns a new `asOf`, `projectVersion`, state, and component set. `Partial` or `Unavailable` becomes `Complete` only after fresh recomputation proves all required evidence current. Expired or stale confirmation returns `RenewPreview` and admits no task; a lost admission response returns `PollTask` or an equivalent Idempotency Key retry that resolves to the original task. Dependency delay returns `WaitingForDependency`; human-recoverable work returns `NeedsAttention` plus `ResolveNeedsAttention`; terminal Task Status values remain immutable.

## 6. Functional Requirements

### 6.1 Project Workspace Management

**Description:** Projects provides the durable workspace record and recoverable workflows used to create, open, update, archive, restore, and list AI Projects. Project name remains the only required user-authored creation input.

#### FR-1: Create Project

Chatbot can admit Project creation as an idempotent Durable Task. A Project becomes caller-visible and `Active` only after exactly one authorized Project Folder is verified and bound. Realizes UJ-2.

**Consequences (testable):**

- The only required user-authored field is Project name; canonical requests also carry a valid system-supplied Metadata Classification.
- A supplied Project Folder is authorized and verified. When none is supplied, Projects requests same-name Folder creation from `Hexalith.Folders`.
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
- Opening follows the Context Response State, Evidence Freshness State, and Recovery Action Code semantics in section 5.
- Pre-activation creation tasks are not exposed through Project open APIs.
- Archived or unavailable Projects are identified and cannot silently become active Conversation context.

#### FR-3: Update Project Setup

Chatbot can update Project Setup used for Conversation continuity.

**Consequences (testable):**

- Updates are idempotent, durable, and observable from the authoritative read model.
- Setup may include goals, user-facing instructions, context preferences, source inclusion/exclusion policy, and Conversation-start defaults.
- Setup describes Conversation behavior and context policy, not model-provider internals.
- Updates remain additive and serialization-tolerant and reject secrets, unrestricted paths, and foreign payloads.

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
- Each result contains authorized Project identity, Project name, lifecycle state, current Project version, Project Folder availability, and the §5 response/freshness/recovery metadata needed for selection without loading full Project Context.
- Pre-activation tasks never appear as Projects.
- Cursor pages default to 50 items and cap at 200; cursors remain scoped to the authenticated query.

### 6.2 Context References

**Description:** Projects owns associations to Conversations, the Project Folder, File References, and Memories. The referenced bounded contexts remain authoritative for payloads and authorization. Cross-context work produces durable receipts and remains recoverable after retries, duplicate delivery, concurrency, or lost responses.

#### FR-6: Link Conversation

An authorized Project User can link an existing Conversation to a Project. Realizes UJ-1 and UJ-3.

**Consequences (testable):**

- A Conversation belongs to exactly one Project in v1.
- An explicitly actor-selected additive link uses an idempotent Durable Task without a second confirmation; an inferred link requires Preview and confirmation.
- Linking a Conversation already assigned elsewhere requires FR-7 rather than a second membership.
- Authorization failure prevents any protected resource access or durable effect.
- The link stores stable identity and metadata, never transcript content.

#### FR-7: Move Conversation Between Projects

An authorized Project User or Tenant Project Administrator can move a Conversation through Preview, single-use confirmation, and an idempotent Durable Task.

**Consequences (testable):**

- Preview binds both Projects, the Conversation, actor, and current resource versions.
- Completion yields exactly one Project membership and a durable cross-context receipt.
- Failure, duplicate delivery, or lost response cannot leave two memberships silently valid.
- The move is audited using metadata only and fails closed when either Project or the Conversation cannot be authorized.

#### FR-8: Set Project Folder

An authorized Project User can set the single Project Folder; a Project User or Tenant Project Administrator can replace it through Preview and confirmation. Realizes UJ-2.

**Consequences (testable):**

- Every Active Project has exactly one authorized Project Folder.
- Initial actor-selected binding is idempotent; inferred binding requires confirmation.
- Replacement binds old and new Folder evidence to the Confirmation Artifact and completes only after the authoritative read model confirms the replacement.
- Projects stores Folder identity and metadata, never file contents or unrestricted paths.
- `Hexalith.Folders` remains the authorization and system-of-record boundary.

#### FR-9: Link File Reference

An authorized Project User can link a File Reference without changing the Project Folder.

**Consequences (testable):**

- File References are optional and do not replace the Project Folder.
- Actor-selected additive linking is idempotent; inferred linking requires confirmation.
- Projects stores stable File identity and metadata only; authorization remains delegated to `Hexalith.Folders`.

#### FR-10: Link Memory

An authorized Project User can link a Memory. Realizes UJ-1 and UJ-3.

**Consequences (testable):**

- Actor-selected additive linking is idempotent; inferred linking requires confirmation.
- Projects stores stable Memory identity and metadata only.
- Authorization remains delegated to `Hexalith.Memories`.

#### FR-11: Unlink Context Reference

An authorized Project User or Tenant Project Administrator can unlink a Conversation, File Reference, or Memory through Preview, confirmation, and an idempotent Durable Task. The Project Folder can be replaced but not removed from an Active Project.

**Consequences (testable):**

- Unlinking removes only the association and never deletes the underlying resource.
- Preview identifies the affected reference and current Project version.
- Completion is durable, audited using metadata only, and confirmed by the read model.
- The operation fails closed on stale authorization or resource evidence.

### 6.3 Project Resolution

**Description:** Projects recomputes Candidate Projects from current authorized metadata. Resolution favors explicit intent over silent attachment and does not retain candidate-score history.

#### FR-12: Resolve Project From Conversation

Chatbot can request Candidate Projects for a Conversation with no explicit Project. Realizes UJ-3.

**Consequences (testable):**

- The result is `NoMatch`, `SingleCandidate`, or `MultipleCandidates` with current Resolution Reason Codes.
- Only Active, read-model-confirmed Projects are considered by default.
- Pre-activation tasks and unauthorized or stale resources cannot become candidates.
- The response follows the §5 contract; `Unavailable` and `Denied` never return a selected candidate.

#### FR-13: Resolve Project From Attachments

Chatbot can resolve Candidate Projects from an attached Project Folder or File References. Realizes UJ-2.

**Consequences (testable):**

- Matching uses current authorized Folder/File identity and metadata, not file contents.
- Applicable candidates include `ProjectFolderMatched` or `FileReferenceMatched` reason codes.
- Missing, stale, or unavailable authorization evidence fails closed.

#### FR-14: Confirm Ambiguous Project

When resolution returns multiple candidates, Chatbot presents an accessible, unselected comparison and records the Project User's choice through a Confirmation Artifact and Durable Task. Realizes UJ-3.

**Consequences (testable):**

- No candidate is silently or visually preselected.
- The artifact is bound to Tenant, actor, action, Conversation, candidates, normalized request, Preview, and current versions; it expires after 15 minutes and is single-use.
- Stale, expired, replayed, or tampered confirmation is rejected safely and requires a fresh Preview.
- Only Read-Model-Confirmed Completion creates or updates the Conversation association and audit history.
- Chatbot supports states for confirmation, cancellation, retry, expiry or staleness, lost-response recovery, and task status.

#### FR-15: Propose New Project

When no suitable Project exists, Chatbot can present a proposed Project and admit creation only after the Project User confirms a bound Preview. Realizes UJ-2.

**Consequences (testable):**

- The proposal may suggest a Project name and setup metadata but creates nothing before confirmation.
- The Confirmation Artifact binds the initiating Conversation, authorized attachments, Folder plan, normalized request, and current evidence.
- Confirmed creation follows FR-1 and exposes no Project before Folder binding and read-model confirmation.
- Non-success outcomes follow the §5 recovery contract; cancellation returns `Cancelled`, terminal failure returns `Failed`, and expired or stale evidence creates no task.

### 6.4 Project Context Assembly

**Description:** Projects supplies scoped and explainable Project Context without accidental cross-Project contamination.

#### FR-16: Get Project Context

Chatbot can request Project Context for an Active Project. Realizes UJ-1 and UJ-4.

**Consequences (testable):**

- Context is Tenant-scoped, actor-authorized, and available only for a read-model-confirmed Active Project with exactly one authorized Project Folder.
- It contains Project Setup and reference metadata, not payloads owned by other bounded contexts.
- It follows the §5 contract, representing every excluded, stale, rebuilding, or unavailable reference as a metadata-only component; `Denied` discloses no protected detail.

#### FR-17: Explain Context Selection

Authorized callers can obtain current metadata explaining why a reference was included or excluded. Realizes UJ-4.

**Consequences (testable):**

- Explanations are current Resolution Traces, not reconstructed history.
- Traces contain no secrets, payloads, prompts, unrestricted paths, raw upstream problems, or unconfirmed-candidate detail.
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
- Validation rejects secrets, unrestricted paths, unsupported references, control/invisible characters where unsafe, and foreign payloads.
- Failures identify safe field/reason codes without echoing sensitive values.

#### FR-20: Retrieve Conversation-Start Setup

Chatbot can retrieve the subset of Project Setup needed to start or resume a Conversation.

**Consequences (testable):**

- The result includes goals, user-facing instructions, context preferences, and default source policy.
- It excludes internal audit metadata and unavailable or unauthorized references.
- It is bound to one authorized `projectVersion` and `asOf` snapshot and follows §5. Chatbot may admit the first response only for `Complete` or `Partial`; `Unavailable` or `Denied` blocks first-response admission and returns the applicable Recovery Action Codes without re-querying every bounded context.

### 6.6 Audit and Operations

**Description:** Projects exposes metadata-only operational truth while keeping authority action-specific. Web, CLI, MCP, or Chatbot surface choice never expands permission.

#### FR-21: Record Project Audit Events

Projects records metadata-only audit events for consequential task admission and outcome, confirmed Project mutations, security-relevant confirmation outcomes, reconciliation, and Safe Diagnostic Export.

**Consequences (testable):**

- Audit covers task admission and terminal outcome; confirmation use and cancellation; rejection of stale, replayed, or tampered confirmations; authorization denial; creation, archive, restore, move, relink, Folder replacement, unlink, confirmed resolution, and confirmed proposed creation; manual reconciliation; and Safe Diagnostic Export creation. Audit also records stable upstream receipt identifiers.
- Equivalent idempotent retries do not create duplicate audit events.
- Intermediate task states, polls, retries, dependency latency, notifications, unused expiry, and read-only Resolution Traces remain operational telemetry rather than durable audit.
- Audit contains Tenant, actor, Project/action identity, timestamp, safe reason/outcome codes, and affected reference identifiers, never payloads or secrets.

#### FR-22: Support Operator Read Access

Tenant Operators and Tenant Project Administrators can inspect authorized Project metadata, lifecycle state, references, Durable Task status, confirmed resolution outcomes, and audit metadata.

**Consequences (testable):**

- Access is Tenant-scoped, action-authorized, and metadata-only across Web, CLI, and MCP.
- Project Users may inspect only their own permitted task status through Chatbot.
- Pre-activation tasks remain separate from Project list/open APIs; Tenant Operators and Tenant Project Administrators may inspect their safe status, and Tenant Project Administrators may perform authorized reconciliation.
- Read permission alone grants neither Safe Diagnostic Export nor a mutation.

#### FR-23: Restore Archived Project

An authorized Project User, Tenant Operator, or Tenant Project Administrator can restore an Archived Project through Preview, confirmation, and an idempotent Durable Task. This is the restore counterpart to FR-4 and realizes UJ-5.

**Consequences (testable):**

- Preview verifies Tenant, actor, authority, current Project version, and exactly one authorized Project Folder.
- If the prior Folder is invalid or missing, Preview requires an authorized replacement or same-name Folder creation before confirmation.
- The Project remains Archived until Folder evidence and read-model-confirmed restore completion succeed.
- If Folder creation succeeds but activation cannot commit, the task enters `NeedsAttention`; Projects never automatically deletes a Folders-owned resource.
- Stale/unavailable evidence, replay, cancellation, duplicate delivery, concurrency, and lost response cannot expose an invalid Active Project.
- Completion and reconciliation outcomes are audited using metadata only.

#### FR-24: Create Safe Diagnostic Export

A separately authorized Tenant Operator or Tenant Project Administrator can create a bounded Safe Diagnostic Export through Web, CLI, or MCP.

**Consequences (testable):**

- Export permission is distinct from FR-22 read permission; Chatbot cannot create exports.
- Every attempt and outcome is audited using metadata only.
- The complete encoded export, including envelope and truncation metadata, is at most 1 MiB and contains at most 500 reference rows and 100 audit rows.
- Reference ordering is stable and deterministic; audit rows are newest-first with stable tie-breaking.
- Truncation reports included/omitted counts and safe reasons without excluded detail; exports have no continuation cursor.
- Upstream unavailability is represented safely without raw errors or fabricated completeness.
- Projects never retains generated exports.

## 7. Cross-Cutting Non-Functional Requirements

### Security, Privacy, Reliability, and Recovery

- **NFR-1 — Security and privacy:** Every read, write, task, confirmation, audit event, and export is Tenant-, actor-, action-, target-, and current-version-scoped. Trust-bearing mutations fail closed when authorization evidence is stale, unknown, rebuilding, or unavailable. Logs, telemetry, errors, and evidence remain metadata-only.
- **NFR-2 — Encryption and key management:** Production traffic uses platform-approved authenticated encryption in transit. Durable Project, task, idempotency, and audit data uses platform-managed encryption at rest. Projects owns no private keys; approved platform KMS/secret-provider rotation and revocation evidence is release-blocking.
- **NFR-3 — Availability and recovery:** Authenticated metadata APIs and task admission target 99.9% monthly availability excluding planned maintenance. With required dependencies healthy, service RTO after process/node failure is 15 minutes, and accepted tasks resume or reach truthful `NeedsAttention` within 5 minutes.
- **NFR-4 — Durability and idempotency:** A Project event acknowledged as committed has RPO 0 within the configured primary-region durability domain. Active Projects are never folderless. Equivalent retries return the same task; changed requests conflict. Accepted tasks are never silently dropped or duplicated.

### Scale and Back-pressure

- **NFR-5 — Performance and scale:** v1 supports 10,000 Projects per Tenant, 5,000 Context References per Project excluding its Folder, and 100,000 retained audit records per Project. Metadata reads target p95 under 500 ms at a data shape of 1,000 Projects and 500 references, and p95 under 1 second at the supported maximum. Durable-task admission targets p95 under 500 ms under authenticated warm steady-state with required dependencies available.
- **NFR-6 — Pagination and export bounds:** Cursor pages default to 50 and cap at 200. Safe Diagnostic Export obeys FR-24's per-export global size/row bounds and a per-Tenant limit of two concurrent exports.
- **NFR-7 — Back-pressure and dependency control:** Per Tenant, v1 supports 100 metadata reads/second with burst 200, 20 mutation admissions/second with burst 40, 1,000 nonterminal tasks, and 2 concurrent Safe Diagnostic Exports. Interactive dependency timeout defaults to 2 seconds and durable-step timeout to 10 seconds. Idempotent calls retry at most three times within 30 seconds before truthful waiting or intervention status. Overload returns structured retry guidance.

### Retention, Accessibility, Compatibility, and Release Evidence

- **NFR-8 — Retention and transient data:** Active tasks remain pollable until terminal. A terminal result and its scoped idempotency record remain available for at least 30 days or for the result's lifetime, whichever is longer. Preview/Confirmation Artifacts expire after 15 minutes. Audit metadata is retained at least 365 days and never less than applicable retained event-history obligations. Resolution Traces and generated exports are not persisted.
- **NFR-9 — Accessibility:** Chatbot candidate, confirmation, cancellation, recovery, and task journeys, plus operator read, mutation, and export journeys, conform to WCAG 2.2 AA. They are keyboard operable, visibly focused, announced to assistive technology, do not rely on color or timing alone, and are usable at 200% zoom and a width of 320 CSS pixels. Verification combines automated evidence with authenticated manual keyboard and screen-reader evidence.
- **NFR-10 — Compatibility:** Contracts are additive and serialization-tolerant unless a breaking change is explicitly approved. Historical v1 data and unversioned name-only creation remain readable/accepted throughout v1. Retirement requires a major version, migration notice, usage evidence, compatibility tests, and rollback evidence; event history is not rewritten.
- **NFR-11 — Release evidence:** Authenticated persisted-boundary, cross-Tenant, restart/concurrency, duplicate-delivery, lost-response, accessibility, privacy, performance, deployment, smoke, rollback, and stakeholder-acceptance evidence must pass. A failed critical case or unexplained critical skip blocks release; unavailable environments remain “not verified,” never “passed.”

## 8. Success Metrics

**Outcome measurement contract**

- User-outcome metrics use rolling 30-day production windows. Release acceptance must first prove the metadata-only measurement path with deterministic authenticated fixtures; production reporting begins when the capability is enabled.
- An **eligible resumption** is an authorized Conversation-start request for an existing Conversation already associated with an Active Project and with at least one prior read-model-confirmed Project Context. Synthetic/operational traffic and a user's explicit request for a different or new Project before context retrieval are excluded. Degraded, unavailable, abandoned, and timed-out eligible resumptions remain in the denominator.
- A **continuity success** is an eligible resumption that returns `Complete` or `Partial`, reaches Chatbot first-response admission, and has no context-correction outcome before the next accepted user turn. A **context correction** is a Project switch, reattachment of a reference that was already linked at resumption start, or a Project Setup change explicitly classified as repair of missing prior context.
- The data source combines Projects' metadata-only response and admission facts with Chatbot's metadata-only companion outcomes. Measurement records may contain response, freshness, reason, and action codes; a Task or Resolution outcome; timestamps; an ephemeral correlation token; and a correction category. They contain no Conversation text, Project name, prompt, path, foreign payload, or secret. The architecture and test strategy define the exact event transport and aggregation.

**Primary**

- **SM-1 — Safe context availability:** At least 99.9% monthly availability for authenticated Project metadata/context admission, and context is usable only for read-model-confirmed Active Projects with exactly one authorized Project Folder. Validates FR-1, FR-2, FR-16, NFR-3, and NFR-4.
- **SM-2 — Recoverable Project decisions:** In release acceptance, 100% of creation, archive, restore, confirmation, and cross-context tasks under restart, duplicate delivery, concurrency, and lost response reach the correct terminal state or truthful `NeedsAttention`; recovered tasks meet the 5-minute target. Validates FR-1, FR-4, FR-7, FR-8, FR-11, FR-14, FR-15, and FR-23.
- **SM-3 — Context and authority isolation:** Zero unauthorized or cross-Tenant disclosures through Project Context, tasks, Confirmation Artifacts, audit, operator access, or Safe Diagnostic Export. Validates FR-12 through FR-18 and FR-21 through FR-24.
- **SM-4 — Interactive metadata latency:** List, open, resolution, context, and task admission meet NFR-5 at declared median and maximum data shapes. Validates FR-2, FR-5, FR-12, FR-16, and NFR-5.
- **SM-5 — Accessible completion:** All in-scope Chatbot and operator journeys pass automated checks and authenticated manual keyboard/screen-reader review with no unresolved critical or serious accessibility violation. Validates FR-14, FR-15, FR-22 through FR-24, and NFR-9.
- **SM-6 — Release evidence integrity:** Zero failed critical cases and zero unexplained critical skips are represented as passing evidence. Validates NFR-11.
- **SM-7 — Resolution usefulness:** In each rolling 30-day window, at least 90% of authorized Project Resolution episodes with no explicit Project reach an accepted Candidate Project or accepted new-Project proposal within 15 minutes without an operator metadata repair or a Project correction before the next accepted user turn. Synthetic/operational and unauthorized/invalid requests are excluded; expired, abandoned, degraded, and unavailable eligible episodes remain in the denominator. The source is the metadata-only Resolution/Chatbot outcome feed defined above. Validates FR-12 through FR-15.
- **SM-8 — Continuity without reconstruction:** In each rolling 30-day window, at least 90% of eligible resumptions are continuity successes as defined above. Report numerator, denominator, excluded-count-by-safe-reason, `Partial` count, `Unavailable` count, and context-correction count; a window with no eligible resumptions is reported as insufficient volume, not 100%. Validates UJ-1, FR-2, FR-16, FR-18, FR-20, NFR-1, and NFR-3.

**Counter-metrics**

- **SM-C1:** Do not optimize automatic attachment rate at the expense of explicit intent, correctness, or replay safety.
- **SM-C2:** Do not optimize context or export volume at the expense of relevance, authorization, privacy, or bounded response behavior.
- **SM-C3:** Do not optimize acknowledgement latency by presenting unconfirmed mutation or notification as completion.
- **SM-C4:** Context corrections must remain at or below 5% of eligible resumptions in each rolling 30-day window; unknown, abandoned, degraded, or unavailable outcomes are reported separately and never reclassified as successes to meet SM-8.
