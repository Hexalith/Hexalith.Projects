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

# Test Design for QA: Hexalith.Projects

**Purpose:** Test execution recipe for the QA/Dev teams. What to test, at which level, and what QA needs from other teams.

**Date:** 2026-05-25
**Author:** Murat (Master Test Architect)
**Status:** Draft
**Project:** Hexalith.Projects

**Related:** See `test-design-architecture.md` for testability concerns, architectural blockers, and risk mitigation plans.

---

## Executive Summary

**Scope:** Test coverage for the greenfield `Hexalith.Projects` module (EventStore + Dapr + Aspire; Web/MCP/CLI generated surfaces) across all 5 epics — FR-1–22, NFR-1–9, foundational slices FS-1–8.

**Risk Summary:** 13 risks — 9 high-priority (score ≥6), 4 medium, 0 critical. Critical categories: SEC/DATA (cross-tenant isolation, payload safety) and TECH/DATA (schema evolution, idempotency, rebuild determinism).

**Coverage Summary (scenario groups — each expands to multiple atomic tests at epic level):**
- P0: ~15 (foundational invariants FS-1–8, security/data-integrity, core lifecycle)
- P1: ~20 (references, ACL CDC, assembly, resolution, surfaces, a11y)
- P2: ~4 (CLI/MCP detail, components, conversation-start setup)
- P3: ~4 (perf baselines, soak, exploratory)
- **Total:** ~43 scenario groups · ~175–280 engineering hours (see QA Effort Estimate)

> The P0 count is intentionally high: it is dominated by **one-time foundational harnesses** (FS-1–8) and **by-construction security invariants**, not per-feature happy paths. Once the harnesses exist, per-epic P0 cost drops sharply.

---

## Not in Scope

| Item | Reasoning | Mitigation |
| ---- | --------- | ---------- |
| Sibling-context internals (Conversations/Folders/Memories/Tenants domain logic) | Owned by sibling modules; Projects references by ID only | Covered by sibling test suites; Projects tests the ACL boundary + CDC |
| FR-6/FR-7 conversation link/move **write-side** | Blocked on AR-G1 (PR-1) upstream re-parent event | Read-discovery (Story 2.1) tested now; write-side scaffolded, gated on PR-1 |
| Auto-folder-create on `CreateProject` | Folders `CreateFolder` not yet external (AR-G3); deferred per PR-3 | `CreateProject` tested without auto-folder; auto-folder slice gated |
| Final NFR PASS/CONCERNS/FAIL verdicts | Requires implementation evidence | Deferred to `nfr-assess`; this doc plans evidence only |

**Note:** items here are reviewed and accepted as out-of-scope by QA, Dev, and PM.

---

## Dependencies & Test Blockers

**CRITICAL:** QA cannot proceed on the affected areas without these.

### Backend/Architecture Dependencies (Pre-Implementation)

_Source: see Architecture doc "Quick Guide" for mitigation plans._

1. **FS-1 payload-classification allowlist** — Domain/Arch — Epic 1 (first content story)
   - Needed: machine-usable safe-vs-forbidden field taxonomy.
   - Blocks: every `NoPayloadLeakage` test (R2) — no boundary to assert without it.
2. **AR-G2 `ListConversationsAsync`** — Conversations — before Story 2.1
   - Needed: list/search on `IConversationClient` (or `ConversationReadApi`).
   - Blocks: Pattern A conversation discovery ACL and any conversation-aware view/resolution.
3. **AR-G1 Conversations re-parent event (PR-1)** — Arch/Conversations — before Epic 2 write-side
   - Needed: additive post-creation project assignment/move command+event.
   - Blocks: FR-6/FR-7/FR-15 write-side tests.
4. **Fault-injection ACL fakes + projection-readiness probe** — Domain/Platform — Epic 1
   - Needed: fakes emitting every trust state; deterministic watermark/ETag convergence helper.
   - Blocks: fail-closed (NFR-3) negatives and flake-free Tier-3/E2E (R8).

### QA Infrastructure Setup (Pre-Implementation)

1. **Test data builders/fakes** — QA/Dev
   - `ProjectBuilder` + event-stream builders; reuse EventStore/Tenants `Testing` fakes/builders before inventing doubles.
   - Fault-injection ACL fakes (`IProjectConversationDirectory`/`IProjectFolderDirectory`/`IProjectMemoryDirectory`/`ITenantAccess`) covering `unauthorized/unavailable/stale/rebuilding/forbidden/redacted/tenant_mismatch`.
2. **Test environments** — QA/Platform
   - Local: Aspire AppHost (eventstore, tenants, projects, workers, projects-ui, Keycloak, Dapr + Redis).
   - CI: filtered `dotnet test` lanes per tier; Testcontainers for Tier-3; Playwright browsers installed in `tests/e2e` (Node ≥24).
   - E2E security: real Keycloak/OIDC realm `hexalith`; synthetic JWT for Tier-1/2.

**Example — dominant tier (Tier-1, xUnit v3 + Shouldly):**

```csharp
// tests/Hexalith.Projects.Tests/Aggregates/CreateProjectTests.cs
[Fact] // P0-008
public void Handle_CreateProject_WithoutTenant_EmitsRejectionNotException()
{
    var aggregate = new ProjectAggregate();
    var result = aggregate.Handle(new CreateProject(name: "Acme", tenant: null));

    result.Events.ShouldHaveSingleItem()
          .ShouldBeOfType<ProjectCreationRejected>()       // IRejectionEvent, not an exception
          .ReasonCode.ShouldBe(ProjectRejectionReason.TenantContextMissing);
    result.Events.ShouldNotContain(e => e is ProjectCreated); // no mixed success+rejection DomainResult
}
```

**Example — E2E lane (Playwright + playwright-utils, `tea_use_playwright_utils: true`):**

```typescript
import { test } from '@seontechnologies/playwright-utils/api-request/fixtures';
import { expect } from '@playwright/test';

// P0-002 cross-tenant isolation at the surface (real Keycloak token, tenant B)
test('@P0 @Security tenant B cannot read tenant A project → 404', async ({ apiRequest }) => {
  const { status, body } = await apiRequest({
    method: 'GET',
    path: `/api/v1/projects/${process.env.TENANT_A_PROJECT_ID}`, // token scoped to tenant B
  });

  expect(status).toBe(404);          // safe-denial: unauthorized == nonexistent
  expect(JSON.stringify(body)).not.toContain(process.env.TENANT_A_PROJECT_NAME!); // no existence leak
});
```

---

## Risk Assessment

_Full risk details, scores, and mitigation plans in the Architecture doc. This summarizes QA-relevant coverage._

### High-Priority Risks (Score ≥6)

| Risk ID | Category | Description | Score | QA Test Coverage |
| ------- | -------- | ----------- | ----- | ---------------- |
| **R1** | SEC/DATA | Cross-tenant leakage | **6** | P0-001 identity conformance, P0-002 cross-tenant suite (T1+T2+E2E), P0-010 query-side filtering |
| **R2** | SEC/DATA | Payload leakage | **6** | P0-003 NoPayloadLeakage harness; leakage assertions woven into P0-009, P1-008, P1-014 |
| **R3** | TECH/DATA | Schema-evolution break | **6** | P0-004 golden-corpus round-trip (Contracts.Tests) |
| **R4** | TECH/OPS | Idempotency under at-least-once | **6** | P0-006 command dedup, P0-007 projection-apply idempotency, P0-011 Idempotency-Key contract |
| **R5** | TECH/BUS | AR-G1 blocks conversation write-side | **6** | P1-006 read discovery now; P1-007/P1-012 write-side scaffolded + gated on PR-1 |
| **R6** | TECH | ACL↔sibling contract drift | **6** | P1-005 consumer-driven contract tests (Pact) + Tier-3 against pinned siblings |
| **R7** | DATA/OPS | Projection rebuild non-determinism | **6** | P0-005 rebuild determinism (T1) + Tier-3 replay/dead-letter |
| **R8** | OPS/TECH | Eventual-consistency/E2E flakiness | **6** | **QA-owned:** prove idempotency Tier-1 first; deterministic readiness probes; network-first; stable test IDs; quarantine lane |
| **R9** | SEC/OPS | Dapr authz bypass | **6** | Tier-3 deployment-config test: deny-by-default; internal endpoints unreachable externally |

### Medium-Priority Risks

| Risk ID | Category | Description | Score | QA Test Coverage |
| ------- | -------- | ----------- | ----- | ---------------- |
| R10 | BUS | Resolution silent auto-attach | 4 | P0-015 never-silent-attach; P1-013 precise scoring (blocked on CL-2) |
| R11 | PERF | p95<500ms missed | 4 | P3-001 k6 p95 (blocked on CL-1); measure Pattern A read |
| R12 | OPS/TECH | Generated-code drift / RC churn | 4 | P1-017 Verify snapshots; P1-020 FS-7 regeneration gate |
| R13 | SEC | Safe-denial inconsistency | 4 | P0-012 404-for-both on every identifier endpoint |

---

## NFR Test Coverage Plan

Maps NFRs to planned validation work and the evidence `nfr-assess` will later consume. No final PASS/CONCERNS/FAIL here.

| NFR Category | Requirement / Threshold | Planned Validation | Tool / Level | Evidence Artifact | Priority |
| ------------ | ----------------------- | ------------------ | ------------ | ----------------- | -------- |
| Security (NFR-1) | Tenant isolation by construction; safe-denial 404 | Identity conformance; cross-tenant suite; per-endpoint denial | T1 + T2 + E2E (real Keycloak) | Cross-tenant suite report; identity-conformance results | P0 |
| Privacy (NFR-2) | Zero forbidden fields anywhere | NoPayloadLeakage harness on events/logs/DTO/audit/surfaces | Contracts.Tests + T1, extend T2/E2E | NoPayloadLeakage CI gate report | P0 |
| Reliability/fail-closed (NFR-3) | Trust states deny inclusion | Fault-injection per trust state | T1 (fault fakes) | Trust-state negative-path results | P0 |
| Idempotency (NFR-7) | Dedup by message id; Idempotency-Key on mutations, rejected on queries | Dup-command + dup-projection-delivery | T1 + T3 | Idempotency test results | P0 |
| Compatibility (NFR-6) | Additive/tolerant; no `V2`; all events deserialize | Golden-corpus round-trip | Contracts.Tests | Corpus round-trip report | P0 |
| Performance (NFR-5) | p95 < 500 ms (list/open/resolve/context) | Load + micro baselines | k6 + BenchmarkDotNet | k6 summary (p95, error rate); bench baselines | P3 |
| Observability (NFR-4) | Structured reason-code/correlation/freshness; OTel; no payloads | Structured-log contract + trace assertions | T2 + leakage | Log-contract test; trace assertions | P1 |
| Parity (NFR-8) | CLI = MCP = Web identical facts | Parity oracle diff | parity golden | Parity diff report | P1 |
| Accessibility (UX-DR27) | WCAG 2.2 AA | axe-core scans + keyboard/focus/SR checks | E2E + bUnit | axe-core scan report | P1 |
| Maintainability | Tier purity; no hand-edited generated code | Purity guards; regeneration gate; OpenAPI fingerprint | CI | Gate logs | P1 |

**Missing thresholds / evidence needing clarification before `nfr-assess`:** CL-1 (perf error-rate/load/cardinality) · CL-2 (resolution scoring/confidence bands) · CL-3 (availability SLA + RTO/RPO scope) · CL-4 (Memories Case-vs-Unit) · CL-5 (dedup retention + replay SLO).

---

## Entry Criteria

- [ ] FS-1 payload allowlist published; FS-3 identity helper available.
- [ ] Test environments provisioned (Aspire AppHost local; Testcontainers CI; Keycloak realm `hesalith` for E2E; Playwright browsers in `tests/e2e`).
- [ ] `ProjectBuilder` + fault-injection ACL fakes ready (or EventStore/Tenants fakes reused).
- [ ] Pre-implementation blockers resolved for the area under test (AR-G2 for discovery; AR-G1 for conversation write-side).
- [ ] Feature/slice deployed to the relevant test lane.

## Exit Criteria

- [ ] All P0 tests passing (100%).
- [ ] All P1 tests passing (≥95%; failures triaged and accepted).
- [ ] No open high-priority/high-severity bugs.
- [ ] CI gates green: NoPayloadLeakage, OpenAPI fingerprint/compat, `frontcomposer inspect --fail-on-warning`, FS-7 regeneration staleness, schema golden-corpus round-trip.
- [ ] Zero un-quarantined flaky tests in the PR lane.
- [ ] Performance baselines captured once CL-1 thresholds are set (gate may be deferred).

---

## Test Coverage Plan

**IMPORTANT:** P0/P1/P2/P3 = **priority and risk level** (what to focus on if time-constrained), NOT execution timing. See "Execution Strategy" for when tests run. Levels: **T1** Tier-1 pure · **CT** Contracts.Tests · **T2** Server · **T3** Integration · **CMP** bUnit component · **E2E** Playwright · **CDC** Pact · **PERF** k6/BenchmarkDotNet.

### P0 (Critical)

**Criteria:** Blocks core functionality + high risk (≥6) OR security/data-integrity-critical + no workaround.

| Test ID | Requirement | Test Level | Risk Link | Notes |
| ------- | ----------- | ---------- | --------- | ----- |
| **P0-001** | Identity derivation only from `{tenant}:projects:{projectId}` (FS-3) | T1 | R1 | All keys/topics/groups/log scopes; no payload/header/query-derived identity |
| **P0-002** | Cross-tenant isolation — no cross-tenant read/write/leak (FS-8) | T1 + T2 + E2E | R1 | Defense-in-depth across layers (justified, not duplicate) |
| **P0-003** | NoPayloadLeakage taxonomy + harness (FS-1/FS-2) | CT + T1 | R2 | Blocked until FS-1 allowlist exists |
| **P0-004** | Schema-evolution golden corpus round-trip; no `V2` (FS-5) | CT | R3 | Frozen sample per event ever produced |
| **P0-005** | Projection rebuild determinism (same events → same state) (FS-6) | T1 + T3 | R7 | Tier-3 covers replay/dead-letter |
| **P0-006** | Command dedup (same Idempotency-Key → one event; diff payload → conflict) | T1 | R4 | Field-scoped equivalence hashing |
| **P0-007** | Projection-apply idempotent under duplicate delivery | T1 + T3 | R4 | Separate failure mode from P0-006 |
| **P0-008** | `CreateProject` → `ProjectCreated`/`Active`; missing tenant → `ProjectCreationRejected` | T1 | R1 | Pure, persist-then-publish; no mixed DomainResult |
| **P0-009** | Setup validation rejects secrets/unrestricted paths/foreign payloads/unsupported types | T1 | R2 | Names rejected field without echoing its value (FR-19) |
| **P0-010** | `ListProjects` tenant-scoped + authorization-filtered + lifecycle filter | T2 | R1 | Query-side filtering, not just API/JWT |
| **P0-011** | Command-async 202 `AcceptedCommand`; reads carry freshness; Idempotency-Key required-on-mutation/rejected-on-query | T2 | R4 | No read-after-write assumption |
| **P0-012** | Safe-denial: unauthorized == nonexistent → 404 on every identifier endpoint | T2 | R13 | Central denial mapper tested once |
| **P0-013** | ACL translators map sibling DTO → Projects view + trust state; sibling denial → Projects-safe problem | T1 | R2 | No raw upstream detail rethrown |
| **P0-014** | ACL fault injection: each trust state → fail-closed exclusion | T1 (fault fakes) | R-NFR3 | `unauthorized/unavailable/stale/rebuilding/forbidden/redacted/tenant_mismatch` |
| **P0-015** | ProjectContext allowlist assembly; never-silent-attach (resolution outcomes) | T1 | R1/R2/R10 | Inclusion only after tenant+project+lifecycle+authz+freshness pass; exclusions carry reason code |

**Total P0:** ~15 scenario groups.

### P1 (High)

**Criteria:** Core user journeys + medium/high risk; integration points; UX-affecting features.

| Test ID | Requirement | Test Level | Risk Link | Notes |
| ------- | ----------- | ---------- | --------- | ----- |
| **P1-001** | List/Detail projections reflect create; archive excludes from auto-resolution but stays discoverable | T1 + T2 | — | Lifecycle Active/Archived |
| **P1-002** | `GetProject`/Open returns metadata + authorized refs; archived clearly identified | T2 | — | — |
| **P1-003** | `SetProjectFolder` (exactly one); Link/Unlink File/Memory bounded; unlink ≠ delete; folder replace-only-while-active | T1 | — | — |
| **P1-004** | `ProjectReferenceIndexProjection` health/freshness states | T1 | — | — |
| **P1-005** | ACL↔sibling contract verification (Conversations/Folders/Memories) | CDC + T3 | R6 | Pact or pinned-version integration |
| **P1-006** | Conversation discovery (Pattern A, read-only, Story 2.1) tenant-scoped/fail-closed | T1 + T2 | R5 | Depends on AR-G2 |
| **P1-007** | (gated) FR-6/7 conversation link/move write-side | T1 | R5 | Scaffold only until PR-1 lands |
| **P1-008** | `ExplainContextSelection` inclusion/exclusion metadata (no payloads) | T1 + leakage | R2 | — |
| **P1-009** | `RefreshProjectContext` surfaces stale/unavailable rather than hiding | T1 | R-NFR3 | — |
| **P1-010** | Resolution from attachments (folder/file ref-index); fail-closed on stale authz | T1 | R-NFR3 | — |
| **P1-011** | `ConfirmProjectResolution` persists only confirmation; rejected candidates not linked | T1 | — | — |
| **P1-012** | `ProposeNewProject` create-on-confirm; (gated) link initiating conversation | T1 | R5 | AR-G1 gate |
| **P1-013** | (blocked) precise resolution scoring/confidence-band assertions | T1 | R10 | Pending CL-2 heuristics |
| **P1-014** | Audit timeline projection metadata-only (FR-21) | T1 + leakage | R2 | — |
| **P1-015** | Operator read access authz-gated, tenant-scoped, metadata-only (FR-22) | T2 | R1 | — |
| **P1-016** | Cross-surface parity oracle (CLI == MCP == Web) | parity golden | NFR-8 | Fields/reason-codes/audit-ids |
| **P1-017** | FrontComposer generated output snapshots stable | CMP/generator | R12 | Verify.XunitV3 |
| **P1-018** | E2E critical journeys: open→context · resolve→confirm · maintenance archive/restore w/ dry-run · audit view | E2E | — | Network-first; stable `data-testid` |
| **P1-019** | WCAG 2.2 AA: keyboard/focus/contrast/status-not-color-only/SR tables+timelines | E2E + CMP | ASR-14 | axe-core |
| **P1-020** | FrontComposer regeneration/staleness gate green | CI | R12 | Added Epic 1; generators land Epic 5 |

**Total P1:** ~20 scenario groups.

### P2 (Medium)

**Criteria:** Secondary features + low risk + edge cases.

| Test ID | Requirement | Test Level | Risk Link | Notes |
| ------- | ----------- | ---------- | --------- | ----- |
| **P2-001** | `GetConversationStartSetup` subset (excludes audit/unavailable/unauthorized) | T1 | — | — |
| **P2-002** | CLI structured JSON, stable exit codes, redaction-safe, no color-reliance | T2/CLI | — | — |
| **P2-003** | MCP resources-vs-tools separation; tenant-aware; mutating tools require target+confirmation+dry-run | T2/MCP | — | — |
| **P2-004** | Blazor component coverage (states, a11y attributes) | CMP | — | bUnit |

**Total P2:** ~4 scenario groups.

### P3 (Low)

**Criteria:** Nice-to-have, exploratory, performance benchmarks.

| Test ID | Requirement | Test Level | Notes |
| ------- | ----------- | ---------- | ----- |
| **P3-001** | Performance p95<500ms (list/open/resolve/context) | PERF (k6) | Blocked on CL-1 thresholds |
| **P3-002** | Micro baselines (assembly/resolution hot paths) | BenchmarkDotNet | Regression baselines |
| **P3-003** | Endurance/soak: large-dataset projection rebuild | T3 (weekly) | Memory/resource exhaustion |
| **P3-004** | Exploratory resolution-heuristic tuning | manual | After CL-2 defined |

**Total P3:** ~4 scenario groups.

---

## Execution Strategy

**Philosophy:** run everything in PRs unless there's significant infrastructure overhead. The pure Tier-1/Contracts/Tier-2 + bUnit suite is fast and parallelizable; defer only the expensive/long-running tiers.

### Every PR (<15 min)
- **T1** (aggregate/projection/resolution/context-assembly/ACL-translator), **CT** (serialization/golden-corpus), **T2** (API/query/authz/ProblemDetails/freshness), **CMP** (bUnit).
- **CI gates (blocking):** NoPayloadLeakage, OpenAPI fingerprint/compat, `frontcomposer inspect --fail-on-warning`, FS-7 regeneration staleness, schema golden-corpus round-trip.

### Nightly (~30–60 min)
- **T3** integration (Dapr/Aspire/Testcontainers, at-least-once pub/sub, restart, dead-letter, rebuild/replay), **E2E** Playwright + axe-core (real Keycloak/OIDC), **CDC** Pact provider verification.

### Weekly (~hours)
- **PERF** k6 (load/stress/spike) + BenchmarkDotNet baselines, endurance/soak, large-dataset projection rebuild, full cross-surface parity regression.

**Quarantine lane:** genuinely flaky T3/E2E isolated and excluded from the main lane — never silence assertions to go green (R8).

---

## QA Effort Estimate

QA/test-engineering effort only (excludes feature implementation, DevOps, Finance). Greenfield includes building the harness, not just tests.

| Priority | Count | Effort Range | Notes |
| -------- | ----- | ------------ | ----- |
| P0 | ~15 | ~80–120 h | Foundational harnesses (FS-1–8), fault-injection fakes, security/data-integrity suites |
| P1 | ~20 | ~60–100 h | References, ACL CDC, assembly, resolution, surfaces, a11y, snapshots |
| P2 | ~4 | ~25–45 h | CLI/MCP detail, components, conversation-start setup |
| P3 | ~4 | ~8–15 h | Perf baselines, soak, exploratory |
| **Total** | ~43 | **~175–280 h** | Front-loaded: P0 harness in Epic 1; remainder across Epics 2–5 |

**Assumptions:** includes test design, implementation, debugging, CI integration; excludes ongoing maintenance (~10%); assumes reuse of EventStore/Tenants `Testing` fakes. Ranges only — no false precision.

---

## Implementation Planning Handoff

| Work Item | Owner | Target Milestone | Dependencies/Notes |
| --------- | ----- | ---------------- | ------------------ |
| FS-1 payload allowlist + FS-2 NoPayloadLeakage harness | Domain + QA | Epic 1 (first) | Blocks all leakage tests (R2) |
| FS-3 identity helper + FS-8 cross-tenant suite | Domain + QA | Epic 1 | R1; re-run per surface |
| FS-5 schema golden corpus | Domain | Epic 1 | R3 |
| FS-6 rebuild determinism + dual idempotency stories | Domain + Platform | Epic 1 → per epic | R4, R7 |
| Fault-injection ACL fakes + projection-readiness probe | Domain/Platform + QA | Epic 1 | NFR-3, R8 |
| FS-7 regeneration/staleness gate | Platform | Epic 1 | R12 (before generators land Epic 5) |
| ACL consumer-driven contract tests (Pact) | Domain + sibling owners | Epic 2 | R6; after AR-G2 |
| Parity oracle (CLI/MCP/Web) | Platform + QA | Epic 5 | NFR-8 |
| k6 perf harness + baselines | QA + Platform | Epic 5 (gate) | Blocked on CL-1 |

---

## Tooling & Access

| Tool/Service | Purpose | Access Required | Status |
| ------------ | ------- | --------------- | ------ |
| Keycloak (realm `hexalith`) | Real OIDC tokens for E2E security | Test realm + client creds | Pending |
| Testcontainers | Tier-3 infra boundaries | Container runtime in CI | Pending |
| Pact / broker | ACL↔sibling contract verification | Broker or local pacts | Pending (decision: R6) |
| k6 | Performance/load | Runner + CI integration | Pending (blocked CL-1) |

**Access requests:** [ ] Keycloak test realm · [ ] CI container runtime · [ ] Pact broker (if adopted).

---

## Interworking & Regression

| Service/Component | Impact | Regression Scope | Validation |
| ----------------- | ------ | ---------------- | ---------- |
| Hexalith.Conversations | New re-parent command/event (PR-1); list method (PR-2) | Existing conversation create/read unaffected (additive) | Sibling suite + Projects CDC |
| Hexalith.Folders | `CreateFolder` external exposure (AR-G3) | Existing Folders flows unaffected | Sibling suite + Projects ACL integration |
| Hexalith.Tenants | Tenants events consumed into `TenantAccessProjection` | Tenant event schema stable (additive) | Tenant-event handler idempotency tests |
| Hexalith.EventStore | New aggregate/projections registered | No change to EventStore core | Tier-1 replay + Tier-3 pipeline |

**Regression strategy:** additive contract changes only; CDC + golden-corpus guard against drift; run sibling suites on their submodule PRs (submodule-first, never mixed with Projects code).

---

## Appendix A: Code Examples & Tagging

```typescript
// E2E (Playwright + playwright-utils) — tag for selective execution
import { test } from '@seontechnologies/playwright-utils/api-request/fixtures';
import { expect } from '@playwright/test';

test('@P0 @Security @API unauthorized project read returns 404 (safe-denial)', async ({ apiRequest }) => {
  const { status } = await apiRequest({ method: 'GET', path: '/api/v1/projects/unknown-or-forbidden' });
  expect(status).toBe(404);
});
```

```csharp
// Tier-1 (xUnit v3 + Shouldly) — NoPayloadLeakage assertion (after FS-1 allowlist exists)
[Fact] // P0-003
public void ProjectCreated_SerializesWithoutForbiddenFields()
{
    var evt = new ProjectCreated(/* metadata-only */);
    var json = JsonSerializer.Serialize(evt, ProjectsJson.Options);
    PayloadAllowlist.ForbiddenFields.ShouldAllBe(f => !json.Contains(f, StringComparison.OrdinalIgnoreCase));
}
```

**Run by tag (E2E lane):**
```bash
npx playwright test --grep @P0            # P0 only
npx playwright test --grep "@P0|@P1"      # P0 + P1
npx playwright test --grep @Security       # security only
```

**Run by tier (.NET):**
```bash
dotnet test Hexalith.Projects.slnx --filter "Category=Tier1"   # PR lane (pure)
dotnet test tests/Hexalith.Projects.Integration.Tests          # nightly (Tier-3)
```

---

## Appendix B: Knowledge Base References

- **Risk Governance** — `risk-governance.md` (scoring P×I, gate decisions)
- **Test Priorities Matrix** — `test-priorities-matrix.md` (P0–P3 criteria)
- **Test Levels Framework** — `test-levels-framework.md` (level selection; here mapped to Tier-1/2/3 + E2E)
- **NFR Criteria** — `nfr-criteria.md` (security/perf/reliability/maintainability validation)
- **Test Quality** — `test-quality.md` (DoD: no hard waits, <300 lines, <1.5 min, self-cleaning)
- **ADR Quality Readiness** — `adr-quality-readiness-checklist.md` (8-category testability framework)

---

**Generated by:** BMad TEA Agent (Murat)
**Workflow:** `bmad-testarch-test-design`
**Version:** 4.0 (BMad v6)
