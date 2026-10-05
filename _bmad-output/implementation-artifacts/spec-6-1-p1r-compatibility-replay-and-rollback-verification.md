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
- Selected `attempt-17` executes all 17 scenarios and 72 cases with 2,822 assertions and 2,119 literal command receipts. Invocation/offline validation exit 2 for unrelated shared-container removal; seven incompatible dispositions remain. Owned cleanup and all operation bindings pass. Final verification stays in review pending a quiet shared-resource interval. Qualification, usability and owner acceptance remain false.
- Parent acceptance audit reproduced incomplete assembly coverage and startup-discovery cleanup gaps, then verified strict signed archive/DLL anchors, live/source identity bindings, minimal Docker diagnostics and retained failure receipts. That earlier review passed 60 runner controls; the third pass below passes 100.
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


Resumed review (2026-10-04): each finding below is a private verification correction; no acceptance decision or published API changes.

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| BH2-01 | medium | patch | Documented attempt-10 validation exits 2 for missing query_command_id; preserve prior bytes and capture a fresh packet with the current controls. |
| BH2-02 | medium | patch | Live EventStore HEAD differs from the fixed comparison coordinate; materialize the three pinned Git sources in owned scratch and build only those exact inputs. |
| BH2-03 | medium | patch | Inventory query_output and its digest can diverge from the retained literal diagnostic; bind the complete structural output to the executed query receipt. |
| BH2-04 | medium | patch | One before-query can satisfy both observations; require distinct queries bracketing actual actor operations and restore chronology. |
| BH2-05 | medium | patch | Identity URL suffixes can refer to unrelated ports; bind each response/actor/sequence endpoint to its application and owned sidecar launch. |
| BH2-06 | medium | patch | Inventory commands accept an unrelated Docker target; bind PostgreSQL operations to the runtime-owned PostgreSQL ID. |
| BH2-07 | medium | patch | copytree includes unbound source/build/output files; copy only the fixed fixture allowlist. |
| BH2-08 | medium | patch | KeyboardInterrupt during final process cleanup escapes container/scratch finalization; retain it while continuing independent cleanup actions. |
| BH2-09 | medium | patch | Nested checkout metadata/wire coverage lacks exact-case checks; require the complete unique nested matrix and positive assertion counts. |
| BH2-10 | medium | patch | Published output inventory receipts exist but their observed maps are unchecked; compare every Host/Domain/Probe map with signed DLL anchors. |
| EC2-01 | medium | patch; same cause as BH2-01 | Independently reproduced attempt-10 failure; fresh capture replaces the selected report without rewriting history. |
| EC2-02 | medium | patch | Six dispatcher cases omit their pre-started host receipts; include scoped prerequisite launch/identity receipts. |
| VG2-01 | medium | patch | Pre-verified control rejects duplicate receipt ID 2 before assembly validation; use a unique ID and assert the assembly mismatch failure. |
| VG2-02 | medium | patch | Pre-verified wire validator removal leaves all 60 controls green; add valid wire fixtures plus hash/field/disposition mutations. |
| VG2-03 | medium | patch; same cause as BH2-01 | Other finding verified by the actual exit-2 command; the selected packet and README are stale. |

Previous G01-G14 hardening is present in the checked-in runner; this pass repairs the remaining receipt bindings and proves the controls against fresh evidence. G15 still requires real surviving-descendant timeout/cancellation/repeated-cleanup controls.


Fresh-capture corrections (2026-10-04):

- `medium`, patch: Attempt 11 executed all 17 scenarios, 72 cases, 2,782 assertions and 2,122 receipts. Its snapshot observations are exactly sequence 9 (the published interval-10 behavior), whereas the new validator incorrectly expected 10. Corrected the literal fixture expectation to 9 while preserving every replay/state assertion. All operation/source/wire/inventory bindings then passed an independent direct validator check.
- `medium`, patch: Docker discovery returned short IDs while cidfiles recorded full IDs; cleanup removed each short identity then falsely treated the full alias's lowercase no-such-object response as failure. Request full IDs, recognize the actual missing-object diagnostic, and prove independent removal/retry behavior with a focused control. Attempt 11 is retained unchanged as nonpassing evidence; its real owned containers were removed and scratch was deleted.
- Attempt 11 also records external shared-resource changes and fixture-control edits made during that diagnostic capture. It grants no preservation or completeness acceptance. A new invocation must bind the final unchanged fixtures and its own shared-resource baseline.
- G15 is resolved with real SIGTERM-ignoring descendants for timeout, cancellation and repeated cleanup after parent exit. The controls redirect descendant streams so a naturally finishing child cannot hide a missing kill; they tolerate the observed /proc removal race. Removing only the final group kill causes all three controls to fail. Removing HTTP identity validation also makes the repaired unique-receipt control fail; removing the wire validator breaks its positive-path control.

- Attempt 12 was deliberately cancelled when the new cleanup-retry control exposed an inaccurate absent-container response in its Docker stub. Corrected the stub, then ran all 77 controls successfully before starting attempt 13. Attempt 12 retains cancellation and fixture-drift receipts; owned processes/containers, scratch cleanup and shared-resource preservation all passed.

- Attempt 13 completed all 17 scenarios, 72 cases, 2,782 assertions and 2,120 receipts; all bound operation/source/wire/persisted-invariant checks passed. Owned process/container/scratch cleanup passed without errors. Strict validation rejected the disappearing unrelated shared container `8fff54bb833de009698ae4c455ee4811b15a730b7e35ad16be9b1c657dc2fd12`; no other shared identity changed, and no owned removal receipt targets it. Preserve this packet as nonpassing evidence and rerun against a quiet baseline.



Resumed review (2026-10-04, third pass):

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| BH3-01 | medium | patch | Cleanup booleans accept failed Docker removals after resealing; bind ownership/removal and final label-discovery receipts. |
| BH3-02 | medium | patch | Successful fresh createdb receipts are not required before persisted database operations; bind unique creation before first use. |
| BH3-03 | medium | patch | Host/Domain launch failures and lifetimes do not constrain identity/actor requests; require successful applications and sidecars bracketing requests. |
| BH3-04 | medium | patch | Writer finish timestamps can follow pg_dump without rejection; require all preceding writers quiesced before backup/restore. |
| BH3-05 | medium | patch | Status URLs can target unrelated hosts; bind selected/rollback endpoints to their application identities and lifetimes. |
| BH3-06 | medium | patch | Cursor query URLs can target unrelated consumers; bind mint/consume requests to their actual Domain endpoints. |
| BH3-07 | medium | patch | Safe request observations can drift from request hashes/scope; retain a safe observation commitment and validate exact cursor fixture scope. |
| BH3-08 | medium | patch | Failed adjacent public-package verifier receipts do not reject passed provenance; require the successful preflight receipt. |
| BH3-09 | medium | patch | Common query routing/payload/authority/paging fields are omitted from typed observations and compatibility checks; record and compare all supplied common fields. |
| BH3-10 | medium | patch | Common projection event fields can drift while the watermark passes; record and compare the full common event fixture. |
| EC3-01 | medium | patch | Interrupted binary commands retain raw dump stdout; apply digest/length-only retention on timeout and cancellation. |
| EC3-02 | medium | patch | Cancellation between child creation and registration leaks an untracked process and receipt; defer cancellation through launch/registration and retain failed launch attempts. |
| EC3-03 | medium | patch | Termination failures during timeout/cancellation erase command receipts; retain attempted operations before independent cleanup retries. |
| EC3-04 | medium | patch | Killed archive verification children can leave default temporary directories outside owned scratch; set child TMPDIR to owned scratch. |
| EC3-05 | medium | patch | Released ephemeral sockets permit duplicate six-port allocation; reserve distinct sockets together before launch. |
| EC3-06 | medium | patch | Exceptions reading HTTPError bodies escape receipt capture; place response-body reading inside the protected request boundary. |
| VG3-01 | medium | patch | Pre-verified: deleting the restore inventory invariant leaves all 77 controls green; add resealed restored/post-replay event mutation controls for both restore scenarios. |

All findings correct demonstrated private verification paths, with no public surface or owner decision. Temporary-copy reproductions preserved shared fixtures. Historical captures retain their original bytes; the corrected fixtures require a fresh capture.



Third-pass live validation correction (2026-10-04): attempt 16 completed all 17 scenarios/72 cases and 2,822 assertions, with successful owned cleanup and unchanged shared resources. Its offline validator rejected the status case because setup application/identity receipts were outside its case bindings. BH3-05 now includes those scoped prerequisites; a temporary diagnostic copy verifies the remaining new bindings without editing the original packet. A focused omission control covers this boundary. The original attempt 16 remains unchanged and nonpassing; final verification requires a fresh capture.

## Verification

- From EventStore: `python3 _bmad-output/implementation-artifacts/evidence/6-1-p1r-3110/verify_public_packages.py` — exact published provenance preflight.
- From `verification/`: `python3 -m unittest test_run_verification.py` and `python3 run_verification.py --out <new-directory>` — nonzero for incompatible/unavailable required lanes; preserve receipts. `python3 run_verification.py --validate <result-directory>` checks complete hash-bound evidence without live infrastructure.
- From Projects: production-authority story/index guards and `git diff --check`; acceptance JSON, historical evidence and sprint statuses retain their original hashes.

- 2026-10-04 parent checks: `python3 -W error::ResourceWarning -m unittest test_run_verification.py` — 60 tests passed; `python3 run_verification.py --validate attempt-10` — exit 0, complete hash-bound investigation, unqualified.
- `attempt-10/cleanup.json` — all owned processes stopped, all four owned containers removed, scratch deleted, no cleanup/discovery errors and identical shared-resource snapshots. Manifest preserved-before/after hashes match.
- Projects: `python3 tools/planning/validate_production_authority.py --story-id 6.1` and `--validate-index` — passed; `python3 -m unittest tests/tools/test_production_authority_guard.py` — 21 tests passed. Fixed acceptance JSON, historical acceptance and sprint status hashes are unchanged.


### Resumed verification result (2026-10-04)

- EventStore: `python3 _bmad-output/implementation-artifacts/evidence/6-1-p1r-3110/verify_public_packages.py` — exit 0; all 14 public archives match hashes/source/release assets/ZIP payloads/signatures/manifest. Each fresh invocation also executes that preflight.
- Verification: `python3 -W error::ResourceWarning -m unittest test_run_verification.py` — 77 tests pass. Real timeout/cancellation/repeated-cleanup descendant controls reject removal of the final group kill. Identity and wire mutation checks expose removal of their validators.
- Verification: `python3 run_verification.py --out attempt-14` — all 17 scenarios/72 cases execute, 2,782 assertions and 2,114 receipts; seven incompatible dispositions. Exit 2: `RETAINED NONPASSING: Shared resource drift`.
- Verification: `python3 run_verification.py --validate attempt-14` — exit 2: `INVALID: Shared resource drift`. New unrelated shared ID: `c1c8c0d4f857486f86eb7d863bcb86b8b0930b5076429ec336e0307b11260150`; every pre-existing shared identity is unchanged. Owned process/container/scratch cleanup passes without errors. No owned removal receipt targets this new container.
- Attempt 13 independently retained an unrelated shared-container removal; another full retry did not settle the preservation gate. Do not stop shared resources or relax the guard. The explicit build workflow requires a halt when verification cannot pass; resume the same final fixtures during a quiet Docker/Aspire interval.
- Projects authority story/index guards pass, and all 21 focused authority controls pass. Fixed acceptance JSON, historical acceptance and sprint-status hashes match the initial values; 408 tracked historical invocation files remain unchanged.

### Third-pass verification result (2026-10-04)

- `python3 -W error::ResourceWarning -m unittest test_run_verification.py` — 100 controls pass. All 17 review findings were patched in the private runner, typed probe and controls.
- `python3 run_verification.py --out attempt-17` — all 17 scenarios/72 cases execute, 2,822 assertions and 2,119 receipts; exit 2 retains shared-resource drift and seven incompatible dispositions.
- `python3 run_verification.py --validate attempt-17` — exit 2, `INVALID: Shared resource drift`. Owned process/container/scratch cleanup passes without errors. Unrelated shared container `d7de6a5cd37701ec145fff56694b5e51d4d5296764a1d4eec7abfe3e060eb49d` disappeared; no owned removal targets it. All other binding checks pass before the shared-resource gate. P1R remains unqualified.
- Attempt 15 independently completed the preceding fixtures with identical shared snapshots. Its packet and all earlier invocations remain unchanged; the hardened validator requires the fresh selected capture.
- Independent in-memory mutation verification confirms all four new restore controls fail when only the invariant is disabled.
- Projects production-authority story/index guards and all 21 authority controls pass. Acceptance JSON, archived acceptance, sprint status, prior archive evidence and historical invocation files retain their original hashes.
