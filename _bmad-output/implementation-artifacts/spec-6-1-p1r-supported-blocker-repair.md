---
title: '6.1-P1R Repair supported qualification fixtures and isolate execution'
type: 'bugfix'
created: '2026-10-14'
status: 'in-progress'
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

- [ ] `tools/p1r_published_executor.py` and focused tests — replace malformed historical fixture state; verify envelope, protection, tenant and history preservation plus active-writer refusal.
- [ ] New `tools/p1r_isolation.py`, CLI and tests — implement private execution namespace and daemon; retain exact identity and ownership proof; repeated owned cleanup on success/failure/interruption. Use a digest-pinned glibc SDK runner with ephemeral tools, without repository dependency updates. Prevent ambient Docker endpoint leakage.
- [ ] Server tests — run meaningful existing corruption/protection/floor controls and add paired folded legacy versus DTO collision regression if missing.
- [ ] New evidence directory — retain diagnosis, source diff, test/live verification and historical preservation. Re-execute the six affected cases, negative controls, genuine snapshot restore, both additions and strict cleanup with private resources. If broader full qualification is run, keep its scope honest and all failures visible.
- [ ] README/proposal — provide reviewed correction and concrete next publication/qualification sequence, immutable identities, unchanged profile and pending gates. Never choose an unapproved successor version or claim old package proof covers it.

**Acceptance Criteria:**

- Given malformed historical evidence, when corrected fixture construction runs, then only fixture state changes and production guards retain their rejection behavior.
- Given private qualification execution, when unrelated Docker activity occurs, then complete private-daemon shared inventories stay equal and owned cleanup passes.
- Given verified correction with pending qualification gates, when reporting, then all role acceptance and Story 6.1 remain pending/blocked.

## Implementation Notes

The user explicitly authorizes preserving and extending this dirty working tree. This continuation's concrete spec executes that authorization; no additional approval is needed. Do not synchronize sprint state. Root independently verifies after the implementation handoff. If environment constraints require an equivalent dedicated-daemon implementation, record it and demonstrate isolation rather than claiming a pass.

## Spec Change Log

## Review Triage Log

## Verification

- `python3 -m unittest discover -s tools/tests -p 'test_p1r_published*.py' -v` and focused isolation controls.
- Build Server test project in Debug and run built xUnit assembly with `-class` for EventStreamReaderTests, SnapshotManagerTests, AggregateActorManualSnapshotTests and AggregateActorFoldedSnapshotMigrationTests; use source references where available.
- Fresh isolated package and distinct Debug/source measurements; independently rederive retained witnesses, coordinates, recovery and strict cleanup.
- Compare historical SHA-256 manifest, pre-existing tooling diff, Git index and blocked-state files. No staging or publication.
