---
stepsCompleted:
  - 1
  - 2
  - 3
  - 4
  - 5
  - 6
inputDocuments:
  prd:
    - _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md
    - _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/addendum.md
  architecture:
    - _bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md
  epics:
    - _bmad-output/planning-artifacts/epics.md
  architectureConformance:
    - _bmad-output/planning-artifacts/epics-architecture-conformance-checklist-2026-07-16.md
  ux:
    - _bmad-output/planning-artifacts/ux-design-specification.md
excludedDocuments:
  - _bmad-output/planning-artifacts/archive/architecture-2026-05-24-superseded.md
---

# Implementation Readiness Assessment Report

**Date:** 2026-08-02
**Project:** projects

## Document Discovery

### Documents selected for assessment

- **PRD:** `prds/prd-Hexalith.Projects-2026-05-24/prd.md` with `addendum.md`
- **Architecture:** `architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md`
- **Epics and stories:** `epics.md`
- **Architecture-conformance evidence:** `epics-architecture-conformance-checklist-2026-07-16.md`
- **UX:** `ux-design-specification.md`

### Discovery resolution

- The obsolete May architecture was compared with the current Architecture Spine. Details absent
  from the spine were either superseded by its adopted decisions or intentionally delegated to
  canonical contracts, ADRs, checklists, and evidence gates.
- The obsolete document was archived as
  `archive/architecture-2026-05-24-superseded.md` and excluded from this assessment.
- The PRD bundle has no `index.md`; the confirmed whole-document assessment set is `prd.md` plus
  `addendum.md`.
- No required document type is missing from the confirmed assessment set.

## PRD Analysis

### Functional Requirements

#### FR-1: Create Project

Chatbot can admit Project creation as an idempotent Durable Task. A Project becomes caller-visible
and `Active` only after exactly one authorized Project Folder is verified and bound. Realizes UJ-2.

- The only required user-authored field is Project name; canonical requests also carry a valid
  system-supplied Metadata Classification.
- A supplied Project Folder is authorized and verified. When none is supplied, Projects requests
  same-name Folder creation from `Hexalith.Folders`.
- Admission returns a pollable Durable Task rather than an immediately Active Project.
- Dependency denial, timeout, cancellation, duplicate delivery, lost response, or reconciliation
  never exposes an Active folderless Project.
- Equivalent Idempotency Key retries return the original task. A materially different request using
  the same scoped key returns an idempotency conflict.
- Terminal success exposes the Project identity only after Read-Model-Confirmed Completion.
- Historical unversioned name-only creation requests remain supported throughout v1; retirement
  requires an explicitly approved major version.
- Creation never duplicates transcripts, file contents, prompts, secrets, or Memory payloads.

#### FR-2: Open Project

Chatbot can open an authorized Project and receive the metadata, lifecycle state, Project Setup, and
references needed to initialize a Conversation. Realizes UJ-1.

- Opening returns only data visible to the requesting Tenant and actor.
- Opening follows the Context Response State, Evidence Freshness State, and Recovery Action Code
  semantics in PRD section 5.
- Pre-activation creation tasks are not exposed through Project open APIs.
- Archived or unavailable Projects are identified and cannot silently become active Conversation
  context.

#### FR-3: Update Project Setup

Chatbot can update Project Setup used for Conversation continuity.

- Updates are idempotent, durable, and observable from the authoritative read model.
- Setup may include goals, user-facing instructions, context preferences, source
  inclusion/exclusion policy, and Conversation-start defaults.
- Setup describes Conversation behavior and context policy, not model-provider internals.
- Updates remain additive and serialization-tolerant and reject secrets, unrestricted paths, and
  foreign payloads.

#### FR-4: Archive Project

An authorized Project User, Tenant Operator, or Tenant Project Administrator can archive an Active
Project through server Preview, single-use confirmation, and an idempotent Durable Task. FR-23
defines the corresponding restore operation.

- Project Lifecycle State remains limited to `Active` and `Archived`.
- Confirmation is invalidated when actor authority or Project version changes.
- Archived Projects are excluded from Project Resolution unless explicitly requested.
- Completion is not reported until the read model confirms `Archived`.
- Existing references remain auditable after archival.

#### FR-5: List Projects

Authorized callers can list visible Active and Archived Projects.

- Results are Tenant-scoped, authorization-filtered, and filterable by Project Lifecycle State.
- Each result contains authorized Project identity, Project name, lifecycle state, current Project
  version, Project Folder availability, and the PRD section 5 response/freshness/recovery metadata
  needed for selection without loading full Project Context.
- Pre-activation tasks never appear as Projects.
- Cursor pages default to 50 items and cap at 200; cursors remain scoped to the authenticated query.

#### FR-6: Link Conversation

An authorized Project User can link an existing Conversation to a Project. Realizes UJ-1 and UJ-3.

- A Conversation belongs to exactly one Project in v1.
- An explicitly actor-selected additive link uses an idempotent Durable Task without a second
  confirmation; an inferred link requires Preview and confirmation.
- Linking a Conversation already assigned elsewhere requires FR-7 rather than a second membership.
- Authorization failure prevents any protected resource access or durable effect.
- The link stores stable identity and metadata, never transcript content.

#### FR-7: Move Conversation Between Projects

An authorized Project User or Tenant Project Administrator can move a Conversation through Preview,
single-use confirmation, and an idempotent Durable Task.

- Preview binds both Projects, the Conversation, actor, and current resource versions.
- Completion yields exactly one Project membership and a durable cross-context receipt.
- Failure, duplicate delivery, or lost response cannot leave two memberships silently valid.
- The move is audited using metadata only and fails closed when either Project or the Conversation
  cannot be authorized.

#### FR-8: Set Project Folder

An authorized Project User can set the single Project Folder; a Project User or Tenant Project
Administrator can replace it through Preview and confirmation. Realizes UJ-2.

- Every Active Project has exactly one authorized Project Folder.
- Initial actor-selected binding is idempotent; inferred binding requires confirmation.
- Replacement binds old and new Folder evidence to the Confirmation Artifact and completes only
  after the authoritative read model confirms the replacement.
- Projects stores Folder identity and metadata, never file contents or unrestricted paths.
- `Hexalith.Folders` remains the authorization and system-of-record boundary.

#### FR-9: Link File Reference

An authorized Project User can link a File Reference without changing the Project Folder.

- File References are optional and do not replace the Project Folder.
- Actor-selected additive linking is idempotent; inferred linking requires confirmation.
- Projects stores stable File identity and metadata only; authorization remains delegated to
  `Hexalith.Folders`.

#### FR-10: Link Memory

An authorized Project User can link a Memory. Realizes UJ-1 and UJ-3.

- Actor-selected additive linking is idempotent; inferred linking requires confirmation.
- Projects stores stable Memory identity and metadata only.
- Authorization remains delegated to `Hexalith.Memories`.

#### FR-11: Unlink Context Reference

An authorized Project User or Tenant Project Administrator can unlink a Conversation, File
Reference, or Memory through Preview, confirmation, and an idempotent Durable Task. The Project
Folder can be replaced but not removed from an Active Project.

- Unlinking removes only the association and never deletes the underlying resource.
- Preview identifies the affected reference and current Project version.
- Completion is durable, audited using metadata only, and confirmed by the read model.
- The operation fails closed on stale authorization or resource evidence.

#### FR-12: Resolve Project From Conversation

Chatbot can request Candidate Projects for a Conversation with no explicit Project. Realizes UJ-3.

- The result is `NoMatch`, `SingleCandidate`, or `MultipleCandidates` with current Resolution Reason
  Codes.
- Only Active, read-model-confirmed Projects are considered by default.
- Pre-activation tasks and unauthorized or stale resources cannot become candidates.
- The response follows the PRD section 5 contract; `Unavailable` and `Denied` never return a selected
  candidate.

#### FR-13: Resolve Project From Attachments

Chatbot can resolve Candidate Projects from an attached Project Folder or File References. Realizes
UJ-2.

- Matching uses current authorized Folder/File identity and metadata, not file contents.
- Applicable candidates include `ProjectFolderMatched` or `FileReferenceMatched` reason codes.
- Missing, stale, or unavailable authorization evidence fails closed.

#### FR-14: Confirm Ambiguous Project

When resolution returns multiple candidates, Chatbot presents an accessible, unselected comparison
and records the Project User's choice through a Confirmation Artifact and Durable Task. Realizes UJ-3.

- No candidate is silently or visually preselected.
- The artifact is bound to Tenant, actor, action, Conversation, candidates, normalized request,
  Preview, and current versions; it expires after 15 minutes and is single-use.
- Stale, expired, replayed, or tampered confirmation is rejected safely and requires a fresh Preview.
- Only Read-Model-Confirmed Completion creates or updates the Conversation association and audit
  history.
- Chatbot supports states for confirmation, cancellation, retry, expiry or staleness, lost-response
  recovery, and task status.

#### FR-15: Propose New Project

When no suitable Project exists, Chatbot can present a proposed Project and admit creation only after
the Project User confirms a bound Preview. Realizes UJ-2.

- The proposal may suggest a Project name and setup metadata but creates nothing before confirmation.
- The Confirmation Artifact binds the initiating Conversation, authorized attachments, Folder plan,
  normalized request, and current evidence.
- Confirmed creation follows FR-1 and exposes no Project before Folder binding and read-model
  confirmation.
- Non-success outcomes follow the PRD section 5 recovery contract; cancellation returns `Cancelled`,
  terminal failure returns `Failed`, and expired or stale evidence creates no task.

#### FR-16: Get Project Context

Chatbot can request Project Context for an Active Project. Realizes UJ-1 and UJ-4.

- Context is Tenant-scoped, actor-authorized, and available only for a read-model-confirmed Active
  Project with exactly one authorized Project Folder.
- It contains Project Setup and reference metadata, not payloads owned by other bounded contexts.
- It follows the PRD section 5 contract, representing every excluded, stale, rebuilding, or
  unavailable reference as a metadata-only component; `Denied` discloses no protected detail.

#### FR-17: Explain Context Selection

Authorized callers can obtain current metadata explaining why a reference was included or excluded.
Realizes UJ-4.

- Explanations are current Resolution Traces, not reconstructed history.
- Traces contain no secrets, payloads, prompts, unrestricted paths, raw upstream problems, or
  unconfirmed-candidate detail.
- Traces are request-scoped and not persisted; only confirmed outcomes enter audit history.

#### FR-18: Refresh Project Context

Chatbot can request a read-only refresh after links, setup, authorization, or resource availability
changes.

- Refresh recomputes from current authorized Project, Conversation, Folder, File Reference, Memory,
  and version metadata.
- Refresh itself never mutates Project or reference state and creates no maintenance audit event.
- The refreshed response follows PRD section 5, including new snapshot metadata, component evidence,
  recovery actions, and the binding transition rules for `Partial`, `Unavailable`, and `Complete`.

#### FR-19: Validate Project Setup

Projects validates setup and creation admission before accepting durable work.

- Project name remains the only required user-authored creation field.
- Canonical creation requests require valid system-supplied Metadata Classification; invalid
  classification is rejected before command submission.
- Validation permits a supplied authorized Project Folder or same-name Folder creation, but never
  defaults a caller-visible Project to Active before Folder completion.
- Validation rejects secrets, unrestricted paths, unsupported references, control/invisible
  characters where unsafe, and foreign payloads.
- Failures identify safe field/reason codes without echoing sensitive values.

#### FR-20: Retrieve Conversation-Start Setup

Chatbot can retrieve the subset of Project Setup needed to start or resume a Conversation.

- The result includes goals, user-facing instructions, context preferences, and default source
  policy.
- It excludes internal audit metadata and unavailable or unauthorized references.
- It is bound to one authorized `projectVersion` and `asOf` snapshot and follows PRD section 5.
  Chatbot may admit the first response only for `Complete` or `Partial`; `Unavailable` or `Denied`
  blocks first-response admission and returns the applicable Recovery Action Codes without
  re-querying every bounded context.

#### FR-21: Record Project Audit Events

Projects records metadata-only audit events for consequential task admission and outcome, confirmed
Project mutations, security-relevant confirmation outcomes, reconciliation, and Safe Diagnostic
Export.

- Audit covers task admission and terminal outcome; confirmation use and cancellation; rejection of
  stale, replayed, or tampered confirmations; authorization denial; creation, archive, restore, move,
  relink, Folder replacement, unlink, confirmed resolution, and confirmed proposed creation; manual
  reconciliation; and Safe Diagnostic Export creation. Audit also records stable upstream receipt
  identifiers.
- Equivalent idempotent retries do not create duplicate audit events.
- Intermediate task states, polls, retries, dependency latency, notifications, unused expiry, and
  read-only Resolution Traces remain operational telemetry rather than durable audit.
- Audit contains Tenant, actor, Project/action identity, timestamp, safe reason/outcome codes, and
  affected reference identifiers, never payloads or secrets.

#### FR-22: Support Operator Read Access

Tenant Operators and Tenant Project Administrators can inspect authorized Project metadata,
lifecycle state, references, Durable Task status, confirmed resolution outcomes, and audit metadata.

- Access is Tenant-scoped, action-authorized, and metadata-only across Web, CLI, and MCP.
- Project Users may inspect only their own permitted task status through Chatbot.
- Pre-activation tasks remain separate from Project list/open APIs; Tenant Operators and Tenant
  Project Administrators may inspect their safe status, and Tenant Project Administrators may
  perform authorized reconciliation.
- Read permission alone grants neither Safe Diagnostic Export nor a mutation.

#### FR-23: Restore Archived Project

An authorized Project User, Tenant Operator, or Tenant Project Administrator can restore an Archived
Project through Preview, confirmation, and an idempotent Durable Task. This is the restore
counterpart to FR-4 and realizes UJ-5.

- Preview verifies Tenant, actor, authority, current Project version, and exactly one authorized
  Project Folder.
- If the prior Folder is invalid or missing, Preview requires an authorized replacement or same-name
  Folder creation before confirmation.
- The Project remains Archived until Folder evidence and read-model-confirmed restore completion
  succeed.
- If Folder creation succeeds but activation cannot commit, the task enters `NeedsAttention`;
  Projects never automatically deletes a Folders-owned resource.
- Stale/unavailable evidence, replay, cancellation, duplicate delivery, concurrency, and lost
  response cannot expose an invalid Active Project.
- Completion and reconciliation outcomes are audited using metadata only.

#### FR-24: Create Safe Diagnostic Export

A separately authorized Tenant Operator or Tenant Project Administrator can create a bounded Safe
Diagnostic Export through Web, CLI, or MCP.

- Export permission is distinct from FR-22 read permission; Chatbot cannot create exports.
- Every attempt and outcome is audited using metadata only.
- The complete encoded export, including envelope and truncation metadata, is at most 1 MiB and
  contains at most 500 reference rows and 100 audit rows.
- Reference ordering is stable and deterministic; audit rows are newest-first with stable
  tie-breaking.
- Truncation reports included/omitted counts and safe reasons without excluded detail; exports have
  no continuation cursor.
- Upstream unavailability is represented safely without raw errors or fabricated completeness.
- Projects never retains generated exports.

**Total Functional Requirements: 24**

### Non-Functional Requirements

#### NFR-1: Security and privacy

Every read, write, task, confirmation, audit event, and export is Tenant-, actor-, action-, target-,
and current-version-scoped. Trust-bearing mutations fail closed when authorization evidence is stale,
unknown, rebuilding, or unavailable. Logs, telemetry, errors, and evidence remain metadata-only.

#### NFR-2: Encryption and key management

Production traffic uses platform-approved authenticated encryption in transit. Durable Project,
task, idempotency, and audit data uses platform-managed encryption at rest. Projects owns no private
keys; approved platform KMS/secret-provider rotation and revocation evidence is release-blocking.

#### NFR-3: Availability and recovery

Authenticated metadata APIs and task admission target 99.9% monthly availability excluding planned
maintenance. With required dependencies healthy, service RTO after process/node failure is 15
minutes, and accepted tasks resume or reach truthful `NeedsAttention` within 5 minutes.

#### NFR-4: Durability and idempotency

A Project event acknowledged as committed has RPO 0 within the configured primary-region durability
domain. Active Projects are never folderless. Equivalent retries return the same task; changed
requests conflict. Accepted tasks are never silently dropped or duplicated.

#### NFR-5: Performance and scale

v1 supports 10,000 Projects per Tenant, 5,000 Context References per Project excluding its Folder,
and 100,000 retained audit records per Project. Metadata reads target p95 under 500 ms at a data shape
of 1,000 Projects and 500 references, and p95 under 1 second at the supported maximum. Durable-task
admission targets p95 under 500 ms under authenticated warm steady-state with required dependencies
available.

#### NFR-6: Pagination and export bounds

Cursor pages default to 50 and cap at 200. Safe Diagnostic Export obeys FR-24's per-export global
size/row bounds and a per-Tenant limit of two concurrent exports.

#### NFR-7: Back-pressure and dependency control

Per Tenant, v1 supports 100 metadata reads/second with burst 200, 20 mutation admissions/second with
burst 40, 1,000 nonterminal tasks, and 2 concurrent Safe Diagnostic Exports. Interactive dependency
timeout defaults to 2 seconds and durable-step timeout to 10 seconds. Idempotent calls retry at most
three times within 30 seconds before truthful waiting or intervention status. Overload returns
structured retry guidance.

#### NFR-8: Retention and transient data

Active tasks remain pollable until terminal. A terminal result and its scoped idempotency record
remain available for at least 30 days or for the result's lifetime, whichever is longer.
Preview/Confirmation Artifacts expire after 15 minutes. Audit metadata is retained at least 365 days
and never less than applicable retained event-history obligations. Resolution Traces and generated
exports are not persisted.

#### NFR-9: Accessibility

Chatbot candidate, confirmation, cancellation, recovery, and task journeys, plus operator read,
mutation, and export journeys, conform to WCAG 2.2 AA. They are keyboard operable, visibly focused,
announced to assistive technology, do not rely on color or timing alone, and are usable at 200% zoom
and a width of 320 CSS pixels. Verification combines automated evidence with authenticated manual
keyboard and screen-reader evidence.

#### NFR-10: Compatibility

Contracts are additive and serialization-tolerant unless a breaking change is explicitly approved.
Historical v1 data and unversioned name-only creation remain readable/accepted throughout v1.
Retirement requires a major version, migration notice, usage evidence, compatibility tests, and
rollback evidence; event history is not rewritten.

#### NFR-11: Release evidence

Authenticated persisted-boundary, cross-Tenant, restart/concurrency, duplicate-delivery,
lost-response, accessibility, privacy, performance, deployment, smoke, rollback, and
stakeholder-acceptance evidence must pass. A failed critical case or unexplained critical skip blocks
release; unavailable environments remain “not verified,” never “passed.”

**Total Non-Functional Requirements: 11**

### Additional Requirements

#### Product and authority constraints

- v1 is an internal Hexalith.Chatbot platform module, not a standalone project-management product.
- Project lifecycle is exactly `Active` or `Archived`; task states never become lifecycle values.
- Every caller-visible Active Project has exactly one authorized Folder and read-model-confirmed
  completion.
- One Conversation belongs to exactly one Project in v1.
- Projects stores metadata and stable references only; Conversations, Folders, and Memories retain
  resource and authorization authority.
- Resolution is current recomputation. Candidate-score history, Resolution Traces, and generated
  exports are not persisted.
- Role and adapter choice never expands authority. Service and workflow callers act only through the
  delegated original actor's scope.
- FR-1 through FR-20 and FR-23 are core value; FR-21, FR-22, FR-24, and every NFR are
  release-blocking. No approved v1 FR or NFR is deferrable from production release.

#### Observable response and recovery contract

- Project open, list, resolution, context, Conversation-start, and proposal-recovery responses share
  `responseState`, `asOf`, authorized `projectVersion`, conditional `resolutionResult`, metadata-only
  `components`, and `recoveryActions`.
- Response states are exactly `Complete`, `Partial`, `Unavailable`, and `Denied`; evidence freshness
  is exactly `Current`, `Stale`, `Rebuilding`, and `Unavailable`.
- Recovery Action Codes are exactly `None`, `Retry`, `RefreshContext`, `RequestPreview`,
  `RenewPreview`, `PollTask`, `ResolveNeedsAttention`, `SelectAlternative`, and
  `ContactAdministrator`.
- `Partial` is usable only when Project, Folder, Setup, and first-response authorization evidence are
  current and every optional omission is represented. `Unavailable` and `Denied` block Conversation
  initialization or resumption.
- Refresh produces a new evidence snapshot and never silently rewrites an earlier result.

#### Durable workflow, confirmation, and idempotency constraints

- Durable workflow design must define checkpoints, worker ownership, leases, restart recovery,
  two-instance convergence, duplicate delivery, lost responses, cancellation cut-off, immutable
  terminal states, receipts, compensation, and reconciliation.
- Cross-context work must preserve owner authority and recover Folder provisioning, Conversation
  membership, File/Memory links, archive, and restore without automatic deletion of owner resources.
- Confirmation Artifacts bind Tenant, actor, action, targets, normalized request, Preview, and current
  resource versions; they expire after 15 minutes, are tamper-evident and single-use, and admit no
  task when stale, expired, replayed, tampered, or unauthorized.
- Idempotency is scoped to `(Tenant, actor, operation, key)`. Equivalent reuse returns the original
  task; changed-request reuse conflicts; the result and idempotency record remain together for at
  least 30 days or the longer result lifetime.
- Canonicalization must preserve direct-server/generated-helper parity, including U+2028/U+2029
  rules, collision resistance against LF and literal backslash-`u`, and bounded compatibility for
  any persisted legacy fingerprints.

#### Contract, package, and migration constraints

- Canonical Create Project metadata classification is exactly `public_metadata`,
  `tenant_sensitive`, `credential_sensitive`, or `secret`; it is system-supplied and never inferred
  from user text. Authorization precedes protected parsing, invalid values produce metadata-only
  `400 ValidationFailure`, and direct creation and proposal confirmation share one validator.
- The historical unversioned name-only creation shape remains accepted throughout v1 and can retire
  only through an approved major-version migration with usage, compatibility, and rollback evidence.
- `Hexalith.Projects.UI.Contracts` is a non-packable descriptor host depending inward on the UI-free
  Contracts kernel. It cannot pull FrontComposer Shell, Fluxor, Fluent UI, or ASP.NET dependencies
  into the kernel or change MCP/CLI authority.
- `Hexalith.Builds` owns the approved NSwag and Fluxor versions; Projects uses versionless package
  references and preserves central transitive pinning.
- Migration must inventory legacy folderless Active Projects and in-flight Folder work, preserve
  immutable event history, use additive event evolution, prove replay/value-slice equivalence, avoid
  unsafe dual writes, and retain a rollback path.
- Any EventStore, FrontComposer, Conversations, Folders, Memories, Chatbot, identity, or Builds
  change needs separate repository-local approval and evidence; the Projects PRD grants no implicit
  sibling-repository mutation authority.

#### Presentation and verification constraints

- Chatbot owns end-user presentation; Projects owns versioned Preview, Confirmation Artifact,
  Durable Task, Resolution, and Context contracts; platform/FrontComposer owns operational Web, CLI,
  and MCP composition.
- Candidate comparison has no preselection and must support explicit confirm/cancel, expiry/staleness
  recovery, lost-response recovery, accessible focus/status behavior, 200% zoom, and 320 CSS-pixel
  reflow.
- Safe Diagnostic Export is `projects.safe-diagnostic-export.v1`, separately authorized,
  metadata-only, non-retained, deterministic, at most 1 MiB, at most 500 reference rows and 100 audit
  rows, without continuation cursors, and limited to two concurrent exports per Tenant.
- Release evidence requires deterministic small/median/maximum fixtures; authenticated
  persisted-boundary and cross-Tenant proof; restart/two-instance/duplicate/concurrency/cancellation
  and lost-response proof; Web/CLI/MCP parity; accessibility automation plus manual authenticated
  keyboard/screen-reader review; performance, deployment, smoke, rollback, compatibility, and
  stakeholder acceptance.
- Failed critical cases and unexplained critical skips block release. Missing environments remain
  `not verified` rather than `passed`.
- The addendum's evidence index records the supersession chain through E-14. It treats Epics 1–5 as
  immutable implementation history, Epics 6–8 as corrective production authority, the 2026-07-17
  E-13 report as the `READY` planning rerun, and Story 8.11 as the remaining terminal release gate.

### PRD Completeness Assessment

The product baseline is complete enough for traceability validation. It contains 24 continuous,
uniquely numbered Functional Requirements and 11 continuous, uniquely numbered Non-Functional
Requirements. Each FR has explicit, testable consequences; every NFR has measurable or
evidence-oriented acceptance language. Roles, authority boundaries, user journeys, vocabulary,
response-state semantics, recovery behavior, release classification, success metrics, and
counter-metrics are all explicit.

The addendum supplies substantial implementation-readiness depth without redefining the observable
product contract. Its delegated details—wire schemas, cryptographic/store choices, durable-workflow
mechanisms, repository-local dependency pins, UI mappings, test fixtures, and release evidence—must
be present and aligned in architecture, UX, epics, contracts, and verification artifacts. Their
delegation is intentional, but absence downstream would be a readiness gap.

The principal traceability risk is temporal rather than product ambiguity: the addendum's readiness
and evidence index stops at E-14 and must be reconciled against newer August planning artifacts and
the current implementation state. Later validation must also verify that the exact role/action
matrix, response vocabularies, Story 8.11 release containment, sibling-repository entry gates, and
all measurable NFR thresholds are owned by epics and stories without treating historical Epics 1–5
as current release evidence.

## Epic Coverage Validation

The epics document distinguishes completed Epics 1–5 as historical implementation evidence and
assigns current production authority exclusively to the 33 AC-bearing stories in Epics 6–8. The
matrix below therefore uses the Epics 6–8 production owner rather than the historical implementation
reference.

### Coverage Matrix

| FR | PRD requirement | Production epic/story coverage | Status |
| --- | --- | --- | --- |
| FR-1 | Create Project through Folder-first, idempotent Durable Task activation. | Epic 7, Story 7.1 | ✓ Covered |
| FR-2 | Open an authorized Project with metadata, lifecycle, setup, references, and response-state evidence. | Epic 6, Story 6.1 | ✓ Covered |
| FR-3 | Update durable Project Setup additively and safely. | Epic 7, Story 7.2 | ✓ Covered |
| FR-4 | Archive an Active Project through Preview, confirmation, and Durable Task. | Epic 7, Story 7.13 | ✓ Covered |
| FR-5 | List visible Active and Archived Projects with scoped cursors and selection metadata. | Epic 6, Story 6.1 | ✓ Covered |
| FR-6 | Link an unassigned Conversation while Conversations retains membership authority. | Epic 7, Story 7.3 | ✓ Covered |
| FR-7 | Move a Conversation between Projects through confirmed durable recovery. | Epic 7, Story 7.4 | ✓ Covered |
| FR-8 | Initially bind or replace the exactly-one Project Folder. | Epic 7, Story 7.6; initial binding through Story 7.1 | ✓ Covered |
| FR-9 | Link an authorized File Reference without copying content. | Epic 7, Story 7.7 | ✓ Covered |
| FR-10 | Link an authorized Memory without copying payload. | Epic 7, Story 7.9 | ✓ Covered |
| FR-11 | Unlink Conversation, File, or Memory associations without deleting owner resources. | Epic 7, Stories 7.5, 7.8, and 7.10 | ✓ Covered |
| FR-12 | Resolve Candidate Projects from Conversation metadata. | Epic 6, Story 6.4 | ✓ Covered |
| FR-13 | Resolve Candidate Projects from Folder/File attachments. | Epic 6, Story 6.4 | ✓ Covered |
| FR-14 | Confirm an ambiguous Project choice without silent preselection or attachment. | Epic 7, Story 7.11; candidates read by Story 6.4 | ✓ Covered |
| FR-15 | Confirm a proposed new Project through the Folder-first creation path. | Epic 7, Story 7.12 | ✓ Covered |
| FR-16 | Retrieve allowlist-assembled, metadata-only Project Context. | Epic 6, Story 6.3 | ✓ Covered |
| FR-17 | Explain current context inclusion/exclusion without persisted trace history. | Epic 6, Stories 6.3 and 6.4 | ✓ Covered |
| FR-18 | Refresh Project Context through read-only recomputation. | Epic 6, Story 6.3 | ✓ Covered |
| FR-19 | Validate Project Setup and canonical Metadata Classification before admission. | Epic 7, Story 7.1; contract cutover through Story 6.7 | ✓ Covered |
| FR-20 | Retrieve the Conversation-start subset of Project Setup with admission truth. | Epic 6, Story 6.2 | ✓ Covered |
| FR-21 | Record and expose metadata-only Project audit truth. | Epic 8, Story 8.1 | ✓ Covered |
| FR-22 | Provide authorized operator read access across supported operational surfaces. | Epic 6, Stories 6.5 and 6.6; Epic 8, Stories 8.1, 8.3, 8.4, and 8.5 | ✓ Covered |
| FR-23 | Restore an Archived Project only after Folder validity and read-model confirmation. | Epic 7, Story 7.14 | ✓ Covered |
| FR-24 | Create a separately authorized, bounded, non-retained Safe Diagnostic Export. | Epic 8, Story 8.2 | ✓ Covered |

### Missing Requirements

No PRD Functional Requirement is absent from the current production-authority coverage map. No FR
claimed by the epics falls outside the PRD's canonical FR-1 through FR-24 set.

This result establishes declared coverage only. It does not judge whether individual stories are
independently implementable, correctly sequenced, sufficiently testable, or currently unblocked;
those questions are handled in later workflow steps.

### Coverage Statistics

- Total PRD FRs: 24
- FRs covered in production-authority epics: 24
- Missing FRs: 0
- Extra epic FR identifiers not present in the PRD: 0
- Coverage: 100%

## UX Alignment Assessment

### UX Document Status

The 1,121-line `ux-design-specification.md` is present and marked complete. It correctly separates
Projects-owned operational UX (FrontComposer Web, CLI, and MCP) from Chatbot-owned end-user
presentation while treating the independently owned Chatbot companion UX as a release input rather
than as Projects implementation authority.

### UX ↔ PRD Alignment

The UX is substantively aligned with the PRD in the following areas:

- It preserves the metadata-only, fail-closed boundary across Project, Conversation, Folder, File,
  Memory, Chatbot, operator, and audit surfaces.
- It carries the canonical `Complete|Partial|Unavailable|Denied` response consequences, component
  inclusion/freshness facts, resolution cardinalities, and Recovery Action Codes into Web, CLI,
  MCP, and the required Chatbot companion contract.
- It keeps `RefreshContext` synchronous and read-only and separates it from confirmed maintenance
  and Durable Task behavior.
- It defines explicit, accessible candidate/proposal confirmation, single-use 15-minute
  Confirmation Artifact behavior, lost-response recovery, authoritative task polling, all eight
  Durable Task states, bounded cancellation, and read-model-confirmed completion.
- It reproduces FR-24's separate authorization, synchronous/non-retained execution, two-export
  Tenant concurrency, 1 MiB response, 500-reference, 100-audit-row, deterministic ordering,
  truncation, unavailable-component, audit, and no-payload rules.
- It targets WCAG 2.2 AA, keyboard operation, deterministic focus behavior, non-color status,
  assistive-technology announcements, responsive reflow, and cross-surface semantic parity.

### UX ↔ Architecture Alignment

The architecture directly supports the UX's principal shape: `Projects.UI.Contracts` owns only
presentation descriptors, FrontComposer/platform hosts compose Web/CLI/MCP, the UI-free Contracts
kernel owns the canonical vocabulary and schemas, EventStore/platform owns Confirmation Artifact
and Durable Task infrastructure, and Chatbot owns candidate/proposal presentation. AD-21, AD-29,
AD-30, AD-32, and AD-34 align closely with the UX's diagnostic export, MCP limits, accessibility,
shared response snapshot, and companion presentation requirements.

That support remains conditional. G-1 says the required Durable Task engine and opaque Confirmation
Artifact capability is still unselected; G-3 requires resolution of the FrontComposer 4.0.0 versus
4.0.1 disposition and authenticated generated-surface parity; G-4 through G-6 retain composition,
identity/runtime, and toolchain gates. The UX is architecturally expressible, but these external
capability gates prevent treating it as implementation-ready evidence today.

### Alignment Issues

1. **HIGH — Confirmation policy is over-broad in the UX.** The Journey Patterns say to “Preview
   every mutation,” and the Confirmation Pattern says confirmations are required for mutating
   actions. PRD FR-6, FR-8, FR-9, and FR-10 and architecture AD-5 deliberately distinguish
   actor-selected additive Conversation/File/Memory links and initial Folder binding—which are
   Durable-Task-only—from inferred links and consequential archive/restore/move/replacement/unlink/
   resolution/proposal actions, which require Preview and Confirmation. The UX must publish the
   same action-admission classification rather than adding a second confirmation to every mutation.
2. **MEDIUM — The Resolution Trace component introduces noncanonical outcome labels.** Its listed
   states include `Resolved`, `excluded`, and `failed closed`, while the shared resolution result is
   exactly `NoMatch|SingleCandidate|MultipleCandidates`. Exclusion and fail-closed behavior belong
   in response/component inclusion, freshness, and reason fields. The component specification must
   separate those dimensions and use `SingleCandidate` instead of a local `Resolved` synonym.
3. **MEDIUM — Operator accessibility verification is less exact than the governing contract.** The
   UX testing section includes automated checks, keyboard testing, screen-reader spot checks,
   responsive viewports, and high-volume data, but it does not explicitly require operator testing
   at 200% zoom, authenticated manual keyboard/screen-reader evidence at small/median/maximum data
   shapes, or the AD-30 rule that unresolved critical or serious violations block capability and
   release. Those evidence conditions should be stated directly, not left to inference from WCAG
   conformance.

### Missing UX Evidence and Warnings

- **CRITICAL RELEASE BLOCKER — No approved, version-pinned Chatbot companion UX artifact was found.**
  The Projects UX specification defines what that independently owned artifact must prove, but it
  is not itself the companion artifact and no workspace file matches that role. The required owner
  repository, immutable revision, contract version, approval date, approving authority, and
  reproducible authenticated evidence are therefore absent. By the UX's own gate, this blocks
  Stories 8.8 and 8.11 and prevents release approval.
- **WARNING — FrontComposer evidence cannot yet validate the proposed operational views.** Until G-3
  is satisfied, descriptor discovery, generated Web/CLI/MCP schemas, real credential propagation,
  current MCP annotations/tasks, and authenticated parity remain planned rather than accepted UX
  implementation evidence.

### UX Alignment Conclusion

UX documentation exists and covers the product's key journeys and architectural boundaries, so
there is no wholesale UX-specification absence. It is not release-ready: the missing approved
Chatbot companion artifact is critical, the action-admission classification must be corrected, and
canonical resolution vocabulary and operator accessibility evidence need tightening before the UX
can be used as clean implementation authority.

## Epic Quality Review

### Review Scope and Structure

The quality review evaluates the 33 AC-bearing production-authority stories in Epics 6–8. Epics
1–5 are explicitly retained as immutable implementation history, not as the current implementation
plan, so their superseded technical/tracer-bullet structure is not scored against current readiness.
The prerequisite ledgers (`6.1-P*`, `7.1-P*`, `8.8-P*`, and `8.11-P*`) are also correctly identified
as external enablement/evidence packages rather than being disguised as user stories.

All 33 current stories have an actor, desired outcome, value statement, traceability, acceptance
criteria containing Given/When/Then tokens, an estimate, and an explicit completion boundary. The
plan is recognizably brownfield: reversible shadow-read cutover, historical-event compatibility,
single-writer fencing, legacy-record reconciliation, package/public-surface compatibility, and
rollback evidence are present. No starter-template requirement applies, and no up-front “create all
tables/entities” story exists; persistence and projections are introduced through the value slice
that needs them.

### Epic-Level Compliance

| Epic | User-value focus | Independence and sequencing | Story quality | Result |
| --- | --- | --- | --- | --- |
| Epic 6 — Authorized Project Reads | Delivers usable authorized list/open/context/resolution and operator read surfaces. | Correctly precedes write/release work, but Stories 6.5 and 6.6 pull audit behavior from future Story 8.1. | Mostly cohesive and testable; Story 6.7 is broad and has one malformed BDD criterion. | **Noncompliant until forward dependency is removed.** |
| Epic 7 — Durable Project Decisions | Delivers direct creation, setup, association, lifecycle, confirmation, and recovery outcomes. | Uses only the Epic 6 baseline and earlier stories; references to later actions are safe rejection/next-step statements, not completion dependencies. | Generally well sliced by operation; Story 7.2 omits one required observable completion condition and Story 7.14 reverses Preview/confirmation order in an AC. | **Conditionally compliant after AC repair.** |
| Epic 8 — Safe Operations and Release Confidence | Stories 8.1–8.5 deliver operator/support value, while 8.6–8.11 primarily deliver platform, test, supply-chain, and release-governance milestones. | Dependencies point to prior epics/stories and explicit external evidence packages; no forward story dependency was found inside Epic 8. | Strong measurable evidence language, but the epic mixes operational product value with a second release-evidence value stream and Story 8.3 is oversized. | **Major structural concern.** |

### Critical Violations

1. **Stories 6.5 and 6.6 have a forward dependency on Story 8.1.** Story 6.5 promises a Web audit
   timeline and Story 6.6 promises an `audit` CLI command and audit-identifier parity. The current
   production plan does not deliver the task/audit/reconciliation read capability until Story 8.1.
   Consequently, those Epic 6 stories cannot satisfy their own completion boundaries using only
   prior work. Either move the FR-21 read slice before 6.5/6.6, or remove audit behavior from those
   stories and add the Web/CLI audit adapters to Story 8.1 or a later prior-ordered story.

### Major Issues

1. **Epic 8 mixes product value with technical/release milestones.** Operator visibility, export,
   and Web/CLI/MCP operation (8.1–8.5) form one consumable operational outcome. Health plumbing,
   package graph/supply-chain enforcement, evidence integration, performance qualification,
   resilience qualification, and the terminal release record (8.6–8.11) form a different
   enablement/release-governance outcome. Under the user-value standard, the latter stories read as
   technical milestones even though they are necessary. Split the epic into an operational-value
   epic and an explicitly stakeholder-valued production-qualification/release epic, or represent
   the pure evidence acquisitions as gates/work packages attached to the value they accept.
2. **Story 8.3 is too broad for an independently completable story.** It asks one Web story to
   present every archive, restore, Conversation move, Folder replacement, unlink, Preview,
   Confirmation Artifact, eight-state task, renewal, lost-response, `NeedsAttention`, cancellation,
   terminal, denial, accessibility, Fluent governance, and refresh behavior. Split a shared
   task/confirmation presentation foundation from lifecycle and association action slices, keeping
   each slice independently demonstrable.
3. **Story 7.2 does not prove FR-3 completion.** FR-3 requires updates to be observable from the
   authoritative read model, and AD-32 says Setup update completes only after read-model
   confirmation. Story 7.2's ACs cover durable admission, validation, idempotency, and denial but
   omit that terminal-success condition. Add an AC that withholds `Succeeded` until stored Setup and
   the authoritative read model agree.
4. **Story 7.14's first AC reverses the interaction order.** It starts with a “valid confirmation”
   and then says “When Preview runs,” although Preview must verify the archived Project/Folder plan
   and issue the Confirmation Artifact before confirmation can exist. Split this into a
   `RequestPreview` AC and a subsequent valid-artifact confirmation/task AC.
5. **Story 8.2 specifies undefined duplicate-export behavior.** Its third AC combines the defined
   third-concurrent-export throttle with a lost/duplicate request and requires that it “does not
   double-produce.” FR-24 and AD-21 intentionally define a synchronous, non-retained query with no
   task or cursor, but define no export idempotency identity or retained result. Separate concurrency
   throttling from transport-loss behavior and either define an authorized idempotency contract
   consistent with non-retention or remove the unsupported exactly-once implication.

### Minor Concerns

1. **Story 6.7 has malformed BDD syntax.** Its first criterion contains one `Given`, two consecutive
   `When` clauses, and one `Then`. Combine the equivalence/contract alignment facts into the `Given`
   precondition and reserve one `When` for the cutover action.
2. **Epic titles remain system-centric.** “Authorized Project Reads,” “Durable Project Decisions,”
   and “Safe Operations and Release Confidence” are understandable, but only the body text makes
   the actor outcome explicit. Rephrasing titles around what Chatbot users/operators/release owners
   can accomplish would improve scan-level user-value clarity.

### Dependency and Sizing Summary

- Prior-epic dependencies (Epic 7 on Epic 6; Epic 8 on Epics 6–7) are sequenced correctly.
- Apart from the audit dependency in Stories 6.5/6.6, explicit story references point backward or
  describe a safe later option without preventing the current story from completing.
- Stories 6.3, 6.4, 6.7, 7.1, 7.4, 7.6, 7.11, 7.12, 7.14, 7.15, 8.2, 8.9, and 8.10 are estimated
  `L`; their completion boundaries remain cohesive enough to retain, subject to the specific issues
  above. Story 8.3 is the outlier whose cross-action surface is effectively epic-sized.
- Traceability is present on every story and collectively covers FR-1–FR-24 and NFR-1–NFR-11.

### Epic Quality Conclusion

The corrective plan is unusually explicit about external ownership, immutable evidence,
compatibility, rollback, and completion boundaries, and most stories are independently testable.
It does not yet pass the enforced quality bar because two Epic 6 stories depend on future Epic 8
audit functionality. The mixed technical/release structure of Epic 8 and the identified story-level
AC/sizing defects also require correction before the story set can be considered implementation
ready.

## Summary and Recommendations

### Overall Readiness Status

**NOT READY**

The requirements baseline is complete and all 24 Functional Requirements have declared production
story coverage. The current architecture spine also contains the authoritative decisions needed to
shape the solution, and the explicitly superseded architecture has been archived after confirming
that it contains no unique requirement that belongs in the spine.

Those strengths do not authorize implementation. The production plan itself remains frozen behind
Story 6.1's unresolved external prerequisite chain and a required independent `READY` rerun. In
addition, a release-mandatory Chatbot companion UX artifact is missing, two Epic 6 stories have a
forbidden forward dependency, and the UX/story contracts contain material admission, vocabulary,
testability, and sizing defects. Starting production-authority story implementation now would make
the team build against inconsistent and non-executable planning inputs.

### Critical Issues Requiring Immediate Action

1. **Complete the Story 6.1 entry path before implementation begins.** The current plan records
   `P1R → {P0, P2} → P3 → P4 → independent readiness rerun → Story 6.1`; P1R, P0, P2, P3, and P4 are
   open, the target is not yet an accepted immutable revision, and G-1/G-4/G-5/G-6 capabilities are
   not accepted as executable clean-checkout evidence. Story 6.1 must remain blocked.
2. **Obtain and pin the independently approved Chatbot companion UX and evidence package.** No such
   artifact exists in the workspace. Record owner repository, immutable revision, contract version,
   approval date/authority, authenticated commands, fixtures, artifacts, results, and disposition.
   Its absence blocks 8.8-P3, Story 8.8, Story 8.11, and release.
3. **Remove the Epic 6 → Epic 8 audit dependency.** Stories 6.5 and 6.6 cannot deliver an audit
   timeline/command before Story 8.1 supplies the production-authority task/audit/reconciliation read
   slice. Reorder that slice or defer the audit adapters to Story 8.1.
4. **Correct the UX action-admission contract.** Replace the blanket “Preview every mutation” rule
   with the canonical split between task-only actor-selected create/setup/additive links/initial
   Folder binding and confirmation-required inferred or consequential actions.

### Recommended Next Steps

1. Revise `epics.md` first: repair the audit sequencing; split oversized Story 8.3; correct Stories
   7.2, 7.14, 8.2, and 6.7; and separate or explicitly reframe Epic 8's operational value and
   release-qualification work.
2. Revise `ux-design-specification.md` to carry the exact action classification, use only canonical
   resolution outcome vocabulary, and state the full authenticated operator accessibility evidence
   contract, including 200% zoom and small/median/maximum data shapes.
3. Have the Chatbot owner produce and approve the immutable companion UX/evidence package required
   by 8.8-P3. Reference its pin from Projects artifacts without importing Chatbot implementation
   authority into this repository.
4. Execute and accept the external capability and Story 6.1 prerequisite chain from clean checkouts,
   including the recorded run/test/down/evidence commands and executable rollback proof. Do not
   treat stale, local, uncommitted, unavailable, failed, or skipped evidence as acceptance.
5. Rerun implementation readiness independently after the planning corrections and prerequisite
   acceptance. Proceed to production-authority story implementation only if that superseding result
   is exactly `READY`; retain Story 8.11 and dated Jerome + John approval as the terminal release
   gate after implementation evidence is complete.

### Final Note

This assessment identified **13 explicit artifact-quality issues across UX alignment and epic/story
quality**, in addition to the open external entry-gate chain that already blocks Story 6.1. The
highest-priority defects are the missing Chatbot companion input, the Epic 6 forward dependency,
and the UX admission-policy contradiction. Functional coverage is 100%, but coverage does not
compensate for non-executable prerequisites or internally inconsistent implementation authority.

**Assessment date:** 2026-08-02  
**Assessor:** Codex using the BMad Implementation Readiness workflow
