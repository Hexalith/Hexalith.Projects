---
workflowStatus: 'completed'
totalSteps: 5
stepsCompleted: ['step-01-detect-mode', 'step-02-load-context', 'step-03-risk-and-testability', 'step-04-coverage-plan', 'step-05-generate-output']
lastStep: 'step-05-generate-output'
nextStep: ''
lastSaved: '2026-07-17'
---

# Test Design: Epic 6 — Authorized Project Reads on Supported Platform

**Date:** 2026-07-17  
**Author:** Jerome  
**Status:** Draft

---

## Executive Summary

**Scope:** Full Epic-level test design for Stories 6.1–6.7: supported list/open, conversation-start subset, context/refresh/explain, current-project resolution, authenticated FrontComposer and CLI reads, and shadow-read cutover/rollback.

The plan tests the new platform-supported read path independently from the well-covered legacy behavior. Existing domain, server, UI, and E2E suites remain regression inputs, but do not prove the missing SDK query seams, scoped opaque cursors, real persisted Dapr boundary, or reversible read migration.

**Risk Summary:**

- Total risks identified: **16**
- High-priority risks (score ≥6): **14**
- Critical categories: **TECH, OPS, DATA, SEC**
- Highest exposure: missing G4 evidence runner and SDK seams; non-deterministic live authorization data; absent real persisted-boundary proof; no shadow-cutover/rollback suite; unresolved external G3/G5/G6 gates.

**Coverage Summary:**

- P0 scenarios: **37** (~95–145 hours)
- P1 scenarios: **11** (~45–75 hours)
- P2/P3 scenarios: **4** (~16–32 hours)
- **Total effort:** ~156–252 test-engineering hours, approximately 5–8 weeks for one engineer or 3–5 calendar weeks for two engineers after dependencies are ready.

The 2026-07-17 implementation-readiness verdict is **READY** for story creation and planning, not release. This plan preserves every per-story entry gate and treats absent evidence as non-passing.

---

## Not in Scope

| Item | Reasoning | Mitigation |
| --- | --- | --- |
| Production implementation of Epic 6 or the platform SDK | This document plans verification; it does not implement the feature. | R-601/R-602 make G4 and the exact SDK surface entry gates before test execution. |
| Remediation owned by external G3/G5/G6 platform teams | FrontComposer composition, identity, and toolchain readiness are dependencies, not Projects test work. | Track R-616 at every story entry; substitutes may support lower-level testing but cannot support release claims. |
| Full re-test design for Epics 1–5 | Their legacy suites are already established and are not the target of this epic. | Run impacted regression suites and reuse their scenario corpus for shadow equivalence and legacy replay. |
| Project command/write workflows | Epic 6 is metadata-only read behavior. Refresh/explain/current must prove they do not write. | Negative persistence assertions cover accidental commands, events, traces, or dual writers. |
| Final NFR PASS/CONCERNS/FAIL decision | Implementation evidence does not yet exist. | Run `nfr-assess` after the planned evidence artifacts are available. |
| Remote Pact broker/PactFlow verification | No broker connector, pacticipant landscape, or provider states are available. | Use versioned schemas/goldens; add a deterministic local Pact contract only if FrontComposer is confirmed as an independent HTTP consumer. |
| Webhook behavior | Epic 6 has no webhook requirement. | None; add a separate design if a webhook boundary is introduced. |

---

## Risk Assessment

Probability and impact use a 1–3 scale; score = probability × impact. A score of 6 or 9 is high.

### High-Priority Risks (Score ≥6)

| Risk ID | Category | Description | P | I | Score | Mitigation | Owner | Timeline / gate | Residual risk after mitigation |
| --- | --- | --- | ---: | ---: | ---: | --- | --- | --- | --- |
| R-601 | TECH | G4 platform-composition runner and evidence commands are absent, so acceptance evidence cannot run reproducibly. | 3 | 3 | **9** | Deliver a clean-checkout runner and fail-closed machine-readable evidence contract. | Platform / DevEx | Before 6.1 implementation | Low only after the required runner is green in CI. |
| R-602 | TECH | Required SDK query/projection/store/cursor seams have neither implementation nor tests; legacy suites may hide AD-14 incompatibility. | 3 | 3 | **9** | Test the exact compile-time surface, pure behavior, and platform-composed path before transports consume it. | Platform + Projects backend | 6.1 entry/merge gate | Medium until at least two consumers use the supported surface without adapters. |
| R-603 | DATA | Live tests lack deterministic authorized `tenant-a` access projection state, producing false denial/404 results. | 3 | 3 | **9** | Seed identity, access, projects, references, and watermarks through API/events with run-key isolation and readiness polling. | Test infrastructure + Identity | Before 6.1/6.5/6.6 live suites | Low if the factory is deterministic under parallel runs. |
| R-604 | SEC | Dual-principal or tenant-scoping defects could reveal another tenant's project existence or metadata. | 2 | 3 | **6** | Cross-tenant/principal negative matrices at SDK, transport, UI, and CLI boundaries; assert observationally safe denial. | Security / Identity + Projects backend | Every read story; release blocker | Low, with continued regression on every authorization change. |
| R-605 | DATA | Cursor reuse/tampering across scope could leak data or create missing/duplicate results. | 2 | 3 | **6** | Property/parameterized tests for tamper, version, scope, bounds, ordering, and gap/duplicate-free resume. | Projects backend | 6.1 merge gate | Low if codec version and scope remain centralized. |
| R-606 | DATA | Projection lag/failure could be falsely labeled `Complete`, corrupting context/resolution evidence. | 3 | 3 | **9** | Inject lag, missing partitions, rebuild, and store failure; assert truthful `Partial`/`Unavailable` plus watermark/provenance semantics. | Projects backend + Data platform | 6.1–6.4; AD-30 gate | Medium because production lag shapes require ongoing telemetry validation. |
| R-607 | OPS | Untested shadow comparison, cutover, and rollback could enable a divergent path without safe recovery. | 3 | 3 | **9** | Canonical comparison, zero-unexplained-mismatch cutover gate, authorized feature control, rollback-under-fault rehearsal. | Platform operations + Projects backend | Before any 6.7 cutover | Medium until a production-like rollback rehearsal completes. |
| R-608 | TECH | Current integration tests use placeholders or a fake state backend; live serialization/partition/concurrency defects may escape. | 3 | 3 | **9** | AppHost tests through real Dapr sidecars/store with retry/restart and persisted end-state assertions. | Integration test infrastructure | Before 6.1–6.4 complete | Low in covered store configuration; new store types require revalidation. |
| R-609 | DATA | Legacy identifiers/events may become unreadable or a hidden rewrite/dual writer may corrupt history. | 2 | 3 | **6** | Replay golden legacy streams; prove no source mutation/dual writer and equivalent supported snapshots before/after rollback. | Data migration + Projects backend | 6.7 cutover gate | Low after replay corpus represents all historical schema versions. |
| R-610 | SEC | Response, UI, CLI, logs, or traces could expose content, secrets, PII, claims, or internal IDs despite metadata-only rules. | 2 | 3 | **6** | Schema allowlists, adversarial denylist scans, status snapshots, and log/trace redaction tests. | Security + transport owners | 6.1–6.6; release blocker | Low if all output surfaces use shared contracts/redaction. |
| R-611 | PERF | No load evidence proves p95 <500 ms and hard max <1 s at 1,000 projects / 500 references. | 2 | 3 | **6** | Representative k6/equivalent load with default/max pages, parallel principals, and store/server telemetry. | Performance / SRE + Projects backend | Before Epic 6 release | Medium until concurrency and hardware profile are agreed. |
| R-612 | OPS | The 99.9% availability goal and graceful degraded-read recovery lack a measurement window and fault evidence. | 2 | 3 | **6** | Clarify SLI window/error budget; test sidecar/store/identity faults, recovery, health, and rebuild; retain dashboard evidence. | SRE / Operations | Clarify before 6.1; prove before release | Medium until production SLI history exists. |
| R-613 | BUS | FrontComposer live evidence is red and warning-console assets are missing; authorized reads and WCAG 2.2 AA may remain unevidenced. | 3 | 2 | **6** | Restore live fixture/assets; run semantic journeys, axe, keyboard/focus, and manual assistive-technology review. | FrontComposer + QA / Accessibility | 6.5 entry/completion gate | Low after the supported live path and manual protocol are green. |
| R-616 | OPS | External G3/G5/G6 dependencies can block UI, identity, or toolchain evidence late in the epic. | 3 | 3 | **9** | Named gate owners, executable entry checks, sprint tracking, and fail-closed release policy. | Program / platform leads | Every story entry; all green before release | Medium because schedules remain outside the epic team's control. |

### Medium-Priority Risks (Score 3–4)

| Risk ID | Category | Description | P | I | Score | Mitigation | Owner | Residual risk |
| --- | --- | --- | ---: | ---: | ---: | --- | --- | --- |
| R-614 | BUS | Thin CLI coverage may miss JSON/exit-code drift, unsafe errors, or web/API divergence. | 2 | 2 | 4 | Golden process contracts and a shared cross-transport scenario corpus. | CLI owner + QA | Low after contract and parity jobs are required. |
| R-615 | TECH | AD-32 response snapshots may drift across handlers and transports. | 2 | 2 | 4 | Shared versioned schemas/goldens and semantic parity checks. | Architecture + transport owners | Low while one canonical contract remains authoritative. |

### Low-Priority Risks (Score 1–2)

No evidence-based low risks were identified. Unspecified NFR thresholds are tracked as clarification blockers within R-611, R-612, R-613, and R-616 rather than being artificially scored low.

### Risk Category Legend

- **TECH:** Technical/architecture integration
- **SEC:** Security, authorization, and exposure
- **PERF:** Latency, throughput, or resource performance
- **DATA:** Integrity, consistency, and migration
- **BUS:** User/business impact
- **OPS:** Deployment, configuration, evidence, and monitoring

---

## NFR Planning

This section plans validation and later evidence consumption. It does not make final NFR status decisions.

| NFR Category | Requirement / Threshold | Risk Link | Planned Validation | Evidence Needed |
| --- | --- | --- | --- | --- |
| Security / tenant privacy | Authenticated and authorized reads; strict tenant isolation; safe denial; metadata-only output; no false evidence. Binary invariants. | R-604, R-605, R-610 | Cross-principal/tenant API and E2E matrices, cursor tamper/scope tests, schema allowlists, response/log/trace scans. | TRX/JUnit, redaction report, response/CLI goldens, sanitized failure traces. |
| Performance | p95 <500 ms at 1,000 projects / 500 references; each read <1 s; default page 50, cap 200. | R-611 | k6/equivalent representative load, warm/cold reads, page bounds, parallel principals, resource/query telemetry. | Load summary plus server/store telemetry. |
| Reliability / availability | 99.9% availability; truthful `Partial`/`Unavailable`; recovery without projection corruption. | R-606, R-608, R-612 | Sidecar/store/identity fault injection, retry/restart, rebuild/idempotency, health and persisted end-state tests. | Fault matrix, traces/logs, persisted snapshots, rebuild report, SLI export. |
| Scalability | Baseline 1,000/500 dataset with bounded pagination. | R-605, R-611 | Large-tenant cursor walk, parallel readers, soak/stress, resource-growth monitoring. | Cursor audit, load/stress/soak reports and resource graphs. |
| Maintainability / verifiability | AD-30 fail-closed evidence and executable supported SDK composition; ≥80% line/branch coverage for new/changed Epic 6 read code. | R-601, R-602, R-615, R-616 | Clean-checkout runner, schema/contract determinism, coverage and static analysis, repeated runs. | Evidence manifest, coverage and quality reports, versioned contract artifacts. |
| Accessibility / compliance | FrontComposer meets WCAG 2.2 AA. | R-613 | axe, semantic-role assertions, keyboard/focus, contrast/zoom/reflow, manual assistive-technology protocol. | axe JSON/browser report and signed manual record. |
| Compatibility / migration | Legacy IDs/events remain readable; no rewrite/dual writer; shadow read before reversible cutover. | R-607, R-609 | Legacy replay, canonical equivalence, mismatch telemetry, feature-control audit, cutover/rollback rehearsal. | Replay digest, diff report, telemetry, control audit and rollback report. |
| Response integrity | AD-32 Complete/Partial/Unavailable shapes remain consistent; explain/refresh/current do not persist unintended state. | R-606, R-615 | Shared schemas/goldens, parity corpus, persistence-negative tests. | Versioned snapshots and semantic-diff report. |

**Unknown thresholds and required clarifications:**

1. Performance concurrency, throughput, hardware profile, percentile sample size, and measurement window.
2. Availability measurement window/error budget, RTO, RPO, and maximum acceptable projection staleness.
3. Maximum tenant size, concurrent principals, growth rate, and soak duration.
4. Mutation, duplication, and flake-budget thresholds beyond the 80% code coverage gate.
5. Required browser/OS/screen-reader matrix and manual WCAG review protocol.
6. Shadow observation duration, response-contract deprecation policy, and rollback-time objective.
7. Acceptable authorization timing side-channel tolerance and token/session lifetime are not specified by Epic 6.

These values must be agreed before the corresponding release evidence is run; no value is inferred here.

---

## Entry Criteria

- [ ] Stories 6.1–6.7 acceptance criteria and AD-32 snapshots are agreed by Product, Architecture, Development, and QA.
- [ ] G4 platform-composition runner and evidence contract execute from a clean checkout.
- [ ] The exact supported SDK query/projection/store/cursor interfaces compile and can be hosted by the test runner.
- [ ] G5 identity/dual-principal integration and a deterministic authorized tenant/access data factory are available.
- [ ] Real AppHost/Dapr sidecar/state-store integration is provisioned for persisted-boundary scenarios.
- [ ] G3 FrontComposer composition and required static assets are available before Story 6.5 live testing.
- [ ] G6 toolchain prerequisites and required CI jobs are available.
- [ ] Shadow comparison, cutover, and rollback controls are observable and test-authorized before Story 6.7.
- [ ] UNKNOWN NFR parameters needed for the scheduled workload or manual evidence have named decision owners.

## Exit Criteria

- [ ] P0 pass rate is 100%; P1 pass rate is at least 95% with all failures triaged.
- [ ] No open critical/high defect affects security, tenant isolation, data truth, migration, or rollback.
- [ ] Every score-6/9 mitigation is complete with retained evidence; no absent gate is waived by a stub.
- [ ] New/changed Epic 6 read-path code reaches at least 80% line and branch coverage with explicit acceptance-criterion traceability.
- [ ] Cross-tenant leaks, unsafe metadata findings, and unexplained shadow mismatches are zero.
- [ ] Golden legacy replay, real persisted-boundary tests, cutover, and rollback rehearsal are green.
- [ ] Performance thresholds and page bounds are met using the agreed workload.
- [ ] WCAG 2.2 AA automated evidence and agreed manual protocol are green.
- [ ] Each NFR category has the specified artifact ready for `nfr-assess`; all release-relevant UNKNOWNs are resolved.

---

## Test Coverage Plan

> P0/P1/P2/P3 express priority and risk, not execution timing. PR/nightly/weekly placement is defined separately and depends on infrastructure cost.

### P0 — Critical

**Criteria:** Blocks a core read, security/data truth, or reversible migration; high risk; no safe workaround.

| Test ID | Requirement | Level | Risk Link | Owner | Atomic scenario |
| --- | --- | --- | --- | --- | --- |
| E6.1-U01 | 6.1 list | Unit | R-602, R-615 | DEV | Deterministic Complete snapshot; default page 50, cap 200. |
| E6.1-U02 | 6.1 pagination | Unit | R-605 | DEV | Valid cursor resumes with no gap/duplicate. |
| E6.1-U03 | 6.1 cursor security | Unit | R-604, R-605 | DEV | Tampered or scope-mismatched cursor is safely rejected. |
| E6.1-A01 | 6.1 list | API/service | R-601–R-603, R-608 | DEV + QA | Supported handler lists authorized persisted projections. |
| E6.1-A02 | 6.1 open | API/service | R-602, R-610 | DEV + QA | Supported handler opens exact metadata-only snapshot. |
| E6.1-A03 | 6.1 safe denial | API/service | R-604, R-610 | DEV + QA | Missing/denied/cross-tenant open is observationally safe. |
| E6.1-A04 | 6.1 stale state | API/service | R-606 | DEV + QA | Stale watermark yields Partial, never Complete. |
| E6.1-A05 | 6.1 failure state | API/service | R-606, R-612 | DEV + QA | Store/partition failure yields truthful Unavailable. |
| E6.1-A06 | 6.1 shadow read | API/service | R-607 | DEV + QA | Legacy/new list and open are canonically equivalent. |
| E6.2-U01 | 6.2 eligibility | Unit | R-602, R-609 | DEV | Only Active eligible conversation-start projects remain. |
| E6.2-A01 | 6.2 complete read | API/service | R-602, R-608 | DEV + QA | Supported authorized query returns persisted eligible subset. |
| E6.2-A02 | 6.2 safe denial | API/service | R-604, R-610 | DEV + QA | Missing/denied/cross-tenant request leaks nothing. |
| E6.2-A03 | 6.2 degraded read | API/service | R-606 | DEV + QA | Lag and failure map to Partial/Unavailable. |
| E6.3-U01 | 6.3 context | Unit | R-610, R-615 | DEV | Context snapshot contains allowlisted metadata only. |
| E6.3-U02 | 6.3 refresh | Unit | R-606 | DEV | Refresh recomputes without commands/events/mutation. |
| E6.3-U03 | 6.3 explain | Unit | R-606, R-610 | DEV | Explain has deterministic reason codes and no persisted trace. |
| E6.3-A01 | 6.3 supported composition | API/service | R-601–R-603 | DEV + QA | Context/refresh/explain run through supported auth composition. |
| E6.3-A02 | 6.3 safe denial | API/service | R-604 | DEV + QA | Denied/cross-tenant context is indistinguishable from missing. |
| E6.3-A03 | 6.3 degraded read | API/service | R-606, R-608 | DEV + QA | Lag/failure preserves truthful status and provenance. |
| E6.4-U01 | 6.4 resolution | Unit | R-602, R-615 | DEV | Exact NoMatch/Single/Multiple results are deterministic. |
| E6.4-U02 | 6.4 archive rule | Unit | R-609 | DEV | Archived candidates are excluded. |
| E6.4-A01 | 6.4 read-only | API/service | R-606, R-608 | DEV + QA | Resolution selects/persists nothing. |
| E6.4-A02 | 6.4 denial/degradation | API/service | R-604, R-606 | DEV + QA | Denied/stale/unavailable resolution is safe and truthful. |
| E6.5-C03 | 6.5 accessible controls | Component | R-613 | FrontComposer DEV + QA | Semantic names/roles, visible focus, keyboard operation. |
| E6.5-E01 | 6.5 live journey | E2E | R-603, R-608, R-613 | QA | Authenticated list→open→context→current on supported path. |
| E6.5-E02 | 6.5 safe UI denial | E2E | R-604, R-610 | QA | Denied user leaks nothing in DOM/network/console/trace. |
| E6.5-E03 | 6.5 WCAG | E2E + manual | R-613 | QA / Accessibility | WCAG A/AA automation and agreed manual protocol. |
| E6.6-C01 | 6.6 Complete CLI | Component | R-614, R-615 | CLI DEV + QA | Deterministic success JSON and exit code. |
| E6.6-C03 | 6.6 safe CLI denial | Component | R-604, R-610, R-614 | CLI DEV + QA | Identity/tenant denial has safe JSON and stable exit. |
| E6.7-A01 | 6.7 shadow equality | API/service | R-607 | DEV + QA | Canonical equality covers all statuses and paging. |
| E6.7-A02 | 6.7 mismatch | API/service | R-607, R-610 | DEV + QA | Mismatch emits redacted telemetry and blocks cutover. |
| E6.7-A03 | 6.7 cutover | API/service | R-607, R-616 | Platform + QA | Authorized cutover requires green entry criteria. |
| E6.7-A04 | 6.7 rollback | API/service | R-607, R-612 | Platform + QA | New-path fault can roll back to equivalent legacy reads. |
| E6.7-A05 | 6.7 compatibility | API/service | R-609 | Data + QA | Legacy streams rebuild/read with no rewrite or dual writer. |
| E6-X01 | Cross-cutting privacy | API/service + E2E | R-604, R-610 | Security + QA | Adversarial values expose no forbidden data anywhere. |
| E6-X03 | Reliability | API/service fault injection | R-606, R-608, R-612 | Platform + QA | Fault/recovery preserves truthful and persisted state. |
| E6-X05 | Evidence integrity | CI/evidence contract | R-601, R-616 | DevEx + QA | Clean runner rejects missing/stale/malformed/false evidence. |

**Total P0:** 37 scenario groups, ~95–145 hours.

### P1 — High

**Criteria:** Important common workflow or high-value defense whose failure has a contained operational workaround.

| Test ID | Requirement | Level | Risk Link | Owner | Atomic scenario |
| --- | --- | --- | --- | --- | --- |
| E6.1-A07 | 6.1 retry/restart | API/service | R-608, R-612 | DEV + QA | Restart/retry preserves cursor and persisted result semantics. |
| E6.2-A04 | 6.2 shadow read | API/service | R-607 | DEV + QA | Conversation-start legacy/new results are equivalent. |
| E6.3-A04 | 6.3 shadow read | API/service | R-607 | DEV + QA | Context/refresh/explain legacy/new results are equivalent. |
| E6.5-C01 | 6.5 Complete UI | Component | R-610, R-615 | FrontComposer DEV | Complete view models render supported metadata/status only. |
| E6.5-C02 | 6.5 degraded UI | Component | R-606, R-610, R-613 | FrontComposer DEV | Partial/Unavailable/denied guidance is distinct and accessible. |
| E6.6-C02 | 6.6 degraded CLI | Component | R-606, R-614 | CLI DEV | Partial/Unavailable JSON and exit codes are stable. |
| E6.6-E01 | 6.6 parity | E2E | R-614, R-615 | QA | One corpus is semantically equivalent across API/web/CLI. |
| E6.7-E01 | 6.7 transport continuity | E2E | R-607, R-614 | QA | Web/CLI remain stable through cutover and rollback. |
| E6-X02 | Performance | k6/equivalent | R-611 | SRE + QA | Agreed 1,000/500 workload meets latency thresholds. |
| E6-X04 | Scalability | Load/API | R-605, R-611 | SRE + QA | Parallel large-tenant cursor walk is bounded and correct. |
| E6-X06 | Response contracts | Unit/contract/CI | R-615 | Architecture + QA | Shared schemas/goldens detect transport drift. |

**Total P1:** 11 scenario groups, ~45–75 hours.

### P2 — Medium

**Criteria:** Secondary edge behavior or longer-running resilience coverage with lower immediate release probability.

| Test ID | Requirement | Level | Risk Link | Owner | Atomic scenario |
| --- | --- | --- | --- | --- | --- |
| E6.1-U04 | 6.1 cursor lifecycle | Unit | R-605 | DEV | Unknown cursor version/expiry is rejected safely. |
| E6.6-C04 | 6.6 invalid usage | Component | R-614 | CLI DEV | Malformed arguments have deterministic usage and no service call. |
| E6-X07 | Reliability/scalability | Weekly soak | R-606, R-612 | SRE + QA | Sustained rebuild/read load has no leak or irreversible lag. |

**Total P2:** 3 scenario groups, ~12–24 hours.

### P3 — Low / Exploratory

**Criteria:** Exploratory benchmark that characterizes future capacity rather than proving the stated release baseline.

| Test ID | Requirement | Level | Owner | Atomic scenario |
| --- | --- | --- | --- | --- |
| E6-X08 | Scalability breakpoint | Weekly stress | SRE + QA | Identify breaking point and verify graceful degradation beyond it. |

**Total P3:** 1 scenario group, ~4–8 hours.

---

## Execution Strategy

**Philosophy:** Run everything in PRs when the complete functional signal stays under 15 minutes; defer only expensive, long-running, destructive, or manual work. Use Playwright sharding/parallelism so browser volume does not become a reason to remove critical coverage.

### PR — Target ≤15 minutes

- All unit/component scenarios and schema/contract checks.
- Focused supported-handler composition and a small real persisted-boundary slice.
- Metadata/redaction checks and one authenticated browser/CLI P0 smoke.
- The required live job explicitly enables live execution; a default skip or missing prerequisite fails closed.

### Nightly

- Full AppHost browser/CLI and cross-principal/tenant matrix.
- Complete persisted-boundary, legacy replay, shadow/cutover/rollback, and recoverable fault suite.
- Multi-browser accessibility automation and representative performance smoke.

### Weekly

- Full load, stress, soak, and extended rebuild/recovery profiles.
- Production-like cutover/rollback rehearsal.
- Scheduled manual WCAG keyboard and assistive-technology protocol.

All asynchronous assertions poll explicit status/watermark signals within bounded deadlines. Hard waits are prohibited. Failures retain sanitized traces, logs, seed/run identifiers, canonical diffs, and persisted-state evidence.

---

## Resource Estimates

Ranges include test harnesses, isolated data factories, automation, evidence plumbing, and documentation. They exclude production feature work and external gate remediation.

| Priority | Scenario groups | Effort range | Complexity drivers |
| --- | ---: | ---: | --- |
| P0 | 37 | ~95–145 hours | New SDK/G4 harness, authorization data, real Dapr state, migration controls. |
| P1 | 11 | ~45–75 hours | Cross-transport parity, performance/load, UI state variants. |
| P2 | 3 | ~12–24 hours | Cursor lifecycle, CLI edges, soak harness. |
| P3 | 1 | ~4–8 hours | Exploratory stress characterization. |
| **Total** | **52** | **~156–252 hours** | Includes setup and evidence work. |

**Timeline:** ~5–8 weeks for one test engineer or ~3–5 calendar weeks for two, after required platform and identity dependencies are usable.

### Prerequisites

**Test data and fixtures:**

- Tenant-scoped project/reference factory with unique run keys, metadata-only adversarial values, and automatic cleanup.
- Identity/access-projection factory for authorized, denied, cross-tenant, expired/invalid identity, and parallel worker principals.
- Projection-status controller for Complete, Partial, Unavailable, lag, rebuild, and fault states.
- Golden legacy event-stream corpus spanning all historical schema/identifier versions.
- Shadow/cutover feature-control fixture with telemetry capture and rollback teardown.

**Tooling:**

- .NET 10/xUnit 3/Shouldly/NSubstitute for unit and service tests.
- Aspire AppHost, Dapr sidecars, and configured state store for real persisted-boundary tests.
- Playwright with existing API-first fixtures, `recurse`, axe, stable `data-testid`/ARIA selectors, and retained traces.
- k6 or equivalent for performance/load/stress/soak evidence.
- G4 clean-checkout evidence runner and manifest validator.
- Local Pact.js utilities only if an independent HTTP consumer/provider boundary is confirmed; remote MCP/broker access is not assumed.

**Environment:**

- Test environment with Keycloak/identity, Dapr, state store, server transports, FrontComposer, and CLI built from the same revision.
- CI secrets and service endpoints for test identities only; no production credential or content data.
- Controlled fault-injection permissions and feature-control authorization for non-production cutover/rollback rehearsal.

---

## Quality Gate Criteria

### Pass/Fail Thresholds

- **P0:** 100% pass, no exceptions.
- **P1:** ≥95% pass; no security, data-integrity, or cutover failure may be waived.
- **P2:** ≥90% pass or a triaged non-release defect with owner/timeline.
- **P3:** Result and breaking-point characterization documented; not a release pass-rate gate.
- **High-risk mitigations:** 100% complete with retained evidence before release.

### Coverage Targets

- New/materially changed Epic 6 read-path code: **≥80% line and branch coverage**.
- Acceptance criteria and authorization/status branches: **100% scenario traceability**.
- Security and tenant-isolation scenarios: **100% pass**.
- Cross-tenant leakage, unsafe metadata findings, and unexplained shadow mismatches: **zero**.

### Non-Negotiable Requirements

- [ ] No skipped, quarantined, or flaky P0/P1 result counts as evidence.
- [ ] Required jobs fail closed if G3/G4/G5/G6 prerequisites or evidence rows are missing.
- [ ] Real persisted-boundary proof exists; fake store tests alone are insufficient.
- [ ] Legacy replay, zero-unexplained-mismatch shadow evidence, authorized cutover, and rollback rehearsal are green.
- [ ] Performance meets p95 <500 ms and hard max <1 s under the agreed workload; page default/cap are 50/200.
- [ ] WCAG 2.2 AA automation and the agreed manual protocol are green.
- [ ] Every in-scope NFR has its planned artifact and all release-relevant UNKNOWN thresholds are resolved.
- [ ] Final NFR status remains deferred to `nfr-assess` when implementation evidence exists.

---

## Mitigation Plans

### Workstream A — Executable Supported Path (R-601, R-602, R-616)

**Strategy:**

1. Publish the exact SDK interfaces and compile-time contract tests.
2. Deliver the G4 clean-checkout runner and machine-readable evidence schema.
3. Add executable entry checks for G3/G5/G6 and reject substituted release evidence.

**Owners:** Platform / DevEx, Projects backend, program/platform leads  
**Timeline:** Before Story 6.1 implementation; reevaluate at every story entry  
**Status:** Planned  
**Verification:** E6.1-A01, E6-X05, clean-checkout CI evidence manifest.

### Workstream B — Deterministic Identity and Persisted State (R-603, R-608)

**Strategy:**

1. Create API/event-first identity, access, project, reference, and watermark factories.
2. Isolate parallel runs by tenant/principal/run key and clean created state.
3. Execute through AppHost, real Dapr sidecars, and the configured store; assert persisted state after retry/restart.

**Owners:** Test infrastructure, Identity, integration-test infrastructure  
**Timeline:** Before live Stories 6.1, 6.5, and 6.6; full harness before 6.1–6.4 completion  
**Status:** Planned  
**Verification:** E6.1-A01/A07, E6.5-E01, E6-X03.

### Workstream C — Isolation, Truth, and Metadata Safety (R-604, R-605, R-606, R-610)

**Strategy:**

1. Reuse one cross-principal/tenant negative corpus at the lowest effective boundary.
2. Centralize cursor scope/version and response allowlists.
3. Inject lag/failure and adversarial metadata; scan every transport and diagnostic surface.

**Owners:** Security/Identity, Projects backend/Data platform, transport owners  
**Timeline:** Merge gate for every read story; release blocker  
**Status:** Planned  
**Verification:** E6.1-U03/A03–A05, E6.3-A02/A03, E6.5-E02, E6.6-C03, E6-X01.

### Workstream D — Reversible Migration (R-607, R-609)

**Strategy:**

1. Define canonical comparison and redacted mismatch telemetry.
2. Replay all legacy schema/identifier goldens with no rewrite or dual writer.
3. Require zero unexplained mismatch, then rehearse cutover and rollback under injected fault.

**Owners:** Platform operations, Projects backend, Data migration  
**Timeline:** Before any Story 6.7 cutover  
**Status:** Planned  
**Verification:** E6.7-A01–A05 and E6.7-E01; replay/diff/control/rollback artifacts.

### Workstream E — Release NFR Evidence (R-611, R-612, R-613)

**Strategy:**

1. Resolve load, availability, recovery, staleness, and accessibility matrix unknowns.
2. Run representative load and controlled fault/recovery with telemetry.
3. Complete supported live FrontComposer WCAG automation and manual protocol.

**Owners:** SRE/Operations, FrontComposer, QA/Accessibility  
**Timeline:** Clarifications before relevant story execution; all evidence before Epic 6 release  
**Status:** Planned  
**Verification:** E6-X02–X04/X07/X08 and E6.5-C03/E03.

### Workstream F — Stable Consumer Contracts (R-614, R-615)

**Strategy:** Share schemas/goldens and one scenario corpus across SDK/API, FrontComposer, and CLI; normalize only transport-specific fields.

**Owners:** Architecture, CLI and FrontComposer owners, QA  
**Timeline:** Before first supported transport merge; continuous CI  
**Status:** Planned  
**Verification:** E6.6-C01–C04/E01 and E6-X06.

---

## Assumptions and Dependencies

### Assumptions

1. `ARCHITECTURE-SPINE.md` dated 2026-07-15 is the sole normative architecture; the root architecture file is only a superseded pointer.
2. Epic 6 remains metadata-only and does not add project command/write behavior.
3. AD-32 defines one canonical semantic response; parity normalization removes only transport-specific presentation fields.
4. Existing domain/server suites remain green and can supply legacy scenario inputs, but are not counted as new-path evidence.
5. A local Pact contract is conditional on a real independent HTTP consumer/provider boundary; no broker verification is presumed.

### Dependencies

1. G4 runner, SDK surface, and evidence schema — required before Story 6.1 implementation.
2. G5 identity/dual-principal behavior and deterministic access projection factory — required before live authorization tests.
3. AppHost/Dapr/state-store environment — required before persisted-boundary completion claims.
4. G3 FrontComposer composition and static assets — required before Story 6.5 live evidence.
5. Shadow comparison and authorized cutover/rollback controls — required before Story 6.7.
6. NFR decision owners — required before performance, availability, scalability, and manual accessibility evidence runs.

### Risks to Plan

- **External gates may slip.** Impact: lower-level tests can progress, but E2E/release evidence remains blocked. Contingency: use fakes only for unit/component development, track the blocked live gate explicitly, and make no release claim.
- **NFR load/SLI parameters may remain undecided.** Impact: tests could produce non-decisive measurements. Contingency: block the evidence run and record UNKNOWN rather than inventing thresholds.
- **Historical system-level test designs are stale.** Impact: old FR/NFR/epic mappings could be mistaken as current. Contingency: treat them as regression guidance only; current PRD/addendum/spine and this document control Epic 6.

---

## Follow-on Workflows (Manual)

- Run `*atdd` explicitly to generate red-phase P0 acceptance scaffolds after G4 and the supported SDK surface exist.
- Run `*automate` explicitly to expand P1/P2 coverage after implementation is testable.
- Run `nfr-assess` after performance, reliability, security, scalability, maintainability, accessibility, and migration evidence exists.
- Run traceability/gate analysis after automated test IDs and evidence artifacts are available.

---

## Approval

**Test Design Approved By:**

- [ ] Product Manager: _TBD_ — Date: _TBD_
- [ ] Tech Lead / Architect: _TBD_ — Date: _TBD_
- [ ] QA / Test Architect: _TBD_ — Date: _TBD_
- [ ] Platform / SRE owner for G3–G6: _TBD_ — Date: _TBD_

**Comments:** Draft pending owner assignment and resolution of the listed entry criteria and UNKNOWN NFR parameters.

---

## Interworking & Regression

| Service / component | Epic 6 impact | Required regression scope | Coordination |
| --- | --- | --- | --- |
| Platform SDK/runtime | Adds supported domain-query, projection-store, cursor, and composition seams. | SDK compile contracts, platform composition, runner/evidence validation. | Platform / DevEx |
| Projects domain/projections | Existing rules become inputs to supported reads and rebuilds. | Aggregate, inclusion/leakage, idempotency, rebuild, resolution, schema-golden suites. | Projects backend |
| Dapr/state store/AppHost | Becomes the real persisted read boundary. | Topology/configuration plus real sidecar/store retry/restart/end-state tests. | Integration infrastructure / SRE |
| Identity/access projections | Supplies dual-principal tenant authorization. | Auth, safe-denial, access-projection and cross-tenant matrices. | Identity / Security |
| Server transports | Must expose AD-32 statuses and safe responses through supported handlers. | Existing auth/endpoints plus schema and transport parity tests. | Server owner |
| FrontComposer | Moves authenticated reads to the supported path. | bUnit states, live journeys, axe/manual accessibility, no-leak traces. | FrontComposer / Accessibility |
| CLI | Adds authenticated stable JSON/exit contracts. | Existing CLI suite plus golden, denial, degraded-state, and parity tests. | CLI owner |
| Legacy read path/events | Remains readable during shadow migration and rollback. | Legacy suite, event-stream replay, canonical diff, no rewrite/dual-writer checks. | Data migration / Operations |

---

## Appendix

### Knowledge Base References

- `risk-governance.md` — risk classification and mitigation governance
- `probability-impact.md` — 1–3 probability/impact scoring
- `test-levels-framework.md` — lowest-effective test-level selection
- `test-priorities-matrix.md` — P0–P3 classification
- `nfr-criteria.md` — measurable NFR planning and evidence boundaries
- `test-quality.md`, `fixture-architecture.md`, `data-factories.md`, `network-first.md` — deterministic fixtures and API-first setup
- `api-request.md`, `auth-session.md`, `selector-resilience.md`, `test-healing-patterns.md`, `playwright-cli.md` — full-stack automation guidance
- Pact.js utility and MCP fragments — conditional local contract guidance; no remote broker evidence was available

### Related Documents

- [Current PRD](../planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md)
- [PRD addendum](../planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/addendum.md)
- [Epics and Story 6 acceptance criteria](../planning-artifacts/epics.md)
- [Normative architecture spine](../planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md)
- [Latest implementation-readiness report](../planning-artifacts/implementation-readiness-report-2026-07-17.md)
- [Sprint status](../implementation-artifacts/sprint-status.yaml)
- [Latest test summary](../implementation-artifacts/tests/test-summary.md)
- [Live AppHost operational-console verification spec](../implementation-artifacts/spec-5-12-live-apphost-operational-console-verification.md)

---

**Generated by:** BMad TEA Agent — Test Architect Module  
**Workflow:** `bmad-testarch-test-design`  
**Version:** 5.0 step-file workflow
