---
title: 'Story 6.1: List and open Projects through supported authenticated paths'
type: 'feature'
created: '2026-10-02'
status: 'draft'
route: 'dispatch'
updated: '2026-10-09'
baseline_commit: '8de2e5bb2600fde2565f06638d51cd2eba4a4c53'
review_loop_iteration: 0
story_key: '6-1-list-and-open-projects-through-supported-authenticated-paths'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Authenticated callers lack supported DomainService list/open reads with authorization-filtered Project truth and the shared admission snapshot.

**Approach:** After the entry gate, add list/open contracts, watermarked models, authenticated handlers, and shadow comparison beside legacy REST. Queries make no Project domain writes.

**Decision:** Use the full Epic 6.1 scope: Chatbot Folder-read filtering, inspection-gated names with durable FR-21 audit, and shadow equivalence.

## Boundaries & Constraints

**Always:** Require P0, usable current P1R, P2, P3, same-baseline architect sign-off, P4 clean-checkout acceptance, complete approved spec, and independent `READY` before runtime work. P4 pins Folder authority, inspection/audit, adapter, and finite normalization contracts. Bind Tenant, original actor, workload, delegation, audience, and action from the authenticated envelope. Authorize before protected validation; reuse platform store/cursor/denial seams; one C# type per file.

**Never:** While `blocked` or `NOT_READY`, do not edit runtime/tests. Do not switch `/api/v1/projects`, hand-roll persistence/cursors, rewrite events, expose unaudited names/protected detail, select candidates, mutate Project state, move pins, or trust caller Tenant headers.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Gate open | Missing prerequisite | No runtime/test/pin edit | Remain blocked |
| Authorized | Current evidence | AD-32 snapshot; default 50/cap 200 list and stable cursor; role-specific visibility | Safe `400` after authorization for structural errors |
| Denied/absent | Denied, cross-Tenant, nonexistent | No protected data | Indistinguishable `404` |
| Degraded | Stale/rebuilding/unknown event | Honest `Partial`/`Unavailable`; no write | `Unavailable` blocks context |
| Shadow | Same authorized query on both paths | Approved canonical result matches | Unexplained delta fails |

**Prerequisite decision (2026-10-03):** Start separate P1R compatibility, replay, and rollback verification for accepted EventStore 3.110.0 / Builds 4.29.1. Story 6.1 remains pending its entry gate.

</frozen-after-approval>

## Code Map

- `_bmad-output/implementation-artifacts/sprint-status.yaml` — exact gate: current P1R unusable; P0/P2/P3/P4, architect signature, and independent `READY` pending. Story is blocked.
- `src/Hexalith.Projects/Projections/{ProjectList,ProjectDetail}/` — reuse deterministic folds; supported persisted envelopes need authoritative watermark.
- `src/Hexalith.Projects.Server/Queries/` — reuse envelope checks; domain handlers need the approved principal adapter.
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` — retain legacy list/open; `ProjectsServerServiceCollectionExtensions.cs` registers `/query`.
- `tests/Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.cs` and `tests/Hexalith.Projects.Tests/Replay/ProjectionRebuildDeterminismTests.cs` — retain regressions; fake stores do not prove persistence.

## Tasks & Acceptance

**Execution:**

- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` — check current entry gate; no runtime/test edit while blocked.
- [ ] `evidence/epic6/6.1-entry-gate.yaml` — consume owner-accepted P0/P1R/P2/P3/P4, architect signature, approved spec and independent `READY`.
- [ ] `src/Hexalith.Projects.Contracts/Queries/` — add additive list/open contracts and AD-32 snapshot with list-only 50/200 paging.
- [ ] `src/Hexalith.Projects/Projections/{ProjectList,ProjectDetail}/` — add incremental persisted models over existing folds; classify events and expose honest faults.
- [ ] `src/Hexalith.Projects/Queries/Handlers/` and `src/Hexalith.Projects.Server/` — implement/register authenticated reads using approved Folder authority, inspection/audit, store and cursor seams; preserve legacy route.
- [ ] `src/Hexalith.Projects.Testing/Reads/` — compare outputs, keys, watermarks, cursors and Tenant isolation using P4 rules.
- [ ] `tests/Hexalith.Projects.Server.Tests/Queries/` and `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadTests.cs` — test matrix, persisted replay/restart, safe denial, leakage, cursors and zero Project writes; generate real G-4 evidence.

**Acceptance Criteria:**

- Given authorized current evidence, when list/open runs, then AD-32 Safe Metadata is scoped; Chatbot Folder reads filter before paging; inspection-gated names cause a metadata-only durable FR-21 audit; Archived blocks context use.
- Given denied, cross-Tenant or absent identity, when open runs, then indistinguishable safe `404` discloses nothing protected.
- Given stale, incomplete or unknown-event evidence, when authorized reads run, then `Partial`/`Unavailable` is honest and no Project state changes.
- Given persisted shadow comparison, when any unapproved delta appears, then qualification fails and legacy routing remains active.

## Implementation Notes

## Spec Change Log

- 2026-10-06: Jerome approved the P1R course correction. Restored explicit Folder-before-pagination, inspection-gated names with durable FR-21 audit, and indistinguishable denial criteria in the non-frozen section. The frozen intent/entry gate, Story status, runtime/tests and pins remain unchanged. These cases become executable only after all existing entry prerequisites pass.

## Review Triage Log

## Verification

- `python3 tools/planning/validate_production_authority.py --story-id 6.1` — authority scope only, not readiness.
- After accepted gates: Debug build, Server/Integration tests and G-4 persisted reads profile with actual `.trx`, JSON and shadow reports.
