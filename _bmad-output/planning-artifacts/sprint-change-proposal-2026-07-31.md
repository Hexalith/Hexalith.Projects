# Sprint Change Proposal: U+2028/U+2029 Idempotency Parity Duplicate-Trigger Closure

**Date:** 2026-07-31
**Project:** Hexalith.Projects
**Prepared for:** Jerome
**Status:** Draft — awaiting review and explicit approval
**Change scope:** Minor closure; no new implementation or backlog change recommended

## 1. Issue Summary

The requested action is to add U+2028/U+2029 idempotency canonicalizer parity coverage across server and generated-client fingerprinting. That action described a real historical defect: accepted descriptive metadata containing Unicode LINE SEPARATOR (`U+2028`) or PARAGRAPH SEPARATOR (`U+2029`) could produce different server and generated-client hashes, while identifier and envelope contexts also needed explicit rejection.

The current repository no longer contains that gap. The exact correction was approved, reconciled, implemented, reviewed twice, and recorded as done. This invocation is therefore a duplicate trigger against completed work rather than a new sprint change.

### Trigger and Evidence

- The original trigger was recorded during Story 2.5 review and carried through the Epic 2–5 retrospective/action chain.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-14-u2028-u2029-idempotency-parity.md` approved the direct correction on 2026-07-14.
- The final PRD reconciliation records the requirement as fully reconciled, with no current PRD/addendum gap.
- `_bmad-output/implementation-artifacts/spec-u2028-u2029-idempotency-canonicalizer-parity-coverage.md` is `status: done`, after two review-loop expansions that covered trimming, request-wide raw fingerprint selection, bounds, every affected mutation helper, and proposal confirmation.
- Commit `88be25e2f819d1f66a0c8d652121442d6444582b` (`fix(idempotency): align Unicode separator canonicalization`, 2026-07-19) contains the committed correction.
- The relevant production and test files are clean in the current working tree.
- Current code explicitly escapes U+2028/U+2029 as lowercase literal backslash-`u` sequences in server fingerprinting and rejects both separators in envelope identifiers.
- Existing direct parity tests invoke real generated helpers for Create Project, Update Project Setup, Set Project Folder, Link File Reference, Link Memory, and proposal confirmation. They cover leading, embedded, trailing, and separator-only values where valid; LF/literal-backslash-u non-collision; raw-length rejection; unaffected-hash stability; safe identifier rejection; and generated-file immutability.
- The completed implementation record reports 656/656 domain tests, 114/114 client tests, 556/556 server tests, a warning-free solution build, a current OpenAPI fingerprint gate, unchanged generated artifacts, and a clean whitespace check. These are recorded results from 2026-07-19, not a new 2026-07-31 execution claim.

## 2. Impact Analysis

### Epic Impact

- **Epics 1–5:** No change. They remain implementation history rather than current production authority. Reopening historical Story 1.3, Story 1.5, mutation stories, or retrospectives would rewrite ownership history without adding coverage.
- **Epic 6:** No impact. It owns supported reads and compatibility-controlled read cutover, not mutation fingerprint computation.
- **Epic 7:** No new story is required. Its durable mutation stories consume the shared idempotency invariant already covered by the completed correction.
- **Epic 8:** Story 8.10 remains the production-authority resilience owner for NFR-4, and Story 8.11 remains the terminal release gate. The completed parity tests are useful implementation evidence, but they do not by themselves prove persisted-boundary deployment compatibility or complete either story.
- **Ordering and priority:** No resequencing, reprioritization, new epic, or obsolete epic results from this duplicate trigger.

### Story Impact

No story should be added, reopened, removed, or renumbered. The completed frozen implementation spec is the authoritative implementation record for this bounded correction. Any new story with the same intent would duplicate already-completed work and create conflicting status authority.

### Artifact Conflicts

- **PRD/addendum:** No conflict and no edit. FR-1, FR-19, NFR-4, NFR-10, NFR-11, addendum §2, and addendum §7.3 already preserve retry equivalence, Unicode parity, safe rejection, compatibility, and evidence obligations.
- **Architecture:** No conflict and no edit. The current Architecture Spine consistency convention explicitly requires U+2028/U+2029 rejection in identity/envelope fields, byte-identical server/generated-helper escaping for descriptive metadata, non-collision vectors, stable unaffected hashes, generated-file protection, and the deployed-fingerprint gate.
- **UX:** No impact. The correction changes neither visual behavior nor interaction flow.
- **Sprint status:** No edit. There is no new backlog item to track, and existing unrelated user changes in `sprint-status.yaml` must remain untouched.
- **Implementation spec:** No edit. It is frozen after approval and marked done.
- **Testing/CI:** No new coverage gap was identified. A fresh execution may be attached later to Story 8.10/8.11 evidence, but that is verification work, not a duplicate implementation change.

### Technical and Deployment Impact

No new code, schema, OpenAPI, generated artifact, package, topology, or deployment change is proposed. The historical deployment-state caveat remains valid: repository tests do not prove that a live durable ledger lacks legacy separator-bearing fingerprints. Production release remains governed by the current Epic 8 evidence and terminal acceptance gates.

## 3. Recommended Approach

### Chosen Path: Direct Closure Without Duplicate Adjustment

Close this invocation as already satisfied and preserve the current backlog and artifacts.

This path is recommended because:

- The requested implementation and parity matrix already exist in committed code.
- The current PRD, addendum, and Architecture Spine already contain the durable requirement.
- A rollback would remove a valid safety correction.
- An MVP review is unnecessary because no product scope or objective changed.
- A second story or implementation spec would create duplicate authority and misleading sprint status.

### Estimate, Risk, and Timeline

- **Effort:** No implementation effort. Administrative review/closure only.
- **Schedule impact:** None.
- **Implementation risk:** None from this proposal because it changes no runtime artifact.
- **Residual risk:** A future production evidence claim must still satisfy the deployed-state, persisted-boundary, compatibility, and release gates; historical unit/integration results are not deployment proof.
- **Risk if this duplicate is treated as new work:** Conflicting story/status authority and unnecessary churn in frozen or historical artifacts.

## 4. Detailed Change Proposals

### 4.1 Epics and Stories

**Artifact:** `_bmad-output/planning-artifacts/epics.md`
**Section:** NFR-4 ownership across Epic 7 and Story 8.10

**CURRENT:**

The current production plan assigns durable mutation behavior across Epic 7 and adversarial durability/idempotency evidence to Story 8.10.

**PROPOSED:**

No change. Do not add, reopen, remove, or renumber a story.

**Rationale:** The requested parity work is already a completed implementation record; Story 8.10 remains the correct production-evidence owner.

### 4.2 PRD and Addendum

**Artifacts:** `prd.md`, `addendum.md`

**CURRENT:**

The product and verification contracts already require equivalent retries, U+2028/U+2029 parity, safe identifier/envelope rejection, unaffected-hash stability, non-collision, generated-file integrity, and deployment compatibility treatment.

**PROPOSED:**

No change.

**Rationale:** The final reconciliation explicitly found no remaining requirement gap.

### 4.3 Architecture

**Artifact:** `architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md`
**Section:** Consistency Conventions → Serialization and canonicalization

**CURRENT:**

The canonicalization convention already states the exact separator, parity, collision, stable-hash, generated-artifact, and deployed-fingerprint rules.

**PROPOSED:**

No change.

**Rationale:** The current architecture already captures the intended invariant at the correct level.

### 4.4 UX

**Artifact:** `_bmad-output/planning-artifacts/ux-design-specification.md`

**CURRENT / PROPOSED:**

No change.

**Rationale:** Fingerprint canonicalization has no user-interface or accessibility effect.

### 4.5 Sprint Tracking and Implementation Record

**Artifacts:** `_bmad-output/implementation-artifacts/sprint-status.yaml`, `spec-u2028-u2029-idempotency-canonicalizer-parity-coverage.md`

**CURRENT:**

The implementation spec is frozen and done; sprint status contains unrelated in-progress user changes.

**PROPOSED:**

Leave both unchanged. Record this proposal only as a duplicate-trigger closure after approval.

**Rationale:** This preserves user work, avoids duplicate backlog authority, and respects the frozen implementation intent.

## 5. Implementation Handoff

### Scope Classification

**Minor — administrative closure only.** No Developer implementation handoff is required.

### Recipients and Responsibilities

- **Jerome / Project Lead:** Review and explicitly approve, reject, or revise this duplicate-trigger closure.
- **Product Owner:** After approval, treat the requested action as already satisfied and create no new backlog entry.
- **Test Architect:** Reuse or freshly execute the existing focused lanes only when assembling current Story 8.10/8.11 evidence; record new results with their execution date and environment.
- **Developer:** No action unless a current failing parity test or new uncovered mutation surface is produced as evidence.

### Success Criteria

1. No duplicate story, epic, PRD, architecture, UX, sprint-status, or implementation-spec edit is created.
2. The completed parity implementation remains the single bounded implementation record.
3. Any future release claim distinguishes repository test evidence from deployed-state compatibility evidence.
4. A future correction is opened only if current evidence demonstrates a regression or an uncovered fingerprint surface.

## Checklist Disposition

- [x] **1.1–1.3 Trigger/context:** Historical trigger, defect category, and concrete implementation/test evidence identified.
- [x] **2.1–2.5 Epic impact:** No epic scope, dependency, ordering, or priority change required.
- [x] **3.1 PRD conflict:** None; requirements already cover the invariant.
- [x] **3.2 Architecture conflict:** None; the current spine already states the exact convention.
- [N/A] **3.3 UX impact:** No visual, journey, interaction, or accessibility effect.
- [x] **3.4 Other artifacts:** Implementation, tests, generated-file gate, sprint tracking, and deployment evidence boundaries assessed.
- [x] **4.1 Direct adjustment:** Viable only as direct closure; no duplicate implementation adjustment.
- [N/A] **4.2 Rollback:** Not viable or beneficial.
- [N/A] **4.3 MVP review:** Not required; MVP scope remains achievable and unchanged.
- [x] **4.4 Recommended path:** Direct duplicate-trigger closure selected.
- [x] **5.1–5.5 Proposal components:** Issue, impact, recommendation, MVP effect, and handoff recorded.
- [x] **6.1–6.2 Review readiness:** Draft is internally consistent and evidence-backed.
- [!] **6.3 Explicit approval:** Awaiting Jerome's decision.
- [N/A] **6.4 Sprint-status update:** No epic/story addition, removal, or renumbering.
- [!] **6.5 Final handoff:** Pending explicit approval.

## Review Record

- **Mode:** Incremental
- **Drafted:** 2026-07-31
- **Approval status:** Awaiting review; this document does not yet authorize or record final closure.
