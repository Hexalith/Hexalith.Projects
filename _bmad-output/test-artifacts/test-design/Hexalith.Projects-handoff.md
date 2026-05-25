---
title: 'TEA Test Design → BMAD Handoff Document'
version: '1.0'
workflowType: 'testarch-test-design-handoff'
inputDocuments:
  - _bmad-output/test-artifacts/test-design-architecture.md
  - _bmad-output/test-artifacts/test-design-qa.md
sourceWorkflow: 'testarch-test-design'
generatedBy: 'TEA Master Test Architect'
generatedAt: '2026-05-25'
projectName: 'Hexalith.Projects'
---

# TEA → BMAD Integration Handoff

## Purpose

Bridges TEA's system-level test design with BMAD's epic/story decomposition (`create-epics-and-stories`). Quality requirements, risk assessments, and test strategy flow into implementation planning so the load-bearing invariants become acceptance criteria, not afterthoughts.

## TEA Artifacts Inventory

| Artifact | Path | BMAD Integration Point |
| -------- | ---- | ---------------------- |
| Test Design — Architecture | `_bmad-output/test-artifacts/test-design-architecture.md` | Epic quality requirements, testability blockers, NFR gaps |
| Test Design — QA | `_bmad-output/test-artifacts/test-design-qa.md` | Story acceptance criteria, coverage plan, execution strategy |
| Risk Assessment | (embedded in both) | Epic risk classification, story priority |
| Coverage Strategy (~43 scenario groups, P0–P3) | (embedded in QA doc) | Story test requirements |

## Epic-Level Integration Guidance

### Risk References (epic-level quality gates)

- **Epic 1 — Foundation:** R1 (cross-tenant), R2 (payload leakage), R3 (schema evolution), R4 (idempotency), R7 (rebuild determinism), R13 (safe-denial), R12 (regeneration gate). These are the FS-1–8 slices; Epic 1 is **not** done until each has a green, demoable gate.
- **Epic 2 — Context References:** R5 (AR-G1 conversation re-parent — PR-1 prerequisite), R6 (ACL↔sibling drift). Gate write-side stories on PR-1; require CDC tests before consuming siblings.
- **Epic 3 — Context Assembly:** R11 (Pattern A perf watch-point), plus R1/R2 re-run against the assembled `ProjectContext` surface.
- **Epic 4 — Resolution:** R10 (never-silent-attach). Define CL-2 scoring/confidence bands before precise tests.
- **Epic 5 — Operational Console:** R12 (generated-code drift / RC churn), parity oracle (NFR-8), WCAG 2.2 AA; re-run R1/R2 leakage against rendering surfaces.

### Quality Gates (per epic)

| Epic | Gate (must be green before "done") |
| ---- | ---------------------------------- |
| 1 | NoPayloadLeakage harness, schema golden-corpus, cross-tenant suite, identity-conformance, dual idempotency, rebuild determinism, safe-denial 404-for-both, FS-7 regeneration gate wired |
| 2 | PR-1 landed before conversation write-side; ACL CDC green; fault-injection fail-closed per trust state |
| 3 | Allowlist assembly exclusion-reason coverage; leakage re-run on ProjectContext; Pattern A read measured |
| 4 | Never-silent-attach + archived-excluded assertions; CL-2 heuristics defined for precise tests |
| 5 | Cross-surface parity oracle; Verify snapshots stable; axe-core WCAG 2.2 AA; leakage on rendered surfaces |

## Story-Level Integration Guidance

### P0/P1 Test Scenarios → Story Acceptance Criteria

These MUST appear as acceptance criteria on their stories:

- **Story 1.2 (vocabulary/identifiers/taxonomy):** FS-1 payload allowlist published (P0-003 precondition); FS-3 identity derivation conformance (P0-001).
- **Story 1.3 (OpenAPI spine + client):** command-async 202 + Idempotency-Key required-on-mutation/rejected-on-query (P0-011); safe-denial 404-for-both (P0-012).
- **Story 1.4 (CreateProject tracer):** Handle→ProjectCreated/Active + rejection-as-event (P0-008); setup validation rejects secrets/paths/foreign (P0-009); NoPayloadLeakage + schema corpus on the new events (P0-003/P0-004).
- **Story 1.5 (rebuild & idempotency):** rebuild determinism (P0-005); command dedup (P0-006); projection-apply idempotency under dup delivery (P0-007).
- **Epic 2 ACL stories:** translators map denials to safe problems + trust state (P0-013); fault-injection fail-closed per trust state (P0-014); ACL CDC (P1-005).
- **Epic 3 assembly stories:** allowlist inclusion with exclusion reason codes (P0-015); explain/refresh surface stale/unavailable (P1-008/P1-009).
- **Epic 4 resolution stories:** never-silent-attach + outcome reason codes (within P0-015 / P1-010..013).
- **Epic 5 surface stories:** parity oracle (P1-016); generated snapshots (P1-017); E2E journeys (P1-018); WCAG 2.2 AA (P1-019).

### Data-TestId Requirements (UX-DR28)

Generated FrontComposer views must expose deterministic, role/label-based `data-testid` selectors for automation: project inventory grid rows, detail inspector field groups, reference-health matrix rows, resolution-trace candidate cards, audit-timeline entries, maintenance action-panel controls (dry-run/confirm), and safe-diagnostic-export copy/download. Preserve stable component keys across regeneration.

## Risk-to-Story Mapping

| Risk ID | Category | P×I | Recommended Story/Epic | Test Level |
| ------- | -------- | --- | ---------------------- | ---------- |
| R1 | SEC/DATA | 6 | Epic 1 (Story 1.2 FS-3, FS-8 suite); re-run Epics 3/5 | T1 + T2 + E2E |
| R2 | SEC/DATA | 6 | Epic 1 (Story 1.2 FS-1, Story 1.4 FS-2); all epics | CT + T1, extend T2/E2E |
| R3 | TECH/DATA | 6 | Epic 1 (Story 1.4 golden corpus) | CT |
| R4 | TECH/OPS | 6 | Epic 1 (Story 1.5 dedup + apply-idempotency) | T1 + T3 |
| R5 | TECH/BUS | 6 | Epic 2 (PR-1 prerequisite; FR-6/7 write-side) | T1 + CDC |
| R6 | TECH | 6 | Epic 2 (ACL stories) | CDC + T3 |
| R7 | DATA/OPS | 6 | Epic 1 (Story 1.5 rebuild); per epic | T1 + T3 |
| R8 | OPS/TECH | 6 | Cross-cutting (all tiers; QA-owned) | T1→T3/E2E |
| R9 | SEC/OPS | 6 | Epic 1 (1c topology) + deploy | T3 + deploy |
| R10 | BUS | 4 | Epic 4 (resolution stories; CL-2 first) | T1 |
| R11 | PERF | 4 | Epic 3/4 (measure); Epic 5 (gate) | PERF |
| R12 | OPS/TECH | 4 | Epic 1 (FS-7 gate) → Epic 5 (generators) | CI + CMP |
| R13 | SEC | 4 | Epic 1 (Story 1.3 denial mapper) | T2 |

## Recommended BMAD → TEA Workflow Sequence

1. **TEA Test Design** (`TD`) → produces this handoff document ✅ (done)
2. **BMAD Create Epics & Stories** → consume this handoff; embed quality gates and the P0/P1 acceptance criteria above
3. **TEA ATDD** (`AT`) → generate failing acceptance tests per story (start with Story 1.4 P0 scenarios)
4. **BMAD Implementation** → developers implement test-first
5. **TEA Automate** (`TA`) → expand the full suite per the coverage plan
6. **TEA Trace** (`TR`) → validate coverage completeness and make the gate decision

## Phase Transition Quality Gates

| From Phase | To Phase | Gate Criteria |
| ---------- | -------- | ------------- |
| Test Design | Epic/Story Creation | All 9 high-priority (≥6) risks have a mitigation strategy and owner; 3 BLOCKERS (AR-G1, AR-G2, FS-1) scheduled |
| Epic/Story Creation | ATDD | Stories carry the P0/P1 acceptance criteria from this handoff; CL-1/CL-2 resolved for the stories they block |
| ATDD | Implementation | Failing acceptance tests exist for all P0 scenarios (and P1 where unblocked) |
| Implementation | Test Automation | All acceptance tests pass; CI gates (leakage, fingerprint, regeneration, golden-corpus) green |
| Test Automation | Release | Trace matrix shows ≥80% coverage of P0/P1 requirements; P0 pass rate 100%, P1 ≥95%; zero un-quarantined flakes |
