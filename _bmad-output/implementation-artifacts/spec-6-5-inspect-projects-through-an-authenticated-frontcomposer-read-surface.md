---
title: 'Inspect Projects through an authenticated FrontComposer read surface'
type: 'feature'
created: '2026-08-25'
updated: '2026-11-06'
status: in-progress
route: dispatch
readiness: approved
implementation_authorized: true
baseline_commit: 2e2b6cff6c59775ce06d5e26b5c5913ac54c8edd
baseline_revision: 2e2b6cff6c59775ce06d5e26b5c5913ac54c8edd
blocked_by: []
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/evidence/story-6-5-prerequisite-readiness-20261105/readiness-final/contract-map.md'
---

<frozen-after-approval reason="Planning refresh requested; implementation requires a separate request">

## Intent

Expose accepted Project reads through authenticated FrontComposer inventory/detail/advisory journeys. Reuse delivered authentication; replace legacy REST adaptation with the existing SDK query client.

## Boundaries & Constraints

Require authenticated server identity/current Tenant and FrontComposer session credentials. Preserve Safe Metadata by default, separate audited descriptive inspection, Fluent UI V5 and WCAG 2.2 AA. No token storage/relay implementation in Projects, sibling edits, generated-client edits, domain writes, audit-history queries, maintenance/selection controls, payloads, secrets or protected denial detail. Retain legacy routing for 6.7 comparison; no cutover. Never edit sprint status here.

DW-1 remains done for the verified owner repair, not the unchanged gitlink. Accepted 6.1–6.4 and exact repair/test references are in [readiness evidence](evidence/story-6-5-prerequisite-readiness-20261105/readiness-final/README.md). Missing usable prerequisites block; publication/production enrollment are not added gates.

## I/O & Edge-Case Matrix

| Given | When | Then |
| --- | --- | --- |
| Anonymous/missing authority | Supported route loads | Challenge or fail closed before protected queries |
| Authorized page and degraded row | Inventory renders | Preserve page enumeration and each row snapshot independently |
| Open/context/start observations differ | Detail renders | Separate states, times, versions, omissions and recovery; no combined admission |
| Absent/denied/foreign/unverifiable target | Open requested | Identical generic safe absence without protected metadata |
| Incomplete advisory or unsupported File | Suggestions render | Explicit coverage limits; no exhaustive NoMatch, selection or admission claim |

</frozen-after-approval>

## Code Map

UI paths use `src/Hexalith.Projects.UI/`. The context contract map specifies DTOs, wire names and snapshots.

- `Program.cs`, `ProjectsUiSecurity.cs`, `Hexalith.Projects.UI.csproj`: add EventStore.Client/authorization using existing OIDC and source/package conventions. `src/Hexalith.Projects.AppHost/Program.cs`: gateway reference/address.
- `Diagnostics/{ProjectInventorySource,ProjectWarningsDashboardSource,ProjectDetailSource,ProjectResolutionTraceSource}.cs`: replace REST adaptation; adapt their interfaces/results/mappers and `src/Hexalith.Projects.Contracts/Ui/` descriptors.
- `Components/Pages/{Home,ProjectDiagnostics}.razor`, `Components/Shared/{ProjectNavigation,ProjectResolutionTraceWorkbench}.razor`: independent evidence and truthful labels.
- `Legacy*` pages, navigation and diagnostic sources: preserve the existing REST comparison routes.
- `src/Hexalith.Projects.Server/Queries/{GetProjectContextQueryHandler,GetConversationStartSetupQueryHandler}.cs`: leave computed-result `ProjectionType` unset for SDK routing; preserve delivered payload and admission semantics.
- `tools/qualification/{run_supported_project_reads,supported_browser_reads}.py`, private AppHost profiles and E2E fixture observers: real OIDC/browser proof with persisted-state comparison, continuous metadata-only telemetry checks and exact owned cleanup.

## Tasks & Acceptance

- [x] Compose SDK registration in the host/project files above. Attach `AddFrontComposerGatewayAuthorization()` after owner security registration. Derive query Tenant from `IFrontComposerTenantContextAccessor`; reject missing authority before querying, including local auth-disabled startup. Use `SubmitQueryAsync<T>`, exact targets/discriminators and typed payloads; propagate cancellation and sanitize gateway failures.
- [x] Adapt inventory/warnings sources and Home/navigation: opaque paging with Tenant/filter reset, lifecycle/quarantine selectors, explicit descriptive-inspection mode, independent page/row evidence; remove diagnostic N+1 and invented timestamps/warnings.
- [x] Adapt detail source/results/page: separately labelled open/context/start states, versions, components, omissions and recovery. Preserve the distinct Conversation-start snapshot and accepted owner-trust limits. Audit capability is unavailable; remove maintenance controls and unsupported Refresh calls.
- [x] Adapt advisory source/workbench/descriptors: nullable result, unselected candidates with IDs/rank/score/safe reasons, evaluation time distinct from source freshness, incompleteness distinct from lag, explicit unsupported File coverage, no paging, 32 raw attachments before deduplication.
- [x] Extend `tests/Hexalith.Projects.UI.Tests/Authentication/`, `Diagnostics/`, `Components/` for the frozen matrix and independent evidence. Extend `tests/Hexalith.Projects.Integration.Tests/AspireTopologyTests.cs` and `tests/e2e/specs/` for real authenticated SDK journeys and accessibility. Connect test-owned persisted observation to browser reads through `tests/Hexalith.Projects.E2E.Fixtures/`; isolate setup/cleanup/concurrent writes, expose no production state endpoint.

Given interleaved authenticated circuits, including same-subject sessions within/across Tenants, when supported queries execute, then each uses only its session credential/current Tenant. Given complete/degraded/empty/archived observations, when rendered, then delivered semantics survive without inferred admission. Given Safe Metadata versus separately authorized descriptive inspection, when reads execute, then protected values remain absent unless permitted and required inspection audits persist separately. Given browser inspection, when persisted key sets/bytes/ETags/events and telemetry are compared, then domain state is unchanged and telemetry leaks no protected values. Given keyboard/screen-reader/320px/200% zoom journeys, when axe/manual checks run, then evidence records no unresolved critical/serious findings.

## Verification

Apply every emitted Projects mapping value. From each `tests/Hexalith.Projects.{UI,Integration}.Tests/` directory run `dotnet build --configuration Debug -m:1 -p:NuGetAudit=false --disable-build-servers`; invoke built xUnit assemblies with `-class` and native XML. Run `npm --prefix tests/e2e run typecheck` and `npm --prefix tests/e2e run test:live:managed`; use local realm fixtures privately. Retain command/XML/browser/manual receipts, totals and canonical tested revisions. Readiness passes are not feature acceptance.

## Implementation Notes

2026-11-06: user approved and continued. Implementation is authorized; frozen intent remains locked. Its explicit sprint-status exclusion overrides workflow synchronization. The five implementation tasks are complete; overall acceptance remains pending below.

Historical readiness, before this approval: synthetic fixture migration and the mapped UI build passed, with authentication composition 12/12. FrontComposer HEAD/root gitlink is `f577f958ed1c043eec7feabc4885f59d48428818`; all 17 repair hashes match the retained owner repair. Its historical 114/114 receipts remain historical because binaries changed. Readiness evidence does not accept this feature.

Implemented the opt-in `/inspect/projects` inventory/detail/advisory surface through typed SDK queries and FrontComposer-owned authentication/current-Tenant/session credentials. Preserve the legacy routes for 6.7. Page, row, open, context and Conversation-start observations remain independent. Descriptive/quarantine inspection is explicit and audited. No Projects token store/relay, maintenance/selection control or audit-history query was introduced.

The two Projects-owned computed-query handlers now leave `ProjectionType` unset: the SDK router rejected their former query discriminator as projection metadata. Payloads and accepted snapshot/admission semantics are unchanged.

Verification: selected native UI 197/197, Server 185/185, Contracts 46/46 and topology/observer 28/28 pass with zero skips/errors; mapped Debug/source builds have zero warnings/errors. Launcher controls 133/133, TypeScript, Python and shell checks pass. The final wholly fresh managed run has native real-owner seed 1/1 and actual OIDC browser 1/1 passing, with unchanged persisted domain fingerprints, separate inspection-audit growth, continuous stdout/stderr leak checks, pre-existing resource preservation and verified owned cleanup. Exact commands, XML, artifact hashes, tested revisions and source manifest are in the [implementation evidence](evidence/story-6-5-implementation-20261106/README.md). The primary-agent [matrix audit](evidence/story-6-5-implementation-20261106/matrix-audit.json) binds all five frozen rows to 12 executed passing cases. Three native cases requiring separate owned `STORY61_DAPR` endpoints are explicitly excluded and not counted as passes or skips; their initial failure receipts are retained.

Concurrent live reads can return a current Project/authorization observation with an unavailable Folder and Retry guidance. The browser proof retains an initial actual Complete observation and checks the precise delivered degraded state during interleaving. No universal availability or recovery guarantee is inferred; no owner timeout or behavior was changed, and the cause of variable Folder availability remains unestablished.

Remaining acceptance: manual screen-reader and native 200% browser zoom checks have not been performed. Automated 320px axe has no violations or incomplete rules; CSS 200% zoom has no violations but an inconclusive `color-contrast` rule requiring manual evaluation. The [manual availability receipt](evidence/story-6-5-implementation-20261106/manual-check-availability.json) is pending evidence, not a passing accessibility receipt. The ordinary managed comparison stopped before collection (CreateTenant HTTP 403; retained legacy probes HTTP 404; zero collected); no unrelated writer/bootstrap/routing repair was added. Failed diagnostic runs remain separately retained, including a browser-passing run whose pre-existing-resource preservation guard failed.

Status remains `in-progress`. The BMAD implementation step requires “every acceptance criterion is satisfied” before review; manual acceptance is still missing, so the workflow review phase has not started. No full WCAG, package, production-readiness or 6.7 equivalence acceptance is claimed. Sprint status remains byte-unchanged; nothing was committed or pushed.

## Spec Change Log

2026-08-25 prerequisite hard gate and November 2 correction remain historical in the [exact pre-refresh spec](evidence/story-6-5-prerequisite-readiness-20261105/readiness-final/spec-before-refresh.md). The approved ordinary-input gate supersedes the former prerequisite_record ceremony.

2026-11-06: refreshed actual Diagnostics/authentication/SDK map and independent response/advisory semantics; cleared consumer fixture failure. Preserve no-token-bleed, safe disclosure and feature acceptance requirements. No Story 6.5 implementation dispatched.

2026-11-06: refine consumer/verification map; frozen intent preserved.

2026-11-06: implemented and verified the approved SDK read surface; all five frozen matrix rows pass. Manual accessibility acceptance remains pending; retain in-progress status and unchanged sprint status.
