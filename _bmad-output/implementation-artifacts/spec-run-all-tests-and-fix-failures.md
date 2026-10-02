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

- The initial worktree was clean on `main`. Eight .NET projects, Python tooling/skill fixtures, and the Playwright workspace are current test owners; archived browser backups and submodule-owned suites are excluded.
- TypeScript checking passes. Initial Python discovery ran 113 tests and reported 19 stale-evidence failures: G-6 attempt-16 bindings differ from current gitlinks, and the P1R test still targets the superseded EventStore 3.109.0 checkout.
- Local broad Debug build needs the documented `HexalithCommonsRoot` environment variable to route sibling dependencies to the root-declared Commons checkout; a focused Contracts build and its 194 tests pass with that route.
- Browser failures are under investigation, including axe timeouts and interactive maintenance fixtures. Historical qualification packets and owner approvals remain intact.
- Fixed browser launch selection to prefer installed Playwright Chromium after explicit overrides, with system Chromium as fallback. Kept the previous browser-matrix policy for hosts with and without system browsers. Nine simulated selection scenarios passed. System Chrome stalled animation frames; WebKit needed software rendering in this environment.
- Updated P1R replay to the already accepted published EventStore 3.110.0 tuple, preserving historical 3.109.0 release, ancestry, source, and disposition checks. Added checks of publication proof, independent push CI for the tagged commit, the tagged package manifest, and exact consumer fixture bytes. Both the CI fetch and its exact workflow-policy expectation include the 3.110.0 tag.
- Corrected obsolete custom-module marketplace naming expectations in the three mirrored BMAD fixtures and synchronized the installer manifest hash.
- No production code, dependency versions, submodule revisions, qualification packets, or named owner approvals were changed. No commit or push was made. The task remains in progress because current G-6 qualification and live credentials are unresolved.

## Review Triage Log

- Medium, patched: preferring managed Chromium initially suppressed the existing full default matrix on hosts without system Chromium. Launch selection and the original fallback-based matrix policy now use separate variables; simulated cases and the explicit full matrix pass.
- Medium, patched: an empty consumer hash map could avoid checking fixtures. The selected replay now requires exactly `Consumer.csproj`, `PublishedApiSmoke.cs`, and `NuGet.Config`.
- Medium, patched: recorded manifest hash, unchanged claim, and diff exit code were not checked. The replay now verifies these fields and the actual Git diff.
- Medium, patched: the recorded independent CI conclusion was ignored. The replay now binds the recorded success to the API result.
- Low, patched: the new replay called a successful `main` push at the tagged source revision a tag-triggered run. Names and prose now say independent push CI for the tagged commit; no unsupported tag-branch requirement was added.
- All five reproduced evidence mutations are rejected. The selected replay passes all 13 tests after the review corrections.

## Verification

- `HexalithCommonsRoot=/home/administrator/projects/hexalith/projects/references/Hexalith.Commons dotnet build Hexalith.Projects.slnx --configuration Debug -m:1`: passed, zero warnings/errors. The CI solution is intended for Release/package consumption; local Debug validation uses the source solution.
- Each owned project ran with `dotnet test --project tests/<project>/<project>.csproj --configuration Debug --no-build --no-progress --no-ansi`: all eight passed, zero failures/skips. CLI 18; Client 118; Contracts 194; Integration 27; MCP 30; Server 808; domain 683; UI 160. Total 2,038.
- `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tools -p 'test_*.py' -v`: 118 tests, 115 passing methods and three G-6 methods reporting 18 drift assertion failures. Log: `/tmp/hexalith-projects-python-tests-final.log`.
- All 66 root-owned Python skill test files ran: 1,299 cases pass across three mirrored trees. Corrected standalone fixtures and merge/integrity controls were rerun. Logs: `/tmp/hexalith-skill-python-tests.log`, `/tmp/hexalith-skill-python-recheck.log`.
- `python3 -m unittest tests/tools/test_p1r_candidate_evidence.py -v`: all 13 passed, including independent GitHub publication/CI observations. No historical or current acceptance was changed.
- `pwsh -NoProfile -File tests/tools/run-ci-workflow-gates.ps1`: passed. Python workflow policy fixtures passed.
- `npm --prefix tests/e2e run typecheck`: passed.
- `npm --prefix tests/e2e test -- --workers=4`: 21 passed, 66 live-only skipped.
- From `tests/e2e`, `LIBGL_ALWAYS_SOFTWARE=1 PLAYWRIGHT_INCLUDE_MANAGED_BROWSERS=1 npm test -- --workers=4 --output=/tmp/projects-e2e-final-policy-matrix --reporter=list`: 63 passed across Chromium/Firefox/WebKit, 198 live-only skips. Log: `/tmp/hexalith-projects-playwright-final-policy-matrix.log`.
- `NuGetAudit=false pwsh -NoProfile -File tests/tools/run-frontcomposer-inspect-gate.ps1`: passed. Independently inspected freshly built Debug artifacts with zero warnings/errors.
- `pwsh -NoProfile -File tests/tools/run-openapi-fingerprint-gate.ps1`: ordinary Release restore stalled. Focused fallback `dotnet build tests/Hexalith.Projects.Client.Tests/Hexalith.Projects.Client.Tests.csproj --configuration Release -m:1 -p:NuGetAudit=false` passed with zero warnings/errors, then `dotnet tests/Hexalith.Projects.Client.Tests/bin/Release/net10.0/Hexalith.Projects.Client.Tests.dll -class Hexalith.Projects.Client.Tests.ClientGenerationTests -noColor` passed all 101 fingerprint/compatibility assertions. The same 101 Debug assertions also passed. Only this session's stalled validation processes were stopped.
- `git diff --check`: passed. Root-declared submodule worktrees still match their recorded gitlinks. No nested submodules were initialized.

## Remaining Blockers

- `python3 tests/tools/run_g6_candidate_gate.py --baseline references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-10-01.json --packet _bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain-20261001/attempt-16/packet.json`: exit 1, 11 failed checks. The accepted-only validator reports `Full source closure drift`, nine packet-bound submodule gitlinks have advanced, and CI executes Builds `51af786cf156d2a3396dbd49f5e4898222e55c12` while the root gitlink is `611f7e882ec615bad534de73149c7e95431b4cd4`. Log: `/tmp/hexalith-g6-current-gate.log`. These require a new committed qualification tuple, a fresh packet, reconciled workflow/evidence bindings, and a new named owner acceptance; transferring historical acceptance or weakening these checks would be incorrect.
- `npm --prefix tests/e2e run test:live:managed`: exit 1, `KEYCLOAK_CLIENT_ID is required`. The client ID, test username, and test password are absent, and `tests/e2e/.env` does not exist. The 66 live journeys per browser remain unexecuted. Log: `/tmp/hexalith-projects-live-e2e.log`.
- The preliminary `aspire start --apphost /home/administrator/projects/hexalith/projects/src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj --isolated --format Json --non-interactive` attempt timed out after 120 seconds during restore. Exact-target cleanup confirmed no Projects AppHost remains running; the pre-existing Tenants AppHost was preserved.
