# Story 1.9: Aspire/Dapr/Workers Topology & Operational Skeleton

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **Projects platform engineer**,
I want **the local Aspire topology, Dapr components, Workers host, and service defaults wired with resiliency, dead-letter, and observability**,
so that **the module runs end-to-end locally and is operationally sound for later epics** _(AR-20, AR-21, AR-22; NFR-4 baseline)_.

This story closes Epic 1 by turning the current compile-time placeholders into an operational local skeleton. Stories 1.4 through 1.8 already delivered the command-async API, list/detail projections, tenant-access projection, layered fail-closed authorization, update setup, and archive behavior. Story 1.9 must make those pieces runnable together through Aspire and Dapr, while keeping domain logic pure and keeping durable infrastructure behind Dapr abstractions.

**Scope discipline:** this story owns `Hexalith.Projects.AppHost`, `Hexalith.Projects.Aspire`, `Hexalith.Projects.ServiceDefaults`, the executable `Hexalith.Projects.Workers` host, Dapr component/configuration files, local durable projection/dedup store binding, operational health/telemetry, topology smoke tests, and runbooks. Do not implement new domain commands/events, reference link/unlink, ProjectContext assembly, Project Resolution, audit timeline, CLI/MCP tools, FrontComposer-generated screens, or sibling module code. If `projects-ui` is included in the AppHost, keep it to a minimal hostable placeholder/resource dependency only; Epic 5 owns real generated UI.

## Acceptance Criteria

1. **Aspire AppHost boots the Projects local topology (AR-22).**
   **Given** `src/Hexalith.Projects.AppHost`
   **When** `dotnet run --project src/Hexalith.Projects.AppHost` is executed in a prepared local environment
   **Then** the AppHost declares a coherent topology for `eventstore`, `tenants`, `projects`, `projects-workers`, the shared EventStore security resource, and Dapr sidecars/components
   **And** local security is initialized through `builder.AddHexalithEventStoreSecurity()` rather than direct `AddKeycloak(...)` wiring
   **And** the Dapr `statestore` and `pubsub` components use the configured local Redis backing endpoint, defaulting to the Dapr-initialized Redis instance rather than creating a second Redis server
   **And** service dependencies use Aspire references and `WaitFor` ordering rather than hard-coded URLs
   **And** the AppHost fails fast with a clear message when required Dapr configuration files are missing
   **And** only root-level sibling project references are used; no recursive submodule setup is introduced.

2. **Dapr remains the only infrastructure abstraction and is configured for internal access control, resiliency, pub/sub, and state (AR-20, NFR-7).**
   **Given** Dapr sidecars for the Projects API and Workers hosts
   **When** internal endpoints such as `/process`, projection dispatch, and Tenants subscriptions are exposed
   **Then** Dapr app IDs and component names are stable contracts: `eventstore`, `tenants`, `projects`, `projects-workers`, `statestore`, and `pubsub`
   **And** Dapr access-control configuration scopes internal service-invocation operations instead of relying on unaudited open access
   **And** resiliency policies apply retry, timeout, and circuit-breaker behavior at Dapr service-invocation/component boundaries, not inside aggregate/projection domain code
   **And** dead-letter topics exist for undeliverable pub/sub messages and have a documented replay/drain path
   **And** any permissive development access-control policy is clearly marked local-only and is not presented as production configuration.

3. **Workers host projection processing and Tenants-event subscription with durable production binding (AR-13, AR-21).**
   **Given** `Hexalith.Projects.Workers`
   **When** the worker host starts
   **Then** it registers service defaults, Dapr CloudEvents/subscription endpoints, `MapSubscribeHandler`, the existing Tenants event handler chain, and project projection-processing endpoints/handlers
   **And** Tenants events still feed `TenantAccessProjection` through `ProjectsTenantEventHandler` with message-id dedupe, out-of-order tolerance, replay-conflict detection, and metadata-only evidence
   **And** Project events feed `ProjectListProjection` and `ProjectDetailProjection` through one deterministic fold shared with the existing Story 1.5 rebuild tests
   **And** Server and Workers read/write the same durable Dapr state-backed projection/dedup store in local/runtime hosts; the existing in-memory stores remain for tests and explicit pre-runtime fakes only
   **And** after restart, projections fail closed until durable state is loaded or rebuilt; no empty in-memory default may silently authorize or satisfy reads.

4. **ServiceDefaults provide telemetry, health, service discovery, and outbound HTTP resilience (NFR-4).**
   **Given** Projects Server, Workers, and any hostable UI/resource process
   **When** they run under the AppHost
   **Then** `Hexalith.Projects.ServiceDefaults` configures OpenTelemetry logs/metrics/traces, service discovery, standard HttpClient resilience, and health endpoints aligned with the Aspire ServiceDefaults pattern
   **And** `/health`, `/alive`, and `/ready` distinguish liveness from readiness, including Dapr sidecar, state-store, pub/sub, EventStore gateway, and projection freshness readiness where applicable
   **And** telemetry carries tenant/project/correlation/task/reason metadata only and never logs command bodies, setup text, sibling payloads, tokens, secrets, raw paths, or transcripts
   **And** health responses are metadata-only and do not reveal cross-tenant resource existence.

5. **Operational runbook and proof tests cover the skeleton (AR-21, AR-23).**
   **Given** the topology and Dapr configuration
   **When** operators need to diagnose or recover local projection/pub-sub failures
   **Then** `docs/runbooks/` includes a replay/rebuild/dead-letter runbook with exact local commands, expected metadata, failure modes, and safe escalation
   **And** integration/structural tests prove stable app IDs, component names, Dapr config file presence, dead-letter/resiliency configuration, service default endpoint mapping, and worker subscription metadata
   **And** a topology smoke path proves the AppHost can be built and its resource graph includes the required Projects resources without requiring payload fixtures or production secrets.

6. **Existing Epic 1 behavior remains intact.**
   Story 1.9 must not regress Stories 1.4-1.8: command-async mutations still use EventStore gateway submission; Open/List read behavior remains tenant-scoped and safe-denial aware; update/archive idempotency and strict request schema behavior remain unchanged; contract-spine and generated-client files are untouched unless topology wiring genuinely requires a metadata-only health/readiness contract change. No compiler/analyzer settings are weakened and no sibling submodule pointer changes are introduced.

## Tasks / Subtasks

- [x] **Task 1 - Convert AppHost from placeholder to executable Aspire topology** (AC: 1, 2, 5, 6)
  - [x] Update `src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj` to use the Aspire AppHost SDK pattern mirroring `Hexalith.Folders.AppHost`; mark the `Hexalith.Projects.Aspire` reference with `IsAspireProjectResource="false"`.
  - [x] Add `Program.cs` under `src/Hexalith.Projects.AppHost` using `DistributedApplication.CreateBuilder(args)`, `AddProject<Projects.*>()`, `WithReference`, `WaitFor`, and `Build().Run()`.
  - [x] Add root-level project references for sibling `Hexalith.EventStore` and `Hexalith.Tenants` via existing `$(Hexalith*Root)` MSBuild properties only. Do not add nested submodule assumptions.
  - [x] Register resources with stable app IDs: `eventstore`, `tenants`, `projects`, `projects-workers`, and `projects-ui` only if the UI project is made minimally hostable.
  - [x] Initialize the shared EventStore security resource with `builder.AddHexalithEventStoreSecurity()`; use `WithJwtBearerSecurity(...)`, `WithSecurityDependency(...)`, and, where UI OIDC is implemented, `WithOpenIdConnectSecurity(...)`; preserve `EnableKeycloak=false` without weakening production guidance.
  - [x] Resolve Dapr config paths from `AppHostDirectory` first and current directory second; throw a clear `FileNotFoundException` if required config is missing.
  - [x] Keep the existing `ProjectsAppHost.Name` placeholder class only if tests still need a marker; otherwise replace it with the executable entry point and update tests.

- [x] **Task 2 - Implement Projects Aspire orchestration module and resource contract** (AC: 1, 2, 5)
  - [x] Replace `src/Hexalith.Projects.Aspire/ProjectsAspire.cs` placeholder with an Aspire module similar to `Hexalith.Folders.Aspire/FoldersAspireModule.cs`.
  - [x] Expose constants for app IDs and Dapr component names: `EventStoreAppId`, `TenantsAppId`, `ProjectsAppId`, `ProjectsWorkersAppId`, optional `ProjectsUiAppId`, `StateStoreComponentName`, and `PubSubComponentName`.
  - [x] Add a resource record such as `HexalithProjectsResources` so structural tests can assert that topology resources are not accidentally dropped.
  - [x] Add shared Dapr components with Redis-backed `state.redis` and `pubsub.redis` semantics through CommunityToolkit Aspire Dapr APIs. Redis must remain a Dapr component backend, default to the Dapr-initialized local endpoint, and not be treated as a required Projects-owned server; do not add direct Redis access from Contracts, Client, domain core, Server business logic, or Workers handlers.
  - [x] Attach Dapr sidecars to `eventstore`, `tenants`, `projects`, and `projects-workers` with the same component references and Dapr config path.
  - [x] Apply `WithReference` and `WaitFor` from `projects`/`projects-workers` to EventStore and Tenants so command/query and tenant-event dependencies start predictably.
  - [x] Add package references without inline versions; put any required new `Aspire.Hosting`, `Aspire.Hosting.Redis`, `Aspire.Hosting.Keycloak`, and `CommunityToolkit.Aspire.Hosting.Dapr` versions in `Directory.Packages.props` with comments. Prefer the current sibling Folders pins unless restore/build proves a narrower version is required.

- [x] **Task 3 - Replace ServiceDefaults placeholder with real shared defaults** (AC: 4, 6)
  - [x] Implement `AddServiceDefaults`, `ConfigureOpenTelemetry`, `AddDefaultHealthChecks`, and `MapDefaultEndpoints` in `src/Hexalith.Projects.ServiceDefaults`, following EventStore/Folders ServiceDefaults shape.
  - [x] Add OpenTelemetry, service discovery, and HTTP resilience package references centrally only; no inline versions.
  - [x] Include ASP.NET Core, HttpClient, and runtime instrumentation; export through OTLP when `OTEL_EXPORTER_OTLP_ENDPOINT` is configured.
  - [x] Map `/health`, `/alive`, and `/ready`. Readiness must be tagged and must include Dapr/state/pubsub/projection freshness where the host owns those checks.
  - [x] Exclude health endpoints from tracing noise where local patterns already do.
  - [x] Use structured JSON/log scopes only for safe metadata. Do not log command payloads, setup text, tenant event payloads, or raw Dapr CloudEvent bodies.

- [x] **Task 4 - Make Server and Workers real runtime hosts** (AC: 2, 3, 4, 6)
  - [x] Update `src/Hexalith.Projects.Server/Program.cs` to call Projects ServiceDefaults and map default endpoints instead of the current one-off `/health` string endpoint.
  - [x] Register the production `EventStoreProjectCommandSubmitter` and EventStore gateway client in runtime host composition without changing the existing test fake patterns.
  - [x] Add `src/Hexalith.Projects.Workers/Program.cs` as an ASP.NET Core worker host that calls ServiceDefaults, `UseCloudEvents()`, `MapSubscribeHandler()`, and `MapProjectsTenantEventWorkerEndpoints()`.
  - [x] Replace or wrap the current Tenants subscription mapping if necessary so the Projects endpoint can set Dapr `TopicOptions.DeadLetterTopic`; do not edit the Tenants submodule for this story.
  - [x] Keep `ProjectsWorkersModule` constants as the single source for app ID, route, pub/sub, topic, and dead-letter topic names.
  - [x] Ensure endpoint routes used only by Dapr (`/tenants/events`, projection dispatch, dead-letter drain) are documented as internal plumbing, not public REST APIs.
  - [x] Preserve the existing `ProjectTenantEventProjectionWriter` migration switch; Workers should be the runtime writer, and non-owning hosts must not mutate projections.

- [x] **Task 5 - Add durable Dapr-backed projection and dedup stores without polluting domain core** (AC: 2, 3, 4, 6)
  - [x] Keep pure projection types in `src/Hexalith.Projects/Projections/*` infrastructure-free. Do not add `DaprClient`, Redis, HTTP, or ASP.NET dependencies to Contracts or the domain core.
  - [x] Introduce Dapr-backed store adapters in host/infrastructure code for `TenantAccessProjection`, `ProjectListProjection`, and `ProjectDetailProjection`, using the `statestore` component and canonical keys derived from `ProjectIdentity`.
  - [x] If a shared adapter is needed by both Server and Workers, create the smallest host-infrastructure boundary that preserves dependency direction (Server/Workers depend on it; Contracts/Client/domain do not). Do not make Server depend on Workers or Workers depend on Server.
  - [x] Preserve optimistic concurrency and processed-message evidence. Duplicate message IDs must be idempotent; same message ID with incompatible fingerprint must surface as replay conflict and fail closed.
  - [x] Server reads must use the durable stores in runtime composition; in-memory read models remain only for Tier-2 tests and explicit non-runtime fakes.
  - [x] Add projection freshness/watermark state needed for readiness and safe failure after restart.
  - [x] Do not duplicate fold logic. Rebuild remains `Empty.Apply(envelopes)` and runtime rebuild/drain code feeds the same envelope shape.

- [x] **Task 6 - Add Dapr component/configuration files and operational runbooks** (AC: 2, 5)
  - [x] Add `src/Hexalith.Projects.AppHost/DaprComponents/accesscontrol.yaml` with local-development warnings and explicit Projects policies for internal endpoints.
  - [x] Add `src/Hexalith.Projects.AppHost/DaprComponents/resiliency.yaml`; in self-hosted Dapr the resiliency spec must be named `resiliency.yaml`.
  - [x] Add or generate Dapr component resources for `statestore` and `pubsub` with Redis-backed metadata using the configured local Redis endpoint. Scope components to the required app IDs.
  - [x] Configure pub/sub dead-letter behavior for Tenants events and Project event projection dispatch. If using programmatic subscriptions, use Dapr `TopicOptions.DeadLetterTopic`; if using declarative subscriptions, scope them to `projects-workers`.
  - [x] Add `docs/runbooks/projects-topology.md` covering local start/stop, expected resources, health/readiness interpretation, dead-letter inspection, replay, projection rebuild, and safe failure handling.
  - [x] The runbook must not include recursive submodule commands, production secrets, payload examples, or direct Redis inspection as the primary recovery path. Use Dapr/EventStore/Admin-safe paths first.

- [x] **Task 7 - Add focused structural and integration tests** (AC: 1, 2, 3, 4, 5, 6)
  - [x] Add Aspire structural tests in `tests/Hexalith.Projects.Integration.Tests` mirroring `Hexalith.Folders.IntegrationTests/AspireTopologyTests.cs`: stable app IDs, component names, resource record shape, and component registration.
  - [x] Add tests that required Dapr config files exist, are local-dev marked where permissive, include access-control operations, include resiliency targets for apps/components, and define dead-letter behavior.
  - [x] Add ServiceDefaults tests for health endpoint mapping and readiness/liveness separation without requiring live Dapr sidecars.
  - [x] Add Workers host tests proving `UseCloudEvents`/subscription metadata, dead-letter topic metadata, and `MapSubscribeHandler` are wired.
  - [x] Add durable store adapter tests using fakes or Dapr/testcontainers only in integration scope. Pure Tier-1 projection tests must stay Dapr/Aspire/network-free.
  - [x] Extend `ProjectsIntegrationSkeletonTests` or replace it with meaningful topology tests; do not leave it as the only Tier-3 proof.
  - [x] Preserve all existing 1.8 server/domain/client tests. The topology story should not force contract-spine regeneration.

- [x] **Task 8 - Run verification and record evidence** (AC: 5, 6)
  - [x] `dotnet build Hexalith.Projects.slnx`
  - [x] `tests/tools/run-filtered-tests.ps1`
  - [x] Targeted integration/topology tests for `Hexalith.Projects.Integration.Tests`
  - [x] `tests/tools/run-contract-spine-gates.ps1` if any contract/client generation input changed; otherwise record why it was unchanged
  - [x] `tests/tools/run-openapi-fingerprint-gate.ps1` if contract artifacts are touched; otherwise record why it was unchanged
  - [x] `tests/tools/run-frontcomposer-inspect-gate.ps1` should remain skip-clean unless this story intentionally makes a minimal UI host; no generated UI is expected
  - [x] `git diff --check`
  - [x] `git status --short`, verifying no sibling submodule pointer changes, no recursive submodule churn, and unrelated `.codex/` or orchestration-state files are preserved

## Dev Notes

### Current On-Disk State to Build From

- `src/Hexalith.Projects.AppHost/ProjectsAppHost.cs` and `src/Hexalith.Projects.Aspire/ProjectsAspire.cs` are placeholder marker classes that explicitly say Story 1.9 will land the topology. `Hexalith.Projects.AppHost.csproj` currently uses `Microsoft.NET.Sdk`, not the Aspire AppHost SDK, and references Projects Server/UI/Workers plus Projects Aspire.
- `src/Hexalith.Projects.ServiceDefaults/ProjectsServiceDefaults.cs` is a placeholder marker. Server currently maps a manual `GET /health` returning `"Hexalith.Projects.Server"`; 1.9 should replace this with real ServiceDefaults endpoints.
- `src/Hexalith.Projects.Workers` has `ProjectsWorkersModule` and `ProjectsTenantEventHandler`, but no `Program.cs`. The module already registers `DaprClient`, Tenants client subscription handling, `ProjectTenantAccessHandler`, and stable metadata: app ID `projects-workers`, route `/tenants/events`, pub/sub `pubsub`, topic `system.tenants.events`.
- `TenantAccessProjection` already has `IProjectTenantAccessProjectionStore`, `InMemoryProjectTenantAccessProjectionStore`, optimistic concurrency, processed-message evidence, replay conflict/malformed flags, and `ProjectTenantEventProjectionWriter`. The in-memory store comment says it is used by tests and pre-1.9 local hosts.
- `ProjectListProjection` and `ProjectDetailProjection` are pure, deterministic, rebuildable folds. Server has `InMemoryProjectListReadModel` and `InMemoryProjectDetailReadModel`; the detail read model comment explicitly says Story 1.9 replaces it with the Dapr-backed projection store.
- `EventStoreProjectCommandSubmitter` now supports create, update setup, and archive. Story 1.9 should register it in runtime composition rather than reworking mutation behavior.
- `tests/Hexalith.Projects.Integration.Tests/ProjectsIntegrationSkeletonTests.cs` is a placeholder that says real Testcontainers/Dapr/Aspire topology tests land later. This story is that later boundary.
- `Hexalith.Folders` is the closest topology oracle: `FoldersAspireModule`, `HexalithFoldersResources`, `AppHost/Program.cs`, `DaprComponents/accesscontrol.yaml`, and `AspireTopologyTests.cs`. Mirror its shape where appropriate, but improve Projects access-control/resiliency to satisfy this story's stricter AR-20/AR-21 requirements.

### Architecture and Boundary Guardrails

- Dapr is the only infrastructure abstraction. Do not introduce direct Redis/Postgres/Cosmos/broker dependencies in Contracts, Client, domain core, Server business logic, or Workers handlers. Redis is acceptable only as the Dapr component backend configured by AppHost/Dapr resources.
- Domain core remains pure: aggregate `Handle`, state/projection `Apply`, setup validation, and idempotency hashing must stay free of Dapr, Aspire, network, filesystem, and logging side effects.
- Server and Workers may depend inward on the domain core. Client, CLI, and MCP must not reference domain event types or Dapr directly. Contracts stays low-dependency.
- EventStore owns command persistence and publishing. Do not publish domain events directly from Projects Server/Workers.
- Tenant authority remains claims/EventStore envelope evidence only. Payload/header/query tenant values are comparison evidence, never authority.
- Unauthorized, unknown, stale, unavailable, and cross-tenant states fail closed. Do not let an empty durable store after restart behave like "allowed" or "no projects found" when trust evidence is unavailable.
- Root-level submodules only. Do not add setup docs, scripts, or commands that use `git submodule update --init --recursive`.

### Version and Library Requirements

- Current root pins include .NET SDK `10.0.300`, Dapr.AspNetCore `1.17.9`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `5.3.0`, NSwag `14.7.1`, YamlDotNet `17.1.0`, and Microsoft.Extensions abstractions `10.0.8`.
- Projects root `Directory.Packages.props` does not yet pin Aspire/ServiceDefaults packages. Add any required versions centrally. Do not add inline `Version` attributes.
- Sibling Folders currently pins `Aspire.Hosting`/`Aspire.Hosting.Redis` `13.3.5`, `Aspire.AppHost.Sdk/13.2.2`, `Aspire.Hosting.Keycloak` `13.2.2-preview.1.26207.2`, `CommunityToolkit.Aspire.Hosting.Dapr` `13.0.0`, OpenTelemetry packages in the `1.15.x` family, and `Microsoft.Extensions.Http.Resilience`/`ServiceDiscovery` `10.6.0`. Use these as the local parity oracle unless restore/build incompatibility requires a documented exception.
- Do not upgrade Dapr, Aspire, Fluent UI, Roslyn, xUnit, or the .NET SDK casually. This is topology work, not a dependency modernization story.

### Previous Story Intelligence

- Story 1.8 completed update setup/archive end-to-end and review fixed setup idempotency parity, strict `requestSchemaVersion` handling, validator coverage, and projection conformance coverage. Do not alter those contracts or generated client artifacts unless topology wiring truly requires it.
- Story 1.8 verification passed build, filtered tests, contract-spine gate, OpenAPI fingerprint gate, FrontComposer inspect gate, and `git diff --check`. Use the same verification posture and add topology-specific tests.
- Story 1.8 deliberately deferred Aspire topology, production durable projection storage, and runbooks to 1.9. This story should remove those deferrals from comments where they are now stale.
- Stories 1.5-1.7 established rebuild/replay, TenantAccessProjection, layered authorization, and query-side tenant filtering. Reuse those conformance tests and helpers; do not create a second projection fold or a second authz path.

### Git Intelligence

- Recent commits show the expected sequence: `feat(story-1.8): update setup and archive projects`, `feat(story-1.7): open and list projects`, and `feat(story-1.6): tenant access and layered authorization`.
- The current pattern is to keep story work additive, run focused filtered lanes, preserve OpenAPI/client fingerprints unless intentionally changed, and avoid sibling submodule pointer churn.
- The current worktree may already contain unrelated orchestration-state and `.codex/` changes. Implementation must preserve them.

### Latest Technical Information

- Aspire AppHost is the declarative architecture/resource graph; `AddProject`, `WithReference`, and `WaitFor` express service dependencies and startup ordering. The CommunityToolkit Dapr integration provides `WithDaprSidecar`, `AddDaprStateStore`, `AddDaprPubSub`, and `AddDaprComponent` patterns. [Source: aspire.dev docs `what-is-the-apphost`; `dapr-framework-integration`]
- Aspire ServiceDefaults should configure OpenTelemetry, health checks, service discovery, and standard HttpClient resilience. Health endpoints conventionally include `/health` and `/alive`; Projects also needs `/ready` because this story has Dapr/state/pubsub/projection readiness. [Source: aspire.dev docs `c-service-defaults`; https://learn.microsoft.com/dotnet/core/diagnostics/observability-with-otel]
- Dapr v1.17 docs define resiliency specs with retries, timeouts, and circuit breakers. In self-hosted mode the file must be named `resiliency.yaml`; targets can apply policies to apps and components, including pub/sub inbound/outbound. [Source: https://docs.dapr.io/operations/resiliency/resiliency-overview/; https://docs.dapr.io/operations/resiliency/targets/]
- Dapr dead-letter topics forward undeliverable pub/sub messages and should be paired with retry resiliency; programmatic subscriptions can use `deadLetterTopic`, and Dapr.AspNetCore 1.17.9 exposes `TopicOptions.DeadLetterTopic`. [Source: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-deadletter/; local NuGet `Dapr.AspNetCore` 1.17.9 XML docs]
- Dapr service invocation access control is a `Configuration` resource with `accessControl.defaultAction`, policies, operations, HTTP verbs, and actions. The Dapr config schema documents access-control fields and operation allow/deny shape. [Source: https://docs.dapr.io/operations/configuration/invoke-allowlist/; https://docs.dapr.io/reference/resource-specs/configuration-schema/]

### Expected File / Structure Changes

```text
Directory.Packages.props                                      # MODIFY - Aspire/ServiceDefaults package versions, central only
src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj # MODIFY - Aspire.AppHost SDK + project/package refs
src/Hexalith.Projects.AppHost/Program.cs                       # NEW - distributed app entry point
src/Hexalith.Projects.AppHost/ProjectsAppHost.cs               # MODIFY/DELETE marker if replaced by Program/tests
src/Hexalith.Projects.AppHost/DaprComponents/accesscontrol.yaml # NEW
src/Hexalith.Projects.AppHost/DaprComponents/resiliency.yaml    # NEW
src/Hexalith.Projects.Aspire/Hexalith.Projects.Aspire.csproj    # MODIFY - Aspire/Dapr hosting package refs
src/Hexalith.Projects.Aspire/ProjectsAspire.cs                  # MODIFY/REPLACE - Aspire module constants/extensions
src/Hexalith.Projects.Aspire/HexalithProjectsResources.cs       # NEW - resource record
src/Hexalith.Projects.ServiceDefaults/Hexalith.Projects.ServiceDefaults.csproj # MODIFY - OTel/resilience/service discovery refs
src/Hexalith.Projects.ServiceDefaults/ProjectsServiceDefaults.cs # MODIFY/REPLACE - defaults extensions
src/Hexalith.Projects.Server/Program.cs                         # MODIFY - service defaults + default endpoints
src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs # MODIFY - runtime registration hooks only as needed
src/Hexalith.Projects.Server/TenantAccess/*Dapr*                # NEW/MODIFY - durable runtime store adapter if host-local
src/Hexalith.Projects.Workers/Hexalith.Projects.Workers.csproj  # MODIFY - service defaults/runtime refs
src/Hexalith.Projects.Workers/Program.cs                        # NEW - executable workers host
src/Hexalith.Projects.Workers/ProjectsWorkersModule.cs          # MODIFY - dead-letter constants/subscription mapping
src/Hexalith.Projects.Workers/Tenants/*                         # MODIFY only if needed for dead-letter/runtime host wiring
src/Hexalith.Projects.Infrastructure/*                          # OPTIONAL NEW - only if needed to share Dapr stores without breaking boundaries
docs/runbooks/projects-topology.md                              # NEW
docs/projection-catalog.md                                      # MODIFY - durable store/readiness/rebuild runtime notes
tests/Hexalith.Projects.Integration.Tests/*                     # MODIFY/ADD topology/config/store tests
tests/Hexalith.Projects.Server.Tests/*                          # MODIFY/ADD service-default/readiness/runtime registration tests
tests/Hexalith.Projects.Tests/*                                 # MODIFY only for pure store contract/conformance additions; no Dapr
```

If a new infrastructure project is introduced, add it to `Hexalith.Projects.slnx` and keep references one-way from Server/Workers to that project. Do not add it as a Contracts/Client/domain dependency.

### Testing Requirements

- **Pure Tier-1:** existing aggregate/projection/idempotency/schema/leakage tests must remain Dapr/Aspire/network-free. Add only pure conformance tests for key derivation, projection store contracts, or metadata filtering if needed.
- **Tier-2 host tests:** Server and Workers registration, service defaults, readiness tags, subscription metadata, authz fail-closed default, and no-payload logging/health response tests.
- **Tier-3 topology tests:** Aspire resource graph, Dapr component/config files, resiliency/dead-letter config, AppHost build/resource shape, durable state-store adapter behavior using fakes or testcontainers where justified.
- **Negative paths:** missing Dapr config file, unavailable state store, unavailable pub/sub, stale projection after restart, dead-lettered Tenants event, replay conflict, Dapr policy denial/unavailable, cross-tenant projection read.
- **No regressions:** run the existing filtered lane plus targeted integration tests. Contract-spine/OpenAPI gates should stay unchanged unless source artifacts are touched.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.9: Aspire/Dapr/Workers topology & operational skeleton] - story statement and BDD acceptance criteria.
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 1: Project Workspace Foundation] - Epic 1 objective and CI/topology foundation.
- [Source: _bmad-output/planning-artifacts/architecture.md#Infrastructure & Deployment] - Aspire topology, Dapr sidecars, workers, observability, resiliency, runbooks.
- [Source: _bmad-output/planning-artifacts/architecture.md#Architectural Boundaries] - domain core pure, Server/Workers own infrastructure, Client/CLI/MCP no Dapr.
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md#Non-Goals (Explicit)] - Projects must not bypass Dapr, EventStore, or tenant isolation.
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Design Principle: One Operational Model, Three Surfaces] - operational surfaces must be metadata-only and consistent.
- [Source: _bmad-output/implementation-artifacts/1-8-update-project-setup-archive-project.md] - previous story behavior, verification, and deferred Aspire/durable-store scope.
- [Source: _bmad-output/implementation-artifacts/1-7-open-list-projects.md] - Open/List read behavior and query-side filtering to preserve.
- [Source: _bmad-output/implementation-artifacts/1-6-tenant-access-layered-fail-closed-authorization.md] - TenantAccessProjection and layered fail-closed authorization context.
- [Source: _bmad-output/implementation-artifacts/1-5-projection-rebuild-replay-idempotency.md] - rebuild/replay and idempotency guardrails.
- [Source: _bmad-output/project-context.md] - pinned stack, Dapr-only infra, central package management, tenant isolation, no recursive submodules.
- [Source: src/Hexalith.Projects.AppHost/ProjectsAppHost.cs] - current placeholder AppHost marker.
- [Source: src/Hexalith.Projects.Aspire/ProjectsAspire.cs] - current placeholder Aspire marker.
- [Source: src/Hexalith.Projects.ServiceDefaults/ProjectsServiceDefaults.cs] - current placeholder ServiceDefaults marker.
- [Source: src/Hexalith.Projects.Workers/ProjectsWorkersModule.cs] - current worker constants, Tenants subscription registration, and endpoint mapping.
- [Source: src/Hexalith.Projects.Workers/Tenants/TenantEventHandlers/ProjectsTenantEventHandler.cs] - current Tenants event handler and ProjectionWriter guard.
- [Source: src/Hexalith.Projects/Projections/TenantAccess] - existing tenant projection store interface, in-memory pre-1.9 store, handler, options, and evidence model.
- [Source: src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs] and [Source: src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailProjection.cs] - pure projection folds to reuse.
- [Source: src/Hexalith.Projects.Server/InMemoryProjectDetailReadModel.cs] - current comment deferring Dapr-backed projection store to Story 1.9.
- [Source: Hexalith.Folders/src/Hexalith.Folders.Aspire/FoldersAspireModule.cs] - sibling Aspire/Dapr module pattern.
- [Source: Hexalith.Folders/src/Hexalith.Folders.AppHost/Program.cs] - sibling AppHost topology, Keycloak, config path resolution pattern.
- [Source: Hexalith.Folders/tests/Hexalith.Folders.IntegrationTests/AspireTopologyTests.cs] - sibling structural topology test pattern.
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Extensions.cs] - richer local ServiceDefaults pattern with OTel/health/readiness.
- [Source: aspire.dev docs `dapr-framework-integration`] - current Aspire CommunityToolkit Dapr sidecar/component APIs.
- [Source: aspire.dev docs `c-service-defaults`] - ServiceDefaults extension methods, OTel, health, service discovery, and HTTP resilience.
- [Source: https://learn.microsoft.com/dotnet/core/diagnostics/observability-with-otel] - .NET OpenTelemetry and Aspire ServiceDefaults guidance.
- [Source: https://docs.dapr.io/operations/resiliency/resiliency-overview/] - Dapr resiliency spec and self-hosted filename requirement.
- [Source: https://docs.dapr.io/operations/resiliency/targets/] - app/component resiliency targets for service invocation and pub/sub.
- [Source: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-deadletter/] - dead-letter topics and retry policy guidance.
- [Source: https://docs.dapr.io/operations/configuration/invoke-allowlist/] - Dapr service invocation access control scenarios.
- [Source: https://docs.dapr.io/reference/resource-specs/configuration-schema/] - Dapr Configuration access-control schema.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- RED: `dotnet test tests\Hexalith.Projects.Integration.Tests\Hexalith.Projects.Integration.Tests.csproj --no-restore` failed before implementation as expected because the Aspire topology, Dapr config files, infrastructure project, and runtime store types did not exist yet.
- `dotnet build Hexalith.Projects.slnx` - PASSED; 0 warnings, 0 errors.
- `tests\tools\run-filtered-tests.ps1` - PASSED; Contracts 115/115, Client 20/20, Projects 128/128, Server 68/68.
- `dotnet test tests\Hexalith.Projects.Integration.Tests\Hexalith.Projects.Integration.Tests.csproj --no-build` - PASSED; 10/10.
- `tests\tools\run-frontcomposer-inspect-gate.ps1` - PASSED skip-clean; no `[Projection]`/`[Command]` contracts present and no generated UI expected.
- `git diff --check` - PASSED; only Git CRLF normalization warnings, no whitespace errors.
- `git status --short --untracked-files=all` - reviewed; no sibling submodule pointer changes, no recursive submodule churn, pre-existing `.codex/` and orchestration-state edits preserved.
- Contract-spine and OpenAPI fingerprint gates were not run because Contracts/OpenAPI/generated-client inputs and outputs were not touched by this topology story.
- REVIEW cycle 1: `dotnet test tests\Hexalith.Projects.Integration.Tests\Hexalith.Projects.Integration.Tests.csproj --no-restore --filter "FullyQualifiedName~DaprProjectionStoreTests"` - PASSED; 7/7.
- REVIEW cycle 1: `dotnet test tests\Hexalith.Projects.Server.Tests\Hexalith.Projects.Server.Tests.csproj --no-restore --filter "FullyQualifiedName~ServiceDefaultsEndpointTests"` - PASSED; 3/3.
- REVIEW cycle 1: `dotnet build Hexalith.Projects.slnx` - PASSED; 0 warnings, 0 errors.
- REVIEW cycle 1: `tests\tools\run-filtered-tests.ps1` - PASSED; Contracts 115/115, Client 20/20, Projects 128/128, Server 69/69.
- REVIEW cycle 1: `dotnet test tests\Hexalith.Projects.Integration.Tests\Hexalith.Projects.Integration.Tests.csproj --no-build` - PASSED; 14/14.
- REVIEW cycle 1: `tests\tools\run-frontcomposer-inspect-gate.ps1` - PASSED skip-clean; no FrontComposer annotations/generated UI expected.
- REVIEW cycle 1: `git diff --check` - PASSED; only Git LF-to-CRLF normalization warnings, no whitespace errors.
- REVIEW cycle 1: `git status --short --untracked-files=all` - reviewed; no sibling submodule pointer changes, no recursive submodule churn, `.codex/` local files preserved. Contract-spine and OpenAPI fingerprint gates were not run because no Contracts/OpenAPI/generated-client inputs or outputs changed during review.
- REVIEW cycle 1: Aspire MCP `doctor` - PASSED with warnings; .NET 10.0.300 and Docker running, no failures; warnings only for multiple/older HTTPS development certificates.

### Completion Notes List

- Replaced AppHost/Aspire placeholders with an executable Aspire topology for EventStore, Tenants, Projects Server, Projects Workers, shared EventStore security resource opt-out, Redis-backed Dapr `statestore`/`pubsub`, Dapr sidecars, config path validation, references, and `WaitFor` ordering.
- Added Projects ServiceDefaults for OpenTelemetry, service discovery, HTTP resilience, `/health`, `/alive`, and `/ready`.
- Added runtime Dapr infrastructure adapters for tenant access and project projection journals, preserving pure projection folds and fail-closed replay-conflict/malformed behavior.
- Made Server and Workers executable runtime hosts, with EventStore gateway runtime registration, durable runtime read models, CloudEvents, `MapSubscribeHandler`, and programmatic Dapr subscriptions with dead-letter topics.
- Added local Dapr access-control/resiliency config, operational runbook, projection catalog runtime notes, topology/config/store tests, service-default tests, and worker subscription metadata tests.

### File List

- Directory.Packages.props
- Hexalith.Projects.slnx
- _bmad-output/implementation-artifacts/1-9-aspire-dapr-workers-topology-operational-skeleton.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- docs/projection-catalog.md
- docs/runbooks/projects-topology.md
- src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj
- src/Hexalith.Projects.AppHost/ProjectsAppHost.cs
- src/Hexalith.Projects.AppHost/Program.cs
- src/Hexalith.Projects.AppHost/DaprComponents/accesscontrol.yaml
- src/Hexalith.Projects.AppHost/DaprComponents/resiliency.yaml
- src/Hexalith.Projects.Aspire/Hexalith.Projects.Aspire.csproj
- src/Hexalith.Projects.Aspire/HexalithProjectsResources.cs
- src/Hexalith.Projects.Aspire/ProjectsAspire.cs
- src/Hexalith.Projects.Aspire/ProjectsAspireModule.cs
- src/Hexalith.Projects.Infrastructure/DaprProjectProjectionStore.cs
- src/Hexalith.Projects.Infrastructure/DaprProjectTenantAccessProjectionStore.cs
- src/Hexalith.Projects.Infrastructure/DaprProjectsStateStore.cs
- src/Hexalith.Projects.Infrastructure/Hexalith.Projects.Infrastructure.csproj
- src/Hexalith.Projects.Infrastructure/IProjectProjectionStore.cs
- src/Hexalith.Projects.Infrastructure/IProjectsStateStore.cs
- src/Hexalith.Projects.Infrastructure/ProjectEventProjectionProcessor.cs
- src/Hexalith.Projects.Infrastructure/ProjectProjectionAppendResult.cs
- src/Hexalith.Projects.Infrastructure/ProjectProjectionAppendStatus.cs
- src/Hexalith.Projects.Infrastructure/ProjectProjectionReadiness.cs
- src/Hexalith.Projects.Infrastructure/ProjectsInfrastructureServiceCollectionExtensions.cs
- src/Hexalith.Projects.Infrastructure/ProjectsStateEntry.cs
- src/Hexalith.Projects.Infrastructure/ProjectsStateStoreOptions.cs
- src/Hexalith.Projects.Server/DaprProjectDetailReadModel.cs
- src/Hexalith.Projects.Server/DaprProjectListReadModel.cs
- src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj
- src/Hexalith.Projects.Server/InMemoryProjectDetailReadModel.cs
- src/Hexalith.Projects.Server/Program.cs
- src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs
- src/Hexalith.Projects.ServiceDefaults/Hexalith.Projects.ServiceDefaults.csproj
- src/Hexalith.Projects.ServiceDefaults/ProjectsServiceDefaults.cs
- src/Hexalith.Projects.Workers/Hexalith.Projects.Workers.csproj
- src/Hexalith.Projects.Workers/Program.cs
- src/Hexalith.Projects.Workers/ProjectsWorkersModule.cs
- src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailProjection.cs
- src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs
- src/Hexalith.Projects/Projections/TenantAccess/InMemoryProjectTenantAccessProjectionStore.cs
- tests/Hexalith.Projects.Integration.Tests/AspireTopologyTests.cs
- tests/Hexalith.Projects.Integration.Tests/DaprConfigurationTests.cs
- tests/Hexalith.Projects.Integration.Tests/DaprProjectionStoreTests.cs
- tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj
- tests/Hexalith.Projects.Server.Tests/ServiceDefaultsEndpointTests.cs
- tests/Hexalith.Projects.Server.Tests/WorkersEndpointMetadataTests.cs

### Senior Developer Review (AI)

- Reviewer: GPT-5 Codex on 2026-05-25.
- Outcome: approved after auto-fixes; 0 CRITICAL issues remain.
- References checked: Aspire docs `what-is-the-apphost`, `dapr-framework-integration`, and `c-service-defaults`.

Findings fixed:

- [HIGH] The durable project projection journal used per-aggregate `SequenceNumber` as a tenant-wide watermark, so a second project aggregate starting at sequence `1` would be dropped as out-of-order. Fixed by using EventStore global position as the projection watermark and added multi-project proof coverage.
- [HIGH] Project projection append accepted envelope/event tenant or aggregate mismatches as applied no-ops. Fixed by marking the journal malformed and failing closed on identity mismatch.
- [MEDIUM] Duplicate message fingerprinting ignored EventStore metadata such as aggregate id and global position. Fixed the fingerprint to include event type, tenant, aggregate, sequence, global position, and payload hash.
- [MEDIUM] Runtime reads could return empty/null when the durable projection journal was absent, malformed, or conflicted. Fixed Dapr-backed list/detail reads to fail closed so Server endpoints map the condition to unavailable instead of silently satisfying reads.
- [MEDIUM] Runtime Server composition kept the fail-closed EventStore and Dapr policy evidence placeholders registered after adding runtime infrastructure. Fixed `AddProjectsServerRuntimeInfrastructure` to replace them with explicit local runtime evidence providers and added registration coverage.

Review verification:

- `dotnet test tests\Hexalith.Projects.Integration.Tests\Hexalith.Projects.Integration.Tests.csproj --no-restore --filter "FullyQualifiedName~DaprProjectionStoreTests"` - PASSED; 7/7.
- `dotnet test tests\Hexalith.Projects.Server.Tests\Hexalith.Projects.Server.Tests.csproj --no-restore --filter "FullyQualifiedName~ServiceDefaultsEndpointTests"` - PASSED; 3/3.
- `dotnet build Hexalith.Projects.slnx` - PASSED; 0 warnings, 0 errors.
- `tests\tools\run-filtered-tests.ps1` - PASSED; Contracts 115/115, Client 20/20, Projects 128/128, Server 69/69.
- `dotnet test tests\Hexalith.Projects.Integration.Tests\Hexalith.Projects.Integration.Tests.csproj --no-build` - PASSED; 14/14.
- `tests\tools\run-frontcomposer-inspect-gate.ps1` - PASSED skip-clean.
- `git diff --check` - PASSED; only LF-to-CRLF normalization warnings.
- `git status --short --untracked-files=all` - reviewed; no sibling submodule pointer changes, no recursive submodule churn, `.codex/` local files preserved.
- Aspire MCP `doctor` - PASSED with warnings; .NET SDK and Docker checks passed, HTTPS dev certificate cleanup recommended but not blocking.

### Change Log

- 2026-05-25: Created Story 1.9 context for Aspire/Dapr/Workers topology and operational skeleton; status set to ready-for-dev.
- 2026-05-25: Implemented Story 1.9 Aspire/Dapr/Workers topology, durable runtime projection stores, ServiceDefaults, Dapr config, runbook, and proof tests; status set to review.
- 2026-05-25: Completed story-automator code review cycle 1; auto-fixed 2 HIGH and 3 MEDIUM issues, verified gates, and set status to done.
