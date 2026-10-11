---
title: '6.1-P1R Enforce a measured support boundary'
type: 'bugfix'
created: '2026-10-10'
status: 'done'
baseline_commit: '55f4f23e99b235169b71dddcd8209504b7129dfd'
owner_repo_baseline_commit: 'f2b480b68a8bd5369b3cdf550340e0fadfb2332b'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/spec-6-1-p1r-remediation.md'
  - '{project-root}/references/Hexalith.EventStore/_bmad-output/specs/spec-6-1-p1r-remediation/qualification-contract.md'
---

<frozen-after-approval reason="human-owned P1R supported envelope">

## Intent

**Problem:** EventStore source repair is done, but the valid 3.119.0 published packet fails qualification. Immutable 3.70.1/3.110.0 binaries lose authority/watermark fields or lack newer methods; 3.119.0 also fails required stale-fence and logical-alias checks. P1R remains unusable.

**Approach:** Define one case-level support table from the sealed measurements. Keep compatible old calls supported and require an explicit, tested refusal before incapable protected routes execute. Make the qualification gate distinguish supported passes from justified refusals without hiding historical failures. Leave the published successor for an exact later owner selection.

**Decision (2026-10-11):** The user approved the measured support/refusal boundary and boundary-first execution. Every failed historical comparison remains visible; a refusal qualifies only with proof at the actual admission boundary. Candidate failures stay required and nonpassing. No published successor, package pin, or publication was selected.

## Boundaries & Constraints

**Always:** Preserve sealed packets, committed events, Tenant isolation, one writer, dual-principal authority, positive global watermark, nullable recovery status, package identities and all failed historical rows. With rollback=null, retain Projects AD-17 mutation freeze and forward recovery. Owner decisions and downstream gates remain separate.

**Never:** Hide failures, count typed reads as safe writes, accept opaque errors as fence denial, infer effects from null dispatch, accept backup data loss or source Debug output as package proof. Do not change fixed acceptance, pins, Stack or sprint.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Historical supported call | Passing old/new case | Keep required and measured | Missing receipt fails |
| Incapable old route | Lost dual principal, position 987, unsupported actor method or floor-losing writer | Refuse before protected access/effect/write; retain original failed case | Unproved refusal stays incompatible |
| Candidate obligation | Stale/forged proof, registered alias, restore/append/restart | Keep all candidate checks required | Opaque denial or missing effect fails |
| Policy tamper | Omitted case, substituted version, invented refusal or missing assertion | Reject policy/packet; retain original result | Qualification/usability remain false |

</frozen-after-approval>

## Code Map

EventStore paths below are relative to `references/Hexalith.EventStore/`.

- `tools/p1r_published_qualification.py` — `executed_case_inventory`, `required_lanes` and `evaluate` currently require all historical directions; preserve packet validation.
- `tools/p1r_published_executor.py`; `tools/p1r-published-consumers/host/QualificationCapabilities.cs` — existing measured case receipts and admission path; reuse before adding code.
- `src/Hexalith.EventStore.Contracts/Queries/QueryEnvelope.cs`, `src/Hexalith.EventStore.Contracts/Projections/ProjectionEventDto.cs`, `src/Hexalith.EventStore.Server/Actors/IAggregateActor.cs` — capability differences to classify, not duplicate.
- `_bmad-output/implementation-artifacts/evidence/6-1-p1r-31190-published-run/README.md` — sealed, nonqualifying baseline.
- `tools/tests/test_p1r_published_qualification.py`, `tools/tests/test_p1r_published_executor.py` — existing negative controls.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad-output/specs/spec-6-1-p1r-remediation/qualification-contract.md` — derive exact supported/refused case IDs from the sealed 3.119.0 packet; state the required admission proof for each refusal.
- [x] `tools/p1r_published_qualification.py` — load the fixed support table, validate its full case inventory and receipt bindings, require supported passes and proved refusals, and keep candidate failures blocking.
- [x] `tools/p1r_published_executor.py` and `tools/p1r-published-consumers/host/QualificationCapabilities.cs` — collect only the bounded old-route refusal observations missing from existing receipts; do not replay a candidate without selection.
- [x] `tools/tests/test_p1r_published_qualification.py` and `tools/tests/test_p1r_published_executor.py` — reject omitted/altered cases, unproved refusals, lost authority/watermarks, false completion and unsafe old writes.
- [x] `_bmad-output/implementation-artifacts/evidence/6-1-p1r-support-boundary/README.md` — retain the policy derivation and focused source/tooling receipts with no published qualification claim.

**Acceptance Criteria:**
- Given a historical failing direction, when a protected route targets an incapable endpoint, then it is refused before access, effect or write and its original failure remains visible.
- Given a compatible old call, when evaluated, then it remains required and must still pass measured checks.
- Given candidate failures, an unproved refusal or no exact successor selection, when the gate runs, then P1R usability and Story 6.1 remain blocked.

## Implementation Notes

- Bound 163 sealed cases to the original outcomes and receipt hashes: 123 remain supported, 39 require proved old-endpoint refusal, and one backup containment case remains incompatible.
- The 39 bounded admission calls and the live 3.110.0 Host stale-fence route refused before protected access. The retained tooling receipt hashes raw command output and binds final source and policy. This is no published successor or persisted-state qualification.
- Candidate stale-fence and logical-alias failures remain required and nonpassing; P1R and Story 6.1 usability remain blocked.

## Spec Change Log

- 2026-10-11: Implemented the approved measured support boundary in EventStore and retained one final tooling receipt.

## Review Triage Log

| Finding | Verdict / route | Evidence |
| --- | --- | --- |
| B1 | medium / patch | `satisfied` accepts a boundary-approved lane despite `execution=failed`; `lane_outcome` can fail solely on nonzero lane exit. Require a passed lane. |
| B2 | medium / patch | `3.110.0-3.70.1` query serialization writes through 3.110.0 first; its missing admission proof can be lost before 3.70.1. Select the first incapable endpoint. |
| B3 | low / reject | Raw `wire query` can bypass the fixture admission command, but it is outside `Executor.lane` and the current 3.119.0 candidate remains nonqualifying. Adding a second direct-CLI guard would expand this bounded tooling slice. |
| B4 | low / reject | Raw projection serialization is callable outside `Executor.lane`; no published envelope or production admission is claimed. Duplicating a guard in the direct Probe CLI adds scope without changing this blocked gate. |
| B5 | low / reject | A raw historical writer can still be invoked outside the managed lane, but Projects AD-17 freezes mutation and `Executor.lane` refuses it before dispatch. Retrofitting immutable old binaries is beyond this tooling observation. |
| B6 | low / reject | The old invalid-evidence route remains callable outside the managed lane; the bounded refusal measures the old endpoint before route entry and claims no persisted-state proof. A new production ingestion guard requires later owner scope. |
| B7 | low / reject | The direct old status HTTP route is outside this qualification admission path; no published successor or runtime switch was made. A second route guard would broaden the current fixture-only slice. |
| B8 | false / reject | The table marks 3.110.0 fenced/trusted/unauthorized effects supported, and the 3.70.1 Host lacks the capabilities route. The only 3.110.0 Host refusal requiring this guard is stale-fence, which is guarded. |
| B9 | low / reject | The Host 409 was run and its exact status/body recorded in README, but raw HTTP bytes are not retained. Adding a second large receipt for a smoke result brings little value while qualification stays blocked. |
| B10 | false / reject | `admit` returns before file, actor, or persisted-state access; its false effect flags and equal observation digests describe that no-dispatch tooling check. The contract and README explicitly deny persisted-state proof. |
| B11 | medium / patch | `startswith(3.70.1)` accepts `3.70.10`; bind the informational version to the exact endpoint and optional build metadata. |
| E1 | low / reject | A direct raw old command is outside the controlled executor envelope and AD-17 mutation freeze. This tooling change does not authorize or qualify direct old protected operations. |
| E2 | medium / patch | A nonzero lane exit can coexist with all passing case checks, so boundary satisfaction needs the lane execution status. Same root cause as B1. |
| E3 | low / reject | The checker binds source closure and selected archive/loaded assemblies, but does not independently attest a swapped Probe executable. No current candidate can qualify; adding binary attestation for this bounded tooling probe is disproportionate now. |
| E4 | low / reject | The standalone admission is the entry selected by `Executor.lane` for refused cases; it is not represented as a production route. The later published/operational qualification is still blocked. |
| V1 | low / reject | The real 39-case Probe commands ran and their output hashes are retained. Rebuilding published packages inside the focused Python suite adds a heavy external dependency for a one-time bounded evidence run. |
| V2 | low / reject | The live Host 3.110.0 route was exercised and recorded with exact HTTP status/body. Making the focused Python suite start a Dapr-capable web host adds more moving parts than this source/tooling slice needs. |

## Verification

**Commands:**
- `python3 -m unittest discover -s tools/tests -p 'test_p1r_published*.py'` from EventStore — focused controls pass.
- `python3 tools/p1r-published-qualification.py --help` from EventStore — verify the bounded policy/validation interface; no package invocation is implied.
- `git diff --check` in each changed owner repository — no whitespace errors.

**Results (2026-10-11):** 111 focused tests passed; Probe builds for 3.70.1/3.110.0 and Host builds for 3.110.0/3.119.0 passed with zero warnings or errors; all 39 bounded admissions and the live Host 3.110.0 HTTP 409 refusal passed. The final receipt verified 41 retained output hashes and all 39 policy case IDs. CLI help and both repository diff checks passed.

**Matrix audit:** Historical supported call: `testSupportedCandidateAndOldCallsStillNeedPassingChecks`; incapable old route: `testAdmissionReceiptBindsProbeAndSourceWitness`, `testUnsafeOldWriterRefusesBeforeAnyWriterOrActorCall`, and 39 admission probes; candidate obligation: `testPolicyBindsFullInventoryAndPreservesCandidateFailures`, `testHistoricalRefusalCanSatisfyLaneButCandidateFailureStillBlocks`; policy tamper: `testTamperedPolicyCannotOmitSubstituteOrInventRefusals`, `testRefusalNeedsExactAdmissionCommandAndNoProtectedEffect`. All named tests ran in the 111-test focused suite.

**Historical validator note:** Revalidating the sealed 3.119.0 packet against the changed live source returns exit 2 (`source/configuration inputs changed`). The sealed packet remains unchanged and nonqualifying; its captured validation belongs to its original source state.
