Current final result: **2,690 passing tests**, both review corrections resolved; see [final parent verification](parent-final/README.md). The checkpoint below predates the final corrections and is retained as history.

# Story 6.4 fourth-pass local verification

All fourth-pass local implementation, regression and documentation patches are complete. Conversations publication and its consumer pin remain pending separate approvals. No Git staging, commit, push, branch, dependency-version update or deployment was performed.

Projects baseline: `0247ccb999d788a6d92984362e5717955d58caa8`. Conversations baseline: `71fab44b7dd664699cedbbfc17ed0d7822e8bdb9`. The original spec baseline fields are preserved. The frozen scope block is byte-identical to HEAD; its hash and all 36 changed/new source, configuration and documentation hashes are in [source-manifest.json](source-manifest.json).

| Current check | Passed | Failures/skips |
| --- | ---: | ---: |
| Projects.Server full, including actual gateway/owner/store integration | 1,085 | 0/0 |
| Conversations.Server full | 1,162 | 0/0 |
| Projects.Contracts full | 202 | 0/0 |
| Resolution engine, mappers and aggregate confirmation | 107 | 0/0 |
| Client generation | 101 | 0/0 |
| Planning guard | 29 | 0/0 |
| Total distinct passing tests | 2,686 | 0/0 |

Focused executions are subsets: Projects advisory and legacy routes 189; Conversation advisory, registration, strict owner and scoring classification 113; corrected worker control 2. Individual affected Debug builds and the package-mode Projects.Server Release build passed with zero warnings/errors. The e2e TypeScript check passed after `CI=1 npm ci --ignore-scripts`; the existing lockfile is unchanged.

[results.json](results.json), [executed-tests.json](executed-tests.json), [matrix.json](matrix.json) and [patch-coverage.json](patch-coverage.json) bind these counts and all seven frozen matrix rows to actual passing XML. [commands.json](commands.json) retains exact commands, working directories, exit codes and the controlled integration endpoints. Runner summaries are archived; runtime logs stay in the named private `/tmp` directories.

The current Projects receipt is [projects-full-current.xml](projects-full-current.xml); [conversations-full.xml](conversations-full.xml), [contracts-full.xml](contracts-full.xml), [engine-final.xml](engine-final.xml) and [client-generation.xml](client-generation.xml) provide the other final suites. Earlier failing receipts remain historical and are superseded by the current receipt.

The fresh integration uses synthetic fixture JWT authority at the real gateway, the real signed owner transport and owner handlers, controlled Tenant/Folder authority projections, and actual isolated Dapr/Redis persistence. It asserts every observed snapshot is nonempty before comparing unchanged persisted bytes. [fixture-current.json](fixture-current.json) records the uniquely owned resources and successful Dapr/container cleanup. It does not touch shared resources or establish deployed acceptance.

Initial compile failures were fixed in the test code (two migrated Folder-fake callers, one ConfigureAwait and one missing namespace). Early full Projects runs exposed a transitive source/package assembly-version split for Commons.UniqueIds. A complete source build with explicit root-declared EventStore, Commons and Tenants roots fixed it; the corrected worker control and full Projects suite pass. No production build policy or gate was weakened. Owner whitespace checks use `core.whitespace=cr-at-eol` to recognize the required existing CRLF endings; raw CRLF false positives remain recorded. The first e2e typecheck lacked local dependencies; locked restore resolved it without changing package versions.

Folderless Projects now rely on exact Project Read authority with the final recheck. Bound Folders still require Included state and actual owner acceptance. Conversation scans retain only scoring candidates before Folder checks. Legacy inactive/invalid Folders are definitive exclusions, Files never score, and final scope failures use consistent mapping. Advisory owners accept ordinary exact allowed Tenant decisions, while the strict native query remains fail-closed. Null stored/scoring labels project complete absence; unclassified text remains incomplete.

Conversations 1.3.1 candidate notes are in `references/Hexalith.Conversations/docs/release-notes-1.3.1.md`. Existing unlabeled rows benefit after reprojection or their next update; queries perform no repair. No Conversations publication, Builds package pin, root gitlink update or `tools/release-packages.json` update was performed. Unsupported Files, the live AppHost browser lane, production OIDC binding, common G-4 runner qualification and deployment acceptance retain their explicit limitations. The parent workflow audit/review is the next step.
