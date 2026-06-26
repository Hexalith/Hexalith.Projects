# Sprint Change Proposal: EventStore Security Service in Projects AppHost

Date: 2026-06-26
Project: Projects
Requested by: Jerome
Approval: approved by Jerome on 2026-06-26
Workflow mode: Batch

## 1. Issue Summary

The Projects Aspire AppHost currently creates and wires Keycloak directly instead of initializing the shared EventStore security service through `HexalithEventStoreSecurityExtensions.AddHexalithEventStoreSecurity()`.

This creates topology drift from the current Hexalith EventStore and Tenants AppHost pattern. The shared helper now owns the local security resource abstraction, Keycloak enable/disable behavior, realm import defaults, persistent fast-start knobs, JWT bearer environment wiring, EventStore service credentials, OpenID Connect settings, and security dependency wiring.

Evidence:

- `src/Hexalith.Projects.AppHost/Program.cs` manually declares `IResourceBuilder<KeycloakResource>? keycloak`, calls `builder.AddKeycloak("keycloak", 8180)`, builds a realm URL, and uses a local `ConfigureJwt(...)` helper to set `Authentication__JwtBearer__*` environment variables.
- `Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreSecurityExtensions.cs` provides `AddHexalithEventStoreSecurity()`, `WithSecurityDependency(...)`, `WithJwtBearerSecurity(...)`, `WithEventStoreClientCredentials(...)`, and `WithOpenIdConnectSecurity(...)`.
- `Hexalith.EventStore/src/Hexalith.EventStore.AppHost/Program.cs` and `Hexalith.Tenants/src/Hexalith.Tenants.AppHost/Program.cs` both initialize `HexalithEventStoreSecurityResources? security = builder.AddHexalithEventStoreSecurity();`.
- Tenants already has a regression test that expects `AddHexalithEventStoreSecurity(` and rejects direct `AddKeycloak("keycloak"` hand-wiring in its AppHost.
- Projects Story 1.9 completed the AppHost topology before this shared security helper became the sibling-module pattern.

Conclusion: Projects should adopt the shared EventStore security service helper in its AppHost. This is a topology consistency and security maintainability correction, not a product-scope change.

## 2. Impact Analysis

### Epic Impact

Affected epic: Epic 1, Story 1.9, "Aspire/Dapr/Workers topology & operational skeleton".

The epic remains valid and complete in intent. The story needs a direct adjustment so its completed topology uses the current shared security service helper instead of hand-rolled Keycloak/JWT wiring.

No new epic is required. No epic should be removed or resequenced.

### Story Impact

Story 1.9 should be amended to require:

- The AppHost initializes local security through `builder.AddHexalithEventStoreSecurity()`.
- API resources that consume `Authentication:JwtBearer` use `WithJwtBearerSecurity(security)`.
- Resources that only need startup ordering use `WithSecurityDependency(security)`.
- `projects-ui` should not receive unused JWT bearer settings unless its startup code consumes them. If interactive UI authentication is added, it should be implemented with FrontComposer server authentication and then wired with `WithOpenIdConnectSecurity(...)`.
- Direct `builder.AddKeycloak("keycloak", 8180)` wiring is removed from Projects AppHost.

### Artifact Conflicts

PRD: no functional requirement change. The PRD security/privacy NFR already supports this correction because it strengthens centralized tenant/security posture.

Epics: only Epic 1 Story 1.9 needs wording and implementation notes updated.

Architecture: the Infrastructure & Deployment section should name the shared EventStore security service helper as the local AppHost security composition pattern.

UX: no UX specification change. This does not add or change user-visible workflows.

Runbook/tests: the local topology runbook and AppHost structural tests should reflect the `security` resource/helper and should reject direct Keycloak hand-wiring.

### Technical Impact

Recommended implementation change:

- Add a non-resource project reference from `src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj` to `$(HexalithEventStoreRoot)\src\Hexalith.EventStore.Aspire\Hexalith.EventStore.Aspire.csproj`.
- Remove the direct AppHost package/reference need for `Aspire.Hosting.Keycloak` if no longer used directly.
- Replace the manual Keycloak block in `src/Hexalith.Projects.AppHost/Program.cs` with `builder.AddHexalithEventStoreSecurity()`.
- Replace `ConfigureJwt(...)` with shared helper calls.
- Add/adjust tests in `tests/Hexalith.Projects.Integration.Tests/AspireTopologyTests.cs` to assert the AppHost uses `AddHexalithEventStoreSecurity(` and does not call `AddKeycloak("keycloak"`.
- Update `docs/runbooks/projects-topology.md` expected resources from "Keycloak" to "security resource backed by Keycloak when `EnableKeycloak` is not `false`".

Expected risk is low. The main risk is accidentally changing UI authentication behavior. Avoid that by keeping `projects-ui` as a security dependency only unless a separate UI-auth change is implemented.

## 3. Recommended Approach

Use Direct Adjustment.

Rationale:

- The change is localized to AppHost topology composition, structural tests, and documentation.
- It aligns Projects with EventStore and Tenants without changing domain, API, OpenAPI, generated client, projection, CLI, MCP, or Web behavior.
- It removes duplicated security wiring and puts future helper improvements in one place.
- It preserves the existing `EnableKeycloak=false` local fallback behavior through the shared helper.

Effort estimate: Low.

Risk level: Low.

Timeline impact: no epic resequencing required.

## 4. Detailed Change Proposals

### Story Change

Story: Epic 1, Story 1.9: Aspire/Dapr/Workers topology & operational skeleton

Section: Acceptance Criteria 1

OLD:

```markdown
Then the AppHost declares a coherent topology for `eventstore`, `tenants`, `projects`, `projects-workers`, Keycloak, and Dapr sidecars/components
```

NEW:

```markdown
Then the AppHost declares a coherent topology for `eventstore`, `tenants`, `projects`, `projects-workers`, the shared EventStore security resource, and Dapr sidecars/components
And local security is initialized through `builder.AddHexalithEventStoreSecurity()` rather than direct `AddKeycloak(...)` wiring.
```

Rationale: keeps Story 1.9 aligned with the reusable EventStore/Tenants AppHost pattern.

Section: Task 1

OLD:

```markdown
- [x] Wire Keycloak with realm import if the local realm assets exist; support `EnableKeycloak=false` for dev-mode parity with EventStore/Folders patterns without weakening production guidance.
```

NEW:

```markdown
- [x] Initialize the shared EventStore security resource with `builder.AddHexalithEventStoreSecurity()`; use `WithJwtBearerSecurity(...)`, `WithSecurityDependency(...)`, and, where UI OIDC is implemented, `WithOpenIdConnectSecurity(...)`; preserve `EnableKeycloak=false` without weakening production guidance.
```

Rationale: captures the current shared helper as the expected implementation surface.

### Architecture Change

Artifact: `_bmad-output/planning-artifacts/architecture.md`

Section: Infrastructure & Deployment

OLD:

```markdown
Topology: Aspire AppHost (REST + SignalR + `MapMcp`); Dapr sidecars (state, pub/sub,
actors, service invocation, resiliency, access control); local Dapr components backed by the configured Redis endpoint, defaulting to the Dapr-initialized Redis instance.
```

NEW:

```markdown
Topology: Aspire AppHost (REST + SignalR + `MapMcp`); shared EventStore security resource via `AddHexalithEventStoreSecurity`; Dapr sidecars (state, pub/sub, actors, service invocation, resiliency, access control); local Dapr components backed by the configured Redis endpoint, defaulting to the Dapr-initialized Redis instance.
```

Rationale: records the security helper as part of local topology composition.

### Runbook Change

Artifact: `docs/runbooks/projects-topology.md`

Section: Expected Resources

OLD:

```markdown
- Keycloak when `EnableKeycloak` is not set to `false`
```

NEW:

```markdown
- Shared EventStore security resource, backed by Keycloak when `EnableKeycloak` is not set to `false`
```

Rationale: describes the AppHost contract by the Hexalith security abstraction rather than by its current implementation detail.

### Code Change

Artifact: `src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj`

Change:

```xml
<ProjectReference Include="$(HexalithEventStoreRoot)\src\Hexalith.EventStore.Aspire\Hexalith.EventStore.Aspire.csproj" IsAspireProjectResource="false" />
```

Remove direct `Aspire.Hosting.Keycloak` package usage from the AppHost project if the AppHost no longer references `KeycloakResource` directly.

Artifact: `src/Hexalith.Projects.AppHost/Program.cs`

OLD:

```csharp
IResourceBuilder<KeycloakResource>? keycloak = null;
ReferenceExpression? realmUrl = null;
if (!string.Equals(builder.Configuration["EnableKeycloak"], "false", StringComparison.OrdinalIgnoreCase))
{
    keycloak = builder.AddKeycloak("keycloak", 8180);
    if (Directory.Exists(Path.Combine(builder.AppHostDirectory, "KeycloakRealms")))
    {
        _ = keycloak.WithRealmImport("./KeycloakRealms");
    }

    EndpointReference keycloakEndpoint = keycloak.GetEndpoint("http");
    realmUrl = ReferenceExpression.Create($"{keycloakEndpoint}/realms/hexalith");
}
```

NEW:

```csharp
HexalithEventStoreSecurityResources? security = builder.AddHexalithEventStoreSecurity();
```

OLD:

```csharp
if (keycloak is not null && realmUrl is not null)
{
    ConfigureJwt(eventStore, keycloak, realmUrl);
    ConfigureJwt(tenants, keycloak, realmUrl);
    ConfigureJwt(projects, keycloak, realmUrl);
    ConfigureJwt(projectsUi, keycloak, realmUrl);
    ConfigureJwt(projectsWorkers, keycloak, realmUrl);
}
```

NEW:

```csharp
if (security is not null)
{
    _ = eventStore.WithJwtBearerSecurity(security);
    _ = tenants.WithJwtBearerSecurity(security);
    _ = projects.WithJwtBearerSecurity(security);
    _ = projectsWorkers.WithSecurityDependency(security);
    _ = projectsUi.WithSecurityDependency(security);
}
```

Notes:

- Use `WithOpenIdConnectSecurity(...)` for `projects-ui` only if the UI startup is updated to consume FrontComposer/OIDC settings in the same implementation.
- Delete the local `ConfigureJwt(...)` helper after the shared helper replaces it.

Artifact: `tests/Hexalith.Projects.Integration.Tests/AspireTopologyTests.cs`

Change:

- Add a structural assertion that `Program.cs` contains `AddHexalithEventStoreSecurity(`.
- Add a structural assertion that `Program.cs` does not contain `AddKeycloak("keycloak"`.
- Add a structural assertion that `Program.cs` contains `WithJwtBearerSecurity(security)`.
- Optionally assert `projectsUi.WithSecurityDependency(security)` until UI OIDC is explicitly implemented.

## 5. Implementation Handoff

Scope classification: Minor.

Route to: Developer agent.

Responsibilities:

- Update Projects AppHost to use `HexalithEventStoreSecurityExtensions`.
- Keep UI authentication behavior unchanged unless a separate UI-auth scope is intentionally added.
- Update structural topology tests.
- Update Story 1.9 notes and local topology runbook.
- Run focused verification:
  - `dotnet build Hexalith.Projects.slnx`
  - `dotnet test tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj --filter AspireTopologyTests`
  - `git diff --check`

Success criteria:

- Projects AppHost initializes security through `AddHexalithEventStoreSecurity()`.
- No direct `builder.AddKeycloak("keycloak", 8180)` wiring remains in Projects AppHost.
- Projects API/EventStore/Tenants resources receive JWT bearer settings through `WithJwtBearerSecurity(...)`.
- `projects-ui` and `projects-workers` do not receive misleading unused JWT settings unless their startup code consumes them.
- Runbook and tests describe the shared security resource abstraction.

## Checklist Status

- [x] 1.1 Triggering story identified: Epic 1, Story 1.9.
- [x] 1.2 Core problem defined: AppHost security helper drift from EventStore/Tenants.
- [x] 1.3 Evidence gathered from Projects AppHost, EventStore Aspire helper, EventStore AppHost, Tenants AppHost, Tenants tests, PRD, architecture, and runbook.
- [x] 2.1 Current epic remains completable as planned.
- [x] 2.2 Epic-level change is a Story 1.9 direct adjustment.
- [x] 2.3 Remaining planned epics unaffected.
- [x] 2.4 No new epic required.
- [x] 2.5 No resequencing required.
- [x] 3.1 PRD has no conflict; security/privacy NFR supports the change.
- [x] 3.2 Architecture needs a topology wording update.
- [N/A] 3.3 UI/UX specification unaffected.
- [x] 3.4 Runbook, AppHost project file, AppHost program, and topology tests need updates.
- [x] 4.1 Direct Adjustment viable; low effort, low risk.
- [x] 4.2 Rollback not useful.
- [N/A] 4.3 MVP Review not needed.
- [x] 4.4 Recommended path selected: Direct Adjustment.
- [x] 5.1 Issue summary created.
- [x] 5.2 Epic impact and artifact adjustments documented.
- [x] 5.3 Recommended path and rationale documented.
- [x] 5.4 PRD MVP impact and action plan defined.
- [x] 5.5 Handoff plan established.
- [x] 6.1 Checklist completion reviewed.
- [x] 6.2 Proposal accuracy reviewed.
- [x] 6.3 Explicit user approval obtained from Jerome on 2026-06-26.
- [N/A] 6.4 Sprint status update not required unless implementation changes story statuses.
- [x] 6.5 Next steps and handoff plan defined pending approval.
