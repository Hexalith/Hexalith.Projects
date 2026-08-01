---
stepsCompleted:
    - step-01-document-discovery
    - step-02-prd-analysis
    - step-03-epic-coverage-validation
    - step-04-ux-alignment
    - step-05-epic-quality-review
    - step-06-final-assessment
overallReadiness: NOT_READY
assessmentDate: 2026-08-01
assessor: Codex, independent implementation-readiness assessor
issueCounts:
    critical: 2
    majorOrAlignment: 8
    warningOrMinor: 4
includedFiles:
    prd:
        - _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md
        - _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/addendum.md
    architecture:
        - _bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md
        - _bmad-output/planning-artifacts/architecture.md
        - _bmad-output/planning-artifacts/epics-architecture-conformance-checklist-2026-07-16.md
    epics:
        - _bmad-output/planning-artifacts/epics.md
        - _bmad-output/planning-artifacts/epics-architecture-conformance-checklist-2026-07-16.md
    ux:
        - _bmad-output/planning-artifacts/ux-design-specification.md
supportingFiles:
    - _bmad-output/planning-artifacts/ux-design-directions.html
---

# Implementation Readiness Assessment Report

**Date:** 2026-08-01
**Project:** projects

## Document Discovery

### PRD Files Found

**Canonical Documents:**

- `prds/prd-Hexalith.Projects-2026-05-24/prd.md` (46,095 bytes; modified 2026-07-15 08:50 CEST)
- `prds/prd-Hexalith.Projects-2026-05-24/addendum.md` (24,867 bytes; modified 2026-07-20 00:00 CEST)

**Folder-Based Supporting Documents:**

- The PRD folder contains 23 additional decision-log, memory-log, editorial-review, reconciliation, handoff, and validation files.
- The folder does not contain an `index.md`; the canonical files above were explicitly confirmed for this assessment.

### Architecture Files Found

**Canonical Document:**

- `architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md` (54,848 bytes; modified 2026-07-18 08:38 CEST)

**Supporting Documents:**

- `architecture.md` (51,607 bytes; modified 2026-07-16 20:11 CEST)
- `epics-architecture-conformance-checklist-2026-07-16.md` (20,715 bytes; modified 2026-08-01 16:09 CEST)
- The architecture folder contains seven additional review and memory-log files.
- The folder does not contain an `index.md`; the canonical file above was explicitly confirmed for this assessment.

### Epics and Stories Files Found

**Canonical Document:**

- `epics.md` (186,007 bytes; modified 2026-08-01 16:11 CEST)

**Supporting Document:**

- `epics-architecture-conformance-checklist-2026-07-16.md` (20,715 bytes; modified 2026-08-01 16:09 CEST)

No sharded epics-and-stories document was found.

### UX Design Files Found

**Canonical Document:**

- `ux-design-specification.md` (62,694 bytes; modified 2026-08-01 16:10 CEST)

**Supporting Document:**

- `ux-design-directions.html` (25,390 bytes; modified 2026-05-29 07:59 CEST)

No sharded UX document was found.

### Discovery Resolution

- The user confirmed the canonical and supporting document selections on 2026-08-01.
- The existing `implementation-readiness-report-2026-08-01.md` was preserved.
- This assessment continues in the separate `implementation-readiness-report-2026-08-01-rerun.md` report.
 
## PRD Analysis

### Functional Requirements

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

**Total FRs: 24**

### Non-Functional Requirements

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

**Total NFRs: 11**

### Additional Requirements

#### AR-1: Product and Release Boundary

- v1 is an internal Hexalith.Chatbot platform module rather than a standalone project-management product.
- Project Lifecycle State is exactly `Active` or `Archived`; Durable Task status is a separate concern.
- A Conversation belongs to exactly one Project, and every Active Project has exactly one authorized Project Folder.
- Projects stores metadata and references only. Conversations, Folders, and Memories retain payload and authorization ownership.
- No approved v1 FR or NFR is deferrable from production release. Core-only delivery is internal evidence, not production authorization.
- Projects must not persist transcripts, file contents, prompts, secrets, Memory payloads, unrestricted paths, candidate-score history, Resolution Traces, or generated diagnostic exports.

#### AR-2: Observable Context and Recovery Contract

- Open, list, resolution, context, Conversation-start, and proposal-recovery responses preserve `responseState`, `asOf`, `projectVersion`, applicable `resolutionResult`, metadata-only component evidence, and Recovery Action Codes.
- `Complete` requires current mandatory evidence. `Partial` permits use only when mandatory admission evidence is current and every optional omission is represented. `Unavailable` blocks initialization or resumption. `Denied` discloses no protected metadata.
- Refresh recomputes authorization and evidence instead of rewriting prior responses. Expired or stale confirmation returns `RenewPreview`; lost admission responses recover through `PollTask` or equivalent idempotent retry.
- `WaitingForDependency` and `NeedsAttention` remain truthful nonterminal recovery states; terminal task states are immutable.

#### AR-3: Durable Workflow and Platform Ownership

- Architecture must define durable checkpoints, worker ownership, leases, restart recovery, two-instance convergence, duplicate delivery, lost responses, cancellation cutoff, terminal-state immutability, cross-context receipts, compensation, and reconciliation.
- Read-model confirmation is the completion authority; neither request acknowledgement nor SignalR notification proves completion.
- Projects owns domain policy, contracts, and Project-specific task transitions. Hexalith.EventStore owns reusable hosting, persistence/publication, subscriptions, read-model stores, cursors, health, telemetry, and durable-workflow capability.
- The platform AppHost owns distributed topology. FrontComposer/platform hosts own Web, MCP, and CLI composition.
- Production-capable hosts must carry real credentials and delegated service identity and must not register allow-all development identity or authorization stubs.

#### AR-4: Preview, Confirmation, and Idempotency

- Architecture must define Preview and Confirmation Artifact schemas, signing and key ownership, normalized request material, resource-version binding, 15-minute expiry, single-use enforcement, replay behavior, and safe renewal.
- Idempotency is scoped by `(Tenant, actor, operation, key)`. Equivalent reuse returns the original task; materially different reuse conflicts.
- Idempotency records remain available for at least 30 days and never less than the associated result lifetime.
- Canonicalization must be Unicode-safe, including U+2028/U+2029 parity, without broadening request equivalence.
- Cancellation is allowed before the irreversible commit point; after it, callers receive conflict or safe status.
- Safe reason codes distinguish expired, stale, replayed, tampered, unauthorized, dependency-waiting, and reconciliation-required outcomes without protected detail.

#### AR-5: Safe Diagnostic Export Contract

- The versioned `projects.safe-diagnostic-export.v1` representation must remain semantically consistent across Web, CLI, and MCP.
- The complete encoded response is capped at 1 MiB, 500 reference rows, and 100 audit rows, with at most two concurrent exports per Tenant.
- Ordering and stable tie-breaking are deterministic; truncation includes safe counts and reasons.
- Exports have no continuation cursor, are not retained, expose no raw upstream problem, and require separate permission plus metadata-only attempt/outcome audit.

#### AR-6: Create Project Metadata Classification

- Canonical Create Project requests use one of `public_metadata`, `tenant_sensitive`, `credential_sensitive`, or `secret`; classification is supplied by an authenticated integration adapter, not inferred from user text.
- Only the historical unversioned name-only request receives v1 compatibility treatment.
- Missing, blank, null, non-string, case/whitespace variants, duplicate properties, and unknown canonical values are invalid.
- Authorization precedes protected parsing. Invalid classification returns `400 ValidationFailure` with `details.rejectedField = projectMetadata.metadataClass`, echoes no rejected value, and submits no command.
- Direct creation and proposal confirmation share the server-owned `SensitiveMetadataTierValidator`.

#### AR-7: Package and Shared-Build Boundaries

- `Hexalith.Projects.UI.Contracts` is a non-packable descriptor host that depends on the UI-free Contracts kernel; it must not make that kernel depend on FrontComposer Shell, Fluxor, Fluent UI, or `Microsoft.AspNetCore.App`.
- MCP and CLI remain independent of the UI descriptor host. Contract/package release remains gated by Story 6.2 consumer, non-packability, dependency, isolation, accessibility, and leakage evidence.
- Hexalith.Builds is the single version owner for `NSwag.MSBuild` `14.7.1` and `Fluxor.Blazor.Web` `6.9.0`; Projects uses versionless references and preserves central transitive pinning.
- Repository-local upstream work requires its own approved story. The PRD does not authorize sibling-repository mutations by implication.

#### AR-8: Migration and Compatibility

- Legacy Active folderless Projects and in-flight Folder work must be inventoried and reconciled before list, resolution, or context admission.
- Event evolution is additive and preserves historical readability; event history is not rewritten.
- Planning must cover compatibility adapters, replay comparison, value-slice cutover, routing rollback, retirement evidence, archived Projects with missing or unauthorized Folders, and avoidance of unsafe dual writes.

#### AR-9: Chatbot Companion and Accessibility

- Chatbot owns end-user presentation; Projects owns versioned Preview, Confirmation Artifact, Durable Task, Resolution, and Context contracts.
- Companion UX must provide unselected candidate comparison, explicit confirmation/cancellation, expiry and staleness recovery, lost-response retry, task status, keyboard/focus handling, live announcements, 200% zoom, and 320-CSS-pixel responsive behavior.
- Safe denial and degradation must never infer completion from acknowledgement or SignalR.

#### AR-10: Verification and Release Evidence

- Test evidence must cover deterministic small, median, and maximum fixtures; authenticated persisted boundaries; cross-Tenant denial; authorization freshness; encryption/KMS; replay/tamper; privacy; restart; concurrency; duplicate delivery; cancellation; lost response; compensation; reconciliation; and read-model confirmation.
- Web, CLI, and MCP must preserve authorized facts and safe failure-category parity.
- Accessibility requires both automated checks and authenticated manual keyboard/screen-reader evidence.
- Performance/outcome measurement is metadata-only and must not include Conversation text, Project name, prompt, path, foreign payload, or secret.
- Failed critical cases and unexplained critical skips block release; environment absence is `not verified`.
- Live Playwright has deterministic and opt-in live lanes. The live lane discovers the ready `projects-ui` endpoint dynamically, rejects guessed URLs, uses Aspire-managed teardown, and converts permanent `test.fixme` declarations into conditional tests with explicit prerequisites.

### PRD Completeness Assessment

- **Requirement structure:** Complete and contiguous. The authoritative PRD defines 24 FRs and 11 NFRs with no missing or duplicate identifiers.
- **Testability:** Strong. FR consequences and NFRs contain explicit lifecycle, authorization, capacity, latency, retention, recovery, accessibility, compatibility, and release-evidence criteria.
- **Scope clarity:** Strong. Product boundaries, systems of record, exclusions, role authority, lifecycle vocabulary, and production-release containment are explicit.
- **Downstream delegation:** Deliberate. Exact wire schemas, cryptographic mechanisms, durable-workflow persistence, topology, and test harness details are assigned to architecture, API-contract, UX, and test artifacts rather than left as implicit product decisions.
- **Supersession clarity:** Adequate but dense. The addendum contains historical readiness and gate references; downstream validation must honor its explicit supersession chain, especially the 2026-07-17 `READY` planning authorization and the continuing Story 8.11 production-release gate.
- **Initial conclusion:** The PRD set is complete enough for coverage validation. This conclusion does not establish epic/story coverage or implementation readiness; those are assessed in subsequent steps.

## Epic Coverage Validation

### Epic FR Coverage Extracted

The production-authority coverage map in `epics.md` assigns the PRD requirements as follows:

- FR-1: Story 7.1
- FR-2: Story 6.1
- FR-3: Story 7.2
- FR-4: Story 7.13
- FR-5: Story 6.1
- FR-6: Story 7.3
- FR-7: Story 7.4
- FR-8: Story 7.6, with initial Folder binding through Story 7.1
- FR-9: Story 7.7
- FR-10: Story 7.9
- FR-11: Stories 7.5, 7.8, and 7.10
- FR-12: Story 6.4
- FR-13: Story 6.4
- FR-14: Story 7.11, with candidate reads through Story 6.4
- FR-15: Story 7.12
- FR-16: Story 6.3
- FR-17: Stories 6.3 and 6.4
- FR-18: Story 6.3
- FR-19: Story 7.1, with contract cutover through Story 6.7
- FR-20: Story 6.2
- FR-21: Story 8.1, with audit production embedded in the Epic 7 shared durable-workflow invariants
- FR-22: Stories 6.5, 6.6, 8.1, 8.3, 8.4, and 8.5
- FR-23: Story 7.14
- FR-24: Story 8.2

**Total FRs claimed in the production-authority map: 24**

### Coverage Matrix

| FR Number | PRD Requirement | Production Epic Coverage | Status |
| --- | --- | --- | --- |
| FR-1 | Create a Project through a Folder-first, idempotent Durable Task and expose it only after read-model-confirmed activation. | Story 7.1 | ✓ Covered |
| FR-2 | Open an authorized Project with lifecycle, setup, references, and observable context/recovery state. | Story 6.1 | ✓ Covered |
| FR-3 | Update durable Project Setup safely and idempotently. | Story 7.2 | ✓ Covered |
| FR-4 | Archive an Active Project through Preview, confirmation, and a Durable Task. | Story 7.13 | ✓ Covered |
| FR-5 | List authorization-filtered Active and Archived Projects with scoped pagination. | Story 6.1 | ✓ Covered |
| FR-6 | Link an existing Conversation while preserving single-Project membership. | Story 7.3 | ✓ Covered |
| FR-7 | Move a Conversation between Projects through confirmed, recoverable work. | Story 7.4 | ✓ Covered |
| FR-8 | Establish or replace the exactly-one authorized Project Folder. | Stories 7.1 and 7.6 | ✓ Covered |
| FR-9 | Link an authorized File Reference without changing the Project Folder. | Story 7.7 | ✓ Covered |
| FR-10 | Link an authorized Memory by stable identity and metadata only. | Story 7.9 | ✓ Covered |
| FR-11 | Unlink Conversation, File, or Memory associations without deleting resources. | Stories 7.5, 7.8, and 7.10 | ✓ Covered |
| FR-12 | Resolve Candidate Projects from a Conversation. | Story 6.4 | ✓ Covered |
| FR-13 | Resolve Candidate Projects from authorized Folder/File attachments. | Story 6.4 | ✓ Covered |
| FR-14 | Confirm one accessible candidate from an ambiguous resolution. | Stories 6.4 and 7.11 | ✓ Covered |
| FR-15 | Propose and confirm a new Project without creating from unconfirmed inference. | Story 7.12 | ✓ Covered |
| FR-16 | Retrieve allowlist-assembled, metadata-only Project Context. | Story 6.3 | ✓ Covered |
| FR-17 | Explain current context inclusion/exclusion without persisting traces. | Stories 6.3 and 6.4 | ✓ Covered |
| FR-18 | Refresh Project Context as a read-only recomputation. | Story 6.3 | ✓ Covered |
| FR-19 | Validate setup and canonical Metadata Classification before admission. | Stories 6.7 and 7.1 | ✓ Covered |
| FR-20 | Retrieve the authorized Conversation-start setup subset with admission truth. | Story 6.2 | ✓ Covered |
| FR-21 | Record metadata-only audit truth for consequential admissions and outcomes. | Story 8.1 and Epic 7 shared invariants | ✓ Covered |
| FR-22 | Provide Tenant-scoped, metadata-only operator read access across supported surfaces. | Stories 6.5, 6.6, 8.1, 8.3, 8.4, and 8.5 | ✓ Covered |
| FR-23 | Restore an Archived Project with Folder validity established before activation. | Story 7.14 | ✓ Covered |
| FR-24 | Produce a separately authorized, bounded, non-retained Safe Diagnostic Export. | Story 8.2 | ✓ Covered |

### Missing Requirements

No PRD Functional Requirement identifier is missing from the production-authority coverage map. No FR identifier appears in the epics document that is absent from the PRD.

This is an identifier-level coverage result only. Whether each mapped story is independently implementable and whether its acceptance criteria preserve the full requirement semantics is assessed in the later story-quality step.

### Coverage Statistics

- Total PRD FRs: 24
- FRs represented in production-authority epics: 24
- Missing PRD FR identifiers: 0
- Extra epic FR identifiers: 0
- Identifier coverage: 100%

## UX Alignment Assessment

### UX Document Status

**Found and complete:** `ux-design-specification.md` has workflow status `complete` and covers the Projects-owned administrative/operational UX across Web, CLI, and MCP. It explicitly treats the direct end-user Chatbot presentation as separately owned while making its approved companion UX artifact a release input.

No sharded UX document was found. `ux-design-directions.html` is a supporting prototype and is correctly treated as non-normative.

### Alignment Summary

| Area | PRD Alignment | Architecture Support | Assessment |
| --- | --- | --- | --- |
| Product and UX scope | UX preserves Projects as a metadata control plane and keeps end-user Chatbot presentation outside Projects ownership. | AD-1, AD-2, AD-24, and AD-29 assign domain, runtime, adapter, and Chatbot ownership explicitly. | Aligned |
| Context and recovery states | UX uses `Complete`, `Partial`, `Unavailable`, `Denied`, component inclusion/freshness, and the canonical Recovery Action Codes. | AD-19 and AD-32 define one observable transport and response snapshot for every adapter. | Aligned |
| Resolution and Project Context | UX exposes candidates, reason codes, inclusion/exclusion evidence, explicit ambiguity, and fail-closed context without payloads. | AD-7, AD-10, AD-11, AD-14, and AD-32 support current-only resolution, the Reference Trust Index, and allowlist context assembly. | Aligned, subject to Issue UX-A2 below |
| Consequential actions | UX presents Preview, Confirmation Artifact, Durable Task, authoritative polling, expiry/staleness renewal, cancellation boundaries, and `NeedsAttention`. | AD-4, AD-5, AD-9, AD-12, AD-13, and AD-19 define the durable state machine and observable mappings. | Aligned, subject to Issue UX-A1 below |
| Cross-surface authority and parity | UX keeps Web, CLI, and MCP on one action/state/reason-code model and prevents agent authority expansion. | AD-16, AD-19, AD-20, AD-29, and AD-33 generate and constrain all adapters from the canonical Contracts model. | Aligned |
| Safe Diagnostic Export | UX preserves separate authorization, synchronous execution, 1 MiB/500-reference/100-audit bounds, deterministic ordering, two-Tenant leases, no cursor, and no retention. | AD-21 and AD-27 provide the query, lease, audit, and back-pressure substrate. | Aligned |
| Metadata-only audit and privacy | UX excludes transcripts, file/memory content, prompts, secrets, unsafe summaries, and protected existence signals. | AD-20, AD-21, AD-26, and AD-28 define dual-principal authorization, metadata-only channels, and platform security ownership. | Aligned, subject to Issue UX-A3 below |
| Accessibility and responsive behavior | UX specifies keyboard/focus/live-region behavior, non-color status, 200% zoom, 320 CSS-pixel reflow, responsive layouts, and automated plus manual evidence. | AD-30 and AD-34 make authenticated accessibility evidence release-blocking; AD-24/AD-29 place presentation in the platform adapters. | Aligned |
| Performance and degraded-state feedback | UX distinguishes loading, stale, rebuilding, partial, unavailable, retry, and recovery states. | AD-14, AD-27, and AD-28 implement bounded reads, performance targets, quotas, timeouts, and truthful health. | Aligned |

### Alignment Issues

#### UX-A1 — Confirmation language is broader than the canonical admission classification

Several UX passages say to "preview every mutation" or that confirmations are required for all mutating actions. The PRD and AD-5 deliberately distinguish confirmation-required actions from actor-selected task-only actions. Direct creation, Setup update, additive Conversation/File/Memory links, and initial Folder setting are Durable Task-only and do not require a second confirmation.

**Impact:** Implementers could add unnecessary Confirmation Artifact flows and diverge from the generated action-admission contract.

**Required correction:** Qualify these UX statements as applying to **confirmation-required mutations**. Keep the canonical action classification in AD-5 and `Projects.Contracts` authoritative.

#### UX-A2 — “Replay,” “resolution case ID,” and returning-later trace language can imply persisted inference history

The UX specification correctly states elsewhere that Resolution Traces are current and nonpersistent, but residual phrases such as “resolution replay,” starting from a “resolution case ID,” and reconstructing from “diagnostic traces” can be read as persisted trace lookup.

**Impact:** A presentation or API design could accidentally create a persisted Resolution Trace identity/history, violating the PRD boundary and AD-7.

**Required correction:** Define replay as a fresh, read-only recomputation. Treat any case identifier as request-scoped correlation metadata, not a retained trace identifier. Returning-later reconstruction must use durable metadata-only audit outcomes, not inference history.

#### UX-A3 — `dry-run event` is not part of the durable audit contract

The Audit Timeline component lists a `dry-run event` state, while FR-21 and AD-26 enumerate durable audit truth without classifying ordinary Preview/dry-run diagnostics as durable audit events. The architecture separates operational telemetry from durable audit and requires audit only for the specified admissions, confirmations, denials, mutations, reconciliation, exports, and terminal outcomes.

**Impact:** Implementers could retain diagnostic/Preview detail or create inconsistent audit records across surfaces.

**Required correction:** Either classify dry-run/Preview feedback as transient operational telemetry, or explicitly amend the product and architecture audit contract before making it durable. Never retain inference or payload detail.

### Warnings

#### UX-W1 — The separately owned Chatbot companion artifact remains a release dependency

The Projects UX specification does not and cannot replace the version-pinned Chatbot companion UX artifact. The epics identify package `8.8-P3` as open and require an approved immutable Chatbot revision plus authenticated candidate, proposal, confirmation, recovery, first-response-admission, and accessibility evidence. Its absence blocks Stories 8.8 and 8.11.

#### UX-W2 — Presentation implementation support is gated by unresolved platform/toolchain entries

The Architecture Spine supports the UX through FrontComposer/platform adapters, but G-3 still requires the FrontComposer 4.0.0 package versus 4.0.1 source disposition and authenticated adapter parity. G-6 still requires approval/evidence for Fluent UI V5 RC and related prerelease/toolchain bindings. The design is aligned; implementation evidence remains conditional on these gates.

### UX Alignment Conclusion

The UX, PRD, and Architecture Spine share a coherent product model and no major user journey or architectural presentation capability is absent. The three issues above are bounded terminology/channel corrections rather than missing design foundations. The external Chatbot companion artifact and platform presentation gates remain material implementation-readiness dependencies.

## Epic Quality Review

### Review Scope

- Eight epics and 71 story definitions were reviewed.
- Epics 1–5 contain 38 historical stories and are explicitly non-schedulable implementation history.
- Epics 6–8 contain the 33 production-authority stories: 7 in Epic 6, 15 in Epic 7, and 11 in Epic 8.
- The external prerequisite and evidence packages were assessed as entry gates, not misrepresented as user stories.

### Epic Compliance Summary

| Epic | User/Stakeholder Outcome | Independence and Dependency Direction | Story/AC Quality | Result |
| --- | --- | --- | --- | --- |
| Epic 1 — Project Workspace Foundation | Mixes technical foundation with create/open/list/update/archive value. | Historical Story 1.4 deferred the required Folder outcome to later work and therefore was not standalone for the current FR-1 contract. It is explicitly superseded by Story 7.1. | BDD criteria are detailed, but the folderless-Active criteria contradict current authority. | Historical only; must remain non-schedulable |
| Epic 2 — Context References | Delivers Conversation/Folder/File/Memory association value. | Explicit dependencies are prior-only: 2.3→2.2 and 2.7→2.6. Historical Story 2.4 tolerated a folderless interval and is superseded. | Mostly testable; Story 2.6 is a technical decision spike rather than user value. | Historical only; contained |
| Epic 3 — Project Context Assembly | Delivers authorized context, explanation, refresh, and Conversation-start setup. | Uses only Epics 1–2 and prior stories. | Clear, testable BDD criteria with negative paths. | Historical evidence passes structure |
| Epic 4 — Project Resolution | Delivers candidate resolution, ambiguity confirmation, and proposal behavior. | Uses only prior Epic 1–2 outputs; 4.5→2.2 is backward. | Clear value and testable criteria. | Historical evidence passes structure |
| Epic 5 — Operational Console & Audit | Delivers operator inspection and maintenance surfaces. | Historical Story 5.9 described restore before the production restore workflow existed, and Story 5.7 predates the binding FR-24 export contract. Both are superseded by Epics 7–8. | Rich BDD criteria, but historical/current semantics coexist. | Historical only; must not drive implementation |
| Epic 6 — Authorized Project Reads | Delivers operator/Chatbot list, open, context, resolution, Web, and CLI read outcomes. | No dependency on Epic 7 or 8. Internal comparisons reference only prior stories. External entry gates are not accepted. | Strong traceability, BDD, evidence, estimate, and completion-boundary structure. | Structurally sound but blocked |
| Epic 7 — Durable Project Decisions | Delivers all consequential creation, association, lifecycle, confirmation, and recovery outcomes. | Depends on completed Epic 6 and G-1/G-2; no forward story dependency was found. | Strong shared invariants and per-story negative/recovery paths. Role and audit ownership defects remain below. | Structurally sound but blocked |
| Epic 8 — Safe Operations and Release Confidence | Delivers operator adapters, bounded export, observability, resilience, and release-owner decision value. | Uses Epics 6–7 and earlier Epic 8 outputs only. The 8.8 and 8.11 packages are explicit prior-entry evidence. | Strong evidence contracts; Story 8.10 is oversized. Several external evidence packages remain open. | Structurally sound but blocked |

### 🔴 Critical Violations

#### EQ-C1 — The production backlog has no executable first story

Story 6.1 is explicitly `blocked-external`. Its mandatory 6.1-P0, P1R, P2, P3, and P4 work packages are open; the current package/runtime candidate is not accepted at immutable revisions; required owner approvals and rollback proof are absent. Epic 7 additionally depends on unaccepted G-1/G-2 Durable Task and sibling-owner contracts.

This is not a forward-story dependency, but it violates implementation readiness: a developer cannot complete the first value story from the current accepted repository state.

**Required remediation:** Keep every production story out of `ready-for-dev`. Accept P0/P1R/P2/P3/P4 with immutable repository/package pins, named owner approvals, reproducible evidence, and executable rollback; then run the independent readiness assessment again.

#### EQ-C2 — Story verification commands are target contracts, not executable gates

The production stories rely on `hexalith-module` and `hexalith-evidence` commands. The Architecture Spine and epics explicitly state that the G-4 tools/runner are target, uncommitted capabilities and are not claims of current existence. Therefore the acceptance criteria cannot presently be verified from a clean checkout.

**Required remediation:** Publish and remotely restore the approved pinned tools, check in a valid manifest/tool lock, execute all five Story 6.1 clean-checkout commands successfully, retain their artifacts, and prove teardown/rollback. Local or stale binaries are inadmissible.

### 🟠 Major Issues

#### EQ-M1 — Production story role coverage is narrower or ambiguous for Project User association actions

AD-33 and the PRD allow an action-authorized Project User to move/unlink associations and replace the Project Folder. Story 7.6 is framed only for a Tenant Project Administrator; Stories 7.4, 7.5, 7.8, and 7.10 use “delegated caller” without explicitly proving that the original Project User authority is preserved.

**Impact:** A conforming server or generated adapter could omit Project User authorization paths despite the coverage map claiming full FR-7/FR-8/FR-11 coverage.

**Recommendation:** Update personas and acceptance criteria to enumerate the allowed AD-33 roles and assert direct/delegated parity plus negative cases for Tenant Operator and unauthorized callers.

#### EQ-M2 — FR-21 audit production has no single complete owning acceptance matrix

The coverage map assigns FR-21 to Story 8.1, but Story 8.1 primarily inspects audit/task truth. Audit production is distributed across the Epic 7 shared invariants and Story 8.2. No one production story or canonical matrix explicitly tests the complete FR-21 emission/deduplication taxonomy: task admission and terminal outcome, confirmation use/cancel/rejection, authorization denial, every confirmed mutation, reconciliation, upstream receipts, and export attempts/outcomes, while excluding polls/retries/notifications/unused expiry/current traces.

**Impact:** Individual workflows may implement inconsistent audit channels even though the FR identifier is mapped.

**Recommendation:** Add a canonical FR-21 audit-emission matrix to Story 8.1 or a dedicated prior story and require every producer to pass it, including equivalent-idempotency deduplication and telemetry-vs-audit separation.

#### EQ-M3 — Story 8.10 is epic-sized evidence work

Story 8.10 attempts to prove restart, two-instance execution, retry, duplicate delivery, concurrency, partial failure, lost response, compensation, reconciliation, RTO, RPO, and idempotency across multiple critical workflows in one `L` story.

**Impact:** The story is difficult to estimate, complete, or diagnose independently; one failing workflow can hide progress and ownership across the entire resilience envelope.

**Recommendation:** Split the evidence into independently completable slices—for example task-engine restart/lease convergence, cross-owner saga recovery, duplicate/concurrency/idempotency, and legacy reconciliation—then retain 8.10 as a small integration/acceptance roll-up.

#### EQ-M4 — Required external Chatbot UX evidence is unpinned and unaccepted

Package 8.8-P3 has no approved Chatbot owner revision or executable companion evidence. This is an explicit dependency for Story 8.8 and the terminal Story 8.11 release decision.

**Impact:** The NFR-9 and SM-5 end-to-end user journey cannot be completed by the current plan alone.

**Recommendation:** Obtain separate Chatbot repository authorization, immutable revision and contract version, owner approval, fixtures, exact commands, artifacts, rollback/containment, and acceptance before Story 8.8 can enter development.

#### EQ-M5 — Historical and production-authority stories coexist with contradictory criteria

The document clearly marks Epics 1–5 as history, yet retains criteria such as caller-visible folderless creation and pre-production restore/export behavior that directly contradict current FRs and Epics 6–8.

**Impact:** Story generation or implementation agents can select superseded criteria from the same canonical file.

**Recommendation:** Preserve history in a separately indexed historical appendix/file and keep the schedulable production document limited to Epics 6–8 plus explicit trace links. At minimum, machine-readable story selection must reject Epics 1–5.

### 🟡 Minor Concerns

#### EQ-N1 — Shared Epic 7 invariants must be copied into standalone story specifications

Epic 7 stories rely on “shared invariants 1–6” for core task, confirmation, idempotency, saga, audit, and compatibility behavior. This is concise in the epic document but insufficient if a generated story file omits those clauses.

**Recommendation:** Story creation must materialize every applicable invariant and its evidence expectations in each standalone story file rather than leaving only a cross-reference.

#### EQ-N2 — Stories 6.3 and 8.3 are broad but still coherent vertical slices

Story 6.3 combines context retrieval, refresh, and explanation; Story 8.3 covers the Web action matrix and accessibility behavior. Both are coherent reuse-heavy slices, but they should be split if estimation or test-fixture ownership becomes non-uniform.

### Best-Practices Checks Passed

- Production Epics 6–8 describe observable user, operator, test-owner, or release-owner outcomes rather than database/model setup milestones.
- Production epic dependency direction is valid: Epic 6 does not require 7/8; Epic 7 uses Epic 6; Epic 8 uses 6/7.
- No explicit forward story dependency exists in the production plan.
- Production stories consistently include a beneficiary, observable outcome, traceability, BDD criteria, negative/recovery paths, verification command, evidence artifact, estimate, and completion boundary.
- No up-front database/table-creation story exists; EventStore/read-model storage is introduced by the value slices that need it.
- The current plan is correctly treated as brownfield and includes compatibility, shadow-read, reversible cutover, single-writer, legacy reconciliation, package-boundary, and rollback work.
- Historical Story 1.1 records the initial scaffold from established sibling patterns; no missing greenfield starter-template action affects the current brownfield plan.

### Epic Quality Conclusion

The production epic sequence is unusually disciplined in traceability, dependency direction, BDD structure, and containment. It is nevertheless **not implementable now**: the first story and its verification substrate are explicitly blocked, required external evidence is unresolved, and the FR-8/FR-21 story contracts need correction. Historical contradictions must remain mechanically excluded from scheduling.

## Summary and Recommendations

### Overall Readiness Status

# NOT READY

The planning set has strong requirements coverage and a coherent architecture, but Phase 4 implementation must not begin. Story 6.1 has no accepted executable entry path, its required clean-checkout verification tools are still target capabilities, and its prerequisite revisions/approvals/evidence are incomplete. Later production stories also retain unresolved role, audit, sizing, companion-UX, and external platform gates.

This result supersedes neither release containment nor external owner decisions. Production release remains separately blocked until Story 8.11 passes with dated terminal acceptance from Jerome and John.

### Assessment Summary

- **Document set:** Complete enough to assess after explicit canonical-file selection; no valid sharded indexes exist.
- **PRD:** Complete and contiguous: 24 FRs and 11 NFRs with strong, testable consequences.
- **FR identifier coverage:** 24 of 24 mapped to production-authority stories; no extra or missing FR identifiers.
- **UX/architecture alignment:** Fundamentally coherent, with three bounded wording/channel defects and two dependency warnings.
- **Epic dependency direction:** Valid for Epics 6–8; no production forward-story dependency was found.
- **Story structure:** Generally strong, but two critical readiness blockers, five major story/backlog defects, and two minor concerns remain.

### Critical Issues Requiring Immediate Action

1. **No executable Story 6.1 entry path.** Work packages 6.1-P0, P1R, P2, P3, and P4 remain open or unaccepted. Required immutable pins, owner approvals, qualification evidence, and rollback are absent.
2. **Verification substrate is unavailable.** The required `hexalith-module` runner and `hexalith-evidence` validator are target/uncommitted G-4 capabilities, so the clean-checkout commands and story evidence gates cannot currently run.

### Required Planning Corrections

1. Amend UX wording so only canonical **confirmation-required** mutations require Preview/Confirmation Artifact; define resolution “replay” as current recomputation; remove or explicitly authorize durable `dry-run` audit semantics.
2. Align Stories 7.4–7.6, 7.8, and 7.10 with the explicit AD-33 Project User/Operator/Administrator action matrix, including delegated-principal parity and denial cases.
3. Give FR-21 a canonical audit-emission and exclusion matrix with one accountable story/evidence owner.
4. Split Story 8.10 into independently verifiable resilience slices and retain a small integration roll-up.
5. Mechanically exclude historical Epics 1–5 from story generation and scheduling; preferably move their contradictory criteria into a separately indexed historical artifact.
6. Materialize all applicable Epic 7 shared invariants inside each standalone story specification.

### External Acceptance Work

1. Publish and pin the G-4 runner/evidence tools as remotely restorable packages; check in the approved tool manifest and module manifest.
2. Revalidate and accept the current EventStore/platform package, runtime, architecture, and rollback tuple through P1R, then accept P2/P3/P4 with all accountable owners.
3. Accept G-1 Durable Task/Confirmation capabilities and G-2 Conversations/Folders/Memories owner contracts before Epic 7 stories can become executable.
4. Resolve G-3 FrontComposer package/source parity, G-5 identity/KMS/environment evidence, and G-6 runtime/prerelease/toolchain alignment before their consuming stories.
5. Obtain the separately authorized, immutable Chatbot companion UX/evidence package 8.8-P3 before Story 8.8 and release acceptance.

### Clean-Checkout Gate Before Reassessment

The accepted repository state must successfully execute and retain evidence for:

```text
dotnet tool restore
dotnet tool run hexalith-module run --manifest module/hexalith-projects.module.json
dotnet tool run hexalith-module test --manifest module/hexalith-projects.module.json --profile full
dotnet tool run hexalith-module down --manifest module/hexalith-projects.module.json
dotnet tool run hexalith-evidence validate _bmad-output/planning-artifacts/implementation-readiness-traceability-matrix.yaml
```

No local-only, stale, mutable, failed, skipped, blocked, or unavailable critical evidence may be recorded as passing.

### Recommended Next Steps

1. Apply the local UX and epic/story corrections above and revalidate the resulting artifacts.
2. Close P0/P1R/P2/P3/P4 and supply the executable clean-checkout evidence package.
3. Close the applicable G-1 through G-6 external capability records with immutable pins and owner approvals.
4. Pin and accept the Chatbot companion package and split the oversized resilience story.
5. Run this independent implementation-readiness workflow again. Story 6.1 may enter `ready-for-dev` only if the superseding assessment returns exactly `READY`.

### Final Note

This assessment recorded **14 findings requiring attention across UX alignment, epic/story quality, and external implementation gates**: 2 critical blockers, 8 major/alignment issues, and 4 warnings/minor concerns. The artifacts are close in traceability and conceptual alignment, but the blockers are objective and executable: implementation cannot be verified or safely started from the current accepted state.

**Assessment date:** 2026-08-01  
**Assessor:** Codex, independent implementation-readiness assessor
