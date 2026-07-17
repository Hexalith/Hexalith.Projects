---
title: "Solution-Architect Conformance Checklist — Corrective Epics 6–8"
project: Hexalith.Projects
created: 2026-07-16
purpose: "Pre-readiness-rerun conformance review of the 33 corrective stories against the Architecture Spine (AD-1..34) and external entry gates (G-1..G-6)."
authority: architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md
reviews:
  - epics.md (Corrective Production Plan, Epics 6–8)
  - implementation-readiness-traceability-matrix.yaml
status: draft-for-review
---

# Solution-Architect Conformance Checklist — Corrective Epics 6–8

**Reviewer role (per SCP-07-16 / -rerun §5):** verify every corrective story conforms to its
applicable ADs and G-1…G-6 entry gates, and **prevent** implicit sibling/platform authority, event
rewrite, unsafe dual writes, or false target-dependency claims. This is a **planning conformance
review** — no implementation is authorized. Historical Epics 1–5 are evidence, not review scope.

**How to use.** Walk Section A once (applies to all 33 stories), confirm Section B gate status, then
tick Section C per story. Section D is the AD-1…34 coverage cross-check. Record the verdict in
Section E. Any unchecked box in A or a per-story blocker is a conformance finding to resolve before
the independent readiness rerun.

**Verdict scale:** `conforms` · `conforms-with-note` · `non-conformant (blocking)`.

---

## Section A — Cross-cutting conformance gate (applies to EVERY corrective story)

These invariants bind all 33 stories regardless of their own AD list. A single failure here is a
blocking finding.

- [ ] **A1 — No platform-runtime reimplementation (AD-1).** Each story consumes hosting, persistence,
      publication, projections, cursors, health, telemetry, and durable-workflow capability from
      the EventStore/platform; nothing re-hosts or re-implements them inside Projects.
- [ ] **A2 — No implicit sibling authority (AD-6/AD-11).** No story authorizes a Conversations/
      Folders/Memories mutation on the sibling's behalf or copies sibling payload; owners keep
      existence/payload/lifecycle/authorization. Cross-repo work needs separate repository-local
      approval + pinned revision.
- [ ] **A3 — No event-history rewrite / no unsafe dual writer (AD-17/AD-22).** Additive,
      serialization-tolerant evolution only; single-writer cutover; `ProjectFolderCreationPending`
      deserializer/apply retained.
- [ ] **A4 — Prior-only dependencies (SCP §Story Impact).** No story depends on a later story; the
      only upstream is its epic entry gate + named earlier stories. Reject any forward/false
      dependency claim.
- [ ] **A5 — Denial indistinguishable from nonexistence (AD-19).** Every read/action fails closed
      with denial ≡ absence (safe `404`); no existence or protected-metadata leakage.
- [ ] **A6 — Dual-principal auth + universal reauthorization (AD-20).** Tenant + original actor +
      workload identity carried and revalidated by Projects, each owner, and queries; allow-all only
      in explicit test composition; host fails fast on incomplete authority/audience/signing/key.
- [ ] **A7 — Metadata-only everywhere (NFR-1/AD-26).** Audit, logs, telemetry, errors, exports carry
      metadata only — never transcripts, file contents, memory payloads, prompts, secrets, tokens,
      full command bodies, or unrestricted paths. Durable audit is separate from telemetry.
- [ ] **A8 — Contract & package discipline (AD-16/AD-18/AD-24).** Editable types only in
      `Projects.Contracts` (dependency-light; no Shell/Fluxor/Fluent/ASP.NET-App/Dapr/Aspire);
      generated artifacts never hand-edited; ULID identifiers; central package versions
      (Builds-owned NSwag/Fluxor); target package inventory enforced.
- [ ] **A9 — Response snapshot & lifecycle vocabulary (AD-32).** Reads carry
      `responseState`/`asOf`/`projectVersion`/`components`/`recoveryActions`; lifecycle is only
      `Active`/`Archived`; neither `Partial` nor `Unavailable` selects a candidate.
- [ ] **A10 — Estimates are not commitments.** No story implies a release date; corrective work stays
      unscheduled until the independent rerun returns `READY`.

---

## Section B — External entry-gate (G-1…G-6) readiness

Gates are prerequisites, **not** delivered value. A story cannot pass conformance for execution
while its gate is unmet; it may still pass *planning* conformance (spec correctness).

| Gate | Unblocks | Blocked stories (primary) | Status (2026-07-16) |
|---|---|---|---|
| **G-1** platform Durable Task + Confirmation Artifact engine (AD-4/9/13) | all durable writes | 7.1–7.14, 6.7 (command path), 8.10 | ☐ pending platform owner |
| **G-2** sibling owner contracts (expected-version, idempotency, receipt/status, batch-read, compensation) (AD-12) | cross-context sagas | 7.3–7.10, 7.4, 6.7 | ☐ pending — subsumes AR-G1…G4 |
| **G-3** FrontComposer adapters + 4.0.0/4.0.1 disposition (descriptor/schema/credential/MCP parity) | Web/MCP surfaces | 6.5, 8.3, 8.5, 8.8 (a11y) | ☐ pending G-3 disposition |
| **G-4** platform composition runner + `hexalith-evidence` tool (AD-25/AD-30) | fixtures, CI, evidence gate | 8.1, 8.11, all evidence rows | ☐ tool `not-available` |
| **G-5** identity/KMS/secrets/telemetry bindings (AD-20/AD-28) | auth, encryption, admission | 6.5/6.6, 8.6, 8.11, NFR-2 | ☐ pending G-5 |
| **G-6** runtime/toolchain alignment (Dapr runtime↔SDK, Fluent UI RC, CommunityToolkit, NSubstitute RC, Fluxor governance) | build/UI | 8.3, 8.7, 7.15 | ☐ pending G-6 |

- [ ] **B-check:** every story's declared entry gate matches its actual dependencies (no story claims
      readiness ahead of an unmet gate; no story silently depends on an unpinned capability).

---

## Section C — Per-story conformance checklist

Each story lists its **ADs** (from the epics traceability line), **gate** dependencies, and targeted
conformance assertions. Tick each assertion; mark the story verdict at the end of its block.

### Epic 6 — Authorized Project Reads on the Supported Platform (reads only; no writes)

- **6.1 List/open** — ADs 3,14,19,20,32,33 · Epic-6 gate
  - [ ] Reads via `IDomainQueryHandler` + opaque `QueryCursorScope`; default 50 / cap 200; AD-32 snapshot present.
  - [ ] Shadow-read equivalence gate precedes any routing switch (routing reversible).
  - [ ] No write/side effect; candidate never selected.
- **6.2 Conversation-start setup** — ADs 3,14,19,32 · Epic-6 gate
  - [ ] Returns only the start subset; excludes audit metadata; `Unavailable` blocks first-response admission.
- **6.3 Project Context (get/refresh/explain)** — ADs 7,11,14,32 · Epic-6 gate
  - [ ] Allowlist inclusion (tenant+project+lifecycle+authz+freshness); exclusions carry reason codes; refresh is read-only; explanation is transient/non-persisted; NoPayloadLeakage.
- **6.4 Resolution reads** — ADs 7,10,11,14,32 · Epic-6 gate + G-2
  - [ ] Compute-on-demand; Resolution Trace **not persisted**; selects nothing (confirm is 7.11/7.12); raw file content never treated as data.
- **6.5 FrontComposer read surface** — ADs 2,19,20,29,32,33,34 · Epic-6 gate + G-3/G-5
  - [ ] Platform-provided identity (no client-supplied authority); role-specific read-only visibility; WCAG 2.2 AA operator evidence.
- **6.6 CLI read surface** — ADs 2,19,20,29,33 · Epic-6 gate + G-5
  - [ ] Deterministic JSON + stable exit codes; no color-dependent meaning; parity with 6.5.
- **6.7 Read cutover** — ADs 1,6,16,17,18,22,24,25,31 · Epic-6 gate + G-1/G-2/G-4
  - [ ] Equivalence gate + ULID/OpenAPI/generated-consumer alignment before switch; routing reversible; legacy retained until 8.11; **no history rewrite, no dual writer**; legacy runtime read plumbing retired here (AD-1).
  - _Verdict: ☐ conforms ☐ with-note ☐ blocking_

### Epic 7 — Durable Project Decisions and Cross-Context Recovery (all writes)

_Shared invariants 1–6 (AD-4/5/9/12/13/22/26) apply to every 7.x story — confirm each is honored, not just referenced._

- **7.1 Create (Folder-first)** — ADs 3,8,12,18,22,31 · Epic-7 gate + G-1/G-2
  - [ ] No observable folderless-Active interval; metadata classified (AD-31) **before** command submit; Folder-created/uncommitted → `NeedsAttention`, never auto-delete Folder; name-only compatibility preserved.
- **7.2 Update Setup** — ADs 5,15,16,31 · Epic-7 gate + G-1
  - [ ] Task-only (no confirmation); shared validator reused; equivalent-retry-same-task; changed-request conflict.
- **7.3 Link Conversation** — ADs 10,12,14 · Epic-7 gate + G-2
  - [ ] Conversations remains system of record; reverse index only (aggregate stores no membership); already-in-another-Project → move required.
- **7.4 Move Conversation** — ADs 5,10,12,13 · Epic-7 gate + G-1/G-2 · **confirmation-required**
  - [ ] Prior membership removed before new one; saga compensates/`NeedsAttention` on mid-flight crash; stale artifact → `409`+`RenewPreview`.
- **7.5 Unlink Conversation** — ADs 5,10,12,13 · confirmation-required
  - [ ] Conversation not deleted; reverse index membership removed via owner; stale/replay fails closed.
- **7.6 Replace Folder** — ADs 3,11,12,13 · confirmation-required
  - [ ] New Folder verified before bind; exactly-one-Folder throughout; **remove-from-Active rejected**; no owner auto-delete.
- **7.7 Link File Reference** — ADs 11,12,15 · Epic-7 gate + G-2
  - [ ] Task-only additive; no Folder change; ≤5,000 refs; safe reason code on denial (no raw upstream detail).
- **7.8 Unlink File Reference** — ADs 5,11,12,13 · confirmation-required
  - [ ] File not deleted; stale/replay fails closed.
- **7.9 Link Memory** — ADs 11,12,15 · Epic-7 gate + G-2
  - [ ] Metadata-only; Case-vs-Unit per pinned G-2 contract; tolerant of async/`[Experimental]` ingestion; no payload copy.
- **7.10 Unlink Memory** — ADs 5,11,12,13 · confirmation-required
  - [ ] Memory not deleted; stale/replay fails closed.
- **7.11 Confirm ambiguous** — ADs 5,13,29,32,34 · confirmation-required
  - [ ] No preselection; rejected candidates not linked; **MCP cannot self-confirm (AD-29)**; lost-response converges to single association.
- **7.12 Confirm proposed** — ADs 5,8,13,31 · confirmation-required
  - [ ] Reuses 7.1 Folder-first + classification; no Project before confirmation; autonomous MCP confirmation disabled.
- **7.13 Archive** — ADs 4,5,13 · confirmation-required
  - [ ] Lifecycle→Archived after read-model confirm; references stay auditable; duplicate/lost-response converge; **restore is 7.14**.
- **7.14 Restore** — ADs 3,4,5,8,13,23 · confirmation-required
  - [ ] Folder validity established **while Archived**; `ProjectFolderSet` before `ProjectRestored` in one commit; Folder-created/failed-activation → `NeedsAttention`, no auto-delete; no invalid Active exposed on stale/replay/cancel/duplicate/lost-response.
- **7.15 Reconcile legacy/interrupted** — ADs 12,17,22,30 · Epic-7 gate + G-4/G-5/G-6
  - [ ] Compensating task per legacy record; no history rewrite/dual writer; unreconcilable → `NeedsAttention` (honest blocker, not false success); single-writer command cutover completes.
  - _Verdict: ☐ conforms ☐ with-note ☐ blocking_

### Epic 8 — Safe Operations and Release Confidence (evidence + release)

- **8.1 Task/audit/reconciliation reads** — ADs 21,26,30 · Epic-8 gate
  - [ ] Metadata-only; audit ≥365d, task/idempotency ≥30d; traces/exports absent; telemetry separated from audit (AD-26).
- **8.2 Safe Diagnostic Export** — ADs 7,19,21,26,27 · Epic-8 gate
  - [ ] **Separate** permission (Chatbot rejected); ≤1 MiB/500/100; deterministic order; no cursor/retention; two-lease/Tenant; every attempt audited; unavailable components marked safely.
- **8.3 Web console** — ADs 2,19,29,32,33,34 · Epic-8 gate + G-6
  - [ ] Maintenance actions drive **Epic 7 durable tasks** with Preview+confirmation; reevaluate = read-only Refresh; Fluent V5 + `FluentAccordion` (HTML prototype non-normative); no client authority.
- **8.4 CLI contract** — ADs 2,19,29,33 · Epic-8 gate
  - [ ] Explicit target+confirmation/dry-run for mutations; deterministic JSON+exit codes; parity with 8.3/8.5.
- **8.5 MCP contracts** — ADs 2,19,20,29,33 · Epic-8 gate
  - [ ] Resources vs tools separated; structured metadata **plus** short explanation; cannot bypass Preview/admission or expand authority; **cannot self-confirm**; autonomous consequential mutation **disabled** until gates pass.
- **8.6 Health/telemetry** — ADs 20,26,28 · Epic-8 gate
  - [ ] Real dependency/projection state (no always-green); source-generated logs (no payloads/names/tokens); **fail-fast** on incomplete identity/key config (AD-20/28).
- **8.7 Packaging/supply chain** — ADs 16,24,25 · Epic-8 gate + G-6
  - [ ] AD-24 target inventory enforced; `Client.Generation`/`.Shared` retired only after generator reproduces output+fingerprints; central versions; reproducible+signed; boundary violations fail CI.
- **8.8 Parity/isolation/accessibility** — ADs 19,20,29,33,34 · Epic-8 gate
  - [ ] Cross-surface parity + cross-Tenant isolation + NoPayloadLeakage **blocking** (supersede 19/56); WCAG 2.2 AA operator evidence; **Chatbot companion evidence has a separately approved owner + pinned revision** (else release blocked).
- **8.9 Performance/back-pressure** — ADs 14,15,21,27 · Epic-8 gate
  - [ ] Perf at small/median/max (reads p95<500ms/<1s; admission p95<500ms); paging 50/200; per-Tenant limits reject **before** partial durable work; no retry/quota logic in domain handlers.
- **8.10 Resilience** — ADs 4,9,12,17,28 · Epic-8 gate + G-1
  - [ ] Restart/two-instance resume-or-`NeedsAttention` ≤5min (RTO 15min, RPO 0); duplicate/lost-response/concurrency converge; fenced ownership; reconciliation reaches terminal disposition.
- **8.11 Deployment/rollback/acceptance** — ADs 25,28,30 · Epic-8 gate + G-4/G-5 · **TERMINAL**
  - [ ] `hexalith-evidence validate` rejects placeholders/missing-owner/failed-critical/`passed`-on-unavailable; deployment+encryption/KMS+smoke+rollback-drill recorded; **dated Jerome+John acceptance**; **cannot complete by recording a blocker**.
  - _Verdict: ☐ conforms ☐ with-note ☐ blocking_

---

## Section D — AD-1…34 coverage cross-check

Every Architecture Decision traces to ≥ 1 corrective story (from epics traceability lines). Confirm
each mapping is *substantive* (the story actually realizes the AD, not just cites it). ✅ = full coverage.

| AD | Owning / referencing stories | AD | Owning / referencing stories |
|---|---|---|---|
| AD-1 | 6.7 | AD-18 | 6.7, 7.1 |
| AD-2 | 6.5, 6.6, 8.3, 8.4, 8.5 | AD-19 | 6.1, 6.2, 6.5, 6.6, 8.2–8.5, 8.8 |
| AD-3 | 6.1, 6.2, 7.1, 7.6, 7.14 | AD-20 | 6.1, 6.5, 6.6, 8.5, 8.6, 8.8 |
| AD-4 | 7.13, 7.14, 8.10 | AD-21 | 8.1, 8.2, 8.9 |
| AD-5 | 7.2, 7.4, 7.5, 7.8, 7.10–7.14 | AD-22 | 6.7, 7.1, 7.15 |
| AD-6 | 6.7 | AD-23 | 7.14 |
| AD-7 | 6.3, 6.4, 8.2 | AD-24 | 6.7, 8.7 |
| AD-8 | 7.1, 7.12, 7.14 | AD-25 | 6.7, 8.7, 8.11 |
| AD-9 | 8.10 | AD-26 | 8.1, 8.2, 8.6 |
| AD-10 | 6.4, 7.3, 7.4, 7.5 | AD-27 | 8.2, 8.9 |
| AD-11 | 6.3, 6.4, 7.6–7.10 | AD-28 | 8.6, 8.10, 8.11 |
| AD-12 | 7.1, 7.3–7.10, 7.15, 8.10 | AD-29 | 6.5, 6.6, 8.3, 8.4, 8.5, 8.8 |
| AD-13 | 7.4–7.6, 7.8, 7.10–7.14 | AD-30 | 8.1, 8.11 |
| AD-14 | 6.1–6.4, 7.3, 8.9 | AD-31 | 6.7, 7.1, 7.2, 7.12 |
| AD-15 | 7.2, 7.7, 7.9, 8.9 | AD-32 | 6.1–6.4, 7.11, 8.3 |
| AD-16 | 6.7, 7.2, 8.7 | AD-33 | 6.1, 6.5, 6.6, 8.3, 8.4, 8.5, 8.8 |
| AD-17 | 6.7, 7.15, 8.10 | AD-34 | 6.5, 7.11, 8.3, 8.8 |

- [ ] **D-check:** no AD is covered *only* by a citation with no realizing acceptance criterion.
      (AD-1 currently traces only to 6.7 — confirm 6.7's ACs actually retire Projects-owned runtime;
      it is otherwise an implicit cross-cutting assumption of the whole slice.)

---

## Section E — Reviewer sign-off

| Item | Result |
|---|---|
| Section A cross-cutting gate | ☐ all pass / ☐ findings: ____ |
| Section B gate/dependency alignment | ☐ pass / ☐ findings: ____ |
| Section C per-story (6.x) | ☐ conforms / ☐ notes: ____ |
| Section C per-story (7.x) | ☐ conforms / ☐ notes: ____ |
| Section C per-story (8.x) | ☐ conforms / ☐ notes: ____ |
| Section D AD coverage substantive | ☐ pass / ☐ findings: ____ |
| Implicit sibling/platform authority introduced? | ☐ none / ☐ found: ____ |
| Event-history rewrite / unsafe dual write introduced? | ☐ none / ☐ found: ____ |
| False target-dependency claims? | ☐ none / ☐ found: ____ |

**Overall conformance verdict:** ☐ conforms ☐ conforms-with-notes ☐ non-conformant (blocking)

**Solution Architect:** ______________________  **Date:** ____________

> Conformance sign-off is a prerequisite input to the independent implementation-readiness rerun; it
> does **not** by itself lift containment or authorize implementation.
