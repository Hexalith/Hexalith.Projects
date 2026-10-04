---
title: '6.1-P1R Compatibility, replay, and rollback verification'
type: 'chore'
created: '2026-10-03'
status: 'in-review'
route: 'dispatch'
work_package_id: '6.1-P1R-verification'
baseline_commit: 'cbcf54fa4d7a8c17bbfc3f9fb555ac0a85c179c0'
implementation_repository: 'references/Hexalith.EventStore'
observed_eventstore_revision: '2c58ffda41759e895ace4b9625c9bd931a217672'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/6-1-p1r-current-exact-baseline-candidate.md'
  - '{project-root}/_bmad-output/implementation-artifacts/6-1-p1r-compatibility-scenarios.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The accepted EventStore 3.110.0 / Builds 4.29.1 tuple has archive proof but lacks actor replay, metadata downgrade, mixed-version, and operational rollback evidence. P1R usability remains false.

**Approach:** Produce one EventStore-owned verification packet using actual published 3.110.0 and rollback 3.70.1 binaries, an isolated PostgreSQL/Dapr topology, and a separately identified current-source comparison. Exercise the finite supporting matrix; retain both passing and incompatible outcomes.

**Decision (2026-10-03):** User selected P1R verification before Story 6.1 implementation. This authorizes investigation and preparation, not a changed rollback guarantee or owner acceptance.

## Boundaries & Constraints

**Always:** Preserve the fixed acceptance JSON, tuples, prior evidence and downstream states. Bind tag/source/package/assembly hashes and literal commands. Use NuGet.org-only isolated package lanes, separate Debug/source comparison, Dapr 1.18.2 and the recorded PostgreSQL image. Restore only fresh invocation-owned databases. Retain no secrets or real payloads. One C# type per file.

**Never:** Substitute checkout libraries or OQ8 injected test assemblies for published binaries; infer compatibility from compilation; weaken an assertion; auto-flip usability; fix production runtime; publish, deploy, commit, push, update dependencies/gitlinks, initialize nested submodules, or stop shared resources. Backup-only containment cannot become an accepted rollback policy without a later named owner decision.

## I/O & Edge-Case Matrix

| State | Behavior |
| --- | --- |
| Compatible replay | Assert persisted state, sequence, event hashes and Tenant isolation |
| Dropped floor/authority/watermark, unreadable data or unsupported API | Record incompatible/rejected behavior; never report qualified |
| Missing artifact, zero tests, timeout, failed cleanup | Retain nonpassing receipts; exit nonzero |
| Completed report | Every scenario present; P1R/Story 6.1 states unchanged |

</frozen-after-approval>

## Code Map

All implementation paths are relative to `references/Hexalith.EventStore`.

- `_bmad-output/implementation-artifacts/evidence/6-1-p1r-3110/` — reuse archive verifier/record; create `verification/` with prior bytes intact. The 3109 probe covers only synthetic event/snapshot keys.
- `src/Hexalith.EventStore.Server/Events/{AggregateMetadata,EventPersister,EventStreamReader}.cs` — old writes discard `RetainedFloor`; missing required events reject. Existing same-version tests supply cases, not cross-package proof.
- `tests/Hexalith.EventStore.Server.LiveSidecar.Tests/Fixtures/Oq8PostgresqlFixture.cs` — reuse resource ownership/discovery patterns, not `PrepareShadowApplications` or `--additional-deps` injection.
- `samples/Hexalith.EventStore.Sample/` — reuse Counter semantics in minimal package fixtures. Current source additionally introduces Reminder APIs/registration; declare that compatibility boundary.

## Tasks & Acceptance

**Execution:**

- [x] `verification/{NuGet.Config,Directory.Build.props,Directory.Packages.props}` — isolate exact package versions and restore graphs; bind signed archive provenance for both versions.
- [x] `verification/{host/Host,domain/Domain,probe/Probe}.csproj` and `Program.cs`/Counter type files — create version-selected package-only hosts/domain/probes; build each into separate scratch outputs. Exercise actual actor/domain replay and mixed-version calls.
- [x] `verification/run_verification.py` — execute every supporting scenario, launch only owned resources, quiesce writers, capture committed state, dump/restore separate PostgreSQL databases, verify rollback/restart, and retain command/cleanup receipts.
- [x] `verification/test_run_verification.py` — verify rejection of missing scenarios, mismatched artifacts, secret-bearing receipts and failed cleanup, plus cancellation/idempotent cleanup controls.
- [x] `verification/{manifest.json,scenario-results.json,SHA256SUMS,README.md}` — retain actual results, package/source identities and limitations; append the report link to the Projects owner packet without changing decisions or readiness.

**Acceptance Criteria:**

- Given exact package inputs, when verification runs, then every case in the supporting matrix has actual assertions and bound artifacts; incompatibility remains distinct from a passed negative control.
- Given a quiesced owned database, when backup/restore and package downgrade execute, then persisted invariants and actor rehydration are measured independently of serialization smokes.
- Given the completed packet, when replayed or tampered, then its validator accepts complete matching evidence or rejects discrepancies; it never grants P1R or downstream acceptance.

## Implementation Notes

- Implemented the EventStore-owned [verification packet](../../references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-3110/verification/README.md) with isolated published 3.110.0/3.70.1 Release lanes and a separate exact-current-source Debug lane.
- Final `attempt-10` executes all 17 scenarios and 72 cases with 2,693 assertions and 1,626 literal command receipts. Seven compatibility dispositions remain incompatible; the live invocation exits 1, and offline completeness validation exits 0 while qualification, usability and owner acceptance remain false.
- Parent acceptance audit reproduced incomplete assembly coverage and startup-discovery cleanup gaps, then verified strict signed archive/DLL anchors, live/source identity bindings, minimal Docker diagnostics and retained failure receipts. All 60 runner controls pass.
- The rollback archive source discrepancy, scoped current-source equivalence, null-input selected-method dispatch limit and pre-upgrade backup containment limit are recorded explicitly. Production runtime and protected acceptance/sprint evidence retain their original bytes.
- Matrix audit: real actor/domain replay and restore cases assert state, sequence, event inventories and tenant isolation; incompatible/drop/unsupported controls retain their dispositions; missing/tampered artifacts, zero assertions, timeout, SIGINT and failed cleanup controls ran and passed; completed-report controls preserve every scenario and downstream state.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| BH-01 | medium | patch G01 | Current build() checks only HEAD and compiles the live worktree; source/config edits can retain the approved coordinate. Add committed-input closure checks for the already resolved source/build inputs. |
| BH-02 | medium | patch G02 | Domain ready/cursor endpoints execute SDK code but no runtime Domain identity is captured. Include assembly identities in the existing private readiness diagnostics and bind them to verified package/source outputs. |
| BH-03 | medium | patch G03 | Parent removed runtime, source-comparison and shared-comparison files separately from copied attempt-10 packets; each resealed packet validated. Require completed-lane artifacts and check their recorded observations/coordinates against receipts and cases. |
| BH-04 | medium | patch G04 | Replacing Host-lock.json with {} and resealing validated. Check retained lock graphs against assets identities and the verified archive content hashes. |
| BH-05 | medium | patch G05 | Parent relabeled all incompatible scenarios compatible and set exit_code=0; resealed validation accepted. Derive dispositions and exit status from the existing retained observations and cleanup result. |
| BH-06 | medium | patch G06 | Parent rebound all scenario/nested-case receipts to unrelated docker receipt 1; validation accepted. Require case-specific executed operation/fixture/outcome bindings, including per-case dump/restore receipts. |
| BH-07 | medium | patch G07 | Wire fixtures supply Scopes/Audience, but probe property projection and preserved comparison omit both. Record and compare their complete collections and legacy defaults. |
| BH-08 | medium | patch G08 | One exception encloses the whole container-removal loop, and failed cleanup is cached unconditionally. A first resource failure prevents later owned removal/retry; make cleanup attempts independent and retry unresolved failures. |
| BH-09 | medium | patch G09 | Runner scratch exists before the unprotected fixture-hash comprehension; a missing bound fixture bypasses finalization. Protect that construction so missing inputs retain a nonpassing packet and scratch cleanup. |
| BH-10 | medium | patch G10 | http() records only after successful responses/HTTPError; transport errors and cancellation escape without an attempted-request receipt. Record literal request/hash and failure outcome in those paths. |
| BH-11 | medium | patch G11 | Missing-event/uncovered cases accept any rejected zero-event actor result; ErrorMessage is retained only as a hash, and no explicit missing-event category is asserted. Bind the observed missing-event failure category from safe runtime evidence and reject unrelated rejection. |
| EC-01 | medium | patch G12 | start_nodes appends application and sidecar to active only after both launches. A sidecar launch failure leaves the application outside scenario cleanup; register each owned process immediately after launch. |
| EC-02 | medium | patch G13 | stop_nodes catches KeyboardInterrupt and raises RuntimeError; scenario sees no KeyboardInterrupt and can continue the matrix. Preserve cancellation after attempting owned cleanup and stop subsequent scenarios. |
| EC-03 | medium | patch G10 | Transport failure escapes http() before record(); the same missing attempted-request receipt is verified by BH-10. Group only after recording this separate verdict. |
| EC-04 | medium | patch G14 | Parent changed a retained replay event hash, recomputed inventory/projection/hash references and resealed; validation accepted a compatible replay. Validate before/after persisted invariants and bind inventories to the actual case receipts. |
| VG-01 | medium | patch G15 | Pre-verified broken-verification gap: deleting final process-group SIGKILL still passes all 60 controls while a SIGTERM-ignoring owned descendant survives. Add descendant termination coverage for timeout/cancellation and repeated cleanup callers. |
| VG-02 | medium | patch G06 | Other finding verified against validator/callers and parent receipt rebinding reproduction: existing command IDs need not evidence actor execution. This shares BH-06 receipt-binding root cause, not the persisted-invariant root cause. |

All surviving entries are direct corrections to existing fixture diagnostics, checks and cleanup paths; they add no published API or owner decision. The captured intent already requires exact input identity, bound actual operations, persisted invariants and retained nonpassing lifecycle evidence. No intent/spec loopback or deferral is required.

## Verification

- From EventStore: `python3 _bmad-output/implementation-artifacts/evidence/6-1-p1r-3110/verify_public_packages.py` — exact published provenance preflight.
- From `verification/`: `python3 -m unittest test_run_verification.py` and `python3 run_verification.py --out <new-directory>` — nonzero for incompatible/unavailable required lanes; preserve receipts. `python3 run_verification.py --validate <result-directory>` checks complete hash-bound evidence without live infrastructure.
- From Projects: production-authority story/index guards and `git diff --check`; acceptance JSON, historical evidence and sprint statuses retain their original hashes.

- 2026-10-04 parent checks: `python3 -W error::ResourceWarning -m unittest test_run_verification.py` — 60 tests passed; `python3 run_verification.py --validate attempt-10` — exit 0, complete hash-bound investigation, unqualified.
- `attempt-10/cleanup.json` — all owned processes stopped, all four owned containers removed, scratch deleted, no cleanup/discovery errors and identical shared-resource snapshots. Manifest preserved-before/after hashes match.
- Projects: `python3 tools/planning/validate_production_authority.py --story-id 6.1` and `--validate-index` — passed; `python3 -m unittest tests/tools/test_production_authority_guard.py` — 21 tests passed. Fixed acceptance JSON, historical acceptance and sprint status hashes are unchanged.
