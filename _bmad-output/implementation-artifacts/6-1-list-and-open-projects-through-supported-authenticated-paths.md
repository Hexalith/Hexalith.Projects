---
story_id: 6.1
story_key: 6-1-list-and-open-projects-through-supported-authenticated-paths
epic: "Epic 6: Authorized Project Reads on the Supported Platform"
created: 2026-07-17
source_story_status: backlog
status: in-progress
blocked_by: []
baseline_commit: 90e481c
traceability:
  requirements: [fr-2, fr-5]
  nfrs: [nfr-1, nfr-5, nfr-10]
  architecture: [AD-3, AD-14, AD-19, AD-20, AD-32, AD-33]
  journey: UJ-1
  findings: [ARCH-001, API-001]
repository_authority: "Hexalith.Projects contracts, read models, and query handlers with platform-generated read adapters"
owners: [Product Owner, Solution Architect, Test Architect]
entry_gate: "Usable supported SDK reads and fail-closed authenticated actor/Folder authority, followed by focused specification readiness; approved 2026-11-02 E1-E7"
release_disposition: "Not a production-release gate; Story 8.11 owns terminal deployment, rollback, and stakeholder acceptance"
estimate: M
---

# Story 6.1: List and Open Projects Through Supported Authenticated Paths

Status: in-progress

**Current authority:** [approved entry correction](../planning-artifacts/sprint-change-proposal-2026-11-01.md), applied 2026-11-02. The dated July/August gate decisions in the Change Log are historical.

## Story

As a Project User on Chatbot or a Tenant-role actor on a permitted Web/CLI/MCP surface,
I want to list visible Projects and open one Project's authorized metadata, lifecycle, setup summary, and reference summary through the supported DomainService read models,
so that operators and Chatbot get current, authorization-filtered Project truth to initialize a Conversation (FR-2, FR-5) with no legacy runtime.

## Acceptance Criteria

1. **Supported authorized list/open path.** Given an authenticated caller with valid Tenant and actor authority and current read models, when `ListProjects` or `GetProject` executes through an `IDomainQueryHandler`, then the result is Tenant-scoped and authorization-filtered; carries the AD-32 snapshot fields `responseState`, `asOf`, `projectVersion`, `components`, and `recoveryActions`; and never computes, persists, or selects a resolution candidate. `ListProjects` uses an opaque platform cursor bound with `QueryCursorScope`, defaults to 50 rows, caps requests at 200 rows, filters by lifecycle, returns visible `Active` and `Archived` Projects, and has deterministic gap-free ordering. `GetProject` is a singleton read and does not accept paging.
2. **Indistinguishable safe denial.** Given a well-formed opaque Project identity that is denied, cross-Tenant, or nonexistent, when the read executes, then the boundary fails closed with a safe `404`; status, headers, body shape, timing category, logs, and telemetry do not disclose whether the Project or Tenant exists or why access failed. Unauthorized rows are absent from list results, while an unauthorized list scope fails safely. Structurally invalid query/filter/page/cursor input follows AD-19 `400` metadata-only validation semantics after authorization where protected validation could disclose information; it is not conflated with a well-formed absent identity.
3. **Honest response state.** Given stale, rebuilding, unavailable, or incomplete read-model evidence, when an already-authorized read executes, then it returns an honest `Partial` or `Unavailable` snapshot with component-level freshness, inclusion, safe reason, and last-verified evidence rather than inferred completeness. `Partial` is usable only when current Project, Folder, Setup, and authorization evidence are present and every optional omission is declared. `Unavailable` blocks Conversation context use. An authorized Archived Project may be inspected, but its lifecycle always blocks context initialization even when its metadata snapshot is `Complete`. If authorization itself cannot be established, no protected snapshot is returned.
4. **Additive compatibility and retained routing.** Given the supported list/open capability is accepted, when consumers use it alongside the legacy path, then contracts remain additive and historical events remain readable; legacy public routing is preserved. Reuse the existing local comparator as a regression. Exhaustive gateway/owner equivalence, canonical output/key/watermark/cursor comparison, finite approved normalization and reversible public-route cutover are mandatory in Story 6.7.
5. **FR-2/FR-5 response completeness.** Given an authorized list/open response, then it contains only the explicit metadata allowlist in Dev Notes. List rows expose authorized Project identity, name, lifecycle, current Project version, Project Folder availability, and enough AD-32 freshness/recovery evidence to choose a Project without loading full Project Context. Open exposes the approved Project metadata, lifecycle, typed setup summary, and Projects-owned reference summary. Pre-activation creation tasks never appear as Projects; full context, unvalidated setup source text, source payloads, and sibling-owned content are excluded.
6. **Read-only and additive compatibility.** Given any success, denial, retry, restart, rebuild, or fault scenario, then the operation performs no Project/domain write, event append, task creation, maintenance audit, resolution persistence, sibling mutation, or direct read-model repair. Platform-owned metadata-only durable security audit remains required for authorization denial under FR-21/AD-26 and must not disclose protected identity/resource detail. Existing event history remains readable, no event is rewritten, public contract evolution is additive, and the legacy read path remains available for comparison and later rollback until Story 6.7/8.11 retires it.
7. **Persisted authenticated verification.** Given the feature is considered complete, when existing focused test projects and the distributed Development lane run, then actual authenticated gateway → Projects → Folders list/open, persisted no-domain-write, replay/duplicates, Projects process restart, cursor and authority-change controls pass. Record tested Projects/direct dependency commits, exact commands, pass/fail/skip totals, native XML and result report. Reuse available receipts only at their recorded revision and for covered criteria; run relevant missing evidence. Common-runner publication and a second hash inventory are not completion requirements.

**AC2 observability and timing scope — user decisions, 2026-11-04:** Denied, missing and cross-Tenant reads must also have no identifying differences throughout internal service logs and traces. Aggregate infrastructure metrics may remain without Project/Tenant identifiers or trace links. The user explicitly accepted the residual possibility of inferring internal work from aggregate counter changes during quiet periods; hiding that operation-count traffic pattern is outside this story's denial-privacy requirement. The user approved a minimum-duration mitigation starting at 50ms total gateway time for public SDK query 404 replies, to be verified through the real distributed path. Cancellation and existing deadlines retain precedence; slower/faulted replies may still vary. This is a bounded mitigation and does not claim constant-time or production/load latency indistinguishability.

**Approved denial-privacy policy (2026-11-04):** The user requires identifying log/trace differences to be removed throughout the read chain and accepts aggregate infrastructure metrics without Project/Tenant IDs or trace links, including quiet-period operation-count inference. The subsequently approved timing mitigation is a 50 ms minimum total public SDK POST `/api/v1/queries` safe-404 gateway duration before headers, waiting only for any remainder and preserving cancellation/deadlines. Already-slow replies receive no additional wait; slower/fault variation remains, with no constant-time or production/load timing guarantee. This approved policy supersedes older pending timing-choice notes below.

## Tasks / Subtasks

- [x] Correct successful list-page enumeration snapshots independently of row readiness; cover mixed/empty/paged/quarantine results while retaining per-row evidence and unavailable/denial paths. Focused and fresh authenticated acceptance evidence is recorded below.
- [x] Finish/review existing supported contracts, projection/query handlers and composition against AC1–3/5–6. Preserve current Folder-read authority before paging, Safe Metadata defaults, distinct descriptive inspection permission and durable metadata-only audit; audit persistence failure denies disclosure.
- [x] Classify the complete event catalog explicitly: known relevant events apply; explicitly catalogued harmless additive events are deterministic no-ops proven by compatibility tests. Unknown potentially relevant, corrupt or undecodable events fail projection/rebuild honestly and yield Unavailable; neither blanket skip nor blanket rejection of harmless additions satisfies NFR-10. Verify additive serialization and legacy behavior under AC4. Keep public routing intact and hand the existing comparator to 6.7 for exhaustive migration qualification.
- [x] Inspect retained receipts and run the existing focused/persisted lane for missing AC7 evidence; record actual commands, commit references, native XML, totals and result report. Do not infer feature acceptance from this readiness correction.

### Review Findings

Code review 2026-11-05 covered chunk G1 only: production `src/`, `git diff --ignore-cr-at-eol 47a1ffd..bc545fd -- src`, 33 files and 1,736 diff lines. The recorded `baseline_commit` 90e481c was 517 commits stale. Four layers ran and none failed: Blind Hunter, Edge Case Hunter, Verification Gap and Acceptance Auditor. Three chunks still need their own reviews: G2 Server/Contracts tests, G3 Integration tests and G4 qualification launcher. The sibling EventStore privacy/timing diff `0ace1545..2dca77cc` was out of scope.

- [x] [Review][Decision] The fold-time `IsValid` gate fails event histories the write side accepts, which empties the whole Tenant list (high). Resolved 2026-11-05 by Jerome: option (a), now a patch below — `ProjectReadProjectionHandler.cs:320` now rejects any detail where `ProjectPersistedDetailValidator.IsValid` is false.
  - That validator requires each file reference's `FolderId` to equal the Project Folder, and each `ObservedAt` to fall within `[CreatedAt, UpdatedAt]`.
  - Nothing on the write side enforces either rule. `SetProjectFolder` with `ReplacementConfirmed` keeps the existing file references. `LinkFileReference` never compares the file's Folder with the Project's Folder, in the processor or the aggregate, and it accepts links while the Folder is still Pending. `UpdatedAt` follows each event's `OccurredAt`, so clock skew between replicas can also trip the lifetime check.
  - Result: the read model stays Unavailable for good, because FullReplay fails at the same event. `index.UnavailableProjectIds` is then non-empty, and every `ListProjects` call for that Tenant returns an empty Unavailable page (AC3/AC4/AC6, NFR-10). Before this diff the fold only checked `detail is null`, and the read-time `referencesValid` check degraded the references instead.
  - Options: (a) restore the pre-diff fold check and keep validation and degradation at read time (recommended); (b) check only `IsHeaderValid` at fold time, which still leaves its lifetime checks sensitive to clock skew; (c) keep `IsValid` and change the write/projection semantics instead: rebase or drop file references on Folder replacement and keep `UpdatedAt` monotonic, which reaches into other stories.
  - Whichever option is chosen, add a regression test: link a file, confirm a Folder replacement, then list and open.
- [x] [Review][Decision] Quarantined rows and quarantined opens advertise `Retry` as their recovery action (medium). Resolved 2026-11-05 by Jerome: use `ResolveNeedsAttention`, now a patch below — `ProjectReadQueryExecutor.cs:320` returns `Retry` for every non-Complete snapshot. Quarantine only ends through FR-8 Administrator Folder binding, so retrying never helps. Choose `ResolveNeedsAttention`, `ContactAdministrator`, or `None` plus a distinct `Quarantined` safe reason for quarantine snapshots.
- [x] [Review][Decision] `IncludeQuarantined` selects only quarantined rows, despite the "include" name (low). Resolved 2026-11-05 by Jerome: keep the name and sharpen the XML documentation, now a patch below — `ListProjectsQuery.cs:17`. With `true` the response is the Administrator inventory alone, not ordinary results plus quarantined ones, which Web, CLI and MCP authors will likely misread. Rename it before the contract ships (for example `QuarantineInventory`), or keep the name and rely on the XML documentation.
- [x] [Review][Patch] Restore the pre-diff fold check (`detail is null` only), keep read-time `IsValid`/`referencesValid` degradation, and add a regression test: link a file, confirm a Folder replacement, then list and open, with the Project shown as Partial and the Tenant list still Complete [src/Hexalith.Projects.Server/Projections/ProjectReads/ProjectReadProjectionHandler.cs:320]
- [x] [Review][Patch] Return `ResolveNeedsAttention` instead of `Retry` as the recovery action on quarantined inventory rows and quarantined opens, with a test [src/Hexalith.Projects.Server/Queries/ProjectReadQueryExecutor.cs:320]
- [x] [Review][Patch] Sharpen the `IncludeQuarantined` XML documentation: it returns only the Administrator quarantine inventory, never ordinary results plus quarantined ones [src/Hexalith.Projects.Contracts/Queries/ListProjectsQuery.cs:9]
- [x] [Review][Patch] The worker catalog test throws in the package-mode CI run [tests/Hexalith.Projects.Server.Tests/ProjectsWorkerCatalogTests.cs:27] — `UseProjectsDevelopmentWorkerSecurity(selected: true)` throws when `SUPPORTED_PROJECT_READ_SOURCE` is undefined, which is the case when CI sets `CI=true`. Server.Tests runs in CI unfiltered. The failure is currently hidden because CI run 41973647780 stops earlier, at "Validate workflow policy regression fixtures". Gate the source-only tests on the source-mode symbol. Source gating is fixed; package-build confirmation is tracked separately below.
- [x] [Review][Patch] Add a test that an Administrator `InspectQuarantined: true` open of a Folder-bound Project returns safe-denial [src/Hexalith.Projects.Server/Queries/ProjectReadQueryExecutor.cs:201]
- [x] [Review][Patch] Add a test that a rejection no-op with a foreign tenant, a foreign ProjectId or an undefined `Reason` fails and leaves `Sequence` unchanged [src/Hexalith.Projects.Server/Projections/ProjectReads/ProjectReadProjectionHandler.cs:221]
- [x] [Review][Patch] Add a test that an Archived folderless Project stays in ordinary list/open and stays out of the quarantine inventory [src/Hexalith.Projects.Server/Queries/ProjectReadQueryExecutor.cs:249]
- [x] [Review][Patch] Add a test that a payload with case-variant duplicate keys (`name`/`Name`) fails the content gate [src/Hexalith.Projects.Server/Projections/ProjectReads/ProjectReadProjectionHandler.cs:187]
- [x] [Review][Patch] `HasRequiredContent`'s default arm silently gives future catalog types a schema; list the four rejection types explicitly and make the default fail closed [src/Hexalith.Projects.Server/Projections/ProjectReads/ProjectReadProjectionHandler.cs:205]
- [x] [Review][Patch] Remove the dead "optional" catalog-lane branches and the misleading compatibility comment (`Validate` already requires those keys), plus the Tenants bootstrap and `BoundCommand` environment settings duplicated in `SupportedProjectReadDevelopmentInfrastructure` [src/Hexalith.Projects.AppHost/SupportedProjectReadFixtureProfile.cs:240]
- [x] [Review][Patch] The AppHost resolves Platform and Builds only under `references/`; add the sibling-layout `Exists` probes that `Directory.Build.props` uses, otherwise sibling-layout Debug builds fail with CS2001 [src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj:8]
- [x] [Review][Patch] The intent adapter's comment says "preserving null", but its options drop nulls; the options are also a hand copy of the processor's. Fix the comment and share one options instance [src/Hexalith.Projects.Server/ProjectsIntentAdapters.cs:35]
- [x] [Review][Patch] The fold parses each payload twice; deserialize from the `JsonDocument` it already parsed [src/Hexalith.Projects.Server/Projections/ProjectReads/ProjectReadProjectionHandler.cs:274]
- [x] [Review][Patch] Remove the unused `using` directives in the new `*Payload.cs` record files [src/Hexalith.Projects.Server/ArchiveProjectPayload.cs:3]
- [ ] [Verification] Confirm the Server.Tests package-mode build; the attempted Release build is blocked at restore by NU1109 (Tenants.Client 7.0.0 requires EventStore >=4.0.4 while Projects selects 3.133.0).
- [ ] [Verification] Complete fresh authenticated distributed AC7 verification and subsequent workflow review; current owner SDK platform-workload source capability is missing before Project reads.
- [x] [Review][Defer] Advisory resolution still offers Active folderless (quarantined) Projects as candidates [src/Hexalith.Projects.Server/Queries/ProjectAdvisoryQueryExecutor.cs:168] — deferred: pre-existing. The file was last changed at ded1e70, outside this diff. `ProjectAdvisoryQueryTests.Review4.FolderlessProjectsUseExactProjectAuthorityWithoutOwnerCalls` expects `SingleCandidate`. This contradicts PRD §2.4/NFR-4 and these Dev Notes. Resolution is Story 6.4 scope (medium).

Rejected findings, 24 in total. "False" means the claimed problem does not occur; "low" means it is real but not worth fixing:
- False: newly created Projects hidden while their Folder is pending. PRD NFR-4/§2.4 require that no caller-visible Active Project is folderless.
- False: an empty quarantine inventory page writes no audit. FR-22 requires one inspection event per Project returned, and none are returned.
- Low: a per-row audit failure partway through a page leaves over-audit records. This fails closed and is rare; fixing it needs batching or compensation.
- Low: up to 200 sequential audit writes per inventory page. The inventory is legacy-only, and batching needs new SDK surface.
- False: historical events breaking the new content gate. The event contracts have not changed since they were created, and the writer enforces the same rules, including grant expiry `> now`.
- False: the `is ProjectId` reflection gap. The main fold loop already checks `IProjectEvent` identity; the `NullReferenceException` filter fails closed.
- False: the package-mode AppHost profile throwing. The private lane is source-only by design and throws an explicit message.
- Low: owner-assembly provenance. The check targets load-context substitution, and the Folders adapters are built from the same source in the same build.
- Low: catalog combined with the anonymous Development bypass fails at request time. It is a Development-only invalid combination, and guarding it adds branching.
- False: order-dependent security registration. Calling them in the wrong order fails loudly at startup with "scheme already exists".
- Low: admin flags appear in agent-eligible schemas, and strict unknown-field rejection would break a newer client against an older server. No in-repo serializer or released strict server exists.
- Low: `AddQualificationResiliency` throws `NullReferenceException` before its friendly error for a malformed baseline. That baseline is controlled by the repository.
- Low: route literals repeated by hand in the policy bindings.
- Low: anonymous health endpoints in every environment. Outside the Development lane there is no fallback policy, so behavior does not change.
- Low: the three hand-maintained payload lists. The existing parity test catches drift.
- False: `observations.Add` throwing on a duplicate ID. Index IDs are checked for distinctness beforehand; filling the dictionary on every row is negligible.
- False: requiring both the role and the A-5 permission. With A-5 now present, that satisfies both FR-22 clauses.
- Rejected: the AC7 lane using Development-catalog security. That is the approved scope extension; changing it would mean editing the spec.
- False: compiled catalog declarations reaching production. Only in-process Development owners read them, and Projects maps the catalog only in the Development lane.
- Low: diagnostics and tuning flags in the qualification lane. They affect Development route-catalog plumbing, not list/open semantics.
- Low: the catalog is not disposed when `Add` throws after composing it. The launcher's owned cleanup removes the private material.
- Low: the mTLS receipt `CreateNew` collision. It can only happen if a private directory is reused, and the launcher allocates a fresh one each run.
- False: removing the page-level aggregation. Task 1 and the Dev Notes require the page snapshot to describe enumeration only.
- False: Archived folderless Projects not quarantined. PRD §2.4 quarantines only Active folderless Projects.

## Dev Notes

### Authority, Readiness, and Scope

The approved 2026-11-02 E1–E7 correction supersedes the old P0–P4 entry chain and independent READY prerequisite for 6.1 and its 6.5 consumer only. 6.1-P0/P1R-remediation/P2/P3 remain open under Epic 8; P1/P1R remain historical accepted qualification with current P1R usability false; P4 is superseded. Actual defects in the selected read/authority path still block that path. No sibling edit, publication, enrollment, runtime retirement or cutover is authorized here.

Readiness confirms the implemented supported substrate and real authority composition plus executable verification commands. Story acceptance remains outstanding; completion follows the November 1 rule at its tested commit. 6.2/6.3/6.4 remain done, DW-1 remains open and 6.5 blocked. All release and writer boundaries remain separate.

### Canonical Response Semantics

- AD-32 fields are `responseState`, `asOf`, `projectVersion`, optional `resolutionResult`, `components`, and `recoveryActions`. Canonical states are `Complete`, `Partial`, `Unavailable`, and `Denied`; protected `Denied` detail stays internal and collapses to the safe-404 boundary.
- Components use inclusion exactly `Included|Excluded`, freshness exactly `Current|Stale|Rebuilding|Unavailable`, a metadata-only safe reason, and last-verified time when known. Recovery codes are exactly `None|Retry|RefreshContext|RequestPreview|RenewPreview|PollTask|ResolveNeedsAttention|SelectAlternative|ContactAdministrator`; Story 6.1 emits only the applicable read recovery codes and their bounded metadata.
- `asOf` is the timestamp of this authorization-and-evidence computation, not a fabricated projection timestamp. `projectVersion` is the authorized aggregate sequence when disclosable; keep it distinct from store ETags, projection dispatch positions, and the legacy global watermark.
- `Partial` is not a synonym for stale. It is usable only when Project, Folder, Setup, and authorization evidence are current and every optional omission has component evidence. `Unavailable` blocks initialize/resume. Recovery/refresh never silently rewrites an earlier response.
- List enumeration is authorized and current before paging; each visible Active/Archived entry retains its own AD-32 state, and Folder not Current makes that entry Unavailable. Tenant-role responses use Safe Metadata unless distinct descriptive inspection is authorized and durably audited. Pre-activation durable-task records never appear. Archived metadata can be complete for inspection while `lifecycleState = Archived` independently blocks Conversation context use.
- Setup summary means the existing metadata-only Project setup fields authorized for list/open. It is not Story 6.2's bounded Conversation-start payload. Reference summary means opaque reference kind/id/display metadata, availability/trust/freshness, and safe reason only; it is not Story 6.3 full context or sibling-owned content.
- Stable structured state/reason/component values are required so later Web/CLI surfaces can be accessible and surface-invariant. Story 6.1 does not implement UI, CLI, MCP, Playwright, or accessibility acceptance.

### Metadata Allowlist

- List page (`ProjectListPage`): only `items`, protected `cursor`, `hasMore`, and nested `snapshot`. Each `ProjectListRow` has only `projectId`, authorized `name` (omitted for Safe Metadata callers), `lifecycleState`, `projectFolderAvailability`, and its own nested `snapshot`. Each `AdmissionSnapshot` contains `responseState`, `asOf`, authorized aggregate-sequence `projectVersion`, `components`, and `recoveryActions`; these fields are not flattened onto the page or row. The page snapshot describes enumeration, while each row snapshot describes that Project. Do not echo Tenant or actor identity.
- Open Project (`GetProjectResponse`): only `projectId`, authorized `name`, validated authorized `description`, `lifecycleState`, `createdAt`, `updatedAt`, authorized `setup`/`setupMetadata`, `references`, and nested `snapshot` with the same `AdmissionSnapshot` fields. `projectVersion` stays inside `snapshot`. Do not add audit, task, Conversation, resolution, or sibling payload fields.
- Typed setup summary: only the already validated/bounded `ProjectSetup` contract (`Goals`, `UserInstructions`, `PreferredSourceKinds`, `ExcludedSourceKinds`, `ConversationStartDefaults`) plus the existing opaque `SetupMetadata` reference when authorized. These values remain subject to payload/leakage validation; never expose an unvalidated source document, provider/model prompt, transcript, secret, token, or sibling content. Story 6.2 owns the separately bounded Conversation-start response and admission truth.
- Reference summary: only `referenceKind`; the Projects-stored opaque `FolderId`, `FileReferenceId`, or `MemoryReferenceId` applicable to that kind; the Projects-owned bounded `DisplayName` intent already in the event/read model; `ReferenceState`; stable safe `ReasonCode`; and event-carried `ObservedAt`/AD-32 freshness evidence. Do not fetch or expose sibling-owned labels, workspace/path/URI/content/hash/classification/provider data, MemoryUnit fields, raw authorization detail, or raw upstream problems.
- Recovery actions carry only the exact AD-32 code and applicable bounded retry guidance. Story 6.1 creates no task/preview; therefore it does not invent task identity/status or Preview-expiry data.

### Current Implementation and Verification

The supported implementation exists in `src/Hexalith.Projects.Server/Queries/ListProjectsQueryHandler.cs`, `GetProjectQueryHandler.cs`, `ProjectReadQueryExecutor.cs`, `ProjectReadQueryAdmission.cs`, `ProjectReadAudit.cs` and `Projections/ProjectReads/ProjectReadProjectionHandler.cs`. It composes SDK query/projection/store/batch/cursor seams beside legacy routing. `ProjectReadShadowComparator` in `src/Hexalith.Projects.Testing/Reads/` is a retained local regression, not accepted full migration equivalence.

`tests/Hexalith.Projects.Server.Tests/Queries/SupportedProjectReadTests*.cs` and `SupportedProjectReadHttpTests*.cs` cover policy, persistence/cursors, safe denial and local compatibility. `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadDistributedTests.cs`, its real owner-backed fixture and evidence controls supply the authenticated persisted/restart/replay lane. Never substitute handler-only allow stubs for that proof.

- Keep the current repository SDK/package configuration; no version changes are part of feature readiness. Development builds use Debug/source references; CI retains Release/package references.
- Use existing platform persistence and cursor infrastructure; no custom Dapr journal, query runtime, signature scheme or read-time repair.
- Stop for missing actor/workload delegation, wrong audience/scope, stale authority, unsafe denial or required audit failure. Queries append no domain event or Project/sibling/task/resolution mutation; metadata-only security/inspection audit is observed separately.
- Legacy Active folderless Projects remain quarantined and excluded from ordinary list, resolution, context and Conversation-start. FR-22 quarantine inventory is Tenant Project Administrator-only: Safe Metadata plus the narrowly scoped exception for Project name and any pre-v1 Folder or creation receipt, never Setup or titles, with one metadata-only FR-21 inspection event per Project returned; role-gated until the A-5 inspection permission exists, then gated on it. Binding requires FR-8 Preview and confirmation; inventory inspection never admits context. Archived inspection never admits context.
- Current required Project/Folder/Setup/authorization evidence is mandatory for usable open; optional omissions stay explicit. Unknown evidence never becomes Complete.

Run missing evidence using the existing lane with a fresh destination:

```bash
MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 DOTNET_PROCESSOR_COUNT=2 python3 tools/qualification/run_supported_project_reads.py --regressions --timeout 900
```

The launcher builds individual Debug test projects, invokes built assemblies and retains native XML, matrix and cleanup results. Record actual tested commits and totals in this story. Local source proof does not certify published binaries, production enrollment or 6.7 cutover. NFR-5 10k/p95 proof remains 8.9.

### Focused readiness — 2026-11-02

Read-only inspection at Projects `710c992fe703878f20732044584c97757d6dad05` confirmed the supported SDK handlers, persisted projection/batch recovery and platform cursor path; authenticated gateway/Folder authority composition; inspection audit; and existing Server/Integration matrices and launcher. `python3 tools/qualification/run_supported_project_reads.py --help` exits 0 and exposes the documented regression lane. The retained October 27 results in `docs/supported-project-reads.md` describe eleven distributed rows, Integration 128/128, Server 1306/1306 and launcher 63/63 with no failures/skips; these are historical results, not rerun or accepted here. Any unavailable underlying XML/report or uncovered criterion requires relevant verification before done. Readiness also inspected EventStore `0ace1545c433bf234f54cef3a57ac77d62b4175f`, Folders `0eabc23856fde13dd76cab5d7ac2cb45739df8c5` and Builds `4d2ef11ea7f36b955ed701fef5af1f26a6414942`; these are inspection coordinates, not fresh runtime-test claims. These concrete inputs support ready-for-dev to finish/review, with no missing capability identified by this scheduling inspection. No .NET/live qualification or feature completion is claimed.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-6.1-List-and-open-Projects-through-supported-authenticated-paths]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic-6-Authorized-Project-Reads-on-the-Supported-Platform]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md#5-Observable-Context-and-Recovery-Contract]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md#Functional-Requirements]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md]
- [Source: _bmad-output/planning-artifacts/implementation-readiness-report-2026-07-17.md]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-17.md]
- [Source: _bmad-output/planning-artifacts/implementation-readiness-traceability-matrix.yaml]
- [Source: _bmad-output/planning-artifacts/epics-architecture-conformance-checklist-2026-07-16.md]
- [Source: _bmad-output/test-artifacts/test-design-epic-6.md]
- [Source: _bmad-output/project-context.md]
- [Source: references/Hexalith.AI.Tools/hexalith-llm-instructions.md]
- [Source: references/Hexalith.AI.Tools/hexalith-state-instructions.md]
- [Source: references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/IAsyncDomainProjectionHandler.cs]
- [Source: references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/IDomainQueryHandler.cs]
- [Source: references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/IReadModelStore.cs]
- [Source: references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/ReadModelWritePolicy.cs]
- [Source: references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Queries/IQueryCursorCodec.cs]
- [Source: references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Queries/QueryCursorScope.cs]
- [Source: references/Hexalith.Tenants/src/Hexalith.Tenants/Queries/Handlers/ListTenantsQueryHandler.cs]
- [Source: references/Hexalith.Tenants/src/Hexalith.Tenants/Queries/Handlers/GetTenantQueryHandler.cs]
- [Source: src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs]
- [Source: src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs]
- [Source: src/Hexalith.Projects.Infrastructure/DaprProjectProjectionStore.cs]
- [Source: src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs]
- [Source: src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailProjection.cs]
- [Source: tests/Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.cs]
- [Source: tests/Hexalith.Projects.Tests/Replay/ProjectionRebuildDeterminismTests.cs]
- [Official .NET 10 compatibility guidance: https://learn.microsoft.com/dotnet/core/compatibility/10]
- [Official .NET release and support policy: https://learn.microsoft.com/dotnet/core/releases-and-support]

## Dev Agent Record

### Historical story-creation record (2026-07-17)

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Story creation only; no implementation build or test was run.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Source discovery covered the corrective epics, canonical PRD, canonical architecture spine, readiness/reconciliation artifacts, Epic 6 test design, all project-context facts, current code/tests, EventStore supported APIs, current package/submodule revisions, recent Git history, and official .NET 10 lifecycle/compatibility guidance.
- Story creation records the 2026-07-17 readiness supersession while preserving the unresolved Epic 6 package, identity, composition-runner, and planning-reconciliation gates as explicit implementation hard stops.
- Independent create-story checklist review was applied: corrected AD-19 invalid-input semantics and FR-21/AD-26 denial audit handling; fixed the authoritative Epic/source references; enumerated the metadata allowlist and update-file guidance; constrained shadow normalization; and required proof through the authenticated gateway-to-`/query` chain.

### File List

- `_bmad-output/implementation-artifacts/6-1-list-and-open-projects-through-supported-authenticated-paths.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Implementation and verification attempt (2026-11-02)

The supported list/open path now excludes Active folderless Projects from ordinary reads. Explicit Tenant Project Administrator quarantine inspection exposes only the narrowly allowed name/receipt metadata, with one durable metadata-only audit per returned Project. Projection tests classify the complete known event catalog and fail unknown, corrupt, or undecodable duplicate events. The isolated AppHost fixture now opts into the current SDK Development authority contract and probes Folders' anonymous `/alive`.

[Evidence and exact commands](evidence/story-6-1-20261102/README.md) retain native XML, the sanitized launcher report, build logs, tested Projects/direct dependency commits, and cleanup results. Projects.Server build: 0 warnings/errors. Projection class: 28 pass, 0 fail, 0 skip. Targeted quarantine cases: 6 pass, 0 fail, 0 skip. Broader read class: 73 pass, 3 fail, 0 skip because the standalone selector lacked live Dapr endpoints. Fresh distributed lane: 1 fail, 0 pass, 0 skip at `BootstrapGlobalAdmin` HTTP 503; EventStore logged `RouteCatalogUnavailableException`. Its owned services were healthy, its Integration.Tests build passed, and cleanup preserved preexisting resources; no persisted matrix completed. Full source Server.Tests build failed with 8 Conversations.Server missing EventStore checkpoint/tenant snapshot type errors.

Story acceptance remains open pending current route-catalog enrollment for the isolated lane, source compatibility, and a clean authenticated persisted matrix covering no-domain-write, replay/restart, cursor, and authority change.

### Follow-up implementation and verification (2026-11-02)

The isolated launcher now prepares the Platform-selected Projects Debug source mapping itself. It keeps mapping directories traversable only to the owner and removes them during owned cleanup. No Project query/projection source, sibling source, public route, domain event, or dependency version changed in this follow-up.

[Fresh evidence packet](evidence/story-6-1-20261102-followup/README.md) records Projects HEAD `3f2931a4680180db177d315aab327087eee05fef` plus its uncommitted launcher change bound by `sourceManifestSha256`; direct dependencies EventStore `7c93a1234d14c66f5ee85ed48caaee1d30a671f3`, Tenants `964d64561b8fdd06e146ef7e304f25f51d276ace`, Folders `531bee88db685b703c7092ea5e2eaf683f15cede`, Conversations `c9e6fdebc7725df3462a1db991f5d69a4b1f22b9`, and Builds `d2a430c988462a634674a69c562ea4afd1f06ef4`. The exact prescribed command ran from the Projects root:

```bash
MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 DOTNET_PROCESSOR_COUNT=2 python3 tools/qualification/run_supported_project_reads.py --regressions --timeout 900
```

Launcher controls passed 63/63. The Integration assembly built, and all owned services were healthy. Native [distributed XML](evidence/story-6-1-20261102-followup/distributed.xml) records 0 pass, 1 fail, 0 skip: `BootstrapGlobalAdmin` HTTP 503 with EventStore `RouteCatalogUnavailableException`. The [sanitized report](evidence/story-6-1-20261102-followup/run.json) records `matrixCompleted=false`, exact AppHost/owned-container removal, preexisting resource preservation, and private material removal. No persisted matrix or launcher regressions ran.

Local [projection XML](evidence/story-6-1-20261102-followup/projection.xml) passed 28/28; quarantine and inventory selectors passed 3/3. The broader [read XML](evidence/story-6-1-20261102-followup/read.xml) passed 73/76; its three live gateway cases failed because the standalone process lacked the launcher's owned Dapr endpoints. A prepared AppHost Debug source build passed with 0 warnings/errors, while the full Server.Tests source build failed with eight missing Conversations.Server/EventStore checkpoint and Tenant snapshot types; the prebuilt-sibling fallback passed with 0 warnings/errors. Exact commands and logs are in the packet.

AC7 remains blocked. The authenticated persisted parts of AC1–3 and AC5–6 therefore remain unaccepted at this source revision. The AC4 local comparator passed separately (1/1; [native XML](evidence/story-6-1-20261102-followup/shadow.xml)), while the launcher did not reach its regression phase. Route-catalog enrollment and sibling source repair are outside this story's approved scope; exhaustive cutover equivalence remains Story 6.7. Status stays in progress.

### Catalog prototype diagnostic (2026-11-02)

An uncommitted Projects-owned Development route-catalog prototype reached committed catalog receipts on EventStore, Tenants, Projects, and Folders. Its live distributed test still failed before list/open: Tenants' bound bootstrap received Dapr `ERR_DIRECT_INVOKE` (HTTP 500), with no mTLS caller identity available to the closed Dapr ACL. The [diagnostic packet](evidence/story-6-1-20261102-catalog-prototype/README.md) retains the tested revisions, command, sanitized report, and native XML (**0 pass, 1 fail, 0 skip**). The prototype was removed because enrollment is outside this story's approved scope. No authenticated persisted matrix completed, and no AC7 or live AC1–3/5–6 acceptance is claimed. Story status remains in progress.

### Enumeration correction and current verification blocker (2026-11-02)

Successful authorized list pages now report Complete enumeration with current Enumeration evidence; individual rows retain their Complete/Partial/Unavailable state, freshness, omissions and recovery guidance. Quarantine inventory follows the same page/row distinction. Failed authorization sweeps, changing indexes and unknown Chatbot Folder visibility retain their existing unavailable/denial behavior. The event catalog now documents the three existing ChatBot authority contracts and their explicit list/open metadata no-op classification.

The [retained packet](evidence/story-6-1-enumeration-20261102/README.md) binds Projects HEAD `98efe700b60cacfe0b3cc3cb0c134e0902419b4c` plus this uncommitted correction and the recorded direct dependency revisions. Fresh Debug fallback builds compiled the changed Server/tests with zero warnings/errors against prebuilt sibling outputs. Native XML records **111 passed, 0 failed, 1 skipped** across read/projection/HTTP selectors; the retained legacy HTTP comparator passed. The seven new enumeration cases are included in those passes and passed again after CRLF normalization (**7/7, no failures/skips**), checking mixed/empty continuations and unchanged stored values/ETags. The one existing Dapr restart fixture is skipped, and three live gateway catalog cases are explicitly excluded; neither is accepted coverage. Complete event-catalog/projection controls passed 28/28.

The baseline authenticated launcher exited 1 before AppHost/resource allocation. Its controls passed 63/63 and cleanup preserved existing resources. A direct source-preparation retry exited 2 with `HXW009`: pinned Builds file `Props/Directory.Packages.props` at `4456dfb7869a307d009038c6b8523a9b6c0a732e` cannot be read from the root-declared Builds repository. Ordinary source builds also retain Conversations' missing nested import and Tenants/Conversations source-compatibility/mapping failures. Exact commands, outputs and limits are in the packet. This run did not reach the earlier route-catalog/mTLS failures. AC7 and authenticated persisted AC1–3/5–6 remain unaccepted; Story 6.1 remains in progress. Both production-authority validators and whitespace checks pass.

### Focused acceptance follow-up — 2026-11-04

The canonical spec's latest continuation supersedes older source-preparation blockers without accepting the feature. The [current isolated test packet](evidence/story-6-1-transport-20261102/ac2-pipeline-scope-20261104/commands.json) adds safe-denial response-header, selected SDK-log, server-activity and HTTP metric-category comparisons, plus controls against ambient/nested observation substitution. Its mapped Debug build has zero warnings/errors; five non-Dapr methods passed with zero failures/errors/skips/not-run. The real-owner method was excluded and cross-Tenant observability remains unrun. Timing-category metric equality supplies no response-time side-channel guarantee; the test plan leaves tolerance unspecified.

Parent independently verified native XML, current test assembly, all four source/raw-copy changes and the complete 16,666-input manifest `38344f0b6f1f0ef44396e22c7fca34d7a5ac3f951bd3a7eac3745cdd3d9131c8`. [Preparation and validation receipts](evidence/story-6-1-resume-20261104/focused-checkpoint-validation.json) retain the current limits. No fresh distributed runtime started: the approved canonical handoff requires user-confirmed quiet runtime coordination after earlier external-resource changes. Missing real-owner/cross-Tenant and timing evidence, a fresh full aggregate/regressions and remaining workflow review keep Story 6.1 in progress.

### Passing functional qualification and privacy continuation — 2026-11-04

The fresh [live-63 aggregate](evidence/story-6-1-transport-20261102/live-63/run.json) passes all eleven distributed cases across independent duplicate/full-replay/main providers at source manifest `1f4e55a334e7949abdfb78b266d9cca57513641dc2efd83e2443d594baa291aa`. Complete Integration passes 196/196 and Server 1,347/1,347, with zero failures/errors/skips/not-run. Every group's exact owned cleanup, private removal and external preservation pass. [Independent parent validation](evidence/story-6-1-transport-20261102/live-63/parent-validation.json) verifies the actual native nodes, strict source/provider/matrix joins and fresh absence of all twelve captured containers. Runtime startup/catalog/mTLS prerequisites are now functionally verified at this source.

The user approved whole-chain internal log/trace privacy and explicitly accepted coarse aggregate metrics' count-inference limit, as recorded beside AC2 above. [Persisted native evidence](evidence/story-6-1-transport-20261102/query-diagnostics-design-20261104/parent-persisted-traces.json) confirms a 9/4/4 native span-count difference in the real-owner component test despite its passing outer-boundary comparison. The isolated SDK privacy repair and actual provider/exporter tests remain in progress; numeric response-time tolerance remains unaccepted. The passing functional baseline does not accept these later changes or complete AC2. Story/spec/sprint remain in progress pending remaining verification and workflow review.

### Privacy implementation and timing decision — 2026-11-04

The isolated SDK privacy implementation now suppresses identifying query log/trace subtrees and preserves a uniform metadata-only boundary event, ordinary diagnostics and durable audit. SDK privacy controls pass 26/26, host-security controls 46/46, observer controls 6/6 and launcher controls 117/117. [The actual real-owner diagnostic](evidence/story-6-1-transport-20261102/real-owner-diagnostic-20261104/sanitized-observations.json) passes 1/1 with fifteen observations, zero emitted service spans and independently rechecked covered console silence across all six native peers. Exact owned cleanup and external preservation pass. It is intentionally non-qualifying.

Live 64 passed the eleven native cases and Integration 196/196 but Server failed 1/1,347 because a test observer serialized reflection metadata during setup. [Its sanitized failure result](evidence/story-6-1-transport-20261102/live-64/sanitized-failure-result.json) and private artifact custody remain retained; the observer is repaired. The [current source checkpoint](evidence/story-6-1-transport-20261102/query-final-privacy-checkpoint-20261104/commands.json) binds manifest `045be45a8105a12286ba44de95013c2776d6196a7af43e51116b5d02a5268a03`. No final aggregate at that source is accepted.

The component sample medians are 11.70ms denied, 4.66ms absent and 4.61ms foreign-only; four samples per outcome are diagnostic only. The user has been asked to choose between a proposed 50ms minimum with residual slow/fault variation, explicit acceptance of current latency variation, or a stronger timing-privacy design. No choice is approved yet. Live 65 is held with no owned runtime active; remaining qualification and workflow review follow the user's source-affecting timing decision. Story/spec/sprint remain in progress.

### Final verified implementation and review — 2026-11-04

The user-approved 50 ms remaining-only public query 404 gate is applied in the isolated source checkout, alongside the whole-chain privacy implementation and reviewed list/open repairs. The [final source-bound aggregate](evidence/story-6-1-transport-20261102/live-69/run.json) passes all eleven distributed cases across three fresh providers, Integration 196/196 and Server 1,368/1,368, with no failures/errors/skips/not-run. [Independent acceptance](evidence/story-6-1-transport-20261102/live-69/parent-validation.json) matches all 16,697 inputs at manifest `a7ce136a2d29382e591dcd3535f79d6d4483831baab47c2b9ca4e17128ad029d`, verifies native XML/matrix/quarantine/audit/restart controls, all twelve captured containers and owned processes absent, original exact cleanup/private removal/external preservation and released original admission.

[Current timing evidence](evidence/story-6-1-transport-20261102/live-69/final-verification-supplement.json) records 39 actual distributed 404 header samples, all at least 50 ms (minimum 339.8733 ms, median 362.4943 ms, maximum 3390.9522 ms); component gateway samples span 50.9962–54.2612 ms. The floor protects faster paths but does not equalize this slower Development routing path. All six covered native consoles are silent across fifteen real-owner windows; their positively bound persisted dashboard has zero exported spans, linked logs or metric exemplars for those traces, positive ordinary native/server controls and fourteen positive protected POST metric dimensions without exemplars. Aggregate count inference and recorded fault/load/observation limits remain explicit.

[Three-layer review resolution](evidence/story-6-1-transport-20261102/review-20261104/resolution.json) fixes fifteen findings in twelve small patch groups, refutes two claims and retains two distinct deferred risks. Original failed runs remain historical at their actual source, including live67’s corrected invalid foreign fixture and live68’s pre-read catalog failure with separate exact recovery. The final fully passing run alone supplies acceptance. All execution tasks and workflow presentation are complete; story and sprint tracking are in review. Code remains under `/tmp/story61-isolated-20261103-r5tqvlm3/projects`; no source copyback, staging, commit or push occurred. Global NOT_READY, publication/production enrollment, 6.7 migration, 8.9 load, 8.11 release and existing DW-1/6.5 gates remain distinct.

### G1 review patches and current verification — 2026-11-05

All fourteen open G1 code/test patches are implemented, including the accepted null-only projection fold, Partial reads after confirmed Folder replacement, `ResolveNeedsAttention` quarantine recovery, explicit rejection schemas, shared payload JSON options, source-only worker-test gating and AppHost cleanup/layout probes. The two Projects evidence controls now use the current SDK native metadata-reader interface without changing their assertions. A topology control proves that replaying the removed later bootstrap assignments leaves all 47 Tenants and 58 gateway environment entries equal.

The [current packet](evidence/story-6-1-review-patches-20261105/README.md) retains exact commands, native XML, full dependency commits, original failed attempts and separate recovery. Projects HEAD is `a6c415f38053cbdb257b12d196be0bfa70aadc2d` plus these uncommitted changes. Current local manifest is `2e4b2076406fe46f47b27cea844d71353e62a6eccadd81c01171267145a6e5d7`. Mapped Debug builds have zero warnings/errors. Local Integration passes **196/196** and Server **1,328/1,328**, with zero failures/errors/skips/not-run; required live selectors are explicitly excluded and supply no AC7 coverage. Focused projection/read/catalog controls pass **51/7/12**, SDK evidence controls **83/83**, launcher controls **119/119**, and both MSBuild layout probes pass.

The fresh distributed attempt at manifest `32ac7ec2d471f0c68a0d871aa42541c35ae3e1121917b235e75d064d9592796e` failed its native duplicate method **0 passed, 1 failed, 0 skipped/errors** before Project reads: `Real Tenants hosted bootstrap has no original submission receipt`; EventStore logged `RouteCatalogUnavailableException`. All owned runtime/private resources were removed and external preservation passes. The subsequent source-only topology control proves environment equivalence; no further runtime attempt or sibling repair occurred. Package-mode Release restore also fails with **3 NU1109 errors**: Tenants.Client 7.0.0 requires EventStore Client/Contracts >=4.0.4 while Projects centrally selects 3.133.0. No dependency override was applied.

Patch implementation is complete, while current AC7 acceptance, package-mode build confirmation and subsequent workflow review remain incomplete. Story and sprint status remain **in-progress**; prior passing runtime evidence stays bound to its historical source.

## Change Log

| Date | Change |
| --- | --- |
| 2026-07-17 | Created Story 6.1 comprehensive implementation context and set status to ready-for-dev. |
| 2026-07-17 | Approved course correction set the story to blocked, externalized platform enablement as 6.1-P0 through 6.1-P4, and removed cross-repository gate resolution from Story 6.1 tasks. |
| 2026-08-01 | Approved Correct Course proposal inserted 6.1-P1R after post-P1 dependency drift and retained Story 6.1 as blocked through P4 plus a passing readiness rerun. |
| 2026-11-02 | Applied approved E1–E7 entry correction; focused readiness permits finishing/review of existing implementation. Qualification remains open, P4 superseded; no feature acceptance. |
| 2026-11-02 | Started supported read implementation review, added quarantine/event-catalog safeguards, corrected the isolated fixture, and retained failed verification evidence; story remains in progress. |
| 2026-11-02 | Prepared the isolated launcher's Projects source mapping, reran focused and distributed checks, and retained native XML and cleanup evidence; route catalog and sibling source blockers keep the story in progress. |
| 2026-11-02 | Recorded a failed route-catalog prototype diagnostic and removed its out-of-scope edits; authenticated Dapr caller identity and enrollment remain external blockers. |
| 2026-11-02 | Corrected page/row snapshot semantics and completed authority-event documentation; retained passing focused fallback evidence and current source-preparation/build blockers. |
| 2026-11-04 | Applied the approved denial-privacy/timing policies, completed reviewed repairs and final same-source acceptance of 11 distributed cases, 196 Integration tests and 1,368 Server tests with independent telemetry/cleanup proof; source remains isolated and later release gates unchanged. |
| 2026-11-05 | Code review of chunk G1 (production `src/`, 47a1ffd..bc545fd): 3 decisions resolved, 14 patches left as action items, 1 deferred, 24 rejected. The high-severity fold-time `IsValid` Tenant-list blackout is open, so status returns to in-progress. Chunks G2–G4 are still unreviewed. |
| 2026-11-05 | Implemented fourteen G1 review patches and the current SDK evidence-fixture adaptation; local Integration 196/196 and Server 1,328/1,328 pass with live cases excluded. Fresh distributed bootstrap/catalog and package NU1109 blockers keep Story 6.1 in progress. |


### Bootstrap authority follow-up and required replan — 2026-11-05

The fixture-only issuer now grants the explicit `global-administrators:bootstrap` permission. Its new exact-permission assertion failed before the repair and passes after it; all 119 launcher controls pass. A fresh isolated Integration build passes with zero warnings/errors. The native duplicate method now observes an original bootstrap receipt but fails `Hosted bootstrap receipt binding failed`: 0 passed, 1 failed, 0 skipped/errors/not-run. No later group or full regression ran. All owned resources/private material were removed, unrelated resources preserved, and final container/AppHost absence verified.

Further inspection identifies a broader public-fixture scope mismatch: bootstrap/Administrator credentials carry reserved storage Tenant grants, seed requests submit public `system`, and status reads lack explicit scope headers. Aligning these boundaries with current platform contracts must retain genuine persisted `system` history and exact authority checks. The CLR command-name alias remains valid. No receipt check was weakened and no further fixture code was changed after identifying this expanded scope.

Independent static reviews have now covered G2 Server/Contracts tests, G3 Integration tests and G4 launcher changes from `47a1ffd02cb3e1cde80bd7697ac1b46fdefda8cc` through the current worktree. G3/G4 are clean. G2 confirms a denial-observer export-completion race requiring a post-provider completion gate and bounded blocked-export/suppressed-root controls; nine additional low-impact coverage suggestions were rejected with evidence. This completes the previously missing static reviews, not current acceptance.

The exact CI-mode Release Server.Tests command was rerun and still fails restore with 3 NU1109 errors: Tenants.Client 7.0.0 requires EventStore Client/Contracts >=4.0.4 while Projects selects 3.133.0. The source Debug build is focused fallback evidence only. Story and sprint remain in-progress. The Build workflow returned its spec to draft for the expanded fixture/observer repair before further coding. No pins, sibling sources, staging, commits or pushes changed.

See [the new packet](evidence/story-6-1-bootstrap-authority-20261105/README.md), including exact commands, native XML, full tested commits, manifest, cleanup and complete [review triage](evidence/story-6-1-bootstrap-authority-20261105/review-triage.md), and [the draft replan](spec-6-1-list-and-open-projects-through-supported-authenticated-paths-12.md).


### Public fixture scope and observer completion follow-up — 2026-11-05

The [approved spec continuation](spec-6-1-list-and-open-projects-through-supported-authenticated-paths-12.md) now implements platform-only bootstrap credentials, finite public Administrator permissions, owner-declared Tenants seed scope and explicit status headers. Historical `system` envelopes/events remain unchanged. The denial observer installs its completion gate after the real gateway provider initializes. A blocked exporter cannot produce an early empty snapshot, a suppressed root completes empty, and the retained unbound-span control now exercises the installed provider/gate and positively observes its nested export.

The [new packet](evidence/story-6-1-public-scope-observer-20261105/README.md) records signed issuer 1/1, launcher 119/119, mapped Debug builds with zero warnings/errors, local Integration **208/208** (95 evidence controls, including 12 new public scope/grant cases), final focused gateway **8/8**, and final local Server **1,330/1,330**. All local totals have zero failures/errors/skips/not-run; the existing live selectors remain excluded and supply no AC7 acceptance. Initial analyzer failure and corrected commands are retained separately.

Fresh native duplicate qualification now validates the original bootstrap receipt and reads actual durable `Completed` status through HTTP 200 with explicit `platform:global-administrators`. It then stops with `Owner capability required: the EventStore bound-workload SDK does not support platform:global-administrators source reads.` Native totals are **0 passed, 1 failed, 0 errors/skipped/not-run**. The progress artifact explicitly marks persisted bootstrap source verification false. No Project seed/read, later provider group or full runtime regression ran; no assertion was weakened and no invalid public reserved-scope request or custom transport workaround was issued. The [owner handoff](evidence/story-6-1-public-scope-observer-20261105/owner-handoff.md) identifies the exact missing SDK behavior.

The native source manifest is `41df764d7b1a222455c81f74258f5b819021a87604fd0fdb5928f8aa30c723e0` at Projects `a6c415f38053cbdb257b12d196be0bfa70aadc2d` plus preserved/current uncommitted changes; full direct dependency commits and native XML remain retained. Source matched at native cleanup. The exact AppHost, all four captured containers, six sidecars and private material were removed; all five preexisting containers were preserved. One subsequent Server-test-only negative-control correction yields final local manifest `a56fe2d12ab5c1077217ebb02e1f67a9c879e3e73fce6e06098dd622b609099b`; its sole-path difference and fresh 8/8 plus 1,330/1,330 Server proof are explicit. Final source is not claimed equal to native source; Integration/runtime source is unchanged.

The packet maps actual evidence to all seven acceptance criteria. Global-administrator persisted source proof and the fresh distributed Project matrix remain incomplete; the known package-mode restore NU1109 still prevents package-build confirmation at the retained exact prior command/output. Parent workflow review remains pending this handoff. Story and sprint remain **in-progress**, without dependency, sibling-source or Git mutations.


### Parent review closure and post-correction verification — 2026-11-05

All three configured dispatch review layers completed. Four direct corrections are resolved: checkout-visible byte-identical command outputs, accurate early progress attribution, an actual HTTP gateway positive exporter control, and Folder replacement in separate persisted deliveries. Five low-impact suggestions and one false finding were rejected with evidence; nothing was deferred. The full triage is recorded in the approved spec.

Parent local verification passes **208/208 Integration** and **1,331/1,331 Server**, with no failures/errors/skips/not-run and the documented live selectors excluded. Final manifest `0454152151df5bf58fa74343bc59a0d660bb4c4df56dc96cdc25ddf3ad82943d` remained unchanged during both runs. The new [parent receipts](evidence/story-6-1-public-scope-observer-20261105/parent-verification/results.json) and XML preserve exact commands, totals and assembly hashes. All 17 retained copied outputs validate byte-for-byte and remain checkout-visible. The owned source-mapping workspace is removed; native cleanup and preservation receipts remain intact.

Current authenticated persisted source proof and AC7 remain incomplete at the documented owner SDK capability; package-mode NU1109 remains separately recorded. Native evidence stays bound to its earlier source, with all later differences explicit. Story/sprint remain in-progress; no feature completion, dependency change, staging, commit or push is claimed.
