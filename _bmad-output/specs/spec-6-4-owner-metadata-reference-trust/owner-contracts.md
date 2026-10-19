# Proposed owner contract changes

Every new name and field in this file is proposed, pending owning-repository acceptance. Existing APIs are identified explicitly. Delivery must be additive, serialization-tolerant and one type per file. Projects cannot implement a substitute for an unaccepted owner capability.

## Shared authorization and read rules

Use authenticated gateway dispatch, `DelegatedOwnerQueryClient.InvokeAsync(SubmitQueryRequest, CancellationToken)`, the existing signed bound-owner route and owner `IDomainQueryHandler` dispatch. Payloads carry targets/options only. Tenant, actor, workload, delegation, permissions, scope and audience come from the verified envelope, never a caller-supplied authority DTO, header or payload field.

The receiving owner requires `VerifiedQueryCaller.Get(context)` for the exact dispatched envelope, expected `CallerAuthority.Origin`, current Tenant access, an allowed requesting workload/action, the originating actor/workload's current authority and current resource-specific policy. Same-Tenant checks precede resource authorization. Folder permissions, Conversation membership and Project policy stay with their owners. Delegation cannot increase actor authority. A Tenant role, valid workload assertion or OAuth scope alone is insufficient.

Authorize the request scope before target-specific validation or lookup. Transport byte/count ceilings may reject uniformly before scope evaluation, without target details. Once scope is authorized, malformed canonical inputs get the existing coarse invalid-envelope result. A protected absent, denied or cross-Tenant target gets `QueryResult.Failure("safe-denial")`, normalized by the supported gateway to the same safe `404`, body and headers. Missing/invalid authentication retains existing platform authentication behavior; no target lookup occurs.

Already-authorized computation may report `Stale|Rebuilding|Unavailable` without resource identity, owner version or denial detail. If existence/omission disclosure is itself unverified, use safe denial. Never return raw owner problems, distinguish absent from denied, or echo invalid values. Responses use `Cache-Control: no-store`; disable shared query-result caching for these operations because a cache hit cannot replace current authority checks.

## E-1 — EventStore caller provenance

Reuse `BoundOwnerQueryRequest.OriginatingQuery` / `OriginatingAssertion`, `BoundOwnerQueryProof` and `QueryCallerAuthorityBinding`. The route already verifies the original assertion against its requesting workload audience and exact query digest. No second token, custom caller transport or Projects identity implementation is proposed.

Proposed changes in EventStore:

1. Add optional, null-omitted `string? RequestingWorkloadId` and `bool? OriginatingIsDelegated` init properties to `QueryCallerAuthority`. Include them in equality, hash, JSON/DataContract round trips and the existing digest binding. Absent additions retain prior legacy serialization/proof bytes.
2. On the verified originating-query branch of `SubmitBoundOwnerAuthorityAsync`, copy original `OriginalActorId`, `UserId`, `AuthenticatedWorkloadId`, `DelegationId`, `Scopes` and `Audience` into the owner query. Set owner-hop `IsDelegated=true`; set `RequestingWorkloadId` from the authenticated requesting workload and `OriginatingIsDelegated` from the verified original flag. Keep `Origin="delegated-owner-query"`, empty roles and original permissions. Do not turn scopes into permissions or propagate administrator hints as authority.
3. `QueryCallerAuthorityBinding.Verify` validates the additions for new queries and binds every target, payload, authority and provenance field. Owners read the original workload and delegation from the verified envelope/additions, and the requesting workload from the new signed property. `VerifiedQueryCaller.Principal` currently reconstructs actor/roles/permissions only; absence of workload claims there is not permission to skip workload authorization.
4. Both workload hops remain authenticated by existing assertions. The original token audience is checked against its approved originating operation; the transport assertion audience is checked against the receiving service. Do not require those two different audiences to be identical.
5. Legacy bound-owner callers retain their current behavior. The new metadata queries require complete originating provenance and refuse legacy `bound-owner-query` or incomplete delegated provenance. Missing, expired, substituted or malformed provenance never falls back to a workload-only owner read. No cross-owner chaining or arbitrary query allowlist expansion.

Deployment configuration enrolls only the accepted query discriminators under `Authentication:BoundOwnerQuery:AllowedQueries:<requesting-workload>`, with existing key/audience configuration. New owner handlers additionally verify domain, action, canonical target and limits. An allowlist string alone is not a resource authorization grant.

## C-1 / C-2 — Conversations metadata read

Add dependency-light `GetConversationResolutionMetadataQuery` and `ConversationResolutionMetadataResult` under `src/Hexalith.Conversations.Contracts/Queries/`; proposed discriminator `Hexalith.Conversations.Queries.GetResolutionMetadata.v1`, domain `conversations`. The query body is exactly `{}`; the single target is `AggregateId`, with dispatched `EntityId` equal to it and no paging/search/filter/hydration options. This is one bounded exact read, not a Conversation search or a message read.

| Result field | Contract |
| --- | --- |
| `ConversationId` | Exact authorized opaque target; absent on denial or non-current evidence. |
| `LinkedProjectId` | Nullable stable membership reference owned by Conversations. No membership list copied into Projects. |
| `SafeLabel` | Nullable allowlisted matching label, max 160 characters; never message-derived text. Internal scoring only, never forwarded in resolution responses/logs. |
| `LifecycleState` | Existing Conversations lifecycle token; only owner-permitted states qualify. |
| `FreshnessState`, `ReasonCode`, `Freshness` | Reuse existing `ProjectionTrustState` and `ProjectionFreshnessReasonCode`; nullable `ProjectionFreshnessV1` is present only when genuine source evidence exists. Current metadata requires matching current state/reason and `Freshness.AllowsTrustBearingDecision()`. Non-current responses carry only safe status/reason and genuine observations, with no target/label/version; absent observations stay null rather than inventing a valid cursor/time. |
| `OwnerVersion`, `OwnerWatermark` | Positive source aggregate version plus completed projection generation/cursor; their coordinate meanings are documented separately, never equated to global position. |
| `TenantAuthorizationWatermark`, `AuthorizationVersion`, `LastVerifiedAt` | Current owner-produced Tenant/action/resource-policy revision and actual UTC verification time. No raw ACL, roles or participant roster. |

Reuse existing Tenant-access evaluation, owner policy and completed-generation validation from `ConversationProjectionReadService` / `IConversationProjectionReadStore`. Add a metadata-only store operation that reads a narrow summary/metadata projection and its completion evidence without fetching/deserializing detail, timeline, messages or hydration. If the current summary is too broad, add a narrow slot to the existing supported projection, using existing dispatch/generation rules rather than a second journal. Require both initial and final current authorization; revocation or generation change during assembly fails closed.

Do not invoke `ConversationQueryHandler.GetAsync`, `conversation-detail`, `conversation-agent-read`, temporal reads, citations or hydration and then discard their unwanted fields. Existing agent accessibility reads do not supply the membership/label/full caller proof this operation needs. The owner must explicitly accept its exact metadata-read permission/action mapping and non-inspection computation classification; none is assumed here.

## F-1 / F-2 — Folders metadata batches and file binding

Preserve `FolderOwnerReadQuery.QueryType = "Hexalith.Folders.Queries.AuthorizeFolderRead.v1"`, its empty request and `FolderOwnerReadResult(FolderOwnerReadOutcome)` unchanged. Reuse its `LayeredFolderAuthorizationService` strict `read_metadata` path for every batch resource, including the current Tenant, ACL, lifecycle, EventStore permission and Dapr policy evidence.

Add proposed `FolderOwnerMetadataBatchQuery`, `FolderOwnerMetadataTarget`, `FolderOwnerMetadataBatchResult` and `FolderOwnerMetadataItem` under `src/Hexalith.Folders.Contracts/`; proposed discriminator `Hexalith.Folders.Queries.ReadOwnerMetadataBatch.v1`, domain `folders`, and `BatchAggregateId = "owner-metadata-batch"`. That fixed `AggregateId` routes the batch; it cannot authorize any member resource. Request body is exactly `{ targets: [...] }`; each target contains `Kind` (`Folder|File`), `FolderId`, and nullable `OwnerFileReferenceId` required only for `File`. No actor, Tenant, path, workspace, URI, provider, label, task or audit option. Result body contains only `Items`, with the following per-slot fields.

| Per-item result field | Contract |
| --- | --- |
| `RequestIndex` | Original bounded target slot, allowing correlation without returning a denied target identity. Exactly one result per normalized request target; consumers reject duplicates, omissions and extras. |
| `Authorization` | Existing `FolderOwnerReadResult`; null represents the same safe denial for missing, forbidden and cross-Tenant resources. No protected fields accompany null. |
| `Freshness` | Proposed dependency-light owner enum `Current|Stale|Rebuilding|Unavailable`; `Allowed` is valid only with `Current`. `Stale` maps to existing `Stale`; rebuilding/unavailable map to existing `Unavailable`. |
| `FolderId`, `OwnerFileReferenceId`, `LifecycleState` | Exact identity/lifecycle only on current authorized evidence. A file must belong to that exact Folder and Tenant. No description/name/path. |
| `OwnerVersion`, `OwnerWatermark` | Resource binding/lifecycle version and owner projection generation/watermark. File evidence includes current file-binding revision, not merely the Folder version. |
| `TenantAuthorizationWatermark`, `AuthorizationVersion`, `LastVerifiedAt` | Current owner-produced Tenant/ACL/action-policy proof and actual UTC verification time. Internal evidence only; never a returned ACL. |

Outer scope denial returns safe `404`. In an authorized batch, each null-authorization slot has identical shape for absent/denied/cross-Tenant; it contains only `RequestIndex`. Stale/rebuilding/unavailable slots contain only slot and safe evidence status. Whole-batch failure or invalid response yields unavailable evidence, never implicit `Allowed`. The later Story 6.4 boundary applies its frozen safe-denial/degradation policy to explicit protected inputs; candidate-only denials never expose candidate identities or counts.

Files require an owner-issued opaque binding. Proposed additive `ProjectFileReference.OwnerFileReferenceId` is nullable and omitted when absent; keep Projects-owned `FileReferenceId` as its stable scoring/link identity. Folders owns the mapping from its issued ID to current file existence, Folder, Tenant, lifecycle and authorization. Reads never create or repair that mapping. Existing IDs do not establish a binding; path-based `GetFolderFileMetadata` is not an allowed fallback.

Folders and Projects must separately approve how a genuine owner-issued binding is established by an already-authorized lifecycle/link operation and attested for legacy references. Any needed event/command evolution or migration gets its own bounded owner scope and rollback. Until that exists, those file rows stay `Unavailable`; do not guess, reinterpret IDs, recover a path or claim file-resolution readiness.

## Freshness and disclosure contract

`Current` requires all of: exact Tenant/resource/caller binding; current Tenant enablement; current actor and workload/action/delegation authority; owner existence/lifecycle policy; positive resource and policy revisions; complete projection generation at the checked authoritative source head; real observation/verification time; and owner-approved freshness thresholds. Age alone, ETag alone, an `Allowed` enum, or a cached Project reference state proves none of these.

`OwnerVersion`, source head, projection cursor, global position and authorization revision are distinct coordinates. Owners must state each coordinate's producer and comparison rule in their acceptance. Reuse supported source-head readers and freshness utilities; no timestamp or watermark fabricated by a consumer. Timestamps beyond accepted skew or missing/zero evidence cannot acquire trust from an age helper's permissive skew behavior. Maximum age/skew and authority expiry must be explicit owner-approved configuration, with stale/expiry boundary tests before qualification. Existing Folders `TenantAccessOptions` distinguishes mutation freshness (5 minutes), diagnostic staleness (30 minutes) and clock-skew tolerance (5 seconds); the owner must record the effective strict-read mapping rather than inheriting diagnostic tolerance as authorization freshness.

Check current authority and evidence before reading metadata and again before returning it. Every required relevant revision/generation must remain stable through the bounded read; a changed proof causes denial or unavailable evidence as appropriate, without retries in a domain handler. This proves a bounded observed snapshot, not a global cross-owner transaction or future permission lease. No `304`, positive cache or persisted RTI observation substitutes for this check.

## P-1 / P-2 / P-3 — Projects RTI evidence

Add internal server contracts under `src/Hexalith.Projects.Server/Projections/ReferenceTrust/`: `IReferenceTrustIndexReader`, `ReferenceTrustReadRequest`, `ReferenceTrustReadResult`, `ReferenceTrustEvidence`, and supported projection key/model types. These are prerequisite names, not existing shipped APIs or public resolution endpoints.

Proposed reader signature: `Task<ReferenceTrustReadResult> ReadAsync(QueryEnvelope query, ReferenceTrustReadRequest request, CancellationToken cancellationToken)`. Bind the exact verified request, using existing Projects authority helpers; never accept payload identity. Request carries canonical resource selectors, query kind/options and supported cursor only. Result carries scoped evidence rows, safe completeness/freshness state, generation and continuation; it contains no chosen candidate or inference score.

| Evidence scope/data | Required contents and behavior |
| --- | --- |
| Authority scope | Tenant, original actor, originating workload, requesting workload where applicable, original delegation flag/ID, exact read action, canonical scope/audience sets, permission/policy fingerprint and current authorization revision. Derive from verified identity; use scoped opaque hashes in keys/cursors, not tokens. |
| Resource binding | Kind, exact owner Folder/file/Conversation identity; Projects file-reference ID and separately attested owner ID where applicable. Only needed safe metadata, no foreign payload or hidden owner details. |
| Owner proof | Outcome, owner version/watermark, Tenant/ACL/policy revisions, actual last verification, freshness, producer/schema and generation. Stored `Allowed` is evidence requiring current reauthorization, never reusable permission. |
| Project association | Supported Project ID/version/lifecycle, Folder binding and required Setup-current indicator; association source sequence/global position/generation. Relationships come from current supported Projects projections, not the legacy reverse index. |

Use two explicit kinds of evidence: a rebuildable Tenant-scoped association/catalog projection and actor-scoped owner evidence. Shared Project membership rows may be projected once; an authorization outcome must never be shared across actors/workloads/delegations. Keys and `IQueryCursorCodec` / `QueryCursorScope` bind all authority fields, canonical input/options hash, query type, schema and generation. Reusing another actor's cursor/evidence, changed permission revision or changed association generation fails closed.

The supported catalog supplies only read-model-confirmed Project identity, lifecycle, Folder reference, version, Setup-current evidence and authorized internal matching name. It must prove complete candidate coverage, including label matching for Conversation resolution; a direct membership lookup alone would lose the existing `MetadataMatched` rule. Do not call public list/get inspection executors. Validate safe names internally against existing metadata rules; omit names from all consumer-facing resolution candidates and exclusions.

The reader joins current authorized owner batches with supported associations and rechecks required revisions/generations before disclosure. Candidate-side Folder/File references passed to the unchanged attachment mapper must be restricted to verified joins and carry the verified state. A stale presented item cannot acquire an `Included` state from an association row. No unauthorized candidate identity/name enters an exclusion. An empty lookup is usable only with positive completeness/current-generation proof; absent index and empty complete index differ.

Producers implement named `IAsyncDomainProjectionHandler` instances over `IReadModelStore` / `IReadModelBatchStore` and explicit `ReadModelWritePolicy`, with duplicate/out-of-order/rebuild/unknown-event handling and atomic association/detail generations. Owner-evidence publication/invalidation has a separate, explicit producer boundary outside read execution. Its owner revision sources and non-regression rules must be pinned; never promote membership events into foreign authorization. No cache population, on-demand repair, owner-evidence refresh write or background publication triggered by the resolution read. A cache miss safely degrades; current owner checks may supply request-scoped evidence without persisting it.

Use the existing `AdmissionSnapshot`, `AdmissionResponseState`, `AdmissionComponent`, `EvidenceFreshnessState` and exact `AdmissionRecoveryAction` codes when adapting evidence later. `AsOf` is the current request computation instant from the injected clock; it is distinct from each producer's `LastVerifiedAt` and is never persisted as inference history. `Complete` requires all required current evidence. `Partial` additionally requires current Project, Folder, Setup and first-response authorization plus an authorized explicit optional omission. `Unavailable` has no usable resolution result/candidates; it never means `NoMatch`. Whole-query `Denied` maps to safe `404`. Never manufacture a singular Project version for a multi-candidate result; use the shared undisclosable sentinel `0` unless the frozen response contract can legitimately disclose one.

## Bounded inputs and work

These are proposed reviewable limits. The observed legacy combined attachment cap is `32`; the remaining new operation budgets require owner acceptance. They do not change the existing Project reference cap or release-performance gate.

| Dimension | Proposed bound and overflow behavior |
| --- | --- |
| Conversation input | One canonical Conversation target, empty body. No list or transcript expansion. |
| Attachment input | At most 32 raw combined Folder/File entries before deduplication; reject 33 even when duplicated. Canonicalize using existing identity rules, preserve case, deduplicate exact kind/identity tuples and sort ordinally. No URI/path decoding or GUID-only restriction. |
| IDs / matching metadata | Aggregate IDs max 256; reference IDs max 128; reject whitespace/control/path-shaped values under existing validators. Matching names/labels max 160; never truncate into a different match. |
| Authority / proof metadata | Existing roles/permissions bounds remain 64 values of max 512 characters. Proposed scope/audience limit also 64 values of max 512; proof strings/watermarks max 512; safe reason codes max 64. Canonicalize sets without changing permission semantics. |
| Owner batches / storage chunks | Max 200 normalized targets/keys per call; no duplicates inside a call. `IReadModelBulkStore.GetManyAsync` validates keyed output. Max 4 in-flight owner batches or bulk chunks per request; cancellation flows to each call. No per-reference remote loop. |
| Candidate/association enumeration | Max 10,000 candidate Projects and 10,000 relevant association rows; page default 50/max 200, at most 200 pages per enumeration. Count scanned rows/pages, including rejected rows, against budgets without disclosing hidden counts. Positive completeness proof is mandatory; limit, malformed page or deadline exhaustion returns minimal `Unavailable`, never truncated ranking. |
| Total work | Max 100 owner batch calls including final verification, 200 read-store bulk calls, 10,000 retained candidate/evidence rows per request. Exceeding any budget stops safely. These are ceilings, not permission to scan every Project's 5,000 references. |
| Transport | Existing 1 MiB bound-owner request ceiling remains; proposed serialized metadata response ceiling 1 MiB per batch/page. A larger, extra-field or malformed response is unavailable evidence; never log its body. |
| Time / retries | Existing AD-27 interactive dependency budget: 2 seconds total, including verification. No infrastructure retries in handlers; timeout/cancellation never yields a usable partial computation. Gateways retain their existing quota/error policy. |

No cap silently removes a signal or changes `MultipleCandidates` to `SingleCandidate`. Supported maxima remain 10,000 Projects and 5,000 references per Project; budget exhaustion is honest unavailability, not a lower advertised capacity or a claim that NFR-5 has passed. Story 8.9 owns measured p95/max release proof and any required separately approved tuning.

## Zero-write boundary

All successful, denied, invalid, stale, rebuilding, faulted, timed-out and cancelled reads must leave Project streams, commands, read models, RTI rows, owner state, tasks, confirmations, maintenance/inspection audit and resolution history byte/semantic-identical. No TTL refresh, repair, staging marker, owner receipt, selection or inference is created by a read. Only the existing platform-owned metadata authorization-denial audit may differ; retain its exact allowed store/key class and scrubbed fields separately in evidence. A Projects `ProjectReadAudit` write is not automatically that exception.

Run projection, publication and file-enrollment setup outside the measured read window. Prove no read initiates those writes even when a record is absent or corrupt. Operational telemetry is bounded and scrubbed, with no persisted resolution trace or protected identifiers. Do not exempt generic “telemetry” or audit stores wholesale.
