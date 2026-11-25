---
title: 'DW-63 Migrate Projects Server to the EventStore SDK host'
type: 'refactor'
created: '2026-11-24'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 1
baseline_commit: '5f1d955b13c69a9c24b6a6c2d185770da2197355'
context: ['{project-root}/module/README.md', '{project-root}/docs/read-cutover.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Projects owns its `/process`, `/project` and `/query` mappers. This leaves its Server outside the SDK’s route validation and canonical catalog dispatch. The current SDK sends HTTP 200 for query failures, which changes both the Projects transport and how the Gateway consumes failures.

**Approach:** Adopt assembly-explicit `AddEventStoreDomainService` and `UseEventStoreDomainService` for the Server. Add opt-in query response mapping in EventStore, then qualify the Projects host with its existing keyed processor, historical fold and supported read behavior.

## Boundaries & Constraints

**Always:** Preserve the v1 REST and client contracts, query and command discriminators, event history, state keys, both projection stores, audit effects and cursor scope. Require operation-specific workload, sidecar and catalog admission on SDK callbacks. Retain human principal and current owner checks. Keep the existing command processor as the single writer during host qualification.

**Cutover authority:** This work covers source changes and isolated verification. Story 7.1 writer cutover requires AD-17 replay and command equivalence, writer drain and fencing, AD-35 exclusive claims and executable rollback. The approved recovery for a writer that loses the retained floor is mutation freeze and forward recovery. AD-25 governs runtime retirement.

**Package mode (human decision, 2026-11-25):** The ordinary local AppHost composes catalog admission only from a local Debug source build. A package-mode ordinary run refuses at startup with an error that names this requirement, so SDK callbacks never start without a committed catalog. Package-mode catalog composition is separate deferred work.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Failure Behavior |
|---|---|---|---|
| Query response | Valid call, invalid envelope or cursor, missing or unauthorized resource | Same success payload, 400 invalid input for the six existing failure-normalized queries and indistinguishable 404 denial | Other query failures retain their existing 404 mapping. Unavailable administrator verification retains the SDK 503. |
| Command dispatch | Historical state and compatible command envelope | Existing wire events and metadata for success/rejection, empty events for an idempotent no-op | Unauthorized or unadmitted requests cannot dispatch or append. |
| Projection and replay | Ordered history and named delivery with a valid catalog | Same legacy fold and named read-model contents, including `ProjectFolderCreationPending` history | Wrong tenant/domain, invalid catalog or replay input fails closed. Duplicate delivery preserves the persisted end-state. |

</frozen-after-approval>

## Code Map

- `references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs` owns the canonical routes, endpoint policies and query response. Behavior confirmed in source `204cc0eb990e3bfa7d806eeb9a84fcdfd9252d9f` and package `5.5.1`. `DomainServiceRequestRouter` uses keyed `IDomainProcessor` and requires a separate replay registration. `DaprDomainQueryInvoker` maps receiver 404/403 to safe denial and forwards error details from successful HTTP responses.
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs` holds the keyed processor, query registrations and failure mapping. Reuse `ProjectQueryEnvelopePrincipalBinding`, `ProjectsDomainProcessor` and `ProjectStateApply`. Preserve checks of the original authenticated query when the SDK normalizes administrator flags.
- `src/Hexalith.Projects.Server/ProjectsDevelopmentCatalogExtensions.cs` registers synthetic discovery. Replace it with SDK discovery plus the existing processor payload inventory. Reuse the named handlers in `Projections/ProjectReads/` and `Projections/ConversationStartSetup/`.
- `src/Hexalith.Projects.AppHost/ProjectsLocalDevelopmentCatalog.cs` composes the ordinary catalog in source mode. Its package-mode branch keeps the startup refusal. Reuse the byte check in `SupportedProjectReadDevelopmentInfrastructure.Load`.

## Tasks & Acceptance

**Execution:**

- [ ] `references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs` — add an opt-in response policy after authorized query dispatch with unchanged default SDK behavior. Test policy invocation, authorization ordering and administrator-verifier unavailability in the EventStore owner repository. Consume the approved source and package through existing dependency ownership.
- [ ] `src/Hexalith.Projects.Server/Program.cs`, `ProjectsServerServiceCollectionExtensions.cs` and `ProjectsDevelopmentCatalogExtensions.cs` in that directory — register the SDK once, move query response mapping into the compatibility policy, and let the SDK map canonical endpoints and health. Keep the retained HTTP routes under explicit human authorization and qualify both normal and private catalog profiles with real catalog admission.
- [x] `src/Hexalith.Projects.Server/ProjectProjectionHandler.cs` and `ProjectsDomainProcessor.cs` in the Server directory — register the legacy full replay projection as an SDK handler and expose aggregate reconstruction through the SDK replay seam. Compare ordered history, nested snapshots and exact wire outcomes against the baseline.
- [ ] `src/Hexalith.Projects.AppHost/Program.cs`, `module/hexalith.module-manifest.v2.json`, `module/README.md` and `tests/Hexalith.Projects.Server.Tests/` — supply SDK catalog admission and peer binding in the ordinary local topology. Reconcile declared callbacks and catalog metadata with the host. Cover missing or wrong credentials, actor bindings, route ownership, statuses and persisted effects. Enroll additional callbacks only with scoped grants.

**Acceptance Criteria:**

- Given the actual Server, when the approved catalog is admitted, then each canonical receiver has one SDK route and only the intended caller, operation and audience can reach its handler.
- Given golden histories, when both hosts process the same envelopes and deliver or rebuild projections, then replayed state, command wire results and persisted projections agree at their original sequences and global positions.
- Given the authenticated read fixtures, when the Gateway and retained clients read the candidate, then v4 comparison and rollback receipts pass for all seven supported operations and store deltas match the required effects.

## Implementation Notes

- The Projects executable maps the SDK receiver and keeps the existing `projects` keyed command processor. `/replay-state` and legacy `/project` use the same stored-event fold. The private profiles accept only exact POST grants, including where Dapr ACL prefix matching can reach an auxiliary route. Positive admission in the ordinary AppHost awaits verification with owner-held Redis catalog custody and a bound workload issuer. [Local configuration](../../module/README.md)
- Working-tree source evidence includes 7 EventStore response-policy and catalog-fence cases, 252 focused Projects Server passes, the separately executed Dapr restart case, 2 domain history cases and client compatibility. The authenticated v4 comparison passed 77 cells and the separate rollback invocation passed its matrix and all 7 retained reads with zero domain writes. The matrix manifest binds the working-tree files and identifies the Git revision as the baseline. [SDK tests](../../artifacts/dw-63/eventstore-fence-and-response.xml) · [Focused tests](../../artifacts/dw-63/source-focused-scoped-candidate.xml) · [Dapr restart](../../artifacts/dw-63/source-persisted-restart.xml) · [v4 packet](../../artifacts/dw-63/v4-fixture-control/v4-packet.json) · [Rollback receipt](../../artifacts/dw-63/v4-fixture-control/v4-rollback-proof.json)
- Full-suite verification at `280b1eed` (EventStore `208af26a`, Folders `d56e868f`, Conversations `b1c94eb7`) found migration regressions that the focused lanes missed. The fixes are in tests. The authority fixture removes the SDK's outbound HTTP resilience, as the comparison fixture already does, so per-request faults stay deterministic. Direct receiver calls present the committed catalog lease. A tenant-scoped Projects receiver catalog now refuses a tenant altered in transit before envelope dispatch, and the Gateway reports that refusal as its bounded catalog 503 instead of the safe 404. With the AppHost's wildcard Projects registration, the envelope binding still returns 404. The workload-credential topology tests use the published composition because ordinary run mode requires owner custody. With a disposable daprd, Server 1634, Integration 216, domain 693, Client 167, Contracts 208, UI 310, Cli 22 and Mcp 35 tests pass. EventStore DomainService ran 1440 tests with 1 skip; its one timing-sensitive `ProcessPublicationTests` failure passed on the single allowed rerun. The 77-cell matrix and the seven retained rollback reads pass again against the recorded v4 packet. [Full-suite receipts](../../artifacts/dw-63/full-suite-20261124/)
- An unconfigured ordinary `aspire run` now stops at the custody requirement. This includes the ordinary-startup phase of the managed live lane, which needs ordinary catalog custody and a bound workload issuer for its fresh backend.
- The final comparison fixture uses direct Conversation fault-injection transport to prevent circuit state from one cell influencing another. The normal application registers SDK resilience. [Read cutover verification](../../docs/read-cutover.md)
- The live narrow run recorded committed catalog generations for Projects, EventStore, Tenants and Folders. Its distributed build timed out at 600 seconds. The retry, `run-with-temp bash -c 'exec python3 tools/qualification/run_supported_project_reads.py --regressions --timeout 1500 --artifacts "$TMPDIR/distributed-build" --evidence artifacts/dw-63/live-regressions-long'`, built the Integration harness and exited 1. EventStore returned HTTP 500 for `Hexalith.Folders.Commands.CreateFolder` during fixture seeding, before any Projects business callback. The owning invoker logged a domain-service invocation failure. The downstream Dapr detail was unavailable. The runner verified removal of its AppHost, containers and sidecars. [Live catalog commits](../../artifacts/dw-63/live-catalog-commits.log) · [Owner failure trace](../../artifacts/dw-63/live-regressions-long/owner-error-trace.json) · [Native test result](../../artifacts/dw-63/live-regressions-long/duplicate/distributed.xml)
- At `280b1eed`, the same retry from a clean clone completes `CreateFolder` and then times out waiting for the Folders named seed delivery. The `folders-workers` host logged `OperationalStatusUnavailableException` in both the run and its single retry. No Projects business callback ran. [Live receipts](../../artifacts/dw-63/full-suite-20261124/live-regressions-head/) · [Retry](../../artifacts/dw-63/full-suite-20261124/live-regressions-head-retry/)
- Release package-mode compilation fails with `CS0246` for `IDomainQueryResponsePolicy` against EventStore 5.5.1. The SDK change is on EventStore `main` as `208af26a`. Its release and the Builds `HexalithEventStoreVersion` pin, still 5.5.1, remain required before DW-63 acceptance. [Release build output](../../artifacts/dw-63/release-package-server-build.log)
- Review loop 2 verification ran in a clean clone at `31b35637` with the working-tree edits applied (EventStore `208af26a` plus its uncommitted edits), against disposable daprd sidecars. Results: Server 1,715, Integration 216, domain 693, EventStore DomainService 1,439 with 1 skip, the 77-cell matrix, all 7 retained rollback reads against packet `294c8631…`, and the production-authority index check. Four setup gaps produced first-run failures, and each passed when rerun with corrected setup:
  - the sidecar needed the `projects-conversation-start-setup` store;
  - the EventStore guardrail tests must be built inside the EventStore tree;
  - the owner-metadata tests need prebuilt owners;
  - those tests also need one build layout.

  The live narrow lane was not rerun; its Folders seeding blocker is unchanged. [Loop 2 receipts](../../artifacts/dw-63/full-suite-20261125-loop2/)
- The [Epic 7 entry verification](evidence/epic7/entry-blockers-2026-11-25.md#dw-63--projects-sdk-host) records checks against the current gitlinks and the evidence required for Projects and EventStore owner acceptance. DW-63 remains open.

- Follow-up on Projects baseline `56667abee72e176a303f7c219f93a94978cfdeda`. Ordinary, isolated and package fixture catalogs grant the EventStore caller exact POST access to `/project/rebuild/part/v1` and `/project/v2/reconcile`. The restricted SDK receiver test confirms the manifest grants match its actual admitted endpoint inventory. Complete large-rebuild and terminal-reconciliation runs are still due.
- The local NuGet cache contains EventStore `5.6.0` from source `295b5a6ba584167f21cfef40787477b8814e32da` with the required response policy. EventStore alone produced `NU1109` because its dependencies require Commons `3.0.3`. The aligned candidate also exposed that the invalid-cursor test referenced a source-only fixture in the Release test assembly. The test now runs in the source authority suite. The checked-in Builds selection is still `5.5.1` and requires an owner catalog advance.
- Source verification passed 170 Server tests and 35 AppHost topology tests with zero skips. The candidate Release Server build and all 1,648 Server tests passed with zero skips. The Release build used `CI=true dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj -c Release -m:1 -p:UseHexalithProjectReferences=false -p:HexalithEventStoreVersion=5.6.0 -p:HexalithCommonsVersion=3.0.3` in a disposable checkout. The Server runs used a disposable Dapr sidecar and the existing read-store components.
- The working-tree candidate passed all 77 authenticated v4 cells and a separate execution of all seven retained rollback reads. The v4 packet SHA-256 is `319207d3dfea0c5bf9c36a77fae0c03202931feb5f95c5a82efbd8affe7b3c32`. The rollback proof SHA-256 is `0781582e9db7331a2aef3219f8691bbda20982dbf00271ee077b40b4f9cb9a1e`. Native test receipts, build output and the packet are in `~/.local/state/hexalith-projects/dw-63-candidate-5.6.0/`.
- The live duplicate scenario repeated the Folders named seed delivery timeout before any Projects callback. The follow-up source fixture now gives the Folders server and worker separate issuer credentials and source scan keys scoped to the two fixture tenants and the Folders domain. The worker's ordinary stream read is metadata-only. The fixture mirrors the package topology's native-source enrollment. The live runner's process cleanup also handles an exited process reported as `ESRCH`. The launcher suite passed 167 tests and 660 subtests.
- The repaired live fixture passed the duplicate delivery scenario. It persisted both owners' Folder and Project seed results and exercised duplicate delivery against that state. The independent full-replay group exited 1 while waiting for the second Tenant's exact Projects worker projection. The coordinator stopped before the main group. Logs and native results are under `live-scoped-folders/regressions` in the local receipt directory.
- Review added a hosted historical pending-Folder replay test and typed replay failures for invalid identities, incomplete reference metadata and contradictory rejection scope. It restored standalone runtime issuer registration and wired the ordinary AppHost's configured human authority receipt secret. Both private Dapr directory cleanup attempts run even when catalog disposal fails. Ordinary owner credentials and persisted end-to-end proof remain open.
- The post-review package candidate passed 1,650 Server tests with zero failures or skips using EventStore 5.6.0 and Commons 3.0.3. The native test result and build log are under `release-final` in the local receipt directory.
- The post-review mapped Debug build passed. The focused source Server suite ran 320 tests with zero failures or skips, including catalog admission, hosted replay, historic payload checks, caller authentication and durable query authority. The native result and build log are under `source-final`.

- The independent HTTP comparison ran the legacy mappings at `5f1d955b13c69a9c24b6a6c2d185770da2197355` and the reviewed SDK candidate in separate disposable Release checkouts with the same EventStore 5.6.0 and Commons 3.0.3 package selection. It retained the legacy full projection as the nested snapshot input and compared create, exact retry, rejection and the historical pending Folder projection at both endpoints. Both tests passed with zero skips. The legacy wire receipt SHA-256 is `ebb25978be8256dfbaf8f866f6b61ffff93bb17b2fd4c28bcc681c2a767333f9`. The two fixtures, native results and build log are under `independent-wire-retry`.

- The separate full-replay retry used `python3 tools/qualification/run_supported_project_reads.py --scenario full-replay --timeout 600 --artifacts "$TMPDIR/distributed-build" --evidence "$receipt_dir/full-replay"` under `run-with-temp`. The disposable Sentry health check timed out until its owned resource was restarted. The retry completed seed, all four full rebuilds, persisted state comparisons and the 400 stale-cursor check. It exited 1 in the final fresh paging check when the second page returned HTTP 200 with `FolderVisibility` reporting `FolderUnavailable`. The native result SHA-256 is `38f8cf9db253771c469542713ade91b588b271ca7a0696926b940b806a871bac` under `live-full-replay-retry/full-replay`. The runner verified cleanup of the owned AppHost and containers. The main live regression group remained gated on full-replay success.

## Spec Change Log

- 2026-11-24, step-03 matrix audit, human decision. The I/O matrix row "missing or unauthorized resource → indistinguishable 404" does not cover a Tenant altered on the internal Gateway-to-Projects call under a single-Tenant receiver catalog. The SDK receiver's catalog check refuses that call before any Projects code runs, and the Gateway returns its bounded catalog 503. That body is identical whether or not the Project exists, and AD-19 names no status for catalog refusals. Real catalogs bind Projects to every Tenant (`*`), so the same tampering fails the signed-digest check and returns the safe 404. `SupportedProjectAuthorityHttpTests` therefore expects 503 for that one case. Rejected: an EventStore Gateway 404 mapping (shared behavior and another release) and an all-Tenant authority fixture (full-suite rerun for no product gain). Frozen text unchanged. KEEP: the other tamper cases still assert the public 404 through the digest check.
- 2026-11-25, step-04 review loop 1, human decision. Group A (BH-1, EC-5, EC-17, VG-7) asked what package mode does when it cannot compose a catalog. The SDK receiver refuses every canonical callback with 503 until a catalog generation is committed, and it requires `APP_API_TOKEN` in every environment. The pre-DW-63 ordinary composition provided neither, and Tenants is already an SDK host, so a fallback would start a topology that serves no callback. CI cannot use source mode (`Directory.Build.props` rejects project references with `CI=true`). The scheduled E2E lane already stops before AppHost start because its test-user secret is empty. Decision: keep the startup refusal and name the real requirement in its message; defer package-mode catalog composition. The pushed code was patched in place rather than reverted, because the gap was a missing decision and re-derivation would reproduce the same code. Frozen text: added the package-mode decision. KEEP: every other part of the reviewed implementation.

## Review Triage Log

| ID | Finding | Verdict | Evidence | Route |
|---|---|---|---|---|
| BH-1 | Ordinary run-mode AppHost throws in package mode; error names Debug, not project-reference mode | high | `Program.cs` calls `ProjectsLocalDevelopmentCatalog.Add` in every ordinary run; its `#else` throws, and the AppHost csproj defines `SUPPORTED_PROJECT_READ_SOURCE` only when `UseHexalithProjectReferences=true`. | intent_gap (A) |
| BH-2 | `projects-workers` is not enrolled in the ordinary catalog | maybe-false | The catalog builder selects every sidecar app but throws only for shared app IDs. Settle with a positive-admission run under real custody: does bootstrap or the workers ACL depend on enrolment? | defer (unverified medium) |
| BH-3 | New `Load` helper omits the loaded-bytes check and hard-codes Debug | low | `SupportedProjectReadDevelopmentInfrastructure.Load` verifies the loaded bytes against the file; the new helper does not, so a mismatch surfaces later as an admission refusal. | patch |
| BH-4 | Catalog and mTLS directories leak if code between `Add` and `try` throws | low | `DisposeAsync` only retains custody and releases its lock, and a startup throw ends the process. `supportedReadCatalog` already used the same pre-`try` pattern. | reject |
| BH-5 | Run-mode composition untested; persistent-secret assertion removed | medium | Same gap as VG-3. | defer (with VG-3) |
| BH-6 | Many tests exercise a test copy of the removed receiver endpoints | medium | 11 test files call the copy `MapProjectsServerEndpoints`; only the `workload: true` branch of `SupportedProjectReadHttpTests.StartAsync` uses `AddProjectsDomainService`. The copy calls `ProjectsQueryResponsePolicy` directly, so its 400 branch is not exercised through the SDK registration. | patch (with VG-2) |
| BH-7 | `GetOriginalQuery` fails open without a verified caller | low | `DomainQueryDispatcher` records `VerifiedQueryCaller` whenever caller authority is bound, which the Gateway requires for public Projects queries. Without it the handler sees the SDK-normalized envelope, so an unverified admin flag runs as a non-admin read; there is no privilege gain. | reject |
| BH-8 | Replay reports unknown type, null payload and malformed requests under imprecise categories | low | EventStore Admin still receives `Failed` with the failed sequence and event type; only the category label differs. | reject |
| BH-9 | Replay tests cover only the success and two failure statuses | medium | Same gap as VG-6. | patch (with VG-6) |
| BH-10 | Anonymous-mode remnants contradict the new docs | low | The option still serves `AddProjectsAuthentication` and tests; the executable refuses it at startup. | reject |
| BH-11 | Invocation scope ignores the caller and fails silently when empty | false | Canonical callbacks also require the EventStore workload caller policy (wrong-caller `/process` returns 401 in `SupportedProjectReadHttpTests`). An empty scope refuses every call, which is fail-closed, and malformed JSON fails startup. | reject |
| BH-12 | `IDomainQueryResponsePolicy` is last-registration-wins and its doc omits that the envelope is the pre-normalization wire form | low | The SDK resolves one policy with `GetService` and passes the bound wire envelope; one doc-comment line corrects the contract before the EventStore release. | patch |
| BH-13 | No check keeps manifest grants and composition grants aligned | medium | The four compositions and the manifest predate this change and had already drifted (`replay-state`); a cross-check is new test infrastructure. | defer |
| BH-14 | Executable service defaults changed (formatted log messages, Development health writer) | low | Log state values are exported as attributes either way; the metadata-only writer applied only in Development. | reject |
| BH-15 | Docs do not explain first-run custody provisioning | low | EventStore `docs/reference/development-route-catalog.md` documents custody provisioning; `module/README.md` does not link it. | patch |
| BH-16 | Fixtures strip resilience more widely than the docs say; no test pins executable resilience | low | The authority fixture removes resilience to keep per-request faults deterministic; `docs/read-cutover.md` describes only the comparison fixture. | reject |
| BH-17 | `AddProjectsDevelopmentCatalog` stays public with an ignored result | low | No caller other than `AddProjectsDomainService` and its tests. | reject |
| EC-1 | Replay identity construction can throw `ArgumentException` and return 500 | low | Blank ids are refused before construction, and stored aggregate ids were created through `ProjectId`. | reject |
| EC-2 | `restrictAuxiliaryToInvocations` refuses sidecar deliveries | low | The classifier returns null for any endpoint outside the POST grants before its sidecar check. Projects Server maps no subscriptions and is the only opt-in host. | patch |
| EC-3 | Catalog leaks if code between `Add` and `try` throws | low | Same as BH-4. | reject |
| EC-4 | A cleanup failure in `finally` skips later deletions and hides the original exception | low | On Linux, `Directory.Delete` succeeds while files are open; at worst temp directories remain. | reject |
| EC-5 | Ordinary AppHost always throws in package mode | high | Same as BH-1. | intent_gap (A) |
| EC-6 | `Load` can return a same-identity assembly from another path; Release layout unsupported | low | Same as BH-3. | patch (with BH-3) |
| EC-7 | An http workload Authority fails only at runtime | low | User-supplied issuer setting; the failure is loud at metadata retrieval. | reject |
| EC-8 | A host-registered `IDomainProcessor` splits `/process` and `/replay-state` | false | Nothing registers `IDomainProcessor` before `AddProjectsServer`; the only registration is in `ProjectsServerServiceCollectionExtensions`. | reject |
| EC-9 | `GetOriginalQuery` fails open without caller authority | low | Same as BH-7. | reject |
| EC-10 | Removing `AddEventStoreWorkloadAssertionIssuer` breaks non-SDK composition | false | The executable always adds the SDK; every non-SDK caller of `AddProjectsServerRuntimeInfrastructure` is a test, and all of them passed in the 1,634-test run. | reject |
| EC-11 | SDK metadata endpoint accepts blank or foreign identity with 200 | low | Only EventStore calls this endpoint, with its registered domain and app identity, and the response carries no protected data. | reject |
| EC-12 | `/project` domain match is now case-insensitive | low | EventStore sends the exact registered domain. | reject |
| EC-13 | Receiver-contract tests validate a test-only copy | medium | Same as BH-6. | patch (with VG-2) |
| EC-14 | Formatted log messages are now exported | low | Same as BH-14. | reject |
| EC-15 | Persistent workload-secret wiring untested | medium | Same gap as VG-3. | defer (with VG-3) |
| EC-16 | Comparison fixture strips resilience only for the Conversation client | maybe-false | Settle by checking whether any cutover cell injects a Folder or owner fault through a client that keeps SDK resilience; the 77-cell run passed twice. | defer (unverified medium) |
| EC-17 | Package-mode users cannot start the ordinary topology | high | Same as BH-1. | intent_gap (A) |
| VG-1 | Advisory global-admin rejection untested on the SDK `/query` path | medium | Pre-verified gap; `ProjectReadAuthorityFixture` already composes `AddProjectsDomainService` with caller-authority binding. | patch |
| VG-2 | Metadata and endpoint tests in `ProjectsServerModuleTests` assert a test copy | medium | Pre-verified gap. | patch |
| VG-3 | Ordinary run-mode composition has no model test; persistent-secret assertion removed | medium | Pre-verified gap; run mode needs owner custody and Dapr mTLS before a model test can run. | defer |
| VG-4 | SDK observation filter for `restrictAuxiliaryToInvocations` is not exercised | medium | Pre-verified gap in the EventStore owner tests. | patch |
| VG-5 | Startup refusal of anonymous Development mode is untested | medium | Pre-verified gap. | patch |
| VG-6 | Replay failure categories and failed sequence are unpinned | medium | Pre-verified gap. | patch |
| VG-7 | Scheduled CI E2E lane fails at AppHost start | high | The `e2e` job runs `aspire start` with `CI=true`, and `Directory.Build.props` then selects package mode, which reaches the throwing branch. | intent_gap (A) |
| VG-8 | `/project/v2` and `/project/rebuild/v1` error mapping changed | false | `NamedProjectionDispatchCoordinator` branches only on `IsSuccessStatusCode`, so a 500 takes the same path as the former 503 and 400. | reject |

Group A (BH-1, EC-5, EC-17, VG-7): the ordinary topology composes catalog admission only from a project-reference build, so package mode, Release `aspire run` and the scheduled E2E lane stop at startup. The frozen intent requires catalog admission on SDK callbacks but does not say how package mode composes or forgoes it. Resolved 2026-11-25 by the package-mode decision in Boundaries & Constraints; the message fix is applied with the patch entries.

### Loop 2 (2026-11-25)

| ID | Finding | Verdict | Evidence | Route |
|---|---|---|---|---|
| BH2-1 | Replay reports unknown type, null payload and malformed requests under imprecise categories; no test for unknown type or bad request | low | carried BH-8 and BH-9: the category labels are unchanged, and the replay tests now pin `DeserializationFailed` and `ApplyFailed`. | reject (carried) |
| BH2-2 | Paged replay is refused as `Conflict`; aggregate-type aliases and an empty type are accepted | false | No EventStore source sets `PagedContext`, so a paged replay is never requested. The aliases only widen what this host accepts. | reject |
| BH2-3 | A blank-AppId metadata request now returns 200 inside a theory named "Rejects" | low | carried EC-11 for the behavior. The theory `OperationalIndexMetadataRejectsForeignIdentity` now holds an accepted row, so the test misstates the decision. | patch |
| BH2-4 | Most endpoint tests still run against the test-only endpoint copy | medium | carried BH-6: the other test files still call `MapProjectsServerEndpoints`. | patch (carried) |
| BH2-5 | Anonymous-Development branches remain in production code | low | carried BH-10. | reject (carried) |
| BH2-6 | `GetOriginalQuery` falls back to the normalized envelope; the policy doc conflicts with Projects using the wire flag; the Conversation advisory query is untested | low | carried BH-7 for the fallback. The doc covers the response policy's envelope, while Projects reads the original flag only to refuse, in the executor. Both advisory queries share `ProjectAdvisoryQueryExecutor.cs:72`, which the attachments case covers. | reject |
| BH2-7 | Run-mode AppHost composition has no automated coverage | medium | carried VG-3 (DW-92). | defer (carried) |
| BH2-8 | Catalog and mTLS directories leak if code before the `try` throws; `daprRunDirectory` doubles as the catalog flag | low | carried BH-4 for the leak. The flag remark names no harm. | reject (carried) |
| BH2-9 | Restricted invocation mode fails silently; verb match is case-sensitive; bad JSON and a null observation throw | false | carried BH-11: an empty scope refuses every call and bad input fails startup. The generated grants always use `POST`. | reject (carried) |
| BH2-10 | A null or throwing response policy, last-registration-wins and the hard-coded query list hide failures | low | Last-wins is carried BH-12 (doc patched). Null and throwing policies are host bugs that new guards would be needed to catch. The six-query list is AD-19's bounded exception. | reject |
| BH2-11 | Public catalog-refusal statuses changed without documentation | low | The 503 is the Gateway's bounded catalog refusal, met only when a single-Tenant fixture catalog sees an altered Tenant. Real catalogs bind `*` and return the safe 404 (Spec Change Log, 2026-11-24). | reject |
| BH2-12 | Fixtures strip resilience more widely than the docs say | low | carried BH-16. | reject (carried) |
| BH2-13 | Docs: dangling link in the identity contract; README omits the Development requirement and section shape; topology runbook omits Keycloak detail | low | The identity-contract link closes a complete sentence (line 149), in the docs' usual style. The README names the source-build requirement and links custody provisioning. `Compose` fails loudly outside Development. | reject |
| BH2-14 | The ServiceDefaults reference and its tests are orphaned; SDK health endpoints are untested | low | carried BH-14. | reject (carried) |
| BH2-15 | The owner `Load` helper is duplicated; its mismatch error misleads | low | carried BH-3 for the duplication. The error fires exactly when the loaded image differs from the selected artifact, which is what it says. | patch (carried) |
| BH2-16 | Catalog profile tests check only return values; `Enabled` without a bootstrap directory is ignored | low | Without a bootstrap directory the host composes no catalog and the SDK refuses callbacks with 503, so the ignored flag is loud at the first call. Stronger assertions add test code for an unlikely misconfiguration. | reject |
| EC2-1 | Replay identity construction can throw and return 500 | low | carried EC-1. | reject (carried) |
| EC2-2 | `ApplyStoredEvent` exceptions outside the caught families return 500 | low | Stored history is written by the same processor, and malformed JSON already maps to `DeserializationFailed`. A catch-all adds a branch for an input nobody has shown. | reject |
| EC2-3 | Aggregate type `Projects` is refused while `PROJECT` is accepted | false | Nothing sends a case variant of the domain name; EventStore sends the registered domain. | reject |
| EC2-4 | A cleanup failure skips later deletions | low | carried EC-4. | reject (carried) |
| EC2-5 | An exception before the `try` leaks catalog and mTLS directories | low | carried BH-4. | reject (carried) |
| EC2-6 | Restricted Projects grants omit `project/rebuild/part/v1` and `project/v2/reconcile` | medium | `NamedProjectionDispatchCoordinator` invokes both, for large rebuild transports and terminal reconciliation, and a restricted receiver refuses ungranted callbacks. The pre-DW-63 host mapped neither route (baseline search), so both flows already failed with 404. | defer |
| EC2-7 | Undeclared SDK projection protocols are reachable without a Development bootstrap | false | Every SDK operational route requires the EventStore workload, the Project operation and a committed catalog generation; outside Development the deployed catalog decides admission. | reject |
| EC2-8 | A POST-only grant admits another verb at the same path | false | The SDK maps one non-POST route, `GET /`, and `/` is never a POST grant. | reject |
| EC2-9 | Null `DeclaredInvocations` entries throw at registration | false | carried BH-11. | reject (carried) |
| EC2-10 | A metadata request with a null AppId advertises the Projects catalog | low | carried EC-11. | reject (carried) |
| EC2-11 | Removing `AddEventStoreWorkloadAssertionIssuer` breaks non-SDK hosts | false | carried EC-10. | reject (carried) |
| EC2-12 | `/project/v2` and rebuild failures now return 500 | false | carried VG-8. | reject (carried) |
| EC2-13 | Formatted log messages are exported; the ServiceDefaults reference is orphaned | low | carried BH-14 and EC-14. | reject (carried) |
| EC2-14 | Run-mode credential wiring and secret persistence are untested | medium | carried VG-3 and EC-15 (DW-92). | defer (carried) |
| EC2-15 | The response policy can remap refusals issued before dispatch | false | Caller-binding and missing-handler failures are `QueryResult` values the dispatcher returns, so they are dispatch outcomes. Admission refusals and the verifier 503 return before the policy runs (`EventStoreDomainServiceExtensions.cs:440-447`). | reject |
| VG2-1 | The ordinary run-mode AppHost composition is not executed by any test | medium | carried VG-3 (DW-92). | defer (carried) |
| VG2-2 | No test checks that composed topologies grant every canonical POST receiver | medium | carried BH-13 (DW-95). EC2-6 records two current omissions. | defer (carried) |
| VG2-3 | A metadata request with no AppId is accepted under a "Rejects" theory | low | Same as BH2-3. | patch (with BH2-3) |
| VG2-4 | Always-run tests exercise the test-only host | medium | carried BH-6. | patch (carried) |

Loop 2 left one patch entry (BH2-3, VG2-3: the metadata theory name) and one new defer (EC2-6 as DW-97). Carried rows keep their loop-1 verdicts and are not patched or deferred again.

### Follow-up review

| ID | Finding | Verdict | Route |
|---|---|---|---|
| BH3-1 | The active Builds pin lacks the SDK response policy | high | The aligned EventStore 5.6.0 and Commons 3.0.3 candidate passed. The owner catalog selection is open. |
| BH3-2 | Ordinary human command submission has no composed receipt signing key | medium | Pass the configured receipt secret to the Gateway and Projects. Live ordinary proof needs the owner configuration. |
| BH3-3 | The ordinary outbound Projects owner client needs issuer and proof credentials and EventStore trust | medium | Ordinary peer and owner registration remain open under the actual AppHost task. |
| BH3-4 | SDK replay advances over malformed or foreign rejection payloads | medium | Require a JSON object and check any supplied Tenant and Project identifiers. Valid rejections retain their no-op state effect. |
| BH3-5 | Command parity compares SDK wire against the installed processor | medium | A separate fixture now compares HTTP wire results and the nested snapshot against an independently generated pre-migration receipt. |
| BH3-6 | DW-97 mentions missing grants after the declarations were added | low | Record the candidate grants and the pending persisted verification in DW-97. The entry evidence describes its original pinned revision. |
| BH3-7 | The P1 guard relies on the recorded closure | medium | This concerns accepted P1. The current run covers P2 and DW-63. |
| BH3-8 | The retained P1 runner refuses its existing output directory | medium | This concerns the accepted P1 receipt rerun. |
| BH3-9 | A fresh P1 XML receipt has run-specific fields | medium | This concerns the accepted P1 receipt comparison. |
| BH3-10 | Artifact hashes alone can close P2 | false | The guard verifies evidence integrity and the five decisions over the qualification digest. Owners and architects review the persisted results. |
| BH3-11 | A receipt from a different reviewed qualification can be substituted | false | The qualification digest binds the revision selections and receipt references to the five decisions. Owner review establishes the correspondence. |
| EC3-1 | An invalid canonical replay identity throws before typed result handling | medium | Return a typed conflict for invalid identity construction. |
| EC3-2 | Missing stored reference metadata throws while applying replay | medium | Refuse incomplete Folder, File and Memory event payloads with a typed replay failure. |
| EC3-3 | Catalog retention failure skips private AppHost directory cleanup | medium | Attempt both private directory cleanups on shutdown even if retention fails. |
| EC3-4 | Standalone Server runtime omits the assertion issuer dependency | medium | Restore the idempotent issuer registration and test the owner authority client resolution. |
| EC3-5 | Package-mode build uses the old selected SDK | high | Same release and owner selection evidence as BH3-1. |
| VG3-1 | Positive full replay only exercised the static projection helper | medium | Exercise the admitted SDK callback with a historical pending Folder and inspect the returned state. |
| VG3-2 | P2 closure did not exercise a linked receipt with matching bytes | medium | The P2 guard regression now includes the linked recovery receipt. |

### Resumed review (2026-11-25)

| ID | Finding | Verdict | Evidence and disposition |
|---|---|---|---|
| BH4-1 | Package-mode build cannot resolve the SDK policy | high | Carried BH3-1. New owner catalog passes locally. DW-99 still requires the tracked gitlink and main CI result. |
| BH4-2 | Ordinary Projects outbound credentials and proof trust are incomplete | medium | Carried BH3-3. Projects needs its own issuer credentials and matching Gateway proof trust. Ordinary proof requires owner custody. |
| BH4-3 | Browser fixture lacks the Conversation-start state component | medium | The baseline also attached only shared state and pub/sub while registering the handler. Deferred for the browser fixture. |
| BH4-4 | Receiver test does not exercise actual composition grants | medium | Carried BH-13 and VG2-2. DW-95 and DW-97 track parity and callback verification. |
| BH4-5 | Rejection validation differs between SDK replay and the legacy fold | low | SDK replay validates the supplied payload. The existing fold preserves the historical no-op disposition. |
| BH4-6 | Timeline captures a full state for each event | maybe-false | The reviewer supplied no failing history. Large-stream timeline qualification belongs to EventStore Admin replay. |
| BH4-7 | P1 guard does not check its pin and receipts | medium | The accepted P1 package owns its pin integrity. |
| BH4-8 | P1 runner cannot reuse its tracked output directory | medium | The accepted P1 package owns the clean-checkout reproduction path. |
| BH4-9 | P1 runner does not require a complete suite inventory | medium | The accepted P1 package owns its native receipt validation. |
| BH4-10 | Arbitrary receipt bytes can bypass P2 approval | false | The guard binds receipt bytes to all five decisions. The signing owners and architects verify the results before approval. |
| BH4-11 | Runbook suggests disabling Keycloak without a human issuer | medium | Patched the topology runbook to require external human OIDC configuration for that path. |
| BH4-12 | Legacy-wire parity receipt is local | medium | Owner acceptance still requires immutable checkout-visible evidence. The recorded local fixtures and native results remain available. |
| EC4-1 | Package-mode build cannot resolve the SDK policy | high | See BH4-1. |
| EC4-2 | Staged rebuild callbacks lack grants | medium | The staged protocol predates the SDK host migration and is separate from the qualified full-prefix rebuild. |
| EC4-3 | Named reconciliation lacks a persisted handler | medium | The SDK requires an explicit reconciliation handler. DW-97 still requires persisted reconciliation evidence. |
| EC4-4 | Ordinary owner and stream reads lack outbound proof settings | medium | See BH4-2. |
| EC4-5 | Replay accepts a rejection with missing payload identities | low | The legacy fold treats recognized rejections as no-op. Extra identity requirements need evidence from retained histories. |
| EC4-6 | Arbitrary native receipts can bypass P2 approval | false | See BH4-10. |
| VG4-1 | Hosted replay did not verify timeline state | medium | Patched the admitted HTTP test to compare both sequences with their legacy full projection prefixes. |
| VG4-2 | No P2 negative test isolates a missing Platform rollback revision | medium | The P2 guard test now checks a candidate with matching approvals and an incomplete rollback target. |
| VG4-3 | Actual AppHost rebuild-part and reconcile grants are unverified | medium | See BH4-4. |
| VG4-4 | Ordinary credential selection is unverified | medium | Carried VG-3. DW-92 tracks the ordinary run proof. |

### Resumed review 2 (2026-11-25)

The layers reviewed the DW-63 paths of the `5f1d955b..` diff. Planning artifacts, 7.1-P1 creation-contract files and the P1/P2 guard tooling were excluded.

| ID | Finding | Verdict | Evidence and disposition |
|---|---|---|---|
| BH5-1 | The live lane's ordinary-startup phase fails without ordinary custody, and the docs describe it as routine | medium | `run_live_project_fixtures.py:1131-1142` starts the ordinary source AppHost without `Projects:DevelopmentRouteCatalog`, so `ProjectsLocalDevelopmentCatalog.Add` refuses. Patched `tests/e2e/README.md` and the topology runbook to state the requirement. Supplying fresh custody and a bound issuer in the runner stays open under the unchecked AppHost task. |
| BH5-2 | The ordinary catalog omits the Tenants gateway grants and trust | medium | The gateway receiver grants only `projects` (`ProjectsLocalDevelopmentCatalog.cs:51-56`), while Tenants always registers `TenantOwnerSourceGateway`. The isolated profile grants `tenants` both workload calls and sets its allowed caller and bound-read keys. Same family as BH3-3 and BH4-2; open under the unchecked AppHost task. |
| BH5-3 | The manifest-grant test cannot detect a callback missing from the manifest | medium | carried BH-13 (DW-95). Defer (carried). |
| BH5-4 | A test removes the last hosted service by position | low | `ProjectsRuntimeCatalogTests.cs:128` removes the catalog activator today. A reorder would surface as a catalog activation failure, and matching the SDK-internal type adds code for a hypothetical change. Reject. |
| BH5-5 | The Projects callback list is copied into five compositions | medium | carried BH-13 (DW-95). Defer (carried). |
| BH5-6 | Replay rejection identity compares the tenant case-insensitively and accepts a null projectId | low | The processor compares stored tenants ordinally elsewhere (`ProjectsDomainProcessor.cs:298`, `:966`). Patched to `Ordinal`; the new replay test covers a case-variant tenant. The null projectId is carried EC4-5. |
| BH5-7 | Partial custody configuration surfaces a binder error | low | The binder names the missing constructor parameter at startup. Per-key guards add code for a loud misconfiguration. Reject. |
| BH5-8 | The Conversation-start setup store is missing outside the ordinary run and private profiles | medium | The baseline declared it only in the supported-read profile, so the publish gap predates this change. `module/README.md` lists it as a runtime prerequisite. The isolated browser profile is carried BH4-3. Defer. |
| BH5-9 | The module README overstates the ordinary catalog's readiness | low | The disclaimer was removed while positive admission, owner credentials and two callbacks remain unqualified. Patched with a limitation sentence naming DW-92 and DW-97. |
| BH5-10 | The README omits the workload issuer constraints | low | carried EC-7 for HTTPS. The README already states that the issuer grants the exact operation and audience for each callback. Reject. |
| BH5-11 | The supported-read docs omit the Folders scan inputs, and the `Add` pre-check is partial | low | The profile's input check (`SupportedProjectReadFixtureProfile.cs:93`) already requires every Folders key and names a missing one. Patched `docs/supported-project-reads.md` with the Folders grant and the metadata-only worker read. |
| BH5-12 | Replay format, sequence-bound, paging, domain and cancellation branches are untested | medium | carried BH-9. The format branch is covered by the VG5-5 patch. |
| BH5-13 | The response policy lacks direct table-driven tests | medium | carried BH-6. |
| EC5-1 | Unknown event types are reported as `ApplyFailed` | low | carried BH-8. Reject (carried). |
| EC5-2 | Replay failures report `LastAppliedSequenceNumber` 0 | low | `AdminStreamQueryController.cs:1875` and the manual snapshot log expose the value, and the `ApplyFailed` contract preserves the prefix. Patched every in-loop failure to pass the applied prefix; the new test pins it. |
| EC5-3 | Unlisted Apply exceptions return 500 | low | carried EC2-2. Reject (carried). |
| EC5-4 | A JSON null event payload is reported as `ApplyFailed` | low | carried BH-8. Reject (carried). |
| EC5-5 | A rejection without identity properties passes replay | low | carried EC4-5. Reject (carried). |
| EC5-6 | A case-variant tenant rejection is accepted | low | Same as BH5-6. Patched. |
| EC5-7 | Calling `AddProjectsServer` twice registers two replay owners | false | Only `AddProjectsDomainService` composes the SDK host, and it calls `AddProjectsServer` once. An ambiguous router fails loudly, and DW-108 tracks the startup guard. Reject. |
| EC5-8 | A host-registered `IDomainProcessor` splits process and replay | false | carried EC-8. Reject (carried). |
| EC5-9 | Projects handlers lack `EventStoreDomain` | medium | `GetHandlerDomainNames` (`EventStoreDomainServiceExtensions.cs:849-873`) omits `projects` from domain telemetry and logs a warning for each unattributed handler. Patched the 12 type-registered handlers; the package and source suites pass. |
| EC5-10 | An exception between `Add` and `try` leaks directories | low | carried BH-4. Reject (carried). |
| EC5-11 | A cleanup exception replaces the AppHost failure | low | carried EC-4. Reject (carried). |
| EC5-12 | A component-directory deletion failure loses the composed catalog | low | The directory is a just-created private temp directory. A guard adds code for an unshown failure. Reject. |
| EC5-13 | A test removes a hosted service by position | low | Same as BH5-4. Reject. |
| EC5-14 | The fixture `Lazy` caches a capabilities exception | low | Test-only; a throwing callback still fails every dependent test loudly. Reject. |
| EC5-15 | The deleted endpoints change the `/project/v2` error mapping | false | carried VG-8. Reject (carried). |
| EC5-16 | The persistent workload secret assertion was removed | medium | carried VG-3 and EC-15 (DW-92). Defer (carried). |
| EC5-17 | The test replica maps `/api/v1` without the human policy | medium | carried BH-6. |
| EC5-18 | Formatted log messages are exported | low | carried BH-14. Reject (carried). |
| EC5-19 | Server tests do not admit the ordinary profile | medium | carried VG-3 (DW-92). Defer (carried). |
| EC5-20 | Replay validates rejections that the legacy fold skips | low | carried BH4-5. Reject (carried). |
| VG5-1 | The ordinary run-mode composition is never executed | medium | carried VG-3 (DW-92). Defer (carried). |
| VG5-2 | The AppHost grant lists are not tied to the manifest | medium | carried BH-13 and VG2-2 (DW-95). Defer (carried). |
| VG5-3 | The persistent workload secret lost its assertion | medium | carried VG-3 and EC-15 (DW-92). Defer (carried). |
| VG5-4 | The advisory original-admin refusal is pinned only by a source-only test | medium | Pre-verified. `VerifiedQueryCaller` has a private constructor, so a package-mode unit test cannot bind the original envelope. The source lane runs the existing test. Defer. |
| VG5-5 | Replay refusal branches beyond the tenant and Folder cases are untested | medium | Pre-verified. Added `ReplayRefusesForeignOrIncompleteHistoryWithTypedFailures` for a foreign projectId, a case-variant tenant, File and Memory links without metadata, and an unsupported format. |
| VG5-O1 | Two launchers start the ordinary topology without catalog settings | medium | Same as BH5-1 for the live runner. The scheduled `run-live-apphost.sh` lane is VG-7. Open under the unchecked AppHost task. |

## Verification

- Prepare local Debug source mapping using `hexalith-module prepare-build --manifest module/hexalith.module-manifest.v2.json --mode source --workspace "$TMPDIR/source-mapping" --output json`. Run the focused Server tests in `ProjectsRuntimeCatalogTests`, `ProjectsDomainProcessorTests`, `SupportedProjectReadHttpTests` and `ProjectsAuthenticationContractTests`, plus the domain `ReadCutoverHistoryTests`. Use `run-with-temp` and the emitted mapping environment.
- Run `run-with-temp python3 tools/qualification/run_supported_project_reads.py --regressions`, the exact-source 77-cell v4 matrix and seven independent retained rollback reads in `docs/read-cutover.md`. Validate the published SDK dependency through the separate Release package-mode Server lane.
- Run `python3 tools/planning/validate_production_authority.py --validate-index` and record the new route, authorization, projection and read evidence before DW-63 review.

- At Projects `5025fde60965b77b4e7a5662b26ce31867a8b4c3` with its tracked gitlinks, the isolated `--scenario full-replay --timeout 600` run exited 0. Both Tenants passed all four full-prefix rebuilds, the persisted state comparison, stale cursor invalidation and fresh paging. The runner verified removal of its AppHost and four containers and preservation of preexisting resources. The native result SHA-256 is `058c7a3e5fba0fc2d72acdffac0bfb82aeb85a17f36ea823f02cdb3ae0ffbd11`. Receipts are under `~/.local/state/hexalith-projects/dw-63-resume-live-mJ9gAJO4/full-replay/`.
- The tracked Builds revision `bf8a28b4bbcec619e8d9764d335f786f2bafb69e` reproduced `CS0246` on `IDomainQueryResponsePolicy`. EventStore 5.6.0 and Commons 3.0.3 passed 54 focused tests with temporary version overrides. A separate Release package build with Builds owner revision `a6abb7908ec38dcb3fbd99d69e7ed29ae25794d2`, which selects EventStore 5.6.0 and Commons 3.0.4, passed without overrides. Its 54 focused tests passed. With the working-tree timeline assertion added, that selection also passed 54 focused tests and one explicit timeline test with zero skips. Receipts are under `~/.local/state/hexalith-projects/dw-63-builds-owner-HRPnTXa3/` and `~/.local/state/hexalith-projects/dw-63-timeline-zi0OdN2D/`.
- The separate `--scenario main --regressions --timeout 600` run passed the distributed scenario. The Integration regression assembly reported 349 passes and six failures. Five tests looked for the checkout beside the disposable build artifacts. The Production adapter test inherited the launcher's Development WebHost setting. The tests now use the existing repository-root resolver and select their WebHost environment explicitly. The initial run verified cleanup. Its results are under `~/.local/state/hexalith-projects/dw-63-resume-main-8p48BuLV/main/`.
- The live main rerun passed the distributed scenario again and verified cleanup. In the following Integration lane, three declaration checks refused mismatched SDK bytes from the separate AppHost and test builds. The metadata test now takes prebuilt owner binaries from the same MSBuild artifact tree as its SDK assemblies. It retains the existing byte-identity check and conventional output lookup.
- A separate mapped Debug run with those fixture changes built both test projects using `--artifacts-path "$TMPDIR/artifacts" -p:SupportedProjectReadDistributed=true -m:1`. It ran the launcher's Integration regression selection against disposable build outputs and the complete Server project with an owned Dapr sidecar. Integration passed 355 and Server passed 1,721, with zero failures or skips. Native XML hashes are `146b03bb660ea884490c9060a7942314f0bd2c3096ce742c5a735a25ec9ccfa0` and `8bc9a725433a417747ed7e760047fb07ee8eee3b8fab8c946180781346907083` respectively. The separate run retained its test overlay and logs under `~/.local/state/hexalith-projects/dw-63-source-suites-epBBAxNJ/`.
- Resumed review 2 patches were verified on the working tree over Projects `602816fc5f3bb206beafcbf5a61eed0a43b75416`. The package-mode Release Server.Tests build (`CI=true`, Builds `a6abb7908ec38dcb3fbd99d69e7ed29ae25794d2`, isolated NuGet cache) had zero warnings and passed 1,651 tests. The mapped source Debug build, which includes the uncommitted 7.1-P2 EventStore overlay, had zero warnings and passed 1,722 tests. Both runs had zero failures or skips and used a disposable in-memory daprd. `--validate-index` passed. The live lanes, v4 matrix and rollback reads were not rerun; the patches change replay failure metadata, handler telemetry attributes, one test and docs.
- Main CI for `602816fc` first failed at submodule initialization, because EventStore `be511806` and Platform `7b4cc722` were not on their remotes. After both were pushed, the rerun built the CI solution in Release package mode with zero errors, so `IDomainQueryResponsePolicy` now resolves. It then failed the release package check, because `tools/release-packages.json` still pinned EventStore 5.5.1 and Commons.UniqueIds 3.0.2. The aligned manifest validated all five packages and their consumers locally. G-6 fails separately with NU1109, because Parties pins Commons 3.0.2 under the EventStore 5.6.0 catalog. [CI run](https://github.com/Hexalith/Hexalith.Projects/actions/runs/45639277367)
