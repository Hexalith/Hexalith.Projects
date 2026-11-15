---
title: 'Inspect Projects through an authenticated CLI read surface'
type: 'feature'
created: '2026-11-15'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
blocked_by:
  - '6-8-split-hexalith-projects-ui-contracts-from-contracts'
  - 'approved bmad-correct-course correction (decisions D2, D4-D6)'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
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

**Always:** Identity and Tenant come from the platform credential, and the Gateway authorizes every call. Output is Safe Metadata by default; descriptive fields appear only with `projects:inspect_descriptive_metadata`, under the server's existing durable audit. Output is deterministic JSON, and no meaning depends on color, width or prose. Denied, cross-Tenant and absent targets return one safe result. Confirmation-required actions are denied.

**Never:**
- Changes to `Hexalith.Projects.Cli`.
- Module-specific McpCli adapters.
- Writes, previews, maintenance or Diagnostic Export.
- An audit operation before Story 8.1.
- Echoing remote detail.
- Changing query wire names, payloads, response records or server admission.
- Changing the 6.5 sources.
- Routing cutover (Story 6.7).
- Running `sprint_plan.py`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Authorized list | Valid credential, page ≤ 200 | `ProjectListPage` JSON with snapshot and opaque cursor; exit 0 | N/A |
| Descriptive inspection | Permission present / absent | Name and Setup present and audited / null | Audit write failure denies |
| Denied, absent or foreign target | Any operation | Identical safe 404 error; exit 2; no payload | No remote detail |
| Missing credential | Any operation | Fails before any query is sent; exit 2 | Safe error code |
| Stale or unavailable model | `responseState` Unavailable or Partial | Snapshot and components reported truthfully; exit 0 | N/A |
| `audit` | Any | Not enrolled, so the operation is absent | No query |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Projects.UI/Diagnostics/ProjectReadQueryClient.cs:42-74` -- the parity reference. It calls `SubmitQueryAsync<T>(new SubmitQueryRequest(tenant, "projects", target, queryType, payload))` and maps denial to `safe_denial` without remote detail. Do not reference the UI project.
- `src/Hexalith.Projects.UI/Diagnostics/{ProjectInventorySource,ProjectDetailSource,ProjectResolutionTraceSource}.cs` -- the target and payload for each operation:
  - `ListProjectsQuery` on target `projects`.
  - `GetProjectQuery` on the project id.
  - `ResolveProjectFrom{Conversation,Attachments}Query`, with at most 32 attachments.
  - The context and start-setup queries are not in D5, so they stay unenrolled.
- `src/Hexalith.Projects.Contracts/Queries/{ListProjectsQuery,GetProjectQuery,ResolveProjectFromConversationQuery,ResolveProjectFromAttachmentsQuery}.cs` -- the decoration targets. After 6.8, Contracts must be dependency-light; recheck line references then.
- `src/Hexalith.Projects.Server/Queries/{ProjectReadQueryAdmission.cs:12-33,ProjectReadQueryExecutor.cs:215-248}` -- the scopes, roles, descriptive permission and audit-before-disclosure. Do not change these.
- `references/Hexalith.Parties/src/Hexalith.Parties.Contracts/{Hexalith.Parties.Contracts.csproj:17-18,Queries/GetParty.cs}` -- the pattern for the `Hexalith.McpCli.Abstractions` reference (source or package) and for decoration.
- `references/Hexalith.McpCli/src/Hexalith.McpCli/Hexalith.McpCli.csproj:24-31` -- the `HexalithContracts="true"` enrollment point. It is a sibling repo and needs its own authorization.
- `references/Hexalith.McpCli/_bmad-output/implementation-artifacts/projects-migration-inventory-v1.md` -- every row is `defer / exclude` today. Enrollment needs a versioned v2 that marks only the new reads as included.
- `references/Hexalith.McpCli/src/Hexalith.McpCli/Cli/CliOutput.cs:125-137` -- exits are 0 for a result and 2 for an error. Keep this unchanged (D6).

## Tasks & Acceptance

Tasks are written when the build resumes after 6.8 is done, because the split changes the Contracts layout and package references.

**Acceptance Criteria:**
- Given identical facts, when McpCli and the 6.5 Web read the same Project, then lifecycle and reference states, reason codes, `asOf`/`lastVerifiedAt` timestamps and warnings are semantically equal.
- Given the McpCli build with Projects enrolled, when `dotnet list package --include-transitive` runs, then no FrontComposer, Fluxor, Fluent UI or `Microsoft.AspNetCore.App` dependency comes in through Projects Contracts.
- Given any operation, when persisted state is compared before and after, then no domain event or read-model write occurred apart from the inspection audit.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands (D4 lane):**
- `dotnet build tests/<test-project> --configuration Debug -m:1 -p:NuGetAudit=false --disable-build-servers`, then invoke each built xUnit assembly with native XML -- expected: every matrix row passes. Record totals and tested revisions in `evidence/epic6/6.6-cli-read-surface.{trx,json}`.
- `python tools/planning/validate_production_authority.py --validate-index` -- expected: passes after every status change.
