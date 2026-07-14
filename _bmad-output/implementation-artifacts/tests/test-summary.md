# Test Automation Summary

## Story 5.12 Live AppHost Verification (2026-07-14)

- AppHost startup: passed after aligning `Aspire.AppHost.Sdk`, `Aspire.Hosting.AppHost`,
  `Aspire.Hosting.Orchestration.linux-x64`, and `Aspire.Hosting` on `13.4.6`.
- Topology discovery: `projects-ui`, `projects`, and `security` reported distinct healthy routes via
  targeted `aspire wait` / `aspire describe`; no route was guessed or persisted.
- Disabled live lane: focused Chromium run passed 13 offline contracts and skipped 13 live cases
  before auth/seed fixture resolution.
- Live fail-fast matrix: missing `BASE_URL`, `API_URL`, `KEYCLOAK_URL`, or client ID, identical UI/API
  origins, and URL-embedded credentials each failed configuration collection immediately with no
  value disclosure.
- Focused live lane: all 13 requested operational-console cases executed; 13 offline contracts passed
  and 13 live cases failed. Ten live failures report project seed safe-denial `404` because the local
  `tenant-a` access projection is unavailable; three report missing warning-console UI/static-asset
  prerequisites.
- Full live lane: all 75 Chromium cases collected and ran; 19 passed and 56 failed. No live case was
  silently skipped. Counts and the per-spec partition were copied into the Story 5.12 artifact; raw
  token-bearing auth/output directories were removed after summarization and live traces are now disabled.
- Harness validation: `npm run typecheck` passed; real Keycloak token prefetch and a targeted
  authenticated safe-denial test passed using Aspire development TLS.

| Product specification | Total | Passed | Failed |
| --- | ---: | ---: | ---: |
| accessibility | 10 | 5 | 5 |
| audit | 2 | 0 | 2 |
| console shell | 3 | 0 | 3 |
| file reference | 5 | 0 | 5 |
| inventory detail | 5 | 0 | 5 |
| lifecycle | 5 | 2 | 3 |
| maintenance | 10 | 5 | 5 |
| operator read access | 4 | 1 | 3 |
| proposal | 6 | 1 | 5 |
| reference health | 4 | 0 | 4 |
| resolution trace | 5 | 2 | 3 |
| resolution | 9 | 1 | 8 |
| warnings dashboard | 7 | 2 | 5 |
| **Total** | **75** | **19** | **56** |

## Generated Tests

### API Tests
- [x] `tests/e2e/specs/projects-maintenance.spec.ts` - AppHost-dependent live-gated coverage for archive command submission, asserting the `POST /api/v1/projects/{projectId}/archive` call returns `202` before final projection confirmation.

### E2E Tests
- [x] `tests/e2e/specs/projects-maintenance.spec.ts` - AppHost-dependent live-gated journeys for archive, restore, relink, unlink, and re-evaluate maintenance previews through `/projects/{projectId}` Actions.
- [x] `tests/e2e/specs/projects-maintenance.spec.ts` - Runnable no-AppHost selector contract for the Story 5.9 maintenance panel stable selectors and safe text.
- [x] `tests/e2e/specs/projects-maintenance.spec.ts` - Runnable no-AppHost dry-run and confirmation gating for archive execution.
- [x] `tests/e2e/specs/projects-maintenance.spec.ts` - Runnable no-AppHost blocked dry-run coverage for restore invalid lifecycle, missing reference target, and file relink transient validation.
- [x] `tests/e2e/specs/projects-maintenance.spec.ts` - Runnable no-AppHost restore, unlink, and re-evaluate audit/safety semantics coverage.
- [x] `tests/e2e/specs/projects-maintenance.spec.ts` - Runnable no-AppHost mobile viewport visibility coverage for the panel controls.
- [x] `tests/e2e/support/page-objects/project-detail.page.ts` - Added `maintenanceDryRunRun` page-object locator for the shared dry-run button selector.

## Coverage

- API endpoints covered: `POST /api/v1/projects/{projectId}/archive` in the AppHost-backed live lane.
- UI features covered: maintenance panel action selection, stable selectors, preview state, dry-run state, confirmation gating, submit enablement, safe feedback codes, expected audit operation, and mobile visibility.
- Happy path coverage: archive dry-run to confirmation to succeeded state, restore preview for archived projects, unlink preview, and re-evaluate diagnostic preview.
- Critical error coverage: restore on active project, relink/unlink with missing reference, file relink requiring transient validation, and forbidden payload marker exclusion.
- Selector coverage: `maintenance-action-panel`, `maintenance-action-select`, `maintenance-action-state`, `maintenance-action-current-state`, `maintenance-action-proposed-state`, `maintenance-action-warning`, `maintenance-action-dry-run`, `maintenance-action-dry-run-run`, `maintenance-action-confirm`, `maintenance-action-submit`, `maintenance-action-feedback`, `maintenance-action-audit-event`, and per-action option selectors.

## Historical Story 5.9 Validation (superseded by Story 5.12)

- [x] `git diff --check -- tests/e2e/specs/projects-maintenance.spec.ts tests/e2e/support/page-objects/project-detail.page.ts`
- [x] `npm --prefix tests/e2e run typecheck` - superseded: passed during Story 5.12.
- [x] Focused Playwright execution - superseded: both offline and live lanes ran during Story 5.12; live failures are recorded above.

## Checklist Notes

- API tests generated: yes, where command submission is observable in the AppHost-backed lane.
- E2E tests generated: yes, with explicit live-gated journeys plus runnable no-AppHost selector/interaction coverage.
- Standard framework APIs: yes, Playwright `test`, `expect`, semantic roles, stable `data-testid` locators, and the existing page object.
- Happy path covered: yes.
- Critical error cases covered: yes.
- Proper locators: yes.
- Clear descriptions: yes.
- No hardcoded waits or sleeps: yes.
- Independent tests: offline contracts use isolated page content; live execution is serialized while shared cross-module fixture identifiers remain deferred work.
- Test summary created: yes.
- Tests saved to appropriate directory: yes.
- Summary includes coverage metrics: yes.
