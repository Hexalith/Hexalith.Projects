---
title: 'Story 6.4: Qualify owner hosts against the current SDK'
type: 'bugfix'
created: '2026-11-01'
status: 'done'
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

Folders now composes the canonical SDK host services, endpoint source and security, explicit human/channel/workload policies and callback catalog guards. Shared HTTP security runs after route selection in all three API routing modes. Canonical `/health`, `/alive` and `/ready` probes are available; `/health/live` and `/health/ready` retain explicit human policies. The extra signed-process fixture uses that actual host and retains its event, replay, credential refusal and legacy no-write assertions.

Conversations fixtures supply validated committed receiver catalog/capability/auxiliary bindings, exact host identity, generation headers and service surfaces while running the actual SDK guards. Eleven negative authority cases compare persisted byte/ETag snapshots. Deletion fixtures provide a separately owned invocation transport beneath the SDK wrapper; an actual 307 redirect and uncommitted recipient cannot transfer authority. The original one-parameter participant API CLR signature and generic default-policy semantics remain; its additive named-policy overload is explicit.

Fresh public producer mappings for Projects and Conversations were applied unchanged. Folders has no supported v2 manifest: its ordinary individual Debug/source build passes with the existing documented global `-p:UseHexalithProjectReferences=true -p:UseNuGetDeps=false` inputs and complete dependency rebuilding. The owner passes its selected source mode and independently located Platform root into Memories. Standalone selection has an empty mapping hash and one source Commons.UniqueIds assembly. Plain Debug without the global source inputs still mixes sibling package defaults; those failures and the abandoned item-default attempt are retained. No no-project-rebuild fallback is selected as acceptance.

Receipt: `evidence/6-4-advisory-resolution-20261021/owner-host-qualification-20261101/README.md`. It binds exact commands, generated inputs, source and binary hashes, native XML, executed matrix coverage, real-Dapr physical zero-write digests, preserved failures and owned runtime cleanup. Static receiver-fixture admission labels are SDK-valid fixture labels, not the owner's verbatim or qualified production admission schema; no digest-key availability or installation authority is inferred. Original dirty work, historical evidence, all real repository identities/indexes/refs/gitlinks and shared Parties bytes remain preserved. Canonical Story 6.4/sprint stay in-progress; publication/deployment/consumer gates and the Parties publication hold remain independent.


## Review Triage Log

The parent reviewed the complete three-layer result before classifying any finding. The edge-case layer returned no findings. Source and runtime adoption remain separate: the source receipt does not qualify an installed production catalog or an end-to-end conforming deployment.

| ID | Layer | Verdict | Evidence and route |
| --- | --- | --- | --- |
| B1 | Blind | high | `AddFoldersServerHost` calls both Folders and SDK defaults. Folders registers per-signal OTLP exporters while SDK registers `UseOtlpExporter`; the installed-package reproduction and both implementations establish a provider startup exception under the documented OTLP environment. Patch: consolidate actual-host defaults while retaining module signals/readiness. |
| B2 | Blind | medium | Both defaults configure standard HTTP resilience; the reproduced chain contains two handlers. SDK defaults also partition Dapr pipelines by target, so retain its single registration. Patch, grouped with B1 because duplicate defaults cause both outcomes. |
| B3 | Blind | high | Folders' `MapDefaultEndpoints` maps health only; the SDK lifecycle mapping is absent although catalog-dependent callbacks are installed. Patch: call the existing protected SDK lifecycle mapper and assert its inventory; this enrolls an existing SDK contract without introducing a module protocol. |
| B4 | Blind | high | The production entry point has no independent catalog observer; `RouteCatalogAuthority.ValidateAsync` refuses acquisition without one and independent loaded topology/authentication/owner policy inputs. This pre-existing conforming producer/adoption prerequisite is outside this local fixture/source acceptance intent and remains open with the main story. Defer; do not fabricate observations or claim production activation. |
| B5 | Blind | medium | The production observability artifact and gate still select `/health/live` and `/health/ready`, which the new actual-host inventory protects. Patch the Server probe declarations and operator guidance to canonical paths and cover readiness states. Group with G2 for the same stale-consumer defect. |
| B6 | Blind | medium | The existing gateway harness has manually composed owner endpoints and does not prove a complete guarded/installed owner deployment. That harness predates this source repair and its current receipt claims signed gateway/owner/store behavior, not installed catalog or consumer adoption. Defer the full guarded-owner acceptance lane to that existing adoption gate; keep the separate actual-host and SDK filter controls. |
| B7 | Blind | false | The direct-delegate projection fixture claims catalog-filter and persisted-state behavior only. Caller/policy coverage is separately bound to four executed real HTTP workload/channel/operation/caller cases in `FoldersCallbackBoundaryTests`; the matrix does not infer authenticated caller validation from the eleven injected-principal cases. Rejected. |
| B8 | Blind | medium | The newly added missing-signed-surface request asserts 403 after the earlier storage comparison, so that particular refusal lacks the stated before/after proof. Patch: snapshot persisted state and owner dispatch count around this request. |
| B9 | Blind | medium | The fixture's operation digests use one generic gateway envelope even for internal owner queries. `RouteOperationSchema` exports the supplied serializer types, so these digests cannot track the actual owner query/result shapes. Patch: bind the six existing request/result contract pairs without changing query payloads or runtime guards. |
| B10 | Blind | medium | The new mode tests verify route selection for anonymous probes and unauthenticated denial, but have no authenticated successful handler observation through the three modes and v2 rewrite. Patch: extend actual-host mode coverage with valid signed caller success using existing host/test seams. |
| B11 | Blind | low | The normal owner README lacks the supported global source-mode properties while the qualified standalone command requires them. This is a direct documentation correction for everyday source setup. Patch the normal development instructions. |
| G1 | Verification gap | medium | Accepted as pre-verified: actual-host tests never request the newly mapped `/ready`; slim-host legacy health tests cannot detect a wrong/empty new readiness predicate. Patch healthy/degraded/unhealthy production readiness HTTP cases, retaining 503 when the monitored Dapr dependency is down. |
| G2 | Verification other | medium | Verified the same stale Server probe paths in `observability.yaml` and the PowerShell report as B5. Patch, grouped with B5; preserve distinct worker probe contracts where those still use legacy defaults. |

Patch groups: SDK defaults (B1/B2), SDK lifecycle mapping (B3), Server probe consumers (B5/G2), missing-surface zero writes (B8), fixture schemas (B9), authenticated mode success (B10), source setup documentation (B11), production readiness regression coverage (G1). The two deferred production/adoption prerequisites do not accept release, deployment or consumer gates.

### Review follow-up discovery R1

Verdict: medium; route: defer. The exploratory OTLP HTTP/protobuf export control exposed the pre-existing SDK `RouteCatalogInvocationHandler` synchronous-send limitation: it overrides only `SendAsync`, whereas this exporter invokes synchronous `HttpClient.Send`. Retain the failed export execution and its exact diagnostic evidence. The bounded owner patch verifies configured provider startup and module subscriptions only, not successful export; do not replace or weaken an existing accepted export assertion, change SDK transport, or accept the producer delivery gate here.

## Review Fix Verification

All eight patch groups were implemented and independently inspected. The [focused successor](evidence/6-4-advisory-resolution-20261021/owner-host-qualification-review1-20261101/README.md) selects 216 passing targeted cases. Parent [post-review verification](evidence/6-4-advisory-resolution-20261021/owner-host-review-verification-20261101/README.md) then rebuilt six individual source projects and passed all five full suites (5,047 cases), plus 26 probe/operations contract cases: 5,073 distinct passing cases, zero failures/skips/errors/not-run.

The parent reverified 735 current executable-file hashes, 19 matrix groups and three real-Dapr refusal cases with fourteen unchanged physical key/value/ETag snapshots each. Both validators, owning whitespace, PowerShell syntax, source/Git/historical/shared-Parties preservation and owned-runtime cleanup pass. B4, B6 and follow-up R1 remain deferred; none accepts production catalog activation, OTLP delivery, publication/deployment or consumer adoption. Canonical story/sprint remain in-progress and the publication hold remains active.

## Workflow completion constraints

The scoped source workflow is complete. The approved boundaries override the generic sprint-promotion and local-commit steps: canonical Story 6.4/sprint remain in-progress, and no stage/commit/push operation is authorized or performed. The three deferred production/adoption/SDK prerequisites remain open.
