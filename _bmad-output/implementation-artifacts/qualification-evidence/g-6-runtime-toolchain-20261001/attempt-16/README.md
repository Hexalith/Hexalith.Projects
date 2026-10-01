# Current G-6 runtime/toolchain qualification — attempt-16

Captured `2026-10-01T14:22:04Z`. Jérôme Piquot explicitly accepted attempt 16 at `2026-10-01T14:58:12Z` on reviewed pending SHA-256 `b7f940e512e17bcad98b021e4a7d4b9acce4f0c9fba4d3dc23f9d160245be139`. The exact [reviewed packet](reviewed-packet.json) is preserved. Selected [accepted packet](packet.json), normalized-LF file SHA-256 `49ae8176481f5ef130aa95ba296a9f3c7ca7d30f7ee6f457a0cf94378fa20f05`; [named decision](acceptance.json). Status is `accepted`, `technicalValidity=true`, `closure.committed=true`, and `usableAsPrerequisite=true`. Accepted-only validation passes in the clean checkout.

Actual results: 1/1 qualifier; 33/33 support cases across 21 selectors; 31/31 fixture controls; all ten AppHosts and McpCli; strict OQ8 capture validation; 103 current evidence controls plus accepted metadata-commit positive control; 12 runner controls; zero failures/skips. All 22 recorded commands exit `0`. Exact commands, outcomes and log hashes are retained in [command-record.json](command-record.json); [capture](capture/observations.json), [test results](capture/test-results.json), [support results](capture/deterministic-support.json), and [validation receipt](capture/capture-validation.json) bind the unchanged OQ8 matrix.

All 12 checkout revisions equal their committed root gitlinks. The status-aware candidate gate and exact Builds CI SHA check pass. The clean isolated checkout is `/home/administrator/projects/hexalith/g6-committed-closure-20261001`; unrelated G-4 work remains byte-preserved in the original Builds checkout. The named acceptance grants G-6 prerequisite usability for this exact committed-source scope; P1R and downstream usability remain separate. Local commits are authorized; no push, publication, deployment, downstream approval or Workflow activation occurred.

## Approved execution and limitations

Spec/tuple/run approval is `2026-10-01T08:29:35Z`. SDK `10.0.401`, Aspire SDK/CLI/hosting `13.6.0`, Toolkit `13.6.0-beta.910`, Dapr CLI/runtime/.NET `1.18.0`/`1.18.2`/`1.18.10`, Fluent UI `5.0.0`, NSubstitute `6.2.0`, and Fluxor `6.11.0` include the approved Toolkit prerelease and Dapr exceptions. The exact catalog Keycloak preview remains explicitly unqualified; older/different previews fail.

Local builds use Debug/source with isolated tools and cache. Platform's file-based Debug build uses its explicit NuGet directives, including EventStore `3.110.0`; it is package consumption. Checkout-source runtime proof does not qualify the published EventStore `3.110.0` archives or P1R rollback. Works/mTLS `1.18.3`, preview integrations, Dapr.Workflow, G-4/G-5, release and deployment remain outside current acceptance.

Pinned mDNS does not scope service lookups by `NAMESPACE`. The test fixture therefore selects SQLite v1 discovery against one fixture-private registry on all three owned sidecars and the stopped/restarted sidecar. SQLite discovery is Alpha and selected only for isolated test infrastructure; it is not a qualified production provider. Domain state remains PostgreSQL through Dapr; `sample`/`eventstore` app IDs, independent processes, replay/authority/expiry/leakage assertions and support selectors are unchanged. Test-only hosting startup, deterministic time, intent adapter and boundary counter remain disclosed.

[Cleanup](cleanup.json) binds unchanged shared container/binary snapshots, four exact owned container IDs removed, stopped owned processes, and removal of runner and fixture scratch. Every launch/restart records the same private registry identity hash and configuration hash. The registry identity hashes its absolute path, while the configuration hash binds resolver configuration bytes; raw paths and connection strings are not retained.

## Captured repository coordinates

The pre-execution [source manifest](source-state.json) binds 12,244 files, digest `16f588694881a0b33f0dc57095d828d46ab169d63103f5d9ebf45750128cd9fd`; pre/post snapshots match. Independent checks find no remaining owned process-group members and no owned containers.

| Repository | Checkout revision | Root gitlink | Dirty |
| --- | --- | --- | --- |
| Hexalith.Projects | `b26129c35604689a3a0a13e8af545bab7586e77e` | none | false |
| Hexalith.AI.Tools | `3f194e17174994d308ec84af9ee2b5aa68674d0d` | `3f194e17174994d308ec84af9ee2b5aa68674d0d` | false |
| Hexalith.EventStore | `8096455e4f23f2912998e36738058b8e3d961be6` | `8096455e4f23f2912998e36738058b8e3d961be6` | false |
| Hexalith.Tenants | `c28d0efacd77a42719f4ce3f71789961597ae6a7` | `c28d0efacd77a42719f4ce3f71789961597ae6a7` | false |
| Hexalith.FrontComposer | `a73edf89618faa664c872865d33f920db960602f` | `a73edf89618faa664c872865d33f920db960602f` | false |
| Hexalith.Conversations | `e857b89e2e7c67b086d5e2e340513b137f2135db` | `e857b89e2e7c67b086d5e2e340513b137f2135db` | false |
| Hexalith.Folders | `9bd3ad58d6855a70c0dae432e6e999fcb35d9cd2` | `9bd3ad58d6855a70c0dae432e6e999fcb35d9cd2` | false |
| Hexalith.Parties | `937cb2a343aaa74963db9bb867a2c3a01ff48677` | `937cb2a343aaa74963db9bb867a2c3a01ff48677` | false |
| Hexalith.Commons | `53f7961b517becde5b84ed4d20fe696b849b5cd9` | `53f7961b517becde5b84ed4d20fe696b849b5cd9` | false |
| Hexalith.Builds | `51af786cf156d2a3396dbd49f5e4898222e55c12` | `51af786cf156d2a3396dbd49f5e4898222e55c12` | false |
| Hexalith.Memories | `ece4edc4c9a37a62b34d3b7c8aa901fc363c038c` | `ece4edc4c9a37a62b34d3b7c8aa901fc363c038c` | false |
| Hexalith.Platform | `b38612856c6338e6e4768dbf2ba372137e6af854` | `b38612856c6338e6e4768dbf2ba372137e6af854` | false |
| Hexalith.McpCli | `29cf33a8927b12ef1232f25663d9daf5c1ad8369` | `29cf33a8927b12ef1232f25663d9daf5c1ad8369` | false |
Root captured source revision is `b26129c35604689a3a0a13e8af545bab7586e77e`. Builds checkout/root gitlink and CI execution are `51af786cf156d2a3396dbd49f5e4898222e55c12`. EventStore checkout/root gitlink are `8096455e4f23f2912998e36738058b8e3d961be6`; it is 28 commits and 44 changed `src` paths after published `v3.110.0` (40 added, 4 modified), separately from tag `27279fe6431925a6ea046c3f89af61487185c7de`. Checkout proof does not qualify published archives.

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

Superseded [attempt-14](../attempt-14/packet.json), SHA-256 `9caa2699bc6dca0c2366e0f9eaa088a9ffa9ce9ec4d27f448b31b8c53a87cfaa`, remains unchanged as technically passing dirty-source evidence. Superseded [attempt-15](../attempt-15/packet.json), SHA-256 `46159e22bbf0c2ebe4dd6a83dec5247b47c19244c5f443679b6d139524922e48`, retains the Docker scheduler port-publication failure before builds/tests. Its two exact owned containers were removed and shared snapshots match; the required full fixture cleanup proof is absent, so it remains rejected.

Capture-time limitations and the pre-acceptance verification remain preserved. Their pending-acceptance statements describe the reviewed capture state; the named decision above resolves that boundary without changing any technical evidence or runtime limitation. See [acceptance verification](acceptance-verification.json) for the accepted-only gate and metadata/source preservation checks.

The sprint index preserves `current_candidate_packet` as the exact pending review copy, with `current_candidate_usable=false`. The active `current_accepted_packet` selects `packet.json`, with `current_accepted_usable=true`; its named decision and accepted-only gate are the authority for current G-6 qualification. This distinction preserves the historical P1R fixture and its independent readiness boundary.
