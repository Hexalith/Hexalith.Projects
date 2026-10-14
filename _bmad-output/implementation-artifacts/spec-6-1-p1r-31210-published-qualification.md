---
title: '6.1-P1R Qualify the selected 3.121.0 published packages'
type: 'chore'
created: '2026-10-14'
status: 'done'
route: 'dispatch'
baseline_commit: 'b82e563d8dbeefdf393bb8926cb7cadfa70a80c9'
owner_repo_baseline_commit: '21b34c01ec919769cfba9cfe0d47e50886b57ded'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-state-instructions.md'
  - '{project-root}/_bmad-output/implementation-artifacts/evidence/6-1-p1r-31210-publication/qualification-proposal.json'
---

<frozen-after-approval reason="user selected the exact published family and approved execution inputs on 2026-10-14">

## Intent

**Problem:** Publication proves EventStore 3.121.0 exists, but no fresh persisted package qualification or successor support measurement exists. P1R is unusable and Story 6.1 remains blocked.

**Approach:** Execute the approved proposal in fresh isolated Release/package consumers, retain every measured result, create a genuine qualified folded snapshot, restore persisted state into an owned database, append and restart, measure the successor support table and independently verify the evidence. Update only qualification tooling needed to exercise these published APIs.

**Decision:** The user states: "I select EventStore 3.121.0 at source 21b34c01ec919769cfba9cfe0d47e50886b57ded and approve the execution inputs in: _bmad-output/implementation-artifacts/evidence/6-1-p1r-31210-publication/qualification-proposal.json". This supplies execution authority, not the four later role acceptances or support-table disposition.

## Boundaries & Constraints

**Always:** Bind all five signed archives to the proposal, exact tag/source and Builds identities. Retain rollback=null, AD-17 single-writer drain/mutation freeze/forward recovery, Dapr 1.18.2 and the pinned PostgreSQL digest, comparisons 3.70.1/3.110.0, adopted p1r-executed-checks-v1 authority and Reminder recovery/logical-event-evolution additions. Preserve all historical and failed evidence. Only invocation-owned resources may be modified or removed. Keep source Debug comparison distinct from published Release proof. Record qualified snapshot creation as actual published execution with persisted version/state checks, never SQL synthesis. Keep four roles and conformance pending, P1R unusable and Story 6.1 blocked.

**Never:** Change production packages, pins, Stack, historical acceptance or sprint/readiness states; commit, stage, push or initialize nested submodules; weaken protected-hash, assertion or support-disposition gates; invent owner decisions; hide failed historical directions or data loss.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected behavior | Error handling |
| --- | --- | --- | --- |
| Package binding | Approved immutable archives | Exact hashes, graphs, signatures and physical loads | Retain mismatches as failures |
| Qualified snapshot | Nine committed Counter events | Published manual creation persists version-one folded state; append to twelve | Missing/unqualified capture fails |
| Restore | Floor five/head twelve/snapshot nine | Fresh restore, append thirteen, restart with tenant/history preservation | Failed results stay nonpassing |
| Successor boundary | All seventeen scenarios and two additions | Fresh complete measured table with pending disposition | No invented refusal or acceptance |
| Cleanup | Failure, timeout and repeat cleanup | Owned resources removed, shared identities unchanged | Retain cleanup errors |

</frozen-after-approval>

## Code Map

Paths under EventStore are owned by `references/Hexalith.EventStore/`.

- `tools/p1r-published-executor.py`, `tools/p1r_published_executor.py` — fresh run, private consumers, command/check witnesses, restore and cleanup; successor guard requires HEAD equal tag. `seed` currently synthesizes snapshot nine. Keep historical fixtures separate.
- `tools/p1r-published-consumers/{Directory.Build.props,host/Host.csproj,domain/Domain.csproj}` — hardcoded 3.119.0 defines/manifest inclusion omit the selected successor. Add bounded explicit successor handling.
- `tools/p1r-published-consumers/{host/Program.cs,domain/Program.cs,probe/Program.cs}` — register matching fixture qualifications and expose published manual snapshot measurement. Counter aggregate has no automatic snapshot processor; use manual creation at nine, then three actual appends.
- `src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs`, `src/Hexalith.EventStore.Server/Actors/AggregateActor.cs` — existing published qualification/manual creation API; inspect/reuse without production edits. Minimum automatic interval is ten.
- `tools/p1r_published_qualification.py` — strict package/receipt binding, measure_successor_support_table and read-only validation. Full packet admission requires Solution Architect disposition; preserve it.
- `tools/tests/test_p1r_published_{executor,qualification}.py` — existing negative controls; extend only for materially changed fixture behavior.

## Tasks & Acceptance

**Execution:**
- [x] `evidence/6-1-p1r-31210-published-run/` in EventStore implementation artifacts — retain approval/proposal hash, owner-inputs, four pending decisions, unchanged-history inventory and new unique run paths. Reuse exact historical comparison and instrumentation records without new Test acceptance.
- [x] EventStore consumer/tooling paths above — enable selected successor features and genuine qualified snapshot creation, with measured fixture parity and finite shared limits; preserve historical fixtures and raw outputs.
- [x] EventStore focused Python controls and isolated consumers — run meaningful regression checks before execution.
- [x] New EventStore run directory — execute all canonical lanes/additions, backup/restore/append/restart and cleanup; collect package graph/load proof and persist all failures. Fix authorized tooling defects and rerun in fresh paths if necessary, retaining attempts.
- [x] New measured boundary and independent verification receipts — rederive case inventory, predicates/probe output, command/config/source/package bindings, restore/cleanup evidence and hashes. Attempt strict packet preparation/validation only honestly; preserve any pending-disposition refusal rather than granting it.
- [x] New run README and Projects decision-packet append — report actual results, exact blockers and pending gates; preserve prior content.

**Acceptance Criteria:**
- Given approved inputs, when fresh consumers execute, then every package lane binds the five selected signed archives without EventStore source dependencies.
- Given a qualified snapshot at nine, when persisted restore, append and restart execute, then sequence thirteen, floor five, original hashes and tenant isolation are measured.
- Given pending role decisions or failed required checks, when evidence is evaluated, then qualification/usability and Story 6.1 stay blocked.

## Implementation Notes

The existing execution approval satisfies the build checkpoint; no additional permission is required for the same concrete inputs. Story-key synchronization is deliberately omitted because this prerequisite execution must preserve the user's explicit Story 6.1 blocked state. Source bindings must be stable during live execution. Package evidence arguments refer to consumer directories containing evidence.json.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B1 | medium | patch | The new guard checks only Probe.dll basename. Full validate_case admits a substituted /tmp/unverified/Probe.dll, and bind_lane verifies archive identities without associating this invocation path with them. |
| B2 | medium | patch | probe_command parses retained output without checking successful exit. A manual exit_code=1 is admitted; the audit adds this check for only the restore case. |
| B3 | medium | patch | The seed/manual endpoints match, but the tail endpoint is unbound. Launch/configuration evidence exists and can bind the restarted sidecar to the same persisted stream. |
| B4 | medium | patch | The provider selector accepts any docker exec containing psql. The owned container/configuration evidence does not guard that selector; substitution is admitted. |
| B5 | medium | patch | The helper compares seed.tail to output but never recomputes the tenant-a:committed-count predicate from it. Changing both to committed=2/sequence=11 leaves a true check admitted. |
| B6 | medium | patch | Both snapshots can match any inventory output. Reusing before_tail as after_tail is admitted, so preservation alone does not demonstrate the three appends. |
| B7 | medium | patch | The new positive helper fixture has zero prefix events. No guard requires head nine/events1..9 before tail or head twelve/events1..12 afterward. |
| B8 | medium | patch | Version/state/sequence omit embedded tenant/domain/aggregate and protection metadata. Those fields are available in the retained genuine snapshot envelope but are not recomputed. |
| B9 | medium | patch | Only maximumSimultaneousLiveBytes is bound. Other limits and derived reservations from the retained shared fixture can be replaced and admitted. |
| B10 | medium | patch | The explicit21-receipt selection includes original cleanup/restore but omits the contributing fallback invocation cleanup. The audit does not require its independent validation. |
| E1 | medium | patch | Removing both original recovery receipts leaves the canonical19-lane inventory and rederived table unchanged. The audit can return valid_evidence without auditing retained recovery failures. |
| V1 | medium | patch | The filed mutation disables requires_qualified_snapshot only at validate_case and all124 focused controls pass. Add an intact/deleted-proof pair through admission with candidate3.121.0. |

All twelve findings were assessed individually after all three reviewers returned. B6/B7 share incomplete before/after seed-inventory proof; B10/E1 share incomplete audit receipt inventory. Other findings remain separate causes. Each repair is a private guard or focused control for a demonstrated evidence mutation, with no public API or acceptance change. The prior artifacts remain immutable; corrected source requires a new isolated execution and independent audit.

The pre-existing incomplete-checkout validator `KeyError` was confirmed against EventStore HEAD21b34c01. The adjacent call-site repair now guards comparison access for incomplete cases; its earlier failed audit and checkout remain retained. No review finding remains deferred.

## Verification

- `python3 -m unittest discover -s tools/tests -p 'test_p1r_published*.py' -v` from EventStore.
- `python3 tools/p1r-published-executor.py run --out <new-directory> --inputs <approved-owner-inputs>` from EventStore.
- `python3 tools/p1r-published-qualification.py measure-boundary --inputs <inputs> --receipt <each-lane> --out <new-table>`.
- Independent package/check/restore/cleanup validation; strict prepare/validate preserve pending-disposition rejection. Run Projects production-authority guard and focused preservation checks.

## Implementation execution evidence — 2026-10-14

All selected package consumers and a separate serialized Debug/source checkout executed on the frozen EventStore/Builds source closure. The selected evidence contains 19 lanes and 163 cases with 4,754 assertions (4,738 passed, 16 failed). Focused qualification controls passed 124 tests. Genuine published manual creation persisted a version-one folded snapshot at nine, followed by actual appends, fresh restore, append to thirteen, restart, floor-five preservation and tenant/history checks.

The original full attempt, initial typed-remoting smoke failure, original Debug/source timeout, failed independent audit attempts and all historical comparison failures remain retained. A fresh serialized checkout supplement uses the identical source closure; its explicitly selected receipt set retains the original failed recovery receipts. The original and selected-set successor tables are both measured pending disposition.

Independent audit C reports valid evidence with no validation errors and 89/89 checks for each package role. Six required cases retain sixteen failed checks in historical retained hydration/append directions and their source/package comparisons. Original cleanup is 6/7 and restore 11/12 because unrelated shared mTLS containers changed; every owned resource was removed, and unowned resources were untouched. The original incomplete-checkout validator KeyError is recorded; the complete selected checkout validates successfully.

Both strict packet prepare/validate attempts preserve the pending Solution Architect disposition refusal. Four role decisions and conformance remain pending, P1R remains unusable, and Story 6.1 remains blocked. No frozen requirement, production source, package pin, Stack, historical acceptance, sprint or readiness state was changed.

Evidence: `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-31210-published-run/README.md`; `selected-receipts.json`; `successor-support-boundary-with-source-fallback.json`; `independent-validation-20261014-c/independent-validation.json`.

## Review-fixed completion — 2026-10-14

All 12 reviewed findings were repaired and the parent verified 134 focused controls. Fresh execution on the corrected frozen source completed 19 lanes/163 cases with 4,774 assertions (4,758 passed, 16 failed), no executor errors and a successful distinct Debug/source build. The 61/61 genuine snapshot/restore/append/restart lane, 641/641 mixed API, 131/131 Reminder recovery and 193/193 logical-event evolution measurements pass.

Independent audit D validates all 21 receipts, including cleanup and candidate restore, with no validation errors and 89/89 checks for each package role. Six supported cases retain sixteen historical hydration/append failures. Owned cleanup completes, but shared-set equality fails because three unrelated mTLS containers disappeared, leaving cleanup 6/7 and dependent restore 11/12 failed. Strict prepare/validate retain the pending measured-owner-disposition refusal. All historical and earlier failed evidence is preserved.

The prerequisite execution spec is complete. Four role decisions, conformance, support disposition and Story 6.1 acceptance remain pending/blocked. No commit or sprint transition was performed; the user's preservation and blocked-state instructions override the workflow's generic commit/review-state step. No review finding remains deferred.

Final evidence: [review-fixed qualification](../../references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-31210-published-run/review-round-1/README.md).
