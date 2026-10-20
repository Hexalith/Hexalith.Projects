---
title: 'Story 6.4 prerequisite: current authorized resolution evidence'
type: 'feature'
created: '2026-10-19'
updated: '2026-10-20'
status: 'draft'
route: 'dispatch'
artifact_kind: 'selected-prerequisite-design'
related_story: '6-4-resolve-projects-with-transient-current-explanations'
design_decisions: 'selected'
implementation_authorized: false
baseline_commit: 'b0caf2b5c25a1f761abed685c7c9ef811732c6fb'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/spec-6-4-resolve-projects-with-transient-current-explanations.md'
  - '_bmad-output/implementation-artifacts/6-4-resolution-evidence-contracts.md'
---

<frozen-after-approval reason="human-selected recommendations; implementation evidence remains incomplete">

## Intent

**Problem:** Story 6.4 cannot obtain content-free Conversation metadata, authoritative opaque File evidence, or current actor-scoped Reference Trust Index (RTI) evidence through the inspected contracts.

**Approach:** Reuse EventStore transport, Folder authorization and Projects read models with narrow owner contracts and one RTI. The [contract annex](6-4-resolution-evidence-contracts.md) specifies the selected design and outstanding source evidence.

**Decision (2026-10-20):** The user instructed “apply recommended,” selecting D1–D6 in the [decision record](6-4-prerequisite-decisions-2026-10-20.md). Existing implementation and qualification gates remain.

## Boundaries & Constraints

**Always:** Preserve Story 6.4's frozen decisions, blocked tracking, scoring, unselected candidates and prerequisites-first gate. Bind Tenant, actor, workload and delegation; require current owner authority before disclosure. Keep metadata names inside authorized scoring and omit `DisplayName` from candidates and exclusions. Reuse AD-32 and supported persisted readers.

**Never:** Fetch content/messages/paths/URIs/workspaces/secrets; use inspection-auditing reads, cached grants or the legacy reverse index as authority; persist query inference or repair; change packages, gitlinks, generated artifacts, legacy routes, sprint state or acceptance. Selected designs do not establish missing source capabilities or qualification.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Current | Complete supported topology and current actor-authorized owner evidence | Eligible metadata reaches existing mappers | No automatic selection or query writes |
| Protected | Denied, absent or cross-Tenant protected target | Indistinguishable safe 404 | No protected identity or existence detail |
| Degraded | Missing/incomplete/stale/rebuilding evidence or changed authority | Minimal AD-32 `Unavailable` after authority is established | Never fabricate `NoMatch` |
| Optional | Required evidence current; omission disclosure authorized | Explicit `Partial` | No protected exclusion identity |
| Reordered/oversized | Duplicate/permuted or over-budget input | Deterministic deduplication or bounded failure | No silent truncation or unbounded fan-out |

</frozen-after-approval>

## Implementation Evidence Required

D1–D6 directions are selected. Readiness still needs named File/feed APIs, native revision mappings, production binding/enrollment, full-result/performance/compatibility proof, and exact-current G-4 and shared prerequisite acceptance. Missing source contracts remain blockers; no provider or passing result is inferred.

## Code Map

- `src/Hexalith.Projects.Server/Projections/ProjectReads/` — reuse persisted detail/index, ETags, atomic projection/rebuild and keys; `Current` alone proves no owner authority.
- `src/Hexalith.Projects.Server/Queries/ProjectContextQueryExecutor.cs` — reuse two-pass authorization pattern; its pre-RTI fallback is insufficient.
- `src/Hexalith.Projects.Server/Queries/ProjectReadQueryExecutor.cs` — reuse validation ideas only; its inspection audit disqualifies invocation.
- `src/Hexalith.Projects/Resolution/` — retain existing engine, mappers and weights.
- Annex §§2–9 — owner paths, selected directions and missing implementation evidence.

## Tasks & Acceptance

**Execution after source contracts are ready:**

- [ ] EventStore/deployment owners — deliver §2's production binding and verified-origin contract; name exact enrollment configuration.
- [ ] Folders owner — establish §4's actual stable File source/mapping and native receipt evidence; verify compatibility.
- [ ] Conversations owner — implement the narrow metadata key/read and delegated handler in annex §3; no detail hydration.
- [ ] Projects owner — deliver §5's sole RTI, complete inverse/name lookup and actor-scoped reader; no resolution handlers yet.
- [ ] Owner test suites — execute annex §8's contract, isolation, freshness, leakage and projection controls; retain exact revisions and results.
- [ ] Existing `6.1-P0` work — independently deliver G-4 qualification; do not create a substitute runner here.

**Acceptance Criteria:**

- Given approved owner contracts, when metadata reads execute, then only the annex allowlist is retrieved and every admitted row has current resource and actor authority.
- Given actor, Tenant, workload, delegation, permission or owner revision changes, when final disclosure is checked, then previous grants cannot authorize the result.
- Given complete empty versus uninitialized RTI evidence, when read, then only the former can establish absence of matches.
- Given owner faults, malformed responses or bounds violations, when normalized, then the matrix holds without protected identities or invented freshness.
- Given either eventual resolution query, when persisted state is compared through G-4, then domain, read-model, RTI, trace, task, repair and inspection-audit contents are unchanged.

## Implementation Notes

2026-10-19: Initial draft and annex; no implementation.

2026-10-20: Applied selected recommendations to this draft and annex; added the decision record. Historical preflight evidence is unchanged. Parent implementation remains gated.

## Spec Change Log

- 2026-10-20 — Recorded D1–D6 selections; corrected transport/raw-count assumptions and simplified receipt/ingestion scope. Preserve parent gates and scoring.

## Review Triage Log

## Design Notes

One evidence prerequisite spans owner repositories. The annex carries detailed contracts so the execution summary stays short. Unknown storage/identity authority is a blocking decision, never a permanent unavailable stub presented as delivery.

## Verification

- `python3 tools/planning/validate_production_authority.py --story-id 6.4` — planning authority only; no implementation readiness claim.
- `git diff --check` and document checks — validate this documentation change.
- Compare tracked-file hashes and repository state with the pre-write snapshot; Story 6.4, sprint, pins, owners and legacy routes must remain unchanged.
- Application tests and G-4 runs are future prerequisite evidence, not claimed by this draft. See annex §§8–9.
