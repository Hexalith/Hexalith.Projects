---
title: '7.1: Close remaining Story 7.1 gates (DW-100 AD-18 binding and ListProjectsV2 test parity)'
type: 'feature'
created: '2026-11-28'
status: 'done'
baseline_commit: '8c3119f47ae0a977d4c2b21e6353be814b760b29'
route: 'dispatch'
review_loop_iteration: 0
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-7-context.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The isolated Folder-first SDK writer slice (`spec-7-1-activate-a-project-with-exactly-one-authorized-folder.md`) is `done` and accepted at `540f3f65`, yet `DW-100` (Story 7.1 AD-18 single-Tenant binding on retained v1 routes) remains open because `HttpContextProjectTenantContextAccessor` and `ProjectsClaimsTransformation` still select the first matching Tenant claim on multi-Tenant principals, and `ListProjectsEnvelopePagingTests` lacks an explicit `IsCreationAbandoned` invisibility test for `ListProjectsV2`.

**Approach:** Enforce AD-18 single-Tenant fail-closed claim resolution across `HttpContextProjectTenantContextAccessor` and `ProjectsClaimsTransformation`, verify safe `404` rejection for multi-Tenant principals without verified single selection on retained v1 routes, add `ListProjectsV2` abandoned-creation invisibility coverage, and close `DW-100` in `deferred-work.md`.

## Boundaries & Constraints

**Always:** Keep the deployed writer on `ProjectsDomainProcessor` until Story 7.17. Keep shipping `src/` and `Hexalith.Projects.CI.slnx` package-mode compatible (`CI=true`, `UseHexalithProjectReferences=false`, 0 warnings/errors). Keep one C# type per file, CRLF preservation on existing CRLF files, and `validate_production_authority.py --validate-index` green (`[6, 7, 8]`).

**Never:** Do not select the first of multiple Tenant claims on any code path. Do not modify `7.1-folder-first-writer-capability.json`, `7.1-folder-first-writer-decision.json`, `sprint-status.yaml`, `validate_production_authority.py`, or the uncommitted local edit in `tests/Hexalith.Projects.SdkWriter/Hexalith.Projects.SdkWriter.csproj`.

**Decisions (human-confirmed 2026-11-28):**
- **Scope (`1A`):** Implement and verify `DW-100` (AD-18 single-Tenant binding) plus `ListProjectsV2` `IsCreationAbandoned` test coverage, and close `DW-100` in `deferred-work.md`. Leave the accepted `540f3f65` capability pin, `sprint-status.yaml`, and `validate_production_authority.py` untouched.
- **Uncommitted `SdkWriter.csproj` edit (`2A`):** Preserve `tests/Hexalith.Projects.SdkWriter/Hexalith.Projects.SdkWriter.csproj` untouched (do not edit, revert, or stage it).
- **`eventstore:current-tenant` membership rule (`3A`):** A single distinct `eventstore:current-tenant` claim binds only when it is contained in the primary identity's `eventstore:tenant` membership set (or raw `tenants` when `eventstore:tenant` is absent) whenever membership claims exist; conflicting `eventstore:current-tenant` or membership mismatch fails closed (`404`). Update `HttpContextProjectTenantContextAccessorTests.PrimaryIdentityRetainsCanonicalClaimPrecedence` accordingly.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Single Tenant claim (v1) | Primary authenticated identity has one distinct concrete Tenant (`eventstore:current-tenant`, `eventstore:tenant`, or raw `tenantId`/`tenant_id`/`tid`/`tenants`) | Binds that exact Tenant in `AuthoritativeTenantId` and `EventStoreClaimTransformEvidence` | N/A |
| Multi-Tenant principal with matching `eventstore:current-tenant` | Primary identity has `eventstore:tenant = ["tenant-a", "tenant-b"]` and single `eventstore:current-tenant = "tenant-b"` | Binds `"tenant-b"` | N/A |
| Multi-Tenant principal without `eventstore:current-tenant` | Primary identity carries `>= 2` distinct Tenant claims (`eventstore:tenant`, `tenants`, or conflicting `tenantId`/`tenant_id`/`tid`) and no `eventstore:current-tenant` | `AuthoritativeTenantId` is `null`; `GetClaimTransformEvidence` is malformed; retained v1 routes return safe `404` (`tenant_access_denied` / `resource_unavailable`) before body parsing | Fail closed; never select first claim |
| Mismatched or duplicate `eventstore:current-tenant` | Primary identity has `eventstore:current-tenant = "other"` not in `eventstore:tenant = ["tenant-a"]`, OR `>= 2` distinct `eventstore:current-tenant` claims, OR blank `eventstore:current-tenant` | Fails closed (`null` / malformed -> safe `404`) | Fail closed; no first-claim fallback |
| `ListProjectsV2` with abandoned reservation | Persisted `ProjectReadModel` with `IsCreationAbandoned == true` | Fails closed (`Unavailable`, empty items, `HasMore: null`) matching `ListProjects` v1 | No hidden `ProjectId` disclosure |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Projects.Server/HttpContextProjectTenantContextAccessor.cs:28-91` — Uses `FindFirst` across `eventstore:current-tenant`, `eventstore:tenant`, and `tenantId`; replace with one shared single-Tenant fail-closed resolver implementing Decision `3A` (matching `tests/e2e/support/helpers/tenant-access-readiness.ts:46-59` semantics).
- `src/Hexalith.Projects.Server/Authentication/ProjectsClaimsTransformation.cs:39-42,58-71` — `AddTenantClaims` currently uses `FindFirst` across `tenantId`, `tenant_id`, `tid` and returns before reading `tenants`; promote all distinct primary-identity tenant values across `tenantId`, `tenant_id`, `tid`, and `tenants` into `eventstore:tenant` without picking first-of-many.
- `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs:417-448` & `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs:2019-2033` — Already maps `null` Tenant or malformed claim evidence to safe `404` (`SafeDenial`) before body parsing. Reuse unchanged.
- `src/Hexalith.Projects.Server/Queries/ProjectReadQueryExecutor.cs:38-105` & `ListProjectsV2QueryHandler.cs:10-20` — Already enforces `!model.IsCreationAbandoned` via shared `ListCoreAsync`. Keep unchanged.
- `tests/Hexalith.Projects.Server.Tests/HttpContextProjectTenantContextAccessorTests.cs:76-94`, `ProjectsClaimsTransformationTests.cs`, `ProjectAuthorizationGateTests.cs`, `CreateProjectEndpointTests.cs`, and `Queries/ListProjectsEnvelopePagingTests.cs` — Update `PrimaryIdentityRetainsCanonicalClaimPrecedence` for Decision `3A` and add unit/route coverage for all I/O Matrix scenarios.
- `_bmad-output/implementation-artifacts/deferred-work.md:918-927` — Open `DW-100` ledger entry (`gate: 7-1-activate-a-project-with-exactly-one-authorized-folder`); mark `done` with verification evidence.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Projects.Server/Authentication/ProjectsClaimsTransformation.cs` -- update `AddTenantClaims` to collect and promote all distinct non-whitespace tenant values across primary-identity `tenantId`, `tenant_id`, `tid`, and `tenants` (never stopping at the first claim and preserving existing `eventstore:tenant` when already populated) -- ensures multi-Tenant tokens are never collapsed to one claim during transformation.
- [x] `src/Hexalith.Projects.Server/HttpContextProjectTenantContextAccessor.cs` -- replace `FindFirst` tenant lookups in `AuthoritativeTenantId` and `GetClaimTransformEvidence` with a single fail-closed resolver enforcing Decision `3A` (unique non-blank `eventstore:current-tenant` verified against `eventstore:tenant`/`tenants` membership when present, and no conflicting raw tenant claims; when `eventstore:current-tenant` is absent, require exactly one distinct non-blank tenant across `eventstore:tenant` or raw `tenantId`/`tenant_id`/`tid`/`tenants`, rejecting malformed/blank values) -- closes DW-100 AD-18 single-Tenant binding with zero first-claim selection.
- [x] `tests/Hexalith.Projects.Server.Tests/HttpContextProjectTenantContextAccessorTests.cs`, `ProjectsClaimsTransformationTests.cs`, `CreateProjectEndpointTests.cs`, and `Queries/ListProjectsEnvelopePagingTests.cs` -- update `PrimaryIdentityRetainsCanonicalClaimPrecedence` to include `"current"` in `eventstore:tenant` and add tests covering every I/O Matrix row (single-Tenant binding, multi-Tenant + matching `eventstore:current-tenant`, multi-Tenant without selection returning safe `404` on retained v1 routes with zero body bytes read, mismatched/duplicate/blank `eventstore:current-tenant`, and `ListProjectsV2` `IsCreationAbandoned` -> `Unavailable`) -- proves DW-100 removal conditions and `ListProjectsV2` parity.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md` -- update `DW-100` (`:918-927`) to `status: done 2026-11-28` with concise resolution and verification results -- closes the Story 7.1 AD-18 ledger item.

**Acceptance Criteria:**
- Given an authenticated principal with two or more distinct Tenant claims and no verified single `eventstore:current-tenant` binding, when retained v1 HTTP routes (create/list/open) are invoked, then `HttpContextProjectTenantContextAccessor` returns `null` / malformed evidence and the endpoint returns safe `404` (`tenant_access_denied` / `resource_unavailable`) without reading the request body or dispatching a command.
- Given a multi-Tenant principal with one `eventstore:current-tenant` claim that matches a member of `eventstore:tenant`, when retained v1 routes run, then that exact Tenant binds; when `eventstore:current-tenant` is absent from `eventstore:tenant` or duplicated with distinct values, it fails closed with `404`.
- Given a persisted `ProjectReadModel` with `IsCreationAbandoned == true`, when `ListProjectsV2QueryHandler` executes, then it returns `Unavailable` with empty items and `HasMore == null`.

## Implementation Notes

- The shared resolver uses the primary identity's canonical membership for explicit selections. Locally promoted claims retain their source so normalization preserves the original raw membership check.
- Bearer validation preserves malformed Tenant evidence from the signed payload when claim materialization would drop empty arrays or collapse duplicate keys. The retained route tests exercise that behavior through signed tokens.
- The v2 regression test first observes the seeded Project, then marks its persisted model abandoned and verifies that the handler returns an unavailable snapshot without rows, ProjectId, or paging continuation.

## Spec Change Log

## Review Triage Log

| Lens | Finding | Verdict | Route and evidence |
| --- | --- | --- | --- |
| Blind | Empty Tenant claim arrays disappear during JWT materialization | high | Patch. IdentityModel emits each array element as a claim. An empty declared membership set could be mistaken for an absent set. |
| Blind | Duplicate JSON Tenant properties collapse to one value | high | Patch. IdentityModel's payload dictionary keeps the last property value, losing conflicting selection evidence before the resolver runs. |
| Blind | Invalid structured raw membership can promote valid fragments | medium | Patch. The transformation intentionally collects valid non-whitespace values. A null-containing array or object-shaped membership was previously rejected as a unit and must stay indivisible. |
| Blind | Canonical selection accepts a single differently valued raw alias | false | Reject. Decision 3A and the named precedence test make native canonical membership authoritative for explicit selection. The resolver rejects distinct conflicting raw scalar aliases. |
| Blind | The v1 resolver accepts Tenant strings outside the public Gateway grammar | low | Reject as outside the approved v1 claim-binding scope. AD-27 governs public Gateway Tenant IDs. Additional Tenant-format validation changes the retained v1 compatibility boundary. |
| Blind | Missing or malformed idempotency key on create returns 400 before the Tenant gate | low | Reject as outside the approved change. The existing create contract handles the idempotency key before authorization and has a dedicated missing-key regression test. The 7.1 verification uses requests that reach the Tenant gate. |
| Blind | The paging test has no continuation in its control | low | Patch. Seed an earlier visible item and use a one-item page to verify that encountering an indexed abandoned entry suppresses the previously available continuation. |
| Blind | Verification results omit exact runner invocations | low | Reject as a spec-only review finding. Record the successful commands alongside the final verification results. |
| Edge | Explicit canonical selection can bind when raw plural membership excludes it | false | Reject. Decision 3A makes raw `tenants` the fallback when native canonical membership is absent. The source-mapped normalization test exercises this fallback. |
| Edge | A raw membership array containing null now produces canonical grants | medium | Patch with the malformed structured membership finding. The old transformation rejected the full null-containing array. |
| Edge | Normal projection excludes abandoned IDs from the index | low | Reject as outside the approved parity check. Normal projection already hides abandoned IDs. The matrix exercises the read guard when the index still points to an abandoned model, using the same behavior in both query versions. |
| Verification | Signed multivalued memberships lack retained-route coverage | medium | Patch. The endpoint matrix builds a principal directly. A signed canonical membership array verifies the actual bearer materialization and a later matching selection. |

## Verification

**Results (2026-11-28)**

- Package-mode CI solution, package-mode Server test project, and source-mapped Debug Server test project builds passed with 0 warnings and 0 errors.
- All six focused Server test classes passed in source-mapped Debug and package-mode Release (375 passed, 0 failed, 0 skipped in each). Test XML confirmed that all matrix cases ran and passed, including five retained-route cases, eight signed-token cases, and the abandoned-creation paging case.
- The bare Debug build reported source-mapping errors in `references/Hexalith.Conversations/Directory.Build.props:9`. The checked-in source preparation script enabled the clean Debug build.
- The first isolated-output Release run had one failure. The OpenAPI fixture needs a checkout layout, and the same 375-test run passed both from checkout output and from isolated output with solution and source links.
- `python3 tools/planning/validate_production_authority.py --validate-index` passed with `[6, 7, 8]`.
- The mapped AppHost built with 0 warnings and 0 errors. `KeycloakPersistent=false aspire start --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj --isolated --non-interactive` exited 2 before resource composition because `Projects:DevelopmentRouteCatalog` requires owner custody for the selected Redis backend.
- Follow-up at Projects `e2490842fffba312e1d6caf13cc883ce4e828bde`: the [disposable ordinary Debug qualification](../../module/README.md#disposable-ordinary-debug-qualification) passed using `run_live_project_fixtures.py --ordinary-only` under Platform's run-owned issuer with fresh Redis and private catalog custody. All 54 launcher controls passed. Aspire reported the ordinary resources healthy. The native test verified a shared committed catalog, persisted delivery through the named SDK callback, a matching Project query, and unchanged state after credential refusals. The runner verified cleanup and preservation of preexisting resources.

**Reproduction from the repository root**

```bash
run-with-temp bash -euo pipefail -c '
project=tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj
export MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0
CI=true dotnet build Hexalith.Projects.CI.slnx -c Release -p:UseHexalithProjectReferences=false -m:1 --artifacts-path "$TMPDIR/Release"
CI=true dotnet build "$project" -c Release -p:UseHexalithProjectReferences=false -m:1 --artifacts-path "$TMPDIR/Release"
(
  source scripts/prepare-projects-source.sh
  mkdir "$TMPDIR/projects-mapping"
  projects_prepare_source "$PWD" "$TMPDIR/projects-mapping"
  dotnet build "$project" -c Debug -m:1 --artifacts-path "$TMPDIR/Debug"
)
for lane in Release Debug; do
  ln -s "$PWD/Hexalith.Projects.slnx" "$TMPDIR/$lane/Hexalith.Projects.slnx"
  ln -s "$PWD/src" "$TMPDIR/$lane/src"
  dotnet "$TMPDIR/$lane/bin/Hexalith.Projects.Server.Tests/${lane,,}/Hexalith.Projects.Server.Tests.dll" -noColor -failSkips \
    -class Hexalith.Projects.Server.Tests.HttpContextProjectTenantContextAccessorTests \
    -class Hexalith.Projects.Server.Tests.ProjectsClaimsTransformationTests \
    -class Hexalith.Projects.Server.Tests.ProjectAuthorizationGateTests \
    -class Hexalith.Projects.Server.Tests.CreateProjectEndpointTests \
    -class Hexalith.Projects.Server.Tests.Queries.ListProjectsEnvelopePagingTests \
    -class Hexalith.Projects.Server.Tests.Authentication.ProjectsAuthenticationContractTests \
    -result-xml "$TMPDIR/$lane.xml"
done
python3 tools/planning/validate_production_authority.py --validate-index
'
```
