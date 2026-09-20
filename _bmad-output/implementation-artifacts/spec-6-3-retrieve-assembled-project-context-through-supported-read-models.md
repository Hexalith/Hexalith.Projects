---
title: 'Retrieve assembled Project Context through supported read models'
type: 'feature'
created: '2026-08-24'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 4
baseline_commit: '5a37f9e4ba9cd7f35afae212398db9f945d4d475'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-state-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The Story 3.x context routes assemble from a Projects-owned Dapr journal, truncate Conversation evidence to one page, and refresh through incomplete live fan-out. They cannot prove the supported AD-32 Project, Folder, Setup, authorization, version, component, recovery, and Reference Trust Index truth required for Chatbot grounding.

**Approach:** Add one cohesive, read-only DomainService slice for context retrieval and explanation over EventStore-managed persisted Project models. Reuse the supported query pattern already used by Conversation-start, preserve the pure allowlist policy and legacy routes for shadow comparison, and expose no sibling payload or durable diagnostic trace. Defer supported Refresh and Reference Trust Index ingestion until their approved contracts exist.

**Decision:** Implement on the current local EventStore and `/query` seams now, following Story 6.2. Focused tests may pass; G-4 and Story 6.1-chain evidence stay non-qualifying until those gates exist.

**Decision:** When other required evidence is current and Setup is null, treat Setup as current empty values (Story 6.2 current-empty). `Complete` and `Partial` remain allowed.

**Decision:** Extract shared AD-32 snapshot types from the Story 6.2 Conversation-start records, one type per file, and reuse them for context. Keep the existing Conversation-start wire shape.

**Decision:** Scope Story 6.3 completion to supported Get and Explain over Project-owned persisted detail and the allowlist. Keep the additive Refresh query contract, but leave its handler unregistered and defer owner-batch Refresh plus RTI ingestion to follow-up work after approved G-2 and RTI artifacts exist. An optional omission is `Partial` only when its identity is already authorized for disclosure; denied or unconfirmed candidates return minimal `Unavailable` until an approved RTI supplies a disclosure-safe ordinal.

**Decision:** Forward the caller's already-validated bearer token through the EventStore Dapr `/query` callback. On Projects, derive the bounded dual-principal values from that authenticated token, require its subject to equal the envelope original actor, and require the remaining normalized envelope identity fields to match. Do not introduce a workload-only impersonation or new signed-delegation scheme.

**Decision:** Before an approved RTI supplies persisted ordinals, order context references deterministically by source kind and then opaque identifier. Switch to persisted Project ordinal ordering only with the approved RTI schema; do not invent an interim ordinal.

## Boundaries & Constraints

**Always:** Authenticate `/query`; forward the already-validated caller bearer through EventStore; derive bounded dual-principal values from that token; require its subject to equal the immutable envelope original actor; and require Tenant, workload, delegation, scopes, and audience to match the normalized envelope. Resolve expected action, target, and version server-side and require exact matches. Use the supported `IReadModelStore` path and `ReadModelWritePolicy`. Require an Active Project with exactly one authorized Folder. Include a reference only after Tenant, Project, lifecycle, authorization, and freshness checks pass; make every disclosure-safe omission explicit and return minimal `Unavailable` when an omission cannot be represented without disclosing an unauthorized identity. Use one AD-32 snapshot vocabulary (`responseState`, `asOf`, authorized `projectVersion` when disclosable, metadata-only `components`, closed recovery actions). Before RTI, order by source kind then opaque identifier. Keep Get and Explain zero-write with zero sibling owner calls. Keep explanation current, request-scoped, and nonpersistent. Reauthorize after persisted reads and before returning `Complete`, `Partial`, or `Unavailable`.

**Never:** Write or revert `sprint-status.yaml`. Trust payloads or custom headers for authority, accept workload-only actor substitution, or invent a signed-delegation or interim-ordinal scheme. Add direct Dapr state access, another journal or query runtime, a second AD-32 vocabulary, an unbounded per-reference fan-out, a parallel trust store, or an invented per-actor authorization fingerprint. Persist explanation or selection traces. Mutate Projects or siblings during refresh. Expose Tenant or actor authority, claims, tokens, prompts, transcripts, paths, file or memory content, secrets, raw owner errors, or unconfirmed-candidate detail. Hand-edit generated files. Switch or retire legacy routing before Story 6.7. Invent unapproved Conversations, Folders, or Memories G-2 batch-read contracts, an unapproved Reference Trust Index schema, or a substitute G-4 module manifest. Relabel `ProjectReferenceIndex` as the Reference Trust Index. Treat Story 6.1 list/open contracts as landed in this checkout.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Complete context | Authorized Active Project; current Project, exactly one Folder, current-empty or current Setup, authorization, and all selected pre-RTI reference evidence | Metadata-only setup and ordered included/excluded references with AD-32 `Complete`, `asOf`, authorized version, components, and `None` recovery | No error expected |
| Partial context | Required evidence current; an optional omission is intentional or otherwise already authorized for identity disclosure | Usable `Partial`; every disclosure-safe omission has closed state/reason/last-verified evidence and applicable recovery | No raw owner detail; never silently drop a disclosure-safe candidate |
| Required or disclosure evidence non-current | Project, Folder, authorization missing/non-current; Setup non-current for a reason other than current-empty; or a denied or unconfirmed candidate cannot be represented without disclosing an unauthorized identity | Minimal `Unavailable`; context use blocked and only applicable recovery actions returned | No candidate identity, fabricated data, timestamp, version, or completeness |
| Protected target | Archived, absent, denied, cross-Tenant, or unverifiable Project | No protected context or explanation | Observationally identical safe `404` |
| Deferred supported Refresh | Approved G-2 owner-batch contracts and RTI schema are absent | Additive query contract remains available, supported handler remains unregistered, and legacy Refresh routing remains unchanged | No invented owner calls, RTI schema, writes, or routing switch |
| Explain | Current assembled evidence includes included and excluded candidates | Deterministic per-reference explanation with no persisted identity | No secrets, payloads, raw upstream problems, or durable trace |
| Replay or fault | Duplicate dispatch, rebuild, restart, store fault, or oversized reference set | Deterministic persisted convergence or honest `Partial`/`Unavailable`; bounded work up to 5,000 references | Preserve cancellation and fail closed without leakage |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Projects.Server/Queries/GetProjectContextEndpoint.cs:52` -- legacy Get; `ProjectContextConversationsPageSize` 100 at 50; authorizes at 73-81; Conversation first page at 104-110; no ACL recheck after lookup. Preserve for shadow; do not extend into the supported handler.
- `src/Hexalith.Projects.Server/Queries/RefreshProjectContextEndpoint.cs:65` -- legacy Refresh; live Folder/Memory/Conversation `Task.WhenAll` fan-out at 121-153; File evidence stays on the projection. Preserve; do not copy live fan-out into the supported path.
- `src/Hexalith.Projects.Server/Queries/GetProjectContextExplanationEndpoint.cs:52` -- legacy Explain; HTTP-bound reassembly. Preserve.
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs:134` -- legacy REST map for `GET .../context`, `.../context/explain`, `.../context/refresh`. Keep until Story 6.7.
- `src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs:41` -- pure allowlist to reuse. Success still hardcodes `Assembled` at 183. Stale-Tenant allowance at 228 must not override AD-32 required-evidence rules.
- `src/Hexalith.Projects/Context/ProjectContextInclusionOrder.cs:34` -- TenantAuthority → ProjectVisibility → ProjectLifecycle → ReferenceAuthorization → ReferenceLifecycle → ReferenceFreshness → ReferenceKindAllowlist. Reuse; do not fork.
- `src/Hexalith.Projects.Contracts/Models/ProjectContext.cs:47` -- legacy DTO; no AD-32 snapshot fields. Keep additive compatibility; do not treat it as supported truth.
- `src/Hexalith.Projects.Contracts/Queries/GetConversationStartSetupQuery.cs` -- Story 6.2 singleton query (`ProjectId`). Reuse the query shape; do not edit unless extracting shared snapshot types.
- `src/Hexalith.Projects.Contracts/Queries/ConversationStartSetupResponse.cs:16` -- story-local AD-32 types (`ConversationStartResponseState`, `ConversationStartComponent`, `ConversationStartAdmissionSnapshot`, `ConversationStartSetupResponse`) in one file. Pattern to copy; no shared AD-32 types exist elsewhere.
- `src/Hexalith.Projects.Server/Queries/GetConversationStartSetupQueryHandler.cs:25` -- supported handler: envelope `TenantId`/`UserId`/`OriginalActorId`/`EntityId??AggregateId` at 45-51; `TenantAccessAuthorizer.AuthorizeDiagnosticReadAsync` before store read at 55-63; Active-only at 69-72; null Setup → `ConversationStartSetup.Empty` at 86-90; `Complete` vs `Unavailable` only (Partial declared, never set); `QueryResult.Failure("safe-denial")`. Copy this shape; do not use body/header identity.
- `src/Hexalith.Projects.Server/Projections/ConversationStartSetup/ConversationStartSetupProjectionHandler.cs` -- `IAsyncDomainProjectionHandler`; `StoreName` `projects-conversation-start-setup` at 26; key `{tenantId}:projects:{projectId}` at 45; `Seed` plus `Apply` at 88-120; `ReadModelWritePolicy.UpdateAsync`. Copy this fold; do not `Rebuild` from an event slice.
- `src/Hexalith.Projects.Server/InMemoryProjectReadModelStore.cs` and `ProjectsServerServiceCollectionExtensions.cs:69` -- fake-then-swap: `AddProjectsServer` registers in-memory `IReadModelStore` at 69-70 and `GetConversationStartSetupQueryHandler` at 123; `AddProjectsServerRuntimeInfrastructure` removes it and installs EventStore at 138-141, then registers the projection. Register 6.3 handlers the same way.
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs:208` -- hand-rolled `POST /query` via `DomainQueryDispatcher`; failure → `Results.NotFound`. Add 6.3 query types here. Do not migrate `AddEventStoreDomainService` in this story (DW-63).
- `src/Hexalith.Projects.Server/Program.cs:15` -- `AddProjectsServer` plus `AddProjectsServerRuntimeInfrastructure`; no SDK host.
- `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailProjection.cs:28` -- deterministic Project fold to reuse. `ProjectDetailProjection.Seed` already exists for incremental apply.
- `src/Hexalith.Projects/Projections/ProjectReferenceIndex/ProjectReferenceIndexProjection.cs:21` -- Project membership/reverse index only. Not the Reference Trust Index. No `ReferenceTrust*` types in `src/`.
- `src/Hexalith.Projects.Infrastructure/DaprProjectProjectionStore.cs:23` -- forbidden journal/rebuild-on-read store. Preserve for legacy shadow only.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Queries/QueryEnvelope.cs` -- TenantId, UserId, OriginalActorId, AuthenticatedWorkloadId, IsDelegated, DelegationId, Scopes, Audience. No Action/Target/Version members.
- `references/Hexalith.EventStore/src/Hexalith.EventStore/Queries/DaprDomainQueryInvoker.cs` -- production handler-query callback creates the Dapr `/query` request. Inject the already-registered `IHttpContextAccessor` and copy only a non-empty inbound Bearer credential when the outbound request has none; never serialize, persist, or log the token.
- `references/Hexalith.EventStore/src/Hexalith.EventStore/Authorization/DualPrincipalClaimsHelper.cs` -- canonical bounded extraction for actor, workload, delegation, scopes, and audience. Reuse from Projects principal binding; do not maintain a second parser.
- `references/Hexalith.EventStore/tests/Hexalith.EventStore.QueryRouting.Tests/` -- add focused invoker coverage for exact bearer forwarding, missing context/header, and no-overwrite behavior. Existing router tests substitute the invoker and cannot prove this hop.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/IDomainQueryHandler.cs` and `IAsyncDomainProjectionHandler.cs` -- required handler seams.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/IReadModelStore.cs`, `IReadModelBatchStore.cs`, `IReadModelBulkStore.cs`, and `ReadModelWritePolicy.cs` -- only permitted persisted read-model path. Batch/bulk stores are unused in Projects `src/` today. Owner G-2 is not `IReadModelBulkStore`.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Queries/SafeDenialQueryRouter.cs` -- SDK opt-in denial router. Unused until SDK host; keep 6.2 `"safe-denial"` mapping on `/query`.
- `src/Hexalith.Projects.Server/EventStoreGatewayTokenForwardingHandler.cs` -- existing bearer-only, no-overwrite forwarding precedent in the opposite direction. Mirror its rules; do not share a cross-repository implementation.
- `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs` -- canonical ordered read chain: JWT/principal, claim transform, tenant freshness, Project ACL, EventStore validator, Dapr deny-by-default. Add a supported-detail reader overload for Story 6.3; do not call or replace the legacy `IProjectDetailReadModel` path.
- `src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidator.cs` -- canonical setup, reference identifier, and safe display-metadata rules currently used at the write boundary. Extract/reuse one pure validation seam for persisted reads; do not recode its bounds or blocklist in handlers.
- `src/Hexalith.Projects/Context/ProjectContextAdmissionAssembler.cs` and `AdmissionRecoveryAction.cs` -- map existing states/diagnostics to the existing closed recovery vocabulary, deduplicate in declaration order, and keep pre-RTI sorting as ordinal `(source kind, opaque ID)`.
- `src/Hexalith.Projects.Testing/Reads/ProjectContextShadowComparator.cs` and `ProjectContextShadowComparison.cs` -- landed Get/Explain shadow comparator; Refresh comparison stays skipped until G-2.
- `_bmad-output/test-artifacts/test-design-epic-6.md:180` -- required E6.3-U01/U02/U03, A01/A02/A03 and P1 E6.3-A04; plus E6-X01 privacy.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- DW-63 SDK host migration, DW-66 Partial admission, DW-67 Folder Unauthorized collapse; all open, gated to 6.7. Do not close them here.
- `module/hexalith-projects.module.json` -- required G-4 manifest; absent. Do not author a substitute.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Projects.Contracts/Queries/` shared AD-32 snapshot types (one type per file) plus `GetProjectContextQuery.cs`, `RefreshProjectContextQuery.cs`, and `ExplainContextSelectionQuery.cs` -- extract the Story 6.2 snapshot vocabulary into shared types without changing Conversation-start JSON, then add additive singleton context queries. Rationale: one snapshot family for Chatbot; keep the legacy `ProjectContext` DTO.
- [x] `src/Hexalith.Projects.Server/Queries/GetProjectContextQueryHandler.cs`, `ExplainContextSelectionQueryHandler.cs`, and `ProjectContextQueryExecutor.cs` -- bind the forwarded principal through canonical dual-principal extraction, require subject-to-actor equality, exact normalized envelope identity and target equality, and use the complete `ProjectAuthorizationGate` read chain with the supported persisted-detail loader before assembly and final return. Store faults before Project authority are safe denial. Rationale: prevent workload impersonation and tenant-only authorization.
- [x] `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs` -- add a narrow overload that evaluates the existing ordered read chain with a supplied supported Project-detail loader/snapshot, returning its authorized detail and tenant evidence without invoking or replacing the legacy `IProjectDetailReadModel`. Rationale: one authority policy for legacy and supported reads without forbidden journal access.
- [x] `src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidator.cs` plus one pure persisted-detail validation seam -- share canonical setup text, enum, identifier, and safe display-metadata validation between writes and supported reads. Reject malformed sequence/lifecycle/time/reference coherence as minimal `Unavailable`. Rationale: fail closed without duplicating domain rules.
- [x] `src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs` and `ProjectContextAdmissionAssembler.cs` -- adapt assembly to AD-32 usability without duplicating decisions in handlers; evaluate missing trust before intentional exclusion, keep stale-Tenant required-evidence rules, sort pre-RTI results by ordinal source kind then opaque ID, report truthful components, and map recovery by cause. Rationale: preserve the pure allowlist and disclosure boundary.
- [x] `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs` and `ProjectQueryEnvelopePrincipalBinding.cs` -- require authentication on `/query` except the existing explicit Development diagnostics bypass; bind the principal through `DualPrincipalClaimsHelper`; register Get and Explain on the existing fake-then-swap composition; keep legacy REST routing. Rationale: authenticate the callback without an SDK host migration.
- [x] `references/Hexalith.EventStore/src/Hexalith.EventStore/Queries/DaprDomainQueryInvoker.cs` and focused query-routing tests -- forward the live request's already-validated Bearer credential to the Dapr `/query` callback, only when present and when the created request has no authorization header. Rationale: make the authenticated Projects callback work in production without adding token fields or minted credentials.
- [x] `src/Hexalith.Projects.Testing/Reads/ProjectContextShadowComparator.cs` -- compare legacy and supported Get/Explain on a frozen representable corpus now; add Refresh when that handler exists; do not normalize known legacy deficits. Rationale: E6.3-A04.
- [x] `tests/Hexalith.Projects.Contracts.Tests/`, `tests/Hexalith.Projects.Tests/Context/`, `tests/Hexalith.Projects.Tests/Queries/`, `tests/Hexalith.Projects.Server.Tests/Queries/` -- cover the I/O matrix, full authority denial at every layer, identity-field mismatches and bounded normalization, Development bypass, persisted-data corruption, Project-lifetime timestamps, file/folder coherence, missing-trust ordering, truthful Partial components, cause-specific recovery, exact 5,000 boundary, zero-write Get/Explain, leakage, safe-404, and current-empty Setup; add U02/Refresh when un-skipped. Rationale: focused proof before G-4.

**Deferred follow-up work — not Story 6.3 completion tasks:**
- Supported `RefreshProjectContextQueryHandler` and `ProjectContextOwnerRefreshService` using approved counted G-2 owner batches. The additive query type remains, but its handler stays unregistered until G-2 is approved.
- Reference Trust Index handler, item, backfill, and bounded producer using an approved Tenant-scoped RTI schema and atomic `IReadModelBatchStore` writes. No RTI types are introduced before schema approval.

**Acceptance Criteria:**
- Given current authorized evidence, when Get or Explain runs through the supported `/query` handler, then the result matches the matrix, uses one AD-32 snapshot vocabulary, and exposes metadata only.
- Given any candidate reference, when assembly evaluates it, then Tenant, Project, lifecycle, authorization, and freshness checks run in order and the candidate is either included or explicitly excluded.
- Given missing, denied, Archived, or cross-Tenant Project authority, when any Story 6.3 query runs, then the caller-observable response is the canonical safe `404`.
- Given explanation, when persisted state is compared before and after, then no Project event, task, audit, selection trace, or sibling mutation was written.
- Given duplicate delivery, rebuild, restart, store or owner fault, or more than 5,000 candidates, when the supported models run, then state converges or the query returns honest `Partial`/`Unavailable` without truncation.
- Given current required evidence and no selected pre-RTI candidates, when assembly runs, then it returns `Complete` with empty collections and does not invent a candidate.
- Given current required evidence and an intentional optional allowlist exclusion, when assembly runs, then the result is `Partial`.
- Given legacy and supported routes coexist, when the frozen comparable corpus runs, then Get and Explain match after the approved AD-32 normalization, and routing stays legacy until Story 6.7.
- Given an authenticated handler query reaches EventStore, when it is dispatched to Projects through Dapr, then the original Bearer credential is forwarded without mutation or persistence and Projects binds its canonical dual-principal values to the immutable envelope before dispatch.
- Given supported Project Context is requested, when any claim-transform, tenant, Project ACL, EventStore-validator, or Dapr-policy layer denies or changes during the read, then the result is canonical safe denial with no protected detail.
- Given persisted setup or reference metadata violates the canonical write-boundary rules, when Get or Explain reads it, then the result is minimal `Unavailable` and serialization never exposes or throws on that content.
- Given RTI is not approved, when response collections are emitted, then they use ordinal source-kind plus opaque-ID ordering and no interim ordinal field; cause-specific recovery actions use the existing closed vocabulary in declaration order.
- Given approved G-2 and RTI artifacts are absent, when Projects services are composed, then supported Refresh remains unregistered, no RTI types or owner calls are introduced, and the legacy Refresh route remains unchanged.

### Review Findings

- [x] [Review][Patch] Require authentication on `/query` and bind envelope identity to the authenticated principal or workload [`src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs:211`]
- [x] [Review][Patch] Restore the legacy Story 6.2 public CLR types and `ConversationStartSetupResponse.Snapshot` signature, adapting internally to the shared AD-32 model [`src/Hexalith.Projects.Contracts/Queries/ConversationStartSetupResponse.cs:13`]
- [x] [Review][Patch] Return `Unavailable` for denied or unconfirmed candidate evidence until the approved RTI/ordinal schema can represent disclosure-safe exclusions [`src/Hexalith.Projects/Context/ProjectContextAdmissionAssembler.cs:317`]
- [x] [Review][Patch] Return canonical safe denial when Project detail cannot be read before protected-target authority is established [`src/Hexalith.Projects.Server/Queries/ProjectContextQueryExecutor.cs:79`]
- [x] [Review][Patch] Reject `Included` Folder evidence whose `FolderId` is null or blank instead of emitting the synthetic `pending` identifier [`src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs:303`]
- [x] [Review][Patch] Validate persisted Project/reference/setup structure, sequence, timestamps, and evidence-cutoff coherence before assembly [`src/Hexalith.Projects.Server/Queries/ProjectContextQueryExecutor.cs:120`]
- [x] [Review][Patch] Reject allowed authorization evidence with an empty projection watermark before comparing reauthorization versions [`src/Hexalith.Projects.Server/Queries/ProjectContextQueryExecutor.cs:156`]
- [x] [Review][Patch] Make admission component flags describe the evidence actually established and returned on overflow, corruption, and `Partial` results [`src/Hexalith.Projects/Context/ProjectContextAdmissionAssembler.cs:56`]
- [x] [Review][Patch] Map recovery actions by omission cause instead of returning `RefreshContext` for every non-authorization omission, including intentional policy exclusions [`src/Hexalith.Projects/Context/ProjectContextAdmissionAssembler.cs:349`]
- [x] [Review][Resolved] Keep the frozen-corpus assembly comparator for Get/Explain; live route shadow dispatch remains deferred to Story 6.7 while routing is intentionally legacy [`src/Hexalith.Projects.Testing/Reads/ProjectContextShadowComparator.cs:26`]
- [x] [Review][Patch] Verify populated persisted Setup survives Get and Explain handler serialization [`tests/Hexalith.Projects.Server.Tests/Queries/GetProjectContextQueryHandlerTests.cs:41`]
- [x] [Review][Patch] Pin contradictory aggregate/entity targets and exact scope/audience casing in handler tests [`tests/Hexalith.Projects.Server.Tests/Queries/GetProjectContextQueryHandlerTests.cs:100`]
- [x] [Review][Patch] Test the exact 5,000-candidate boundary and duplicate persisted-memory identities [`tests/Hexalith.Projects.Tests/Context/ProjectContextAdmissionTests.cs:156`]
- [x] [Review][Resolved] Keep the EventStore pointer unchanged while validating the story-scoped bearer-forwarding working-tree change; no Folders or Tenants pointer changes are part of Story 6.3 [`references/Hexalith.EventStore`]

#### Review pass 7 re-derivation

- [x] [Review][Intent] Forward the validated caller Bearer in `DaprDomainQueryInvoker`, preserving an existing outbound authorization header, with focused EventStore tests.
- [x] [Review][Intent] Use ordinal `(source kind, opaque ID)` ordering before RTI; add no ordinal field.
- [x] [Review][Spec] Reuse the complete `ProjectAuthorizationGate` chain with the supported Project-detail loader, including final authority revalidation and exact tenant-result binding.
- [x] [Review][Spec] Share canonical command-boundary setup/reference validation with persisted reads; include Project-lifetime timestamps and File/Folder coherence.
- [x] [Review][Spec] Map each evidence cause to applicable closed recovery actions and make Partial component status describe usable returned evidence.
- [x] [Review][Patch] Require token subject = envelope actor, reuse bounded `DualPrincipalClaimsHelper` extraction, and cover each identity mismatch plus the Development bypass.
- [x] [Review][Patch] Evaluate missing owner trust before intentional source-kind exclusion so unconfirmed identities cannot leak.
- [x] [Review][Patch] Cover an authorized archived omission as `Partial` plus `SelectAlternative`.

#### Rejected

- [false] The sprint-status transition is not an implementation write: the sprint workflow owns the required `in-progress` to `review` transition and review timestamp.
- [false] The unchecked Refresh/RTI tasks are explicit frozen G-2/RTI deferrals; changing their acceptance wording would require editing the reviewed spec.
- [false] The absent Refresh handler is intentional until G-2, and legacy refresh routing remains available until Story 6.7.
- [false] Supplying no Conversation candidates is the approved pre-RTI behavior; the frozen implementation notes explicitly keep Get/Explain at zero sibling calls.
- [false] Files and memories deliberately use Project-owned persisted detail until the RTI exists; only supplied Conversation candidates are relocated for missing owner-backed trust.
- [false] An undefined `ExcludedSourceKinds` enum value is ignored by `MapSourceKind`; it does not trigger the claimed serialization exception.
- [false] A protected target cannot reach the duplicate/overflow precheck through the supported handler because tenant, lifecycle, tenant ID, and Project ID guards run first in `ProjectContextQueryExecutor`.
- [false] A context/detail Project-ID mismatch cannot reach assembly through the supported handler because the executor validates both persisted IDs before calling the policy.
- [false] Policy-produced legacy `Assembled` results cannot carry `Unavailable` or `Unknown` freshness through the actual authorizer path, so the claimed shadow normalization outcome is unreachable.
- [false] The duplicate sprint-status finding is rejected for the same workflow-owned status-transition reason above.

#### Review pass 9 (chunk 1a — production source)

- [ ] [Review][Patch] Map a post-authority supported-detail store fault to identity-free `Unavailable` plus `Retry`, not canonical safe `404` [`src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs:470`]
- [ ] [Review][Patch] Bind Conversation-start `/query` to the callback principal with the same DualPrincipal envelope match used by Get/Explain [`src/Hexalith.Projects.Server/Queries/GetConversationStartSetupQueryHandler.cs:55`]
- [ ] [Review][Patch] On stripped `Unavailable` admissions, set Folder/Setup component flags from returned evidence, not from pre-strip assembly inputs [`src/Hexalith.Projects/Context/ProjectContextAdmissionAssembler.cs:73`]
- [ ] [Review][Patch] Sanitize overflow `Unavailable` snapshots the same way as persisted-validation failures (`Enum.IsDefined` lifecycle, `projectVersion: 0`) [`src/Hexalith.Projects.Server/Queries/ProjectContextQueryExecutor.cs:114`]
- [ ] [Review][Patch] Fail closed on final reauthorization when Folder/Setup/reference payload changed at the same sequence and timestamps [`src/Hexalith.Projects.Server/Queries/ProjectContextQueryExecutor.cs:188`]
- [ ] [Review][Patch] Assert authorized Get/Explain with blank `ProjectionWatermark` is `"safe-denial"` [`src/Hexalith.Projects.Server/Queries/ProjectContextQueryExecutor.cs:78`]
- [ ] [Review][Patch] Assert Included Folder with blank `FolderId` is invalid and never emits synthetic `"pending"` [`src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs:304`]
- [ ] [Review][Patch] Assert `ConversationStartAdmissionSnapshot.FromShared` preserves mapped component Name/Included/Freshness/Reason [`src/Hexalith.Projects.Contracts/Queries/ConversationStartAdmissionSnapshot.cs:39`]

#### Rejected (review pass 9)

- [false] Empty persisted optional metadata is not a write/read mismatch: Create/Update canonicalizes whitespace to null, and a stored empty string is a write-boundary violation that must be minimal `Unavailable`.
- [false] `MatchesPresented` treating empty scopes/audience as omitted is the frozen Story 6.2 compatibility rule; production Get/Explain still exact-match DualPrincipal lists in `TryBind`.
- [false] Conversation-start Complete emitting empty `recoveryActions` is the frozen Story 6.2 wire; Project Context `None` is the new snapshot contract.
- [false] Unbounded `AdmissionComponent.Reason` is the extracted Conversation-start string; closing it would change preserved JSON.
- [false] Handlers return `"safe-denial"` before `ToReadResponse`/`ToExplanation`; SafeDenial snapshots are not serialized on `/query`.
- [false] Shadow comparison omits `ProjectVersion`, components, and recovery because legacy bodies have no common fields for them.
- [false] Executor `HasTooManyCandidates` omitting conversations cannot disagree with assembly on the supported path, which always supplies `Conversations: []`.
- [false] Null `ReasonCode` on intentional `Excluded` rows is the existing DTO; `FailedCheck`/`Diagnostic` remain the closed omission reason.
- [false] Unregistered Refresh remains the frozen G-2 deferral; dispatcher failure for `RefreshProjectContext.v1` must not impersonate a denied Project.
- [false] `FromShared` null-collection NREs are unreachable: the only caller constructs non-null component and recovery arrays.
- [false] `CountCandidates` Int32 overflow remains unreachable under the 5,000 bound and in-memory collection sizes.
- [false] Null File/Memory elements do not reach assembly on the supported path: `ProjectPersistedDetailValidator` rejects them, and overflow returns before enumeration.
- [false] `ProjectSetup.ExcludedSourceKinds` is a non-nullable list; `CanonicalizeSetup` fails closed on null, and current-empty uses `ProjectSetup.Empty`.
- [false] Development bypass skipping DualPrincipal workload/delegation/scopes/audience is the specified diagnostics exception, not a production bind hole.
- [false] `ApplyExcludedSourceKinds` relocating allowlisted refs is the specified Partial optional-omission path, not a missed include/exclude guard.
- [false] Omitted DualPrincipal collections on Get/Explain correctly fail `TryBind` when the token presents scopes/audience; Story 6.2 omitted-collection compatibility stays on the Conversation-start handler.
- [low] Duplicated `TryReadProjectId` is developer-only duplication with identical Get/Explain parse rules; extracting a shared parser is not a direct correction users would meet.
- [low] The unused `Unavailable(..., bool overflow)` overload is leftover surface after the cause enum split; deleting it would change a public assembler API for no caller.
- [low] Shadow-comparator null-collection NREs require adding guards for inputs current callers never construct.

## Implementation Notes

- Shared AD-32 types live in `AdmissionResponseState`, `AdmissionComponent`, `AdmissionSnapshot`, and `AdmissionRecoveryAction`. `ConversationStartSetupResponse` retains the Story 6.2 `ConversationStartAdmissionSnapshot` CLR signature and receives an internal compatibility projection from the shared snapshot without changing JSON property names.
- Get and Explain share `ProjectContextQueryExecutor` over the Conversation-start `IReadModelStore` (`projects-conversation-start-setup`). Conversations are not owner-fetched (empty list). If a conversation candidate reaches assembly without disclosure-safe owner trust, the result is minimal `Unavailable` with no candidate identity.
- Stale-Tenant on the supported path is `Unavailable`; legacy `Assemble` still allows it. Shadow comparison treats that as a known deficit.
- Refresh query type is defined; no handler is registered until G-2. RTI types were not added.
- Direct xUnit v3 execution (Debug): Contracts 192/192, domain 678/678, Server 662/662, and EventStore QueryRouting 12/12. Solution restore succeeds. The broad solution build remains blocked outside Story 6.3 by duplicate FrontComposer source/package references in Projects.UI and Projects.Mcp (four `CS1704` errors, with downstream `MSB4181` failures). Direct single-node MSBuild and xUnit execution were used because the shared `/tmp` filesystem exhausted its inode quota.
- Intentional or otherwise disclosure-safe optional omissions remain `Partial`; denied or unconfirmed omissions return minimal `Unavailable` without candidate identity.

## Spec Change Log

### 2026-08-24 — Review repair 1
- Trigger: the first review pass found ambiguous projection/query write boundaries and underspecified authority, evidence, ordering, owner-batch, failure, shadow, and verification rules.
- Amended: the post-gate execution and acceptance instructions now distinguish projection ingestion from zero-write queries, include File References, bind authorization per request, define deterministic fail-closed edge behavior and measurable approved limits, constrain shadow normalization, clarify manifest ownership, and add an implementation decision table plus revision preflight.
- Known-bad state avoided: inventing an actor-reusable trust decision, silently truncating or mis-associating owner evidence, treating mixed-version truth as `Complete`, persisting refresh/explanation side effects, or manufacturing a G-4 pass.
- KEEP: preserve the single shared AD-32 vocabulary, supported EventStore read-model seams, safe denial, metadata-only responses, legacy shadow routing, operator-owned prerequisites, and untouched `sprint-status.yaml`.

### 2026-08-24 — Review repair 2
- Trigger: the second review pass found unresolved authority-source distinctions, normal-read authorization semantics, shadow-corpus comparability, evidence-cutoff meaning, ingestion entry points, aggregate limits, and revision preflight.
- Amended: the post-gate plan now separates envelope claims from server expectations, defines exact-scope persisted authorization, freezes comparable shadow inputs, defines authoritative `asOf`, revalidates all query paths, maps aggregate overflow, names ingestion/backfill artifacts and transitions, expands all-operation bounds, requires a checkout allowlist, and splits operator actions into independently completable imperative actions.
- Known-bad state avoided: trusting caller-selected action/scope, comparing changing or known-non-equivalent legacy evidence, leaking after mid-read revocation, accepting same-version mutation, exceeding output bounds, or accepting evidence whose approved inputs cannot be identified and matched.
- KEEP: retain every Review repair 1 safeguard, do not edit the intent contract, do not manufacture absent schemas/pins/tooling, and finalize operator-owned prerequisites as `awaiting-operator` without touching `sprint-status.yaml`.

### 2026-08-24 — Review repair 3
- Trigger: the third review pass exposed an invented unbounded actor-fingerprint model plus remaining ambiguity in gate conjunction, target binding, omission disclosure, empty-index proof, ingestion atomicity, serialization limits, telemetry retention, and evidence checkout phases.
- Amended: all operator actions are now a conjunctive readiness gate; authorization outcomes require an approved bounded producer/applicability model; query identity binds exactly to the envelope target; empty indexes require completeness markers; checkpoints/backfills/tombstones are bounded and atomic; actual serialized bytes govern response limits; evidence provenance has explicit pre-run, generated-artifact, commit, and final-clean phases.
- Known-bad state avoided: unbounded per-principal trust state, authorization evidence with no producer, topology leakage, uninitialized state labeled empty, backfill/live-update races, underestimated responses, and dirty-checkout evidence accepted as clean.
- KEEP: retain Review repairs 1 and 2, current-only AD-32 truth, supported stores, safe denial, bounded zero-write queries, frozen shadow comparison, operator-owned gates, and the untouched orchestrator status file.

### 2026-09-20 — Human resolution: disclosure-safe omissions
- Trigger: implementation review exposed a contradiction between the frozen `Partial` rule for missing owner trust and the later prohibition on disclosing denied or unconfirmed candidate identities before RTI.
- Amended: `Partial` is now limited to omissions whose identity is already authorized for disclosure; denied or unconfirmed candidates return minimal `Unavailable` until an approved RTI provides a disclosure-safe ordinal.
- Known-bad state avoided: leaking candidate existence through exclusion identifiers or inventing an interim opaque-identity scheme.
- KEEP: no RTI, owner-batch, generated-contract, routing, or sprint-status changes are introduced by this clarification.

### 2026-09-20 — Human resolution and review repair 4
- Trigger: review pass 7 found that authenticated Projects callbacks had no production credential path, the pre-RTI ordering rule required a nonexistent ordinal, and the plan omitted the full Project authorization chain, canonical persisted-data validation, and cause-specific recovery.
- Amended: the frozen intent now forwards the already-validated caller Bearer and binds its canonical dual-principal subject to the envelope actor; pre-RTI ordering is ordinal source kind plus opaque ID. The plan now reuses `ProjectAuthorizationGate` with the supported detail loader, shares `ProjectCommandValidator` rules for persisted reads, and maps existing evidence causes to the closed recovery vocabulary.
- Known-bad state avoided: production `401`, workload actor substitution, tenant-only Project disclosure, corrupt metadata leakage or serialization failure, inapplicable recovery guidance, and an invented interim ordinal schema.
- KEEP: preserve every earlier repair, the disclosure-safe omission decision, bearer-only/no-overwrite forwarding, legacy route and read-model behavior, zero-write queries, current-empty Setup, shared AD-32 wire compatibility, operator-owned G-2/RTI/G-4 gates, and the untouched sprint-status file.

### 2026-09-20 — Human resolution: Story 6.3 completion scope
- Trigger: the frozen plan simultaneously prohibited inventing G-2/RTI contracts and required supported Refresh/RTI work for Story 6.3 completion.
- Amended: Story 6.3 completion now covers supported Get and Explain. Supported Refresh owner batches, RTI ingestion, and Refresh shadow equivalence are explicit follow-up work after approved G-2 and RTI artifacts exist; the additive Refresh query remains unregistered and legacy routing remains unchanged.
- Known-bad state avoided: fabricating owner-batch or RTI contracts, falsely marking deferred work complete, or blocking the verified Get/Explain slice on unavailable external artifacts.
- KEEP: preserve bearer forwarding, dual-principal binding, safe denial, metadata-only AD-32 output, zero-write Get/Explain, the pre-RTI ordering rule, legacy routing through Story 6.7, and the untouched sprint-status file.

## Review Triage Log

### 2026-08-24 — Review pass 1
- intent_gap: 0
- bad_spec: 12: (high 9, medium 3, low 0)
- patch: 0
- defer: 0
- reject: 4: (high 0, medium 2, low 2)
- addressed_findings:
  - `[high]` `[bad_spec]` Separated EventStore projection ingestion writes from the zero-write Get, Refresh, and Explain query path; required approved ingestion, backfill, tombstone, gap, duplicate, stale-delivery, and rebuild rules.
  - `[high]` `[bad_spec]` Partitioned pre-authority safe denial from post-authority required-evidence `Unavailable`, including zero/multiple Folder, corrupt model, and mixed-version cases.
  - `[high]` `[bad_spec]` Defined protected-candidate redaction, never-verified provenance, unknown-kind, duplicate-identity, and deterministic exclusion behavior.
  - `[high]` `[bad_spec]` Added File References to G-2 and prohibited persisted/reused positive actor authorization in favor of per-request envelope authorization.
  - `[high]` `[bad_spec]` Required one captured `asOf`, injected time, approved freshness boundaries, non-regressing owner versions, and coherence checks across owner batches.
  - `[high]` `[bad_spec]` Defined fail-closed behavior above 5,000 references and made batch, concurrency, timeout, retry, metadata-size, response-size, and cancellation bounds measurable from approved pins.
  - `[medium]` `[bad_spec]` Specified stable type/ordinal/identity ordering and duplicate rejection for included and excluded decisions.
  - `[high]` `[bad_spec]` Bound parsed query identity and fixed operation authority to the envelope, with safe denial on malformed, missing, mismatched, or revoked authority.
  - `[high]` `[bad_spec]` Added canonical handling for required versus optional faults, owner/store faults, partial batches, cancellation, and `QueryResult`/HTTP outcomes.
  - `[medium]` `[bad_spec]` Fixed Explain to the same persisted evidence and assembly decision set as Get at one request `asOf`, without live owner fan-out.
  - `[high]` `[bad_spec]` Constrained shadow normalization and the full safe-404 observable surface so comparison cannot hide authorization, freshness, omission, ordering, header, body, cache, log, or trace differences.
  - `[medium]` `[bad_spec]` Clarified that the operator supplies the approved manifest and added exact baseline/submodule preflight evidence before canonical verification.

### 2026-08-24 — Review pass 2
- intent_gap: 0
- bad_spec: 15: (high 10, medium 5, low 0)
- patch: 0
- defer: 0
- reject: 4: (high 0, medium 2, low 2)
- addressed_findings:
  - `[high]` `[bad_spec]` Clarified that Folders G-2 owns Folder and File Reference evidence and that all aggregate-response and freshness limits must be approved before implementation.
  - `[high]` `[bad_spec]` Distinguished immutable envelope-presented claims from server-owned expected action, audience, scope, target, and version values and required exact matching.
  - `[high]` `[bad_spec]` Restricted normal Get/Explain to exact-scope indexed authorization evidence and Refresh to counted actor-scoped G-2 owner batches.
  - `[high]` `[bad_spec]` Required final authority-version revalidation after persisted or owner reads on every operation.
  - `[medium]` `[bad_spec]` Defined `asOf` as the authoritative evidence cutoff and kept request observation time separate.
  - `[high]` `[bad_spec]` Limited shadow equivalence to frozen, commonly representable inputs and required known legacy truncation/File-refresh deficits to be exercised separately rather than normalized away.
  - `[medium]` `[bad_spec]` Defined Explain as current-at-request and required only same-snapshot test comparisons, removing any implied prior-Get correlation token.
  - `[medium]` `[bad_spec]` Distinguished forbidden durable selection traces from redacted operational telemetry and normalized nondeterministic telemetry fields for safe-denial comparison.
  - `[high]` `[bad_spec]` Named owner-change subscription/checkpoint and backfill artifacts and added tests for deletion, gap, out-of-order, duplicate, restart, and rebuild transitions.
  - `[high]` `[bad_spec]` Defined authoritative empty-index and aggregate-response-overflow outcomes and prohibited same-version/different-evidence acceptance.
  - `[medium]` `[bad_spec]` Made canonical persisted opaque identity, not ad hoc text normalization, the final deterministic sort key.
  - `[high]` `[bad_spec]` Limited opaque exclusion topology to Project-owned ordinals already authorized for disclosure.
  - `[high]` `[bad_spec]` Required canonical prerequisite artifact identifiers/hashes and a porcelain checkout allowlist before evidence acceptance.
  - `[high]` `[bad_spec]` Removed the integration-environment-blocker alternative once G-4 is accepted and extended bounds to Get and Explain.
  - `[medium]` `[bad_spec]` Split bundled operator prerequisites into independently completable imperative actions.

### 2026-08-24 — Review pass 3
- intent_gap: 0
- bad_spec: 14: (high 10, medium 4, low 0)
- patch: 0
- defer: 0
- reject: 5: (high 0, medium 2, low 3)
- addressed_findings:
  - `[high]` `[bad_spec]` Made all frontmatter operator actions a conjunctive expansion of the `Block If` gates, including performance/freshness pins, ingestion rules, hashes, and prerequisite-record availability.
  - `[high]` `[bad_spec]` Reasserted exact parsed-query-to-envelope-target equality and explained that envelope-derived authority means reading presented immutable claims before matching server-owned expectations.
  - `[high]` `[bad_spec]` Removed the invented per-actor fingerprint and required an approved bounded authorization-outcome applicability key, producer, expiry/retention, and deletion model before implementation.
  - `[high]` `[bad_spec]` Made last-verified and Project-ordinal disclosure conditional on approved Project-owned topology disclosure and mapped unavailable topology to required-evidence `Unavailable` after final authority revalidation.
  - `[medium]` `[bad_spec]` Unified unknown-kind handling as schema-invalid required trust evidence and defined an authoritative empty index through completeness/checkpoint markers.
  - `[high]` `[bad_spec]` Defined atomic checkpoint/index writes, fenced paged backfill, live/backfill ordering, resumable cancellation, and bounded tombstone retention/compaction/re-creation.
  - `[high]` `[bad_spec]` Required actual final-serializer byte measurement and G-4-pinned performance methodology rather than estimated payload/allocation bounds.
  - `[medium]` `[bad_spec]` Classified intentional optional policy exclusions as `Partial`, reserving `Complete` for no optional omission.
  - `[high]` `[bad_spec]` Added stale-Tenant policy to the independently tested legacy deficits and required final authority revalidation to precede all evidence-state responses.
  - `[medium]` `[bad_spec]` Expanded safe-denial equivalence to the accepted full response policy and bounded timing oracle while distinguishing semantics from nondeterministic telemetry identifiers.
  - `[high]` `[bad_spec]` Applied the no-selection-trace rule to retained/exported telemetry, not merely in-process lifetime.
  - `[high]` `[bad_spec]` Added a canonical prerequisite-record frontmatter reference populated by the operator and consumed by the named G-4 runner for executable hash matching.
  - `[high]` `[bad_spec]` Split checkout validation into pre-run clean implementation, post-run evidence allowlist, evidence commit, and final clean phases.
  - `[medium]` `[bad_spec]` Gave review passes stable numeric identities while retaining the workflow-required aggregate rejection counts.

### 2026-08-25 — Review pass 4
- intent_gap: 0
- bad_spec: 0
- patch: 6: (high 2, medium 4, low 0)
- defer: 0
- reject: 20: (high 0, medium 8, low 12)
- addressed_findings:
  - `[high]` `[patch]` Corrected batch-fault acceptance so required evidence fails `Unavailable` while optional candidate faults remain explicit `Partial` exclusions.
  - `[high]` `[patch]` Added zero-write Get coverage and authorization-outcome producer/applicability failure cases to the mandatory test task.
  - `[medium]` `[patch]` Added bounded live-change buffering/spill and safe-denial timing-equivalence to the operator-approved performance limits.
  - `[medium]` `[patch]` Required one schema-selected canonical opaque identity representation and comparer rather than allowing byte/string implementation choice.
  - `[medium]` `[patch]` Distinguished forbidden selection-trace identities from ordinary operational request/span identifiers.
  - `[medium]` `[patch]` Required the G-4 profile to prove prerequisite tampering fails closed and to enforce pre/post evidence checkout allowlists.

### 2026-09-06 — Review pass 5 (implementation)
- intent_gap: 0
- bad_spec: 0
- patch: 7: (high 1, medium 6, low 0)
- defer: 0
- reject: 28: (high 0, medium 8, low 20)
- findings:
  - `[false]` Get `Excluded` rows keep `ReasonCode: null` and have no last-verified field. Explain evaluations carry `ObservedAt`; `FailedCheck`/`Diagnostic` are the closed omission reason on the existing `ProjectContextExclusion` DTO. Adding last-verified would be public surface.
  - `[false]` `asOf` is `detail.UpdatedAt` (persisted evidence cutoff), not request wall-clock. Passing the same instant as assembly `Now` keeps freshness mapping on that cutoff.
  - `[false]` Live Get supplies `Conversations: []` and reports Complete without conversation rows. Frozen 4A: zero owner calls; missing owner-backed trust is Partial only when conversation candidates are supplied.
  - `[false]` Unregistered Refresh returns dispatcher 404, not `"safe-denial"`. An unimplemented query type must not impersonate a denied Project. Frozen skip of the Refresh handler.
  - `[false]` Omitted/empty envelope scopes and audience match on the Story 6.2 `/query` seam, where DualPrincipal fields are not populated. Exact policy matching applies when those collections are presented.
  - `[medium]` `[patch]` When scopes/audience are presented, ordinal sequence equality against `["projects.read"]` / `["projects"]` denies the documented EventStore DualPrincipal envelope (`projects.read`+`projects.list`, `hexalith-projects` plus extra `aud`).
  - `[false]` Query `ProjectId` uses the same non-whitespace parse as `Hexalith.Projects.Contracts.Identifiers.ProjectId`. The REST `IsCanonicalIdentifier` regex is a different HTTP path boundary.
  - `[false]` Store-fault `Unavailable` after allowed reauth uses `LastEventTimestamp` from the tenant projection (null timestamps are not Allowed) and `projectVersion: 0` meaning not disclosable.
  - `[false]` Conversation-start Complete still emits empty `recoveryActions`. Frozen: keep Conversation-start wire shape.
  - `[false]` Shared AD-32 CLR types replaced story-local names; JSON property names are unchanged and in-repo callers were updated.
  - `[medium]` `[patch]` Commit `de17e80` set Story 6.5 spec status to `in-progress` while it remains `blocked_by` 6.1–6.4 and sprint-status `backlog`.
  - `[false]` E6.3-A04 compares legacy `Assemble` vs supported `AssembleAdmission` on the frozen representable corpus. HTTP still uses legacy REST until Story 6.7; known deficits are not normalized.
  - `[low]` `ExecuteAsync_UnknownTenant_ReturnsSafeDenialWithoutReadingProjection` does not count `GetAsync`. Auth fails before the read; everyday callers do not hit a wrong read.
  - `[false]` Zero-write tests count `IReadModelStore` Save/TrySave on the only persisted write seam this path can reach. No command dispatcher exists on Get/Explain.
  - `[low]` Get and Explain share `ProjectContextQueryExecutor.ExecuteAsync`, so they share `asOf` and components by construction.
  - `[false]` `IsRequiredEvidenceStale` only maps Stale because Allowed diagnostic reads are Fresh or Stale. Unavailable/Future tenant outcomes never reach assembly as Allowed. Rebuilding/corrupt RTI signals are excluded until RTI exists.
  - `[medium]` `[patch]` Partial recovery is always `RefreshContext`, including Unauthorized optional files, which need `ContactAdministrator`.
  - `[low]` Duplicated `TryReadProjectId` is developer-only duplication with no caller divergence on the wire.
  - `[false]` `CountCandidates` Int32 overflow is unreachable under the 5,000 bound and in-memory collection sizes.
  - `[low]` `ExcludedSourceKinds` containing `ProjectFolder` is skipped because Folder is required evidence, not an optional kind lane.
  - `[false]` Unknown `ProjectContextSourceKind` in the map is unreachable; the enum is closed.
  - `[medium]` `[patch]` `ProjectContextShadowComparator.CompareGet` does not compare Setup, so current-empty vs populated Setup can still report a match.
  - `[false]` `CompareRefresh` is absent because Refresh is skipped until G-2.
  - `[low]` Delegated envelope with null `OriginalActorId` falls back to `UserId`. On the current `/query` seam the caller already supplies `UserId`; DualPrincipal always sets `OriginalActorId` from `sub`.
  - `[medium]` `[patch]` No handler test presents a mismatched or exact production `Audience` collection (verification-gap).
  - `[medium]` `[patch]` Handler fixtures use empty `MemoryReferences`; dropping memories in the executor would still pass those tests (verification-gap).
  - `[medium]` `[patch]` Post-read reauthorization deny and watermark mismatch are untested (verification-gap).
  - `[medium]` `[patch]` Malformed payload never exercises the `JsonException` catch (verification-gap).
- addressed_findings:
  - `[high]` `[patch]` Match presented scopes/audience as required policy values (`projects.read`, `hexalith-projects`) contained in the DualPrincipal lists; keep omitted collections compatible with the Story 6.2 `/query` envelope.
  - `[medium]` `[patch]` Map Partial Unauthorized omissions to `ContactAdministrator`.
  - `[medium]` `[patch]` Compare Setup in the shadow comparator, treating null and `ProjectSetup.Empty` as current-empty.
  - `[medium]` `[patch]` Restore the Story 6.5 spec to `ready-for-dev` without a borrowed 6.3 baseline.
  - `[medium]` `[patch]` Cover production DualPrincipal audience/scopes, persisted memories, reauth watermark mismatch, and malformed JSON payload on Get/Explain handlers.

### 2026-09-06 — Review pass 6
- intent_gap: 0
- bad_spec: 0
- patch: 4: (high 0, medium 4, low 0)
- defer: 0
- reject: 27: (high 0, medium 4, low 5)
- findings:
  - `[medium]` `[patch]` Story 6.5 spec is still `in-progress` with borrowed `baseline_revision` `5a37f9e4…` after pass 5 claimed to restore `ready-for-dev`.
  - `[false]` Acceptance/matrix still naming live Refresh is a spec-text mismatch with frozen Decision 4; the fix would edit this spec. Unregistered Refresh remains dispatcher 404 (`Query_RefreshProjectContext_IsNotRegisteredUntilG2`). [carried] Unregistered Refresh returns dispatcher 404, not `"safe-denial"`.
  - `[medium]` `[patch]` Missing-Folder `Unavailable` passes `setupCurrent: true` while the payload nulls `Setup`, so the Setup component reads current.
  - `[false]` [carried] Store-fault `Unavailable` after allowed reauth uses tenant `LastEventTimestamp` and `projectVersion: 0` meaning not disclosable. Lifecycle `Active` is the required-evidence placeholder on a path that already passed Tenant allow and never discloses Folder/Setup collections.
  - `[false]` Serialized-byte overflow waits on G-4 pins; this checkout enforces `ProjectContextReadLimits.MaxReferences` (5,000) only.
  - `[false]` Singleton `ProjectContextQueryExecutor` capturing transient `ProjectContextInclusionPolicy` does not change include/exclude decisions; the policy is pure except logger.
  - `[false]` [carried] Live Get supplies `Conversations: []` and reports Complete without conversation rows. Frozen 4A: missing owner-backed trust is Partial only when conversation candidates are supplied. Files/memories come from Project-owned persisted detail.
  - `[false]` [carried] E6.3-A04 compares legacy `Assemble` vs supported `AssembleAdmission` on the frozen representable corpus. HTTP stays legacy until Story 6.7.
  - `[false]` [carried] Conversation-start Complete still emits empty `recoveryActions`. Frozen: keep Conversation-start wire shape. Context adds a `References` component on the shared CLR type without changing Conversation-start JSON names.
  - `[false]` `AdmissionComponent.Reason` is the extracted Conversation-start unconstrained string; closing it would change the preserved wire.
  - `[false]` `IsDelegated`/`DelegationId`/`AuthenticatedWorkloadId` are unused, matching Story 6.2: Tenant allow uses `OriginalActorId ?? UserId`.
  - `[false]` Per-request Get has no envelope Version to compare; regressing Project watermarks are an RTI/G-4 concern.
  - `[false]` Handlers return `"safe-denial"` before `ToReadResponse`/`ToExplanation`; SafeDenial snapshots are not serialized on `/query`.
  - `[false]` Explain omits Get-covered executor branches (Partial/stale/fault/cancel/scopes) because Get and Explain share `ProjectContextQueryExecutor.ExecuteAsync`.
  - `[low]` Conversation-start still uses magic recovery strings instead of `AdmissionRecoveryAction` constants. Everyday callers see identical JSON; the fix would retouch the 6.2 handler.
  - `[false]` Composition tests use `AddProjectsServer` on the fake-then-swap seam, matching Story 6.2; runtime EventStore swap is not this story's host migration (DW-63).
  - `[false]` [carried] Omitted/empty envelope scopes and audience match on the Story 6.2 `/query` seam. `QueryEnvelope.Scopes`/`Audience` are nullable; DualPrincipal presents non-empty lists.
  - `[false]` New handlers reuse metadata-only `DisplayName` already covered by legacy leakage tests; no new E6-X01 channel.
  - `[false]` [carried] `CountCandidates` Int32 overflow is unreachable under the 5,000 bound and in-memory collection sizes.
  - `[false]` Pending folders are `ReferenceState.Pending` with null `FolderId`. `folderCurrent` requires `Included`, so pending is `Unavailable`, not Complete.
  - `[false]` `ProjectSetup.ExcludedSourceKinds` is a non-nullable list; current-empty uses `ProjectSetup.Empty`.
  - `[false]` [carried] `IsRequiredEvidenceStale` only maps Stale because Allowed diagnostic reads are Fresh or Stale.
  - `[false]` Post-read `AuthorizeDiagnosticReadAsync` throwing is an infrastructure failure; it does not serialize protected context.
  - `[low]` [carried] Delegated envelope with null `OriginalActorId` falls back to `UserId`. Unlikely on DualPrincipal `/query`.
  - `[false]` Acceptance requiring Refresh/Explain shadow match is a spec-text mismatch with frozen skip of Refresh; `CompareRefresh` is absent because Refresh is skipped until G-2. [carried]
  - `[medium]` `[patch]` Store-fault catch that denies on post-fault reauthorization is untested. `ExecuteAsync_StoreFaultAfterAuthority_ReturnsUnavailable` always seeds an allowed tenant store, so deleting the SafeDenial branch would still pass.
  - `[medium]` `[patch]` Store-fault `Unavailable` test asserts only `ResponseState` and null `Setup`, not `AsOf == ObservedAt`, `ProjectVersion == 0`, or `RefreshContext`/`ContactAdministrator` recovery.
- addressed_findings:
  - `[medium]` `[patch]` Restore the Story 6.5 spec to `ready-for-dev` and drop the borrowed 6.3 `baseline_revision`.
  - `[medium]` `[patch]` Pass `setupCurrent: false` on missing-Folder `Unavailable`.
  - `[medium]` `[patch]` Add store-fault + denied reauthorization coverage and pin store-fault snapshot provenance.

### 2026-09-20 — Review pass 7
- intent_gap: 2 groups / 3 findings (high 2, medium 1)
- bad_spec: 3 groups / 7 findings (high 5, medium 2)
- patch: 10 groups / 14 findings (high 4, medium 9, low 1)
- reject: 8 findings (false 7, low 1)
- findings:
  - `[high]` `[intent_gap]` The production `DaprDomainQueryInvoker` creates a fresh `/query` request without forwarding any bearer credential, while the changed Projects endpoint requires bearer authorization; normal supported dispatch therefore receives `401` before handler execution. The frozen authenticated-path intent does not choose a service-to-service credential propagation contract, and the user limited review/implementation to story-file diffs. [blind-hunter 1]
  - `[high]` `[patch]` `ProjectQueryEnvelopePrincipalBinding` accepts `subject == AuthenticatedWorkloadId` without binding the claimed original actor, so a workload token can substitute another tenant principal and the executor authorizes that substituted actor. Requiring the authenticated subject to equal the envelope actor, absent a separately verified delegation binding, is the smallest safe correction. [blind-hunter 2]
  - `[high]` `[bad_spec]` `ProjectContextQueryExecutor` checks tenant membership plus presented scope/audience but bypasses the existing Project ACL, EventStore authorization validator, and Dapr deny-by-default policy used by `ProjectAuthorizationGate`; any admitted tenant principal knowing an active Project ID can read it. The handler task copied the Story 6.2 tenant-only pattern without specifying how the supported handler obtains the complete server-side read authorization chain. [blind-hunter 3]
  - `[false]` A same-watermark tenant projection mutation does not produce the claimed authorization bypass through the current authorizer: removal of the current principal returns `Denied`, while role/content changes that remain `Allowed` are not consumed by this tenant-membership decision. The missing Project authorization chain is recorded separately. [blind-hunter 4]
  - `[false]` `[carried]` `asOf = detail.UpdatedAt` remains the persisted Project evidence cutoff, not request or authorization observation time; current authorization is independently revalidated before return. This is the same claim rejected in review pass 5 and the code still reads as logged. [blind-hunter 5]
  - `[false]` `[carried]` Serialized-byte overflow remains explicitly gated on absent G-4 pins; this checkout intentionally enforces only the 5,000-candidate bound. This is the same claim rejected in review pass 6 and the prerequisites remain absent. [blind-hunter 6]
  - `[medium]` `[patch]` Persisted Folder/File/Memory observations earlier than `ProjectDetailItem.CreatedAt` pass structural validation and can be reported current. Requiring `CreatedAt <= ObservedAt <= UpdatedAt` is a direct coherence correction. [blind-hunter 7]
  - `[high]` `[bad_spec]` Persisted Setup validation checks list presence and source-kind enums but omits the canonical text bounds/safety rules and `LinkedSourcePolicy`; corrupt state can leak forbidden metadata or make the closed enum converter throw during serialization. The spec requires fail-closed persisted validation but names no reusable canonical validation seam, so re-derivation must identify or extract one rather than duplicate domain rules. [blind-hunter 8]
  - `[medium]` `[patch]` A disclosure-safe `Partial` always marks the `References` component `Included=false` and `Unavailable`, even when current reference evidence is returned and only an intentional omission occurred. The component should describe established usable evidence while its reason records optional incompleteness. [blind-hunter 9]
  - `[medium]` `[bad_spec]` `Unavailable` selects the same recovery pair for every non-overflow cause and `Retry` for deterministic overflow, although Refresh is unregistered and neither refresh nor retry repairs every represented cause. The spec requires applicable recovery but does not define the cause-to-action mapping needed by the assembler. [blind-hunter 10]
  - `[medium]` `[intent_gap]` The frozen contract requires persisted Project ordinal ordering while also deferring the only approved ordinal/RTI schema; current contracts carry no ordinal and therefore sort by kind and identity. Human intent must clarify the pre-RTI ordering rule rather than invent an ordinal. [blind-hunter 11]
  - `[false]` `[carried]` The assembly-level shadow corpus is intentional until Story 6.7 keeps HTTP routing legacy; review pass 5 already rejected the claim that E6.3-A04 must dispatch both routes, and the code and routing decision remain unchanged. [blind-hunter 12]
  - `[false]` The comparator omits `ProjectVersion`, components, and recovery because legacy responses have no common fields for them; separate AD-32 contract tests cover those supported-only fields. Protected-target safe denial is verified by the composition safe-404 test rather than by comparing a legacy body to a nonexistent supported body. [blind-hunter 13]
  - `[medium]` `[patch]` The binder reimplements EventStore dual-principal extraction without its list/value bounds, JSON depth, and duplicate-actor handling, so the producer-normalized envelope can disagree with the same raw principal. Reusing `DualPrincipalClaimsHelper.Extract` is the smallest way to eliminate the drifting parser. [blind-hunter 14]
  - `[high]` `[patch]` The edge-case reviewer independently confirmed that accepting a workload-matching subject without actor binding permits tenant-principal impersonation. Require subject-to-actor equality unless a separately verified signed delegation contract exists. [edge-case-hunter 1]
  - `[medium]` `[patch]` The edge-case reviewer independently confirmed that unbounded raw claim comparison rejects valid EventStore envelopes after the producer's 64-entry/512-character normalization. Reuse the producer helper rather than duplicating its rules. [edge-case-hunter 2]
  - `[low]` `[reject]` Case-insensitive domain/query dispatch is the SDK dispatcher's deliberate matching behavior, and accepting a casing variant produces no demonstrated authorization or payload difference. Adding handler-specific casing guards would add branches for a negligible developer-only concern. [edge-case-hunter 3]
  - `[high]` `[patch]` Stable tenant evidence does not require `TenantAccessAuthorizationResult.TenantId` to equal the authoritative envelope tenant, unlike `ProjectAuthorizationGate`; a mismatched store result can authorize cross-tenant disclosure. Add the ordinal tenant equality check before any Project read. [edge-case-hunter 4]
  - `[medium]` `[bad_spec]` An undefined persisted `LinkedSourcePolicy` is not rejected and the closed JSON converter can throw instead of returning minimal `Unavailable`. This is one concrete member of the missing canonical persisted-validation group. [edge-case-hunter 5]
  - `[high]` `[bad_spec]` Persisted Setup text is not checked against canonical count, length, or safe-metadata rules before being returned. Corrupt projection content can therefore bypass the write boundary and leak forbidden metadata. [edge-case-hunter 6]
  - `[high]` `[bad_spec]` Persisted reference identifiers and display labels are not revalidated for bounds or safe metadata before supported output. Corrupt projection content can therefore disclose payload-like or path-like fragments despite the metadata-only contract. [edge-case-hunter 7]
  - `[medium]` `[patch]` The edge-case reviewer independently confirmed that pre-creation reference timestamps pass as current. Bound observations to the Project lifetime. [edge-case-hunter 8]
  - `[medium]` `[bad_spec]` The edge-case reviewer independently confirmed that recovery actions are not selected by failure cause, so callers receive inapplicable guidance. Re-derive a closed cause-to-action mapping in the spec. [edge-case-hunter 9]
  - `[high]` `[intent_gap]` The edge-case claims audit independently confirmed that production Dapr handler dispatch carries no bearer token into the newly authorized endpoint. The service-to-service authentication choice must be resolved before this story can safely require principal binding. [edge-case-hunter 10]
  - `[high]` `[patch]` `ApplyExcludedSourceKinds` runs before missing-owner-trust handling, so an excluded Conversation becomes disclosure-safe `Excluded` and returns `Partial` with its identity even though owner trust is absent. Evaluate missing trust first, or otherwise preserve the unconfirmed marker so the approved minimal `Unavailable` rule cannot be bypassed. [edge-case-hunter 11]
  - `[false]` `[carried]` The claim that the shadow gate must dispatch both live routes was already rejected in review pass 5: the frozen representable corpus compares legacy and supported assembly while routing remains legacy until Story 6.7. [edge-case-hunter 12]
  - `[false]` `[carried]` Refresh is intentionally unregistered until approved G-2 contracts exist; review pass 6 recorded the matrix wording mismatch and rejected implementation of a substitute handler. [edge-case-hunter 13]
  - `[medium]` `[patch]` No endpoint theory independently mismatches workload, delegation, scopes, audience, and tenant while preserving a valid actor, so removal of individual binding checks can escape verification. Add a composition theory covering each immutable identity field. [verification-gap 1]
  - `[low]` `[patch]` No Development composition test proves that the explicitly configured anonymous diagnostics bypass reaches `/query`; an unconditional authorization or principal-binding regression would break local diagnostics unnoticed. Add the direct Development-mode endpoint test. [verification-gap 2]
  - `[medium]` `[patch]` No persisted-read handler test proves that an included File belonging to a different Folder returns minimal `Unavailable`; deleting the executor's folder-coherence check would leave the suite green. Add the focused Get-handler case. [verification-gap 3]
  - `[medium]` `[patch]` No admission test pins an authorized archived optional reference to `Partial` plus `SelectAlternative`; regressions to `Unavailable` or the wrong recovery would pass. Add the focused archived-reference case. [verification-gap 4]
  - `[medium]` `[patch]` The verification reviewer independently confirmed that callback claim comparison does not reproduce EventStore's bounded normalization, causing valid producer envelopes to be safe-denied for large claim sets. Reuse the producer's extraction helper and cover its boundary. [verification-gap other 1]
- grouped_root_causes:
  - `[high]` `[intent_gap]` Production callback authentication contract: blind-hunter 1 + edge-case-hunter 10.
  - `[medium]` `[intent_gap]` Pre-RTI ordering contract: blind-hunter 11.
  - `[high]` `[bad_spec]` Complete supported Project-read authorization chain: blind-hunter 3.
  - `[high]` `[bad_spec]` Canonical persisted-detail validation seam: blind-hunter 8 + edge-case-hunter 5/6/7.
  - `[medium]` `[bad_spec]` Cause-specific recovery mapping: blind-hunter 10 + edge-case-hunter 9.
  - `[high]` `[patch]` Actor/workload binding: blind-hunter 2 + edge-case-hunter 1.
  - `[medium]` `[patch]` EventStore claim normalization reuse: blind-hunter 14 + edge-case-hunter 2 + verification-gap other 1.
  - `[medium]` `[patch]` Project-lifetime timestamp coherence: blind-hunter 7 + edge-case-hunter 8.
  - `[medium]` `[patch]` Partial component truth: blind-hunter 9.
  - `[high]` `[patch]` Tenant-result binding: edge-case-hunter 4.
  - `[high]` `[patch]` Missing-trust ordering: edge-case-hunter 11.
  - `[medium]` `[patch]` Identity-binding verification: verification-gap 1.
  - `[low]` `[patch]` Development bypass verification: verification-gap 2.
  - `[medium]` `[patch]` File/Folder coherence verification: verification-gap 3.
  - `[medium]` `[patch]` Archived omission verification: verification-gap 4.

### 2026-09-20 — Review pass 8
- high: 2
- medium: 17
- low: 1
- false: 3
- findings:
  - `[false]` `[reject]` The baseline-to-current diff contains unrelated committed history, but it is not a proposed Story 6.3 commit or working-tree bundle: the workflow preserved the story's original baseline and explicitly requires reviewing all later changes from it. No split or history rewrite is authorized. [blind-hunter 1]
  - `[high]` `[patch]` `ProjectQueryEnvelopePrincipalBinding` validates token `sub` against the envelope but can return an accessor whose `PrincipalId` prefers a conflicting `ClaimTypes.NameIdentifier`; the authorization gate then evaluates a different principal. Require the accessor principal to equal the validated subject and add the conflicting-claim case. [blind-hunter 2]
  - `[high]` `[patch]` Final authorization reuses the initially loaded `ProjectDetailItem`, so an archive or version change between the two passes can return stale context. Reload supported detail on the final pass and fail closed unless identity, lifecycle, and sequence remain stable. [blind-hunter 3]
  - `[medium]` `[patch]` The 5,000-candidate bound is checked only after persisted validation iterates every File and Memory. Reject an oversized model before per-item validation while retaining both authorization passes. [blind-hunter 4]
  - `[medium]` `[patch]` Persisted optional metadata validates trimmed length but emits the original value, allowing an arbitrarily padded label past the declared bound. Require persisted metadata to be raw-bounded and already canonical. [blind-hunter 5]
  - `[medium]` `[patch]` Persisted reference identifiers validate only their trimmed form while the original value is retained, allowing padded, noncanonical, over-limit identities into responses. Require exact canonical persisted identifiers. [blind-hunter 6]
  - `[low]` `[patch]` Persisted Setup validation proves a trimmed canonical copy can be produced but returns the original lists, so bounded whitespace-padded values can cross the supported boundary. Require persisted Setup to equal its canonical snapshot. [blind-hunter 7]
  - `[medium]` `[patch]` The overflow/duplicate fast path marks authorization non-current even after successful authorization, producing a false stale authorization component. Preserve established authorization truth on this post-authority failure. [blind-hunter 8]
  - `[medium]` `[patch]` Every unavailable admission labels References as `optional-omission`; required-evidence failure, overflow, duplicates, and corruption need bounded cause-appropriate component reasons. [blind-hunter 9]
  - `[medium]` `[patch]` Shadow comparison ignores included-reference display name, reason code, and observation time, allowing visible Get divergence to pass. Compare every common reference field. [blind-hunter 10]
  - `[medium]` `[patch]` Shadow comparison ignores exclusion reason code and diagnostic, allowing changed omission explanations to pass. Compare every common exclusion field. [blind-hunter 11]
  - `[medium]` `[patch]` Shadow comparison ignores evaluation reason code, diagnostic, and observation time, allowing changed Explain evidence to pass. Compare every common evaluation field. [blind-hunter 12]
  - `[medium]` `[patch]` `CompareGet` omits legacy `ObservedAt` versus supported `Snapshot.AsOf`, so different evidence cutoffs can compare equal. Include the common cutoff in parity. [blind-hunter 13]
  - `[false]` `[reject]` Absolute `source_spec` values in the deferred ledger follow the workflow's required `{spec_file}` format and predate this story; they are intentionally provenance identifiers, not portable Markdown links. [blind-hunter 14]
  - `[false]` `[reject]` Although `AuthorizeSupportedReadAsync` itself does not compare the supplied detail's Project ID, its only production caller rejects `detail.ProjectId != projectId` before returning or assembling content. The claimed wrong-Project disclosure does not occur. [edge-case-hunter 1]
  - `[medium]` `[patch]` A duplicate candidate below the limit takes the boolean overload's missing/stale cause and recommends `RefreshContext` instead of the specified corruption action `ContactAdministrator`. Select the corruption cause explicitly. [edge-case-hunter 2]
  - `[medium]` `[patch]` The persisted metadata helper's trimmed bound does not enforce the claimed minimal-Unavailable behavior for raw-long padded labels; this is the same permissive persisted-validation root cause as blind-hunter 5. [edge-case-hunter 3]
  - `[medium]` `[patch]` Required failures are mislabeled as `optional-omission` in the References component; this is the same unavailable-component root cause as blind-hunter 9. [edge-case-hunter 4]
  - `[medium]` `[patch]` The comparator can pass observable common-field divergence in labels, reasons, diagnostics, timestamps, and evidence cutoff. The claim about supported-only version/components/recovery remains carried false from pass 7 because legacy has no fields to compare. [edge-case-hunter 5]
  - `[medium]` `[patch]` No test pins Pending Folder to minimal `Unavailable` plus exactly `PollTask`; the verification-gap evidence demonstrates both mapping branches can regress while the suite stays green. [verification-gap 1]
  - `[medium]` `[patch]` No supported handler test proves unsafe persisted File or Memory display metadata is rejected without leakage; the verification-gap evidence demonstrates removal of that validator branch stays green. [verification-gap 2]
  - `[medium]` `[patch]` Stale-Tenant tests do not assert the `FirstResponseAuthorization` component, so it can regress from stale/excluded to current/included without failure. [verification-gap 3]
  - `[medium]` `[defer]` The unrelated release exact-green-source preflight has source-text checks but no hermetic execution coverage for green SHA, stale main, malformed API data, or missing successful push CI. This belongs to the release workflow, not Story 6.3. [verification-gap 4]
- grouped_root_causes:
  - `[high]` `[patch]` Bind authorization principal to validated token subject: blind-hunter 2.
  - `[high]` `[patch]` Reload and compare Project detail during final authorization: blind-hunter 3.
  - `[medium]` `[patch]` Enforce the candidate limit before per-item persisted validation: blind-hunter 4.
  - `[medium]` `[patch]` Require canonical persisted metadata, identifiers, and Setup, with supported-boundary leakage coverage: blind-hunter 5/6/7 + edge-case-hunter 3 + verification-gap 2.
  - `[medium]` `[patch]` Correct duplicate/overflow cause and established-authorization reporting: blind-hunter 8 + edge-case-hunter 2.
  - `[medium]` `[patch]` Give unavailable References a truthful bounded reason: blind-hunter 9 + edge-case-hunter 4.
  - `[medium]` `[patch]` Compare all common Get/Explain shadow fields and cutoff: blind-hunter 10/11/12/13 + edge-case-hunter 5.
  - `[medium]` `[patch]` Add Pending Folder recovery coverage: verification-gap 1.
  - `[medium]` `[patch]` Pin stale authorization component truth: verification-gap 3.
  - `[medium]` `[defer]` Hermetically test the unrelated release exact-source preflight: verification-gap 4.

## Design Notes

Get, Refresh, and Explain stay one story: one authority boundary, one allowlist, one snapshot contract, one fixture corpus, and a zero-write query invariant. Get and Explain read persisted current evidence only. Refresh substitutes a bounded owner-batch snapshot without updating the index. Explain returns the same assembly evaluations with no persisted trace identity.

### Replan facts (2026-09-20)

- Intent gaps: none. The human selected original-Bearer callback propagation with subject-to-actor binding and deterministic pre-RTI `(source kind, opaque ID)` ordering.
- Irreversibles: none. There is no migration, persisted-data rewrite, dependency update, deployment, external call, or configuration trigger.
- Footprint: Projects authorization/query/context/domain-validation code and focused tests, plus one EventStore query invoker and its focused tests. No generated files, public query payloads, RTI/G-2 schema, legacy route, submodule commit/pointer, or sprint status changes.

Checkout facts (2026-09-06): Story 6.1 list/open handlers did not land. Story 6.2 did land a supported `/query` handler, incremental Conversation-start projection, fake-then-swap `IReadModelStore`, and story-local AD-32 types in `ConversationStartSetupResponse.cs`; null Setup becomes `ConversationStartSetup.Empty` and still reports Setup current; `Partial` is never produced (DW-66) and Folder Unauthorized collapses to Unavailable (DW-67). No Conversations/Folders/Memories G-2 batch-read contracts, no Reference Trust Index types, no `IReadModelBatchStore` usage, and no `module/hexalith-projects.module.json` exist in this checkout. `QueryEnvelope` has no Action/Target/Version members. SDK host migration remains DW-63.

Agent decisions (not user-visible): follow the Story 6.2 host and DI pattern; use `Seed` plus `Apply` rather than slice `Rebuild`; keep `"safe-denial"` on the hand-rolled `/query` rather than `SafeDenialQueryRouter`; put new handlers next to `GetConversationStartSetupQueryHandler`; one C# type per new file; extract shared snapshot types under `src/Hexalith.Projects.Contracts/` without changing Conversation-start JSON; do not edit `sprint-status.yaml`. Operator accepted the full-spec size (review logs retained).

### Post-gate implementation decisions

"Derive from `QueryEnvelope`" means read immutable presented caller context and validate it against server-owned expectations. It never lets the caller choose expected action, audience, scope, target, or version policy. Qualifying G-4 evidence still requires an operator `prerequisite_record`; focused tests are not that evidence.

| Boundary | Required decision |
|---|---|
| Write ownership | Only asynchronous projection ingestion may write the approved read model and its partition checkpoint, atomically through the supported batch store under the accepted schema. Get, Refresh, and Explain perform no EventStore command, domain event, read-model/checkpoint write/delete, task, audit, repair, sibling mutation, or selection-diagnostic persistence. Zero-write assertions bracket query dispatch separately from projection-delivery tests. |
| Callback authentication | EventStore copies a non-empty inbound Bearer credential to the Dapr handler-query request only when the outbound request has none. Projects requires authentication outside the existing Development diagnostics bypass, extracts identity with `DualPrincipalClaimsHelper`, requires token subject = envelope original actor, and matches normalized Tenant/workload/delegation/scopes/audience. Never put the token in `SubmitQuery`, `QueryEnvelope`, logs, or persistence. |
| Authority and target | Treat Tenant, original actor, workload, delegation, presented scopes/audience, and target in `QueryEnvelope` as immutable caller context, never expected policy. Canonically parse the query Project identity and require exact value equality with the envelope target. Resolve fixed expected action/audience/scope server-side. Run the existing `ProjectAuthorizationGate` order—JWT/principal, claim transform, tenant freshness, supported Project ACL/detail, EventStore validator, Dapr deny-default—and use its returned detail/evidence; do not call the legacy detail store. Missing, malformed, mismatched, denied, cross-Tenant, Archived, or not-yet-authorized existence returns canonical safe `404`. Revalidate the complete authority after persisted/owner reads and before returning content. |
| Persisted validation | Validate Project structure, positive sequence, lifecycle, `CreatedAt <= ObservedAt <= UpdatedAt`, exact Folder ownership, setup enums/text bounds, reference identifiers, and display metadata through one pure seam shared with `ProjectCommandValidator`. Corrupt data is minimal `Unavailable`; no raw value appears in errors. |
| Required evidence | After Project authority is established, missing, stale, rebuilding, corrupt, version-incoherent, zero-Folder, or multiple-Folder Project/Folder/authorization evidence yields canonical `Unavailable`. Null Setup is current-empty, not missing. Until RTI exists, missing owner-backed trust yields minimal `Unavailable` whenever an omission would disclose an unauthorized candidate identity. After RTI is accepted, missing or non-current trust-index evidence is required `Unavailable`. Never label mixed or regressed evidence `Complete`. |
| Candidate disclosure | Emit identity/type metadata only for a Project-linked candidate whose sibling authority is confirmed for the current envelope. For denied or unconfirmed candidates, emit one deterministic opaque exclusion per Project ordinal only after the Projects-owned link topology and that ordinal are authorized for disclosure. Unknown kinds and duplicate identity/ordinal make required trust evidence `Unavailable`. |
| Authorization persistence | The Tenant-scoped index may retain only approved non-payload owner provenance and authorization outcome. Get/Explain make zero owner calls. Refresh obtains current actor-scoped outcomes only through counted G-2 owner batches but remains zero-write. Do not invent per-actor fingerprints. |
| Snapshot and freshness | Derive one authoritative evidence cutoff `asOf` from coherent Project/index or owner watermarks; never substitute local request-observation time. Reject regressing watermarks and same-version/different-evidence input. |
| Bounds and owner batches | Reject more than 5,000 candidates or an over-limit serialized response as canonical minimal `Unavailable`; never truncate. Get/Explain issue zero owner calls. Match owner responses by opaque identity, never array position. |
| Determinism | `ProjectContextInclusionOrder` remains the check sequence, not a kind ranking. Before RTI, order response rows with ordinal string comparison on `(source kind, opaque ID)`. After RTI is approved, use persisted Project ordinal then the schema's opaque identity comparer. Reject conflicting duplicates and add no interim ordinal. |
| Explain | Use the same persisted snapshot and assembly policy as Get at one evidence `asOf`. Do not live-refresh owners. Do not require a prior-Get token. |
| Shadow corpus | Compare only the frozen representable intersection (legacy 100-Conversation page, identical File evidence, no stale-Tenant allowance). Exercise Conversation overflow, changed File evidence, and stale-Tenant as known legacy deficits; never normalize them away. |
| Telemetry | No selection-trace identities or payload-bearing diagnostics. Ordinary request/span IDs are allowed through existing redaction. |
| Index completeness | After RTI exists, zero candidates are authoritative `Complete` empty only when completeness/checkpoint/watermark/rebuild markers prove a fully initialized index; otherwise required trust evidence is `Unavailable`. Until RTI exists, assemble from Project-owned persisted detail and the allowlist; do not invent RTI completeness markers. |
| Failure mapping | Before authority, only canonical safe denial. After authority: required or disclosure-unsafe failure is minimal `Unavailable`; a disclosure-safe optional omission is explicit `Partial`; cancellation propagates. Deduplicate actions in `AdmissionRecoveryAction.Values` order: `Pending` → `PollTask`; `Stale`/`Unavailable` → `RefreshContext`; `Archived`/`Ambiguous` → `SelectAlternative`; `Conflict`/`InvalidReference` → `ResolveNeedsAttention`; `Unauthorized`/`TenantMismatch` → `ContactAdministrator`; intentional `Excluded` → no action. Identity-free store fault → `Retry`; missing/stale required context → `RefreshContext`; materialization in progress → `PollTask`; overflow/duplicate/corruption/authorization uncertainty → `ContactAdministrator`; emit `None` only when no action is required. |
| Verification provenance | Qualifying evidence needs an operator `prerequisite_record` and G-4 runner. Focused unit/server tests are not that evidence. |

## Verification

**Commands:**
- `python3 tools/planning/validate_production_authority.py --story-id 6.3` -- expected: Story 6.3 remains within production authority.
- `dotnet restore Hexalith.Projects.slnx` -- expected: restore succeeds with central dependency ownership.
- `dotnet build Hexalith.Projects.slnx --configuration Debug` -- expected: zero warnings and errors.
- `dotnet test tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj --configuration Debug` -- expected: additive supported and legacy contracts pass.
- `dotnet test tests/Hexalith.Projects.Tests/Hexalith.Projects.Tests.csproj --configuration Debug` -- expected: policy, projection, handler, replay, leakage, and zero-write cases pass.
- `dotnet test tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Debug` -- expected: supported composition, safe denial, shadow, and legacy regressions pass.
- `git diff --check` -- expected: no whitespace errors.

**Manual checks (if no CLI):**
- Confirm `sprint-status.yaml` has no diff from this story.
- Do not treat `dotnet tool run hexalith-module test --profile reads --filter Story=6.3` as runnable until the operator supplies `module/hexalith-projects.module.json` and the G-4 reads profile.
