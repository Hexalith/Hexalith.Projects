# Current G-6 qualification — retained failed candidate

Selected packet: [packet.json](packet.json), SHA-256 `724939520f2d8850073fd0091ffb7f0ada88e87231bb6ff457063bacb562a6bd`; captured `2026-10-01T09:18:33Z`. Status `pending`, `technicalValidity=false`, `usableAsPrerequisite=false`, committed closure `false`, acceptance `null`. This packet records actual failure and does not qualify the tuple. After capture, the validator/runner and mutation controls were corrected to allow metadata-only root commits while requiring committed bound source and exact submodule closure. The retained packet therefore also binds earlier implementation bytes and is stale against the final source; its packet and source bindings were not rewritten.

The approved spec/run at `2026-10-01T08:29:35Z` authorizes SDK `10.0.401`, Aspire SDK/CLI/hosting `13.6.0`, Toolkit `13.6.0-beta.910`, Dapr CLI/runtime/.NET `1.18.0`/`1.18.2`/`1.18.10`, Fluent UI `5.0.0`, NSubstitute `6.2.0`, and Fluxor `6.11.0`. Named acceptance on this exact reviewed hash is still absent. The baseline records the Toolkit prerelease and unlisted Dapr support-table exceptions. Historical G-6 and separate P1R acceptance remain scoped to their own evidence.

The latest run executed 1 qualifier (0 passed, 1 failed, 0 skipped), all 33 deterministic support cases across 21 exact selectors (33 passed, 0 failed, 0 skipped), and 21 fixture controls (21 passed, 0 failed, 0 skipped). The qualifier failed at `IdempotencyAdmissionOq8PostgresqlTests.cs:51`: one concurrent writer returned HTTP 500 rather than HTTP 202. Sanitized [diagnostics](fixture-diagnostics.json) show `DaprDomainServiceInvoker` invoking `sample/process` and receiving HTTP 404. Strict capture rejects the incomplete observation matrix. No domain behavior or matrix assertion was changed to produce a pass.

Five of ten AppHosts build: Conversations, EventStore, Folders, Builds Module, and Platform. Projects, Memories and Tenants fail the existing missing nested `references/Hexalith.PolymorphicSerializations` source guard. FrontComposer CounterFixture has missing nested EventStore source references; FrontComposer and Parties additionally have missing UI types. Nested submodules were not initialized and repository gates were not bypassed. McpCli's actual CLI transitive consumer builds successfully. Platform's file-based Debug build explicitly consumes NuGet packages; it disables inherited CPM for its versioned file directives and binds EventStore Aspire `3.110.0`. It supplies package-build evidence, while other local AppHost builds use Debug/source.

At capture, the candidate gate exited `1` with two reported checks: the validator rejects stale Memories resolved `Aspire.Hosting` package assets after its source guard stopped restoration, and the EventStore checkout HEAD differs from the root gitlink. CI Builds execution now exactly equals the root Builds gitlink. Against final post-capture source, the validator instead rejects the changed current mutation-test artifact hash (`test_runtime_toolchain_v2.py`), and the EventStore gitlink check still fails. See [post-capture verification](../post-capture-implementation-verification.md). Acceptance additionally requires all technical failures resolved, clean committed source and exact root gitlinks, then a separate named decision on the final reviewed packet hash.

## Bound source coordinates

| Repository | Actual checkout HEAD | Root gitlink | Dirty |
| --- | --- | --- | --- |
| Hexalith.Projects | `4d8dcf65803792f7def3b10ed21227329536154b` | superproject | true |
| Hexalith.AI.Tools | `3f194e17174994d308ec84af9ee2b5aa68674d0d` | `3f194e17174994d308ec84af9ee2b5aa68674d0d` | false |
| Hexalith.EventStore | `96a6041c5d63a933a58f1df09f7fad123c9ada41` | `6dededdecd62dd6dc6d1f15810108d860ec70c8f` | true |
| Hexalith.Tenants | `54ceb3e1d50d3fc7bf1846e08cd2835828adfcdf` | `54ceb3e1d50d3fc7bf1846e08cd2835828adfcdf` | true |
| Hexalith.FrontComposer | `48f7dfef920e8217e5c6221f364f0a1e0f61f387` | `48f7dfef920e8217e5c6221f364f0a1e0f61f387` | true |
| Hexalith.Conversations | `8519c24bfb0a04da8710c03174ce3576898e3e03` | `8519c24bfb0a04da8710c03174ce3576898e3e03` | true |
| Hexalith.Folders | `b9dd03ee56907ca17c8f8e29df6e6af0d6170dc0` | `b9dd03ee56907ca17c8f8e29df6e6af0d6170dc0` | true |
| Hexalith.Parties | `60b9836ea23151c5319dd06fd3deb80122f7abc3` | `60b9836ea23151c5319dd06fd3deb80122f7abc3` | true |
| Hexalith.Commons | `53f7961b517becde5b84ed4d20fe696b849b5cd9` | `53f7961b517becde5b84ed4d20fe696b849b5cd9` | false |
| Hexalith.Builds | `21ce044ab465ccb2adab58b3d66e394ffbecf3c2` | `21ce044ab465ccb2adab58b3d66e394ffbecf3c2` | true |
| Hexalith.Memories | `289773387e0c6b665b669031616c1e84cc079297` | `289773387e0c6b665b669031616c1e84cc079297` | true |
| Hexalith.Platform | `7c2f0f89f29c79f5d7ab4b731155e2cb07fc690f` | `7c2f0f89f29c79f5d7ab4b731155e2cb07fc690f` | true |
| Hexalith.McpCli | `29cf33a8927b12ef1232f25663d9daf5c1ad8369` | `29cf33a8927b12ef1232f25663d9daf5c1ad8369` | false |

The source manifest digest is `94e04b528559ee1786112109817d1a028a2a831a5d9e232c74aaaf0fb2e063d4`. `source-state.json`, consumer audit, resolved packages, commands, logs, cleanup, diagnostics and validator/policy/test/runner bytes are separately hashed in the packet. Actual checkout HEAD and root gitlink are distinct coordinates; no Git mutation was performed to close their difference.

## Commands and outcomes

Reproduction uses a new directory because failed attempts are preserved:

```bash
python3 tools/qualification/run_g6_qualification.py --baseline references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-10-01.json --output _bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain-20261001/<new-attempt>
```

Exact command arguments and outcome summaries are retained in [command-record.json](command-record.json).

| Command purpose | Exit | Evidence |
| --- | --- | --- |
| install isolated tools | `0` | [sanitized log](logs/install-isolated-tools.log) |
| observe exact tool versions | `0` | [sanitized log](logs/observe-exact-tool-versions.log) |
| fresh EventStore qualifier build | `0` | [sanitized log](logs/fresh-eventstore-qualifier-build.log) |
| fresh EventStore support build | `0` | [sanitized log](logs/fresh-eventstore-support-build.log) |
| fixture override tests | `0` | [sanitized log](logs/fixture-override-tests.log) |
| real PostgreSQL two-sidecar stop/restart qualifier | `1` | [sanitized log](logs/real-postgresql-two-sidecar-stop-restart-qualifier.log) |
| 21 exact deterministic support selectors | `0` | [sanitized log](logs/21-exact-deterministic-support-selectors.log) |
| strict EventStore capture validation | `1` | [sanitized log](logs/strict-eventstore-capture-validation.log) |
| AppHost build Hexalith.Projects.AppHost | `1` | [sanitized log](logs/apphost-build-hexalith-projects-apphost.log) |
| AppHost build Hexalith.Conversations.AppHost | `0` | [sanitized log](logs/apphost-build-hexalith-conversations-apphost.log) |
| AppHost build Hexalith.EventStore.AppHost | `0` | [sanitized log](logs/apphost-build-hexalith-eventstore-apphost.log) |
| AppHost build Hexalith.Folders.AppHost | `0` | [sanitized log](logs/apphost-build-hexalith-folders-apphost.log) |
| AppHost build Hexalith.FrontComposer.AppHost | `1` | [sanitized log](logs/apphost-build-hexalith-frontcomposer-apphost.log) |
| AppHost build Hexalith.Memories.AppHost | `1` | [sanitized log](logs/apphost-build-hexalith-memories-apphost.log) |
| AppHost build Hexalith.Parties.AppHost | `1` | [sanitized log](logs/apphost-build-hexalith-parties-apphost.log) |
| AppHost build Hexalith.Tenants.AppHost | `1` | [sanitized log](logs/apphost-build-hexalith-tenants-apphost.log) |
| AppHost build Hexalith.Builds.Module.AppHost | `0` | [sanitized log](logs/apphost-build-hexalith-builds-module-apphost.log) |
| AppHost build Hexalith.Platform | `0` | [sanitized log](logs/apphost-build-hexalith-platform.log) |
| McpCli transitive Dapr consumer | `0` | [sanitized log](logs/mcpcli-transitive-dapr-consumer.log) |
| G-6 mutation controls | `0` | [sanitized log](logs/g-6-mutation-controls.log) |
| root workflow pin contract | `0` | [sanitized log](logs/root-workflow-pin-contract.log) |

The mutation log retains 35 current controls, 30 scenarios for each of three historical baselines, 48 baseline-drift controls, 165 authority controls and two historical SHA-256 pins. Root gate/gitlink fixture tests pass 19 controls. Packet-reference, workflow and scheduling checks verify coordinated references without accepting qualification.

## Isolation and cleanup

Tools, Dapr runtime, NuGet cache and .NET home were isolated under disposable scratch. PostgreSQL, Redis, placement and scheduler used disposable exact-owned IDs and loopback publications. The unique namespace `g6-oq8-5fa2537cf8a4444d832fb045b3f2d077` was supplied through `NAMESPACE` to all three owned daprd processes; [cleanup.json](cleanup.json) records their node/app IDs and process identities. The pinned Dapr [v1.18.2 runtime](https://github.com/dapr/dapr/blob/v1.18.2/pkg/runtime/runtime.go) binds its namespace into name resolution; the official [isolation documentation](https://docs.dapr.io/concepts/isolation-concept/) describes `NAMESPACE` for self-hosted execution. Application IDs and matrix semantics were preserved.

Cleanup proves identical before/after shared container IDs, images, running states, start timestamps, restart counts and shared Dapr binary hashes. All four owned container IDs were removed, owned process groups stopped, and scratch removed. Shared Dapr/Redis/sidecars were not stopped or reinitialized. No retained domain data was mutated.

## Failed-attempt inventory and limitations

| Attempt | Qualifier exit | Retained packet SHA-256 |
| --- | --- | --- |
| 1 | `1` | `43713e531691dacd8a40c7757f9e8428075d628cc7cb191eb67bf204129aec7d` |
| 2 | `1` | `ee9e8a1d24871f513895d28ed7673e350214e86bc1d94fe35ea7143c64d452f7` |
| 3 | `1` | `724939520f2d8850073fd0091ffb7f0ada88e87231bb6ff457063bacb562a6bd` |

Attempt 1 remains in the parent directory; attempt 2 is in `../attempt-2/`. Both qualifier failures and actual 33-case support passes remain preserved. The first producer wrote missing-capture placeholders after strict validation failed, so its packet totals of zero are inaccurate; [the actual-outcome supplement](../attempt-1-actual-outcomes-supplement.json) binds the original packet and command logs and records 1 failed qualifier and 33 passed support cases. The original failed packet was preserved. Attempt 2 retains correct counts and diagnostics; attempt 3 adds complete self-hosted namespace isolation and still fails. Earlier packets bind earlier implementation bytes and are retained failed evidence, not current candidates.

Checkout-source proof does not qualify published EventStore `3.110.0` archives, `RetainedFloor` metadata downgrade to `3.70.1`, actor replay, mixed-version compatibility or operational rollback. Platform Works/mTLS Dapr `1.18.3`, catalog Keycloak/Kubernetes preview integrations and Folders' conditional stable Toolkit `13.0.0` public integration remain explicit unqualified exclusions. Dapr.Workflow remains unselected.

Rollback is to restore prior repository pins and rerun restore/build/qualification; no retained PostgreSQL data is rewritten. That rollback path is a documented action, not an exercised operational drill. P1R/P0 Stage 1/DW-35/DW-68 retain existing accepted/done states; P0 stages 2–7, P2–P4, readiness `NOT_READY`, blocked Story 6.1 and dependent work retain existing states.
