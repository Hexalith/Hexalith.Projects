# Verified FrontComposer session credential repair — 2026-11-05

**Owner repair verified and unchanged; DW-1 remains closed for this source working tree. Final consumer/spec readiness now passes for a separate implementation request.** [The 2026-11-06 continuation](../readiness-final/README.md) applies the authorized fixture migration, records the mapped build and 12/12 native authentication composition passes, and refreshes the spec. The remaining planning/consumer-test issues recorded below describe the earlier repair checkpoint. No Story 6.5 runtime or feature-test implementation was performed.

## Tested source and scope

FrontComposer base is `4c24c287cde520631e960a322413bee804f7c3d4` plus [owner-source.patch](owner-source.patch), SHA-256 `cfca262eb860a4cb99efe6d5e76da401cc0ab58f88eb001d262e1eb9724d4d62`. The patch includes all 17 modified/new owner source/test files; [source-inputs.json](source-inputs.json) records exact file and assembly hashes, full directly used revisions and the uncommitted status. The base commit alone remains defective. No repaired accepting commit is invented: commits and dependency/gitlink updates are forbidden in this task. Closure applies to the verified checkout with this repair present; a clean checkout of the unchanged gitlink does not contain it.

Projects started at `fb538a23201b419d1329d06faf2cd2b290ff8991` with existing readiness edits. Those edits were committed externally during this run as `b9a890e38044d23affe6b5d403d4b2a69b6edf34`; this agent did not commit or change branches. They are preserved, with the current correction appended. Story 6.1–6.4 acceptance remains the ordinary commit/test references in [the original packet](../README.md#source-and-accepted-inputs-initial-assessment); no accepted read-handler behavior was changed or rerun.

## Repair and executed coverage

The owner store now requires an immutable `(local sign-in session, stable subject, current Tenant)` binding. Every final OIDC ticket gets a fresh 256-bit random local session claim; provider-supplied copies are removed. Capture uses the configured OIDC scheme and final `OnTicketReceived`, preserving prior callbacks and saved-token expiry. The final ticket follows OIDC claim mapping in the [ASP.NET Core handler](https://github.com/dotnet/aspnetcore/blob/v10.0.9/src/Security/Authentication/OpenIdConnect/src/OpenIdConnectHandler.cs); [RemoteAuthenticationHandler](https://github.com/dotnet/aspnetcore/blob/v10.0.9/src/Security/Authentication/Core/src/RemoteAuthenticationHandler.cs) invokes `TicketReceived` before cookie sign-in.

Gateway relay, EventStore acquisition and the server user/Tenant context all prefer the active circuit over a retained request. Tenant validation reuses the existing fail-closed claim extractor; opaque subjects retain their original values, including colons. No subject-only fallback exists. Missing/ambiguous/expired/revoked circuit bindings cannot recover a saved-cookie credential. Trusted explicit host token providers retain their existing contract; this repair qualifies FrontComposer's default server relay.

Captured SignalR delegates close over the original binding and authentication-state source, rechecking state, store expiry and revocation on every call. A missing binding yields a bound null-result delegate, preventing reconnect from falling back to another ambient session. Sign-out revokes by local sign-in identifier after successful cookie sign-out, even if Tenant claims changed or disappeared; sibling sessions remain valid. Failed sign-out preserves existing credentials. Expired abandoned entries are purged on the next store write and expired entries on lookup; this is not timer-driven sweeping or distributed persistence.

| Required behavior | Passing executed evidence |
| --- | --- |
| Same subject across Tenants, same subject within one Tenant, distinct users | Three `ConcurrentCircuits_RelayOnlyTheirSessionCredential` theory rows interleave real pooled gateway/provider operations through shared AsyncLocal accessors, and check user/Tenant context with a stale request present. |
| Missing, unknown, anonymous, changed or ambiguous authority | Ten `InvalidBinding_CannotRecoverAnExistingCredential` cases exercise gateway, EventStore and captured providers; OIDC tests reject missing token/expiry and ambiguous Tenant. |
| Expiry, safe replacement and abandoned sessions | Injected-clock tests cover exact expiry boundary, expired replacement races, and write-triggered purge without revisiting the expired key. |
| Sign-out and session isolation | Real protected-cookie sign-in/authenticate/sign-out round trip with two same-subject/same-Tenant sessions; changed-Tenant sign-out invalidates an old capture while preserving a sibling. Existing sign-out failure/idempotence cases pass. |
| Captured SignalR providers | Captured original identity survives absent/other ambient circuits, expires, is revoked and rejects later Tenant/anonymous state. Existing ProjectionSubscriptionService reconnect callback uses the same provider and raises sanitized HFC2013 after revocation. |
| OIDC integration and compatibility | Fresh local claims replace incoming IDs; configured challenge scheme and a Tenant added by a prior final-ticket callback pass. Opaque stable subject plus application email alias passes. Existing redaction and authentication boundary tests pass. |

Owner source references (relative to FrontComposer): `Extensions/FrontComposerTokenRelayServiceExtensions.cs:55` (named final-ticket hook), `Services/Auth/FrontComposerTokenSession.cs:21` (binding), `Services/Auth/FrontComposerClaimExtractor.cs:24` (Tenant authority), `Services/Auth/FrontComposerUserTokenStore.cs:22` (store/expiry), `Services/Auth/FrontComposerGatewayAuthorizationHandler.cs:29` (outbound lookup), `Services/Auth/FrontComposerAccessTokenProvider.cs:76` (capture), `Services/Auth/ServerCircuitUserContextAccessor.cs:73` (context authority), `Extensions/FrontComposerAuthenticationServiceExtensions.cs:124` (sign-out). All paths are under `src/Hexalith.FrontComposer.Shell/` and are retained in the patch.

## Exact verification

[commands.json](commands.json) retains argument arrays, working directories, exit codes and output filenames for every retained command, including failed attempts. The reproducible capture wrapper is [run-command.py](run-command.py). SDK is `10.0.403`; native test runtime is `.NET 10.0.14`.

| Final check | Exact command/result reference |
| --- | --- |
| Owner test-project source build | `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Debug -m:1 -p:NuGetAudit=false`, from FrontComposer. [owner-final-build.txt](owner-final-build.txt): exit 0, 0 warnings/errors. |
| Focused native owner cases | `dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll` with nine single-dash `-class` selectors and `-xml` recorded exactly in `owner-final-auth` in commands.json. [owner-final-auth.xml](owner-final-auth.xml): **114 passed, 0 failed, 0 errors, 0 skipped, 0 not-run**, exit 0. |
| Projects consumer source build | Platform tool-selected **Projects** mapping, then `dotnet build src/Hexalith.Projects.UI/Hexalith.Projects.UI.csproj --configuration Debug -m:1 -p:NuGetAudit=false --disable-build-servers`. Exact mapping variables/command in `ui-source-only`; [output](ui-source-only.txt): exit 0, 0 warnings/errors. |
| Projects existing test-project build | Same correct mapping, existing UI test project. [ui-source-build-2.txt](ui-source-build-2.txt): exit 1, one CS1503 at `ProjectsUiAuthenticationCompositionTests.cs:122`, which seeds the removed subject-only store API. No Projects UI xUnit pass is claimed. |
| Authority and consistency | Final authority/index and diff checks are retained in commands.json. Passing authority permits the status tuple; it is not Story 6.5 acceptance. |

Earlier retained attempts include the eleven analyzer errors in new test code (corrected), a revoked SignalR assertion expecting null instead of existing HFC2013 (corrected), and standalone relay DI registration missing Options (corrected). Intermediate 101/106-case results are superseded by the final 114-case run. The initial mapping attempt incorrectly used Conversations as the active module and was rejected with HXW006 for Projects paths; the Projects mapping corrects that invocation without weakening the gate. The mapping tool's HXW010/HXW011 diagnostics are retained; they did not prevent the local source preparation and are not publication qualification.

The required AppHost baseline start failed during its parallel Debug dependency build on EventStore.Client `GenerateDepsFile` file contention (exit 2). [Startup output](apphost-startup.txt) and the exact command are retained; exact-target stop confirmed no AppHost running. The subsequent serialized focused owner/consumer source builds passed. No running topology, browser, production provider, real credential or domain read/write was used. No live authentication/accessibility or distributed/package acceptance is claimed.

## Review

The Build Blind Hunter reviewed 42.863 kB (finding floor 7). All seven findings were checked and patched: revocation independent of Tenant changes; abandoned-session expiry cleanup; final-ticket capture ordering; user-context/circuit precedence; configured OIDC scheme; opaque-subject compatibility; and a real protected-cookie round trip. Its follow-up found all seven addressed and no material flaw. The final focused suite was run after those patches. No review finding was deferred.

## DW-1 disposition and Story 6.5 readiness at the owner repair checkpoint (historical)

The four accepted read capabilities remain delivered, FrontComposer's default server identity/current-Tenant/session relay passes the focused security matrix, and the existing Projects UI source composes the repaired owner API. **DW-1 is done for this verified source checkout.** Publication, a new dependency pin, production enrollment and live browser acceptance are not added to this prerequisite gate.

**Story 6.5 remains blocked / requires a planning refresh before dispatch.** This assessment does not implement it or change sprint status:

- The current code map still names `UI/Services/`; actual adapters are in `src/Hexalith.Projects.UI/Diagnostics/`. Program/Routes and `ProjectsUiSecurity` already compose OIDC; the plan must distinguish the delivered composition from remaining supported-read adaptation.
- The plan's combined coherent detail/context/setup result is absent. `ProjectListPage.Snapshot`, each `ProjectListRow.Snapshot`, `GetProjectResponse.Snapshot`, context admission and `ConversationStartSetupResponse.Snapshot` have separate authority. `ProjectAdvisoryResolutionResponse.EvaluatedAt/Incomplete/UnsupportedInputKinds` are advisory evidence, not invented AD-32 admission. Re-plan using actual Contracts/Queries and SDK `IEventStoreGatewayClient.SubmitQueryAsync<T>`.
- The existing Projects authentication composition fixture must migrate from subject-only seeding to the owner `FrontComposerTokenSession` plus authenticated local-session/current-Tenant claims. [The concrete proposed patch](projects-test-fixture-proposal.patch) is prepared but unapplied pending explicit scope permission because source/test authorization names FrontComposer. This changes only synthetic test setup, not Projects runtime token plumbing. Until it is applied and the focused consumer test rerun, the UI test-project gate fails as recorded above.
- Supported browser-to-handler, two-circuit Tenant isolation, safe absence, independent admission states, zero-write and accessibility journeys remain future Story 6.5 acceptance work. Source tests here do not satisfy those feature ACs.

Old failing reproduction files in the parent packet remain unchanged as historical evidence against the old owner revision. Its old subject-only harness does not compile unchanged against the intentionally repaired store API; use the native owner tests above to verify the repair.


## Consumer/spec readiness completed — 2026-11-06

The user explicitly authorized the retained synthetic-fixture patch. It is now applied, and the Projects UI test project builds with the same established Projects mapping (Debug/source, zero warnings/errors). Direct built-assembly authentication composition execution passes 12/12 with zero failures/errors/skips/not-run. The stale Diagnostics/authentication map and combined-snapshot assumptions are corrected in the spec, with detailed delivered-contract references in the continuation.

All original repair commands, native outputs, patches and source-inputs remain unchanged; all 17 owner source/test file hashes and prior tested owner assembly hashes still match this repair. DW-1 stays done for this verified checkout. The final result is ready for a separate implementation request; spec approval/dispatch, runtime feature implementation and sprint-status changes did not occur. [Exact continuation receipts and scope](../readiness-final/README.md) supersede the historical readiness blockers above.


## Subsequent publication — 2026-11-06

The later user request to push all changes authorized publication of this completed work. FrontComposer repair is now committed and pushed as `f577f958ed1c043eec7feabc4885f59d48428818`; fresh owner 114/114 and Projects authentication composition 12/12 checks pass, with clean Debug/source builds. Earlier uncommitted-checkout statements remain historical. See [the publication record](../publication/README.md). DW-1 remains done; no Story 6.5 implementation or sprint-status edit occurred.
