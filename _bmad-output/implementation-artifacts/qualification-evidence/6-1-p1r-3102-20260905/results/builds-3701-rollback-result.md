# Hexalith.Builds Rollback Alignment — EventStore 3.70.1

## Identity

| Field | Value |
| --- | --- |
| `rollback_builds_revision` | `6ea4c27ade695964ee3c95b1d28e0058f4e1430e` |
| Base revision (do not touch) | `e81e62770bcc72bc3d6722aefe6844775b82b6cf` |
| Branch | `fix/p1r-3701-rollback` |
| Worktree | `/tmp/claude-1000/-home-administrator-projects-hexalith-projects/db9a45af-df47-47c7-b1c2-39d9e83626f8/scratchpad/wt-builds-3701` |
| `git describe --tags --always --dirty` (post-lane) | `v4.27.0-28-g6ea4c27` |
| `git status --porcelain=v1` (post-lane) | empty (clean) |

## Commit

Single commit, `fix(runner): bind EventStore pin to 3.70.1 rollback`, 38 files changed (41 insertions, 41 deletions):

```
fix(runner): bind EventStore pin to 3.70.1 rollback

Rebind the catalog default, platform pins, manifest schema, positive
fixtures, serialized evidence, and coupled hashes from the stale
3.90.0 runner corpus to the accepted rollback version 3.70.1.
Repoint the tampered-platform-pin fixture to the selected candidate
3.102.0 so it proves the rollback worktree rejects that pin with
HXM016, and leave the superseded 3.88.0 fixture untouched.
```

Changed files: `Props/Directory.Packages.props`; `Tools/G4PackageQualification.functions.ps1`, `Tools/README.md`, `Tools/test-g4-tool-package-contracts.ps1`, `Tools/test-publish-g4-tool-packages.ps1`; `schemas/hexalith.module-manifest.v1.json`; `src/libraries/Hexalith.Builds.Tooling/Manifest/SupportedPlatformPins.cs`; 5 files under `test/Hexalith.Builds.Module.Tests/`; 8 files under `test/fixtures/evidence/**` (4 negative `.json` evidence fixtures + coupled-hash `.yaml` files: `binding-mismatch.yaml`, `coverage-shortfall.yaml`, `no-executed-tests.yaml`, `outcome-mismatch.yaml`, `policy-controls.yaml`, `secret-metadata.yaml`, plus `readiness.yaml`); 16 files under `test/fixtures/module/**` (15 negative fixtures whose stale `3.90.0` pin became `3.70.1`, plus the positive manifest). `test/fixtures/module/negative/tampered-platform-pin.json` was set to `3.102.0` (not `3.70.1`) per the reciprocal-rollback requirement. `test/fixtures/module/negative/superseded-platform-pin.json` (`3.88.0`) was **not** touched. `Tools/package-version-audit.json` was deliberately **not** modified in this commit — see "Known blocker" below.

**Commitlint validation (pinned repo CLI, `node_modules/.bin/commitlint`):**

- Pre-commit (`printf '%s\n' "<message>" | node_modules/.bin/commitlint`): **exit 0**, no rule violations.
- Post-commit (`git log -1 --pretty=%B | node_modules/.bin/commitlint`) against final revision `6ea4c27ade695964ee3c95b1d28e0058f4e1430e`: **exit 0**, no rule violations.

Note on process: the worktree's `node_modules` (gitignored, identical `package-lock.json` to the base checkout) was copied in from the base checkout rather than reinstalled, to avoid a network-dependent `npm ci`; this did not touch the base checkout's working tree.

An earlier attempt amended in a naively-regenerated `Tools/package-version-audit.json` (commit `0ed1141b851f50c73cd36a1a3702172885b09665`), which was superseded once the audit tool's ancestor-based self-consistency requirement was understood (see "Known blocker"). The final, retained commit is `6ea4c27ade695964ee3c95b1d28e0058f4e1430e`.

## Known blocker: `validate-package-version-audit.ps1` fails at the single-commit rollback revision (recorded honestly, not fabricated)

**Result: FAIL, root cause identified, not resolvable within the "exactly one commit" boundary.**

`Tools/package-version-audit.json` was intentionally left unmodified in the single commit. Running `pwsh -NoProfile -File ./Tools/validate-package-version-audit.ps1` at `6ea4c27ade695964ee3c95b1d28e0058f4e1430e` fails with 14 errors, the operative ones being:

```
- Audit catalogSha256 does not match the evaluated catalog declaration bytes.
- Internal package 'Hexalith.EventStore.Admin.Abstractions' cannot downgrade accepted version floor '3.102.0' to catalog version '3.70.1'.
  (...same for the other 11 Hexalith.EventStore.* package IDs)
```

Root cause and why this cannot be fixed inside a single commit: `Tools/audit-central-package-versions.ps1` (the only sanctioned generator) refuses to run against a dirty catalog — it requires `Props/Directory.Packages.props` at the working tree to byte-match the catalog committed at `git rev-parse HEAD`. Concretely tested:

1. Generating the audit while the catalog change is *uncommitted* fails: `Central package freshness audit failed: catalog 'Props/Directory.Packages.props' is dirty relative to generated-from revision 'e81e62770bcc72bc3d6722aefe6844775b82b6cf'.`
2. Generating the audit *after* committing the catalog change, then folding the regenerated audit into that same commit via `git commit --amend`, produces a **new** commit hash; the audit's self-recorded `generatedFromRevision` (the pre-amend hash) is then not reachable from the new HEAD. Validation then fails differently: `Package version audit validation failed with 1 error(s): - Generated-from revision '64f55269f39d49ebed2d7176111a6feaa833fb32' is not an ancestor of the audited worktree HEAD.` (confirmed via `git merge-base --is-ancestor` in `validate-package-version-audit.ps1` line ~809).
3. A commit's hash is a function of its own tree (which would have to embed that same hash inside the audit JSON) — a true single-commit self-reference is a SHA-1 preimage problem, not achievable in this session. A audit that is both (a) generated from a real, already-committed catalog state and (b) an ancestor of the final HEAD **requires at least two commits**: one that commits the catalog/runner/fixture change, and a second, child commit that adds the regenerated audit referencing the first as its `generatedFromRevision`.

Given the explicit boundary "Exactly one commit for this rollback alignment," this rollback intentionally stays at one commit and accepts that `validate-package-version-audit.ps1` fails there with the anti-downgrade-floor error shown above. This is a genuine, reproducible tool/process constraint discovered during qualification, not an oversight — the base repository's own history shows the identical two-commit pattern (`308e392` "fix: update HexalithEventStoreVersion to 3.102.0" immediately followed by `e81e627` "fix: update package version audit with new revisions and timestamps"). Unblocking this requires either (a) explicit authorization for a second, dependent "build: refresh package-version-audit for 3.70.1 rollback" commit on this branch, or (b) a tooling change to `audit-central-package-versions.ps1`/`validate-package-version-audit.ps1` to accept a two-phase (uncommitted-catalog) generation mode. Diagnostic regeneration attempts (both the dirty-catalog refusal and the ancestor-check failure) are retained in the log ledger below for evidence.

All other static-alignment and reciprocal-rejection checks (catalog structural test, Module/Evidence unit tests, and the live packaged G-4 tool's fixture/evidence assertions) pass at `6ea4c27ade695964ee3c95b1d28e0058f4e1430e` and independently corroborate that the catalog, runner corpus, and fixtures are correctly bound to `3.70.1` — see below.

## Known blocker: `dotnet test ... -m:1` reports "Zero tests ran" (environment-specific; fallback evidence retained)

**Result: contract-exact command FAIL (exit 5) — reproducible; sanctioned fallback command PASS.**

The contract's exact command `dotnet test <project>.csproj --configuration Release --no-restore -m:1` fails in this sandboxed environment with exit code 5 and "Zero tests ran," for **both** `Hexalith.Builds.Module.Tests` and `Hexalith.Builds.Evidence.Tests`, reproduced across 6 independent attempts (with/without `dangerouslyDisableSandbox`, with a short `TMPDIR`, from a short-path copy of the worktree, and with `-diagnostic`). The Microsoft.Testing.Platform diagnostic log (`TestResults/*.diag`) shows the `dotnet test` MSBuild driver invokes the built test host with `--server dotnettestcli --dotnet-test-pipe /tmp/<guid>` (a named-pipe IPC handshake between the outer `dotnet test` process and the inner MTP test host); with `-m:1` this handshake never completes and the driver reports zero tests with no further diagnostic.

Root cause isolated: **it is specifically `-m:1`** — not sandboxing, not path length, not `TMPDIR`. The identical build, run without `-m:1` (still executed strictly serially, one command at a time, satisfying "serialize build and test lanes that share generated output"), passes cleanly: Module.Tests 114/114, Evidence.Tests 31/31. Running the built assembly directly (`./test/.../Hexalith.Builds.Module.Tests --diagnostics`) also passes 114/114 — this is the sanctioned fallback-ladder step 2 from `hexalith-llm-instructions.md` ("build the target test project and invoke the built test assembly directly"). Per that same instructions file: "Record the exact broad-gate blocker separately from the focused evidence that did run, and never weaken the build gate to hide the blocker" — both are recorded below.

## Command ledger

All commands run from `cwd = /tmp/claude-1000/-home-administrator-projects-hexalith-projects/db9a45af-df47-47c7-b1c2-39d9e83626f8/scratchpad/wt-builds-3701`, `repository = Hexalith.Builds`, `revision = 6ea4c27ade695964ee3c95b1d28e0058f4e1430e` (final, post-commit, clean), with `environment_overrides = NUGET_PACKAGES=.../wt-builds-3701-env/nuget, DOTNET_CLI_HOME=.../wt-builds-3701-env/dotnet-home, TMPDIR=.../wt-builds-3701-env/tmp`, all run with `dangerouslyDisableSandbox: true` unless noted. Logs under `/tmp/claude-1000/-home-administrator-projects-hexalith-projects/db9a45af-df47-47c7-b1c2-39d9e83626f8/scratchpad/logs/builds-3701/`.

| id | command | started_utc | ended_utc | exit | result | log | log_sha256 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| R01 | `dotnet restore Hexalith.Builds.slnx --disable-parallel --verbosity minimal` | 2026-09-05T13:16:10Z | 2026-09-05T13:16:12Z | 0 | PASS | R01-restore.log | ea155fd23d0b09f7428b1fbc53a71759339fa5d098d5e874eca5c2ec059ac3cb |
| R02 | `dotnet build Hexalith.Builds.slnx --configuration Release --no-restore -p:GeneratePackageOnBuild=false -m:1` | 2026-09-05T13:16:12Z | 2026-09-05T13:16:18Z | 0 | PASS (0 warnings, 0 errors) | R02-build.log | ca7f9dfcba1b503bc80e60d987f4ea5aeb11d872e27a236947a115621a487d98 |
| R03 | `dotnet test test/Hexalith.Builds.Module.Tests/Hexalith.Builds.Module.Tests.csproj --configuration Release --no-restore -m:1` (contract-exact) | 2026-09-05T13:16:27Z | 2026-09-05T13:16:29Z | 5 | FAIL — "Zero tests ran" (environment `-m:1`/MTP-pipe blocker; see above) | R03-module-m1.log | cced927950d720d54f0c95b7d703e9c337fd6aa4562a96bd7f33ac0e878968fe |
| R03b | `dotnet test test/Hexalith.Builds.Module.Tests/Hexalith.Builds.Module.Tests.csproj --configuration Release --no-restore` (sanctioned fallback, still serialized, no other lane concurrent) | 2026-09-05T13:16:29Z | 2026-09-05T13:16:33Z | 0 | PASS — 114/114 (0 failed, 0 skipped) | R03b-module-fallback.log | 0e1aa5983564a341865769eb3842eb0ea5654d58e7d11171277909f1a6d09721 |
| R04 | `dotnet test test/Hexalith.Builds.Evidence.Tests/Hexalith.Builds.Evidence.Tests.csproj --configuration Release --no-restore -m:1` (contract-exact) | 2026-09-05T13:16:33Z | 2026-09-05T13:16:36Z | 5 | FAIL — "Zero tests ran" (same blocker) | R04-evidence-m1.log | 13a55b8fe254cc1f7991471e684aa3fb52add0eaf8abad9049e3032c46bbbbcc |
| R04b | `dotnet test test/Hexalith.Builds.Evidence.Tests/Hexalith.Builds.Evidence.Tests.csproj --configuration Release --no-restore` (fallback) | 2026-09-05T13:16:36Z | 2026-09-05T13:16:38Z | 0 | PASS — 31/31 (0 failed, 0 skipped) | R04b-evidence-fallback.log | d6379d1e1ca32a0ab795a312f3b58dd9f032cc2384dcb06d1813b5c00a11a154 |
| R05 | `pwsh -NoProfile -File ./Tools/test-authoritative-package-catalog.ps1` | 2026-09-05T13:16:46Z | 2026-09-05T13:16:48Z | 0 | PASS — "50 approved package identities and 3 shared package versions" | R05-catalog.log | a43f6ca3c93e7087ae20e004c9e72f545e8c19f760f01ebacda2f66476defa07 |
| R06 | `pwsh -NoProfile -File ./Tools/validate-package-version-audit.ps1` | 2026-09-05T13:16:48Z | 2026-09-05T13:17:07Z | 1 | FAIL — anti-downgrade guard (audit not regenerated; see "Known blocker") | R06-audit-validate.log | 7f8b08cad0283c2678757e87d3b04b4454fc2c80a30f981a95539a29a2deecb7 |
| R07 | `pwsh -NoProfile -File ./Tools/test-g4-tool-package-contracts.ps1 -Version 0.0.0-p1r3701.1 -RequireControls -RetainPackageDirectory` | 2026-09-05T13:17:22Z | 2026-09-05T13:18:00Z | 0 | PASS — "Packed G-4 tool contract qualification passed for version '0.0.0-p1r3701.1'"; `releaseEligible: true`; no stall (38s) | R07-g4.log | 8dba256f42a350f8f1c7fe3340ca58d77d9e1a064b9d1b21374f8f98472cb226 |
| R08 | `./test/Hexalith.Builds.Module.Tests/bin/Release/net10.0/Hexalith.Builds.Module.Tests -verbose -method '*LoadManifestNegativeControlReturnsStableRule*'` (targeted reciprocal-rejection evidence capture) | — | — | 0 | PASS — 15/15 (all module negative-control fixtures) | R08-reciprocal-evidence.log | 6da62064e609974186263b9525357579c21f2d58081005b615373deecbfe6342 |

Diagnostic-only attempts retained for the record (not part of the accepted gate, all against transient/superseded commit states while isolating the `-m:1` and audit-ancestor blockers): `01-restore.log`, `01b-restore-final.log`, `02-build.log`, `02b-build-final.log`, `03-module-tests.log` (pre-`dangerouslyDisableSandbox`), `03b-module-tests-direct.log` (direct-exe fallback, 114/114), `03c`–`03j-module-tests-*.log`, `04-evidence-tests-m1.log`, `04b`–`04d-evidence-tests-*.log`, `05-catalog-test.log`, `05b-catalog-test-final.log`, `06-audit-validate.log`, `06b-audit-validate-rerun.log`, `06c-audit-validate-final.log`, `07-audit-generate.log`, `07b-audit-generate-final.log` — all under the same log directory.

## G-4 packaged-tool inventory and artifacts

Inventory: `/tmp/claude-1000/-home-administrator-projects-hexalith-projects/db9a45af-df47-47c7-b1c2-39d9e83626f8/scratchpad/wt-builds-3701-env/tmp/hexalith-builds-g4-packages-a57f1c82d11347be908cf517c6ad8515/g4-tool-package-inventory.json`

| Artifact | SHA-256 |
| --- | --- |
| `Hexalith.Builds.Module.Cli.0.0.0-p1r3701.1.nupkg` | `2543a3da7c7246575dab659057ea239e47d13695a365840d84cd967c473c1c65` |
| `Hexalith.Builds.Module.Cli.0.0.0-p1r3701.1.snupkg` | `bfe0db8649bd4012e2658d64f38e482ba94719ab744005626a99996982240517` |
| `Hexalith.Builds.Evidence.Cli.0.0.0-p1r3701.1.nupkg` | `e544e22ae833fe76904a98352c413fe04cf4107cdef3ae4f4d75ec51948dbc1e` |
| `Hexalith.Builds.Evidence.Cli.0.0.0-p1r3701.1.snupkg` | `2a160210644475393224fbf4e6415360a39da7a6492cbfbb011cd299ca904385` |

Inventory declares: `sourceTree.clean: true`, `sourceTree.revision: "6ea4c27ade695964ee3c95b1d28e0058f4e1430e"`, `qualification.packageBuild.result: "passed"`, `qualification.sourceValidation.result: "passed"` (executed, not `-SkipSourceValidation`), `qualification.controls.result: "passed"`, `releaseEligible: true`, `ineligibilityReasons: []`.

## Static alignment and reciprocal-rejection evidence (item 9)

1. **All 13 catalog rows resolve `HexalithEventStoreVersion=3.70.1`.** `Props/Directory.Packages.props` line 8: `<HexalithEventStoreVersion Condition="'$(HexalithEventStoreVersion)' == ''">3.70.1</HexalithEventStoreVersion>`; all 13 `Hexalith.EventStore.*` `<PackageVersion>` rows key off `$(HexalithEventStoreVersion)`. Confirmed by `R05` (structural catalog test, PASS) and by `R07`'s packaged consumer restore, which resolved and packed against the same catalog.
2. **`SupportedPlatformPins.EventStoreVersion`, manifest schema, positive fixtures, serialized evidence, and coupled hashes all use `3.70.1`.**
   - `src/libraries/Hexalith.Builds.Tooling/Manifest/SupportedPlatformPins.cs:14`: `public const string EventStoreVersion = "3.70.1";`
   - `schemas/hexalith.module-manifest.v1.json:65`: `"eventStoreVersion": { "const": "3.70.1" }`
   - `test/fixtures/module/positive/hexalith.module-manifest.v1.json:25`: `"eventStoreVersion": "3.70.1"`
   - `test/fixtures/evidence/positive/evidence/release-passed.json`, `.../negative/evidence/{down-run,test-no-tests,secret-metadata}.json`: all `"eventStoreVersion":"3.70.1"`.
   - Coupled SHA-256 hashes recomputed from the actual edited bytes (`sha256sum`) and propagated into the referencing `.yaml` fixtures' `artifact_sha256` fields: `release-passed.json` → `2dc3d3f3af9525e420c4fffa7504708df6f82635490ebf0d8b64e1a83b390b94` (used by `readiness.yaml`, `outcome-mismatch.yaml`, `policy-controls.yaml` ×2, `coverage-shortfall.yaml`); `down-run.json` → `2993484cfa9e73928f42a5651c8baf2b7dcf5275093ebf2c4f8b5a6be3397c1d` (`binding-mismatch.yaml`); `secret-metadata.json` → `b5c9826b4ab1d2f337052cbdaa01dab341c472b0ad76bf3c2e5a0299761e1ecb` (`secret-metadata.yaml`); `test-no-tests.json` → `ea403cf7fdec0177a43316513dea6884675a8d41a3418c81d829ca030f2d3e76` (`no-executed-tests.yaml`). The `Hexalith.Builds.Evidence.Tests` run (R04b, 31/31 PASS) exercises all of these `.yaml`/`.expected.json` pairs, confirming the recomputed hashes are self-consistent. The G-4 inventory's `fixtures.files[]` array independently re-hashes every one of these files at rest and records identical values (see excerpt above), corroborating both the source-of-truth commit and the live packaged tool's own read of the tree.
3. **Tampered fixture (`3.102.0`) is REJECTED with exactly `HXM016`.** `test/fixtures/module/negative/tampered-platform-pin.json` now carries `"eventStoreVersion": "3.102.0"`. Its `.expected.json` (unchanged) asserts `ruleIds: ["HXM016"]`. `R08` shows `LoadManifestNegativeControlReturnsStableRule(fileName: "tampered-platform-pin.json")` PASSED. The live packaged `hexalith-module` CLI (via `R07`'s G-4 gate) independently confirms: `qualification-evidence/module-negative-tampered-platform-pin-output.json` = `{"status":"failed","outcome":{...,"ruleId":"HXM016"},"diagnostics":[{"ruleId":"HXM016","field":"platform.eventStoreVersion","message":"The manifest platform pins are not supported by this runner."}]}`. **This is the required reciprocal proof: the rollback worktree rejects the selected candidate `3.102.0`.**
4. **Superseded fixture (`3.88.0`) is still rejected with `HXM016`.** `test/fixtures/module/negative/superseded-platform-pin.json` unchanged at `"eventStoreVersion": "3.88.0"`. `R08` confirms `LoadManifestNegativeControlReturnsStableRule(fileName: "superseded-platform-pin.json")` PASSED. G-4 live output `module-negative-superseded-platform-pin-output.json` — byte-identical to the tampered case's output (`HXM016`, same message/field).
5. **`invalid-profile.json` still yields `HXM009`.** Unchanged fixture; `.expected.json` asserts `HXM009`. `R08` confirms PASS. G-4 live output: `module-negative-invalid-profile-output.json` = `{"status":"failed","outcome":{...,"ruleId":"HXM009"},"diagnostics":[{"ruleId":"HXM009","field":"profiles[0].classes","message":"The manifest contains an invalid qualification profile."}]}`.
6. **Evidence hash-mismatch control remains invalid.** `test/fixtures/evidence/negative/artifact-hash-mismatch.yaml` (unmodified, deliberately wrong `FFFF…` hash) still fails: `R04b` (31/31 includes this case) and G-4 live output `evidence-negative-artifact-hash-mismatch-output.json` = `{"status":"failed","outcome":{...,"ruleId":"HXE147"},...,"message":"The evidence artifact SHA-256 does not match its declared value."}`.

## Overall disposition

The rollback worktree at `6ea4c27ade695964ee3c95b1d28e0058f4e1430e` genuinely binds `Directory.Packages.props`, `SupportedPlatformPins`, the manifest schema, all touched positive/negative fixtures, and their coupled SHA-256 hashes to EventStore `3.70.1` in one Conventional-Commits-valid, commitlint-validated commit; restore, Release build (0 warnings/errors), both unit-test suites (Module 114/114, Evidence 31/31, via the sanctioned fallback command after the contract-exact `-m:1` invocation reproducibly failed with an isolated, non-code environment defect), the structural catalog test, and the full G-4 packaged-tool gate (including a live restore of the packed CLI tools and execution of every fixture/evidence control, `releaseEligible: true`) all PASS — and, critically, they independently and reproducibly prove the required reciprocal result: this rollback worktree rejects the selected candidate `3.102.0` with `HXM016` (via both the in-process test suite and the live packaged tool), while still correctly rejecting the historical `3.88.0` pin (`HXM016`), `invalid-profile.json` (`HXM009`), and the deliberate evidence hash-mismatch control (`HXE147`). The one genuine gap is `Tools/package-version-audit.json`/`validate-package-version-audit.ps1`: the audit-generation tool's ancestor-based self-consistency check makes it architecturally impossible to regenerate the audit inside the same single commit that changes the catalog (proven by direct experimentation, and consistent with the base repository's own two-commit precedent for the same kind of change); this rollback honors the "exactly one commit" boundary and instead records that check as an honest, root-caused FAIL rather than fabricating or hand-editing a passing audit. Net disposition: **rollback is independently executable and its runner/schema/fixture/evidence/reciprocal-rejection evidence qualifies**, gated by one explicit, non-code, tooling-sequencing blocker on the audit-provenance file that requires either a second authorized commit or an audit-tool change to close.
