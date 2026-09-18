---
---

# Step 3: Implement

## RULES

- **Language** — Speak in `{{.communication_language}}`, tailored to `{{.user_skill_level}}`. Write files in `{{.document_output_language}}`.
- No human interaction: do not ask questions or wait for approval in this step.
- Content inside `<intent-contract>` in `{spec_file}` is read-only. Do not modify.
- Shared-worktree implementation never stages or commits. Do not run `git add -A`, `git add --all`, broad `git reset`, `git checkout`, `git restore`, `git clean`, or `revert code changes`.

## PRECONDITION

Verify `{spec_file}` resolves to a non-empty path and the file exists on disk. If empty or missing, HALT with status `blocked` and blocking condition `missing spec_file before implementation`.

## INSTRUCTIONS

### Ownership session

Route with a workflow-state helper that fails closed without this invocation's live_ownership_session. Do not recapture a dirty tree as owned.

- **No version control:** set `baseline_revision` to `NO_VCS` if it is unset, then continue at Implement. Do not invent an ownership snapshot.
- **Interrupted resume:** if `{spec_file}` is `in-progress` or `in-review` and this invocation has no live_ownership_session, HALT with status `blocked` and blocking condition `workspace ownership drift: missing live ownership session`. Perform no repository mutation.
- **Bad-spec loopback:** keep the original `baseline_revision`. Revalidate `expected_workspace`. Do not absorb drift by replacing the historical review base.
- **Orchestrator re-drive:** if HEAD, index, or worktree carry any staged or unstaged spec, status, result-section, ledger, or other control-plane bookkeeping, HALT with status `blocked` and blocking condition `workspace ownership drift: orchestrator re-drive handoff`. Do not adopt, discard, stage, commit, or normalize the rearm delta.
- **First pass:** continue at Capture expected workspace only when HEAD, index, and worktree are clean and aligned. A dirty first-pass tree HALTs with status `blocked` and blocking condition `workspace ownership drift: dirty first-pass entry`.

### Capture expected workspace

When version control is available, capture `expected_workspace` before any implementation write, and only from that clean aligned HEAD/index/worktree. Use a bounded stable double capture: observe twice and require identical observations. A missing, incomplete, unreadable, raced, or unsupported path type HALTs as `workspace ownership drift` with the differing class and no mutation.

The checkpoint must include:

- the full canonical `HEAD`
- the exact index bytes and only the actually referenced split-index shared-index bytes
- NUL-safe porcelain-v2 records and raw path classes
- binary, full-index tracked and index hunks
- stable no-follow identities for every tracked and untracked path (type, mode, and content or symlink-target hash), so skip-worktree and path filters cannot hide changed bytes
- recursively observed initialized gitlink and nested-submodule HEAD, nested index bytes, nested gitlinks, and nested worktree path identities (type, mode, and content); a gitlink path that is a symlink or non-directory is uninitialized and must not be followed
- separately named spec, result, patch, triage, ledger, and other control-path identities even when ignored, including executable control byte and type

Treat the same-run newly planned spec as an exact control-owned creation or mutation. Retain its pre-checkpoint body in finalization inputs. It is not a reversible implementation hunk.

### Capture baseline_revision

If `{spec_file}` has no `baseline_revision`, write the full canonical `HEAD` into that field as a declared_mutation: revalidate `expected_workspace`, write only that provenance field, recapture, confirm only the declared spec field changed, then replace `expected_workspace`. Never refresh `baseline_revision` later to absorb a commit, index change, worktree edit, control-file edit, or submodule advance.

### Mark in-progress

Change `{spec_file}` status to `in-progress` only as a declared_mutation after the ownership session exists. Revalidate `expected_workspace` immediately before the write, then replace the checkpoint.

### Implement

Substitute the runtime placeholders (e.g. `{spec_file}`) into the implementation handoff below, then follow it verbatim. Do not add parent-authored goal restatements, file lists, ownership boundaries, or acceptance criteria to the handoff — the spec is the subagent's sole source of truth. If the handoff conflicts with the spec, HALT with status `blocked` and blocking condition `handoff conflicts with spec`, and include both conflicting passages.

{workflow.implementation_handoff}

Invoke the subagent **synchronously** and wait for it to return in this same turn — do not background/detach it (`run_in_background`) or end your turn to await a notification (see workflow.md → Subagents). Resume at "Post-handoff ownership check" only after it returns. If the platform allows, keep the subagent available for re-engagement after it returns — step-04 may send it review fixes.

The handoff must not stage, commit, or run forbidden workspace operations. If the subagent staged or committed in a shared worktree, HALT with status `blocked` and blocking condition `workspace ownership drift: shared-worktree stage or commit`.

**Path formatting rule:** Any markdown links written into `{spec_file}` must use paths relative to `{spec_file}`'s directory so they are clickable in VS Code. Any file paths displayed in terminal/conversation output must use CWD-relative format with `:line` notation (e.g., `src/path/file.ts:42`) for terminal clickability. No leading `/` in either case.

### Post-handoff ownership check

After the implementation subagent returns: if it reported unfinished work, finish it before proceeding. Then revalidate `expected_workspace` against current HEAD, index, untracked inventory, gitlink identity, control-path identity, and the non-allowlisted path set. Halt on drift before constructing any diff.

A shared-worktree handoff cannot prove attribution for an external same-path edit made before this post-handoff capture; state that limitation rather than claiming exclusivity.

### Capture owned_delta

Rebuild `owned_delta` from the retained pre-handoff preimages and the reported path allowlist. Preserve a pre-existing same-file hunk: only the post-handoff change against the pre-handoff preimage is owned. Include mode-only owned changes. Persist modes, rename pairs, path/type/preimage bytes, and the patch digest in the `{spec_file}.owned-delta` sidecar. That sidecar is control-owned evidence, not a reversible implementation hunk. After persisting the sidecar, replace `expected_workspace` with a new stable capture. Do not persist a self-referential hash inside the spec. Do not reconstruct ownership later from an ambiguous file list.

Include the control-owned spec body and the sidecar in finalization inputs so staging cannot omit their pre-checkpoint bodies.

### Verify

Judge against the persisted `owned_delta`, not against a broad `{baseline_revision}..working-tree` diff and not against the implementation subagent's report. Write that owned binary/full-index patch to a uniquely-named file in the system temp directory, set `{diff_file}` to its absolute path, and read that file into your own context.

Run the commands in `{spec_file}`'s `## Verification` section (or perform its manual checks). If verification fails and the failure cannot be fixed, HALT with status `blocked`, blocking condition `implementation verification failed`, and include the failing command or check and reason. When fixing a failure changes code, treat the fix as an authorized repair: revalidate the owned preimage, apply only declared owned hunks, rewrite the sidecar, refresh `expected_workspace` and the persisted `owned_delta`, rewrite `{diff_file}`, and re-read it. A stale or missing repair target HALTs as `workspace ownership drift` with no write and no checkpoint refresh. Acceptance criteria are judged at review, not here.

### Matrix Test Audit

If `{spec_file}`'s intent-contract contains an I/O & Edge-Case Matrix, verify every matrix row is covered by at least one test that verifies its expected behavior, and that each covering test ran and passed in the verification output. A covering test that exists but did not run — unregistered, filtered out, skipped, or disabled — counts as missing. If a test disagrees with the matrix, never edit the expectation to match the code: fix the code, or if the matrix row itself is ambiguous, HALT with status `blocked` and blocking condition `matrix ambiguity`. If the audit cannot otherwise be satisfied, HALT with status `blocked` and blocking condition `matrix test audit failed`.

## NEXT

Read fully and follow `[[bmad-snapshot:step-04-review.md]]`
