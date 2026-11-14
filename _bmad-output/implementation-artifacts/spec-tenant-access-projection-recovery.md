---
title: 'Supported tenant-access projection recovery'
type: feature
created: '2026-11-14'
status: done
route: dispatch
baseline_commit: '9c804569031eee647207fa3b0d88407dadca38a0'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="implementation explicitly authorized by the recovery request">

## Intent

**Problem:** Existing tenant commands cannot restore Projects authorization after missing or stale projections. Historical replay targets another projection and encounters message, sequence, and freshness barriers.

**Approach:** Add supported authenticated `RefreshTenantAccess` command and `TenantAccessRefreshed` evidence event. Emit a complete bounded snapshot from authoritative aggregate state at a fresh sequence; preserve all business state. Projects replaces access evidence atomically through EventStore persistence.

## Boundaries & Constraints

**Always:** Work within the root-declared EventStore, Tenants, and Projects repositories; preserve existing changes. Require current TenantOwner or verified global administrator. Permit recovery of disabled tenants without enabling them. Use existing authenticated command admission, aggregate actor serialization, command-ID idempotency and durable publication. Bound snapshot count and serialized bytes before emission, with reusable limits in EventStore. Use EventStore read-model conditional writes for concurrent creation/update. Use a fresh operation/message ID for a later recovery; reuse it across retries of one recovery. Poll terminal command status and outer Projects list with a deadline.

**Never:** Initialize nested submodules; alter tenant roles, membership or lifecycle; mutate projections outside their supported event handlers; bypass authentication; commit, push, or edit the deferred-work ledger. Do not implement the full fixture provisioning spec. Implementation agents must not edit this or the original spec or workflow control artifacts; report changes and evidence to the parent.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Missing projection | Existing tenant and membership | New authoritative evidence restores exact access and persisted watermark | No direct seeding |
| Stale projection | Old roles, obsolete principals, stale timestamps or conflict flags | Newer complete snapshot replaces evidence and removes obsolete principals | Older/equal conflicting evidence cannot grant access |
| Disabled tenant | Disabled authoritative tenant | Remains disabled with exact memberships | Access still denied |
| Unauthorized | Anonymous, foreign actor, insufficient role or forged administrator hint | Rejected by supported admission/authority | No successful recovery event |
| Concurrent retries | Same operation ID, concurrent workers and live events | One command result; monotonic atomic projection state | Bounded conflicts retry or fail visibly |
| Oversized snapshot | Excess entries or encoded bytes | No partial snapshot emitted | Explicit bounded rejection |

</frozen-after-approval>

## Code Map

- `references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/` — reusable snapshot budget guard and existing `IReadModelStore` conditional persistence. `ReadModelWritePolicy` owns retries; do not duplicate Dapr infrastructure.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs` — existing serialization, idempotency and durable publication.
- `references/Hexalith.Tenants/src/Hexalith.Tenants.Contracts/{Commands,Events,Routing}` — command/event and declared supported routing.
- `references/Hexalith.Tenants/src/Hexalith.Tenants.Server/{Aggregates,Routing,Validators}` — state-derived snapshot, trusted authority, intent adapter and validation. Existing duplicate commands remain unchanged.
- `src/Hexalith.Projects.Workers/Tenants/TenantEventHandlers/ProjectsTenantEventHandler.cs`, `ProjectsWorkersModule.cs` — normal subscription delivery and registration.
- `src/Hexalith.Projects/Projections/TenantAccess/` — normalized evidence, strict ordering, fingerprints and atomic complete replacement.
- `src/Hexalith.Projects.Infrastructure/DaprProjectTenantAccessProjectionStore.cs` — preserve persisted key/document compatibility while using platform conditional persistence; existing `DaprProjectsStateStore` first-create branch is unconditional.
- `tests/Hexalith.Projects.Server.Tests/ProjectsTenantEventWorkerTests.cs`, `tests/Hexalith.Projects.Tests/Projections/TenantAccess/ProjectTenantAccessHandlerTests.cs` and module test projects — focused integration seams.

## Tasks & Acceptance

**Execution:**
- [x] EventStore Client projection sources and tests — implement reusable snapshot limits and consume supported conditional persistence.
- [x] Tenants Contracts/Server/Client and their tests — expose, authorize, validate, emit and safely apply refresh evidence, including catalog/REST adapters and existing projections.
- [x] Projects Workers, tenant-access handler and infrastructure store — accept complete snapshots, exact roles/status/configuration-key evidence, fresh watermark/fingerprint and safe concurrent writes.
- [x] Focused module and Projects tests — cover every matrix row and reread persisted state through a fresh store instance.
- [x] `tools/qualification/run_supported_project_reads.py`, `supported_browser_reads.py`, and `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadDistributed*` — add a focused real-OIDC recovery verification scenario to the existing owned runtime; do not claim the full read/browser matrices.
- [x] `_bmad-output/implementation-artifacts/spec-live-e2e-fixture-provisioning.md` — parent records supported HTTP contract, retry semantics, exact commands/results and live evidence; leave fixture tasks pending.

**Acceptance Criteria:**
- Given supported authenticated ingress and existing source tenant, when recovery completes, then no tenant business state changes and persisted Projects authorization matches the source.
- Given concurrent retries and delayed deliveries, when handlers settle, then evidence does not regress or admit obsolete memberships.
- Given the existing live token tenant/principal, when recovery converges, then authenticated Projects list returns HTTP 200; 400/403/503 are not acceptance.

## Implementation Notes

Initial AppHost start failed before code changes: Conversations requires Platform tool-selected source mapping. Use the repository-supported prepare-build path. Do not weaken build gates. Baselines: EventStore `ad1a36a80` (obtain canonical identifier in its repo); Projects baseline above. Existing fixture spec remains in-progress.

- Source baselines: EventStore `ad1a36a80c357da71fbddeb9b55c369e6e457dd3`; Tenants `401d926eb7d51bdf332696ed3f0fe39a812b3501`.
- Supported source preparation succeeded; the mapped ordinary AppHost started with all required owners healthy and was stopped before focused builds. Ordinary topology lacks the canonical owner catalog required by current platform-scoped commands. Live contract verification therefore reuses the existing supported-read runtime with its explicit fresh backend, real OIDC, catalog custody and Dapr identity infrastructure. No additional fixture/profile backdoor is introduced.
- The pre-review launcher guard suite passed: `python3 -m unittest discover -s tests/tools -p test_supported_project_reads_launcher.py` — 147 tests, exit 0.
- First real-OIDC verification attempt reached healthy owner/worker/mTLS resources, then the Integration test build rejected a new test adapter with CA2007 at `ProjectsStateReadModelTestAdapter.cs:13`. No recovery acceptance was claimed; exact AppHost/private containers were removed and pre-existing resources were preserved. The adapter is being corrected before rerun.
- The full Tenants Contracts run executed 1,051 tests: 842 passed, 209 failed, zero skipped. Four failures were additive command/event inventory expectations to correct. The remaining governance checks require unavailable nested Builds fixtures; no nested submodule was initialized and no gate was weakened.
- Inventory corrections now pass. The final full Tenants Contracts run executed 1,054 tests: 849 passed, 205 failed only in missing nested Builds governance/release fixtures, zero skipped. Pre-review full Tenants Client passed 100/100 after recognizing the additional checkpoint-only event.
- Pre-review focused lanes passed 333 tests: EventStore snapshot budgets 4; Projects handler/authorization 28 and persisted store/worker 11; Tenants server/authority/validation/projections/documentation 72, complete Client 100, and affected Contracts 118. XML results and exact commands are retained for final evidence.
- A supported but unrelated advisory-readiness check inherited by the existing live seed helper timed out before recovery. The focused scenario now excludes that advisory prerequisite while preserving it for the original browser/read scenarios. Anonymous live recovery returned 401; a foreign authenticated caller reached the expected synchronous `InsufficientPermissionsRejection` (the existing gateway maps domain rejections to 422). The live test now checks that exact typed rejection and terminal status rather than assuming every domain rejection is an asynchronous 202.
- Snapshot serialization is bounded before sorting/materializing keys. Recovery also accepts unrelated non-empty whitespace configuration-key names that existing Tenants validation permits; Projects continues filtering to its own configuration namespace.

## Spec Change Log

- 2026-11-14: Review corrections enforce the frozen ordering, preservation and bounded-verification requirements. The owned live scenario narrows both freshness budgets to 90 s; no frozen intent or full-fixture task was changed.

## Review Triage Log

| Finding | Verdict / route | Evidence and disposition |
| --- | --- | --- |
| BH-1: legacy Tenants consumer advances through partial access evidence | high / patch | The non-checkpointed handler calls the metadata-only transition at sequence 4 and subsequently drops a delayed removal at 3. Access-only evidence must not advance that consumer's whole-state watermark; retain the durable adapter's contiguous recovery path and authoritative aggregate no-op. |
| BH-2: live recovery starts with usable evidence | medium / patch | The live proof verifies delivery, exact state and 200 but does not establish failed access before recovery. Use the existing configurable freshness policy in this owned scenario to observe stale denial first; no direct projection edits or business-state changes. |
| BH-3: duplicate transforms still issue a conditional save | low / reject | Confirmed physical ETag/write overhead while logical version and contents remain unchanged. Normal completed deliveries are skipped by the SDK marker layer. Eliminating the rare extra write requires another policy/branch mechanism; it is not an unsafe or non-idempotent logical outcome and is outside this recovery fix's necessary complexity. |
| BH-4: per-conflict handler debug logs removed | low / reject | Confirmed per-attempt detail is not passed into the platform policy. Exhaustion still includes key and attempt budget and is logged by the SDK processing boundary with event context; no silent delivery success occurs. Adding a separate event-context transport or retry wrapper would duplicate platform plumbing for a debug-only improvement. |
| BH-5: default atomic update rejects old custom writers | false / reject | The default explicitly represents an unsupported read-only capability. Both registered production writers override it, and remaining read-only fakes are used by authorization reads. No current writer reaches the default; unsupported external implementations must implement the newly required atomic capability rather than receive a concurrency-unsafe compatibility write loop. |
| BH-6: projection deadline not propagated | medium / patch | `StateAsync` receives the test token instead of the 30-second token, and a late successful read can return after expiry. Share the deadline token and check it before accepting convergence. Same root cause as EH-2. |
| BH-7: evidence verifier accepts zero/negative matching watermarks | medium / patch | Executed the verifier with both 0 and -1: both were accepted. Require positive integer source/projection/snapshot watermarks and explicit persisted-version advancement, with negative controls. |
| BH-8: fresh recovery ID after prior completion unverified at ingress | medium / patch | The current live test exercises only one operation ID. Add a second authenticated operation without resetting subscriber/store state and verify one additional event and newer persisted evidence. |
| BH-9: receiver malformed-input coverage | low / patch | Add constructible invalid DTO cases for size, unknown status, invalid roles and repeated configuration keys, asserting no grant/partial valid projection. Duplicate principals cannot be represented by the event's conforming `IReadOnlyDictionary`; do not invent an impossible dictionary implementation to test that suggestion. |
| BH-10: pre-existing .NET 11 qualifier paths still target net10 | medium / outside task | The .NET 11 props changes were present at activation and were not authored by this recovery build. Actor/package qualification repair belongs to that existing migration work; no pre-existing file is reverted and no deferred-work ledger is edited. Source/local verification does not claim that qualifier passed. |
| EH-1: delayed revocation hidden by newer unrelated delta | high / patch | Snapshot 50 → TenantUpdated 52 → removal 51 leaves the principal authorized with the current drop rule. After a complete recovery base, known gaps and unseen late deltas newer than that base must fail closed until fresh complete evidence; events covered by a newer snapshot remain safely ignored. |
| EH-2: late state reads exceed convergence deadline | medium / patch | Confirmed same token omission as BH-6; address once in the shared private helper. |
| VG-1: real mapped snapshot conflict detection unverified | medium / patch | Existing conflict tests substitute fingerprints manually. Exercise real worker payload changes with identical message metadata, plus reordered-equivalent collections, and assert persisted conflict/denial or idempotence. |
| BH-2 follow-up: recovery proof narrows only mutation freshness | medium / patch | Live rerun `f` observed 120 list HTTP 200 responses and then timed out before any recovery command. `AuthorizeListAsync` uses the diagnostic budget; set both existing budgets to 90 seconds only for this owned scenario and assert both in topology controls. |

All accepted patch findings, including the diagnostic-budget follow-up, are resolved and verified. Rejected findings remain rejected as logged. The pre-existing .NET 11 qualifier issue remains outside this task; the deferred-work ledger was not edited. No commit or push was made under the workspace-preservation instruction.

## Verification

Build and run individual Debug/source test projects with native xUnit filters when necessary. Record exact commands, XML/totals, persisted state, unauthorized rejection, concurrent retries and live list status. Use `aspire describe` for endpoint discovery; report environmental blockers separately from focused test evidence. No secrets in logs or retained reports.

Final verification: 351 focused native checks plus 150 launcher controls passed,
with zero failures/skips. Native coverage comprises EventStore limits 4, Projects
core 30 and persisted store/worker 21, Tenants server 72, full Client 101, Contracts 118,
and deadline/topology controls 5. Exact commands and XML are retained.

`python3 tools/qualification/run_supported_project_reads.py --scenario tenant-access-recovery --timeout 600 --evidence /tmp/tenant-access-recovery-live-20261114-g`
passed its single real-OIDC test. Existing unchanged access evidence first became
stale at 90.60 s and Projects list returned 404. Anonymous recovery returned 401;
foreign recovery returned 422 with the exact permission rejection and durable
Rejected status. Eight concurrent 202 responses produced one snapshot at 4; a
second fresh authenticated operation produced one snapshot at 5 without resets.
Persisted versions advanced 2 → 3 → 4, and source/projection/snapshot watermarks matched
at 4 then 5. Exact status, memberships, roles and configuration-key evidence were
preserved. The same token/principal reached Projects list HTTP 200. Exact owned
cleanup, private-material removal and pre-existing-resource preservation passed.

Every intent-matrix row has executed coverage: missing/stale/disabled evidence,
obsolete membership, ordering gaps and delayed revocation; source authority,
target substitution and oversized evidence; first-create races and bounded
conflicts; authentication, concurrent retry deduplication, fresh subsequent
operations and outer HTTP 200. [Evidence summary](evidence/tenant-access-projection-recovery-20261114/verification-summary.json)
and [exact commands](evidence/tenant-access-projection-recovery-20261114/commands.json)
retain results, review dispositions and source fingerprints.

Earlier rerun `e` failed before collection with `OSError` while the filesystem had
only about 220 MiB available after cleanup. Removing only this task's temporary
build outputs recovered about 1 GiB. Rerun `f` then revealed that list reads use the
diagnostic freshness budget; both existing budgets are now 90 s only for this
scenario. Its initial 120 HTTP 200 responses and timeout were not accepted.
Final `g` verified the actual runtime settings and passed against stable source.

Broader gate remains blocked: full Tenants Contracts has 205 release/governance
failures from absent nested Builds fixtures (849 pass of 1,054; zero skipped).
Package-mode delivery requires coordinated EventStore→Tenants→Projects release
and cohort qualification. This completed prerequisite does not claim production
readiness, package qualification, shared-deployment migration or completion of
the full fixture spec, which remains in-progress.
