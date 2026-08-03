---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
status: NOT_READY
assessor: Codex implementation-readiness workflow
completedDate: 2026-08-02
inputDocuments:
  prd:
    - prds/prd-Hexalith.Projects-2026-05-24/prd.md
    - prds/prd-Hexalith.Projects-2026-05-24/addendum.md
  architecture:
    - architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md
  epics:
    - epics.md
    - epics-architecture-conformance-checklist-2026-07-16.md
  ux:
    - ux-design-specification.md
---

# Implementation Readiness Assessment Report

**Date:** 2026-08-02
**Project:** projects

## Document Discovery

### Documents Selected for Assessment

#### PRD

- `prds/prd-Hexalith.Projects-2026-05-24/prd.md` — 46,095 bytes; modified 2026-07-15
- `prds/prd-Hexalith.Projects-2026-05-24/addendum.md` — 26,121 bytes; modified 2026-08-02

The PRD is maintained as an organized folder without an `index.md`. Editorial variants and reconciliation, validation, review, audit, and handoff working documents were excluded from the assessment set.

#### Architecture

- `architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md` — 54,848 bytes; modified 2026-07-18

The architecture is maintained as an organized folder without an `index.md`. Its review and reconciliation working documents were excluded. The explicitly superseded architecture under `archive/` was also excluded.

#### Epics and Stories

- `epics.md` — 216,138 bytes; modified 2026-08-02
- `epics-architecture-conformance-checklist-2026-07-16.md` — 43,839 bytes; modified 2026-08-02; selected as supplementary conformance evidence

No sharded epic set was found. The pre-reconciliation backup was excluded.

#### UX Design

- `ux-design-specification.md` — 66,935 bytes; modified 2026-08-02

No sharded UX set was found. The HTML design-directions companion was excluded from the Markdown assessment set.

### Discovery Issues

- All four required document types were found.
- No whole-versus-sharded duplicate conflict was found.
- Alternate PRD-named editorial artifacts were resolved by selecting the canonical `prd.md` plus its current `addendum.md`.

## PRD Analysis

### Functional Requirements

#### FR-1: Create Project

Chatbot can admit Project creation as an idempotent Durable Task. A Project becomes caller-visible and `Active` only after exactly one authorized Project Folder is verified and bound. Realizes UJ-2.

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

- Opening returns only data visible to the requesting Tenant and actor.
- Opening follows the Context Response State, Evidence Freshness State, and Recovery Action Code semantics in PRD section 5.
- Pre-activation creation tasks are not exposed through Project open APIs.
- Archived or unavailable Projects are identified and cannot silently become active Conversation context.

#### FR-3: Update Project Setup

Chatbot can update Project Setup used for Conversation continuity.

- Updates are idempotent, durable, and observable from the authoritative read model.
- Setup may include goals, user-facing instructions, context preferences, source inclusion/exclusion policy, and Conversation-start defaults.
- Setup describes Conversation behavior and context policy, not model-provider internals.
- Updates remain additive and serialization-tolerant and reject secrets, unrestricted paths, and foreign payloads.

#### FR-4: Archive Project

An authorized Project User, Tenant Operator, or Tenant Project Administrator can archive an Active Project through server Preview, single-use confirmation, and an idempotent Durable Task. FR-23 defines the corresponding restore operation.

- Project Lifecycle State remains limited to `Active` and `Archived`.
- Confirmation is invalidated when actor authority or Project version changes.
- Archived Projects are excluded from Project Resolution unless explicitly requested.
- Completion is not reported until the read model confirms `Archived`.
- Existing references remain auditable after archival.

#### FR-5: List Projects

Authorized callers can list visible Active and Archived Projects.

- Results are Tenant-scoped, authorization-filtered, and filterable by Project Lifecycle State.
- Each result contains authorized Project identity, Project name, lifecycle state, current Project version, Project Folder availability, and the PRD section 5 response/freshness/recovery metadata needed for selection without loading full Project Context.
- Pre-activation tasks never appear as Projects.
- Cursor pages default to 50 items and cap at 200; cursors remain scoped to the authenticated query.

#### FR-6: Link Conversation

An authorized Project User can link an existing Conversation to a Project. Realizes UJ-1 and UJ-3.

- A Conversation belongs to exactly one Project in v1.
- An explicitly actor-selected additive link uses an idempotent Durable Task without a second confirmation; an inferred link requires Preview and confirmation.
- Linking a Conversation already assigned elsewhere requires FR-7 rather than a second membership.
- Authorization failure prevents any protected resource access or durable effect.
- The link stores stable identity and metadata, never transcript content.

#### FR-7: Move Conversation Between Projects

An authorized Project User or Tenant Project Administrator can move a Conversation through Preview, single-use confirmation, and an idempotent Durable Task.

- Preview binds both Projects, the Conversation, actor, and current resource versions.
- Completion yields exactly one Project membership and a durable cross-context receipt.
- Failure, duplicate delivery, or lost response cannot leave two memberships silently valid.
- The move is audited using metadata only and fails closed when either Project or the Conversation cannot be authorized.

#### FR-8: Set Project Folder

An authorized Project User can set the single Project Folder; a Project User or Tenant Project Administrator can replace it through Preview and confirmation. Realizes UJ-2.

- Every Active Project has exactly one authorized Project Folder.
- Initial actor-selected binding is idempotent; inferred binding requires confirmation.
- Replacement binds old and new Folder evidence to the Confirmation Artifact and completes only after the authoritative read model confirms the replacement.
- Projects stores Folder identity and metadata, never file contents or unrestricted paths.
- `Hexalith.Folders` remains the authorization and system-of-record boundary.

#### FR-9: Link File Reference

An authorized Project User can link a File Reference without changing the Project Folder.

- File References are optional and do not replace the Project Folder.
- Actor-selected additive linking is idempotent; inferred linking requires confirmation.
- Projects stores stable File identity and metadata only; authorization remains delegated to `Hexalith.Folders`.

#### FR-10: Link Memory

An authorized Project User can link a Memory. Realizes UJ-1 and UJ-3.

- Actor-selected additive linking is idempotent; inferred linking requires confirmation.
- Projects stores stable Memory identity and metadata only.
- Authorization remains delegated to `Hexalith.Memories`.

#### FR-11: Unlink Context Reference

An authorized Project User or Tenant Project Administrator can unlink a Conversation, File Reference, or Memory through Preview, confirmation, and an idempotent Durable Task. The Project Folder can be replaced but not removed from an Active Project.

- Unlinking removes only the association and never deletes the underlying resource.
- Preview identifies the affected reference and current Project version.
- Completion is durable, audited using metadata only, and confirmed by the read model.
- The operation fails closed on stale authorization or resource evidence.

#### FR-12: Resolve Project From Conversation

Chatbot can request Candidate Projects for a Conversation with no explicit Project. Realizes UJ-3.

- The result is `NoMatch`, `SingleCandidate`, or `MultipleCandidates` with current Resolution Reason Codes.
- Only Active, read-model-confirmed Projects are considered by default.
- Pre-activation tasks and unauthorized or stale resources cannot become candidates.
- The response follows the PRD section 5 contract; `Unavailable` and `Denied` never return a selected candidate.

#### FR-13: Resolve Project From Attachments

Chatbot can resolve Candidate Projects from an attached Project Folder or File References. Realizes UJ-2.

- Matching uses current authorized Folder/File identity and metadata, not file contents.
- Applicable candidates include `ProjectFolderMatched` or `FileReferenceMatched` reason codes.
- Missing, stale, or unavailable authorization evidence fails closed.

#### FR-14: Confirm Ambiguous Project

When resolution returns multiple candidates, Chatbot presents an accessible, unselected comparison and records the Project User's choice through a Confirmation Artifact and Durable Task. Realizes UJ-3.

- No candidate is silently or visually preselected.
- The artifact is bound to Tenant, actor, action, Conversation, candidates, normalized request, Preview, and current versions; it expires after 15 minutes and is single-use.
- Stale, expired, replayed, or tampered confirmation is rejected safely and requires a fresh Preview.
- Only Read-Model-Confirmed Completion creates or updates the Conversation association and audit history.
- Chatbot supports states for confirmation, cancellation, retry, expiry or staleness, lost-response recovery, and task status.

#### FR-15: Propose New Project

When no suitable Project exists, Chatbot can present a proposed Project and admit creation only after the Project User confirms a bound Preview. Realizes UJ-2.

- The proposal may suggest a Project name and setup metadata but creates nothing before confirmation.
- The Confirmation Artifact binds the initiating Conversation, authorized attachments, Folder plan, normalized request, and current evidence.
- Confirmed creation follows FR-1 and exposes no Project before Folder binding and read-model confirmation.
- Non-success outcomes follow the PRD section 5 recovery contract; cancellation returns `Cancelled`, terminal failure returns `Failed`, and expired or stale evidence creates no task.

#### FR-16: Get Project Context

Chatbot can request Project Context for an Active Project. Realizes UJ-1 and UJ-4.

- Context is Tenant-scoped, actor-authorized, and available only for a read-model-confirmed Active Project with exactly one authorized Project Folder.
- It contains Project Setup and reference metadata, not payloads owned by other bounded contexts.
- It follows the PRD section 5 contract, representing every excluded, stale, rebuilding, or unavailable reference as a metadata-only component; `Denied` discloses no protected detail.

#### FR-17: Explain Context Selection

Authorized callers can obtain current metadata explaining why a reference was included or excluded. Realizes UJ-4.

- Explanations are current Resolution Traces, not reconstructed history.
- Traces contain no secrets, payloads, prompts, unrestricted paths, raw upstream problems, or unconfirmed-candidate detail.
- Traces are request-scoped and not persisted; only confirmed outcomes enter audit history.

#### FR-18: Refresh Project Context

Chatbot can request a read-only refresh after links, setup, authorization, or resource availability changes.

- Refresh recomputes from current authorized Project, Conversation, Folder, File Reference, Memory, and version metadata.
- Refresh itself never mutates Project or reference state and creates no maintenance audit event.
- The refreshed response follows PRD section 5, including new snapshot metadata, component evidence, recovery actions, and the binding transition rules for `Partial`, `Unavailable`, and `Complete`.

#### FR-19: Validate Project Setup

Projects validates setup and creation admission before accepting durable work.

- Project name remains the only required user-authored creation field.
- Canonical creation requests require valid system-supplied Metadata Classification; invalid classification is rejected before command submission.
- Validation permits a supplied authorized Project Folder or same-name Folder creation, but never defaults a caller-visible Project to Active before Folder completion.
- Validation rejects secrets, unrestricted paths, unsupported references, control/invisible characters where unsafe, and foreign payloads.
- Failures identify safe field/reason codes without echoing sensitive values.

#### FR-20: Retrieve Conversation-Start Setup

Chatbot can retrieve the subset of Project Setup needed to start or resume a Conversation.

- The result includes goals, user-facing instructions, context preferences, and default source policy.
- It excludes internal audit metadata and unavailable or unauthorized references.
- It is bound to one authorized `projectVersion` and `asOf` snapshot and follows PRD section 5. Chatbot may admit the first response only for `Complete` or `Partial`; `Unavailable` or `Denied` blocks first-response admission and returns the applicable Recovery Action Codes without re-querying every bounded context.

#### FR-21: Record Project Audit Events

Projects records metadata-only audit events for consequential task admission and outcome, confirmed Project mutations, security-relevant confirmation outcomes, reconciliation, and Safe Diagnostic Export.

- Audit covers task admission and terminal outcome; confirmation use and cancellation; rejection of stale, replayed, or tampered confirmations; authorization denial; creation, archive, restore, move, relink, Folder replacement, unlink, confirmed resolution, and confirmed proposed creation; manual reconciliation; and Safe Diagnostic Export creation. Audit also records stable upstream receipt identifiers.
- Equivalent idempotent retries do not create duplicate audit events.
- Intermediate task states, polls, retries, dependency latency, notifications, unused expiry, and read-only Resolution Traces remain operational telemetry rather than durable audit.
- Audit contains Tenant, actor, Project/action identity, timestamp, safe reason/outcome codes, and affected reference identifiers, never payloads or secrets.

#### FR-22: Support Operator Read Access

Tenant Operators and Tenant Project Administrators can inspect authorized Project metadata, lifecycle state, references, Durable Task status, confirmed resolution outcomes, and audit metadata.

- Access is Tenant-scoped, action-authorized, and metadata-only across Web, CLI, and MCP.
- Project Users may inspect only their own permitted task status through Chatbot.
- Pre-activation tasks remain separate from Project list/open APIs; Tenant Operators and Tenant Project Administrators may inspect their safe status, and Tenant Project Administrators may perform authorized reconciliation.
- Read permission alone grants neither Safe Diagnostic Export nor a mutation.

#### FR-23: Restore Archived Project

An authorized Project User, Tenant Operator, or Tenant Project Administrator can restore an Archived Project through Preview, confirmation, and an idempotent Durable Task. This is the restore counterpart to FR-4 and realizes UJ-5.

- Preview verifies Tenant, actor, authority, current Project version, and exactly one authorized Project Folder.
- If the prior Folder is invalid or missing, Preview requires an authorized replacement or same-name Folder creation before confirmation.
- The Project remains Archived until Folder evidence and read-model-confirmed restore completion succeed.
- If Folder creation succeeds but activation cannot commit, the task enters `NeedsAttention`; Projects never automatically deletes a Folders-owned resource.
- Stale/unavailable evidence, replay, cancellation, duplicate delivery, concurrency, and lost response cannot expose an invalid Active Project.
- Completion and reconciliation outcomes are audited using metadata only.

#### FR-24: Create Safe Diagnostic Export

A separately authorized Tenant Operator or Tenant Project Administrator can create a bounded Safe Diagnostic Export through Web, CLI, or MCP.

- Export permission is distinct from FR-22 read permission; Chatbot cannot create exports.
- Every attempt and outcome is audited using metadata only.
- The complete encoded export, including envelope and truncation metadata, is at most 1 MiB and contains at most 500 reference rows and 100 audit rows.
- Reference ordering is stable and deterministic; audit rows are newest-first with stable tie-breaking.
- Truncation reports included/omitted counts and safe reasons without excluded detail; exports have no continuation cursor.
- Upstream unavailability is represented safely without raw errors or fabricated completeness.
- Projects never retains generated exports.

**Total FRs: 24**

### Non-Functional Requirements

#### NFR-1: Security and privacy

Every read, write, task, confirmation, audit event, and export is Tenant-, actor-, action-, target-, and current-version-scoped. Trust-bearing mutations fail closed when authorization evidence is stale, unknown, rebuilding, or unavailable. Logs, telemetry, errors, and evidence remain metadata-only.

#### NFR-2: Encryption and key management

Production traffic uses platform-approved authenticated encryption in transit. Durable Project, task, idempotency, and audit data uses platform-managed encryption at rest. Projects owns no private keys; approved platform KMS/secret-provider rotation and revocation evidence is release-blocking.

#### NFR-3: Availability and recovery

Authenticated metadata APIs and task admission target 99.9% monthly availability excluding planned maintenance. With required dependencies healthy, service RTO after process/node failure is 15 minutes, and accepted tasks resume or reach truthful `NeedsAttention` within 5 minutes.

#### NFR-4: Durability and idempotency

A Project event acknowledged as committed has RPO 0 within the configured primary-region durability domain. Active Projects are never folderless. Equivalent retries return the same task; changed requests conflict. Accepted tasks are never silently dropped or duplicated.

#### NFR-5: Performance and scale

v1 supports 10,000 Projects per Tenant, 5,000 Context References per Project excluding its Folder, and 100,000 retained audit records per Project. Metadata reads target p95 under 500 ms at a data shape of 1,000 Projects and 500 references, and p95 under 1 second at the supported maximum. Durable-task admission targets p95 under 500 ms under authenticated warm steady-state with required dependencies available.

#### NFR-6: Pagination and export bounds

Cursor pages default to 50 and cap at 200. Safe Diagnostic Export obeys FR-24's per-export global size/row bounds and a per-Tenant limit of two concurrent exports.

#### NFR-7: Back-pressure and dependency control

Per Tenant, v1 supports 100 metadata reads/second with burst 200, 20 mutation admissions/second with burst 40, 1,000 nonterminal tasks, and 2 concurrent Safe Diagnostic Exports. Interactive dependency timeout defaults to 2 seconds and durable-step timeout to 10 seconds. Idempotent calls retry at most three times within 30 seconds before truthful waiting or intervention status. Overload returns structured retry guidance.

#### NFR-8: Retention and transient data

Active tasks remain pollable until terminal. A terminal result and its scoped idempotency record remain available for at least 30 days or for the result's lifetime, whichever is longer. Preview/Confirmation Artifacts expire after 15 minutes. Audit metadata is retained at least 365 days and never less than applicable retained event-history obligations. Resolution Traces and generated exports are not persisted.

#### NFR-9: Accessibility

Chatbot candidate, confirmation, cancellation, recovery, and task journeys, plus operator read, mutation, and export journeys, conform to WCAG 2.2 AA. They are keyboard operable, visibly focused, announced to assistive technology, do not rely on color or timing alone, and are usable at 200% zoom and a width of 320 CSS pixels. Verification combines automated evidence with authenticated manual keyboard and screen-reader evidence.

#### NFR-10: Compatibility

Contracts are additive and serialization-tolerant unless a breaking change is explicitly approved. Historical v1 data and unversioned name-only creation remain readable/accepted throughout v1. Retirement requires a major version, migration notice, usage evidence, compatibility tests, and rollback evidence; event history is not rewritten.

#### NFR-11: Release evidence

Authenticated persisted-boundary, cross-Tenant, restart/concurrency, duplicate-delivery, lost-response, accessibility, privacy, performance, deployment, smoke, rollback, and stakeholder-acceptance evidence must pass. A failed critical case or unexplained critical skip blocks release; unavailable environments remain “not verified,” never “passed.”

**Total NFRs: 11**

### Additional Requirements

#### Product and release constraints

- v1 is an internal Hexalith.Chatbot platform module rather than a standalone project-management product.
- Project lifecycle is exactly `Active` or `Archived`; task states are separate and must never become Project lifecycle values.
- Every Active Project has exactly one authorized Project Folder, and each Conversation belongs to exactly one Project.
- Projects stores metadata and references only. Conversations, Folders, and Memories remain systems of record and authorization boundaries.
- Core value may be implemented first, but all FR-1–FR-24 and NFR-1–NFR-11 are mandatory for production release; there is no approved smaller production cut.
- Excluded capabilities include content indexing, Memory payload storage, transcript storage, persisted inference history or diagnostic exports, generic project management, autonomous MCP confirmation, blanket service-identity mutation, cross-Tenant sharing, customer-managed encryption keys, and cross-region disaster-recovery guarantees.

#### Observable context and recovery contract

- Project open, list, resolution, context, Conversation-start, and proposal-recovery responses preserve `responseState`, `asOf`, `projectVersion`, `resolutionResult`, component inclusion/freshness/reason metadata, and applicable recovery actions.
- Context Response State is exactly `Complete`, `Partial`, `Unavailable`, or `Denied`; Evidence Freshness State is exactly `Current`, `Stale`, `Rebuilding`, or `Unavailable`.
- Recovery Action Codes are `None`, `Retry`, `RefreshContext`, `RequestPreview`, `RenewPreview`, `PollTask`, `ResolveNeedsAttention`, `SelectAlternative`, and `ContactAdministrator`.
- Refresh always recomputes authorization and evidence and returns a new snapshot; it never silently rewrites an earlier response.
- Durable Task status is exactly `Pending`, `Running`, `WaitingForDependency`, `NeedsAttention`, `Succeeded`, `Rejected`, `Failed`, or `Cancelled`; the final four are terminal and immutable, while `NeedsAttention` is nonterminal and recoverable.

#### Durable workflow and platform ownership

- Architecture must define exact durable task state transitions, checkpoints, worker ownership, leases, restart recovery, two-instance convergence, duplicate-delivery handling, lost-response recovery, cancellation cutoff, terminal-state immutability, cross-context receipts, compensation, and reconciliation.
- Architecture must identify the irreversible commit point and distinguish dependency waiting, human reconciliation, rejection, and failure.
- Orphaned or reserved Folders require recovery without automatic deletion of Folders-owned resources.
- `Hexalith.Projects` owns domain policy, Project contracts, and Project-specific task transitions. EventStore DomainService/platform owns hosting, event persistence/publication, subscriptions, read models, cursors, health, telemetry, and reusable workflow capability. The platform AppHost owns topology. FrontComposer/platform hosts own Web, CLI, and MCP composition.
- Production-capable hosts must use real credentials and delegated service identity; allow-all development identity/authorization stubs are forbidden.

#### Confirmation, idempotency, and canonicalization

- Preview and Confirmation Artifact design must define schemas, signing/key ownership, normalized request material, resource-version binding, 15-minute expiry, single-use enforcement, replay response, and safe renewal.
- Idempotency is scoped to `(Tenant, actor, operation, key)` with explicit equivalence canonicalization, conflict behavior, lost-response recovery, and retention for at least 30 days and never less than the result lifetime.
- Canonicalization must preserve Unicode safety and U+2028/U+2029 parity without broadening input equivalence.
- Cancellation is allowed before the irreversible commit point; afterward the system returns a conflict or safe status response.
- Safe reason codes distinguish expired, stale, replayed, tampered, unauthorized, dependency-waiting, and reconciliation-required outcomes without protected detail.

#### Safe Diagnostic Export

- API/architecture must define versioned `projects.safe-diagnostic-export.v1` behavior consistently across Web, CLI, and MCP.
- The complete encoded response is capped at 1 MiB, 500 reference rows, and 100 audit rows; at most two exports may run concurrently per Tenant.
- Ordering is deterministic, truncation is explicit and metadata-only, unavailable components use safe markers, exports have no continuation cursor, and generated exports are never retained.

#### Contracts, packages, and compatibility

- Canonical Create Project metadata classification vocabulary is exactly `public_metadata`, `tenant_sensitive`, `credential_sensitive`, and `secret`; the label never authorizes secret storage.
- An authenticated integration adapter supplies classification from integration policy; Projects validates it before submission and never infers it from user text.
- Canonical requests reject missing, blank, null, non-string, case/whitespace variants, duplicate properties, and unknown classification values. Authorization precedes protected parsing. Rejection is `400 ValidationFailure` with `details.rejectedField = projectMetadata.metadataClass`, no rejected-value echo, and no command submission.
- Direct creation and proposal confirmation reuse one server-owned `SensitiveMetadataTierValidator`.
- Historical unversioned name-only creation alone receives v1 compatibility treatment; retirement requires migration, major-version approval, usage evidence, compatibility tests, and rollback criteria.
- The approved UI boundary is a non-packable `Hexalith.Projects.UI.Contracts` descriptor host depending on the UI-free Contracts kernel. MCP and CLI remain independent, and FrontComposer/Fluent/Shell dependencies must not enter the kernel.
- `Hexalith.Builds` is the sole version owner for `NSwag.MSBuild` 14.7.1 and `Fluxor.Blazor.Web` 6.9.0; Projects consumes versionless references and preserves central transitive pinning.

#### Migration and integration

- Legacy Active folderless Projects and in-flight Folder work must be inventoried and reconciled before those Projects appear in lists, resolution, or context.
- Event evolution is additive; historical events remain readable and event history is never rewritten.
- Migration planning includes compatibility adapters, replay comparison, value-slice cutover, routing rollback, archived Projects with invalid prior Folders, and prevention of unsafe dual writes.
- Any required EventStore, FrontComposer, Conversations, Folders, or Chatbot change needs its own approved story and verification; the Projects PRD does not implicitly authorize sibling-repository mutation.
- Chatbot owns end-user presentation; Projects owns versioned Preview, Confirmation Artifact, Durable Task, Resolution, and Context contracts. Companion UX must cover no-preselection comparison, confirmation/cancellation, expiry/staleness, lost responses, task status, exact state mappings, focus/live announcements, 200% zoom, 320 CSS-pixel responsiveness, safe degraded states, compatibility, and authenticated integration evidence.

#### Verification and release gates

- Tests require deterministic small, median, and maximum fixtures; authenticated persisted-boundary and cross-Tenant denial proof; authorization freshness; encryption/KMS evidence; replay/tamper and privacy proof; restart, convergence, duplicate, concurrency, cancellation, loss, compensation, reconciliation, and read-model confirmation; Web/CLI/MCP parity; accessibility evidence; outcome metrics; deployment, smoke, rollback, compatibility, and stakeholder acceptance.
- Unicode idempotency evidence must reject U+2028/U+2029 in identifier/envelope fields, preserve deterministic accepted descriptive metadata escaping, prove byte-for-byte real-server/generated-helper parity and non-collision, and gate any legacy fingerprint compatibility impact.
- Live Playwright has deterministic non-AppHost and explicit live opt-in lanes. The live lane discovers the ready `projects-ui` endpoint dynamically, rejects guessed URLs, uses Aspire teardown, and requires every retained skip to identify a concrete missing prerequisite.
- A failed critical case or unexplained critical skip blocks release. Missing environments remain `not verified`.
- The current addendum disposition is `NOT READY` (E-15). Containment remains until architecture-conformance sign-off, the Story 6.1 prerequisite chain, an independently owned Chatbot 8.8-P3 package, acceptable 8.3-P2/P3 packages, explicit Epic 8 cohesion disposition, and a later independent rerun returning exactly `READY`.

### PRD Completeness Assessment

The PRD is product-requirement complete and unusually explicit: all 24 FRs have stable identities and testable consequences; all 11 NFRs contain measurable or evidentiary acceptance conditions; roles, authority boundaries, lifecycle and recovery vocabularies, exclusions, success metrics, compatibility, and release-cut policy are defined. The addendum supplies the implementation-routing detail intentionally excluded from the observable product contract.

The completeness of the PRD does not imply implementation readiness. The addendum explicitly records the current implementation disposition as `NOT READY` and names unresolved prerequisite, ownership, acceptance-package, architecture-sign-off, and Epic 8 cohesion gates. Those gates must be validated against the selected epics and architecture in subsequent steps.

## Epic Coverage Validation

### Epic FR Coverage Extracted

The authoritative coverage map in `epics.md` assigns every PRD FR to at least one AC-bearing production owner in corrective Epics 6–8. Historical Epics 1–5 are implementation history/evidence and are not treated as current production authority.

| FR | PRD Requirement | Production Epic/Story Coverage | Status |
| --- | --- | --- | --- |
| FR-1 | Create Project | Epic 7, Story 7.1 | ✓ Covered |
| FR-2 | Open Project | Epic 6, Story 6.1 | ✓ Covered |
| FR-3 | Update Project Setup | Epic 7, Story 7.2 | ✓ Covered |
| FR-4 | Archive Project | Epic 7, Story 7.13 | ✓ Covered |
| FR-5 | List Projects | Epic 6, Story 6.1 | ✓ Covered |
| FR-6 | Link Conversation | Epic 7, Story 7.3 | ✓ Covered |
| FR-7 | Move Conversation Between Projects | Epic 7, Story 7.4 | ✓ Covered |
| FR-8 | Set/Replace Project Folder | Epic 7, Story 7.6; initial binding through Story 7.1 | ✓ Covered |
| FR-9 | Link File Reference | Epic 7, Story 7.7 | ✓ Covered |
| FR-10 | Link Memory | Epic 7, Story 7.9 | ✓ Covered |
| FR-11 | Unlink Context Reference | Epic 7, Stories 7.5, 7.8, and 7.10 | ✓ Covered |
| FR-12 | Resolve Project From Conversation | Epic 6, Story 6.4 | ✓ Covered |
| FR-13 | Resolve Project From Attachments | Epic 6, Story 6.4 | ✓ Covered |
| FR-14 | Confirm Ambiguous Project | Epic 7, Story 7.11; candidates read by Story 6.4 | ✓ Covered |
| FR-15 | Propose New Project | Epic 7, Story 7.12 | ✓ Covered |
| FR-16 | Get Project Context | Epic 6, Story 6.3 | ✓ Covered |
| FR-17 | Explain Context Selection | Epic 6, Stories 6.3 and 6.4 | ✓ Covered |
| FR-18 | Refresh Project Context | Epic 6, Story 6.3 | ✓ Covered |
| FR-19 | Validate Project Setup and Metadata Classification | Epic 7, Story 7.1; contract cutover through Story 6.7 | ✓ Covered |
| FR-20 | Retrieve Conversation-Start Setup | Epic 6, Story 6.2 | ✓ Covered |
| FR-21 | Record Project Audit Events | Epic 8, Story 8.1 | ✓ Covered |
| FR-22 | Support Operator Read Access | Epic 6, Stories 6.5 and 6.6; Epic 8, Stories 8.1, 8.3, 8.4, and 8.5 | ✓ Covered |
| FR-23 | Restore Archived Project | Epic 7, Story 7.14 | ✓ Covered |
| FR-24 | Create Safe Diagnostic Export | Epic 8, Story 8.2 | ✓ Covered |

### Missing Requirements

No PRD Functional Requirement is absent from the authoritative Epic 6–8 production coverage map.

No FR identifier appears in the epics coverage map that is absent from the PRD. The epic artifact uses the same canonical FR-1 through FR-24 inventory.

The selected conformance checklist independently confirms that every FR has an AC-bearing production owner. Its `conforms-with-notes` status and pending Solution Architect signature affect execution readiness, not the existence of FR coverage.

### Coverage Statistics

- Total PRD FRs: 24
- FRs covered in production-authority epics: 24
- Missing FRs: 0
- Extra epic FR identifiers not present in the PRD: 0
- Coverage: 100%

## UX Alignment Assessment

### UX Document Status

**Found:** `ux-design-specification.md` is present, marked complete, and defines the internal Hexalith.Projects operational UX across FrontComposer Web, CLI, and MCP.

The specification also declares a separately owned, version-pinned Hexalith.Chatbot companion UX artifact to be a mandatory release input. No Chatbot companion UX manifest or `8.8-P3` companion pin is present in the workspace.

### UX ↔ PRD Alignment

The internal Projects UX aligns substantially with the PRD:

- Its scope is the same metadata control plane described by the PRD, not a generic project-management product.
- The Project User, Tenant Operator, Tenant Project Administrator, and delegated-caller authority boundaries are preserved.
- The exact `Active`/`Archived` lifecycle, eight Durable Task states, four Context Response States, four Evidence Freshness States, three Resolution Results, and canonical Recovery Action Codes are represented.
- Preview/Confirmation is limited to confirmation-required actions; actor-selected additive actions remain task-only; `RefreshContext` remains synchronous and read-only.
- Project Context, reference health, candidate comparison, recovery, operator reads, audit, and Safe Diagnostic Export match FR-2, FR-4 through FR-24.
- Safe Diagnostic Export preserves separate authorization, synchronous/non-retained behavior, 1 MiB/500-reference/100-audit bounds, deterministic ordering, two concurrent exports per Tenant, and Web/CLI/MCP parity.
- WCAG 2.2 AA, keyboard operation, focus behavior, assistive announcements, 200% zoom, and 320 CSS-pixel reflow align with NFR-9.
- Payload exclusion, fail-closed disclosure, Tenant scope, and metadata-only evidence align with NFR-1 and the PRD product boundaries.

### UX ↔ Architecture Alignment

The Architecture Spine supplies explicit support for the internal UX:

- AD-2, AD-16, and AD-24 place presentation descriptors in non-packable `Projects.UI.Contracts` and runtime composition in FrontComposer/platform adapters, keeping the Contracts kernel UI-free.
- AD-4, AD-5, AD-13, and AD-19 provide the task, confirmation, recovery, transport, and authoritative-requery behavior required by the Maintenance Action Panel.
- AD-7, AD-21, and AD-26 support current-only diagnostics, bounded exports, and audit/telemetry separation.
- AD-29 constrains MCP authority and prevents self-confirmation or privilege expansion.
- AD-32 defines the exact response snapshot and recovery vocabulary used by all UX surfaces.
- AD-33 defines role-specific, surface-invariant action authorization.
- AD-34 makes the complete accessibility contract release-blocking.
- AD-27 supplies the metadata-read performance, pagination, and back-pressure envelope needed by dense operational surfaces.

All named internal UX components have an architectural home. Their implementation remains gated by the open G-3 FrontComposer-adapter and G-6 toolchain dispositions, but that is an execution-readiness blocker rather than a missing architecture decision.

### Alignment Issues

#### UX-ALIGN-1 — Missing independently owned Chatbot companion UX and evidence package — Critical

FR-14, FR-15, FR-20, NFR-9, AD-32, AD-34, and SM-5 span the end-user Chatbot journey. The internal UX document correctly routes that presentation outside Projects, but the separately approved Chatbot owner manifest and Projects pin (`evidence/epic8/8.8-P3-chatbot-companion-pin.json`) are absent. The UX, architecture, epics, and conformance checklist all state that this blocks Story 8.8 and terminal release acceptance.

**Required correction:** the Chatbot Presentation Owner and Chatbot Test Owner must supply and approve an immutable companion specification/evidence manifest; Projects may then record only the accepted pin. Projects must not author a substitute.

#### UX-ALIGN-2 — Historical resolution-case/replay language conflicts with current-only traces — Moderate

The UX specification still says users can “re-run or replay safe resolution diagnostics” and that diagnostic forms accept a “resolution case ID.” PRD FR-17 and Architecture AD-7 require Resolution Traces to be request-scoped, current-only, and nonpersistent. A lookup by durable resolution-case identity or replay of persisted inference history is therefore not supported.

**Required correction:** replace historical replay/case-ID wording with current recomputation using authorized Conversation/Folder/File/Memory inputs and an ephemeral request/correlation identifier. Make clear that only confirmed outcomes enter durable audit.

#### UX-ALIGN-3 — Legacy resolution-result labels remain in Journey 1 — Moderate

Journey 1 uses `Resolved`, `Ambiguous`, `Excluded`, and `FailedClosed` as resolution outcomes. The later component section is correct, but the PRD and AD-32 permit only `NoMatch`, `SingleCandidate`, and `MultipleCandidates` as `resolutionResult`; exclusion/fail-closed behavior belongs to response/component dimensions.

**Required correction:** revise the Journey 1 diagram to use the three canonical Resolution Results and express `Denied`, `Unavailable`, and excluded components through their proper response/evidence fields.

#### UX-ALIGN-4 — “Dry-run event” is ambiguous in the durable Audit Timeline — Moderate

The Audit Timeline component lists a `dry-run event` state, while PRD FR-21 and Architecture AD-26 define the durable audit set and keep intermediate/read-only diagnostic activity in telemetry. A dry-run or Preview must not silently become durable audit history unless it meets an explicitly approved security/audit event category.

**Required correction:** classify Preview/dry-run observations explicitly as telemetry or add an approved audit-category mapping; do not leave the channel ambiguous.

### Warnings

- The internal Projects UX is well aligned and architecturally supported, but the missing external Chatbot companion is a release-blocking UX completeness gap.
- The three internal terminology/channel issues should be corrected before UX is treated as a single canonical vocabulary source for implementation and test generation.

## Epic Quality Review

### Best-Practices Compliance by Epic

| Epic | User-value focus | Independence and sequencing | Story/AC quality | Assessment |
| --- | --- | --- | --- | --- |
| Epic 1 — Project Workspace Foundation | Mixed user and technical foundation value | Historical; stands alone | Mostly testable BDD, but contains several technical enablers | Historical quality debt; not production authority |
| Epic 2 — Context References | Clear workspace-reference value | Uses only Epic 1 and earlier external enablers | Clear scenarios and owner-boundary criteria | Pass as historical evidence |
| Epic 3 — Project Context Assembly | Clear Chatbot context value | Uses Epics 1–2 only | Strong allowlist, denial, and leakage criteria | Pass as historical evidence |
| Epic 4 — Project Resolution | Clear resolution value | Uses earlier reference capabilities only | Clear outcomes; historical confirmation criteria are superseded | Pass as historical evidence with supersession caveat |
| Epic 5 — Operational Console and Audit | Clear operator value | Uses earlier epics only | Strong UX scenarios, but historical export/maintenance semantics are superseded | Historical quality debt; not production authority |
| Epic 6 — Retrieve Authorized Project Truth | Clear Chatbot/operator read value | Prior-only; no dependency on Epic 7 or 8 for story completion | Specific BDD, denial/freshness cases, commands, evidence, estimates, completion boundaries | Structurally strong but externally blocked |
| Epic 7 — Complete Durable Project Decisions | Clear user/administrator workflow value | Depends only on Epic 6 and external entry gates | Strong shared invariants plus action-specific BDD and recovery coverage | Structurally strong but externally blocked |
| Epic 8 — Safe Operations and Release Decision | Mixes operator value and release-qualification work | Two prior-only tracks; no forward story dependency, but one epic contains two distinct outcomes | Operator stories are strong; later stories are predominantly technical/evidence/governance work | Major best-practice exception pending authorized acceptance |

### Dependency Analysis

- No forward story dependency was found in corrective Epics 6–8. Stories depend on prior stories, explicit external entry gates, or independently accepted prerequisite packages.
- Epic 6 does not require an Epic 7/8 capability to complete its read stories. Audit UI/CLI behavior is explicitly absent or unsupported until Story 8.1 rather than being claimed as part of Epic 6 completion.
- Epic 7 depends on the earlier Epic 6 read/contract cutover and external G-1/G-2 capabilities, not on future Epic 8 work.
- Epic 8 orders operator surfaces, packages, integrated evidence, performance/resilience, environment evidence, and the terminal decision without backward completion claims.
- The external packages are prerequisites rather than hidden forward dependencies. Their current absence still prevents independent implementation, as recorded below.
- No circular dependency was found.

### Story Quality and Acceptance Criteria

Corrective stories consistently provide:

- a named beneficiary and observable outcome;
- explicit FR/NFR/AD/journey/evidence traceability;
- Given/When/Then acceptance criteria;
- positive, denial/cross-Tenant, stale/unavailable, retry/replay, restart/concurrency/lost-response, compatibility, and rollback coverage where applicable;
- deterministic fixture and exact verification targets;
- evidence artifacts, estimate, and completion boundary.

No database-first or “create all tables up front” defect exists. Persistence/read models are introduced at the owning value slice, and the architecture uses EventStore plus incremental read models rather than an upfront relational schema.

The historical starter requirement is satisfied by Story 1.1, which defines the sibling-derived scaffold, dependency/configuration baseline, and CI setup. The corrective plan properly treats the current system as brownfield and includes shadow reads, compatibility, reconciliation, reversible cutover, and package-boundary migration.

### Critical Violations

#### EQ-C1 — Production stories are not independently executable while the entry chain is open

Story 6.1 is explicitly `blocked-external`. P1R, P0, P2, P3, authorized Solution Architect conformance sign-off, P4 clean-checkout acceptance, Story 6.1 specification readiness, and a superseding independent `READY` result remain prerequisites. G-1 through G-6 are open and block later Epic 7/8 stories. A well-written story that cannot start from accepted dependencies is not implementation-ready.

**Remediation:** accept the immutable prerequisite revisions and owner dispositions in the documented order, execute the clean-checkout gates, complete the Story 6.1 specification readiness check, and rerun this assessment. Do not move any production-authority story to `ready-for-dev` before the result is exactly `READY`.

#### EQ-C2 — Story 8.8 and terminal release depend on an absent external Chatbot package

Package 8.8-P3 has no owner-supplied immutable companion manifest or Projects pin. Story 8.8 therefore cannot complete independently from accepted inputs, and Story 8.11 cannot reach terminal acceptance.

**Remediation:** obtain independent Chatbot owner approval and evidence, then record the immutable Projects pin. Keep this outside Projects implementation authority.

### Major Issues

#### EQ-M1 — Epic 8 combines two materially different outcomes

Epic 8 contains an operator-value track (8.1–8.6) and a release-qualification track (8.7–8.11). The document explains the cohesion exception, but the second track is primarily package, evidence, performance, resilience, environment, and release-decision work rather than a cohesive operator capability. This deviates from the rule that one epic should deliver one user-centered outcome.

The exception is currently accepted only at the AI-assisted planning-contract level; the conformance checklist still has a pending authorized Solution Architect signature.

**Remediation:** either split the release-qualification track into a separate release-readiness epic/initiative while preserving stable evidence identities, or obtain the named Product Owner and authorized Solution Architect disposition on the exact current baseline. Reopen the split if either track gains an independent release cadence, unrelated scope, or a forward dependency.

#### EQ-M2 — Several current stories are technical/evidence milestones rather than user-value stories

Stories 8.7 through 8.11 primarily qualify packages, integrate evidence, prove performance/resilience, acquire environment evidence, and record a release decision. These are necessary because NFR-2 and NFR-11 are release-blocking, but they are not ordinary end-user stories.

**Remediation:** keep them explicitly typed as release/quality/governance work with Release Owner or operational beneficiaries, or move them under the separate qualification epic recommended by EQ-M1. Do not represent evidence production alone as product capability completion.

#### EQ-M3 — Some finding traceability uses non-stable labels

Current story traceability includes phrases such as `findings (audit)`, `findings (health)`, and `supply-chain` rather than always naming the exact P1/P2 stable finding identifiers promised by the story completion contract. The canonical matrix may carry the detailed rows, but the story text is not independently auditable from these labels.

**Remediation:** replace descriptive placeholders with exact canonical matrix row keys/stable finding IDs in every affected story and verify one-to-one mapping.

#### EQ-M4 — Superseded historical stories remain interleaved with production authority

Epics 1–5 correctly label themselves historical, and individual unsafe/outdated criteria such as folderless create and executed-but-failing E2E are marked superseded. Keeping these stories in the same live epic document still creates a material risk that an implementer follows obsolete acceptance criteria or mistakes historical coverage for release authority.

**Remediation:** retain the history but segregate it into an explicitly archived section/document or add machine-checkable guards that exclude historical stories from scheduling, story creation, and release evidence.

### Minor Concerns

#### EQ-m1 — Story 6.3 is at the upper bound of a cohesive story

Story 6.3 combines context retrieval, read-only refresh, and explanation across FR-16 through FR-18. The three behaviors share one assembly policy and are reasonably cohesive, but the story is large and has a broad evidence matrix.

**Remediation:** retain it only if the specification proves one bounded implementation/test slice; otherwise split along retrieval versus refresh/explanation without introducing forward dependencies.

### Overall Epic Quality Result

The corrective stories are structurally stronger than typical implementation plans: sequencing is prior-only, ACs are concrete and adversarial, storage is introduced by value slice, and traceability is comprehensive. However, open prerequisites make the backlog non-executable, the external Chatbot package is absent, and the Epic 8 mixed-outcome exception lacks final authorized sign-off. Epic/story quality therefore does not support implementation start yet.

## Summary and Recommendations

### Overall Readiness Status

**NOT READY**

The product requirement and FR-coverage layers are complete: 24 of 24 FRs and 11 of 11 NFRs are identified, and all 24 FRs have AC-bearing production owners in Epics 6–8. The selected PRD, addendum, architecture, UX, epics, and traceability matrix hashes exactly match the baseline recorded by the conformance checklist.

Implementation cannot begin because the production-authority entry chain is not accepted or executable. The current artifacts themselves say `NOT READY`, Story 6.1 remains `blocked-external`, the authorized Solution Architect signature is absent, prerequisite capabilities and clean-checkout gates are open, and no superseding independent assessment has returned exactly `READY`.

### Critical Issues Requiring Immediate Action

1. **Close the Story 6.1 prerequisite chain.** Accept 6.1-P1R, then P0 and P2, then P3 at immutable revisions with named-owner approval, validation evidence, and executable rollback.
2. **Obtain authorized same-baseline architecture conformance sign-off.** The checklist is prepared and its artifact hashes match the current selected baseline, but the Solution Architect identity/date remain pending. AI-assisted review cannot supply this signature.
3. **Pass 6.1-P4 from an accepted clean checkout.** The pinned module runner and evidence validator must be remotely restorable and the exact restore/run/full-test/down/validate sequence must pass with recorded artifacts and hashes.
4. **Pass Story 6.1 specification readiness and rerun implementation readiness.** Story 6.1 must not move to `ready-for-dev` until a new independent assessment returns exactly `READY`.
5. **Obtain the external Chatbot companion package.** The owner-approved immutable 8.8-P3 manifest and Projects pin are absent; this blocks Story 8.8, NFR-9/SM-5 evidence, and terminal release acceptance.
6. **Keep failed release evidence failed.** The current `release-smoke` evidence remains 19 passed and 56 failed. It requires a superseding authenticated passing run; it cannot be reclassified through planning changes.

### Recommended Next Steps

1. Have the authorized Solution Architect review and sign the exact current conformance baseline, including an explicit disposition of the Epic 8 cohesion exception.
2. Execute the documented `P1R → {P0, P2} → P3 → conformance sign-off → P4` dependency chain with immutable owner-approved revisions and rollback evidence.
3. Run the exact clean-checkout P4 commands and preserve command results, environment identity, artifact paths, and hashes in the canonical evidence record.
4. Produce and validate the complete Story 6.1 specification, including every entry-gate pin, negative path, fixture, command, expected artifact, and completion boundary.
5. Correct the UX canonical-language gaps: remove durable resolution-case/replay implications, use only the three Resolution Results, and resolve dry-run audit-versus-telemetry classification.
6. Replace vague story finding labels with stable canonical matrix row keys and either segregate historical Epics 1–5 or enforce their exclusion from scheduling mechanically.
7. Obtain the independently owned Chatbot companion UX/evidence manifest and record only its accepted immutable Projects pin.
8. Accept each later G-1–G-6 and 7.1/8.3/8.8/8.11 package before its consuming story begins; missing tools, environments, signatures, or owner dispositions remain blockers rather than skips.
9. Rerun implementation readiness only after Steps 1–4 are complete. Require the literal result `READY` before starting Story 6.1.

### Findings Summary

- Document discovery: all four required artifact types found; no whole-versus-sharded duplicate conflict.
- PRD: 24 FRs and 11 NFRs; product-requirement complete.
- Epic coverage: 24/24 FRs covered; no missing or extra FR identifiers.
- UX alignment: 4 findings, including the critical missing Chatbot companion package.
- Epic quality/readiness: 7 findings, including 2 critical, 4 major, and 1 minor concern.
- Total recorded findings: 11 across UX alignment and epic quality/readiness.

### Assessment Record

- Assessment date: 2026-08-02
- Assessor: Codex implementation-readiness workflow
- Selected baseline: PRD + addendum, Architecture Spine, `epics.md`, epic/architecture conformance checklist, and UX design specification
- Final disposition: `NOT READY`

### Final Note

This assessment identified 11 findings across two issue categories. The planning artifacts now provide complete FR coverage and a detailed remediation path, but they do not authorize implementation. Resolve the Story 6.1 entry chain and authorized sign-off first; then run a fresh independent readiness assessment. Production release remains separately blocked by the later evidence packages and Story 8.11 terminal acceptance from Jerome and John.
