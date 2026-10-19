# Inspected contracts and ownership

Inspection date: 2026-10-19. Paths below are repository-relative; sibling paths refer only to root-declared checkouts. All five repositories were clean on `main` at inspection. No gitlink, package, code or tracking file was changed.

| Repository | Inspected HEAD | Relevant observed contract / gap |
| --- | --- | --- |
| Hexalith.Projects | `78dc352c5ae0bb21773ea1831275e598d55a12b2` | Owns resolution policy, Project associations, supported read models and RTI. No `ReferenceTrust*` production types exist. |
| Hexalith.Conversations | `1f3891274eba5cae0754987ee79b4ffc87944401` | Owns Conversation existence, metadata and Conversation-to-Project membership. No dedicated metadata-only owner resolution read. |
| Hexalith.Folders | `efb98b1e5215ad73b4591f2a2a8ff00f8b8c794a` | Owns Folder/File existence, lifecycle and authorization. Current exact Folder authorization query lacks batch/file/revision evidence. |
| Hexalith.EventStore | `1eaabca4f8d3597a3a1f9fadf341be5cb9a3a8c0` | Owns signed query transport, verified identity, dispatch, storage and safe-denial routing. Owner dispatch loses part of originating identity. |
| Hexalith.Builds | `b435f2e3b3892e03f69322d24de6003b0baf7a7d` | Owns centralized package versions, not owner authorization or RTI policy. Any future package-catalog update is separate work. |

`global.json` currently pins SDK `10.0.402`. The Builds catalog currently names EventStore `3.126.0`, Conversations `1.2.0`, Folders `1.3.0`. The frozen Story 6.4 record of prior P1R `3.125.1` enrollment/binding does not qualify this current catalog. Older version statements in adopted context are historical; no new qualification is inferred here.

## Gaps and supported reuse

| Gap | Owning repository and source anchors | Required delta |
| --- | --- | --- |
| C-1: dedicated bounded Conversation metadata | Conversations: `Contracts/Queries/ConversationDetailsV1.cs`, `Server/Queries/ConversationDomainQueryHandler.cs`, `Server/Queries/ConversationQueryHandler.cs` under `src/Hexalith.Conversations.*` | New narrow owner query; do not call `conversation-detail` and redact afterward. `ConversationDetailsV1` includes messages, evidence and reference hydration. |
| C-2: complete caller/resource authority and freshness | Conversations: `Server/Projections/ConversationProjectionReadService.cs`, `IConversationProjectionReadStore.cs`, `Server/TenantAccess/ConversationTenantAccessService.cs`, `Contracts/Projections/ProjectionFreshnessV1.cs` | Reuse Tenant access and completed-generation checks; add verified workload/delegation/action checks and a metadata-only storage read. The existing detail service reads summary/detail after Tenant/principal access; that is not proof of the new full owner contract. |
| F-1: revision-bearing Folder/File batches | Folders: `src/Hexalith.Folders.Contracts/FolderOwnerRead{Query,Result,Outcome}.cs`, `src/Hexalith.Folders.Server/Queries/FolderOwnerReadQueryHandler.cs` | Reuse exact `read_metadata` layered authorization and `safe-denial`; add an independent additive batch contract. Existing `AuthorizeFolderRead.v1` returns only `Allowed|Stale|Unavailable`; preserve it. |
| F-2 / P-1: opaque file ownership binding | Projects: `src/Hexalith.Projects.Contracts/Models/ProjectFileReference.cs`, `src/Hexalith.Projects.Server/Folders/FoldersProjectFileReferenceDirectory.cs`; Folders: `src/Hexalith.Folders/Queries/FileContext/WorkspaceFileContextQuery*.cs` | `FileReferenceId` is Projects-owned. Its recheck explicitly returns `Unavailable`: no opaque owner lookup exists. Current file-context reads require workspace/path and may carry content. An owner-issued identity and provenance are prerequisite work; a Projects ID cannot be silently relabeled as a Folders ID. |
| E-1: original caller preserved at owner | EventStore: `src/Hexalith.EventStore/Controllers/QueriesController.cs::SubmitBoundOwnerAuthorityAsync`, `Contracts/Queries/BoundOwnerQueryRequest.cs`, `QueryCallerAuthority.cs`, `DomainService/DelegatedOwnerQueryClient.cs`, `VerifiedQueryCaller.cs` | Current route verifies signed original Tenant/actor/permissions, then creates `SubmitQuery` with requesting workload and `IsDelegated=true`, omitting original scopes/audience/delegation ID. Extend the existing contract; retain original and hop authority distinctly. |
| E-2: read-only platform evidence | EventStore: `Client/Projections/IReadModelStore.cs`, `IReadModelBulkStore.cs`, `DaprReadModelStore.cs`, `ReadModelBatchProtocol.cs::ResolveVisibleAsync` | Existing single/bulk reads and marker visibility can be reused. `ResolveVisibleAsync` reads marker/value without repair. Prove zero writes through the actual owner/store chain; do not add another store runtime. |
| P-2: actor-scoped RTI and complete associations | Projects: `src/Hexalith.Projects.Server/Projections/ProjectReads/ProjectRead{Model,Index}.cs`, `Queries/ProjectContextQueryExecutor.cs`, `Queries/ProjectReadQueryExecutor.cs` | Project reads exist; context explicitly uses `ownerBackedTrustAvailable:false`. Add a separately qualified RTI producer/read contract and bounded association/catalog projection. List/get executors can write inspection or denial audit and cannot be called as an internal zero-write candidate source. |
| P-3: safe mapper inputs | Projects: `src/Hexalith.Projects/Resolution/{Conversation,Attachment}ResolutionEvidenceMapper.cs`, `ProjectResolutionEngine.cs` | Reuse without edits. Attachment mapper selects presented IDs regardless of the presented row's trust state, then scores candidate-side reference states. The prerequisite adapter must remove unverified joins and set candidate-side states correctly; filtering presented inputs alone is insufficient. |

Prefixes such as `Contracts/` in this table abbreviate `src/Hexalith.<Module>.Contracts/`; `Server/` and `Client/` follow the same convention.

## Frozen and historical boundaries

The adopted `spec-6-4-resolve-projects-with-transient-current-explanations.md` includes the 2026-10-19 no-name/no-inspection decision and is the current resolution intent. The older implementation story supplies gate rationale; its historical names, versions or display-field suggestions do not override the frozen spec.

`IProjectReferenceIndexReadModel`, `DaprProjectReferenceIndexReadModel`, legacy resolution endpoints, legacy journal and default routes remain unchanged. Their rows prove neither current Folders authority nor actor visibility. Internal mapper-compatible candidate DTOs may be populated from the new supported evidence; that does not authorize use of the legacy store.

G-4 remains separately owned platform runner delivery under AD-25. `.config/dotnet-tools.json` and `module/hexalith-projects.module.json` are absent at this baseline. No Story 6.4 persisted-boundary TRX/JSON can be claimed from this inspection.
