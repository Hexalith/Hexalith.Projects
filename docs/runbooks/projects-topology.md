# Projects Local Topology Runbook

This runbook covers the local Aspire/Dapr topology for Hexalith.Projects. It is a development
operations guide, not production configuration.

## Start and Stop

Prerequisites:

- .NET SDK `10.0.300`.
- Docker-compatible container runtime for Aspire-managed infrastructure.
- Dapr CLI/runtime installed and initialized for local sidecars, with the Dapr-initialized Redis
  backing endpoint reachable from the host. By default the AppHost uses `localhost:6379`; override
  with `Dapr:RedisHost` when your local Dapr Redis is exposed elsewhere.
- Root-level sibling repositories already available through the workspace layout.

Start the topology:

```powershell
dotnet run --project src\Hexalith.Projects.AppHost
```

Start without Keycloak when working on non-auth local plumbing:

```powershell
dotnet run --project src\Hexalith.Projects.AppHost -- --EnableKeycloak=false
```

Stop the topology from the Aspire dashboard or stop the `dotnet run` process with `Ctrl+C`.

Do not run recursive submodule initialization for this topology. If a root-level sibling project is
missing, fetch only the required root-level submodule or repository according to the workspace setup.

## Expected Resources

The AppHost resource graph should include:

- `eventstore`
- `tenants`
- `projects`
- `projects-workers`
- Redis-backed Dapr component `statestore`
- Redis-backed Dapr component `pubsub`
- A reachable local Redis backing endpoint, normally the Dapr-initialized `dapr_redis` instance
- Keycloak when `EnableKeycloak` is not set to `false`

The stable Dapr app IDs are `eventstore`, `tenants`, `projects`, and `projects-workers`. The Dapr
component names are `statestore` and `pubsub`. Redis is a Dapr component backend in this topology;
Projects code must not access Redis directly.

## Health and Readiness

Use these endpoints through the service endpoint shown by Aspire:

```powershell
Invoke-WebRequest http://localhost:<projects-port>/alive
Invoke-WebRequest http://localhost:<projects-port>/ready
Invoke-WebRequest http://localhost:<projects-workers-port>/alive
Invoke-WebRequest http://localhost:<projects-workers-port>/ready
```

Interpretation:

- `/alive` proves the process can respond.
- `/ready` includes metadata-only readiness checks for Dapr sidecar/configuration, state store,
  pub/sub, EventStore gateway dependency, and projection store wiring.
- `/health` returns the aggregate health status for local diagnostics.

Health responses must stay metadata-only. They must not include tenant data, project names, command
bodies, setup text, paths, tokens, secrets, raw event payloads, or sibling denial detail.

## Dead-Letter Topics

Configured dead-letter topics:

- Tenants events: `deadletter.system.tenants.events`
- Project events: `deadletter.projects.events`

Use Dapr or the EventStore/Admin-safe tooling path first when inspecting dead-letter state. Treat
dead-lettered messages as sensitive operational evidence: inspect metadata such as topic, app ID,
message ID, tenant ID, correlation ID, sequence, and failure reason. Do not paste or persist raw event
payloads in tickets or logs.

Recommended local triage:

```powershell
dapr list
Invoke-WebRequest http://localhost:<projects-workers-port>/ready
```

If the worker is not ready, fix the sidecar/component dependency before replay. If it is ready, replay
from the authoritative EventStore stream or an approved Dapr pub/sub drain tool that re-delivers to
`projects-workers`; do not mutate the Redis component directly as the primary recovery path.

## Projection Replay and Rebuild

Runtime projection state is stored through the Dapr `statestore` component:

- Tenant access key shape: `projects:tenant-access:{tenantId}`
- Project projection journal key shape: `projects:projection-journal:{tenantId}`

Project list/detail/reference-index rebuild uses the same deterministic pure folds proven by the
projection tests: `ProjectListProjection.Rebuild(...)`, `ProjectDetailProjection.Rebuild(...)`, and
`ProjectReferenceIndexProjection.Empty.Apply(...)` (the reference index has no separate `Rebuild`
method — its rebuild path is `Empty.Apply(envelopes)`). A rebuild should feed EventStore
`EventEnvelope` data back through the worker projection path or an approved admin drain that uses the
same envelope shape. Do not create an alternate fold or write projected rows by hand.

Safe rebuild order:

1. Confirm `projects-workers` `/ready` is healthy.
2. Identify the tenant and stream watermark from metadata only.
3. Replay the authoritative EventStore project events for the tenant through the worker projection
   endpoint or approved drain tool.
4. Recheck `/ready` and then query the Projects API with normal authorization.

## Safe Failure Handling

Fail closed in these cases:

- Dapr sidecar, `statestore`, or `pubsub` is unavailable.
- The configured Redis backing endpoint for Dapr components is unavailable.
- Tenant access projection is missing, stale, malformed, or in replay conflict.
- Project projection journal is missing, malformed, or in replay conflict.
- Envelope tenant and event tenant disagree.
- Message ID appears with a different fingerprint.

Operators should collect metadata-only evidence: Dapr app ID, component name, endpoint, topic,
dead-letter topic, message ID, tenant ID, correlation ID, task ID, sequence/watermark, and health check
name/status. Escalate with that metadata and the command/test output; do not include raw payloads,
credentials, tokens, setup text, project context content, transcripts, or filesystem paths containing
private data.
