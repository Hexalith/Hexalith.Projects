# P1R compatibility, replay, and rollback scenarios

This is the finite verification contract for the accompanying build spec. It is
not a qualification or an owner decision. All data is disposable fixture data.

## Bound coordinates

| Role | Coordinate |
| --- | --- |
| Selected | EventStore 3.110.0, tag v3.110.0, source `27279fe6431925a6ea046c3f89af61487185c7de`; Builds 4.29.1, `21ce044ab465ccb2adab58b3d66e394ffbecf3c2` |
| Rollback | EventStore 3.70.1, tag v3.70.1, `f13f9925fdca53efa2ab8c90d396ab106f91bb9c`; Builds `7af20f8bafbfe561df6f7705913a0800603090b5` |
| Current comparison | EventStore `2c58ffda41759e895ace4b9625c9bd931a217672`; Projects `cbcf54fa4d7a8c17bbfc3f9fb555ac0a85c179c0`; current Builds `688eec9a4333245cc0ff7772115c769094471863` |
| Runtime | Dapr 1.18.2; PostgreSQL `postgres@sha256:a02db8cac496f15b094798a38254f14d6e00741f709360e5e00bb6668ea31636`; private placement/scheduler/discovery and loopback ports |

Record actual rollback archive repository metadata separately from the rollback
tag. Historical evidence indicates those source revisions differ. Compare the
owned source trees and retain the discrepancy; never relabel package provenance.
Re-run comparisons if the current source coordinate changes; never update it
implicitly. The current-source lane is Debug/project references; artifact lanes
use isolated package restoration and Release outputs. Proofs stay distinct.

## Required cases

Every row supplies Given/When/Then acceptance for the named scenario. Run both
version directions where indicated, with independent processes/outputs/caches.

| ID | Given | When | Then |
| --- | --- | --- | --- |
| provenance | Published selected archives and rollback host/probe dependencies | Restore/verify actual NuGet archives | Assert versions, signatures, repository sources, assets graphs and loaded assembly hashes; reject unexpected EventStore source dependencies |
| legacy-metadata | Rollback metadata JSON without a floor, using Pascal and Web conventions | Selected code deserializes it | Preserve sequence/time/ETag and default floor to one |
| metadata-read | Selected metadata with floor one and floor greater than one | Old code reads it | Measure fields/raw-byte stability and ignored-floor behavior; read alone cannot prove downgrade-safe writing |
| metadata-write | Selected retained metadata and a valid covering snapshot | Rollback writer appends through its real EventPersister | Measure persisted metadata before/after; loss of floor is an incompatibility, even if command/state otherwise succeeds |
| full-replay | Untrimmed streams written by each version through real actor/domain commands | Restart using the other version | Assert Counter state, current sequence, event hashes and Tenant isolation without rewriting prior events |
| snapshot-tail | Persisted snapshot plus subsequent committed events | Restart/replay with the other package | Assert snapshot/tail fold equals full replay and event inventory remains unchanged |
| retained-covered | Missing prefix, floor F, covering snapshot at H >= F-1 | Each package hydrates | Assert correct tail/state or record actual incompatibility; do not infer floor support from a successful snapshot |
| retained-uncovered | Missing prefix with absent snapshot or H < F-1 | Each package hydrates | Record explicit missing-event failure; no new domain event or aggregate-state mutation |
| missing-event | Missing required interior or tail envelope | Each package hydrates | Reject rather than silently advance, with no new domain event or aggregate-state mutation |
| invalid-evidence | Invalid floor, unreadable/protected data, and unknown event/type/version fixtures | Reader, actor and domain replay execute | Record exact handling and safe rejection; retain unsupported outcomes instead of substituting an easier fixture |
| query-wire | QueryEnvelope with legacy and dual-principal fields | JSON and DataContract round trips cross versions | Measure preserved/defaulted/dropped fields; authority loss forbids compatibility claims for delegated reads |
| projection-wire | ProjectionEventDto with positive global position | Both cross-version round trips run | Preserve or report lost/unknown watermark; never treat zero or aggregate sequence as authoritative |
| mixed-api | Common legacy calls and selected-only status/trusted-effect/fenced/cursor capabilities | Old client/new host and new client/old host execute | Common calls match; absent APIs yield honest unsupported results; compilation alone is insufficient |
| checkout | Same applicable shared scenarios against selected package and exact current source | Compare outputs and inventories | Declare scoped equivalence or actual delta; inventory new Reminder APIs, registration and dependencies outside the shared scope |
| post-upgrade-restore | Stable committed selected-version state and stopped writers | Dump the owned database, restore to a second fresh owned database, start rollback hosts | Verify backup hash, committed inventory, actor rehydration, Tenant isolation, sequences, event hashes and health; record incompatible downgrade |
| pre-upgrade-restore | A verified rollback-version backup captured before selected writers start | Stop all writers, restore it into another fresh owned database and restart old hosts | Prove the backup restoration mechanics and invariants; this does not establish acceptance of backup-only containment or retain later writes |
| failure-cleanup | Startup failure, timeout/cancellation, and repeated cleanup | Runner terminates its invocation | Bound command receipts remain; owned processes/containers disappear; shared-resource identities/states remain unchanged |

Actor hydration failures may create documented command-status/dead-letter
bookkeeping. Record those separately from prohibited new domain events or
aggregate-state mutations. Do not assert zero infrastructure writes.

## Evidence and interpretation

Retain per-case command argv, working directory, timestamps, exit status,
assertion count, package/assembly/source hashes, fixture identifiers and
persisted inventory hashes. Store only safe diagnostics, counts and hashes;
runtime credentials and database dumps stay in the invocation-owned scratch
area and are destroyed after their hashes and restore results are recorded.

Each execution is `passed`, `failed`, or `unavailable`. Separately record the
compatibility disposition: `compatible`, `incompatible`, or `unverified`.
A test that successfully detects lost metadata or an unsupported API can pass
its negative control while compatibility remains incompatible. Required missing,
skipped, zero-assertion, incompatible or unavailable lanes cannot produce a
qualified P1R result. Validator completeness must not masquerade as qualification.

An executed pre-upgrade restore proves a possible containment mechanism only.
Any proposal to restrict rollback, exclude features, accept data loss or accept
an incompatible newer state/API requires a later named owner decision after the
actual evidence. The existing fixed acceptance record, usability flag, P0/P2/P3/P4,
G-gates, independent readiness and Story 6.1 retain their states.
