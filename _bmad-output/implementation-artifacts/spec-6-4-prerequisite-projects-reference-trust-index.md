---
title: 'Projects RTI prerequisite: complete current candidate evidence'
type: 'feature'
created: '2026-10-20'
status: 'draft'
route: 'dispatch'
artifact_kind: 'separately-scoped-rti-prerequisite'
readiness: 'blocked-owner-source-contracts'
baseline_commit: '16886b7d9c15b5356fad860b0d7b0f97eb9f32b3'
revalidated_commit: '02ce53a039d2fd3c87b7c7232662eaa89ec2ddd7'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/6-4-prerequisite-decisions-2026-10-20.md'
  - '_bmad-output/implementation-artifacts/6-4-resolution-evidence-contracts.md'
---

<frozen-after-approval reason="user-directed separate RTI scope; development readiness not established">

## Intent

**Problem:** Projects lacks a rebuildable RTI that proves complete current candidate discovery. Observed Project rows cannot establish initialized-empty Tenant coverage or foreign-owner currentness.

**Approach:** Verify executable owner sources first, then use supported EventStore projection/storage machinery for one Tenant-scoped RTI with complete inverse-reference lookup, Project-owned name lookup and request-scoped owner authorization. Reuse existing resolution mappers and engine.

## Boundaries & Constraints

**Always:** Preserve existing work and D1–D6. Require source-certified initialization/head coverage, committed-generation lookup and native scoped checkpoints. Keep actor authorization request-scoped and queries zero-write. Include authoritative Conversation membership and every Project-name equality candidate using the mapper's trim/OrdinalIgnoreCase semantics.

**Never:** Invent an ingestion feed or File identity mapping; persist actor grants or scoring labels in RTI rows; create a second RTI; truncate candidates; call full Conversation detail or inspection-auditing reads; register Story 6.4 resolution handlers; change sprint status, commits, dependencies or pins.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Current empty | Certified initialized Tenant, covered native heads, committed empty generation | Complete empty evidence | Never infer this from missing keys |
| Absent/stale/rebuilding | Missing certificate, head gap or uncommitted generation | Required evidence unavailable | Never produce false NoMatch |
| Candidates | Inverse references, authoritative membership, duplicate equality names | Every qualifying engine input retained | No arbitrary top-N |
| Authority changes | Actor/Tenant/workload/delegation/revision changes between passes | Reauthorize and reject changed evidence | No borrowed grants or protected disclosure |
| Replay | Duplicate/reordered delivery, interruption, restart, rebuild | Convergent persisted committed state | Mixed/partial generations unusable |

</frozen-after-approval>

## Source Dependencies

The [current revalidation](evidence/6-4-projects-rti-revalidation-20261020/owner-contracts.md) confirms missing certified owner/Projects coverage and initialized-empty proof, content-free Conversation metadata/purpose authority, stable File mapping/native evidence and supported-scale enrollment. EventStore transport repairs are committed and focused SDK checks pass; deployment qualification remains separate. These dependencies keep this draft blocked. The [earlier preflight](evidence/6-4-projects-rti-preflight-20261020/owner-contracts.md) remains historical.

## Code Map

- `src/Hexalith.Projects.Server/Projections/ProjectReads/` — reuse ETags, atomic batches and recovery/rebuild handling; current index has no Tenant completeness certificate.
- `src/Hexalith.Projects.Server/Queries/ProjectContextQueryExecutor.cs` — reuse two-pass stability checks, not its pre-RTI fallback.
- `src/Hexalith.Projects/Resolution/{ConversationResolutionEvidenceMapper,AttachmentResolutionEvidenceMapper,ProjectResolutionEngine,ProjectResolutionScoringRules}.cs` — preserve equality, weights, reasons and deterministic ranking.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/` — supported store/batch/epoch machinery exists. `ReadDiscoveryAsync` can reconcile state; it is unsuitable for a zero-write query path.
- Annex §5 — proposed RTI files remain conditional on real source enrollment; proposed contracts are not implementation evidence.

## Tasks & Acceptance

**Execution, gated on Source Dependencies:**

- [ ] `src/Hexalith.Projects.Server/ReferenceTrust/ReferenceTrustIndexKeys.cs` — add collision-safe Tenant/generation and inverse lookup keys.
- [ ] `src/Hexalith.Projects.Server/ReferenceTrust/ReferenceTrustIndexProjectionHandler.cs` — consume identified Project events/native owner sources with supported atomic writes and recovery; commit completeness only against certified source coverage.
- [ ] `src/Hexalith.Projects.Server/Projections/ProjectReads/ProjectReadProjectionHandler.cs` — maintain approved Project-owned equality lookup consistently with Project names.
- [ ] `src/Hexalith.Projects.Server/ReferenceTrust/ReferenceTrustCandidateLookup.cs` — union complete inverse, authoritative membership and equality candidates without label persistence.
- [ ] `src/Hexalith.Projects.Server/ReferenceTrust/ReferenceTrustResolutionReader.cs` — bind SDK caller, read only committed evidence and reauthorize before disclosure; perform no ingestion/repair.
- [ ] `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs` — enroll prerequisite components only after source qualification; add no resolution query handlers.
- [ ] `tests/Hexalith.Projects.Server.Tests/ReferenceTrust/` — verify persisted convergence, matrix behavior, revocation, candidate equivalence and bounded complete-result performance against actual sources.

**Acceptance Criteria:**

- Given interrupted, duplicate and replayed producer delivery, when rebuilding/restarting, then persisted partitions, inverse entries and scoped checkpoints converge to the same committed generation.
- Given every authorized engine input, when indexed candidates pass through existing mappers/scoring, then candidates, reasons, outcomes and order equal complete discovery, including duplicate names and unrelated Projects.
- Given revocation or revocation/regrant during an await, when final authority is checked, then prior evidence cannot authorize disclosure across actors or changed revisions.
- Given isolated producer activity, when queries execute, then persisted domain/read-model/RTI/task/repair/inspection-audit state remains byte-for-byte unchanged.
- Given supported complete shapes, when authenticated requests execute, then p95 is below 500 ms at 1,000 Projects/500 references and below one second at maxima; at most four concurrent owner calls share the two-second deadline, cancellation propagates, and no query retries, truncation or systematic Unavailable counts as success. Freeze a measured complete-response bound before readiness.

## Implementation Notes

2026-10-20: User authorized this separate RTI scope. Source inspection failed the prerequisite gate; no product implementation or RTI proof is claimed. Existing decision/annex/spec edits are preserved. See the linked report and receipt for exact source evidence and validation limits.

2026-10-20 resume: Revalidated the advanced owner checkouts; source gates remain closed. Only this spec and new evidence change; frozen intent is preserved. Parent handlers remain gated on owner evidence, exact-current qualification and accepted G-4 consumption. No intent gap or irreversible action is introduced.

## Spec Change Log

## Review Triage Log

## Verification

- `python3 tools/planning/validate_production_authority.py --story-id 6.4` and `--validate-index` check planning consistency only.
- `git diff --check` plus explicit new-file format/link checks validate these artifacts.
- Snapshot original file hashes, owner revisions/status and Git index before/after; preserve concurrent work.
- Application replay/rebuild, revocation, equivalence, zero-write and performance proofs remain unexecuted until the missing owner contracts exist. Existing tests cannot qualify nonexistent operations.
- Current receipt: resolution tests 101/101; SDK authority tests 20/20. Server test build fails with Folders CS1061 at `FoldersServerHostComposition.cs:138` and `:139`; no current server test assembly is available. Both planning validators pass.
