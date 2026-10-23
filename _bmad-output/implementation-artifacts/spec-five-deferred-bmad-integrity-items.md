---
title: 'Complete five deferred BMAD configuration integrity items'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_revision: 'cfa97ede0576820896a02d82a02c8daf64826e7b'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Deferred BMAD setup defects let live core answers overwrite module configuration, discard existing user preferences during migration, combine incompatible help-CSV schemas, and import sibling project context into root planning. DW-38 also requests one bounded independent follow-up over the cleanup scripts and tests.

**Approach:** Complete these five obligations on main and push the verified commit as requested. Allowlist live core keys using the established core schema before publication. Migrate existing root or legacy-nested core user preferences into config.user.yaml with explicit answers taking precedence and existing user configuration preserved over shared fallbacks. Normalize legacy after/before help columns to production preceded-by/followed-by names and align source rows by column name; reject ambiguous or malformed schemas before writes or cleanup. Narrow both root planning persistent-context overrides to _bmad-output/project-context.md. Run exactly one independent DW-38 cleanup review and patch only newly substantiated defects in that scope, keeping DW-36 separately tracked. Keep all installed/template copies and manifest hashes synchronized. Extend existing hermetic CI fixtures with success, failure, preservation and root-versus-sibling controls. Preserve unrelated user changes, dependencies, historical evidence and acceptance state.

</frozen-after-approval>

## Implementation Notes

- Investigation confirmed the four unnumbered obligations in deferred-work.md and the explicit DW-38 decision. No unresolved intent gaps, new public API, data migration against this checkout, deployment or dependency changes are needed. The one-shot route applies; commit and push to main are authorized.
- Code map: .agents/skills/bmad-bmb-setup/scripts/merge-config.py and merge-help-csv.py are canonical scripts, with nine identical copies each across .agent/.agents/.claude and setup/standalone templates. Existing publish_config_pair and publish_csv retain failure-atomic publication and legacy cleanup ordering.
- .agents/skills/bmad-module-builder/scripts/tests/test-merge-atomicity.py is the blocking CI suite; preserve its three synchronized copies and update relevant _bmad/_config/files-manifest.csv hashes. Extend tests/tools/test_render_skill.py for root customization behavior. _bmad/custom/bmad-spec.toml and bmad-sprint-planning.toml currently use the broad context glob.
- Cleanup scripts and tests already contain quarantine-based cleanup from later work; DW-38 review must judge the current implementation and preserve the separately tracked DW-36 scope rather than reconstructing obsolete code.
- Existing Python scripts use LF and retain it; new spec uses repository CRLF. The root project context and relevant tracked config, script tests, workflow and historical specs were inspected.

- Regression-first evidence: four new merge regression groups failed against the original scripts (unsafe core accepted, migrated user file absent, reordered source module misidentified, malformed header accepted). Both root-context subcases loaded the synthetic sibling context before the override fix.
- Live answers with unknown core keys now return validation exit 1 before publication; the merge itself also filters to the established core key set. Core-user precedence is explicit answer > existing config.user.yaml > imported legacy fallback > existing shared nested/root fallback. Existing pair publication provides rollback if user-config migration cannot commit.
- Help CSV normalizes old after/before aliases, validates the exact supported column set and row widths, then reorders by names before module identity/filtering. Unknown, duplicate or alias-colliding schemas fail before publication or legacy cleanup; production preceded-by/followed-by is emitted consistently.
- Existing merge suite baseline passed 20/20; focused new regressions passed across nine script copies. Renderer/context suite passed 18/18 and whitespace policy fixtures passed 5/5.
- DW-38 independent review found four vendor-update regressions: path escapes, live migration marker deletion, absent/false/inside-target skill replacements and unhandled scan failures. Repairs restore previous guards while retaining the current quarantine transaction and tests. DW-36 remains separate.

- DW-38 repair: restored the previously proven guard helpers from canonical history while passing validated targets into the existing quarantine transaction. Six script and three cleanup test copies retain LF. AST comparison confirms the transaction body and every original test/helper are preserved; cleanup suite passed 22/22 after manifest refresh.
- Real installed BMB and Loop help assets merged successfully against a temporary copy of the checked-in shared help CSV; the header-only setup template was correctly refused without target mutation.

- Final Blind Hunter review returned four substantiated findings. All are patched: low-level filesystem errors could be hidden by pathlib predicates/rglob; permissive CSV parsing accepted malformed quotes; legacy preference precedence lacked conflicting-input coverage; and reordered target catalogs lacked a positive control.
- Cleanup metadata now uses explicit stat/lstat, treating only absent/non-directory paths as absent. An explicit scandir walk propagates traversal errors and avoids directory symlink traversal; both skill discovery and pre-staging file counting use it. Real-main controls inject underlying scandir/stat/lstat faults and assert JSON exit 2 with complete target, marker, symlink and sibling preservation. The quarantine transaction remains intact.
- CSV reads use strict parsing; unclosed quotes and trailing garbage after a quoted field are rejected before publication and cleanup. Preference fixtures now conflict across explicit, personal, imported-legacy and shared sources for both user keys. CSV fixtures reverse both source and target columns and assert every field by name.

## Review Triage Log

- DW38-C1 — high, patched: absolute/traversal/symlink target values could escape or substitute a sibling. Restored batch direct-child validation and collision refusal before scanning; preserved all-candidate sentinels in subprocess controls.
- DW38-C2 — high, patched: live config.yaml/module-help.csv markers no longer blocked cleanup after the vendor update. Restored full-batch marker protection; both marker kinds and each target position have preservation controls.
- DW38-C3 — high, patched: absent --skills-dir and missing/false/inside-target replacements permitted deleting sole skill copies. Restored mandatory distinct external regular SKILL.md replacement verification with positive and negative controls.
- DW38-C4 — medium, patched: scan/inspection errors escaped the JSON/exit-2 contract. Restored guarded inspection, then strengthened it against underlying error suppression under BH1 below.
- BH1 — high, patched: real-main probes on Python 3.14 hid scandir/stat/lstat errors and deleted an uninstalled skill, live config or a sibling via an internal symlink. Explicit metadata and traversal operations now propagate errors; low-level injected controls prove preservation before any staging.
- BH2 — medium, patched: an unmatched quote in the thirteenth CSV field passed shape validation and permitted publication/cleanup. Strict CSV parsing and source/target malformed-quote preservation controls close the reachable path.
- BH3 — medium, patched: user preference fixtures omitted imported legacy values. Expanded conflicts for both preferences prove the documented four-source precedence across all nine copies.
- BH4 — medium, patched: only source-column reordering was covered. Both catalogs are reordered and each resulting column is asserted against distinct original field values.


## Verification

- `PYTHONDONTWRITEBYTECODE=1 python3 -u .agents/skills/bmad-module-builder/scripts/tests/test-merge-atomicity.py` — final 26/26 test groups passed, covering all nine merge script copies, three suite copies and exact manifest hashes.
- `PYTHONDONTWRITEBYTECODE=1 python3 -u .agents/skills/bmad-module-builder/scripts/tests/test-cleanup-legacy.py` — final 23/23 test groups passed, including real-main underlying filesystem fault controls, installed/template paths, quarantine failure/recovery and six-script/three-suite synchronization.
- `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests/tools/test_render_skill.py -v` — 18/18 passed, including root-versus-sibling planning context for both overrides.
- `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests/tools/test_git_whitespace_policy.py -v` — 5/5 passed.
- `pwsh -NoProfile -File tests/tools/run-ci-workflow-gates.ps1` — passed.
- Real BMB and Loop asset merge subprocesses passed against temporary copies of the current shared catalog after strict parsing was enabled. Header-only template refusal preserved the target. All ten relevant canonical manifest rows verify.
- `git diff --check` — passed. All previously LF Python files retain LF, existing CRLF TOML/manifest files retain their endings, and this new spec uses CRLF.
- Commit subject passes cached commitlint with the repository config and its conventional rules. Fetch confirmed no divergence from origin/main. No submodule changes or dependency updates are included.
- Review completion: four dedicated DW-38 findings and four Blind Hunter findings are patched and verified. No new findings were deferred. DW-36 remains separately open; partial physical deletion during final cleanup is a pre-existing limitation explicitly covered by the retained test.
