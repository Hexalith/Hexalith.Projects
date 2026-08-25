---
title: 'Make BMAD Loop hooks portable and filename-safe'
type: 'bugfix'
created: '2026-08-25'
status: 'done'
baseline_revision: '8dd12fccd6c0027d1e6aa53bee2199621fc57e69'
baseline_commit: '8dd12fccd6c0027d1e6aa53bee2199621fc57e69'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/_bmad-output/project-context.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** The checked-in Codex BMAD Loop hook commands name one developer's absolute checkout, while the relay interpolates unvalidated task IDs and event names into event filenames. Moved checkouts can therefore lose lifecycle delivery, and separator or traversal input can escape or break the events channel.

**Approach:** Resolve the installed relay from the active Git root, reject unsafe filename components before publication, and cover the relay through temporary-directory subprocess tests that prove portable, canonical, atomic delivery.

## Boundaries & Constraints

**Always:** Preserve the relay's stdlib-only runtime, explicit `BMAD_LOOP_EVENTS_DIR` preference, legacy `BMAD_LOOP_RUN_DIR/events` fallback, silent no-op contract, redirect defenses, exclusive temporary creation, atomic rename, `0o600` mode, and canonical JSON task/event values. Keep changes root-owned and make the new suite a blocking workflow gate.

**Block If:** A fix requires changing the external BMAD Loop orchestrator, a `references/` submodule, a package/dependency version, or the unrelated story-automator hook command.

**Never:** Edit `_bmad-output/implementation-artifacts/deferred-work.md`, sprint status, run-control files, generated skill copies, submodule content, or payload values to make them filename-safe. Never reintroduce checkout-specific absolute paths or weaken existing atomic/link protections.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Interactive no-op | Run directory or task ID absent/empty | Exit `0`; create no event or directory | Silent no-op |
| Current and legacy channels | Valid task/event with explicit, absent, or empty events-dir variable | Prefer explicit directory; otherwise publish once under run `events` | Exit `0` on filesystem refusal |
| CLI payload variants | snake_case, `conversation_id`, camelCase, or `workspacePaths` payload | Emit the existing canonical `session_id`, `transcript_path`, and `cwd` fields | Malformed/non-object payload emits null optional fields |
| Safe attribution | Bounded slug-like task ID and canonical lifecycle event | Filename contains the unchanged safe components; JSON preserves both values exactly | No error expected |
| Unsafe attribution | Separator, traversal, control/unsupported character, invalid edge, or overlong component | Publish nothing inside or outside the events directory | Exit `0` through the relay's failure contract |
| Relocated checkout | Hook config and relay copied to a temporary Git checkout; command launched from a nested directory | Both Codex loop commands resolve and deliver from that checkout | Test fails on any original-workspace dependency |
| Atomic publication | Successful CLI delivery in a temporary events directory | Exactly one complete parseable `.json` exists with no `.tmp` residue and restricted mode on POSIX | Test fails on partial/residual output |

</intent-contract>

## Code Map

- `.codex/hooks.json:14-29` -- the two BMAD Loop command registrations hard-code the current checkout; the story-automator command at lines 3-12 is read-only for this bundle.
- `.bmad-loop/bmad_loop_hook.py:181-224` -- `main()` reads the task/event values and currently builds the filename directly; validation belongs before `_write_event` while payload construction keeps the originals.
- `.bmad-loop/bmad_loop_hook.py:98-178` -- existing exclusive temp-file, redirect-resistant, atomic publish implementation to preserve and exercise, not replace.
- `tests/tools/test_production_authority_guard.py:132-203` -- root precedent for stdlib `unittest`, subprocess CLI checks, temporary directories, clean environments, and asserting CI ownership.
- `.github/workflows/ci.yml:24-49` -- dependency-free workflow-gates job where the new Python relay suite must run before the CI/CD invariant gate.
- `.claude/settings.json:24-64` -- read-only evidence that non-Codex registrations already use a checkout variable and pass canonical event names.

## Tasks & Acceptance

**Execution:**
- [x] `.codex/hooks.json` -- replace only the two BMAD Loop absolute commands with quoted `git rev-parse --show-toplevel` relay paths so session subdirectories and relocated checkouts resolve consistently.
- [x] `.bmad-loop/bmad_loop_hook.py` -- validate task IDs and event names as bounded portable filename components, reject separators/traversal/invalid edges before delivery, and preserve their accepted values in the event payload.
- [x] `tests/tools/test_bmad_loop_hook.py` -- add temporary-directory subprocess coverage for environment selection, payload key variants, canonical attribution, malformed payloads, traversal/invalid-component rejection, atomic cleanup/mode, and relocated nested-directory hook invocation; assert CI retains the suite.
- [x] `.github/workflows/ci.yml` -- run the dependency-free relay suite with bytecode writes disabled in `workflow-gates`.

**Acceptance Criteria:**
- Given a checkout at any absolute path and a Codex session started below its Git root, when `Stop` or `SessionStart` runs, then the checked-in command executes that checkout's relay and publishes the canonical event.
- Given any successful relay publication, when the event is observed, then it is a complete restricted JSON file, contains the exact accepted task ID/event values, and leaves no temporary artifact.
- Given an unsafe task ID or event name, when the relay runs, then it exits successfully without creating a file or escaping the selected events directory.
- Given the root CI workflow, when workflow gates run, then the complete BMAD Loop hook subprocess suite is blocking and dependency-free.

## Spec Change Log

- 2026-08-25: Implemented the portable hook commands, bounded filename-component validation, blocking CI registration, and subprocess acceptance suite.

## Review Triage Log

### 2026-08-25 — Review pass

- intent_gap: 0
- bad_spec: 0
- patch: 0
- defer: 0
- reject: 12 (high 2, medium 4, low 6)
- addressed_findings:
  - none

## Design Notes

Official OpenAI hook guidance says commands run with the session working directory and recommends resolving repository-local scripts from the Git root. The quoted Git-root expression therefore remains stable from subdirectories and paths containing spaces. Accept filename components only when they begin with ASCII alphanumeric text, contain ASCII alphanumerics plus `.`, `_`, or `-`, do not end in `.`, and fit task/event limits of 160/64 characters; with the timestamp and suffix this keeps the filename within 255 bytes. Validation changes only the publication decision, never the canonical payload.

## Verification

**Commands:**
- `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests/tools/test_bmad_loop_hook.py -v` -- expected: all environment, payload, safety, atomicity, and relocation cases pass.
- `python3 -m json.tool .codex/hooks.json >/dev/null` -- expected: hook registration remains valid JSON.
- `pwsh ./tests/tools/run-ci-workflow-gates.ps1` -- expected: workflow policy remains green.
- `git diff --check` -- expected: no whitespace errors.

## Auto Run Result

- Summary: Replaced the two checkout-specific Codex BMAD Loop commands with Git-root-relative commands, rejected unsafe filename components without altering accepted payload values, and added dependency-free CLI regression coverage to the blocking workflow gates.
- Files changed: `.codex/hooks.json`, `.bmad-loop/bmad_loop_hook.py`, `tests/tools/test_bmad_loop_hook.py`, `.github/workflows/ci.yml`, and this implementation spec.
- Review disposition: 0 patches, 0 deferred findings, and 12 rejected findings (2 high, 4 medium, 6 low) because they concerned platform guarantees, pre-existing hardening, or execution surfaces outside this bundle's contract.
- Follow-up review: not recommended; patched-risk score `3×0 + 0 = 0`.
- Verification: 11 relay tests passed; 7 production-authority guard tests passed; hook JSON parsed successfully; workflow gates passed; `git diff --check` passed.
- Residual risk: The configured commands assume Codex launches hooks inside the intended Git checkout with `git` and `python3` available, matching the documented repository-local hook model. Platform-specific command overrides are outside this bundle.
