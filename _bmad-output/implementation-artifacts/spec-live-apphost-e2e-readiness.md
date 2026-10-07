---
title: 'Live AppHost E2E readiness'
type: 'feature'
created: '2026-08-27'
status: 'done'
baseline_revision: '9b8ba049a8329a6346311782bd3311d3e492a3dd'
baseline_commit: '9b8ba049a8329a6346311782bd3311d3e492a3dd'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md'
warnings: [multiple-goals, oversized]
deferred:
  - summary: 'The live archived-exclusion resolution case seeds no archived match, so its every() assertion passes on an empty candidate list.'
    evidence: 'Review B6. Pre-existing assertion design in tests/e2e/specs/projects-resolution.spec.ts; this change only replaced the placeholder conversation id.'
  - summary: 'Archive-to-convergence fixture teardown may be aborted when a live test exhausts its 60 s timeout, leaving Projects Active and replacing the primary failure.'
    evidence: 'Review E24, unverified medium. Settle with a forced-timeout live run that checks whether teardown completes and which error is reported.'
  - summary: 'An exact retry of a successful proposal confirm re-runs resolution, finds the new Project, and returns 400 instead of replaying the original result.'
    evidence: 'Observed during live verification; ProposeNewProjectEndpoint evaluates resolution before the idempotency replay check. Pre-existing ordering.'
  - summary: 'A tenant with no projected Project gets 503 read_model_unavailable from GET /api/v1/projects instead of an empty list.'
    evidence: 'Observed live: DaprProjectProjectionStore.EnsureReadable throws when the tenant journal is absent, while GetReadinessAsync treats the same state as watermark 0. Pre-existing since Story 1.9.'
  - summary: 'A Project name containing / is rejected as 404 tenant_access_denied instead of a 400 validation problem.'
    evidence: 'Observed live: POST /api/v1/projects with a slash in the name returns 404 category tenant_access_denied; a colon-only name is accepted. Pre-existing error mapping.'
---

<intent-contract>

## Intent

**Problem:** The recurring browser lane is offline-only, Projects UI has no real browser OIDC session, and the partially built live-fixture profile is not wired into AppHost or Playwright. Consequently scheduled verification cannot prove startup, authenticated UI access, deterministic tenant/sibling state, parallel isolation, cleanup, or absence of fixture ingress outside the explicit profile.

**Approach:** Complete the explicit live-E2E profile as one runner-owned path: provision deterministic tenant and sibling fixtures, secure Projects UI with FrontComposer's server OIDC/token-relay seams, and execute startup smoke plus the full live Playwright suite from a recurring managed lifecycle that fails on every live skip.

## Boundaries & Constraints

**Always:** Use the checked-in real Keycloak realm and authorization-code browser flow; derive tenant/principal authority from the access token; use supported authenticated APIs and bounded convergence polling; generate disjoint IDs from run/worker/retry/repeat/scenario; archive Projects and wait for archived convergence; seed only metadata through an explicitly enabled fixture profile; preserve primary failures while reporting cleanup by role/status only; use exact non-interactive Aspire start/wait/describe/stop commands and dynamic endpoints.

**Block If:** Safe tenant membership cannot converge through installed Tenants/EventStore contracts; required sibling behavior needs a sibling-repository mutation or direct production projection write; or host capacity prevents the explicit profile and focused live acceptance lane from starting after safe diagnostics.

**Never:** Edit the deferred-work ledger, bundle intent, prior workflow specs, generated files, sibling repositories/submodule pointers, nested submodules, production persistence, raw Dapr state, guessed ports, or browser interception for fixture provisioning. Never expose credentials, tokens, payloads, private paths, raw topology/log dumps, or token-bearing traces; never weaken authorization or accept a skipped live case.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Managed live run | Scheduled job with clean checkout and local test realm | Runner starts the exact AppHost, waits, describes once, exports endpoints, runs smoke/full Chromium with two workers and zero skips, then stops it | Any phase fails the job; exact-AppHost stop runs unconditionally |
| Browser session | Anonymous browser opens a protected Projects route | Real Keycloak code flow returns an HttpOnly cookie; reload remains authorized and UI-to-API calls relay the user token server-side | Missing/expired session challenges again without browser token storage |
| Parallel fixtures | Two attempts vary worker/retry/repeat/scenario | Tenant readiness converges once; every Project/sibling/request ID is disjoint and cleanup affects only its graph | Preserve test failure and attach metadata-only reverse-cleanup diagnostics |
| Profile disabled | Normal AppHost startup | No fixture resources, control endpoint, or sibling stub ingress exists | Fail-closed topology coverage detects any exposure |

</intent-contract>

## Code Map

- `.github/workflows/ci.yml` and `tests/tools/run-ci-workflow-gates.ps1` -- the scheduled job is currently offline-only and its policy gate enforces that obsolete shape; make the managed lifecycle and unconditional teardown structural invariants.
- `tests/e2e/playwright.config.ts`, `global-setup.ts`, `support/merged-fixtures.ts` -- live environment, real token warm-up, tenant readiness, fixture composition, browser session state, two-worker isolation, and live-only zero-skip enforcement converge here.
- `tests/e2e/support/helpers/{eventstore-api-client,tenant-access-readiness,live-fixtures-api-client}.ts` and `support/fixtures/{live-fixtures,projects-fixtures}.ts` -- reuse the partial authenticated readiness/graph scaffolds; send caller-owned Project IDs, wait for archive convergence, and keep cleanup diagnostics closed.
- `tests/e2e/support/factories/live-fixture-identities.ts` and affected `specs/projects-*.spec.ts` -- replace live fixed/placeholder IDs and states with graph outputs; offline HTML contract literals remain independent test specimens.
- `tests/Hexalith.Projects.E2E.Fixtures/**` -- partial metadata-only role/control host; make graph DTOs symmetric and return typed attempted-role/status cleanup results without bodies or endpoints.
- `src/Hexalith.Projects.AppHost/Program.cs`, `ProjectsLiveE2EFixtureProfile.cs`, AppHost project/solution files -- wire the profile only when enabled, expose its control endpoint to the runner, and compose Projects UI OIDC through the existing EventStore Aspire helper.
- `src/Hexalith.Projects.UI/Program.cs` and `Components/Routes.razor` -- reuse `AddHexalithFrontComposerServerSecurity`, `AddFrontComposerGatewayAuthorization`, auth middleware/endpoints, cascading state, and protected route rendering; do not invent auth plumbing.
- `src/Hexalith.Projects.AppHost/KeycloakRealms/hexalith-realm.json` -- add a confidential Projects UI code-flow client and single-valued current-tenant claims while retaining the separate API ROPC client.
- `tests/Hexalith.Projects.{UI,Integration}.Tests/**` and `tests/e2e/specs/live-*.spec.ts` -- focused security composition, realm/topology fail-closed, startup/session, identity, concurrency, and cleanup coverage.
- `.bmad-loop/runs/20260827-032611-21b4/bundles/live-apphost-e2e-readiness/intent.md`, `_bmad-output/implementation-artifacts/spec-5-12-live-apphost-operational-console-verification.md`, and `spec-live-e2e-fixture-provisioning.md` -- read-only intent and historical evidence.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Projects.AppHost/{Program.cs,ProjectsLiveE2EFixtureProfile.cs,KeycloakRealms/hexalith-realm.json,Hexalith.Projects.AppHost.csproj}`, `Hexalith.Projects.slnx`, and `tests/Hexalith.Projects.Integration.Tests/{AspireTopologyTests.cs,DaprConfigurationTests.cs}` -- wire the explicit fixture graph and confidential UI OIDC client; prove enabled resources and disabled-profile no-ingress structurally.
- [x] `src/Hexalith.Projects.UI/{Program.cs,Components/Routes.razor}` and `tests/Hexalith.Projects.UI.Tests/Authentication/**` -- enable FrontComposer server security conditionally, protect routes/endpoints, relay the signed-in token through `AddProjectsClient()`, and verify cookie/code-flow/session composition plus auth-disabled startup.
- [x] `tests/Hexalith.Projects.E2E.Fixtures/**` -- make graph DTOs symmetric and return typed reverse-order attempted-role/status cleanup results while keeping all ingress metadata-only.
- [x] `tests/e2e/{global-setup.ts,playwright.config.ts,support/merged-fixtures.ts,support/factories/live-fixture-identities.ts,support/fixtures/*.ts,support/helpers/*.ts}` -- finish token-derived tenant readiness, deterministic caller-owned Projects/sibling graphs, two-worker isolation, archived convergence, typed cleanup evidence, and browser OIDC storage state.
- [x] `tests/e2e/specs/{live-fixture-identities,live-fixtures-lifecycle,live-apphost-startup,projects-authentication,projects-file-reference,projects-resolution,projects-proposal,projects-resolution-trace,projects-reference-health,projects-console-shell}.spec.ts` -- add startup/session/cleanup/concurrency coverage and replace live fixed IDs or placeholder states with observable fixture outputs.
- [x] `tests/e2e/reporters/zero-live-skip-reporter.ts`, `tests/e2e/run-live-apphost.sh`, `.github/workflows/ci.yml`, `tests/tools/run-ci-workflow-gates.ps1`, `tests/e2e/.env.example`, `tests/e2e/README.md`, and `docs/runbooks/projects-topology.md` -- require every live input and own start/wait/one describe/smoke/full run/always-stop with zero skips, dynamic endpoints, and metadata-only artifacts.

**Acceptance Criteria:**
- Given the scheduled managed lane, when the explicit profile runs, then startup smoke and every collected live Chromium case execute with two-worker isolation, zero skips, dynamic endpoints, and exact-AppHost teardown even after failure.
- Given an anonymous browser, when it opens Projects UI and completes real Keycloak login, then protected content renders through a persistent HttpOnly server session, outbound Projects calls carry the user token server-side, and no access/refresh/id token is browser-readable.
- Given a valid live token and missing or existing tenant projection, when global setup completes, then supported tenant commands and the outer Projects authorization response converge without direct projection edits.
- Given reference, proposal, trace, warning/empty/feedback, retry, and cleanup scenarios, when live specs run concurrently, then observable API/UI state comes from disjoint fixture outputs and reverse cleanup preserves the primary failure with metadata-only diagnostics.
- Given the fixture profile is absent, when AppHost/UI start and routes are inspected, then no fixture resource/control ingress is reachable and authentication remains enforced.

## Spec Change Log

## Review Triage Log

Review 1 (2026-10-07) over `ae37c23`, the spec paths of `ada852f`, and the uncommitted tree. B = blind-hunter, E = edge-case-hunter, V = verification-gap.

| ID | Location | Verdict | Route | Evidence |
|----|----------|---------|-------|----------|
| E1 | `ProjectsDomainProcessor.RehydrateProjectState` | high | patch | EventStore persists `IRejectionEvent` results as normal stream events (`AggregateActor.cs:1323`); Projects rejection types implement only `IRejectionEvent`, so rehydration throws and every later command on that Project fails. |
| V6 | `ProjectsDomainProcessor.DeserializeSnapshot` | high | patch | EventStore snapshots the whole `DomainServiceCurrentState` (`AggregateActor.cs:1294-1300,1411`); Projects deserializes that nested shape as a flat `ProjectState`, losing all pre-snapshot history after the snapshot interval. |
| E2 | `ProjectProjectionHandler.Project` | medium | patch | The same persisted rejection events are not in `ProjectEventTypes`, so `/project` throws (500) on every replay of that aggregate. |
| V1 | `ProjectsDomainProcessorTests` | medium | patch | Pre-verified gap: no case supplies a non-null snapshot, nested snapshot-aware state, rejection history, or mismatched metadata. |
| V5 | `ProjectsServerModuleTests` | medium | patch | Pre-verified gap: the `/project` gap/format/unknown-type branches and the foreign-domain 404 are untested. |
| V2 / B19 | `ProjectsWorkersModule` subscriptions | medium | patch | Pre-verified gap: nothing posts a flat publisher payload to `/projects/events` or `/tenants/events` or asserts the `SUCCESS` acknowledgement. |
| V3 / B15 | `ProjectDiagnostics.ReloadAsync`, `ProjectDetailPageTests` | medium | patch | A reload that returns feedback replaces `_result` and removes the inspector with the confirmed action; the Succeeded/confirmed assertions were dropped. |
| V4 | `zero-live-skip-reporter.ts` | medium | patch | Pre-verified gap: only a source regex in the gate checks the reporter; no executable self-check. |
| B5 / E23 / E25 | `projects-console-shell`, `projects-reference-health` specs, fixture roles | medium | patch (AC4 restore, approved by the user 2026-10-07) | Live no-data/denied/unavailable/filtered empty, success/warning/error/loading feedback, and reference failure-state assertions were removed; fixture roles return only healthy states, so AC4's warning/empty/feedback scenarios are not exercised live. |
| B14 | `projects-proposal.spec.ts` confirm case | medium | patch | Asserts only 202 and a correlation id; folder/file links of the confirmed Project are never observed converging. |
| B10 | `projects-authentication.spec.ts` | medium | patch | Matrix row requires missing/expired sessions to challenge again; only a missing session is tested. |
| E15 | `run-live-apphost.sh` | medium | patch | `started=1` precedes `aspire start`; if the exact AppHost is already running, cleanup stops an instance the runner did not start. |
| B8 / B21 | `global-setup.ts`, `browser-session.ts`, runner, README, `merged-fixtures.ts` header | low | patch | `storageState` saves Keycloak-origin cookies and the token-bearing FrontComposer cookie and is never deleted after the run; README/comments claim only the server cookie persists and still mention auth-session. |
| B7 | `projects-proposal.spec.ts` `safeFailureSummary` | low | patch | The assertion message embeds `JSON.stringify(body)`, putting response bodies into JUnit/HTML artifacts. |
| B9 | touched `tests/e2e` files | low | patch | `.editorconfig` requires CRLF; `playwright.config.ts`, `tsconfig.json`, `merged-fixtures.ts`, `projects-fixtures.ts` became LF and `project-factory.ts`, `framework-smoke.spec.ts`, `package.json` became mixed. |
| E11 | `ProjectsUiSecurity.AddProjectsUiSecurity` | low | patch | `Uri.TryCreate("/realms/x", Absolute)` yields `file://` on Linux, so a non-HTTP authority passes the fail-closed check. |
| E18 | `scenarioForTest` | low | patch | The digest uses the leaf title; same-titled tests in different describe blocks of one spec collide. |
| E19 | `projects-authentication.spec.ts` traffic observer | low | patch | `request.headers()` omits security-related headers; `allHeaders()` is required for the no-Authorization check to be meaningful. |
| E20 | `projects-proposal.spec.ts` | low | patch | `trackProject` runs only on 202; a partially accepted confirm leaves its Project Active and untracked. |
| E21 | `AspireTopologyTests` disabled-profile test | low | patch | `DistributedApplication.CreateBuilder()` reads `Projects__E2E__LiveFixtures` from the environment; an exported value makes the test fail. |
| B6 | `projects-resolution.spec.ts` archived-exclusion case | low | defer | Pre-existing assertion design (only the placeholder id changed): no archived match is seeded, so `every` passes on an empty list. |
| E24 | fixture teardown inside the 60 s test timeout | maybe-false | defer | Unverified medium: whether Playwright aborts archive-to-convergence teardown after a timed-out test; settle with a forced-timeout run. |
| B1 / E4 | `/process`, `/project` routes | low | reject | The platform SDK's `MapEventStoreDomainService` maps the same callbacks anonymously; handlers are pure and persist nothing; hardening belongs to a platform-wide Dapr app-token policy. |
| B2 / E6 | snapshot tail continuity | low | reject | EventStore supplies contiguous reads; an extra continuity guard adds complexity for a state not demonstrated. |
| B3 / E8 | journal watermark | false | reject | `GetReadinessAsync` has no consumer outside the store; response freshness uses per-row sequences. |
| E7 | journal sort ties | false | reject | Positions are `GlobalPosition`, allocated uniquely by EventStore's `EventPersister` global position allocator. |
| E22 | `OutOfOrder` enum arm | low | reject | Dead switch arm only; removing a public enum member is more than a direct deletion. |
| E5 | array-form `currentState` | false | reject | `AggregateActor` always sends `DomainServiceCurrentState` or `null` (`AggregateActor.cs:1294-1300`). |
| E3 | adapted projection payloads | maybe-false | reject | Projects registers no event contract adapters; if adapters were configured the impact would be low. |
| E9 | current-tenant claim | low | reject | Signed Keycloak claim; the tenant-access projection still enforces principal membership downstream. |
| E10 | repeated freshness header lines | low | reject | Only reachable with two separate header lines; the API serves only eventual consistency anyway. |
| E12 | single post-action reload | maybe-false | reject | If a Conversations-owned action confirms before detail convergence the impact is a stale view until refresh (low). |
| B4 / E13 | fixture seed compensation on 409 | low | reject | Graph ids are attempt-derived and unique; collisions need misuse; the fix adds branches. |
| E14 | cancelled seed skips compensation | low | reject | Fixture-only, requires a client disconnect mid-seed; the fix adds cancellation handling. |
| E17 | archive registration after create retries | low | reject | Rare (accepted create followed by 5xx until timeout); earlier registration would add 30 s cleanup noise on rejected creates. |
| B11 | smoke reports overwritten | low | reject | The full run re-executes the smoke specs; separate output plumbing is more than a direct correction. |
| B12 | teardown diagnostics | low | reject | The runner turns a failed stop into a non-zero exit; the CI step is a redundant second stop. |
| B13 | fixture control ingress / Folders tenant | low | reject | Test-only profile gated by explicit enablement; no current case needs cross-tenant folder negatives. |
| B16 | child idempotency key migration | false | reject | EventStore's `SubmitCommandRequestValidator` regex rejects `:`, so every old-format child command failed; no in-flight state exists. |
| B17 | hardcoded UI client secret | low | reject | Local test realm secret, colocated with the realm's checked-in test credentials; the AppHost is local orchestration. |
| B18 | AppHost references the fixture project | low | reject | Compile-only reference (`IsAspireProjectResource=false`); resources are added only under the explicit profile. |
| B20 / E16 | readiness create-400 probe | false | reject | The create endpoint authorizes before parsing the body and the probe sends a canonical Idempotency-Key, so its 400 proves authorization. |

Patch outcome (review 1): every `patch` row above was applied by the implementation agent, and the AC4 row was resolved by letting the attempt graph drive Stale, Forbidden, and Unavailable conversation trust through the metadata-only roles. The orchestrator then fixed, after live runs: the invalidated-session case racing Keycloak's silent re-login redirect (it now waits for a settled UI or login destination); degraded conversations rendering as two rows (list plus context evaluation), so assertions cover every matching row; the unreachable references empty state, so an unlinked Project now asserts its explicit Pending folder lane and `project-empty-none` is recorded as live-unproducible; faker catch-phrases tripping payload markers, so generated description and setup text are hex-suffixed labels; and the AC3 folder-lane baseline racing the asynchronous pending folder row, so the case now asserts the lane invariant. The B9 line-ending fix was narrowed to restoring each file's HEAD style, avoiding whole-file rewrites of LF files.

## Design Notes

Keep direct API fixtures and the browser session separate: ROPC remains an E2E-only supported API setup seam, while UI proof uses Keycloak authorization code plus the FrontComposer HttpOnly cookie/token relay. The managed runner is the lifecycle owner; Playwright owns application assertions, not AppHost process control.

## Verification

**Commands:**
- `dotnet build Hexalith.Projects.slnx --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` -- all affected C# projects compile with warnings as errors.
- Build and invoke `Hexalith.Projects.UI.Tests` and `Hexalith.Projects.Integration.Tests` individually -- focused auth/profile/topology tests pass.
- `npm --prefix tests/e2e run typecheck` and the offline Chromium contract lane -- strict TypeScript passes and disabled live cases resolve no auth/network fixtures.
- `pwsh -NoProfile -File ./tests/tools/run-ci-workflow-gates.ps1` -- the recurring managed lifecycle, immutable actions, root-only submodules, zero-skip gate, and unconditional exact-AppHost stop are enforced.
- Documented managed live command -- AppHost start/wait/describe, startup/session/full Chromium lane with two workers and zero skips, metadata-only cleanup evidence, and stop all pass.
- `git diff --check` -- no whitespace errors.
