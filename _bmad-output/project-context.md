---
project_name: 'Hexalith.Projects'
user_name: 'Jerome'
date: '2026-05-10'
sections_completed: ['technology_stack', 'language_rules', 'framework_rules', 'testing_rules', 'quality_rules', 'workflow_rules', 'anti_patterns']
status: 'complete'
rule_count: 76
optimized_for_llm: true
existing_patterns_found: 12
---

# Project Context for AI Agents

_This file contains critical rules and patterns that AI agents must follow when implementing code in this project. Focus on unobvious details that agents might otherwise miss._

---

## Technology Stack & Versions

- Root repository: umbrella repo composed of root-level Git submodules: `Hexalith.EventStore`, `Hexalith.FrontComposer`, `Hexalith.Parties`, `Hexalith.Tenants`, plus related modules.
- .NET SDK: `10.0.103` where pinned by `global.json`; projects target `net10.0`.
- C#: nullable enabled, implicit usings enabled, warnings treated as errors; several modules set `LangVersion` to `latest`.
- Package management: central NuGet package management via `Directory.Packages.props`.
- Dapr: newer modules use `Dapr.Client`, `Dapr.AspNetCore`, `Dapr.Actors`, and `Dapr.Actors.AspNetCore` around `1.17.7`; Dapr is the infrastructure abstraction.
- Aspire: `13.2.x` AppHost/service defaults pattern; local topologies run through Aspire AppHost projects.
- EventStore foundation: domain services build on Hexalith.EventStore command, aggregate, projection, query, SignalR, and testing packages.
- Blazor/Fluent UI: Microsoft Fluent UI Blazor `5.0.0-rc.2-26098.1`; FrontComposer uses Blazor Auto constraints.
- FrontComposer: Roslyn incremental generators, Fluxor `6.9.0`, MCP packages, generated Razor/Fluxor/registration artifacts.
- Testing: xUnit v3 in EventStore/Tenants/FrontComposer, xUnit v2 in Parties, Shouldly, NSubstitute, bUnit `2.7.2`, Testcontainers `4.10.0`, Playwright for E2E, and Verify/FsCheck in FrontComposer.
- JavaScript tooling: Node/npm is mainly for semantic-release, commitlint, Husky, and Playwright E2E; FrontComposer E2E requires Node `>=24.0.0`.

## Critical Implementation Rules

### Language-Specific Rules

- Keep `net10.0`, nullable reference types, implicit usings, and warnings-as-errors intact; do not weaken project-wide compiler settings to make a change pass.
- Use file-scoped namespaces matching the folder/project path, following the existing `Hexalith.*` namespace structure.
- Prefer sealed classes/records unless the existing extension point requires inheritance.
- Keep domain logic pure: aggregate `Handle(...)` methods return domain results/events, and state `Apply(...)` methods mutate only in-memory aggregate/read-model state.
- Command records use imperative names without a `Command` suffix, for example `CreateTenant` or `AddContactChannel`.
- Event records use past-tense domain names without an `Event` suffix, for example `TenantCreated`; rejection events use `Cannot` naming where the module already does so.
- Do not add `V2` event types for schema evolution; prefer additive, tolerant serialization-compatible changes.
- Use `System.Text.Json` conventions already used by EventStore/Tenants for contracts and event payloads.
- TypeScript in E2E tests is strict: no implicit `any`, strict null checks, no unused locals/parameters, ES modules, and path aliases under `tests/e2e`.

### Framework-Specific Rules

- Dapr is the permitted infrastructure abstraction. Domain services must not import Redis, PostgreSQL, Cosmos DB, broker clients, or other direct infrastructure clients.
- EventStore identity follows `{tenant}:{domain}:{aggregateId}`; derive state keys, actor IDs, topics, and projection keys from the established identity helpers/patterns.
- Domain service invocation goes through Dapr service invocation and EventStore routing conventions; avoid custom HTTP shortcuts between bounded contexts.
- Persist-then-publish is the EventStore event flow; do not publish events before persistence succeeds.
- Tenant isolation is mandatory at every layer: API/auth, aggregate identity, state keys, projection keys, pub/sub topics, query handlers, SignalR notifications, and logs.
- Query/read-side code should use EventStore query/projection infrastructure where available instead of inventing custom routing or state access.
- Aspire AppHost projects own local topology wiring; add service defaults/health/telemetry consistently with existing `ServiceDefaults` projects.
- FrontComposer source generation must preserve the parse -> transform -> emit pipeline; keep transform logic pure and testable with IR/model tests.
- FrontComposer generator discovery uses `ForAttributeWithMetadataName`; do not replace it with broad reflection-like Roslyn scans.
- Generated FrontComposer artifacts use `.g.cs` naming and the established generated headers; do not hand-edit generated output.
- Fluxor integration uses explicit `IState<T>` subscribe/dispose patterns; avoid `FluxorComponent` unless the architecture explicitly changes.
- Fluent UI v5 RC APIs are pinned and risky; follow existing component usage and do not upgrade Fluent UI casually.
- Blazor Auto matters: preserve server-to-WASM transition behavior, persistent state rules, and DI lifetime assumptions.

### Testing Rules

- Put tests in the module's existing `tests/` project structure; do not create ad hoc test locations.
- Match the module's xUnit generation: EventStore/Tenants/FrontComposer use xUnit v3 packages; Parties currently uses xUnit v2 packages.
- Use Shouldly for assertions and NSubstitute for mocks unless a local test project already uses a different pattern.
- Keep Tier 1 tests pure and fast: aggregate `Handle/Apply`, projection handlers, validators, transforms, and emitters should not require Dapr, Aspire, network, or containers.
- Use EventStore/Testing fakes and builders before creating new test doubles.
- Use Testcontainers or Aspire topology tests only for integration boundaries that genuinely need infrastructure.
- For FrontComposer source generators, test parse/transform/emitter units separately and compile generated output through generator-driver tests.
- For Blazor components, use bUnit; for browser/runtime behavior, use Playwright E2E under the existing E2E workspace.
- Preserve deterministic tests: avoid wall-clock sleeps where fake time, polling helpers, or explicit readiness probes exist.
- Security/tenant-isolation behavior needs negative-path tests, not only happy-path coverage.

### Code Quality & Style Rules

- Do not disable nullable, implicit usings, warnings-as-errors, analyzers, or package centralization to get a build green.
- Add package versions only in `Directory.Packages.props`; project files should reference packages without inline versions unless the module already has a local exception.
- Keep package/project boundaries intact: Contracts for DTOs/events/commands, Client for consumer-facing integration, Server for domain/server behavior, Aspire/AppHost for orchestration, Testing for reusable test utilities.
- Prefer existing extension methods, builders, fakes, validators, and service registration patterns over new abstractions.
- Avoid direct infrastructure coupling in domain packages; infrastructure-specific code belongs in host/server/integration layers.
- Public contract changes must be additive and serialization-tolerant unless a breaking change is explicitly requested.
- Keep generated-code logic in generators/emitters/templates, not in checked-in generated artifacts.
- Use XML documentation where the surrounding public API already documents package-facing members; avoid noisy comments on obvious implementation code.
- Preserve conventional commit tooling: commitlint extends `@commitlint/config-conventional`.
- Do not casually upgrade pinned RC/runtime packages; version bumps should be deliberate and cross-module aware.

### Development Workflow Rules

- This root is an umbrella repository with Git submodules. Initialize/update only root-level submodules unless nested submodules are explicitly requested.
- Never run `git submodule update --init --recursive` or equivalent recursive submodule commands without explicit nested-submodule approval.
- When working inside a submodule, treat that submodule as its own repository for status, tests, commits, and branches.
- Use module-specific solution files (`*.slnx` or `.sln`) and project-local commands; avoid assuming a single root solution builds every submodule.
- Prefer `dotnet test` or targeted project tests from the affected module; use Aspire AppHost only when validating topology/runtime behavior.
- FrontComposer E2E commands live in `Hexalith.FrontComposer/tests/e2e`; install browsers/dependencies there before running Playwright when needed.
- Conventional commits are expected by semantic-release/commitlint in modules that include `package.json` and `commitlint.config.mjs`.
- Do not mix unrelated submodule pointer updates with code changes unless the task explicitly requires it.
- Preserve existing uncommitted user changes across root and submodule repositories.

### Critical Don't-Miss Rules

- Do not recursively initialize or update nested submodules; root-level submodules only unless explicitly requested.
- Do not bypass Dapr for persistence, pub/sub, service invocation, configuration, or actors in domain services.
- Do not let tenant IDs, aggregate IDs, projection keys, SignalR groups, pub/sub topics, or logs drift from the canonical EventStore identity model.
- Do not log payloads or personal data; security/privacy rules are part of the architecture, not optional cleanup.
- Do not treat domain rejections as infrastructure failures; rejections are expected domain outcomes/events, while infrastructure failures are exceptions/dead-letter paths.
- Do not weaken authorization or tenant-validation paths to simplify tests; use fakes/builders/test auth schemes instead.
- Do not publish events, projection notifications, or SignalR messages before persistence and consistency requirements are satisfied.
- Do not create direct database/broker dependencies in contract/client/domain packages.
- Do not replace pure aggregate/projection handler logic with infrastructure-bound code.
- Do not collapse FrontComposer generator parse/transform/emit layers; this breaks testability and incremental-generator discipline.
- Do not hand-edit generated output or snapshot expected generated output without updating the generator/emitter source and tests.
- Do not upgrade Fluent UI, Dapr, Aspire, xUnit generation, Roslyn, or .NET SDK versions casually; cross-module compatibility matters.
- Do not assume all modules use the same xUnit major version; match the target module.
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

Last Updated: 2026-05-10
