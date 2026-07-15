---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
status: complete
overallReadiness: NOT_READY
completedAt: 2026-07-15
assessor: Codex
documentsIncluded:
  prd:
    - prds/prd-Hexalith.Projects-2026-05-24/prd.md
    - prds/prd-Hexalith.Projects-2026-05-24/addendum.md
    - prds/prd-Hexalith.Projects-2026-05-24/reconcile-product-brief.md
    - prds/prd-Hexalith.Projects-2026-05-24/reconcile-sprint-change-proposal-2026-07-06.md
    - prds/prd-Hexalith.Projects-2026-05-24/reconcile-sprint-change-proposal-2026-07-14.md
    - prds/prd-Hexalith.Projects-2026-05-24/reconcile-sprint-change-proposal-2026-07-14-frontcomposer-contract-boundary.md
    - prds/prd-Hexalith.Projects-2026-05-24/reconcile-sprint-change-proposal-2026-07-14-implementation-readiness-correction.md
    - prds/prd-Hexalith.Projects-2026-05-24/reconcile-sprint-change-proposal-2026-07-14-live-apphost.md
    - prds/prd-Hexalith.Projects-2026-05-24/reconcile-sprint-change-proposal-2026-07-14-u2028-u2029-idempotency-parity.md
    - prds/prd-Hexalith.Projects-2026-05-24/reconcile-sprint-change-proposal-2026-07-15.md
    - prds/prd-Hexalith.Projects-2026-05-24/reconciliation-closure.md
  architecture:
    - architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md
    - architecture.md
  epics:
    - epics.md
  ux:
    - ux-design-specification.md
    - ux-design-directions.html
  sprintChangeProposals:
    - sprint-change-proposal-2026-07-06.md
    - sprint-change-proposal-2026-07-14.md
    - sprint-change-proposal-2026-07-14-frontcomposer-contract-boundary.md
    - sprint-change-proposal-2026-07-14-implementation-readiness-correction.md
    - sprint-change-proposal-2026-07-14-live-apphost.md
    - sprint-change-proposal-2026-07-14-u2028-u2029-idempotency-parity.md
    - sprint-change-proposal-2026-07-15.md
documentPrecedence:
  prd: prds/prd-Hexalith.Projects-2026-05-24/prd.md is primary; addendum and reconciliation documents are companions
  architecture: architecture.md is the only substantive architecture; architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md is an uninstantiated draft template and is non-authoritative
  supportingEvidence: editorial, validation, audit, review-rubric, and handoff artifacts are non-normative
---

# Implementation Readiness Assessment Report

**Date:** 2026-07-15
**Project:** projects

## Document Inventory

### PRD

Primary document:

- `prds/prd-Hexalith.Projects-2026-05-24/prd.md` (46,095 bytes; modified 2026-07-15)

Companion documents:

- `prds/prd-Hexalith.Projects-2026-05-24/addendum.md`
- `prds/prd-Hexalith.Projects-2026-05-24/reconcile-product-brief.md`
- Seven July sprint-change reconciliation documents
- `prds/prd-Hexalith.Projects-2026-05-24/reconciliation-closure.md`

The PRD bundle has no `index.md`. Editorial, validation, audit, review-rubric, and handoff artifacts are classified as supporting evidence rather than normative requirements.

### Architecture

- `architecture.md` (51,169 bytes; modified 2026-07-14) — only substantive architecture document
- `architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md` (4,357 bytes; modified 2026-07-15) — uninstantiated draft template; excluded as an architectural authority

### Epics and Stories

- `epics.md` (110,516 bytes; modified 2026-07-14)

### UX

- `ux-design-specification.md` (53,895 bytes; modified 2026-05-29)
- `ux-design-directions.html` (25,390 bytes; modified 2026-05-29) — visual companion

### July 2026 Sprint-Change Proposals

- `sprint-change-proposal-2026-07-06.md`
- `sprint-change-proposal-2026-07-14.md`
- `sprint-change-proposal-2026-07-14-frontcomposer-contract-boundary.md`
- `sprint-change-proposal-2026-07-14-implementation-readiness-correction.md`
- `sprint-change-proposal-2026-07-14-live-apphost.md`
- `sprint-change-proposal-2026-07-14-u2028-u2029-idempotency-parity.md`
- `sprint-change-proposal-2026-07-15.md`

### Discovery Resolution

All required document categories were found. Content validation later established that the architecture spine candidate is an uninstantiated template, so `architecture.md` is the only substantive architecture. The PRD bundle is retained with a primary-document designation. No unresolved duplicate blocks remain.

## PRD Analysis

### Functional Requirements

#### FR-1: Create Project

Chatbot can admit Project creation as an idempotent Durable Task. A Project becomes caller-visible and `Active` only after exactly one authorized Project Folder is verified and bound.

- Project name is the only required user-authored field; canonical requests also carry a valid system-supplied Metadata Classification.
- A supplied Project Folder is authorized and verified. When none is supplied, Projects requests same-name Folder creation from `Hexalith.Folders`.
- Admission returns a pollable Durable Task rather than an immediately Active Project.
- Dependency denial, timeout, cancellation, duplicate delivery, lost response, or reconciliation never exposes an Active folderless Project.
- Equivalent Idempotency Key retries return the original task; a materially different request using the same scoped key conflicts.
- Terminal success exposes Project identity only after Read-Model-Confirmed Completion.
- Historical unversioned name-only requests remain supported throughout v1; retirement requires an explicitly approved major version.
- Creation never duplicates transcripts, file contents, prompts, secrets, or Memory payloads.

#### FR-2: Open Project

Chatbot can open an authorized Project and receive the metadata, lifecycle state, Project Setup, and references needed to initialize a Conversation.

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

An authorized Project User, Tenant Operator, or Tenant Project Administrator can archive an Active Project through server Preview, single-use confirmation, and an idempotent Durable Task.

- Project Lifecycle State remains limited to `Active` and `Archived`.
- Confirmation is invalidated when actor authority or Project version changes.
- Archived Projects are excluded from Project Resolution unless explicitly requested.
- Completion is not reported until the read model confirms `Archived`.
- Existing references remain auditable after archival.

#### FR-5: List Projects

Authorized callers can list visible Active and Archived Projects.

- Results are Tenant-scoped, authorization-filtered, and filterable by lifecycle state.
- Each result contains authorized Project identity, name, lifecycle state, current version, Project Folder availability, and the response/freshness/recovery metadata needed for selection without loading full context.
- Pre-activation tasks never appear as Projects.
- Cursor pages default to 50 and cap at 200; cursors remain scoped to the authenticated query.

#### FR-6: Link Conversation

An authorized Project User can link an existing Conversation to a Project.

- A Conversation belongs to exactly one Project in v1.
- An explicitly actor-selected additive link uses an idempotent Durable Task without a second confirmation; an inferred link requires Preview and confirmation.
- Linking a Conversation already assigned elsewhere requires FR-7 rather than a second membership.
- Authorization failure prevents protected resource access or durable effect.
- The link stores stable identity and metadata, never transcript content.

#### FR-7: Move Conversation Between Projects

An authorized Project User or Tenant Project Administrator can move a Conversation through Preview, single-use confirmation, and an idempotent Durable Task.

- Preview binds both Projects, the Conversation, actor, and current resource versions.
- Completion yields exactly one Project membership and a durable cross-context receipt.
- Failure, duplicate delivery, or lost response cannot leave two memberships silently valid.
- The move is audited using metadata only and fails closed when either Project or the Conversation cannot be authorized.

#### FR-8: Set Project Folder

An authorized Project User can set the single Project Folder; a Project User or Tenant Project Administrator can replace it through Preview and confirmation.

- Every Active Project has exactly one authorized Project Folder.
- Initial actor-selected binding is idempotent; inferred binding requires confirmation.
- Replacement binds old and new Folder evidence to the Confirmation Artifact and completes only after the authoritative read model confirms replacement.
- Projects stores Folder identity and metadata, never file contents or unrestricted paths.
- `Hexalith.Folders` remains the authorization and system-of-record boundary.

#### FR-9: Link File Reference

An authorized Project User can link a File Reference without changing the Project Folder.

- File References are optional and do not replace the Project Folder.
- Actor-selected additive linking is idempotent; inferred linking requires confirmation.
- Projects stores stable File identity and metadata only; authorization remains delegated to `Hexalith.Folders`.

#### FR-10: Link Memory

An authorized Project User can link a Memory.

- Actor-selected additive linking is idempotent; inferred linking requires confirmation.
- Projects stores stable Memory identity and metadata only.
- Authorization remains delegated to `Hexalith.Memories`.

#### FR-11: Unlink Context Reference

An authorized Project User or Tenant Project Administrator can unlink a Conversation, File Reference, or Memory through Preview, confirmation, and an idempotent Durable Task. The Project Folder can be replaced but not removed from an Active Project.

- Unlinking removes only the association and never deletes the underlying resource.
- Preview identifies the affected reference and current Project version.
- Completion is durable, metadata-only audited, and read-model-confirmed.
- The operation fails closed on stale authorization or resource evidence.

#### FR-12: Resolve Project From Conversation

Chatbot can request Candidate Projects for a Conversation with no explicit Project.

- The result is `NoMatch`, `SingleCandidate`, or `MultipleCandidates` with current Resolution Reason Codes.
- Only Active, read-model-confirmed Projects are considered by default.
- Pre-activation tasks and unauthorized or stale resources cannot become candidates.
- The response follows the PRD section 5 contract; `Unavailable` and `Denied` never return a selected candidate.

#### FR-13: Resolve Project From Attachments

Chatbot can resolve Candidate Projects from an attached Project Folder or File References.

- Matching uses current authorized Folder/File identity and metadata, not file contents.
- Applicable candidates include `ProjectFolderMatched` or `FileReferenceMatched` reason codes.
- Missing, stale, or unavailable authorization evidence fails closed.

#### FR-14: Confirm Ambiguous Project

When resolution returns multiple candidates, Chatbot presents an accessible, unselected comparison and records the Project User's choice through a Confirmation Artifact and Durable Task.

- No candidate is silently or visually preselected.
- The artifact is bound to Tenant, actor, action, Conversation, candidates, normalized request, Preview, and current versions; it expires after 15 minutes and is single-use.
- Stale, expired, replayed, or tampered confirmation is rejected safely and requires a fresh Preview.
- Only Read-Model-Confirmed Completion creates or updates the Conversation association and audit history.
- Chatbot supports confirmation, cancellation, retry, expiry/staleness, lost-response recovery, and task-status states.

#### FR-15: Propose New Project

When no suitable Project exists, Chatbot can present a proposed Project and admit creation only after the Project User confirms a bound Preview.

- The proposal may suggest a Project name and setup metadata but creates nothing before confirmation.
- The Confirmation Artifact binds the initiating Conversation, authorized attachments, Folder plan, normalized request, and current evidence.
- Confirmed creation follows FR-1 and exposes no Project before Folder binding and read-model confirmation.
- Non-success outcomes follow the recovery contract: cancellation returns `Cancelled`, terminal failure returns `Failed`, and expired or stale evidence creates no task.

#### FR-16: Get Project Context

Chatbot can request Project Context for an Active Project.

- Context is Tenant-scoped, actor-authorized, and available only for a read-model-confirmed Active Project with exactly one authorized Project Folder.
- It contains Project Setup and reference metadata, not foreign bounded-context payloads.
- Every excluded, stale, rebuilding, or unavailable reference is represented as a metadata-only component; `Denied` discloses no protected detail.

#### FR-17: Explain Context Selection

Authorized callers can obtain current metadata explaining why a reference was included or excluded.

- Explanations are current Resolution Traces, not reconstructed history.
- Traces contain no secrets, payloads, prompts, unrestricted paths, raw upstream problems, or unconfirmed-candidate detail.
- Traces are request-scoped and nonpersistent; only confirmed outcomes enter audit history.

#### FR-18: Refresh Project Context

Chatbot can request a read-only refresh after links, setup, authorization, or resource availability changes.

- Refresh recomputes from current authorized Project, Conversation, Folder, File Reference, Memory, and version metadata.
- Refresh never mutates Project/reference state and creates no maintenance audit event.
- The refreshed response includes new snapshot metadata, component evidence, recovery actions, and binding `Partial`, `Unavailable`, and `Complete` transition rules.

#### FR-19: Validate Project Setup

Projects validates setup and creation admission before accepting durable work.

- Project name remains the only required user-authored creation field.
- Canonical creation requests require valid system-supplied Metadata Classification; invalid classification is rejected before command submission.
- Validation permits a supplied authorized Project Folder or same-name Folder creation, but never exposes a caller-visible Active Project before Folder completion.
- Validation rejects secrets, unrestricted paths, unsupported references, unsafe control/invisible characters, and foreign payloads.
- Failures identify safe field/reason codes without echoing sensitive values.

#### FR-20: Retrieve Conversation-Start Setup

Chatbot can retrieve the subset of Project Setup needed to start or resume a Conversation.

- The result includes goals, user-facing instructions, context preferences, and default source policy.
- It excludes internal audit metadata and unavailable or unauthorized references.
- It is bound to one authorized `projectVersion` and `asOf` snapshot. Chatbot may admit the first response only for `Complete` or `Partial`; `Unavailable` or `Denied` blocks admission and returns applicable Recovery Action Codes without re-querying every bounded context.

#### FR-21: Record Project Audit Events

Projects records metadata-only audit events for consequential task admission and outcome, confirmed Project mutations, security-relevant confirmation outcomes, reconciliation, and Safe Diagnostic Export.

- Audit covers task admission/terminal outcome; confirmation use/cancellation; rejection of stale, replayed, or tampered confirmations; authorization denial; creation, archive, restore, move, relink, Folder replacement, unlink, confirmed resolution/proposed creation; manual reconciliation; Safe Diagnostic Export creation; and stable upstream receipt identifiers.
- Equivalent idempotent retries do not create duplicate audit events.
- Intermediate task states, polls, retries, dependency latency, notifications, unused expiry, and read-only Resolution Traces remain operational telemetry rather than durable audit.
- Audit contains Tenant, actor, Project/action identity, timestamp, safe reason/outcome codes, and affected reference identifiers, never payloads or secrets.

#### FR-22: Support Operator Read Access

Tenant Operators and Tenant Project Administrators can inspect authorized Project metadata, lifecycle state, references, Durable Task status, confirmed resolution outcomes, and audit metadata.

- Access is Tenant-scoped, action-authorized, and metadata-only across Web, CLI, and MCP.
- Project Users may inspect only their own permitted task status through Chatbot.
- Pre-activation tasks remain separate from Project list/open APIs; authorized operators may inspect safe status, and Tenant Project Administrators may perform authorized reconciliation.
- Read permission alone grants neither Safe Diagnostic Export nor mutation authority.

#### FR-23: Restore Archived Project

An authorized Project User, Tenant Operator, or Tenant Project Administrator can restore an Archived Project through Preview, confirmation, and an idempotent Durable Task.

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

**Total Functional Requirements: 24**

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

v1 supports 10,000 Projects per Tenant, 5,000 Context References per Project excluding its Folder, and 100,000 retained audit records per Project. Metadata reads target p95 under 500 ms at 1,000 Projects and 500 references, and p95 under 1 second at the supported maximum. Durable-task admission targets p95 under 500 ms under authenticated warm steady-state with required dependencies available.

#### NFR-6: Pagination and export bounds

Cursor pages default to 50 and cap at 200. Safe Diagnostic Export obeys FR-24's per-export global size/row bounds and a per-Tenant limit of two concurrent exports.

#### NFR-7: Back-pressure and dependency control

Per Tenant, v1 supports 100 metadata reads/second with burst 200, 20 mutation admissions/second with burst 40, 1,000 nonterminal tasks, and two concurrent Safe Diagnostic Exports. Interactive dependency timeout defaults to 2 seconds and durable-step timeout to 10 seconds. Idempotent calls retry at most three times within 30 seconds before truthful waiting/intervention status. Overload returns structured retry guidance.

#### NFR-8: Retention and transient data

Active tasks remain pollable until terminal. A terminal result and its scoped idempotency record remain available for at least 30 days or for the result's lifetime, whichever is longer. Preview/Confirmation Artifacts expire after 15 minutes. Audit metadata is retained at least 365 days and never less than applicable retained event-history obligations. Resolution Traces and generated exports are not persisted.

#### NFR-9: Accessibility

Chatbot candidate, confirmation, cancellation, recovery, and task journeys, plus operator read, mutation, and export journeys, conform to WCAG 2.2 AA. They are keyboard operable, visibly focused, announced to assistive technology, do not rely on color or timing alone, and are usable at 200% zoom and 320 CSS pixels. Verification combines automated evidence with authenticated manual keyboard and screen-reader evidence.

#### NFR-10: Compatibility

Contracts are additive and serialization-tolerant unless a breaking change is explicitly approved. Historical v1 data and unversioned name-only creation remain readable/accepted throughout v1. Retirement requires a major version, migration notice, usage evidence, compatibility tests, and rollback evidence; event history is not rewritten.

#### NFR-11: Release evidence

Authenticated persisted-boundary, cross-Tenant, restart/concurrency, duplicate-delivery, lost-response, accessibility, privacy, performance, deployment, smoke, rollback, and stakeholder-acceptance evidence must pass. A failed critical case or unexplained critical skip blocks release; unavailable environments remain `not verified`, never `passed`.

**Total Non-Functional Requirements: 11**

### Additional Requirements

- **AR-1 — Observable response contract:** Project open, list, resolution, context, Conversation-start, and proposal-recovery responses carry `responseState`, `asOf`, authorized `projectVersion`, optional `resolutionResult`, metadata-only component evidence, and applicable recovery actions. `Complete`, `Partial`, `Unavailable`, and `Denied` have binding admission/disclosure consequences; refresh produces a new snapshot rather than silently rewriting prior truth.
- **AR-2 — Durable workflow design:** Architecture must specify task persistence and the exact transitions among `Pending`, `Running`, `WaitingForDependency`, `NeedsAttention`, `Succeeded`, `Rejected`, `Failed`, and `Cancelled`, including checkpoints, leases, worker ownership, restart recovery, two-instance convergence, duplicate delivery, lost response, cancellation cutoff, irreversible commit, terminal immutability, compensation, receipts, and manual reconciliation.
- **AR-3 — Platform ownership:** Projects owns domain policy, contracts, and Project-specific task transitions. EventStore DomainService/platform owns reusable hosting, persistence/publication, subscriptions, read-model stores, cursors, health, telemetry, and reusable durable-workflow capability. Platform AppHost owns topology; FrontComposer/platform hosts own Web/MCP/CLI composition. Production paths may not register allow-all identity or authorization stubs.
- **AR-4 — Preview, confirmation, and idempotency:** Downstream design must define artifact schemas, signing/key ownership, normalized request material, version binding, 15-minute expiry, single-use enforcement, replay response, safe renewal, scope `(Tenant, actor, operation, key)`, Unicode-safe equivalence canonicalization, retention, conflict handling, and successful-operation/lost-response recovery.
- **AR-5 — Unicode parity:** Identifier and envelope fields reject U+2028/U+2029 where unsafe; accepted descriptive metadata must have deterministic server/generated-helper escaping and byte parity, no collision with LF or literal backslash-`u` text, stable unaffected hashes, generated-file protection, deployed fingerprint inspection, and a bounded legacy-hash strategy if existing persisted values are affected.
- **AR-6 — Safe Diagnostic Export wire contract:** Architecture/API design must define a versioned `projects.safe-diagnostic-export.v1` representation consistent across Web, CLI, and MCP while preserving all FR-24 bounds, ordering, truncation, authorization, audit, non-retention, and unavailable-component semantics.
- **AR-7 — Contract/package boundaries:** Canonical Create Project accepts exactly `public_metadata`, `tenant_sensitive`, `credential_sensitive`, or `secret` from an authenticated integration adapter and rejects malformed/unknown values before submission with metadata-only field diagnostics. `Hexalith.Projects.UI.Contracts` is a Projects-owned non-packable descriptor host depending on the UI-free Contracts kernel; the kernel may not depend on FrontComposer Shell, Fluxor, Fluent UI, or `Microsoft.AspNetCore.App`. `Hexalith.Builds` is the sole version owner for the approved NSwag and Fluxor versions.
- **AR-8 — Migration and compatibility:** Planning must inventory legacy Active folderless Projects and in-flight Folder work; preserve additive event evolution and historical readability; provide compatibility adapters, replay comparison, value-slice cutover, routing rollback, and safe handling of archived Projects with invalid Folders; avoid history rewrite and unsafe dual writes.
- **AR-9 — Chatbot companion:** Chatbot owns accessible presentation while Projects owns versioned Preview, Confirmation, Durable Task, Resolution, and Context contracts. Candidate comparison has no preselection and includes confirm/cancel, expiry/staleness, lost-response, task-status, safe-denial, keyboard/focus/live-region, zoom, narrow-viewport, and authenticated screen-reader behavior.
- **AR-10 — Verification:** Evidence requires deterministic small/median/maximum fixtures; authenticated persisted-boundary and cross-Tenant tests; freshness, encryption/KMS, replay/tamper, privacy, restart, convergence, duplicate delivery, concurrency, cancellation, lost response, compensation, reconciliation, read-model confirmation, Web/CLI/MCP parity, accessibility, deployment, smoke, rollback, compatibility, outcome measurement, and stakeholder acceptance.
- **AR-11 — Planning containment:** Epics 1–5 are implementation history. The 23 Epic 6–8 entries are findings placeholders rather than schedulable stories. Corrective development and production release remain frozen until the placeholders are replaced by outcome-based stories, an E-2-superseding readiness rerun returns `READY`, Story 8.9 release evidence passes, and Jerome/John record a terminal dated disposition.
- **AR-12 — Evidence status:** The prior live run established a runnable mechanism but failed release acceptance: focused 13 passed/13 failed and full Chromium 19 passed/56 failed. Missing deterministic authorized Tenant seeding and warning-console/static-asset prerequisites require Story 8.6 remediation. Safe-denial `404` evidence does not prove authorized FR-22 behavior.
- **AR-13 — Repository authority:** Projects planning does not implicitly authorize changes in EventStore, FrontComposer, Conversations, Folders, Chatbot, Builds, or other sibling repositories. Each sibling change requires its own approved scope and verification, and only root-declared submodule checkouts may be used.

### PRD Completeness Assessment

The current product contract is internally complete and mechanically strong: FR-1 through FR-24 and NFR-1 through NFR-11 are contiguous, measurable, and accompanied by explicit journeys, state vocabulary, response/recovery semantics, success metrics, and downstream ownership. The final reconciliation audit reports zero material and formal traceability gaps across all seven July sprint-change inputs.

Product-definition completeness does not imply implementation readiness. The PRD addendum deliberately preserves the previous `NOT_READY` result, 23 unschedulable findings placeholders, failed live evidence, unresolved P1/P2 audit findings, and blocked release handoff. These constraints must be tested against architecture, UX, and epic/story coverage in the remaining assessment steps.

## Epic Coverage Validation

### Epic FR Coverage Extracted

The epics document's primary Requirements Inventory and FR Coverage Map claim coverage only for the older FR-1 through FR-22 baseline. The corrective addendum adds explicit FR-23 coverage in Epic 7. FR-24 is not named anywhere in the epics document.

### Coverage Matrix

| FR | PRD requirement | Epic/story coverage | Status |
| --- | --- | --- | --- |
| FR-1 | Create Project | Epic 1 / Story 1.4, corrected by Epic 7 / Story 7.3 | Covered |
| FR-2 | Open Project | Epic 1 / Story 1.7 | Covered |
| FR-3 | Update Project Setup | Epic 1 / Story 1.8 | Covered |
| FR-4 | Archive Project | Epic 1 / Story 1.8, corrected durable flow in Epic 7 / Story 7.6 | Covered |
| FR-5 | List Projects | Epic 1 / Story 1.7 | Covered |
| FR-6 | Link Conversation | Epic 2 / Stories 2.1–2.3, corrected by Epic 7 / Story 7.4 | Covered |
| FR-7 | Move Conversation Between Projects | Epic 2 / Stories 2.2–2.3, corrected by Epic 7 / Story 7.4 | Covered |
| FR-8 | Set Project Folder | Epic 2 / Story 2.4, corrected by Epic 7 / Stories 7.3 and 7.6 | Covered |
| FR-9 | Link File Reference | Epic 2 / Story 2.5 | Covered |
| FR-10 | Link Memory | Epic 2 / Stories 2.6–2.7 | Covered |
| FR-11 | Unlink Context Reference | Epic 2 / Stories 2.3, 2.5, and 2.7; corrected bound-task flow in Epic 7 / Story 7.6 | Covered |
| FR-12 | Resolve Project From Conversation | Epic 4 / Stories 4.1–4.2 | Covered |
| FR-13 | Resolve Project From Attachments | Epic 4 / Stories 4.1 and 4.3 | Covered |
| FR-14 | Confirm Ambiguous Project | Epic 4 / Story 4.4, corrected by Epic 7 / Stories 7.2 and 7.5 | Covered |
| FR-15 | Propose New Project | Epic 4 / Story 4.5, corrected by Epic 7 / Story 7.5 | Covered |
| FR-16 | Get Project Context | Epic 3 / Stories 3.1–3.2 | Covered |
| FR-17 | Explain Context Selection | Epic 3 / Story 3.3 | Covered |
| FR-18 | Refresh Project Context | Epic 3 / Story 3.4 | Covered |
| FR-19 | Validate Project Setup | Epic 1 / Stories 1.4 and 1.8 | Covered |
| FR-20 | Retrieve Conversation-Start Setup | Epic 3 / Story 3.5 | Covered |
| FR-21 | Record Project Audit Events | Epic 5 / Story 5.1, expanded corrective ownership in Epic 7 | Covered |
| FR-22 | Support Operator Read Access | Epic 5 / Stories 5.2–5.11, conformance in Epic 8 / Stories 8.3–8.6 | Covered |
| FR-23 | Restore Archived Project | Corrective Epic 7 completion gate and Story 7.6 | Covered |
| FR-24 | Create Safe Diagnostic Export | Historical Story 5.7 mentions an export and Stories 8.3/8.8 mention machine/performance conformance, but no epic or story names FR-24 or owns its complete current contract | **Missing explicit coverage** |

No functional requirement appears in the epics document that is absent from the current PRD. The inverse problem is present: the epics Requirements Inventory and FR Coverage Map still assert a 22-FR baseline and have not been reconciled to the final 24-FR PRD.

### Missing Requirements

#### Critical: FR-24 — Create Safe Diagnostic Export

A separately authorized Tenant Operator or Tenant Project Administrator can create a bounded Safe Diagnostic Export through Web, CLI, or MCP. The requirement includes distinct export permission, metadata-only audit of every attempt/outcome, a complete encoded-response cap of 1 MiB, caps of 500 reference rows and 100 audit rows, deterministic reference ordering, newest-first audit ordering with stable tie-breaking, safe included/omitted counts and truncation reasons, no continuation cursor, safe upstream-unavailability representation, and no server retention.

- **Impact:** The current Epic 5 Story 5.7 covers only a generic safe metadata export. It does not explicitly own the current authorization separation, quantitative bounds, deterministic ordering/truncation, audit, upstream-failure, no-cursor, or non-retention obligations. This leaves a release-blocking safety/operations requirement without a complete implementation path.
- **Recommendation:** Replace the applicable Epic 8 findings placeholder with an outcome-based FR-24 story, or explicitly expand an approved replacement story, and give it deterministic Web/CLI/MCP parity, authorization, audit, bounds, ordering, truncation, failure, non-retention, performance, and release-evidence acceptance criteria. Update the Requirements Inventory and FR Coverage Map to the final FR-1–FR-24 baseline.

### Coverage Statistics

- Total PRD FRs: 24
- FRs with explicit or corrective epic coverage: 23
- FRs missing complete explicit coverage: 1
- Effective coverage: 95.8%

## UX Alignment Assessment

### UX Document Status

**Found.** The assessment reviewed the complete `ux-design-specification.md` and `ux-design-directions.html` companion. The UX specification is marked complete but was produced from the May 2026, FR-1–FR-22 baseline. It does not list the July PRD/addendum, the final FR-1–FR-24 baseline, or any July sprint-change proposal among its inputs.

The visual-direction HTML is a concept prototype, not implementation-ready FrontComposer code. Its hard-coded colors, native controls, raw table/layout markup, and bespoke CSS must not be copied into product code; implementation remains bound to FrontComposer, Fluent UI Blazor V5, Fluent 2 tokens, and the repository's no-theme-redefinition rules.

### Confirmed Alignment

- UX and PRD agree that Projects is a metadata control plane rather than a generic project-management or payload-browsing product.
- Both require Tenant-scoped, metadata-only, fail-closed behavior and explicit safe reason codes.
- The Project inventory, detail, reference health, resolution explanation, audit, warning, maintenance, CLI, MCP, and Web concepts broadly support FR-16 through FR-22.
- The UX correctly requires FrontComposer/Fluent UI, shared cross-surface vocabulary, non-color-only status, keyboard access, visible focus, responsive behavior, stable automation identifiers, and WCAG 2.2 AA intent.
- Candidate comparison avoids silent selection, and maintenance concepts generally use preview/confirmation and audit evidence.

### Alignment Issues

#### Critical

1. **Chatbot companion UX is missing.** The UX explicitly excludes the end-user Chatbot experience from its direct scope, while the final PRD/addendum requires UX and integration artifacts to define Project User candidate comparison, no-preselection confirmation, proposal creation, cancellation, expiry/staleness renewal, lost-response recovery, task polling/status, and first-response admission for `Complete`/`Partial` versus blocking `Unavailable`/`Denied`. The operator console cannot substitute for these FR-14, FR-15, FR-20, NFR-9 journeys.

2. **FR-24 UX is incomplete.** `Safe Diagnostic Export` is described as a generic copy/download/resource action, but UX does not express the separately authorized role boundary, 1 MiB complete-response cap, 500-reference and 100-audit-row caps, deterministic ordering, included/omitted counts, safe truncation reasons, absence of continuation, safe upstream-unavailability state, audit of every attempt/outcome, or non-retention.

3. **Architecture does not represent the current approved ownership model.** `architecture.md` remains a 22-FR design that assigns Projects-owned `UI`, `Mcp`, `Cli`, `ServiceDefaults`, `Aspire`, and `AppHost` projects and places FrontComposer descriptors in the Contracts kernel. The final addendum instead assigns reusable hosting/topology/runtime composition to EventStore/platform/FrontComposer hosts and requires a non-packable `Hexalith.Projects.UI.Contracts` descriptor boundary. The architecture therefore cannot currently support the UX through the approved package and host boundaries.

4. **The July architecture spine is unusable.** `ARCHITECTURE-SPINE.md` still contains placeholders such as `{name}`, `{date}`, `{decision}`, an empty stack, and template comments with `status: draft`. It supplies no decisions or invariants and cannot supersede or repair `architecture.md`.

#### High

5. **State models are not mapped.** UX uses its own five-state command lifecycle and maintenance-panel states without a normative mapping to the PRD's Durable Task statuses, Context Response States, Evidence Freshness States, Recovery Action Codes, Preview expiry, and Read-Model-Confirmed Completion. This permits acknowledgement or SignalR nudge to be mistaken for success.

6. **Read-only refresh is presented as mutation.** UX repeatedly classifies `reevaluate` as a mutating maintenance tool requiring confirmation and audit. The final PRD makes FR-18 refresh/re-evaluation read-only, current recomputation that creates no maintenance audit event. The UI terminology and action classification must be corrected.

7. **Audit examples drift from FR-21.** The visual direction shows `ProjectOpened`, `ReferenceValidated`, and `ArchiveDryRun` as audit-timeline events. The current PRD keeps ordinary reads, unused previews, dependency latency, polls, retries, and read-only traces as operational telemetry rather than durable audit. UX needs the canonical audit taxonomy and a distinct telemetry/history presentation if these facts remain useful.

8. **Role/action authority is too generic.** UX primarily says “administrator” or “operator” and exposes restore/relink/unlink/confirmation concepts without consistently distinguishing Project User, Tenant Operator, Tenant Project Administrator, delegated service caller, and separately authorized export permission. Surface availability must never expand role authority.

9. **MCP containment is absent.** UX assumes mutating MCP tools and proposal confirmation are available, but current planning explicitly keeps consequential autonomous MCP mutation and proposal confirmation disabled until the corrective and release gates pass. UX needs gated/disabled states and must distinguish a human-authorized mediated action from autonomous agent confirmation.

#### Medium

10. **Current response/recovery semantics are absent from primary views.** The UX lacks explicit rendering requirements for `responseState`, `asOf`, `projectVersion`, per-component freshness/inclusion evidence, and Recovery Action Codes across open, list, resolution, context, Conversation-start, and proposal recovery.

11. **Accessibility evidence is underspecified.** UX calls for automated checks and screen-reader spot checks, but NFR-9 requires authenticated manual keyboard and screen-reader evidence in addition to automation, including 200% zoom and 320 CSS pixels for all in-scope Chatbot and operator journeys.

12. **Page-section composition conflicts with current repository rules.** UX proposes tabs/pivots for multiple sibling titled regions. Current Hexalith UX rules require page-like surfaces with two or more sibling titled sections to use one `FluentAccordion`, keep the sole primary region visible, and expand the primary accordion item by default.

13. **Performance/cardinality UX behavior is missing.** The final NFRs define 10,000 Projects per Tenant, 5,000 references per Project, cursor defaults/caps, two concurrent exports, and median/maximum p95 targets. UX mentions high-volume testing but does not define pagination/virtualization, loading, truncation, or recovery behavior at those binding shapes.

### Warnings

- `architecture.md` self-reports `READY WITH MINOR GAPS`, but that conclusion is based on the obsolete FR-1–FR-22/NFR-1–NFR-9 contract and conflicts with the July PRD/addendum and current platform rules. It must not be used as current readiness evidence.
- The visual HTML contains vocabulary drift (`FolderMatched`, `conversationReferenceMatched`, `ambiguous_requires_confirmation`, `ProjectFolderRelinked`) relative to the PRD's canonical reason/audit vocabulary. Mock data must be normalized before it becomes contract, fixture, or snapshot input.
- UX and architecture both assume persisted or replayable “resolution cases/traces” in places, while the final contract requires current request-scoped, nonpersistent Resolution Traces and no reconstructed candidate-score history.

## Epic Quality Review

### Epic-Level Compliance

| Epic | User-value focus | Independence and readiness | Verdict |
| --- | --- | --- | --- |
| Epic 1 — Project Workspace Foundation | Mixed: delivers create/open/list/update/archive, but also packages scaffold, contract generation, replay harnesses, authorization, and topology as technical milestones | Current FR-1 and FR-4 compliance depends on later corrective Epic 7; Story 1.4 directly contradicts the final Folder-gated activation invariant | **Critical defects** |
| Epic 2 — Context References | Clear workspace/reference value | Depends on separately delivered Conversations and Folders capabilities; Story 2.4 permits a folderless-active outcome that the final PRD forbids | **Critical defects** |
| Epic 3 — Project Context Assembly | Clear Chatbot value | Depends only on earlier Epics 1–2, but stories omit the final response/freshness/recovery and first-response-admission contract | **Major gaps** |
| Epic 4 — Project Resolution | Clear Chatbot value | Depends on earlier Epic 2, but current confirmation, expiry, tamper, task, and recovery behavior is deferred to later Epic 7 | **Critical forward-compliance gap** |
| Epic 5 — Operational Console & Audit | Clear operator value | Several stories are epic-sized, the export contract is incomplete, role boundaries are weak, and live verification can complete with failures/blockers | **Critical defects** |
| Epic 6 — Supported Platform Boundary and Secure Identity | Primarily a technical migration milestone | Seven one-paragraph placeholders; no user-story form, acceptance criteria, evidence, sizing, or complete requirement traceability | **Not schedulable** |
| Epic 7 — Durable Cross-Context and Agent-Safe Workflows | Partly expresses user safety/recovery value | Seven one-paragraph placeholders; hidden platform/sibling prerequisites and broad multi-flow stories | **Not schedulable** |
| Epic 8 — Production Conformance and Release Evidence | Primarily a test/release technical milestone | Nine one-paragraph placeholders; no acceptance criteria or deterministic evidence contract; FR-24 not explicitly owned | **Not schedulable** |

### Critical Violations

1. **Twenty-three corrective placeholders are represented as stories.** Stories 6.1–6.7, 7.1–7.7, and 8.1–8.9 are descriptions, not stories. None has `As a / I want / So that`, BDD acceptance criteria, happy/error/recovery cases, verification commands, pass/fail artifacts, named repository/owner, estimate, or completion boundary. The PRD addendum explicitly classifies all 23 as findings inventory and forbids scheduling them until replaced.

2. **Story 1.4 violates FR-1/NFR-4.** It emits `ProjectCreated`, sets `Active`, and explicitly says creation without a Folder succeeds. The final contract forbids caller-visible or Active state until exactly one authorized Folder and read-model confirmation exist.

3. **Story 2.4 repeats the folderless activation defect.** Its acceptance criteria allow Folder creation to be queued/flagged “without blocking project creation.” This is incompatible with the absolute Folder-gated visibility/activation rule and makes the story's stated outcome false.

4. **Current requirements depend on future corrective epics.** Historical stories claim FR-1, FR-4, FR-6–FR-8, FR-11, FR-14, FR-15, and FR-21 coverage, but their final durable/confirmation behavior is deferred to later Epic 7. A story cannot be considered complete against the current requirement when compliance depends on future work.

5. **Story 5.12 can complete with failed acceptance evidence.** Its criteria permit a missing route/fixture/product prerequisite to be recorded as a blocker. The story was treated as completed despite 13/13 focused and 19/56 full pass/fail results. Under NFR-11, failed critical cases and unavailable prerequisites are `not verified`, not a successful story outcome.

6. **Epics 6 and 8 are technical milestones rather than user-value epics.** Platform migration, packaging, CI, supply chain, telemetry, fixtures, and release evidence are valuable constraints but should be sliced into user/operator outcomes or embedded as acceptance work for the capability slices they protect.

7. **FR-24 has no complete story owner.** Story 5.7 offers generic export UX; Stories 8.3 and 8.8 mention machine/performance concerns. No story owns the complete separately authorized, bounded, deterministic, audited, non-retained export outcome.

### Major Issues

1. **Epic 2 is not independently executable from Epic 1 alone.** Story 2.2 requires a separate Conversations repository capability, while Story 2.4 depends on Folders command exposure. These external prerequisites are described but lack approved, completed dependency records that a Projects story can consume without blocking mid-story.

2. **Technical/enabler stories lack direct independently demonstrable value.** Stories 1.2, 1.5, 1.9, 2.2, and 2.6 are taxonomy, test harness, topology, sibling capability, and decision-spike work. Some are necessary, but they should be bounded enablers with explicit consumer, exit evidence, and no claim of delivered product behavior, or folded into the first vertical slice that uses them.

3. **Story 1.9 conflicts with approved platform ownership.** It creates Projects-owned AppHost, Workers topology, Dapr components, ServiceDefaults, telemetry, and health plumbing that the final addendum assigns to EventStore/platform owners. It is both a technical story and architecturally stale.

4. **Stories 1.7, 3.2–3.5, 4.2–4.5 omit the final observable contract.** Acceptance criteria do not fully require `responseState`, `asOf`, `projectVersion`, component inclusion/freshness/reason evidence, Recovery Action Codes, or the binding `Complete`/`Partial`/`Unavailable`/`Denied` transitions.

5. **Stories 1.8, 2.3–2.5, 2.7, 4.4, and 4.5 omit current Preview/Durable Task rules.** They lack server-issued/single-use/version-bound confirmation, expiry/tamper/replay handling, scoped idempotency, lost-response recovery, task truth, and read-model-confirmed completion where the final PRD requires them.

6. **Story 5.1 uses the old audit scope.** It does not own the final task admission/outcome, confirmation use/cancellation/rejection, authorization denial, reconciliation, upstream receipt, and export attempt/outcome taxonomy, or the rule separating telemetry from durable audit.

7. **Story 5.9 is epic-sized and semantically inconsistent.** It bundles restore, relink, unlink, and reevaluation across UI lifecycle, validation, confirmation, and audit. These have different authorization and failure models; reevaluation is read-only FR-18 refresh, not a mutation.

8. **Stories 5.10 and 5.11 are oversized.** One combines the entire MCP and CLI surface; the other combines cross-surface semantics, four responsive ranges, full WCAG behavior, stable selectors, tenant isolation, and leakage proof. Each contains multiple independently failing outcomes and cannot be reliably completed or reviewed as one story.

9. **Story 7.6 is epic-sized.** It combines archive, restore, relink, and unlink durable workflows, each with different roles, targets, compensation, confirmation, and audit behavior.

10. **Corrective stories have no NFR/P1/P2 ownership matrix.** Epic completion gates name audit IDs such as `ARCH-001` and `REL-001`, but individual stories do not trace every NFR, nine P1 findings, seven P2 findings, or the deterministic evidence each must produce.

11. **Repository authority is ambiguous in corrective stories.** Stories 7.1 and 7.4 imply shared platform and Conversations changes without naming separate repository-local approvals, versions, commits, consumer contracts, or rollback conditions.

### Minor Concerns

- The Requirements Inventory and FR Coverage Map still state 22 FRs and nine NFRs, while the final contract contains 24 FRs and 11 NFRs.
- Story numbering mixes completed historical work and unschedulable corrective placeholders without a machine-visible status warning in each story entry.
- The Memories decision spike remains in the epic plan although `architecture.md` later claims it was resolved by an ADR; the plan does not reconcile or remove the obsolete uncertainty.
- Several ACs use architecture-specific class/method names as the outcome instead of observable behavior, increasing coupling to an architecture that is itself stale.
- No relational table-upfront violation was found; persistence is event/read-model based. However, read stores, topology, and platform plumbing should be introduced only with the first outcome slice that requires them, not as a standalone “all infrastructure” milestone.

### Required Remediation

1. Replace all 23 Epic 6–8 placeholders with outcome-based, independently completable stories before sprint planning.
2. Give every replacement story a user/operator beneficiary, bounded outcome, prior-only dependencies, explicit repository/owner, FR/NFR/finding traceability, Given/When/Then criteria, negative/recovery/concurrency cases where applicable, exact verification commands, and named pass/fail artifacts.
3. Re-slice technical migration and evidence work into vertical outcomes; retain narrowly bounded enablers only where they have an explicit consuming story and measurable exit gate.
4. Remove folderless activation from Stories 1.4 and 2.4 and make Folder authorization/creation plus read-model confirmation part of the same deliverable outcome.
5. Split multi-flow stories—especially 5.9, 5.10, 5.11, 7.6, and broad Epic 8 placeholders—along distinct authorization, state, failure, and evidence boundaries.
6. Add a complete FR-24 owner and reconcile the entire epic requirements/coverage inventory to FR-1–FR-24 and NFR-1–NFR-11.
7. Make verification stories pass the required critical cases; recording an environment blocker may document status but cannot satisfy completion.

## Summary and Recommendations

### Overall Readiness Status

## NOT READY

The PRD package is complete enough to serve as the governing product contract, but implementation must not begin from the current downstream artifacts. Architecture is obsolete and internally conflicted, UX omits required product journeys and final contracts, epic coverage stops short of FR-24, and all 23 corrective Epic 6–8 entries are explicitly unschedulable placeholders.

This result supersedes neither the prior `NOT_READY` assessment nor the blocked release handoff with a more permissive status. It confirms their central concern against the finalized July 15 PRD.

### Critical Issues Requiring Immediate Action

1. **No current architecture exists.** `architecture.md` is a 22-FR design that conflicts with the final PRD/addendum and approved platform ownership; the July 15 architecture spine is an untouched template.
2. **Twenty-three corrective entries are not stories.** Epics 6–8 cannot enter sprint planning because their entries lack user outcomes, acceptance criteria, dependency proof, ownership, evidence, and sizing.
3. **The plan violates the mandatory Folder invariant.** Stories 1.4 and 2.4 explicitly allow a Project to become Active without a confirmed Folder.
4. **FR-24 lacks complete epic/story coverage.** The separately authorized, bounded, deterministic, audited, non-retained Safe Diagnostic Export has no full implementation owner.
5. **Required Chatbot UX is absent.** Candidate confirmation, proposed creation, Preview renewal, lost-response recovery, task truth, and first-response admission behavior are outside the current UX scope despite being required by the final PRD.
6. **Confirmation, task, response, and recovery state models are not aligned.** Historical stories and UX can infer success from acknowledgement/notification and omit expiry, replay, tamper, version, idempotency, and read-model-confirmation rules.
7. **Platform/package boundaries are stale.** Current architecture assigns runtime/topology/UI responsibilities to Projects that the approved addendum assigns to EventStore/platform/FrontComposer owners and omits the non-packable `Hexalith.Projects.UI.Contracts` boundary.
8. **Failed evidence can be represented as story completion.** Story 5.12 completed with substantial live failures and missing prerequisites, contrary to NFR-11's no-false-pass rule.

### Recommended Next Steps

1. **Create a substantive current architecture.** Replace the template with an approved architecture spine and reconcile the detailed architecture to FR-1–FR-24, NFR-1–NFR-11, current DomainService/platform ownership, `UI.Contracts`, durable workflow/confirmation, migration/cutover, response/recovery semantics, FR-24, and repository authority boundaries.
2. **Update UX against the final contract.** Add the Chatbot companion journeys; map role/action authority, task/response/freshness/recovery states, and read-model confirmation; fully specify FR-24; correct read-only refresh and audit taxonomy; add MCP containment; and align page composition, Fluent V5 tokens/components, authenticated accessibility evidence, and cardinality behavior.
3. **Replace all 23 placeholders.** Rebuild Epics 6–8 as outcome-based, dependency-ordered stories with bounded scope, prior-only dependencies, repository ownership, BDD criteria, negative/recovery/concurrency cases, exact verification commands, evidence artifacts, and estimates.
4. **Repair the historical contradictions used by corrective planning.** Explicitly supersede the folderless activation criteria, old confirmation flows, obsolete audit scope, generic operator authority, and completion-on-blocker acceptance language so implementers cannot follow stale criteria.
5. **Complete traceability.** Produce one machine-checkable matrix mapping every FR, NFR, additional requirement, P1/P2 audit finding, UX journey, architecture decision, story, test environment/fixture, command, evidence artifact, owner, and release disposition. Add a complete FR-24 path and update the epic inventory from 22/9 to 24/11.
6. **Preserve containment.** Keep corrective implementation, production release, consequential autonomous MCP mutation, and proposal confirmation disabled until the repaired artifacts pass a new implementation-readiness assessment and the named release gates receive dated approval.
7. **Rerun this assessment before sprint planning.** The next readiness run should use only the reconciled architecture, UX, and replacement story set; historical artifacts may remain as evidence but must not be treated as current instructions.

### Final Note

This assessment recorded **37 findings across three validation categories**, plus three supporting warnings. Some findings are repeated cross-artifact manifestations of the same root defect—for example, FR-24 is missing from epic coverage, UX detail, and story ownership. The blockers are structural rather than cosmetic: address the critical issues before implementation or sprint planning.

**Assessment date:** 2026-07-15  
**Assessor:** Codex — Implementation Readiness / Product Management review
