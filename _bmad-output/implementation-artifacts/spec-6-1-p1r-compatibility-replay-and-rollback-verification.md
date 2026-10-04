---
title: '6.1-P1R Compatibility, replay, and rollback verification'
type: 'chore'
created: '2026-10-03'
status: 'ready-for-dev'
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

- [ ] `verification/{NuGet.Config,Directory.Build.props,Directory.Packages.props}` — isolate exact package versions and restore graphs; bind signed archive provenance for both versions.
- [ ] `verification/{host/Host,domain/Domain,probe/Probe}.csproj` and `Program.cs`/Counter type files — create version-selected package-only hosts/domain/probes; build each into separate scratch outputs. Exercise actual actor/domain replay and mixed-version calls.
- [ ] `verification/run_verification.py` — execute every supporting scenario, launch only owned resources, quiesce writers, capture committed state, dump/restore separate PostgreSQL databases, verify rollback/restart, and retain command/cleanup receipts.
- [ ] `verification/test_run_verification.py` — verify rejection of missing scenarios, mismatched artifacts, secret-bearing receipts and failed cleanup, plus cancellation/idempotent cleanup controls.
- [ ] `verification/{manifest.json,scenario-results.json,SHA256SUMS,README.md}` — retain actual results, package/source identities and limitations; append the report link to the Projects owner packet without changing decisions or readiness.

**Acceptance Criteria:**

- Given exact package inputs, when verification runs, then every case in the supporting matrix has actual assertions and bound artifacts; incompatibility remains distinct from a passed negative control.
- Given a quiesced owned database, when backup/restore and package downgrade execute, then persisted invariants and actor rehydration are measured independently of serialization smokes.
- Given the completed packet, when replayed or tampered, then its validator accepts complete matching evidence or rejects discrepancies; it never grants P1R or downstream acceptance.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

- From EventStore: `python3 _bmad-output/implementation-artifacts/evidence/6-1-p1r-3110/verify_public_packages.py` — exact published provenance preflight.
- From `verification/`: `python3 -m unittest test_run_verification.py` and `python3 run_verification.py --out <new-directory>` — nonzero for incompatible/unavailable required lanes; preserve receipts. `python3 run_verification.py --validate <result-directory>` checks complete hash-bound evidence without live infrastructure.
- From Projects: production-authority story/index guards and `git diff --check`; acceptance JSON, historical evidence and sprint statuses retain their original hashes.
