---
title: "Sprint Change Proposal: Restore Implementation Readiness After Rerun 3"
date: 2026-08-02
status: approved
workflow: bmad-correct-course
review_mode: batch
change_scope: moderate
trigger: "The 2026-08-02 implementation-readiness rerun 3 returned NOT READY with four implementation blockers and one major planning-quality issue."
source_report: implementation-readiness-report-2026-08-02-rerun-3.md
requirements_coverage: "24/24"
recommended_approach: direct-adjustment
approval_status: "approved by Jerome on 2026-08-02"
application_status: applied
routed_to: "Product Owner / Solution Architect / Test Architect / named prerequisite and Chatbot owners"
---

# Sprint Change Proposal: Restore Implementation Readiness After Rerun 3

## 1. Issue Summary

The current product baseline remains coherent: all 24 Functional Requirements and all 11
Non-Functional Requirements have production-authority owners, the PRD, Architecture Spine, and
internal Projects UX agree, and the 33 production stories are otherwise structurally sound.

Implementation remains **NOT READY** because four acceptance inputs are absent or incomplete:

1. the Solution Architect has not executed or signed the architecture-conformance checklist;
2. Story 6.1's external prerequisite chain remains open and is not executable from an accepted clean
   checkout;
3. the independently owned Chatbot `8.8-P3` companion UX/evidence package is missing; and
4. `8.3-P2` and `8.3-P3` are summary ledger rows rather than independently acceptable packages.

One additional major planning-quality issue remains: Epic 8 combines an operator-value track
(Stories 8.1–8.6) and a release-qualification track (Stories 8.7–8.11) without an explicit approved
cohesion exception.

### Trigger and evidence

| Evidence | Observed state | Consequence |
| --- | --- | --- |
| `implementation-readiness-report-2026-08-02-rerun-3.md` | `NOT READY`; five material findings | Production-authority implementation remains blocked |
| `epics-architecture-conformance-checklist-2026-07-16.md` | Sections A–D unchecked; Epic verdicts and overall verdict blank; Solution Architect/date blank | Independent architecture acceptance is absent |
| Story 6.1 ledger | `P1R → {P0, P2} → P3 → P4` remains open; accepted clean-checkout runner/evidence capability absent | The first production story cannot start |
| Workspace search and `sprint-status.yaml` | `8.8-P3` is `blocked-external`; no approved Chatbot companion artifact exists | Stories 8.8 and 8.11 remain blocked |
| `epics.md` and `sprint-status.yaml` | `8.3-P2` and `8.3-P3` contain only outcome, owners, state, and a broad expected-evidence sentence | Story 8.3 lacks independently acceptable entry inputs |
| Epic 8 narrative | Two stakeholder tracks are named but no formal exception is recorded | Strict epic-cohesion quality gate remains open |

### Problem statement

> The product and architecture are not being changed, but implementation authority cannot open
> because the first-story acceptance chain lacks independent architecture sign-off and accepted
> external capabilities, two required Web packages lack finite acceptance contracts, the external
> Chatbot release input is absent, and Epic 8's deliberate two-track structure is not governed by an
> explicit cohesion exception.

## 2. Impact Analysis

### Epic impact

#### Epic 6 — viable, first in sequence, and blocked

Epic 6 remains the correct first production epic. Story 6.1 cannot start until the external chain is
accepted, the architecture-conformance review is signed against the exact current artifact and
dependency baseline, the clean-checkout command sequence passes, the Story 6.1 specification passes
ready-for-development review, and an independent assessment returns exactly `READY`.

The current critical path also contains a sequencing ambiguity: `sprint-status.yaml` lists the
independent `READY` rerun inside P4's expected evidence, while `epics.md` correctly places that rerun
after P4. The corrected path makes P4 an input to the independent rerun rather than requiring P4 to
contain its own downstream result.

#### Epic 7 — no change

Epic 7 remains correctly sequenced after Epic 6. Its 15 stories have complete outcome and
acceptance boundaries. No story, requirement, or priority change is proposed.

#### Epic 8 — viable after package materialization and a cohesion exception

Stories 8.1–8.6 provide operator value. Stories 8.7–8.11 qualify packages, evidence, resilience, and
the terminal release decision. Splitting the epic would force a large migration of stable story
references, prerequisite packages, Architecture Spine bindings, and 63 canonical evidence-row
relationships without changing execution order or closing an implementation blocker.

The recommended correction therefore retains Epic 8 and records an explicit, reviewable cohesion
exception with independent track boundaries, completion semantics, ownership, and reopen triggers.
`8.3-P2` and `8.3-P3` are expanded to the same objective package standard established by
`8.3-P1`.

#### Epics 1–5 — unchanged history

Historical Epics 1–5 remain immutable implementation evidence and never regain production or
release authority.

### Story and package impact

| Item | Change |
| --- | --- |
| Story 6.1 gate | Add signed architecture-conformance evidence before P4; remove the downstream independent rerun from P4's own expected evidence |
| `8.3-P2` | Materialize archive/restore entry gates, fixtures, acceptance criteria, commands, evidence manifest, estimate, completion, and rollback |
| `8.3-P3` | Materialize move/Folder-replacement/unlink entry gates, fixtures, acceptance criteria, commands, evidence manifest, estimate, completion, and rollback |
| Story 8.3 | Preserve as bounded integration of immutable accepted P1/P2/P3 inputs |
| `8.8-P3` | Preserve `blocked-external`; clarify accountable Chatbot owners and the Projects-side accepted-pin record |
| Epic 8 | Add an explicit cohesion exception; retain story and evidence IDs |

### Artifact conflicts and dispositions

| Artifact | Impact | Proposed disposition |
| --- | --- | --- |
| PRD | No requirement or MVP conflict | Preserve unchanged |
| PRD addendum | Product semantics remain valid; current readiness provenance stops at E-14 | Add provenance-only E-15/E-16 rows after approval; no requirement change |
| Architecture Spine | Existing AD-1…AD-34 already govern the correction | Preserve unchanged |
| Internal Projects UX | Complete and aligned | Preserve unchanged |
| External Chatbot UX/evidence | Required independent artifact absent | Route to Chatbot Presentation/Test owners; Projects records only an accepted immutable pin |
| `epics.md` | P2/P3 incomplete; Epic 8 exception absent; Story 6.1 sign-off sequencing implicit | Apply the detailed edits in Section 4 after approval |
| Architecture-conformance checklist | Review not executed or signed; P2/P3 and exception checks incomplete | Reconcile review inputs, then require Solution Architect execution and sign-off |
| Traceability YAML/Markdown | Stable rows exist, but P2/P3 manifest bindings and current provenance are incomplete | Preserve row keys; add package-manifest bindings and regenerate the Markdown view |
| `sprint-status.yaml` | P2/P3 rows underspecified; P4 sequencing ambiguous; Chatbot accountability incomplete | Reconcile after approval without marking any blocker done |

### Technical impact

This proposal changes planning and acceptance authority only. It does not implement Durable Tasks,
Confirmation Artifacts, Web adapters, Chatbot presentation, identity/KMS, runner/evidence tooling,
or production code. It changes no event history, package version, runtime route, sibling repository,
or deployment.

## 3. Recommended Approach

Use **Direct Adjustment** and retain containment.

1. Materialize `8.3-P2` and `8.3-P3` as finite independently accepted packages.
2. Make architecture sign-off an explicit Story 6.1 gate input and remove the P4/rerun sequencing
   ambiguity.
3. Record and approve the Epic 8 cohesion exception rather than splitting stable authority IDs.
4. Preserve `8.8-P3` as an independently owned external deliverable and strengthen owner/pin
   routing without creating a Projects-authored substitute.
5. Reconcile epics, conformance, traceability, sprint tracking, and addendum provenance only after
   this proposal is approved.
6. Rerun readiness only after real acceptance records exist.

### Options evaluated

| Option | Viability | Effort | Risk | Decision |
| --- | --- | --- | --- | --- |
| Direct Adjustment | Viable | Medium planning effort; external execution timing uncommitted | Medium after correction | **Recommended** |
| Split Epic 8 | Viable but disproportionate | High migration effort | High traceability and evidence-key churn | Reject unless the cohesion exception is not approved |
| Potential Rollback | Not viable | High | High | No production-authority implementation needs reversal and rollback supplies none of the missing evidence |
| MVP Review / scope reduction | Not viable | High | High | All approved FRs/NFRs are release-binding; scope reduction cannot manufacture platform, conformance, or Chatbot evidence |

### Effort, risk, and timeline impact

- Planning correction: **Medium**, expected to fit one focused Product Owner/Solution Architect/Test
  Architect review cycle.
- `8.3-P2`: **L** implementation/evidence package.
- `8.3-P3`: **L** implementation/evidence package.
- External Story 6.1 and Chatbot package execution: **uncommitted** until accountable owners accept
  immutable revisions, commands, artifacts, and rollback.
- Residual risk: **Medium** after artifact correction and **High** while any external blocker remains.
- No production date may be inferred from this proposal.

### Scope classification

**Moderate.** The product goals, requirements, architecture decisions, and production-story
inventory remain intact. The work reorganizes acceptance contracts and backlog governance and
requires Product Owner/Developer coordination with Solution Architect, Test Architect, Web, platform,
and Chatbot owners.

## 4. Detailed Change Proposals

### 4.1 Make architecture sign-off an explicit Story 6.1 gate input

**Artifacts:** `epics.md`, `epics-architecture-conformance-checklist-2026-07-16.md`,
`sprint-status.yaml`, traceability YAML/Markdown.

**OLD — Story 6.1 critical path:**

```text
P1R → {P0, P2} → P3 → P4 → independent readiness rerun → Story 6.1
```

**NEW — Story 6.1 critical path:**

```text
P1R → {P0, P2} → P3
    → Solution Architect conformance sign-off against the exact current baseline
    → P4 clean-checkout gate acceptance
    → Story 6.1 specification readiness
    → independent implementation-readiness rerun
    → Story 6.1 only when the rerun returns READY
```

**OLD — P4 expected evidence in `sprint-status.yaml`:**

```yaml
expected_evidence: "evidence/epic6/6.1-entry-gate.yaml; accepted revisions and owner approvals; executable rollback; passing exact clean-checkout commands; passing Story 6.1 ready-for-development result; independent implementation-readiness result READY"
```

**NEW:**

```yaml
expected_evidence: "evidence/epic6/6.1-entry-gate.yaml; accepted P0, P1R, P2, and P3 revisions and owner approvals; signed Solution-Architect conformance checklist against the same baseline; executable rollback; passing exact clean-checkout restore/run/full-test/down/evidence-validation commands"
```

The Story 6.1 ready-for-development result and independent `READY` rerun remain downstream gates,
not evidence that P4 must somehow contain before P4 can complete.

**Conformance-review execution contract:**

1. Update the checklist review set with the rerun-3 report and this approved proposal.
2. Record the exact PRD/addendum, Architecture Spine, UX, epics, traceability-matrix, and prerequisite
   revision/hash baseline reviewed.
3. The Solution Architect executes every applicable Section A–D check and records notes for open
   G-gates rather than marking them accepted.
4. Record the Epic 6/7/8 planning-conformance verdicts, overall verdict, Solution Architect identity,
   and date.
5. `conforms-with-notes` may describe correctly modeled but unaccepted external gates; it cannot
   convert an open gate into execution readiness.
6. An AI readiness assessor or Product Owner must not manufacture the Solution Architect signature.

**Rationale:** Makes the independent architecture review a real acceptance input while preserving
the separation between planning conformance, external capability acceptance, Story 6.1 readiness,
and the independent implementation-readiness verdict.

### 4.2 Materialize the `8.3-P2` archive/restore Web package

**Artifact:** `epics.md`, immediately after the accepted `8.3-P1` package contract.

**OLD:**

> `8.3-P2` — Archive and restore Web journeys, including Folder-before-activation restore and
> role/denial/renewal evidence. Owner: Projects Web Owner + Product Owner + Test Architect. State:
> open; blocked by `8.3-P1` and the current implementation-readiness freeze.

**NEW package contract:**

#### 8.3-P2 acceptance contract — archive and restore Web journeys

**Purpose.** Accept the Projects Web adapter's archive and restore journeys over the immutable
`8.3-P1` presentation foundation and the accepted Story 7.13/7.14 server contracts. This package is
an independently reviewable entry input to Story 8.3, not a new production story.

**Entry gate.** Acceptance requires:

1. a superseding implementation-readiness result of exactly `READY`;
2. immutable acceptance of `8.3-P1` and its G-3/G-6 dependency tuple;
3. accepted Story 7.13 archive and Story 7.14 restore capabilities at immutable revisions;
4. accepted Story 8.1 audit truth for expected/resulting metadata-only audit evidence; and
5. an authenticated G-4 runner fixture using platform-provided actor and Tenant authority.

**Repository authority.** Projects owns lifecycle action descriptors, adapter consumption,
Projects fixtures, and route composition. FrontComposer owns generic Fluent V5 presentation,
focus/live-region primitives, and test helpers. EventStore/platform owns Preview, artifact
consumption, task execution, cancellation checkpoints, and authoritative task/read-model truth.
Folders owns Folder existence, authorization, provisioning, and recovery. No browser code decides
authority, task transitions, or Folder validity.

**Deterministic fixture.** `epic8/8.3-p2-lifecycle-journeys` contains versioned cases for:

1. role-visible and role-hidden archive/restore actions for Tenant Operator and Tenant Project
   Administrator, plus cross-Tenant and stale-authority denial;
2. Active → archive Preview, explicit confirmation, admission, all applicable task states, and
   read-model-confirmed `Archived` completion;
3. Archived → restore with a still-authorized Folder;
4. restore with a missing, stale, or unauthorized prior Folder, requiring an authorized replacement
   or same-name Folder plan while the Project remains Archived;
5. Folder-created/activation-uncommitted recovery to `NeedsAttention` without owner-resource
   deletion;
6. expired, stale, replayed, tampered, actor-, Tenant-, target-, and version-mismatched artifacts,
   each admitting no task and exposing canonical `RenewPreview` or safe denial;
7. lost admission response, equivalent retry/`PollTask`, duplicate/stale notifications, and
   authoritative re-query before success;
8. cancellation before and conflict after the irreversible checkpoint, immutable terminal states,
   and Administrator-only reconciliation visibility;
9. metadata-only audit identifiers with no payload, token, artifact, Folder path, or authority-claim
   leakage; and
10. keyboard completion, deterministic focus/restoration, restrained announcements, non-color
    meaning, 200% zoom, and 320 CSS-pixel reflow.

**Acceptance Criteria:**

1. **Canonical admission.** Archive and restore render server Preview and explicit confirm/cancel,
   consume one bound Confirmation Artifact, and then monitor the canonical Durable Task states.
2. **Archive truth.** `202`, a task ID, or SignalR never renders archive success. Success appears only
   after authoritative task and read-model evidence confirms `Archived`; references remain safely
   auditable.
3. **Restore truth.** Restore verifies or establishes Folder validity while the Project remains
   Archived. Replacement/same-name creation is presented from server Preview; the UI never infers a
   Folder plan. `Active` appears only after the expected read model confirms completion.
4. **Recovery.** Lost responses converge through the idempotency identifier and `PollTask` or an
   equivalent safe retry. Partial Folder success renders `NeedsAttention`; no control implies or
   initiates deletion of the Folders-owned resource.
5. **Authorization and confirmation safety.** Role/action visibility consumes AD-33. Denied,
   cross-Tenant, stale, replayed, tampered, and mismatched cases admit no task and reveal no protected
   existence or payload.
6. **Cancellation and terminal integrity.** Cancellation appears only before the irreversible
   checkpoint. Later attempts show current task truth and a safe conflict. Terminal states never
   transition.
7. **Accessibility and privacy.** Every state/control satisfies the fixture's keyboard, focus,
   announcement, zoom, reflow, non-color, and metadata-only requirements with no unresolved critical
   or serious finding.
8. **Evidence.** All required clean-checkout commands pass; the immutable manifest contains
   repository revisions, dependency/contract/fixture hashes, commands, exit codes, artifacts,
   results, owner approvals, containment, and executable rollback. Missing environments or skips do
   not pass.

**Verification commands:**

```bash
DiffEngine_Disabled=true dotnet test tests/Hexalith.Projects.UI.Tests/Hexalith.Projects.UI.Tests.csproj \
  --filter "Category=Story8.3P2" \
  --logger "trx;LogFileName=8.3-p2-projects-lifecycle.trx" \
  --results-directory evidence/epic8

dotnet tool run hexalith-module test \
  --manifest module/hexalith-projects.module.json \
  --profile web-ops \
  --filter Package=8.3-P2

dotnet tool run hexalith-evidence validate \
  evidence/epic8/8.3-p2-lifecycle-manifest.json
```

These commands are acceptance targets. If their categorized tests, runner, or validator do not
exist at the accepted revisions, the package remains blocked rather than skipped or passed.

**Evidence manifest.** `evidence/epic8/8.3-p2-lifecycle-manifest.json` records immutable repository
revisions; accepted P1/G-3/G-6 and Story 7.13/7.14/8.1 pins; fixture/contract hashes; exact commands,
exit codes, artifacts, and results; role/denial/renewal/recovery, audit, privacy, and component-
accessibility results; accountable owner dispositions; containment; and rollback.

**Compatibility and rollback.** Retain the last accepted read-only/lifecycle route and dependency
tuple until Story 8.3 acceptance. On regression, disable the new lifecycle adapter/routes, restore
the prior tuple/route, and rerun the prior smoke lane. Rollback changes no domain event, task,
artifact record, Folder resource, or server workflow.

**Estimate:** L.

**Completion boundary:** The archive/restore package is accepted only when every required command
passes from accepted clean checkouts and the immutable manifest, hashes, results, approvals, and
rollback exist. Association journeys and full authenticated NFR-9 release evidence remain outside
P2.

### 4.3 Materialize the `8.3-P3` association-maintenance Web package

**Artifact:** `epics.md`, immediately after the new `8.3-P2` contract.

**OLD:**

> `8.3-P3` — Conversation move, Folder replacement, and Conversation/File/Memory unlink Web
> journeys with owner-preserving recovery evidence. Owner: Projects Web Owner + Product Owner + Test
> Architect. State: open; blocked by `8.3-P1` and the current implementation-readiness freeze.

**NEW package contract:**

#### 8.3-P3 acceptance contract — association-maintenance Web journeys

**Purpose.** Accept the Projects Web adapter's Conversation move, Folder replacement, and
Conversation/File/Memory unlink journeys over the immutable `8.3-P1` foundation and accepted Epic 7
server contracts. This is an independently reviewable Story 8.3 input, not a production story.

**Entry gate.** Acceptance requires:

1. a superseding implementation-readiness result of exactly `READY`;
2. immutable acceptance of `8.3-P1` and its G-3/G-6 dependency tuple;
3. accepted Stories 7.4, 7.5, 7.6, 7.8, and 7.10 at immutable revisions with the applicable G-1/G-2
   owner-contract pins;
4. accepted Story 8.1 audit truth; and
5. an authenticated G-4 runner fixture using platform-provided actor and Tenant authority.

**Repository authority.** Projects owns action descriptors, adapter consumption, fixtures, and
route composition. FrontComposer owns generic presentation behavior. EventStore/platform owns
Preview, artifacts, tasks, idempotency, checkpoints, and authoritative truth. Conversations,
Folders, and Memories remain the only resource/membership authorities. The Web adapter never
deletes, compensates, or mutates an owner resource directly.

**Deterministic fixture.** `epic8/8.3-p3-association-journeys` contains versioned cases for:

1. Conversation move from an expected source Project to one target Project, preserving exactly one
   membership and presenting owner-confirmed completion;
2. mid-saga move failure after prior removal, showing compensation or truthful `NeedsAttention`
   without claiming success or fabricating membership;
3. Project Folder replacement with old/new Folder evidence, new-Folder verification before bind,
   and exactly one Folder throughout;
4. removal of an Active Project's Folder rejected; no delete control or owner-deletion implication;
5. Conversation, File, and Memory unlink journeys, each preserving the underlying owner resource;
6. role visibility, cross-Tenant denial, stale owner evidence, and indistinguishable safe absence;
7. expired, stale, replayed, tampered, actor-, Tenant-, target-, and version-mismatched artifacts;
8. lost response, equivalent retry/`PollTask`, duplicate notifications, cancellation before/after
   checkpoint, immutable terminal states, and authorized reconciliation;
9. owner receipt and metadata-only audit evidence with no transcript, file content/path, Memory
   payload, token, artifact, raw upstream problem, or authority claim; and
10. keyboard completion, deterministic focus/restoration, restrained announcements, non-color
    meaning, 200% zoom, and 320 CSS-pixel reflow for every journey.

**Acceptance Criteria:**

1. **Canonical admission.** Move, Folder replacement, and each unlink action use server Preview,
   explicit confirm/cancel, one bound artifact, and canonical task monitoring. No local adapter
   synonym changes the action class.
2. **Conversation ownership.** Move presents source/target scope and owner evidence, and success only
   after the Conversations owner and Projects reverse index converge to exactly one membership.
   Partial work presents compensation or `NeedsAttention` rather than orphaned/double membership.
3. **Folder invariant.** Replacement verifies the new Folder before binding and never presents an
   Active folderless state. Removal is unavailable/rejected, and rollback or recovery never deletes
   a Folders-owned resource.
4. **Unlink preservation.** Conversation/File/Memory unlink removes only the association. UI copy,
   controls, recovery, and audit evidence never imply deletion of the underlying resource.
5. **Authorization and confirmation safety.** AD-33 role visibility and owner reauthorization are
   contract-driven. Denied, cross-Tenant, stale, replayed, tampered, and mismatched cases admit no
   task and expose no protected existence or payload.
6. **Task/recovery truth.** `202` and SignalR are non-completion signals. Lost-response,
   cancellation/checkpoint, terminal immutability, bounded dependency guidance, and authorized
   reconciliation follow the accepted P1 foundation and authoritative re-query.
7. **Accessibility and privacy.** Every journey passes the fixture's keyboard, focus,
   announcement, zoom, reflow, non-color, metadata-only, and leakage checks with no unresolved
   critical or serious finding.
8. **Evidence.** All required clean-checkout commands pass and the immutable manifest records every
   revision, hash, command, exit code, artifact, result, owner disposition, containment, and
   executable rollback. Missing environments or skips do not pass.

**Verification commands:**

```bash
DiffEngine_Disabled=true dotnet test tests/Hexalith.Projects.UI.Tests/Hexalith.Projects.UI.Tests.csproj \
  --filter "Category=Story8.3P3" \
  --logger "trx;LogFileName=8.3-p3-projects-associations.trx" \
  --results-directory evidence/epic8

dotnet tool run hexalith-module test \
  --manifest module/hexalith-projects.module.json \
  --profile web-ops \
  --filter Package=8.3-P3

dotnet tool run hexalith-evidence validate \
  evidence/epic8/8.3-p3-association-manifest.json
```

These are acceptance targets; unavailable tests/tools keep the package blocked.

**Evidence manifest.** `evidence/epic8/8.3-p3-association-manifest.json` records immutable revisions;
accepted P1/G-3/G-6, Epic 7 action, G-1/G-2 owner-contract, and Story 8.1 pins; fixture/contract
hashes; exact commands, exit codes, artifacts, and results; membership/Folder/resource-preservation,
denial/recovery/audit/privacy/accessibility results; accountable dispositions; containment; and
rollback.

**Compatibility and rollback.** Retain the last accepted read-only/maintenance route and dependency
tuple until Story 8.3 acceptance. On regression, disable the new association action adapters,
restore the prior tuple/route, and rerun the prior smoke lane. Rollback changes no event history,
task/artifact record, owner resource, or server workflow.

**Estimate:** L.

**Completion boundary:** P3 completes only when every required clean-checkout command passes and the
immutable manifest, hashes, results, approvals, and rollback exist. Archive/restore and full
authenticated NFR-9 release evidence remain outside P3.

### 4.4 Record an explicit Epic 8 cohesion exception

**Artifacts:** `epics.md`, architecture-conformance checklist.

**OLD:** Epic 8 names operator and release-owner tracks but does not explicitly justify retaining
both in one epic.

**NEW — add after the Epic 8 introduction:**

#### Epic 8 cohesion exception

Epic 8 deliberately contains two prior-only tracks under one release-blocking outcome:

- **Operator-value track (8.1–8.6):** operators inspect task/audit truth, create bounded exports,
  perform conformant maintenance, use CLI/MCP, and observe truthful health. Each story has an
  independently consumable operator outcome and completes without a later release story.
- **Release-qualification track (8.7–8.11):** downstream consumers and Release Owners qualify the
  package graph, integrate independently owned evidence, prove performance/resilience, acquire
  deployment/KMS/rollback evidence, and record the terminal decision.

The exception is justified because the PRD makes FR-21, FR-22, FR-24, and every NFR release-blocking;
safe operation is not production-releasable without the qualification track, and the qualification
track has no value without the accepted operational capabilities. Splitting now would migrate stable
story/package references, Architecture Spine bindings, and canonical evidence relationships while
leaving the same order and terminal Story 8.11 gate.

Track A may be demonstrated and consumed as internal pre-release evidence after its own story gates
pass; it is not a production release. No 8.1–8.6 story depends on 8.7–8.11 for its own completion.
Epic 8 completes only when Story 8.11 records the terminal decision. Product Owner and Solution
Architect review this exception during conformance sign-off.

Reopen the split decision if a later change introduces unrelated product capability, gives either
track an independent production/release cadence, creates a forward story dependency, or allows
release qualification to move to a platform-owned release epic without breaking stable evidence
identity.

**Conformance addition:**

```text
[ ] Epic 8 cohesion exception reviewed: both tracks are prior-only and independently consumable;
    operator stories do not complete on technical milestones; Story 8.11 remains the sole terminal
    decision; no requirement/evidence identity is hidden by retaining one epic.
```

**Rationale:** Resolves the major cohesion finding explicitly while avoiding high-risk renumbering
that provides no execution benefit.

### 4.5 Reconcile package tracking and traceability without claiming acceptance

#### `sprint-status.yaml`

**OLD `8.3-P2`/`8.3-P3`:** broad expected-evidence summaries and only `8.3-P1` dependencies.

**NEW:**

- keep both `status: open` and `target_date: uncommitted`;
- add Solution Architect to accountable acceptance;
- set P2 dependencies to `8.3-P1`, Stories 7.13/7.14, Story 8.1, and the superseding `READY` result;
- set P3 dependencies to `8.3-P1`, Stories 7.4/7.5/7.6/7.8/7.10, Story 8.1, and the superseding
  `READY` result;
- bind P2 to `evidence/epic8/8.3-p2-lifecycle-manifest.json` and this proposal Section 4.2;
- bind P3 to `evidence/epic8/8.3-p3-association-manifest.json` and this proposal Section 4.3;
- copy the full expected evidence categories, completion, containment, and rollback from the package
  contracts; and
- do not mark a package accepted because its tests/tools are absent, skipped, local-only, mutable,
  or ownerless.

#### Traceability YAML and generated Markdown

Preserve all 63 stable row keys. Update only the authoritative YAML, then regenerate Markdown.

- `finding-ux-001` continues to map to Story 8.3 but names the P1, P2, and P3 manifest paths as
  immutable entry evidence.
- `fr-22` and `nfr-9` keep their existing story owners; their blocker/notes name P2/P3 package
  acceptance where applicable without treating component evidence as full NFR-9 release evidence.
- Add non-authoritative notes defining P2/P3 package boundaries and retaining `8.8-P3` as external.
- No status changes to `passed` are authorized by this proposal.

#### `8.8-P3` owner and pin routing

**OLD tracking owner:** `Chatbot Presentation Owner / Test Architect / Product Owner`.

**NEW tracking owner:**

```text
Chatbot Presentation Owner / Chatbot Test Owner / Product Owner / Test Architect
```

The Chatbot owners create and approve the source manifest in their repository. After independent
acceptance, Projects records only an immutable pin and evidence-row binding at
`evidence/epic8/8.8-P3-chatbot-companion-pin.json`. Absence of either the owner manifest or the
Projects pin remains `blocked-external`; Projects does not author a substitute companion artifact.

### 4.6 Reconcile PRD-addendum provenance without changing the product contract

**Artifact:** `prds/prd-Hexalith.Projects-2026-05-24/addendum.md`.

Add two evidence rows after E-14:

- **E-15:** `implementation-readiness-report-2026-08-02-rerun-3.md` — current `NOT READY`
  implementation disposition; 24/24 FR coverage; four implementation blockers and one Epic 8
  planning-quality issue.
- **E-16:** this proposal — proposed Moderate direct adjustment; no implementation authority; upon
  approval, governs P2/P3 materialization, architecture-sign-off sequencing, the Epic 8 cohesion
  exception, and external Chatbot routing.

Update the addendum's current-readiness paragraph to state that E-13 remains historical
planning-layer authorization, while E-15 is the current implementation disposition. This is
provenance/status reconciliation only; FR-1–FR-24, NFR-1–NFR-11, architecture mechanisms, and UX
semantics remain unchanged.

### 4.7 Application sequence after approval

1. Apply the Story 6.1 gate-sequencing correction.
2. Insert the complete P2 and P3 package contracts into `epics.md`.
3. Add the Epic 8 cohesion exception.
4. Reconcile `sprint-status.yaml` while keeping all blockers open.
5. Update the canonical traceability YAML and regenerate its Markdown view.
6. Reconcile addendum provenance as E-15/E-16.
7. Update the conformance checklist review set and detailed P2/P3/exception checks.
8. Run document/reference/schema checks.
9. Route the checklist to the Solution Architect for actual execution and signature.
10. Do not rerun implementation readiness until the signed checklist, Story 6.1 acceptance records,
    and independently owned `8.8-P3` artifact exist. A rerun may still return `NOT READY` if any
    other gate remains open.

## 5. Implementation Handoff

### Product Owner / Developer

- Apply the approved planning edits in the sequence above.
- Preserve stable FR, NFR, story, package, and evidence-row identities.
- Keep Story 6.1, P2, P3, and `8.8-P3` blocked/open until their actual acceptance records exist.
- Do not infer a production date or mark missing tools/environments as passed.

### Solution Architect

- Execute and sign the conformance checklist against the exact accepted baseline.
- Review the Epic 8 cohesion exception explicitly.
- Confirm no platform/sibling authority expansion, event-history rewrite, unsafe dual writer, false
  dependency, or unpinned capability is introduced.

### Platform, Builds, EventStore, identity/security, and prerequisite owners

- Complete the Story 6.1 chain at immutable revisions with accepted commands, artifacts, ownership,
  compatibility, and executable rollback.
- Supply P4's clean-checkout record after the signed conformance review; do not include the later
  independent readiness result inside P4.

### Projects Web, FrontComposer/Web, and Test Architect

- Accept P1 first, then execute P2/P3 against their deterministic fixtures and exact package
  contracts.
- Publish immutable manifests with hashes, results, dispositions, containment, and rollback.
- Keep full authenticated operator NFR-9 evidence in `8.8-P2`, not P1/P2/P3.

### Chatbot Presentation Owner and Chatbot Test Owner

- Produce, approve, and pin the independent Chatbot companion UX/evidence manifest.
- Supply the required authenticated commands, fixtures, artifacts, hashes, results, terminal
  disposition, and drift containment/rollback.
- Projects records the accepted pin only and receives no authority to change Chatbot.

### Test Architect

- Reject mutable revisions, missing environments, absent categorized tests, failed critical cases,
  unexplained skips, and ownerless/unhashed artifacts.
- Regenerate the traceability Markdown from YAML and verify stable row identities.
- Run the independent readiness rerun only after all prerequisite acceptance inputs exist.

### Success criteria

1. The Solution Architect has signed every applicable conformance section and the overall verdict
   against the exact current baseline.
2. P4 contains accepted P0/P1R/P2/P3 plus signed conformance and clean-checkout execution, while
   Story 6.1 specification readiness and the independent rerun remain downstream.
3. `8.3-P2` and `8.3-P3` each have accepted immutable manifests satisfying every contract item.
4. The Epic 8 cohesion exception is approved and conformance-reviewed, or Epic 8 is split through a
   separately approved replan.
5. The external Chatbot `8.8-P3` source manifest and Projects pin record are accepted at immutable
   revisions.
6. Canonical YAML, generated Markdown, epics, sprint status, conformance, and addendum provenance
   agree without marking unavailable evidence passed.
7. A superseding independent implementation-readiness assessment returns exactly `READY` before
   Story 6.1 begins.
8. Story 8.11 remains the separate terminal production-release gate.

## 6. Change Navigation Checklist Record

### Section 1 — Understand the trigger and context

- [x] **1.1** Trigger identified: the rerun-3 assessment, with Story 6.1 as the first blocked
  production story and Stories 8.3/8.8 as downstream blocked integration/release points.
- [x] **1.2** Core problem categorized as an incomplete acceptance chain and planning-package
  specification gap, not a new requirement or strategic pivot.
- [x] **1.3** Evidence recorded from the unsigned conformance checklist, open Story 6.1 ledger,
  absent Chatbot package, summary-only P2/P3 rows, and compound Epic 8 narrative.

### Section 2 — Epic impact

- [!] **2.1** Epic 6 remains viable but cannot start until its acceptance chain and independent
  readiness gate pass.
- [!] **2.2** Epic 8 needs full P2/P3 contracts and an explicit cohesion exception; no requirement
  or production story is added or removed.
- [x] **2.3** All remaining epics reviewed; Epic 7 and historical Epics 1–5 require no change.
- [N/A] **2.4** No epic is invalidated and no new epic is required if the exception is approved.
- [x] **2.5** Order remains Epic 6 → Epic 7 → Epic 8.

### Section 3 — Artifact conflict and impact analysis

- [x] **3.1** PRD reviewed: no product, MVP, FR, or NFR semantic change; addendum needs provenance
  reconciliation only.
- [x] **3.2** Architecture reviewed: AD-1…AD-34 remain sufficient; the missing item is independent
  conformance execution/sign-off, not a new architecture decision.
- [x] **3.3** Internal Projects UX reviewed: aligned and unchanged; the separately owned Chatbot
  artifact remains an external release input.
- [!] **3.4** Epics, conformance, sprint tracking, traceability, and addendum provenance require the
  synchronized edits specified above.

### Section 4 — Path forward evaluation

- [x] **4.1** Direct Adjustment is viable — medium planning effort, medium residual risk after
  correction; external execution remains uncommitted.
- [N/A] **4.2** Rollback is not justified because no current production-authority work must be
  reverted.
- [N/A] **4.3** MVP reduction is not an acceptable substitute for missing conformance/platform/
  Chatbot evidence.
- [x] **4.4** Selected path: Direct Adjustment, explicit Epic 8 exception, external acceptance, then
  an independent readiness rerun.

### Section 5 — Sprint Change Proposal components

- [x] **5.1** Issue summary and trigger evidence complete.
- [x] **5.2** Epic, story/package, and artifact impacts documented.
- [x] **5.3** Recommended path, alternatives, effort, risk, and timeline impact documented.
- [x] **5.4** MVP remains unchanged; sequencing and action plan are explicit.
- [x] **5.5** Handoff roles and responsibilities are defined.

### Section 6 — Final review and handoff

- [x] **6.1** All applicable checklist sections are addressed and open actions are explicit.
- [x] **6.2** Proposal is checked against the canonical PRD/addendum, Architecture Spine, UX,
  complete epics, conformance checklist, traceability view, sprint status, and rerun-3 report.
- [x] **6.3** Jerome approved the complete proposal on 2026-08-02.
- [N/A] **6.4** No epic/story addition, removal, or renumbering is proposed; sprint-status package
  rows are reconciled only after approval.
- [x] **6.5** Handoff responsibilities, success criteria, and containment are explicit.

## 7. Review Decision

Jerome approved this proposal on 2026-08-02, and the authorized planning-artifact edits in Section 4
have been applied to the epics, conformance checklist, sprint status, canonical traceability YAML
and Markdown view, and PRD-addendum provenance. No external repository was changed. Application
does not accept an external gate, create the Chatbot-owned artifact, supply a Solution Architect
signature, lift the implementation freeze, start Story 6.1, or authorize production release. The
current implementation-readiness disposition remains `NOT READY` pending the named acceptance work
and a later independent rerun.
