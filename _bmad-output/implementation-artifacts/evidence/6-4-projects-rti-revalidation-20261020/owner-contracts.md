# Projects RTI prerequisite: current source revalidation

The RTI prerequisite remains **blocked-owner-source-contracts**. The owner checkouts advanced, and EventStore's D1 transport repair is now committed with focused SDK regression passes. Certified complete Projects/owner coverage, initialized-empty proof, the narrow Conversation read, native Folder/File evidence and File-reference mapping are still absent. No RTI implementation, prerequisite registration or parent Story 6.4 handler was added.

This report supplements the [resumed spec](../../spec-6-4-prerequisite-projects-reference-trust-index.md), [selected D1–D6 decisions](../../6-4-prerequisite-decisions-2026-10-20.md) and [contract annex](../../6-4-resolution-evidence-contracts.md). It supersedes the [previous preflight](../6-4-projects-rti-preflight-20261020/owner-contracts.md) only for current source observations and verification results. Historical evidence stays intact. The [source receipt](source-audit.json) records exact commands, exit codes, source hashes, negative probes and preservation checks.

## Inspected source tuple

All six checkouts were clean on `main` when this resumed inspection began. Owner HEADs match the root gitlinks; no checkout, update, dependency/pin change or commit was performed.

| Repository | Full current HEAD |
| --- | --- |
| Projects | `02ce53a039d2fd3c87b7c7232662eaa89ec2ddd7` |
| EventStore | `6c56c27bc1a8d63c83f9631f7162cf79fdde0a1e` |
| Conversations | `0c7bfac19cafbf4cbb2f67340b06e569280ef05a` |
| Folders | `9bacd7f62848c23222dc7309635eb65dc6e5ed9c` |
| Builds | `5d03f1db9c38db34ecac7fe6048937478ffb82cc` |
| Platform | `bb81fc9e98533246cfeed43ffcce08866a2a57f6` |

By receipt time, concurrent edits appeared in the Projects AppHost SDK pin and runtime qualifier, with untracked Builds qualification work. They are preserved and separately recorded in the receipt. They are not this RTI run, and no exact-current qualification is inferred from them.

The Builds catalog still defaults EventStore to `3.126.0`, Conversations to `1.2.0` and Folders to `1.3.0` (`references/Hexalith.Builds/Props/Directory.Packages.props:7–11`). These are observed defaults, not an accepted immutable execution tuple or proof that packages contain the inspected source.

## Native coverage and initialized-empty proof

Paths in the following EventStore observations are relative to `references/Hexalith.EventStore/`.

- `src/Hexalith.EventStore.Server/Streams/DaprSourcePublicationNamespaceSource.cs:24–57` reads an installed roster, independently authorizes it, obtains actual aggregate metadata twice, and rechecks roster/authority before issuing a complete native head cut. Registered but uncreated sources may carry a native zero head (`:37–38`). `Client/Streams/SourceNamespaceSnapshotReader.cs:53–55` skips their source-byte read while retaining the certified roster. This mechanism can distinguish real empty coverage only after installation authority exists.
- `Server/Streams/ISourcePublicationNamespaceAuthority.cs:5–10` explicitly requires finite legacy coverage, complete inventory and all-writer enforcement. Neither it nor `ISourcePublicationOperationAuthority` has a concrete production implementation/enrollment in inspected `src` or `samples`; references are contracts/consumers. The publication namespace/index actors are not installed by production actor registration (`Server/Configuration/ServiceCollectionExtensions.cs:258–279`). A static manifest or synthetic empty fixture cannot supply this authority.
- The separately registered shared source-roster actor does not close the gap. `Server/Projections/SharedProjectionSourceRosterActor.cs:33–43` defines `IInstallationCoverageAuthority` without a production implementation; installation rejects its absence (`:169`). `SharedProjectionSourceWriteFence.cs:9–16` defaults enabled domains to empty. `SharedProjectionSourceBeginProducer.cs:127–128,189–203` requires enforcement and captures native heads in a producer lifecycle. It is not an enrolled resolution source or a zero-write query operation.
- Projects `src/Hexalith.Projects.Server/Projections/ProjectReads/ProjectReadIndex.cs:8–17` stores observed IDs and maximum delivered position. `ProjectReadProjectionHandler.cs:47–60` rejects empty replay and checks an individual aggregate's rebuild head; `:64–87` merges observed IDs. No independent complete-Tenant installation certificate, source-head vector, source/RTI consumer or name-equality index exists. Empty IDs, missing keys and a per-aggregate replay head cannot certify an empty Tenant.

Required before implementation: named owner-approved source scopes and field mappings, installed complete legacy inventory or certified virgin installation, all-writer admission, native current-head/change/invalidation evidence, an actual enrolled consumer, and supported-scale coverage. These are source delivery dependencies; D1–D6 need no new design vote.

## Current owner contracts

Paths in each row are relative to its owner root under `references/`.

| Owner | Executable today | Remaining RTI dependency |
| --- | --- | --- |
| Conversations | Membership changes and labels are materialized (`src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs:888–905,978–985`); summary/freshness contracts exist. | No `GetConversationResolutionMetadata`, isolated metadata key/read, native resource/permission result, or owner-enforced scoring-label classification/purpose. `src/Hexalith.Conversations.Server/Projections/IConversationProjectionReadStore.cs:27` / `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs:61` read combined summary/detail. `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelKeys.cs:35` has no isolated metadata key. Tenant access (`src/Hexalith.Conversations.Server/TenantAccess/IConversationTenantAccessService.cs:28`) cannot substitute for this resource/purpose contract. |
| Conversations coverage | `src/Hexalith.Conversations.Server/Agents/EventStoreConversationTenantCatalogue.cs:30–40` folds lifecycle and binds a scoped cut. | Catalogue fields omit Project membership/label. `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs:146–159` explicitly cannot distinguish erased and never-initialized indexes. `src/Hexalith.Conversations.Server/Agents/ConversationAgentServiceCollectionExtensions.cs:36–57,69–98` requires external providers and composes only `approved-deletion-v1`; it does not install resolution inventory or namespace authority. `tests/Hexalith.Conversations.Server.Tests/Agents/EventStoreConversationTenantCatalogueTests.cs:16` explicitly retains synthetic namespace authority. |
| Folders | `src/Hexalith.Folders.Server/Queries/FolderOwnerReadQueryHandler.cs:19–39` verifies delegated caller, exact target, empty payload and strict `read_metadata`. Known-resource ACL projections compare native Folder/Organization heads (`src/Hexalith.Folders/Authorization/EventStoreFolderEffectivePermissionsReadModel.cs:85–133`). | `src/Hexalith.Folders.Contracts/FolderOwnerReadResult.cs:5` is still outcome-only. Native authority/resource vectors are not exported; two Allowed responses cannot detect revoke/regrant. No File owner query/receipt or complete Folder/File resolution namespace is implemented. |
| Files | `src/Hexalith.Folders/Projections/SemanticIndexing/ISemanticIndexingBridgeReadModel.cs:11–28` can look up/list versions; `src/Hexalith.Folders/Projections/SemanticIndexing/SemanticIndexingFileVersionIdentity.cs:53–102` derives version identity from operation/content/path. | No authoritative `FileReferenceId`/owner File mapping, stable identity lifecycle API or current same-Tenant parent binding. Version identity does not establish rename/move/replacement continuity. `src/Hexalith.Folders/FoldersServiceCollectionExtensions.cs:174,192–203` defaults workspace/semantic providers to unavailable or an opt-in in-memory bridge. Folders source has no resolution publication namespace. |

Projects' `src/Hexalith.Projects.Contracts/Models/ProjectFileReference.cs:17–25` explicitly owns its opaque FileReferenceId. The legacy path/workspace validator and full Conversation adapter cannot be promoted into the missing source contracts. Existing unmapped references require separately authorized owner re-verification.

## Transport progress, request authorization and query writes

The earlier report's in-progress D1 repair is now real committed source. EventStore `ServiceDefaults/Authentication/QueryCallerAuthorityBinding.cs:15–44` binds Tenant/domain/exact query; `Controllers/QueriesController.cs:74–88` preserves originating actor/workload/delegation/scopes/audiences separately from the owner hop. `DomainService/VerifiedQueryCaller.cs:21–31` exposes that verified context; `DomainQueryDispatcher.cs:46–51,73–75` creates and clears request authority. Query-digest issuance handling and the aggregate proof bound of 256 are present. This inspection does not qualify a production issuer/enrollment or package tuple.

Focused current SDK tests pass the real `/query` trust boundary for signed origin preservation, tampering/stripping, actor/Tenant/payload/permission changes, forged headers/context, current owner denial, unavailable signing and feature cleanup. Their owner is substituted and their signing fixture does not establish production authorization. Actual RTI reader reauthorization, revoke/regrant detection and isolation across actors still require the missing owner-native permission/resource evidence.

Zero-write design must account for existing write paths:

- `Client/Streams/SourcePublicationFeed.ReadAsync` reconciles/persists its index; it belongs to the producer.
- `Client/Projections/SharedProjectionEpochCoordinator.ReadDiscoveryAsync:2477` calls `GetStatusAsync:2532` and control-index reconciliation (`:2585–2595`); `SharedProjectionControlIndexWriter.cs:106` can save state.
- `SharedProjectionEpochCoordinator.ReadAsync:2407–2460` and `ReadManyAsync:2713–2768` are reusable committed-generation reads, subject to their existing stabilization behavior and the selected query budget.
- Native `AggregateActor.GetStreamMetadataAsync:2523` invokes the actor recovery barrier (`:5532–5558`), which can write pending recovery state. A method named read does not establish zero persisted changes.

No query adapter was added around these seams. Required proof remains byte-for-byte persisted domain/read-model/RTI/task/repair/inspection-audit comparison with producer activity isolated. Existing pure-engine tests do not supply that proof.

## Complete candidate equivalence and supported scale

Projects `Resolution/ConversationResolutionEvidenceMapper.cs:75–126` independently emits authoritative membership and trim/OrdinalIgnoreCase name equality. `ProjectResolutionScoringRules.cs:20–31` preserves weights 50/45/35/30/20 and threshold 20. Consequently, every duplicate-name equality match may qualify even without an inverse reference. Future discovery must union all inverse-reference candidates, authoritative membership and every equality candidate in approved Project-owned name data, then preserve mapper/engine candidates, reasons, outcomes and ordering. No actor grant or scoring label belongs in RTI rows. Existing mapper passes validate their behavior; no indexed-versus-complete equivalence implementation exists to test.

EventStore `SourcePublicationFeed.cs:13–18` explicitly separates its 1,000-source/10,000-entry denial bounds from qualified capacity. Projects AD-27 requires 10,000 Projects per Tenant and up to 5,000 references per Project (`ARCHITECTURE-SPINE.md:300`). No actual resolution scope/partition enrollment demonstrates that coverage. Synthetic shared-rebuild scale fixtures qualify different primitives. Do not widen limits or invent partitions to claim readiness.

Required authenticated complete-result performance remains unexecuted: p95 below 500 ms at 1,000 Projects/500 references and below one second at supported maxima; at most four concurrent owner calls under one two-second deadline; cancellation and no query retries; no truncation, top-N or systematic Unavailable counted as success. The provisional 1 MiB response ceiling remains unfrozen until complete supported results are measured.

## Validation and retained parent gate

| Check | Actual result | Scope |
| --- | --- | --- |
| Story 6.4 and full-index planning validators | Both pass | Planning consistency only; prior capability-value parsing failure is repaired independently. |
| Projects.Tests Debug build; direct assembly `-class Hexalith.Projects.Tests.Resolution.*` | Build 0 warnings/errors; 101 passed, 0 failed/skipped | Existing mappers/engine/contract regressions, not RTI candidate equivalence or persisted query proof. |
| EventStore.DomainService.Tests Debug build; direct authority-correction and origin-context filters | Build 0 warnings/errors; 8 + 12 passed, 0 failed/skipped | Existing SDK trust boundary, with fixture limits above. |
| Projects.Server.Tests Debug build `-m:1` | Exit 1; CS1061 in FoldersServerHostComposition.cs:138,139 | `MapEventStoreDomainProjectionEndpoints` and `MapEventStoreDomainProjectionMetadataEndpoint` are unavailable to that source file. No tests execute. No current server DLL exists for direct-assembly fallback. |
| Required RTI persisted recovery/rebuild, revoke/regrant, complete equivalence, zero query writes and performance | Not executed | Missing source contracts keep implementation non-executable. No RTI acceptance is claimed. |

Exact commands, captured logs and genuine xUnit XML are retained as deterministic gzip artifacts and hashed in the receipt. The serial focused test-project build exposed a concrete owner compile failure. Narrower Projects core and SDK test projects built and ran successfully; no dependency override, source fix, gate weakening or stale server assembly was used.

The Projects `.config/dotnet-tools.json`, `module/hexalith-projects.module.json` and `evidence/g4/6.1-p0-acceptance.json` are absent. The [P0 handoff](../../6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md) still records implemented/historical owner tooling, absent consumer pin/acceptance and pending owner acceptance. Source tooling is not accepted consumption. Adopt its supported manifest/profile command only through that work; do not introduce another runner or reuse the rejected historical platform-v2 `--filter` example.

Parent Story 6.4 remains blocked until owner evidence, exact-current qualification and accepted G-4 consumption are available. This run does not qualify or self-accept P1R, G-2, G-4, G-6 or P2–P4. This task preserves parent specs/story, sprint tracking, decisions/annex, source registrations, legacy routes, engine/scoring, dependency pins and existing evidence. Its only edits are the resumed prerequisite spec and this new evidence bundle. Before/after checks distinguish those changes from concurrent work; protected parent files and inspected source hashes are checked explicitly in the receipt.
