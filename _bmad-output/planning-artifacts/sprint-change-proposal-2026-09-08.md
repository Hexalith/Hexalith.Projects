---
title: "Sprint Change Proposal: Land the 2026-09-08 PRD semantic revision"
date: 2026-09-08
status: approved
approved: 2026-09-08
approved_by: Jerome
workflow: bmad-correct-course
review_mode: batch
change_scope: major
trigger: "The Hexalith.Projects PRD was revised on 2026-09-08 (status final, 25 FRs) with no approving sprint-change proposal; every downstream artifact is stale against it."
source_artifacts:
  - _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md
  - _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/addendum.md
  - _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/review-planning-drift.md
  - _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/review-adversarial-isolation.md
  - _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/review-rubric.md
  - _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/.memlog.md
requirements_coverage: "25/25"
missing_artifact_types: 0
overall_approval: approved
handoff_status: complete
application_status: applied
routed_to: "Product Manager / Solution Architect, with Product Owner, UX owner, Test Architect, and independent readiness assessor"
prd_edits: "none (body frozen); addendum E-31 only"
---

# Sprint Change Proposal: Land the 2026-09-08 PRD semantic revision

## 1. Issue Summary

`prd.md` (`status: final`, `updated: 2026-09-08`, SHA-256 `c4aed920…430b`) is a semantic product change: FR bodies changed, FR-25 Select Association Target was appended (25 FRs), and §2.1, §2.4, §2.5, §3.2, §4, §5, NFR-1, NFR-4, NFR-8, §8, and §9 A-1..A-7 now bind rules that Epics 6–8, the Architecture Spine, the UX specification, and the readiness matrix do not carry. No FR/NFR/UJ/SM ID was renumbered, split, retired, or dropped. There is no approving sprint-change proposal. §2.5 and addendum Current Readiness therefore freeze Epic 6–8 scheduling until this proposal is approved and applied, the conformance checklist is re-baselined, and an independent implementation-readiness rerun returns a new disposition.

Discovery: the 2026-09-08 validation / reviewer-gate run (four passes; final grade Fair; 0 critical). Post-final-review closures already landed in `prd.md` / `addendum.md` (memlog tail 2026-09-08). This proposal does not reopen those PRD closures. It lands the **current** PRD into downstream artifacts. `review-planning-drift.md` Findings remain the working input map; where that file still asks to edit `prd.md` (UJ-5 counts, FR-23 “actor-selected”, Administrator self-grant, FR-25 Realizes claim, SM-3 FR-25 line), those PRD edits are **out of scope** — they were closed in the PRD or are frozen by this proposal’s no-PRD-body rule.

### Evidence

| Artifact | Current SHA-256 (first/last 8) | Stale against |
| --- | --- | --- |
| `prd.md` | `c4aed920…888430b` | Source of truth; body not edited here |
| `addendum.md` | `29f99a35…f10a532` (after E-31 `applied`) | E-31 only; no other addendum edit |
| `epics.md` | `9c946367…88566e` | Frontmatter still `FR-1..FR-24` and `7/15/11`; 34 stories in substance (E-30 Story 6.8); no FR-25 / Story 7.16; personas contradict §3.2 |
| `ARCHITECTURE-SPINE.md` | `3227d8f8…6dbb1b` | `scope`/`binds` FR-1..FR-24; AD-5/10/13/20/33 predate the 2026-09-08 rules; `updated: 2026-07-16` despite commit `2d9c75a` (2026-09-06 G-6 toolchain) |
| `ux-design-specification.md` | `cc957fa8…2cbca6` | Unchanged since 2026-08-03; `relink`; no Selection Evidence / FR-25 / MCP gate branch |
| Traceability matrix YAML/MD | `4597191c…f58176` / `a43efefc…24e7145` | `FR-1..FR-24`; 63 rows; no `fr-25` |
| Conformance checklist | `078f9ce9…61fdb0b` | Bound hashes from the 2026-08-02 rerun-4 baseline (`prd.md` `37a33065…c234d`, `addendum.md` `176b461b…c909d`) |

E-17 remains the controlling implementation disposition (`NOT READY`, 2026-08-02 rerun 4). Live `release-smoke` last recorded 19 passed / 56 failed. E-30 still has no named approver; this proposal does not invent one.

### Problem statement

> The product baseline moved on 2026-09-08 and is final. The production backlog, architecture, UX, and readiness evidence still describe the 24-FR, pre-authority-split contract. Scheduling any Epic 6–8 story against those artifacts would implement a superseded product. The correction is a Major planning-layer Direct Adjustment: add Story 7.16, rewrite personas and ACs to the current PRD, rebind the named ADs and UX matrices, extend coverage to 25/25, then re-baseline conformance and rerun readiness. No MVP cut. No rollback. No ID renumbering.

## 2. Impact Analysis

### Epic impact

**Epic 6** remains the first production epic and stays viable. Stories 6.1, 6.2, 6.3, 6.4, 6.5, and 6.6 must absorb Folder-derived list/open, per-entry §5 state, Conversation-named context/start, Safe vs Descriptive Metadata, same-Tenant input filtering, and the generalized `Denied` / FR-13 exception. No new Epic 6 story. Story 6.8 (E-30) is unchanged.

**Epic 7** remains the write epic and stays viable. It cannot be completed as written: personas grant Administrator creation/additive-link and Service/Workflow Caller linking that §3.2 withholds; additive links ignore Selection Evidence; move/unlink still say “relink” / remove the reverse index; FR-8 has no coverage/rejection/designated-manager ACs; FR-9/FR-10 lack the durable-task and foreign-Folder contract. **Story 7.16** is added for FR-25. Epic 7 becomes 16 AC-bearing stories.

**Epic 8** remains the operations and release epic and stays viable. Story 8.1 must own the FR-21 inclusion matrix (re-anchoring class, inspection events, quarantine-name exception, Setup credential kind). Stories 8.3–8.5 must split Safe/Descriptive fields, add `TaskControl` mode, gate MCP confirmation, and decouple CLI (A-7 only). Story 8.11 AC 1 must validate `FR-1…25`. No new Epic 8 story.

**Epic order and priority.** Unchanged: Epic 6 → 7 → 8. No epic is obsolete. No new epic. Production-authority scheduling remains frozen until the post-apply checklist re-baseline and an independent `READY` rerun.

**Story inventory after apply.** 35 AC-bearing production stories: **8 / 16 / 11** (Epic 6 keeps Story 6.8; Epic 7 gains Story 7.16). Frontmatter today still reads `33 stories: 7/15/11` and is stale against both E-30 and this proposal.

### Artifact conflicts

| Artifact | Conflict | This proposal |
| --- | --- | --- |
| `prd.md` | Source of truth | **No body edit** |
| `addendum.md` | Needed an evidence row for this proposal and for Spine `2d9c75a` | **E-31 only** (applied with this document) |
| `epics.md` | Counts, personas, ACs, `relink`, MCP-gate vocabulary, missing Story 7.16 | Apply after approval |
| `ARCHITECTURE-SPINE.md` | 24-FR binds; AD-5/10/13/20/33; no `selection.mint`; stale `updated` | Apply after approval |
| `ux-design-specification.md` | Action/role matrices, header/inventory anatomies, Journey 4 MCP path, `relink` | Apply after approval |
| Traceability matrix | 24 FRs; no `fr-25` | Apply after approval |
| Conformance checklist | Bound hashes predate the PRD revision and Spine `2d9c75a` | **Re-baseline after apply**, not in this apply pass |
| `sprint-status.yaml` | No `7-16-…` key | Add `backlog` after approval |

### Technical impact

No production-authority implementation is authorized. No code, package, or sibling-repository change. After a later `READY`, implementation must add a Projects-owned FrontComposer selection component (Chatbot in v1), session/surface-bound Confirmation Artifacts and Selection Evidence with no mint/confirm API, Folder read/manage queries, create-only same-name Folder creation, and Tenant-salted Safe-channel surrogates. Those are story work, not this proposal.

## 3. Recommended Approach

| Option | Viable? | Effort | Risk | Why |
| --- | --- | --- | --- | --- |
| **1. Direct Adjustment** | **Yes — selected** | High (planning apply); implementation stays behind E-17 | Medium | Epics 6–8 still match the product. One new story (7.16). Personas and ACs rewrite inside existing IDs. No timeline claim until `READY`. |
| 2. Potential Rollback | No | High | High | The PRD is already `final`. Reverting it would discard accepted isolation/authority rules. No completed Epic 6–8 production story implements the new rules. |
| 3. PRD MVP Review | No | — | — | §2.3 still forbids deferring any approved FR/NFR. FR-25 is Core user value. No smaller safe release. |

**Selected: Option 1 — Direct Adjustment**, classified **Major** because it is a semantic PRD change that rebinds architecture, UX, epics, and readiness coverage. It is not a rollback and not an MVP cut.

**Rationale.** The product decision is already recorded in `prd.md`. The gap is downstream fidelity. Direct Adjustment preserves E-17 containment, the Story 6.1 prerequisite chain, and the Story 8.11 terminal gate. It adds one story rather than a new epic. Risk is contained because nothing in Epics 6–8 may be scheduled until the checklist is re-baselined and an independent rerun returns `READY`.

**MVP impact.** None. All 25 FRs and 11 NFRs remain release-blocking as classified in §2.3.

**Sequencing after approval**

1. Apply the §4 landings (`epics.md`, Spine, UX, matrix, `sprint-status.yaml`).
2. Re-baseline `epics-architecture-conformance-checklist-2026-07-16.md` against the exact applied bytes (including this proposal and the post-E-31 addendum).
3. Run an independent implementation-readiness rerun.
4. Only then may any Epic 6–8 story be scheduled.

## 4. Detailed Change Proposals

`prd.md` is not edited. Each change below is the landing of a current PRD rule. Residual PRD wording items (FR-25 “Realizes UJ-1 and UJ-2”, SM-3 not naming FR-25, per-request vs per-Project inspection-audit envelope) stay in the PRD; stories implement the frozen text and, where the drift review asked for a PRD fix, record the residual in §4.7.

### 4.0 Landing map (13 rules)

| # | Rule (current PRD) | Primary landing |
| --- | --- | --- |
| 1 | Inferred Association / `SingleCandidate` confirmation; `ConversationLinked` exemption; `ReadOnlyCandidate` | AD-5; Stories 6.4, 7.3, 7.7, 7.9, 7.11; UX classification; rows `fr-6`, `fr-12`, `fr-14` |
| 2 | Selection Evidence and FR-25 (Projects-owned component; no mint/confirm API; 5-minute expiry; Chatbot-only in v1) | **Story 7.16**; `selection.mint` row; AD-5/AD-13/AD-20/AD-33; UX selection step; row `fr-25` |
| 3 | Prior-membership receipt; move-only re-link; “relink” struck | AD-10; Stories 7.3, 7.4, 7.5; UX-DR19/DR20 and Journey 3; rows `fr-6`, `fr-7`, `fr-11` |
| 4 | Same-Tenant predicate including the acting session | AD-11; Epic 7 invariant 7; Stories 6.3, 6.4, 7.3–7.14; NFR-1 stories |
| 5 | Exclusive Folder anchor; create-only same-name creation; sole-manager / designated-manager creation | AD-3; AD-8; Stories 7.1, 7.14, 7.6; G-2 / 7.1-P2 (A-2, A-3) |
| 6 | Folder read/manage split; Folder-derived permitted set; subject read in the owning system; Chatbot-only Project User authority | AD-33 (split columns); AD-3; AD-20; Stories 6.1, 7.2, 7.3, 7.6, 7.7, 7.9, 7.13; G-2 |
| 7 | Session/surface/authorization-evidence binding; dual-principal adapter vs Service/Workflow Caller; consequential-MCP gate (Story 8.11 + A-7); CLI decoupled | AD-13; AD-20; AD-29; AD-33 footnote; Stories 7.4–7.16, 8.4, 8.5, 8.11; UX Journey 4 |
| 8 | FR-8 coverage; Administrator `NoLoss`/`Loss`; self-inclusion **rejection**; legacy binding via FR-8; designated-manager creation | Stories 7.6, 7.14, 7.15; AD-3; AD-17; AD-33; Story 8.1; G-2 |
| 9 | FR-22 task-control reconciliation bound; pre-activation allow-list; legacy inventory read with per-Project audit | AD-4; AD-12; `task.reconcile` rows; Stories 8.1, 7.15, 6.5/6.6; UX `TaskControl` |
| 10 | Safe vs Descriptive Metadata; audited inspection; FR-5 conditional name; Preview body; opaque and Tenant-salted identifiers | AD-18; AD-21; AD-26; AD-33 inspection row; Stories 6.1, 6.5, 6.6, 7.13, 7.14, 8.1–8.5; UX header/inventory |
| 11 | FR-9/FR-10 durable-task + foreign-Folder confirmation; FR-3 snapshot rule; FR-16/FR-20 name the Conversation | Stories 7.7, 7.9, 7.2, 6.2, 6.3; `file-reference.link` annotation |
| 12 | §5 required sets; per-entry list/resolution state; generalized `Denied` with FR-13 multi-input exception | Stories 6.1, 6.3, 6.4; Preview ACs 7.4–7.14; Story 8.1 task-status |
| 13 | §8 restructure (SM-2/SM-3 release checks; resolution episode; accepted; operator metadata repair) and §9 A-1..A-7 | Stories 8.8 / 8.8-P3; every story that cites an assumption uses the §9 ID; Story 8.11 |

### 4.1 `epics.md`

#### 4.1.1 Frontmatter, inventory, and containment counts

**Section:** YAML frontmatter `reconciledAgainst`, `productionAuthority`; Overview inventory line 64; line 285; Corrective Production Plan lines 1247–1250, 1263–1264, 1294.

**OLD:**

```text
reconciledAgainst: 'final PRD FR-1..FR-24 / NFR-1..NFR-11'
productionAuthority: 'Epics 6-8 (33 stories: 7/15/11) plus …'
_Source: … (FR-1–FR-24).
_All 24 FRs and 11 NFRs have an AC-bearing production owner…
33-story outcome inventory — 7 in Epic 6, 15 in Epic 7, 11 in Epic 8
(FR-1…24 / NFR-1…11)
Production release, consequential autonomous MCP mutation, and proposed-Project confirmation remain blocked until **Story 8.11**
Required row coverage: FR-1…24, NFR-1…11, …
```

**NEW:**

```text
reconciledAgainst:
  - 'final PRD FR-1..FR-25 / NFR-1..NFR-11 (updated 2026-09-08; E-31)'
  - 'ARCHITECTURE-SPINE.md AD-1..AD-34 (FR-25 and 2026-09-06 G-6 index via E-31)'
productionAuthority: 'Epics 6-8 (35 stories: 8/16/11) plus explicit prerequisite/evidence work-package ledgers'
_Source: final PRD §6–§8 + addendum (FR-1–FR-25).
_All 25 FRs and 11 NFRs have an AC-bearing production owner in Epics 6–8.
35-story outcome inventory — 8 in Epic 6 (includes Story 6.8 / E-30), 16 in Epic 7 (adds Story 7.16 / E-31), 11 in Epic 8
(FR-1…25 / NFR-1…11)
Production release, consequential MCP confirmation by a human actor (and MCP Selection Evidence), and proposed-Project confirmation on MCP remain blocked until **Story 8.11** terminal acceptance together with §9 A-7. Autonomous MCP confirmation stays out of scope (PRD §2.4) and is never enabled by that record. CLI confirmation depends on A-7 only.
Required row coverage: FR-1…25, NFR-1…11, …
```

Add this proposal to `inputDocuments`. Rewrite the FR inventory blurbs for FR-1, FR-3, FR-5–FR-14, FR-16, FR-20–FR-23 to match the current PRD (Folder-derived authority, Selection Evidence, prior-membership, same-Tenant, FR-22 title, FR-25 appended after FR-11). Insert an **FR-25** inventory bullet:

```text
- **FR-25: Select Association Target** — Project User picks an additive-association target inside the Projects-owned selection component, minting Selection Evidence (Chatbot-only in v1; no mint/confirm API; 5-minute expiry; never on open/list/resolution/proposal). _Production owner: Story 7.16._
```

Retitle FR-22 in the inventory to **Support Operator Read Access and Task Reconciliation**.

**Rationale:** Frontmatter still contradicts E-30 (34) and the 25-FR PRD. Story 8.11 will reject `fr-25` as extra until the validator clause changes.

#### 4.1.2 Personas (Stories 6.1, 6.2, 7.1–7.10, 7.14)

Apply §3.2 / AD-33. On every rewritten association or confirmation story, add the three E-24 blocks: (1) direct/delegated parity for the same actor, (2) Tenant Operator denied for association actions, (3) workload identity does not widen authority.

| Story | OLD persona | NEW persona |
| --- | --- | --- |
| 6.1 | Tenant Operator or delegated Chatbot service caller | Project User on Chatbot (Folder-derived permitted set) or Tenant-role actor on Web/CLI/MCP |
| 6.2 / 6.3 / 6.4 | delegated Chatbot service caller | Project User on Chatbot; request names the Conversation (6.2, 6.3) |
| 7.1 | delegated Chatbot service caller or Tenant Project Administrator | Project User (no prior Project authority) on Chatbot, or a Service/Workflow Caller with **no Folder supplied** (same-name creation names the original actor as sole manager). Administrator is not a creation persona. |
| 7.2 | delegated Chatbot service caller or Tenant Project Administrator | Project User with Folder manage, or a Service/Workflow Caller within the original actor’s authority |
| 7.3 / 7.7 / 7.9 | delegated Chatbot service caller (7.7 also “or Tenant Project Administrator”) | Project User with Folder manage through the Chatbot dual-principal adapter, carrying valid FR-25 Selection Evidence (or confirmation when the association is inferred) |
| 7.4 / 7.5 / 7.8 / 7.10 | Tenant Project Administrator or delegated caller | Action-authorized Project User (Folder manage) or Tenant Project Administrator |
| 7.6 | Tenant Project Administrator | Project User with manage on **both** Folders, or Tenant Project Administrator |
| 7.14 | Project User, Tenant Operator, or Tenant Project Administrator (unchanged roles; tighten authority) | Same three roles with FR-23 limits: Project User may restore only by rebinding a prior Folder they manage; Tenant Operator may only rebind the prior Folder; replacement or same-name creation is a Tenant Project Administrator operation under FR-8 |

**Rationale:** Current personas give the Administrator creation and additive-link authority §3.2 withholds, and they name a Service/Workflow Caller that “never links, confirms, or binds an existing Folder.”

#### 4.1.3 Shared Epic 7 invariants

**Section:** Shared durable-workflow invariants (lines 1549–1572).

**OLD (invariant 2 excerpt):** Confirmation Artifact bound to Tenant/actor/action/targets/request-hash/Preview/current-versions; additive links are task-only.

**NEW — extend invariant 2 and add 7 and 8:**

- Invariant 2 also binds session identifier, surface, and authorization-evidence version. Issuance and consumption fail closed on a delegated-service or non-interactive claim. Confirmation and Selection Evidence are consumed only inside the Projects-owned interactive components (no confirm or mint API). Additive links and initial Folder set are task-only **only when** the request carries valid Selection Evidence; otherwise they are Inferred Associations (`RequestPreview`).
- **Invariant 7 — Same-Tenant:** every reference, session, resolution input, context component, audit row, and export row is the same Tenant as the Project; authorization on both ends is necessary and not sufficient.
- **Invariant 8 — Prior membership:** a Conversation with current or prior confirmed membership is an FR-7 move, never an additive link; the prior-membership record is retained for the Conversation lifetime.

Cite §9 IDs (A-2, A-3, A-6, A-7) instead of restating them.

#### 4.1.4 Story AC landings (existing stories)

Compact ACs to add. Each is Given/When/Then on the named story.

**Story 6.1**

- Chatbot list/open returns exactly the Projects whose Folder the actor can currently read; Tenant-role entries never appear on Chatbot.
- Response-level list state is `Complete` when the enumeration is authorized and current; each entry carries its own state; Folder not `Current` → entry `Unavailable`.
- Safe Metadata by default; Project name for Tenant-role callers only under descriptive-metadata inspection authorization, audited.
- Pre-activation tasks never appear as Projects.

**Story 6.2 / 6.3**

- Request names the Conversation. Projects serves start-setup / context only for that Conversation’s member Project, or, if unmembered, only for a Project the actor explicitly opened in that session (FR-16 / FR-20).
- Required set: Project record, Project Folder, Project Setup, actor authorization evidence; Setup-marked references join the required set; optional omissions use `ExcludedBySetupPolicy`.
- Same-Tenant filter on every included reference; owner-system read re-checked at assembly; titles only when that read is current.

**Story 6.4**

- Authorization filtering never manufactures certainty.
- `SingleCandidate` without `ConversationLinked` is an Inferred Association (not a selection).
- `ConversationLinked` short-circuits to an existing membership and is not a resolution episode (SM-7).
- `ReadOnlyCandidate` is returned, excluded from FR-14 accept, and excluded from SM-7.
- Unauthorized / cross-Tenant / nonexistent Conversation → one indistinguishable `Denied`. `NoMatch` only for an authorized Conversation.
- FR-13: a foreign-Tenant, unauthorized, or unknown attachment is indistinguishable from an authorized attachment with no match (request not denied).

**Story 7.1**

- Created Folder names the original actor as sole manager with no Tenant-default read. Projects’ workload identity holds no Folder authorization.
- Binding an existing Folder is never implicit and is not available to a Service/Workflow Caller.
- Given a request without valid Selection Evidence when a Folder is supplied by a Project User, Then `RequestPreview` (inferred binding under FR-8/FR-15). Service/Workflow Caller creation is no-Folder only.
- Exclusive Folder: fail closed if the target is already another Active Project’s Folder or another Tenant.
- Create-only same-name creation; collision fails closed (A-2). Orphaned/reserved Folder stays `NeedsAttention` and is never an implicit target.

**Story 7.2**

- Folder manage required; Folder read without manage → `Denied`, no task.
- New `projectVersion`; no already-admitted Conversation-start or context snapshot is widened; policy changes appear at the next start or FR-18 refresh with a safe reason code.
- Setup source policy may name only existing Context References.
- FR-21 records admitting credential kind (interactive or delegated).

**Stories 7.3 / 7.7 / 7.9**

- Given no valid Selection Evidence, Then rejected with `RequestPreview`.
- Given evidence bound to another actor, session, surface, subject, target, or version, or minted on another surface, Then rejected with `RequestPreview`.
- Given Folder read without manage, Then `Denied` with no artifact or task.
- Actor holds current read on the subject in its owning system **and** manage on the Project Folder; subject and Project are same-Tenant.
- 7.3 AC 2: current or prior confirmed membership → FR-7 move; unlink-then-link cannot bypass.
- 7.7 / 7.9: Read-Model-Confirmed Completion; lost response recovers through `PollTask` or equivalent Idempotency Key retry **before** evidence is re-validated; stale authorization fails closed; authorization re-checked at every context assembly.
- 7.7: File outside the bound Project Folder requires confirmation whose Preview names the foreign Folder and, only when the actor is permitted for it, any Active Project bound to that Folder (otherwise a safe code).
- 7.7 / 7.9: re-home is two audited actions; the link audit carries the prior-link receipt when one exists.

**Story 7.4**

- Preview binds both Projects or the prior-membership record as an opaque receipt after unlink, the Conversation, actor, and current versions; all one Tenant.
- After confirmed unlink: target + Conversation authority only. Prior Project disclosed only to a permitted actor; otherwise `PriorMembershipRecorded`.
- Given the actor’s artifact presented from another session, surface, or a non-interactive credential, Then `409` and no task.

**Story 7.5**

- Reverse-index / prior-membership record is **retained** for the Conversation lifetime, not removed.
- Zero membership arises only from confirmed unlink or a transient `NeedsAttention` that still binds the prior membership.

**Story 7.6**

- Exclusive Folder; fail closed if target is another Active Project’s Folder or another Tenant.
- Project User: manage on both Folders; fail closed on any reader loss; Preview delta as counts (already readable in Folders).
- Tenant Project Administrator: rejected when the Administrator is in the target Folder’s reader set; may reduce the permitted set only when the current Folder is invalid or missing; Preview discloses `NoLoss` or `Loss` only; audit records that class, never counts.
- Same-name creation on an Administrator’s behalf names as sole manager the Project User designated in the Preview, never the Administrator.
- Confirmation Artifact binds both Folders’ reader-set evidence (versioned digest).
- Legacy folderless binding is this path, not reconciliation.

**Stories 7.8 / 7.10 / 7.13**

- Session/surface/non-interactive rejection (`409`, no task).
- 7.13: Folder manage for Project User; Tenant-role archive on Web; CLI once A-7 holds; MCP once A-7 holds **and** the consequential-MCP gate passes.
- Preview bodies for Tenant-role callers are Descriptive Metadata (inspection authorization).

**Story 7.11**

- Applies to `MultipleCandidates` **and** to any `SingleCandidate` that is an Inferred Association. Sole candidate is unselected, with reason metadata and explicit accept and decline.
- `ConversationLinked` is not a confirmation episode.
- `ReadOnlyCandidate` has no accept action.
- Artifact bound to Chatbot interactive session; Chatbot is a dual-principal adapter and cannot originate or self-confirm; Service/Workflow Caller never receives it.
- When the Conversation already has membership, the confirmed choice is an FR-7 move.

**Story 7.12**

- Completion boundary: “consequential MCP confirmation by a human actor stays disabled until §9 A-7 holds and Story 8.11 records terminal acceptance; autonomous MCP confirmation stays out of scope (§2.4).”

**Story 7.14**

- If the prior Folder is invalid or missing: Tenant Project Administrator inferred binding under FR-8 (no Selection Evidence) or create-only same-name creation naming a designated Project User; Project User replacement-restore is unreachable when the Folder is missing.
- Tenant Operator: prior Folder only.
- Project User restore: rebind a prior Folder they manage, with Selection Evidence when the replacement is actor-selected on Chatbot.
- Same FR-8 coverage / rejection / designated-manager rules as 7.6.
- Orphaned Folder is never the implicit target.

**Story 7.15**

- Active-folderless records are quarantined and inventoried, never auto-bound. Binding is Story 7.6 / FR-8 by a Tenant Project Administrator.
- Per-record compensating tasks are migration (AD-17), not FR-22 reconciliation.

**Story 8.1**

- Canonical FR-21 inclusion matrix adds: Folder re-anchoring (counts for Project User; `NoLoss`/`Loss` for Administrator; never Administrator counts); each descriptive-metadata inspection (actor, Project, timestamp, field class) — implement as **one audit event per request** carrying the inspected Project set as counts plus field class, so list pages cannot exhaust the 100,000-row envelope (residual of FR-21’s per-Project wording; PRD body not changed); quarantine-inventory name exception: one inspection event **per Project returned**; Setup credential kind; Selection Evidence consumption is not a durable audit event unless tied to task admission.
- Given a reconciliation that changes targets, confirms a resolution/proposal, or admits new intent, Then rejected; a new Preview is required. Reconciliation: no Confirmation Artifact, no new task, original bindings only, original-actor re-authorization at commit, fail closed to `Rejected`, audited.
- Pre-activation task status for every role: task identity, Task Status, safe reason and Recovery Action Codes, timestamps, expiry only.
- Generalized `Denied` for single-target reads / Preview / task-status.
- Audit, traces, and export rows carry opaque / Tenant-salted surrogates only (AD-18).

**Stories 8.3 / 8.4 / 8.5**

- Safe Metadata by default; Descriptive fields only under inspection authorization.
- 8.3-P1 fixture case 2 adds session-mismatched, surface-mismatched, non-interactive credential.
- 8.4: confirmation-required CLI actions only after the accepted A-7 claim; otherwise safe denial. CLI is **not** gated on Story 8.11.
- 8.5 AC 2: “consequential MCP confirmation by a human actor remains disabled until A-7 holds and Story 8.11 records terminal acceptance, with containment state shown explicitly; autonomous mutation stays out of scope and is never enabled.”
- 8.5 completion boundary: drop “autonomous mutation stays disabled” as the only phrase; state the human gate and the exclusion separately.

**Story 8.2 / 8.8 / 8.8-P3**

- Export field allow-list: surrogates and Tenant-salted pseudonymous actors; enumerated reason codes; no Descriptive Metadata.
- 8.8-P3 journey evidence adds the FR-25 selection step, sole-candidate accept/decline, `ConversationLinked` exemption, `ReadOnlyCandidate`, and session/surface mismatch.
- 8.8 / 8.8-P3: SM-7 / SM-8 / SM-C4 companion feed per A-1; SM-2 and SM-3 are release-acceptance checks, not outcome metrics.

**Story 8.11**

**OLD AC 1:** `all FR-1…24, NFR-1…11, P1×9, P2×7, and critical release rows are present and honest.`

**NEW AC 1:** `all FR-1…25, NFR-1…11, P1×9, P2×7, and critical release rows are present and honest.`

**OLD AC 3:** `production, consequential autonomous MCP mutation, and proposed-Project confirmation may be enabled only after that accepted record.`

**NEW AC 3:** `production, consequential MCP confirmation by a human actor, MCP Selection Evidence, and proposed-Project confirmation on MCP may be enabled only after that accepted record together with §9 A-7. Autonomous MCP confirmation and blanket service-identity mutation stay out of scope (PRD §2.4) and are never enabled by the record.`

#### 4.1.5 New Story 7.16

Insert after Story 7.15, before the Epic 7 / Epic 8 rule.

```markdown
### Story 7.16: Select an association target

As a **Project User with Folder manage on a Chatbot session (dual-principal adapter)**,
I want **to pick the target of an additive association inside the Projects-owned selection component and produce Selection Evidence**,
So that **actor-selected links and initial Folder binding are distinguishable from Inferred Associations (FR-25, §4, §9 A-6) without a mint or confirm API**.

- **Traceability:** FR-25; NFR-1, NFR-8; AD-2, AD-5, AD-13, AD-20, AD-33; UJ-1, UJ-2 (PRD Realizes claim; story journeys also exercise UJ-3); evidence row `fr-25`.
- **Entry gate:** Epic 7 gate, G-2, G-3, G-5; Chatbot host via accepted 8.8-P3 companion pin. Web or MCP hosting is out of v1 unless a Project User surface is registered there and, for MCP, the consequential-MCP gate (Story 8.11 + A-7) has passed.
- **Prior-only deps:** Stories 6.1 (permitted-set enumeration) and 7.1 (creation/initial Folder). Stories 7.3, 7.7, 7.9, and 7.14 consume this story’s evidence contract.

**Acceptance Criteria** (plus shared invariants 1–8):

**Given** a Project User session on Chatbot, **When** the selection component renders, **Then** the enumeration is Projects-served and limited to Projects on whose Folder the actor holds manage, or, for Folder targets, Folders on which `Hexalith.Folders` confirms manage at pick time; Tenant-role identities never appear.

**Given** the actor’s pick inside the component, **When** evidence is minted, **Then** it is bound to Tenant, actor, interactive session, action, subject reference, target, and current versions; it is minted by the pick, never by an API call a caller makes on the actor’s behalf; the component authenticates with a Projects-issued credential bound to the actor’s session and unavailable to the host adapter.

**Given** minted evidence, **When** it is held, **Then** it is single-use, expires within 5 minutes (NFR-8), and is never attached to open, list, resolution, or proposal responses.

**Given** an admitted Durable Task that consumed the evidence, **When** an equivalent Idempotency Key retry arrives, **Then** it resolves to that task before evidence is re-validated.

**Given** evidence that is missing, expired, consumed, mismatched on subject or target, or minted on another surface, **When** a link or binding is requested, **Then** it is rejected with `RequestPreview`.

**Given** CLI, or MCP before the consequential-MCP gate, **When** an association is requested, **Then** it is an Inferred Association; no Selection Evidence is minted.

- **Estimate:** L. **Completion boundary:** Chatbot selection component mints session-bound Selection Evidence; no mint/confirm API; Web/MCP hosting not in the v1 completion boundary.
```

**Rationale:** FR-25 is a fifth admission class (`selection.mint`). It fits none of the four 2026-08-02 classes and has no story owner.

#### 4.1.6 Canonical operator action matrix and UX-DR vocabulary

**Section:** epics matrix lines 1969–1984; UX-DR19 (line 216); UX-DR20 (line 220); Epic 5 historical “relink” (line 327) may stay as history if labeled historical; production lines 1143–1182, 220 must change.

**OLD:** four admission classes; “Explicitly actor-selected additive actions remain task-only”; CLI/MCP `relink`.

**NEW:**

| Admission class | Stable action IDs | Contract |
| --- | --- | --- |
| Confirmation + Durable Task | (unchanged set) | Unchanged, plus session/surface/authorization-evidence binding |
| Durable Task only | (unchanged set) | **Carrying valid Selection Evidence (FR-25)**; otherwise the action is inferred and confirmation-required |
| Durable Task control | `task.cancel`, `task.reconcile` | No artifact, no new task, original bindings only, audited; reconciliation Administrator-only |
| Synchronous read | (unchanged) | Unchanged |
| **Selection mint** | `selection.mint` | Synchronous, session-bound, mints Selection Evidence, no Durable Task, no Confirmation Artifact |

Replace every production `relink` with `move` / `replace-folder` (UX-DR19, UX-DR20, Story 5.x production echoes used by Epic 8, lines 1143, 1148, 1178, 1182).

Annotate `file-reference.link`: inferred or foreign-Folder → confirmation-required.

### 4.2 `ARCHITECTURE-SPINE.md`

**Frontmatter**

- **OLD:** `updated: 2026-07-16`; `scope` / `binds`: FR-1 through FR-24.
- **NEW:** `updated: 2026-09-08`; `scope` / `binds`: FR-1 through FR-25. Add this proposal and E-31 to `sources`. Index commit `2d9c75a` in the Stack preamble: G-6 accepted 2026-09-06 (toolchain only; no AD rule change). Keep the G-6 table row as already written; do not treat the toolchain pin as a product change.

**AD-2, AD-16, AD-18, AD-19, AD-33 binds:** FR-1 through FR-25.

**AD-3 — Rule add:**

```text
Permitted Projects are Folder-derived. Projects issues no Project-level grant and its workload identity holds no Folder authorization. A Folder is the Project Folder of at most one Active Project. Legacy Active folderless Projects are quarantined until a Tenant Project Administrator binds a Folder through FR-8 (Story 7.6); they are not auto-bound and are not FR-22 reconciliation.
```

**AD-4 — Rule add:**

```text
`task.reconcile` resumes, retries, or compensates from the recorded checkpoint against the original bindings and re-evaluates the original actor’s current authorization at commit, failing closed to `Rejected`. It creates no Confirmation Artifact and no new task and cannot admit new intent.
```

**AD-5 — Rule replace for the actor-selected sentence:**

```text
OLD: Actor-selected additive Conversation/File/Memory links, initial Folder setting, Setup update, and direct creation are task-only …
NEW: Additive Conversation/File/Memory links and initial Folder setting that carry valid Selection Evidence (FR-25) are task-only. Selection Evidence is minted only by the actor’s pick inside the Projects-owned component (Chatbot in v1), bound to Tenant, actor, interactive session, action, subject, target, and versions, single-use, 5-minute lifetime, never attached to open/list/resolution/proposal responses; there is no mint API. An association without that evidence is an Inferred Association and requires Preview and confirmation (`RequestPreview`). A `SingleCandidate` whose reasons include `ConversationLinked` is an existing membership, not an association to bind. Setup update and no-Folder creation remain task-only. Confirmation Artifacts remain 15-minute, single-use, and additionally bind session, surface, and authorization-evidence version.
```

**AD-10 — Rule add:**

```text
After unlink, Conversations retains a prior-membership record for the Conversation lifetime. The record is an opaque receipt: it discloses the prior Project only to an actor permitted for that Project and otherwise appears as `PriorMembershipRecorded`. Any later link is an FR-7 move. Projects’ reverse index is rebuildable and Tenant-scoped and must be able to reconstruct that receipt.
```

**AD-11 — Rule add:**

```text
The same-Tenant predicate is evaluated before authorization for every reference, the acting session, resolution input, context component, audit row, and export row. Authorization on both ends never substitutes for it.
```

**AD-12 — Rule add:** same reconciliation bound as AD-4.

**AD-13 — retained bindings add:**

```text
OLD: Tenant, actor/delegation, action, targets, normalized request hash, Preview digest, owner versions
NEW: those fields plus interactive session identifier, surface, and authorization-evidence version. Issuance and consumption fail closed on a delegated-service or non-interactive claim. Consumption occurs only inside the Projects-owned confirmation component (no confirm API). Selection Evidence is the same opaque platform record class with a 5-minute lifetime and pick-time bindings (FR-25).
```

**AD-18 — Rule add:**

```text
Safe-channel identifiers are opaque and non-derivable from path-shaped or slug-bearing upstream identities. Projects owns a rebuildable Tenant-scoped surrogate mapping. Safe Diagnostic Export actor identifiers are pseudonymous and Tenant-salted. Reason codes are enumerated and never embed names.
```

**AD-20 — Rule add:**

```text
The immutable context includes the interactive-session claim (A-7), Folders read/manage evidence with freshness no older than the interactive dependency timeout, and the per-Tenant descriptive-metadata inspection permission when present. Dual-principal adapters (Chatbot, Web, CLI) carry the actor’s session and cannot originate or self-confirm. A Service/Workflow Caller is any caller without that session and never receives Confirmation Artifacts or Selection Evidence.
```

**AD-21 — Rule add:** export schema uses surrogates and Tenant-salted actor identifiers; enumerated reasons only.

**AD-23 — Rule add:** replacement or same-name restore is a Tenant Project Administrator operation when the prior Folder is missing; a Project User may restore only by rebinding a prior Folder they manage; Tenant Operator rebinds the prior Folder only.

**AD-26 — Rule add:** descriptive-metadata inspection events are durable audit; traces and audit carry surrogates only.

**AD-29 — footnote:**

```text
MCP: read and task control until the consequential-MCP gate (Story 8.11 terminal acceptance + A-7). The gate never admits autonomous confirmation. Resolution and proposal confirmation remain Chatbot-only.
```

**AD-33 — replace the matrix.** Split the Project User column into Folder read and Folder manage. Add rows. Footnote MCP as AD-29.

| Action | Project User (Folder read) | Project User (Folder manage) | Tenant Operator | Tenant Project Administrator |
| --- | --- | --- | --- | --- |
| Open / list / resolve / context / explain / refresh / Conversation-start | Allowed (Chatbot; Folder-derived) | Allowed | Allowed (Web/CLI/MCP; Safe Metadata default) | Allowed (Web/CLI/MCP; Safe Metadata default) |
| Inspect Descriptive Metadata | Allowed for permitted Projects | Allowed for permitted Projects | Separately authorized, audited | Separately authorized, audited |
| Confirm ambiguous resolution or proposed creation | Denied | Allowed (Chatbot only) | Denied | Denied |
| Select association target (`selection.mint`) | Denied | Allowed (Chatbot in v1) | Denied | Denied |
| Create Project (`project.create`) | Allowed (no prior Project authority; created Folder names actor sole manager) | Allowed | Denied | Denied |
| Update Setup (`project-setup.update`) | Denied | Allowed | Denied | Denied |
| Archive or restore (rebind prior Folder) | Denied | Allowed | Allowed (Web; CLI after A-7; MCP after A-7 + gate) | Allowed (same surfaces) |
| Restore with replacement or same-name Folder | Denied | Denied when Folder missing | Denied | Allowed under FR-8 |
| Add Conversation/File/Memory link or initially set Folder | Denied | Allowed (Selection Evidence or confirmed inference) | Denied | Denied |
| Move / unlink | Denied | Allowed | Denied | Allowed |
| Replace Folder / bind quarantined legacy Folder | Denied | Allowed (manage on both; no reader loss) | Denied | Allowed (may reduce only if current Folder invalid/missing; `NoLoss`/`Loss`; rejected if Administrator is in the target reader set; designated-manager on create) |
| Inspect pre-activation safe task status | Own permitted tasks through Chatbot | Own permitted tasks through Chatbot | Allowed (allow-list only) | Allowed (allow-list only) |
| Reconcile `NeedsAttention` | Denied | Denied | Denied | Allowed (task-control; original bindings only) |
| Safe Diagnostic Export | Denied | Denied | Separately authorized | Separately authorized |

**Canonical action-admission classification (Spine lines 412–421):** add the `selection.mint` row; change Durable Task only contract to “carrying valid Selection Evidence (FR-25)”; annotate `file-reference.link`; change `task.reconcile` to the FR-22 bound.

**G-2 entry condition add:**

```text
Folders: distinct read/manage levels; current-authorization query; reader-set coverage/delta query with versioned digest; create-only creation that names a sole manager or a designated manager (or no Tenant-default read). Conversations: Tenant-role recognition for move and prior-membership receipt. No degraded fallback (A-3). Identifier shape of Folders/Conversations/Memories identities for the Safe-channel surrogate map (AD-18).
```

**G-5 entry condition add:** interactive-session claim (A-7); per-Tenant descriptive-metadata inspection permission (A-5).

### 4.3 `ux-design-specification.md`

**Canonical Action-Admission Classification (lines 678–695)** — same five-class table as §4.1.6, plus a **Selection step** bullet: the Projects-owned component on Chatbot; no preselection from list/open/resolution; missing evidence → `RequestPreview`.

**Journey 1** — do not treat resolution output as a selection; sole candidate shows accept/decline; `ConversationLinked` is resumption, not a pick.

**Journey 3 step 1 / Maintenance Action Panel (line 814) / Empty State**

- Replace `relink` with `move` / `replace-folder`.
- Add mode `TaskControl` (no Preview, no artifact) for reconciliation.
- FR-8 Preview: counts for Project User; `NoLoss`/`Loss` for Administrator; quarantine inventory as a distinct empty/maintenance state.

**Journey 4 (lines 703–715)** — add a closed-gate branch:

```text
B -- Confirmation + Durable Task --> gate closed? --> safe denial naming Web
```

Show containment state (A-7 + Story 8.11) on MCP mutating tools.

**Project Diagnostic Header (line 766)** — split identity: opaque id always; name only for Project Users or Tenant-role callers holding inspection authorization.

**Reference Health Matrix (line 778)** — titles/names only when owner-system read is current; otherwise opaque id + safe code.

**Audit Timeline / Safe Export / Status and Reason-Code (lines 949, 982, 986)** — Safe fields only in default operator views; surrogates; Tenant-salted export actors; enumerated reasons.

**Confirmation Pattern (line 990) and Chatbot Companion (line 998)** — add session-mismatched, surface-mismatched, non-interactive credential to the rejection list; add the selection step; add sole-candidate accept/decline; add `ReadOnlyCandidate`; add read-only vs manage capability states.

**UX-DR19 / UX-DR20** — `move` / `replace-folder`; MCP confirmation gated; CLI confirmation = A-7 only.

### 4.4 Implementation-readiness traceability matrix

**YAML `required_coverage.fr`:** `FR-1..FR-25`.

**YAML comment and MD header:** `FUNCTIONAL REQUIREMENTS (FR-1..FR-25)` / `Functional Requirements (25)`.

**Rows total:** 64 (FR 25 · NFR 11 · findings 16 · release 12).

**New row `fr-25`:**

```yaml
  - key: fr-25
    category: fr
    ids: [FR-25]
    description: "Select Association Target — Projects-owned Chatbot component mints session-bound Selection Evidence; no mint/confirm API; 5-minute expiry; never on open/list/resolution/proposal"
    architecture_decisions: [AD-2, AD-5, AD-13, AD-20, AD-33]
    user_journeys: [UJ-1, UJ-2]
    primary_story: "7.16"
    supporting_stories: ["7.1", "7.3", "7.7", "7.9", "7.14"]
    entry_gates: [Epic-7-gate, G-2, G-3, G-5]
    fixture: epic7/7.16-selection
    verification_command: "dotnet tool run hexalith-module test --profile durable --filter Story=7.16"
    evidence_artifact: evidence/epic7/7.16-selection.trx
    estimate: L
    status: blocked-external
    blocker: "Epic 7 gate + G-2 Folders manage check + 8.8-P3 Chatbot companion pin; Web/MCP hosting out of v1"
```

**Supporting-story / AD updates on existing rows** (apply in YAML; regenerate MD):

| Key | Add |
| --- | --- |
| `fr-1` | A-2/A-3; sole-manager / no implicit existing-Folder bind |
| `fr-2`, `fr-5` | Folder-derived Chatbot list; inspection-gated name |
| `fr-3` | snapshot rule; manage gate |
| `fr-6` | Selection Evidence; prior-membership → FR-7 |
| `fr-7` | prior-membership receipt; `PriorMembershipRecorded` |
| `fr-8` | coverage / rejection / designated-manager; AD-3, AD-13 |
| `fr-9`, `fr-10` | durable-task + foreign-Folder (FR-9); AD-5 |
| `fr-11` | record retained |
| `fr-12`, `fr-13`, `fr-14` | Inferred Association; `ReadOnlyCandidate`; FR-13 exception |
| `fr-16`, `fr-20` | named Conversation |
| `fr-21` | inclusion-matrix additions |
| `fr-22` | task-control bound; quarantine inventory |
| `fr-23` | per-role restore; AD-23 |
| `nfr-1` | same-Tenant session; AD-13 session bind |
| `nfr-8` | Selection Evidence 5-minute expiry |
| `nfr-11` | A-1 companion; SM-2/SM-3 as release checks |
| containment `release_block` | human consequential-MCP gate; autonomous stays excluded |

### 4.5 `sprint-status.yaml` (after approval)

Add under Epic 7, after `7-15-reconcile-legacy-and-interrupted-workflows`:

```yaml
  7-16-select-an-association-target: backlog
```

Do not change Epic 6–8 statuses. Do not schedule. `last_updated` → 2026-09-08.

### 4.6 Addendum E-31 (applied with this document)

The E-31 row is the only addendum edit. It names this proposal, records `prd.md` `c4aed920…430b`, indexes Spine `2d9c75a` / G-6 packet path, notes E-30 still has no named approver, and states the post-approval order: apply landings → re-baseline the conformance checklist → independent readiness rerun. `prd.md` is unchanged.

Post-E-31 addendum SHA-256: `563936b7…92987c8c`.

### 4.7 Explicitly not changed

- `prd.md` body, IDs, SM-3 Validates line, FR-25 Realizes clause.
- Addendum prose other than the E-31 row (including the landing-map sentence on line 30; this proposal is the landing map).
- E-17 `NOT READY`, Story 6.1 prerequisite chain, P0/P1R/P2/P3/P4, 8.8-P3 Chatbot package, live 19/56 smoke.
- E-30 approval (still “Not stated”).
- Historical Epics 1–5.
- Sibling repositories.

**Accepted residuals** (do not block this proposal): A-6 host-drives-component risk; FR-8 one-bit `Loss` disclosure; Confirmation Artifact strength now equals the Projects-owned component credential (already in the PRD; stories must not add a confirm API).

## 5. Implementation Handoff

**Scope classification: Major.** Semantic PRD change; architecture, UX, epic, and readiness rebind; no ID renumbering; no MVP cut.

**Handoff recipients**

| Role | Responsibility |
| --- | --- |
| **Jerome** | Approve or revise this proposal. Named approver for E-31. |
| **Product Manager / Solution Architect** | After approval: apply Spine AD edits and G-2/G-5 notes; sign the re-baselined conformance checklist against the applied bytes. |
| **Product Owner** | Apply `epics.md` and `sprint-status.yaml` (Story 7.16 `backlog`; 35 stories 8/16/11). |
| **UX owner** | Apply `ux-design-specification.md` matrices, anatomies, Journey 4 gate, companion bullets. |
| **Test Architect** | Apply matrix YAML; regenerate MD; keep `hexalith-evidence validate` honest (tool still published-stale). |
| **Independent assessor** | After the checklist re-baseline, run a new implementation-readiness assessment. Do not treat this proposal as `READY`. |

**Success criteria for implementation of this proposal (planning apply)**

1. Every row in §4.0 has explicit story / AD / UX / matrix text in the applied artifacts.
2. `epics.md` frontmatter reads `FR-1..FR-25` and `35 stories: 8/16/11`; Story 7.16 is present; Story 8.11 AC 1 says `FR-1…25`.
3. Spine `scope`/`binds` are FR-1 through FR-25; `selection.mint` exists; `updated: 2026-09-08`; `2d9c75a` is indexed.
4. UX has no production `relink`; Journey 4 has a closed-gate branch; header/inventory split Safe vs Descriptive.
5. Matrix `required_coverage.fr` is `FR-1..FR-25` and row `fr-25` exists (25/25).
6. `prd.md` SHA-256 remains `c4aed920…430b`.
7. Conformance checklist is **not** claimed current until a separate re-baseline records the applied hashes.
8. No Epic 6–8 story is scheduled until that re-baseline and an independent rerun complete.

**Out of scope for the apply pass:** story-file creation, sibling mutations, G-gate acceptance, Chatbot 8.8-P3, production enablement.

## 6. Change-analysis checklist

### Section 1 — Trigger and context

- [x] 1.1 Trigger: 2026-09-08 PRD semantic revision (not a single story). Planning-layer; E-17 freeze still holds.
- [x] 1.2 Type: New/changed requirements already accepted in the PRD; downstream misunderstanding relative to that baseline.
- [x] 1.3 Evidence: hashes, drift Findings, memlog closures, 25th FR, `2d9c75a`.

### Section 2 — Epic impact

- [x] 2.1 Epic 7 cannot complete as written; Epic 6 and 8 need AC/persona/matrix updates.
- [x] 2.2 Modify Epic 6–8 stories; add Story 7.16; no new epic; no epic removed.
- [x] 2.3 Remaining epics: 6 and 8 as above; order unchanged.
- [x] 2.4 No epic obsolete; no new epic required.
- [x] 2.5 No resequence.

### Section 3 — Artifact conflict

- [x] 3.1 PRD: no conflict with goals; **no body modification**.
- [x] 3.2 Architecture: AD-5/10/13/20/33 plus binds, `selection.mint`, G-2/G-5, `2d9c75a` index.
- [x] 3.3 UX: action/role matrices, header/inventory, Journey 4, `relink`, companion.
- [x] 3.4 Matrix, checklist (re-baseline after apply), sprint-status, addendum E-31.

### Section 4 — Path forward

- [x] 4.1 Direct Adjustment: **Viable** — High effort / Medium risk.
- [x] 4.2 Rollback: **Not viable**.
- [x] 4.3 MVP Review: **Not viable**.
- [x] 4.4 Selected: Option 1, Major.

### Section 5 — Proposal components

- [x] 5.1–5.5 Completed in §§1–5 of this document.

### Section 6 — Final review

- [x] 6.1 Checklist complete for proposal issuance.
- [x] 6.2 Proposal written; apply not started.
- [x] 6.3 User approval — Jerome approved 2026-09-08.
- [x] 6.4 `sprint-status.yaml` — Story 7.16 added as `backlog`; no Epic 6–8 story scheduled.
- [x] 6.5 Handoff confirmation — apply complete; conformance checklist re-baselined; independent readiness rerun remains the next gate.

---

**Applied 2026-09-08.** Downstream landings are in `epics.md`, `ARCHITECTURE-SPINE.md`, `ux-design-specification.md`, the traceability matrix, and `sprint-status.yaml`. `prd.md` is unchanged (`c4aed920…430b`). The conformance checklist records the applied hashes. Do not treat this proposal as `READY`. An independent assessor must rerun implementation-readiness before any Epic 6–8 story is scheduled.
