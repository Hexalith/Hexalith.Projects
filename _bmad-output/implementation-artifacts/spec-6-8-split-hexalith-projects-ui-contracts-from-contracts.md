---
title: 'Split Hexalith.Projects.UI.Contracts from the packable Contracts package'
type: 'refactor'
created: '2026-11-15'
status: 'done'
baseline_commit: '6bea95a379b6ab38d6ad71792da743072164ce3e'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Packable `Hexalith.Projects.Contracts` references FrontComposer Contracts/Shell/SourceTools, `Microsoft.AspNetCore.App`, Fluxor and Fluent UI (`Hexalith.Projects.Contracts.csproj:13-47`), so every CLI/MCP consumer (Stories 6.6, 8.4, 8.5) inherits Blazor transitively, violating AD-16. DW-55 tracks it.

**Approach:** Create non-packable `src/Hexalith.Projects.UI.Contracts` (depends inward on Contracts only) and move the FrontComposer projections, descriptors, badge mapping, generator and UI-only package references into it; strip FrontComposer attributes from the enums that stay; repoint consumers, release manifest and gates. No runtime behavior change.

## Boundaries & Constraints

**Always:**
- Types referenced by Contracts commands/events/models/queries stay in Contracts with namespace and JSON/wire names unchanged: `ProjectContextAssemblyOutcome`, `ProjectContextFreshness`, `ProjectContextInclusionCheck`, `ProjectContextInclusionDiagnostic`, `ProjectLifecycle`, `ProjectReasonCode`, `ReferenceState`, `ResolutionResult`, plus `ProjectMaintenanceActions` and `ProjectMaintenanceCommandLifecycleStates` (used by Mcp).
- Moved types keep namespace `Hexalith.Projects.Contracts.Ui`: generated view keys and manifest names derive from the full type name, so they stay identical and no `using` changes.
- Rendered badges stay identical: `ProjectVocabularyDescriptors` replaces its `[ProjectionBadge]` reflection with an explicit table carrying today's exact slot for every member.
- One C# type per file; the FrontComposer source-mode/package-mode reference pattern enforced by `run-ci-workflow-gates.ps1` applies to the new project.

**Never:**
- No redefinition of operations, vocabulary or security; no mirror enums.
- No change to `Hexalith.Projects.Client.csproj` framework reference (Story 8.7 owns it), McpCli, sibling repositories, or the five frozen package IDs.
- Do not mark DW-83 done, or pin FrontComposer 6.0.0, to make CI green.

</frozen-after-approval>

## Code Map

- `src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj` -- move FrontComposer refs (15-20, 37-41), `FrameworkReference` (44), Fluxor/FluentUI (45-46), `NoWarn ASP0006` and `FlattenFrontComposerGeneratedFiles` to UI.Contracts; delete `Microsoft.Extensions.Validation` (35) and `PreserveStableValidationDependency` (51-56), which exist only for the FluentUI chain (`7bcb375f`).
- `src/Hexalith.Projects.Contracts/Ui/` -- move the 12 `*Projection.cs` files, `ProjectVocabularyDescriptors`, `VocabularyDescriptor`, `ProjectsFrontComposerDomain`; split `ProjectMaintenanceActionProjection.cs` (constants :131/:183 stay as their own files, enum :155 and `ProjectMaintenancePanelStates` :141 move).
- `src/Hexalith.Projects.UI/Program.cs:38-40,64`, `Components/Routes.razor:34` -- `AddHexalithDomain<ProjectsFrontComposerDomain>` scans that type's assembly for generated `*Registration`; the generator therefore runs in UI.Contracts. The generated `{T}View` components have no route and no consumer.
- `src/Hexalith.Projects.UI/Hexalith.Projects.UI.csproj`, `tests/Hexalith.Projects.Tests/Hexalith.Projects.Tests.csproj` (`Leakage/NoPayloadLeakageTests.cs`) -- add a ProjectReference to UI.Contracts.
- `tests/Hexalith.Projects.Contracts.Tests/Ui/{ProjectAuditExportProjectionTests,ProjectResolutionTraceProjectionTests}.cs`, `ProjectVocabularyTests.cs` (:72, :91, :95-236, :397) -- FrontComposer-metadata and severity tests; JSON-name tests stay.
- `Hexalith.Projects.slnx`, `Hexalith.Projects.CI.slnx` -- list the new project.
- `tools/release-packages.json:21-177` -- hand-maintained, validated by `scripts/_nuget.py`; Contracts, `Hexalith.Projects`, Client and Testing graphs carry the FrontComposer 5.0.0/Fluxor/FluentUI edges inherited from Contracts. ServiceDefaults is unaffected.
- `tests/tools/run-package-dependency-gate.ps1:44-56`, `tests/tools/test_release_package_tools.py:479-530` -- SourceTools rule hard-coded to Contracts; no negative test.
- `tests/tools/run-frontcomposer-inspect-gate.ps1:14-63` -- hard-codes Contracts; after the move it would print SKIPPED and pass silently.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Projects.UI.Contracts/Hexalith.Projects.UI.Contracts.csproj` -- create (`IsPackable=false`, ProjectReference to Contracts, moved references/targets/NoWarn) and move the types above -- AD-16 boundary.
- [x] `src/Hexalith.Projects.Contracts/` -- slim csproj; strip `[ProjectionBadge]` and FrontComposer usings/crefs from the staying enums; split the maintenance file -- Contracts dependency-light.
- [x] `src/Hexalith.Projects.UI.Contracts/Ui/ProjectVocabularyDescriptors.cs` -- explicit per-member slot table, same API, unknown member still throws -- keep rendered badges.
- [x] `Hexalith.Projects.slnx`, `Hexalith.Projects.CI.slnx`, UI and Projects.Tests csproj -- wire the new project.
- [x] `tests/Hexalith.Projects.UI.Tests/Contracts/` -- move the metadata tests; add a golden test that pins every member's slot to the pre-split attribute values; keep JSON-name tests in Contracts.Tests -- prove no badge or wire drift.
- [x] `tests/tools/run-package-dependency-gate.ps1` -- fail if the Contracts csproj references any `Hexalith.FrontComposer.*`; apply the existing SourceTools analyzer rule to UI.Contracts -- AC4.
- [x] `tests/tools/test_release_package_tools.py` -- add a sandboxed negative case: a Contracts csproj that references FrontComposer fails the gate.
- [x] `tests/tools/run-frontcomposer-inspect-gate.ps1` -- retarget detection, build and inspect to UI.Contracts.
- [x] `tools/release-packages.json` -- update every changed graph from the packed nuspecs (Contracts `frameworkReferences: []`; Client keeps its own).
- [x] Closure: `deferred-work.md` DW-55 → `status: done` plus a separate `closed:` line; if the packed graphs no longer carry FrontComposer, add a note to DW-83 that its drift item (1) is cleared, leaving it open for G-6 (2).

**Acceptance Criteria:**
- Given the split, when Contracts is restored in package mode, then `dotnet list … package --include-transitive` shows no Blazor, Fluxor, FrontComposer, Fluent UI or AspNetCore.App entry, and the csproj declares none.
- Given the UI, when built and tested, then every moved type resolves from UI.Contracts, generated registrations still load through `ProjectsFrontComposerDomain`, and rendered badge slots and JSON names are unchanged.
- Given the gates, when run, then the dependency gate and manifest validation pass, the inspect gate runs (not SKIPPED) against UI.Contracts, and the negative case fails.

## Design Notes

FrontComposer reads badges only from `[ProjectionBadge]` on enum fields (`AttributeParser.cs:1075-1110`). Once the attributes are stripped, 8 generated views lose their enum badge and status-filter slots. Nothing renders those views, so users see no difference. Visible badges come from `ProjectVocabularyDescriptors`, whose values are pinned by the golden test. If the generator or inspect step raises a new warning about the missing badges, record it in Implementation Notes and suppress only that diagnostic ID, only in UI.Contracts.

## Verification

Use an isolated `NUGET_PACKAGES` with `CI=true` for package-mode commands. Run `dotnet` with the sandbox disabled, serially (`-m:1`).

**Commands:**
- `dotnet build Hexalith.Projects.slnx --configuration Debug -m:1` -- 0 warnings, 0 errors.
- `pwsh tests/tools/run-package-dependency-gate.ps1 -PackageDirectory <scratch>` -- passes.
- `dotnet restore src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj -p:UseHexalithProjectReferences=false` then `dotnet list src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj package --include-transitive` -- no forbidden entry.
- `pwsh tests/tools/run-frontcomposer-inspect-gate.ps1` -- PASSED.
- `python3 -m unittest discover -s tests/tools -p 'test_release_package_tools.py'` and `-p 'test_run_ci_workflow_gates.py'` -- pass.
- Contracts, Projects, Server, UI test projects: build each Debug and run the built xUnit assembly individually -- all pass.
- `python3 tools/planning/validate_production_authority.py --story-id 6.8` and `--validate-index` -- PASS.

Record commands, totals and the tested commit in Implementation Notes, with logs under `_bmad-output/implementation-artifacts/evidence/story-6-8-ui-contracts-split/`.

## Implementation Notes

Implemented 2026-11-15 on the uncommitted working tree over baseline `6bea95a3`; nothing is committed. `evidence/story-6-8-ui-contracts-split/source-manifest.tsv` (SHA-256 `0781e9f8…7b0783f28`) pins every tested file. Full commands and logs: `evidence/story-6-8-ui-contracts-split/README.md`.

- **Split.** `src/Hexalith.Projects.UI.Contracts` (non-packable, net11.0) references Contracts plus the FrontComposer Contracts, Shell and SourceTools references (source and package mode), `Microsoft.AspNetCore.App`, Fluxor and Fluent UI, with `NoWarn ASP0006` and `FlattenFrontComposerGeneratedFiles`. The 12 projections, `ProjectVocabularyDescriptors`, `VocabularyDescriptor`, `ProjectsFrontComposerDomain`, `ProjectMaintenancePanelState` and `ProjectMaintenancePanelStates` moved with their bytes unchanged except where noted; the namespace stays `Hexalith.Projects.Contracts.Ui`. `ProjectMaintenanceActions` and `ProjectMaintenanceCommandLifecycleStates` stay in Contracts as their own files. Contracts keeps only the EventStore and Conversations references; `Microsoft.Extensions.Validation` and `PreserveStableValidationDependency` are gone. The 7 staying enums lost `[ProjectionBadge]`, the FrontComposer using and the FrontComposer crefs; JSON names are unchanged.
- **Badges.** `ProjectVocabularyDescriptors` declares the slot next to each member's label (21 members of Lifecycle, ReferenceState, ResolutionResult and ProjectReasonCode). Private slot tables (no new public API) carry the pre-split slots of `ProjectContextAssemblyOutcome`, `ProjectContextFreshness` and `ProjectContextInclusionCheck` (14 members, including the safe-denial Neutral slot of `ProjectUnavailable`). `GetSeverity<TEnum>` keeps its signature and returns the 6bea95a3 slot for all 35 members of the seven stripped enums. It throws `InvalidOperationException` for any other member (unbadged enum, undefined value). `ProjectMaintenancePanelState` keeps its attributes for the generator and is not in the tables.
- **Generated output.** Baseline vs split output has the same 62 files; 54 are byte-identical, including all registrations. 7 views using the stripped enums lose badge/status-filter rendering. The unloaded generated MCP manifest loses the matching badge maps and changes those 7 schema fingerprints. As anticipated, the 7 views (the Design Notes said 8) and the unloaded manifest are the only changes. No new generator or inspect diagnostic appeared, so nothing is suppressed.
- **Tests.** The FrontComposer-metadata, mapping, descriptor and severity tests moved to `tests/Hexalith.Projects.UI.Tests/Contracts/`. Contracts.Tests keeps the 21 JSON-name cases. The new tests are: `ProjectVocabularyDescriptorTests` (35 golden pre-split slots, a check that every member of the seven stripped enums has a golden row, and the unknown-member throw), and `ProjectUiContractsBoundaryTests` (assembly placement, no UI assembly referenced by Contracts, and the 12 generated registrations loading through `AddHexalithDomain<ProjectsFrontComposerDomain>`).
- **Gates.** The dependency gate now fails on any `Hexalith.FrontComposer.*` item in the Contracts csproj and applies the SourceTools rule to UI.Contracts. The new sandboxed case `test_wrapper_rejects_frontcomposer_reference_in_contracts` covers both a package-mode PackageReference and a source-mode ProjectReference. It fails before any delegated command runs. The inspect gate targets UI.Contracts and now fails (exit 1) instead of printing SKIPPED when UI.Contracts is missing or has no `[Projection]`/`[Command]` file. `test_case_insensitive_duplicate_dependency_fails` now duplicates `Hexalith.EventStore.Contracts`, because Contracts has no Fluxor edge any more.
- **Manifest.** Contracts, `Hexalith.Projects`, Client and Testing were regenerated from the packed nuspecs. Contracts has `frameworkReferences: []`, Client keeps `Microsoft.AspNetCore.App`, and ServiceDefaults is unchanged. No graph carries FrontComposer, Fluxor or Fluent UI. Not caused by the split: the current Builds catalog (`73e60af8`) selects `Hexalith.Conversations.Contracts` 2.0.0, so the regenerated graphs replace the stale 1.6.0. This is recorded on DW-83.
- **Verification** (totals: failed/skipped are 0 everywhere).
  - Debug solution build: 0 warnings, 0 errors.
  - Contracts.Tests 205, Projects.Tests 691, UI.Tests 263 (also 263 in Release package mode), Server.Tests 1407. Server.Tests needs the `STORY61_DAPR_*` endpoints; a disposable in-memory `daprd` was used. Extra runs: Mcp 35, Cli 22, Client 125.
  - Package dependency gate and manifest validation: PASSED.
  - Inspect gate: PASSED, not SKIPPED.
  - Python suites: release package tools 20, CI workflow gates 28, authority guard 49.
  - `validate_production_authority.py --story-id 6.8` and `--validate-index`: PASS.
  - **Post-review rerun (2026-11-15). These totals supersede the pre-review totals above.** Source manifest SHA-256 `cb823dfc…b366a6efbd`; details in `evidence/story-6-8-ui-contracts-split/README.md`.
    - Debug solution build: 0 warnings, 0 errors.
    - Built assemblies with native XML: Contracts.Tests 205, Projects.Tests 691, UI.Tests 277, Server.Tests 1407 (fresh in-memory `daprd`), Mcp 35, Cli 22, Client 125. Failed and skipped are 0 in all of them.
    - UI.Tests Release package mode: build 0 warnings/errors, 277 passed.
    - Contracts package-mode graph: Conversations.Contracts 2.0.0, EventStore.Contracts 5.1.0, Ulid and Commons; framework `Microsoft.NETCore.App` only.
    - Package dependency gate: PASSED (5 consumers). Inspect gate: PASSED on 12 annotated files.
    - Python suites: release package tools 20 OK, CI workflow gates 28 OK, authority guard 49 OK.
    - Authority `--story-id 6.8` and `--validate-index`: PASS.
    - Generated output is byte-identical to the recorded after-set.
    - One Server.Tests run failed intermittently, 1407 total with 1 failure: `ProposeNewProjectEndpointTests.Confirm_SeparatorFingerprintsDoNotCollideAndUnsafeVariantsAreRejected(field: "description")` got 404 instead of 202 at line 587. The retry and all other full runs passed. This story changes neither that test nor any Server code. The log and XML are kept as `tests/Hexalith.Projects.Server.Tests.intermittent-failure.*`.
- **Review fixes (2026-11-15).** These came after the recorded run: the three context slot tables, the 14 golden rows, and the inspect gate failing on missing inputs. Checks re-run: UI.Tests Debug build (0 warnings/errors), `-class ...ProjectVocabularyDescriptorTests` 42/42, `run-ci-workflow-gates.ps1` PASSED, `test_run_ci_workflow_gates.py` 28 OK. A sandbox copy of the inspect gate exits 1 when UI.Contracts is missing and when it has no annotations. The UI.Tests total and the file hashes in the evidence predate these fixes. The golden rows and table entries were cross-checked against the `[ProjectionBadge]` attributes at HEAD: 35/35 match.
- **Closure.** DW-55 is `status: done` with a `closed:` line. DW-83 has a note that drift item (1) is cleared locally while main CI has not been rerun; it stays open for (2). Sprint status is untouched.

## Spec Change Log

## Review Triage Log

Pass 1 (2026-11-15). Layers: blind-hunter (B), edge-case-hunter (E), verification-gap (V).

| # | Finding | Verdict | Evidence | Route |
|---|---------|---------|----------|-------|
| B1, E6, E10 | Pre-split slots of `ProjectContextAssemblyOutcome`, `ProjectContextFreshness`, `ProjectContextInclusionCheck` (14 members, including Neutral `ProjectUnavailable`) were stripped without being re-declared; the golden table covers 21 of 35 members | medium | Before the split, `GetSeverity` answered for these members by reflection. The frozen boundary requires an explicit table with today's exact slot for every member, but `GetSeverity` now throws for them and nothing declares the safe-denial Neutral slot. | patch (G1) |
| B2 | Golden test compares two hand-written tables, not an independent frozen list | medium | Same root cause as G1. The coverage check only spans the four described enums, so dropping the context enums went undetected. | patch (G1) |
| B3, E1 | `GetSeverity` now throws for `ProjectMaintenancePanelState`; `Describe` vs `GetSeverity` exception types differ | false | The only `GetSeverity` callers are in UI.Contracts and its tests; nothing passes the panel-state enum. `Describe` throwing `KeyNotFoundException` on undefined values is unchanged baseline behavior. | reject |
| B4, E2, V1 | Inspect gate still exits 0 with SKIPPED when UI.Contracts is missing or unannotated; no automated check catches a target slip | medium | `run-frontcomposer-inspect-gate.ps1:22-26` keeps the stale Story 1.3 skip branch. V1 is pre-verified: no test runs the gate, and CI `project-gates` would stay green. | patch (G2) |
| B5, E3 | Contracts gate matches only `Hexalith.FrontComposer.*`; AspNetCore.App, Fluxor and Fluent UI re-additions pass it | false | Every reference that reaches the packed Contracts graph (FrameworkReference, package-mode PackageReference, imported items) is compared exactly by manifest validation in the same gate. Source-mode-only references cannot reach package consumers, and any code that uses them fails the package-mode CI build. | reject |
| B6 | Moved SourceTools rule has no negative test; new test duplicates the wrapper harness | low | The rule logic is unchanged and only retargeted; its missing negative test predates this story. The fix would add tests and a refactor for a path developers rarely touch. | reject |
| B7 | Manifest regenerated from the nuspecs it validates; Conversations 1.6.0→2.0.0 accepted without review | false | Delta reviewed here: only FrontComposer/Fluxor/FluentUI/ASP.NET/IdentityModel-chain removals, plus Conversations 2.0.0. The latter is what the committed Builds gitlink `73e60af8` (from 6bea95a3) selects; it is recorded on DW-83. | reject |
| B8, E7, V2 | Public types removed from the published Contracts package (shipped in v1.2.4) with no breaking-change marker for semantic-release | medium | `git ls-tree v1.2.4` ships them; `release.config.cjs` uses the default commit analyzer, so an unmarked commit releases as non-major. Conventional Commits gives one reading: the commit must carry `!`/`BREAKING CHANGE:`. | patch at commit time (G3) |
| B9 | DW-55 `closed:` line cites "the working tree" before a commit exists | low | The guard stays closed while 6-8 is `in-progress`, but the line will be stale once 6.8 commits; a direct rewording fixes it. | patch at commit time (G4) |
| B10 | epic-6-context Goal says "6.6–6.8 are in backlog" while 6-8 is in progress; DW-83 reason cites old pointers | low | The epic-context clause is stale; the fix is a direct one-clause edit. The DW-83 reason is historical, and the new note records `73e60af8`. | patch (G5b) for epic context; DW-83 part reject |
| B11 | `epic-6-context.md` CRLF→LF rewrite turns a 24-line change into a 54-line one | low | `git ls-files --eol` shows index CRLF, worktree LF; `--ignore-cr-at-eol` shrinks the diff to 24/23. Restoring CRLF is a direct correction. | patch (G5a) |
| B12 | Evidence lacks baseline test totals to prove moved tests reappear | false | The diff shows every removed Contracts test with its counterpart: two files renamed at 96–98% similarity, the metadata/mapping tests verbatim in `ProjectProjectionMetadataTests`, and coverage/uniqueness/severity in `ProjectVocabularyDescriptorTests`. | reject |
| B13 | Evidence environment carries HXW010/HXW011 and scratch paths | low | Pre-existing workspace tooling diagnostics, not caused by 6.8; the done rule accepts a recorded local run. | reject |
| B14, E5, E8, E9 | Generated views lose badge/status-filter rendering and the generated MCP manifest loses badge maps; "no runtime behavior change" and AC2 claims overstated | false | Generated `{T}View` components have no `[Route]`, no Shell `DynamicComponent` host and no Projects reference. No Projects host sets `FrontComposerMcpOptions.ManifestAssemblies`, and Mcp registers its own hand-written manifest. Nothing that renders or loads changes, and the spec's Design Notes disclose the generated-only delta. | reject |
| B15 | `Hexalith.Projects.Tests` now references UI.Contracts (Blazor) | low | Spec-directed, test-only, with no effect on package consumers; moving the leakage tests would be more than a direct correction. | reject |
| E4 | Evidence `*.log` files are gitignored (`.gitignore:92`) | medium | `git check-ignore` confirms it; the 161 tracked logs in earlier evidence folders were force-added. A plain `git add` would commit a README citing missing logs. | patch at commit time (G6) |
| E11 | Validation PackageReference and stable-floor target dropped rather than moved to UI.Contracts | false | The Code Map directed deletion. The Builds catalog pins `Microsoft.Extensions.Validation` 11.0.0 (`Props/Directory.Packages.props:426`) with `CentralPackageTransitivePinningEnabled=true`, so UI.Contracts' transitive Validation resolves to the stable floor. | reject |
