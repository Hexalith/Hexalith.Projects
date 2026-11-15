---
title: 'Inspect Projects through an authenticated CLI read surface'
type: 'feature'
created: '2026-11-15'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
  - '{project-root}/references/Hexalith.McpCli/AGENTS.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Tenant Operators and automation callers cannot read authorized Project truth (FR-22) from a terminal. The only CLI, `Hexalith.Projects.Cli`, calls legacy REST routes without credentials, ignores unknown options and lets `audit` query diagnostics.

**Approach:** Enroll the accepted 6.1–6.4 supported read queries in `Hexalith.McpCli` as decorated Projects Contracts queries. McpCli submits the same EventStore Gateway queries as the 6.5 Web surface, so facts, reason codes and timestamps match by construction.

## Boundaries & Constraints

**Decisions (Jerome, 2026-11-15):**
- **D1 Sequencing:** Story 6.8 lands first as its own story, and 6.6 resumes only after 6.8 is done.
- **D2 Authority:** 6.6 advances only under an approved `bmad-correct-course` correction. It adds a guarded scheduling exception (6.8 before 6.6) and changes `validate_production_authority.py` and its tests in the same commit.
- **D3 Host:** McpCli enrollment. `Hexalith.Projects.Cli` stays frozen and unchanged.
- **D4 Lane:** the same correction extends the individual Debug test-project exception to 6.6.
- **D5 Names:** set explicitly through `[HexalithQuery(Name=…)]`: `projects.list`, `projects.describe`, `projects.trace-conversation` and `projects.trace-attachments`. `describe` returns descriptive fields only when authorized, and its reference states cover `validate`. The correction amends UX-DR19 and the 6.6 AC to match.
- **D6 Stale exit:** McpCli is unchanged. A 200 response with `responseState` Unavailable or Partial exits 0, and the JSON carries the truth. The correction amends the 6.6 AC.
- **D7 Contracts closure (Jerome, 2026-11-15):** Contracts drops its `Hexalith.Conversations.Contracts` dependency so its closure fits McpCli's dependency policy. `ProjectConversationItem.ConversationId` becomes a Projects-owned string whose JSON keeps the byte-identical `conv:<id>` form. This is the only response-record change allowed. It is CLR-level only, and it ships in the same unreleased major version as Story 6.8.
- **D8 Missing credential (Jerome, 2026-11-15):** McpCli is unchanged. The Gateway rejects a call without a token (401, exit 2, no payload) before any Projects query executes. This amends the 6.6 AC's "fails before any query is sent".

**Always:** Identity and Tenant come from the platform credential, and the Gateway authorizes every call. Output is Safe Metadata by default; descriptive fields appear only with `projects:inspect_descriptive_metadata`, under the server's existing durable audit. Output is deterministic JSON, and no meaning depends on color, width or prose. Denied, cross-Tenant and absent targets return one safe result. Confirmation-required actions are denied.

**Never:**
- Changes to `Hexalith.Projects.Cli`.
- Module-specific McpCli adapters.
- Writes, previews, maintenance or Diagnostic Export.
- An audit operation before Story 8.1.
- Echoing remote detail.
- Changing query wire names, payloads, response records (apart from the D7 CLR change) or server admission.
- Changing the 6.5 sources.
- Routing cutover (Story 6.7).
- Running `sprint_plan.py`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Authorized list | Valid credential, page ≤ 200 | `ProjectListPage` JSON with snapshot and opaque cursor; exit 0 | N/A |
| Descriptive inspection | Permission present / absent | Name and Setup present and audited / null | Audit write failure denies |
| Denied, absent or foreign target | Any operation | Identical safe 404 error; exit 2; no payload | No remote detail |
| Missing credential | Any operation | Gateway rejects with 401 before any Projects query executes; exit 2; no payload (D8) | Safe error code |
| Stale or unavailable model | `responseState` Unavailable or Partial | Snapshot and components reported truthfully; exit 0 | N/A |
| `audit` | Any | Not enrolled, so the operation is absent | No query |

</frozen-after-approval>

## Code Map

Projects (paths from the repo root):
- `src/Hexalith.Projects.Contracts/Queries/{ListProjectsQuery,GetProjectQuery,ResolveProjectFromConversationQuery,ResolveProjectFromAttachmentsQuery,ProjectAdvisoryAttachment}.cs` -- decoration targets. Each has a `QueryType` constant `Hexalith.Projects.Queries.<Name>.v1`. Payload keys are fixed by the server allowlists (`Server/Queries/ProjectReadQueryExecutor.cs:42-47`, `ProjectAdvisoryQueryExecutor.cs:72,86-88`). `GetProjectQuery` has no id property; the project id is the aggregate id.
- `src/Hexalith.Projects.Contracts/Queries/ProjectConversationItem.cs:10` -- the only Conversations use in Contracts (D7).
  - Today `ConversationId` writes `"conv:<value>"` (`references/Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Serialization/{PrefixedIdentifierJsonConverter.cs,IdentifierJsonConverters.cs:10-16}`).
  - Typed consumers: `src/Hexalith.Projects.Server/Conversations/{ProjectConversationTranslator.cs:62-68,ProjectContextConversationEvidenceMapper.cs:57-63}` and `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs:26,841`.
  - The UI reads the generated client's own `string` copy (`src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs:5800`), and Server references Conversations directly.
- `Directory.Build.props:17-39`, `src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj` -- the `UseHexalithProjectReferences` switch and the `Hexalith*Root` probes. `HexalithMcpCliRoot` does not exist yet.
- `references/Hexalith.Parties/src/Hexalith.Parties.Contracts/{Hexalith.Parties.Contracts.csproj:17-18,Module.cs,Queries/GetParty.cs,Queries/ListParties.cs}` -- the decoration and reference pattern.
- `tools/release-packages.json:21-34` -- the Contracts graph. `tests/tools/run-package-dependency-gate.ps1` and `scripts/validate-nuget-packages.py` compare nuspec dependencies exactly.
- `src/Hexalith.Projects.UI/Diagnostics/ProjectReadQueryClient.cs:42-69` and its sources -- the 6.5 request shape:
  - domain `projects`;
  - target `projects` for list and the traces, the project id for get;
  - Web camelCase payload, `ProjectionType` unset.
  Do not change.
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs:282-295` -- for the four query types, 400 for an invalid envelope or cursor; otherwise 404 `QueryResult.Failure("safe-denial")`. Do not change.
- `tests/Hexalith.Projects.Server.Tests/Queries/SupportedProjectGatewayHttpTests.cs:166-280,523-580` -- runs the real EventStore `QueriesController` in-process with authorization and builds all four requests. Home for the matrix tests.
- `tests/Hexalith.Projects.Contracts.Tests/Queries/` -- home for the decoration tests.

McpCli (`references/Hexalith.McpCli/`):
- `src/Hexalith.McpCli/Hexalith.McpCli.csproj:22-24` -- the enrollment point. Version pins are in `Directory.Build.props:4-8`.
- `src/Hexalith.McpCli.Core/Catalog/{CatalogBuilder.cs:245,276,RoutingResolver.cs:40-87}` -- the full name is module + `.` + `Name`, and `Name` may contain no dot. Every query needs a `ProjectionType`.
- `src/Hexalith.McpCli.Core/Execution/OperationExecutor.cs:229-276` -- the aggregate id is `--aggregate-id`, else the property, else the constant. The payload is sent as-is.
- `tools/dependency-policy.json` -- the `modules` map. The closure must fit the baseline plus `Hexalith.McpCli.Abstractions` 2.0.1, which already has every attribute member needed.
- Enrollment artifacts:
  - `tools/inventory-coverage/v1/<module>-{approved,accepted-lint}.json`;
  - `tools/conformance-vectors/v1/current/modules/<PackageId>/{*.vector.json,approval.json}` (example: `Hexalith.Parties.Contracts/get-party.vector.json`);
  - `tools/live-topology/plans-current/<operation>.json` (`vectorSha256`);
  - `tools/conformance-vectors/v1/{check_enrolled.py,verify_artifact.py}`.
- `_bmad-output/implementation-artifacts/projects-migration-inventory-v1.md` -- frozen. For the v2 successor format, follow `parties-migration-inventory-v1.md`.
- `README.md:372-374` (error privacy) and `:499-527` (enrollment) -- update the operation list there.

## Tasks & Acceptance

**Execution:**
- [ ] `src/Hexalith.Projects.Contracts/Queries/{ProjectConversationItem,ProjectConversationIdJsonConverter}.cs`, the typed consumers above and the Contracts csproj -- make `ConversationId` a validated, non-whitespace `string`, and add a property converter that reads and writes `conv:<value>`. Then drop both Conversations item groups from the Contracts csproj -- D7, with byte-identical JSON.
- [ ] `tests/Hexalith.Projects.Server.Tests/Conversations/ProjectConversationItemWireTests.cs` -- serialize the item and check that the JSON equals Conversations' own `ConversationId` converter output and round-trips -- this guards the restated prefix.
- [ ] `Directory.Build.props`, `src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj` -- add `HexalithMcpCliRoot`. Reference Abstractions by source when `UseHexalithProjectReferences` is true; otherwise use a versionless package reference that resolves to McpCli's decoration pin, 2.0.1 -- this follows E8.5 and E8.6.
- [ ] `src/Hexalith.Projects.Contracts/ProjectsMcpCliModule.cs` and the five query files -- decorate as follows (no wire change):
  - `[assembly: HexalithModule("projects", …, IdentifierKind.String)]`;
  - `[HexalithQuery(…, Name = "list"|"describe"|"trace-conversation"|"trace-attachments", Domain = "projects", WireType = <QueryType>, ProjectionType = "projects")]`, adding `AggregateId = "projects"` on list and both traces;
  - `[property: Description]` on every member, and `[HexalithIdentifier]` on `ConversationId` and `ReferenceId`.
- [ ] `tools/release-packages.json` -- record the new Contracts graph and every other entry whose graph changes.
- [ ] `tests/Hexalith.Projects.Contracts.Tests/Queries/ProjectMcpCliDecorationTests.cs` -- check that:
  - names and routing values equal the 6.5 constants;
  - the serialized payload keys equal the server allowlists;
  - the setup and context queries, and any audit query, stay undecorated.
- [ ] `tests/Hexalith.Projects.Server.Tests/Queries/SupportedProjectGatewayHttpTests.McpCli.cs` (partial class) -- run the I/O matrix through the real Gateway:
  - McpCli-shaped requests (built from the attributes) and 6.5-shaped requests return equal status and JSON;
  - the descriptive grant behaves as specified, including audit-failure denial;
  - the 404 is byte-identical apart from `correlationId`;
  - a call without a token gets 401 and the handler is never invoked;
  - `Partial` and `Unavailable` return 200;
  - the persisted-store delta is inspection audits only.
- [ ] McpCli -- all enrollment changes:
  - the csproj enrollment, and `HexalithProjectsVersion` set to the local candidate;
  - the `modules` entry;
  - `projects-approved.json` (T01–T04 include), and `projects-accepted-lint.json` for the `payload_paging_member` findings on `ListProjectsQuery`;
  - one vector per Operation Shape plus `approval.json`;
  - one live plan per declared kind;
  - the README operation list;
  - `projects-migration-inventory-v2.md` as E8.4 specifies.
- [ ] `_bmad-output/implementation-artifacts/evidence/epic6/6.6-cli-read-surface.{trx,json}` -- record totals, the tested Projects and McpCli revisions and the candidate `.nupkg` SHA-256.

**Acceptance Criteria:**
- Given identical facts, when McpCli and the 6.5 Web read the same Project, then lifecycle and reference states, reason codes, `asOf`/`lastVerifiedAt` timestamps and warnings are semantically equal.
- Given the McpCli build with Projects enrolled, when `dotnet list package --include-transitive` runs, then no FrontComposer, Fluxor, Fluent UI or `Microsoft.AspNetCore.App` dependency comes in through Projects Contracts.
- Given any operation, when persisted state is compared before and after, then no domain event or read-model write occurred apart from the inspection audit.
- Given a legacy conversations page, when it is serialized before and after D7, then its JSON is byte-identical, and `Hexalith.Projects.Contracts` declares no Conversations or Commons.Serialization dependency.
- Given offline strict discovery, when `hexalith operations projects --strict` runs, then it lists exactly `projects.list`, `projects.describe`, `projects.trace-conversation` and `projects.trace-attachments`, and describe lint is clean or carries only an approved accepted-lint row.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

- **Names.** D5's names are module `projects` plus `Name`, because McpCli rejects a dot inside `Name`.
- **`projects.describe` addressing.** The project id comes from `--aggregate-id`: the payload cannot gain a `projectId`, since that would change the payload and server admission.
- **`ProjectionType = "projects"`.**
  - Outside route-catalog mode, this is what the Gateway substitutes for an unset type (`references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/QueriesController.cs:374-376`).
  - Under a route catalog, an explicit type must equal the declared one (`:315`).
  - The gateway test shows which case applies. If the Gateway rejects the type, HALT: no Gateway or McpCli routing change is authorized.
- **Safe 404.** McpCli relays the Projects sentinel `safe-denial` as `detail`. Only the per-call `correlationId` differs. Naming a Tenant the credential does not grant is a Gateway authorization failure (403, exit 2, no payload), not a target denial.
- **Local candidate.** Pack Contracts in Release package mode with an isolated `NUGET_PACKAGES`. The umbrella `nuget.config` maps every package to nuget.org, so restore McpCli through a task-owned config with a package-source mapping for `Hexalith.Projects.Contracts`, or through `verify_artifact.py`.

## Verification

**Commands (D4 lane):**
- From each of `tests/Hexalith.Projects.{Contracts,Server,UI}.Tests`, run `dotnet build --configuration Debug -m:1 -p:NuGetAudit=false --disable-build-servers`, then invoke the built assembly with native `-xml`. Server.Tests needs the disposable in-memory daprd through the `STORY61_DAPR_*` endpoints, and needs a run outside the sandbox. Expected: every matrix row passes, with no new failures.
- `pwsh tests/tools/run-package-dependency-gate.ps1` (package mode, isolated `NUGET_PACKAGES`) -- expected: passes on the new manifest.
- In McpCli:
  - `dotnet restore Hexalith.McpCli.slnx`, then `dotnet build Hexalith.McpCli.slnx --no-restore --configuration Release -warnaserror`;
  - `dotnet test tests/Hexalith.McpCli.Manifest.Tests --no-build --configuration Release`;
  - `dotnet tests/Hexalith.McpCli.Core.Tests/bin/Release/net11.0/Hexalith.McpCli.Core.Tests.dll -class Hexalith.McpCli.Core.Tests.EnrolledModuleCoverageTests`;
  - `python3 tools/conformance-vectors/v1/check_enrolled.py --local --output <dir>`.
  Expected: all pass, with Projects listed.
- `python3 tools/planning/validate_production_authority.py --validate-index` -- expected: passes after every status change.
