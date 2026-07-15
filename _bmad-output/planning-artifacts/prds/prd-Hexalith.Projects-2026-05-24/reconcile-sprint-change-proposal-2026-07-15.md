# Final Reconciliation: Planning Rebaseline After Implementation Readiness Assessment

## Input

- Approved source: `sprint-change-proposal-2026-07-15.md` (2026-07-15), amending the July 14 readiness correction.
- Compared with: revised `prd.md` and `addendum.md` (2026-07-15).
- Verdict: **Substantially reconciled — the product contract is faithfully rebaselined, with three downstream addendum gaps and two explicit supersessions to record.**

## Faithfully captured

- FR-1/FR-19 now make creation a durable pre-activation task and prevent any caller-visible Active Project before exactly one authorized Folder is bound and read-model-confirmed.
- FR-23 defines restore with current authority/Folder evidence, server Preview and confirmation, idempotent durable execution, recovery, and metadata-only audit.
- FR-14/FR-15, the glossary, journeys, and addendum §§1–2 preserve server-issued bound confirmation, expiry/single-use/replay rules, task truth, lost-response recovery, and the separation of task status from the `Active`/`Archived` lifecycle.
- FR-12–FR-18 preserve current-only resolution, transient traces, read-only Refresh, no candidate-history store, and persistence of confirmed outcomes only.
- FR-22, FR-24, and the role matrix now make operator authority action-specific and give Safe Diagnostic Export separate authorization, bounded output, deterministic ordering/truncation, metadata-only content, and no retention; addendum §3 retains the versioned contract depth.
- NFR-1–NFR-11 supply measurable security, availability, recovery, durability, performance, cardinality, pagination, rate/back-pressure, retention, accessibility, compatibility, and release-evidence gates. Failed critical cases and unexplained skips remain release-blocking.
- Addendum §§5–7 preserve staged migration, no event-history rewrite or unsafe dual write, compatibility adapters/replay/cutover/rollback, Chatbot-owned presentation, deterministic verification, and the distinction between runnable evidence and release acceptance.

## Gaps

1. **The target ownership matrix is not preserved.** The addendum should state that Projects owns domain policy and project-specific task transitions; EventStore DomainService/platform owns persistence, publication, subscriptions, read-model stores, cursors, health, telemetry, and generic durable workflow capability; platform hosts own topology and Web/MCP/CLI runtime composition. The current generic routing is insufficient and addendum §4.2 incorrectly assigns Projects' presentation transition to the FrontComposer repository.
2. **Planning containment and backlog rebaseline are under-captured.** The revised documents do not retain that Epics 1–5 remain immutable history rather than release authorization, all 23 Epic 6–8 placeholders are findings inventory rather than schedulable stories, corrective development and production release remain frozen, replacement stories must be outcome-based, and implementation readiness must return `READY` before sprint planning or story creation.
3. **Evidence ownership is not implementation-ready.** The PRD defines strong NFR gates, but the addendum does not preserve the source's requirement to map every NFR and P1/P2 finding to an owning story, deterministic fixture/environment, verification command, pass/fail artifact, and terminal Jerome/John release disposition. The readiness trigger evidence—29 issues, 23 placeholders, 19/56 live results, and blocked release handoff—is also absent as the rationale for that gate.

## Contradictions and supersessions

- **Safe Diagnostic Export ownership is intentionally superseded.** The source placed export inside FR-22; the revised PRD keeps FR-22 as operator read access and creates FR-24 for separately authorized export. The capability is preserved and the split is clearer, but this later stable-ID decision should be recorded as superseding the proposal's FR-22 wording.
- **FR count is intentionally superseded.** The source began from 22 FRs and added restore; the revised PRD has 24 because restore is FR-23 and export is independently specified as FR-24.
- Addendum §4.2's claim that the movable Projects presentation inventory/transition is owned by the FrontComposer repository conflicts with the source's consumer-side Projects/platform ownership model and should be corrected.
- No other direct product contradiction remains.

## Qualitative intent at risk

- This is planning-first containment, not authorization to implement the old placeholders.
- Preserve useful Epics 1–5 implementation evidence without treating history as current prescription or release proof.
- Migrate valid domain behavior by value slice; do not rewrite events, introduce unsafe dual writes, or perform a big-bang runtime replacement.
- Keep reusable platform mechanics out of the Projects domain module while retaining Projects-specific policy ownership.
- Acknowledgement and SignalR are never completion; clients follow server Preview, task, and read-model truth.
- Chatbot owns accessible end-user interaction; generated Web/CLI/MCP surfaces never expand authority by implication.
- No failed/skipped critical evidence, autonomous consequential operation, sibling-repository change, or production release bypasses its readiness and repository-local approval gate.

## Disposition

- Leave `prd.md` unchanged; it faithfully captures the approved product rebaseline and its deliberate FR-24 refinement.
- Extend the addendum with the target ownership matrix, Epic 6–8/story containment and readiness sequence, and story-level NFR/finding evidence ownership; correct the FrontComposer ownership sentence.
- Record the FR-22→FR-24 and 22→24 FR supersessions explicitly for traceability.
- Route exact architecture tree, story inventory, BMad execution sequence, fixtures, commands, and sprint-status replacement to architecture, epics/stories, test strategy, readiness, and sprint-planning artifacts.
- Final disposition: **revise addendum, then accept; keep implementation and release gated until readiness is `READY`.**
