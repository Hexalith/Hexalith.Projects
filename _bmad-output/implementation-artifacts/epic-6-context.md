# Epic 6 Context: Supported Platform Boundary and Secure Identity

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Migrate Hexalith.Projects to the supported EventStore DomainService and platform-owned hosting model, restore clean contract and presentation boundaries, enforce authenticated caller and service identity across all surfaces, and complete a compatibility-controlled cutover without rewriting committed event history. This establishes the secure, supported foundation required by the later durability and production-conformance epics.

## Stories

- Story 6.1: Pin platform capabilities and migration baseline
- Story 6.2: Restore Contracts, presentation, identity, and API boundaries
- Story 6.3: Enforce secure platform admission and authorization evidence
- Story 6.4: Migrate read models and queries to DomainService
- Story 6.5: Migrate command hosting and platform topology
- Story 6.6: Authenticate FrontComposer UI and CLI consumers
- Story 6.7: Execute compatibility cutover and retire legacy runtime

## Requirements & Constraints

- Preserve existing functional outcomes, public consumers, persisted events, and replay behavior throughout migration. Existing event history is immutable; rollback changes routing or adapters rather than committed events, and command paths must not dual-write without proven deduplication.
- Inventory and pin supported platform seams, versions, routes, public APIs, state keys, events, cursors, consumers, pending records, compatibility constraints, verification lanes, and rollback conditions before implementation changes begin. Any required sibling-platform change needs its own approved work and repository-local validation.
- Production requires complete JWT and workload/service-identity configuration. Tenant and actor authority are derived server-side, and development authorization stubs must not resolve outside explicit development or test hosts. Incomplete authority, audience, signing, key, or credential configuration must fail closed.
- Authorization remains tenant-scoped and layered. Projects authorizes the Project operation, owning contexts authorize their resources, and query results are filtered again. Denied, cross-tenant, nonexistent, or unverifiable resources must not leak existence or payload data.
- Stable domain and wire contracts remain infrastructure-free and serialization-tolerant. Canonical .NET command/query contracts must generate, or be mechanically verified against, OpenAPI and downstream consumer schemas; existing generated consumers receive explicit compatibility adapters.
- New Project, message, correlation, task, causation, and confirmation identifiers use platform ULID generation. Persisted legacy identifiers remain readable during a documented compatibility window, while identifiers owned by other contexts remain opaque.
- List queries are cursor-bounded, use stable ordering, and bind cursors to the authenticated query scope. Read-model migration must prove deterministic replay equivalence before traffic moves.
- Production release, consequential autonomous MCP operations, and proposal-confirmation enablement remain blocked until the later production-conformance and acceptance gates pass.

## Technical Decisions

- Hexalith.Projects owns stable contracts, aggregates, commands, events, validators, resolution/context policy, and domain query/projection handlers. EventStore DomainService owns hosting, persistence, publication, subscriptions, read-model stores, cursors, health, and telemetry. The platform AppHost owns Dapr components and distributed topology; FrontComposer and platform hosts own Web, MCP, and CLI runtime composition.
- `Hexalith.Projects.Contracts` contains only stable domain and wire vocabulary: identifiers, commands, events, rejection events, query contracts, and DTOs. It has no presentation, FrontComposer shell, ASP.NET hosting, MCP, CLI, Dapr, or Aspire dependency. Presentation descriptors live in a separate adapter whose dependencies point inward toward Contracts and the application/domain handlers.
- Incremental projections use `IAsyncDomainProjectionHandler` with `IReadModelStore` or `IReadModelBatchStore`; queries use `IDomainQueryHandler`; cursors use the supported platform cursor and scope abstractions. Custom journals, stores, worker subscriptions, and cursor implementations are retired after equivalent behavior is proven.
- Cutover is staged: establish the inventory and compatibility shims, replay existing streams into SDK read models, compare old and new outputs deterministically, route reads to the SDK path, move command hosting to DomainService, migrate authenticated consumers, then remove Projects-owned runtime and topology plumbing. Reversible routing remains available until all gates pass.
- UI and CLI obtain credentials from platform providers. No client-supplied tenant or actor field is authoritative, and authenticated runtime composition must carry both caller and workload identity evidence to the server boundary.

## UX & Interaction Patterns

Projects supplies safe view models and presentation descriptors to the platform-hosted FrontComposer console; it does not own a standalone shell, authentication flow, theme, telemetry pipeline, or topology. Web, CLI, and MCP retain the same server-defined authorization semantics and metadata-only vocabulary. FrontComposer Web uses Fluent UI Blazor V5, Fluent 2 tokens, stable accessible names, and stable test identifiers; authentication or authorization failures render safe, non-leaking states rather than payload details.

## Cross-Story Dependencies

- Story 6.1 gates all later work by establishing the supported seams, owners, migration inventory, compatibility window, and rollback conditions.
- Story 6.2 establishes the contract, identifier, OpenAPI, presentation-adapter, and compatibility boundaries consumed by the security, read-model, hosting, and client migrations.
- Secure admission must be established before authenticated Web and CLI composition is accepted. Read-model replay parity and reversible read routing precede command-host cutover.
- Story 6.7 follows all prior compatibility, security, read, command, and authenticated-consumer gates and retires legacy plumbing only after supported replacements are proven.
- Epic 7 depends on this platform boundary for durable workflows and confirmations; Epic 8 owns production conformance and release evidence, so Epic 6 completion alone does not authorize release.
