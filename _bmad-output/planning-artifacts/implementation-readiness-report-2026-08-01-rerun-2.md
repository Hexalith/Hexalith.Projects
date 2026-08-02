---
stepsCompleted:
    - step-01-document-discovery
    - step-02-prd-analysis
    - step-03-epic-coverage-validation
    - step-04-ux-alignment
    - step-05-epic-quality-review
    - step-06-final-assessment
overallStatus: NOT_READY
assessmentDate: 2026-08-01
assessor: Codex
issueSummary:
    critical: 4
    major: 6
    minor: 3
    total: 13
includedFiles:
    prd:
        - _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md
        - _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/addendum.md
    architecture:
        - _bmad-output/planning-artifacts/architecture.md
    epics:
        - _bmad-output/planning-artifacts/epics.md
    ux:
        - _bmad-output/planning-artifacts/ux-design-specification.md
    supportingEvidence:
        - _bmad-output/planning-artifacts/epics-architecture-conformance-checklist-2026-07-16.md
excludedFiles:
    - _bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md
    - _bmad-output/planning-artifacts/ux-design-directions.html
    - logs, reviews, reconciliation records, backups, and prior readiness reports
---

# Implementation Readiness Assessment Report

**Date:** 2026-08-01
**Project:** projects

## Document Discovery

### Selected Assessment Documents

**PRD**

- `prds/prd-Hexalith.Projects-2026-05-24/prd.md` — 46,095 bytes; modified 2026-07-15
- `prds/prd-Hexalith.Projects-2026-05-24/addendum.md` — 24,867 bytes; modified 2026-07-20

**Architecture**

- `architecture.md` — 51,607 bytes; modified 2026-07-16

**Epics and Stories**

- `epics.md` — 186,007 bytes; modified 2026-08-01

**UX Design**

- `ux-design-specification.md` — 62,694 bytes; modified 2026-08-01

**Supporting Evidence**

- `epics-architecture-conformance-checklist-2026-07-16.md` — 20,715 bytes; modified 2026-08-01

### Discovery Resolution

- The nonstandard PRD bundle was accepted with `prd.md` and `addendum.md` as its canonical assessment inputs.
- `architecture.md` was selected as the canonical architecture document.
- `ARCHITECTURE-SPINE.md`, logs, editorial reviews, reconciliation records, backups, HTML directions, and prior readiness reports were excluded.
- The architecture-conformance checklist was classified as supporting evidence rather than a competing architecture or epics specification.

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

- **Release cut:** FR-1 through FR-20 and FR-23 are core user value. FR-21, FR-22, FR-24, and NFR-1 through NFR-11 are release-blocking safety and operations. No approved v1 FR or NFR is deferrable from production release.
- **Lifecycle separation:** Project lifecycle is exactly `Active` or `Archived`; task states are `Pending`, `Running`, `WaitingForDependency`, `NeedsAttention`, `Succeeded`, `Rejected`, `Failed`, or `Cancelled`, with the final four terminal and `NeedsAttention` recoverable/nonterminal.
- **Folder invariant:** No Project is caller-visible or Active before exactly one authorized Project Folder is bound and authoritative read-model completion is confirmed.
- **Conversation membership:** A Conversation belongs to exactly one Project in v1.
- **Authority:** Surface or role choice never expands authority. Service/workflow callers operate only with delegated actor authority; production hosts must not use allow-all identity or authorization stubs.
- **Bounded-context ownership:** Conversations, Folders, and Memories remain systems of record. Projects stores stable identities and metadata only, never transcripts, file contents, Memory payloads, prompts, secrets, unrestricted paths, raw upstream problems, or foreign payloads.
- **Observable response contract:** Open, list, resolution, context, Conversation-start, and proposal recovery preserve `responseState`, `asOf`, `projectVersion`, applicable `resolutionResult`, metadata-only `components`, and `recoveryActions` semantics. `Complete`, `Partial`, `Unavailable`, and `Denied` have binding admission and disclosure consequences.
- **Fresh recomputation:** Refresh and resolution recompute current authorization and evidence; they do not rewrite earlier responses or persist candidate scores/Resolution Traces. Only confirmed outcomes enter durable audit.
- **Completion authority:** Read-model confirmation—not request acknowledgement or SignalR notification—establishes consequential completion.
- **Durable workflow design:** Architecture must define durable checkpoints, worker ownership, leases, restart recovery, two-instance convergence, duplicate delivery, lost-response recovery, cancellation cut-off, terminal-state immutability, receipts, compensation, reconciliation, irreversible commit points, and orphan/reserved Folder recovery without deleting Folders-owned resources automatically.
- **Platform ownership:** Projects owns domain policy, contracts, and Project-specific task transitions. EventStore DomainService/platform owns hosting, event persistence/publication, subscriptions, read-model stores, cursors, health, telemetry, and reusable durable workflow capability. Platform AppHost owns topology; FrontComposer/platform hosts own Web, MCP, and CLI composition.
- **Confirmation:** Architecture/contracts must define schema, signing/key ownership, normalized request material, current-resource binding, 15-minute expiry, single-use enforcement, replay/tamper behavior, and safe renewal.
- **Idempotency:** Scope is `(Tenant, actor, operation, key)` with Unicode-safe equivalence canonicalization, conflict handling, lost-response recovery, and retention of at least 30 days and never less than the associated result lifetime. U+2028/U+2029 identifier/envelope inputs are rejected, while accepted descriptive metadata retains deterministic escaping and byte parity.
- **Safe Diagnostic Export:** Versioned representation `projects.safe-diagnostic-export.v1`; complete encoded size at most 1 MiB; at most 500 references and 100 audit rows; at most two concurrent exports per Tenant; deterministic ordering; safe truncation counts/reasons; no cursor; no retained generated export; separate authorization and metadata-only audit.
- **Metadata Classification:** Canonical Create Project uses exactly `public_metadata`, `tenant_sensitive`, `credential_sensitive`, or `secret`, supplied by the authenticated integration adapter rather than the user or inferred text. Invalid canonical values fail with `400 ValidationFailure`, `details.rejectedField = projectMetadata.metadataClass`, no echoed rejected value, and no command submission. Historical unversioned name-only creation retains v1 compatibility.
- **UI contract boundary:** `Hexalith.Projects.UI.Contracts` is a Projects-owned, non-packable descriptor host depending on the UI-free Contracts kernel. The kernel must not depend on FrontComposer Shell, Fluxor, Fluent UI, or `Microsoft.AspNetCore.App`; MCP and CLI remain independent.
- **Central package ownership:** `Hexalith.Builds` owns `NSwag.MSBuild` `14.7.1` and `Fluxor.Blazor.Web` `6.9.0`; Projects consumes versionless references, preserves central transitive pinning, and must not restore local pins or opportunistically upgrade them.
- **Migration:** Inventory and reconcile legacy Active folderless Projects and in-flight Folder work before list/resolution/context visibility; evolve events additively; preserve historical readability; use compatibility adapters, replay comparison, value-slice cutover and rollback; avoid event-history rewrite and unsafe dual writes.
- **Chatbot companion:** Candidate comparison has no preselection and exposes explicit confirm/cancel, expiry/staleness recovery, lost-response retry, truthful task status, safe degraded/denied states, keyboard/focus behavior, live announcements, 200% zoom, 320 CSS-pixel responsiveness, and authenticated screen-reader evidence.
- **Verification:** Deterministic small/median/maximum fixtures; authenticated persisted-boundary, cross-Tenant denial, freshness, KMS, replay/tamper, privacy, restart, convergence, duplicate, concurrency, cancellation, lost-response, compensation, reconciliation, read-model, Web/CLI/MCP parity, accessibility, performance, deployment, smoke, rollback, compatibility, and stakeholder evidence are required. Critical failures or unexplained critical skips block release; absent environments are not verified.
- **Live Playwright:** Keep deterministic no-AppHost and explicit live opt-in lanes separate; discover the ready `projects-ui` endpoint, reject guessed/invalid URLs, use Aspire-managed teardown, and make retained skips name concrete unmet prerequisites.
- **Repository boundaries:** Work required in EventStore, FrontComposer, Conversations, Folders, or Chatbot needs its own approved story and verification; the Projects PRD does not authorize sibling-repository changes implicitly.
- **Current containment stated by the addendum:** The 2026-07-17 readiness rerun authorized corrective story creation/sprint reconciliation, but production release and consequential autonomous MCP/proposal confirmation remain blocked until Story 8.11 terminal evidence and dated Jerome/John disposition.

### PRD Completeness Assessment

The selected PRD is structurally complete and unusually testable: it has 24 stable FRs, 11 stable NFRs, quantified limits, explicit authority and data boundaries, observable failure/recovery states, release classification, measurable success criteria, and a detailed implementation/evidence addendum. It states that no phase-blocking product questions remain.

Two traceability risks require attention in later validation. First, the addendum treats an “Architecture Spine” as preserved authority while document discovery selected `architecture.md` and excluded the unindexed `ARCHITECTURE-SPINE.md`; architecture validation must determine whether the selected file incorporates that authority. Second, the addendum's evidence index stops at July 17 while the selected epics, UX specification, and supporting checklist were modified on August 1; later steps must verify that their changes preserve the final FR-1–FR-24 and NFR-1–NFR-11 baseline and do not rely on superseded readiness evidence.

## Epic Coverage Validation

### Epic FR Coverage Extracted

The epics document declares Epics 6–8 as the current production-authority plan and Epics 1–5 as completed implementation history only. Its explicit coverage map assigns every final PRD FR to at least one AC-bearing production story.

### Coverage Matrix

| FR | PRD Requirement | Production Epic/Story Coverage | Status |
| --- | --- | --- | --- |
| FR-1 | Create Project | Epic 7, Story 7.1 | ✓ Covered |
| FR-2 | Open Project | Epic 6, Story 6.1 | ✓ Covered |
| FR-3 | Update Project Setup | Epic 7, Story 7.2 | ✓ Covered |
| FR-4 | Archive Project | Epic 7, Story 7.13 | ✓ Covered |
| FR-5 | List Projects | Epic 6, Story 6.1 | ✓ Covered |
| FR-6 | Link Conversation | Epic 7, Story 7.3 | ✓ Covered |
| FR-7 | Move Conversation Between Projects | Epic 7, Story 7.4 | ✓ Covered |
| FR-8 | Set/Replace Project Folder | Epic 7, Story 7.6, with initial binding through Story 7.1 | ✓ Covered |
| FR-9 | Link File Reference | Epic 7, Story 7.7 | ✓ Covered |
| FR-10 | Link Memory | Epic 7, Story 7.9 | ✓ Covered |
| FR-11 | Unlink Context Reference | Epic 7, Stories 7.5, 7.8, and 7.10; Folder replace-only constraint in 7.6 | ✓ Covered |
| FR-12 | Resolve Project From Conversation | Epic 6, Story 6.4 | ✓ Covered |
| FR-13 | Resolve Project From Attachments | Epic 6, Story 6.4 | ✓ Covered |
| FR-14 | Confirm Ambiguous Project | Epic 7, Story 7.11, using candidate reads from 6.4 | ✓ Covered |
| FR-15 | Propose New Project | Epic 7, Story 7.12 | ✓ Covered |
| FR-16 | Get Project Context | Epic 6, Story 6.3 | ✓ Covered |
| FR-17 | Explain Context Selection | Epic 6, Stories 6.3 and 6.4 | ✓ Covered |
| FR-18 | Refresh Project Context | Epic 6, Story 6.3 | ✓ Covered |
| FR-19 | Validate Project Setup and Metadata Classification | Epic 7, Story 7.1, with contract cutover support in 6.7 | ✓ Covered |
| FR-20 | Retrieve Conversation-Start Setup | Epic 6, Story 6.2 | ✓ Covered |
| FR-21 | Record Project Audit Events | Epic 8, Story 8.1 | ✓ Covered |
| FR-22 | Support Operator Read Access | Epic 6, Stories 6.5 and 6.6; Epic 8, Stories 8.1, 8.3, 8.4, and 8.5 | ✓ Covered |
| FR-23 | Restore Archived Project | Epic 7, Story 7.14 | ✓ Covered |
| FR-24 | Create Safe Diagnostic Export | Epic 8, Story 8.2 | ✓ Covered |

### Missing Requirements

No PRD Functional Requirement is absent from the production-authority coverage map. No FR identifier appears in the epics document that is outside the PRD's canonical FR-1 through FR-24 set.

### Coverage Statistics

- Total PRD FRs: 24
- FRs covered in production-authority epics: 24
- Missing FRs: 0
- Extra epic-only FR identifiers: 0
- Coverage: 100%

This result establishes requirement-to-story presence only. It does not establish story quality, implementability, prerequisite readiness, or release readiness; those concerns are assessed in later workflow steps.

## UX Alignment Assessment

### UX Document Status

**Found:** `_bmad-output/planning-artifacts/ux-design-specification.md`

The UX specification is marked complete, was updated on 2026-08-01, and explicitly incorporates the final PRD/addendum, the July Architecture Spine, the 2026-08-01 readiness report, and the approved readiness correction. It defines the Projects-owned operational experience for Web, CLI, and MCP plus the required externally owned Chatbot companion release input.

### UX ↔ PRD Alignment

The UX specification is substantially aligned with the final PRD:

- It preserves the module boundary: Projects provides metadata-only operational surfaces, while end-user presentation remains owned by Chatbot.
- It maps the PRD's Project Resolution, Project Context, operator-read, maintenance, audit, recovery, and Safe Diagnostic Export journeys into concrete operational flows.
- It preserves `Complete`, `Partial`, `Unavailable`, and `Denied`; `Current`, `Stale`, `Rebuilding`, and `Unavailable`; `NoMatch`, `SingleCandidate`, and `MultipleCandidates`; and the canonical Recovery Action Codes.
- It treats `RefreshContext` as synchronous, read-only recomputation with no Confirmation Artifact, Durable Task, lifecycle mutation, or maintenance audit.
- It models Preview, the 15-minute single-use Confirmation Artifact, the pollable Durable Task, lost-response recovery, cancellation around the irreversible checkpoint, all eight task states, immutable terminal states, and read-model-confirmed completion.
- It preserves the exactly-one-Folder invariant and metadata-only payload boundary.
- It carries FR-24's separate export authorization, 1 MiB/500-reference/100-audit-row limits, deterministic ordering, two-export Tenant concurrency limit, safe omission markers, auditing, and no-retention rule.
- It covers NFR-9 with keyboard, focus, assistive-technology announcements, non-color semantics, 200% zoom, 320 CSS-pixel behavior, automated checks, and authenticated manual evidence.
- It explicitly makes the separately owned, version-pinned Chatbot companion UX/evidence package a blocking release input.

### UX ↔ PRD Alignment Issues

1. **Canonical resolution vocabulary drift — Moderate.** The normative shared-state section correctly restricts `resolutionResult` to `NoMatch`, `SingleCandidate`, or `MultipleCandidates`, but the Resolution Trace component and Journey 1 also use presentation states such as `Resolved`, `Excluded`, and `FailedClosed`. Those may be display outcomes, but the document's “no local synonyms” rule does not distinguish them from canonical contract values. The UX contract should explicitly map them to `resolutionResult`, response state, and component evidence or remove the synonyms.

2. **“Replay” terminology conflicts with current recomputation — Moderate.** Several early UX passages offer “re-run or replay” of resolution diagnostics. The PRD forbids persisted candidate-score/Resolution Trace history and defines current recomputation only. Later UX sections correctly define `RefreshContext` and current transient traces. Replace “replay” where it implies historical inference reconstruction with “fresh recomputation,” unless a separate bounded deterministic-fixture diagnostic is explicitly meant.

3. **Role/action authorization matrix is insufficiently explicit — High.** The PRD distinguishes Project User, Tenant Operator, Tenant Project Administrator, and delegated Service/Workflow Caller. The UX frequently speaks generically of “administrators” or “operators” performing archive, restore, relink, and unlink. It must bind each visible action and confirmation path to the PRD role matrix so a Tenant Operator is not shown administrator-only relink/unlink authority and MCP/service callers never gain autonomous end-user confirmation.

### UX ↔ Architecture Alignment

The selected architecture file cannot validate current UX support.

**Critical finding:** `_bmad-output/planning-artifacts/architecture.md` declares `status: superseded`, says it is “not normative,” and identifies `_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md` as the sole current architecture. That current spine was explicitly excluded during document discovery, while both the UX specification and current epics use it as an input. Consequently, this assessment can only compare UX against historical architecture, not validate current architecture alignment.

Concrete gaps in the selected historical architecture are:

- It validates 22 FRs, not the final 24, and has no normative architecture coverage for FR-23 Restore Archived Project or FR-24 Safe Diagnostic Export.
- It uses an older broad NFR inventory rather than NFR-1 through NFR-11 and does not architect the final quantitative KMS, recovery, durability, scale, pagination, back-pressure, retention, accessibility, compatibility, and release-evidence envelopes.
- It permits historical folderless-active creation and a simple `202 AcceptedCommand`/five-state UI lifecycle, conflicting with Folder-first hidden activation, Preview/Confirmation, Durable Task truth, all eight task statuses, recovery actions, irreversible-checkpoint cancellation, and read-model-confirmed completion.
- It lacks the final bounded Safe Diagnostic Export schema and concurrency/retention contract.
- It does not establish the final `Complete|Partial|Unavailable|Denied` response model, complete component evidence semantics, or Chatbot first-response admission rules.
- Its accessibility statement is limited to WCAG 2.2 AA and axe/Playwright; it does not establish the final authenticated manual keyboard/screen-reader, 200% zoom, 320 CSS-pixel, focus-restoration, and live-region evidence contract.
- It does not define the separately owned, immutable, version-pinned Chatbot companion artifact as a release-blocking input.
- Its project/runtime ownership model includes Projects-owned UI/MCP/CLI/AppHost/Aspire/ServiceDefaults hosts, while the final PRD addendum and workspace baseline route reusable runtime/presentation composition to platform owners and require the non-packable `Hexalith.Projects.UI.Contracts` descriptor boundary.
- Its optional `ProjectResolutionTraceProjection` language creates ambiguity against the final non-persistence rule for Resolution Traces.

### Warnings

- A current UX-to-architecture alignment verdict requires either selecting the Architecture Spine as an assessment input or proving that a current canonical architecture has replaced it. The selected historical file is not sufficient implementation authority.
- The required Chatbot companion UX/evidence input is specified but not present in the selected document inventory; the epics mark package 8.8-P3 open and release-blocking.
- The UX HTML directions artifact was excluded and is correctly treated as non-normative; implementation must follow the Markdown UX contract and Fluent/FrontComposer governance rather than prototype markup.

### UX Alignment Disposition

**PRD alignment:** Substantially aligned, with three correctable vocabulary/authority clarifications.

**Architecture alignment:** **Not verifiable from the selected architecture and therefore not implementation-ready.** The critical cause is explicit use of a superseded historical architecture while current UX and epics depend on a different normative architecture artifact.

## Epic Quality Review

### Review Scope and Authority

All eight epics and their stories were reviewed. Epics 1–5 are explicitly retained as completed implementation history, not schedulable production authority. The strict implementation-readiness assessment therefore gives decisive weight to the 33 AC-bearing stories in Epics 6–8 and their prerequisite/evidence work-package ledgers.

The selected supporting conformance checklist was also reviewed. It declares the Architecture Spine authoritative, limits its scope to Epics 6–8, and has status `correction-applied-pending-independent-rerun`.

### Epic-Level Compliance

| Epic | User-value focus | Independence/sequencing | Quality disposition |
| --- | --- | --- | --- |
| 1 — Project Workspace Foundation | Mixed user value and technical foundation | Historical Epic 1 originally deferred part of FR-1 to Epic 2; not independently complete against the final Folder-first PRD | Historical only; superseded criteria must not be scheduled |
| 2 — Context References | Clear workspace-reference value | Uses Epic 1 outputs; no dependency on later historical epics, but Story 2.4 contains explicitly superseded folderless-degradation behavior | Historical only |
| 3 — Project Context Assembly | Clear Chatbot context value | Correctly consumes Epics 1–2 only | Conforms as historical evidence |
| 4 — Project Resolution | Clear Project User/Chatbot value | Correctly consumes earlier reference capabilities only | Conforms as historical evidence |
| 5 — Operational Console & Audit | Clear operator value, mixed with technical surface hardening | Depends on earlier epics; historical maintenance/export criteria are superseded by Epics 7–8 | Historical only |
| 6 — Authorized Project Reads | Clear operator/Chatbot read value | Prior-only dependencies; no dependency on Epics 7–8 | Structurally strong but blocked by open entry gates |
| 7 — Durable Project Decisions | Clear Project User/operator workflow value | Correctly depends on Epic 6 and earlier stories within Epic 7 | Structurally strong but blocked by open platform/sibling gates |
| 8 — Safe Operations and Release Confidence | Mixed: operator value in 8.1–8.5, engineering/release assurance in 8.6–8.11 | Correctly depends on Epics 6–7 and earlier Epic 8 evidence | Major cohesion/sizing concern; blocked by evidence and environment gates |

### 🔴 Critical Violations

#### Q-C1 — The production-authority backlog is explicitly not executable

Story 6.1 is marked `blocked-external`. Its critical path remains:

`6.1-P1R → {6.1-P0, 6.1-P2} → 6.1-P3 → 6.1-P4 → clean-checkout verification → story-spec readiness → independent READY rerun → Story 6.1`.

The epics document and conformance checklist record P0, P1R, P2, P3, and P4 as open or blocked. G-1 through G-6 also remain pending, unavailable, or unaccepted. Because Epic 6 is the first production-authority epic and Epics 7–8 depend on it, no current implementation story can begin under the plan's own containment rules.

**Remediation:** Accept immutable owner-approved revisions and executable rollback for every applicable work package; run the documented clean-checkout restore/run/test/down/evidence-validation sequence; create and validate the Story 6.1 implementation specification; then perform a superseding independent readiness assessment.

#### Q-C2 — Architecture conformance has not been accepted

The supporting conformance checklist leaves every Section A cross-cutting box, Section B gate check, per-epic verdict, AD coverage check, final verdict, architect name, and date blank. Its status is pending independent rerun, not signed conformance. This is not evidence that all 33 stories satisfy the current Architecture Spine.

**Remediation:** Review every current story against A1–A10, G-1–G-6, the per-story checks, and substantive AD-1–AD-34 coverage; resolve findings; record a dated Solution Architect verdict tied to the immutable epics and architecture revisions.

#### Q-C3 — The assessment input omits the architecture that the stories require

The production stories and their conformance checklist are reconciled against `ARCHITECTURE-SPINE.md`, while the selected architecture input is its explicitly superseded predecessor. Story quality cannot be validated against its normative decisions from the selected artifact set.

**Remediation:** Add the Architecture Spine—or a demonstrably newer canonical replacement—to the assessment inventory and rerun architecture/UX/story conformance. Do not treat the superseded May architecture as normative.

### 🟠 Major Issues

#### Q-M1 — Epic 8 mixes product value with a release-engineering program

Stories 8.1–8.5 deliver operator-visible value. Stories 8.6–8.11 are primarily health instrumentation, packaging, evidence integration, performance qualification, resilience qualification, and a release decision. Combining them under one epic weakens the single-outcome test and makes “Epic complete” mean both product capability and organizational approval.

**Remediation:** Preserve stable IDs if required, but distinguish an operator-capability value stream from a production-assurance/release-gate stream. Treat evidence packages and the terminal decision as explicit governance gates rather than conventional user stories, or split the epic into coherent outcomes.

#### Q-M2 — Several stories are epic-sized

The clearest examples are:

- **6.3:** get context, read-only refresh, explanations, all ACL evidence, leakage proof, and three FRs.
- **6.5:** inventory, detail, reference health, resolution trace, audit, authentication, fail-closed states, and full accessibility evidence.
- **8.3:** the complete Web action matrix across archive, restore, relink, unlink, RefreshContext, renewal, cancellation, recovery, terminal behavior, and Fluent governance.
- **8.8:** integration of three separately owned security/privacy/parity/accessibility evidence packages.
- **8.9:** performance, supported cardinality, pagination, export bounds, rate limits, task limits, timeouts, retries, and overload safety.
- **8.10:** restart, two-instance fencing, duplicate delivery, concurrency, partial failure, lost response, compensation, and reconciliation across multiple workflows.

These may be coherent test programs, but they are not small independently completable implementation stories.

**Remediation:** Split by observable vertical slice or verification concern while retaining an integrating acceptance gate. For example, separate context retrieval from refresh/explanation; split Web actions by lifecycle versus reference actions; split performance/cardinality from admission/back-pressure; qualify individual durable workflow families before an aggregate resilience gate.

#### Q-M3 — The role-to-action authorization matrix is incomplete

The PRD distinguishes Project User, Tenant Operator, Tenant Project Administrator, and delegated service authority. The canonical operator action table lists action classification and admission contract but no permitted roles. Story 8.3 then says “authenticated operator” for all actions, risking Tenant Operator visibility of administrator-only relink/unlink operations. Story 8.5 constrains autonomous confirmation but does not replace a complete per-role matrix.

**Remediation:** Add a canonical role × surface × action matrix and bind Web, CLI, MCP, Chatbot, and service/workflow ACs to it. Include negative tests proving that surface selection and workload identity never expand actor authority.

#### Q-M4 — Entry work packages are not self-contained handoff artifacts

The epics document summarizes P0/P1R/P2/P3/P4, 7.1-P1/P2, 8.8-P1/P2/P3, and 8.11-P1/P2/P3, but delegates their complete acceptance contracts to change proposals or owner repositories. This is acceptable as a dependency ledger, yet it prevents the first implementation story from being independently actionable from the epics artifact alone.

**Remediation:** For each open work package, link a versioned owner-repository specification containing entry conditions, exact commands, expected artifacts, pass/fail semantics, immutable revision, rollback, owner acceptance, and consumer compatibility proof. The Projects story should consume only the accepted package record.

### 🟡 Minor Concerns

#### Q-m1 — Superseded stories remain interleaved with current authority

Historical Stories 1.4, 2.4, 5.12, and related criteria visibly conflict with the final Folder-first, Durable Task, and evidence rules, although they carry supersession warnings. Keeping them in the same operational document increases accidental implementation risk.

**Recommendation:** Move historical Epics 1–5 to a clearly separated appendix or immutable history artifact, and machine-mark every historical story non-schedulable.

#### Q-m2 — Shared invariant references reduce local scenario clarity

Epic 7 efficiently centralizes six durable-workflow invariants, but many story ACs say “plus shared invariants” rather than naming the exact restart, duplicate, concurrency, lost-response, stale-authority, and audit cases that apply to that story.

**Recommendation:** Keep the shared contract but add a per-story applicability table and required evidence-case IDs so test generation cannot omit a relevant invariant.

#### Q-m3 — Evidence commands are partly templated

Shared commands such as `--filter Story=<id>` and globbed evidence filenames are deterministic at the planning level but are not yet executable proof. This is correctly represented as blocked, not passed.

**Recommendation:** Materialize the exact commands and expected artifact hashes/paths in each story file when its gates are accepted.

### Dependency Analysis

- **Current Epics 6–8:** No forbidden dependency on a later epic or later-numbered story was found. Story 6.6 compares against earlier 6.5; 6.7 integrates prior Epic 6 reads; Epic 7 composes prior Epic 6 and earlier 7.x work; 8.5 compares earlier 8.3/8.4; 8.8 integrates earlier 8.3–8.5; 8.11 consumes earlier 8.6–8.10.
- **External gates:** Explicitly represented as prerequisites rather than hidden future feature work. This is structurally sound, but their unresolved state blocks execution.
- **Historical exceptions:** Story 1.4 deferred final creation semantics to later Story 2.4, and historical Story 5.9 described restore before the final restore story existed. These violate strict standalone sequencing but are explicitly superseded and are not current production authority.
- **Circular dependencies:** None found in the current production-authority plan.

### Story and AC Quality

Strengths:

- All current stories name a beneficiary and observable outcome.
- Traceability lines connect FRs/NFRs, architecture decisions, journeys, findings, and evidence rows.
- Acceptance criteria predominantly use explicit Given/When/Then conditions and cover happy, denied, stale, unavailable, duplicate, recovery, compatibility, or rollback behavior where applicable.
- Current stories identify verification commands, expected evidence, estimates, and completion boundaries, either locally or through explicit epic-level shared contracts.
- The plan uses prerequisite packages rather than pretending cross-repository/platform work is delivered inside Projects stories.

Weaknesses:

- Several stories remain too broad for independent implementation and review.
- Role/action authorization is not expressed as one testable canonical matrix.
- Critical work-package and conformance evidence remains open, so AC completeness has not become executable readiness.

### Starter, Brownfield, and Persistence Checks

- The historical first story follows the selected hybrid starter/scaffold and includes solution, dependency, environment, and CI wiring.
- The current plan correctly treats the module as brownfield and includes shadow-read cutover, reversible routing, additive event evolution, legacy reconciliation, and no-history-rewrite constraints.
- No “create all database tables upfront” violation was found. Persistence/read-model changes are introduced with the value slices that need them, with EventStore remaining the write authority.

### Best-Practices Verdict

**Non-conformant (blocking) for implementation start.**

The current story architecture is substantially stronger than the historical plan: FR coverage is complete, sequencing is prior-only, and ACs are unusually explicit. It nevertheless fails implementation readiness because its own first-story entry gates are unresolved, current architecture conformance is unsigned and unavailable from the selected inputs, and several stories/matrices remain too broad or insufficiently explicit for independent execution.

## Summary and Recommendations

### Overall Readiness Status

# NOT READY

Implementation must not begin under the production-authority plan. The 24 PRD Functional Requirements have 100% story coverage, but coverage is not readiness. The selected architecture is explicitly superseded; the current stories depend on an excluded normative Architecture Spine; Story 6.1 and its platform/identity/evidence prerequisites remain blocked; and architecture conformance has not been signed.

The backlog's own containment language reaches the same conclusion: production-authority implementation remains blocked until the Story 6.1 entry package is accepted and executable from a clean checkout, its story specification passes readiness, and an independent assessment returns exactly `READY`.

### Critical Issues Requiring Immediate Action

1. **Use the normative architecture.** Replace the selected superseded `architecture.md` input with `architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md` or a proven newer canonical replacement. Revalidate PRD, UX, epics, and AD-1–AD-34 against that immutable revision.
2. **Close the Story 6.1 critical path.** Accept P1R, P0, P2, P3, and P4 with owner-approved immutable revisions, machine-checkable artifacts, and executable rollback. G-1 through G-6 must be accepted where applicable; a local or uncommitted candidate is not acceptance.
3. **Complete the clean-checkout gate.** From the accepted checkout, successfully run `dotnet tool restore`, module run, full-profile module test, module down, and evidence-matrix validation exactly as declared in the epics document. Record commands, results, artifacts, and hashes.
4. **Obtain architecture conformance sign-off.** Complete the supporting checklist's A1–A10, B, all 33 per-story checks, and substantive AD-1–AD-34 mapping; resolve every blocker; record the Solution Architect, date, immutable inputs, and verdict.
5. **Supply the Chatbot companion release input.** Obtain the separately owned, approved, immutable, version-pinned companion UX and authenticated evidence package required by 8.8-P3. Its absence must continue to block Stories 8.8 and 8.11.

### Recommended Next Steps

1. Reopen document discovery with the normative architecture added and the superseded architecture retained only as historical evidence.
2. Materialize self-contained owner-repository specifications for every open prerequisite/evidence package, including exact entry criteria, commands, artifacts, rollback, and approvals.
3. Add a canonical role × surface × action authorization matrix covering Project User, Tenant Operator, Tenant Project Administrator, service/workflow caller, Web, CLI, MCP, and Chatbot; attach negative authorization tests.
4. Normalize UX terminology: map or remove `Resolved`, `Excluded`, and `FailedClosed` where they could be mistaken for `resolutionResult`, and replace historical “replay” wording with current recomputation where appropriate.
5. Split oversized stories or add bounded sub-slices and integrating gates, especially 6.3, 6.5, 8.3, 8.8, 8.9, and 8.10. Separate Epic 8 operator capability from release-assurance governance if stable planning IDs permit.
6. Move Epics 1–5 to a clearly non-schedulable historical appendix or artifact and add a per-story applicability matrix for shared durable-workflow invariants.
7. Create the full Story 6.1 implementation specification only after its prerequisite records are accepted, then validate that story for ready-for-development quality.
8. Rerun implementation readiness independently using the corrected canonical inventory. Proceed only if the resulting verdict is exactly `READY`; Story 8.11 remains the separate terminal production-release gate.

### Issue Summary

- **4 critical:** missing normative architecture in the assessment, unresolved Story 6.1/platform gates, unsigned architecture conformance, and absent mandatory Chatbot companion evidence.
- **6 major:** canonical UX vocabulary drift, historical “replay” terminology, incomplete role/action matrix, mixed-value Epic 8, oversized stories, and non-self-contained dependency packages.
- **3 minor:** superseded stories interleaved with current authority, shared-invariant applicability not localized, and templated rather than executable evidence commands.
- **Total:** 13 issues across document authority, dependency/conformance readiness, UX/authorization alignment, and epic/story quality.

### Final Note

The planning set has strong requirement discipline: 24/24 FRs are mapped, the current production stories have no forbidden forward dependencies, and their acceptance criteria are materially better than the superseded historical plan. Those strengths do not offset the explicit blockers. Address all critical issues before Phase 4 implementation; major story/UX defects should be corrected in the same planning revision so the next readiness run evaluates an executable, internally consistent baseline.

**Assessment date:** 2026-08-01  
**Assessor:** Codex, independent implementation-readiness assessor
