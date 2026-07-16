# Reconcile Note: SCP 2026-07-16 Implementation-Readiness Rerun vs PRD + Addendum

- **Reconciler role:** reconciliation-extraction subagent (PRD Update run). This note does NOT modify the PRD.
- **Proposal analyzed:** `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-16-implementation-readiness-rerun.md`
- **Proposal title:** "Sprint Change Proposal: Materialize the Corrective Planning Layer"
- **Declared status:** `approved` (frontmatter `status: approved`, `approved: 2026-07-16`, `approved_by: Jerome`); Section 7 "Status: **Approved by Jerome on 2026-07-16**".
- **Scope declared:** Moderate. Mode: Batch.
- **Amends:** `sprint-change-proposal-2026-07-16.md` (an earlier same-day proposal). **Preserves:** `prd.md`, `addendum.md`, `ARCHITECTURE-SPINE.md`.
- **PRD baseline compared against:** `prd-Hexalith.Projects-2026-05-24/prd.md` (status: final, updated 2026-07-15, FR-1..FR-24, NFR-1..NFR-11) and its `addendum.md`.

---

## 1. What this rerun proposes to change (issue + recommended path)

### Issue / findings

An independent implementation-readiness rerun dated 2026-07-16 returned `NOT_READY` and **superseded the 2026-07-15 rerun**. It confirms the product + architecture rebaseline is substantively complete, but the final approved planning step was never executed:

- The corrective Epic 6–8 inventory is still **23 title-and-paragraph placeholders** (Stories 6.1–6.7, 7.1–7.7, 8.1–8.9) with no Given/When/Then acceptance criteria, verification commands, fixtures, evidence artifacts, estimates, or completion boundaries.
- The **AD-30 canonical evidence matrix does not exist**.
- `epics.md` top-of-file inventory still asserts **FR-1–FR-22 / NFR-1–NFR-9** and cites the **superseded May architecture**.
- Missing explicit AC owners for FR-19, FR-23, FR-24, NFR-2, NFR-7, NFR-8, and Chatbot NFR-9/SM-5.
- **Release-gate mismatch:** current Epic 8 ends at Story 8.9; AD-30 and the approved replacement inventory end at **Story 8.11**.
- Open evidence: live Chromium 19 passed / 56 failed, release handoff `BLOCKED`, nine P1 + seven P2 audit findings still open.

Classified as **incomplete execution of an approved planning correction** (an artifact-materialization gap), NOT a new product/architecture/MVP change.

### Recommended path forward — "Direct Backlog and Evidence Adjustment"

1. Atomically rewrite Epics 6–8 into the approved **7/15/11 = 33 outcome stories** with full BDD/ownership/dependency/verification/evidence/estimate/rollback detail (§4.4, §4.5).
2. Author canonical `implementation-readiness-traceability-matrix.yaml` (+ Markdown view), schema `hexalith.readiness-evidence.v1` (§4.7).
3. Reconcile traceability with explicit owners for FR-19/23/24, NFR-2/7/8, Chatbot accessibility, all P1/P2 findings, and Story 8.11 (§4.6).
4. Run independent implementation readiness in a fresh context.
5. Only if verdict is exactly `READY`, atomically reconcile `sprint-status.yaml` and begin normal create-story/dev one story at a time (§4.8).

The proposal states plainly (Section 2 "PRD Impact", Section 4.1): **"No PRD edit is required. The final PRD and addendum remain governing without scope reduction or renumbering."**

---

## 2. Per-change mapping to PRD / FR / NFR / addendum + classification

### Change A — §4.1 PRD: "No text change"

- **Proposal text:** *"OLD: Final 24-FR/11-NFR PRD and addendum. NEW: No text change. Preserve them as governing product and downstream-routing authority. Rationale: The readiness report found the PRD complete, testable, and internally consistent."*
- **Touches:** whole PRD (no specific FR/NFR).
- **Classification:** **ALREADY-REFLECTED / NO-PRD-EDIT.** The PRD is already final at FR-1..FR-24 / NFR-1..NFR-11. No action.

### Change B — §4.2 Architecture: epics.md must cite the final Architecture Spine

- **Proposal text:** OLD "`epics.md` frontmatter and content cite and encode the superseded May architecture." NEW: use `ARCHITECTURE-SPINE.md` as sole architecture input; keep `architecture.md` only as superseded historical evidence.
- **Touches:** `epics.md` frontmatter — NOT the PRD, NOT the addendum. PRD §0 already delegates technical mechanisms to the addendum; it does not cite the May architecture.
- **Classification:** **NO-PRD-IMPACT** (epics.md edit only).

### Change C — §4.3 Requirements Inventory and Historical Labels

- **Proposal text:** OLD (in epics.md) `Source: PRD §4 (FR-1–FR-22) … All 22 FRs mapped. NFR-1–9 are cross-cutting.` NEW `Source: final PRD §6–§8 and addendum (FR-1–FR-24; NFR-1–NFR-11) …`. Plus add supersession notices to Stories 1.4, 2.4, 5.12.
- **Touches:** `epics.md` inventory header + Epic 1/2/5 story records. On the PRD side, FR-1–FR-24 / NFR-1–NFR-11 are **already** the current numbering (PRD §6 lists FR-1..FR-24; §7 lists NFR-1..NFR-11). The addendum supersession trace already records "the pre-rebaseline FR-1–FR-22 and adds FR-23 … FR-24 … total of 24 Functional Requirements."
- **Classification:** **NO-PRD-IMPACT.** PRD side is ALREADY-REFLECTED; the stale "FR-1–FR-22 / NFR-1–9" text lives only in `epics.md`, not the PRD/addendum.

### Change D — §4.4 Replace the corrective story inventory atomically (23 → 33)

- **Proposal text:** Replace 23 placeholders with 7 stories in Epic 6, 15 in Epic 7, 11 in Epic 8 (tables of outcome titles). Epic 8 now ends at **Story 8.11** ("Complete deployment, rollback evidence, and stakeholder acceptance").
- **Touches:** `epics.md` only. Outcome titles map cleanly onto existing PRD FRs (e.g., 7.1 exactly-one-Folder → FR-1/FR-8; 7.14 Restore → FR-23; 8.2 Safe Diagnostic Export → FR-24; 8.5 agent-safe MCP → PRD §3.2 / §2.4 no-autonomous-MCP boundary). No FR/NFR is added, removed, or renumbered.
- **Classification:** **NO-PRD-IMPACT** (epics.md decomposition). See Conflict note 1 re Story 8.9→8.11 vs addendum.

### Change E — §4.5 Replacement Story Completion Contract

- **Proposal text:** a fixed story skeleton (As-a/I-want/So-that, Traceability, entry gates, 5 Given/When/Then AC classes, fixture, verification command, evidence artifact, estimate, rollback, completion boundary).
- **Touches:** `epics.md` story authoring convention. Fully consistent with PRD §5 recovery contract and NFR-11 no-false-pass rule.
- **Classification:** **NO-PRD-IMPACT.**

### Change F — §4.6 Explicit Requirement and Evidence Ownership

- **Proposal text:** table binding each gap to a primary owning story + evidence: FR-19/AD-31/E-9 → 7.1 (+6.7); FR-23 → 7.14; FR-24 → 8.2 (`projects.safe-diagnostic-export.v1`, 1 MiB/500/100); NFR-2 → 8.11 (+8.6/8.7); NFR-7 → 8.9; NFR-8 → 8.1/8.2 (+Epic 7); Chatbot NFR-9/SM-5/AD-34 → 8.8; nine P1 + seven P2 findings → applicable 6.x–8.x; release handoff + live E2E → 8.8/8.10/8.11.
- **Touches:** traceability matrix + `epics.md`. Every FR/NFR reference is **verified consistent** with the current PRD text:
  - FR-19 "Validate Project Setup" carries the Metadata Classification requirement. ✓
  - FR-23 "Restore Archived Project" (Preview/confirmation, Folder validation, `NeedsAttention`, no owner-resource deletion). ✓
  - FR-24 "Create Safe Diagnostic Export" (1 MiB / 500 refs / 100 audit, no cursor, no retention, separate permission). ✓ (matches PRD FR-24 + addendum §3)
  - NFR-2 encryption/KMS ✓; NFR-7 back-pressure ✓; NFR-8 retention (30-day / 15-min confirmation / 365-day audit) ✓; NFR-9/SM-5 accessibility ✓.
- **Classification:** **NO-PRD-IMPACT** (traceability/evidence ownership; PRD unchanged and self-consistent).

### Change G — §4.7 Canonical AD-30 Evidence Matrix (NEW artifact)

- **Proposal text:** OLD "No YAML or Markdown traceability matrix exists. The prior proposal mentioned only a Markdown matrix." NEW: create `implementation-readiness-traceability-matrix.yaml` (canonical, schema `hexalith.readiness-evidence.v1`) + `.md` view; row coverage FR-1–FR-24, NFR-1–NFR-11, nine P1 + seven P2 findings, all critical release categories per NFR-11/AD-30. Target gate command `dotnet tool run hexalith-evidence validate …` (declared not-yet-existing; row must truthfully record external blocker until Builds/platform supplies AD-30/G-4).
- **Touches:** new secondary artifacts under `_bmad-output/planning-artifacts/`. Ties to PRD NFR-11 (release evidence) but requires no PRD text change.
- **Classification:** **NO-PRD-IMPACT** (new evidence artifacts). This is the primary **genuinely-new** element vs the 2026-07-14 correction (which mentioned only a Markdown matrix).

### Change H — §4.8 Sprint Tracking

- **Proposal text:** leave `sprint-status.yaml` unchanged during planning; after and only after a fresh `READY`, atomically replace the 23 placeholder keys with 33 approved keys, keep Epics 1–5 `done`, Epics 6–8 `backlog`, preserve all gates.
- **Touches:** `sprint-status.yaml` (implementation artifact). Consistent with PRD §2.5 planning status and the READY-before-sprint decision.
- **Classification:** **NO-PRD-IMPACT.**

---

## 3. Net PRD / Addendum impact

**Zero PRD edits. Zero addendum edits.** The proposal is entirely a planning-artifact materialization: `epics.md` rewrite, a new evidence matrix (YAML + MD), traceability reconciliation, sprint-status reconciliation (deferred until READY), and an independent readiness rerun. Every FR/NFR referenced by the proposal already exists in the finalized PRD at the same ID and with consistent semantics.

- **NEW-PRD-EDIT:** none.
- **ADDENDUM-ONLY (technical how):** none proposed — the proposal explicitly preserves the addendum unchanged.
- **ALREADY-REFLECTED:** the FR-1–FR-24 / NFR-1–NFR-11 numbering, the FR-22 read / FR-23 Restore / FR-24 Safe Diagnostic Export split, the Active-only-after-Folder invariant, the Preview/single-use-confirmation boundary, the release-blocking NFR set (PRD §2.3), and the READY-before-sprint gate are all already in the PRD/addendum.
- **NO-PRD-IMPACT (implementation/story/CI/readiness-tracking):** Changes B, C, D, E, F, G, H.

---

## 4. Conflicts / flags

1. **Addendum staleness — Story 8.9 vs Story 8.11 release gate (INFORMATIONAL, not a PRD conflict).** The new inventory moves the terminal release gate from Story 8.9 to **Story 8.11**. The current `addendum.md` still references **Story 8.9** as the release-evidence gate in two places: the "Current Readiness, Release Containment, and Supersession" section ("Story 8.9 release evidence (E-8) passes") and the Evidence and Gate Index rows **E-3** and **E-4** ("Story 8.9"). The proposal explicitly **preserves the addendum with no edit**, so these references will be stale relative to the materialized epics until a future addendum revision reconciles them. This is an addendum-level inconsistency to note for the PRD-Update run; it is **not** a PRD-text conflict and does not require a PRD edit. (Note also that addendum E-3/E-4 story-number semantics for "Story 8.6" live-E2E remediation shift under the new 11-story Epic 8, where accessibility/parity/live-E2E now maps to Story 8.8 per §4.6.)

2. **No conflict with any prior decision or PRD text.** All prior decisions (Active-only-after-Folder + read-model completion; server preview + single-use confirmation; FR-22 read / FR-23 Restore / FR-24 Safe Diagnostic Export; measurable NFRs; four-role matrix) are honored by the proposal. No FR/NFR renumbering, no scope reduction, no new FR/NFR. PRD §2.4 "no autonomous MCP confirmation" is preserved (Story 8.5 is "agent-safe MCP contracts"; containment keeps consequential autonomous MCP blocked until 8.11).

---

## 5. New vs already-applied (distinguishing this rerun from the 2026-07-14 correction)

Already reflected in the addendum from the 2026-07-14/07-15 work: FR-1–FR-24 / NFR-1–NFR-11 rebaseline; the 23 placeholders as findings inventory; READY-before-sprint gate; 19/56 live E2E result; nine P1 + seven P2 findings; supersession trace; FR-24 owns Safe Diagnostic Export.

**Genuinely new in this 2026-07-16 rerun:**
- Trigger: fresh 2026-07-16 `NOT_READY` that supersedes the 2026-07-15 rerun.
- 23 → **33** story decomposition (7/15/11), with the full completion contract (§4.5).
- Terminal release gate moved **8.9 → 8.11** (deployment + rollback + dated Jerome/John acceptance).
- **Canonical YAML** evidence matrix + schema `hexalith.readiness-evidence.v1` and the `hexalith-evidence validate` target gate (prior proposal mentioned only a Markdown matrix — §4.7 states this explicitly).
- Explicit per-gap evidence-ownership table (§4.6).

None of the above touch PRD text; all are epics/traceability/tracking layer.

---

## 6. Status / out-of-scope declarations

- **Status:** Approved by Jerome on 2026-07-16 (frontmatter + Section 7 + Workflow Execution Log Section 8).
- **Explicit out-of-scope (Section 7 + Immediate Containment §5):** approval authorizes planning-artifact correction only. It does NOT authorize corrective implementation, story-file creation, sprint activation, sibling-repository mutation, production release, consequential autonomous MCP operations, proposed-Project confirmation, or weakening any evidence gate. Developer receives no handoff until an independent assessment returns exactly `READY`, sprint tracking is reconciled atomically, and an approved story is subsequently created as `ready-for-dev`.
- **Checklist Section 6.4** is marked `[N/A]`: `sprint-status.yaml` stays unchanged until an independent rerun returns exactly `READY`.

---

## 7. Recommendation to the PRD-Update run

**Make no edit to `prd.md` for this proposal.** The proposal itself asserts, and this reconciliation confirms, that the finalized PRD (FR-1..FR-24, NFR-1..NFR-11) is complete, testable, internally consistent, and requires no change. All work is downstream (epics.md, evidence matrix, sprint tracking, readiness rerun).

Optional (out of PRD-Update scope, flag to Product Owner / addendum owner): when the epics are materialized, the `addendum.md` references to the release gate as **Story 8.9** (Current Readiness section + Evidence/Gate Index E-3, E-4) will be stale against the new **Story 8.11** terminal gate and should be reconciled in a future addendum revision — but only per the addendum's own supersession rule (Section 8), and not as part of this PRD-Update run.
