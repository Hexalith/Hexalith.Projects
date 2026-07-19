---
title: 'Resolve CreateProject metadataClass enforcement'
type: 'bugfix'
created: '2026-07-19'
status: 'done'
review_loop_iteration: 0
baseline_commit: 'f601a31ee3be33fa41bc0e2b82f01790571b8873'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-14.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The canonical `POST /api/v1/projects` request schema requires `projectMetadata.metadataClass`, but the server currently ignores the nullable field and can submit a command for a non-conforming request. Proposal confirmation implements the same four-value policy privately, allowing the two boundaries to drift.

**Approach:** Enforce the existing OpenAPI classification vocabulary before direct-create command construction, extract one server-owned validator reused by direct creation and proposal confirmation, and add focused OpenAPI/server regressions while preserving the historical name-only compatibility request.

## Boundaries & Constraints

**Always:** Keep direct-create authorization before protected-body parsing; accept exactly `public_metadata`, `tenant_sensitive`, `credential_sensitive`, or `secret`; return metadata-only `400 ValidationFailure` with `details.rejectedField = projectMetadata.metadataClass` and no command submission for missing, null, blank, whitespace, case-variant, or unknown string classifications; retain both positive request shapes; keep the OpenAPI document and generated client byte-stable; preserve proposal-confirmation ordering and observable errors while replacing its private predicate.

**Ask First:** Halt if an existing consumer requires a canonical `projectMetadata` request without a valid classification; if correct handling requires changing OpenAPI/generated artifacts; or if this task must add field-specific diagnostics for non-string values or duplicate JSON properties rather than retaining generic malformed-body handling for those broader cases.

**Never:** Pull Story 7.1 Folder-first/Durable Task work into this fix; alter domain commands, events, projections, persistence, topology, UX, packages, or submodules; infer classification from user text; persist classification as secret-content authorization; relax the schema or proposal validation.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Legacy compatibility | Authorized unversioned top-level `name` request | `202 AcceptedCommand`; one create command | Existing validation remains unchanged |
| Canonical valid | Authorized v1 `projectMetadata` with any exact allowed tier | `202 AcceptedCommand`; one create command | No classification echo or persistence added |
| Canonical missing | `projectMetadata` omits or nulls `metadataClass` | `400`; zero create commands | Exact rejected-field path; metadata-only and leakage-safe |
| Canonical invalid string | Blank, whitespace, case variant, or unsupported tier | `400`; zero create commands | No submitted value echoed; leakage assertion passes |
| Malformed JSON/type | Non-string tier or duplicate property | Retain current malformed-body semantics in this bounded E-9 fix | Do not claim broader Story 7.1/addendum diagnostics are complete |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` -- direct-create authorization, parsing, compatibility branching, command admission, and ProblemDetails helpers.
- `src/Hexalith.Projects.Server/Queries/ProposeNewProjectEndpoint.cs` -- current private tier predicate consumed by proposal confirmation.
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` -- unchanged source of required fields and exact classification vocabulary.
- `tests/Hexalith.Projects.Contracts.Tests/OpenApi/OpenApiContractSpineTests.cs` -- focused contract-spine assertions.
- `tests/Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.cs` -- HTTP-boundary compatibility, rejection, leakage, and no-submission proof.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- carry-forward action status and green evidence record.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Projects.Server/SensitiveMetadataTierValidator.cs` -- add the single documented exact-value predicate in its own C# file.
- [x] `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` and `src/Hexalith.Projects.Server/Queries/ProposeNewProjectEndpoint.cs` -- validate canonical direct create before command construction and reuse the validator in proposal confirmation without changing its behavior.
- [x] `tests/Hexalith.Projects.Contracts.Tests/OpenApi/OpenApiContractSpineTests.cs` -- pin both required-field sets, the nested tier reference, and the exact four-value enum without editing the spine.
- [x] `tests/Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.cs` -- cover all matrix rows in scope, exact safe errors, no value echo, leakage safety, and zero submissions on rejection.
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- mark only this action done and record exact successful evidence after all gates pass.

**Acceptance Criteria:**
- Given the approved E-9 contract and unchanged OpenAPI spine, when focused contract and fingerprint gates run, then required fields, enum parity, and generated artifacts remain current.
- Given either endpoint uses metadata classification, when production code is inspected and tested, then both call the same validator and proposal-confirmation behavior is unchanged.
- Given any in-scope request matrix row, when it reaches the real HTTP server boundary, then its status, submission count, rejected-field metadata, and leakage behavior match the matrix.
- Given the final diff, when scope is audited, then it contains no schema, generated-client, domain, infrastructure, UX, dependency, or submodule change.

## Spec Change Log

## Design Notes

The canonical/legacy discriminator remains structural: a submitted `projectMetadata` object is canonical and requires `requestSchemaVersion: v1`; absence of that object preserves the historical top-level-name adapter. The new validator centralizes exact string vocabulary only. JSON token-shape and duplicate-property diagnostics are intentionally not broadened by this bounded correction.

## Verification

**Commands:**
- `aspire start --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj --non-interactive --format Json` followed by `aspire describe --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj --non-interactive --format Json` -- expected: current AppHost resources reach a known observable baseline, or the exact environment blocker is recorded.
- `dotnet build tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj --configuration Release -warnaserror` then `./tests/Hexalith.Projects.Contracts.Tests/bin/Release/net10.0/Hexalith.Projects.Contracts.Tests -class Hexalith.Projects.Contracts.Tests.OpenApi.OpenApiContractSpineTests` -- expected: focused OpenAPI tests pass.
- `dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Release -warnaserror` then `./tests/Hexalith.Projects.Server.Tests/bin/Release/net10.0/Hexalith.Projects.Server.Tests -class Hexalith.Projects.Server.Tests.CreateProjectEndpointTests` -- expected: direct-create server matrix passes.
- `./tests/Hexalith.Projects.Server.Tests/bin/Release/net10.0/Hexalith.Projects.Server.Tests -class Hexalith.Projects.Server.Tests.Queries.ProposeNewProjectEndpointTests` -- expected: proposal-confirmation shared-validator regressions pass.
- `pwsh ./tests/tools/run-openapi-fingerprint-gate.ps1` -- expected: generated artifacts match the unchanged spine.
- `dotnet restore Hexalith.Projects.slnx -p:HexalithCommonsRoot=/home/administrator/projects/hexalith/projects/references/Hexalith.Commons` then `dotnet build Hexalith.Projects.slnx --no-restore --configuration Release -warnaserror -p:HexalithCommonsRoot=/home/administrator/projects/hexalith/projects/references/Hexalith.Commons` -- expected: warning-free Release build.

## Verification Results

- AppHost baseline: the pre-edit start was blocked by a separately running `Hexalith.Memories` AppHost holding shared Debug outputs. A post-review rerun built successfully and started the dashboard, but the detached Projects AppHost disconnected before resource discovery, so no resource-readiness claim is made. The external AppHost was not stopped or altered; this bounded server/contract fix required no topology change.
- Focused Contracts OpenAPI Release lane: 26 passed, 0 failed, 0 skipped.
- Focused CreateProject endpoint Release lane: 81 passed, 0 failed, 0 skipped; all I/O matrix rows ran, including authorization-before-classification.
- Proposal-confirmation regression Release lane: 32 passed, 0 failed, 0 skipped.
- `pwsh ./tests/tools/run-openapi-fingerprint-gate.ps1`: 41 passed, 0 failed, 0 skipped; generated artifacts match the unchanged Contract Spine.
- Solution restore completed successfully.
- Warning-as-error Release solution build completed with 0 warnings and 0 errors.

## Suggested Review Order

**Validation boundary**

- Reject canonical invalid classifications after authorization and before command construction.
  [ProjectsDomainServiceEndpoints.cs:591](../../src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs#L591)

- Centralize the exact four-value wire vocabulary.
  [SensitiveMetadataTierValidator.cs:9](../../src/Hexalith.Projects.Server/SensitiveMetadataTierValidator.cs#L9)

- Reuse the same policy without changing proposal-confirmation error behavior.
  [ProposeNewProjectEndpoint.cs:609](../../src/Hexalith.Projects.Server/Queries/ProposeNewProjectEndpoint.cs#L609)

**Contract and HTTP evidence**

- Pin required fields, nested reference, and exact tier vocabulary without order sensitivity.
  [OpenApiContractSpineTests.cs:384](../../tests/Hexalith.Projects.Contracts.Tests/OpenApi/OpenApiContractSpineTests.cs#L384)

- Prove every allowed tier retains canonical acceptance.
  [CreateProjectEndpointTests.cs:87](../../tests/Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.cs#L87)

- Reject missing and invalid string tiers without submission or leakage.
  [CreateProjectEndpointTests.cs:116](../../tests/Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.cs#L116)

- Lock authorization-before-classification against protected-body oracle regressions.
  [CreateProjectEndpointTests.cs:151](../../tests/Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.cs#L151)

- Document scoped malformed-token and duplicate-property compatibility behavior.
  [CreateProjectEndpointTests.cs:182](../../tests/Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.cs#L182)

**Tracking and follow-up**

- Close the carry-forward action with exact green evidence.
  [sprint-status.yaml:178](sprint-status.yaml#L178)

- Defer the separate displayName enforcement gap explicitly.
  [deferred-work.md:56](deferred-work.md#L56)
