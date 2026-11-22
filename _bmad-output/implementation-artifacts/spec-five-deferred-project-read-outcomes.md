---
title: 'Close five Projects read feedback and consistency follow-ups'
type: 'bugfix'
created: '2026-11-22'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Projects read callers need accurate feedback and candidates based on the Project evidence that was authorized. Close five entries from the Projects deferred-work register using the current read contracts and test fixtures.

- Preserve the supported Open client's bounded feedback when Open returns no payload. Keep safe denial for wrong-target and denied payloads, and stop before Context and Setup reads.
- Exclude Active quarantined Projects from supported advisory candidates on the initial and final detail reads. Preserve complete `NoMatch` for a deliberate exclusion and incomplete coverage for malformed evidence.
- Give supported List and Open cause-specific recovery actions. Use `Retry` for noncurrent required evidence and pending automatic Folder creation, `ResolveNeedsAttention` for current historical quarantine, `ContactAdministrator` for denied Folder access, `SelectAlternative` for archived Folder evidence or Archived folderless history, and `None` for complete or optional-only degraded reads.
- Use authorized Project details for retained list metadata and Conversation candidate scoring. Check the requested lifecycle against the authorized detail, retain list discovery and audit checks, and reject detail bound to another Project.
- Verify that retained Conversation owner unavailability yields a retryable 503 without candidate disclosure. Preserve the existing denial and completed no-match semantics.

</frozen-after-approval>

## Implementation Notes

- `ProjectDetailSource` consumes `ProjectReadQueryClient` feedback. `ProjectAdvisoryQueryExecutor.DetailAsync` is the shared candidate gate. `ProjectReadQueryExecutor.Snapshot` supplies row and Open recovery actions.
- `ProjectsDomainServiceEndpoints.ListProjectsAsync` audits the authorized rows before rechecking them. `ResolveProjectFromConversationEndpoint` uses discovery to bound candidate work and uses final authorized detail to score the discovered candidates. The existing legacy candidate gate will check detail identity.
- Use existing supported source, advisory, inventory, and retained HTTP fixtures. Keep the workspace's pulled submodules on their own `main` branches and verify in a separate checkout at the Projects gitlink revisions.
- The Story 6.7 matrix exposed inconsistent quarantine handling during rollback. Both advisory paths use the existing Active-quarantine predicate. Retained list and Conversation scores only consume authorized detail metadata, with explicit identity and shape checks.
- The retained routes still discover candidates from the eventual-consistency projection. A lifecycle or label match that has yet to reach that projection remains outside the candidate set. The register now tracks this separately because expanding discovery would affect the owner-read budget.

## Review Triage Log

| Finding | Disposition |
|---|---|
| Retained list discovery can lag a newly matching lifecycle | Medium, deferred with the corresponding Conversation discovery coverage gap. |
| Retained Conversation discovery can lag a newly matching label | Medium, deferred. Exact owner checks stay bounded to candidates with a discovery signal. |
| Retained detail could carry malformed descriptive fields | Medium, patched. Validation precedes list audit and final advisory scoring. |
| Quarantine could mask noncurrent Folder evidence with a reconciliation action | Medium, patched. Stale and unavailable Folder states now return Retry, with two independent fixture cases. |
| Open feedback tests covered only gateway exceptions | Medium, patched. Empty, mismatched, and denied payload cases assert a single request. |
| The retained outage test needed owner translation and short-circuit coverage | Medium, patched. The production adapter has a typed 503 regression and the retained route checks that discovery and Folder reads never start. |
| The Conversation provenance test used the same detail twice | Medium, patched. The final fixture changes lifecycle and label or returns a different Project identity. |
| The work register still showed the selected entries as open during review | Medium, patched. The five original entries carry targeted resolution notes. |

## Verification

- Debug/source builds of `Hexalith.Projects.Server.Tests` and `Hexalith.Projects.UI.Tests` passed with zero warnings and errors using the emitted `hexalith-module prepare-build` mapping, the committed dependency revisions, and `MinVerVersionOverride=1.0.0`.
- Ran both test assemblies with their native xUnit runner and XML output. UI passed 310 of 310. Server passed 1,609 of 1,610 on an isolated Dapr in-memory sidecar with zero skips. The sole failed test, `NativeConversationMembershipUsesTheRealGatewayOwnerAndPersistedNativeSource`, reported the same four Context and Explain cells for owner unavailability and Conversation binding on this change and on baseline `7cbfbf8a028bf764fc1b629388c63e895f7cd440`. The quarantine cell passed after applying the retained-route predicate.
- `pwsh -NoProfile -File tests/tools/run-ci-workflow-gates.ps1`, `python3 tools/planning/validate_production_authority.py --validate-index`, `git diff --check`, and commitlint passed.
- The mapped AppHost built successfully. `aspire run --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj --launch-profile http --isolated --detach --non-interactive --format Json` exited 2 because `/home/agent/.aspire/cli/bch` is read-only, so live AppHost startup was unavailable in this environment.
