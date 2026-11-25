---
title: Close the November 25 implementation-readiness planning gaps
project: Hexalith.Projects
date: 2026-11-25
mode: batch
scope: minor
application_status: applied
baseline_commit: 294181274e2c02530d2cf417e8b94ffec69d1e2c
---

# Sprint Change Proposal — November 25 readiness gaps

## 1. Issue summary

The November 25 readiness rerun returned FAIL at `29418127`. The plan covered SDK writer equivalence for FR-26 but lacked acceptance criteria for the grant's behavior. Three story entry gates, the proposal-confirmation and safe-denial cases, and several indexes also needed correction. The readiness report records separate open owner gates. [Readiness rerun](implementation-readiness-report-2026-11-25.md) · [Spine upstream sync](sprint-change-proposal-2026-11-25-spine-upstream-sync.md)

## 2. Impact analysis

Epics 7 and 8 gain explicit entry and evidence requirements for already adopted behavior. Epic 6 changes only in the story count. The PRD and Architecture Spine already define the requirements. The architect's package and hosting decisions still gate entry. The UX journeys, MVP scope, implementation sequence, and runtime code require no change for this correction. [FR-26](prds/prd-Hexalith.Projects-2026-05-24/prd.md#fr-26-grant-scoped-chatbot-authority) · [Spine deferred decisions](architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md)

## 3. Recommended approach

Apply the planning edits directly. Adding the FR-26 behavior cases to 7.17 would put all writer proof in one story, but would make the cutover depend on correcting and qualifying existing authority behavior. A ledger entry alone gives a fix owner and leaves evidence acceptance implicit. Use DW-113 for Projects ownership and the existing 8.8-P1 release security package for acceptance, with 8.8 integrating the results. DW-112 continues to own the query disclosure fix. The planning edit is low effort, adds no story to the schedule, and makes the open evidence dependency visible before release.

## 4. Applied edits

| Item | Before → after and mechanism | Benefit | Cost or trade-off | Recommendation |
|---|---|---|---|---|
| FR-26 behavior | 7.17 command equivalence and no behavior cases → add authenticated cases to 8.8-P1 release security evidence, verified by the new 8.8 AC. DW-113 owns current Tenant-owner admission, exact scope and lifetime, Folder authority, and the unchanged permitted set. | Every grant consequence has a test and a named owner. The cutover retains its own ACs. | One ledger item and evidence coordination between the Projects owner and Test Architect. | Use DW-113 and the existing release package. |
| FR-26 traceability | `fr-26` pointed at 7.17 and its cutover receipt → both matrix views point at 8.8 and the existing 8.8-P1 authenticated security fixture and receipt. Keep 7.17 as the supporting writer proof and align the FR inventory and coverage map. | A writer-equivalence receipt alone cannot satisfy the behavior row. | Both matrix views need synchronized ownership and evidence references. | Keep the existing FR key and one primary verification lane. |
| 7.11, 7.12 and 7.16 entry | Package and hosting decisions lived in the spine → each gate names the architect-owned Projects selection and confirmation component package and FrontComposer hosting contract. 7.16 retains the independent host pin. | Story dispatch sees the required inputs without rediscovering them in the memlog. | Three gates must track any future change in the architect's decision. | Put the dependency at each dispatch point. |
| 7.12 acceptance | General fail-closed confirmation → test authorization before reading the body, zero bytes read and safe `404` on denied requests, and `400 projectMetadata` with no submission for authorized duplicate or non-string classifications. | Directly closes DW-96 and DW-87 with measurable evidence. | Adds retained-v1 endpoint cases to the existing story. | Use one AC for the two cases at the same request boundary. |
| 8.8 safe denial | Generic leakage checks → require the DW-112 `ResolveProjectChatBotAuthority` safe-`404` cases with the same response and no stream position or observation. A different grant-state response requires current delegated Folder read. | Makes the disclosure rule release-blocking through authenticated evidence. | Adds critical cases to the existing package. | Keep the fix with DW-112 and validate its evidence in 8.8. |
| Epic bookkeeping | FR-25 and AD-34 bounds and stale story counts → FR-26 and AD-35 bounds, 36 stories across Epics 6–8 (8/17/11), the FR-25 row from the matrix, and FR-26 in the Epic 7 introduction. | Readers can reconcile the plan against its stories and matrix. | One-time synchronization of duplicated indexes. | Correct the affected lines and preserve story keys. |
| Conformance checklist | 7.15 “single-writer command cutover completes” → 7.15 qualifies reconciliation on the SDK writer in test composition. 7.17 owns the cutover. | Section C agrees with AD-17 before owners sign. | The checklist edit needs a fresh byte manifest, already required by the story edits. | Fix the line now. The adopted architecture settles ownership. |
| Section G manifest | Previous November 25 candidate → rehash the current candidate after validation and record the byte deltas. | The two owner decisions can refer to the exact reviewed files. | Both owners must use the superseding digest set. | Record the manifest last and keep both rows Pending. |

[Epics and stories](epics.md) · [Deferred-work ledger](../implementation-artifacts/deferred-work.md) · [Canonical matrix](implementation-readiness-traceability-matrix.yaml) · [Matrix view](implementation-readiness-traceability-matrix.md) · [Conformance checklist](epics-architecture-conformance-checklist-2026-07-16.md)

## 5. Implementation handoff

The Projects maintainer owns the 7.12 endpoint fix when that story is eligible. The Solution Architect owns the component package and hosting contract. The Projects owner delivers any FR-26 fixes and authenticated results for DW-113 and DW-112. The Test Architect accepts those results through 8.8-P1 and checks the integrated cases in 8.8. The separately accepted pre-cutover validator and delegated-credential evidence continue to gate 7.17.

Planning acceptance requires both production-authority checks to pass, the matrix views to retain identical keys covering 26 FRs, the hand-reconciled tracker to retain its bytes, and the new Section G digests to match the working tree. The Product Owner and authorized Solution Architect retain their Pending decisions. [Section G](epics-architecture-conformance-checklist-2026-07-16.md#superseding-byte-manifest--reviewed-candidate)
