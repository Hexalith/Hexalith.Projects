---
workflowStatus: 'completed'
totalSteps: 5
stepsCompleted: ['step-01-detect-mode', 'step-02-load-context', 'step-03-risk-and-testability', 'step-04-coverage-plan', 'step-05-generate-output']
lastStep: 'step-05-generate-output'
nextStep: ''
lastSaved: '2026-05-25'
mode: 'system-level'
detectedStack: 'fullstack'
inputDocuments:
  - _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/epics.md
  - _bmad-output/planning-artifacts/ux-design-specification.md
  - _bmad-output/project-context.md
  - knowledge/risk-governance.md
  - knowledge/test-levels-framework.md
  - knowledge/nfr-criteria.md
  - knowledge/test-quality.md
  - knowledge/adr-quality-readiness-checklist.md
---

# Test Design — Progress

## Step 1: Detect Mode & Prerequisites

**Mode:** System-Level (confirmed by user — whole-module test strategy across all 5 epics).

**Rationale:** Both a PRD/ADR and an epic breakdown exist (workflow prefers System-Level first); the `Hexalith.Projects` module is greenfield (Story 1.1 scaffolds it), so establishing the test-levels framework, risk model, NFR validation approach, and CI quality gates once — before any epic-level plan — is the highest-value step.

**Prerequisites (System-Level) — all satisfied:**
- PRD: `_bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md` (FR-1–22, NFR-1–9)
- ADR / Architecture: `_bmad-output/planning-artifacts/architecture.md` (AR-1–23, gaps AR-G1–G4)
- Epic breakdown: `_bmad-output/planning-artifacts/epics.md` (Epics 1–5, FS-1–8, PR-1–4)
- UX spec: `_bmad-output/planning-artifacts/ux-design-specification.md` (UX-DR1–28)
- Research reports + in-flight story `2-1-conversation-reference-read-acl.md`

**Config:** risk_threshold=p1; test_design_output=`_bmad-output/test-artifacts/test-design`; xUnit v3 + Shouldly + NSubstitute; Playwright (Node ≥24) + axe-core for E2E.

## Step 2: Load Context & Knowledge Base

**Config flags resolved:** tea_use_playwright_utils=true; tea_use_pactjs_utils=false; tea_pact_mcp=none; tea_browser_automation=auto; test_stack_type=auto.

**Detected stack:** `fullstack` — .NET 10 backend (EventStore + Dapr + Aspire, `*.csproj`/`.slnx`) + generated FrontComposer Blazor Web with planned Playwright E2E (Node ≥24) + axe-core. (Module is greenfield — no test files exist yet to scan, so stack inferred from architecture, not file scan.)

**Playwright Utils profile:** Full UI+API profile applies (fullstack + planned browser E2E). Heavy utils fragments deferred to framework/automation steps to preserve context — not needed for system-level strategy. Note: this is a .NET/xUnit + bUnit + Playwright stack; the Playwright Utils (TS) fragments inform the E2E lane only, not the dominant Tier-1/2/3 .NET tiers.

**Project artifacts loaded:** PRD (FR-1–22, NFR-1–9, SM-1–4 + SM-C1/C2), Architecture Decision Document (AR-1–23, AR-G1–G4, implementation sequence, gap analysis), epics (Epics 1–5 + FS-1–8 + PR-1–4), UX spec (UX-DR1–28), umbrella project-context (96 rules).

**Knowledge fragments loaded (System-Level required):** risk-governance, test-levels-framework, nfr-criteria, test-quality, adr-quality-readiness-checklist.

**Key NFR threshold extracted:** Performance p95 < 500 ms (internal target, NOT external SLA) for Project list/open/resolution/context retrieval when dependent bounded-context metadata is available (NFR-5, SM-4).

**Threshold gaps to flag (default → CONCERNS per nfr-criteria):** No explicit error-rate target, no load/concurrency target, no availability SLA, no RTO/RPO, no rate-limit threshold, resolution scoring/confidence bands undefined (deferred to resolution story). These are documented as open NFR questions in Step 3.

## Step 3: Testability & Risk Assessment

### 3.1 System-Level Testability Review

#### 🚨 Testability Concerns (actionable)

**Controllability**
- **TC-1 — Sibling ACL fakes must simulate every trust/denial state.** Fail-closed (NFR-3) is only provable if the four ACL fakes (`IProjectConversationDirectory`, `IProjectFolderDirectory`, `IProjectMemoryDirectory`, `ITenantAccess`) can emit each state: `unauthorized / unavailable / stale / rebuilding / forbidden / redacted / tenant_mismatch`. Build these into `Hexalith.Projects.Testing` as first-class fault-injection fakes, not happy-path stubs.
- **TC-2 — ACLs are being built against not-yet-real sibling APIs.** Conversations `ListConversationsAsync` (AR-G2), Folders `CreateFolder` external REST (AR-G3), and Memories Case-vs-Unit (AR-G4) are immature/ahead-of-server. Tests green against fakes can diverge from real siblings → require consumer-driven contract tests (Pact) or pinned integration tests against real sibling versions.
- **TC-3 — Command-async + no read-after-write + projection nudge→re-query.** Integration/E2E need a deterministic projection-readiness probe (poll for watermark/ETag/freshness convergence), never sleeps. Provide a synchronous projection-pump or convergence helper in the test harness.
- **TC-4 — Auto-folder-create path (FR-1/AR-G3) not fully testable until Folders `CreateFolder` is reachable.** Per PR-3, `CreateProject` ships without auto-folder; keep the auto-folder slice gated and its E2E deferred until the upstream is reachable.
- **TC-5 — Resolution scoring/confidence-band heuristics are UNDEFINED.** Precise resolution assertions (which candidate, what reason code, where the Single/Multiple boundary sits) cannot be written until the heuristics are specified in the resolution story. Today only the binary outcomes (`NoMatch` / `SingleCandidate` / `MultipleCandidates`) and "never silently attach" are assertable.

**Observability**
- **TC-6 — NoPayloadLeakage assertions need the FS-1 allowlist first.** Until the payload-classification taxonomy (safe-vs-forbidden fields) exists in a machine-usable form, every leakage test is guessing the boundary. Author FS-1 as the first content story of Epic 1.
- **TC-7 — Resolution traces are compute-on-demand, not persisted.** Troubleshooting/observing a resolution decision depends on structured trace metadata emitted at query time (reason codes, included/excluded evidence). The resolution compute must expose this deterministically for the Trace Workbench (UX-DR9) and for assertions.
- **TC-8 — Cross-surface parity (NFR-8) needs a parity oracle.** "CLI = MCP = Web expose identical operational facts" is only verifiable with a test that diffs the three surfaces' fields/reason-codes/audit-ids for the same query. Shared enums + FrontComposer generation make it structurally enforceable, but the oracle/golden must be built.

**Reliability (of the test suite)**
- **TC-9 — Tier-3 (Dapr/Aspire/Testcontainers + at-least-once pub/sub) is an inherent flake surface.** Out-of-order/duplicate delivery must be proven idempotent at Tier-1 first, then convergence asserted deterministically at Tier-3. Flakiness here is critical tech debt — quarantine lane required, never weaken assertions.
- **TC-10 — Blazor Auto + SignalR nudge→re-query is a classic E2E flake source.** Server→WASM render-mode transition, persisted state, and SignalR reconnection timing need network-first interception, stable `data-testid` (UX-DR28), reduced-motion, and explicit readiness — not waits.
- **TC-11 — Pinned high-risk RC deps (Fluent UI v5 RC, Roslyn 4.12, Dapr, Aspire, xUnit gen).** Any deliberate bump can break FrontComposer generation and Verify snapshots; re-baselining is expected. Keep `Verify.XunitV3` snapshots and the regeneration gate (FS-7) as the early-warning system.

#### ✅ Testability Assessment Summary (already strong)
- **Pure Tier-1 core by design** — aggregate `Handle/Apply`, projections, resolution, context-assembly, ACL translators, and generator parse/transform/emit are all pure (no Dapr/Aspire/network/browser/containers). Fast, deterministic, parallel-safe seeding via event replay.
- **State seeding via events + existing fakes/builders** — EventStore/Tenants `Testing` fakes/builders + planned `ProjectBuilder` mean controllability is high without infrastructure.
- **Deterministic assertions via shared vocabulary** — `[ProjectionBadge]` enums (stable codes, not free text) make leakage, denial, and parity assertions stable.
- **Structured metadata-only observability** — reason codes + correlation/causation + freshness + OTel are designed in; deterministic to assert.
- **Identity derivation is single-sourced** — `{tenant}:projects:{projectId}` → all keys/topics/groups/scopes; one conformance test class covers the whole derivation surface (FS-3).
- **Invariants are CI-enforceable** — FS-2 NoPayloadLeakage harness, FS-5 schema-evolution golden corpus, FS-7 regeneration gate, OpenAPI fingerprint, `frontcomposer inspect --fail-on-warning`.
- **Safe-denial is centralizable** — one `ProjectAuthorizationDenialMapper` → test once, assert 404-for-both everywhere.

#### Architecturally Significant Requirements (ASRs)

| ID | ASR | Class | Test consequence |
|----|-----|-------|------------------|
| ASR-1 | Tenant isolation by construction (`{tenant}:projects:{projectId}`) | **ACTIONABLE** | FS-8 cross-tenant negative suite; query-side filtering negatives at every layer; re-run per surface |
| ASR-2 | Metadata-only / NoPayloadLeakage on every event/log/DTO/audit/surface | **ACTIONABLE** | FS-1 allowlist + FS-2 CI harness; extend Epic 5 rendering surfaces |
| ASR-3 | Fail-closed authz + projection freshness/trust | **ACTIONABLE** | Fault-injection ACL fakes (TC-1); negative paths for each trust state |
| ASR-4 | Idempotency under at-least-once (command dedup ≠ projection-apply idempotency) | **ACTIONABLE** | Two separate test stories (FS-6); duplicate delivery Tier-1 + Tier-3 |
| ASR-5 | Additive/serialization-tolerant schema; no `V2` | **ACTIONABLE** | FS-5 frozen golden-event corpus round-trip in CI; event-catalog gate |
| ASR-6 | Persist-then-publish + pure `Handle/Apply` | **ACTIONABLE** | Tier-1 purity tests + replay/rebuild determinism |
| ASR-7 | OpenAPI spine = source of truth → generated client | **ACTIONABLE** | OpenAPI fingerprint/compat CI gate; never hand-edit `.g.cs` |
| ASR-8 | FrontComposer one-contract→three-surfaces parity | **ACTIONABLE** | FS-7 staleness gate + parity oracle (TC-8) |
| ASR-9 | Compute-on-demand resolution; persist only `ProjectResolutionConfirmed` | **ACTIONABLE (blocked)** | Needs scoring heuristics (TC-5) before precise assertions |
| ASR-10 | Pattern A (reference-don't-own conversations) + AR-G1 immutable `ProjectId` | **ACTIONABLE** | Gate FR-6/7/15 write-side on PR-1 upstream re-parent event; CDC test for ACL |
| ASR-11 | Safe-denial (404 for unauthorized = nonexistent) | **ACTIONABLE** | Negative tests on every identifier-accepting endpoint |
| ASR-12 | p95 < 500 ms via precomputed projections; Pattern A fan-out is the watch-point | **ACTIONABLE** | k6/BenchmarkDotNet once endpoints exist; measure Pattern A read |
| ASR-13 | Dapr-only infra; access-control allowlists; mTLS; dev policy never to prod | **FYI** | Tier-3 + deployment-config tests; deny-by-default assertion |
| ASR-14 | WCAG 2.2 AA accessibility | **ACTIONABLE** | axe-core + Playwright in Epic 5 |

### 3.2 Risk Assessment Matrix

Scoring: Probability (1–3) × Impact (1–3) = Score (1–9). ≥6 = HIGH (mitigation + owner required); =9 = CRITICAL gate-fail. `risk_threshold=p1`.

| ID | Cat | Risk | P | I | Score | Level | Mitigation | Owner (role) | When |
|----|-----|------|---|---|-------|-------|------------|--------------|------|
| R1 | SEC/DATA | Cross-tenant leakage in ProjectContext/projections/queries (violates SM-3) | 2 | 3 | **6** | HIGH | FS-8 cross-tenant suite; FS-3 identity helper; query-side filtering negatives every layer; adversarial tests | Domain + QA | Epic 1; re-run per surface |
| R2 | SEC/DATA | Payload leakage (transcripts/files/memory/prompts/secrets/paths) into events/logs/audit/DTO/surfaces | 2 | 3 | **6** | HIGH | FS-1 taxonomy + FS-2 NoPayloadLeakage CI harness; extend every epic (esp. Epic 5 rendering) | Domain + QA | Epic 1 → all |
| R3 | TECH/DATA | Schema-evolution break (non-additive change / `V2`) breaks historical event deserialization | 2 | 3 | **6** | HIGH | FS-5 frozen golden corpus round-trip in CI; event-catalog entry gate per new event | Domain | Epic 1 → all |
| R4 | TECH/OPS | Idempotency failure under at-least-once delivery (dup command / dup projection delivery) | 3 | 2 | **6** | HIGH | FS-6 — separate stories for command dedup vs projection-apply idempotency; dup-delivery Tier-1 + Tier-3 | Domain + Platform | Epic 1 → per epic |
| R5 | TECH/BUS | Upstream AR-G1 (Conversations immutable `ProjectId`) blocks FR-6/7/15 write-side | 3 | 2 | **6** | HIGH | PR-1 upstream re-parent command/event (ADR + submodule-first PR) before scheduling write-side; CDC test for ACL | PM/Arch + Conversations | Before Epic 2 write-side |
| R6 | TECH | ACL ↔ sibling contract drift (Conversations/Folders/Memories ahead-of-server) | 3 | 2 | **6** | HIGH | Consumer-driven contract tests (Pact) or pinned integration vs real sibling versions; treat sibling contract as versioned dep | Domain + sibling owners | Epic 2 |
| R7 | DATA/OPS | Projection rebuild/replay non-determinism (rebuilt ≠ incremental state) | 2 | 3 | **6** | HIGH | FS-6 deterministic rebuild tests (same events → same state); replay/dead-letter runbook tests Tier-3 | Domain + Platform | Epic 1 → per epic |
| R8 | OPS/TECH | Eventual-consistency + Blazor Auto/SignalR test flakiness erodes signal | 3 | 2 | **6** | HIGH | Prove idempotency/convergence at Tier-1/2 first; deterministic readiness probes; network-first; stable test IDs; quarantine lane | QA | All tiers |
| R9 | SEC/OPS | Dapr dev access-control policy shipped to prod / internal endpoints (`/process`, dispatch, subs) exposed → authz bypass | 2 | 3 | **6** | HIGH | Deny-by-default; separate dev/prod policies; deployment-config test; mTLS where deployed | Platform/Ops | Tier-3 + deploy |
| R10 | BUS | Resolution incorrectness / silent auto-attach on ambiguity (violates NFR-9/SM-C1) | 2 | 2 | 4 | MED | Define scoring/confidence bands first (TC-5); case catalog NoMatch/Single/Multiple; assert never-silent-attach; archived excluded | Domain + PM | Epic 4 |
| R11 | PERF | p95 < 500 ms missed (Pattern A cross-context read + resolution ACL fan-out) | 2 | 2 | 4 | MED | Precomputed tenant-scoped projections; paging/short-TTL cache; Pattern B as documented escalation; k6/BenchmarkDotNet | Domain + Platform | Epic 3/4; measure Epic 5 |
| R12 | OPS/TECH | FrontComposer generated-code drift / RC churn (Fluent UI v5 RC, Roslyn) breaks generation/snapshots/parity | 2 | 2 | 4 | MED | FS-7 regeneration/staleness gate added Epic 1 (before generators land); `frontcomposer inspect --fail-on-warning`; Verify.XunitV3; deliberate bumps only | Platform | Epic 1 gate → Epic 5 |
| R13 | SEC | Safe-denial inconsistency (distinguishable 403/404 or detail leak) → cross-tenant existence inference | 2 | 2 | 4 | MED | Central denial mapper tested once; 404-for-both negative tests on every identifier endpoint | Server | Epic 1 |

**No score-9 (CRITICAL gate-fail) risks** — every score-6 risk has a pre-designed mitigation already embedded in the Epic 1 foundational slices (FS-1..8) or the upstream PR sequence (PR-1..4). This is consistent with the architecture's "READY WITH MINOR GAPS" assessment: the high risks are concentrated in security / data-integrity / consistency and are structurally pre-mitigated, not open-ended.

### 3.3 NFR Planning Assessment

_Planning only — not a PASS/CONCERNS/FAIL verdict. Run `nfr-assess` after implementation evidence exists._

| NFR category | Threshold | Status | Planned evidence source |
|--------------|-----------|--------|-------------------------|
| Security — tenant isolation (NFR-1) | "Impossible by construction" (binary) | Defined | Tier-1 identity-derivation conformance (FS-3); FS-8 cross-tenant suite; Tier-2 query-side authz negatives; E2E real Keycloak |
| Security — layered authz / safe-denial (AR-19, AR-16) | 404 for unauthorized = nonexistent | Defined | Tier-2 denial-mapper tests; per-endpoint negatives |
| Privacy — NoPayloadLeakage (NFR-2) | Zero forbidden fields in any output | Defined qualitatively; needs FS-1 allowlist to be machine-checkable | FS-2 serialization-boundary/Verify CI harness on events/logs/DTO/audit/surfaces |
| Reliability — fail-closed (NFR-3) | Enumerated trust states deny inclusion | Defined | Fault-injection ACL fakes (TC-1); negative path per state |
| Reliability — idempotency (NFR-7) | Dedupe by message id; Idempotency-Key on mutations, rejected on queries | Mechanism defined | FS-6 Tier-1 dup-command + dup-projection; Tier-3 at-least-once |
| Compatibility — schema evolution (NFR-6) | Additive/tolerant; no `V2`; every event ever produced deserializes | Defined | FS-5 golden corpus round-trip; Contracts.Tests serialization |
| Performance (NFR-5, SM-4) | **p95 < 500 ms** internal target (list/open/resolve/context) when sibling metadata available | Partial — **UNKNOWN: error-rate target, load/concurrency profile, project/tenant cardinality, ACL timeout budget** | k6 (system) + BenchmarkDotNet (micro) once endpoints exist; measure Pattern A read |
| Observability (NFR-4) | Structured reason-code/correlation/freshness metadata; OTel | Defined qualitatively — **UNKNOWN: specific RED metrics, trace sampling** | OTel assertions; structured-log contract tests (no payloads) |
| Cross-surface parity (NFR-8) | CLI = MCP = Web identical operational facts | Defined qualitatively | Parity oracle/golden (TC-8); shared-enum + FrontComposer generation gate |
| Accessibility (UX-DR27) | WCAG 2.2 AA | Defined | axe-core + Playwright (Epic 5); bUnit for component a11y |
| Maintainability | Test-tier discipline; no hand-edited generated code | Defined | Tier purity guards; FS-7 regeneration gate; OpenAPI fingerprint |
| Availability / DR | **UNKNOWN** — no SLA, no RTO/RPO stated | UNKNOWN | Confirm whether in v1 scope; likely deferred (internal target, not external SLA) |

**UNKNOWN thresholds → clarification items (do not guess):**
- **CL-1 (PERF):** Error-rate ceiling, target concurrency/load, and tenant/project cardinality for the p95<500ms target? (relates to R11)
- **CL-2 (BUS):** Resolution scoring weights + Single-vs-Multiple confidence-band cutoffs? (blocks precise resolution tests — TC-5/R10; architecture defers to resolution story)
- **CL-3 (OPS):** Availability SLA + RTO/RPO in v1 scope, or explicitly deferred?
- **CL-4 (DATA/TECH):** Memories link granularity — `Case` vs individual `MemoryUnit` (AR-G4)? (shapes Memory-link tests)
- **CL-5 (TECH/OPS):** Idempotency dedup retention window + projection replay SLO?

### 3.4 Risk Findings Summary

Highest-priority mitigation order (all HIGH/score-6, front-loaded into Epic 1 foundations + upstream PRs):
1. **R1 + R2 (security/privacy foundation)** — cross-tenant isolation and NoPayloadLeakage. Named success metric SM-3 and the AI-governance boundary; gate everything. FS-1 allowlist is the very first content story (unblocks all leakage tests).
2. **R3 + R7 (data integrity)** — schema-evolution corpus and projection rebuild determinism. Silent-corruption risks; cheap to establish on the trivial Epic-1 event set, extended per epic.
3. **R4 + R8 (consistency + flakiness)** — idempotency under at-least-once (proven Tier-1 first) is the antidote to Tier-3/E2E flakiness; treat flakiness as critical tech debt.
4. **R5 + R6 (cross-module dependencies)** — sequence PR-1 (Conversations re-parent) before Epic 2 write-side; CDC tests to stop ACL/sibling drift.
5. **R9 (infra authz)** — deny-by-default Dapr access control; deployment-config test.
6. **R10–R13 (MED)** — resolution correctness (after heuristics defined), perf watch-point, generated-code drift gate, safe-denial consistency.

> **Step 4 & Step 5 outputs follow. Final deliverables generated at:** `test-design-architecture.md`, `test-design-qa.md`, `test-design/Hexalith.Projects-handoff.md`. Workflow **completed**.

## Step 4: Coverage Plan & Execution Strategy

**Test-level vocabulary mapped to this stack:**
- **T1 (Unit, pure / xUnit v3)** — aggregate `Handle/Apply`, projection `Apply`, resolution compute, context-assembly allowlist, ACL translators, validators, identity derivation, generator parse/transform/emit. No Dapr/Aspire/network/browser/containers.
- **CT (Contracts.Tests)** — serialization / additive-tolerance / golden-corpus round-trip / naming.
- **T2 (Server)** — API/query endpoints, query-side authz, ProblemDetails/safe-denial, freshness/trust state, tenant-event handling. (≈ "API/Integration".)
- **T3 (Integration, infra)** — Dapr pub/sub, `/process`, actor lifecycle, restart, dead-letter, projection rebuild/replay, real at-least-once. Testcontainers / Dapr slim / Aspire.
- **CMP (bUnit)** — Blazor components.
- **E2E (Playwright + axe-core, real Keycloak)** — critical journeys + WCAG 2.2 AA.
- **CDC (Pact)** — ACL ↔ sibling contract verification.
- **PERF (k6 + BenchmarkDotNet)** — p95 target + micro baselines.

### 4.1 Coverage Matrix (system-level; representative scenarios, not exhaustive — epic-level plans decompose further)

> Duplicate-coverage guard: defense-in-depth across levels (e.g. cross-tenant) is intentional and justified because each level tests a different boundary (pure logic vs query-side filtering vs real-token E2E); it is **not** redundant logic re-testing.

**Area A — Foundational invariants (Epic 1, FS-1..8)**

| # | Scenario | Level | Pri | Risk |
|---|----------|-------|-----|------|
| A1 | Identity derivation: every actor id/state key/projection key/topic/group/log scope derives only from `{tenant}:projects:{projectId}` (FS-3) | T1 | P0 | R1 |
| A2 | Cross-tenant isolation: aggregate/projection/query never returns or mutates another tenant's data (FS-8) | T1 + T2 + E2E | P0 | R1 |
| A3 | NoPayloadLeakage: no forbidden field in any event/log/DTO/audit/rendered surface (FS-1 taxonomy → FS-2 harness) | CT + T1 + (extend T2/E2E) | P0 | R2 |
| A4 | Schema evolution: every event ever produced round-trips from frozen golden corpus; no `V2` (FS-5) | CT | P0 | R3 |
| A5 | Projection rebuild determinism: rebuilt state == incrementally-applied state (FS-6) | T1 + T3 (replay/dead-letter) | P0 | R7 |
| A6 | Idempotency — command dedup (same Idempotency-Key → one event; diff payload → conflict) | T1 | P0 | R4 |
| A7 | Idempotency — projection-apply idempotent under duplicate delivery (separate failure mode) | T1 + T3 | P0 | R4 |
| A8 | FrontComposer regeneration/staleness gate green (generators land Epic 5; gate added Epic 1) (FS-7) | CI gate | P1 | R12 |

**Area B — Workspace lifecycle (Epic 1, FR-1–5/19)**

| # | Scenario | Level | Pri | Risk |
|---|----------|-------|-----|------|
| B1 | `CreateProject` → `ProjectCreated`, `Apply` sets `Active` (pure, persist-then-publish) | T1 | P0 | — |
| B2 | Missing/unauthorized tenant → `ProjectCreationRejected` (IRejectionEvent), not exception; no mixed success+rejection DomainResult | T1 | P0 | R1 |
| B3 | Setup validation rejects secrets/unrestricted paths/foreign payloads/unsupported types; names rejected field without echoing value (FR-19) | T1 | P0 | R2 |
| B4 | List/Detail projections reflect create; `ArchiveProject` → excluded from auto-resolution but still discoverable/auditable | T1 + T2 | P1 | — |
| B5 | `ListProjects` tenant-scoped + authorization-filtered + lifecycle filter | T2 | P0 | R1 |
| B6 | Command-async: 202 `AcceptedCommand`; reads carry freshness/trust; `Idempotency-Key` required-on-mutation / rejected-on-query | T2 | P0 | R4 |
| B7 | Safe-denial: unauthorized == nonexistent → 404 on every identifier-accepting endpoint | T2 | P0 | R13 |

**Area C — Context references & ACLs (Epic 2, FR-6–11)**

| # | Scenario | Level | Pri | Risk |
|---|----------|-------|-----|------|
| C1 | `SetProjectFolder` (exactly one), Link/Unlink File/Memory (bounded sets), unlink ≠ delete, folder replace-only-while-active | T1 | P1 | — |
| C2 | `ProjectReferenceIndexProjection` health/freshness states | T1 | P1 | — |
| C3 | ACL translators map sibling DTO → Projects view + `ProjectionTrustState` + sibling denial → Projects-safe problem (no raw upstream detail) | T1 | P0 | R2 |
| C4 | ACL fault injection: each trust state (`unauthorized/unavailable/stale/rebuilding/forbidden/redacted/tenant_mismatch`) → fail-closed exclusion | T1 (fault fakes) | P0 | R-NFR3 |
| C5 | ACL ↔ sibling contract verification (Conversations list / Folders / Memories) | CDC + T3 | P0 | R6 |
| C6 | Conversation discovery (Pattern A, read-only, Story 2.1) tenant-scoped/fail-closed | T1 + T2 | P1 | R5 |
| C7 | **(gated)** FR-6/7 conversation link/move write-side — scaffold only until PR-1 (AR-G1) lands | T1 | P1 | R5 |

**Area D — Context assembly (Epic 3, FR-16–18/20)**

| # | Scenario | Level | Pri | Risk |
|---|----------|-------|-----|------|
| D1 | Allowlist assembly: reference included only after tenant+project+lifecycle+authz+freshness all pass; exclusions carry state+reason code | T1 | P0 | R1/R2 |
| D2 | `ExplainContextSelection` inclusion/exclusion metadata (no payloads) | T1 + leakage | P1 | R2 |
| D3 | `RefreshProjectContext` surfaces stale/unavailable rather than hiding | T1 | P1 | R-NFR3 |
| D4 | `GetConversationStartSetup` subset (excludes audit/unavailable/unauthorized) | T1 | P2 | — |

**Area E — Resolution (Epic 4, FR-12–15)**

| # | Scenario | Level | Pri | Risk |
|---|----------|-------|-----|------|
| E1 | Outcomes `NoMatch`/`SingleCandidate`/`MultipleCandidates` + reason codes; **never silently attach**; archived excluded unless requested | T1 | P0 | R10 |
| E2 | Resolution from attachments (folder/file ref-index match); fail-closed on stale/missing authz | T1 | P1 | R-NFR3 |
| E3 | `ConfirmProjectResolution` persists only confirmation; rejected candidates not linked | T1 | P1 | — |
| E4 | `ProposeNewProject` create-on-confirm; **(gated)** "link initiating conversation" on AR-G1 | T1 | P1 | R5 |
| E5 | **(blocked)** precise scoring/confidence-band assertions — pending CL-2 heuristics | T1 | P1 | R10/CL-2 |

**Area F — Operational surfaces & audit (Epic 5, FR-21–22, UX-DR*)**

| # | Scenario | Level | Pri | Risk |
|---|----------|-------|-----|------|
| F1 | Audit timeline projection metadata-only (FR-21) | T1 + leakage | P1 | R2 |
| F2 | Operator read access authz-gated, tenant-scoped, metadata-only (FR-22) | T2 | P1 | R1 |
| F3 | Cross-surface parity oracle: CLI == MCP == Web fields/reason-codes/audit-ids for same query (NFR-8) | parity golden | P1 | TC-8 |
| F4 | FrontComposer generated output snapshots stable (Verify.XunitV3) | generator/CMP | P1 | R12 |
| F5 | E2E critical journeys: open→context · resolve→confirm · maintenance archive/restore w/ dry-run · audit view | E2E | P1 | — |
| F6 | WCAG 2.2 AA: keyboard/focus/contrast/status-not-color-only/SR tables+timelines (axe-core) | E2E + CMP | P1 | ASR-14 |
| F7 | CLI structured JSON, stable exit codes, redaction-safe, no color-reliance | T2/CLI | P2 | — |
| F8 | MCP resources-vs-tools separation; tenant-aware; mutating tools require target+confirmation+dry-run | T2/MCP | P2 | — |

### 4.2 NFR Coverage & Evidence Plan (concise)

| NFR | Planned validation scenario(s) | Level/tool | Evidence artifact for `nfr-assess` |
|-----|-------------------------------|-----------|-----------------------------------|
| Security/tenant-isolation (NFR-1) | A1, A2, B5, B7, F2 | T1+T2+E2E (real Keycloak) | Cross-tenant suite report; identity-conformance test results |
| Privacy/NoPayloadLeakage (NFR-2) | A3, C3, D2, F1 | CT+T1 harness, extend T2/E2E | NoPayloadLeakage CI gate report; allowlist (FS-1) |
| Reliability/fail-closed (NFR-3) | C4, D1, D3, E2 | T1 fault-injection | Trust-state negative-path results |
| Idempotency (NFR-7) | A6, A7 | T1 + T3 | Dup-command & dup-delivery test results |
| Schema evolution (NFR-6) | A4 | CT | Golden-corpus round-trip report |
| Performance (NFR-5) | List/open/resolve/context p95<500ms | PERF (k6) + BenchmarkDotNet | k6 summary (p95, error rate); micro-bench baselines — **blocked on CL-1 thresholds** |
| Observability (NFR-4) | Structured-log/OTel contract; reason codes present, payloads absent | T2 + leakage | Structured-log contract test; trace assertions |
| Cross-surface parity (NFR-8) | F3 | parity golden | Parity oracle diff report |
| Accessibility (UX-DR27) | F6 | axe-core + Playwright + bUnit | axe-core scan report |
| Maintainability | Tier purity guards, FS-7 gate, OpenAPI fingerprint | CI | Gate logs |

### 4.3 Execution Strategy (PR / Nightly / Weekly)

- **PR (<15 min, must be 100% green):** all **T1** + **CT** + **T2** + **CMP** + CI gates (NoPayloadLeakage, OpenAPI fingerprint/compat, `frontcomposer inspect --fail-on-warning`, FS-7 regeneration staleness, schema golden-corpus round-trip). This is the bulk of coverage and is pure/fast/deterministic.
- **Nightly:** **T3** integration (Dapr/Aspire/Testcontainers, at-least-once pub/sub, restart, dead-letter, rebuild/replay), **E2E** Playwright + axe-core (real Keycloak/OIDC), **CDC** Pact provider verification.
- **Weekly:** **PERF** (k6 load/stress/spike + BenchmarkDotNet baselines), endurance/soak, large-dataset projection rebuild, full cross-surface parity regression.
- **Quarantine lane:** genuinely flaky T3/E2E isolated and excluded from the main lane — never silence assertions to go green (R8).

### 4.4 Resource Estimates (ranges — greenfield includes building the test harness, not just tests)

| Priority | Scope | Estimate |
|----------|-------|----------|
| P0 | Foundational harness (Testing fakes, fault-injection ACLs, NoPayloadLeakage harness, golden corpus, identity conformance, cross-tenant suite, idempotency/rebuild) + core lifecycle/assembly/resolution correctness | ~80–120 h |
| P1 | References, ACL CDC, projections, audit, parity oracle, E2E journeys + a11y, generated snapshots | ~60–100 h |
| P2 | CLI/MCP detail, components, conversation-start setup, secondary flows | ~25–45 h |
| P3 | Exploratory, extra benchmarks, nice-to-have | ~8–15 h |
| **Total** | — | **~175–280 h**, front-loaded: P0 harness in Epic 1; remainder distributed across Epics 2–5 |

_Estimates are test-engineering effort and assume reuse of EventStore/Tenants Testing fakes; they exclude feature implementation. No false precision — ranges only._

### 4.5 Quality Gates

- **P0 pass rate = 100%; P1 ≥ 95%.**
- **High-risk (score-6) mitigations complete before the owning epic's release:** R1/R2/R3/R4/R7 before Epic 1 "done"; R5/R6 before Epic 2 write-side; R9 before any prod-targeted deploy.
- **Coverage targets:** pure core (T1) > 90% on P0 logic; overall ≥ 80% (justified: pure core is cheap, high-value, and where the invariants live).
- **CI gates green (blocking):** NoPayloadLeakage, OpenAPI fingerprint/compat, `frontcomposer inspect --fail-on-warning`, FS-7 regeneration staleness, schema golden-corpus round-trip.
- **Zero un-quarantined flaky tests** in the PR/main lane.
- **No score-9 risk OPEN** (currently none).
- **NFR validation evidence identified** for each in-scope category (4.2); final PASS/CONCERNS/FAIL deferred to `nfr-assess` once implementation evidence exists.
- **Clarifications CL-1..CL-5** resolved before the tests they block (esp. CL-1 before PERF gate, CL-2 before E5 precise resolution assertions).
