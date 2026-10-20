# Story 6.4 prerequisite investigation — 2026-10-20

This is source-inspection evidence for the existing [prerequisite draft](../../spec-6-4-prerequisite-current-authorized-resolution-evidence.md) and [contract annex](../../6-4-resolution-evidence-contracts.md). Both documents are preserved unchanged. This addendum identifies corrections and decisions; it grants no implementation, owner acceptance, publication or runtime qualification.

## Result and scope

Story 6.4 remains blocked by approved owner metadata/RTI contracts, an accepted and usable G-4 consumer profile, and its other exact-current entry gates. The user authorized continuing prerequisite work with the existing documents preserved. Between turns, those documents were committed externally in Projects `16886b7d9c15b5356fad860b0d7b0f97eb9f32b3`; that clean revision is this investigation's baseline.

The accompanying [preflight receipt](preflight.json) records actual repository revisions, source hashes, missing consumer files and planning-check output. It is not a G-4 run, readiness acceptance or the final Story 6.4 evidence package. No product code, dependency, gitlink, frozen decision, sprint status or owner checkout was changed.

## Findings that refine the annex

### 1. Production query signing has two demonstrated compatibility gaps

EventStore `src/Hexalith.EventStore.ServiceDefaults/Authentication/QueryCallerAuthorityBinding.cs:16` creates only the exact-envelope digest binding, `eventstore:bound-query-authority-v1`. `JwtWorkloadAssertionIssuer.ResourceBindings.cs:13` validates encoded requests using `WorkloadAuthorizationDetails.Decode` before contacting the issuer. `WorkloadAuthorizationDetails.cs:70` requires separate Tenant and domain bindings whenever any binding exists. Consequently the current digest-only query request is rejected locally by the production authority path.

Adding Tenant/domain alone is insufficient. `WorkloadBindingAuthority.cs:34` authorizes every binding value against exact configured grant values. It supplies no dynamic per-query digest policy; correlation, payload and authority changes alter the digest. Development symmetric signing bypasses this authority-profile path, so the Development gateway fixture cannot prove production support. A compatible platform-owned profile and issuance policy, or an equivalent enrolled external issuer contract, needs EventStore/Security approval and negative tests. No wildcard grant or identity fallback is proposed.

Separately, `src/Hexalith.EventStore/Controllers/QueriesController.cs:74` verifies the exact originating signed query, actor and Tenant. Its downstream `SubmitQuery` at line 90 forwards actor, Projects workload, delegated status and permissions, but does not supply the originating delegation identifier, scopes or audience. An owner receives its own verified envelope, not the original Projects envelope. D1 must explicitly accept the gateway provenance model or require an additive verified originating-context representation; this investigation makes neither decision.

### 2. The proposed identifier limit exceeds the reused transport limit

EventStore `src/Hexalith.EventStore.Client/Gateway/BoundOwnerQueryProof.cs:39` rejects principal or aggregate identifiers longer than **200** characters. The general envelope limit of 256 does not establish support through this signed owner route. Annex §7's proposed 256-character Conversation target therefore needs a platform limit amendment or an explicitly approved narrower input/mapping contract. Never truncate, hash an identity into authority or assume an existing surrogate. `DelegatedOwnerQueryClient` does not normalize this proof-construction `ArgumentException` itself.

### 3. The raw attachment ceiling is a new proposal

Projects `src/Hexalith.Projects.Server/Queries/ResolveProjectFromAttachmentsEndpoint.cs:177` trims and deduplicates each input collection into a `SortedSet`; line 53 applies the 32-reference ceiling to those deduplicated collections. Annex §7's **raw** 32-input ceiling before deduplication is a proposed new admission rule, not exact reuse of legacy behavior. It needs D4 acceptance and tests that distinguish raw and unique counts. Legacy routing and parsing remain unchanged.

### 4. Owner evidence and complete candidate lookup still need real sources

Conversations `ConversationProjectionReadStore.ReadAsync` retrieves `ConversationProjectedReadModels`, including detail. Its summary fields and native `ProjectionFreshnessV1` are reusable meanings, but a narrow response over that read still retrieves content. A separate metadata key must participate in durable generation/rebuild consistency; an expiring dispatch ledger alone is insufficient. Existing `Read` purpose and free-text `Label` do not establish the proposed scoring-label authority receipt.

Folders `FolderOwnerReadQueryHandler` provides strict, unaudited `read_metadata` authorization, but its result contains only an outcome. No authoritative opaque File-reference provider was found; the owner also records that absence in `docs/contract/pd10-v2-consumer-discovery.md:54`. Workspace/path metadata and search-derived file versions cannot substitute for it.

Projects `ProjectReadIndex` contains observed IDs, global position, event time and unavailable IDs, without a Tenant initialization/current-head certificate. `ProjectReadProjectionHandler.PrepareRebuildAsync` rejects empty replay. Stable ETags and a protected per-aggregate rebuild head do not prove a complete empty Tenant or foreign-owner coverage. Reuse existing atomic projection/recovery machinery only after approving the sole RTI's producer, owner invalidation/checkpoint source and generation semantics. A complete inverse-reference lookup must also preserve metadata-name equality candidates; optimizing only explicit reference matches would change scoring.

### 5. G-4 source exists; accepted Projects consumption is missing

Builds implements composition, persisted execution and native-report handling. Its existing `6.1-P0` owner story records local qualification and partial acceptance, with full owner/Test Architect acceptance absent. `evidence/g4/6.1-p0-acceptance.json` is absent in that checkout. Projects has neither `.config/dotnet-tools.json` nor `module/hexalith-projects.module.json`, and no accepted reads profile.

Builds `src/libraries/Hexalith.Builds.Tooling/Runtime/ModuleCommandExecutionService.cs:1044` rejects `--filter` for platform-v2 execution. The parent's historical `--profile reads --filter Story=6.4` example cannot be treated as a runnable current v2 command. Reconcile Story 6.4 selection through the approved profile/manifest under existing `6.1-P0`; create no replacement runner or duplicate work item. No persisted run was attempted or claimed here.

## Decisions still required

All six annex decisions remain open. A design selection does not supply a missing source API, owner acceptance or successful qualification.

1. **D1 — EventStore/Security and Conversations:** approve a production-compatible query-binding profile/issuer policy and verified owner-visible origin fields, or identify an equivalent approved issuer/provenance contract. Recommendation: resolve the production binding mismatch first; retain exact workload, audience, operation, resource and permission restrictions. Required evidence: agreed field semantics and production-path forgery, expiry, delegation and replay tests.
2. **D2 — Folders and Projects:** provide the authoritative opaque File namespace/provider and exact API, or authorize a separate Folders-owned capability design. Recommendation: establish the owner source before approving the File handler. Required evidence: same-Tenant parent binding, existence and rename/delete/replacement semantics. Neither option permits a Projects path adapter.
3. **D3 — Projects, architect and owner maintainers:** provide an approved owner change/checkpoint/invalidation feed, or separately design that owner capability before the RTI producer. Recommendation: one RTI with generation-complete inverse membership lookup and request-only actor authorization. Required evidence: initialized-empty/current-head proof, native revision mappings, atomic replay/rebuild and revocation tests.
4. **D4 — Product, owner maintainers and Test Architect:** accept the proposed raw-count admission budget and explicitly reconcile 200 versus 256 transport characters, or revise those bounds before implementation. Recommendation: retain raw-input work protection, require a complete indexed candidate strategy and qualify owner batches if singleton calls cannot meet latency. No truncation or systematic Unavailable counts as success.
5. **D5 — Product and owner/Security maintainers:** approve bounded owner-classified labels solely for internal scoring without inspection audit and clarify protected explicit attachments versus candidate-local omissions, or supply a compatible alternative purpose contract. Recommendation: retain response-name omission, indistinguishable protected-target 404, and minimal Unavailable when an omission cannot be disclosed. Any scoring or frozen denial amendment requires explicit renegotiation.
6. **D6 — Maintainers, deployment owner and architect:** approve a tolerant optional v1 Folder receipt after compatibility tests, or require an additive version. Also identify the actual Projects deployment allowlist/issuer configuration and selected immutable consumer tuple. Recommendation: leave new operations disabled until exact enrollment, compatibility, qualification and rollback evidence exist. No deployed Projects allowlist was found; Timesheets configuration is only a precedent.

## Next bounded scope

Resolve D1's production query-binding contract before attempting owner handlers. In parallel, D2/D3 require source contracts, rather than guessed provider implementations. After approved transport, metadata purpose and receipt semantics exist, the first owner implementation slice can be the isolated Conversations metadata projection/read/handler with tests that fail on detail, hydration or tenant-list access. File/RTI implementation and parent resolution handlers remain gated. G-4 continues independently in existing `6.1-P0`.

## Verification limits

Only source inspection, repository-state checks and the recorded planning checks ran. Document validation verifies links, formatting and unchanged original files. No application test, production signer execution, owner approval, performance qualification or persisted Story 6.4 evidence is claimed. The receipt hashes the inspected sources so later owner changes require revalidation.
