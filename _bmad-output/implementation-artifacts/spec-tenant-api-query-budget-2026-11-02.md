---
title: 'Align the API gateway query attempt with native authority verification'
type: 'fix'
created: '2026-11-02'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'ca93fe19847d3535353c8b4850497350aaba47d3'
context: []
---

<frozen-after-approval reason="user authorized actual CI repairs, commits/pushes and verified release without human validation">

## Intent

Fix the demonstrated premature API gateway-client attempt timeout in the actual source profile, and improve the unchanged first REST assertion with an existing bounded diagnostic. Keep the existing30second total request budget, standard retry/breaker/rate-limiter behavior and all authorization/native proof checks. Final REST failure classification in run41364760838 is not captured, so do not claim this independently proven timeout is its complete cause until hosted qualification passes.

## Boundaries & Constraints

Only replace the inherited resilience pipeline for the owning API gateway client. Keep ordinary/readiness clients unchanged; preserve bearer forwarding then final Dapr handler ownership and catalog primary transport guard. Do not change native10s+2s reader bounds, fixture60s HTTP timeout, profile deadlines, successful status/persisted/metadata/lease assertions, SDK defaults, UI budgets, or add test polling/retries. No nested checkout initialization or local topology/backend startup. Parent owns remote operations. Direct step03 of existing parent bmad-build; do not invoke/render/orchestrate it again.

</frozen-after-approval>

## Code Map and proof

- Tenants src/Hexalith.Tenants.Api/Program.cs118 AddEventStoreGatewayClient returns actual named IHttpClientBuilder, then attaches InboundBearerForwardingHandler and AddEventStoreDaprServiceInvocation. Keep that final order.
- EventStore ServiceDefaults/Extensions.cs ConfigureHttpClientDefaults adds the inherited standard handler. Its empty builder name produces shared options/pipeline -standard. Pinned Microsoft.Extensions.Http.Resilience10.11.0 API/source inspected with allowed dotnet-inspect: standard options name is actual builder.Name+"-standard". Named configuration alone would miss the inherited handler. The convenience RemoveAllResilienceHandlers method is marked experimental in10.11.0. Use the existing stable ConfigureAdditionalHttpMessageHandlers reverse loop that removes ResilienceHandler instances for this builder, followed by AddStandardResilienceHandler, to create the scoped replacement without a pragma or global warning suppression.
- Configure only that client's AttemptTimeout30s, existing TotalRequestTimeout30s, required CircuitBreaker.SamplingDuration60s. The framework validator requires Attempt<=Total and sampling>=2*Attempt. Other retry, breaker thresholds/break duration and rate-limiter settings remain standard. Do not remove resilience entirely.
- Actual source41364760838 atTenants020e40e6/Eventa825 passes bootstrap, CreateTenant202/Completed and persisted tenant. Firstlist API gateway attempt starts00:12:34.022; gateway receiveslist36.115; nativehead38.035→40.197HTTP200; eventpage40.199→42.729HTTP200; finalhead42.732pending. Standard-AttemptTimeout fires44.023 at10002.55ms, before the native10second deadline (~48.035) and before the2second final observation. Retry starts46.121; firstRESTIsSuccessfalse47.656. No final APIstatus/FailureKind captured. Earlier5130catalog-disagreement08.487 is bootstrap polling, not proof of this body failure.
- Raw actual log /tmp/projects-release-maintenance/tenants-020e-source-failed.log. Test caller is60seconds, not15seconds. Do not infer a missing final classification.
- SourceCatalogOwnerProfileTests.cs61 currently asserts awaitedList.IsSuccess directly; retain the returned result and pass its existing safe ToString() to the same ShouldBeTrue assertion. TenantsRestQueryResponse.ToString() reports only success flags, closed FailureKind and numeric StatusCode, never payload/URLs/proofs/ETags/cursors.
- Existing production API WebApplicationFactory/handler fixtures should verify actual registration and chain. Use owning IntegrationTests; avoid a parallel synthetic registration that could pass if Program wiring is removed. Source and package modes must remain compatible.
- Latest upstreamca93fe19 independently resolves the recorded standalone deployment-policy follow-up (production tenantsInvocation30/examplegateway30+tests). Preserve it, no duplicate policy edits.

## Tasks & Acceptance

- [x] Scope the supported replacement to actual API gateway builder with30/30/60 options before bearer forwarding/Dapr routing; preserve other clients and standard retry/breaker/rate-limit behavior. Add only a direct central PackageReference if using new public framework APIs requires it by owner conventions.
- [x] Verify real production client name/chain and exactly one resilience handler, validated scoped options, unchanged inherited ordinary/readiness options; use actual transport transient-failure→success retry and caller cancellation controls. Prove removing Program policy wiring or retaining inherited short outer handler fails regression. Keep bounded no-backend tests, do not mirror only literals.
- [x] Add support-safe first-list assertion context without changing success/metadata/persistence/lease requirements.
- [x] Run narrow owning serial Debug/source build and affected classes, report exact commands/logs/totals, freeze. Do not stage/commit/push.

Given a valid native authority read, the API attempt cannot expire at the inherited10second boundary before its bounded suboperations. Given early transient failure, standard retries remain. Given caller/total cancellation, the actual transport still cancels. Given an unrelated client, its inherited resilience remains unchanged. Given a remaining REST failure, the exact closed result is visible and the original assertion still fails. Actual hosted owner qualification remains required.

## Implementation choice

The first build exposed EXTEXP0001 on the convenience removal API. The candidate now uses the stable per-client handler hook already used by the owning bootstrap transport, preserving the same scoped removal semantics. No experimental call or warning suppression remains. Product changes remain confined to API Program and the safe assertion context. Tests exercise the real production factory, exact guarded source transport, scopes/order/options, an11second positive call, transient retry, caller cancellation and actual30second total cancellation; unrelated readiness tests are the control. Final local results and hosted qualification remain pending.

## Local verification

The stable source IntegrationTests build passes with zero warnings/errors (83.92s); final test-only compile also passes with zero warnings/errors (7.73s). Five actual production-factory gateway cases pass with zero failures/errors/skips/not-run (43.584s), including an11second positive held call, transient503 retry with owned headers, caller cancellation and actual30second total cancellation. Six existing API readiness controls passed on the same stable product candidate; subsequent edits only corrected new fixture endpoint/policy declarations and runtime-generated credential values. Exact logs: /tmp/tenant-api-query-budget-stable-build.log, /tmp/tenant-api-query-budget-final-test-build.log, /tmp/tenant-api-query-budget-final-tests.log; initial fixture failures plus passing readiness controls remain in /tmp/tenant-api-query-budget-tests.log. Qualified current/floor package checks and hosted source qualification remain pending; the final earlier REST FailureKind is still not captured.

## Review Triage Log

- Blind1 — medium, patch: removing the original standard handler leaves service discovery before the newly appended policy, changing the prior retry/discovery ordering. Restore the standard handler before service discovery and assert that actual production order; keep bearer forwarding and Dapr ownership afterward.
- Blind2 — low, patch: attempt30 equals total30 intentionally leaves no budget for retry after a full attempt timeout. The existing total-cancellation case already requires one attempt and the503case proves early transient retries. Clarify this deliberate behavior in the owning code comment; no new timeout policy or API is needed.
- Blind3 — low, patch: the behavioral total test checks expiry but not survival near its configured boundary. Add a monotonic lower-bound assertion after resolving the production client, so an early15/20second runtime timeout cannot pass. Keep the existing30second total and watchdog bounds.
- Blind4 — false: no per-retry total-reset path is introduced. A single standard handler owns its total timeout outside retries; the existing held-call case requires a TimeoutRejectedException with one actual attempt while default timeout retries remain enabled. A per-attempt-only total would retry and reach the35second caller cancellation instead, failing that case. Early503retry is separately verified. Another framework-invariant case is unnecessary for the changed application wiring.
- Blind5 — medium, defer: standard HTTP resilience ends when response headers return, and the SDK's subsequent response buffering/deserialization uses the caller token. This is the same preexisting response-body deadline gap at baselineca93, not introduced by the scoped attempt change. A blocked-body transport case and an explicit owning body deadline would settle the appropriate platform correction; preserve current proof/caller semantics and do not expand this API attempt repair into shared SDK transport changes.
- Edge reviewer — no findings.
- Verification-gap reviewer — no gaps.

All three layers launched before triage. Parent read the final source and tests. Only ordering, explanatory comment and earliest-expiry assertion require a patch; package-mode verification remains in progress.

Package-profile control observation before review patches: current3.131.0 Release/package build passes with0warnings/errors; all five new gateway tests pass. Four of six preexisting readiness cases fail because their CaptureHandler replaces the catalog invocation primary transport without configuring the SDK-owned redirect-disabled invocation transport (RouteCatalogUnavailableException), before their handler is invoked. Readiness registration is outside the changed gateway builder and the six source-mode controls already pass. Do not relabel this11-case receipt as green: /tmp/tenant-api-budget-published-31310-tests.xml records7pass/4fail/0skip. The unchanged upstreamca93 exact main CI41366387993 independently passes all six enabled jobs, including current/floor published HTTP, authenticated live routes and Aspire. Final new gateway cases will be rerun in both package profiles after the direct review patches; actual hosted source qualification remains mandatory.

Final reviewed candidate is committed/pushed as 3a603db76f894c4ffacbae16683eb04f179d5f99. All three direct review corrections are applied and parent-read. Source owning API and test compile pass0warnings/errors; source gateway5/5 and readiness6/6 pass with0skips. Final Release/package builds pass0warnings/errors for current3.131.0 and floor3.129.0; gateway5/5 pass in each (42.401s/44.611s), including the restored actual handler order and monotonic near30second cancellation. Credential-free counts/log hashes are in release-evidence/tenant-api-query-local-verification-2026-11-02.json. Commit message preflight and hooks remain enabled. Actual fresh hosted source/main/root qualification is pending; no release acceptance is inferred from local checks.

Hosted run41370746958 at exact3a603db7 passes the complete v2/bootstrap/command prefix, all six REST reads and conditional metadata, actual Admin UI proxy/owner-read, refusal and persisted Redis assertions. The demonstrated first REST interruption is repaired. This run still fails the separate final zero-leases wait atline92 after120seconds; bounded attribution is release-evidence/tenant-zero-lease-runtime-attribution-2026-11-02.json and a separately scoped native-query/AdminApi transport repair is active. This closes the API client correction only; complete owner profile and final release remain unqualified.
