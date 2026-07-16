---
stepsCompleted: [1, 2, 3, 4, 5, 6]
inputDocuments:
  - 'Hexalith.Folders/src/Hexalith.Folders.Contracts/openapi/hexalith.folders.v1.yaml'
  - 'Hexalith.Folders/src/Hexalith.Folders.Client/Generated/HexalithFoldersClient.g.cs'
  - 'Hexalith.Folders/src/Hexalith.Folders.Server/FoldersServerModule.cs'
  - 'Hexalith.Folders/Directory.Build.props'
  - '_bmad-output/project-context.md'
  - 'Hexalith.Folders/_bmad-output/project-context.md'
workflowType: 'research'
lastStep: 6
research_type: 'technical'
research_topic: 'How to use Hexalith.Folders to manage folder references in Hexalith.Projects'
research_goals: 'Produce a practical integration how-to: build/reference wiring + folder domain model + runtime consumption via the typed SDK Client and the REST API.'
user_name: 'Jerome'
date: '2026-05-24'
web_research_enabled: true
source_verification: true
---

# Research Report: Technical

**Date:** 2026-05-24
**Author:** Jerome
**Research Type:** Technical (internal-module integration)

---

## Research Overview

This report answers **"How to use `Hexalith.Folders` to manage folder references in `Hexalith.Projects`?"** as a practical **integration how-to**, grounded in the actual module source rather than the public web.

`Hexalith.Folders` is an **internal module** of the `Hexalith.Projects` umbrella repository (a root-level Git submodule), so the authoritative sources are the module's OpenAPI Contract Spine, its generated client, its server wiring, and its `project-context.md` — not third-party documentation. Public web sources are used only to ground **general .NET integration patterns** (typed-`HttpClient` registration, bearer-token delegating handlers), since the consumer-side client registration pattern does not yet exist in the repo and must be recommended.

**Methodology:**

- **Primary source = code.** Every concrete claim cites a file path (and line where verified) inside `Hexalith.Folders`.
- **Current-state honesty.** Where the contract/client surface is *ahead of* the wired server implementation (a real and important gap), this is called out explicitly rather than papered over.
- **Web grounding for recommendations only.** The proposed client DI registration is validated against current ASP.NET Core / NSwag guidance.

> ⚠️ **Headline finding (read first):** The module is in a **scaffold / phased** state. The OpenAPI Contract Spine and the NSwag-generated typed client expose ~20 folder operations (create, repository-backed create, repository bindings, provider bindings, ACL writes, workspace lock/release, queries). The **running server currently maps only a subset externally**: `GET .../lifecycle-status`, `POST .../archive`, `GET .../effective-permissions`, plus the internal EventStore `/process` pipeline. Mutations such as `CreateFolder` and `GrantFolderAccess` exist as domain commands/aggregates and flow through `/process`, but are **not yet exposed as external `/api/v1` REST endpoints**. Plan integration against the **typed client interface** (stable contract) while treating un-mapped operations as "contract-ready, server-pending."

---

## Technical Research Scope Confirmation

**Research Topic:** How to use Hexalith.Folders to manage folder references in Hexalith.Projects
**Research Goals:** Practical integration how-to covering build/reference wiring + folder domain model + runtime consumption.

**Confirmed scope (via clarification):**

- **Meaning of "folder references":** *All of the above* — build/reference wiring **and** the folder domain model (folder identity, parent references, repository bindings, ACL references) **and** runtime consumption.
- **Consumer surface focus:** **SDK Client (typed)** as the primary path, with **REST API** as the parallel transport.
- **Output:** Integration how-to guide with code/config examples grounded in the module.

**Technical Research Scope:**

- Architecture Analysis — EventStore command pipeline, Dapr boundary, OpenAPI Contract Spine, tenant isolation, layered authorization.
- Implementation Approaches — build wiring, typed-client DI, REST consumption, idempotency/correlation/auth headers.
- Technology Stack — .NET 10, Dapr 1.17.7, Aspire, NSwag-generated client, EventStore foundation.
- Integration Patterns — project references vs. NuGet, SDK vs. REST, internal `/process` vs. external `/api/v1`.
- Current-State Considerations — what is wired today vs. contract-ready.

**Scope Confirmed:** 2026-05-24

---

## 1. Technical Overview — What `Hexalith.Folders` Is and What a "Folder Reference" Means

### 1.1 Module purpose

`Hexalith.Folders` is a **tenant-scoped, repository-backed folder-management module built on `Hexalith.EventStore`**. Its package description: *"Tenant-scoped repository-backed folder management module for .NET built on Hexalith.EventStore"* (`Hexalith.Folders/Directory.Build.props:25-33`).

A "folder" is an **event-sourced aggregate** that can be:

- created (optionally under a **parent folder**),
- bound to a **Git repository** (GitHub / Forgejo via provider ports),
- governed by a **metadata-only ACL** (grants/revokes per principal + action),
- prepared/locked as a **workspace** (single-active-writer), and
- archived.

### 1.2 What "folder references" concretely are

There is **no single "link" operation**. "Managing folder references" decomposes into five distinct reference concepts in the domain model:

| Reference kind | How it is modeled | Source |
|---|---|---|
| **Folder identity** | Canonical stream key `{managedTenantId}:folders:{folderId}` | `src/Hexalith.Folders/Aggregates/Folder/FolderStreamName.cs:52` |
| **Parent reference (hierarchy)** | Optional `parentFolderId` on `CreateFolderRequest` (an `OpaqueIdentifier`) | OpenAPI `CreateFolderRequest` (spine ~lines 7133-7147) |
| **Organization reference** | Separate stream `{managedTenantId}:organizations:{organizationId}` | `src/Hexalith.Folders/Aggregates/Organization/OrganizationStreamName.cs:45` |
| **Repository binding (provider ref)** | `BindRepository` / `CreateRepositoryBackedFolder` + `ConfigureProviderBinding` | OpenAPI ops; client methods (below) |
| **Access/ACL reference** | `FolderAccessEntryKey(ManagedTenantId, FolderId, PrincipalKind, PrincipalId, Action)` | `src/Hexalith.Folders/Aggregates/Folder/FolderAccessEntryKey.cs:3-35` |

Identity rules to respect (from `project-context.md` and code):

- The reserved **`system`** tenant must **never** back a managed folder stream. `managedTenantId` and `folderId` match `^[a-z0-9._-]+$`, max 256 chars (`FolderStreamName.cs:70`).
- All references use **`OpaqueIdentifier`** on the wire: ULID-shaped, 16–128 chars, `^[A-Za-z0-9][A-Za-z0-9_-]{15,127}$` — *"Not a path, credential, token, or tenant authority"* (OpenAPI `OpaqueIdentifier`).
- Tenant authority is **never** taken from a payload/header/query — only from the authenticated principal + EventStore claim-transform evidence (`docs/contract/contract-spine-foundation.md`).

### 1.3 Technology stack (consumer-relevant)

- **.NET 10** (`global.json` SDK `10.0.302`, `rollForward: latestPatch`), nullable + implicit usings + warnings-as-errors.
- **EventStore foundation** for write-side commands/events/projections — do not introduce a parallel write model.
- **Dapr `1.17.7`** as the only infrastructure abstraction (pub/sub `pubsub`, state `statestore`, service invocation). Stable Dapr app IDs: `eventstore`, `tenants`, `folders`, `folders-workers`, `folders-ui`.
- **OpenAPI Contract Spine** (OpenAPI 3.1) at `src/Hexalith.Folders.Contracts/openapi/hexalith.folders.v1.yaml` — the single source of truth; the client is **NSwag-generated** from it (`Hexalith.Folders.Client/nswag.json`).
- Central package management; **opt-in packaging** (`Directory.Build.props:19-22`).

### 1.4 Packable surface (what a consumer can reference)

Six projects set `<IsPackable>true</IsPackable>` and are the legitimate consumption surface:

| Project | Role |
|---|---|
| `Hexalith.Folders.Contracts` | DTOs, events, commands, models, **OpenAPI spine** (low-dependency) |
| `Hexalith.Folders.Client` | Typed SDK client (NSwag-generated) — **primary integration path** |
| `Hexalith.Folders` | Core domain library |
| `Hexalith.Folders.ServiceDefaults` | Aspire shared defaults |
| `Hexalith.Folders.Testing` | Reusable test utilities |
| `Hexalith.Folders.Cli` | CLI tool (`folders`) — currently a scaffold |

Host/adapter projects (`Server`, `AppHost`, `Aspire`, `Workers`, `UI`, `Mcp`) are **not packable** and are consumed only at the orchestration (AppHost) layer.

---

## 2. Integration Patterns

Three layers, addressed in order: **(A) build/reference wiring**, **(B) typed SDK Client consumption**, **(C) REST consumption**, plus **(D) cross-cutting concerns** (auth, tenant, idempotency, correlation).

### 2.A Build / reference wiring (how a consumer adds Folders)

**Current reality:** No other umbrella module references `Hexalith.Folders` today — searches of all sibling `.csproj`/`.props` returned zero references. Folders is consumed **only at the umbrella/AppHost orchestration level**. `Hexalith.Projects` itself has no dedicated product module/app yet; it is the umbrella workspace. So the wiring below is the **established pattern by analogy** to how Folders itself consumes EventStore/Tenants — follow it for any new consumer.

**The established root-detection pattern** (`Hexalith.Folders/Directory.Build.props:1-8`): a module locates a sibling either **nested** (as its own submodule) or at the **umbrella root**, with a graceful fallback:

```xml
<!-- Pattern Folders uses for EventStore/Tenants; replicate for a Folders consumer -->
<HexalithFoldersRoot Condition="'$(HexalithFoldersRoot)' == '' and Exists('$(MSBuildThisFileDirectory)Hexalith.Folders\src\Hexalith.Folders.Contracts')">$(MSBuildThisFileDirectory)Hexalith.Folders</HexalithFoldersRoot>
<HexalithFoldersRoot Condition="'$(HexalithFoldersRoot)' == '' and Exists('$(MSBuildThisFileDirectory)..\Hexalith.Folders\src\Hexalith.Folders.Contracts')">$(MSBuildThisFileDirectory)..\Hexalith.Folders</HexalithFoldersRoot>
```

Then reference the **packable** projects you need (mirrors `Hexalith.Folders.Server.csproj:16-18`, which references sibling `*.Client`/`*.Contracts` via the resolved root variable):

```xml
<ItemGroup>
  <ProjectReference Include="$(HexalithFoldersRoot)\src\Hexalith.Folders.Contracts\Hexalith.Folders.Contracts.csproj" />
  <ProjectReference Include="$(HexalithFoldersRoot)\src\Hexalith.Folders.Client\Hexalith.Folders.Client.csproj" />
</ItemGroup>
```

**Submodule policy (critical):** Folders is registered as a root-level submodule of the umbrella (`.gitmodules:16-18`). Folders **itself contains nested submodules** (EventStore, Tenants, FrontComposer, Memories, Commons, AI.Tools — `Hexalith.Folders/.gitmodules`). Per `CLAUDE.md`, initialize **only root-level** submodules — **never** `--recursive`:

```text
git submodule update --init Hexalith.AI.Tools Hexalith.Commons Hexalith.EventStore Hexalith.FrontComposer Hexalith.Memories Hexalith.Tenants
```

**Reference decision:**

- **Inside the umbrella** → `ProjectReference` via the root-detection variable (above). Preferred for co-developed modules.
- **External / decoupled** → consume the published NuGet packages (`Hexalith.Folders.Contracts`, `Hexalith.Folders.Client`). Versions belong in the nearest `Directory.Packages.props` (central package management), never inline.

### 2.B Typed SDK Client (primary path)

The client is **NSwag-generated** (`src/Hexalith.Folders.Client/Generated/HexalithFoldersClient.g.cs`), exposing interface `IClient` and implementation `Client` in namespace `Hexalith.Folders.Client.Generated`. The constructor injects an `HttpClient` (`:1308`) and the generator was configured with **`useBaseUrl: false`** (`:25`) — i.e. it is purpose-built to be a **typed `HttpClient`** whose `BaseAddress` is supplied by DI.

> ⚠️ **There is no `AddFoldersClient(...)` helper.** `FoldersClientModule.cs` contains only the module name. The consumer **must register the typed client and base address itself** (and attach auth). Do not hand-edit anything under `Generated/` — change the OpenAPI spine and regenerate instead.

**Representative method signatures** (from the generated client):

```csharp
// Create — note: contract-ready; external REST POST not yet mapped server-side (see §5)
Task<AcceptedCommand> CreateFolderAsync(
    string idempotency_Key, string x_Correlation_Id, string x_Hexalith_Task_Id,
    CreateFolderRequest body, CancellationToken cancellationToken);   // CreateFolderRequest: requestSchemaVersion, parentFolderId?, folderMetadata

// Queries — wired today
Task<FolderLifecycleStatus> GetFolderLifecycleStatusAsync(
    string folderId, string x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness);
Task<EffectivePermissions> GetEffectivePermissionsAsync(
    string folderId, string x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness);

// Mutation — wired today
Task<AcceptedCommand> ArchiveFolderAsync(
    string folderId, string idempotency_Key, string x_Correlation_Id,
    string x_Hexalith_Task_Id, ArchiveFolderRequest body);

// ACL / references — contract-ready
Task<FolderAclEntryList> ListFolderAclEntriesAsync(string folderId, string x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness, string cursor, int? limit, string filter);
Task<AcceptedCommand> UpdateFolderAclEntryAsync(string folderId, string aclEntryId, string idempotency_Key, string x_Correlation_Id, string x_Hexalith_Task_Id, UpdateFolderAclEntryRequest body);

// Repository / provider references — contract-ready
Task<AcceptedCommand> CreateRepositoryBackedFolderAsync(...);
Task<AcceptedCommand> BindRepositoryAsync(string folderId, ...);
Task<AcceptedCommand> ConfigureProviderBindingAsync(string providerBindingRef, ...);
```

**Mutations return `AcceptedCommand` (HTTP 202)** — the API is **command/async**: a 202 means *accepted for processing*, not *applied*. Read back state via `GetFolderLifecycleStatusAsync` (optionally with a freshness hint). This is the persist-then-publish / CQRS shape of the EventStore foundation.

### 2.C REST API (parallel transport)

External REST is `/api/v1/...`; internal EventStore invocation is `/process` and `/project`. **Currently mapped external routes** (`FoldersDomainServiceEndpoints.cs`, wired via `FoldersServerModule.MapFoldersServerEndpoints` → `MapFoldersDomainServiceEndpoints`):

| Method | Route | Purpose | Required headers |
|---|---|---|---|
| `GET` | `/api/v1/folders/{folderId}/lifecycle-status` | Folder lifecycle + binding status | `X-Correlation-Id`; optional `X-Hexalith-Freshness` |
| `GET` | `/api/v1/folders/{folderId}/effective-permissions` | Caller's effective permissions | `X-Correlation-Id`; optional `X-Hexalith-Freshness` |
| `POST` | `/api/v1/folders/{folderId}/archive` | Archive folder (mutating) | `Idempotency-Key`, `X-Correlation-Id`, `X-Hexalith-Task-Id` |
| `POST` | `/process` | Internal EventStore domain-service pipeline | (service-to-service) |
| `POST` | `/project` | Projection endpoint — **stub** (returns Problem) | — |

The full operation set (CreateFolder, repository-backed create, repository/provider bindings, ACL PUT, workspace lock/release) is **defined in the spine** but not yet mapped as external REST. Prefer the typed client so your call sites are insulated from this transport-level gap.

### 2.D Cross-cutting: auth, tenant, idempotency, correlation

These apply to **every** call, SDK or REST:

- **Authentication:** JWT bearer (OIDC; Keycloak realm `hexalith` in the AppHost, audience `hexalith-eventstore` in dev). The server refuses to start outside Development without an auth scheme (`FoldersAuthSchemeValidator`). Validation params (`docs/exit-criteria/s2-oidc-validation.md`): validate issuer/audience/lifetime/signing-key, `ClockSkew = 30s`, JWKS refresh 10 min.
- **Tenant authority** comes from authenticated claims + EventStore claim-transform evidence (`eventstore:tenant`, `eventstore:permission`), **never** from payload/header/query — those are comparison inputs only.
- **Idempotency:** `Idempotency-Key` is **required on mutations, rejected on queries**. Same key + equivalent payload ⇒ same logical result (`IdempotentReplay`); same key + different payload ⇒ `idempotency_conflict`. Equivalence is **field-scoped** per operation (e.g. CreateFolder: `folder_metadata.display_name`, `parent_folder_id`, `request_schema_version`), not whole-body. A canonical SHA-256 hasher (`HexalithIdempotencyHasher.Compute`) backs cross-surface (SDK/CLI/MCP) parity.
- **Correlation/task:** `X-Correlation-Id` (auto-generated if absent) and `X-Hexalith-Task-Id` (caller-provided for task-scoped ops) thread through events, logs, and Problem responses.
- **Errors:** RFC 9457 Problem Details + Hexalith extensions (`category`, `code`, `message`, `correlationId`, `retryable`, `clientAction`, `details.visibility`). The generated client throws `HexalithFoldersApiException`. Denials are **safe**: unauthorized vs. nonexistent are indistinguishable at the boundary (both 404) to prevent cross-tenant existence leakage.

---

## 3. Architectural Patterns (what consuming Folders commits you to)

- **CQRS / command-async.** Writes are commands accepted (202 `AcceptedCommand`) then processed; reads are separate queries with optional read-consistency (`ReadConsistencyClass` freshness hints). Never assume read-after-write without a freshness hint or poll.
- **Persist-then-publish on EventStore.** Folder state changes are events (`FolderCreated`, `FolderAccessGranted`, …) appended to the `{tenant}:folders:{id}` stream; projections build read models. Consumers must not bypass the EventStore command pipeline or write folder state directly.
- **Dapr-only infrastructure boundary.** A consumer talks to Folders over HTTP (typed client/REST) or, in-topology, via Dapr service invocation/pub-sub. Do not reach for Redis/Postgres/broker clients.
- **OpenAPI Contract Spine is law.** The spine drives the generated client, parity oracle, and idempotency helpers; all of these are kept in sync with hashes (`HexalithFoldersIdempotencyHelpers.g.cs`). Treat the **spine + typed client interface** as your stable contract.
- **Layered authorization (contractual order):** JWT → EventStore claim transform → tenant-access projection freshness → folder ACL → EventStore validator → Dapr deny-by-default. As a consumer you supply a valid token; the server enforces the chain and short-circuits with safe denials.
- **Tenant isolation everywhere.** Folder/organization streams, projection keys, ACL keys, pub/sub topics, logs — all tenant-scoped. The `system` tenant is platform-only.
- **Metadata-only discipline.** Events, logs, Problem details, and console responses never carry file contents, secrets, tokens, raw diffs, or absolute paths. Honor this in anything you log about Folders responses.

---

## 4. Implementation Guide (step-by-step)

### Step 1 — Wire the build reference

In your consuming module's `Directory.Build.props`, add the `HexalithFoldersRoot` detection (see §2.A), then `ProjectReference` `Hexalith.Folders.Client` (+ `Hexalith.Folders.Contracts` if you need the DTOs/enums directly). Initialize root-level submodules only.

### Step 2 — Register the typed client (recommended pattern)

No helper ships today; register it as a standard typed client. This is the current ASP.NET Core / NSwag-recommended approach, and matches the generated client's `useBaseUrl: false` + `injectHttpClient` shape:

```csharp
using Hexalith.Folders.Client.Generated;

// options bound from configuration (base address of the Folders server / Dapr invoke URL)
builder.Services
    .AddHttpClient<IClient, Client>(http =>
    {
        http.BaseAddress = new Uri(builder.Configuration["Folders:BaseAddress"]!);
    })
    .AddHttpMessageHandler<FoldersBearerTokenHandler>(); // attach JWT (see Step 3)
```

In an Aspire topology you would instead resolve the address via a service reference to the `folders` app id / Dapr service invocation, consistent with how the AppHost wires resources.

### Step 3 — Attach the bearer token via a DelegatingHandler

Keep token acquisition out of call sites (current .NET best practice — token logic in a handler, not on a shared client):

```csharp
public sealed class FoldersBearerTokenHandler(IFoldersTokenProvider tokens) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var token = await tokens.GetAccessTokenAsync(ct).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, ct).ConfigureAwait(false);
    }
}
```

Register `FoldersBearerTokenHandler` and your `IFoldersTokenProvider` (client-credentials or on-behalf-of, issued by the same Keycloak realm with audience the Folders server accepts).

### Step 4 — Manage folder references (call patterns)

```csharp
public sealed class FolderReferenceService(IClient folders)
{
    // Create a folder, optionally under a parent (parent reference)
    public Task<AcceptedCommand> CreateAsync(string? parentFolderId, FolderMetadata metadata, CancellationToken ct)
        => folders.CreateFolderAsync(
            idempotency_Key: Ulid.NewUlid().ToString(),     // 16–128 char opaque, stable per logical attempt
            x_Correlation_Id: Activity.Current?.Id ?? Ulid.NewUlid().ToString(),
            x_Hexalith_Task_Id: Ulid.NewUlid().ToString(),
            body: new CreateFolderRequest { RequestSchemaVersion = "v1", ParentFolderId = parentFolderId, FolderMetadata = metadata },
            cancellationToken: ct);

    // Read back state (don't assume read-after-write)
    public Task<FolderLifecycleStatus> StatusAsync(string folderId, CancellationToken ct)
        => folders.GetFolderLifecycleStatusAsync(folderId, Activity.Current?.Id ?? Ulid.NewUlid().ToString(), x_Hexalith_Freshness: null);

    // Manage an access reference (ACL grant/revoke)
    public Task<AcceptedCommand> SetAccessAsync(string folderId, string aclEntryId, UpdateFolderAclEntryRequest req, CancellationToken ct)
        => folders.UpdateFolderAclEntryAsync(folderId, aclEntryId, Ulid.NewUlid().ToString(), /*corr*/ Ulid.NewUlid().ToString(), /*task*/ Ulid.NewUlid().ToString(), req);

    // Manage a repository reference (bind a repo to a folder)
    public Task<AcceptedCommand> BindRepoAsync(string folderId, BindRepositoryRequest req, CancellationToken ct)
        => folders.BindRepositoryAsync(folderId, Ulid.NewUlid().ToString(), Ulid.NewUlid().ToString(), Ulid.NewUlid().ToString(), req);
}
```

Key rules baked into the above:

- **One stable `Idempotency-Key` per logical attempt** (reuse it on retry; don't regenerate). Equivalence is field-scoped per operation.
- **202 ≠ done.** Treat `AcceptedCommand` as accepted; confirm via `lifecycle-status`.
- **Use `OpaqueIdentifier`-shaped ids** for `folderId`, `parentFolderId`, `aclEntryId` (ULID-shaped, 16–128 chars). They are not paths/credentials/tenant authority.
- **Never pass tenant id as authority** — it comes from the token.

### Step 5 — Handle errors and denials

Catch `HexalithFoldersApiException`; branch on the Problem `category`/`code` and `clientAction` (`retry`, `revise_request`, `check_credentials`, `wait_for_reconciliation`, `contact_operator`, `no_action`). Treat 404 as **either** not-found **or** access-denied (safe denial) — do not infer existence. Honor `retryable` before retrying.

### Step 6 — Run it locally

Bring up the topology via the AppHost (`Hexalith.Folders.AppHost`): it starts `eventstore`, `tenants`, `folders`, `folders-workers`, `folders-ui`, Keycloak (realm `hexalith`, dev port 8180), and Dapr sidecars with `statestore` (Redis) + `pubsub`. AppHost fails fast if `DaprComponents/accesscontrol.yaml` is missing. Build/verify from the module root: `dotnet restore Hexalith.Folders.slnx && dotnet build Hexalith.Folders.slnx --no-restore`. Point your consumer's `Folders:BaseAddress` at the running `folders` endpoint.

---

## 5. Current-State Caveats (must-read before committing scope)

1. **Contract is ahead of the server.** Today only `archive`, `lifecycle-status`, `effective-permissions` are external REST endpoints. `CreateFolder`, ACL `PUT`, repository/provider bindings, and workspace lock/release are **defined in the spine + generated client** but **not yet mapped to `/api/v1`**. They process internally via `/process`. If your integration needs them externally now, that is a server story to land first.
2. **`AddFoldersClient` DI helper — now added (uncommitted).** Originally absent; a typed-client registration extension (`FoldersClientServiceCollectionExtensions.AddFoldersClient` + `FoldersClientOptions`) has since been drafted in `Hexalith.Folders.Client` (with tests), pending commit in the Folders submodule. You still own auth handler attachment (Step 3) via the returned `IHttpClientBuilder`.
3. **CLI and MCP are scaffolds.** `Cli/Program.cs` and `Mcp/Program.cs` only print the module name; they do **not** yet wrap the client. Don't plan to shell out to the CLI for automation yet.
4. **Local Dapr access control is allow-by-default.** `accesscontrol.yaml` is dev-only; production requires deny-by-default + mTLS. Don't ship the dev policy.
5. **Folders has nested submodules.** Never `git submodule update --init --recursive` from the umbrella — root-level only.

---

## 6. Synthesis & Recommendations

**Bottom line:** To manage folder references from `Hexalith.Projects`, **reference `Hexalith.Folders.Client` (+ `.Contracts`) via the root-detection `ProjectReference` pattern, register the NSwag `IClient` as a typed `HttpClient` with a bearer-token `DelegatingHandler`, and drive folder/parent/repository/ACL references through the typed client — treating every mutation as command-async (202 → poll `lifecycle-status`).** Lean on the **typed client interface + OpenAPI spine** as your stable contract, because the wired REST surface is still catching up to it.

**Recommended path (priority order):**

1. **Use the typed SDK Client**, not raw REST — it absorbs serialization, header, and idempotency-hashing details and insulates you from the transport gap.
2. **Wire references via `ProjectReference` + root detection** (umbrella co-development) or NuGet (decoupled). Root-level submodules only.
3. **Standardize client DI — done.** `AddFoldersClient(this IServiceCollection)` (config-bound) and `AddFoldersClient(this IServiceCollection, Action<FoldersClientOptions>)` (explicit) now register `IClient` as a typed `HttpClient`, validate that `BaseAddress` is an absolute URI, and return `IHttpClientBuilder` so consumers chain a bearer-token `DelegatingHandler`. Drafted in the Folders submodule (`src/Hexalith.Folders.Client/`), with unit tests; pending commit/release.
4. **Model "references" explicitly:** parent (`parentFolderId`), repository (`BindRepository`/`CreateRepositoryBackedFolder` + `ConfigureProviderBinding`), and access (`UpdateFolderAclEntry`). Use `OpaqueIdentifier`-shaped ids throughout.
5. **Respect the invariants:** tenant authority from token only; `system` never a managed tenant; `Idempotency-Key` required+stable on mutations and forbidden on queries; metadata-only logging; safe-denial handling.

**Integration checklist:**

- [ ] Root-level submodules initialized (no `--recursive`).
- [ ] `Directory.Build.props` root-detection for `HexalithFoldersRoot`; `ProjectReference` to `.Client` (+ `.Contracts`).
- [ ] Typed `AddHttpClient<IClient, Client>` with configured `BaseAddress`.
- [ ] `DelegatingHandler` attaching a Keycloak-issued JWT with an audience the Folders server accepts.
- [ ] Stable per-attempt `Idempotency-Key` on every mutation; none on queries.
- [ ] `X-Correlation-Id` + `X-Hexalith-Task-Id` threaded from your call context.
- [ ] 202 responses confirmed via `lifecycle-status` (with freshness hint where needed).
- [ ] `HexalithFoldersApiException` handled by `category`/`clientAction`; 404 treated as safe denial.
- [ ] Needed operations confirmed **server-mapped** (else scheduled as a server story).
- [ ] No dev `accesscontrol.yaml` in production; deny-by-default + mTLS.

**Suggested follow-ups (module work, not consumer work):** map the remaining spine operations (CreateFolder, ACL, bindings, workspace) to external `/api/v1`; ship `AddFoldersClient`; flesh out CLI/MCP to wrap the client.

---

## Sources

**Primary (internal codebase — authoritative):**

- Build/packaging: `Hexalith.Folders/Directory.Build.props:1-33`; `Hexalith.Folders/.gitmodules`; `D:\Hexalith.Projects\.gitmodules:16-18`; `Hexalith.Folders/CLAUDE.md`; `Hexalith.Folders/README.md`.
- Client: `src/Hexalith.Folders.Client/Generated/HexalithFoldersClient.g.cs` (`IClient` :28, `Client` ctor :1308, `useBaseUrl:false` :25, method signatures); `src/Hexalith.Folders.Client/FoldersClientModule.cs`; `src/Hexalith.Folders.Client/nswag.json`; `.../Idempotency/HexalithIdempotencyHasher.cs`.
- Contracts/domain: `src/Hexalith.Folders.Contracts/openapi/hexalith.folders.v1.yaml` (paths, `CreateFolderRequest`, ACL schemas, `ProblemDetails`, `OpaqueIdentifier`, idempotency extensions); `src/Hexalith.Folders/Aggregates/Folder/*` (`FolderStreamName.cs:52,70`, `CreateFolder.cs`, `FolderCreated.cs`, `FolderAccessGranted.cs`, `FolderAccessEntryKey.cs`, `FolderResultCode.cs`); `src/Hexalith.Folders/Aggregates/Organization/OrganizationStreamName.cs:45`.
- Server/hosting: `src/Hexalith.Folders.Server/FoldersServerModule.cs:40-86`; `.../FoldersDomainServiceEndpoints.cs`; `.../Program.cs`; `.../Authentication/*`, `.../Authorization/*`; `src/Hexalith.Folders.Aspire/FoldersAspireModule.cs:10-93`; `src/Hexalith.Folders.AppHost/Program.cs` + `DaprComponents/accesscontrol.yaml`; `docs/exit-criteria/s2-oidc-validation.md`; `docs/contract/*`.
- Project rules: `_bmad-output/project-context.md`; `Hexalith.Folders/_bmad-output/project-context.md`.

**Secondary (web — for recommended .NET patterns only):**

- [Generating a Typed Client for HttpClientFactory using NSwag — Stuart Lang](https://stu.dev/generating-typed-client-for-httpclientfactory-with-nswag)
- [Use HTTP Client Factory with NSwag Generated Classes — Eric Anderson (ITNEXT)](https://itnext.io/use-http-client-factory-with-nswag-generated-classes-in-asp-net-core-3-c1dd66ee004c)
- [Inject IHttpClientFactory into generated C# ApiClient — NSwag #3968](https://github.com/RicoSuter/NSwag/issues/3968)
- [How to Add a BearerToken to an HttpClient Request — Code Maze](https://code-maze.com/add-bearertoken-httpclient-request/)
- [Extending HttpClient With Delegating Handlers — Milan Jovanović](https://www.milanjovanovic.tech/blog/extending-httpclient-with-delegating-handlers-in-aspnetcore)
- [Encapsulating access tokens with a typed HttpClient and MessageHandler — Joao Grassi](https://blog.joaograssi.com/typed-httpclient-with-messagehandler-getting-accesstokens-from-identityserver/)

---

_Research conducted 2026-05-24 for Jerome. Internal-module integration research: primary evidence is the `Hexalith.Folders` source; web sources ground only the recommended (not-yet-existing) client registration pattern._
