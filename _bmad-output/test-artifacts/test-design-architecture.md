---
workflowStatus: 'completed'
totalSteps: 5
stepsCompleted: ['step-01-detect-mode', 'step-02-load-context', 'step-03-risk-and-testability', 'step-04-coverage-plan', 'step-05-generate-output']
lastStep: 'step-05-generate-output'
nextStep: ''
lastSaved: '2026-05-25'
workflowType: 'testarch-test-design'
inputDocuments:
  - _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/epics.md
  - _bmad-output/planning-artifacts/ux-design-specification.md
  - _bmad-output/project-context.md
---

# Test Design for Architecture: Hexalith.Projects

**Purpose:** Architectural concerns, testability gaps, and NFR requirements for the Architecture/Dev teams. The contract between QA and Engineering on what must be addressed before test development begins.

**Date:** 2026-05-25
**Author:** Murat (Master Test Architect)
**Status:** Architecture Review Pending
**Project:** Hexalith.Projects
**PRD Reference:** `planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md`
**ADR Reference:** `planning-artifacts/architecture.md`

---

## Executive Summary

**Scope:** Greenfield, tenant-aware AI-workspace boundary module on Hexalith.EventStore + Dapr + Aspire. A metadata control plane that references (never owns) Conversations/Folders/Files/Memories and exposes one model through three generated surfaces (Web/MCP/CLI). Covers all 5 epics (FR-1–22, NFR-1–9, AR-1–23, FS-1–8).

**Business Context (from PRD):**
- **Problem:** Chatbot lacks durable, tenant-scoped project context; conversations cannot be reliably anchored or resolved to a workspace.
- **Impact:** Success metrics SM-1 (context availability), SM-2 (resolution usefulness), SM-3 (context isolation), SM-4 (interactive latency).

**Architecture (from ADR):**
- Single `ProjectAggregate` write authority; `ProjectContext` is an assembled read result, not a persisted aggregate.
- Canonical identity `{tenant}:projects:{projectId}`; persist-then-publish; pure `Handle`/`Apply`; rejections are events.
- Four Anti-Corruption Layers (Conversations Pattern A, Folders, Memories, Tenants); reference-by-ID only, fail-closed.
- OpenAPI 3.1 Contract Spine → generated client; FrontComposer one-contract → three surfaces; Dapr-only infrastructure.

**Expected Scale:** Internal interactive metadata operations; performance target p95 < 500 ms (internal target, not an external SLA). No stated concurrency/availability targets (see Unknown thresholds).

**Risk Summary:**
- **Total risks:** 13 — **9 high-priority (score ≥ 6)**, 4 medium, 0 critical (score 9).
- **Test effort:** ~43 scenario groups across Tiers 1–3 + E2E; ~175–280 engineering hours (ranges; see QA doc). Detail in companion `test-design-qa.md`.

---

## Quick Guide

### 🚨 BLOCKERS — Team Must Decide (Can't Proceed Without)

1. **AR-G1: Conversations post-creation re-parent command/event (PR-1)** — Conversations' `ProjectId` is immutable (set only at `ConversationCreated`). FR-6 (link), FR-7 (move), and FR-15 (link initiating conversation) write-side are blocked end-to-end. Needs an additive Conversations command/event, submodule-first, before Epic 2 write-side. (Owner: Architecture + Conversations)
2. **AR-G2: Conversations `ListConversationsAsync` (PR-2)** — `IConversationClient` has no list/search; Pattern A discovery (Story 2.1) and any conversation-aware view/resolution depend on it. (Owner: Conversations)
3. **FS-1: Payload-classification taxonomy & allowlist** — the safe-vs-forbidden field boundary. Until it exists in a machine-usable form, every `NoPayloadLeakage` test is guessing. First content story of Epic 1. (Owner: Domain + Architecture)

**What we need from the team:** resolve these 3 pre-implementation items or test development for the affected areas is blocked.

### ⚠️ HIGH PRIORITY — Team Should Validate (Recommendation → You Approve)

1. **R6 ACL↔sibling contract drift** — recommend consumer-driven contract tests (Pact) or pinned integration vs real sibling versions; treat each sibling contract as a versioned dependency. (Approve: Domain + sibling owners)
2. **CL-1 Performance thresholds** — p95<500ms is defined, but error-rate ceiling, target concurrency/load, and tenant/project cardinality are not. (Define: PM + Architecture)
3. **AR-G3 Folders `CreateFolder` reachability** — FR-1 auto-folder depends on a Folders op not yet exposed as external REST. Confirm availability before the create-project-with-folder story; ship `CreateProject` without auto-folder first (PR-3). (Confirm: Folders)
4. **AR-G4 / CL-4 Memories link granularity** — `Case` vs individual `MemoryUnit`; resolve via decision spike before any Memories write-side. (Decide: Domain + PM)
5. **R9 Dapr access control** — confirm dev access-control policy never ships to prod; deny-by-default on internal endpoints (`/process`, projection dispatch, subscriptions). (Approve: Platform/Ops)

### 📋 INFO ONLY — Solutions Provided (Review, No Decisions)

1. **Test strategy:** test-pyramid-heavy — the pure event-sourced core (aggregate/projections/resolution/context-assembly/ACL-translators) is Tier-1; thin Tier-3/E2E. (Rationale: invariants live in pure logic.)
2. **Tooling:** xUnit v3 + Shouldly + NSubstitute, bUnit, Verify.XunitV3, Testcontainers, Playwright (Node ≥24) + axe-core, k6, BenchmarkDotNet, Pact.
3. **Tiered CI/CD:** PR (<15 min: Tier-1/Contracts/Tier-2/bUnit + CI gates) · Nightly (Tier-3/E2E/CDC) · Weekly (perf/soak/parity).
4. **Coverage:** ~43 scenario groups prioritized P0–P3 (see QA doc). Detail and quality gates live in the QA doc.

---

## For Architects and Devs — Open Topics

### Risk Assessment

**Total risks identified:** 13 (9 high-priority score ≥6, 4 medium, 0 low/critical).

#### High-Priority Risks (Score ≥6) — IMMEDIATE ATTENTION

| Risk ID | Category | Description | P | I | Score | Mitigation | Owner | Timeline |
| ------- | -------- | ----------- | - | - | ----- | ---------- | ----- | -------- |
| **R1** | **SEC/DATA** | Cross-tenant leakage in ProjectContext/projections/queries (violates SM-3) | 2 | 3 | **6** | FS-8 cross-tenant suite; FS-3 identity helper; query-side filtering negatives at every layer | Domain + QA | Epic 1; re-run per surface |
| **R2** | **SEC/DATA** | Payload leakage (transcripts/files/memory/prompts/secrets/paths) into events/logs/audit/DTO/surfaces | 2 | 3 | **6** | FS-1 taxonomy + FS-2 NoPayloadLeakage CI harness; extend per epic (esp. Epic 5 rendering) | Domain + QA | Epic 1 → all |
| **R3** | **TECH/DATA** | Schema-evolution break (non-additive / `V2`) breaks historical event deserialization | 2 | 3 | **6** | FS-5 frozen golden corpus round-trip; event-catalog gate per new event | Domain | Epic 1 → all |
| **R4** | **TECH/OPS** | Idempotency failure under at-least-once delivery (dup command / dup projection delivery) | 3 | 2 | **6** | FS-6 — separate dedup vs apply-idempotency stories; dup-delivery Tier-1 + Tier-3 | Domain + Platform | Epic 1 → per epic |
| **R5** | **TECH/BUS** | AR-G1 (Conversations immutable `ProjectId`) blocks FR-6/7/15 write-side | 3 | 2 | **6** | PR-1 upstream re-parent (ADR + submodule-first PR) before scheduling write-side; CDC test for ACL | Arch + Conversations | Before Epic 2 write-side |
| **R6** | **TECH** | ACL↔sibling contract drift (Conversations/Folders/Memories ahead-of-server) | 3 | 2 | **6** | Consumer-driven contract tests (Pact) or pinned integration vs real versions | Domain + sibling owners | Epic 2 |
| **R7** | **DATA/OPS** | Projection rebuild/replay non-determinism (rebuilt ≠ incremental state) | 2 | 3 | **6** | FS-6 deterministic rebuild tests; replay/dead-letter runbook tests Tier-3 | Domain + Platform | Epic 1 → per epic |
| **R8** | **OPS/TECH** | Eventual-consistency + Blazor Auto/SignalR E2E flakiness erodes signal | 3 | 2 | **6** | (QA-owned — see QA doc) prove idempotency Tier-1 first; deterministic readiness probes; quarantine lane | QA | All tiers |
| **R9** | **SEC/OPS** | Dapr dev access-control policy → prod / internal endpoints exposed → authz bypass | 2 | 3 | **6** | Deny-by-default; separate dev/prod policies; deployment-config test; mTLS where deployed | Platform/Ops | Tier-3 + deploy |

#### Medium-Priority Risks (Score 3–5)

| Risk ID | Category | Description | P | I | Score | Mitigation | Owner |
| ------- | -------- | ----------- | - | - | ----- | ---------- | ----- |
| R10 | BUS | Resolution incorrectness / silent auto-attach on ambiguity (violates NFR-9/SM-C1) | 2 | 2 | 4 | Define scoring/confidence bands first (CL-2); never-silent-attach assertion; archived excluded | Domain + PM |
| R11 | PERF | p95<500ms missed (Pattern A cross-context read + resolution ACL fan-out) | 2 | 2 | 4 | Precomputed projections; paging/short-TTL cache; Pattern B as documented escalation | Domain + Platform |
| R12 | OPS/TECH | FrontComposer generated-code drift / RC churn (Fluent UI v5 RC, Roslyn) | 2 | 2 | 4 | FS-7 regeneration/staleness gate added Epic 1; `frontcomposer inspect --fail-on-warning`; Verify.XunitV3 | Platform |
| R13 | SEC | Safe-denial inconsistency (distinguishable 403/404 / detail leak) → existence inference | 2 | 2 | 4 | Central denial mapper tested once; 404-for-both negatives on every identifier endpoint | Server |

_No low-priority (score 1–2) risks identified at system level._

#### Risk Category Legend
**TECH** technical/architecture · **SEC** security · **PERF** performance · **DATA** data integrity · **BUS** business impact · **OPS** operations.

---

### NFR Testability Requirements

What architecture must provide so NFR validation can be automated later (planning guidance, not final evidence).

| NFR Category | Threshold / Requirement | Current Design Support | Gap / Decision Needed | Planned Evidence |
| ------------ | ----------------------- | ---------------------- | --------------------- | ---------------- |
| Security (NFR-1) | Tenant isolation "impossible by construction"; safe-denial 404 | Supported (canonical identity + layered authz) | None — verify via tests | Cross-tenant suite; identity-conformance; E2E real Keycloak |
| Privacy (NFR-2) | Zero forbidden fields in any output | Partial — needs **FS-1 allowlist** to be machine-checkable | FS-1 (BLOCKER) | NoPayloadLeakage CI gate |
| Reliability (NFR-3) | Enumerated trust states deny inclusion | Supported (fail-closed design) | ACL fakes must inject every trust state (TC-1) | Trust-state negative paths |
| Idempotency (NFR-7) | Dedupe by message id; Idempotency-Key on mutations | Supported (mechanism defined) | Dedup retention window (CL-5) | Dup-command + dup-delivery results |
| Compatibility (NFR-6) | Additive/tolerant; no `V2` | Supported | None | Golden-corpus round-trip |
| Performance (NFR-5) | p95 < 500 ms (list/open/resolve/context) | Partial | **Error-rate, load, cardinality unknown (CL-1)** | k6 + BenchmarkDotNet |
| Observability (NFR-4) | Structured reason-code/correlation/freshness; OTel | Supported | Specific RED metrics / sampling unknown | Structured-log contract test; OTel assertions |
| Parity (NFR-8) | CLI = MCP = Web identical operational facts | Supported (shared enums + generation) | Parity oracle must be built (TC-8) | Parity golden diff |
| Accessibility (UX-DR27) | WCAG 2.2 AA | Supported (Fluent UI + design rules) | None | axe-core + Playwright |

**Unknown thresholds (→ clarification items, not guessed):** CL-1 perf error-rate/load/cardinality · CL-2 resolution scoring/confidence bands · CL-3 availability SLA + RTO/RPO (confirm if in v1 scope) · CL-4 Memories Case-vs-Unit · CL-5 dedup retention + replay SLO.

**Assessment boundary:** final PASS/CONCERNS/FAIL belongs in `nfr-assess` after implementation evidence exists.

---

### Testability Concerns and Architectural Gaps

**🚨 ACTIONABLE CONCERNS — Architecture Team Must Address**

#### 1. Blockers to Fast Feedback (what we need from architecture)

| Concern | Impact on testing | What architecture must provide | Owner | Timeline |
| ------- | ----------------- | ------------------------------ | ----- | -------- |
| **FS-1 payload allowlist absent** | Leakage tests (R2) have no boundary to assert | Machine-usable safe-vs-forbidden field taxonomy | Domain + Arch | Epic 1, first content story |
| **ACL fakes are happy-path only** | Fail-closed (NFR-3) unprovable | Fault-injection fakes in `Projects.Testing` emitting every trust/denial state (TC-1) | Domain | Epic 1–2 |
| **Sibling APIs immature/ahead-of-server** | ACL tests green but diverge from reality (R6) | Versioned sibling contracts + `ListConversationsAsync` (AR-G2), CreateFolder reachability (AR-G3) | Conversations/Folders | Before Epic 2 |
| **No deterministic projection-readiness probe** | Command-async + nudge→re-query forces sleeps (R8) | Synchronous projection-pump or watermark/ETag convergence helper for tests (TC-3) | Platform | Epic 1 |

#### 2. Architectural Improvements Needed

1. **Resolution trace must be observable at query time** — traces are compute-on-demand, not persisted. Without structured trace metadata (candidates, reason codes, included/excluded evidence) the Trace Workbench (UX-DR9) and resolution assertions have nothing to read. (Owner: Domain · Epic 4)
2. **Resolution scoring heuristics undefined** — only binary outcomes (`NoMatch`/`SingleCandidate`/`MultipleCandidates`) are assertable until scoring/confidence-band cutoffs are specified (CL-2). (Owner: Domain + PM · before Epic 4 precise tests)
3. **Cross-surface parity needs an oracle** — NFR-8 is structurally enforced by shared enums + generation, but requires a golden test that diffs CLI/MCP/Web fields, reason codes, and audit IDs for the same query (TC-8). (Owner: Platform · Epic 5)

### Testability Assessment Summary

**CURRENT STATE — FYI**

#### What Works Well
- Pure Tier-1 core by design (aggregate/projection/resolution/context-assembly/ACL-translators) — fast, deterministic, parallel-safe seeding via event replay.
- State seeding via events + reuse of EventStore/Tenants `Testing` fakes/builders → high controllability without infrastructure.
- Shared `[ProjectionBadge]` vocabulary → deterministic assertions on stable codes, not free text.
- Single-sourced identity derivation (`{tenant}:projects:{projectId}`) → one conformance test class covers the whole derivation surface.
- Invariants are CI-enforceable (NoPayloadLeakage, schema golden corpus, regeneration gate, OpenAPI fingerprint).

#### Accepted Trade-offs (No Action Required for v1)
- **Pattern A request-time conversation read** instead of a local projection — acceptable for v1; Pattern B is the documented escalation if profiling shows the read is too slow (R11).
- **Resolution-trace history not persisted** — only the confirmed choice is stored; acceptable until the trace workbench needs replayable depth.

---

### Risk Mitigation Plans (High-Priority, Production-Code-Owned)

_QA-owned mitigations (R8) live in `test-design-qa.md`._

#### R1: Cross-tenant leakage (Score 6) — CRITICAL PATH
**Strategy:** 1) Implement FS-3 identity-derivation helper; all keys/topics/groups/scopes derive only from canonical identity. 2) Enforce query-side result filtering in every query handler (not just API/JWT). 3) Stand up FS-8 cross-tenant negative suite as an owned, demoable acceptance test, re-run per new surface.
**Owner:** Domain + QA · **Timeline:** Epic 1 · **Status:** Planned · **Verification:** FS-8 suite green; identity-conformance Tier-1 green at every layer.

#### R2: Payload leakage (Score 6) — CRITICAL PATH
**Strategy:** 1) Author FS-1 taxonomy (BLOCKER). 2) Build FS-2 serialization-boundary/Verify CI harness forcing every new event/DTO/log/audit/surface through a leakage assertion. 3) Extend the harness each epic, especially Epic 5 rendering.
**Owner:** Domain (taxonomy/harness) · **Timeline:** Epic 1 → all · **Status:** Planned · **Verification:** CI gate fails on any forbidden field.

#### R3: Schema-evolution break (Score 6)
**Strategy:** 1) Freeze a serialized golden sample of every event into FS-5 corpus. 2) Round-trip the corpus in CI. 3) Require an event-catalog entry (fields, sensitivity class, consumers) before merging any new event; reject `V2` types.
**Owner:** Domain · **Timeline:** Epic 1 → all · **Status:** Planned · **Verification:** corpus round-trip green for every historical event.

#### R4: Idempotency under at-least-once (Score 6)
**Strategy:** 1) Command dedup (same Idempotency-Key → one event; different payload → conflict). 2) Projection-apply idempotency under duplicate delivery — a *separate* story (distinct failure mode). 3) Validate both at Tier-1, then under real at-least-once at Tier-3.
**Owner:** Domain + Platform · **Timeline:** Epic 1 → per epic · **Status:** Planned · **Verification:** dup-delivery tests green at Tier-1 and Tier-3.

#### R5: AR-G1 upstream dependency (Score 6)
**Strategy:** 1) Record ADR for Conversations post-creation re-parent. 2) Land PR-1 (additive command/event) submodule-first, no `V2`. 3) Add a consumer-driven contract test for the ACL before consuming. Gate FR-6/7/15 write-side stories on PR-1.
**Owner:** Architecture + Conversations · **Timeline:** before Epic 2 write-side · **Status:** Planned · **Verification:** Conversations exposes re-parent; CDC test passes.

#### R6: ACL↔sibling contract drift (Score 6)
**Strategy:** 1) Adopt consumer-driven contract tests (Pact) for each ACL, or pin integration tests to real sibling versions. 2) Treat sibling contracts as versioned dependencies with compatibility checks. 3) Land AR-G2 (`ListConversationsAsync`) and confirm AR-G3 reachability first.
**Owner:** Domain + sibling owners · **Timeline:** Epic 2 · **Status:** Planned · **Verification:** provider verification green against pinned versions.

#### R7: Projection rebuild non-determinism (Score 6)
**Strategy:** 1) Prove same-events → same-state on the trivial Epic-1 set (FS-6), extend per epic. 2) Tier-3 replay/dead-letter runbook tests. 3) Durable projection + dedup store in production (not in-memory).
**Owner:** Domain + Platform · **Timeline:** Epic 1 → per epic · **Status:** Planned · **Verification:** rebuilt == incremental state; replay restores read models.

#### R9: Dapr access-control / authz bypass (Score 6)
**Strategy:** 1) Deny-by-default access-control allowlists for internal endpoints. 2) Separate dev and prod policies; never ship dev policy to prod. 3) mTLS where deployed; deployment-config test asserting prod policy.
**Owner:** Platform/Ops · **Timeline:** Tier-3 + deploy · **Status:** Planned · **Verification:** deployment test confirms deny-by-default; internal endpoints unreachable externally.

---

### Assumptions and Dependencies

#### Assumptions
1. EventStore/Tenants `Testing` fakes/builders are reusable for Projects seeding and tenant-event simulation.
2. p95 < 500 ms is an internal target, not an external SLA; no availability/RTO/RPO commitment in v1 (pending CL-3).
3. FrontComposer generates Web/MCP/CLI from annotated contracts; surface parity is a build output, not hand-written.
4. Pinned RC dependencies (Fluent UI v5 RC, Roslyn 4.12, Dapr, Aspire, xUnit gen) stay pinned; deliberate bumps trigger snapshot re-baselining.

#### Dependencies
1. **PR-1** Conversations re-parent command/event — before Epic 2 write-side.
2. **PR-2** Conversations `ListConversationsAsync` — before Story 2.1 ACL.
3. **AR-G3** Folders `CreateFolder` external reachability — before create-with-folder story.
4. **CL-1/CL-2** perf thresholds and resolution heuristics — before the PERF gate and precise resolution tests respectively.

#### Risks to the Plan
- **Risk:** sibling contracts shift after ACLs are built against fakes. **Impact:** green unit tests, broken integration. **Contingency:** consumer-driven contract tests + version pinning (R6).
- **Risk:** RC dependency churn breaks generation/snapshots. **Impact:** Epic 5 surface tests red. **Contingency:** FS-7 regeneration gate as early warning; deliberate, cross-module-aware bumps only.

---

**End of Architecture Document**

**Next Steps for Architecture Team:** (1) resolve the 3 BLOCKERS; (2) assign owners/timelines for the 9 high-priority risks; (3) validate the 5 HIGH PRIORITY items; (4) confirm Unknown thresholds (CL-1–5).
**Next Steps for QA Team:** (1) wait on FS-1 + AR-G2 before leakage/discovery test dev; (2) see `test-design-qa.md` for the coverage recipe; (3) begin test-infrastructure setup (fakes, fault-injection ACLs, factories).
