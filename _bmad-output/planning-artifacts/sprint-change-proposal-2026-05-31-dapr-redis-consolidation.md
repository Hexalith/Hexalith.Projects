# Sprint Change Proposal: Dapr Redis Consolidation

Date: 2026-05-31
Project: Projects
Requested by: Jerome
Approval: approved by Jerome on 2026-05-31

## 1. Issue Summary

The current local topology text and AppHost implementation can be read as requiring a separate Redis server even though Hexalith.Projects persistence is accessed through Dapr components. The user identified the redundant server concern: data persistence is already provided through the Dapr-initialized Redis container, commonly visible locally as `dapr_redis`.

The core issue is not whether Redis is needed at all. Redis is still the local backing implementation for Dapr `state.redis` and `pubsub.redis`. The issue is whether Hexalith.Projects should create an additional Aspire-managed Redis resource when the Dapr runtime already provides the Redis endpoint used by the Dapr components.

Evidence:

- PRD non-goals state that Projects will not bypass Dapr, Hexalith.EventStore, or tenant isolation patterns.
- Architecture AR-20 states Dapr is the only infrastructure abstraction for state/pub-sub/actors/service-invocation.
- Architecture infrastructure section states local topology uses Redis-backed Dapr components.
- `src/Hexalith.Projects.AppHost/Program.cs` currently creates `builder.AddRedis(ProjectsAspireModule.RedisResourceName)` and passes that endpoint into Dapr component metadata.
- `src/Hexalith.Projects.Aspire/ProjectsAspireModule.cs` defines Dapr components `statestore` as `state.redis` and `pubsub` as `pubsub.redis`.
- `docs/projection-catalog.md` defines runtime projection storage through Dapr `statestore`, not through a direct Redis client.

Conclusion: a Redis backing store is required for the local Dapr components, but a second Redis server is redundant if `dapr_redis` is already the standard local backing store.

## 2. Impact Analysis

### Epic Impact

Affected epic: Epic 1, Story 1.9, "Aspire/Dapr/Workers topology & operational skeleton".

The story remains valid. The wording should be clarified so the AppHost boots services and Dapr sidecars/components, while Redis is treated as the backing implementation for the Dapr components, not as a separate Projects-owned persistence server.

No new epic is required. No existing epic becomes obsolete.

### Story Impact

Story 1.9 should be updated to remove the implication that an extra Redis instance is always started by Projects. Acceptance criteria should verify:

- Projects uses Dapr `statestore` and `pubsub` for persistence and pub/sub.
- Local Dapr components can target the Dapr-initialized Redis endpoint.
- No direct Redis client or direct Redis persistence path is introduced into Projects domain, contracts, client, server, workers, CLI, MCP, or UI code.
- If an Aspire-managed Redis fallback is retained, it is explicitly optional and disabled when using Dapr's initialized Redis.

### Artifact Conflicts

PRD: no functional scope change. The non-goal "Projects will not bypass Dapr" already supports this correction.

Architecture: requires a wording clarification in AR-22 and the infrastructure/development workflow sections. The current phrase "Dapr components + Redis" should be revised to make Redis a Dapr backing dependency, not a separate Projects infrastructure dependency.

Runbook: `docs/runbooks/projects-topology.md` currently lists `redis` as an expected AppHost resource. This should change to either "Dapr-initialized Redis backing store" or "optional Aspire-managed Redis fallback" depending on the chosen local execution mode.

Tests: `tests/Hexalith.Projects.Integration.Tests/AspireTopologyTests.cs` currently asserts `ProjectsAspireModule.RedisResourceName.ShouldBe("redis")`. That assertion should be removed or changed to verify Dapr component names and Redis backing metadata only.

### Technical Impact

Recommended implementation change:

- Stop always creating an Aspire Redis resource in `src/Hexalith.Projects.AppHost/Program.cs`.
- Configure Dapr components to use a configurable Redis host, defaulting to the Dapr-initialized local endpoint (`localhost:6379` for host access to `dapr_redis`).
- Keep Dapr component names stable: `statestore` and `pubsub`.
- Keep Dapr component types stable: `state.redis` and `pubsub.redis`.
- Keep all Projects persistence code behind Dapr `statestore`.

Expected risk is low. The main operational risk is local developer environments that have not run `dapr init`; this should be handled by a clear runbook prerequisite and readiness failure.

## 3. Recommended Approach

Use Direct Adjustment.

Rationale:

- This is an infrastructure topology correction, not a product-scope change.
- Existing architecture already requires Dapr-only infrastructure access.
- Removing the unconditional extra Redis resource reduces local resource duplication and avoids confusion over which Redis contains state.
- The Dapr component names and persistence abstraction stay unchanged, so domain and API behavior are unaffected.

Effort estimate: Low.

Risk level: Low to Medium. Low in code behavior, Medium for developer setup if `dapr_redis` is not present or not exposed on `localhost:6379`.

Timeline impact: no epic resequencing required.

## 4. Detailed Change Proposals

### Story Change

Story: Epic 1, Story 1.9: Aspire/Dapr/Workers topology & operational skeleton

Section: Acceptance Criteria

OLD:

```markdown
Then it boots eventstore, tenants, projects, workers, Keycloak, and Dapr components (Redis-backed) as a coherent local topology
```

NEW:

```markdown
Then it boots eventstore, tenants, projects, workers, Keycloak, and Dapr sidecars/components as a coherent local topology
And the Dapr `statestore` and `pubsub` components use the configured local Redis backing endpoint, defaulting to the Dapr-initialized Redis instance rather than creating a second Redis server.
```

Rationale: keeps Redis as Dapr's local backing store while eliminating a redundant Redis server requirement.

### Architecture Change

Artifact: `_bmad-output/planning-artifacts/architecture.md`

Section: Infrastructure & Deployment

OLD:

```markdown
Topology: Aspire AppHost (REST + SignalR + `MapMcp`); Dapr sidecars (state, pub/sub,
actors, service invocation, resiliency, access control); Redis-backed Dapr components locally.
```

NEW:

```markdown
Topology: Aspire AppHost (REST + SignalR + `MapMcp`); Dapr sidecars (state, pub/sub,
actors, service invocation, resiliency, access control); local Dapr components backed by the configured Redis endpoint, defaulting to the Dapr-initialized Redis instance.
```

Rationale: clarifies that Redis is implementation backing for Dapr components, not a Projects-owned dependency.

### Runbook Change

Artifact: `docs/runbooks/projects-topology.md`

Section: Expected Resources

OLD:

```markdown
- `redis`
- Redis-backed Dapr component `statestore`
- Redis-backed Dapr component `pubsub`
```

NEW:

```markdown
- Redis-backed Dapr component `statestore`
- Redis-backed Dapr component `pubsub`
- A reachable local Redis backing endpoint, normally the Dapr-initialized `dapr_redis` instance
```

Rationale: removes the expectation that Projects AppHost owns a standalone Redis resource.

### Code Change

Artifact: `src/Hexalith.Projects.AppHost/Program.cs`

OLD:

```csharp
IResourceBuilder<RedisResource> redis = builder.AddRedis(ProjectsAspireModule.RedisResourceName);
...
redis.GetEndpoint("tcp"),
```

NEW:

```csharp
string redisHost = builder.Configuration["Dapr:RedisHost"] ?? "localhost:6379";
...
redisHost,
```

Rationale: reuse the Dapr-initialized Redis by default and keep an explicit configuration knob for nonstandard local environments.

Artifact: `src/Hexalith.Projects.Aspire/ProjectsAspireModule.cs`

Change:

- Add an `AddHexalithProjects(...)` overload accepting `string redisHost`.
- Route both endpoint-reference and string-host paths through the existing `AddProjectsSharedDaprComponentsCore(...)`.
- Keep `StateStoreComponentName = "statestore"` and `PubSubComponentName = "pubsub"` unchanged.
- Remove or deprecate `RedisResourceName` if no optional Aspire-managed fallback remains.

Artifact: `tests/Hexalith.Projects.Integration.Tests/AspireTopologyTests.cs`

Change:

- Remove the assertion that `RedisResourceName` is a required topology contract.
- Add/keep assertions that `statestore` is `state.redis`, `pubsub` is `pubsub.redis`, and both carry Redis host metadata.

## 5. Implementation Handoff

Scope classification: Minor.

Route to: Developer agent.

Responsibilities:

- Update AppHost and Aspire helper code to use configured Dapr Redis backing instead of unconditional Aspire Redis creation.
- Update integration tests for the revised topology contract.
- Update runbook and architecture wording.
- Run focused tests:
  - `dotnet test tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj --filter AspireTopologyTests`
  - `dotnet test tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj --filter DaprConfigurationTests`

Success criteria:

- Local topology uses Dapr components `statestore` and `pubsub` as the only Projects persistence/pub-sub abstraction.
- No second Redis server is required when `dapr_redis` is available.
- No direct Redis dependency is introduced into Projects runtime code.
- Documentation no longer presents Redis as a separate Projects-owned persistence server.

## Checklist Status

- [x] 1.1 Triggering story identified: Epic 1, Story 1.9.
- [x] 1.2 Core problem defined: redundant local Redis server vs Dapr-backed persistence.
- [x] 1.3 Evidence gathered from PRD, architecture, runbook, AppHost, Aspire helper, projection catalog.
- [x] 2.1 Current epic remains completable.
- [x] 2.2 Epic-level change is wording and topology-contract clarification.
- [x] 2.3 Remaining epics unaffected.
- [x] 2.4 No new epic required.
- [x] 2.5 No resequencing required.
- [x] 3.1 PRD has no conflict; it supports Dapr-only access.
- [x] 3.2 Architecture needs clarification.
- [N/A] 3.3 UI/UX unaffected.
- [x] 3.4 Runbook and integration tests need updates.
- [x] 4.1 Direct Adjustment viable; low effort, low-to-medium risk.
- [x] 4.2 Rollback not viable or useful.
- [N/A] 4.3 MVP Review not needed.
- [x] 4.4 Recommended path selected.
- [x] 5.1-5.5 Proposal and handoff defined.
- [x] 6.3 User approval obtained before implementation.
- [N/A] 6.4 Sprint status update not required unless implementation changes story statuses.
