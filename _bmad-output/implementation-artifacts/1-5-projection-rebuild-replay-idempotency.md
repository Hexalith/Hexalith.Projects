# Story 1.5: Projection rebuild / replay & idempotency

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **Projects platform engineer**,
I want **deterministic projection rebuild and idempotent handling of duplicate command and projection-event delivery proven on the trivial Epic-1 event set**,
so that **at-least-once Dapr delivery and projection rebuilds never corrupt read models or aggregate state, and the property is cheap to extend per epic** _(realizes FS-6, NFR-7; AR-23)_.

This is the **FS-6 cross-cutting foundational slice** — one of the load-bearing Epic-1 invariants defined **once** and CI-enforced, not reinvented per epic. Story 1.4 built `ProjectAggregate.Handle`/`ProjectStateApply`, the `ProjectListProjection` + `ProjectDetailProjection` read models, and the `ProjectProjectionEnvelope`, all already **deterministic and idempotent-tolerant by construction** (1.4 explicitly deferred the proof suite here: _"the rebuild/replay-determinism + duplicate-delivery idempotency proof suite is Story 1.5… here just keep `Apply` deterministic and idempotent-tolerant"_). Story 1.5 turns those latent properties into an **explicit, repeatable, named test suite** and adds the small production affordance a rebuild needs (an explicit full-stream rebuild entry point that is provably identical to incremental application).

**Scope discipline — this story is the rebuild / replay / idempotency PROOF SUITE for the EXISTING 1.4 events/projections ONLY** (`ProjectCreated` + the `ProjectListProjection`/`ProjectDetailProjection`/`ProjectState`). The epic is explicit that duplicate-**command** dedup and duplicate-**projection-delivery** idempotent `Apply` are **separate properties that fail independently** — keep them as **separate Tier-1 tests**. Defer to their own stories: `TenantAccessProjection` + Tenants-event dedup-by-message-id + durable dedup store (1.6); `GetProject`/`ListProjects` query surfaces (1.7); `UpdateProjectSetup`/`ArchiveProject` and their events (1.8) — when those events arrive, the per-epic extension hook authored here is reused, not rewritten; Aspire/Dapr/Workers topology + the real durable projection/dedup store + the dead-letter / replay-rebuild **runbook** (1.9). This story proves the property **purely** on the in-memory event set; the production durable rebuild path is 1.9. Do **not** introduce a `V2` event, a new command/event, an authorization layer, or a Dapr/state-store dependency here.

## Acceptance Criteria

1. **Deterministic projection rebuild — rebuild from events == incremental state (FS-6, AR-8, NFR-7).**
   **Given** a stream of `ProjectCreated` events (the trivial Epic-1 event set)
   **When** `ProjectListProjection` / `ProjectDetailProjection` are **rebuilt from the full event stream** (an explicit, repeatable rebuild operation) and compared against the state produced by **incrementally applying the same events**
   **Then** the rebuilt projection state is **identical** to the incrementally-applied state (same events → same state) — value-equal across every projected item (keys, names, lifecycle, sequence watermark, timestamps)
   **And** rebuild is **order-stable and deterministic**: rebuilding twice from the same stream yields byte-for-byte-equal state, rebuilding from a shuffled-but-sequence-consistent enumerable yields the same final state (the `(Sequence, IdempotencyKey, IdempotencyFingerprint)` tiebreaker is honored), and the fold uses **no wall-clock/`DateTime.Now`/random/GUID** — only event-carried data
   **And** rebuild is exposed as a **named, tested production affordance** (a `Rebuild(IEnumerable<ProjectProjectionEnvelope>)` static factory on each projection that is provably equivalent to `Empty.Apply(...)`), not an ad-hoc test-only fold.

2. **Idempotent duplicate-COMMAND delivery — at-least-once dedup with field-scoped equivalence (NFR-7, AR-3, FS-6).**
   **Given** at-least-once command delivery (the same logical `CreateProject` attempt delivered twice through the pipeline)
   **When** the second delivery carries the **same `Idempotency-Key` and an equivalent (field-scoped) payload**
   **Then** `ProjectAggregate.Handle` **dedupes**: the second delivery produces **no second `ProjectCreated`** (an `IdempotentReplay` result with an empty events list), and applying it to state is a no-op (`ProjectStateApply` skips the recorded fingerprint) — proving end-to-end that a redelivered command never produces a second event
   **And** a **same-`Idempotency-Key` / non-equivalent-payload** redelivery yields an `IdempotencyConflict` (rejected, mapped to the shared `ReferenceState.Conflict`) — never a silent overwrite, never a second event
   **And** equivalence is **field-scoped via the canonical idempotency fingerprint** (the Story 1.3 `HexalithIdempotencyHasher` contract over the spine's `x-hexalith-idempotency-equivalence` list, already computed by `ProjectCommandValidator`) — not raw object equality.

3. **Idempotent duplicate / out-of-order PROJECTION-event delivery — separate property from command dedup (NFR-7, AR-8, AR-23, FS-6).**
   **Given** at-least-once projection-event delivery (the same `ProjectCreated` dispatched to a projection twice, and an out-of-order delivery)
   **When** the duplicate envelope is folded into `ProjectListProjection` / `ProjectDetailProjection` (same event, same or tied sequence) **and** when envelopes arrive out of sequence order
   **Then** `Apply` is **idempotent**: the projection reflects the event **exactly once** (one item, no duplicate key, no double-count) and tolerates the duplicate without throwing or mutating the item incorrectly
   **And** out-of-order delivery converges to the **same final state** as in-order delivery (the `Sequence` ordering + tiebreaker make the fold order-insensitive for a consistent stream)
   **And** these projection-delivery idempotency tests are authored as **separate Tier-1 tests from the command-dedup tests of AC-2** (the epic requires them to be separate because the two properties fail independently) — and the `ProjectStateApply` replay-dedup (event-level, by recorded fingerprint) is likewise asserted as its own case distinct from the projection-fold idempotency.

4. **Reusable, per-epic-extensible rebuild/idempotency conformance scaffold (FS-6 "cheap to extend per epic"; AR-23).**
   **Given** FS-6 is "proven on the trivial Epic-1 event set **and extended per epic**"
   **When** the proof suite is authored
   **Then** the rebuild-equivalence and idempotency assertions are factored into a **reusable Tier-1 conformance helper** (e.g. `ProjectionRebuildConformance` / assertions in `Hexalith.Projects.Testing`) parameterized over an event stream + a projection factory, so Epic 2/4/5 add a new event type and call the same conformance entry point rather than re-deriving the proof
   **And** the suite documents the **two-axis matrix** it covers (rebuild-vs-incremental × duplicate/out-of-order × command-delivery/projection-delivery/state-apply) so a later author can see exactly where a new event slots in
   **And** the helper is a **reusable guard, not a one-off** (mirrors how Story 1.4 authored `NoPayloadLeakageAssertions`), and stays **pure Tier-1** (no Dapr/Aspire/network/containers/browser).

5. **Determinism guardrails honored; no production behavior regressed; build + filtered lane + gates green (NFR-6, NFR-7, project-context.md).**
   **Given** the existing 1.4 production code is already deterministic/idempotent-tolerant by construction
   **When** Story 1.5 adds the rebuild affordance + proof suite
   **Then** any production change is **minimal and additive** (the `Rebuild(...)` factory must produce state identical to `Empty.Apply(...)` — the projection's internal fold is the single source of truth, not duplicated); **no `V2` event, no new command/event, no authorization layer, no Dapr/state-store dependency** is introduced; schema tolerance (FS-5) and metadata-only (FS-2) are preserved (the new tests reuse the existing events and assert no payload leakage if they serialize)
   **And** all touched projects keep `net10.0` (Contracts netstandard2.0-safe), `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true` — **no `NoWarn`/`#pragma`/`SuppressMessage`/nullable-disable** to force green; xUnit v3 + Shouldly; hand-written `.cs` are CRLF/UTF-8/final-newline; prefer `sealed`; file-scoped namespaces
   **And** `dotnet build Hexalith.Projects.slnx` (0 warnings), `tests/tools/run-filtered-tests.ps1` (the new Tier-1 tests join the fast lane), `run-openapi-fingerprint-gate.ps1` (must stay **PASSED** — no spine/client drift, since no contract change is expected), and `run-frontcomposer-inspect-gate.ps1` (skip-clean — no `[Projection]`/`[Command]` annotations added) all pass. No sibling submodule pointer changes; any (unlikely) new package version reuses the exact sibling-pinned version in root `Directory.Packages.props`, never inline.

## Tasks / Subtasks

- [x] **Task 1 — Add the explicit, repeatable projection rebuild affordance (production, minimal/additive)** (AC: 1, 5)
  - [x] Add a `public static ProjectListProjection Rebuild(IEnumerable<ProjectProjectionEnvelope> envelopes)` to `src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs` that is defined as **exactly** `Empty.Apply(envelopes)` (one-liner delegating to the existing deterministic fold — do **not** duplicate the fold logic; the existing `Apply` is the single source of truth so rebuild and incremental application cannot drift). Add the matching `Rebuild` to `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailProjection.cs`.
  - [x] XML-doc each `Rebuild` as the FS-6 / AR-8 "rebuild from the full event stream" entry point: pure, deterministic, order-stable, tenant-guarded (inherits `Apply`'s guard), throw-on-unknown-event (inherits `Apply`'s policy), no wall-clock/random. Note it is the in-memory rebuild proof; the durable/production rebuild path (state-store reload + dead-letter replay runbook) is Story 1.9.
  - [x] Do **not** change the `Apply` fold semantics, the envelope shape, the ordering, the tenant-guard, or the throw-on-unknown policy. This task is purely the named entry point + docs. Confirm `ProjectStateApply` already exposes the equivalent aggregate-side rebuild via `ProjectState.Empty.Apply(events, identity)` (it does — used as the aggregate-rebuild baseline in tests; no production change needed there).

- [x] **Task 2 — Reusable Tier-1 rebuild/idempotency conformance helper (FS-6 per-epic extension hook)** (AC: 1, 3, 4)
  - [x] Create `src/Hexalith.Projects.Testing/Replay/ProjectionRebuildConformance.cs` (mirror the reuse intent of Story 1.4's `Hexalith.Projects.Testing/Leakage/NoPayloadLeakageAssertions.cs`): pure, no infra deps. Provide assertions parameterized over (a) an `IReadOnlyList<ProjectProjectionEnvelope>` event stream and (b) a projection-rebuild delegate + projection-equality comparison, asserting:
    - **rebuild == incremental**: `Rebuild(stream)` value-equals folding the stream one envelope at a time;
    - **rebuild is order-stable**: rebuild twice → equal; rebuild from a reversed/shuffled (but sequence-consistent) enumerable → same final state;
    - **duplicate delivery is idempotent**: appending a duplicate of any envelope leaves the rebuilt state value-equal to the de-duplicated stream;
    - **out-of-order converges**: a permutation of the stream rebuilds to the same final state.
  - [x] Make the helper generic enough that Epic 2/4/5 pass a new event type's stream + projection without re-deriving the proof; document the covered matrix in XML doc (rebuild-vs-incremental × duplicate/out-of-order, applied per projection). Keep it pure Tier-1 (no Dapr/Aspire/network/containers/browser). Reference `Hexalith.Projects.Testing` from the test project (it already is, via the 1.4 leakage harness).

- [x] **Task 3 — Tier-1 projection REBUILD determinism tests (AC-1)** (AC: 1, 5)
  - [x] Create `tests/Hexalith.Projects.Tests/Replay/ProjectionRebuildDeterminismTests.cs` (xUnit v3 + Shouldly, pure). Build a small multi-project, multi-tenant `ProjectCreated` stream (reuse the constants/`Created(...)` factory shape from `tests/Hexalith.Projects.Tests/Projections/ProjectProjectionTests.cs` — do not invent new fixtures). Assert via the Task-2 helper for **both** `ProjectListProjection` and `ProjectDetailProjection`:
    - `Rebuild(stream)` equals incremental `Empty.Apply` per-envelope (same events → same state);
    - rebuild twice → equal state (deterministic); rebuild from a reversed enumerable with consistent sequences → same final state (order-stable tiebreaker);
    - the rebuilt detail item's `Sequence` watermark + `CreatedAt`/`UpdatedAt` come **only** from event-carried data (no wall-clock) — assert against the fixed `DateTimeOffset` in the fixture.
  - [x] Add an **aggregate-state rebuild** test: `ProjectState.Empty.Apply(events, identity)` (full-stream replay) equals incrementally applying the same events one-by-one — same events → same `ProjectState` (mirror the FS-6 invariant on the write side too).

- [x] **Task 4 — Tier-1 duplicate-COMMAND-delivery dedup tests (AC-2) — SEPARATE from AC-3** (AC: 2, 5)
  - [x] Create `tests/Hexalith.Projects.Tests/Replay/CommandDeliveryIdempotencyTests.cs` (pure). These prove the **command** axis end-to-end (extend, don't duplicate, the replay/conflict cases already in `ProjectAggregateHandleTests`): redeliver the **same** `CreateProject` (same `Idempotency-Key`, equivalent payload) against the post-create state → `IdempotentReplay`, `Events` empty, and **applying that (empty) result to state is a no-op**; redeliver same key + non-equivalent payload → `IdempotencyConflict` → `ReferenceState.Conflict`, no second event. Assert equivalence is **field-scoped via the canonical fingerprint** (same key + a field that is NOT in the equivalence list, if any such exists per the spine, still replays; a field that IS in the list differs → conflict) — verify against `ProjectCommandValidator`/the `HexalithIdempotencyHasher` semantics, not raw equality.
  - [x] Explicitly assert the **separation**: the second delivery produces **no second `ProjectCreated`** when folded through `ProjectStateApply` (the recorded `IdempotencyFingerprints` entry deduplicates), proving the command-dedup property independently of the projection-fold property.

- [x] **Task 5 — Tier-1 duplicate / out-of-order PROJECTION-event-delivery idempotency tests (AC-3) — SEPARATE from AC-2** (AC: 3, 5)
  - [x] Create `tests/Hexalith.Projects.Tests/Replay/ProjectionDeliveryIdempotencyTests.cs` (pure). For **both** projections: fold the **same** `ProjectCreated` envelope twice (same sequence, and a tied-sequence duplicate) → exactly **one** item, no duplicate key, lifecycle/name correct (mirror the Folders `DuplicateCreationEventsShouldReplayDeterministically` pattern in `Hexalith.Folders/tests/.../FolderCreationProjectionReplayTests.cs`); fold envelopes **out of sequence order** → same final state as in-order; assert no double-count and that `Projects.Count` is exactly the number of distinct canonical keys.
  - [x] Add the `ProjectStateApply` **event-level replay dedup** case as its own test (distinct from the projection-fold): applying the same `ProjectCreated` (same recorded `IdempotencyKey` + `IdempotencyFingerprint`) twice via `ProjectStateApply.Apply` returns unchanged state (extends the existing `IdenticalReplay_IsDeduped_StateUnchanged` in `ProjectStateApplyTests` — reference it, keep the new replay-suite case focused on the at-least-once-delivery framing).
  - [x] Keep this file's tests **physically separate** from Task 4's command-dedup file so the FS-6 "separate stories / separate tests because they fail independently" requirement is structurally visible.

- [x] **Task 6 — Wire into the filtered fast lane + run all gates green** (AC: 4, 5)
  - [x] Confirm the new `tests/Hexalith.Projects.Tests/Replay/*` land in `Hexalith.Projects.Tests` (already in the filtered lane per Story 1.4) — no csproj change expected; if `Hexalith.Projects.Testing` gained a new folder, confirm it still builds as part of the slnx and is referenced by the test project. Verify `tests/tools/run-filtered-tests.ps1` picks them up (Tier-1 lane).
  - [x] Run and record in the Dev Agent Record: `dotnet build Hexalith.Projects.slnx` → 0W/0E; `tests/tools/run-filtered-tests.ps1` → all pass (note the new test count delta); `tests/tools/run-openapi-fingerprint-gate.ps1` → **PASSED** (must be unchanged — no spine/client edit; if it reports drift, you accidentally touched the spine/`.g.cs` — revert); `tests/tools/run-frontcomposer-inspect-gate.ps1` → **SKIPPED (clean)** (no `[Projection]`/`[Command]` annotations added).
  - [x] Confirm no sibling submodule pointer moved (`git status` shows only Projects-module files under `src/`/`tests/`); no inline `Version=`; no compiler-setting weakening.

## Dev Notes

### What Story 1.4 left you (verified on-disk reality — PROVE the latent property, don't rebuild the machinery)
- **Production projections already deterministic + idempotent-tolerant by construction** — do not rewrite them, prove them:
  - `src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs`: `sealed record`, private ctor over `FrozenDictionary<string, ProjectListItem>`, `Empty` singleton, pure `Apply(IEnumerable<ProjectProjectionEnvelope>)` with deterministic ordering `OrderBy(Sequence).ThenBy(IdempotencyKey, Ordinal).ThenBy(IdempotencyFingerprint, Ordinal)`, **envelope/event tenant-agreement guard** (foreign event skipped via `continue`), `TryKey` canonical-identity derivation (malformed → skipped), and **throw-on-unknown-event** (`default:` throws `InvalidOperationException`). `Contains`/`Get` accessors exist.
  - `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailProjection.cs`: same pattern; `ProjectDetailItem` carries a `Sequence` watermark + `CreatedAt`/`UpdatedAt` (both = `OccurredAt` at creation).
  - `src/Hexalith.Projects/Projections/ProjectList/ProjectProjectionEnvelope.cs`: `sealed record (string TenantId, long Sequence, IProjectEvent Event)` — the rebuild input unit.
- **Write-side already replay-built + replay-deduped:** `src/Hexalith.Projects/Aggregates/Project/ProjectState.cs` has `Empty` + `Apply(IEnumerable<IProjectEvent>, ProjectIdentity)` (full-stream replay). `ProjectStateApply.cs` enforces the canonical-identity foreign-event guard, **dedupes identical idempotent replays** (same `IdempotencyKey` + `IdempotencyFingerprint` already recorded → returns state unchanged), records the fingerprint on apply, and **throws on unknown event types**. `IProjectEvent` (`src/Hexalith.Projects.Contracts/Events/IProjectEvent.cs`) carries `TenantId`, `ProjectId`, `CorrelationId`, `TaskId`, `IdempotencyKey`, `IdempotencyFingerprint`, `OccurredAt` — the fold uses only event-carried data (no wall-clock).
- **Command-side idempotency already implemented:** `src/Hexalith.Projects/Aggregates/Project/ProjectAggregate.cs` `Handle` checks `state.IdempotencyFingerprints` BEFORE the duplicate-create guard → same key + equal fingerprint = `IdempotentReplay` (empty events), same key + different fingerprint = `IdempotencyConflict`; `ProjectResult.ToRejectionReason()` maps both Duplicate/Conflict → `ReferenceState.Conflict`. The canonical fingerprint is computed in `ProjectCommandValidator` (reusing the Story 1.3 `HexalithIdempotencyHasher` field-scoped equivalence over the spine's `x-hexalith-idempotency-equivalence` list).
- **Existing tests to EXTEND (reference, don't duplicate):**
  - `tests/Hexalith.Projects.Tests/Aggregates/Project/ProjectAggregateHandleTests.cs` already has `IdempotentReplay_SameKeySamePayload_NoSecondEvent`, `IdempotencyConflict_SameKeyDifferentPayload_Rejected`, `DuplicateCreate_Rejected`, plus the `Command()` + `ApplyCreated(...)` factories — reuse those shapes for AC-2.
  - `tests/Hexalith.Projects.Tests/Aggregates/Project/ProjectStateApplyTests.cs` already has `IdenticalReplay_IsDeduped_StateUnchanged` — extend it (or reference it) for the AC-3 state-apply replay-dedup case.
  - `tests/Hexalith.Projects.Tests/Projections/ProjectProjectionTests.cs` already has `ListProjection_DeterministicOrdering_AcrossEqualSequences`, `*_AppliesProjectCreated`, the tenant-guard skip, throw-on-unknown, and the FS-8 cross-tenant case, plus the `Created(...)` factory + `TenantA`/`TenantB`/`ProjectIdValue` constants — reuse those for AC-1/AC-3 fixtures.
- **Reuse harness pattern:** `src/Hexalith.Projects.Testing/Leakage/NoPayloadLeakageAssertions.cs` (Story 1.4) is the precedent for a reusable Tier-1 conformance helper in `Hexalith.Projects.Testing` — mirror its "reusable guard, not a one-off" intent for the Task-2 `ProjectionRebuildConformance` helper.

### Mirror the Folders replay-test pattern — do NOT reinvent (primary anti-pattern to prevent)
`Hexalith.Folders` already has the exact FS-6 proof-suite shape this story mirrors. Read and mirror:
- **`Hexalith.Folders/tests/Hexalith.Folders.Tests/Aggregates/Folder/FolderCreationProjectionReplayTests.cs`** — `DuplicateCreationEventsShouldReplayDeterministically` (duplicate envelope → count 1, deterministic), `TiedSequencesWithDifferentContentShouldOrderByIdempotencyKey` (forward/reversed → same final state via the `(IdempotencyKey, IdempotencyFingerprint)` tiebreaker), `ProjectionShouldIsolateSameFolderIdAcrossTenants`, null/malformed-segment skip. **This is the AC-1/AC-3 template.**
- **`.../Projections/FolderList/FolderArchiveProjectionReplayTests.cs`** — replay across two event types with the envelope/event tenant-agreement guard (the shape Epic 2/4 will reuse when `ProjectArchived`/reference events arrive — note it for the Task-2 extension-hook design).
- **`.../Aggregates/Folder/FolderAccessProjectionReplayTests.cs`** — additional replay-conformance examples.
- These are **xUnit v2** in Folders (Parties/Folders convention) — author the Projects equivalents in **xUnit v3 + Shouldly** to match the Projects module (Stories 1.1–1.4). Mirror the *test design*, not the xUnit version.

### Architecture compliance (guardrails)
- **Pure `Handle`/`Apply`; persist-then-publish; read model never authority** [AR-3, architecture.md#Communication Patterns L411-412, #Enforcement L441/L445]. The rebuild affordance reads events and produces a read model; it never writes the read model as authority and never mutates outside the fold. `Handle` stays pure; `Apply` mutates in-memory state only.
- **Idempotency under at-least-once delivery** [NFR-7, architecture.md#Format Patterns L407-408, #Communication Patterns L417]. `Idempotency-Key` per logical attempt, field-scoped equivalence via the shared hasher, **required on mutations / rejected on queries** (already enforced by the spine/validator — this story proves the dedup behavior, it does not change the rule). Pub/sub is at-least-once → handlers must be idempotent and tolerate out-of-order — the AC-3 tests are the proof.
- **Projection rebuild is a mandatory negative-path test** [architecture.md#Pattern Enforcement L449-451, AR-23 L132]. "Tier-1 tests assert pure handlers, **replay compatibility**…"; "Mandatory negative-path tests: … idempotency (duplicate command + duplicate projection delivery), **projection rebuild**." This story owns those two named entries on the trivial event set.
- **Deterministic rebuild = same events → same state** [FS-6 epics.md L280, Story 1.5 ACs epics.md L420-431]. Duplicate-**command** dedup and duplicate-**projection-delivery** idempotent `Apply` are **separate** because they fail independently — keep them as separate tests (the epic states this explicitly).
- **Canonical identity + tenant isolation** [AR-4, FS-8, NFR-1]. Rebuild keys derive only from `ProjectIdentity` (the projections already do via `TryKey`); the cross-tenant skip is inherited from `Apply`. Don't re-prove FS-8 broadly (1.4 owns it) but the rebuild fixtures should include ≥2 tenants so isolation is implicitly exercised under replay.
- **Metadata-only + additive schema** [FS-2, FS-5, NFR-2, NFR-6]. Reuse existing `ProjectCreated`; introduce **no `V2`** and no new event; if any test serializes an event, no forbidden field may appear (reuse `NoPayloadLeakageAssertions` if you serialize).
- **Determinism rule** [project-context.md L87]. "Preserve deterministic tests: avoid wall-clock sleeps where fake time… exists." Use the fixed `DateTimeOffset` fixtures (UnixEpoch / a fixed instant, as the existing projection tests do); never `DateTimeOffset.Now`/`Guid.NewGuid()` in a Tier-1 fold or fixture.

### Library / framework requirements
- **.NET 10**; domain core + `Hexalith.Projects.Testing` stay pure; nullable + implicit usings + warnings-as-errors; prefer `sealed`; file-scoped namespaces. **Central Package Management only — no inline `Version=`.** This story should need **no new package** (it reuses the existing xUnit v3 / Shouldly / NSubstitute stack + the existing projection/aggregate types); if anything is genuinely missing, add it to root `Directory.Packages.props` reusing the exact sibling-pinned version, never inline.
- **xUnit v3 + Shouldly** (match EventStore/Tenants/Folders-in-Projects convention; pins already in `Directory.Packages.props`). Do **not** use xUnit v2. Reuse EventStore/Testing + Tenants/Testing fakes/builders before inventing doubles (for this pure event-set proof, the existing in-module factories suffice — no EventStore runtime needed).
- **`System.Text.Json`** only for any (optional) serialization in tests — events already have their converters; vocabulary enums are name-based. Newtonsoft is the NSwag-client serializer only (not used here).
- **Do not** add/upgrade Fluent UI, Dapr, Aspire, Roslyn, Fluxor, or the SDK. **Do not** add a Dapr/state-store/Workers dependency — the durable rebuild path is Story 1.9; this story is the **pure in-memory proof**.
- **Identifier rule (R2-A7):** never `Guid.TryParse` a `ProjectId`; ULID-shaped non-whitespace strings (the existing `ProjectIdValue` fixture constant is a valid ULID shape — reuse it).

### File / structure requirements (target — additive)
```text
src/Hexalith.Projects/Projections/
├── ProjectList/ProjectListProjection.cs            # MODIFY — add static Rebuild(...) => Empty.Apply(...) (no fold duplication)
└── ProjectDetail/ProjectDetailProjection.cs        # MODIFY — add static Rebuild(...) => Empty.Apply(...)
src/Hexalith.Projects.Testing/
└── Replay/ProjectionRebuildConformance.cs          # NEW — reusable Tier-1 rebuild/idempotency conformance helper (FS-6 per-epic hook)
tests/Hexalith.Projects.Tests/Replay/
├── ProjectionRebuildDeterminismTests.cs            # NEW — AC-1: rebuild == incremental, order-stable, no wall-clock; + aggregate-state rebuild
├── CommandDeliveryIdempotencyTests.cs              # NEW — AC-2: duplicate-command dedup (replay vs conflict), field-scoped, no second event
└── ProjectionDeliveryIdempotencyTests.cs           # NEW — AC-3: duplicate/out-of-order projection delivery + state-apply replay dedup (SEPARATE from AC-2)
```
- File-scoped namespaces under `Hexalith.Projects.*` matching folder path. **CRLF** on all hand-written `.cs`, UTF-8, final newline (Stories 1.1–1.4 reviews fixed LF violations — write CRLF from the start). Private fields `_camelCase`; interfaces `I`-prefixed; `Async` suffix on async methods (none expected here — pure synchronous folds); prefer `sealed`.
- **No generated `.g.cs` change expected** — this story must not touch the spine or regenerate the client (fingerprint gate must stay PASSED unchanged).

### Testing requirements
- **Tier-1 pure** (`Hexalith.Projects.Tests`, `Hexalith.Projects.Testing`): rebuild/replay/idempotency are aggregate/projection-level pure properties — **no Dapr/Aspire/network/containers/browser** (project-context.md#Testing Rules; AR-23 L132). No Tier-2/Server test is needed for this story (the durable/Workers rebuild path is Story 1.9).
- **Mandatory, named, SEPARATE proofs** (architecture.md#Pattern Enforcement L450-451): (1) **projection rebuild** = same events → same state; (2) **duplicate-command** dedup (replay vs conflict, field-scoped); (3) **duplicate/out-of-order projection delivery** idempotent `Apply`. The epic requires (2) and (3) to be **separate tests** because they fail independently — enforce this structurally (separate files, Task 4 vs Task 5).
- **Reusable, not one-off:** the Task-2 conformance helper is the FS-6 "cheap to extend per epic" affordance — Epic 2/4/5 reuse it for new events. Mirror Story 1.4's reusable `NoPayloadLeakageAssertions` precedent.
- **Determinism:** fixed `DateTimeOffset` fixtures only; assert order-stability by rebuilding from reversed/permuted (sequence-consistent) enumerables and comparing final state; no wall-clock/random/GUID in folds or fixtures.
- **Do NOT** broaden scope into Story 1.6 (Tenants-event dedup-by-message-id / durable dedup store / restart-fail-closed), 1.7 (query surfaces), 1.8 (new lifecycle events), or 1.9 (Aspire/Workers/durable store/runbook). Prove the property on the **existing trivial event set** only.

### Project Structure Notes
- **Alignment:** targets match architecture.md#Complete Project Directory Structure (`Projections/` rebuildable read models; `Testing/` reusable test utilities; `tests/` Tier-1). The `Rebuild` factory + the `Replay/` test folder are additive — no structural deviation.
- **Decisions the dev must make + document in the Dev Agent Record:**
  1. **`Rebuild` API shape** — a static `Rebuild(envelopes)` factory delegating to `Empty.Apply(...)` (recommended: zero fold duplication, provable equivalence) vs an instance method. Confirm it does not duplicate fold logic. Note the choice.
  2. **Conformance-helper signature** — how generic to make `ProjectionRebuildConformance` (delegate over rebuild + equality) so Epic 2/4 plug a new event type in without re-deriving. Document the extension point.
  3. **Field-scoped equivalence assertion** — how you exercise that equivalence is fingerprint-based (which `x-hexalith-idempotency-equivalence` fields participate) rather than raw equality. Verify against `ProjectCommandValidator`/`HexalithIdempotencyHasher`; note which fields are in/out of the equivalence set.
- **Variances to flag:** (1) Two production projection files get a small additive `Rebuild` method — confirm it is a pure delegate to the existing fold (no behavior change → fingerprint/leakage/schema unaffected). (2) New `Replay/` test folder + a new `Testing/Replay/` helper folder. (3) Generated `.g.cs` and the spine are **untouched** — fingerprint gate stays PASSED; frontcomposer gate stays skip-clean.
- **Submodule discipline:** root-level submodules only; never `--recursive`; never modify sibling submodule pointers. All new/modified files live under umbrella-root Projects-module paths (`src/`, `tests/`).

### References
- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.5: Projection rebuild/replay & idempotency (L412-431)] — the three BDD acceptance criteria: (1) rebuild from events == incremental state (same events → same state, tested/repeatable); (2) duplicate-command dedup (no second `ProjectCreated`, field-scoped equivalence; same-key/different-payload = conflict); (3) duplicate/out-of-order projection-event delivery → idempotent `Apply`, **separate Tier-1 tests** from command-dedup.
- [Source: _bmad-output/planning-artifacts/epics.md#Cross-Cutting Foundational Slices (FS-6, L280)] — "Deterministic rebuild (same events → same state) proven on the trivial Epic-1 event set and **extended per epic**; duplicate-**command** dedup and duplicate-**projection-delivery** idempotent `Apply` are **separate stories** (they fail independently). (NFR-7; AR-23.)"
- [Source: _bmad-output/planning-artifacts/epics.md#NonFunctional Requirements (NFR-6, NFR-7, L84-85)] — schema additive/no-`V2`/backward-compatible; idempotency under at-least-once delivery (dedupe by message id; `Idempotency-Key` required on mutations, rejected on queries; field-scoped equivalence hashing).
- [Source: _bmad-output/planning-artifacts/epics.md#Additional Requirements (AR-3, AR-4, AR-8, AR-23)] — EventStore sole write authority + pure `Handle`/`Apply` + persist-then-publish; canonical `{tenant}:projects:{projectId}`; tenant-scoped **rebuildable** freshness/watermark-bearing projections; xUnit v3 + Shouldly + reuse EventStore/Tenants Testing fakes + **mandatory negative-path tests incl. idempotency (duplicate command + duplicate projection delivery) + projection rebuild/replay**.
- [Source: _bmad-output/planning-artifacts/architecture.md#Format/Communication/Process Patterns (L401-435) + #Enforcement Guidelines / Pattern Enforcement (L437-454)] — persist-then-publish; pure Handle/Apply, read model never authority; `Idempotency-Key` field-scoped equivalence required-on-mutation/rejected-on-query; at-least-once pub/sub → idempotent + out-of-order-tolerant handlers; **Tier-1 tests assert replay compatibility**; mandatory negative-path tests include duplicate-command + duplicate-projection-delivery idempotency + projection rebuild.
- [Source: _bmad-output/implementation-artifacts/1-4-create-project-end-to-end-tracer-bullet.md] — the production projections/state/aggregate this story proves (deterministic ordering `(Sequence, IdempotencyKey, IdempotencyFingerprint)`, tenant-guard, throw-on-unknown, replay-dedup map), the explicit deferral of the rebuild/idempotency proof suite to 1.5, the `Command()`/`Created()`/`ApplyCreated()` test factories to reuse, and the reusable `NoPayloadLeakageAssertions` precedent for the conformance helper.
- [Source: _bmad-output/implementation-artifacts/1-3-openapi-contract-spine-generated-typed-client.md] — the `HexalithIdempotencyHasher` field-scoped canonical fingerprint over `x-hexalith-idempotency-equivalence`; the `run-openapi-fingerprint-gate.ps1` (real gate — must stay PASSED, no spine/`.g.cs` edit) and `run-frontcomposer-inspect-gate.ps1` (skip-clean) gate scripts.
- [Source: Hexalith.Folders/tests/Hexalith.Folders.Tests/Aggregates/Folder/FolderCreationProjectionReplayTests.cs · Projections/FolderList/FolderArchiveProjectionReplayTests.cs · Aggregates/Folder/FolderAccessProjectionReplayTests.cs] — the canonical replay/rebuild/idempotency Tier-1 test templates to mirror (duplicate envelope → count 1; forward/reversed → same final state via the idempotency-key tiebreaker; tenant isolation under replay; multi-event-type replay with the envelope/event tenant-agreement guard). Author the Projects equivalents in xUnit v3 + Shouldly.
- [Source: src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs · ProjectDetail/ProjectDetailProjection.cs · ProjectList/ProjectProjectionEnvelope.cs] — the production folds to add `Rebuild(...)` to and prove (single-source-of-truth `Apply`, deterministic ordering, tenant-guard, throw-on-unknown).
- [Source: src/Hexalith.Projects/Aggregates/Project/ProjectState.cs · ProjectStateApply.cs · ProjectAggregate.cs] — write-side replay (`Empty.Apply(events, identity)`), event-level replay-dedup by recorded fingerprint, and command-side replay/conflict idempotency to prove for AC-2/AC-3-state-apply.
- [Source: _bmad-output/project-context.md#Testing Rules / Critical Don't-Miss Rules (incl. L87 determinism rule)] — Tier-1 pure (no Dapr/Aspire/network/containers/browser); preserve deterministic tests (no wall-clock); reuse EventStore/Tenants Testing fakes; central package management; xUnit v3 + Shouldly; additive/no-`V2`; never hand-edit `.g.cs`.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.7 (1M context) — `claude-opus-4-7[1m]`

### Debug Log References

- `dotnet build Hexalith.Projects.slnx` → **0 Warning(s), 0 Error(s)**.
- `tests/tools/run-filtered-tests.ps1` → all green: Contracts **111**, Client **16**, Projects.Tests **70** (was 50 → **+20** new Replay tests), Server **11** = **208 total** (Story 1.4 baseline was 188 → +20).
- New `Hexalith.Projects.Tests.Replay` namespace alone: **20 passed / 0 failed**.
- `tests/tools/run-openapi-fingerprint-gate.ps1` → **PASSED** (generated artifacts match the Contract Spine fingerprint — spine/`.g.cs` untouched).
- `tests/tools/run-frontcomposer-inspect-gate.ps1` → **SKIPPED (clean)** (no `[Projection]`/`[Command]` annotations added).
- `git status` → only Projects-module files under `src/`/`tests/` + bmad artifacts; no sibling submodule pointer moved; no `Directory.Packages.props` change (no new package needed).

### Completion Notes List

**What was built (proof suite for the latent 1.4 properties — production change minimal/additive):**
- **Task 1 (production, additive):** Added `public static Rebuild(IEnumerable<ProjectProjectionEnvelope>)` to both `ProjectListProjection` and `ProjectDetailProjection`, each defined as **exactly** `=> Empty.Apply(envelopes)` — a one-liner delegate to the single-source-of-truth fold, so rebuild and incremental application provably cannot drift. No `Apply` fold semantics, envelope shape, ordering, tenant-guard, or throw-on-unknown policy changed. Aggregate-side rebuild already exists via `ProjectState.Empty.Apply(events, identity)` (no production change needed there). Fingerprint/leakage/schema unaffected (no serialized contract changed) → fingerprint gate stayed PASSED.
- **Task 2 (reusable Tier-1 hook):** New `Hexalith.Projects.Testing/Replay/ProjectionRebuildConformance.cs` — pure, no infra. `AssertAll<TProjection, TItem>(stream, empty, rebuild, applyOne, extractItems, itemComparer?)` proves the full matrix (rebuild==incremental, deterministic, order-stable via reversed enumerable, duplicate-delivery idempotent, out-of-order converges via a deterministic seedless rotation). Also exposes a reusable `AssertContentEqual` content comparator. Mirrors the reuse intent of Story 1.4's `NoPayloadLeakageAssertions`.
- **Tasks 3/4/5 (three SEPARATE Tier-1 test files):** `ProjectionRebuildDeterminismTests` (AC-1, incl. aggregate-state full-stream-replay == incremental-apply, and explicit no-wall-clock watermark/timestamp assertions against fixed `DateTimeOffset` fixtures), `CommandDeliveryIdempotencyTests` (AC-2), and `ProjectionDeliveryIdempotencyTests` (AC-3) are physically separate files so the FS-6 "fail independently" requirement is structurally visible.

**Decisions documented (per Project Structure Notes):**
1. **`Rebuild` API shape:** chose a static `Rebuild(envelopes) => Empty.Apply(envelopes)` factory (zero fold duplication, provable equivalence) over an instance method — matches the recommended option.
2. **Conformance-helper signature:** parameterized over `(stream, empty, rebuild, applyOne, extractItems, itemComparer?)`. The `extractItems` extractor returns the keyed item dictionary so equivalence is asserted by **content** (value-equal items per canonical key), NOT record equality — important because both projections hold a `FrozenDictionary` reference member, and record-generated `Equals` compares that member by reference, so two independently-rebuilt projections are never record-equal even with identical contents. The `applyOne` delegate (`(p, envelope) => p.Apply([envelope])`) models incremental delivery; Epic 2/4/5 plug a new event type's stream + the same projection delegates with no re-derivation.
3. **Field-scoped equivalence:** verified against `ProjectCommandValidator`/`HexalithIdempotencyHasher` semantics, NOT raw equality. The spine `x-hexalith-idempotency-equivalence` set is `project_metadata.display_name` (the `CreateProject.Name`) + `request_schema_version` (pinned `v1`). **In-set:** `Name` differs → fingerprint differs → `IdempotencyConflict`. **Out-of-set:** `Description`/`SetupMetadata` differ → fingerprint identical → `IdempotentReplay`. `CommandDeliveryIdempotencyTests` proves both directions, including a direct fingerprint cross-check against the validator (`EquivalenceIsFingerprintBased_NotRawEquality_ConfirmedAgainstValidator`).

**Variances flagged (all expected/additive):**
- Two production projection files gained a small additive `Rebuild` method (pure delegate to the existing fold — no behavior change).
- New `src/Hexalith.Projects.Testing/Replay/` helper folder + new `tests/Hexalith.Projects.Tests/Replay/` test folder (additive; `Hexalith.Projects.Tests` was already in the filtered lane and already references `Hexalith.Projects.Testing` — **no csproj/slnx change required**).
- Generated `.g.cs` and the OpenAPI spine **untouched** → fingerprint gate stays PASSED; frontcomposer gate stays SKIPPED (clean).
- No new package added (`Directory.Packages.props` unchanged); reused the existing xUnit v3 / Shouldly stack. No `NoWarn`/`#pragma`/`SuppressMessage`/nullable-disable. Hand-written `.cs` are CRLF/UTF-8/final-newline; all new types `sealed`/`static` with file-scoped namespaces.

**Scope deferred (untouched per story):** Tenants-event dedup-by-message-id / durable dedup store / restart-fail-closed (1.6); `GetProject`/`ListProjects` query surfaces (1.7); `UpdateProjectSetup`/`ArchiveProject` + their events (1.8); Aspire/Dapr/Workers topology + durable projection/dedup store + dead-letter/replay-rebuild runbook (1.9). No `V2` event, no new command/event, no authorization layer, no Dapr/state-store dependency introduced.

### File List

**Modified (production — additive):**
- `src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs` — added static `Rebuild(...)` factory + XML doc.
- `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailProjection.cs` — added static `Rebuild(...)` factory + XML doc.

**New (reusable test helper):**
- `src/Hexalith.Projects.Testing/Replay/ProjectionRebuildConformance.cs` — FS-6 per-epic rebuild/idempotency conformance guard + `ProjectionRebuildConformanceException`.

**New (Tier-1 tests, xUnit v3 + Shouldly, pure):**
- `tests/Hexalith.Projects.Tests/Replay/ProjectionRebuildDeterminismTests.cs` — AC-1 (rebuild==incremental, deterministic, order-stable, no-wall-clock watermark/timestamps, + aggregate-state full-stream replay).
- `tests/Hexalith.Projects.Tests/Replay/CommandDeliveryIdempotencyTests.cs` — AC-2 (duplicate-command dedup: replay vs conflict, field-scoped via fingerprint, no second event).
- `tests/Hexalith.Projects.Tests/Replay/ProjectionDeliveryIdempotencyTests.cs` — AC-3 (duplicate/out-of-order projection delivery + state-apply event-level replay dedup; SEPARATE from AC-2).

**Tracking:**
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — `1-5-…: in-progress → review`.
- `_bmad-output/implementation-artifacts/1-5-projection-rebuild-replay-idempotency.md` — Tasks checked, Dev Agent Record, Status → review.
