# Test Automation Summary

## Generated Tests

### API / Source Tests
- [x] `tests/Hexalith.Projects.UI.Tests/Diagnostics/ProjectResolutionTraceSourceTests.cs` - Generated-client backed trace source coverage for conversation mode, attachment mode, deterministic attachment input normalization, safe validation, 400/404/503/API failure mapping, transport/deserialization failure mapping, cancellation propagation, outcome derivation, and metadata-only serialization.

### Component / Contract / Leakage Tests
- [x] `tests/Hexalith.Projects.UI.Tests/Components/ProjectDetailPageTests.cs` - bUnit coverage for Resolution tab selector stability, initial state, conversation trace, attachment trace, NoMatch, MultipleCandidates, Excluded, FailedClosed, exclusion evidence, long readable identifiers, validation feedback, and payload-leakage guards.
- [x] `tests/Hexalith.Projects.Contracts.Tests/Ui/ProjectResolutionTraceProjectionTests.cs` - descriptor metadata, contract version, shared-vocabulary, and forbidden-field coverage for transient trace contracts.
- [x] `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs` - metadata-only serialization checks for resolution trace descriptor, candidate, and exclusion rows.

### E2E Tests
- [x] `tests/e2e/specs/projects-resolution-trace.spec.ts` - Playwright fixme scaffolding for trace selector stability, keyboard path, conversation and attachment trace journeys, axe accessibility scan, and no payload leakage.
- [x] `tests/e2e/support/page-objects/project-detail.page.ts` - Resolution Trace Workbench selectors added to the existing Project detail page object.

## Coverage

- Query modes covered: conversation id and folder/file attachment ids.
- UI outcomes covered: Resolved, NoMatch, MultipleCandidates, Excluded, and FailedClosed.
- Error cases covered: empty conversation input, empty attachment input, mixed inputs, 400 validation, 404 safe denial, 503 data unavailable, generic API failure, transport/deserialization failure, and cancellation.
- Selector coverage: `project-resolution-trace-workbench`, mode/input controls, include archived, run button, feedback, outcome, input summary, candidate rows, candidate comparison, reasons, and exclusions.
- Leakage coverage: tenant id, correlation/task/trace history, transcript, prompt, file path/content, workspace, memory payload, secret/token, command/proposal body, raw ProblemDetails, candidate score/rank persistence boundaries.

## Validation

- [x] `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Projects.slnx -warnaserror --no-restore -m:1 -nr:false /p:UseSharedCompilation=false /p:NuGetAudit=false -v:minimal`
- [x] xUnit v3 in-process `Hexalith.Projects.UI.Tests` focused classes: 22/22 passed (dev pass; superseded by the full VSTest run below).
- [x] xUnit v3 in-process `Hexalith.Projects.Contracts.Tests` focused classes: 24/24 passed (dev pass; the story Debug Log's "59/59" referred to a wider focused set — both are superseded by the full VSTest run below).
- [x] xUnit v3 in-process `Hexalith.Projects.Tests` `NoPayloadLeakageTests`: 55/55 passed (dev pass; superseded by the full VSTest run below).
- [x] `pwsh tests/tools/run-frontcomposer-inspect-gate.ps1`
- [x] `git diff --check`
- [x] `dotnet test` VSTest lane (re-run 2026-05-30 in review with the sandbox local-socket constraint lifted): `Hexalith.Projects.Contracts.Tests` 152/152, `Hexalith.Projects.UI.Tests` 74/74 (81/81 after review auto-fixes), `Hexalith.Projects.Tests` incl. `NoPayloadLeakageTests` 574/574 — all green. These full-project counts are authoritative and supersede the focused in-process subset counts above.
- [ ] `npm run typecheck` in `tests/e2e` - blocked because `tsc` is not installed locally (`node_modules` absent).
- [ ] Playwright browser execution - not attempted because E2E dependencies/AppHost fixture are not installed/running; Story 5.6 specs remain `test.fixme` per current fixture convention.

## Checklist Notes

- API/source tests generated: yes, for the two compute-on-demand trace query modes and safe failure mapping.
- E2E tests generated: yes, as Playwright fixme scaffolding using existing fixtures and page-object patterns.
- Standard framework APIs: yes, xUnit v3, bUnit, NSubstitute, Shouldly, and Playwright.
- Happy path coverage: yes, conversation and attachment trace flows.
- Critical error coverage: yes, safe validation and failure mapping paths.
- Semantic locators: yes, stable `data-testid` selectors plus accessible labels and readable identifiers.
- No hardcoded waits/sleeps: yes.
- Independent tests: yes, each source/component test creates isolated fakes and trace results.
