---
title: "Sprint Change Proposal: Complete the Supported G-4 Persisted Runner and Evidence Tooling"
date: 2026-08-03
status: approved
approved: 2026-08-04
approved_by: Jerome
workflow: bmad-correct-course
review_mode: incremental-default
edit_review: approved
proposal_approval: approved
handoff_status: routed
implementation_status: pending-prerequisites-and-owner-execution
approved_changes: [1, 2, 3, 4, 5, 6, 7, 8]
change_scope: moderate
trigger: "Deliver the supported G-4 persisted runner and machine-checkable evidence tooling required by Story 6.1."
affected_epic: 6
affected_story: "6.1"
primary_action: "6.1-P0"
refines_approved_proposal: "_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03.md#change-5--correct-the-g-4-publication-and-consumer-baseline-under-p0"
supersedes: none
---

# Sprint Change Proposal: Complete the Supported G-4 Persisted Runner and Evidence Tooling

## 1. Issue Summary

The 6.1-P0 enablement package is authorized, owned by `Hexalith/Hexalith.Builds`,
and in progress, but it has not delivered the supported G-4 persisted runner or
its independently valid acceptance evidence. The current implementation provides
useful module-manifest, diagnostic, package, run-envelope, and AD-30 readiness-
matrix foundations. It does not yet compose or qualify the required live runtime.

The request is therefore not a new story or a new product feature. It is a
course correction that makes the existing P0 state truthful, fixes the remaining
delivery boundary, and routes the finite missing work without bypassing P1R,
G-6, or owner acceptance.

The approved proposal
`_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03.md` already
governs the upstream post-P1R dependency drift. This proposal refines its approved
Change 5 for P0 and does not replace or weaken that P1R authority.

### 1.1 Revalidated current state on 2026-08-03

| Surface | Observed state | P0 disposition |
|---|---|---|
| Projects action ledger | P0 is `open` / `in-progress-external`; owner revision and publication status are stale | Factual correction required after approval; no completion transition |
| Builds checkout | Clean `main` at `7bdbd293991985d150dfca62f77709e61152de76` | Current observation only, not an accepted delivery baseline |
| Published packages | NuGet V3 lists both tool IDs at `4.23.0`; Module CLI also has `4.20.0`, Evidence CLI has the `4.20.0`–`4.23.0` line | Published and remotely available, but not the accepted P0 consumer tuple |
| Published `v4.23.0` platform pin | EventStore `3.70.1`, Dapr runtime `1.18.0`, Dapr SDK `1.18.5`, FrontComposer `4.0.1` | Historical P1 tuple; not the pending P1R tuple |
| Published `v4.23.0` runtime path | `RuntimePrerequisiteGate.Check` always returns unavailable because G-6 is unapproved | Live composition cannot start |
| Current Builds runtime path | After manifest/profile validation, `run` and `test` still stop at the unconditional G-6 gate; the later path persists metadata and returns `HXR003` because no accepted descriptor ABI exists | No supported EventStore/Dapr/identity/FrontComposer/Aspire composition exists |
| Runner state | Run identity and metadata state types exist; the reachable live path does not use them | Not live-qualified; latent teardown, user-scoping, and failure-cleanup findings remain |
| Persisted fixture | Two-module manifest, profile metadata, and negative-control assets exist | No real persisted write/read/restart/idempotency/two-instance/authenticated execution has passed |
| Evidence CLI | `validate` handles `hexalith.readiness-evidence.v1`; module commands can emit `hexalith.module-run-evidence.v1` | `hexalith.g4-p0-acceptance.v1` validation is not implemented |
| Projects consumer | `.config/dotnet-tools.json`, `module/hexalith-projects.module.json`, and `evidence/g4/6.1-p0-acceptance.json` are absent | No accepted package pin, supported consumer composition, or P0 record |
| Acceptance owners | Builds Owner and Platform Owner are Jerome; Test Architect remains `unassigned-required-before-acceptance` | Implementation may continue; acceptance cannot |
| Prerequisites | 6.1-P1R is open; G-6 is a declared qualification dependency with no accepted affected-lane disposition | Live binding and P0 acceptance remain blocked |

### 1.2 Root cause

P0 intentionally shipped fail-closed contract tooling before the platform tuple
and G-6 support disposition were available. Planning and package publication then
advanced farther than the live implementation. This produced three misleading
edges that must be corrected:

1. `published_tools: not-available` is factually stale, while `published` still
   must not be confused with `supported` or `accepted`;
2. the public `run` and `test` commands exist, while their supported live
   composition does not; and
3. the readiness-matrix validator exists, while the separately required P0
   acceptance-record validator does not.

## 2. Impact Analysis

### 2.1 Epic and story impact

- Epic 6 remains viable without redefinition.
- 6.1-P0 remains the correct enablement action and remains `open` while work
  proceeds in Builds.
- P0 implementation may prepare tuple-independent components, but live binding
  and acceptance require an accepted 6.1-P1R tuple and an accepted affected-lane
  G-6 disposition.
- P0 continues to unblock only 6.1-P4. It does not make Story 6.1 ready for
  development.
- P2, P3, Solution Architect sign-off, P4, specification readiness, and an
  independent result exactly `READY` retain their current ordering.
- Epics 7 and 8 remain transitive consumers of G-4; no action is closed or
  reordered.
- No epic, user-value story, MVP goal, acceptance criterion, or priority is
  removed or added.

### 2.2 Artifact impact

| Artifact | Required disposition |
|---|---|
| PRD and addendum | No change; FR-2, FR-5, NFR-11, and MVP outcomes remain achievable |
| UX specification | No change; browser/CLI/MCP are runner profile classes, not a new product journey |
| Architecture Spine | AD-25 and AD-30 remain authoritative; update only the accepted post-P1R binding when P1R is accepted |
| Epic 6 and sprint ledger | Correct the stale observed revision and capability vocabulary while keeping P0 open |
| Projects P0 handoff | Rebaseline observed facts, split implemented foundations from missing live/acceptance capabilities, and retain all gates |
| Builds P0 owner story | Add a dated current-head revalidation and make the remaining stages executable against the accepted inputs |
| Builds source/tests/schemas | Implement supported composition, P0 acceptance schema/validator, real fixture execution, package-mode qualification, negative controls, and rollback |
| Projects consumer artifacts | Add exact tool and module manifests only after a qualifying package exists; add the accepted P0 record only after all lanes pass |
| Readiness traceability | Mark published-but-stale tools accurately; do not pass any G-4 row before the independent P0 record validates |

### 2.3 Delivery assessment

- **Classification:** Moderate. The work is an existing XL owner-repository
  package with bounded cross-repository planning and consumer handoff.
- **Remaining effort:** XL. The missing surface is real topology execution and
  acceptance-grade qualification, not a documentation-only patch.
- **Current risk:** High/critical evidence-chain risk. The runner must bind a
  moving platform tuple, own security-sensitive lifecycle, and prove persistence
  without false positives.
- **Schedule:** Uncommitted. A credible date starts only after P1R selects the
  exact tuple, G-6 has an affected-lane disposition, and a Test Architect is
  assigned.

## 3. Path Forward Evaluation

### Option 1 — Direct adjustment inside 6.1-P0: viable and recommended

Retain the Builds-owned packages, schemas, command names, and action identity.
Correct current-state reporting, implement the missing live composition and P0
record validator, publish one exact qualifying version, and complete independent
consumer qualification.

- Effort: XL remaining P0 work.
- Risk: High initially; reducible only through the machine gates below.
- Timeline effect: bounded to the existing prerequisite chain but uncommitted.

### Option 2 — Treat `4.23.0` or contract tests as P0 delivery: rejected

The package restores, but its live path is intentionally unavailable and it does
not validate `hexalith.g4-p0-acceptance.v1`. Treating publication, static fixture
validation, or hand-authored evidence as acceptance would violate AD-25, AD-30,
and P0 ACs 4, 5, 11, 13, and 15.

### Option 3 — Roll back or reduce MVP scope: rejected as a primary path

Rollback to the historical 3.70.1 tuple remains an executable containment lane,
not a delivery of G-4. Reducing MVP scope would not provide supported persisted
verification for Story 6.1 or its downstream consumers.

### Recommendation

Select **Option 1**. It preserves existing authority and public contracts while
making the smallest change that can produce machine-checkable, independently
accepted G-4 evidence.

## 4. Detailed Change Proposals

### Change 1 — Correct P0 planning truth without changing its gate

**Artifacts:**

- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/implementation-readiness-traceability-matrix.{md,yaml}`

Replace the stale observation with a capability model that distinguishes
availability, implementation, qualification, and acceptance:

```yaml
owner_observed_revision: "7bdbd293991985d150dfca62f77709e61152de76"
capability_status:
  manifest_contract: implemented-unaccepted
  module_run_evidence: implemented-unaccepted
  readiness_evidence_validator: implemented-unaccepted
  p0_acceptance_validator: not-implemented
  supported_composition: not-implemented
  persisted_qualification: not-run
  published_tools: published-stale-pin-not-supported
  published_consumer_pin: absent
  owner_acceptance: absent
```

Record `4.23.0` as the observed published version and its embedded EventStore
`3.70.1` pin. Keep `status: open`, `execution_status: in-progress-external`,
`depends_on: [6.1-P1R]`, the G-6 qualification dependency, and all downstream
blocks unchanged. Do not mark any G-4 traceability row passing.

### Change 2 — Rebaseline the Builds owner story around executable remaining stages

**Artifact:**
`references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md`

Append a dated 2026-08-03 revalidation section rather than rewriting historical
implementation and review evidence. It must record:

- current clean Builds revision `7bdbd293991985d150dfca62f77709e61152de76`;
- published `4.23.0` as remotely available but live-unavailable and bound to
  EventStore `3.70.1`;
- current source pin `3.88.0` as an unaccepted P1R candidate, not a P0-selected
  baseline;
- absent descriptor ABI/live composition, absent P0 acceptance validator,
  absent Projects consumer pin, absent accepted record, and unassigned Test
  Architect;
- explicit wait conditions for accepted P1R and the affected G-6 disposition;
  and
- the remaining stage order in Changes 3–7 below.

The story remains `in-progress`. Completed contract work stays historical and is
not unchecked merely because the live work is incomplete.

### Change 3 — Implement the runner-owned supported composition

**Owner repository:** `references/Hexalith.Builds`

After P1R and affected G-6 inputs are immutable and accepted:

1. bind `SupportedPlatformPins`, schemas, positive fixtures, run evidence, and
   negative stale-pin controls atomically to the accepted tuple;
2. replace the unconditional prerequisite stop with a machine-checked
   prerequisite decision that names the accepted P1R and G-6 dispositions;
3. define and version the Builds-owned descriptor/composition ABI needed to
   discover the declared module descriptors;
4. compose runner-owned EventStore, Dapr, identity/development credentials,
   FrontComposer, dynamic endpoints, health/readiness, telemetry, and Aspire
   lifecycle without consumer-owned platform code;
5. make every resource and retained state user-scoped and run-identity-scoped;
6. make `down` idempotent and exact-run bounded, and make failure/cancellation
   attempt cleanup while preserving the first causal failure and its evidence;
7. prohibit credentials, payloads, transcripts, prompts, raw environment dumps,
   or protected Tenant/resource details from retained state and evidence; and
8. remove the latent `HXR003` write-then-unavailable path or turn it into a
   reachable, tested success/failure lifecycle with no false success state.

An unavailable prerequisite remains exit `2`; it is never skipped or passed.

### Change 4 — Execute the real persisted two-module control

The qualifying fixture must use at least two real module descriptors and a
run-unique Tenant/resource namespace. The packaged runner, not an external
script or consumer fixture, must orchestrate these blocking lanes:

| Lane | Required machine observation |
|---|---|
| Persisted boundary | Authenticated write; exact event and projection/read state; expected sequence/revision |
| Restart | Stop, restart, and rehydrated read from persisted state |
| Idempotency | Retry produces the defined duplicate/idempotent result without an extra event |
| Two instance | A second supported instance observes the same authorized persisted result |
| Auth parity | Browser, CLI, and MCP profiles receive stable endpoint/identity handoff and execute real checks |
| Tenant isolation | Cross-Tenant access is denied with no payload leakage |
| State controls | Absent event, absent projection, stale namespace, and wrong sequence each fail |
| Prerequisite control | Unavailable platform input reports non-passing exit `2` |
| Cancellation/cleanup | Cancellation reports `130`, attempts safe cleanup, and retains non-passing evidence |

Fake/in-memory stores, handler-return assertions, static topology inspection,
zero/all-skipped tests, or hand-authored result JSON cannot satisfy a lane.

### Change 5 — Implement fail-closed P0 acceptance validation

Keep `hexalith-evidence validate <path>` as the public command. Extend it to
detect and validate both existing `hexalith.readiness-evidence.v1` YAML and new
`hexalith.g4-p0-acceptance.v1` JSON without weakening either schema.

The P0 schema and validator must require and cross-check:

- exact Projects, Builds, EventStore source/package, descriptor ABI, and
  platform revisions/pins;
- exact package IDs, versions, NuGet source identities, SHA-256 hashes, restored
  command versions, and clean-consumer marker;
- manifest/profile/fixture hashes and the run-unique namespace policy;
- exact `restore`, `run`, `test`, `down`, validation, negative-control, and
  rollback commands plus exit codes;
- native TRX/MTP report paths and hashes, nonzero executed-test counts, phase
  outcomes, persisted assertions, expected sequences, and run-evidence hashes;
- all required positive, negative, unavailable, cancellation, cleanup, and
  rollback lanes with no failed, skipped, or missing release-blocking result;
- metadata-only/privacy results;
- dated Builds Owner, Platform Owner, and named Test Architect dispositions that
  bind the same immutable record hash; and
- an executable rollback target and result.

Unknown fields, duplicate keys, placeholders, secrets, hash/schema/binding
mismatches, missing artifacts, dirty authoritative checkouts, stale package
pins, unavailable-as-passed, or self-approval fail closed with stable rule IDs
and evidence-validation exit `6`.

Add the JSON Schema, positive sample, and curated negative fixtures to Builds.
Contract tests must invoke the packed Evidence CLI, not only library methods.

### Change 6 — Qualify and publish one exact consumer version

At one accepted Builds revision:

1. build and test the source/Debug path from a clean checkout;
2. pack exactly `Hexalith.Builds.Module.Cli` and
   `Hexalith.Builds.Evidence.Cli` at the same version;
3. install and execute both packages from an isolated local feed for package-
   mode parity;
4. publish through the existing release workflow with exact-SHA, package-set,
   symbol-package, partial-publication recovery, and credential-scope controls;
5. query NuGet remotely, restore both tools into a fresh consumer with no
   authoritative `bin`/`obj`, and verify the installed command versions and
   package hashes;
6. rerun the full persisted and negative-control suite through the remotely
   restored tools; and
7. retain immutable reports, run evidence, inventory, and hashes.

Neither `4.23.0` nor a later version passes P0 merely because it is published.
The accepted version must contain the accepted P1R/G-6 composition and must pass
all package-mode lanes.

### Change 7 — Add Projects consumer artifacts and accept P0 independently

Only after Change 6 passes, add under Projects authority:

- `.config/dotnet-tools.json` with the exact accepted versions;
- `module/hexalith-projects.module.json` bound to the accepted schema, tuple,
  descriptors, dependencies, identifiers, and profiles; and
- `evidence/g4/6.1-p0-acceptance.json` generated from retained machine evidence.

From a clean Projects consumer, execute the Architecture-stable commands:

```text
dotnet tool restore
dotnet tool run hexalith-module run --manifest module/hexalith-projects.module.json
dotnet tool run hexalith-module test --manifest module/hexalith-projects.module.json --profile full
dotnet tool run hexalith-module down --manifest module/hexalith-projects.module.json
dotnet tool run hexalith-evidence validate evidence/g4/6.1-p0-acceptance.json
dotnet tool run hexalith-evidence validate _bmad-output/planning-artifacts/implementation-readiness-traceability-matrix.yaml
```

The named Test Architect validates the record independently; Builds Owner and
Platform Owner approve the same hash. Only then may P0 become `done` and unblock
P4. Story 6.1 and every other prerequisite remain blocked according to the
existing chain.

### Change 8 — Exercise containment and rollback

Before acceptance, keep the last accepted 3.70.1 tuple as platform containment
and remove/disable unaccepted Projects tool adoption if a rollout fails. Once a
P0 package version is accepted, that exact version becomes the first legitimate
G-4 package rollback pin.

Rollback must:

- invoke idempotent `down` for runner-owned resources;
- preserve failure and cleanup evidence;
- restore the prior accepted package/manifest tuple through an authorized
  forward change;
- rerun the prior accepted smoke and evidence-validation lanes; and
- never reset or rewrite a developer's working tree or persisted event history.

## 5. Implementation Handoff

### 5.1 Roles

| Recipient | Responsibility |
|---|---|
| Product Owner / planning maintainer | Apply approved factual corrections, retain the gate sequence, and prevent publication from being reported as acceptance |
| EventStore Owner + Solution Architect | Supply the accepted P1R source/package tuple and architecture binding |
| G-6 accountable owner | Supply the immutable affected-lane Dapr runtime/SDK support disposition |
| Builds Owner / Developer | Implement Changes 3–6, retain exact revisions and hashes, and publish the qualifying packages |
| Platform Owner | Accept runner-owned topology, identity/secrets, lifecycle, containment, and rollback |
| Named Test Architect | Independently execute clean source/package/remote-consumer lanes and validate the final record |
| Projects maintainer | Add the exact consumer artifacts only after qualifying packages exist; update status only after three-owner acceptance |

### 5.2 Required sequence

1. Approve this proposal and correct planning truth without changing P0 status.
2. Accept 6.1-P1R and the affected G-6 disposition at immutable coordinates.
3. Rebind the runner and implement supported composition.
4. Implement and contract-test the P0 acceptance schema and validator.
5. Execute source and local-package persisted qualification plus negative and
   rollback lanes.
6. Publish one exact version and repeat qualification from remote packages in a
   clean consumer.
7. Add Projects consumer manifests and produce the P0 record.
8. Obtain Builds Owner, Platform Owner, and named Test Architect acceptance of
   the same record hash.
9. Close only P0 and continue the existing P2/P3/sign-off/P4/readiness chain.

### 5.3 Completion boundary

P0 is complete only when the exact remote packages restore, the supported live
composition passes every required persisted/profile/control lane, the Projects
consumer artifacts exist, `hexalith-evidence` validates the P0 JSON and AD-30
matrix, rollback passes, and all three accountable owners approve the same
immutable evidence record.

No source edit, green unit test, package upload, manually written JSON, or owner
statement can substitute for that boundary.

## 6. Checklist Disposition

| ID | Status | Finding |
|---|---|---|
| 1.1 | Done | Trigger is the existing 6.1-P0 action under Story 6.1. |
| 1.2 | Done | Delivery gap: contracts/packages exist; supported composition and P0 acceptance validation do not. |
| 1.3 | Done | Current source paths, published versions, pins, missing artifacts, owner state, and gates are recorded. |
| 2.1 | Done | Epic 6 remains completable through the existing prerequisite chain. |
| 2.2 | N/A | No epic scope, AC, addition, removal, or redefinition is required. |
| 2.3 | Done | Epics 7 and 8 remain transitive G-4 consumers. |
| 2.4 | N/A | No epic is invalidated and no new epic is required. |
| 2.5 | Done | Existing sequence and priority remain correct. |
| 3.1 | Done | PRD goals and MVP remain achievable; no PRD edit. |
| 3.2 | Done | AD-25/AD-30 support the correction; accepted binding remains post-P1R. |
| 3.3 | N/A | No UI/UX change. |
| 3.4 | Action-needed | Builds runtime, P0 validator, schemas, fixtures, publication, consumer artifacts, tests, and planning truth require work. |
| 4.1 | Viable | Direct P0 adjustment; XL remaining effort, High initial risk. |
| 4.2 | Not viable as primary | Rollback is containment, not delivery. |
| 4.3 | Not viable | MVP reduction does not solve the supported verification gap. |
| 4.4 | Done | Option 1 is recommended for bounded authority-preserving delivery. |
| 5.1 | Done | Issue and current evidence summarized. |
| 5.2 | Done | Epic/artifact impacts and eight concrete changes documented. |
| 5.3 | Done | Recommendation and rejected alternatives documented. |
| 5.4 | Done | No MVP change; sequence, dependencies, gates, and completion boundary defined. |
| 5.5 | Done | Cross-repository roles assigned. |
| 6.1 | Done | All applicable checklist sections addressed. |
| 6.2 | Done | Draft reconciled against clean root/Builds observations and current package inventory. |
| 6.3 | Done | Jerome explicitly approved the complete proposal on 2026-08-04 after approving all eight edits incrementally. |
| 6.4 | N/A | No epic/story add, remove, or renumber operation is proposed. |
| 6.5 | Done | Moderate-scope handoff is routed to the named planning, architecture, Builds, platform, G-6, test, and Projects owners. |

## 7. Approval and Handoff Record

- **Approval:** Approved by Jerome on 2026-08-04 after incremental approval of
  all eight changes.
- **Review mode:** Incremental default.
- **Approved path:** Direct adjustment within 6.1-P0 under the accepted P1R/P0
  sequence.
- **Scope:** Moderate course correction; existing XL implementation package.
- **Primary handoff:** Product Owner / planning maintainer and Builds Owner /
  Developer, with EventStore, Solution Architect, G-6, Platform, Test Architect,
  and Projects-owner participation at the defined gates.
- **Implementation authority:** Route the eight changes to the named owners subject
  to repository boundaries and every prerequisite/acceptance gate.
- **Approval boundary:** This workflow approval does not itself perform code
  edits, dependency updates, source or submodule moves, publication, release,
  commit, push, or a P0 status transition. Those remain implementation work
  governed by repository ownership and the approved gates.
- **Next action:** Apply the approved factual planning corrections; accept P1R
  and the affected G-6 disposition; then execute Changes 3–8 in order.

### 7.1 Incremental review record

| Change | Decision | Decided by | Date | Effect |
|---|---|---|---|---|
| 1 — Correct P0 planning truth without changing its gate | Approved | Jerome | 2026-08-04 | Include in the final proposal; no implementation or status transition occurs before full proposal approval |
| 2 — Rebaseline the Builds owner story around executable remaining stages | Approved | Jerome | 2026-08-04 | Preserve historical evidence and append the current factual delivery boundary and wait conditions |
| 3 — Implement the runner-owned supported composition | Approved | Jerome | 2026-08-04 | Include the live composition work; binding and implementation remain gated on accepted P1R and affected G-6 inputs |
| 4 — Execute the real persisted two-module control | Approved | Jerome | 2026-08-04 | Require every persisted, restart, identity, isolation, failure, cancellation, and cleanup lane; prohibit simulated substitutes |
| 5 — Implement fail-closed P0 acceptance validation | Approved | Jerome | 2026-08-04 | Extend the public validator with strict P0 JSON dispatch, immutable evidence binding, stable diagnostics, and packed-command controls |
| 6 — Qualify and publish one exact consumer version | Approved | Jerome | 2026-08-04 | Require clean source, local-package, release, remote-restore, and remotely restored full qualification evidence |
| 7 — Add Projects consumer artifacts and accept P0 independently | Approved | Jerome | 2026-08-04 | Delay Projects adoption until a qualifying package exists and require clean commands plus three-owner acceptance of one record hash |
| 8 — Exercise containment and rollback | Approved | Jerome | 2026-08-04 | Require bounded cleanup, retained failure evidence, forward restoration, prior-lane verification, and a qualified package rollback pin |
