---
title: 'EventStore Security Service in Projects AppHost'
type: 'chore'
created: '2026-06-26'
status: 'done'
baseline_commit: '1f9c85dd26cbde96ba76704ccaffc514afeb76ce'
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-26.md'
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent -- do not modify unless human renegotiates">

## Intent

**Problem:** `Hexalith.Projects.AppHost` still creates Keycloak directly and hand-wires JWT bearer environment variables, while EventStore and Tenants now initialize local security through `HexalithEventStoreSecurityExtensions.AddHexalithEventStoreSecurity()`. This duplicates security topology logic and lets Projects drift from the shared Hexalith AppHost pattern.

**Approach:** Replace the direct Keycloak/JWT block with the shared EventStore Aspire security helper, then update structural tests and operator documentation so the Projects topology is described as a shared security resource backed by Keycloak when enabled.

## Boundaries & Constraints

**Always:** Keep the change scoped to AppHost topology composition, topology tests, Story 1.9 notes, and runbook wording. Use central package management and project references without inline package versions. Keep `EnableKeycloak=false` behavior intact through the shared helper. Keep `projects-ui` behavior unchanged unless its startup code is explicitly updated to consume FrontComposer/OIDC settings.

**Ask First:** Ask before adding interactive UI authentication, changing the Keycloak realm file, introducing new packages beyond the shared EventStore Aspire project reference, changing Dapr component topology, changing EventStore/Tenants submodule code, or editing generated files.

**Never:** Do not add direct Redis, direct Keycloak client, or direct infrastructure access to Contracts, Client, domain core, Server business logic, Workers handlers, CLI, MCP, or UI. Do not hand-edit generated `.g.cs` files. Do not initialize nested submodules. Do not pass misleading unused JWT settings to `projects-ui` or `projects-workers` unless their startup code consumes those settings.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Security enabled | `EnableKeycloak` unset or not `false` | AppHost creates the shared EventStore security resource through `AddHexalithEventStoreSecurity()` and wires JWT-bearing resources through helper extensions | Startup dependency remains on the shared security resource |
| Security disabled | `EnableKeycloak=false` | `AddHexalithEventStoreSecurity()` returns null; AppHost does not apply JWT/OIDC helper wiring and preserves the existing no-Keycloak local fallback | No direct Keycloak fallback block is reintroduced |
| UI unchanged | `projects-ui` still lacks OIDC startup composition | AppHost only uses `WithSecurityDependency(security)` for `projects-ui` or leaves it otherwise behavior-compatible | Interactive OIDC wiring is deferred unless UI startup is changed in the same approved scope |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Projects.AppHost/Program.cs` -- current direct Keycloak creation and local `ConfigureJwt(...)`; primary implementation target.
- `src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj` -- AppHost references/packages; needs EventStore Aspire helper reference and no direct Keycloak package if unused.
- `tests/Hexalith.Projects.Integration.Tests/AspireTopologyTests.cs` -- structural topology tests; add security-helper regression coverage.
- `docs/runbooks/projects-topology.md` -- local topology operator docs; describe shared security resource abstraction.
- `_bmad-output/implementation-artifacts/1-9-aspire-dapr-workers-topology-operational-skeleton.md` -- completed story evidence/notes; update wording to the current security helper pattern.
- `_bmad-output/planning-artifacts/architecture.md` -- topology wording; record shared EventStore security helper as the security composition pattern.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj` -- reference `Hexalith.EventStore.Aspire` as `IsAspireProjectResource="false"` and remove direct `Aspire.Hosting.Keycloak` if no direct symbol remains -- share the security helper implementation.
- [x] `src/Hexalith.Projects.AppHost/Program.cs` -- replace direct `AddKeycloak(...)`, `realmUrl`, and `ConfigureJwt(...)` with `AddHexalithEventStoreSecurity()` plus `WithJwtBearerSecurity(...)` / `WithSecurityDependency(...)` calls -- centralize AppHost security wiring.
- [x] `tests/Hexalith.Projects.Integration.Tests/AspireTopologyTests.cs` -- add structural assertions for `AddHexalithEventStoreSecurity(`, `WithJwtBearerSecurity(security)`, no direct `AddKeycloak("keycloak"`, and current UI dependency behavior -- prevent regression to hand-rolled wiring.
- [x] `docs/runbooks/projects-topology.md` -- update expected resources and start/stop text to refer to the shared security resource backed by Keycloak -- keep operator docs aligned with code.
- [x] `_bmad-output/implementation-artifacts/1-9-aspire-dapr-workers-topology-operational-skeleton.md` and `_bmad-output/planning-artifacts/architecture.md` -- update minimal topology wording only -- preserve planning evidence consistency.

**Acceptance Criteria:**
- Given the Projects AppHost source, when inspected, then it initializes security with `builder.AddHexalithEventStoreSecurity()` and contains no direct `builder.AddKeycloak("keycloak", 8180)` block.
- Given security is enabled, when resources are wired, then EventStore, Tenants, and Projects API use `WithJwtBearerSecurity(security)` and resources without JWT consumers use dependency-only wiring unless explicitly upgraded.
- Given security is disabled, when `AddHexalithEventStoreSecurity()` returns null, then the AppHost keeps the no-Keycloak local fallback and does not attempt direct Keycloak setup.
- Given topology tests run, when `AspireTopologyTests` executes, then it protects the shared-helper pattern and existing Dapr/Redis topology assertions.

## Spec Change Log

- 2026-06-26: Verified current workspace already implements the approved AppHost shared security helper correction; marked execution tasks complete.

## Design Notes

The shared helper names the Aspire resource `security` by default, while the backing implementation remains Keycloak. Operator-facing text should use the abstraction first and mention Keycloak only as the local backing implementation. `projects-ui` currently does not call FrontComposer authentication setup, so wiring OIDC settings into it would be behaviorally misleading unless the UI startup is changed in an explicit follow-up.

## Verification

**Commands:**
- `dotnet build Hexalith.Projects.slnx` -- passed on 2026-06-26 with 0 warnings and 0 errors.
- `dotnet test tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj --filter AspireTopologyTests` -- passed on 2026-06-26: 6 passed, 0 failed, 0 skipped.
- `git diff --check` -- passed on 2026-06-26 with no whitespace errors.

Review note: quick-dev sub-agent review was not run because spawning sub-agents requires explicit authorization in this session.

## Suggested Review Order

**Security Topology**

- Start at the shared helper initialization replacing direct Keycloak wiring.
  [`Program.cs:21`](../../src/Hexalith.Projects.AppHost/Program.cs#L21)

- Verify JWT-bearing resources use shared helper wiring only.
  [`Program.cs:55`](../../src/Hexalith.Projects.AppHost/Program.cs#L55)

- Confirm UI and workers receive dependency-only security wiring.
  [`Program.cs:60`](../../src/Hexalith.Projects.AppHost/Program.cs#L60)

**Project Boundary**

- Confirm EventStore Aspire is a non-resource helper reference.
  [`Hexalith.Projects.AppHost.csproj:13`](../../src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj#L13)

**Regression Coverage**

- Structural test locks in helper use and rejects direct Keycloak wiring.
  [`AspireTopologyTests.cs:70`](../../tests/Hexalith.Projects.Integration.Tests/AspireTopologyTests.cs#L70)

**Planning And Ops Evidence**

- Story 1.9 acceptance criteria now name the shared security resource.
  [`1-9-aspire-dapr-workers-topology-operational-skeleton.md:22`](1-9-aspire-dapr-workers-topology-operational-skeleton.md#L22)

- Architecture records the shared EventStore security helper in topology.
  [`architecture.md:294`](../planning-artifacts/architecture.md#L294)

- Runbook explains security disablement and expected resource naming.
  [`projects-topology.md:23`](../../docs/runbooks/projects-topology.md#L23)
