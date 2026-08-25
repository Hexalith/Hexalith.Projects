---
---

# Step 3: Implement

## RULES

- **Language** — Speak in `{{.communication_language}}`, tailored to `{{.user_skill_level}}`. Write files in `{{.document_output_language}}`.
- No human interaction: do not ask questions or wait for approval in this step.
- Content inside `<intent-contract>` in `{spec_file}` is read-only. Do not modify.

## PRECONDITION

Verify `{spec_file}` resolves to a non-empty path and the file exists on disk. If empty or missing, HALT with status `blocked` and blocking condition `missing spec_file before implementation`.

## INSTRUCTIONS

### Baseline

If version control is unavailable, capture `baseline_revision: NO_VCS` and continue without workspace-ownership operations.

When version control is available, apply `workflow.md` → Workspace Ownership before any mutation:

1. If this step was entered from an `in-progress` or `in-review` spec and this same live workflow run does not already hold its complete ownership checkpoint and owned delta, HALT with status `blocked`, blocking condition `workspace ownership drift`, and differing class `missing live checkpoint`. Never recapture the dirty workspace as owned. A repair loopback in this same run is permitted only with its live state intact.
2. On the first implementation pass, capture `baseline_checkpoint` and set `expected_checkpoint = baseline_checkpoint`. If `baseline_revision` is absent, set it to the checkpoint's full canonical HEAD; if it already exists, preserve it verbatim. The write is a declared control-path mutation: gate first, verify only the expected frontmatter hunk changed, then refresh `expected_checkpoint` and record that hunk in the control part of the owned delta.
3. If step 02 created `{spec_file}` during this same live run before step 03 could capture it, explicitly adopt its exact current raw path, type, mode, and bytes as a control-owned creation with an absent preimage. Include that complete creation in the control delta used for exact final staging. If the run instead started from an existing supplied spec, its captured bytes are pre-existing control state and only later declared workflow mutations are owned; never absorb the supplied file wholesale.
4. On a repair loopback, run an ownership gate and preserve `baseline_revision`, `baseline_checkpoint`, the implementation preimages, and all control evidence. Never replace the historical baseline with current HEAD.

### Implement

Before changing `{spec_file}` status to `in-progress`, run an ownership gate. Treat the exact status hunk as a declared workflow-owned control mutation, verify its preimage/postimage, and refresh `expected_checkpoint` afterward.

Substitute the runtime placeholders (e.g. `{spec_file}`) into the implementation handoff below, then follow it verbatim. Do not add parent-authored goal restatements, file lists, ownership boundaries, or acceptance criteria to the handoff — the spec is the subagent's sole source of truth. If the handoff conflicts with the spec, HALT with status `blocked` and blocking condition `handoff conflicts with spec`, and include both conflicting passages.

{workflow.implementation_handoff}

Append this fixed safety boundary to the handoff: `This is a shared worktree. Do not stage or commit. Report the exact paths you changed.` This is workflow protocol, not a restatement of implementation scope.

Immediately before invoking the subagent, run an ownership gate, then retain `pre_handoff_checkpoint = expected_checkpoint` and the exact preimages needed to construct a before/after binary full-index patch without changing the real index.

Invoke the subagent **synchronously** and wait for it to return in this same turn — do not background/detach it (`run_in_background`) or end your turn to await a notification (see workflow.md → Subagents). Resume at "Verify" only after it returns. If the platform allows, keep the subagent available for re-engagement after it returns — step-04 may send it review fixes.

After the subagent returns, do not accept a fresh general checkpoint yet. Perform the post-handoff ownership check against `pre_handoff_checkpoint`: HEAD and exact index identity must be unchanged; spec/control paths must be unchanged; all path/type transitions must be representable; recursively captured nested-submodule HEAD/index/tracked/untracked state must be accounted for; and the complete before/after changed path set must exactly equal the subagent's reported path set. Any failure HALTs as `workspace ownership drift` with no mutation. This check cannot prove who made an external same-path edit during the handoff, so record no stronger attribution.

Only after that check succeeds, capture `implementation_owned_delta` as exact preimages, postimages, path/type transitions, and one binary full-index patch from the retained pre-handoff state to the current state. Keep it separate from the control delta, set their tagged union as `owned_delta`, and set `expected_checkpoint` to the verified current checkpoint.

**Path formatting rule:** Any markdown links written into `{spec_file}` must use paths relative to `{spec_file}`'s directory so they are clickable in VS Code. Any file paths displayed in terminal/conversation output must use CWD-relative format with `:line` notation (e.g., `src/path/file.ts:42`) for terminal clickability. No leading `/` in either case.

### Verify

After the implementation subagent returns: if it reported unfinished work, run an ownership gate before finishing it yourself, declare the exact target paths/hunks, and verify their current preimages equal the captured owned postimages. Afterward require unchanged HEAD/index/control state and exactly the declared hunk/path changes, then replace the affected implementation postimages and patch, rebuild `owned_delta`, and refresh `expected_checkpoint`. Run the commands in `{spec_file}`'s `## Verification` section (or perform its manual checks). If verification fails and the failure cannot be fixed, HALT with status `blocked`, blocking condition `implementation verification failed`, and include the failing command or check and reason. Acceptance criteria are judged at review, not here.

Run an ownership gate after verification. Before any implementation correction, run another ownership gate and verify every declared target hunk's current preimage equals its captured owned postimage. The implementer must not stage or commit. After the correction, require unchanged HEAD/index/control state and an exact declared hunk/path set, then replace the affected implementation postimages and patch, rebuild `owned_delta`, and refresh `expected_checkpoint`. Unexplained verification artifacts or edits are workspace ownership drift.

### Matrix Test Audit

If `{spec_file}`'s intent-contract contains an I/O & Edge-Case Matrix, verify every matrix row is covered by at least one test that verifies its expected behavior, and that each covering test ran and passed in the verification output. A covering test that exists but did not run — unregistered, filtered out, skipped, or disabled — counts as missing. If a test disagrees with the matrix, never edit the expectation to match the code: fix the code, or if the matrix row itself is ambiguous, HALT with status `blocked` and blocking condition `matrix ambiguity`. If the audit cannot otherwise be satisfied, HALT with status `blocked` and blocking condition `matrix test audit failed`.

## NEXT

Read fully and follow `[[bmad-snapshot:step-04-review.md]]`
