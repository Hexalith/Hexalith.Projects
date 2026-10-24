---
title: 'Complete five deferred tooling safety items'
type: 'bugfix'
created: '2026-10-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_revision: '5325ad18443f4085b5cc97685e7420a4eee7a286'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five deferred developer-tooling obligations remain: cleanup target containment, blocking cleanup CI coverage, renderer ancestor containment, concurrent snapshot verification, and independent historical G-6 catalog drift controls.

**Approach:** Verify the existing cleanup containment and blocking CI with executable preservation and workflow-mutation checks; anchor renderer publication and snapshot reads to directory descriptors without following links; add independent test-owned package expectations and drift controls around the real historical baseline validator. Close exactly these five entries with dated evidence. Keep durable cleanup/crash recovery, current qualification acceptance, dependency pins, submodule revisions and sprint state outside this change. Commit and push the verified result on main as requested.

</frozen-after-approval>

## Implementation Notes

- Small reversible tooling changes use the one-shot route. No intent gaps, product APIs, domain persistence, deployment or runtime-topology edits. Main publication is explicitly authorized.
- Root guidance, Hexalith baseline/Git guidance, build/editor configuration, project context and Architecture Spine were inspected. Files touched: _bmad/scripts/render_skill.py, tests/tools/test_render_skill.py, tests/tools/test_g6_deferred_contracts.py, tests/tools/test_run_ci_workflow_gates.py, deferred-work.md and this spec; add concise renderer boundary documentation if needed.
- Renderer code map: _publish currently follows ancestor links via mkdir and path-based staging; _verify_existing checks lstat then reopens files by pathname. Use descriptor-relative no-follow directory traversal, reads and rename, reject substitutions and close handles on every failure. Preserve generation identity, immutable reuse, exact file sets and one-line HALT behavior. Platforms lacking the required safe filesystem primitives must fail clearly before publication rather than silently follow links.
- Cleanup source/tests already reject absolute, traversal, nested and linked target names across installed copies. Existing CI and exact-step policy already run the suite; mutation checks must prove omission and tolerated failures are rejected. Partial final deletion and crash recovery remain open.
- Historical Builds fixture constructs its catalog from the validator mapping. New root coverage must supply a separate six-package table, assert the real positive baseline, then require each independent version mutation to fail for its package. Historical validator and retained evidence bytes remain unchanged; these tests grant no qualification acceptance.

- Installation provenance: update only the renderer row in _bmad/_config/files-manifest.csv to its actual SHA-256. Existing LF files retain LF; existing CRLF files retain CRLF.
- Compatibility boundary: no-follow directory descriptor support is required for safe publication/reuse. Linux is exercised here; platforms without these primitives (including native Windows Python) return a clear HALT before publication. Root ancestors are trusted; paths can still be changed after the renderer returns.
- Initial cleanup verification: canonical suite passed 23/23, including target preservation, all installed-copy identity/manifest checks, staging rollback and explicit partial-final-delete limitations. DW-36 and durable recovery stay open.
- Initial test failures were fixture expectations: policy correctly rejected all mutations but the asserted diagnostic was too narrow; the after-read substitution test needed deterministic manifest-last enumeration. Corrected both fixtures without weakening the checks.

## Verification

- `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests/tools/test_render_skill.py -v` — 22/22 passed in 3.984 seconds. Includes eight ancestor link cases, four deterministic in-read substitutions, an after-read substitution and unsupported-platform refusal, alongside all existing renderer controls.
- `PYTHONDONTWRITEBYTECODE=1 python3 .agents/skills/bmad-module-builder/scripts/tests/test-cleanup-legacy.py` — 23/23 passed, zero failures. Existing source and manifest copy checks pass.
- `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests.tools.test_run_ci_workflow_gates.WorkflowGateTests.test_cleanup_suite_cannot_be_omitted_or_made_nonblocking -v` — passed in 28.065 seconds; all five mutations rejected by the real PowerShell policy. The unchanged checkout positive also passed before that run.
- Independent historical catalog method passed: three positive v1 baselines, 18 per-package version drift cases and restored positives, without deriving expectations from the validator mapping.
- `PYTHONDONTWRITEBYTECODE=1 python3 tools/planning/validate_production_authority.py --validate-index` — passed; authority remains [6, 7, 8]. `git diff --check` passed.
- Exactly five existing deferred entries received dated resolved dispositions; remaining obligations, dependencies and sprint/acceptance state are unchanged.

- Full G-6 contract suite passed 3/3 in 79.412 seconds, with the existing 103 mutation controls plus accepted metadata positive and all 18 new independent historical catalog drifts. No tests skipped.

- Byte reconstruction confirms exactly five added ledger dispositions and no other ledger edits. The renderer installation manifest matches its current bytes.
- Fetch confirms main/origin/main divergence 0/0. Repository commitlint accepts the prepared Conventional Commit message; no commit-validation bypass is used.

## Review Triage Log

- BH1 — medium, patched: a staging-name swap immediately before rename could publish a symlink. Hold the staging descriptor through publication, compare the published entry with the owned staging inode and verify the published generation. The exact symlink-swap regression now refuses publication success and preserves outside bytes.
- BH2 — medium, patched: a replaced staging output could bypass reuse-only checks. Verify both the complete staging tree and published generation. A same-byte external workflow symlink injected during manifest creation now halts and leaves the outside file intact.
- BH3 — medium, patched: cleanup could remove a foreign directory installed at a vacated/replaced staging name. Skip cleanup after successful rename; otherwise require the original staging identity. Two regression modes preserve the entire unrelated sentinel tree.
- BH4 — medium, patched: buffered-byte hashing previously followed final handle checks. Hold the generation directory through manifest/hash validation and then re-traverse identities with no-follow directory opens. Hash-time replacement now fails the final identity check.
- BH5 — medium, patched: initial ancestor movement was checked only at context exit, permitting writes through a moved handle before refusal. Check each binding immediately after open and before mutating phases; add an open-time ancestor relocation regression proving unchanged outside bytes. Document the necessary exclusive ancestor-ownership boundary: a renderer cannot atomically prohibit arbitrary relocation by another process with the same permissions. No claim covers adversarial ancestor mutation during individual syscalls or after return.
- BH6 — medium, patched: keeping one handle per file/directory exhausted a small descriptor limit on reuse. Close each file after reading and traverse directories by depth; recheck stored identities after hashing. A 70-directory fixture must both publish and reuse under RLIMIT_NOFILE=48.

- No finding is deferred. Changes remain within the planned renderer safety boundary and existing test files; the trusted-ancestor assumption is now explicit. New tests correct the initial fixture recursion/import setup errors without weakening production checks.
- Catalog sensitivity check: removing each of the six production catalog mappings in memory causes the new test to fail for all three historical baselines. Workflow/whitespace gates and authority index passed before the review corrections; renderer and manifest checks are repeated after them.

## Final verification after review

- Renderer suite passed 29/29 with zero skips after review corrections; this supersedes the earlier 22/28-method counts. The low-descriptor-limit fixture exercises both initial publication and unchanged reuse across 70 nested directories.
- BH3 follow-up: the independent reviewer reproduced a remaining check-to-rmtree race. Pathname recursive cleanup was removed entirely; the held staging descriptor now remains open through cleanup, and recursive deletion stays relative to that handle. A new exact cleanup-entry substitution test preserves the foreign nested tree while clearing only the original displaced staging contents. The final parent-relative rmdir is nonrecursive.
- Reviewer independently reran all three focused cleanup tests and confirmed no remaining issue in the fix; the other five findings were already confirmed addressed. No review finding is deferred.
- Existing cleanup, workflow policy, catalog and whitespace checks remain passing. Renderer manifest hash was refreshed after the final source correction. Five ledger closures remain the only ledger additions; no dependency, gitlink, authority or acceptance state changes.
