---
title: 'Harden Build Auto workspace ownership'
type: 'bugfix'
created: '2026-08-25'
status: 'blocked'
review_loop_iteration: 0
followup_review_recommended: true
baseline_revision: '6f0c0c8125df46cfb4bb62641f5869b4da94b741'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/project-context.md'
warnings: [oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** Build Auto currently reviews, reverses, and commits a broad baseline diff without proving that HEAD, the index, worktree bytes, or the changed path/hunk set still belong to its run. Concurrent commits, staged changes, submodule advances, or same-path edits can therefore be reviewed as owned or erased by an unscoped repair loop.

**Approach:** Establish an exact runtime workspace-ownership checkpoint around implementation, revalidate it before review construction, repairs, and commit, and replace generic reversal with verified isolated-worktree or exact owned-hunk restoration. Add hermetic fixtures that exercise the drift decision table and keep every installed agent copy synchronized.

## Boundaries & Constraints

**Always:** Keep the historical `baseline_revision` used for review provenance distinct from the current expected HEAD; on the first implementation pass capture full canonical HEAD, NUL-safe porcelain-v2 status/path classes, exact index identity, tracked binary/full-index worktree hunks, untracked path/type/content identity, and submodule/gitlink worktree identity; capture the implementation-owned path/hunk delta only after a clean post-handoff ownership check; revalidate exact HEAD/index/worktree/path/hunk state before changing review status or constructing the diff, before every repair/re-engagement loop, and immediately before staging/commit; refresh the expected checkpoint only after a known workflow-owned mutation; build review and commit inputs only from the captured owned delta; keep control-plane spec/triage/patch artifacts separate from reversible implementation hunks; preserve the original baseline across repair loopbacks; treat a `done` follow-up review as a fresh ownership session while retaining its historical review base; synchronize `.agent`, `.agents`, and `.claude` copies and their manifest hashes.

**Block If:** Version control exists but the exact ownership snapshot is missing, incomplete, unreadable, or cannot represent a path/type; an `in-progress`/`in-review` resumption has no live checkpoint from the current run; HEAD, index, worktree bytes, untracked inventory, submodule identity, path set, or owned hunks drift; the spec/control path itself no longer matches its checkpoint; a shared-worktree implementation or reviewer stages or commits; exclusive isolated-worktree ownership cannot be proven when that restoration mode is selected; or an exact reverse preflight/restore cannot complete without overlap or partial mutation. Halt as `workspace ownership drift`, report the differing class, and do not diff, reverse, stage, commit, or overwrite an overlapping result path.

**Never:** Edit the deferred-work ledger or bundle intent; edit generated `_bmad/render/**`; absorb current drift by replacing the original baseline; infer exclusivity merely from `git worktree list`; use broad reset, checkout, restore, clean, `git add -A`, whole-path staging that can include unowned hunks, or the generic instruction `revert code changes`; delete or overwrite an owned untracked path unless its exact captured type/content still matches; claim shared-worktree attribution can distinguish an external same-path edit made during the initial handoff.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Clean owned run | Stable HEAD/index/worktree through implementation, review, and finalization | Capture owned paths/hunks, review exactly that delta, stage exactly reviewed hunks, and commit once | No error expected |
| HEAD or index drift | External commit/fast-forward or staged/index-only mutation after capture | Stop before review, repair, or commit; preserve all bytes and index entries | Halt `workspace ownership drift` with the differing class |
| Worktree or path-set drift | Added/removed/renamed path, extra same-file hunk, changed untracked bytes/type, or submodule worktree advance | Detect despite unchanged coarse status where applicable; perform no mutation | Halt `workspace ownership drift` |
| Authorized repair | Verification or patch repair changes only declared owned hunks without staging/commit | Revalidate the preimage, apply the repair, then replace the expected owned checkpoint | Halt if the preimage or resulting path/hunk set differs |
| Shared repair reversal | Intent-gap/bad-spec loop with exact owned patch and unchanged preimages | Reverse only implementation-owned hunks/creates/deletes; preserve control artifacts and unrelated bytes | Reverse-check first; on overlap halt without partial restoration |
| Exclusive isolated reversal | Run is positively identified as sole owner of an isolated worktree | Restore that worktree to the captured baseline only, preserving evidence outside it | Halt if exclusivity or exact restoration cannot be proven |
| Interrupted resume | VCS-backed `in-progress` or `in-review` spec is supplied without this run's live ownership checkpoint | Refuse to recapture dirty state as owned | Halt blocked without repository mutation |
| Done follow-up review | Previously committed `done` spec has historical `baseline_revision` and clean current HEAD | Capture a fresh expected-workspace HEAD, review the historical owned change, and avoid a false HEAD-drift failure | Halt only on drift after the fresh checkpoint |

</intent-contract>

## Code Map

- `{.agent,.agents,.claude}/skills/bmad-build-auto/workflow.md:7-49` -- global HALT write-back currently has no safe exception when ownership drift overlaps the spec; add the shared ownership vocabulary and no-mutation rule.
- `{.agent,.agents,.claude}/skills/bmad-build-auto/step-03-implement.md:18-40` -- captures only `baseline_revision`; first-pass/resume handling, no-stage/no-commit handoff boundary, exact baseline, and post-implementation owned-delta capture belong here.
- `{.agent,.agents,.claude}/skills/bmad-build-auto/step-04-review.md:11-17,59-62,91-94` -- currently mutates before checking, builds a broad diff, says `revert code changes`, and commits without ownership gates; this owns review, repair, and final commit enforcement.
- `{.agent,.agents,.claude}/skills/bmad-build-auto/scripts/tests/test_workspace_ownership.py` -- new dependency-free fixture owner; use temporary Git repositories for drift/restoration behavior and inspect every synchronized workflow copy.
- `.agents/skills/bmad-module-builder/scripts/tests/test-merge-atomicity.py:1110-1139` -- reuse byte-identity, unique manifest-row, SHA-256, and self-running test patterns.
- `_bmad/_config/files-manifest.csv:247,252,271` -- canonical hashes for Build Auto step 03, step 04, and workflow; add one unique row for the fixture.
- `.github/workflows/ci.yml:24-59` and `tests/tools/run-ci-workflow-gates.ps1:54-83` -- blocking workflow-policy lane and exact-step guard for the new fixture.
- `.bmad-loop/runs/20260825-121015-999b/bundles/build-auto-workspace-ownership/intent.md` and `_bmad-output/implementation-artifacts/deferred-work.md` -- read-only orchestrator inputs; never edit.

## Tasks & Acceptance

**Execution:**
- `{.agent,.agents,.claude}/skills/bmad-build-auto/workflow.md` -- define exact ownership checkpoint/drift semantics and an ownership-drift HALT path that never overwrites a changed control file.
- `{.agent,.agents,.claude}/skills/bmad-build-auto/step-03-implement.md` -- capture the first-pass baseline and post-handoff owned delta, forbid shared-worktree staging/commits, preserve the baseline on loopback, and fail closed on unsupported resume.
- `{.agent,.agents,.claude}/skills/bmad-build-auto/step-04-review.md` -- gate review/status mutation, repairs, reversal, staging, and commit; review only captured owned changes; implement exact isolated/owned-hunk restoration and fresh done-review checkpoints.
- `{.agent,.agents,.claude}/skills/bmad-build-auto/scripts/tests/test_workspace_ownership.py` -- add table-driven temporary-Git fixtures for every matrix row plus instruction ordering, forbidden broad operations, copy identity, and manifest integrity.
- `_bmad/_config/files-manifest.csv` -- refresh the three changed logical asset hashes and register the fixture exactly once.
- `.github/workflows/ci.yml` and `tests/tools/run-ci-workflow-gates.ps1` -- run and enforce the canonical fixture as a blocking, bytecode-free workflow gate.

**Acceptance Criteria:**
- Given any captured Build Auto run, when review diff construction, a repair loopback, or final commit begins, then an exact ownership gate runs first and any HEAD/index/worktree/untracked/submodule/path/hunk drift halts without diffing, reversal, staging, or commit.
- Given an intent-gap or bad-spec reversal in a shared or proven-exclusive isolated worktree, when restoration runs, then only the captured implementation delta is reversed, control evidence survives, unrelated bytes are preserved, and a failed preflight leaves the workspace unchanged.
- Given a normal implementation, interrupted resume, and committed `done` follow-up review, when the workflow enters step 03 or 04, then it respectively captures once, fails closed without live ownership state, or establishes a fresh expected HEAD without replacing the historical review base.
- Given the three installed agent trees and installer manifest, when the focused fixture and CI-policy gate run, then all workflow/test copies are byte-identical, hashes/rows are exact and unique, every matrix scenario passes, and no ledger, bundle intent, or rendered snapshot is changed.

## Spec Change Log

## Review Triage Log

### 2026-08-25 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 20: (high 13, medium 7, low 0)
- defer: 0
- reject: 2: (high 1, medium 1, low 0)
- addressed_findings:
  - `[high]` `[patch]` Adopted a same-run newly planned spec as an exact control-owned creation so final staging cannot omit its pre-checkpoint body.
  - `[high]` `[patch]` Moved the `done` review-loop reset out of step 01 and behind the fresh follow-up ownership gate.
  - `[high]` `[patch]` Replaced ambiguous file-list reconstruction with persisted, hashed implementation patch and path/type evidence for follow-up review.
  - `[medium]` `[patch]` Replaced the claimed atomic observation with bounded stable double capture.
  - `[high]` `[patch]` Added raw no-follow tracked path identities so filters and skip-worktree state cannot hide changed bytes.
  - `[high]` `[patch]` Added ignored spec/control path identities and executable control byte/type drift cases.
  - `[high]` `[patch]` Rebuilt owned deltas from retained pre-handoff preimages and the reported path allowlist, including preservation of a pre-existing same-file hunk.
  - `[high]` `[patch]` Strengthened exact staging and private-index commit checks so unrelated staged entries and worktree bytes survive.
  - `[high]` `[patch]` Added a stale repair-preimage failure case that proves no write or checkpoint refresh occurs.
  - `[high]` `[patch]` Made the reversal fixture reach reverse-patch preflight failure and verify worktree/index atomicity.
  - `[high]` `[patch]` Replaced self-asserted exclusivity with a continuously held OS lease and exercised exact isolated restoration plus negative lease cases.
  - `[high]` `[patch]` Fixed uninitialized gitlink detection and recursively accounted for initialized nested-submodule state.
  - `[medium]` `[patch]` Restricted split-index identity to the actually referenced shared index.
  - `[high]` `[patch]` Exercised interrupted active-spec routing through a workflow-state helper that fails closed without live ownership state.
  - `[high]` `[patch]` Gated `review_loop_iteration` increments as declared control mutations.
  - `[medium]` `[patch]` Deferred the triage-log write until branch outcome and required exactly one append.
  - `[medium]` `[patch]` Added behavioral cases for raw newline/non-UTF-8 names, rename pairs, executable-mode changes, and unsupported types.
  - `[medium]` `[patch]` Added an explicit no-version-control review and finalization path.
  - `[medium]` `[patch]` Sanitized repository-routing Git environment variables in the hermetic fixture.
  - `[medium]` `[patch]` Added stable no-follow file and symlink reads that reject raced type/content observations.

## Design Notes

Runtime ownership state avoids persisting a self-referential hash inside the spec. The historical `baseline_revision` remains review provenance; a separate expected-workspace snapshot governs mutation safety. In a shared worktree, the initial before/after handoff delta is attributable only by workflow convention—strong attribution requires a positively identified exclusive worktree—so the skill must state that limit and detect all drift occurring after capture.

## Verification

**Commands:**
- `PYTHONDONTWRITEBYTECODE=1 python3 .agents/skills/bmad-build-auto/scripts/tests/test_workspace_ownership.py` -- expected: temporary-Git behavior, three-copy synchronization, and manifest integrity all pass.
- `PYTHONDONTWRITEBYTECODE=1 python3 .agent/skills/bmad-build-auto/scripts/tests/test_workspace_ownership.py && PYTHONDONTWRITEBYTECODE=1 python3 .claude/skills/bmad-build-auto/scripts/tests/test_workspace_ownership.py` -- expected: every installed entry point independently passes.
- `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tools -p 'test_*.py' -v` -- expected: all root workflow/tooling tests pass.
- `pwsh ./tests/tools/run-ci-workflow-gates.ps1` -- expected: CI policy requires the exact blocking workspace-ownership fixture step.
- `python3 /home/administrator/.codex/skills/.system/skill-creator/scripts/quick_validate.py .agents/skills/bmad-build-auto` -- expected: the updated skill remains structurally valid.
- `git diff --check` -- expected: no whitespace errors.

## Auto Run Result

Status: blocked
Blocking condition: dirty working tree at run start blocks the required clean-HEAD/index/worktree precondition for a `done`-entry follow-up review (step 04).

At invocation, `git status` showed two pre-existing unstaged modifications versus HEAD (`6b89a3b`, this story's own implementation commit):

- `_bmad-output/implementation-artifacts/deferred-work.md` — DW-13 and DW-14 flipped from `status: open` to `status: done 2026-08-25` with `resolution: resolved by sweep bundle dw-build-auto-workspace-ownership`. This is orchestrator-owned ledger bookkeeping; this run must never edit, re-open, or rewrite it, so it cannot resolve this drift itself.
- This spec file — the `## Auto Run Result` section recorded by the prior run (present at HEAD) was absent from the working copy, while frontmatter still read `status: 'done'`.

Neither condition can be resolved automatically without risking loss of in-progress state: discarding either change could erase real orchestrator or prior-session work, and staging/committing them is outside this run's authority (this run made no code changes and has nothing of its own to combine with them). This needs a human decision — confirm whether the pending `deferred-work.md` and spec edits should be committed, discarded, or investigated — before a follow-up review can safely establish a fresh ownership checkpoint.

No implementation, review, or repository mutation beyond this status/result write-back was performed.

