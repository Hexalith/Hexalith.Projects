---
title: 'Inspect Projects through an authenticated FrontComposer read surface'
type: 'feature'
created: '2026-08-25'
status: ready-for-dev
blocked_by: ['6.1', '6.2', '6.3', '6.4']
prerequisite_record: null
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

**Block If:** The accepted Story 6.1 list/open and AD-32 contracts, Story 6.2 Conversation-start contract, Story 6.3 context contract, or Story 6.4 transient-resolution contract and supported handlers are unavailable; the production identity boundary has no approved single-current-Tenant claim/client shape; or implementation would require edits to a sibling submodule. Do not invent local substitute contracts or promote legacy REST as the supported path.

**Prerequisite Gate:** Do not re-arm or dispatch Story 6.5 while deferred-work entry `DW-1` is `open`. Close `DW-1` only after `prerequisite_record` names a committed repository-relative record that pins immutable revisions and content hashes for the accepted Story 6.1 shared AD-32/list/open contracts, persisted read models, authorization seam, and supported handlers; the Story 6.2 Conversation-start contract and handler; the Story 6.3 context contracts, projections, and handlers; the Story 6.4 transient-resolution contracts and handlers; the approved FrontComposer/OIDC single-current-Tenant contract; and a G-4 module manifest with executable `reads` and `web-reads` profiles. Every named artifact must exist at its pinned revision. Legacy endpoints, generated REST clients, draft specifications, unchecked task lists, or an `awaiting-operator` status do not satisfy this gate.

**Never:** Expose sibling payloads, transcripts, prompts, paths, URIs, tokens, secrets, raw owner failures, unauthorized candidates, or denial detail; query or render audit history; present maintenance/write actions; select, preselect, confirm, persist, or imply a resolution choice; mutate Project, sibling, task, audit, or read state; hand-edit generated clients; cut over or remove legacy routes before Story 6.7; write or revert `sprint-status.yaml`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Prerequisite readiness | `DW-1` is open, `prerequisite_record` is null/missing, a named artifact is absent or hash-mismatched, or an acceptance is incomplete | No Story 6.5 development session starts; the orchestrator pauses at the story gate | Keep the story parked; do not substitute legacy REST, invent contracts, or treat planning/status artifacts as delivered capability |
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
- Given Story 6.5 is considered for dispatch, when readiness is evaluated, then `DW-1` is closed and `prerequisite_record` resolves to committed, hash-matching, accepted 6.1–6.4, identity, and G-4 artifacts; otherwise no development session starts.
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

**Commands:**
- `dotnet restore Hexalith.Projects.slnx && dotnet build Hexalith.Projects.slnx --configuration Debug` -- expected: clean build with warnings as errors.
- `dotnet test tests/Hexalith.Projects.UI.Tests/Hexalith.Projects.UI.Tests.csproj --configuration Debug` -- expected: authenticated route, canonical-state, safe-absence, disclosure, and component accessibility cases pass.
- `dotnet test tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj --configuration Debug --filter FullyQualifiedName~AspireTopologyTests` -- expected: the Projects UI receives the approved OIDC settings and depends on the identity provider.
- `npm --prefix tests/e2e test -- --project=chromium` -- expected: browser-to-supported-handler, isolation, zero-write, and accessibility journeys pass.
- `dotnet tool run hexalith-module test --profile reads --filter Story=6.5` -- expected: the approved G-4 profile emits real persisted evidence for the browser-to-handler path.
- `git diff --check` -- expected: no whitespace errors.

