---
title: 'Complete five deferred read-request safety items'
type: 'bugfix'
created: '2026-10-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_revision: '602ea263b6c9eca5baefc396c1ba93ff1f905e06'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Legacy Projects read operations duplicate their request preamble (DW-53), omit generated correlation IDs (DW-49), omit correlation response headers on errors (DW-50), accept query-string Idempotency-Key (DW-58), and omit tenant-sensitive cache controls (DW-62).

**Approach:** Complete these five related obligations through one private shared read boundary, and push verified changes to main as requested. Normalize correlation/task IDs consistently, retain valid caller correlation IDs and generate a ULID otherwise, and set correlation and no-store headers before any handler return. Preserve existing Vary entries while varying authenticated reads by Authorization and Cookie. Extract the canonical Project-ID, authorization, detail and TenantAccessResult preamble for the four context/setup reads. Reuse post-authorization idempotency and strict freshness validation across all eleven legacy read operations, including proposal preview; reject Idempotency-Key in either header or query string, including empty and case-varied query keys. Preserve authorization-before-validation, safe-denial responses, existing error task-ID conventions and unrelated-query-parameter acceptance. Cover success, validation, denial and dependency failure through actual HTTP requests, including cross-operation regression controls and unchanged mutation admission. Record exactly five ledger closures. This intentionally changes legacy query validation and response headers described by DW-58/62; document these differences for Story 6.7 shadow rebaselining without accepting its cutover, changing normalization rules or advancing story gates. Retain legacy freshness semantics, supported /query behavior, dependency pins and all qualification decisions.

</frozen-after-approval>

## Implementation Notes

- No unresolved human intent or irreversible data action. This is a bounded private-helper refactor and HTTP safety fix with no new public API, migration or dependency update. Main push is explicitly authorized. The eleven call-site replacements are mechanical and belong to the same read boundary.
- Code map: ProjectsDomainServiceEndpoints.cs owns four legacy reads plus mutation envelopes; Queries/{GetProjectContext,GetProjectContextExplanation,RefreshProjectContext,GetConversationStartSetup}Endpoint.cs share the complete authorized-context preamble; both resolution endpoints and proposal preview share header/validation handling. Add the private helpers in Queries/ProjectQueryRequestPreamble.cs. Extend the existing GetConversationStartSetupTests HTTP fixture using a partial test class; existing sibling/read/mutation suites provide regression coverage.
- Required baseline/Git instructions, root EditorConfig/Git attributes/build/test projects, relevant AD-18/AD-20 and supported-read compatibility documentation were inspected. No submodule content or pointers will change. IDs accept the existing canonical opaque identifier grammar; new IDs use the transitive platform ULID library already restored.
- Exact AppHost start succeeded. `aspire describe --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj --format Json` retained at /tmp/five-read-boundary-aspire-baseline.json reports EventStore Finished with exitCode 134. This blocks claiming a healthy live baseline; focused in-process HTTP and individual-project validation remain available. Exact-target Aspire stop succeeded before code changes. The unrelated Tenants AppHost was untouched.

- Implementation replaces eleven read preambles with BeginQueryRequest and post-authorization ValidateQueryRequest. AuthorizeContextQueryAsync owns the complete shared canonical-ID/auth/detail/tenant-evidence preamble for the four context/setup handlers. Helpers are private in the existing endpoint partial; generated IDs use ByteAether.Ulid 1.4.2 already in the restored source and package dependency graphs (inspected with dotnet-inspect). Existing mutation envelopes, freshness response values and authorization failure mapping are preserved.
- New HTTP regressions exercise every read route, query-key spelling/empty/repeated/encoded cases after authorization, strict freshness, correlation generation/preservation at 200/400/404/503, and existing Vary preservation. Compatibility documentation records the repaired legacy baseline and leaves cutover acceptance gated. Initial test compilation required the repository CA1062 null guard on the MemberData URL input; fixed without suppressing diagnostics.

- Review corrections: fixed the old attachment test's absent-header expectation and asserted the generated ID reaches every Folder-owner read. Preserved Vary wildcard and case-insensitive existing authentication entries. Added null, trimmed valid and malformed task-ID controls across all eleven reads and both authorization outcomes; successful proposal preview with generated/caller correlation; cache/correlation equality on both resolution dependency and timeout branches and proposal dependency failure. Full Debug initially found two exact safe-denial comparison tests with independent generated IDs; they now supply identical caller correlation input while retaining the complete exact JSON comparison.

## Review Triage Log

- BH1 — medium, patched: the pre-existing attachment-resolution test asserted no response correlation on omitted input. It now asserts a generated ULID and exact equality with all captured owner-hop correlation IDs.
- BH2 — low, patched: preserve existing Vary wildcard and avoid case-insensitive authentication-field duplication. Three existing-Vary controls verify these cases without weakening no-store.
- BH3 — low, finalized: documentation/ledger mismatch was pending workflow finalization. Exactly DW-49/50/53/58/62 now have dated evidence-backed resolutions; reversing those five edits reconstructs every other original ledger byte. Story 6.7 acceptance remains gated.
- BH4 — low, finalized: spec status and reproducible validation were pending completion. This final record includes successful individual-project builds/runs and the separate AppHost exit-134 limitation.
- BH5 — medium verification gap, patched: the query-key HTTP matrix now checks absent, trimmed-valid and malformed task IDs at all eleven read operations for both authorized and denied callers, preserving each operation's error-disclosure convention.
- BH6 — medium verification gap, patched: the existing successful proposal-preview test now runs with caller-provided and generated correlation and verifies no-store plus authentication Vary fields. It retains all metadata-only/no-leakage assertions.
- BH7 — medium verification gap, patched: both resolution read-model failures and proposal dependency failure run with provided/generated IDs and check cache headers plus exact Problem Details correlation. Existing real resolution deadline cases now check those headers and correlation too.
- Independent recheck found no unresolved runtime findings and ran 53 targeted tests successfully. No new work was deferred.

## Final Verification

- `dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Debug -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` — passed with zero warnings/errors; /tmp/five-read-boundary-debug-build.log.
- `dotnet tests/Hexalith.Projects.Server.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.Tests.dll -noColor` — 1264 total, 1262 passed, zero failures, two existing opt-in live-infrastructure skips; /tmp/five-read-boundary-debug-tests.log. All 44 added cases run unconditionally, alongside strengthened existing owner/deadline/privacy regressions.
- `dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Release -p:UseHexalithProjectReferences=false -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` — passed with zero warnings/errors; /tmp/five-read-boundary-release-build.log. This verifies the existing published-package graph supplies the same ULID API without a dependency change.
- `dotnet tests/Hexalith.Projects.Server.Tests/bin/Release/net10.0/Hexalith.Projects.Server.Tests.dll -noColor` — 1263 total, 1262 passed, zero failures, one existing opt-in live-infrastructure skip; /tmp/five-read-boundary-release-tests.log. The extra Debug skip is source-fixture-only; no skip was introduced here.
- `python3 tools/planning/validate_production_authority.py --validate-index` — passed; authority index remains [6, 7, 8].
- `pwsh -NoProfile -File tests/tools/run-ci-workflow-gates.ps1` — passed for all five workflow files.
- `git diff --check` — passed. New files use CRLF; existing files retain their endings. No submodule content/pointer, dependency pin or story scheduling changed.
- `node_modules/.bin/commitlint --edit /tmp/five-read-boundary-commit-message.txt` — passed using repository configuration. `git fetch origin main` followed by `git rev-list --left-right --count HEAD...origin/main` returned 0/0 before commit. Main push is explicitly requested.
- Live baseline limitation remains the exact pre-change Projects AppHost describe result: EventStore Finished, exitCode 134. Focused HTTP and source/package tests do not claim healthy live-topology qualification; exact-target AppHost stop succeeded.
