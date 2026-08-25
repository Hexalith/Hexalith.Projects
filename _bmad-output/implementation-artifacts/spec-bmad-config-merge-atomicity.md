---
title: 'Make BMAD configuration merges failure-atomic'
type: 'bugfix'
created: '2026-08-25'
status: 'in-review'
baseline_revision: '8d2db08f8cc296b8002763faf246903ea98e817a'
review_loop_iteration: 1
followup_review_recommended: false
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/.bmad-loop/runs/20260825-081747-f4a1/bundles/bmad-config-merge-atomicity/intent.md'
warnings: [multiple-goals, oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** `merge-help-csv.py` can rewrite its target before rejecting `--legacy-dir` without `--module-code`, while `merge-config.py` can publish `config.yaml` before discovering that `config.user.yaml` cannot be loaded or converted. These late failures leave partially applied setup state.

**Approach:** Validate related arguments and every input/prospective document before mutation, stage serialized outputs beside their targets, and publish through atomic replacements with coordinated rollback for the configuration pair. Delete legacy files only after publication succeeds, and preserve identical installed/template behavior.

## Boundaries & Constraints

**Always:** Preserve the CLI arguments, JSON success summaries, anti-zombie merge semantics, legacy fallback precedence, file permissions where targets already exist, and exit-code meanings. Resolve and validate all prospective state before creating/replacing target files; keep `config.yaml` and `config.user.yaml` unchanged as a pair after any publication failure; use same-directory temporary files and remove temporary/rollback artifacts on every path; clean legacy files only after successful target publication; synchronize every tracked `.agent`, `.agents`, and `.claude` installed/setup-template/standalone copy and its manifest hash.

**Block If:** The two target paths resolve to the same file, or preserving both targets after a failed coordinated publication is impossible on the detected filesystem.

**Never:** Edit `_bmad-output/implementation-artifacts/deferred-work.md`; delete legacy files before publication completes; truncate a target in place; silently coerce a non-mapping YAML/JSON configuration document; alter unrelated skill behavior or generated render output.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Valid help merge | Valid source/target CSV, optional valid cleanup pair | Target is replaced atomically and legacy CSVs are then removed | Exit 0 with the existing JSON summary |
| Invalid cleanup arguments | `--legacy-dir` without `--module-code` | Target and legacy files remain byte-identical | Exit 1 before any target mutation |
| Invalid prospective config | Either existing target is malformed or not a mapping | Neither config target nor legacy input changes | Nonzero validation/runtime result before publication |
| Config publication failure | Both prospective YAML documents validate, but any staged replace fails | Both targets are restored to their original existence, bytes, and modes; legacy files remain | Exit 2; no staged/rollback files remain |
| CSV publication failure | Prospective CSV validates, but staged replace fails | Target and legacy files remain unchanged | Exit 2; no staged file remains |

</intent-contract>

## Code Map

- `{.agent,.agents,.claude}/skills/bmad-bmb-setup/scripts/merge-help-csv.py:166-229` -- installed CLI copies; argument relationship is currently checked after `write_csv`, and `write_csv` truncates the target directly.
- `{.agent,.agents,.claude}/skills/bmad-bmb-setup/scripts/merge-config.py:324-415` -- installed config CLI copies; direct YAML writes occur before the user document is loaded/converted.
- `{.agent,.agents,.claude}/skills/bmad-module-builder/assets/setup-skill-template/scripts/{merge-config.py,merge-help-csv.py}` -- generated setup-skill sources that must match the installed scripts byte-for-byte.
- `{.agent,.agents,.claude}/skills/bmad-module-builder/assets/standalone-module-template/{merge-config.py,merge-help-csv.py}` -- standalone scaffolding sources that share the same merge behavior and hashes.
- `{.agent,.agents,.claude}/skills/bmad-module-builder/scripts/tests/test-cleanup-legacy.py:562-609` -- reuse the established subprocess/temp-directory and cross-entry-point byte-identity test pattern.
- `.github/workflows/ci.yml` -- root CI currently runs no BMAD merge-script suite; add the canonical synchronized regression suite to a blocking job.
- `_bmad/_config/files-manifest.csv:98-103` -- installed BMAD content hashes for all six logical merge-script assets; add the synchronized regression-test asset entry.
- `.bmad-loop/runs/20260825-081747-f4a1/bundles/bmad-config-merge-atomicity/intent.md` -- read-only bundle authority for DW-6/DW-7.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- orchestrator-owned ledger; read-only for this run.

## Tasks & Acceptance

**Execution:**
- [x] `{.agent,.agents,.claude}/skills/bmad-bmb-setup/scripts/merge-help-csv.py` and both corresponding template paths -- before any read or mutation, require a safe single-component cleanup module code, require it to identify the source module when cleanup is requested, and reject source/target or target/legacy-cleanup aliases; render CSV before mutation, publish with same-directory atomic replacement, preserve existing mode and normal create-mode semantics, classify invalid input as exit 1, tolerate interruption with cleanup, and defer legacy cleanup until publication succeeds.
- [x] `{.agent,.agents,.claude}/skills/bmad-bmb-setup/scripts/merge-config.py` and both corresponding template paths -- require mapping-shaped inputs and a safe non-reserved module code; reject output/output aliases (including hard links and case-insensitive collisions), nested output paths, output/input aliases, and output/legacy-cleanup aliases; compute and serialize both prospective YAML documents before mutation, stage the changed targets, coordinate publication with rollback after ordinary failures or interruption, preserve existing modes and normal create-mode semantics, exhaust artifact cleanup attempts, and perform legacy cleanup only after the pair commits.
- [x] `{.agent,.agents,.claude}/skills/bmad-module-builder/scripts/tests/test-merge-atomicity.py` -- add CLI-level invalid-input/success regressions and injected staging/publication/interruption failures for every installed, setup-template, and standalone copy; cover hard-link/case/nested aliases, unsafe or mismatched module codes, input/cleanup collisions, target bytes/existence/modes, cleanup ordering, temp cleanup, and exact cross-entry-point/manifest identity including unique manifest rows.
- [x] `.github/workflows/ci.yml` -- execute the canonical `.agents` atomicity suite in an existing blocking Python-capable job so future regressions fail normal CI.
- [x] `_bmad/_config/files-manifest.csv` -- replace merge-script hashes and register the new synchronized test hash after final bytes stabilize.

**Acceptance Criteria:**
- Given any tracked merge-script copy and a failure before or during publication, when the command returns, then no target is partially updated and no legacy file has been deleted.
- Given valid inputs for any tracked copy, when the command succeeds, then merged output matches existing anti-zombie/user-setting behavior, uses atomic target replacement, and cleanup occurs afterward.
- Given the completed patch, when synchronization and manifest checks run, then all equivalent script/test copies are byte-identical and every recorded hash matches its tracked asset.
- Given unsafe, mismatched, aliased, case-colliding, or nested path arguments, when either CLI runs, then it exits with validation status before reading cleanup-scoped data, publishing output, or deleting legacy files.
- Given a new target or a pre-existing target with restricted permissions, when publication succeeds, then the new file follows normal process create-mode semantics and the replacement retains the pre-existing permission mode.
- Given staging failure, replacement failure, or interruption during a coordinated publication, when the command unwinds, then the pre-command target/legacy state is retained and cleanup attempts leave no disposable staging artifacts.
- Given a root CI run, when the blocking Python gate executes, then the atomicity suite runs rather than remaining manual-only evidence.

## Spec Change Log

### 2026-08-25 — Review pass 1 repair
- Trigger: adversarial review found that the planning tasks reduced “all argument relationships” to one late cleanup pair and one output-pair collision, leaving module-code traversal/mismatch, output/input and output/cleanup aliases, nested/case-colliding outputs, and unexercised hard-link behavior capable of overwriting or deleting unrelated files.
- Amendment: expanded the Code Map, execution tasks, acceptance criteria, and verification surface to enumerate every destructive argument relationship, create-mode compatibility, interruption/staging failures, unique manifest rows, and blocking CI execution.
- Known-bad state avoided: a locally atomic helper that can still escape cleanup scope, delete its own published target, overwrite an input, create nested-path debris, regress fresh-file modes, or regress later without CI noticing.
- KEEP: preserve early validation; complete prospective-document serialization; same-directory staged replacement; config-pair rollback; byte/mode/existence assertions; all nine installed/template/standalone copies; manifest hash validation; current success/anti-zombie/user-setting behavior; and the read-only deferred ledger.

## Review Triage Log

### 2026-08-25 — Review pass
- intent_gap: 0
- bad_spec: 8: (high 7, medium 1, low 0)
- patch: 8: (high 1, medium 4, low 3)
- defer: 4: (high 1, medium 3, low 0)
- reject: 4: (high 0, medium 2, low 2)
- addressed_findings:
  - `[high]` `[bad_spec]` Require safe module codes in both CLIs so legacy reads/deletes cannot escape their root or collide with `core`.
  - `[high]` `[bad_spec]` Bind help cleanup `--module-code` to the source module instead of permitting publication for one module and deletion for another.
  - `[high]` `[bad_spec]` Reject config and CSV targets that alias legacy cleanup candidates.
  - `[high]` `[bad_spec]` Reject config outputs that alias module/answers inputs.
  - `[high]` `[bad_spec]` Reject output aliases across distinct names, including case-insensitive collisions.
  - `[medium]` `[bad_spec]` Reject ancestor/descendant config targets before staging can create a directory at an absent target path.
  - `[high]` `[bad_spec]` Add blocking CI execution for the new failure-path regression suite.

## Design Notes

The config pair cannot be swapped simultaneously as two directory entries. The required failure-atomic guarantee is implemented by fully staging both replacements, retaining rollback artifacts for pre-existing targets, applying replacements, and restoring already-published targets if a later replacement fails. Each individual target transition remains an atomic same-filesystem replace; the post-command state is all-old or all-new.

## Verification

**Commands:**
- `uv run --no-cache .agents/skills/bmad-module-builder/scripts/tests/test-merge-atomicity.py` -- expected: every success, validation, injected-failure, synchronization, and manifest scenario passes across all tracked copies.
- `uv run --no-cache .agents/skills/bmad-module-builder/scripts/tests/test-scaffold-setup-skill.py` -- expected: setup scaffolding still passes with synchronized templates.
- `python3 -m py_compile $(rg --files --hidden .agent .agents .claude | rg '/(merge-config|merge-help-csv|test-merge-atomicity)\.py$')` -- expected: all modified Python files compile.
- `git diff --check && git status --short` -- expected: no whitespace errors; only the planned scripts, tests, manifest, and workflow spec are changed, with the deferred ledger absent.
