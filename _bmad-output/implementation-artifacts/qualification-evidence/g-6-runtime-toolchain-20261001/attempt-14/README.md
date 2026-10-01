# Current G-6 runtime/toolchain qualification — attempt-14

Captured `2026-10-01T13:23:13Z`. Selected [packet](packet.json), normalized-LF SHA-256 `9caa2699bc6dca0c2366e0f9eaa088a9ffa9ce9ec4d27f448b31b8c53a87cfaa`. Candidate validation passes. Status is `pending`, `technicalValidity=true`, `closure.committed=false`, `acceptance=null`, and `usableAsPrerequisite=false`.

Actual results: 1/1 qualifier; 33/33 support cases across 21 selectors; 31/31 fixture controls; all ten AppHosts and McpCli; strict OQ8 capture validation; 103 current evidence controls plus accepted metadata-commit positive control; 12 runner controls; zero failures/skips. All 22 recorded commands exit `0`. Exact commands, outcomes and log hashes are retained in [command-record.json](command-record.json); [capture](capture/observations.json), [test results](capture/test-results.json), [support results](capture/deterministic-support.json), and [validation receipt](capture/capture-validation.json) bind the unchanged OQ8 matrix.

Parties checkout `8c49383e94d16162ef30d102f4c3e536cda9790f` differs from root gitlink `60b9836ea23151c5319dd06fd3deb80122f7abc3`. The status-aware root candidate gate exits `1` on that exact-gitlink check. Dirty implementation source and absent named packet acceptance independently keep prerequisite usability false. No staging, commit, push, dependency initialization, downstream approval or Workflow activation occurred.

## Approved execution and limitations

Spec/tuple/run approval is `2026-10-01T08:29:35Z`. SDK `10.0.401`, Aspire SDK/CLI/hosting `13.6.0`, Toolkit `13.6.0-beta.910`, Dapr CLI/runtime/.NET `1.18.0`/`1.18.2`/`1.18.10`, Fluent UI `5.0.0`, NSubstitute `6.2.0`, and Fluxor `6.11.0` include the approved Toolkit prerelease and Dapr exceptions. The exact catalog Keycloak preview remains explicitly unqualified; older/different previews fail.

Local builds use Debug/source with isolated tools and cache. Platform's file-based Debug build uses its explicit NuGet directives, including EventStore `3.110.0`; it is package consumption. Checkout-source runtime proof does not qualify the published EventStore `3.110.0` archives or P1R rollback. Works/mTLS `1.18.3`, preview integrations, Dapr.Workflow, G-4/G-5, release and deployment remain outside current acceptance.

Pinned mDNS does not scope service lookups by `NAMESPACE`. The test fixture therefore selects SQLite v1 discovery against one fixture-private registry on all three owned sidecars and the stopped/restarted sidecar. SQLite discovery is Alpha and selected only for isolated test infrastructure; it is not a qualified production provider. Domain state remains PostgreSQL through Dapr; `sample`/`eventstore` app IDs, independent processes, replay/authority/expiry/leakage assertions and support selectors are unchanged. Test-only hosting startup, deterministic time, intent adapter and boundary counter remain disclosed.

[Cleanup](cleanup.json) binds unchanged shared container/binary snapshots, four exact owned container IDs removed, stopped owned processes, and removal of runner and fixture scratch. Every launch/restart records the same private registry identity hash and configuration hash. The registry identity hashes its absolute path, while the configuration hash binds resolver configuration bytes; raw paths and connection strings are not retained.

## Captured repository coordinates

The pre-execution [source manifest](source-state.json) binds 12,246 files, digest `656f5a357bbb4ddb370c4d6d7777ddb9152ad43b1cb6138bcabc52a64a374401`; pre/post snapshots match. Independent checks find no remaining owned process-group members and no owned containers.

| Repository | Checkout revision | Root gitlink | Dirty |
| --- | --- | --- | --- |
| Hexalith.Projects | `bb364eb86e2008068617d4f95fb32c2661a8a08f` | none | true |
| Hexalith.AI.Tools | `3f194e17174994d308ec84af9ee2b5aa68674d0d` | `3f194e17174994d308ec84af9ee2b5aa68674d0d` | false |
| Hexalith.EventStore | `08b9cfbce38fd34b9d791c63c848a8e685d72f36` | `08b9cfbce38fd34b9d791c63c848a8e685d72f36` | true |
| Hexalith.Tenants | `54ceb3e1d50d3fc7bf1846e08cd2835828adfcdf` | `54ceb3e1d50d3fc7bf1846e08cd2835828adfcdf` | true |
| Hexalith.FrontComposer | `48f7dfef920e8217e5c6221f364f0a1e0f61f387` | `48f7dfef920e8217e5c6221f364f0a1e0f61f387` | true |
| Hexalith.Conversations | `8519c24bfb0a04da8710c03174ce3576898e3e03` | `8519c24bfb0a04da8710c03174ce3576898e3e03` | true |
| Hexalith.Folders | `b9dd03ee56907ca17c8f8e29df6e6af0d6170dc0` | `b9dd03ee56907ca17c8f8e29df6e6af0d6170dc0` | true |
| Hexalith.Parties | `8c49383e94d16162ef30d102f4c3e536cda9790f` | `60b9836ea23151c5319dd06fd3deb80122f7abc3` | true |
| Hexalith.Commons | `53f7961b517becde5b84ed4d20fe696b849b5cd9` | `53f7961b517becde5b84ed4d20fe696b849b5cd9` | false |
| Hexalith.Builds | `21ce044ab465ccb2adab58b3d66e394ffbecf3c2` | `21ce044ab465ccb2adab58b3d66e394ffbecf3c2` | true |
| Hexalith.Memories | `289773387e0c6b665b669031616c1e84cc079297` | `289773387e0c6b665b669031616c1e84cc079297` | true |
| Hexalith.Platform | `7c2f0f89f29c79f5d7ab4b731155e2cb07fc690f` | `7c2f0f89f29c79f5d7ab4b731155e2cb07fc690f` | true |
| Hexalith.McpCli | `29cf33a8927b12ef1232f25663d9daf5c1ad8369` | `29cf33a8927b12ef1232f25663d9daf5c1ad8369` | false |

Root captured source revision is `bb364eb86e2008068617d4f95fb32c2661a8a08f`. Builds checkout/root gitlink and CI execution are `21ce044ab465ccb2adab58b3d66e394ffbecf3c2`. EventStore checkout/root gitlink are `08b9cfbce38fd34b9d791c63c848a8e685d72f36`; the committed source is 22 commits and 44 changed `src` paths after `v3.110.0` (40 added, 4 modified), separately from published tag `27279fe6431925a6ea046c3f89af61487185c7de`.

## Recorded commands

| Purpose | Exit | Log |
| --- | --- | --- |
| install isolated tools | `0` | [install-isolated-tools.log](logs/install-isolated-tools.log) |
| observe exact tool versions | `0` | [observe-exact-tool-versions.log](logs/observe-exact-tool-versions.log) |
| fresh EventStore qualifier build | `0` | [fresh-eventstore-qualifier-build.log](logs/fresh-eventstore-qualifier-build.log) |
| fresh EventStore support build | `0` | [fresh-eventstore-support-build.log](logs/fresh-eventstore-support-build.log) |
| fixture override tests | `0` | [fixture-override-tests.log](logs/fixture-override-tests.log) |
| real PostgreSQL two-sidecar stop/restart qualifier | `0` | [real-postgresql-two-sidecar-stop-restart-qualifier.log](logs/real-postgresql-two-sidecar-stop-restart-qualifier.log) |
| 21 exact deterministic support selectors | `0` | [21-exact-deterministic-support-selectors.log](logs/21-exact-deterministic-support-selectors.log) |
| strict EventStore capture validation | `0` | [strict-eventstore-capture-validation.log](logs/strict-eventstore-capture-validation.log) |
| AppHost build Hexalith.Projects.AppHost | `0` | [apphost-build-hexalith-projects-apphost.log](logs/apphost-build-hexalith-projects-apphost.log) |
| AppHost build Hexalith.Conversations.AppHost | `0` | [apphost-build-hexalith-conversations-apphost.log](logs/apphost-build-hexalith-conversations-apphost.log) |
| AppHost build Hexalith.EventStore.AppHost | `0` | [apphost-build-hexalith-eventstore-apphost.log](logs/apphost-build-hexalith-eventstore-apphost.log) |
| AppHost build Hexalith.Folders.AppHost | `0` | [apphost-build-hexalith-folders-apphost.log](logs/apphost-build-hexalith-folders-apphost.log) |
| AppHost build Hexalith.FrontComposer.AppHost | `0` | [apphost-build-hexalith-frontcomposer-apphost.log](logs/apphost-build-hexalith-frontcomposer-apphost.log) |
| AppHost build Hexalith.Memories.AppHost | `0` | [apphost-build-hexalith-memories-apphost.log](logs/apphost-build-hexalith-memories-apphost.log) |
| AppHost build Hexalith.Parties.AppHost | `0` | [apphost-build-hexalith-parties-apphost.log](logs/apphost-build-hexalith-parties-apphost.log) |
| AppHost build Hexalith.Tenants.AppHost | `0` | [apphost-build-hexalith-tenants-apphost.log](logs/apphost-build-hexalith-tenants-apphost.log) |
| AppHost build Hexalith.Builds.Module.AppHost | `0` | [apphost-build-hexalith-builds-module-apphost.log](logs/apphost-build-hexalith-builds-module-apphost.log) |
| AppHost build Hexalith.Platform | `0` | [apphost-build-hexalith-platform.log](logs/apphost-build-hexalith-platform.log) |
| McpCli transitive Dapr consumer | `0` | [mcpcli-transitive-dapr-consumer.log](logs/mcpcli-transitive-dapr-consumer.log) |
| isolated container cleanup controls | `0` | [isolated-container-cleanup-controls.log](logs/isolated-container-cleanup-controls.log) |
| G-6 mutation controls | `0` | [g-6-mutation-controls.log](logs/g-6-mutation-controls.log) |
| root workflow pin contract | `0` | [root-workflow-pin-contract.log](logs/root-workflow-pin-contract.log) |

## Preserved attempts

- Superseded [attempt-3](../attempt-3/packet.json), SHA-256 `724939520f2d8850073fd0091ffb7f0ada88e87231bb6ff457063bacb562a6bd`: failed qualifier and five AppHosts; later source drift. Packet bytes and recorded outcomes remain preserved.
- Superseded [attempt-4](../attempt-4/packet.json), SHA-256 `162649ffec5aa8ee9a795b5f306d33376b8bfa2e87abc6b6f010a0ca7845b8e1`: failed runtime reproduction. Packet bytes and recorded outcomes remain preserved.
- Superseded [attempt-5](../attempt-5/packet.json), SHA-256 `a4aec7b8c4a676ede15335225ada47f701af9b188888691d80566141a38c9f01`: runtime/build commands passed; strict package validation rejected older Keycloak preview. Packet bytes and recorded outcomes remain preserved.
- Superseded [attempt-6](../attempt-6/packet.json), SHA-256 `0eb0b46e8ed2ec8fdcaf01317cb9f6122328185942c78fc76656171aabc4d853`: runtime/build commands passed; strict package validation rejected older Redis; later test-source layout changed. Packet bytes and recorded outcomes remain preserved.
- Superseded [attempt-7](../attempt-7/packet.json), SHA-256 `edd626a36f50a53114f3abdfe0c6b5cf725a75dad9b46801f679882342f717fa`: technical qualification passed; superseded by reviewed source/validator/cleanup corrections and a fresh capture. Packet bytes and recorded outcomes remain preserved.
- Superseded [attempt-8](../attempt-8/packet.json), SHA-256 `dfb2ed58424678f564f1b2debceb385ee2620645ac5d7aaf2af9e0543ef69931`: Docker loopback scheduler publication failed before builds/tests; both owned exact IDs removed and shared snapshots unchanged. Packet bytes and recorded outcomes remain preserved.
- Superseded [attempt-9](../attempt-9/packet.json), SHA-256 `b42104afedd6e92bb8cf146f73d2cf2b48c225abbb2a1b7c9cea72363e92a48d`: qualifier/support/fixture and all consumer builds passed; strict validation rejected premature support projection and required dependency audit rejected an irrelevant group condition. Packet bytes and recorded outcomes remain preserved.
- Superseded [attempt-10](../attempt-10/packet.json), SHA-256 `865fa27083b6ef16907b2735116899a68b3a898c82911a93f8b0263fb1771adf`: all 22 commands passed; cleanup receipt conservatively failed before verifying post-kill owned-group disappearance; packet remains rejected. Packet bytes and recorded outcomes remain preserved.
- Superseded [attempt-11](../attempt-11/packet.json), SHA-256 `bbc418e944815e7d2bc0108d326367405884e076d5e6084742534da405a2124a`: GitHub timed out downloading approved Dapr placement binary; no containers started, shared snapshots unchanged and scratch removed. Packet bytes and recorded outcomes remain preserved.
- Superseded [attempt-12](../attempt-12/packet.json), SHA-256 `d257e7133abe74f4599c0524e8437a1f29b56cd42390bb4f5b47eb286d6b8e1a`: GitHub release download returned HTTP 504; no containers started, shared snapshots unchanged and scratch removed. Packet bytes and recorded outcomes remain preserved.
- Superseded [attempt-13](../attempt-13/packet.json), SHA-256 `ff49ce42b8eb8ecd238bce6be10b6764325b1f341339e0d981c09199f34147d1`: all 22 commands and owned cleanup passed; separate Aspire process outside qualification groups restarted shared resources, so snapshot comparison correctly rejected the packet. Packet bytes and recorded outcomes remain preserved.

Attempts 1 and 2 and their actual-count supplement remain historical failures. The diagnostic-http-client directories contain diagnostic attempts and cleanup dispositions; they are not selected qualification packets. Historical v1 qualification hashes and separate P1R acceptance remain unchanged.

[Final independent verification](verification.md) records root controls, standalone Builds CI, immutable prior hashes and the remaining acceptance boundary.
