---
title: "Sprint Change Proposal: Externalize Story 6.1 Platform Prerequisites"
status: approved
created: 2026-07-17
approved: 2026-07-17
approved_by: Jerome
handoff_status: routed
project: Hexalith.Projects
prepared_for: Jerome
scope: major
mode: batch
trigger_story: _bmad-output/implementation-artifacts/6-1-list-and-open-projects-through-supported-authenticated-paths.md
trigger_spec: _bmad-output/implementation-artifacts/spec-6-1-list-and-open-projects-through-supported-authenticated-paths.md
trigger_report: _bmad-output/planning-artifacts/implementation-readiness-report-2026-07-17.md
preserves:
  - _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md
  - _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/addendum.md
  - _bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md
---

# Sprint Change Proposal: Externalize Story 6.1 Platform Prerequisites

## 1. Issue Summary

### Trigger

The Story 6.1 specification failed the `ready-for-development` standard on the
**Sufficient** criterion. The failure is correct: the story describes the required product behavior,
but implementation would still require the developer to choose or create platform contracts that
are neither available nor authorized inside the Hexalith.Projects repository.

The blocking gaps are:

- EventStore checked source, the adopted architecture pin, and centrally managed package versions
  are incompatible or not covered by a single owner-approved normalization decision;
- the query path cannot carry the complete immutable dual-principal identity required by AD-20;
- the gateway can expose a distinguishable `403` path instead of AD-19's safe-denial `404`;
- the supported projection API does not expose an accepted global-position watermark;
- production authentication is conditional rather than fail-closed under an approved G-5 contract;
- the Epic 6 entry gate and finite source/package/API normalization record lack owner approval; and
- the G-4 persisted module runner and machine-checkable evidence tool are unavailable.

These are known cross-repository, platform, Builds, identity, and architecture decisions. Treating
them as Story 6.1 implementation tasks would transfer approval authority to the story developer and
would make acceptance evidence dependent on locally invented seams.

### Evidence

- The Story 6.1 file is marked `ready-for-dev` while its `entry_gate` says the package, identity,
  safe-denial, watermark, and G-4 gaps are unresolved.
- The Story 6.1 specification is already marked `blocked` and records the same failed sufficiency
  condition.
- The 2026-07-17 implementation-readiness report authorizes story planning, not implementation,
  identifies external gates as the critical path, and calls out G-4 as the first enablement need.
- AD-6 prohibits unapproved sibling-repository changes; AD-19, AD-20, AD-25, and AD-30 prohibit
  weakening denial, identity, runner, or evidence behavior to make the story locally implementable.
- The PRD addendum requires dependency and version gates to be verified before affected stories
  start.

### Classification

This is a technical and ownership limitation exposed during story specification. It is not a new
product requirement and not a failure of FR-2 or FR-5.

The current sequencing is invalid: a product-value story was promoted to `ready-for-dev` while the
platform capabilities it consumes remain unapproved or absent.

## 2. Impact Analysis

### PRD Impact

No PRD change is required. FR-2, FR-5, NFR-1, NFR-5, and NFR-10 remain in scope without weakening,
deferral, or renumbering.

### Architecture Impact

No adopted invariant should be relaxed. The correction operationalizes the existing spine:

- AD-6 requires separately authorized sibling work and named owners;
- AD-14 requires supported incremental read-model and cursor seams;
- AD-19 requires externally indistinguishable safe denial;
- AD-20 requires immutable actor and workload principal separation;
- AD-25 requires the G-4 persisted platform runner; and
- AD-30 requires machine-checkable evidence before implementation or release claims.

The architecture version table may require an atomic revision after the EventStore and Builds
owners select a supported baseline. This proposal does not select that version. The selected source
revision, architecture pin, central package version, and tested API surface must agree before the
revision is accepted.

### UX Impact

No UX change is required. The operator list/open journey, safe denial, recovery guidance, and
snapshot-state presentation remain governing.

### Epic Impact

Epic 6 remains necessary and its product outcome remains viable. It cannot begin with Story 6.1 as
currently sequenced.

The Epic 6 entry gate should become an explicit dependency ledger with story-applicable gates,
rather than one prose task embedded inside the first value story. All relevant gates remain
mandatory; unrelated later-story gates do not need to block 6.1:

- the 6.1 read baseline requires the EventStore/Builds normalization, G-4 runner, query security and
  watermark capabilities, G-5 production identity contract, and final owner acceptance;
- G-2 sibling read contracts additionally gate the Epic 6 stories that read those siblings; and
- G-3 FrontComposer or CLI adapter gates additionally apply to their respective surface stories.

Epics 7 and 8 remain in sequence and unchanged in product scope. They inherit the accepted shared
platform baseline and must not claim completion using evidence produced by an unavailable or
unapproved runner.

No completed Epic 1–5 story should be rolled back. Those artifacts provide comparison and regression
authority but do not satisfy the new supported-platform gates by themselves.

### Story Impact

Story 6.1 remains the FR-2/FR-5 value story. Its acceptance criteria remain valid, but its status and
task boundary do not.

Required correction:

- change Story 6.1 from `ready-for-dev` to `blocked`;
- retain the blocked specification status;
- remove responsibility for satisfying cross-repository gates from its implementation tasks;
- add explicit `blocked_by` references to the prerequisite work packages below; and
- return the story to `ready-for-dev` only after the final 6.1 gate record is accepted and the spec
  passes all ready-for-development criteria.

Stories 6.2–6.7 remain backlog items. Their applicable external gates must be resolved before each
is promoted, rather than being assumed satisfied by Story 6.1.

### Secondary Artifact Impact

If approved, the following artifacts require coordinated updates:

- `epics.md`: add the prerequisite work packages and replace the monolithic Epic 6 gate prose with
  the dependency ledger;
- Story 6.1 and its spec: align status, dependencies, and task boundaries;
- `sprint-status.yaml`: add a non-startable `blocked` story state, blocker references, prerequisite
  action items, owners, and acceptance state;
- `epic-6-context.md`: identify repository authority and evidence ownership per prerequisite;
- `implementation-readiness-traceability-matrix.yaml` and `.md`: mark FR-2/FR-5 implementation
  readiness as externally blocked and link the gate evidence;
- `deferred-work.md`: record only work that remains genuinely deferred after the prerequisites are
  created; it must not hide an entry-gate blocker; and
- the Architecture Spine and central package files: update atomically only if the approved version
  decision changes the adopted pin.

### Technical and Schedule Impact

The implementation critical path moves from Projects application code to platform enablement and
owner acceptance. This is the honest schedule state: no credible Story 6.1 implementation date can
be committed until the external owners accept the work packages and target dates.

The change prevents several high-risk shortcuts:

- selecting an EventStore version from whichever checkout happens to build;
- collapsing actor and workload identities into one principal;
- preserving a gateway-visible `403` because it is technically distinguishable;
- using time, local sequence, or an inferred offset as a global-position watermark;
- permitting production startup without an authoritative authentication configuration; or
- substituting unit-test output for G-4 persisted runner evidence.

## 3. Recommended Approach

### Selected Path: Direct Adjustment with External Enablement

Preserve Story 6.1's value contract, extract the unresolved enabling work into separately owned
prerequisite work packages, and block the story until the resulting entry-gate record is approved.

The prerequisite graph is:

```text
6.1-P0 G-4 persisted runner and evidence tooling ───────────────┐
                                                                │
6.1-P1 EventStore/Builds version and source normalization       │
  └─> 6.1-P2 Query identity, safe denial, watermark capability  ├─> 6.1-P4 Gate acceptance
       └─> 6.1-P3 Production identity/auth contract             │       └─> Story 6.1 ready-for-dev
                                                                │
owner evidence and rollback records for P0-P3 ──────────────────┘
```

P0 and P1 may proceed in parallel. P2 depends on the normalized platform baseline. P3 depends on
the approved dual-principal contract. P4 accepts, but does not invent, the preceding decisions.

### Option Evaluation

| Option | Viability | Effort | Risk | Effect |
|---|---|---:|---:|---|
| Direct adjustment with external prerequisites | Recommended | High | High, controlled by gates | Preserves MVP and architectural safety while exposing the real critical path |
| Keep all gaps inside Story 6.1 | Not viable | Apparently medium, actually unbounded | Critical | Requires unauthorized platform and sibling decisions during implementation |
| Roll back completed Epics 1–5 | Not viable | High | High | Does not create the missing supported platform contracts or evidence tool |
| Reduce MVP by weakening identity, denial, watermark, or evidence | Not viable | Medium | Critical | Violates adopted NFRs and architecture; produces no releasable supported path |
| Defer FR-2/FR-5 | Not recommended | Low now, high later | High | Removes a core Epic 6 outcome without resolving the platform dependency |

### Scope Classification

**Major.** The product behavior is unchanged, but delivery requires separately authorized work in
EventStore, Builds/platform tooling, and identity/security ownership, followed by coordinated
Projects planning changes. Product Manager and Solution Architect approval is required before
implementation routing.

## 4. Detailed Change Proposals

### 4.1 Story 6.1 Status and Boundary

**Artifact:**
`_bmad-output/implementation-artifacts/6-1-list-and-open-projects-through-supported-authenticated-paths.md`

**Before:**

```yaml
entry_gate: "Epic 6 gate must be explicitly satisfied; current package, identity, safe-404, watermark, and G-4 gaps are unresolved"
```

```markdown
Status: ready-for-dev
```

The current task list asks the Story 6.1 implementer to satisfy and record the gate before changing
runtime code.

**After:**

```yaml
status: blocked
blocked_by: [6.1-P0, 6.1-P1, 6.1-P2, 6.1-P3, 6.1-P4]
entry_gate: "6.1-P4 must be accepted and the Story 6.1 spec must pass ready-for-development before implementation begins"
```

```markdown
Status: blocked
```

Replace the cross-repository gate-resolution task with a non-implementation entry condition. Story
6.1 may consume the approved contracts and implement Projects-owned adapters, handlers, read models,
authentication composition, and evidence scenarios. It may not select platform versions, revise
sibling contracts, or create G-4 tooling.

The acceptance criteria remain unchanged except for references needed to bind them to the accepted
gate artifacts.

### 4.2 Add Epic 6.1 Prerequisite Work Packages

These are enablement work packages, not user-value stories and not substitutes for Story 6.1. Each
must be represented by an owner-approved repository-local issue or story before work begins.

#### 6.1-P0 — Deliver the G-4 Persisted Runner and Evidence Tooling

- **Repository authority:** Builds/platform tooling repository selected by the Builds owner.
- **Accountable owners:** Builds Owner, Platform Owner, Test Architect.
- **Required outcome:** a supported runner composes the platform with at least two modules, exercises
  persisted write/read/rehydration behavior, emits deterministic machine-readable evidence, records
  tool and package versions, and distinguishes execution failure from evidence failure.
- **Required evidence:** owner-approved runner revision; reproducible command; persisted-state
  fixture; evidence schema and sample; negative-control result; rollback procedure.
- **Completion boundary:** Projects can invoke the pinned tool without copying or reimplementing it.

#### 6.1-P1 — Normalize EventStore Source, Architecture, and Central Package Versions

- **Repository authority:** EventStore and Builds repositories, with the Architecture Spine updated
  only through architecture-owner approval.
- **Accountable owners:** EventStore Owner, Builds Owner, Solution Architect.
- **Required outcome:** one supported EventStore source revision and package version is selected;
  the source checkout, central package management, architecture pin, transitive package graph, and
  G-4 runner all agree.
- **Required evidence:** a finite normalization record listing repository revision, package IDs and
  versions, namespaces, public symbols/signatures used by 6.1, compatibility results, owner approvals,
  and rollback pins.
- **Completion boundary:** no `latest`, floating branch, local patch, or unrecorded compatibility
  shim remains in the 6.1 dependency chain.

#### 6.1-P2 — Supply the Supported Query Security and Projection Capabilities

- **Repository authority:** EventStore/platform repository.
- **Accountable owners:** EventStore Owner, Identity/Security Owner, Solution Architect.
- **Required outcome:** the supported public API supplies:
  - an immutable query envelope that keeps Tenant, original actor, authenticated workload,
    delegation, scopes/audience, and correlation distinct;
  - a boundary policy or adapter contract that makes forbidden and nonexistent list/open resources
    externally indistinguishable under the AD-19 safe-denial contract; and
  - an authoritative persisted global-position/watermark exposed to projections and cursor scope.
- **Required evidence:** public-contract tests, safe-denial equivalence tests, cross-tenant negative
  controls, watermark replay/restart tests, G-4 evidence, package/revision pin, and rollback plan.
- **Completion boundary:** Projects need no reflection, internal API access, inferred watermark,
  identity synthesis, or gateway-status exception.

#### 6.1-P3 — Approve the Production Identity and Authentication Contract

- **Repository authority:** identity/security platform and its production configuration authority.
- **Accountable owners:** Identity/Security Owner, Projects Owner, Solution Architect.
- **Required outcome:** an approved platform contract makes production authentication and
  authorization composition mandatory and fail-closed; trusted workload identity and original
  actor/delegation claims map to the P2 query envelope; development-only bypass behavior is explicit
  and cannot activate in Production.
- **Required evidence:** configuration contract and supported test fixtures for missing production
  auth, delegated and non-delegated identity mapping, invalid audience/scope, secret/config ownership,
  and rollback behavior. Story 6.1 uses those fixtures to prove the Projects host adoption.
- **Completion boundary:** Story 6.1 can adopt an approved configuration without designing the G-5
  authority model.

#### 6.1-P4 — Accept the Story 6.1 Entry Gate

- **Repository authority:** Hexalith.Projects planning and evidence artifacts.
- **Accountable owners:** Product Owner, Solution Architect, Test Architect, plus recorded approvals
  from every P0-P3 owner.
- **Required outcome:** a finite entry-gate record links the accepted P0-P3 revisions, normalization
  record, verification commands, evidence paths, exception state, expiry/revalidation conditions,
  and rollback pins.
- **Required evidence:** refreshed AD-30 traceability rows for FR-2/FR-5 and applicable NFRs; successful
  gate commands; owner signatures/approval dates; no open critical exception; rerun of the Story 6.1
  spec readiness check.
- **Completion boundary:** only a passing readiness result authorizes transition from `blocked` to
  `ready-for-dev`.

### 4.3 Epic 6 Entry-Gate Text

**Before:** a shared prose gate asks the first story to pin EventStore, FrontComposer, Builds,
sibling reads, G-4, G-5, and G-6 before implementation.

**After:** replace it with a gate ledger that records:

| Gate | Applies before | Evidence owner |
|---|---|---|
| 6.1-P0 through 6.1-P4 | Story 6.1 and every later story consuming the shared read baseline | Owners named above |
| G-2 sibling read contracts | Stories that consume Conversations, Folders, Memories, Parties, or Tenants reads | Respective sibling owner + Projects Architect |
| G-3 FrontComposer contract | FrontComposer/web surface story | FrontComposer Owner |
| G-3 CLI adapter contract | CLI surface story | CLI/platform owner |
| G-6 runtime/toolchain alignment | Every story whose build or evidence depends on the affected runtime/toolchain | Builds Owner + Test Architect |

No gate is waived. The change makes applicability, ownership, sequencing, and evidence explicit.

### 4.4 Architecture and Version Normalization

**Before:** the adopted architecture pin, checked EventStore source, and centrally managed packages
do not form one approved baseline.

**After:** once 6.1-P1 is accepted, atomically align:

1. the Architecture Spine version/source record;
2. EventStore and Builds repository revision pins;
3. centrally managed package versions;
4. the G-4 runner manifest; and
5. Story 6.1's normalization and evidence references.

The selected version is deliberately not specified by this proposal. Choosing it without the named
owners would repeat the original blocking condition.

### 4.5 Story Specification

Keep the current specification status as `blocked`. Add the five prerequisite references and change
the approach language from “after the Epic 6 gate is approved” to the exact 6.1-P4 acceptance record.

After P4 acceptance, rerun specification validation. Do not manually flip the story status if the
spec still fails Complete, Unambiguous, Testable, Consistent, or Sufficient.

### 4.6 Sprint Status and Action Ledger

**Before:**

```yaml
6-1-list-and-open-projects-through-supported-authenticated-paths: ready-for-dev
```

**After:**

```yaml
6-1-list-and-open-projects-through-supported-authenticated-paths: blocked
```

Add `blocked` to the Story Status definitions as a non-startable state and record transitions:

- `ready-for-dev -> blocked` when an entry-gate or specification failure is confirmed;
- `blocked -> ready-for-dev` only when every `blocked_by` item is accepted and the specification
  passes; and
- `blocked` never transitions directly to `in-progress`.

Create action-ledger entries for 6.1-P0 through 6.1-P4 with repository authority, accountable owner,
status, target date or `uncommitted`, evidence URI/path, revision pin, and blocking relationships.
Do not fabricate owners' acceptance or dates.

Epic 6 may remain `in-progress` while authorized prerequisite work is active. If no prerequisite has
an accepted owner and active work item, return Epic 6 to `backlog` rather than implying execution.

### 4.7 Traceability and Evidence

Update the canonical readiness matrix so that FR-2 and FR-5 retain complete requirement and design
coverage but carry an implementation-readiness state of `blocked-external`. Link NFR-1, NFR-5, and
NFR-10 to the P2/P3/P4 evidence and G-4 runner outputs.

Passing planning coverage must not be reported as passing implementation readiness. Evidence entries
must include the exact command, tool version, source/package pins, persisted fixture, result artifact,
owner, and timestamp.

### 4.8 Immediate Containment

Until this proposal is approved and 6.1-P4 passes:

- do not begin Story 6.1 runtime implementation;
- do not mutate EventStore, Builds, identity, FrontComposer, or other sibling repositories under
  Story 6.1 authority;
- do not select or normalize a package version without the P1 owners;
- do not substitute `403`, synthesized principals, inferred offsets, conditional production auth,
  or local-only test evidence for the adopted contracts; and
- retain the Story 6.1 spec as the authoritative blocked record.

## 5. Implementation Handoff

### Required Route

Because this is a major cross-repository correction, route it as follows after explicit approval:

1. **Product Manager / Product Owner:** accept the sequencing change and preserve FR-2/FR-5 scope.
2. **Solution Architect:** sponsor P1/P2, resolve the version baseline with EventStore and Builds
   owners, and approve any Architecture Spine revision.
3. **Builds and Platform Owners:** authorize and deliver P0 in the owning repository.
4. **EventStore and Identity/Security Owners:** authorize and deliver P1-P3 in their owning
   repositories with evidence and rollback pins.
5. **Test Architect:** validate the G-4 tool and the P2/P3 negative-control evidence.
6. **Scrum Master / planning owner:** apply the Epic 6, Story 6.1, sprint status, and action-ledger
   changes atomically.
7. **Story/spec workflow owner:** rerun Story 6.1 readiness after P4; only then may development be
   scheduled.

Repository-local implementation stories or issues must be created in each owning repository. This
proposal authorizes none of those mutations by itself.

### Effort and Risk

- P0: large platform-tooling effort; high evidence-chain risk.
- P1: medium normalization effort; high dependency and compatibility risk.
- P2: large platform-contract effort; critical security and persistence risk.
- P3: medium cross-boundary configuration effort; critical production-authentication risk.
- P4: small-to-medium planning/evidence effort after P0-P3; high risk if treated as paperwork rather
  than validation.
- Story 6.1: remains medium only after the prerequisites are genuinely supplied.

Schedule impact is indeterminate until accountable owners accept P0-P3 and provide target dates.

### Success Criteria

The course correction is complete when:

1. this proposal is explicitly approved;
2. the planning artifacts consistently show Story 6.1 as blocked by P0-P4;
3. each prerequisite has a repository-local authority, named accountable owner, accepted revision,
   reproducible evidence, and rollback pin;
4. source, architecture, central packages, and runner manifests agree on the supported baseline;
5. the G-4 runner proves persisted behavior and emits machine-checkable evidence;
6. dual-principal identity, safe denial, global watermark, and production authentication contracts
   are public, supported, and negatively tested;
7. P4 is owner-approved with no open critical exception;
8. the Story 6.1 spec passes the ready-for-development standard; and
9. only then, Story 6.1 transitions to `ready-for-dev`.

## 6. Change Navigation Checklist Status

### Section 1 — Trigger and Context

- [x] 1.1 Story 6.1 identified as the triggering story.
- [x] 1.2 Problem classified as a technical/ownership limitation exposed by specification readiness.
- [x] 1.3 Story, spec, readiness report, PRD, architecture, epics, UX, and tracking evidence loaded.

### Section 2 — Epic Impact

- [x] 2.1 Epic 6 cannot proceed with its current first-story sequencing.
- [x] 2.2 Epic 6 remains viable with explicit prerequisite enablement.
- [x] 2.3 Epics 7 and 8 remain in scope and inherit accepted shared evidence.
- [x] 2.4 No existing epic is obsolete; enablement is tracked as non-value prerequisite work.
- [x] 2.5 P0/P1 can start in parallel; P2, P3, P4, then Story 6.1 form the critical sequence.

### Section 3 — Artifact Conflict and Impact

- [x] 3.1 PRD remains valid without edits.
- [x] 3.2 Architecture invariants remain valid; version records may require owner-approved alignment.
- [x] 3.3 UX remains valid without edits.
- [x] 3.4 Story/spec, epics, sprint tracking, context, traceability, and evidence artifacts require
  coordinated correction.

### Section 4 — Path Forward

- [x] 4.1 Direct adjustment is viable and preserves the MVP.
- [x] 4.2 Rollback is not viable and does not resolve the platform gaps.
- [x] 4.3 MVP reduction would violate adopted safety and evidence contracts.
- [x] 4.4 Selected path: direct adjustment with external enablement; scope is Major.

### Section 5 — Proposal Components

- [x] 5.1 Issue and evidence summarized.
- [x] 5.2 Epic, story, artifact, technical, and schedule impacts documented.
- [x] 5.3 Selected path, alternatives, effort, risk, and sequencing documented.
- [x] 5.4 Before/after edits and owner-bound work packages specified.
- [x] 5.5 Handoff recipients and responsibilities specified.

### Section 6 — Final Review and Handoff

- [x] 6.1 Checklist reviewed for internal consistency.
- [x] 6.2 Proposal prepared in batch mode.
- [x] 6.3 Explicit user approval received from Jerome on 2026-07-17.
- [x] 6.4 Approved local changes applied to epics, story/spec, sprint status, Epic 6 context, and
  traceability; no sibling-repository mutation was authorized or performed.
- [x] 6.5 Major-scope recipients identified.

## 7. Approval and Routing

Current state: **Approved by Jerome on 2026-07-17; Major handoff routed.**

The approved local planning correction aligns Epic 6, Story 6.1, its specification, sprint status,
Epic 6 context, and the readiness matrix. It does not approve any P0-P3 capability, revision,
version, evidence result, owner acceptance, target date, architecture pin, package edit, or sibling
repository mutation.

## 8. Workflow Execution Log

- **2026-07-17 — analysis:** Story 6.1 failed the Sufficient ready-for-development criterion; the
  failure was traced to externally owned platform, Builds, identity, version, and evidence gaps.
- **2026-07-17 — proposal:** Major direct adjustment prepared in batch mode with work packages
  6.1-P0 through 6.1-P4 and a non-startable Story 6.1 state.
- **2026-07-17 — approval:** Jerome explicitly approved the complete proposal.
- **2026-07-17 — local application:** Epic 6 gate applicability, Story 6.1/spec blockers,
  sprint-status action items, Epic 6 context, and FR-2/FR-5/NFR evidence states were reconciled.
- **2026-07-17 — handoff:** routed to Product Manager and Solution Architect, with repository-local
  execution assigned to Builds/Platform (P0), EventStore/Builds/Architecture (P1),
  EventStore/Identity-Security (P2), Identity-Security/Projects (P3), and Product Owner/Solution
  Architect/Test Architect plus P0-P3 owners (P4). All target dates remain `uncommitted` pending
  owner acceptance.
