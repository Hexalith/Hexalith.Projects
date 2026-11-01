# Proposed continuation: current SDK owner-host qualification

The approved computed-query protocol repair is implemented. All 2,223 Projects tests, the real signed gateway/owner/store flow and the targeted Conversation/Folder owner cases pass. Full owner qualification still fails 117 cases: Conversations 79 and Folders 38. These checks were executed without skips or weakened guards. This proposal describes additional work; it is not applied or accepted.

## Single goal

Bring the existing owner hosts and their fixtures into the current EventStore SDK HTTP/catalog protocol so the full affected Server test projects pass. Preserve the repaired advisory behavior, existing query keys and payloads, actual persisted projection names, lifecycle/authorization semantics and zero-write reads. Keep Story 6.4/sprint in-progress and all release/deployment acceptance separate.

## Concrete changes to investigate and implement

1. **Folders host and its four failing host-test classes.** In `references/Hexalith.Folders/src/Hexalith.Folders.Server/FoldersServerHostComposition.cs`, reconcile existing service registration and pipeline enrollment with SDK `EventStoreHttpHostSecurityExtensions`. Register/enroll the HTTP endpoint source that current projection endpoints require, then validate every existing endpoint against its actual named caller/operation policy. Do not fix tests by injecting services absent from the production composition. Cover `ServerEndpointRegistrationTests`, `FoldersServerHostCompositionTests`, `FoldersCallbackBoundaryTests` and `FolderRepositoryStartupTests`; retain request binding, callback authorization and durable repository assertions. The current 38 failures stop on the missing endpoint-source service.

2. **Conversations native receiver fixtures.** Update `references/Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/ResolutionMetadata/ResolutionEnrollmentFixture.cs` and its existing projection gateway fixture to enroll an actual SDK-valid immutable catalog, exact host identity and generation/epoch invocation bindings. Use the SDK's existing projection capabilities/auxiliary operations and authenticated service surface. Keep the receiver filter active. The current 74 failures (SelectionRecovery 19, RebuildCompletion 33, NativeEnrollment 22) return 503 with `Reason=configuration-missing` before their intended assertions. Verify wrong/missing binding refusal as well as valid dispatch, restart/recovery and final persisted state.

3. **Conversations opted-in deletion host and five failing cases.** Examine `references/Hexalith.Conversations/src/Hexalith.Conversations.Server/Api/ConversationCommandApi.cs:51`, `Agents/ConversationDeletionRuntimeHostExtensions.cs`, `Agents/ConversationDeletionRuntime*.cs` and `tests/Hexalith.Conversations.Server.Tests/Agents/ConversationDeletionRuntimeHostTests.cs`. Preserve the existing caller policy and principal/tenant bindings while making protected endpoint policy metadata explicit. Preserve the generic API extensions' supported authentication semantics. Use the SDK-supported configured redirect-disabled invocation transport and catalog binding; inspect the inner transport under its SDK wrapper. Do not replace guards or change pump/quarantine expectations merely to match failed delivery.

4. **Folders standalone source graph.** Inspect its existing Memories standalone fallback and source/package collision (CS1704/MSB3243). Find the supported owner/source enrollment or configuration correction without invented manifests, forged mapping markers, dependency-version changes or altered generated imports. Preserve the already executed narrow `BuildProjectReferences=false` test-project fallback as separate evidence rather than a broad build pass.

## Verification and containment

Use disposable exact-source dependency clones while the Parties publication hold applies; make no shared Parties edits/builds. Use fresh owner-selected mappings where supported, individual Debug builds and direct xUnit v3 runs. No commits, branch/ref/index changes in real repositories, pushes, publication, deployment, nested initialization or dependency selection changes. Preserve existing work, source/binary identities, original failures and all historical evidence. Keep current SDK startup, named-policy, catalog, generation, caller and redirect safeguards active.

Given valid authority and current declared targets, the full owner test projects must pass on their actual corrected source. Given missing/incorrect caller, policy, generation, catalog or transport authority, the relevant guard must still refuse. Native projection and deletion tests must prove their actual persisted outcomes. Qualify the final combined Projects/owner source and record exact commands, XML totals, snapshots, hashes and cleanup in a new receipt; never promote this to published/deployed acceptance.

## Scope choice

- **Extend owner qualification and continue:** authorize this additional host/catalog/source-enrollment compatibility work and its full test verification.
- **Keep the query repair bounded:** accept the already proven query-repair scope, record these owner-wide failures as separate unresolved qualification work, and keep main Story 6.4 and its independent delivery gates open.
