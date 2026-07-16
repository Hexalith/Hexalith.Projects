---
stepsCompleted: [1, 2, 3, 4, 5, 6]
inputDocuments:
  - _bmad-output/planning-artifacts/ux-design-specification.md
  - _bmad-output/project-context.md
  - Hexalith.FrontComposer/_bmad-output/project-context.md
  - Hexalith.FrontComposer/docs/
workflowType: 'research'
status: 'complete'
lastStep: 6
research_type: 'technical'
research_topic: 'How to use Hexalith.FrontComposer to implement the Hexalith.Projects Web UX'
research_goals: 'Produce a concrete implementation playbook that maps each Hexalith.Projects UX view to FrontComposer projection/attribute definitions and customization-gradient levels, covering the full one-model-three-surfaces (Web + MCP + CLI) generation from shared projection contracts, including EventStore projection sourcing and tenant isolation. Repo-first sourcing, with web search used to validate .NET 10 / Blazor Auto / Fluent UI v5 RC / Roslyn incremental-generator facts.'
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

This report is an **implementation playbook** for building the Hexalith.Projects Web UX with Hexalith.FrontComposer. The Projects UX spec already mandates that the Web surface be *composed* through FrontComposer rather than hand-built, and frames the product as **one operational model exposed through three surfaces** (CLI, MCP, Web). This research confirms that FrontComposer is purpose-built for exactly that shape: an annotated **projection contract** is compiled by Roslyn incremental generators into Razor UI + Fluxor state + MCP descriptors + CLI evidence simultaneously, so cross-surface consistency is a build output, not a coordination burden.

Key findings: the `ProjectionRole` enum (`ActionQueue`, `StatusOverview`, `DetailRecord`, `Timeline`, `Dashboard`) maps almost 1:1 onto the spec's views; the four-level **customization gradient** (annotation → template → slot → full replacement) lets ~80% of views stay at Level 1 while reserving escape hatches for the resolution trace; the **five-state command lifecycle** structurally encodes persist-then-publish + SignalR-nudge→re-query; and tenant isolation, accessibility, and metadata-only redaction are framework contracts the generated output already honors. The single material risk is the **Fluent UI Blazor v5 RC** dependency (pinned, pre-GA), which the repo's pin-and-reuse posture correctly mitigates.

The full executive summary, key findings, recommendations, and a 7-phase implementation roadmap appear in the **Technical Research Synthesis** section below; the **Implementation Approaches** section carries the per-view mapping table and verified-signature code sketches.

---

<!-- Content will be appended sequentially through research workflow steps -->

## Technical Research Scope Confirmation

**Research Topic:** How to use Hexalith.FrontComposer to implement the Hexalith.Projects Web UX
**Research Goals:** Produce a concrete implementation playbook that maps each Hexalith.Projects UX view to FrontComposer projection/attribute definitions and customization-gradient levels, covering the full one-model-three-surfaces (Web + MCP + CLI) generation from shared projection contracts, including EventStore projection sourcing and tenant isolation.

**Technical Research Scope:**

- Architecture Analysis - FrontComposer parse→transform→emit model, attribute source-of-truth, 4-level customization gradient, Shell + Fluxor, Blazor Auto lifecycle
- Implementation Approaches - per-view mapping of UX-spec surfaces to projection types + attributes + gradient level, with code sketches and gotchas
- Technology Stack - .NET 10, Fluent UI Blazor v5 RC, Fluxor 6.9, Roslyn 4.12.0 incremental generators, MCP 1.2.0, CLI evidence tooling
- Integration Patterns - one model → three surfaces (Web + MCP + CLI), EventStore projection sourcing (REST + SignalR nudges), reason-code/state consistency
- Performance & Cross-Cutting - tenant isolation, accessibility contract, metadata-only redaction, incremental-generation discipline, Fluent UI RC / Blazor Auto risk

**Research Methodology:**

- Repo-first sourcing (FrontComposer docs, attribute contracts, Projects UX spec, project-context rules are authoritative)
- Web search to validate .NET 10 / Blazor Auto / Fluent UI v5 RC / Roslyn ISG framework facts
- Multi-source validation for critical technical claims
- Confidence level framework for uncertain information

**Scope Confirmed:** 2026-05-24

## Technology Stack Analysis

This section establishes the verified technology landscape that FrontComposer sits on. Versions are taken from the repo's authoritative sources (`global.json`, `Directory.Packages.props`, project-context rules) and **validated against current public documentation** as of May 2026. Confidence is **High** for repo-pinned facts (read directly from source) and noted per-item for web-sourced framework facts.

### Core Platform & Language — .NET 10 / C#

- **.NET SDK `10.0.302`** pinned via `global.json` with `rollForward: latestPatch`; primary projects target `net10.0`; Roslyn SourceTools target `netstandard2.0`; Contracts multi-target `net10.0;netstandard2.0`. (Confidence: High — repo source.)
- **C#** with nullable enabled, implicit usings, `TreatWarningsAsErrors=true`, `LangVersion=latest`. File-scoped namespaces, `_camelCase` private fields, `I`-prefixed interfaces, `Async` suffix. (Confidence: High — repo `Directory.Build.props` + `.editorconfig`.)
- .NET 10 is the current LTS line; Blazor's render-mode and prerender story is materially improved in this release (see UI layer below). (Confidence: High.)
- _Implication for Projects:_ Contracts that feed the generator must remain `netstandard2.0`-safe; any `net10.0`-only API in a projection contract must be guarded or isolated.
- _Source:_ https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0

### UI Framework Layer — Blazor Auto + Fluent UI Blazor v5 RC

- **Render model: Blazor Auto (Interactive Auto).** First visit renders on the **server** (fast first paint) while the WebAssembly runtime downloads in the background; subsequent visits run client-side in WASM. **Prerendering is on by default** for interactive components. (Confidence: High — Microsoft Learn.)
- **Lifecycle constraints that bind FrontComposer-generated components:** `OnAfterRender{Async}` does not fire during prerender; browser-only APIs (local/session storage) must not be touched during prerender. .NET 10 introduces the **`[PersistentState]`** attribute to carry data across prerender→hydration without flicker or refetch. This directly maps to the repo rule "do not access browser-only storage during prerender; use storage abstractions/test doubles." (Confidence: High.)
- **Fluent UI Blazor pinned to `5.0.0-rc.2-26098.1`.** Public timeline: **v5 RC1 released 2026-02-18**, rebuilt on **Fluent UI Web Components v3** (the same components behind Microsoft 365 / Teams / Windows 11), moving off the old FAST web components. **No GA date published** as of May 2026; v4 is supported until **November 2026**. This confirms the project-context rule that Fluent UI v5 APIs are **RC-sensitive, pinned, and high-risk** — generated/customized UI must use existing Shell + Fluent UI components and avoid casual upgrades. (Confidence: High for RC1 date and v3 foundation; Medium for GA timing — not yet announced.)
- _Implication for Projects:_ Every generated view (lists, detail, badges) renders through the Fluent UI v5 RC component set under Blazor Auto. The accessibility contract (labels, keyboard, focus, live-region, reduced-motion, forced-colors) is part of the framework contract and must survive customization.
- _Sources:_ https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0 · https://learn.microsoft.com/en-us/aspnet/core/blazor/components/prerender?view=aspnetcore-10.0 · https://baaijte.net/blog/microsoft-fluentui-aspnetcore.components-50-rc1/ · https://github.com/microsoft/fluentui-blazor/releases

### State Management — Fluxor 6.9

- **`Fluxor.Blazor.Web` pinned to `6.9.0`.** FrontComposer **generates** Fluxor actions/state/reducers/effects that must align with the Shell's reducer and feature conventions (feature folders under `src/Hexalith.FrontComposer.Shell/State`). (Confidence: High — repo source.)
- Architectural rule: **SourceTools must not take a compile dependency on Fluxor** — it emits Fluxor code as strings / fully-qualified names. Generated components follow an explicit subscribe/dispose (`IState<T>` / `FluxorComponent`) pattern. (Confidence: High — repo project-context.)
- _Implication for Projects:_ The Projects operational state (current project list filter, selected project, resolution-trace request, in-flight maintenance action) flows through generated Fluxor features, not ad-hoc component state.

### Code-Generation Engine — Roslyn Incremental Source Generators

- **`Microsoft.CodeAnalysis.CSharp` pinned to exactly `4.12.0`**; SourceTools are `IsRoslynComponent=true`, `netstandard2.0`, analyzer deps `PrivateAssets="all"`. (Confidence: High — repo source.)
- **Discovery uses `ForAttributeWithMetadataName`** — validated as the correct high-level API: **~99× more efficient** than `CreateSyntaxProvider`, provides `GeneratorAttributeSyntaxContext` (TargetNode + TargetSymbol + matched Attributes) so the semantic model isn't manually queried, and requires CodeAnalysis **≥ 4.4.0** (repo's 4.12.0 satisfies this). (Confidence: High — Roslyn cookbook + Thinktecture/Andrew Lock.)
- **Incremental-correctness rules that the playbook must respect:** use `record` models with value equality / `IEquatable<T>`; **never capture `INamedTypeSymbol`/`SyntaxNode`** in the pipeline data model (kills incrementality); register marker attributes via `RegisterPostInitializationOutput`. The repo enforces the parse→transform→emit split and "emit output or an explicit `HFC` diagnostic — never silently skip an input." (Confidence: High — web best practices corroborate repo rules exactly.)
- _Implication for Projects:_ Projects' projection contracts are the generator inputs; transforms stay pure and snapshot-testable (Verify.XunitV3), and generated `.g.cs` is never hand-edited.
- _Sources:_ https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.cookbook.md · https://andrewlock.net/creating-a-source-generator-part-9-avoiding-performance-pitfalls-in-incremental-generators/ · https://www.thinktecture.com/en/net-core/roslyn-source-generators-high-level-api-forattributewithmetadataname/

### Surface & Integration Stack — MCP C# SDK, CLI, EventStore transport

- **MCP: `ModelContextProtocol.AspNetCore` pinned to `1.2.0`.** Public timeline: the **MCP C# SDK reached v1.0 on 2026-03-05** (current NuGet `1.3.0`), shipping as two packages — `ModelContextProtocol` (core) + `ModelContextProtocol.AspNetCore` (HTTP hosting, `MapMcp` endpoint routing). Repo's 1.2.0 is a maintained post-1.0 line; an upgrade to 1.3.0 is plausible but governed by the "don't casually upgrade" rule. (Confidence: High for SDK timeline; Medium on whether 1.2.0→1.3.0 is breaking — not yet verified.)
- Repo rule: **MCP tools/resources must be generated or registry-backed from typed descriptors**, reject unknown tools at the contract boundary with suggestions, and stay tenant-aware. MCP is a translation layer — no domain logic in tool code.
- **CLI** (`Hexalith.FrontComposer.Cli`) emits **sanitized evidence/diagnostics** (no absolute paths, payloads, tokens, tenant/user identifiers, or unbounded logs).
- **EventStore transport has two channels: REST** (commands + queries) **and SignalR** (projection-change *nudges* only). A nudge triggers a re-query with proper tenant/user/cache context; SignalR payloads are **never** treated as source-of-truth projection data. (Confidence: High — repo project-context.)
- _Sources:_ https://github.com/modelcontextprotocol/csharp-sdk · https://www.nuget.org/packages/ModelContextProtocol/ · https://devblogs.microsoft.com/dotnet/build-a-model-context-protocol-mcp-server-in-csharp/

### Data Sourcing & Infrastructure — EventStore projections + Dapr

- The Projects read models that feed FrontComposer projections are produced by **EventStore projection infrastructure** (`CachingProjectionActor`, ETag actors, projection notifiers). Identity is canonical `{tenant}:{domain}:{aggregateId}`; **Dapr is the only permitted infrastructure abstraction** — no direct Redis/Postgres/Cosmos/broker clients in contracts/client/domain or framework code. (Confidence: High — repo project-context.)
- **Aspire** (`Aspire.Hosting` 13.2.x, `CommunityToolkit.Aspire.Hosting.Dapr` 13.0.0) owns local topology for samples and runtime validation. (Confidence: High — repo source.)
- _Implication for Projects:_ FrontComposer never reaches infrastructure directly; it queries the Projects read API over REST and re-queries on SignalR nudges. Tenant isolation is enforced end-to-end.

### Technology Adoption Trends & Risk Notes

- **Migration / direction:** Fluent UI Blazor's move to Web Components v3 (RC, pre-GA) is the single largest external-risk vector — API churn until GA is expected; the repo's pinning + "use existing Shell components first" posture is the correct mitigation. (Confidence: Medium — based on RC status.)
- **Emerging:** MCP reaching v1.0 (with auth-server discovery, tool sampling, long-running requests) signals the agent-facing surface is stabilizing — favorable for the "one model → three surfaces" goal. (Confidence: High.)
- **Stable foundations:** Roslyn incremental generators and .NET 10 Blazor Auto are mature/GA; risk there is *discipline* (incremental correctness, prerender safety), not platform volatility. (Confidence: High.)
- **Pinned/high-risk set the playbook must not casually bump:** Fluent UI, Fluxor, Roslyn, MCP, xUnit generation, Playwright, .NET SDK. (Confidence: High — repo project-context.)

## Integration Patterns Analysis

For this topic, "integration" is the **one operational model → three surfaces** principle the UX spec mandates. The integration contracts are real and read from `Hexalith.FrontComposer.Contracts/Communication` and `…/Mcp`. Generic protocol facts are validated against current standards; FrontComposer-specific mechanics are repo-grounded (Confidence: High).

### The Core Integration Pattern — One Projection Contract → Three Generated Surfaces

FrontComposer's central integration idea: an annotated **projection contract** is the single source of truth, and the parse→transform→emit pipeline projects it onto three surfaces simultaneously:

- **Web** — generated Razor view + Fluxor feature (list/detail/badges), rendered through the Shell.
- **MCP** — a generated, SDK-neutral `McpManifest` of **command tool descriptors** (`McpCommandDescriptor`) and **projection resource descriptors** (`McpResourceDescriptor`), with a structural `SchemaFingerprint`.
- **CLI** — `frontcomposer inspect`/`migrate` evidence over the same generated metadata, with a JSON contract for automation.

This is what makes "three adapters over one diagnostic model" achievable for Projects: the lifecycle states, reason codes, badges, and field semantics are declared **once** on the projection contract and flow to every surface. (Confidence: High — repo `McpManifest.cs`, `GeneratedManifestAttribute`, customization-gradient + source-generation docs.)

### Web Surface Integration — REST query + SignalR nudge → re-query

- **Query channel (REST):** `IQueryService.QueryAsync<T>(QueryRequest, ct)` returns `QueryResult<T>` carrying **items, total count, and an ETag**. Generated Fluxor effects call this; the ETag supports optimistic concurrency / conditional refresh. (Confidence: High — `IQueryService.cs`, `QueryResult.cs`.)
- **Change channel (SignalR):** `IProjectionChangeNotifier` raises `ProjectionChanged(projectionType)`; the tenant-aware `IProjectionChangeNotifierWithTenant` raises `ProjectionChangedForTenant(projectionType, tenantId)` so a single circuit observing multiple tenants routes nudges correctly. A nudge is a **signal to re-query**, never projection data. (Confidence: High — `IProjectionChangeNotifier.cs`.)
- **Why this shape:** it is the textbook **CQRS read-model + eventual-consistency** pattern — notify-then-requery rather than pushing read-model state over the socket — with ETag-based optimistic concurrency to reconcile races. (Confidence: High — corroborated by CQRS materialized-view sources.)
- _Implication for Projects:_ "Replay a resolution trace" or "refresh the project list after a maintenance action" are re-queries triggered by either user action or a tenant-scoped nudge — generated effects handle both; components must tolerate the eventual-consistency window.
- _Sources:_ https://dev.to/xuan_56087d315ff4f52254e6/complex-query-handling-in-cqrs-minimizing-roundtrips-and-latency-with-projection-materialization-494n · https://clouddc.substack.com/p/day-11-cqrs-consistency-handling

### Command / Mutation Integration — guided maintenance actions

- **Command channel (REST):** `ICommandService` (+ `ICommandServiceWithLifecycle`) submits commands and returns `CommandResult`. The contract models **distinct outcomes as typed exceptions/payloads**: `CommandRejectedException`, `CommandValidationException` (with field allowlist via `ICommandValidationFieldAllowlist`), `CommandWarningException` (+ `CommandWarningKind`), and `AuthRedirectRequiredException` (+ `IAuthRedirector`). Failures carry `ProblemDetailsPayload`. (Confidence: High — `Communication/` contracts.)
- **Error standard:** `ProblemDetailsPayload` follows **RFC 9457**, which obsoletes RFC 7807 and is fully backward-compatible — matching the project-context rule to use "ProblemDetails / RFC 7807 or RFC 9457 with correlation/tenant context." (Confidence: High.)
- _Implication for Projects:_ "Guided maintenance actions" (the UX spec's mutating surface) map to `[Command]`/`[Destructive]`-annotated commands. The **five-state command lifecycle** and bounded diagnostics must be preserved; a destructive action surfaces impact + tenant scope + audit consequence before executing, and a rejection is a domain outcome, not an exception path.
- _Source:_ https://www.rfc-editor.org/rfc/rfc9457.html · https://www.milanjovanovic.tech/blog/problem-details-for-aspnetcore-apis

### MCP Surface Integration — agent-assisted diagnostics

- **Generated manifest:** `McpManifest(SchemaVersion, Commands[], Resources[], Fingerprint)` is emitted by SourceTools — **SDK-neutral** descriptors, not hand-written MCP tool code. `McpProjectionRenderStrategy` controls how a projection is rendered into an MCP resource. (Confidence: High — `Mcp/` contracts.)
- **Contract-boundary safety:** MCP tools/resources must be **generated or registry-backed from typed descriptors**, reject unknown tool names at the boundary (with suggestions), and stay tenant-aware. MCP is a translation layer — no domain logic in tool code. (Confidence: High — repo project-context.)
- **SDK landing:** the MCP C# SDK exposes HTTP hosting via `ModelContextProtocol.AspNetCore`'s `MapMcp`; read-only **resources** are separated from mutating **tools** (matching the UX spec's "read-only resources separated from mutating tools"). (Confidence: High — MCP SDK docs.)
- _Implication for Projects:_ project metadata, reference health, and resolution evidence become MCP **resources** (read-only); safe maintenance becomes MCP **tools** (mutating), all derived from the same projection/command contracts as the Web views.
- _Source:_ https://github.com/modelcontextprotocol/csharp-sdk · https://devblogs.microsoft.com/dotnet/build-a-model-context-protocol-mcp-server-in-csharp/

### CLI Surface Integration — scriptable evidence

- `frontcomposer inspect` reports generated-output paths, projection metadata, **schema fingerprints**, and diagnostics, with machine-readable JSON for automation; `migrate` supports dry-run/apply with bounded, project-relative edits. (Confidence: High — `reference/cli.md`.)
- **Deterministic automation contract:** exit codes are semantic (0 success; 1 fail-flag-promoted findings; 2 invalid input; 3 generated output unavailable; 4 filesystem/apply failure), and **all output is redacted** (project-relative forward-slash paths; usernames/tenant IDs/secrets/absolute paths/raw exceptions stripped). (Confidence: High.)
- _Implication for Projects:_ the CLI gives operators/support the same project-state diagnostics as Web/MCP in a scriptable, redaction-safe form — the third adapter over the one model.

### Cross-Surface Consistency Contract

The integration only holds if the three surfaces stay semantically identical. The shared invariants the generator enforces:

- **Lifecycle/reference states** (`included`, `excluded`, `unauthorized`, `unavailable`, `stale`, `archived`, `ambiguous`, `tenant_mismatch`, `conflict`, `invalidReference`) declared once and rendered as state badges on Web, fields on MCP resources, and columns on CLI.
- **Reason codes, timestamps, audit identifiers, warnings, redaction behavior** identical across surfaces.
- **`SchemaFingerprint`** ties MCP descriptors and generated output to a structural version — changing contracts requires updating compatibility tests + downstream consumers (no silent drift). (Confidence: High — repo project-context + UX spec "one operational model, three surfaces.")

### Integration Security & Tenant Isolation

- **Two enforced layers:** API/JWT authorization **and** query-side result filtering. JWT checks alone are insufficient for user-search-style queries; row-level/result filtering is required. (Confidence: High — repo project-context.)
- **Tenant context flows through everything:** commands, queries, SignalR subscriptions (tenant-scoped nudges), MCP visibility, cache keys, and CLI evidence. Cross-tenant visibility is a security bug, not a UI detail. (Confidence: High.)
- **Auth handoff:** `AuthRedirectRequiredException` + `IAuthRedirector` integrate the Blazor Auto auth flow into command submission without leaking state. (Confidence: High — repo contract.)

### Data Sourcing Integration — EventStore projections

- Read models are produced by EventStore projection infrastructure (`CachingProjectionActor`, ETag actors, projection notifiers) under canonical identity `{tenant}:{domain}:{aggregateId}`; FrontComposer consumes them only via REST query + SignalR nudge. **Persist-then-publish** guarantees the nudge never precedes durable state. (Confidence: High — repo project-context.)
- Eventual consistency is explicit: a nudge means "re-query," and the ETag reconciles concurrent updates — the playbook must design Projects views to tolerate the brief stale window rather than assume read-after-write. (Confidence: High — CQRS sources corroborate.)

## Architectural Patterns and Design

FrontComposer is an opinionated **contract-first UI compiler**. Seven patterns define how the Projects Web UX gets built; each is read from the repo and cross-checked against current design-pattern literature.

### Pattern 1 — Contract-First / Model-Driven Generation (attributes are the single source of truth)

- A `[Projection]`-annotated **partial class** is compiled (not reflected) into Razor DataGrid UI + Fluxor state + MCP metadata + domain registration. Display metadata reuses the BCL `[Display]` attribute (Name/Description/Order) read via Roslyn symbol analysis — no custom display attribute. (Confidence: High — `ProjectionAttribute.cs`, `IdeParityCounterProjection.cs`.)
- **Why compile-time, not reflection:** source generation gives zero-startup-overhead registration (a single `RegisterDomain()` call), trimming/AOT friendliness, and IDE navigation into generated sources — exactly the tradeoff the literature recommends over runtime reflection, and the explicit repo rule ("do not use runtime reflection as the primary discovery mechanism when compile-time descriptors exist"). (Confidence: High.)
- _Projects implication:_ the entire operational console is *declared*, not hand-built. Adding a view = adding an annotated projection contract; the surfaces follow.
- _Sources:_ https://www.devleader.ca/2026/02/07/source-generation-vs-reflection-in-needlr-choosing-the-right-approach · https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/reflection-vs-source-generation

### Pattern 2 — The Customization Gradient (progressive escape-hatch) — the central design decision

The single most important architectural choice per view: **use the lowest gradient level that solves the problem.**

| Level | Mechanism | Use when | Preserves |
| --- | --- | --- | --- |
| 1. Annotation | `[RelativeTime]`, `[Currency]`, `[Icon]`, `[ProjectionBadge]`, `[ColumnPriority]`, `[ProjectionFieldGroup]`, `[ProjectionRole]`… | Generated renderer already does what you need | Lifecycle wrapper, a11y contract, metadata, hot reload, diagnostics, generated tests |
| 2. Typed Razor template | `[ProjectionTemplate(typeof(P))]` + `ProjectionTemplateContext<P>` | Rearrange layout/rows but keep field rendering framework-owned | Lifecycle wrapper, field diagnostics, metadata, default field rendering |
| 3. Typed slot | `FieldSlotContext<P,TField>` + `ProjectionSlotDescriptor` | One field needs custom rendering | View shell, lifecycle wrapper, field identity, registry diagnostics, default fallback |
| 4. Full replacement | `ProjectionViewContext<P>` + `ProjectionViewOverrideDescriptor` | The whole projection body is the wrong shape | Shell, lifecycle boundary, authorization, telemetry, override diagnostics |

- This is the textbook **escape-hatch / progressive-customization pattern** — let adopters reach external complexity for the tricky 5% without complicating the other 95%; Fluent UI's own slots API is built on the same principle. (Confidence: High — `customization-gradient-cookbook.md` + escape-hatch literature.)
- Overrides are registered via `IOverrideRegistry.Register(projectionType, overrideType ["slot"|"view"], implementationType)` and three rendering registries: `IProjectionTemplateRegistry`, `IProjectionSlotRegistry`, `IProjectionViewOverrideRegistry`. Each customization carries a **contract version** (`ProjectionSlotContractVersion`, `ProjectionTemplateContractVersion`, `ProjectionViewOverrideContractVersion`) so overrides survive generator evolution. (Confidence: High — `Rendering/` contracts.)
- _Projects implication:_ most views should sit at **Level 1–2**; the **resolution trace** (bespoke evidence layout) is the most likely Level 3/4 candidate. Dropping to Level 4 forfeits generated field rendering, so it must be justified.
- _Sources:_ https://react.dev/learn/escape-hatches · https://dev.to/paulgildea/using-slots-with-fluent-ui-react-v9-jf1

### Pattern 3 — ProjectionRole-Driven Rendering Strategy (capped at 5 roles)

`ProjectionRole` (deliberately capped at five to force focused modeling) selects the default rendering strategy and maps almost 1:1 onto the Projects UX surfaces:

| `ProjectionRole` | Generated intent | Projects UX view (from UX spec) |
| --- | --- | --- |
| `ActionQueue` | Pending items needing intervention | **Warnings** / guided-maintenance queue |
| `StatusOverview` | High-level status tile | Project **status badges / overview tiles** |
| `DetailRecord` | Single-record detail | **Project detail** + reference inventory |
| `Timeline` | Chronological events | **Audit timeline** |
| `Dashboard` | Aggregated metrics | Cross-project **operational dashboard** |

- The project **list** itself is the default DataGrid projection (filterable, virtualizable — see `FilterActions`, `VirtualizationActions`, `ExpandedRowActions`, `DensityLevel`). (Confidence: High — `ProjectionRole.cs` + `Rendering/` contracts; mapping is my analysis, Confidence: Medium-High pending validation against generated output.)

### Pattern 4 — Registry-Based Shell Composition (compile-time, companion opt-in)

- The generator emits `RegisterDomain(DomainManifest)` against `IFrontComposerRegistry`; the Shell aggregates manifests, builds nav groups (`AddNavGroup(name, boundedContext)`), and composes views. (Confidence: High — `IFrontComposerRegistry.cs`.)
- **Route reachability** uses a companion-interface opt-in: implement `IFrontComposerFullPageRouteRegistry.HasFullPageRoute(commandType)` so the command palette hides unreachable commands; non-implementers get a permissive `true` fallback, and a build-time analyzer (Story 9-4) enforces companion implementation for any registry that registers commands. (Confidence: High — repo contract + remarks.)
- _Projects implication:_ the Projects domain assembly exposes one generated registration entry point; the Shell wires Projects nav, routes, and palette without bespoke glue.

### Pattern 5 — Lifecycle-Wrapper / Five-State Command Architecture

- Every generated view sits inside a **lifecycle wrapper** owning loading/empty/error states, authorization boundary, telemetry context, density, and disposal — preserved across all four gradient levels.
- Commands follow `CommandLifecycleState`: `Idle → Submitting → Acknowledged (EventStore 202) → Syncing (projection update detected) → Confirmed / Rejected`. This is the architectural embodiment of persist-then-publish + nudge→requery: `Acknowledged` = command accepted, `Syncing` = SignalR nudge arrived, `Confirmed` = re-query reflected the change in the UI. `McpLifecycleStateNames` shares these names with the MCP surface — cross-surface consistency is structural, not convention. (Confidence: High — `CommandLifecycleState.cs`, `Lifecycle/` contracts.)
- _Projects implication:_ guided maintenance actions get this five-state feedback for free (with bounded diagnostics on `Rejected`), and the same state vocabulary appears in MCP tool responses.

### Pattern 6 — Blazor Auto Component Architecture (prerender-safe by construction)

- Generated components must tolerate prerender → Server circuit → WASM handoff and reconnect. Browser-only storage is never touched during prerender (use the storage abstractions); `OnAfterRender` is not called while prerendering; `[PersistentState]` carries data across the prerender→hydration boundary without flicker/refetch. (Confidence: High — Microsoft Learn + repo project-context.)
- _Projects implication:_ first paint of the project list/detail renders server-side (fast, works before WASM downloads); generated effects re-query after hydration. The playbook must verify generated components don't assume client-only APIs at first render.
- _Source:_ https://learn.microsoft.com/en-us/aspnet/core/blazor/components/prerender?view=aspnetcore-10.0

### Pattern 7 — Accessibility-as-Contract

- Labels, keyboard reachability, focus visibility, live-region parity, reduced-motion, and forced-colors behavior are **part of the framework contract**, verified with `@axe-core/playwright`. Customizations (Levels 2–4) must not normalize these away. (Confidence: High — repo project-context + accessibility-verification docs.)
- _Projects implication:_ state badges and reason codes must remain screen-reader-legible (not color-only); the resolution trace must stay keyboard-navigable even if rendered at Level 3/4.

### Security & Data Architecture (summary)

- Tenant isolation, two-layer authorization (JWT + query-side filtering), metadata-only redaction, and canonical `{tenant}:{domain}:{aggregateId}` identity are architectural requirements, not view options (detailed in Integration Patterns above). FrontComposer never reaches infrastructure directly; Dapr/EventStore sit behind the REST/SignalR boundary. (Confidence: High — repo project-context.)

### Architectural Decision Summary for Projects

1. Declare each operational view as a `[Projection]` contract with an explicit `ProjectionRole`; reuse `[Display]` for labels.
2. Default to gradient **Level 1**; escalate only where a specific view demands it (resolution trace is the prime Level 3/4 candidate).
3. Model maintenance as `[Command]`/`[Destructive]`; rely on the five-state lifecycle and `ProblemDetailsPayload` for feedback.
4. Let the generator own Shell registration, nav, routing, palette reachability, MCP descriptors, and CLI evidence — one contract, three surfaces.
5. Treat Fluent UI v5 RC, prerender safety, accessibility, and tenant isolation as fixed constraints the generated output already honors — don't re-implement them at the view level.

## Implementation Approaches and Technology Adoption

This is the actionable playbook: how to implement each Hexalith.Projects Web UX view with FrontComposer, plus the workflow, testing, and rollout strategy. Code is illustrative and uses **verified attribute signatures** read from `Hexalith.FrontComposer.Contracts`; field names must be reconciled with the actual EventStore Projects read models.

### Adoption Strategy — Contract-First, Lowest-Gradient-First, Surface-Parity-By-Construction

- **Declare, don't build.** Each view begins as a `[Projection]` (or `[Command]`) contract in the Projects domain assembly. Web + MCP + CLI are emitted from it — surface parity is a build output, not a coordination task.
- **Start at gradient Level 1** for every view; escalate per-view only when a concrete rendering need forces it. This keeps lifecycle, accessibility, diagnostics, MCP schema, and generated tests authoritative.
- **One shared vocabulary.** Define the UX spec's reference/lifecycle states as **shared enums with `[ProjectionBadge]`** so the same state→color mapping flows to Web badges, MCP resource fields, and CLI columns.
- _Source (compile-time vs reflection rationale):_ https://www.devleader.ca/2026/02/07/source-generation-vs-reflection-in-needlr-choosing-the-right-approach

### Development Workflow & Tooling — the build/inspect/test inner loop

1. Add the FrontComposer packages to the Projects host + domain projects.
2. Define/annotate the projection or command contract.
3. **Build** so the generator emits Razor + Fluxor + MCP metadata + registration under the generated-output path (`obj/.../generated/HexalithFrontComposer`). Keep generated files read-only.
4. **`frontcomposer inspect --format json`** to verify generated-output paths, projection metadata, schema fingerprints, and diagnostics; wire `--fail-on-warning` into CI for fail-closed gates.
5. Iterate the contract, rebuild; **never hand-edit `.g.cs`** — a skipped input must surface an `HFC` diagnostic.
6. Main-lane test parity: `dotnet test Hexalith.FrontComposer.slnx --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`.
- _Source:_ https://learn.microsoft.com/en-us/aspnet/core/blazor/test?view=aspnetcore-10.0

### Per-View Implementation Playbook (the core mapping)

| UX-spec view | Contract + `ProjectionRole` | Key attributes | Gradient level | Surfaces emitted |
| --- | --- | --- | --- | --- |
| **Project list** | `ProjectSummaryProjection` (default DataGrid) | `[ColumnPriority]`, `[ProjectionBadge]` (lifecycle enum), `[RelativeTime]`, `[ProjectionEmptyStateCta]` | **L1** | Web grid, MCP resource, CLI list |
| **Project detail** | `ProjectDetailProjection` / `DetailRecord` | `[ProjectionFieldGroup]`, `[ProjectionBadge]`, `[Display]` | **L1–L2** (L2 to group sections) | Web detail, MCP resource, CLI |
| **Reference inventory** | `ProjectReferenceProjection` / `DetailRecord` | `[ProjectionBadge]` (reference-state enum), `[ColumnPriority]` | **L1** | Web sub-grid, MCP resource |
| **Resolution trace** | `ResolutionTraceProjection` / `DetailRecord` | bespoke evidence layout | **L3 slot / L4 view** | Web (custom), MCP resource |
| **Audit timeline** | `ProjectAuditProjection` / `Timeline` | `[RelativeTime]`, `[Display(Order=…)]` | **L1** | Web timeline, MCP resource, CLI |
| **Warnings / maintenance queue** | `ProjectWarningProjection` / `ActionQueue` `WhenState="…"` | `[ProjectionBadge]`, `[ProjectionEmptyStateCta]` | **L1** | Web queue, MCP resource |
| **Operational dashboard** | `ProjectHealthProjection` / `Dashboard` or `StatusOverview` | `[ProjectionBadge]`, `[DerivedFrom]` | **L1–L2** | Web tiles, MCP resource |
| **Guided maintenance action** | `ArchiveProject` etc. `[Command]` | `[Destructive]`, `[RequiresPolicy]`, `[Icon]` | n/a (command) | Web form + MCP tool + lifecycle |

#### Step 1 — Shared state enums with badges (the consistency anchor)

```csharp
using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.Projects.Contracts.Ui;

// One enum, one color mapping → Web badges + MCP fields + CLI columns stay identical.
public enum ReferenceState
{
    [ProjectionBadge(BadgeSlot.Success)] Included,
    [ProjectionBadge(BadgeSlot.Neutral)] Excluded,
    [ProjectionBadge(BadgeSlot.Danger)]  Unauthorized,
    [ProjectionBadge(BadgeSlot.Warning)] Unavailable,
    [ProjectionBadge(BadgeSlot.Warning)] Stale,
    [ProjectionBadge(BadgeSlot.Neutral)] Archived,
    [ProjectionBadge(BadgeSlot.Warning)] Ambiguous,
    [ProjectionBadge(BadgeSlot.Danger)]  TenantMismatch,
    [ProjectionBadge(BadgeSlot.Danger)]  Conflict,
    [ProjectionBadge(BadgeSlot.Danger)]  InvalidReference,
}
```

> Gotcha: `[ProjectionBadge]` is a **field-level** attribute applied to enum members (verified `BadgeSlot` slots: `Neutral, Info, Success, Warning, Danger, Accent`). Badges must stay screen-reader-legible — never color-only (accessibility contract).

#### Step 2 — Project list (default DataGrid, Level 1)

```csharp
[BoundedContext("Projects", DisplayLabel = "Projects")]
[Projection]
[ProjectionEmptyStateCta("Hexalith.Projects.Contracts.Commands.RegisterProject")]
public partial class ProjectSummaryProjection
{
    // Author obligation: ItemKey (Id/AggregateId/Key) must be non-null for every visible row.
    [ColumnPriority(int.MinValue)]               // pin identity column to front
    public string Id { get; set; } = string.Empty;

    [ColumnPriority(0)]
    [Display(Name = "Project")]
    public string DisplayName { get; set; } = string.Empty;

    [ColumnPriority(1)]
    public ProjectLifecycleState Lifecycle { get; set; }   // enum → colored badge

    [ColumnPriority(2)]
    [RelativeTime(relativeWindowDays: 30)]
    public DateTimeOffset UpdatedAt { get; set; }
}
```

> Gotchas: declaring **>15 columns** triggers `FcColumnPrioritizer` (UX-DR63); priority collisions emit `HFC1028`. Under **FluentDataGrid virtualization, column-reset is unavailable** (can't enumerate all rows) — a known Fluent UI v5 limitation to design around.
> _Source:_ https://deepwiki.com/microsoft/fluentui-blazor/5.1-datagrid-component · https://www.fluentui-blazor.net/datagrid-virtualize

#### Step 3 — Warnings / maintenance queue (`ActionQueue` with state filter, Level 1)

```csharp
[BoundedContext("Projects")]
[Projection]
[ProjectionRole(ProjectionRole.ActionQueue, WhenState = "Stale,Conflict,InvalidReference")]
public partial class ProjectWarningProjection
{
    [ColumnPriority(int.MinValue)] public string Id { get; set; } = string.Empty;
    [ColumnPriority(0)] public string ProjectId { get; set; } = string.Empty;
    [ColumnPriority(1)] public ReferenceState State { get; set; }   // drives WhenState filter + badge
    [ColumnPriority(2)] public string ReasonCode { get; set; } = string.Empty;
}
```

> Gotcha: `WhenState` is a **case-sensitive CSV matched against enum member names**; unknown members emit `HFC1022` and become always-no-match. Keep it in sync with the enum.

#### Step 4 — Audit timeline (`Timeline`, Level 1)

```csharp
[BoundedContext("Projects")]
[Projection]
[ProjectionRole(ProjectionRole.Timeline)]
public partial class ProjectAuditProjection
{
    [ColumnPriority(int.MinValue)] public string AuditId { get; set; } = string.Empty;
    [Display(Name = "When", Order = 0)] [RelativeTime] public DateTimeOffset OccurredAt { get; set; }
    [Display(Name = "Action", Order = 1)] public string ActionCode { get; set; } = string.Empty;
}
```

#### Step 5 — Guided maintenance action (`[Command]` + destructive + policy)

```csharp
[Command]
[BoundedContext("Projects")]
[RequiresPolicy("projects.maintenance.archive")]      // host owns policy registration
[Icon("Regular.Size20.Archive")]                       // Fluent UI v5 icon contract fragment
[Destructive(ConfirmationTitle = "Archive project?",
             ConfirmationBody  = "The project becomes read-only. This is audited and reversible by an operator.")]
public sealed class ArchiveProject
{
    public string MessageId { get; set; } = string.Empty;  // required dispatch id
    public string ProjectId { get; set; } = string.Empty;
}
```

> This emits a Web form + renderer + **MCP tool** + lifecycle bridge. `[Destructive]` injects a **pre-submit** confirmation (the `FcLifecycleWrapper` is post-submit only). Rejections come back as the `Rejected` lifecycle state with `ProblemDetailsPayload`, not exceptions. Name heuristics (`Archive*`/`Delete*`) only raise advisory `HFC1020` — `[Destructive]` is the authoritative signal.

#### Step 6 — Resolution trace (the Level 3/4 candidate)

The resolution trace is the one view whose layout the generated DataGrid/detail body likely can't express. Approach in order:
- **Try Level 2** first (`[ProjectionTemplate(typeof(ResolutionTraceProjection))]` + `ProjectionTemplateContext<T>`) to restructure the layout while still calling `Context.FieldRenderer(...)` per field — keeps field rendering, badges, and a11y framework-owned.
- **Drop to Level 3** (`FieldSlotContext<T,TField>` + `ProjectionSlotDescriptor`) if only the candidate-comparison field needs bespoke rendering.
- **Reserve Level 4** (`ProjectionViewContext<T>` + `ProjectionViewOverrideDescriptor`) for a full custom body — you forfeit generated field rendering but keep shell, lifecycle, authorization, telemetry, and override diagnostics. Register via `IProjectionViewOverrideRegistry` and stamp the `ProjectionViewOverrideContractVersion`.

### Testing & Quality Assurance

- **Tier 1 (pure, fast):** generator parse/transform/emit units; assert transform models directly; **Verify.XunitV3 snapshots** for generated Razor/Fluxor/MCP output (use `Verify.SourceGenerators` — it handles multi-file output + diagnostics for incremental generators). No Dapr/Aspire/network/browser.
- **Component:** **bUnit** with the project's storage/service doubles (never real browser storage or prior-test state); optionally `Verify.Bunit` for rendered-markup snapshots.
- **E2E:** Playwright under `tests/e2e` (Node ≥24), selectors via `data-testid`/role/label only (never CSS/text), wait on observable state (no `waitForTimeout`); accessibility via `@axe-core/playwright` with the full violation set.
- **Negative paths required:** tenant isolation (cross-tenant must be invisible), query-side authorization, redaction (no payloads/PII/tokens/tenant IDs in any surface or evidence).
- _Sources:_ https://andrewlock.net/creating-a-source-generator-part-2-testing-an-incremental-generator-with-snapshot-testing/ · https://github.com/VerifyTests/Verify.Bunit · https://bunit.dev/

### Deployment & Operations

- Local topology and runtime validation via **Aspire AppHost** (align with existing `ServiceDefaults`); the Projects host exposes REST (query/command) + SignalR (nudges) + `MapMcp` MCP endpoints. The CLI ships as the third adapter for scripted operations/support.
- CI gates: `frontcomposer inspect --fail-on-warning`, the main-lane filtered `dotnet test`, and the release evidence chain (package inventory, SBOM/checksums/signatures, schema-fingerprint compatibility) stay synchronized.

### Risk Assessment & Mitigation

| Risk | Severity | Mitigation |
| --- | --- | --- |
| **Fluent UI v5 RC API churn** (no GA; RC2 pinned) | High | Pin version; use Shell + existing components only; isolate any Fluent UI surface; track GA before upgrading |
| Contract/fingerprint drift breaking surfaces | Med-High | Schema-fingerprint compatibility tests; additive, serialization-tolerant changes; no `V2` types |
| Dropping to Level 4 too eagerly (loses generated field rendering/a11y) | Med | Enforce lowest-gradient-first; justify L4 in review; keep contract versions |
| Blazor Auto prerender bugs (client-only API at first render) | Med | Use storage abstractions; `[PersistentState]`; bUnit prerender doubles |
| Eventual-consistency confusion (read-after-write expectation) | Med | Design views for the nudge→requery window; surface `Syncing` lifecycle state |
| Tenant leakage across surfaces | High | Two-layer auth (JWT + query-side filter); tenant-scoped SignalR nudges; negative-path tests |
| Incremental-generator perf regression (captured symbols) | Med | `record` models, `IEquatable<T>`, never capture `ISymbol`/`SyntaxNode`; `ForAttributeWithMetadataName` |

## Technical Research Recommendations

### Implementation Roadmap

1. **Phase 0 — Foundation:** add FrontComposer packages to Projects host/domain; confirm `deps.local.props` resolves the root `Hexalith.EventStore`; stand up Aspire AppHost wiring (REST + SignalR + `MapMcp`).
2. **Phase 1 — Shared vocabulary:** define `ReferenceState` / `ProjectLifecycleState` enums with `[ProjectionBadge]`; lock reason-code list. (Unblocks every view + surface consistency.)
3. **Phase 2 — Read surfaces (Level 1):** `ProjectSummaryProjection` (list), `ProjectDetailProjection` (detail), `ProjectReferenceProjection` (reference inventory), `ProjectAuditProjection` (Timeline). Verify with `frontcomposer inspect` + Verify snapshots + bUnit.
4. **Phase 3 — Diagnostics views:** `ProjectWarningProjection` (ActionQueue + `WhenState`), `ProjectHealthProjection` (Dashboard). 
5. **Phase 4 — Resolution trace:** attempt L2 template; escalate to L3 slot / L4 view only if required; register override + contract version.
6. **Phase 5 — Maintenance commands:** `[Command]` + `[Destructive]` + `[RequiresPolicy]` + `[Icon]`; host wires policies; verify five-state lifecycle + ProblemDetails on rejection.
7. **Phase 6 — Surface parity & hardening:** confirm MCP resources (read) / tools (mutate) split and CLI evidence; full negative-path + a11y + tenant-isolation E2E; CI fail-closed gates.

### Technology Stack Recommendations

- Keep all pinned versions (Fluent UI `5.0.0-rc.2`, Fluxor `6.9`, Roslyn `4.12`, MCP `1.2`, .NET SDK `10.0.302`). Defer any Fluent UI bump until v5 GA; evaluate MCP `1.2→1.3` separately for breaking changes.
- Reuse EventStore/Tenants testing fakes and the Shell's storage/service doubles; do not introduce new assertion/mocking libraries.

### Skill Development Requirements

- Roslyn incremental-generator discipline (parse/transform/emit, value-equality models, no captured symbols) for anyone touching SourceTools.
- Fluxor feature/reducer conventions and Blazor Auto lifecycle (prerender safety, `[PersistentState]`).
- The customization gradient + registry model (template/slot/view-override) and accessibility verification (`@axe-core/playwright`).
- EventStore CQRS mental model: persist-then-publish, nudge→requery, ETag concurrency, tenant identity.

### Success Metrics & KPIs

- **Surface parity:** identical state/reason-code/timestamp vocabulary across Web, MCP, CLI (asserted by shared enums + fingerprint tests).
- **Gradient discipline:** ≥ ~80% of views at Level 1–2; every Level 4 justified in review.
- **Quality gates green:** `frontcomposer inspect --fail-on-warning` clean; main-lane tests + Verify snapshots + axe a11y passing; zero cross-tenant leakage in negative-path E2E.
- **Redaction:** no payloads/PII/tokens/tenant IDs in any surface, log, diagnostic, or evidence artifact.
- **Incrementality:** generator stays in the low-millisecond range on edits (no symbol capture regressions).

---

# The Compiled Console: Implementing the Hexalith.Projects Web UX with FrontComposer

## Executive Summary

Hexalith.Projects is an administrative/operational control plane, and its UX specification makes an unusual, decisive commitment: the Web surface must be **composed through Hexalith.FrontComposer**, and CLI, MCP, and Web must behave as three adapters over **one operational model**. This research confirms that commitment is not just feasible but is the exact problem FrontComposer was built to solve. FrontComposer is a contract-first UI compiler: a `[Projection]`- or `[Command]`-annotated domain type is processed by Roslyn incremental source generators (`ForAttributeWithMetadataName`, `Microsoft.CodeAnalysis` 4.12) into Razor DataGrid UI, Fluxor state, SDK-neutral MCP descriptors, and CLI inspect/migrate evidence — all from a single declaration. The industry's hardest version of this problem, **design/behavior drift across surfaces**, is eliminated by construction because the surfaces *are* the contract.

The fit is remarkably tight. The framework's `ProjectionRole` enum — capped at five roles — maps almost one-to-one onto the Projects views: `ActionQueue`→warnings/maintenance queue, `StatusOverview`→status tiles, `DetailRecord`→project detail, `Timeline`→audit history, `Dashboard`→operational overview, with the default DataGrid serving the project list. The four-level **customization gradient** (annotation → typed template → typed slot → full replacement) is a textbook escape-hatch pattern that keeps the vast majority of views at Level 1 (where lifecycle, accessibility, MCP schema, diagnostics, and generated tests stay authoritative) while reserving deeper customization for the one genuinely bespoke view, the resolution trace. The **five-state command lifecycle** (`Idle→Submitting→Acknowledged(202)→Syncing→Confirmed/Rejected`) is the structural embodiment of EventStore's persist-then-publish flow plus SignalR-nudge→re-query, and `McpLifecycleStateNames` shares that vocabulary with the MCP surface so consistency is enforced in the type system, not by convention.

The strategic implication: building the Projects Web UX is primarily an exercise in **declaring the right projection and command contracts** with a shared, badge-annotated state vocabulary — not in writing Blazor components. The one material external risk is the **Fluent UI Blazor v5 RC** dependency (RC2 pinned, no GA as of May 2026); the repo's pin-and-reuse discipline is the correct mitigation, and every other foundation (.NET 10 Blazor Auto, Roslyn generators, Fluxor, MCP v1.0) is stable.

**Key Technical Findings:**

- **Contract-first, three-surface generation is the core mechanism** — one annotated projection emits Web + MCP + CLI, making surface parity a compiler guarantee rather than a coordination task. (Confidence: High)
- **`ProjectionRole` maps ~1:1 to the UX-spec views**, so most of the console is *declared*, not built. (Confidence: High; specific role→view mapping Medium-High pending generated-output validation)
- **The customization gradient bounds bespoke work** — default Level 1; the resolution trace is the only likely Level 3/4 view. (Confidence: High)
- **Cross-cutting guarantees are framework contracts**: tenant isolation (JWT + query-side filtering), accessibility (axe-verified), metadata-only redaction, and the five-state lifecycle are already honored by generated output. (Confidence: High)
- **Eventual consistency is explicit**: SignalR nudges trigger re-query (never carry data); ETag handles concurrency — views must tolerate the brief stale window. (Confidence: High)
- **Single material risk is Fluent UI v5 RC churn**; mitigated by pinning and component reuse. (Confidence: High for the risk; Medium on GA timing)

**Technical Recommendations:**

1. Define a **shared, `[ProjectionBadge]`-annotated state vocabulary** (`ReferenceState`, `ProjectLifecycleState`) and reason-code list *first* — it unblocks every view and is what makes the three surfaces identical.
2. Implement each view as a `[Projection]` with an explicit `ProjectionRole`, defaulting to **gradient Level 1**; require review justification before dropping to Level 4.
3. Model maintenance as `[Command]` + `[Destructive]` + `[RequiresPolicy]` + `[Icon]`; rely on the five-state lifecycle and `ProblemDetailsPayload` (RFC 9457) for feedback.
4. Wire **fail-closed CI** with `frontcomposer inspect --fail-on-warning`, the main-lane filtered `dotnet test`, Verify.XunitV3 snapshots, and `@axe-core/playwright`; mandate cross-tenant negative-path E2E.
5. **Freeze the pinned dependency set** (especially Fluent UI v5 RC) until GA; treat any bump as a deliberate, cross-module decision.

## Table of Contents

1. Technical Research Scope Confirmation *(above)*
2. Technology Stack Analysis — .NET 10, Fluent UI v5 RC, Fluxor, Roslyn, MCP, EventStore *(above)*
3. Integration Patterns Analysis — one model → three surfaces; REST + SignalR; commands; MCP; CLI; consistency; security *(above)*
4. Architectural Patterns and Design — contract-first generation, customization gradient, `ProjectionRole`, registry composition, lifecycle, Blazor Auto, accessibility *(above)*
5. Implementation Approaches and Technology Adoption — per-view playbook, code sketches, testing, deployment, risks *(above)*
6. Technical Research Recommendations — roadmap, stack, skills, KPIs *(above)*
7. Technical Research Synthesis — introduction, cross-cutting insights, conclusion *(this section)*
8. Source Documentation & Methodology *(below)*

## Introduction & Significance

Administrative and operational consoles are where multi-surface drift does the most damage. An operator diagnosing a failed project-context resolution needs the CLI, the agent-facing MCP tools, and the Web console to agree precisely on what a state like `tenant_mismatch` or `stale` *means*, what reason code accompanies it, and what maintenance action is safe — because disagreement between surfaces during an incident is worse than a missing feature. Industry practice frames this as **design drift**: the slow divergence between intended patterns and what actually ships, driven by translation steps between teams and tools; the recommended cure is to build from the same components that ship to production rather than re-translating intent per surface.

Hexalith.FrontComposer takes that cure to its logical conclusion: there is no translation step at all. The projection contract is the single source of truth, and the generator emits every surface from it. For Hexalith.Projects — explicitly scoped as metadata-only operational tooling that must never expose conversation, file, prompt, or memory payloads — this is doubly valuable: redaction and tenant-isolation guarantees are enforced once, at the framework boundary, and inherited by all three surfaces. The significance of this research is therefore practical and immediate: it shows that the Projects Web UX is achievable as a *declarative* effort tightly aligned with the existing UX specification, with bounded, well-understood risk.

This research was conducted **repo-first** — the FrontComposer documentation, the verified attribute contracts under `Hexalith.FrontComposer.Contracts`, the Projects UX specification, and the workspace `project-context.md` rules are authoritative — with web search used to validate every framework-level fact (.NET 10 Blazor Auto, Fluent UI v5 RC status, Roslyn incremental-generator best practices, MCP C# SDK, RFC 9457, CQRS consistency, bUnit/Verify testing). Confidence levels are stated throughout; repo-sourced facts are High, and forward-looking items (Fluent UI GA timing, role→view mapping pending generated-output inspection) are flagged.

## Technical Research Synthesis — Cross-Cutting Insights

- **The whole design composes around one decision per view: which gradient level.** Everything else (state vocabulary, role, badges, policies) is declarative. Getting the gradient discipline right — Level 1 by default — is the difference between a console that evolves cleanly with the generator and one that accumulates Level-4 components that re-implement (and slowly break) the framework's accessibility and lifecycle contracts.
- **Consistency is structural, not procedural.** Shared badge-annotated enums, `McpLifecycleStateNames`, and the `SchemaFingerprint` mean the three surfaces cannot silently diverge without failing a compatibility test. This is the architectural answer to the UX spec's strongest requirement.
- **The CQRS/eventual-consistency model is visible in the UI contract, not hidden.** The `Syncing` lifecycle state and ETag-bearing `QueryResult<T>` give operators honest feedback during the nudge→requery window — exactly what an operational diagnostic tool needs, and a deliberate contrast to read-after-write assumptions.
- **Security is inherited, not authored per view.** Two-layer authorization, tenant-scoped SignalR nudges, and redaction are framework-level; the playbook's job is to *not weaken* them (e.g., not leaking tenant identity into evidence, not collapsing distinct failure states into a generic error).
- **The risk surface is small and named.** Fluent UI v5 RC is the only volatile external dependency; everything else is platform-stable, so engineering attention belongs on discipline (incremental-generator correctness, prerender safety, gradient restraint) rather than on platform volatility.

## Technical Research Conclusion

**Summary of Key Findings.** FrontComposer is a contract-first UI compiler whose generation model, role taxonomy, customization gradient, command lifecycle, and cross-cutting contracts align closely with the Hexalith.Projects UX specification's "one operational model, three surfaces" mandate. Implementing the Projects Web UX is principally a matter of declaring projection and command contracts over a shared, badge-annotated state vocabulary, then letting the generator emit Web, MCP, and CLI in lockstep.

**Strategic Technical Impact.** The approach converts what is usually a multi-surface coordination problem into a single-source-of-truth compilation problem, eliminating drift by construction and inheriting tenant-isolation, accessibility, and redaction guarantees from the framework. This materially lowers both build cost and the long-term consistency risk that operational tooling is most vulnerable to.

**Next Steps.** (1) Stand up the Projects host with FrontComposer packages and Aspire wiring (REST + SignalR + `MapMcp`); (2) define the shared state enums and reason codes; (3) implement the Level-1 read views (list, detail, reference inventory, timeline) and verify with `frontcomposer inspect` + Verify snapshots + bUnit; (4) add the diagnostics views (warnings ActionQueue, health Dashboard); (5) tackle the resolution trace at the lowest gradient level that works; (6) implement maintenance commands; (7) harden surface parity, accessibility, and tenant isolation in CI. This research is ready to feed `/bmad-create-story` for epic/story breakdown.

## Source Documentation & Methodology

**Repo-internal authoritative sources (Confidence: High):**
- Hexalith.Projects UX specification — `_bmad-output/planning-artifacts/ux-design-specification.md`
- Workspace + FrontComposer rules — `_bmad-output/project-context.md`, `Hexalith.FrontComposer/_bmad-output/project-context.md`
- FrontComposer docs — `docs/tutorials/getting-started.md`, `docs/concepts/source-generation-and-mcp-split.md`, `docs/how-to/customization-gradient-cookbook.md`, `docs/reference/cli.md`, `docs/reference/mcp/index.md`
- Verified attribute & contract source — `src/Hexalith.FrontComposer.Contracts/Attributes/*`, `…/Communication/*`, `…/Mcp/*`, `…/Registration/*`, `…/Rendering/*`, `…/Lifecycle/*`; sample `samples/IdeParityCounter/*`

**Web sources used for framework-fact validation:**
- Blazor render modes / prerender (.NET 10): https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0 · https://learn.microsoft.com/en-us/aspnet/core/blazor/components/prerender?view=aspnetcore-10.0
- Fluent UI Blazor v5 RC1/RC2: https://baaijte.net/blog/microsoft-fluentui-aspnetcore.components-50-rc1/ · https://github.com/microsoft/fluentui-blazor/releases
- Roslyn incremental generators / `ForAttributeWithMetadataName`: https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.cookbook.md · https://andrewlock.net/creating-a-source-generator-part-9-avoiding-performance-pitfalls-in-incremental-generators/ · https://www.thinktecture.com/en/net-core/roslyn-source-generators-high-level-api-forattributewithmetadataname/
- MCP C# SDK: https://github.com/modelcontextprotocol/csharp-sdk · https://www.nuget.org/packages/ModelContextProtocol/ · https://devblogs.microsoft.com/dotnet/build-a-model-context-protocol-mcp-server-in-csharp/
- RFC 9457 Problem Details: https://www.rfc-editor.org/rfc/rfc9457.html · https://www.milanjovanovic.tech/blog/problem-details-for-aspnetcore-apis
- CQRS read-model consistency: https://dev.to/xuan_56087d315ff4f52254e6/complex-query-handling-in-cqrs-minimizing-roundtrips-and-latency-with-projection-materialization-494n · https://clouddc.substack.com/p/day-11-cqrs-consistency-handling
- Source-gen vs reflection / escape-hatch pattern: https://www.devleader.ca/2026/02/07/source-generation-vs-reflection-in-needlr-choosing-the-right-approach · https://react.dev/learn/escape-hatches · https://dev.to/paulgildea/using-slots-with-fluent-ui-react-v9-jf1
- Testing (bUnit / Verify) & FluentDataGrid: https://bunit.dev/ · https://github.com/VerifyTests/Verify.Bunit · https://andrewlock.net/creating-a-source-generator-part-2-testing-an-incremental-generator-with-snapshot-testing/ · https://deepwiki.com/microsoft/fluentui-blazor/5.1-datagrid-component · https://www.fluentui-blazor.net/datagrid-virtualize
- Multi-surface consistency / design drift: https://www.uxpin.com/studio/blog/design-drift/

**Web search queries executed:** Fluent UI Blazor v5 release status; .NET 10 Blazor Auto render mode/prerender; Roslyn `ForAttributeWithMetadataName` best practices; MCP C# SDK ASP.NET Core; RFC 9457 vs 7807; CQRS read-model SignalR/ETag concurrency; model-driven UI generation vs reflection; escape-hatch progressive customization; bUnit/Verify snapshot source-generator testing; FluentDataGrid virtualization/accessibility; internal-platform generated console / UI drift.

**Methodology & quality assurance.** Scope: full one-model-three-surfaces (Web + MCP + CLI) generation, repo-first with web validation. Analysis framework: technology stack → integration patterns → architectural patterns → implementation playbook → synthesis. All framework claims cross-checked against current public docs; confidence levels stated per claim; limitations noted (illustrative projection field names must be reconciled with actual EventStore Projects read models; `ProjectionRole`→view mapping should be confirmed against generated output; Fluent UI GA timing unannounced).

---

**Technical Research Completion Date:** 2026-05-24
**Research Type:** Technical (implementation playbook)
**Source Verification:** Repo-authoritative + web-validated framework facts, cited inline
**Overall Confidence:** High for repo-sourced and validated framework facts; Medium where forward-looking (flagged inline)

_This document is an authoritative implementation reference for building the Hexalith.Projects Web UX with Hexalith.FrontComposer, ready to feed epic/story breakdown via `/bmad-create-story`._

