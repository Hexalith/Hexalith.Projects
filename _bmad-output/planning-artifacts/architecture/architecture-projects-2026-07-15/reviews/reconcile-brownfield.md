# Brownfield and platform-reality reconciliation

**Reviewed:** 2026-07-16  
**Artifact:** `ARCHITECTURE-SPINE.md`  
**Lens:** Current Projects code, package/release graph, EventStore DomainService APIs, migration feasibility, exact local pins, and repository-local run/test parity after technical-layer extraction  
**Verdict:** **FAIL pending clear corrections**

The target direction is sound: the spine correctly moves Dapr, Aspire, persistence, health, telemetry, runtime Web/MCP/CLI composition, and generic durable execution to technical modules while retaining a two-line Projects host and making deletion of the old technical projects conditional on parity. It is not yet safe to finalize because the target package graph omits current artifacts that require an explicit disposition, the migration does not bind the current aggregate/state shapes to the actual DomainService discovery contract, and the development-runner invariant is not yet machine-checkable enough to guarantee that this repository remains independently runnable and testable.

## Evidence established from the current workspace

| Area | Current fact | Reconciliation consequence |
| --- | --- | --- |
| Projects runtime graph | Projects currently owns `Infrastructure`, `Workers`, `ServiceDefaults`, `Aspire`, `AppHost`, `UI`, `Mcp`, and `Cli`, and `Server` directly references the infrastructure and sibling clients. | AD-1, AD-24, and AD-25 correctly require staged extraction; deletion cannot be an initial refactor step. |
| Release packages | The release/package gate currently emits five packages: `Projects.Contracts`, `Projects`, `Projects.Client`, `Projects.Testing`, and `Projects.ServiceDefaults`. | The target graph must explicitly retain or retire `Projects.Testing` and remove `Projects.ServiceDefaults`; it currently names neither complete disposition. |
| Client generation | `Projects.Client.Generation` and `Projects.Client.Generation.Shared` are separate current projects, while AD-16 assigns generation to platform generators. | Their move/retirement and the reproducibility gate need an explicit target disposition. |
| DomainService host | EventStore exposes `AddEventStoreDomainService(...)` and `UseEventStoreDomainService()`. Because Projects domain logic is in a separate assembly from `Projects.Server`, the supported overload is the explicit assembly overload such as `AddEventStoreDomainService(typeof(ProjectAggregate).Assembly)`. | The two-line host is feasible, but the host seed must name the explicit domain-assembly overload rather than imply no-argument discovery from the Server assembly. |
| Aggregate discovery | EventStore discovers only concrete `EventStoreAggregate<TState>` subclasses. Its handlers must expose `Handle(command, state[, envelope])` returning `DomainResult`; state replay expects public `void Apply(TEvent)` methods and a constructible state. | Current `ProjectAggregate` is a static partial class returning `ProjectResult`; `ProjectState` is an immutable positional record without a parameterless constructor and delegates to a static `ProjectStateApply`. It is not discoverable by the target host today. |
| Current commands | Projects commands implement the local `IProjectCommand` and expose an instance `CommandType`, while generated EventStore command contracts use `ICommandContract` with static kebab-case `Domain`/`CommandType` and `AggregateId`. | Command routing/generated API compatibility must be migrated and proven; it cannot be assumed from the current contracts. |
| Projection APIs | Current EventStore exposes `IDomainQueryHandler`, legacy synchronous full-replay `IDomainProjectionHandler`, preferred named incremental `IAsyncDomainProjectionHandler`, `IReadModelStore`, `IReadModelBatchStore`, `ReadModelWritePolicy`, and `IQueryCursorCodec`. | AD-14's incremental-read invariant needs to bind the preferred interfaces explicitly so builders do not choose the legacy full-replay seam. |
| Durable task/confirmation | No generic Projects-usable Durable Task or Confirmation Artifact API was found in the current EventStore release surface. | G-1 correctly blocks implementation and deletion until the platform capability exists and has restart/two-instance proof. |
| AppHost integration | EventStore exposes `AddEventStoreDomainModule` for platform AppHosts; its current AppHost wires domain modules explicitly and has no module-manifest discovery. | G-4 correctly identifies a missing platform capability. |
| Manifest feasibility | Aspire.Hosting 13.4.6 exposes non-generic `AddProject(builder, name, projectPath)` overloads as well as generated-type overloads. | A generic platform runner can load a checked-in manifest without compile-time Projects project types; the direction is feasible on the pinned Aspire line. |
| Integration fixtures | `DaprDomainServiceTestFixtureBase` exists for a domain host; `AspireTopologyFixtureBase<TAppHost>` exists but requires a compile-time AppHost marker. | The platform must add a manifest-aware runner/fixture entry point or publish a generic runner AppHost type that consumes a manifest path. Current fixtures alone do not preserve the target repository lane. |
| Dapr pins | Central package catalog: Dapr .NET packages `1.18.4`; CI runtime input `1.18.0`. The unused central catalog entry `Dapr=1.17.9` is not restored by any inspected project and is not the current CI runtime. | The stack row and G-6 currently mislabel `1.17.9` as the runtime/CLI seed and must be corrected. |
| Other exact pins | Root SDK `10.0.302` with `latestPatch`; EventStore line `3.67.3`; FrontComposer `4.0.0`; Aspire `13.4.6`; Dapr Aspire toolkit preview `13.4.0-preview.1.260602-0230`; Fluent UI `5.0.0-rc.4-26180.1`; xUnit `3.2.2`; Shouldly `4.3.0`; NSubstitute `6.0.0-rc.1`. | The spine's remaining stack rows match the current central catalog. RC/preview lines correctly require compatibility gates rather than casual upgrades. |

## Findings

### BR-1 — Target package graph omits `Projects.Testing` and client-generation disposition

**Severity:** Blocker for finalization  
**Affected:** AD-24, Structural Seed, AD-30, release/package evidence

AD-24 says to retain `Projects.Contracts` and `Projects`, conditionally retain `Projects.Client`, add `Projects.UI.Contracts`, and retire the named technical runtime projects. The current release graph also contains packable `Hexalith.Projects.Testing`, whose source is domain-specific builders, leakage assertions, replay conformance, and tenant-isolation helpers. It is neither a forbidden runtime layer nor named in the target graph. The current solution also contains two non-packable client-generation projects, while AD-16 says platform generators own generation.

This silence permits incompatible implementations: one team may delete `Projects.Testing`, another may continue publishing it, and another may move its domain helpers into EventStore.Testing. Likewise, the current generator may survive as an ungoverned technical layer even though the spine assigns generation to the platform.

**Required correction:**

1. Add `Hexalith.Projects.Testing` to the target artifacts as a domain-specific test-support package, or explicitly retire it after its reusable generic pieces move to `Hexalith.EventStore.Testing(.Integration)` and its Projects-specific builders move to tests. State whether it remains packable.
2. Explicitly move/retire `Projects.Client.Generation` and `.Shared` after the platform generator reproduces the same generated client and compatibility fingerprints; they must not be silently left behind.
3. Bind the release-package manifest/gate to the target inventory so `Projects.ServiceDefaults` disappears only when its platform replacement is consumed and `Projects.Testing` follows the adopted disposition.

### BR-2 — Current aggregate/state cannot be discovered by DomainService

**Severity:** Blocker for migration feasibility  
**Affected:** AD-1, AD-17, AD-22, AD-24, G-1, migration verification

The target two-line host does not discover today's `ProjectAggregate`. EventStore's `AssemblyScanner` recognizes only concrete subclasses of `EventStoreAggregate<TState>`. The current aggregate is static, its handlers have the order `(state, command, occurredAt)` and return `ProjectResult`, and state replay is implemented as immutable `ProjectState` plus static `ProjectStateApply`. EventStore expects handler forms returning `DomainResult` and state instance `Apply(TEvent)` methods.

This is a solvable domain refactor, not a reason to retain the custom `IDomainProcessor`. EventStore's guardrail explicitly forbids domain modules from keeping a hand-written legacy `IDomainProcessor` as the target design.

**Required correction:** strengthen AD-17 or add a migration gate requiring all of the following before writer cutover:

- one convention-discovered Projects aggregate with domain name `projects`;
- command handlers that produce the same success/rejection/no-op semantics through `DomainResult`;
- replay support for every historical success and rejection event, including retained `ProjectFolderCreationPending` behavior;
- deterministic golden-history equivalence between the current state fold and the new SDK state fold;
- command discriminator compatibility or explicit legacy adapters;
- no remaining Projects-owned `IDomainProcessor` after cutover.

The adapter/refactor shape may remain implementation detail, but the equivalence and discovery gates are architectural migration invariants.

### BR-3 — Incremental projection seam is underspecified

**Severity:** High  
**Affected:** AD-1, AD-14, AD-17, Consistency Conventions

The spine mandates incremental bounded read models, but it never names the current platform seam that implements them. EventStore has two projection models with materially different behavior:

- `IDomainProjectionHandler` is legacy synchronous full replay and returns one rebuilt state;
- `IAsyncDomainProjectionHandler` is the named, cancellation-aware incremental seam with stable dispatch identity and persisted read-model support.

Leaving the choice implicit could recreate the current O(history) rebuild problem while appearing compliant.

**Required correction:** bind Projects named projections to `IAsyncDomainProjectionHandler` plus `IReadModelStore`/`IReadModelBatchStore` and `ReadModelWritePolicy`; bind queries to `IDomainQueryHandler` and cursors to `IQueryCursorCodec`/`QueryCursorScope`. Reserve `IDomainProjectionHandler` for explicitly identified full-replay compatibility only. Require persisted end-state, duplicate-dispatch, rebuild, and cursor scope/tamper evidence.

### BR-4 — Repository-local runner invariant needs a consumable command contract

**Severity:** Blocker for the user's explicit run/test requirement  
**Affected:** AD-25, G-4, Structural Seed, AD-30

AD-25 is directionally correct and Aspire 13.4.6 makes path-based manifest composition feasible. However, no manifest or generic platform runner exists in the checked-out EventStore/AppHost code. The current reusable topology fixture also requires a compile-time AppHost type. The phrase "platform runner" alone does not prove this repository can still be cloned, restored, run, debugged, and tested without recreating a Projects AppHost.

**Required correction:** make G-4's exit criteria machine-checkable:

- a versioned manifest schema and a checked-in manifest validated in CI;
- a pinned, independently consumable runner delivered as a technical-module package/tool or a root-declared submodule entry point—never a nested or floating repository dependency;
- one repository-owned run command and one teardown command, with no manual edit in the platform repository;
- path resolution relative to this repository, non-secret configuration only, deterministic app/resource IDs, and clear secret/identity injection by the runner;
- local debug support for the Projects host/domain source and descriptor assembly;
- a manifest-aware test entry point covering persisted state, restart, two instances, and authenticated Web/CLI/MCP lanes;
- parity evidence from a clean checkout before removing Projects AppHost/Aspire/ServiceDefaults/UI/MCP/CLI/Workers/Infrastructure;
- CI package-mode proof that does not depend on Debug/source-only project references.

These criteria preserve technical ownership in the platform while keeping execution authority and reproducible commands in this repository.

### BR-5 — Dapr `1.17.9` is misclassified as the active runtime

**Severity:** High factual correction  
**Affected:** Stack, G-6

The current CI workflow supplies Dapr runtime `1.18.0`, while the .NET SDK packages are `1.18.4`. `Dapr=1.17.9` is an unused central catalog entry; no inspected project restores it. Calling it the current "runtime/CLI seed" makes G-6 demand an upgrade that the CI configuration already performed and obscures the real verification gap.

**Required correction:** list `Dapr runtime test baseline = 1.18.0` and `.NET SDK packages = 1.18.4`, then require live compatibility/restart/two-instance evidence. Remove or separately label the unused `Dapr=1.17.9` catalog entry; do not present it as runtime evidence.

### BR-6 — Runtime call arrows risk creating domain-specific dependencies in EventStore

**Severity:** Medium/high boundary ambiguity  
**Affected:** Design Paradigm diagram, AD-2, AD-9, AD-12, AD-24

The primary diagram draws `EventStore --> Conversations/Folders/Memories` using the same arrow style used for package dependencies. A generic EventStore technical module must not take compile-time dependencies on Projects-specific owner services. Projects owns workflow meaning and dependency steps; the platform owns the generic executor and transport.

**Required correction:** distinguish compile-time dependency from runtime invocation. Bind the target so EventStore depends only on owner-neutral workflow/transport contracts; Projects workflow definitions or generated owner adapters bind the Conversations/Folders/Memories contracts; the platform supplies authenticated service invocation. Label runtime call arrows or use a separate style. Do not solve the gap by reintroducing `Projects.Infrastructure`.

### BR-7 — The exact two-line host shape must scan the Projects assembly

**Severity:** Medium  
**Affected:** AD-1, AD-24, Structural Seed

The no-argument DomainService registration scans the calling assembly. In the adopted split, the calling assembly is `Projects.Server`, while the aggregate/handlers live in `Projects`. The supported explicit-assembly overload exists and keeps the host at two registration lines.

**Required correction:** seed the host contract as the equivalent of:

```csharp
builder.AddEventStoreDomainService(typeof(ProjectAggregate).Assembly);
app.UseEventStoreDomainService();
```

The exact marker type may change during BR-2, but assembly-explicit discovery is binding while Server and domain remain separate projects.

## Migration feasibility assessment

| Migration slice | Assessment | Evidence required before cutover |
| --- | --- | --- |
| Preserve existing event history | Feasible | Additive `ProjectCreated` Folder field; old `ProjectFolderCreationPending` deserialization/apply retained; golden replay through the SDK aggregate for every event type. |
| Shadow SDK read models | Feasible | `IAsyncDomainProjectionHandler`, read-model stores/batches, stable dispatch IDs, replay/backfill, exact key/watermark/order/cursor/tenant comparison. |
| Read route cutover | Feasible | Slice-by-slice route flag, compatibility adapter, deterministic comparison, reversible route selection. |
| Command writer cutover | Feasible only after BR-2 | Convention-discovered aggregate, command discriminator adapters, fence/drain proof, exactly one admitted writer, and old-writer replay capability decision. |
| Durable cross-owner workflows | Not currently implementable | G-1 platform engine and G-2 owner receipt/status/idempotency contracts, crash/restart/two-instance evidence. |
| Confirmation artifacts | Not currently implementable | Protected durable record plus atomic consumption/admission and expiry/replay/version-race tests. |
| Remove technical Projects projects | Not yet allowed | G-1 through G-6, BR-1 package disposition, BR-4 runner parity, real persisted/authenticated lanes. |

## Exact API and package-fit disposition

- **Ratified:** `.NET 10.0.302`, `net10.0`, C# 14, EventStore `3.67.3`, FrontComposer `4.0.0`, Aspire `13.4.6`, Dapr .NET `1.18.4`, Fluent UI RC4, xUnit 3.2.2, Shouldly 4.3.0, and NSubstitute RC1 match the current root package catalog.
- **Ratified with usage correction:** `IDomainQueryHandler`, `IAsyncDomainProjectionHandler`, `IReadModelStore`, `IReadModelBatchStore`, `ReadModelWritePolicy`, and `IQueryCursorCodec` are present and fit the target read architecture.
- **Compatibility-only:** `IDomainProjectionHandler` is the full-replay v1 seam and must not be the default implementation for incremental Projects views.
- **Missing external prerequisites:** generic Durable Task/Confirmation Artifact capability and manifest-driven platform composition runner.
- **Factual correction required:** active CI Dapr runtime is 1.18.0, not 1.17.9.

## Final pass/fail checklist

| Check | Result |
| --- | --- |
| Technical runtime ownership moves out of Projects | Pass |
| Domain and contract authority remain in Projects | Pass |
| Old runtime retained until verified parity | Pass |
| Repository remains independently runnable/testable | **Fail until BR-4 is made machine-checkable and G-4 is delivered** |
| Target package/release graph has complete disposition | **Fail: BR-1** |
| DomainService migration matches current API conventions | **Fail: BR-2, BR-3, BR-7** |
| Event/read migration avoids rewrite and dual writers | Pass with BR-2 gate added |
| Exact active runtime pins are accurate | **Fail: BR-5** |
| Technical module remains generic across bounded contexts | Needs clarification: BR-6 |

The spine should pass this lens after BR-1 through BR-7 are incorporated. No change to the adopted paradigm is required.

## Recheck — 2026-07-16

**Disposition: PASS.** BR-1 through BR-7 are closed in the updated spine:

- AD-24 and Structural Seed retain and bound `Projects.Testing`, classify client-generation projects as migration inputs, and bind the release inventory.
- AD-17 now gates writer cutover on convention-discovered `EventStoreAggregate<TState>`, `DomainResult` equivalence, complete historical `Apply` replay, discriminator compatibility, golden-history comparison, and removal of Projects-owned `IDomainProcessor`.
- AD-14 binds incremental reads to `IAsyncDomainProjectionHandler`, `IReadModelStore`/`IReadModelBatchStore`, `ReadModelWritePolicy`, `IDomainQueryHandler`, and scoped `IQueryCursorCodec`; legacy `IDomainProjectionHandler` is compatibility-only.
- AD-25, Structural Seed, and G-4 define the versioned manifest, pinned consumable runner, repository-owned run/down/test commands, manifest-aware fixtures, repository-relative paths, debug support, and clean-checkout plus CI package-mode parity. The commands are explicitly target contracts, not claims that the missing platform tool already exists; old Projects runtime projects cannot be removed until G-4 passes.
- Stack and G-6 correctly distinguish Dapr runtime `1.18.0` from .NET packages `1.18.4` and treat unused `Dapr=1.17.9` as catalog hygiene.
- The paradigm diagram now distinguishes domain contract dependencies from owner-neutral runtime invocation, keeping EventStore generic.
- The Server seed uses assembly-explicit `AddEventStoreDomainService(typeof(ProjectAggregate).Assembly)` with `UseEventStoreDomainService()`.

The remaining Durable Task, Confirmation Artifact, owner-contract, FrontComposer, runner, and environment work is correctly represented as fail-closed external entry gates rather than hidden current capability. Exact SDK/package facts still match `global.json`, the centralized package catalog, and the CI Dapr inputs. No brownfield blocker remains in the spine itself.
