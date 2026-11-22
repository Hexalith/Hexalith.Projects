---
title: Epic 6 read cutover and context scope
date: '2026-11-22'
status: approved
approved_by: Jerome
approved_on: '2026-11-22'
review: batch
scope: moderate
---

# Epic 6 read cutover and context scope

**Approved decision.** Keep Epic 6 in progress. Use its four existing retrospective actions to qualify the current read selector, correct the operator instructions, assign the deferred context work and fix the Web authentication failure. Reassess Epic 6 once there is positive v4 cutover and rollback evidence. The eight stories retain their completion decisions at their recorded revisions. [Retrospective](../implementation-artifacts/epic-6-retro-2026-11-22.md) · [Sprint tracker](../implementation-artifacts/sprint-status.yaml)

## Why the retrospective rejected Epic 6

Story 6.7 changed the retained v1 reason-code spellings and their comparison rules. The v3 packet contains 77 distinct passing cells and the separate rollback proof names its SHA-256 and records seven executed reads. The current selector requires `story67-read-normalization-2026-11-22-v4`. On Projects `6e9fc722`, starting the Web app with v3 and requesting `/projects` returned HTTP 200 with **Workspace unavailable** and logged `supported-gate-rejected`. The 2,888 pass-3 package-mode test passes excluded the source-mode matrix. The retrospective’s distributed attempt stopped during setup, before its first read. These observations establish refusal and explain the missing positive evidence. [Historical packet](../implementation-artifacts/evidence/epic6/6.7/review-pass2-fixes/read-cutover-final-1.json) · [Historical rollback](../implementation-artifacts/evidence/epic6/6.7/review-pass2-fixes/rollback-final-proof-1.json) · [Selector](../../src/Hexalith.Projects.Client/ProjectReadCutoverGate.cs)

## Approved edits and acceptance

### 1. Qualify reversible reads on one pinned revision

**Action** `epic-6-retro-requalify-v4` · Projects maintainer and Test Architect.

**At review.** The Story 6.7 criterion requires reversible routing. A source-fixture run can finish its comparison matrix even when the routing-proof configuration is absent and it has executed zero configured rollback reads.

**Proposed.** Use Projects `d5f8417be88bd6696c9bf17ac5eebd06c78d8d33` and its committed direct gitlinks as the first candidate. It pins EventStore `d810b7a9` and Platform `44dd1e52`, resolving the two mismatches observed in the retrospective. Qualify an isolated checkout of those pins. The all-repository pull advanced CiCd to `307405a`, beyond the candidate’s `7dc3a2e`. If the intended build includes the Web fix below, freeze its resulting Projects commit before running the matrix. Record the actual source and dependency manifest, contract fingerprint, commands and native XML.

1. Build with the source fixture enabled and run all seven operations against the eleven scenarios declared by the selector. Require 77 unique executed, passing cells with the native XML hashes, persisted no-domain-write checks and the approved v4 comparison rules. Keep the old v3 artifacts intact.
2. Start the production selector with the new packet, its SHA-256, the exact source revision and contract hash. Observe positive `Supported` selection. Verify refusal with a stale or incomplete packet.
3. Supply the fixture’s routing-proof configuration and explicitly select `LegacyRollback` using the same packet. Retain the separate proof with that packet’s SHA-256 and one executed authenticated retained HTTP read for each operation. Check authority, Safe Metadata, nondisclosure and persisted no-domain-write evidence. Verify incomplete legacy security evidence selects `Unavailable`.
4. Record the result against Story 6.7 and evidence row `nfr-10`. A missing prerequisite, cell or rollback read leaves the action open and the affected route unavailable. Bring the results back for the epic decision.

[Story 6.7 criteria](epics.md#story-67-cut-over-supported-reads-while-preserving-compatibility-and-rollback) · [Matrix fixture](../../tests/Hexalith.Projects.Server.Tests/Queries/SupportedProjectGatewayHttpTests.CutoverMatrix.cs) · [Rollback fixture](../../tests/Hexalith.Projects.Server.Tests/Queries/SupportedProjectGatewayHttpTests.RollbackProof.cs)

### 2. Correct the operator runbook immediately

**Action** `epic-6-retro-correct-cutover-runbook` · Projects maintainer and Technical Writer.

**Before the correction.** The introduction calls v3 the “final source acceptance packet.” The independent verification section calls the pass-2 receipts “Current receipts,” while the selection instructions already require v4.

**Replacement for both claims.** “The pass-2 packet and seven retained reads qualify the recorded v3 source manifest. The v4 selector refused that packet on November 22. Keep the affected reads Unavailable until the intended revision qualifies. Before selecting Supported, verify the v4 packet hash, source revision, contract fingerprint and all 77 native-backed cells. To qualify rollback, explicitly select LegacyRollback and verify seven executed retained reads bound to that same packet hash.” Link the new receipts only after they exist. Preserve the explicit startup choice, immutable routing and existing refusal instructions. [Cutover runbook](../../docs/read-cutover.md)

### 3. Reconcile Story 6.3 and give the rest an owner

**Action** `epic-6-retro-reconcile-context-scope` · Product Owner and Solution Architect.

**Before the correction.** The Epic 6 introduction assumes a rebuildable Reference Trust Index. Story 6.3’s completion boundary says “assembled context + refresh + explanation,” and the FR-18 coverage row assigns supported Refresh to 6.3. The implementation decision delivered supported Get and Explain against Project-owned persisted detail. Refresh is available on retained v1. A subsequent Folder revocation can affect these context reads only after their persisted evidence refreshes.

**Replacement.** “Story 6.3 delivered supported Get and Explain for a named Conversation with current native membership under the original actor, using Project-owned persisted evidence. Refresh remains a retained v1 read. Projects owns the supported Refresh and Reference Trust Index follow-up.” Apply the distinction to the Epic 6 introduction, Story 6.3 criteria and completion boundary, FR-16/17/18 coverage, and the compiled Epic 6 context. Remove the compiled context’s superseded opened-session fallback. Preserve the target PRD and AD-10/14/32 requirements.

Promote the existing deferred Folder owner/RTI admission entry into **DW-85**, owned by the Projects maintainer with the Folders, Conversations and Memories contract owners. The Product Owner and Solution Architect accept the scope and G-2 owner contracts. Closure requires bounded original-actor owner batches for read-only supported Refresh, rebuildable Tenant-scoped index ingestion, and authenticated Get/Explain/Refresh evidence for revocation, stale evidence, cross-Tenant denial, no payload leakage and zero query writes. Include Refresh shadow equivalence. Reference the same work item from the FR rows and historical deferral. Recording ownership closes the planning action. Delivering that behavior closes DW-85. [Story 6.3 decision](../implementation-artifacts/spec-6-3-retrieve-assembled-project-context-through-supported-read-models.md) · [Deferred work](../implementation-artifacts/deferred-work.md) · [Epic 6 context](../implementation-artifacts/epic-6-context.md)

### 4. Bound initial Web authentication failure

**Action** `epic-6-retro-handle-web-auth-unavailability` · Projects UI maintainer.

**At review.** The first `GetAuthenticationStateAsync()` precedes the query client’s exception handler. A provider fault can escape the inventory page. The client already bounds faults from the later authentication recheck.

**Proposed.** Apply the existing bounded feedback at the initial lookup. A faulting provider must render the existing `data_unavailable` feedback with no payload and zero Gateway calls. Missing or mismatched identity retains `safe_denial`. Caller-requested cancellation propagates. Verify those cases through the supported query client and render the fault case on the inventory page. Preserve the post-query identity recheck and the UI.Contracts split. The existing UX vocabulary covers this state. [Query client](../../src/Hexalith.Projects.UI/Diagnostics/ProjectReadQueryClient.cs) · [Inventory](../../src/Hexalith.Projects.UI/Components/Pages/Home.razor) · [UI tests](../../tests/Hexalith.Projects.UI.Tests/Diagnostics/SupportedProjectReadSourceTests.cs)

## Sprint impact and handoff

**Moderate direct adjustment.** The runbook and scope edits are small and can proceed in parallel. The UI change is localized. The source-mode qualification is the largest effort and determines when Epic 6 can be reassessed. Its main risks are toolchain availability and mixing dependency revisions. A code rollback would discard the v1 wire-contract correction and require another qualification. Reducing MVP scope would drop existing isolation and reversibility obligations. The direct adjustment preserves both. No delivery date is committed.

The Product Owner coordinates the four existing tracker rows. Close each on its stated evidence. The retrospective is done. Epic 7 stays in backlog and Story 7.1 is blocked on SDK-host migration (DW-63) and cross-replica writer claims/fencing. Epic 8 is in progress. Story 8.1 remains an internal preview blocked on 7.1-P2, 7.15 and the four governed evidence rows (`fr-21`, `nfr-8`, `finding-test-001`, `release-authenticated-persisted-boundary`). Production readiness stays `NOT_READY`. Release owners retain deployment binding, package publication, AD-25 retirement and production rollback qualification. [Epic 8 scheduling decision](sprint-change-proposal-2026-11-22-schedule-story-8-1.md) · [Production-authority guard](../../tools/planning/validate_production_authority.py)

## Disposition on 22 November

Jerome approved the batch proposal and authorized its planning changes. The sprint tracker records two completed actions. The runbook now separates the historical v3 pass from the v4 requirements. The Epic 6 plan and evidence matrix describe delivered Get/Explain and assign supported Refresh and the Reference Trust Index to open DW-85. The Projects maintainer and Test Architect own the open v4 qualification. The Projects UI maintainer owns the open authentication-failure fix. Epic 6 awaits reassessment against the new evidence. [Sprint actions](../implementation-artifacts/sprint-status.yaml) · [DW-85](../implementation-artifacts/deferred-work.md#dw-85-supported-context-refresh-and-reference-trust-index)
