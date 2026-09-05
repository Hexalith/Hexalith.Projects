---
title: '6.1-P1R Align EventStore and Builds Baseline to 3.102.0'
type: 'bugfix'
created: '2026-09-05'
status: 'blocked'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '9f5c81976cc546a03275caf84d77db710745f0f4'
context:
  - '{project-root}/_bmad-output/specs/spec-6-1-p1r-revalidation-owner-acceptance/qualification-contract.md'
  - '{project-root}/references/Hexalith.Builds/DEVELOPMENT.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** EventStore source/packages and the Builds catalog have moved to `v3.102.0`, superseding the stopped `3.97.0` attempt in `spec-6-1-p1r-revalidation-owner-acceptance.md` (never passed its first gate). The Builds runner/schema/fixture/test corpus still hardcodes `3.90.0`, and P1R remains `open` with a stale 2026-08-03 `observed_drift` snapshot in `sprint-status.yaml`.

**Approach:** Select the immutable `v3.102.0` tag commit `4ae9cee1e9abe050402fd1405a9abd54892ba13f` as both source-mode and package-mode revision (source/package equivalent, skipping the 18 untagged post-tag commits), align one immutable Builds candidate to `3.102.0`, prove a separate executable `3.70.1` rollback, and append a superseding section to the canonical P1R record with four pending owner decisions. Supersedes `spec-6-1-p1r-revalidate-platform-baseline-2.md` and `spec-6-1-p1r-revalidation-owner-acceptance.md` (both historical only).

## Boundaries & Constraints

**Always:** Follow `qualification-contract.md` exactly — clean isolated worktrees per revision, serialized lanes with unique NuGet/CLI-home/temp/output dirs, retained first results (UTC, commands, exits, hashes), `.slnx`-only restore/build, commitlint-validated narrowly-scoped Builds commits, append-only record/audit edits.

**Never:** Push, publish, release, move gitlinks, init nested submodules, mark P1R or any downstream gate (P0/P2/P3/P4/Story 6.1) done/accepted, infer owner acceptance, edit the Architecture Spine or `sprint-status.yaml` status fields (append evidence pointers only), select a coordinate other than `v3.102.0`/`3.102.0` without recorded owner rationale, or skip classifying the four post-tag `Actors/*` files.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Candidate | `v3.102.0` source+package tag, clean worktrees | All source, package, 14-ID remote-restore, seven-API, audit, runner, packaged-tool controls pass | Append blocker row; keep P1R `open` |
| Post-tag drift | HEAD 18 commits past tag, only `Actors/*` touched | Record as unselected observation, explicit diff classification | Named-path change requires owner direction |
| Stale pins | `3.90.0` (runner), `3.88.0`, `3.70.1` | Active-runner negatives reject with only `HXM016` | Unexpected diagnostic fails qualification |
| Rollback | Atomic Builds `3.70.1` revision | `3.70.1` passes; `3.102.0` rejects with `HXM016` | Missing reciprocal proof fails qualification |
| Evidence failure | Missing artifact, stall, cancel, nonzero exit | No pass/packet; record `INCONCLUSIVE`/`FAIL` with root cause | Retain evidence; never silently retry into a false pass |

</frozen-after-approval>

## Code Map

- `qualification-contract.md` -- authoritative phases/schema/matrices/owner table; do not restate here.
- `references/Hexalith.EventStore` @ `v3.102.0` = `4ae9cee1e9abe050402fd1405a9abd54892ba13f` -- selected revision; `tools/release-packages.json` there has the 14-ID inventory.
- `references/Hexalith.Builds/Props/Directory.Packages.props:8` -- catalog reads `3.102.0`; re-verify at qualifying revision.
- `.../Manifest/SupportedPlatformPins.cs:14` -- `EventStoreVersion = "3.90.0"`, stale, align to `3.102.0`.
- `references/Hexalith.Builds/test/fixtures/{module,evidence}/**`, `test/Hexalith.Builds.Module.Tests/*.cs` -- runner corpus hardcoding `3.90.0`; keep `superseded-platform-pin.json` (`3.88.0`) and `tampered-platform-pin.json` (`3.70.1`) as intentional negatives.
- `references/Hexalith.Builds/Tools/package-version-audit.json` -- `hexalith-eventstore` family, `retained`, already `3.102.0`-catalog-ancestor; append only.
- `references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p1r-eventstore-source-architecture-runner-revalidation-record.md` -- append-only; latest section is the stopped `3.97.0` attempt (failed `es-001-coordinates` on a shell-quoting bug, not a coordinate defect).
- `_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md:368` -- still bound to `3.70.1`/`f13f9925fdca53efa2ab8c90d396ab106f91bb9c`; read-only.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (6.1-P1R) -- `status: open`, stale drift snapshot; append evidence pointer only.

## Tasks & Acceptance

**Execution:**
- [x] `Hexalith.EventStore` clean worktree @ `4ae9cee1e9abe050402fd1405a9abd54892ba13f` -- coordinate/source comparison, 14-package manifest+remote-restore, seven-API comparison, source-mode + package-mode lanes -- fills the coordinate record. **Partial**: coordinate/source comparison, 14-package manifest, seven-API comparison, and remote-restore all PASS. Source-mode and package-mode build/test lanes FAIL — structurally blocked (see Implementation Notes).
- [x] `Hexalith.Builds` clean worktree -- align catalog/runner/schema/fixture/evidence/hashes to `3.102.0`, preserve `3.88.0`/`3.70.1` negatives, pass full audit, one immutable candidate commit -- fills `builds_qualifying_revision`. **Substantively done**: commit `3d16d3e090ae822bc9cdc64c4156d31c9acf1146` (local, unpushed). Static alignment, audit, catalog test, and G-4 gate PASS; two `dotnet test -m:1` rows are INCONCLUSIVE (environment tooling, offset by fallback evidence).
- [x] `Hexalith.Builds` separate clean worktree -- validated rollback commit binding everything to `3.70.1`, rejecting `3.102.0` -- fills `rollback_builds_revision`. **Partial**: commit `6ea4c27ade695964ee3c95b1d28e0058f4e1430e` (local, unpushed). Reciprocal `HXM016` rejection of `3.102.0` proven; audit validator FAILs (two-commit tooling requirement, see Implementation Notes).
- [x] Run contract phases 0-5 serially; stop at first non-passing/incomplete gate. Stopped at Phase 3 (Qualify EventStore) / Phase 5 (Qualify rollback) — see Implementation Notes.
- [x] Append dated superseding section (10 required subsections, four `pending` owner rows) to the canonical record; Architecture and `sprint-status.yaml` status untouched. Done via commit `fff0af2dc03fd01b667bd0a7d7bf054f52588b5a` (local, unpushed) on `Hexalith.Builds`; `sprint-status.yaml` got only an append-only comment (no parsed field changed).

**Acceptance Criteria:**
- Given the `v3.102.0` candidate and `3.70.1` rollback revisions, when every contract lane runs from clean serialized worktrees, then every row is `PASS`, hashes resolve, and reciprocal `HXM016` behavior is exact.
- Given complete evidence, when the record is appended, then it names the exact tuple/revisions, leaves all four owner decisions `pending`, and no downstream status changes.

## Implementation Notes

Executed via four parallel isolated-worktree lanes (EventStore selected `v3.102.0`,
EventStore rollback `v3.70.1`, Builds candidate alignment to `3.102.0`, Builds
rollback alignment to `3.70.1`), plus direct git-plumbing work (coordinate
record, seven-API comparison, post-tag-drift classification) and the
canonical-record append, all detailed in the appended "3.102.0 candidate /
3.70.1 rollback revalidation attempt — 2026-09-05" section of
`references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p1r-eventstore-source-architecture-runner-revalidation-record.md`
(Hexalith.Builds commit `fff0af2dc03fd01b667bd0a7d7bf054f52588b5a`, branch
`docs/p1r-3102-3701-record`).

**Two structural blockers stopped the contract short of Phase 6 (owner
acceptance)** — both root-caused and recorded, not routed around:

1. **EventStore source/package-mode lanes cannot execute from an isolated
   worktree without a boundary conflict.** `Hexalith.EventStore`'s
   `Directory.Packages.props` imports central package versions from
   `references/Hexalith.Builds/Props/Directory.Packages.props`, and
   `Hexalith.Builds` is declared as a submodule in EventStore's *own*
   `.gitmodules` (pinned to a commit that differs from both the
   `Hexalith.Projects` umbrella's root-declared `Hexalith.Builds` pin and
   `Hexalith.Builds`' own current HEAD). Initializing it is a nested-submodule
   initialization relative to the umbrella this task runs under, which this
   spec's frozen boundary ("Never ... init nested submodules") prohibits
   without owner authorization. Without it, every EventStore restore fails
   `NU1010`/`MSB4019` — deterministic, not a stall. One parallel agent worked
   around this by running `git submodule update --init` inside its worktree;
   on review this was judged the same boundary violation, was reverted
   (`git submodule deinit --all -f`), and its resulting PASS evidence was
   **not** retained as qualifying. The remote-package restore proof (all 14
   release-manifest IDs, `nuget.org` only) is unaffected and PASSES cleanly —
   it doesn't touch `Directory.Packages.props`.
2. **The Builds rollback's `package-version-audit.json` cannot be refreshed
   inside the single allowed commit.** The sanctioned generator refuses to run
   against an uncommitted catalog change, and folding a regenerated audit into
   the same commit via amend breaks the audit's own ancestor self-consistency
   check (a SHA-1 preimage impossibility) — a correct refresh needs a second,
   child commit, matching the base repository's own historical two-commit
   pattern for the same class of change. The rollback commit honors "exactly
   one commit" and records the resulting audit-validator failure honestly.

Also found and corrected in-flight, not left latent: the frozen intent's
Boundaries assumed EventStore HEAD was 18 commits past `v3.102.0` touching
only 4 `Actors/*` files; actual HEAD is 21 commits past, touching 11
production files (7 more, under `Subscriptions/*`). Per the contract's own
rule, all 11 are classified in the appended record rather than only the 4
named ones; this does not change the selected coordinate (still the `v3.102.0`
tag commit itself).

A separate process mistake was caught and fixed before finishing: the record
append was first written directly into the *live* `Hexalith.Builds` checkout
(uncommitted), inconsistent with this task's own clean-worktree-and-commit
pattern; that edit was reverted (`git checkout --`) and redone properly in an
isolated worktree as commit `fff0af2dc03fd01b667bd0a7d7bf054f52588b5a`.

**Left incomplete / risky for whoever continues this work:**
- Owner ruling needed on the nested-submodule question (§5 of the appended
  record) before the EventStore source/package lanes can be re-attempted.
- A second, authorized commit (or a tooling change) needed on the
  `fix/p1r-3701-rollback` branch before its audit validator can pass.
- All evidence logs/hashes for this attempt live only in this session's
  ephemeral scratch directory, not yet relocated into a durable, git-tracked
  `qualification-evidence/` bundle the way prior attempts were — flagged
  explicitly in the appended record's §8, not concealed.
- Four new local, unpushed, unmerged Hexalith.Builds branches exist:
  `fix/p1r-3102-candidate` (`3d16d3e0…`), `fix/p1r-3701-rollback`
  (`6ea4c27a…`), and `docs/p1r-3102-3701-record` (`fff0af2d…`) — none merged
  into `main`, none pushed.

## Spec Change Log

- 2026-09-05: Discovered the frozen Boundaries' post-tag-drift assumption
  (18 commits / 4 `Actors/*` files) is stale versus current EventStore HEAD
  (21 commits / 11 files across `Actors/*` and `Subscriptions/*`). Not a
  contradiction requiring renegotiation — the contract's own rule already
  requires classifying every discovered change, which the appended record
  does for all 11 files. Recorded here for traceability, frozen section left
  untouched.

## Review Triage Log

## Design Notes

The stopped `3.97.0` attempt failed its first gate on a shell-quoting bug, not a coordinate defect — expand every contract command literally and retain the exact expanded string to avoid repeating that failure class.

## Verification

**Commands:**
- Contract's EventStore source-mode/package-mode lanes -- expected: all four test projects pass both modes; 14-ID remote restore resolves cleanly, no local feed/fallback/floating version.
- Contract's Builds serialized lane -- expected: restore/build/Module.Tests/Evidence.Tests/catalog script/audit script/G-4 gate all exit `0`, zero stale mismatches, no `-SkipSourceValidation`.
- Contract's reciprocal rollback matrix -- expected: candidate rejects `3.88.0`/`3.70.1`; rollback passes `3.70.1`, rejects `3.102.0`; exact `HXM016`.
- `git status --porcelain=v1` before/after each worktree -- expected: clean, no dirty marker.
