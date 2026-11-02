---
title: 'Story 6.1: List and open Projects through supported authenticated paths'
type: 'feature'
created: '2026-07-17T11:38:50+02:00'
status: 'ready-for-dev'
blocked_by: []
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-state-instructions.md'
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-01.md'
  - '{project-root}/_bmad-output/implementation-artifacts/6-1-list-and-open-projects-through-supported-authenticated-paths.md'
warnings: [oversized]
---

<intent-contract>

## Intent

**Problem:** Project Users on Chatbot and Tenant-role actors on permitted Web/CLI/MCP surfaces need accepted authenticated list/open. The supported implementation and local persisted lane exist; the inherited publication/qualification chain prevents finishing/review on feature evidence.

**Approach:** Finish/review the existing supported query/projection implementation using the two capability prerequisites and the current story acceptance. Retain authenticated Tenant/original actor/workload/delegation, current Folder filtering before paging, safe denial, AD-32, scoped cursors and durable inspection audit. Keep legacy routing and additive compatibility; exhaustive migration equivalence belongs to 6.7.

## Boundaries & Constraints

**Always:** Derive Tenant, original actor, workload, delegation, scopes, and audience from immutable authenticated context; reauthorize before protected validation; filter unauthorized rows before paging; use `IReadModelStore`, `ReadModelWritePolicy`, `IQueryCursorCodec`, and `QueryCursorScope`; preserve existing event history and legacy routes; keep one public C# type per file; assert persisted end-state and metadata-only responses.

**Block If:** The selected supported read substrate or actual fail-closed actor/Folder authority path is missing or fails, required audit cannot persist, or an unauthorized sibling edit/public-route switch is needed. Publication, historical P1R usability, production enrollment, P4 and independent READY are not feature entry requirements under the approved 2026-11-02 E1–E7 correction.

**Never:** Do not hand-roll Dapr persistence, cursors, query routing, authentication context, or projection actors; do not extend the legacy journal as the supported store; do not expose payload-bearing sibling content, resolution candidates, or denial detail; do not write domain state, append events, create tasks, rewrite history, remove legacy routing, or fabricate evidence.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Authorized list | Current read models; valid Tenant, actor, workload, delegation and scope; optional lifecycle/page cursor | Visible Active and Archived rows in stable order; default 50, cap 200; opaque caller/filter-bound cursor; AD-32 state and recovery evidence | Structurally invalid paging/filter/cursor returns metadata-only `400` after authorization where validation could leak |
| Authorized open | Well-formed visible Project identity | Metadata, lifecycle, typed setup summary, safe reference summary, component freshness and recovery evidence; no resolution result | Stale/incomplete evidence returns honest `Partial` or `Unavailable`; Archived remains inspectable but unusable for context |
| Denied or absent | Denied, cross-Tenant, or nonexistent well-formed Project | No protected snapshot and no unauthorized list row | Same safe `404` status, headers, body shape, timing category, logs, and telemetry |
| Rebuild/fault | Duplicate delivery, restart, rebuild, unavailable store, or unknown relevant event | Replay and incremental paths converge, or state is honestly unavailable; no writes from queries | Unknown/corrupt relevant events fail projection rather than being silently skipped |

</intent-contract>

## Code Map

- `src/Hexalith.Projects.Server/Queries/ListProjectsQueryHandler.cs`, `GetProjectQueryHandler.cs`, `ProjectReadQueryExecutor.cs`, `ProjectReadQueryAdmission.cs`, `ProjectReadAudit.cs` -- existing authenticated supported read policy and output.
- `src/Hexalith.Projects.Server/Projections/ProjectReads/ProjectReadProjectionHandler.cs` -- existing incremental/rebuild persisted models over SDK stores/batches.
- `src/Hexalith.Projects.Testing/Reads/ProjectReadShadowComparator.cs` -- existing additive-compatibility regression; full migration proof is 6.7.
- `tests/Hexalith.Projects.Server.Tests/Queries/SupportedProjectReadTests*.cs`, `SupportedProjectReadHttpTests*.cs` -- focused behavior and adapter regressions.
- `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadDistributedTests.cs` and fixture/evidence controls; `tools/qualification/run_supported_project_reads.py` -- existing real gateway/owner/persisted/restart/replay lane.

## Tasks & Acceptance

**Execution:**
- Finish/review supported list/open against the [current story](6-1-list-and-open-projects-through-supported-authenticated-paths.md), including its metadata allowlist, query ordering, quarantine and inspection rules.
- Classify the complete event catalog explicitly: known relevant events apply; explicitly catalogued harmless additive events are deterministic no-ops proven by compatibility tests. Unknown potentially relevant, corrupt or undecodable events fail projection/rebuild honestly and yield Unavailable; neither blanket skip nor blanket rejection of harmless additions satisfies NFR-10.
- Preserve additive contracts and retained legacy routing; use the existing comparator as a regression and hand exhaustive output/key/watermark/cursor/normalization equivalence to 6.7.
- Inspect available recorded receipts, run relevant missing verification and record tested Projects/direct dependency commits, exact commands, native XML, pass/fail/skip totals and the real result report. Do not require publication or another immutable hash inventory.

**Acceptance Criteria:**
- Given a real authenticated authorized caller, when list/open traverses the supported gateway, SDK and owner handlers, then current Folder authority filters before paging; list uses default 50/cap 200, deterministic ordering and scoped platform cursors; Tenant-role output is Safe Metadata unless inspection permission and durable metadata-only audit authorize disclosure.
- Given denied, absent, cross-Tenant, wrong audience/scope, missing delegation, stale authority or revocation, when reads execute, then safe denial fails closed without protected leakage; authorized structural invalid input follows the existing metadata-only 400 policy.
- Given current or degraded evidence, when output is built, then AD-32 distinguishes current authorized enumeration/per-entry state, required Project/Folder/Setup/authority for usable open and explicit optional omissions. Unknown evidence never becomes Complete; folderless legacy data stays quarantined under the explicit limits below, and Archived inspection never admits context.
- Given success, denial, retry, replay, duplicate delivery, Projects restart or fault, when persisted before/after state is compared, then queries append no domain event and mutate no Project/sibling/task/resolution state; required metadata-only security/inspection audit is checked separately and disclosure fails if audit cannot persist.
- Given feature completion is proposed, when individual focused test-project and existing distributed Development results are inspected, then actual XML/results cover the matrix and story ACs at recorded commits with no invented pass; additive compatibility and intact legacy routing hold. Exhaustive migration equivalence and reversible public-route activation remain required in 6.7.

## Spec Change Log

- 2026-07-17: Approved Sprint Change Proposal externalized platform enablement as 6.1-P0 through 6.1-P4 and retained `blocked` until P4 acceptance plus a passing readiness rerun.
- 2026-08-01: Approved Correct Course proposal added 6.1-P1R after post-P1 dependency drift; Story 6.1 remains blocked until P0, P1, P1R, P2, P3, and P4 pass.

## Review Triage Log

## Design Notes

Focused readiness is recorded in the current story on 2026-11-02 against the supported handlers, real authority composition, persisted fixtures and executable launcher. Status is ready-for-dev to finish/review, not done. Historical P1/P1R decisions, current-usability false and global NOT_READY remain. P0/P1R-remediation/P2/P3 stay open under Epic 8; P4 is superseded. Dated local sub-specs and frozen proof intent are preserved.

### Exact list/open shape and quarantine restrictions

- List page (`ProjectListPage`): only `items`, protected `cursor`, `hasMore`, and nested `snapshot`. Each `ProjectListRow` has only `projectId`, authorized `name` (omitted for Safe Metadata callers), `lifecycleState`, `projectFolderAvailability`, and its own nested `snapshot`. Each `AdmissionSnapshot` contains `responseState`, `asOf`, authorized aggregate-sequence `projectVersion`, `components`, and `recoveryActions`; these fields are not flattened onto the page or row. The page snapshot describes enumeration, while each row snapshot describes that Project. Do not echo Tenant or actor identity.
- Open Project (`GetProjectResponse`): only `projectId`, authorized `name`, validated authorized `description`, `lifecycleState`, `createdAt`, `updatedAt`, authorized `setup`/`setupMetadata`, `references`, and nested `snapshot` with the same `AdmissionSnapshot` fields. `projectVersion` stays inside `snapshot`. Do not add audit, task, Conversation, resolution, or sibling payload fields.

Legacy Active folderless Projects remain quarantined and excluded from ordinary list, resolution, context and Conversation-start. FR-22 quarantine inventory is Tenant Project Administrator-only: Safe Metadata plus the narrowly scoped exception for Project name and any pre-v1 Folder or creation receipt, never Setup or titles, with one metadata-only FR-21 inspection event per Project returned; role-gated until the A-5 inspection permission exists, then gated on it. Binding requires FR-8 Preview and confirmation; inventory inspection never admits context. Preserve the story's typed Setup/reference allowlists and existing security/payload restrictions. These rules describe required behavior; the entry correction adds no runtime capability or acceptance claim.

## Verification

```bash
MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 DOTNET_PROCESSOR_COUNT=2 python3 tools/qualification/run_supported_project_reads.py --regressions --timeout 900
```

This existing lane builds Debug/source test projects individually, invokes their assemblies and retains native XML, persisted matrices and cleanup reports. Use a fresh evidence destination. Inspect existing receipts first and run relevant missing coverage; package-mode qualification, production enrollment, 8.9 load evidence and release acceptance remain separate. Feature acceptance records actual commits/commands/results once; unrelated later movement does not reopen it.

## Auto Run Result

Status: blocked
Blocking condition: spec failed ready-for-development standard

Failing criterion: **Sufficient**. Known dependency and implementation gaps remain unresolved: P1R baseline drift; incomplete dual-principal query identity; indistinguishable safe-denial and supported global-position-watermark capability; production authentication approval; the supported G-4 persisted runner, published consumer pin, and machine-checkable owner acceptance record; and the P4 entry-gate record. These gaps cannot be repaired within Story 6.1 without separately authorized platform, Builds, or sibling-repository work.

### Supersession — 2026-11-02

The preceding Auto Run Result is the historical old-gate assessment, not the current scheduling disposition. Jerome approved E1–E7 of [the November 1 proposal](../planning-artifacts/sprint-change-proposal-2026-11-01.md); it replaces the P0–P4 development ceremony with concrete supported-read/authority capability readiness. The focused check in the current story found the existing seams and commands, so this spec is ready-for-dev for finishing/review. Historical pass counts are unaccepted feature evidence until available receipts and coverage are checked. No live qualification or list/open feature change was performed by the correction.
