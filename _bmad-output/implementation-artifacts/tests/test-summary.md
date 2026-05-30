# Test Automation Summary

## Generated Tests

### API Tests
- [x] `tests/Hexalith.Projects.UI.Tests/Diagnostics/ProjectOperatorDiagnosticSourceTests.cs` - Generated-client diagnostic source coverage for 200, 400, 404, 500, and 503 outcomes, eventual-consistency request wiring, and safe feedback mapping without echoing unsafe API response content.
- [x] `tests/Hexalith.Projects.Client.Tests/ClientGenerationTests.cs` - Existing generated client query coverage for the operator diagnostics route used by the shell.
- [x] `tests/Hexalith.Projects.Contracts.Tests/Ui/ProjectVocabularyTests.cs` - FrontComposer projection metadata and shared vocabulary descriptor coverage.
- [x] `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs` - Existing metadata-only leakage harness coverage, including Story 5.3 shell projection artifacts.

### E2E Tests
- [x] `tests/Hexalith.Projects.UI.Tests/Components/ProjectDiagnosticHeaderTests.cs` - bUnit shell rendering tests for diagnostic header fields, non-color-only lifecycle badge, copyable tenant/project IDs, all console modes, and long project IDs.
- [x] `tests/Hexalith.Projects.UI.Tests/Rendering/ProjectRenderingPrimitiveTests.cs` - bUnit rendering primitive tests for required empty-state examples, feedback categories, feedback reason-code sanitization, and accessible status badges across lifecycle/reference/resolution/reason vocabulary.
- [x] `tests/e2e/support/page-objects/project-detail.page.ts` - Playwright page object selectors for diagnostic header, lifecycle badge, copyable IDs, empty state, and feedback regions.
- [x] `tests/e2e/specs/projects-console-shell.spec.ts` - Playwright shell-level selector contract scaffolding for header/navigation, empty states, feedback categories, and protected payload marker non-disclosure. These remain `test.fixme` until the AppHost/UI fixture can launch with authenticated operator context.

## Coverage
- Shell diagnostic header: tenant scope, project identity, lifecycle badge, warning count, last-updated timestamp, mode indicator, copyable IDs, long IDs.
- Shared vocabulary rendering: lifecycle, reference state, resolution result, and reason-code badges render visible labels plus accessible names.
- Empty states: no projects, no references, no audit events, data unavailable, access denied, and filter returned no results.
- Feedback: success, warning, error, fail-closed, and loading states; unsafe reason-code inputs are normalized before rendering.
- Diagnostic source: happy path plus validation, safe denial, unavailable, and unexpected server failure paths.
- API endpoints: no new Story 5.3 endpoint; Story 5.3 reuses `GET /api/v1/projects/{projectId}/operator-diagnostics` from Story 5.2.

## Validation
- [x] `dotnet build Hexalith.Projects.slnx -warnaserror /p:UseSharedCompilation=false /p:NuGetAudit=false -m:1 -v:minimal`
- [x] `tests/Hexalith.Projects.UI.Tests/bin/Debug/net10.0/Hexalith.Projects.UI.Tests -parallel none -noLogo` - 35 passed.
- [x] `tests/Hexalith.Projects.Contracts.Tests/bin/Debug/net10.0/Hexalith.Projects.Contracts.Tests -class "*ProjectVocabularyTests" -parallel none -noLogo` - 30 passed.
- [x] `tests/Hexalith.Projects.Client.Tests/bin/Debug/net10.0/Hexalith.Projects.Client.Tests -class "*ClientGenerationTests" -parallel none -noLogo` - 37 passed.
- [x] `tests/Hexalith.Projects.Tests/bin/Debug/net10.0/Hexalith.Projects.Tests -class "*NoPayloadLeakageTests" -parallel none -noLogo` - 50 passed.
- [x] `PATH="/tmp/hexalith-frontcomposer-shim:$PATH" pwsh tests/tools/run-frontcomposer-inspect-gate.ps1` - passed with 7 generated files and no warnings.
- [x] `git diff --check`
- [ ] `dotnet test Hexalith.Projects.slnx --filter "FullyQualifiedName~ProjectVocabulary|FullyQualifiedName~FrontComposer|FullyQualifiedName~ProjectsUI|FullyQualifiedName~NoPayloadLeakage|FullyQualifiedName~ClientGeneration" /p:UseSharedCompilation=false /p:NuGetAudit=false -m:1 -v:minimal` - blocked by VSTest socket permissions (`System.Net.Sockets.SocketException (13): Permission denied`); equivalent xUnit v3 in-process lanes passed.
- [ ] `npm run typecheck` - blocked because `tsc` is not installed in `tests/e2e`.
- [ ] `npm ci --ignore-scripts --offline` - blocked because the local cache is missing `zip-stream-4.1.1` and the local runtime is Node `v22.22.1` while `tests/e2e/package.json` requires Node `>=24.0.0`.

## Checklist Notes
- API tests generated: yes for the generated diagnostic source/client route used by the shell; no new API endpoint was introduced by Story 5.3.
- E2E tests generated: yes, bUnit executable rendering tests plus Playwright shell selector scaffolding.
- Standard framework APIs: yes, xUnit v3, bUnit, Shouldly, NSubstitute, and Playwright test APIs.
- Happy path coverage: yes, diagnostic header and generated-client success path.
- Critical error coverage: yes, 400, 404, 500, 503, fail-closed feedback, unavailable data, and leakage guards.
- Semantic locators: yes, shell selectors are stable `data-testid` contracts with visible labels and accessible names in the executable component tests.
- No hardcoded waits/sleeps: yes.
- Independent tests: yes; Playwright specs remain `test.fixme` until runtime fixtures exist.
