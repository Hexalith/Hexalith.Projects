# Build Auto Workflow

**Goal:** Turn intent into a hardened, reviewable artifact, without human interaction.

**CRITICAL:** If a step directs you to another snapshot file, read it fully and follow it. No exceptions.

## Workspace Ownership

When version control is available, every implementation and review run owns a live, runtime-only workspace session. The historical `baseline_revision` is immutable review provenance; it is never the expected current HEAD and must never be refreshed to absorb drift.

An **ownership checkpoint** is a stable double capture: obtain two complete consecutive observations and accept them only when they are byte-identical. Retry the pair at most three times; if no pair stabilizes, HALT as `workspace ownership drift` with differing class `incomplete checkpoint`. Capture command output as bytes, use NUL-delimited Git output for path-bearing records, disable optional Git locks/read refreshes, clear repository-routing environment variables (`GIT_DIR`, `GIT_WORK_TREE`, `GIT_INDEX_FILE`, `GIT_COMMON_DIR`, object/alternate-object routing, prefix, and ceiling variables), and retain captured preimages outside the worktree. Each complete observation contains:

- the full canonical HEAD object id;
- `git status --porcelain=v2 -z --untracked-files=all --ignore-submodules=none` bytes and decoded record/path classes;
- exact index identity: the byte identity of the resolved index and only the split index named by `git rev-parse --shared-index-path` when non-empty, plus `git ls-files --stage -v -z` bytes;
- tracked worktree identity: raw NUL-delimited path/type records, no-follow `lstat` type/mode and exact content-or-link-target identity for every `git ls-files -z` worktree path (including skip-worktree and filtered paths), and the full `git diff --binary --full-index --no-ext-diff --no-textconv --no-renames` hunk stream; compare pre/post `lstat` around every read and reject a raced observation;
- every Git-visible untracked path's raw name, `lstat` type/mode, and exact regular-file bytes or symlink-target bytes, with a cryptographic digest for comparison;
- every tracked gitlink's index object id and, when its worktree is initialized, its canonical HEAD, index identity, tracked path identities, porcelain-v2 records, full-index binary tracked diff, untracked identities, and recursively initialized nested-gitlink state; determine initialization from the gitlink path's own no-follow `.git` marker before running Git so an uninitialized path cannot resolve upward to its superproject, and never initialize a submodule to capture it;
- exact no-follow byte/type/mode identities for the spec and every workflow control/evidence path that exists, including ignored paths.

If any field is missing, unreadable, captured ambiguously, or cannot represent a discovered path or type, HALT as `workspace ownership drift` with differing class `incomplete checkpoint`. Never silently omit an item.

The **expected checkpoint** is the last verified checkpoint. An **ownership gate** captures the same fields again and compares them byte-for-byte with the expected checkpoint. A mismatch is classified as `HEAD`, `index`, `status/path set`, `tracked worktree/hunks`, `untracked inventory/type/content`, `submodule/gitlink`, or `spec/control path`. On any mismatch, enter no-mutation mode and HALT as `workspace ownership drift`, reporting the class and safely encoded paths only; do not construct a review diff, reverse, stage, commit, or overwrite an overlapping result path.

Only a known workflow-owned mutation may replace the expected checkpoint. Gate immediately before it, verify every changed path, type, preimage, and hunk is in its declared allowlist immediately afterward, then capture the replacement checkpoint. A repair must also replace the captured owned postimage and exact patch. Never refresh a checkpoint merely because current state differs.

After the implementation handoff, capture an **owned delta** only after HEAD, index, spec/control identities, and the subagent's exact reported path set pass the post-handoff check. Store exact preimages, postimages, path/type transitions, and a binary full-index patch. Tag implementation hunks separately from workflow-owned spec, triage, and patch artifacts: control artifacts may be committed but are never part of reversible implementation hunks. In a shared worktree this attribution is a workflow convention and cannot distinguish an external same-path edit made during the initial handoff; never claim stronger ownership.

The deferred-work ledger, bundle intent, and generated `_bmad/render/**` snapshots are read-only evidence for this workflow. Never edit or include them in an owned delta.

Implementers and reviewers in a shared worktree must not stage or commit. Review and commit inputs are built only from the captured owned delta. Staging uses an exact cached patch after a whole-patch preflight; whole-path staging and broad Git mutation commands are forbidden, including `git add -A`, reset, checkout, restore, and clean.

Shared-worktree reversal means a whole-patch reverse preflight followed by reversal of only the captured implementation patch, with exact pre/post verification. It must preserve control artifacts and all unrelated bytes. Before deleting or replacing an owned untracked path, its current type and content must equal the captured postimage. A failed preflight leaves the workspace unchanged. Exclusive isolated-worktree restoration is allowed only when the workflow created that worktree for this run, retained a private ownership token and exclusive lock continuously, and verifies its recorded git-dir/worktree identity; `git worktree list` alone never proves exclusivity. Restore exact captured preimages there without broad Git mutation and preserve evidence outside it.

## HALT

To HALT with a final status and optional blocking condition:

If the blocking condition is `workspace ownership drift`, the no-mutation rule above overrides normal HALT write-back. Report the terminal status, differing class, and paths in the response only; do not update or create a spec/result file, run `On Complete`, or make any other repository mutation. Stop immediately.

1. **Folder+id dispatch** (`{spec_folder}` and `{story_id}` are set): the write-back always lands at the id-keyed story spec. The `{{.implementation_artifacts}}` fallback in step 2 below is never used in this mode, even for halts before planning starts.
   - If `{spec_file}` is still empty, resolve it now:
     - **Entry not resolved** (`stories.yaml` is missing/unparseable, or `{story_id}` has no matching entry): use the fixed slug segment `unresolved`: `{spec_file}` = `{spec_folder}/stories/{story_id}-unresolved.md`.
     - **Ambiguous on-disk match** (the halt is `ambiguous story file match` — more than one file already matches `{spec_folder}/stories/{story_id}-*.md`): use the fixed slug segment `ambiguous` instead of deriving from the title, so the write-back neither creates a third title-derived candidate nor risks silently landing on one of the existing ambiguous files: `{spec_file}` = `{spec_folder}/stories/{story_id}-ambiguous.md`.
     - **Otherwise** (the entry was resolved and no ambiguous on-disk match exists): derive `{spec_file}` = `{spec_folder}/stories/{story_id}-{slug}.md`, where `{slug}` is a kebab-case slug from `title` (and `description` if needed) with no `{story_id}` prefix — the same derivation step-01's Route uses.
   - If `{spec_file}` exists on disk, update `status` in frontmatter and append missing result details under `## Auto Run Result`.
   - If it does not exist, create it as a skeletal story spec:
     ```markdown
     ---
     status: <final status>
     ---

     # <entry title, or "Story {story_id}" if the entry could not be resolved or the on-disk match was ambiguous>

     ## Auto Run Result

     Status: <final status>
     Blocking condition: <blocking condition, if any>
     ```
2. **Otherwise:**
   - If `{spec_file}` is known and exists, update `status` in frontmatter and append missing result details under `## Auto Run Result`.
   - If `{spec_file}` is unknown or missing, create `{{.implementation_artifacts}}/bmad-build-auto-result-<slug-or-timestamp>.md` with:
     ```markdown
     ---
     status: <final status>
     ---

     # BMad Build Auto Result

     Status: <final status>
     Blocking condition: <blocking condition, if any>
     ```
3. Follow **On Complete** below, then stop the workflow.

### On Complete

If anything appears below, follow it as the final terminal instruction before exiting; otherwise exit normally.

{workflow.on_complete}

## Subagents

Using subagents when instructed is mandatory. If you cannot, HALT with status `blocked` and blocking condition `no subagents`.

Invoke every subagent **synchronously**: launch it, wait for it to return within the same turn, then continue with its result. When a step says to run subagents "in parallel" (e.g. the reviewers), that means several **blocking** calls awaited together in one turn — not detached execution. Never run a subagent in the background / detached / async (e.g. `run_in_background: true`), and never end your turn to "await a completion notification." This workflow runs unattended: there is no event loop to resume a yielded turn, so a backgrounded subagent never hands control back and the run stalls. The only sanctioned way to end a turn is the HALT protocol above with an explicit terminal `status`.

## READY FOR DEVELOPMENT STANDARD

A specification is "Ready for Development" when:

- **Actionable**: Every task has a file path and specific action.
- **Logical**: Tasks ordered by dependency.
- **Testable**: All ACs use Given/When/Then.
- **Surface-anchored**: ACs observe the outermost surface the intent references — never a more internal proxy for it.
- **Complete**: No placeholders or TBDs.
- **Sufficient**: No known requirement, acceptance, dependency, or implementation gaps remain unresolved.
- **Coherent**: No unresolved ambiguities or internal contradictions.

## Conventions

- Every operational cross-file reference in this workflow is an absolute snapshot path. Open it directly; do not resolve it relative to a skill directory.
- `{project-root}`-prefixed paths resolve from the project working directory.
- Speak in `{{.communication_language}}`, tailor communication to `{{.user_skill_level}}`, and write documents in `{{.document_output_language}}`.
- Whenever this workflow captures or records a version-control revision, obtain the full canonical identifier directly from version control and preserve it verbatim.

## On Activation

### Step 1: Execute Prepend Steps

Execute each of these steps in order before proceeding (`_None._` means skip):

{workflow.activation_steps_prepend}

### Step 2: Load Persistent Facts

Treat every entry below as foundational context you carry for the rest of the workflow run. Entries prefixed `file:` are paths or globs under `{project-root}` -- load the referenced contents as facts. All other entries are facts verbatim (`_None._` means none):

{workflow.persistent_facts}

### Step 3: Execute Append Steps

Execute each of these steps in order (`_None._` means skip):

{workflow.activation_steps_append}

Activation is complete after all activation steps have run.

## Workflow Execution

Follow the step files in order. Read one step fully, execute it, then load the next step only when directed. Do not skip, reorder, or pre-load steps.

## First Workflow Step

Read fully and follow: `[[bmad-snapshot:step-01-clarify-and-route.md]]`.
