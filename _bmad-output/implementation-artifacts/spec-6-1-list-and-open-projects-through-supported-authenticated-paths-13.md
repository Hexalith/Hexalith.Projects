---
title: 'Story 6.1: Complete supported bootstrap proof and platform maintenance'
type: 'bugfix'
created: '2026-11-05'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
story_key: '6-1-list-and-open-projects-through-supported-authenticated-paths'
baseline_commit: '0047c961ccad19f1a7344095ecb70bcd8e4bb65b'
context:
  - 'AGENTS.md'
  - '_bmad-output/implementation-artifacts/evidence/story-6-1-platform-maintenance-20261105/pre-edit-baseline.json'
  - 'references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '_bmad-output/implementation-artifacts/epic-6-context.md'
  - '_bmad-output/implementation-artifacts/6-1-list-and-open-projects-through-supported-authenticated-paths.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Finish Story 6.1's fresh authenticated verification. The SDK/source-helper and fixture/observer repairs are implemented and reviewed. Complete persisted bootstrap source proof now passes; the next checkpoint reconciliation request is refused because it exposes reserved storage Tenant `system` on an ordinary Tenant route.

Approved continuation: repair shared EventStore support, provide a finite owner-hosted Development test helper, then complete Projects' strict persisted bootstrap proof and existing acceptance lane. On 2026-11-05 the user chose option 1, authorizing EventStore Client/Testing changes, Tenants test-only enrollment and Projects wiring. This supersedes the earlier Projects-only restriction; all other boundaries below remain effective.

**Approved maintenance extension — 2026-11-05:** The user approved the concrete maintenance owner proposal after the final source-repair handoff. Add only `POST /api/v1/admin/projections/platform/global-administrators/delivery-reconciliation`, with explicit namespace `platform:global-administrators-delivery-reconciliation` and exact human permission `admin:reconcile-global-administrators-delivery`. Require the existing authenticated human/GlobalAdministrator authority and attributable operator, then reconcile only the internal global-administrator domain/aggregate/projection in retained storage `system`. Keep the read-only Admin catalog and ordinary Tenant routes unchanged. Align the fixture's exact URL, header and finite Administrator grant, preserve its cutover assertions, and rerun fresh acceptance. This approved route is the sole exception to the earlier new-public-endpoint prohibition.

## Boundaries & Constraints

**Always:** Preserve original-request assertion/proof binding, exact approved storage mappings, receipt/causation/correlation/event checks, privacy, authority, no-domain-write assertions and owned cleanup. Keep credentials inside Tenants. Keep story/sprint in progress until required evidence passes.

**Never:** Add public endpoints beyond the one approved maintenance route, generic relays, Projects-owned transport or impersonated owner assertions; grant public `system` access; rewrite history; weaken checks; change pins, branches or submodule checkouts; stage, commit or push. Production enrollment and other stories remain outside scope.

## I/O & Edge-Case Matrix

| Input/state | Expected behavior |
| --- | --- |
| Approved platform scope/domain | Explicit scope header; accept only mapped storage Tenant and exact Domain/AggregateId. |
| Unsupported pairing or conflicting headers/response identity | Fail closed without changing proof binding. |
| Ordinary Tenant request | Existing transport and exact Tenant validation. |
| Explicit Debug/Development helper selection | Exact administrator stream through SDK and real owner credentials. |
| Wrong helper target/mode/identity, expiration or oversized result | Refuse; propagate failures, never synthesize successful proof. |
| Exact maintenance scope + permission + authenticated attributable GlobalAdministrator | Reconcile only the fixed retained administrator checkpoint; preserve sequence/source and record attributable technical work. |
| Missing/duplicate/wrong scope or permission, mixed Tenant selectors, non-administrator or missing operator | Refuse before state access; read-only Admin scope cannot authorize maintenance. |
| Existing ordinary Tenant maintenance and global writer activation | Retain current admission, validation and result behavior. |

</frozen-after-approval>

## Code Map

Paths below are relative to their named owning repository: EventStore at `references/Hexalith.EventStore`, Tenants at `references/Hexalith.Tenants`, Projects at this workspace root.

- EventStore `src/Hexalith.EventStore.Client/Gateway/EventStoreGatewayClient.cs`, `ReadWorkloadStreamAsync`: missing scope header/mapped response validation. Reuse `LegacyTenantStorageMap.IsPlatformSourceRead` and `TenantCanonicalizer.ReservedStorageTenant`; update adjacent `IEventStoreGatewayClient.cs` documentation after verification. Existing `StreamsController` already enforces platform admission and original-request proof.
- EventStore `src/Hexalith.EventStore.Testing/CatalogInvocation/`: reuse finite private IPC, deadlines and failure patterns. Existing worker's gateway issuer cannot impersonate Tenants. New `src/Hexalith.EventStore.Testing/OwnerSourceRead/` helper must run in the owner process; one type per file.
- Tenants `src/Hexalith.Tenants/Program.cs` and adjacent `Hexalith.Tenants.csproj`: explicitly selected Debug/source enrollment using actual issuer, bound key and catalog-fenced route. Preserve production owner-source behavior.
- Projects `src/Hexalith.Projects.AppHost/SupportedProjectReadDevelopmentInfrastructure.cs`, adjacent `SupportedProjectReadFixtureProfile.cs`, and `tools/qualification/run_supported_project_reads.py`: helper selection, private directory/build custody and cleanup.
- Projects `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadDistributedFixture.cs`, `ReadHostedBootstrapSourceAsync`: replace unconditional guard; retain subsequent exact event/receipt validation.

- EventStore `src/Hexalith.EventStore.Contracts/Routing/ProjectionDeliveryMaintenanceScopeCatalog.cs` (new): exact maintenance namespace/permission/route and request-local admission marker; resolve only the new action. Leave `AdminPlatformScopeCatalog` read declarations unchanged.
- EventStore `src/Hexalith.EventStore/Authorization/AdminTenantAuthorizationFilter.cs`: resolve the new exact action before the no-Tenant early return; require one explicit namespace/permission, valid public grants and no Tenant route/query/header selector. Preserve ordinary and read-only branches.
- EventStore `src/Hexalith.EventStore/Controllers/AdminProjectionRebuildController.PlatformDelivery.cs` (new partial): require validated maintenance admission, existing HumanPolicy/GlobalAdministrator and an attributable subject. Construct only internal `system` / `global-administrators` / `global-administrators`, projection `global-administrators`; use existing reconciler/result semantics. Preserve ordinary `CreateScope` validation and existing service/validation ordering.
- EventStore `src/Hexalith.EventStore.Server/Projections/ProjectionDeliveryReconciler.cs` and `EventStoreProjectionDeliveryHistoryReader.cs`: existing APIs already read/validate the exact persisted source and atomically store hydrated delivery state plus reconciliation work. Reuse without schema/source/history changes.
- Projects fixture `ActivateFreshDeliveryWriterProtocolAsync`, `HumanClaims`, and existing human HTTP send: replace only the legacy maintenance URL and add explicit finite scope/Administrator permission. Retain original bootstrap, backup/quiescence, preserved-sequence, marker and readiness assertions; keep the owner-source helper read-only.

- SDK `src/Hexalith.EventStore.DomainService/DomainProjectionNativeSourceAdmission.cs`: select the SDK-qualified native workload reader before the existing native-proof global-gateway fallback, preserving ordinary unkeyed reader registrations. Fallback is allowed only when no reader is registered, never after refusal/unavailability/error; preserve all native metadata and complete-prefix/event/tamper checks.
- SDK `src/Hexalith.EventStore.DomainService/EventStoreGatewayWorkloadTransportExtensions.cs`: add a named workload registration for the existing authoritative reader, using existing `DomainServiceGatewayWorkloadAssertionSource` and `DomainServiceGatewayBoundStreamReadProofSource`. Keep its client private to the read-only reader and independent of the host human relay client.
- SDK `src/Hexalith.EventStore.DomainService/DomainServiceNativeSourceCredentials.cs`: request the exact existing tenant/domain assertion bindings for metadata reads; preserve the existing owner proof and finite operation. The fixture's already-bound `folders-owner-head` grant stays unchanged.
- Folders `src/Hexalith.Folders.Server/FoldersDevelopmentOwnerMetadata.cs`: opt into the SDK authoritative-reader registration only under the existing selected Debug/source/Development/catalog guards. Preserve `FoldersServerHostComposition.cs` and `Authentication/FoldersGatewayHumanBearerHandler.cs` behavior; no Projects-owned source transport or new domain boilerplate.

## Tasks & Acceptance

- [x] EventStore Client paths above: implement exact platform mapping. Extend `tests/Hexalith.EventStore.Client.Tests/Gateway/EventStoreGatewayWorkloadClientTests.cs` with both mappings, wrong identities, original proof/assertion binding and ordinary-Tenant controls.
- [x] EventStore Testing path above: add separate owner-source helper restricted to `platform:global-administrators` / `global-administrators` / `global-administrators`. Add `tests/Hexalith.EventStore.Testing.Tests/OwnerSourceReadTests.cs` for admission, SDK dispatch, bounds and failure propagation.
- [x] Tenants host paths above: opt-in enrollment only; extend `tests/Hexalith.Tenants.Server.Tests/Authorization/TenantOwnerSourceRuntimeTests.cs` for real issuer/catalog composition and default refusal.
- [x] Projects paths above: wire shared helper; extend `tests/tools/test_supported_project_reads_launcher.py` and Integration `SupportedProjectReadTopologyTests.cs` / `SupportedProjectReadEvidenceTests.cs` for selection, proof and cleanup.
- [x] Projects `_bmad-output/implementation-artifacts/evidence/`: retain commands, full tested commits/source changes, XML, totals and cleanup; append factual results to story/sprint tracking.

- [x] EventStore new maintenance contract/filter/controller paths above: implement exact public admission and fixed internal target without broadening read-only or Tenant authority.
- [x] EventStore `tests/Hexalith.EventStore.Server.Tests/Authorization/AdminTenantAuthorizationFilterTests.cs` and `Controllers/AdminProjectionRebuildControllerTests.cs`: prove positive admission and missing/duplicate/mixed/foreign/read-scope/non-admin/missing-operator refusals before state access; cover unchanged ordinary admission.
- [x] EventStore `tests/Hexalith.EventStore.Server.Tests/Projections/ProjectionDeliveryReconcilerTests.cs` and relevant history-reader controls: assert actual persisted technical end-state, preserved source/sequence and attributable work for the retained administrator stream, with no projection dispatch or domain-event append.
- [x] Projects `SupportedProjectReadDistributedFixture.cs` and `SupportedProjectReadEvidenceTests.cs`: align exact route/header/finite grant and add scope/credential controls without weakening any prior assertion.
- [x] `_bmad-output/implementation-artifacts/evidence/story-6-1-platform-maintenance-20261105/`: retain fresh focused and three-provider qualification/regression commands, native XML, totals, full heads/source patches/manifest, byte-identical nonignored outputs and exact owned cleanup. Finish review and update story/sprint only from actual acceptance.

- [x] SDK source admission/workload registration/metadata credential paths above: isolate complete authenticated prefix reads from the human submission client and bind metadata assertions to their unchanged exact scope.
- [x] SDK `tests/Hexalith.EventStore.DomainService.Tests/NativeSourceMetadataCompositionTests.cs` and `DomainProjectionNativeSourceAdmissionTests.cs`: prove actual named HTTP/assertion/proof behavior, zero human-client calls, source/tamper rejection and no fallback after a selected reader refuses; preserve existing fallback when unregistered.
- [x] Folders `tests/Hexalith.Folders.Server.Tests/FoldersDevelopmentOwnerMetadataTests.cs` and unchanged `tests/Hexalith.Folders.Server.Tests/FoldersNativeGatewayBearerTests.cs`: prove selected/unselected source composition and actual human/workload client isolation, with human credential refusal retained. Add original projection/native source matching proof; then rerun fresh qualification and relevant full local regressions.

- [x] Folders `tests/Hexalith.Folders.Server.Tests/Task36LifecycleDiagnosticObserverTests.cs`: correct the demonstrated test-only shutdown race with a bounded wait for its actual observed `request_end` with zero in-flight requests before initiating StopAsync. Preserve every existing zero-count, transient-resolution, lifecycle-order and privacy assertion; no production observer changes. Rerun isolated rows and full Folders checks sequentially before the next native attempt.

- [x] Projects `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadDistributedFixture.cs`, `HumanClaims`, and `SupportedProjectReadEvidenceTests.cs`: add the optional foreign coarse grant only when distinct from the credential's existing canonical Tenant. Prove foreign actor has one grant and the intentional primary-to-foreign negative control retains its two distinct grants; keep production duplicate-grant rejection and all authority assertions unchanged.

Given the exact admitted administrator maintenance request, when the real reconciler runs, then only its technical checkpoint/work records change, original source events and delivered sequence remain intact, and ordinary Tenant behavior remains unchanged.

Given valid bootstrap, when its actual source is read, then exactly one persisted administrator event matches the original command, correlation and actor without remapping event history.

Given current source, when distributed qualification and regressions finish, then all seven story criteria have explicit evidence or an exact remaining blocker; historical receipts never substitute for changed-source acceptance.

## Implementation Notes

Qualification-4 at `e249cd16697f064a50aa95870532e910188c34431b98b02ed6ecc282d505ff24` runs alone with healthy infrastructure, succeeds through maintenance and every real seed command, then receives 403 at the first foreign-Tenant supported list. Source tracing identifies HumanClaims(ForeignTenant, foreignTransportGrant:true) adding the existing ForeignTenant grant twice; QueriesController correctly rejects duplicates before lookup. Deduplicate only that fixture addition and preserve the two distinct grants needed by the intentional cross-Tenant denial case. This is routine credential-fixture correctness, with no production authorization or new grant.


The broad Folders suite exposes a test-only completion race in the unchanged Task36 rows. Isolated exact-child metadata proves application_stopping/host_stop run with inFlight=1 before the framework request_end callback subsequently records zero. Correct only test sequencing using a bounded actual-observation barrier; do not change observer behavior or weaken assertions. Qualification-3 never executes native tests because owned placement is unhealthy; preserve it as nonqualifying, retain the original capture/cleanup failure and separate exact-identity recovery receipt. Cleanup is complete before sequential reruns. No timeout, assertion or infrastructure policy is weakened.


Maintenance qualification-1 and same-source qualification-2 at `5e29dacadd3659b0f830432815a0ddbc610a4c3eb3329e1bdfd3b22fcc431bc5` both prove the new reconciliation and writer activation, then reach CreateFolder HTTP 500. A finite exact-owned-session memory observer captures the static human-credential exception and source stack. Initial attribution to the metadata reader was corrected: that reader already has a separate named client. Complete-prefix admission directly resolves the host's global human gateway, and metadata credentials independently issue unbound assertions against an already-bound grant.

The additional SDK/Folders correction above is ordinary source-read composition needed by the original real Folder-authority goal. It introduces no public endpoint, permission, caller authority, grant or dependency change. Only the selected Folders Development composition opts into an existing-interface SDK reader. This refines the implementation plan without changing frozen user intent. Keep the human relay and every existing source/native proof check intact; do not bypass the handler based on workload-looking headers. Preserve the successful maintenance phase, old source bundles, failures and diagnostic limits, then verify causality through a fresh strict native run.


2026-11-05 maintenance continuation: the user explicitly approved expanding the frozen intent as above; no further scope approval is pending. Preserve every earlier authorized change and the completed six-group review corrections. This is an active implementation dispatch of the existing bmad-build workflow; execute directly from this spec, do not start another Build workflow or run parallel implementation agents. Only the new unchecked maintenance tasks need implementation.

Current source exactly matches corrected manifest `970e0d526fd71d5ca3dfba99b53515e662f10c26f56a2c2b7709740d76de4cfc`; the immediately preceding native qualification-2 supplies the pre-edit healthy Running resources, actual source proof, maintenance 403 and exact cleanup baseline at the same source/heads. The new pre-edit-baseline.json records this equality. Inspect that evidence and current owner guidance; repeat baseline lifecycle only if source/toolchain drift requires it. Root/owner working trees contain authorized earlier work: preserve them; no resets, staging, commits, pin/checkout changes or dependency work.

Use the existing prepare-build CLI to create an owned Debug/source mapping, record exact selected environment flags and commands, and clean it after testing. The existing isolated launcher is the fresh acceptance lifecycle. A missing capability must be documented honestly; do not bypass owner authorization or remove assertions. Package-mode NU1109 remains a separately recorded unrerun obligation; do not change pins or claim package acceptance. Source-only helpers and Tenants production behavior remain untouched by this maintenance extension.


2026-11-05: User approved option 1. Re-read draft from disk before recording approval. Root is clean at `fb6e1dfbe55fcade245da251bc5ff0d645a3456d` after an external /pushall recorded the previously observed advances and this draft. Preserve the original baseline_commit and inspect all owner guidance/status before editing. Source repair dispatch begins from this approved spec.

The gap was confirmed at EventStore `2dca77cc2f22a9b85e4fcc3f8e18cbcaa532b135` and rechecked after external advancement to `bbc165a5cd3a1f21822b4b53daa7fd895883f8a5`. Tenants is `8e20524574901d71ad5f4876804c64c3c6c87b88`. Multiple submodule checkouts advanced externally during planning; preserve them and recheck source/status before implementation. Retained local results are Integration 208/208 and Server 1,331/1,331, excluding live selectors; not rerun here. Package-mode NU1109 remains a separate unresolved conflict. No irreversible production changes; new callable helper is confined to Testing and explicitly selected local hosts.

## Spec Change Log

2026-11-05: Human approval expanded the frozen new-endpoint boundary for the exact global-administrator delivery reconciliation proposal. Added separate maintenance namespace/permission, exact public/internal target, filter-before-early-return guard, service reuse and acceptance tasks. KEEP all prior source/helper/observer repairs, stricter complete-history checks, selected/unselected/Linux test controls, original evidence/source bundles and cleanup protections. Avoid reserved public storage grants and read-only permission escalation.

## Review Triage Log

2026-11-05: Blind Hunter, Edge Case Hunter and Verification Gap all completed at the same model capability; none was skipped. Full unified snapshot includes untracked source/evidence and preserves the externally committed workspace sync. Each finding is graded separately below before grouping.

| Finding | Verdict and evidence | Route |
| --- | --- | --- |
| B1: Positive Tenants tests run without host opt-in. | **medium.** Tests compile under DEBUG/source alone; the host additionally requires EnablePrivateOwnerSourceReadProof. Normal source builds therefore execute two positive theory rows against the refusing host. Align selected tests and add an unselected-build refusal control. | patch |
| B2: Complete source validation omits range/sequence consistency. | **medium.** Shared selection accepts bounded/omitted-tail pages with matching count; the independent fixture only adds FromSequence=0. Correct the existing validators and cover bounded ranges, omitted tails and invalid order/positions. | patch |
| B3: Malformed private filenames can stop the owner worker. | **low.** Claim is outside the handler and rejects invalid custody. The sole supported publisher creates 32 lowercase-hex identities inside fresh owner-private custody; malformed files need unsupported manual/private tampering. Extra ignore/recovery branches are unwarranted and could mask custody failures. | reject |
| B4: Client cancellation after publication does not cancel remote dispatch. | **low.** Cross-process cancellation is not communicated; the client still refuses late results and the owner operation remains bounded by its original 45-second expiry and host lifetime. This is a finite source-only read in disposable custody. Adding a cancellation protocol is disproportionate to rare canceled fixture work. | reject |
| B5: New IPC tests fail on non-Linux hosts. | **medium.** CatalogInvocationFiles explicitly requires Linux; new tests instantiate it without an OS condition. Use the existing xUnit platform-selection pattern for IPC-dependent cases while retaining portable admission controls. | patch |
| B6: Release/package compile boundary has no dedicated executable coverage. | **low.** Extra matrix coverage is absent, but the csproj condition excludes the reference/symbol and the unselected host branch explicitly refuses. The B1 repair adds ordinary unselected-source refusal evidence; no Release admission defect is demonstrated. Additional build harnesses exceed a direct correction. | reject |
| B7: Caller discards sanitized worker failure category. | **low.** True: failure still propagates as refusal and cannot synthesize proof, but diagnosis loses the category. Safe classification exposure would require validating the private field before echoing it. The bounded Development-only diagnostic improvement does not warrant extra classification surface here. | reject |
| B8: New-file patch paths are absolute. | **medium.** Retained owner patches prefix new paths with home/administrator while tracked paths are repository-relative, so replay creates the wrong tree. Normalize paths and update patch digests; verify reverse application against the actual owner tree. | patch |
| B9: Focused command receipts omit opt-in build properties. | **medium.** tenants-build-final.command.json lists mapping but omits EnablePrivateOwnerSourceReadProof=true, required for the tested selected host. Retain fresh exact commands/environments for both selected and unselected builds and distinguish the incomplete historical receipts. | patch |
| B10: Referenced command logs are ignored. | **medium.** git check-ignore confirms focused/launcher-controls-final.log is absent from the reviewable change set. Preserve byte-identical checkout-visible output copies plus digest/path mapping without staging or weakening ignore policy. | patch |
| E1: Bounded range or omitted tail can satisfy complete source validation. | **medium.** Same demonstrated validator defect as B2, including latest/last mismatch. Existing expected complete-history semantics already settle the correction. | patch (group B2) |
| E2: Invalid private filename throws outside the handler. | **low.** Same private-custody scenario as B3; the legitimate publisher cannot produce it and fail-closed custody is appropriate. Adding skip/recovery guards is unnecessary for ordinary use. | reject |
| V1: Ordinary Debug/source tests compile without the host opt-in. | **medium.** Filed as Other finding and verified against the differing test/host preprocessor gates and csproj. Same normal-build regression as B1. | patch (group B1) |

Six patch groups survive: selected-test compilation, complete history validation, Linux IPC test selection, replayable patches, exact build receipts and checkout-visible outputs. No intent-gap, bad-spec or deferred entry survives. The existing maintenance 403 and historical NU1109 remain acceptance blockers, not waived gates.

### Maintenance continuation — three-layer review, 2026-11-05

All three same-capability layers completed against the 106,472,895-byte unified snapshot `8fa14f0e72da4e419feef22dda340912963ce0e39cbeb067e92eadaf4a351af5`, including untracked source/evidence and externally advanced root gitlinks. No layer was skipped. Each finding is graded independently before grouping; the Blind Hunter finding-floor arithmetic is not an issue.

| Finding | Verdict and evidence | Route |
| --- | --- | --- |
| M-B1: Unkeyed legacy readers reject valid native deliveries. | **medium.** Projects, Tenants and Parties register legacy authoritative readers whose successful result intentionally omits NativeProof. Admission now selects those and throws delivery_identity_conflict. The preserved isolated reproduction confirms true/false constructor behavior. Select only the SDK's explicitly enrolled native reader, retaining the previous native-proof gateway fallback when no such enrollment exists; cover coexistence and no fallback after an enrolled reader refuses. | patch |
| M-B2: Malformed full-reader proof keys escape as provider exceptions. | **medium.** BoundStreamReadProof.Issue can throw FormatException/CryptographicException, which neither this proof source nor the authoritative reader translates. The metadata credential path already treats these configuration failures as unavailable. Preserve cancellation and refuse malformed full-reader proof credentials without HTTP dispatch; add both malformed-base64 and malformed-PKCS8 controls. | patch |
| M-B3: An injectable reader can return a foreign or inconsistent authoritative result. | **low.** A deliberately substituted implementation can violate IAuthoritativeEventStreamReader's authoritative-result contract. The actual SDK AuthoritativeEventStreamReader constructs Identity from the requested identity and validates every page, head, count and complete native digest. M-B1 removes arbitrary unkeyed registrations from this private native enrollment. No supported reader or ordinary request can produce the claimed counterfeit result; an additional validation framework for faulty in-process providers is unwarranted. | reject |
| M-B4: Multiple domain registrations select the last unkeyed reader. | **low.** Repeated registrations can replace the default reader; the new method explicitly configures one exact managed domain and its sole production consumer enrolls folders once. Foreign domains refuse without dispatch. Multi-domain enrollment is not an existing caller, and additional registration arbitration or public selection machinery is disproportionate to this unsupported configuration. M-B1 still preserves all ordinary reader registrations. | reject |
| M-B5: Maintenance lacks a second HTTP-level negative credential matrix. | **low.** The native lane proves the actual new HTTP route. Focused tests exercise the exact compiled filter/action, missing/duplicate/read scope, permission, tenant-selector, unauthenticated and role/operator refusals before state access. The controller retains the installed HumanPolicy rather than introducing a new authentication stack. No credential-admission failure is demonstrated; a separate duplicate HTTP harness adds complexity beyond a direct correction. | reject |
| M-B6: Owner delivery documentation omits the new operator route. | **low.** The owner cutover runbook still describes only Tenant reconciliation. Add the exact platform route/header/permission, fixed target, role/operator requirements and existing outcome semantics, preserving backup/quiescence and no-downgrade rules. This is a direct documentation correction. | patch |
| M-B7: Corrected Folders suite was run serialized only. | **low.** The isolated race and serialized correction are proven, but there is no post-fix full default-policy receipt. Run the unchanged full Folders assembly under its ordinary execution policy and retain its exact command/totals; no test assertion or execution default changes are required. | patch (verification) |
| M-B8: Telemetry audit has no retained executable queries or binding evidence. | **false.** The reviewed snapshot includes qualification-5/parent-telemetry-audit.py. It uses the original native-console peer service/instance pairs, requires exactly one row per pair in exactly one database, and contains every read-only span/log/exemplar/metric query. The raw peer binding and original trace windows remain in native-console.json. | reject |
| M-B9: Log and exemplar silence lacks positive channel controls. | **low.** The stopped, positively bound current database contains 39,374 logs and 29,599 exemplars, but these positive counts were not retained in the first summary. Add bounded read-only positive channel counts and sanitized resource-binding rows to a supplement, retaining the original receipt unchanged and its observed-window limits. | patch (evidence) |
| M-B10: Cancellation test covers only pre-dispatch cancellation. | **low.** InvokeAsync already checks linked cancellation and actual expiry after the SDK returns, and the client checks again after response receipt. The test's late-success claim is broader than its current execution. Add delayed-result controls for cancellation and expiry after dispatch, with an SDK callback that ignores cancellation; retain existing deadlines and behavior. | patch |
| M-E1: Invalid private request names/failed atomic claim stop the worker. | **low — carried.** Same location/claim and unchanged code as prior B3/E2. The only supported publisher creates unique lowercase-hex identities in fresh owner-private custody. Unsupported manual tampering or a broken private filesystem fails closed; recovery/skip branches would mask custody failures. Preserve the prior rejection and do not patch/defer again. | reject |
| M-V1: Real hosts' legacy unkeyed reader registration rejects native requests. | **medium.** Independently confirmed through real Projects/Tenants/Parties registrations and `/tmp/platform-reader-review-repro/repro.csproj`: an identical valid request passes with requireNativeProof=true and throws delivery_identity_conflict with false. This is an Other finding, verified against the actual reader and admission. | patch (group M-B1) |

Six correction groups survive: native enrollment compatibility, malformed proof credentials, operator documentation, default Folders verification, positive telemetry-channel receipt, and post-dispatch cancellation/expiry tests. No intent gap, spec redesign or deferred issue survives; approved authority, source-proof checks and runtime assertions remain unchanged. Verification/evidence-only corrections run on the parent; code/documentation corrections re-engage the existing implementation child.

## Verification

Inspect owned runtime before code changes through the existing isolated lifecycle. Build relevant individual owner projects in Debug/source mode, then run built xUnit assemblies with `-class` selectors for workload/stream client, controller, helper and owner-runtime controls. Require nonempty passes and clean builds.

Run `python3 -m unittest discover -s tests/tools -p test_supported_project_reads_launcher.py`, then fresh `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 DOTNET_PROCESSOR_COUNT=2 python3 tools/qualification/run_supported_project_reads.py --regressions --timeout 900`. Require persisted scenarios, relevant full local assemblies and owned cleanup. Retain failed attempts separately; run `git diff --check` per changed repository.


### Implementation dispatch result — 2026-11-05

The approved Client/Testing/Tenants/Projects source repair is implemented. The fresh native duplicate attempt proves the actual persisted administrator event against the original accepted receipt, command, correlation and actor. The prior unconditional owner-SDK guard is resolved; storage history remains unchanged.

At source manifest `1849fc6b0f9ab7f644828e23fee2d893f28555eafb37c6e95e712643fb951f60`, focused SDK/helper/owner/controller/Projects/launcher checks pass **25/19/10/131/127/122**. Local Integration passes **212/212** and local Server **1,331/1,331**, with the documented live selectors excluded and zero failures/errors/skips/not-run. Debug/source builds have zero warnings/errors. Initial failed builds and both native attempts remain separate.

Fresh distributed qualification is **0 passed, 1 failed** at the subsequent existing `POST api/v1/admin/projections/system/global-administrators/delivery-reconciliation/global-administrators/global-administrators`: HTTP **403**. The admin filter and controller scope creation reject reserved public `system`, and this maintenance action has no supported platform admission. No Project seed/read, later provider or full live regression runs. The source-only helper was not expanded and no maintenance/history/checkpoint bypass was applied. This is a distinct remaining blocker; package-mode NU1109 is still separate.

The [implementation packet](evidence/story-6-1-owner-source-20261105/README.md) retains all commands, full heads/source changes, native XML, seven-criterion evidence limits, source manifest and exact owned cleanup/external preservation. Story/sprint/spec remain in-progress pending remaining authenticated acceptance and parent review. No Git mutations or dependency changes occurred.


### Parent review closure and final verification — 2026-11-05

All three review layers completed. All six correction groups are resolved: exact Tenants test/host compile selection, complete source range/sequence/tail validation, Linux IPC test selection, repository-relative replayable source patches, exact selected/unselected build receipts, and checkout-visible byte-identical outputs. The other findings retain their separately recorded rejected verdicts; nothing was deferred and no frozen intent changed.

At corrected source manifest `970e0d526fd71d5ca3dfba99b53515e662f10c26f56a2c2b7709740d76de4cfc` (17,078 inputs), parent verification passes SDK **25/25**, helper **32/32**, selected Tenants **10/10**, Integration **212/212** and Server **1,331/1,331**, plus **122/122** launcher controls. The separate unselected Tenants correction run passes **9/9**. All totals have zero failures/errors/skips/not-run; full local Project runs retain their documented live exclusions. Builds have zero warnings/errors.

The fresh parent native attempt again proves actual durable bootstrap completion and the exact original persisted administrator event under the stricter complete-history checks, then fails **0 passed, 1 failed** at the unchanged checkpoint reconciliation HTTP **403**. No Project seed/read, later provider or full live regression runs. All four captured containers, six exact sidecars and the AppHost/CLI original process identities are independently absent; private removal and external preservation pass. The owned review build mapping is removed.

[Parent commands, XML and final source/cleanup checks](evidence/story-6-1-owner-source-20261105/parent-final/final-verification.json) bind these results to unchanged source. All **36** copied outputs validate byte-for-byte and remain checkout-visible. Packet-local raw-artifact attributes preserve XML, patch and command bytes; source/build attributes are unchanged. Original source bundles and failed attempts remain separate.

Implementation and review of the authorized source repair are complete; Story 6.1 acceptance remains incomplete. Spec/story/sprint stay **in-progress**, and no Git mutation or pin change occurred. The remaining [maintenance owner proposal](evidence/story-6-1-owner-source-20261105/maintenance-owner-handoff.md) requires renegotiating the frozen prohibition on new public endpoints before any production maintenance admission is implemented. No package-mode rerun or acceptance is claimed.


### Maintenance implementation handoff — 2026-11-05

The approved platform maintenance route, exact admission/technical-state controls, selected SDK/Folders authoritative source isolation, bounded Task36 test completion barrier and distinct foreign-grant fixture correction are implemented. The evidence task remains open for parent acceptance verification and workflow review; no frozen intent or production authority changed beyond the approved maintenance route.

Fresh qualification-5 at source manifest `2a5c6333df9a267f5a6132dc908d3218d8ce3f20692f8485525dd51a0a63c005` (17,082 inputs) passes all 11 cases across three independent fresh providers: duplicate 1/1, full replay 1/1 and main 1/1. Sequential full runtime Integration passes 222/222 and Server 1,378/1,378, all with zero failures/errors/skips/not-run. Focused maintenance 142/142, SDK source controls 52/52, Folders source/human controls 22/22, corrected isolated Task36 2/2, serialized full Folders 1,424/1,424 and final grant controls 4/4 pass. The full SDK DomainService run has 1,374 passes, zero failures/errors/not-run and one existing opt-in live-content skip. Exact commands, source phases and initial failures remain distinct.

The [implementation packet](evidence/story-6-1-platform-maintenance-20261105/README.md) maps all seven criteria to actual evidence and explicit limits. In particular, current Server XML embeds denial headers/logs/exported traces and timing observations; the distributed timing file records 27 safe-404 observations. Current native console custody has coverageVerified=true but privacyEqualityAccepted=false and makes no causal/fault/load equality claim. The independent [parent persisted-dashboard audit](evidence/story-6-1-platform-maintenance-20261105/qualification-5/parent-telemetry-validation.json) positively binds six original peers to the stopped dashboard database, proves zero exported spans/logs/exemplars for 15 original traces and zero exemplars across 14 positive protected POST dimensions, with 1,407 ordinary server-span controls and positive native exports. Query-only inspection leaves the database unchanged. Observed-window, 50 ms mitigation, accepted aggregate-count inference and fault/load limits remain; final parent acceptance/review is pending. The [parent console audit](evidence/story-6-1-platform-maintenance-20261105/qualification-5/parent-console-validation.json) verifies all six original private file identities/hashes/coverage and removes only those files plus their original directory; no raw custody remains. The [parent runtime audit](evidence/story-6-1-platform-maintenance-20261105/qualification-5/parent-runtime-validation.json) independently revalidates source, all 11 cases, regressions and runtime absence.

Independent final checks prove all 13 heads and frozen source unchanged, all 12 captured containers and 18 exact sidecars absent, original AppHost/CLI and old/new Projects restart identities absent, no matching session, and external preservation. Fixture private material and the owned focused mapping are removed; the separately retained native-console custody was subsequently verified and exactly removed by the parent. All 44 output copies are byte-identical and nonignored, and four complete owner-relative source patches pass reverse checks. Failed maintenance phases, placement startup/cleanup/recovery, Task36 failures and foreign-grant before/after results remain immutable historical evidence.

Spec/story/sprint remain in-progress until parent acceptance and review. Package-mode NU1109 remains separately recorded and unrerun; no package acceptance, dependency/pin/checkout change, staging, commit or push is claimed.


### Final parent acceptance and review closure — 2026-11-05

All six maintenance-review correction groups are resolved. The existing implementation child supplied an internal qualified native-reader registration, preserved ordinary legacy readers and native gateway fallback, safe malformed-key refusal, operator documentation, and post-dispatch cancellation/expiry controls. Focused SDK/helper checks pass 59/59 and 34/34. No finding was deferred; rejected/carried findings retain the individual verdicts above. Frozen intent is unchanged.

At final source manifest `fd22b04355f941d920ee3ecaa5d5b68bea047e7f01486df55cc33d2a30451e22` (17,083 inputs), parent verification passes full SDK 1,381 with one existing opt-in live-content skip, helper 34/34, full Folders under its default execution policy 1,424/1,424 and launcher 122/122. SDK/helper/Folders Debug builds have zero warnings/errors. Fresh qualification-6 passes all 11 distributed cases across three independent providers and full runtime Integration 222/222 and Server 1,378/1,378, with zero failures/errors/skips/not-run. Source remains unchanged throughout.

Independent query-only current dashboard verification positively binds all six original peers; all 15 original denial traces have zero spans/logs/exemplars, all 14 positive protected POST dimensions have zero exemplars, and ordinary native/server/span/log/exemplar controls are positive. Six private console captures independently match their original identities/hashes and are silent across the 15 windows; exact raw custody and the separate review mapping are removed. All 12 captured containers, 18 sidecars and original AppHosts are independently absent. All seven story criteria have current evidence under the already approved 50 ms floor, aggregate-count inference and observed-window/fault/load limits.

[Final parent verification, seven-criterion map, commands, XML and review outcome](evidence/story-6-1-platform-maintenance-20261105/parent-review/README.md) supersede earlier pending acceptance statements. Spec is done; story and sprint are review per Build's terminal synchronization rule. Historical package-mode NU1109 remains separate and unrerun under the approved Epic 6 readiness contract; production enrollment, published packages, global READY and exhaustive 6.7 equivalence are not claimed. No staging, commit, push, branch, dependency or checkout change occurred; the approved no-Git-mutation constraint overrides the workflow's default local-commit step.
