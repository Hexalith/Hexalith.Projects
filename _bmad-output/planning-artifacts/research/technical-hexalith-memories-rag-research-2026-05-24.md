---
stepsCompleted: [1, 2, 3, 4, 5, 6]
inputDocuments: []
workflowType: 'research'
lastStep: 6
status: 'complete'
research_type: 'technical'
research_topic: 'How to use Hexalith.Memories to manage project conversation memories and file search (RAG)'
research_goals: 'Equip an integrating developer to (1) store and recall memory items/documents across sessions and (2) perform document ingestion and file search (RAG) with Hexalith.Memories, accessed via the .NET Contracts/Client library and the REST/HTTP API. Cover correct usage plus enough retrieval internals (ranking/fusion) to tune result quality. Scope is the Memories module standalone (not the Conversations module).'
user_name: 'Jerome'
date: '2026-05-24'
web_research_enabled: true
source_verification: true
research_perspective: 'Integrating developer'
research_surfaces: ['Library / Contracts (.NET)', 'REST / HTTP client']
research_rag_depth: 'Usage + enough to tune'
research_conversation_scope: 'Memories module standalone'
---

# Research Report: technical

**Date:** 2026-05-24
**Author:** Jerome
**Research Type:** technical

---

## Research Overview

This report is a practitioner's guide for an **integrating developer** consuming **Hexalith.Memories** — the Hexalith memory module providing syntactic + semantic (and optional graph) search — to (1) store and recall memory items / documents across sessions and (2) ingest files and run retrieval (RAG). It focuses on the **.NET Contracts/Client library** and the **REST/HTTP API** surfaces, with enough retrieval internals to tune result quality. The Memories module is treated standalone (the Conversations module is out of scope).

**Methodology.** Findings draw on two evidence classes:

- **Module ground truth** — the Hexalith.Memories repository (contracts, client, REST endpoints, ADRs, `docs/dev`, `Directory.Packages.props`). Authoritative for *how this module behaves*; cited by repo path.
- **Current public web sources** — for the underlying technologies (Redis Stack / RediSearch / Redis vector, FalkorDB, Dapr, MCP, embedding providers), verified against live documentation. Authoritative for *industry patterns and rationale*; cited by URL.

Confidence levels are flagged where sources are sparse or fast-moving. Module-specific behavior always defers to repo source over general web claims.

**Key takeaways at a glance.** Hexalith.Memories is a multi-tenant RAG service you consume over **REST** (or the typed **.NET `MemoriesClient`**); you organize memories as **Tenant → Case → MemoryUnit**, **ingest** files asynchronously (202 + workflow), and **search** across **syntactic / semantic / graph / hybrid** axes. Behind the API, every unit is **triple-written** to RediSearch + Redis Vector + FalkorDB, and hybrid results are produced by **normalized weighted-average fusion** (default weights 0.4/0.4/0.2). Your real tuning levers are **axis selection, `maxResults`/`tokenBudget`, `explain`, `caseId` scoping, and the embedding provider** — fusion weights are **not** per-request today. The biggest adoption caveat: the core write methods are **`[Experimental("HXL001")]`**. See the **Research Synthesis** section below for the full executive summary, recommendations, and roadmap.

---

## Technical Research Scope Confirmation

**Research Topic:** How to use Hexalith.Memories to manage project conversation memories and file search (RAG)
**Research Goals:** Equip an integrating developer to store/recall memory items and run document ingestion + file search with Hexalith.Memories, via the .NET Contracts/Client library and the REST/HTTP API, with enough retrieval internals to tune results. Memories module standalone.

**Refined scope (from clarification):**

- **Perspective:** Integrating developer
- **Surfaces in depth:** .NET Library/Contracts + REST/HTTP client (MCP and CLI only where they clarify integration)
- **RAG depth:** Usage + enough retrieval internals to tune
- **Conversation memories:** Hexalith.Memories standalone (Conversations module out of scope)

**Technical Research Scope:**

- Architecture & data model — memory/document representation, tenant/case identity, ingestion → index → search flow
- Implementation approaches — concrete usage via .NET client and REST API
- Technology stack — what a consumer must run/configure
- Integration patterns — contract shapes, DI registration, idempotency, errors, tenant isolation obligations
- Retrieval tuning — syntactic / semantic / optional graph fusion, ranking knobs, degradation, quality signals

**Research Methodology:**

- Module ground truth (repo source) + current web data with source verification
- Multi-source validation for critical technical claims
- Confidence levels for uncertain information

**Scope Confirmed:** 2026-05-24

## Technology Stack Analysis

This stack is derived from the module manifest (`Hexalith.Memories/Directory.Packages.props`) and `docs/`, then verified against current public sources. For an **integrating developer**, the practical split is: *what you call* (REST API / .NET client, JWT), *what you must run* (Aspire + Dapr + Redis Stack + FalkorDB), and *what shapes results* (Kreuzberg extraction + embedding provider + Redis/FalkorDB retrieval).

### Programming Languages & Runtime

- **.NET 10 / C# 14**, all projects target `net10.0`; SDK pinned to `10.0.302` (`global.json`). Nullable, implicit usings, and **warnings-as-errors** are on; **central package management** is mandatory (versions in `Directory.Packages.props`, never inline).
- _Consumer impact:_ to reference the **client/contracts library in-process** you must build on `net10.0`. If you integrate purely over **REST**, runtime is irrelevant — any HTTP client works.
- _Source:_ `Directory.Packages.props`, `global.json`; module `project-context.md`.

### Development Frameworks and Libraries

- **Dapr 1.17.9** — `Dapr.Client`, `Dapr.Workflow`, `Dapr.Actors`, `Dapr.AI`. The server uses Dapr **Workflow** for durable, replay-safe multi-step ingestion (and tenant provisioning/deletion, consistency repair), and **Actors** for per-tenant stateful singletons (rate limits, corpus stats). Workflows replay from event-sourced history and must stay deterministic. _As an integrating developer you never call Dapr directly_ — you call REST/the client; Dapr is an internal abstraction. _Source:_ `Directory.Packages.props`; [Dapr Workflow overview](https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-overview/).
- **Aspire 13.3.3** (`Aspire.Hosting.Testing`, `CommunityToolkit.Aspire.Hosting.Dapr 13.0.0`) — the AppHost owns local topology: Dapr component generation, Redis endpoint wiring, sidecar options. Local boot: `dotnet run --project src/Hexalith.Memories.AppHost`. _Source:_ `Directory.Packages.props`; `README.md`.
- **HTTP client stack** — `Microsoft.Extensions.Http` + `Microsoft.Extensions.Http.Resilience 10.6.0` + `Microsoft.Extensions.ServiceDiscovery`. _Consumer impact:_ the generated REST client is expected to be registered through the standard `IHttpClientFactory`/resilience pipeline, not hand-rolled `HttpClient` instances.
- **Auth:** `Microsoft.AspNetCore.Authentication.JwtBearer 10.0.8` — the API is JWT-protected; callers present a bearer token (see ADR-10.2-003 JWT selection, ADR-10.2-004 auth granularity).
- **MCP:** `ModelContextProtocol 1.3.0` (+ `.AspNetCore`) hosts the agent tool surface over Streamable HTTP — *secondary surface for this research.*
- **CLI:** `System.CommandLine 2.0.8` backs the `memories` CLI (recursive/global options, async + `CancellationToken`).
- _Source:_ `Directory.Packages.props`.

### Database and Storage Technologies (the RAG backends)

- **Redis Stack** via `NRedisStack 1.4.0` + `StackExchange.Redis 3.0.2-preview`:
  - **RediSearch** powers **syntactic / full-text** search.
  - **Redis vector** powers **semantic** search — KNN (FLAT) and ANN (HNSW) indexing, with **cosine / L2 / inner-product** metrics; **hybrid** text+vector queries combine structured filters with semantic similarity (Redis 8.4.0 adds an `FT.HYBRID` linear score combination). _Source:_ [Redis vector similarity](https://redis.io/tutorials/howtos/solutions/vector/getting-started-vector/), [Redis hybrid FT.SEARCH](https://oneuptime.com/blog/post/2026-03-31-redis-ft-hybrid-vector-text-search/view).
  - Tenant isolation is **physical**: tenant-scoped RediSearch and vector indexes, not just filtered.
- **FalkorDB** via `NFalkorDB 1.0.6`: a **property-graph** database (GraphBLAS sparse-matrix engine, **OpenCypher**) built for **GraphRAG**. In Memories this powers **optional, degradable graph search**; graph queries must be parameterized (no string-concatenated Cypher). Tenant isolation uses per-tenant graphs/databases. _Source:_ [FalkorDB docs](https://docs.falkordb.com/), [FalkorDB GitHub](https://github.com/FalkorDB/FalkorDB).
- _Consumer impact:_ you don't talk to Redis/FalkorDB directly — but their behavior is the *tuning surface* (which index types/metrics exist, why graph search can degrade). Covered in the retrieval-tuning step.

### Content Extraction and Embeddings (the ingestion path)

- **Kreuzberg 4.9.8** — the **C# binding** of the Kreuzberg polyglot *document-intelligence* framework (Rust core). Extracts text, metadata, tables, and images from **PDFs, Office (DOCX/PPTX/XLSX), HTML, images and 90+ formats**, with automatic **OCR** for scanned/hybrid PDFs and images (Tesseract / PaddleOCR / EasyOCR / VLM backends). This is what turns an ingested file into searchable text + chunks. _Consumer impact:_ defines **which file types you can ingest** and that scanned PDFs are OCR'd. _Source:_ `Directory.Packages.props`; [Kreuzberg](https://kreuzberg.dev/), [Kreuzberg GitHub](https://github.com/kreuzberg-dev/kreuzberg), [Format support](https://docs.kreuzberg.dev/reference/formats/).
- **Embedding providers** (`TenantEmbeddingConfig` / `EmbeddingClient`):
  - **Google API key** — default managed-provider path.
  - **Ollama (OIDC client-credentials)** — committed self-hosted path: `POST /api/embed`, DAPR secret lookup, **`qwen3-embedding:4b` → 2560-dimension** vectors.
  - _Consumer impact:_ embedding **vector dimension is provider-bound** (e.g. 2560 for the Ollama path); the provider is **tenant configuration**, set when you provision/configure a tenant. _Source:_ `docs/dev/embedding-providers.md`, `docs/operations/embedding-providers.md`.

### Development Tools, Observability & Platform

- **Observability:** OpenTelemetry **1.15.3** core/exporter (+ AspNetCore 1.15.2, Http/Runtime 1.15.1, StackExchange.Redis 1.15.1-beta.1) — OTLP traces/metrics/logs via `MemoriesActivitySource`/`MemoriesMeter` with low-cardinality tags. _Source:_ `Directory.Packages.props` (versions pinned lock-step, Story 8.5/10.1).
- **Testing:** xUnit **v3** (`3.2.2`), Shouldly `4.3.0`, NSubstitute `5.3.0`, Testcontainers `4.11.0`, bUnit `2.7.2`, `Aspire.Hosting.Testing`, `Microsoft.AspNetCore.Mvc.Testing` (Tier-2 WAF tests, no Docker).
- **Local platform prerequisites:** Docker Desktop (Redis Stack + FalkorDB containers), .NET SDK 10.0.302+, `git` with submodules. _Source:_ `README.md`.

### Technology Adoption / Rationale Notes

- The stack implements the modern **hybrid-retrieval RAG** pattern: lexical (RediSearch) + dense-vector (Redis vector) + optional **GraphRAG** (FalkorDB), fused into one ranked result set. This mirrors current industry direction (Redis `FT.HYBRID`, FalkorDB GraphRAG positioning). _Confidence: high_ on the individual technologies (verified); _module-specific fusion details_ are covered from repo source in the retrieval-tuning step.
- Several packages are intentionally **prerelease/pinned** (`StackExchange.Redis 3.0.2-preview`, OTel Redis instrumentation `-beta.1`, Fluent UI RC). Treat them as pinned; do not upgrade casually.

**Sources:** [Kreuzberg](https://kreuzberg.dev/) · [Kreuzberg GitHub](https://github.com/kreuzberg-dev/kreuzberg) · [Kreuzberg formats](https://docs.kreuzberg.dev/reference/formats/) · [Redis vector search](https://redis.io/tutorials/howtos/solutions/vector/getting-started-vector/) · [Redis hybrid search](https://oneuptime.com/blog/post/2026-03-31-redis-ft-hybrid-vector-text-search/view) · [FalkorDB docs](https://docs.falkordb.com/) · [FalkorDB GitHub](https://github.com/FalkorDB/FalkorDB) · [Dapr Workflow overview](https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-overview/)

## Integration Patterns Analysis

This section is the practical core for an integrating developer. It is grounded in the actual client/contract source (`src/Hexalith.Memories.Client.Rest`, `src/Hexalith.Memories.Contracts/V1`) and cross-checked against current Microsoft/industry guidance on typed HTTP clients and async REST.

### Identity & Resource Model

Everything is addressed by a 3-level hierarchy — this is the mental model for "managing project memories":

- **Tenant** (`TenantId`) → physical isolation boundary (own indexes/graphs).
- **Case** (`CaseId`) → a named grouping of memory units *within* a tenant (e.g. one "project" or "conversation thread"). A `Case` carries `Name`, `Description`, `Status`, `MemoryUnitCount`.
- **Memory unit** (`MemoryUnit.Id`, ULID) → one ingested+indexed item: `Content`, `ContentHash`, `SourceUri`, `SourceType`, `IngestedBy`, timestamps, `Status`, `Metadata`, and embedding provenance (`EmbeddingProvider`/`EmbeddingModel`/`EmbeddingDimensions`).

_To "manage project conversation memories":_ create a **Case** per project/conversation, **ingest** each note/transcript/file as a memory unit into that case, then **search** scoped to the case (`caseId`) or across the whole tenant (omit `caseId`). _Source:_ `Contracts/V1/Case.cs`, `MemoryUnit.cs`, `IngestionInput.cs`.

### API Design Patterns (REST surface)

The server is a conventional **resource-oriented REST API over HTTP** with JSON bodies; the typed client maps 1:1 to these routes. Endpoints exercised by `MemoriesClient`:

| Operation | Method & route | Notes |
|---|---|---|
| Health probe | `GET /health` | 5s timeout in client; 2xx ⇒ healthy |
| List tenants | `GET /api/tenants` | → `TenantSummary[]` |
| Provision tenant | `POST /api/tenants` | **202**, body `{ workflowInstanceId }` — async |
| Get tenant | `GET /api/tenants/{tenantId}` | 404 ⇒ client returns `null` (not an exception) |
| List cases | `GET /api/tenants/{tenantId}/cases` | → `Case[]` |
| Create case | `POST /api/tenants/{tenantId}/cases` | body `CreateCaseInput` → `Case` |
| Get case | `GET /api/tenants/{tenantId}/cases/{caseId}` | → `Case` |
| Get memory unit | `GET /api/tenants/{tenantId}/cases/{caseId}/memory-units/{id}` | → `MemoryUnit` |
| **Ingest file** | `POST /api/ingest` | body `IngestionInput`; **202**, body `{ instanceId }` — async |
| **Single-axis search** | `GET /api/search?tenantId=&axis=&query=&caseId=&maxResults=&explain=&tokenBudget=` | `axis ∈ {syntactic, semantic, graph}` → `SearchResult` |
| **Hybrid search** | `GET /api/search?...&axis=hybrid` | → `HybridSearchResult` (fused) |
| Graph traversal | `GET /api/tenants/{tenantId}/traverse?startNodeId=&depth=&caseId=&edgeTypes=&tokenBudget=` | depth clamped `[0,10]` |
| Telemetry summary | `GET /api/tenants/{tenantId}/telemetry/summary` | → `TelemetrySummary` |
| Export case / tenant | `GET .../cases/{caseId}/export`, `GET /api/tenants/{tenantId}/export` | streamed JSON (`ResponseHeadersRead`) |
| Consistency verify/inspect/repair | `POST/GET .../consistency/{verify,inspect,repair}` | verify/repair are async workflows |

_Convention:_ search uses **GET with query-string params**, and the client **omits server-default values** (`maxResults=10`) to keep URLs clean. Path segments are `Uri.EscapeDataString`-encoded. _Source:_ `Client.Rest/MemoriesClient.cs`.

### .NET Client Integration (typed client + DI)

Registration is a single call returning an `IHttpClientBuilder` so you can attach resilience/discovery:

```csharp
builder.Services.AddMemoriesClient(o =>
{
    o.Endpoint  = new Uri("https://memories.internal/");      // base address
    o.ApiToken  = builder.Configuration["HEXALITH_MEMORIES_API_TOKEN"]; // prefer env/secret over argv
})
.AddStandardResilienceHandler();   // Microsoft.Extensions.Http.Resilience — ONE resilience handler

// then inject MemoriesClient anywhere:
public sealed class ProjectMemoryService(MemoriesClient memories) { /* ... */ }
```

`AddMemoriesClient` (`MemoriesClientServiceCollectionExtensions.cs`) registers `MemoriesClientOptions`, the transient `MemoriesAuthHandler`, and a **typed client** via `AddHttpClient<MemoriesClient>` with a **30s default timeout**, base address from `Endpoint`, and the auth handler in the pipeline.

Design notes that affect your code:

- **`MemoriesClient` is a concrete class with `virtual` methods (no interface) — Architecture D9.** Don't write your own `IMemoriesClient`; for tests, mock at the `HttpClient`/`IHttpClientFactory` boundary (or override the virtual methods). _Source:_ `MemoriesClient.cs` class remarks.
- Follows current Microsoft guidance: typed client over `IHttpClientFactory`, delegating-handler pipeline, and **exactly one** resilience handler (don't stack Polly handlers). _Source:_ [Use IHttpClientFactory](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory), [Build resilient HTTP apps](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience).
- Every method takes a `CancellationToken`; library code uses `ConfigureAwait(false)`.

### Communication Protocols & Data Formats

- **HTTP/JSON**, serialized with **`System.Text.Json` source generation** (`MemoriesJsonContext.Options`) for trim/AOT-friendly, allocation-light (de)serialization. Enums serialize **camelCase** via `CamelCaseStringEnumConverter<T>` (e.g. `SourceType.File` → `"file"`). _Source:_ `Contracts/V1/MemoriesJsonContext.cs`, `SourceType.cs`.
- **Versioned contracts** live under `Contracts.V1` and evolve **additively / serialization-tolerantly** (e.g. `IngestionResult.NaturalLanguageEmbeddingStatus` defaults to `NotApplicable` for pre-9.2 payloads). **Client and server must share a compatible `Contracts.V1`** — a 2xx body the client can't parse surfaces as an `INVALID_RESPONSE` error advising a version check. _Source:_ `MemoriesClient.cs`, `IngestionResult.cs`.
- **Metadata** is a `Dictionary<string, MetadataField>` where each `MetadataField(Value, Origin, Confidence)` tracks provenance + confidence; the dictionary is pinned to `StringComparer.Ordinal` (CloudEvent key fidelity). _Source:_ `MetadataField.cs`, `IngestionInput.cs`.

### Asynchronous Integration (202 + polling) — critical for ingestion

Ingestion and tenant provisioning are **not synchronous**. They map exactly to the **Asynchronous Request-Reply** pattern (verified against Microsoft Azure Architecture Center):

- `IngestAsync(...)` → `POST /api/ingest` returns **202** with `{ instanceId }`; the Dapr **Workflow** runs extract → embed → index in the background. The returned id is your handle.
- `CreateTenantAsync(...)` → `POST /api/tenants` returns **202** with `{ workflowInstanceId }`; **poll `GetTenantAsync` until `TenantStatus.Active`** (or the provision-status endpoint) before ingesting.
- Consistency verify/repair return a **`Location`** header → status URI, consumed by `StartConsistency*Async` / `GetConsistency*StatusAsync`.

_Implication:_ after `IngestAsync` the unit is **not immediately searchable** — gate dependent work on workflow/health/status, don't assume read-after-write. `IngestionInput` rules: `ContentBytes` **required** for `SourceType.File`, and **must be null** for `SourceType.Url` (the workflow fetches the body server-side). **Dedup is content-hash based** — `IngestionResult.WasDuplicate` flags a re-ingest of identical content. Optional `CausationId`/`CorrelationId` propagate tracing. _Source:_ `MemoriesClient.cs`, `IngestionInput.cs`, `IngestionResult.cs`; [Async Request-Reply](https://learn.microsoft.com/en-us/azure/architecture/patterns/asynchronous-request-reply).

### Integration Security Patterns

- **Bearer/JWT** on the server (`Microsoft.AspNetCore.Authentication.JwtBearer`). The client's `MemoriesAuthHandler` chooses the header by transport: **HTTPS ⇒ `Authorization: Bearer {token}`**; **plain-HTTP loopback ⇒ `dapr-api-token: {token}`** (local sidecar); **plain-HTTP to a non-localhost host ⇒ it throws** rather than leak a token over cleartext. _Source:_ `MemoriesAuthHandler.cs`.
- **Token hygiene:** prefer the **`HEXALITH_MEMORIES_API_TOKEN` env var / secret store** over `--token` argv (visible in shell history/process listings); never log tokens. _Source:_ `MemoriesClientOptions.cs`.

### System Interoperability & Tenant-Isolation Obligations on the Caller

- **`TenantId` is a required, explicit argument on essentially every call** and is validated eagerly (`ArgumentException.ThrowIfNullOrWhiteSpace`). Server-side isolation is physical, but the caller still owns passing the correct tenant and a JWT scoped to it — there is no ambient tenant. _Source:_ `MemoriesClient.cs`.
- `caseId` is **optional** on search/traversal: provide it to scope to one project/conversation, omit it for tenant-wide recall.
- **Non-REST surfaces are translation layers, not shortcuts:** the **MCP** server and **CLI** both call this same REST/client path and enforce the same tenant/auth gates — useful as alternative front-ends, but they don't expose extra capability or bypass authorization.

### Error-Handling Pattern

- Any non-2xx becomes a **`MemoriesRemoteException`** exposing `StatusCode` (HTTP) **and** a structured **`ErrorResponse(Code, Message, Suggestion)`** — every error carries an *actionable suggestion*. Catch it, branch on `Error.Code`. _Source:_ `MemoriesRemoteException.cs`, `ErrorResponse.cs`, `ErrorResponseDecoder.cs`.
- Observed codes: `TENANT_NOT_FOUND`, `INVALID_MEMORY_UNIT_ID` (400), `MEMORY_UNIT_NOT_FOUND` (404), `INVALID_RESPONSE` (version/parse mismatch). **Soft-miss exception:** `GetTenantAsync` and the consistency-status getters return **`null` on 404** instead of throwing — model "absent" vs "error" accordingly.
- **Streaming results** (`ExportCaseAsync`/`ExportTenantAsync`) hand back a `Stream` the **caller must dispose** (request uses `HttpCompletionOption.ResponseHeadersRead`).

```csharp
try
{
    var result = await memories.HybridSearchAsync(
        new HybridSearchRequest(TenantId: "acme", Query: "deployment rollback steps", CaseId: "proj-42", Explain: true),
        ct);
    foreach (var hit in result.Results)            // FusedScoredResult, ordered by CompositeScore
        Console.WriteLine($"{hit.CompositeScore:F3}  {hit.ContentSnippet}");
    if (result.Degraded)                           // graph axis may have been unavailable
        logger.LogWarning("Search degraded; axes unavailable: {Axes}", string.Join(",", result.UnavailableAxes));
}
catch (MemoriesRemoteException ex) when (ex.Error.Code == "TENANT_NOT_FOUND")
{
    // provision the tenant first
}
```

**Sources:** [IHttpClientFactory](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory) · [HTTP resilience](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience) · [Delegating handlers](https://www.milanjovanovic.tech/blog/extending-httpclient-with-delegating-handlers-in-aspnetcore) · [Async Request-Reply pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/asynchronous-request-reply) · repo: `Client.Rest/*`, `Contracts/V1/*`

## Architectural Patterns and Design

This section explains *how the RAG engine is built* and, crucially for the goals, *what an integrating developer can actually tune*. It is grounded in the server search code (`src/Hexalith.Memories.Server/Search/*`) and `docs/dev/consistency.md`, cross-checked against current hybrid-search/RAG practice.

### System Architecture Patterns

- **Event-sourced + persist-then-publish.** Memories builds on **Hexalith.EventStore**; writes go through the command/event pipeline, and projections/state apply events. Ingestion is a **Dapr Workflow** (durable, replay-safe: deterministic IDs, `context.CurrentUtcDateTime`, side effects only in activities). _Source:_ module `project-context.md`, `docs/dev/eventstore-integration.md`.
- **Per-tenant Dapr Actors** hold stateful singletons (embedding **rate limiter**, corpus stats/counters) — not static caches — so concurrency and limits are tenant-scoped.
- **Triple-write storage (the defining data-architecture decision).** Every memory unit is written to **three backends**: **RediSearch** (full-text / syntactic — *authoritative*), **Redis Vector** (semantic embeddings), **FalkorDB** (graph). The syntactic hash `{tenantId}:mu:{memoryUnitId}` is the **authoritative source of truth** (full content + metadata to rebuild the others). _Source:_ `docs/dev/consistency.md`.

### Data Architecture & Consistency Patterns

- Because writes fan out to three stores, **partial-failure divergence** is a first-class concern. A **consistency verify/repair** workflow detects presence divergence `(syntactic, semantic, graph) ≠ (T,T,T)` and maps each triple to a deterministic recommendation via `RepairPlanCalculator` (e.g. `T,F,T → ReIndexSemantic`; `F,T,T → RemoveOrphanedSemanticAndGraph`; `F,F,F → Unrepairable`). Verify is **read-only**; repair **re-verifies before acting**, caps at **3 convergence passes**, and is **never automatic**. _Source:_ `docs/dev/consistency.md`.
- _Consumer implication:_ ingestion may report a `ConsistencyNote` / leave a unit partially indexed; treat full searchability as eventually-consistent and (for compliance-critical flows) run `consistency verify` before export.

### Retrieval Architecture & the Tuning Surface ★

This is the heart of the research. A search executes per-axis, **normalizes each axis to [0,1]**, then **fuses by weighted average**:

**1. Per-axis normalization** (`Server/Search/ScoreNormalizer.cs`, pure/deterministic):

| Axis | Raw signal | Normalization to [0,1] |
|---|---|---|
| Syntactic | RediSearch **BM25** (unbounded) | **Corpus-adaptive saturation** `raw / (raw + k)`, `k = log2(docCount+1) · (avgDocLen/100)` → mid-range ≈ 0.5 regardless of corpus |
| Semantic | Redis Vector **cosine** similarity | Already [0,1] (distance→similarity upstream); defensive clamp |
| Graph | FalkorDB **proximity** | **Hop decay** `1/(1+hops)` → hop0=1.0, hop1=0.5, hop2≈0.33 |

**2. Weighted-average fusion** (`Server/Search/FusionEngine.cs`): composite = `Σ(weightᵢ · scoreᵢ) / Σ(active weightᵢ)`, clamped [0,1], summed over the **axes that were queried**. A unit missing from a queried axis contributes `0` for that axis **but its weight stays in the denominator** — so **units that match on more axes are rewarded** (multi-axis agreement boost). Ties break deterministically by `MemoryUnitId`. Default weights live in `FusionWeights`: **Syntactic 0.4 / Semantic 0.4 / Graph 0.2**.

**3. What you can actually tune (as an integrating developer), in priority order:**

1. **Axis selection** — single-axis (`syntactic` for exact keywords/IDs, `semantic` for meaning/paraphrase, `graph` for relationship proximity) vs **`hybrid`** (balanced recall). This is your biggest lever and is fully exposed on the REST/client API.
2. **`maxResults`** (default 10) and **`tokenBudget`** — cap result count / output tokens; the response reports `OmittedCount`, `OmittedReason`, `EstimatedTokensTotal` when truncated.
3. **`explain=true`** — returns `SearchExplanation` with per-axis `NormalizationMethod` (e.g. `bm25_saturation`), a relevance **`Caveat`** ("scores measure query-result relevance, not factual accuracy"), and `WeightsUsed`. Use it to *diagnose* why a result ranked where it did. _Source:_ `Contracts/V1/SearchExplanation.cs`.
4. **`caseId` scoping** — narrowing to a project/conversation improves precision and BM25 normalization stability.
5. **Embedding provider/model** (tenant configuration) — drives semantic quality and the vector `EmbeddingDimensions`; switching providers changes recall characteristics. _Source:_ `docs/dev/embedding-providers.md`.

> ⚠️ **Fusion weights are NOT a per-request parameter in the current REST/client surface.** The search endpoint constructs `new FusionWeights()` server-side (defaults 0.4/0.4/0.2); neither `SearchRequest`/`HybridSearchRequest` nor `BuildSearchPath` carries weights. To re-weight axes today you change the **server** (config/code), not the call. _Confidence: high_ — verified in `Server/Program.cs` (`var weights = new FusionWeights();`) and the client request DTOs. _If axis re-weighting per query matters to you, that's a server-side change / feature request, not a client option._

### Fusion Strategy: Trade-off vs the Industry Trend

Memories uses **normalized weighted-average fusion**. Current industry direction (Azure AI Search, OpenSearch, Elasticsearch, Milvus) increasingly favors **Reciprocal Rank Fusion (RRF)** because raw BM25 and cosine live on **incompatible scales**, making naive weighted averaging fragile and dataset-dependent. _Source:_ [Azure AI Search hybrid RRF](https://learn.microsoft.com/en-us/azure/search/hybrid-search-ranking), [RRF vs weighted fusion](https://www.maxpetrusenko.com/blog/rrf-vs-weighted-fusion-for-hybrid-ranking), [ParadeDB on RRF](https://www.paradedb.com/learn/search-concepts/reciprocal-rank-fusion).

**However**, Memories largely **neutralizes the classic weighted-average pitfall** by normalizing each axis to a comparable [0,1] range *before* weighting (corpus-adaptive BM25 saturation rather than raw BM25), and by averaging only over active axes. Net assessment (_confidence: medium_, design judgment): the approach is sound and tunable for in-corpus relevance; if you observe scale-driven false-negatives on diverse corpora, RRF would be the standard alternative — but that is a server-side engine change, not a consumer knob.

### Evaluation / Quality Patterns

Rank-aware **NDCG** (and Recall@k / MRR) is the standard way to measure retrieval quality and correlates better with end-to-end RAG quality than binary relevance; the module itself keeps **NDCG/scoring benchmarks** over a synthetic corpus (per module `project-context.md`). When you tune axis selection or provider, **measure per-stage** so you don't tune the wrong parameter. _Source:_ [RAG evaluation guide 2025](https://www.getmaxim.ai/articles/rag-evaluation-a-complete-guide-for-2025/), [NDCG for RAG](https://towardsdatascience.com/how-to-evaluate-retrieval-quality-in-rag-pipelines-part-3-dcgk-and-ndcgk/).

### Scalability & Performance Patterns

- **Graceful degradation:** if an axis backend is down, hybrid search returns `200` with `Degraded=true` + `UnavailableAxes` (and `AllEnabledAxesUnavailable` tri-state → `503` only when *every* attempted axis failed). Single-axis exposes a simpler `Degraded` boolean. Always inspect these before trusting result completeness.
- **Token-budget truncation** keeps responses LLM-friendly; **enumeration cap** (50k units/verify run) and **discrepancy truncation** (10k entries / ~1 MB Dapr workflow-state budget) bound large-tenant operations.
- **Embedding rate-limiter actor** throttles provider calls; re-index latency is dominated by embedding API calls (~100–500 ms/unit). _Source:_ `docs/dev/consistency.md`.

### Security Architecture

- **Physical tenant isolation** at every layer (per-tenant RediSearch/vector indexes, FalkorDB graphs, actor IDs, telemetry tags) — not query-filtered. **JWT** auth; **parameterized** graph queries (no Cypher string-concat); **payload/secret redaction** in logs and `ToString()`. Access telemetry audits exactly four operation types (search/ingest/traverse/case-access). _Source:_ module `project-context.md`, `docs/dev/consistency.md`, `docs/dev/telemetry.md`.

### Deployment & Operations Architecture

- **Aspire AppHost** owns local topology (Dapr component generation, Redis/FalkorDB endpoints, sidecar options). Health surface: **`/alive`** (liveness), **`/ready`** (backend reachable, <1s, tenant-unaware), **`/health`** (client probe). Consistency is explicitly *not* part of health. _Source:_ `docs/dev/health-checks.md`, `README.md`.

**Sources:** [Azure AI Search hybrid RRF](https://learn.microsoft.com/en-us/azure/search/hybrid-search-ranking) · [RRF vs weighted fusion](https://www.maxpetrusenko.com/blog/rrf-vs-weighted-fusion-for-hybrid-ranking) · [ParadeDB RRF](https://www.paradedb.com/learn/search-concepts/reciprocal-rank-fusion) · [RAG evaluation 2025](https://www.getmaxim.ai/articles/rag-evaluation-a-complete-guide-for-2025/) · [NDCG for RAG](https://towardsdatascience.com/how-to-evaluate-retrieval-quality-in-rag-pipelines-part-3-dcgk-and-ndcgk/) · repo: `Server/Search/{FusionEngine,ScoreNormalizer}.cs`, `docs/dev/consistency.md`

## Implementation Approaches and Technology Adoption

This step turns the architecture into a concrete adoption path for an integrating developer, with a runnable end-to-end walkthrough and the gotchas that bite first.

### Technology Adoption Strategy

- **Integrate over REST / the typed client; never reach the backends directly.** RediSearch, Redis Vector, and FalkorDB are server-owned. Your contract is `MemoriesClient` + `Contracts.V1`.
- **Pin client and server to a compatible `Contracts.V1`.** A version skew surfaces as an `INVALID_RESPONSE` error; treat the contract package version as a coupled dependency and bump both together.
- **Expect to opt into experimental surfaces today.** The core write methods — `CreateTenantAsync`, `CreateCaseAsync`, `IngestAsync`, `GetTelemetrySummaryAsync` — are **`[Experimental("HXL001")]`**; `ListHandlersAsync`/`GetHandlerMismatchesAsync` are **HXL002**. You must `#pragma warning disable HXL001` at the call site, which keeps the opt-in visible in review. `TraverseAsync`/`GetCaseAsync` are **stable since Story 10.2**. _Source:_ `docs/dev/experimental-apis.md`, `MemoriesClient.cs`. _Adoption implication:_ isolate these calls behind a thin internal façade so a Phase-1.5 signature change is a one-file edit.

### Development Workflow — End-to-End Walkthrough

The canonical lifecycle (mirrors the `memories quickstart` wizard): **provision tenant → wait Active → create case → ingest → wait searchable → search**.

```csharp
#pragma warning disable HXL001 // experimental quickstart-support surface (CreateTenant/CreateCase/Ingest)

public sealed class ProjectMemory(MemoriesClient memories, ILogger<ProjectMemory> log)
{
    public async Task<HybridSearchResult> CaptureAndRecallAsync(CancellationToken ct)
    {
        const string tenant = "acme";

        // 1. Provision tenant (202 + async workflow) — idempotent-ish: check first.
        if (await memories.GetTenantAsync(tenant, ct) is null)
        {
            await memories.CreateTenantAsync(tenant, "ACME Corp", ct);
            // 2. Poll until Active before writing anything.
            await PollUntilAsync(
                async () => (await memories.GetTenantAsync(tenant, ct))?.Status == TenantStatus.Active,
                ct);
        }

        // 3. Create a case = a "project / conversation" grouping.
        Case project = await memories.CreateCaseAsync(tenant, "Migration project", "Q2 platform migration", ct);

        // 4. Ingest a memory (202 — runs extract → embed → triple-index in the background).
        byte[] note = System.Text.Encoding.UTF8.GetBytes("Decision: roll back via blue/green if error rate > 2%.");
        string instanceId = await memories.IngestAsync(
            tenantId: tenant, caseId: project.Id,
            sourceUri: "note://decisions/2026-05-24",
            content: note, contentType: "text/plain",
            ingestedBy: "jerome",
            metadata: new Dictionary<string, MetadataField>
            {
                ["topic"] = new("deployment", MetadataOrigin.UserProvided, 1.0f),
            },
            ct);
        log.LogInformation("Ingestion workflow {Instance} accepted.", instanceId);

        // 5. Ingestion is eventually consistent — gate recall on the unit appearing, not on the 202.
        await PollUntilAsync(
            async () => (await memories.HybridSearchAsync(
                new HybridSearchRequest(tenant, "rollback", project.Id), ct)).TotalCount > 0,
            ct);

        // 6. Recall. Inspect Degraded/UnavailableAxes before trusting completeness.
        HybridSearchResult hits = await memories.HybridSearchAsync(
            new HybridSearchRequest(tenant, "how do we roll back a bad deploy?", project.Id, MaxResults: 5, Explain: true),
            ct);
        if (hits.Degraded)
            log.LogWarning("Degraded search; axes down: {Axes}", string.Join(",", hits.UnavailableAxes));
        return hits;
    }
}
#pragma warning restore HXL001
```

Implementation notes that prevent real bugs:

- **There is no client-exposed "ingestion status" call** — `IngestAsync` returns only the workflow `instanceId`, and you don't learn the `MemoryUnitId` from it. In practice, **gate dependent reads on a follow-up search returning the unit** (step 5) rather than assuming read-after-write. _Source:_ `MemoriesClient.cs`.
- **`SourceType` rules:** `IngestAsync` sends `SourceType.File` with `ContentBytes`; for URL ingestion the server fetches the body (bytes must be null). **Dedup is content-hash based** — re-ingesting identical bytes returns the existing unit (`WasDuplicate`).
- **Metadata carries provenance:** each `MetadataField(Value, Origin, Confidence)` — set `MetadataOrigin` honestly (it feeds confidence/governance).
- **Single-axis when you know the shape of the query:** `SearchAsync(new SearchRequest(tenant, "syntactic", "exact-error-code-123"))` for keyword/ID lookups; `"semantic"` for meaning; `"hybrid"` as the default balanced recall.

### Chunking & Content Quality (server-owned, but it affects your results)

Document **chunking happens server-side** in the ingestion pipeline (Kreuzberg extraction → chunk → embed); the consumer does **not** pass chunk-size parameters. Industry guidance (256–512-token recursive/semantic chunks, 10–20% overlap, smaller chunks for fact lookup) is therefore a **server-tuning** concern — but as a consumer you still control the two things that matter most upstream: **clean, well-structured source content** and **accurate metadata**. _Source:_ [Databricks chunking guide](https://community.databricks.com/t5/technical-blog/the-ultimate-guide-to-chunking-strategies-for-rag-applications/ba-p/113089), [Redis chunking](https://redis.io/blog/chunking-strategy-rag-pipelines/).

### Testing Your Integration

Because `MemoriesClient` is a concrete `virtual` typed client (Architecture D9), test at the **`HttpMessageHandler` boundary**, not by faking an interface — this exercises the real URL-building, auth-header, and error-decoding logic. Two standard patterns: a stub `HttpMessageHandler` injected via `ConfigurePrimaryHttpMessageHandler`, or a `WebApplicationFactory` that swaps the primary handler. _Source:_ [Stub typed HttpClients](https://mattfrear.com/2023/03/23/stub-typed-httpclients-in-asp-net-6-integration-tests/), [In-process HTTP mocking](https://github.com/edumserrano/dotnet-sdk-extensions/blob/main/docs/integration-tests/http-mocking-in-process.md). Cover the soft-miss paths (404 → `null`), `MemoriesRemoteException` codes, and `Degraded` handling.

### Deployment & Operations (what a consumer must stand up)

- **Local:** Docker Desktop + `dotnet run --project src/Hexalith.Memories.AppHost` boots Redis Stack, FalkorDB, and the Dapr sidecar via Aspire; `memories quickstart` validates the path end-to-end. _Source:_ `README.md`.
- **Config you own:** the `Endpoint`, the **API token** (via `HEXALITH_MEMORIES_API_TOKEN` / secret store — not argv), and the **per-tenant embedding provider** (Google API key or Ollama OIDC). _Source:_ `docs/dev/embedding-providers.md`, `MemoriesClientOptions.cs`.
- **Health:** wire readiness on `/ready`; use `MemoriesClient.ProbeHealthAsync` (5s) for a quick liveness gate before issuing calls.

### Cost & Resource Management

- **Embedding calls are the dominant cost/latency** — bounded by a per-tenant **rate-limiter actor**; re-indexing is ~100–500 ms/unit. Batch ingestion thoughtfully and prefer dedup over re-ingest. _Source:_ `docs/dev/consistency.md`.
- **`tokenBudget`** keeps search responses LLM-affordable (`OmittedCount`/`EstimatedTokensTotal` report truncation).
- **Three backends = three storage footprints** per tenant; large corpora hit enumeration/discrepancy caps (50k / 10k) on maintenance ops.

### Risk Assessment & Mitigation

| Risk | Severity | Mitigation |
|---|---|---|
| **HXL001/HXL002 experimental write methods** may change in Phase 1.5 | Medium | Façade the calls; pin contract version; track the Story 7.4/7.5 surface |
| **Prerelease pins** (`StackExchange.Redis 3.0.2-preview`, OTel `-beta.1`, Fluent UI RC) | Low–Med | Don't upgrade casually; follow the module's lock-step notes |
| **Eventual consistency** after ingest (no read-after-write) | Medium | Poll a follow-up search; don't key UX on the 202 |
| **Weighted-average fusion** can mis-rank on scale-incompatible signals | Low–Med | Normalization mitigates; use `explain`; escalate to a server RRF change if NDCG shows it |
| **No per-request fusion weights** | Low | Use axis selection; request server-side weight config if needed |
| **Contract version skew** | Medium | Co-version client/server `Contracts.V1`; handle `INVALID_RESPONSE` |

## Technical Research Recommendations

### Implementation Roadmap

1. **Stand up locally** — Aspire AppHost + `memories quickstart`; confirm `/ready` and a sample search.
2. **Register the client** — `AddMemoriesClient(...).AddStandardResilienceHandler()`; token via secret store; pin `Contracts.V1`.
3. **Model your domain** — one **Case per project/conversation**; decide your `sourceUri` scheme and metadata taxonomy up front.
4. **Wrap experimental writes** in an internal `IProjectMemoryStore` façade (provision/create-case/ingest) so HXL001 churn is contained.
5. **Implement the ingest→poll→search lifecycle**; gate recall on eventual consistency.
6. **Default to `hybrid`**, drop to single-axis for known query shapes; turn on `explain` while tuning.
7. **Add an offline relevance harness** (NDCG/Recall@k over representative queries) and vary one parameter at a time.
8. **Operationalize** — health gating, degraded-search handling, token budgets, and `consistency verify` before compliance exports.

### Technology Stack Recommendations (consumer side)

- Target **`net10.0`** if using the in-process client; otherwise any HTTP stack over REST.
- Use **`IHttpClientFactory` + one resilience handler** (`Microsoft.Extensions.Http.Resilience`); never `new HttpClient()`.
- Prefer the **managed Google embedding** path to start; move to **Ollama OIDC** only when self-hosting/data-residency requires it (and account for 2560-dim vectors).

### Skill Development Requirements

- **Core:** modern C#/.NET DI + typed `HttpClient`, async/cancellation, `System.Text.Json` (source-gen contracts).
- **Conceptual:** RAG retrieval (lexical vs vector vs graph), hybrid fusion & normalization, eventual consistency, multi-tenant isolation.
- **Awareness (not hands-on):** Dapr Workflow/Actors and Aspire topology — enough to read logs and reason about async/degradation, since you won't call them directly.

### Success Metrics & KPIs

- **Retrieval quality:** NDCG@k / Recall@k / Precision@k on a fixed query set; track per axis and for hybrid.
- **Latency:** p50/p95 search latency; ingestion-to-searchable time.
- **Reliability:** degraded-search rate (`Degraded=true`), consistency-divergence rate (verify findings).
- **Cost:** embedding calls per ingest, dedup hit-rate (`WasDuplicate`), tokens per search.

**Sources:** [RAG chunking best practices](https://community.databricks.com/t5/technical-blog/the-ultimate-guide-to-chunking-strategies-for-rag-applications/ba-p/113089) · [Redis chunking](https://redis.io/blog/chunking-strategy-rag-pipelines/) · [Stub typed HttpClients in tests](https://mattfrear.com/2023/03/23/stub-typed-httpclients-in-asp-net-6-integration-tests/) · [In-process HTTP mocking](https://github.com/edumserrano/dotnet-sdk-extensions/blob/main/docs/integration-tests/http-mocking-in-process.md) · [RAG evaluation 2025](https://www.getmaxim.ai/articles/rag-evaluation-a-complete-guide-for-2025/) · repo: `MemoriesClient.cs`, `docs/dev/experimental-apis.md`, `docs/dev/embedding-providers.md`

---

# Research Synthesis — Wiring Project Memory: A Developer's Guide to Hexalith.Memories for Conversation Memory & File Search (RAG)

## Executive Summary

By 2026, retrieval-augmented generation has become the default way enterprise software gives language models durable, trustworthy access to organizational knowledge — Gartner projects ~40% of enterprise applications will embed task-specific AI agents (up from <5% in 2025), most fronted by a RAG layer ([Techment](https://www.techment.com/blogs/rag-in-2026/), [Squirro](https://squirro.com/squirro-blog/state-of-rag-genai)). **Hexalith.Memories is exactly that layer for the Hexalith platform**: a multi-tenant, event-sourced memory service that ingests documents and returns ranked, explainable, tenant-isolated results across lexical, semantic, and graph axes. For an integrating developer the value proposition is concrete — you get persistent, searchable "project memory" without owning Redis, vectors, embeddings, or a graph database.

This research establishes that the **consumer contract is small and well-shaped**: a concrete typed **`MemoriesClient`** (or the equivalent REST routes) over `Contracts.V1`, organized around a three-level identity — **Tenant → Case → MemoryUnit**. You **create a Case per project/conversation, ingest each note or file as a memory unit, and search scoped by case or across the tenant**. Ingestion is **asynchronous** (HTTP 202 + a durable Dapr workflow that extracts via Kreuzberg, embeds, and **triple-writes** to RediSearch, Redis Vector, and FalkorDB), so recall is **eventually consistent** — a fact that must shape your UX. Retrieval normalizes each axis to [0,1] and fuses them by **weighted average** (defaults 0.4 syntactic / 0.4 semantic / 0.2 graph), rewarding multi-axis agreement and degrading gracefully when a backend is down.

The dominant practical caveats are honest and actionable: the **core write methods (`CreateTenantAsync`/`CreateCaseAsync`/`IngestAsync`) are `[Experimental("HXL001")]`** and should be wrapped behind a façade; **fusion weights are server-side defaults, not a per-request knob**; and **client/server `Contracts.V1` must be co-versioned**. Where Memories diverges from the 2026 industry trend toward Reciprocal Rank Fusion, it largely neutralizes the classic weighted-average pitfall through per-axis normalization. Net: the module is ready to integrate today for storing and recalling project/conversation memories and file search, provided you respect the experimental surface and eventual-consistency semantics.

**Key Technical Findings:**

- **Identity model = Tenant → Case → MemoryUnit (ULID).** A "project conversation memory store" maps cleanly to one **Case**; memories are `MemoryUnit`s carrying content, provenance metadata, and embedding lineage.
- **Triple-write storage with an authoritative source.** RediSearch (syntactic) is canonical; Redis Vector and FalkorDB are rebuildable. Partial-write divergence is reconciled by a read-only **verify** + explicit **repair** workflow.
- **Hybrid retrieval = normalize → weighted-average fuse.** Corpus-adaptive BM25 saturation, cosine clamp, `1/(1+hops)` graph decay; composite over *queried* axes; default weights 0.4/0.4/0.2; deterministic tie-break.
- **Async ingestion (202 + workflow), eventually consistent.** No client read-after-write and no client-side ingestion-status call — gate dependent reads on a follow-up search.
- **Tuning is consumer-accessible but bounded:** axis selection, `maxResults`/`tokenBudget`, `explain`, `caseId`, embedding provider — **but not fusion weights**.
- **Security is structural:** physical per-tenant isolation across all backends, JWT auth, transport-aware token handling, parameterized graph queries, payload/secret redaction.
- **Adoption risk concentrates in `[Experimental("HXL001/HXL002)]` methods and prerelease pins**, not in the stable read/search surface.

**Technical Recommendations (top 5):**

1. **Integrate via the typed client over REST; never touch the backends.** Register with `AddMemoriesClient(...).AddStandardResilienceHandler()`, token from a secret store, and **co-version `Contracts.V1`**.
2. **Wrap experimental writes** (`CreateTenant`/`CreateCase`/`Ingest`) behind a thin internal façade so a Phase-1.5 signature change is a one-file edit.
3. **Design for eventual consistency:** implement the **ingest → poll-until-searchable → search** lifecycle; never key UX on the 202.
4. **Default to `hybrid`, switch to single-axis for known query shapes, and keep `explain` on while tuning;** measure relevance with **NDCG/Recall@k**, varying one parameter at a time.
5. **Always inspect `Degraded`/`UnavailableAxes`** before trusting result completeness, and run **`consistency verify`** ahead of compliance-critical exports.

## Table of Contents

1. **Research Overview & Scope Confirmation** *(top of document)* — goals, refined scope, methodology
2. **Technology Stack Analysis** — runtime, Dapr/Aspire, Redis Stack, FalkorDB, Kreuzberg, embeddings, observability
3. **Integration Patterns Analysis** — identity model, REST surface, typed-client DI, auth, async 202+polling, errors, tenant-isolation obligations
4. **Architectural Patterns and Design** — event-sourced triple-write, consistency, **retrieval & the tuning surface**, fusion trade-off vs RRF, evaluation, scalability, security, ops
5. **Implementation Approaches and Technology Adoption** — end-to-end walkthrough, chunking, testing, deployment, cost, **risk table**
6. **Technical Research Recommendations** — roadmap, stack, skills, success metrics
7. **Research Synthesis** *(this section)* — executive summary, narrative, methodology & sources, conclusion

## Narrative Introduction — Why This Matters Now

The 2026 practitioner consensus is blunt: *"vanilla RAG fails for agentic use cases"* ([dev.to: State of AI Agent Memory](https://dev.to/vektor_memory_43f51a32376/the-state-of-ai-agent-memory-in-2026-what-the-research-actually-shows-3aja)). What separates a toy retriever from production "memory" is precisely what Hexalith.Memories invests in: **multi-tenant isolation, explainable ranking, graceful degradation, content provenance, and consistency guarantees across heterogeneous stores**. The enterprise priorities driving RAG adoption — accuracy, explainability, compliance, cost efficiency ([Techment](https://www.techment.com/blogs/rag-in-2026/)) — line up one-for-one with the module's design choices (the `SearchExplanation` caveat, audited access telemetry, the authoritative-source consistency model, and `tokenBudget` cost control).

For a developer, the strategic implication is that **you should consume these guarantees rather than rebuild them**. The research deliberately scoped to the integrating-developer perspective over the **.NET client and REST surfaces** because that is where the leverage is: a compact, well-documented contract hides a substantial distributed system (Dapr workflows, three storage engines, an embedding pipeline). The remainder of this document is the map of that contract and the system behind it — enough to adopt confidently and tune deliberately.

## Methodology & Source Verification

**Approach.** Two evidence classes were combined: (a) **module ground truth** read directly from the Hexalith.Memories repository — `Contracts.V1`, `Client.Rest`, `Server/Search/*`, `Program.cs`, and `docs/dev/*` — which is authoritative for module behavior; and (b) **current public web sources** for the underlying technologies and industry patterns, each verified against live documentation. Module-specific claims cite repo paths; technology and pattern claims cite URLs. Where the two could conflict, repo source wins for "how it works" and web sources inform "how the industry does it / why."

**Confidence & limitations.** High confidence on the verified facts (package versions, client method shapes, fusion/normalization formulas, the experimental-API surface — all read from source). Medium confidence on the *design-judgment* call that normalized weighted-average fusion is adequate vs RRF (reasoned, not benchmarked here). Not independently re-run: the module's own NDCG benchmarks and clean-machine quickstart timings (taken from repo docs). The MCP and CLI surfaces and the Conversations module were intentionally out of scope.

**Primary web sources (by theme):**

- *Storage & retrieval tech:* [Kreuzberg](https://kreuzberg.dev/) · [Kreuzberg GitHub](https://github.com/kreuzberg-dev/kreuzberg) · [Redis vector search](https://redis.io/tutorials/howtos/solutions/vector/getting-started-vector/) · [Redis hybrid FT.SEARCH](https://oneuptime.com/blog/post/2026-03-31-redis-ft-hybrid-vector-text-search/view) · [FalkorDB docs](https://docs.falkordb.com/) · [FalkorDB GitHub](https://github.com/FalkorDB/FalkorDB)
- *Orchestration & integration:* [Dapr Workflow overview](https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-overview/) · [IHttpClientFactory](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory) · [HTTP resilience](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience) · [Async Request-Reply pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/asynchronous-request-reply) · [Stub typed HttpClients](https://mattfrear.com/2023/03/23/stub-typed-httpclients-in-asp-net-6-integration-tests/)
- *Fusion, evaluation & RAG strategy:* [Azure AI Search hybrid RRF](https://learn.microsoft.com/en-us/azure/search/hybrid-search-ranking) · [RRF vs weighted fusion](https://www.maxpetrusenko.com/blog/rrf-vs-weighted-fusion-for-hybrid-ranking) · [ParadeDB RRF](https://www.paradedb.com/learn/search-concepts/reciprocal-rank-fusion) · [RAG evaluation 2025](https://www.getmaxim.ai/articles/rag-evaluation-a-complete-guide-for-2025/) · [NDCG for RAG](https://towardsdatascience.com/how-to-evaluate-retrieval-quality-in-rag-pipelines-part-3-dcgk-and-ndcgk/) · [RAG chunking](https://community.databricks.com/t5/technical-blog/the-ultimate-guide-to-chunking-strategies-for-rag-applications/ba-p/113089)
- *Significance / market:* [RAG in 2026](https://www.techment.com/blogs/rag-in-2026/) · [State of AI Agent Memory 2026](https://dev.to/vektor_memory_43f51a32376/the-state-of-ai-agent-memory-in-2026-what-the-research-actually-shows-3aja) · [Squirro: State of RAG](https://squirro.com/squirro-blog/state-of-rag-genai)

**Key repository sources:** `Directory.Packages.props`; `Client.Rest/{MemoriesClient,MemoriesClientServiceCollectionExtensions,MemoriesClientOptions,MemoriesAuthHandler,MemoriesRemoteException}.cs`; `Contracts/V1/{IngestionInput,IngestionResult,MemoryUnit,Case,SearchResult,HybridSearchResult,ScoredResult,FusionWeights,SearchExplanation,ErrorResponse,MetadataField,SourceType}.cs`; `Server/Search/{FusionEngine,ScoreNormalizer}.cs`; `Server/Program.cs`; `docs/dev/{consistency,embedding-providers,experimental-apis}.md`; `README.md`.

## Conclusion

### Summary of Key Findings

Hexalith.Memories gives an integrating developer a **small, explainable contract over a substantial RAG system**. The mental model is three levels deep (Tenant → Case → MemoryUnit); the workflow is *create case → ingest → poll → search*; the retrieval is hybrid with transparent normalization and graceful degradation; and the guarantees an enterprise cares about — isolation, provenance, auditability, consistency — are built in rather than bolted on.

### Strategic Impact Assessment

Adopting Memories converts "project conversation memory + file search" from a build problem into an **integration problem**, and a modest one: register a typed client, model your cases, and respect two semantics (eventual consistency, experimental writes). The chief strategic watch-items are the **`[Experimental]` write surface** (contain it behind a façade) and **fusion configurability** (axis selection today; per-request weights or RRF would be a server-side evolution).

### Next-Step Recommendations

1. Spike the end-to-end walkthrough locally against the Aspire AppHost; confirm `/ready` and a hybrid search round-trip.
2. Define your **Case/`sourceUri`/metadata** conventions before writing production code.
3. Implement the **façade + ingest→poll→search lifecycle** and an **NDCG relevance harness**; instrument degraded-search and dedup rates.
4. Decide the **embedding provider** (managed Google to start) and lock **client/server `Contracts.V1`** versions in CI.

---

**Technical Research Completion Date:** 2026-05-24
**Research Scope:** Integrating-developer guide to Hexalith.Memories (REST + .NET client) for conversation-memory storage/recall and file search (RAG); Memories module standalone.
**Source Verification:** Module claims read from repo source; technology/pattern claims verified against current public sources and cited inline.
**Technical Confidence Level:** High on verified module/technology facts; Medium on fusion-strategy design judgment.

_This document is an authoritative, source-verified reference for adopting Hexalith.Memories as a project conversation-memory and file-search (RAG) layer, and a basis for informed integration decisions._

<!-- Content will be appended sequentially through research workflow steps -->
