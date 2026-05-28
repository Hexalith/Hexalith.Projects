# Mutation & Query Negative-Test Checklist

> **Source:** Epic 2 retrospective Action Item 7 (promoted from per-story practice to a repo-level
> canonical checklist as part of Story 3.2). Every Epic 3+ mutation and query story must reference
> this list explicitly in its Dev Agent Record and tick each applicable row.

Story 1.4–2.7 already exercised every row below per-story; the goal of this checklist is to make the
pattern visible across the repo so future story creators and reviewers do not have to re-derive it.
The rows are exhaustive for the current Hexalith.Projects HTTP surface — additional rows belong here
when a new shared invariant emerges.

## Canonical Rows

For each row: name the test, name the surface (endpoint), and assert the documented outcome. The
"AC reference" column points at the story where the row originated. The "When applicable" column
notes which surface category (mutation / query / both) must tick it.

| # | Row | When applicable | Expected outcome | AC reference |
|---|-----|-----------------|------------------|--------------|
| 1 | Malformed identifier (whitespace / NUL / control bytes / unicode bidi / `..` / leading-trailing whitespace / extra path segments). | Mutation + Query | Safe-denial **404** (queries) — never reveals existence. Mutations → **400 validation_error** with field name. | Stories 1.4, 2.3, 2.5, 2.7, 3.2 (AC 9) |
| 2 | Route ↔ body identity mismatch (route `projectId` ≠ body `projectId`, route `fileReferenceId` ≠ body `fileReferenceId`, …). | Mutation | **400 validation_error** with field `identity` BEFORE handler dispatch / before any sibling ACL call. | Stories 2.3, 2.5, 2.7 (AC 9) |
| 3 | Missing `Idempotency-Key` on mutation. | Mutation | **400 validation_error** with field `idempotency_key`. Order: read header → validate before authorize. | Stories 1.4, 2.3, 2.5, 2.7 |
| 4 | `Idempotency-Key` PRESENT on query. | Query | **400 validation_error** with field `idempotency_key` AFTER authorization (mirrors `GetProject`). Unauthorized callers receive only safe-denial 404. | Stories 1.7, 3.2 (AC 8) |
| 5 | Non-`eventually_consistent` `X-Hexalith-Freshness` requested on an eventually-consistent query. | Query | **400 validation_error** with field `freshness` AFTER authorization. | Stories 1.7, 3.2 (AC 4f) |
| 6 | Cross-tenant scope (tenant-A claim, tenant-B resource). | Mutation + Query | Safe-denial **404** (NEVER 403). The response Problem Details body never echoes the requested tenant id or any resource metadata. Reuses the FS-8 / SM-3 cross-tenant harness. | Stories 1.4, 1.6, 3.1, 3.2 (AC 11) |
| 7 | Unknown / mismatched `Idempotency-Key` on a mutation retry (same fingerprint, different idempotency key OR different fingerprint, same idempotency key). | Mutation | **409 IdempotencyConflict** with the canonical problem-details oracle. | Stories 1.4, 2.3, 2.5, 2.7 |
| 8 | Authorized but stale / unavailable projection (`ReferenceState.Unavailable && Retryable`). | Query | **503 ReadModelUnavailable** with `Retryable=true`. Mutations apply the analogous retryable rejection path (see 1.4 mutation flow). | Stories 1.4, 1.7, 3.2 (AC 4d) |

## How to apply

When closing a story's Dev Agent Record, copy the following block and tick the applicable rows. If a
row does not apply (e.g. row 2 on a query without a body), record "N/A".

```
Negative-test checklist (docs/checklists/mutation-and-query-negative-tests.md):
[ ] 1 Malformed identifier
[ ] 2 Route/body identity mismatch
[ ] 3 Missing Idempotency-Key
[ ] 4 Idempotency-Key on query
[ ] 5 Stricter freshness on eventually-consistent query
[ ] 6 Cross-tenant scope
[ ] 7 Unknown Idempotency-Key retry
[ ] 8 Stale/unavailable projection
```

## Story 3.2 reference application

Story 3.2 (`Get Project Context`) — a query — applies rows 1, 4, 5, 6, 8. Row 2 is N/A (no body),
rows 3 and 7 are N/A (mutation-only).

- Row 1 (malformed identifier) → `GetProjectContext_MalformedProjectId_ReturnsSafeDenial404`
  (xUnit Theory across whitespace / `..` / control bytes / etc.).
- Row 4 (Idempotency-Key on query) → `GetProjectContext_IdempotencyKeyPresent_ReturnsValidationProblem`
  + `GetProjectContext_IdempotencyKeyPresentAndUnauthorized_ReturnsSafeDenial404` (ordering proof).
- Row 5 (stricter freshness rejected) → `GetProjectContext_StricterFreshnessRequested_ReturnsValidationProblem`.
- Row 6 (cross-tenant) → `GetProjectContext_CrossTenant_ReturnsSafeDenial404`.
- Row 8 (stale/unavailable projection) → covered through the `ReadModelUnavailable` branch in
  `GetProjectContextAsync` (`authorization.Retryable && Reason == Unavailable`).

## Maintenance

- Append a new row only when a load-bearing invariant is shared across two or more surfaces.
- Never delete a row without a corresponding ADR explaining why the invariant no longer holds.
- The architecture pointer for this checklist is cross-linked from
  `_bmad-output/planning-artifacts/architecture.md` near the AR-9 fail-closed posture / Process
  Patterns section (Story 3.2, Task 12).
