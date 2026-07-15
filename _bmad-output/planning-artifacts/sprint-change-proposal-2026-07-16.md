---
title: "Sprint Change Proposal: Downstream Artifact Repair After Final PRD Rebaseline"
status: approved
created: 2026-07-16
approved: 2026-07-16
approved_by: Jerome
project: Hexalith.Projects
prepared_for: Jerome
scope: major
mode: incremental
trigger_report: _bmad-output/planning-artifacts/implementation-readiness-report-2026-07-15.md
amends: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15.md
---

# Sprint Change Proposal: Downstream Artifact Repair After Final PRD Rebaseline

## 1. Issue Summary

### Trigger

The July 15 implementation-readiness assessment completed after the approved July 15 planning
rebaseline had updated the governing product contract to 24 Functional Requirements and 11
Non-Functional Requirements.

The rerun returned `NOT READY`. It confirmed that the PRD correction succeeded, but the downstream
architecture, UX, epics, stories, traceability, and tracking artifacts did not converge on that
final contract.

### Problem Statement

Hexalith.Projects has a complete governing PRD but no current implementation substrate. The only
substantive architecture remains based on the superseded 22-FR/9-NFR design and assigns technical
runtime ownership contrary to current EventStore and FrontComposer platform rules. Its proposed
replacement is an untouched template. The UX remains operator-only and omits required Chatbot
candidate, proposal, confirmation, recovery, task, and first-response-admission behavior. The epic
plan retains two explicit violations of the mandatory Folder invariant and represents 23 findings
placeholders as stories.

Starting corrective development from these artifacts would force implementation agents to decide
architecture, authority, durability, UX, compatibility, and release policy inside code stories.
That would convert known planning defects into unsafe behavior, rework, and false completion.

### Evidence

- Governing PRD: complete, with FR-1 through FR-24 and NFR-1 through NFR-11.
- Epic coverage: 23 of 24 FRs, or 95.8%.
- Missing complete owner: FR-24 Safe Diagnostic Export.
- Architecture: obsolete detailed document plus an uninstantiated architecture-spine template.
- UX: no binding Chatbot companion for FR-14, FR-15, FR-20, and NFR-9.
- Folder invariant: Stories 1.4 and 2.4 permit caller-visible or Active folderless Projects.
- Corrective backlog: all 23 Epic 6–8 entries are unschedulable findings placeholders.
- Assessment findings: 37 across coverage, UX alignment, and epic quality, plus three warnings.
- Codebase audit: nine P1 and seven P2 findings remain open or require terminal disposition.
- Live evidence: focused 13 passed/13 failed; full Chromium 19 passed/56 failed.
- Release handoff: blocked, with deployment and stakeholder acceptance unverified.
- Sprint tracking: Epics 6–8 and their current entries remain `backlog`; corrective development has
  not started, so no rollback of corrective implementation is required.

### Classification

Primary classification: downstream planning artifacts failed to materialize the finalized product
contract.

Secondary classifications:

- failed planning approach requiring a more explicit artifact sequence;
- stale architecture and package/runtime ownership;
- missing end-user companion UX;
- unschedulable corrective backlog;
- incomplete verification and release traceability.

## 2. Impact Analysis

### PRD Impact

No product-scope change is proposed. The final PRD and its addendum remain governing.

The PRD already establishes:

- exactly one authorized Folder before a Project is caller-visible or Active;
- Durable Task, Preview, Confirmation Artifact, idempotency, response, freshness, and recovery
  semantics;
- role and surface authority;
- FR-23 Restore Archived Project;
- FR-24 Safe Diagnostic Export;
- measurable scale, availability, durability, retention, accessibility, compatibility, and
  release-evidence obligations; and
- containment until a superseding readiness run returns `READY`.

### Epic Impact

#### Epics 1–5

Epics 1–5 remain immutable implementation history. They are not current acceptance authority.
Known-conflicting criteria are marked explicitly as superseded so future planning and development
agents cannot treat them as current instructions.

#### Epics 6–8

The current 23 entries are removed as stories and retained only as findings history. The three
corrective themes and their order remain, but they are redefined as observable outcome epics:

1. **Epic 6 — Authorized Project Reads on the Supported Platform**
2. **Epic 7 — Durable Project Decisions and Cross-Context Recovery**
3. **Epic 8 — Safe Operations and Release Confidence**

The replacement inventory contains 33 bounded stories: seven in Epic 6, fifteen in Epic 7, and
eleven in Epic 8.

### Story Impact

- Stories 1.4 and 2.4 receive non-authoritative supersession notices for their folderless
  activation criteria.
- Story 5.12 receives a superseded completion rule: executed-but-failing evidence is not
  acceptance.
- The 23 current corrective placeholders are replaced rather than promoted to story files.
- Every replacement story requires a beneficiary, bounded outcome, prior-only dependencies,
  repository authority, traceability, BDD acceptance, deterministic verification, estimate, and
  completion boundary.
- Multi-flow placeholders are split along authorization, state, failure, compensation, and
  evidence boundaries.

### Architecture Impact

The May architecture is retained only as superseded history. The July architecture spine becomes
the sole normative architecture after it is instantiated and approved.

The replacement architecture must define:

- domain-centric EventStore DomainService and platform-owned runtime/presentation composition;
- `Hexalith.Projects.UI.Contracts` as the non-packable descriptor boundary;
- the mandatory Folder invariant;
- Durable Task transitions and completion authority;
- Preview, bound Confirmation Artifact, idempotency, and recovery rules;
- response, freshness, task, and Recovery Action Code mappings;
- compatibility-controlled migration without event rewrite or unsafe dual writes;
- repository-local authority for sibling changes;
- current-only, nonpersistent Resolution Traces;
- the complete FR-24 contract;
- MCP containment; and
- machine-checkable evidence ownership.

### UX Impact

The operational UX remains a metadata control plane, but the specification must also contain the
binding Projects-to-Chatbot companion contract. UX must align every surface to server task and
read-model truth, current role authority, read-only refresh, canonical audit boundaries, FR-24,
FrontComposer/Fluent V5 governance, binding scale behavior, and authenticated accessibility
evidence.

The visual-direction HTML remains a concept artifact only. Its raw controls, tables, CSS, hard-coded
colors, and vocabulary are not implementation input.

### Secondary Artifact Impact

- Add a machine-checkable implementation-readiness traceability matrix.
- Produce system/epic test design with deterministic persisted-boundary fixtures.
- Reconcile `sprint-status.yaml` atomically after the replacement story inventory is fully
  authored and approved.
- Reroute existing action items from obsolete story identifiers.
- Preserve the release block, failed evidence, audits, and historical implementation artifacts.
- Do not authorize sibling-repository changes implicitly.

### Technical Impact

The eventual implementation is a staged compatibility migration, not a rewrite of domain history.
Supported read paths move first. Durable consequential workflows move after the shared platform and
sibling capabilities are approved and pinned. Operational surfaces and system evidence follow the
working value slices. Old routing remains available for rollback until release acceptance.

### MVP and Release Impact

The v1 product scope is unchanged. The PRD explicitly defines no approved FR or NFR that may be cut
from production release. Corrective implementation, production release, consequential autonomous
MCP mutation, and proposed-Project confirmation remain disabled until the repaired planning set
passes implementation readiness and the terminal release gates pass.

## 3. Recommended Approach

### Selected Path: Planning-First Direct Adjustment

Use the current final PRD and completed implementation as evidence, then repair only the downstream
planning substrate:

1. instantiate and approve the current architecture spine;
2. update operational UX and add the Chatbot companion contract;
3. replace the corrective findings inventory with outcome epics and bounded stories;
4. create the traceability matrix and system/epic test design;
5. rerun implementation readiness independently;
6. reconcile sprint planning only after a `READY` result; and
7. create and implement one readiness-approved story at a time.

### Option Evaluation

#### Option 1 — Direct Adjustment

**Viable and selected.** The PRD is complete, corrective implementation has not started, and the
remaining defects are contained in downstream planning artifacts.

- Planning effort: Medium.
- Subsequent implementation effort: High; re-estimate from the replacement stories.
- Current risk: High.
- Expected risk after rebaseline: Medium and controlled through dependency gates, value slices,
  deterministic evidence, and rollback.

#### Option 2 — Rollback

**Not viable or useful.** There is no corrective implementation to roll back. Discarding Epics 1–5
would lose valid domain behavior and evidence without repairing architecture, UX, or planning.

#### Option 3 — PRD/MVP Reduction

**Not recommended.** The product contract is complete and explicitly has no deferrable v1 release
cut. Reducing scope would not repair platform ownership, durability, authority, or story quality.

### Schedule Impact

Corrective development remains unscheduled until a fresh readiness assessment returns `READY`.
The prior engineering estimate is historical only and must not be used as a release commitment.
The replacement story set requires estimation after architecture, UX, and external capability gates
are approved.

## 4. Detailed Change Proposals

### 4.1 PRD

**Decision:** No PRD modification.

The primary PRD and addendum remain the product and downstream-routing authority.

### 4.2 Architecture

#### Change A — Authority and Ownership

**OLD:** `architecture.md` is `complete`, claims `READY WITH MINOR GAPS`, and assigns Projects-owned
AppHost, Aspire, ServiceDefaults, Workers, state/projection plumbing, health, telemetry, and UI host
composition. `ARCHITECTURE-SPINE.md` is an untouched template.

**NEW:** Mark `architecture.md` as superseded historical evidence. Instantiate the architecture
spine with:

```yaml
name: Hexalith.Projects
type: architecture-spine
purpose: build-substrate
altitude: initiative
paradigm: Domain-centric EventStore DomainService with platform-owned runtime and presentation adapters
scope: Hexalith.Projects v1 — FR-1 through FR-24 and NFR-1 through NFR-11
status: final
binds: [FR-1..FR-24, NFR-1..NFR-11, Epic-6, Epic-7, Epic-8]
```

Ownership becomes:

| Concern | Owner |
| --- | --- |
| Project domain policy, contracts, handlers, and Project-specific task transitions | Hexalith.Projects |
| Stable domain/wire contracts | Hexalith.Projects.Contracts |
| FrontComposer descriptors | Non-packable Hexalith.Projects.UI.Contracts |
| Generic hosting, persistence, publication, projections, cursors, health, telemetry, durable-workflow capability | Hexalith.EventStore/platform |
| Distributed topology and Dapr components | Platform AppHost |
| Web/MCP/CLI runtime composition and credentials | FrontComposer/platform hosts |
| End-user candidate/proposal presentation | Hexalith.Chatbot |

#### Change B — Durable State and Completion Truth

**OLD:** Command acknowledgement and a five-state UI lifecycle exist without an authoritative
Durable Task transition model or completion mapping.

**NEW:** Add binding decisions for:

- no caller-visible or Active Project before exactly one authorized Folder and read-model
  confirmation;
- `Pending → Running`, with `WaitingForDependency` and `NeedsAttention` recoverable, and
  `Succeeded`, `Rejected`, `Failed`, and `Cancelled` terminal and immutable;
- durable checkpoints, leases, worker ownership, restart/two-instance convergence, duplicate
  delivery, compensation, receipts, and reconciliation;
- `202` and SignalR as non-completion signals;
- 15-minute, server-issued, tamper-evident, atomically single-use Confirmation Artifacts bound to
  Tenant, actor, action, targets, request hash, Preview, and current versions;
- idempotency scope `(Tenant, actor, operation, key)`, equivalent retry, changed-request conflict,
  and at least 30-day/result-lifetime retention; and
- normative mappings among response, freshness, task, and recovery states.

#### Change C — Migration and Repository Authority

**OLD:** The architecture treats the module as greenfield and embeds sibling/platform work inside
Projects planning.

**NEW:** Require:

- inventory of existing events, state keys, read models, cursors, routes, clients, identities,
  in-flight work, and consumers;
- compatibility adapters, deterministic replay comparison, value-slice cutover, and routing
  rollback;
- no event-history rewrite and no unsafe dual writes;
- compensating Durable Tasks for legacy folderless, pending-Folder, or partial records; and
- separate repository-local approval, owner, version, evidence, entry gate, and rollback for every
  sibling change.

#### Change D — Resolution, Export, Parity, and Evidence

**OLD:** Architecture permits optional persisted Resolution Trace state and provides no complete
FR-24 or machine-checkable release contract.

**NEW:** Define:

- request-scoped, current-only Resolution Trace with no persisted case identity or score history;
- `projects.safe-diagnostic-export.v1` with separate authorization, 1 MiB complete-response cap,
  500 reference rows, 100 audit rows, deterministic ordering, safe truncation, no cursor, no
  retention, unavailable-component markers, two concurrent exports per Tenant, and audit of every
  attempt/outcome;
- semantic parity without authority expansion across Web, CLI, MCP, and Chatbot;
- disabled consequential autonomous MCP mutation/proposal confirmation until gates pass; and
- a machine-checkable evidence map with no critical false pass.

### 4.3 UX

#### Change A — Chatbot Companion

**OLD:** End-user Chatbot UX is excluded from the specification.

**NEW:** Chatbot remains the presentation owner, but the Projects UX specification defines binding
candidate comparison, no preselection, proposed creation, confirm/cancel, expiry/staleness renewal,
lost-response recovery, task status, and first-response-admission journeys. It requires keyboard,
focus, live-region, authenticated screen-reader, 200% zoom, and 320 CSS-pixel evidence.

#### Change B — Roles, Task Truth, Refresh, Audit, and MCP

**OLD:** Surface-local lifecycle states, generic roles, mutating `reevaluate`, drifted audit
examples, and assumed MCP mutation availability.

**NEW:** Map every UI phase to Durable Task/read-model truth; use the PRD role/action matrix;
classify reevaluation as read-only Refresh; separate durable audit from telemetry; and show MCP
availability/containment states explicitly.

#### Change C — Response and FR-24 UX

**OLD:** Primary views omit final snapshot/recovery fields; export is a generic action.

**NEW:** Applicable views expose `responseState`, `asOf`, `projectVersion`, component evidence, and
Recovery Action Codes. FR-24 shows permission, schema version, snapshot, bounds, included/omitted
counts, truncation, unavailable components, concurrency guidance, non-retention, and audit outcome
through equivalent Web, CLI, and MCP semantics.

#### Change D — Composition, Scale, and Accessibility

**OLD:** Tabs/pivots are recommended for sibling titled sections; the HTML prototype uses bespoke
markup/styles; volume and manual accessibility evidence are underspecified.

**NEW:** Use FrontComposer/Fluent V5, Fluent 2 tokens, and the required `FluentAccordion` section
pattern. Treat the HTML as non-normative. Define server cursor/paging/virtualization behavior for
10,000 Projects, 5,000 references, and 100,000 audit records. Require authenticated automation,
keyboard, screen reader, focus/live-region, zoom/reflow, reduced-motion, and stable-selector
evidence.

### 4.4 Epic Baseline and Historical Supersession

Update Requirements Inventory and coverage to FR-1–FR-24 and NFR-1–NFR-11. Label Epics 1–5 as
implementation history, the current Epic 6–8 addendum as an unschedulable findings inventory, and
replacement stories as the only future schedulable units after readiness.

Add supersession notices to Stories 1.4, 2.4, and 5.12 for the Folder invariant and no-false-pass
rule.

### 4.5 Replacement Epic 6 Inventory

**Epic 6 — Authorized Project Reads on the Supported Platform**

| Story | Outcome |
| --- | --- |
| 6.1 | List and open Projects through supported authenticated paths |
| 6.2 | Retrieve Conversation-start setup with admission truth |
| 6.3 | Retrieve assembled Project Context through supported read models |
| 6.4 | Resolve Projects with transient current explanations |
| 6.5 | Inspect Projects through an authenticated FrontComposer read surface |
| 6.6 | Inspect Projects through an authenticated CLI read surface |
| 6.7 | Cut over supported reads while preserving compatibility and rollback |

An Epic 6 entry gate pins approved EventStore, FrontComposer, identity, Builds, and sibling
capabilities. It is not counted as delivered value.

### 4.6 Replacement Epic 7 Inventory

**Epic 7 — Durable Project Decisions and Cross-Context Recovery**

| Story | Outcome |
| --- | --- |
| 7.1 | Create a Project with exactly one authorized Folder |
| 7.2 | Update Project Setup idempotently |
| 7.3 | Link an unassigned Conversation |
| 7.4 | Move a Conversation between Projects |
| 7.5 | Unlink a Conversation |
| 7.6 | Replace a Project Folder |
| 7.7 | Link an authorized File Reference |
| 7.8 | Unlink a File Reference |
| 7.9 | Link an authorized Memory |
| 7.10 | Unlink a Memory |
| 7.11 | Confirm an ambiguous Project choice |
| 7.12 | Confirm a proposed new Project |
| 7.13 | Archive an Active Project |
| 7.14 | Restore an Archived Project |
| 7.15 | Reconcile legacy and interrupted workflows |

An Epic 7 entry gate requires approved, pinned durable-workflow/confirmation and sibling contracts.

### 4.7 Replacement Epic 8 Inventory

**Epic 8 — Safe Operations and Release Confidence**

| Story | Outcome |
| --- | --- |
| 8.1 | Inspect task, audit, and reconciliation truth |
| 8.2 | Create a bounded Safe Diagnostic Export |
| 8.3 | Operate Projects through a conformant Web console |
| 8.4 | Operate Projects through a deterministic CLI contract |
| 8.5 | Operate Projects through agent-safe MCP contracts |
| 8.6 | Observe truthful dependency and projection health |
| 8.7 | Consume reproducible packages and supply-chain evidence |
| 8.8 | Verify authenticated parity, isolation, and accessibility |
| 8.9 | Meet bounded performance and back-pressure objectives |
| 8.10 | Prove cross-workflow resilience |
| 8.11 | Complete deployment and stakeholder acceptance |

An Epic 8 evidence entry gate provides deterministic persisted-boundary fixtures and blocking CI.
Story 8.11 cannot complete by recording a blocker.

### 4.8 Traceability and Story Entry

Create `implementation-readiness-traceability-matrix.md` with requirement, finding, architecture,
UX, story, repository/owner, dependencies, environment/fixture, verification command, evidence,
estimate, status, and release-disposition columns.

No replacement story becomes `ready-for-dev` with an unresolved `TBD`, failed critical case,
unexplained critical skip, missing external approval, missing command/evidence path, or incomplete
traceability.

### 4.9 Sprint Tracking

After the replacement stories are fully authored and approved:

- preserve Epics 1–5 as `done`;
- keep Epics 6–8 as `backlog`;
- atomically replace the 23 placeholder entries with the approved 7/15/11 inventories;
- reroute existing action items to the new owners;
- add the `READY`-before-story-creation containment note; and
- preserve the production, MCP, proposal-confirmation, sibling-authority, and evidence blocks.

## 5. Implementation Handoff

### Scope Classification

**Major.** Architecture, UX, backlog, traceability, test design, and sprint tracking require
fundamental replan before implementation.

### Required Route

Use fresh context at each stage and carry forward the approved artifacts explicitly:

1. **`bmad-correct-course`** — approve this proposal and containment.
2. **`bmad-architecture`** — instantiate and approve the current architecture spine; mark the old
   architecture superseded.
3. **`bmad-ux`** — update operator UX and add the binding Chatbot companion.
4. **`bmad-create-epics-and-stories`** — replace the current findings inventory with the approved
   outcome epics and 33 fully specified stories.
5. **`bmad-testarch-test-design`** — produce deterministic fixture, test-tier, NFR, and evidence
   ownership.
6. **Traceability reconciliation** — complete every matrix row and external dependency gate.
7. **`bmad-check-implementation-readiness`** — independently assess the repaired planning set.
   Required result: `READY`.
8. **`bmad-sprint-planning`** — reconcile the sprint only after readiness passes.
9. **`bmad-create-story` / `bmad-dev-story`** — create and implement the first approved value slice.

### Roles and Responsibilities

#### Solution Architect

- Instantiate the architecture spine and ownership graph.
- Define state, confirmation, migration, compatibility, repository, FR-24, and evidence decisions.
- Pin external platform/sibling entry gates.

#### UX Designer and Chatbot Owner

- Update operational UX to current FrontComposer/Fluent rules.
- Produce and approve the Chatbot companion journeys and accessibility contract.
- Align response, task, role, audit, refresh, export, and MCP behavior.

#### Product Owner

- Replace the corrective findings inventory with the 33 approved story outcomes.
- Author complete BDD, dependency, ownership, estimate, verification, and completion detail.
- Reconcile sprint tracking only after the replacement inventory is approved.

#### Test Architect

- Own persisted-boundary fixtures, test levels, performance shapes, accessibility evidence, NFR
  gates, and no-skip critical release criteria.
- Provide exact commands and named pass/fail artifacts.

#### Independent Readiness Assessor

- Use only the reconciled architecture, UX, stories, traceability, and test design as current
  authority.
- Treat historical artifacts as evidence, not current instructions.
- Require `READY` before sprint planning.

#### Developer

- Do not implement a current placeholder or create a story file early.
- Begin only from a readiness-approved, repository-authorized story.
- Preserve event history, bounded-context ownership, and rollback.

#### Release Owners: Jerome and John

- Retain the production block until Story 8.11 passes.
- Record dated residual-risk dispositions and final acceptance.

### Success Criteria

- One substantive current architecture governs FR-1–FR-24 and NFR-1–NFR-11.
- The old architecture is visibly superseded.
- Chatbot and operator UX cover all required journeys and state mappings.
- Active Projects cannot be folderless.
- FR-24 has a complete implementation and verification owner.
- The 23 placeholders are replaced by 33 bounded, dependency-ordered stories.
- Every requirement and finding has repository authority, owner, command, evidence, estimate, and
  terminal disposition.
- Failed or unavailable critical evidence cannot satisfy acceptance.
- A fresh implementation-readiness assessment returns `READY` before sprint planning.
- Production release remains blocked until deployment, rollback, and dated Jerome/John acceptance.

### Immediate Containment

Until all success criteria and release gates are satisfied:

- no corrective story enters development;
- no production release proceeds;
- no consequential autonomous MCP operation or proposed-Project confirmation is enabled;
- no failed or skipped critical test is represented as passing;
- no event history is rewritten and no unsafe dual write is introduced; and
- no sibling repository is modified without separate repository-local approval and validation.

## 6. Change Navigation Checklist Status

### Section 1 — Trigger and Context

- [x] 1.1 Trigger identified: July 15 readiness rerun, rooted in Story 5.12 evidence and incomplete
  downstream execution of the approved rebaseline.
- [x] 1.2 Core problem classified and stated.
- [x] 1.3 PRD, coverage, architecture, UX, epic, audit, live-test, release, and sprint evidence
  recorded.

### Section 2 — Epic Impact

- [x] 2.1 Current corrective epics assessed as not executable in their present form.
- [x] 2.2 Epics 6–8 redefined around observable outcomes.
- [x] 2.3 All 23 future entries dispositioned for replacement.
- [x] 2.4 No new product-scope epic required; external work becomes separately approved gates.
- [x] 2.5 Architecture → UX → stories → test design → readiness → sprint sequence selected.

### Section 3 — Artifact Conflict and Impact

- [x] 3.1 PRD reviewed; no product-scope correction required.
- [x] 3.2 Architecture replacement specified.
- [x] 3.3 UX and Chatbot companion correction specified.
- [x] 3.4 Traceability, test design, CI evidence, sprint tracking, and release handoff impacts
  specified.

### Section 4 — Path Forward

- [x] 4.1 Direct adjustment selected; planning effort Medium, current risk High.
- [x] 4.2 Rollback rejected as non-beneficial.
- [x] 4.3 MVP reduction rejected as unnecessary and insufficient.
- [x] 4.4 Planning-first direct adjustment selected and justified.

### Section 5 — Proposal Components

- [x] 5.1 Issue summary complete.
- [x] 5.2 Epic and artifact impacts complete.
- [x] 5.3 Recommended path and alternatives complete.
- [x] 5.4 MVP impact and action sequence complete.
- [x] 5.5 Major-scope handoff plan complete.

### Section 6 — Final Review and Handoff

- [x] 6.1 Applicable checklist sections reviewed and action-needed items routed.
- [x] 6.2 Proposal internally reconciled to the July 15 readiness report and final PRD.
- [x] 6.3 Complete proposal approved explicitly by Jerome on 2026-07-16.
- [N/A] 6.4 Sprint status remains unchanged by the approved atomicity rule: replace its 23
  placeholder entries only after the 33 replacement stories are fully authored and approved. The
  current `backlog` state safely prevents premature implementation.
- [x] 6.5 Handoff roles, sequence, containment, and success criteria defined.

## 7. Approval and Routing

Status: **Approved by Jerome on 2026-07-16**.

Incremental edit decisions: all 15 proposals approved by Jerome across the July 15–16 workflow.

Approval of the complete proposal authorizes planning-artifact correction and later atomic sprint
reconciliation after the replacement stories are fully authored and approved. It does not
authorize corrective implementation, sibling-repository mutation, production release,
consequential autonomous MCP operations, proposed-Project confirmation enablement, bypassing
readiness, or weakening any evidence gate.

Change scope: **Major**.

Initial route: **Solution Architect**, followed by UX/Chatbot owner, Product Owner, Test Architect,
independent readiness assessor, and—only after `READY`—sprint planning and development.

## 8. Workflow Execution Log

| Field | Recorded result |
| --- | --- |
| Completed | 2026-07-16 |
| User | Jerome |
| Mode | Incremental |
| Trigger | July 15 implementation-readiness assessment: `NOT READY` |
| Scope | Major |
| Incremental decisions | Proposals 1–15 approved |
| Proposal | Approved |
| PRD disposition | Preserve final FR-1–FR-24 / NFR-1–NFR-11 baseline |
| Architecture disposition | Replace empty spine; supersede May architecture |
| UX disposition | Add Chatbot companion and align operator/agent surfaces |
| Epic disposition | Replace 23 placeholders with 33 bounded stories across Epics 6–8 |
| Sprint tracking | Deferred atomically until replacement stories are authored and approved |
| First handoff | Solution Architect |
| Release disposition | Blocked pending readiness and terminal release evidence |
