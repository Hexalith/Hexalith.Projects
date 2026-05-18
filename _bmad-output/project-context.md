---
project_name: 'Hexalith.Projects'
user_name: 'Jerome'
sections_completed: ['technology_stack', 'language_rules', 'framework_rules', 'testing_rules', 'quality_rules', 'workflow_rules', 'anti_patterns']
status: 'complete'
date: '2026-05-12'
rule_count: 96
optimized_for_llm: true
existing_patterns_found: 12
---

# Project Context for AI Agents

_This file contains critical rules and patterns that AI agents must follow when implementing code in this project. Focus on unobvious details that agents might otherwise miss._

---

## Technology Stack & Versions

- Root repository: umbrella repo composed of root-level Git submodules: `Hexalith.AI.Tools`, `Hexalith.Conversations`, `Hexalith.EventStore`, `Hexalith.Folders`, `Hexalith.FrontComposer`, `Hexalith.Parties`, and `Hexalith.Tenants`.
- .NET SDK: `10.0.300` via `global.json` with `rollForward: latestPatch`; primary projects target `net10.0`.
- C#: nullable enabled, implicit usings enabled, warnings treated as errors; several modules set `LangVersion` to `latest`.
- Package management: central NuGet package management via `Directory.Packages.props`; package versions belong there, not inline in project files.
- Dapr: primary modules use `Dapr.Client`/`Dapr.AspNetCore` `1.17.7`; Parties still pins `Dapr.Actors`/`Dapr.Actors.AspNetCore` `1.16.1`.
- Aspire: AppHost/service defaults pattern using `Aspire.Hosting` `13.2.x` and `CommunityToolkit.Aspire.Hosting.Dapr` `13.0.0`.
- EventStore foundation: domain services build on Hexalith.EventStore command, aggregate, projection, query, SignalR, admin, CLI, MCP, and testing packages.
- Blazor/Fluent UI: Microsoft Fluent UI Blazor `5.0.0-rc.2-26098.1`; treat the RC API as pinned and high-risk.
- FrontComposer: Roslyn incremental generators with `Microsoft.CodeAnalysis.CSharp` exactly `4.12.0`, Fluxor `6.9.0`, MCP packages, and generated Razor/Fluxor artifacts.
- Testing: xUnit v3 in EventStore/Tenants/FrontComposer, xUnit v2 in Parties, Shouldly `4.3.0`, NSubstitute `5.3.0` or module-pinned version, bUnit `2.7.2`, Testcontainers `4.10.0`, Verify `31.15.0`, FsCheck `3.3.1`, BenchmarkDotNet `0.15.4`, and Playwright for E2E.
- JavaScript tooling: Node/npm is mainly for semantic-release, commitlint, Husky, and Playwright E2E; FrontComposer E2E requires Node `>=24.0.0`.

## Critical Implementation Rules

### Language-Specific Rules

- Keep `net10.0`, nullable reference types, implicit usings, and warnings-as-errors intact; do not weaken project-wide compiler settings to make a change pass.
- Use file-scoped namespaces matching the folder/project path, following the existing `Hexalith.*` namespace structure.
- Follow `.editorconfig`: 4-space indentation, CRLF, UTF-8, final newline, private fields with `_camelCase`, interfaces prefixed with `I`, and async methods suffixed with `Async`.
- Prefer sealed classes/records unless the existing extension point requires inheritance.
- Command records use imperative names without a `Command` suffix, for example `CreateTenant` or `AddContactChannel`.
- Event records use past-tense domain names without an `Event` suffix, for example `TenantCreated`; rejection events implement `IRejectionEvent`.
- Do not add `V2` event types for schema evolution; prefer additive, tolerant serialization-compatible changes.
- Domain result events must not mix success payloads and rejection payloads in one `DomainResult`.
- Use `System.Text.Json` conventions already used by EventStore/Tenants/Parties for contracts and event payloads.
- TypeScript in E2E tests is strict: no implicit `any`, strict null checks, no unused locals/parameters, ES modules, and path aliases under `tests/e2e`.

### Framework-Specific Rules

- Dapr is the permitted infrastructure abstraction. Domain services must not import Redis, PostgreSQL, Cosmos DB, broker clients, or other direct infrastructure clients.
- EventStore identity follows `{tenant}:{domain}:{aggregateId}`; derive actor IDs, state keys, topics, projection keys, SignalR groups, and logs from the established identity helpers/patterns.
- EventStore owns event envelope metadata. Domain services return event payloads only and must not populate or spoof envelope fields.
- Persist-then-publish is the EventStore event flow; do not publish events before persistence succeeds.
- Keep domain logic pure: aggregate `Handle(...)` methods return domain results/events, and state/read-model `Apply(...)` methods mutate only in-memory state.
- Domain rejections are expected domain outcomes, represented by rejection events; infrastructure failures are exceptions/dead-letter paths.
- Tenant isolation is mandatory at every layer: API/auth, aggregate identity, state keys, projection keys, pub/sub topics, query handlers, SignalR notifications, and logs.
- Use EventStore query/projection infrastructure, including `CachingProjectionActor`, ETag actors, and projection notifiers where available, before inventing custom read-side routing.
- Admin CLI and MCP clients call Admin API over HTTP; they do not access Dapr directly or bypass the EventStore command pipeline.
- Tenants uses `system` as the platform tenant context; managed tenant IDs belong in event payloads and read models, not as the EventStore envelope tenant.
- Tenants query handlers must apply query-side authorization/result filtering where required; command-side RBAC and API JWT checks are not enough for user-search style queries.
- Parties MCP operations are a translation layer over commands and queries. Do not put domain logic or event-type references in MCP tool code.
- Parties composite commands are processed in a single aggregate turn with explicit payload limits; avoid sequential MCP command orchestration for atomic create/update flows.
- Aspire AppHost projects own local topology wiring; add service defaults/health/telemetry consistently with existing `ServiceDefaults` projects.
- FrontComposer source generation must preserve the parse -> transform -> emit pipeline; keep transform logic pure and testable with IR/model tests.
- FrontComposer generator discovery uses `ForAttributeWithMetadataName`; do not replace it with broad reflection-like Roslyn scans.
- Generated FrontComposer artifacts use `.g.cs` naming and the established generated headers; do not hand-edit generated output.
- FrontComposer generator emits Fluxor-related code as strings/fully-qualified names; SourceTools must not depend directly on Fluxor.
- FrontComposer production components currently use Fluxor `IState<T>` and, where present, `FluxorComponent`; generated components should follow the architecture's explicit subscribe/dispose pattern unless the local component pattern says otherwise.
- Fluent UI v5 RC APIs are pinned and risky; follow existing component usage and do not upgrade Fluent UI casually.
- Blazor Auto matters: preserve server-to-WASM transition behavior, persistent state rules, SignalR reconnection behavior, and DI lifetime assumptions.

### Testing Rules

- Put tests in the module's existing `tests/` or sample test project structure; do not create ad hoc test locations.
- Match the module's xUnit generation: EventStore/Tenants/FrontComposer use xUnit v3 packages; Parties currently uses xUnit v2 packages.
- Use Shouldly for assertions and NSubstitute for mocks unless the target test project already establishes a different pattern.
- Keep Tier 1 tests pure and fast: aggregate `Handle/Apply`, projection handlers, validators, source-generator parse/transform, and emitters should not require Dapr, Aspire, network, browser, or containers.
- Use EventStore/Testing fakes, Tenants/Testing fakes, builders, and local helpers before creating new test doubles.
- Use Testcontainers, Dapr slim tests, or Aspire topology tests only for integration boundaries that genuinely need infrastructure.
- For EventStore security/runtime proof, E2E security tests use real Keycloak/OIDC tokens; synthetic JWT generators are only for fast unit/integration tests.
- For tenant isolation, test both defense layers where relevant: API/JWT authorization and domain/query-side filtering.
- For Tenants projections, include cross-tenant index concurrency and query-side authorization negative-path tests when touching those paths.
- For Parties composite commands, story/test specs should include explicit case catalogs before implementation; cover mixed add/update/remove, duplicates, conflicts, payload limits, and partial-no-op behavior.
- For Parties projections, keep handler logic Tier 1 testable with no Dapr references; actor lifecycle belongs in higher-tier tests.
- For FrontComposer source generators, test parse/transform/emitter units separately and compile generated output through generator-driver tests.
- For FrontComposer generated output, use Verify snapshot baselines with `Verify.XunitV3`, not xUnit v2 Verify packages.
- For Blazor components, use bUnit; for browser/runtime behavior, use Playwright E2E under the existing E2E workspace.
- Preserve deterministic tests: avoid wall-clock sleeps where fake time, polling helpers, explicit readiness probes, or SignalR/test abstractions exist.
- Quarantined/flaky tests must be explicitly marked and excluded from the main lane; do not silently weaken assertions to make unstable tests pass.
- Security, tenant isolation, privacy, and payload-protection behavior need negative-path tests, not only happy-path coverage.

### Code Quality & Style Rules

- Do not disable nullable, implicit usings, warnings-as-errors, analyzers, central package management, or `.editorconfig` rules to get a build green.
- Add package versions only in the nearest applicable `Directory.Packages.props`; project files should reference packages without inline versions unless the module already has a local exception.
- Keep package/project boundaries intact: Contracts for DTOs/events/commands/models, Client for consumer-facing integration, Server for domain/server behavior, Projections for read-side projection logic where the module has it, Aspire/AppHost for orchestration, Testing for reusable test utilities.
- Keep dependency direction strict and machine-checkable: Contracts must stay low-dependency; Server depends inward; API/CommandApi hosts orchestration; MCP/CLI clients do not reference domain event types or Dapr.
- Prefer existing extension methods, builders, fakes, validators, service registration patterns, and options objects over new abstractions.
- Public contract changes must be additive and serialization-tolerant unless a breaking change is explicitly requested.
- Domain packages must not take direct infrastructure dependencies; infrastructure-specific code belongs in host/server/integration layers.
- EventStore admin reads use the approved state-store/key-derivation path; writes are delegated through the EventStore command pipeline.
- Keep generated-code logic in generators/emitters/templates, not in checked-in generated artifacts.
- Do not hand-edit `.g.cs` or other generated output; update the generator/emitter and tests instead.
- Use XML documentation where the surrounding public API already documents package-facing members; avoid noisy comments on obvious implementation code.
- Error responses should use the established ProblemDetails/RFC 7807 or RFC 9457 patterns with correlation/tenant context where applicable.
- Logging must use structured metadata and never log event payloads, personal data, secrets, raw tokens, or full command bodies.
- Preserve conventional commit tooling: commitlint extends `@commitlint/config-conventional`.
- Do not casually upgrade pinned RC/runtime packages; version bumps should be deliberate and cross-module aware.

### Development Workflow Rules

- This root is an umbrella repository with Git submodules. Initialize/update only root-level submodules unless nested submodules are explicitly requested.
- Never run `git submodule update --init --recursive` or equivalent recursive submodule commands without explicit nested-submodule approval.
- When working inside a submodule, treat that submodule as its own repository for status, tests, commits, and branches.
- Do not mix unrelated submodule pointer updates with code changes unless the task explicitly requires it.
- Preserve existing uncommitted user changes across root and submodule repositories.
- Use module-specific solution files (`*.slnx` or `.sln`) and project-local commands; avoid assuming a single root solution builds every submodule.
- Prefer `dotnet test` or targeted project tests from the affected module; use Aspire AppHost only when validating topology/runtime behavior.
- FrontComposer E2E commands live in `Hexalith.FrontComposer/tests/e2e`; install browsers/dependencies there before running Playwright when needed.
- FrontComposer local dependency resolution must use the authoritative root `Hexalith.EventStore` path from `deps.local.props`; do not reference Tenants' nested EventStore copy.
- Module `Directory.Build.props` files can be affected by MSBuild walk-up; preserve existing guards/import switches when changing project structure.
- Conventional commits are expected by semantic-release/commitlint in modules that include `package.json` and `commitlint.config.mjs`.
- Parties uses MinVer tag-based versioning; do not assume every module releases through semantic-release.
- CI/test lanes may separate unit, integration, E2E, benchmark, and quarantined tests; keep category filters and traits intact.
- Do not casually change Node engine requirements, Playwright workspace layout, or npm script names used by CI.

### Critical Don't-Miss Rules

- Do not recursively initialize or update nested submodules; root-level submodules only unless explicitly requested.
- Do not bypass Dapr for persistence, pub/sub, service invocation, configuration, or actors in domain services.
- Do not let tenant IDs, aggregate IDs, projection keys, SignalR groups, pub/sub topics, or logs drift from the canonical EventStore identity model.
- Do not let domain services populate EventStore envelope metadata; EventStore owns envelope fields.
- Do not treat unknown event types during rehydration as normal. Domain services must maintain backward-compatible deserialization for every event type they have produced.
- Do not log payloads, personal data, secrets, raw tokens, or full command bodies; security/privacy rules are architecture requirements.
- Do not treat domain rejections as infrastructure failures; rejections are expected domain outcomes/events, while infrastructure failures are exceptions/dead-letter paths.
- Do not weaken authorization, tenant validation, query-side filtering, or privacy paths to simplify tests; use fakes/builders/test auth schemes instead.
- Do not publish events, projection notifications, or SignalR messages before persistence and consistency requirements are satisfied.
- Do not create direct database/broker dependencies in contract/client/domain packages.
- Do not replace pure aggregate/projection handler logic with infrastructure-bound code.
- Do not collapse FrontComposer generator parse/transform/emit layers; this breaks testability and incremental-generator discipline.
- Do not hand-edit generated output or snapshot expected generated output without updating the generator/emitter source and tests.
- Do not add domain logic to Parties MCP tools; MCP translates intent to commands/queries and returns results.
- Do not implement Parties composite create/update flows as sequential MCP command orchestration when atomic composite commands are required.
- Do not assume Tenants read/query authorization is covered by EventStore JWT checks; some queries require row-level/result filtering.
- Do not upgrade Fluent UI, Dapr, Aspire, xUnit generation, Roslyn, or .NET SDK versions casually; cross-module compatibility matters.
- Do not assume all modules use the same xUnit major version, Dapr actor version, release tooling, or test lane layout.
- Do not mix submodule pointer updates into unrelated changes.

---

## Usage Guidelines

**For AI Agents:**

- Read this file before implementing any code in this workspace.
- Follow all rules exactly as documented.
- When in doubt, prefer the more restrictive option.
- Update this file when durable project patterns change.

**For Humans:**

- Keep this file lean and focused on agent needs.
- Update it when technology stack, workflow, or architecture decisions change.
- Review periodically for outdated rules.
- Remove rules that become obvious or stop preventing real mistakes.

Last Updated: 2026-05-12
