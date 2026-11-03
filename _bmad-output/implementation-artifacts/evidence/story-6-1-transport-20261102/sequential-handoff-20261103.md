Story 6.1 sequential implementation handoff — incomplete

The canonical spec remains the sole source of truth: `_bmad-output/implementation-artifacts/spec-6-1-list-and-open-projects-through-supported-authenticated-paths.md`, including “Current implementation handoff: independent persisted scenario groups”. The parent requested this incomplete checkpoint and will append the next concrete handoff. Do not mark Story 6.1 complete or advance acceptance from this packet. No new run or feature edit was started after that request.

Current source implements three separate fresh persisted scenarios under the ordinary unoptimized Debug/Development profile. The public launcher runs `duplicate` (one case), `full-replay` (one case), then `main` (the original nine cases). Each group seeds both Tenants once, uses its own provider, bootstrap, exact six-peer registry/mTLS/runtime/source receipts and native test output, and must finish owned cleanup and external preservation before another group starts. The v2 coordinator requires exactly those three passing current groups and all eleven ordered cases; it cannot reuse a historical passing provider, failed row, cursor or snapshot. Only main runs the complete Projects Integration/Server regressions before its own teardown.

Full replay independently proves its four actual source streams; it does not claim duplicateVerified. Its eight invalid and eight credential attempts, four real rebuilds, original cursor assertions, three recorded eight-read phases, initial actual expected-metadata sweep and every complete state/ETag/source assertion remain. The main nine substantive statements/assertions remain unchanged. The parent independently audited the split, coordinator cancellation/failure paths, strict native/provider/mTLS/census/source joins and current controls with no remaining finding.

The last source change affects only four root C# fixture files: `SupportedProjectReadDistributedFixture.cs`, `SupportedProjectReadDistributedTests.cs`, `SupportedProjectReadScenarioEvidence.cs`, and `SupportedProjectReadEvidenceTests.cs`. Snapshot’s identical twenty required keys now use existing StatesAsync once: four raw named-store hashes are observed atomically and sixteen logical state reads use the existing cap of eight. Exact required inventory/order/nonempty checks and the full owned domain census remain. The unchanged complete provider parity control (actual 104 domain keys plus four binary/escaping controls in recent runs) executes after feature assertions and before the matrix receipt/native success. Fault/cancellation/late-cancellation prevents a receipt. All feature HTTP observations and phases remain; no runtime authority, timestamp, clock, deadline, grant or SDK lease behavior changed in this source correction.

Current source-bound checks are retained in `observer-scheduling/commands.json` and `scenario-groups/commands.json`:

- Focused root C# controls: 125/125, zero errors/failures/skips/not-run; `observer-scheduling/story61-observer-scheduling-fixture.xml`.
- Python launcher controls: 103/103, zero failures/skips, including the original 84 and coordinator/binding/refusal controls; `scenario-groups/story61-scenario-provider-joins-controls.log`.
- Current owning test project build: zero warnings/errors, 6.03s. The preceding complete mapped Integration/AppHost dependency build was clean (52.75s); the final change was confined to its C# test assembly and was rebuilt. Every live attempt also built its actual fresh AppHost/source graph as recorded.
- Previously source-bound, parent-verified unchanged SDK gates include 1,319 broad SDK routing/controller/actor tests; 48 held-metadata controls in Debug and 48 with DEBUG absent; 78 owning ServiceDefaults cancellation/finite/opt-in controls; 17 DomainService auxiliary controls; 199 contract controls. These are not the missing complete Projects Integration/Server regressions.
- Initial analyzer failures and corrected results are preserved; no suppression or skip was introduced. Final source hashes still match the 125-control record. SDK ActivationBinding source was not edited.

Exact local verification commands (working directory is the workspace root):

```sh
python3 /tmp/build-story61-helper.py dotnet build tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj -c Debug -m:1 -p:SupportedProjectReadDistributed=true --no-restore -p:BuildProjectReferences=false

dotnet tests/Hexalith.Projects.Integration.Tests/bin/Debug/net10.0/Hexalith.Projects.Integration.Tests.dll -class '*SupportedProjectReadEvidenceTests' -class '*SupportedProjectReadFixtureTopologyTests' -class '*SupportedProjectReadTopologyTests' -class '*SupportedProjectReadConcurrencyTests' -xml /tmp/story61-observer-scheduling-fixture.xml

python3 -m unittest discover -s tests/tools -p test_supported_project_reads_launcher.py

python3 /tmp/build-story61-helper.py dotnet build tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj -c Debug -m:1 -p:SupportedProjectReadDistributed=true

git diff --check
```

The build helper’s exact source is retained in `scenario-groups/build-story61-helper.py`. It adds the actual source roots, UseHexalithProjectReferences=true, UseNuGetDeps=false, EnablePrivateCatalogInvocationProof=true, DOTNET_PROCESSOR_COUNT=2 and disabled MSBuild reuse. These are the exact seven emitted local mapping values used for focused verification (live groups emit their own independent mapping receipt; all seven remain unchanged within each group):

```json
{
  "CustomBeforeDirectoryBuildProps": "/tmp/story61-prerequisite-dev-mapping-20261102/source-mapping/SourceMapping.targets",
  "CustomAfterMicrosoftCommonProps": "/tmp/story61-prerequisite-dev-mapping-20261102/source-mapping/SourceMapping.props",
  "CustomBeforeDirectoryBuildTargets": "/tmp/story61-prerequisite-dev-mapping-20261102/source-mapping/SourceMapping.before-targets.props",
  "CustomAfterDirectoryBuildTargets": "/tmp/story61-prerequisite-dev-mapping-20261102/source-mapping/SourceMapping.late.props",
  "AfterMicrosoftNETSdkTargets": "/tmp/story61-prerequisite-dev-mapping-20261102/source-mapping/SourceMapping.final.targets",
  "HEXALITH_SOURCE_MAPPING_HASH": "c900d9d8c2670b712fdf8e3cd501924d50ce0417e186ee04838ae958bfd101ec",
  "Configuration": "Debug"
}
```

The standard qualification command remains:

```sh
python3 tools/qualification/run_supported_project_reads.py --regressions --timeout 900 --evidence <new-absolute-evidence-directory>
```

Run it under the established owner-checked, nonblocking advisory lock `/tmp/hexalith-timesheets-local-runtime.lock`; preserve its inode/contents/permissions and never invoke Timesheets lifecycle actions. Retained `live-*/runtime-admission-wrapper.py`, wrapper receipt and admission receipt show the exact wrapper command. A busy lock starts nothing. No new run is authorized by this document alone beyond the next canonical parent handoff.

Recent actual outcomes, all preserved rather than accepted as substitutes:

- live-48 duplicate qualified: native 1/1, 239.254s, complete persisted controls and cleanup/external preservation pass. Separate full replay failed at 312.595s after all four rebuilds and cursor checks. Its final Foreign page used unchanged Tenant projection timestamps 302.271157 seconds old, correctly exceeding 300 seconds. Original Sentry absence was unproved and private material was retained; the separate exact receipt/process/config/namespace/all-four-container recovery proved absence and removed it. Original failed report remains unchanged.
- live-49 duplicate qualified: native 1/1, 223.282s; complete provider control and all cleanup/external preservation pass. Separate full replay failed at 216.113s on a post-invalid open HTTP 404. Exact Folder callback canceled at 2003.893ms; Tenant evidence remained fresh. All real proof pairs were available/equal; the final canceled client received no HTTP status. There were zero CAS conflicts before the Folder deadline, so contention is not an established cause. Four fresh read decisions and higher fresh-observation time consumed the window. Cleanup/external preservation pass; main never started.
- live-50 refused before seeding: strict registry had five peers, missing projects-workers. Actual retained native daprd error proves sqlite/v1 initialization migrations failed with SQLITE_BUSY(5). The existing 30×1s strict census retry remained. Cleanup/private removal/external preservation pass.
- live-51 failed after fifteen seed commands: GrantFolderAccess HTTP 422, exact domain result UnavailableProjection. The last permission observation expired at 502.612ms against the unchanged 500ms private bound; outer CallerCanceled was false. Genuine held metadata transport was observed, and the canceled client had no HTTP acknowledgement. Native 1 failed/124.305s. All owned cleanup/private removal/external preservation pass.
- live-52 refused before seeding: strict registry had five peers, missing folders-workers. The further native cause is unknown; do not relabel it SQLITE_BUSY from missing registration alone. No optional continuous watchers/proc scans/Redis sampling ran. The sole optional one-shot canonical capture was not ready and refused without accepting input. All owned cleanup/private removal/external preservation pass. No later group started.

All original 500ms permission, 2s native metadata/callback, 300s Tenant source-age, 90s recovery, 15s state timeout and 900s per native group limits remain. No late native completion was accepted, no synthetic refresh/no-op Tenant event or clock adjustment was introduced, and no direct raw-actor authority fallback was added. The selected held metadata path still has genuine issuer/receiver/cancellation/enrollment provenance, fresh host/trust validation and independent receiver admission.

Actual native daprd is 1.18.7, SHA256 `e988a6f53abec017fc9ebcb4f89de9bed11bce005eb49bd8e85e8d3d6adbf488`; see `live-51/actual-native-daprd.json` and the actual six-peer census receipts. No version/pin change was made. Do not inherit an older 1.18.2 description. Root package property 3.133.0 and the retained SDK/catalog coordinate 3.132.1 are distinct; do not silently change either pin.

The proposed auxiliary duplicate-check optimization is rejected. `activation-binding-rejected-profile/commands.json` retains the exact original source, separately compiled baseline/candidate, original standalone project/source layout, binary/input hashes, all trials and exact commands on the retained actual 85-auxiliary activation. The premise was a closing-parenthesis reading error: the original full-list check already executes once outside Any. Cold baseline/candidate medians were 10.257/10.050ms with approximately 13.460/13.461MB allocation; existing warm byte memo medians were 1.101/1.121ms with approximately 1.511MB each. No useful benefit was shown. Original SDK/copied functions agree on the binding digest, each operation freshly deserializes owned input, no authority/state/host/trust results are cached, and no SDK edit was selected. Further semantic tests for this rejected candidate were not run.

No owned runtime is active. `sequential-handoff-resource-proof.json` contains fresh read-only proof that the last exact AppHost session/PID, namespace sidecars, all four captured immutable container IDs and exact receipt-bound private configuration directory are absent; our admission is released. All tracked tool/watcher/profile sessions ended. Raw previously captured diagnostics remain only in owner-private directories outside this packet, notably `/tmp/story61-owned-diagnostics-live51-duplicate-nmlen2af` (retained actual activation used by the rejected profile). Public artifacts contain hashes/closed facts, not private keys or bearer tokens.

The original task is incomplete. Required remaining work is a fresh source-stable qualification of all three groups, actual exact eleven-row aggregate, complete Projects Integration and Projects Server regressions under main, per-group exact cleanup/external preservation and the parent’s final source/evidence/workflow review. Historical passing duplicate groups and main-row progress cannot qualify a later failed provider. Parent owns the next canonical handoff and the next sequential implementation owner; do not start another run, source edit or concurrent implementation from this checkpoint.

The public marker scan covered 1,372 files and found no bearer JWT or encoded private-key material. Its two literal PEM-marker hits are unchanged passing Harness_DetectsForbiddenContent inline test names in domain-initial.xml/domain.xml, with no key body or end marker; the classification receipt preserves that distinction and both original XML files.
