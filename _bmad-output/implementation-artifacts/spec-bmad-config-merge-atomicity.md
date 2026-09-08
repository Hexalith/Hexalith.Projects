---
title: 'Make BMAD configuration merges failure-atomic'
type: 'bugfix'
created: '2026-08-25'
status: 'done'
baseline_commit: '6f29dce9b5aac103fcd04830f198de6da60ced2d'
baseline_revision: '8d2db08f8cc296b8002763faf246903ea98e817a'
review_loop_iteration: 2
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
- [x] `.github/workflows/ci.yml` -- execute the canonical `.agents` atomicity suite in an existing blocking Python-capable job so future regressions fail normal CI. Do not leave a step that invokes a path that is not yet in the tree. Do not rewrite later workflow-gates, project-gates, package-gates, scheduled E2E, or dispatch-only `release.yml` work except to add or pin this suite.
- [x] `_bmad/_config/files-manifest.csv` -- replace merge-script hashes and register the new synchronized test hash after final bytes stabilize. Do not rewrite unrelated catalog rows.

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

### 2026-09-08 — Review pass 2 repair
- Trigger: resumed review of the current tree found that `c2f60e9` no longer exists in the merge scripts after `5fe125e` (`fix: update BMAD 6.12.0`). `merge-help-csv.py` still writes the target at line 216 and only then rejects `--legacy-dir` without `--module-code`; `merge-config.py` still writes `config.yaml` at line 407 before loading `config.user.yaml`; `test-merge-atomicity.py` is absent from all three entry-point trees; `ci.yml:59-65` invokes that missing path; manifest rows 98-103 still hash the pre-change `fcc3687…` / `a222fd5…` bytes.
- Amendment: reset every execution task to incomplete; require re-derivation from the current vendor script bytes (hashes `fcc3687bf6628279d4c035e29d2dc192163314aa217c425d905a5becc63b02c2` and `a222fd5b9856fa2cca0d345f08fa1a50d7313f9c6bc97c6c3bd6fbf6a502a71a`); forbid a dangling CI invoke; forbid rewriting later workflow, cleanup-legacy, release.yml, or unrelated manifest rows.
- Known-bad state avoided: closing an `in-review` spec whose tasks are checked while late in-place writes remain; re-deriving by rolling back later CI/E2E/manifest work; leaving `workflow-gates` pointed at a file that is not in the tree.
- KEEP: keep `reject_unresolved_paths`; keep anti-zombie merge plus JSON success summaries and exit codes 0/1/2; keep PEP 723 `pyyaml` on `merge-config.py`; keep all nine copies byte-identical; keep later workflow-gates siblings (production-authority, loop hook, renderer, whitespace, workspace-ownership, cleanup-legacy, `run-ci-workflow-gates.ps1`); keep project-gates, package-gates, scheduled E2E, and dispatch-only `.github/workflows/release.yml`; keep `test-cleanup-legacy.py`; keep the read-only deferred ledger; keep unrelated manifest rows; keep early validation, prospective serialization, same-directory staged replacement, config-pair rollback, byte/mode/existence assertions, and current success/user-setting behavior when re-applying atomicity.

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

### 2026-09-08 — Review pass 2
- intent_gap: 0
- bad_spec: 1 group (high): vendor overwrite left late in-place writes, no suite, dangling CI invoke, stale merge hashes
- patch: 0 (moot; loopback)
- defer: 0 processed (moot; loopback)
- reject: 7

| Verdict | Route | Finding | Evidence |
| --- | --- | --- | --- |
| high | bad_spec | BH-1 / EC-4 / EC-5 / VG-3: merge CLIs still mutate before validation | `.agents/.../merge-help-csv.py:216-226` calls `write_csv` (open `"w"` at 117) then rejects `--legacy-dir` without `--module-code`. `.agents/.../merge-config.py:407-415` writes `config.yaml` then loads user YAML. Current sha256 still `fcc3687…` / `a222fd5…`. |
| high | bad_spec | BH-2 / EC-2 / EC-6 / EC-7 / VG-1: CI runs a suite that is not in the tree | `ci.yml:59-65` invokes `.agents/skills/bmad-module-builder/scripts/tests/test-merge-atomicity.py`. That path is absent under `.agent`, `.agents`, and `.claude`. `run-ci-workflow-gates.ps1` does not pin the step. |
| high | bad_spec | BH-3 / EC-8: manifest still records pre-change merge hashes and has no test row | `_bmad/_config/files-manifest.csv:98-103` still `fcc3687…` / `a222fd5…`; no `test-merge-atomicity` row. Those hashes match the current non-atomic scripts. |
| high | bad_spec | BH-13: no cross-copy identity gate | CI would only invoke the `.agents` suite. The suite file does not exist, so the required nine-copy / three-test identity check is absent. |
| low | reject | BH-5: venv/pip has no lockfile | GitHub Actions `-e` fails the step if `venv` or `pip` fails. `pyyaml==6.0.3` is pinned. Adding uv/lockfile is extra machinery, not a demonstrated user-facing defect. |
| false | reject | BH-4: cleanup-legacy catalog/CI is the wrong family | `test-cleanup-legacy.py` and its CI step belong to the later legacy-cleanup story and are the correct gate for that family. |
| false | reject | BH-6: cleanup step should share the merge venv | `test-cleanup-legacy.py` has no PyYAML import. A bare `python3` invoke is enough. |
| false | reject | BH-7 / EC-3: release job deleted with no replacement | In-workflow `release` was moved to dispatch-only `.github/workflows/release.yml`, which `run-ci-workflow-gates.ps1:114-157` now requires. |
| false | reject | BH-9: E2E has no host-side evidence channel | `ci.yml:226-235` uploads `tests/e2e/test-results` and `playwright-report` on failure. |
| false | reject | BH-12: new catalog rows have no matching files | `.agents/skills/bmad-walkthrough/SKILL.md` exists. The scoped diff omitted already-present files. |
| defer | moot | BH-8 / BH-10 / BH-11 / BH-14 / EC-1 / VG-2: later CI/E2E/G-6/ownership/manifest work | Not caused by this story. Loopback makes defer processing moot. |

### 2026-09-08 — Review pass 3
- intent_gap: 0
- bad_spec: 0
- patch: 5 groups (high 2, medium 3)
- defer: 3
- reject: 12
- prior pass-2 rows: not carried; late-write / missing-suite claims no longer match the tree

| Verdict | Route | Finding | Evidence |
| --- | --- | --- | --- |
| high | patch | BH-2: help cleanup identity ignores the module column | `validate_source_identity` only requires a `{code}-setup` skill. `run()` already requires one module-column value but never checks it equals `--module-code`, so cleanup can delete `{legacy}/{module-code}` after publishing a different column. |
| high | patch | EC nested-help claim: help CLI never rejects nested paths | Config uses `is_nested_path` at `merge-config.py:372-373`. Help `run()` only calls `reject_alias`. The suite's help variants omit `nested`. Spec AC requires nested arguments to fail on either CLI. |
| medium | patch | BH-4 / EC encoding claims: decode errors are not ValidationError | `load_yaml_file` / `load_json_file` catch only YAML/JSON errors. `read_csv_rows` catches nothing. `UnicodeDecodeError` and `csv.Error` traceback instead of exit 1. |
| medium | patch | VG-1: result templates have no CLI success case | `apply_result_templates` at `merge-config.py:174-195` is untested. `prepare_config` writes no `result:` key. |
| medium | patch | VG-2 / VG-3: unresolved `{project-root}` and config anti-zombie stale key are unpinned | Suite has no `{project-root}` argv case. `prepare_config` plants `demo.stale` but `test_valid_existing_merges` never asserts it is gone. |
| medium | defer | BH-1: live `answers.core` is not allowlisted | Vendor `merge_config` still writes every non-user core key. Preserved success behavior, not introduced by atomicity. |
| medium | defer | BH-3: help header vocabulary can drift from production columns | `HEADER` still uses `after`/`before`. That is the vendor document shape this story preserved. |
| medium | defer | BH-11: stripped root user keys are not copied into user YAML | `extract_user_settings` only reads answers. Same vendor behavior as before this story. |
| low | reject | BH-5 / BH-10 / EC SystemExit helper | Venv lockfile, CI step order, and `int(SystemExit.code)` are unlikely everyday defects whose fixes add machinery. |
| false | reject | BH-6 / EC crash after first replace | Design Notes already accept two sequential `os.replace` calls. SIGKILL mid-pair is that limitation. |
| false | reject | BH-7 stage_text double-close | `os.fdopen` owns the fd; the later `os.close` in `except` is guarded by `OSError`. |
| false | reject | BH-8 / EC legacy unlink TOCTOU | After successful publish, a vanished legacy file is a race the program never has to treat as validation. |
| false | reject | BH-9 case-insensitive resolve | Fail-closed symlink confinement is the intended guard. |
| false | reject | BH-12 skill-row constraint / EC FIFO / YAML vanish races / non-mapping nested `core` | Documented fail-closed identity, loud OSError on exotic targets, and preserved vendor `core` migration. |
| false | reject | EC-3 / BH-4 leftover from pass 2 about missing files | Those paths now exist and hashes match. |

## Design Notes

The config pair cannot be swapped simultaneously as two directory entries. The required failure-atomic guarantee is implemented by fully staging both replacements, retaining rollback artifacts for pre-existing targets, applying replacements, and restoring already-published targets if a later replacement fails. Each individual target transition remains an atomic same-filesystem replace; the post-command state is all-old or all-new.

## Verification

**Commands:**
- `uv run --no-cache .agents/skills/bmad-module-builder/scripts/tests/test-merge-atomicity.py` -- expected: every success, validation, injected-failure, synchronization, and manifest scenario passes across all tracked copies.
- `uv run --no-cache .agents/skills/bmad-module-builder/scripts/tests/test-scaffold-setup-skill.py` -- expected: setup scaffolding still passes with synchronized templates.
- `python3 -m py_compile $(rg --files --hidden .agent .agents .claude | rg '/(merge-config|merge-help-csv|test-merge-atomicity)\.py$')` -- expected: all modified Python files compile.
- `git diff --check && git status --short` -- expected: no whitespace errors; only the planned scripts, tests, manifest, and workflow spec are changed, with the deferred ledger absent.
