---
stepsCompleted: [1, 2, 3, 4, 5, 6]
inputDocuments: []
workflowType: 'research'
lastStep: 6
research_type: 'technical'
research_topic: 'Referencing Hexalith.Conversations from the Hexalith.Projects domain (Projects → Conversations reference mechanics)'
research_goals: 'Integration how-to + concrete C# code patterns for how the Hexalith.Projects domain holds and manages references to conversations owned by Hexalith.Conversations. Scope: reference mechanics only — stable identifiers, the link field, discovery of a project''s conversations, read-time hydration, and query contracts — codebase-grounded and respecting EventStore/Tenants/Parties boundary rules.'
user_name: 'Jerome'
date: '2026-05-24'
web_research_enabled: true
source_verification: true
---

# Research Report: technical

**Date:** 2026-05-24
**Author:** Jerome
**Research Type:** technical

---

## Research Overview

This report answers a focused integration question: **how should the (greenfield) `Hexalith.Projects` domain hold and manage references to conversations owned by `Hexalith.Conversations`?** — the **Projects → Conversations** direction, scoped to **reference mechanics only**, delivered as an integration how-to with concrete C# patterns. It is grounded primarily in the actual `Hexalith.Conversations` code in this repository (contracts, events, server, client) and the loaded `project-context.md` rule sets; generic cross-context patterns are corroborated by current public DDD/event-sourcing/CQRS sources cited inline.

The headline finding is that the link **already exists and is owned by Conversations**: a conversation carries an optional, immutable `ProjectId` (set on `ConversationCreated`), and Conversations already supports discovery by it (`ConversationListFilterV1.ProjectId` + `ConversationQueryHandler.ListAsync`, with fail-closed tenant scoping and `ProjectionTrustState` freshness). Therefore Projects should **reference and discover** rather than own — never storing an unbounded `ConversationId` list in its aggregate. The recommended default (**Pattern A**) is to query Conversations by `ProjectId` behind a Projects-owned **Anti-Corruption Layer**; the single real gap is that the typed `IConversationClient` exposes only Create/Append/Get, so a `ListConversationsAsync` method (or a read-API call) must be added. An optional event-fed **Pattern B** projection is justified only when hot-path listing, local joins, or offline resilience demand it.

See the **Research Synthesis** section at the end for the executive summary, decision matrix, consolidated sources, and next steps; the five analysis sections in between carry the detailed, cited reasoning and code.

---

## Technical Research Scope Confirmation

**Research Topic:** Referencing Hexalith.Conversations from the Hexalith.Projects domain (Projects → Conversations reference mechanics)
**Research Goals:** Integration how-to + concrete C# code patterns for how the Hexalith.Projects domain holds and manages references to conversations owned by Hexalith.Conversations. Scope: reference mechanics only — stable identifiers, the link field, discovery of a project's conversations, read-time hydration, and query contracts — codebase-grounded and respecting EventStore/Tenants/Parties boundary rules.

**Technical Research Scope:**

- Reference identity & ownership — stable `ConversationId`, tenant scoping, source-of-truth for the link (existing `ConversationCreated.ProjectId` back-reference vs. a Projects-owned forward reference)
- The link field & durable storage — storing stable IDs in Projects events vs. deriving them; never copying upstream conversation state
- Discovery mechanics — querying Conversations by `ProjectId` vs. a Projects-side projection of `ConversationId`s
- Read-time hydration — `IConversationReferenceHydrationDirectory`, `ProjectReferenceHydrationV1`, `ProjectionTrustState`, fail-closed boundary
- Query/contract surface — adopter-facing `Hexalith.Conversations.Client`/query/projection contracts and boundary rules

**Research Methodology:**

- Codebase-grounded first: primary sources are the `Hexalith.Conversations` contracts/events/server code and the loaded `project-context.md` rule sets
- Web/docs to validate the pattern: current, citeable sources on event-sourcing / DDD bounded-context reference-by-ID, read-side hydration, and fail-closed cross-context reads
- Confidence levels flagged where the not-yet-built Projects domain forces an assumption
- Module-specific guidance (from code) kept clearly separated from generic pattern claims (from public sources)

**Scope Confirmed:** 2026-05-24

---

## Technology Stack Analysis

> Scope note: `Hexalith.*` modules are private, so the **module-specific** facts below come from reading the code in this repository (paths cited inline). The **generic pattern** claims (reference-by-ID across bounded contexts) are corroborated by current public DDD/event-sourcing sources, cited at the end of each subsection. Confidence is **High** for code-grounded facts and **High** for the generic pattern.

### Programming Language & Runtime

- **C# / .NET 10** (`net10.0`); SDK pinned `10.0.300` (`global.json`, `rollForward: latestPatch`). Nullable reference types, implicit usings, and warnings-as-errors are on; Central Package Management via `Directory.Packages.props`.
- Contracts are **`sealed record`s with `required`/positional members** and eager boundary validation (e.g. `ProjectId` throws on empty `value`). Identifier value objects serialize through custom `System.Text.Json` converters (`ProjectIdJsonConverter`, `IdentifierJsonConverters.cs`).
- _Confidence: High — `_bmad-output/project-context.md`; `Hexalith.Conversations/.../Identifiers/ProjectId.cs`._

### Event-Sourcing Substrate — `Hexalith.EventStore`

- The authoritative write-side persistence path. A conversation is `ConversationAggregate : EventStoreAggregate<ConversationState>`; identity is canonical `{tenant}:{domain}:{aggregateId}`. Flow is **persist-then-publish**; `Handle(...)` is pure and returns a `DomainResult`, `Apply(...)` mutates only in-memory state.
- EventStore owns routing, actor identity, snapshots, publication, projection invalidation, and command status. Adopters must **not** see raw EventStore envelopes, aggregate IDs, or projection internals as the integration surface.
- _Confidence: High — `Hexalith.Conversations/_bmad-output/project-context.md`; `State/ConversationState.cs`._

### Infrastructure Abstraction — Dapr

- Dapr is the **only** permitted infrastructure abstraction in domain services (no direct Redis/PostgreSQL/Cosmos/broker clients). Actors serialize writes per aggregate identity. Dapr pub/sub is **at-least-once**, so every projection/event handler must tolerate duplicates and replay; tenant events may arrive out of order.
- Versions differ per module (Conversations tracks Dapr `1.17.7`); do not assume one version across the umbrella.
- _Confidence: High — root + Conversations `project-context.md`._

### Bounded-Context Reference Model — the core of this research

- Conversations references every upstream identity by a **stable, opaque ID value object**, never by importing the upstream entity or its state:
  - `ProjectId(string Value)` — _"References an upstream project identity without copying project state."_ (`Contracts/Identifiers/ProjectId.cs:12-18`)
  - Siblings: `FolderId`, `FileId`, plus `PartyId`/`TenantId` from upstream modules.
- This is the textbook cross-bounded-context pattern: *"an entity that is fully defined in one context … is just an id in other contexts"*, and aggregates reference each other **only through IDs, not object references**, with domain events for propagation. The same rule applies in reverse for `Hexalith.Projects` referencing conversations: hold a **`ConversationId` value**, never a `ConversationState`/projection object.
- _Confidence: High (code) + High (pattern). Sources: [Sapiensworks – Identifying Bounded Contexts](https://blog.sapiensworks.com/post/2014/10/31/DDD-Identifying-Bounded-Contexts-and-Aggregates-Entities-and-Value-Objects.aspx); [Context Mapper – Event Sourcing & CQRS Modeling](https://contextmapper.org/docs/event-sourcing-and-cqrs-modeling/); [SSENSE-TECH – Mastering Multi-Bounded-Context Integration](https://medium.com/ssense-tech/ddd-beyond-the-basics-mastering-multi-bounded-context-integration-ca0c7cec6561)._

### Conversations Adopter Surface (what `Hexalith.Projects` would consume)

| Layer | Type | Project-relevant shape | Path |
|---|---|---|---|
| Contracts (identity) | `ConversationId`, `ProjectId` | Opaque stable IDs | `Contracts/Identifiers/` |
| Contracts (event) | `ConversationCreated` | carries optional `ProjectId? ProjectId` | `Contracts/Events/ConversationCreated.cs` |
| Contracts (query filter) | `ConversationListFilterV1` | **`ProjectId? ProjectId`** exact-match filter | `Contracts/Queries/ConversationListFilterV1.cs:36` |
| Contracts (query result) | `ConversationSummaryV1` | returns `ConversationId`, `ProjectId`, `ProjectHydration`, `Freshness` | `Contracts/Queries/ConversationSummaryV1.cs` |
| Contracts (hydration) | `ProjectReferenceHydrationV1` | `ProjectId` + `ProjectionTrustState` + `SafeLabel/SafeStatus` | `Contracts/Queries/ProjectReferenceHydrationV1.cs` |
| Client (typed .NET) | `IConversationClient` | **Create / Append / Get only — no List** | `Client/IConversationClient.cs` |
| Server (query) | `ConversationQueryHandler.ListAsync` | applies the `ProjectId` filter; tenant-scoped + fail-closed | `Server/Queries/ConversationQueryHandler.cs:303,351` |
| Server (HTTP) | `ConversationReadApi` | exposes reads over HTTP | `Server/Api/ConversationReadApi.cs` |

- **⚠️ Key stack gap for the Projects→Conversations direction:** the *server* already supports "list conversations where `ProjectId == X`" (`ConversationQueryHandler.ListAsync` + `ConversationListFilterV1.ProjectId`, with a dedicated `ConversationSearchMatchSource.ProjectReference` reason), but the *typed `IConversationClient`* does **not** expose a list/search method yet. So a consuming Projects module reaches conversations-by-project via the **read API / `ListConversationsQuery`**, not via today's typed client — or the client must be extended. This is the single most important mechanical fact for the integration.
- _Confidence: High — files cited above._

### Read-Side Trust & Hydration Stack

- `ProjectionTrustState` (`Current/Stale/Rebuilding/Unavailable/Redacted/Forbidden`) + `ProjectionFreshnessV1` travel with every read; the list handler aggregates **worst-case** freshness and refuses **mixed-generation** rows (`ConversationQueryHandler.cs:223,434`).
- Cross-reference display data is resolved behind `IConversationReferenceHydrationDirectory` (`HydrateProjectsAsync`, etc.), which is **fail-closed**: reads degrade to safe labels rather than leaking upstream personal/tenant data.
- _Confidence: High — `Server/Hydration/IConversationReferenceHydrationDirectory.cs`; `Server/Queries/ConversationQueryHandler.cs`._

**Cross-stack takeaway:** every layer Projects would touch is already **tenant-scoped, ID-referenced, and fail-closed**. The reference link itself is solved; the only stack-level work is choosing how Projects *discovers* and *holds* `ConversationId`s (covered in Integration Patterns next).

---

## Integration Patterns Analysis

> Deliverable focus: concrete, codebase-grounded patterns for the **Projects → Conversations** direction. Code marked _illustrative_ infers a constructor/route shape from the real types read in this repo; types in `code` are verified to exist. Confidence: **High** for the recommendation, **Medium** where it depends on the not-yet-built `Hexalith.Projects` aggregate.

### Two confirmed mechanics that constrain every pattern

1. **The link is immutable and owned by Conversations.** `ProjectId` is set only on `ConversationCreated` and copied into `ConversationState.ProjectId`; no event re-parents a conversation (`ConversationMetadataUpdated` changes only Label/BusinessReference/Attributes). So **Conversations is the single source of truth for the conversation↔project link**, and the link never changes after creation. Projects must *reference and discover*, not *own*.
2. **`Hexalith.Projects` is greenfield** — it can choose its consumption boundary freely, so the only question is *how Projects discovers and holds `ConversationId`s*, never *how it stores conversation content* (it never does).

### Decision: who owns the reference?

| | Conversation → Project (exists) | Project → Conversation (this research) |
|---|---|---|
| Source of truth | `Conversation.ProjectId` (immutable) | **None new** — derived from the above |
| Storage in the aggregate | n/a | **Avoid** an unbounded `ConversationId` list in the `Project` aggregate |
| How Projects sees its conversations | n/a | Query Conversations by `ProjectId` **or** keep a Projects-side read model |

Holding a growing list of conversation IDs in the `Project` aggregate is the classic **unbounded-collection anti-pattern**: _"if there's no underlying invariant to protect, don't return them all for commands — execute queries directly against the read side."_ ([Ardalis – Aggregate Responsibility Design](https://ardalis.com/aggregate-responsibility-design/); [Khalil Stemmler – Collections in Aggregates](https://khalilstemmler.com/articles/typescript-domain-driven-design/one-to-many-performance/)). So the link lives on the read side, and the choice is the standard CQRS trade-off below ([Microsoft Learn – CQRS](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs); [microservices.io – CQRS](https://microservices.io/patterns/data/cqrs.html)).

### Pattern A — Query by back-reference (recommended default)

Projects asks Conversations for "all conversations where `ProjectId == X`". The filter and handler already exist (`ConversationListFilterV1.ProjectId`, `ConversationQueryHandler.ListAsync`, match-source `ProjectReference`). No Projects-side storage, no consistency lag beyond Conversations' own projection freshness.

```csharp
// Illustrative: shapes follow ListConversationsQuery + ConversationListFilterV1 (verified) and
// the query handler's tenant-scoped ListAsync (verified). Page/cursor types per ConversationQueryCursor.
var query = new ListConversationsQuery(
    SchemaVersion: ConversationSchemaVersions.V1,
    TenantId: tenantId,                 // Hexalith.Conversations.Contracts TenantId
    CallerPrincipalId: callerPrincipal, // never trust the JWT tenant claim alone
    CorrelationId: correlationId,
    Filter: new ConversationListFilterV1(ProjectId: new ProjectId(project.Id.Value)),
    Page: new ConversationPage(PageSize: 50, ContinuationCursor: cursor));

ConversationListResult result = await _conversations.ListAsync(query, ct);

// result carries ProjectionTrustState + per-row ConversationSummaryV1 (ConversationId, ProjectHydration, Freshness).
// Treat anything other than Current as a degraded read and surface it — do not pretend data is fresh.
```

**The one wiring gap (verified):** the typed `IConversationClient` exposes **Create/Append/Get only — no list**. To use Pattern A you must either:
- **(A1)** extend the typed client with a `ListConversationsAsync(ListConversationsQuery, …)` method that forwards to the existing server handler / read API (cleanest for in-process adopters); or
- **(A2)** call `ConversationReadApi` over HTTP from Projects (the path Admin CLI/MCP already use — "Admin CLI and MCP clients call Admin API over HTTP").

- **Use when:** the project view can tolerate a cross-context read and Conversations' projection freshness; you want zero duplication and the link to always reflect the owner. _Confidence: High._

### Pattern B — Projects-side reference projection (when you need hot-path reads / joins)

Projects maintains its own read model, e.g. `ProjectConversationsView { ProjectId, IReadOnlyList<ConversationId>, counts, lastActivity }`, fed by **subscribing to Conversations' published events** (Dapr pub/sub). Because `ProjectId` rides on `ConversationCreated`, the project key is available at creation; lifecycle events update status/counts.

```csharp
// Illustrative projection handler in Hexalith.Projects (Projections layer).
// MUST be idempotent and tolerate duplicates + out-of-order delivery (Dapr pub/sub is at-least-once).
public void Apply(ConversationCreated e)
{
    if (e.ProjectId is null) return;                 // not all conversations belong to a project
    if (_view.Conversations.Contains(e.Metadata.ConversationId)) return; // dedupe / replay-safe
    _view = _view.AddConversation(e.Metadata.ConversationId, e.ProjectId);
}

public void Apply(ConversationClosed e)   => _view = _view.MarkClosed(e.Metadata.ConversationId);
public void Apply(ConversationArchived e) => _view = _view.MarkArchived(e.Metadata.ConversationId);
```

- Store **only the stable `ConversationId`** (and derived counts/status) — never conversation content. This mirrors the rule that Conversations stores upstream `ProjectId` without copying project state.
- Immutability simplifies this: there is **no re-parent event** to reconcile. The only ordering concern is a conversation event arriving before Projects knows the project exists — handle by fail-closed/late-create or rebuild.
- **Use when:** the project page must list/sort/count conversations locally, join them with project data, or stay readable when Conversations is unavailable. Accept **eventual consistency**. _Confidence: High (pattern) / Medium (exact Projects projection shape)._

### Pattern C — Durable reference inside the `Project` aggregate (only for an invariant)

Put a conversation reference in the `Project`'s own **events/state** *only* if the Project must enforce a rule over it (e.g. "a project cannot be archived while it has open conversations"). Even then, store a **bounded derived fact** (e.g. `OpenConversationCount`) fed by a policy/event, not an unbounded `ConversationId[]`. Default: **don't** — prefer A or B. _Confidence: High._

### The reference value object (boundary choice)

Hold the link as a typed **`ConversationId` value**, never a `ConversationState`/projection object. Two options:
- **Depend on `Hexalith.Conversations.Contracts`** and reuse its `ConversationId` (+ `ProjectId`) — avoids ID drift and serialization mismatch; Contracts is intentionally low-dependency, so this respects the boundary rules.
- **Define a Projects-owned `ConversationReference` VO** wrapping the opaque string — zero contract coupling, but you own validation/serialization and must keep it byte-compatible.

Recommendation: **reuse `Hexalith.Conversations.Contracts` identifiers** unless you have a reason to fully decouple; it is the same posture Conversations took toward `ProjectId`.

### Communication protocol & tenant scoping (applies to all patterns)

- **In-process** (Projects Server hosting Conversations Server): call the query handler / typed client directly.
- **Out-of-process** (Admin CLI, MCP, separate service): call `ConversationReadApi` / Admin API **over HTTP** — never reach into Dapr or EventStore streams directly.
- Every call passes **`TenantId` + caller principal**; authorization is decided by the local Tenants projection, **not** the JWT tenant claim. Cross-tenant access must be impossible by construction.

### Display hydration

For a project view that shows conversation labels/status, consume `ConversationSummaryV1` (already carries `ProjectHydration` + `Freshness`) or hydrate via `IConversationReferenceHydrationDirectory`. Hydration is **fail-closed**: render `SafeLabel`/`SafeStatus` and the `ProjectionTrustState`, degrading rather than leaking upstream personal/tenant data.

### Recommended combination

- **Start with Pattern A** (add the `ListConversationsAsync` client method or call the read API) — it is correct, fully tenant-safe, and zero-duplication today.
- **Add Pattern B** only when profiling shows the project page needs local listing/joins or offline resilience; feed it from `ConversationCreated`/lifecycle events with idempotent, replay-safe handlers.
- **Reserve Pattern C** for a genuine project-side invariant, storing a bounded derived fact.

**Sources (this section):** [Ardalis – Aggregate Responsibility Design](https://ardalis.com/aggregate-responsibility-design/) · [Khalil Stemmler – Handling Collections in Aggregates](https://khalilstemmler.com/articles/typescript-domain-driven-design/one-to-many-performance/) · [Microsoft Learn – CQRS Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs) · [microservices.io – CQRS](https://microservices.io/patterns/data/cqrs.html) · [Microsoft Learn – Event Sourcing Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing)

---

## Architectural Patterns and Design

> Where the chosen reference pattern sits inside the (greenfield) `Hexalith.Projects` module, and the invariants that keep it aligned with the umbrella architecture. Confidence: **High** for placement/dependency rules (they follow existing Hexalith conventions), **Medium** for the exact Projects type names (module not yet built).

### Anti-Corruption Layer — the central architectural placement

`Hexalith.Projects` should reach Conversations **only through a Projects-owned adapter** — an Anti-Corruption Layer (ACL) — never by calling `IConversationClient`/the read API from domain or projection logic directly. This mirrors the exact pattern already in this codebase: Conversations wraps `Hexalith.Parties.Client` behind `IParticipantDirectory` and resolves upstream display behind `IConversationReferenceHydrationDirectory` ("_never call Parties from aggregate logic_").

```csharp
// Projects-owned ACL facade (Hexalith.Projects.Server). Exposes Projects' language, hides Conversations contracts.
public interface IProjectConversationDirectory
{
    // Pattern A discovery, translated into a Projects-shaped result (not ConversationListResult).
    ValueTask<ProjectConversationsPage> ListForProjectAsync(
        ProjectId projectId, TenantId tenantId, CallerPrincipalId caller, PageRequest page, CancellationToken ct);
}
```

The ACL is the **Adapter** (HTTP/retries when out-of-process) + **Translator** (Conversations `ConversationSummaryV1` → Projects view model) + **Facade** (Projects-language operations). It is the only place `Hexalith.Conversations.*` types appear. ([Microsoft Learn – Anti-corruption Layer](https://learn.microsoft.com/en-us/azure/architecture/patterns/anti-corruption-layer); [AWS – ACL pattern](https://docs.aws.amazon.com/prescriptive-guidance/latest/cloud-design-patterns/acl.html); [DevIQ – Anti-Corruption Layer](https://deviq.com/domain-driven-design/anti-corruption-layer/)).

### Where each piece lives (module layout)

| Concern | Package | Notes |
|---|---|---|
| `ConversationId`/`ConversationReference` VO | `Projects.Contracts` | reuse `Conversations.Contracts` ID, or wrap the opaque string; keep Contracts low-dependency |
| `IProjectConversationDirectory` + adapter impl | `Projects.Server` | the ACL; depends on `Conversations.Client`/`Conversations.Contracts` |
| `ProjectConversationsView` projection (Pattern B) | `Projects.Projections` | fed by `ConversationCreated`/lifecycle events; idempotent, replay-safe |
| Bounded derived fact (Pattern C only) | `Projects` (aggregate/state) | e.g. `OpenConversationCount`; no unbounded ID list |
| Admin UI wiring | `Projects.Admin.Web` via FrontComposer contracts | display `SafeLabel`/`ProjectionTrustState`; never browse EventStore streams |

### Dependency direction (machine-checkable boundary)

- `Projects.Contracts` → at most `Conversations.Contracts` (for the ID type); **no** Dapr, HTTP, EventStore server packages.
- `Projects.Server`/`Projects.Projections` → `Conversations.Client` + `Conversations.Contracts` (inward only).
- **Aggregate logic must not call Conversations** — discovery/hydration is application/projection concern, exactly as Conversations keeps Parties calls out of aggregate logic. Authorization/orchestration stays outside the aggregate.

### Schema-evolution & versioning posture

- If you adopt **Pattern B**, the Projects projection subscribes to `ConversationCreated` (and lifecycle events). It must keep **backward-compatible, additive, serialization-tolerant** deserialization for every version ever produced — **no `V2` event types**. Treat `ProjectId == null` as "not project-scoped" (the field is optional).
- The conversation↔project link is **immutable** (no re-parent event). If a future requirement needs re-parenting, that is a **new Conversations capability** (a new event), not something Projects can synthesize — record it as an open decision.

### Consistency & freshness architecture

- **Pattern A** inherits Conversations' projection freshness: every read carries `ProjectionTrustState` (`Current/Stale/Rebuilding/Unavailable/Redacted/Forbidden`) and the list handler aggregates **worst-case** freshness and refuses **mixed-generation** rows. Surface this in the project view.
- **Pattern B** is **eventually consistent** by construction; the Projects projection must expose stale/rebuilding/unavailable states rather than pretending its conversation list is fresh, and tolerate at-least-once duplicates / out-of-order delivery.
- Both **fail closed**: missing/stale/ambiguous → degrade or hide, never invent.

### Security architecture

- Tenant isolation at every layer; the **local Tenants projection** decides access, not the JWT tenant claim. Cross-tenant conversation discovery must be **impossible by construction** and tested adversarially.
- The ACL must map Conversations denials/failures to Projects-safe problem types and must not leak upstream personal/tenant data; logs use structured metadata, never payloads or full conversation content.

### Architectural decisions to record (ADRs)

Per the Conversations rule that load-bearing choices get ADRs, the Projects module should record:
1. **Reference ownership** — Conversations owns the link; Projects derives it (no aggregate-owned conversation list).
2. **Discovery pattern** — A (query owner) as default; B (local projection) only when profiling justifies it.
3. **Identifier boundary** — reuse `Conversations.Contracts.ConversationId` vs. Projects-owned VO.
4. **Freshness surface** — how `ProjectionTrustState` is presented in the project view.
5. **Re-parenting** — explicitly out of scope until Conversations adds an event; do not assume it.

**Sources (this section):** [Microsoft Learn – Anti-corruption Layer](https://learn.microsoft.com/en-us/azure/architecture/patterns/anti-corruption-layer) · [AWS Prescriptive Guidance – ACL](https://docs.aws.amazon.com/prescriptive-guidance/latest/cloud-design-patterns/acl.html) · [DevIQ – Anti-Corruption Layer](https://deviq.com/domain-driven-design/anti-corruption-layer/) · [Software Architecture Guild – Integration of Bounded Contexts](https://software-architecture-guild.com/guide/architecture/domains/integration-of-bounded-contexts/)

---

## Implementation Approaches and Technology Adoption

> A concrete, incremental build sequence for the reference mechanics, grounded in the Hexalith conventions already in this repo. Confidence: **High** for the sequence/tests; **Medium** for exact Projects type names (module not yet built).

### Technology adoption strategy — incremental, Pattern A first

1. **Identifier boundary.** In `Projects.Contracts`, reuse `Hexalith.Conversations.Contracts.Identifiers.ConversationId` (preferred, avoids drift) or wrap the opaque string in a Projects-owned `ConversationReference`.
2. **Close the client gap.** Add `ListConversationsAsync(ListConversationsQuery, …)` to `IConversationClient`/`ConversationClient` forwarding to the existing server handler **(A1)**, or have the ACL call `ConversationReadApi` over HTTP **(A2)** for out-of-process Projects.
3. **Build the ACL** (`Projects.Server`): `IProjectConversationDirectory` → calls the client/read API with `ConversationListFilterV1(ProjectId: …)`, translates `ConversationSummaryV1` → a Projects view model, and maps `ProjectionTrustState` to a Projects-safe freshness signal.
4. **Wire the project view** to the ACL; render `SafeLabel`/`SafeStatus` + freshness. Ship this as the MVP — it is fully correct and tenant-safe today.
5. **(Optional) Pattern B projection** (`Projects.Projections`): subscribe to `ConversationCreated` + lifecycle events; maintain `ProjectConversationsView`; idempotent + replay-safe.
6. **(Optional) Pattern C** only if a project invariant requires it — store a bounded derived fact.

### Development workflows and tooling

- Per-submodule build (`dotnet build Hexalith.Projects.slnx`), Central Package Management, `net10.0`, nullable + warnings-as-errors. Conventional commits / semantic-release where `package.json` + `commitlint.config.mjs` exist. Admin UI via FrontComposer **contract annotations**, never hand-edited generated files.

### Testing and quality assurance

- **Tier 1 (pure, fast)** — the highest-value tests here:
  - **ACL translator:** `ConversationSummaryV1 → Projects view model` maps IDs, labels, and every `ProjectionTrustState` correctly; non-`Current` states surface as degraded.
  - **Pattern B projection idempotency/ordering:** apply the same `ConversationCreated` twice → **one** entry (dedupe by `ConversationId`); a lifecycle event arriving before/after create is handled; `ProjectId == null` is skipped. This matches the at-least-once + out-of-order test recipe (process the event twice, assert the side effect occurred once; guard stale overwrites).
  - **Tenant isolation (negative path):** discovery for tenant A never returns tenant B's conversations; cross-tenant request → hidden/empty, fail-closed.
- Use `Hexalith.EventStore.Testing` / `Hexalith.Tenants.Testing` fakes, **xUnit v3**, **Shouldly**, **NSubstitute** — do not invent new doubles or mock inside aggregate logic.
- **Integration:** the `client → ConversationReadApi` HTTP path; Dapr slim test for the Pattern B subscription.
- _Sources:_ [microservices.io – Idempotent consumer](https://microservices.io/post/microservices/patterns/2020/10/16/idempotent-consumer.html) · [Domain Centric – ES projection deduplication](https://domaincentric.net/blog/event-sourcing-projection-patterns-deduplication-strategies) · [OneUptime – Idempotent Dapr handlers](https://oneuptime.com/blog/post/2026-03-31-dapr-idempotent-event-handlers/view) · [Cockroach Labs – Idempotency & ordering](https://www.cockroachlabs.com/blog/idempotency-and-ordering-in-event-driven-systems/)

### Deployment and operations practices

- Wire topology through the Projects Aspire AppHost consistently with `ServiceDefaults`. Structured logging with tenant/correlation context — **never** log conversation content or payloads. For Pattern B, monitor **projection lag/freshness** (pub/sub is at-least-once) via OpenTelemetry.

### Team organization and skills

- Required familiarity: EventStore aggregate/projection model, Dapr pub/sub (at-least-once) semantics, Hexalith tenant-isolation/fail-closed discipline, and ACL/adapter hygiene. No new infra skills — it is the same stack as sibling modules.

### Cost optimization and resource management

- **Pattern A:** one cross-context read per project-conversation view — mitigate with paging (cursor already supported) and short-lived caching; avoid N+1 hydration (batch via the hydration directory).
- **Pattern B:** extra storage + projection compute + an eventual-consistency window — adopt only when profiling shows Pattern A is too slow or you need offline resilience/local joins.

### Risk assessment and mitigation

| Risk | Likelihood | Mitigation |
|---|---|---|
| Typed client lacks list | Certain (today) | Add `ListConversationsAsync` (A1) or use read API (A2) |
| Eventual-consistency confusion (Pattern B) | Medium | Surface `ProjectionTrustState`; never present stale as fresh |
| Conversation event before project known | Low–Med | Late-create/rebuild; idempotent handlers |
| Assuming re-parenting works | Medium | Link is immutable; re-parenting needs a **new Conversations event** |
| Cross-tenant leakage | Low/High-impact | Tenant decided by local Tenants projection; adversarial tests |
| Unbounded ID list in aggregate | Low | Forbid; keep the link on the read side |

## Technical Research Recommendations

### Implementation Roadmap

- **Phase 1 (MVP):** identifier boundary → client `ListConversationsAsync`/read-API → `IProjectConversationDirectory` ACL → project view with freshness. **Pattern A only.**
- **Phase 2 (conditional):** `ProjectConversationsView` projection fed by `ConversationCreated`/lifecycle events — add when hot-path listing, local joins, or offline resilience are required.
- **Phase 3 (rare):** aggregate-side bounded derived fact for a genuine project invariant.

### Technology Stack Recommendations

- `net10.0`; depend on `Hexalith.Conversations.Client` + `Hexalith.Conversations.Contracts`; Dapr pub/sub for Phase 2; testing with xUnit v3 + Shouldly + NSubstitute + `Hexalith.EventStore.Testing`/`Hexalith.Tenants.Testing`.

### Skill Development Requirements

- EventStore aggregate/projection patterns; Dapr at-least-once semantics + idempotent handlers; tenant-isolation/fail-closed enforcement; ACL/adapter discipline.

### Success Metrics and KPIs

- **Correctness:** zero cross-tenant disclosures in adversarial tests; every degraded read surfaces a trust state.
- **Performance:** p95 project-conversation-view latency within budget; Phase 2 projection lag bounded and observable.
- **Quality:** tests cover dedupe, out-of-order, `ProjectId==null`, and tenant negative paths; no `.g.cs` hand-edits; warnings-as-errors clean.

---

# Referencing Conversations from Projects: A Reference-Mechanics Guide for `Hexalith.Projects` → `Hexalith.Conversations`

## Executive Summary

`Hexalith.Conversations` already solves the conversation↔project link, and it solves it in the direction this research needs to consume. A conversation carries an **optional, immutable `ProjectId`** stamped at `ConversationCreated`, and the module already ships the discovery path for it — `ConversationListFilterV1.ProjectId`, `ConversationQueryHandler.ListAsync`, a dedicated `ProjectReference` match-source, fail-closed tenant scoping, and `ProjectionTrustState` freshness travelling on every read. The architectural consequence is decisive: **Conversations is the single source of truth for the link**, so the greenfield `Hexalith.Projects` domain should *reference and discover*, never *own*. Storing a growing list of conversation IDs inside the `Project` aggregate would be the textbook unbounded-collection anti-pattern; the link belongs on the read side.

The recommended design is **Pattern A — query by back-reference behind an Anti-Corruption Layer**: a Projects-owned `IProjectConversationDirectory` calls Conversations filtered by `ProjectId`, translates `ConversationSummaryV1` into a Projects view model, and surfaces freshness. This is correct, fully tenant-safe, and zero-duplication today. There is exactly one mechanical gap to close: the typed `IConversationClient` exposes only **Create/Append/Get**, so Projects must either add a `ListConversationsAsync` method that forwards to the existing server handler, or call `ConversationReadApi` over HTTP (the path Admin CLI/MCP already use). An optional **Pattern B** — a local, event-fed `ProjectConversationsView` projection — is warranted only when profiling shows Pattern A is too slow or local joins/offline resilience are required, and it must be idempotent and replay-safe because Dapr pub/sub is at-least-once.

Because the link is **immutable** (no re-parent event exists), the integration is unusually simple: there is no cross-aggregate mutation to coordinate, only discovery and display. The remaining discipline is the standard Hexalith posture — tenant isolation decided by the local Tenants projection (not the JWT claim), fail-closed degradation, additive/tolerant schema evolution for any event subscriber, and ACL hygiene that keeps `Hexalith.Conversations.*` types out of Projects' domain and projection logic.

**Key Technical Findings:**

- **The link already exists and is owned by Conversations** — `ProjectId` on `ConversationCreated`/`ConversationState`, immutable after creation.
- **Discovery is built-in server-side** — `ConversationListFilterV1.ProjectId` + `ConversationQueryHandler.ListAsync`, tenant-scoped and fail-closed, with worst-case freshness + mixed-generation guards.
- **One real wiring gap** — the typed `IConversationClient` has no list/search; reach conversations-by-project via a new client method or `ConversationReadApi` HTTP.
- **Reference, don't own** — no unbounded `ConversationId` list in the `Project` aggregate; the link lives on the read side (Pattern A query, or Pattern B projection).
- **Immutability simplifies everything** — no re-parenting; if a project needs to move a conversation, that is a *new Conversations event*, not a Projects responsibility.

**Technical Recommendations:**

1. Adopt **Pattern A** as the default: a Projects-owned `IProjectConversationDirectory` ACL over Conversations, filtering by `ProjectId`.
2. **Close the client gap** with `ListConversationsAsync` (forwarding to the server handler) or a `ConversationReadApi` HTTP call.
3. **Reuse `Hexalith.Conversations.Contracts.ConversationId`** as the reference value (avoid ID drift); never hold a conversation object.
4. **Keep the link off the aggregate** — read-side only; add **Pattern B** projection only when profiling justifies it, with idempotent/replay-safe handlers.
5. **Record ADRs** for ownership, discovery pattern, identifier boundary, freshness surfacing, and the out-of-scope re-parenting decision.

## Table of Contents

1. Research Overview _(top of document)_
2. Technical Research Scope Confirmation
3. Technology Stack Analysis — substrate, reference model, and the adopter surface
4. Integration Patterns Analysis — Patterns A/B/C, code, and the ownership decision
5. Architectural Patterns and Design — ACL placement, layout, dependency direction, ADRs
6. Implementation Approaches and Technology Adoption — build sequence, tests, risks, roadmap
7. Research Synthesis _(this section)_ — decision matrix, sources, conclusion, next steps

## Decision Matrix — choosing the discovery pattern

| Criterion | Pattern A (query owner) | Pattern B (local projection) | Pattern C (aggregate fact) |
|---|---|---|---|
| Source of truth | Conversations (no copy) | Conversations (+ derived copy) | Conversations (+ invariant fact) |
| Consistency | Conversations' own freshness | **Eventually consistent** | Strong, but only a derived fact |
| Duplication | None | `ConversationId`s + counts | Bounded fact only |
| Best for | Default; correctness; low effort | Hot-path lists, joins, offline | A genuine project invariant |
| Cost | Cross-context read per view | Storage + projection compute | Aggregate complexity |
| Prereq | Add client list / use read API | Subscribe to conversation events | An actual invariant to protect |
| Recommendation | **Start here** | Add when profiling justifies | Avoid unless required |

## Research Methodology and Source Verification

**Approach.** Codebase-grounded for all module-specific claims (every such claim cites a file path read in this repository), with current public sources validating the generic cross-context patterns. Confidence was flagged per section: High for code-grounded facts and the recommended design; Medium where it depends on the not-yet-built `Hexalith.Projects` aggregate.

**Module-specific primary sources (this repo):** `Hexalith.Conversations` — `Contracts/Identifiers/ProjectId.cs`, `Contracts/Events/ConversationCreated.cs`, `State/ConversationState.cs`, `Contracts/Queries/ConversationListFilterV1.cs`, `Contracts/Queries/ConversationSummaryV1.cs`, `Contracts/Queries/ProjectReferenceHydrationV1.cs`, `Client/IConversationClient.cs`, `Server/Queries/ConversationQueryHandler.cs`, `Server/Hydration/IConversationReferenceHydrationDirectory.cs`; plus root and Conversations `_bmad-output/project-context.md`, and the `Hexalith.Projects` PRD/brief (2026-05-24).

**Web search queries used:** (1) event sourcing reference other aggregates by id across bounded contexts; (2) DDD store foreign aggregate id not entity reference / read-model hydration; (3) event sourcing aggregate unbounded-collection anti-pattern; (4) CQRS query-owner vs local read-model copy trade-off; (5) anti-corruption layer bounded-context integration; (6) testing idempotent event handlers / at-least-once / out-of-order.

**Generic-pattern sources (verified):**
- [Sapiensworks – Identifying Bounded Contexts & Aggregates](https://blog.sapiensworks.com/post/2014/10/31/DDD-Identifying-Bounded-Contexts-and-Aggregates-Entities-and-Value-Objects.aspx)
- [Context Mapper – Event Sourcing & CQRS Modeling](https://contextmapper.org/docs/event-sourcing-and-cqrs-modeling/)
- [SSENSE-TECH – Mastering Multi-Bounded-Context Integration](https://medium.com/ssense-tech/ddd-beyond-the-basics-mastering-multi-bounded-context-integration-ca0c7cec6561)
- [Ardalis – Aggregate Responsibility Design](https://ardalis.com/aggregate-responsibility-design/)
- [Khalil Stemmler – Handling Collections in Aggregates](https://khalilstemmler.com/articles/typescript-domain-driven-design/one-to-many-performance/)
- [Microsoft Learn – CQRS Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [microservices.io – CQRS](https://microservices.io/patterns/data/cqrs.html)
- [Microsoft Learn – Event Sourcing Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing)
- [Microsoft Learn – Anti-corruption Layer](https://learn.microsoft.com/en-us/azure/architecture/patterns/anti-corruption-layer)
- [AWS Prescriptive Guidance – ACL pattern](https://docs.aws.amazon.com/prescriptive-guidance/latest/cloud-design-patterns/acl.html)
- [DevIQ – Anti-Corruption Layer](https://deviq.com/domain-driven-design/anti-corruption-layer/)
- [Software Architecture Guild – Integration of Bounded Contexts](https://software-architecture-guild.com/guide/architecture/domains/integration-of-bounded-contexts/)
- [microservices.io – Idempotent consumer](https://microservices.io/post/microservices/patterns/2020/10/16/idempotent-consumer.html)
- [Domain Centric – ES projection deduplication](https://domaincentric.net/blog/event-sourcing-projection-patterns-deduplication-strategies)
- [OneUptime – Idempotent Dapr event handlers](https://oneuptime.com/blog/post/2026-03-31-dapr-idempotent-event-handlers/view)
- [Cockroach Labs – Idempotency & ordering in event-driven systems](https://www.cockroachlabs.com/blog/idempotency-and-ordering-in-event-driven-systems/)

**Limitations.** `Hexalith.Projects` has no code yet, so Projects-side type names are illustrative and the exact `ListConversationsQuery`/`ConversationPage` constructor shapes were inferred from usage in `ConversationQueryHandler`; confirm them against the source before coding.

---

## Research Synthesis — Conclusion

### Summary of Key Technical Findings

The conversation↔project relationship is already modeled, owned by Conversations, immutable, and discoverable by `ProjectId` through tenant-safe, fail-closed query infrastructure. The Projects side therefore needs only a thin, read-oriented integration: an Anti-Corruption Layer that queries Conversations by `ProjectId` and presents results in Projects' language with freshness preserved. The only build-time gap is a missing list method on the typed client.

### Strategic Technical Impact Assessment

This keeps the two bounded contexts cleanly decoupled: Projects gains a complete, authorized view of its conversations with no duplication and no new consistency burden, while Conversations remains the uncompromised owner of conversation identity, content, and governance. The design scales by paging/caching (Pattern A) and can be upgraded to a local projection (Pattern B) without changing the ownership model.

### Next Steps Technical Recommendations

1. **Confirm shapes** of `ListConversationsQuery`/`ConversationPage` in `Hexalith.Conversations` source.
2. **Implement Pattern A MVP**: identifier boundary → `ListConversationsAsync` (or read-API call) → `IProjectConversationDirectory` ACL → project view with `ProjectionTrustState`.
3. **Author the 5 ADRs** before coding load-bearing choices.
4. **Add Tier 1 tests** for the ACL translator and tenant-isolation negative paths; add Pattern B idempotency/ordering tests if/when that projection is built.
5. **Defer Pattern B/C** until a profiled need or a concrete project invariant appears.

---

**Technical Research Completion Date:** 2026-05-24
**Research Scope:** Projects → Conversations reference mechanics (focused integration how-to)
**Source Verification:** Module-specific claims cite repo file paths; generic patterns cite current public sources.
**Technical Confidence Level:** High for code-grounded facts and the recommended design; Medium for not-yet-built Projects type details.

_This document is an authoritative, codebase-grounded reference for wiring `Hexalith.Projects` to reference conversations owned by `Hexalith.Conversations`, and a basis for the module's integration ADRs and first implementation slice._
