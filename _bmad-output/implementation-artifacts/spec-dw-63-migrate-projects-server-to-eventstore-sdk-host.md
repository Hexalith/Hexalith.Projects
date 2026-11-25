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

## Verification

- Prepare local Debug source mapping using `hexalith-module prepare-build --manifest module/hexalith.module-manifest.v2.json --mode source --workspace "$TMPDIR/source-mapping" --output json`. Run the focused Server tests in `ProjectsRuntimeCatalogTests`, `ProjectsDomainProcessorTests`, `SupportedProjectReadHttpTests` and `ProjectsAuthenticationContractTests`, plus the domain `ReadCutoverHistoryTests`. Use `run-with-temp` and the emitted mapping environment.
- Run `run-with-temp python3 tools/qualification/run_supported_project_reads.py --regressions`, the exact-source 77-cell v4 matrix and seven independent retained rollback reads in `docs/read-cutover.md`. Validate the published SDK dependency through the separate Release package-mode Server lane.
- Run `python3 tools/planning/validate_production_authority.py --validate-index` and record the new route, authorization, projection and read evidence before DW-63 review.
