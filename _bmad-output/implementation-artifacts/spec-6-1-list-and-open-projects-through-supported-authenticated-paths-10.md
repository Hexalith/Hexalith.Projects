---
title: 'Story 6.1: Verify supported reads after a real Projects process restart'
type: 'feature'
created: '2026-10-25'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
story_key: '6-1-list-and-open-projects-through-supported-authenticated-paths'
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

- [ ] `tools/qualification/run_supported_project_reads.py`: restrict actions to owned Projects/Folders; prove old process exit/new start identity; rediscover endpoints and record sanitized evidence.
- [ ] `tests/tools/test_supported_project_reads_launcher.py`: cover target allowlist, Folder compatibility, replaced/unobservable sessions, stop/start failures and unchanged-process refusal.
- [ ] `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadDistributedFixture.cs`: add bounded Projects restart/recovery; compare metadata, versions, freshness, ordering and persisted state, excluding volatile observation timestamps and opaque token bytes.
- [ ] `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadDistributedTests.cs`: require restart, pre-restart continuation and foreign/absent denial controls; then revoke permission using a cursor proved valid after restart.
- [ ] `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadEvidenceTests.cs`: negatively test new evidence predicates against missing process change and altered state.
- [ ] `docs/supported-project-reads.md`: record reproducible command, actual results and remaining gates.

**Acceptance Criteria:**

- Given the managed fixture, when qualification runs, then all nine rows pass without skips with distinct observed process identities and authorized recovery.
- Given unchanged evidence, when an unexpired cursor reaches the restarted host, then paging remains gap-free; later revocation invalidates that previously valid continuation.
- Given any outcome, when teardown completes, then actual results, owned cleanup and external preservation are recorded without protected payloads or credentials.

## Implementation Notes

2026-10-25: User approved this spec and requested stopping before implementation. Approval covers the local restart proof only; frozen intent is locked and formal Story 6.1 gates remain unchanged.

## Spec Change Log

## Review Triage Log

## Verification

- Before code changes, use the Aspire skill to capture AppHost baseline; preserve unrelated sessions.
- Run `python3 -m unittest discover -s tests/tools -p test_supported_project_reads_launcher.py`; all ownership controls must execute and pass.
- Run `python3 tools/qualification/run_supported_project_reads.py --evidence /tmp/story61-projects-restart-20261025 --regressions --timeout 900` into a new destination. Inspect XML, matrix/process observations, source bindings and cleanup. The launcher builds Debug and runs individual test assemblies.
- Run `python3 tools/planning/validate_production_authority.py --story-id 6.1`, `python3 tools/planning/validate_production_authority.py --validate-index` and `git diff --check`. Record exact blockers; missing evidence cannot pass.
