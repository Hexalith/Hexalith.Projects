---
title: 'Enforce canonical Create Project display name'
type: 'bugfix'
created: '2026-08-27'
status: 'in-progress'
review_loop_iteration: 0
followup_review_recommended: false
baseline_revision: '5638c038f888a5dc9035f22248cca50397cacd64'
baseline_commit: '5638c038f888a5dc9035f22248cca50397cacd64'
context:
  - '{project-root}/_bmad-output/project-context.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** `POST /api/v1/projects` accepts a canonical `projectMetadata` object whose `displayName` is missing or blank whenever the legacy top-level `name` is populated, even though the published canonical schema requires `projectMetadata.displayName`. This lets the direct server boundary diverge from its contract.

**Approach:** Make nested `projectMetadata.displayName` mandatory whenever `projectMetadata` is supplied, reject invalid canonical requests before command construction, and prove missing, blank, and valid nested names at the real HTTP endpoint while retaining the name-only legacy adapter.

## Boundaries & Constraints

**Always:** Keep authorization before protected-body validation; treat the presence of `projectMetadata` as the canonical discriminator; reject missing, null, empty, or whitespace-only canonical display names with metadata-only `400 ValidationFailure`, `details.rejectedField = projectMetadata.displayName`, and zero command submissions; use a valid nested display name as the submitted command name; preserve the existing legacy request with no `projectMetadata` and a nonblank top-level `name`; preserve current schema-version, metadata-class, name-conflict, and leakage behavior.

**Block If:** Correct behavior requires changing the OpenAPI document or generated client, changing the domain command contract, or choosing a compatibility behavior for canonical requests that is not established by the bundle and existing endpoint conventions.

**Never:** Edit the deferred-work ledger or bundle evidence; relax `ProjectMetadata` required fields; infer a canonical name from the legacy top-level field; alter proposal confirmation, domain events, projections, persistence, topology, UX, packages, dependencies, generated artifacts, or submodules.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Legacy compatibility | Authorized request omits `projectMetadata` and supplies nonblank top-level `name` | `202 AcceptedCommand`; command uses the legacy name | No error expected |
| Canonical valid | Authorized v1 request supplies valid metadata class and nonblank `projectMetadata.displayName` | `202 AcceptedCommand`; command uses the nested display name | No error expected |
| Canonical display name missing or null | Canonical object plus a valid top-level legacy `name`, but nested `displayName` is omitted or null | No command submission | Metadata-only `400`; reject `projectMetadata.displayName` without echoing payload data |
| Canonical display name blank | Canonical object plus a valid top-level legacy `name`, but nested `displayName` is empty or whitespace-only | No command submission | Metadata-only `400`; reject `projectMetadata.displayName` without echoing payload data |

</intent-contract>

## Code Map

- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` -- `CreateProjectAsync` currently validates canonical schema version and metadata class, then falls back from `ProjectMetadata.DisplayName` to top-level `Name`; add the nested display-name admission check near lines 590-604 before conflict checking and command construction. The private `ProjectMetadataHttpRequest`/`CreateProjectHttpRequest` records near line 2378 already preserve missing/null values for validation.
- `tests/Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.cs` -- focused HTTP-boundary suite. Existing canonical classification tests near lines 82-146 and request builders near lines 2120-2162 provide the reuse points; extend the builder so tests can omit or set nested `displayName` while still supplying a top-level fallback name, and assert exact safe errors plus zero submissions.
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` -- read-only contract evidence near lines 2726-2755: `ProjectMetadata.required` already contains `displayName` and `metadataClass`; this bundle must not edit it.
- `tests/Hexalith.Projects.Contracts.Tests/OpenApi/OpenApiContractSpineTests.cs` -- read-only regression near lines 390-428 already pins both required-field sets and the metadata vocabulary; use it and the fingerprint gate to prove the published schema remains stable.
- `.bmad-loop/runs/20260827-204026-a22d/bundles/create-project-metadata-validation/intent.md`, `_bmad-output/implementation-artifacts/deferred-work.md`, and `_bmad/render/**` -- workflow evidence only; never edit or include in the implementation delta.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` -- reject canonical requests whose nested display name is missing or whitespace before evaluating legacy-name compatibility or constructing `CreateProject`; keep the legacy adapter restricted to requests without `projectMetadata`.
- [x] `tests/Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.cs` -- add endpoint coverage for missing, null, empty, whitespace-only, and valid canonical display names, including a populated top-level name on rejection cases to prove fallback removal; retain explicit status, rejected-field, leakage, and submission-count assertions.

**Acceptance Criteria:**
- Given the unchanged published OpenAPI spine, when contract and fingerprint gates run, then `ProjectMetadata.displayName` remains required and generated artifacts remain current without any schema diff.
- Given the focused Create Project endpoint suite, when all legacy, canonical display-name, metadata-class, authorization-order, and conflict cases run, then all pass and rejected canonical names never submit a command.
- Given the final implementation delta, when scope is audited, then only the endpoint implementation, focused endpoint tests, and workflow-owned spec/result metadata changed; the deferred-work ledger and bundle evidence remain byte-identical.

## Spec Change Log

## Review Triage Log

## Design Notes

The discriminator is structural: `projectMetadata != null` selects the canonical path. Validate its `DisplayName` directly, then retain the existing conflict check for requests that also carry top-level `name`. Only the legacy path may derive the command name from `body.Name`.

## Verification

**Commands:**
- `aspire start --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj --non-interactive --format Json` followed by `aspire describe --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj --non-interactive --format Json` -- expected: obtain a known observable pre-edit resource baseline, or record the exact environment blocker without changing topology.
- `dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Release -warnaserror` then `./tests/Hexalith.Projects.Server.Tests/bin/Release/net10.0/Hexalith.Projects.Server.Tests -class Hexalith.Projects.Server.Tests.CreateProjectEndpointTests` -- expected: warning-free build and all focused endpoint tests pass.
- `dotnet build tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj --configuration Release -warnaserror` then `./tests/Hexalith.Projects.Contracts.Tests/bin/Release/net10.0/Hexalith.Projects.Contracts.Tests -class Hexalith.Projects.Contracts.Tests.OpenApi.OpenApiContractSpineTests` -- expected: the unchanged required-field contract passes.
- `pwsh ./tests/tools/run-openapi-fingerprint-gate.ps1` -- expected: generated artifacts match the unchanged Contract Spine.
