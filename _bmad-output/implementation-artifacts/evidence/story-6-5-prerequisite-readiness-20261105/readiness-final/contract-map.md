# Delivered Story 6.5 planning inputs — 2026-11-06

This is a source-based planning map, not new feature implementation or acceptance. Exact inspected/tested source hashes and canonical revisions are in [source-inputs.json](source-inputs.json). Repository-relative paths below refer to that checkout.

## Authentication and transport

`src/Hexalith.Projects.UI/Program.cs`, `ProjectsUiSecurity.cs`, `Components/Routes.razor` and `Components/Shared/RedirectToChallenge.razor` already compose configured OIDC, the FrontComposer cookie, Server authentication state, challenge flow and generated REST-client relay. `ProjectsUiSecurity` uses stable `sub` and `eventstore:current-tenant`. Wholly absent configuration deliberately permits local auth-disabled startup; partial configuration fails closed. Future supported read journeys require the authenticated configuration and must not treat the local fallback as an authenticated path.

FrontComposer retains ownership of server context, the immutable session/subject/Tenant credential store and relay. The fixture migration seeds only synthetic owner state. Projects must not introduce a token cache, capture provider or subject-only fallback.

Future SDK registration uses `AddEventStoreGatewayClient()` in `references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Registration/EventStoreServiceCollectionExtensions.cs`, with `AddFrontComposerGatewayAuthorization()` from `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerTokenRelayServiceExtensions.cs` attached to that returned HTTP builder. Use `IEventStoreGatewayClient.SubmitQueryAsync<T>(SubmitQueryRequest, ifNoneMatch, cancellationToken)` from `Gateway/IEventStoreGatewayClient.cs` and `EventStore.Contracts/Queries/SubmitQueryRequest.cs`; handle the SDK result/failure wrapper rather than bypassing it with a local transport.

`src/Hexalith.Projects.UI/Hexalith.Projects.UI.csproj` currently has no explicit EventStore.Client reference. The future adapter must add the existing SDK client using the repository's Debug/source and CI/package reference conventions and existing central versions, without assuming Projects.Client supplies that reference. No new library API, query endpoint, generated client edit or dependency upgrade is needed. `src/Hexalith.Projects.AppHost/Program.cs` already injects OIDC; the remaining topology change is the UI's EventStore gateway endpoint/reference. It currently references the Projects REST service. This is future Story 6.5 work.

## Independent response authority

All Project DTO paths below start at `src/Hexalith.Projects.Contracts/Queries/`.

| Query / response | Delivered meaning the UI must preserve |
| --- | --- |
| `ListProjectsQuery` → `ProjectListPage` | `Items`, opaque `Cursor`, `HasMore`, enumeration `Snapshot`; default 50/max 200, ordinal Project ID ordering, lifecycle/descriptive-inspection/quarantine selectors. Successful current enumeration can be Complete while individual rows are Unavailable. Preserve authenticated filter/cursor scope; changing Tenant or view resets continuation. |
| `ProjectListRow` | `ProjectId`, optional authorized `Name`, `LifecycleState`, `ProjectFolderAvailability`, its own `Snapshot`. Never substitute the page snapshot for row evidence. |
| `GetProjectQuery` → `GetProjectResponse` | Target Project comes from request envelope, not an invented query ID property. Response owns its `Setup`, `SetupMetadata`, `References` and `Snapshot`; descriptive fields require separate authorized/audited inspection. |
| `GetProjectContextQuery(ProjectId)` → `ProjectContextReadResponse` | Independent `Snapshot`, lifecycle, Setup, ProjectFolder, Conversations, FileReferences, MemoryReferences and explicit `Excluded` references. Open/list observations do not grant context or first-response authority. |
| `GetConversationStartSetupQuery(ProjectId)` → `ConversationStartSetupResponse` | Its own `ConversationStartAdmissionSnapshot` and component CLR types. Preserve the delivered state; `FromShared` intentionally maps shared Partial to Complete and filters components. Do not reinterpret it using context state. |
| `ResolveProjectFromConversationQuery` / `ResolveProjectFromAttachmentsQuery` → `ProjectAdvisoryResolutionResponse` | Independent advisory `Result?`, `Candidates`, `EvaluatedAt`, `Incomplete`, `UnsupportedInputKinds`. There is no AD-32 admission snapshot in this response. |

Shared `AdmissionSnapshot` owns `ResponseState` (Complete/Partial/Unavailable/Denied), `AsOf`, disclosable `ProjectVersion`, `Components` and `RecoveryActions`. `AdmissionComponent` owns `Name`, `Included`, `Freshness`, bounded `Reason`, optional `LastVerifiedAt`. Present each delivered observation/version separately; no atomic composite, copied admission, inferred timestamps or version reconciliation. Safe absence hides protected target metadata and distinctions. Archived recovery inspection never grants context admission. Keep accepted unavailable/owner-trust limitations and unregistered Refresh honest; re-read through supported Get operations without inventing a Refresh capability.

List/open/advisory DTOs expose `QueryType`. Context/start discriminators are declared in `src/Hexalith.Projects.Server/ProjectsServerModule.cs`: `Hexalith.Projects.Queries.GetProjectContext.v1` and `Hexalith.Projects.Queries.GetConversationStartSetup.v1`. Reference the existing wire names without introducing UI→Server compilation dependencies. Domain is `projects`; list/advisory aggregate target is `projects`, while named reads target their exact Project. Construct scope from owner server context and route identifiers; browser inputs never become identity or Tenant authority.

## Advisory limits

`ProjectAdvisoryCandidate` contains only `ProjectId`, `ReasonCodes`, `Rank` and `Score`. Do not invent candidate names, current components, context/setup or protected exclusions. `EvaluatedAt` is computation time, not a source watermark or currentness certificate. Lag alone is not incompleteness. Empty incomplete observations have `Result = null`; incomplete output cannot imply exhaustive NoMatch. SingleCandidate remains an unselected observation, never exclusive/resolved or an admission decision.

Attachments use opaque `ProjectAdvisoryAttachment(ReferenceKind, ReferenceId)`. Preserve the server maximum of 32 raw entries before deduplication; the existing UI's pre-deduplication must not bypass this bound. Advisory requests reject paging. Current File identity mapping is unsupported: retain `Incomplete=true` and `UnsupportedInputKinds=["file"]` when presented. Do not guess File mappings or claim full File acceptance.

## Concrete future edit map

- `src/Hexalith.Projects.UI/Diagnostics/ProjectInventorySource.cs`, `IProjectInventorySource.cs`, `ProjectInventoryLoadResult.cs`: consume supported paged list/row DTOs and expose opaque continuation plus independent snapshots.
- `Diagnostics/ProjectWarningsDashboardSource.cs`, `ProjectWarningsDashboardMapper.cs`, `ProjectWarningsDashboardLoadResult.cs`: adapt the existing Home consumer without per-row diagnostic/audit N+1 calls or fabricated warnings.
- `Diagnostics/ProjectDetailSource.cs`, `IProjectDetailSource.cs`, `ProjectDetailLoadResult.cs`, `ProjectReferenceHealthMapper.cs`: replace legacy project/diagnostic-audit/context-explanation/conversations fan-out with separately labelled open/context/start results. Retain honest unavailable sections.
- `Diagnostics/ProjectResolutionTraceSource.cs`, `ProjectResolutionTraceMapper.cs`, `ProjectResolutionTraceLoadResult.cs`: use advisory DTOs and safe unselected semantics. Existing request validation may be retained if it preserves raw bounds.
- `Components/Pages/Home.razor`, `Components/Pages/ProjectDiagnostics.razor`, `Components/Shared/ProjectResolutionTraceWorkbench.razor`, `Contracts/Ui` descriptors and existing `Rendering/` helpers: render safe metadata, per-operation admission, omissions/recovery and advisory coverage; remove maintenance controls and audit requests from the supported journeys. Keep legacy routing available for Story 6.7 comparison; no cutover or deletion here. `Contracts/Ui` means `src/Hexalith.Projects.Contracts/Ui/`; the later 6.8 split is not assumed delivered.
- `tests/Hexalith.Projects.UI.Tests/Diagnostics/ProjectInventorySourceTests.cs`, `ProjectWarningsDashboardSourceTests.cs`, `ProjectDetailSourceTests.cs`, `ProjectResolutionTraceSourceTests.cs`: exercise typed query/result mapping, cancellation/fault/safe-absence handling and no forbidden calls.
- `tests/Hexalith.Projects.UI.Tests/Components/ProjectInventoryPageTests.cs`, `ProjectDetailPageTests.cs`, `ProjectResponsiveAccessibilityContractTests.cs`, existing `Authentication/ProjectsUiAuthenticationCompositionTests.cs`: verify render/auth composition without claiming browser acceptance from selector checks.
- `tests/Hexalith.Projects.Integration.Tests/AspireTopologyTests.cs`, `tests/e2e/specs/projects-authentication.spec.ts`, `projects-inventory-detail.spec.ts`, `projects-resolution-trace.spec.ts`, `projects-accessibility.spec.ts`: extend actual authenticated supported-handler journeys, two-circuit isolation, persisted zero-domain-write proof and accessible interaction. Managed browser entry remains `npm --prefix tests/e2e run test:live:managed`; inspect its current fixture/lifecycle instructions before executing in a separate implementation request.

No unresolved product decision was found. Missing feature journeys are implementation/acceptance tasks, not new prerequisite delivery gates. No common-runner/package/publication/production-enrollment requirement is added to DW-1.
