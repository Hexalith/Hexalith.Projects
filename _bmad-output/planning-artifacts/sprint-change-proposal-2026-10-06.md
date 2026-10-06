---
title: "Sprint Change Proposal: Remediate 6.1-P1R Compatibility and Rollback"
date: 2026-10-06
status: approved
approved: 2026-10-06
approved_by: Jerome
workflow: bmad-correct-course
review_mode: batch
proposal_approval: approved
implementation_status: planning-applied-owner-implementation-pending
handoff_status: routed
change_scope: moderate
trigger: "Attempt 21 completes verification but retains seven incompatible dispositions; current P1R prerequisite usability remains false."
affected_epics: [6, 7, 8]
primary_gate: 6.1-P1R
supersedes: none
---

# Sprint Change Proposal: Remediate 6.1-P1R Compatibility and Rollback

## 1. Issue Summary

Story 6.1 cannot start because acceptance of the exact EventStore 3.110.0 / Builds
4.29.1 tuple did not establish a usable platform prerequisite. The completed
[P1R investigation](../../references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-3110/verification/README.md)
now supplies the missing measurements: attempt 21 executes all 17 scenarios and
72 cases, with 2,845 assertions and seven incompatible dispositions. A passing
negative control establishes that a defect was observed; it does not establish
compatibility. Independent offline validation on 2026-10-06 exits 0 with
`VALID: complete hash-bound investigation; P1R remains unqualified`.

The investigation is complete. Remediation requires a new implementation scope
because its [approved verification spec](../implementation-artifacts/spec-6-1-p1r-compatibility-replay-and-rollback-verification.md)
explicitly prohibits production runtime fixes. Do not reopen that completed
investigation or relabel its original package results.

### Observed incompatibilities

| Scenario | Measured problem | Required disposition |
| --- | --- | --- |
| `metadata-read` | The 3.70.1 typed metadata reader has no retained-floor member. Four JSON cases discard that information. | An old typed read is not proof of safe writing. A writable rollback target must understand and preserve retained metadata. |
| `metadata-write` | An actual 3.70.1 append advances the stream to sequence 13 but drops retained floor 5. | Block this writer on newer retained streams; qualify a capable rollback target before permitting writable rollback. |
| `invalid-evidence` | Both published versions accept floor 0, a `json+pdenc-v1` serialization marker on the fixture event, and metadata version 987 unsafely. Unreadable data and unknown event types reject. | Reproduce on the proposed repair source and make malformed or unsupported persisted evidence fail closed before domain application. |
| `query-wire` | Cross-version JSON/DataContract round trips through old types discard original actor, workload, delegation, scopes, or audience. | Protected delegated reads must use a complete supported transport or reject before reaching an incapable endpoint. |
| `projection-wire` | The old projection type drops positive persisted global position 987. | Unknown/zero remains unknown; an incapable transport cannot provide authoritative projection or cursor truth. |
| `mixed-api` | Old typed status reads lose recovery fields, old clients lack the watermark helper, and old actors reject fenced-command, trusted-effect, and retained-floor methods. | Keep common legacy calls supported and enforce the capability boundary for newer operations. Null-input dispatcher checks do not prove successful fenced/effect behavior. |
| `checkout` | The pinned source comparison reproduces the selected package's unsafe invalid-evidence handling. Shared-scope equivalence therefore preserves a defect. | Requalify repaired source and actual published packages; separately inventory APIs outside the measured shared scope. |

### Scope of the evidence

- Accepted selected tuple: EventStore `3.110.0` / `v3.110.0` /
  `27279fe6431925a6ea046c3f89af61487185c7de`; Builds `4.29.1` /
  `21ce044ab465ccb2adab58b3d66e394ffbecf3c2`.
- Existing rollback tuple: EventStore `3.70.1` / `v3.70.1` /
  `f13f9925fdca53efa2ab8c90d396ab106f91bb9c`; Builds
  `7af20f8bafbfe561df6f7705913a0800603090b5`. The rollback archive reports
  repository revision `650faf053a98ed1c03c048b8e0d2e4d281b095cf`; its source
  tree comparison matches the tag. Preserve both identities.
- Attempt 21 compares source `2c58ffda41759e895ace4b9625c9bd931a217672` and
  Builds `688eec9a4333245cc0ff7772115c769094471863`, using isolated source
  copies. It does not describe every later EventStore commit.
- At this proposal's inspection, Projects HEAD is
  `311aa85c8e81c7c0b19d5c0004ddf36ceafd5651`, the committed EventStore gitlink is
  `46d7b2eb61864c056a7323fecfa4246b41b1ec3b`, and its checkout is
  `a7404a1d9edf3bf7d125851001c0699ff664abf0`. Builds checkout is
  `ba4ca78c3868a4757cb92d912a54c8a237871b54`. These observations confer no new
  qualification. Later source already contains floor-validation and
  floor-preserving write code; reproduce the runtime failures before deciding
  which additional fixes it needs.
- Post-upgrade dump/restore and old-host hydration preserve floor 5, sequence
  12, events 5–12, their hashes, and tenant isolation with a sequence-9 covering
  snapshot. This passing restore does not repair the separate old-writer append.
- Pre-upgrade restore removes writes made after the backup. It proves
  containment mechanics, not an approved data-loss policy. NFR-4 RPO 0 remains.
- Attempt 21 stops all 1,100 owned processes, removes four owned containers and
  scratch, records no cleanup errors, and preserves all six shared observations.
  The report records 135 passing harness controls. This proposal independently
  reran offline packet validation, not the live matrix or those controls.

## 2. Impact Analysis

| Artifact or area | Impact |
| --- | --- |
| PRD | FR-1–FR-25 and NFR-1–NFR-11 remain binding. NFR-1, NFR-4, NFR-10, and NFR-11 directly prevent authority loss, unsafe writes, data-loss rollback, or treating negative controls as qualification. Index the approved decision in addendum §8; no main PRD scope change. |
| Epic 6 | Add bounded remediation under the existing P1R prerequisite and coordinate transport evidence with P2. Story 6.1 stays blocked. Story 6.7 consumes reversible read-routing proof, which is distinct from writable package downgrade. Existing P0/P2/P3/P4 completion gates remain. |
| Epic 7 | No new user story or resequencing. Durable workflows require capable actors and the AD-17 single-writer boundary. They cannot inherit unsupported old actor methods or weakened authority. |
| Epic 8 | Stories 8.7, 8.10 and release package 8.11-P3 consume the corrected published-family, resilience and rollback evidence. Story 8.11 still requires complete AD-30 evidence and dated Jerome + John decisions. |
| Architecture | Apply AD-6 repository ownership, AD-16 contract authority, AD-17 freeze-and-roll-forward on unsafe command rollback, AD-20 dual-principal authority and AD-30 truthful evidence. Add the measured P1R disposition to Stack and migration guidance. |
| UX | No screen, component, journey or accessibility revision. Existing truthful unavailable/denial/recovery semantics remain. An unsupported transport cannot present fabricated completion, authorization or freshness. |
| Platform code | EventStore owns persisted-evidence validation and metadata writes; EventStore/Platform own transport and actor capability enforcement. Reuse existing P2 contracts rather than duplicating them in Projects. |
| Builds and CI | A corrected source build alone cannot fix immutable published 3.110.0 or 3.70.1 binaries. New package coordinates, consumer pins and actual package-mode verification are required before adopting a replacement. |
| Planning consistency | Sprint fields still describe replay/rollback as unverified, while the latest packet measures both compatible and incompatible results. The epics P1R initial-state row still names 3.89.0. Add a dated current disposition and distinguish historical acceptance, completed investigation and unqualified operational use. |

The [fixed acceptance record](../implementation-artifacts/6-1-p1r-acceptance.json)
retains the four named October 1 accepts with their original limitations.
`6.1-P1R`, P0 Stage 1, DW-35 and DW-68 remain done for that decision. Current
usability remains false. Do not reopen those decisions to express remediation.
G-6 is a separate source/runtime qualification and does not qualify P1R packages;
use its own current result rather than transferring an old packet's acceptance.

## 3. Recommended Approach

Choose **direct adjustment with an EventStore-owned remediation follow-up**.
Retain the accepted tuple and historical evidence while preparing a corrected
candidate. This is a moderate prerequisite/backlog correction, not a new epic
or a reduction of the MVP.

1. Approve and define a separate `6.1-P1R-remediation` work package. Obtain
   repository-local scope from the EventStore and Builds owners under AD-6;
   Projects planning alone cannot authorize their runtime or pin changes.
2. Reproduce the unsafe persisted-evidence cases on the exact proposed repair
   source. Fix remaining defects and prove legacy replay, retained snapshots,
   safe errors and unchanged committed history. Keep existing effective fixes.
3. Define the supported selected/rollback capability matrix. Prefer a qualified
   rollback family that preserves retained metadata, authority, watermarks and
   required behavior. Until that target is qualified, fence incompatible old
   writers and apply AD-17 mutation freeze plus forward recovery. Read-routing
   rollback may proceed only through its own unchanged equivalence gate.
4. Complete capability-boundary proof through P2 and the existing platform
   admission seams. An operation needing lost fields or absent actor methods
   cannot be downgraded silently. Common compatible legacy operations remain.
5. Verify a candidate's real published packages and an explicitly selected
   rollback family in new evidence. Preserve attempt 21 and all earlier packets.
   No dependency version is selected by this proposal, and no archive is rebuilt
   under an existing published version.
6. Obtain the exact new tuple/rollback owner decisions after green evidence.
   Coordinate any later minimal-record, guard, catalog, architecture and consumer
   pin transition atomically. Keep the existing acceptance JSON v1 schema.
7. Return to P0/P2/P3, same-baseline architect sign-off, P4 and independent
   readiness. Fixing P1R alone cannot enable Story 6.1 or production release.

**Effort:** provisionally 5–10 engineering days for reproduction, remaining
runtime fixes, transport/rollback proof and planning alignment, plus publication
and owner-review lead time. Re-estimate after reproduction; existing source fixes
may reduce the work. **Risk:** high for retention and authority handling, medium
for planning/CI alignment. Timeline remains uncommitted until the replacement
package and rollback target are selected and available.

**Alternatives assessed:** Reverting the verification work would remove evidence
without repairing published behavior. Restoring a pre-upgrade backup would lose
later committed events and is not an approved RPO-0 recovery path. Reducing v1
scope or accepting authority/floor/watermark loss would conflict with the PRD and
is not recommended. The old binaries cannot become fully compatible through a
source-only repair; their measured incompatibilities remain historical facts.

## 4. Detailed Change Proposals

Jerome approved Changes 1–6 on 2026-10-06. The planning edits and bounded handoff below are applied; owner-local runtime, package publication and exact candidate/rollback decisions remain pending.

### Change 1 — Add a separate remediation work package

**Artifacts:** new
`_bmad-output/implementation-artifacts/spec-6-1-p1r-remediation.md`, the P1R
section of `epics.md`, and an open follow-up action in `sprint-status.yaml`.

**OLD:** The verification spec is `done`; it permits investigation and says
`Never: ... fix production runtime`. There is no bounded package for the seven
measured incompatibilities.

**NEW:** Add `6.1-P1R-remediation`, initially open, owned by EventStore with Builds,
Solution Architect and Test Architect review. Its intent is to repair unsafe
persisted-evidence handling, qualify supported transport/rollback behavior and
prepare a replacement candidate. Repository-local implementation approval is
required. The completed verification spec, accepted P1R action and protected
records keep their original status and bytes.

**Acceptance:** Each of the seven scenario IDs maps to a repair or an enforced,
explicit supported-capability boundary with a test and owner disposition. A
replacement baseline cannot be usable with an incompatible required lane.

**Rationale:** Investigation approval did not authorize runtime remediation.

### Change 2 — Make invalid persisted evidence fail closed

**Owner paths:** EventStore
`src/Hexalith.EventStore.Server/Events/{EventStreamReader,EventPersister,AggregateMetadata}.cs`,
the aggregate actor's hydration path and existing payload-protection/contract
validation seams; relevant Server and persisted-boundary tests.

**OLD:** The measured package and pinned-source actor lane accepts invalid floor
0, protected-format mismatch and unknown metadata version 987.

**NEW:** Validate retained metadata and envelope interpretation before domain
application or a new domain append. Reject invalid floors and unknown metadata
versions. A protected format must pass its actual supported protection provider;
plaintext with a protection marker must not fall through to JSON decoding. Keep
valid legacy floor-one defaults and supported protected-payload behavior.

**Acceptance:** All five existing invalid-evidence mutations run on fresh actors
and both tenants; malformed/unsupported inputs reject safely, with unchanged
domain state and event hashes. Valid legacy and supported protected inputs still
replay. Track permitted command-status/dead-letter bookkeeping separately. Prove
the remaining repair on source and then actual candidate packages.

**Rationale:** Rejecting persisted corruption is a runtime safety requirement;
matching unsafe behavior across source and package does not satisfy it.

### Change 3 — Qualify writable rollback and mixed-version boundaries

**Artifacts:** new remediation spec's rollback/capability section; existing P2
handoff and evidence; AD-17 guidance; later 8.11-P3 drill inputs.

**OLD:** The approved rollback coordinate is 3.70.1. Hydration succeeds for the
retained snapshot, but an old append drops floor 5; authority/watermark/status
members and newer actor methods are not preserved or supported.

**NEW:** State explicitly that 3.70.1 is not a demonstrated writable rollback
target for this newer stream/capability scope. Retain its historical coordinate.
Choose a capable rollback candidate only through the owners' subsequent exact
decision. Fence and drain writers before transition; retain one writer. If
complete replay and compliant writes cannot be proved, freeze mutation and roll
forward under AD-17. Do not promote pre-upgrade backup restore to a policy that
discards committed writes.

Use the pinned capability declaration at existing platform admission/routing
boundaries to refuse delegated reads, authoritative-watermark operations or
newer methods on incapable endpoints. Legacy defaults mean unknown, not actor
or watermark substitution. Preserve real authenticated actor/workload authority
and existing P2 implementations.

**Acceptance:** A proposed writable rollback preserves floor 5, sequence 12 and
covering snapshot during restore, then appends sequence 13 while preserving the
floor, prior event hashes and tenant isolation; restart/replay remains correct.
JSON and DataContract transports preserve all required dual-principal fields and
positive watermark 987, or the incapable route rejects before protected access.
Recovery null/false/true semantics remain distinct. Required fenced/trusted-effect
methods have successful persisted behavior and rejection of stale/unauthorized
calls; null-input dispatch alone is insufficient. An old unsupported endpoint
cannot produce an authorized effect or a false completion.

**Rationale:** Published old serializers and actor dispatchers cannot be repaired
by modifying a newer client's source. The permitted runtime route must enforce
the measured boundary.

### Change 4 — Qualify the replacement without rewriting the investigation

**Artifacts:** a fresh EventStore-owned candidate/rollback evidence directory,
package verifier/consumer inputs, P2 proof and Projects CI replay references.

**OLD:** Attempt 21 binds the original 3.110.0/3.70.1 inputs and source comparison,
with `qualified=false`; the fixed acceptance record accepts those coordinates
only with recorded limitations.

**NEW:** Keep the original fixture contract, validator and sealed packets intact.
Collect separate evidence for the repaired candidate and proposed rollback at
explicit immutable source/package/Builds coordinates. Reuse ownership and
evidence-validation patterns without changing old expected outcomes. Verify
NuGet.org archive provenance, signatures, resolved assets and physically loaded
assemblies. Local Debug/source proof and Release/package proof remain distinct.
List Reminder APIs/registration and other added capabilities outside the old
shared-scope comparison; qualify any that enter the new supported envelope.

**Acceptance:** Every required candidate lane passes real persisted assertions;
failed, missing, skipped, zero-assertion, unavailable or incompatible required
lanes remain nonpassing. Cleanup and shared-resource preservation are proved.
The original seven incompatible outcomes remain unchanged. Four exact-tuple
owner decisions and same-baseline conformance are required before a coordinated
selection/rollback transition; proposal approval cannot substitute for them.

**Rationale:** New evidence establishes a new supported baseline; it cannot make
the original binaries retroactively compatible.

### Change 5 — Reconcile current planning and restore explicit Story 6.1 criteria

**Artifacts:** `epics.md`, `epic-6-context.md`, `sprint-status.yaml`, current P1R
owner packet, Architecture Spine Stack/AD-17 note, and
`spec-6-1-list-and-open-projects-through-supported-authenticated-paths-6.md`.

**OLD:** Current sprint metadata says
`eventstore_metadata_rollback: unverified-RetainedFloor-shape-differs-from-3.70.1`,
`eventstore_actor_replay: unverified-3.110.0-to-3.70.1` and
`eventstore_checkout_compatibility: unverified`. Epic 6's P1R initial-state row
still records a pending 3.89.0 candidate. The latest Story 6.1 implementation spec
has only the general acceptance sentence "the full frozen scope and AD-32 fields
apply" plus a shadow-delta clause.

**NEW:** Add a dated current P1R disposition: October 1 tuple acceptance remains;
attempt 21 is complete and independently valid; ordinary and retained-snapshot
replay/restore pass; seven incompatible scenarios keep operational qualification
and usability false; remediation is open. Label the epics initial state as
historical and point current readers to this disposition. Append to dated owner
and architecture observations rather than rewriting earlier decisions.

Restore these explicit acceptance clauses in the non-frozen Story 6.1 section:

- Given a Chatbot Project User, list/open evaluates current Folder-read authority
  before pagination; rows, counts and scoped cursors reveal only the permitted
  set, and Tenant-role access never widens Chatbot authority.
- Given a Tenant-role caller without inspection authorization, list/open returns
  Safe Metadata and no Project name. With inspection authorization, authorized
  descriptive fields require one durable FR-21 event per request recording the
  inspected Project-set counts and field class, without descriptive payload.
- Given a denied, cross-Tenant or nonexistent target, the externally observable
  read follows the accepted indistinguishable safe-404 contract with no protected
  existence or metadata disclosure.
- Retain the existing AD-32, pre-activation, Archived, paging and shadow-equivalence
  requirements. These clauses become runtime tests only after the entry gate.

Record resolution of the existing acceptance-condensation deferred entry after
the approved text is applied; do not create a duplicate. Update measured P1R
status without changing `usable_as_prerequisite: false`, the fixed JSON or done
acceptance statuses. Any later qualification/usability change requires its own
validated evidence and exact named decisions. Do not conflate unguarded optional
status fields with implementation authorization.

**Rationale:** Makes the work and blockers reviewable and restores obligations
already present in the frozen Story 6.1 scope. No runtime/test edit while blocked.

### Change 6 — Index the decision; preserve PRD and UX scope

**Artifact:** PRD `addendum.md` §8, next unused evidence row (currently E-33).

**OLD:** E-32 indexes the G-6 course correction; no row indexes this remediation.

**NEW:** After approval, add a dated row linking this proposal, naming its
approver, implementation owners and pending implementation/qualification state.
Reference NFR-1/4/10/11, AD-6/17/20/30, P1R/P2 and 8.11-P3. Preserve every FR/NFR
and existing PRD/UX semantics. The row must not assert a new accepted tuple,
usable prerequisite or completed release.

**Rationale:** Maintains the product-to-platform audit trail without a scope cut.

## 5. Implementation Handoff

**Classification:** Moderate. The Product Owner reorganizes prerequisite work
within Epic 6; the Solution Architect reviews the supported envelope and rollback
contract. No epic is added, removed or renumbered.

| Recipient | Responsibility and deliverable |
| --- | --- |
| Product Owner / Projects maintainer | Apply approved planning edits, add the open remediation follow-up, preserve completed acceptance states, restore explicit Story 6.1 criteria and reconcile the existing deferred entry. |
| EventStore Owner / Developer | Establish repository-local scope; reproduce against the exact chosen source; repair remaining hydration/validation defects; prove metadata-preserving writes and actor behavior. |
| Builds Owner | Review candidate package alignment, catalog/tool consumption and provenance; coordinate later exact pins only after owner decisions. |
| Platform and Identity/Security Owners, P2 implementer | Prove capability admission, complete authority transport, persisted watermarks, recovery semantics and unsupported-route denial through existing seams. |
| Solution Architect | Approve supported migration/rollback envelope and later exact Stack rebinding; sign same-baseline conformance. |
| Test Architect | Independently validate source/package evidence, all seven dispositions, successful effects, restore-plus-append, tenant isolation, cleanup and historical preservation. |
| Release Engineering | Supply package publication and later 8.11-P3 rollback drill under their own approved execution scope. |

**Sequence:** proposal approval → owner-local remediation scopes → current-source
reproduction and repairs → candidate/capability/rollback proof → owner-authorized
publication → published-package proof → exact tuple/rollback decisions and atomic
pin/record alignment → P0/P2/P3/conformance/P4 → independent readiness. Publication,
deployment, commits, pushes and dependency/gitlink changes are not executed by
this proposal run.

**Completion criteria:** All seven findings have enforced and verified treatment
within the newly approved operational envelope; actual candidate packages are
bound to their source; writable rollback is proven or mutation is fenced under
the approved AD-17 forward-recovery rule; no committed event is discarded; exact
owner decisions and conformance exist; current usability cannot be inferred from
the historical acceptance alone. Story 6.1 remains blocked until its entire
existing entry chain passes. Story 8.11 remains the terminal release decision.

**Deferred outside this package:** broad unrelated gitlink provenance, the
verification-host null-body concern pending reproduction, independent P3 identity
findings and unrelated G-6 gaps stay with their existing owners/ledger entries.
They are not silently resolved or duplicated here. R01–R07 harness corrections
already completed by attempt 21 are retained.

## 6. Checklist and Validation Record

| Checklist item | Status | Disposition |
| --- | --- | --- |
| 1.1–1.3 Trigger, problem, evidence | [x] | Story 6.1/P1R; seven measured incompatible dispositions; attempt 21 independently validated. |
| 2.1–2.5 Epic impact, dependencies, order | [x] | Existing Epic 6 prerequisite follow-up; Epics 7–8 consume resulting capability/rollback evidence; no new epic or ordering change. |
| 3.1 PRD | [x] | Existing NFRs require the remediation; addendum E-33 applied; MVP unchanged. |
| 3.2 Architecture | [x] | AD-6/17/20/30 and Stack landings identified; no new runtime architecture required. |
| 3.3 UX | [N/A] | No screen, journey, component or accessibility change. |
| 3.4 Other artifacts | [x] | Runtime, P2, published evidence, CI, current status and Story 6.1 acceptance edits specified. |
| 4.1 Direct adjustment | [x] | Recommended; provisional 5–10 engineering days plus external lead time. |
| 4.2 Rollback completed work | [N/A] | Removing investigation evidence does not repair published behavior. |
| 4.3 MVP reduction | [N/A] | No approved requirement removed or weakened. |
| 4.4, 5.1–5.5 Recommendation and proposal | [x] | Evidence, before/after changes, responsibilities and success criteria recorded. |
| 6.1–6.2 Completeness and accuracy | [x] | Coordinates, statuses, seven findings and immutable historical boundaries reconciled. |
| 6.3 Explicit proposal approval | [x] | Jerome replied "approve" on 2026-10-06, approving Changes 1–6 and the handoff. |
| 6.4 Sprint update | [x] | Applied the open remediation follow-up and measured dispositions after exact candidate-index validation; original accepted and downstream statuses retained. |
| 6.5 Handoff | [x] | Routed through spec-6-1-p1r-remediation.md and the action ledger; owner execution remains pending repository-local scope and evidence. |

Commands independently executed before the approved planning application:

```text
EventStore verification directory:
PYTHONDONTWRITEBYTECODE=1 python3 run_verification.py --validate attempt-21
exit 0: VALID: complete hash-bound investigation; P1R remains unqualified

Projects root:
PYTHONDONTWRITEBYTECODE=1 python3 tools/planning/validate_production_authority.py --story-id 6.1
exit 0: PASS: Story 6.1 is within production authority

PYTHONDONTWRITEBYTECODE=1 python3 tools/planning/validate_production_authority.py --validate-index
exit 0: PASS: production-authority index is [6, 7, 8]
```

The exact staged guard validated the staged sprint and deferred-work files with
byte-identical copies of the fixed acceptance record and unchanged P0 artifact.
All 22 staged tests passed, including command-line Story mode, supplied companion
paths and premature remediation-status/inventory rejection. After application,
the active checks produced:

```text
Projects root:
PYTHONDONTWRITEBYTECODE=1 python3 tests/tools/test_production_authority_guard.py
exit 0: Ran 22 tests; OK

PYTHONDONTWRITEBYTECODE=1 python3 tools/planning/validate_production_authority.py --validate-index
exit 0: PASS: production-authority index is [6, 7, 8]

PYTHONDONTWRITEBYTECODE=1 python3 tools/planning/validate_production_authority.py --story-id 6.1
exit 0: PASS: Story 6.1 is within production authority

Git whitespace check:
git diff --check
exit 0: no output

EventStore verification directory, after the approved sprint edit:
PYTHONDONTWRITEBYTECODE=1 python3 run_verification.py --validate attempt-21
exit 2: INVALID: Protected inventory differs from independently checked workspace hashes
```

The final packet result is the expected historical-workspace mismatch: the sprint
changed by approval, while every packet byte remained unchanged. The validator
and captured hashes were preserved. Independently compared 1,772 protected file
hashes, all 24 original action states, the frozen Story 6.1 block, fixed acceptance,
readiness, qualification and development states. All matched; only the approved
open follow-up was added. The pre-existing owner-packet bytes were retained as a
prefix, six added local links resolved, and frontmatter, line endings and
whitespace passed. The EventStore working tree stayed clean; observed repository
revisions and gitlinks did not change through this application. Scheduling checks
do not establish P1R usability or readiness.

## 7. Approval and Execution Record

Jerome explicitly replied **approve** on 2026-10-06 after batch review of this
complete proposal. Approval covers Changes 1–6 and the §5 handoff. It does not
select a new candidate or rollback tuple, transfer the October 1 decisions,
authorize publication/deployment, or advance prerequisite/release readiness.

Applied planning deliverables: separate handed-off remediation spec and open
action; measured P1R disposition in sprint/epics/context/owner packet/Architecture
Spine; explicit non-frozen Story 6.1 criteria; resolution of the existing
acceptance-condensation ledger entry; PRD addendum E-33. The exact-inventory
scheduling guard adds only the approved open remediation item; a negative control
rejects premature completion, execution status and renamed inventory.

The fixed acceptance JSON, completed investigation spec, Story 6.1 frozen block,
all EventStore evidence/fixtures and gitlinks retain their prior bytes/coordinates.
P1R tuple acceptance, P0 Stage 1, DW-35 and DW-68 stay done; current P1R usability
stays false. Other action/development statuses and NOT_READY remain unchanged.

The packet validator also checks the live protected sprint hash. Applying these
approved planning edits intentionally changes that hash; the unmodified old
validator must reject this newer live workspace rather than silently accept it.
Attempt 21 retains its complete evidence and the successful validation recorded
against its captured workspace. That sprint is recoverable from Projects
`311aa85c8e81c7c0b19d5c0004ddf36ceafd5651`; it hashes to
`bc0d564d8c07a9299c9e678dca2df1d98345ae870a798a6ec99a258bf11e6a93`.
No old packet or fixture is resealed. New qualification must bind new inputs.

Handoff is routed to the §5 recipients through the new spec and sprint action.
The next implementation step is EventStore/Builds owner-local scope followed by
current-source reproduction, remaining fixes and separate package/rollback proof.
Correct-course planning and routing are complete; runtime qualification is open.
