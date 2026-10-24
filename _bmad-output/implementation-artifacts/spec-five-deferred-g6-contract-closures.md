---
title: 'Complete five deferred G-6 contract obligations'
type: 'chore'
created: '2026-10-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_revision: 'f9ce4e934dc25215bcd0c098308b83a97d3cfd9d'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five historical G-6 obligations remain open despite replacement contracts: unbound limitations and closure evidence, a required failing restart preflight, missing package families in the audit, a required failing package inventory, and packet acceptance without a bound named decision.

**Approach:** Verify the existing v2 fixes with executable root regressions, document their supersession and acceptance/index boundaries, and close exactly these five deferred entries. Reuse the Builds-owned hermetic packet fixture and actual validator, with independent tampering, successful/failed extra-command and named-decision controls. Use a test-owned package table and real temporary catalog/project inventory to check Dapr actor/AspNetCore, Aspire.Hosting and EventStore coordinate drift. Run the checks in the existing blocking G-6 preflight after root dependencies are initialized. Preserve v1 historical validator behavior, retained packet bytes, submodule revisions, package pins and sprint/acceptance state. Commit and push the verified changes on main as requested.

</frozen-after-approval>

## Implementation Notes

- This small reversible root verification/documentation change has no unresolved intent gaps, product API, data mutation or deployment. Main publication is explicitly authorized. The one-shot route applies.
- Required baseline/Git guidance, root editor/build/test configuration, project context, Architecture Spine and current topology runbook were inspected. No AppHost/runtime code changes are involved.
- Code map: Builds Tools/runtime_toolchain_v2.py owns artifact hashing, all-success commands, broad controlled package classification and named packet decisions. Tools/test_runtime_toolchain_v2.py owns hash-bound ordinary test fixtures; it is source, not an agent skill. The active compact g6_current.py execution contract remains separate from archived v2 acceptance.
- Planned root footprint: tests/tools/test_g6_deferred_contracts.py, .github/workflows/ci.yml preflight invocation, docs/runbooks/projects-topology.md, the five ledger dispositions and this spec. No dependency files change.
- The packet fixture may model Git/source observations, but the actual packet checks, artifact hashing, command outcomes and decision validation remain real. Each negative control must fail at its intended validation boundary, with a passing unmodified fixture first. No fixture or test result grants current operational qualification.

- CI policy also requires the exact new blocking command; temporary workflow mutations remove it, comment it out or append `|| true`. The existing no-submodule positive gate remains. This adds two small existing policy/test edits to the planned footprint.
- Initial verification: 26 existing current-runner, historical-runner and whitespace tests passed with no skips. The live workflow policy and active production-authority index both passed; no runtime qualification was run.

- Existing implementation provenance: root-declared Builds gitlink `0d8b910e0ee4df31198838f35242d0e6be9c80c4`. Exactly five dated dispositions were added to deferred-work.md; removing those additions reconstructs the original bytes exactly.
- Workflow positive and three omission/ignore mutations passed (two unittest methods, 22.056 seconds). The PowerShell gate and active production-authority index passed. Existing LF files retain LF; the new Python/spec files use CRLF.

## Verification

- `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests/tools/test_g6_deferred_contracts.py -v` — final completed-source run: 2/2 methods passed in 64.307 seconds, zero failures/skips. The reused Builds fixture reports 103 existing mutation controls plus accepted metadata-commit positive validation. New root matrix: 35 named cases, comprising 25 packet negatives, two successful extra-command controls and eight package/property drift negatives with restored positive inventory.
- `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests/tools/test_run_g6_current.py tests/tools/test_g6_qualification_runner.py tests/tools/test_git_whitespace_policy.py -v` — 26/26 passed in 3.230 seconds, zero failures/skips.
- `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests.tools.test_run_ci_workflow_gates.WorkflowGateTests.test_checkout_passes_without_submodules tests.tools.test_run_ci_workflow_gates.WorkflowGateTests.test_deferred_g6_contract_controls_cannot_be_omitted_or_ignored -v` — 2/2 methods passed in 22.056 seconds; all three omission/ignore mutations rejected.
- `pwsh -NoProfile -File tests/tools/run-ci-workflow-gates.ps1` — passed across five workflows.
- `PYTHONDONTWRITEBYTECODE=1 python3 tools/planning/validate_production_authority.py --validate-index` — passed; authority remains [6, 7, 8].
- `git diff --check` — passed. Existing line endings preserved; new files use CRLF. Byte reconstruction verifies only five ledger dispositions were added.
- Fetch confirmed main/origin/main divergence 0/0. Repository commitlint 21.2.0 accepted the prepared Conventional Commit message. No commit-validation bypass is used.

## Review Triage Log

- BH1 — medium, patched: matching one command line allowed surrounding `set +e` or `if false` to bypass execution. The existing exact-block helper now requires the whole preflight name/shell/run step, including explicit Bash failure handling. The mutation matrix covers removal, comment, tolerated exit, disabled errexit, false conditional and shell override.
- BH2 — medium, patched: earlier failure cases only tested contradictory technicalValidity=true metadata. Added consistent technicalValidity=false failures for both legacy command purposes and the real restart qualifier; each must reach the separate final candidate-failure guard.
- BH3 — medium, patched: the package fixture previously inventoried only its consumer. A separate temporary Builds Git repository now tracks and inventories the real catalog. Positive assertions include both rows; an unconsumed Dapr.Client catalog drift case rejects at the catalog path and restores the passing state.
- BH4 — low, patched: the acceptance-index guide omitted scope, usability, role and follow-up summaries. It now names their exact G-6/P1R locations, limits and evidence/owner-decision update conditions while preserving historical index bytes.
- BH5 — medium, patched: reviewed-hash wording omitted required serialization. The runbook now specifies UTF-8, ensure_ascii=false, indent=2, original field order and final LF with a runnable normalization recipe. Executing that exact documented body reproduces the retained attempt-16 reviewed decision hash.

- All five findings were accepted for local correction; none is deferred. Initial verification counts above precede review corrections; final results follow below.

## Final verification after review

- Final completed-source contract run: 2/2 methods passed in 65.003 seconds, zero failures/skips; 39 root cases (30 packet cases and nine package/property drift cases) plus the existing 103 mutation controls and accepted positive. This supersedes the earlier contract-case counts.
- Final workflow-policy fixture run: 2/2 methods passed in 35.149 seconds, including all six omission/ignore mutations. The live workflow gate, production-authority index and whitespace checks pass after corrections.
- Independent reviewer rechecked every correction and confirmed no remaining issues. Removing the candidate-failure guard now produces the three intended failures; removing catalog PackageVersion handling now fails the positive catalog assertion. The documented hash recipe reproduces the retained reviewed decision hash exactly. No finding is deferred.
- No package pin, submodule pointer, sprint index, fixed acceptance record or historical packet changed. The five ledger resolutions distinguish existing v2 fixes from the new verification and documentation. Main commit/push remains explicitly authorized.
