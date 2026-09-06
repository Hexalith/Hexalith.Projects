---
title: 'Make BMAD multi-target legacy cleanup failure-atomic'
type: 'bugfix'
created: '2026-09-06'
status: 'done'
baseline_revision: 'd5c6396823d56f227c7ade6a8eef34a091c6a4b3'
review_loop_iteration: 0
followup_review_recommended: true
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/.bmad-loop/runs/20260906-095906-a8e8/bundles/failure-atomic-legacy-cleanup/intent.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-legacy-cleanup-path-safety.md'
warnings: [oversized]
deferred:
  - summary: >-
      Concurrent G-6 qualification changes are not reconstructable from the root
      repository while their submodule work remains uncommitted.
    evidence: |-
      The review diff records dirty gitlinks for Builds and five consuming
      submodules, while the packet binds working-tree files absent from the
      recorded gitlink revisions. This work appeared after this run's clean-tree
      gate and is owned by a separate concurrent workflow.
    location: >-
      references/Hexalith.Builds
    severity: medium
  - summary: >-
      Several concurrent G-6 validation claims lack retained machine-readable
      result evidence.
    evidence: |-
      commands.json describes some builds and test executions only in prose,
      without retained result/log artifacts that independently substantiate the
      stated outcomes. This is unrelated to DW-36 and must be resolved by the G-6
      owner.
    location: >-
      _bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain/commands.json
    severity: medium
  - summary: >-
      The scheduled Dapr runtime assertion is not scoped to the scheduled E2E step.
    evidence: |-
      tests/tools/run-ci-workflow-gates.ps1 searches all of ci.yml for
      runtime-version 1.18.2, so the reusable-workflow dapr-runtime-version input
      can satisfy the check even if the scheduled dapr-init input disappears.
      This belongs to the concurrent G-6 workflow.
    location: >-
      tests/tools/run-ci-workflow-gates.ps1:210
    severity: medium
  - summary: >-
      A releasable push SHA does not run the upgraded live AppHost topology.
    evidence: |-
      The live AppHost smoke is schedule-only, while release eligibility uses the
      successful push CI run for the exact SHA. A topology startup regression can
      therefore remain green until the later scheduled lane; this is part of the
      separate runtime-toolchain change.
    location: >-
      .github/workflows/ci.yml:e2e
    severity: high
  - summary: >-
      Current cleanup target handling still accepts escaping and overlapping paths.
    evidence: |-
      Absolute/traversal names can resolve outside _bmad, and aliases or nested
      targets can be counted twice or force rollback. The atomic staging change
      preserves the current target-validation boundary; restoring the previously
      implemented direct-child guard is separate from DW-36.
    location: >-
      .agents/skills/bmad-bmb-setup/scripts/cleanup-legacy.py:cleanup_directories
    severity: high
  - summary: >-
      Resolving the _bmad root can bypass structured runtime-error JSON.
    evidence: |-
      Path(bmad_dir).resolve() is outside the cleanup error handlers, so a
      resolution failure can emit a traceback rather than exit-2 JSON. This
      filesystem-inspection contract gap predates the requested multi-target
      deletion atomicity behavior.
    location: >-
      .agents/skills/bmad-bmb-setup/scripts/cleanup-legacy.py:cleanup_directories
    severity: medium
---

<intent-contract>

## Intent

**Problem:** `cleanup_directories()` deletes validated targets one at a time, so a filesystem failure on a later target can leave earlier targets permanently removed while the CLI reports exit code 2. This violates batch failure atomicity and can destroy recoverable legacy state.

**Approach:** Preflight the complete removable batch, move every target into a unique staging directory under `_bmad` using reversible same-filesystem renames, and treat completion of all moves as the logical commit. Roll back every staged target after any pre-commit failure; after commit, delete the staging tree and retain/report its recoverable state if physical cleanup fails.

## Boundaries & Constraints

**Always:** Preserve the current JSON-on-stdout contract, exit codes, target order, not-found handling, and successful file counts; create staging on the target filesystem; attempt rollback for every staged target in reverse order; expose any pending recovery directory and target mapping on exit 2; keep all installed/template scripts and all agent-entry-point tests byte-identical; use temporary directories and byte-level sentinels in destructive tests.

**Never:** Edit the deferred-work ledger; delete an original target before a reversible staging location exists; use a cross-device copy fallback; remove a staging directory that still contains recovery data; claim rollback or final deletion succeeded when filesystem operations failed; broaden this bundle into unrelated legacy path-validation or skill-replacement behavior.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Successful batch | Multiple existing targets plus missing/non-directory entries | Existing directories are staged, physically removed, and reported in request order with the preflight file count | Exit 0; no transaction directory remains |
| Staging failure | Rename of target N fails after earlier targets were staged | Every earlier target is restored byte-for-byte at its original path; later targets remain untouched | Exit 2 JSON names the staging phase, failed target, restored targets, and any pending recovery state |
| Incomplete rollback | A staging failure is followed by a restore failure for one staged target | Restoration continues for all other staged targets and recoverable data is retained | Exit 2 JSON includes the recovery directory and original/staged mapping for each unrestored target |
| Final cleanup failure | All target moves commit, then recursive deletion of the staging tree fails | Original paths stay logically removed and surviving staged contents remain available for recovery | Exit 2 JSON names the final-cleanup phase and recovery directory |
| Empty batch | No requested entry is an existing directory | Preserve current not-found results without creating staging state | Exit 0 |

</intent-contract>

## Code Map

- `.agents/skills/bmad-bmb-setup/scripts/cleanup-legacy.py:count_files,cleanup_directories` -- canonical installed implementation; lines 149-197 currently interleave counting and irreversible `shutil.rmtree` calls.
- `.agents/skills/bmad-module-builder/assets/setup-skill-template/scripts/cleanup-legacy.py` -- canonical generated setup-skill template; currently byte-identical to the installed implementation.
- `.agent/skills/bmad-bmb-setup/scripts/cleanup-legacy.py`, `.agent/skills/bmad-module-builder/assets/setup-skill-template/scripts/cleanup-legacy.py`, `.claude/skills/bmad-bmb-setup/scripts/cleanup-legacy.py`, `.claude/skills/bmad-module-builder/assets/setup-skill-template/scripts/cleanup-legacy.py` -- synchronized agent entry points that must receive the exact canonical bytes.
- `.agents/skills/bmad-module-builder/scripts/tests/test-scaffold-setup-skill.py` -- local dependency-free, self-running temporary-directory test convention.
- `.agents/skills/bmad-module-builder/scripts/tests/test-cleanup-legacy.py` plus `.agent` and `.claude` counterparts -- new synchronized hermetic fault-injection and byte-identity suites; this path existed in the earlier hardening change but is absent from the current BMAD 6.12.0 tree.
- `_bmad/_config/files-manifest.csv:cleanup-legacy rows` -- canonical installed-file inventory; refresh the two script hashes and add the canonical test row.
- `_bmad-output/implementation-artifacts/spec-legacy-cleanup-path-safety.md:deferred DW-36 evidence` -- read-only design continuity; its ledger remains orchestrator-owned.

## Tasks & Acceptance

**Execution:**
- `.agents/skills/bmad-bmb-setup/scripts/cleanup-legacy.py`, `.agents/skills/bmad-module-builder/assets/setup-skill-template/scripts/cleanup-legacy.py`, `.agent/skills/bmad-bmb-setup/scripts/cleanup-legacy.py`, `.agent/skills/bmad-module-builder/assets/setup-skill-template/scripts/cleanup-legacy.py`, `.claude/skills/bmad-bmb-setup/scripts/cleanup-legacy.py`, `.claude/skills/bmad-module-builder/assets/setup-skill-template/scripts/cleanup-legacy.py` -- replace sequential deletion with preflight, reversible staging, all-target rollback, and recoverable finalization while retaining the CLI contract.
- `.agents/skills/bmad-module-builder/scripts/tests/test-cleanup-legacy.py`, `.agent/skills/bmad-module-builder/scripts/tests/test-cleanup-legacy.py`, `.claude/skills/bmad-module-builder/scripts/tests/test-cleanup-legacy.py` -- add hermetic success, multi-target staging-failure, incomplete-rollback, final-cleanup-failure, byte-identity, and manifest-hash coverage for installed and template scripts.
- `_bmad/_config/files-manifest.csv` -- record SHA-256 for both changed canonical scripts and the new canonical test file.

**Acceptance Criteria:**
- Given at least three targets with distinct byte sentinels, when staging target N raises a filesystem error, then the CLI exits 2 and every target is present at its original path with byte-identical contents.
- Given one restore operation also fails after a pre-commit staging error, when rollback runs, then every other staged target is restored and JSON identifies the surviving recovery directory and exact unrestored mapping.
- Given all targets have been staged, when final physical deletion raises a filesystem error before removing the injected fixtures, then the CLI exits 2, original target paths remain absent, and JSON points to byte-identical recoverable staged contents.
- Given the synchronized test is run from each agent entry point, when it inspects installed/template scripts, peer tests, and manifest entries, then all six scripts are byte-identical, all three tests are byte-identical, hashes match the manifest, and every destructive scenario stays inside its temporary directory.

## Spec Change Log

## Review Triage Log

### 2026-09-06 — Review pass
- verdicts: 26 findings — high 2, medium 18, low 1, false 5, maybe-false 0
- findings:
  - `[medium]` `[defer]` BH-01: concurrent submodule changes are represented only as dirty gitlinks — verified in six root gitlinks; the separate G-6 workflow must commit and bind them before delivery.
  - `[false]` `[reject]` BH-02: release passes an unsupported runtime input to the immutable Builds revision — current release.yml omits that input and the workflow gate explicitly forbids it.
  - `[medium]` `[defer]` BH-03: CI consumes mutable Builds main while the packet hashes a dirty local checkout — verified, but introduced by the concurrent G-6 workflow rather than DW-36.
  - `[medium]` `[defer]` BH-04: packet validator/schema files are absent from the recorded Builds gitlink — verified while Builds remains dirty; grouped with the concurrent qualification commit gap.
  - `[medium]` `[defer]` BH-05: source-state records base revisions and file hashes but no committed resulting revisions — verified and grouped with the concurrent qualification commit gap.
  - `[false]` `[reject]` BH-06: approval is only an unsigned editable string — the G-6 record binds explicit user approval through the approved baseline hash, and neither its intent nor DW-36 requires signature infrastructure.
  - `[medium]` `[defer]` BH-07: several retained command strings are prose or contain placeholders — verified in commands.json; the separate G-6 owner must decide the required replayability contract.
  - `[medium]` `[defer]` BH-08: several claimed G-6 outcomes lack retained machine-readable results — verified for the stated builds/test summaries and unrelated to this cleanup bundle.
  - `[false]` `[reject]` BH-09: mutation controls omit identity/result tampering — current G-6 tests and its triage record identify twelve controls including self-consistently rehashed replay, identity, and result drift.
  - `[medium]` `[defer]` BH-10: the scheduled runtime-pin regex can match the reusable-workflow input — verified at run-ci-workflow-gates.ps1:210 and left to the concurrent G-6 owner.
  - `[medium]` `[patch]` BH-11: the cleanup regression suite was absent from CI — added a blocking workflow-gates step for the canonical suite and a test asserting its placement.
  - `[medium]` `[defer]` BH-12: aliases and overlapping cleanup targets can fail staging or inflate counts — verified, but rooted in the pre-existing missing direct-child/distinct-target validation rather than DW-36.
  - `[low]` `[reject]` BH-13: an abrupt process termination can lose the in-memory numeric-path mapping — process-kill durability is outside the explicit caught-failure contract, and a durable journal would add disproportionate lifecycle complexity.
  - `[medium]` `[defer]` BH-14: _bmad root resolution can escape the JSON error contract — verified, but it is a pre-existing filesystem-inspection error-contract gap outside multi-target deletion atomicity.
  - `[medium]` `[patch]` BH-15: final-cleanup injection never exercised partial deletion — added a mode that deletes one staged target before raising and asserts only surviving byte-valid mappings are reported.
  - `[high]` `[defer]` EC-01: absolute or traversal targets can escape _bmad — verified and grouped with the pre-existing target-validation gap.
  - `[medium]` `[defer]` EC-02: duplicate or overlapping targets can produce rollback or count anomalies — verified and grouped with the pre-existing target-validation gap.
  - `[medium]` `[patch]` EC-03: recovery-path probes can suppress the original final-cleanup JSON — probe failures now retain conservative mappings and appear in recovery_probe_errors, with injected coverage.
  - `[medium]` `[defer]` EC-04: the scheduled runtime assertion is not step-scoped — verified and grouped with BH-10 under the concurrent G-6 workflow.
  - `[medium]` `[defer]` EC-05: _bmad resolution failures can return a traceback instead of exit-2 JSON — verified and grouped with BH-14.
  - `[medium]` `[patch]` EC-06: verbose stderr failures can strand or misclassify staged targets — cleanup diagnostics are now best-effort and injected staging/rollback diagnostic failures are covered.
  - `[medium]` `[patch]` VG-01: no normal CI gate ran the atomic-cleanup suite — pre-verified; the new blocking workflow-gates step and placement assertion close the gap.
  - `[high]` `[defer]` VG-02: releasable push SHAs do not exercise the upgraded live topology — pre-verified, but this belongs to the concurrent G-6 runtime-toolchain workflow.
  - `[medium]` `[patch]` VG-03: the final-cleanup test could not detect stale mappings after partial deletion — pre-verified; partial physical deletion now runs and checks surviving mappings and bytes.
  - `[false]` `[reject]` IA-01: the change must restore every target after a partially completed final deletion — the verbatim intent explicitly separates all-target pre-commit rollback from post-commit preservation/reporting of recoverable state; the patched test now exercises real partial progress.
  - `[false]` `[reject]` IA-02: unrelated G-6 files show this implementation diverged in scope — the initial clean-tree evidence and live status show those files arrived from a separate concurrent workflow; this run neither authored nor adopted them.

## Design Notes

The staging directory is created beneath `_bmad`, guaranteeing the same filesystem for normal target renames. Numeric staged names decouple recovery safety from caller-provided directory spellings, while an explicit original-to-staged mapping supports deterministic reverse rollback and actionable recovery JSON. Once every rename succeeds, logical removal is committed; a later recursive-delete failure must not attempt to reconstruct a possibly partially deleted batch.

## Verification

**Commands:**
- `PYTHONDONTWRITEBYTECODE=1 python3 .agents/skills/bmad-module-builder/scripts/tests/test-cleanup-legacy.py` -- expected: every installed/template transactional and synchronization scenario passes.
- `PYTHONDONTWRITEBYTECODE=1 python3 .agent/skills/bmad-module-builder/scripts/tests/test-cleanup-legacy.py && PYTHONDONTWRITEBYTECODE=1 python3 .claude/skills/bmad-module-builder/scripts/tests/test-cleanup-legacy.py` -- expected: peer entry-point suites pass byte-identically.
- `PYTHONDONTWRITEBYTECODE=1 python3 .agents/skills/bmad-module-builder/scripts/tests/test-scaffold-setup-skill.py` -- expected: setup-skill scaffolding remains green.
- `git diff --check` -- expected: no whitespace errors.

## Auto Run Result

### Summary

Implemented failure-atomic multi-target legacy cleanup. The CLI now preflights
the complete batch, stages targets with same-filesystem renames, restores every
staged target after pre-commit failure, commits only after all moves succeed,
and retains actionable recovery state when final physical cleanup fails.

### Files Changed

- All six `cleanup-legacy.py` installed/template copies under `.agent`,
  `.agents`, and `.claude` -- synchronized transactional staging, rollback,
  finalization recovery reporting, and best-effort verbose diagnostics.
- All three `bmad-module-builder/scripts/tests/test-cleanup-legacy.py` copies --
  synchronized hermetic success and injected-failure coverage, byte-identity
  checks, manifest verification, and CI-wiring assertion.
- `.github/workflows/ci.yml` -- blocking canonical legacy-cleanup atomicity step
  in `workflow-gates`; concurrent G-6 hunks in this file were preserved and are
  not part of this bundle.
- `_bmad/_config/files-manifest.csv` -- refreshed both canonical cleanup-script
  hashes and added the canonical cleanup-test hash.
- This specification -- plan, full review triage, deferrals, verification, and
  completion evidence.

### Review Findings Breakdown

- Patched entries: 4 medium. Added CI execution (BH-11/VG-01), partial-final
  deletion coverage (BH-15/VG-03), conservative recovery-probe handling
  (EC-03), and transaction-independent verbose diagnostics (EC-06).
- Deferred entries: 6 groups covering concurrent G-6 commit/evidence/live-gate
  work plus pre-existing cleanup target-validation and root-resolution gaps.
  The machine-readable frontmatter records their evidence and ownership.
- Rejected findings: BH-02 was already refuted by the current immutable-release
  input guard; BH-06 requested unrequired signature infrastructure; BH-09 was
  refuted by the current twelve mutation controls; BH-13 proposed durable
  process-kill journaling outside the caught-failure contract; IA-01 contradicted
  the intent's explicit pre-commit versus final-cleanup boundary; IA-02 attributed
  independently arriving concurrent G-6 files to this bundle despite the clean
  activation baseline.

### Follow-up Review Recommendation

`true`. This first pass patched four medium-severity entries. The specific
unverified risk is that the post-review changes to conservative recovery-probe
reporting and best-effort diagnostics have comprehensive injected tests but have
not received a fresh independent review pass.

### Verification Performed

- Canonical `.agents` cleanup suite: 11 passed, 0 failed.
- `.agent` cleanup suite: 11 passed, 0 failed.
- `.claude` cleanup suite: 11 passed, 0 failed.
- Setup-skill scaffold regression suite: 7 passed, 0 failed.
- Matrix audit: all five I/O rows have registered tests that ran and passed.
- All six scripts share SHA-256
  `72432178aad6d55067e8c4f9592aa469ea9bf2c4463bf39561fd1262e3657c53`.
- All three tests share SHA-256
  `319256991fdb191b923f472d0b730c36adee8d3984b60d8ca3d3a452d1ce617b`.
- `git diff --check`: passed.

### Residual Risks

A recursive final deletion can remove some staged targets before failing; the
CLI now reports only surviving byte-valid recovery targets and cannot recreate
already physically deleted data. Abrupt process termination between staging
moves is not durably journaled. Pre-existing escaping/overlapping-target and
root-resolution JSON gaps remain deferred and unchanged. Concurrent G-6 work
and its orchestrator-owned ledger edit remain outside this bundle and were not
modified by this run.
