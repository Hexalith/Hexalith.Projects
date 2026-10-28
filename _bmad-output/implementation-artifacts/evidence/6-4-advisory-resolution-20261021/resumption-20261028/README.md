# Story 6.4 current-source handoff — 2026-10-28

The approved advisory implementation and the last supplemental source corrections are already committed in the checkout. This pass changes evidence only. It preserves the specification baseline and the original frozen block, SHA-256 `78e8a29557f949b064db4111f8d487ff4c4d15f3f7d57284e72905f797b9a7df`. Story 6.4 remains in progress.

Current local owner revisions are Projects `0e45105476902fcfce1f8dbd8416d7234bb16b41`, Conversations `31a76998b8f8489971c6dbcd3b8fa28d4adeeda5`, Tenants `2079f25448625cf7a6a4bc1417ff9889e11d5781`, Builds `1745ea0f09df9e518aeea3eeb1a7df836f67e09d`, EventStore `eec6ed131a8959c85ae191dff75f88dd34a74832`, and Platform `a5e1389fe2c97b14e8c697a2e54f45631d07ef95`. The consumer catalog and `tools/release-packages.json` still select Conversations 1.3.0; the current EventStore selection is 3.128.3.

The accepted Conversations local tool is now CLI `4.38.1+16ff70a2321ad576faaac3fe53088c2f1bdacae3`. Its public `prepare-build` command succeeds in source mode. Using the exact emitted import environment, the current Conversations.Server.Tests Debug build passes with zero warnings/errors. The six focused advisory, strict, SDK-registration, tenant-access-registration, metadata-projection and recovery classes pass **243 unique tests**, zero failures/errors/skips/not-run. `commands.json` preserves the exact arguments, working directories and generated environment; compressed logs and xUnit XML retain the execution results. `source-audit.json` binds these observations to current source hashes.

From Conversations, the exact package-preparation request is:

```text
dotnet tool run hexalith-module -- prepare-build --manifest module/hexalith.module-manifest.v2.json --mode package --workspace /tmp/story64-current-20261028-frjjatw_/package --output json
```

It exits 2 with `HXW011`: local Platform commit `a5e1389fe2c97b14e8c697a2e54f45631d07ef95` has no local SemVer release tag; and `HXW009`: that Platform pins Builds `51a71cb1cfe860d5fc135eb3b8521fd80c413810`, while the umbrella index and Builds checkout select `1745ea0f09df9e518aeea3eeb1a7df836f67e09d`. Source mode permits these identity findings as warnings. This is a producer/Platform/catalog qualification boundary, not an advisory-source defect. Remote CI and official publication/destination state were not refreshed in this pass. Local tag absence does not establish official package or release absence.

From Projects, this source verification request exits 1 before compiling the test assembly:

```text
dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj -c Debug -m:1 -p:UseHexalithProjectReferences=true -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 -v:minimal
```

Conversations refuses the missing producer-selected mapping. The narrower Projects.Contracts.Tests build fails for the same reason. Repeating the server build with the actual Conversations preparation environment correctly exits 1 with `HXW006`: Projects.Server is outside the mapping's active module and mapped source roots.

The accepted CLI's `prepare-build --help` exposes only `--manifest`, `--workspace`, `--mode`, and `--output`. The current producer's `CreatePrepareBuildCommand` and `WorkspaceRootResolver` likewise provide no supported active-module or entry-project override. The resolver derives the active root from the existing manifest's location; the existing Conversations declaration selects Conversations. Projects has no root module/platform JSON declaration. No manifest, mapping or enrollment was invented to bypass that boundary.

Remaining work is qualified package preparation and a supported root verification route, successful current-source CI, separately approved Conversations publication, official package verification and verified consumer pins. No new source patch is warranted by these results. No full Projects suite, signed gateway/store lane, live AppHost or deployed acceptance is claimed. File mapping remains unsupported/incomplete. No dependency, tool pin, index, Git ref, remote or runtime was changed. Parent workflow auditing and planning checks remain separate.

Parent audit independently verified all 47 source hashes, the initial 14 evidence-file hashes, all 243 unique passing XML test cases and the zero-warning/error mapped build. The planning guard passed 37 tests, and both story/index authority validators passed. Current scoped evidence therefore records **280 passing tests**, zero failures/skips. Exact parent commands and results are in `parent-audit.json` and the four adjacent logs. The frozen matrix remains incompletely verified on the current tree because Projects source/gateway/store execution is blocked; no final-review or story-completion claim is made. The specification's current resumption checkpoint records this handoff.
