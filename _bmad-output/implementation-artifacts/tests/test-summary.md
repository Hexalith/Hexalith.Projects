# Test Automation Summary - Story 4.3 Resolve Project From Attachments

Workflow: `bmad-qa-generate-e2e-tests`
Date: 2026-05-30
Engineer: QA automation
Story file: `_bmad-output/implementation-artifacts/4-3-resolve-project-from-attachments.md`

## Test Framework Detected

- API / endpoint tests: xUnit v3 + Shouldly in `tests/Hexalith.Projects.Server.Tests` and `tests/Hexalith.Projects.Tests`.
- E2E tests: Playwright in `tests/e2e`.

## Generated Tests

### API Tests

- [x] `tests/Hexalith.Projects.Server.Tests/Queries/ResolveProjectFromAttachmentsTests.cs` - added `Resolve_TooManyAttachmentIds_ReturnsValidationProblem`, covering the 32-attachment cap and `400 validation_error` with `rejectedField=attachments`.

Existing Story 4.3 API coverage already present:

- [x] Folder attachment happy path returns `SingleCandidate` with `ProjectFolderMatched`.
- [x] File attachment happy path returns `SingleCandidate` with `FileReferenceMatched`.
- [x] Folder + file evidence can return `MultipleCandidates`.
- [x] No referenced project returns `NoMatch`.
- [x] `Idempotency-Key` is rejected as `idempotency_key`.
- [x] Invalid freshness is rejected.
- [x] Missing / malformed attachment identifiers return safe-denial `404`.
- [x] Read-model outage returns retryable `503 read_model_unavailable`.
- [x] Archived candidates are excluded by default and included when requested.
- [x] Cross-tenant references do not leak.
- [x] Response bodies pass payload-leakage checks across representative outcomes.

### E2E Tests

- [x] `tests/e2e/support/helpers/projects-api-client.ts` - added typed `resolveProjectFromAttachments(...)` helper for the spine-backed GET route with repeated `folderId` / `fileId` query params.
- [x] `tests/e2e/specs/projects-resolution.spec.ts` - added Story 4.3 Playwright scenarios for folder resolution, file resolution, multiple candidates, query validation, safe-denial, and payload-leakage assertions.
- [x] Updated existing resolution scaffolds from placeholder conversation routes to the spine-backed `GET /api/v1/projects/resolution/from-conversation` route shape.

The Playwright domain tests remain `test.fixme`, matching the existing E2E workspace convention until the AppHost exposes seeded folder/file reference fixtures.

## Coverage

- API endpoints: Story 4.3 endpoint covered for happy path, no match, multiple candidates, validation, safe-denial, read-model unavailable, archived filtering, tenant isolation, payload leakage, and max-count validation.
- UI features: none for Story 4.3.
- E2E workflows: scaffolded for the attachment-resolution user journey; unblocked when runtime seeded references are available.

## Validation

- `tests/Hexalith.Projects.Server.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.Tests -class Hexalith.Projects.Server.Tests.Queries.ResolveProjectFromAttachmentsTests -noLogo -noColor`
  - Passed: 13, Failed: 0, Skipped: 0.
  - Note: this uses the existing compiled assembly, so it validates the pre-existing class coverage but does not compile the newly added max-count test.
- `/home/administrator/.dotnet/dotnet test tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --filter FullyQualifiedName~ResolveProjectFromAttachmentsTests`
  - Blocked by restricted network restore to `https://api.nuget.org/v3/index.json` (`NU1301 Permission denied`).
- `npm run typecheck` from `tests/e2e`
  - Blocked because `tsc` is not installed in the workspace (`sh: 1: tsc: not found`).
- `git -c core.whitespace=blank-at-eol,blank-at-eof,space-before-tab,tab-in-indent,cr-at-eol diff --check -- <changed test files>`
  - Passed. The repository uses CRLF via `.editorconfig`.

## Checklist Validation

- [x] API tests generated where applicable.
- [x] E2E tests generated where UI/runtime workflow exists.
- [x] Tests use standard framework APIs (xUnit v3 / Shouldly, Playwright).
- [x] Tests cover happy path.
- [x] Tests cover critical error cases.
- [ ] All generated tests run successfully - blocked for newly edited sources by missing local TypeScript compiler and restricted NuGet restore.
- [x] Tests use semantic/API-level interaction patterns; no brittle UI selectors added.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent.
- [x] Test summary created.
- [x] Tests saved to appropriate directories.
- [x] Summary includes coverage metrics.
