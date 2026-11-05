---
title: 'Inspect Projects through an authenticated FrontComposer read surface'
type: 'feature'
created: '2026-08-25'
updated: '2026-11-06'
status: draft
route: dispatch
readiness: ready-for-separate-implementation-request
implementation_authorized: false
baseline_revision: b9a890e38044d23affe6b5d403d4b2a69b6edf34
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

Paths below are relative to `src/Hexalith.Projects.UI/` unless qualified. [Detailed delivered contracts and edit map](evidence/story-6-5-prerequisite-readiness-20261105/readiness-final/contract-map.md) are implementation inputs.

- `Program.cs`, `ProjectsUiSecurity.cs`, `Components/Routes.razor`: OIDC/cookie/server state/challenge already exist. Require configured authentication on supported journeys; preserve explicit local auth-disabled startup semantics.
- `Hexalith.Projects.UI.csproj`: reference existing EventStore.Client under current source/package conventions. Register `AddEventStoreGatewayClient().AddFrontComposerGatewayAuthorization()`; use `IEventStoreGatewayClient.SubmitQueryAsync<T>(SubmitQueryRequest, …)`. `src/Hexalith.Projects.AppHost/Program.cs`: add gateway endpoint/reference, retain delivered OIDC.
- `Diagnostics/ProjectInventorySource.cs`, `Diagnostics/ProjectWarningsDashboardSource.cs`: paged list and separate row evidence; no diagnostic N+1. `Diagnostics/ProjectDetailSource.cs`: independent open/context/start instead of legacy audit/conversation fan-out. `Diagnostics/ProjectResolutionTraceSource.cs`: advisory DTOs. Adapt corresponding interfaces/results/mappers, not generated REST clients.
- `Components/Pages/Home.razor`, `Components/Pages/ProjectDiagnostics.razor`, `Components/Shared/ProjectResolutionTraceWorkbench.razor`: supported metadata presentation; audit capability unavailable and no maintenance controls.

## Tasks & Acceptance

- [ ] Compose SDK registration/reference and gateway topology in the files above, reusing owner server context and cancellation/failure handling.
- [ ] Adapt Diagnostics sources/results and pages using `src/Hexalith.Projects.Contracts/Queries/`: `ListProjectsQuery`→`ProjectListPage`/`ProjectListRow`, `GetProjectQuery`→`GetProjectResponse`, `GetProjectContextQuery`→`ProjectContextReadResponse`, `GetConversationStartSetupQuery`→`ConversationStartSetupResponse`.
- [ ] Preserve each response's snapshot. `AdmissionSnapshot` carries state/AsOf/disclosable ProjectVersion/components/recovery; Conversation-start has its distinct CLR snapshot. Do not copy admission or invent coherence. Preserve accepted unavailable/owner-trust limits; no unregistered Refresh operation.
- [ ] Adapt advisory source/workbench to both delivered `ResolveProjectFrom*Query` operations and `ProjectAdvisoryResolutionResponse`: Result?, Candidates, EvaluatedAt, Incomplete, UnsupportedInputKinds. Candidates expose IDs/rank/score/safe reasons only. Evaluation time is not source freshness; lag alone is not incomplete. Preserve 32 raw attachment bound before deduplication, unsupported File coverage, no paging and unselected SingleCandidate semantics.
- [ ] Extend `tests/Hexalith.Projects.UI.Tests/Authentication/`, `Diagnostics/`, `Components/` tests for the matrix and independent evidence. Extend `tests/Hexalith.Projects.Integration.Tests/AspireTopologyTests.cs` and `tests/e2e/specs/` authenticated inventory/detail/resolution/accessibility journeys; retain persisted no-domain-write proof.

Given two authenticated circuits (including same-subject sessions across/within Tenants), when queries interleave, then only each session's credential/current Tenant is used. Given complete/degraded/empty/archived results, when rendered, then exact delivered semantics survive without inferred admission. Given any inspection, when storage and telemetry are compared, then domain bytes/events remain unchanged and protected values stay absent; required metadata inspection audit remains a separate permitted effect. Given keyboard/screen-reader/320px/200% zoom journeys, when inspected, then authenticated axe/manual evidence passes with no unresolved critical/serious findings.

## Verification

Future implementation must build each UI/Integration test project separately in Debug/source mode with every emitted Projects mapping value, then invoke built xUnit assemblies directly (`-class`, native XML); extend assertions beyond source selectors. Run `npm --prefix tests/e2e run test:live:managed` for supported browser journeys and retain actual XML/browser/manual evidence, commands, totals and tested source references. No browser, persisted-boundary or accessibility acceptance is claimed by readiness tests.

## Implementation Notes

Readiness only: authorized synthetic fixture migration applied; mapped UI test build passed with zero warnings/errors; native authentication composition 12/12 passed. Owner repair unchanged; prior owner 114/114 receipts retained. Spec remains draft pending approval in a separate implementation request; sprint status unchanged.

## Spec Change Log

2026-08-25 prerequisite hard gate and November 2 correction remain historical in the [exact pre-refresh spec](evidence/story-6-5-prerequisite-readiness-20261105/readiness-final/spec-before-refresh.md). The approved ordinary-input gate supersedes the former prerequisite_record ceremony.

2026-11-06: refreshed actual Diagnostics/authentication/SDK map and independent response/advisory semantics; cleared consumer fixture failure. Preserve no-token-bleed, safe disclosure and feature acceptance requirements. No Story 6.5 implementation dispatched.
