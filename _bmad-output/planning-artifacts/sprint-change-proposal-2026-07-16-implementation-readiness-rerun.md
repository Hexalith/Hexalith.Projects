---
title: "Sprint Change Proposal: Materialize the Corrective Planning Layer"
status: approved
created: 2026-07-16
approved: 2026-07-16
approved_by: Jerome
project: Hexalith.Projects
prepared_for: Jerome
scope: moderate
mode: batch
trigger_report: _bmad-output/planning-artifacts/implementation-readiness-report-2026-07-16.md
amends: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-16.md
preserves:
  - _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md
  - _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/addendum.md
  - _bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md
---

# Sprint Change Proposal: Materialize the Corrective Planning Layer

## 1. Issue Summary

### Trigger

The independent implementation-readiness rerun dated 2026-07-16 returned `NOT_READY` and
superseded the 2026-07-15 rerun. It confirmed that the product and architecture rebaseline is now
substantively complete, but the final approved planning step has not been executed: the corrective
Epic 6–8 findings inventory remains 23 title-and-paragraph placeholders, and the AD-30 evidence
matrix does not exist.

The failed Story 6.1 automation attempt is supporting evidence, not a process failure. It correctly
refused to synthesize an implementation story from the placeholder and retained `backlog` state.

### Problem Statement

Hexalith.Projects has a final 24-FR/11-NFR PRD, a final 34-decision architecture spine, a complete
operator UX specification, and strong completed Epics 1–5. It does not yet have a schedulable
corrective planning layer. The current Epic 6–8 addendum cannot carry implementation because it has
no acceptance criteria, no current FR/NFR map, no complete story-to-AD traceability, no
machine-checkable evidence ownership, and no reconciled Story 8.11 release gate.

This is an artifact-materialization gap in the already approved rebaseline, not a new product,
architecture, or MVP change. Corrective development would still force developers to resolve known
planning decisions inside code if containment were lifted now.

### Evidence

- Current PRD: final, FR-1 through FR-24 and NFR-1 through NFR-11, with testable consequences and
  the binding section 5 response/recovery contract.
- Current architecture: final Architecture Spine, 34 `ADOPTED` decisions, including AD-30's
  machine-checkable release model and Story 8.11 terminal gate.
- Current epics: 23 Epic 6–8 placeholders without Given/When/Then criteria, verification commands,
  fixtures, evidence artifacts, estimates, or completion boundaries.
- Traceability: top-of-file inventory and maps still assert FR-1–FR-22 / NFR-1–NFR-9 and cite the
  superseded May architecture.
- Missing explicit AC owners: FR-19 Metadata Classification, FR-23 Restore, FR-24 Safe Diagnostic
  Export, NFR-2 encryption/KMS, NFR-7 back-pressure, NFR-8 retention, and Chatbot NFR-9/SM-5
  accessibility/integration evidence.
- Missing canonical artifact:
  `_bmad-output/planning-artifacts/implementation-readiness-traceability-matrix.yaml`, schema
  `hexalith.readiness-evidence.v1`.
- Release-gate mismatch: current Epic 8 ends at Story 8.9; AD-30 and the approved replacement
  inventory end at Story 8.11.
- Open evidence: live Chromium 19 passed/56 failed, release handoff `BLOCKED`, and nine P1 plus
  seven P2 audit findings requiring closure or authorized disposition.

### Classification

Primary classification: incomplete execution of an approved planning correction.

Closest checklist category: a failed planning approach requiring a different materialization path.
The current placeholders must be replaced atomically from approved outcomes rather than promoted or
expanded opportunistically during story creation.

No new requirement, strategic pivot, product-scope reduction, architecture replacement, or
corrective-code rollback is proposed.

## 2. Impact Analysis

### PRD Impact

No PRD edit is required. The final PRD and addendum remain governing without scope reduction or
renumbering.

### Architecture Impact

No architecture decision edit is required. The Architecture Spine is final and is now the sole
normative design. The epics must consume it directly, including AD-30 and Story 8.11. The
superseded root `architecture.md` remains historical evidence only.

### UX Impact

No operator-UX redesign is required for this readiness correction. The current specification is
complete and fully supported by the spine.

The planning layer must add an explicit cross-repository owner and release evidence gate for the
Chatbot candidate, proposal, confirmation, cancellation, recovery, task, first-response-admission,
keyboard, focus, live-region, zoom/reflow, and authenticated screen-reader journeys required by
PRD NFR-9, SM-5, addendum section 6, and AD-34. This ownership does not authorize a Chatbot
repository change; any such work requires separate repository-local approval and evidence.

The low-risk UX document re-stamp to FR-23/FR-24 may be performed as documentation maintenance, but
it is not a substitute for the owning story and evidence rows.

### Epic Impact

#### Epics 1–5

Preserve as `done` implementation history. They remain useful technical and behavioral evidence,
but their earlier `Standalone: Yes` wording is not production-release authority. Add concise
supersession notices where necessary:

- Story 1.4 must not authorize caller-visible or Active folderless creation.
- Story 2.4 must not degrade Folder creation failure into an Active folderless Project.
- Story 5.12 execution counts do not establish acceptance when critical cases fail.

#### Epics 6–8

Replace the complete 23-placeholder corrective addendum atomically with the approved 33-story
outcome inventory: seven stories in Epic 6, fifteen in Epic 7, and eleven in Epic 8. Preserve the
corrective order Epic 6 → Epic 7 → Epic 8 and the entry gates defined by the Architecture Spine.

Do not promote, create implementation files from, or individually patch the current placeholders.

### Story Impact

Every replacement story must contain:

- a named beneficiary and observable outcome;
- current FR, NFR, AD, UX-journey, audit-finding, and release-case traceability;
- repository authority, named owner, pinned prerequisite version/revision, and entry gate;
- prior-only dependencies and an explicit non-dependency on future stories;
- positive, negative, denial, stale/unavailable, duplicate/replay, and recovery criteria where
  applicable;
- exact deterministic environment/fixture and verification command;
- expected pass/fail evidence artifact and release disposition;
- estimate, compatibility/rollback boundary, and completion boundary; and
- no unresolved `TBD`, placeholder, missing external approval, unexplained critical skip, or false
  `passed` state.

### Secondary Artifact Impact

- Create the canonical YAML evidence matrix and a human-readable Markdown view over identical row
  keys.
- Regenerate complete FR-1–FR-24 and NFR-1–NFR-11 coverage maps.
- Map all nine P1 and seven P2 audit findings and every critical release-evidence category.
- Define system/epic test ownership, fixtures, commands, artifacts, and no-false-pass behavior.
- Update `epics.md` frontmatter to cite the final PRD/addendum, final Architecture Spine, current UX,
  this proposal, and the current readiness report.
- Reconcile `sprint-status.yaml` only after an independent fresh assessment returns exactly
  `READY`.

### Technical Impact

No implementation is authorized by this proposal. Event history, routes, runtime composition,
packages, sibling repositories, and production state remain unchanged.

The eventual implementation remains the spine's staged compatibility migration: supported reads,
then durable decisions and recovery, then safe operations and release evidence, with single-writer
cutover, shadow-read comparison, explicit owner contracts, and rollback/freeze boundaries.

### MVP and Release Impact

The v1 scope is unchanged, and no approved FR or NFR is deferrable from production release.
Corrective development and sprint planning remain blocked until `READY`. Production release,
consequential autonomous MCP mutation, and proposed-Project confirmation remain blocked until
Story 8.11 passes and Jerome and John record dated terminal acceptance.

## 3. Recommended Approach

### Selected Path: Direct Backlog and Evidence Adjustment

Use the already approved replacement inventory and final Architecture Spine to materialize the
remaining planning artifacts:

1. Atomically rewrite Epics 6–8 into the approved 7/15/11 outcome stories with complete BDD,
   ownership, dependency, verification, evidence, estimate, compatibility, and rollback detail.
2. Author the canonical `implementation-readiness-traceability-matrix.yaml` and its Markdown view;
   add every FR/NFR/finding/release row and wire the planned AD-30 validation contract truthfully.
3. Reconcile traceability, including explicit owners for FR-19, FR-23, FR-24, NFR-2, NFR-7,
   NFR-8, Chatbot accessibility/integration, all P1/P2 findings, and Story 8.11.
4. Run independent implementation readiness in a fresh context.
5. Only if the verdict is exactly `READY`, atomically reconcile sprint tracking and begin the
   normal create-story/dev sequence one approved story at a time.

The completed architecture step is not repeated. A separate UX redesign is not an entry condition
for the epics rewrite; the missing Chatbot companion obligation is expressed as explicit story,
repository-owner, and evidence rows under the existing PRD/addendum/AD-34 contract.

### Option Evaluation

#### Option 1 — Direct Adjustment

**Viable and selected.** Product, architecture, and operator UX baselines are strong. No corrective
code has begun. The remaining work is bounded backlog organization and evidence-contract creation.

- Planning effort: Medium.
- Subsequent implementation effort: High; estimate per replacement story.
- Current risk: High because the full corrective release scope is unschedulable.
- Expected planning risk after completion: Medium, controlled by AD-30 and independent readiness.
- Timeline effect: implementation remains unscheduled until `READY`; no credible release date is
  inferred from the placeholder inventory.

#### Option 2 — Potential Rollback

**Not viable or beneficial.** No corrective implementation exists to roll back. Reverting the final
PRD or Architecture Spine would discard resolved decisions and recreate uncertainty.

#### Option 3 — PRD/MVP Review

**Not viable for this trigger.** The PRD already makes the release cut explicit and has no
deferrable v1 FR/NFR. Scope reduction would not repair missing acceptance criteria, ownership, or
release evidence.

### Scope Classification

**Moderate.** The prior rebaseline was Major; this amendment does not reopen product or architecture
decisions. It requires material backlog reorganization, traceability, and test/evidence ownership
before development. Product Owner/Test Architect coordination is primary, with Solution Architect
conformance review and named cross-repository owners.

## 4. Detailed Change Proposals

### 4.1 PRD

**OLD:** Final 24-FR/11-NFR PRD and addendum.

**NEW:** No text change. Preserve them as governing product and downstream-routing authority.

**Rationale:** The readiness report found the PRD complete, testable, and internally consistent.

### 4.2 Architecture

**OLD:** `epics.md` frontmatter and content cite and encode the superseded May architecture.

**NEW:** Use
`architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md` as the sole current
architecture input and trace every corrective story to applicable ADs. Keep `architecture.md` only
as explicitly superseded historical evidence.

**Rationale:** The spine is final and binds all FRs, NFRs, corrective epics, and release evidence.

### 4.3 Requirements Inventory and Historical Labels

**OLD:**

```text
Source: PRD §4 (FR-1–FR-22)
...
All 22 FRs mapped. NFR-1–9 are cross-cutting.
```

**NEW:**

```text
Source: final PRD §6–§8 and addendum (FR-1–FR-24; NFR-1–NFR-11).
Epics 1–5 are completed implementation history and internal evidence, not current release authority.
Epics 6–8 are the sole future corrective plan after independent readiness returns READY.
Every FR, NFR, P1/P2 finding, external gate, and release case maps to an AC-bearing story and an
identically keyed AD-30 evidence row.
```

Add explicit supersession notices to Stories 1.4, 2.4, and 5.12 without rewriting historical
completion records.

**Rationale:** Prevent future agents from executing stale folderless-activation or false-pass
criteria while preserving audit history.

### 4.4 Replace the Corrective Story Inventory Atomically

**OLD:** 23 title-and-paragraph placeholders: Stories 6.1–6.7, 7.1–7.7, and 8.1–8.9.

**NEW:** the following approved outcomes, each expanded using the story completion contract in
section 4.5.

#### Epic 6 — Authorized Project Reads on the Supported Platform

| Story | Outcome |
| --- | --- |
| 6.1 | List and open Projects through supported authenticated paths |
| 6.2 | Retrieve Conversation-start setup with admission truth |
| 6.3 | Retrieve assembled Project Context through supported read models |
| 6.4 | Resolve Projects with transient current explanations |
| 6.5 | Inspect Projects through an authenticated FrontComposer read surface |
| 6.6 | Inspect Projects through an authenticated CLI read surface |
| 6.7 | Cut over supported reads while preserving compatibility and rollback |

Epic 6 entry gate pins approved EventStore, FrontComposer, Builds, identity, runner, and sibling
capabilities. The gate is a prerequisite, not delivered user value.

#### Epic 7 — Durable Project Decisions and Cross-Context Recovery

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

Epic 7 entry gate requires approved, pinned platform Durable Task/Confirmation Artifact and sibling
owner contracts. It preserves immutable event history and single-writer cutover.

#### Epic 8 — Safe Operations and Release Confidence

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
| 8.11 | Complete deployment, rollback evidence, and stakeholder acceptance |

Epic 8 entry gate supplies deterministic persisted-boundary fixtures and blocking CI. Story 8.11
cannot complete by recording a blocker or unavailable environment.

**Rationale:** This is the already approved 7/15/11 decomposition, now reconciled to the finalized
spine. It separates user/operational outcomes and failure boundaries instead of promoting technical
finding bundles.

### 4.5 Replacement Story Completion Contract

For every story above, replace the current one-paragraph scope with this structure:

```text
Story <id>: <outcome title>

As a <named beneficiary>,
I want <bounded observable capability>,
So that <user, operator, safety, or release outcome>.

Traceability: FR-...; NFR-...; AD-...; UX/UJ-...; finding/release rows...
Repository authority and owner: ...
Pinned entry gates and prior-only dependencies: ...

Acceptance Criteria:
- Given <authorized/current positive state>, when <action>, then <authoritative outcome>.
- Given <denied/cross-Tenant/stale/unavailable state>, when <action>, then fail closed with no
  protected access or partial durable effect.
- Given <duplicate/replay/restart/concurrency/lost-response state where applicable>, when retried,
  then converge to the original durable truth.
- Given <compatibility/migration state where applicable>, when cut over or rolled back, then
  preserve readable history and single-writer behavior.
- Given the declared fixture and command, when verification runs, then the named evidence artifact
  records an honest pass/fail result; failure, unexplained skip, or unavailable environment cannot
  satisfy completion.

Environment/fixture: ...
Verification command(s): ...
Evidence artifact(s): ...
Estimate: ...
Compatibility/rollback boundary: ...
Completion boundary and release disposition: ...
```

Do not create story implementation files while authoring this planning layer. `epics.md` remains the
planning authority until independent readiness passes and sprint planning authorizes story entry.

### 4.6 Explicit Requirement and Evidence Ownership

| Gap | Primary owning story | Required binding evidence |
| --- | --- | --- |
| FR-19 canonical Metadata Classification / AD-31 / E-9 | 7.1, supported by 6.7 contract cutover | Canonical/legacy request matrix, four-value enum, shared validator parity, authorization-before-parse, safe `400`, no command submission, compatibility and rollback evidence |
| FR-23 Restore Archived Project | 7.14 | Preview/confirmation, Folder validation or replacement, Archived-until-confirmed, `NeedsAttention`, no owner-resource deletion, restart/lost-response evidence |
| FR-24 Safe Diagnostic Export | 8.2 | Separate permission, `projects.safe-diagnostic-export.v1`, 1 MiB/500/100 bounds, two leases/Tenant, deterministic order, no cursor/retention, attempt/outcome audit |
| NFR-2 encryption/KMS | 8.11, supported by 8.6 and 8.7 | Authenticated encryption, platform-managed at rest, KMS/secret provider pin, rotation/revocation evidence, fail-fast configuration, deployment disposition |
| NFR-7 back-pressure/dependency control | 8.9 | Declared read/admission/task/export limits, timeout/retry rules, structured overload guidance, authenticated performance artifacts |
| NFR-8 retention/transience | 8.1 and 8.2, with Epic 7 task/confirmation stories | 30-day-or-result-lifetime task/idempotency retention, 15-minute confirmation expiry, 365-day audit, nonpersistent traces/exports |
| Chatbot NFR-9 / SM-5 / AD-34 | 8.8 | Separately approved Chatbot owner/revision; authenticated candidate/proposal/confirm/cancel/recovery/task evidence; keyboard, focus/live region, screen reader, 200% zoom, 320px reflow |
| Nine P1 + seven P2 audit findings | Applicable 6.x–8.x stories | One matrix row per stable finding ID with owner, revision, command, artifact, status, and terminal release disposition |
| Release handoff and live E2E failures | 8.8, 8.10, 8.11 | Deterministic authorized tenant fixture, required UI/static assets, 19/56 superseding run, deployment/smoke/rollback, dated Jerome/John acceptance |

Supporting stories may share evidence, but each row has one primary owner and no requirement may be
considered mapped solely by an epic-level statement.

### 4.7 Canonical AD-30 Evidence Matrix

**OLD:** No YAML or Markdown traceability matrix exists. The prior proposal mentioned only a
Markdown matrix.

**NEW:** Create:

```text
_bmad-output/planning-artifacts/implementation-readiness-traceability-matrix.yaml
_bmad-output/planning-artifacts/implementation-readiness-traceability-matrix.md
```

The YAML file is canonical with schema `hexalith.readiness-evidence.v1`; Markdown is a generated or
mechanically reconciled human view over the same stable row keys.

Each row contains at least:

- stable row key and category (`fr`, `nfr`, `finding`, `release`);
- requirement/finding/release-case IDs and description;
- architecture decisions and UX/user journeys;
- primary story and supporting stories;
- repository and named owner;
- pinned revision/version and dependency/entry gates;
- deterministic environment and fixture;
- exact verification command;
- evidence artifact path;
- estimate;
- status; and
- terminal release disposition.

Required row coverage is FR-1–FR-24, NFR-1–NFR-11, all nine P1 and seven P2 findings, and every
critical release category required by NFR-11/AD-30. Validation must reject duplicate or missing
keys, unresolved placeholders, incomplete ownership/version/command/artifact fields, failed
critical evidence, unexplained critical skips, and `passed` for unavailable environments.

The planned validation contract is:

```text
dotnet tool run hexalith-evidence validate \
  _bmad-output/planning-artifacts/implementation-readiness-traceability-matrix.yaml
```

This command is a target gate, not a claim that the tool already exists. Until the Builds/platform
owner supplies and pins the AD-30/G-4 capability, the row and gate status must truthfully record the
external blocker and may not be marked `passed`.

### 4.8 Sprint Tracking

**OLD:** `sprint-status.yaml` contains the 23 current placeholders as `backlog`.

**NEW:** Leave it unchanged during planning correction and readiness review. After and only after a
fresh assessment returns exactly `READY`, atomically:

- preserve Epics 1–5 as `done`;
- preserve Epics 6–8 as `backlog` until their first approved story is created;
- replace the 23 placeholder keys with the approved 33 replacement keys;
- reroute existing action items to the new story IDs;
- add the `READY` provenance and containment note; and
- preserve all production, MCP, proposal-confirmation, external-authority, and evidence gates.

**Rationale:** Partial sprint reconciliation could make a placeholder appear schedulable and would
contradict AD-30's atomicity rule.

## 5. Implementation Handoff

### Scope Classification

**Moderate:** backlog reorganization and evidence ownership under an already approved product and
architecture baseline.

### Required Route

1. Product Owner authors the complete Epic 6–8 replacement in `epics.md` using the approved 33
   outcomes and this proposal's completion contract.
2. Solution Architect performs a conformance review against the final Architecture Spine and
   external gates; no architecture reopening by default.
3. Test Architect authors/reconciles the YAML/Markdown evidence matrix, deterministic fixtures,
   commands, artifacts, NFR/release rows, and critical no-skip policy with story authors.
4. Chatbot presentation owner accepts the separately authorized NFR-9/SM-5 evidence obligation and
   supplies repository-local revision/verification ownership.
5. Product Owner and Test Architect perform atomic FR/NFR/finding/release traceability
   reconciliation.
6. Independent assessor reruns implementation readiness in a fresh context. Required result:
   `READY`.
7. Product Owner runs sprint planning and atomically reconciles `sprint-status.yaml` only after
   `READY`.
8. Developer begins only from a readiness-approved story subsequently created as `ready-for-dev`.

### Responsibilities

#### Product Owner

- Replace—not promote—the 23 placeholders.
- Author all 33 outcome stories with complete BDD, owner/dependency/verification/evidence/estimate
  detail.
- Rebuild the 24-FR/11-NFR maps and historical supersession notes.
- Reconcile sprint tracking only after `READY`.

#### Solution Architect

- Verify every story conforms to applicable ADs and G-1 through G-6 entry gates.
- Prevent implicit sibling/platform authority, event rewrite, unsafe dual writes, or false target
  dependency claims.

#### Test Architect

- Own the canonical evidence schema instance and row completeness.
- Define deterministic persisted-boundary, identity, restart/concurrency, accessibility,
  performance, deployment, rollback, and stakeholder evidence.
- Ensure failed/skipped/unavailable critical cases cannot satisfy completion.

#### Chatbot Presentation Owner

- Own separately authorized end-user companion interaction and accessibility evidence.
- Bind a pinned repository revision and authenticated verification artifacts without granting
  Projects implicit authority to mutate Chatbot.

#### Independent Readiness Assessor

- Assess only the reconciled current artifacts in a fresh context.
- Require executable stories and complete evidence ownership; historical implementation is evidence,
  not release authorization.

#### Developer

- Do not create or implement a 6.x/7.x/8.x story before `READY` and sprint reconciliation.
- Preserve event history, bounded-context ownership, compatibility, and rollback gates.

#### Release Owners: Jerome and John

- Retain the release block until Story 8.11 and all critical evidence pass.
- Record dated residual-risk dispositions and terminal acceptance.

### Success Criteria

- Epics 6–8 contain 33 outcome-based, AC-bearing, independently verifiable stories.
- `epics.md` cites the final Architecture Spine and maps every corrective story to applicable ADs.
- FR-1–FR-24 and NFR-1–NFR-11 have complete AC-bearing story ownership.
- FR-19, FR-23, FR-24, NFR-2, NFR-7, NFR-8, and Chatbot NFR-9/SM-5 have the explicit owners and
  evidence defined in section 4.6.
- The canonical AD-30 YAML matrix and matching Markdown view exist with complete FR/NFR/P1/P2/release
  rows and no unresolved placeholder values.
- Epic 8 ends at Story 8.11 and binds deployment, rollback, and dated Jerome/John acceptance.
- Live E2E, audit findings, and blocked release evidence remain honestly open until superseded by
  passing or authorized terminal evidence.
- An independent implementation-readiness rerun returns exactly `READY` before sprint planning or
  corrective story creation.

### Immediate Containment

Until all success criteria and gates are satisfied:

- no corrective implementation story is created or advanced;
- no sprint status is partially reconciled;
- no production release proceeds;
- no consequential autonomous MCP operation or proposed-Project confirmation is enabled;
- no failed, skipped, blocked, or unavailable critical evidence is represented as passing;
- no event history is rewritten and no unsafe dual writer is introduced; and
- no sibling repository is changed without separate repository-local authorization and validation.

## 6. Change Navigation Checklist Status

### Section 1 — Trigger and Context

- [x] 1.1 Trigger identified: Story 6.1 self-block plus the 2026-07-16 independent `NOT_READY`
  rerun.
- [x] 1.2 Core problem classified as incomplete approved planning materialization, not new product
  scope.
- [x] 1.3 Current PRD, architecture, UX, epics, sprint status, automation, audit, live E2E, and
  release evidence recorded.

### Section 2 — Epic Impact

- [x] 2.1 Current corrective epics cannot complete from their placeholder form.
- [x] 2.2 Required replacement is the approved 7/15/11 outcome inventory.
- [x] 2.3 Epics 1–5 and all remaining corrective work reviewed.
- [x] 2.4 No new product epic is required; ten additional bounded story outcomes are required by
  decomposition and release-gate reconciliation.
- [x] 2.5 Epic order remains 6 → 7 → 8; planning is authored atomically before readiness.

### Section 3 — Artifact Conflict and Impact

- [x] 3.1 PRD reviewed; no edit or MVP change required.
- [x] 3.2 Architecture reviewed; no new AD required; epics must consume the final spine.
- [x] 3.3 UX reviewed; operator spec remains valid; Chatbot companion evidence needs explicit
  cross-repository story ownership.
- [x] 3.4 Traceability, evidence validation, sprint status, tests/CI, audit findings, E2E, and release
  handoff impacts documented.

### Section 4 — Path Forward

- [x] 4.1 Direct adjustment viable and selected; Medium planning effort, High current risk.
- [x] 4.2 Rollback not viable or beneficial.
- [x] 4.3 PRD/MVP review not viable for this trigger.
- [x] 4.4 Direct backlog/evidence adjustment selected with containment and independent readiness.

### Section 5 — Proposal Components

- [x] 5.1 Issue summary complete.
- [x] 5.2 Epic and artifact impacts complete.
- [x] 5.3 Recommended path and alternatives complete.
- [x] 5.4 MVP impact, sequencing, and action plan complete.
- [x] 5.5 Moderate-scope handoff roles and responsibilities defined.

### Section 6 — Final Review and Handoff

- [x] 6.1 Applicable checklist sections reviewed and all action-needed work recorded.
- [x] 6.2 Proposal reconciled to the final PRD, final Architecture Spine, current readiness
  report, and prior approved proposal.
- [x] 6.3 Complete proposal approved explicitly by Jerome on 2026-07-16.
- [N/A] 6.4 `sprint-status.yaml` must remain unchanged until an independent rerun returns exactly
  `READY`; the proposed atomic update is documented in section 4.8.
- [x] 6.5 Handoff roles, sequence, containment, and success criteria confirmed; the planning
  package is routed to the Product Owner and Test Architect, with Solution Architect conformance
  review and named Chatbot/repository owners.

## 7. Approval and Routing

Status: **Approved by Jerome on 2026-07-16**.

Approval authorizes planning-artifact correction only: the Epic 6–8 rewrite, evidence matrix,
traceability reconciliation, and independent readiness rerun. It does not authorize corrective
implementation, story-file creation, sprint activation, sibling-repository mutation, production
release, consequential autonomous MCP operations, proposed-Project confirmation, or weakening any
evidence gate.

Change scope: **Moderate**.

Handoff route: **Product Owner and Test Architect**, with Solution Architect conformance review and
named Chatbot/repository owners. The Developer receives no implementation handoff until an
independent assessment returns exactly `READY`, sprint tracking is reconciled atomically, and an
approved story is subsequently created as `ready-for-dev`.

## 8. Workflow Execution Log

| Field | Recorded result |
| --- | --- |
| Date | 2026-07-16 |
| User | Jerome |
| Mode | Batch |
| Trigger | July 16 independent implementation-readiness rerun: `NOT_READY` |
| Baseline disposition | Preserve final PRD, final Architecture Spine, and current operator UX |
| Epic disposition | Replace 23 placeholders atomically with approved 33-story inventory |
| Evidence disposition | Create AD-30 canonical YAML plus matching Markdown view |
| Sprint tracking | No change before independent `READY` |
| Scope | Moderate |
| Route | Product Owner + Test Architect; architecture conformance and Chatbot owner review |
| Handoff | Planning package finalized and routed; Developer implementation handoff remains gated by `READY` |
| Approval | Approved by Jerome on 2026-07-16 |
| Release disposition | Blocked pending Story 8.11 and terminal evidence |
