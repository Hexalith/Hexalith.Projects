---
title: 'Run all Projects tests and fix failures'
type: 'bugfix'
created: '2026-10-02'
status: 'in-progress'
route: 'oneshot'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Run every current Projects-owned .NET, Python/tooling, and browser test suite and applicable generated-artifact gates. Repair reproducible defects with the smallest correct changes, preserving test strength and existing qualification evidence. Use the pinned toolchain and local Debug/source references; report exact environment or qualification blockers separately from passing tests. Preserve unrelated applications and submodules. Do not commit, push, update dependencies, or transfer historical owner acceptance to a changed dependency tuple.

</frozen-after-approval>

## Implementation Notes

- Validation resumed against root HEAD `91a3c13027db61f1b2f96e7bdfef8f22f6dd305b`. Earlier browser selection, selected P1R replay, CI tag-fetch policy, and mirrored marketplace fixture fixes are already in that revision. This session made no commit or push.
- Test owners are eight Projects .NET projects, tooling discovery under `tests/tools`, 66 root-owned Python skill test files across the three mirrored trees, and the Playwright workspace. Archived browser backups and submodule-owned suites are excluded.
- Local Debug/source builds use the documented `HexalithCommonsRoot` route. Browser checks now use Node `24.21.0`, matching `tests/e2e/.nvmrc`; no repository dependency versions were changed.
- Serialized the OpenAPI gate build with `-m:1`, matching the supported focused fallback. The actual Release gate now completes and all 101 fingerprint/compatibility assertions pass.
- Tightened selected P1R assertions to compare the whole canonical accepted tuple, exact repository provenance, recorded consumer outcomes and package-only resolution, and tool identity/archive hashes. Three mutation regression methods reject historical tuple substitution, failed/source-based consumer claims, and mutually agreeing wrong provenance. Historical records and named acceptance remain untouched.
- Authentication global setup now reuses Chromium launch options from the resolved Playwright configuration. Persisted nine executable-selection/browser-matrix regression scenarios; all pass in Chromium, Firefox, and WebKit.
- Release/package compilation exposed the pinned Folders 1.0.0 client's older effective-permissions signature. The Server adapter passes the task-ID argument only for source-reference builds, preserving the current source header and supporting the existing pinned package without changing versions. The header regression checks the resolved generated client contract; its HTTP handler was extracted to a separate file.
- Additional Release tests exposed an empty-success response deserializing into default metadata/freshness in the older Folders client. The file-reference adapter now treats absent or unobserved freshness as unavailable. The existing malformed-success assertion passes in both build modes. The required metadata `v2` route assertion remains intact; the pinned client's internal `v1` route is recorded as a dependency capability blocker.
- Used the user's kubeconfig for read-only cluster discovery. It reaches Keycloak at `auth.tache.ai`; no Projects resources were found. Discovered machine authentication metadata does not supply an end-user browser password. The source-local realm has demo accounts, but the managed Projects launch shares Dapr app IDs and Redis state with the pre-existing Tenants application; isolated ports alone do not isolate these resources. No Kubernetes resources, credentials, shared containers, or unrelated applications were changed.
- Dependency pins, root-declared submodule revisions, historical qualification packets, and named owner approvals are unchanged. Status remains `in-progress` because current G-6 qualification, pinned Folders package parity, and live browser verification remain blocked.

## Review Triage Log

- Medium, patched: preferring managed Chromium initially suppressed the existing full default matrix on hosts without system Chromium. Launch selection and the original fallback-based matrix policy now use separate variables; simulated cases and the explicit full matrix pass.
- Medium, patched: an empty consumer hash map could avoid checking fixtures. The selected replay now requires exactly `Consumer.csproj`, `PublishedApiSmoke.cs`, and `NuGet.Config`.
- Medium, patched: recorded manifest hash, unchanged claim, and diff exit code were not checked. The replay now verifies these fields and the actual Git diff.
- Medium, patched: the recorded independent CI conclusion was ignored. The replay now binds the recorded success to the API result.
- Low, patched: the new replay called a successful `main` push at the tagged source revision a tag-triggered run. Names and prose now say independent push CI for the tagged commit; no unsupported tag-branch requirement was added.
- All five reproduced evidence mutations are rejected. The selected replay passes all 13 tests after the review corrections.

Resumed independent review:

- Medium, patched: searching owner-packet prose for the Builds revision could accept a historical publication coordinate. Full selected-tuple equality against the canonical scheduling guard now rejects that substitution; the mutation control passes.
- Medium, patched: consumer fixture hashes alone did not establish successful package consumption. Replay now checks recorded zero exits, warnings/errors, package-only library counts, API probes, and tool identity/hash. Failed and source-based claim mutations are rejected.
- Medium, patched: two provenance dictionaries could agree on an invalid repository. Both must now match the exact repository type, URL, and tagged commit; wrong URL/type mutations are rejected.
- Medium, patched: browser authentication launched Chromium independently of the configured managed/system fallback. Global setup now passes the resolved Chromium project's launch options; type checking and the browser matrix pass.
- Low, patched: the nine earlier simulated browser-selection checks were not retained. Nine executable configuration regression cases are now checked in and pass across all three browser projects.
- Low, deferred: `6-1-p1r-current-exact-baseline-candidate.md:40` still describes the CI lane as fetching only 3.109.0. The current workflow and replay include 3.110.0. Record a dated owner-packet clarification in separate documentation work, preserving historical acceptance and remaining qualification limits; appended to `deferred-work.md`.
- Independent follow-up review found no new code issues, including the Folders compatibility branch and unobserved-freshness guard. It confirmed that preserving the `v2` assertion exposes the pinned-package capability blocker rather than weakening parity evidence.

## Verification

All commands ran from the repository root unless stated otherwise. Current logs use `/tmp/projects-build-20261002-`.

- `HexalithCommonsRoot=/home/administrator/projects/hexalith/projects/references/Hexalith.Commons NuGetAudit=false dotnet build Hexalith.Projects.slnx --configuration Debug -m:1`: passed, zero warnings/errors. Final log: `/tmp/projects-build-20261002-dotnet-build-final.log`.
- Each owned project ran with `dotnet test --project tests/<project>/<project>.csproj --configuration Debug --no-build --no-progress --no-ansi`: all eight passed, zero failures/skips. CLI 18; Client 118; Contracts 194; Integration 27; MCP 30; Server 808; domain 683; UI 160. Total 2,038. Summary: `/tmp/projects-build-20261002-dotnet-results-reviewed.json`. Server and Integration were rerun after the final freshness correction: `/tmp/projects-build-20261002-final-Server.Tests.log`, `/tmp/projects-build-20261002-final-Integration.Tests.log`.
- `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tools -p 'test_*.py' -v`: 121 tests, 118 passing methods and three G-6 methods reporting 18 drift assertion failures. Log: `/tmp/projects-build-20261002-python-reviewed.log`.
- All 66 root-owned skill test files ran: 1,299 cases pass across three mirrored trees. Files requiring pytest/ruamel.yaml used isolated declared dependencies via `uv run --no-project --with pytest --with ruamel.yaml python -m pytest ... -q -p no:cacheprovider`; no dependency manifests changed. Inventory: `/tmp/projects-build-20261002-skills-inventory.json`. Logs: `/tmp/projects-build-20261002-skills.log`, `/tmp/projects-build-20261002-skills-focused.log`, `/tmp/projects-build-20261002-skills-agent-focused.log`, `/tmp/projects-build-20261002-skills-claude-focused.log`.
- `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests/tools/test_p1r_candidate_evidence.py -v`: all 16 passed, including independent GitHub publication/CI observations and three new mutation regression methods. Log: `/tmp/projects-build-20261002-p1r-reviewed.log`. This replays the recorded archive/consumer claims and verifies fixture bindings; it is not a fresh archive qualification or owner acceptance.
- `pwsh -NoProfile -File tests/tools/run-ci-workflow-gates.ps1`: passed after the corrections. Log: `/tmp/projects-build-20261002-workflow-reviewed.log`.
- From `tests/e2e`, with Node 24 selected, `npm run typecheck`: passed. `LIBGL_ALWAYS_SOFTWARE=1 PLAYWRIGHT_INCLUDE_MANAGED_BROWSERS=1 npm test -- --workers=4 --output=/tmp/projects-build-20261002-browser-reviewed --reporter=list`: 90 passed across Chromium/Firefox/WebKit, 198 live-only skips (66 per browser). Logs: `/tmp/projects-build-20261002-typecheck-reviewed.log`, `/tmp/projects-build-20261002-browser-reviewed.log`.
- `NuGetAudit=false pwsh -NoProfile -File tests/tools/run-frontcomposer-inspect-gate.ps1`: passed. Log: `/tmp/projects-build-20261002-frontcomposer.log`.
- `NuGetAudit=false pwsh -NoProfile -File tests/tools/run-openapi-fingerprint-gate.ps1`: passed after serialization; 101 Release assertions pass. Log: `/tmp/projects-build-20261002-openapi-serialized.log`. Earlier timeout and focused fallback remain recorded in `/tmp/projects-build-20261002-openapi.log` and `/tmp/projects-build-20261002-openapi-fallback-tests.log`.
- `NuGetAudit=false pwsh -NoProfile -File tests/tools/run-package-dependency-gate.ps1 -PackageDirectory /tmp/projects-build-20261002-packages`: passed after the effective-permissions signature fix. Release CI build has zero warnings/errors; five packages and five isolated real-API consumers pass metadata, graph, provenance, and compile-asset checks. Log: `/tmp/projects-build-20261002-package-gate-reviewed.log`.
- Additional package-mode tests: `dotnet test --project tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj --configuration Release --no-build --no-progress --no-ansi` passed 27/27. After the freshness correction, the same command for `Hexalith.Projects.Server.Tests` ran 808 tests: 807 passed, one failed, no skips (the required metadata `v2` route). Focused Release build passed with zero warnings/errors. Logs: `/tmp/projects-build-20261002-release-Integration.Tests.log`, `/tmp/projects-build-20261002-release-Server-reviewed.Tests.log`, `/tmp/projects-build-20261002-release-Server-reviewed-build.log`.
- `git diff --check`: passed. Root-declared submodule worktrees match their recorded gitlinks. No nested submodules were initialized. Sanitized `aspire ps --format Json --non-interactive` confirms only the pre-existing Tenants AppHost remains running.

## Remaining Blockers

- `PYTHONDONTWRITEBYTECODE=1 python3 tests/tools/run_g6_candidate_gate.py --baseline references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-10-01.json --packet _bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain-20261001/attempt-16/packet.json`: exit 1, 11 failed checks. The accepted-only validator reports `Full source closure drift`, nine packet-bound submodule gitlinks have advanced, and CI executes Builds `51af786cf156d2a3396dbd49f5e4898222e55c12` while the root gitlink is `611f7e882ec615bad534de73149c7e95431b4cd4`. Log: `/tmp/projects-build-20261002-g6.log`. Resolving this requires a fresh committed qualification tuple, reconciled workflow/evidence bindings, a fresh packet, and new named owner acceptance; the frozen Intent prohibits transferring historical acceptance or weakening these checks.
- The additional Release Server run exits 2: `ProjectFileReferenceDirectoryTests.ValidateLink_AuthorizedNotRedactedFile_Accepts` requires `/api/v2/.../context/metadata`, while the pinned Folders.Client 1.0.0 generated client calls `/api/v1/.../context/metadata`. Debug/source tests use the current `v2` client and pass. Updating dependency pins is outside the frozen Intent, and accepting the older route would weaken the current parity assertion. The earlier malformed-response failure was repaired separately; the effective-permissions source task header remains verified.
- `npm --prefix tests/e2e run test:live:managed`: exit 1, `KEYCLOAK_CLIENT_ID is required`; the expected test-user environment and `tests/e2e/.env` are absent. Log: `/tmp/projects-build-20261002-live.log`. Kubeconfig access established Keycloak availability but did not provide a deployed Projects topology or the browser test user's password. Source-local demo accounts exist, but a live local launch needs discovery/state isolation from the running Tenants application's same-named Dapr services and shared Redis. The 66 live journeys per browser remain unexecuted; cluster or credential mutations were not performed.
- The preliminary `aspire start --apphost /home/administrator/projects/hexalith/projects/src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj --isolated --format Json --non-interactive` attempt timed out after 120 seconds during restore (exit 124). Log: `/tmp/projects-build-20261002-aspire-start.log`. Exact-target cleanup confirmed no Projects AppHost remains running; the pre-existing Tenants AppHost was preserved. Subsequent builds pass, but that does not establish safe live discovery/state isolation.
