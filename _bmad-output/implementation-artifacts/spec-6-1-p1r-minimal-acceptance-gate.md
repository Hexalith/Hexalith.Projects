---
title: '6.1-P1R Minimal Acceptance Gate'
type: 'refactor'
created: '2026-09-22'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '6e6b03969d9d526bc1c913e25e67f14d1a9c0126'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The 6.1-P1R gate has grown into 47 attempt bundles, retained workspaces and packages, layered ledgers and manifests, replay tooling, custom YAML parsing, and attempt-specific documentation. This obscures the actual production decision and consumes about 1 GiB without providing a maintainable acceptance boundary.

**Approach:** Remove the 3.106 attempt system and replace it with one optional, small JSON acceptance record plus a focused production-authority guard. Because no valid minimal record exists, leave P1R open and create no placeholder record.

## Boundaries & Constraints

**Always:** Before deletion, enumerate and confirm the exact 47 roots `6-1-p1r-3106-20260921-attempt1` through `attempt47` as P1R-only. Preserve the selected EventStore `3.106.0` / `v3.106.0` / `76051c70cbf868c40edc00ca0344fa5bd8879b69` and Builds `ad52f350a2f0bc47849179ae17b4594dafff5363` coordinates and rollback EventStore `3.70.1` / `v3.70.1` / `f13f9925fdca53efa2ab8c90d396ab106f91bb9c` and Builds `7af20f8bafbfe561df6f7705913a0800603090b5` coordinates. Parse sprint YAML with repository-supported PyYAML and acceptance JSON with the standard library. Each of the four role decisions must explicitly name the exact record path and reference its `selected` tuple.

**Never:** Create Attempt 48, a placeholder acceptance record, another bundle/ledger/manifest/snapshot, or retained dependency copy. Do not parse Markdown as YAML, build a YAML parser, change product/runtime code, broaden deletion to older P1R or G-6 evidence roots, add an unneeded dependency, commit, or push.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Open gate | Acceptance JSON absent and guarded items open/blocked | Current index passes; P1R stays open | Any acceptance-only closure fails |
| Valid acceptance | Exact schema, coordinates, timestamp, and four named accepts | Only P1R, P0 Stage 1, DW-35, and DW-68 may close | Any other closure fails |
| Role failure | One role missing or rejected | Acceptance fails | Name the invalid role |
| Coordinate mismatch | Record differs from sprint-selected tuple | Acceptance fails | Report the mismatched coordinate |
| Malformed record | Invalid JSON or wrong field shape | Acceptance fails closed | Report a concise validation error |

</frozen-after-approval>

## Code Map

- `tools/planning/validate_production_authority.py` -- retain production-epic/history scheduling authority; replace attempt, Markdown-table, provenance, and custom-YAML machinery with the minimal gate.
- `tests/tools/test_production_authority_guard.py` -- replace bundle synthesis with the five focused acceptance cases while retaining ordinary scheduling coverage.
- `tools/planning/verify_p1r_postacceptance.py`, `tests/tools/test_p1r_postacceptance_guard.py` -- evidence-only verifier and suite to delete.
- `.gitignore`, `.gitattributes`, `.github/workflows/ci.yml` -- remove attempt-only rules/references and make pinned PyYAML available to the guard lane.
- `_bmad-output/implementation-artifacts/{sprint-status.yaml,deferred-work.md,6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md,spec-6-1-p1r.md}` -- authoritative open state, explicit tuples, and permitted closure boundary.
- `_bmad-output/project-context.md`, `_bmad-output/implementation-artifacts/qualification-evidence/README.md`, `_bmad-output/specs/spec-6-1-p1r-revalidation-owner-acceptance/{SPEC.md,qualification-contract.md}` -- concise process documentation without attempt histories or hash tables.
- `_bmad-output/implementation-artifacts/{spec-6-1-p1r-revalidation-owner-acceptance.md,spec-6-1-p1r-revalidate-platform-baseline-2.md,spec-6-1-p1r-pragmatic-qualification-storage.md}` -- mark obsolete qualification/storage flows as superseded so they cannot trigger another attempt; preserve older evidence roots and unrelated historical documents.

## Tasks & Acceptance

**Execution:**
- [x] Delete only the confirmed 47 attempt roots, the standalone post-acceptance verifier/test, ignored caches, and attempt-specific Git/CI rules.
- [x] Rewrite the guard around PyYAML plus an optional fixed-path JSON record whose exact keys cover schema, selected/rollback tuples, UTC timestamp, and four role decisions with approvers and explicit record/tuple references.
- [x] Rewrite focused unit tests for valid acceptance, missing/rejected role, coordinate mismatch, unauthorized closure, and malformed JSON; retain compact production-authority scheduling tests.
- [x] Simplify the P1R/P0/tracking/context/spec/README artifacts, remove attempt history and hashes, and keep P1R, P0 Stage 1, DW-35, and DW-68 open because no valid record exists.

**Acceptance Criteria:**
- Given the reset workspace, when references are scanned, then no targeted attempt root, runner, ledger, manifest, retained-copy rule, or post-acceptance verifier remains.
- Given the current record-absent state, when the production-authority guard runs, then it passes the open boundary without accepting P1R.
- Given a valid four-role record in tests, when only the four authorized items close, then validation passes while P0 stages 2–7, P2/P3/P4, Story 6.1, readiness, and dependent Epic 7/8 work remain open or blocked.
- Given any focused negative fixture, when validation runs, then it fails closed without reading Markdown as YAML.

## Implementation Notes

- Confirmed all 47 exact P1R roots before moving them to recoverable trash;
  older P1R and G-6 evidence roots were left untouched.
- The fixed optional record is
  `_bmad-output/implementation-artifacts/6-1-p1r-acceptance.json` with schema
  `hexalith.projects.p1r-acceptance.v1`. No record was created.
- The guard uses PyYAML with duplicate-key rejection only for sprint YAML,
  extracts named Markdown/frontmatter fields without YAML parsing, and uses
  standard-library JSON with duplicate-key rejection for acceptance.

## Spec Change Log

- 2026-09-22: Replaced the attempt system with the minimal optional acceptance
  record, retained the open boundary, and preserved unrelated deferred work.

## Review Triage Log

- `medium` — verification-gap: rollback-coordinate rejection is implemented but untested; removing the rollback comparison leaves the suite green, so add focused negative cases.
- `medium` — verification-gap: approver, record-path, selected-pointer, and timestamp checks have no negative coverage; regressions could admit an unbound role decision.
- `medium` — verification-gap: the accepted fixture closes every authorized representation at once; independently leaving P1R, either P0 Stage 1 representation, DW-35, or DW-68 open is not tested.
- `medium` — verification-gap: downstream non-closure is exercised only through Story 6.1; P0 stages 2–7, P2/P3/P4, readiness, and Epic 7/8 need representative negatives.
- `medium` — verification-gap other: the accepted sprint fixture retains `decision`/`result` prose that says the record is absent and the gate is open, so the test currently blesses contradictory planning truth.
- `false` — blind-hunter: closing from role attestations without retained build/test evidence is the explicitly approved minimal-gate design; adding evidence references would recreate the retired attempt boundary.
- `false` — blind-hunter: every role intentionally references `#/selected`; the frozen requirement does not require a second rollback pointer, while the record-level rollback tuple is validated exactly.
- `low` — blind-hunter: the companion SPEC promises “fresh” decisions although the approved schema defines only a canonical record timestamp; remove that unsupported adjective rather than inventing a freshness window.
- `false` — blind-hunter: the frozen contract requires four role decisions with named approvers, not four distinct people, so the shared-approver fixture is valid.
- `medium` — blind-hunter: `Path.is_file()` follows symlinks, so an external target can satisfy the fixed record path; reject symlinks explicitly.
- `medium` — blind-hunter: story mode accepts companion-path/workspace options but ignores them, so it can validate a different gate state than the caller supplied.
- `medium` — blind-hunter: story mode validates the sprint file and then rereads it, losing the previous single-snapshot property; reuse one parsed validated snapshot.
- `medium` — blind-hunter: the agent context documents only `--sprint-status` for candidate validation even though accepted transitions span four inputs; this context-file correction must be deferred by review policy.
- `medium` — blind-hunter: accepted-state fixtures preserve open-state sprint narratives; remove or validate those state-bearing prose fields so structured acceptance cannot contradict them.
- `medium` — blind-hunter: the Story 6.5 status-mismatch obligation was removed with DW-68 even though the spec remains `blocked` and sprint status remains `backlog`; restore the deferred item.
- `medium` — blind-hunter: the retained `6-1-p1r-397-*` roots still contain 23 untracked logs, so their durability obligation remains real and must stay deferred.
- `medium` — blind-hunter: four retained historical manifests still lack executable re-hash coverage; removing that deferred obligation erased unresolved work.
- `medium` — blind-hunter: eight retained packages still have no retention/LFS decision; preserve the policy obligation for the older evidence roots.
- `medium` — blind-hunter: the retained 3.102 bundle still has no machine-readable command ledger; preserve the open policy decision rather than treating it as resolved.
- `false` — blind-hunter: the specified CI guard lane creates a dedicated environment and installs pinned PyYAML before running; the change does not claim a standalone dependency-free Python entry point.
- `low` — blind-hunter: a hash-locked PyPI artifact would harden supply chain, but version-pinned TLS-backed installation is the repository’s existing pattern and changing it is disproportionate to this gate.
- `false` — blind-hunter: a deletion tombstone or inventory is explicitly forbidden by the frozen “no bundle/ledger/manifest/snapshot” boundary; recoverable trash and the controlling spec retain the scope.
- `low` — blind-hunter: the human-readable `# last_updated` comment is stale after the parsed field moved to 2026-09-22; synchronize the comment.
- `low` — blind-hunter: the exact acceptance shape is prose-only; a non-persisted canonical JSON example is a direct usability improvement and does not create a placeholder record.
- `low` — edge-case-hunter: invalid UTF-8 in planning files escapes `_read_text` as a traceback because `UnicodeError` is not caught; convert it to `GuardViolation`.
- `medium` — edge-case-hunter: later P0 capability and blocker fields are not guarded, so owner acceptance could advance while only Stage 1 is authorized; pin those explicit fields.
- `low` — edge-case-hunter: the four files can change during sequential reads, but this local one-shot workflow has no demonstrated concurrent writer and locking/retry machinery would be disproportionate.
- `medium` — edge-case-hunter: the second sprint read in story mode creates an avoidable validation/use race; reuse the validated object.
- `medium` — edge-case-hunter: alternate companion paths and workspace root are ignored in story mode; thread them through the same snapshot validation.
- `medium` — edge-case-hunter: accepted P1R can coexist with open-state `decision` and `result` fields; eliminate that contradictory state-bearing prose.
- `medium` — edge-case-hunter: five unresolved obligations were deleted with the old DW-68 narrative; restore them as independent deferred entries while keeping the minimal gate.

## Verification

The focused suite covers every I/O matrix row and passed 18 tests. The current
record-absent index also passed while retaining the open boundary.

**Commands:**
- `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests/tools/test_production_authority_guard.py -v` -- expected: focused guard suite passes.
- `python3 tools/planning/validate_production_authority.py --validate-index` -- expected: current P1R-open index passes.
- `rg '6-1-p1r-3106-20260921-attempt|verify_p1r_postacceptance|test_p1r_postacceptance_guard' . --hidden -g '!/.git/**'` -- expected: no live-system references remain.
- `git diff --check` -- expected: no whitespace errors.

**Observed:**
- Focused guard suite: 18 tests passed.
- Current index: `PASS`, production-authority epics `[6, 7, 8]`.
- Reference scan: only the three controlling-spec self-references remain; no
  live-system reference remains.
- Whitespace validation: passed.
