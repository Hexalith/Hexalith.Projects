---
title: "Sprint Change Proposal: Deliver the Supported G-4 Persisted Runner and Evidence Tooling"
date: 2026-08-01
status: approved
approved: 2026-08-01
approved_by: Jerome
workflow: bmad-correct-course
review_mode: incremental
change_scope: moderate
trigger: "Deliver the supported G-4 persisted runner and machine-checkable evidence tooling required by Story 6.1."
affected_epic: 6
affected_story: "6.1"
primary_action: "6.1-P0"
proposed_action: "6.1-P1R"
---

# Sprint Change Proposal: Deliver the Supported G-4 Persisted Runner and Evidence Tooling

## 1. Issue Summary

Story 6.1 remains correctly blocked because the supported G-4 persisted runner and its accepted, consumer-restorable evidence tooling do not yet exist as usable platform capabilities.

The Builds owner implementation at revision `b529b66` contains substantial source progress: the tool/package project spine, module-manifest command contract, `hexalith-evidence validate`, contract tests, partial package qualification, and release-control improvements. It does not yet provide the supported runtime composition, real persisted fixture, complete live evidence, remotely restored consumer pin, or owner acceptance required to close 6.1-P0.

A second issue now affects the delivery baseline. The completed 6.1-P1 record normalized EventStore, Builds, Architecture, and the runner contract on version `3.70.1`. The observed Builds catalog at revision `b529b66` selects `3.86.0`, while `SupportedPlatformPins`, the module-manifest schema, the Architecture Spine, and the readiness record still select `3.70.1`. This is post-P1 dependency drift. The historical P1 completion remains valid, but the current candidate must be revalidated before P0 implementation or acceptance proceeds.

### Evidence supporting the correction

- Projects sprint tracking keeps 6.1-P0 open and Story 6.1 blocked.
- The Projects P0 handoff has been delivered, while the Builds owner story remains in progress.
- The current runtime prerequisite gate fails closed before platform composition; therefore `run` and `test` do not yet exercise a supported persisted platform.
- No accepted root tool manifest, published consumer pin, or final owner acceptance record is available to Projects.
- G-4 is an explicit Story 6.1 prerequisite and transitively affects downstream Epics 7 and 8.

## 2. Impact Analysis

### Epic and story impact

- Epic 6 remains viable; no replacement epic or new user-value story is required.
- Story 6.1 remains blocked by its prerequisite package. Its scope and acceptance criteria do not change.
- Epics 7 and 8 remain transitively dependent on a qualified G-4 capability.
- No completed story is invalidated. The completed 6.1-P1 record is preserved and followed by a new revalidation action, 6.1-P1R.

### Artifact impact

- **PRD:** no change. Product outcomes and NFR-11 continue to require real persisted-path proof.
- **UX specification:** no change. The correction concerns delivery infrastructure and evidence, not user interaction.
- **Architecture:** AD-25 and AD-30 remain authoritative. No invariant is relaxed. The exact EventStore version is updated only after 6.1-P1R owner approval, atomically across all binding artifacts.
- **Epics and Story 6.1:** add 6.1-P1R to the prerequisite chain and blocked-by declarations.
- **Projects P0 handoff and sprint ledger:** distinguish external source progress from supported-capability readiness.
- **Readiness matrix:** retain fail-closed `not-available` status until the published package and acceptance record are independently validated.
- **Builds P0 owner story:** rebaseline the remaining work around the observed candidate and explicit acceptance stages.

### Delivery impact

- Change classification: **Moderate**. The product and architecture remain stable, but coordinated planning changes are required across Projects, Builds, EventStore, architecture ownership, and test architecture.
- Remaining P0 effort: **XL**, replacing the stale `L` estimate. The increase reflects supported composition, real persisted qualification, publishing, remote restoration, rollback proof, and multi-owner acceptance.
- Schedule: **uncommitted** until 6.1-P1R selects and proves one exact dependency baseline.
- Risk: **High/Critical** while the persisted runner, consumer pin, and named Test Architect acceptance remain absent.

## 3. Recommended Approach

Use a direct adjustment with a dependency-baseline refresh:

1. Revalidate the exact EventStore/Builds/runner/Architecture baseline through 6.1-P1R.
2. Reconcile the P0 review findings against the current Builds head so already-corrected findings are closed with evidence and remaining findings stay explicit.
3. Implement the supported platform composition only against the accepted P1R baseline.
4. Qualify the real persisted and security-sensitive lanes.
5. Publish and remotely restore the exact independently consumable tools.
6. Produce a fail-closed, machine-checkable acceptance record and obtain named owner approvals.
7. Hand the accepted capability to P4 and rerun Story 6.1 readiness.

Rollback does not create the missing supported runner. Reducing the MVP would violate the architecture and NFR-11 evidence boundary. Neither alternative is recommended.

## 4. Detailed Change Proposals

### Change 1 — Rebaseline the Builds P0 owner story

**Artifact:** `references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md`

**Old planning state:**

```yaml
status: in-progress
implementation_dependencies: []
qualification_dependencies: [6.1-P1, G-6]
estimate: L
```

**New planning state:**

```yaml
status: in-progress
implementation_dependencies: [6.1-P1R]
qualification_dependencies: [6.1-P1R, G-6]
estimate: XL
delivery_state:
  contract_and_validator: implemented
  package_controls: partially-implemented
  supported_composition: blocked
  persisted_qualification: not-run
  published_consumer_pin: absent
  owner_acceptance: absent
observed_candidate:
  builds_revision: b529b66
  builds_eventstore_version: 3.86.0
  accepted_runner_and_architecture_version: 3.70.1
  disposition: owner-revalidation-required
```

Preserve all 15 acceptance criteria and all existing implementation. Replace the stale chronological remainder with these acceptance stages:

1. Complete 6.1-P1R baseline revalidation.
2. Reconcile every release-review finding against the current Builds revision, closing only findings backed by current evidence.
3. Implement the supported runtime composition and remove the unconditional prerequisite stop only after dependencies are accepted.
4. Qualify a real two-module fixture across persisted, restart, retry, two-instance, authenticated, and cross-tenant lanes.
5. Capture native reports and deterministic evidence through packaged tools.
6. Publish the exact prerelease, restore it remotely, and prove rollback.
7. Obtain Builds Owner, Platform Owner, and named Test Architect acceptance before P4 handoff.

### Change 2 — Add dependency revalidation action 6.1-P1R

**Artifacts:** sprint status, Epic 6 prerequisite table, Story 6.1, Story 6.1 specification, Projects P0 handoff, and the readiness matrix.

**Old planning state:**

```yaml
6.1-P1:
  status: done
  normalized_version: 3.70.1
6.1-P4:
  depends_on: [6.1-P0, 6.1-P1, 6.1-P2, 6.1-P3]
```

**New action:**

```yaml
- id: "6.1-P1R"
  action: "Revalidate the EventStore source, Builds catalog, Architecture Spine, and G-4 runner baseline after post-P1 dependency drift."
  repository_authority: "EventStore and Builds repositories; Architecture Spine through architecture-owner approval"
  owner: "EventStore Owner / Builds Owner / Solution Architect / Test Architect"
  status: open
  depends_on: ["6.1-P1"]
  unblocks: ["6.1-P0", "6.1-P4"]
  target_date: uncommitted
  observed_drift:
    builds_eventstore_version: "3.86.0"
    runner_manifest_version: "3.70.1"
    architecture_version: "3.70.1"
  expected_evidence: "Owner-approved exact source/package/runner/architecture pin, clean restore, required API compatibility, live-runner compatibility, and rollback record"
```

**New dependency chain:**

```yaml
6.1-P0:
  depends_on: [6.1-P1R]
6.1-P4:
  depends_on: [6.1-P0, 6.1-P1, 6.1-P1R, 6.1-P2, 6.1-P3]
story_6.1:
  blocked_by: [6.1-P0, 6.1-P1, 6.1-P1R, 6.1-P2, 6.1-P3, 6.1-P4]
```

6.1-P1R must not preselect `3.70.1`, `3.86.0`, or another version. Its accepted result must atomically align the EventStore revision, Builds package property, G-4 pins and schema, Architecture Spine, and readiness matrix.

### Change 3 — Separate implementation progress from supported capability and require machine-checkable acceptance

**Old planning state:**

- The Projects handoff records P0 as handed off and source action as open without decomposing capability readiness.
- Sprint tracking records only an open action.
- The readiness matrix correctly reports the tool as unavailable but does not distinguish an unaccepted source candidate.
- No single versioned machine-readable record joins package identity, live qualification evidence, rollback proof, and owner acceptance.

**New tracking state:**

```yaml
status: handed-off
source_action_status: open
owner_execution_status: in-progress
owner_observed_revision: b529b66
capability_status:
  manifest_contract: implemented-unaccepted
  evidence_validator: implemented-unaccepted
  persisted_runner: not-available
  published_tools: not-available
  owner_acceptance: not-available
blocked_by: [6.1-P1R, G-6, supported-composition, persisted-qualification, owner-acceptance]
```

Keep the sprint action canonically `open` and the readiness matrix `tool_status: not-available`. Add external execution and observed-candidate metadata without presenting source progress as a usable platform capability.

The Builds owner must emit a versioned P0 acceptance record validated fail-closed by the packaged `hexalith-evidence` tool. The record must contain:

- schema version and exact repository revision;
- accepted dependency source, package, runner, and architecture pins;
- package IDs, versions, cryptographic hashes, and feed identity;
- manifest and evidence-schema hashes;
- exact qualification commands and environment identity;
- persisted, restart, retry, two-instance, authenticated, and cross-tenant lane outcomes;
- native test-report and evidence-artifact hashes;
- cleanup and rollback results; and
- Builds Owner, Platform Owner, and named Test Architect approvals with timestamps.

Validation must return non-zero for missing fields, mismatched pins, missing artifacts, hash mismatches, failed required lanes, or absent approvals. P0 may become `done` only when the exact published package restores from the declared remote source and the acceptance record validates independently.

## 5. Implementation Handoff

### Ordered execution

1. **EventStore Owner, Builds Owner, Solution Architect, Test Architect:** execute 6.1-P1R and approve one exact baseline.
2. **Builds Owner:** reconcile review findings and implement the remaining supported composition and packaging work.
3. **Platform Owner and Test Architect:** qualify every required live lane and negative control using the published candidate.
4. **Builds Owner:** publish the exact prerelease, capture hashes and native reports, prove remote restoration and rollback, and emit the acceptance record.
5. **Builds Owner, Platform Owner, named Test Architect:** approve the machine-checkable record.
6. **Projects Product Owner/Developer:** update P0 and P4 readiness from accepted evidence and rerun Story 6.1 readiness. Story implementation must not begin before all gates pass.

### Completion conditions

- 6.1-P1R has an owner-approved exact baseline and rollback record.
- The supported G-4 command launches the required persisted platform composition rather than stopping at an unconditional prerequisite gate.
- The real two-module fixture passes all required persistence, restart, retry, concurrency, authentication, and tenant-isolation lanes.
- Package consumers restore the exact version from the declared remote source without project-reference fallback.
- Evidence and native reports are deterministic, tamper-evident, and validated fail-closed by the packaged tool.
- The acceptance record validates independently and contains all required approvals.
- The readiness matrix remains fail-closed until these conditions are met.

## Correct Course Checklist Outcome

- Trigger and evidence: confirmed.
- Epic impact: contained to the existing Epic 6 prerequisite chain, with downstream dependency risk recorded.
- Artifact conflicts: no PRD or UX conflict; Architecture invariants preserved; version-binding drift requires revalidation.
- Recommended path: direct adjustment with baseline refresh.
- Scope and routing: Moderate; coordinate Product Owner/Developer, EventStore Owner, Builds Owner, Solution Architect, Platform Owner, and Test Architect.
- Final proposal approval: approved by Jerome on 2026-08-01.
