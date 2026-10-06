---
title: 'Reconcile CRLF whitespace policy'
type: 'bugfix'
created: '2026-08-27'
status: 'done'
baseline_revision: 'd9dbf9e1f53f26e9a6b23d241bab0112c0fcae4c'
baseline_commit: 'd9dbf9e1f53f26e9a6b23d241bab0112c0fcae4c'
review_loop_iteration: 0
followup_review_recommended: false
context: []
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** The repository requires CRLF for ordinary files, but it has no tracked Git whitespace policy, so an ordinary `git diff --check` incorrectly reports the required carriage returns as trailing whitespace.

**Approach:** Add a repository-wide Git whitespace attribute that permits CR only at end of line, and add a hermetic executable gate proving required CRLF is accepted while genuine whitespace defects still fail.

## Boundaries & Constraints

**Always:** Keep `.editorconfig` authoritative for line endings: ordinary files remain CRLF and the existing shell-script, Dockerfile, YAML, and YML LF exceptions remain unchanged. Use tracked repository configuration, preserve Git's default checks for trailing blanks, blank lines at EOF, and spaces before tabs, and make the gate runnable locally and in CI without external dependencies.

**Block If:** Block if Git's tracked attribute mechanism cannot distinguish a CR that is part of CRLF from genuine trailing whitespace, or if the test cannot exercise the real `git diff --check` command in an isolated repository.

**Never:** Do not edit the deferred-work ledger or bundle evidence. Do not change `.editorconfig`, set developer-local/global Git configuration, add line-ending normalization attributes, weaken real whitespace detection, initialize submodules, update dependencies, stage, or commit.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Required CRLF | A changed ordinary text file whose lines end in CRLF | Plain `git diff --check` exits successfully | Test fails with captured Git output if CR is diagnosed |
| Real trailing blank | A changed CRLF line containing a space before CRLF | Plain `git diff --check` exits nonzero and reports trailing whitespace | Test fails if Git accepts the defect or reports no diagnostic |
| LF exception | Shell, Dockerfile, YAML, and YML paths governed by existing `.editorconfig` exceptions | The tracked Git policy sets no `eol` attribute and therefore does not override their LF policy | Test fails if repository attributes prescribe an EOL value |

</intent-contract>

## Code Map

- `.editorconfig:1` -- authoritative CRLF default; lines 13-21 define the existing LF exceptions that must remain byte-for-byte unchanged.
- `.gitattributes` -- currently absent; create the narrow tracked `whitespace=cr-at-eol` policy here without adding `text` or `eol` normalization.
- `tests/tools/test_git_whitespace_policy.py` -- new dependency-free `unittest` gate; use a temporary Git repository and the checked-in attribute file to exercise the actual Git command boundary.
- `.github/workflows/ci.yml:20` -- `workflow-gates` already runs focused Python policy tests; add the new whitespace-policy test beside those gates.
- `_bmad-output/implementation-artifacts/spec-prevent-submodule-skill-loading.md:62` -- read-only provenance showing the temporary `git -c core.whitespace=cr-at-eol` workaround and the deferred repository-wide conflict.
- `.bmad-loop/runs/20260827-123511-8e94/bundles/crlf-whitespace-policy/intent.md` -- read-only bundle intent and verbatim DW-19 evidence; never modify it.

## Tasks & Acceptance

**Execution:**
- [x] `.gitattributes` -- define `whitespace=cr-at-eol` for repository paths while leaving EOL conversion unspecified -- reconcile ordinary Git checks with intentional CRLF without altering checkout normalization or LF exceptions.
- [x] `tests/tools/test_git_whitespace_policy.py` -- add hermetic positive, negative, and non-interference coverage using a temporary initialized Git repository -- prove the tracked configuration is consumed by plain `git diff --check` and still rejects real defects.
- [x] `.github/workflows/ci.yml` -- execute the focused unittest in `workflow-gates` -- prevent regressions in the repository policy.

**Acceptance Criteria:**
- Given the repository's tracked whitespace configuration and an ordinary file changed with required CRLF endings, when plain `git diff --check` runs without `-c core.whitespace=...`, then it exits zero.
- Given the same tracked configuration and a changed CRLF line with a trailing space, when plain `git diff --check` runs, then it exits nonzero with a trailing-whitespace diagnostic.
- Given the existing LF exception patterns in `.editorconfig`, when Git attributes are queried for representative shell, Dockerfile, YAML, and YML paths, then no `eol` value is configured and the editor policy remains unchanged.
- Given a clean CI checkout, when the workflow-policy job runs, then the focused whitespace-policy unittest is executed successfully without submodule initialization or third-party packages.

## Spec Change Log

- 2026-08-27: Implemented the tracked whitespace policy, hermetic Git command tests, and CI gate.
- 2026-10-06: Completed the resumed build by isolating Git configuration, attributes, templates, repository overrides, and diagnostic locale in subprocess environments. Removed the unnecessary temporary baseline commit, added default-whitespace and inherited-setting regression coverage, and preserved module, direct-file, and discovery execution. The existing root attribute and CI gate required no changes; the original baseline identifiers remain unchanged.

## Review Triage Log

Review on 2026-10-06 used the preserved baseline and the task's attribute, test, CI, and spec paths. The complete workspace baseline diff was captured separately; it includes unrelated later work. The edge-case and verification-gap reviewers reported no findings. The blind-hunter findings were classified individually below; none requires a whitespace-policy patch or a deferred-work entry.

| Finding | Verdict | Evidence and disposition |
|---------|---------|--------------------------|
| BH-1: Unrelated dependency and CI changes appear in the review diff. | false | The historical baseline includes later CI changes, but this build changes neither `.github/workflows/ci.yml` nor dependency configuration. No prohibited dependency update was performed; reject the claim of a deviation by this implementation. |
| BH-2: The shared CI package lane omits the SourceTools declaration checks. | medium | The pinned shared workflow executes Python packaging tools without the declaration checks in `tests/tools/run-package-dependency-gate.ps1:42-55`; `release.config.cjs` still executes that script before publication. This is a pre-existing CI coverage gap for analyzer-reference configuration, independent of the whitespace intent; reject as outside that intent. The ledger remains untouched as required. |
| BH-3: The test would not detect future EditorConfig changes. | low | The gate tests Git attributes rather than EditorConfig syntax, so a separate future editor-policy edit would not fail it. This build preserves `.editorconfig` byte-for-byte (SHA-256 `b703637e2bf829f7628ac63a6b11761e55a4e23a6d29e4520ca1a262cf06d4ea`), and its diff from the preserved baseline is empty. Reject additional configuration guards for hypothetical edits to the explicitly excluded file. |
| BH-4: LF exception paths have attribute checks but no dedicated behavioral test. | low | The suite verifies the same exact whitespace value and unspecified `eol`/`text` on shell, Dockerfile, YAML, and YML paths. Additional isolated real-command checks on `.sh` and `.yaml` files accepted valid LF content and rejected trailing spaces. No LF defect was demonstrated; reject redundant coverage rather than adding test complexity. |
| BH-5: Nested evidence attributes override the root policy. | low | The existing `qualification-evidence/g-6-current-20261003/.gitattributes` intentionally uses `text eol=lf` and `*.log whitespace=-trailing-space` for hash-bound historical evidence. Those pre-existing overrides remain unchanged and are excluded by the intent's prohibition on editing bundle evidence. Reject the proposed spec edit; the hermetic gate proves the root policy. |

## Design Notes

Git's `whitespace=cr-at-eol` attribute augments the default whitespace rules for matching paths: CR at line end is accepted, while trailing blanks and the other default error classes remain active. Avoid `text` and `eol` attributes because this bundle concerns diagnostics, not content normalization.

The gate removes inherited Git environment overrides and disables external configuration and attributes only in child processes. A temporary empty template prevents developer templates from adding repository settings or hooks. The staged temporary fixture provides the plain diff baseline without requiring a commit or author identity. An adversarial child run checks isolation without relying on the unittest import name.

## Verification

**Commands:**
- `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests/tools/test_git_whitespace_policy.py -v` -- expected: all policy scenarios pass using isolated temporary repositories.
- `git check-attr whitespace eol -- AGENTS.md tests/e2e/run-live-apphost.sh Dockerfile .github/workflows/ci.yml` -- expected: `whitespace` is `cr-at-eol` for every path and `eol` is unspecified.
- `git diff --check` -- expected: no whitespace errors in the implementation diff under the tracked policy.

**Results (2026-10-06):**
- `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests/tools/test_git_whitespace_policy.py -v` -- passed all five tests, including the child run with conflicting inherited Git settings.
- `PYTHONDONTWRITEBYTECODE=1 python3 tests/tools/test_git_whitespace_policy.py -v` -- passed all five tests through the executable entry point.
- `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tools -p test_git_whitespace_policy.py -v` -- passed all five tests through discovery.
- The documented module command also passed in a temporary minimal checkout containing only the root attribute and test, using Python `-B -S` to disable bytecode and site packages; no `references/` directory was present.
- `git check-attr whitespace eol text -- AGENTS.md tests/e2e/run-live-apphost.sh scripts/example.bash scripts/example.zsh Dockerfile .github/workflows/ci.yml configuration/example.yaml` -- every path reported `whitespace: cr-at-eol`, `eol: unspecified`, and `text: unspecified`.
- `git diff --check` -- passed. The policy and test retain CRLF, and `.editorconfig` is byte-identical to its pre-build contents.
- Matrix audit: required CRLF acceptance, real trailing-space rejection, and LF exception non-interference all ran and passed. Negative coverage additionally rejects trailing tabs, blank lines at EOF, spaces before tabs, and extra carriage returns.
- All three independent review layers completed; no in-scope finding remains and no work was deferred. No workspace staging or commit was performed.
