---
title: '6.1-P1R Repair supported qualification fixtures and isolate execution'
type: 'bugfix'
created: '2026-10-14'
status: 'done'
route: 'dispatch'
baseline_commit: '532c0fb35054a44beb9725a0f795b41c91b46d5f'
owner_repo_baseline_commit: '21b34c01ec919769cfba9cfe0d47e50886b57ded'
review_loop_iteration: 0
context:
  - '/home/administrator/projects/hexalith/projects/references/Hexalith.EventStore/AGENTS.md'
  - '/home/administrator/projects/hexalith/projects/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
---

<frozen-after-approval reason="user explicitly authorized this continuation, implementation, review and verification">

## Intent

**Problem:** Final 3.121.0 qualification retains six required supported cases with sixteen failed assertions and strict cleanup failure from unrelated Docker activity. Story 6.1 is blocked.

**Approach:** Determine the refusal's cause from persisted evidence, repair the smallest contract-preserving defect, establish a private qualification daemon and process namespace, add meaningful positive and negative controls, independently review and verify, then propose the next immutable successor's publication and package qualification.

## Boundaries & Constraints

**Always:** Preserve current tooling changes, all historical/failed evidence, rollback=null, Dapr 1.18.2, the pinned PostgreSQL profile, comparisons 3.70.1/3.110.0, p1r-executed-checks-v1, Reminder recovery and logical-event evolution. Preserve strict shared-resource equality, all owned cleanup, corruption/protection/floor guards and supported case classifications. Keep four roles, conformance and disposition pending, P1R unusable and Story 6.1 blocked. Source verification is distinct from immutable package qualification.

**Never:** Stage, commit, push, publish, initialize nested dependencies, change package/runtime pins, fabricate acceptance, filter unowned resources out of an observed daemon, remove unowned resources or replace old evidence.

## I/O & Edge-Case Matrix

| Scenario | Input | Expected behavior |
| --- | --- | --- |
| Historical covered fixture | Replay DTO at snapshot nine | Stopped-writer fixture replaces state with exact folded count nine, preserving envelope/protection |
| Required supported cases | Two historical writers to selected package, plus two source/package comparisons | Hydrate twelve, supported append/restart thirteen, floor five, history and second tenant preserved |
| Corrupt/unsafe state | DTO collision, absent/uncovered snapshot, invalid floor, unreadable/protected event | Existing refusal and unchanged-data checks still pass |
| Docker isolation | Unrelated outer-daemon container activity | Private daemon's complete shared set remains strictly equal; owned resources removed |
| Isolation failure | Startup/interruption/command error | Nonpassing result retained; repeated exact-ownership cleanup; no unowned removal |

</frozen-after-approval>

## Code Map

Owner is `references/Hexalith.EventStore/`; paths below are relative to that repository.

- `tools/p1r_published_executor.py::mutate('snapshot-nine')` currently adds count to a DomainServiceCurrentState DTO. SQL command pairs 138/141,255/258,981/984,1077/1080,3735/3738,3781/3784,4007/4010,4055/4058 in final execution prove the collision. Replace the whole state with folded Counter state; preserve other fields. Genuine successor manual snapshot path already bypasses this mutation.
- `SnapshotEnvelopeInspector`, `SnapshotManager`, `EventStreamReader` correctly reject the DTO collision and replay below floor. Leave production unchanged. Existing Server tests cover these guards; add paired fixture regression where coverage is missing.
- `tools/p1r-published-executor.py`, `tools/p1r_published_executor.py::{run,topology,cleanup}` use the ambient Docker daemon. Add a narrow isolated runner with private dockerd and executor/Python/.NET/Dapr inside its namespace. No mounted host socket, host networking or published runner ports. Pin runner/toolchain bytes, retain daemon identity and namespace proof. Host cleanup targets only exact runner identity plus invocation label.
- `tools/tests/test_p1r_published_executor.py` and a focused isolation test module: preserve all existing controls. Source binding includes new tools; freeze changes before execution.
- `tools/p1r-published-consumers/README.md`: explain fixture correction and isolated invocation. No generic deployment framework or Dockerfile.
- Final diagnosis input: `_bmad-output/implementation-artifacts/evidence/6-1-p1r-31210-published-run/review-round-1/README.md`. New evidence only: `evidence/6-1-p1r-31210-blocker-repair-20261014/`.

## Tasks & Acceptance

**Execution:**

- [x] `tools/p1r_published_executor.py` and focused tests — replace malformed historical fixture state; verify envelope, protection, tenant and history preservation plus active-writer refusal.
- [x] New `tools/p1r_isolation.py`, CLI and tests — implement private execution namespace and daemon; retain exact identity and ownership proof; repeated owned cleanup on success/failure/interruption. Use a digest-pinned glibc SDK runner with ephemeral tools, without repository dependency updates. Prevent ambient Docker endpoint leakage.
- [x] Server tests — run meaningful existing corruption/protection/floor controls and add paired folded legacy versus DTO collision regression if missing.
- [x] New evidence directory — retain diagnosis, source diff, test/live verification and historical preservation. Re-execute the six affected cases, negative controls, genuine snapshot restore, both additions and strict cleanup with private resources. If broader full qualification is run, keep its scope honest and all failures visible.
- [x] README/proposal — provide reviewed correction and concrete next publication/qualification sequence, immutable identities, unchanged profile and pending gates. Never choose an unapproved successor version or claim old package proof covers it.

**Acceptance Criteria:**

- Given malformed historical evidence, when corrected fixture construction runs, then only fixture state changes and production guards retain their rejection behavior.
- Given private qualification execution, when unrelated Docker activity occurs, then complete private-daemon shared inventories stay equal and owned cleanup passes.
- Given verified correction with pending qualification gates, when reporting, then all role acceptance and Story 6.1 remain pending/blocked.

## Implementation Notes

The user explicitly authorizes preserving and extending this dirty working tree. This continuation's concrete spec executes that authorization; no additional approval is needed. Do not synchronize sprint state. Root independently verifies after the implementation handoff. If environment constraints require an equivalent dedicated-daemon implementation, record it and demonstrate isolation rather than claiming a pass.

## Spec Change Log

- Fixture construction was confirmed as the cause; production readers remain unchanged. The approved intent and acceptance criteria did not change.
- Added actual daemon/namespace proof, exact ownership cleanup and immutable verification-script binding. Follow-up patches address reviewed failure paths and the actual embedded recovery contract.
- The concurrent external Git event is documented and preserved. User instructions override the workflow's commit and sprint-sync defaults: no staging, commit, push, publication or Story status advance occurred.

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B1 | high | patch | Output outside the only workspace mount loses private evidence during owned teardown; reject before provisioning. Resolved. |
| B2 | medium | patch | CLI changes cwd to ROOT but forwards relative paths; normalize path-valued arguments against caller cwd. Resolved. |
| B3 | high | patch | SIGTERM uses default termination and Ctrl+C can remove the SDK before its supervisor records cleanup; forward orderly interruption and retain final receipts. Resolved. |
| B4 | high | patch | Direct repair-driver execution can reach ambient Docker; require actual private daemon/namespace proof before Docker resources. Resolved. |
| B5 | high | patch | Evidence scripts are excluded from standard source_binding and recorded driver hash is not independently checked; bind an additional immutable script closure before and after execution. Resolved. |
| B6 | high | patch | Audit checks only selected receipt paths and trusts summary verified; require exact ten distinct lanes and all registered recovery receipts. Resolved. |
| B7 | high | patch | Measurement audit does not bind or validate outer teardown; add independent final controller audit binding both isolation receipts and actual owned absence. Resolved. |
| B8 | high | patch | Fixed plan/summary paths overwrite earlier attempts; use unique measurement/audit directories and refuse reuse. Resolved. |
| B9 | medium | patch | Public CLI returns from captured Docker exec without structured stdout; restore concrete execution and isolation locations. Resolved. |
| V1 | medium | patch | Existing CI discovers exact filenames and omits isolation controls; add that suite to the P1R workflow. Resolved. |
| V2 | medium | patch | Tests mock private_proof itself; execute actual daemon/namespace refusal predicates with mismatched inputs and assert no command launch. Resolved. |
| V3 | medium | patch | No control exercises automatic public CLI reentry; add routing/path/output controls and a retained live CLI entry-point proof. Resolved. |
| E1 | medium | patch | Same reproduced relative-path defect as B2; separate reviewer finding retained. Resolved. |
| E2 | high | patch | Same outside-mount evidence loss as B1; separate reviewer finding retained. Resolved. |
| E3 | high | patch | Same SIGTERM cleanup gap as B3; separate reviewer finding retained. Resolved. |
| R1 | high | patch | Wrapper permits an explicit command leaving a private sentinel, then removes its daemon/volume without inventorying nested resources. Refuse teardown while unexpected inner containers remain; preserve their parent daemon and volume. Resolved. |
| R2 | medium | patch | Follow-up edge review found docker() replaced a SIGTERM IsolationInterrupted instance with a plain KeyboardInterrupt. Re-raise the original instance so the retained signal remains accurate; focused regression passes in the 23-control isolation suite. Resolved. |
| R3 | high | patch | Follow-up verification found interruption could skip final controller checks and leave validity true. Initialize validity false, retain SIGINT/SIGTERM, and require the exact nine completed checks. Real injected SIGINT and SIGTERM controls both retain nonpassing final-audit receipts. Resolved. |
| R4 | high | patch | Follow-up verification found the outer activity proof could substitute an owned SDK container. Require a distinct exact identity, launch-bound nonce/ownership and recorded creation, running/stopped observations and removal in order. Independent receipt-substitution control is required before handoff. Resolved. |
| R5 | medium | patch | Final independent proposal review found future V enrollment omitted three strict snapshot witness gates. Proposal now enumerates requires_qualified_snapshot, qualified-snapshot writer allowlist and restore writer selection, all three compiler defines and meaningful missing/substituted witness controls. No current version guard was changed. Resolved. |
| R6 | high | patch | The first complete live measurement passed all 3,218 assertions but its independent audit invented a top-level restore.invocation field. Actual restore schema embeds the exact cleanup receipt. Require cleanup.invocation==execution.invocation and full restore.cleanup==cleanup, with unrelated/mismatched invocation negatives; preserve the failed audit and repeat a fresh frozen complete attempt. Resolved. |

All 21 findings are resolved; none is deferred. Three independent layers completed before the initial triage; follow-up independent checks confirmed the repairs. The final proposal enrollment omission and actual recovery-schema mismatch were corrected and independently checked before handoff.

## Verification

- 136 published Python controls and 23 final isolation controls passed.
- Debug Server build passed; all 178 selected EventStreamReader, SnapshotManager, AggregateActorManualSnapshot and AggregateActorFoldedSnapshotMigration tests passed, with no skipped tests.
- Fresh private run: ten complete lanes, 72 cases, 3,218 assertions passed. All six original supported cases retain their classification and all sixteen former failures pass. Signed package role proofs pass 89/89 each.
- Independent measurement audit: 45 checks passed. Final controller audit: all nine required checks passed after teardown. Cleanup 7/7 and dependent restore 12/12 pass, including complete nonempty strict shared equality during actual outer-daemon activity.
- Real private-resource mutation remains nonpassing; actual SIGINT/SIGTERM and copied outer-ID substitution are rejected. Real automatic public CLI isolation retains expected refusal and owned cleanup. Recovery contract recheck passes 41 independent controls including six meaningful mismatched-receipt negatives.
- Historical preservation: all 174,690 prior evidence files and the complete inventory match; three protected Projects files and 68 untouched tooling files match. Final read-only absence scan confirms 34 registered outer resources and six scratch directories removed. No unowned removal.
- All old/new failed attempts remain unchanged. A complete earlier measurement passed but its independent audit failed on an invented restore field; that failure remains visible and a new frozen attempt passed after correction.

Evidence: `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-31210-blocker-repair-20261014/README.md` and its concrete successor proposal.

`done` applies only to this repair spec. Four-role acceptance, same-baseline conformance and support disposition remain pending; P1R is unusable and Story 6.1 remains blocked. Full immutable successor qualification and all separate Story gates remain required. No stage, commit, push or publication was performed by this agent.

Final independent handoff recheck: 46 controls passed without Docker commands or source changes. Final workspace verification confirms the frozen intent, all nine source hashes, three protected documents, both external HEADs and canonical indexes, no staged paths, and unchanged production source.
