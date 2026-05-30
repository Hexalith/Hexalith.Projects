# Test Automation Summary

## Generated Tests

### API Tests
- [x] `tests/Hexalith.Projects.Server.Tests/ProjectConversationAssignmentEndpointTests.cs` - ConfirmProjectResolution success paths for source-to-target move and no-source link.
- [x] `tests/Hexalith.Projects.Server.Tests/ProjectConversationAssignmentEndpointTests.cs` - ConfirmProjectResolution validation failures for missing Idempotency-Key, route/body mismatch, malformed JSON, unknown members, duplicate candidates, non-ambiguous evidence, missing selected candidate, one-candidate evidence, and `sourceProjectId == projectId`.
- [x] `tests/Hexalith.Projects.Server.Tests/ProjectConversationAssignmentEndpointTests.cs` - ConfirmProjectResolution authorization/lifecycle failures for hidden source, archived target, and archived source.
- [x] `tests/Hexalith.Projects.Server.Tests/ProjectConversationAssignmentEndpointTests.cs` - ConfirmProjectResolution assignment failure mapping and "do not submit Projects command before assignment accepted" coverage.
- [x] `tests/Hexalith.Projects.Server.Tests/ProjectConversationAssignmentEndpointTests.cs` - ConfirmProjectResolution command submission outcome mapping after assignment acceptance.

### E2E Tests
- [x] `tests/e2e/support/helpers/projects-api-client.ts` - Added typed `confirmProjectResolution(...)` helper for `POST /api/v1/projects/{projectId}/conversations/{conversationId}/resolution/confirm`.
- [x] `tests/e2e/specs/projects-resolution.spec.ts` - Updated FR-14 scaffold to use the spine-backed confirm route, explicit `MultipleCandidates` evidence, mutation headers, and metadata-only response assertions.
- [x] `tests/e2e/specs/projects-resolution.spec.ts` - Added scaffolded E2E negative coverage for missing `Idempotency-Key` and non-ambiguous confirmation evidence.

## Coverage
- ConfirmProjectResolution endpoint success paths: 2/2 story-required orchestration variants covered by API tests.
- ConfirmProjectResolution endpoint critical validation/error paths: 14 cases covered by API tests.
- ConfirmProjectResolution black-box E2E/API route coverage: scaffolded in the existing Playwright resolution suite; tests remain `fixme` until seeded ambiguous-conversation fixtures are available.

## Validation
- [x] `git diff --check -- tests/Hexalith.Projects.Server.Tests/ProjectConversationAssignmentEndpointTests.cs tests/e2e/support/helpers/projects-api-client.ts tests/e2e/specs/projects-resolution.spec.ts`
- [ ] `/home/administrator/.dotnet/dotnet test tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --filter FullyQualifiedName~ProjectConversationAssignmentEndpointTests`
  - Blocked in this sandbox: first attempt failed with MSBuild out-of-process node pipe permission denial; single-process retry progressed but restore/audit failed with `NU1900` because nuget.org is unreachable under restricted network access.
- [ ] `npm run typecheck` from `tests/e2e`
  - Blocked in this workspace: `node_modules` is absent and `tsc` is not installed locally.

## Checklist Notes
- API tests generated: yes.
- E2E tests generated: yes, as Playwright API-route scaffold in the existing suite.
- Standard framework APIs: xUnit/Shouldly and Playwright API helper patterns.
- Happy path coverage: yes.
- Critical error coverage: yes.
- Semantic UI locators: not applicable; Story 4.4 adds an API mutation, not UI.
- No hardcoded waits/sleeps: yes.
- Independent tests: yes.
