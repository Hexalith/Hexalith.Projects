---
title: '6.1-P1R Recover reviewed successor source and evidence'
type: 'chore'
created: '2026-10-15'
status: 'done'
route: 'dispatch'
baseline_commit: '42c8e7bc3a58d78e7c6ea4273711d697e81175e7'
owner_repo_baseline_commit: '5b0df296abc86adf8d37a92fae02e26574d19ad5'
recovery_commit: 'b7a1917cec2a59ffe80dc3ee977cf3aa31376b14'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Active EventStore main omits reviewed P1R repairs, proposed 3.122.1 enrollment and delivery evidence. The exact work survives on local branch `fix/p1r-successor-enrollment`.

**Approach:** Recover only its hash-bound reviewed closure and missing evidence, verify against the active checkout, and retain a current publication handoff.

## Boundaries & Constraints

**Always:** Preserve the user's Story 6.1 spec edit and all existing evidence bytes. Check starting destination bytes before writes; refuse conflicting evidence or concurrent changes. Retain genuine nine/manual/three snapshot proof, finite enrollment, private isolation, complete shared inventory equality and exact owned cleanup. Preserve rollback=null, Dapr 1.18.2, recorded PostgreSQL digest/profile, comparisons 3.70.1/3.110.0 and adopted Reminder/evolution additions. Separate fresh checks from historical passes.

**Never:** Switch branches, move HEAD/gitlinks, stage, commit, push, publish, update dependencies, initialize nested submodules, change production `src/`, overwrite history or select/reserve a version. Keep P1R unusable and Story 6.1 blocked; sprint, acceptance, architecture and readiness state stay unchanged. Publication-authority workflow wiring remains a separate prerequisite.

## I/O & Edge-Case Matrix

| Scenario | Input | Expected Behavior |
| --- | --- | --- |
| Reviewed source | 23 manifest paths | Verify exact Git blobs, lengths and hashes |
| Existing evidence | Present destination | Require equality; leave untouched; conflict stops recovery |
| Missing evidence | 294 paths at investigation | Recover exact blobs; record actual counts and hashes |
| Concurrent change | Changed starting bytes | Stop before overwrite |
| Proposed successor/source | Existing controls | Strict proof runs; arbitrary successors stay excluded |
| Fresh failure | Baseline differs | Retain exact failure and cause; historical pass grants nothing |

</frozen-after-approval>

## Code Map

Paths below are relative to `references/Hexalith.EventStore`. Evidence lives under `_bmad-output/implementation-artifacts/evidence/`.

- Recovery commit above, parent `210b90da172cc7ca946fbb06cfd44390bffa707b`: extract per-file blobs; full branch replacement would import unrelated production/gitlink changes.
- `6-1-p1r-successor-enrollment-20261014/reviewed-source.json`: authoritative 23-path closure covering tooling, consumers, tests, preparation workflow and four audit helpers. Six match active main; eleven differ; six are missing. All recorded hashes verified during investigation.
- Same bundle's `delivery-manifest.json`: all 117 entries verified. `required-ci-test-dependencies.json` binds the four audit helpers.
- `6-1-p1r-31210-blocker-repair-20261014/` and successor bundle: 294 missing files total 173,221,071 bytes; 2,348 compared existing evidence files match without conflicts. Published-run evidence already matches.
- Recovered `publication-readiness-handoff.md`: retain its qualification/authority boundaries. Reuse `verify-compiler-enrollment.py` logic with fresh output in a new directory; its original output must remain intact.

## Tasks & Acceptance

**Execution:**

- [x] New EventStore evidence directory `6-1-p1r-recovery-20261015/`: capture initial source state, protected-file hashes, recovery inventory and exact identities.
- [x] All 23 `reviewed-source.json` paths: recover exact blobs after collision checks; leave identical paths untouched; verify resulting hashes and current API applicability.
- [x] Both missing evidence bundles: copy only absent blobs, verify manifests and preserve every compared existing byte.
- [x] Existing Python suites, Server snapshot tests and compiler profiles: run fresh focused checks; record literal commands/results/source bindings in the new directory. Resolve only recovery defects within this closure.
- [x] New recovery `README.md`: record scope, results and remaining landing, exact-source CI, release authority, publication, observed-package selection/qualification and four-role decisions.

**Acceptance Criteria:**

- Given intact recovery objects, when recovered, then every reviewed/delivery hash matches and unrelated production/history remains unchanged.
- Given the recovered checkout, when controls run, then actual results and baseline differences are independently reviewable.
- Given source-only recovery, when assessed, then no publication, package qualification or prerequisite acceptance is claimed.

## Implementation Notes

Recovered the exact local reviewed closure on 2026-10-16 without changing either
repository revision, branch, index or gitlinks. All 23 reviewed hashes match:
11 differing paths replaced, six absent paths restored, six identical paths left
untouched. Restored 294 absent evidence files (173,221,071 bytes); all 2,348
compared existing evidence files match and were left untouched. All 117 delivery
entries and four required CI helpers verify.

The [recovery handoff](../../references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-recovery-20261015/README.md)
links the initial state, inventory, exact command receipts, applicability and
remaining publication/qualification gates. [Final implementation preservation](../../references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-recovery-20261015/final-preservation.json)
verified 206,456 starting files and 3,376 fresh-check inputs. That receipt predates
these authorized workflow-only updates to this recovery spec; the separate
user-edited Story 6.1 spec and frozen intent remain unchanged.

Fresh checks passed: 228 Python tests, 178 snapshot tests (zero skipped), all 24
compiler profiles without restore, and isolated conflict/concurrent-change
refusal controls. Server.Tests built in Debug/source mode. Both repositories'
`git diff --check` pass; root production-authority preflight passes for Story 6.1.

The pre-change AppHost build succeeded, but startup failed on absent nested
Tenants host projects. Exact commands, failures and logs remain in the new bundle.
Installed Dapr runtime 1.18.7 was observed; no live qualification was attempted,
and preserved qualification inputs retain Dapr 1.18.2. Canonical generated
credential fixtures require reconciliation during separately authorized landing.
P1R remains unusable, Story 6.1 remains blocked, and sprint, acceptance,
architecture and readiness bytes remain unchanged.

Review completion: both new-helper defects were fixed; all seven independent
parent regression controls passed. The parent repeated the complete focused
verification: 228 Python tests, 178 snapshot tests, 24 compiler profiles,
Debug/source build, production-authority preflight and both whitespace checks
passed. Nine pre-existing recovered-tooling gaps are recorded as append-only
follow-up work; they require separate hardening before future qualification.
No commit or remote mutation was performed, as required by the frozen intent.
[Parent verification](../../references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-recovery-20261015/parent-verification/verification.json)
and the final recovery manifest retain the post-review evidence. Spec status
`done` applies to this source-only recovery; Story 6.1 and all prerequisite
acceptance/readiness state retain their original bytes.

## Spec Change Log

## Review Triage Log

Independent blind, edge-case and verification-gap reviewers completed. Each
finding is recorded before grouping; the recovered 23-path closure is immutable
under this intent, so its pre-existing hardening gaps are deferred to separate
work before relying on future qualification.

| ID | Verdict | Evidence and route |
| --- | --- | --- |
| B1 | medium | The real proposed-successor importer accepts the fixture without literal create/image-inspection records or a Python-tools container. Reproduced in a temporary fixture; pre-existing in exact recovery commit. Defer image provenance hardening. |
| B2 | medium | Removing both retained `--inputs` arguments still imports; no literal SDK execution is required. Independently reproduced; pre-existing exact reviewed validator. Defer launch/input binding. |
| B3 | medium | An added, correctly hashed `docker rm -f unowned-id` journal entry still imports. Independently reproduced without executing Docker; pre-existing reviewed validation gap. Defer mutation-target validation. |
| B4 | medium | Removing private supervisor/owned-process cleanup observations still imports. Independently reproduced; pre-existing reviewed validator. Defer private process proof. |
| B5 | medium | Substituted Docker/dotnet executable identities and dotnet version still import; only Python identity is validated. Independently reproduced; pre-existing reviewed validator. Defer full toolchain binding. |
| B6 | medium | Duplicating the first absence observation as the second is accepted. Independently reproduced; pre-existing reviewed validator. Defer distinct observation checks. |
| B7 | medium | The recovered outer controller removes all `DOCKER_*` inputs while accepting `tcp://`, breaking TLS-authenticated controllers. Source confirms this existing limitation; historical handoff already excludes that profile. Defer; local Unix recovery checks grant no remote qualification. |
| B8 | medium | Each of 120 private-daemon readiness attempts can wait the default 300 seconds, giving a roughly ten-hour ceiling. Source confirms this pre-existing control flow. Defer bounded startup timing. |
| B9 | high | `assert` safeguards in the new recovery helper disappear under `-O`/`PYTHONOPTIMIZE`, permitting evidence collision and concurrency refusals to be bypassed. Group with E1; patch by refusing optimized interpreters before any filesystem action and run isolated negative controls. |
| B10 | medium | The new preservation helper computes `index_bytes_unchanged` but omits it from `passed`; an index refresh can leave no staged diff yet change bytes. Patch the verdict and test unchanged/changed index cases. The actual original receipt reported true equality for both indexes. |
| E1 | high | Independent edge review reproduced disabled conflict refusal with optimized Python in an inert temporary workspace. Same defect as B9; one patch and combined normal/optimized refusal verification. |
| V1 | medium | Verification review disabled the controller's final attachment branch in memory and all 228 workflow tests still passed. The direct finalizer tests do not cover its real caller. Defer a caller-level regression test for the already-reviewed runner; source-only recovery does not exercise or accept published qualification. |

## Verification

From EventStore, run `python3 -m unittest discover -s tools/tests -p 'test_p1r_published_executor.py'` and the existing isolation, published-qualification and preparation suites individually. Build Server.Tests in Debug/source mode; run its built xUnit v3 assembly for EventStreamReader, SnapshotManager, AggregateActorManualSnapshot and AggregateActorFoldedSnapshotMigration classes. Evaluate all 24 compiler profiles without package restore into new output paths. Compare preservation hashes and run `git diff --check` in both repositories.

Root scheduling preflight passed: `python3 tools/planning/validate_production_authority.py --story-id 6.1`. Read-only GitHub observation found canonical main `746b843a24f18b17a7572be25b76b735fa8e4ca6` and latest release v3.122.0. Its green CI does not validate the missing closure. Canonical security/test changes include two generated-credential fixture changes requiring applicability assessment.
