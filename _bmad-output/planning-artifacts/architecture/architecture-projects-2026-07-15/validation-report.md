# Hexalith.Projects architecture validation

November 20, 2026 · Standalone BMAD validation · Projects `d6583c7f`

**Changes needed before the affected writers proceed.** The read-cutover evidence supports the recorded source scope. Four high-priority gaps affect who may move a Conversation, what a Folder replacement confirms, which Project can claim a Folder, and how a task terminates after authority is revoked. Three have answers in the PRD. The owner of the exclusive Folder claim still needs an explicit decision.

| High | Medium | Low | Mechanical lint findings |
| --- | --- | --- | --- |
| 4 | 6 | 1 | 0 |

## Findings

### H1 · Conversation moves need a distinct prior-Project permission check

**High** · AD-10 and AD-33 distinguish current membership from a receipt left after unlink, but the matrix combines move and unlink under “Allowed.” A Project User who manages the target Folder and can read the Conversation could be admitted by one implementation while another also checks access to the still-bound prior Project.

**Proposed resolution.** Separate move from unlink. For a Project User, require target Folder manage and Conversation read in both cases. Require prior Project read while the membership is still bound. After a confirmed unlink, use the opaque receipt and omit the prior-Project permission check. Keep the Tenant Project Administrator path separate.

**Acceptance check.** Submit the same move before and after confirmed unlink with prior Project access revoked. Check both the admission outcome and what the Preview discloses.

[AD-10 and AD-33 · lines 190, 388](ARCHITECTURE-SPINE.md) · [FR-7 · lines 293–297](../../prds/prd-Hexalith.Projects-2026-05-24/prd.md)

### H2 · Folder replacement confirmation must bind both reader sets

**High** · G-2 calls for a versioned reader-set digest, and AD-13 binds authorization evidence. Their combination still leaves room to check only the target Folder’s reader set. A change to the current Folder’s readers between Preview and confirmation can invalidate the original no-loss comparison.

**Proposed resolution.** Bind the evidence for both Folders to the Confirmation Artifact and recheck it at admission. Give a Project User the permitted-set delta as counts. Give a Tenant Project Administrator only the coverage class, and audit the Administrator’s Preview and completion with that class.

**Acceptance check.** Change either Folder’s readers after Preview. The previous artifact must fail. Check the Administrator response and both audit records for absence of reader counts.

[AD-5, AD-13, AD-33 and G-2 · lines 160, 208, 389, 525](ARCHITECTURE-SPINE.md) · [FR-8 and FR-21 · lines 305–308, 488](../../prds/prd-Hexalith.Projects-2026-05-24/prd.md) · [Story 7.6 · lines 1844–1848](../../epics.md)

### H3 · Concurrent claims on one Folder need a shared owner decision

**High** · AD-3 makes a Folder exclusive to one Active Project. AD-8 validates the target before the Project commit, while AD-15 serializes writes per Project. Two different Project streams can both see the same supplied Folder as available. The spine leaves open which owner serializes their competing claims and which receipt each writer trusts.

**Proposed resolution.** Have Projects and Folders agree which component owns the exclusive claim for a Tenant and Folder across create, replacement and restore. Record the winning receipt, retry behavior after a lost response and the point at which a previous claim can be released. The storage implementation can stay with that owner.

**Acceptance check.** Race two Projects for one Folder, interrupt the winner after its claim, then retry on another replica. At most one Project may become Active with the Folder. Identify the persisted evidence that proves it.

[AD-3, AD-8, AD-15 and G-2 · lines 126, 178, 222, 525](ARCHITECTURE-SPINE.md) · [FR-8 · line 305](../../prds/prd-Hexalith.Projects-2026-05-24/prd.md) · [Stories 7.1 and 7.6 · lines 1702, 1840](../../epics.md)

### H4 · Reconciliation needs an explicit transition for revoked authority

**High** · FR-22 requires reconciliation to terminate as Rejected when the original actor lacks authority at commit. AD-4 treats its state diagram as exhaustive. The only recovery edge from NeedsAttention to Running says “authorized recovery,” so workers can disagree about whether the administrator’s authority is enough to enter Running and record the original actor’s denial.

**Proposed resolution.** Specify the authorized administrator’s entry point and when the worker checks the original actor. Show the exact path to Rejected when that check fails, with no owner mutation or new task.

**Acceptance check.** Start with a task in NeedsAttention, revoke the original actor’s permission and authorize an Administrator to reconcile. Verify one terminal Rejected outcome on the original task, an audit record and zero new owner effects.

[AD-4 and AD-12 · lines 132–149, 202](ARCHITECTURE-SPINE.md) · [FR-22 · line 504](../../prds/prd-Hexalith.Projects-2026-05-24/prd.md) · [Reconciliation acceptance · line 2225](../../epics.md)

### M1 · Distinguish invalid Selection Evidence from stale confirmation

**Medium** · AD-13 groups token mismatches and expiry under HTTP 409. AD-19 gives expired confirmation the recovery action RenewPreview, while FR-25 requires RequestPreview for an unusable selection. A client needs to know which recovery action to present.

**Proposed resolution.** Specify RequestPreview for missing, expired, spent or mismatched Selection Evidence on an otherwise new binding. Keep RenewPreview for a stale or expired Confirmation Artifact. Resolve an authorized equivalent retry to its original admitted task first.

**Acceptance check.** Exercise stale selection and stale confirmation with different keys, then repeat an already-admitted request with its original key.

[AD-5, AD-13 and AD-19 · lines 160, 208, 278](ARCHITECTURE-SPINE.md) · [PRD §5 and FR-25 · lines 190, 358–359](../../prds/prd-Hexalith.Projects-2026-05-24/prd.md)

### M2 · An idempotency conflict must withhold the original task

**Medium** · AD-19 specifies HTTP 409 for changed intent under the same scoped key. It leaves the response body and Location header open even though FR-1 and the PRD’s Idempotency Key definition forbid disclosing the original task.

**Proposed resolution.** Specify that a changed request returns a safe conflict with no original task ID, URL, status or other correlating detail. Reserve the retained task location for an authorized equivalent retry.

**Acceptance check.** Compare the headers and bodies of an equivalent retry and one with a changed target under the same scoped key.

[AD-5, AD-13 and AD-19 · lines 160, 208, 278](ARCHITECTURE-SPINE.md) · [Idempotency Key and FR-1 · lines 157, 218](../../prds/prd-Hexalith.Projects-2026-05-24/prd.md)

### M3 · Operator reads need two explicit disclosure rules

**Medium** · AD-33 says pre-activation status uses an allow-list without naming its fields. The same matrix also omits FR-22’s narrow exception for an Administrator inspecting quarantined legacy Projects. The first omission can disclose resource IDs too early. The second can hide the evidence needed for a permitted repair or expose it on an ordinary read.

**Proposed resolution.** For every role, pre-activation status contains only task identity, task status, safe reason and recovery codes, timestamps and expiry. Document the separate quarantine inventory: Administrator only, with Project name and any pre-v1 Folder or creation receipt, no Setup or titles, and one inspection audit per returned Project. Apply the PRD’s A-5 transition specifically to that inventory.

**Acceptance check.** Compare ordinary task status, ordinary list/open and the Administrator quarantine inventory. Check the returned fields, permission and audit count for each route.

[AD-3, AD-18 and AD-33 · lines 126, 272, 391–395](ARCHITECTURE-SPINE.md) · [FR-22 · lines 499–505](../../prds/prd-Hexalith.Projects-2026-05-24/prd.md)

### M4 · Audit retention must follow longer event-history obligations

**Medium** · AD-26 sets a 365-day minimum. AD-27 describes archiving within that window. NFR-8 also requires audit metadata to survive for the applicable event-history retention period whenever it is longer.

**Proposed resolution.** Make the retention floor the longer of 365 days and the applicable event-history obligation. Keep the operational 30-day analytics window separate.

**Acceptance check.** Verify a policy case where event history is retained beyond day 365 and its audit metadata remains queryable.

[AD-26 and AD-27 · lines 324, 330](ARCHITECTURE-SPINE.md) · [NFR-8 · line 554](../../prds/prd-Hexalith.Projects-2026-05-24/prd.md)

### M5 · A complete list can contain an unavailable entry

**Medium** · AD-32 describes response completeness across read surfaces. The PRD evaluates an authorized, current list enumeration independently of each Project row. Treating one row’s Folder status as the list-wide status would change paging and recovery behavior.

**Proposed resolution.** Specify the list envelope and row separately. Return Complete for an authorized, current enumeration, and mark an individual row Unavailable when its Folder evidence is non-current. Apply the existing disclosure and quarantine rules.

**Acceptance check.** List two otherwise visible Projects with different Folder freshness. Check the envelope, each row and continuation behavior.

[AD-32 · lines 362–368](ARCHITECTURE-SPINE.md) · [PRD §5 · lines 183–187](../../prds/prd-Hexalith.Projects-2026-05-24/prd.md)

### M6 · Release inventory and Gateway admission have different roles

**Medium** · AD-29 says eligible reads and task control require the owning inventory. McpCli’s pinned owner contract says a cataloged operation missing from the release inventory is reported. Treating the release inventory as a runtime allow-list would introduce a second authorization decision.

**Proposed resolution.** State what each check owns. The module maintainer opts an operation in by decorating its contract, McpCli discovers it, and the Gateway authorizes the call. Track release inventory coverage separately. Both public heads continue to refuse UI-only and confirmation-required operations.

**Acceptance check.** Trace an owner-decorated, already-cataloged operation with no release-inventory row. Verify the coverage diagnostic and independent Gateway authorization.

[AD-29 and G-3 · lines 342, 526](ARCHITECTURE-SPINE.md) · [Pinned McpCli discovery contract · line 493](https://github.com/Hexalith/Hexalith.McpCli/blob/6993abfebc02c877970b20df4b17a19ed8caf102/README.md#L493) · [InventoryCoverageTests · line 159](https://github.com/Hexalith/Hexalith.McpCli/blob/6993abfebc02c877970b20df4b17a19ed8caf102/tests/Hexalith.McpCli.Core.Tests/InventoryCoverageTests.cs#L159)

### L1 · The target runner needs a supported way to bind two manifest inputs

**Low** · AD-25 expects the module manifest to name a UI descriptor assembly and fixture profile. The committed v2 schema rejects unknown properties and the checked-in Projects declaration describes its internal domain Server. The source-preparation command alone cannot prove these bindings.

**Proposed resolution.** Record the supported runner inputs or track their owner decision under G-4. Keep the current v2 declaration valid while the target representation is agreed.

**Acceptance check.** Validate the owner declaration against the committed schema, then demonstrate how the runner selects the intended assembly and fixture before runtime retirement.

[AD-25, structural seed and G-4 · lines 316, 456, 527](ARCHITECTURE-SPINE.md) · [Projects owner declaration](../../../../module/README.md) · [Pinned Builds v2 schema](https://github.com/Hexalith/Hexalith.Builds/blob/2c33b2d749a2f7b8c67a15130a815996f83a51a5/schemas/hexalith.module-manifest.v2.json)

## Checks completed

- **Mechanical integrity.** BMAD lint found zero issues. AD-1 through AD-34 match the prior committed ID sequence. All 17 frontmatter source paths and 28 local link occurrences resolve. [Evidence](ARCHITECTURE-SPINE.md)
- **Requirements coverage.** The capability map covers all 25 functional and 11 nonfunctional requirement IDs. The findings below concern the behavior specified under those IDs. [Evidence](../../prds/prd-Hexalith.Projects-2026-05-24/prd.md)
- **Read cutover.** All 77 recorded cells (seven reads × eleven scenarios) were executed, passed and reported zero domain writes. Each references a passing test in one of five hash-verified native XML receipts. The separately hashed rollback proof records execution of all seven retained reads. The packet binds a working tree based on 85ff9795. This validation inspected those records without rerunning the distributed suite. [Evidence](../../../../docs/read-cutover.md)
- **Selected technology.** The EventStore 5.4.0 source exposes the projection, query, cursor and host APIs named by the spine. The repository and Platform G-6 catalog pins are distinct. The source-mode Dapr runtime policy selects 1.19.1, while the committed root catalog selects 1.19.2. [Evidence](reviews/review-technology-2026-11-20.md)
- **Planning and release.** The production-authority index and scoped Story 6.7 guard both passed. The tracker still lists 6.7 in review and 7.1 and 8.11 blocked. On d6583c7f, build-and-test passed, G-6 qualification failed at preflight and the scheduled managed journey was skipped. [Evidence](https://github.com/Hexalith/Hexalith.Projects/actions/runs/44758917793)

## Disposition

Apply the existing PRD rules to Conversation moves, Folder replacement and reconciliation. Resolve the shared Folder claim with the Projects and Folders owners before the first writer can activate a Project. Story 6.7 review and the Story 8.11 release decision have separate evidence requirements.

Validated the uncommitted [architecture spine](ARCHITECTURE-SPINE.md), SHA-256 `9a725a27ced1446a65664fceb02bb5454ffbe29eb6f128710514dbe060e3c1c5`, on branch `work`. The review used the requirements and evidence at this revision. No application tests were rerun or architectural decisions applied during this validation.

[Rubric review](reviews/review-rubric-2026-11-20.md) · [Technology review](reviews/review-technology-2026-11-20.md) · [Adversarial review](reviews/review-compatibility-2026-11-20.md) · [HTML report](validation-report.html)
