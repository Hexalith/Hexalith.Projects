# Story 6.8 evidence: split Hexalith.Projects.UI.Contracts from Contracts

Recorded 2026-11-15 on the uncommitted working tree over baseline
`6bea95a379b6ab38d6ad71792da743072164ce3e`. This is the post-review rerun. It
supersedes the first recording and includes the review fixes: the context-enum
slot tables, the 14 extra golden rows, and the inspect gate failing on missing
inputs. `source-manifest.tsv` (SHA-256
`cb823dfc0bbca92b2fcfb1015a8025d37d2ba8f39effc11fbed644b366a6efbd`) lists the
SHA-256 of every tested source, test and tooling file the story adds or changes,
and every baseline path it deletes. Files under `_bmad-output/` (spec, ledger,
epic context, sprint status, this evidence) are excluded. No commit was made.

## Environment

- .NET SDK 11.0.100, Linux (WSL2), `~/.dotnet/tools` on `PATH` for `pwsh`. Every
  `dotnet` command ran serially (`-m:1`) outside the Bash sandbox.
- Debug/source commands used the Platform-owned source mapping from
  `Hexalith.Builds.Module.Cli prepare-build --manifest module/hexalith.module-manifest.v2.json --mode source`
  (status `prepared`; diagnostics HXW010/HXW011 only, see
  `build/source-preparation-summary.json`).
- Package-mode commands used an isolated `NUGET_PACKAGES` and
  `NUGET_HTTP_CACHE_PATH` in the session scratchpad, plus `CI=true`.
- Server.Tests used one freshly started, disposable `daprd` 1.19.1 sidecar
  (`--dapr-http-port 3610 --dapr-grpc-port 3611 --metrics-port 3612`, a single
  `state.in-memory` component named `statestore`), passed through
  `STORY61_DAPR_HTTP_ENDPOINT` and `STORY61_DAPR_GRPC_ENDPOINT`. The ports were
  confirmed free before it started; it was stopped and the ports were free again
  afterwards.

## Results

| Check | Command | Result |
| --- | --- | --- |
| Solution build | `dotnet build Hexalith.Projects.slnx --configuration Debug -m:1` | exit 0; 0 warnings, 0 errors (`build/solution-debug-build.log`) |
| Contracts package graph (AC1) | `dotnet restore src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj -p:UseHexalithProjectReferences=false` then `dotnet list src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj package --include-transitive` | exit 0, 0. Top level: Conversations.Contracts 2.0.0, EventStore.Contracts 5.1.0. Transitive: ByteAether.Ulid 1.4.2, Commons.Serialization 3.0.0, Commons.UniqueIds 3.0.0. Framework references: `Microsoft.NETCore.App` only (`package-mode/`) |
| Package dependency gate (AC3) | `pwsh tests/tools/run-package-dependency-gate.ps1 -PackageDirectory <scratch>/packages-rerun` | exit 0, PASSED; 5 isolated consumers compiled (`gates/package-dependency-gate.log`) |
| Manifest validation | inside the gate (`validate-nuget-packages.py`) | 5 packages verified. Packed graphs are in `package-mode/packed-nuspec-graphs.json`; none has a FrontComposer, Fluxor, Fluent UI or Components dependency |
| FrontComposer inspect gate (AC3) | `pwsh tests/tools/run-frontcomposer-inspect-gate.ps1` (`CI=true`) | exit 0, PASSED; 12 annotated UI.Contracts files (`gates/frontcomposer-inspect-gate.log`). If UI.Contracts is missing or has no `[Projection]`/`[Command]` file, the gate now exits 1 instead of printing SKIPPED |
| UI.Tests Release package mode | `dotnet build tests/Hexalith.Projects.UI.Tests/Hexalith.Projects.UI.Tests.csproj --configuration Release -warnaserror -p:UseHexalithProjectReferences=false -m:1`, then the built DLL | build exit 0, 0 warnings, 0 errors (FrontComposer 6.0.0 packages); 277 total, 0 failed, 0 skipped |
| Release package tools | `python3 -m unittest discover -s tests/tools -p 'test_release_package_tools.py'` | Ran 20, OK, including the negative case `test_wrapper_rejects_frontcomposer_reference_in_contracts` |
| CI workflow gates | `python3 -m unittest discover -s tests/tools -p 'test_run_ci_workflow_gates.py'` | Ran 28, OK |
| Production authority guard tests | `python3 -m unittest discover -s tests/tools -p 'test_production_authority_guard.py'` | Ran 49, OK |
| Authority | `python3 tools/planning/validate_production_authority.py --story-id 6.8` and `--validate-index` | PASS (Story 6.8 within production authority); PASS (index [6, 7, 8]) |

Built xUnit assemblies were run individually with
`dotnet tests/<project>/bin/Debug/net11.0/<project>.dll -noColor -result-xml <xml>`
(native XML in `tests/`):

| Assembly | Total | Failed | Skipped |
| --- | --- | --- | --- |
| Hexalith.Projects.Contracts.Tests | 205 | 0 | 0 |
| Hexalith.Projects.Tests | 691 | 0 | 0 |
| Hexalith.Projects.UI.Tests | 277 | 0 | 0 |
| Hexalith.Projects.Server.Tests (fresh in-memory `daprd`) | 1407 | 0 | 0 |
| Hexalith.Projects.Mcp.Tests (extra) | 35 | 0 | 0 |
| Hexalith.Projects.Cli.Tests (extra) | 22 | 0 | 0 |
| Hexalith.Projects.Client.Tests (extra) | 125 | 0 | 0 |

UI.Tests includes 42 `ProjectVocabularyDescriptorTests`. Among them are 35 golden
rows, one per member of the seven stripped enums, each checked against the
`[ProjectionBadge]` attributes at HEAD.

## Intermittent Server.Tests failure

In one full Server.Tests run during this rerun, 1 test failed:
1407 total, 1 failed, 0 skipped
(`tests/Hexalith.Projects.Server.Tests.intermittent-failure.{log,xml}`).

- Test:
  `Hexalith.Projects.Server.Tests.Queries.ProposeNewProjectEndpointTests.Confirm_SeparatorFingerprintsDoNotCollideAndUnsafeVariantsAreRejected(field: "description")`.
- Assertion: `response.StatusCode` should be `HttpStatusCode.Accepted` but was
  `HttpStatusCode.NotFound`, at
  `tests/Hexalith.Projects.Server.Tests/Queries/ProposeNewProjectEndpointTests.cs(587,0)`.
- Conditions: same Debug build and command as above, against a freshly started
  in-memory `daprd` on 3610/3611/3612.
- Retry: the same command against a new fresh `daprd` passed 1407/1407 with 0
  skipped. That run is the result recorded above.
- Earlier runs on this tree also passed 1407/1407. These were two earlier
  full-assembly runs and the first run of this rerun, which by mistake reused a
  `daprd` left running from an earlier run.
- As a diagnostic only, `-class Hexalith.Projects.Server.Tests.Queries.ProposeNewProjectEndpointTests`
  passed 77/77 three times in a row.
- This story changes neither that test file nor any `src/Hexalith.Projects.Server`
  code. The cause was not investigated.

## Generated output (AC2)

`generated/` compares FrontComposer generator output from Contracts at the
baseline with the output from UI.Contracts after the split (Debug/source). The
rerun regenerated the after-set, and it is byte-identical to the recorded one.

Both sets contain the same 62 file names. 54 files are byte-identical, including
every `*Registration`, `*Feature`, `*Actions` and `*Reducers` file. The 8 that
differ are:

- the 7 view components whose properties use a stripped vocabulary enum (they
  lose their enum badge and status-filter rendering), and
- the unloaded `FrontComposerMcpManifest.g.cs` (empty badge maps and new schema
  fingerprints for those 7 resources).

No Projects host loads that generated MCP manifest: `Hexalith.Projects.Mcp`
registers only `ProjectsMcpDescriptors.Manifest`. The generator and
`frontcomposer inspect` raised no new diagnostic, so nothing was suppressed.

`gates/package-dependency-gate-pre-manifest.log` is kept from the first
recording. It is the gate run whose packed nuspecs were used to rewrite
`tools/release-packages.json`, and it failed validation as expected against the
old manifest.
