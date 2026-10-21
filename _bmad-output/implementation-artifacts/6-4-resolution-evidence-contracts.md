

**Authority update — approved 2026-10-21:** This is the historical strict-current design. The approved advisory scope reset supersedes its D3-D6 native-proof, complete-result and qualification prerequisites for Story 6.4. Retain D1 authenticated owner checks, D2 real identity mapping, safe metadata purpose, request bounds and zero writes. Do not infer context or confirmation authority from suggestions. See `../planning-artifacts/sprint-change-proposal-2026-10-21-simplify-story-6-4.md` and the rewritten parent story/spec for current implementation scope.
# Story 6.4 prerequisite contract annex

This annex belongs to [the prerequisite specification](spec-6-4-prerequisite-current-authorized-resolution-evidence.md). The user selected D1–D6 on 2026-10-20 with “apply recommended”; the [decision record](6-4-prerequisite-decisions-2026-10-20.md) defines that selection. APIs marked **proposed** remain unimplemented contract candidates until their native source mappings and owner execution evidence exist. Design selection does not establish G-2 delivery, an implemented RTI or development readiness.

## 1. Authority and inspected baseline

The [parent specification](spec-6-4-resolve-projects-with-transient-current-explanations.md), including its October 19 name decision, is authoritative. The [implementation story](6-4-resolve-projects-with-transient-current-explanations.md) and `sprint-status.yaml` remain blocked. The historical [resume preflight](evidence/6-4-resume-20261018/preflight-c91c704.json) correctly identifies full Conversation hydration, incomplete Folder/File metadata evidence, inspection audits, and missing RTI/runner capabilities. This investigation confirms those capability gaps against the following clean source checkouts on 2026-10-19:

| Owning repository | Observed full HEAD, equal to the Projects gitlink where applicable |
| --- | --- |
| Hexalith/Hexalith.Projects | `b0caf2b5c25a1f761abed685c7c9ef811732c6fb` |
| Hexalith/Hexalith.Conversations | `1f3891274eba5cae0754987ee79b4ffc87944401` |
| Hexalith/Hexalith.Folders | `efb98b1e5215ad73b4591f2a2a8ff00f8b8c794a` |
| Hexalith/Hexalith.EventStore | `1eaabca4f8d3597a3a1f9fadf341be5cb9a3a8c0` |
| Hexalith/Hexalith.Builds | `b435f2e3b3892e03f69322d24de6003b0baf7a7d` |

The [2026-10-20 source preflight](evidence/6-4-prerequisite-preflight-20261020/owner-readiness.md) revalidated the owner revisions against Projects `16886b7d9c15b5356fad860b0d7b0f97eb9f32b3`. Its original receipt is historical inspection evidence, captured before D1–D6 were selected. The source corrections below incorporate that inspection without rewriting its recorded state.

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

`src/Hexalith.EventStore/Controllers/QueriesController.cs:SubmitBoundOwnerAuthorityAsync` restricts exact query types using `Authentication:BoundOwnerQuery:AllowedQueries:projects`, verifies the originating assertion, and forwards original permissions with `CallerAuthority.Origin = delegated-owner-query`. It reconstructs the downstream envelope with Tenant, original actor, the calling Projects workload and `IsDelegated=true`; it does **not** supply the original delegation identifier, scopes or audience. Selected D1 requires an additive SDK-verified originating context for those fields, keeping original and owner-hop identities distinct. Its exact source contract is still required. Projects cannot populate authority from owner payloads or claim missing fields already exist.

**Production binding repair selected by D1:** `src/Hexalith.EventStore.ServiceDefaults/Authentication/QueryCallerAuthorityBinding.cs` creates only the exact-query digest. Adjacent `JwtWorkloadAssertionIssuer.ResourceBindings.cs` decodes the request before issuer contact, while `WorkloadAuthorizationDetails.cs` requires Tenant/domain bindings for every nonempty map. The digest-only request therefore fails locally. Adjacent `WorkloadBindingAuthority.cs` additionally requires exact configured binding values; it has no per-query digest issuance policy. Add compatible Tenant/domain bindings and a narrowly defined digest capability for the enrolled gateway under exact workload, Tenant/domain, audience and operation restrictions. The owner still authorizes the actor/resource. Keep arbitrary keys and wildcard values denied; use existing signing/lifetime mechanisms. Production-path tests must prove the contract; Development symmetric signing does not.

`src/Hexalith.EventStore.Client/Gateway/BoundOwnerQueryProof.cs` currently bounds aggregate and principal IDs at **200**, separately from the general 256-character aggregate envelope. Selected D4 requires a tested aggregate-proof amendment to 256 before claiming that supported range. Domain-specific and principal limits remain separately enforced; no truncation or unapproved surrogate is permitted.

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

The least-change action mapping reuses `projects:list` for candidate discovery, `projects:read` for each candidate, Folders `read_metadata` for exact resources, and Conversations `ConversationTenantAccessRequirement.Read` for the new metadata purpose. Selected D1/D5 require explicit owner purpose enforcement; no new permission token or automatic grant is inferred. Existing `ProjectContextQueryAuthority` uses OAuth scope `projects.read` and audience `hexalith-eventstore` when presented, with compatibility for omitted collections. That helper is not permission authority. Exact origin-field and omitted-field handling remain required source contracts before handler implementation.

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
    string? ResourceVersion, string? PermissionVersion,
    DateTimeOffset? AuthorizedAt);
```

The query payload is `{}` and target is the envelope's exact Conversation ID. A successful current result requires metadata, native freshness, source-backed resource/permission revisions and the actual owner verification instant. Non-current results carry no membership/label; native freshness may be absent only for Unavailable. Denial has no DTO and returns `QueryResult.Failure("safe-denial")`. Exact native field mappings and safe-label purpose enforcement remain required. PermissionVersion may represent an approved bounded tuple of native permission/Tenant revisions; it cannot be an invented allocator or arbitrary string. Selected D6 removes a mandatory validity window: add expiry only if the owner guarantees it. A verification timestamp alone does not prove freshness or permission.

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

Existing `src/Hexalith.Folders.Contracts/FolderOwnerReadQuery.cs` is parameterless, QueryType `Hexalith.Folders.Queries.AuthorizeFolderRead.v1`. Existing `FolderOwnerReadResult(FolderOwnerReadOutcome Outcome)` has only Allowed/Stale/Unavailable. Its handler uses `VerifiedQueryCaller`, `delegated-owner-query`, exact target, `{}` payload, no paging, and `read_metadata` with `LayeredFolderOperationPolicy.StrictRead()`. Reuse this per-resource authorization stack. A later measured batch must evaluate each resource scope independently; one Folder grant cannot authorize another.

`FolderOwnerReadQueryHandler.cs` writes no inspection audit. The existing workspace/path metadata route uses `FolderAuditEndpointFilter`, requires workspace/path inputs and has no demonstrated stable File-ID source. `GetFolderFileMetadataAsync(...)` and `IWorkspaceFileContextSource` cannot supply this prerequisite; the latter defaults to Unavailable. Do not change those routes or add a path-to-ID adapter in Projects.

### Proposed receipt and File signatures

Selected Folder evolution: preserve the existing positional result constructor and outcome representation, adding optional native evidence if compatibility tests pass; use an additive version if they fail. These signatures remain proposed until their source fields are mapped:

```csharp
public sealed record FolderOwnerReadEvidence(
    string PermissionVersion, string ResourceVersion, string ResourceWatermark,
    DateTimeOffset LastVerifiedAt, DateTimeOffset AuthorizedAt);

// Existing FolderOwnerReadResult gains this init property:
public FolderOwnerReadEvidence? Evidence { get; init; }

public sealed record FolderFileOwnerReadQuery(string OwnerFileId)
{
    public const string QueryType =
        "Hexalith.Folders.Queries.AuthorizeFolderFileRead.v1";
}

public sealed record FolderFileOwnerReadResult(
    FolderOwnerReadOutcome Outcome, FolderOwnerReadEvidence? Evidence);
```

All are **proposed**. Old results without Evidence remain valid for old consumers but cannot satisfy this new evidence boundary. Selected D6 prefers tolerant v1 only when compatibility and existing semantics are proven. Two fresh Allowed booleans alone cannot prove a stable permission/resource version. LastVerifiedAt refers to native resource observation; AuthorizedAt is the actual owner policy check. Use native permission revisions or an approved tuple, without a new revision allocator, authorization lease or separate cryptographic receipt service. No expiry is required unless the owner promises a validity window.

File target is envelope AggregateId = canonical owning Folder ID; payload contains only the canonical owner File ID obtained through the authoritative mapping. The owner must validate current existence, active lifecycle, exact parent binding, same Tenant, current revocation and exact file/Folder read authority. Return no name, path, URI, workspace, content or provider data. Non-current results have no usable evidence; a missing/denied/cross-Tenant/wrong-parent File gives the same safe denial.

`Projects.Contracts/Models/ProjectFileReference.cs` explicitly calls `FileReferenceId` **Projects-owned**; it is not a demonstrated Folders `FileId`. Selected D2 first reuses a real stable provider identity with explicit owner-supported mapping. Add only a missing Folders-owned identity mapping if the provider cannot supply suitable identity. The actual provider/API and rename/move/delete/replacement behavior remain required source evidence. Never treat caller-supplied FolderId or a path hash as that mapping. Existing unmapped references require owner-backed re-verification through separate work; neither history rewrite nor silent validation/NoMatch is allowed.

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
    DateTimeOffset LastVerifiedAt, string PermissionVersion,
    DateTimeOffset AuthorizedAt);

internal sealed record ReferenceTrustResolutionEvidence(
    VerifiedQueryCaller Caller, long Generation, long ProjectsWatermark,
    bool IsComplete, EvidenceFreshnessState Freshness,
    DateTimeOffset? LastVerifiedAt,
    IReadOnlyList<ReferenceTrustResolutionItem> Items,
    IReadOnlyList<AdmissionComponent> SafeOmissions);
```

The reader verifies reference equality with the SDK feature's query and binds the existing principal/gate before lookup. Inputs come from authorized supported Project membership, never trusted request metadata. Returned Items contain only current authorized evidence for this exact caller; results and Caller never enter storage, logs or wire responses. A foreign actor/workload/delegation cannot reuse an earlier result. Missing feature means safe denial; no direct-caller or legacy fallback is added.

OwnerVersion identifies native resource/membership revision; OwnerWatermark identifies projection/head coverage. Map native fields, including Folder ResourceVersion/PermissionVersion, within documented owner scope; do not equate them to Projects sequence or compare unrelated owner positions. No invented validity window or revision allocator is needed. The DTO cannot authorize anything until its source mapping and currentness protocol are established.

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
    IReadOnlyDictionary<string, string> OwnerCheckpoints,
    bool IsComplete, bool IsRebuilding);

internal sealed record ReferenceTrustIndexProjectPartition(
    string TenantId, string ProjectId, long ProjectVersion, long Generation,
    IReadOnlyList<ReferenceTrustIndexRow> Rows);
```

The Tenant snapshot is a bounded control record; rows are partitioned by Project, with at most 5,001 rows including its Folder. OwnerCheckpoints is bounded to the registered source scopes and retains each owner's native checkpoint. Do not materialize a Tenant-wide array or invent a shared cross-owner watermark. Selected D3 requires generation-atomic lookup and an initialized/current-head proof before IsComplete can be true; zero observed rows alone proves neither completeness nor empty state. Every lookup must match the committed generation. No actor grant, label, payload or inferred candidate is persisted by this minimal resolution schema. Current actor outcomes are an overlay, freshly owner-checked for disclosure; RTI rows are not permission. This is the resolution subset of the **sole** RTI. Context-refresh/ordinal contracts remain outside this prerequisite.

Maintain inverse membership lookup by canonical `(kind, owner identity)` and Project. Conversation candidate discovery must include authoritative Conversation membership and every name-equality candidate under the existing mapper's trim/OrdinalIgnoreCase semantics. Use approved Project-owned name data/lookup; do not add labels to RTI rows. Test indexed discovery against complete engine inputs, including duplicate names and unrelated Projects, so an optimization cannot silently remove MetadataMatched or change MultipleCandidates. Candidate authorization and scoring remain downstream of discovery.

| Projects-relative file | Selected action after native source contracts are ready |
| --- | --- |
| `src/Hexalith.Projects.Server/ReferenceTrust/ReferenceTrustIndexKeys.cs` | Define collision-safe Tenant-scoped committed-generation/checkpoint and inverse membership lookup keys. |
| `src/Hexalith.Projects.Server/ReferenceTrust/ReferenceTrustIndexProjectionHandler.cs` | Maintain Project-owned relation facts from approved source events, with explicit ReadModelWritePolicy and platform atomic/rebuild seams. |
| `src/Hexalith.Projects.Server/ReferenceTrust/ReferenceTrustCandidateLookup.cs` | Discover the complete reference/membership/name candidate set using the sole RTI and approved Project-owned name data; preserve mapper semantics. |
| `src/Hexalith.Projects.Server/ReferenceTrust/ReferenceTrustOwnerEvidenceIngestor.cs` (conditional) | Prefer extending an existing supported owner checkpoint consumer. Add this narrow adapter only if the named native source requires it; do not build a general ingestion framework or placeholder producer. |
| `src/Hexalith.Projects.Server/ReferenceTrust/ReferenceTrustResolutionReader.cs` | Implement the read interface over existing IReadModelStore plus approved owner singleton reads; no write, refresh, repair or task method. |
| `src/Hexalith.Projects.Server/Conversations/ConversationOwnerResolutionEvidenceReader.cs` | Invoke only the proposed content-free Conversation owner query through DelegatedOwnerQueryClient. |
| `src/Hexalith.Projects.Server/Folders/FolderOwnerResolutionEvidenceReader.cs` | Invoke existing Folder authorization with approved receipt and the approved opaque File query; retain typed evidence. Do not change legacy adapters. |
| `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs` | Add prerequisite readers/projection registration only; do not register Story 6.4 resolution query handlers or change routing defaults. |

**Blocking source gap:** the design direction is selected, but no approved owner ingestion/checkpoint/invalidation feed is available today. Identify its actual upstream API, existing consumer, definitive keys and initial-backfill/head-completeness proof before creating an adapter. Project events supply membership only, not foreign currentness. Existing ProjectReadIndex and per-aggregate rebuild heads do not certify complete empty Tenant coverage. This missing source dependency keeps the prerequisite draft; it is not another design vote or permission to invent a feed.

Query execution cannot populate or repair RTI. Producer writes are allowed only in its separately approved projection/ingestion scope. Resolution's persisted before/after comparison includes RTI and must prove the query caused no changes. Use isolated producer activity during zero-write verification so background writes cannot mask query effects.

## 6. Authorization, freshness and disclosure algorithm

1. Bind the authenticated SDK caller and exact expected operation/target before protected validation or item lookup. Structural byte/count limits may be checked at admission, without echoing values. Verify same Tenant before owner authorization. Never accept Tenant, actor, workload, delegation, permission, expected freshness or trusted time from payload.
2. Read supported Project/index/RTI generation and ETags. Require confirmed Project membership, authorized Folder, lifecycle option and complete owner-head coverage. Null Setup is current-empty only under the existing supported rule; otherwise missing/non-current required Project, Folder, Setup or first-response authority prevents usable admission.
3. Discover candidates through the complete index, then owner-read only approved metadata for distinct needed resources. Start with bounded singleton calls; add scoped owner batching only where measurements require it, preserving independent authorization for each resource. Verify response identity through the exact signed target/payload, schema, source/version and native freshness; never pair results by completion order. Strict Current is required. Folders' existing five-minute strict projection option does not automatically meet the two-second resolution authority requirement.
4. Reauthorize after awaited reads, immediately before disclosure. Compare current Tenant/native permission evidence, owner resource versions, Project sequence/references/ETags, RTI generation/scoped checkpoints and head coverage. A grant that changes between passes cannot be reused, including revocation followed by regrant. Same version with different evidence, regression, malformed/unknown state, future/incoherent time or mixed generation fails closed. Native versions are compared only within their documented owner scope; owners must define legitimate advancement versus invalidation.
5. Only current owner-authorized items reach `Included` and the unchanged mappers. `ConversationLinked` uses authoritative Conversations membership only; no hydration hint or legacy reverse-index membership substitutes. Selected D5 authorizes the narrow scoring-label purpose as product policy; owner classification/enforcement still must exist. Compare authorized labels internally, then omit response DisplayName on candidates and exclusions. No inspection audit is called. A free-text Label or length check alone does not establish safe classification; do not truncate labels or silently drop MetadataMatched.
6. Reuse `AdmissionSnapshot`, `AdmissionComponent`, `AdmissionResponseState`, `AdmissionRecoveryAction` and `EvidenceFreshnessState`. `AsOf` is the server-controlled computation cutoff; source LastVerifiedAt/event/generation timestamps remain distinct and are never overwritten with request time. Use injected time for deterministic tests. No cross-owner global watermark is fabricated.

Required evidence non-current/incomplete gives minimal Unavailable with no resolution result or protected rows; preserve a disclosable version only where already authorized, otherwise use the existing zero sentinel. Selected D5 treats an explicit protected Conversation or attachment target as whole-target safe denial: absent/denied/cross-Tenant targets and lost authority give identical `QueryResult.Failure("safe-denial")`/safe 404. Candidate-local failures expose no protected identity or existence detail. Cancellation propagates. Normalize raw exceptions and owner problems to fixed safe states. Optional omissions yield Partial only if required evidence remains current **and** disclosure of that omission is authorized; otherwise use minimal Unavailable. No omission may manufacture a complete search or NoMatch.

Before scope authorization, malformed or unverifiable input uses safe-denial with no field values. After scope authorization, structural/count/byte overflow uses the existing `QueryAdapterFailureReason.InvalidEnvelope`; preserve the platform's transport mapping and generic field-free response. A transport-level size rejection can remain the platform's ordinary 413. A valid authorized query whose evidence, response-size or deadline budget fails returns minimal Unavailable, never InvalidEnvelope or NoMatch. The prerequisite does not add an HTTP error vocabulary or change legacy mappings.

Use existing closed recovery codes: Retry for a safe dependency fault; RefreshContext only when a supported recovery path is actually available; ContactAdministrator for unresolved/corrupt authority; None only when nothing is required. Add no new recovery enum or inference-history identifier. Platform-owned metadata security-denial auditing remains permitted; Projects/owner inspection, maintenance and selection-trace auditing do not.

## 7. Selected D4 admission limits and outstanding sizing evidence

| Input/work | Selected direction and behavior | Basis/remaining gap |
| --- | --- | --- |
| Resolution JSON | 8,192 UTF-8 bytes; reject unknown/duplicate fields, null entries, unsupported kinds, paging and contradictory targets | Reuse the supported parser precedent on this additive boundary; legacy parsing remains unchanged. |
| Conversation target | Exactly one nonblank canonical opaque owner ID; general AggregateId range up to 256 ASCII characters after the platform proof amendment | Current signed owner proof accepts only 200; selected alignment is not yet implemented. Principal limits are separate. |
| Attachments | 1–32 raw Folder plus File inputs combined, **before deduplication**; empty/overflow invalid | New supported-path rule selected by the user. Legacy checks 32 after trim/deduplication; its behavior stays unchanged. |
| Reference IDs | Preserve Projects persisted bound 128 and owner-specific identity rules; no GUID parsing or universal case-folding | D2's real mapping must reconcile namespaces/bounds. No truncation, silent narrowing or synthetic authority. |
| Safe scoring label | Maximum 160 characters under explicit owner classification/purpose; reuse safe metadata validation and existing comparison | Product direction is selected. Owner enforcement/native field mapping is still required; existing Label is not automatically safe. |
| Candidate domain | At most 10,000 confirmed Projects/Tenant and 5,000 aggregate File/Memory references/Project; Project Folder is separate | Architecture AD-27. Existing context evaluation limit 5,000 includes Folder; do not silently reinterpret it or scan 50 million rows per query. |
| Owner scheduling | Start with at most 4 concurrent calls; one combined 2-second dependency deadline; no query-level retries; deduplicate and cancel outstanding work | Index first and measure actual resource checks. The old full-sweep calculation of 20,066 calls across two passes is not the selected strategy or performance evidence. |
| Response/workspace | 1 MiB remains provisional until complete-result serialization is proven; freeze a justified ceiling before readiness | Every qualifying candidate must fit the accepted contract. Overflow is minimal Unavailable, never truncation/top-N or a passing performance result. |

Canonicalize by exact owner identity rules, then deduplicate `(kind, id)` and ordinal-sort. Reject leading/trailing whitespace instead of silently changing identity; do not case-fold opaque IDs. Only label equality uses the existing mapper's trim/OrdinalIgnoreCase semantics. Sort candidate IDs with the existing engine. Unknown or conflicting duplicate owner evidence is unavailable, not last-write-wins.

**Scalability evidence still required:** selected D3/D4 use complete indexed discovery, including name equality, before current owner checks. Prove current-head/empty coverage and p95 under 500 ms at 1,000 Projects/500 references and under one second at supported maxima. Add a scoped owner batch capability only if measurements require it, with independent per-resource authorization. Neither bounded singleton fan-out, a partial candidate set, a deadline refusal nor overflow demonstrates success. The response ceiling must represent complete supported results; qualification cannot count systematic Unavailable as completion.

## 8. Tests and evidence to deliver after approval

Each acceptance below uses Given/When/Then. Test paths are relative to the named owning repository. New tests must check behavior and persisted boundaries rather than mirror private implementation.

| Owner and exact test files | Required assertions |
| --- | --- |
| Conversations: new `tests/Hexalith.Conversations.Contracts.Tests/Queries/ConversationResolutionMetadataContractTest.cs`; new `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationResolutionMetadataQueryHandlerTest.cs` | Given valid current authorized metadata, when dispatched, then exactly the allowlisted fields appear. Given full-detail/tenant-index/hydration sources instrumented to fail, when the metadata query executes, then none is called. Given denied/absent/cross-Tenant targets, then identical safe-denial shapes result before protected reads. |
| Conversations: existing `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionReadStoreFailClosedTest.cs`, `ConversationProjectionReadServiceTest.cs`; new `ConversationResolutionMetadataProjectionTest.cs` in that Projections folder | Given incomplete/backfilled/mixed generations, expired dispatch ledger, stale/gap/rollback/poison, when read/rebuilt/replayed, then no false current metadata is admitted; complete generations preserve native timestamps. Given authority revision changes during await, then no prior metadata is disclosed. |
| Folders: existing `tests/Hexalith.Folders.Server.Tests/FolderOwnerReadQueryHandlerTests.cs`; new adjacent `FolderFileOwnerReadQueryHandlerTests.cs` | Given verified actor/resource authority, when read, then native receipt scope/version/time is exact. Given old/new v1 consumers, then the constructor/outcome semantics and optional-field compatibility hold; otherwise use an additive version. Given absent/wrong-parent/deleted/replaced/cross-Tenant File or Folder and revoked permission, then denial is indistinguishable; stale/unavailable head remains non-current. Malformed input, mismatch, overflow and cancellation stay bounded and safe. |
| Folders: existing `tests/Hexalith.Folders.Tests/Authorization/EffectivePermissionsRevocationFreshnessTests.cs`, `EffectivePermissionsReadModelFreshnessTests.cs`, `EffectivePermissionsCrossPrincipalTests.cs`, `EffectivePermissionsMetadataLeakageTests.cs` | Given actor/ACL/head changes, when both passes run, then strict metadata authority cannot be borrowed across actors or from stale evidence. Assert no workspace/path/content calls and no inspection audit. |
| EventStore: existing `tests/Hexalith.EventStore.Server.Tests/Authentication/JwtWorkloadAssertionIssuerTests.cs`, `Controllers/BoundOwnerQueryControllerTests.cs`, `BoundOwnerQueryAuthorityTests.cs`; contract tests in `tests/Hexalith.EventStore.Contracts.Tests/Queries/QueryCallerAuthorityTests.cs` | Given production authority signing, when a Tenant/domain/query-digest binding executes, then the enrolled gateway succeeds only within its exact grant and originating fields survive verifiably. Given unknown keys, wildcard grants, forged actor/workload/delegation/audience/payload or expired/revoked authority, then no permission is gained. Given aggregate lengths 200/201/256/257, then the selected amended bound holds without widening principal/domain limits. |
| Projects: existing `tests/Hexalith.Projects.Server.Tests/Queries/ProjectQueryEnvelopePrincipalBindingTests.cs`, `GetProjectContextQueryHandlerTests.cs`, `SupportedProjectProjectionTests.cs` and `.RecoveryFloor.cs`; new `tests/Hexalith.Projects.Server.Tests/ReferenceTrust/ReferenceTrustResolutionReaderTests.cs` and `ReferenceTrustIndexProjectionTests.cs` | Given different actors/Tenants/workloads/delegations or post-read revocation, when evidence is read, then cached Allowed cannot transfer. Given empty-complete vs absent/uninitialized RTI, source-head gaps, corruption, replay/rebuild, mixed generation or changed Project ETag, then only coherent current evidence passes. Producer tests assert actual persisted state, atomic checkpoints and restart/rebuild convergence. |
| Projects: new `tests/Hexalith.Projects.Server.Tests/ReferenceTrust/ResolutionEvidenceGatewayTests.cs`; existing `tests/Hexalith.Projects.Server.Tests/Queries/SupportedProjectGatewayHttpTests.cs` | Given the authenticated `/query` chain, when denial/fault/malformed evidence occurs, then status/body/headers disclose no protected existence. Given 33 duplicate raw attachments, then supported admission rejects while legacy behavior stays unchanged. Given maximum output, then complete serialization and authenticated latency meet the final bounds without top-N or systematic Unavailable. Verify bounded calls, zero inspection audit and cancellation. |
| Projects: new `tests/Hexalith.Projects.Server.Tests/ReferenceTrust/ReferenceTrustCandidateLookupTests.cs` | Given complete membership and Project-name inputs, when indexed discovery runs, then it yields every qualifying candidate under the unchanged mappers, including duplicate-name matches, with deterministic order and no borrowed actor authority. |
| Projects, when parent implementation is separately authorized: existing `tests/Hexalith.Projects.Tests/Resolution/{ConversationResolutionEvidenceMapperTests,AttachmentResolutionEvidenceMapperTests,ProjectResolutionLeakageTests,ProjectResolutionPersistsNothingTests}.cs` | Given the same admitted snapshot in any permutation, when mapped/scored, then weights, reasons, outcomes, rank and archive behavior remain exact; candidates stay unselected and names are absent from both candidates and exclusions. Given non-current required evidence, then no false NoMatch is produced. |

Run individual affected `.csproj` projects in local Debug/source mode, following each repository's configured xUnit/MTP lane. For example: `dotnet test tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Debug`. Do not claim these future tests ran during specification authoring. Clean Release/package compatibility is a separately approved owner qualification, not permission to change pins now.

After accepted G-4 exists, extend the existing Projects Integration.Tests reads profile to compare actual before/after persisted event, Project read-model, RTI, trace, task, repair and inspection-audit stores, across Projects and owner services, through gateway and both eventual resolution handlers. Native reports must not be synthetic, zero/all-skipped or substituted with mock call counts. Exercise restart/two-instance, cross-Tenant, revocation during read and the frozen shadow comparison excluding names. Required final paths remain `evidence/epic6/6.4-resolution-reads.trx` and `.json`; this draft creates neither.

## 9. Selected designs and outstanding delivery evidence

All D1–D6 directions below were selected by the user's 2026-10-20 instruction. The final column tracks capabilities and evidence still to deliver; it is not a request to repeat those design selections.

| ID | Selected design and accountable owners | Outstanding source/delivery evidence |
| --- | --- | --- |
| D1 | EventStore/Security + Conversations: repair existing production Tenant/domain/digest issuance and expose SDK-verified originating delegation context | Exact profile/policy and field mappings; production issuer/forgery/expiry/revocation tests; current actor/resource authorization. Existing cryptographic mechanisms suffice. |
| D2 | Folders + Projects: reuse real stable provider identity with explicit mapping; add only a necessary owner mapping | Named provider/API/files and parent/lifecycle semantics; separate owner-backed re-verification for unmapped existing references. No guessed source or history rewrite. |
| D3 | Projects + Solution Architect + owners: one minimal RTI, complete inverse/name discovery and request-scoped actor checks | Actual owner checkpoint/head/invalidation source, committed-generation keys, initialized-empty proof, native revision mappings and persisted replay/rebuild tests. Narrow adapters only where required. |
| D4 | Product + owners + Test Architect: 32 raw inputs; align aggregate proof to 256; preserve other bounds; index first with initially four calls/two-second deadline | Tested platform amendment, raw/unique regressions, complete candidate equivalence, final response sizing and authenticated latency. Owner batching is conditional on measurements; no truncation. |
| D5 | Product + Conversations/Folders/Security: bounded owner-classified scoring labels, omitted response names, no inspection audit; explicit protected targets safe 404 | Actual owner purpose/classification enforcement and freshness/leakage tests. Required non-current evidence is Unavailable; Partial requires a disclosure-safe optional omission. Existing free-text Label alone is insufficient. |
| D6 | Maintainers + deployment owner + architect: optional v1 native evidence if compatible, additive version otherwise; exact disabled-until-accepted enrollment | Native field mapping, old/new consumer tests, actual environment/configuration, immutable package tuple, qualification and rollback. No synthetic expiry, separate receipt service or package selection is implied. |

Rollback for this additive prerequisite is to leave new handlers/owner allowlist entries disabled, stop using the new reader, and retain existing legacy routing and stored history. RTI generations may be rebuilt only under owner approval; rollback never deletes events or rewrites old contracts. Publication, adoption/configuration and rollback execution need their own authorized owner scope.

**G-4 stays in existing 6.1-P0:** the [Projects handoff](6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md) and [continuation spec](spec-6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md) route to Hexalith.Builds, specifically `references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md`. Builds owns the runner/tool/profile support and acceptance with Platform/Test owners. Projects `.config/dotnet-tools.json` and `module/hexalith-projects.module.json` are absent. No substitute manifest/runner or duplicate work item is created here. P0 owner acceptance, P1R/P2/P3/P4, same-baseline conformance/readiness and applicable G-6 remain independent blockers; this document neither closes nor waives them.

Builds already has source composition/persisted execution/native-report capabilities and local qualification. What remains missing is full P0 acceptance and accepted Projects consumer adoption. Its platform-v2 `ModuleCommandExecutionService` rejects `--filter`; reconcile the parent's historical example through a declared supported reads profile under existing P0. No current runnable consumer command or accepted report is fabricated.

## 10. Historical specification verification record — 2026-10-19

This change delivers only the prerequisite spec and this annex. Source contracts were inspected read-only in parallel under the invoked bmad-build investigation step. No product code, owner contracts, dependency pins, Git state, Story 6.4 frozen text, blocked tracking or legacy routes were changed. No application-suite or persisted-runner pass is claimed.

- `python3 tools/planning/validate_production_authority.py --story-id 6.4` exited 0: `PASS: Story 6.4 is within production authority`. This is planning validation, not a readiness verdict.
- Document checks passed for draft frontmatter, six explicit owner decisions, local Markdown links, CRLF, final newline and trailing whitespace. `git diff --check` exited 0; the separate document checks cover the two untracked new files as well.
- A pre-write SHA-256 snapshot of every original tracked regular file matched after drafting. All five inspected repository HEADs and owner working-tree states matched. The final root change allowlist contains exactly this annex and the prerequisite draft; no original tracked file or submodule was changed.

## 11. Selected recommendation application — 2026-10-20

The user accepted the explained recommendations with “apply recommended.” This update records that selection in the prerequisite spec, this annex and the new decision record. It corrects production signing and transport/raw-count assumptions, removes unsupported mandatory expiry/revision-allocation requirements, distinguishes File identity namespaces, narrows ingestion to real owner sources, and requires complete inverse/name candidate discovery. Design choices are selected; absent source APIs and qualification remain explicit delivery blockers.

The parent Story 6.4 spec/story, sprint status, source code, package pins, gitlinks and owner checkouts remain unchanged. The earlier preflight Markdown/JSON retain their original bytes and capture-time state. No application test, production signer execution, native G-4 report or owner acceptance is claimed for this documentation update.

- `python3 tools/planning/validate_production_authority.py --story-id 6.4`, `--validate-index`, and `git diff --check` all exited 0.
- Document checks passed for frontmatter, six selected decisions, unchanged behavior matrix/safeguards, native receipt consistency, links, UTF-8/CRLF and whitespace. The prerequisite spec is 1,471 tokens using `o200k_base`.
- Snapshot comparison confirmed every unrelated tracked file, both existing evidence files, six repository HEADs, five owner working trees and the index unchanged. The authorized change set is this annex, the prerequisite spec and one new decision record.
