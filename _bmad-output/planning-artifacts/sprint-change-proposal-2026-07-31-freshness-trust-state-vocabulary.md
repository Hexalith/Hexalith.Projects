# Sprint Change Proposal: Canonical Reference-Health Freshness Vocabulary

**Date:** 2026-07-31
**Project:** Hexalith.Projects
**Prepared for:** Jerome
**Status:** Approved — routed for direct correction and Story 8.8 parity verification
**Change scope:** Minor direct correction across reference-health contracts, adapters, documentation, and focused parity tests; no new epic or story

## 1. Issue Summary

### Trigger

The Epic 5 retrospective carries this in-progress action:

> Canonicalize or explicitly document the mixed FreshnessTrustState vocabulary used by reference-health rows.

Story 5.5 review identified the underlying problem. A single reference-health column currently exposes three producer vocabularies:

- folder, file, and memory summaries copy `ProjectionTrustState`, commonly `trusted`;
- context-evaluation enrichment lowercases `ProjectContextFreshness`, commonly `fresh`;
- conversation enrichment lowercases `ProjectConversationTrustSignal`, commonly `current` or `stale`.

Because later sources overwrite earlier rows during enrichment, the value visible to an operator can depend on which producer supplied the last row rather than on one stable evidence-freshness contract.

### Normative Conflict

The current PRD glossary and Architecture Spine already define Evidence Freshness State as exactly `Current`, `Stale`, `Rebuilding`, or `Unavailable`. Architecture decisions AD-16 and AD-32 require Contracts to own shared vocabulary and prohibit adapters from introducing synonyms. Merely documenting the current mixed public values would therefore preserve a known conflict with current product and architecture authority.

### Approved Semantic Decision

Canonicalize every reference-health `freshnessTrustState` value to one of these lower-case codes:

| Producer input | Canonical reference-health code |
| --- | --- |
| `trusted`, `fresh`, `current` | `current` |
| `stale`, `mixedGeneration` | `stale` |
| `rebuilding` | `rebuilding` |
| `unavailable`, `unknown`, `forbidden`, `redacted` | `unavailable` |
| empty, whitespace, or unrecognized | `unavailable` |

Input matching is trimmed and case-insensitive. `forbidden`, `redacted`, and other detailed outcomes retain their exact meaning in the existing inclusion, health, failed-check, reason, and diagnostic fields; freshness normalization must not erase those fields.

### Evidence

- `_bmad-output/implementation-artifacts/5-5-reference-inventory-health-view.md` records the open review follow-up and concrete mixed values.
- `_bmad-output/implementation-artifacts/epic-5-retro-2026-06-26.md` records the matching action item and its success criterion.
- `src/Hexalith.Projects.Contracts/Ui/ProjectReferenceHealthRowProjection.cs` currently copies the raw summary trust state.
- `src/Hexalith.Projects.UI/Diagnostics/ProjectReferenceHealthMapper.cs` currently emits lower-cased context and conversation enum names.
- Web, safe-export, warning, CLI, and MCP consumers currently copy or emit these values without one shared reference-health normalization rule.
- `docs/projection-catalog.md`, `docs/parity-matrix.md`, and `docs/event-catalog.md` describe the participating fields or producer vocabularies but do not define the complete canonical boundary mapping.

## 2. Impact Analysis

### Epic Impact

- **Epic 5:** Remains closed implementation history. Story 5.5 is not reopened; its review follow-up is checked only after the correction and verification pass.
- **Epic 6:** No story scope change. Story 6.2 does not own the reference-health matrix and is removed from this action's routing.
- **Epic 8:** No story scope change. Existing Story 8.8 remains the appropriate parity-verification gate for the observable Web, CLI, MCP, warning, and export behavior.
- **Ordering and priority:** No epic or story is added, removed, reopened, renumbered, or resequenced.

### Story Impact

This is a bounded developer correction against an existing retrospective action and review follow-up. Creating another story would duplicate historical Story 5.5 ownership and add unnecessary backlog authority. The implementation can proceed directly after this proposal is approved, with parity evidence routed through Story 8.8.

### Artifact Conflicts and Changes

- **PRD:** No edit. Its Evidence Freshness State glossary already supplies the canonical four-state vocabulary.
- **Architecture Spine:** No edit. AD-16, AD-32, and the consistency conventions already require the selected outcome.
- **Epics:** No edit. Current production ownership and the Story 8.8 parity gate are sufficient.
- **UX specification:** Clarify the canonical visible labels and non-color-only communication for reference health.
- **Projection, parity, and event catalogs:** Define the boundary values, full normalization table, and producer-local nature of source enums.
- **Sprint status:** Correct the existing action's routing and attach this proposal as planning evidence. Keep the action `in-progress` until implementation and focused verification pass.
- **Story 5.5 implementation record:** Check only the matching review follow-up after the correction passes; preserve all other historical content and open follow-ups.

### Technical and Compatibility Impact

- The observable `freshnessTrustState` field name and string type remain unchanged.
- Reference-health values such as `trusted` and `fresh` change semantically to `current`; `mixedGeneration` changes to `stale`; `forbidden`, `redacted`, unknown, and invalid inputs fail closed as `unavailable`.
- The correction changes no event, persisted projection schema, command, route, generated client, OpenAPI schema, package version, or deployment topology.
- Producer-specific enums remain unchanged and valid inside their owning boundaries.
- Consumers that asserted a legacy reference-health string may require an expectation update. Focused Web, CLI, MCP, warning, and export parity tests contain that compatibility risk.
- Metadata-only and payload-exclusion constraints are unchanged.

### MVP and Schedule Impact

MVP scope and release ordering are unchanged. Expected effort is small to medium: one shared vocabulary/normalizer, bounded adapter changes, documentation, and focused tests. Implementation risk is low to medium because field shape is stable but observable values change.

## 3. Recommended Approach

### Selected Path: Direct Canonicalization

Add one Contracts-owned Evidence Freshness vocabulary and normalization rule, apply it at every reference-health construction boundary, document it, and prove parity across consumers.

This path is recommended because it:

- conforms to the already-approved PRD and architecture vocabulary;
- removes source and merge-order dependence;
- keeps producer contracts and generated artifacts stable;
- fails closed for ambiguous or unknown evidence;
- preserves detailed authorization and diagnostic meaning in its existing fields; and
- avoids creating or reopening backlog items for a bounded review correction.

### Alternatives Considered

#### Document the Mixed Public Vocabulary

Rejected. It would make the current ambiguity deliberate while conflicting with the PRD glossary, AD-16, and AD-32.

#### Change Producer or OpenAPI Vocabularies

Rejected for this correction. It would broaden a reference-health clarity issue into upstream and generated-contract migrations. Producer values can remain local inputs when the shared boundary is deterministic.

#### Add or Reopen a Story

Rejected. Story 5.5 and Epic 5 remain historical implementation records, while Story 8.8 already owns parity verification.

#### Roll Back or Reduce MVP Scope

Not applicable. The defect is bounded, the direct correction is viable, and product scope does not change.

## 4. Detailed Change Proposals

### 4.1 Canonical Contract and Mapping

**Artifacts:**

- `src/Hexalith.Projects.Contracts/Models/EvidenceFreshnessState.cs` — new
- `src/Hexalith.Projects.Contracts/Models/EvidenceFreshnessStateCode.cs` — new

**CURRENT:**

There is no Projects-owned canonical Evidence Freshness type or shared conversion rule for reference-health rows. Callers pass arbitrary strings through a public `FreshnessTrustState` property.

**PROPOSED:**

- Define `EvidenceFreshnessState` with only `Current`, `Stale`, `Rebuilding`, and `Unavailable`.
- Define `EvidenceFreshnessStateCode.Normalize(string?)` as the single source-to-code mapping listed in Section 1.
- Return stable lower-case codes: `current`, `stale`, `rebuilding`, or `unavailable`.
- Treat empty and unrecognized inputs as `unavailable`.
- Optionally expose a presentation helper for the exact visible labels `Current`, `Stale`, `Rebuilding`, and `Unavailable`; do not store presentation labels in the row contract.

**Rationale:** Contracts owns the editable shared vocabulary, while adapters consume rather than redefine it.

### 4.2 Reference-Health Construction and Consumer Boundaries

**Artifacts:**

- `src/Hexalith.Projects.Contracts/Ui/ProjectReferenceHealthRowProjection.cs`
- `src/Hexalith.Projects.UI/Diagnostics/ProjectReferenceHealthMapper.cs`
- `src/Hexalith.Projects.UI/Diagnostics/ProjectWarningsDashboardMapper.cs`
- `src/Hexalith.Projects.Cli/Hexalith.Projects.Cli.csproj`
- `src/Hexalith.Projects.Cli/ProjectsCliApplication.cs`
- `src/Hexalith.Projects.Mcp/ProjectsMcpResourceReader.cs`
- `src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor`
- `src/Hexalith.Projects.UI/Components/Pages/Home.razor`

**CURRENT:**

- `ProjectReferenceHealthRowProjection.FromReferenceSummary` copies `summary.Freshness.TrustState`.
- Context and conversation enrichment use `ToString().ToLowerInvariant()`.
- Merge logic accepts whichever non-empty source value arrived last.
- Warning rows copy project or reference trust strings.
- CLI and MCP reference-health and reference-warning rows emit the generated enum code directly.
- Web surfaces display the raw code as their visible label.

**PROPOSED:**

- Keep `ProjectReferenceHealthRowProjection.FreshnessTrustState` as a string and preserve its serialized field name.
- Normalize operator summaries, context evaluations, and conversation signals before a row enters merge logic.
- Normalize both diagnostic-unavailable and reference-derived warning rows.
- Add the existing Contracts project as a CLI project reference, then use the shared string normalizer for CLI reference-health and reference-warning outputs.
- Use the same Contracts normalizer for MCP reference-health and reference-warning outputs.
- Render canonical title-cased labels on Web surfaces while retaining lower-case machine codes in projections and exports.
- Leave inventory, project-detail header, server response, generated client, and OpenAPI freshness vocabularies outside this bounded reference-health correction.

**Rationale:** Every reference-health consumer receives the same semantics, while upstream compatibility and field shape remain stable.

`ProjectSafeDiagnosticExportBuilder.cs` continues copying the already-normalized reference-health row. It must not introduce a second mapping table.

### 4.3 Documentation and UX

**Artifacts:**

- `docs/projection-catalog.md`
- `docs/parity-matrix.md`
- `docs/event-catalog.md`
- `_bmad-output/planning-artifacts/ux-design-specification.md`

**CURRENT:**

The documents name reference freshness or enumerate producer-specific states but do not define one complete boundary mapping, allowed output set, or cross-surface expectation.

**PROPOSED:**

- Add the four allowed machine codes and the complete mapping table.
- State that producer enums remain local and are normalized at the reference-health boundary.
- Require identical canonical values for Web projections, CLI output, MCP output, warnings, and safe exports.
- Define visible labels as `Current`, `Stale`, `Rebuilding`, and `Unavailable`.
- Require text in addition to any color treatment.
- Explain that authorization, redaction, mixed-generation, and diagnostic detail remains in the dedicated inclusion/health/reason/failed-check/diagnostic fields.

**Rationale:** The boundary becomes reviewable and prevents future adapters from recreating synonyms.

### 4.4 Focused Verification

**Artifacts:**

- `tests/Hexalith.Projects.Contracts.Tests/Ui/ProjectVocabularyTests.cs` and/or a focused Models test for the new normalizer
- `tests/Hexalith.Projects.UI.Tests/Diagnostics/ProjectDetailSourceTests.cs`
- `tests/Hexalith.Projects.UI.Tests/Diagnostics/ProjectWarningsDashboardSourceTests.cs`
- `tests/Hexalith.Projects.UI.Tests/Diagnostics/ProjectSafeDiagnosticExportBuilderTests.cs`
- `tests/Hexalith.Projects.UI.Tests/Components/ProjectDetailPageTests.cs`
- `tests/Hexalith.Projects.Cli.Tests/ProjectsCliApplicationTests.cs`
- `tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpResourceReaderTests.cs`
- `tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpResourceReaderFailureTests.cs` where failure-row coverage belongs

**CURRENT:**

Tests largely assert `trusted`, row counts, or safe shape. They do not exhaustively prove the source mapping, fail-closed default, merge behavior, detailed-reason preservation, or cross-surface reference-health parity.

**PROPOSED:**

- Table-test every approved source mapping, including empty, whitespace, alternate casing, and unrecognized input.
- Prove that folder/file/memory summaries, context evaluations, and every conversation signal produce only the canonical four-value set.
- Prove that enrichment and merge order cannot reintroduce producer vocabulary.
- Prove that `forbidden`, `redacted`, and `mixedGeneration` retain their dedicated inclusion, health, failed-check, reason, and diagnostic meaning.
- Assert canonical warning and safe-export reference rows.
- Assert exact CLI/MCP/Web parity for reference-health values.
- Assert exact visible labels and text-based state communication.

Run the narrow verification lanes first:

```text
dotnet test tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj --no-restore
dotnet test tests/Hexalith.Projects.UI.Tests/Hexalith.Projects.UI.Tests.csproj --no-restore
dotnet test tests/Hexalith.Projects.Cli.Tests/Hexalith.Projects.Cli.Tests.csproj --no-restore
dotnet test tests/Hexalith.Projects.Mcp.Tests/Hexalith.Projects.Mcp.Tests.csproj --no-restore
dotnet build Hexalith.Projects.slnx --no-restore
```

Record exact commands, pass counts, warnings, and failures as implementation evidence. A missing restore asset or environmental failure is a blocker to report, not a passing result.

### 4.5 Sprint Tracking and Historical Follow-Up

**Artifact:** `_bmad-output/implementation-artifacts/sprint-status.yaml`
**Section:** Epic 5 freshness-vocabulary action

**CURRENT:**

```yaml
status: in-progress
routed_to: "Stories 6.2 and 8.3"
evidence: "_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-14-implementation-readiness-correction.md"
```

**PROPOSED ON FINAL APPROVAL:**

```yaml
status: in-progress
routed_to: "Developer direct correction; Story 8.8 parity verification"
planning_evidence: "_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-31-freshness-trust-state-vocabulary.md"
evidence: "_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-14-implementation-readiness-correction.md"
```

Preserve every unrelated sprint-status change.

**PROPOSED AFTER IMPLEMENTATION AND VERIFICATION PASS:**

- Set only this action to `done`.
- Add a concise `result` with the implementation scope and dated verification results.
- Point `evidence` to the completed implementation record while retaining this proposal as `planning_evidence`.
- Check only the matching freshness-vocabulary review follow-up in `_bmad-output/implementation-artifacts/5-5-reference-inventory-health-view.md` and append its completion evidence without rewriting Story 5.5 history.

**Rationale:** Planning approval authorizes and routes the work; passing implementation evidence closes it.

## 5. Implementation Handoff

### Scope Classification

**Minor direct correction.** Product goals, MVP scope, epics, and stories remain unchanged. The change requires coordinated development, documentation, and focused parity verification.

### Recipients and Responsibilities

- **Jerome / Project Lead:** Explicitly approve, reject, or request revision of this proposal.
- **Product Owner:** Reconcile only the existing sprint action's route and planning evidence after approval; create no new story.
- **Developer (Amelia):** Add the Contracts-owned vocabulary/normalizer and apply it at every approved reference-health construction and adapter boundary without editing generated artifacts.
- **Technical Writer (Paige):** Add the mapping and parity rules to the projection, parity, and event catalogs.
- **UX owner:** Add the canonical labels and non-color-only requirement to the UX specification and ensure the Web rendering follows it.
- **Test Architect (Murat):** Verify the complete mapping, fail-closed behavior, detail preservation, and Web/CLI/MCP/warning/export parity; route the resulting evidence through Story 8.8.

### Success Criteria

1. Every reference-health `freshnessTrustState` is exactly `current`, `stale`, `rebuilding`, or `unavailable`.
2. `trusted`, `fresh`, and `current` normalize to `current`.
3. `stale` and `mixedGeneration` normalize to `stale`; `rebuilding` remains `rebuilding`.
4. `unavailable`, `unknown`, `forbidden`, `redacted`, empty, and unrecognized input normalize to `unavailable`.
5. Authorization, redaction, mixed-generation, inclusion, health, failed-check, reason, and diagnostic detail remains available in its dedicated fields.
6. Merge order cannot change the canonical meaning.
7. Web, CLI, MCP, warning rows, and safe exports agree on reference-health values.
8. Web labels are textual and use `Current`, `Stale`, `Rebuilding`, or `Unavailable`.
9. OpenAPI, generated clients, producer enums, events, persisted schemas, and unrelated freshness surfaces are unchanged.
10. All focused tests and the solution build pass before the sprint action and Story 5.5 follow-up are closed.

## Checklist Disposition

- [x] **1.1–1.3 Trigger/context:** Story 5.5 review, Epic 5 retrospective action, concrete mixed values, and merge behavior identified.
- [x] **2.1–2.5 Epic impact:** Epic 5 remains historical; no epic/story addition, removal, reopening, renumbering, reprioritization, or resequencing.
- [x] **3.1 PRD conflict:** Current mixed output conflicts with the existing canonical glossary; no PRD edit is needed.
- [x] **3.2 Architecture conflict:** Current mixed output conflicts with AD-16/AD-32; the proposed Contracts-owned normalization conforms without an architecture edit.
- [x] **3.3 UX impact:** Canonical labels and text-based state communication require a bounded UX clarification and rendering update.
- [x] **3.4 Other artifacts:** Projection/parity/event catalogs, adapters, exports, warnings, tests, story follow-up, and sprint tracking assessed.
- [x] **4.1 Direct adjustment:** Viable and selected.
- [N/A] **4.2 Rollback:** Does not address the vocabulary conflict.
- [N/A] **4.3 MVP review:** Product scope remains achievable and unchanged.
- [x] **4.4 Recommended path:** Direct canonicalization with shared mapping and parity verification.
- [x] **5.1–5.5 Proposal components:** Issue, impact, recommendation, detailed edits, MVP effect, and handoff recorded.
- [x] **6.1 Draft quality:** Internally reviewed against PRD, epics, Architecture Spine, UX, implementation evidence, source, tests, and current tracker state.
- [x] **6.2 Incremental review:** Five proposed change sets approved by Jerome.
- [x] **6.3 Explicit final approval:** Jerome approved the proposal on 2026-07-31.
- [x] **6.4 Sprint-status update:** Existing action routing and planning evidence updated; the action remains `in-progress` until implementation and focused verification pass.
- [x] **6.5 Final handoff:** Direct correction routed to the Developer, documentation to Paige, and parity verification to Murat through Story 8.8.

## Review Record

- **Mode:** Incremental
- **Drafted:** 2026-07-31
- **Change-set review signals:** `a`, `a`, `a`, `a`, `a`
- **Reviewed by:** Jerome
- **Final approval signal:** `yes`
- **Approved by:** Jerome
- **Approval date:** 2026-07-31
- **Final approval status:** Approved

## Workflow Execution Note

The default dated output path already contained a modified proposal for a different action. This proposal therefore uses the non-colliding filename `sprint-change-proposal-2026-07-31-freshness-trust-state-vocabulary.md`; the existing proposal was not overwritten.

## Approval, Handoff, and Workflow Execution Log

- **Issue addressed:** Canonicalize or explicitly document the mixed `FreshnessTrustState` vocabulary used by reference-health rows.
- **Disposition:** Canonicalize at the reference-health boundary using the approved four-state Evidence Freshness vocabulary; retain source-specific enums only as internal producer inputs.
- **Final classification:** Minor direct correction; no new or reopened epic/story and no MVP change.
- **Artifacts modified by this workflow:** This Sprint Change Proposal and the targeted existing action in `sprint-status.yaml`.
- **Artifacts intentionally unchanged during planning:** PRD, Architecture Spine, epics, UX specification, documentation catalogs, Story 5.5 implementation record, production code, tests, OpenAPI, generated clients, producer enums, persisted schemas, and every unrelated sprint-status entry.
- **Routed to:** Amelia for the direct correction, Paige for documentation, and Murat for focused parity verification through Story 8.8.
- **Handoff status:** Complete; implementation remains outstanding.
- **Closure gate:** Keep the sprint action `in-progress` and the Story 5.5 follow-up unchecked until the approved implementation succeeds and the focused verification commands pass.
