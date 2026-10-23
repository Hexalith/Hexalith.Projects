---
title: 'Complete five deferred CI and qualification tooling items'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_revision: '44cec09d46b882acb742834fc9f7fe9d042209c2'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five deferred CI tooling items leave catalog version-set overrides undetected, suppress all but the first policy failure, lack executable workflow-gate regressions, overstate the scope of the Builds execution-SHA check, and select a stale G-6 default policy.

**Approach:** Complete this bounded tooling slice on main and push the verified commit as requested. Reject Conversations/Folders version-set property overrides in the root props files, active workflows and inherited environment. Print every collected failure before returning exit 1. Execute the real PowerShell gate against temporary copies with positive and negative controls, and run these fixtures in blocking CI. Describe the Builds check explicitly as checking direct ci.yml references, using the ledger's permitted narrower-assertion resolution. Align both G-6 Python entry-point defaults and topology-runbook validation commands with the approved October 20 policy already selected by CI, retaining explicit policy overrides. Add behavior tests for default and explicit policy forwarding. Mark the five selected obligations resolved, including both ledger descriptions of the same default-policy defect. Preserve dependency pins, historical evidence and qualification/acceptance decisions.

</frozen-after-approval>

## Implementation Notes

- Investigation confirmed the five obligations in deferred-work.md. This is a bounded tooling change with no unresolved intent gaps, new public API, data mutation or deployment. The one-shot route applies; pushing main is explicitly authorized.
- Code map: tests/tools/run-ci-workflow-gates.ps1 owns catalog checks, direct workflow SHA diagnostics and final failure reporting. Its fixture must run with no submodules initialized. tests/tools/test_run_ci_workflow_gates.py will copy only gate inputs and root-owned project files into a temporary root. .github/workflows/ci.yml will run it before invariant validation.
- tools/qualification/run_g6_current.py and tests/tools/run_g6_ci_gate.py already accept --policy; change only their defaults. Existing adjacent test modules provide mockable audit/subprocess boundaries. docs/runbooks/projects-topology.md documents the runner and validator commands.
- Baseline: PowerShell workflow gate passed; ten existing G-6 CI/runner tests passed. Existing files retain their established line endings; new Python and Markdown files follow the root CRLF convention.

- Regression-first evidence: root-property override controls failed four ways against the baseline, while the checkout control passed; the multi-failure test reproduced the missing second-through-fourth errors.
- The historical candidate gate is the original source of the broad Builds assertion. Its docstring and both success/failure diagnostics now explicitly describe direct references in the selected workflow; adjacent fixtures retain mismatch rejection and demonstrate the nested-reference boundary.

- Independent review found seven actionable observations. The default-policy CI fixture now synthesizes its policy under a temporary workspace, so workflow-gates needs no Builds checkout. Property discovery uses namespace-independent XML traversal. Literal assignment detection covers the tested GitHub environment-file and PowerShell forms, with positive controls for inline comments and echoed assignment text.

## Review Triage Log

- BH1 — high, patched: the new CI default-policy fixture initially read the absent Builds policy in workflow-gates. It now supplies a test-owned policy under a patched temporary ROOT and preserves forwarding assertions.
- BH2 — medium, patched: MSBuild default namespaces hid direct property declarations from the new XPath. Namespace-independent traversal and an executable negative control close it.
- BH3 — medium, deferred: pre-existing local imported props can change effective properties. The bounded gate checks literal declarations in the two root props files and explicit workflow/environment inputs, and does not evaluate arbitrary import closure or dynamically constructed scripts. The ledger resolution now states that boundary; a separate follow-up records the reproduced import bypass.
- BH4 — medium, patched: GitHub multiline environment-file writes were missed. The literal NAME<<delimiter protocol has a real gate fixture.
- BH5 — medium, patched: PowerShell Set-Item Env assignments were missed. Environment-drive and SetEnvironmentVariable patterns have real gate fixtures.
- BH6 — medium, patched: assignment-shaped inline comments caused false failures. The scanner now recognizes assignment contexts, and an inline-comment positive control passes.
- BH7 — medium, patched: echoing assignment-shaped diagnostic text caused a false failure. A separate positive control permits echo without an environment-file write.

## Verification

- `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests/tools/test_run_ci_workflow_gates.py tests/tools/test_run_g6_candidate_gate.py tests/tools/test_run_g6_ci_gate.py tests/tools/test_run_g6_current.py tests/tools/test_release_workflow.py tests/tools/test_git_whitespace_policy.py -v` — 56/56 passed, zero failures or skips (313.901 seconds).
- `pwsh -NoProfile -File tests/tools/run-ci-workflow-gates.ps1` — passed on the final implementation.
- `git diff --check` — passed. New files and previously CRLF-tracked Python files retain CRLF; existing LF files retain LF.
- Commit message validated with commitlint 21.1.0 and the repository commitlint.config.mjs using the existing npm tool cache.
- Independent review: six findings patched and verified; the pre-existing imported-props closure gap is separately recorded. Five selected obligations are closed, including both descriptions of the duplicate G-6 default-policy item.
