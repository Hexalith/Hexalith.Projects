---
stepsCompleted: [1, 2, 3, 4, 5, 6]
inputDocuments: []
workflowType: 'research'
lastStep: 6
research_type: 'technical'
research_topic: 'Hexalith.Tenants in Hexalith.Projects tenant management and isolation'
research_goals: 'Study how to use Hexalith.Tenants in the Hexalith.Projects module to implement tenant management and isolation'
user_name: 'Jerome'
date: '2026-05-24'
web_research_enabled: true
source_verification: true
---

# Research Report: Technical

**Date:** 2026-05-24
**Author:** Jerome
**Research Type:** technical

---

## Research Overview

This technical research studies how `Hexalith.Projects` should use `Hexalith.Tenants` to implement tenant management and tenant isolation for a tenant-aware AI project workspace. The research combines local source and planning artifacts from `Hexalith.Projects`, `Hexalith.Tenants`, `Hexalith.EventStore`, `Hexalith.Parties`, `Hexalith.Folders`, `Hexalith.Conversations`, and `Hexalith.FrontComposer` with current public documentation from Microsoft, Dapr, CloudEvents, and OWASP.

The core finding is that `Hexalith.Projects` should be implemented as its own Hexalith.EventStore/Dapr bounded context. `Hexalith.Tenants` should remain the source of truth for tenant lifecycle, membership, role, status, and configuration, while Projects consumes Tenants contracts/events and maintains local fail-closed tenant access projections. Projects must own only project identity, setup, lifecycle, context links, project resolution metadata, and metadata-only audit/explanation records.

The full synthesis below converts the research into an implementation strategy: scaffold Projects from the Tenants module shape, build pure contracts/aggregates/projections first, wire Tenants event consumption through `Hexalith.Tenants.Client`, enforce object-level authorization and query-side filtering, and add production gates for durable projections, idempotent event handling, Dapr access control, resiliency, dead letters, observability, and replay/rebuild operations.

---

<!-- Content will be appended sequentially through research workflow steps -->

## Technical Research Scope Confirmation

**Research Topic:** Hexalith.Tenants in Hexalith.Projects tenant management and isolation
**Research Goals:** Study how to use Hexalith.Tenants in the Hexalith.Projects module to implement tenant management and isolation

**Technical Research Scope:**

- Architecture Analysis - design patterns, frameworks, system architecture
- Implementation Approaches - development methodologies, coding patterns
- Technology Stack - languages, frameworks, tools, platforms
- Integration Patterns - APIs, protocols, interoperability
- Performance Considerations - scalability, optimization, patterns

**Research Methodology:**

- Current web data with rigorous source verification
- Multi-source validation for critical technical claims
- Confidence level framework for uncertain information
- Comprehensive technical coverage with architecture-specific insights

**Scope Confirmed:** 2026-05-24

## Technology Stack Analysis

### Programming Languages

Hexalith.Projects should follow the existing Hexalith module baseline: C# on .NET, using SDK-style projects and `net10.0` where the workspace already pins .NET 10. The root project context states the umbrella repo uses `.NET SDK 10.0.302`, nullable reference types, implicit usings, warnings-as-errors, and module-specific solution files. Microsoft's target framework documentation lists `net10.0` as the target framework moniker for .NET 10, which aligns with the current repo configuration.

For tenant integration, C# records remain the right contract shape for commands, events, rejections, and query DTOs. `Hexalith.Tenants.Contracts` already exposes command/event/query contracts such as `CreateTenant`, `AddUserToTenant`, `TenantCreated`, `UserAddedToTenant`, `ListTenantsQuery`, and `GetUserTenantsQuery`. Projects should define its own project-domain contracts rather than reusing tenant event payloads as project state, but it should reference `Hexalith.Tenants.Contracts` for tenant identifiers, roles, statuses, and event handling.

_Popular Languages:_ C# is the dominant language in this repo for service modules, domain contracts, projections, and tests.
_Emerging Languages:_ No alternate runtime language is justified for v1 Projects; TypeScript/Playwright may apply only to E2E or UI layers.
_Language Evolution:_ Keep additive, serialization-tolerant `System.Text.Json` contracts rather than versioned event forks.
_Performance Characteristics:_ C#/.NET is suitable for command/query services, projection processing, and Dapr/Aspire-hosted distributed app modules.
_Sources:_ Local `_bmad-output/project-context.md`; local `Hexalith.Tenants/_bmad-output/project-context.md`; Microsoft target frameworks documentation: https://learn.microsoft.com/en-us/dotnet/standard/frameworks

### Development Frameworks and Libraries

Projects should reuse the same server-side stack used by Tenants and sibling Hexalith modules:

- ASP.NET Core for API endpoints, authentication, authorization, Problem Details responses, and health endpoints.
- MediatR with EventStore `SubmitCommand` / `SubmitQuery` patterns for command and query dispatch.
- Hexalith.EventStore for command envelopes, aggregate identity, event sourcing, projection routing, command status, and tenant validation primitives.
- Hexalith.Tenants packages for tenant contracts, client-side event processing, local projection state, testing fakes, and Aspire topology integration.
- FluentValidation only for structural command validation where domain `Handle` methods are not sufficient.
- xUnit v3, Shouldly, NSubstitute, and existing Hexalith testing packages for fast unit and conformance tests.

ASP.NET Core authorization documentation supports the repo's layered model: role and claim checks are expressed through authorization services and policies, while resource-specific decisions belong in application/domain logic. This maps cleanly to Projects: API/JWT checks should establish authenticated tenant/user context, while project operations must still enforce tenant membership, resource binding, and project-level authorization locally.

_Major Frameworks:_ ASP.NET Core, MediatR, Hexalith.EventStore, Dapr SDK, .NET Aspire.
_Micro-frameworks:_ `Hexalith.Tenants.Client` provides focused DI/event handling abstractions (`AddHexalithTenants`, `MapTenantEventSubscription`) that consuming services can adopt without hosting Tenants internals.
_Evolution Trends:_ The workspace is standardizing on EventStore-hosted command pipelines and Dapr sidecars rather than direct database/broker dependencies.
_Ecosystem Maturity:_ Official ASP.NET Core, Dapr, and Aspire docs cover the underlying runtime contracts; Hexalith.Tenants provides local domain-specific packages.
_Sources:_ `Hexalith.Tenants/src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs`; `Hexalith.Tenants/src/Hexalith.Tenants.Client/Subscription/TenantEventSubscriptionEndpoints.cs`; ASP.NET Core role authorization: https://learn.microsoft.com/en-us/aspnet/core/mvc/security/authorization/roles?view=aspnetcore-10.0; ASP.NET Core policy authorization: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0

### Database and Storage Technologies

Projects should not introduce direct database or broker dependencies in the domain layer. The workspace rules explicitly require Dapr as the infrastructure abstraction and EventStore identity for state keys, topics, projection keys, and logs. Tenants uses Dapr-backed EventStore state and projections, with Redis-backed Dapr components in Aspire for local/shared state. Dapr's official state management documentation describes pluggable state stores and optimistic concurrency via ETags; this supports the existing Hexalith pattern of projection writes guarded by ETags or actor state.

For Projects, the durable source of truth should be event-sourced project aggregates in EventStore. Tenant membership/status should be a local projection derived from `Hexalith.Tenants` events, not an ad hoc cross-service query on every request. A local projection lets Projects fail closed when tenant state is missing, disabled, stale, malformed, or unavailable, as seen in Parties, Folders, and Conversations.

_Relational Databases:_ Not indicated for v1 Projects domain storage; any concrete store should remain behind Dapr/EventStore abstractions.
_NoSQL Databases:_ Dapr state components can back EventStore/projections without Projects depending directly on the store.
_In-Memory Databases:_ In-memory tenant projection stores are useful for tests and local MVP paths but are not enough for scaled production deduplication or multi-replica projection consistency.
_Data Warehousing:_ Not in scope for tenant management/isolation v1.
_Sources:_ Dapr state management: https://docs.dapr.io/developing-applications/building-blocks/state-management/state-management-overview/; local `Hexalith.Tenants/src/Hexalith.Tenants.Aspire/HexalithTenantsExtensions.cs`; local `Hexalith.Parties/src/Hexalith.Parties/Authorization/TenantAccessService.cs`; local `Hexalith.Folders/src/Hexalith.Folders/Authorization/TenantAccessAuthorizer.cs`

### Development Tools and Platforms

Projects should use the repo's established tooling rather than creating a separate build lane. The relevant local platform pieces are:

- Module-specific `.slnx` files and targeted `dotnet build` / `dotnet test` commands.
- Central package management through `Directory.Packages.props`.
- Dapr CLI/runtime for local actor, state, pub/sub, and service invocation behavior.
- Aspire AppHost/service defaults for local distributed topology, health, telemetry, and service discovery.
- Docker for local backing services used by Aspire/Dapr.

Microsoft's Aspire service defaults documentation describes service defaults as shared extension methods for telemetry, health checks, and service discovery. This matches `Hexalith.Tenants.ServiceDefaults` and should be mirrored in Projects if/when a Projects host is added.

_IDE and Editors:_ No repo-specific constraint beyond .NET-compatible tooling.
_Version Control:_ The root is an umbrella repository with root-level submodules only; no recursive submodule initialization should be used.
_Build Systems:_ .NET SDK, central package management, module-local solutions.
_Testing Frameworks:_ xUnit v3/Shouldly/NSubstitute for Tenants-style modules; Playwright only if Projects exposes UI/E2E workflows.
_Sources:_ Local `_bmad-output/project-context.md`; Microsoft Aspire service defaults: https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/service-defaults

### Cloud Infrastructure and Deployment

The Projects module should be designed for the same distributed-app platform as Tenants:

- Dapr sidecars for service invocation, pub/sub, actors, state, resiliency, and access-control boundaries.
- Aspire AppHost for local topology composition and service-default wiring.
- Containers through .NET SDK publishing conventions where applicable.
- OpenTelemetry/health checks through service defaults.

Dapr's official API reference groups service invocation, state management, pub/sub, actors, secrets, health, workflows, and related APIs as building blocks. Dapr actor documentation states actors are stateful objects with unique identity and state stored in the configured provider; this fits Hexalith.EventStore aggregate/projection actor conventions. Dapr CloudEvents documentation states Dapr uses CloudEvents 1.0 for pub/sub metadata, tracing, content type, and sender verification; this matches the Tenants event delivery model.

_Major Cloud Providers:_ Azure Container Apps is already referenced in Tenants package configuration, but Projects should remain cloud-portable through Dapr/Aspire abstractions.
_Container Technologies:_ .NET SDK containers and Docker-backed local topology are the established pattern.
_Serverless Platforms:_ Not a primary fit for actor/event-sourced command processing in this workspace.
_CDN and Edge Computing:_ Not relevant to tenant management/isolation v1.
_Sources:_ Dapr API reference: https://docs.dapr.io/reference/api/; Dapr actors overview: https://docs.dapr.io/developing-applications/building-blocks/actors/actors-overview/; Dapr CloudEvents pub/sub: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-cloudevents/; Aspire documentation: https://learn.microsoft.com/en-us/dotnet/aspire

### Technology Adoption Trends

The local architecture trend is clear: Hexalith modules are moving tenant isolation into multiple defensive layers instead of relying on a single API gate. Examples:

- Tenants owns tenant lifecycle, membership, role, status, and configuration contracts.
- Parties checks local tenant projection state and fails closed on unavailable state.
- Folders validates authoritative tenant binding, requested tenant binding, projection freshness, enabled status, and principal membership.
- Conversations guards protected operations after tenant access checks and records bounded denial telemetry.
- FrontComposer validates tenant/user segments, rejects tenant mismatches, blocks synthetic contexts unless configured, and revalidates stale snapshots.

For Projects, the same trend implies a local tenant access service backed by tenant events, plus project-domain checks for project ownership, linked resource authorization evidence, and context-assembly filtering. The key adoption decision is to treat `Hexalith.Tenants` as the system of record for tenant membership/status while making `Hexalith.Projects` responsible for its own project authorization and data minimization.

_Migration Patterns:_ New modules should subscribe to tenant events and project local access state instead of querying Tenants synchronously for every operation.
_Emerging Technologies:_ Aspire + Dapr remains the active platform pattern for local distributed development and sidecar-based infrastructure abstraction.
_Legacy Technology:_ Direct persistence/broker clients and recursive submodule assumptions are explicitly disallowed by repo rules.
_Community Trends:_ Official ASP.NET Core docs note there is no built-in multi-tenant authentication solution, so Hexalith's explicit tenant-context and projection-based isolation model is appropriate rather than relying on framework magic.
_Sources:_ ASP.NET Core authentication overview: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-10.0; local `Hexalith.Conversations/src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessGuard.cs`; local `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Infrastructure/Tenancy/FrontComposerTenantContextAccessor.cs`

## Integration Patterns Analysis

### API Design Patterns

Hexalith.Projects should expose a Projects-specific REST/query surface for Chatbot while keeping command writes on the established EventStore command path. The Tenants host demonstrates the pattern: a single imported EventStore command endpoint (`POST /api/v1/commands` in docs, controller imported from EventStore), thin REST query controllers that translate HTTP requests into `SubmitQuery`, and domain processing through `/process` invoked by EventStore aggregate actors.

For Projects, the write-side API should use Project commands such as `CreateProject`, `UpdateProjectSetup`, `ArchiveProject`, `LinkConversationToProject`, `SetProjectFolder`, `LinkProjectFileReference`, and `LinkProjectMemory`. These commands should carry tenant and project identity through the EventStore envelope, not through ad hoc HTTP-only state. The read-side API should provide task-specific endpoints for Chatbot: list/open project, resolve project from conversation or attachments, get project context, explain context selection, and refresh project context.

Projects should not expose tenant lifecycle management directly. Tenant creation, enabling/disabling, user membership, and role changes remain Tenants commands (`CreateTenant`, `AddUserToTenant`, `ChangeUserRole`, etc.). Projects can invoke Tenants command/query APIs only through the approved EventStore/Admin HTTP/Dapr paths when a workflow explicitly needs to coordinate tenant provisioning; normal request authorization should use local projected tenant state.

_RESTful APIs:_ Use REST for Chatbot-facing project reads and workflow endpoints, translating to EventStore queries internally.
_GraphQL APIs:_ No local evidence supports GraphQL for v1; it would add an authorization/filtering surface that the PRD does not require.
_RPC and gRPC:_ Dapr service invocation may use HTTP or gRPC between sidecars, but Projects code should stay at the Dapr/EventStore abstraction layer.
_Webhook Patterns:_ Not needed for first-party modules; Dapr pub/sub subscriptions are the local event callback model.
_Sources:_ Local `Hexalith.Tenants/src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`; local `Hexalith.Tenants/src/Hexalith.Tenants/Program.cs`; Dapr service invocation overview: https://docs.dapr.io/developing-applications/building-blocks/service-invocation/service-invocation-overview/

### Communication Protocols

The runtime integration should use three communication paths:

1. Synchronous HTTP through ASP.NET Core for external/Chatbot API calls.
2. Dapr service invocation for service-to-service calls, including EventStore invoking Projects' `/process` endpoint and project services invoking bounded-context APIs where approved.
3. Dapr pub/sub for asynchronous tenant event propagation into Projects' local tenant-access projection.

Dapr service invocation provides service discovery, standard HTTP/gRPC invocation, mTLS-capable sidecar communication, access control, observability, and resiliency hooks. This matches Hexalith's Dapr sidecar architecture and avoids direct calls to infrastructure clients. Dapr resiliency specs can apply retries, timeouts, and circuit breakers to apps, components, and actors, so Projects should use explicit resiliency policies for Tenants/EventStore/Conversations/Folders/Memories integration paths rather than custom retry loops inside domain logic.

_HTTP/HTTPS Protocols:_ Use ASP.NET Core endpoints for Chatbot and query surfaces; Dapr sidecars handle service-to-service routing.
_WebSocket Protocols:_ Not required for tenant management/isolation; if Chatbot needs live updates, use existing SignalR/EventStore notification patterns rather than inventing a Projects-specific socket protocol.
_Message Queue Protocols:_ Dapr pub/sub abstracts broker details; topic subscriptions and dead-letter behavior belong in Dapr component/subscription config.
_gRPC and Protocol Buffers:_ Dapr sidecars may communicate over gRPC internally; Projects does not need public protobuf contracts for v1.
_Sources:_ Dapr service invocation overview: https://docs.dapr.io/developing-applications/building-blocks/service-invocation/service-invocation-overview/; Dapr resiliency overview: https://docs.dapr.io/operations/resiliency/resiliency-overview/; Dapr access control allowlist: https://docs.dapr.io/operations/configuration/invoke-allowlist/

### Data Formats and Standards

Contracts should use `System.Text.Json`, camelCase HTTP JSON where controllers expose REST responses, and EventStore's event envelope for persisted/published events. Tenants events are delivered as CloudEvents through Dapr pub/sub and include EventStore metadata such as `MessageId`, `AggregateId`, `TenantId`, `EventTypeName`, `SequenceNumber`, `Timestamp`, `CorrelationId`, and serialized payload bytes. Dapr CloudEvents documentation confirms Dapr uses CloudEvents 1.0 to add context such as tracing, content type, and sender information to pub/sub messages.

Projects should treat tenant event payloads as integration facts, not as project-domain events. A `TenantDisabled` event should update local tenant access state; it should not become a Project aggregate event. Project events should be named in the Project domain and include the project tenant ID as a top-level payload field according to Hexalith rules.

_JSON and XML:_ JSON is the standard for commands, queries, event payloads, and REST responses; XML is not indicated.
_Protobuf and MessagePack:_ Not justified for v1 because EventStore/Dapr/ASP.NET Core integration already standardizes JSON/event envelopes.
_CSV and Flat Files:_ Not relevant to runtime tenant isolation.
_Custom Data Formats:_ Use Hexalith.EventStore envelopes and CloudEvents rather than custom event wrapper formats.
_Sources:_ Local `Hexalith.Tenants/src/Hexalith.Tenants.Client/Subscription/TenantEventEnvelope.cs`; local `Hexalith.Tenants/src/Hexalith.Tenants.Client/Subscription/TenantEventProcessor.cs`; Dapr CloudEvents: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-cloudevents/; CloudEvents specification: https://cloudevents.io/

### System Interoperability Approaches

Projects is an orchestration/coordinator bounded context, but it must not take ownership of data that belongs to Conversations, Folders, or Memories. The PRD explicitly states Projects stores references and metadata, not full transcripts, file contents, memory payloads, raw prompts, secrets, or unrestricted filesystem paths. Therefore interoperability should be reference-first and authorization-aware:

- Store stable identifiers and bounded metadata for linked Conversations, Project Folder, File References, and Memories.
- Before linking or context assembly, verify the authoritative bounded context has tenant/user authorization evidence.
- Fail closed when referenced resource authorization, lifecycle, or availability cannot be verified.
- Record metadata-only explanations for inclusion/exclusion decisions.

This favors local project state plus bounded-context verification over a central API gateway that composes raw data from every service. It also avoids point-to-point persistence coupling because all infrastructure access remains behind EventStore/Dapr.

_Point-to-Point Integration:_ Acceptable only through Dapr service invocation and bounded-context APIs; no direct database/shared table integration.
_API Gateway Patterns:_ Chatbot can act as the workflow caller, but Projects should not become a gateway that bypasses owning services' authorization.
_Service Mesh:_ Dapr sidecars provide the service invocation, mTLS, tracing, and access-control layer used by this repo.
_Enterprise Service Bus:_ Not needed; Dapr pub/sub plus EventStore envelopes cover the integration event needs.
_Sources:_ Local PRD sections 4.2, 4.4, 5, and 7; Dapr service invocation overview: https://docs.dapr.io/developing-applications/building-blocks/service-invocation/service-invocation-overview/; Dapr mTLS docs: https://docs.dapr.io/operations/security/mtls/

### Microservices Integration Patterns

Projects should follow the same bounded-context pattern described in Microsoft's DDD/CQRS microservices guidance: each microservice owns its model, commands, queries, and integration events. Locally, the Hexalith pattern is more specific:

- Commands go through EventStore and aggregate actors.
- Domain service code exposes `/process` for aggregate actor callbacks.
- Queries go through projection actors/read models.
- Cross-service state needed for authorization is projected from events.
- Dapr access control scopes which sidecar app IDs can invoke which endpoints.

The local pattern in Parties is the closest consumer reference: `TenantAccessService` reads `ITenantProjectionStore`, denies missing/unknown/disabled tenants, checks user membership/role, and fails closed when projection lookup fails. Folders adds stricter tenant-binding and freshness checks. Conversations wraps protected operations so the operation only runs after tenant access is allowed. Projects should combine these patterns: tenant membership/status from Tenants projection, project authorization from Project aggregates/read models, and referenced-resource authorization from owning contexts.

_API Gateway Pattern:_ Keep Chatbot-facing composition explicit; do not centralize domain logic in gateway code.
_Service Discovery:_ Use Dapr service invocation and Aspire topology.
_Circuit Breaker Pattern:_ Use Dapr resiliency specs for service invocation targets where failures are expected to be transient.
_Saga Pattern:_ For create-project flows that may also create folders or link conversations, prefer explicit process state and compensating commands/events. Do not perform hidden multi-service writes in a single synchronous controller action.
_Sources:_ Microsoft DDD/CQRS microservices guidance: https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/; local `Hexalith.Parties/src/Hexalith.Parties/Authorization/TenantAccessService.cs`; local `Hexalith.Folders/src/Hexalith.Folders/Authorization/TenantAccessAuthorizer.cs`; local `Hexalith.Conversations/src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessGuard.cs`

### Event-Driven Integration

Projects should subscribe to Tenants integration events and maintain a local tenant access projection. `Hexalith.Tenants.Client` already provides:

- `AddHexalithTenants()` for DI registration.
- `MapTenantEventSubscription()` for Dapr topic subscription.
- `TenantEventProcessor` for event type resolution, deserialization, deduplication by `MessageId`, and dispatch to typed handlers.
- `TenantProjectionEventHandler` and `ITenantProjectionStore` for local tenant state.

Dapr pub/sub provides at-least-once delivery, so Projects handlers must be idempotent. The existing Tenants docs call out the need for message-level deduplication and handler-level idempotency; production deployments should use bounded or external deduplication because the default in-memory processed-message dictionary resets on restart and is per instance. Dapr dead-letter topics should be configured for messages Projects cannot process, with retry policies applied before dead-letter routing.

Recommended Projects event subscriptions:

- Subscribe to Tenants events to maintain tenant status, membership, role, and configuration facts required for access checks.
- Optionally subscribe to Conversations/Folders/Memories metadata events if those modules publish stable reference lifecycle events; otherwise verify references synchronously through bounded-context APIs when linking or assembling context.
- Publish Projects domain events for project lifecycle, setup changes, links/unlinks, resolution confirmation, and archive decisions.

_Publish-Subscribe Patterns:_ Dapr pub/sub topic subscription for Tenants events, with typed local handlers.
_Event Sourcing:_ Projects aggregate state should be event-sourced through Hexalith.EventStore; tenant facts are projected integration state.
_Message Broker Patterns:_ Dapr abstracts the concrete broker; configure retry/dead-letter behavior in Dapr resources.
_CQRS Patterns:_ Separate Project commands from Project query/context read models.
_Sources:_ Dapr pub/sub overview: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/; Dapr dead-letter topics: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-deadletter/; local `Hexalith.Tenants/docs/idempotent-event-processing.md`; local `Hexalith.Tenants/src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs`

### Integration Security Patterns

Tenant isolation for Projects must be layered:

1. Authenticate JWTs at the API boundary and derive the trusted user ID from `sub`.
2. Validate the requested tenant against authenticated tenant claims/EventStore tenant validation.
3. Use a local Tenants projection to check tenant exists, is active, and contains the user at the required role.
4. Validate route/body/aggregate/projection/idempotency tenant bindings match before protected operations.
5. Apply Project-specific authorization: project belongs to tenant, caller may access project, project is active unless explicit archive/operator flow.
6. Verify linked resource authorization with Conversations/Folders/Memories before linking or including references in Project Context.
7. Log only bounded metadata: tenant ID, project ID, denial reason, correlation ID, projection freshness/status, never payloads or secrets.

Dapr access-control allowlists should restrict which app IDs can call Projects' internal endpoints (`/process`, projection routes, event subscriptions if exposed). Dapr mTLS should secure sidecar-to-sidecar traffic in hosted environments. ASP.NET Core policy/claims authorization remains useful for API gates, but it is not enough for project context assembly; row-level/result filtering is required because context leakage is the core risk in the PRD.

_OAuth 2.0 and JWT:_ Use existing EventStore JWT bearer/claims transformation model and `sub` as user identity.
_API Key Management:_ Not recommended for first-party service auth; use Dapr sidecar identity/access control and JWT for user-facing API calls.
_Mutual TLS:_ Use Dapr mTLS for service-to-service sidecar communication where deployed.
_Data Encryption:_ Keep payload storage out of Projects where owned by other contexts; transport security and Dapr component security remain platform concerns.
_Sources:_ ASP.NET Core policy authorization: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0; Dapr access control allowlist: https://docs.dapr.io/operations/configuration/invoke-allowlist/; Dapr mTLS: https://docs.dapr.io/operations/security/mtls/; local `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Infrastructure/Tenancy/FrontComposerTenantContextAccessor.cs`

## Architectural Patterns and Design

### System Architecture Patterns

The recommended architecture is a Hexalith.EventStore-backed bounded context for Projects, integrated with Tenants as an external source of tenant truth. Projects should not be a library-only feature inside Chatbot and should not be a generic CRUD service. It should be a module with:

- `Hexalith.Projects.Contracts` for commands, events, queries, DTOs, enums, and identifiers.
- `Hexalith.Projects.Server` for pure aggregates, state, projections, validators, and domain handlers.
- `Hexalith.Projects` host for ASP.NET Core API endpoints, EventStore command/query registration, Dapr actors, `/process`, projection dispatch, Tenants event subscription, authentication, and service defaults.
- `Hexalith.Projects.Client` for consuming-service helpers if Chatbot or sibling modules need reusable integration.
- `Hexalith.Projects.Aspire`, `AppHost`, `ServiceDefaults`, and `Testing` projects following the Tenants module shape.

The command side should be event-sourced: Project aggregates validate commands and emit Project domain events. The read side should use materialized projections optimized for Chatbot workflows: project summary list, project detail/context view, conversation-to-project index, folder/file/memory reference indexes, project resolution index, and audit/explanation views.

Tenants remains the system of record for tenant lifecycle, membership, role, and configuration. Projects maintains a local tenant access projection from Tenants events and uses it as authorization evidence. This mirrors Parties/Folders/Conversations and avoids synchronous coupling to Tenants for every request.

_Trade-off:_ Event sourcing + CQRS increases implementation complexity. The Azure Architecture Center explicitly calls out that CQRS plus Event Sourcing can be more complex and introduces eventual consistency. In this repo, the complexity is justified because Projects needs auditability, context selection traceability, rebuildable views, and strict separation between write intent and read-context assembly.
_Sources:_ Azure CQRS pattern: https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs; Azure Event Sourcing pattern: https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing; local `Hexalith.Tenants/_bmad-output/planning-artifacts/architecture.md`; local PRD sections 4-7

### Design Principles and Best Practices

Projects should apply these design principles:

- Bounded-context ownership: Projects owns only Project identity, setup, lifecycle, links, resolution metadata, and project audit. It does not own conversation transcripts, file contents, memory payloads, or tenant membership.
- Defense in depth: API authorization, tenant projection checks, project aggregate authorization, query-side filtering, and referenced-resource verification all remain active.
- Fail closed: missing, unknown, disabled, stale, malformed, rebuilding, unavailable, forbidden, or redacted tenant/resource evidence must deny trust-bearing operations.
- Pure domain logic: aggregate `Handle` and state/projection `Apply` logic should be testable without Dapr, ASP.NET Core, Redis, or network.
- Metadata-only diagnostics: context explanations and audit logs must describe inclusion/exclusion without exposing payloads, prompts, secrets, or personal content.
- Additive contracts: commands/events/query DTOs should remain serialization-tolerant, with no `V2` event fork unless explicitly approved.

The strongest local precedent is Conversations' projection freshness vocabulary: only `Current` enables trust-bearing decisions. Projects should adopt the same idea for context assembly. A linked Conversation/Folder/File/Memory reference can be listed as unavailable or excluded, but it should not be included in active Chatbot context unless the owning module's authorization and freshness evidence is trustworthy.

_Source:_ Local `Hexalith.Conversations/docs/projection-read-models.md`; local `_bmad-output/project-context.md`; Microsoft domain analysis for microservices: https://learn.microsoft.com/en-us/azure/architecture/microservices/model/domain-analysis

### Scalability and Performance Patterns

The PRD's p95 target for list/open/resolution/context retrieval is under 500 ms when dependent bounded-context metadata is available. The architecture should favor precomputed, tenant-scoped materialized views rather than assembling everything from remote calls on each request.

Recommended scalability patterns:

- Tenant partitioning in identity and keys: use the canonical `{tenant}:{domain}:{aggregateId}` identity model and include tenant ID in projection keys, indexes, cache scopes, SignalR groups, and logs.
- Materialized project context summaries: maintain read models for common Chatbot operations instead of replaying aggregate streams or calling multiple bounded contexts at request time.
- Cursor-based pagination: keep Tenants-style cursor paging for list/search operations and reject invalid cursors with Problem Details.
- Projection rebuild/replay: project views are rebuildable from Project events and Tenants events; expose freshness and rebuild status rather than returning partial trust-bearing data.
- Durable production projection store: do not rely on default in-memory tenant projections for production. Parties documents that in-memory projections start empty after restart and will deny every tenant until events replay.
- Dapr resiliency boundaries: apply timeouts, retries, and circuit breakers to service invocation targets, not inside domain handlers.

For expensive or failure-prone context assembly, use a bulkhead-style separation between interactive Chatbot reads and background repair/rebuild/reconciliation work. Microsoft/Azure design pattern guidance treats circuit breakers, retries, materialized views, and event sourcing as separate tools that should be combined intentionally.

_Source:_ Azure CQRS pattern: https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs; Azure Event Sourcing pattern: https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing; Azure Circuit Breaker pattern: https://learn.microsoft.com/en-us/azure/architecture/patterns/circuit-breaker; local `Hexalith.Parties/docs/tenant-access-projection.md`

### Integration and Communication Patterns

The target architecture uses four integration modes:

1. EventStore command pipeline for Project writes.
2. EventStore query/projection pipeline for Project reads.
3. Dapr pub/sub for Tenants events and optional metadata events from Conversations/Folders/Memories.
4. Dapr service invocation for bounded-context verification during link/context operations.

Projects' own aggregate actor identity should be tenant-scoped. Unlike Tenants, which manages user-facing tenants under the platform `system` tenant, Projects contains tenant-owned workspace data. Therefore Project commands should use the actual user-facing tenant as the EventStore envelope tenant, with a Projects domain such as `projects` and aggregate ID equal to the Project ID. Tenant management commands still target Tenants under `system:tenants:{managedTenantId}`.

This distinction matters for isolation: Project state keys, Project aggregate streams, Project projections, and Project query scopes should be physically/logically partitioned by the user-facing tenant. Tenants events may still arrive from `system.tenants.events`, but the managed tenant ID is taken from the event payload/context and projected into Projects' local access store.

_Source:_ Dapr service invocation overview: https://docs.dapr.io/developing-applications/building-blocks/service-invocation/service-invocation-overview/; Dapr actors overview: https://docs.dapr.io/developing-applications/building-blocks/actors/actors-overview/; local `Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Identity/TenantIdentity.cs`; local Tenants architecture identity mapping

### Security Architecture Patterns

Projects' primary security risk is context leakage: returning a Conversation, Folder/File, Memory, or Project reference across tenant, project, or user boundaries. OWASP API Security Top 10 identifies broken object-level authorization as the top API risk and calls out object identifiers in endpoints as a broad attack surface. Projects has many object identifiers by design, so every endpoint that accepts `tenantId`, `projectId`, `conversationId`, `folderId`, `fileId`, or `memoryId` needs object-level authorization.

Recommended security architecture:

- Trusted tenant context is derived from authentication/EventStore claims, not raw route/body input.
- Route tenant, body tenant, aggregate tenant, projection tenant, idempotency tenant, and linked-resource tenant must match where relevant.
- Tenant access checks return structured denial reasons rather than leaking whether a resource exists in another tenant.
- Project Context assembly uses allowlist inclusion: a reference is included only after tenant, project, lifecycle, authorization, and freshness checks pass.
- Operator/admin reads are metadata-only and tenant-scoped.
- Dapr access-control policies restrict app IDs and internal endpoints such as `/process`, projection dispatch, and event subscriptions.
- Logs include correlation IDs and reason codes, not prompts, transcripts, file paths, memory payloads, command bodies, raw tokens, or secrets.

Projects should model denial classes explicitly, similar to Conversations and Folders, so UI/API/MCP surfaces can translate denials without branching on exception types.

_Source:_ OWASP API Security Top 10 2023: https://owasp.org/API-Security/editions/2023/en/0x11-t10/; Dapr access control allowlist: https://docs.dapr.io/operations/configuration/invoke-allowlist/; local `Hexalith.Folders/src/Hexalith.Folders/Authorization/TenantAccessAuthorizer.cs`; local `Hexalith.Conversations/src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessGuard.cs`

### Data Architecture Patterns

Projects needs three categories of data:

1. Authoritative Project event streams: Project lifecycle, setup, link/unlink, resolution confirmation, archive, and metadata-only audit events.
2. Project read models: tenant-scoped summaries, details, context views, resolution indexes, and audit/explanation projections.
3. External evidence projections: Tenants membership/status and possibly resource lifecycle/freshness metadata from Conversations/Folders/Memories.

Do not merge these categories. Project event streams should not contain copied tenant membership maps, conversation transcripts, file contents, or memory bodies. External evidence should be rebuildable and replaceable. Context read models should store only enough metadata to answer Chatbot's workflow quickly and safely.

Recommended aggregate boundaries:

- `ProjectAggregate`: lifecycle, name/description, setup, archive state, and primary project folder reference.
- `ProjectReferences` within the Project aggregate or a closely related aggregate if reference cardinality remains modest in v1. If references grow materially, split into reference-specific aggregates/indexes later.
- Avoid a cross-tenant aggregate. Cross-tenant indexes are projections only and must never bypass per-tenant authorization.

Recommended projection boundaries:

- `ProjectSummaryProjection` per tenant for list/open.
- `ProjectContextProjection` per project for context assembly metadata.
- `ProjectResolutionProjection` per tenant for conversation/folder/file/memory candidate lookup.
- `ProjectAuditProjection` per project/tenant for metadata-only audit.
- `TenantAccessProjection` local to Projects, fed from Tenants events.

_Source:_ Azure Materialized View/CQRS discussion in CQRS docs: https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs; Azure Event Sourcing pattern: https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing; local PRD section 4; local Tenants architecture projection design

### Deployment and Operations Architecture

Deployment should mirror the Tenants module:

- Aspire AppHost wires Projects, EventStore, Dapr components, Redis/state store, pub/sub, access-control config, service defaults, and dependent module endpoints.
- Dapr sidecars provide service invocation, pub/sub, actors, state, resiliency, and mTLS/access-control where deployed.
- Health checks should distinguish API liveness, Dapr sidecar health, state store readiness, pub/sub subscription health, tenant projection freshness, and referenced-context availability.
- Operational runbooks need projection replay/rebuild procedures, tenant event subscription diagnostics, dead-letter monitoring, and stale-context handling.
- Production should configure durable projection/deduplication stores; in-memory defaults are acceptable for tests/local development only.

Dapr's actor model is appropriate for EventStore aggregate/projection actors because actors have identity and state stored in a configured provider. Aspire service defaults should be used for consistent health, telemetry, and service discovery. Dapr dead-letter topics and resiliency policies should be part of the AppHost/configuration baseline for Projects event subscriptions.

_Source:_ Dapr actors overview: https://docs.dapr.io/developing-applications/building-blocks/actors/actors-overview/; Dapr pub/sub dead-letter topics: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-deadletter/; Microsoft Aspire service defaults: https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/service-defaults; local `Hexalith.Tenants/src/Hexalith.Tenants.Aspire/HexalithTenantsExtensions.cs`; local `Hexalith.Parties/docs/tenant-access-projection.md`

## Implementation Approaches and Technology Adoption

### Technology Adoption Strategies

Use a staged adoption path that mirrors Tenants rather than a big-bang module build. Dapr's overview explicitly supports incremental adoption of cloud-native patterns, and the local repo already has a proven Tenants module template. Projects should first establish a narrow, testable domain core, then add runtime infrastructure only after the contracts and authorization model are stable.

Recommended adoption sequence:

1. Scaffold `Hexalith.Projects` as a module following `Hexalith.Tenants` project layout: Contracts, Server, host, Client, Aspire, ServiceDefaults, Testing, tests, samples if needed.
2. Define Projects contracts for Project lifecycle, setup, references, resolution, context, audit, and rejections.
3. Implement pure aggregates and state with Tier 1 tests before adding Dapr/Aspire.
4. Add Project projections and query contracts for list/open/context/resolution.
5. Add local Tenants event subscription and tenant access projection using `Hexalith.Tenants.Client`.
6. Add host wiring: EventStore registration, MediatR, auth, `/process`, projection dispatch, query controllers, Dapr actors, CloudEvents, subscribe handler.
7. Add AppHost topology and Dapr access-control/resiliency/dead-letter configuration.
8. Add integration tests for command path, query path, tenant event consumption, projection rebuild, and cross-tenant denial.
9. Add production readiness gates: durable projection store, durable/eventual deduplication, tenant event replay/rebuild procedure, mTLS/access-control, observability dashboards.

_Source:_ Dapr overview: https://docs.dapr.io/concepts/overview/; local `Hexalith.Tenants/README.md`; local `Hexalith.Tenants/_bmad-output/planning-artifacts/architecture.md`

### Development Workflows and Tooling

Projects implementation should follow the repo's module-local workflow:

- Use `.slnx` and project-local `dotnet build` / `dotnet test` commands.
- Keep package versions in `Directory.Packages.props`.
- Use nullable, warnings-as-errors, file-scoped namespaces, and existing `.editorconfig` conventions.
- Keep commands/events/rejections in `Contracts`; aggregates/projections/validators in `Server`; host orchestration in `Hexalith.Projects`; reusable test fakes in `Testing`.
- Use conventional commits/semantic-release conventions if the module is intended for NuGet packages.
- Do not initialize nested submodules recursively.

Microsoft's DevOps documentation frames CI as automated merge/test feedback and CD as producing deployable artifacts across environments. For Projects, CI should have separate lanes: fast contracts/server tests, host/API integration tests, Dapr/Aspire integration tests, and any UI/E2E tests if Chatbot-facing UI is added later.

_Source:_ Azure DevOps overview: https://learn.microsoft.com/en-us/devops/what-is-devops; local `_bmad-output/project-context.md`; local `Hexalith.Tenants/tests/Directory.Build.props`

### Testing and Quality Assurance

Testing should be risk-driven and tenant-isolation-heavy.

Tier 1 unit tests:

- Contract naming, serialization, additive-tolerant DTO behavior.
- Aggregate `Handle` and state/projection `Apply` behavior.
- Project authorization decisions: missing tenant, tenant mismatch, disabled tenant, missing membership, insufficient role, archived project, wrong project tenant.
- Context inclusion/exclusion matrix for Conversations/Folders/Files/Memories.
- Resolution ambiguity: no match, single candidate, multiple candidates, archived exclusion, unauthorized resource exclusion.
- Cursor codec/pagination and invalid cursor Problem Details.

Tier 2 host/integration tests:

- `WebApplicationFactory`-style API tests for REST/query behavior where infrastructure can be faked.
- Tenants event processing through `TenantEventProcessor` and local projection store.
- Query-side row filtering and object-level authorization.
- Projection freshness and rebuild/unavailable behavior.
- Problem Details reason codes and correlation IDs.

Tier 3 Dapr/Aspire tests:

- Command submission through EventStore into Projects `/process`.
- Dapr pub/sub subscription to Tenants events.
- Dapr access-control policy allows intended app IDs and denies unintended paths.
- State store/projection persistence across restart.
- Dead-letter and retry behavior for poison tenant events.

Microsoft's unit testing guidance supports fast, isolated tests for small units, while ASP.NET Core integration testing docs cover app-hosted tests with `WebApplicationFactory`. The local Tenants test suite is the better concrete model: it includes contract tests, server tests, client tests, testing fakes, and Dapr/Aspire integration tests.

_Source:_ .NET unit testing best practices: https://learn.microsoft.com/dotnet/core/testing/unit-testing-best-practices; ASP.NET Core integration tests: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests; local `Hexalith.Tenants/tests`; local `Hexalith.Tenants/tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`

### Deployment and Operations Practices

Operations should be designed before the first production deployment, not deferred behind "it works locally." The Azure Well-Architected Framework emphasizes reliability, security, cost optimization, operational excellence, and performance efficiency. Dapr production guidance adds concrete platform requirements for mTLS, app-to-Dapr authentication, Dapr-to-app authentication, secret stores, component scoping, control plane namespace isolation, placement high availability for actors, and resource baselining.

Projects operational checklist:

- Health endpoints for API readiness, EventStore reachability, Dapr sidecar, state store, pub/sub, Tenants event subscription, and projection freshness.
- OpenTelemetry traces/metrics/logs through ServiceDefaults and Dapr observability.
- Structured logs with tenant/project/correlation/reason metadata only.
- Dapr resiliency policies for EventStore, Tenants, Conversations, Folders, Memories, state store, pub/sub, and actor calls.
- Dead-letter topic and replay/repair procedure for Projects and Tenants event subscriptions.
- Durable tenant access projection and deduplication store for production.
- Projection rebuild/replay operations with "not Current means not trust-bearing" behavior.
- Runbooks for disabled tenant propagation lag, unknown tenant after restart, stale projection, poison event, access-control denial, and command pipeline failure.

_Source:_ Azure Well-Architected Framework: https://learn.microsoft.com/en-us/azure/well-architected/; Dapr production Kubernetes guidelines: https://docs.dapr.io/operations/hosting/kubernetes/kubernetes-production/; Dapr observability: https://docs.dapr.io/concepts/observability-concept/; Dapr resiliency: https://docs.dapr.io/operations/resiliency/resiliency-overview/

### Team Organization and Skills

Implementation needs a small cross-functional slice, not only backend coding:

- Domain/EventStore engineer: contracts, aggregates, command/query conventions, projections.
- Security/authorization engineer: tenant/project/resource authorization matrix, denial semantics, OWASP object-level authorization checks.
- Distributed systems/Dapr engineer: AppHost topology, sidecar config, access control, resiliency, dead letters, durable projections.
- Test engineer: Tier 1 isolation matrix, integration tests, Dapr/Aspire tests, replay/rebuild scenarios.
- Chatbot/product integration engineer: Project Context DTOs, resolution UX semantics, explanation metadata, no-payload guarantees.
- Operations owner: dashboards, runbooks, alerts, projection rebuild/replay procedures.

The first stories should be written so most domain behavior can be implemented and reviewed without starting Dapr. This keeps feedback fast and reduces the risk that infrastructure complexity hides authorization bugs.

_Source:_ Local Tenants architecture "Tier 1 testability as architectural constraint"; Azure Well-Architected operational excellence: https://learn.microsoft.com/en-us/azure/well-architected/pillars

### Cost Optimization and Resource Management

Primary cost risks are not raw compute; they are avoidable cross-service calls, unbounded projection/deduplication state, and inefficient context assembly.

Cost controls:

- Precompute Project read models and resolution indexes instead of composing every request from multiple remote services.
- Store references and metadata only; do not duplicate transcripts, files, memories, prompts, or embeddings.
- Use bounded TTL/external deduplication for event message IDs.
- Use cursor paging and page-size clamps.
- Use background rebuild/reconciliation lanes rather than repeated synchronous retries from user requests.
- Baseline Dapr sidecar CPU/memory in production; Dapr docs note sidecars perform I/O-heavy work and need tuned resources.
- Avoid premature GraphQL, per-tenant databases, or custom brokers until the concrete Projects workload demands them.

_Source:_ Dapr Kubernetes production resource guidance: https://docs.dapr.io/operations/hosting/kubernetes/kubernetes-production/; Azure Well-Architected cost optimization: https://learn.microsoft.com/en-us/azure/well-architected/

### Risk Assessment and Mitigation

Key risks and mitigations:

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Tenant context leakage in Project Context | Critical security failure | Object-level authorization on every identifier; allowlist inclusion; deny hidden/forbidden resources without revealing existence |
| In-memory tenant projection after restart | All tenants deny or stale decisions | Durable `ITenantProjectionStore`, replay/rebuild procedure, readiness gate until projection is usable |
| At-least-once/out-of-order tenant event delivery | Incorrect membership/status | Idempotent handlers, sequence/freshness metadata where needed, rebuild path, dead-letter poison events |
| Synchronous dependency cascade during context assembly | Latency and availability failures | Materialized views, bounded-context evidence, Dapr timeouts/circuit breakers, stale/unavailable exclusion |
| Commands use wrong tenant identity model | Cross-tenant state pollution | Project commands use user-facing tenant as EventStore tenant; Tenants commands use `system` only |
| Query filtering assumed covered by API auth | Unauthorized list/context rows | Query-side filtering tests for list/open/resolve/context endpoints |
| Diagnostics leak sensitive payloads | Privacy/security breach | Metadata-only log/audit DTOs, test assertions against payload fields |
| Implementation starts with infrastructure before domain | Slow feedback and hidden bugs | Contracts and pure aggregate/projection tests first |

_Source:_ OWASP API Security Top 10 2023: https://owasp.org/API-Security/editions/2023/en/0x11-t10/; Dapr pub/sub overview: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/; local `Hexalith.Parties/docs/tenant-access-projection.md`

## Technical Research Recommendations

### Implementation Roadmap

Recommended MVP story sequence:

1. Scaffold Projects solution structure from Tenants.
2. Add Project contract naming/serialization tests.
3. Implement Project aggregate lifecycle: create, update setup, archive.
4. Implement Project reference commands: link/move conversation, set folder, link/unlink files and memories.
5. Implement Project read models: summary, detail, context, audit.
6. Implement tenant access projection using Tenants client, with durable-store interface from the start.
7. Implement tenant/project access service and denial reason model.
8. Implement query endpoints for list/open/context/resolve/explain/refresh.
9. Implement context assembly with referenced-resource authorization evidence.
10. Wire host with EventStore, Dapr actors, `/process`, projection dispatch, auth, Problem Details, telemetry.
11. Wire Aspire/Dapr topology, access-control, resiliency, pub/sub, dead letters.
12. Add integration and conformance tests for cross-tenant isolation, projection freshness, restart behavior, and poison events.

### Technology Stack Recommendations

- Use C#/.NET 10, ASP.NET Core, MediatR, Hexalith.EventStore, Dapr, Aspire, System.Text.Json.
- Use `Hexalith.Tenants.Contracts` and `Hexalith.Tenants.Client`; do not reference Tenants host internals from Projects.
- Use xUnit v3, Shouldly, NSubstitute, Testcontainers/Aspire testing where infrastructure is required.
- Use OpenTelemetry via ServiceDefaults and Dapr observability.
- Use Dapr state/pubsub/actors/service invocation and avoid direct infrastructure clients in domain/server packages.

### Skill Development Requirements

The team needs working knowledge of:

- Hexalith.EventStore command lifecycle, identity scheme, envelopes, projections, and query routing.
- Dapr sidecars, pub/sub, CloudEvents, state stores, actors, access control, resiliency, and production security.
- ASP.NET Core authentication/authorization and object-level authorization.
- Event sourcing/CQRS trade-offs, projection freshness, replay/rebuild, idempotent event handling.
- Tenant isolation test design and negative-path security testing.

### Success Metrics and KPIs

Implementation success should be measured by:

- 100% coverage of tenant/project/resource authorization decision matrix in fast tests.
- No Project Context includes unauthorized, stale, forbidden, redacted, archived, or cross-tenant references.
- List/open/resolve/context p95 under 500 ms when local metadata is current.
- Tenant disabled/user removed propagation is observable and bounded by documented projection lag.
- Projection restart/rebuild procedure restores service without manual data repair.
- Dapr access-control tests prove denied app IDs cannot call internal endpoints.
- Logs/audits pass payload-leak checks.
- Command/query contracts remain additive and serialization-tolerant.

# Tenant-Aware Project Workspaces: Comprehensive Hexalith.Tenants in Hexalith.Projects Technical Research

## Executive Summary

`Hexalith.Projects` should not embed tenant management logic or call `Hexalith.Tenants` internals. The correct architecture is a dedicated Projects bounded context that treats `Hexalith.Tenants` as the tenant system of record, consumes Tenants contracts and events, and maintains local tenant access projections for fast, fail-closed authorization. Projects then applies its own project-domain authorization, query-side filtering, context assembly rules, and metadata-only diagnostics.

The key design distinction is identity ownership. `Hexalith.Tenants` manages user-facing tenants under the platform `system` tenant because tenant management is platform-level. `Hexalith.Projects` stores tenant-owned workspace data, so Project command streams, state keys, projections, query scopes, and context views should be partitioned by the actual user-facing tenant. Tenant lifecycle and membership facts enter Projects as projected integration evidence, not copied authoritative state.

The implementation should follow the established Hexalith module pattern already proven in Tenants: Contracts, Server, host, Client, Aspire, ServiceDefaults, Testing, and module-specific tests. Start with pure contracts and aggregate/projection tests, then add query/read models, local Tenants event projection, host wiring, Aspire/Dapr topology, and production hardening.

**Key Technical Findings:**

- Projects should be a Hexalith.EventStore/Dapr bounded context, not an in-process Chatbot feature.
- Tenants remains the source of truth for tenant lifecycle, membership, roles, status, and configuration.
- Projects should consume Tenants through `Hexalith.Tenants.Contracts`, `Hexalith.Tenants.Client`, Dapr pub/sub, and local projections.
- Project Context assembly must be allowlist-based and fail closed unless tenant, project, referenced-resource authorization, lifecycle, and freshness evidence are trustworthy.
- Production requires durable tenant projections, bounded/durable event deduplication, replay/rebuild runbooks, Dapr access control, resiliency policies, and dead-letter handling.

**Technical Recommendations:**

- Scaffold Projects from the Tenants module shape and reuse EventStore command/query/projection conventions.
- Use the actual user-facing tenant as the EventStore tenant for Project data; reserve `system:tenants:*` for Tenants management commands.
- Implement a Projects tenant access service backed by Tenants events, with structured denial reasons.
- Keep Project events authoritative only for Project data; do not copy transcripts, file contents, memory payloads, secrets, prompts, or unrestricted paths.
- Test tenant/project/resource isolation as a first-class acceptance surface, not as incidental API coverage.

## Table of Contents

1. Technical Research Introduction and Methodology
2. Technical Landscape and Architecture Analysis
3. Implementation Approaches and Best Practices
4. Technology Stack Evolution and Current Trends
5. Integration and Interoperability Patterns
6. Performance and Scalability Analysis
7. Security and Compliance Considerations
8. Strategic Technical Recommendations
9. Implementation Roadmap and Risk Assessment
10. Future Technical Outlook and Innovation Opportunities
11. Technical Research Methodology and Source Verification
12. Technical Appendices and Reference Materials

## 1. Technical Research Introduction and Methodology

### Technical Research Significance

Projects is the boundary that tells Chatbot which conversations, folders, files, memories, and setup metadata may be used together. That makes tenant isolation a correctness and security requirement, not just an access-control feature. A single context assembly mistake can leak unrelated tenant or project information into an AI prompt.

The current architecture must therefore centralize trust-bearing checks server-side: tenant identity, project identity, resource authorization, projection freshness, and metadata minimization all need to be enforced before Chatbot receives context. This is aligned with OWASP's warning that object identifiers in APIs create broad object-level authorization attack surfaces, and with current multi-tenant agent security research emphasizing server-side authorization and state isolation.

_Sources:_ OWASP API Security Top 10 2023: https://owasp.org/API-Security/editions/2023/en/0x11-t10/; arXiv "Securing the Agent": https://arxiv.org/abs/2605.05287

### Technical Research Methodology

- **Technical Scope:** Technology stack, integration patterns, architecture, implementation, operations, security, and risk.
- **Data Sources:** Local Hexalith code/docs/planning artifacts plus current official Microsoft, Dapr, CloudEvents, and OWASP sources.
- **Analysis Framework:** Compare local module patterns against current distributed-system/security guidance, then derive Projects-specific implementation decisions.
- **Time Period:** Research completed on 2026-05-24 with current web verification.
- **Technical Depth:** Implementation-level recommendations suitable for architecture, story creation, and engineering planning.

### Technical Research Goals and Objectives

**Original Technical Goal:** Study how to use `Hexalith.Tenants` in the `Hexalith.Projects` module to implement tenant management and isolation.

**Achieved Objectives:**

- Defined the correct ownership boundary between Tenants and Projects.
- Identified the EventStore tenant identity model Projects should use.
- Mapped Tenants event consumption into local Projects authorization projections.
- Produced a staged implementation roadmap and risk matrix.
- Captured production-readiness requirements that affect architecture, not only deployment.

## 2. Technical Landscape and Architecture Analysis

### Current Technical Architecture Patterns

The dominant local pattern is EventStore + Dapr + Aspire:

- EventStore command streams are the authoritative write model.
- Projection/read models serve query and workflow needs.
- Dapr provides sidecar service invocation, actors, state, pub/sub, resiliency, and access control.
- Aspire composes local topology and service defaults.
- Tenant isolation is repeated at API, command, aggregate, projection, query, and logging boundaries.

Azure Architecture Center guidance supports using Event Sourcing with CQRS/materialized views when auditability, rebuildable read models, and query optimization justify the complexity. Projects meets that threshold because context assembly requires explainability, isolation, and derived views across linked references.

_Sources:_ Azure Event Sourcing: https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing; Azure CQRS: https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs; local `Hexalith.Tenants/_bmad-output/planning-artifacts/architecture.md`

### System Design Principles and Best Practices

Projects should use bounded-context ownership:

- Tenants owns tenant lifecycle and membership.
- Projects owns project identity, lifecycle, setup, context links, resolution, and project audit.
- Conversations, Folders, and Memories own their payloads and authorization boundaries.
- Projects stores references and evidence, not owned payloads.

Only current, authorized, tenant-matched evidence should feed trust-bearing context assembly. Conversations' projection guidance is directly relevant: unknown, stale, rebuilding, unavailable, forbidden, redacted, contradictory, out-of-order, or poison evidence must not be treated as current truth.

_Sources:_ Local `Hexalith.Conversations/docs/projection-read-models.md`; local Projects PRD

## 3. Implementation Approaches and Best Practices

### Current Implementation Methodologies

The safest implementation path is incremental:

1. Scaffold module structure from Tenants.
2. Add contracts and serialization/naming tests.
3. Implement pure aggregates and state tests.
4. Implement read models and query contracts.
5. Add local Tenants event projection.
6. Add authorization/context assembly services.
7. Wire ASP.NET Core/EventStore/Dapr host.
8. Add Aspire topology and Dapr config.
9. Add integration, conformance, restart, replay, and poison-event tests.

This keeps the security-critical domain behavior testable without Dapr or network infrastructure, then validates the runtime paths later.

_Sources:_ Local `Hexalith.Tenants/tests`; local `Hexalith.Tenants/src`; ASP.NET Core integration tests: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests

### Implementation Framework and Tooling

Use the existing stack:

- C#/.NET 10
- ASP.NET Core
- MediatR
- Hexalith.EventStore
- Dapr SDK/runtime
- .NET Aspire
- System.Text.Json
- xUnit v3, Shouldly, NSubstitute
- Testcontainers/Aspire testing where real infrastructure is needed

_Sources:_ Microsoft target frameworks: https://learn.microsoft.com/en-us/dotnet/standard/frameworks; Dapr overview: https://docs.dapr.io/concepts/overview/; Aspire service defaults: https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/service-defaults

## 4. Technology Stack Evolution and Current Trends

### Current Technology Stack Landscape

The Hexalith workspace is already standardized around .NET 10, Dapr, Aspire, EventStore, and module-local package boundaries. Projects should follow that platform instead of introducing new infrastructure. Dapr's sidecar architecture and building blocks let Projects avoid direct database/broker clients while still using state, pub/sub, actors, service invocation, resiliency, and observability.

_Sources:_ Dapr overview: https://docs.dapr.io/concepts/overview/; Dapr API reference: https://docs.dapr.io/reference/api/

### Technology Adoption Patterns

Adopt Tenants integration through `Hexalith.Tenants.Client` and `Hexalith.Tenants.Contracts`, not by copying Tenants code or reaching into the Tenants host. This supports package boundaries, testability, and future module release independence.

_Sources:_ Local `Hexalith.Tenants/src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs`; local `Hexalith.Tenants/docs/idempotent-event-processing.md`

## 5. Integration and Interoperability Patterns

### Current Integration Approaches

Projects should use four integration modes:

- EventStore command pipeline for Project writes.
- EventStore query/projection pipeline for Project reads.
- Dapr pub/sub for Tenants events.
- Dapr service invocation for bounded-context verification.

Dapr pub/sub is at-least-once, so event handlers must be idempotent and backed by durable/bounded deduplication in production. Dead-letter topics and retries are configuration requirements, not optional polish.

_Sources:_ Dapr service invocation: https://docs.dapr.io/developing-applications/building-blocks/service-invocation/service-invocation-overview/; Dapr pub/sub overview: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/; Dapr dead letters: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-deadletter/

### Interoperability Standards and Protocols

Use JSON contracts and EventStore envelopes. Tenants events are published through Dapr/CloudEvents, and Projects should consume the existing event envelope and payload contracts rather than defining a second tenant event protocol.

_Sources:_ Dapr CloudEvents: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-cloudevents/; CloudEvents: https://cloudevents.io/

## 6. Performance and Scalability Analysis

### Performance Characteristics and Optimization

The Projects PRD sets an internal target of p95 under 500 ms for list, open, resolution, and context retrieval when dependent metadata is available. Achieve this through materialized views and local projections, not repeated remote composition. Remote verification should happen at link/update boundaries and during refresh/rebuild flows; interactive reads should prefer current projected evidence.

### Scalability Patterns and Approaches

Use tenant-partitioned keys, cursor pagination, page-size clamps, projection freshness metadata, and background rebuild/reconciliation. The default in-memory Tenants projection is acceptable for tests/local development but insufficient for production restarts or scaled replicas.

_Sources:_ Azure CQRS: https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs; local `Hexalith.Parties/docs/tenant-access-projection.md`

## 7. Security and Compliance Considerations

### Security Best Practices and Frameworks

The central security pattern is object-level authorization on every user-supplied identifier. Projects endpoints and commands will handle `tenantId`, `projectId`, `conversationId`, `folderId`, `fileId`, and `memoryId`; each must be authorized against trusted tenant/user context and hidden on denial when disclosure would leak existence.

Security controls:

- Derive user identity from `sub`, not display claims.
- Compare route/body/aggregate/projection/idempotency tenant bindings.
- Deny missing, unknown, disabled, stale, unavailable, redacted, forbidden, archived, or cross-tenant evidence.
- Use query-side filtering for list/resolve/context endpoints.
- Keep logs/audits metadata-only.
- Restrict internal Dapr endpoints with access-control policies.
- Use mTLS and Dapr API authentication in production.

_Sources:_ OWASP API Security Top 10: https://owasp.org/API-Security/editions/2023/en/0x11-t10/; Dapr security: https://docs.dapr.io/concepts/security-concept/; Dapr access control: https://docs.dapr.io/operations/configuration/invoke-allowlist/

### Compliance and Governance Considerations

Projects should make auditability explicit: metadata-only Project events and audit projections should record actor, tenant, project, operation, timestamp, affected reference identifiers, and reason codes. They must not store payloads from Conversations, Folders, Memories, or prompts.

## 8. Strategic Technical Recommendations

### Technical Strategy and Decision Framework

Recommended target state:

- Dedicated `Hexalith.Projects` module.
- Project streams use actual user-facing tenant context.
- Tenants facts arrive through projected integration state.
- Project Context assembly is a guarded read model, not an open composition query.
- Production readiness includes durable projections, replay/rebuild, deduplication, Dapr security, and observability.

### Competitive Technical Advantage

The differentiator is not raw project CRUD. It is a tenant-aware AI workspace boundary that can explain and enforce why a piece of context was included or excluded. That gives Chatbot safer continuity across conversations, files, folders, and memories without mixing tenant/project boundaries.

## 9. Implementation Roadmap and Risk Assessment

### Technical Implementation Framework

Recommended implementation phases:

1. **Foundation:** Scaffold solution, contracts, naming/serialization tests.
2. **Domain:** Project aggregate, setup, archive, reference commands, pure tests.
3. **Read Side:** Summary/detail/context/resolution/audit projections and query contracts.
4. **Tenants Integration:** Local Tenants event projection and access service.
5. **Context Assembly:** Referenced-resource authorization evidence and inclusion/exclusion reasons.
6. **Host Runtime:** EventStore, ASP.NET Core, auth, Dapr actors, `/process`, query controllers.
7. **Topology:** Aspire, Dapr state/pubsub/access-control/resiliency/dead letters.
8. **Production Gates:** Durable projections, replay/rebuild, deduplication, observability, runbooks.

### Technical Risk Management

| Risk | Mitigation |
| --- | --- |
| Cross-tenant context leak | Object-level authorization, allowlist inclusion, query filtering, negative-path tests |
| Stale/missing tenant projection | Durable store, readiness checks, replay/rebuild, fail closed |
| At-least-once duplicate event handling | Message ID deduplication plus idempotent handlers |
| Out-of-order or poison events | Freshness states, dead letters, rebuild path |
| Synchronous dependency cascade | Materialized views, Dapr timeouts/circuit breakers, bounded refresh |
| Payload leakage in logs/audit | Metadata-only DTOs and leak tests |
| Wrong EventStore tenant identity | Explicit Project identity tests and command envelope validation |

## 10. Future Technical Outlook and Innovation Opportunities

### Emerging Technology Trends

Near-term work should focus on production-grade projections and tenant-safe context assembly. Medium-term opportunities include richer project resolution scoring, replayable context selection explanations, and operator dashboards for projection freshness. Long-term opportunities include policy-driven context assembly and tenant-aware AI retrieval/tool orchestration, but only after the baseline tenant isolation model is proven.

### Innovation and Research Opportunities

Promising areas:

- A shared Hexalith tenant-access conformance suite for consuming modules.
- Standard projection freshness contracts across Projects, Conversations, Folders, and Memories.
- Cross-module context evidence DTOs that remain metadata-only.
- Automated payload-leak tests for logs/audit/context responses.

## 11. Technical Research Methodology and Source Verification

### Comprehensive Technical Source Documentation

Primary local sources:

- `_bmad-output/project-context.md`
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md`
- `Hexalith.Tenants/_bmad-output/project-context.md`
- `Hexalith.Tenants/_bmad-output/planning-artifacts/architecture.md`
- `Hexalith.Tenants/README.md`
- `Hexalith.Tenants/docs/quickstart.md`
- `Hexalith.Tenants/docs/event-contract-reference.md`
- `Hexalith.Tenants/docs/idempotent-event-processing.md`
- `Hexalith.Parties/docs/tenant-access-projection.md`
- `Hexalith.Conversations/docs/projection-read-models.md`

Primary external sources:

- Microsoft .NET, ASP.NET Core, Aspire, Azure Architecture Center, Azure Well-Architected, Azure DevOps docs.
- Dapr official docs for service invocation, pub/sub, CloudEvents, state, actors, resiliency, observability, security, access control, and production Kubernetes guidance.
- CloudEvents official specification site.
- OWASP API Security Top 10.

### Technical Research Quality Assurance

Confidence is high for architecture and implementation recommendations because they are supported by both local code patterns and official/current platform documentation. The main limitation is that a dedicated `Hexalith.Projects` service/solution does not yet exist in the checkout, so recommendations define the first implementation surface rather than modifying existing Projects code.

## 12. Technical Appendices and Reference Materials

### Architectural Pattern Summary

| Area | Recommendation |
| --- | --- |
| Project writes | EventStore command streams under user-facing tenant |
| Tenant source of truth | `Hexalith.Tenants` |
| Tenant access in Projects | Local projection from Tenants events |
| Project reads | Materialized projections/query actors |
| Context assembly | Allowlist inclusion with trust-bearing evidence |
| Runtime integration | Dapr service invocation and pub/sub |
| Local topology | Aspire AppHost and ServiceDefaults |
| Production hardening | Durable projections, deduplication, resiliency, access control, observability |

### Technical Resources and References

- .NET target frameworks: https://learn.microsoft.com/en-us/dotnet/standard/frameworks
- ASP.NET Core authorization policies: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies
- Aspire service defaults: https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/service-defaults
- Azure Event Sourcing: https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing
- Azure CQRS: https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs
- Dapr docs: https://docs.dapr.io/
- CloudEvents: https://cloudevents.io/
- OWASP API Security: https://owasp.org/API-Security/

---

## Technical Research Conclusion

### Summary of Key Technical Findings

`Hexalith.Projects` should use `Hexalith.Tenants` by consuming tenant contracts/events and projecting local tenant access state. It should not duplicate tenant management, bypass Tenants through direct infrastructure access, or assume API authentication alone protects project/context reads. The target architecture is an EventStore/Dapr bounded context with materialized views, fail-closed authorization, and metadata-only context assembly.

### Strategic Technical Impact Assessment

This design keeps tenant management centralized while allowing Projects to make fast, local, auditable decisions. It also aligns Projects with existing Hexalith modules, lowers implementation risk by reusing proven patterns, and directly addresses the highest-risk failure mode: cross-tenant or cross-project context leakage into Chatbot.

### Next Steps Technical Recommendations

1. Create a Projects architecture document from this research.
2. Create epics/stories starting with scaffold, contracts, aggregate, projections, and tenant access projection.
3. Add an explicit tenant/project/resource authorization matrix before implementation.
4. Build Tier 1 tests first; defer Dapr/Aspire runtime until domain behavior is stable.
5. Treat durable projection/deduplication and Dapr security config as production-readiness blockers.

**Technical Research Completion Date:** 2026-05-24  
**Research Period:** Current comprehensive technical analysis  
**Source Verification:** Local code/planning artifacts plus current official external sources  
**Technical Confidence Level:** High for architecture and implementation direction; medium for future optimization details pending actual Projects service implementation

_This technical research document is the authoritative reference for using `Hexalith.Tenants` in `Hexalith.Projects` to implement tenant management integration and tenant isolation._
