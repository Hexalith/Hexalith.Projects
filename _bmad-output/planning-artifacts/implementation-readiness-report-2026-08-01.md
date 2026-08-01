---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
overallReadiness: NOT_READY
completedDate: 2026-08-01
assessor: Codex
inputDocuments:
  prd:
    - prds/prd-Hexalith.Projects-2026-05-24/prd.md
    - prds/prd-Hexalith.Projects-2026-05-24/addendum.md
  architecture:
    - architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md
  epics:
    - epics.md
  ux:
    - ux-design-specification.md
supportingDocuments:
  architecture:
    - architecture/architecture-projects-2026-07-15/reviews/reconcile-brownfield.md
    - architecture/architecture-projects-2026-07-15/reviews/reconcile-change-readiness.md
    - architecture/architecture-projects-2026-07-15/reviews/reconcile-prd.md
    - architecture/architecture-projects-2026-07-15/reviews/review-adversarial-incompatibility.md
    - architecture/architecture-projects-2026-07-15/reviews/review-rubric-walker.md
    - architecture/architecture-projects-2026-07-15/reviews/review-tech-current-reality.md
  epics:
    - epics-architecture-conformance-checklist-2026-07-16.md
  ux:
    - ux-design-directions.html
excludedDocuments:
  architecture:
    - architecture.md
  epics:
    - epics.md.pre-reconcile-2026-07-16.bak
---

# Implementation Readiness Assessment Report

**Date:** 2026-08-01
**Project:** projects

## Document Discovery

### Authoritative Inputs

- PRD: `prds/prd-Hexalith.Projects-2026-05-24/prd.md` with `addendum.md`
- Architecture: `architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md`
- Epics and stories: `epics.md`
- UX: `ux-design-specification.md`

### Supporting Inputs

- Six architecture reconciliation and review documents under the selected architecture set
- `epics-architecture-conformance-checklist-2026-07-16.md`
- `ux-design-directions.html`

### Discovery Resolution

- All required document types were found.
- No formal whole-plus-sharded (`index.md`) duplicate was found.
- The PRD folder has no shard manifest; `prd.md` and `addendum.md` were confirmed as authoritative.
- The newer architecture spine was confirmed as authoritative; root `architecture.md` was excluded as superseded.

## PRD Analysis

### Functional Requirements

#### FR-1: Create Project

Chatbot can admit Project creation as an idempotent Durable Task. A Project becomes caller-visible and Active only after exactly one authorized Project Folder is verified and bound. Realizes UJ-2.

Consequences:

- The only required user-authored field is Project name; canonical requests also carry a valid system-supplied Metadata Classification.
- A supplied Project Folder is authorized and verified. When none is supplied, Projects requests same-name Folder creation from Hexalith.Folders.
- Admission returns a pollable Durable Task rather than an immediately Active Project.
- Dependency denial, timeout, cancellation, duplicate delivery, lost response, or reconciliation never exposes an Active folderless Project.
- Equivalent Idempotency Key retries return the original task. A materially different request using the same scoped key returns an idempotency conflict.
- Terminal success exposes the Project identity only after Read-Model-Confirmed Completion.
- Historical unversioned name-only creation requests remain supported throughout v1; retirement requires an explicitly approved major version.
- Creation never duplicates transcripts, file contents, prompts, secrets, or Memory payloads.

#### FR-2: Open Project

Chatbot can open an authorized Project and receive the metadata, lifecycle state, Project Setup, and references needed to initialize a Conversation. Realizes UJ-1.

Consequences:

- Opening returns only data visible to the requesting Tenant and actor.
- Opening follows the Context Response State, Evidence Freshness State, and Recovery Action Code semantics in section 5.
- Pre-activation creation tasks are not exposed through Project open APIs.
- Archived or unavailable Projects are identified and cannot silently become active Conversation context.

#### FR-3: Update Project Setup

Chatbot can update Project Setup used for Conversation continuity.

Consequences:

- Updates are idempotent, durable, and observable from the authoritative read model.
- Setup may include goals, user-facing instructions, context preferences, source inclusion/exclusion policy, and Conversation-start defaults.
- Setup describes Conversation behavior and context policy, not model-provider internals.
- Updates remain additive and serialization-tolerant and reject secrets, unrestricted paths, and foreign payloads.

#### FR-4: Archive Project

An authorized Project User, Tenant Operator, or Tenant Project Administrator can archive an Active Project through server Preview, single-use confirmation, and an idempotent Durable Task. FR-23 defines the corresponding restore operation.

Consequences:

- Project Lifecycle State remains limited to Active and Archived.
- Confirmation is invalidated when actor authority or Project version changes.
- Archived Projects are excluded from Project Resolution unless explicitly requested.
- Completion is not reported until the read model confirms Archived.
- Existing references remain auditable after archival.

#### FR-5: List Projects

Authorized callers can list visible Active and Archived Projects.

Consequences:

- Results are Tenant-scoped, authorization-filtered, and filterable by Project Lifecycle State.
- Each result contains authorized Project identity, Project name, lifecycle state, current Project version, Project Folder availability, and the section 5 response/freshness/recovery metadata needed for selection without loading full Project Context.
- Pre-activation tasks never appear as Projects.
- Cursor pages default to 50 items and cap at 200; cursors remain scoped to the authenticated query.

#### FR-6: Link Conversation

An authorized Project User can link an existing Conversation to a Project. Realizes UJ-1 and UJ-3.

Consequences:

- A Conversation belongs to exactly one Project in v1.
- An explicitly actor-selected additive link uses an idempotent Durable Task without a second confirmation; an inferred link requires Preview and confirmation.
- Linking a Conversation already assigned elsewhere requires FR-7 rather than a second membership.
- Authorization failure prevents any protected resource access or durable effect.
- The link stores stable identity and metadata, never transcript content.

#### FR-7: Move Conversation Between Projects

An authorized Project User or Tenant Project Administrator can move a Conversation through Preview, single-use confirmation, and an idempotent Durable Task.

Consequences:

- Preview binds both Projects, the Conversation, actor, and current resource versions.
- Completion yields exactly one Project membership and a durable cross-context receipt.
- Failure, duplicate delivery, or lost response cannot leave two memberships silently valid.
- The move is audited using metadata only and fails closed when either Project or the Conversation cannot be authorized.

#### FR-8: Set Project Folder

An authorized Project User can set the single Project Folder; a Project User or Tenant Project Administrator can replace it through Preview and confirmation. Realizes UJ-2.

Consequences:

- Every Active Project has exactly one authorized Project Folder.
- Initial actor-selected binding is idempotent; inferred binding requires confirmation.
- Replacement binds old and new Folder evidence to the Confirmation Artifact and completes only after the authoritative read model confirms the replacement.
- Projects stores Folder identity and metadata, never file contents or unrestricted paths.
- Hexalith.Folders remains the authorization and system-of-record boundary.

#### FR-9: Link File Reference

An authorized Project User can link a File Reference without changing the Project Folder.

Consequences:

- File References are optional and do not replace the Project Folder.
- Actor-selected additive linking is idempotent; inferred linking requires confirmation.
- Projects stores stable File identity and metadata only; authorization remains delegated to Hexalith.Folders.

#### FR-10: Link Memory

An authorized Project User can link a Memory. Realizes UJ-1 and UJ-3.

Consequences:

- Actor-selected additive linking is idempotent; inferred linking requires confirmation.
- Projects stores stable Memory identity and metadata only.
- Authorization remains delegated to Hexalith.Memories.

#### FR-11: Unlink Context Reference

An authorized Project User or Tenant Project Administrator can unlink a Conversation, File Reference, or Memory through Preview, confirmation, and an idempotent Durable Task. The Project Folder can be replaced but not removed from an Active Project.

Consequences:

- Unlinking removes only the association and never deletes the underlying resource.
- Preview identifies the affected reference and current Project version.
- Completion is durable, audited using metadata only, and confirmed by the read model.
- The operation fails closed on stale authorization or resource evidence.

#### FR-12: Resolve Project From Conversation

Chatbot can request Candidate Projects for a Conversation with no explicit Project. Realizes UJ-3.

Consequences:

- The result is NoMatch, SingleCandidate, or MultipleCandidates with current Resolution Reason Codes.
- Only Active, read-model-confirmed Projects are considered by default.
- Pre-activation tasks and unauthorized or stale resources cannot become candidates.
- The response follows the section 5 contract; Unavailable and Denied never return a selected candidate.

#### FR-13: Resolve Project From Attachments

Chatbot can resolve Candidate Projects from an attached Project Folder or File References. Realizes UJ-2.

Consequences:

- Matching uses current authorized Folder/File identity and metadata, not file contents.
- Applicable candidates include ProjectFolderMatched or FileReferenceMatched reason codes.
- Missing, stale, or unavailable authorization evidence fails closed.

#### FR-14: Confirm Ambiguous Project

When resolution returns multiple candidates, Chatbot presents an accessible, unselected comparison and records the Project User's choice through a Confirmation Artifact and Durable Task. Realizes UJ-3.

Consequences:

- No candidate is silently or visually preselected.
- The artifact is bound to Tenant, actor, action, Conversation, candidates, normalized request, Preview, and current versions; it expires after 15 minutes and is single-use.
- Stale, expired, replayed, or tampered confirmation is rejected safely and requires a fresh Preview.
- Only Read-Model-Confirmed Completion creates or updates the Conversation association and audit history.
- Chatbot supports states for confirmation, cancellation, retry, expiry or staleness, lost-response recovery, and task status.

#### FR-15: Propose New Project

When no suitable Project exists, Chatbot can present a proposed Project and admit creation only after the Project User confirms a bound Preview. Realizes UJ-2.

Consequences:

- The proposal may suggest a Project name and setup metadata but creates nothing before confirmation.
- The Confirmation Artifact binds the initiating Conversation, authorized attachments, Folder plan, normalized request, and current evidence.
- Confirmed creation follows FR-1 and exposes no Project before Folder binding and read-model confirmation.
- Non-success outcomes follow the section 5 recovery contract; cancellation returns Cancelled, terminal failure returns Failed, and expired or stale evidence creates no task.

#### FR-16: Get Project Context

Chatbot can request Project Context for an Active Project. Realizes UJ-1 and UJ-4.

Consequences:

- Context is Tenant-scoped, actor-authorized, and available only for a read-model-confirmed Active Project with exactly one authorized Project Folder.
- It contains Project Setup and reference metadata, not payloads owned by other bounded contexts.
- It follows the section 5 contract, representing every excluded, stale, rebuilding, or unavailable reference as a metadata-only component; Denied discloses no protected detail.

#### FR-17: Explain Context Selection

Authorized callers can obtain current metadata explaining why a reference was included or excluded. Realizes UJ-4.

Consequences:

- Explanations are current Resolution Traces, not reconstructed history.
- Traces contain no secrets, payloads, prompts, unrestricted paths, raw upstream problems, or unconfirmed-candidate detail.
- Traces are request-scoped and not persisted; only confirmed outcomes enter audit history.

#### FR-18: Refresh Project Context

Chatbot can request a read-only refresh after links, setup, authorization, or resource availability changes.

Consequences:

- Refresh recomputes from current authorized Project, Conversation, Folder, File Reference, Memory, and version metadata.
- Refresh itself never mutates Project or reference state and creates no maintenance audit event.
- The refreshed response follows section 5, including new snapshot metadata, component evidence, recovery actions, and the binding transition rules for Partial, Unavailable, and Complete.

#### FR-19: Validate Project Setup

Projects validates setup and creation admission before accepting durable work.

Consequences:

- Project name remains the only required user-authored creation field.
- Canonical creation requests require valid system-supplied Metadata Classification; invalid classification is rejected before command submission.
- Validation permits a supplied authorized Project Folder or same-name Folder creation, but never defaults a caller-visible Project to Active before Folder completion.
- Validation rejects secrets, unrestricted paths, unsupported references, control/invisible characters where unsafe, and foreign payloads.
- Failures identify safe field/reason codes without echoing sensitive values.

#### FR-20: Retrieve Conversation-Start Setup

Chatbot can retrieve the subset of Project Setup needed to start or resume a Conversation.

Consequences:

- The result includes goals, user-facing instructions, context preferences, and default source policy.
- It excludes internal audit metadata and unavailable or unauthorized references.
- It is bound to one authorized projectVersion and asOf snapshot and follows section 5. Chatbot may admit the first response only for Complete or Partial; Unavailable or Denied blocks first-response admission and returns the applicable Recovery Action Codes without re-querying every bounded context.

#### FR-21: Record Project Audit Events

Projects records metadata-only audit events for consequential task admission and outcome, confirmed Project mutations, security-relevant confirmation outcomes, reconciliation, and Safe Diagnostic Export.

Consequences:

- Audit covers task admission and terminal outcome; confirmation use and cancellation; rejection of stale, replayed, or tampered confirmations; authorization denial; creation, archive, restore, move, relink, Folder replacement, unlink, confirmed resolution, and confirmed proposed creation; manual reconciliation; and Safe Diagnostic Export creation. Audit also records stable upstream receipt identifiers.
- Equivalent idempotent retries do not create duplicate audit events.
- Intermediate task states, polls, retries, dependency latency, notifications, unused expiry, and read-only Resolution Traces remain operational telemetry rather than durable audit.
- Audit contains Tenant, actor, Project/action identity, timestamp, safe reason/outcome codes, and affected reference identifiers, never payloads or secrets.

#### FR-22: Support Operator Read Access

Tenant Operators and Tenant Project Administrators can inspect authorized Project metadata, lifecycle state, references, Durable Task status, confirmed resolution outcomes, and audit metadata.

Consequences:

- Access is Tenant-scoped, action-authorized, and metadata-only across Web, CLI, and MCP.
- Project Users may inspect only their own permitted task status through Chatbot.
- Pre-activation tasks remain separate from Project list/open APIs; Tenant Operators and Tenant Project Administrators may inspect their safe status, and Tenant Project Administrators may perform authorized reconciliation.
- Read permission alone grants neither Safe Diagnostic Export nor a mutation.

#### FR-23: Restore Archived Project

An authorized Project User, Tenant Operator, or Tenant Project Administrator can restore an Archived Project through Preview, confirmation, and an idempotent Durable Task. This is the restore counterpart to FR-4 and realizes UJ-5.

Consequences:

- Preview verifies Tenant, actor, authority, current Project version, and exactly one authorized Project Folder.
- If the prior Folder is invalid or missing, Preview requires an authorized replacement or same-name Folder creation before confirmation.
- The Project remains Archived until Folder evidence and read-model-confirmed restore completion succeed.
- If Folder creation succeeds but activation cannot commit, the task enters NeedsAttention; Projects never automatically deletes a Folders-owned resource.
- Stale/unavailable evidence, replay, cancellation, duplicate delivery, concurrency, and lost response cannot expose an invalid Active Project.
- Completion and reconciliation outcomes are audited using metadata only.

#### FR-24: Create Safe Diagnostic Export

A separately authorized Tenant Operator or Tenant Project Administrator can create a bounded Safe Diagnostic Export through Web, CLI, or MCP.

Consequences:

- Export permission is distinct from FR-22 read permission; Chatbot cannot create exports.
- Every attempt and outcome is audited using metadata only.
- The complete encoded export, including envelope and truncation metadata, is at most 1 MiB and contains at most 500 reference rows and 100 audit rows.
- Reference ordering is stable and deterministic; audit rows are newest-first with stable tie-breaking.
- Truncation reports included/omitted counts and safe reasons without excluded detail; exports have no continuation cursor.
- Upstream unavailability is represented safely without raw errors or fabricated completeness.
- Projects never retains generated exports.

Total Functional Requirements: 24

### Non-Functional Requirements

#### NFR-1: Security and privacy

Every read, write, task, confirmation, audit event, and export is Tenant-, actor-, action-, target-, and current-version-scoped. Trust-bearing mutations fail closed when authorization evidence is stale, unknown, rebuilding, or unavailable. Logs, telemetry, errors, and evidence remain metadata-only.

#### NFR-2: Encryption and key management

Production traffic uses platform-approved authenticated encryption in transit. Durable Project, task, idempotency, and audit data uses platform-managed encryption at rest. Projects owns no private keys; approved platform KMS/secret-provider rotation and revocation evidence is release-blocking.

#### NFR-3: Availability and recovery

Authenticated metadata APIs and task admission target 99.9% monthly availability excluding planned maintenance. With required dependencies healthy, service RTO after process/node failure is 15 minutes, and accepted tasks resume or reach truthful NeedsAttention within 5 minutes.

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

Total Non-Functional Requirements: 11

### Additional Requirements

#### Product and release constraints

- v1 is an internal Hexalith.Chatbot platform module, not a standalone project-management product.
- No approved v1 FR or NFR is deferrable from production release. FR-1 through FR-20 and FR-23 are core user value; FR-21, FR-22, FR-24, and NFR-1 through NFR-11 are release-blocking safety and operations.
- Projects stores metadata and references only. Conversations, Folders, and Memories remain systems of record and authorization boundaries.
- A Conversation belongs to exactly one Project; every Active Project has exactly one authorized Project Folder; Project lifecycle remains exactly Active or Archived.
- Resolution is current recomputation. Candidate-score history, Resolution Traces, and generated diagnostic exports are not persisted.
- Consequential or inferred actions require server Preview and a tamper-evident, expiring, single-use Confirmation Artifact.
- Read-model confirmation, not acknowledgement or notification, is completion authority.

#### Durable workflow and platform ownership

- Architecture must define durable checkpoints, worker ownership, leases, restart recovery, two-instance convergence, duplicate delivery, lost responses, cancellation cut-off, terminal-state immutability, compensation, reconciliation, and cross-context receipts.
- Projects owns domain policy, Project contracts, and Project-specific Durable Task transitions.
- EventStore DomainService/platform owns hosting, event persistence/publication, subscriptions, read-model stores, cursors, health, telemetry, and reusable durable-workflow capability.
- The platform AppHost owns distributed topology; FrontComposer/platform hosts own Web, MCP, and CLI runtime composition.
- Production-capable hosts must use real identity, authorization, delegated service identity, and credentials; allow-all development stubs are forbidden.
- Folder creation recovery must never automatically delete a Folders-owned orphan or reserved resource.

#### Confirmation and idempotency

- Confirmation must bind Tenant, actor, action, targets, normalized request, Preview, and current resource versions, with signing/key ownership, 15-minute expiry, single-use enforcement, replay handling, and safe renewal.
- Idempotency scope is Tenant, actor, operation, and key. Request-equivalence canonicalization, conflict behavior, lost-response recovery, and retention for at least 30 days or the associated result lifetime—whichever is longer—must be defined.
- Unicode-safe canonicalization must preserve U+2028/U+2029 parity and must not broaden request equivalence.
- Cancellation is permitted before the irreversible commit point; after it, callers receive conflict or safe status.

#### Safe Diagnostic Export

- The versioned representation is projects.safe-diagnostic-export.v1 and must remain semantically consistent across Web, CLI, and MCP.
- The complete encoded response is capped at 1 MiB, 500 reference rows, and 100 audit rows, with at most two concurrent exports per Tenant.
- Ordering is deterministic; truncation reports counts and safe reasons; no continuation cursor or retained export is permitted.
- Unavailable components use safe markers, export authorization is separate, and every attempt/outcome is audited.

#### Contract and package boundaries

- Canonical Create Project uses the exact metadata-classification vocabulary public_metadata, tenant_sensitive, credential_sensitive, and secret. The authenticated adapter supplies the value from policy; users do not author it and Projects does not infer it from text.
- Authorization precedes protected parsing. Invalid canonical classification returns 400 ValidationFailure with rejectedField projectMetadata.metadataClass, echoes no rejected value, and invokes no command submitter.
- SensitiveMetadataTierValidator is shared by direct creation and proposal confirmation.
- Only the historical unversioned name-only shape receives v1 compatibility treatment.
- Hexalith.Projects.UI.Contracts is a Projects-owned, non-packable descriptor host depending on the UI-free Contracts kernel. It must not make the kernel depend on FrontComposer Shell, Fluxor, Fluent UI, or Microsoft.AspNetCore.App.
- Hexalith.Builds remains the single version owner for NSwag.MSBuild 14.7.1 and Fluxor.Blazor.Web 6.9.0; Projects uses versionless PackageReference entries and preserves central transitive pinning.

#### Migration and integration

- Legacy Active folderless Projects and in-flight Folder work must be inventoried and reconciled before list, resolution, or context exposure.
- Event evolution is additive; historical event readability is preserved and event history is never rewritten.
- Migration requires compatibility adapters, replay comparison, value-slice cutover, routing rollback, retirement evidence, safe archived-Project handling, and no unsafe dual writes.
- Sibling-repository changes require separately approved stories and verification.
- Chatbot owns presentation; Projects owns versioned Preview, Confirmation Artifact, Durable Task, Resolution, and Context contracts.
- Chatbot UX must provide unselected candidate comparison, confirm/cancel, expiry/staleness recovery, lost-response retry, task status, safe degraded/denied states, keyboard/focus behavior, live announcements, 200% zoom, and 320-CSS-pixel responsive behavior.

#### Verification and containment

- Deterministic small, median, and maximum fixtures must substantiate performance requirements.
- Required evidence includes authenticated persisted-boundary, cross-Tenant denial, authorization freshness, encryption/KMS, replay/tamper, privacy, metadata-only, restart, two-instance, duplicate-delivery, concurrency, cancellation, lost-response, compensation, reconciliation, read-model confirmation, Web/CLI/MCP parity, accessibility, deployment, smoke, rollback, compatibility, and stakeholder acceptance.
- Failed critical cases and unexplained critical skips block release; missing environments remain not verified.
- Unicode idempotency tests must prove real-server/generated-helper byte parity, rejection in identifier/envelope fields, deterministic descriptive-metadata escaping, non-collision, unaffected-hash stability, and a legacy-hash deployment gate where necessary.
- Live Playwright evidence separates deterministic no-AppHost checks from explicit live opt-in checks with dynamic projects-ui endpoint discovery and Aspire-managed teardown.
- The addendum records the 2026-07-17 readiness report as READY for corrective story-file creation and sprint reconciliation only. Production release and consequential autonomous MCP/proposal confirmation remain blocked until Story 8.11 terminal acceptance and its prerequisite gates pass.

### PRD Completeness Assessment

The PRD is structurally strong and unusually testable: it defines 24 stable FRs, 11 measurable NFRs, explicit roles, state vocabularies, invariants, recovery semantics, boundaries, success metrics, and release evidence. The addendum supplies implementation-routing and verification constraints without changing the observable product contract.

The principal completeness risk is not missing product requirements but downstream preservation: the architecture and epics must carry every consequence, quantitative NFR threshold, platform-ownership constraint, migration rule, compatibility path, live-evidence obligation, and release-containment gate into implementable stories with deterministic acceptance evidence.

## Epic Coverage Validation

### Epic FR Coverage Extracted

The epics document declares Epics 6–8 as the sole current production authority and maps every PRD FR to at least one AC-bearing production-owner story. Epics 1–5 are retained only as historical implementation evidence.

### Coverage Matrix

| FR | PRD requirement | Production epic/story coverage | Status |
| --- | --- | --- | --- |
| FR-1 | Chatbot can admit Project creation as an idempotent Durable Task; activation follows exactly-one-authorized-Folder verification and binding. | Epic 7, Story 7.1 | Covered |
| FR-2 | Chatbot can open an authorized Project and receive metadata, lifecycle, setup, and references needed to initialize a Conversation. | Epic 6, Story 6.1 | Covered |
| FR-3 | Chatbot can update Project Setup used for Conversation continuity. | Epic 7, Story 7.2 | Covered |
| FR-4 | Authorized users/operators can archive an Active Project through Preview, single-use confirmation, and an idempotent Durable Task. | Epic 7, Story 7.13 | Covered |
| FR-5 | Authorized callers can list visible Active and Archived Projects. | Epic 6, Story 6.1 | Covered |
| FR-6 | An authorized Project User can link an existing Conversation to a Project. | Epic 7, Story 7.3 | Covered |
| FR-7 | An authorized Project User or Tenant Project Administrator can move a Conversation through Preview, confirmation, and a Durable Task. | Epic 7, Story 7.4 | Covered |
| FR-8 | An authorized Project User can set the single Project Folder and replace it through Preview and confirmation. | Epic 7, Story 7.6; initial binding via 7.1 | Covered |
| FR-9 | An authorized Project User can link a File Reference without changing the Project Folder. | Epic 7, Story 7.7 | Covered |
| FR-10 | An authorized Project User can link a Memory. | Epic 7, Story 7.9 | Covered |
| FR-11 | An authorized Project User or Tenant Project Administrator can unlink a Conversation, File Reference, or Memory through Preview, confirmation, and a Durable Task. | Epic 7, Stories 7.5, 7.8, and 7.10 | Covered |
| FR-12 | Chatbot can request Candidate Projects for a Conversation with no explicit Project. | Epic 6, Story 6.4 | Covered |
| FR-13 | Chatbot can resolve Candidate Projects from an attached Project Folder or File References. | Epic 6, Story 6.4 | Covered |
| FR-14 | Chatbot presents accessible, unselected candidates and records the Project User's confirmed choice through a Confirmation Artifact and Durable Task. | Epic 7, Story 7.11; candidate reads via 6.4 | Covered |
| FR-15 | Chatbot can present a proposed Project and admit creation only after the Project User confirms a bound Preview. | Epic 7, Story 7.12 | Covered |
| FR-16 | Chatbot can request Project Context for an Active Project. | Epic 6, Story 6.3 | Covered |
| FR-17 | Authorized callers can obtain current metadata explaining why a reference was included or excluded. | Epic 6, Stories 6.3 and 6.4 | Covered |
| FR-18 | Chatbot can request a read-only refresh after links, setup, authorization, or resource availability changes. | Epic 6, Story 6.3 | Covered |
| FR-19 | Projects validates setup and creation admission before accepting durable work. | Epic 7, Story 7.1; contract cutover via 6.7 | Covered |
| FR-20 | Chatbot can retrieve the Project Setup subset needed to start or resume a Conversation. | Epic 6, Story 6.2 | Covered |
| FR-21 | Projects records metadata-only audit events for consequential admission/outcomes, confirmed mutations, security-relevant confirmations, reconciliation, and export. | Epic 8, Story 8.1 | Covered |
| FR-22 | Tenant Operators and Tenant Project Administrators can inspect authorized Project, task, resolution, and audit metadata. | Epic 6, Stories 6.5 and 6.6; Epic 8, Stories 8.1, 8.3, 8.4, and 8.5 | Covered |
| FR-23 | Authorized users/operators can restore an Archived Project through Preview, confirmation, and an idempotent Durable Task. | Epic 7, Story 7.14 | Covered |
| FR-24 | A separately authorized Tenant Operator or Tenant Project Administrator can create a bounded Safe Diagnostic Export through Web, CLI, or MCP. | Epic 8, Story 8.2 | Covered |

### Missing Requirements

No PRD Functional Requirement is absent from the epics coverage map.

No Functional Requirement appears in the epics document without a corresponding PRD FR.

### Coverage Statistics

- Total PRD FRs: 24
- FRs claimed covered in production-authority epics: 24
- Missing FRs: 0
- Extra epic FR IDs: 0
- Functional-requirement coverage: 100%

This result establishes traceability presence only. Whether the mapped epics and stories preserve the full requirement consequences, dependencies, and acceptance quality is assessed in later workflow steps.

## UX Alignment Assessment

### UX Document Status

Found: ux-design-specification.md is a complete 1,031-line operational UX specification. It defines a FrontComposer Web console plus CLI and MCP adapters over one metadata-only diagnostic model, with responsive and WCAG 2.2 AA requirements.

The UX document explicitly scopes direct end-user Chatbot presentation outside Hexalith.Projects. That separation is architecturally valid, but the PRD still makes the Chatbot companion journeys part of release acceptance.

### Confirmed Alignment

- The UX and PRD agree that Projects is a metadata control plane rather than a generic project-management product.
- Both preserve Conversations, Folders, Files, Memories, prompts, secrets, and other foreign payloads outside Projects-owned operational surfaces.
- Project inventory, detail, reference health, resolution traces, audit history, maintenance Preview/confirmation, and Safe Diagnostic Export correspond to FR-16 through FR-18 and FR-21 through FR-24.
- CLI, MCP, and Web use the same state, reason-code, warning, timestamp, audit, Tenant, and redaction semantics.
- Read-only MCP resources are separated from mutating tools, and no surface expands actor authority.
- The UX now uses the canonical Evidence Freshness State labels Current, Stale, Rebuilding, and Unavailable at the reference-health boundary.
- Responsive requirements cover 320–767, 768–1023, 1024+, and 1440+ layouts, with critical scope, warnings, reasons, and action consequences preserved.
- Accessibility requirements match NFR-9: keyboard use, visible focus, semantic structure, non-color-only status, sufficient contrast, assistive-technology support, reduced motion, 200% zoom, and 320 CSS-pixel reflow.

### Architecture Support

- AD-2, AD-16, AD-24, and AD-29 support a UI-free Contracts kernel, non-packable Projects.UI.Contracts descriptors, and platform-owned FrontComposer Web/CLI/MCP adapters.
- AD-19 defines one transport and task/recovery mapping for every surface.
- AD-21 fully specifies the bounded, separately authorized, synchronous, non-retained Safe Diagnostic Export.
- AD-27 supplies performance, pagination, back-pressure, timeout, and concurrency limits.
- AD-32 defines the complete response snapshot and recovery vocabulary used by list, open, resolution, context, Conversation-start, and recovery flows.
- AD-33 supplies one role/action authorization matrix for all surfaces.
- AD-34 makes accessible completion across operator and Chatbot journeys a release invariant.

No UX component requires an unsupported architectural ownership or runtime pattern. The architecture deliberately leaves exact information architecture and presentation styling to UX while fixing authority, semantics, contracts, accessibility, and platform boundaries.

### Alignment Issues

#### UX-ALIGN-1 — Final response and recovery contract is absent from UX

The UX specification does not define the PRD section 5 response fields responseState, asOf, projectVersion, resolutionResult, components, or recoveryActions. It also omits the exact Complete, Partial, Unavailable, and Denied consequences; first-response admission rules; and Recovery Action Codes None, Retry, RefreshContext, RequestPreview, RenewPreview, PollTask, ResolveNeedsAttention, SelectAlternative, and ContactAdministrator.

Impact: Web, CLI, MCP, or Chatbot presentation could improvise incompatible state or recovery behavior even though architecture AD-32 is precise.

Recommendation: Add a shared UX state model and journey variants that render every AD-32 response and recovery transition without creating surface-local synonyms.

#### UX-ALIGN-2 — Durable confirmation and task recovery states are underspecified

The UX uses generic Preview, dry-run, confirmation, executing, succeeded, and failed patterns but does not cover the final 15-minute opaque single-use Confirmation Artifact, stale/replayed/tampered renewal, equivalent lost-response retry, PollTask, WaitingForDependency, NeedsAttention, immutable terminal outcomes, or cancellation before versus after the irreversible checkpoint.

Impact: FR-4, FR-7, FR-8, FR-11, FR-14, FR-15, FR-23, NFR-4, and NFR-8 can be implemented correctly in the backend while remaining incomplete or misleading in presentation.

Recommendation: Add explicit Chatbot and operator task journeys derived from AD-4, AD-5, AD-13, and AD-19, including focus/live-region behavior for renewal, polling, intervention, cancellation conflict, and terminal states.

#### UX-ALIGN-3 — Safe Diagnostic Export lacks the final bounded UX contract

The UX defines a generic safe JSON/export component but omits separate export permission, the 1 MiB complete-response cap, 500-reference/100-audit row caps, two concurrent exports per Tenant, deterministic ordering, included/omitted counts, truncation reasons, safe unavailable-component markers, no continuation cursor, no retained bytes/task, and audit of every attempt/outcome.

Impact: FR-24 and NFR-6 could be technically enforced without the operator understanding truncation, incompleteness, authorization, or retry behavior.

Recommendation: Update the Safe Diagnostic Export component and Web/CLI/MCP flows to present the exact AD-21 snapshot, truncation, unavailable, authorization, concurrency, and audit semantics.

#### UX-ALIGN-4 — Chatbot companion UX is release-required but not an owned input

The selected UX document intentionally excludes direct end-user Chatbot presentation. The PRD nevertheless requires candidate comparison, proposed creation, confirmation/cancellation, expiry/staleness recovery, lost-response recovery, task status, first-response admission, and WCAG 2.2 AA evidence for Chatbot.

Architecture AD-2, AD-29, and AD-34 correctly assign this to the Chatbot presentation owner, and Story 8.8 requires a separately approved owner, pinned revision, and authenticated evidence. No corresponding Chatbot companion UX artifact appears in the confirmed document inventory.

Impact: NFR-9 and release acceptance remain unverifiable for the complete user journey even if the Projects operator UX passes.

Recommendation: Add or reference an approved, version-pinned Chatbot companion UX specification covering FR-14, FR-15, FR-20, AD-32, and AD-34 before Story 8.8 can close.

### Warnings

- G-3 remains an architecture entry gate: FrontComposer package-mode 4.0.0 versus checked-out source 4.0.1 requires an approved parity disposition and authenticated generated-adapter evidence.
- G-6 remains an architecture entry gate for Fluent UI RC4 and other prerelease/runtime bindings. Conceptual UX support exists, but implementation evidence cannot be claimed until these pins are approved and validated.
- The current UX document's exact freshness vocabulary is newer than the Architecture Spine date but aligns with AD-32. Any further vocabulary update must remain generated from Contracts rather than duplicated in presentation code.

## Epic Quality Review

### Review Scope

All eight epics and all story definitions in epics.md were reviewed for user value, independence, dependency direction, sizing, BDD acceptance quality, brownfield migration readiness, starter/scaffold treatment, data-store timing, and FR traceability.

Epics 6–8 are the sole production authority. Epics 1–5 were reviewed as historical evidence but are not used to authorize current production behavior.

### Critical Violations

#### EQ-C1 — The first production-authority story is explicitly blocked by unresolved enablement

Story 6.1 is blocked by work packages 6.1-P0, P1, P1R, P2, P3, and P4. P0 is blocked by P1R; P1R has an uncommitted target and pending acceptance-grade validation, executable rollback, accepted revision, and four-owner acceptance; P2, P3, and P4 remain open in sequence.

The shared verification commands also depend on the G-4 runner/evidence tool that the architecture and epics describe as a target capability rather than an available tool.

Violation: Story 6.1 cannot be independently completed or verified from currently accepted prior outputs. Since every later production story consumes the supported baseline and applicable entry gates, the production backlog is not ready for implementation even though its requirement mapping is complete.

Remediation: Finish and accept the prerequisite chain in its owning repositories, pin the accepted revisions and rollback evidence, record P4 acceptance, and mechanically prove that the exact verification commands run from a clean checkout before Story 6.1 returns to ready-for-development.

#### EQ-C2 — Story 8.4 has an explicit forward dependency on Story 8.5

Story 8.4 requires CLI semantics to be compared with Web Story 8.3 and MCP Story 8.5. Story 8.5 is later work, so Story 8.4 cannot satisfy its own parity acceptance criterion when reached in sequence.

Violation: Forward story dependency; Story 8.4 is not independently completable.

Remediation: Make Story 8.4 assert CLI conformance directly against the canonical generated contract and evidence fixture. Move the three-surface comparison to Story 8.5 after all three adapters exist, or to the later authenticated parity gate in Story 8.8.

#### EQ-C3 — Story 8.8 is epic-sized and depends on unmaterialized cross-repository work

Story 8.8 combines authenticated live topology, Web/CLI/MCP parity, cross-Tenant isolation, payload-leakage testing, operator WCAG evidence, three data shapes, and the separately owned Chatbot candidate/proposal/confirmation/recovery/first-response journeys. It is estimated XL and requires a separately approved Chatbot owner and pinned revision, but no corresponding Chatbot UX/evidence artifact is present in the confirmed planning inputs or expressed as an accepted prior work package.

Violation: The story spans multiple repositories, owners, test disciplines, and independently releasable outcomes. It cannot be completed independently from accepted prior work and is too large for one story.

Remediation: Split it into at least authenticated adapter parity/isolation/privacy, operator accessibility, and an externally owned Chatbot companion evidence package. Make their accepted artifacts prior dependencies of a small final integration/evidence story.

### Major Issues

#### EQ-M1 — Story 7.1 is an XL multi-capability slice

Story 7.1 combines authorization-before-parsing, exact Metadata Classification, shared validation, legacy request compatibility, ProjectId reservation, Folder validation/provisioning, durable workflow recovery, creation commit, and read-model activation.

Risk: Multiple independently failing contract, owner-integration, durability, migration, and projection seams make the story difficult to implement, review, and verify as one unit.

Remediation: Preserve one end-to-end user outcome but split delivery into prior contract/validator compatibility and Folder-provisioning/task admission enablers with executable contract evidence, followed by a bounded activation slice. Do not mark any partial slice as delivering FR-1.

#### EQ-M2 — Story 8.3 is broad and reevaluate is classified inconsistently

Story 8.3 combines the full operator Web console with archive, restore, relink, unlink, reevaluate, Preview/confirmation, recovery fields, safe failures, WCAG behavior, and Fluent governance. Story 8.4 then calls reevaluate a mutating CLI command while also requiring it to remain read-only; Story 8.3 similarly groups it with mutating maintenance actions before stating that it is read-only Refresh.

Risk: The broad surface can hide action-specific UX and authorization gaps, while contradictory action classification can produce incompatible Web/CLI/MCP contracts.

Remediation: Define reevaluate once as the read-only RefreshContext action from FR-18/AD-32, remove it from mutating command lists, and split or explicitly matrix each actual mutation against Preview, confirmation, task, denial, stale, and recovery behavior.

#### EQ-M3 — Story 8.11 is a release gate packaged as a large delivery story

Story 8.11 spans deployment, environment identification, encryption/KMS rotation and revocation, health/smoke evidence, rollback drill, residual-risk dispositions, matrix validation, and two-owner terminal acceptance.

Risk: Evidence acquisition and the final go/no-go decision have different owners and completion mechanics. Treating the whole gate as one story obscures which prerequisite evidence is incomplete.

Remediation: Keep Story 8.11 as a small terminal release decision/gate that consumes prior accepted evidence. Move deployment/KMS/smoke/rollback acquisition into explicit earlier evidence stories or work packages with their own owners and commands.

#### EQ-M4 — Epics frontmatter contains stale workflow state

The frontmatter still says the backlog awaits an independent readiness rerun returning READY, while the PRD addendum records the 2026-07-17 rerun as READY and the epic body has since been amended with the 2026-08-01 P1R correction.

Risk: Automation and reviewers cannot tell whether the historical readiness gate is satisfied or whether the current P1R changes require a new superseding gate.

Remediation: Reconcile frontmatter to distinguish the completed 2026-07-17 planning gate from the current open P0/P1R/P2/P3/P4 implementation entry gates and this 2026-08-01 reassessment.

### Minor Concerns

#### EQ-m1 — Historical technical enablers are retained as user stories

Historical Stories 1.1, 1.2, 1.3, 1.5, 1.9, 2.1, 2.2, and 2.6 are primarily scaffolding, contracts, test harnesses, infrastructure, ACL enablement, or decision spikes rather than independently consumable user outcomes.

They are clearly marked as history or enablers and do not authorize production, so this is not a current implementation blocker. Preserve the distinction and do not copy this structure into new production stories.

#### EQ-m2 — Historical Standalone labels are misleading after supersession

Epics 1–5 retain Standalone: Yes language even though several of their key behaviors are explicitly superseded for production and the document later clarifies that standalone means historical feature evidence only.

Recommendation: Rename the label to Historical standalone evidence or remove it to prevent accidental release interpretation.

### Passed Quality Checks

- Epics 6, 7, and 8 each state a recognizable beneficiary and observable outcome rather than being pure layer-by-layer implementation phases.
- Production epic dependency direction is otherwise correct: Epic 6 reads precede Epic 7 durable writes, which precede Epic 8 operational/release evidence.
- No circular epic dependency was found.
- Within Epic 7, link, move, unlink, Folder, File, Memory, confirmation, archive, restore, and reconciliation stories are ordered so their referenced story capabilities are prior work.
- Acceptance criteria are predominantly specific Given/When/Then scenarios with denial, stale/unavailable, retry, duplicate, restart, concurrency, compatibility, rollback, or evidence behavior where applicable.
- All production stories contain traceability, an estimate, a completion boundary, verification intent, and expected evidence.
- Brownfield migration and compatibility are explicitly covered by Stories 6.7, 7.15, and 8.7; no event-history rewrite or unsafe dual writer is authorized.
- No up-front relational table/database mega-story exists. Read models, indexes, durable records, and storage behavior are introduced with the capabilities that use them.
- The historical scaffold story appears early and matches the then-selected hybrid scaffold; the current brownfield plan correctly focuses on migration to the target package/runtime boundary.
- FR traceability remains complete at 24 of 24.

### Epic-Level Compliance Summary

| Epic | User value | Independence | Story sizing | Forward dependencies | AC quality | Current disposition |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Mixed with technical foundation | Historical only | Several technical enablers | None material | Generally testable | Historical evidence |
| 2 | Context-reference value | Uses Epic 1 and external owners | Two enablers/spike | Prior dependencies only | Generally testable | Historical evidence |
| 3 | Clear Chatbot context value | Uses Epics 1–2 | Cohesive | Prior dependencies only | Strong | Historical evidence |
| 4 | Clear resolution value | Uses earlier reference capabilities | Cohesive | Prior dependencies only | Strong | Historical evidence |
| 5 | Clear operator value | Uses Epics 1–4 | Broad but historical | Prior dependencies only | Generally strong | Historical evidence |
| 6 | Clear read value | Blocked by open external packages/gates | Mostly reasonable | No future story; unresolved external chain | Strong but currently non-executable | Not implementation-ready |
| 7 | Clear durable-decision value | Uses Epic 6 and G-1/G-2 | Story 7.1 is XL | Prior dependencies only | Strong | Blocked by entry gates |
| 8 | Clear operator/release value | Uses Epics 6–7 and entry fixtures | Stories 8.8 and 8.11 are oversized | Story 8.4 depends on 8.5 | Mixed due scope/contradiction | Structurally requires correction |

## Summary and Recommendations

### Overall Readiness Status

**NOT READY**

The product requirements are complete and traceable: all required artifacts exist, the PRD contains 24 FRs and 11 NFRs, all 24 FRs have production-story mappings, and the Architecture Spine provides strong coverage of domain, platform, security, recovery, compatibility, UX, and release-evidence obligations.

Implementation readiness nevertheless fails. Story 6.1—the first production-authority story—is explicitly blocked by unresolved and partly uncommitted prerequisite work; its verification tooling is still a target capability. The production backlog also contains a forbidden forward dependency, oversized cross-repository stories, stale workflow state, and missing release-binding UX inputs.

This verdict blocks starting the production-authority implementation sequence. It does not prohibit the planning corrections and separately authorized prerequisite work required to remove the blockers. Production release remains independently blocked by Story 8.11 even after implementation readiness is restored.

### Critical Issues Requiring Immediate Action

1. **Close the production entry-gate chain.** Accept and pin 6.1-P0, P1R, P2, P3, and P4 and every applicable G-1 through G-6 capability with owner-approved revisions, clean-checkout commands, executable rollback, and machine-checkable evidence. Story 6.1 must remain blocked until this is complete.
2. **Remove the Story 8.4 forward dependency.** Story 8.4 cannot require future Story 8.5 for completion. Move three-surface parity to 8.5 or 8.8 and make 8.4 independently validate against the canonical contract.
3. **Split and materialize Story 8.8 prerequisites.** Separate adapter parity/isolation/privacy, operator accessibility, and the externally owned Chatbot companion work. Add an approved Chatbot UX/evidence artifact with owner, pinned revision, commands, and expected evidence before the final integration story.
4. **Update the UX contract to the final PRD/architecture vocabulary.** Add AD-32 response/recovery states, AD-4/AD-5/AD-13 task and Confirmation Artifact journeys, the complete AD-21 export behavior, and the end-user Chatbot companion flows.
5. **Correct oversized and contradictory story scope.** Reclassify reevaluate as read-only RefreshContext across Web/CLI/MCP, decompose Story 7.1's enablement seams, narrow Story 8.3, and keep Story 8.11 as a terminal decision consuming previously acquired evidence.
6. **Reconcile artifact state.** Update epics frontmatter to distinguish the completed 2026-07-17 planning gate from the new 2026-08-01 P1R and readiness state, then synchronize the evidence matrix and any sprint/story status that relies on it.

### Recommended Next Steps

1. Route P0/P1R/P2/P3/P4 and G-1 through G-6 to their named repository owners; record accepted revisions, qualification results, rollback commands, and four-owner acceptance where required.
2. Revise epics.md to remove the Story 8.4 forward dependency, split Story 8.8, correct reevaluate classification, and narrow Stories 7.1, 8.3, and 8.11.
3. Update ux-design-specification.md and add the separately owned, version-pinned Chatbot companion UX/evidence input.
4. Reconcile epics frontmatter and the canonical implementation-readiness traceability matrix against the corrected artifacts and current prerequisite state.
5. Re-run this implementation-readiness workflow. Return READY only when the first production story is independently executable and its exact validation commands work from an accepted clean-checkout baseline.

### Issue Summary

- UX alignment issues: 4
- Epic-quality issues: 9
  - Critical: 3
  - Major: 4
  - Minor: 2
- Total issues requiring attention: 13
- Functional-requirement coverage gaps: 0
- Missing required artifact types: 0

### Final Note

The planning set has a strong requirements and architecture core, but traceability alone is not implementation readiness. The open prerequisite chain and structural story defects are concrete stop conditions. Correct them before Phase 4 implementation begins.

**Assessment date:** 2026-08-01  
**Assessor:** Codex, Implementation Readiness Product Management Review
