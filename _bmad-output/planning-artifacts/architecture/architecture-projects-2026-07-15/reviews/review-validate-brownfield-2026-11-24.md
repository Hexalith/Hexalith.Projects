# Reviewer Gate (Validate intent): brownfield code conformance — Hexalith.Projects architecture spine

- **Target:** `_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md` (status final, updated 2026-11-24, SHA-256 `01b89a2f5e076a52…`, re-verified unchanged at review time)
- **Code baseline:** HEAD `31b35637` on `main` plus the uncommitted working tree (read only). EventStore gitlink `208af26a`, Builds gitlink `bf8a28b4`.
- **Lens:** does the spine ratify, rather than contradict, the code implementers will start from?
- **Reviewer independence:** no file under `reviews/` or `validation-report.*` was read.

**Verdict: PASS WITH FINDINGS.** Every load-bearing DW-63 host claim checks out against the code. Four high-severity gaps remain where the code already does something the spine forbids or never mentions, and the next writer stories would have to guess.

| Severity | Count |
| --- | --- |
| Critical | 0 |
| High | 4 |
| Medium | 4 |
| Low | 4 |

---

## High

### BF-1 — The writer persists Projects-issued ChatBot project grants that AD-3 forbids, and the spine half-ratifies them

- **Severity:** High
- **Anchor:** AD-3 (spine l.132) "Projects issues no Project-level grant"; AD-19 (l.289) names `ResolveProjectChatBotAuthority` as an excepted query; AD-17 writer gate (l.245); AD-33 matrix (l.389–408); action classification (l.500–507).
- **Evidence:**
  - `src/Hexalith.Projects.Contracts/Commands/GrantProjectChatBotAuthority.cs`: "Issues an exact, expiring ChatBot project grant after current owner admission." There are matching `RevokeProjectChatBotAuthority`, `ProjectChatBotAuthorityGranted/Revoked/Rejected` events and a `ResolveProjectChatBotAuthority` query. These came from commit `b7ae7273` "feat(auth): persist scoped ChatBot project grants" (2026-10-11).
  - `src/Hexalith.Projects.Server/ProjectsDomainProcessor.cs:171,234`: the single writer routes both commands to `ProcessChatBotAuthorityAsync`. `src/Hexalith.Projects/Aggregates/Project/ProjectState.cs:51-55` folds them into `ChatBotGrants`.
  - `ProjectsRuntimeCatalogDeclarations.cs:19,32-33`: "the exact twelve processor payload bindings", including `grant-chatbot-authority` and `revoke-chatbot-authority`.
  - `docs/event-catalog.md` (§ProjectChatBotAuthorityGranted): "Records a bounded, exact ChatBot authority issuance."
  - PRD `prd.md:37` says "Projects issues no Project-level grants of its own". `prd.md:601` (A-3) says: "If an approved use case needs Project membership independent of Folder access, add explicit grant/revoke FRs under a new ID". No FR covers these commands.
- **Failure scenario:** Story 7.1 builds the SDK `EventStoreAggregate<TState>` from the spine's own inventory: the AD-33 actions, the classification table and AD-3. It omits the grant/revoke commands and their `Apply`, or treats them as forbidden. Replaying existing streams then fails AD-17's "instance `Apply` replay for every historical success/rejection event", and the Chatbot authority query loses its evidence. A second story could instead extend the grant model, citing AD-19's ratification. The two diverge with the spine supporting both.
- **Proposed resolution:** A human decision is needed; the spine and the code contradict each other. Either:
  - (a) Amend AD-3 to say "Projects issues no Project-membership grant. The scoped, expiring ChatBot authority issuance (`GrantProjectChatBotAuthority`/`Revoke…`, owner-bootstrapped) is a delegated-operation record, not Project access." Then add both commands to the AD-17 equivalence inventory, the AD-33 matrix (who may issue or revoke) and the classification table.
  - (b) Declare the capability unapproved. Freeze it to replay-only (events stay readable under AD-6/AD-22) and route it to a PRD FR under A-3.
  
  Either way, AD-17 must name `ProjectChatBotAuthority*` events in the replay-equivalence set.
- **Acceptance check:** The spine names the grant commands and events in AD-17 and in either AD-33 or the Deferred table. AD-3 no longer contradicts `ProjectsRuntimeCatalogDeclarations.Commands`, and a reader can tell whether Story 7.1 must reproduce `ProcessChatBotAuthorityAsync`.
- **Status:** CONFIRMED (source, git history, PRD).

### BF-2 — The AD-20 allow-all composition on the shipped writer and gate is acknowledged but has no owner, gate or exit

- **Severity:** High
- **Anchor:** AD-20 (l.297) "Allow-all implementations exist only in explicit test composition"; Structural Seed (l.487) "registers allowing EventStore-authorization and Dapr-policy validators, which AD-20 forbids".
- **Evidence:**
  - `ProjectsServerServiceCollectionExtensions.cs:169-170` sets deny-all defaults. The shipped path (`AddProjectsDomainService` → `AddProjectsServerRuntimeInfrastructure`, l.60) replaces them at l.240-243 with `AllowingProjectEventStoreAuthorizationValidator` and `AllowingProjectDaprPolicyEvidenceProvider`.
  - `src/Hexalith.Projects/Authorization/AllowingProjectEventStoreAuthorizationValidator.cs` returns `Allowed("eventstore_validator_test_allow")` unconditionally. It ships inside the *packable* domain package `Hexalith.Projects` (`tools/release-packages.json`).
  - Consumers: `ProjectsDomainProcessor.cs:197` gates every Project command on it. `ProjectAuthorizationGate.cs:508,539` uses it for the v1 adapter and the SDK queries' `EventStoreValidator` and `DaprDenyByDefaultPolicy` layers.
  - `epics.md:2774` (8.8-P1): "Server authorization decisions require verified EventStore and Dapr policy evidence. Record … denial when either authority is missing or denies". `epics.md:2848`: Story 8.8 does "no new … feature implementation".
  - Memlog l.104 routes it to "the Projects Server owner and 8.8-P1 evidence". The spine text names neither.
- **Failure scenario:** DW-63 is accepted on its own acceptance criteria, which cover SDK route admission. Story 7.1 then builds its cutover on a processor whose authorization layer always allows. 8.8-P1 can never record the required denial case, and Story 8.8 may not implement the fix. The violation reaches release with no story owning the removal.
- **Proposed resolution:** Replace the descriptive sentence at l.487 with a binding disposition. Name the owning unit and the gate (for example, "must be replaced by real validators, or the duplicate layer removed in favour of the SDK `/process` policy, before Story 7.1 entry; G-5 supplies the evidence source"). State whether `Allowing*` types may remain in the packable `Hexalith.Projects` assembly.
- **Acceptance check:** The spine names one story or gate that removes `Allowing*` from the shipped composition, with the evidence it requires. `rg "AllowingProject" src/` then shows registrations only in test or explicit development composition.
- **Status:** CONFIRMED.

### BF-3 — The Server owns the query handlers, projections and owner adapters that the seed places in the domain package, and the SDK seams cannot move there as the seed implies

- **Severity:** High
- **Anchor:** AD-24 (l.321) "retain non-packable `Projects.Server` only as the assembly-explicit two-line DomainService host plus module registration and one `MapProjectsHumanEndpoints` call"; Structural Seed table (l.480 `Hexalith.Projects`: "handlers, projections"; l.482); diagram node `Domain` (l.62); seed paragraph (l.487).
- **Evidence:**
  - Thirteen files in `src/Hexalith.Projects.Server` implement `IDomainQueryHandler`, `IAsyncDomainProjectionHandler` or `IDomainProjectionHandler`. They cover all 10 query types (registered at `ProjectsServerServiceCollectionExtensions.cs:185-208,231`), the two named projections and the legacy full-replay projection. `src/Hexalith.Projects` has none.
  - The Server also holds the Dapr read models, `ProjectReadAudit`, the authorization gate, authentication, and the Conversations/Folders/Memories owner directories.
  - Those seams live in `Hexalith.EventStore.DomainService` (`IDomainQueryHandler.cs:18`, `IAsyncDomainProjectionHandler.cs:8`), whose csproj l.14 has `<FrameworkReference Include="Microsoft.AspNetCore.App" />`. `src/Hexalith.Projects/Hexalith.Projects.csproj` references only Contracts, and its packed dependency set (`tools/release-packages.json`) has no EventStore.Client or DomainService.
  - l.487 lists only the processor, runtime infrastructure, dev catalog and response policy as interim Server residents.
- **Failure scenario:** Story 7.x adds `GetProjectTaskStatus` successors and Story 8.2 adds the export query. One follows the code and adds them under `Server/Queries`; the other follows the seed and adds them to `Hexalith.Projects`, pulling `Microsoft.AspNetCore.App` into a packable domain package. Discovery then splits across two assemblies, the `AddEventStoreDomainService(typeof(ProjectAggregate).Assembly)` target misses half the handlers, and the published domain package changes shape.
- **Proposed resolution:** Add one rule to AD-24 or the seed. For example: "Until the AD-17 writer cutover, every `IDomainQueryHandler`, named projection, read-model adapter and owner directory lives in `Projects.Server`. At or after that cutover they move as one unit into [named assembly], which may depend on `Hexalith.EventStore.DomainService`, or the packable domain stays SDK-free and a non-packable `Projects.Server` keeps them." Also correct "only" in AD-24 to name the interim residents, or point to a Deferred row.
- **Acceptance check:** The spine names one interim home and one target home for query and projection handlers. That home is consistent with the dependency rule for `Hexalith.Projects`, and a new handler's location follows from the text alone.
- **Status:** CONFIRMED.

### BF-4 — v1 Project identity is GUID-shaped or caller-selected, and the post-cutover disposition is undefined

- **Severity:** High
- **Anchor:** AD-18 (l.283) prevents "GUID-shaped identifiers, caller-selected aggregate identity". AD-31 (l.371) says "Only the historical unversioned name-only request is a v1 compatibility shape and remains accepted/readable throughout v1". AD-24 (l.321) says "From that cutover, a v1 mutation admits work only through the selected SDK writer's governed admission, with platform-generated identities".
- **Evidence:**
  - `ProjectsDomainServiceEndpoints.cs:650-652`: `string.IsNullOrWhiteSpace(body.ProjectId) ? new ProjectId(Guid.NewGuid().ToString("N")) : new ProjectId(body.ProjectId)`. This applies to both the legacy and the canonical shape.
  - The legacy shape is not name-only. The pinned 7.1-P1 matrix (`spec-7-1-p1-qualify-projects-creation-contracts.md`, "Legacy" row) reads: "Authorized `{name}`, optionally with `projectId`, `description` and `setupMetadata`", and commit `31b35637` pins those fixtures.
  - The canonical OpenAPI request (`hexalith.projects.v1.yaml:2785-2797`) carries no `projectId`, so the adapter mints a GUID for every canonical create.
- **Failure scenario:** Story 7.1 implements AD-18 ULID reservation and ignores a supplied `projectId`. Retained consumers that create with their own ID and then `GET /api/v1/projects/{projectId}` receive 404, which breaks AD-31's "remains accepted throughout v1". Alternatively, 7.1 honours the supplied ID and violates AD-18. Separately, GUID-N ProjectIds keep accumulating in history until the cutover with no stated bound.
- **Proposed resolution:** State the pre-cutover exception and the post-cutover rule explicitly. In AD-18, add: "Until the AD-17 writer cutover the retained v1 create mints GUID-N or accepts the caller's `projectId`; these remain readable (persisted legacy IDs)." Then choose one post-cutover behaviour for a supplied `projectId` (reject with 400 `projectId`, or accept-as-idempotency-alias) and correct AD-31's "name-only" to the actual legacy field set.
- **Acceptance check:** Given a legacy body with `projectId` after the cutover, the spine yields exactly one expected status and identity. AD-31's legacy field list matches the 7.1-P1 fixture's legacy row.
- **Status:** CONFIRMED.

---

## Medium

### BF-5 — Retained v1 proposal confirmation parses and validates before it authorizes; the spine is silent and the promised DW does not exist

- **Severity:** Medium
- **Anchor:** AD-31 (l.371) "Authorization precedes protected metadata parsing … One `SensitiveMetadataTierValidator` governs direct creation and proposal confirmation".
- **Evidence:** `Queries/ProposeNewProjectEndpoint.cs:191` reads the body and l.199 runs `TryValidateConfirmRequest`, both before `AuthorizeCreateAsync` at l.205. The 7.1-P1 spec (Decisions, Q2=A) says "Its parse-before-authorize order goes to a DW entry routed to Story 7.12". `rg` over `deferred-work.md` finds no such entry.
- **Failure scenario:** An unauthorized caller probes `POST /api/v1/projects/proposals/confirm` and learns validation feedback, including the classification `rejectedField`. That is exactly the probing AD-31 prevents. Story 7.12 never picks the issue up because the ledger entry was never written, and the spine does not record the exception.
- **Proposed resolution:** The code is wrong here. Either add one sentence under AD-24 or AD-31 naming this known retained-adapter deviation and its owner (Story 7.12), or require the DW entry before 7.1-P1 acceptance.
- **Acceptance check:** A DW entry or a spine note exists that names the route, the deviation and the owning story, or the endpoint authorizes before `ReadFromJsonAsync`.
- **Status:** CONFIRMED.

### BF-6 — A hand-edited OpenAPI "Contract Spine" is a second editable contract authority that AD-16 does not classify

- **Severity:** Medium
- **Anchor:** AD-16 (l.235-239) "Versioned .NET contracts are the sole editable contract authority. Prevents: editable C#, OpenAPI … from drifting"; Conventions "Contracts" row (l.444).
- **Evidence:**
  - `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml:1-12` calls itself the "Canonical OpenAPI 3.1 foundation … Contract Spine".
  - Story commits edit it directly: `git log` shows `9dadd65c`, `f10129c9`, `005c2d5d`.
  - It drives Client generation through `Hexalith.Projects.Client.csproj` (`HexalithProjectsContractSpine` → NSwag) and is asserted by `OpenApiContractSpineTests`. The 7.1-P1 spec treats it as "the enum and required fields" source ("Never: change the spine or generated client").
- **Failure scenario:** A story changes a retained v1 wire shape (for example the AD-17 "compatible overloads" for Conversation metadata). One implementer edits C# DTOs per AD-16; another edits the YAML because that is what regenerates `Projects.Client`. The generated client and the server then drift, with each citing a rule.
- **Proposed resolution:** Add to AD-16 or AD-24: "`hexalith.projects.v1.yaml` is the frozen retained-v1 authority for the 25 AD-24 pairs and `Projects.Client` generation. It accepts only compatibility fixes, never new operations, and retires with the adapter. New operations originate only in C# Contracts."
- **Acceptance check:** The spine states which artifact a v1 wire-shape change edits first, and the statement matches `Hexalith.Projects.Client.csproj` generation inputs.
- **Status:** CONFIRMED.

### BF-7 — The ownership diagram routes all sibling runtime calls through workflow transport, while the Server calls owners directly

- **Severity:** Medium
- **Anchor:** Design Paradigm diagram (l.58-110), especially `Domain --> ConversationContracts/FolderContracts/MemoryContracts` (l.100-102) and `WorkflowTransport -. runtime call .-> …` (l.106-108); caption (l.112).
- **Evidence:**
  - `Hexalith.Projects.Server.csproj:18-23` references `Conversations.Client`, `Folders.Client` and `Memories.Client.Rest` plus their Contracts.
  - `ProjectsServerServiceCollectionExtensions.cs:112-168,256-279` registers direct owner clients with `EventStoreGatewayTokenForwardingHandler` and `DelegatedOwnerQueryClient`. These serve query-time reauthorization (AD-11/AD-14) and v1 membership writes.
  - `Hexalith.Projects.csproj` (the diagram's `Domain`) references no sibling contract.
- **Failure scenario:** A writer story implementing owner reauthorization at mutation time (AD-14 "mutations always reauthorize with the owner") wires a direct client in the host, following the code. The saga story routes the same owner call through platform workflow transport, following the diagram. The two produce different identity, retry and idempotency semantics for the same owner.
- **Proposed resolution:** Add a dashed edge `Host -. "delegated owner query (reauthorization)" .-> Conversations/Folders/Memories` and one caption sentence: request-scoped owner reads and reauthorization go host-direct under the delegated actor, and durable cross-context mutation goes through workflow transport (AD-12).
- **Acceptance check:** Every runtime owner call in `ProjectsServerServiceCollectionExtensions.cs` maps to a diagram edge, and the caption says which path a new owner call takes.
- **Status:** CONFIRMED.

### BF-8 — The proprietary `Projects.Cli` performs Boolean-confirmed lifecycle mutations, and the spine gives it no interim constraint

- **Severity:** Medium
- **Anchor:** AD-24 (l.321) retires the CLI/MCP runtime projects only after replacement; AD-29 (l.353) "Both public heads refuse UI-only and confirmation-required actions"; AD-5 (l.166) prevents "Boolean confirmation"; AD-33 row "Archive or restore" (l.398).
- **Evidence:** `src/Hexalith.Projects.Cli/ProjectsCliApplication.cs:50` handles "archive", "restore", "relink", "unlink" and "reevaluate" in `MutateAsync`. l.350-352 gate them on `invocation.Has("confirm")` plus `--dry-run-evidence`, and l.428 sets `ReplacementConfirmed = invocation.Has("confirm")`. All of these go over the retained v1 REST through `Projects.Client`. `Projects.Mcp` also exists (non-packable). The baseline `hexalith-llm-instructions.md` (MCP and CLI ownership) limits such packages to "safety and continuity fixes" and requires an operation inventory.
- **Failure scenario:** A Tenant Operator uses the in-repo CLI to archive a Project, a path AD-33 says McpCli must refuse. A later story extends this CLI for continuity, for example adding "move", because no spine rule freezes it. That creates an agent-capable surface outside AD-29.
- **Proposed resolution:** Add one AD-24 or AD-29 sentence: "`Projects.Cli` and `Projects.Mcp` are frozen obsolete migration sources. They accept safety and continuity fixes only, gain no operations, are never shipped or enrolled, and their mutation commands are inventoried for retirement."
- **Acceptance check:** The spine names both projects' interim status, and `release-packages.json` and the McpCli enrollment exclude them.
- **Status:** CONFIRMED.

---

## Low

### BF-9 — The AD-19 exception wording "the same safe `404` as denial" is true only after the Gateway

- **Severity:** Low
- **Anchor:** AD-19 (l.289).
- **Evidence:**
  - For the four excepted types, `ProjectsQueryResponsePolicy.cs:21-25` returns `Results.NotFound(result)` with the handler's own body. That body is `safe-denial` for denial but `invalid-envelope` for bad paging (`GetProjectContextQueryHandler.cs:37,45,48`) and `Forbidden` for every Chatbot-authority failure.
  - The Gateway normalizes the difference: `DaprDomainQueryInvoker.cs:95` maps receiver 404/403 to `safe-denial`.
  - Memlog l.107 records "bodies carry only safe-denial, InvalidEnvelope or Forbidden", but the spine dropped that clause.
- **Failure scenario:** A direct-receiver test (as in `ProjectsCompatibilityTestEndpoints`) asserts byte-equal denial and invalid-input bodies and fails. Or a reviewer files the body difference as a disclosure bug.
- **Proposed resolution:** Change the wording to "…returns a `404` whose receiver body carries only `safe-denial`, `invalid-envelope` or `Forbidden`; the Gateway returns the same safe `404` for all".
- **Acceptance check:** The wording matches `ProjectsQueryResponsePolicy` plus `DaprDomainQueryInvoker` behaviour.
- **Status:** CONFIRMED.

### BF-10 — No test asserts AD-17's "exactly one keyed `projects` processor"

- **Severity:** Low
- **Anchor:** AD-17 (l.245); seed snippet (l.489-492).
- **Evidence:** `ProjectsRuntimeCatalogTests.cs:54` asserts a single keyed `IAsyncAggregateReplay`, but l.44 uses `GetRequiredKeyedService<IDomainProcessor>("projects")`, which returns the last registration and does not detect a second one. When the target marker `typeof(ProjectAggregate).Assembly` gains a convention aggregate, SDK `AddEventStore(domainAssemblies)` registers its own keyed processor beside `ProjectsServerServiceCollectionExtensions.cs:174`.
- **Failure scenario:** A cutover story adds the SDK aggregate. Two keyed `projects` processors register, the guard passes, and the old processor silently wins or loses depending on registration order. AD-17 requires the two to be replaced together.
- **Proposed resolution:** Add one acceptance check to AD-17: "assert `GetKeyedServices<IDomainProcessor>(\"projects\")` and the replay are each single and refer to the same writer".
- **Acceptance check:** The spine states the observable assertion.
- **Status:** CONFIRMED (test code); the failure itself is PLAUSIBLE.

### BF-11 — Route-catalog authority is split between the compiled declarations and the module manifest, and the dev catalog's retirement owner is contradictory

- **Severity:** Low
- **Anchor:** Seed (l.487) "development catalog, which retire[s] under AD-25"; runner contract (l.511) "It names the internal `projects/domain` Server, dependencies and workload-facing operations".
- **Evidence:**
  - `ProjectsDevelopmentCatalogExtensions.cs:27-28` enrolls the SDK `AddEventStoreDevelopmentDomainCatalog` over Projects-owned `ProjectsRuntimeCatalogDeclarations`: 25 declarations, asserted at `ProjectsRuntimeCatalogTests.cs:45`. Those 25 are unrelated to the 25 v1 REST pairs.
  - The DW-63 spec task says "Replace it with SDK discovery plus the existing processor payload inventory" (unchecked) and "Reconcile declared callbacks and catalog metadata with the host".
  - No test compares the manifest with the declarations (`rg manifest ProjectsRuntimeCatalogTests.cs` finds nothing).
- **Failure scenario:** A story adds a query to `ProjectsRuntimeCatalogDeclarations` but not to `module/hexalith.module-manifest.v2.json`, or the reverse. Catalog admission then differs between the source fixture and the runner.
- **Proposed resolution:** Name one authority for workload-facing operations and the check that binds the other. Say whether DW-63 or AD-25 retires the dev catalog.
- **Acceptance check:** The spine names which file is edited first for a new callback, and which check reconciles the two.
- **Status:** CONFIRMED.

### BF-12 — The one-type-per-file convention has drifted in the Server

- **Severity:** Low
- **Anchor:** Conventions "Files and types" (l.445).
- **Evidence:** `IProjectCommandSubmitter.cs` declares 3 types (interface, enum, record). `ProjectMemoryValidationResult.cs`, `ProjectFileReferenceValidationResult.cs`, `ProjectFolderValidationResult.cs` and `ProjectConversationAssignmentResult.cs` declare 2 each.
- **Failure scenario:** A story moving these types under BF-3 copies the pattern, or a lint introduced later fails unrelated stories.
- **Proposed resolution:** No spine change is needed. Route it as a hygiene DW to the Server owner, or note that existing violations are tolerated until the type moves.
- **Acceptance check:** A DW entry exists, or the files are split.
- **Status:** CONFIRMED.

---

## Checked and held

- `Program.cs:13,17,18`: `AddProjectsDomainService()` → `UseEventStoreDomainService()` → exactly one `MapProjectsHumanEndpoints()`, matching the Writers row (l.51) and AD-24.
- `AddProjectsDomainService` calls `builder.AddEventStoreDomainService(typeof(ProjectsDomainProcessor).Assembly)` (`ProjectsServerServiceCollectionExtensions.cs:62`) on the Server assembly, as l.487 states. It refuses the anonymous development bypass (l.54-57).
- The retained routes are exactly 25 `/api/v1/projects` method-and-path pairs, identical at `5f1d955b` and HEAD (`ProjectsDomainServiceEndpoints.cs:84-532`):
  - GET `/api/v1/projects`
  - POST `/api/v1/projects`
  - GET `/{projectId}`
  - GET `/{projectId}/operator-diagnostics`
  - GET `/{projectId}/conversations`
  - GET `/{projectId}/context`
  - GET `/{projectId}/context/explain`
  - GET `/{projectId}/context/refresh`
  - GET `/{projectId}/setup/conversation-start`
  - GET `/resolution/from-conversation`
  - GET `/resolution/from-attachments`
  - POST `/resolution/new-project-proposal`
  - POST `/proposals/confirm`
  - POST `/{projectId}/conversations/{conversationId}/link`
  - POST `/{projectId}/conversations/{conversationId}/move`
  - POST `/{projectId}/conversations/{conversationId}/resolution/confirm`
  - DELETE `/{projectId}/conversations/{conversationId}`
  - PUT `/{projectId}/folder`
  - POST `/{projectId}/files/{fileReferenceId}/link`
  - DELETE `/{projectId}/files/{fileReferenceId}`
  - POST `/{projectId}/memories/{memoryReferenceId}/link`
  - DELETE `/{projectId}/memories/{memoryReferenceId}`
  - PATCH `/{projectId}/setup`
  - POST `/{projectId}/archive`
  - POST `/{projectId}/restore`
  
  At `5f1d955b` the Server also mapped `/process`, `/project`, `/query`, `/project/v2` and `/project/rebuild/v1` itself; those are now SDK-owned.
- The whole group requires `EventStoreHttpHostSecurityExtensions.HumanPolicy` (`ProjectsServerServiceCollectionExtensions.cs:76-78`), and no `AllowAnonymous` appears in the Server.
- Adapter seams:
  - 12 Project command submissions go through `IProjectCommandSubmitter` (gateway-backed `EventStoreProjectCommandSubmitter`).
  - link, move, unlink, resolution-confirm and proposal-confirm membership go through `IProjectConversationAssignmentDirectory`.
  - The only adapter durable write is the AD-33 audit: `ProjectReadAudit` → `ReadModelWritePolicy.UpdateAsync` over `IReadModelStore`, swapped to `AddEventStoreReadModelStore()` on the runtime path.
  - The proposal ledger is a per-replica `InMemoryProjectProposalConfirmationIdempotencyLedger` singleton.
- `ProjectsQueryResponsePolicy` is registered as `IDomainQueryResponsePolicy`. It failure-normalizes exactly List, GetProject, Audit, TaskStatus, ResolveFromConversation and ResolveFromAttachments (400 for `invalid-envelope`/`invalid-cursor`, otherwise 404 `safe-denial`). The four AD-19-excepted types are exactly the remaining registered handlers. Without a policy the SDK returns 200 for failures (`EventStoreDomainServiceExtensions.cs:446-447`).
- `IDomainQueryResponsePolicy` exists only at EventStore `208af26a` (`v5.5.2-11-g208af26a5`, untagged) and is absent from `v5.5.1`/`v5.5.2`. Builds `bf8a28b4` pins EventStore `5.5.1` and FrontComposer `6.1.0`. The gitlinks match the Stack section.
- AD-17 host shape: one `AddKeyedTransient<IDomainProcessor>("projects")` and one matching keyed `IAsyncAggregateReplay` resolve to `ProjectsDomainProcessor`, and no `EventStoreAggregate<` exists in `src/`. `ProjectAggregate` is a static partial class, so the seed's `typeof(ProjectAggregate).Assembly` marker compiles.
- AD-14 seams: two named `IAsyncDomainProjectionHandler`s plus the single identified legacy `IDomainProjectionHandler` (`ProjectProjectionHandler`); cursors use `AddEventStoreQueryCursorCodec`.
- Package layout:
  - Packable: `Contracts`, `Projects`, `Testing`, `Client`.
  - Non-packable: `UI.Contracts`, `Server`, `Cli`, `Mcp`.
  - `Projects.Client.Generation` and `.Shared` exist, nested under `src/Hexalith.Projects.Client/Generation/`.
  - `tools/release-packages.json` lists exactly five packages: Contracts, Projects, Client, Testing and ServiceDefaults.
  - `Projects.Testing` holds only builders and replay, leakage and Tenant-isolation helpers.
- Module manifest v2: server `id: domain`, `appId: projects`, matching l.511.
- AD-31 direct create: `Idempotency-Key` check, then authorization before any body read (`ProjectsDomainServiceEndpoints.cs:568-575`). Duplicate or non-string classification maps to `projectMetadata.metadataClass`, and a versioned request without metadata maps to `projectMetadata`. `SensitiveMetadataTierValidator` is the shared predicate.
- Uncommitted working tree: the `docs/runbooks/projects-topology.md` and `WorkloadCredentialTopologyTests.cs` edits (publish-mode composition, owner-held catalog custody) touch no spine claim.
