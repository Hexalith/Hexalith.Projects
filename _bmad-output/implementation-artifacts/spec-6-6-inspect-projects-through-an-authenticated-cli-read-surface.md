---
title: 'Inspect Projects through an authenticated CLI read surface'
type: 'feature'
created: '2026-11-15'
status: 'done'
route: 'dispatch'
baseline_commit: 'ce71717a965eb21454b3e13068b9072404dac5f5'
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
- **D9 Routing correction (Jerome, 2026-11-15; session response "authorize"):** Resolve the demonstrated Gateway/McpCli routing-contract mismatch within 6.6. Computed domain queries preserve an absent projection selector: the four Projects decorations omit `ProjectionType` and `ProjectionActorType`, and the generic McpCli catalog accepts absent query projection selectors. Explicit selector validation, interface/attribute precedence, Gateway authorization and committed route matching remain unchanged. This authorizes the generic McpCli resolver/tests/documentation and corresponding enrollment-vector/hash updates; it does not authorize a module-specific adapter or a Gateway admission bypass. The previous projection-rejection HALT is resolved by this decision, with its failed evidence retained.

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
- `src/Hexalith.McpCli.Core/Catalog/{CatalogBuilder.cs:245,276,RoutingResolver.cs:40-87}` -- the full name is module + `.` + `Name`, and `Name` may contain no dot. D9 permits computed queries to omit both projection selectors; explicit selectors retain their existing validation and precedence.
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
- [x] `src/Hexalith.Projects.Contracts/Queries/{ProjectConversationItem,ProjectConversationIdJsonConverter}.cs`, the typed consumers above and the Contracts csproj -- make `ConversationId` a validated, non-whitespace `string`, and add a property converter that reads and writes `conv:<value>`. Then drop both Conversations item groups from the Contracts csproj -- D7, with byte-identical JSON.
- [x] `tests/Hexalith.Projects.Server.Tests/Conversations/ProjectConversationItemWireTests.cs` -- serialize the item and check that the JSON equals Conversations' own `ConversationId` converter output and round-trips -- this guards the restated prefix.
- [x] `Directory.Build.props`, `src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj` -- add `HexalithMcpCliRoot`. Reference Abstractions by source when `UseHexalithProjectReferences` is true; otherwise use a versionless package reference that resolves to McpCli's decoration pin, 2.0.1 -- this follows E8.5 and E8.6.
- [x] `src/Hexalith.Projects.Contracts/ProjectsMcpCliModule.cs` and the five query files -- decorate as follows (no wire change):
  - `[assembly: HexalithModule("projects", …, IdentifierKind.String)]`;
  - `[HexalithQuery(…, Name = "list"|"describe"|"trace-conversation"|"trace-attachments", Domain = "projects", WireType = <QueryType>)] (D9: omit both projection selectors)`, adding `AggregateId = "projects"` on list and both traces;
  - `[property: Description]` on every member, and `[HexalithIdentifier]` on `ConversationId` and `ReferenceId`.
- [x] `tools/release-packages.json` -- record the new Contracts graph and every other entry whose graph changes.
- [x] `tests/Hexalith.Projects.Contracts.Tests/Queries/ProjectMcpCliDecorationTests.cs` -- check that:
  - names and routing values equal the 6.5 constants;
  - the serialized payload keys equal the server allowlists;
  - the setup and context queries, and any audit query, stay undecorated.
- [x] `tests/Hexalith.Projects.Server.Tests/Queries/SupportedProjectGatewayHttpTests.McpCli.cs` (partial class) -- run the I/O matrix through the real Gateway:
  - McpCli-shaped requests (built from the attributes) and 6.5-shaped requests return equal status and JSON;
  - the descriptive grant behaves as specified, including audit-failure denial;
  - the 404 is byte-identical apart from `correlationId`;
  - a call without a token gets 401 and the handler is never invoked;
  - `Partial` and `Unavailable` return 200;
  - the persisted-store delta is inspection audits only.
- [x] McpCli `src/Hexalith.McpCli.Core/Catalog/RoutingResolver.cs`, `src/Hexalith.McpCli.Abstractions/HexalithQueryAttribute.cs`, existing Core catalog/executor tests and their fixture contracts, and `README.md` -- implement D9 generically: accept no query projection selector, preserve null through catalog and Gateway request, retain malformed/explicit selector guards and precedence, and cover both selector-free computed requests and existing explicit-selector behavior. Update relevant routing documentation and vectors, derive the new shapes through the existing C# implementation, and regenerate approvals/live-plan hashes. Run Core, Abstractions, CLI/MCP tests plus sample validation/loopback because Core routing validation changes.
- [x] McpCli -- all enrollment changes:
  - the csproj enrollment, and `HexalithProjectsVersion` set to the local candidate;
  - the `modules` entry;
  - `projects-approved.json` (T01–T04 include), and `projects-accepted-lint.json` for the `payload_paging_member` findings on `ListProjectsQuery`;
  - one vector per Operation Shape plus `approval.json`;
  - one live plan per declared kind;
  - the README operation list;
  - `projects-migration-inventory-v2.md` as E8.4 specifies.
- [x] `_bmad-output/implementation-artifacts/evidence/epic6/6.6-cli-read-surface.{trx,json}` -- record totals, the tested Projects and McpCli revisions and the candidate `.nupkg` SHA-256.

**Acceptance Criteria:**
- Given identical facts, when McpCli and the 6.5 Web read the same Project, then lifecycle and reference states, reason codes, `asOf`/`lastVerifiedAt` timestamps and warnings are semantically equal.
- Given the McpCli build with Projects enrolled, when `dotnet list package --include-transitive` runs, then no FrontComposer, Fluxor, Fluent UI or `Microsoft.AspNetCore.App` dependency comes in through Projects Contracts.
- Given any operation, when persisted state is compared before and after, then no domain event or read-model write occurred apart from the inspection audit.
- Given a legacy conversations page, when it is serialized before and after D7, then its JSON is byte-identical, and `Hexalith.Projects.Contracts` declares no Conversations or Commons.Serialization dependency.
- Given offline strict discovery, when `hexalith operations projects --strict` runs, then it lists exactly `projects.list`, `projects.describe`, `projects.trace-conversation` and `projects.trace-attachments`, and describe lint is clean or carries only an approved accepted-lint row.

## Implementation Notes

- D9 continuation is implemented and locally verified; independent review is complete; local feature verification is accepted and sprint tracking awaits human review. Computed queries preserve absent selectors through Contracts, the generic McpCli resolver and vector tooling. Explicit selector validation/precedence and Gateway admission remain unchanged. The final candidate is `2.0.0-story66.3`, SHA-256 `349f938db77dd83ff45e200997b1cba97147c46b1855ec58fd6e2a4b0572eff5`; pre-D9 artifacts/failed receipts remain retained.
- Release inventory decision rows T01–T04 represent the new supported targets; no legacy R/M/L/V identity was reused. All 46 legacy Markdown rows remain unchanged `defer / exclude`. Approval/lint references cite the approved proposal’s §E8 session decision; no external owning-maintainer sign-off is asserted. The refreshed Projects artifact and 14 coverage tests pass after this metadata clarification.
- Verification compares every Project payload field exactly, including `asOf` and `lastVerifiedAt`. Gateway `metadata.servedAt` uses direct wall-clock time per call (`QueriesController.cs:407`); each value must be non-null and within its request window and is treated separately from Project fact equality. Safe 404 bodies compare as original bytes after replacing only `correlationId`. No server or Web source changed.

- 2026-11-15 implementation handoff: added the four Contracts declarations, explicit camelCase payload names, the McpCli Abstractions source/package switch pinned at 2.0.1, the permitted CLR string Conversation reference and `conv:` property converter, typed consumer updates, and the revised release dependency graphs. Added decoration/wire tests and a real Gateway parity matrix. Drafted McpCli candidate enrollment, exact lint acceptances, four vectors covering two shapes, one query live plan, README notes and the v2 successor inventory; legacy sources and v1 inventory remain unchanged.
- **Historical pre-D9 HALT (resolved by D9).** The first McpCli-shaped list request sends `ProjectionType = "projects"`, while the fixture's committed declaration has `ProjectionType = null` (`tests/Hexalith.Projects.Server.Tests/Queries/GatewayFixtureRouteCatalog.cs:88`). The real Gateway's exact selector guard (`references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/QueriesController.cs:315`) rejects that mismatch. The matrix received HTTP 500 instead of 200. This matched the original pre-D9 Design Notes stop condition. No Gateway, catalog-routing or McpCli routing workaround was made. The remaining acceptance rows have not executed and no completion claim is made.
- Tested dirty working trees over Projects `ce71717a965eb21454b3e13068b9072404dac5f5` and McpCli `23072f04f423eea3f30456fe5af13d2d382624fe`; source hashes are retained in `evidence/epic6/6.6/source-manifest.json`. Candidate `Hexalith.Projects.Contracts` 2.0.0-story66.1 SHA-256: `c4f3a68ace2d295ccd816042bc6878e50301383be48c3c3dd1005000b169d09d`. No commits, pushes, publication or gitlink moves were performed.

## Spec Change Log

- 2026-11-15: Jerome authorized expanding 6.6 to resolve the observed routing-contract mismatch. Added D9, selector-free computed query routing, generic McpCli regression coverage and validation requirements; retained the prior failed run and all remaining boundaries.

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B1 | maybe-false | defer | Owner authority authenticity cannot be established from the repository. §E8 explicitly approves the four local enrollment artifacts; a missing external signature does not itself disprove a module-local owner decision. Confirm the signer capacity before DW-82 release/main integration; no external approval is asserted. |
| B2 | medium | patch | Confirmed with git check-ignore: the retained candidate nupkg is excluded by the global *.nupkg rule, so a future checkout cannot reproduce the cited artifact from this change. Add a narrow evidence exception. |
| B3 | medium | patch | Confirmed with git check-ignore: cited native logs, including the separate 373-test Manifest receipt, are excluded by *.log. Add a narrow retained-evidence exception. |
| B4 | medium | defer | The query live plan uses the offline vector's empty/Unavailable assertions without a native degraded fixture. This is not a qualified healthy live scenario. Native main integration is explicitly assigned to DW-82 by the human-approved proposal §E11, outside this local feature acceptance; qualify/replace the scenario before that integration. |
| B5 | medium | patch | ProjectLifecycle and ProjectContextFreshness declare string enum converters, while the canned vectors use numeric values. Correct the scripted response values and assert them, then refresh vector/plan bindings. |
| B6 | medium | patch | EqualGatewayReadsAsync constructs its Web request from the attribute-derived CLI request. The established 6.5 request constructors are available in the enclosing fixture; use them independently so routing drift can be detected. |
| B7 | medium | patch | The new integration matrix permits only SafeDenial audits to satisfy its nonempty audit assertion. Existing handler tests assert Inspection records, but the real Gateway matrix should separately read back successful list/detail Inspection records with expected counts/field classes. |
| B8 | medium | patch | The raw denied/absent/foreign byte comparison currently covers detail only. Add attribute-derived negative paths for the other enrolled operations while preserving the Gateway-vs-target distinction (unauthorized tenant envelope is 403). |
| B9 | medium | patch | The new matrix lists two records with page size 200 and never follows its own payload cursor. Existing Web-only continuation coverage does not exercise the new serialized cursor path. Add continuation and changed-bound-input rejection using the declared CLI payload. |
| B10 | medium | patch | The five raw keys include Conversations metadata/tenant state and Projects models, but omit Folder owner state; the write observer is installed only on Projects. Extend observation to participating owner persisted state and retain byte/ETag checks around reads. |
| V1 | medium | defer | Confirmed LiveVectorTests discovers all current plans, while the AppHost, routes and signed grants contain no Projects service/domain. The new plan cannot pass that native lane. The human-approved §E11 explicitly leaves McpCli main CI integration to DW-82; add this concrete prerequisite there without claiming live acceptance. |

### Final independent review — 2026-11-16

All three layers reported before triage. Blind hunter: ten findings; edge-case hunter: two findings; verification-gap reviewer: no gaps. Every finding was classified individually before grouping. Five carried findings preserve earlier DW-82 routes; five low findings are rejected with the evidence below, one is false, and the canonical D8 wording correction is the sole patch group.

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| R3-B1 | medium | defer (carried) | carried V1: automatic native discovery still lacks Projects topology/routes/grants. The approved E11 local acceptance leaves native main integration with DW-82; no new deferral is appended. |
| R3-B2 | medium | defer (carried) | carried B4: the list plan still binds offline empty/Unavailable assertions without arranging native state. Qualification remains a DW-82 prerequisite. |
| R3-B3 | maybe-false | defer (carried) | carried B1: signer capacity remains unverified; E8 authorizes local artifacts without asserting external owner sign-off. Release authority confirmation remains with DW-82. |
| R3-B4 | low | reject | Current trace parity inputs all use IncludeArchived=false; no existing parity case produces an incorrect comparison. Contracts payload-key tests preserve includeArchived and server advisory suites exercise lifecycle behavior. Adding true-valued fixtures/helper parameters expands test coverage without a demonstrated current read defect; the same suggestion was rejected in the prior review. |
| R3-B5 | low | reject | The matrix uses a reusable platform bearer credential and the unchanged admission checks authenticated actor/workload/delegation, audience, scopes and Tenant roles; it does not require an interactive session or operator-web identity. Additional noninteractive client fixtures would expand coverage; no failing credential path was shown. Current source/Gateway and actual catalog/transport replay evidence cover the approved local scope. |
| R3-B6 | low | reject | AdministratorInventoryAuditsEveryReturnedProjectAndBindsCursorToInventory and QuarantineDisclosureRequiresAdministratorPermissionAndDurableAudit both passed in fresh Server.xml. They cover authorized/denied quarantine and durable per-row audit. Decoration tests pin unchanged boolean payload names; duplicating these fixtures in the new matrix adds complexity without a demonstrated routing defect. |
| R3-B7 | low | reject | Fresh Server.xml passes InspectedAndChatbotListsAuditReturnedNamesOnceAndFailClosed for both modes and the real-Gateway InspectionNeedsDistinctPermissionAndDurableMetadataOnlyAuditIncludingUnchangedRetries for list/open. Both list inspection and audit-outage denial are already executed; a duplicate new-matrix row adds no demonstrated missing behavior. |
| R3-B8 | false | reject | The store observers capture SaveAsync/TrySaveAsync for every key, including new keys, and reject owner writes. The exercised read executors depend on read-model stores and audit adapters; they have no direct Dapr write or command-submission path. Raw bytes/ETags cover seeded and separate Partial models. No reachable mutation escaping this instrumentation was identified. |
| R3-B9 | low | reject (spec edit) | The matrix wording abbreviates default safe disclosure versus explicitly requested inspection; runtime tests correctly distinguish null default fields from 404 on unauthorized inspection. The proposed remedy edits this build specification, which review rules reject; frozen intent is not changed. |
| R3-B10 | medium | patch | The canonical epics.md criterion still says a missing credential fails before any query is sent. D8 expressly authorizes the exact correction to Gateway 401 before Projects execution; updating that one sentence requires no new behavior or decision. Resolve the matching deferred documentation entry after correction. |
| R3-E1 | medium | defer (carried) | carried V1: same plan/topology/grant gap and DW-82 route as R3-B1; its earlier disposition is preserved without another ledger entry. |
| R3-E2 | medium | defer (carried) | carried B4: same unarranged native empty/Unavailable fixture gap and DW-82 route as R3-B2; no duplicate deferral is added. |


Review layers: blind hunter supplied ten findings; edge-case hunter returned no confirmed findings; verification-gap reviewer supplied V1 as an Other finding. All findings were classified before grouping. B2/B3 share the missing evidence-ignore-exception cause; the other patch findings remain separate. Deferrals follow the approved local-only/main-integration boundary, not a claim that the native defects are already fixed.

- Parent task/acceptance verification: all ten implementation tasks checked against source diff and native/package/replay receipts. Every I/O matrix row is covered by a passed native test; see `evidence/epic6/6.6/d9/parent-matrix-audit.json`.

## Design Notes

- **Names.** D5's names are module `projects` plus `Name`, because McpCli rejects a dot inside `Name`.
- **`projects.describe` addressing.** The project id comes from `--aggregate-id`: the payload cannot gain a `projectId`, since that would change the payload and server admission.
- **Computed query routing (D9).** The four queries omit both projection selectors, matching the existing 6.5 Web request and committed computed declarations. `RoutingResolver` permits omission generically; an explicitly supplied selector still passes its existing validation and the Gateway's exact committed-target guard. Preserve the original rejected-selector receipt as historical failed evidence. D9 supersedes the original `ProjectionType = "projects"` requirement and its HALT; no Gateway change is needed.
- **Safe 404.** Projects returns its `safe-denial` sentinel to the Gateway; `QueryNotFoundExceptionHandler.cs:35` maps it to the canonical safe detail `The requested resource was not found.`. McpCli preserves that safe Gateway error. The raw Gateway error bodies differ only in per-call `correlationId`; no rejected target or remote domain detail is disclosed. Naming a Tenant the credential does not grant is a Gateway authorization failure (403, exit 2, no payload), not a target denial.
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

### Historical pre-D9 execution evidence — 2026-11-15 (required halt)

All disposable workspaces, feeds, caches and build outputs were owned by `run-with-temp`. Useful logs, candidate bytes and native XML are retained under `evidence/epic6/6.6/`; the historical incomplete machine-readable result is `evidence/epic6/6.6/pre-d9-summary.json`. Contracts' initial relocated-output attempt failed 26 repository-root lookup tests; the retained `contracts-relocation-failure.{xml,log}` documents that attempt. Repository-marker/document symlinks in the disposable workspace resolved it without changing expectations.

| Command / lane | Actual result |
| --- | --- |
| `dotnet build tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj --configuration Debug -m:1 -p:NuGetAudit=false --disable-build-servers --artifacts-path /tmp/hexalith-job.8i3cpR1a/build`, then the built DLL with `-xml <evidence>/contracts.xml` | Build passed, 0 warnings/errors; 207 passed, 0 failed/skipped. The actual output path is also bound by `contracts.xml` and `contracts-build.log`. |
| `dotnet pack src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj --configuration Release -p:UseHexalithProjectReferences=false -p:NuGetAudit=false -p:HexalithProjectsPackageVersion=2.0.0-story66.1 --artifacts-path /tmp/hexalith-job.NSjCywI2/build -o _bmad-output/implementation-artifacts/evidence/epic6/6.6/candidate -m:1 --disable-build-servers` | Exit 0; isolated package cache; candidate retained with SHA-256 above. |
| Platform Builds `prepare-build --manifest module/hexalith.module-manifest.v2.json --mode source --workspace /tmp/hexalith-job.6fjrI2Pz/prepared --output json` | Prepared; emitted environment used unchanged. HXW010/HXW011 qualification warnings only, retained in `source-preparation-final.json`. |
| `dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Debug -m:1 -p:NuGetAudit=false --disable-build-servers --artifacts-path /tmp/hexalith-job.6fjrI2Pz/build` | Final build exit 0, 0 warnings/errors (`server-build-final-2.log`). |
| Built Server.Tests DLL with native `-class Hexalith.Projects.Server.Tests.Conversations.ProjectConversationItemWireTests -xml <evidence>/conversation-wire.xml` | 10 passed, 0 failed/skipped; owner converter and legacy-page bytes round-trip. |
| Same DLL with `-class Hexalith.Projects.Server.Tests.Queries.SupportedProjectGatewayHttpTests -xml <evidence>/gateway-matrix.xml`, using fresh owned in-memory daprd and `STORY61_DAPR_*` endpoints | 10 total, 9 passed, 1 failed, 0 skipped. First attribute-derived list request returned 500; required halt. `gateway-matrix-tests.log` and `gateway-matrix.xml` retain the exact failure. |
| McpCli `dotnet restore Hexalith.McpCli.slnx -p:NuGetAudit=false -m:1`; `dotnet build Hexalith.McpCli.slnx --no-restore --configuration Release -warnaserror -m:1 --disable-build-servers` | Both exit 0; build 0 warnings/errors in a disposable copy with exact candidate-only source mapping and isolated cache. |
| McpCli `dotnet test tests/Hexalith.McpCli.Manifest.Tests --no-build --configuration Release` | 373 passed, 0 failed/skipped (`mcpcli-manifest-tests-final.log`). |
| McpCli built Core.Tests DLL with `-class Hexalith.McpCli.Core.Tests.EnrolledModuleCoverageTests -xml <evidence>/mcpcli-enrolled.xml` | 14 passed, 0 failed/skipped. |
| `python3 tools/conformance-vectors/v1/check_enrolled.py --local --output <evidence>/mcpcli-enrolled` | Projects child verification passed: four operations, both shapes, strict discovery/lint/inventory and CLI/MCP replay. The overall run was interrupted with exit 130 during Parties replay after the required Gateway halt; top-level receipt is pending, never a pass. |
| `/home/administrator/.dotnet/tools/pwsh tests/tools/run-package-dependency-gate.ps1 -PackageDirectory /tmp/hexalith-job.TP7qJXT4/feed` from `/tmp/hexalith-job.TP7qJXT4/projects` with isolated caches and package mode | Exit 1: copied workspace lacked Platform's linked `DaprSelfHostedMtls.cs` and `DaprSelfHostedMtlsSubscriber.cs`; Integration.Tests compiler CS2001. See `package-dependency-gate-final.log`. The earlier copy also omitted EventStore/Tenants runtime project paths (`package-dependency-gate.log`). Neither result establishes a repository dependency-graph failure; no gate was weakened or source changed. |
| `python3 tools/planning/validate_production_authority.py --validate-index` | Exit 0: production-authority index `[6, 7, 8]`. |

Remaining unrun: matrix rows beyond its first request, full Server.Tests, UI.Tests Debug build/native run, a successful complete package dependency gate, and complete all-module replay. No combined acceptance TRX is produced for this incomplete run. The owned daprd was terminated/reaped, all owned jobs finished or were interrupted and reaped, and their temporary workspaces were removed.

### D9 final local verification — 2026-11-15

All requested local gates pass. The final [JSON result](evidence/epic6/6.6-cli-read-surface.json) records tested full baseline revisions, dirty-source hashes, the candidate archive hash, exact native invocations, skips, receipts and limitations. The [TRX aggregate](evidence/epic6/6.6-cli-read-surface.trx) combines eight retained native suites without counting Gateway/enrollment subsets twice. The separate 373-test Manifest MTP run is included in JSON/log totals, not fabricated into native cases. The story remains `in-progress`; independent review and root-owned transitions follow.

| Final lane | Total | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: | ---: |
| Projects Contracts | 207 | 207 | 0 | 0 |
| Projects Server (fresh private in-memory daprd) | 1417 | 1417 | 0 | 0 |
| Projects UI | 277 | 277 | 0 | 0 |
| Projects domain, including NoPayloadLeakageTests | 691 | 691 | 0 | 0 |
| McpCli Abstractions | 20 | 20 | 0 | 0 |
| McpCli Core | 920 | 916 | 0 | 4 |
| McpCli CLI | 625 | 623 | 0 | 2 |
| McpCli MCP | 94 | 94 | 0 | 0 |
| McpCli Manifest (MTP) | 373 | 373 | 0 | 0 |
| **Unique .NET tests** | **4624** | **4618** | **0** | **6** |

The six skips are five existing Windows ACL cases on Linux and the fresh benchmark child-only case. Supplemental strengthened Gateway coverage passes 10/10; refreshed enrollment coverage passes 14/14. Native XML/TRX and raw logs are under `evidence/epic6/6.6/d9/`.

- Each named Projects test project built individually with `--configuration Debug -m:1 -p:NuGetAudit=false --disable-build-servers` and task-owned artifact paths; all final builds report zero warnings/errors. Platform `prepare-build` environment values were used unchanged. UI’s source-reading tests required a private runtime copy at the normal `tests/<project>/bin/Debug/net11.0` depth; no UI source/assertion changed. The separate Partial model is seeded before its read baseline; raw JSON and ETag remain identical after both reads. Existing measured models are never rewritten.
- `pwsh tests/tools/run-package-dependency-gate.ps1 -PackageDirectory /tmp/hexalith-job.54t9wKJM/feed` passes: five exact packages and five isolated consumers. `package-graphs.json` and `contracts-package-closure.json` record the slim Contracts closure. The disposable copy includes the required EventStore/Tenants executable resources and Platform helper sources; earlier missing-copy and typed-consumer failures are retained separately.
- McpCli solution restore and Release `-warnaserror` build pass. Core, Abstractions, CLI/MCP and Manifest gates pass. `check_enrolled.py --local` reports `local-only` with a passing combined production replay of all 26 operations across three packages. Existing Tenants vector-version/unmentioned-operation reports remain informational. This runtime/vector replay is unchanged by the later inventory identity/reference clarification; `projects-enrollment-final/verification.json` supersedes its Projects inventory metadata and passes strict discovery, lint, both C#-derived shapes, inventory, artifact integrity and complete four-operation CLI/MCP replay.
- `bash tools/conformance-vectors/v1/run-sample-validation.sh Release` and `bash tools/conformance-vectors/v1/run-sample-loopback.sh` both exit 0; each runs 186 Python tests. The final packaged fixture passes combined production-plus-sample replay and its negative approval/semantic checks. Its temporary `RestoreSources` includes the unreleased candidate feed; the failed first run without that feed is retained. Production solution restore/build passes again afterward. Actual `operations projects --strict` exits 0 with exactly the four approved names; `dotnet list ... package --include-transitive` exits 0 without a UI/Conversations/Commons.Serialization dependency introduced through Projects Contracts.
- `python3 tools/planning/validate_production_authority.py --validate-index` exits 0 (`[6, 7, 8]`). All owned daprd processes were terminated and reaped, child jobs finished, and `run-with-temp` removed every private build/feed/cache/runtime workspace. Scoped attributes preserve raw/hash-bound evidence bytes.

No candidate publication, main-branch integration, gitlink movement, production routing cutover, external approval or live release acceptance is claimed. DW-82 and the existing Epic 8 release gates remain outside this feature verification.

### Review completion — 2026-11-15

All local patch findings are fixed. Evidence packages/logs are retained through narrow ignore exceptions; vectors use the Contracts enum strings with refreshed bindings. The real Gateway matrix now uses an independent Web oracle, per-request Inspection audit readback, cross-operation safe denials/403, payload cursor continuation/binding, and owner-store bytes/ETags/write observation. Focused matrix 10/10, enrollment 14/14 and four-operation artifact replay pass; parent full Server rerun passes 1,417/1,417 (zero skips). The unchanged source/runtime lanes retain their earlier receipts. Aggregate: 4,618 passed, zero failed, six expected OS/benchmark skips.

B1 owner authenticity (unverified), B4 live fixture assertions and V1 native Projects topology/grants are recorded for the human-approved DW-82 main-integration boundary; no live or release acceptance is granted. All ten implementation tasks and I/O matrix rows are verified. The approved correction forbids commits, publication and gitlink moves, so the workflow's automatic local-commit instruction is not applied. Changes remain uncommitted.

### Review Findings

Second-pass adversarial review (2026-11-15): Projects working tree over `ce71717a` plus the dirty McpCli tree over `23072f04`; layers blind-hunter, edge-case-hunter, verification-gap, acceptance-auditor; 2 decision-needed, 6 patch, 3 defer, 23 rejected.

- [x] [Review][Decision] (resolved 2026-11-15 by Jerome: reject actor-only → patch) McpCli now accepts actor-only Query selectors, beyond D9's "omit both" — Removing the `ProjectionType` requirement in `RoutingResolver` also catalogs a Query that declares `ProjectionActorType` without `ProjectionType`; this was `missing_routing_values` before, D9 authorizes only omitting both selectors and states explicit selector validation is unchanged, no C# fixture pins the case, and `test_validate.py` now explicitly accepts actor-only Gateway bodies. The Gateway accepts actor-only requests (`QueriesController.cs:315-316`), so either restore the rejection (D9-literal) or accept it and pin it with a Catalog fixture and README text. A blank explicit `ProjectionType` also moved from `missing_routing_values` to `invalid_routing_value` (cosmetic). (sources: verification-gap+edge-case-hunter+blind-hunter; medium)
- [x] [Review][Decision] (resolved 2026-11-15 by Jerome: retain a patch file → patch) The uncommitted McpCli enrollment sits on McpCli `main` and is exposed to a routine sync — `references/Hexalith.McpCli` is on `main` at `23072f04` with a dirty tree pinning the unpublished `2.0.0-story66.2` (`Directory.Build.props`, `tools/dependency-policy.json`), and both repositories already carry "build: sync local changes via /pushall" commits. One such sync would commit and push the enrollment to McpCli `main` (E8 forbids that) with a pin CI cannot restore; nothing outside the dirty tree reproduces the tested McpCli source. Choose a protection: a local unpushed branch, a retained patch file under the 6.6 evidence, or leave as is. (source: blind-hunter; medium)
- [x] [Review][Patch] Restore rejection of actor-only Query selectors [references/Hexalith.McpCli/src/Hexalith.McpCli.Core/Catalog/RoutingResolver.cs:51] — Raise `missing_routing_values` when a Query resolves `ProjectionActorType` without `ProjectionType`; add an invalid-fixture Catalog case; make the `test_validate.py` actor-only body a rejection; keep the both-omitted computed case accepted.
- [x] [Review][Patch] Retain the tested McpCli working tree as a patch file [_bmad-output/implementation-artifacts/evidence/epic6/6.6/] — Write the McpCli diff over `23072f04` (tracked and untracked files) with its SHA-256 under the 6.6 evidence; no branch, commit or push.
- [x] [Review][Patch] McpCli live-topology suite fails on the four current-only Projects vectors [references/Hexalith.McpCli/tools/live-topology/test_current_cohort.py:28] — Reproduced: `python3 -m unittest discover -s tools/live-topology -p test_current_cohort.py` fails (set mismatch on `Hexalith.Projects.Contracts/*.vector.json`), and McpCli CI runs `unittest discover -s tools/live-topology -p 'test_*.py'` (`ci.yml:193`). Keep the legacy-to-current equality and transform for packages that have legacy vectors, and admit packages with no legacy counterpart as new enrollments without pinning a module name or count. The 6.6 verification did not run this AGENTS-listed non-live check. (source: edge-case-hunter; high)
- [x] [Review][Patch] McpCli README still describes a two-module production manifest [references/Hexalith.McpCli/README.md:499] — `:499` says the production manifest references only Parties and Tenants and that `hexalith modules --strict` lists `parties` and `tenants` (contradicted by the updated `ConfigCommandTests`); `:380` says one registration exposes "both modules"; the strict-discovery operation table (`:505-522`) has no Projects column; the error-privacy paragraph (`:374`) names only Parties' sentinel behavior. (source: acceptance-auditor; medium)
- [x] [Review][Patch] Inventory v2 frontmatter does not cite the §E8 successor decision [references/Hexalith.McpCli/_bmad-output/implementation-artifacts/projects-migration-inventory-v2.md:1-12] — `source_spec` (spec 4.3) and `approval_ref` (`#a1-session-approval-2026-10-16`, whose A1 text says no Projects operation is included) still name the v1 approval; unlike the Parties successor format it has no `amendment_ref`/`amendment_source_spec` naming the Story 6.6 successor decision and this spec, as E8.4 requires. (sources: blind-hunter+acceptance-auditor; low)
- [x] [Review][Patch] The three 6.6 deferrals sit under the Story 6.1 review heading [_bmad-output/implementation-artifacts/deferred-work.md:1386] — B1, B4 and V1 were appended under "Deferred from: code review of 6-1-… (2026-11-05)"; file them under a Story 6.6 review heading. DW-82's green-main-CI condition already blocks on B4/V1, while B1 (signer capacity) is not otherwise enforced. (sources: blind-hunter+acceptance-auditor; low)
- [x] [Review][Patch] `ProjectConversationIdJsonConverter` adds unnecessary public API to the packable Contracts [src/Hexalith.Projects.Contracts/Queries/ProjectConversationIdJsonConverter.cs:7] — Conversations keeps its equivalent converter `internal`, and a public `JsonConverter<string>` registered in `options.Converters` would prefix every string; making it internal before the unreleased 2.0.0 ships avoids a later breaking removal. Drive `MalformedWireIdentifiersAreRejected` through `ProjectConversationItem` deserialization instead of the converter type. (source: blind-hunter; low)
- [x] [Review][Patch] Decorated query records break the repository's using order and parameter layout [src/Hexalith.Projects.Contracts/Queries/GetProjectQuery.cs:3] — `GetProjectQuery.cs` and `ListProjectsQuery.cs` split `System.*` usings around `Hexalith.McpCli.Abstractions` with a stray blank line (`dotnet_sort_system_directives_first`); `GetProjectQuery` and both `ResolveProjectFrom*Query` records put the next parameter's `[property: …]` attributes at the end of the previous parameter's line; `Directory.Packages.props:7` places `HexalithMcpCliVersion` under the EventStore/Tenants comment. (source: blind-hunter; low)
- [x] [Review][Defer] The official 6.6 AC still says "A missing credential fails before any query is sent" [_bmad-output/planning-artifacts/epics.md:1522] — deferred: D8 amends that AC, but only `epic-6-context.md` was updated; the correction belongs to a planning-artifact edit outside this spec (medium).
- [x] [Review][Defer] The McpCli parity matrix compiles only in the source-fixture lane and never runs in CI [tests/Hexalith.Projects.Server.Tests/Queries/SupportedProjectGatewayHttpTests.McpCli.cs:1] — deferred: pre-existing harness limitation, already tracked (`deferred-work.md:1249`).
- [x] [Review][Defer] `projects-approved.json` asserts `authority: "owning-maintainer"` while its rows and inventory v2 assert no owner sign-off [references/Hexalith.McpCli/tools/inventory-coverage/v1/projects-approved.json:72] — deferred: first-round B1, already tracked (`deferred-work.md:1386`).

#### Rejected

- low (spec edit) — Story status disagrees across spec frontmatter, body, evidence JSON and sprint status: correcting it edits the spec under review; this workflow's status step sets the result (blind-hunter+acceptance-auditor+verification-gap+edge-case-hunter).
- low (spec edit) — "No server or Web source changed" is inaccurate (two trivial Server consumer updates) and Code Map line ranges are stale: correction edits the spec under review (blind-hunter+edge-case-hunter).
- false — Parity matrix "never runs McpCli": the spec task requires attribute-built requests; McpCli's executor-to-body mapping is pinned by the loopback replay, which compares the real request body field by field (blind-hunter).
- false — McpCli executor test checks its own serializer: its options (Web + `WhenWritingNull`) equal `EventStoreGatewayClient.JsonOptions` (`EventStoreGatewayClient.cs:26-27`) (blind-hunter).
- false — Projects error exit codes are untested: D6/D8 leave McpCli unchanged, and its generic 401/error mapping to `gateway_error` exit 2 is pinned (`BoundaryProcessHarnessTests.cs:45-65`) (blind-hunter+acceptance-auditor).
- low — Matrix skips `includeArchived`, `lifecycleState`, quarantine and >32-attachment inputs, and the Web oracle drops `IncludeArchived`: those server paths are covered by the 6.1–6.4 suites; routing/payload parity is input-independent and no current case passes `IncludeArchived` (blind-hunter).
- low — Matrix is a helper inside an existing 6.5 test: splitting the shared daprd fixture is a refactor; failures still surface loudly (blind-hunter).
- low — Decoration tests hard-code allowlists: drift in added record keys is caught; shared constants would be a refactor (blind-hunter).
- low — Evidence retention exceptions are broad (11 nupkgs ≈0.94 MB, 89 logs ≈4 MB): DW-73 owns the retention policy; narrowing needs a per-receipt selection (blind-hunter).
- low — Inventory v2 copies v1 text and uses `replace` for T01–T04: the successor section labels copied text historical and superseded, and E8.4 mandates `replace / include` (blind-hunter).
- low/false — Interface-supplied null selectors and the `MissingProjectionQuery` fixture name: an interface null equals omission and precedence is unchanged; the name is cosmetic (blind-hunter).
- low — Test-helper `Writes` key shape and the triplicated decorator registration: no collision occurs with one wrapper per app (blind-hunter).
- low — Read-store wrapper does not forward the capability interfaces: reads never write through them (only projection handlers do), and the exercised paths pass; forwarding is non-trivial (edge-case-hunter).
- low — Persisted-key set is fixed before the matrix and domain-event streams are not observed: the fixture hosts no command pipeline, read handlers submit no commands, and `IReadModelStore` writes are observed for every key (edge-case-hunter+acceptance-auditor).
- false — Sparse McpCli payloads never reach server admission: `TryPayload` accepts any subset of the allowlist (`ProjectReadQueryExecutor.cs:356-372`), and always-run receiver tests send `{}` (acceptance-auditor+edge-case-hunter).
- false — Missing `HexalithMcpCliRoot` breaks source builds obscurely: the rootless reference fails loudly, as for every other `Hexalith*Root`, and the root is a root-declared submodule (edge-case-hunter).
- low — 404 correlation replacement could miss or over-replace: an absent property throws loudly and a ULID collision elsewhere in the body is implausible (edge-case-hunter).
- low — Decoration test omits `PlatformScope`/`Example` checks: McpCli Operation Shape coverage and lint catch both drifts (edge-case-hunter).
- low — Unavailable row uses a chatbot credential: the index-unavailable path returns `Success(Unavailable)` for every caller (`ProjectReadQueryExecutor.cs:64-67`) (acceptance-auditor).
- false — `[JsonPropertyName]` changes the wire: the server deserializes with Web defaults and rejects non-camelCase keys ordinally, so the only admitted wire was already camelCase (acceptance-auditor).
- low (spec edit) — Descriptive row says "absent → null" while flag-without-permission returns 404: server admission must not change; the wording belongs to the spec (acceptance-auditor).
- false — CLI and Web requests are byte-identical, so parity proves nothing: they are built independently (attributes vs 6.5 constructors), so routing drift makes them differ (verification-gap).
- low — `describe.vector.json` scripts Partial with empty components: canned loopback response only; the live/parity paths assert real components (acceptance-auditor).

### Resumed review corrections — 2026-11-15

All eight second-pass patch items are complete. Actor-only query selectors again fail `missing_routing_values`; selector-free computed queries remain accepted. The converter is internal and its malformed-input tests deserialize the response record. The retained McpCli patch reproduces all 30 files over `23072f04f423eea3f30456fe5af13d2d382624fe`; its SHA-256 is `5403689474f974ce0f3796a918169d3cd1019801ddb7ea266da01b1a588fe210`. README, successor approval provenance, deferral headings and formatting are corrected. Generic non-live oracles now derive actual enrollment order and approved plan-backed vector membership, preserving missing-kind/orphan-plan checks.

[Fresh verification](evidence/epic6/6.6/review-pass2/verification.json) records Projects `5b4bdeacde318a1d948db7ef76843df0c548f084`, McpCli `1f9df7c629c4983587363afd25b8ddca5da167e5` plus the hashed patch, enabled audits, exact commands and cleaned temporary workspaces. Fresh .NET results are 3,651 passed, zero failed and six expected skips. With the unchanged revision-bound UI/domain receipts, the [refreshed aggregate](evidence/epic6/6.6-cli-read-surface.json) contains 4,619 passed, zero failed and six skips (4,625 total). Combined enrollment contains 26 catalog operations and replays all 25 approved vectors; `tenants.refresh-tenant-access` has no direct replay evidence. Both sample gates pass 187 Python tests. The initial 251-case non-live run had 244 passes, three test-oracle failures and four missing-copy-input errors; 18 affected reruns pass, with 233 unchanged passing cases retained. No clean full non-live rerun is claimed. The primary aggregate includes native cases only; the separate 373-test Manifest run is recorded in JSON/logs.

External user commits/submodule changes occurred during this run. No agent changed active Git state or published the candidate. DW-82 retains publication/main integration, signer authenticity and native live qualification. Final independent review follows this parent task/matrix audit.

### Final review outcome — 2026-11-16

All three independent layers are complete. No new verification gap or production-code defect was found. Twelve individual findings were triaged: five carried existing DW-82 deferrals, five low coverage/spec-edit suggestions were rejected with cited passing evidence, one hypothetical mutation-path claim was disproved, and one canonical acceptance sentence was patched. `epics.md` now records D8: Gateway 401, exit 2 and no payload before Projects execution; its matching deferred documentation entry is resolved. No Contracts, Server or McpCli runtime/test source changed after the fresh verification, so the existing native/package/replay receipts remain applicable. Parent final checks revalidated native/candidate/patch hashes and the production-authority index.
