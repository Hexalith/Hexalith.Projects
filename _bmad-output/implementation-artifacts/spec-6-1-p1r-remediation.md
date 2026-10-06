---
title: '6.1-P1R Remediate Compatibility and Qualify Supported Rollback'
type: 'fix'
created: '2026-10-06'
status: 'handed-off'
route: 'dispatch'
work_package_id: '6.1-P1R-remediation'
artifact_kind: 'external-prerequisite-story-handoff'
source_action_status: 'open'
planning_approved: '2026-10-06'
planning_approved_by: 'Jerome'
owner_execution_status: 'pending-repository-local-scope'
implementation_repository: 'references/Hexalith.EventStore'
baseline_commit: '311aa85c8e81c7c0b19d5c0004ddf36ceafd5651'
observed_eventstore_revision: 'a7404a1d9edf3bf7d125851001c0699ff664abf0'
owners: [EventStore Owner, Builds Owner, Solution Architect, Test Architect]
supporting_owners: [Platform Owner, Identity-Security Owner, Product Owner, Release Engineering]
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-10-06.md'
  - '{project-root}/_bmad-output/implementation-artifacts/6-1-p1r-current-exact-baseline-candidate.md'
  - '{project-root}/_bmad-output/implementation-artifacts/6-1-p1r-compatibility-scenarios.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-6-1-p2-supply-supported-query-security-projection-capabilities.md'
---

# 6.1-P1R Remediation — Approved Planning Handoff

<frozen-after-approval reason="human-owned remediation intent — separate repository-local runtime scope and exact candidate decisions remain required">

## Intent

**Problem:** Attempt 21 completes the 3.110.0/3.70.1 investigation but retains seven
incompatible dispositions. Current P1R is unusable and Story 6.1 remains blocked.

**Approach:** Reproduce on the exact proposed repair source, retain effective
existing fixes, repair unsafe persisted-evidence handling, enforce supported
authority/watermark/API boundaries through existing P2/platform seams, and prepare
actual candidate/rollback package proof. A source build cannot retroactively fix
an immutable old published binary.

**Decision:** Jerome approved the complete October 6 sprint change proposal and
this separate planning handoff. EventStore and Builds owners establish their
repository-local runtime scope under AD-6 before implementation. Candidate,
rollback, release and pin decisions follow evidence and stay separately owned.

## Boundaries & Constraints

**Always:** Preserve the fixed acceptance JSON, original tuple decisions,
completed verification spec and every sealed packet/fixture. Bind new source,
Builds, package, runtime and physically loaded assemblies to separate new
evidence. Use root-declared repositories; local Debug/project references and
published Release/package lanes stay distinct. Assert persisted domain state,
sequence, event hashes, retained floor and Tenant isolation. Keep one writer;
fence and drain incompatible ingress. Reuse EventStore/P2 contracts and protection
validators. Keep legacy defaults and common compatible calls.

**Owner decisions:** Repository-local runtime scope; exact published candidate
and capable rollback coordinates; any pin/catalog/Stack transition; publication,
deployment and eventual prerequisite acceptance. Proposal approval is planning
authority, not a new exact-tuple decision.

**Never:** Rewrite event history, reseal old evidence, weaken assertions or old
validator protected-hash checks, substitute workload for actor authority or
sequence/time for global watermark, permit an unsupported old actor operation,
claim complete effect execution from null-input dispatch, discard later committed
writes via pre-upgrade restore, or mark P1R/Story 6.1 usable from this handoff.

## I/O & Edge-Case Matrix

| Scenario | Required treatment and proof |
| --- | --- |
| metadata-read | A writable rollback reader understands retained metadata. Missing legacy floor defaults to one; modern floor five is not silently discarded. |
| metadata-write | Restore floor five/sequence twelve with its covering snapshot, append sequence thirteen through the actual rollback writer, preserve floor, prior hashes and tenant isolation, and restart/replay. Otherwise fence mutation and roll forward under AD-17. |
| invalid-evidence | Reproduce floor zero, unreadable payload, protection-marker mismatch, unknown type and metadata version 987. Malformed/unsupported evidence rejects before domain application; valid legacy and supported protected data still replay. Infrastructure bookkeeping is counted separately. |
| query-wire | JSON and DataContract preserve authenticated original actor, workload, delegation, scopes and audience, or admission rejects the incapable protected route before access. No invented legacy authority. |
| projection-wire | Preserve exact positive global position 987 and cursor binding, or refuse authoritative use; unknown/zero is never authoritative. |
| mixed-api | Common legacy calls remain supported. Required newer methods prove successful persisted effects/fencing and stale/unauthorized rejection; incapable endpoints cannot execute or present false completion. Recovery null/false/true distinctions survive. |
| checkout | Bind the actual chosen repair revision and requalify its shared scope and actual packages; Reminder or later additions require independent coverage if selected. |

</frozen-after-approval>

## Code Map

Owner paths below are relative to `references/Hexalith.EventStore`:

- `src/Hexalith.EventStore.Server/Events/{AggregateMetadata,EventStreamReader,EventPersister}.cs` and aggregate actor hydration: validate persisted evidence and preserve retained-floor writes. Later source already contains floor checks/preservation; reproduce behavior before patching.
- Existing event/payload protection and contract-version validation seams: reject unsupported formats/versions without plaintext fallback or sensitive diagnostics.
- `src/Hexalith.EventStore.Contracts/Queries/QueryEnvelope.cs`, `.../Projections/ProjectionEventDto.cs`, status DTOs, cursor scope and actor contracts: preserve existing P2 implementation and prove transport/capability boundaries.
- Existing Server/Contracts/Client/routing/persisted tests: add meaningful regression and real restart/restore-plus-append coverage at the owning repository.
- New candidate evidence directory: reuse original ownership/binding patterns while leaving `evidence/6-1-p1r-3110/verification/` unchanged.
- Builds central catalog and Projects minimal gate/pins: later coordinated transition only after actual package proof and exact decisions.

## Tasks & Acceptance

- [ ] EventStore/Builds owners record repository-local scope, exact repair source and baseline inputs; preserve unrelated changes.
- [ ] Reproduce all seven scenario families against the chosen source and classify already-effective fixes, actual defects and incapable historical binaries.
- [ ] Repair remaining malformed metadata/envelope handling; prove safe rejection plus valid legacy/protected replay and unchanged committed history.
- [ ] Through P2/platform owners, enforce complete authenticated authority, exact watermarks, recovery semantics and supported actor methods; prove positive persisted effects and negative capability/fencing cases.
- [ ] Select and execute the supported rollback envelope: capable restore-plus-append/restart, or approved AD-17 mutation fence/forward recovery. Pre-upgrade restore cannot satisfy RPO 0 by discarding writes.
- [ ] Obtain owner-authorized candidate publication, then independently verify actual archives, signatures, assets/lock graphs and loaded assemblies in isolated package lanes. Select no version by guesswork.
- [ ] Collect complete new persisted, replay, tenant, cleanup and shared-preservation evidence. Keep missing/failed/skipped/zero-assertion/unavailable/incompatible required lanes nonpassing.
- [ ] Obtain exact EventStore/Builds/Solution/Test owner decisions and conformance for the candidate/rollback envelope, then stage and validate any later record/guard/pin transition.
- [ ] Return independently to P0/P2/P3/conformance/P4/spec readiness and READY. P1R alone cannot complete Story 6.1 or Story 8.11.

**Completion:** Every finding has an enforced and tested treatment in the approved
supported envelope; actual published inputs are proved; committed writes are
preserved; independent evidence and exact owner decisions exist. Current source
and old package proof remain distinct. No incompatible required lane can qualify.

## Handoff and Current State

Planning changes are applied and the open action is routed. EventStore Developer
owns reproduction/runtime work, Builds owns catalog/publication alignment,
Platform and Identity/Security through P2 own transport/capability admission,
Solution Architect owns the envelope/conformance, and Test Architect independently
validates evidence. Product Owner keeps the existing epic/story inventory;
Release Engineering owns later publication/drill execution scope.

Current P1R acceptance stays done for the October 1 tuple; usability stays false.
The original verification package stays done. New runtime execution, package
selection and qualification are pending the required owner-local steps.

## Historical Evidence Reproduction

Attempt 21 passed independent offline validation before the approved sprint
update. Its unchanged validator compares live protected files with captured
hashes, so it must reject the later changed sprint. The captured sprint is
recoverable from Projects 311aa85c8e81c7c0b19d5c0004ddf36ceafd5651 with SHA-256
bc0d564d8c07a9299c9e678dca2df1d98345ae870a798a6ec99a258bf11e6a93.
Preserve the packet and validate it only against its captured workspace; never
patch a protected-hash check to make current planning appear captured.

## Validation and Effort

Planning uses the exact staged sprint/companion guard and its focused negative
controls, plus frozen-block/acceptance/evidence preservation checks. Runtime
qualification uses owner-approved persisted/package lanes after reproduction.
Provisional effort is 5–10 engineering days plus publication and review lead time;
re-estimate after identifying existing effective fixes. Schedule is uncommitted.

## Planning Application Validation (2026-10-06)

The exact staged and active production-authority guards passed; the active Story
6.1 authority check passed; all 22 focused tests passed. Fixed acceptance, all
original action/development/readiness/qualification states, the frozen Story 6.1
block and 1,772 protected file hashes were preserved. The existing owner packet
was appended without rewriting its prior bytes. Git whitespace, local links,
frontmatter and line-ending checks passed. No repository revision or pin changed.

Attempt 21 independently validated with exit 0 before the approved sprint update.
Afterwards the unchanged command `python3 run_verification.py --validate attempt-21`
returns exit 2: `Protected inventory differs from independently checked workspace
hashes`. This records the expected live sprint mismatch; historical packet bytes
and captured hashes remain intact. New operational proof remains outstanding.
