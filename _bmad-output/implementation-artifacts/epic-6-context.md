# Epic 6 Context: Supported Platform Boundary and Secure Identity

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Migrate Hexalith.Projects off its self-owned hybrid runtime (Server, Workers, Infrastructure, ServiceDefaults, Aspire/AppHost, Dapr plumbing) onto the supported Hexalith.EventStore DomainService and platform-owned hosting seams; restore clean contract and presentation package boundaries; enforce real caller and service identity end-to-end; and execute a compatibility-controlled cutover that preserves all committed event history. This is the first of three corrective epics required before any production release: a production-readiness audit found nine P1 blockers, and Epic 6 closes six of them (Projects-owned runtime, package-boundary violations, production authorization stubs, unauthenticated UI/CLI, non-canonical GUID identifiers, and OpenAPI/runtime contract drift). Observable outcome: authorized Chatbot callers can create, open, list, update, archive, and obtain Project data through the supported DomainService and platform runtime with real caller identity.

## Stories

- Story 6.1: Pin platform capabilities and migration baseline
- Story 6.2: Restore Contracts, presentation, identity, and API boundaries
- Story 6.3: Enforce secure platform admission and authorization evidence
- Story 6.4: Migrate read models and queries to DomainService
- Story 6.5: Migrate command hosting and platform topology
- Story 6.6: Authenticate FrontComposer UI and CLI consumers
- Story 6.7: Execute compatibility cutover and retire legacy runtime

## Requirements & Constraints

**Containment (in force until lifted):** No production release, consequential autonomous MCP operation, or proposal-confirmation enablement before Story 8.9 passes. Corrective development is frozen until an implementation-readiness rerun returns READY; current Epic 6 entries are an approved findings inventory that story creation must refine without weakening any entry, verification, compatibility, security, durability, or release gate. Sibling platform repos (EventStore, FrontComposer, Conversations, Folders, Chatbot) must not be modified without their own approved stories.

**Security and identity:**
- Production hosts require complete JWT and service-identity configuration; startup must fail closed when it is incomplete.
- Development allow-all authorization stubs must be unable to resolve outside explicitly declared development/test hosts.
- Tenant and actor authority are always server-derived; no client (Web, CLI, MCP, Chatbot, service-to-service) may supply authoritative tenant or actor identity.
- UI and CLI obtain credentials through platform credential providers with delegated service identity.
- Trust-bearing mutations deny on stale, unknown, rebuilding, or unavailable authorization evidence; unauthorized and nonexistent both surface as 404 (no cross-tenant existence leakage).

**Identity and API:**
- All new Project, message, correlation, task, causation, and confirmation IDs use platform ULID generation (no `Guid.NewGuid`); legacy identifiers stay readable through a documented compatibility window.
- Domain command/query contracts are canonical; OpenAPI is generated from or mechanically validated against them; existing generated consumers get a versioned adapter.
- All lists are cursor-bounded: default page 50, cap 200.
- Canonical Create Project requires a system-supplied metadata classification (`public_metadata`, `tenant_sensitive`, `credential_sensitive`, `secret` — a label, never storage authorization), derived by the owning integration adapter, not user-authored. Invalid classification: 400 ValidationFailure with `details.rejectedField = projectMetadata.metadataClass`, no value echo, no command submission; authorization precedes protected request parsing. Only the historical unversioned name-only shape gets v1 compatibility treatment. A shared server-owned validator is reused by direct create and proposal confirmation.
- Idempotency canonicalization must reject U+2028/U+2029 in identifier/envelope fields with byte-for-byte fingerprint parity between server and generated helpers; check deployed state for persisted legacy fingerprints before canonical bytes change.

**Migration safety:** Event history is never rewritten; rollback is a routing/adapter decision only; no dual-writing commands without proven deduplication; committed Project events have RPO 0. All existing metadata-only (`NoPayloadLeakage`), tenant-isolation, fail-closed, and additive-schema-evolution rules continue to apply.

## Technical Decisions

**Target ownership** (supersedes the old "Projects owns everything" scaffold):

| Concern | Owner |
| --- | --- |
| Aggregate, commands, events, validators, resolution/context policy, Project-specific task transitions | Hexalith.Projects |
| Projection and query handlers | Projects, via EventStore DomainService SDK seams |
| Hosting, persistence, publication, subscriptions, read-model stores, cursors, health, telemetry | EventStore DomainService |
| Dapr components and distributed topology | Platform AppHost |
| Web/MCP/CLI runtime composition and credential providers | FrontComposer/platform hosts |
| Stable domain and wire contracts | Hexalith.Projects.Contracts |
| Presentation descriptors | Separate presentation adapter |
| Generic durable workflow/task/confirmation capability | Shared EventStore/platform capability (consumed, not built here) |

**Contracts purity:** `Hexalith.Projects.Contracts` contains identifiers, commands, events, rejection events, query contracts, and stable DTOs only — no Fluent UI, Fluxor, FrontComposer Shell, ASP.NET hosting, MCP, CLI, or UI dependency. Presentation descriptors move to a non-packable `Hexalith.Projects.UI.Contracts` host that depends on the Contracts kernel (never the reverse) and stays out of the published package inventory; MCP and CLI remain independent of it. Dependencies point inward only: platform hosts → presentation adapter → application/domain handlers → Contracts. Story 6.2 completion gates release readiness of the Contracts package.

**Read/query migration:** Replace custom journals, stores, worker subscriptions, and cursors with `IAsyncDomainProjectionHandler`, `IReadModelStore`/`IReadModelBatchStore`, `IDomainQueryHandler`, and platform cursor/scope abstractions, using supported DomainService endpoint mapping and host composition. Replay existing events and compare deterministic old/new outputs before any cutover.

**Migration sequence:** (1) inventory routes, public APIs, state keys, events, cursors, consumers, and pending records; (2) pin/validate platform seams; (3) introduce clean Contracts/presentation boundaries with compatibility shims; (4) replay-compare SDK read models; (5) reconcile legacy folderless/pending records via compensating workflows (Epic 7 owns the durable-task side); (6) cut reads by value slice; (7) cut commands/workflows to DomainService; (8) migrate authenticated Web/MCP/CLI composition; (9) remove Projects-owned runtime/topology plumbing; (10) retain routing rollback until all release gates pass.

**Build:** Hexalith.Builds is the single version owner for NSwag.MSBuild 14.7.1 and Fluxor.Blazor.Web 6.9.0; Projects uses versionless `PackageReference` with central transitive pinning and must not reintroduce local pins. Generated `.g.cs` files are never hand-edited.

## Cross-Story Dependencies

- Story 6.1 is the entry gate for every other story: exact platform seams, upstream versions, routes, public APIs, state keys, events, cursors, consumers, and pending-record inventory must be pinned before implementation changes; epic effort is re-estimated after 6.1.
- Story 6.2 absorbs three previously approved corrections: the FrontComposer descriptor-boundary decision (ex-Story 5.13), Create Project metadata-classification enforcement, and U+2028/U+2029 idempotency parity; freshness-vocabulary alignment is shared with Story 8.3.
- Story 6.3 (server-side admission and authority) precedes Story 6.6 (client credential integration) — the server defines audience/claims before clients integrate. 6.3 acceptance is proven through authenticated create/open/list plus adversarial denial evidence.
- Stories 6.4 (reads) and 6.5 (commands/topology) follow the migration order — reads cut over before commands — and both depend on 6.1's inventory and 6.2's contract boundaries. Rebaseline guidance: slice both by observable outcomes (list/open/context/operator reads; first supported mutations), not as single all-projections/all-runtime stories.
- Story 6.7 runs last, requires per-slice compatibility and rollback gates from all prior stories, and retains routing rollback until release gates pass.
- Epic 7 (durable workflows) builds on Epic 6's platform seams; the generic workflow/task/confirmation capability is upstream platform work that Projects pins and consumes. Epic 8 supplies the blocking release evidence; Story 5.12 live-run failures are routed to Story 8.6, not into Epic 6.
