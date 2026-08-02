---
title: "Sprint Change Proposal: Close the 2026-08-02 Implementation-Readiness Gaps"
date: 2026-08-02
status: approved
approved: 2026-08-02
approved_by: Jerome
workflow: bmad-correct-course
review_mode: batch
change_scope: moderate
trigger: "The 2026-08-02 implementation-readiness assessment returned NOT READY despite 24/24 Functional Requirement coverage."
source_report: implementation-readiness-report-2026-08-02.md
requirements_coverage: "24/24"
missing_required_artifact_types: 0
recommended_approach: direct-adjustment
overall_approval: approved
handoff_status: complete
application_status: applied
applied: 2026-08-02
approval_status: approved
routed_to: "Product Owner and Developer, with Solution Architect, Test Architect, FrontComposer/Web owner, and Chatbot presentation owners"
---

# Sprint Change Proposal: Close the 2026-08-02 Implementation-Readiness Gaps

## 1. Issue Summary

The 2026-08-02 implementation-readiness assessment found a complete product baseline but an
inconsistent and not-yet-executable production plan. All 24 Functional Requirements have a current
production-story owner, the 11 Non-Functional Requirements are represented, and the Architecture
Spine contains the necessary technical invariants. The obsolete May architecture contained no
unique information requiring transfer; it has already been archived as
`archive/architecture-2026-05-24-superseded.md`, with active references updated and historical
point-in-time records preserved.

The remaining blockers are not missing product scope. They are entry-gate, dependency, UX-authority,
and story-quality defects:

1. Story 6.1 remains blocked by the open external prerequisite chain
   `P1R -> {P0, P2} -> P3 -> P4 -> independent readiness rerun -> Story 6.1`.
2. The separately owned, approved, immutable Chatbot companion UX/evidence artifact required by
   8.8-P3 does not exist in the workspace.
3. Stories 6.5 and 6.6 promise audit behavior that is not delivered until Story 8.1, creating a
   forbidden forward dependency.
4. The UX specification applies Preview and Confirmation to every mutation, contradicting PRD
   FR-6/FR-8/FR-9/FR-10 and Architecture AD-5, which preserve task-only actor-selected creation,
   setup, additive-link, and initial-Folder actions.
5. The assessment identified nine additional material or minor issues: noncanonical Resolution
   Trace labels, incomplete operator accessibility evidence language, an oversized Story 8.3, a
   missing Story 7.2 completion condition, reversed Preview/confirmation order in Story 7.14,
   undefined duplicate-export behavior in Story 8.2, malformed BDD syntax in Story 6.7, mixed
   stakeholder outcomes in Epic 8, and system-centric epic titles.

### Change trigger and evidence

- Triggering entry story: **Story 6.1 — List and open Projects through supported authenticated
  paths**.
- Triggering assessment: `implementation-readiness-report-2026-08-02.md`.
- Overall verdict: **NOT READY**.
- Functional coverage: **24/24 FRs; no missing or extra FR identifiers**.
- Artifact-quality findings: **13 across UX alignment and epic/story quality**, plus the already-open
  external Story 6.1 entry chain.
- Missing release input: approved, immutable Chatbot companion UX/evidence package.

### Problem statement

> The product and architecture baselines are complete, but production-authority implementation
> cannot start because the first story lacks accepted external prerequisites, two read-surface
> stories depend on later audit capability, the active UX contradicts the canonical action-admission
> contract, and several stories or evidence gates are not independently completable or precisely
> testable.

## 2. Impact Analysis

### Epic impact

#### Epic 6 — read value remains first, but is blocked and internally inconsistent

Epic 6 remains the correct first production epic. Story 6.1 stays `blocked-external`; no planning
edit may mark P1R, P0, P2, P3, or P4 accepted. Stories 6.5 and 6.6 must stop claiming audit
timeline/command delivery. Their authorized inventory, detail, reference-health, resolution-trace,
and validation outcomes remain independently completable. Web and CLI audit adapters move to Story
8.1, after its task/audit/reconciliation read capability exists.

#### Epic 7 — viable after focused acceptance-criterion repair

Epic 7 remains correctly sequenced after Epic 6 and needs no new product capability. Story 7.2 must
withhold `Succeeded` until the authoritative read model agrees with stored Project Setup. Story
7.14 must issue the Preview before a Confirmation Artifact can be presented or consumed.

#### Epic 8 — retain numbering, separate stakeholder outcomes and independently accepted packages

Epic 8 remains one production epic to avoid renumbering the evidence matrix, PRD addendum, and
Architecture Spine. Its narrative must explicitly separate:

- the **operator outcome track** in Stories 8.1–8.6; and
- the **release-owner qualification track** in Stories 8.7–8.11.

Story 8.3 is decomposed through three independently accepted Web work packages while the stable
Story 8.3 ID becomes their bounded integration story. Story 8.2's synchronous, non-retained export
semantics are repaired without inventing exactly-once or retained idempotency behavior.

#### Epics 1–5 — historical evidence remains unchanged

Historical story contents remain point-in-time evidence. They do not regain production authority and
are not rewritten to mimic the corrective plan. Where a historical term conflicts with current
authority, the active requirements inventory or supersession note identifies the canonical current
meaning.

### Artifact conflicts and dispositions

| Artifact | Impact | Disposition |
| --- | --- | --- |
| PRD and addendum | Complete product baseline; no MVP conflict | No semantic change |
| Architecture Spine | AD-5, AD-30, AD-32, and AD-34 already contain the required authority | No semantic change |
| Archived architecture | Superseded and already archived | Preserve unchanged |
| `epics.md` | Forward dependency, story AC defects, oversized Story 8.3, mixed Epic 8 narrative, stale readiness date | Update after approval |
| `ux-design-specification.md` | Over-broad confirmation rule, local resolution synonyms, incomplete operator evidence wording | Update after approval |
| Chatbot companion UX | Required external release input is absent | Chatbot owner must produce and approve; Projects records only the immutable pin |
| Architecture-conformance checklist | Mirrors affected story wording and readiness date | Reconcile after epic/UX edits |
| Traceability YAML and generated Markdown | Must preserve stable row keys while updating owners/supporting mappings and 2026-08-02 containment | Update YAML and regenerate Markdown after approval |
| `sprint-status.yaml` | No epic/story renumbering; new 8.3 work-package ledger and external blockers require tracking | Reconcile only after proposal approval |

### Technical impact

This proposal changes planning authority only. It does not implement platform tooling, Durable
Tasks, Confirmation Artifacts, identity/KMS bindings, FrontComposer adapters, Chatbot presentation,
or production code. External repository work remains separately authorized and validated at an
immutable revision. No dependency version, event history, runtime route, or deployment is changed by
this proposal.

## 3. Recommended Approach

Use **Direct Adjustment** and retain the implementation freeze until the corrected artifacts and
external entry conditions pass an independent readiness rerun.

1. Repair current Epic 6 audit ownership without reordering the epic sequence.
2. Align UX and story presentation with the canonical action-admission matrix.
3. Make Story 8.3 independently reviewable through three accepted Web work packages while
   preserving stable story numbering.
4. Correct the Story 7.2, 7.14, 8.2, and 6.7 acceptance criteria.
5. Require the Chatbot owner to deliver the immutable 8.8-P3 companion artifact and evidence
   manifest; record only its pin in Projects.
6. Reframe Epic 8 around explicit operator and release-owner outcomes.
7. Synchronize conformance, evidence, and sprint tracking, then rerun implementation readiness.

### Options evaluated

| Option | Viability | Effort | Risk | Decision |
| --- | --- | --- | --- | --- |
| Direct Adjustment | Viable | Medium planning effort; external execution schedule uncommitted | Medium after correction | **Recommended** |
| Potential Rollback | Not viable | High | High | No production-authority implementation needs reversal; rollback would discard valid planning evidence without closing external gates |
| MVP Review / scope reduction | Not viable as a corrective path | High | High | The MVP is coherent and all approved FR/NFRs are release-binding; removing scope would not supply the missing platform or Chatbot evidence |

### Scope classification

**Moderate.** This is backlog and artifact reorganization rather than a fundamental product or
architecture replan. It requires Product Owner/Developer coordination plus Solution Architect,
Test Architect, FrontComposer/Web, and Chatbot-owner acceptance. External prerequisite timing stays
uncommitted until immutable revisions, commands, artifacts, rollback, and owner approvals exist.

## 4. Detailed Change Proposals

### 4.1 Preserve the Story 6.1 external gate and update containment

**Artifacts:** `epics.md`, conformance checklist, traceability YAML/Markdown

**OLD:** Active artifacts refer to the 2026-08-01 `NOT READY` assessment, and the accepted historical
P1 baseline can be misread as current package/runtime acceptance.

**NEW:** Record the 2026-08-02 assessment as current and preserve this executable critical path:

```text
6.1-P1R -> {6.1-P0, 6.1-P2} -> 6.1-P3 -> 6.1-P4
          -> independent readiness rerun -> Story 6.1
```

Historical 6.1-P1 remains a satisfied evidence input only. Story 6.1 remains `blocked-external`
until P4 records accepted immutable revisions, successful clean-checkout commands, expected
artifacts, executable rollback, and accountable-owner approvals; the Story 6.1 specification then
passes readiness and an independent assessment returns exactly `READY`.

**Rationale:** Planning must represent the blocker accurately and cannot manufacture acceptance for
external capabilities.

### 4.2 Remove the Epic 6 forward dependency on Story 8.1

#### Story 6.5 — FrontComposer read surface

**Section:** Story statement and first acceptance criterion

**OLD:**

```text
inspect Project inventory, detail, reference health, resolution traces, and audit timeline

When the Web read surface renders inventory/detail/health/trace/audit,
Then every view ...
```

**NEW:**

```text
inspect Project inventory, detail, reference health, and current resolution traces

When the Web read surface renders inventory/detail/health/trace,
Then every view is Tenant-scoped, authorization-filtered, metadata-only,
shows the AD-32 response/recovery fields, and remains read-only.

The audit tab may render an explicit "not yet available" capability state, but it
does not query or imply audit data before Story 8.1.
```

#### Story 6.6 — CLI read surface

**Section:** Story statement, command inventory, and parity criterion

**OLD:**

```text
list / describe / inspect / trace / validate / audit
...
lifecycle/reference states, reason codes, timestamps, warnings, and audit identifiers
```

**NEW:**

```text
list / describe / inspect / trace / validate
...
lifecycle/reference states, reason codes, timestamps, and warnings
```

The `audit` command is absent or returns a stable unsupported-capability result until Story 8.1.

#### Story 8.1 — audit adapter ownership

**Section:** Acceptance criteria

**OLD:** Story 8.1 supplies task/audit/reconciliation reads without explicitly taking the deferred
Web and CLI audit adapters from 6.5/6.6.

**NEW:** Add:

```text
Given accepted Story 8.1 task/audit/reconciliation read capability and the earlier
authenticated Web/CLI read adapters, When the operator audit surfaces are enabled,
Then the Web audit timeline and CLI audit command consume Story 8.1 truth,
preserve Tenant scope, metadata-only output, stable audit identifiers, and
cross-surface semantic parity, and introduce no backward dependency into Epic 6.
```

**Rationale:** Epic 6 becomes independently completable using prior work only; Story 8.1 owns the
first production audit capability and its adapters.

### 4.3 Publish one canonical action-admission classification in UX

**Artifact:** `ux-design-specification.md`

**Affected sections:** Design Implications, Journey 2, Journey Patterns, Flow Optimization,
Maintenance Action Panel, Button Hierarchy, Form Patterns, Confirmation Pattern, MCP Journey, and
any blanket mutation/confirmation wording.

**OLD:**

```text
Preview every mutation, then admit it only through a valid Confirmation Artifact ...

Confirmations are required for mutating actions ...

Make every state-changing action explicit, scoped, confirmed, and auditable.
```

**NEW:**

```text
Classify every action from the versioned Projects contract. Task-only actions are
explicit, scoped, idempotent, recoverable, and audited where required, but do not
add a second confirmation. Confirmation-required actions use server Preview,
single-use Confirmation Artifact, and Durable Task admission. Read-only actions
create neither Confirmation Artifacts nor Durable Tasks.
```

Add the binding matrix:

| Admission class | Stable actions | UX contract |
| --- | --- | --- |
| Confirmation + Durable Task | `project.archive`, `project.restore`, `conversation.move`, `project-folder.replace`, `context-reference.unlink`, `resolution.confirm`, `project-proposal.confirm` | Request server Preview, present explicit confirm/cancel, consume one bound artifact, then monitor task truth |
| Durable Task only | `project.create`, `project-setup.update`, `conversation.link`, `project-folder.set-initial`, `file-reference.link`, `memory.link` | Authorize, validate, and admit idempotently without a second confirmation; present task and recovery states |
| Durable Task control | `task.cancel`, `task.reconcile` | Authorize against task/current checkpoint; reconciliation remains Administrator-only |
| Synchronous read | list/open/resolve/context/refresh/validate/Conversation-start/audit/operator-read and `safe-diagnostic-export.create` | No Confirmation Artifact or Durable Task; Safe Diagnostic Export retains separate authorization and bounds |

The Maintenance Action Panel gains explicit modes `TaskOnly` and `ConfirmationRequired`; it renders
Preview/artifact controls only for the second mode. Inferred Conversation/File/Memory links and
inferred initial Folder selection use the confirmation-required policy applicable to the inferred
action, while explicitly actor-selected additive actions remain task-only.

**Rationale:** Restores parity with FR-6, FR-8, FR-9, FR-10, AD-5, and the Contracts-owned
classification without weakening consequential-action safeguards.

### 4.4 Use canonical Resolution Trace dimensions

**Artifact:** `ux-design-specification.md`

**Section:** Resolution Trace component and related journey/outcome wording

**OLD:**

```text
States: Resolved, no match, multiple candidates, excluded, failed closed.
```

**NEW:**

```text
resolutionResult: NoMatch | SingleCandidate | MultipleCandidates
responseState: Complete | Partial | Unavailable | Denied
component inclusion: Included | Excluded
freshness: Current | Stale | Rebuilding | Unavailable
reason: canonical safe reason code
```

Remove local `Resolved`, `Excluded`, and `FailedClosed` result synonyms. A `SingleCandidate` result
does not mean the UX selected or durably attached it. Exclusion and fail-closed behavior remain
component/response dimensions.

**Rationale:** Prevents presentation vocabulary from redefining AD-32 or implying selection.

### 4.5 Strengthen operator accessibility acceptance language

**Artifact:** `ux-design-specification.md`

**Section:** Responsive Design & Accessibility / Testing Strategy

**OLD:**

```text
Automated accessibility checks, keyboard-only navigation, screen-reader spot
checks, responsive viewports, high data volume, and status-not-color-only checks.
```

**NEW:**

```text
Operator release evidence combines automated checks with authenticated manual
keyboard and screen-reader execution at deterministic small, median, and maximum
data shapes. It explicitly verifies 200% zoom and 320 CSS-pixel reflow, focus
placement/restoration, live-region behavior, timing-independent completion, and
non-color state. Any unresolved critical or serious accessibility violation blocks
the affected capability and release; unavailable evidence is not verified, never passed.
```

**Rationale:** Makes the operator evidence contract exactly match NFR-9, AD-30, and AD-34.

### 4.6 Require and pin the external Chatbot companion UX artifact

**Artifacts:** `epics.md` 8.8-P3, traceability YAML/Markdown, UX release-input section

**OLD:**

```text
8.8-P3 initial state: open; external owner and approved immutable revision required;
absence blocks Story 8.8 and release.
```

**NEW:**

```text
8.8-P3 status: blocked-external.

Acceptance requires an owner-supplied manifest containing:
- Chatbot owner repository and immutable revision;
- companion contract version;
- approval date, approving authority, and accountable owner;
- authenticated commands and deterministic fixtures;
- expected artifact paths, hashes, results, and terminal disposition;
- containment/rollback treatment for contract drift;
- coverage of candidate/proposal presentation, no preselection, confirm/cancel,
  expiry/staleness/replay/tamper/mismatch, lost response, all task states,
  cancellation, recovery, authoritative completion re-query, response-state
  admission, and NFR-9 accessibility.

Projects records only the accepted immutable pin and evidence-row binding. It does
not create, approve, or change the Chatbot-owned implementation artifact.
```

**Rationale:** Converts an implicit missing input into a fail-closed, independently owned release
contract without expanding Projects authority.

### 4.7 Decompose Story 8.3 without renumbering the production plan

**Artifact:** `epics.md`, Epic 8 Web work-package ledger and Story 8.3

**OLD:** Story 8.3 combines the shared Preview/Confirmation/task presentation framework, archive,
restore, Conversation move, Folder replacement, unlink, renewal, lost-response recovery,
`NeedsAttention`, cancellation, all terminal states, denial, accessibility, Fluent governance, and
read-only refresh in one Story `L` completion boundary.

**NEW:** Add three independently accepted work packages:

| Package | Independently demonstrable outcome | Owners |
| --- | --- | --- |
| 8.3-P1 | Shared Web Preview, Confirmation Artifact, Durable Task, recovery, cancellation, authoritative re-query, focus, and live-region presentation foundation over canonical contracts | FrontComposer/Web Owner + Test Architect |
| 8.3-P2 | Archive and restore Web journeys, including Folder-before-activation restore and role/denial/renewal evidence | Projects Web Owner + Product Owner + Test Architect |
| 8.3-P3 | Conversation move, Folder replacement, and Conversation/File/Memory unlink Web journeys with owner-preserving recovery evidence | Projects Web Owner + Product Owner + Test Architect |

Revise Story 8.3 to:

```text
Story 8.3: Integrate conformant Project maintenance in the Web console

Entry gate: 8.3-P1, 8.3-P2, and 8.3-P3 accepted at immutable revisions.

The bounded story composes the accepted lifecycle and association journeys into
the authenticated FrontComposer console, proves canonical action classification,
role visibility, safe denial, and read-only RefreshContext separation, and performs
no server workflow implementation.

Estimate: M.
```

**Rationale:** Each behavior is independently reviewable and demonstrable while stable story IDs,
FR/NFR mappings, PRD addendum references, Architecture Spine references, and release-gate numbering
remain intact.

### 4.8 Repair individual story acceptance criteria

#### Story 7.2 — authoritative completion

**OLD:** No criterion prevents `Succeeded` before the authoritative read model reflects the stored
Project Setup.

**NEW:** Add:

```text
Given the setup event is committed but the authoritative read model has not yet
confirmed the new Project Setup, When the task is polled, Then it does not report
Succeeded; Succeeded is terminal only after stored Setup and the authorized read
model agree on the expected Project version and values.
```

#### Story 7.14 — Preview before confirmation

**OLD:**

```text
Given a valid confirmation, When Preview runs, Then it verifies Tenant, actor,
authority, current Project version, and exactly one authorized Folder ...
```

**NEW:**

```text
Given an authorized Archived Project, When RequestPreview runs, Then it verifies
Tenant, actor, authority, current Project version, and exactly one authorized
Folder; if the prior Folder is invalid or missing, the Preview requires an
authorized replacement or same-name Folder plan and issues a bound artifact.

Given the resulting unexpired, single-use artifact and unchanged bound evidence,
When the caller confirms, Then the restore Durable Task is admitted atomically.
```

#### Story 8.2 — defined concurrency and transport-loss semantics

**OLD:**

```text
Given a third concurrent export for the Tenant or a lost/duplicate request,
When attempted, Then it is throttled with structured retry guidance and does not
double-produce.
```

**NEW:**

```text
Given two exports are executing for a Tenant, When a third export is requested,
Then it is rejected by the two-lease gate with structured retry guidance and no
partial snapshot is produced.

Given an export response is lost or the caller repeats the request, When no
separately approved export-idempotency contract exists, Then the adapter does not
claim exactly-once delivery or return retained bytes; a new authorized request may
produce a new current snapshot, and every attempt/outcome is audited metadata-only.
```

#### Story 6.7 — one Given, one When

**OLD:**

```text
Given the shadow-read equivalence gate ..., When it passes ... and contracts are
aligned, When cutover runs, Then ...
```

**NEW:**

```text
Given the shadow-read equivalence gate has passed across all Epic 6 reads and the
ULID identity, OpenAPI, and generated-consumer contracts are mechanically aligned,
When cutover runs, Then read routing switches to supported models and remains
reversible with legacy retained until Epic 8 release acceptance.
```

### 4.9 Reframe epic titles and Epic 8 stakeholder value

**Artifact:** `epics.md`

**OLD titles:**

```text
Epic 6: Authorized Project Reads on the Supported Platform
Epic 7: Durable Project Decisions and Cross-Context Recovery
Epic 8: Safe Operations and Release Confidence
```

**NEW titles:**

```text
Epic 6: Chatbot and Operators Retrieve Authorized Project Truth
Epic 7: Users Complete Durable Project Decisions and Recover Them
Epic 8: Operators Run Projects Safely and Release Owners Decide from Evidence
```

Revise the Epic 8 introduction to name two explicit stakeholder outcomes without changing story
IDs:

```text
Stories 8.1–8.6 let operators inspect truth, export bounded diagnostics, perform
conformant maintenance, and observe real health. Stories 8.7–8.11 let downstream
consumers and Release Owners qualify reproducible packages, integrate independently
owned evidence, prove performance/resilience, and record the terminal release
decision. Evidence acquisition remains in named prerequisite packages; no story
completes merely by producing a test milestone or recording a blocker.
```

**Rationale:** Makes the user/stakeholder value visible at scan level while avoiding a disruptive
new epic, story renumbering, or evidence-key migration.

### 4.10 Synchronize dependent planning and tracking artifacts

After approval and after applying the epics/UX edits:

1. Update `epics-architecture-conformance-checklist-2026-07-16.md` for the 2026-08-02 containment,
   Story 6.5/6.6 audit removal, Story 8.1 adapter ownership, 8.3-P1/P2/P3, and repaired Story 7.2,
   7.14, 8.2, and 6.7 criteria.
2. Edit `implementation-readiness-traceability-matrix.yaml` as the authority, preserving all stable
   row keys; regenerate `implementation-readiness-traceability-matrix.md` mechanically.
3. Update `sprint-status.yaml` without adding, removing, or renumbering epics/stories; add the
   8.3-P1/P2/P3 action ledger and retain `blocked-external` for Story 6.1 and 8.8-P3.
4. Add this proposal and the 2026-08-02 readiness report to active artifact provenance where the
   current-artifact list is maintained.
5. Run document/reference checks and an independent implementation-readiness rerun. Do not change
   Story 6.1 to `ready-for-dev` unless the superseding result is exactly `READY` and every entry
   condition is accepted.

## 5. Change Analysis Checklist Record

### Section 1 — Trigger and context

- [x] **1.1** Trigger identified: Story 6.1 entry plus the 2026-08-02 readiness assessment.
- [x] **1.2** Core problem defined: external technical prerequisites, inconsistent downstream
  interpretation of approved requirements, and a missing separately owned release artifact.
- [x] **1.3** Evidence recorded: 24/24 coverage, 13 artifact-quality findings, open prerequisite
  chain, missing companion artifact, and exact dependency/UX contradictions.

### Section 2 — Epic impact

- [!] **2.1** Epic 6 cannot complete as written until the forward audit dependency is removed and
  Story 6.1's external gate is accepted.
- [!] **2.2** Epic-level changes required: repair Epic 6 ownership and reframe Epic 8's two
  stakeholder outcomes.
- [x] **2.3** Remaining epics reviewed: Epic 7 needs focused AC repair; Epic 8 needs work-package
  decomposition and story-quality repair.
- [N/A] **2.4** No epic is obsolete and no new epic is required.
- [x] **2.5** Epic order remains `6 -> 7 -> 8`; no priority inversion is introduced.

### Section 3 — Artifact impact

- [x] **3.1** PRD reviewed: no goal, requirement, or MVP-scope change required.
- [x] **3.2** Architecture reviewed: existing adopted decisions already resolve the conflicts; no
  semantic spine change required.
- [!] **3.3** UX requires the action-classification, resolution-vocabulary, and accessibility edits,
  plus the external Chatbot companion pin.
- [!] **3.4** Conformance, traceability, sprint tracking, and provenance require synchronization
  after approval.

### Section 4 — Path forward

- [x] **4.1** Direct Adjustment is viable — medium planning effort, medium residual risk.
- [N/A] **4.2** Rollback is not justified; no production-authority implementation must be undone.
- [N/A] **4.3** MVP reduction is not an acceptable substitute for the missing platform/UX evidence.
- [x] **4.4** Selected approach: Direct Adjustment followed by an independent readiness rerun.

### Section 5 — Proposal components

- [x] **5.1** Issue summary complete.
- [x] **5.2** Epic and artifact impacts documented.
- [x] **5.3** Recommended path and alternatives documented.
- [x] **5.4** MVP impact and sequenced action plan documented.
- [x] **5.5** Handoff responsibilities defined below.

### Section 6 — Final review and handoff

- [x] **6.1** All applicable checklist sections addressed; open actions are explicit.
- [x] **6.2** Proposal checked against PRD, Architecture Spine, epics, UX, conformance evidence, and
  the 2026-08-02 report.
- [x] **6.3** Jerome explicitly approved the proposal on 2026-08-02.
- [x] **6.4** `sprint-status.yaml` was reconciled without adding, removing, or renumbering stories;
  8.3-P1/P2/P3 were added to the action ledger and 8.8-P3 remains `blocked-external`.
- [x] **6.5** Moderate-scope handoff is active for the roles below.

## 6. Implementation Handoff

The approved planning correction is applied to `epics.md`, `ux-design-specification.md`, the
Solution-Architect conformance checklist, the canonical traceability YAML and its mechanically
regenerated Markdown view, and `sprint-status.yaml`. Internal consistency checks pass. The declared
`hexalith-evidence validate` command remains unavailable because no tool with command
`hexalith-evidence` exists in the current manifest; this is recorded as an external blocker, not a
passing result.

### Product Owner / Developer

- Preserve the applied `epics.md` and `ux-design-specification.md` corrections, historical records,
  and stable story IDs.
- Track 8.3-P1/P2/P3 without representing them as completed user-value stories.
- Keep Story 6.1 blocked until every entry condition and the independent `READY` result exist.

### Solution Architect

- Verify the corrected story set against AD-1 through AD-34 and G-1 through G-6.
- Confirm no implicit sibling/platform authority, event-history rewrite, unsafe dual writer, or
  false forward dependency was introduced.

### Test Architect

- Execute the independently specified evidence for 8.3-P1/P2/P3 and enforce the strengthened
  8.8-P3 manifest validation recorded in the canonical YAML.
- Reject missing environments, failed critical cases, unexplained skips, or mutable/unpinned
  evidence.

### FrontComposer/Web owner

- Own 8.3-P1 and operator accessibility evidence.
- Keep `RefreshContext` read-only and implement the canonical action-admission matrix without local
  inference.

### Chatbot Presentation Owner and Chatbot Test Owner

- Produce and approve the external companion UX/evidence artifact at an immutable revision.
- Supply the required manifest, commands, fixtures, artifacts, hashes, results, and disposition for
  8.8-P3. Projects records the pin only.

### External platform and capability owners

- Complete P1R, P0, P2, P3, and P4 under their repository authorities.
- Supply accepted clean-checkout evidence and executable rollback; local, stale, uncommitted, or
  unavailable candidates remain blockers.

## 7. Success Criteria and Next Decision

The approved planning correction has the following disposition:

1. [x] Stories 6.5/6.6 no longer depend on Story 8.1 and Story 8.1 owns the Web/CLI audit adapters.
2. [x] UX and story presentation use the canonical action-admission matrix and resolution dimensions.
3. [x] Operator accessibility evidence is explicitly specified against NFR-9/AD-34/AD-30.
4. [x] 8.3-P1/P2/P3 are independently scoped, owned, and testable; Story 8.3 is a bounded integration
   story.
5. [x] Stories 7.2, 7.14, 8.2, and 6.7 contain the corrected acceptance criteria.
6. [x] Epic 8 exposes explicit operator and release-owner outcomes without renumbering stories.
7. [ ] 8.8-P3 still lacks the approved immutable Chatbot-owned artifact and accepted complete
   evidence manifest; its specification and `blocked-external` state are now explicit.
8. [x] Conformance, YAML evidence authority, generated Markdown, and sprint status agree.
9. [ ] Story 6.1's external prerequisite chain is not yet accepted or executable from an accepted
   clean checkout.
10. [ ] A superseding independent implementation-readiness assessment cannot return `READY` until
    items 7 and 9 and all other applicable entry conditions pass.

Until all applicable criteria pass, production-authority implementation remains blocked. Story
8.11 and dated terminal acceptance from Jerome and John remain the release gate after implementation
evidence is complete.
