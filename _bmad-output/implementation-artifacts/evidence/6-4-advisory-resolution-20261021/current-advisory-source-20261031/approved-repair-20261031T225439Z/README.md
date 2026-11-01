# Approved current-source computed-query repair receipt

The computed response identity repair passes the actual enforced gateway and all Projects tests. Full affected-project qualification remains incomplete because owner-wide host/enrollment tests expose separate current SDK composition gaps. Canonical Story 6.4 and sprint remain **in-progress**.

## Executed source and footprint

All seven approved candidate files match `../proposed-repair.json`. Projects list/open/advisory, Conversations advisory and Folder owner-read results omit fabricated `ProjectionType`; request discriminators and actual persisted projection identities remain. `SupportedProjectReadTests.cs` adds successful list/open protocol assertions, three catalog-refusal cases and SHA-256-only physical snapshots. The separately authorized fixture compatibility change makes only its embedded test issuer Development with `NonconformingDevelopmentFixture=true`; gateway, Projects and Folders receivers remain Production-configured. Existing policies, catalog authority, signed claims and credentials remain enforced. This fixture supplies no deployment acceptance.

`direct-source-inventory.json` identifies the root's 12 direct source revisions and checkout/gitlink equality. `isolated-source-inputs.json`, `preservation-final.json` and compressed before/after source inventories bind real and disposable source bytes. All builds ran in disposable shared-object clones; no nested submodule was initialized, and no shared Parties build or edit occurred.

## Builds and producer mappings

The current Builds CLI was built from source in the isolated checkout. Separate public `prepare-build --manifest <owning module/hexalith.module-manifest.v2.json> --mode source --workspace <fresh temporary directory> --output json` commands prepared Projects and Conversations. Their complete emitted environments are retained in `projects-environment.json` and `conversations-environment.json` and applied unchanged. The generated imports are copied unchanged; `generated-inputs.json` and `generated-inputs-final.json` prove they stayed identical. Producer executable hashes are retained. HXW009/HXW010/HXW011 source warnings remain warnings; package identity guards were not changed.

Individual Debug builds passed for Projects Contracts.Tests, Projects.Tests, Projects Server.Tests, and Conversations Server.Tests. Initial Server build CA2007 is preserved; the new fixture lifetime was corrected with ConfigureAwait(true). Folders has no enrolled v2 manifest. Its supported standalone Debug source build fails CS1704/MSB3243 because the Memories standalone package fallback brings Commons.UniqueIds 2.30.7 alongside current source 1.0.0. The narrow `--no-restore -p:BuildProjectReferences=false` owner test-project build succeeds against exclusively freshly built isolated dependencies (`folders-fallback-inputs.json`); the broad build failure remains a blocker. No Projects mapping or old passing binary was substituted for an owner build.

Every `*.command.json` contains exact argv, cwd, safe explicit environment, UTC timestamps, exit and stream hashes. Test streams and private runtime logs remain outside this public evidence directory. Native xUnit XML is retained; no failing or skipped acceptance case was relabelled.

## Selected final full-project results

| Test project | Passed / total | Failed | Skipped | Selected native XML |
| --- | ---: | ---: | ---: | --- |
| Projects.Contracts.Tests | 230 / 230 | 0 | 0 | `projects-contracts-full.xml` |
| Projects.Tests | 684 / 684 | 0 | 0 | `projects-core-full.xml` |
| Projects.Server.Tests | 1309 / 1309 | 0 | 0 | `snapshot-final-projects-server-full.xml` |
| Conversations.Server.Tests | 1429 / 1508 | 79 | 0 | `conversations-server-full.xml` |
| Folders.Server.Tests | 1245 / 1283 | 38 | 0 | `folders-server-full.xml` |

The Projects total is **2223 passing cases**. All full affected suites executed **5014 cases: 4897 passed, 117 failed, zero skipped/not-run**. All selected executable closures remained unchanged throughout execution. `selected-test-results.json` and `execution-matrix.json` identify the exact selected runs and class/method coverage; focused runs overlap full suites and are not added to these distinct totals.

Focused evidence also passes: advisory contracts 8; engine/mappers 107; Conversations advisory/strict/scoring-label 119; Folder owner-read 30; final read/authority 115; existing SDK bootstrap guards 2. Final full Server XML covers ProjectAdvisoryQueryTests (139), delegated Conversation directory (13), supported HTTP (4), module registration (16), SupportedProjectReadTests (73), the gateway acceptance (1), and all other Server cases.

The first full Server run failed 45 cases before assertions because the embedded issuer ran in Production (42 existing authority tests and three new catalog controls). That failure and the intermediate passing full run are preserved. Only the separate test issuer was moved to the supported explicit Development configuration; the final full run passes on the final source/binary. Production+fixture=true and Development+fixture=false still refuse under the two passing existing SDK guard cases.

## Actual gateway and persisted observations

The existing authenticated gateway/JWT/delegated owner/Dapr storage acceptance passes all six immutable fixture routes. Conversation suggestions retain scores [50,20], Folder attachments score 45, and unsupported Files remain incomplete with null result and unsupported kind `file`. No selection is minted. Missing/denied Conversation results stay safely indistinguishable, current Folder ACL changes remain authoritative, malformed/failed owners remain bounded, and a missing signed surface claim returns Forbidden.

The existing gateway test compares five real physical state records before/after advisory execution in-process; raw values are not exported. The three catalog controls record unchanged SHA-256 bindings of all 14 physical key/byte/ETag observations per refusal in `zero-write-snapshots.json` and final native XML. Caller-selected projection, actor and uncommitted query are refused before writes. Existing inspected list/open durable audit checks remain; the zero-write claim covers advisory and refused queries, not authorized descriptive audit records.

Each runtime used command-owned loopback Redis and Dapr with a unique app ID, no shared application teardown, and readiness checks. Exact binary/version provenance, argv and owned PIDs/start identities are retained. `cleanup.json`, `authority-corrected-cleanup.json`, and `snapshot-final-cleanup.json` show orderly owned process exits and absent listeners. Disposable build checkouts/private execution logs are retained temporarily for review at the command-owned path in `workspace.json`; no background runtime remains.

## Remaining corrections and independent gates

- Conversations: 19 SelectionRecovery + 33 RebuildCompletion + 22 NativeEnrollment failures are receiver 503 with `Reason=configuration-missing`. `tests/Hexalith.Conversations.Server.Tests/ResolutionMetadata/ResolutionEnrollmentFixture.cs` must enroll current committed receiver catalog authority, exact host identity and invocation bindings before its endpoint-delegate tests can exercise projection behavior. Do not replace the current guard with an allowing fake.
- Conversations deletion runtime: five failures in `tests/Hexalith.Conversations.Server.Tests/Agents/ConversationDeletionRuntimeHostTests.cs`. One handler assertion must inspect the redirect-disabled inner transport under the SDK's RouteCatalogInvocationHandler. Two host starts report an unnamed protected participants endpoint in production composition; two pump/receipt scenarios encounter missing configured invocation transport. Review the owning `src/Hexalith.Conversations.Server/Api/ConversationCommandApi.cs` policy and `src/Hexalith.Conversations.Server/Agents/ConversationDeletionRuntime*.cs` transport/catalog enrollment with its host tests. The latter paths are located exactly in `remaining-corrections.json`.
- Folders: its standalone source dependency graph needs owner-supported producer enrollment that removes mixed source/package duplicate assemblies. Four full-suite fixture classes (endpoint registration 1, host composition 5, callback boundary 26, repository startup 6) fail because `src/Hexalith.Folders.Server/FoldersServerHostComposition.cs` does not register the current HTTP endpoint-source/security composition required when it maps SDK endpoints. Reconcile production composition and host tests together; adding only test registrations could hide the production gap.

These separate corrections are deliberately not applied in this bounded response-metadata repair. Full affected-project acceptance/task remains incomplete. Producer security/npm qualification, Platform publication, deployed policy/runtime and consumer adoption remain independent; the Parties publication hold remains active.

## Preservation and validators

`preservation-final.json` verifies real HEAD/tree/symbolic HEAD/index/refs/gitlinks unchanged; only the nine authorized source/test paths changed. It compares all 216 pre-existing current-source evidence files and all 8935 shared Parties files without changes. Reversing only the authorized issuer compatibility edit reconstructs the exact original dirty fixture bytes; the other original dirty fixtures are byte-identical. `canonical-state-preservation.json` binds unchanged canonical story/sprint bytes. Both production-authority validators and all three owning-repository whitespace checks pass. New untracked spec/evidence no-index checks pass with expected diff exit 1 and no whitespace diagnostics. The archived authority compatibility patch and its exact diagnostic streams have a narrow whitespace exemption because its mandatory blank context-line prefix is patch syntax; its bytes remain hash-bound. The initial no-index patch-context diagnostic is retained. The initial audit's two filtering mistakes are retained separately and corrected explicitly; they did not represent source or Parties changes.

`SHA256SUMS` binds every retained public artifact except itself, ignored Python bytecode and temporary review scratch. No credentials or private runtime logs are included.
