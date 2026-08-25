# Step 4: Review

## RULES

- **Language** — Speak in `{{.communication_language}}`, tailored to `{{.user_skill_level}}`. Write files in `{{.document_output_language}}`.
- No human interaction: do not ask questions or wait for approval in this step.
- All review subagents must run at the same model capability as the current session.

## INSTRUCTIONS

### Establish Review Session

Record the entry status before changing it. Every ownership gate, checkpoint, owned-delta, exact-restoration, staging, and commit instruction below is conditional on version control being available. When it is unavailable, set `no_vcs_review = true`, skip those operations, retain in-memory before/after copies where possible, and use the explicit best-effort paths below.

When version control is available:

- A normal implementation or same-run repair loop must arrive with the complete live `expected_checkpoint`, `owned_delta`, and retained preimages from step 03. An `in-progress` or `in-review` entry without them is an interrupted resumption: HALT as `workspace ownership drift` with differing class `missing live checkpoint` and do not mutate the spec.
- A `done` entry is a follow-up review, not a resumed ownership session. Require a clean current HEAD/index/worktree, capture a fresh stable `baseline_checkpoint` and `expected_checkpoint` at current full HEAD, and preserve the existing historical `baseline_revision` verbatim. Read the exact evidence path and SHA-256 recorded by the completed run under `## Auto Run Result`; load the persisted implementation-owned binary/full-index patch and safely encoded path/type/preimage/postimage metadata, verify the recorded digest and internal patch digest, and use that exact evidence as `historical_owned_delta`. Missing, unreadable, ambiguous, or tampered evidence HALTs as `workspace ownership drift` with differing class `incomplete historical owned delta`. Never reconstruct a follow-up diff from a human file list or a broad baseline-to-current comparison.

For a `done` entry, only after the fresh checkpoint and historical evidence verification succeed, run an ownership gate and reset `review_loop_iteration` to `0` as a declared control mutation. Verify only that hunk and refresh `expected_checkpoint`. Step 01 must not perform this mutation.

When version control is available, run an ownership gate before changing `{spec_file}` status to `in-review`. Apply that exact status hunk as a declared control mutation, verify it, add it to the control part of `owned_delta`, and refresh `expected_checkpoint`. Under `no_vcs_review`, change the status directly and retain its prior bytes for best-effort recovery.

### Construct Diff

When version control is available, run an ownership gate immediately before constructing `{diff_output}`. For a normal run, construct it only from the captured `implementation_owned_delta`; for a `done` follow-up, use only the verified persisted `historical_owned_delta`. Preserve binary/full-index hunks, creations, deletions, renames, modes, and safely encoded paths. Never substitute a broad current-worktree or `{baseline_revision}`-to-current-HEAD diff. Under `no_vcs_review`, construct a clearly labeled best-effort diff only from the implementation handoff's reported paths and retained before/after copies; if those are unavailable, report the limitation rather than guessing repository history.

Do NOT `git add` anything — this is read-only inspection.

### Review

Runtime placeholders: `{diff_output}` is the diff constructed above. `{verbatim_intent}` is the invocation intent exactly as this run received it at step-01; if the run started from an existing spec file rather than a fresh intent, it is the spec's `<intent-contract>` block instead. Before launching a layer, expand its skill-root placeholder to this skill's absolute installed directory; never leave that placeholder unresolved in a child prompt.

Execute these review layers in parallel wherever their execution methods allow: substitute the runtime placeholders (e.g. `{diff_output}`) into each layer's instruction. When an instruction launches a reviewer subagent, launch that child with the prompt text after placeholder substitution; do not load the reviewer instruction file yourself. For any other customized instruction, execute it as written. Parallel means several blocking calls awaited together in this turn — never backgrounded or detached, never ending the turn to await results (see workflow.md → Subagents). Spawn every reviewer subagent before reading or reacting to any of their output; begin collection and triage only once all are launched.

Every reviewer is read-only and must not edit, stage, or commit. After all reviewers return, run an ownership gate before classifying findings. A reviewer mutation is workspace ownership drift, not a review finding.

{workflow.review_layers}

### Classify

1. Deduplicate only findings with the same claim and same required action. Then evaluate each remaining finding independently. Do not reject a finding because a related finding was rejected.
2. Assign severity to each finding by consequence for the artifact's main consumer (software user, document reader, etc).
   Disregard any severity assigned by a reviewing subagent. Review subagents operate under by-design information asymmetry and do not have enough context to set final severity for this workflow.
   - `low`: none or cosmetic
   - `medium`: tolerable
   - `high`: intolerable
3. Route each finding into exactly one triage category. The first three categories are **this story's problem** — caused or exposed by the current change. The last two are **not this story's problem**.
   Scope authority: a finding may be routed to defer or reject *as out of scope* only on the authority of the intent itself. The spec's scope language, the plan, and the diff's own shape are not admissible scope authorities — if only they exclude a finding, treat it as evidence against the chosen reading (intent_gap or bad_spec), not as out of scope.
   - **intent_gap** — caused by the change; cannot be resolved from the spec because the captured intent is incomplete. Do not infer intent unless there is exactly one possible reading.
   - **bad_spec** — caused by the change, including direct deviations from spec. The spec should have been clear enough to prevent it. When in doubt between bad_spec and patch, prefer bad_spec — a spec-level fix is more likely to produce coherent code.
   - **patch** — caused by the change; trivially fixable without human input. Just part of the diff.
   - **defer** — pre-existing issue not caused by this story, surfaced incidentally by the review. Collect for later focused attention.
   - **reject** — noise. Drop silently. When unsure between defer and reject, prefer reject — only defer findings you are confident are real.
4. Build one pending `## Review Triage Log` entry in memory, but do not append it yet. Its final counts and addressed findings depend on the selected branch outcome. Use this format:
   ```markdown
   ### {date} — Review pass
   - intent_gap: count
   - bad_spec: count
   - patch: count
   - defer: count
   - reject: count
   - addressed_findings:
     - `[high|medium|low]` `[patch|bad_spec]` <finding summary and action taken in this pass>
   ```
   Where `{date}` is the current system date and `count` is either just `0`, or total with breakdown by severity `N: (high Nhigh, medium Nmedium, low Nlow)`.
   If no patch was fixed and no bad_spec repair loopback was triggered in this pass, write:
   ```markdown
   - addressed_findings:
     - none
   ```
5. Process findings in cascading order. If intent_gap exists, lower findings are moot; follow the intent_gap branch below. If bad_spec exists, lower findings are moot since code will be re-derived. If neither exists, process patch and defer normally. Before each bad_spec loopback, read `{spec_file}` frontmatter `review_loop_iteration` (missing means `0`), run an ownership gate, increment it by 1 as a declared control mutation, verify only that hunk, and refresh `expected_checkpoint`. If it exceeds 5, set the pending triage entry to `addressed_findings: none`, then proceed to the single append in item 6 before HALT with status `blocked` and blocking condition `review repair loop exceeded 5 iterations (non-convergence)`.
   - **intent_gap** — Root cause is inside `<intent-contract>`. Run an ownership gate, save the exact captured implementation patch in `{{.implementation_artifacts}}` as a declared control artifact, verify it, and refresh `expected_checkpoint`. Then use the restoration protocol below. Set the pending entry to `addressed_findings: none`; after item 6, HALT with status `blocked`, blocking condition `intent gap`, and include the unresolved questions and saved patch path.
   - **bad_spec** — Root cause is outside `<intent-contract>`. Do not modify content inside `<intent-contract>`. Extract KEEP instructions for positive preservation, then use the restoration protocol below. Read the `## Spec Change Log` in `{spec_file}` and strictly respect all logged constraints. Gate before amending only the sections outside `<intent-contract>` that contain the root cause; verify the declared control hunks and refresh `expected_checkpoint`. Append a change-log entry recording the triggering finding, amendment, known-bad state avoided, and KEEP instructions. Put every bad-spec finding in the pending entry; after item 6, read fully and follow `[[bmad-snapshot:step-03-implement.md]]` with the live ownership state intact.
   - **patch** — Auto-fix. These are the only findings that survive loopbacks. Run an ownership gate immediately before repair and verify each declared target's current preimage equals its captured owned postimage. If the step-03 implementation subagent can be re-engaged, send all patch findings in one synchronous message and explicitly forbid staging/commit; otherwise apply them yourself. Require unchanged HEAD/index/control state and exactly the declared owned hunk/path set afterward, then replace the affected postimages and binary patch, rebuild `owned_delta`, and refresh `expected_checkpoint`. Re-run verification and gate again; on failure that cannot be fixed, HALT with blocking condition `patch verification failed`. Put every applied patch in the pending triage entry for item 6.
   - **defer** — Run an ownership gate, then update the single `deferred` list in `{spec_file}` frontmatter as a declared control mutation. If the field is absent (including on specs created before this field existed), add it once as an empty list. If it is `deferred: []`, replace that empty value when adding the first item; otherwise append to the existing list. Preserve every existing item, do not look for duplicates, and never add a second `deferred:` key. Serialize free-form values as YAML block scalars so characters such as `:`, `#`, quotes, and line breaks remain data. Each item uses this shape:
     ```yaml
     deferred:
       - summary: >-
           <one sentence>
         evidence: |-
           <why this is real>
         location: >- # optional — file:line or component
           src/foo.py:42
         severity: medium # optional — high | medium | low
     ```
     After all appends, parse the complete frontmatter as YAML and verify that `deferred` is one list containing every prior item plus the new items with their intended text. Verify only the declared control hunk changed and refresh `expected_checkpoint`; repair serialization errors only through the same gated mutation protocol.
   - **reject** — Drop silently.
6. After the selected branch outcome and addressed findings are known, append the one pending triage entry exactly once. When version control is available, gate immediately before the append, verify only that declared control hunk afterward, add it to the control delta, and refresh `expected_checkpoint`. Under `no_vcs_review`, append once using the retained preimage. No branch may append a second copy.

### Restoration Protocol

Run an ownership gate immediately before any intent-gap or bad-spec restoration. In a shared worktree, verify every current implementation path/type/postimage, preflight the complete captured binary patch in reverse without mutation, and only then reverse that patch as one operation. Never include the spec, triage log, saved patch, deferred ledger, bundle intent, or unrelated bytes. A failed preflight or overlap HALTs as workspace ownership drift and leaves every byte and index entry unchanged. After success, verify exact implementation preimages and an unchanged index/control set, clear only the implementation part of `owned_delta`, and refresh `expected_checkpoint`.

Use isolated-worktree restoration only with the continuous private token, lock, and recorded identity proof required by `workflow.md`; a worktree-list entry is insufficient. Verify all current postimages first, restore exact captured preimages without broad Git mutation, preserve external evidence, verify the complete baseline checkpoint, then refresh `expected_checkpoint`. If exclusivity or exact restoration cannot be proven, HALT without falling back to a broad operation.

## Finalize

When version control is available and the reviewed implementation delta is new or changed in this run, run an ownership gate and persist immutable follow-up evidence under `{{.implementation_artifacts}}`: the exact implementation-only binary/full-index patch plus metadata containing base64/safely encoded raw path names, path types/modes, preimage/postimage digests, and the preserved `baseline_revision`. Write via a temporary artifact and publish only after full verification; treat both evidence files as declared control creations. Compute SHA-256 after publication, verify by rereading, add them to the control delta, and refresh `expected_checkpoint`. The evidence must not contain its own digest. A `done` follow-up with no implementation patch changes retains the already verified evidence unchanged.

Run an ownership gate before finalization. Write the following details to `{spec_file}` under `## Auto Run Result` as one declared control mutation, verify it, add it to the control delta, and refresh `expected_checkpoint`:
- Summary of implemented change
- Files changed with one-line descriptions
- Review findings breakdown: patches applied, items deferred, items rejected
- Follow-up review recommendation: count only this pass's findings triaged `patch` — never defer or reject. `true` if any patched finding was `high` severity, or if `3 × medium count + 1 × low count` is 5 or more; otherwise `false`. Record the patched counts by severity and the score.
- Historical owned evidence paths and SHA-256 values for both the implementation patch and its path/type metadata, or an explicit `NO_VCS` limitation
- Verification performed, including command outcomes or manual inspection notes
- Any residual risks

Set `{spec_file}` frontmatter `followup_review_recommended` from the computation above through the same gated control mutation protocol.

If version control is unavailable, use retained before/after copies to avoid overwriting unrelated paths, set `{spec_file}` frontmatter `status: done`, record that review/finalization were best effort and no commit was possible, then proceed to HALT. Do not execute any ownership, Git staging, or commit instruction.

If version control is available, gate, write `status: done` into `{spec_file}` frontmatter as an exact control mutation, verify it, and refresh `expected_checkpoint`. Then:

1. Run an ownership gate immediately before staging. Build one exact binary full-index staging patch from the uncommitted implementation and control portions of the captured `owned_delta`. Preflight it against a private temporary index seeded from expected HEAD and against the real index only at owned paths. Apply it cached to both indexes; never use whole-path staging. Verify worktree path identities are byte-identical, the private-index staged set equals only the owned patch, and every pre-existing real-index entry outside the owned path/hunk set remains byte-for-byte equivalent. Treat the exact real-index update as a known mutation and refresh `expected_checkpoint`.
2. Run another ownership gate immediately before commit. Commit once using the verified private index so pre-existing real-index content cannot enter the commit; an earlier implementation/reviewer commit is drift. Verify the new commit contains exactly the captured owned delta, the real index still preserves pre-existing entries while owned entries match the commit, then refresh expected HEAD/index. Preserve `baseline_revision` and do not push.
3. Verify the version-controlled working copy matches the captured pre-run unrelated state and has no owned changes left uncommitted. Otherwise HALT with status `blocked` and blocking condition `finalization left repository dirty`.

HALT with status `done`.
