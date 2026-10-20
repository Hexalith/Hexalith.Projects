---
title: 'Story 6.4 prerequisite: current authorized resolution evidence'
type: 'feature'
created: '2026-10-19'
status: 'draft'
route: 'dispatch'
artifact_kind: 'prerequisite-design-for-review'
related_story: '6-4-resolve-projects-with-transient-current-explanations'
implementation_authorized: false
baseline_commit: 'b0caf2b5c25a1f761abed685c7c9ef811732c6fb'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/spec-6-4-resolve-projects-with-transient-current-explanations.md'
  - '_bmad-output/implementation-artifacts/6-4-resolution-evidence-contracts.md'
---

<frozen-after-approval reason="proposed prerequisite scope; owner decisions remain unapproved">

## Intent

**Problem:** Story 6.4 cannot obtain content-free Conversation metadata, authoritative opaque File evidence, or current actor-scoped Reference Trust Index (RTI) evidence through the inspected contracts.

**Approach:** Establish one approved evidence boundary using existing EventStore owner transport, Folder authorization and Projects read models, with the minimum additive owner contracts and one RTI. The [contract annex](6-4-resolution-evidence-contracts.md) specifies signatures, files, semantics, tests and unresolved owner decisions. This deliverable is a review draft; implementation requires separate owner scopes.

## Boundaries & Constraints

**Always:** Preserve Story 6.4's frozen decisions, blocked tracking, scoring, unselected candidates and prerequisites-first gate. Bind Tenant, actor, workload and delegation; require current owner authority before disclosure. Keep metadata names inside authorized scoring and omit `DisplayName` from candidates and exclusions. Reuse AD-32 and supported persisted readers.

**Never:** Fetch content/messages/paths/URIs/workspaces/secrets; use inspection-auditing reads, cached grants or the legacy reverse index as authority; persist query inference or repair; change packages, gitlinks, generated artifacts, legacy routes, sprint state or acceptance. No sibling implementation, qualification or owner approval is authorized by this draft.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Current | Complete supported topology and current actor-authorized owner evidence | Eligible metadata reaches existing mappers | No automatic selection or query writes |
| Protected | Denied, absent or cross-Tenant protected target | Indistinguishable safe 404 | No protected identity or existence detail |
| Degraded | Missing/incomplete/stale/rebuilding evidence or changed authority | Minimal AD-32 `Unavailable` after authority is established | Never fabricate `NoMatch` |
| Optional | Required evidence current; omission disclosure authorized | Explicit `Partial` | No protected exclusion identity |
| Reordered/oversized | Duplicate/permuted or over-budget input | Deterministic deduplication or bounded failure | No silent truncation or unbounded fan-out |

</frozen-after-approval>

## Open Questions

The annex's D1–D6 record accountable owners, recommended choices, alternatives and consequences. All remain open: delegated owner authority, File identity, RTI production/completeness, finite work budgets, scoring-label/denial policy, and immutable publication/compatibility. None may be inferred from this specification. Status remains `draft` until those decisions have usable evidence.

## Code Map

- `src/Hexalith.Projects.Server/Projections/ProjectReads/` — reuse persisted detail/index, ETags, atomic projection/rebuild and keys; `Current` alone proves no owner authority.
- `src/Hexalith.Projects.Server/Queries/ProjectContextQueryExecutor.cs` — reuse two-pass authorization pattern; its pre-RTI fallback is insufficient.
- `src/Hexalith.Projects.Server/Queries/ProjectReadQueryExecutor.cs` — reuse validation ideas only; its inspection audit disqualifies invocation.
- `src/Hexalith.Projects/Resolution/` — retain existing engine, mappers and weights.
- Annex §§2–6 — exact owner source paths and proposed additions; distinguish supported APIs from approval candidates.

## Tasks & Acceptance

**Execution after owner approval, in dependency order:**

- [ ] Conversations owner — implement the narrow metadata key/read and delegated handler in annex §3; no detail hydration.
- [ ] Folders owner — approve receipt compatibility and stable File identity, then deliver §4; reuse singleton Folder authorization.
- [ ] EventStore/deployment owners — validate existing bound-owner transport and exact allowlist; address only the demonstrated identity gap in §2.
- [ ] Projects owner — deliver the sole RTI producer/rebuild and actor-scoped reader in §5, using supported storage; no changes to the Story 6.4 resolution handlers yet.
- [ ] Owner test suites — execute annex §8's contract, isolation, freshness, leakage and projection controls; retain exact revisions and results.
- [ ] Existing `6.1-P0` work — independently deliver G-4 qualification; do not create a substitute runner here.

**Acceptance Criteria:**

- Given approved owner contracts, when metadata reads execute, then only the annex allowlist is retrieved and every admitted row has current resource and actor authority.
- Given actor, Tenant, workload, delegation, permission or owner revision changes, when final disclosure is checked, then previous grants cannot authorize the result.
- Given complete empty versus uninitialized RTI evidence, when read, then only the former can establish absence of matches.
- Given owner faults, malformed responses or bounds violations, when normalized, then the matrix holds without protected identities or invented freshness.
- Given either eventual resolution query, when persisted state is compared through G-4, then domain, read-model, RTI, trace, task, repair and inspection-audit contents are unchanged.

## Implementation Notes

No implementation performed. This planning request stops at the reviewable draft; it does not resume the parent `ready-for-dev` spec. The two new documents are the entire authorized footprint.

## Spec Change Log

## Review Triage Log

## Design Notes

One evidence prerequisite spans owner repositories. The annex carries detailed contracts so the execution summary stays short. Unknown storage/identity authority is a blocking decision, never a permanent unavailable stub presented as delivery.

## Verification

- `python3 tools/planning/validate_production_authority.py --story-id 6.4` — planning authority only; no implementation readiness claim.
- `git diff --check` and document checks — validate this documentation change.
- Compare tracked-file hashes and repository state with the pre-write snapshot; Story 6.4, sprint, pins, owners and legacy routes must remain unchanged.
- Application tests and G-4 runs are future prerequisite evidence, not claimed by this draft. See annex §§8–9.
