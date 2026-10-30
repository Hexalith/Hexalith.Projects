# Platform analyzer repair — 2026-10-29

The four authorized analyzer corrections are applied locally in Platform. The affected Composition test project builds in Debug with **0 warnings and 0 errors**; all **76 existing tests pass**, with zero errors, failures, skips or not-run cases. This is local source verification, not exact published tuple or runtime integration qualification. Story 6.4 remains **in-progress**.

## Reviewable change

[Platform patch](platform-analyzer-fixes.patch): four files, four insertions and one deletion, relative to Platform `494ecb32802d1454c15704066487fa206f5bcec0`.

- `AgentsAdmissionCurrentBoundaryTests.cs`: add `ConfigureAwait(false)` to private `Fixture.Issue`'s await (CA2007).
- `AgentsHostStartupBoundaryTests.cs` and `PortableDevelopmentCompositionTests.cs`: guard the `fault` theory parameter with `ArgumentNullException.ThrowIfNull` before fixture allocation (CA1062).
- `AcceptedLaunchBoundaryTests.cs`: guard `mutation` before fixture allocation (CA1062).

All InlineData, assertions, production code, dependency versions and analyzer configuration are unchanged. The input guards do not change any existing theory data case. The private helper's continuation only extracts the returned Envelope; it requires no synchronization context. Existing file newline bytes were preserved.

The owning repository has no root AGENTS.md or baseline; the root-declared AI.Tools baseline and inherited workspace EditorConfig were read, together with Platform build files, test project, README, architecture and candidate policy. These tests construct disposable metadata/resources; no live AppHost qualification was performed.

## Exact verification commands

Working directory for every command below: `/home/administrator/projects/hexalith/projects/references/Hexalith.Platform`.

The build uses the same environment flags as the retained earlier failing command. `NuGetAudit=false` disables restore vulnerability auditing only; it does not suppress C# analyzers. `MinVerVersionOverride=1.0.0` is a command-local build identity override, not a dependency version edit. Evaluated `EnableNETAnalyzers=true`, `TreatWarningsAsErrors=true` and `UseHexalithProjectReferences=true`; neither RunAnalyzers nor RunAnalyzersDuringBuild is overridden.

### debug-build

```bash
dotnet build tests/Hexalith.Platform.Composition.Tests/Hexalith.Platform.Composition.Tests.csproj -c Debug -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 -v:minimal
```

Exit 0; 55.908 seconds. Raw output: [debug-build.stdout](debug-build.stdout), [debug-build.stderr](debug-build.stderr).

### focused-tests

```bash
dotnet tests/Hexalith.Platform.Composition.Tests/bin/Debug/net10.0/Hexalith.Platform.Composition.Tests.dll -class Hexalith.Platform.Composition.Tests.AgentsAdmissionCurrentBoundaryTests -class Hexalith.Platform.Composition.Tests.AgentsHostStartupBoundaryTests -class Hexalith.Platform.Composition.Tests.PortableDevelopmentCompositionTests -class Hexalith.Platform.Composition.Tests.AcceptedLaunchBoundaryTests -result-xml /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/evidence/6-4-advisory-resolution-20261021/platform-analyzer-fixes-20261029/focused-tests.xml
```

Exit 0; 1.729 seconds. Raw output: [focused-tests.stdout](focused-tests.stdout), [focused-tests.stderr](focused-tests.stderr).

### analyzer-properties

```bash
dotnet msbuild tests/Hexalith.Platform.Composition.Tests/Hexalith.Platform.Composition.Tests.csproj -p:Configuration=Debug -getProperty:Configuration,EnableNETAnalyzers,RunAnalyzers,RunAnalyzersDuringBuild,TreatWarningsAsErrors,UseHexalithProjectReferences,HexalithBuildsResolvedRoot
```

Exit 0; 0.447 seconds. Raw output: [analyzer-properties.stdout](analyzer-properties.stdout), [analyzer-properties.stderr](analyzer-properties.stderr).

### whitespace

```bash
git diff --check
```

Exit 0; 0.006 seconds. Raw output: [whitespace.stdout](whitespace.stdout), [whitespace.stderr](whitespace.stderr).

## Focused coverage

| Existing test class | Passed |
| --- | ---: |
| AcceptedLaunchBoundaryTests | 22 |
| AgentsAdmissionCurrentBoundaryTests | 13 |
| AgentsHostStartupBoundaryTests | 28 |
| PortableDevelopmentCompositionTests | 13 |

The [raw xUnit XML](focused-tests.xml) was independently parsed to verify all 76 cases passed. Repeated `-class` filters select the union of the four classes; no method filter was combined with them. No new tests were needed for these mechanical corrections.

## Review and preservation

The bmad-build independent reviewer found no code defect after reinspection. Its one finding was incomplete validation while the build was running. Classification: low, resolved by the completed successful Debug build and 76-case XML; no deferred source defect. No additional code changes were needed.

`preservation-before.json` records original file hashes and all 13 repository HEAD/index identities; `preservation-result.json` confirms all checked historical evidence, existing story and sprint-status bytes, HEADs and indices remain unchanged. The existing tracking comment mentioning four errors is retained as historical checkpoint text; this new receipt records their local resolution. No staging, commit, push, publication, deployment, nested initialization or dependency change occurred.

## Concrete remaining tuple handoff

The prior [recorded owner handoff](../published-tuple-preparation-20261028/recorded-owner-checkpoint-20261029/current-publication-adoption-handoff.json) remains historical and unchanged. This patch resolves only its four-analyzer repair item.

Current coordinates (read locally; no fresh remote publication audit):

- Platform base: `494ecb32802d1454c15704066487fa206f5bcec0`, plus this uncommitted patch.
- Platform's recorded Builds gitlink: `e129a29e6c6e2f8a200581e09b861d75a9f3a7b8`.
- Actual sibling Builds source used by this Debug build: `e909ffd9fe9a6097e36d932010b983d029396968`.
- Prepared module tool: `4.38.2`, published source `2574bf2f3da5367d8801f0d7661d66155e5d90b0`.

These different identities are not a qualified tuple. The latest retained public package preparation still reports HXW011 for the untagged Platform identity and HXW009 for the Builds pin mismatch. It was not rerun here because this uncommitted test-only patch cannot resolve either gate. The prior owner-pin test also remains blocked by missing nested release-tag context; no nested checkout was initialized to bypass it.

1. Owner selects the exact published Builds/catalog tuple preserving intended dependencies (including current Tenants 5.11.1). Do not apply the stale historical 4.38.2 gitlink patch or assume the current source is published/accepted.
2. Under separate recording authorization, record the reviewed analyzer patch and approved matching Platform Builds pin/tool/catalog coordinates; retain the resulting full canonical revision. No future revision is guessed here.
3. Qualify that exact Platform candidate through owner integration and CI, with its real pinned dependencies and required runtime environment. This Debug build uses sibling source and cannot substitute for that gate. Resolve release-tag context through the approved owner qualification environment.
4. Obtain separate publication authorization, verify the resulting official Platform identity and package/source qualification, then approve consumer/umbrella adoption.
5. Retry actual public Projects package preparation on the adopted exact tuple; require HXW009/HXW011 to clear and finish remaining all-package/symbol acceptance. Refresh consumer evidence for the adopted inputs; do not promote earlier full-suite or gateway/store evidence to a new tuple without qualification.

No local analyzer/build/focused-test blocker remains. Platform/Builds tuple selection, exact-candidate integration/CI, publication and verified consumer adoption remain delivery blockers. The main story and sprint state remain in progress.
