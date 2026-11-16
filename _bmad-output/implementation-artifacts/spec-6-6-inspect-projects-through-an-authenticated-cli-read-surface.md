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

### Resumed build review — 2026-11-16

All three independent layers completed before triage: ten blind-hunter findings, three edge-case findings, and one verification reviewer Other finding (no new verification gaps). Every finding is classified separately below. Existing deferrals preserve their prior decisions; no new runtime patch is required.

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| R5-B1 | medium | defer (carried) | Carried pass-4 publication finding and DW-82 fix-forward decision. The unpublished .3 pin remains; README now describes NU1102 accurately. No publication is authorized by this feature run. |
| R5-B2 | medium | defer (carried) | Carried V1: default native discovery lacks Projects service, committed routes and grants. Existing DW-82 entry retains this requirement; no duplicate deferral. |
| R5-B3 | medium | defer (carried) | Carried B4: the offline list vector does not arrange a discriminating native fixture. DW-82 retains live qualification; no duplicate deferral. |
| R5-B4 | maybe-false | defer (carried) | Carried B1: session artifact approval does not establish external owning-maintainer capacity. Existing DW-82 signer-capacity requirement remains unverified; no release acceptance is asserted. |
| R5-B5 | medium | defer (carried) | Carried pass-4 EventStore graph drift: manifest 5.1.0 versus merged central pin 5.1.1. Existing 2026-11-16 deferred entry and DW-83 retain the correction; no duplicate deferral. |
| R5-B6 | false | reject | The cited aggregate explicitly records revision-bound historical package evidence. New review-pass4/verification.json states no fresh full-suite or package-mode pass; revision-equivalence.json discloses the EventStore pin exception. No current-source package compatibility claim is made. |
| R5-B7 | low | reject | List page/lifecycle/cursor restrictions are described in discovery and enforced by ProjectReadQueryExecutor before reads. A structural CLR schema allowing semantically invalid values can cost an extra safe 400; synchronizing semantic constraints requires additional schema annotations/generation rather than a direct correction, with no demonstrated valid-input failure. |
| R5-B8 | low | reject | Advisory raw count, null entries and owner kinds are checked by ProjectAdvisoryQueryExecutor.cs:85-88 before owner processing and documented by the declarations. Encoding every semantic constraint adds schema behavior; the demonstrated consequence is rejection of invalid input, with no unauthorized access or write. |
| R5-B9 | low | reject | Generic SchemaDeriver.cs:89 enforces object roots only for Commands, unchanged by 6.6; advisory Payload at :315-316 rejects non-object JSON. Tightening generic Query nullability changes shared schema behavior for a malformed-input optimization, so the low complexity-adding suggestion is rejected. |
| R5-B10 | low | reject (spec edit) | The remedy edits this build specification and is rejected by the review workflow. D4 explicitly records the approved historical lane; fresh receipt distinguishes the authorized Debug command from the audit-enabled failed Release attempt. |
| R5-E1 | medium | defer (carried) | Carried V1, same native Projects topology/routes/grants gap as R5-B2. Existing DW-82 route is preserved without another ledger entry. |
| R5-E2 | medium | defer (carried) | Carried B4, same unarranged live empty/Unavailable fixture as R5-B3. Existing DW-82 route is preserved without another ledger entry. |
| R5-E3 | medium | defer (carried) | Carried pass-4 EventStore package-manifest drift, same location and claim as R5-B5. Historical package evidence remains scoped to its recorded revision. |
| R5-V1 | medium | defer (carried) | Other finding, carried V1: default native plan discovers Projects without a usable service/route/grant. Existing DW-82 prerequisite remains open; this is not a new verification gap. |
| R5-P1 | medium | defer | Fresh Release/package Server build stops before compilation with NU1109: Folders.Client 2.0.0 requires McpCli.Abstractions >=3.0.0 against Projects 2.0.1. Existing dependency cohort drift is independent of these test/documentation corrections; record it for package/release maintenance without changing pins under 6.6. |

The parent task/matrix audit is [retained here](evidence/epic6/6.6/review-pass4/parent-task-matrix-audit.json). Fresh supplemental verification is 4/4 admission cases and 20/20 release-tool tests; Debug/source build has zero warnings/errors. Historical full-suite receipts remain unchanged.

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

### Review Findings (code review pass 4, 2026-11-16)

Fourth adversarial review. Diff: Projects `e228f185..de8a710b` without `_bmad-output/` or unrelated submodule bumps, plus McpCli `23072f04` to its working tree. That tree is content-identical to `4b4a808f`, which now sits on McpCli `origin/main` as `ab92da41`. All four layers completed (blind-hunter, edge-case-hunter, verification-gap, acceptance-auditor). 33 layer findings plus 1 found during triage: 2 decision-needed (both resolved), 7 patch, 5 defer, 17 rejected.

- [x] [Review][Decision] (resolved 2026-11-16 by Jerome: fix forward under DW-82 → patch + defer) The enrollment reached McpCli `main` ahead of DW-82, and McpCli main CI is red — McpCli `ab92da41` ("feat: enroll Projects contracts 2.0.0-story66.3…") was pushed to `origin/main` at 2026-11-16 08:48. CI run 43826462552 fails `enrolled-module-conformance`, `ci / build-and-test`, `conformance-vector-loopback` and `topology-vector-discovery` with NU1102: `Hexalith.Projects.Contracts (>= 2.0.0-story66.3)` is not on nuget.org (nearest 1.2.4). Projects local `de8a710b` and `5f8c3d5c` (unpushed, ahead 2) move the McpCli gitlink `23072f04 → 1f9df7c6 → ab92da41`. Pushing them would also break Projects' G-6 restore of `references/Hexalith.McpCli/src/Hexalith.McpCli/Hexalith.McpCli.csproj` (`tools/qualification/g6-current-policy-2026-11-12-dapr-net1191.json`). §E8 does not authorize McpCli `main` merges, pushes or gitlink moves. The pushed README still says "main-branch enrollment remain[s] DW-82", and the carried B1/B4/V1 conditions were set "before main integration". (sources: acceptance-auditor+blind-hunter+verification-gap+edge-case-hunter; high)
- [x] [Review][Decision] (resolved 2026-11-16 by Jerome: record reachable equivalents now → patch) The recorded verification revisions are not reachable from any branch — the fresh verification binds Projects `5b4bdeac` and McpCli `1f9df7c6` plus the hashed patch. After the user-side rebases, no ref contains `5b4bdeac`, `1f9df7c6` or `4b4a808f`. The story paths at `5b4bdeac` equal `4eed8caf`/HEAD except the merged EventStore 5.1.1 pin, and McpCli story paths at `4b4a808f` equal `ab92da41` apart from upstream changes. Inventory v2 `source_revision` and its "successor baseline" cite `ce71717a`, which predates the declarations (they first appear in `059b467c`). Choose between: recording the reachable equivalents without rerunning; re-verifying at reachable commits once Decision 1 settles; or accepting the reflog objects, retained patch and source hashes as sufficient. (sources: acceptance-auditor+blind-hunter; medium)
- [x] [Review][Patch] The McpCli README says main-branch enrollment remains DW-82 [references/Hexalith.McpCli/README.md:527] — from Decision 1. State that the Projects enrollment is on McpCli `main` ahead of publication, and that restore fails with NU1102 until DW-82 publishes `Hexalith.Projects.Contracts`.
- [x] [Review][Patch] Record the reachable equivalents of the orphaned verification revisions [_bmad-output/implementation-artifacts/evidence/epic6/6.6/review-pass4/revision-equivalence.json:1] — from Decision 2. Add a new evidence file with the git tree comparisons mapping Projects `5b4bdeac` to `4eed8caf`/HEAD (story paths identical apart from the merged EventStore 5.1.1 pin) and McpCli `1f9df7c6`/`4b4a808f` to `ab92da41`. Also name `059b467c` as the first reachable commit carrying the declarations. Existing hash-bound evidence and inventory v2 stay untouched.
- [x] [Review][Patch] Add a CI-run attribute-to-executor admission test for the four enrolled queries [tests/Hexalith.Projects.Server.Tests/Queries/ProjectMcpCliAdmissionTests.cs:1] — no CI-run test ties the `[HexalithQuery]` routing (`Domain`, `AggregateId ?? <project id>`, `WireType`, `Example`) to server admission. The only such check is the source-fixture-only Gateway matrix. Build each envelope from the attribute, execute it through the existing `SupportedProjectReadFixture`/advisory executors outside `#if HEXALITH_SOURCE_FIXTURE`, and assert it is not `InvalidEnvelope` or a safe denial. (sources: verification-gap+blind-hunter; medium)
- [x] [Review][Patch] The release-tool fixture still reads the removed Conversations dependency and turns Projects main CI red [tests/tools/test_release_package_tools.py:215] — D7 removed `Hexalith.Conversations.Contracts` from the Contracts graph. `test_external_dependency_may_equal_candidate_version` then raises `KeyError` (Projects CI run 43772496346, job "Validate workflow policy"; reproduced locally). Point the test at a remaining external dependency, `Hexalith.EventStore.Contracts`. (source: triage, found while verifying the acceptance-auditor release-graph finding; medium)
- [x] [Review][Patch] The McpCli README misstates the Projects safe 404 output [references/Hexalith.McpCli/README.md:374] — the README says "Projects returns the safe `safe-denial` result". Operators actually receive the Gateway's canonical `detail: "The requested resource was not found."` from `QueryNotFoundExceptionHandler.cs:35`, which the matrix asserts. Describe the `gateway_error` they see. (source: blind-hunter; low)
- [x] [Review][Patch] The McpCli README still implies Projects Contracts is excluded by the dependency policy [references/Hexalith.McpCli/README.md:497] — "`Hexalith.Projects.Contracts` 1.0.0 remains excluded" is left without saying that the slim 2.0.0 candidate closure now passes the policy. (source: blind-hunter; low)
- [x] [Review][Patch] Restore the Tenants pin rationale lost in the rebase [Directory.Packages.props:9] — the comment "Consume the .NET 11 SDK snapshot limits and tenant access recovery contracts." was replaced by the McpCli pin comment, which leaves the `HexalithTenantsVersion` 8.0.0 pin unexplained. (source: blind-hunter; low)
- [x] [Review][Defer] Publish `Hexalith.Projects.Contracts` so McpCli `main` restores again [references/Hexalith.McpCli/Directory.Build.props:5] — deferred: Decision 1, fix forward. Reason: keep the enrollment on McpCli `main` and publish under DW-82 rather than revert. McpCli main CI stays red (run 43826462552) until publication. Publication needs green Projects release CI (the release-tool patch above, the EventStore drift below, DW-83), and B1/B4/V1 are now due. Do not push the Projects gitlink moves (`de8a710b`, `5f8c3d5c`) before publication, because Projects G-6 restores the McpCli host. Already tracked as DW-82 (`deferred-work.md:760`); note added there.
- [x] [Review][Defer] Projects pins EventStore 5.1.1 while `tools/release-packages.json` still expects 5.1.0 [tools/release-packages.json:27] — deferred: this predates the story. It came in with `f9ec093e`, merged at `e228f185`, which is already red at "Validate package consumer references". It belongs to the same drift class as DW-83. All 6.6 receipts were taken under 5.1.0.
- [x] [Review][Defer] The Projects live plan cannot pass on the current McpCli topology [references/Hexalith.McpCli/tools/live-topology/plans-current/projects.list.json:1] — deferred: carried V1, already tracked (`deferred-work.md:1397`). It falls due now if Decision 1 keeps the enrollment on main.
- [x] [Review][Defer] The list vector's live assertions demand Unavailable and zero items [references/Hexalith.McpCli/tools/conformance-vectors/v1/current/modules/Hexalith.Projects.Contracts/list.vector.json:62] — deferred: carried B4, already tracked (`deferred-work.md:1393`). It falls due now if Decision 1 keeps the enrollment on main.
- [x] [Review][Defer] `projects-approved.json` labels a session approval `owning-maintainer` [references/Hexalith.McpCli/tools/inventory-coverage/v1/projects-approved.json:39] — deferred: carried B1, already tracked (`deferred-work.md:1389`). Its "before main integration" trigger has now been reached.

#### Rejected (pass 4)

- false — "Setup present" under the grant is never exercised: CI-run `SupportedProjectReadTests.cs:165` asserts that Setup is disclosed under permission. The matrix proves CLI/Web route parity, not executor disclosure (acceptance-auditor).
- false — the persisted-delta check admits `SafeDenial` audits: `ProjectReadAudit.cs:24` is the single inspection-audit writer, and `SafeDenial` is its outcome for a denied inspection (acceptance-auditor).
- false — approval references pin a commit without the enrolled Contracts: `approvalReference` targets the §E8 proposal, which exists at `ce71717a` (line 313). The `source_revision` part is in Decision 2 (blind-hunter).
- false — the matrix doesn't send what McpCli sends: `TryPayload` accepts allowlist subsets. Envelope paging, a mismatched `EntityId` and a non-`projects` aggregate id fail loudly as 400 (`ProjectReadQueryExecutor.cs:42-46`), and the README documents payload paging (blind-hunter).
- false — `RoutingResolver` accepts any selector-free Query without opt-in: D9 authorizes generic omission, and the Gateway then selects its committed route. An opt-in marker would re-decide D9, and the `feat:` commit drives semantic-release (blind-hunter).
- false — no minimum McpCli version is recorded and released hosts reject the queries: enrollment happens at compile time through McpCli's own csproj, released hosts never reference Projects Contracts, and plugin loading is outside v1 (blind-hunter+edge-case-hunter).
- false — other document contradictions: conformance README:274 "21 operations" is labeled historical. Inventory v2 lines 21 and 38 mark A1 and G1–G6 as historical. The Projects README lists only legacy compatibility surfaces (blind-hunter).
- low — the `describe` text names the CLI flag `--aggregate-id`: MCP agent flows belong to Story 8.5, and a correction needs a new candidate, re-pin and re-approval (blind-hunter).
- low — `projects-approved.json` has no `defer / exclude` rows for forbidden surfaces: the exact enumeration in `ProjectMcpCliDecorationTests` fails if any other type is decorated, and adding rows cascades through the inventory and approval hashes (blind-hunter).
- low — the OpenAPI `conversationId` pattern rejects `conv:`: the prefixed wire predates D7 (it is the Conversations converter's), the schema last changed in 6.4, the generated client uses a plain `string`, and the fix is a contract regeneration (blind-hunter).
- low — the evidence `.gitignore` exceptions are broad: this was rejected in pass 2 because DW-73 owns retention, and the archives are already pushed, so narrowing the rule would not untrack them (blind-hunter).
- false — `test_live_plans.py` shells out to `dotnet msbuild` and the module orders disagree: the test already required a restored McpCli project, and csproj order is the actual phase order of the reports (blind-hunter).
- low — the fixture registers only part of the production exception handlers: parity compares CLI and Web on the same fixture, and reusing the production registration is a refactor (blind-hunter).
- low — the release probe never loads McpCli Abstractions: the exact nuspec dependency comparison already guards that closure (blind-hunter).
- false — brittle matrix checks: no `ProjectReadModelKeys` helper exists for the Conversations owner key, which the 6.5 fixture builds the same way (line 255). Every 404 call asserts its own status, so the lower bound cannot hide a dropped case, and the duplicate safe read is harmless (blind-hunter).
- false — an empty `HexalithMcpCliRoot` breaks Debug restore with no opt-out: McpCli is a root-declared submodule (`.gitmodules:34`), and a missing root fails loudly like every other `Hexalith*Root` (pass-2 rejection) (edge-case-hunter).
- false — the read-store wrapper misses capability store interfaces: Projects read and audit paths use only `IReadModelStore`, and owner read paths perform no writes (R3-B8 refutation) (edge-case-hunter).

### Pass 4 implementation corrections — 2026-11-16

All seven patch items are complete. The McpCli README now states that enrollment is on `main` ahead of publication, ordinary restore fails with NU1102 until DW-82 publishes the candidate, the slim candidate passes the dependency policy, and safe Projects 404 errors expose the Gateway canonical detail through `gateway_error`. The always-compiled four-query admission theory executes each declared `Domain`, `AggregateId` fallback, `WireType` and JSON `Example` against the existing executors. The release-tool fixture uses the remaining EventStore Contracts dependency, and the Tenants pin rationale is restored.

[Supplemental verification](evidence/epic6/6.6/review-pass4/verification.json) records a clean Debug/source Server build (zero warnings/errors), four passing admission cases and 20 passing release-tool tests. [Reachable revision equivalence](evidence/epic6/6.6/review-pass4/revision-equivalence.json) records exact committed-tree comparisons and reproduces the retained 30-file McpCli patch against reachable `ab92da41`; `059b467c9668a9ab994fc3a52cc704b8116a83e0` is the first reachable declarations commit. Existing hash-bound receipts and inventory v2 remain untouched. The supplementary Release/package Server build stops at NU1109: Folders.Client 2.0.0 requires McpCli.Abstractions 3.0.0 while Projects pins 2.0.1. No dependency update or fresh package/full-suite acceptance is claimed. DW-82, DW-83 and the carried live prerequisites remain open. All owned temporary workspaces were cleaned; no commit, push, publication or gitlink movement was performed.

### Review Findings (code review pass 6, 2026-11-16)

Sixth adversarial review. Diff: Projects `e228f185..0573c52c` without `_bmad-output/` or submodule pointers, plus the 30 McpCli story files from `23072f04` to `baa20ef2` (McpCli `main` = `origin/main`). All four layers completed: blind-hunter 18 findings, edge-case-hunter 8, verification-gap 1 (pre-verified gap), acceptance-auditor 4. Two compound findings were split by claim (BH11, BH13), giving 32 claims: 1 decision-needed, 1 patch, 5 defer (all carried; no duplicate ledger entries, as in passes R3/R5), 18 rejected.

- [x] [Review][Decision] (resolved 2026-11-16 by Jerome: option 2, accept the pushed moves and move the pointer to `baa20ef2` → patch) The McpCli gitlink moves reached Projects `origin/main` before DW-82 publication — `de8a710b` (`23072f04 → 1f9df7c6`) and `5f8c3d5c` (`1f9df7c6 → ab92da41`) are ancestors of `origin/main`; the reflog records `origin/main@{2026-11-16 09:23:33 +0100}: update by push` to `bf9f9896`, the commit that itself records the pass-4 instruction "Do not push the Projects gitlink moves (`de8a710b`, `5f8c3d5c`) before publication". §E8 does not authorize gitlink moves. Consequence: Projects G-6 restores `references/Hexalith.McpCli/src/Hexalith.McpCli/Hexalith.McpCli.csproj` (`tools/qualification/g6-current-policy-2026-11-12-dapr-net1191.json:40`), whose `ab92da41` pins the unpublished `Hexalith.Projects.Contracts` 2.0.0-story66.3, so it will fail NU1102 once main CI gets past its current first failure ("Validate release package tool fixtures", runs 43830831274 and 43838261389; fixed by unpushed `0573c52c`). Related (BH14): the umbrella pointer stays at `ab92da41`, so it does not record the pass-4 README corrections in McpCli `baa20ef2`; moving it would also bring unrelated McpCli `fix: story 5.7`/`5.8` and two `/pushall` commits. Options: (1) accept the pushed pointer at `ab92da41`, note in DW-82 that the "do not push" condition is moot and G-6 restore fails until publication; (2) as (1) and also move the pointer to `baa20ef2`; (3) revert the Projects pointer on `main` to `23072f04` until DW-82 publishes. (sources: acceptance-auditor+blind-hunter; high)
- [x] [Review][Patch] Inventory v2 still says McpCli `main` enrollment remains DW-82 [references/Hexalith.McpCli/_bmad-output/implementation-artifacts/projects-migration-inventory-v2.md:36] — "Package publication and landing McpCli enrollment on main remain DW-82; no production enrollment … is claimed" contradicts README `:499`/`:529`, `Hexalith.McpCli.csproj:24` and `tools/dependency-policy.json` on `main`. Restate it the way the pass-4 README does: enrollment is on `main` ahead of publication; publication, live qualification and the carried B1/B4/V1 conditions remain DW-82. The Markdown is not covered by `approval.json` (`inventorySha256` binds `projects-approved.json`); only historical evidence records its earlier hash. (sources: acceptance-auditor+blind-hunter+edge-case-hunter; low) Applied 2026-11-16 as McpCli `1ffbdd2d`.
- [x] [Review][Patch] Record the McpCli pointer at `baa20ef2` [references/Hexalith.McpCli] — from the Decision. Commit only the gitlink `ab92da41 → baa20ef2` on local `main`, so the umbrella records the pass-4 README corrections. This also brings McpCli `908d9566` (`fix: story 5.8`), `0dbf6dba` and `8fb5091c` (`/pushall`) and `fbe4b6bd` (`fix: story 5.7`). No push. `baa20ef2` still pins 2.0.0-story66.3, so G-6 restore still fails until publication. (source: decision; high) Applied as `ed7eb610`; the pass-6 record commit then moves the pointer to McpCli `1ffbdd2d`, which adds the inventory v2 correction.
- [x] [Review][Patch] Note in DW-82 that the Projects gitlink moves were pushed [_bmad-output/implementation-artifacts/deferred-work.md:767] — from the Decision. The "keep unpushed until publication" condition is moot: `de8a710b` and `5f8c3d5c` reached `origin/main` with `bf9f9896` at 09:23:33, and Jerome accepted them. Record that Projects G-6 "Restore effective G-6 package graphs" fails NU1102 for `Hexalith.Projects.Contracts` 2.0.0-story66.3 until publication, and that the pointer moves to `baa20ef2`. (source: decision; high)
- [x] [Review][Defer] McpCli `main` cannot restore the unpublished candidate, so its CI never runs the D9 routing, Projects coverage and wire-omission replay tests [references/Hexalith.McpCli/Directory.Build.props:5] — deferred (carried): pass-4 Decision 1 fix-forward under DW-82 (`deferred-work.md:760`). A routing regression would look the same as the known NU1102 failure until publication. Re-check the `domain-ci` and `enrolled-module-conformance` jobs after DW-82 publishes. The candidate SHA-256 `349f938d…` is recorded in `evidence/epic6/6.6-cli-read-surface.json`, so DW-82 can compare published bytes against it. (sources: verification-gap+blind-hunter; medium)
- [x] [Review][Defer] Package-mode NU1109 blocks Projects Server/Server.Tests restore in CI, so `ProjectMcpCliAdmissionTests` does not yet run in CI [Directory.Packages.props:7] — deferred (carried): already tracked as the 2026-11-16 Folders.Client/McpCli.Abstractions cohort entry (`deferred-work.md:1413`). One side of the conflict is the 2.0.1 pin that Task 3 mandates for McpCli decoration compatibility. The other is Folders.Client 2.0.0 (≥3.0.0), which arrived through the Builds pointer move in `4eed8caf` after D9 verification (taken at Builds `73e60af8`, Folders 1.4.0). That is upstream movement, so it does not reopen 6.6. A fix needs either a new candidate and McpCli re-pin, or a Folders/Builds cohort choice. (sources: acceptance-auditor+blind-hunter; medium)
- [x] [Review][Defer] The Projects live plan asserts an Unavailable, empty list [references/Hexalith.McpCli/tools/live-topology/plans-current/projects.list.json:1] — deferred (carried B4, `deferred-work.md:1394`). (sources: blind-hunter+edge-case-hunter; medium)
- [x] [Review][Defer] `projects-approved.json` labels a session approval `owning-maintainer` [references/Hexalith.McpCli/tools/inventory-coverage/v1/projects-approved.json:39] — deferred (carried B1, `deferred-work.md:1390`); signer capacity remains unverified. (sources: blind-hunter+edge-case-hunter; maybe-false)
- [x] [Review][Defer] The real-Gateway parity matrix compiles only in the source-fixture lane [tests/Hexalith.Projects.Server.Tests/Queries/SupportedProjectGatewayHttpTests.McpCli.cs:1] — deferred (carried, `deferred-work.md:1249`). The sub-claim "never runs the real CLI" stays refuted (pass 2): loopback replay compares McpCli's actual request body. (source: blind-hunter; medium)

#### Rejected (pass 6)

- low — BH5 `projects-approved.json` has no defer/exclude rows: pass-4 rejection stands. The exact enumeration in `ProjectMcpCliDecorationTests` fails if any other type is decorated (blind-hunter).
- false — BH6 inventory v2 keeps stale A1/G1/G4 text: v2 `:21` ties A1 and source observations to the v1 pin, and `:38` labels the R/L/V rows and G1–G6 historical. Linking T01–T04 to legacy rows is a low, additive suggestion (blind-hunter).
- false — BH7 `RoutingResolver` silently accepts selector-free queries: D9 authorizes generic omission; pass-4 rejection (blind-hunter).
- false — BH8 the executor test uses its own `WhenWritingNull` options: they equal `EventStoreGatewayClient.JsonOptions` (pass 2), and vector loopback compares the real request body (blind-hunter).
- low — BH10 the matrix skips limits and flags, and the Web oracle drops `IncludeArchived`: rejected in pass 2 and R3-B4. No current case passes `IncludeArchived`, and the 6.1–6.4 suites cover the limits (blind-hunter).
- false/low — BH11 loose assertions:
  - The denial lower bound is a pass-4 rejection; every 404 call asserts its own status.
  - The admission test checks admission only (content is covered by the 6.1–6.4 suites).
  - "List-specific fields leak" is false: Domain, AggregateId, QueryType and Payload are all overridden, and the remaining envelope fields are credential identity (`SupportedProjectReadFixture.cs:81-86`) (blind-hunter).
- low — BH12 decoration tests hard-code the allowlists: pass-2 rejection (blind-hunter).
- false/low — BH13a and ECH2 no `HexalithMcpCliFromSource` opt-out or empty-root guard:
  - A missing root fails loudly, as for every `Hexalith*Root` (passes 2 and 4).
  - Task 3 requires a source reference whenever `UseHexalithProjectReferences` is true.
  - Projects' only `FromSource` switch is the CI-specific Conversations one (`ci.yml:274`) (blind-hunter+edge-case-hunter).
- false — BH15 `test_current_cohort.py` should pin `{Hexalith.Projects.Contracts}`: the pass-2 patch required admitting new packages without pinning a module name or count. The `test_live_plans.py` msbuild claim is a pass-4 rejection (blind-hunter).
- false — BH16 and ECH6 `.gitignore` re-includes:
  - `git status --ignored` reports no ignored 6.6 evidence.
  - No file other than a `.nupkg` is tracked under an `artifacts/` directory.
  - Retention breadth is DW-73's (passes 2 and 4) (blind-hunter+edge-case-hunter).
- low — BH17 McpCli README counts (21 vectors and operations, parties+tenants v1): README `:525` frames them as Story 4.11 history, `v1-scope.json` does require only `parties` and `tenants`, and generic `gateway_error` mapping is pinned (pass 2) (blind-hunter).
- false — BH18 `ProjectConversationTranslator` can throw a `NullReferenceException`: the `ConversationSummaryV1` constructor throws on a null `ConversationId` (`ConversationSummaryV1.cs:47`), so `.Value` never sees null (blind-hunter).
- false — AA4 the matrix posts explicit `null` selectors instead of omitting them: the Gateway binds `[FromBody] SubmitQueryRequest` (`QueriesController.cs:222-223`) with optional nullable selector parameters, so absent and null produce the same request and no untested path exists (acceptance-auditor).
- false — ECH1 interface queries that return a null `ProjectionType` are cataloged: an interface null equals omission and precedence is unchanged (pass-2 rejection) (edge-case-hunter).
- false — ECH3 released McpCli 2.0.1/3.0.0 hosts drop the four queries: enrollment is compile-time through McpCli's own csproj, and released hosts never reference Projects Contracts (pass-4 rejection) (edge-case-hunter).
- false — ECH5 the persisted-key set is fixed before the matrix: the observers capture `SaveAsync`/`TrySaveAsync` for every key, including new ones (R3-B8, pass 2) (edge-case-hunter).
