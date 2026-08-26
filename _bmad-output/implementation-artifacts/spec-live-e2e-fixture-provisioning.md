---
title: 'Live E2E fixture provisioning'
type: 'feature'
created: '2026-08-26'
status: ready-for-dev
baseline_revision: '1e9f847169c94e275d6c7277fdb5d2d040cefc87'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** AppHost-backed Projects E2E cannot reach application behavior deterministically because the selected tenant may lack a projected access grant, sibling domains have no provisioned reference state, UI scenarios use placeholder project IDs, and several live tests reuse fixed identifiers.

**Approach:** Provision deterministic projected-Tenant access plus sibling reference, proposal, and UI-state fixtures with per-run identifiers and cleanup. Replace fixed live IDs with fixture outputs and add parallel-safety and convergence coverage.

## Boundaries & Constraints

**Always:** Use supported authenticated APIs and observable convergence checks; derive the authoritative tenant and principal from the live token; keep the shared tenant repair idempotent and serialized; generate bounded identifiers from run, worker, retry, repeat, and scenario identity; archive created Projects and wait for archived convergence; keep sibling fixtures metadata-only and accessible only in an explicitly enabled AppHost E2E profile.

**Block If:** The installed Tenants/EventStore contracts cannot safely converge an existing tenant membership, or a required sibling behavior cannot be represented through its public HTTP/client contract without changing a sibling repository or writing a production projection directly.

**Never:** Edit the deferred-work ledger or bundle intent; allow an implementation subagent to edit this spec or another workflow-owned control artifact, including frontmatter status or baseline fields, task checkboxes, change or triage logs, and auto-run results (the subagent reports completion and its exact changed paths in its handoff only); initialize nested submodules; seed Projects/Tenant projections directly; add production backdoors; hard-delete or disable the shared tenant; use fixed live entity IDs, guessed ports, sleeps, or browser interception as API fixture provisioning.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| First live run | Authorized token but missing/stale Projects tenant projection | Supported tenant commands reach terminal success and Projects list reaches 200 before tests start | Bounded poll reports command/status response and last Projects response |
| Existing tenant | Tenant and principal membership already exist | Provisioning is idempotent and refreshes only when needed | Domain rejection is accepted only when postcondition converges |
| Parallel workers | Same run, different worker/retry/repeat/scenario dimensions | Every Project, conversation, folder, file, workspace, proposal, correlation, task, and idempotency ID is deterministic and disjoint | Duplicate dimensions fail a factory contract test |
| Fixture failure | Setup, test, or teardown fails | Already-created resources are cleaned in reverse order; primary failure is preserved | Cleanup failures are attached as diagnostics |
| E2E profile disabled | Normal AppHost/server startup | No fixture ingress or sibling stub registration is reachable | Fail closed; production clients retain normal sibling addresses |

</intent-contract>

## Code Map

- `src/Hexalith.Projects.AppHost/Program.cs` -- current EventStore, Tenants, Projects, workers, UI topology and E2E-profile composition point.
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs` -- production sibling client registrations that the E2E profile must satisfy without weakening defaults.
- `tests/e2e/global-setup.ts` -- live credential validation/token warm-up and serialized tenant readiness gate.
- `tests/e2e/playwright.config.ts` -- live environment contract and current single-worker restriction.
- `tests/e2e/support/merged-fixtures.ts` -- composed tenant and seeded-Project fixtures consumed by specs.
- `tests/e2e/support/fixtures/projects-fixtures.ts` -- Project create/convergence/archive lifecycle.
- `tests/e2e/support/helpers/readiness.ts` -- bounded polling primitive.
- `tests/e2e/specs/projects-{file-reference,resolution,proposal,resolution-trace,reference-health,console-shell}.spec.ts` -- fixed IDs and placeholder states to replace.
- `tests/e2e/README.md`, `docs/runbooks/projects-topology.md`, `tests/e2e/.env.example` -- live runner discovery and fixture-profile operator contract.
- `.bmad-loop/runs/20260826-164827-cb61/bundles/live-e2e-fixture-provisioning/intent.md` and `_bmad-output/implementation-artifacts/spec-5-12-live-apphost-operational-console-verification.md` -- read-only source evidence.

## Tasks & Acceptance

**Execution:**
- `tests/e2e/support/factories/live-fixture-identities.ts` and `tests/e2e/specs/live-fixture-identities.spec.ts` -- add a pure bounded identity factory and verify deterministic, collision-free run/worker/retry/repeat/scenario dimensions.
- `tests/e2e/support/helpers/eventstore-api-client.ts`, `tests/e2e/support/helpers/tenant-access-readiness.ts`, and `tests/e2e/global-setup.ts` -- submit/poll authenticated tenant commands and gate live execution on the outer Projects authorization response.
- `tests/Hexalith.Projects.E2E.Fixtures/Hexalith.Projects.E2E.Fixtures.csproj` and its one-type-per-file C# sources -- implement metadata-only Conversations/Folders/Memories public contracts plus a run-scoped control API.
- `src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj`, `src/Hexalith.Projects.AppHost/Program.cs`, `Hexalith.Projects.slnx`, and `tests/Hexalith.Projects.Integration.Tests/AspireTopologyTests.cs` -- compose and verify the sibling fixture roles only under the explicit E2E profile, fail closed otherwise.
- `tests/e2e/support/helpers/live-fixtures-api-client.ts`, `tests/e2e/support/fixtures/live-fixtures.ts`, `tests/e2e/support/fixtures/projects-fixtures.ts`, and `tests/e2e/support/merged-fixtures.ts` -- seed the per-test fixture graph, expose its IDs/states, and clean in reverse order while waiting for Project archival.
- `tests/e2e/specs/projects-file-reference.spec.ts`, `tests/e2e/specs/projects-resolution.spec.ts`, `tests/e2e/specs/projects-proposal.spec.ts`, `tests/e2e/specs/projects-resolution-trace.spec.ts`, `tests/e2e/specs/projects-reference-health.spec.ts`, and `tests/e2e/specs/projects-console-shell.spec.ts` -- replace fixed IDs and placeholder routes with fixture outputs and observable API/UI scenarios.
- `tests/e2e/specs/live-fixtures-lifecycle.spec.ts` and `tests/e2e/playwright.config.ts` -- verify convergence, reverse cleanup/failure diagnostics, and enable two-worker live isolation.
- `tests/e2e/.env.example`, `tests/e2e/README.md`, and `docs/runbooks/projects-topology.md` -- document dynamic EventStore/fixture endpoint discovery, explicit profile startup, and cleanup.

**Acceptance Criteria:**
- Given a clean supported AppHost and valid live credentials, when global setup completes, then the token tenant/principal is present and fresh in Projects authorization without manual database or projection edits.
- Given two live workers and retries/repeats, when fixture graphs run concurrently, then their identifiers and sibling state are disjoint and each graph tears down without affecting another.
- Given reference, resolution, proposal, trace, warning, empty, and feedback scenarios, when live specs execute, then state comes from fixture outputs and assertions observe real Projects API/UI behavior.
- Given the E2E profile is absent, when AppHost starts, then no fixture control surface or stub sibling binding is exposed.

## Spec Change Log

## Review Triage Log

## Design Notes

The test host is a runner-owned compatibility fixture, not a replacement sibling implementation. AppHost may launch role-specific instances under the sibling app identities only when the explicit live-fixture profile is enabled; seed/reset operations remain scoped by run identity and expose metadata required by Projects, never content payloads.

## Verification

**Commands:**
- `dotnet build Hexalith.Projects.slnx --no-restore` -- expected: all projects compile with warnings as errors.
- `dotnet test tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj --no-build --no-restore` -- expected: fixture-profile and fail-closed coverage passes.
- `npm --prefix tests/e2e run typecheck` -- expected: strict TypeScript checks pass.
- `npm --prefix tests/e2e test -- --workers=2` with the documented AppHost fixture profile -- expected: live suite uses disjoint fixtures, converges, and cleans up.

