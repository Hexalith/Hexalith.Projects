---
title: 'Cut over supported reads while preserving compatibility and rollback'
type: 'feature'
created: '2026-11-16'
status: 'done'
route: 'dispatch'
baseline_commit: 'c6541a305a420078b16dceaba47f36468022850b'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/story-6-7-approved-read-cutover-amendments-2026-11-16.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Supported reads coexist with legacy REST reads; exhaustive equivalence, contract alignment and reversible selection are missing.

**Approach:** Prove seven supported reads through the existing Gateway, align generated contracts, and switch primary Web routing reversibly.

## Decisions

Jerome selected the initial recommendations on 2026-11-16: "apply recommended". The user approved all four follow-up recommendations with "approve" on 2026-11-16; the approved amendment record is loaded through frontmatter context.

- **D1 Transport:** Existing Gateway with typed supported consumption; preserve v1 REST/client signatures. One explicit configuration choice switches primary Web entry points; restart/redeploy applies it. Record migrated consumers; external Chatbot adoption requires separate evidence. Limit EventStore export work to these reads; a substantial generator redesign becomes a prerequisite, never a Projects replacement generator.
- **D2 Comparison:** Version `story67-read-normalization-2026-11-16-v3` permits the conditional mappings below and the closed outcome pairs in the approved amendment record only. Approval defines comparison rules, not a passing gate.
- **D3 Hosting:** DW-63 stays open, owned by Projects/EventStore, as a Story 7.1 prerequisite alongside writer integration. The approved planning correction removes its 6.7 gate; AD-25 retirement remains gated.
- **D4 Binding:** Require a named Conversation with current authorized same-Tenant native Project membership for context/setup/explain. Harden the existing owner transport for original-actor/delegated use. Preserve existing signatures/constructors/deconstruction and add input compatibly. Unbound callers get no usable context; session fallback is deferred.
- **D5 Legacy inspection:** Legacy list/open retain real required names only after explicit inspection opt-in, existing descriptive-inspection permission and successful durable audit. Preserve v1 signatures/old overloads; callers without that authority fail safely. Compare authorized inspection modes; independently prove the supported Safe Metadata default.
- **D6 Pending Folder:** The user explicitly approved the narrow materialization-in-progress pair: independently proven historical Pending/folderless required Folder, retained 503/no usable context versus supported 200/Unavailable/null Setup/PollTask, with native binding, quarantine and unchanged history proved. This is no generic missing-Folder or unavailable mapping.

## Boundaries & Constraints

**Always:** Tenant/original-actor authority, Safe Metadata, descriptive-inspection audit, exact positions, scoped cursors and AD-32. Preserve historical identifiers/events, including `ProjectFolderCreationPending`, and name-only creation. Reuse platform transport/generation. Retain legacy until Epic 8 acceptance.

**Never:** Rewrite history, add writers, weaken security, invent owner contracts, fall back after denial, hand-edit generated output, or retire runtime before AD-25. No deployment, publication, Git mutations or `sprint_plan.py`; readiness/Epic 7/8 pins stay unchanged.

## I/O & Edge-Case Matrix

| Scenario | Expected behavior |
| --- | --- |
| Complete approved comparison | Supported route selectable; rollback remains executable |
| Missing case, mismatch or regression | Block activation; explicit rollback only to a security-verified legacy path, otherwise unavailable |
| Foreign/absent/denied target | Same safe denial; no protected detail |
| Historical/pending/folderless record | Readable history; quarantine retained; no repair by read |
| Reads and rollback | No domain writes; inspection audits accounted separately |

## Comparison Contract

D2 mappings: JSON property order; lifecycle spelling to the same enum; folder fields to the identical authorized reference; legacy Sequence to its exact source GlobalPosition and supported global position, with supported ProjectVersion independently checked against original SequenceNumber; ObservedAt to AsOf only for the same observation; null Setup to empty only with independently verified current-empty evidence. Map physical keys to AggregateIdentity while retaining raw keys/collision checks. Decode cursors independently and compare boundaries/scopes plus continuation, expiry and rejection behavior. No timestamp tolerance, array reordering, discarded differences or inferred authority. Assert supported-only snapshots/components independently. Repair Conversation binding, stale-Tenant admission and descriptive/advisory disclosure. The approved finite stale-Tenant and unsupported/degraded advisory 503 versus explicit supported Unavailable/incomplete outcome pairs require independent authority/nondisclosure and full AD-32/coverage assertions; retain both raw responses. The approved D4 no-usable-context requirement also permits the narrowly verified native-binding-unavailable pair recorded in context (legacy 503/no context versus supported 200/Unavailable/null Setup). The separately approved D6 required-Folder-materialization pair requires original Pending evidence, null Setup, PollTask and unchanged quarantine/history. No generic error/empty equivalence is allowed.

</frozen-after-approval>

## Code Map

`Server`, `Client`, `Testing`, `UI` mean `src/Hexalith.Projects.<name>`.

- `Server/ProjectsDomainServiceEndpoints.cs`: legacy routes/creation DTOs. `Queries/ProjectQueryRequestPreamble.cs`: existing ULID correlation IDs.
- `Server/Queries/{ProjectReadQueryExecutor,ProjectContextQueryExecutor,ProjectAdvisoryQueryExecutor}.cs`: supported reads; service registration retains both paths.
- `Testing/Reads/{ProjectReadShadowComparator,ProjectContextShadowComparator}.cs`: strict comparators; existing local revision remains historical.
- `Server/ProjectsRuntimeCatalogDeclarations.cs`: typed runtime schema/digests. EventStore `RestApi.Generators` lacks typed result export.
- `Client/Hexalith.Projects.Client.csproj`: legacy NSwag/fingerprints. `UI/Components/Pages/{LegacyHome,LegacyProjectDiagnostics,Home,ProjectDiagnostics}.razor`, `Components/Layout/MainLayout.razor`: Web selection.
- `tests/Hexalith.Projects.Server.Tests/Queries/{SupportedProjectShadowTests,SupportedProjectReadHttpTests.Shadow}.cs`: complementary local/HTTP coverage, not exhaustive equivalence.

## Tasks & Acceptance

**Execution:**
- [x] `Contracts/Queries/`, `Server/Queries/`, `Server/Conversations/` and Conversations owner Contracts/Server — implement D4 using the existing native membership read with original-actor enforcement; preserve direct authorized owner calls and existing public signatures; update actual context consumers and native-only/deferred-session documentation.
- [x] `Server/ProjectsDomainServiceEndpoints.cs`, existing read audit/policy, Contracts OpenAPI, generated Client and legacy UI adapters — implement D5 explicit authorized audited inspection with preserved old signatures; prove refusal and audit-failure nondisclosure.
- [x] `docs/read-cutover.md` — inventory events, keys, projections, cursors, routes, identities, consumers and partial/in-flight records across the seven reads; distinguish legacy-only refresh/conversations/diagnostics.
- [x] `Server/ProjectsRuntimeCatalogDeclarations.cs`, `src/Hexalith.Projects.Contracts/` and platform `RestApi.Generators/` — add minimal typed export/host reconciliation; consume it in `Client/`. Preserve creation/classification and v1 signatures; regenerate fingerprints.
- [x] `Testing/Reads/`, `tests/Hexalith.Projects.Server.Tests/Queries/ReadCutoverTests.cs` — compare authenticated paths on identical history; independently capture keys/positions/cursor scope. Test every failure; repair safety defects or block affected slices.
- [x] `Server/Queries/`, Contracts OpenAPI and generated Client — resolve DW-57/59/60/66 with tests and recorded decisions; preserve completed repairs. DW-63 is assigned to 7.1.
- [x] `Client/ProjectsClientServiceCollectionExtensions.cs`, `UI/Components/Pages/` and `UI/Components/Layout/MainLayout.razor` — implement D1 selection and explicit security-verified rollback; reject incomplete evidence; errors retain supported denial/unavailability.
- [x] `tests/Hexalith.Projects.Tests/SchemaEvolution/`, Server/Client/Contracts/UI tests — prove history, ULIDs, opaque historical IDs, contract parity, rollback and persisted no-domain-write behavior.
- [x] `evidence/epic6/6.7/review-01/read-cutover-final.json`, `rollback-final-proof.json` and `source-manifest.json` — retain revisions, hashes, executed matrix, approved v3 rules, XML, counts and explicit rollback receipts. Earlier blocked `6.7-read-cutover.json`/`6.7-rollback.log` remain immutable. Review and post-acceptance tracking belong to the enclosing build workflow.

**Acceptance Criteria:**
- Given all seven delivered reads, when the gate runs, then every required case passes canonical output, key, exact watermark, cursor/scope and isolation checks; no missing/skipped cell passes.
- Given a passing gate, when regression triggers explicit rollback, then a security-verified legacy path preserves domain bytes/history; otherwise reads become unavailable.
- Given generated contracts, when compared with the live host and legacy consumers, then schemas, identity handling and compatibility fingerprints agree.

### Review Findings

Code review 2026-11-16, pass 1 of 2: production source, owner submodule commits (Conversations `f1a2b3da`, EventStore `0be0c922`), docs and generated client from sync commit `e8fe8141`. The test files (pass 2) are reviewed separately. Layers: Blind Hunter, Edge Case Hunter, Verification Gap, Acceptance Auditor. All four completed.

- [x] [Review][Decision] In-repo CLI and MCP read consumers now fail against the retained server (high) — `ProjectsCliApplication` (list `:96`, describe `:121`, warning scan `:222`, re-evaluate `:532`), `ProjectsMcpResourceReader` (`:98`, `:248`, `:270`, `:398`, `:420`) and `ProjectsMcpCommandService.ReevaluateAsync` (`:211`) still call the four-argument overloads. Those overloads now send `inspectDescriptiveMetadata=false`, so list/describe always get a safe 404. Refresh has no `conversationId`, so it always gets a 503. CLI/MCP tests mock `IClient` and still pass. The docs' consumer table does not list these consumers. Options: (a) opt them into descriptive inspection, which requires the inspection permission and writes a durable audit per call, and add a Conversation input for refresh; (b) accept and document the break, since these are obsolete proprietary module surfaces superseded by McpCli; (c) defer with a DW entry. — resolved 2026-11-16: Jerome chose "Document + defer" → doc patch plus deferral below.
- [x] [Review][Decision] Supported `asOf` changed from evidence time to the request clock (medium) — `ProjectContextQueryExecutor.cs:66,224` replaces `detail.UpdatedAt` / `LastEventTimestamp` with `TimeProvider.GetUtcNow()`. The Contracts XML docs (`ConversationStartAdmissionSnapshot.AsOf`, `ProjectContextAdmissionAssembler`) change from "persisted read-model observation instant / authoritative evidence cutoff" to "current computation instant". Conversation-start Setup `ObservedAt` copies that `asOf` with `Fresh` (`GetConversationStartSetupQueryHandler.cs:63`). Component observations keep their source times. This contract-semantics change appears only in the implementation notes and is not among the approved D1–D6 decisions. Keep it as is (and record the decision), or restore evidence-time `asOf`? — resolved 2026-11-16: Jerome chose "Keep + record" → record-the-decision patch below.
- [x] [Review][Decision] Legacy list now performs 2N+1 unbounded authorization calls (medium) — `ProjectsDomainServiceEndpoints.cs:1793-1821` calls `AuthorizeReadAsync` (a Tenant-access authorization plus a detail read) for every row before the audit and again after it, then calls `AuthorizeListAsync` again. The legacy list has no paging, so latency grows linearly with Tenant size in rollback mode. This came from the E2 repair. Options: reuse one fresh list-level Tenant authority with per-row detail checks only; cap rows and fail Unavailable above the cap; or accept it as a rollback-only cost. — resolved 2026-11-16: Jerome chose "Defer with DW" → deferral below.
- [x] [Review][Decision] Startup route selection drops to Unavailable without a reason or log (medium) — `ProjectsReadRouteSelection.cs:22-43` maps every failure to `Unavailable` with no reason: mode typo or wrong case, missing file, hash mismatch, duplicate property, missing cell, bad receipt. `EvidencePath` resolves against the process working directory. An operator performing an incident rollback cannot see why it was refused. Fixing this means adding a public refusal reason and a startup log, or documenting the diagnosis procedure. — resolved 2026-11-16: Jerome chose "Patch: reason + log" → patch below.
- [x] [Review][Patch] Record the in-repo CLI/MCP v1 consumer break in the cutover consumer table (decision 1) [docs/read-cutover.md:90] — list `Hexalith.Projects.Cli`/`Hexalith.Projects.Mcp` as obsolete surfaces that now receive safe 404 (list/describe) and 503 (refresh/re-evaluate), with the deferral reference.
- [x] [Review][Patch] Record the computation-time `asOf` decision (decision 2: keep) [docs/read-cutover.md:126] — state that snapshot `asOf` and Conversation-start Setup `ObservedAt` are the computation instant, component last-verified times carry source evidence age, and add a Spec Change Log entry.
- [x] [Review][Patch] Expose the route-selection refusal reason and log it once at startup (decision 4) [src/Hexalith.Projects.Client/ProjectsReadRouteSelection.cs:22] — additive reason on the selection, startup log in the UI host, and document EvidencePath resolution.
- [x] [Review][Patch] Folders v1 read DTOs reject additive owner fields (medium) [src/Hexalith.Projects.Server/Folders/FoldersV1ProjectReadDirectory.cs:24] — `UnmappedMemberHandling.Disallow` turns any additive owner field into Unavailable for every Folder read. That affects legacy open, attachment resolution and supported Folder visibility through the shared directory. It violates "serialization-tolerant"; the `JsonRequired` members already pin the v1 shape.
- [x] [Review][Patch] Approved unavailable-pair check does not assert "no included references" (medium) [src/Hexalith.Projects.Testing/Reads/ReadCutoverComparator.cs:64] — the context branch checks null Setup, Unavailable, at least 4 components and non-empty recovery. It never checks that references are absent or not included, which the D4 clarification explicitly requires.
- [x] [Review][Patch] Context authority check diverges from the shared read admission (low) [src/Hexalith.Projects.Server/Queries/ProjectContextQueryExecutor.cs:74] — when `Projects:Authentication:Audience` is unset it falls back to a hard-coded `"hexalith-eventstore"`, whereas `ProjectReadQueryAdmission` fails closed. `ProjectContextQueryAuthority` is now dead code.
- [x] [Review][Patch] Legacy quarantine inspection ignores the plain `role` claim (low) [src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs:2068] — `MayInspectLegacyQuarantine` accepts `IsInRole` or `projects:role` only, while the supported `HasRole` also accepts `role`. The two paths therefore decide quarantine inspection differently for the same administrator.
- [x] [Review][Patch] Final membership recheck runs before the denial check (low) [src/Hexalith.Projects.Server/Queries/GetProjectContextEndpoint.cs:105] — if the assembled outcome is Unauthorized/ProjectUnavailable and membership also fails, the response is 503 instead of the safe 404. The same ordering is in `GetProjectContextExplanationEndpoint.cs:105` and `RefreshProjectContextEndpoint.cs:190`.
- [x] [Review][Patch] Cursor allowance matches the Conversations route by exact path (low) [src/Hexalith.Projects.Server/Queries/ProjectQueryRequestPreamble.cs:54] — an ordinal `EndsWith("/conversations")` rejects a valid paged request that uses a trailing slash or different casing, both of which ASP.NET routing accepts.
- [x] [Review][Patch] Client normalizer leaves duplicated XML documentation (low) [src/Hexalith.Projects.Client/Generation/Program.cs:72] — removing the non-token overloads keeps their `///` blocks. These merge into the next member, giving duplicate `<summary>`, `<remarks>` and `<param>` elements on all six metadata operations in `Generated/HexalithProjectsClient.g.cs`.
- [x] [Review][Patch] Cutover docs contradict the final implementation (low) [docs/supported-project-reads.md:11]
  - `docs/supported-project-reads.md:11` links the pre-review `approved-v3` packet instead of `review-01`.
  - `docs/supported-project-reads.md:283` says stale evidence returns 503. Legacy and supported list/open both return 404.
  - The `docs/read-cutover.md:210` heading still says "proposed; not implemented".
  - The commit converted `docs/supported-project-reads.md` from CRLF to LF, so the whole 260-line file shows as changed.
- [x] [Review][Patch] Primary Web routing for `/`, `/projects`, `/projects/warnings` and layout navigation is untested (medium) [src/Hexalith.Projects.UI/Components/Pages/PrimaryHome.razor:1] — no test renders `PrimaryHome`, or `MainLayout` on a primary path in each mode. Reverting `MainLayout.razor:5-8` reintroduces legacy reads in Unavailable mode, and no test fails.
- [x] [Review][Patch] Safe Metadata sanitization of context labels is unasserted (medium) [src/Hexalith.Projects/Context/ProjectContextSafeMetadata.cs:9] — removing `Sanitize(...)` at the executor or the legacy context/explain/refresh endpoints fails no test.
- [x] [Review][Patch] Tightened context scope/audience rule is not pinned (medium) [src/Hexalith.Projects.Server/Queries/ProjectContextQueryExecutor.cs:72] — there are no null-scope, null-audience or configured-audience cases, so restoring `MatchesPresented` passes every test.
- [x] [Review][Patch] Route-selection negative cases are untested (medium) [src/Hexalith.Projects.Client/ProjectsReadRouteSelection.cs:32] — wrong evidence or receipt hash, `../` receipt path, duplicate property, absent `nativeTestName`, and `contractParity`/`identityCompatibility` false are all uncovered.
- [x] [Review][Patch] Legacy rollback Conversation forwarding is checked only with `Arg.Any` (low) [src/Hexalith.Projects.UI/Diagnostics/LegacyProjectDetailSource.cs:113] — the same applies to `ProjectMaintenanceActionPanel.razor:389`.
- [x] [Review][Patch] Advisory-only owner read contract is unverified across the Conversations boundary (medium) [src/Hexalith.Projects.Server/Conversations/ConversationsProjectConversationResolutionDirectory.cs:50] — add a real-owner advisory case to `NativeConversationMembershipTransportTests`.
- [x] [Review][Patch] Resolve-from-conversation Unauthorized/Excluded metadata → safe 404 is unpinned (low) [src/Hexalith.Projects.Server/Queries/ResolveProjectFromConversationEndpoint.cs:119]
- [x] [Review][Patch] Exact DW-57 Conversation-start headers are only loosely asserted (low) [src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs:111] — there is no exact watermark assertion and no `Stale` case outside the Dapr-gated matrix.
- [x] [Review][Patch] Explain handler result is not pinned to a null ProjectionType (low) [src/Hexalith.Projects.Server/Queries/ExplainContextSelectionQueryHandler.cs:50]
- [x] [Review][Patch] Folder v1 adapter accept/deny/archive/status mappings are untested (medium) [src/Hexalith.Projects.Server/Folders/FoldersV1ProjectReadDirectory.cs:60]
- [x] [Review][Patch] Paging and cursor refusal is verified only in the Dapr-gated matrix (medium) [src/Hexalith.Projects.Server/Queries/GetProjectContextQueryHandler.cs:48] — the same applies to `ProjectQueryRequestPreamble.cs:54`.
- [x] [Review][Defer] In-repo CLI and MCP read consumers fail against the retained server (high) [src/Hexalith.Projects.Cli/ProjectsCliApplication.cs:96] — deferred: Obsolete per-module CLI/MCP surfaces superseded by McpCli; limited to safety/continuity fixes and retired under the McpCli migration plan.
- [x] [Review][Defer] Legacy list 2N+1 unbounded authorization calls (medium) [src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs:1793] — deferred: Rollback-only legacy path; the per-row recheck preserves the E2 stale-authority guarantee; optimize before any production rollback drill.
- [x] [Review][Defer] Startup gate trusts self-declared evidence identity and cell flags (medium) [src/Hexalith.Projects.Client/ProjectReadCutoverGate.cs:30] — deferred: extends the existing B1 intrinsic-binding obligation. New facts:
  - The final packet labels `sourceRevision` `981ceef1`, which contains none of the cutover source; the 144 manifest hashes match committed `e8fe8141`.
  - All 77 cells and all 7 legacy slices bind to one matrix test.
  - `contractParity`/`identityCompatibility` are flags, not checked against `contractParityProof`.
- [x] [Review][Defer] Package-mode (CI/Release) builds depend on unreleased owner APIs (high) [src/Hexalith.Projects.Contracts/Queries/GetProjectQuery.cs:15] — deferred: publication is out of scope for this story.
  - `QueryResultContractAttribute` and the typed emitter exist only at untagged EventStore `0be0c922` (`v5.2.0-6`); no cached release, 5.1.1 or 5.2.0, contains them, and Projects pins 5.1.1.
  - The new Conversations owner APIs (`NativeMembershipOnly`, `AdvisoryMetadataOnly`, `ReadPurpose`) exist only at `f1a2b3da`.
  - The Contracts, Client and Server package builds cannot compile until both owners publish. No DW entry tracked this.
- [x] [Review][Defer] Folders v1 read wire contract is hand-mirrored in Projects (medium) [src/Hexalith.Projects.Server/Folders/FoldersV1ProjectReadDirectory.cs:19] — deferred: the documented owner mismatch (the generated Folders client targets the non-routed v2 candidate). The fix belongs to the Folders owner.
- [x] [Review][Defer] Comparator leaves D2 mappings and cursor/address proof to caller-supplied flags (medium, unverified) [src/Hexalith.Projects.Testing/Reads/ReadCutoverComparator.cs:31] — deferred, maybe-false: settle in pass 2 by confirming that the CutoverMatrix callers independently assert cursor scope/boundary and physical addresses before setting `TransportCursorAssertions`/`completeSupportedAssertions`.
- [x] [Review][Defer] Conversations ambiguous tenant/subject claim refusal is untested (medium) [references/Hexalith.Conversations/src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs:454] — deferred: owner-repository test.
- [x] [Review][Defer] EventStore correlation-header restore after error is untested (low) [references/Hexalith.EventStore/src/Hexalith.EventStore/Middleware/CorrelationIdMiddleware.cs:32] — deferred: owner-repository test.

#### Rejected

- `false` — Old v1 list/open overloads always fail: this is the approved amendment 4 behavior change ("calls without the required inspection authority fail safely"). Signatures are preserved.
- `false` — Permanent conditions return retryable 503: these are the approved closed pairs (D4 clarification, D6, amendment 2).
- Rejected (fix renegotiates the spec) — LegacyRollback is unusable for users without descriptive-inspection permission, and each page view writes a durable audit. This is a direct consequence of approved amendment 4.
- `false` — Conversation-start 401/403 removed and Fresh/Stale header: these implement the recorded DW-59 ("Declare safe 404 only") and DW-57 decisions.
- `false` — Paging checked after admission: validation deliberately follows authorization so invalid envelopes cannot distinguish denied callers.
- `false` — Conversations `trustedDirectCall` with a null accessor: owner DI registers `IHttpContextAccessor`, so the two-argument constructor is used. The baseline handler had no caller verification, and `GetAsync` still authorizes `UserId`.
- `false` — Stale advisory evidence reuses native freshness: `ConversationAdvisoryMetadataResult` carries only `Incomplete` (enforced), and the epic permits advisory lag.
- `false` — Legacy open/list return 404 instead of 503 for stale authority: the matrix shows supported list/open also return 404. Only the doc sentence is wrong, and it is patched above.
- `false` — Context preamble admits Included with a null FolderId: the inclusion policy records an invalid identifier and excludes the Folder (`ProjectContextInclusionPolicy.cs:307`), giving Unavailable.
- `low` — Legacy open reports Included without an owner check for Included with a null FolderId: the projection sets Included only via `ProjectFolderSet` with an ID, so the state is not reachable.
- `false` — Allowed outcome with empty permissions maps to Unavailable: contradictory owner evidence correctly fails closed.
- `false` — Ambiguous `tid`/`tenant_id` or `sub` claims return hidden: failing closed on ambiguous identity is the intended hardening.
- `false` — Audit reuse with a caller-supplied correlation ID: this is the documented `ProjectReadAudit` design (unchanged retries reuse one record).
- `false` — Comparator throws on an Undefined JsonElement: a test helper that fails loudly on invalid input is correct.
- `low` — Owner detail read refuses EntityId ≠ AggregateId: this is the same rule the existing owner advisory and resolution handlers use, and no caller sends a differing EntityId.
- `false` — `/inspect/` pages stay reachable in Unavailable/LegacyRollback: D1 scopes the switch to primary entry points, and the docs' consumer table records `/inspect/` separately.
- `false` — Normalizer reshapes code outside the fingerprints: the client regenerates from spine, nswag and `Program.cs` (now a target input) on every build, and the fingerprint scope is unchanged from baseline.
- `low` — Owner adapters swallow exceptions without logging: they fail closed, and adding loggers changes constructors.
- `low` — Folders v1 maps terminal states to Archived and accepts `["write"]` without `read`: the owner's permission hierarchy and state taxonomy are design choices this review could not establish.
- `low` — Typed-query generator is non-incremental, plus namespace, heuristic and null-argument edge cases: the generator is PrivateAssets to Projects.Client, only Projects queries carry the attribute, and the rewrite is owner work.
- `low` — Duplicated discriminator literals, a `GetTypes` scan and an unused `AddProjectsSupportedReads` registration: there is no divergence today.
- `low` — `conversationId` is unvalidated in OpenAPI: the server fails closed, and the fix needs a spine edit and regeneration.
- `low` — Pre-registered FoldersClient without a BaseAddress: this fails closed and needs a nonstandard host registration.
- `low` — Evidence file grows after the length check: the file is an operator-owned startup input.
- `low` — PascalCase `ConversationId` is ignored by the executor: generated bindings serialize with `JsonSerializerOptions.Web`, and the request fails closed.
- `low` — Legacy list returns row-level stale 503 versus list-level 404: this is a race between checks and fails closed.
- `false` — LegacyRollback detail drops Descriptive/Quarantined, and the quarantine switches are absent from OpenAPI: this is already rejected as prior triage item B7.

## Implementation Notes

Implementation, independent review, review patches and full source verification are complete; final presentation and Story 6.7 review tracking complete the workflow. The primary Web mode defaults to Unavailable and requires an explicit hash-bound startup selection. No deployment or package-mode adoption is claimed.

- The existing EventStore generator emits the seven Contracts-owned typed Gateway bindings; runtime declarations reconcile the same result types and schema digests. Existing v1 callable signatures, query constructors/deconstruction and name-only creation remain. New IClient metadata overloads have default bodies so existing implementers retain old calls and fail safely on unsupported new metadata. Generated artifacts/fingerprints are regenerated from the spine.
- Context/setup/explain require a named Conversation with current authorized native membership. The existing authenticated/delegated owner read preserves the original actor and returns bounded identity/membership/freshness evidence with a server-owned purpose marker. Full direct owner reads remain available. Advisory reads use separately classified committed isolated metadata with a distinct purpose; rich detail, advisory ProjectId, hydration and session inference never substitute for native membership.
- Legacy Get/Explain/Refresh recheck membership at final disclosure and sanitize metadata. The compatible Refresh overload and maintenance UI action carry the explicit Conversation. Base context assembly now honors original Setup exclusions, preserving actual Partial admission and exact omission evaluations on both paths.
- Legacy list/open require explicit descriptive inspection, existing permission and durable metadata-only audit; unbound/unauthorized/default calls fail safely. Supported Safe Metadata defaults and all audit-failure nondisclosure controls remain independently covered.
- The bounded Folder read adapter calls the owner's authoritative two v1 GET contracts with the existing bearer-forwarding handler. Folders' generated client currently targets its non-routed v2 candidate; this exact transport mismatch is documented. No Folders edit, route rewrite, per-request fallback, generator fork or write-adapter change is introduced.
- DW-57/59/60/66 repairs retain exact source global positions, separate supported aggregate versions, explicit unavailable/Partial behavior and final authority checks. Admission AsOf/Setup ObservedAt use the injected computation clock; source observations remain exact original timestamps. DW-63 remains open for Story 7.1.
- The [final v3 packet](evidence/epic6/6.7/review-01/read-cutover-final.json) derives all 77 passing authenticated comparisons and all seven legacy security slices from retained native XML and raw diagnostics. It binds original histories, independent physical/logical keys, exact source positions, decoded cursor behavior, current authority/native evidence, full supported-only AD-32 fields, persisted bytes/ETags and absence of unexpected SDK writes. Genuine Partial and rich-native-present/isolated-advisory-pending controls are executed.
- The [real-receipt rollback proof](evidence/epic6/6.7/review-01/rollback-final-proof.json) uses production selection: passing Supported, failed-comparison refusal, immutable existing selection, explicit LegacyRollback with seven real retained HTTP reads and unchanged domain records, and refusal with incomplete legacy security evidence. This is controlled authenticated local execution, not a deployed operational rollback.

The historical [blocked packet](evidence/epic6/6.7-read-cutover.json) and [rollback refusal](evidence/epic6/6.7-rollback.log) remain unchanged and superseded by versioned v3 source evidence. External Chatbot adoption and publication/versioning of owner/Projects artifacts for package-mode consumption remain release obligations. Neither adds an unapproved source feature-acceptance gate.

Baseline_commit remains unchanged. Frozen intent was explicitly renegotiated by the user's approval of the four follow-up recommendations; all other constraints remain. No deployment, publication, Git mutation, dependency version update, readiness promotion, Epic 7/8 pin change or AD-25 retirement occurs. DW-63 remains open for Story 7.1.

## Spec Change Log

- 2026-11-16: Recorded review decision 2: retain computation-time snapshot `asOf` and Conversation-start Setup `ObservedAt`; source/component observations retain their exact evidence times. Review decision 1 documents/defer the obsolete per-module CLI/MCP consumers; decision 3 defers rollback list authorization fan-out; decision 4 adds bounded startup refusal reasons and a single structured host log. Frozen D1–D6 decisions and prior receipts remain unchanged.

- 2026-11-16: User explicitly approved the narrow Pending-Folder materialization pair; advanced comparison revision to v3 and recorded D6 conditions. Prior receipts remain immutable and do not become passes.
- 2026-11-16: User approved all four follow-up recommendations with "approve". Recorded v2 comparison rules, native-membership-only admission and audited legacy inspection; added the approved decision record to context and the remaining implementation tasks. Prior blocked receipts remain historical and do not become passes.
- 2026-11-16: Recorded completed source implementation, retained explicit incomplete acceptance tasks, and linked blocked evidence and owner-decision proposals. Frozen intent and approved normalization are unchanged.

## Review Triage Log

| ID | Verdict | Route | Verified evidence |
| --- | --- | --- | --- |
| B1 | medium | defer | Startup selection trusts deployment-declared revision and contract values; stale configuration can authorize old evidence after a future binary replacement. Current source-only execution binds the actual checked manifest and immutable input packet. Intrinsic running-artifact/dependency binding remains a release/adoption obligation; no deployed qualification is claimed. |
| B2 | medium | defer | Confirmed pre-existing at c6541a3: context admission consumes persisted Folder evidence and the authorization gate performs no live Folder call. Delivered 6.2/6.3 explicitly prohibit sibling ACL fan-out and defer owner-batch Refresh/RTI. The reviewer withdrew this as a new regression. |
| B3 | false | reject | The reviewer withdrew the claim: approved metadata-only Conversation suggestions deliberately admit folderless/Pending-null Projects after exact Project authorization. Supported advisory has the same documented exception; attachment matching excludes them. |
| B4 | medium | defer | Retained attachment scoring can retain an earlier Folder match when the owning Folder changes between discovery and final authorization. The unchanged baseline helper/call sites already discard the new relationship; normal journal folding removes displaced references. This is a pre-existing race, not introduced by the cutover. |
| B5 | medium | patch | The newly added legacy per-row list filter ignores Retryable authorization/read failures and emits a successful truncated list. E1 has the same root cause; return unavailable instead of presenting exhaustive coverage. |
| B6 | medium | patch | The v1 marker/named transport registration is inside the no-existing-Folders-client branch. Pre-registering the standard generated client with an endpoint therefore bypasses the retained v1 repair. Register the read transport independently while retaining explicit custom directory overrides. |
| B7 | low | reject | Legacy quarantine query switches are real but absent from generated v1 parameters. They serve exceptional administrator inspection, which remains available through the typed supported query or explicit retained HTTP input. Adding generated overloads is more than a direct correction and is unlikely to affect ordinary consumers; preserve the existing public surface for this cutover. |
| B8 | medium | defer | The Open failure-to-safe_denial collapse is identical at baseline c6541a3 in ProjectDetailSource. It loses timeout/unavailable feedback, but predates this story; retain it as follow-up work. |
| B9 | medium | patch | The matrix labels context-store keys as legacy physical keys while reading legacy rows from the journal, and supplies fixture-synthesized identities to both observations. Record actual journal/container and supported storage addresses, independently decode/validate their identities, and retain logical aggregate keys distinctly. |
| B10 | medium | patch | The matrix assigns the same cursorObservation object to both observations, so that equality check is tautological. Existing authenticated CutoverSafety tests independently verify scope, but the native matrix must retain separate legacy transport evidence and supported decoded scope/boundary/continuation evidence. |
| B11 | medium | patch | The matrix absent-denied cell only changes to an ungranted actor against existing resources. Add genuinely missing targets with a valid authenticated actor and verify the documented per-operation safe absence behavior without equating valid-empty results to denials. |
| E1 | medium | patch | Confirmed the same new retryable-row truncation defect as B5; keep both findings in this log and apply one shared correction. |
| E2 | high | patch | Initial list authority is fresh, but later per-row IsAllowed decisions can carry bounded-stale Tenant authority. The new row filter accepts them and returns descriptive metadata. Recheck current row/final authority and withhold names on stale evidence. |
| E3 | high | patch | Supported IsQuarantined covers every Active binding not Included with nonnull FolderId. New legacy IsLegacyQuarantined only covers null/Pending-null, allowing Pending-with-ID or other non-Included historical records to bypass quarantine and expose Setup to ordinary inspection. Align the predicates and prove safe inspection. |
| E4 | high | patch | LegacyProjectDiagnostics now accepts ConversationId but its load cache compares only ProjectId. Changing the Conversation query parameter can retain prior context without the new native binding read. Include the Conversation in the load identity and discard stale in-flight results. |
| V1 | medium | patch | Pre-verified regression gap: current UI tests omit nonempty ConversationId through the real detail source and accept any Gateway payload. Add a binding-sensitive route/source/render test so removing either context/setup assignment fails. |
| V2 | medium | patch | Pre-verified regression gap: stale lifecycle/permissions tests instantiate the old v2 adapter. Exercise successful stale v1 bodies through production registration and prove legacy Folder state/label withholding. |

| R1 | medium | patch | Full verification exposed order-dependent dictionary snapshot comparison. All 22 keys and hashes were identical; snapshot enumeration is now sorted by storage address before comparison. The complete Server rerun passes 1484/1484; no application-array normalization or domain-state difference was introduced. |

All three review layers completed before classification. B5/E1 share one root cause; the other patch entries are independent. The implementation source manifest is the immutable pre-review handoff snapshot; workflow status/triage edits change this document separately.

### Enclosing review, resumed pass 2 — 2026-11-16

All three required lenses completed before classification. B3/E2 and B4/E1 share fixes; each finding retains its own verdict below. Frozen intent and prior decisions are unchanged.

| ID | Verdict | Route | Verified evidence |
| --- | --- | --- | --- |
| R2-B1 | medium | carried defer | Carried B4: retained attachment resolution still uses reverse-index relationships after authorization. The baseline and current helper discard the newly read relationship; this remains the previously recorded pre-existing attachment-link race. |
| R2-B2 | medium | defer | Both original-baseline retained resolvers and list assembly emit discovery-row metadata after separate detail authorization. No new metadata provenance rule or refresh was introduced by these patches; current-detail/list consistency remains pre-existing work. |
| R2-B3 | high | patch | The executor performs both Project/Tenant checks before awaiting native membership, then assembles usable context from that earlier detail. Revalidation after the introduced owner await is required to withhold stale, revoked, archived or changed evidence. |
| R2-B4 | high | patch | Legacy open now awaits durable inspection audit and live Folder lookup after its only authorization. The current function has no post-await fresh/stable authorization check, unlike legacy list; withhold descriptive output after a change. |
| R2-B5 | medium | defer | The supported 200-candidate advisory limit and unbounded retained discovery both predate the original baseline. The 77-cell source acceptance uses the approved bounded matrix and does not exercise a 201-Project population; a population-limit equivalence decision and test remain follow-up work. |
| R2-B6 | medium | patch | The new v1 adapter buffers the entire owner response and allows additive fields before JSON validation. An owner can return a large body; bound buffering and reject oversize without admitting metadata. |
| R2-B7 | medium | defer | The NuGetAudit=false override is present in the release-packaging refactor already committed before this resumed build, with no change in this session. The source verification preserves audits; remove the pre-existing packaging override in its owning release-tooling work. |
| R2-B8 | medium | patch | The authenticated cursor matrix ends after two one-row pages, so no second continuation cursor exists to inspect. Add a third ordinary Project and an advancing-clock continuation assertion that retains the original expiry. |
| R2-B9 | medium | patch | The transport matrix decodes authority scope and rejects altered filters but never replays a cursor under a different authenticated actor/workload/delegation/Tenant. Add actual authenticated replay refusals without relaxing admission. |
| R2-B10 | false | reject | The historical test serializes an anonymous object with explicitly spelled legacy field names and fixed values, not the current event type. Deserialization into the event therefore already tests an independently pinned wire shape; the cited serializer-roundtrip claim omits that independent construction. |
| R2-E1 | high | patch | Same verified legacy-open post-audit/Folder await authority gap as R2-B4; both reports require one final fresh/stable disclosure guard. |
| R2-E2 | high | patch | Same verified supported-context post-membership authority gap as R2-B3; recheck exact Project/Tenant authority and evidence after membership before assembly. |
| R2-E3 | high | patch | Legacy list checks Project authority/quarantine but never applies the supported delegated-ChatBot current Folder visibility rule. An explicitly inspection-authorized ChatBot can therefore receive rows that supported list omits or withholds. |
| R2-E4 | high | patch | Legacy open converts a denied live Folder read only into an unavailable reference and still emits descriptive Project fields. The supported ChatBot path denies that same caller; apply the existing caller-specific Folder boundary before disclosure. |
| R2-V1 | medium | patch | Verified the three real-owner test/helper files import Conversations.Server unconditionally while the test project supplies that dependency only in source mode. Guard them with HEXALITH_SOURCE_FIXTURE; owner Contracts/Client publication alone cannot resolve these server-only test imports. |
| R2-A1 | low | patch | The final paragraph of supported-project-reads.md still calls review-01/2813 tests current after the top link was updated. Correct the paragraph to the latest packet/count to keep the authorized documentation correction consistent. |


## Verification

Use `run-with-temp`; retain results outside `$TMPDIR`. Individually build the csproj files in `tests/Hexalith.Projects{,.Server,.Contracts,.Client,.UI}.Tests/` with Debug, `-m:1 --disable-build-servers`; run built xUnit v3 assemblies with native XML. Preserve audits; report lane-policy conflicts. Run production-authority validation with `--story-id 6.7` and `--validate-index`, and `git diff --check`; retain exact commands/results.

The pre-review source verification below used Debug, serial builds, disabled build servers, enabled audits and actual native xUnit XML. Final private outputs resolve beneath the owned run-with-temp directory; see [scratch correction](evidence/epic6/6.7/approved-v3/scratch-isolation-correction.json), [command arrays](evidence/epic6/6.7/approved-v2/final-session-commands.json) and [final checks](evidence/epic6/6.7/approved-v3/final-checks-reviewed.json).

| Suite | Native XML under approved-v3 | Passed / total | Skipped / not run |
| --- | --- | --- | --- |
| Domain | Domain-private-final-2.xml | 693 / 693 | 0 / 0 |
| Contracts | Contracts-private-final-2.xml | 207 / 207 | 0 / 0 |
| Client | Client-private-final-2.xml | 145 / 145 | 0 / 0 |
| Server, owned isolated Dapr | Server-private-final-4.xml | 1473 / 1473 | 0 / 0 |
| UI | UI-private-final-2.xml | 281 / 281 | 0 / 0 |
| Conversations owner | Conversations-owner-final-1.xml, catalog correction Conversations-catalog-final-2.xml | 1674 / 1675 before sole obsolete field-expectation repair; final catalog 3 / 3 | 0 / 0 |
| Final authenticated matrix and exact start headers | gateway-final-v3-5.xml | 1 / 1; 77 / 77 cells | 0 / 0 |
| Final real-receipt selection and rollback | gateway-rollback-final-v3-7.xml | 1 / 1; 77 / 77 cells plus 7 / 7 rollback reads | 0 / 0 |

All five Projects suites total 2799 passing tests. The owner broad run's sole failure was its old catalog field list; the targeted correction additionally proves NativeMembershipOnly remains optional/default-false for original payloads. No failing XML is eligible for activation. Earlier attempts, including the provisional output-root isolation error and pre-amendment comparisons, remain historical and are not counted as final private acceptance.

The owned session runner is `evidence/epic6/6.7/approved-v2/verify-session-final.py`; it owns the isolated state.in-memory Dapr sidecar and removes private scratch on exit. Both nested Client generator calls now receive the same private artifacts path. Exact source hashes, owner revisions, source-parity test names, XML hashes and cleanup are retained in the versioned evidence. Builds report zero warnings/errors. Source mapper diagnostics HXW011 (absent local Platform release tag), HXW009 (Builds pin mismatch) and HXW010 (local tool range) remain recorded; no dependency was fetched or changed to mask them.

`validate_production_authority.py --story-id 6.7`, `--validate-index` and root/Conversations/EventStore whitespace checks pass in the final checks receipt. Historical readiness, deferred-ledger acceptance, Epic 7/8 pins and AD-25 retirement are unchanged. The independent review and final verification below supersede that handoff.

### Post-review verification

The current packet is [review-01/read-cutover-final.json](evidence/epic6/6.7/review-01/read-cutover-final.json), bound to the final full-suite XML. Its [rollback proof](evidence/epic6/6.7/review-01/rollback-final-proof.json) executes seven retained HTTP reads against that exact packet hash. All earlier attempts remain immutable.

| Suite | XML under review-01 | Passed / total | Skipped / not run |
| --- | --- | --- | --- |
| Domain | Domain-full-1.xml | 693 / 693 | 0 / 0 |
| Contracts | Contracts-full-1.xml | 207 / 207 | 0 / 0 |
| Client | Client-full-1.xml | 145 / 145 | 0 / 0 |
| UI | UI-full-1.xml | 284 / 284 | 0 / 0 |
| Server | Server-full-2.xml | 1484 / 1484 | 0 / 0 |
| Real-receipt rollback | Rollback-full-2.xml | 1 / 1; 77 cells plus 7 retained reads | 0 / 0 |

The five full suites total 2813 passing tests. Every required matrix cell passes; the seven additional genuine missing-target/valid-empty probes retain current authority, raw responses and no-write evidence. Physical journal containers, logical aggregate keys and independently read identities are distinct; legacy transport and supported cursor evidence are retained separately with independent scope/boundary/expiry assertions.

All patch-routed findings are resolved: retryable/stale legacy list handling and final authorization, quarantine parity, pre-registered Folder transport plus stale-body refusal, Conversation-aware page reload/in-flight isolation, bound real-source Web rendering, and stronger storage/cursor/absence proofs. B5/E1 share one correction. Four deferred obligations were appended without changing existing entries: intrinsic production artifact binding, planned Folder owner/RTI admission, pre-existing retained attachment-link race, and pre-existing Open feedback collapse. No live-owner fan-out or public read surface was added for those deferred items.

The initial Server full run had one order-only snapshot assertion failure (22 identical key/value pairs), corrected and followed by the full passing Server rerun. Rollback-full-1 had no routing configuration and counts only as an additional matrix run, never operational rollback evidence; Rollback-full-2 is the explicit configured proof. A copied receipt-builder filename was corrected before the final packet was written. No failing or unconfigured attempt qualifies activation.

The fresh run-with-temp session used verified private artifacts, enabled audits, serial Debug builds and an owned Dapr sidecar. All builds succeeded with zero warnings/errors. The sidecar is reaped and its private workspace removed. Exact commands, native hashes, source snapshot and final static checks are retained under review-01. Source mapper HXW011/HXW009/HXW010 remain unchanged qualification limitations. Source qualification uses operator-declared startup identity; intrinsic running-artifact/dependency binding remains a release obligation before production adoption. Readiness, Epic 7/8 and AD-25 pins are unchanged.


### Latest review patch verification — 2026-11-16

All 22 latest `[Review][Patch]` entries are implemented and verified. The new current packet is [read-cutover-final-2.json](evidence/epic6/6.7/review-pass1-fixes/read-cutover-final-2.json), with [seven-read rollback proof](evidence/epic6/6.7/review-pass1-fixes/rollback-final-proof-2.json) and [source-manifest-2.json](evidence/epic6/6.7/review-pass1-fixes/source-manifest-2.json). All previous packets, native XML and failed attempts remain unchanged.

| Suite | Final native XML under review-pass1-fixes | Passed / total | Skipped / not run |
| --- | --- | --- | --- |
| Domain | Domain-full-1.xml | 693 / 693 | 0 / 0 |
| Contracts | Contracts-full-1.xml | 207 / 207 | 0 / 0 |
| Client | Client-full-2.xml | 154 / 154 | 0 / 0 |
| UI | UI-full-2.xml | 294 / 294 | 0 / 0 |
| Server | Server-full-3.xml | 1529 / 1529 | 0 / 0 |
| Explicit rollback | Rollback-full-3.xml | 1 / 1; 77 cells plus 7 retained reads | 0 / 0 |

The five full suites total 2877 passing tests. Every final build reports zero warnings/errors, with audits preserved. The primary entries and layout are covered in all three modes. Actual authenticated supported and retained context/explain/refresh paths withhold source labels. Additional controls pin configured audience/scope, quarantine role parity, native and classified advisory owner transport, Folder v1 tolerance/mappings, unavailable reference exclusion, cursor routing/refusal, exact Conversation forwarding, and Explain's null ProjectionType. A built UI startup probe records exactly one bounded refusal log and reaps the host. Twelve regenerated metadata-member XML blocks each contain one summary and unique parameter documentation.

The audience tests use the existing `ProjectsAuthenticationOptions.SectionName` (`Authentication:JwtBearer:Audience`). DW-57 now asserts the exact Fresh source watermark outside Dapr. Stale Tenant authority is intentionally refused with 503 and no usable Setup or freshness/watermark headers; the new non-Dapr stale case pins that security boundary rather than inventing a usable Stale response.

The original source mapper refused the pre-existing initialized Conversations/Builds nested checkout with HXW003. Verification preserved that checkout and used a private `run-with-temp` source copy with nested dependencies excluded. The supported mapper then passed; exact byte parity covers 22,242 source/test/build/documentation files across Projects and all 12 root-declared dependencies. Historical documentation artifact folders are explicitly excluded. All artifacts and package outputs stayed in private scratch. HXW007 (bounded Git inspection of the reduced copy), HXW009, HXW010 and HXW011 remain recorded qualification limits.

An early rollback attempt correctly failed after its fixed domain computation clock produced expired 30-second owner proofs. The integration fixture now constructs the existing SDK delegated-owner client with the real protocol clock while retaining the controlled domain/cursor clock. No proof lifetime, verifier, authority rule, deadline or comparison assertion changed. The final full Server rerun and explicit rollback pass; failing attempts never qualify activation.

Exact commands, hashes, original broad-gate blocker, startup proof and final cleanup are retained in [session commands](evidence/epic6/6.7/review-pass1-fixes/session-commands.json) and [final checks](evidence/epic6/6.7/review-pass1-fixes/final-checks.json). The owned Dapr sidecar is reaped and the private workspace removed. No deployment, publication, Git mutation, dependency update, readiness promotion, Epic 7/8 pin change or AD-25 retirement occurred. Existing deferred release/adoption obligations and DW-63 remain open; enclosing workflow review/tracking is separate from this completed patch implementation.


### Enclosing review completion and final verification — 2026-11-16

The three required review lenses completed and every finding received a separate verdict in the resumed pass-2 triage table. All patch-routed R2 findings, including the documentation correction, are resolved. B3/E2 share the post-membership authority fix; B4/E1 share the post-audit/Folder disclosure fix. Existing attachment/discovery/population-limit/release-tooling obligations remain deferred; the independent historical-wire-shape claim was rejected on its explicit literal-field construction. Frozen decisions, baseline, D2 rules, dependency pins and historical receipts are unchanged.

The latest source acceptance is [read-cutover-final-1.json](evidence/epic6/6.7/review-pass2-fixes/read-cutover-final-1.json), with [explicit rollback](evidence/epic6/6.7/review-pass2-fixes/rollback-final-proof-1.json), [final manifest](evidence/epic6/6.7/review-pass2-fixes/source-manifest-final-1.json), and [final checks](evidence/epic6/6.7/review-pass2-fixes/final-checks.json). These versioned receipts supersede earlier successful packets for the current source without rewriting them.

| Suite | Native XML under review-pass2-fixes | Passed / total | Skipped / not run |
| --- | --- | --- | --- |
| Domain | Domain-final-1.xml | 693 / 693 | 0 / 0 |
| Contracts | Contracts-final-1.xml | 207 / 207 | 0 / 0 |
| Client | Client-final-1.xml | 154 / 154 | 0 / 0 |
| UI | UI-final-1.xml | 294 / 294 | 0 / 0 |
| Server | Server-final-1.xml | 1561 / 1561 | 0 / 0 |
| Explicit rollback | Rollback-final-1.xml | 1 / 1; 77 cells plus 7 retained reads | 0 / 0 |

The five full suites pass 2909 tests with zero build warnings/errors and audits enabled. Root-owned commands are retained in `enclosing-full-commands.json` and `enclosing-rollback-commands.json`. Fresh comparisons bind all 77 passing cells and seven security slices to final native XML; the configured rollback executes seven actual retained HTTP reads and proves unchanged domain bytes/ETags. Exact source-copy parity covers 22,243 files across Projects and all 12 root-declared dependencies. The original HXW003 nested-checkout blocker and private-copy mapper diagnostics remain visible; no user checkout was changed to evade the mapper.

Disclosure now rechecks fresh, unchanged Project/Tenant/EventStore evidence after native membership and retained audit/Folder awaits. Regression tests cover revocation, staleness, changed authority generations, changed detail and membership refusal. Retained ChatBot inventory/open share the supported current Folder rule; six raw caller/outcome controls explicitly set `acceptedNormalizationPair=false`, preserving the approved finite D2 pairs. Folder v1 bodies stream under a private 16 KiB cap. The three-page cursor proof advances time, verifies the second continuation retains the original exact expiry, and observes successful fresh controls plus rejected old-cursor replay for changed actor/workload/delegation/Tenant.

A separate actual-Release-constants Roslyn check proves all three guarded owner-only files yield no owner declarations; it does not claim package-mode adoption. UI startup again emits one refusal log and is reaped. The final checks retain authority/index/whitespace validation and owned Dapr/private-workspace cleanup. No deployment, publication, Git mutation, readiness promotion, Epic 7/8 pin change or AD-25 retirement occurs.
