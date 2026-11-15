---
title: 'Schedule Story 6.8 before Story 6.6 and move the CLI read surface to Hexalith.McpCli'
date: '2026-11-15'
status: 'approved'
approved_date: '2026-11-15'
approval_source: 'Jerome selected "Approve trimmed (Recommended)" on 2026-11-15 after an options review; trims T1-T4 are recorded in §7'
change_scope: 'moderate: guarded scheduling exception, verification lane, story rewrites, sibling-repo authorization and deferred-work routing; no product code'
mode: 'batch'
trigger: '/bmad-build 6.6 stopped at planning on 2026-11-15. The guard pins 6.6-6.8 at backlog, the story names an undelivered runner, the 6.8 Contracts split has not landed, and the story still targets a frozen module CLI.'
decisions: 'D1-D6 frozen by Jerome on 2026-11-15 in spec-6-6-inspect-projects-through-an-authenticated-cli-read-surface.md; this proposal turns them into planning changes and does not reopen them'
precedents:
  - 'sprint-change-proposal-2026-11-01.md (E1 individual Debug test-project lane; E5 6.5 dependency guard)'
  - 'sprint-change-proposal-2026-11-15-close-story-6-5.md (fixture independence, DW routing)'
---

# Proposal: land Story 6.8, then resume Story 6.6 on Hexalith.McpCli

## 1. Issue summary

On 2026-11-15, `/bmad-build 6.6` stopped at planning. The draft spec ([spec-6-6](../implementation-artifacts/spec-6-6-inspect-projects-through-an-authenticated-cli-read-surface.md)) records Jerome's decisions D1–D6, but five planning facts still block the story:

1. **The guard forbids it.** `tools/planning/validate_production_authority.py:80-82` pins 6.6, 6.7 and 6.8 at `backlog`. `tests/tools/test_production_authority_guard.py:668-679` uses "6.6 → ready-for-dev" as its example of a forbidden transition.
2. **The prerequisite has not landed.** Story 6.8 (the UI.Contracts split, DW-55) is in backlog. `Hexalith.Projects.Contracts.csproj:13-47` still references FrontComposer Contracts, Shell and SourceTools, plus `Microsoft.AspNetCore.App`, Fluxor and Fluent UI. Any CLI consumer would inherit Blazor through those references.
3. **The lane does not exist.** Story 6.6 verifies with `hexalith-module test --profile cli-reads` (epics.md:1517), and that runner is not delivered (6.1-P0 stages 2–7 are open). The individual Debug test-project exception (E1 of the November 1 proposal) does not cover 6.6.
4. **The story text contradicts the decisions:**
   - It describes a module CLI with `list`/`describe`/`inspect`/`trace`/`validate`. D3 picks the McpCli host and D5 sets the operation names.
   - It enables confirmation "after the accepted A-7 claim". McpCli cannot confirm (epics.md:59, AD-29).
   - It requires a non-zero exit for stale models. D6 keeps McpCli exit codes unchanged.
5. **Provenance and accuracy gaps.** No record exists for the "Approved McpCli course correction (2026-09-27)". DW-81 cites an old McpCli gitlink. The Story 6.8 text cites the wrong csproj lines and omits the release tooling that currently locks the dependency leak in.

**Category:** misunderstanding of original requirements (host and exit semantics), plus process gaps (guard, lane, provenance). The PRD needs no change.

### Evidence gathered on 2026-11-15

| Fact | Source |
| --- | --- |
| Guard pins 6-6/6-7/6-8 at `backlog`; 6.5 uses the `STORY_6_5_KEY` + swap-loop + DW-1 dependency pattern | `validate_production_authority.py:25-31`, `:80-82`, `:279-283`, `:526-546` |
| Forbidden-transition example uses 6.6 | `test_production_authority_guard.py:668-679` |
| McpCli gitlink is `23072f04` and targets `net11.0` | `git ls-tree HEAD references/Hexalith.McpCli`; `references/Hexalith.McpCli/Directory.Build.props:33` (moved from `a92a2969` via `63df6d2b` in `b756d01a`/`fc58cefd`) |
| G-6 passed at `b756d01a` (run 43692737105, McpCli `63df6d2b`, already net11.0), so `NU1202` is fixed. It failed again at `fc58cefd` (runs 43694739240 and 43697046014): the step "Restore effective G-6 package graphs" exits 2 while `Hexalith.Builds.Module.Cli prepare-build` runs for Conversations, with empty stderr. The Conversations pointer is unchanged (`c34dc3c1`); the Builds pointer moved `99f7c860` → `7cd0c17a`. | CI job logs 132760982795 and run 43692737105 jobs; `git diff b756d01a fc58cefd -- references` |
| `ci / build-and-test` is red on main at `b756d01a` and `fc58cefd`: "Package Hexalith.Projects dependency graph differs from the manifest; wrong_versions=[FrontComposer.Contracts, FrontComposer.Contracts.UI, FrontComposer.Shell]". The committed Builds pointers `99f7c860` and `7cd0c17a` select FrontComposer 6.0.0; `tools/release-packages.json` pins 5.0.0. The build last passed at `eaa95558` with Builds at FrontComposer 5.0.0. | CI job 132760982865; `references/Hexalith.Builds/Props/Directory.Packages.props:155` |
| McpCli enrollment needs a NuGet package asset: `ModuleAssemblyManifest.targets` matches `NuGetPackageId`. McpCli also validates locally in Release with packaged dependencies, and enrollment needs `tools/dependency-policy.json`, `tools/inventory-coverage/v1/<module>-approved.json` and conformance vectors. | `references/Hexalith.McpCli/src/Hexalith.McpCli/Build/ModuleAssemblyManifest.targets`; McpCli `AGENTS.md` Policy and Running sections |
| The central catalog already pins `Hexalith.Projects.Contracts` at `$(HexalithProjectsVersion)` = 1.2.4 (the published, dependency-heavy, undecorated package) and `Hexalith.McpCli.Abstractions` at `$(HexalithMcpCliVersion)` | `references/Hexalith.Builds/Props/Directory.Packages.props:167`, `:247`, `:263` |
| The AI.Tools migration plan pins every inventory to McpCli `c694bff5`. Re-pinning an indexed inventory requires a new `plan_version`. | `references/Hexalith.AI.Tools/hexalith-mcpcli-migration-instructions.md` |
| No `sprint-change-proposal-2026-09-27*` exists in Projects, McpCli or AI.Tools. The notes arrived in `b42beb3a` (2026-09-28). | `deferred-work.md:838-840`; repository search |
| Projects has no `HexalithMcpCliRoot` property; Contracts uses `UseHexalithProjectReferences`, not per-module `*FromSource` switches | `Directory.Build.props:17-32`; `Hexalith.Projects.Contracts.csproj:13-42` |

## 2. Impact analysis

- **Epic 6:**
  - 6.8 becomes schedulable now.
  - 6.6 is rewritten for McpCli and becomes schedulable once 6.8 and DW-55 are done.
  - 6.7 stays pinned at `backlog`.
  - 6.1–6.5 stay `done`, and the epic stays `in-progress`.
- **Epic 8:** none of its stories is edited. 8.4 and 8.5 still carry the old "CLI/MCP confirmation after A-7" wording, which the AD-33 amendment below supersedes. DW-84 routes their text to the time they are specified. 6.8 remains their prerequisite.
- **PRD:** no requirement change. The prd.md:10 note gains its approval of record (E9). §9 A-7 already keeps McpCli confirmation disabled under Platform AD-14, which matches the rewritten 6.6.
- **Architecture:**
  - AD-29 is unchanged.
  - AD-33's archive/restore row and its MCP footnote (Spine :375, formerly :342) are amended so that McpCli never confirms (E5). This settles the Spine-vs-AD-29 half of the untracked item at deferred-work.md:838-840.
  - The Spine :32 note gains its approval of record.
- **UX:** UX-DR19 maps the read group to the D5 operation names and drops "CLI confirmation after A-7". The UX spec is not edited: its CLI navigation list and Journey 4 (the MCP confirmation gate) go to DW-84.
- **Tracking and tooling:**
  - The guard and its tests change together.
  - `sprint-status.yaml` gets hand edits and no status move.
  - Two traceability-matrix rows are updated in the YAML and in the Markdown view.
  - The deferred-work ledger gets three new entries (DW-82 to DW-84), DW-81 closed, a corrected location for DW-55, and one untracked item resolved.
- **Sibling repositories:**
  - McpCli changes are authorized for 6.6 only (E8), each validated by McpCli's own gates.
  - The AI.Tools plan re-pin is routed to DW-82.
  - No submodule pointer moves.
- **Code:** none in this correction. 6.8 and 6.6 own the code.

## 3. Recommended approach

**Direct adjustment.** Keep Epic 6's shape. Schedule 6.8 → 6.6 behind a guard that fails closed. Rewrite 6.6 for the McpCli host. Correct 6.8. Route publication, CI hygiene and Epic 8 text to owned deferred-work entries, following the close-on-own-ACs rule.

- **Effort:** low for the correction itself (documents, guard and tests). The two stories carry the real work: 6.8 is S and 6.6 is M.
- **Risk:** medium, from three sources:
  - **Package-only enrollment.** McpCli consumes Contracts only as packages, so 6.6 must prove enrollment against a locally packed `Hexalith.Projects.Contracts`. A published, slim, decorated package depends on Projects main CI being green, which DW-83 currently blocks. That gap becomes DW-82 instead of a 6.6 gate.
  - **Wire vocabulary.** The 6.8 split touches the enums used by events and responses. E7 requires their namespaces and wire names to stay unchanged.
  - **Red main CI.** 6.8 starts from a red `build-and-test` (DW-83). The done rule allows an equivalent recorded local run, but the regression must not be hidden.
- **Rejected alternatives:**
  - *Schedule 6.6 and 6.8 together.* D1 forbids it.
  - *Wait for the common runner.* That couples 6.6 to 6.1-P0, which E1 already rejected for 6.1 and 6.5.
  - *Enroll Projects Contracts in McpCli from source.* McpCli's manifest and dependency policy reject source helpers, and inventory G1 requires the released closure.
  - *Make 6.6 wait for the published package.* That contradicts the done rule, under which publication is a separate obligation.

## 4. Detailed change proposals

### E1. Scheduling guard: `tools/planning/validate_production_authority.py`

Add the keys and the allowed set next to the 6.5 entry (around :25-31):

```python
STORY_6_6_KEY = "6-6-inspect-projects-through-an-authenticated-cli-read-surface"
STORY_6_8_KEY = "6-8-split-hexalith-projects-ui-contracts-from-contracts"
# Approved 2026-11-15 scheduling exception (D1/D2): 6.8 may progress; 6.6 may leave backlog
# only after 6.8 and DW-55 are done. Grants no readiness, P1R, 6.7, Epic 7/8 or release acceptance.
SCHEDULED_STORY_STATUSES = {"backlog", "blocked", "ready-for-dev", "in-progress", "review", "done"}
```

Remove the 6-6 and 6-8 lines from `EPIC6_DEVELOPMENT_STATUSES` (:80, :82). Keep `6-7-…: "backlog"` at :81.

In `_validate_scheduling`, after the 6.4 block (:284-287), add the swap loop and call the new check next to the 6.5 check:

```python
    for key, story in ((STORY_6_8_KEY, "6.8"), (STORY_6_6_KEY, "6.6")):
        status = production.get(key)
        if status not in SCHEDULED_STORY_STATUSES:
            raise GuardViolation(f"Story {story} scheduled status is missing or invalid")
        expected[key] = status
    if deferred_work is not None:
        _validate_read_dependencies(sprint, deferred_work)
        _validate_cli_read_dependencies(sprint, deferred_work)
```

Add a new function after `_validate_read_dependencies` (:527-546):

```python
def _validate_cli_read_dependencies(sprint: dict[str, Any], deferred_work: str) -> None:
    """Keep the CLI read surface behind the landed Contracts split (2026-11-15 D1)."""

    development = sprint["development_status"]
    if development[STORY_6_6_KEY] == "backlog":
        return
    if development[STORY_6_8_KEY] != "done":
        raise GuardViolation("Story 6.6 progression requires done Story 6.8")
    if _deferred_status(deferred_work, 55) != "done":
        raise GuardViolation("Story 6.6 progression requires DW-55 done")
```

What this enforces:

- **6.8** may take any status in the set.
- **6.6** may leave `backlog` (including going to `blocked`) only when 6.8 is `done` and DW-55 is `done`.
- **6.7** stays `backlog`, and `readiness_provenance.current_result` must still be `NOT_READY`. Neither check changes.
- **DW-55 status format.** `_deferred_status` accepts a single canonical word, so DW-55 must close as `status: done`, with the date on a separate `closed:` line.
- **Every production entry point** (`--validate-index`, `--story-id`) supplies the ledger, so the check always runs there.

**Rationale:** this is the E5 pattern applied to D1/D2, failing closed in both directions.

### E2. Guard tests: `tests/tools/test_production_authority_guard.py` (same commit as E1)

- **Fixture independence** (E6 precedent). In `setUpClass`:
  - After the 6.1/6.5 reset at :92-99, reset the 6-6 and 6-8 lines to bare `backlog` lines.
  - Add the DW-55 heading (`### DW-55: Create the standalone Hexalith.Projects.UI.Contracts split story and land it before Stories 6.6, 8.4 and 8.5.`) to `deferred_sections` so DW-55 is seeded `open`.

  The tests then stay valid when the live file later records 6.8 or 6.6 progress.
- **Move the forbidden example.** In `test_unauthorized_closure_fails_closed` (:668-679), replace the 6-6 lines with the `6-7-cut-over-supported-reads-while-preserving-compatibility-and-rollback` line, and loop over every forward 6-7 status. The expected error stays "open boundary".
- **New cases:**
  - `test_contracts_split_story_can_progress_without_opening_other_gates`: every value in `SCHEDULED_STORY_STATUSES` passes for 6-8, on both the open and the accepted snapshot. 6-6 and 6-7 stay `backlog`, and readiness stays `NOT_READY`. It also asserts the exact set.
  - `test_cli_progression_requires_done_split_and_done_dw55`: for each non-`backlog` 6-6 status, 6-8 not `done` fails with "requires done Story 6.8". 6-8 `done` with DW-55 `open` fails with "DW-55 done". Both `done` passes.
  - `test_scheduled_story_keys_and_statuses_fail_closed`: for 6-6 and 6-8, a missing, renamed or duplicated key, or a status of `released`, `accepted`, `ready` or `null`, fails closed.
- All existing assertions stay unchanged.

### E3. Verification lane: extend E1 to Story 6.6

The individual Debug test-project lane of [sprint-change-proposal-2026-11-01.md](sprint-change-proposal-2026-11-01.md) E1 (:109) now also covers Story 6.6.

**The 6.6 lane:**

- **Projects side.** Run each test project the 6.6 spec names individually:
  - Build: `dotnet build tests/<project>/<project>.csproj --configuration Debug -m:1 -p:NuGetAudit=false --disable-build-servers`.
  - Then run the built xUnit v3 assembly with native `-xml` output.
- **McpCli side.** The repository's own gates, in Release with packaged dependencies, per McpCli `AGENTS.md`:
  - `DependencyPolicyTests`;
  - `EnrolledModuleCoverageTests`;
  - `tools/conformance-vectors/v1/check_enrolled.py`.

  These run against a locally packed `Hexalith.Projects.Contracts`, restored from a task-owned local feed with an isolated `NUGET_PACKAGES` (the global cache holds locally packed Hexalith packages that shadow nuget.org).
- **Evidence.** Record totals and the tested Projects and McpCli revisions in `evidence/epic6/6.6-cli-read-surface.{trx,json}`.
- **G-6.** It stays a platform qualification, not a 6.6 prerequisite.

D4's "and 6.8 if it needs it" is not used: Story 6.8's verification never named the common runner.

**Edits:**

- **`epics.md:1517`.** Replace the Verification bullet; the text is in E4.
- **`epics.md:1372-1376` (Epic 6 shared attributes).**
  - "under the approved 6.1/6.5 exception" → "under the approved 6.1/6.5/6.6 exceptions".
  - "individual test projects/distributed Development composition for 6.1/6.5" → "… for 6.1/6.5/6.6 (6.6 added 2026-11-15)".
- **`implementation-readiness-traceability-matrix.yaml:397` (`fr-22.environment`).**
  - OLD: `6.6: unchanged deterministic persisted-boundary fixture on the G-4 platform composition runner.`
  - NEW: `6.6 (E1 lane extended 2026-11-15): individual Debug/source Projects test projects with native xUnit XML plus McpCli's own Release/package enrollment gates against a locally packed Hexalith.Projects.Contracts; no common runner.`
- **`:410` (`fr-22.notes`).**
  - OLD: `The separate 6.6 command remains dotnet tool run hexalith-module test --profile cli-reads --filter Story=6.6; its CLI gates/evidence and full deployed release qualification remain unchanged.`
  - NEW: `Story 6.6 runs on the individual Debug test-project lane (E1 extended by sprint-change-proposal-2026-11-15-schedule-stories-6-8-and-6-6.md) with native xUnit XML and evidence/epic6/6.6-cli-read-surface.{trx,json}, and leaves backlog only after Story 6.8 and DW-55 are done; full deployed release qualification remains unchanged.`
- **`:676` (`finding-client-001.verification_command`).**
  - OLD: `"dotnet tool run hexalith-module test --profile web-reads,cli-reads --filter Story=6.5|6.6&Trait=identity"`
  - NEW: `"6.5: authentication composition cases in the accepted native UI lane (evidence/epic6/6.5-ui.xml); 6.6: credential/identity cases on the individual Debug test-project lane recorded in evidence/epic6/6.6-cli-read-surface.{trx,json}, plus McpCli credential and enrollment gates in that repository"`
- **Markdown view (`implementation-readiness-traceability-matrix.md:146`, `:176`).** Mirror the two text changes.
- The approved 2026-11-01 proposal is not edited (trim T2).

### E4. Story 6.6 rewrite: `epics.md:1498-1517`

```markdown
### Story 6.6: Inspect Projects through an authenticated CLI read surface

As a **Tenant Operator or automation caller**,
I want **authenticated read-only `Hexalith.McpCli` operations over the supported Projects reads, with deterministic JSON output**,
So that **operators and pipelines get authorized, metadata-only Project truth (FR-22) through the shared Hexalith CLI, with stable exit codes and no color-dependent meaning**.

- **Traceability:** FR-22; NFR-1, NFR-8; AD-2, AD-16, AD-19, AD-20, AD-29, AD-33; UX-DR19; evidence rows `fr-22`, `finding-client-001`.
- **Host (approved 2026-11-15, D3):** `Hexalith.McpCli` enrolls `Hexalith.Projects.Contracts` (`HexalithContracts="true"`) and submits the decorated Contracts queries through the EventStore Gateway, the same route the 6.5 Web surface uses. `Hexalith.Projects.Cli` and the Projects MCP plug-in stay frozen and unchanged; their inventory rows stay `defer / exclude`. No module-specific McpCli adapter is added.
- **Operations (D5), named through `[HexalithQuery(Name=…)]`:**
  - `projects.list` → `ListProjectsQuery`.
  - `projects.describe` → `GetProjectQuery`. It is the `describe`/`inspect` read. Descriptive fields appear only when authorized, and its reference states also cover `validate`/`validate-references`.
  - `projects.trace-conversation` → `ResolveProjectFromConversationQuery`.
  - `projects.trace-attachments` → `ResolveProjectFromAttachmentsQuery` (at most 32 attachments). Together with `trace-conversation` it is the `trace`/`trace-resolution` read.
  - The Conversation-start setup and context queries are not enrolled. There is no audit operation until Story 8.1.
- **Prerequisite:** Story 6.8 done and DW-55 done. The production-authority guard enforces both (sprint-change-proposal-2026-11-15-schedule-stories-6-8-and-6-6.md).

**Acceptance Criteria:**

**Given** an McpCli invocation with a platform credential, **When** an enrolled Projects read runs, **Then**:
- Tenant and actor come from the credential, and the Gateway authorizes the call.
- Output is deterministic JSON, Tenant-scoped, authorization-filtered and Safe Metadata by default. No meaning depends on color, terminal width or prose.
- Project name, Setup bodies, titles, paths and Preview bodies appear only under `projects:inspect_descriptive_metadata` and are recorded by the server's existing durable inspection audit. An audit write failure denies the disclosure.
- A missing credential fails before any query is sent.
- McpCli refuses every confirmation-required and UI-only action (Platform AD-14, AD-29). A-7 and Story 8.11 never enable confirmation on McpCli.
- The CLI is not gated on Story 8.11.

**Given** a denied, absent or cross-Tenant target, **When** any enrolled read runs, **Then** McpCli returns one identical safe `404` error with no payload or remote detail, and exits `2`.

**Given** an authorized read whose model is stale or unavailable, **When** the Gateway answers `200` with `responseState` `Partial` or `Unavailable`, **Then** McpCli exits `0`, and the JSON reports the snapshot, components, freshness and reason codes truthfully. McpCli exit codes are unchanged: `0` for a result, `2` for an error (D6).

**Given** identical facts, **When** McpCli and the 6.5 Web surface read the same Project, **Then** lifecycle and reference states, reason codes, `asOf`/`lastVerifiedAt` timestamps and warnings are semantically equal (parity).

**Given** Story 8.1 is not accepted, **When** a caller looks for an audit read, **Then** the McpCli catalog has no Projects audit operation and no query is sent.

**Given** the McpCli build with Projects enrolled, **When** `dotnet list package --include-transitive` runs, **Then** no FrontComposer, Fluxor, Fluent UI, Blazor or `Microsoft.AspNetCore.App` dependency comes in through `Hexalith.Projects.Contracts`.

**Given** any enrolled read, **When** persisted state is compared before and after, **Then** no domain event or read-model write occurred apart from the inspection audit.

- **Verification (E1 lane, extended 2026-11-15):**
  - Projects side: the individual Debug test projects named in the 6.6 spec, each built with `-m:1 -p:NuGetAudit=false --disable-build-servers` and run as a built xUnit assembly with native XML.
  - McpCli side: that repository's own Release/package enrollment gates, against a locally packed `Hexalith.Projects.Contracts` restored with an isolated `NUGET_PACKAGES`.
- **Evidence:** `evidence/epic6/6.6-cli-read-surface.{trx,json}`, with totals and the tested Projects and McpCli revisions.
- **Estimate:** M.
- **Completion boundary:**
  - Delivered: four authenticated read-only McpCli operations with Web parity and a dependency-light Contracts closure.
  - DW-82: publishing the decorated Contracts package and integrating the McpCli enrollment on McpCli main.
  - Story 8.4: the mutating CLI, option validation and `refresh-context`.
  - Story 8.5: MCP agent flows.
```

**What changed and why:**

- **Host and operations** follow D3 and D5.
- **The stale-model exit** follows D6; it was previously a non-zero exit.
- **Confirmation** is now always denied. That matches epics.md:59, the AD-29 rule and the AD-33 amendment in E5, and supersedes the "CLI after A-7" wording that came from sprint-change-proposal-2026-09-08.md:57, :119 and :317 (that historical file is not edited).
- **The `audit` AC** keeps the "absent" branch, which is what D5 picks.
- **Kept ACs:** Web parity, Safe Metadata by default, audited descriptive inspection, the identical safe `404`, and no Blazor/Fluxor/FrontComposer dependency.
- **Spec ACs:** the 6.6 spec's two additional ACs (dependency listing, no domain write) are adopted.
- **Evidence rows:** the dangling row name `fr-22-cli` is replaced with the existing rows `fr-22` and `finding-client-001`.

### E5. Requirement, architecture and UX reconciliation

**UX-DR19 (`epics.md:227`).** Replace the last sentence. OLD: *"CLI confirmation-required actions are enabled only after the accepted A-7 interactive-session claim; otherwise safe denial."* NEW:

> On `Hexalith.McpCli` (approved 2026-11-15), the read-only group is exposed as:
> - `projects.list`;
> - `projects.describe`, which covers `describe`, `inspect` and `validate`/`validate-references` through its reference states;
> - `projects.trace-conversation` and `projects.trace-attachments`, which cover `trace`/`trace-resolution`.
>
> `audit` is absent until Story 8.1. `refresh-context`, preview and mutating commands belong to Story 8.4. McpCli refuses confirmation-required and UI-only actions (Platform AD-14, AD-29), and neither A-7 nor Story 8.11 enables them on McpCli. Confirmation stays on the confidential interactive Web and Chatbot UI.

**AD-33 (`ARCHITECTURE-SPINE.md`).**

- Archive/restore row, Tenant Operator cell. OLD: `Allowed (Web; CLI after A-7; MCP after A-7 + gate)`. NEW: `Allowed (Web only; McpCli CLI/MCP refuse it under AD-29)`. The Tenant Project Administrator cell ("same surfaces") then resolves to Web only.
- Footnote (:375). OLD: *"MCP follows AD-29: read and task control until the consequential-MCP gate; the gate never admits autonomous confirmation."* NEW:

  > McpCli (CLI and MCP heads) follows AD-29: read and task control only. A-7 and Story 8.11 never enable confirmation-required actions on McpCli; they stay on the confidential interactive Web and Chatbot UI, and autonomous confirmation is never admitted.

**UX spec.** Not edited (trim T3). Its CLI navigation list (`ux-design-specification.md:948-956`) and Journey 4's MCP confirmation gate (:716) are routed to DW-84.

### E6. `epics.md:61`: new correction note

Insert after the "Approved entry correction — 2026-11-02" paragraph:

> **Approved scheduling correction — 2026-11-15.** Jerome approved [this proposal](sprint-change-proposal-2026-11-15-schedule-stories-6-8-and-6-6.md):
> - Story 6.8 may be scheduled now. Story 6.6 leaves backlog only after 6.8 and DW-55 are done, and the production-authority guard enforces this.
> - Story 6.6 targets `Hexalith.McpCli` with four decorated Projects queries and runs on the extended E1 individual Debug test-project lane.
> - 6.7 stays in backlog; historical NOT_READY and every release obligation remain.

### E7. Story 6.8 accuracy: `epics.md:1537-1556`

- **AC1 line reference and scope.** Replace *"the `Hexalith.Projects.Contracts.csproj:22-33` FrontComposer/Fluxor/Fluent UI/ASP.NET Core references, … those types and references move to a new non-packable `Hexalith.Projects.UI.Contracts` project"* with:

  > the `Hexalith.Projects.Contracts.csproj:13-47` FrontComposer Contracts/Shell/SourceTools, `Microsoft.AspNetCore.App`, Fluxor and Fluent UI references, **When** the split completes, **Then**:
  > - the references, the FrontComposer projection/descriptor types and the badge mapping move to a new non-packable `Hexalith.Projects.UI.Contracts` project that depends inward on `Hexalith.Projects.Contracts` only;
  > - every type that Contracts commands, events, models or queries reference stays in Contracts, with its FrontComposer attributes removed and its namespace and wire names unchanged.

  **Why:** `ReferenceState`, `ProjectLifecycle`, `ProjectReasonCode` and other enums in the `Ui/` folder are used by Contracts events, models and queries, so they cannot move outward.
- **New AC (release tooling).**

  > **Given** the split, **When** it lands, **Then**:
  > - `tools/release-packages.json:22-55` lists the slim Contracts graph (no `Microsoft.AspNetCore.App` framework reference, and no FrontComposer, Fluxor, Fluent UI or ASP.NET Core dependencies), together with every other package entry whose graph changes;
  > - `tests/tools/run-package-dependency-gate.ps1:44-56` stops requiring Contracts to declare the FrontComposer SourceTools analyzer. It instead fails if Contracts references FrontComposer, and it applies the analyzer rule to UI.Contracts if that project keeps the generator.
- **Client scope.** Add to the completion boundary:

  > `Hexalith.Projects.Client.csproj:6` adds its own `Microsoft.AspNetCore.App` framework reference. 6.8 does not change it: AD-16's rule names Contracts, and McpCli consumes only Contracts. Any decision on the Client graph belongs to Story 8.7 (AD-24 package graph).
- **Verification.** Make the transitive check package-mode and add the gate and the 6.5 consumer tests:

  > `dotnet build Hexalith.Projects.slnx --configuration Debug` (zero warnings/errors). Then `pwsh tests/tools/run-package-dependency-gate.ps1` in package mode (`-p:UseHexalithProjectReferences=false`, isolated `NUGET_PACKAGES`). In package mode, `dotnet list src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj package --include-transitive` shows no Blazor/Fluxor/FrontComposer/AspNetCore.App reference. The existing Contracts, Projects, Server and UI test projects pass unchanged, each run individually.
- **DW-55.** Add to the completion boundary:

  > Closing 6.8 sets DW-55 to `status: done` (one canonical value, with the date on a separate `closed:` line). The guard requires this before 6.6 leaves backlog.

  In the ledger, correct DW-55's `location` from `:22-33` to `:13-47`.
- **Main CI.** 6.8 starts from a red `build-and-test` (DW-83). The done rule allows a recorded local run, but 6.8 must not hide DW-83. If its manifest regeneration clears all of the FrontComposer drift, record that against DW-83 when 6.8 closes.

### E8. McpCli sibling-repository authorization (Story 6.6 only)

This approves the following changes, to be made by the Story 6.6 implementation. Each is validated by that repository's own gates.

**In `references/Hexalith.McpCli`:**

1. **Enrollment.** In `src/Hexalith.McpCli/Hexalith.McpCli.csproj:24-31`, add `<PackageReference Include="Hexalith.Projects.Contracts" HexalithContracts="true" />` next to the Parties and Tenants entries.
2. **Version pin.** Add a `HexalithProjectsVersion` pin in the root `Directory.Build.props`, next to the EventStore, Parties and Tenants pins. It selects the candidate decorated package; the current catalog default 1.2.4 cannot enroll.
3. **Enrollment artifacts** required by McpCli `AGENTS.md`:
   - the exact version and restored closure under `modules` in `tools/dependency-policy.json`;
   - `tools/inventory-coverage/v1/projects-approved.json`;
   - conformance vectors covering every new Operation Shape, with `tools/conformance-vectors/v1/current/modules/Hexalith.Projects.Contracts/approval.json`;
   - hash-bound plans wherever its rules require them;
   - a README update for user-visible behavior.
4. **Inventory v2.** Publish `_bmad-output/implementation-artifacts/projects-migration-inventory-v2.md` (`inventory_version: '2.0.0'`), superseding v1. The v1 bytes stay unchanged. v2:
   - adds normalized target operations T01–T04 (`projects.list`, `projects.describe`, `projects.trace-conversation`, `projects.trace-attachments`) as `replace / include`, each with its decorated type and exact Gateway route;
   - leaves every R01–R11, M01–M05, L01–L18 and V01–V12 row `defer / exclude`;
   - cites this proposal as Jerome's session approval and asserts no external owner sign-off, under the same limits as A1.

**In Projects (6.6 code):**

5. **Abstractions reference.** `Hexalith.Projects.Contracts` references `Hexalith.McpCli.Abstractions` the way `Hexalith.Parties.Contracts.csproj:17-18` does, adapted to the Projects convention:
   - a `ProjectReference` to `$(HexalithMcpCliRoot)\src\Hexalith.McpCli.Abstractions\Hexalith.McpCli.Abstractions.csproj` when `UseHexalithProjectReferences == 'true'`;
   - otherwise a versionless `PackageReference` (catalog `$(HexalithMcpCliVersion)`, Builds `Props/Directory.Packages.props:247`).
6. **Root property.** Define `HexalithMcpCliRoot` in `Directory.Build.props`, beside the other `Hexalith*Root` entries. `references/Hexalith.McpCli` is a root-declared submodule.
7. **Release manifest.** Add `Hexalith.McpCli.Abstractions` to the Contracts entry in `tools/release-packages.json`.

**Not authorized:**

- merging to McpCli or AI.Tools `main`, pushing, publishing, or moving any gitlink;
- changes to McpCli exit codes (D6), module-specific adapters, generated subcommands, or the Projects CLI/MCP sources;
- changes to the AI.Tools plan index. The re-pin waits for DW-82.

### E9. Approval of record for the McpCli course correction

No approval record exists for a 2026-09-27 decision, and this proposal does not invent one. **Approving this proposal is the approval of record, dated 2026-11-15**, for the McpCli course-correction notes in force since `b42beb3a` (2026-09-28):

- `prd.md:10`;
- `ARCHITECTURE-SPINE.md:32`, plus the AD-29 rule at `:330`;
- `epics.md:59`.

In each of the three notes, replace the lead *"Approved McpCli course correction (2026-09-27)"* with *"McpCli course correction (drafted 2026-09-27; approved 2026-11-15 by sprint-change-proposal-2026-11-15-schedule-stories-6-8-and-6-6.md §E9)"*. The note bodies stay unchanged.

The untracked item at `deferred-work.md:838-840` becomes resolved:

```
  status: resolved
  resolution: 2026-11-15; sprint-change-proposal-2026-11-15-schedule-stories-6-8-and-6-6.md §E9 records the approval; §E5 reconciles Spine :375 (formerly :342) with AD-29.
```

### E10. DW-81: close it (revised during application, T5)

**Decision:** close DW-81, with no new rerun. Main CI already re-verified it: G-6 **passed** at `b756d01a` (run 43692737105), with McpCli at `63df6d2b`, which already targets net11.0. `NU1202` no longer occurs.

G-6 failed again at `fc58cefd` (runs 43694739240 and 43697046014). The step "Restore effective G-6 package graphs" exits 2 while `Hexalith.Builds.Module.Cli prepare-build` runs for `references/Hexalith.Conversations`, with empty stderr. The Conversations pointer is unchanged (`c34dc3c1`); the Builds pointer moved `99f7c860` → `7cd0c17a`. This is a new regression, not DW-81, so it joins DW-83 (E11).

Edit the entry: `status: done`, plus a `closed:` line that cites run 43692737105 at `b756d01a` and points the later failure to DW-83.

### E11. `deferred-work.md`: new entries (inserted after DW-81; none gates 6.8 or 6.6)

| ID | Item | Owner | Removal condition / gate |
| --- | --- | --- | --- |
| DW-82 | Publish the slim, decorated `Hexalith.Projects.Contracts` and integrate the McpCli Projects enrollment on McpCli `main`. Then re-pin the Projects row of the AI.Tools migration plan to inventory v2 under a new `plan_version` and `inventory_revision`. 6.6 is validated against a locally packed package, and McpCli CI restores only published packages. Depends on Projects release CI (DW-83). | Projects release maintainer; McpCli maintainers; AI.Tools maintainer | McpCli `main` CI passes with Projects enrolled at a published version, and the AI.Tools plan links v2 at that McpCli revision. Not a 6.6 gate; consumers are Stories 8.4, 8.5 and 8.7. |
| DW-83 | Restore green Projects main CI after the 2026-11-15 pointer moves. (1) Since `b756d01a`, `ci / build-and-test` fails at "Validate package consumer references": the `Hexalith.Projects` graph has `wrong_versions=[FrontComposer.Contracts, FrontComposer.Contracts.UI, FrontComposer.Shell]`, because the committed Builds pointers `99f7c860`/`7cd0c17a` select FrontComposer 6.0.0 while `tools/release-packages.json` pins 5.0.0. As a knock-on, Tier 1 reports no failure count. (2) At `fc58cefd`, G-6 fails at the Conversations `prepare-build` step (E10). | Projects build hygiene with the Builds Module CLI maintainers | `build-and-test` and G-6 both pass on main at a recorded commit. Not a 6.8 gate, and 6.8 must not mask it (E7). |
| DW-84 | Reconcile the Story 8.4/8.5 confirmation wording ("CLI after A-7", "MCP after A-7 + 8.11") and UX Journey 4's MCP gate (`ux-design-specification.md:716`) with AD-29 and the amended AD-33: McpCli never confirms. Also map the UX spec's CLI navigation list (`:948-956`) to the UX-DR19 McpCli operation names. Recheck PRD §9 A-7's revisit clause at the same time. | Product Owner with Solution Architect | Done when 8.4 and 8.5 are specified against the amended AD-33. Not an Epic 6 gate. |

### E12. `sprint-status.yaml` (hand edit; no status moves)

- Add trailing comments (the guard fixtures reset these lines, so the comments are safe):
  - `6-6-…: backlog # leaves backlog only after 6-8 done + DW-55 done (sprint-change-proposal-2026-11-15-schedule-stories-6-8-and-6-6.md)`;
  - `6-8-…: backlog # schedulable under the 2026-11-15 guarded exception; closing it closes DW-55`.
- No new top-level key (trim T4); the guard and this proposal carry the rule.
- `last_updated` is already 2026-11-15.
- Never run `sprint_plan.py`.
- 6.8 moves to `ready-for-dev` only when `/bmad-build 6.8` creates its spec.

### Not changed

- **Left as they are:**
  - the uncommitted 6.6 spec and `epic-6-context.md` edits. Their "CLI output" (stale → non-zero exit) and "verification lane" bullets become stale after approval, so regenerate them once those edits settle;
  - the unrecorded root submodule pointer moves;
  - every done story;
  - the historical 2026-09-08 and 2026-11-01 proposal text (apart from the E3 pointer);
  - the PRD body;
  - AD-29 and AD-30;
  - Epic 8 story text.
- **Out of scope:** the stale `6.5 remains blocked…` sentence and the `net10.0` paths in matrix row `fr-22` describe the 6.5 history and are not touched here.

## 5. Checklist record

| Item | Status | Note |
| --- | --- | --- |
| 1.1–1.3 Trigger, problem, evidence | [x] | §1 |
| 2.1 Current epic | [x] | Completable: 6.8 → 6.6, then 6.7 |
| 2.2 Epic-level changes | [x] | Story text only; no epic added or removed |
| 2.3–2.4 Future epics | [x] | Epic 8 wording to DW-84; nothing obsolete |
| 2.5 Order | [x] | 6.8 before 6.6, guard-enforced |
| 3.1 PRD | [x] | No requirement change; note ratified (E9) |
| 3.2 Architecture | [x] | AD-33 row and footnote (E5); Spine note ratified |
| 3.3 UX | [x] | UX-DR19 and the CLI navigation note; Journey 4 to DW-84 |
| 3.4 Other artifacts | [!] | Guard, tests, matrix, ledger, release tooling (via 6.8), McpCli (via 6.6); CI red on main (DW-83; DW-81 closed) |
| 4.1 Direct adjustment | Viable | Low effort for the correction; medium risk |
| 4.2 Rollback | Not viable | No done work conflicts |
| 4.3 MVP review | Not viable | MVP unaffected |
| 4.4 Selected path | [x] | Direct adjustment |
| 5.1–5.5 Proposal components | [x] | §1–§6 |
| 6.1–6.2 Review | [x] | Every cited line re-read on 2026-11-15 |
| 6.3 Approval | [x] | Jerome approved the trimmed version on 2026-11-15 (§7) |
| 6.4 sprint-status | [x] | E12 applied by hand; no status moves |
| 6.5 Handoff | [x] | §6 |

## 6. Implementation handoff

- **Scope:** moderate. This means backlog reorganization plus a guard change. The developer agent applies E1–E12 after approval. Product Owner and Solution Architect sign-off is Jerome's approval of this proposal.
- **Order of application:**
  1. E1 and E2 together (one commit when a commit is requested).
  2. E12.
  3. E3–E7 and E9.
  4. E10 and E11.
- **Validation after applying:**
  - `python3 tools/planning/validate_production_authority.py --validate-index`;
  - `python3 tools/planning/validate_production_authority.py --story-id 6.8`;
  - `python3 -m unittest discover -s tests/tools -p test_production_authority_guard.py`;
  - a YAML load of the edited matrix and sprint status;
  - `git diff --check`.
- **Success criteria:**
  - The guard passes on the live index and accepts any 6.8 status.
  - The guard rejects 6.6 progress until 6.8 and DW-55 are done, and rejects any 6.7 progress.
  - All guard tests pass, including the moved 6.7 example and the new cases.
  - Readiness stays `NOT_READY`.
  - No status moves; no product source, dependency, submodule or Git change.
- **Next steps:**
  1. `/bmad-build 6.8`.
  2. When 6.8 closes, set DW-55 to `done`.
  3. Resume `/bmad-build 6.6`. Its Tasks are written then, against the post-split Contracts layout.
- **Not authorized:** commits, pushes, `sprint_plan.py`, dependency or submodule updates, and any McpCli or AI.Tools change outside E8.

## 7. Approval record

Jerome reviewed each solution's mechanism, pros and cons on 2026-11-15 and approved the trimmed version. The four trims below are already reflected in E1–E12 above:

- **T1 (E1/E2).** No "no ledger → fail" branch and no "DW-55 cannot close before 6.8" rule. The guard enforces only what the brief asked for: 6.6 leaves backlog only when 6.8 and DW-55 are done, 6.7 stays pinned, and readiness stays NOT_READY. There are three new tests plus the moved 6.7 example.
- **T2 (E3).** The approved 2026-11-01 proposal gets no appended pointer line.
- **T3 (E5).** The UX spec is not edited. Its CLI navigation list joins Journey 4 under DW-84.
- **T4 (E12).** No new top-level sprint-status key, only the two line comments.
- **T5 (E10/E11, decided during application).** Run 43692737105 shows G-6 passed at `b756d01a` with the net11.0 McpCli. Jerome chose to close DW-81 and to widen DW-83 to cover both main-CI regressions (the `build-and-test` FrontComposer drift and the `fc58cefd` G-6 failure), instead of keeping DW-81 open.
