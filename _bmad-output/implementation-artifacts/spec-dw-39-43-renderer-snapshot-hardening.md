---
title: 'Complete DW-39 through DW-43 renderer snapshot hardening'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_revision: 'f10129c97def15e32d9ab5780d71f9f72913d7c1'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five open renderer items, DW-39 through DW-43, leave immutable snapshot verification vulnerable to symlink substitution and leave identity calculation, retained review conditions, empty `open_spec` overrides, and source containment insufficiently tested.

**Approach:** Reject linked or nonregular snapshot entries before reading them, centralize identity calculations for reuse by the manifest fixture, and retain independent fixed-vector and byte-integrity checks. Add hermetic CLI fixtures proving surviving review conditions, the empty `open_spec` exception and its nonexception control, and rejected external source links. Preserve normal reuse, generation identity algorithms, exact CLI contracts, existing damaged generations and external targets, and the current allowed internal source-link behavior. Update only the renderer's installer manifest hash and the five completed ledger entries. Preserve all pre-existing worktree edits; the user authorized proceeding and pushing `main`, including its existing local commit, on 2026-10-23.

</frozen-after-approval>

## Implementation Notes

- Investigation confirmed all five ledger entries remain applicable; this is a small tooling-only slice with no unresolved intent questions or implementation-side irreversible operations. The one-shot route applies.
- `_bmad/scripts/render_skill.py`: validate the existing generation directory and every descendant with `lstat` before manifest/output reads; reject links and special files, including dangling generation links; extract identity helpers without changing their algorithms.
- `tests/tools/test_render_skill.py`: reuse the renderer identity helpers, retain independent byte hashing, add fixed identity vectors and CLI fixtures for DW-39/DW-41/DW-42/DW-43. Internal source symlinks remain supported and render into regular files.
- `_bmad/_config/files-manifest.csv`: refresh only the renderer SHA-256. `deferred-work.md`: close DW-39 through DW-43 with evidence; preserve all other entries.
- Baseline: 8/8 renderer tests passed. The new symlink matrix failed against the old implementation, reproducing accepted external-byte substitutions; after implementation, 16/16 tests pass with no skips using `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests/tools/test_render_skill.py -v`.
- Existing Python files retain their established LF format to avoid whole-file churn and keep installer byte hashes consistent. The new spec follows the repository CRLF convention.

- Review follow-up: add a controlled post-rename competing dangling-link fixture, and exercise FIFO substitution for both an output and `manifest.json`. Final suite: 17/17 passing, zero failures/skips.
- Verification: `pwsh -NoProfile -File tests/tools/run-ci-workflow-gates.ps1` passed; `git diff --check` passed; the installer checksum matches the renderer; the Conventional Commit message passed commitlint. Byte comparisons confirm all pre-existing Story 6.4 edits, untracked handoff artifacts and Conversations state remain unchanged.

## Review Triage Log

- R1: medium, defer. Parent-directory symlinks can redirect an otherwise regular generation tree. Confirmed using a relocated temporary generation bucket; this predates DW-39 and requires a separate trusted publication-boundary policy. Recorded in the deferred ledger.
- R2: medium, defer. Concurrent replacement after structural preflight can substitute matching external bytes before output reads. Confirmed by replacing `guide.md` during manifest reading; this predates the fix and needs separately scoped descriptor-based verification. Recorded in the deferred ledger.
- R3: low, patch. The post-rename dangling-link detection branch lacked direct regression coverage. Added a controlled competing-publisher case asserting rejection, preserved link/absent target, and complete staging cleanup.
- R4: low, patch. The FIFO fixture did not establish preflight-before-manifest ordering. Expanded it to test both output and manifest FIFOs without reading or altering either.
- R5: low, reject. Creating symlinks may require Windows privileges, but this repository's required renderer lane runs on Ubuntu (`.github/workflows/ci.yml`, `workflow-gates.runs-on: ubuntu-latest`), as does this verified execution. Platform-wide capability-skip machinery would weaken the required negative controls; no Windows support contract is changed by this slice.
