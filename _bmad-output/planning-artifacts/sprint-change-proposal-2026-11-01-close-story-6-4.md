---
title: 'Close Story 6.4 on its approved acceptance and move non-story gates to their owners'
date: '2026-11-01'
status: 'approved'
approved_date: '2026-11-01'
approval_source: 'User selected "Yes, apply E1-E6"'
change_scope: 'minor (tracking and definition-of-done only; no code)'
trigger: 'User: the story had many iterations without completing; is it too complex or over-engineered; can we simplify pragmatically?'
mode: 'batch'
supersedes: 'nothing; complements sprint-change-proposal-2026-10-21-simplify-story-6-4.md'
---

# Proposal: close Story 6.4 and stop the resumption loop

## 1. Issue summary

Story 6.4 is not too complex, and after the 2026-10-21 reset its product scope is not over-engineered. The problem is that its definition of done kept growing after the feature was finished.

- **Feature done on 2026-10-22.** The advisory Conversation/Folder queries passed 2,690 tests, including the real authenticated gateway → delegated owner → Dapr/Redis store case with zero writes. Conversations 1.3.0 was published and its consumer pin landed. All four acceptance boxes in the story file are ticked.
- **The story still did not close.** The main spec gained 29 dated "resumption" or "delivery gate" sections after 2026-10-22, and 25 side specs now exist for 6.4. The 6.4 evidence directories hold 7,654 files (about 1.5 GB on disk, 5,444 of the files tracked). Most rounds ended with *"Story 6.4 stays in-progress"*.
- **The gates that kept it open are not in the story.** They were: the Conversations 1.3.1 publication, Conversations CI and producer mapping, Platform tag and pin mismatches (HXW011/HXW009), Platform analyzer fixes, TenantRegistry alignment, the Builds 4.39.0 publication, the Builds `npm audit` (37 moderate, 1 high), a 5-second readiness flake in Builds' Module CLI qualification, Conversations/Folders owner-host requalification, and consumer adoption.
- **The plan already excludes them.** `epics.md` Story 6.4 says *"Existing common-runner and deployed release gates remain separate and unclaimed."* `epic-6-context.md` says the 6.4 exception *"advances no other story, common-runner or release gate"* and *"Epic 8 owns terminal deployment and release acceptance."*
- **The loop that cannot converge.** Each resumption re-verified 6.4 against the *current* workspace. Other sessions advance the 12 root submodule pointers several times a day, so each round marked the previous results "historical" (for example, 2026-10-27: *"Eleven of the 39 previously recorded source files have later changes, so the older full-suite and gateway receipts remain historical"*). Each fresh run then surfaced the next upstream issue, which became the next 6.4 gate.

**Category:** failed approach (process). The product requirements need no change.

## 2. Evidence for closing

| Anchor | Result |
| --- | --- |
| Projects `353cfd33` (2026-10-31), CI run [41229091248](https://github.com/Hexalith/Hexalith.Projects/actions/runs/41229091248) | Full CI green: build-and-test, G-6 current runtime qualification, P1R replay, generated-artifact gates |
| `evidence/6-4-advisory-resolution-20261021/owner-host-review-verification-20261101/README.md` | All five affected full suites pass: 5,047 cases plus 26 probe/contract cases, zero failures/skips/errors |
| Gateway lane in that receipt | All six catalog query routes and three real-Dapr catalog refusals pass, with unchanged persisted byte/ETag digests |

Current `main` (`ded1e70`, CI run 41298330907) is red for reasons outside 6.4:

- *Validate package consumer references* reports `wrong_versions=['Hexalith.EventStore.Contracts']`, so no test shard ran.
- The G-6 *real PostgreSQL two-sidecar stop/restart qualifier* fails one EventStore live-sidecar case.

Both come from the 2026-11-01 submodule sync. They are workspace build hygiene and are tracked as DW-76 below.

## 3. Impact analysis

- **Epic 6:** 6.4 moves to `done`. 6.5 stays blocked by DW-1, because Story 6.1 is still blocked. 6.6–6.8 are unchanged. The Epic stays `in-progress`.
- **Other epics:** no change. Stories 7.11/7.12 still treat suggestions as non-authoritative. Epic 8 keeps deployment and release acceptance, as it already does.
- **PRD / Architecture / UX:** no change. The 2026-10-21 amendments (FR-12/13, AD-7/14/32 advisory contract, UX incomplete-coverage display) already describe the delivered behavior.
- **Guard:** `tools/planning/validate_production_authority.py` already permits `done` for Story 6.4 (`STORY_6_4_STATUSES`), so neither the guard nor its test changes.
- **Code / CI / deployment:** none. No source, dependency, submodule or Git change is part of this proposal.

## 4. Recommended approach

**Direct adjustment.** Close 6.4 on its approved acceptance, anchored at a recorded green commit. Move every non-story gate to the deferred-work ledger under the repo that owns it. Add a story completion rule to Epic 6 so the loop does not restart on 6.1, 6.5 or later stories.

- **Effort:** low. About an hour of tracking and document edits, with no code.
- **Risk:** low. Nothing is claimed beyond the approved scope; File matching, deployed authorization and production release stay explicitly unclaimed.
- **Rejected alternatives:**
  - *Fix current main CI, then close.* This repeats the moving-target pattern.
  - *Rollback.* The feature works.
  - *MVP cut.* The scope is already minimal.

## 5. Detailed change proposals

### E1. Story file: `implementation-artifacts/6-4-resolve-projects-with-transient-current-explanations.md`

Frontmatter:

```
OLD: status: in-progress
NEW: status: done
     completed: '2026-11-01'
     accepted_at: 'Projects 353cfd33e54270a1acb028c1c66ecb7cd548f3e7; CI run 41229091248'
     closure_approval: 'sprint-change-proposal-2026-11-01-close-story-6-4.md'
```

Append:

> ## Closure — 2026-11-01
>
> Accepted on the four acceptance items above, at Projects `353cfd33` (full CI run 41229091248 green). Current-source local qualification is in `evidence/6-4-advisory-resolution-20261021/owner-host-review-verification-20261101/README.md` (5,047 cases, zero failures). Delivered scope: Conversation/Folder advisory suggestions; File inputs remain explicitly unsupported/incomplete (DW-78). Not claimed: deployed authorization, production release (Epic 8), Builds/Platform publication or security (DW-74, DW-75, DW-77). Later upstream changes do not reopen this story; regressions are ordinary defects.

Rationale: the approved acceptance is met, and the closure says exactly what is and is not claimed.

### E2. Main spec: `implementation-artifacts/spec-6-4-resolve-projects-with-transient-current-explanations.md`

Frontmatter `status: 'in-progress'` → `'done'`, `updated: '2026-10-28'` → `'2026-11-01'`. Append a short `## Closure — 2026-11-01` section that points to E1 and states: *"No further resumption sections. New findings become deferred-work entries or new stories."* The frozen block and history stay untouched.

### E3. Open side specs

`spec-6-4-builds-producer-publication-preparation.md` and `spec-6-4-producer-qualification-publication-verification.md`: `status: 'in-progress'` → `'superseded'`, with a one-line note: *"Remaining work moved to DW-74/DW-75 under Hexalith.Builds by sprint-change-proposal-2026-11-01-close-story-6-4.md."* No other content changes.

### E4. `implementation-artifacts/sprint-status.yaml`

```
OLD: 6-4-resolve-projects-with-transient-current-explanations: in-progress # Builds 4.39.0 publication verified ...; npm security and local full qualification blocked; no adoption
NEW: 6-4-resolve-projects-with-transient-current-explanations: done # accepted 2026-11-01 at 353cfd33 (CI 41229091248) on approved advisory ACs; non-story gates moved to DW-74..DW-78
```

Update `last_updated` (header comment and key) to 2026-11-01. Edit by hand (never `sprint_plan.py`), then run the production-authority guard.

### E5. `implementation-artifacts/deferred-work.md`: new entries, none gating Story 6.4

| ID | Item | Owner | Gate |
| --- | --- | --- | --- |
| DW-74 | Resolve the Builds release-tooling `npm audit` findings (37 moderate, 1 high, eight advisories). A fail-closed audit patch is prepared in `evidence/6-4-advisory-resolution-20261021/producer-publication-verification-20261031/`. | Hexalith.Builds | Builds release security |
| DW-75 | Stabilize Builds Module CLI local full qualification: HXR021 at the 5-second readiness bound in `PublicCommandWritesResolvedMappingIntoEnginePlanAsync(test, null)`, plus port-bind/file-lock flakes. Apply or drop the prepared hidden-file robustness patch. | Hexalith.Builds | Builds qualification |
| DW-76 | Restore green Projects `main` CI after the 2026-11-01 submodule sync: the `Hexalith.EventStore.Contracts` package-manifest version mismatch and the G-6 PostgreSQL two-sidecar stop/restart qualifier (run 41298330907). | Projects build hygiene / EventStore | CI on main |
| DW-77 | Platform owner recording and publication for package-mode preparation: HXW011 (no SemVer tag at the recorded Platform identity) and HXW009 (Platform's Builds pin differs from the umbrella). Re-check first; last observed 2026-10-30. | Hexalith.Platform / Builds | Story 6.7 / Epic 8 |
| DW-78 | Provide an owner-backed File identity mapping so advisory resolution can score File attachments, which are explicitly unsupported today. | Hexalith.Folders (File identity owner) | Future story; consumers 6.4 follow-up, 7.7 |

Also retarget the existing catalog-observer entry, whose text says *"retain fail-closed behavior and main Story 6.4 in-progress"*: change that clause to *"retain fail-closed behavior; gates Folders deployment/consumer adoption (Epic 8), not Story 6.4"*. The existing OTLP and owner-composition entries do not gate 6.4 and stay as they are.

### E6. Epic 6 story completion rule

Add to `epics.md` §Epic 6 (after the entry-gate paragraph) and mirror into `epic-6-context.md` under Cross-Story Dependencies:

> **Story completion rule (2026-11-01).** A story is done when its own acceptance criteria pass at a recorded commit, with CI green on that commit or an equivalent recorded local run. Later upstream or submodule movement does not reopen it; a regression is an ordinary defect. Publication, consumer adoption, producer security, platform qualification and deployment belong to their owning repository or Epic 8 and never extend a story's done criteria. A gate discovered during a story becomes a deferred-work entry or a new story, not another resumption of the same story. Story evidence is the commit SHA, the exact test commands, the pass totals and their XML; byte-preservation snapshots and hash ledgers of unchanged files are optional.

Rationale: without this rule, the same loop will form around Stories 6.1 and 6.5, which already carry multi-month prerequisite chains.

## 6. Implementation handoff

- **Scope:** minor. Direct implementation by the developer agent (E1–E6, documents only).
- **Validation:**
  - `python3 tools/planning/validate_production_authority.py` (index and `--story-id 6.4`)
  - `python3 -m unittest discover -s tests/tools -p test_production_authority_guard.py`
  - `git diff --check`
- **Success criteria:**
  - `sprint-status.yaml` shows 6.4 `done` and the guard passes.
  - No 6.4 spec remains `in-progress`.
  - Every moved gate has an owned DW entry.
  - No source, dependency, submodule or Git change.
- **Follow-up recommendation (not part of this proposal):** run a separate correct-course on the Epic 6 entry gate (6.1-P0…P4, G-6 applicability), the same over-gating pattern behind Story 6.1's long block and Story 6.5's dependency on it.
- **Not authorized:** commits, pushes, deletion or pruning of existing evidence, dependency or submodule updates.
