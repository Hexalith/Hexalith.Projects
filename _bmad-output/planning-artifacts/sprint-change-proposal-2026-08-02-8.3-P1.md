---
title: "Sprint Change Proposal: Make 8.3-P1 Independently Executable"
date: 2026-08-02
status: proposed
workflow: bmad-correct-course
review_mode: batch
target: 8.3-P1
change_scope: moderate
trigger: "The approved 2026-08-02 correction created 8.3-P1, but its ledger entry does not define a finite cross-repository acceptance contract."
source_proposal: sprint-change-proposal-2026-08-02.md
source_report: implementation-readiness-report-2026-08-02.md
recommended_approach: direct-adjustment
approval_status: pending
application_status: not-applied
routed_to: "Product Owner and Developer, with FrontComposer/Web Owner, Projects Web Owner, Test Architect, and Solution Architect"
---

# Sprint Change Proposal: Make 8.3-P1 Independently Executable

## 1. Issue Summary

The approved 2026-08-02 course correction correctly decomposed Story 8.3 into three independently
accepted Web work packages. It did not, however, give the first package a finite acceptance
contract. `8.3-P1` currently names a broad outcome, owners, blockers, and a general evidence list,
but it does not define:

- the exact responsibility split between FrontComposer and the Projects Web adapter;
- the canonical inputs the shared presentation foundation must consume;
- the state, recovery, cancellation, and accessibility cases that constitute acceptance;
- deterministic fixtures, clean-checkout verification commands, or expected artifact paths;
- the immutable evidence manifest and owner dispositions required to close the package; or
- a rollback and compatibility boundary.

This is an **underspecified decomposition of the approved requirements**, not a new product
requirement or a strategic pivot. Until the package boundary is explicit, the FrontComposer/Web
Owner and Test Architect cannot independently demonstrate acceptance, `8.3-P2` and `8.3-P3` cannot
consume a stable foundation, and bounded Story 8.3 integration remains non-executable.

### Trigger and evidence

- `epics.md` lists `8.3-P1` only as one row under “Story 8.3 prerequisite Web work packages.”
- `sprint-status.yaml` repeats the broad outcome and a non-path-specific evidence description, but
  supplies no package fixture, commands, artifact paths, compatibility fingerprint, or rollback.
- `8.3-P2` and `8.3-P3` both depend directly on acceptance of `8.3-P1`.
- Traceability row `finding-ux-001` blocks Story 8.3 on G-3, G-6, and all three Web packages, while
  exposing only the final Story 8.3 fixture and verification command.
- The UX specification already defines the required canonical model: four admission classes,
  bound Confirmation Artifacts, exactly eight Durable Task states, recovery and cancellation,
  authoritative re-query, and keyboard/focus/live-region behavior.
- The checked-out Projects UI contains a Projects-specific maintenance panel with local panel and
  lifecycle terms. The checked-out FrontComposer shared lifecycle wrapper exposes `Idle`,
  `Submitting`, `Acknowledged`, `Syncing`, `Confirmed`, and `Rejected`. Neither prototype is the
  canonical eight-state Durable Task and Confirmation Artifact foundation specified for 8.3-P1.
- G-3 still requires an accepted FrontComposer version/runtime-adapter disposition, and G-6 still
  requires accepted toolchain governance. The current implementation-readiness result remains
  `NOT READY`; this proposal must not bypass those gates.

### Problem statement

> `8.3-P1` is a dependency-shaped placeholder rather than an independently executable work package:
> its approved behavior exists across PRD, Architecture, epics, and UX, but its repository boundary,
> acceptance matrix, commands, evidence manifest, compatibility treatment, and completion boundary
> are not assembled in one authoritative contract.

## 2. Impact Analysis

### Epic impact

Epic 8 remains viable and correctly ordered. No epic or production story needs to be added, removed,
renumbered, or reprioritized. The necessary correction is to expand the existing non-story package
`8.3-P1` and preserve Story 8.3 as bounded integration of accepted `P1`, `P2`, and `P3` inputs.

The direct dependency impact is:

```text
G-3 + G-6 + superseding readiness READY
                    |
                    v
                 8.3-P1
                 /     \
                v       v
             8.3-P2   8.3-P3
                 \     /
                    v
                 Story 8.3
                    |
                    v
       8.8-P2 / Story 8.8 / Story 8.11
```

`8.8-P2` retains ownership of full authenticated operator WCAG 2.2 AA evidence at small, median,
and maximum shapes. `8.3-P1` proves the reusable component-level accessibility behavior needed by
the later journeys; it does not claim the terminal NFR-9 release result.

Epics 1–7 are unaffected. Their canonical action, Confirmation Artifact, Durable Task, authority,
and recovery contracts remain input authority for this package.

### Artifact conflicts and dispositions

| Artifact | Impact | Proposed disposition after approval |
| --- | --- | --- |
| PRD and addendum | Requirements already define the necessary behavior | No semantic change |
| Architecture Spine | AD-2, AD-19, AD-29, AD-32–AD-34 and G-3/G-6 already define the boundary and gates | No semantic change |
| `epics.md` | `8.3-P1` lacks a finite acceptance contract | Add a dedicated package contract without creating a story |
| UX specification | Journey 3 and Maintenance Action Panel already define the intended interaction | No semantic change; cite as normative package input |
| Architecture conformance checklist | One checkbox compresses all P1 obligations | Expand into independently checkable P1 clauses |
| Traceability YAML/Markdown | Story-level row does not identify the P1 evidence manifest | Preserve `finding-ux-001`; add the package manifest as a prerequisite note and regenerate Markdown |
| `sprint-status.yaml` | Action row lacks exact evidence and completion boundary | Reconcile the existing row; keep status `open` and target date `uncommitted` |
| Source code and tests | Existing surfaces are informative prototypes, not accepted P1 evidence | No source edit in this planning correction; implementation follows accepted package contract |

### Technical and ownership impact

- **FrontComposer repository:** owns reusable Fluent UI V5 presentation primitives and state
  composition over canonical/generated descriptors. It must not contain Projects action names,
  Projects domain rules, Projects authorization decisions, or Projects workflow implementation.
- **Hexalith.Projects repository:** owns `Projects.UI.Contracts` descriptors, canonical action
  classification, generated-client adapter mapping, Projects fixtures, and consumption proof. It
  must not fork FrontComposer presentation logic or reproduce the Durable Task engine.
- **Platform/Identity:** remains the authority for runtime credentials and authenticated actor/Tenant
  context. Neither Web repository may synthesize or expand authority.
- **Test Architect:** owns the deterministic cross-repository fixture, evidence manifest schema,
  hashes/results, negative controls, and terminal acceptance disposition.
- **Solution Architect:** accepts compatibility and rollback treatment and verifies the AD/G
  boundary. The Product Owner accepts that P1 is sufficient for P2/P3 consumption.

No event schema, persisted history, API admission rule, dependency version, runtime route, or
deployment is changed by this proposal.

## 3. Recommended Approach

Use **Direct Adjustment**. Convert the existing `8.3-P1` ledger entry into a reviewable package
contract, synchronize its evidence references, and leave all current gates and statuses honest.

### Options evaluated

| Option | Viability | Effort | Risk | Decision |
| --- | --- | --- | --- | --- |
| Direct Adjustment | Viable | Medium planning effort; large implementation package | Medium after correction | **Recommended** |
| Potential Rollback | Not viable | High | High | The approved 2026-08-02 decomposition is sound; deleting it would recreate the oversized Story 8.3 problem |
| PRD/MVP review | Not viable as a corrective path | High | High | MVP requirements and architecture already agree; reducing scope would not create an executable shared foundation |

### Scope classification

**Moderate.** The correction changes backlog and evidence authority across several planning
artifacts, but it neither changes product scope nor introduces a new production story. The
implementation estimate for `8.3-P1` remains **L** because it spans two repository authorities,
canonical state/admission coverage, accessibility primitives, deterministic fixtures, and an
immutable evidence handoff. Its delivery date remains uncommitted until G-3, G-6, and the
implementation-readiness freeze are cleared.

## 4. Detailed Change Proposals

These edits are proposals only. They are not applied to authoritative downstream artifacts until
explicit approval.

### 4.1 Expand `8.3-P1` in `epics.md`

**Artifact:** `_bmad-output/planning-artifacts/epics.md`

**OLD:**

> `8.3-P1` — Shared Web Preview, Confirmation Artifact, Durable Task, recovery, cancellation,
> authoritative re-query, focus, and live-region presentation foundation over canonical contracts —
> FrontComposer/Web Owner + Test Architect — open; blocked by G-3, G-6, and the current
> implementation-readiness freeze.

**NEW:** Preserve the table row and add the following package contract immediately below the table.

#### Proposed `8.3-P1` package contract

**Purpose.** Accept a reusable Web presentation foundation that consumes the versioned Projects
action/descriptors and canonical Preview, Confirmation Artifact, Durable Task, recovery, and safe
failure contracts. It is a prerequisite delivery package, not a production story and not user value
on its own.

**Entry gate.** Work may be planned while blocked, but acceptance and downstream consumption require:

1. a superseding independent implementation-readiness result of exactly `READY`;
2. G-3 acceptance at immutable revisions, including one supported FrontComposer dependency mode,
   descriptor discovery/generation, real credential propagation, and authenticated Web adapter
   proof; and
3. G-6 acceptance at immutable revisions, including the approved Fluent UI V5 RC and Fluxor
   governance used by this package.

No local checkout, floating version, skipped environment, or partially satisfied gate is equivalent
to acceptance.

**Repository authority.** FrontComposer owns generic Fluent UI V5 primitives, presentation-state
composition, focus management, restrained live-region behavior, and test helpers. Projects owns
versioned UI descriptors, stable action classification, generated-contract mapping, Projects
fixtures, and adapter consumption. Server Preview validation, Confirmation Artifact issuance and
consumption, task admission/execution, authorization, idempotency, checkpoint decisions, and
read-model truth remain server/platform responsibilities and are never reimplemented in the Web
client.

**Canonical inputs.** The package consumes, without local synonyms or inferred authority:

- the four-class canonical operator action matrix;
- the approved Preview and opaque Confirmation Artifact contracts, including server-supplied
  expiry and safe recovery actions;
- exactly `Pending`, `Running`, `WaitingForDependency`, `NeedsAttention`, `Succeeded`, `Rejected`,
  `Failed`, and `Cancelled` for task truth;
- canonical reason/recovery codes, including `RenewPreview`, `PollTask`, authorized reconciliation,
  and safe conflict behavior; and
- authenticated actor/Tenant context and generated contracts from the accepted G-3 runtime path.

**Deterministic fixture.** `epic8/8.3-p1-web-foundation` contains versioned cases for:

1. every canonical admission class, including read-only `RefreshContext` and an identical retained
   `reevaluate` compatibility alias if that alias remains;
2. valid confirmation plus expired, stale, replayed, tampered, actor-mismatched, Tenant-mismatched,
   and target-mismatched artifacts;
3. all eight task states, allowed transitions, immutable terminal states, bounded dependency
   guidance, and Administrator-only reconciliation visibility;
4. lost admission response, idempotent retry/`PollTask`, duplicate notifications, stale SignalR
   nudges, and authoritative HTTP or equivalent structured re-query;
5. cancellation before and after the irreversible checkpoint;
6. authorized, hidden, denied, and stale-authority controls with safe reason codes and no payload
   echo; and
7. keyboard operation, deterministic focus transitions/restoration, restrained announcements,
   non-color status, 200% zoom, and 320 CSS-pixel component presentation.

Fixture rows name their contract version and expected state, controls, focus target, announcement,
authoritative query, and safe result. P2/P3 provide real archive/restore/association journey data;
P1 does not fabricate those domain outcomes.

**Acceptance Criteria.** In addition to the shared Epic 8 evidence rules:

1. **Admission-class fidelity.** Confirmation-required actions render server Preview and explicit
   confirm/cancel, consume one bound artifact, and then monitor task truth. Task-only actions admit
   without a second confirmation. Task controls appear only when authorized against current task
   truth. Synchronous reads create neither artifact nor task.
2. **Confirmation safety.** The presentation displays server-provided scope, versions, current and
   proposed state, warnings, expected metadata-only audit evidence, and expiry. Every invalid or
   mismatched artifact admits no task, exposes only a safe summary and canonical recovery, and moves
   focus predictably to the summary/renewal path.
3. **Task truth.** A `202` or task identifier means admitted, never succeeded. The component renders
   exactly the eight canonical states. `Succeeded` is rendered only after authoritative task and
   expected read-model confirmation; SignalR is a re-query nudge, never completion authority.
4. **Recovery and control.** Lost-response recovery converges through the idempotency identifier and
   `PollTask` or equivalent safe retry. Cancellation is offered only before the irreversible
   checkpoint; later requests show current truth and a safe conflict. `NeedsAttention` exposes only
   authorized reconciliation. Terminal states cannot transition.
5. **Accessibility.** All states and controls are keyboard operable. Focus moves deterministically
   after validation, renewal, admission, cancellation, and terminal outcomes, then restores to the
   originating safe control when appropriate. Live regions announce meaningful changes without
   repeated polling noise. Meaning never relies only on color, motion, position, or elapsed time,
   and the fixture passes at 200% zoom and 320 CSS pixels.
6. **Boundary and privacy.** FrontComposer contains no Projects domain decision; Projects contains
   no cloned generic lifecycle component or server workflow. The browser receives no secrets,
   payload-bearing content, or authority claim and logs no artifact/token value.
7. **Compatibility.** Existing Projects and FrontComposer prototype lifecycles are not declared
   canonical. Any temporary adapter maps them explicitly at one boundary, rejects an unrepresentable
   canonical state safely, and is removed or retained only through an owner-approved compatibility
   disposition.
8. **Evidence.** Every required fixture case passes from accepted clean checkouts. Missing
   environments, unexplained skips, mutable revisions, unresolved critical/serious accessibility
   findings, and unhashed or ownerless artifacts block acceptance.

**Verification commands.** The accepted revisions must make these commands executable from the
named repository roots and record their exit codes:

```bash
# Hexalith.FrontComposer
DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx \
  --filter "Category=Story8.3P1" \
  --logger "trx;LogFileName=8.3-p1-frontcomposer.trx" \
  --results-directory evidence/epic8

# Hexalith.Projects
DiffEngine_Disabled=true dotnet test tests/Hexalith.Projects.UI.Tests/Hexalith.Projects.UI.Tests.csproj \
  --filter "Category=Story8.3P1" \
  --logger "trx;LogFileName=8.3-p1-projects-adapter.trx" \
  --results-directory evidence/epic8

# Accepted workspace evidence runner supplied by the cleared Epic 8/G-4 gate
dotnet tool run hexalith-evidence validate \
  evidence/epic8/8.3-p1-foundation-manifest.json
```

The `Category=Story8.3P1` tests and evidence runner do not exist as accepted capabilities today;
their absence is a blocker, not permission to substitute an ad hoc command or mark a skipped lane
passing.

**Evidence manifest.** `evidence/epic8/8.3-p1-foundation-manifest.json` records:

- FrontComposer and Projects repository URLs, immutable revisions, dependency/contract versions,
  and compatibility fingerprints;
- accepted G-3/G-6 and readiness dispositions;
- exact commands, exit codes, environment identity, fixture version, expected/actual artifact
  paths, hashes, and results;
- the FrontComposer and Projects TRX artifacts plus component accessibility results;
- negative-control, privacy/redaction, and boundary-check results;
- accountable FrontComposer/Web, Projects Web, Test Architect, Solution Architect, and Product
  Owner dispositions; and
- containment and executable rollback instructions.

**Compatibility and rollback.** Pin the previous accepted FrontComposer/toolchain tuple and retain
the last accepted Web route until P2/P3 and Story 8.3 accept the new foundation. If P1 evidence
regresses, withhold or disable the new presentation package/adapter, restore the pinned tuple and
route, and re-run the prior smoke/evidence lane. Rollback changes no domain event, Confirmation
Artifact, Durable Task record, server workflow, or sibling-owned resource.

**Estimate and completion boundary.** **L.** Complete only when both repository revisions and the
manifest are immutable, every required command passes from accepted clean checkouts, every required
artifact/hash/result exists, rollback is executable, and all accountable owners accept the package.
This completion does not accept P2/P3 domain journeys or the full authenticated NFR-9 release lane.

### 4.2 Reconcile the `sprint-status.yaml` action row

**Artifact:** `_bmad-output/implementation-artifacts/sprint-status.yaml`

**OLD:** Broad action/evidence text; owner omits Projects Web and Solution Architect; no fixture,
manifest, compatibility fingerprint, or rollback path.

**NEW:** Keep `id: "8.3-P1"`, `status: open`, `target_date: uncommitted`, dependencies, and routing.
Change the action and evidence fields to:

```yaml
action: "Accept the finite cross-repository Web presentation foundation defined by the 8.3-P1 package contract, preserving canonical admission, Confirmation Artifact, eight-state task truth, recovery/control, authoritative re-query, accessibility, privacy, and ownership boundaries."
repository_authority: "Hexalith.FrontComposer generic Web foundation and Hexalith.Projects UI-contract/adapter consumption"
owner: "FrontComposer-Web Owner / Projects Web Owner / Test Architect / Solution Architect / Product Owner"
expected_evidence: "Accepted G-3/G-6 and READY dispositions; immutable repository revisions and compatibility fingerprints; epic8/8.3-p1-web-foundation fixture; Story8.3P1 clean-checkout TRX results; eight-state, negative-artifact, lost-response, cancellation, authoritative-re-query, privacy/boundary, keyboard/focus/live-region/200%-zoom/320px results; evidence/epic8/8.3-p1-foundation-manifest.json with hashes, owner approvals, containment, and executable rollback"
planning_evidence: "_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02-8.3-P1.md#41-expand-83-p1-in-epicsmd"
```

The row remains `open`; proposing or applying this planning text is not package acceptance.

### 4.3 Expand the architecture-conformance checklist

**Artifact:**
`_bmad-output/planning-artifacts/epics-architecture-conformance-checklist-2026-07-16.md`

**OLD:** One P1 checkbox names Preview/Confirmation/task/recovery/cancellation/re-query/focus/live
regions without independently checkable evidence.

**NEW:** Preserve the existing Story 8.3 checklist and add P1 sub-checks for:

- accepted entry gates and immutable dependency/contract fingerprints;
- FrontComposer-generic versus Projects-owned descriptor/adapter separation;
- four-class action fidelity and exact eight-state task truth;
- invalid-artifact, lost-response, idempotent recovery, cancellation/checkpoint, reconciliation, and
  authoritative re-query cases;
- privacy/redaction and no client authority;
- component keyboard/focus/live-region/non-color/200%-zoom/320px evidence, while reserving the full
  authenticated NFR-9 result for `8.8-P2`; and
- manifest completeness, hashes, owner dispositions, containment, and executable rollback.

### 4.4 Preserve traceability row identity and add the package handoff

**Artifacts:**

- `_bmad-output/planning-artifacts/implementation-readiness-traceability-matrix.yaml`
- `_bmad-output/planning-artifacts/implementation-readiness-traceability-matrix.md`

**OLD:** `finding-ux-001` correctly maps UX-001 to Story 8.3 and blocks on `8.3-P1/P2/P3`, but it
does not identify the P1 fixture or evidence manifest that Story 8.3 consumes.

**NEW:** Keep the stable row key, primary/supporting stories, final Story 8.3 fixture, verification
command, evidence artifact, estimate, status, and blocker. Add a note that:

> `8.3-P1` is accepted separately through `epic8/8.3-p1-web-foundation` and
> `evidence/epic8/8.3-p1-foundation-manifest.json`; Story 8.3 consumes the immutable accepted
> manifest and does not regenerate or reinterpret it. Full authenticated operator accessibility
> evidence remains owned by `8.8-P2`.

Regenerate the Markdown view mechanically from the canonical YAML after approval. Do not create a
second UX-001 row or change the stable `finding-ux-001` identity.

### 4.5 Preserve PRD, Architecture, and UX semantics

No semantic edit is required to the PRD, addendum, Architecture Spine, or UX specification. The P1
contract assembles already-approved rules from those authorities. During application, only add a
cross-reference if an artifact owner requires navigation; do not restate or fork the normative
contract.

### 4.6 Application sequence after approval

1. Product Owner applies the `epics.md` package contract without changing story IDs or the current
   implementation freeze.
2. Developer/Product Owner reconcile the existing `sprint-status.yaml` row; it stays `open` and
   `uncommitted`.
3. Solution Architect expands conformance checks without weakening G-3, G-6, AD-2, AD-19, or
   AD-32–AD-34.
4. Test Architect updates the canonical traceability YAML and mechanically regenerates its Markdown
   view while preserving stable row identity.
5. FrontComposer/Web and Projects Web owners implement in their respective repositories only after
   the entry gate is accepted.
6. Test Architect executes the package commands and assembles the immutable manifest. Accountable
   owners record acceptance or an explicit blocking disposition.
7. Only an accepted P1 manifest unblocks P2/P3. Only accepted P1/P2/P3 manifests satisfy Story 8.3's
   entry gate.

## 5. MVP Impact and Handoff

The PRD MVP is unchanged and remains achievable. This correction makes an existing release-binding
Web package executable; it neither removes requirements nor advances the schedule. Production-
authority implementation remains blocked until the superseding readiness result and named gates
pass.

### Product Owner / Developer

- Apply the approved planning edits and preserve stable epic/story/work-package IDs.
- Keep `8.3-P1` open until its immutable manifest is accepted; do not equate merged code or a local
  test run with package acceptance.
- Confirm that the package is sufficient for P2/P3 consumption and does not absorb their domain
  journeys.

### FrontComposer/Web Owner

- Implement only reusable presentation primitives and generic state/accessibility composition.
- Publish the accepted revision, dependency fingerprint, clean-checkout command/results, and
  containment/rollback instructions.

### Projects Web Owner

- Own versioned Projects UI descriptors, generated-contract mapping, adapter fixture, and Projects
  consumption proof.
- Remove or explicitly contain local lifecycle semantics that cannot represent the canonical
  contract; do not duplicate the shared component or server workflow.

### Test Architect

- Own `epic8/8.3-p1-web-foundation`, required test categorization, result paths, negative controls,
  accessibility evidence, manifest validation, and hashes.
- Reject missing environments, unexplained skips, mutable evidence, unrepresentable states, or
  unresolved critical/serious accessibility findings.

### Solution Architect

- Accept G-3/G-6 inputs, ownership boundaries, compatibility fingerprint, containment, and rollback.
- Verify that signal delivery never becomes completion authority and that Web logic never becomes
  server/domain authority.

## 6. Change Navigation Checklist Results

### Section 1 — Understand the trigger and context

- [x] **1.1** Trigger identified: `8.3-P1`, created by the approved 2026-08-02 correction as the
  shared Web foundation required before P2/P3 and Story 8.3.
- [x] **1.2** Core problem categorized as misunderstanding/underspecification of the approved
  decomposition; no new requirement or strategy change.
- [x] **1.3** Evidence recorded from epics, sprint status, traceability, UX, current readiness gates,
  and the checked-out Projects/FrontComposer lifecycle surfaces.

### Section 2 — Epic impact

- [x] **2.1** Epic 8 remains completable after making P1 independently executable.
- [!] **2.2** Existing Epic 8 package detail must change; no new epic or story is needed.
- [x] **2.3** Remaining epics and downstream dependencies reviewed; direct impact is P2/P3, Story
  8.3, and later acceptance of operator accessibility/release evidence.
- [N/A] **2.4** No epic is obsolete and no new epic is required.
- [x] **2.5** Epic/story order and priorities remain unchanged.

### Section 3 — Artifact impact

- [x] **3.1** PRD reviewed; no goal, requirement, or MVP change required.
- [x] **3.2** Architecture reviewed; existing decisions and gates remain authoritative with no
  semantic change.
- [x] **3.3** UX reviewed; Journey 3 and Maintenance Action Panel already contain the needed behavior.
- [!] **3.4** Epics, conformance, traceability, and sprint tracking require synchronized edits after
  approval; source implementation and evidence follow under repository-owner authority.

### Section 4 — Path forward

- [x] **4.1** Direct Adjustment is viable — medium planning effort, large implementation package,
  medium residual risk.
- [N/A] **4.2** Rollback is not justified; the prior approved decomposition should be preserved.
- [N/A] **4.3** MVP reduction does not solve the acceptance-contract gap.
- [x] **4.4** Selected approach: Direct Adjustment with unchanged readiness and release gates.

### Section 5 — Proposal components

- [x] **5.1** Issue and discovery evidence summarized.
- [x] **5.2** Epic and artifact impacts documented.
- [x] **5.3** Recommended path, trade-offs, effort, and risk documented.
- [x] **5.4** MVP impact, dependencies, sequencing, commands, evidence, and completion boundary
  documented.
- [x] **5.5** Product Owner/Developer, FrontComposer Web, Projects Web, Test Architect, and Solution
  Architect handoffs defined.

### Section 6 — Final review and handoff

- [x] **6.1** All applicable checklist sections are addressed; proposed downstream edits are explicit.
- [x] **6.2** Proposal checked against the complete PRD bundle, Architecture Spine, epics, UX,
  conformance checklist, traceability authority, sprint status, current readiness report, existing
  2026-08-02 proposal, and checked-out repository surfaces.
- [ ] **6.3** Explicit user approval is pending.
- [N/A] **6.4** No story status or story ID changes are proposed; the existing action row remains
  `open` until independent acceptance.
- [ ] **6.5** Moderate-scope handoff begins only after approval and application of the planning edits.

## 7. Approval Gate and Success Criteria

This proposal does not lift the implementation freeze, satisfy G-3/G-6, accept `8.3-P1`, unblock
P2/P3, or modify source code. It is successful when:

1. the proposed planning edits are explicitly approved and applied without overwriting the approved
   `sprint-change-proposal-2026-08-02.md` record;
2. authoritative artifacts agree on the P1 entry gate, repository split, fixture, acceptance matrix,
   commands, manifest, owner dispositions, compatibility, rollback, and completion boundary;
3. P1 remains `open` until accepted clean-checkout evidence exists at immutable revisions;
4. P2/P3 and Story 8.3 consume the accepted manifest without regenerating or weakening it;
5. full authenticated operator WCAG evidence remains owned by `8.8-P2`; and
6. no client presentation, prototype lifecycle, SignalR notification, or local synonym becomes
   domain, authorization, task, or completion authority.

Pending review disposition: **Continue (`c`)** to move to the explicit approval decision, or
**Edit (`e`)** to revise the proposal first.
