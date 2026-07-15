---
title: "Sprint Change Proposal: Planning Rebaseline After Implementation Readiness Assessment"
status: approved
created: 2026-07-15
approved: 2026-07-15
approved_by: Jerome
project: Hexalith.Projects
prepared_for: Jerome
scope: major
mode: batch
trigger_report: _bmad-output/planning-artifacts/implementation-readiness-report-2026-07-14.md
amends: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-14-implementation-readiness-correction.md
---

# Sprint Change Proposal: Planning Rebaseline After Implementation Readiness Assessment

## 1. Issue Summary

### Trigger

Story 5.12 produced executable live-topology evidence after Epics 1–5 had been treated as
implementation history. Jerome then approved the Major-scope July 14 implementation-readiness
correction, which established the strategic direction and added corrective Epics 6–8 to the
planning and sprint-tracking artifacts.

The independent implementation-readiness assessment completed later on July 14 found that the
approved correction had not yet been materialized into an implementation-ready planning baseline:

- all 22 PRD functional requirements are allocated to epics;
- 29 issues remain across requirements, UX/architecture alignment, and epic quality;
- all 23 corrective entries in Epics 6–8 remain one-line placeholders;
- the PRD, architecture, and UX documents still prescribe superseded or unresolved behavior; and
- mandatory-Folder and durable cross-context workflow behavior remain release blockers.

The trigger for this proposal is therefore not the absence of a correction strategy. It is the gap
between the approved strategy and the executable artifacts needed to implement it safely.

### Problem Statement

The active corrective backlog is not schedulable. It names valid work but does not define bounded
user or operational outcomes, entry prerequisites, acceptance criteria, failure and recovery
behavior, verification evidence, or exact dependency order. At the same time, the canonical PRD,
architecture, and UX specification still conflict with the corrective direction that the backlog
is meant to implement.

Starting Story 6.1 from the current planning set would force developers to make product,
architecture, security, durability, and test-policy decisions inside implementation stories. That
would convert known planning gaps into rework and unsafe completion claims.

### Evidence

- Implementation-readiness status: `NOT READY`.
- Functional coverage: 22 of 22 PRD FRs mapped.
- Readiness findings: 29 issues across three categories, plus four UX warnings.
- Corrective placeholders: 23 of 23 lack executable acceptance criteria.
- Story 5.12 live evidence: 19 passed and 56 failed product cases.
- Production-readiness audit: nine P1 and seven P2 findings.
- Release handoff: blocked; no production deployment or stakeholder acceptance evidence.
- Sprint status: Epics 1–5 are historical `done`; Epics 6–8 and all corrective entries remain
  `backlog`, so no corrective implementation has to be rolled back.

### Issue Classification

Primary classification: approved course correction incompletely materialized into canonical
planning artifacts.

Secondary classification: stale architecture and UX assumptions plus under-specified product and
NFR decisions.

## 2. Impact Analysis

### Epic Impact

#### Epics 1–5

Epics 1–5 remain completed implementation history. Their story files and evidence are not reopened
or rewritten. They are not release authorization, and any prescriptive content that conflicts with
the rebaselined PRD or architecture is explicitly superseded.

#### Epics 6–8

Epics 6–8 remain in backlog but are not implementation-ready epics. Their current one-line entries
are treated as a findings/work inventory, not as stories that may advance to `ready-for-dev`.

They must be rewritten around these observable outcomes:

- **Epic 6 outcome:** Authorized Chatbot callers can create, open, list, update, archive, and obtain
  Project data through the supported EventStore DomainService and platform-owned runtime, with real
  caller identity and compatibility-controlled migration.
- **Epic 7 outcome:** Chatbot users and authorized operators can complete Project creation,
  assignment, confirmation, and maintenance operations through durable, restart-safe, bound tasks
  without violating bounded-context ownership.
- **Epic 8 outcome:** Operators and release owners can rely on the same authenticated, accessible,
  metadata-only facts across Web, CLI, and MCP, backed by blocking persisted-boundary, isolation,
  resilience, performance, deployment, and acceptance evidence.

The epic-creation workflow may adjust numbering and story count, but it must preserve all approved
findings, requirements, migration obligations, containment rules, and release gates.

### Story Impact

No dedicated Story 6.x, 7.x, or 8.x implementation file should be created from the current
placeholder text. Each replacement story must include:

- actor/persona, observable outcome, and bounded scope;
- owning requirements and findings;
- verified upstream repository/version/API prerequisites;
- explicit dependency order and entry/exit gates;
- Given/When/Then acceptance criteria;
- authorization, tenant-isolation, privacy, compatibility, and negative paths;
- restart, duplicate delivery, lost response, stale state, concurrency, cancellation,
  compensation, reconciliation, and idempotency cases where applicable;
- test tier, deterministic fixtures, verification commands, and required evidence; and
- a measurable completion rule that cannot pass through an unexplained skip or a later story.

### Artifact Conflicts

| Artifact | Current conflict | Required correction |
| --- | --- | --- |
| PRD | No FR-23 despite approved restore scope; Active-folder semantics remain ambiguous; operator actions, Chatbot handoff, and NFR proofs are incomplete | Update and validate the PRD before architecture/backlog work |
| Architecture | Still says `READY WITH MINOR GAPS`; prescribes Projects-owned AppHost/Aspire/ServiceDefaults/Workers and presentation concerns in Contracts | Replace with one migration-required DomainService/platform architecture |
| UX | Assumes resolution case IDs/history, mutating reevaluation, caller-authored confirmation, and unratified operator actions | Update current-only trace, bound-task, role, and Chatbot handoff behavior |
| Epics | Historical stories and an active corrective findings list share one `status: complete` document | Separate/label history and replace the corrective addendum with outcome-based executable stories |
| Sprint status | Corrective entries are correctly backlog but look schedulable by identifier | Keep them backlog; do not advance any until the rebaseline passes readiness |
| Test/release artifacts | Failed or skipped critical evidence exists; release is blocked | Preserve the block and make deterministic evidence part of each owning story |
| Cross-surface contracts | Parity/export contracts exist outside the canonical architecture and on a stale presentation boundary | Ratify versioned parity and export contracts on the supported boundary |

### Technical Impact

The planned implementation remains a staged migration, not a rewrite of committed events. It must
preserve valid aggregate/domain behavior while moving runtime ownership, projections, queries,
identity, authentication, cross-context workflows, confirmations, operational surfaces, and
verification to supported platform seams.

No event history may be edited or dual-written without proven deduplication. Rollback remains a
routing/adapter decision until all release gates pass.

### MVP Impact

The original Project workspace value remains. The MVP is clarified, not converted into a generic
project-management product:

- an Active Project always has exactly one authorized Folder;
- pre-activation progress is durable task state, not a third Project lifecycle state;
- restore is added as FR-23 under the already approved correction;
- reevaluation is read-only Refresh/recomputation;
- resolution traces are current recomputation results, not persisted case history;
- safe diagnostic export is a bounded FR-22 read capability;
- consequential actions use server-issued, bound confirmation evidence; and
- Chatbot owns the accessible candidate/proposal interaction through an explicit companion
  integration contract.

## 3. Recommended Approach

### Selected Path

Use a **planning-first hybrid correction**:

1. preserve implementation history and the approved July 14 strategic decisions;
2. freeze corrective implementation and production release;
3. update and validate the PRD;
4. replace the canonical architecture;
5. align the UX and Chatbot handoff;
6. rewrite Epics 6–8 and their stories around observable outcomes;
7. define system-level test/NFR evidence ownership;
8. rerun implementation readiness in a fresh context; and
9. create a new sprint plan only if readiness is `READY`.

### Alternatives Considered

#### Direct adjustment of the 23 placeholders

Not viable alone. Story authors would still be forced to resolve PRD, architecture, UX, security,
durability, and NFR conflicts independently, and Epics 6 and 8 would remain technical work programs
rather than coherent outcomes.

#### Rollback of completed Epics 1–5

Not recommended. Existing pure domain behavior, validation, deterministic context/resolution
policy, metadata-only modeling, client generation, and focused tests remain useful. No corrective
story has started, so the safer rollback is to withdraw the active schedule, not discard valid
implementation history.

#### MVP reduction only

Insufficient. Clarifying restore, trace, export, and operator scope is necessary, but it does not
repair platform ownership, real identity, mandatory-Folder durability, cross-context recovery, or
release evidence.

### Effort, Risk, and Schedule Impact

- Scope classification: **Major**.
- Planning/rebaseline effort: **Medium**.
- Subsequent implementation effort: **High**, to be re-estimated from the rewritten stories.
- Current technical/release risk: **High**.
- Risk after rebaseline: controlled through entry gates, end-to-end value slices, compatibility
  seams, deterministic evidence, and routing rollback.

The previous 13–20 engineering-week implementation range is retained only as a rough historical
indicator. It is not a release commitment and must be replaced after the new backlog is estimated.

## 4. Detailed Change Proposals

### 4.1 PRD Changes

#### FR-1 / FR-19 — Mandatory Folder and creation state

OLD:

> Creating a Project records durable Project metadata and sets Project Lifecycle State to Active.
> Creating a Project can create or attach a Project Folder with the same name as the Project when
> no folder is supplied.

NEW:

> Create Project is admitted as a durable, pollable creation task. A Project becomes caller-visible
> with lifecycle `Active` only after exactly one authorized Project Folder has been verified and
> bound. A supplied Folder is verified; otherwise the task requests a same-name Folder through
> Hexalith.Folders. Dependency unavailability, timeout, lost response, denial, or reconciliation
> never exposes an Active folderless Project. Equivalent retries return the same task; a same-key,
> different-request retry conflicts. Pending, blocked, failed, cancelled, and needs-attention are
> task states and do not extend the `Active`/`Archived` Project lifecycle.

Rationale: resolves the direct FR-1/FR-11/Story 1.4/Story 2.4 contradiction and makes the Epic 7
invariant canonical before implementation.

#### FR-23 — Restore Archived Project

OLD:

> No FR-23 exists. Restore appears only in UX, Epic 5, and the corrective addendum.

NEW:

> **FR-23: Restore Archived Project.** Authorized Chatbot or operator workflows can restore an
> Archived Project. Restoration changes `Archived` to `Active` only after current tenant,
> actor, Project, and exactly-one-authorized-Folder evidence is verified. It uses a server-issued
> preview and confirmation bound to current state/version, executes as an idempotent durable task,
> fails closed on stale or unavailable evidence, and produces metadata-only audit evidence.
> The Project is not treated as active context until the read model confirms completion.

Rationale: implements the already approved restore decision and removes the undefined FR-23
reference from traceability.

#### FR-14 / FR-15 — Chatbot confirmation handoff

OLD:

> User confirmation creates or updates the Project-to-Conversation association. No Project is
> created from inference until authorized user action confirms creation.

NEW:

> Projects returns a server-issued, expiring, single-use confirmation artifact bound to tenant,
> actor, action, targets, normalized request hash, preview, and current resource version. Chatbot
> presents candidates or the proposed Project with no preselected ambiguous choice and provides
> accessible confirm, cancel, retry, expired/stale, lost-response, and task-status interactions.
> Confirmation executes through a durable task; state changes after preview invalidate the
> artifact. Only confirmed completion is persisted and audited.

Rationale: closes the Chatbot UX handoff and AGENT-001/REL-001 gaps without moving the end-user UI
into Projects.

#### FR-22 — Operator scope and Safe Diagnostic Export

OLD:

> Operator read access exposes metadata only and provides no write capabilities beyond archive and
> troubleshooting workflows explicitly exposed by Chatbot or generated/admin surfaces.

NEW:

> The PRD includes an explicit role/operation matrix for read, preview, archive, restore, relink,
> unlink, confirm resolution, and confirm proposed Project creation across Chatbot, Web, CLI, and
> MCP. Reevaluation is a read-only Refresh diagnostic. Authorized operators may export an
> already-authorized `projects.safe-diagnostic-export.v1` response with deterministic field order,
> ISO-8601 timestamps, explicit truncation metadata, a 1 MiB encoded-size cap, at most 500 reference
> rows, and at most 100 audit rows. Projects does not retain the export. Payload-bearing data,
> secrets, tokens, unrestricted paths, raw upstream problems, and unconfirmed candidate details
> remain excluded.

Rationale: prevents UX/epics from silently expanding operator authority and ratifies the existing
safe-export contract.

#### NFR and success-metric acceptance

OLD:

> Security, reliability, observability, and compatibility are directional; performance targets
> p95 under 500 ms without a complete measurement envelope.

NEW:

> Before any corrective story becomes ready for development, the PRD records numeric or bounded
> decisions for supported tenant/Project/reference/audit cardinalities, page limits, dependency
> budgets, p95 measurement conditions, availability target, recovery objectives, audit/event/task
> retention, encryption/key-management expectations, rate/back-pressure behavior, authenticated
> role claims, accessibility, and degraded-dependency measurement. Every NFR is mapped to an owning
> story and deterministic release evidence. Cursor pagination defaults to 50 and caps at 200;
> committed Project events have RPO 0; critical verification cannot pass via an unexplained skip.

Rationale: converts NFR intent into acceptance boundaries before implementation choices are fixed.

### 4.2 Architecture Changes

#### Status and ownership

OLD:

> Overall Status: READY WITH MINOR GAPS. Selected starter: Hybrid Hexalith module scaffold.
> Projects owns Server, Workers, ServiceDefaults, Aspire, AppHost, UI host, Dapr components,
> projection processing, health, and telemetry.

NEW:

> Overall Status: TARGET ARCHITECTURE APPROVED — PLANNING AND IMPLEMENTATION MIGRATION REQUIRED.
> Production release and corrective development remain blocked until the PRD, architecture, UX,
> backlog, and readiness gates in this proposal complete.

| Concern | Target owner |
| --- | --- |
| Project aggregate, commands, events, validators, context and resolution policy | Hexalith.Projects |
| Stable domain/wire contracts and query DTOs | Hexalith.Projects.Contracts |
| Projects presentation descriptors | Approved UI/presentation adapter boundary, not the low-dependency Contracts kernel |
| Projection/query handlers | Projects through EventStore DomainService SDK seams |
| Persistence, publication, subscriptions, read-model stores, cursors, health, telemetry | Hexalith.EventStore DomainService/platform |
| Dapr components and distributed topology | Platform AppHost |
| Web/MCP/CLI runtime composition and credential providers | FrontComposer/platform hosts |
| Generic durable workflow/task/confirmation capability | Shared EventStore/platform capability |
| Project-specific task transitions and policies | Hexalith.Projects |

Rationale: aligns the canonical architecture with current EventStore and FrontComposer platform
rules and removes known reimplementation.

#### Durable workflow and confirmation model

OLD:

> Cross-context operations use command-async writes and UI lifecycle states, but no canonical
> restart-safe task/confirmation model defines checkpoints, recovery, or bound evidence.

NEW:

> The architecture defines a durable task state machine, checkpoint ownership, idempotency scope,
> server-issued preview/confirmation schema, expiry and single-use rules, state/version
> invalidation, cancellation, retry, compensation, reconciliation, audit-receipt behavior, and
> restart/two-instance convergence. Each cross-context operation has a transition/failure matrix.
> Conversation ownership remains in Conversations; Projects records an idempotent metadata-only
> receipt only after confirmed upstream completion.

Rationale: makes REL-001 and AGENT-001 architecture decisions rather than story-local invention.

#### Resolution trace, parity, and export

OLD:

> Resolution is compute-on-demand, while UX and Story 5.6 refer to resolution cases and later
> reconstruction. Parity and export details live mainly in companion docs.

NEW:

> Resolution Trace is a current, authorized recomputation over conversation/attachment inputs. It
> has no persisted case ID or historical candidate-score store in MVP. Only explicit confirmation
> persists. The architecture ratifies the cross-surface parity matrix and
> `projects.safe-diagnostic-export.v1`, including field ownership, bounds, authorization,
> partial-failure semantics, accessibility labels, CLI exits, MCP schemas, and audit treatment.

Rationale: removes an undefined data source and makes cross-surface contracts canonical.

#### Migration, cutover, and evidence

OLD:

> The architecture's implementation sequence builds the now-obsolete runtime directly.

NEW:

> The migration sequence inventories existing events/state/routes/consumers; pins upstream
> platform capabilities; introduces compatibility adapters; replay-compares read models; reconciles
> legacy folderless/pending/in-flight records through compensating tasks; cuts authenticated reads
> and commands by value slice; migrates operational consumers; retires Projects-owned runtime
> plumbing; and retains routing rollback through release acceptance. The architecture maps every
> NFR and P1/P2 finding to blocking evidence.

Rationale: prevents event rewrite, unsafe dual writes, and a second big-bang technical migration.

### 4.3 UX Changes

#### Resolution diagnosis

OLD:

> Users can start from a resolution case ID, load trace metadata, return later, and reconstruct what
> happened.

NEW:

> Users start from current authorized Conversation, Folder, File, Project, or correlation inputs
> and request a transient recomputation. The view clearly says that candidate scores/ranks are
> current diagnostic output, not historical evidence. Closing the view discards the trace. Audit
> history shows confirmed outcomes only.

Rationale: aligns UX with the canonical compute-on-demand model.

#### Maintenance actions and task truth

OLD:

> Restore, relink, unlink, and reevaluate are mutating actions confirmed by client controls and a
> five-state UI lifecycle.

NEW:

> Reevaluation is read-only Refresh diagnostics. Archive, restore, relink, unlink, resolution
> confirmation, and proposed-Project confirmation are shown only when authorized by the PRD role
> matrix. Clients request and render a server preview; they never author confirmation truth. Task
> states are `Pending`, `Running`, `WaitingForDependency`, `Succeeded`, `Rejected`, `Failed`,
> `Cancelled`, or `NeedsAttention`. Acknowledgement and SignalR nudges are never completion.

Rationale: prevents cross-surface privilege drift and false-success UX.

#### Chatbot companion contract

OLD:

> The end-user Chatbot UX is out of scope and no linked handoff contract specifies FR-14/FR-15
> presentation and recovery.

NEW:

> A Chatbot-owned companion integration/UX contract specifies candidate comparison, no preselected
> ambiguous option, confirm/cancel, accessible labels and focus, stale/expired confirmation,
> lost-response retry, task status, safe denial, and metadata-only display. Projects supplies the
> contracts and evidence; Chatbot owns end-user presentation.

Rationale: closes UX-A1 without creating a standalone Projects end-user UI.

#### Performance and accessibility budgets

OLD:

> Responsive layouts and WCAG intent are detailed, but data/render budgets and blocking live
> evidence are not measurable.

NEW:

> Each inventory, reference-health, trace, audit/export, dashboard, and confirmation view declares
> supported cardinality, server/query budget, render budget, pagination/truncation behavior, and
> degraded-dependency state. WCAG 2.2 AA, keyboard/focus, tenant isolation, leakage, and critical
> responsive behavior use authenticated deterministic fixtures and blocking evidence.

Rationale: makes UX performance and accessibility acceptance executable.

### 4.4 Epic and Story Rebaseline

The current 23 entries are dispositioned as follows before the epics workflow creates the final
story inventory:

| Current entry | Required disposition |
| --- | --- |
| 6.1 | Convert to an architecture/migration entry gate; do not treat an inventory alone as user-value completion |
| 6.2 | Split domain/wire contracts, presentation adapter, identifier compatibility, and API/client compatibility where they can be independently verified; accept them through the first consuming value slices |
| 6.3 | Retain secure admission as an enabler accepted through authenticated create/open/list and adversarial denial evidence |
| 6.4 | Slice read-model/query migration by observable list/open/context/operator outcomes rather than one all-projections story |
| 6.5 | Slice command/runtime migration by the first supported mutation outcomes; platform topology is an entry dependency, not the epic's value |
| 6.6 | Split authenticated Web and CLI consumer outcomes unless one story can remain bounded and independently verified |
| 6.7 | Retain cutover/retirement only after per-slice compatibility and rollback gates are explicit |
| 7.1 | Move generic workflow/task/confirmation work to an approved upstream platform story; Projects consumes a pinned capability |
| 7.2 | Retain the bound confirmation seam and accept it through the first consequential workflow |
| 7.3 | Retain as an end-to-end mandatory-Folder Project-creation outcome with a full state/failure matrix |
| 7.4 | Split conversation link and move if their failure/compensation paths cannot fit one bounded story |
| 7.5 | Retain as a durable proposed-Project outcome with Chatbot handoff and recovery criteria |
| 7.6 | Split archive, restore, relink, and unlink into independently releasable outcomes; remove reevaluate mutation |
| 7.7 | Retain legacy reconciliation after the target task model and migration inventory are approved |
| 8.1 | Make persisted-boundary CI evidence an entry/acceptance gate attached to the value slices it protects, plus only the minimal shared fixture enabler |
| 8.2 | Split health, telemetry, and generated logging if independently deployable/failing; require truthful bounded signals |
| 8.3 | Split MCP and CLI conformance when their schemas, auth, and failure behavior differ materially |
| 8.4 | Retain authenticated operator UI conformance as an operator outcome, not markup migration alone |
| 8.5 | Split source structure, package/public surface, and supply-chain/release artifact work if independently verifiable |
| 8.6 | Move tenant isolation, leakage, and accessibility evidence into every owning surface story; retain only a final authenticated system gate |
| 8.7 | Put restart/retry/concurrency/reconciliation criteria into every durable workflow story; retain only cross-workflow system proof if needed |
| 8.8 | Decide bounds in PRD/architecture before implementation; retain a reproducible performance-validation story after the bounded read models exist |
| 8.9 | Treat deployment and stakeholder acceptance as the terminal release gate/checklist, not as a substitute for missing implementation acceptance |

Rationale: preserves the complete findings inventory while removing forward dependencies,
epic-sized bundles, and technical-milestone completion claims.

### 4.5 Tracking and Release Changes

After approval:

- keep Epics 6–8 and all current corrective entries at `backlog` until the replacement epics are
  generated and validated;
- do not create placeholder story files merely to advance status;
- replace sprint-status entries atomically only after the approved epic/story inventory exists;
- preserve all completed story files, retrospectives, audits, readiness reports, and failed test
  evidence as history;
- retain the blocked Epic 5 release handoff; and
- state explicitly that readiness must be rerun and return `READY` before any corrective story
  enters a development sprint.

The July 14 proposal remains authoritative for its approved product direction and containment. This
proposal supersedes its story-count statement (`21`) and the assumption that adding one-line Epic
6–8 entries completed backlog materialization.

## 5. Implementation Handoff

### Scope Classification

**Major** — canonical requirements, architecture, UX, backlog, test strategy, migration, and release
planning must be rebaselined before implementation.

### Required BMad Route

Use a fresh context for each step and carry forward only the approved source artifacts and explicit
handoff outputs:

1. **`bmad-correct-course` (this proposal):** approve the planning rebaseline and containment.
2. **`bmad-prd` — update, then validate:** materialize FR-1/FR-19, FR-23, FR-14/FR-15, FR-22,
   role/operation, safe-export, Chatbot handoff, and measurable NFR decisions.
3. **`bmad-architecture` — update:** replace the canonical architecture with the supported
   DomainService/platform ownership, durable task/confirmation model, migration/cutover plan,
   parity/export contracts, and NFR evidence map.
4. **`bmad-ux` — update:** align operational UX and produce/reference the Chatbot companion handoff.
5. **`bmad-create-epics-and-stories`:** replace the corrective addendum with outcome-based epics and
   executable story definitions using the disposition table above.
6. **`bmad-testarch-test-design`:** assign system/epic test levels, deterministic fixtures, NFR
   gates, and release-critical evidence to the rewritten stories.
7. **`bmad-check-implementation-readiness`:** independently verify the new PRD, architecture, UX,
   epics, stories, dependencies, and evidence plan. Required result: `READY`.
8. **`bmad-sprint-planning`:** generate a new sprint plan only after the readiness gate passes.
9. **`bmad-create-story`, then `bmad-dev-story`:** create and implement the first ready value slice;
   do not bulk-create 23 placeholder story files.

### Recipients and Responsibilities

#### Product Manager

- Update and validate the PRD and requirements traceability.
- Own FR-23, operator scope, Chatbot handoff, and measurable NFR decisions.

#### Solution Architect

- Replace the canonical architecture and project tree.
- Pin platform/upstream seams and define migration, compatibility, rollback, task, confirmation,
  reconciliation, and evidence contracts.

#### UX Designer / Chatbot Owner

- Align operational UX with current-only trace and server-task truth.
- Produce or approve the Chatbot FR-14/FR-15 companion contract.

#### Product Owner

- Rewrite Epics 6–8 and their stories around observable outcomes.
- Reconcile sprint tracking only after the new inventory is approved.

#### Test Architect

- Own the system/epic test design, fixtures, NFR thresholds, evidence levels, and no-skip critical
  release gates.

#### Developer

- Do not implement a corrective placeholder.
- Begin only from a readiness-approved story and preserve event history and containment rules.

#### Release Owners: Jerome and John

- Keep production release and consequential autonomous operations blocked.
- Approve residual-risk dispositions and final release only after the terminal evidence gate.

### Immediate Containment

Until the rebaseline is approved and readiness passes:

- no corrective story enters development;
- no production release proceeds;
- no consequential autonomous MCP operation or proposal confirmation is enabled;
- no failed or skipped critical test is reported as passing evidence;
- no event history is rewritten and no unsafe dual write is introduced; and
- no sibling platform module is modified without its own approved, repository-local story and
  validation.

### Success Criteria

- PRD, architecture, UX, epics, stories, sprint tracking, and test strategy describe one current
  system with no superseded prescriptive path.
- FR-23 is defined and mapped; all other maintenance behavior has explicit requirement ownership.
- Active Projects cannot be folderless.
- Cross-context operations and confirmation are durable, bound, idempotent, and restart-safe.
- Domain/runtime/presentation ownership matches current EventStore and FrontComposer rules.
- Every corrective story is bounded, dependency-gated, testable, and independently completable.
- NFRs have measurable thresholds, owners, environments, and pass/fail evidence.
- Implementation readiness returns `READY` before sprint planning.
- Production release remains blocked until deployment evidence and dated Jerome/John acceptance.

## 6. Change Navigation Checklist Status

### Section 1 — Trigger and Context

- [x] 1.1 Triggering story identified: Story 5.12 plus the July 14 readiness rerun.
- [x] 1.2 Core problem classified and stated.
- [x] 1.3 Concrete report, audit, live-test, sprint, and handoff evidence recorded.

### Section 2 — Epic Impact

- [x] 2.1 Trigger/current corrective epic viability assessed.
- [!] 2.2 Epics 6–8 require outcome-based redefinition.
- [!] 2.3 All 23 future corrective entries require replacement or re-slicing.
- [x] 2.4 No original PRD feature epic becomes obsolete; technical corrective milestones are not
  accepted as standalone value epics.
- [x] 2.5 Required order is PRD → architecture → UX → epics/stories → test design → readiness →
  sprint planning.

### Section 3 — Artifact Conflict and Impact

- [!] 3.1 PRD requires the changes in Section 4.1.
- [!] 3.2 Architecture requires replacement per Section 4.2.
- [!] 3.3 UX and Chatbot handoff require alignment per Section 4.3.
- [!] 3.4 Backlog, sprint tracking, test strategy, CI evidence, and release handoff require the
  changes in Sections 4.4–4.5.

### Section 4 — Path Forward

- [x] 4.1 Direct adjustment evaluated: not viable alone; effort high, risk high.
- [x] 4.2 Rollback evaluated: not recommended; effort high, limited benefit.
- [x] 4.3 MVP review evaluated: required as part of the hybrid; scope clarification, not product
  abandonment.
- [x] 4.4 Planning-first hybrid selected and justified.

### Section 5 — Proposal Components

- [x] 5.1 Issue summary complete.
- [x] 5.2 Epic and artifact impacts complete.
- [x] 5.3 Recommended path and alternatives complete.
- [x] 5.4 MVP impact and action sequence complete.
- [x] 5.5 Major-scope handoff plan complete.

### Section 6 — Final Review and Handoff

- [x] 6.1 Applicable checklist sections reviewed; action-needed items are routed.
- [x] 6.2 Proposal internally checked against the July 14 readiness report and approved correction.
- [x] 6.3 Explicit approval granted by Jerome on 2026-07-15.
- [N/A] 6.4 Sprint status already keeps Epics 6–8 and all corrective entries at `backlog`;
  replacement entries will be reconciled atomically after the new epic/story inventory is approved.
- [x] 6.5 Handoff roles, sequence, containment, and success criteria confirmed.

## 7. Approval and Routing

Status: **Approved by Jerome on 2026-07-15**.

Change scope: **Major**.

Route first to Product Manager, then Solution
Architect, UX/Chatbot owner, Product Owner, Test Architect, independent readiness assessor, and
only afterward to sprint planning and development.

Approval authorizes planning-artifact correction and sprint-status reconciliation after the new
backlog is approved. It does not authorize production release, consequential autonomous
operations, bypassing readiness, or weakening any containment or evidence gate.
