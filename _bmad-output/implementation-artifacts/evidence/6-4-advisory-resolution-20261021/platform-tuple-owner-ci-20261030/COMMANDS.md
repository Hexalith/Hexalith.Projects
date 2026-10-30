# Executed commands

Each receipt retains the actual argv, cwd, explicit environment and result. Standard command receipts include UTC timing and matching stdout/stderr. SDK-task subcommand receipts retain exact arguments; timing is in their enclosing command receipt. Failed attempts are preserved. GitHub connector reads/writes and official artifact metadata are retained separately as JSON.

| Receipt | Exit | UTC start or enclosing diagnostic |
| --- | --- | --- |
| [assign-before](assign-before.command.json) | 1 | See enclosing inspection command |
| [task-assign-before](task-assign-before.command.json) | 1 | See enclosing inspection command |
| [task-assign-v2-after](task-assign-v2-after.command.json) | 0 | See enclosing inspection command |
| [task-assign-v2-before](task-assign-v2-before.command.json) | 0 | See enclosing inspection command |
| [initial-owner-state](initial-owner-state.command.json) | 0 | 2026-10-30T06:03:41.649280+00:00 |
| [push-exact-candidate](push-exact-candidate.command.json) | 0 | 2026-10-30T06:04:07.103876+00:00 |
| [workflow-registry-before](workflow-registry-before.command.json) | 0 | 2026-10-30T06:04:15.588385+00:00 |
| [create-workflow-worktree](create-workflow-worktree.command.json) | 0 | 2026-10-30T06:04:22.879599+00:00 |
| [prepare-workflow-only](prepare-workflow-only.command.json) | 0 | 2026-10-30T06:04:43.289918+00:00 |
| [validate-workflow-bootstrap](validate-workflow-bootstrap.command.json) | 0 | 2026-10-30T06:05:06.487043+00:00 |
| [bootstrap-commitlint](bootstrap-commitlint.command.json) | 0 | 2026-10-30T06:05:18.268358+00:00 |
| [stage-workflow-only](stage-workflow-only.command.json) | 0 | 2026-10-30T06:05:26.824040+00:00 |
| [commit-workflow-only](commit-workflow-only.command.json) | 0 | 2026-10-30T06:05:26.908526+00:00 |
| [fetch-before-bootstrap-push](fetch-before-bootstrap-push.command.json) | 0 | 2026-10-30T06:05:32.522650+00:00 |
| [push-workflow-registration](push-workflow-registration.command.json) | 0 | 2026-10-30T06:05:57.132346+00:00 |
| [bootstrap-merge-commitlint](bootstrap-merge-commitlint.command.json) | 0 | 2026-10-30T06:06:52.139892+00:00 |
| [verify-registered-workflow](verify-registered-workflow.command.json) | 0 | 2026-10-30T06:07:19.197383+00:00 |
| [fetch-registration-result](fetch-registration-result.command.json) | 0 | 2026-10-30T06:07:19.569383+00:00 |
| [verify-registration-identity](verify-registration-identity.command.json) | 0 | 2026-10-30T06:07:31.493621+00:00 |
| [dispatch-exact-candidate](dispatch-exact-candidate.command.json) | 0 | 2026-10-30T06:07:31.599005+00:00 |
| [list-dispatched-runs](list-dispatched-runs.command.json) | 0 | 2026-10-30T06:07:47.503617+00:00 |
| [integration-merge-preview](integration-merge-preview.command.json) | 1 | 2026-10-30T06:07:48.053073+00:00 |
| [compare-new-main-catalog](compare-new-main-catalog.command.json) | 0 | 2026-10-30T06:09:49.561706+00:00 |
| [verify-original-work-preserved](verify-original-work-preserved.command.json) | 0 | 2026-10-30T06:11:07.789315+00:00 |
| [solution-map-before](solution-map-before.command.json) | 1 | 2026-10-30T06:15:15.749900+00:00 |
| [create-ci-correction-worktree](create-ci-correction-worktree.command.json) | 0 | 2026-10-30T06:15:22.070961+00:00 |
| [solution-configuration-before](solution-configuration-before.command.json) | 0 | 2026-10-30T06:15:48.662773+00:00 |
| [add-fixture-to-solution](add-fixture-to-solution.command.json) | 0 | 2026-10-30T06:16:06.702826+00:00 |
| [solution-configuration-after](solution-configuration-after.command.json) | 0 | 2026-10-30T06:16:06.839364+00:00 |
| [inspect-actual-reference-configuration](inspect-actual-reference-configuration.command.json) | 1 | 2026-10-30T06:17:29.695385+00:00 |
| [preserve-solution-crlf](preserve-solution-crlf.command.json) | 0 | 2026-10-30T06:18:26.484475+00:00 |
| [correction-whitespace](correction-whitespace.command.json) | 0 | 2026-10-30T06:18:26.572456+00:00 |
| [correction-commitlint](correction-commitlint.command.json) | 0 | 2026-10-30T06:18:26.659212+00:00 |
| [inspect-sdk-configuration-task](inspect-sdk-configuration-task.command.json) | 1 | 2026-10-30T06:18:53.860761+00:00 |
| [inspect-sdk-configuration-task-v2](inspect-sdk-configuration-task-v2.command.json) | 0 | 2026-10-30T06:19:35.910496+00:00 |
| [stage-ci-mapping-correction](stage-ci-mapping-correction.command.json) | 0 | 2026-10-30T06:19:43.304316+00:00 |
| [commit-ci-mapping-correction](commit-ci-mapping-correction.command.json) | 0 | 2026-10-30T06:19:43.431789+00:00 |
| [fetch-before-correction-push](fetch-before-correction-push.command.json) | 0 | 2026-10-30T06:19:54.673716+00:00 |
| [push-ci-correction](push-ci-correction.command.json) | 0 | 2026-10-30T06:19:56.227077+00:00 |
| [dispatch-corrected-candidate](dispatch-corrected-candidate.command.json) | 0 | 2026-10-30T06:20:20.854661+00:00 |
| [list-corrected-runs](list-corrected-runs.command.json) | 0 | 2026-10-30T06:21:03.114439+00:00 |
| [summarize-first-ci-evidence](summarize-first-ci-evidence.command.json) | 0 | 2026-10-30T06:22:17.110852+00:00 |
| [final-integration-preview](final-integration-preview.command.json) | 0 | 2026-10-30T06:23:19.824904+00:00 |
| [refresh-remote-integration-state](refresh-remote-integration-state.command.json) | 0 | 2026-10-30T06:24:20.919865+00:00 |
| [reproduce-original-two-processor-race](reproduce-original-two-processor-race.command.json) | 0 | 2026-10-30T06:26:31.332245+00:00 |
| [fix-worker-setup-timer](fix-worker-setup-timer.command.json) | 0 | 2026-10-30T06:27:13.432007+00:00 |
| [build-worker-setup-debug](build-worker-setup-debug.command.json) | 0 | 2026-10-30T06:27:13.525873+00:00 |
| [summarize-second-ci-evidence](summarize-second-ci-evidence.command.json) | 0 | 2026-10-30T06:27:53.578234+00:00 |
| [corrected-two-processor-classes](corrected-two-processor-classes.command.json) | 0 | 2026-10-30T06:28:21.217525+00:00 |
| [close-worker-test-cleanup](close-worker-test-cleanup.command.json) | 0 | 2026-10-30T06:30:32.085124+00:00 |
| [build-reviewed-worker-setup-debug](build-reviewed-worker-setup-debug.command.json) | 0 | 2026-10-30T06:30:32.175018+00:00 |
| [reviewed-two-processor-classes](reviewed-two-processor-classes.command.json) | 0 | 2026-10-30T06:31:14.068397+00:00 |
| [worker-timer-commitlint](worker-timer-commitlint.command.json) | 0 | 2026-10-30T06:31:37.118003+00:00 |
| [stage-worker-test-correction](stage-worker-test-correction.command.json) | 0 | 2026-10-30T06:31:37.406468+00:00 |
| [commit-worker-test-correction](commit-worker-test-correction.command.json) | 0 | 2026-10-30T06:31:37.499136+00:00 |
| [fetch-before-worker-correction-push](fetch-before-worker-correction-push.command.json) | 0 | 2026-10-30T06:31:45.221172+00:00 |
| [push-worker-correction](push-worker-correction.command.json) | 0 | 2026-10-30T06:31:46.509286+00:00 |
| [dispatch-reviewed-candidate](dispatch-reviewed-candidate.command.json) | 0 | 2026-10-30T06:32:12.307929+00:00 |
| [list-reviewed-runs](list-reviewed-runs.command.json) | 0 | 2026-10-30T06:32:55.688886+00:00 |
| [verify-published-tool-versions-current](verify-published-tool-versions-current.command.json) | 0 | 2026-10-30T06:34:04.612700+00:00 |
| [compare-latest-main-catalog](compare-latest-main-catalog.command.json) | 0 | 2026-10-30T06:34:04.883137+00:00 |
| [summarize-successful-ci-evidence](summarize-successful-ci-evidence.command.json) | 0 | 2026-10-30T06:36:44.134094+00:00 |
| [fetch-final-owner-state](fetch-final-owner-state.command.json) | 0 | 2026-10-30T06:37:21.725672+00:00 |
| [final-owner-identities](final-owner-identities.command.json) | 0 | 2026-10-30T06:37:55.765942+00:00 |
| [final-work-preservation](final-work-preservation.command.json) | 0 | 2026-10-30T06:37:55.938493+00:00 |
| [check-unapplied-host-alignment](check-unapplied-host-alignment.command.json) | 0 | 2026-10-30T06:43:00.604336+00:00 |
