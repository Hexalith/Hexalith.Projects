---
title: "Sprint Change Proposal: Correct Implementation-Readiness Rerun-2 Blockers"
date: 2026-08-01
status: approved
approved: 2026-08-02
approved_by: Jerome
workflow: bmad-correct-course
review_mode: batch
edit_review: accepted
batch_reviewed: 2026-08-02
proposal_approval: approved
application_status: not-applied
change_scope: major
trigger: "Implementation-readiness rerun-2 returned NOT READY with 4 critical, 6 major, and 3 minor issues despite 24/24 functional requirements mapped."
source_report: "_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-01-rerun-2.md"
requirements_coverage: "24/24 functional requirements mapped (100%); no PRD scope change proposed"
recommended_path: direct-adjustment
approval_scope: planning-correction-and-implementation-handoff
production_implementation_authority: blocked-until-corrections-applied-and-independent-ready
---

# Sprint Change Proposal: Correct Implementation-Readiness Rerun-2 Blockers

## 1. Issue Summary

The 2026-08-01 rerun-2 assessment found complete functional-requirement
coverage but could not establish an executable, conformant implementation
baseline. The result is **NOT READY** because the assessed document inventory
selected a superseded May architecture instead of the normative July
Architecture Spine, the production starting story still depends on unresolved
external gates, architecture conformance remains unsigned, and the mandatory
Chatbot companion package has no accepted evidence.

The product intent remains stable. All 24 functional requirements map to
production-authority stories, and all 11 non-functional requirements remain
represented. This proposal therefore corrects planning authority, backlog
cohesion, and evidence admission. It does not revise the PRD, declare an
external capability complete, or authorize Story 6.1 implementation.

### Assessment findings and disposition

| Severity | Rerun-2 finding | Proposed disposition |
| --- | --- | --- |
| Critical | The assessment selected superseded `_bmad-output/planning-artifacts/architecture.md` and omitted the normative Architecture Spine. | Promote the normative architecture to the root discovery path, archive the May document as historical evidence, and retain one normative source. |
| Critical | Story 6.1 is not executable because `P0`, `P1R`, `P2`, `P3`, `P4`, `G1`–`G6`, clean-checkout verification, story-spec readiness, and the independent READY rerun are unresolved. | Preserve every blocker; add self-contained, versioned gate records and enforce the existing critical path. |
| Critical | Architecture conformance is unsigned. | Require a dated Solution Architect verdict against the exact canonical architecture and corrected Epics 6–8 baseline. |
| Critical | Mandatory Chatbot companion evidence for `8.8-P3` is absent. | Add a separately owned, immutable evidence-package contract; keep `8.8` and release blocked until it is accepted. |
| Major | UX mixes presentation labels with canonical response and resolution values and uses operational “replay” language for recomputation. | Add a binding presentation mapping and replace operational replay language with fresh recomputation. |
| Major | The authorization model is not expressed as a complete role × surface × action matrix. | Project AD-33 into UX and epic acceptance criteria without widening any role. |
| Major | Epic 8 mixes operator capability delivery with release assurance. | Retain stable IDs but divide Epic 8 into two named outcome tracks. |
| Major | Stories 6.3, 6.5, 8.3, 8.8, 8.9, and 8.10 are too broad for independent execution or verification. | Add bounded, independently evidenced work packages; make 8.8 an aggregation-only gate. |
| Major | Open prerequisite/evidence work packages are not self-contained. | Give every package versioned entry criteria, exact commands, retained artifacts, rollback, approvals, and immutable owner pins. |
| Minor | Historical Epics 1–5 remain interleaved with production authority. | Move their full text to a non-schedulable history artifact and leave a concise evidence pointer in the production plan. |
| Minor | Shared workflow invariants lack per-story applicability and evidence identities. | Add an applicability matrix keyed by story, AD, invariant, and evidence row. |
| Minor | Evidence commands and artifacts are partly templated. | Require exact selectors, immutable revisions, and artifact hashes before any package or story can be ready or passed. |

## 2. Impact Analysis

### Epic and story impact

| Scope | Impact |
| --- | --- |
| Epics 1–5 | Remain completed historical implementation evidence. Their content moves to a history artifact marked `authority: historical` and `schedulable: false`; no status or implementation claim changes. |
| Epic 6 | Remains the first production-authority epic. Story 6.1 remains blocked. Stories 6.3 and 6.5 gain bounded work-package decomposition without changing their FR ownership or stable story IDs. |
| Epic 7 | No requirement, story, order, or scope change. Its stories gain explicit shared-invariant applicability and self-contained external-package references where applicable. |
| Epic 8 | Retains Stories 8.1–8.11 and their stable evidence keys. Stories 8.1–8.5 become the named **Operator Capability** track; Stories 8.6–8.11 become the named **Production Assurance** track. Stories 8.3, 8.9, and 8.10 gain bounded work packages. Story 8.8 is clarified as an aggregation-only evidence gate over 8.8-P1/P2/P3. |
| Story 6.1 | No implementation start. The enforced path remains `P1R → {P0, P2} → P3 → P4 → clean-checkout verification → story-spec readiness → independent READY rerun → Story 6.1`. |
| Story 8.8 | Remains blocked until all three independent evidence packages pass, including the separately owned Chatbot companion package `8.8-P3`. |
| Story 8.11 | Remains the terminal release decision and cannot pass while any required package, critical case, approval, or environment evidence is absent. |

No epic, FR, NFR, or stable story is added, removed, renumbered, or reprioritized.

### Artifact conflict analysis

| Artifact | Conflict | Required correction |
| --- | --- | --- |
| PRD and addendum | None. The assessment confirms complete FR coverage. | No content change. Update input inventories only if needed. |
| Root architecture | It is explicitly superseded but is preferred by whole-document discovery. | Preserve it under architecture history and promote the normative architecture to the root path. |
| Architecture Spine | Normative but omitted by rerun-2 discovery. | Make its decisions the sole root architecture authority; retain the old path only as a compatibility pointer. |
| Architecture conformance checklist | Detailed checks exist, but every verdict and the final signature are blank. | Bind it to an immutable canonical revision and require completed checks, findings, verdict, architect, and date. |
| UX specification | Older vocabulary conflicts with the later AD-32 response contract, and generic administrator/operator wording can widen authority. | Add binding state mapping, fresh-recomputation wording, and the AD-33 role matrix across Web, CLI, MCP, and Chatbot. |
| Epics | Historical and production material are interleaved; Epic 8 has mixed outcomes; large stories and work packages need stronger boundaries. | Separate history, name the two Epic 8 tracks, add bounded work packages, and add applicability/evidence mappings. |
| Traceability matrix | Requirement coverage is complete, but gates and evidence remain unavailable, failed, or unaccepted. | Preserve failing/open states and add exact artifact/revision/package links as owners produce them. |
| Sprint status | Correctly blocks production, but package records need locally discoverable specifications. | Add versioned package-spec links and retain current story statuses until each acceptance contract passes. |
| Chatbot companion | The Projects UX contract requires it, but no accepted owner artifact exists. | Add and independently accept `8.8-P3`; Projects may reference but must not manufacture owner evidence. |

### Technical and delivery impact

- **Change classification:** Major. The correction changes canonical planning
  authority, restructures the production backlog presentation, and requires
  cross-repository evidence and independent sign-off.
- **Implementation effort:** No application estimate is committed until owners
  refine the bounded work packages. Planning corrections are bounded; external
  gate completion remains schedule-determining.
- **Risk:** High until the correct architecture is assessed, P1R and the other
  Story 6.1 gates are accepted, and the Chatbot package exists. The correction
  itself reduces ambiguity without changing product scope.
- **Delivery state:** Production-authority implementation remains blocked.

## 3. Path Evaluation and Recommendation

### Option 1 — Direct adjustment within the existing plan

**Selected.** Preserve the PRD, Epics 6–8, stable story IDs, evidence keys, and
external-gate sequence. Correct document authority, clarify UX and
authorization semantics, decompose broad execution into work packages, and
obtain the missing evidence and sign-offs.

- Effort: Medium for planning correction; external execution uncommitted.
- Risk: Lowest because it preserves approved scope and identifiers.
- Schedule effect: Readiness remains gated by external capability and evidence
  completion, not by this document edit alone.

### Option 2 — Roll back to the prior architecture or platform baseline

**Rejected as the primary path.** The May architecture is superseded and cannot
govern current Epics 6–8. The EventStore 3.70.1 tuple remains an operational
rollback candidate under the approved P1R proposal, but rollback does not
provide P0, P2, P3, P4, Chatbot evidence, conformance approval, or a READY
assessment.

### Option 3 — Reduce MVP scope

**Rejected.** Rerun-2 found no missing FR mapping or infeasible requirement.
Removing scope would not repair the invalid assessment input, unsigned
conformance, or absent proof for retained functionality.

### Recommended course

Apply the nine changes below in the stated sequence. Do not mark a gate,
evidence row, story, implementation-readiness result, or release decision as
passed merely because this proposal is approved or its planning edits are
applied.

## 4. Detailed Change Proposals

### Change 1 — Establish one discoverable canonical architecture

**Artifacts:**

- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/architecture/history/architecture-2026-05-24.md`
- `_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md`
- all current input-document inventories and architecture links

**Old:** The root `architecture.md` contains the superseded May design and
points to a normative document nested below `architecture/`. Whole-document
discovery therefore selects historical evidence and can omit the authority.

**New:**

1. Preserve the complete May document at
   `architecture/history/architecture-2026-05-24.md` with
   `status: superseded`, `historicalEvidence: true`, and
   `schedulable: false`.
2. Promote the complete current Architecture Spine to root
   `architecture.md`. Its frontmatter declares `status: final`,
   `normative: true`, the bound FR/NFR/Epic range, and one canonical document
   identity.
3. Replace the former nested spine with a compatibility pointer that contains
   no competing decisions and directs readers and tools to root
   `architecture.md`.
4. Update current PRD/UX/epic/checklist/matrix input inventories to name the
   root architecture. Historical inventories may retain their original paths.
5. Record the exact canonical file hash or repository revision in the
   conformance and readiness records after promotion.

This is an authority and discovery correction, not a rewrite of AD-1 through
AD-34. The EventStore binding may change only under the already approved P1R
acceptance sequence; no unaccepted 3.88.0 tuple is promoted by this change.

### Change 2 — Execute and sign architecture conformance

**Artifact:**
`_bmad-output/planning-artifacts/epics-architecture-conformance-checklist-2026-07-16.md`

**Old:** Section A, gate checks, story checks, AD coverage, the final verdict,
architect name, and date are blank even though the artifact status implies the
correction was applied.

**New:**

- Set the pre-review state to `pending-solution-architect-review`.
- Pin the canonical architecture identity and exact revision/hash, corrected
  `epics.md` revision/hash, UX revision/hash, and assessment input inventory.
- Require a recorded result for every Section A, B, story, and AD-coverage
  check. `conforms-with-note` entries identify an owner and resolution link;
  `non-conformant (blocking)` entries keep readiness blocked.
- Require a single final verdict, the Solution Architect's accountable name,
  and an ISO date. A proxy, unsigned checkbox population, or document author
  self-attestation is not acceptance.
- Link the signed verdict from `epics.md`, both traceability views, and the next
  readiness report.

Conformance may be signed only after Changes 1, 5, 6, and 7 are applied to the
exact assessed revisions.

### Change 3 — Make the Story 6.1 gate chain self-contained

**New package-spec directory:**
`_bmad-output/planning-artifacts/work-packages/story-6.1/`

Create one versioned local admission record for `6.1-P0`, `6.1-P1R`,
`6.1-P2`, `6.1-P3`, and `6.1-P4`. Each record contains:

- stable package ID, accountable owner, owner repository, immutable revision,
  and contract/version pins;
- exact entry criteria and dependencies;
- exact clean-checkout commands with project/test selectors and expected exit
  behavior;
- retained evidence paths, hashes, environment identity, and timestamps;
- executable rollback procedure and rollback evidence where applicable;
- required named approvals and dated acceptance; and
- an explicit `open`, `accepted`, or `rejected` decision with no inferred pass.

`6.1-P1R` links, without superseding, the approved
`sprint-change-proposal-2026-08-01-p1r-baseline-revalidation.md` and its finite
owner-repository qualification record. Historical P1 remains complete; P1R
remains open until the exact 3.88.0 or rollback tuple is accepted under that
proposal.

The sprint ledger and Epic 6 prerequisite table link these records. Story 6.1
remains `blocked` until all five packages are accepted, the exact clean-checkout
sequence passes, the dedicated Story 6.1 specification passes readiness, and
an independent implementation-readiness rerun returns exactly `READY`.

### Change 4 — Admit the separately owned Chatbot companion package

**New local admission record:**
`_bmad-output/planning-artifacts/work-packages/story-8.8/8.8-P3-chatbot-companion.md`

The record must identify and pin:

- the Chatbot owner repository and immutable revision;
- the approved companion UX specification and contract version;
- FR-14, FR-15, FR-20, NFR-9, AD-32, AD-34, and SM-5 coverage;
- authenticated automated and manual WCAG 2.2 AA evidence at small, median,
  and maximum data shapes;
- semantic response/recovery parity and safe denial/privacy evidence;
- exact commands, environments, retained artifacts, and hashes; and
- dated Chatbot Owner and Test Architect approvals.

The Projects repository records admission only. It cannot generate, approve,
or substitute for Chatbot-owned UX or test evidence. Missing, mutable,
placeholder, failed-critical, or ownerless evidence keeps `8.8-P3`, Story 8.8,
Story 8.11, and release blocked.

### Change 5 — Normalize UX vocabulary and project AD-33 authority

**Artifact:** `_bmad-output/planning-artifacts/ux-design-specification.md`

#### Resolution and response mapping

Replace the older mixed Resolution Trace states with this binding:

| Presentation text | Canonical field/value | Rule |
| --- | --- | --- |
| No match | `resolutionResult: NoMatch` | Valid only when `responseState` permits safe resolution disclosure. |
| One candidate | `resolutionResult: SingleCandidate` | Does not itself confirm or create an association. |
| Multiple candidates / ambiguous | `resolutionResult: MultipleCandidates` | “Ambiguous” is display copy only; user confirmation remains a separate authorized action. |
| Included / excluded | `components[].inclusion: Included\|Excluded` | Never serialize as a resolution result. Include a safe reason for every exclusion. |
| Failed closed | `responseState: Denied\|Unavailable` | Choose the canonical state at the boundary; “failed closed” is explanatory prose, not a machine state. |
| Resolved | Remove as a machine state | If retained as display prose, derive it explicitly from a canonical snapshot and never serialize or use it for control flow. |

Replace operational phrases such as “resolution replay” and “replay dry-run”
with **fresh recomputation** or the canonical action `RefreshContext`.
Recomputation returns a new snapshot and persists no Resolution Trace.
“Replay” remains valid only for replayed/tampered single-use artifact rejection
and deterministic event/recovery test fixtures.

#### Role × surface × action matrix

Project this binding into UX navigation, action visibility, confirmation flows,
CLI help, MCP tool admission, and Chatbot behavior:

| Surface | Action | Project User | Tenant Operator | Tenant Project Administrator |
| --- | --- | --- | --- | --- |
| Chatbot/Web/CLI/MCP, where exposed | Confirm ambiguous resolution or proposed creation | Allowed | Denied | Denied |
| Chatbot/Web/CLI/MCP, where exposed | Archive or restore | Allowed when action-authorized | Allowed when action-authorized | Allowed when action-authorized |
| Chatbot/Web/CLI/MCP, where exposed | Add Conversation/File/Memory link or initially set Folder | Allowed | Denied | Denied |
| Chatbot/Web/CLI/MCP, where exposed | Move/unlink or replace Folder | Allowed | Denied | Allowed |
| Chatbot/Web/CLI/MCP, where exposed | Inspect pre-activation safe task status | Own permitted tasks through Chatbot | Allowed | Allowed |
| Chatbot/Web/CLI/MCP, where exposed | Reconcile `NeedsAttention` | Denied | Denied | Allowed |
| Web/CLI/MCP | Safe Diagnostic Export | Denied | Separately authorized | Separately authorized |

An unavailable surface does not create authority. Hiding or disabling an action
is presentation behavior only; every server boundary enforces the same matrix.
Action-specific policy and owner reauthorization may narrow authority, never
widen it. Delegated services inherit the original actor and cannot self-confirm
an end-user decision.

### Change 6 — Separate history, clarify Epic 8 outcomes, and bound broad stories

**Artifacts:**

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/history/epics-1-5-implementation-history.md`
- new work-package records linked from the affected stories

#### Historical authority

Move the complete Epic 1–5 narrative to the history artifact with explicit
frontmatter:

```yaml
authority: historical
schedulable: false
productionAuthority: none
```

Keep a concise historical-evidence summary and link in `epics.md`; leave Epics
6–8 as the only schedulable production plan. Preserve all historical IDs and
evidence references.

#### Epic 8 outcome tracks

Retain the Epic 8 and story IDs while adding these named tracks:

- **Operator Capability — Stories 8.1–8.5:** audit, export, Web, CLI, and MCP
  value available to authorized operators and administrators.
- **Production Assurance — Stories 8.6–8.11:** health, package discipline,
  cross-surface/privacy/accessibility evidence, performance, resilience, and
  terminal release acceptance.

The second track verifies and admits the first; it must not quietly add new
operator product behavior.

#### Bounded execution packages

| Story | Required packages | Story completion boundary |
| --- | --- | --- |
| 6.3 | `6.3-W1` assembled context read; `6.3-W2` read-only refresh; `6.3-W3` safe explanation | All three exact contracts and integrated snapshot semantics pass. |
| 6.5 | `6.5-W1` authenticated list/detail; `6.5-W2` health/trace/audit views; `6.5-W3` Web authorization and accessibility evidence | One authenticated FrontComposer read surface satisfies all three packages. |
| 8.3 | `8.3-W1` authenticated operational reads; `8.3-W2` canonical mutations and recovery; `8.3-W3` Web-specific parity/accessibility evidence | Web conforms to the canonical action and response contracts without claiming three-surface completion. |
| 8.8 | Existing `8.8-P1` authenticated isolation/privacy/parity; `8.8-P2` operator accessibility; `8.8-P3` Chatbot companion | Aggregation-only: validate immutable manifests and reject missing or failed evidence; perform no new cross-repository implementation. |
| 8.9 | `8.9-W1` small/median/max latency; `8.9-W2` paging/cardinality/bounds; `8.9-W3` back-pressure and pre-admission rejection | All NFR-5/6/7 thresholds pass with retained load-shape evidence. |
| 8.10 | `8.10-W1` restart/two-instance recovery; `8.10-W2` duplicate/lost-response/concurrency/fencing; `8.10-W3` workflow-family terminal reconciliation | Every applicable workflow family reaches its required terminal disposition within the stated RTO/RPO. |

These are independently estimable and verifiable work packages, not new user
stories. Each uses the self-contained package contract defined in Change 3.
The parent story cannot pass on partial package completion.

### Change 7 — Bind shared invariants and exact evidence to every story

**Artifacts:** `epics.md`, the canonical YAML/Markdown traceability matrix,
story specifications, and work-package records

Add a per-story applicability table with these columns:

```text
story-or-package | invariant/AD | applies/not-applicable | acceptance criterion
evidence-row | exact command | retained artifact | accountable owner
```

At minimum, explicitly evaluate AD-4, AD-5, AD-13, AD-19, AD-20, AD-26,
AD-30, AD-32, AD-33, and AD-34 for every production story. A
`not-applicable` result requires a reason; a broad epic-level citation is not
sufficient evidence.

Before any story or package becomes ready or passed:

- replace `Story=<id>`, globs, placeholder revisions, and placeholder artifact
  names with exact executable commands and selectors;
- record clean-checkout prerequisites and environment identity;
- retain artifact hashes and immutable owner revisions; and
- keep the canonical YAML and Markdown view synchronized by stable evidence
  row identity.

Commands and hashes that do not yet exist remain explicitly open; they are not
invented to make the planning document appear complete.

### Change 8 — Synchronize planning and implementation state without false progress

**Artifacts:** `epics.md`, `sprint-status.yaml`, both traceability views, the
conformance checklist, and current input inventories

After proposal approval, apply the planning corrections atomically and record
this state:

```yaml
implementationReadiness: NOT READY
productionImplementationAuthority: blocked
story6_1: blocked
architectureConformance: pending-or-blocking-until-signed
chatbotCompanion8_8_P3: open
```

No existing open/failed/unavailable row becomes passed as a consequence of
document restructuring. Status changes require their own immutable evidence
and named acceptance. Keep the approved P1R proposal and previously applied
implementation-readiness correction in every relevant input history.

### Change 9 — Run an independent readiness assessment over the corrected inventory

The next assessment must explicitly list and load:

- the canonical PRD and addendum;
- root `_bmad-output/planning-artifacts/architecture.md` as the sole normative
  architecture;
- corrected production-authority `epics.md`;
- corrected UX specification;
- the signed architecture-conformance verdict;
- canonical YAML plus Markdown traceability views;
- self-contained Story 6.1 and 8.8 package records; and
- sprint status and the dedicated Story 6.1 specification.

It must identify the May architecture and Epic 1–5 narrative as historical,
non-schedulable evidence rather than current planning authority. The assessor
must verify the exact immutable revisions and run the documented clean-checkout
commands. Only a result of exactly `READY` can remove the implementation
readiness containment state; proposal approval, artifact application, or a
conditional result cannot.

## 5. Implementation Handoff

### Owners and responsibilities

| Owner | Responsibility |
| --- | --- |
| Product Owner / PM | Accept the correction, preserve FR/NFR scope, restructure epic presentation, and maintain the critical-path containment state. |
| Solution Architect | Establish the canonical root architecture, review the corrected exact revisions, resolve findings, and sign the conformance verdict. |
| UX Owner | Apply the AD-32 presentation mapping, fresh-recomputation language, and AD-33 role × surface × action projection. |
| Builds and EventStore Owners | Complete P1R and P0 owner artifacts under their approved contracts with exact revisions, commands, rollback, and approvals. |
| Folders, Conversations, Memories, and other capability owners | Complete the applicable P2/P3 external contracts and retained evidence without transferring authority to Projects. |
| Chatbot Owner | Produce and approve the separately owned 8.8-P3 companion UX and evidence package. |
| Test Architect | Independently validate package manifests, exact commands, critical evidence, accessibility/privacy/isolation evidence, and traceability synchronization. |
| Projects delivery owner | Create the Story 6.1 specification only after entry gates are accepted; do not start implementation before a superseding READY result. |
| Independent readiness assessor | Load the corrected inventory, verify immutable inputs and executable evidence, and issue the superseding decision. |

### Sequencing

1. Approve this proposal without changing any readiness or story status.
2. Apply Changes 1, 5, 6, 7, and the planning-only portion of Change 8.
3. Complete the Solution Architect review in Change 2 against those exact
   revisions.
4. Create the local admission records, then let accountable owners complete
   Story 6.1 and Chatbot evidence under Changes 3 and 4.
5. Materialize exact Story 6.1 commands and artifacts, pass its story-spec
   readiness review, and synchronize evidence/status without false progress.
6. Execute Change 9 independently.
7. Start Story 6.1 only if the superseding result is exactly `READY`.

### Success criteria

- Readiness discovery cannot select a superseded architecture as current
  authority.
- The root canonical architecture, corrected epics, UX, traceability, and
  conformance verdict name the same immutable baseline.
- Architecture conformance has a dated accountable signature and no unresolved
  blocking verdict.
- P0, P1R, P2, P3, and P4 have self-contained accepted records; all G1–G6
  evidence required by the critical path is available and accepted.
- The exact clean-checkout sequence and Story 6.1 specification readiness pass.
- `8.8-P3` contains immutable, owner-approved Chatbot companion evidence and
  passes independent Test Architect review.
- UX uses only canonical machine vocabularies and enforces AD-33 without role
  widening on every surface.
- Historical Epics 1–5 are clearly non-schedulable; current large stories have
  bounded work packages with exact evidence.
- The canonical YAML and Markdown evidence views agree on every stable row.
- An independent implementation-readiness rerun returns exactly `READY` before
  production-authority implementation begins.

## 6. Correct Course Checklist Disposition

| Checklist section | Result |
| --- | --- |
| 1. Trigger and context | Complete — rerun-2 report loaded; NOT READY cause and evidence identified. |
| 2. Epic impact | Complete — Epics 6–8 remain viable; history separation, Epic 8 tracks, and bounded work packages proposed. |
| 3. Artifact conflicts | Complete — PRD stable; architecture, UX, epics, evidence, conformance, and sprint-state conflicts mapped. |
| 4. Path evaluation | Complete — direct adjustment selected; rollback retained only as P1R fallback; MVP reduction rejected. |
| 5. Proposal components | Complete — issue, impacts, nine edits, owners, sequence, and success criteria included. |
| 6.1 Proposal review | Complete — Jerome selected Continue on 2026-08-02. |
| 6.2 Validation | Complete for proposal drafting — scope remains achievable and no false gate closure is proposed. |
| 6.3 Explicit approval | Complete — approved by Jerome on 2026-08-02. |
| 6.4 Sprint-plan synchronization | Required during application; no story ID changes, and status changes still require their own accepted evidence. |
| 6.5 Handoff | Complete — approved Major correction routed to the named Product, Architecture, UX, owner-repository, Test Architecture, Projects delivery, and independent-assessment roles. |

## 7. Review and Approval Record

- **Batch edit review:** Accepted by Jerome on 2026-08-02
- **Proposal approval:** Approved
- **Approved by:** Jerome
- **Approval date:** 2026-08-02
- **Application status:** Not applied
- **Approval scope:** Planning corrections and their implementation handoff
- **Production implementation authority:** Blocked until the approved planning
  corrections are applied, every required gate/evidence package is accepted,
  Story 6.1 specification readiness passes, and an independent rerun returns
  exactly `READY`
- **Next action:** Apply the approved corrections in the sequence and ownership
  boundaries defined in Section 5, without converting any open or unavailable
  gate into a pass.
