# Hexalith.Builds EventStore Pin Alignment — 3.90.0 → 3.102.0

## `builds_qualifying_revision`

```
3d16d3e090ae822bc9cdc64c4156d31c9acf1146
```

Branch: `fix/p1r-3102-candidate`, worktree
`/tmp/claude-1000/-home-administrator-projects-hexalith-projects/db9a45af-df47-47c7-b1c2-39d9e83626f8/scratchpad/wt-builds-3102`,
created from base revision `e81e62770bcc72bc3d6722aefe6844775b82b6cf`.

Final worktree state: `git status --porcelain=v1` → **empty (clean)**;
`git rev-parse HEAD` → `3d16d3e090ae822bc9cdc64c4156d31c9acf1146`;
`git describe --tags --always --dirty` → `v4.27.0-28-g3d16d3e`.

## Commit

Exactly one commit on `fix/p1r-3102-candidate` (base repo at
`/home/administrator/projects/hexalith/projects/references/Hexalith.Builds` was never touched; only the worktree was
modified):

```
fix(runner): align EventStore pin to 3.102.0

Update SupportedPlatformPins, the module-manifest schema, positive
and negative fixtures, serialized evidence, test expectations, and
G-4 packaging tooling from the stale 3.90.0 EventStore pin to the
catalog-selected 3.102.0, leaving the intentional 3.88.0 and 3.70.1
negative-control pins untouched.

Recompute the coupled artifact_sha256 values in the readiness-evidence
YAML fixtures that assert the byte-exact hash of the four edited
module-run-evidence JSON fixtures, so HXE147 hash-mismatch checks
keep passing against the new fixture bytes.
```

36 files changed (29 files with the literal `3.90.0` → `3.102.0` token replacement, plus 7 coupled-hash
`artifact_sha256` fixtures discovered and fixed per the task's step-3 procedure — see "Coupled-hash discovery"
below).

### Commitlint validation

- **Pre-commit** (`cat <message-file> | node_modules/.bin/commitlint`, run from the worktree root): exit `0`,
  empty stdout/stderr (no rule violations) → **PASS**. Log:
  `logs/builds-3102/00-commitlint-precheck.log`, SHA-256
  `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` (empty file).
- **Post-commit** (`git -C <worktree> log -1 --pretty=%B | node_modules/.bin/commitlint`, run against the final
  amended commit): exit `0`, empty stdout/stderr → **PASS**. Log:
  `logs/builds-3102/01-commitlint-postcheck.log`, SHA-256
  `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` (identical empty-output hash).

`node_modules` was copied read-only from the base checkout into the worktree (byte-identical `package-lock.json`
confirmed by hash before copying — `git worktree add` does not materialize gitignored `node_modules`).

### Files changed

29 files with the exact `3.90.0` → `3.102.0` token replacement (verified whole-token, not a substring match):
`src/libraries/Hexalith.Builds.Tooling/Manifest/SupportedPlatformPins.cs`,
`schemas/hexalith.module-manifest.v1.json`,
`test/fixtures/module/positive/hexalith.module-manifest.v1.json`, the 14 `test/fixtures/module/negative/*.json`
fixtures listed in scope (excluding `superseded-platform-pin.json` and `tampered-platform-pin.json`, left at
`3.88.0`/`3.70.1` as required), the 4 `test/fixtures/evidence/*/evidence/*.json` fixtures
(`release-passed.json`, `down-run.json`, `test-no-tests.json`, `secret-metadata.json`), 5
`test/Hexalith.Builds.Module.Tests/*.cs` files, and 4 `Tools/*` files
(`test-publish-g4-tool-packages.ps1`, `README.md`, `test-g4-tool-package-contracts.ps1`,
`G4PackageQualification.functions.ps1`).

7 additional files with coupled `artifact_sha256` recomputation (see below):
`test/fixtures/evidence/positive/readiness.yaml`,
`test/fixtures/evidence/negative/{outcome-mismatch,policy-controls,coverage-shortfall,binding-mismatch,
no-executed-tests,secret-metadata}.yaml`.

`_bmad-output/`, `CHANGELOG.md`, and `Tools/package-version-audit.json` were **not** touched.

## Coupled-hash discovery (task step 3)

Running the Evidence.Tests suite after the literal-token edit surfaced 9 genuine failures (not weakened,
investigated to root cause). All 9 traced to one cause: `test/fixtures/evidence/positive/readiness.yaml` and five
`negative/*.yaml` fixtures assert `artifact_sha256` for the byte-exact hash of the four edited
`evidence/*/evidence/*.json` fixtures; editing those fixtures' bytes (the version string) changed their SHA-256,
so the `HXE147` ("The evidence artifact SHA-256 does not match its declared value.") check now legitimately
rejected them as mismatched.

Recomputed the real SHA-256 of each edited fixture (`sha256sum`) and replaced the coupled `artifact_sha256`
values with those exact values — no hash was invented:

| Fixture | Old SHA-256 (3.90.0 bytes) | New SHA-256 (3.102.0 bytes) | Coupled `artifact_sha256` locations updated |
| --- | --- | --- | --- |
| `evidence/positive/evidence/release-passed.json` | `8553bcef8ed8448096df28b201d064a0798d2ede41a27b5ebb1e9940abc61d92` | `383c0e20f243f57099e0d9a2a49bad777a1afb857287f35ab9e6aed2335b069e` | `positive/readiness.yaml`, `negative/outcome-mismatch.yaml`, `negative/policy-controls.yaml` (×2 occurrences), `negative/coverage-shortfall.yaml` |
| `evidence/negative/evidence/down-run.json` | `3e05ef4b1a3ac8fd6550ac13dd5a7348ce4ca47b0662399806ae58a236463ebf` | `39c5e188199cc626bd0b0f7084b60f8180756729406233256a6e311715e4ea6d` | `negative/binding-mismatch.yaml` |
| `evidence/negative/evidence/test-no-tests.json` | `1d744ec2543afbaa37015f7c58ea6a69dbe7f92d6a5e35e60573bf519b4a9772` | `8fc40d5e5ce4d9672ad76cbde54bddee161cdd4fa2f9439c778053da2701ca0f` | `negative/no-executed-tests.yaml` |
| `evidence/negative/evidence/secret-metadata.json` | `f70fd31222c80a5d5d8073fee2730a04e0cdfbf3c2a1e1cc016c1eb7d4d67cc7` | `fc488c91070f27a1082e70886840409238b9cf28d3528ff82a074b746bc62309` | `negative/secret-metadata.yaml` |

`test/fixtures/evidence/negative/artifact-hash-mismatch.yaml` (the deliberate hash-mismatch control) was
**not** touched — its `artifact_sha256` is intentionally `FFFF…FFF`, an unconditional mismatch regardless of the
referenced fixture's real bytes, and its test `ArtifactHashMismatchFailsClosedAsync` continued to pass (rejecting
with `HXE147`) throughout, unaffected.

After the fix, `dotnet exec test/Hexalith.Builds.Evidence.Tests/bin/Release/net10.0/Hexalith.Builds.Evidence.Tests.dll`
reported `Total: 31, Errors: 0, Failed: 0, Skipped: 0` (previously `Failed: 9`).

## Command ledger

All commands ran from `cwd` = the worktree root
(`/tmp/claude-1000/-home-administrator-projects-hexalith-projects/db9a45af-df47-47c7-b1c2-39d9e83626f8/scratchpad/wt-builds-3102`)
at `revision` = `3d16d3e090ae822bc9cdc64c4156d31c9acf1146` (clean, committed) unless noted otherwise.
`environment_overrides` for every `dotnet`/`pwsh` command in the serialized lane:
`NUGET_PACKAGES=…/wt-builds-3102-env/nuget`, `DOTNET_CLI_HOME=…/wt-builds-3102-env/dotnet-home`,
`TMPDIR=…/wt-builds-3102-env/tmp`. All ran with `dangerouslyDisableSandbox: true`.

| id | command | started_utc | ended_utc | exit | result | log (SHA-256) | notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 02-restore | `dotnet restore Hexalith.Builds.slnx --disable-parallel --verbosity minimal` | 2026-09-05T13:10:35.618Z | 2026-09-05T13:10:37.373Z | 0 | **PASS** | `logs/builds-3102/02-restore.log` (`67530598ced4ac01107ee13b3c38369872d9d2e291fbe7396b1c1fa3fc1d4ddd`) | Clean restore, all 6 projects. |
| 03-build | `dotnet build Hexalith.Builds.slnx --configuration Release --no-restore -p:GeneratePackageOnBuild=false -m:1` | 2026-09-05T13:10:41.873Z | 2026-09-05T13:10:50.402Z | 0 | **PASS** | `logs/builds-3102/03-build.log` (`17ef3b38698744b04bf8fc33fbdd2cc89d8247484ae904dd5ba47603257b3b76`) | `Build succeeded. 0 Warning(s) 0 Error(s)`. |
| 04-test-module | `dotnet test test/Hexalith.Builds.Module.Tests/Hexalith.Builds.Module.Tests.csproj --configuration Release --no-restore -m:1` | 2026-09-05T13:10:56.565Z | 2026-09-05T13:10:58.960Z | 5 | **INCONCLUSIVE** | `logs/builds-3102/04-test-module.log` (`fa4ba68314cc2a2ce500fc14d3869a2476dbed67cdc2183148b87d2360e4e80f`) | See "dotnet test environment blocker" below. Reproduced identically 4× (this row, `04b`, `04c` with sandbox disabled, and an earlier pre-amend attempt at 13:04:59–13:05:04Z, all exit 5 / "Zero tests ran" in ~100–260ms — deterministic, not a stall, so no cancel/retry-with-fresh-cache applies). |
| 05-test-evidence | `dotnet test test/Hexalith.Builds.Evidence.Tests/Hexalith.Builds.Evidence.Tests.csproj --configuration Release --no-restore -m:1` | 2026-09-05T13:10:58.965Z | 2026-09-05T13:11:01.841Z | 5 | **INCONCLUSIVE** | `logs/builds-3102/05-test-evidence.log` (`ebd3dc1f6fed3c57aa37b951e192d446595b0d4e2fdd2cbede526dec55aff3ad`) | Same environment blocker, reproduced on the second test project too. |
| 06-catalog | `pwsh -NoProfile -File ./Tools/test-authoritative-package-catalog.ps1` | 2026-09-05T13:11:18.852Z | 2026-09-05T13:11:21.219Z | 0 | **PASS** | `logs/builds-3102/06-catalog-test.log` (`a43f6ca3c93e7087ae20e004c9e72f545e8c19f760f01ebacda2f66476defa07`) | "Authoritative package catalog tests passed for 50 approved package identities and 3 shared package versions." |
| 07-audit | `pwsh -NoProfile -File ./Tools/validate-package-version-audit.ps1` | 2026-09-05T13:11:25.748Z | 2026-09-05T13:11:45.223Z | 0 | **PASS** | `logs/builds-3102/07-audit-validate.log` (`f616142d120b498943f6fa70ec59907781a5bfd15d6f98604d4c0775b84bbf33`) | "Package version audit validation passed for 286 packages, 141 families, and 1 source(s)." Zero mismatches — `Tools/package-version-audit.json` needed no hand-edit or regeneration, confirming it was already 3.102.0-catalog-ancestor. |
| 08-g4-tool-contracts | `pwsh -NoProfile -File ./Tools/test-g4-tool-package-contracts.ps1 -Version 0.0.0-p1r3102.1 -RequireControls -RetainPackageDirectory` | 2026-09-05T13:11:54.537Z | 2026-09-05T13:12:32.824Z | 0 | **PASS** | `logs/builds-3102/08-g4-tool-contracts.log` (`dd4836418c8f949a78a10b2bc7e8fd178bd3221b0475223873e9f79a9d14531c`) | No stall (38s total). Internally re-ran Module.Tests (114/114) and Evidence.Tests (31/31) via its own native runner invocation, packed both CLI tools, restored them as global tools from the packed output, and executed all packaged-command controls. `sourceTree.clean=true`, `sourceTree.revision=3d16d3e090ae822bc9cdc64c4156d31c9acf1146`, `releaseEligible=true`, `ineligibilityReasons=[]`. See artifacts below. |

### Supplementary fallback-ladder evidence (not contract rows; recorded per the org's documented fallback ladder for an environment-blocked `dotnet test`)

| id | command | started_utc | ended_utc | exit | result | log (SHA-256) | notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 04f-test-module-direct | `dotnet exec test/Hexalith.Builds.Module.Tests/bin/Release/net10.0/Hexalith.Builds.Module.Tests.dll` | 2026-09-05T13:11:09.149Z | 2026-09-05T13:11:10.203Z | 0 | PASS (supplementary) | `logs/builds-3102/04f-test-module-direct.log` (`68cde42de0b9601a99e61a414072c19e9b3aa744f34b8b298d0b2adb828a99b8`) | `Total: 114, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0`. |
| 05f-test-evidence-direct | `dotnet exec test/Hexalith.Builds.Evidence.Tests/bin/Release/net10.0/Hexalith.Builds.Evidence.Tests.dll` | 2026-09-05T13:11:10.211Z | 2026-09-05T13:11:10.829Z | 0 | PASS (supplementary) | `logs/builds-3102/05f-test-evidence-direct.log` (`e5eedc52cab656b3fe6eccca40fc5f93da486a67a98408fb9299362c177bdb86`) | `Total: 31, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0` (after the coupled-hash fix). |
| evidence-direct-pre-fix | `dotnet exec .../Hexalith.Builds.Evidence.Tests.dll` (diagnostic run **before** the coupled-hash fix, retained per "retain every first result") | — | — | 0 (host), 9 test failures | diagnostic (superseded by 05f) | `logs/builds-3102/evidence-direct-full.log` (`7be716d652ec5082af6d75cc16635d26ca547ce82b2b1f85b2423275bc45e64a`) | `Total: 31, Failed: 9` — this is the run that surfaced the coupled-hash `HXE147` mismatches; all 9 failures trace to the single root cause documented above. |
| 04b-test-module-rerun | `dotnet test test/Hexalith.Builds.Module.Tests/…` (rerun, same env, pre-amend revision) | 2026-09-05T13:05:23.255Z | 2026-09-05T13:05:28.605Z | 5 | INCONCLUSIVE (retained duplicate) | `logs/builds-3102/04b-test-module-rerun.log` (`d29b50dd538a208ea2885ebf20185c413361b42b5364643b0034a24cc7de7a86`) | Confirms row 04 is reproducible, not a one-off contention flake. |
| 04c-test-module-nosandbox | same command, `dangerouslyDisableSandbox: true` explicitly re-verified, pre-amend revision | 2026-09-05T13:05:39.972Z | 2026-09-05T13:05:47.353Z | 5 | INCONCLUSIVE (retained duplicate) | `logs/builds-3102/04c-test-module-nosandbox.log` (`bdc9aee98f19244c454c3daedd150e65c6d95cac8e7deebc464c990705f424ba`) | Rules out sandbox socket-binding as the cause. |
| 04g-test-module-verbose | `dotnet exec .../Hexalith.Builds.Module.Tests.dll -verbose` | — | — | 0 | PASS (evidence-of-detail) | `logs/builds-3102/04g-test-module-verbose.log` (`2c17f5297e45aa3b246c75d20240f756805613945b1d71cb48091be95b38e829`) | Per-test `[STARTING]`/`[FINISHED]` trace confirming `tampered-platform-pin.json`, `superseded-platform-pin.json`, and `invalid-profile.json` theory cases all ran and finished without `[FAIL]`. |

### `dotnet test` environment blocker (root cause)

`dotnet test <csproj> --configuration Release --no-restore -m:1` — the exact contract command — fails
immediately and deterministically with exit `5` ("Zero tests ran", generic `error: 1`, no further diagnostic) on
**both** test projects in this environment. This repository's `global.json` selects
`"test": {"runner": "Microsoft.Testing.Platform"}`, so `dotnet test` launches the built assembly in
`--server dotnettestcli --dotnet-test-pipe <path>` IPC mode; the outer `dotnet test` driver and the inner
Microsoft.Testing.Platform test host fail to complete their handshake over that pipe (confirmed via
`--diagnostic --diagnostic-verbosity trace`: the inner host's `.diag` log stops right after
"Setting PlatformExitProcessOnUnhandledException", 21 lines total, with no further activity or exception logged).

This is reproducible: with the sandbox on, with `dangerouslyDisableSandbox: true`, with and without a `TMPDIR`
override, on both `Hexalith.Builds.Module.Tests` and `Hexalith.Builds.Evidence.Tests`, and identically before and
after the amend/coupled-hash fix — ruling out sandboxing, the isolated env-var directories, and the candidate
change itself as the cause. Each failing attempt completes in well under a second (no hang), so this is a
deterministic tooling incompatibility, not a stall requiring cancel-and-retry.

The built assemblies themselves are genuinely correct: invoked directly (`dotnet exec <dll>`, bypassing the MTP
IPC orchestrator — the fallback explicitly authorized by this repository's own
`hexalith-llm-instructions.md` "fallback validation ladder" for exactly this "environment-blocked `dotnet test`"
scenario), both suites are fully green (114/114 and 31/31), and Tools/test-g4-tool-package-contracts.ps1's own
internal test invocation (row 08, which does not go through the `dotnet test` CLI) independently re-confirms
114/114 and 31/31 passing. Rows `04` and `05` are recorded **INCONCLUSIVE** rather than PASS or FAIL: the
contract-mandated command itself did not produce a meaningful test result (zero tests were even discovered), but
this is an execution-orchestration failure external to the candidate's test content, not evidence the candidate's
tests fail.

## Static alignment verification (task step 7 / contract "Builds alignment and qualification matrix → Static alignment")

1. **All 13 catalog rows resolve `HexalithEventStoreVersion=3.102.0`.** `Props/Directory.Packages.props` line 8:
   `<HexalithEventStoreVersion Condition="'$(HexalithEventStoreVersion)' == ''">3.102.0</HexalithEventStoreVersion>`;
   13 `<PackageVersion Include="Hexalith.EventStore.*" Version="$(HexalithEventStoreVersion)" />` rows (lines
   40–52: `Admin.Abstractions`, `Admin.Server`, `Aspire`, `Client`, `Contracts`, `DomainService`, `Gateway`,
   `RestApi.Generators`, `Server`, `ServiceDefaults`, `SignalR`, `Testing`, `Testing.Integration`) all resolve
   through that property. Row 06 (`test-authoritative-package-catalog.ps1`) independently confirms structural
   validity ("50 approved package identities and 3 shared package versions"), and row 07
   (`validate-package-version-audit.ps1`) confirms the full audit passes with zero stale mismatches at this
   revision (286 packages, 141 families, 1 source).

2. **`SupportedPlatformPins.EventStoreVersion`, the manifest schema, positive fixtures, serialized evidence, and
   coupled hashes use `3.102.0`.**
   - `src/libraries/Hexalith.Builds.Tooling/Manifest/SupportedPlatformPins.cs`:
     `public const string EventStoreVersion = "3.102.0";`
   - `schemas/hexalith.module-manifest.v1.json` line 65: `"eventStoreVersion": { "const": "3.102.0" }`.
   - `test/fixtures/module/positive/hexalith.module-manifest.v1.json`: `"eventStoreVersion": "3.102.0"`.
   - All 4 evidence-fixture `platform.eventStoreVersion` values updated to `"3.102.0"`, and their coupled
     `artifact_sha256` readiness-YAML expectations recomputed (see "Coupled-hash discovery" above) — row 08's
     `g4-tool-package-inventory.json.qualification.fixtures.files` entry for
     `evidence/positive/evidence/release-passed.json` independently confirms SHA-256
     `383c0e20f243f57099e0d9a2a49bad777a1afb857287f35ab9e6aed2335b069e` (matches the byte-exact hash used in the
     fixture edits).

3. **Explicit active-runner negatives reject both `3.88.0` and `3.70.1` with exactly `HXM016`.**
   `test/fixtures/module/negative/superseded-platform-pin.json` (`3.88.0`, untouched) and
   `test/fixtures/module/negative/tampered-platform-pin.json` (`3.70.1`, untouched) are both covered by
   `PersistedFixtureAssetTests.LoadManifestNegativeControlReturnsStableRule` (passed, per rows `04f`/`04g`/`08`).
   Row 08's packaged-CLI evidence quotes the exact rejection for both:
   ```
   module-negative-tampered-platform-pin-output.json:
   {"status":"failed","outcome":{"exitCode":"UsageOrManifest","phase":"Manifest","category":"Manifest","ruleId":"HXM016"},
    "diagnostics":[{"ruleId":"HXM016", ... ,"field":"platform.eventStoreVersion", ...}]}
   module-negative-superseded-platform-pin-output.json: (byte-identical payload, same HXM016 rejection)
   ```

4. **Unrelated negative fixtures retain their original rule IDs, including `invalid-profile.json` → `HXM009`.**
   `test/fixtures/module/negative/invalid-profile.expected.json` is unchanged (`"outcomeRuleId": "HXM009"`), and
   row 08's packaged-CLI evidence confirms live behavior:
   ```
   module-negative-invalid-profile-output.json:
   {"status":"failed","outcome":{"exitCode":"UsageOrManifest","phase":"Manifest","category":"Manifest","ruleId":"HXM009"},
    "diagnostics":[{"ruleId":"HXM009","message":"The manifest contains an invalid qualification profile.", ...}]}
   ```
   All other negative-fixture `.expected.json` files (`unknown-schema`, `unknown-field`, `duplicate-id`,
   `absolute-path`, `path-escape`, `missing-descriptor`, `placeholder`, `secret-bearing`,
   `malformed-dependency`, `duplicate-json-key`, `unsupported-profile-class`, `missing-required-value`) were not
   edited and their theory cases in `PersistedFixtureAssetTests.LoadManifestNegativeControlReturnsStableRule`
   continued to pass.

5. **The deliberate evidence-hash mismatch control remains invalid.**
   `test/fixtures/evidence/negative/artifact-hash-mismatch.yaml` (`artifact_sha256: FFFF…FFF`, intentionally
   never matching real fixture bytes) was **not** edited. Row 08's packaged-CLI evidence confirms it is still
   rejected:
   ```
   evidence-negative-artifact-hash-mismatch-output.json:
   {"status":"failed","outcome":{"exitCode":"EvidenceSchemaOrPolicy","phase":"Evidence","category":"EvidencePolicy","ruleId":"HXE147"},
    "diagnostics":[{"ruleId":"HXE147","message":"The evidence artifact SHA-256 does not match its declared value.",
     "field":"artifact_sha256", ..., "source":"test/fixtures/evidence/negative/artifact-hash-mismatch.yaml", ...}]}
   ```
   (`ReadinessEvidenceValidatorTests.ArtifactHashMismatchFailsClosedAsync` and its snapshot-matrix counterpart
   both passed in rows `05f`/`08`.)

## Artifacts (row 08, `Tools/test-g4-tool-package-contracts.ps1 -Version 0.0.0-p1r3102.1`)

Retained under
`/tmp/claude-1000/-home-administrator-projects-hexalith-projects/db9a45af-df47-47c7-b1c2-39d9e83626f8/scratchpad/wt-builds-3102-env/tmp/hexalith-builds-g4-packages-45f9ca64250444a88f43486754b65b1b/`
(`-RetainPackageDirectory`):

| Artifact | SHA-256 |
| --- | --- |
| `Hexalith.Builds.Module.Cli.0.0.0-p1r3102.1.nupkg` | `69c9c71a1dd3a89be397190d389813d8c0e0b7248160953600a481f6741ace45` |
| `Hexalith.Builds.Module.Cli.0.0.0-p1r3102.1.snupkg` | `2bb5e3f52a66482e478094bd6980322b3bde982400d1e1e7d71eaf2761c26311` |
| `Hexalith.Builds.Evidence.Cli.0.0.0-p1r3102.1.nupkg` | `1709c7194731ae8ad59643a881af5a0f7ee93db5c1eddb71ba991de5c350b904` |
| `Hexalith.Builds.Evidence.Cli.0.0.0-p1r3102.1.snupkg` | `3c960e4badc24f098e158dfe26de5aa4a329f59e2885147e23ca4d3f957cd2b2` |
| `g4-tool-package-inventory.json` | `5a22f98606e5d3ad8ef78c7af2345427288a97c5fde2844bcde61f68ac0571e6` |
| `qualification-evidence/qualification.log` | `c3b43ff52437d0cad72d8ea32d4b1ba41995db16715919a97c8166fbb4fd7fbd` |
| `qualification-evidence/source-release-passed.json` | `383c0e20f243f57099e0d9a2a49bad777a1afb857287f35ab9e6aed2335b069e` |

(31 additional per-control `qualification-evidence/*-output.json` files are listed with their SHA-256 inside
`g4-tool-package-inventory.json` itself, including the `module-negative-*` and `evidence-negative-*` rows quoted
above.) `g4-tool-package-inventory.json.qualification.sourceTree` = `{"clean": true, "revision":
"3d16d3e090ae822bc9cdc64c4156d31c9acf1146"}`; `qualification.releaseEligible` = `true`;
`qualification.ineligibilityReasons` = `[]`.

## Final worktree state (task step 8)

```
$ git status --porcelain=v1
(empty — clean)
$ git rev-parse HEAD
3d16d3e090ae822bc9cdc64c4156d31c9acf1146
```

## Disposition summary

The candidate alignment is **substantively PASS with one honestly-recorded environment-tooling INCONCLUSIVE**.
All 29 files carrying the literal `3.90.0` EventStore pin were updated to `3.102.0` (verified whole-token, no
substring collisions), leaving the two intentional stale-pin negative controls (`3.88.0`, `3.70.1`) untouched;
investigating the resulting Evidence.Tests failures surfaced a genuine coupled-hash issue (readiness-evidence YAML
fixtures asserting the byte-exact SHA-256 of four edited evidence fixtures), which was fixed by recomputing the
real hashes — never invented — and both suites are now fully green (114/114 Module, 31/31 Evidence). The change
is committed as one clean, commitlint-validated commit (`3d16d3e090ae822bc9cdc64c4156d31c9acf1146`) with the
worktree clean at the end. Restore, build, the authoritative-catalog test, the package-version-audit validator
(zero mismatches, no regeneration needed), and the G-4 packaged-tool gate (with `-RequireControls
-RetainPackageDirectory`, `releaseEligible=true`, exact worktree revision echoed back) all genuinely PASS,
independently re-confirming both test suites green via a non-`dotnet-test` execution path. The one blemish: the
contract's exact `dotnet test <csproj> --configuration Release --no-restore -m:1` command itself fails
deterministically with exit 5 ("Zero tests ran") on both projects in this environment — a Microsoft.Testing.Platform
`--server dotnettestcli` IPC handshake failure between the `dotnet test` driver and the test host, reproduced
identically with sandbox on/off and before/after the fix — which is recorded honestly as **INCONCLUSIVE** (not
silently passed or hidden) per the org's own documented fallback-ladder guidance, backed by fallback direct-assembly
execution and the G-4 gate's independent internal test run both showing the actual test content is fully correct.
