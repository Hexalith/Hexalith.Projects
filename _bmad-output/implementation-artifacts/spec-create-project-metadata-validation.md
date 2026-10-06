---
title: 'Enforce canonical Create Project display name'
type: 'bugfix'
created: '2026-08-27'
status: 'done'
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

## Authorized Build-Blocker Repair (2026-10-06)

The user explicitly authorized fixing the existing Folders API mismatch after being told this expands scope. This authorization overrides the original file-scope restriction solely for the following adapter repair and related verification; all other boundaries remain in force.

The build resolves `/home/administrator/.nuget/packages/hexalith.folders.client/1.0.0/lib/net10.0/Hexalith.Folders.Client.dll`. Its `IClient.GetEffectivePermissionsAsync` accepts folder ID, correlation ID, nullable freshness, and cancellation token. The checked-out Folders generated client accepts those arguments plus task ID before cancellation. Both were inspected directly. The adapter currently calls the latter signature, causing package-mode CS1501. Do not assume signature shape from the version label alone.

Repair through a small internal compile-time extension bridge in `src/Hexalith.Projects.Server/Folders/FoldersClientCompatibilityExtensions.cs`. Supply both supported cancellation-aware signatures. A five-argument extension forwards to the four-argument call, and a four-argument extension forwards to the five-argument call using correlation ID as task ID. C# native instance methods take precedence over extensions, so each resolved API terminates at its native overload, preserves cancellation, and carries task identity whenever the native client supports it. The existing endpoint call stays unchanged. No reflection/dynamic transport, generated-file edits, dependency updates, new public API, or submodule changes are needed.

Reuse `tests/Hexalith.Projects.Server.Tests/ProjectFolderDirectoryTests.cs`, particularly `ValidateSetProjectFolder_EffectivePermissions_MatchesClientTaskIdContract`, for behavioral verification. Validate against the actual package and the checked-out source API; retain the existing fail-closed ACL behavior. Run the full package-mode server test build and focused endpoint/folder suites, contract/fingerprint gates, and Aspire start/describe. Record any independent remaining environment blocker precisely.

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

- [x] `src/Hexalith.Projects.Server/Folders/FoldersClientCompatibilityExtensions.cs` -- add the internal bridge for both observed effective-permissions signatures, preserving correlation, supported task identity, cancellation, and fail-closed outcomes.
- [x] `tests/Hexalith.Projects.Server.Tests/ProjectFolderDirectoryTests.cs` -- run the existing ACL and client-task-identity regression coverage against the repaired adapter; prove the bridge compiles against both observed API shapes without changing client code or dependency pins.

**Acceptance Criteria:**
- Given the unchanged published OpenAPI spine, when contract and fingerprint gates run, then `ProjectMetadata.displayName` remains required and generated artifacts remain current without any schema diff.
- Given the focused Create Project endpoint suite, when all legacy, canonical display-name, metadata-class, authorization-order, and conflict cases run, then all pass and rejected canonical names never submit a command.
- Given the final implementation delta, when scope is audited, then only the endpoint implementation, authorized Folders compatibility bridge, focused endpoint/folder tests, and workflow-owned spec/result metadata changed; the deferred-work ledger and bundle evidence remain byte-identical.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| Blind-1: invalid canonical names without a legacy name are untested | medium | The generated canonical request model has no top-level name, but every invalid-name theory case supplies one. A guard restricted to mixed canonical/legacy requests would escape these assertions and return the wrong rejected field to canonical-only callers. Patched the theory to cover both shapes; all 10 cases passed. |
| Blind-2: matching canonical/legacy names with surrounding whitespace lack a success case | false | No bad outcome was found. The unchanged trimmed ordinal conflict comparison admits matching names, and the canonical discriminator selects the nested name directly. The existing valid canonical test verifies nested-name submission; changing these preserved comparison semantics is outside this fix. |
| Blind-3: non-string canonical display names lack explicit cases | false | No bad outcome was found. The HTTP record keeps `DisplayName` typed as a nullable string; `ReadFromJsonAsync` rejects non-string values through the existing `JsonException` catch and generic safe body-validation response before command construction. This change does not alter deserialization. |
| Blind-4: duplicate display-name properties lack explicit cases | false | No fallback bypass was found. Existing last-value deserialization semantics resolve the nullable string first; the new guard validates that final value and canonical requests never select `body.Name`. A final blank value is rejected; a final valid value is admitted subject to the preserved conflict check. |
| Verification-1: generated canonical-only callers lack invalid-name regression assertions | medium | Pre-verified regression gap: the generated `CreateProjectRequest` has metadata and schema version, without legacy name. Extend the existing theory for both request shapes. Grouped with Blind-1 and resolved by one patch; all 10 cases passed. |

Edge-case review returned no findings. No findings require a contract, dependency, topology, or deferred-ledger change.

### Authorized-repair review

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| Repair-Blind-1: the four-argument extension lacks execution coverage | false | No reachable product defect was identified. In package mode the forwarding call binds to the native four-argument method. In source mode the adapter's five-argument call binds directly to the native task-aware method. The second extension makes the compatibility class compile against that observed source API; no product caller enters its forwarding body. Both actual APIs compiled and their adapter/header tests passed. |
| Repair-Blind-2: cancellation is not explicitly exercised with a canceled token | false | No dropped cancellation or mapping defect was identified. The bridge passes the exact caller token to the native overload, and the unchanged adapter rethrows `OperationCanceledException` before generic unavailable mapping. The suggested additional cancellation case is optional coverage; the traced implementation preserves the required behavior. |
| Repair-Blind-3: the header regression does not assert every forwarded argument | false | No incorrect forwarding was identified. Both bridge signatures pass folder ID, correlation ID, and freshness unchanged to the native client, and use the caller token. The task-aware source adapter call is native and retains its original arguments. Extra literal header assertions would not correct a defect in this delta. |
| Repair-Blind-4: HTTP status regression cases fail at lifecycle rather than permissions | false | No changed exception mapping was identified. The bridge returns the native task directly without catching or wrapping errors; both reads remain in the same adapter try/catch, where the unchanged status mapper handles the native Folders API exception. The added bridge does not transform denial or unavailable outcomes. |
| Repair-Blind-5: malformed-success coverage stops at the lifecycle read | false | No malformed-permission admission path was identified. Native deserialization exceptions still enter the adapter's generic unavailable catch, while null/mismatched permission results are rejected by `ValidatePermissions`. The forwarding bridge adds no parsing or exception handling that changes these outcomes. |

The authorized-repair edge-case review returned no findings, and the verification-gap review reported no verification gaps. All review findings were triaged individually. No review patch or deferral remains.


## Design Notes

The discriminator is structural: `projectMetadata != null` selects the canonical path. Validate its `DisplayName` directly, then retain the existing conflict check for requests that also carry top-level `name`. Only the legacy path may derive the command name from `body.Name`.

## Verification

**Commands:**
- `aspire start --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj --non-interactive --format Json` followed by `aspire describe --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj --non-interactive --format Json` -- expected: obtain a known observable pre-edit resource baseline, or record the exact environment blocker without changing topology.
- `dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Release -warnaserror` then `./tests/Hexalith.Projects.Server.Tests/bin/Release/net10.0/Hexalith.Projects.Server.Tests -class Hexalith.Projects.Server.Tests.CreateProjectEndpointTests` -- expected: warning-free build and all focused endpoint tests pass.
- `dotnet build tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj --configuration Release -warnaserror` then `./tests/Hexalith.Projects.Contracts.Tests/bin/Release/net10.0/Hexalith.Projects.Contracts.Tests -class Hexalith.Projects.Contracts.Tests.OpenApi.OpenApiContractSpineTests` -- expected: the unchanged required-field contract passes.
- `pwsh ./tests/tools/run-openapi-fingerprint-gate.ps1` -- expected: generated artifacts match the unchanged Contract Spine.


## Verification Results

Run date: 2026-10-06. Implementation and tests were already committed when this run resumed. The endpoint requires a nonblank nested display name whenever `ProjectMetadata` is non-null, validates before command construction, and retains the name-only legacy adapter. This run added mixed-whitespace coverage and explicit `validation_error` category/code assertions in the focused endpoint test.

- `dotnet build tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj --configuration Release -warnaserror` exited 0 with zero warnings/errors.
- `./tests/Hexalith.Projects.Contracts.Tests/bin/Release/net10.0/Hexalith.Projects.Contracts.Tests -class Hexalith.Projects.Contracts.Tests.OpenApi.OpenApiContractSpineTests` exited 0: 26/26 passed.
- `pwsh ./tests/tools/run-openapi-fingerprint-gate.ps1` exited 0: 101/101 passed, with no generated-artifact changes.
- `aspire start --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj --non-interactive --format Json` exited 2. `aspire describe --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj --non-interactive --format Json` exited 0 and reported no AppHost running.
- `dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Release -warnaserror` exited 1; a serialized `-m:1` retry also exited 1. Both this build and Aspire startup fail at `src/Hexalith.Projects.Server/Folders/FoldersProjectFolderDirectory.cs(66,18)` with `CS1501: No overload for method 'GetEffectivePermissionsAsync' takes 5 arguments`. This existing dependency/API mismatch remains unresolved; no dependencies or topology were changed.
- Separate focused fallback: `dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Release --no-restore -m:1 -warnaserror -p:BuildProjectReferences=false` exited 0 with zero warnings/errors. This compiles current tests against prebuilt project dependencies and is not a full source build pass.
- The reused server DLL matches its portable PDB identity; that PDB's endpoint source SHA-256 equals the current `ProjectsDomainServiceEndpoints.cs` SHA-256: `A780E331D07B8CC714ADA437D14CD5B553ABBD1178CD5504FAEFE965E880186F`. The verification script is retained in the temporary sourceproof project at `/tmp/create-project-metadata-validation-sourceproof/`.
- `./tests/Hexalith.Projects.Server.Tests/bin/Release/net10.0/Hexalith.Projects.Server.Tests -class Hexalith.Projects.Server.Tests.CreateProjectEndpointTests` exited 0: 89/89 passed. A parent audit rerun added `-xml /tmp/create-project-metadata-endpoint-results.xml` and again passed 89/89, with zero failed, skipped, errors, or not-run tests. Its output is retained at `/tmp/create-project-metadata-endpoint-tests.log`.

Matrix audit: `PostProject_ValidCreate_Returns202AcceptedCommand` proves legacy compatibility; `PostProject_ValidCanonicalDisplayName_SubmitsNestedName` proves canonical submission; all five `PostProject_InvalidCanonicalDisplayName_ReturnsMetadataOnly400WithoutSubmitting` cases prove omitted/null/empty/spaces/mixed-whitespace rejection despite a populated legacy name. Authorization-order, metadata-class, and name-conflict tests also executed and passed.

Scope audit: only the focused test and this workflow-owned spec changed during this run. The existing endpoint implementation is unchanged. The OpenAPI file and deferred-work ledger retain their initial SHA-256 values, respectively `cf83ea0c7c13ca6df1b37ecba796fe124229b438f1200afc3fbeb42cc01b1b2e` and `6e7051d41d14fecd2ab65019c59f8871222d83075a8af53330dda72640a79ab9`. The historical bundle directory referenced by the spec is absent; it was not created or edited. Unrelated dirty files and submodules were preserved.

The original baseline is preserved. Its complete unified diff, including untracked files, is retained at `/tmp/create-project-metadata-full-oo0g2br_.diff` (62,977,737 bytes across unrelated historical work). Review uses `/tmp/create-project-metadata-review-ht7d4dkw.diff`, containing the endpoint and focused tests relative to that same baseline, so unrelated committed and user-owned work is excluded from this bugfix review. No index staging occurred.

Current HEAD observed for this run: `5cd1e828d7d9be4bcde387e634741a9b0e269f33`.


### Final verification after review patch

The invalid-display-name theory now executes all five value/presence cases both with and without a legacy name. The implementation subagent ran only that theory: 10/10 passed. Final parent verification rebuilt current tests with the documented focused fallback, revalidated the server/PDB/source match, and ran the entire endpoint class: **94/94 passed**, with zero failures, errors, skips, or not-run tests. The contract build remained warning-free, contract-spine tests passed **26/26**, and the fingerprint gate passed **101/101** with no generated-artifact diff.

Final command records are at `/tmp/create-project-metadata-final-verification.json`; output logs are `/tmp/create-project-metadata-final-{server-build,server-focused-build,source-proof,endpoint-tests,contract-build,contract-tests,fingerprint}.log`. Executed-test evidence is in `/tmp/create-project-metadata-final-endpoint-results.xml` and `/tmp/create-project-metadata-final-contract-results.xml`.

The full server build was retried after the patch using the original specified command and still exited 1 with the same Folders `CS1501` error, zero warnings, and one error. Aspire startup remains blocked by that compile failure. These gates have not passed; focused fallback evidence is separate. The workflow remains **in-review**, halted under its review-step failed-verification rule. No local commit or index staging was performed. Review found one regression coverage gap, now resolved, and no remaining endpoint correctness defect. Nothing was deferred; the deferred-work ledger is unchanged.


### Build-blocker repair authorization

The user answered “yes” to fixing the existing Folders build error. Implementation resumed with the bounded scope extension above. The historical verification failures remain recorded; fresh full-build and runtime evidence will be appended after repair.


### Successful verification after authorized build-blocker repair

The new internal `FoldersClientCompatibilityExtensions` compiles against both inspected native client APIs and preserves cancellation/correlation and task identity supported by each client. The endpoint and Folders adapter call sites remain unchanged. This fresh evidence supersedes the earlier full-build/Aspire blockers.

- `dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Release -warnaserror` passed with zero warnings/errors. No prebuilt-dependency fallback was used.
- The rebuilt `CreateProjectEndpointTests` passed **94/94**, and package-mode `ProjectFolderDirectoryTests` passed **15/15**, with zero failures/errors/skips.
- The contract build passed with zero warnings/errors; contract-spine tests passed **26/26** and the fingerprint gate passed **101/101** without any generated-artifact diff.
- `dotnet build /tmp/create-project-metadata-repair-source-client/SourceClientCompatibility.csproj --configuration Release -warnaserror` passed with zero warnings/errors. This temporary narrow project links the actual checked-out `HexalithFoldersClient.g.cs` and required serialization support, the unchanged adapter, the new bridge, and the existing Folders adapter tests. It does not use a synthetic API stub or modify the source module. The same **15/15** tests passed against this task-aware source client, including outgoing task-header and fail-closed assertions.
- `timeout 60s aspire start --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj --non-interactive --format Json` exited 0. Bounded `aspire wait security` and `aspire wait projects` commands exited 0; `aspire describe` reported all ten non-parameter resources Running/Healthy. Only the verification-owned AppHost was stopped; cleanup exited 0 and the final describe confirmed none running.

Exact commands, exit codes, test counts, and log paths are retained at `/tmp/create-project-metadata-repair-verification.json`. Test execution XML is retained at `/tmp/create-project-metadata-repair-{endpoint,folder,contract,source-folder}-results.xml`. Source-client harness setup issues were confined to the temporary project and resolved before the passing build; product dependencies were unchanged.

Protected-file snapshots at `/tmp/create-project-metadata-repair-protected-baseline.json` and `/tmp/create-project-metadata-repair-protected-final.json` match. The original deferred-ledger/OpenAPI hashes also still match. No submodule, dependency, generated artifact, bundle, or unrelated user change was edited. The only added production file is the internal compatibility bridge. There is no remaining verification blocker for the authorized scope.


### Completion

All implementation tasks and acceptance criteria are satisfied for the user-authorized scope. Final full source build, focused runtime-boundary tests, contract/fingerprint gates, both supported client-API checks, and Aspire startup/health/cleanup passed. The earlier halted state is historical and superseded by these successful checks. Status is `done`.

Repository/user instructions prohibit staging or committing unless the task explicitly requires those operations; this authorization was for the code repair and verification. No Git index mutation, local commit, push, branch operation, dependency update, or submodule update was performed. Existing user changes are preserved. Nothing was deferred.
