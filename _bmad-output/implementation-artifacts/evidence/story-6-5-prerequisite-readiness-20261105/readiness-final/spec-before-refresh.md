---
title: 'Inspect Projects through an authenticated FrontComposer read surface'
type: 'feature'
created: '2026-08-25'
status: blocked
baseline_revision: dffdd677e0058240d5951f507cbd2b159b9643a5
blocked_by: []
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** The existing FrontComposer UI reads Projects through legacy generated REST clients without authenticating its Blazor Server circuit or relaying the user token. It cannot prove the supported Epic 6 query path, coherent AD-32 state, Tenant isolation, metadata-only disclosure, or read-only behavior.

**Approach:** After Stories 6.1–6.4 deliver their supported query contracts and handlers, wire the Projects FrontComposer host to the platform OIDC bridge and per-circuit token relay, then adapt inventory and detail journeys to those exact contracts. Preserve the legacy path only for Story 6.7 shadow comparison.

## Boundaries & Constraints

**Always:** Derive identity and current Tenant from authenticated server context; use FrontComposer server security and circuit-safe gateway authorization; render only authorization-filtered metadata; preserve one coherent version-bound `Complete|Partial|Unavailable` snapshot with `asOf`, disclosable `projectVersion`, components, omissions, and recovery actions; use Fluent UI V5 and accessible page structure; prove zero domain writes and no cross-user token bleed.

**Block If:** The accepted Story 6.1 list/open and AD-32 contracts, Story 6.2 Conversation-start contract, Story 6.3 context contract, or Story 6.4 transient-resolution contract and supported handlers are unavailable; the FrontComposer server-authentication/current-Tenant/per-circuit credential seam is unusable; or implementation would require edits to a sibling submodule. Do not invent local substitute contracts or promote legacy REST as the supported path.

**Prerequisite Gate (explicitly renegotiated 2026-11-02 under approved E4):** Do not dispatch while DW-1 is open. Consume accepted 6.1–6.4 contracts and supported handlers plus usable FrontComposer server authentication, current-Tenant and per-circuit gateway credentials. Record accepting commits and test references in DW-1 and ordinary story prerequisites; close DW-1 only when those inputs exist and are usable, then run focused spec readiness. Common-runner publication, package/rollback qualification, live production enrollment and a second immutable hash inventory are not dispatch requirements. Anonymous UI, token bleed, absent read contracts, substituted legacy REST or missing owner authority still block. Current prerequisite result (2026-11-05): 6.1–6.4 are accepted/done and DW-1 is closed for the verified FrontComposer source checkout plus repair. The unchanged owner gitlink alone is not repaired. Ordinary accepting revisions, exact repair patch and passing tests are recorded in the current prerequisite section below. This story remains blocked for spec/consumer-test readiness; no implementation dispatch is authorized.

**Never:** Expose sibling payloads, transcripts, prompts, paths, URIs, tokens, secrets, raw owner failures, unauthorized candidates, or denial detail; query or render audit history; present maintenance/write actions; select, preselect, confirm, persist, or imply a resolution choice; mutate Project, sibling, task, audit, or read state; hand-edit generated clients; cut over or remove legacy routes before Story 6.7; write or revert `sprint-status.yaml`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Prerequisite readiness | DW-1 is open, ordinary prerequisite/evidence references show an absent read contract/handler, unusable authentication/current-Tenant/per-circuit relay, or incomplete acceptance | No Story 6.5 development session starts | Keep the story parked; no legacy substitute, anonymous UI or token bleed; close DW-1 only on usable accepted inputs |
| Anonymous route | Browser opens inventory or detail | FrontComposer starts the approved sign-in flow | No protected UI or gateway call is rendered |
| Authorized inventory/detail | Authenticated current-Tenant user; supported read is complete or degraded | Stable paged inventory and one metadata-only detail snapshot render canonical state, freshness, omissions, and recovery | Authorized degradation distinguishes stale, rebuilding, and unavailable without raw diagnostics |
| Protected target | Absent, denied, cross-Tenant, or unverifiable Project | One generic safe-absence presentation | No protected identifiers, candidate facts, or distinguishable denial reason |
| Resolution inspection | Authorized request-scoped candidate computation | Authorized candidates, rank, score, reasons, inputs, current components, and recovery render as non-selecting evidence | `SingleCandidate` is labelled as a candidate, never as resolved |

</intent-contract>

## Code Map

- `src/Hexalith.Projects.UI/Program.cs` -- add FrontComposer server authentication/endpoints and attach gateway authorization to `AddProjectsClient()`.
- `src/Hexalith.Projects.UI/Components/Routes.razor` -- replace plain `RouteView` with authenticated route handling and a local challenge redirect.
- `src/Hexalith.Projects.AppHost/Program.cs` -- replace dependency-only UI security wiring with approved OIDC configuration injection.
- `src/Hexalith.Projects.AppHost/KeycloakRealms/hexalith-realm.json` -- local-only confidential client, redirects, audience, and single current-Tenant claim fixture; production provisioning remains external.
- `src/Hexalith.Projects.UI/Services/ProjectInventorySource.cs` -- replace legacy REST inventory reads with the delivered supported paged query; remove random GUID/session-authority placeholders.
- `src/Hexalith.Projects.UI/Services/ProjectDetailSource.cs` -- consume one delivered coherent detail/context/setup result; remove audit and conversation fan-out.
- `src/Hexalith.Projects.UI/Services/ProjectWarningsDashboardSource.cs` -- remove per-row diagnostic N+1 reads.
- `src/Hexalith.Projects.UI/Components/Pages/Home.razor` -- render authorized, bounded inventory states with Fluent UI V5 and no raw interactive controls.
- `src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor` -- render metadata/setup/context/reference/resolution evidence; replace audit with explicit unavailable copy and remove maintenance actions.
- `src/Hexalith.Projects.UI/Services/ProjectResolutionTraceMapper.cs` -- stop describing `SingleCandidate` as selected or resolved.
- `tests/Hexalith.Projects.UI.Tests/` -- cover authentication gates, canonical states, generic absence, no audit/actions, accessibility, and zero protected leakage.
- `tests/Hexalith.Projects.Integration.Tests/AspireTopologyTests.cs` and `tests/e2e/` -- prove OIDC topology, interactive login, per-user token relay, two-Tenant isolation, supported-handler traversal, zero writes, keyboard/focus, 320px reflow, and 200% zoom.
- `references/Hexalith.FrontComposer/` -- read-only source of `AddHexalithFrontComposerServerSecurity`, `AddFrontComposerGatewayAuthorization`, and authentication endpoint patterns.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.Projects.UI/Program.cs`, `src/Hexalith.Projects.UI/Components/Routes.razor`, `src/Hexalith.Projects.AppHost/Program.cs`, and `src/Hexalith.Projects.AppHost/KeycloakRealms/hexalith-realm.json` -- wire and gate the authenticated server circuit and bearer relay using approved provider settings.
- `src/Hexalith.Projects.UI/Services/` and `src/Hexalith.Projects.UI/Components/Pages/` -- consume the exact delivered 6.1–6.4 contracts, remove legacy fan-out/audit/write affordances, and render canonical metadata-only states with Fluent UI V5.
- `tests/Hexalith.Projects.UI.Tests/`, `tests/Hexalith.Projects.Integration.Tests/AspireTopologyTests.cs`, and `tests/e2e/` -- add outer-surface security, state, isolation, read-only, and accessibility evidence.

**Acceptance Criteria:**
- Given Story 6.5 is considered for dispatch, when readiness is evaluated, then DW-1 is done and ordinary accepting-commit/test references confirm usable accepted 6.1–6.4 contracts/handlers and the FrontComposer authentication/current-Tenant/per-circuit gateway credential seam; otherwise dispatch remains blocked.
- Given an anonymous browser, when a Project route is requested, then the approved sign-in journey starts and no protected read or content is exposed.
- Given two authenticated users in separate circuits and Tenants, when they inspect inventory and detail, then each supported gateway call relays only that user token and only authorization-filtered metadata renders.
- Given complete, partial, stale, rebuilding, unavailable, archived, or authoritative-empty evidence, when the supported result renders, then the UI preserves the exact canonical state, version, component, omission, and recovery meaning without inference.
- Given an absent, denied, cross-Tenant, or unverifiable target, when it is opened, then the browser presents the same generic safe absence and exposes no distinguishing metadata.
- Given context or transient resolution evidence, when it renders, then no sibling payload, audit query, write action, persisted choice, or claim that a single candidate is resolved appears.
- Given authenticated keyboard, assistive-technology, 320px, and 200% zoom journeys, when the updated surfaces are exercised, then navigation, focus, labels, non-color meaning, landmarks, and reflow remain usable.
- Given any successful, degraded, denied, or faulted inspection, when before/after state and telemetry are compared, then no domain event/state mutation or token/protected-value disclosure occurred.

## Spec Change Log

### 2026-08-25 — Escalation resolution: prerequisite hard gate
- Decision: preserve the prior-only Epic 6 sequence and park Story 6.5 until the supported Story 6.1–6.4 read capabilities and their required identity and evidence inputs are delivered and accepted.
- Clarified: `DW-1` is the scheduler-enforced dispatch gate, `prerequisite_record` is the immutable evidence index used to close it, and legacy REST or planning-only artifacts cannot satisfy readiness.

## Review Triage Log

## Design Notes

FrontComposer owns the session credential and relay; Projects owns authorization policy and response disclosure. Story 6.5 must compose delivered Projects contracts and cannot define a second AD-32 vocabulary. The existing generated REST surface remains a shadow input only.

## Verification

These are future verification commands after DW-1 closes and focused readiness passes; none ran for this correction. Run each Debug/source test project separately and invoke its built xUnit v3 assembly directly. From the repository root:

```bash
mkdir -p evidence/epic6
dotnet build tests/Hexalith.Projects.UI.Tests/Hexalith.Projects.UI.Tests.csproj --configuration Debug
dotnet tests/Hexalith.Projects.UI.Tests/bin/Debug/net10.0/Hexalith.Projects.UI.Tests.dll -xml evidence/epic6/6.5-ui.xml
dotnet build tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj --configuration Debug
dotnet tests/Hexalith.Projects.Integration.Tests/bin/Debug/net10.0/Hexalith.Projects.Integration.Tests.dll -class '*AspireTopologyTests' -xml evidence/epic6/6.5-topology.xml
```

Require nonempty native XML with the expected authenticated route/canonical-state/safe-absence/disclosure/accessibility cases and topology assertions; record actual pass/fail/skip totals and tested commits. Do not use project-level Microsoft.Testing.Platform filters.

For authenticated browser-to-supported-handler, two-circuit isolation, zero-write and accessibility journeys, use the existing managed route from `tests/e2e/README.md` after supplying its controlled local credentials through the established environment:

```bash
npm --prefix tests/e2e run test:live:managed
```

The runner discovers real Aspire endpoints, owns exact lifecycle/teardown and runs the existing Chromium journey route. Retain its actual `tests/e2e/test-results/junit.xml` and `tests/e2e/playwright-report/index.html`, plus required authenticated manual accessibility evidence, with native XML and command/commit references. Missing supported browser coverage is a feature gap to verify, never a pass from offline selector tests. No credentials, tokens, browser storage state or protected payloads enter retained evidence. Story 6.5 remains blocked and DW-1 open; no browser or .NET execution is claimed here.

## Auto Run Result

Status: blocked
Blocking condition (historical run, now superseded by the dated correction below): DW-1 lacked accepted prerequisite/evidence references. The earlier separate prerequisite_record ceremony is no longer an active requirement.

No implementation session started, no production or test files changed, and no implementation verification commands ran because the prerequisite-readiness matrix requires this exact state to remain parked.

### Approved prerequisite correction — 2026-11-02

Jerome approved E1–E7 of [the November 1 proposal](../planning-artifacts/sprint-change-proposal-2026-11-01.md). Only the frozen prerequisite gate is renegotiated above; product/security/UI acceptance remains unchanged. The old separate prerequisite_record/hash/G-4 ceremony in the dated August log and Auto Run Result is historical. Current blocking condition is ordinary dependency acceptance: Story 6.1 is unaccepted and usable FrontComposer authentication/current-Tenant/per-circuit relay evidence must be recorded in DW-1. Story 6.4 is done. DW-1 stays open; no implementation or UI acceptance follows from this correction.


### Initial prerequisite readiness — 2026-11-05 (historical, superseded below)

Story 6.1 is now accepted; Stories 6.2–6.4 remain done. This supersedes earlier factual statements in this document that 6.1 is unaccepted, without changing the approved prerequisite gate or product/security criteria. Full accepting revisions, historical test references and current checks are in [the readiness packet](evidence/story-6-5-prerequisite-readiness-20261105/README.md) and DW-1.

**Current blocker:** FrontComposer `4c24c287cde520631e960a322413bee804f7c3d4` stores credentials under the subject alone. An executable using unchanged owner relay sources proves that, after another Tenant session signs in with the same subject, the original circuit relays the second session's credential. Three initial-session/second-session/distinct-user controls pass; the session-isolation invariant fails (executable exit 1, build exit 0 with zero warnings/errors). This is a controlled, network-free boundary reproduction, not a live disclosure claim.

The server-authentication/current-Tenant APIs exist, but the credential seam does not satisfy the no-token-bleed requirement. The correction belongs to FrontComposer; a Projects-owned token store would duplicate platform plumbing. DW-1 stays open and Story 6.5 blocked. No implementation session was dispatched, no sibling implementation changed, and sprint status was not edited. The UI test-project build separately stops on the missing Platform-selected Conversations source mapping; its exact command/output and narrower executed reproduction are retained in the packet.

Planning handoff: actual adapters live in `src/Hexalith.Projects.UI/Diagnostics/`; accepted open/context/start results have separate snapshots and cannot be merged into an invented coherent result. A future Build planning pass must use those delivered semantics after the owner credential repair passes focused checks. No replacement draft was advanced or implementation approval requested while the prerequisite remains unsatisfied.

### Verified prerequisite repair and focused readiness — 2026-11-05

**DW-1 is done for the verified source checkout.** Stories 6.1–6.4 retain the ordinary accepting commits/tests in [the readiness packet](evidence/story-6-5-prerequisite-readiness-20261105/README.md). FrontComposer base `4c24c287cde520631e960a322413bee804f7c3d4` plus [the retained owner repair](evidence/story-6-5-prerequisite-readiness-20261105/repair/owner-source.patch) binds relayed credentials to a generated authenticated sign-in session, stable subject and current Tenant. Debug owner test-project build: exit 0, zero warnings/errors. Focused native suite: **114 passed, zero failed/errors/skipped/not-run**, including same-subject concurrent sessions across and within Tenants, distinct users, missing/expired credentials, final-ticket/protected-cookie round trips, sign-out and captured SignalR providers. Projects UI source builds under the correct Platform-selected Projects mapping with zero warnings/errors. [Exact commands, source references, native XML and review disposition](evidence/story-6-5-prerequisite-readiness-20261105/repair/README.md) scope this verification to the uncommitted repair; no new repaired accepting commit or dependency/gitlink update is claimed.

**Focused 6.5 readiness: blocked; planning refresh required before dispatch.** The adapter map above is stale (`Diagnostics/`, not `Services/`); existing Program/Routes/ProjectsUiSecurity already compose authentication. List enumeration and rows, open, context and Conversation-start expose separate snapshots; the combined coherent detail/context/setup result named above does not exist. Advisory resolution has its own evaluation/incompleteness evidence, not context admission authority. A future Build planning pass must reconcile those facts against the delivered Contracts/Queries and SDK query client without weakening product/security acceptance. The existing UI authentication composition fixture also calls the removed subject-only store API: its mapped test-project build fails with one CS1503 at line 122. The concrete synthetic-fixture migration is retained but unapplied pending permission outside the authorized owner source/test scope. These current spec/consumer-test readiness findings keep status blocked even though the dependency gate is satisfied.

No Story 6.5 implementation, browser/accessibility acceptance, domain read/write or sprint-status edit occurred. Historical initial failure text and evidence remain dated history; they do not reopen the repaired prerequisite.
