# Projects Local Topology Runbook

This runbook covers the local Aspire/Dapr topology for Hexalith.Projects. It is a development
operations guide, not production configuration.

## Start and Stop

Prerequisites:

- .NET SDK `10.0.402`.
- Aspire CLI `13.6.0` (matching the AppHost SDK/hosting/orchestration stack).
- Dapr CLI `1.18.0` with runtime `1.18.2` initialized for local sidecars.
- `jq` for JSON endpoint extraction and `curl` for health probes.
- Docker-compatible container runtime for Aspire-managed infrastructure.
- The Dapr-initialized Redis backing endpoint must be reachable from the host. By default the AppHost uses
  `localhost:6379`; override
  with `Dapr:RedisHost` when your local Dapr Redis is exposed elsewhere.
- Root-level sibling repositories already available through the workspace layout.

Before upgrading an existing `KeycloakPersistent=true` instance, export/back up its realm and verify
that the backup can be restored. This workload-credential change adds a container environment variable;
[Aspire can recreate persistent containers when configuration changes](https://aspire.dev/app-host/resource-lifetimes/).
The current Keycloak helper has no data volume, so the old container's identity data would be lost on
recreation. Do not restart that instance with the upgraded AppHost until its data has been preserved.
For validation, use a separate isolated session with persistence explicitly disabled:
`KeycloakPersistent=false aspire start --apphost "$PWD/src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj" --isolated --non-interactive`.
This is validation of a fresh realm, not migration of the existing identity data.

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

### WSL: security stays Starting and dependent services stay Waiting

Check the container runtime from the same WSL terminal that launches Aspire:

```bash
docker version
aspire doctor --format Json --non-interactive
aspire describe --apphost "$APPHOST" --format Json --non-interactive
aspire logs security --apphost "$APPHOST" --non-interactive
```

`docker version` must report both client and server information. A successful `docker.exe version`
only proves Windows can reach Docker Desktop. If `docker` reports missing WSL integration while
`docker.exe` works, Aspire cannot create Keycloak: security can remain Starting with no container ID,
endpoints, or logs. Check the AppHost log named by `aspire start` for an unhealthy container runtime
and failed Docker `Version` calls. Increasing the readiness timeout does not repair this condition.

In Docker Desktop, verify **Settings > Resources > WSL Integration** enables the current distribution
and apply the settings, as described in the [Docker WSL integration guide](https://docs.docker.com/desktop/features/wsl/).
If integration is already enabled but its CLI mount/socket is stale, stop the exact Projects AppHost
before restarting Docker Desktop. A Desktop restart briefly interrupts **all** its containers;
coordinate shared workloads, record their identities and restart policies, and verify their recovery.
Do not remove containers or volumes, reset Docker, or shut down WSL as part of this repair.

```bash
aspire stop --apphost "$APPHOST" --non-interactive
docker.exe desktop restart --timeout 180
docker version
```

Proceed only after Linux `docker version` succeeds and the shared containers have recovered. Restart
the topology with the normal command above, then use `aspire wait security` and `aspire wait projects-ui`
with `--apphost "$APPHOST" --timeout 180 --non-interactive`. Preserve Keycloak authentication and the
existing dependency waits; disabling security does not verify recovery of the authenticated topology.

### EventStore exits with missing workload credentials

Once Keycloak is healthy, EventStore requires `Authentication:WorkloadIssuer:ClientId` and
`ClientSecret` to obtain assertions for its internal domain-service calls. The AppHost supplies a
generated secret parameter to both Keycloak's `eventstore` service-account client and EventStore through
the platform's `WithEventStoreWorkloadClientCredentials` helper. The secret follows `KeycloakPersistent`:
ephemeral by default, persisted with an explicitly persistent local Keycloak container.

The local realm grants no audience or operation by default. Its optional scopes cover the `projects`
and `tenants` audiences and the five domain-service operations; each acquisition requests one audience
and one operation. These ordinary local assertions do not establish Story 6.1 resource-binding or
production enrollment acceptance. A reused container does not re-import a changed realm, but the new
environment variable can instead force container recreation. Follow the backup/restore requirement
before the startup commands above; a fresh isolated validation instance does not migrate old data.

After readiness waits, inspect `aspire describe` every five seconds for 30 seconds and require the
long-running services to remain Running and Healthy with unchanged names and start timestamps.
Fail the check on an exit or restart. This is a bounded startup check, not a guarantee of future health.
A successful wait can precede a subsequent process exit; `eventstore` Finished
with exit code 134 is a failure even when its dependents already started. Inspect `aspire logs eventstore`
with the exact AppHost selector for the options-validation error; do not bypass validation or inject
dummy credentials merely to make the process start.

## Current G-6 qualification

G-6 now audits the effective versions and resolved consumer packages against
`references/Hexalith.Builds/Tools/g6-current-policy-2026-10-20.json`. Both the
local runner and CI wrapper default to this October 20 policy: SDK `10.0.402`,
Aspire `13.6.4` and Toolkit `13.6.1-beta.935`. Its approval covers tuple selection
and execution preparation; qualification-result acceptance is a separate decision.
Historical attempt 16 accepted Toolkit
`13.6.0-beta.910` at `2026-10-01T14:58:12Z` on reviewed packet SHA-256
`b7f940e512e17bcad98b021e4a7d4b9acce4f0c9fba4d3dc23f9d160245be139`;
that acceptance remains valid only for its recorded scope.

On a clean committed checkout with the root-declared submodules initialized,
run `python3 tools/qualification/run_g6_current.py --output .g6-current-evidence/<new-run>`.
The runner writes a compact `result.json`, the current tuple/material audit,
sanitized OQ8 capture files, command logs, and isolated cleanup receipt. It
never overwrites an existing result. Check the result with
`python3 references/Hexalith.Builds/Tools/g6_current.py validate --workspace . --policy references/Hexalith.Builds/Tools/g6-current-policy-2026-10-20.json --evidence .g6-current-evidence/<new-run>/result.json`.
Pass `--release` to require the exact root SHA and gitlinks. Release itself
runs this focused proof anew on its checked-out source. CI audits the tuple and
resolved graph on every event. It runs the live proof for changed material
paths, runtime-owning submodule gitlinks, schedules, or an uncertain change
range. An unrelated change is reported as `not required by this change` with
the audit issues and approval status retained; that report makes no passing
qualification claim. Reuse of an older proof remains disabled while transitive
material-input coverage is being closed.

The qualifier installs isolated tools/cache and starts disposable loopback
PostgreSQL, Redis, placement, and scheduler. Every owned sidecar, including
restarts, uses a private SQLite discovery registry in the disposable fixture
directory, preserving the `sample` and `eventstore` app IDs. `NAMESPACE`
separates actors and the scheduler; Dapr 1.18.2's mDNS resolver does not use it
for service lookups. The SQLite resolver is Alpha and selected only for this
fixture, not production. Domain state remains in PostgreSQL through Dapr.
Exact process groups/container IDs and shared-resource comparisons are retained
with the current result.

The checkout-source proof is distinct from published EventStore `3.110.0`
archives. Platform’s Works/mTLS `1.18.3` development preview remains
unselected and unqualified; Dapr.Workflow remains unselected. Attempt 16 has
accepted committed-source closure and exact gitlinks in its retained checkout.
Current P1R usability and downstream readiness remain unchanged.

### Deferred historical contract closures

The v2 packet contract supersedes five restrictions in the retained v1 validator:

- Every limitation, source-state, consumer-audit, AppHost-outcome, resolved-package
  and cleanup artifact is hash-bound. The bound baseline contains the pin audit.
- The real PostgreSQL two-sidecar restart qualifier must pass. The old credential
  preflight is no longer a required failing command.
- The package audit covers Dapr actor/AspNetCore packages, Aspire.Hosting packages
  and the EventStore package coordinate as well as the original package families.
- An additional clean inventory command may succeed. Every recorded command must
  succeed; an expected failure cannot qualify a packet.
- Accepted v2 packets require the allowed named approver's `accept` decision,
  timestamp on or after capture, and SHA-256 of the reviewed pending representation
  described below. The baseline approval binds the Builds, Platform and FrontComposer/Web owner
  roles. A status flip alone cannot grant acceptance or usability.

Run `python3 -m unittest tests/tools/test_g6_deferred_contracts.py -v` after
initializing the root-declared Builds dependency. It reuses hash-pinned synthetic
packet fixtures and the actual validators, with independent tampering and drift
controls. CI runs it in the blocking current G-6 preflight. These fixture results
verify validation behavior; they grant no live qualification or new acceptance.
The original v1 rules and historical evidence remain unchanged.

The v2 reviewed hash uses UTF-8 bytes from
`json.dumps(packet, ensure_ascii=False, indent=2) + "\n"` after setting
`status="pending"`, `acceptance=None` and `usableAsPrerequisite=False`. Preserve
field order and the final LF; hashing a minified or CRLF file is different. For
example, calculate the validator's reviewed hash from a candidate or accepted
packet without changing it:

```bash
python3 - /path/to/packet.json <<'PYHASH'
import hashlib, json, sys
from pathlib import Path
packet = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
packet.update(status="pending", acceptance=None, usableAsPrerequisite=False)
reviewed = (json.dumps(packet, ensure_ascii=False, indent=2) + "\n").encode("utf-8")
print(hashlib.sha256(reviewed).hexdigest())
PYHASH
```

For retained v2 acceptance, `qualification_gates.G-6.current_candidate_*` indexes
its reviewed pending copy; `current_candidate_usable` stays false. The existing
`current_accepted_packet`, `current_accepted_reviewed_sha256`,
`current_accepted_packet_sha256`, `current_accepted_at_utc`, `current_accepted_by`
and `current_accepted_acceptance_record` keys describe the separately accepted
historical packet and its exact scope. The `current_checkout_*` fields describe
separate execution evidence. An older `g6_acceptance_procedure` in historical
follow-up notes is superseded by this named, hash-bound contract. Future
qualification must retain its own exact source, tuple/material fingerprint,
result and owner decision; do not reuse attempt 16 or change the sprint index
based on these regression results. The current compact execution result and its
tuple-selection approval remain separate from v2 packet acceptance.

`qualification_gates.G-6.accepted_scope` limits the accepted source, tuple and
excluded capabilities; `owner_roles` and `approved_by` identify the baseline's
responsible roles and approver. `current_accepted_usable` applies only within the
recorded historical scope. `current_checkout_usable` describes only the source
named by `current_checkout_source_revision`, not the present checkout. These
summaries may change only with independently validated evidence and the relevant
named owner decision; a passing fixture never changes them.

`p1r_current_revalidation.owner_decisions`, `role_decisions`, `accepted_at_utc`
and `usable_as_prerequisite` govern P1R's separate four-role decision. Its
`superseded_candidate.g6_*` follow-up fields retain dated observations: packet
and SHA fields identify that candidate; `g6_current_qualification` describes its
measured result; `g6_ci_jobs` and `g6_acceptance_procedure` record the then-current
workflow and procedure. They are historical context, not new decision records.
Preserve these observations and record a successor under its own source/evidence
identity. A future sprint-index update must use the candidate-workspace
validation procedure in `_bmad-output/project-context.md` before publication,
then rerun `python3 tools/planning/validate_production_authority.py --validate-index`.
This task changes no index fields.

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
The runner enables `Projects__E2E__LiveFixtures=1`, marks the exact AppHost as owned before
`aspire start`, waits for the core, sidecar, and fixture resources with an explicit bound
(`E2E_RESOURCE_TIMEOUT_SECONDS`, default 600), captures `aspire describe` exactly once, exports all
five dynamically assigned endpoints, runs the startup/session smoke and then the full two-worker
Chromium lane with zero skips under distinct `<run>-smoke`/`<run>-full` identities, and stops only
the exact Projects AppHost on every exit, including failures and interruptions. The scheduled CI
job runs it with `CI=true` so the AppHost builds in package-reference mode, and repeats the
exact-AppHost stop in an `if: always()` step. Live artifacts are metadata-only: JUnit, the HTML
report, and role/status cleanup attachments; traces, video, and screenshots are disabled.

With the profile enabled, `conversations`, `folders`, and `memories` are internal role hosts that
Projects reaches through service discovery, and only the `live-fixtures` control resource exposes an
external endpoint to the runner. Each role answers the sibling client contract the Projects server
consumes (published Folders `api/v2/folders`, Conversations `api/v1/conversations`, Memories
`api/v1/tenants/{tenantId}/cases`) with metadata only; `LiveFixtureSiblingContractTests` pins every
role by calling it through the Projects adapters and the referenced sibling client packages. The
`live-fixtures` control resource and sibling fixture routes do
not exist when the profile is disabled; the AppHost then keeps Projects on its normal sibling
addresses and Keycloak-enforced authentication. Never publish or enable this profile outside the
managed local/scheduled test lane.

The Projects UI authenticates through the confidential `hexalith-projects-ui` authorization-code
client in the checked-in realm. The AppHost wires it with the EventStore Aspire
`WithOpenIdConnectSecurity` helper; the UI composes FrontComposer server security (HttpOnly cookie
session, server authentication state, and per-user token relay on the Projects client). Partial
OpenID Connect settings fail UI startup; only a topology started with `EnableKeycloak=false` runs the
UI without authentication.

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
