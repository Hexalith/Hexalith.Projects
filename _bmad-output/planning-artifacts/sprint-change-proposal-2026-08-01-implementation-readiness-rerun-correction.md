---
title: "Sprint Change Proposal: Restore Implementation Readiness After the 2026-08-01 Rerun"
date: 2026-08-01
status: approved-for-implementation
workflow: bmad-correct-course
review_mode: incremental
change_scope: major
trigger: "The 2026-08-01 implementation-readiness rerun returned NOT READY with 14 findings."
source_report: "_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-01-rerun.md"
requirements_coverage: "24/24"
approved_checklist_sections:
  - 1
  - 2
  - 3
  - 4
  - 5
  - 6
approved_edit_proposals:
  - 1
  - 2
  - 3
  - 4
  - 5
  - 6
  - 7
complete_proposal_approval: approved
approved_by: Jerome
approved_on: 2026-08-01
application_status: not-applied
handoff_status: issued
---

# Sprint Change Proposal: Restore Implementation Readiness After the 2026-08-01 Rerun

## 1. Issue Summary

### 1.1 Trigger

The independent implementation-readiness rerun dated 2026-08-01 returned
NOT READY. It recorded 14 findings: two critical blockers, eight major or
alignment issues, and four warnings or minor concerns.

The triggering story is Story 6.1, List and open Projects through supported
authenticated paths. It is the first production-authority story and has no
accepted executable entry path from the current planning state.

### 1.2 Approved Problem Statement

The production backlog cannot safely begin because Story 6.1 lacks an accepted
clean-checkout execution and verification path. The required G-4 runner and
evidence tools are target contracts rather than accepted, remotely restorable
tools, and their repository manifests are absent. Downstream planning also
conflates implemented capability with accepted capability for P2 and P3,
narrows FR-8 and association-action role coverage, and lacks one complete
FR-21 audit-production acceptance owner.

The PRD, Architecture Spine, and Epic 6 to Epic 8 product sequence remain
coherent. The correction is therefore a planning, acceptance, and external
capability adjustment rather than a product pivot.

### 1.3 Evidence

- The readiness rerun reports 24 of 24 Functional Requirement identifiers
  covered, but implementation readiness remains NOT READY.
- Story 6.1 is blocked by the P1R, P0, P2, P3, and P4 acceptance chain.
- The repository does not currently contain .config/dotnet-tools.json or
  module/hexalith-projects.module.json.
- Architecture decisions AD-25 and AD-30 state that the module runner and
  evidence validator are target entry contracts and not claims of current
  tool availability.
- The P2 specification records that the G-4 command failed because the
  hexalith-module command was absent from the tool manifest.
- epics.md and sprint-status.yaml mark P2 open as though the capability were
  absent. The production identity runbook and P3 review instead document the
  EventStore implementation at observed root-declared revision 5c123ccb,
  containing commits 58236cf3 and b904322b.
- PRD FR-7, FR-8, and FR-11 plus AD-33 allow an action-authorized Project User
  to move or unlink associations and replace a Project Folder. Stories 7.4,
  7.5, 7.6, 7.8, and 7.10 do not enumerate that authority completely.
- FR-21 and AD-26 define a complete audit-emission and exclusion taxonomy.
  Story 8.1 currently focuses on inspection while producer responsibility is
  distributed across Epic 7 and Story 8.2 without one complete acceptance
  matrix.

### 1.4 Findings Disposition

| Finding | Summary | Proposed disposition |
| --- | --- | --- |
| EQ-C1 | Story 6.1 has no executable first path | Keep blocked; complete the corrected P1R to P4 acceptance path and rerun readiness |
| EQ-C2 | G-4 commands are not executable clean-checkout gates | Publish and pin restorable tools; add manifests; retain machine evidence |
| UX-A1 | UX confirmation language is broader than AD-5 | Distinguish confirmation-required actions from task-only actions |
| UX-A2 | Replay and case-ID language implies retained inference history | Define fresh recomputation and request-scoped correlation |
| UX-A3 | Dry-run event is not durable FR-21 audit truth | Classify Preview and dry-run as transient telemetry |
| EQ-M1 | Project User role coverage is incomplete or ambiguous | Amend Stories 7.4, 7.5, 7.6, 7.8, and 7.10 |
| EQ-M2 | FR-21 lacks one complete owning matrix | Make Story 8.1 accountable for the canonical producer matrix |
| EQ-M3 | Story 8.10 is epic-sized resilience evidence | Split acquisition into 8.10-P1 through 8.10-P4 and retain a small roll-up |
| EQ-M4 | Chatbot companion evidence is unpinned | Keep 8.8-P3 external and release-blocking |
| EQ-M5 | Historical and production criteria coexist | Move Epics 1 to 5 into a non-schedulable historical appendix |
| UX-W1 | Chatbot companion remains a release dependency | Require separately owned immutable 8.8-P3 evidence |
| UX-W2 | Presentation implementation remains toolchain-gated | Preserve G-3 and G-6 entry gates |
| EQ-N1 | Epic 7 shared invariants may be lost in standalone stories | Materialize applicable invariant and evidence rows in each story specification |
| EQ-N2 | Stories 6.3 and 8.3 are broad | Retain for now; split only if estimate or fixture ownership becomes non-uniform |

## 2. Impact Analysis

### 2.1 Epic Impact

#### Epic 6: Authorized Project Reads

Epic 6 remains the correct first production epic. Its observable outcomes and
seven-story scope remain valid. Implementation cannot begin until its shared
baseline is executable and accepted.

Required adjustment:

- Separate P2 and P3 implementation state from acceptance state.
- Complete P1R against one immutable source, package, runner, and architecture
  tuple with an executable rollback.
- Publish and accept P0/G-4 tooling.
- Accept P2 and P3 against the selected tuple.
- Complete P4 with clean-checkout evidence and owner approvals.
- Run an independent readiness assessment before Story 6.1 can return to
  ready-for-development.

#### Epic 7: Durable Project Decisions

Epic 7 remains viable and correctly follows Epic 6. Its durable-workflow model
does not change.

Required adjustment:

- Correct the personas and acceptance criteria for Stories 7.4, 7.5, 7.6,
  7.8, and 7.10.
- Preserve direct and delegated authority parity.
- Explicitly deny Tenant Operator access to association move, unlink, and
  Folder replacement.
- Materialize every applicable shared durable-workflow and FR-21 audit row in
  each standalone story specification.

#### Epic 8: Safe Operations and Release Confidence

Epic 8 remains the correct release-evidence epic.

Required adjustment:

- Make Story 8.1 the accountable owner for the canonical FR-21 audit-emission
  and exclusion matrix.
- Decompose Story 8.10 evidence acquisition into four independently accepted
  packages while retaining Story 8.10 as a small integration roll-up.
- Keep 8.8-P3 as a separately owned Chatbot dependency.
- Preserve Story 8.11 as the terminal release decision.

#### Historical Epics 1 to 5

Epics 1 to 5 remain implementation history and must not drive future
implementation. Their content is preserved verbatim in a separately indexed
historical appendix, while machine-readable selection rules reject their story
IDs.

### 2.2 Story Impact

| Story or package | Impact |
| --- | --- |
| 6.1 | Remains blocked; blocker language distinguishes implemented P2/P3 capability from pending acceptance |
| 6.1-P0 | Must deliver accepted G-4 tools and manifests, not target-only commands |
| 6.1-P1R | Must select and prove the immutable current tuple and executable rollback |
| 6.1-P2 | Capability documented implemented; acceptance remains open |
| 6.1-P3 | Implementation fixtures documented complete; acceptance remains open |
| 6.1-P4 | Integrates exact accepted P0, P1R, P2, and P3 inputs and the clean-checkout result |
| 7.4, 7.5, 7.6, 7.8, 7.10 | Add explicit Project User and delegated-authority coverage plus Tenant Operator denial |
| All applicable 7.x stories | Copy relevant shared durable-workflow and audit obligations into standalone specifications |
| 8.1 | Own the complete FR-21 matrix, deduplication, and telemetry separation |
| 8.8 | Remains blocked on accepted 8.8-P1, P2, and P3 packages |
| 8.10 | Becomes an S-sized integration acceptance over 8.10-P1 through P4 |
| 8.11 | Remains the terminal release gate |

### 2.3 Artifact Conflicts

| Artifact | Impact | Disposition |
| --- | --- | --- |
| PRD and addendum | No product conflict | No change |
| Architecture Spine decisions | No design conflict | Preserve AD-1 through AD-34 |
| Architecture Stack and gate bindings | P1R candidate and accepted binding differ | Update only after owner acceptance |
| epics.md | Status, role, audit ownership, resilience sizing, and historical selection need correction | Update after complete proposal approval |
| UX specification | Admission, replay, and dry-run audit language conflict with adopted decisions | Update after approval |
| sprint-status.yaml | P2/P3 state is ambiguous and affected dependencies are stale | Synchronize atomically |
| Traceability YAML and Markdown | Blockers conflate implementation with acceptance | Update YAML authority and regenerate Markdown |
| Conformance checklist | Role, audit, package, and gate checks require reconciliation | Synchronize |
| Story 6.1/P2/P3 specifications | Metadata and acceptance state conflict with project documentation | Reconcile without weakening the gates |
| Test designs | FR-21 and Story 8.10 evidence ownership requires decomposition | Update |
| G-4 repository manifests | Required files are absent | Deliver through Builds/platform authority |
| Readiness reports and prior proposals | Historical evidence | Preserve unchanged |

### 2.4 Technical and Delivery Impact

The planning correction does not itself add runtime capability. External
implementation remains required for the G-4 runner and validator packages,
their manifests, and the accepted platform tuple.

No event history is rewritten. No unsafe dual writer is introduced. No sibling
repository change is authorized by this Projects proposal. Every external
change requires repository-local approval, immutable versions, focused
validation, retained evidence, and rollback.

## 3. Recommended Approach

### 3.1 Selected Path

Use Direct Adjustment followed by an independent implementation-readiness
rerun.

The production order remains:

    Epic 6 -> Epic 7 -> Epic 8

The immediate executable path becomes:

    P1R
      -> P0 publication and P2 acceptance
      -> P3 acceptance
      -> P4
      -> independent readiness rerun
      -> Story 6.1 only if the result is READY

### 3.2 Options Evaluated

| Option | Viability | Effort | Risk | Decision |
| --- | --- | --- | --- | --- |
| Direct Adjustment | Viable | Medium planning effort; high external execution effort | Medium after acceptance; high while gates are unresolved | Recommended |
| Potential Rollback | Not viable | High | High | Reject; no production-authority work needs reversal |
| PRD MVP Review | Not viable | High | High | Reject; requirements are coherent and no approved v1 FR/NFR is deferrable |

### 3.3 Timeline Impact

The planning corrections are bounded. The external delivery timeline remains
uncommitted until the responsible owners accept immutable revisions, exact
commands, evidence artifacts, and rollback.

No implementation date or release date may be inferred from estimates in this
proposal. Story 6.1 remains blocked until a superseding assessment returns
exactly READY. Production release remains separately blocked until Story 8.11
passes with dated terminal acceptance from Jerome and John.

## 4. Detailed Change Proposals

### 4.1 Reconcile P2/P3 State and the Story 6.1 Gate

**Artifacts:** epics.md, sprint-status.yaml, traceability matrices, and Story
6.1/P2/P3 specifications.

**Old:**

    6.1-P2: open; blocked by accepted P1R
    6.1-P3: open; blocked by P2

    Story 6.1 describes the absent P2 dual-principal, safe-denial,
    and watermark capability as an open blocker.

**New:**

    6.1-P2
      capability implementation: documented complete in EventStore at
      observed root-declared revision 5c123ccb, containing implementation
      commits 58236cf3 and b904322b
      acceptance: open
      acceptance requires: P1R confirmation of the selected immutable tuple,
      G-4 persisted/restart/cross-Tenant evidence, exact source/package pin,
      executable rollback, acceptance record, and accountable-owner approval

    6.1-P3
      implementation fixtures: documented complete; 582 of 582 server tests
      and 20 of 20 integration tests passed
      acceptance: open
      acceptance requires: accepted P2 contract revision, identity and
      configuration owner approval, immutable configuration boundary,
      and rollback disposition

    Story 6.1
      remains blocked by P1R, P0/G-4, P2 acceptance, P3 acceptance, and P4;
      it no longer describes P2/P3 implementation as absent

The critical path is:

    P1R -> P0 and P2 acceptance -> P3 acceptance -> P4
      -> independent readiness rerun -> Story 6.1

**Rationale:** This separates implemented from accepted, removes stale
cross-repository bookkeeping, and preserves AD-30 without reopening completed
capability development.

### 4.2 Correct FR-8 and Association Role Coverage

**Stories:** 7.4, 7.5, 7.6, 7.8, and 7.10.

**Old:**

    Stories 7.4, 7.5, 7.8, and 7.10:
    As a Tenant Project Administrator or delegated caller...

    Story 7.6:
    As a Tenant Project Administrator...

**New persona:**

    As an action-authorized Project User or Tenant Project Administrator,
    acting directly or through a delegated service caller that preserves
    the original actor's authority...

Add to every affected story:

    Given an action-authorized Project User or Tenant Project Administrator,
    when the action is invoked directly or through an authenticated delegated
    caller, then both paths enforce the same AD-33 authorization policy and
    resource-owner reauthorization.

    Given a Tenant Operator or another unauthorized actor, when Preview or
    admission is requested, then the operation fails closed with no
    Confirmation Artifact, task, partial durable effect, or protected
    existence disclosure.

    Given a delegated workload, when its service identity is valid but the
    original actor lacks authority, then workload authority does not widen
    the actor's permissions.

Story-specific behavior remains move Conversation, unlink Conversation,
replace Folder, unlink File Reference, and unlink Memory respectively.

**Rationale:** Aligns the backlog with FR-7, FR-8, FR-11, and AD-33 while
making direct/delegated parity and Tenant Operator denial testable.

### 4.3 Give FR-21 One Complete Audit Owner

**Artifacts:** Story 8.1, Epic 7 shared invariant 5, Story 8.2, and the fr-21
traceability row.

**Old:**

    Story 8.1 primarily verifies inspection of task, audit, and
    reconciliation records. Audit production remains distributed without
    one complete producer acceptance matrix.

**New:**

Story 8.1 becomes accountable for a canonical machine-checkable FR-21 matrix.

Required durable-audit inclusions:

- Task admission and terminal outcome.
- Confirmation use and cancellation.
- Stale, replayed, or tampered confirmation rejection.
- Authorization denial.
- Every confirmed Project mutation and association outcome.
- Manual reconciliation.
- Stable upstream receipt identifiers.
- Safe Diagnostic Export attempt and outcome.

Required durable-audit exclusions:

- Intermediate task states.
- Polling and retries.
- Dependency latency.
- Notifications.
- Unused confirmation expiry.
- Preview and dry-run diagnostics.
- Read-only RefreshContext.
- Current Resolution Traces.

Add these acceptance criteria:

    Given every Epic 7 producer and Story 8.2, when the FR-21 matrix runs,
    then each required inclusion has exactly one accountable producer and
    evidence row, every exclusion remains telemetry-only, and equivalent
    idempotent retries create no duplicate audit identity.

    Given any emitted audit record, then it contains only Tenant, actor,
    Project/action identity, timestamp, safe reason/outcome codes, affected
    reference identifiers, and applicable upstream receipt identifiers.

Epic 7 shared invariant 5 must require each standalone 7.x story to copy its
applicable matrix rows and evidence expectations.

**Rationale:** Story 8.1 remains a coherent operator outcome while becoming
the single acceptance owner for complete audit production, deduplication, and
audit-versus-telemetry separation.

### 4.4 Decompose Story 8.10

**Old:**

    Story 8.10 proves restart, two-instance execution, retry, duplicate
    delivery, concurrency, partial failure, lost response, compensation,
    reconciliation, RTO, RPO, and idempotency.

    Estimate: L

**New prerequisite packages:**

| Package | Independently accepted outcome |
| --- | --- |
| 8.10-P1 | Task-engine restart, lease expiry, two-instance convergence, fenced ownership, recovery within five minutes, service RTO, and committed-event RPO |
| 8.10-P2 | Cross-owner saga recovery after unknown responses and partial failure, including receipt recovery, compensation, and truthful NeedsAttention |
| 8.10-P3 | Duplicate delivery, concurrent admission, equivalent retry, changed-request conflict, lost-response recovery, and absence of silent drops or duplicate effects |
| 8.10-P4 | Story 7.15 legacy/interrupted-work reconciliation with terminal or NeedsAttention disposition, no history rewrite, and no unsafe dual writer |

Each package records owner, repository, immutable revision, fixture, exact
command, evidence artifact, rollback or containment, and acceptance
disposition.

**Revised Story 8.10:**

    Story 8.10: Accept cross-workflow resilience evidence

    Entry gate: 8.10-P1 through 8.10-P4 accepted.

    Validate the package manifests together, check representative workflow
    coverage and NFR-3/NFR-4 consistency, and reject missing, failed,
    unavailable, ownerless, or unpinned evidence. Perform no new evidence
    acquisition or cross-repository implementation.

    Estimate: S

**Rationale:** Produces independently diagnosable resilience slices while
retaining Story 8.10 as the single integration acceptance point and preserving
stable story numbering.

### 4.5 Mechanically Quarantine Historical Epics 1 to 5

**Old:**

    One epics.md contains all 71 stories. Historical and production criteria
    remain selectable from the same canonical file.

**New epics.md authority:**

    productionAuthority: "Epics 6-8 only"
    schedulableEpicIds: [6, 7, 8]
    historicalEpicIds: [1, 2, 3, 4, 5]
    rejectHistoricalStoryGeneration: true

- Keep Epics 6 to 8, their prerequisite and evidence ledgers, production
  traceability, and concise historical links in epics.md.
- Move Epics 1 to 5 verbatim to
  _bmad-output/planning-artifacts/history/epics-1-5.md.
- Add history/index.md declaring the appendix immutable and non-schedulable.
- Update story-generation and sprint-selection inputs to reject IDs from
  Epics 1 to 5.
- Preserve every historical statement and trace link.

**Rationale:** Physical separation and machine-readable selection rules
prevent accidental use of superseded acceptance criteria while retaining
historical evidence.

### 4.6 Correct UX Admission, Replay, and Audit Semantics

**Artifact:** ux-design-specification.md.

#### Confirmation Classification

**Old:**

    Preview every mutation, then admit it only through a valid
    Confirmation Artifact.

    Confirmations are required for mutating actions.

**New:**

    Preview and Confirmation Artifact are required only for archive, restore,
    Conversation move, Folder replacement, unlink, ambiguous-resolution
    confirmation, and proposed-creation confirmation.

    Direct creation, Setup update, actor-selected additive Conversation,
    File, and Memory links, and initial Folder setting are task-only actions.
    They require authorization, validation, and idempotent Durable Task
    admission but no second confirmation.

#### Resolution Replay and Retained Identity

Replace resolution replay with a fresh synchronous read-only recomputation.
Any case identifier is request-scoped correlation metadata, not a retained
Resolution Trace identity. Returning users reconstruct confirmed outcomes from
metadata-only audit history; transient inference traces are not retained.
Diagnostic forms use request correlation IDs or durable confirmed-outcome and
audit identifiers rather than resolution case IDs.

#### Dry-Run Audit Semantics

Remove dry-run event from Audit Timeline durable states. Preview and dry-run
feedback are transient operational telemetry unless a separately enumerated
FR-21 audit event is required.

**Rationale:** Aligns UX with AD-5, AD-7, AD-26, FR-17, and FR-21 without
changing user journeys or visual design.

### 4.7 Synchronize Artifacts and Establish the Executable G-4 Gate

**Old:**

- Capability implementation and acceptance share ambiguous open/done states.
- YAML and Markdown evidence views repeat stale blocker language.
- Specifications conflict with runbooks and review records.
- G-4 commands are documented while both repository manifests are absent.
- Partial artifact updates can recreate contradictory readiness claims.

**New tracking model:**

    capability_status: documented-implemented | not-implemented
    acceptance_status: pending | accepted | rejected
    accepted_revision: null
    evidence_status: unavailable | failed | passed

Synchronize these artifacts in one reviewed change:

- epics.md.
- sprint-status.yaml.
- implementation-readiness-traceability-matrix.yaml.
- The generated Markdown traceability view.
- The architecture conformance checklist.
- Story 6.1/P2/P3 specifications.
- Affected 7.x and 8.x specifications and test designs.

The YAML traceability matrix remains authoritative. Its Markdown view is
regenerated rather than independently edited.

G-4 delivery belongs to Builds/platform owners. Acceptance requires:

- A checked-in .config/dotnet-tools.json.
- A checked-in non-secret module/hexalith-projects.module.json.
- Remotely restorable immutable tool packages.
- Successful clean-checkout restore, run, full test, down, and evidence
  validation.
- Retained machine evidence.
- Deterministic teardown.
- Executable rollback.
- Named owner approvals.

No placeholder, stale binary, or local-only package satisfies the gate.
Architecture version and pin sections are updated only after P1R and G-4
acceptance. Adopted architecture decisions remain unchanged.

**Rationale:** Makes readiness machine-checkable, prevents partial
synchronization, and converts G-4 from an aspirational command contract into
an objectively accepted entry gate.

## 5. Implementation Handoff

### 5.1 Scope Classification

**Major.**

The product and architecture remain stable, but the correction spans epics,
UX, story specifications, evidence, sprint tracking, external platform tools,
multiple repository owners, and an independent readiness decision.

### 5.2 Recipients and Responsibilities

| Recipient | Responsibility |
| --- | --- |
| Product Manager | Own the cross-artifact replan, containment, and scope integrity |
| Solution Architect | Confirm P1R, P2/P3 acceptance semantics, AD/G-gate conformance, and rollback boundaries |
| Product Owner | Apply approved epics, story, historical-quarantine, traceability, and sprint-status changes atomically |
| Builds Owner | Publish and pin the G-4 tool packages and tool manifest |
| Platform Owner | Accept the module runner, manifest schema, lifecycle, fixtures, evidence, and teardown contract |
| EventStore Owner | Confirm the selected P1R/P2 source and package tuple and public capability signatures |
| Identity/Security Owner | Accept P2/P3 identity, configuration, denial, and rollback evidence |
| UX Owner | Apply the three Projects UX corrections |
| Chatbot Presentation and Test Owners | Supply and approve immutable package 8.8-P3 |
| Test Architect | Own FR-21, 8.10 package, clean-checkout, and independent readiness evidence |
| Developer | Receive Story 6.1 only after the independent result is READY |
| Jerome and John | Retain terminal Story 8.11 production-release authority |

### 5.3 Sequenced Action Plan

1. Preserve implementation containment and the rerun report.
2. Apply the approved local planning and UX edits atomically.
3. Reconcile P2/P3 implementation and acceptance state.
4. Accept P1R.
5. Deliver and accept P0/G-4.
6. Complete P2 and P3 acceptance against the selected tuple.
7. Complete P4 and run all clean-checkout commands.
8. Validate the YAML evidence matrix and regenerated Markdown view.
9. Run an independent implementation-readiness assessment.
10. Move Story 6.1 to ready-for-development only if the result is exactly
    READY.

### 5.4 Success Criteria

- P2 and P3 no longer appear unimplemented when implementation is documented,
  while neither appears accepted without immutable evidence and approval.
- The G-4 packages restore remotely from a clean checkout.
- Both required manifests are checked in, valid, non-secret, and pinned.
- All five Story 6.1 clean-checkout commands pass and retain evidence.
- Story 6.1 has an accepted P4 record and a ready-for-development
  specification.
- Stories 7.4, 7.5, 7.6, 7.8, and 7.10 cover Project User, Administrator,
  delegated-principal parity, and Tenant Operator denial.
- Story 8.1 validates the complete FR-21 producer and exclusion matrix.
- Story 8.10 accepts four independently reviewable evidence packages.
- Historical Epics 1 to 5 cannot be selected for scheduling or story
  generation.
- UX no longer implies universal confirmation, persisted Resolution Trace
  history, or durable dry-run audit.
- The YAML traceability matrix validates and the Markdown view is synchronized.
- An independent implementation-readiness rerun returns exactly READY.
- Production remains blocked until Story 8.11 terminal acceptance.

### 5.5 Containment and Rollback

- This proposal does not authorize a sibling repository mutation.
- No event history is rewritten.
- No dual writer is introduced.
- No current failed, skipped, unavailable, or local-only evidence becomes
  passed.
- Planning changes preserve prior reports and proposals as historical evidence.
- G-4 and P1R acceptance records include executable rollback to the last
  owner-approved tuple.
- If any clean-checkout command, evidence validation, or owner acceptance
  fails, Story 6.1 remains blocked and the failed result is recorded honestly.

## 6. Workflow Review State

Checklist Sections 1 through 5 and all seven detailed edit proposals were
approved incrementally by Jerome on 2026-08-01. Jerome reviewed the complete
proposal and explicitly approved it for implementation on 2026-08-01.

Checklist Section 6 disposition:

| Item | Status | Disposition |
| --- | --- | --- |
| 6.1 Check incremental approvals | Done | Sections 1 through 5 and edit proposals 1 through 7 are approved |
| 6.2 Review complete proposal | Done | Complete proposal reviewed without requested revision |
| 6.3 Obtain explicit approval | Done | Approved by Jerome on 2026-08-01 |
| 6.4 Update sprint-status.yaml | Action Needed | Product Owner updates it atomically with the approved planning changes; this proposal does not claim application |
| 6.5 Confirm handoff and next steps | Done | Major-change handoff issued below |

## 7. Approval and Handoff Record

### 7.1 Approval

Jerome approved this complete Sprint Change Proposal for implementation on
2026-08-01. Approval authorizes coordinated execution of the changes and
external acceptance work defined here. It does not mark any planning edit,
G-4 tool, evidence package, acceptance gate, or sprint-status transition as
completed.

### 7.2 Major-Change Handoff

Primary handoff is to the Product Manager and Solution Architect for the
cross-artifact replan and architecture/gate acceptance. Execution requires
the Product Owner, UX Owner, Test Architect, Builds Owner, Platform Owner,
EventStore Owner, Identity/Security Owner, and Chatbot presentation and test
owners identified in Section 5.2.

The Product Owner applies the approved local artifact changes as one reviewed
unit and updates sprint-status.yaml only when the synchronized state is true.
External repository owners independently deliver and accept immutable G-4,
P1R, P2, P3, P4, 8.8-P3, and 8.10 evidence under their own repository rules.

### 7.3 Immediate Next Steps

1. Product Manager and Solution Architect open the coordinated Major-change
   implementation activity using this proposal as the approved authority.
2. Product Owner prepares the atomic Projects planning, UX, specification,
   traceability, conformance, test-design, history, and sprint-status change.
3. Builds and platform owners establish the accepted, remotely restorable
   G-4 toolchain and both repository manifests.
4. Evidence owners execute the P1R to P4 acceptance path and retain the exact
   commands, immutable revisions, results, approvals, and rollback records.
5. Test Architect runs an independent implementation-readiness assessment.
6. Story 6.1 remains blocked unless that assessment returns exactly READY.
