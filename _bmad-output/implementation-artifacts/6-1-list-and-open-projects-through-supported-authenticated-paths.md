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

## Tasks / Subtasks

- [x] Correct successful list-page enumeration snapshots independently of row readiness; cover mixed/empty/paged/quarantine results while retaining per-row evidence and unavailable/denial paths. Focused fallback evidence is recorded below; authenticated feature acceptance remains incomplete.
- [ ] Finish/review existing supported contracts, projection/query handlers and composition against AC1–3/5–6. Preserve current Folder-read authority before paging, Safe Metadata defaults, distinct descriptive inspection permission and durable metadata-only audit; audit persistence failure denies disclosure.
- [ ] Classify the complete event catalog explicitly: known relevant events apply; explicitly catalogued harmless additive events are deterministic no-ops proven by compatibility tests. Unknown potentially relevant, corrupt or undecodable events fail projection/rebuild honestly and yield Unavailable; neither blanket skip nor blanket rejection of harmless additions satisfies NFR-10. Verify additive serialization and legacy behavior under AC4. Keep public routing intact and hand the existing comparator to 6.7 for exhaustive migration qualification.
- [ ] Inspect retained receipts and run the existing focused/persisted lane for missing AC7 evidence; record actual commands, commit references, native XML, totals and result report. Do not infer feature acceptance from this readiness correction.

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
