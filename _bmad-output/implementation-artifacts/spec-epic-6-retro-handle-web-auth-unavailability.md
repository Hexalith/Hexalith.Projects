---
title: 'Handle Web authentication unavailability'
type: 'bugfix'
created: '2026-11-22'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

When the first authentication-state lookup faults, `ProjectReadQueryClient.QueryAsync` returns the existing `data_unavailable` warning with no payload and sends zero Gateway requests. The supported inventory page displays that warning through its existing feedback component. Preserve `safe_denial` for missing or mismatched identity, caller cancellation, and the post-query identity recheck.

Run the focused query-client and inventory-page regressions and record their commands and results. Close `epic-6-retro-handle-web-auth-unavailability` when they pass. Keep v4 qualification open for the resulting pinned revision. [Approved sprint change](../planning-artifacts/sprint-change-proposal-2026-11-22.md#4-bound-initial-web-authentication-failure)

</frozen-after-approval>

## Implementation Notes

The query client awaits the initial provider lookup before resolving tenant authority and bounds both reads. Inventory scopes its continuation to the tenant validated by the query client. Only the latest load can store its continuation. The inventory regression renders `Home` through the real dashboard, inventory and FrontComposer circuit tenant accessors. The query tests cover the warning, absent payload, zero Gateway calls, identity denial, cancellation during lookup and suppression of a payload after an identity change.

### Verification

Run from the Projects root. The Debug build used local project references on the working tree based on `619593f87f9fdd67e0fc6f549cd0a4fd6cc83ab6`. The links let source-reading tests find checkout files from the isolated test output directory.

```bash
run-with-temp bash -c 'set -e
dotnet build tests/Hexalith.Projects.UI.Tests/Hexalith.Projects.UI.Tests.csproj --configuration Debug --artifacts-path "$TMPDIR/artifacts" -m:1
ln -s "$PWD/Hexalith.Projects.slnx" "$PWD/src" "$PWD/docs" "$TMPDIR/artifacts/"
ui_tests="$TMPDIR/artifacts/bin/Hexalith.Projects.UI.Tests/debug/Hexalith.Projects.UI.Tests.dll"
dotnet "$ui_tests" -noColor -class Hexalith.Projects.UI.Tests.Diagnostics.SupportedProjectReadSourceTests -class Hexalith.Projects.UI.Tests.Components.SupportedProjectReadPageTests
dotnet "$ui_tests" -noColor'
```

- Build exited 0 with zero warnings and errors. The focused run passed **44 of 44** and the full UI run passed **306 of 306**, with zero failures or skips in both runs.
- Before the fix, both provider-fault regressions failed with the uncaught provider exception. A source-reading test also failed because the scratch output directory lacked links to the checkout. Adding the checkout links resolved the test setup failures.
- `run-with-temp timeout 45s aspire start --non-interactive --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj` exited 2 during restore. `references/Hexalith.Conversations/Directory.Build.props(9,5)` requires the Platform tool-selected source mapping. `aspire ps --non-interactive` showed no running AppHost after the attempt.

The v4 comparison and rollback action remains open for the pinned Projects revision containing this fix and its committed direct gitlinks.

## Review Triage Log

- High, fixed. The circuit tenant accessor could interpret a faulted authentication task as missing identity. The query client now awaits the provider first, and the circuit regression verifies `data_unavailable` for a faulted task.
- High, fixed. A synchronous provider throw could escape the inventory cursor scope lookup. The inventory now prepares its cursor after authority validation, and the circuit regression covers an uninitialized server provider.
- Medium, fixed. An unresolved provider task could hold the query open after caller cancellation. The query client waits with the caller token, and a regression cancels only the caller while the provider task stays pending.
- Medium, fixed. A concurrent provider fault could substitute feedback for cancellation. The failure handler checks the caller token, and a regression exercises a canceled caller with a faulted provider task.
