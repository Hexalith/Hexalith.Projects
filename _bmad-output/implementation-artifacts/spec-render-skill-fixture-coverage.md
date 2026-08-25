---
title: 'Add shared skill renderer fixture coverage'
type: 'bugfix'
created: '2026-08-25'
status: 'done'
review_loop_iteration: 0
followup_review_recommended: false
baseline_revision: '8c3cf289f05080acf206b9e3a465b3061d506d9b'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/project-context.md'
warnings: []
deferred:
  - summary: >-
      Existing-generation verification follows symlinked output files, so matching external bytes can satisfy an immutable snapshot check.
    evidence: |-
      `_verify_existing` builds its file set with `Path.is_file()` and hashes outputs with `Path.read_bytes()`; both follow symlinks. A symlink to external bytes matching the manifest therefore verifies successfully, while the external target can change after verification without changing the generation directory or manifest.
    location: >-
      _bmad/scripts/render_skill.py:279
    severity: high
  - summary: >-
      The fixture suite's `assert_manifest_integrity` helper re-derives root_hash, generation_hash, and project_slug with its own copy of the renderer's hashing/slugging formulas instead of importing the real functions.
    evidence: |-
      `tests/tools/test_render_skill.py` recomputes `_sha256(str(project_root).encode())[:12]` and `_sha256(_canonical_json(inputs))[:20]` inline rather than calling into `render_skill.py` (already loadable via the file's own `_load_renderer_module()` helper). A regression in the real hashing/slugging algorithm would be mirrored by the test's parallel implementation and pass silently.
    location: >-
      tests/tools/test_render_skill.py:316-358
    severity: medium
  - summary: >-
      The keyed review layer's `when` clause is only ever asserted absent from rendered output, never asserted present for a layer whose `when` clause survives to the final resolution.
    evidence: |-
      `test_layer_precedence_lists_and_keyed_review_replacement` only exercises the negative case (`assertNotIn("Run only when: always", rendered)`) for a layer whose `when` field is dropped by a whole-table override. No fixture keeps a `when` clause on a surviving layer through to final render, so a regression in the `Run only when: {value}` formatting at `render_skill.py:172` would go undetected.
    location: >-
      _bmad/scripts/render_skill.py:171-172
    severity: low
  - summary: >-
      The `customization.workflow.open_spec` field's special-cased empty-string allowance has no fixture coverage.
    evidence: |-
      `_resolve_customization_value` at `render_skill.py:180` hardcodes `label == "customization.workflow.open_spec"` as an exception permitting an empty override even when the default is non-empty. No fixture defines an `open_spec` customization key, so this named carve-out ships with zero coverage.
    location: >-
      _bmad/scripts/render_skill.py:180
    severity: low
  - summary: >-
      No fixture demonstrates the existing render-source escape guard, so a regression there would ship undetected.
    evidence: |-
      `_load_sources` at `_bmad/scripts/render_skill.py:105-106` resolves each candidate source and raises `RenderError(f"render source escapes skill directory: {name}")` when the resolved path is not relative to `skill_dir` (guarding against a symlinked or otherwise escaping source file). `tests/tools/test_render_skill.py` has no fixture that creates such a source and asserts this HALT message, so a regression that weakens or removes the check would pass the current suite silently.
    location: >-
      _bmad/scripts/render_skill.py:105-106
    severity: low
---

<intent-contract>

## Intent

**Problem:** The shared skill renderer has no executable fixture suite in a normal repository gate, so precedence, opaque substitution, immutable publication, cleanup, or fail-closed behavior can regress while product checks remain green.

**Approach:** Add isolated standard-library tests that invoke the checked-in renderer against temporary project and skill fixtures, and make that suite a required workflow-policy gate without changing renderer behavior or installed artifacts.

## Boundaries & Constraints

**Always:** Exercise `_bmad/scripts/render_skill.py` itself through its CLI for end-to-end cases; cover central and customization layer precedence, whole-table keyed review-layer replacement, one-pass placeholder opacity, snapshot binding, manifest/output integrity, immutable reuse, staging cleanup, and corrupt/invalid inputs; keep fixtures hermetic and dependency-free; preserve exact success and `HALT:` CLI contracts.

**Block If:** A fixture proves the current renderer violates the stated contract and resolving it requires a production behavior decision rather than a narrowly demonstrated defect fix.

**Never:** Edit `_bmad-output/implementation-artifacts/deferred-work.md`; mutate installed skill sources, installer-managed `_bmad/config.toml`, or runtime `_bmad/render/`; resurrect a per-skill Quick Dev renderer; weaken corruption checks; add pytest or another Python dependency; update installer manifest hashes unless a proven renderer defect requires an explicitly justified source change.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Layered render | Four central layers and default/team/user customization layers | Highest applicable scalar wins; unkeyed lists append in order; keyed review layers replace in place or append | No error expected |
| Opaque render | Inserted prose contains config, workflow, runtime, and snapshot-shaped placeholders | Inserted text remains literal; only `{skill-root}` is rebound; source-authored snapshots resolve absolutely | Undeclared source-authored snapshots halt |
| Immutable reuse | Identical fixture rendered twice, then an input changes | Identical run verifies and reuses bytes/path without rewriting; changed identity creates a new generation and preserves the old | Corrupt existing generations are never repaired in place |
| Failed publication | Rename fails after staging is populated | Destination is absent and all `.staging-*` residue is removed | Original failure propagates or is reported as `HALT:` |
| Invalid/corrupt fixture | Missing or malformed sources/config plus damaged manifest/output/file set | CLI exits 1, emits the matching `HALT:` failure class, and publishes nothing new | Existing damaged evidence remains untouched |

</intent-contract>

## Code Map

- `_bmad/scripts/config_utils.py:57` -- keyed arrays replace matching `code`/`id` entries as whole tables; lines 98-119 define central and customization precedence.
- `_bmad/scripts/render_skill.py:72` -- review-layer validation and disabled-layer formatting; lines 192-267 resolve source tokens in one opaque pass and deliberately bind customization `{skill-root}`.
- `_bmad/scripts/render_skill.py:270` -- existing-generation manifest, exact file-set, and hash verification; lines 295-320 own atomic staging cleanup; lines 322-397 expose the renderer and CLI contracts.
- `tests/tools/test_bmad_loop_hook.py:22` -- reusable repository pattern for dependency-free subprocess tests, temporary fixtures, exact CLI assertions, and CI-wiring checks.
- `tests/tools/test_render_skill.py` -- new owner for synthetic renderer fixtures; keep all writes inside temporary directories and invoke the real script by absolute path.
- `.github/workflows/ci.yml:24` -- `workflow-gates` is the normal, submodule-free gate for root Python policy/tooling tests.
- `tests/tools/run-ci-workflow-gates.ps1:59` -- workflow policy assertions must require the renderer fixture command so the gate cannot silently disappear.
- `_bmad/_config/files-manifest.csv:429`, `.agents/skills/**`, and `_bmad/render/**` -- read-only installed/generated surfaces for this test-only bundle.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- orchestrator-owned ledger; do not edit.

## Tasks & Acceptance

**Execution:**
- `tests/tools/test_render_skill.py` -- add a `unittest` fixture harness that runs the real CLI and validates layered rendering, keyed review overrides, opaque placeholders, snapshot/manifest contracts, immutable reuse, staging cleanup including forced rename failure, and table-driven invalid/corrupt cases.
- `.github/workflows/ci.yml` -- run the renderer suite in `workflow-gates` with bytecode generation disabled so it is part of every normal push and pull-request gate.
- `tests/tools/run-ci-workflow-gates.ps1` -- require the exact renderer-suite command and retain the existing workflow invariants.

**Acceptance Criteria:**
- Given only Python 3.11+ and a clean checkout without initialized submodules, when the renderer fixture suite runs, then all specified success, reuse, cleanup, and fail-closed behaviors execute against `_bmad/scripts/render_skill.py` and pass without repository writes.
- Given the checked-in CI workflow, when the workflow-policy gate runs, then it proves the renderer suite is a blocking `workflow-gates` step with `PYTHONDONTWRITEBYTECODE` enabled.
- Given the completed change, when repository differences are inspected, then no installed skill, renderer/config implementation, generated snapshot, installer manifest, dependency file, submodule, or deferred-work ledger has changed.

## Spec Change Log

## Review Triage Log

### 2026-08-25 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 3: (high 0, medium 3, low 0)
- defer: 1: (high 1, medium 0, low 0)
- reject: 14: (high 2, medium 11, low 1)
- addressed_findings:
  - `[medium]` `[patch]` Added a 30-second renderer subprocess timeout so a deadlock fails the focused gate instead of stalling the job.
  - `[medium]` `[patch]` Extended opaque substitution coverage through a nested non-entry Markdown source and nested snapshot/output path.
  - `[medium]` `[patch]` Scoped both CI-presence checks to `workflow-gates` and required the exact blocking renderer step with no appended skip properties.

### 2026-08-25 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 4: (high 0, medium 2, low 2)
- defer: 3: (high 0, medium 1, low 2)
- reject: 10: (high 0, medium 0, low 10)
- addressed_findings:
  - `[medium]` `[patch]` Added a `test_missing_bmad_directory_halts_before_publishing` case so the `_bmad/`-missing HALT guard is covered like the rest of the corrupt/invalid-input matrix.
  - `[medium]` `[patch]` Scoped the CI-policy test's `continue-on-error` check to the renderer step's own text instead of the whole `workflow-gates` job body, so an unrelated future step change can no longer fail it under a misleading name.
  - `[low]` `[patch]` Added explicit `encoding="utf-8"` to the renderer subprocess call so a non-UTF-8 default locale can't turn a clean assertion into a `UnicodeDecodeError`.
  - `[low]` `[patch]` Trimmed a trailing blank line this spec file had accumulated at EOF, restoring a clean `git diff --check`.

### 2026-08-25 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 0
- defer: 1: (high 0, medium 0, low 1)
- reject: 15: (high 0, medium 0, low 15)
- addressed_findings:
  - none

## Design Notes

Use temporary paths containing spaces and subprocess argument lists so path handling is exercised without shell quoting. Assert generation identity structurally rather than hardcoding hashes because the absolute temporary project root participates in the identity. A focused direct-import test may mock `os.rename` solely to force the post-staging cleanup branch; all externally observable scenarios remain CLI-driven.

## Verification

**Commands:**
- `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests/tools/test_render_skill.py -v` -- expected: every synthetic renderer fixture passes with no bytecode or workspace generation.
- `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tools -p 'test_*.py' -v` -- expected: all root Python tooling suites pass together.
- `pwsh ./tests/tools/run-ci-workflow-gates.ps1` -- expected: workflow policy, including the required renderer command, passes.
- `git diff --check` -- expected: no whitespace errors.

## Auto Run Result

Status: done

**Summary:** This was a fresh, no-code-change review pass over the already-implemented story (prior status: `done`). No implementation, spec, or workflow file needed a fix; one new low-severity test-coverage gap was identified and deferred.

**Files changed in this pass:**
- `_bmad-output/implementation-artifacts/spec-render-skill-fixture-coverage.md` -- status transitions (`done` -> `in-review` -> `done`), one new `deferred` entry, this pass's `## Review Triage Log` entry, and this `## Auto Run Result` section.

No other file changed in this pass. The story's substantive change set (unchanged from the prior run) remains:
- `.github/workflows/ci.yml` -- adds the blocking `Validate shared skill renderer` step to `workflow-gates`.
- `tests/tools/run-ci-workflow-gates.ps1` -- requires the exact renderer-suite step text in `workflow-gates`.
- `tests/tools/test_render_skill.py` -- new hermetic `unittest` fixture suite for `_bmad/scripts/render_skill.py`.

**Review findings breakdown (this pass):**
- patch: 0
- defer: 1 (low) -- no fixture exercises the existing render-source escape guard (`_bmad/scripts/render_skill.py:105-106`, `RenderError("render source escapes skill directory: ...")`); a regression there would ship undetected. Added to spec `deferred` frontmatter.
- reject: 15 -- speculative or already-mitigated edge cases (test-only hashing already matches the renderer's own approach; corrupted-vs-missing manifest.json hits the same code path already covered; concurrent-render races the renderer already tolerates via its rename/verify fallback; CLI-argument misuse, Unicode-content coverage, and generation garbage-collection are outside the spec's stated scope; the `os.rename` mock is explicitly sanctioned by this spec's Design Notes; the duplicated CI job-boundary regex in the `.ps1` gate and the Python meta-test is the deliberate double-layered gate the intent calls for, and the "duplicate `workflow-gates:` job key" scenario both flag is invalid YAML that cannot occur in a working workflow file; the remaining style/documentation suggestions do not change behavior). Four additional intent-alignment observations restated the four items already present in the spec's `deferred` list from prior passes (symlink-following existing-generation verification, the fixture's parallel hashing/slugging re-derivation, the untested surviving `when` clause, and the untested `open_spec` carve-out) -- no new action taken since they are already tracked there.

**Follow-up review recommendation:** `false`. This pass patched 0 findings (high 0, medium 0, low 0); score = 3x0 + 1x0 = 0.

**Verification performed:** No code changed, so the story's verification suite was not re-run in this pass; the prior pass's recorded results stand. Frontmatter `deferred` list re-parsed as YAML after the append (5 items, all prior items preserved) to confirm serialization integrity.

**Residual risks:** The five deferred items (one high, one medium, three low) remain open pre-existing/coverage gaps, tracked in this spec's frontmatter for later focused attention; none block this story.
