---
title: 'Story 6.1: Verify supported reads after a real Projects process restart'
type: 'feature'
created: '2026-10-25'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
story_key: '6-1-list-and-open-projects-through-supported-authenticated-paths'
baseline_commit: '05a5280a4f33f03b2b711ae8e6272af8c1d596db'
planning_baseline_commit: '8bb1428632b0f1568a2f15b5d29bfc3122cb51c0'
implementation_gate: 'approved-local-restart-proof-formal-story-blocked'
context:
  - 'AGENTS.md'
  - '_bmad-output/implementation-artifacts/epic-6-context.md'
  - 'docs/supported-project-reads.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Distributed tests restart Folders but never the Projects query process. Existing Projects restart tests recreate an in-process host with authority substitutes.

**Approach:** Restart the owned Projects process in the existing isolated Development lane. Verify authorized list/open, paging, persisted state and subsequent Folder revocation through real owner handlers.

## Boundaries & Constraints

**Always:** Use Debug source references, real handlers, isolated storage and ownership guards. Preserve eight existing matrix rows, concurrent edits and historical evidence. Compare both tenants' domain keys, bytes, ETags and watermarks; separate audit/operational writes. One C# type per file.

**Never:** Reseed/rebuild to conceal failure; substitute authority; change runtime security, cursor policy, pins, defaults, public routes or shared resources. No commits, publication, deployment or sprint/acceptance transitions. Local restart proof establishes no G-4, shadow-equivalence, P4 or formal story acceptance.

## I/O & Edge-Case Matrix

| Scenario | Input / state | Required behavior |
| --- | --- | --- |
| Restart | Populated fixture; successful first page and open | Original Projects process exits; a distinct owned process starts; authorized reads recover without reseeding |
| Continuation | Unexpired pre-restart cursor; unchanged authority and state | Existing cursor returns the next expected row without duplicates or gaps; fresh paging agrees |
| Persistence | Domain snapshot before restart | Both tenants retain identical domain key sets, bytes, ETags and exact watermarks after restart and reads |
| Authority | Revoke Folder permission after recovery | Current owner head advances; hidden Project disappears; previously valid continuation is rejected |
| Ownership failure | Foreign/replaced host, disallowed resource or unreadable identity | Refuse mutation; fail evidence; retain cleanup of independently proven owned resources |
| Recovery failure | Start fails or authorized read never recovers | Bounded failure with metadata-only diagnostics; no passing restart row; existing teardown still executes |

</frozen-after-approval>

## Code Map

- Existing distributed fixture: reuse `ResourceAsync`, `SnapshotAsync`, `AssertUnchangedAsync`, `WaitForAuthorizedRecoveryAsync` and `HeadAsync`. Insert restart before authority mutation; never repeat `SeedAsync`.
- Launcher: extend `mutate_owned_folder` and private `--resource-action`; retain AppHost/configuration identity and discovery/source/cleanup checks.
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs`: existing Data Protection registration and cursor purpose remain unchanged.
- `tests/Hexalith.Projects.Server.Tests/Queries/SupportedProjectReadHttpTests.cs`: in-process recreation coverage remains separate.

## Tasks & Acceptance

**Execution:**

- [x] `tools/qualification/run_supported_project_reads.py`: restrict actions to owned Projects/Folders; prove old process exit/new start identity; rediscover endpoints and record sanitized evidence.
- [x] `tests/tools/test_supported_project_reads_launcher.py`: cover target allowlist, Folder compatibility, replaced/unobservable sessions, stop/start failures and unchanged-process refusal.
- [x] `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadDistributedFixture.cs`: add bounded Projects restart/recovery; compare metadata, versions, freshness, ordering and persisted state, excluding volatile observation timestamps and opaque token bytes.
- [x] `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadDistributedTests.cs`: require restart, pre-restart continuation and foreign/absent denial controls; then revoke permission using a cursor proved valid after restart.
- [x] `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadEvidenceTests.cs`: negatively test new evidence predicates against missing process change and altered state.
- [x] `docs/supported-project-reads.md`: record reproducible command, actual results and remaining gates.

**Acceptance Criteria:**

- Given the managed fixture, when qualification runs, then all nine rows pass without skips with distinct observed process identities and authorized recovery.
- Given unchanged evidence, when an unexpired cursor reaches the restarted host, then paging remains gap-free; later revocation invalidates that previously valid continuation.
- Given any outcome, when teardown completes, then actual results, owned cleanup and external preservation are recorded without protected payloads or credentials.

## Implementation Notes

2026-10-26: Independent review completed with ten individual verdicts: nine findings resolved in seven patch groups; one ownership-predicate claim rejected with caller/writer evidence; nothing deferred. Final corrected-tree qualification passed at `/tmp/story61-projects-restart-reviewed-20261026-2`: all nine distributed rows, Integration 104/104, Server 1306/1306, launcher controls 48/48, zero failures/skips. Focused evidence controls passed 19/19. Both authority validators and whitespace checks passed. All three test-runner process groups, exact AppHost, six sidecars, three containers and private material were verified absent; five pre-existing containers and all eight existing EventStore edits were preserved. No sprint/acceptance transition or commit was made.

2026-10-26: First reviewed-tree attempt `/tmp/story61-projects-restart-reviewed-20261026` stopped before runtime tests: `aspire start` exited 2 with MSBuild MSB4166 worker-node failures. Its owned containers/private material were removed and external resources preserved. The passing retry used `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 DOTNET_PROCESSOR_COUNT=2 python3 tools/qualification/run_supported_project_reads.py --evidence /tmp/story61-projects-restart-reviewed-20261026-2 --regressions --timeout 900`. Repository build/runtime configuration was unchanged.

2026-10-26: All six tasks and nine live matrix rows passed. Evidence: `/tmp/story61-projects-restart-20261025`; launcher controls 44/44, Integration 92/92, Server 1306/1306, no skips. Parent verified XML, distinct process identities, exact before/after domain fingerprints/watermarks, successful owned cleanup and external preservation. Independent review follows.

2026-10-26: User authorized implementation of this approved spec while preserving existing EventStore edits. Sprint/acceptance status stays unchanged under the frozen scope. Current implementation baseline is recorded above.

2026-10-25: User approved this spec and requested stopping before implementation. Approval covers the local restart proof only; frozen intent is locked and formal Story 6.1 gates remain unchanged.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B1 | medium | patch: restart deadline | New restart guards reach exact_apphost_sessions/execute with the 600-second default; repeated guards can outlast the enclosing test timeout. Bound the complete restart, including ownership/discovery commands, and test a stalled guard. |
| B2 | medium | patch: helper cleanup | ResourceAsync now invokes the longer restart helper through ProcessBytesAsync; cancellation disposes Process without terminating descendants, while the outer subprocess timeout kills only the test runner. The new path can overlap teardown; terminate and observe owned helper descendants before cleanup. |
| B3 | false | reject | The predicate is not the ownership authority: RestartProjectsAsync first requires the guarded resource command to exit successfully. mutate_owned_resource verifies AppHost/configuration and discovery before emitting completed=true. Missing ownership fields cannot arise on this successful writer path; duplicating those checks in the C# predicate would not demonstrate the claimed bad outcome. |
| B4 | medium | patch: cursor causal proof | Projects query executor wraps the SDK cursor in a position with a 15-minute expiry; both expiry and changed authority yield InvalidCursor/HTTP 400. Revalidate immediately before revocation and require the original cursor still to be inside its lifetime at rejection. |
| B5 | medium | patch: failed state evidence | VerifyRestartStateAsync asserts equality before writing fingerprints; a real key/byte/ETag regression loses comparison observations when owned storage is torn down. Persist sanitized comparison observations before failing, including missing/unreadable state. |
| B6 | low | patch: refreshed parent endpoints | refresh_owned_endpoints replaces the private endpoint file, but the parent retains the original dictionary for regression environment variables. Reload that file before regression setup so rediscovery has one current consumer view. |
| B7 | medium | patch: recovery controls | The recovery loop adds transport retries, cancellation and failure logging but the recorded live recovery succeeds on its first HTTP 200. Add controlled transient-error, deadline, cancellation and authentication-refusal cases with sanitized diagnostic assertions. |
| B8 | low | patch: reproduction command | The documented evidence path already exists after the recorded passing run and the launcher refuses reuse. Use its automatic fresh destination for the repeatable command while retaining the historical evidence location. |
| E1 | medium | patch: cursor causal proof | Same verified expiry ambiguity as B4: arbitrary positive launcher timeouts permit a slow run to reach cursor expiration before the later rejection. Grouped with B4 after individual verdict. |
| V1 | medium | patch: recovery controls | Pre-verified gap: searches and retained recovery log show no transport-error or deadline-exhaustion exercise. Removing the budget or retry behavior would leave the successful live run green. Grouped with B7 after individual verdict. |

Review disposition: B1/B2 deadline and helper cleanup, B4/E1 causal cursor proof, B5 failed-state artifacts, B6 refreshed endpoints, B7/V1 recovery controls, and B8 reproduction instructions are fixed and covered by the final verification. No findings deferred.

## Verification

- Before code changes, use the Aspire skill to capture AppHost baseline; preserve unrelated sessions.
- Run `python3 -m unittest discover -s tests/tools -p test_supported_project_reads_launcher.py`; all ownership controls must execute and pass.
- Run `python3 tools/qualification/run_supported_project_reads.py --evidence /tmp/story61-projects-restart-20261025 --regressions --timeout 900` into a new destination. Inspect XML, matrix/process observations, source bindings and cleanup. The launcher builds Debug and runs individual test assemblies.
- Run `python3 tools/planning/validate_production_authority.py --story-id 6.1`, `python3 tools/planning/validate_production_authority.py --validate-index` and `git diff --check`. Record exact blockers; missing evidence cannot pass.
