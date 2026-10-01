# Projects Local Topology Runbook

This runbook covers the local Aspire/Dapr topology for Hexalith.Projects. It is a development
operations guide, not production configuration.

## Start and Stop

Prerequisites:

- .NET SDK `10.0.401`.
- Aspire CLI `13.6.0` (matching the AppHost SDK/hosting/orchestration stack).
- Dapr CLI `1.18.0` with runtime `1.18.2` initialized for local sidecars.
- `jq` for JSON endpoint extraction and `curl` for health probes.
- Docker-compatible container runtime for Aspire-managed infrastructure.
- The Dapr-initialized Redis backing endpoint must be reachable from the host. By default the AppHost uses
  `localhost:6379`; override
  with `Dapr:RedisHost` when your local Dapr Redis is exposed elsewhere.
- Root-level sibling repositories already available through the workspace layout.

From the repository root, start the topology through the Aspire CLI. The explicit Commons root keeps
nested sibling project resolution on the root-declared checkout:

```bash
APPHOST="$PWD/src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj"
export HexalithCommonsRoot="$PWD/references/Hexalith.Commons"
aspire start --apphost "$APPHOST" --format Json --non-interactive
cleanup_apphost() { aspire stop --apphost "$APPHOST" --non-interactive >/dev/null 2>&1 || true; }
trap cleanup_apphost EXIT
aspire wait projects-ui --apphost "$APPHOST" --timeout 180 --non-interactive
```

Start without the shared security resource when working on non-auth local plumbing:

```bash
EnableKeycloak=false aspire start --apphost "$APPHOST" --format Json --non-interactive
```

Stop only this topology by selecting its AppHost explicitly:

```bash
aspire stop --apphost "$APPHOST" --non-interactive
trap - EXIT
```

Do not use `aspire stop --all` when other workspaces may have active AppHosts.

Do not run recursive submodule initialization for this topology. If a root-level sibling project is
missing, fetch only the required root-level submodule or repository according to the workspace setup.

## Current G-6 qualification

The approved 2026-10-01 tuple uses SDK `10.0.401`, Aspire SDK/CLI/hosting `13.6.0`, Toolkit `13.6.0-beta.910`, Dapr CLI/runtime/.NET `1.18.0`/`1.18.2`/`1.18.10`, Fluent UI `5.0.0`, NSubstitute `6.2.0`, and Fluxor `6.11.0`. The run approval includes the Toolkit prerelease and unlisted Dapr support-table exceptions; Jérôme Piquot separately accepted attempt 16 at `2026-10-01T14:58:12Z` on reviewed packet SHA-256 `b7f940e512e17bcad98b021e4a7d4b9acce4f0c9fba4d3dc23f9d160245be139`.

Run the approved qualifier from the root with `python3 tools/qualification/run_g6_qualification.py --baseline references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-10-01.json --output _bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain-20261001/<new-attempt>`. It installs isolated tools/cache and starts disposable loopback PostgreSQL, Redis, placement, and scheduler. Every owned sidecar, including restarts, uses a private SQLite discovery registry in the disposable fixture directory, preserving the `sample` and `eventstore` app IDs. `NAMESPACE` supplies separate actor/scheduler isolation; Dapr 1.18.2's mDNS resolver does not use it for service lookups. The SQLite resolver is Alpha and is selected only for this fixture; this proof does not qualify it for production. Domain state remains in PostgreSQL through Dapr. Exact process groups/container IDs, discovery configuration and registry-identity hashes, and shared-resource comparisons are retained in the pending packet. An existing packet is never overwritten.

The checkout-source proof is distinct from published EventStore `3.110.0` archives. Platform’s Works/mTLS `1.18.3` development preview remains unselected and unqualified; Dapr.Workflow remains unselected. Attempt 16 has accepted committed-source closure and exact gitlinks in the retained clean checkout. G-6 prerequisite usability is true for that exact scope; dirty/drifting checkouts cannot reuse the acceptance. Current P1R usability and downstream readiness remain unchanged.

## Expected Resources

The AppHost resource graph should include:

- `eventstore`
- `tenants`
- `projects`
- `projects-workers`
- `projects-ui`
- Redis-backed Dapr component `statestore`
- Redis-backed Dapr component `pubsub`
- A reachable local Redis backing endpoint, normally the Dapr-initialized `dapr_redis` instance
- Shared EventStore security resource, backed by Keycloak when `EnableKeycloak` is not set to `false`

The stable Dapr app IDs are `eventstore`, `tenants`, `projects`, and `projects-workers`. The Dapr
component names are `statestore` and `pubsub`. Redis is a Dapr component backend in this topology;
Projects code must not access Redis directly.

## Health and Readiness

Discover endpoints from the live resource graph; do not copy a prior run's assigned ports:

```bash
TOPOLOGY="$(aspire describe --apphost "$APPHOST" --format Json --non-interactive)"
PROJECTS_URL="$(jq -r '.resources[] | select(.displayName == "projects") | .urls[] | select(.name == "http") | .url' <<<"$TOPOLOGY")"
PROJECTS_WORKERS_URL="$(jq -r '.resources[] | select(.displayName == "projects-workers") | .urls[] | select(.name == "http") | .url' <<<"$TOPOLOGY")"
PROJECTS_UI_URL="$(jq -r '.resources[] | select(.displayName == "projects-ui") | .urls[] | select(.name == "http") | .url' <<<"$TOPOLOGY")"
SECURITY_URL="$(jq -r '.resources[] | select(.displayName == "security") | .urls[] | select(.name == "http") | .url' <<<"$TOPOLOGY")"
```

Use the derived service endpoints:

```bash
curl --fail "$PROJECTS_URL/alive"
curl --fail "$PROJECTS_URL/ready"
curl --fail "$PROJECTS_WORKERS_URL/alive"
curl --fail "$PROJECTS_WORKERS_URL/ready"
```

For Epic 5 browser verification, map `PROJECTS_UI_URL` to `BASE_URL`, `PROJECTS_URL` to `API_URL`,
and `SECURITY_URL` to `KEYCLOAK_URL`, then set `E2E_LIVE_APPHOST=1`. The complete command sequence and
credential variables are documented in `tests/e2e/README.md`.

For the repeatable live verification path, use `npm --prefix tests/e2e run test:live:managed`.
The runner enables `Projects__E2E__LiveFixtures=1`, waits for the core and fixture resources,
captures `aspire describe` exactly once, exports all five dynamically assigned endpoints, runs the
two-worker Chromium lane with zero skips, and stops only the exact Projects AppHost on every exit.
The `live-fixtures` control resource and sibling fixture routes do not exist when the profile is
disabled. Never publish or enable this profile outside the managed local/scheduled test lane.

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

```bash
dapr list
curl --fail "$PROJECTS_WORKERS_URL/ready"
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
