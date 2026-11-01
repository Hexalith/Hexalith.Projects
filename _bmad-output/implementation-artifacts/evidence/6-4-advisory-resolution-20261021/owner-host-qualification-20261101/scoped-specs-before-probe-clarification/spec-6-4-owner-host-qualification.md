---
title: 'Story 6.4: Qualify owner hosts against the current SDK'
type: 'bugfix'
created: '2026-11-01'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
story_key: '6-4-resolve-projects-with-transient-current-explanations'
baseline_commit: '353cfd33e54270a1acb028c1c66ecb7cd548f3e7'
approved_at_utc: '2026-11-01T00:12:16Z'
approval_decision: 'User selected 1: Extend owner qualification and continue'
context:
  - 'AGENTS.md'
---

<frozen-after-approval reason="User approved the concrete owner-host qualification continuation with option 1">

## Intent

Finish local source qualification of the existing Conversations/Folders owners against the current EventStore SDK. The advisory protocol repair and 2,223 Projects cases pass; 79 Conversations and 38 Folders cases expose missing host/catalog/transport enrollment. Correct those actual compositions and fixtures, then pass the full affected test projects.

## Boundaries & Constraints

Preserve every existing dirty change, historical evidence, query key/payload, actual projection identity, scoring, File-incomplete/no-selection semantics, authorization and persisted outcomes. Use existing SDK seams. Keep named-policy, workload/caller, catalog/generation/target and redirect guards active; no allowing fake, expectation weakening, skip, fabricated mapping marker or test-only manifest. Preserve generic API authentication semantics when supplying existing named caller policies.

No dependency-version changes, real Git index/ref/branch/stage/commit/push operations, publication, deployment, nested initialization or shared Parties edits/builds. The publication hold remains active. Build Debug/source in command-owned disposable clones. Keep canonical Story 6.4/sprint in-progress; source success accepts no release/deployment/consumer gates. Reuse prior command-owned scratch with provenance capture; do not delete unrelated files/caches. Roughly 15 GiB remains free.

## I/O & Edge-Case Matrix

| Scenario | Input/state | Required behavior |
| --- | --- | --- |
| Host | Valid existing production composition | SDK endpoint source/pipeline and explicit correct named policies; startup/callbacks succeed |
| Catalog | Valid generation, host, service surface and declared capability | Actual receiver dispatch/metadata/recovery succeeds |
| Refusal | Missing/wrong caller, policy, root, epoch, target or tenant | Existing fail-closed refusal; no unauthorized state change |
| Transport | Bound invocation or redirect | Configured redirect-disabled transport; redirects cannot transfer authority |
| Recovery | Restart, replay, failed/pending receipt | Existing persisted completion/quarantine/retry expectations hold |
| Source | Standalone Folders Debug graph | Supported consistent source dependencies; no duplicate Commons assembly or forged enrollment |

</frozen-after-approval>

## Code Map

Repository prefixes: Folders = `references/Hexalith.Folders/`; Conversations = `references/Hexalith.Conversations/`.

- Folders `src/Hexalith.Folders.Server/FoldersServerHostComposition.cs` and endpoint registrations: SDK projection mapping requires the missing HTTP endpoint-source/security enrollment. Fix production and the four affected host test classes together; test-only DI would conceal the defect.
- Conversations `tests/Hexalith.Conversations.Server.Tests/ResolutionMetadata/ResolutionEnrollmentFixture.cs` and `ResolutionProjectionGateway.cs`: 74 receiver503 cases lack current committed authority, exact identity, generation headers and service surface. Use real SDK-valid catalog/projection-capability bindings, including auxiliaries; keep the receiver filter.
- Conversations `src/Hexalith.Conversations.Server/Api/ConversationCommandApi.cs`, `Agents/ConversationDeletionRuntimeHostExtensions.cs`, `Agents/ConversationDeletionRuntime*.cs`, `tests/Hexalith.Conversations.Server.Tests/Agents/ConversationDeletionRuntimeHostTests.cs`: five cases expose unnamed participant-policy metadata and missing configured invocation transport. Preserve existing caller policy and generic API auth; inspect the inner transport beneath the SDK wrapper.
- Folders `src/Hexalith.Folders.Server/Hexalith.Folders.Server.csproj`, `Directory.Build.props` and Memories dependency configuration: find the supported owner/source graph correction for CS1704/MSB3243. Do not alter versions/generated mappings or present the previous no-project-rebuild fallback as the standalone pass.
- SDK source under `references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/{Authentication,Routing}` and `.DomainService`: existing security inventory, host/catalog options, dispatch guards and transport factories are authoritative. No new platform or per-module protocol.
- Prior evidence: `evidence/6-4-advisory-resolution-20261021/current-advisory-source-20261031/approved-repair-20261031T225439Z/README.md` and `remaining-corrections.json`, relative to this spec. Read them and the captured execution helpers; adapt into a NEW receipt, never rerun into old paths. Prior scratch `/tmp/story64-approved-repair-kvtivzn1/source` contains all12 direct clones and repaired source; confirm current files/revisions before reuse.

## Tasks & Acceptance

- [x] Folders host/endpoint source and `tests/Hexalith.Folders.Server.Tests/{ServerEndpointRegistrationTests,FoldersServerHostCompositionTests,FoldersCallbackBoundaryTests,FolderRepositoryStartupTests}.cs`: enroll actual SDK security and correct named policies; retain callback/repository/refusal assertions.
- [x] Conversations enrollment fixture/gateway and projection recovery/enrollment tests: provide valid committed catalog bindings and cover the matrix with actual SDK guards and persisted state.
- [x] Conversations deletion host/transport and its host tests: fix named-policy/transport composition, preserve caller semantics and original state expectations; verify actual inner redirect behavior.
- [x] Folders source configuration: pass the ordinary individual Debug/source test-project build using supported inputs; retain failed attempts and any separate fallback honestly.
- [x] Fresh owner mappings where supported, individual Debug builds and direct xUnitv3 full runs: Projects Contracts/Core/Server, Conversations Server, Folders Server. No solution-level tests. Preserve all passing advisory and negative-authority cases, proving zero writes and runtime cleanup.
- [x] New receipt under `evidence/6-4-advisory-resolution-20261021/owner-host-qualification-20261101/`: exact argv/cwd/safe environment/UTC/exits, source/generated-input/binary hashes, native XML, matrix-to-executed-case mapping, preservation and cleanup. Update both scoped specs; main story/sprint remain in-progress.

Given corrected valid compositions, when the five full affected test projects execute on final source, then all pass without skips/errors or relaxed guards. Given each matrix scenario, when its existing/targeted case executes, then the stated behavior and persisted outcome are proved. Given the original workspace and historical evidence, when validation ends, then original identities/indexes/refs and unrelated work/resources remain preserved.

## Implementation Notes

Parent Build continuation already rendered the workflow once. Implement this spec without restarting/rendering Build or delegating further. The parent performs acceptance audit and workflow review. Keep this agent available for review fixes; send concise source/build/runtime milestones. Current dirty work is authorized and must survive. Earlier metadata patches remain correct and must not be reverted.

## Verification

Use actual public `prepare-build --manifest <owner manifest> --mode source --workspace <fresh path> --output json` environments unchanged where supported. Build individual test projects Debug with `-m:1`; invoke their freshly built xUnitv3 assemblies with single-dash selectors/`-result-xml` and `-failSkips`. Retain each failed execution; use the documented focused fallback ladder without suppressing source/security/analyzer gates. Run both production-authority validators and owning whitespace checks. Verify manifests/source preservation and owned process/listener cleanup before reporting completion.

## Executed owner-host qualification outcome

All five affected Debug/source projects pass their full native xUnit v3 runs without failures, skips, errors or unexecuted cases: Projects Contracts 230/230, core 684/684, Server 1309/1309; Conversations Server 1522/1522; Folders Server 1287/1287. The total is 5032 passing cases. Final selected executable closures match both their before/after run bindings and the current source-built files.

Folders now composes the canonical SDK host services, endpoint source and security, explicit human/channel/workload policies and callback catalog guards. Shared HTTP security runs after route selection in all three API routing modes. Canonical `/alive` and `/ready` probes are available; legacy health paths retain explicit human policies. The extra signed-process fixture uses that actual host and retains its event, replay, credential refusal and legacy no-write assertions.

Conversations fixtures supply validated committed receiver catalog/capability/auxiliary bindings, exact host identity, generation headers and service surfaces while running the actual SDK guards. Eleven negative authority cases compare persisted byte/ETag snapshots. Deletion fixtures provide a separately owned invocation transport beneath the SDK wrapper; an actual 307 redirect and uncommitted recipient cannot transfer authority. The original one-parameter participant API CLR signature and generic default-policy semantics remain; its additive named-policy overload is explicit.

Fresh public producer mappings for Projects and Conversations were applied unchanged. Folders has no supported v2 manifest: its ordinary individual Debug/source build passes with the existing documented global `-p:UseHexalithProjectReferences=true -p:UseNuGetDeps=false` inputs and complete dependency rebuilding. The owner passes its selected source mode and independently located Platform root into Memories. Standalone selection has an empty mapping hash and one source Commons.UniqueIds assembly. Plain Debug without the global source inputs still mixes sibling package defaults; those failures and the abandoned item-default attempt are retained. No no-project-rebuild fallback is selected as acceptance.

Receipt: `evidence/6-4-advisory-resolution-20261021/owner-host-qualification-20261101/README.md`. It binds exact commands, generated inputs, source and binary hashes, native XML, executed matrix coverage, real-Dapr physical zero-write digests, preserved failures and owned runtime cleanup. Static receiver-fixture admission labels are SDK-valid fixture labels, not the owner's verbatim or qualified production admission schema; no digest-key availability or installation authority is inferred. Original dirty work, historical evidence, all real repository identities/indexes/refs/gitlinks and shared Parties bytes remain preserved. Canonical Story 6.4/sprint stay in-progress; publication/deployment/consumer gates and the Parties publication hold remain independent.
