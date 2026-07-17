---
workflowStatus: 'in-progress'
totalSteps: 5
stepsCompleted: ['step-01-detect-mode', 'step-02-load-context', 'step-03-risk-and-testability', 'step-04-coverage-plan']
lastStep: 'step-04-coverage-plan'
nextStep: '/home/administrator/projects/hexalith/projects/.agents/skills/bmad-testarch-test-design/steps-c/step-05-generate-output.md'
lastSaved: '2026-07-17'
mode: 'epic-level'
detectedStack: 'fullstack'
browserExploration: 'skipped-no-cli-or-live-target'
inputDocuments:
  - '_bmad/tea/config.yaml'
  - '_bmad-output/implementation-artifacts/sprint-status.yaml'
  - '_bmad-output/planning-artifacts/epics.md'
  - '_bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md'
  - '_bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/addendum.md'
  - '_bmad-output/planning-artifacts/architecture.md'
  - '_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md'
  - '_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-17.md'
  - '_bmad-output/test-artifacts/test-design-architecture.md'
  - '_bmad-output/test-artifacts/test-design-qa.md'
  - '_bmad-output/implementation-artifacts/tests/test-summary.md'
  - '_bmad-output/implementation-artifacts/spec-5-12-live-apphost-operational-console-verification.md'
  - 'project-context.md'
  - 'src/Hexalith.Projects.Conversations/project-context.md'
  - 'src/Hexalith.Projects.EventStore/project-context.md'
  - 'src/Hexalith.Projects.Folders/project-context.md'
  - 'src/Hexalith.Projects.FrontComposer/project-context.md'
  - 'src/Hexalith.Projects.Memories/project-context.md'
  - 'src/Hexalith.Projects.Parties/project-context.md'
  - 'src/Hexalith.Projects.Tenants/project-context.md'
  - '.agents/skills/bmad-testarch-test-design/resources/knowledge/risk-governance.md'
  - '.agents/skills/bmad-testarch-test-design/resources/knowledge/probability-impact.md'
  - '.agents/skills/bmad-testarch-test-design/resources/knowledge/test-levels-framework.md'
  - '.agents/skills/bmad-testarch-test-design/resources/knowledge/test-priorities-matrix.md'
  - '.agents/skills/bmad-testarch-test-design/resources/knowledge/test-quality.md'
  - '.agents/skills/bmad-testarch-test-design/resources/knowledge/fixture-architecture.md'
  - '.agents/skills/bmad-testarch-test-design/resources/knowledge/network-first.md'
  - '.agents/skills/bmad-testarch-test-design/resources/knowledge/data-factories.md'
  - '.agents/skills/bmad-testarch-test-design/resources/knowledge/overview.md'
  - '.agents/skills/bmad-testarch-test-design/resources/knowledge/api-request.md'
  - '.agents/skills/bmad-testarch-test-design/resources/knowledge/auth-session.md'
  - '.agents/skills/bmad-testarch-test-design/resources/knowledge/selector-resilience.md'
  - '.agents/skills/bmad-testarch-test-design/resources/knowledge/test-healing-patterns.md'
  - '.agents/skills/bmad-testarch-test-design/resources/knowledge/playwright-cli.md'
  - '.agents/skills/bmad-testarch-test-design/resources/knowledge/nfr-criteria.md'
  - '.agents/skills/bmad-testarch-test-design/resources/knowledge/pactjs-utils-overview.md'
  - '.agents/skills/bmad-testarch-test-design/resources/knowledge/pactjs-utils-consumer-helpers.md'
  - '.agents/skills/bmad-testarch-test-design/resources/knowledge/pactjs-utils-provider-verifier.md'
  - '.agents/skills/bmad-testarch-test-design/resources/knowledge/pactjs-utils-request-filter.md'
  - '.agents/skills/bmad-testarch-test-design/resources/knowledge/pact-mcp.md'
---

# Test Design Workflow Progress

## Step 1 — Mode Detection and Prerequisites

### Selected Mode

**Epic-Level Mode**

### Detection Evidence

- The user selected Create mode without specifying system-level or epic-level scope.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` exists, so the workflow's file-based detection rule selects Epic-Level Mode.
- The sprint status shows Epics 1–5 complete and Epic 6 as the next backlog epic.

### Prerequisite Assessment

- Epic and story requirements are available in `_bmad-output/planning-artifacts/epics.md`.
- The epic document contains explicit acceptance criteria for its stories.
- Architecture context is available in `_bmad-output/planning-artifacts/architecture.md` and the reconciled architecture spine beneath `_bmad-output/planning-artifacts/architecture/`.

**Result:** Epic-level prerequisites are satisfied. The exact epic scope and authoritative context will be resolved in Step 2.

## Step 2 — Context, Stack, and Existing Coverage

### Configuration and Scope

- Target epic: **Epic 6 — Authorized Project Reads on Supported Platform**, selected because it is the next backlog epic in sprint status.
- Stack profile: **full stack** — .NET 10/C# 14 backend and SDK seams, Dapr/EventStore persistence and projections, authenticated FrontComposer UI, CLI, and Playwright end-to-end tests.
- TEA options enabled: Playwright utilities, Pact.js utilities, Pact MCP mode, automatic browser selection, automatic test-stack detection, and P2 risk threshold.
- Epic 6 covers stories 6.1–6.7: supported list/open reads, conversation-start reads, context/refresh/explain, current-project resolution, authenticated UI reads, authenticated CLI reads, and shadow-read cutover/rollback.

### Authoritative Requirements and Architecture

- The current PRD and addendum define 24 functional requirements and 11 NFRs. Epic 6 binds FR2, FR5, FR12, FR13, FR16, FR17, FR18, FR20, and FR22, plus NFR1, NFR5, NFR8, NFR9, and NFR10.
- Quantified read targets include p95 under 500 ms for the median dataset, a hard maximum under 1 second, default page size 50, maximum page size 200, 99.9% availability, metadata-only output, and WCAG 2.2 AA.
- `_bmad-output/planning-artifacts/architecture.md` is a superseded pointer. The sole normative architecture is the 2026-07-15 `ARCHITECTURE-SPINE.md`.
- The controlling architecture decisions are AD-1, AD-14, AD-17, AD-19, AD-20, AD-25, AD-30, AD-32, and AD-34, with entry gates G3–G6 relevant to this epic.
- The 2026-07-17 readiness report concludes **READY** for story creation and planning, while preserving per-story entry gates and noting that 37 of 63 evidence rows depend on external tooling or platform readiness.

### Existing Verification Baseline

- The domain suites already cover aggregate behavior, context inclusion, leakage cases, projections, idempotency, rebuild behavior, current-project resolution, schema goldens, and tenant isolation.
- Server suites cover current legacy endpoints, authentication, context/setup/resolve flows, and operator reads.
- UI and Playwright suites provide bUnit coverage, authentication fixtures, API-first helpers, `recurse`, `data-testid` selectors, network-first patterns, and axe accessibility checks.
- Existing system-level test-design artifacts from 2026-05-25 remain useful as historical foundations, but they target the older FR1–FR22 / NFR1–NFR9 / Epic 1–5 baseline and are superseded where the current PRD or architecture spine differs.
- The latest live Chromium evidence is not green: 19 of 75 tests passed and 56 failed. The main blockers are missing deterministic `tenant-a` access-projection data and missing warning-console UI/static assets. The focused live slice recorded 13 passes and 13 failures.

### Epic 6 Coverage Gaps

- No production or test references were found for `IDomainQueryHandler`, `IAsyncDomainProjectionHandler`, `IReadModelStore`, `IReadModelBatchStore`, `IQueryCursorCodec`, `QueryCursorScope`, or `ReadModelWritePolicy`; the supported platform read path is therefore not covered by the legacy suites.
- Story 6.1 has partial behavioral coverage through legacy in-memory list/open paths, but not through supported SDK handlers, scoped opaque cursors, AD-32 response snapshots, or shadow-read equivalence.
- Stories 6.2–6.4 have strong legacy domain/server coverage, but require migration tests at the supported platform boundary.
- Story 6.5 has partial component and E2E coverage, but live authorized evidence is currently red and WCAG evidence must be regenerated against the supported read path.
- Story 6.6 has only thin CLI coverage and lacks authenticated supported reads, stable exit-code validation, and semantic parity with web output.
- Story 6.7 has no substantive shadow-read, reversible cutover, or rollback suite.
- `ProjectsIntegrationSkeletonTests` is explicitly a Tier-3 placeholder, while the Dapr projection-store tests use a fake state backend. There is no current persisted-state proof across the live sidecar/storage boundary.
- The evidence runner/tooling required by G4 is not present, so command-level evidence validation cannot yet execute. G3, G5, and G6 also remain external entry dependencies.

### Browser and Contract Discovery

- Browser exploration was skipped because `playwright-cli` is unavailable, no equivalent browser connector is exposed, and no live target is running. Static code, documented live-test evidence, and existing Playwright suites were used instead; no selector or route was invented from an unobserved live page.
- Pact guidance was loaded because this is a full-stack distributed system and Pact utilities are enabled. No configured SmartBear/Pact broker connector or project Pact landscape was available to query, so contract planning is limited to local consumer/provider seams and must not claim broker evidence.
- Webhook-specific fragments were intentionally not loaded because Epic 6 contains no webhook behavior.

**Result:** The context is sufficient to design the Epic 6 strategy. The missing SDK seams, live deterministic fixture, persisted boundary, cutover controls, and evidence runner are explicit testability risks to assess in Step 3.

## Step 3 — Risk and NFR Planning Assessment

Epic-level mode does not require the workflow's system-level architecture testability review. Testability constraints that can affect Epic 6 delivery are captured as risks below.

### Risk Register

Probability and impact use the 1–3 scale from the TEA risk model. Scores of 6 or 9 are high and require an explicit mitigation owner and gate.

| ID | Category | Risk event and evidence | P | I | Score | Mitigation and planned evidence | Owner | Timeline / gate |
| --- | --- | --- | ---: | ---: | ---: | --- | --- | --- |
| R-601 | TECH | The G4 platform-composition runner and evidence commands do not exist, so supported SDK acceptance criteria could be implemented without reproducible command-level proof. | 3 | 3 | **9** | Deliver the G4 runner first; make every Epic 6 verification command executable from a clean checkout and publish machine-readable evidence with fail-closed status. | Platform / DevEx | Before Story 6.1 enters implementation |
| R-602 | TECH | The required SDK seams (`IDomainQueryHandler`, projection/store interfaces, and cursor codec/scope) have no current implementation or tests; legacy coverage may conceal incompatibility with AD-14. | 3 | 3 | **9** | Add compile-time contract tests, focused unit tests, and platform-composed integration tests against the exact SDK surface before transport/UI work consumes it. | Platform + Projects backend | Story 6.1 entry and merge gate |
| R-603 | DATA | Current live tests lack a deterministic authorized `tenant-a` access projection, causing widespread false 404/denial results and preventing reliable end-to-end assertions. | 3 | 3 | **9** | Build an API/event-first tenant-scoped data factory that seeds identity, access projection, projects, references, and watermarks; verify readiness before UI/CLI navigation and clean up by unique run key. | Test infrastructure + Identity | Before Stories 6.1, 6.5, and 6.6 live suites |
| R-604 | SEC | A defect in dual-principal authorization, tenant scoping, or safe-404 behavior could expose another tenant's project existence or metadata. Legacy leakage coverage is strong, but the new SDK and transports are untested. | 2 | 3 | **6** | Run cross-tenant and cross-principal negative matrices at domain-query, API, UI, and CLI boundaries; assert identical safe-denial shape, no internal identifier, and no distinguishable timing/body detail. | Security / Identity + Projects backend | Required for every read story; release blocker |
| R-605 | DATA | Opaque cursors may be accepted across tenant, principal, filter, page-size, or snapshot scope, causing leakage, duplicate/missing results, or unstable continuation. | 2 | 3 | **6** | Property/parameterized tests for tampering, expiry/version, scope mismatch, page-size bounds, deterministic ordering, resume without gaps/duplicates, and safe invalid-cursor responses. | Projects backend | Story 6.1 merge gate |
| R-606 | DATA | Projection lag, stale watermarks, or failed rebuilds may be reported as `Complete`, creating false evidence and inconsistent context/resolution results. | 3 | 3 | **9** | Inject lag, missing partitions, rebuild-in-progress, and store failures; assert `Partial`/`Unavailable`, watermark/provenance semantics, no persisted explain trace, and no false `Complete`. | Projects backend + Data platform | Stories 6.1–6.4; AD-30 gate |
| R-607 | OPS | Shadow-read comparison, cutover, and rollback are not currently tested; a divergent new path could be enabled without a safe return to legacy reads. | 3 | 3 | **9** | Implement deterministic canonical comparison with mismatch telemetry; gate cutover on zero unexplained mismatches; test feature-control changes, rollback under load, and post-rollback semantic recovery. | Platform operations + Projects backend | Story 6.7 before any cutover |
| R-608 | TECH | Integration coverage stops at a placeholder or fake Dapr state backend, so serialization, partitioning, concurrency, sidecar configuration, and persisted end-state defects may escape. | 3 | 3 | **9** | Add AppHost-composed tests through real Dapr sidecars and the configured state store; assert persisted state and query output after process/retry boundaries, not only emitted events or mock calls. | Integration test infrastructure | Before Stories 6.1–6.4 are complete |
| R-609 | DATA | Legacy identifiers/events might not remain readable during migration, or a hidden rewrite/dual-writer path could corrupt historical data. | 2 | 3 | **6** | Replay golden legacy streams into the new projection; verify identical supported read snapshots, no source-event mutation, no dual writer, idempotent rebuild, and rollback readability. | Data migration + Projects backend | Story 6.7 cutover gate |
| R-610 | SEC | UI, CLI, or API responses could expose content, secrets, PII, raw authorization data, or internal identifiers despite the metadata-only constraint. | 2 | 3 | **6** | Schema allowlists and denylist scans at every transport; snapshot tests for Complete/Partial/Unavailable/safe-denial shapes; log/trace redaction tests and adversarial project/reference values. | Security + transport owners | Stories 6.1–6.6; release blocker |
| R-611 | PERF | No current load evidence demonstrates p95 under 500 ms and hard maximum under 1 second at 1,000 projects / 500 references, including pagination and stale-read states. | 2 | 3 | **6** | Add k6 or equivalent workload with representative indexed data, warm/cold runs, default/max pages, parallel principals, and latency/error thresholds; capture resource and query telemetry. | Performance / SRE + Projects backend | Before Epic 6 release readiness |
| R-612 | OPS | The 99.9% availability objective and graceful `Partial`/`Unavailable` behavior lack an executable measurement window and fault/recovery evidence. | 2 | 3 | **6** | Clarify measurement window/error budget; add store/sidecar/identity fault injection, health/telemetry checks, recovery/rebuild tests, and production SLI dashboard evidence. | SRE / Operations | Threshold clarification before 6.1; evidence before release |
| R-613 | BUS | FrontComposer live evidence is red and warning-console assets are absent; authorized read behavior and WCAG 2.2 AA could remain unevidenced even if backend reads work. | 3 | 2 | **6** | Restore deterministic live fixture and static assets; run authenticated semantic UI journeys, axe scans, keyboard/focus tests, and targeted manual screen-reader review against the supported path. | FrontComposer + QA / Accessibility | Story 6.5 entry and completion gates |
| R-614 | BUS | CLI coverage is too thin to detect unstable JSON, exit-code drift, unsafe errors, or semantic divergence from the web/API result. | 2 | 2 | 4 | Define golden JSON/error/exit contracts; execute the same scenario corpus through SDK/API, web view-model, and CLI; normalize only transport-specific fields before equivalence comparison. | CLI owner + QA | Story 6.6 merge gate |
| R-615 | TECH | AD-32 response shapes may drift across list/open, conversation-start, context/explain, UI, and CLI implementations, multiplying client-specific behavior. | 2 | 2 | 4 | Generate or share one response-schema contract, add schema/golden tests per status variant, and run semantic parity checks across supported transports. | Architecture + transport owners | Before first public transport merge; continuous CI |
| R-616 | OPS | External G3, G5, and G6 dependencies can block FrontComposer, identity, or toolchain evidence late in the epic even when Projects code is ready. | 3 | 3 | **9** | Track gate owner/status in sprint planning, define executable entry checks and fallback test doubles only for lower levels, and prohibit release claims based on substitutes for required live evidence. | Program / platform leads | Review at every story entry; all green before release |

### NFR Validation Plan

This is a planning assessment only. It does not assign implementation-time PASS/CONCERNS/FAIL decisions.

| NFR category | Requirement / threshold | Planned evidence | Unknowns or linked risk |
| --- | --- | --- | --- |
| Security and tenant privacy | All reads authenticated and authorized; strict tenant isolation; safe 404/denial; metadata-only output; no false evidence. These are binary invariants, not percentage targets. | SDK/API negative matrices; cross-tenant and cross-principal tests; cursor-scope/tamper tests; schema allowlists; log/trace secret scans; UI/CLI safe-error snapshots. | Token/session lifetime and response-timing side-channel tolerance are not specified. Track under R-604 and R-610 without inventing thresholds. |
| Performance | p95 < 500 ms for the median dataset of 1,000 projects and 500 references; every read < 1 second; default page size 50 and maximum 200. | k6/equivalent load profile; server and store telemetry; warm/cold query runs; page-size boundary tests; CI trend report. | Expected concurrency, throughput, hardware profile, and percentile sample/window are **UNKNOWN**. R-611. |
| Reliability / availability | 99.9% availability; stale or degraded reads must return truthful `Partial` or `Unavailable` results and recover without corrupting projections. | SLI dashboard, fault-injection tests, health checks, rebuild/idempotency tests, retry/process-restart integration tests, persisted end-state assertions. | Availability measurement window, error budget, recovery-time objective, recovery-point objective, and maximum acceptable projection staleness are **UNKNOWN**. R-606 and R-612. |
| Scalability | Functional baseline is 1,000 projects / 500 references with bounded pagination (50 default, 200 max). | Larger-tenant dataset profiles, cursor walk across all pages, parallel-reader tests, state-store/index resource metrics, load and soak runs. | Maximum supported tenant size, concurrent principals, growth rate, and soak duration are **UNKNOWN**. R-605 and R-611. |
| Maintainability / verifiability | AD-30 requires fail-closed evidence; architecture requires supported SDK composition and executable entry gates. No numeric coverage or duplication target is specified. | Clean-checkout build/test runner, contract/golden tests, coverage report by critical seam, deterministic repeated runs, static analysis, evidence manifest validation. | Minimum line/branch coverage, mutation threshold, duplication limit, and flake budget are **UNKNOWN**. R-601, R-602, and R-615. |
| Accessibility / compliance | FrontComposer must satisfy WCAG 2.2 AA. | Automated axe scan, semantic role/name assertions, keyboard-only navigation, focus order/visibility, contrast checks, zoom/reflow checks, and documented manual assistive-technology review. | Required browser/OS/screen-reader matrix and accepted manual-review protocol are **UNKNOWN**. R-613. |
| Compatibility / migration | Legacy identifiers and events remain readable; no rewrite and no dual writer; shadow reads precede reversible cutover and rollback. | Legacy-stream replay, canonical snapshot equivalence, mismatch telemetry, feature-control audit, cutover/rollback rehearsal, post-rollback reads from persisted state. | Allowed mismatch rate is implicitly zero for unexplained semantic differences, but cutover observation duration and rollback-time objective are **UNKNOWN**. R-607 and R-609. |
| Response integrity | AD-32 snapshots and Complete/Partial/Unavailable semantics remain consistent across supported transports; explain is read-only and does not persist traces. | Shared schemas/goldens, transport parity corpus, persistence-negative assertions, tampered/partial data cases, deterministic JSON snapshots. | Versioning/deprecation policy for response snapshots is **UNKNOWN**. R-606 and R-615. |

### Highest-Risk Mitigation Order

1. **Unblock executable proof:** complete G4 and establish the exact SDK seams (R-601, R-602).
2. **Make live state controllable:** deliver the deterministic tenant/access fixture and real persisted-boundary harness (R-603, R-608).
3. **Protect truth and isolation:** verify tenant/principal denial, cursor scope, projection status, and metadata-only shapes (R-604, R-605, R-606, R-610).
4. **Prove migration reversibility:** build legacy replay, shadow equivalence, cutover, and rollback evidence before enabling the new path (R-607, R-609).
5. **Close release NFR evidence:** performance, availability, accessibility, and external entry gates must be quantified and green before release claims (R-611, R-612, R-613, R-616).

**Step 3 result:** 16 risks identified; 14 are high (score 6 or 9). Planning may continue, but the high-risk entry and release gates above are mandatory mitigations, not optional test backlog.

## Step 4 — Coverage and Execution Plan

### Test-Level Allocation

- **Unit:** pure query rules, cursor codec/scope, response mapping, resolution, refresh/explain invariants, and CLI formatting. No network, sidecar, or browser.
- **Component:** bUnit FrontComposer states and in-process CLI command hosting. External read results are controlled at the supported boundary.
- **API/service:** supported SDK handlers and transports composed with authorization, projections, Dapr sidecars, and persisted state where the scenario requires the real boundary.
- **E2E:** authenticated AppHost journeys through the browser or CLI process. E2E proves only cross-boundary wiring and user-visible semantics; edge-case combinatorics stay below this level.

Existing legacy domain/server tests remain regression inputs. They are not counted as proof of the new supported SDK path; shared scenario data should be reused to avoid cloning the same behavior at every level.

### Coverage Matrix — Story 6.1: List and Open

| Scenario | Atomic behavior | Level | Priority | Requirement / risk trace |
| --- | --- | --- | --- | --- |
| E6.1-U01 | Authorized list maps one deterministic AD-32 `Complete` snapshot with stable ordering, default page 50, and cap 200. | Unit | P0 | 6.1; R-602, R-615 |
| E6.1-U02 | A valid scoped cursor resumes the next page with no gap or duplicate. | Unit | P0 | 6.1; R-605 |
| E6.1-U03 | Tampered or tenant/principal/filter/page-size-mismatched cursor is rejected with the defined safe error. | Unit | P0 | 6.1; R-604, R-605 |
| E6.1-U04 | Unknown cursor version or expired cursor is rejected deterministically without decoding details. | Unit | P2 | 6.1; R-605 |
| E6.1-A01 | Supported `IDomainQueryHandler` composition lists authorized projects from persisted projection state. | API/service | P0 | 6.1; AD-14; R-601–R-603, R-608 |
| E6.1-A02 | Supported handler opens one authorized project and returns the exact metadata-only snapshot. | API/service | P0 | 6.1; AD-32; R-602, R-610 |
| E6.1-A03 | Missing, denied, and cross-tenant open requests are observationally safe and reveal no project existence. | API/service | P0 | 6.1; R-604, R-610 |
| E6.1-A04 | Stale projection/watermark yields truthful `Partial`, never `Complete`. | API/service | P0 | 6.1; AD-30; R-606 |
| E6.1-A05 | Unavailable store/partition yields truthful `Unavailable` without fabricated data. | API/service | P0 | 6.1; AD-30; R-606, R-612 |
| E6.1-A06 | New and legacy readers produce canonical-equivalent list/open results for the same persisted state. | API/service | P0 | 6.1, 6.7; AD-17; R-607 |
| E6.1-A07 | Process restart and retry preserve cursor/result semantics and persisted read state. | API/service | P1 | 6.1; R-608, R-612 |

### Coverage Matrix — Story 6.2: Conversation-Start Subset

| Scenario | Atomic behavior | Level | Priority | Requirement / risk trace |
| --- | --- | --- | --- | --- |
| E6.2-U01 | Eligibility returns only Active conversation-start projects and excludes archived/ineligible records. | Unit | P0 | 6.2; R-602, R-609 |
| E6.2-A01 | Supported authorized query returns the Complete eligible subset from persisted state. | API/service | P0 | 6.2; R-602, R-608 |
| E6.2-A02 | Missing, denied, and cross-tenant requests use safe denial with no metadata leakage. | API/service | P0 | 6.2; R-604, R-610 |
| E6.2-A03 | Stale and failed projections map to `Partial` and `Unavailable` respectively. | API/service | P0 | 6.2; R-606 |
| E6.2-A04 | Supported and legacy conversation-start results are canonically equivalent during shadow read. | API/service | P1 | 6.2, 6.7; R-607 |

### Coverage Matrix — Story 6.3: Context, Refresh, and Explain

| Scenario | Atomic behavior | Level | Priority | Requirement / risk trace |
| --- | --- | --- | --- | --- |
| E6.3-U01 | Context includes only allowlisted metadata and produces the exact Complete snapshot. | Unit | P0 | 6.3; R-610, R-615 |
| E6.3-U02 | Refresh recomputes from read state without command emission, event append, or read-model mutation. | Unit | P0 | 6.3; R-606 |
| E6.3-U03 | Explain returns deterministic allowlisted reason codes and creates no persisted trace. | Unit | P0 | 6.3; R-606, R-610 |
| E6.3-A01 | All three operations execute through the supported authenticated/authorized composition. | API/service | P0 | 6.3; R-601–R-603 |
| E6.3-A02 | Denied/cross-tenant context operations are safe and indistinguishable from missing data. | API/service | P0 | 6.3; R-604 |
| E6.3-A03 | Lag and store failure produce truthful `Partial`/`Unavailable` snapshots with provenance/watermark rules. | API/service | P0 | 6.3; R-606, R-608 |
| E6.3-A04 | Supported and legacy context/refresh/explain results are canonically equivalent. | API/service | P1 | 6.3, 6.7; R-607 |

### Coverage Matrix — Story 6.4: Current-Project Resolution

| Scenario | Atomic behavior | Level | Priority | Requirement / risk trace |
| --- | --- | --- | --- | --- |
| E6.4-U01 | Parameterized inputs produce exactly `NoMatch`, `Single`, or `Multiple` with deterministic candidates. | Unit | P0 | 6.4; R-602, R-615 |
| E6.4-U02 | Archived candidates are always excluded from resolution. | Unit | P0 | 6.4; R-609 |
| E6.4-A01 | Supported resolution neither selects a project nor persists a trace/event/read-model change. | API/service | P0 | 6.4; R-606, R-608 |
| E6.4-A02 | Denied, stale, and unavailable resolution is safe and truthfully degraded. | API/service | P0 | 6.4; R-604, R-606 |

### Coverage Matrix — Story 6.5: FrontComposer Reads

| Scenario | Atomic behavior | Level | Priority | Requirement / risk trace |
| --- | --- | --- | --- | --- |
| E6.5-C01 | Complete list/open/context/current view models render only supported metadata and stable status cues. | Component | P1 | 6.5; R-610, R-615 |
| E6.5-C02 | Partial, Unavailable, and safe-denial states render distinct accessible guidance without leaking details. | Component | P1 | 6.5; R-606, R-610, R-613 |
| E6.5-C03 | All read controls and status regions have semantic roles/names, visible focus, and keyboard operation. | Component | P0 | 6.5; WCAG 2.2 AA; R-613 |
| E6.5-E01 | Authenticated user completes list → open → context → current journey against the supported live path. | E2E | P0 | 6.5; R-603, R-608, R-613 |
| E6.5-E02 | Cross-tenant/denied user receives safe UI behavior and no metadata in DOM, network body, console, or trace. | E2E | P0 | 6.5; R-604, R-610 |
| E6.5-E03 | Supported live pages satisfy automated WCAG A/AA checks and the defined manual keyboard/screen-reader protocol. | E2E + manual | P0 | 6.5; NFR10; R-613 |

### Coverage Matrix — Story 6.6: CLI Reads

| Scenario | Atomic behavior | Level | Priority | Requirement / risk trace |
| --- | --- | --- | --- | --- |
| E6.6-C01 | Authorized Complete result emits deterministic JSON and the documented success exit code. | Component | P0 | 6.6; R-614, R-615 |
| E6.6-C02 | Partial and Unavailable results emit stable JSON/status/exit combinations without stack traces. | Component | P1 | 6.6; R-606, R-614 |
| E6.6-C03 | Missing/invalid identity, denied, and cross-tenant requests emit safe JSON/errors and stable non-success exits. | Component | P0 | 6.6; R-604, R-610, R-614 |
| E6.6-E01 | One seeded corpus produces semantically equivalent supported API, web, and CLI results after transport-only normalization. | E2E | P1 | 6.6; R-614, R-615 |
| E6.6-C04 | Malformed arguments produce deterministic usage JSON/text and the documented usage exit code without contacting the read service. | Component | P2 | 6.6; R-614 |

### Coverage Matrix — Story 6.7: Shadow Read, Cutover, and Rollback

| Scenario | Atomic behavior | Level | Priority | Requirement / risk trace |
| --- | --- | --- | --- | --- |
| E6.7-A01 | Canonical comparison reports equality for Complete, Partial, Unavailable, safe-denial, and paged results. | API/service | P0 | 6.7; AD-17; R-607 |
| E6.7-A02 | Injected semantic mismatch emits redacted diagnostic telemetry and prevents cutover. | API/service | P0 | 6.7; AD-30; R-607, R-610 |
| E6.7-A03 | Authorized feature-control change cuts reads to the new path only after entry criteria are satisfied. | API/service | P0 | 6.7; R-607, R-616 |
| E6.7-A04 | New-path fault triggers or permits authorized rollback, after which legacy reads recover with equivalent semantics. | API/service | P0 | 6.7; R-607, R-612 |
| E6.7-A05 | Golden legacy event streams rebuild and read successfully with no source rewrite and no dual writer. | API/service | P0 | 6.7; R-609 |
| E6.7-E01 | Authenticated web and CLI journeys remain semantically stable through cutover and rollback toggles. | E2E | P1 | 6.5–6.7; R-607, R-614 |

### Cross-Cutting Risk and NFR Scenarios

| Scenario | Atomic behavior | Level / tool | Priority | Requirement / risk trace |
| --- | --- | --- | --- | --- |
| E6-X01 | Adversarial metadata values never expose content, secrets, PII, raw claims, or internal IDs in response, DOM, CLI, logs, or traces. | API/service + E2E scan | P0 | NFR security/privacy; R-604, R-610 |
| E6-X02 | Representative 1,000-project / 500-reference workload meets p95 <500 ms and max <1 s for default/max pages. | k6/equivalent + telemetry | P1 | NFR performance; R-611 |
| E6-X03 | Sidecar/store/identity faults and recovery preserve truthful status and persisted end state across retry/restart. | API/service fault injection | P0 | NFR reliability; R-606, R-608, R-612 |
| E6-X04 | Large-tenant, parallel-reader cursor walk completes without gaps, duplicates, cross-scope reuse, or unbounded resource growth. | Load/API | P1 | NFR scalability; R-605, R-611 |
| E6-X05 | Clean-checkout G4 runner executes required suites and rejects missing, stale, malformed, or falsely green evidence rows. | CI/evidence contract | P0 | AD-25, AD-30; R-601, R-616 |
| E6-X06 | Shared schemas/goldens detect semantic drift at every supported transport. If FrontComposer is an independent HTTP consumer, a deterministic local Pact consumer/provider contract is added; no broker result is claimed. | Unit/contract/CI | P1 | AD-32; R-615 |
| E6-X07 | Sustained rebuild plus reads shows no memory/resource leak, status corruption, or irreversible lag. | Weekly soak + telemetry | P2 | NFR reliability/scalability; R-606, R-612 |
| E6-X08 | Stress run identifies the supported breaking point and confirms graceful degradation beyond it. | Weekly stress | P3 | NFR scalability; R-611 |

### NFR Evidence Plan

| Category | Scenarios / tool | Evidence artifact for later `nfr-assess` | Current blocker or assumption |
| --- | --- | --- | --- |
| Security / tenant privacy | E6.1-U03, E6.1-A03, E6.2-A02, E6.3-A02, E6.5-E02, E6.6-C03, E6-X01 | TRX/JUnit results, redaction/denylist report, response/CLI goldens, retained failing traces | G5 identity and deterministic access fixture; timing tolerance remains UNKNOWN |
| Performance | E6-X02 | k6 JSON/HTML summary plus server/store resource and query telemetry | Concurrency, hardware, sample/window must be defined before the run |
| Reliability / availability | E6.1-A04/A05/A07, E6-X03, E6-X07 | Fault matrix, AppHost traces/logs, persisted-state snapshots, rebuild report, SLI dashboard export | Measurement window, RTO/RPO, and staleness threshold UNKNOWN |
| Scalability | E6.1-U02/U03, E6-X04, E6-X08 | Cursor audit, load/stress/soak reports, resource-growth graphs | Max tenant size, concurrency, and soak duration UNKNOWN |
| Maintainability / verifiability | E6-X05, E6-X06 | Clean-checkout evidence manifest, coverage report, static-analysis report, deterministic schema/contract artifacts | G4 missing; numeric mutation/flake/duplication targets UNKNOWN |
| Accessibility / compliance | E6.5-C03, E6.5-E03 | axe JSON, browser report, keyboard checklist, manual assistive-technology record | G3 and live fixture; browser/OS/AT matrix UNKNOWN |
| Compatibility / migration | E6.1-A06, E6.2-A04, E6.3-A04, E6.7-A01–A05, E6.7-E01 | Canonical diff report, mismatch telemetry, feature-control audit, legacy replay digest, rollback rehearsal report | Observation duration and rollback-time objective UNKNOWN |
| Response integrity | Story snapshot scenarios plus E6-X06 | Versioned schemas/goldens and cross-transport semantic-diff report | Snapshot version/deprecation policy UNKNOWN |

### Execution Strategy

- **PR (target ≤15 minutes):** all unit and component scenarios, schema/contract checks, focused supported-handler tests, a small real persisted-boundary slice, metadata/redaction checks, and one authenticated browser/CLI P0 smoke. Shard or parallelize rather than omit P0 coverage. The required live job explicitly enables the live gate; a default skip is not evidence.
- **Nightly:** full AppHost E2E matrix, all tenants/principals and status variants, complete persisted-boundary suite, golden legacy replay, shadow/cutover/rollback scenarios, fault injection, multi-browser accessibility automation, and representative performance smoke.
- **Weekly:** full load/stress/soak profiles, extended rebuild and recovery, large-tenant scaling, cutover/rollback rehearsal, and the scheduled manual accessibility protocol.

Failures retain sanitized traces, logs, seed/run identifiers, canonical diffs, and persisted-state evidence. Hard waits are prohibited; asynchronous projections use bounded polling against explicit watermarks/status signals.

### Resource Estimate

These ranges cover test harnesses, data factories, automated scenarios, evidence plumbing, and documentation. They exclude production feature implementation and remediation of external platform gates.

| Priority | Scenario groups | Estimated effort |
| --- | ---: | ---: |
| P0 | 37 | ~95–145 hours |
| P1 | 11 | ~45–75 hours |
| P2 | 3 | ~12–24 hours |
| P3 | 1 | ~4–8 hours |
| **Total** | **52** | **~156–252 hours** |

Expected test-engineering timeline: approximately **5–8 weeks for one engineer** or **3–5 calendar weeks for two engineers**, after G4/G5 and live fixture dependencies are available.

### Quality Gates

- P0 pass rate is **100%**; P1 pass rate is **at least 95%** with no security, data-integrity, or cutover failure waived.
- No skipped, quarantined, or flaky P0/P1 scenario counts as evidence. Required live jobs must fail closed when prerequisites are absent.
- All score-6/9 risks have completed mitigations and named evidence before release.
- New or materially changed Epic 6 read-path code maintains **at least 80% line and branch coverage**, while every acceptance criterion and every authorization/status branch has explicit scenario traceability.
- Cross-tenant leakage, unsafe metadata findings, and unexplained shadow-read semantic mismatches are all **zero**.
- Cutover requires green legacy replay, persisted-boundary evidence, and reversible rollback rehearsal.
- Performance evidence meets p95 <500 ms and max <1 s using the agreed workload; page size is 50 by default and never exceeds 200.
- WCAG 2.2 AA automated checks and the agreed manual protocol are green before Story 6.5 completion.
- Each in-scope NFR has an identified evidence artifact; all UNKNOWN thresholds are resolved before release readiness.
- Final NFR PASS/CONCERNS/FAIL decisions are deferred to `nfr-assess` after implementation evidence exists.

**Step 4 result:** 52 non-redundant scenario groups are planned across the lowest effective test levels, with PR/nightly/weekly execution, evidence artifacts, effort ranges, and release gates defined.
