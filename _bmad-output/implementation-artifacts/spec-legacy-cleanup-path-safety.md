---
title: 'Harden BMAD legacy cleanup path and replacement safety'
type: 'bugfix'
created: '2026-08-25'
status: 'done'
baseline_revision: '3a2de9879f4e2d2e4651496e05b6fce8e0d4462d'
review_loop_iteration: 0
followup_review_recommended: true
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/.bmad-loop/runs/20260825-081747-f4a1/bundles/legacy-cleanup-path-safety/intent.md'
warnings: [multiple-goals]
deferred:
  - summary: >-
      Sequential shutil.rmtree calls across multiple validated targets are not
      atomic, so a later target's filesystem failure can leave earlier
      targets already deleted.
    evidence: |-
      cleanup_directories() in all six cleanup-legacy.py copies loops over
      validated targets and calls shutil.rmtree per target inside its own
      try/except; if an OSError/RuntimeError is raised on target N after
      targets 1..N-1 already succeeded, main() exits via runtime_error
      (exit 2) but the already-removed directories are not restored. This
      sequential, non-atomic deletion behavior is unchanged from the
      pre-diff implementation -- the diff only changed which exception
      types are caught and how the failure is reported.
    location: >-
      .agents/skills/bmad-bmb-setup/scripts/cleanup-legacy.py:cleanup_directories
    severity: medium
  - summary: >-
      A symlink cycle nested inside an already-validated cleanup target can
      make find_skill_dirs and count_files raise an unhandled RecursionError
      instead of the documented JSON exit-2 contract.
    evidence: |-
      Both functions catch only (OSError, RuntimeError) around their
      Path.rglob() calls. rglob recurses into symlinked subdirectories, so a
      self-referential symlink inside a target's subtree (not the top-level
      target itself, which is already rejected by the direct-child symlink
      guard in resolve_cleanup_targets) can trigger unbounded recursion. This
      traversal behavior predates this diff -- both functions used the same
      rglob pattern before this change, with no handling for this case
      either.
    location: >-
      .agents/skills/bmad-bmb-setup/scripts/cleanup-legacy.py:find_skill_dirs,count_files
    severity: medium
---

<intent-contract>

## Intent

**Problem:** The BMAD legacy cleanup CLI accepts absolute, nested, traversal, and symlink-escaping target names, then joins them to `_bmad` before recursive scans and deletion. Its replacement guard also treats any same-named directory as an installed skill and can delete live migration data or the sole real skill copy.

**Approach:** Resolve and validate every requested target as a direct child of the resolved `_bmad` root before any target scan, then remove a skill-bearing legacy directory only after proving each replacement is a distinct external directory with a real `SKILL.md`. Preserve fail-closed, all-or-nothing validation and keep installed, template, and agent-entry-point copies synchronized.

## Boundaries & Constraints

**Always:** Validate the complete deduplicated target set before scanning target contents or calling `shutil.rmtree`; retain JSON output and exit codes (`1` validation, `2` runtime); protect directories with unmigrated `config.yaml` or `module-help.csv`; use isolated temporary directories for destructive tests; update the BMAD installed-file manifest hashes for changed and added canonical files.

**Block If:** An existing supported cleanup case requires deleting a live migration file, or repository evidence shows an agent copy intentionally differs from the installed/template implementation.

**Never:** Edit `_bmad-output/implementation-artifacts/deferred-work.md`; scan or delete a target derived from an unvalidated path; accept a replacement inside any cleanup target, a directory without `SKILL.md`, or a symlink/alias to the legacy copy; weaken the existing unresolved-`{project-root}` guard.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Verified duplicate | Direct-child legacy target contains skills; external skills root contains matching directories and `SKILL.md` files | Remove the legacy target and retain replacements | Exit 0 with removed paths and verified skills in JSON |
| Escaping target | Absolute, nested, `..` traversal, or symlink-escaping module/also-remove value | Perform no target scan or deletion; preserve outside sentinel and otherwise-valid targets | Exit 1 with the rejected target in JSON |
| Live migration data | Direct child still contains `config.yaml` or `module-help.csv` | Preserve the complete directory | Exit 1 with protected-directory evidence |
| Missing or false replacement | Replacement is absent, lacks `SKILL.md`, resolves inside a cleanup target, or aliases the legacy copy | Preserve every cleanup target | Exit 1 with missing/unsafe skill evidence |
| Non-skill legacy directory | Valid direct child has no live migration marker and no skills | Remove it as an explicitly requested legacy directory | Exit 0; existing idempotent missing/non-directory reporting remains intact |

</intent-contract>

## Code Map

- `.agents/skills/bmad-bmb-setup/scripts/cleanup-legacy.py:find_skill_dirs, verify_skills_installed, cleanup_directories, main` -- canonical installed CLI behavior; currently constructs unchecked targets and verifies only `is_dir()`.
- `.agents/skills/bmad-module-builder/assets/setup-skill-template/scripts/cleanup-legacy.py` -- byte-identical generated setup-skill template that must receive the same hardening.
- `.agent/skills/**/cleanup-legacy.py` and `.claude/skills/**/cleanup-legacy.py` -- tracked synchronized entry-point copies; keep byte-identical to the corresponding `.agents` files.
- `.agents/skills/bmad-module-builder/scripts/tests/test-scaffold-setup-skill.py` -- local self-running Python test convention and template-path anchor to reuse.
- `.agents/skills/bmad-workflow-builder/scripts/tests/test_canon_sync.py` -- precedent for byte-identity guards across embedded copies.
- `_bmad/_config/files-manifest.csv` -- canonical BMAD inventory whose SHA-256 rows cover both cleanup scripts, setup-skill docs, and module-builder tests.
- `.bmad-loop/runs/20260825-081747-f4a1/bundles/legacy-cleanup-path-safety/intent.md` -- read-only DW-4/DW-5 source; the orchestrator owns ledger resolution.

## Tasks & Acceptance

**Execution:**
- [x] `.agent/skills/bmad-bmb-setup/scripts/cleanup-legacy.py`, `.agents/skills/bmad-bmb-setup/scripts/cleanup-legacy.py`, `.claude/skills/bmad-bmb-setup/scripts/cleanup-legacy.py`, and the three corresponding `bmad-module-builder/assets/setup-skill-template/scripts/cleanup-legacy.py` files -- add pre-scan direct-child resolution, live-marker protection, and distinct real-replacement validation while preserving all-or-nothing deletion.
- [x] `.agent/skills/bmad-bmb-setup/SKILL.md`, `.agents/skills/bmad-bmb-setup/SKILL.md`, `.claude/skills/bmad-bmb-setup/SKILL.md`, and the three corresponding setup-template `SKILL.md` files -- document the tightened cleanup contract without changing setup flow.
- [x] `.agent/skills/bmad-module-builder/scripts/tests/test-cleanup-legacy.py`, `.agents/skills/bmad-module-builder/scripts/tests/test-cleanup-legacy.py`, and `.claude/skills/bmad-module-builder/scripts/tests/test-cleanup-legacy.py` -- add subprocess tests for every matrix row against installed and template scripts plus synchronization assertions.
- [x] `_bmad/_config/files-manifest.csv` -- add the new canonical test row and refresh hashes for every changed canonical BMAD file.

**Acceptance Criteria:**
- Given any module-code or also-remove value that does not resolve to a direct child of `_bmad`, when cleanup runs, then it exits before scanning or deleting any requested target and all sentinels remain.
- Given a cleanup target with live migration data or an unproven/sole skill copy, when cleanup runs, then the target and every other candidate remain unchanged and JSON identifies the protection failure.
- Given only validated targets and distinct external replacements containing matching `SKILL.md` files, when cleanup runs through either installed or template copy, then only the verified legacy duplicates and explicitly safe non-skill directories are removed.
- Given all three tracked agent entry points, when synchronization and destructive-path tests run, then their installed/template implementations and tests are byte-identical and every scenario passes in temporary directories.

## Spec Change Log

- 2026-08-25: Created from the legacy-cleanup-path-safety deferred-work bundle and marked ready for implementation.
- 2026-08-25: Implemented path, migration-data, and replacement safety; added destructive regression coverage; completed adversarial review and verification.

## Review Triage Log

### 2026-08-25 — Parallel review pass

- `intent_gap`: 0 (`high`: 0, `medium`: 0, `low`: 0)
- `bad_spec`: 0 (`high`: 0, `medium`: 0, `low`: 0)
- `patch`: 7 (`high`: 1, `medium`: 5, `low`: 1)
- `defer`: 0 (`high`: 0, `medium`: 0, `low`: 0)
- `reject`: 17 (`high`: 0, `medium`: 8, `low`: 9)

Addressed findings:

- `[high] [patch]` Reject symlink cleanup-target entries, including in-root aliases and resolved target collisions, before any scan or deletion.
- `[medium] [patch]` Convert scan, count, and deletion filesystem failures to the runtime JSON contract with exit code 2.
- `[medium] [patch]` Exercise transactional preservation when live migration data or a missing replacement appears in a later core or `--also-remove` target.
- `[medium] [patch]` Exercise a multi-skill legacy target where one external replacement is missing.
- `[medium] [patch]` Reject and test a replacement located inside a different cleanup target.
- `[medium] [patch]` Cover absolute and traversal escapes through both target argument sources plus symlink and resolved-collision variants.
- `[low] [patch]` Bound every destructive subprocess test with a timeout.

Rejected findings were either outside the bundle's existing CLI contract (caller-selected `_bmad` roots, nested migration-marker semantics, CI expansion), incompatible with legitimate upgraded replacements (full-directory content equality), filesystem race or mount scenarios not solvable by the scoped validation, pre-existing reporting semantics, or redundant/internal test-shape suggestions already proven by behavioral coverage. No deferred-work entries were added or edited.

### 2026-08-25 — Follow-up review pass (blind hunter, edge-case hunter, verification-gap, intent-alignment)

- `intent_gap`: 0 (`high`: 0, `medium`: 0, `low`: 0)
- `bad_spec`: 0 (`high`: 0, `medium`: 0, `low`: 0)
- `patch`: 3 (`high`: 0, `medium`: 1, `low`: 2)
- `defer`: 2 (`high`: 0, `medium`: 2, `low`: 0)
- `reject`: 8 (`high`: 0, `medium`: 1, `low`: 7)

Addressed findings:

- `[medium] [patch]` Widened the narrower `except OSError` clauses in `resolve_cleanup_targets`, `protect_live_migration_data`, and `verify_skills_installed` (four call sites, all six synchronized `cleanup-legacy.py` copies) to `except (OSError, RuntimeError)`, matching every sibling filesystem-inspection call site and keeping the documented JSON exit-2 contract intact on every inspection failure.
- `[low] [patch]` Documented the SystemExit(2) filesystem-error path in the `resolve_cleanup_targets` and `verify_skills_installed` docstrings, which previously described only the SystemExit(1) validation path.
- `[low] [patch]` Added a `dotdot-module`/`dotdot-also` case to `test_escaping_targets_fail_before_any_cleanup` (all three synchronized test copies) covering a bare `..` target, the one escaping form that passes the single-path-component pre-check and depends solely on the `resolved_target.parent != bmad_root` check.

Deferred to the ledger (pre-existing, not caused by this story): non-atomic multi-target deletion in `cleanup_directories`, and an unhandled `RecursionError` from a symlink cycle nested inside a validated target's subtree in `find_skill_dirs`/`count_files`.

Rejected findings: a TOCTOU race between validation and `shutil.rmtree` (already accepted as scoped-validation residual risk in the prior pass); an uncommitted, out-of-scope edit to `_bmad-output/implementation-artifacts/deferred-work.md` marking DW-4/DW-5 done (confirmed via `git show 98749f4` and `git log` that this story's own commit never touched that file -- the edit is separate orchestrator ledger bookkeeping sitting in the same working tree, which this run must not modify, re-open, or commit); missing CI wiring for the new local, self-running test convention (matches the repo's existing precedent for these suites); a speculative future-maintenance risk from local variables assigned only inside one branch of an if/elif chain (not a reachable bug today, since the exception path that would skip that branch always exits the process first); a dropped type annotation on an already-optional parameter; ambiguous `--skills-dir` help wording; a perceived `review_loop_iteration: 0` inconsistency (expected behavior for a fresh follow-up pass, not a defect); and a redundant relative-path-spelling collision test proposal already covered by existing collision-detection behavioral coverage.

### 2026-08-25 — Fresh review pass (blind hunter, edge-case hunter, verification-gap, intent-alignment)

- `intent_gap`: 0 (`high`: 0, `medium`: 0, `low`: 0)
- `bad_spec`: 0 (`high`: 0, `medium`: 0, `low`: 0)
- `patch`: 4 (`high`: 0, `medium`: 1, `low`: 3)
- `defer`: 0 (`high`: 0, `medium`: 0, `low`: 0)
- `reject`: 9 (`high`: 0, `medium`: 0, `low`: 9)

Addressed findings:

- `[medium] [patch]` Added subprocess test coverage for the three previously-untested runtime-error call sites -- `resolve_cleanup_targets`'s `is_symlink()` check and `verify_skills_installed`'s `legacy_path.is_dir()` and replacement `is_dir()/is_file()/is_symlink()` checks -- so a future narrowing of their `except (OSError, RuntimeError)` clauses (the exact regression this story's prior follow-up pass fixed) would now fail a test instead of shipping silently (all six synchronized `cleanup-legacy.py` and three synchronized `test-cleanup-legacy.py` copies).
- `[low] [patch]` Documented in `SKILL.md` (all six installed/template copies) that `--skills-dir` is required whenever a cleanup target still contains legacy skills -- previously only the script's own `--help` text hinted at this, while the prose paragraph described only the supplied-`--skills-dir` path.
- `[low] [patch]` Documented in the `verify_skills_installed` docstring (all six `cleanup-legacy.py` copies) that an unresolvable `--skills-dir` value also raises `SystemExit(1)`, alongside the already-documented missing/unsafe-replacement path.
- `[low] [patch]` Restored the per-skill `--verbose` stderr diagnostic that the original hardening pass silently dropped for missing and unsafe replacements (it survived only for the verified-replacement case) -- all six `cleanup-legacy.py` copies now print a `MISSING`/`UNSAFE` line with the specific reason for every skill that fails verification, matching the pre-diff diagnostic behavior.

Rejected findings: a `RecursionError` from a symlink cycle nested inside a validated target's subtree in `find_skill_dirs`/`count_files`, and non-atomic multi-target deletion in `cleanup_directories` (both already recorded in this spec's `deferred` frontmatter from the prior pass -- re-surfaced by this pass's reviewers on the same, unchanged code, including via an intent-alignment "all-or-nothing" reading divergence that names the same non-atomic-deletion gap); a TOCTOU race between validation and `cleanup_directories`'s deletion loop (already accepted as scoped-validation residual risk in the prior pass, re-surfaced here by the edge-case hunter and the blind hunter); a speculative `UnboundLocalError` risk from local variables assigned only inside the exhaustive `else` branch of an `if unsafe_reason: / elif ... : / else:` chain (already rejected in the prior pass as not a reachable bug -- the chain has no gap for a future branch to skip the assignment without also being caught by the guard); `reject_unresolved_paths` not scanning `--module-code`/`--also-remove` for an unresolved `{project-root}` token (pre-existing, unchanged by this diff, and out of scope -- the intent's Never clause protects only the existing guard's current scope, not an expanded one); no test for the hardcoded `core`/`module_code` defaults colliding with a differently-spelled `--also-remove` duplicate (a duplicate of the prior pass's already-rejected relative-path-spelling collision test proposal); inconsistent JSON error-payload shapes across the three validation functions (each payload's fields are semantically specific to its own failure type by design; `SKILL.md` only requires surfacing the JSON, not a uniform shape); no automated check that `files-manifest.csv` hashes match tracked file content (the spec's own Verification section already covers this via a documented manual `sha256sum` command); a double `rglob` directory walk across `find_skill_dirs` and `count_files` (pre-existing pattern, unchanged by this diff, minor efficiency concern only); and an intent-alignment reading that a replacement should be proven external to all of `_bmad`, not just the current run's cleanup targets (the intent's own Never clause explicitly scopes the replacement-location guard to "inside any cleanup target," so the narrower, implemented reading is the one the intent itself authorizes).

## Design Notes

Treat path validation as a batch precondition: resolve `_bmad`, reject any target whose user value is not one relative name or whose resolved parent is not that root, and only then inspect contents. Treat a replacement as real only when its resolved skill directory is outside every cleanup target, is not the legacy skill directory, and contains a regular `SKILL.md`. Existing validation failures remain transactional: no candidate is removed when any candidate is unsafe.

## Verification

**Commands:**
- `python3 .agents/skills/bmad-module-builder/scripts/tests/test-cleanup-legacy.py` -- expected: installed and template scenarios pass with no writes outside temporary directories.
- `python3 .agent/skills/bmad-module-builder/scripts/tests/test-cleanup-legacy.py && python3 .claude/skills/bmad-module-builder/scripts/tests/test-cleanup-legacy.py` -- expected: synchronized entry-point suites pass.
- `sha256sum .agent/skills/bmad-bmb-setup/scripts/cleanup-legacy.py .agents/skills/bmad-bmb-setup/scripts/cleanup-legacy.py .claude/skills/bmad-bmb-setup/scripts/cleanup-legacy.py .agent/skills/bmad-module-builder/assets/setup-skill-template/scripts/cleanup-legacy.py .agents/skills/bmad-module-builder/assets/setup-skill-template/scripts/cleanup-legacy.py .claude/skills/bmad-module-builder/assets/setup-skill-template/scripts/cleanup-legacy.py` -- expected: all six hashes match the manifest's two canonical cleanup rows.
- `git diff --check` -- expected: no whitespace errors.

## Auto Run Result

### Summary

This run performed a fresh, independent review pass (blind hunter, edge-case hunter, verification-gap, intent-alignment) over the diff already implemented and committed for this spec, per the `status: done` -> fresh-review-pass rule. No code changes from prior passes were reopened. The pass found and patched one real test-coverage gap in the previous pass's own exception-widening fix, two documentation gaps around the new `--skills-dir` requirement, and one dropped `--verbose` diagnostic; every other finding was a duplicate of an already-recorded/rejected issue or judged out of scope by the intent-contract's own wording.

### Files Changed (this pass)

- All six `cleanup-legacy.py` copies (`.agent`, `.agents`, `.claude` x `bmad-bmb-setup` and `bmad-module-builder/assets/setup-skill-template`): restored the per-skill `--verbose` `MISSING`/`UNSAFE` stderr diagnostic for skills that fail verification, and documented in `verify_skills_installed`'s docstring that an unresolvable `--skills-dir` also raises `SystemExit(1)`.
- All six `SKILL.md` copies (installed + setup-skill-template): documented that `--skills-dir` is required whenever a cleanup target still contains legacy skills.
- All three `test-cleanup-legacy.py` copies: extended the `run_runtime_failure` harness with `resolve-target`, `verify-legacy-scan`, and `verify-replacement` modes, and added `test_target_and_skill_inspection_failures_use_runtime_json_contract` to cover the three runtime-error call sites (in `resolve_cleanup_targets` and `verify_skills_installed`) that the prior pass's exception-widening patch left untested.
- `_bmad/_config/files-manifest.csv`: refreshed hashes for the two `SKILL.md` rows, the two `cleanup-legacy.py` rows, and the `test-cleanup-legacy.py` row changed above.
- This spec's frontmatter (`status`) and `## Review Triage Log`; the deferred-work ledger was not read, edited, or re-opened by this run.

### Review Findings Breakdown (this pass)

0 intent gaps, 0 bad-spec findings, 4 patches (0 high, 1 medium, 3 low), 0 deferred, 9 rejected — all 9 rejections were duplicates of findings already recorded/rejected/deferred in this spec's prior two review passes (re-surfaced by fresh reviewers reviewing the same, unchanged diff), or findings the intent-contract's own text places out of scope.

### Follow-up Review Recommendation

`true`. Score for this pass: `3 * 1 (medium) + 1 * 3 (low) = 6`, which meets the `>= 5` threshold (no high-severity patch this pass).

### Verification Performed

- `.agents` cleanup suite: 12 passed, 0 failed (includes the new `test_target_and_skill_inspection_failures_use_runtime_json_contract`).
- `.agent` cleanup suite: 12 passed, 0 failed.
- `.claude` cleanup suite: 12 passed, 0 failed.
- Scaffold setup-skill suite: 7 passed, 0 failed (unaffected by this pass; re-run as a regression check).
- All six cleanup script copies share SHA-256 `e5614f4aa3e437dd90fcc725c09978e57c2a1c5d1862ced12f84033c5a9ab687`; all three test copies share SHA-256 `7011ef32ed2f6e3c50e38e264941e36e2d8599d7e934dfe59d0784eb06bbca7a`.
- All manifest rows for files changed in this pass match the actual tracked file hashes (verified directly with `sha256sum`).
- `git diff --check`: passed after removing a pre-existing trailing blank line at the spec file's EOF (present before this pass's edits; not reintroduced by them).

### Residual Risks

Two pre-existing, intentionally-deferred gaps remain unchanged and are still recorded in this spec's `deferred` frontmatter: non-atomic multi-target deletion in `cleanup_directories` (a failure partway through a multi-target `shutil.rmtree` batch does not restore already-removed targets), and an unhandled `RecursionError` from a symlink cycle nested inside an already-validated target's subtree in `find_skill_dirs`/`count_files`. A TOCTOU window between validation and deletion also remains an accepted, scoped residual risk (unchanged from prior passes). None of these were introduced or worsened by this pass.
