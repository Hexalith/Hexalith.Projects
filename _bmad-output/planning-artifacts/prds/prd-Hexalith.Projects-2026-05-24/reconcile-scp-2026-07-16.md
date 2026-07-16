# Reconcile Note — Sprint Change Proposal 2026-07-16 vs Final PRD + Addendum

- **Reconcile date:** 2026-07-16
- **Proposal:** `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-16.md`
- **Proposal title:** "Sprint Change Proposal: Downstream Artifact Repair After Final PRD Rebaseline"
- **Declared status:** `approved` (frontmatter `status: approved`, `approved: 2026-07-16`, `approved_by: Jerome`; §7 "Approved by Jerome on 2026-07-16"; §8 log "Proposal: Approved"). Scope: **Major**. Mode: incremental. `amends: sprint-change-proposal-2026-07-15.md` (E-1).
- **PRD reconciled:** `prds/prd-Hexalith.Projects-2026-05-24/prd.md` (status final, updated 2026-07-15; FR-1..FR-24, NFR-1..NFR-11).
- **Addendum reconciled:** `prds/prd-Hexalith.Projects-2026-05-24/addendum.md`.

## 1. What the proposal actually proposes to change

### Issue
The July 15 implementation-readiness rerun returned `NOT READY`. The final PRD correction (to 24 FRs / 11 NFRs) succeeded, but the **downstream substrate did not converge** on that final contract:
- Architecture: the only substantive doc is the obsolete 22-FR/9-NFR design that assigns runtime ownership contrary to EventStore/FrontComposer platform rules; its replacement is an untouched spine template.
- UX: operator-only; omits the required Chatbot candidate/proposal/confirmation/recovery/task/first-response behavior.
- Epics/stories: two Folder-invariant violations (Stories 1.4, 2.4); 23 Epic 6-8 entries are unschedulable findings placeholders; FR-24 has no complete epic owner (coverage 23/24 = 95.8%).
- Verification/release: nine P1 + seven P2 audit findings open; live evidence failing (focused 13/13, Chromium 19 passed/56 failed); release handoff blocked.

### Recommended path forward
**Option 1 — Planning-First Direct Adjustment** (selected; rollback and MVP-reduction rejected). Keep the final PRD + addendum as evidence and repair only downstream planning artifacts, in strict sequence: (1) instantiate + approve the architecture spine and mark the May architecture superseded; (2) update operator UX + add the binding Chatbot companion contract; (3) replace the 23 findings placeholders with **33 outcome-based stories** (7 in Epic 6, 15 in Epic 7, 11 in Epic 8); (4) create a traceability matrix + system/epic test design; (5) rerun readiness independently (must return `READY`); (6) reconcile `sprint-status.yaml` atomically only after `READY`; (7) create/implement one readiness-approved story at a time. Containment holds until all gates pass: no corrective story enters dev, no production release, no autonomous consequential MCP mutation / proposed-Project confirmation, no false-pass evidence, no event-history rewrite / unsafe dual writes, no implicit sibling-repo changes.

## 2. Change-by-change mapping and classification

### 2.0 PRD itself — §4.1 "No PRD modification"
> Proposal §4.1: "**Decision:** No PRD modification. The primary PRD and addendum remain the product and downstream-routing authority."
Also §2 PRD Impact: "No product-scope change is proposed. The final PRD and its addendum remain governing," and §4 MVP Impact: "The v1 product scope is unchanged."

- **Touches:** PRD as a whole; PRD §2.1 (Accepted Planning Decisions), §2.3 (Release Cut Rule), §2.4 (v1 Exclusions).
- **Classification: ALREADY-REFLECTED / NO-PRD-IMPACT.** The proposal explicitly requires **zero** PRD or addendum edits. Every capability it "re-establishes" is already stated in the final PRD. **There is no NEW-PRD-EDIT anywhere in this proposal.**

### 2.1 Architecture Change A — Authority and Ownership (§4.2 A)
Instantiate the spine (`status: final`, `binds: [FR-1..FR-24, NFR-1..NFR-11, Epic-6/7/8]`); ownership table: Projects owns domain policy/contracts/Project-specific task transitions; `Hexalith.Projects.Contracts` owns stable wire contracts; non-packable `Hexalith.Projects.UI.Contracts` owns FrontComposer descriptors; EventStore/platform owns hosting/persistence/projections/cursors/health/telemetry/durable-workflow; platform AppHost owns topology + Dapr; FrontComposer/platform hosts own Web/MCP/CLI composition + credentials; Chatbot owns end-user candidate/proposal presentation.

- **Touches:** Addendum §1.1 "Platform ownership invariant" and §4.2 "Projects UI contract ownership" (non-packable `Hexalith.Projects.UI.Contracts`). No FR/NFR text.
- **Current addendum text (already present):** §1.1 — "`Hexalith.Projects` owns domain policy, Project contracts, and Project-specific Durable Task transitions. Hexalith.EventStore DomainService/platform owns hosting, event persistence/publication, subscriptions, read-model stores, cursors, health, telemetry... FrontComposer/platform hosts own Web, MCP, and CLI runtime composition." §4.2 — "non-packable `Hexalith.Projects.UI.Contracts` descriptor host... remains excluded from the published package inventory."
- **Classification: ADDENDUM-ONLY (already reflected) → NO-PRD-IMPACT.** The ownership graph is verbatim the addendum's existing routing. The action (writing the spine file) is a downstream architecture-artifact task. No PRD edit.

### 2.2 Architecture Change B — Durable State and Completion Truth (§4.2 B)
No caller-visible/Active Project before one authorized Folder + read-model confirmation; `Pending→Running`, `WaitingForDependency`/`NeedsAttention` recoverable, `Succeeded`/`Rejected`/`Failed`/`Cancelled` terminal immutable; durable checkpoints/leases/worker ownership/restart convergence/duplicate delivery/compensation/receipts/reconciliation; `202` + SignalR as non-completion; 15-min single-use tamper-evident Confirmation Artifacts bound to Tenant/actor/action/targets/request-hash/Preview/versions; idempotency scope `(Tenant, actor, operation, key)` with ≥30-day/result-lifetime retention; normative response/freshness/task/recovery mappings.

- **Touches:** PRD Glossary ("Task Status", "Confirmation Artifact", "Idempotency Key", "Read-Model-Confirmed Completion"), FR-1, FR-14, §5, NFR-4, NFR-8; Addendum §1 (Durable Workflow Design Context) and §2 (Preview/Confirmation/Idempotency).
- **Current text (already present):** PRD Glossary — Task Status enumerates the exact eight states with "last four are terminal; `NeedsAttention` is recoverable and nonterminal"; Confirmation Artifact = "expiring, single-use, tamper-evident authorization bound to Tenant, actor, action, targets, normalized request, Preview, and current resource version." FR-14 — "expires after 15 minutes and is single-use." §2.1 — "No Project is caller-visible or Active before exactly one authorized Project Folder is bound and the read model confirms completion." NFR-8 — idempotency "at least 30 days or for the result's lifetime, whichever is longer." Addendum §1 — "Read-model confirmation is the completion authority; a SignalR notification or request acknowledgement does not establish completion" + durable checkpoints/leases/two-instance convergence. Addendum §2 — "Idempotency scope `(Tenant, actor, operation, key)`... 15-minute expiry, single-use enforcement."
- **Classification: ALREADY-REFLECTED (PRD/§5/glossary/NFRs) + ADDENDUM-ONLY (state-machine/lease/checkpoint mechanism). NO-PRD-EDIT.**

### 2.3 Architecture Change C — Migration and Repository Authority (§4.2 C)
Inventory of existing events/state-keys/read-models/cursors/routes/clients/identities/in-flight work; compatibility adapters + deterministic replay comparison + value-slice cutover + routing rollback; no event-history rewrite, no unsafe dual writes; compensating Durable Tasks for legacy folderless/pending-Folder/partial records; separate repository-local approval per sibling change.

- **Touches:** Addendum §5 "Migration and Compatibility"; PRD NFR-10 (Compatibility).
- **Current text (already present):** Addendum §5 — "Inventory and reconciliation of legacy Active folderless Projects... Additive event evolution... no event-history rewrite. Compatibility adapters, replay comparison, value-slice cutover, routing rollback... No unsafe dual writes... Repository-local upstream work in EventStore, FrontComposer, Conversations, Folders, or Chatbot requires its own approved story." NFR-10 — "event history is not rewritten."
- **Classification: ADDENDUM-ONLY (already reflected). NO-PRD-IMPACT.**

### 2.4 Architecture Change D — Resolution, Export, Parity, Evidence (§4.2 D)
Request-scoped current-only Resolution Trace, no persisted case identity/score history; `projects.safe-diagnostic-export.v1` (1 MiB cap, 500 reference rows, 100 audit rows, deterministic ordering, safe truncation, no cursor, no retention, unavailable-component markers, 2 concurrent exports/Tenant, audit every attempt/outcome); semantic parity without authority expansion across Web/CLI/MCP/Chatbot; disabled autonomous consequential MCP mutation/proposal confirmation until gates; machine-checkable evidence map with no critical false pass.

- **Touches:** PRD FR-17, Glossary ("Resolution Trace"), §2.1; FR-24; FR-22; §2.4 (exclusions); NFR-11; NFR-6/NFR-7; Addendum §3 (Safe Diagnostic Export detail), §7.2.
- **Current text (already present):** FR-17 — "Traces are request-scoped and not persisted; only confirmed outcomes enter audit history." FR-24 — "at most 1 MiB and contains at most 500 reference rows and 100 audit rows... exports have no continuation cursor... Projects never retains generated exports." NFR-6/NFR-7 — "per-Tenant limit of two concurrent exports." §2.4 — "Autonomous MCP confirmation or blanket service-identity mutations" out of scope. NFR-11 — "A failed critical case or unexplained critical skip blocks release." Addendum §3 — full `projects.safe-diagnostic-export.v1` cap list.
- **Classification: ALREADY-REFLECTED (PRD FR-17/FR-24/§2.4/NFRs) + ADDENDUM-ONLY (export schema). NO-PRD-EDIT.**

### 2.5 UX Change A — Chatbot Companion (§4.3 A)
UX spec must define binding candidate comparison (no preselection), proposed creation, confirm/cancel, expiry/staleness renewal, lost-response recovery, task status, first-response-admission; keyboard/focus/live-region/authenticated screen-reader/200%-zoom/320-CSS-px evidence. (Proposal OLD: "End-user Chatbot UX is excluded from the specification" — i.e., excluded from the *UX artifact*, not from the PRD.)

- **Touches:** PRD FR-14, FR-15, FR-20, NFR-9, §5, §3.4 (UJ-2/UJ-3); Addendum §6 "Chatbot Companion Contract."
- **Current text (already present):** FR-14 — "Chatbot supports states for confirmation, cancellation, retry, expiry or staleness, lost-response recovery, and task status." FR-15 — proposal/confirm behavior. FR-20 — first-response admission ("`Complete` or `Partial`... `Unavailable` or `Denied` blocks first-response admission"). NFR-9 — WCAG 2.2 AA, keyboard, screen-reader, 200% zoom, 320 CSS pixels. Addendum §6 — "Candidate comparison with no preselection... expiry/staleness recovery, lost-response retry, and task-status rendering... 320 CSS pixels, and authenticated screen-reader evidence."
- **Classification: NO-PRD-IMPACT (UX-artifact repair) — capabilities ALREADY-REFLECTED in PRD FR-14/15/20/NFR-9 and Addendum §6. No PRD edit.**

### 2.6 UX Change B — Roles, Task Truth, Refresh, Audit, MCP (§4.3 B)
Map every UI phase to Durable Task/read-model truth; use the PRD role/action matrix; classify reevaluation as read-only Refresh; separate durable audit from telemetry; show MCP availability/containment states.

- **Touches:** PRD §3.2 (role/surface authority matrix), §5, FR-18 (Refresh), FR-21 (audit vs telemetry), §2.4/FR-22 (MCP). Addendum §1, §6.
- **Current text (already present):** §3.2 role matrix (Project User / Tenant Operator / Tenant Project Administrator / Service-Workflow Caller). FR-18 — "Refresh itself never mutates Project or reference state and creates no maintenance audit event." FR-21 — "Intermediate task states, polls, retries... remain operational telemetry rather than durable audit."
- **Classification: NO-PRD-IMPACT (UX-artifact alignment to existing PRD matrix/§5). ALREADY-REFLECTED. No PRD edit.**

### 2.7 UX Change C — Response and FR-24 UX (§4.3 C)
Expose `responseState`, `asOf`, `projectVersion`, component evidence, Recovery Action Codes; FR-24 UX shows permission/schema version/snapshot/bounds/included-omitted counts/truncation/unavailable components/concurrency guidance/non-retention/audit through equivalent Web/CLI/MCP semantics.

- **Touches:** PRD §5 (Observable Context and Recovery Contract), FR-24.
- **Current text (already present):** §5 logical fields `responseState`/`asOf`/`projectVersion`/`components`/`recoveryActions`. FR-24 consequences enumerate the same bounds/counts/truncation/unavailability/audit.
- **Classification: NO-PRD-IMPACT (UX surfacing of existing §5/FR-24 fields). ALREADY-REFLECTED. No PRD edit.**

### 2.8 UX Change D — Composition, Scale, Accessibility (§4.3 D)
FrontComposer/Fluent V5, Fluent 2 tokens, required `FluentAccordion` section pattern; HTML prototype non-normative; server cursor/paging/virtualization for 10,000 Projects / 5,000 references / 100,000 audit records; authenticated automation + keyboard + screen-reader + zoom/reflow + reduced-motion + stable-selector evidence.

- **Touches:** PRD NFR-5 (scale numbers), NFR-6 (paging), NFR-9 (accessibility); UX artifact (Fluent/FrontComposer/FluentAccordion is implementation-only).
- **Current text (already present):** NFR-5 — "10,000 Projects per Tenant, 5,000 Context References per Project... and 100,000 retained audit records per Project." NFR-6 — cursor pages default 50 cap 200. NFR-9 — accessibility evidence.
- **Classification: NO-PRD-IMPACT.** Scale/accessibility numbers ALREADY-REFLECTED in NFR-5/6/9; Fluent V5 / FluentAccordion / non-normative HTML are UX/implementation detail (ADDENDUM/UX-owned, not PRD). No PRD edit.

### 2.9 Epic Baseline + Historical Supersession (§4.4)
Update Requirements Inventory + coverage to FR-1..FR-24 / NFR-1..NFR-11; label Epics 1-5 implementation history, Epic 6-8 addendum an unschedulable findings inventory, replacement stories the only schedulable units after readiness; add supersession notices to Stories 1.4, 2.4 (Folder invariant) and 5.12 (no-false-pass).

- **Touches:** Epics/stories artifacts (`epics.md`, story files, `sprint-status.yaml`). Addendum §"Current Readiness, Release Containment, and Supersession" and §8 evidence index (E-3, E-4) already describe this state. No FR/NFR text.
- **Classification: NO-PRD-IMPACT.** Epic-artifact bookkeeping; the FR/NFR count and Folder invariant are already the PRD baseline. (Addendum already records "Epics 1-5 remain immutable implementation history" and "23 Epic 6-8 placeholders as findings inventory.")

### 2.10 Replacement Epic 6/7/8 inventories (§4.5-4.7)
33 outcome stories (Epic 6: 7 authorized-read stories; Epic 7: 15 durable-decision/recovery stories incl. 7.1 create-with-one-Folder, 7.6 replace Folder, 7.11 confirm ambiguous, 7.12 confirm proposed, 7.13 archive, 7.14 restore, 7.15 reconcile; Epic 8: 11 safe-ops stories incl. 8.2 Safe Diagnostic Export, 8.11 deployment/acceptance). Entry gates pin EventStore/FrontComposer/identity/Builds/sibling capabilities.

- **Touches:** Epics/stories artifacts only. Each story outcome maps cleanly onto existing FRs (7.1↔FR-1, 7.2↔FR-3, 7.3↔FR-6, 7.4↔FR-7, 7.5/7.8/7.10↔FR-11, 7.6↔FR-8, 7.7/7.9↔FR-9/FR-10, 7.11↔FR-14, 7.12↔FR-15, 7.13↔FR-4, 7.14↔FR-23, 8.2↔FR-24, etc.).
- **Classification: NO-PRD-IMPACT.** Story decomposition; consistent with existing FRs (see §4 below — no conflicts).

### 2.11 Traceability + Story Entry (§4.8) and Sprint Tracking (§4.9)
Create `implementation-readiness-traceability-matrix.md`; no story `ready-for-dev` with unresolved TBD/failed-critical/missing-approval/incomplete traceability; after replacement stories approved, atomically swap 23 placeholders for 7/15/11, reroute action items, add `READY`-before-story-creation containment note, preserve production/MCP/proposal/sibling/evidence blocks.

- **Touches:** Traceability + `sprint-status.yaml` artifacts. No PRD/addendum text.
- **Classification: NO-PRD-IMPACT** (CI/tracking/traceability artifacts).

## 3. Summary classification table

| Proposal item | PRD/Addendum touched | Classification |
| --- | --- | --- |
| §4.1 PRD decision (no modification) | whole PRD | ALREADY-REFLECTED / NO-PRD-IMPACT |
| §4.2 A Ownership + spine | Addendum §1.1, §4.2 | ADDENDUM-ONLY (already reflected) → NO-PRD-IMPACT |
| §4.2 B Durable state/completion | Glossary, FR-1/14, §5, NFR-4/8; Addendum §1/§2 | ALREADY-REFLECTED + ADDENDUM-ONLY |
| §4.2 C Migration/repo authority | Addendum §5; NFR-10 | ADDENDUM-ONLY (already reflected) |
| §4.2 D Resolution/export/parity/evidence | FR-17/22/24, §2.4, NFR-6/7/11; Addendum §3/§7.2 | ALREADY-REFLECTED + ADDENDUM-ONLY |
| §4.3 A Chatbot companion | FR-14/15/20, NFR-9, §5; Addendum §6 | NO-PRD-IMPACT (UX artifact); already reflected |
| §4.3 B Roles/task/refresh/audit/MCP | §3.2, §5, FR-18/21/22 | NO-PRD-IMPACT; already reflected |
| §4.3 C Response + FR-24 UX | §5, FR-24 | NO-PRD-IMPACT; already reflected |
| §4.3 D Composition/scale/accessibility | NFR-5/6/9 (scale); UX (Fluent) | NO-PRD-IMPACT; already reflected |
| §4.4 Epic baseline/supersession | epics/story artifacts | NO-PRD-IMPACT |
| §4.5-4.7 33 replacement stories | epics/story artifacts | NO-PRD-IMPACT |
| §4.8 Traceability / §4.9 Sprint | traceability/sprint artifacts | NO-PRD-IMPACT |

**Net: 0 NEW-PRD-EDITs required. 0 ADDENDUM-EDITs required.** The proposal is a pure downstream-artifact repair that explicitly preserves the final PRD and addendum.

## 4. Conflict check (against prior decisions and existing PRD/addendum text)

No conflicts found. The proposal reaffirms — does not contradict — every prior decision:

- **Active only after one authorized Folder + read-model completion** — Change B §4.2 restates this verbatim; matches PRD §2.1, FR-1, NFR-4, glossary. Aligned.
- **Server preview + single-use confirmation for consequential actions** — Change B: "15-minute, server-issued, tamper-evident, atomically single-use Confirmation Artifacts"; matches PRD glossary + FR-4/7/8/11/14/15/23 + Addendum §2. Aligned.
- **FR-22 operator read / FR-23 Restore / FR-24 Safe Diagnostic Export** — §4.4 and Change D keep FR-22 = operator read and FR-24 = Safe Diagnostic Export; replacement stories 7.14 (Restore↔FR-23) and 8.2 (Export↔FR-24) preserve the split. Matches PRD §2.1 and Addendum supersession trace. Aligned.
- **Measurable NFRs** — export bounds (1 MiB / 500 / 100 / 2 concurrent), idempotency ≥30-day, 15-min expiry, scale 10,000/5,000/100,000 all match NFR-5/6/7/8 and FR-24 exactly. No drift.
- **Role matrix (Project User / Tenant Operator / Tenant Project Administrator / Service-Workflow Caller)** — UX Change B mandates "use the PRD role/action matrix." Aligned with §3.2.

**Bookkeeping observation (not a conflict, not a required edit):** The addendum's Evidence and Gate Index (§8) currently ends at E-9 (all dated ≤2026-07-15) and does not yet list this 2026-07-16 proposal. The proposal does **not** request an evidence-row addition and explicitly forbids addendum modification, so no edit is mandated. If the PRD-update run wants a complete evidence ledger it *could* optionally add an E-10 row pointing at `sprint-change-proposal-2026-07-16.md`, but that is discretionary bookkeeping, not a change the proposal asks for.

## 5. Declared status and explicit out-of-scope

- **Status: Approved** (Jerome, 2026-07-16). All 15 incremental edit decisions approved across the July 15-16 workflow.
- **Explicitly out of scope / not authorized by this approval** (§7, §"Immediate Containment"): corrective implementation; sibling-repository mutation; production release; consequential autonomous MCP operations; proposed-Project confirmation enablement; bypassing readiness; weakening any evidence gate. Rollback (Option 2) and PRD/MVP reduction (Option 3) were considered and rejected. Sprint-status reconciliation is deferred atomically until the 33 replacement stories are fully authored and approved and readiness returns `READY`.

## 6. Bottom line for the PRD-update run

Make **no PRD change and no addendum change** on account of this proposal. It is `approved` but scoped entirely to downstream artifacts (architecture spine, UX/Chatbot spec, epics/stories, traceability, test design, sprint tracking). Every product capability it names is already present in the final PRD (FR-1..FR-24, NFR-1..NFR-11, §5, §3.2) or its addendum (§1-§7). The only discretionary PRD-side action available is an optional evidence-ledger E-10 row in addendum §8, which the proposal does not request.
