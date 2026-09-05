# Sprint Change Proposal — 2026-09-02

## 1. Issue Summary

Story 6.2 (`spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md`) is `awaiting-operator`
and lists four operator actions before it can proceed. Three require external owner sign-off this
session cannot grant (the 6.1-P0…P4 prerequisite chain, landing Story 6.1's shared seams, and the
readiness rerun). The fourth — "Create and schedule the standalone Hexalith.Projects.UI.Contracts
split story removed from this scope on 2026-08-26" — is fully within Hexalith.Projects' own
repository authority and is actionable now.

The underlying defect: `src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj` is
`IsPackable=true` yet references `Hexalith.FrontComposer.Shell`, `Hexalith.FrontComposer.SourceTools`,
an ASP.NET Core `FrameworkReference`, `Fluxor.Blazor.Web`, and `Microsoft.FluentUI.AspNetCore.Components`
(lines 22-33), and `src/Hexalith.Projects.Contracts/Ui/ProjectContextFreshness.cs` carries a
`[ProjectionBadge]` decoration from `Hexalith.FrontComposer.Contracts.Attributes`, consumed by
`ConversationStartSetup.cs:11`. This violates the adopted **AD-16** architecture decision
(`ARCHITECTURE-SPINE.md:198-202`): `Hexalith.Projects.Contracts` "must not depend on FrontComposer
Shell, Fluxor, Fluent UI, `Microsoft.AspNetCore.App`, Dapr, or Aspire," and a non-packable
`Hexalith.Projects.UI.Contracts` — already named in the target package graph
(`ARCHITECTURE-SPINE.md:265,397`) — "depends inward on Contracts and contains presentation metadata
only."

Story 6.2's Spec Change Log (2026-08-26) removed this split from its own scope, noting: "It must land
before Stories 6.6, 8.4, and 8.5 or those surfaces inherit Blazor/Fluxor transitively," and that
scheduling it requires `tools/planning/validate_production_authority.py --story-id` plus a
`sprint-status.yaml` entry — which Story 6.2 itself may not perform.

## 2. Impact Analysis

**Epic impact.** Epic 6 gains one new story (6.8) for the split. No epic is invalidated, resequenced,
or removed. Stories 6.6, 8.4 (Epic 8), and 8.5 (Epic 8) each gain an explicit prior-only dependency
on Story 6.8; their own scope, acceptance criteria, and numbering are unchanged.

**Story impact.**
- New: Story 6.8 (this proposal).
- Modified (dependency note only, no scope change): Story 6.6, Story 8.4, Story 8.5.
- Closed: Story 6.2's third operator action, pointed at Story 6.8.

**Artifact conflicts.**
- PRD: none. This is packaging hygiene implementing an already-adopted architecture decision, not a
  new requirement.
- Architecture: none — AD-16 and the target package graph already specify this split; no spine change
  needed.
- UI/UX: none.
- Other artifacts: `epics.md` (new story + three dependency notes) and `sprint-status.yaml` (new
  tracking entry), validated through the production-authority guard per `_bmad-output/project-context.md`'s
  scheduling rule.

## 3. Recommended Approach

**Option 1 — Direct Adjustment.** Add Story 6.8 within the existing Epic 6 structure; no rollback or
MVP change needed. Effort: Low. Risk: Low — packaging-only, no runtime behavior change, no external
owner dependency.

Selected: **Option 1**. The split is self-contained, already architecturally mandated, and blocks
real downstream work (6.6/8.4/8.5) if left undone.

## 4. Detailed Change Proposals

### 4.1 `epics.md` — new Story 6.8 (inserted after Story 6.7, before the Epic 6 horizontal rule)

```markdown
### Story 6.8: Split Hexalith.Projects.UI.Contracts from the packable Contracts package

As a **Solution Architect / Projects module owner**,
I want **FrontComposer presentation descriptors moved out of packable `Hexalith.Projects.Contracts`
into a new non-packable `Hexalith.Projects.UI.Contracts` project that depends inward on Contracts and
owns presentation metadata only**,
So that **`Hexalith.Projects.Contracts` stops leaking Blazor/Fluxor/FrontComposer/ASP.NET Core
dependencies into every consumer (CLI, MCP, and any future non-Web adapter), matching the adopted
AD-16 package boundary before Stories 6.6, 8.4, and 8.5 build on it**.

- **Traceability:** AD-16 (Contracts must not depend on FrontComposer Shell, Fluxor, Fluent UI,
  `Microsoft.AspNetCore.App`, Dapr, or Aspire; `Projects.UI.Contracts` depends inward on Contracts and
  owns presentation metadata only), AD-2, AD-24 (target package graph); findings ARCH-002, API-001,
  MCP-001; identified as an operator action in Story 6.2
  (`spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md`, Spec Change Log 2026-08-26)
  after bmad-code-review found `Hexalith.Projects.Contracts.csproj` (`IsPackable=true`) referencing
  `Hexalith.FrontComposer.Shell`, `Hexalith.FrontComposer.SourceTools`, an ASP.NET Core
  `FrameworkReference`, `Fluxor.Blazor.Web`, and `Microsoft.FluentUI.AspNetCore.Components`, with
  `ConversationStartSetup.cs:11` consuming `Hexalith.Projects.Contracts.Ui.ProjectContextFreshness`
  decorated with `[ProjectionBadge]` from `Hexalith.FrontComposer.Contracts.Attributes`.

**Acceptance Criteria:**

**Given** the current `src/Hexalith.Projects.Contracts/Ui/` types (including `ProjectContextFreshness`
and its `[ProjectionBadge]` decoration) and the `Hexalith.Projects.Contracts.csproj:22-33`
FrontComposer/Fluxor/Fluent UI/ASP.NET Core references, **When** the split completes, **Then** those
types and references move to a new non-packable `Hexalith.Projects.UI.Contracts` project that depends
inward on `Hexalith.Projects.Contracts` only, and `Hexalith.Projects.Contracts.csproj` no longer
references FrontComposer Shell, FrontComposer SourceTools, `Microsoft.AspNetCore.App`, Fluxor, or
Fluent UI.

**Given** existing consumers of the moved types (Story 3.5/6.5 FrontComposer code, generated
descriptors), **When** the split lands, **Then** every reference is repointed to
`Hexalith.Projects.UI.Contracts` with no behavior change, no redefinition of operations/vocabulary/
security (AD-16), and a green build/test run.

**Given** a CLI- or MCP-only consumer of `Hexalith.Projects.Contracts` (as Stories 6.6/8.4/8.5 will
be), **When** it references the package, **Then** it pulls no Blazor/Fluxor/FrontComposer/ASP.NET
Core transitive dependency.

**Given** the split is complete, **When** Stories 6.6, 8.4, or 8.5 begin, **Then** this story's
completion is a satisfied prior-only dependency for each.

- **Verification:** `dotnet build Hexalith.Projects.slnx --configuration Debug` (zero
  warnings/errors); `dotnet list src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj
  package --include-transitive` shows no Blazor/Fluxor/FrontComposer/AspNetCore.App reference;
  existing Contracts/Projects/Server test suites pass unchanged. **Evidence:** build +
  dependency-listing output attached to the story; no new `evidence/epic6/` artifact required
  (packaging-only, no runtime admission behavior). **Estimate:** S. **Completion boundary:**
  `Hexalith.Projects.Contracts` is dependency-light per AD-16; `Hexalith.Projects.UI.Contracts`
  exists and is consumed; no runtime/behavior change.
```

### 4.2 `epics.md` — prerequisite notes (one line each, no other change)

- Story 6.6 (after the Traceability line): `- **Prerequisite:** Story 6.8 (Hexalith.Projects.UI.Contracts split) must land first, or this CLI surface inherits Blazor/Fluxor transitively through `Hexalith.Projects.Contracts`.`
- Story 8.4 (after the Traceability line): same wording, "this CLI contract" in place of "this CLI surface".
- Story 8.5 (after the Traceability line): same wording, "this MCP surface" in place of "this CLI surface".

### 4.3 `sprint-status.yaml`

Add, immediately after `6-7-cut-over-supported-reads-while-preserving-compatibility-and-rollback: backlog`
and before `epic-6-retrospective: optional`:

```yaml
  6-8-split-hexalith-projects-ui-contracts-from-contracts: backlog
```

Validated via `validate_production_authority.py --validate-index --sprint-status <candidate>` before
atomic replacement, then `--validate-index` re-run on the active file, then `--story-id "6.8"` to
confirm resolution — per `_bmad-output/project-context.md`'s sprint-status scheduling rule.

### 4.4 `spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md`

Mark the third `operator_actions` entry resolved with a pointer to Story 6.8 and this proposal;
Story 6.2 otherwise remains `awaiting-operator` pending the other three actions.

## 5. Implementation Handoff

**Scope classification: Minor.** Packaging-only backlog addition, no code change, no external owner
dependency, no PRD/architecture conflict. Implemented directly in this session as Developer-agent
work (`bmad-correct-course` → direct file edits), no PO/Architect escalation needed.

**Success criteria:** `epics.md` contains Story 6.8 plus the three prerequisite notes;
`sprint-status.yaml` passes `--validate-index` with the new entry present and resolves via
`--story-id "6.8"`; Story 6.2's operator action #3 is marked resolved. Story 6.2 itself remains
`awaiting-operator` — this proposal does not unblock it, only its third operator action.
