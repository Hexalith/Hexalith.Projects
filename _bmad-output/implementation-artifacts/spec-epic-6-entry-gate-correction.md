---
title: 'Apply the approved Epic 6 entry-gate correction'
type: 'chore'
created: '2026-11-02'
status: 'done'
route: 'dispatch'
baseline_commit: '710c992fe703878f20732044584c97757d6dad05'
review_loop_iteration: 0
approval: 'Jerome approved E1-E7 in this conversation on 2026-11-02'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-01.md'
---

<frozen-after-approval reason="approved E1-E7; implementation scope only">

## Intent

**Problem:** Story 6.1 and its FrontComposer consumer inherit a platform-publication and release-qualification chain unrelated to delivering authenticated list/open. The guard hard-pins both stories blocked, so prose edits alone cannot correct scheduling.

**Approach:** Apply all E1-E7 of the approved November 1 proposal together: retain two actual read/security capabilities and feature evidence; route qualification work to Epic 8/owners; supersede P4; reconcile active planning instructions and the fail-closed scheduling guard/tests. The user's approval authorizes this complete implementation; no additional proposal or approval round is needed.

## Boundaries & Constraints

**Always:** Preserve authenticated Tenant/original actor/workload/delegation, current Folder authority before paging, safe denial, AD-32, inspection audit, scoped cursors, persisted no-domain-write evidence and historical P1R decisions. Preserve exact historical Epic 1-5 inventory, all non-excepted story statuses, current-usability false and NOT_READY history. Record focused 6.1 readiness against existing seams/commands; set ready-for-dev only on that evidence, never done. Keep 6.5 blocked and DW-1 open. Retain 6.7 equivalence and Epic 7/8 release/write obligations.

**Never:** Run sprint_plan.py to write sprint-status.yaml; edit runtime/dependencies/submodules, sealed evidence or historical frozen proof intent; manufacture READY or package/production acceptance; stage, commit, branch, push or notify external owners. Capture review diffs without modifying the Git index. Preserve concurrent changes. Do not implement the list/open feature or run the whole live platform merely to apply this planning/guard correction.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Read feature progression | Exact 6.1 key; approved normal status; qualification still open | Index validates; no qualification acceptance | Missing/renamed/duplicate key, backlog or invalid status rejected |
| Web prerequisite incomplete | 6.5 progresses or DW-1 closes before all 6.1-6.4 done | Refuse with missing dependency | 6.5 also requires DW-1 done |
| Web prerequisite accepted | All four reads done, DW-1 done, exact 6.5 key and normal status | Index validates while platform actions remain open | Invalid/missing/renamed/duplicate status/key rejected |
| False acceptance | Wrong action owner epic/status, P4 done, readiness/tuple/history/usability tampering | Existing protected boundary remains closed | Nonzero guard result |

</frozen-after-approval>

## Code Map

- `tools/planning/validate_production_authority.py`: reuse strict YAML loader, exact action inventory, deferred-status parser and full snapshot validation. E5 specifies the precise new finite status sets and dependency check; preserve existing acceptance validation.
- `tests/tools/test_production_authority_guard.py`: existing open/accepted temporary workspace fixtures. Replace obsolete hard-pin tests, add combined positive/negative dependency tests, preserve acceptance atomicity coverage.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-01.md`: approved exact E1-E7 file/routing/edit inventory. Finalize approval and append actual application validation; do not recast proposed tests as historical runs.
- Active docs: `epics.md`, `epic-6-context.md`, 6.1 story/spec, 6.5 spec/DW-1, PRD/addendum, Architecture Spine, project context, traceability YAML/Markdown, Epic 6 test design, supported-read and identity docs, existing P0/P1R-remediation/P2/P3 handoffs. Reconcile active conflicting instructions; preserve dated history.
- `sprint-status.yaml`: manually prepare complete candidate with latest deferred ledger, P0 and fixed acceptance JSON. Use all four candidate guard path options before active replacement. Preserve IDs/order, reparent four qualification actions to Epic 8, supersede P4.

## Tasks & Acceptance

**Execution:**
- [x] Guard and tests -- implement E5 exactly, including dependency-aware 6.5 validation and unchanged historical acceptance controls.
- [x] Epics/context/story/specs -- apply E1/E2/E4, retain security semantics and move exhaustive migration proof to 6.7; record focused readiness.
- [x] Actions/handoffs/deferred ledger -- apply E3/E4 routing, preserve false usability and open qualification; leave DW-1 open.
- [x] PRD/Architecture/context/traceability/test design/runbooks -- apply E6/E7 so no active contradiction reinstates P0-P4 for 6.1/6.5.
- [x] Candidate and active artifacts -- manually edit, validate exact candidate bundle and active guard, preserve acceptance/history and unrelated files.
- [x] Proposal -- record approval, applied files, actual checks and remaining feature acceptance handoff.

**Acceptance Criteria:**
- Given the approved correction, when the active index is checked, then 6.1 is ready for finishing/review without publication or global READY, while 6.5 remains blocked for actual inputs and all other status boundaries remain.
- Given the matrix above, when focused guard tests execute through full snapshot validation, then each positive/negative behavior is verified with nonempty passing results.
- Given documentation consumers, when they follow active instructions, then feature readiness and 6.7/Epic 8 qualification have consistent, separate owners without false passes.

## Implementation Notes

2026-11-02: Specification derived from already approved E1-E7. No intent gaps or irreversible actions. Moderate footprint is one coupled scheduling-policy change across guard, tracking and authoritative instructions. Current root baseline is clean; existing release/package repairs have landed separately. Do not overwrite their updated evidence.

## Spec Change Log

2026-11-02: Applied approved E1–E7 across guard/tests and active artifacts; retained the frozen correction intent unchanged. Concrete source/seam/command readiness supports 6.1 ready-for-dev to finish/review; 6.5 remains blocked and DW-1 open.

## Review Triage Log

| Finding | Verdict | Route and evidence |
| --- | --- | --- |
| B1 | medium | patch: FR-2/FR-5 evidence_artifact became prose. Builds ReadinessEvidenceValidator validates one canonical relative path (HXE109) and resolves a file for execution claims; restore a concrete receipt path and describe retention separately. |
| B2 | medium | patch: FR-22 still inherits the G-4 default environment and combined hexalith-module command. Explicitly separate its 6.5 Development contribution from unchanged CLI/release obligations so the active matrix cannot reinstate E4's removed gate. |
| B3 | medium | patch: the retained exact list allowlist omits ProjectListPage.Snapshot and flattens ProjectListRow.Snapshot fields. Those existing DTOs were read directly; document the actual nested wire shape without changing runtime contracts. |
| B4 | medium | patch: the revised story's generic FR-22 recovery exception does not carry its Administrator-only/no-Setup/title/per-Project-audit limits. Restate the existing PRD exception in the active acceptance handoff; no runtime change or acceptance is inferred. |
| B5 | medium | patch: shortened tasks lost the explicit harmless-additive-event no-op classification requirement while retaining corrupt/unknown-relevant rejection. Restore that NFR-10 distinction as a concise verification requirement. |
| B6 | medium | patch: removing the G-4 command leaves 6.5 commands without native XML destinations and retains unsupported project-level MTP filtering. Specify individual Debug builds and direct assembly invocations with native XML; actual UI acceptance remains blocked. |
| B7 | medium | patch with V1: positive tests enumerate the production status constant, so policy changes alter their own expected cases. Independent literal status expectations are needed for both expansion and shrinkage. |
| B8 | low | patch: stock sprint_plan.py's action reader recognizes open/in-progress/done only, so superseded is reported unknown. Document guard plus direct/manual index inspection as the supported read-only alternative; do not run or modify its writer or expand this into a skill migration. |
| B9 | low | patch: a blank line splits the new 6.1 Change Log row from its Markdown table; direct separator removal restores rendered history. |
| B10 | low | patch: the addendum E-35 row likewise sits outside its table; remove the separator. |
| E1 | medium | patch: full-snapshot reproduction with all reads done and 6.5 ready accepts DW-1 status: done followed by a quoted/commented open field. The existing parser ignores malformed duplicate fields; count every status field before validating its one canonical value and retain negative controls. |
| V1 | medium | patch with B7: reviewer removed review from READ_FEATURE_STATUSES in memory and all 45 tests stayed green while a review snapshot failed. Independent allowed-status tuples close this demonstrated verification gap. |

## Verification

Actual application on 2026-11-02: 46 focused guard tests pass after review repairs; both independent positive progression tests reject the in-memory removal of review (two expected failures); candidate and active index/6.1/6.5 authority checks pass; protected-history/acceptance/remaining-ledger comparisons, strict YAML/frontmatter and new local-link checks pass. Candidate uses all four companion path options. See [actual application record](../planning-artifacts/sprint-change-proposal-2026-11-01.md#7-approved-application--2026-11-02) for commands, scope and remaining feature acceptance. No .NET/live qualification was run for this correction.

- `python3 -m unittest discover -s tests/tools -p test_production_authority_guard.py`: all tests pass, including new negative controls.
- `python3 tools/planning/validate_production_authority.py --validate-index` and `--story-id 6.1` / `--story-id 6.5`: pass against exact candidate and active companion set.
- `git diff --check`: pass; YAML/frontmatter/local links and cross-artifact statuses/routing consistent; no sprint_plan.py writer used.

### Final verification — 2026-11-02

All twelve review findings were individually triaged and resolved by focused patches; none deferred. The final independent guard suite passed 46 tests. Both progression tests reject the missing-review policy mutation. Conflicting quoted/commented DW-1 statuses now fail closed; accepted progression remains independent of qualification. Final candidate /tmp/epic6-entry-final-ct_y8bxn and active index/story 6.1/story 6.5 checks pass. Protected-history comparisons, canonical evidence-path and Web/CLI separation checks, YAML/link validation and git diff --check pass; Git index remains untouched. No runtime/dependency/submodule change, live feature acceptance, commit, push or sprint_plan.py writer. The course-correction implementation is done; Story 6.1 itself remains ready-for-dev, Story 6.5 blocked and DW-1 open.
