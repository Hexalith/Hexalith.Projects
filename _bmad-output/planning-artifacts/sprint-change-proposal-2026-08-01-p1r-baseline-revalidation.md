---
title: "Sprint Change Proposal: Revalidate the Story 6.1-P1R Platform Baseline"
date: 2026-08-01
status: approved
approved: 2026-08-01
approved_by: Jerome
workflow: bmad-correct-course
review_mode: incremental
edit_review: approved
proposal_approval: approved
change_scope: moderate
trigger: "Revalidate the EventStore source, Builds catalog, Architecture Spine, and G-4 runner baseline after post-P1 dependency drift."
affected_epic: 6
affected_story: "6.1"
primary_action: "6.1-P1R"
---

# Sprint Change Proposal: Revalidate the Story 6.1-P1R Platform Baseline

## 1. Issue Summary

The historical Story 6.1-P1 normalization remains valid evidence for the
accepted `3.70.1` baseline. Subsequent dependency updates created a new current
state: the checked-out EventStore source and the Builds centralized package
catalog now select `3.88.0`, while the Architecture Spine and G-4 runner
contract still select `3.70.1`.

This is the dependency drift already routed to 6.1-P1R. It does not invalidate
P1, alter the PRD, or justify starting Story 6.1. It requires one exact,
owner-approved source/package/runner/architecture tuple before P0 may consume
the refreshed baseline.

### Evidence at proposal approval

| Surface | Observed state | Evidence |
|---|---|---|
| EventStore source | tag `v3.88.0`, revision `4843b492dff7c16a4bc74db67509263f969c78c6` | Clean `main` checkout at the exact tag |
| Builds package catalog | `HexalithEventStoreVersion=3.88.0` | Introduced by `0e51a2115581028c8d9ab9395a93dd186ee51071`; approval-time Builds revision `4132725d8bda647cc65880199679f047f7366048` |
| G-4 runner contract | `EventStoreVersion=3.70.1` | `SupportedPlatformPins` and current positive fixtures |
| Architecture Spine | `3.70.1` | Stack table and G-1 evidence text |
| Historical rollback baseline | `3.70.1` | EventStore revision `f13f9925fdca53efa2ab8c90d396ab106f91bb9c`; Builds commit `c074d0d` is catalog-only `3.70.1` provenance and still carries runner `3.70.0`; an exact atomic rollback Builds revision and executed proof remain pending |

The Builds structural catalog contract passed for 49 approved package
identities and three representative shared bindings. A focused diff of the seven
Story 6.1-dependent API files from `v3.70.1` to `v3.88.0` found six
byte-identical files and one additive change:
`QueryCursorScope.AddProjectionWatermark(long?)`.

Two live validation commands from the original Correct Course analysis remain
inconclusive rather than failed. They made no progress for more than five
minutes amid existing workspace MSBuild/NuGet contention and were cancelled:

```text
dotnet restore Hexalith.EventStore.slnx \
  -p:UseHexalithProjectReferences=false --force --no-cache --verbosity minimal

dotnet test test/Hexalith.Builds.Module.Tests/Hexalith.Builds.Module.Tests.csproj \
  --configuration Release --verbosity minimal
```

These original analysis attempts are distinct from the later implementation
qualification reruns recorded in the finite P1R ledger. Every required lane
must complete successfully with retained evidence in a clean validation
context before P1R can be accepted.

## 2. Impact Analysis

### Epic and story impact

- Epic 6 remains viable. No new user-value epic or story is required.
- Story 6.1 remains blocked by P0, P1R, P2, P3, and P4. Historical P1 stays
  complete and preserved.
- P1R continues to unblock P0 and P4 only after its exact tuple and evidence are
  accepted.
- Epics 7 and 8 are affected only transitively through their dependence on the
  qualified platform and persisted evidence path.
- No epic resequencing or priority change is required.

### Artifact impact

- **PRD:** no change. MVP scope and requirements remain achievable.
- **UX:** no change. The correction changes platform bindings and evidence, not
  user flows or interaction design.
- **Architecture Spine:** update the exact EventStore binding and G-1 evidence
  wording only after runner alignment and owner acceptance.
- **Builds runner and fixtures:** align the active contract atomically on
  `3.88.0`, while retaining an explicit stale-pin rejection case.
- **Qualification evidence:** create a finite P1R record with exact revisions,
  compatibility results, validation commands, rollback tuple, and named
  approvals.
- **Planning and handoff artifacts:** replace the stale `3.86.0` observation
  with the actual `3.88.0` source/catalog candidate without clearing any
  downstream blocker prematurely.

### Technical and delivery impact

- Change classification: **Moderate** because the correction is bounded but
  crosses EventStore, Builds, architecture ownership, test architecture, and
  Projects planning.
- Recommended effort: **Medium** for P1R itself.
- Risk: **Medium** after the additive API comparison, remaining elevated until
  restore, runner tests, exact-pin checks, and owner acceptance pass.
- Schedule: **uncommitted** until the validation blockers are cleared and the
  post-alignment Builds revision exists.

## 3. Recommended Approach

Use a direct adjustment within the existing 6.1-P1R action:

1. Record the actual `3.88.0` source/catalog candidate and the remaining
   `3.70.1` runner/architecture drift.
2. Align the G-4 runner contract and all semantically current fixtures on
   `3.88.0`.
3. Re-run package-mode restore, module tests, exact-pin acceptance, stale-pin
   rejection, the structural catalog contract, and the deterministic
   package-version audit in a clean validation context.
4. Create the finite P1R qualification record and bind the exact post-change
   Builds revision.
5. Obtain acceptance from the EventStore Owner, Builds Owner, Solution
   Architect, and Test Architect.
6. Update the Architecture Spine and dependent planning artifacts atomically.
7. Hand the accepted tuple to P0 without closing P0 or any later prerequisite.

A rollback is retained as an operational fallback, not chosen as the primary
course. Reverting to `3.70.1` would discard current dependency alignment and
would not deliver the missing P0 capability. An MVP review is unnecessary
because the product requirements and architecture invariants remain feasible.

## 4. Detailed Change Proposals

### Change 1 — Correct the 6.1-P1R observed baseline

**Artifact:** `_bmad-output/implementation-artifacts/sprint-status.yaml`

**Old:**

```yaml
observed_drift:
  builds_eventstore_version: "3.86.0"
  runner_manifest_version: "3.70.1"
  architecture_version: "3.70.1"
```

**New:**

```yaml
observed_drift:
  eventstore_source:
    version: "3.88.0"
    revision: "4843b492dff7c16a4bc74db67509263f969c78c6"
  builds_catalog:
    version: "3.88.0"
    introducing_revision: "0e51a2115581028c8d9ab9395a93dd186ee51071"
  runner_manifest_version: "3.70.1"
  architecture_version: "3.70.1"
candidate_baseline: "3.88.0"
decision: "pending runner/architecture alignment, clean validation, and named-owner acceptance"
rollback_baseline: "3.70.1"
```

P1R remains `open`. This records the current evidence without treating the
candidate as accepted.

### Change 2 — Rebind the Architecture Spine after acceptance

**Artifact:**
`_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md`

**Old Stack binding:** `3.70.1`, with rollback `3.67.3`.

**New Stack binding:** `3.88.0`, EventStore revision
`4843b492dff7c16a4bc74db67509263f969c78c6`, the exact accepted post-alignment
Builds revision, G-4 manifest `3.88.0`, and rollback baseline `3.70.1` at
EventStore `v3.70.1`. Builds commit `c074d0d` proves only the catalog pin; its
runner remains `3.70.0`, so the exact atomic rollback Builds revision and its
execution evidence remain pending.

**G-1 clarification:** replace the stale `3.70.1` evidence reference with
`3.88.0`, while stating explicitly that P1R revalidates the package/runner
baseline only. It does not select or establish the Durable Task engine or
Confirmation Artifact capability, and stale local binaries remain
inadmissible.

The accepted Builds revision and date must be concrete before this change is
applied.

### Change 3 — Align the G-4 runner contract and fixtures

**Primary artifact:**
`references/Hexalith.Builds/src/libraries/Hexalith.Builds.Tooling/Manifest/SupportedPlatformPins.cs`

```diff
- public const string EventStoreVersion = "3.70.1";
+ public const string EventStoreVersion = "3.88.0";
```

Apply the same change to current positive manifests, test builders, serialized
evidence, and validation expectations. Change
`test/fixtures/module/negative/tampered-platform-pin.json` from `3.67.3` to
`3.70.1` so the rollback tuple is explicitly proven stale while `3.88.0` is
active.

Do not rewrite the historical P1 normalization record. P1R runner alignment
does not complete P0 or prove the supported persisted composition.

### Change 4 — Add the finite P1R qualification record

**New artifact:**
`references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p1r-eventstore-source-architecture-runner-revalidation-record.md`

The record must contain:

- the exact EventStore source/tag/package pin;
- the catalog-introducing revision and exact post-alignment Builds revision;
- the accepted runner and Architecture Spine bindings;
- the seven-API compatibility comparison;
- commands, timestamps, and results for catalog, restore, runner, exact-pin,
  stale-pin, and applicable package evidence;
- rollback tuple and verification procedure;
- an explicit scope guard that P0, P2, P3, P4, and Story 6.1 remain open; and
- dated acceptance by the EventStore Owner, Builds Owner, Solution Architect,
  and Test Architect.

Placeholders or inconclusive commands cannot satisfy acceptance.

### Change 5 — Synchronize dependent planning and handoff state

Update the Builds P0 story, Projects P0 handoff, Story 6.1 specification,
traceability matrix, Epic 6 prerequisite table, and sprint ledger to replace
the stale `3.86.0` catalog observation with this state:

```yaml
eventstore_source_and_catalog_candidate: "3.88.0"
runner_manifest_version: "3.70.1"
architecture_version: "3.70.1"
p1r_decision: "pending alignment, validation, and acceptance"
```

Before acceptance:

- keep P1R open;
- keep P0 Stage 1 unchecked and P0 `in-progress-external`;
- keep Story 6.1 blocked; and
- preserve the historical P1 record.

After the P1R record passes and all four owners approve it:

- set P1R to `done` with exact accepted revisions and the evidence link;
- mark P0 Stage 1 complete and let P0 consume the accepted `3.88.0` tuple; and
- retain every remaining P0, P2, P3, P4, and Story 6.1 blocker until its own
  acceptance contract passes.

## 5. Implementation Handoff

### Scope and recipients

This is a **Moderate** correction routed to the Product Owner and Developer
agents, with cross-repository execution and acceptance by:

| Recipient | Responsibility |
|---|---|
| Builds Owner / Developer | Align runner source and fixtures, produce the exact Builds revision, and run module/package validation |
| EventStore Owner | Confirm the exact `v3.88.0` source/package tuple and package evidence |
| Solution Architect | Accept the Architecture Spine binding and G-1 clarification |
| Test Architect | Independently validate the evidence record, exact-pin behavior, and rollback proof |
| Product Owner | Accept planning-state propagation and preserve downstream blockers |

### Sequencing

1. Apply Change 1 to record the current candidate accurately.
2. Implement Change 3 in Builds and obtain its exact revision.
3. Complete Change 4 validation and owner acceptance.
4. Apply Change 2 and the post-acceptance phase of Change 5 atomically.
5. Hand the accepted tuple to P0 and rerun the applicable readiness checks.

### Success criteria

- EventStore source, published/clean packages, Builds catalog, G-4 runner, and
  Architecture Spine name one exact `3.88.0` tuple.
- All required validation commands pass in a clean context.
- The runner accepts `3.88.0` and rejects stale `3.70.1` while the new baseline
  is active.
- The rollback tuple is executable and independently verified.
- All four required P1R owners approve the finite record.
- P1R alone closes; P0, P2, P3, P4, and Story 6.1 remain governed by their own
  gates.

## 6. Checklist Disposition

| Section | Result |
|---|---|
| Trigger and context | Complete — 6.1-P1R technical dependency drift confirmed with exact evidence |
| Epic impact | Complete — Epic 6 remains viable; no new, removed, or resequenced epic |
| Artifact conflict | Complete — Architecture, runner/evidence, and dependent planning updates required; PRD/UX unaffected |
| Path evaluation | Complete — direct adjustment selected; rollback retained as fallback; MVP review unnecessary |
| Proposal components | Complete — five incrementally approved edits and handoff plan included |
| Final review and approval | Complete — approved by Jerome on 2026-08-01 |

## 7. Approval and Handoff Record

- **Approved by:** Jerome
- **Approval date:** 2026-08-01
- **Scope:** Moderate
- **Routed to:** Product Owner / Developer, with required EventStore Owner,
  Builds Owner, Solution Architect, and Test Architect participation
- **Implementation authority:** the five approved changes in this proposal;
  repository ownership and named acceptance gates remain binding
- **Next action:** record the current candidate accurately, align the Builds
  runner and fixtures, then produce and independently accept the finite P1R
  qualification record before updating the Architecture Spine or closing P1R
