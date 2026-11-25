---
title: Align the PRD, epics, deferred-work ledger and Section G with the 2026-11-25 Architecture Spine
project: Hexalith.Projects
date: 2026-11-25
mode: batch
status: approved
approved_date: 2026-11-25
application_status: partially-applied
classification: moderate
baseline_commit: fdc8ca69ebe0a656f88d55f9e32c8de71b1262f6
authority: architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md (SHA-256 17d45f2cfe0ad0b499aa1c94e10f0be97f6fd9e894aaeb50617e3f2f62b99cae, final, AD-1…AD-35)
decision_log: architecture/architecture-projects-2026-07-15/.memlog.md lines 110–179
---

# Sprint Change Proposal — Spine upstream sync (2026-11-25)

## 1. Issue summary

The Architecture Spine was finalized on 2026-11-25 at SHA-256 `17d45f2c…` after the Update that dispositioned all 63 findings of the 2026-11-24 standalone validation (memlog line 178). The spine is authoritative. Its finalize entry lists the upstream syncs still owed: the PRD (A-3 ChatBot authority requirement, A-6 credential wording, NFR-5 reading), epics.md (per-slice cutover and Projects-issued credential wording), the Section G byte manifest that still names the old spine digest, and deferred-work entries for the routed items.

This proposal makes those syncs and nothing else. It re-decides nothing the spine settles. Each edit below names the memlog line (`ML n` = line *n* of `.memlog.md`) whose decision it carries.

Trigger type: new architecture decisions (approved by Jerome on 2026-11-25) not yet reflected in the planning sources. Mode: batch. Scope: moderate (one new story, one new FR, tracker and guard updates).

## 2. Findings that shape the proposal

| Check | Result |
| --- | --- |
| Baseline | HEAD `fdc8ca69` ("fix: story 7.1 P2") commits the final spine and memlog. `lint_spine.py` exits 0 with zero findings. The production-authority guard passes with `[6, 7, 8]`. |
| Concurrent work | Another session has uncommitted edits to `tools/planning/validate_production_authority.py` (+215/-21), `tests/tools/test_production_authority_guard.py` (+222) and the 7.1-P2 spec. Edit T-2 below touches the same guard file, so it waits until that session commits. |
| M6 gate (ML 138) | **Not missed.** The 7.1-P1 spec decision Q2=A routed the confirm parse-before-authorize order to a Story 7.12 deferred-work entry. DW-96 ("Authorize proposal confirmation before parsing its body", routed to Story 7.12) was committed in 60afa7a7, the same commit that recorded 7.1-P1 `done`. DW-87 is routed with it. The code still parses first, which AD-31 allows as a bounded exception until Story 7.12 or an authorize-first fix. No new M6 entry is needed. |
| DW items already present | M6 only (DW-96). The other 15 routed items have no entry. DW-95 (manifest callback grants) is adjacent to L8 but covers callback grants, not route-catalog authority. |
| PRD FR-19 vs AD-31 (ML 163) | The PRD says "system-supplied Metadata Classification" and does not conflict. epics.md's FR-19 inventory line still carries the superseded "assigned by authenticated integration policy" wording (edit EP-20). |

## 3. Recommended approach

Direct adjustment. Edit the PRD and addendum, epics.md, the traceability matrix, the tracker and guard, the deferred-work ledger, and Section G. Effort: medium. Risk: low; planning artifacts only, no code or story status change. Timeline: no story is re-opened, and Story 7.1 stays `blocked`.

### Where the new work lands (brief item 2c)

**Recommendation: one new Story 7.17, "Cut over Project writes to the SDK writer in one fenced deploy".** The cutover has gates that are outside every existing story's acceptance criteria: G-5 delegated credentials, the real validator, the provider-class selection, Dapr 1.19.2 evidence, and all of Stories 7.1–7.15 qualified. Folding it into Story 7.15 would block 7.15 on those platform gates, so 7.15 could not close on its own ACs. One cutover event also needs one record. The cost is one tracker key and one guard line. Story 7.16 already gives the precedent for an Epic 7 story that waits on an independently accepted Epic 8 package (8.8-P3-H).

| New work | Lands in | Memlog |
| --- | --- | --- |
| AD-35 claim aggregate (`Tenant/project-folder-claims/FolderId`) and its fenced release | Story 7.1 ACs (EP-7, EP-8). Replacement and archive releases are already in 7.6/7.13/7.14. The move into `Hexalith.Projects` happens in Story 7.17. | ML 152, 176 |
| Separate non-shipped project for the SDK aggregates until the cutover | Epic 7 writer-model paragraph (EP-3), created by the first slice; moved by Story 7.17 | ML 175 |
| Startup check for one keyed processor per domain (and the L7 test) | Story 7.17 AC; DW for the cutover owner | ML 144, 175 |
| Moving handlers into `Hexalith.Projects` before the cutover | Epic 7 writer-model paragraph (new types land there; a story that touches a resident moves it). Story 7.17 entry gate requires the build check to pass. | ML 165 |
| Allow-all validator out of shipped composition, plus the real validator | C1(a): DW entry, gating the real validator and Story 7.17. C1(b): the 8.8-P1 row (EP-15) and the Story 7.17 entry gate | ML 154, 174 |
| 25-route v1 before/after table and consumer inventory | Story 7.17 AC (the cutover record) | ML 155, 176 |
| Per-commit actor recheck | Epic 7 shared invariant 4 (EP-4) | ML 162 |
| Project commit's task/step key and expected version | Epic 7 shared invariant 4 (EP-4) | ML 157, 176 |

### Decisions requested

1. **Q1 — cutover home.** Approve new Story 7.17 (recommended), or fold the cutover into Story 7.15 (no tracker key, but 7.15 then waits on G-5).
2. **Q2 — 8.8-P1 sequencing.** The spine puts the real validator, "produced as 8.8-P1 evidence", before the cutover, but the 8.8-P1 row blocks the whole package on Stories 8.3–8.5. Option A (recommended): the 8.8-P1 row and its tracker entry state that the validator and delegated-credential evidence is accepted before Story 7.17, independently of the 8.3–8.5 parity evidence (EP-15, T-3). Option B: leave the row unchanged; the cutover then waits for Stories 8.3–8.5.
3. **Q3 — FR-26 release class.** Core user value (recommended; the grant is an existing Chatbot capability whose safety rules are part of the FR) or release-blocking safety and operations.
4. **Q4 — one conflict outside the listed ADs.** Story 8.9 pins its read and append measurements to Dapr/Redis, while AD-27 (ML 176) and the provider-class Deferred row (ML 149, 177) require the selected production provider class. Include EP-16 (recommended) or leave it.

## 4. Detailed change proposals

Line numbers refer to the files at `fdc8ca69`. Edits are made as bytes: prd.md and epics.md are CRLF; addendum.md, deferred-work.md, the checklist and the matrix are LF. `git diff --stat` is checked after each file.

### 4.1 PRD — prds/prd-Hexalith.Projects-2026-05-24/prd.md

**PRD-1 — new FR-26 (ML 160, 176).** Insert a new §6.7 after FR-24, before §7:

> ### 6.7 Delegated Chatbot Authority
>
> #### FR-26: Grant Scoped Chatbot Authority
>
> The Tenant owner can grant and revoke a scoped, expiring Chatbot authority for one Project. The grant is a delegated-operation record, not Project access (§2.1, §9 A-3).
>
> **Consequences (testable):**
>
> - Only the Tenant owner may grant or revoke, on current Tenant-owner evidence from `Hexalith.Tenants`; every other role, including Tenant Project Administrator, is denied.
> - A grant names exactly one principal, operation, actor class and origin, and carries an expiry; revocation or expiry ends it.
> - A grant is necessary and never sufficient: the delegated actor still needs current Folder authority on the Project Folder, and a Folder revocation wins immediately, whatever the grant's remaining lifetime.
> - A grant never adds a permitted Project (FR-2, FR-5) or substitutes for any action authorization.
> - The authority check returns the same safe denial for an absent Project, a denied caller and a missing grant, with no stream position or observation, unless the delegated actor has current Folder read.

**PRD-2 — §2.1, line 37 (ML 160).**
OLD: "Projects issues no Project-level grants of its own and its workload identity holds no Folder authorization;"
NEW: "Projects issues no Project-level grants of its own (FR-26's scoped, expiring Chatbot authority is a delegated-operation record, not Project access) and its workload identity holds no Folder authorization;"

**PRD-3 — §9 A-3, line 601 (ML 160).** After "…add explicit grant/revoke FRs under a new ID rather than widening FR-2 or FR-5." add: "FR-26 is the one approved delegated-operation record; it is not Project membership and never substitutes for Folder authority."

**PRD-4 — §2.3, line 67 (follows PRD-1; Q3).** "FR-1 through FR-20, FR-23, and FR-25" becomes "FR-1 through FR-20, FR-23, FR-25, and FR-26".

**PRD-5 — §9 A-6, line 604 (ML 166).**
OLD: "The components authenticate their own requests with a Projects-issued credential bound to the actor's session and unavailable to the host adapter,"
NEW: "The components authenticate their own requests with a platform-issued credential for the Projects component, issued by the platform identity service, bound to the actor's session and unavailable to the host adapter; Projects holds no signing keys,"

The PRD does not use the G-gate labels, so this wording names the platform identity service rather than G-5.

**PRD-6 — frontmatter.** `updated: 2026-11-20` becomes `updated: 2026-11-25`.

### 4.2 PRD addendum — addendum.md

**ADD-1 — NFR-5 reading (ML 170).** New §7.5 after §7.4:

> ### 7.5 NFR-5 audit-record reading
>
> NFR-5's 100,000 retained audit records per Project is the online query-performance shape for read and performance evidence (Story 8.9), not a retention cap. Retention follows the architecture's audit-retention rule (AD-26, never below the NFR-8 365-day minimum), and no record is discarded to stay inside the shape. The archive mechanism for older records is deferred to platform design and revisited before Story 8.1 and Story 8.9 entry.

**ADD-2 — evidence index.** Add row E-42 citing this proposal (FR-26, A-3, A-6, NFR-5), and change "through November 20, 2026" (line 5) to "through November 25, 2026".

### 4.3 epics.md — writer cutover model (brief 2a, ML 151)

**EP-1 — Epic 7 entry gate, line 1619.** "(Epic 6 read cutover complete; command cutover lands here)" becomes "(Epic 6 read cutover complete; the one AD-17 writer cutover is Story 7.17)".

**EP-2 — DW-63 paragraph, line 1621.** "as a prerequisite of Story 7.1 writer cutover." becomes "as a prerequisite of Story 7.1."

**EP-3 — new paragraph after shared invariant 8 (ML 151, 165, 175):**

> **Writer model (AD-17, 2026-11-25).** Stories 7.1–7.15 each qualify their slice on the SDK writer in test composition (host and two-instance lanes). Until the cutover, the SDK `projects` and AD-35 claim aggregates live in a separate non-shipped project that only test composition loads, and deployed environments keep the current processor. The writer cutover is one fenced, drained deploy (Story 7.17) after G-5 delegated credentials for saga owner steps and the real EventStore-authorization validator, and before the Epic 8 release. No story enables a slice, freezes a route or runs two writers. After the cutover, writer rollback means freezing mutation and rolling forward. New `IDomainQueryHandler`, projection and read-model adapter types land in `Hexalith.Projects`; a story that touches an existing Server resident moves it there (AD-24).

**EP-6 — Story 7.1 entry gate, line 1694 (ML 151, 136, 137).** Replace the bullet with:

> - **Entry gate:** Epic 7 gate, Story 6.7, 7.1-P1 and 7.1-P2 accepted at immutable revisions with executable rollback, and accepted DW-63 SDK-host integration. Story 7.1 qualifies creation only, on the SDK writer in test composition (host and two-instance lanes); it does not deploy the writer. Its evidence covers single-Tenant request binding (AD-18), one authorized Folder before activation, the AD-35 claim, durable owner receipts, AD-17 historical replay and command equivalence for creation, and single-writer fencing. The claim-race proof runs on Dapr runtime 1.19.2 or later unless the owner records a risk acceptance on that evidence row. The deployed writer selection is Story 7.17. Production routing and release remain governed by their existing approvals.

This removes "Before opening an affected writer slice…" and "keep mutations fenced and execute the approved forward recovery", which assumed a per-slice cutover.

**EP-9 — Story 7.1 legacy-key AC, line 1723 (ML 151, 158).**
OLD: "**Given** legacy writers at cutover, **when** enabling a Tenant/Folder key, **then** fence legacy writes under AD-17, reconstruct the incumbent from authoritative Project history and reserve its claim first. Reject new claims to an occupied key and keep incomplete or conflicting histories closed. Include incumbent, ambiguous history and free-Folder cases in the first writer evidence."
NEW: "**Given** legacy Project histories in test composition, **when** the AD-35 cutover reconstruction runs, **then** reconstruct each incumbent from authoritative Project history and reserve its claim first. Reject new claims to an occupied key, keep a key with incomplete or conflicting history closed, and quarantine each Project on a conflicting key as `Unavailable` with no context. Include incumbent, ambiguous history and free-Folder cases in the creation evidence."

**EP-11 — Story 7.15 (ML 151, 143).**
- So-that: "before their value slice cuts over (NFR-4, NFR-10)" becomes "before the AD-17 writer cutover in Story 7.17 (NFR-4, NFR-10)".
- First AC inventory: "(Active-folderless, `ProjectFolderCreationPending`, in-flight)" becomes "(Active-folderless, `ProjectFolderCreationPending`, in-flight, and v1 proposal-ledger orphans: a Project without membership or a duplicate)".
- Completion boundary: "…disposition with evidence; single-writer command cutover complete." becomes "…disposition with evidence, qualified on the SDK writer in test composition; the cutover itself is Story 7.17."

**EP-14 — Epic 8 shared attributes, line 2231 (ML 151).** "legacy routing retained and reversible until 8.11 acceptance" becomes "legacy read routing retained and reversible until 8.11 acceptance; after the Story 7.17 writer cutover, writer rollback is a mutation freeze plus roll-forward".

**EP-18 — 8.11-P3 row, line 2902 (ML 151).** Outcome gains "; the writer drill exercises the AD-17 mutation freeze and roll-forward". Initial state "open; blocked by accepted cutover/package/deployment inputs and an available rollback environment" becomes "open; blocked by the accepted Story 6.7 read cutover and Story 7.17 writer cutover, package/deployment inputs and an available rollback environment".

### 4.4 epics.md — Story 7.16 credential (brief 2b, ML 166)

**EP-12 — line 2153.**
OLD: "the component authenticates with a Projects-issued credential bound to the actor’s session and unavailable to the host adapter."
NEW: "the component authenticates with a platform-issued credential for the Projects component (G-5), bound to the actor’s session and unavailable to the host adapter; Projects holds no signing keys."

### 4.5 epics.md — new work (brief 2c)

**EP-4 — shared invariant 4, after line 1648 (ML 157, 162, 176).** Append:

> Projects' own commit is a saga step keyed by task and step, independent of MessageId, and carries the expected Project version. `Handle` checks the task/step key first, so an equivalent redelivery returns the original outcome; a version mismatch is a rejection event that changes no state and ends the task `Rejected` (`RenewPreview` if confirmation-bound, else a retryable conflict code) with no automatic retry. Before each Project commit, Projects rechecks the original actor's current Tenant membership and action authority: a confirmed denial ends the task `Rejected` (`NeedsAttention` under AD-8 once a Folder exists) with one audit outcome, and an unavailable check means `WaitingForDependency`, never a commit. Later owner steps use task-bounded G-5 delegated credentials, never the interactive token.

**EP-7 — Story 7.1 claim AC, line 1711 (ML 152, 176).** "Projects atomically acquires the exclusive `(TenantId, FolderId)` claim under AD-35." becomes "Projects atomically acquires the exclusive claim on the AD-35 claim aggregate (`Tenant/project-folder-claims/FolderId`; generation is that stream's version at acquisition)."

**EP-8 — Story 7.1 race AC, line 1721 (ML 152, 176).** "Fence the outstanding commit before releasing an abandoned reservation." becomes "Before releasing an abandoned reservation, commit the terminal creation-abandoned fence event to the reserved ProjectId stream so a late create is rejected; that release runs before the task's terminal transition as a Projects-internal step, or the task stays `NeedsAttention`."

**EP-13 — new Story 7.17 after Story 7.16 (ML 111, 112, 139, 143, 144, 151, 152, 154, 155, 156, 158, 160, 161, 165, 174, 175, 177):**

> ### Story 7.17: Cut over Project writes to the SDK writer in one fenced deploy
>
> As the **Projects Owner operating deployed environments**,
> I want **one recorded, fenced and drained deploy that selects the SDK `projects` processor after every write slice has qualified on it**,
> So that **deployed environments move to the qualified Folder-first writer with no per-slice enablement, frozen route or mixed-writer state (AD-17, NFR-4, NFR-10)**.
>
> - **Traceability:** FR-26 (writer equivalence); NFR-4, NFR-10; AD-5, AD-17, AD-18, AD-20, AD-24, AD-35; evidence rows `fr-26`, `nfr-4`, `nfr-10`.
> - **Entry gate:** Stories 7.1–7.15 each qualified on the SDK writer in test composition (host and two-instance lanes). G-5 platform-issued delegated credentials for saga owner steps and the real EventStore-authorization validator accepted (AD-20, 8.8-P1 evidence). The allow-all registrations removed from shipped composition with the production startup guard. The G-5 production state-store and pub/sub provider class selected. AD-15, AD-17 and AD-35 cutover evidence on Dapr runtime 1.19.2 or later, or a recorded risk acceptance per evidence row. Every `IDomainQueryHandler`, projection and read-model adapter in `Hexalith.Projects` except adapter-only code, enforced by the AD-24 build check.
>
> **Acceptance Criteria:**
>
> **Given** the AD-17 inventory (events, state keys, read models, cursors, routes, clients, identities, in-flight work, consumers and partial records, including v1 proposal-ledger orphans), **when** the cutover is prepared, **then** every partial record has its Story 7.15 repair task and terminal disposition, and the cutover record carries the inventory, the consumer inventory and the 25-pair v1 before/after table as the NFR-10 behavior-change evidence.
>
> **Given** the cutover deploy, **when** it runs, **then** old ingress is fenced and drained, the SDK `projects` and claim aggregates move from the non-shipped test-composition project into `Hexalith.Projects`, the Projects-owned `IDomainProcessor` retires, and processor and replay are replaced together. Aggregate snapshots are invalidated or re-keyed; a processor re-folds from events instead of using a snapshot it did not write.
>
> **Given** a production-capable host with more than one keyed processor registered for a domain, **when** it starts, **then** it refuses to start. A test asserts that `GetKeyedServices<IDomainProcessor>("projects")` and the matching replay are each single and refer to the same writer.
>
> **Given** the AD-17 SDK writer gate, **when** its evidence is recorded, **then** it covers instance `Apply` replay of every historical event (including `ProjectFolderCreationPending`, the ChatBot authority events and the creation-abandoned fence event), command equivalence for unchanged commands including `GrantProjectChatBotAuthority` and `RevokeProjectChatBotAuthority`, the approved deltas only, and retained-fold state equivalence with rehydration from the other writer's snapshot.
>
> **Given** legacy Folder bindings, **when** the claim keys open, **then** Active bindings are reconstructed from Project histories and each occupied key is reserved for its incumbent first. A key with incomplete history or conflicting holders stays closed, and each Project on it is quarantined `Unavailable` with no context until Story 7.15 repair or the Story 7.6 binding leaves at most one holder.
>
> **Given** v1 idempotency keys seen in the prior 30 days, **when** the cutover runs, **then** each is imported as an AD-5 record resolving to its original outcome, or fenced to the nondisclosing `409`.
>
> **Given** the retained v1 routes after the cutover, **when** they are called, **then** create and Setup return `202` with the Gateway task location. Proposal confirm, resolution confirm, Conversation link, move and unlink, `PUT …/folder` (including `replacementConfirmed`), File and Memory link and unlink, archive and restore return `409 RequestPreview` in the v1 problem shape, admit no task and send no command, and governed admission writes one AD-26 denial audit row. A supplied `projectId` or `X-Hexalith-Task-Id` on create returns `400` naming the field.
>
> **Given** the retained adapter after the cutover, **when** it calls EventStore, Conversations or Folders, **then** it uses G-5 delegated credentials and never forwards the human bearer token.
>
> **Given** a post-cutover writer defect, **when** rollback is needed, **then** mutation is frozen and the fix rolls forward; the retired processor is never reselected.
>
> - **Estimate:** L. **Completion boundary:** one recorded cutover deploy with its inventory, route table and evidence. The Epic 8 release follows it.

**EP-22 — story count, line 1268.** "35-story outcome inventory — 8 in Epic 6 (includes Story 6.8 / E-30), 16 in Epic 7 (adds Story 7.16 / E-31)" becomes "36-story outcome inventory — 8 in Epic 6 (includes Story 6.8 / E-30), 17 in Epic 7 (adds Story 7.16 / E-31 and Story 7.17 / 2026-11-25)", following the precedent set when 7.16 was added.

### 4.6 epics.md — FR-26 traceability (follows PRD-1)

- **EP-21:** Requirements inventory: add "**Delegated Chatbot Authority**" with an FR-26 line after FR-24, and change the source note (line 87) to FR-1–FR-26.
- **EP-23:** FR coverage map: add row `| FR-26 | Grant scoped Chatbot authority (delegated-operation record) | **7.17** (SDK writer equivalence); safe-denial fix by the Epic 8 release (DW) | — (b7ae7273) | AD-3, AD-17, AD-19, AD-33 |`.
- **EP-24:** Line 1317 "Required row coverage: FR-1…25" and Story 8.11's first AC "all FR-1…25" become FR-1…26.

### 4.7 epics.md — conflicts with the 2026-11-25 rules (brief 2d)

Every Epic 7 and Epic 8 story was read against AD-3, AD-5, AD-12, AD-14, AD-17, AD-18, AD-19, AD-20, AD-24, AD-29, AD-31 and AD-35.

| # | Location | Conflict | Rule (memlog) | Edit |
| --- | --- | --- | --- | --- |
| 1 | Epic 7 entry gate and DW-63 paragraph | "command cutover lands here"; "Story 7.1 writer cutover" | AD-17 (ML 151) | EP-1, EP-2 |
| 2 | Story 7.1 entry gate | Per-slice cutover, writer opened by 7.1 | AD-17 (ML 151) | EP-6 |
| 3 | Story 7.1, legacy-key AC | "when enabling a Tenant/Folder key" at cutover; no quarantine on conflicting keys | AD-17, AD-35 (ML 151, 158) | EP-9 |
| 4 | Story 7.1, race AC | Unspecified fence before releasing an abandoned reservation | AD-3, AD-35 (ML 152, 176) | EP-8 |
| 5 | Story 7.13, v1 archive AC | "the request alone cannot archive" while the writer is gated; the Boolean route stays under accepted risk until the cutover and returns `409 RequestPreview` after it | AD-24 (ML 155) | EP-10 |
| 6 | Story 7.15 | "before their value slice cuts over"; "single-writer command cutover complete"; orphans missing from the inventory | AD-17 (ML 143, 151) | EP-11 |
| 7 | Story 7.16 | Projects-issued credential | AD-20 (ML 166) | EP-12 |
| 8 | Shared invariant 3 and Story 8.1 | Idempotency records "≥ 30 days", not "never while nonterminal, then ≥ 30 days from the terminal transition" | AD-5 (ML 129) | EP-5, EP-19 |
| 9 | Epic 8 shared attributes | "legacy routing retained and reversible"; writer rollback is freeze plus roll-forward | AD-17 (ML 151) | EP-14 |
| 10 | 8.8-P1 row | The real validator, required before the cutover, is not named and is blocked behind Stories 8.3–8.5 | AD-20 (ML 154, 174) | EP-15 (Q2) |
| 11 | Story 8.10 | "list/detail/audit parity against authoritative replay"; inspection audit is excluded from rebuilds and verified by count | AD-14 (ML 169) | EP-17 |
| 12 | 8.11-P3 row | Rollback drill does not distinguish writer rollback | AD-17 (ML 151) | EP-18 |
| 13 | FR-19 inventory (not a story) | "assigned by authenticated integration policy" | AD-31 (ML 163) | EP-20 |
| 14 | Story 8.9 (outside the listed ADs) | Measurements pinned to Dapr/Redis | AD-27 (ML 149, 176) | EP-16 (Q4) |

Edit text for the rows not shown above:

- **EP-5 — shared invariant 3, line 1641.** "retained ≥ 30 days / result lifetime." becomes "never expires while its task is nonterminal, then retained ≥ 30 days from the terminal transition or the result lifetime."
- **EP-10 — Story 7.13, line 2077.** Replace the AC with: "**Given** a retained v1 archive request carrying the Boolean confirmation flag, **when** it reaches the SDK writer in test composition, **then** the route returns `409 RequestPreview` in the v1 problem shape, ignores the flag, admits no task and sends no command, and governed admission writes one AD-26 denial audit row. Until Story 7.17, deployed environments keep the Boolean route under the recorded accepted risk (AD-24). A fresh, authorized Preview and its bound single-use artifact admit at most one task through the supported confirmation flow."
- **EP-15 — 8.8-P1 row, line 2770 (Option A).** Outcome gains "; before the Story 7.17 writer cutover, the real EventStore-authorization validator on G-5 evidence and G-5 delegated credentials for saga owner steps, accepted independently of the Stories 8.3–8.5 parity evidence". Initial state becomes "open; the parity evidence is blocked by Stories 8.3–8.5, G-4, G-5 and the current implementation-readiness freeze; the validator and delegated-credential evidence by G-5 and that freeze".
- **EP-16 — Story 8.9, line 2868.** "concurrent appends on Dapr/Redis at the PRD shapes" becomes "concurrent appends on the G-5-selected production state-store class at the PRD shapes".
- **EP-17 — Story 8.10, line 2888.** "verify list/detail/audit parity against authoritative replay through a recorded watermark" becomes "verify list/detail parity against authoritative replay through a recorded watermark and inspection audit by count (excluded from the rebuild)".
- **EP-19 — Story 8.1, line 2247.** "task/idempotency records ≥ 30 days / result lifetime" becomes "task/idempotency records kept while nonterminal and ≥ 30 days from the terminal transition or the result lifetime".
- **EP-20 — FR-19 inventory, line 126.** "assigned by authenticated integration policy and never inferred from user text (AD-31/E-9)" becomes "supplied by the canonical Chatbot or Web adapter (the v1 adapter passes its caller's value through), validated but not persisted in v1, and never inferred from user text (AD-31/E-9)".

**Gaps, not contradictions (listed only; no edit proposed):**

- Stories 7.11, 7.12 and 7.16 do not name the architect-owned component package and FrontComposer hosting contract that gates their entry (ML 166; spine Deferred row).
- Story 7.12 has no AC that closes DW-96 and DW-87; those entries route the work to 7.12.
- Story 8.1 does not repeat the rule that the audit and task-status decorations return only with an AD-29 inventory amendment and G-3 evidence (ML 168); DW entry H15 below carries it.
- Story 8.8 does not name the ChatBot authority safe 404; DW entry H2 below carries it.
- The canonical operator action matrix (line 2286) has no "Delegated-operation record" row. The spine table has one, but grant and revoke are not operator actions.
- The FR coverage map has no FR-25 row, and the Epic list counts at lines 366–367 are stale. Both predate this change.
- Checklist Section C, line 254 (Story 7.15 "single-writer command cutover completes") contradicts AD-17. Section C is outside this brief.

### 4.8 Traceability matrix (follows PRD-1 and EP-13)

- **MX-1:** `implementation-readiness-traceability-matrix.yaml`: `required_coverage.fr` becomes `FR-1..FR-26`; the FR header comment changes to match; add row `fr-26` (primary story 7.17, supporting 8.8, ADs AD-3/AD-17/AD-19/AD-33, entry gates Epic-7-gate and G-5, status `pending`); add "7.17" to the `supporting_stories` of `nfr-4` and `nfr-10`; `generated` and `generated_by` name this proposal.
- **MX-2:** `implementation-readiness-traceability-matrix.md`: the same row in the coverage, verification and notes tables, and the same supporting-story additions.

### 4.9 Tracker and guard

- **T-1:** `sprint-status.yaml`: add `7-17-cut-over-project-writes-to-the-sdk-writer: backlog` after 7-16, and update `last_updated`. Reconciled by hand; `sprint_plan.py` runs with `--dry-run` only, to confirm the key it would derive.
- **T-2:** `tools/planning/validate_production_authority.py`: add the 7-17 key with `backlog` to `TRANSITIVE_EPIC_STATUSES`. This file has another session's uncommitted 7.1-P2 changes. T-2 is applied only after that session commits; if it is still dirty at apply time, I stop and ask.
- **T-3 (Q2 Option A only):** the 8.8-P1 tracker entry gains the same validator and delegated-credential wording as EP-15.

### 4.10 Deferred-work ledger (brief item 3)

Fifteen new entries, numbered from the next free ID at apply time (DW-98 onward today). Each has origin (the spine 2026-11-25 update and this proposal), owner, location, source_spec (the spine), reason, removal_condition, gate and `status: open`.

| Item | Title | Owner | Gate it blocks | Memlog |
| --- | --- | --- | --- | --- |
| C1(a) | Move the allow-all registrations out of shipped composition and add the production startup guard | Projects Server owner | No later than the real validator (8.8-P1); before Story 7.17 | ML 154, 174 |
| H1 | Release EventStore with `IDomainQueryResponsePolicy`, select it in Builds, then advance the Projects gitlink | EventStore release owner, then Builds owner, then Projects owner | DW-63 acceptance | ML 135 |
| H8 | Replace first-claim Tenant selection in the v1 Tenant accessor (`HttpContextProjectTenantContextAccessor.cs:46-48`) | Story 7.1 owner | Story 7.1 | ML 136 |
| H11 | Run Dapr 1.19.2 in G-6 and CI; the CI change is a Hexalith.CiCd proposal | G-6 policy owner (CI change: Hexalith.CiCd) | Story 7.1 claim-race proof; Story 7.17 | ML 137 |
| H15 | Remove the `GetProjectAuditQuery` and `GetProjectTaskStatusQuery` decorations and add the reflection test | Projects Contracts owner | Next Projects.Contracts publication (DW-82) | ML 168 |
| M7 | Import v1 idempotency keys from the prior 30 days at the cutover, or fence them to the nondisclosing 409 | AD-17 cutover inventory owner | Story 7.17 | ML 139 |
| M11 | Invert the AD-19 six-type deny-list into the four-type allow-list, with a test over registered query types | DW-63 owner | Before the first new Projects query type | ML 140 |
| M25 | Reconcile the Module.Cli pin (6.4.2 vs 6.8.2) and pin hexalith-evidence | Projects build-tooling owner | AD-30 evidence rows (Epic 8 release) | ML 141 |
| L5 | Bump the FrontComposer CLI tool to 6.1.0 | Projects tooling owner | None; until then 6.0.1 output is not evidence for 6.1.0 descriptors | ML 142 |
| L6 | Add v1 proposal-ledger orphans to the cutover inventory with a repair disposition | AD-17 cutover inventory owner (repair in Story 7.15) | Story 7.17 | ML 143 |
| L7 | Test single keyed processor and replay, and refuse startup on more than one | Cutover owner (Story 7.17) | Story 7.17 | ML 144, 175 |
| L8 | Name one route-catalog authority and the check that binds the other | DW-63 owner | None | ML 145 |
| L9 | Move the extra types out of the five multi-type Server files | Projects Server owner | None; existing cases tolerated until the types move (H3) | ML 146 |
| L10 | Replace the raw PKCS#8 key in `Authentication:BoundStreamRead:PrivateKey` with platform-issued signing material | G-5 owner | Epic 8 release | ML 147 |
| H2 | Make `ResolveProjectChatBotAuthority` return the same safe 404 for absent, denied and ungranted, unless the delegated actor has current Folder read | Projects Server owner | Epic 8 release | ML 160 |

M6 is not added: DW-96 already carries it (§2).

### 4.11 Section G — epics-architecture-conformance-checklist-2026-07-16.md (brief item 4)

- **G-1:** Replace the "Superseding byte manifest — reviewed candidate" subsection with a 2026-11-25 manifest recorded after every edit above is applied, the same way as the 2026-11-24 one: candidate = HEAD at recording plus the listed working-tree bytes; the same artifact rows plus this proposal; a "Since the 2026-11-24 manifest" column; and a checks table (spine lint, guard `--validate-index` and its unittest suite, matrix key and FR/NFR coverage counts, the `hexalith-evidence validate` result, dependency order including Story 7.17, the last completed G-6 run, and unlisted working-tree changes). The new spine digest is `17d45f2c…`. The previous manifest stays in git history.
- **G-2:** frontmatter `updated: 2026-11-25`, and this proposal added to `reviews`.
- Both owner decision rows stay **Pending**. The baseline table and the owner-decision text are unchanged.

## 5. Implementation handoff

| Role | Responsibility |
| --- | --- |
| Developer agent (this session, after approval) | Apply 4.1–4.11 as byte edits, check `git status` and `git reflog` before each file, run the guard and its tests, the spine lint and the `sprint_plan.py` dry run, then record the Section G manifest last. No commit, push or branch. |
| Product Owner / Solution Architect | Section G owner decisions (unchanged, still pending). |
| DW owners in 4.10 | Each entry's removal condition. |

Success criteria: every edit above is present with `git diff --stat` limited to the intended files; the guard passes with `[6, 7, 8]` and its tests pass; matrix YAML and Markdown carry the same keys with 26 FRs; the Section G manifest digests match the bytes on disk; no story status changes.

## 6. Checklist status

| Section | Status |
| --- | --- |
| 1 Trigger and context | [x] Spine finalized 2026-11-25; memlog lines 110–179; validation and gate reviews read |
| 2 Epic impact | [x] Epic 7 gains Story 7.17; Epic 8 wording only; no epic added, removed or resequenced |
| 3 Artifact conflicts | [x] PRD (A-3, A-6, NFR-5), epics (14 conflicts), matrix, tracker, guard, DW, Section G. UX: no conflicting wording found |
| 4 Path forward | [x] Direct adjustment; rollback and MVP review not needed |
| 5 Proposal components | [x] This document |
| 6 Approval and handoff | [x] Approved 2026-11-25; see §7 |

## 7. Approval and application record

Jerome approved the proposal on 2026-11-25 ("continue") without separate answers to Q1–Q4, so the recommended option applies to each: Q1 new Story 7.17, Q2 Option A, Q3 core user value, Q4 EP-16 included.

Applied: PRD-1 to PRD-6, ADD-1 and ADD-2, EP-1 to EP-24, MX-1 and MX-2, T-3, and DW-98 to DW-112. One detail differs from 4.8: row `fr-26` is `blocked-external`, not `pending`, because the matrix legend requires that status for a row gated on G-5.

Pending: T-1 (tracker key), T-2 (guard line) and the Section G manifest (G-1, G-2). The guard and its tests carry another session's uncommitted 7.1-P2 changes. T-1 and T-2 must land together, or the guard fails, and the manifest hashes both files, so these three wait for that session's commit or Jerome's direction.

`sprint_plan.py generate --dry-run` (run on a scratchpad copy) reports Story 7.17 as the only new Epic 7 story. It derives the truncated key `7-17-cut-over-project-writes-to-the-sdk-writer-in-one-fenced-depl`, renames existing keys and drops `blocked` statuses, so the tracker stays hand-reconciled with the approved key `7-17-cut-over-project-writes-to-the-sdk-writer`.
