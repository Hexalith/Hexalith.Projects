# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Shared Hexalith LLM Instructions

Before starting any work in this repository, read and follow
[`Hexalith.AI.Tools\hexalith-llm-instructions.md`](./references/Hexalith.AI.Tools/hexalith-llm-instructions.md).

## Repository Shape

This is an **umbrella repository** composed of 10 root-declared Git submodules under `references/`. There is no root solution and no root build — each submodule is its own .NET repository with its own solution (`*.slnx` / `*.sln`), tests, release tooling, and CI.

Submodules: `references/Hexalith.AI.Tools`, `references/Hexalith.Builds`, `references/Hexalith.Commons`, `references/Hexalith.Conversations`, `references/Hexalith.EventStore`, `references/Hexalith.Folders`, `references/Hexalith.FrontComposer`, `references/Hexalith.Memories`, `references/Hexalith.Parties`, `references/Hexalith.Tenants`.

When working inside a submodule, treat that submodule as its own repository for status, tests, commits, and branches. Do not mix submodule pointer updates with code changes unless the task explicitly requires it.

## Git Submodules

- Initialize only submodules declared in the root `.gitmodules`, never initialize nested submodules.
- Never initialize or update nested submodules recursively unless the user explicitly asks for nested submodules.
- For repositories with submodules, initialize/update only root-declared submodules by default.
- Avoid `git submodule update --init --recursive` and similar recursive submodule commands unless nested submodule initialization is explicitly requested.
- This is a mandatory constraint for Claude Code: never initialize/update nested submodules unless explicitly requested.
- **Do not read bmad folders inside submodules.** Each root submodule can run its own independent bmad / story-automator orchestration (own marker, sprint-status, settings). Keep bmad reads scoped to the umbrella root's `_bmad/`, `_bmad-output/`, and `.claude`/`.codex` — never read `references/Hexalith.*/_bmad`, `references/Hexalith.*/_bmad-output`, `references/Hexalith.*/.claude/skills/bmad-*`, or `references/Hexalith.*/.codex` unless explicitly asked to work inside that submodule's bmad.

## Authoritative Rule Set

`_bmad-output/project-context.md` contains the curated rules for this workspace (technology versions, language/framework/testing/quality/workflow rules, and critical anti-patterns). Read it before implementing changes — it captures the non-obvious invariants that aren't visible from the code alone. Sections below summarize only the load-bearing architectural shape; do not treat them as a substitute.

## Architectural Big Picture

- **.NET 10** (`global.json` pins SDK `10.0.300`, `rollForward: latestPatch`), C# with nullable + implicit usings + warnings-as-errors. Central NuGet package management via `Directory.Packages.props` — versions live there, not inline.
- **Dapr is the only permitted infrastructure abstraction** in domain services. No direct Redis / PostgreSQL / Cosmos / broker clients in contracts/client/domain packages.
- **EventStore is the foundation.** Identity is canonical `{tenant}:{domain}:{aggregateId}` — derive actor IDs, state keys, topics, projection keys, SignalR groups, and log scopes from it. EventStore owns event envelope metadata; domain services return payloads only.
- **Event flow is persist-then-publish.** Aggregate `Handle(...)` is pure and returns domain results/events; projection/state `Apply(...)` mutates only in-memory state. Domain rejections are events (`IRejectionEvent`), not exceptions — infrastructure failures are exceptions/dead-letter paths.
- **Schema evolution is additive and serialization-tolerant.** Do not introduce `V2` event types; backward-compatible deserialization is required for every event ever produced.
- **Tenant isolation is enforced at every layer** (auth, aggregate identity, state keys, projection keys, pub/sub topics, queries, SignalR, logs). Tenants module uses `system` as the platform tenant context; managed tenant IDs live in payloads/read models, not in EventStore envelope tenant.
- **FrontComposer = Roslyn incremental source generators** with a strict parse → transform → emit pipeline. Generator discovery uses `ForAttributeWithMetadataName`. SourceTools must not depend on Fluxor; emit Fluxor code as strings/fully-qualified names. Never hand-edit `.g.cs`.
- **Aspire AppHost** projects own local topology; align with the existing `ServiceDefaults` pattern.
- **Fluent UI Blazor v5 RC is pinned** and high-risk; do not casually upgrade Fluent UI, Dapr, Aspire, xUnit generation, Roslyn, or the .NET SDK.

## Module Package Layout

Most modules follow this boundary structure — preserve it:

- `Contracts` — DTOs, events, commands, models (low-dependency, no infrastructure)
- `Client` — consumer-facing integration
- `Server` — domain/server behavior
- `Projections` — read-side projection logic (where present)
- `Aspire` / `AppHost` — orchestration
- `Testing` — reusable test utilities

Admin CLI and MCP clients call the Admin API over HTTP; they do not access Dapr directly or bypass the EventStore command pipeline. Parties MCP is a translation layer over commands/queries — no domain logic in MCP tool code.

## Build & Test Commands

There is no root build. Operate per submodule:

```bash
# from inside a submodule
dotnet build <Module>.slnx
dotnet test <path/to/test.csproj>            # prefer targeted module tests over root-wide runs
dotnet test --filter "FullyQualifiedName~SomeClass.SomeTest"
```

- xUnit major version differs per module: EventStore / Tenants / FrontComposer use **xUnit v3**; Parties uses **xUnit v2**. Match the surrounding project — including `Verify.XunitV3` for FrontComposer snapshots.
- Tier 1 tests (aggregate `Handle/Apply`, projection handlers, validators, generator parse/transform/emit) must stay pure — no Dapr, Aspire, network, browser, or containers. Use EventStore/Testing and Tenants/Testing fakes/builders before inventing new doubles.
- Integration tests use **Testcontainers**, Dapr slim tests, or Aspire topology tests only when a real boundary is needed.
- EventStore security/runtime proof uses **real Keycloak/OIDC tokens** in E2E; synthetic JWT generators are unit/integration only.
- **FrontComposer E2E** lives in `references/Hexalith.FrontComposer/tests/e2e` (Playwright, Node `>=24.0.0`). Install browsers there before running.
- **FrontComposer local dependency resolution** must use the root `references/Hexalith.EventStore` path from `deps.local.props` — do not reference Tenants' nested EventStore copy.

## Release & Versioning

- Most modules use **semantic-release + commitlint** (extends `@commitlint/config-conventional`) — conventional commits required where `package.json` + `commitlint.config.mjs` exist.
- **Parties uses MinVer** (tag-based versioning) — do not assume every module releases through semantic-release.

## Naming Conventions

- File-scoped namespaces matching folder path under `Hexalith.*`.
- 4-space indentation, CRLF, UTF-8, final newline (per `.editorconfig`). Private fields `_camelCase`; interfaces `I`-prefixed; async methods `Async`-suffixed.
- Command records: imperative, no `Command` suffix (`CreateTenant`, `AddContactChannel`).
- Event records: past tense, no `Event` suffix (`TenantCreated`); rejection events implement `IRejectionEvent`.
- Prefer sealed classes/records unless an existing extension point requires inheritance.
