---
title: 'Dispose retained tenant command flows after their service scope closes'
type: 'bugfix'
created: '2026-11-01'
status: 'in-review'
route: 'dispatch'
baseline_commit: '7c7fe7f47d7970607e37ca5dfb741a3950d10753'
context: []
review_loop_iteration: 0
---

<frozen-after-approval reason="user authorized actual CI fixes and bypassed human validation">

## Intent

Fix demonstrated retained-flow disposal failures when Blazor disposes a component after its DI provider. Cache the exact tracker selected and subscribed during initialization, and use that reference during cleanup. Preserve command ownership, retained attempts, cancellation, generations, admission and event unsubscribe behavior. This is one shared lifetime defect across the existing five flow types.

## Boundaries & Constraints

No service-provider resolution from Dispose, no swallowed disposal exceptions, new service lifetime or broad framework changes. Preserve optional no-gate/no-tracker behavior, each component's current detach/generation/admission semantics and UX. Do not change layout, navigation, browser wait conditions, timeouts, authorization, command receipts or CI assertions. No Git operations, nested initialization, local live topology, backend or Docker changes. Parent owns Git and hosted reruns. No skills under references may be loaded. The parent already rendered bmad-build; this is its step03 handoff, so implement directly without invoking/rendering the skill again.

## I/O & Edge-Case Matrix

| State | Expected behavior |
|---|---|
| Normal provider and subscribed tracker | Dispose unsubscribes and detaches that same tracker with existing admission/generation cleanup |
| Provider already disposed | Dispose does not resolve services, does not throw, and performs the same unsubscribe/detach |
| Optional gate/tracker unavailable at initialization | Disposal is safe and never acquires a later/different tracker |
| Pending retained command during renderer replacement | Original attempt remains recoverable under the existing tracker protocol; late notifications do not re-render disposed component |

</frozen-after-approval>

## Code Map

- Owner `references/Hexalith.Tenants`. Read owning AGENTS, .editorconfig, .gitattributes, UI project/test configuration and permitted workspace AI.Tools LLM/UX baseline. Work in the owning repo, preserve all other changes. Parent has one separate guarded source-profile setting in `src/Hexalith.Tenants.AppHost/TenantsDevelopmentRoutingCatalog.cs`; do not edit it or EventStore/workflows.
- `src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor`: OnInitialized around704 subscribes a locally resolved tracker; Dispose1409 resolves Services again after provider disposal. Retain the exact selected nullable tracker in a component field.
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor`: identical OnInitialized575/Dispose1163 defect.
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/ChangeTenantMemberRoleFlow.razor`: identical OnInitialized626/Dispose1211 defect.
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor`: identical OnInitialized891/Dispose2011 defect.
- Locate `CreateTenantFlow.razor` with rg --files: OnInitialized around438 obtains AttemptTracker?.Attempts; Dispose1304/1310 indirectly resolves Services through AttemptTracker property772–773. Cache the selected attempts tracker, preserving the existing create-flow facade behavior.
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCommandAttemptTracker.cs`: Detach is direct lock-protected cleanup; Dispose clears events/timers. No service lookup is needed to unsubscribe or detach the cached instance.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantRetainedCommandFlowTests.cs`: existing RenderFlow covers create/add/change/remove/metadata and retained detach/lifetime cases. Extend meaningful real component tests for provider-disposed cleanup, selected tracker identity, optional absence, and retained ownership. Use existing per-flow suites as appropriate.

## Tasks & Acceptance

- [x] Cache each flow's exact initialized/subscribed tracker; Dispose uses it without service resolution and preserves all existing cleanup.
- [x] Add parameterized provider-disposed lifetime regression across all five flows with meaningful detach/unsubscribe assertions, plus relevant optional/retained cases.
- [x] Build the owning UI test project and run only affected flow/retention classes, serially with source siblings and no nested initialization. Coordinate shared build outputs with the parent before builds; EventStore implementation is active. Report exact commands, totals, changed files and limitations.

Given each actual component was initialized with a tracker, when its provider is disposed before component disposal, then cleanup succeeds using the original tracker and retained ownership is correctly detached. Given no tracker was selected, when cleanup runs, then it remains safe without any DI access. Parent independently verifies and reruns actual CI; do not claim this proves an upstream Dapr crash is fixed.

## Implementation Notes

Actual Tenants7c7 CI41353745030 Aspire job124994623485 fails AuthenticatedTenantsUiRouteTests.LiveBrowserMeasuresResponsiveForcedColorsAndRestoresContextualAuditBackFocus. Raw log `/tmp/projects-release-maintenance/tenants-7c7-aspire-failed.log`. StaticHtmlRenderer disposal throws ObjectDisposedException from the two named flow Dispose methods at22:14:51.917,22:14:52.174,22:16:02.936 and22:18:18.633. This is a demonstrated independent defect. A separate upstream Dapr nil-pointer response-flush panic occurs22:18:18.643; latest released1.18.10/1.19.1 remain affected, open upstream10371/10373/10398. Correlation does not establish the earliest browser timeout cause. Preserve current Dapr tuple/budgets until new evidence justifies another fix.

## Review Triage Log

## Local verification

Final serial Debug/source UI.Tests build with global EventStore3.131.0 producer identity completed with zero warnings/errors. Seven affected flow/retention classes passed745/745, zero errors/failures/skips, including25 new provider/tracker lifetime cases. Parent read the entire owning diff. Test logs/XML are /tmp/tenant-flow-disposal-ui-build.log and /tmp/tenant-flow-disposal-ui-tests.log/.xml. Independent review and actual hosted browser qualification remain pending; no upstream Dapr fix is claimed.

## Combined review closure

The combined three-layer review is complete. All findings were individually triaged in spec-durable-projection-command-response-2026-11-01.md. No further code changes apply to this scoped repair. Existing local verification remains passing; hosted exact-source qualification is pending.
