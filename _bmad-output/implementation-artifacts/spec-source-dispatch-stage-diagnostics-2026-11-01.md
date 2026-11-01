---
title: 'Locate the actual source command response stall'
type: 'bugfix'
created: '2026-11-01'
status: 'done'
route: 'dispatch'
baseline_commit: '1965ac06af00552636c6d9d77b0cb77996220e05'
context: []
---

## Intent

The user authorizes complete SDK/package/submodule updates, all actual CI repairs, push and final release without human validation. Nested checkouts must stay deinitialized and source references use root-declared siblings. Work from each owning repository after its AGENTS/baseline/config guidance. Parent owns Git and hosted operations. No skill under references may be loaded. No shared local backend/container changes.

## Evidence and Code Map

Hosted Tenants source run41347795093 at331c528219e921e990402a6134fed9bfc50f0441 uses EventStore1965ac06af00552636c6d9d77b0cb77996220e05 and fails bootstrap-command-timeout. Full log /tmp/projects-release-maintenance/tenants-331-source-runtime-failed.log. Bootstrap dispatch20:48:49.312 uses message01M6CDY9AWPMV66AS1PN42BJXF/correlation01M6CDY9B0KRBZCXBDJW4HNB95. The known-length replay fix removes the old400 validation defect. The actor stores one event, publishes and completes20:49:05.902 after1.98s; duplicate invocations return its cached result. Projections complete07.996/18.971. Caller standard30-second total request timeout fires19.393. No idempotency key is present, so admission coordination is not in this path. Existing CommandRouted1103 isDebug; absence atInformation proves nothing. Authority HTTP calls are fast(medianGET1.03ms/write4.64ms,max<29ms) despite101conflicts, so no particular authority stall is established. Existing Dapr OTLP export fails; no traces recover the missing boundaries.

- EventStore src/Hexalith.EventStore.ServiceDefaults/Routing/TechnicalHttpRouteCatalogReceiver.cs native callback261–288 awaits next(context) then lease disposal; outer command receiver also awaits lease release163–167.
- EventStore src/Hexalith.EventStore.Server/Commands/CommandRouter.cs RouteCommandAsync67–100 awaits actorRPC then its catalog lease disposal.
- EventStore src/Hexalith.EventStore.Server/Pipeline/SubmitCommandHandler.cs awaits router, status, archive, correlation, projection delivery, activity/stream tracking, then its lease release. Read the complete methods and logging conventions before editing.
- Tenants scripts/prepare-source-catalog-profile.py and its guarded profile material/config consumers enable only this owned CI profile. Prefer existing per-host profile configuration for targeted diagnostic log levels; avoid unrelated AppHost wiring. If AppHost resource code truly needs editing, follow the allowed Aspire skill first.
- Existing source/runtime tests and configuration; use built individual xUnit classes when necessary.

## Tasks & Acceptance

Implement the smallest support-safe diagnostic needed to attribute the concrete stalled await. No functional timeout, retry, persistence, authorization, lease ownership/disposal, response, or receipt semantics may change. Preserve the committed producer buffering and all strict source-profile assertions. Never turn an unverified state into success.

Record closed per-request stages/timings at the native actor callback and its lease release, CommandRouter actorRPC and its lease release, and Submit handler routing/post-actor awaits/lease release. Use existing logger/source-generation conventions where practical. Stage/status labels must be a finite internal vocabulary; durations and cancellation flags are bounded primitives. Correlate attempts with bounded trace/span identifiers. Avoid raw bodies, headers, credentials, keys, URLs, arbitrary error messages and serialized state; existing protected-idempotency handling must not leak key/identity material. Do not add a public API or transport wrapper with changed completion/cancellation behavior. Use original exceptions and tokens; logging must not fail a request or extend budgets. Make targeted diagnostics available only when their logger is enabled, and enable the exact categories through the owned source-profile configuration for the hosted run.

Add narrow meaningful coverage for privacy and in-flight/completed/failing stage attribution on the real production seam; preserve return values, original exceptions, cancellation and lease ordering. Run only relevant owning builds/tests and source-profile material/config guard tests. All existing API/private-material refusal rules remain intact. Do not run a local live topology or change shared Docker resources. Do not stage/commit/push/init anything. Another agent has completed only a two-file Tenants package-boundary correction; preserve it and avoid those files.

When done report changed files, exact focused verification and anything incomplete. Parent reviews combined owner diffs, commits child repositories before root pointers, and reruns actual hosted source qualification to find the functional cause.

## Parallel complete-package correction in this review

The supplied combined diff also includes the completed two-file Tenants package-boundary correction. Official EventStore.Aspire3.131.0 requires its exact technical SDK ServiceDefaults package, and current CPVM promotes that edge into Tenants.Aspire. Only that precise owning-package exception is added; domain/host/test/unknown/lookalike refusals and exact version/restore/framework checks remain. The complete44-project Release solution, all five declared fresh packs, authoritative validators and all real consumers pass;20 focused validator cases pass, and the previous validator fails against the same Aspire archive. This parallel correction stays independent of all runtime/receipt semantics.

## Local verification

Debug source Server.Tests build passed with zero warnings/errors. Focused production regressions pass155/155, including25 new diagnostic/privacy/ordering cases; no skips. Tenants profile provisioning/config checks pass2/2 with13 refusal scenarios intact. Owning diff checks pass. Actual hosted source qualification is pending to expose the stalled stage; diagnostics do not claim a functional fix.

## Review Triage Log

- Blind1 — medium, patch: actor RPC receives no operation token, so inherited request-token flags misdescribe ongoing work. Record None independently and assert request cancellation remains distinct.
- Blind2 — medium, patch: real lease release has no token and the authority uses None; explicitly record the independent token for every disposal stage.
- Blind3 — medium, patch: protected routing uses RouteFencedCommandAsync, leaving actorRPC/disposal attribution incomplete. Apply the same diagnostic-only boundaries to the fenced path.
- Blind4 — medium, patch: protected admission/capability/begin/initial-fence awaits can stall after acquisition without an open stage. Add closed markers around those existing awaits without changing their behavior.
- Blind5 — medium, patch: the unknown-outcome branch similarly lacks reconciliation/fence/completion attribution before its early return. Trace those exact existing awaits and router reconciliation path.
- Blind6 — medium, patch: actual recovery persists with None after a failed stage closes; open a separate recovery marker through its existing await.
- Blind7 — low, rejected: the marker reports completion of DisposeAsync, which intentionally may retain an ambiguous fence; it does not claim durable quiescence. The existing tests explicitly verify zero release callbacks on retention. Adding new lease-state introspection is more than a direct correction and is unnecessary to find the stalled await.
- Blind8 — high, patch: enabling CommandRouter Debug exposes an existing routing event for protected execution identities, while the new test logger filters to1190. Redact protected routing/failure identities and verify complete newly enabled output, preserving public recovery identifiers on ordinary routes.
- Blind9 — false: the observed frozen fixture emits full resource logs to failing-test stdout before its separate limited classifier. The current raw hosted log retains over34000 lines, including the initial bootstrap. New structured records use that same retained job-log path; Classify filtering does not erase the complete output. A second artifact parser is optional convenience.
- Blind10 — low, rejected for shared-context expansion: the actual owned ASP.NET/OpenTelemetry profile has W3C request activities; no missing-activity or hierarchical request was demonstrated there. Unique bounded fallback attempt IDs still group individual operation stages. A new ambient shared-context lifetime is more than a direct correction for an unencountered layout; preserve the current context model.
- Blind11 — medium, patch: a regression invoking a callback that should remain retained can deadlock the new test before its finally opens the gate. Bound pending awaits and open every gate during cleanup.
- Edge1 — medium, patch: same demonstrated operation-token mismatch as Blind1/2; retain as its own row.
- Edge2 — high, patch: same newly enabled protected routing identity exposure as Blind8; retain as its own row.
- Edge3 — medium, patch: same uninstrumented protected RPC/disposal path as Blind3; retain as its own row.
- Edge4 — medium, patch: same recovery-await attribution gap as Blind6; retain as its own row.
- Verification1 — medium, patch: pre-verified absent held-acquisition assertions. Test all four actual acquisition seams while pending, then their successful/failing close, with downstream stages absent until release.
- Verification2 — medium, patch: pre-verified fence/completion scopes are only no-ops in existing tests. Exercise executable protected sessions with held validation/completion/recovery and preserve original behavior.
- Verification3 — medium, patch: pre-verified missing separate projection-token cancellation assertion; cancel the request during the real held projection and assert request=true/operation-can-cancel=false/operation-canceled=false.

## Pushed diagnostic capture

EventStorefa978a283b29deb05a9c988ff54e3299de9606fc is pushed. Review patches explicitly separate request/operation cancellation for tokenless RPC and disposal, cover protected/fenced/reconciliation/admission/recovery paths, redact protected newly enabled routing/failure output, and bound test cleanup. Parent independently ran184 focused regressions with zero failures/skips after the final54 diagnostic cases passed. SDK source build has zero warnings/errors.

Tenants upstreama4447bb5 already contains the equivalent exact Aspire dependency allowance; it and upstream81b9bb53 guarded AppHost log forwarding were preserved by fast-forward. The local alternative two-file boundary patch remains in/tmp only, not duplicated in source. Parent reran the adopted11 boundary tests and2 profile guards successfully. Tenants7c7fe7f47d7970607e37ca5dfb741a3950d10753 adds the diagnostic SDK gitlink without nested initialization, profile logger settings and explicit guarded native-receiver forwarding. Its source AppHost build passed with zero warnings/errors. Hosted source run41353744654 and main CI41353745030 are pending; the next source result must establish the stalled boundary before any functional correction.

## Hosted attribution completed

Run41353744654 produced558 bounded1190 records. Warm actorRPC421–613ms and router releases7–12ms complete; inline projection delivery takes11787/11114/13612ms, after which activity tracking fails on the canceled request. Bootstrap times out21:56:17.119 without202. This completes the diagnostic goal; functional correction remains in spec-durable-projection-command-response-2026-11-01.md. Durable credential-free attribution is release-evidence/source-dispatch-runtime-attribution-2026-11-01.json.
