# Projects RTI prerequisite: executable-owner-contract preflight

The requested RTI cannot be implemented against the inspected source contracts. D1–D6 are selected; this is a missing-source result, not a request to revisit those decisions. The separately scoped [RTI draft](../../spec-6-4-prerequisite-projects-reference-trust-index.md) remains blocked. No producer, placeholder feed, RTI implementation, handler registration or performance qualification is claimed.

This report supplements the [decision record](../../6-4-prerequisite-decisions-2026-10-20.md) and [annex](../../6-4-resolution-evidence-contracts.md) without editing them. [The receipt](source-audit.json) captures actual revisions, source hashes, absent proposed files, checks and preservation evidence. Source inspection establishes missing contracts; it is not runtime owner acceptance.

## Inspected revisions

| Repository | Full HEAD |
| --- | --- |
| Projects | `16886b7d9c15b5356fad860b0d7b0f97eb9f32b3` |
| EventStore | `1eaabca4f8d3597a3a1f9fadf341be5cb9a3a8c0` |
| Conversations | `1f3891274eba5cae0754987ee79b4ffc87944401` |
| Folders | `efb98b1e5215ad73b4591f2a2a8ff00f8b8c794a` |
| Builds | `b435f2e3b3892e03f69322d24de6003b0baf7a7d` |

Owner HEADs match the root gitlinks. Initial Folders, Conversations and EventStore source checks were clean. Later snapshots observed concurrent owner planning edits and EventStore source implementation, recorded separately in the receipt; they are not this task's work. All inspected branches are `main`. No checkout, submodule update or dependency change was performed. HEAD alone does not identify the changing owner working trees; the receipt also hashes inspected working files and records status.

## 1. Coverage and checkpoints: platform exists; owner enrollment is missing

EventStore supplies real reusable machinery, rather than a missing generic ingestion framework:

- `Client/Streams/SourcePublicationFeed.cs:19` implements `ReadAsync(SourcePublicationScope, long afterOffset = 0, int pageSize = 100, CancellationToken)`, reconciling a finite source cut and persisting its publication index/checkpoint. This is producer work and can write; do not call it from a resolution query.
- `Contracts/Streams/SourcePublicationCheckpoint.cs:11` carries exact scope, index revision, authority revision, source-head vector and last offset. Its own contract explicitly does not prove absence beyond that vector.
- `Server/Streams/DaprSourcePublicationNamespaceSource.cs:24–57` checks an installed roster with `ISourcePublicationNamespaceAuthority.AuthorizeAsync(...)`, obtains each aggregate's actual `GetStreamMetadataAsync()` head and rechecks heads/authority before returning a cut.
- `Client/Streams/ISourcePublicationNamespaceSource.cs:5–10` requires independently authenticated installation, backfill completeness and current head coverage. `Server/Streams/ISourcePublicationNamespaceAuthority.cs:5–10` requires finite legacy coverage, complete inventory and all-writer registration enforcement, with no available default. A static manifest, observed maximum or stored opaque receipt is insufficient.
- `Client/Projections/SharedProjectionEpochCoordinator.cs:2487` exposes `GetCheckpointAsync(scope, sourceStream, cancellationToken)` with durable accepted position/token/owner sequence. This is a consumer checkpoint, not an independently current namespace head. `ReadAsync<T>` selects a committed generation. `ReadDiscoveryAsync` calls `GetStatusAsync`, which reconciles the control index at line 2532 and can write; it cannot establish zero-write query behavior.

**Missing source contract:** an owner-enrolled, authenticated Projects/Folder/File/Conversation resolution scope with definitive source identities, complete initial inventory/backfill, all-writer admission, native current-head coverage and change/invalidation semantics. The actual adapter/installation, registration authority, source-to-RTI field mapping and consumer are absent. No arbitrary feed discriminator or synthetic checkpoint is introduced here.

Projects `Projections/ProjectReads/ProjectReadIndex.cs:8–17` records observed IDs, maximum delivered global position, observation time and unavailable IDs. `ProjectReadProjectionHandler.cs:47–60` rejects empty replay and checks a per-aggregate rebuild head; lines 64–87 merge the replay into observed Tenant IDs. Neither certifies a complete empty Tenant. Incremental atomic detail/index batches at lines 103–165 are reusable after the coverage contract exists. No RTI, owner checkpoint consumer or Project-name equality index currently exists in Projects source.

## 2. Conversations: assignment facts exist; the required narrow owner read does not

Owner-relative paths below are in `references/Hexalith.Conversations/src/` unless stated otherwise.

- `Hexalith.Conversations.Contracts/Events/ConversationProjectChanged.cs:16` publishes `Metadata`, `PreviousProjectId` and `CurrentProjectId`. `Server/Projections/ConversationProjectionMaterializer.cs:888–905` initializes/replaces authoritative membership; lines 978–985 replace the label. These facts must be preserved in candidate discovery.
- `Contracts/Projections/ConversationSummaryProjectionV1.cs:29` has membership, label and native freshness. `ProjectionFreshnessV1.cs:87` defines trust-bearing Current semantics. These meanings are reusable, but the full summary DTO is not the selected narrow contract.
- `Server/Projections/IConversationProjectionReadStore.cs:27` and `ConversationProjectionReadStore.cs` read the combined summary/detail key. Listing/page validation also reads detail. A DTO that discards detail after retrieving it does not satisfy content-free retrieval.
- `ConversationProjectionReadStore.cs:146–159` treats a missing Tenant index as complete empty and explicitly acknowledges erased/evicted-index indistinguishability. That path cannot prove RTI initialization.
- `Server/Agents/EventStoreConversationTenantCatalogue.cs:16` is a real source-prefix catalogue, using `SourceNamespaceSnapshotReader` and a scoped authority/head-vector checkpoint. Its DTO contains identity, creation time and active state, not Project membership or scoring label. Its test explicitly retains synthetic namespace authority.
- Existing publication enrollment in `Server/Agents/ConversationAgentServiceCollectionExtensions.cs:62–101` is `approved-deletion-v1`, requires external namespace/index/stream providers and emits deletion approvals only. Assignment events and the deterministic `LocalConversationPublicationConsumer` sample do not establish an installed resolution feed.
- `Server/TenantAccess/IConversationTenantAccessService.cs:28` checks Tenant access without a Conversation target or scoring-label purpose. Native Tenants projection versions/watermarks are available, but are not automatically Conversation resource permission revisions.

**Missing source contracts:** annex §3's `GetConversationResolutionMetadata` query/result/handler and isolated generation-consistent metadata key; native resource/permission mapping and owner-enforced scoring-label classification/purpose; an enrolled resolution membership/head/change scope. None of the proposed types is callable today. Reusing detail hydration, the deletion feed or a synthetic namespace would violate the requested boundary.

## 3. Folders/Files: exact authorization exists; evidence and stable File mapping do not

Owner-relative paths below are in `references/Hexalith.Folders/src/` unless stated otherwise.

- `Hexalith.Folders.Contracts/FolderOwnerReadQuery.cs` exposes `Hexalith.Folders.Queries.AuthorizeFolderRead.v1`. `FolderOwnerReadResult.cs:5` remains exactly `FolderOwnerReadResult(FolderOwnerReadOutcome Outcome)`: no native revision/checkpoint receipt.
- `Hexalith.Folders.Server/Queries/FolderOwnerReadQueryHandler.cs` enforces verified delegated identity, exact target, empty payload and `read_metadata` using strict layered authorization. It returns Allowed/Stale/Unavailable without inspection audit. Two Allowed results cannot detect revocation followed by regrant without stable native authority evidence.
- `Hexalith.Folders/Authorization/EventStoreFolderEffectivePermissionsReadModel.cs` and `DaprOrganizationStreamSequenceReader` contain actual per-known-resource projection/stream-head comparisons. They are reusable native authority inputs, not a complete RTI inventory/checkpoint contract or exported Folder receipt.
- `ISemanticIndexingBridgeReadModel.GetFileVersionByIdAsync(tenant, folder, fileVersionId)` and `ListFolderAsync` are real candidate APIs. `SemanticIndexingFileVersionIdentity.cs:53–102` derives version identity from operation ID, content hash and path digest; it supplies no demonstrated stable Projects-reference mapping or rename/move/replacement guarantees. Workspace/path APIs and the default unavailable workspace source are also insufficient.
- Projects `Contracts/Models/ProjectFileReference.cs:17–24` explicitly identifies `FileReferenceId` as Projects-owned. Equating it with an owner File/version identity would invent authority.

**Missing source contracts:** named stable owner File identity/provider and authoritative `FileReferenceId` mapping; exact same-Tenant parent binding, existence, lifecycle and rename/move/delete/replacement behavior; annex §4's native Folder receipt and File query/result/handler, plus native checkpoint/invalidation mapping. Existing unmapped references require separately authorized owner re-verification, not silent validation, rewrite or NoMatch.

## 4. Delegated actor authority: concurrent implementation requires requalification

At the initial clean EventStore inspection, `ServiceDefaults/Authentication/QueryCallerAuthorityBinding.cs` emitted only the query digest; `JwtWorkloadAssertionIssuer.ResourceBindings.cs` decoded it through `WorkloadAuthorizationDetails`, whose nonempty maps required Tenant/domain bindings. `WorkloadBindingAuthority.cs` required exact configured grant values without dynamic exact-query digest issuance. Development signing did not prove the selected production authority path.

That initial `Controllers/QueriesController.cs:74–104` forwarded actor, Projects workload, delegated status and permissions without the selected separate verified original context; `VerifiedQueryCaller` lacked it, and `BoundOwnerQueryProof` bounded aggregate/principal IDs to 200.

**Concurrent correction:** before this report was finalized, external EventStore edits added Tenant/domain bindings, query-digest grant handling, `OriginatingQueryContext` and `VerifiedQueryCaller.OriginatingContext`, controller forwarding, and a 256-character aggregate bound with the principal bound retained at 200. These are real in-progress source changes and invalidate any claim that all D1 code is still absent. Their production-path tests, exact enrollment, compatibility and final source tuple were not qualified by this task. Revalidate the completed owner change rather than reimplementing it. The remaining metadata/mapping/coverage blockers above are independent of this transport work.

## 5. Candidate equivalence and bounded performance: required proof remains blocked

Projects `Resolution/ConversationResolutionEvidenceMapper.cs:75–126` emits both authoritative membership and trim/OrdinalIgnoreCase name-equality signals. MetadataMatched alone reaches the existing threshold of 20. An inverse-reference-only lookup would therefore drop qualifying candidates, including duplicate-name Projects, and could change MultipleCandidates into a single result. A Project-owned equality lookup must preserve all such inputs; labels/grants do not belong in RTI rows.

Reuse `AttachmentResolutionEvidenceMapper`, `ProjectResolutionEngine` and `ProjectResolutionScoringRules` unchanged (weights 50/45/35/30/20, threshold 20). Required future evidence is indexed-versus-complete mapper/engine equivalence, persisted replay/rebuild convergence, revocation/regrant during awaits, isolated-producer zero-write state comparison and authenticated complete-result sizing/latency at supported shapes. These proofs cannot run against missing source contracts.

The existing `SourcePublicationFeed` implementation has operational limits of 1,000 sources and 10,000 publication entries; its documentation explicitly separates them from qualified deployment capacity. Projects supports 10,000 Projects per Tenant and up to 5,000 references per Project. A single default feed scope is therefore not a proved maximum-shape solution. Owner-enrolled scope/partition coverage and performance evidence are needed; no limits are widened or invented partitions enrolled here. Preserve four concurrent owner calls, one two-second deadline, cancellation and no query retries. The provisional 1 MiB response ceiling remains unqualified until complete results are measured.

## Verification and preservation

Only source-inspection and artifact checks establish this report's result. A focused existing Folder-owner test was attempted; exact command, compilation blocker and fallback assessment are retained in the receipt. It cannot validate the proposed missing operations. No RTI acceptance, replay/rebuild, revocation, candidate-equivalence, zero-write runtime or performance test pass is claimed.

Planning validators initially passed. After a concurrent edit to the existing root `6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md`, both `python3 tools/planning/validate_production_authority.py --story-id 6.4` and `--validate-index` exited 1: `capability_status must contain only unique named fields`. This task preserves that external edit; the latest failure supersedes the earlier passes. `git diff --check` passes. New-artifact UTF-8/CRLF, whitespace and local links are checked separately because untracked files are outside that Git check.

Before/after checks cover all pre-existing root tracked/untracked regular files, inspected owner source hashes/revisions and Git indexes. Concurrent owner planning/source edits are preserved and identified rather than attributed to this task. This task adds only the separate draft and this evidence bundle, including exact failed-command logs. It leaves original decisions/annex/prerequisite, Story 6.4, sprint tracking, Projects source registrations, pins and history unchanged.
