# Sprint Change Proposal: Live AppHost Route and Epic 5 Playwright Closure

**Date:** 2026-07-14
**Project:** Hexalith.Projects
**Requested by:** Jerome
**Workflow mode:** Incremental
**Status:** Approved and implemented — verification completed with recorded product/fixture failures

## 1. Issue Summary

Epic 5 Story 5.11 completed its no-AppHost accessibility, responsive, keyboard, and selector-contract lane, but deferred the live operational-console browser cases until a running `projects-ui` route could be provisioned. The Epic 5 retrospective carries that deferred work as a release-readiness action: provision or document the route and run the remaining AppHost-backed Playwright cases.

The route was initially unavailable. Diagnostic attempts on 2026-07-14 established two independent blockers before endpoint assignment:

1. The umbrella build resolves Hexalith.Conversations against its uninitialized nested Commons path. This produces 220 compiler errors. Supplying the root-declared `references/Hexalith.Commons` checkout through `HexalithCommonsRoot` makes the AppHost build pass with zero warnings and errors.
2. The AppHost pinned `Aspire.AppHost.Sdk` / orchestration `13.3.5` while shared `Aspire.Hosting` and the CLI were `13.4.6`. The resulting old AppHost invocation passed `--tls-cert-file` to the newer DCP, which rejected it. Aligning the AppHost SDK to `13.4.6` resolved the startup failure.

The browser harness also requires correction after a route is available:

- `tests/e2e/playwright.config.ts` still describes a greenfield AppHost and guesses `https://localhost:7280`.
- `tests/e2e/README.md` still says the API, UI, and AppHost do not exist and recommends `dotnet run`.
- 57 `test.fixme` declarations across 13 product specifications are permanently disabled; parameterization expands them to 61 runtime live cases, and supplying `BASE_URL` alone cannot run them.
- Story 5.11's focused Chromium evidence is 13 passed no-AppHost checks and 13 skipped live checks.

Core problem at proposal time: the operational topology existed in source, but its live browser endpoint was neither provisionable nor documented through supported Aspire discovery, and the Playwright suite could not conditionally enable its live cases. Story 5.12 corrected those gaps and exposed the remaining product/fixture failures.

Issue type: post-implementation release-readiness verification and operational-tooling gap. Product behavior and MVP scope are unchanged.

## 2. Impact Analysis

### Epic and Story Impact

Affected epic: **Epic 5: Operational Console & Audit (CLI / MCP / Web)**.

- Keep completed Story 5.11 `done`; its historical no-AppHost evidence remains valid.
- Add Story 5.12, **Live AppHost operational-console verification**, to own the deferred route, harness, execution, and evidence work.
- Keep `epic-5` in progress while Story 5.12 remains open.
- Do not create or imply an Epic 6.
- No earlier Epic 5 acceptance criteria are weakened or reinterpreted.

Story 1.9 remains completed implementation history. Its topology intent is valid, but its live operational instructions require correction from direct `dotnet run` and manual port substitution to supported Aspire lifecycle and endpoint-discovery commands.

### Artifact Impact

**PRD:** No change. FR-21/FR-22 and the security/observability NFRs remain valid.

**Epics:** Add Story 5.12 under Epic 5 with the approved acceptance criteria below.

**Architecture:** No design change. Correct the development workflow command and make dynamic endpoint discovery explicit.

**UX:** No requirement change. WCAG 2.2 AA, responsive, keyboard, reduced-motion, semantic structure, and safe-denial verification remain the live-browser targets.

**Implementation and test artifacts:** Update the topology runbook, E2E README/configuration, live-spec gating, sprint tracking, Story 5.12 evidence, test summary, and retrospective action result.

### Technical Impact

- The AppHost invocation must use the root-declared Commons checkout without initializing nested submodules.
- The AppHost SDK/orchestration version must align with the already-shared Aspire `13.4.6` hosting and CLI stack. Generated cache binaries must not be hand-edited.
- `projects-ui` must reach ready state before its endpoint is discovered with Aspire; no port may be guessed.
- Playwright must fail fast when live mode is requested without a valid `BASE_URL`.
- Permanent `test.fixme` declarations must become auditable conditional live gates. Any remaining skip needs a concrete missing-route, missing-fixture, or deterministic-seeding reason.
- Authentication tokens, credentials, command payloads, setup text, and private filesystem data must not be written to evidence artifacts.

## 3. Recommended Approach

Recommended path: **Direct adjustment within Epic 5 by adding Story 5.12**, with one separately tracked upstream Conversations dependency-root correction.

Rationale:

- The product requirements and architecture spine remain viable.
- A dedicated follow-up story preserves completed Story 5.11 evidence while making the deferred live work visible and auditable.
- Supported Aspire endpoint discovery avoids hard-coded ports and aligns the runbook, architecture, and browser harness.
- The temporary `HexalithCommonsRoot` umbrella override is sufficient to attempt the requested run without modifying or initializing nested submodules. Durable sibling-root discovery belongs upstream in Hexalith.Conversations.
- Aligning the AppHost SDK to the existing shared/tooling version is safer and smaller than editing the user-level DCP cache directly.

Change classification: **Moderate**. The implementation is focused, but it adds an Epic 5 story, changes the live E2E execution model, and depends on a local toolchain correction plus an upstream dependency-layout follow-up.

Effort: **Medium**. Risk: **Medium**, concentrated in environment/toolchain compatibility, authenticated fixture readiness, and the breadth of the 57 live declarations.

Rollback is not recommended. Reverting completed Story 5.11 would not create a route or executable live lane. No MVP feature reduction is proposed.

## 4. Detailed Change Proposals

All five edits below were approved by Jerome on 2026-07-14 during Incremental review.

### Edit 1 — Add Story 5.12 to Epic 5

OLD:

```text
Epic 5 ends with completed Story 5.11. The retrospective route/test action has no dedicated story.
```

NEW:

```markdown
### Story 5.12: Live AppHost operational-console verification

As a platform test engineer,
I want the AppHost browser endpoint to be provisioned and the deferred operational-console
Playwright cases to be executable through an explicit live lane,
so that Epic 5 accessibility, responsive, keyboard, security, and cross-surface behavior has
recorded live-topology evidence or a reproducible route blocker.

Acceptance criteria:

1. AppHost startup uses root-declared sibling repositories only and never initializes nested submodules.
2. `aspire start`, `aspire wait projects-ui`, and `aspire describe` produce a ready, dynamically discovered `projects-ui` route; no fixed port is used.
3. The E2E harness requires `E2E_LIVE_APPHOST=1` and a valid `BASE_URL` for live execution, while the no-AppHost lane remains runnable.
4. Permanent AppHost `test.fixme` declarations become conditional live tests. Every retained skip has a concrete blocker.
5. The focused Epic 5 lane runs first, followed by all applicable AppHost-backed cases across the 13 product specifications.
6. Evidence records route metadata, commands, pass/fail/skip totals, and blocker reasons without secrets or payloads.
7. The AppHost is stopped through Aspire after the run.
```

Rationale: a new story preserves the completed Story 5.11 record and gives the release-readiness action explicit ownership.

### Edit 2 — Correct AppHost Route Documentation

OLD:

```text
Architecture and runbook use `dotnet run`; E2E guidance guesses https://localhost:7280;
endpoint discovery and Aspire-managed teardown are absent.
```

NEW:

```bash
aspire start \
  --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj \
  --format Json --non-interactive

aspire wait projects-ui --non-interactive
aspire describe --format Json --non-interactive
```

Documentation must derive `BASE_URL` from the discovered `projects-ui` HTTP endpoint and use `aspire stop --non-interactive` for teardown. It must also list the root Commons resolution requirement and compatible Aspire CLI/DCP bundle as pre-route prerequisites.

Affected artifacts:

- `_bmad-output/planning-artifacts/architecture.md`
- `docs/runbooks/projects-topology.md`
- `tests/e2e/README.md`

Rationale: this aligns operational instructions with the existing Aspire topology and prevents false results against a guessed endpoint.

### Edit 3 — Make Live Playwright Cases Executable

OLD:

```text
BASE_URL has a guessed default. Live cases are permanently test.fixme and cannot be enabled by route configuration.
```

NEW:

- Add `E2E_LIVE_APPHOST=1` as the explicit live-suite gate.
- Require and validate `BASE_URL` in live mode.
- Use a reserved non-routable URL for the no-AppHost selector-contract lane.
- Replace permanent live `test.fixme` declarations with normal tests guarded by a shared conditional live helper.
- Retain skips only for concrete unavailable product routes, fixtures, or deterministic seed paths.
- Run the focused Story 5.11 specifications first: framework smoke, accessibility, resolution trace, and warnings dashboard.
- Then run every applicable AppHost-backed test in the 13 product specifications.

Rationale: live execution must be opt-in and fail-fast, while no-AppHost contract checks remain deterministic.

### Edit 4 — Unblock AppHost Startup Safely

OLD:

```text
The umbrella build resolves Conversations through its empty nested Commons path, and the active DCP
bundle rejects the TLS flag before resources start.
```

NEW:

```bash
export HexalithCommonsRoot="$PWD/references/Hexalith.Commons"
```

- Use the root-declared Commons override for this umbrella AppHost run.
- Do not initialize or modify nested submodules.
- Record a separate upstream Hexalith.Conversations change for durable sibling-root discovery.
- Align `Aspire.AppHost.Sdk` with the shared `13.4.6` hosting stack so AppHost, orchestration, hosting, and CLI use one version.
- Start the AppHost, wait for `projects-ui`, and discover the endpoint.
- If DCP remains incompatible, stop and record exact versions and the rejected argument; do not hand-edit tool-cache files.

Rationale: this is the smallest safe path to a live run while preserving repository and toolchain ownership boundaries.

### Edit 5 — Record Sprint and Verification Evidence

OLD:

```text
Story 5.11 is done, Epic 5 is in progress, and the retrospective action has no dedicated sprint item.
Only the 13 passed / 13 skipped no-AppHost focused lane is recorded.
```

NEW:

- Add `5-12-live-apphost-operational-console-verification: ready-for-dev` under Epic 5.
- Keep Story 5.11 `done` and link its deferred evidence to Story 5.12.
- Create a Story 5.12 implementation artifact containing route metadata, lifecycle commands, Playwright commands, counts, and concrete blocker reasons.
- Update `tests/test-summary.md` and the Epic 5 retrospective action with the result.
- Set Story 5.12 to `done` only when live cases run with recorded results or the exact route blocker is reproducibly documented, matching the retrospective criterion.

Rationale: implementation history remains intact while the live evidence has one authoritative owner and result.

## 5. Change Navigation Checklist

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [x] | Story 5.11 deferred the live AppHost cases; the Epic 5 retrospective created the release-readiness action. |
| 1.2 Core problem | [x] | No provisioned route, stale route documentation, and permanently disabled live tests. |
| 1.3 Supporting evidence | [x] | Two startup attempts isolated the Commons resolution and AppHost SDK/DCP compatibility blockers; E2E inventory found 57 `test.fixme` declarations. |
| 2.1 Current epic viability | [x] | Epic 5 remains viable and in progress. |
| 2.2 Epic-level change | [x] | Add Story 5.12; keep completed Story 5.11 unchanged. |
| 2.3 Remaining epics | [N/A] | No future epic is affected. |
| 2.4 New/obsolete epics | [N/A] | No new epic is required. |
| 2.5 Epic order/priority | [x] | Story 5.12 is a release-readiness follow-up after Story 5.11. |
| 3.1 PRD conflict | [x] | No PRD change. |
| 3.2 Architecture conflict | [x] | Architecture intent is valid; only the development command and endpoint-discovery guidance are stale. |
| 3.3 UX conflict | [x] | No UX change; live verification remains required. |
| 3.4 Other artifacts | [x] | Runbook, E2E config/README, live specs, test summary, sprint status, and retro evidence were updated. |
| 4.1 Direct adjustment | [x] | Viable through one Epic 5 story and focused tooling/test changes. |
| 4.2 Rollback | [N/A] | Reverting completed work does not solve the route gap. |
| 4.3 MVP review | [x] | No scope reduction or PRD change. |
| 4.4 Recommended path | [x] | Story 5.12 direct adjustment with an upstream Conversations follow-up. |
| 5.1 Issue summary | [x] | Included above. |
| 5.2 Impact summary | [x] | Included above. |
| 5.3 Recommendation | [x] | Included above with effort, risk, and rationale. |
| 5.4 Action plan | [x] | Approved detailed edits define implementation sequence and evidence. |
| 5.5 Handoff plan | [x] | Developer, Test Architect, and sprint owner responsibilities defined below. |
| 6.1 Checklist completion | [x] | All applicable items addressed. |
| 6.2 Proposal accuracy | [x] | Runtime findings and counts are based on observed commands; the live route was discovered dynamically and torn down. |
| 6.3 User approval | [x] | Detailed edits and implementation were explicitly approved on 2026-07-14. |
| 6.4 Sprint status update | [x] | Story 5.12 and the retrospective action now own the execution evidence. |
| 6.5 Next steps | [x] | Provision/replay deterministic tenant access and finish missing operational-console UI prerequisites before release acceptance. |

## 6. Implementation Handoff

Change scope: **Moderate**.

Primary route: **Developer agent and Test Architect**, with the sprint owner recording Story 5.12.

Implementation sequence:

1. Add Story 5.12 and its sprint-status row.
2. Correct architecture, runbook, E2E README, and Playwright live-mode configuration.
3. Apply the root Commons override and align the AppHost SDK with the shared Aspire `13.4.6` stack.
4. Start the AppHost, wait for `projects-ui`, discover and validate its route.
5. Convert the focused live cases to conditional gating and run the Epic 5 closure lane.
6. Convert, triage, and run the remaining AppHost-backed cases.
7. Record counts, blockers, route metadata, and teardown evidence; update the test summary and retrospective action.
8. Run relevant TypeScript checks, focused Playwright verification, repository test gates proportional to changed files, and `git diff --check`.

Ownership:

- **Developer:** repository documentation, harness, gating, and AppHost invocation changes.
- **Test Architect:** live-case triage, execution, and evidence assessment.
- **Sprint owner:** Story 5.12 status and retrospective action traceability.
- **Hexalith.Conversations owner:** separate durable sibling-root discovery correction.

Success criteria:

- A supported Aspire command sequence either produces a ready, reachable `projects-ui` route or records a reproducible versioned route blocker.
- No live test depends on a guessed port or permanent unexplained `test.fixme`.
- The 13 focused live cases and all applicable remaining AppHost-backed cases have recorded results.
- Every retained skip has a concrete reason.
- Root-only submodule topology is preserved and the AppHost is stopped after verification.
- No evidence artifact contains tokens, credentials, payloads, or private content.

## Implementation Outcome

- AppHost build passed with zero warnings/errors and resolved AppHost/Hosting/Orchestration packages all at `13.4.6`.
- Targeted Aspire lifecycle commands discovered healthy, distinct `projects-ui`, `projects`, and `security` endpoints without fixed ports, then stopped the Projects topology.
- All 57 former permanent declarations now use the explicit live gate; parameterized execution produced 61 live runtime cases with no silent live skip.
- No-AppHost focused lane: 13 passed / 13 live skipped before fixture resolution.
- Focused live lane: 13 offline contracts passed / 13 live cases failed. Ten failures were deterministic project-seed safe-denial `404` from missing `tenant-a` access projection; three exposed missing warning-console UI/static-asset prerequisites.
- Full live lane: 75 Chromium cases ran, 19 passed and 56 failed. Results and blockers are recorded in Story 5.12 and the test summary.

## Approval

Detailed edits 1–5 and implementation were approved by Jerome on 2026-07-14.
