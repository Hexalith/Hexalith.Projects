# Epic 7 entry verification — 2026-11-25

7.1-P2 and DW-63 remain **open**. Story 7.1 is **blocked** and Epic 7 is **backlog**. The local results below describe the pinned source and fixtures. The owner records at these pins authorize scope. Capability acceptance is pending.

[Implementation-readiness report](../../../planning-artifacts/implementation-readiness-report-2026-11-25.md) · [Sprint tracker](../../sprint-status.yaml) · [Deferred-work ledger](../../deferred-work.md) · [Section G](../../../planning-artifacts/epics-architecture-conformance-checklist-2026-07-16.md#owner-decision-record)

## Revisions

| Repository | Verified commit | Relevant input |
| --- | --- | --- |
| Projects | `e5d4a21bf8547c4d1fce1aab97fc489db3c0c09a` | SDK host, tests and current tracker |
| EventStore | `01e412351ca4b99e95cab68750297b3580726421` | [G-1 scope and admission candidate](../../../../references/Hexalith.EventStore/docs/architecture/g1-durable-task-scope-decision.md) and SDK response policy |
| Platform | `d2ee05007ebae4332df5b3e17e747d450faa83f1` | [G-1 runtime scope and provider probe](../../../../references/Hexalith.Platform/docs/implementation/g1-durable-task-scope-decision.md) |
| Folders | `db5a94c26170253b89b37a54aeaa2e30f9284248` | [G-2 scope and design investigation](../../../../references/Hexalith.Folders/docs/governance/g2-folder-provisioning-scope-decision.md) |
| Builds | `bf8a28b4bbcec619e8d9764d335f786f2bafb69e` | [Dependency catalog](../../../../references/Hexalith.Builds/Props/Directory.Packages.props) selects EventStore `5.5.1` in package mode |

`git ls-tree HEAD references/` supplied the gitlinks. Each of the three owner checkouts matched the respective gitlink and had a clean working tree. The build and fixture jobs used committed inputs in disposable workspaces with .NET SDK 11.0.100. Source builds also used the Projects gitlinks for their other dependencies.

## 7.1-P2 — Durable Task and Folder provisioning

| Requirement | Evidence at the pinned revision |
| --- | --- |
| Task admission, current authority and idempotency | `DurableTaskAdmission` scopes the committed row to `(Tenant, original actor, versioned operation, key)` and compares the normalized request digest. It reauthorizes before returning the same task ID on an equivalent retry. The conditional write creates the task and records confirmation consumption together. The admission suite covers a changed intent, a changed confirmation binding, denied reauthorization, and response loss before and after the write, using test authority. A production admission/confirmation host and its authenticated original-actor binding are still due. |
| Task status, effect receipts and recovery | `GetCommittedAsync` reads admission by scope and intent. The candidate has no task lifecycle, status and effect-receipt surface by task ID, worker discovery, retained task checkpoints, cancellation boundary, or task-specific lease and recovery integration. Existing process leases and receipts apply to event-stream processors. The Platform probe confirms provider CAS and persistence for a probe row across Redis and sidecar restart. Its native suite uses both sidecars to verify admission after startup. |
| Folder provisioning, authority and idempotency | Generic `CreateFolder` derives identity from Tenant and key, rechecks Organization authorization before append, and uses a request fingerprint. The owner metadata-read handler rechecks current read authority. The planned create-only operation, expected-version or absent-stream precondition, admitted-step binding, and sole-manager ACL without inherited Tenant read are still due. EventStore `TrustedEffectOrigin` binds workload, purpose and effect identity, with no verified original-actor field. |
| Folder receipt/status and compensation | Workspace task status and delegated metadata reads cover existing operations. The G-2 scope proposes retaining the Folder and receipt for unresolved activation. Provisioning receipt lookup with current read and manage authority, an exercised lost-reply recovery, and the compensation and rollback proofs remain pending. |

### Owner evidence required

| Accountable owner | Artifact or decision needed |
| --- | --- |
| EventStore Owner | Versioned G-1 capability record and authenticated host contract. Include task-only and confirmation-required admission, exact original-actor and interactive binding, task discovery and status transitions, idempotency retention, committed task and owner-receipt lookup, stable step keys, lease fencing and cancellation. Supply persisted evidence for denial, dependency outage, lost reply and takeover from the committed checkpoint, including effect counts and immutable terminal outcomes. Record dated acceptance against the delivered revision and results. |
| Platform Owner | Accepted runtime record selecting the exact EventStore package or source, provider, Dapr/Redis versions and digests, clock, lease and scheduler bounds and two-replica topology. Supply the persisted worker crash and takeover rehearsal, with stale-worker refusal, one owner effect and retained task/checkpoint/receipt checks. Exercise fenced rollback or approved forward recovery against the same store. Record dated acceptance of the runtime and proof. |
| Folders Owner (Jerome), with EventStore and Platform for the task-bound actor credential | Versioned create-only contract and decision on owner identity, collision semantics, expected version, key scope and the credential or committed lookup binding Tenant, original actor, workload, task step, operation and target. Supply persisted direct and delegated authorization and ACL cases, equivalent and conflicting retry, stale version, collision with an unchanged incumbent, cross-Tenant or revoked authority, outage and post-commit lost reply/restart with one Folder history and a matching status/receipt. Accept retention with `NeedsAttention` or name the explicit idempotent compensator and qualify provenance and current authority. Exercise rollback or fenced forward recovery with the Folder history and grants retained. Record dated owner acceptance. |
| Solution Architect and Test Architect | Dated acceptance of the same EventStore, Platform and Folders capability and runtime coordinates and persisted matrix/rollback artifacts. The Projects prerequisite record requires all five hash-bound decisions against one qualification digest before `7.1-P2-capability-pin.json` can be recorded. |

[7.1-P2 specification and pin schema](../../spec-7-1-p2-qualify-durable-task-and-folder-provisioning.md) · [Owner proposals](../../7-1-p2-owner-scope-proposals.md)

## DW-63 — Projects SDK host

| Recorded acceptance criterion | Evidence and remaining work |
| --- | --- |
| One canonical route per receiver with caller, operation and audience admission | The actual Server registers `AddEventStoreDomainService` once and maps `UseEventStoreDomainService`. Retained human endpoints have their own bearer policy. The pinned SDK supplies the opt-in query response policy and runs it after admitted dispatch. Focused catalog, authorization and HTTP tests exercise the private/fixture paths. Ordinary Debug run-mode still needs owner-held Redis catalog custody and a bound workload issuer followed by positive command, query and projection delivery through the actual AppHost. Restricted grants currently omit `project/rebuild/part/v1` and `project/v2/reconcile` (DW-97). |
| Historical replay, command wire parity and persisted projection parity | The existing keyed processor and SDK replay share the stored-event fold with the full-replay projection. Focused checks cover historical `ProjectFolderCreationPending`, command wire/no-op behavior, and protected SDK dispatch and reads after the host and store client restart. The comparison fixture checks exact positions, read-store deltas and zero unexpected writes. Complete the owner-reviewed golden comparison of both hosts, including command success/rejection/no-op, snapshots, named delivery and large/terminal rebuild paths with the original sequences and global positions. |
| Authenticated v4 comparison and independent retained read rollback | The local exact-source packet recorded 77/77 passing cells with the required persisted deltas. A separate invocation selected `LegacyRollback` and executed all seven authenticated retained reads with zero domain writes. The packet and rollback hashes are below. Projects maintainer and Test Architect acceptance is pending. |

### Owner evidence required

| Accountable owner | Artifact or decision needed |
| --- | --- |
| Projects Owner, with Platform for workload binding | Exact-source positive ordinary Debug AppHost receipt over the owner-held Redis catalog and bound workload issuer. Record committed generation, caller/operation/audience and wrong or missing credential cases, route uniqueness, manifest and composition grant reconciliation, persisted command and query outcomes and projection/replay deliveries. Resolve whether `projects-workers` requires enrollment and qualify the required rebuild-part and reconcile callbacks. DW-92, DW-93, DW-95, DW-97 and DW-109 track related work. For DW-94, the `unavailable` cell can inject a Folder 503 through a client that still has SDK resilience, and the rerun showed a Folder retry. Bind the fixture decision to a receipt showing the fault is isolated to the intended cell. |
| Folders runtime maintainer, with Projects integration owner | Deliver a passing persisted `CreateFolder` named seed delivery receipt and an owner trace that accounts for the delivery timeout. Then the Projects integration owner can rerun the distributed read regressions and retain the passing authenticated Projects callbacks and sidecar cleanup. |
| EventStore release owner → Builds catalog owner → Projects Owner, with CiCd for gitlink policy | DW-99 requires a published DomainService package carrying `IDomainQueryResponsePolicy` and compatible SDK route/catalog behavior, selection in Builds, the Projects Builds gitlink advance, and a passing main `ci / build-and-test` in package mode. Package-mode ordinary AppHost composition is separately deferred under DW-91 by the recorded 2026-11-25 decision. |
| Projects maintainer and Test Architect | Accept the exact-source v4 packet, seven independently executed retained reads bound to its hash, native receipts and store deltas for the applicable candidate. This acceptance also belongs to the Epic 6 v4 action. |
| Projects Owner and EventStore Owner | Record dated DW-63 acceptance for the same Projects source, SDK release/build selection and route, authorization, historical replay, command, projection and supported-read receipts. Projects supplies AD-17 and AD-35 writer proof in the governed Story 7.1 qualification and Story 7.17 cutover, including exclusive claims, fencing and executable recovery. |

[DW-63 specification](../../spec-dw-63-migrate-projects-server-to-eventstore-sdk-host.md) · [Read cutover runbook](../../../../docs/read-cutover.md) · [Epic 7 entry gate](../../../planning-artifacts/epics.md#epic-7-users-complete-durable-project-decisions-and-recover-them)
## Commands and results

### 7.1-P2 and EventStore SDK checks

Each block ran under `run-with-temp` from the indicated pinned source. The source-only builds used the Projects-pinned Builds, Commons and Tenants trees alongside the owner repositories.

The EventStore commands ran from `references/Hexalith.EventStore`.

```bash
project=tests/Hexalith.EventStore.DomainService.Tests/Hexalith.EventStore.DomainService.Tests.csproj
dotnet build "$project" --configuration Debug --artifacts-path "$TMPDIR/artifacts" -p:UseHexalithProjectReferences=true -m:1
tests="$TMPDIR/artifacts/bin/Hexalith.EventStore.DomainService.Tests/debug/Hexalith.EventStore.DomainService.Tests.dll"
dotnet "$tests" -class '*DurableTaskAdmissionTests'
python3 ../Hexalith.Platform/eng/durable-task-qualification/probe_state_store.py --native "$tests"
dotnet "$tests" -class '*CheckpointedProcessRunnerTests' -class '*DomainServiceTrustBoundaryTests' -class '*CatalogMetadataBoundaryTests' -class '*DomainRouteCatalogAuxiliaryTests'
```

Build exit 0, zero warnings and errors. Admission passed 4 tests. The native two-sidecar admission check passed 3. The process and SDK trust/catalog lane passed 167. All three test invocations exited 0 with zero failures or skips.

From `references/Hexalith.Platform`, `run-with-temp python3 eng/durable-task-qualification/probe_state_store.py` exited 0. Both probe runs returned first-write statuses `[204, 409]`, `staleWriteRejected=true` and `persistedFenceAfterRestart=2`. Provider coordinates were `daprio/daprd@sha256:ed471547b67db50c831d498b8ee861822fb037d9d594c38ac299546b425f575a` and `redis@sha256:7f7d30b7fdd09467bac58198a1f166dd69789d348d4fe859cccb20e0cf287fbe`. The [pinned runner](../../../../references/Hexalith.Platform/eng/durable-task-qualification/probe_state_store.py) also records the Python and .NET image digests.

The Folders commands ran from `references/Hexalith.Folders`.

```bash
project=Hexalith.Folders.Tests
dotnet build "tests/$project/$project.csproj" --configuration Debug --artifacts-path "$TMPDIR/artifacts" -p:UseHexalithProjectReferences=true -m:1
dotnet "$TMPDIR/artifacts/bin/$project/debug/$project.dll" -class '*FolderCreation*' -class '*EffectivePermissionsLayeringTests'
project=Hexalith.Folders.Server.Tests
dotnet build "tests/$project/$project.csproj" --configuration Debug --artifacts-path "$TMPDIR/artifacts" -p:UseHexalithProjectReferences=true -m:1
dotnet "$TMPDIR/artifacts/bin/$project/debug/$project.dll" -class '*FolderOwnerReadQueryHandlerTests' -class '*EffectivePermissionsEndpointTests'
```

Both builds exited 0 with zero warnings and errors. The Folders domain lane passed 78 and the Server lane passed 35, with zero failures or skips. The first test command initially used `*FolderCreation*Tests`, which xUnit rejected with exit 3. The command shown above used `*FolderCreation*` and exited 0.

### DW-63 source mode, read regression and rollback

From the pinned Projects root under `run-with-temp`, prepare the source mapping. The first attempt used Git archives and got `HXW001` because workspace resolution requires Git metadata. The rerun used `git clone --shared --revision <full SHA>` for Projects and each direct gitlink in the disposable workspace.

```bash
export HexalithBuildsRoot="$PWD/references/Hexalith.Builds"
dotnet run --project references/Hexalith.Builds/src/libraries/Hexalith.Builds.Module.Cli/Hexalith.Builds.Module.Cli.csproj -- prepare-build --manifest module/hexalith.module-manifest.v2.json --mode source --workspace "$TMPDIR/source-mapping" --output json > "$TMPDIR/mapping.json"
```

Mapping and builds exited 0. The run emitted mapping digest `5bb6776ade9b2f35ec8fc31bf71b4ee98309b503955dbb155136976820f40f3a`. The following commands inherited the returned `buildPreparation.environment`. Each native invocation also wrote an xUnit XML receipt with `-result-xml`.

```bash
for project in Hexalith.Projects.Server.Tests Hexalith.Projects.Tests Hexalith.Projects.Client.Tests; do
  dotnet build "tests/$project/$project.csproj" --configuration Debug -m:1
done
server=tests/Hexalith.Projects.Server.Tests/bin/Debug/net11.0/Hexalith.Projects.Server.Tests.dll
domain=tests/Hexalith.Projects.Tests/bin/Debug/net11.0/Hexalith.Projects.Tests.dll
client=tests/Hexalith.Projects.Client.Tests/bin/Debug/net11.0/Hexalith.Projects.Client.Tests.dll
dotnet "$server" -class '*ProjectsRuntimeCatalogTests' -class '*ProjectsDomainProcessorTests' -class '*SupportedProjectReadHttpTests' -class '*ProjectsAuthenticationContractTests'
dotnet "$domain" -class '*ReadCutoverHistoryTests'
dotnet "$client" -method '*FingerprintVerifiesCurrentForCheckedInArtifacts'
dotnet "$client" -class '*ReadClientCompatibilityTests'
dotnet "$server" -method '*ProtectedSdkDispatchAndReadsSurviveHostAndStoreClientRestart' -failSkips
```

The Server pass was 256 passed and one skipped because no Dapr endpoint was supplied. The last command ran that single method successfully with `STORY61_DAPR_HTTP_ENDPOINT` and `STORY61_DAPR_GRPC_ENDPOINT` pointing at a disposable `daprd` 1.19.1 with `statestore` and `projects-conversation-start-setup` in-memory components. Domain history passed 2, Client fingerprint passed 1 and Client compatibility passed 2. All invocations exited 0.

From that build, the existing `artifacts/dw-63/v4-fixture-control/verify_v4.py` helper (SHA-256 `cc40ea4555770201958b4d7fe20e9acd5e3bc79c9dcf5d81565aadd790244dde`) ran the authenticated matrix, host typed-result check and a separate legacy rollback run. The helper used a disposable Dapr sidecar and the three domain/client XML receipts. The command inside the scratch Projects root was `python3 "$base/artifacts/dw-63/v4-fixture-control/verify_v4.py"`, with `$base` pointing at the original Projects workspace. All three native invocations exited 0 and the fixture verified all 77 cells and 7 rollback reads. The source manifest recorded the exact current commit and has SHA-256 `36b223dd90f3fbd5054f6f1f84ee5e401596415c804dead7d5c29861930f5f25`.

| Artifact | SHA-256 |
| --- | --- |
| `v4-packet.json` | `b41791eb14e36b50ea42417250855527ad6c72c8561a787eb0dc1195a3223256` |
| `v4-rollback-proof.json` | `2cafadf8a03e1b09ebb8b20074b1d0dfe0d04db34895a62358b86d47647c33b4` |

### DW-63 package mode and distributed regressions

The Release command ran from a disposable Projects snapshot with the pinned Builds catalog.

```bash
CI=true dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Release --artifacts-path "$TMPDIR/artifacts" -p:UseHexalithProjectReferences=false -m:1
```

Exit 1 after restore with zero warnings and one error. `src/Hexalith.Projects.Server/Queries/ProjectsQueryResponsePolicy.cs(8,53)` reports `CS0246` for `IDomainQueryResponsePolicy` with the selected EventStore `5.5.1`. Server tests could not start in package mode.

The live command ran under `run-with-temp` from the pinned disposable Projects checkout with its direct references.

```bash
python3 tools/qualification/run_supported_project_reads.py --regressions --timeout 600 --artifacts "$TMPDIR/distributed-build" --evidence "$receipt_dir/live-regressions"
```

Exit 1. The disposable live topology accepted the bootstrap commands through Folders `CreateFolder`. `DuplicateDeliveryUsesActualPersistedOwnersAsync` then failed with `TaskCanceledException` in `WaitForSeedNamedDeliveryAsync` during `WaitForFolderCreationReadinessAsync`. This repeats the seed-delivery blocker recorded in the DW-63 spec. It occurred before Projects business callbacks or the complete regression suites. `live-regressions/duplicate/distributed.xml` records 1 failed test, zero skips, and has SHA-256 `2d26f661a41cea3604c4b605c9f09ffaa92063890b06b9b12b469283f9f339ce`. `duplicate/run.json` verified removal of the exact AppHost and four owned containers and records sidecar/process cleanup.

Local logs and native receipts are retained in `~/.local/state/hexalith-projects/epic7-entry-e5d4a21b/`. The v4 rollback result records the temporary packet path used during the run. The retained file with the same SHA-256 is in that local evidence directory.

### Planning guard

`python3 tools/planning/validate_production_authority.py --validate-index` passed with `[6, 7, 8]`. The same command passed with `--sprint-status`, `--deferred-work`, `--p0-artifact` and `--workspace-root` pointed at isolated copies of the live inputs and the existing P1R acceptance record. `run-with-temp env PYTHONDONTWRITEBYTECODE=1 python3 -m pytest tests/tools/test_production_authority_guard.py -q -p no:cacheprovider` passed 58 tests and 442 subtests.

All 19 digests in the active superseding Section G byte manifest matched. The hand-reconciled tracker and ledger retain their recorded states and both Section G owner decisions remain `Pending`.
