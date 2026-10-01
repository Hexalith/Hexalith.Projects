---
title: '6.1-P1R Accept EventStore 3.110.0 and Builds 4.29.1'
type: 'chore'
created: '2026-10-01'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '4d8dcf65803792f7def3b10ed21227329536154b'
context:
  - '{project-root}/_bmad-output/specs/spec-6-1-p1r-revalidation-owner-acceptance/qualification-contract.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** P1R accepts only EventStore 3.106.0 with Builds ad52f350. Current packages use 3.110.0, while the pending owner packet still selects 3.109.0. Historical acceptance cannot authorize either newer tuple.

**Approach:** Prepare the chosen baseline for four-role acceptance, then coordinate the fixed record, guard, contract, architecture binding, and sprint representations using the minimal schema.

**Decision (2026-10-01):** The user selected EventStore `3.110.0` / `v3.110.0` / `27279fe6431925a6ea046c3f89af61487185c7de` with Builds `4.29.1` / `21ce044ab465ccb2adab58b3d66e394ffbecf3c2`. This selects the implementation target; four-role acceptance remains pending.

## Boundaries & Constraints

**Always:** Distinguish the published tag from the later source checkout. Bind each role decision to the record and tuple; preserve the 3.70.1 rollback. Keep current prerequisite usability false while G-6 is stale; P0 stages 2–7, P2–P4, readiness, and Story 6.1 retain their states.

**Never:** Infer owner acceptance from this invocation, successful CI, or older decisions. Add qualification bundles to the acceptance schema, replace historical evidence, waive checks, update dependency pointers, deploy, publish, commit, or push.

## I/O & Edge-Case Matrix

| State | Expected behavior |
| --- | --- |
| Missing or rejected role | Preserve the historical record; report the missing decision |
| Four exact accepts | Every acceptance representation agrees on one tuple |
| Stale G-6 | P1R acceptance does not claim current runtime qualification |
| Malformed or mixed tuple | Guard fails closed |

</frozen-after-approval>

## Code Map

- Current: EventStore tag `v3.110.0` = `27279fe6431925a6ea046c3f89af61487185c7de`; Builds = `21ce044ab465ccb2adab58b3d66e394ffbecf3c2`. EventStore checkout = `6dededdecd62dd6dc6d1f15810108d860ec70c8f`, with 43 changed source paths after the tag.
- `tools/planning/validate_production_authority.py`: `SELECTED_TUPLE`, `_validate_acceptance_record`, `_validate_closure_boundary`; preserve schema, role checks, rollback, and downstream locks.
- `references/Hexalith.Builds/Props/Directory.Packages.props`: catalog, runner, and manifest schema agree on 3.110.0; Toolkit 13.6.0-beta.910 and Fluent UI 5.0.0 lack current G-6 acceptance.
- `.github/workflows/ci.yml`: CI executes Builds 212583e0 instead of its gitlink.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad-output/implementation-artifacts/6-1-p1r-current-exact-baseline-candidate.md` -- record full coordinates, existing evidence, limitations, and each pending role decision. Distinguish successful tag CI 36608682063 from release 36608763986 using BYPASS_VALIDATION and Commitlint source proof; name unverified archive, metadata rollback, actor replay, and checkout compatibility claims.
- [x] `tools/planning/validate_production_authority.py`, `tests/tools/test_production_authority_guard.py` -- after four-role acceptance, rebind the selected tuple and retain all rejection controls.
- [x] `_bmad-output/implementation-artifacts/6-1-p1r-acceptance.json`, `_bmad-output/specs/spec-6-1-p1r-revalidation-owner-acceptance/SPEC.md`, `_bmad-output/specs/spec-6-1-p1r-revalidation-owner-acceptance/qualification-contract.md` -- update the exact tuple, UTC timestamp, and named decisions together.
- [x] `_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md` -- update the EventStore binding only under the explicit Solution Architect decision; preserve independent toolchain dispositions.
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml`, `_bmad-output/implementation-artifacts/6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md`, `_bmad-output/implementation-artifacts/deferred-work.md` -- reconcile P1R, P0 Stage 1, DW-35/68 and candidate prose (already-done statuses stay done); validate a separate candidate index with matching companion files before atomic replacement.

**Acceptance Criteria:**
- Given four explicit named accepts, when the coordinated transition is validated, then every selected coordinate agrees and only the authorized P1R boundary is accepted.
- Given an absent decision or invalid record, when acceptance is attempted, then it fails closed without replacing the historical acceptance.
- Given stale G-6 evidence, when P1R is recorded, then current qualification remains unavailable and downstream work stays blocked.

## Implementation Notes

Owner acceptance received 2026-10-01T06:17:01Z: the user stated, “I Jérôme Piquot am the Owner for all roles and I accept,” in response to the exact candidate-packet decision request. Record Jérôme Piquot as accepting EventStore Owner, Builds Owner, Solution Architect (including the EventStore Stack rebinding), and Test Architect for EventStore `3.110.0` / `v3.110.0` / `27279fe6431925a6ea046c3f89af61487185c7de` and Builds `4.29.1` / `21ce044ab465ccb2adab58b3d66e394ffbecf3c2`, bound to `_bmad-output/implementation-artifacts/6-1-p1r-acceptance.json` and `#/selected`, with the packet’s recorded limitations. This authorizes the remaining coordinated acceptance tasks. Preserve the rollback and historical evidence. G-6 is not accepted by this decision; current usability stays false and downstream states do not advance.

The coordinated acceptance now selects EventStore `3.110.0` / `v3.110.0` / `27279fe6431925a6ea046c3f89af61487185c7de` with Builds `4.29.1` / `21ce044ab465ccb2adab58b3d66e394ffbecf3c2` in the fixed record, guard, owner-acceptance spec and qualification contract, Spine EventStore row, sprint gate, P0 Stage 1 prose, and DW-35/68. The record retains the minimal schema and uses the explicit decision timestamp `2026-10-01T06:17:01Z`. The [prior record](evidence/6-1-p1r-acceptance-20260922-3.106.0.json) is preserved verbatim as historical evidence outside the gate; the older candidate packets and sprint observations are retained.

The staged acceptance tree at `/tmp/hexalith-p1r-accepted-p01butus` passes the guard with all four companion-path overrides and all 21 focused tests. New controls pin the independently stated approved tuple, reject historical/mixed coordinates and the later source checkout, and verify that current P1R acceptance leaves stale G-6 prerequisite usability false. All four roles are exercised for missing/rejected decisions. Independent architecture toolchain dispositions, rollback, already-done statuses, P0 stages 2–7, P2–P4, readiness, and Story 6.1 remain unchanged. G-6 still reports seven failures; acceptance does not claim archive, metadata rollback, actor replay, or checkout compatibility verification.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B1 | medium | defer | Confirmed in a disposable accepted fixture: flipping the optional prerequisite-usability flags to true still passes validate_index. Those fields and the omission predate this P1R change; actual current flags stay false and all scheduling/downstream locks remain enforced. Future candidate-index generation could misstate usability without a guard diagnostic. |
| B2 | medium | defer (with B1) | Confirmed that changing optional p1r_current_revalidation.candidate to 9.0.0 still passes validate_index. The authoritative fixed record and sprint gate remain strictly bound, and this transition checked all representations separately. Optional revalidation metadata was already outside the minimal guard before this change; a future consistency policy must permit legitimate pending candidates to differ from historical acceptance. |
| B3 | medium | defer | The raw signed numeric-sub reproduction passed IdentityModel validation and the existing authentication callback, becoming string subject 42. RFC 7519 section 4.1.2 defines a string subject. Authentication source is concurrent P3 work, unchanged by P1R; preserve it and route raw-type validation/fixtures to that work. |
| B4 | medium | defer | Both forwarding tests instantiate the handler directly; the production registration is in ProjectsServerServiceCollectionExtensions.cs and no test exercises that registered outbound pipeline. Removing registration could break real gateway forwarding without those controls detecting it. This is a concurrent authentication coverage gap, not caused by P1R. |
| B5 | medium | defer | The signed protected-read control changes a tenant hint while the JWT remains tenant-a; the separate binding controls use constructed principals. An end-to-end signed tenant-b token against tenant-a data is absent, leaving that composition without its own negative control. This belongs to concurrent P3/read verification, not this acceptance transition. |
| B6 | medium | defer | CreateToken always supplies expiration and the middleware rejection matrix has no no-exp token. Disabling the currently required lifetime expiration could escape the tests. The actual lifetime validator remains enabled; this is a pre-existing authentication verification gap. |
| B7 | false | reject | The root diff correctly reports dirty owner repositories rather than embedding their implementations. P1R approves immutable published coordinates with explicit checkout/qualification limitations and does not claim approval of concurrent P0/P2 repairs; their owner work remains unaccepted. Their absence from this root diff does not invalidate the bounded P1R decision. |
| B8 | maybe-false | defer (unverified medium) | The concurrent P3 manifest retains summarized no-build Aspire results and test-assembly/source hashes, but no retained launch/describe output or executed AppHost/service hashes. Wrong runtime bytes were not demonstrated; retain those command outputs and binary hashes to settle whether the observation binds to the recorded source. P1R grants no runtime qualification. |
| B9 | medium | patch | P3's preceding Current gate check links the mutable fixed record while asserting 3.106.0, which is now misleading after rebinding. Label the preceding check historical, link the archived record, and note subsequent exact P1R acceptance without changing P3 state. The concurrent P0 spec already records the subsequent 3.110.0 acceptance, so its claimed stale statement no longer exists. |
| B10 | medium | defer | The active candidate replay still targets 3.109.0 and its own retirement conditions are met. It was already stale before this build because the initial EventStore 6dededde checkout differs from the recorded 5b949483 source; CI invariant tests also require the old job. P1R changes no CI checks and records the limitation; resolve the existing historical replay/CI retirement under its owner work without treating it as current qualification. |
| V1 | medium | defer | The verification-gap layer proved the multiple-sub fixture also has a conflicting NameIdentifier alias, masking a cardinality regression. Its filed evidence is trusted: add a signed duplicate-sub control without a conflicting alias in concurrent P3 verification. P1R changed none of those authentication files. |

Edge Case Hunter returned no findings. Review covered the full root diff, including preserved concurrent authentication and owner work. One direct documentation correction was applied; eight grouped pre-existing or unverified entries were deferred, and one unsupported finding was rejected.

## Verification

- `python3 tools/planning/validate_production_authority.py --story-id 6.1` -- pass.
- `python3 tests/tools/test_production_authority_guard.py` -- all focused controls pass.
- `python3 tools/planning/validate_production_authority.py --validate-index` -- pass before and after transition; validate the candidate with `--sprint-status`, `--deferred-work`, `--p0-artifact`, and `--workspace-root` pointing to its coordinated staging tree.
- `git diff --check` -- pass.

Implementation preparation on 2026-10-01: baseline story/index checks passed; all 18 focused production-authority guard tests passed. The candidate index passed with all four staging-path overrides (`--sprint-status`, `--deferred-work`, `--p0-artifact`, `--workspace-root`) against `/tmp/hexalith-p1r-current-c13a6u6f`. Additional assertions confirmed the exact candidate tuple, 13-commit/43-source-path drift (40 added, 3 modified), valid packet companion links, unchanged fixed acceptance/minimal contract bytes, unchanged accepted Spine tables, unchanged scheduling/action/stage states, and false current prerequisite usability.

Post-preparation checks on 2026-10-01: Story 6.1 and index guards passed; all 18 focused controls passed; `git diff --check` and untracked packet/spec whitespace checks passed. Existing tracked Markdown line endings were preserved to keep the diff focused. At that preparation checkpoint the last task remained unchecked pending the four exact decisions; the subsequent accepted transition completes it.

Planning preflight on 2026-10-01: story and index checks passed. `python3 tests/tools/run_g6_candidate_gate.py --baseline references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-09-29.json --packet _bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain-20260929/packet.json` exited 1: Toolkit catalog drift, five submodule revision mismatches, and the Builds execution-SHA mismatch (seven failed checks). This is separate from the minimal P1R record gate.

Acceptance staging on 2026-10-01: staged index validation passed with matching companions and the updated guard; all 21 focused tests passed; consistency assertions confirmed record/guard/index/contracts/Spine tuple agreement, exact historical-record preservation, unchanged independent architecture rows and downstream statuses, canonical UTC decisions, and false current usability.

Post-acceptance verification on 2026-10-01: active Story 6.1 and index guards passed, all 21 focused tests passed, and `git diff --check` plus untracked artifact whitespace checks passed. Live files matched the validated staging tree; the historical JSON was preserved byte-for-byte; companion links resolved; current P1R/G-6 usability stayed false. All implementation tasks are complete; independent fresh G-6 qualification remains outside this acceptance.

Final review verification on 2026-10-01: Story 6.1 and production-authority index guards passed; all 21 focused tests passed; `git diff --check` passed after the documentation correction and deferred-work entries. P1R is complete. The frozen intent prohibits committing or pushing; no commit was created. Story 6.1 and downstream qualification states remain unchanged.
