---
title: 'Story 6.4 prerequisite: producer-owned CI build mapping'
type: 'bugfix'
created: '2026-10-27'
status: 'in-review'
route: 'dispatch'
baseline_commit: '83c6bef90bbac3bb74f41cdb77fe7e84d53c82e0'
review_loop_iteration: 0
owning_baselines:
  builds: 'cad925260713718895282c69bcbdb8bc72e7b6a2'
  conversations: '536bf80d5fd5c2a64c2f64815edc67074fd5a6ea'
context:
  - 'references/Hexalith.Platform/docs/domain-module-enrollment.md'
---

<frozen-after-approval reason="User explicitly approved including upstream Conversations/Builds CI repairs, preparation and testing on 2026-10-27">

## Intent

Conversations CI cannot build after its enrollment change because no producer mapping reaches MSBuild. Prepare and test the shared Builds support and consumer repairs needed to unblock Story 6.4. This approved scope extends the original query-only scope for CI tooling, not publication or runtime migration.

## Boundaries & Constraints

**Always:** Reuse the existing resolver/materializer, exact tool manifest and package/catalog/provenance checks. CI selects package/Release; local source verification uses Debug. Preserve the frozen MinimalModule fixture and four original lock files byte-for-byte, historical evidence and existing test gates. Keep root-declared dependencies only. Work sequentially. Existing owner main revisions were fast-forwarded without nested initialization. The required pre-edit Aspire start failed before runtime on the same missing mapping; retain this exact baseline, not live acceptance.

**Never:** Stage, commit, push, publish, dispatch CI/releases, deploy, launch held runtimes, retire hosts, weaken mapping/locked-restore guards, invent consumer-side mapping or claim the unpublished producer command already exists in accepted 4.35.2. Prepare/test the complete candidate and record genuine producer-release/tuple prerequisites. Do not choose or bump an accepted tool pin before its separate qualification/publication.

## I/O & Edge-Case Matrix

| Scenario | Input/state | Expected behavior | Failure |
| --- | --- | --- | --- |
| Prepare | Valid source or qualified package workspace | Fresh private output, verified mapping/import environment; no runtime | No run identity or declaration mutation |
| Refuse | Invalid/missing manifest, bad package provenance, cancellation, occupied/escaping output | Nonzero diagnostic; existing files intact | Preserve existing resolver gates |
| Consume | Opt-in CI manifest | Exact pinned CLI, whitelisted generated imports before restore/build | No shell evaluation or source fallback |
| Isolate | Existing callers or fresh package-consumer check | Existing callers unchanged; isolated package consumer excludes parent mapping imports | Independent package graph |
| Windows | Long tracked paths | Enable longpaths before source checkout | Keep complete checkout |
| Fixture | Current compatibility graph | External successor locks selected only by compatibility overlay; locked restore retained | Original fixture/locks unchanged |

</frozen-after-approval>

## Code Map

All paths below are relative to the owning root-declared repository. Read its tracked guidance/config before edits; never load skills under references.

- Builds `src/libraries/Hexalith.Builds.Module.Cli/ModuleCommandApplication.cs`: add `prepare-build --manifest ... --mode source|package --workspace ... --output human|json`. Reuse `WorkspaceRootResolver.ResolveAsync`, `SourceMappingMaterializer.WriteAsync/Environment`; separate small service/result files under Tooling/Workspace. No CompositionEngine/run lifecycle call. Preserve package identity/tag/catalog checks and structured diagnostics/cancellation.
- Builds `Github/prepare-module-build/`: new opt-in composite action/helper restores exact local tool and exports only verified generated environment; parse JSON structurally. `.github/workflows/domain-ci.yml` adds optional `module-manifest`, preparation before dotnet in all three jobs; unchanged when absent. Follow governed/local action resolution patterns. Document command/action in existing CLI/workflow docs.
- Conversations `.github/workflows/ci.yml`, `.github/workflows/release.yml`: opt into shared preparation for direct/reused builds; move Windows longpaths setup before checkout. Inspect pack/release validation invocations; keep release/version guards unchanged.
- Conversations `tests/tooling/test_release_tooling.py`: replace deleted ambient-mode expectations with unmapped refusal and actual producer-mapped source/package behavior; reuse `PlatformEnrollmentMappingTest.cs` coverage. Tooling remains non-packable; do not add an unavailable package.
- Conversations `scripts/validate-consumer-package-references.py`: strip parent generated mapping environment for isolated package consumers; preserve explicit Release/package choice and add focused regression.
- Conversations `tests/compatibility/MinimalModule/RegisteredCommand.targets`, new `locks/`: opt-in external successor `NuGetLockFilePath` for fixture projects; original measured fixture stays untouched. Retain all seven compatibility assertions. Reproduce NU1004, then deliberately generate/verify successor locks against selected dependencies without relaxing locked mode. If this source fixture cannot satisfy package-only CI, use an external package-reference compatibility overlay rather than editing frozen sources or silently using source dependencies.

## Tasks & Acceptance

- [x] Builds CLI/service/tests: implement runtime-free preparation, edge cases and actual mapped MSBuild evaluation.
- [x] Builds action/workflow/docs/tests: add opt-in consumption; prove environment whitelist, mapping export, package mode and unaffected callers.
- [x] Conversations workflows/tooling/consumer tests: integrate preparation, Windows ordering and isolated-consumer boundary.
- [x] Conversations compatibility overlay/locks/tests: preserve fixture hashes, current locked restore/build/test evidence.
- [x] Retain exact commands/logs/XML, source hashes and remaining publication prerequisites in Projects evidence. Parent audits diff and matrix; independent workflow review follows.

Given opted-in consumers and a published qualified producer with a valid package tuple, when CI restores/builds, then verified package mapping reaches every relevant invocation. Given current unpublished producer or invalid package tuple, when preparation runs, then refusal stays explicit and no green upstream CI/publication is claimed. Given prepared source fixes, when reviewed, then they can be committed later under separate authorization.

## Implementation Notes

Preparation/test approval is the user's latest `yes` to including upstream Conversations/Builds CI repairs. No additional planning approval is required for this same repair. Original Story 6.4 stays in-progress until actual delivery. Accepted tool 4.35.2 lacks this command; candidate source tests do not constitute producer release qualification. Adapt discovered details within these boundaries and record them.

## Verification

Build affected Builds projects Debug and run focused public-command/workspace/provenance tests directly via xUnit assemblies. Run producer workflow/helper tests, Conversations tooling tests and mapped test builds. Use narrow direct xUnit filters, not solution tests. Exercise real mapping imports; report exact current tuple refusal separately from successful valid synthetic tuple coverage. Check both owning diffs/whitespace and original fixture hashes. No broad suite is passed merely because focused checks pass.

## Parent Verification Checkpoint

Parent inspected the complete source/workflow/test changes, verified all 30 retained source copies and hashes, parsed all 71 passing xUnit cases, checked all 17 frozen fixture hashes and the unchanged original advisory frozen block, and verified the actual npm 95-case passing log. All six CI repair matrix rows have executed coverage; the corrected Windows-order contract ran again as one passing case. Candidate evidence: [implementation and focused checks](evidence/story-6-4-ci-build-mapping-repair-20261027/README.md). The broader producer run is recorded separately and has an existing layout-dependent FAIL_SKIP; no full-suite success is claimed. Independent review follows.

## Review Triage Log

All three layers reported before triage: Blind Hunter, Edge Case Hunter (no findings), and Verification Gap. Each finding has an individual verdict below, before grouping.

| Finding | Verdict / route | Evidence and disposition |
| --- | --- | --- |
| BH1: helper suite absent from producer CI | medium / patch | Verified: producer self-CI has no invocation of the new behavioral suite. Register it in normal producer validation; VG1 is the same defect. |
| BH2: helper does not recompute mapping hash | low / reject | True for arbitrary synthetic JSON, but the exact pinned producer writes the mapping after resolver checks, and every consuming MSBuild import uses the existing producer integrity validation. No reachable corrupt successful producer output or build bypass was shown. Adding duplicate mapping semantics or a new verification interface is disproportionate; package qualification remains pending. |
| BH3: mapping.json accepts external symlink | low / patch | The metadata file lacks the containment check applied to imports. A demonstrated external metadata path can be read. Apply the existing real_path/containment check and add its direct regression. |
| BH4: bool accepted as enum integer | low / patch | Python True equals 1, so the metadata type check is incomplete. Require an actual integer Mode and retain a malformed-boolean control. This is a direct parser correction. |
| BH5: cleanup false result is discarded | maybe-false (low if true) / reject | No failed deletion of the command-owned fresh directory was demonstrated. A controlled I/O/Windows lock fault would settle it; the caller already knows the requested output path and no prepared result is disclosed. Additional cleanup-result state/diagnostic plumbing is disproportionate absent that evidence. |
| BH6: only pre-cancellation is tested | medium / patch | Verified: the sole cancellation case cancels before output allocation. Add deterministic cancellation after ownership/materialization begins and prove owned partial output is removed, without a new public surface. |
| BH7: invalid-input category mismatches exit | low / patch | Verified: Failure maps blank-argument UsageOrManifest to prerequisite phase/category. Align metadata with the existing usage/manifest diagnostic convention and add a precise assertion. |
| BH8: compatibility version may drift from catalog | medium / patch | Verified: both overlay and test use 3.128.0 without checking the selected catalog. A normal catalog bump would leave the compatibility lane testing a superseded graph. Compare with the direct pinned catalog, fail on drift and keep deliberate successor-lock generation. |
| BH9: positive graph class absent from package CI | false / reject | The standalone replacement tests unmapped refusal intentionally; positive package/source imports are exercised by producer CI public-preparation MSBuild tests. Opted-in product restore/build also consumes and validates the actual package graph; current Contracts mapping tests were executed from mapped Debug sources. No positive-graph acceptance loss was demonstrated. |
| BH10: no live Windows execution | false / reject | The candidate explicitly records windows_execution=not performed and makes no verified native Windows or upstream-CI claim. The source-order contract ran; real Windows qualification remains a delivery prerequisite outside this prepare/test-only authorization. |
| BH11: no packaged helper end-to-end check | low / reject | The exact-pinned-tool subprocess path is unit-tested and real public preparation plus mapped current-source builds ran separately. Full packaged producer/artifact qualification is explicitly pending, not claimed. No serialization/package integration defect was shown; a new packaged release harness would exceed a direct correction at this source-preparation checkpoint. |
| VG1: helper rejection tests absent from CI | medium / patch | Pre-verified missing normal invocation accepted. Same location and defect as BH1; keep one registration patch after this individual verdict. |

Surviving patches: helper CI registration (BH1/VG1), metadata containment (BH3), exact enum type (BH4), post-ownership cancellation regression (BH6), input diagnostic metadata (BH7), and selected-catalog compatibility binding (BH8). These require no change to frozen intent or public command surface.
