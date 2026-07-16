# Epic 6 Context: Supported Platform Boundary and Secure Identity

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Migrate Hexalith.Projects to the supported EventStore DomainService and platform-owned hosting model, restore dependency-light contract and presentation boundaries, enforce authenticated caller and workload identity throughout every surface, and complete a compatibility-controlled cutover without rewriting committed event history. This removes Projects-owned technical runtime responsibilities while preserving existing behavior and consumers so later durability and release-evidence work can build on a secure, supported foundation.

## Stories

- Story 6.1: Pin platform capabilities and migration baseline
- Story 6.2: Restore Contracts, presentation, identity, and API boundaries
- Story 6.3: Enforce secure platform admission and authorization evidence
- Story 6.4: Migrate read models and queries to DomainService
- Story 6.5: Migrate command hosting and platform topology
- Story 6.6: Authenticate FrontComposer UI and CLI consumers
- Story 6.7: Execute compatibility cutover and retire legacy runtime

## Requirements & Constraints

- Treat the epic inventory as contained corrective scope: implementation is not schedulable until an independent readiness rerun returns `READY`. Production release, consequential autonomous MCP mutation, and proposal confirmation remain disabled until the later release gates pass.
- Pin every external capability, package or repository revision, route, public API, state key, event, cursor, consumer, in-flight record, compatibility constraint, owner, entry criterion, verification lane, and rollback condition before migration work begins. Changes to EventStore, FrontComposer, identity, Builds, or sibling domains require separate repository-local authority and evidence.
- Production admission must validate both the original actor and the authenticated workload/service identity and fail startup when authority, audience, signing, key, or credential configuration is incomplete. Tenant, actor, aggregate identity, delegation, scopes, and correlation/task context are server-derived; client fields cannot grant authority. Allow-all implementations are limited to explicit test composition.
- Authorization remains layered and fail-closed: Projects evaluates actor/action/Project policy, each owning context reauthorizes its resource, and queries filter again. Denied and nonexistent resources share a safe `404` boundary response, with metadata-only diagnostics and no cross-Tenant existence leakage.
- New governed identifiers use platform-generated ULIDs for Projects, tasks, messages, correlations, causations, events, and receipts. Foreign identifiers stay opaque and are never GUID-parsed; persisted legacy identifiers remain readable.
- Versioned .NET command/query contracts are the editable authority. OpenAPI, generated clients, JSON schemas, CLI/MCP schemas, and runtime descriptors are derived from and live-host-verified against those contracts. Legacy wire/client shapes use explicit versioned adapters.
- Contracts and events evolve additively and remain serialization-tolerant. Existing events, including historical success and rejection forms and pending-Folder history, must replay unchanged. Migration must preserve metadata-only, Tenant-isolation, idempotency, and command success/rejection/no-op semantics.
- List queries use authenticated, scope-bound opaque cursors with stable ordering, default 50 and maximum 200 rows.

## Technical Decisions

- Hexalith.Projects owns stable contracts, aggregate policy, validators, command/query and projection handlers, and Project-specific workflow definitions. EventStore DomainService owns hosting, persistence, publication, subscriptions, read-model stores, cursors, health, and telemetry. The platform AppHost owns Dapr and distributed topology; FrontComposer/platform hosts own Web, CLI, and MCP runtime composition and credentials.
- `Hexalith.Projects.Contracts` remains dependency-light and contains domain/wire vocabulary, operation schemas, identifiers, DTOs, enums, security semantics, action classification, and compatibility metadata. It must not depend on presentation, ASP.NET hosting, Dapr, or Aspire. Non-packable `Hexalith.Projects.UI.Contracts` depends inward on Contracts and contains descriptors only; it cannot redefine operations, vocabulary, or security.
- The target package graph retains packable Contracts, domain, and Projects-specific Testing packages; a non-packable Server is only an assembly-explicit two-line DomainService host. Keep the generated Client adapter only while supported consumers require it. Retire Projects-owned Infrastructure, Workers, ServiceDefaults, Aspire, AppHost, UI, MCP, and CLI runtime projects only after platform replacements and equivalent local, CI, persisted-boundary, restart, multi-instance, and authenticated surface lanes pass.
- Incremental projections implement `IAsyncDomainProjectionHandler` over `IReadModelStore` or `IReadModelBatchStore` with explicit `ReadModelWritePolicy`; full-replay handlers are compatibility-only. Queries implement `IDomainQueryHandler`; cursors use `IQueryCursorCodec` and authenticated `QueryCursorScope`. Verification must cover persisted end state, duplicate dispatch, rebuild, cursor scope, cursor tampering, deterministic keys/watermarks, and Tenant isolation.
- Cut over by replaying existing streams into shadow SDK read models, comparing them deterministically with legacy outputs, then switching read slices through reversible routing. Fence and drain old command ingress before atomically selecting one SDK writer; never run concurrent command writers. If the old writer cannot replay and emit every new compliant event, command rollback is forbidden: freeze mutation and roll forward instead.

## UX & Interaction Patterns

Projects supplies safe view models and presentation descriptors to the platform-hosted FrontComposer console; shell composition, authentication, credential acquisition, theme, telemetry, CLI/MCP registration, and topology remain platform responsibilities. Web, CLI, and MCP preserve the same server-defined authorization and transport semantics without gaining authority. UI and CLI consumers obtain platform credentials and carry actor plus delegated workload identity; they never submit authoritative Tenant or actor values.

## Cross-Story Dependencies

- Story 6.1 gates all later Epic 6 work by proving the exact supported platform seams, versions, owners, consumers, migration inventory, and external entry gates.
- Story 6.2 establishes the canonical contracts, compatibility adapters, presentation boundary, and identifier rules required by the read, command, and consumer migrations.
- Story 6.3 establishes secure server admission before Story 6.6 integrates authenticated Web and CLI consumers.
- Story 6.4 completes shadow-read parity and reversible read cutover before Story 6.5 fences legacy ingress and moves command hosting/topology.
- Story 6.7 runs after all prior compatibility, security, authenticated-consumer, local-operability, and rollback gates pass. It retires legacy runtime without removing required adapters prematurely.
- Epic 7 depends on these platform seams for durable cross-context workflows; the later conformance epic owns production release evidence, so Epic 6 completion alone does not authorize release.
