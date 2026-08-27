---
title: 'Live AppHost E2E readiness'
type: 'feature'
created: '2026-08-27'
status: 'in-progress'
baseline_revision: '9b8ba049a8329a6346311782bd3311d3e492a3dd'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md'
warnings: [multiple-goals, oversized]
deferred: []
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
- `src/Hexalith.Projects.AppHost/{Program.cs,ProjectsLiveE2EFixtureProfile.cs,KeycloakRealms/hexalith-realm.json,Hexalith.Projects.AppHost.csproj}`, `Hexalith.Projects.slnx`, and `tests/Hexalith.Projects.Integration.Tests/{AspireTopologyTests.cs,DaprConfigurationTests.cs}` -- wire the explicit fixture graph and confidential UI OIDC client; prove enabled resources and disabled-profile no-ingress structurally.
- `src/Hexalith.Projects.UI/{Program.cs,Components/Routes.razor}` and `tests/Hexalith.Projects.UI.Tests/Authentication/**` -- enable FrontComposer server security conditionally, protect routes/endpoints, relay the signed-in token through `AddProjectsClient()`, and verify cookie/code-flow/session composition plus auth-disabled startup.
- `tests/Hexalith.Projects.E2E.Fixtures/**` -- make graph DTOs symmetric and return typed reverse-order attempted-role/status cleanup results while keeping all ingress metadata-only.
- `tests/e2e/{global-setup.ts,playwright.config.ts,support/merged-fixtures.ts,support/factories/live-fixture-identities.ts,support/fixtures/*.ts,support/helpers/*.ts}` -- finish token-derived tenant readiness, deterministic caller-owned Projects/sibling graphs, two-worker isolation, archived convergence, typed cleanup evidence, and browser OIDC storage state.
- `tests/e2e/specs/{live-fixture-identities,live-fixtures-lifecycle,live-apphost-startup,projects-authentication,projects-file-reference,projects-resolution,projects-proposal,projects-resolution-trace,projects-reference-health,projects-console-shell}.spec.ts` -- add startup/session/cleanup/concurrency coverage and replace live fixed IDs or placeholder states with observable fixture outputs.
- `tests/e2e/reporters/zero-live-skip-reporter.ts`, `tests/e2e/run-live-apphost.sh`, `.github/workflows/ci.yml`, `tests/tools/run-ci-workflow-gates.ps1`, `tests/e2e/.env.example`, `tests/e2e/README.md`, and `docs/runbooks/projects-topology.md` -- require every live input and own start/wait/one describe/smoke/full run/always-stop with zero skips, dynamic endpoints, and metadata-only artifacts.

**Acceptance Criteria:**
- Given the scheduled managed lane, when the explicit profile runs, then startup smoke and every collected live Chromium case execute with two-worker isolation, zero skips, dynamic endpoints, and exact-AppHost teardown even after failure.
- Given an anonymous browser, when it opens Projects UI and completes real Keycloak login, then protected content renders through a persistent HttpOnly server session, outbound Projects calls carry the user token server-side, and no access/refresh/id token is browser-readable.
- Given a valid live token and missing or existing tenant projection, when global setup completes, then supported tenant commands and the outer Projects authorization response converge without direct projection edits.
- Given reference, proposal, trace, warning/empty/feedback, retry, and cleanup scenarios, when live specs run concurrently, then observable API/UI state comes from disjoint fixture outputs and reverse cleanup preserves the primary failure with metadata-only diagnostics.
- Given the fixture profile is absent, when AppHost/UI start and routes are inspected, then no fixture resource/control ingress is reachable and authentication remains enforced.

## Spec Change Log

## Review Triage Log

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
