# Story 6.4 prerequisite contract annex

This annex belongs to [the prerequisite review draft](spec-6-4-prerequisite-current-authorized-resolution-evidence.md). Observed APIs below exist; every API explicitly marked **proposed** requires its owning repository's approval. The proposed signatures make the review concrete, without representing an agreed G-2 contract or an implemented RTI. Unresolved decisions block development readiness.

## 1. Authority and inspected baseline

The [parent specification](spec-6-4-resolve-projects-with-transient-current-explanations.md), including its October 19 name decision, is authoritative. The [implementation story](6-4-resolve-projects-with-transient-current-explanations.md) and `sprint-status.yaml` remain blocked. The historical [resume preflight](evidence/6-4-resume-20261018/preflight-c91c704.json) correctly identifies full Conversation hydration, incomplete Folder/File metadata evidence, inspection audits, and missing RTI/runner capabilities. This investigation confirms those capability gaps against the following clean source checkouts on 2026-10-19:

| Owning repository | Observed full HEAD, equal to the Projects gitlink where applicable |
| --- | --- |
| Hexalith/Hexalith.Projects | `b0caf2b5c25a1f761abed685c7c9ef811732c6fb` |
| Hexalith/Hexalith.Conversations | `1f3891274eba5cae0754987ee79b4ffc87944401` |
| Hexalith/Hexalith.Folders | `efb98b1e5215ad73b4591f2a2a8ff00f8b8c794a` |
| Hexalith/Hexalith.EventStore | `1eaabca4f8d3597a3a1f9fadf341be5cb9a3a8c0` |
| Hexalith/Hexalith.Builds | `b435f2e3b3892e03f69322d24de6003b0baf7a7d` |

`references/Hexalith.Builds/Props/Directory.Packages.props` currently defaults EventStore to **3.126.0**, Conversations to **1.2.0**, and Folders to **1.3.0**. These are observations, not a selected or accepted execution tuple. The completed [3.125.1 enrollment](spec-6-4-prerequisite-p1r-31251-enrollment.md) and [immutable-binding prerequisite](spec-6-4-prerequisite-p1r-31251-immutable-binding.md) do not supply owner metadata approval, final qualification/acceptance or G-4. Preserve all pins; separate existing qualification work decides exact-current usability. Do not copy older catalog values from historical story prose as current observations.

Frozen behavior remains: weights `50/45/35/30/20`, threshold `20`, one contribution per distinct reason, score descending then ordinal Project ID, no selection, no inferred association writes, no persisted explanation. Archived Projects require explicit inclusion and current authorization. No pre-activation, folderless/quarantined or unconfirmed Project enters scoring. No Memory owner expansion is needed for these Conversation/attachment inputs; do not manufacture a `MemoryMatched` signal.

## 2. Supported seams and demonstrated limits

Paths in each owner section are relative to that repository. Owner checkouts are the root-declared `references/Hexalith.{Owner}` directories; no nested dependency is required or authorized.

### EventStore transport and identity

Existing `src/Hexalith.EventStore.DomainService/DelegatedOwnerQueryClient.cs` provides:

```csharp
Task<QueryResult> InvokeAsync(
    SubmitQueryRequest query,
    CancellationToken cancellationToken = default);
```

It consumes the request's SDK-established `VerifiedQueryCaller`, sends its exact originating query/assertion in `BoundOwnerQueryRequest`, and uses the signed bound-owner gateway route. Reuse it; add no impersonation headers, custom delegation codec, token cache or proprietary endpoint. Proposed owner operations use `SubmitQueryRequest(Tenant, Domain, AggregateId, QueryType, Payload: ...)`. Tenant comes from the verified caller; actor, permissions and freshness never come from payload. Leave `EntityId`, paging, filters and other optional routing overrides unset; the gateway binds the owner envelope target.

`src/Hexalith.EventStore/Controllers/QueriesController.cs:SubmitBoundOwnerAuthorityAsync` restricts exact query types using `Authentication:BoundOwnerQuery:AllowedQueries:projects`, verifies the originating assertion, and forwards original permissions with `CallerAuthority.Origin = delegated-owner-query`. It reconstructs the downstream envelope with Tenant, original actor, the calling Projects workload and `IsDelegated=true`; it does **not** simply copy the original delegation identifier, scopes and audience to the owner envelope. The signed origin is verified at the gateway. D1 must decide whether that existing provenance chain satisfies the full frozen binding, or whether an additive platform-owned verified originating-context representation is required. Projects cannot claim the missing fields are present or put them in owner payloads. An EventStore code change requires its own scope and tests; no particular new identity DTO is approved here.

The deployment owner must enroll only the accepted Conversation/File query types alongside the existing Folder query in that exact allowlist, with existing workload keys/audiences. The actual deployment configuration location was not established in the inspected source; this remains part of D6, not an invented Platform AppHost edit. `tests/Hexalith.Projects.Server.Tests/Queries/SupportedProjectGatewayHttpTests.cs` is a test configuration precedent only.

### Projects authorization and stores

Existing `src/Hexalith.Projects.Server/Queries/ProjectQueryEnvelopePrincipalBinding.cs` provides `bool TryBind(QueryEnvelope query, out IProjectTenantContextAccessor tenantContext)`. Existing `Authorization/ProjectAuthorizationGate.cs` provides:

```csharp
Task<ProjectAuthorizationResult> AuthorizeSupportedScopeAsync(
    bool list, IProjectTenantContextAccessor tenantContext,
    HttpContext httpContext, string correlationId,
    CancellationToken cancellationToken = default);

Task<ProjectAuthorizationResult> AuthorizeSupportedReadAsync(
    string projectId, IProjectTenantContextAccessor tenantContext,
    HttpContext httpContext, string? correlationId, string? taskId,
    Func<string, string, CancellationToken, Task<ProjectDetailItem?>> detailLoader,
    CancellationToken cancellationToken = default);
```

Reuse the full ordered policy and the two-pass stability checks in `Queries/ProjectContextQueryExecutor.cs`. Its older Conversation-start detail store and `ownerBackedTrustAvailable:false` are not the RTI source. The read gate internally permits bounded stale Tenant evidence; resolution must additionally reject non-current Tenant/owner evidence. Scope success alone is insufficient.

Read `Projections/ProjectReads/{ProjectReadModel,ProjectReadIndex,ProjectReadModelKeys}.cs` through `IReadModelStore.GetAsync<T>(storeName, key, cancellationToken)`, retaining returned ETags. `ProjectReadModelKeys.StoreName` is `statestore`; use its length-delimited `Index(tenantId)` and `Detail(tenantId, projectId)` keys. Reuse `ProjectReadProjectionHandler.cs` atomic detail/index updates, recovery floor and coordinated rebuild behavior, plus `Aggregates/Project/ProjectPersistedDetailValidator.cs`.

`ProjectReadModel.Current` proves interpretation of observed history, not current stream-head coverage or owner authorization. `ProjectReadIndex` has no independent initialized/current-head certificate. Missing index versus genuinely empty Tenant therefore needs D3 evidence. Never rename this index or `ProjectReferenceIndex` to RTI.

Do not invoke `Queries/ProjectReadQueryExecutor.ListAsync/GetAsync`: descriptive Chatbot reads and denial paths write `ProjectReadAudit`. Do not invoke `Conversations/ConversationsProjectConversationResolutionDirectory.cs`, which fetches full owner detail, or the path/workspace File adapter. Leave all of them and existing GET routes unchanged.

Proposed least-change action mapping is the existing `projects:list` gate for candidate discovery, `projects:read` for each candidate, Folders `read_metadata` for exact resources, and Conversations `ConversationTenantAccessRequirement.Read` for the new metadata purpose. D1/D5 must approve that purpose/action matrix; no new permission token or automatic grant is inferred. Existing `ProjectContextQueryAuthority` uses OAuth scope `projects.read` and audience `hexalith-eventstore` when presented, with compatibility for omitted collections. That helper is not permission authority or proof that the new resolution scope contract is settled. D1 must state required verified origin fields, including omitted-field handling, before handler implementation.

## 3. Conversations: one content-free owner read

### Existing reusable facts

`src/Hexalith.Conversations.Client/IConversationClient.cs:GetConversationAsync(GetConversationQuery, CancellationToken)` returns `Task<ConversationClientResult<ConversationDetailResult>>`, including messages, participants, file references, attributes and hydration. Discarding those fields after retrieval violates this boundary.

`Contracts/Projections/ConversationSummaryProjectionV1.cs` already defines Conversation identity, `ProjectId`, `Label`, lifecycle and `ProjectionFreshnessV1`. Reuse those meanings, not the whole summary DTO, which also contains unrelated roster/provider fields. `ProjectionFreshnessV1.AllowsTrustBearingDecision()` requires Current/non-stale/current-reason. Preserve native cursor, position and generation semantics.

`Server/Projections/IConversationProjectionReadStore.ReadAsync(...)` returns `ConversationProjectedReadModels` containing summary **and detail** from the same persisted key. Listing reads a tenant index and page validation also reads detail. A narrow return DTO over those calls cannot establish zero content retrieval. Add an isolated metadata key maintained with the existing generation protocol. Do not fetch the old detail key to repair it during a read. Missing/new-generation-unconfirmed metadata stays unavailable. A completed dispatch ledger that has expired cannot be the only lasting consistency proof.

`GetAgentConversationAsync(... AccessibilityOnly:true ...)` is restricted to an independently confirmed Organization Party already holding AiAgent membership. It provides neither the required Project membership nor label and does not prove delegated-human authority. Preserve that operation and its singular `conversation` domain; the normal owner domain is `conversations`.

### Proposed exact public contracts

Add each type to a same-named file under `src/Hexalith.Conversations.Contracts/Queries/`:

```csharp
public sealed record GetConversationResolutionMetadataQuery
{
    public const string QueryType =
        "Hexalith.Conversations.Queries.GetConversationResolutionMetadata.v1";
}

public sealed record ConversationResolutionMetadataV1(
    string ConversationId, string? ProjectId,
    string? SafeLabel, string LifecycleState);

public sealed record ConversationResolutionMetadataResultV1(
    ConversationResolutionMetadataV1? Metadata,
    ProjectionFreshnessV1? Freshness,
    string? AuthorityRevision,
    DateTimeOffset? AuthorizedAt,
    DateTimeOffset? AuthorizationValidUntil);
```

The query payload is `{}` and target is the envelope's exact Conversation ID. A successful current result requires metadata, native freshness and a valid owner-issued authority revision/time window. Non-current results carry no membership/label; native freshness may be absent only for Unavailable. Denial has no DTO and returns `QueryResult.Failure("safe-denial")`. Fields are approval candidates: D1/D5 must establish a real issuer and safe-label permission. A locally generated timestamp or arbitrary revision string does not satisfy them.

Use existing generic EventStore transport rather than adding a second REST/client method. No `IConversationClient`/generated client change is required for this proposed route. If Conversations requires its ordinary client as the public surface, that is a D1 alternative requiring an explicit contract revision and equivalent verified transport, not permission to call the full-detail method.

### Proposed owner files and implementation sequence

| Conversations-relative file | Required action after approval |
| --- | --- |
| `src/Hexalith.Conversations.Server/Projections/ConversationResolutionMetadataReadModel.cs` | Add one content-free stored type containing the metadata subset and native generation/freshness evidence; exclude request authorization grants. |
| `src/Hexalith.Conversations.Server/Projections/IConversationProjectionReadStore.cs` | Add `ValueTask<ConversationResolutionMetadataReadModel?> ReadResolutionMetadataAsync(TenantId tenantId, ConversationId conversationId, CancellationToken cancellationToken)`. |
| `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs` | Implement only the new narrow key read and lasting generation validation. |
| `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelKeys.cs` | Add a collision-safe Tenant/Conversation metadata key using existing owner key conventions. |
| `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelWriter.cs` | Maintain that key with existing projection generation/commit/rebuild logic; exclude partial or mixed generations. Backfill belongs to explicit owner rebuild, never a query. |
| `src/Hexalith.Conversations.Server/Queries/GetConversationResolutionMetadataDomainQueryHandler.cs` | Add `IDomainQueryHandler.ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken = default)`, domain `conversations`, exact proposed QueryType. Verify SDK delegated caller, exact target/action and current owner access before key lookup and again before disclosure. |
| `src/Hexalith.Conversations.Server/Queries/ConversationQueryServiceCollectionExtensions.cs` | Register the additive handler/store seam without changing existing discriminators or routes. |

Reuse `IConversationTenantAccessService.CheckAccessAsync` and existing projection health checks; they do not by themselves prove workload/delegation or resource-specific metadata authority. Its current exact signature is:

```csharp
ValueTask<ConversationTenantAccessDecision> CheckAccessAsync(
    ConversationTenantAccessRequirement requirement,
    TenantId? trustedTenantId, string? callerPrincipalId,
    TenantId? routeTenantId = null, TenantId? commandTenantId = null,
    TenantId? aggregateTenantId = null, TenantId? projectionTenantId = null,
    TenantId? idempotencyTenantId = null,
    CancellationToken cancellationToken = default);
```

Do not add a module-owned authentication system. Owner authority must cover membership and internal scoring-label use separately from descriptive inspection, with no inspection audit.

## 4. Folders: reuse strict authorization; add opaque File evidence

Existing `src/Hexalith.Folders.Contracts/FolderOwnerReadQuery.cs` is parameterless, QueryType `Hexalith.Folders.Queries.AuthorizeFolderRead.v1`. Existing `FolderOwnerReadResult(FolderOwnerReadOutcome Outcome)` has only Allowed/Stale/Unavailable. Its handler uses `VerifiedQueryCaller`, `delegated-owner-query`, exact target, `{}` payload, no paging, and `read_metadata` with `LayeredFolderOperationPolicy.StrictRead()`. Reuse this singleton authorization stack; do not introduce batch authorization across different Folder operation scopes.

`FolderOwnerReadQueryHandler.cs` writes no inspection audit. The existing workspace/path metadata route uses `FolderAuditEndpointFilter`, requires workspace/path inputs and has no demonstrated stable File-ID source. `GetFolderFileMetadataAsync(...)` and `IWorkspaceFileContextSource` cannot supply this prerequisite; the latter defaults to Unavailable. Do not change those routes or add a path-to-ID adapter in Projects.

### Proposed receipt and File signatures

Smallest proposed Folder evolution: preserve the existing positional result constructor and outcome representation, adding an optional evidence property plus one new type in Contracts:

```csharp
public sealed record FolderOwnerReadEvidence(
    string AuthorityRevision, string ResourceVersion, string ResourceWatermark,
    DateTimeOffset LastVerifiedAt, DateTimeOffset AuthorizedAt,
    DateTimeOffset AuthorizationValidUntil);

// Existing FolderOwnerReadResult gains this init property:
public FolderOwnerReadEvidence? Evidence { get; init; }

public sealed record FolderFileOwnerReadQuery(string FileReferenceId)
{
    public const string QueryType =
        "Hexalith.Folders.Queries.AuthorizeFolderFileRead.v1";
}

public sealed record FolderFileOwnerReadResult(
    FolderOwnerReadOutcome Outcome, FolderOwnerReadEvidence? Evidence);
```

All are **proposed**. Old results without Evidence remain valid for old consumers but cannot satisfy this new evidence boundary. D6 must approve tolerant v1 extension or require an additive v2; never silently modify a strict wire contract. Two fresh Allowed booleans alone cannot prove a stable authority/resource version. LastVerifiedAt refers to native resource observation; AuthorizedAt is a separate owner policy check.

File target is envelope AggregateId = canonical owning Folder ID; payload contains only the canonical File-reference ID. The owner must validate current existence, active lifecycle, exact parent binding, same Tenant, current revocation and exact file/Folder read authority. Return no name, path, URI, workspace, content or provider data. Non-current results have no usable evidence; a missing/denied/cross-Tenant/wrong-parent File gives the same safe denial.

`Projects.Contracts/Models/ProjectFileReference.cs` explicitly calls `FileReferenceId` **Projects-owned**; it is not a demonstrated Folders `FileId`. D2 must establish an owner-supported mapping, including rename/delete/replacement stability. Never treat caller-supplied FolderId or a path hash as that mapping.

| Folders-relative file | Required action after approval |
| --- | --- |
| `src/Hexalith.Folders.Contracts/FolderOwnerReadEvidence.cs` | Add the approved native version/time receipt; no invented source version. |
| `src/Hexalith.Folders.Contracts/FolderOwnerReadResult.cs` | Add optional Evidence only after compatibility approval; preserve existing constructor/outcome. |
| `src/Hexalith.Folders.Server/Queries/FolderOwnerReadQueryHandler.cs` | Populate approved receipt from strict current evidence and recheck before return; no audit. |
| `src/Hexalith.Folders.Contracts/FolderFileOwnerReadQuery.cs`, `FolderFileOwnerReadResult.cs` | Add the proposed identity-only File contract. Reuse FolderOwnerReadOutcome. |
| `src/Hexalith.Folders.Server/Queries/FolderFileOwnerReadQueryHandler.cs` | Implement `IDomainQueryHandler.ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken = default)` in domain `folders`, exact File QueryType; reuse strict authorization and approved owner identity source. |
| `src/Hexalith.Folders.Server/FoldersServerServiceCollectionExtensions.cs` | Register only the approved File handler/source. |

**Implementation gap:** no authoritative stable File-reference provider or its implementation file can presently be named from supported source. D2 must supply that owning source and exact method/files before this task is executable. A permanent Unavailable stub is not prerequisite delivery. No source migration or dependency update is included here.

## 5. Projects: one RTI with request-scoped actor authority

### Proposed internal read contract

Add the following types, one per same-named file, under `src/Hexalith.Projects.Server/ReferenceTrust/`. They are internal server contracts, never user payloads or a second public query protocol:

```csharp
internal interface IReferenceTrustResolutionReader
{
    Task<ReferenceTrustResolutionEvidence> ReadAsync(
        QueryEnvelope query,
        IReadOnlyList<ReferenceTrustResolutionInput> inputs,
        CancellationToken cancellationToken);
}

internal sealed record ReferenceTrustResolutionInput(
    string ProjectId, long ProjectVersion,
    string Kind, string ReferenceId, string? FolderId);

internal sealed record ReferenceTrustResolutionItem(
    ReferenceTrustResolutionInput Reference,
    string OwnerVersion, string OwnerWatermark,
    DateTimeOffset LastVerifiedAt, string AuthorityRevision,
    DateTimeOffset AuthorizedAt, DateTimeOffset AuthorizationValidUntil);

internal sealed record ReferenceTrustResolutionEvidence(
    VerifiedQueryCaller Caller, long Generation, long ProjectsWatermark,
    bool IsComplete, EvidenceFreshnessState Freshness,
    DateTimeOffset? LastVerifiedAt,
    IReadOnlyList<ReferenceTrustResolutionItem> Items,
    IReadOnlyList<AdmissionComponent> SafeOmissions);
```

The reader verifies reference equality with the SDK feature's query and binds the existing principal/gate before lookup. Inputs come from authorized supported Project membership, never trusted request metadata. Returned Items contain only current authorized evidence for this exact caller; results and Caller never enter storage, logs or wire responses. A foreign actor/workload/delegation cannot reuse an earlier result. Missing feature means safe denial; no direct-caller or legacy fallback is added.

OwnerVersion identifies native resource/membership revision; OwnerWatermark identifies projection/head coverage. D3 must map native fields, including the proposed Folder receipt's ResourceVersion, without equating either to Projects sequence or comparing unrelated owner position numbers. The proposed DTO has no authority until that mapping is accepted.

### Persisted facts and producer

Use one rebuildable Tenant-scoped RTI, distinct from request authorization. Proposed stored types/files in the same directory:

```csharp
internal sealed record ReferenceTrustIndexRow(
    string ProjectId, long ProjectVersion,
    string Kind, string ReferenceId, string? FolderId,
    string OwnerTenantId, string OwnerVersion, string OwnerWatermark,
    EvidenceFreshnessState Freshness, DateTimeOffset LastVerifiedAt);

internal sealed record ReferenceTrustIndexSnapshot(
    string TenantId, long Generation, long ProjectsWatermark,
    string OwnerCheckpoint, bool IsComplete, bool IsRebuilding);

internal sealed record ReferenceTrustIndexProjectPartition(
    string TenantId, string ProjectId, long ProjectVersion, long Generation,
    IReadOnlyList<ReferenceTrustIndexRow> Rows);
```

The Tenant snapshot is a bounded control record; rows are partitioned by Project, with at most 5,001 rows including its Folder. Do not materialize a Tenant-wide array. D3 must define generation-atomic partition and lookup keys plus completeness proofs; every lookup must match the committed generation. No actor grant, label, payload or inferred candidate is persisted by this minimal resolution schema. Current actor outcomes are an overlay, freshly owner-checked for disclosure; RTI rows are not permission. This is the proposed resolution subset of the **sole** RTI. Do not create an interim store alongside a later RTI. Context-refresh/ordinal contracts remain outside this prerequisite; no invented ordinal is needed to omit protected resolution exclusions.

| Projects-relative file | Proposed action after D3 approval |
| --- | --- |
| `src/Hexalith.Projects.Server/ReferenceTrust/ReferenceTrustIndexKeys.cs` | Define collision-safe Tenant-scoped generation/checkpoint and membership lookup keys using supported store conventions. |
| `src/Hexalith.Projects.Server/ReferenceTrust/ReferenceTrustIndexProjectionHandler.cs` | Maintain Project-owned relation facts from approved source events, with explicit ReadModelWritePolicy and platform atomic/rebuild seams. |
| `src/Hexalith.Projects.Server/ReferenceTrust/ReferenceTrustOwnerEvidenceIngestor.cs` | Consume the approved owner metadata/change/checkpoint source outside queries; establish native coverage/invalidation and completed generations. |
| `src/Hexalith.Projects.Server/ReferenceTrust/ReferenceTrustResolutionReader.cs` | Implement the read interface over existing IReadModelStore plus approved owner singleton reads; no write, refresh, repair or task method. |
| `src/Hexalith.Projects.Server/Conversations/ConversationOwnerResolutionEvidenceReader.cs` | Invoke only the proposed content-free Conversation owner query through DelegatedOwnerQueryClient. |
| `src/Hexalith.Projects.Server/Folders/FolderOwnerResolutionEvidenceReader.cs` | Invoke existing Folder authorization with approved receipt and the approved opaque File query; retain typed evidence. Do not change legacy adapters. |
| `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs` | Add prerequisite readers/projection registration only; do not register Story 6.4 resolution query handlers or change routing defaults. |

**Blocking source gap:** no approved owner ingestion/checkpoint/invalidation feed is available today. The proposed ingestor's callable upstream API and definitive keys/coverage schema cannot be asserted. D3 must identify them and the initial-backfill/head-completeness proof. Project events supply membership only, not foreign currentness. Until then this is a concrete proposed footprint with an unproducible evidence dependency, not ready-for-development work.

Query execution cannot populate or repair RTI. Producer writes are allowed only in its separately approved projection/ingestion scope. Resolution's persisted before/after comparison includes RTI and must prove the query caused no changes. Use isolated producer activity during zero-write verification so background writes cannot mask query effects.

## 6. Authorization, freshness and disclosure algorithm

1. Bind the authenticated SDK caller and exact expected operation/target before protected validation or item lookup. Structural byte/count limits may be checked at admission, without echoing values. Verify same Tenant before owner authorization. Never accept Tenant, actor, workload, delegation, permission, expected freshness or trusted time from payload.
2. Read supported Project/index/RTI generation and ETags. Require confirmed Project membership, authorized Folder, lifecycle option and complete owner-head coverage. Null Setup is current-empty only under the existing supported rule; otherwise missing/non-current required Project, Folder, Setup or first-response authority prevents usable admission.
3. Owner-read only approved metadata for each distinct needed resource using bounded singleton calls. Verify response identity through the exact signed target/payload, schema, source/version and native freshness; never pair results by completion order. Strict Current is required. Folders' existing five-minute strict projection option is not automatically sufficient for AD-20's two-second resolution authority requirement.
4. Reauthorize after awaited reads, immediately before disclosure. Compare current Tenant/permission evidence, owner authority revision/resource versions, Project sequence/references/ETags, RTI generation/checkpoint and head coverage. A grant that changes between passes cannot be reused, including revocation followed by regrant. Same version with different evidence, regression, malformed/unknown state, future/incoherent time or mixed generation fails closed. Native versions are compared only within their documented owner scope; owners must define legitimate advancement versus invalidation.
5. Only current owner-authorized items reach `Included` and the unchanged mappers. `ConversationLinked` uses authoritative Conversations membership only; no hydration hint or legacy reverse-index membership substitutes. Read authorized safe labels solely for internal equality scoring, then omit response DisplayName on candidates and exclusions. No inspection audit is called; if D5 cannot authorize that purpose, development stays blocked rather than silently dropping MetadataMatched.
6. Reuse `AdmissionSnapshot`, `AdmissionComponent`, `AdmissionResponseState`, `AdmissionRecoveryAction` and `EvidenceFreshnessState`. `AsOf` is the server-controlled computation cutoff; source LastVerifiedAt/event/generation timestamps remain distinct and are never overwritten with request time. Use injected time for deterministic tests. No cross-owner global watermark is fabricated.

Required evidence non-current/incomplete gives minimal Unavailable with no resolution result or protected rows; preserve a disclosable version only where already authorized, otherwise use the existing zero sentinel. Protected whole-target denial/absence/cross-Tenant or lost authority gives identical `QueryResult.Failure("safe-denial")`/safe 404. Cancellation propagates. Normalize raw exceptions and owner problems to fixed safe states. Optional omissions yield Partial only if required evidence remains current **and** disclosure of that omission is authorized; otherwise use minimal Unavailable. Do not expose protected omission IDs, names or a surrogate whose authorization has not been established.

Before scope authorization, malformed or unverifiable input uses safe-denial with no field values. After scope authorization, structural/count/byte overflow uses the existing `QueryAdapterFailureReason.InvalidEnvelope`; preserve the platform's transport mapping and generic field-free response. A transport-level size rejection can remain the platform's ordinary 413. A valid authorized query whose evidence, response-size or deadline budget fails returns minimal Unavailable, never InvalidEnvelope or NoMatch. The prerequisite does not add an HTTP error vocabulary or change legacy mappings.

Use existing closed recovery codes: Retry for a safe dependency fault; RefreshContext only when a supported recovery path is actually available; ContactAdministrator for unresolved/corrupt authority; None only when nothing is required. Add no new recovery enum or inference-history identifier. Platform-owned metadata security-denial auditing remains permitted; Projects/owner inspection, maintenance and selection-trace auditing do not.

## 7. Proposed finite limits, requiring D4 acceptance

| Input/work | Proposed limit and behavior | Basis/remaining gap |
| --- | --- | --- |
| Resolution JSON | 8,192 UTF-8 bytes; reject unknown/duplicate fields, null entries, unsupported kinds, paging and contradictory targets | Reuse Projects supported parser precedent; proposed for this new boundary only. |
| Conversation target | Exactly one nonblank canonical opaque owner ID; at most 256 ASCII characters when used as AggregateId | Existing EventStore addressing bound; owner must confirm safe surrogate mapping for other IDs. |
| Attachments | 1–32 raw Folder plus File inputs combined, **before deduplication**; empty/overflow invalid | Reuse legacy resolution's 32-input ceiling without changing that route. |
| Reference IDs | Existing Projects persisted reference bound 128; owner Folder envelope bound 256; no GUID parsing or universal case-folding | Do not silently narrow owner IDs. D2 must decide incompatible mapping/surrogates; reject rather than truncate. |
| Safe scoring label | Proposed maximum 160 characters; reuse Projects safe metadata validator; no paths/prompts/title inference | Existing Project name bound. Existing Conversation Label is not automatically classified safe; D5 approval required. |
| Candidate domain | At most 10,000 confirmed Projects/Tenant and 5,000 aggregate File/Memory references/Project; Project Folder is separate | Architecture AD-27. Existing context evaluation limit 5,000 includes Folder; do not silently reinterpret it or scan 50 million rows per query. |
| Owner scheduling | At most 4 concurrent calls; one combined 2-second dependency deadline; no query-level retries; deduplicate reads and cancel outstanding work | Proposed scheduling under existing interactive timeout. Metadata-only singleton query maximum is 20,066 calls across both passes: 10,000 candidate Folders + 32 File checks + 1 Conversation per pass. This is a finite refusal ceiling, not an acceptable performance claim. |
| Response/workspace | Proposed encoded response ceiling 1 MiB; overflow gives minimal Unavailable, never truncated NoMatch/candidates | A proposed resolution-specific ceiling, not automatic inheritance of the separate diagnostic-export contract. |

Canonicalize by exact owner identity rules, then deduplicate `(kind, id)` and ordinal-sort. Reject leading/trailing whitespace instead of silently changing identity; do not case-fold opaque IDs. Only label equality uses the existing mapper's trim/OrdinalIgnoreCase semantics. Sort candidate IDs with the existing engine. Unknown or conflicting duplicate owner evidence is unavailable, not last-write-wins.

**Scalability gap:** a finite singleton fan-out does not establish the required p95 under 500 ms at 1,000 Projects/500 references or under one second at supported maxima. Current candidate enumeration also lacks an accepted current-head/empty proof. D3/D4 must approve a complete indexed candidate lookup/current authorization strategy and qualify the supported shape. If singleton reuse cannot satisfy it, the Folders/platform owners must separately approve a scoped batch or equivalent supported capability; do not invent one here. Deadline/overflow failure is honest Unavailable, never a passing performance qualification or a fabricated empty result.

## 8. Tests and evidence to deliver after approval

Each acceptance below uses Given/When/Then. Test paths are relative to the named owning repository. New tests must check behavior and persisted boundaries rather than mirror private implementation.

| Owner and exact test files | Required assertions |
| --- | --- |
| Conversations: new `tests/Hexalith.Conversations.Contracts.Tests/Queries/ConversationResolutionMetadataContractTest.cs`; new `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationResolutionMetadataQueryHandlerTest.cs` | Given valid current authorized metadata, when dispatched, then exactly the allowlisted fields appear. Given full-detail/tenant-index/hydration sources instrumented to fail, when the metadata query executes, then none is called. Given denied/absent/cross-Tenant targets, then identical safe-denial shapes result before protected reads. |
| Conversations: existing `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionReadStoreFailClosedTest.cs`, `ConversationProjectionReadServiceTest.cs`; new `ConversationResolutionMetadataProjectionTest.cs` in that Projections folder | Given incomplete/backfilled/mixed generations, expired dispatch ledger, stale/gap/rollback/poison, when read/rebuilt/replayed, then no false current metadata is admitted; complete generations preserve native timestamps. Given authority revision changes during await, then no prior metadata is disclosed. |
| Folders: existing `tests/Hexalith.Folders.Server.Tests/FolderOwnerReadQueryHandlerTests.cs`; new adjacent `FolderFileOwnerReadQueryHandlerTests.cs` | Given verified actor/resource authority, when read, then receipt scope/version/time is exact. Given absent/wrong-parent/deleted/replaced/cross-Tenant File or Folder and revoked permission, then denial is indistinguishable; stale/unavailable head remains non-current. Given malformed extra/duplicate payload, target mismatch, paging, overflow or cancellation, then work is bounded and safe. |
| Folders: existing `tests/Hexalith.Folders.Tests/Authorization/EffectivePermissionsRevocationFreshnessTests.cs`, `EffectivePermissionsReadModelFreshnessTests.cs`, `EffectivePermissionsCrossPrincipalTests.cs`, `EffectivePermissionsMetadataLeakageTests.cs` | Given actor/ACL/head changes, when both passes run, then strict metadata authority cannot be borrowed across actors or from stale evidence. Assert no workspace/path/content calls and no inspection audit. |
| EventStore, if D1 requires changes: existing `tests/Hexalith.EventStore.Server.Tests/Controllers/BoundOwnerQueryControllerTests.cs`, `BoundOwnerQueryAuthorityTests.cs`; contract tests in `tests/Hexalith.EventStore.Contracts.Tests/Queries/QueryCallerAuthorityTests.cs` | Given valid signed origin, when an enrolled owner operation executes, then all approved origin bindings survive verifiably. Given forged actor, Tenant, workload, delegation, audience, payload, query type or origin signature, then no owner authority is admitted. No allowlist broadening by prefix. |
| Projects: existing `tests/Hexalith.Projects.Server.Tests/Queries/ProjectQueryEnvelopePrincipalBindingTests.cs`, `GetProjectContextQueryHandlerTests.cs`, `SupportedProjectProjectionTests.cs` and `.RecoveryFloor.cs`; new `tests/Hexalith.Projects.Server.Tests/ReferenceTrust/ReferenceTrustResolutionReaderTests.cs` and `ReferenceTrustIndexProjectionTests.cs` | Given different actors/Tenants/workloads/delegations or post-read revocation, when evidence is read, then cached Allowed cannot transfer. Given empty-complete vs absent/uninitialized RTI, source-head gaps, corruption, replay/rebuild, mixed generation or changed Project ETag, then only coherent current evidence passes. Producer tests assert actual persisted state, atomic checkpoints and restart/rebuild convergence. |
| Projects: new `tests/Hexalith.Projects.Server.Tests/ReferenceTrust/ResolutionEvidenceGatewayTests.cs`; existing `tests/Hexalith.Projects.Server.Tests/Queries/SupportedProjectGatewayHttpTests.cs` | Given the actual authenticated `/query` chain and approved owner handlers, when denial/fault/malformed evidence occurs, then status/body/headers disclose no protected existence. Given limits/permutations/duplicate input, then calls remain bounded and evidence deterministic. Verify zero inspection audit and cancellation propagation. |
| Projects, when parent implementation is separately authorized: existing `tests/Hexalith.Projects.Tests/Resolution/{ConversationResolutionEvidenceMapperTests,AttachmentResolutionEvidenceMapperTests,ProjectResolutionLeakageTests,ProjectResolutionPersistsNothingTests}.cs` | Given the same admitted snapshot in any permutation, when mapped/scored, then weights, reasons, outcomes, rank and archive behavior remain exact; candidates stay unselected and names are absent from both candidates and exclusions. Given non-current required evidence, then no false NoMatch is produced. |

Run individual affected `.csproj` projects in local Debug/source mode, following each repository's configured xUnit/MTP lane. For example: `dotnet test tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Debug`. Do not claim these future tests ran during specification authoring. Clean Release/package compatibility is a separately approved owner qualification, not permission to change pins now.

After accepted G-4 exists, extend the existing Projects Integration.Tests reads profile to compare actual before/after persisted event, Project read-model, RTI, trace, task, repair and inspection-audit stores, across Projects and owner services, through gateway and both eventual resolution handlers. Native reports must not be synthetic, zero/all-skipped or substituted with mock call counts. Exercise restart/two-instance, cross-Tenant, revocation during read and the frozen shadow comparison excluding names. Required final paths remain `evidence/epic6/6.4-resolution-reads.trx` and `.json`; this draft creates neither.

## 9. Owner decisions and separate G-4 tracking

| ID | Accountable decision and recommended choice | Alternative/consequence; required closing evidence |
| --- | --- | --- |
| D1 | Conversations + EventStore/Security owners: use existing signed delegated-owner transport, with a narrow metadata projection and documented verification of the full originating authority chain | If the gateway provenance chain is insufficient, approve an additive platform-owned originating-context contract before owner handler work. No principal-only or AiAgent substitution. Record exact action/permission, subject/workload/delegation/scope/audience binding, issuer/revision/time semantics and adversarial tests. |
| D2 | Folders owner + Projects owner: establish an authoritative stable opaque File-reference namespace/mapping and parent/existence/lifecycle source | If current Projects IDs cannot be resolved, deliver a separately approved owner capability/mapping before this prerequisite; keep blocked meanwhile. Identify exact source implementation files/API and rename/delete/replacement behavior. Never infer paths, generate synthetic authority or assume equivalence to FileId. |
| D3 | Projects + Solution Architect + owner-system maintainers: approve the sole RTI schema, collision-safe keys, event membership producer, owner ingestion/invalidation source, generation/head/completeness protocol and request-only actor overlay | Persisted actor grants would require an independently approved scoped authorization/revocation protocol; this proposal does not provide one. Record exact upstream APIs, current-empty proof, native version/watermark meanings and replay/rebuild tests before claiming the proposed producer executable. |
| D4 | Product Owner + owner maintainers + Test Architect: accept or revise §7's proposed numeric bounds and a complete candidate/current-authorization lookup that meets performance gates | If singleton owner reuse fails required shape, separately scope owner batching/index support. Do not ship systematic Unavailable as completion or truncate candidates. Record maximum calls/bytes/time and authenticated performance results. |
| D5 | Product Owner + Conversations/Folders/Security owners: authorize bounded safe-label use solely inside scoring without inspection audit, and confirm frozen safe-denial treatment for protected targets | Existing Conversation Label is unvalidated and not automatically safe. If purpose authorization is unavailable, keep blocked rather than fetch content or change scoring. Broader epic prose describes denied attachments as no-match; frozen 6.4 requires protected-target safe 404. Clarify individual attachment vs whole-target handling explicitly under that boundary; any behavior amendment requires separate human renegotiation, not inference here. |
| D6 | Repository maintainers + deployment owner + Solution Architect: approve Folder receipt wire compatibility (optional v1 addition recommended, otherwise additive version), publication/consumer adoption and immutable exact-current tuple | Name the deployment allowlist file/environment, exact approved owner revisions/contracts/packages, compatibility result, acceptance authority/date and rollback/disable plan. This draft selects no release and changes no package. Existing 3.125.1 tooling completion and current 3.126.0 catalog do not settle acceptance. |

Rollback for this additive prerequisite is to leave new handlers/owner allowlist entries disabled, stop using the new reader, and retain existing legacy routing and stored history. RTI generations may be rebuilt only under owner approval; rollback never deletes events or rewrites old contracts. Publication, adoption/configuration and rollback execution need their own authorized owner scope.

**G-4 stays in existing 6.1-P0:** the [Projects handoff](6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md) and [continuation spec](spec-6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md) route to Hexalith.Builds, specifically `references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md`. Builds owns the runner/tool/profile support and acceptance with Platform/Test owners. Projects `.config/dotnet-tools.json` and `module/hexalith-projects.module.json` are absent. No substitute manifest/runner or duplicate work item is created here. P0 owner acceptance, P1R/P2/P3/P4, same-baseline conformance/readiness and applicable G-6 remain independent blockers; this document neither closes nor waives them.

## 10. Specification verification record

This change delivers only the prerequisite spec and this annex. Source contracts were inspected read-only in parallel under the invoked bmad-build investigation step. No product code, owner contracts, dependency pins, Git state, Story 6.4 frozen text, blocked tracking or legacy routes were changed. No application-suite or persisted-runner pass is claimed.

- `python3 tools/planning/validate_production_authority.py --story-id 6.4` exited 0: `PASS: Story 6.4 is within production authority`. This is planning validation, not a readiness verdict.
- Document checks passed for draft frontmatter, six explicit owner decisions, local Markdown links, CRLF, final newline and trailing whitespace. `git diff --check` exited 0; the separate document checks cover the two untracked new files as well.
- A pre-write SHA-256 snapshot of every original tracked regular file matched after drafting. All five inspected repository HEADs and owner working-tree states matched. The final root change allowlist contains exactly this annex and the prerequisite draft; no original tracked file or submodule was changed.
