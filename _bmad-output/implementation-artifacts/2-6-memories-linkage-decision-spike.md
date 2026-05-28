---
baseline_commit: e127b7aab8dbda3cad7aab2509560bfeb6755d88
---

# Story 2.6: Memories linkage decision spike *(enabler / PR-4 / AR-G4)*

## Status

done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **Projects platform engineer**,
I want **a recorded decision on whether a Project Memory link maps to a Hexalith.Memories `Case` or to individual `MemoryUnit` references, together with the Memory reference identifier shape, the ACL contract surface, the eventual-consistency handling strategy, and the Epic 3 allowlist treatment of an absent/unavailable Memories reference**,
so that **the Story 2.7 Memory ACL, contracts, projections, and tests are built against a settled model and do not churn through the upstream `[Experimental("HXL001")]` write surface or the async/eventually-consistent ingestion path**.

This is a **decision spike / enabler story** (PR-4, AR-G4). It produces an Architecture Decision Record (ADR), the Memory reference shape, the `IProjectMemoryDirectory` ACL contract surface design, and the Memories eventual-consistency posture. It does **not** introduce `LinkMemory` / `UnlinkMemory` commands or events, `MemoryLinked` / `MemoryUnlinked` events, server endpoints, OpenAPI changes, regenerated client/idempotency helpers, projection changes, or any executable Projects domain/server code. Story 2.7 is where the link/unlink write-side lands; this story exists so 2.7 can be built once, not rewritten when the Case-vs-Unit question is answered late.

Story 2.4 established the Project Folder reference + ACL pattern, `ProjectReferenceIndexProjection`, capability-gate discipline, and safe rejection handling. Story 2.5 extended the same pattern for optional file references and proved metadata-only authorization via Folders. Story 2.6 must lock down the analogous pattern for the Memories reference type **before** code is written, because `Hexalith.Memories` is materially different from Folders along three axes:

1. Memories core write methods are annotated `[Experimental("HXL001")]` (`CreateTenantAsync`, `CreateCaseAsync`, `IngestAsync`, `GetTelemetrySummaryAsync`); `ListHandlersAsync` / `GetHandlerMismatchesAsync` are `HXL002`.
2. Ingestion is asynchronous (HTTP `202` + Dapr Workflow + `[Experimental]` triple-write to RediSearch + Redis Vector + FalkorDB) with **no client read-after-write**.
3. The Memories identity hierarchy is **Tenant → Case → MemoryUnit** — a Project Memory link does **not** have an obvious one-to-one mapping.

The OpenAPI spine and shared vocabulary already reserve `memory` as a `referenceKind` enum value, both events `MemoryLinked` and `MemoryUnlinked` are listed in AR-6, and `MemoryMatched` is a `[ProjectionBadge]` reason code. Story 2.6 must reach a decision that is consistent with those reserved surfaces and with PR-4 (Memories writes deferred behind this decision spike).

## Acceptance Criteria

1. A Projects ADR is created under `docs/adr/` (for example `docs/adr/memories-link-target.md`) recording, with rationale and trade-off analysis, **whether a single Project Memory link targets a Memories `Case`, an individual `MemoryUnit`, or both (hybrid)**, with the chosen option explicitly selected. The ADR cross-references `AR-G4` and `PR-4` and the canonical `_bmad-output/planning-artifacts/research/technical-hexalith-memories-rag-research-2026-05-24.md` evidence. ADR status is `Accepted`.

2. The ADR records the **eventual-consistency handling strategy** for the chosen target. It must state explicitly that Projects never assumes read-after-write against Memories, that the Projects ACL validates the chosen target via **stable** read routes (not via `IngestAsync` or any `[Experimental("HXL001")]` write method from inside Projects), and that all test convergence is asserted **deterministically** (controllable fakes, polled-state assertions, or explicit state-transition stubs) and **never via wall-clock sleeps**.

3. The ADR defines the **Memory reference identifier shape** that Story 2.7 will store on `ProjectState` and emit in `MemoryLinked` / `MemoryUnlinked`. It enumerates each field (kind, opacity, owning context, validation rule), confirms reuse of the owning context's representation (per `docs/adr/identifier-boundary.md` — plain ULID-shaped string for Memories ids), and explicitly forbids storing memory content, embeddings, vector dimensions, `Content`, `ContentHash`, `SourceUri`, raw `IngestionInput` payloads, raw upstream problem bodies, raw upstream `ErrorResponse` `Message`/`Suggestion` text, tokens, or paths.

4. The ADR defines the **`IProjectMemoryDirectory` ACL contract surface** Story 2.7 will implement under `src/Hexalith.Projects.Server/Memories/` (interface, validation-result type, denial taxonomy, freshness/state mapping). The surface is **read-only / metadata-only**: it validates an authorized Memory reference using `MemoriesClient` stable routes (`GetCaseAsync` since Story 10.2, or `GetMemoryUnitAsync` per the chosen target) and surfaces a Projects-shaped result. It must not call `CreateTenantAsync`, `CreateCaseAsync`, `IngestAsync`, `GetTelemetrySummaryAsync` (HXL001), `ListHandlersAsync` / `GetHandlerMismatchesAsync` (HXL002), or any content-bearing route, and must not forward `MemoryUnit.Content` / `ContentBytes` / `ContentHash` / `SourceUri` to Projects callers.

5. The ADR explicitly states **how Epic 3's allowlist treats an absent or unavailable Memories reference** at `ProjectContext` assembly: it is a **fail-closed-clean exclusion** carrying the shared vocabulary reason from `unauthorized` / `unavailable` / `stale` / `archived` / `tenant_mismatch` / `invalidReference` (per `_bmad-output/planning-artifacts/architecture.md#AR-18` and the OpenAPI `referenceKind`/`referenceState` enums), never an error / 5xx, and never an existence-revealing partial result. This realizes the Epic 2 narrative line "Epic 3's allowlist treats an absent/unavailable Memories reference as a fail-closed-clean state" (PR-4).

6. The ADR maps every Memories failure mode the ACL must handle to a Projects-safe outcome from the **shared vocabulary** (`Active` / `Archived` lifecycle and `included` / `excluded` / `unauthorized` / `unavailable` / `stale` / `archived` / `ambiguous` / `tenant_mismatch` / `conflict` / `invalidReference` reference states). The mapping covers at least: `MemoriesRemoteException` 401 / 403 / 404 / 409, `TENANT_NOT_FOUND`, `MEMORY_UNIT_NOT_FOUND`, `INVALID_MEMORY_UNIT_ID`, `INVALID_RESPONSE` (version skew), unavailable / 5xx / 408 / 503, `TenantStatus` not `Active`, `CaseStatus` not `Active` (per `Hexalith.Memories.Contracts.V1`), and the `MemoryUnitStatus` lifecycle including not-yet-indexed / not-yet-searchable states. The mapping does not echo `MemoryUnit.Content`, `ContentHash`, `SourceUri`, or raw upstream `ErrorResponse` `Message` / `Suggestion` text into Projects-owned outputs.

7. The ADR records the **`[Experimental]` surface containment strategy** for Story 2.7: which Memories methods will be invoked from the Projects ACL, where any unavoidable `[Experimental("HXL001")]` / `[Experimental("HXL002")]` pragma suppressions are allowed and where they are forbidden, and the façade rule that contains Memories-specific signature churn to a single Projects file. The strategy stays consistent with `Hexalith.Memories/docs/dev/experimental-apis.md` and the Memories research recommendations.

8. The ADR is referenced from at least two locations so it cannot drift unnoticed: an addition to `_bmad-output/planning-artifacts/architecture.md` (for example, an `AR-G4` resolution note that points to the ADR), and an entry in any local ADR index file if one exists (otherwise the ADR is discoverable from `docs/adr/`). This story does **not** modify `_bmad-output/planning-artifacts/epics.md` Story 2.6 / 2.7 acceptance criteria — those remain authoritative.

9. No production source under `src/Hexalith.Projects.*` is modified. No new commands, events, projections, validators, endpoints, OpenAPI spine entries, generated client / idempotency helper regenerations, FrontComposer descriptors, audit timeline rows, `ProjectState` fields, or `ProjectReferenceIndexProjection` entries are introduced by this story. The story does not regenerate `.g.cs` artifacts and does not hand-edit any `.g.cs` artifact. The `referenceKind` enum already includes `memory`; that is sufficient for the ADR to refer to and must not be re-emitted.

10. No nested submodule recursive update is performed, the Hexalith.Memories submodule pointer at the root is **not** advanced as part of this story, and no Memories submodule source is modified. The ADR text is allowed to cite Hexalith.Memories repo paths as evidence only; capability gating in Story 2.7 will re-verify those paths in its own working tree before coding (mirroring the Story 2.4 / 2.5 capability-gate discipline).

11. The story file `_bmad-output/implementation-artifacts/2-6-memories-linkage-decision-spike.md` is updated by the dev agent with a Dev Agent Record summarising: which decision was selected (Case / MemoryUnit / hybrid), the ADR path, the chosen Memories client read route(s), the deferred / out-of-scope items left to Story 2.7, and any HALT conditions encountered.

## Tasks / Subtasks

- [x] **Task 1 — Re-read inputs and freeze decision scope (AC: 1, 7, 9, 10)**
  - [x] Re-read `_bmad-output/planning-artifacts/research/technical-hexalith-memories-rag-research-2026-05-24.md` Sections "Identity & Resource Model", "Asynchronous Integration", "Error-Handling Pattern", "Risk Assessment & Mitigation" end-to-end. Do not rely on this story's Dev Notes summary.
  - [x] Re-read `_bmad-output/planning-artifacts/architecture.md` blocks AR-7, AR-9, AR-11, AR-14, AR-18, AR-G4, and PR-4.
  - [x] Re-read `_bmad-output/planning-artifacts/epics.md` Story 2.6 (this spike) and Story 2.7 (the consumer of this decision) verbatim so the decision is reached against the actual downstream contract Story 2.7 must satisfy.
  - [x] Re-read `docs/adr/identifier-boundary.md` so the chosen Memory reference identifier shape is consistent with the rule "sibling references are plain strings, not Projects-owned VOs".
  - [x] Re-confirm the current state of the existing Projects surfaces that already reserve `memory` for this work: the OpenAPI `referenceKind` enum at `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` and the shared vocabulary enums under `src/Hexalith.Projects.Contracts/Ui/`. Do not modify them in this story.
  - [x] Confirm there is no existing `IProjectMemoryDirectory` in `src/Hexalith.Projects.Server/`; this is design-only here.

- [x] **Task 2 — Verify Memories capability surface and stability gates in this working tree (AC: 2, 4, 7)**
  - [x] Read `Hexalith.Memories/src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` and record which methods are `[Experimental("HXL001")]` / `[Experimental("HXL002")]` and which are stable. Record at minimum the status of `CreateTenantAsync`, `CreateCaseAsync`, `IngestAsync`, `GetTelemetrySummaryAsync`, `GetCaseAsync`, `GetMemoryUnitAsync`, `TraverseAsync`, `HybridSearchAsync`, `SearchAsync`, `GetTenantAsync`, `ListHandlersAsync`, `GetHandlerMismatchesAsync`.
  - [x] Read `Hexalith.Memories/docs/dev/experimental-apis.md` and `Hexalith.Memories/docs/dev/consistency.md` and capture the eventual-consistency contract verbatim (no read-after-write; verify-then-repair; convergence cap).
  - [x] Inspect `Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/{Case.cs, CaseStatus.cs, MemoryUnit.cs, MemoryUnitStatus.cs, SourceType.cs, ErrorResponse.cs, ConsistencyNoteKind.cs}` and capture (a) the exact `Case` and `MemoryUnit` identity field names and types and (b) the error codes Projects must map. Do not import these types into Projects; the ACL boundary owns translation.
  - [x] Confirm `Hexalith.Memories/src/Hexalith.Memories.Client.Rest/MemoriesClient.cs::GetCaseAsync` is documented "Stable since Story 10.2" and that no equivalent stable replacement exists for `CreateCaseAsync` / `IngestAsync` (so Projects must never call those from inside this module).
  - [x] Record the read-route safe-denial behavior: which 4xx codes the Memories REST surface returns vs which become `MemoriesRemoteException` with `ErrorResponse.Code`, and which getters soft-miss to `null` (the research lists `GetTenantAsync` and consistency-status getters).
  - [x] **HALT** if the working-tree evidence does not match the assumptions in this story or in the cited research (for example, if `GetCaseAsync` is no longer stable, or if a new content-bearing route now sits at the same path the ACL would use). Surface a `HALT` block in the Dev Agent Record and stop before authoring the ADR.

- [x] **Task 3 — Decide Case vs MemoryUnit vs hybrid, then author the ADR (AC: 1, 3, 4, 5, 6, 7)**
  - [x] Enumerate the three candidate options against criteria: bounded reference cardinality fit with `ProjectState` (per AR-3 and Story 2.5 precedent for bounded sets), authorization stability against the current Memories surface, freshness/eventual-consistency exposure, allowlist semantics, leakage risk, ID shape simplicity, and Story 2.7 implementation cost.
  - [x] **Recommended decision (validate against the criteria, do not adopt blindly):** target the Memories **`Case`** as the single Project Memory link unit, because (a) the research explicitly recommends "one Case per project / conversation", (b) `GetCaseAsync` is the only **stable** read route covering the chosen surface, (c) Case granularity keeps `ProjectState` reference cardinality low and disjoint from per-kind `ProjectReferenceIndexProjection` keys, (d) it side-steps the `[Experimental("HXL001")]` write surface, and (e) it produces a single `memoryReferenceId` (= `caseId`) shape that fits `docs/adr/identifier-boundary.md`. If the dev agent's analysis rejects this option, the ADR must record the rejected option in the "Considered" section with explicit evidence, not silently omit it.
  - [x] Author `docs/adr/memories-link-target.md` with the standard ADR structure used by `docs/adr/identifier-boundary.md` (front-matter or header listing Status, Date, Story 2.6, Requirement AR-G4 / PR-4). Sections required: Context, Decision, Identifier shape, ACL contract surface, Eventual-consistency strategy (deterministic, no sleeps), Failure-to-shared-vocabulary mapping, Epic 3 allowlist treatment, `[Experimental]` surface containment, Consequences, Out of scope, References.
  - [x] In the **Decision** section, name the chosen target (Case / MemoryUnit / hybrid) once, in bold, and reference it consistently below — no contradiction across sections.
  - [x] In the **Identifier shape** section, list each stored field exactly as Story 2.7 will store it on `ProjectState` and emit on `MemoryLinked` / `MemoryUnlinked` (for example, for the Case option: `memoryReferenceId` = `caseId` (ULID-shaped string), `tenantId` (implicit, equals envelope tenant, never re-emitted as payload), optional safe `displayName`, shared `ReferenceState`, optional reason code, event `OccurredAt`). Explicitly enumerate forbidden fields (see AC 3) so Story 2.7 cannot accidentally regress.
  - [x] In the **ACL contract surface** section, describe `IProjectMemoryDirectory` shape (validation method signature, validation-result discriminated union, denial taxonomy mirroring `ProjectFileReferenceValidationOutcome` and `ProjectFolderValidationResult`), the typed-client lifetime rule (transient / request-scoped per Story 2.4 / 2.5 review), and the fail-closed `UnavailableProjectMemoryDirectory` fallback for hosts without a Memories client.
  - [x] In the **Eventual-consistency strategy** section, record the rule that the ACL validates **only** via a stable read route per the chosen target (e.g. `GetCaseAsync`); the link-as-pending behavior (if any) must be a Projects-owned reference state from the shared vocabulary (`unavailable` / `stale`), not a new enum; Story 2.7 tests must drive convergence with deterministic fakes / explicit transitions, never `Thread.Sleep`, `Task.Delay`, or wall-clock retries.
  - [x] In the **Failure-to-shared-vocabulary mapping** section, table every error mode listed in AC 6 to the shared vocabulary value.
  - [x] In the **Epic 3 allowlist treatment** section, state explicitly that an absent / unavailable Memories reference is a fail-closed-clean exclusion (PR-4) carrying a reason code from the shared vocabulary; the assembled `ProjectContext` never errors because a Memories reference is unavailable; existence is never disclosed across tenants.
  - [x] In the **`[Experimental]` surface containment** section, list which Memories client methods Story 2.7's ACL will call (stable reads only) and which it must **never** call (`CreateTenantAsync`, `CreateCaseAsync`, `IngestAsync`, `GetTelemetrySummaryAsync`, `ListHandlersAsync`, `GetHandlerMismatchesAsync`). State the façade rule that any unavoidable HXL pragma suppression in Story 2.7 lives in one Memories ACL file under `src/Hexalith.Projects.Server/Memories/` and nowhere else; aggregate, projection, contracts, and OpenAPI must remain free of `HXL001` / `HXL002` suppressions.
  - [x] In the **Consequences** section, record what is locked in (Story 2.7's command / event / projection / endpoint / ACL shape, OpenAPI link/unlink route shape, idempotency fingerprint field set) and what is intentionally deferred (per-MemoryUnit pins; semantic / hybrid search wiring; ingestion orchestration; bulk re-link).
  - [x] In the **Out of scope** section, mirror the Story 2.5 precedent: no file content / memory body / search payload / vector / embedding storage; no Memories writes from Projects; no nested submodule update; no Projects code change.
  - [x] In the **References** section, link to (a) `_bmad-output/planning-artifacts/architecture.md` AR-G4 / PR-4 / AR-14 / AR-18, (b) `_bmad-output/planning-artifacts/epics.md` Story 2.6 / 2.7, (c) `_bmad-output/planning-artifacts/research/technical-hexalith-memories-rag-research-2026-05-24.md`, (d) `Hexalith.Memories/docs/dev/experimental-apis.md`, (e) `Hexalith.Memories/docs/dev/consistency.md`, (f) `docs/adr/identifier-boundary.md`, (g) `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` (existing `referenceKind: memory` enum value), (h) `docs/event-catalog.md`.

- [x] **Task 4 — Wire the ADR into the architecture as an AR-G4 resolution note (AC: 8)**
  - [x] Update `_bmad-output/planning-artifacts/architecture.md` near the AR-G4 line (the existing "Cross-module upstream dependency gaps" block lists AR-G4) to add a one-sentence "Resolved by Story 2.6 — see `docs/adr/memories-link-target.md`" pointer. Do **not** edit Story 2.6 / 2.7 acceptance criteria in `_bmad-output/planning-artifacts/epics.md`.
  - [x] If a local ADR index exists (for example, a `docs/adr/README.md` or similar), add a one-line entry for the new ADR; otherwise discoverability via `docs/adr/` listing is sufficient (see how `identifier-boundary.md` sits as the sole prior entry).

- [x] **Task 5 — Privacy / leakage self-check of the ADR text (AC: 3, 6, 9)**
  - [x] Re-read the authored ADR end-to-end and confirm the **document text itself** contains no transcript snippets, file contents, prompts, secrets, raw tokens, raw upstream `ErrorResponse` `Message` / `Suggestion` body text, real tenant identifiers, real `MemoryUnit.Content` / `ContentHash` / `SourceUri` values, or real Project / Folder / File / Memory identifiers from any working environment. Any illustrative identifier in the ADR is fabricated and obviously placeholder-shaped.
  - [x] Confirm the ADR does not introduce parallel enums or magic strings outside the shared vocabulary (AR-18) — every reference-state / reason-code term it uses appears already in `src/Hexalith.Projects.Contracts/Ui/`.

- [x] **Task 6 — Validation and Dev Agent Record finalization (AC: 11)**
  - [x] Run `git -C /mnt/d/Hexalith.Projects status` and confirm the only changes are the ADR addition under `docs/adr/`, the one-line architecture pointer, and this story file. No `src/Hexalith.Projects.*` source change, no `.g.cs` change, no submodule pointer change.
  - [x] Run `git diff --check` on the touched files; resolve any whitespace errors. Hand-written `.md` follows the existing CRLF / UTF-8 / final newline norm of neighboring files under `docs/adr/` and `_bmad-output/planning-artifacts/`.
  - [x] No `dotnet build` / `dotnet test` is required for this story (no compiled code changes). Record this explicitly in the Dev Agent Record so reviewers do not expect a green-lane line.
  - [x] Populate the Dev Agent Record below with: decision taken, ADR path, chosen Memories client read route(s), explicit `HALT` items if any, and the items intentionally deferred to Story 2.7.
  - [x] Append a `Change Log` row dated today recording the decision and ADR creation.

## Dev Notes

### Story Scope Boundary

- **In scope:** the ADR `docs/adr/memories-link-target.md`, the one-line AR-G4 resolution pointer in `_bmad-output/planning-artifacts/architecture.md`, and the Dev Agent Record / Change Log on this story file. That is the entire deliverable.
- **Out of scope (deferred to Story 2.7 unless otherwise noted):** `LinkMemory` / `UnlinkMemory` commands and events; `MemoryLinked` / `MemoryUnlinked` success events and rejection mappings; new `MemoryReference` model / `ProjectState.MemoryReferences` collection; `ProjectReferenceIndexProjection` memory-kind rows; `ProjectDetailProjection` / `ProjectDetailItem` memory summaries; new authorization action tokens (`projects:link_memory` / `projects:unlink_memory`); `IProjectMemoryDirectory` implementation; `FoldersProjectMemoryDirectory`-equivalent class; endpoint handlers; OpenAPI route entries; regenerated NSwag client and idempotency helpers; `docs/event-catalog.md` entries for `MemoryLinked` / `MemoryUnlinked`; `NoPayloadLeakage` fixtures extended over Memory events; idempotency fingerprint field lists.
- **Explicitly out of scope at any time (PR-4):** Projects ever calling Memories' `[Experimental("HXL001")]` write methods (`CreateTenantAsync`, `CreateCaseAsync`, `IngestAsync`, `GetTelemetrySummaryAsync`); Projects ever storing `MemoryUnit.Content`, `ContentHash`, `SourceUri`, embeddings, vector dimensions, fusion weights, or raw `IngestionInput` payloads; Projects ever depending on Memories read-after-write semantics; Projects ever performing nested recursive submodule init; Projects advancing the `Hexalith.Memories` submodule pointer as part of this story.

### Current Code Facts Verified

- `Hexalith.Memories/src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` exposes the following methods relevant to this decision: `HybridSearchAsync` (line 109), `GetMemoryUnitAsync` (line 211), `CreateCaseAsync` (line 344, `[Experimental("HXL001")]`), `IngestAsync` (line 405, `[Experimental("HXL001")]`), `TraverseAsync` (line 734), `GetCaseAsync` (line 810, documented "Stable since Story 10.2"). `CreateTenantAsync` and `GetTelemetrySummaryAsync` are also `[Experimental("HXL001")]`; `ListHandlersAsync` / `GetHandlerMismatchesAsync` are `[Experimental("HXL002")]`. _Confirm against this working tree before authoring the ADR._
- `Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/MemoryUnit.cs` declares `MemoryUnit` as a `sealed record` with required string fields `Id`, `TenantId`, `CaseId`, `Content`, `ContentHash`, `SourceUri`, `IngestedBy`, plus `SourceType`, `MemoryUnitStatus`, `Metadata` (`Dictionary<string, MetadataField>` pinned to `StringComparer.Ordinal`), and embedding provenance. `Content`, `ContentHash`, and `SourceUri` are explicit do-not-store-in-Projects targets for AC 3 / AC 6.
- `Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/Case.cs` declares `Case` as a `sealed record` with `Id`, `TenantId`, `Name`, optional `Description`, `Status` (`CaseStatus`), `CreatedAt`, `LastUpdated`, `MemoryUnitCount`. For the Case-link option, `Case.Id` is the `memoryReferenceId`; `Case.TenantId` must equal the Projects envelope tenant; `Case.Status` must be `Active` (whichever value the Memories enum names) for `included`; otherwise it maps to `archived` / `unavailable` / `excluded` per the shared vocabulary.
- The Projects OpenAPI spine already reserves `memory` in two enums (verified):
  - `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` line ~1866 (request reference-kind enum) and line ~2014 (response `ProjectReferenceSummary.referenceKind` enum).
  - Story 2.5 deliberately added `file` to the response enum and left `memory` intact; do not re-emit either enum.
- Shared vocabulary lives under `src/Hexalith.Projects.Contracts/Ui/` (e.g. `ReferenceState.cs`). The ADR's failure-to-state mapping uses exactly these values: `included` / `excluded` / `unauthorized` / `unavailable` / `stale` / `archived` / `ambiguous` / `tenant_mismatch` / `conflict` / `invalidReference` (and the lifecycle `Active` / `Archived`). No new state enum may be invented.
- `src/Hexalith.Projects.Server/Folders/` already implements the analogous pattern this story locks in for memories: `IProjectFolderDirectory` + `FoldersProjectFolderDirectory` + `ProjectFolderValidationResult` + `UnavailableProjectFolderDirectory`, and Story 2.5 added the file-reference twin `IProjectFileReferenceDirectory` + `FoldersProjectFileReferenceDirectory` + `ProjectFileReferenceValidationResult` + `UnavailableProjectFileReferenceDirectory`. Story 2.7 will mirror this same shape under `src/Hexalith.Projects.Server/Memories/`; the ADR describes that shape but **does not create the files**.
- `docs/event-catalog.md` does not currently document `MemoryLinked` / `MemoryUnlinked`; Story 2.7 adds those entries. The ADR may reference the catalog as the place Story 2.7 will update.
- `docs/adr/` currently contains exactly one ADR (`identifier-boundary.md`). The new ADR follows its header style and adopts the same brevity discipline.

### Required Decision (with the Recommended Option)

The ADR must explicitly choose one of:

- **Option A — Case-level link (recommended).** A Project Memory link is exactly one Memories `Case`. The stored `memoryReferenceId` equals `Case.Id`. Validation route: `MemoriesClient.GetCaseAsync(tenantId, caseId)` (stable since Memories Story 10.2). One Case per project / conversation matches the research's recommended consumer model. Bounded cardinality fits `ProjectState` (`MaxMemoryReferences` configured in Story 2.7, mirroring `MaxFileReferences` from Story 2.5). Side-steps the `[Experimental("HXL001")]` write surface entirely. Aligns with the AR-G4 framing ("targets a Case or MemoryUnit(s)") by selecting Case.
- **Option B — MemoryUnit-level link.** A Project Memory link is one or more `MemoryUnit`s. Stored `memoryReferenceId` equals `MemoryUnit.Id` plus implicit `Case.Id` parent. Validation route: `MemoriesClient.GetMemoryUnitAsync(tenantId, caseId, memoryUnitId)`. Finer granularity, but materially higher reference cardinality (Memories' `Case.MemoryUnitCount` is unbounded in practice), higher per-link projection cost, and a richer Projects-side identifier shape (must carry both `caseId` and `memoryUnitId`). Eventual-consistency exposure is materially worse: a newly ingested unit may not be retrievable for some time after the `IngestAsync` 202.
- **Option C — Hybrid.** Primary link is a Case; optional "pinned" individual `MemoryUnit` ids are allowed under the same Project Memory reference. Highest cost; defers naturally to a follow-up story if Option A proves insufficient in practice.

If the dev agent chooses Option A (recommended), the ADR's Decision section names "Case" once, in bold, and the rest of the ADR is consistent with that. If the agent chooses B or C, the ADR must explain why the recommended option's evidence was rejected (do not silently flip).

### Guardrails

- **Pure ADR-only deliverable.** No edits to `src/Hexalith.Projects.*`, no `.g.cs` regeneration, no OpenAPI changes, no shared-vocabulary enum churn. If the chosen design requires a new enum value, that work belongs to Story 2.7 and is recorded as a Consequence in the ADR — not implemented here.
- **Sibling identifier reuse.** Per `docs/adr/identifier-boundary.md`, Memories ids stay as plain ULID-shaped `string`. Do not introduce a `MemoryId` / `CaseId` value object in Projects.
- **Metadata-only.** The ADR's identifier shape forbids `MemoryUnit.Content`, `ContentBytes`, `ContentHash`, `SourceUri`, raw `IngestionInput` payloads, embedding vectors, fusion weights, search snippets, raw `ErrorResponse.Message` / `Suggestion` text, tokens, and paths. The ADR text itself does not include any such payload either.
- **Deterministic eventual-consistency assertions.** Forbid `Thread.Sleep` / `Task.Delay` / wall-clock retries in Story 2.7 tests. The ADR records this rule explicitly so reviewers can enforce it later.
- **`[Experimental]` containment.** Pragma suppressions for `HXL001` / `HXL002` are only allowed (if Story 2.7 ever needs them) inside a single Memories ACL file under `src/Hexalith.Projects.Server/Memories/`. The ADR makes this rule binding.
- **No nested submodule init.** Do not run `git submodule update --init --recursive`. Read-only inspection of the already-initialized `Hexalith.Memories` submodule is allowed; advancing its pointer is not.
- **No upstream Memories source change.** This story does not edit `Hexalith.Memories/**` and does not commit inside that submodule. The ADR may cite Memories repo paths as evidence only.
- **Single source of truth for the decision.** The ADR is the canonical record. Do not duplicate the decision into `_bmad-output/planning-artifacts/architecture.md` beyond the one-line AR-G4 pointer, and do not duplicate it into `epics.md`.

### ACL Contract Surface Sketch (to be detailed in the ADR, not implemented here)

For the Case option (recommended), the ADR fixes a surface Story 2.7 will implement under `src/Hexalith.Projects.Server/Memories/`:

```text
src/Hexalith.Projects.Server/Memories/
├─ IProjectMemoryDirectory.cs              # interface; Story 2.7
├─ MemoriesProjectMemoryDirectory.cs       # MemoriesClient-backed impl; Story 2.7
├─ ProjectMemoryValidationResult.cs        # discriminated outcome; Story 2.7
└─ UnavailableProjectMemoryDirectory.cs    # fail-closed fallback when no client registered; Story 2.7
```

Indicative interface shape (illustrative; the ADR records the canonical signature):

```csharp
public interface IProjectMemoryDirectory
{
    Task<ProjectMemoryValidationResult> ValidateMemoryReferenceAsync(
        string tenantId,
        string projectId,
        string memoryReferenceId,   // = Memories Case.Id for Option A
        CancellationToken cancellationToken);
}
```

Validation-result outcomes Story 2.7 must implement (mirroring `ProjectFileReferenceValidationOutcome` / `ProjectFolderValidationResult`): `Accepted` (carries safe `MemoryReferenceMetadata` — `displayName` from `Case.Name`, `lifecycle` from `Case.Status`, `observedAt`), `Denied`, `NotFound`, `TenantMismatch` (taxonomy symmetry; may be unproduced in practice for safe-denial reasons, see the Story 2.5 review LOW note), `Archived`, `Stale`, `Unavailable`, `ValidationFailed`. The denial taxonomy maps to the shared vocabulary per AC 6.

### Eventual-Consistency Strategy Sketch (to be detailed in the ADR, not implemented here)

For Story 2.7's link path under Option A:

1. The Projects link command/endpoint validates Project mutation intent first (tenant + project auth gate), exactly as Story 2.5 does for file references, before any Memories call.
2. The ACL calls `MemoriesClient.GetCaseAsync(tenantId, caseId, ct)` and maps the typed result to a `ProjectMemoryValidationResult`. No `IngestAsync` / `CreateCaseAsync` / `HybridSearchAsync` is invoked.
3. On `MemoriesRemoteException` 404 → `Denied` (safe-denial; Memories collapses tenant-mismatch and unauthorized into 4xx by design, mirroring Folders 401/403/404 → `Denied`).
4. On `MemoriesRemoteException` 401/403 → `Denied`. On 408/503/5xx → `Unavailable`. On 400/422/`INVALID_RESPONSE` → `ValidationFailed` (treated as `invalidReference`).
5. On `Case.Status` not `Active` → `Archived` / `excluded` per the mapping table.
6. The Memories client lifetime is **transient / request-scoped** (mirrors Story 2.4 / 2.5 review fix `TryAddTransient` for typed-client + bearer-handler chains).
7. Convergence in tests is asserted by injecting a deterministic fake `IProjectMemoryDirectory` (or stubbed `HttpMessageHandler` behind `MemoriesClient` per the Memories research's recommended test pattern), not by waiting.

### Epic 3 Allowlist Treatment Sketch (to be detailed in the ADR, not implemented here)

Per PR-4 and AR-9, when Epic 3's `ProjectContext` assembly evaluates a Memory reference whose ACL outcome is **not** `Accepted`, the reference is excluded from `ProjectContext` and carries the corresponding `referenceState` from the shared vocabulary. The assembled `ProjectContext` response is still 2xx / `success`; the absence does not propagate as a Projects error. Cross-tenant existence is never disclosed. This is identical to how Story 2.4 / 2.5 already treat unavailable Folder / File references; the ADR notes the symmetry rather than inventing a new rule.

### Previous Story Intelligence

- **Story 2.5 (File Reference link/unlink):** established the bounded reference set + per-kind index keys + Folders ACL capability gate + deterministic typed-client tests + `TryAddTransient` client registration + `Unavailable*Directory` fail-closed fallback. Story 2.6's ADR adopts every applicable pattern by reference; do not duplicate the rules into the ADR text — cite the Story 2.5 file.
- **Story 2.5 review LOW observations carry forward:** taxonomy outcomes that are not produced by the upstream surface (because that surface intentionally collapses cases into safe-denial 4xx) are still part of the documented denial taxonomy. The ADR records this stance for Memories.
- **Story 2.4 (Project Folder):** established the capability-gate discipline ("HALT before coding if no trustworthy authorization path exists"). For this spike, Task 2 enforces the equivalent: HALT before authoring the ADR if the Memories surface diverges from cited evidence.
- **Story 2.3 (Conversation write-side):** established the principle that authorization to the sibling resource must be evaluated against authoritative upstream evidence, not against caller-supplied IDs. The ADR records that Projects never accepts a caller-supplied Memory link without ACL validation through `MemoriesClient.GetCaseAsync` (or `GetMemoryUnitAsync` for Option B).
- **Story 2.2 (Conversation upstream capability):** established the pattern that an upstream / enabler story produces a separately-reviewable artifact and that downstream link/move/unlink stories pick it up. Story 2.6's pattern is analogous, but its artifact is an ADR in Projects (not an upstream code change in Hexalith.Memories), because the Memories surface already exposes everything needed; the missing piece is a recorded decision.
- **Recent commit hygiene:** the last six commits (`e127b7a` 2.5, `21da98f` BMAD 6.8.0, `1be3640` BMAD 6.7.1, `7e8494f` subproject pointers, `efeb7a7` orchestration artifacts, `436fc23` 2.4) confirm story-scoped commits and no nested recursive submodule init. Follow the same discipline; if the dev agent commits at all, the diff must be a single ADR + one-line architecture pointer + this story file.

### Files To Read Before Editing

- `_bmad-output/planning-artifacts/epics.md` — Story 2.6 acceptance criteria (lines around 653–667), Story 2.7 dependency on this decision (lines around 669–688), PR-4 framing (line ~293).
- `_bmad-output/planning-artifacts/architecture.md` — AR-G4 (line ~139), AR-14 Memories model (line ~117), AR-9 reference index, AR-11 ACLs (line ~114), AR-18 shared vocabulary (line ~124), Implementation Sequence step 6 (line ~315).
- `_bmad-output/planning-artifacts/research/technical-hexalith-memories-rag-research-2026-05-24.md` — Identity & Resource Model, Integration Patterns, Asynchronous Integration (202 + polling), Error-Handling Pattern, Risk Assessment & Mitigation.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md` — FR-10 Link Memory and FR-11 Unlink Context Reference consequences.
- `_bmad-output/implementation-artifacts/2-5-link-unlink-file-reference.md` — pattern Story 2.7 mirrors; ACL fallback / capability-gate pattern; reference-index key discipline.
- `_bmad-output/implementation-artifacts/2-4-set-auto-create-project-folder.md` — capability-gate discipline; HALT precedent.
- `docs/adr/identifier-boundary.md` — ADR style; sibling identifier reuse rule.
- `docs/event-catalog.md` — confirms `MemoryLinked` / `MemoryUnlinked` are not yet documented (Story 2.7 will add them).
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` — confirm `referenceKind: memory` already reserved at both the request and response enum sites (lines ~1866 and ~2014); do not edit.
- `src/Hexalith.Projects.Contracts/Ui/` — shared vocabulary lives here; do not edit.
- `src/Hexalith.Projects.Server/Folders/` — the entire pattern Story 2.7 will mirror for Memories; the ADR's ACL surface sketch cites this folder.
- `Hexalith.Memories/src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` — confirm experimental annotations and stable routes named in Task 2.
- `Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/{Case.cs, CaseStatus.cs, MemoryUnit.cs, MemoryUnitStatus.cs, ErrorResponse.cs}` — identity field names and error codes for AC 3 / AC 6.
- `Hexalith.Memories/docs/dev/experimental-apis.md` and `Hexalith.Memories/docs/dev/consistency.md` — verbatim source for the eventual-consistency contract and the HXL001 / HXL002 surface.

### Validation / Testing Requirements

- **No compiled tests for this story.** Spike deliverable is documentation. The Dev Agent Record must state this explicitly so reviewers do not expect a `dotnet test` summary line.
- The ADR is reviewed against AC 1–10 and the Story 2.6 checklist line items at handoff to review.
- Story 2.7's `dotnet test` lanes (Contracts / Client / Projects / Server / Integration) and OpenAPI / fingerprint gates will validate the *consequences* of this ADR — not this story.

### Out Of Scope

- Writing any `LinkMemory` / `UnlinkMemory` command / event / handler / projection / endpoint / OpenAPI / generated client / idempotency helper. All of that belongs to Story 2.7.
- Calling Memories' `[Experimental("HXL001")]` writes (`CreateTenantAsync`, `CreateCaseAsync`, `IngestAsync`, `GetTelemetrySummaryAsync`) from Projects ever, even in tests; Story 2.7 tests use fakes / stubs at the `MemoriesClient` boundary.
- Storing `MemoryUnit.Content`, `ContentBytes`, `ContentHash`, `SourceUri`, embeddings, vector dimensions, fusion weights, search snippets, or raw `IngestionInput` / `ErrorResponse` body text in any Projects event / state / projection / log / audit row.
- Implementing semantic / hybrid search, traversal, or any RAG behavior inside Projects.
- Advancing the `Hexalith.Memories` submodule pointer or editing any file under `Hexalith.Memories/**`.
- Performing nested recursive submodule initialization / update.
- Reopening Story 2.5 (File Reference) or Story 2.4 (Project Folder) review LOW observations; those are deferred separately per Story 2.5 review notes.
- Modifying `_bmad-output/planning-artifacts/epics.md` Story 2.6 / 2.7 acceptance criteria.

### Developer HALT Conditions

- **HALT before authoring the ADR** if the working-tree Memories surface diverges from Task 2 evidence — for example, if `GetCaseAsync` is no longer documented "Stable since Story 10.2", if a stable replacement appears for `CreateCaseAsync` (which would change the experimental-containment story), or if a content-bearing route now sits at the same path the ACL would use. Surface a HALT block in the Dev Agent Record citing the divergence and stop.
- **HALT** if reaching the recommended decision (Option A — Case) would require Projects to call any `[Experimental("HXL001")]` write method from runtime code paths; document the conflict and reconsider option choice.
- **HALT** if the decision would require Projects to store `MemoryUnit.Content`, `ContentBytes`, `ContentHash`, `SourceUri`, or any payload material in `ProjectState` / events / projections / audit / logs.
- **HALT** if the decision would require introducing a new reference / inclusion state outside the AR-18 shared vocabulary (e.g. a Memory-specific `pendingIngestion` enum value). Use existing values (`unavailable` / `stale`) instead.
- **HALT** if the decision would require Projects to advance the `Hexalith.Memories` submodule pointer or change any file under `Hexalith.Memories/**` to be implementable.
- **HALT** if reaching the decision would require regenerating `.g.cs` artifacts or hand-editing any `.g.cs` artifact in this story.
- **HALT** if reaching the decision would require nested recursive submodule initialization.
- **HALT** if reaching the decision would require breaking public Projects contracts, introducing a `V2` command / event / schema, or modifying the shared-vocabulary enums in `src/Hexalith.Projects.Contracts/Ui/`.

## References

- `_bmad-output/planning-artifacts/epics.md` — Story 2.6 (decision spike) and Story 2.7 (Link/Unlink Memory) acceptance criteria, plus PR-4 framing (~line 293).
- `_bmad-output/planning-artifacts/architecture.md` — AR-G4 Memories Case-vs-Unit (line ~139), AR-14 Memories model + experimental writes (line ~117), AR-9 reference index, AR-11 ACLs (line ~114), AR-18 shared vocabulary (line ~124), Implementation Sequence step 6 (~line 315).
- `_bmad-output/planning-artifacts/research/technical-hexalith-memories-rag-research-2026-05-24.md` — Memories identity model, REST surface, `[Experimental]` write annotations, async ingest 202+poll pattern, fail-closed error handling, RAG retrieval tuning surface; canonical evidence base for the decision.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md` — FR-10 (Link Memory) and FR-11 (Unlink Context Reference) consequences.
- `_bmad-output/implementation-artifacts/2-5-link-unlink-file-reference.md` — pattern Story 2.7 mirrors; ACL surface / capability gate / typed-client lifetime / shared-vocabulary mapping precedent.
- `_bmad-output/implementation-artifacts/2-4-set-auto-create-project-folder.md` — capability-gate discipline and HALT precedent.
- `_bmad-output/implementation-artifacts/2-3-link-move-conversation-write-side.md` — write-side mutation discipline (route/body identity, idempotency, safe ProblemDetails) Story 2.7 will reuse.
- `_bmad-output/implementation-artifacts/2-2-conversation-project-reassignment-upstream-capability.md` — upstream / enabler story pattern; ADR-as-deliverable precedent.
- `docs/adr/identifier-boundary.md` — sole prior Projects ADR; new ADR follows its style; identifier-reuse rule (Memories ids are plain ULID-shaped strings).
- `docs/event-catalog.md` — authoritative event catalog; Story 2.7 (not 2.6) adds `MemoryLinked` / `MemoryUnlinked` entries.
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` — already reserves `referenceKind: memory` (request and response enums at ~lines 1866 and 2014); do not edit in this story.
- `src/Hexalith.Projects.Contracts/Ui/` — shared vocabulary single source of truth.
- `src/Hexalith.Projects.Server/Folders/` — the analogue pattern Story 2.7 will mirror under `src/Hexalith.Projects.Server/Memories/`.
- `Hexalith.Memories/src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` — `GetCaseAsync` (line ~810, stable since Memories Story 10.2), `GetMemoryUnitAsync` (line ~211), `CreateCaseAsync` (line ~344, HXL001), `IngestAsync` (line ~405, HXL001), `HybridSearchAsync` / `TraverseAsync` (lines ~109, ~734).
- `Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/{Case.cs, MemoryUnit.cs, ErrorResponse.cs}` — Memories contract types and error codes the ACL must translate.
- `Hexalith.Memories/docs/dev/experimental-apis.md` — authoritative HXL001 / HXL002 surface and pragma rules.
- `Hexalith.Memories/docs/dev/consistency.md` — authoritative eventual-consistency contract (no read-after-write; triple-write divergence; verify-then-repair; convergence cap).

## Dev Agent Record

### Agent Model Used

Claude Opus 4.7 (create-story, 2026-05-28)

### Debug Log References

- 2026-05-28: Resolved `bmad-create-story` workflow customization; loaded project config, sprint status (`2-6-memories-linkage-decision-spike` confirmed `backlog`), Epic 2 Story 2.6 / 2.7 / PR-4 framing in `epics.md`, AR-G4 / AR-14 / AR-18 / Implementation Sequence in `architecture.md`, FR-10 / FR-11 in `prd.md`, Memories RAG research, prior Stories 2.2 / 2.3 / 2.4 / 2.5 implementation artifacts, root and Memories submodule state (Memories pinned at `608d15d`), Memories client method annotations (`[Experimental("HXL001")]` on `CreateTenantAsync` / `CreateCaseAsync` / `IngestAsync` / `GetTelemetrySummaryAsync`; `[Experimental("HXL002")]` on `ListHandlersAsync` / `GetHandlerMismatchesAsync`; `GetCaseAsync` documented "Stable since Story 10.2"), Memories `Case` / `MemoryUnit` field shapes, existing Projects OpenAPI `referenceKind` enums (`memory` already reserved at both sites), shared-vocabulary location under `src/Hexalith.Projects.Contracts/Ui/`, prior ADR style (`docs/adr/identifier-boundary.md`).
- Create-story workflow only; no implementation commands were run for this story.
- 2026-05-28 (dev-story): re-loaded Task 1 inputs (`architecture.md` blocks `AR-7`/`AR-9`/`AR-11`/`AR-14`/`AR-18`/`AR-G4`/`PR-4`, `epics.md` Story 2.6/2.7, `docs/adr/identifier-boundary.md`, shared vocabulary at `src/Hexalith.Projects.Contracts/Ui/{ReferenceState.cs, ProjectLifecycle.cs, ProjectReasonCode.cs}`, OpenAPI `referenceKind: memory` reservations at lines ~1166/1200/1865/2014). Confirmed no existing `src/Hexalith.Projects.Server/Memories/` directory and no `IProjectMemoryDirectory` references in `src/Hexalith.Projects.Server/`. Task 2 — re-verified Memories surface in this working tree: `GetCaseAsync` (line ~810) still carries `/// <remarks>Stable since Story 10.2.</remarks>`; `CreateTenantAsync` (~270), `CreateCaseAsync` (~344), `IngestAsync` (~405), `GetTelemetrySummaryAsync` (~595) all annotated `[System.Diagnostics.CodeAnalysis.Experimental("HXL001")]`; `ListHandlersAsync` (~640), `GetHandlerMismatchesAsync` (~683) annotated `[Experimental("HXL002")]`. `Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/Case.cs` is a `sealed record` with required `Id`, `TenantId`, `Name`, `Description?`, `Status` (`CaseStatus { Active, Closed, Deleting }`), `CreatedAt`, `LastUpdated`, `MemoryUnitCount`. `MemoryUnit.cs` includes `Content`, `ContentHash`, `SourceUri`, `SourceType`, `IngestedBy`, `IngestedAt`, `LastUpdated`, `MemoryUnitStatus`, `Metadata`, `EmbeddingProvider`, `EmbeddingModel`, `EmbeddingDimensions`, `Classification`, `FailureDetails`. `ErrorResponse(Code, Message, Suggestion)`. Memories server error codes confirmed in working tree: `TENANT_NOT_FOUND`, `CASE_NOT_FOUND`, `MEMORY_UNIT_NOT_FOUND`, `INVALID_MEMORY_UNIT_ID`, plus client-side synthetic `INVALID_RESPONSE`. `GetTenantAsync` soft-misses to `null` on 404; other read routes throw `MemoriesRemoteException`. No HALT triggered — every assumption in the story Dev Notes matched the working-tree evidence.
- 2026-05-28 (dev-story): Authored `docs/adr/memories-link-target.md` per Task 3 sections (Context, Considered options, Decision, Identifier shape, ACL contract surface, Eventual-consistency strategy, Failure-to-shared-vocabulary mapping, Epic 3 allowlist treatment, `[Experimental]` surface containment, Consequences, Out of scope, References). Decision named once in bold: "A single Project Memory link targets exactly one Memories `Case`." Followed CRLF / UTF-8 / final-newline norm of `docs/adr/identifier-boundary.md` (converted with `sed -i 's/$/\r/'` after Write). No ULIDs, no real tenant ids, no Memories `Content`/`ContentHash`/`SourceUri`/`Message`/`Suggestion` values appear in the ADR; reference-state vocabulary stays inside `ReferenceState.cs` / `ProjectLifecycle.cs` / `ProjectReasonCode.cs`.
- 2026-05-28 (dev-story): Task 4 — added the AR-G4 resolution pointer to `_bmad-output/planning-artifacts/architecture.md` at the existing "Nice-to-Have Gaps" Memories-linkage bullet (the file does not carry a literal `AR-G4` marker line; the bullet at lines 711–712 is the canonical AR-G4 mention). One-line addition only. No edit to `epics.md` Story 2.6 / 2.7 acceptance criteria. No standalone ADR index file exists under `docs/adr/`; per Task 4, `docs/adr/` listing is sufficient (only `identifier-boundary.md` and the new `memories-link-target.md`).
- 2026-05-28 (dev-story): Task 5 self-check — grepped the ADR for ULID-shaped tokens, secret / password / token / api_key strings, content-hash / memoryUnitContent payload exemplars: clean. All reference-state / lifecycle / reason-code terms used (`Included`, `Archived`, `Unauthorized`, `Unavailable`, `Stale`, `TenantMismatch`, `InvalidReference`, `Pending`, `Active`, `MemoryMatched`) come straight from `src/Hexalith.Projects.Contracts/Ui/`. The ACL outcome enum (`ProjectMemoryValidationOutcome`) is Projects-internal and mirrors `ProjectFolderValidationOutcome` / `ProjectFileReferenceValidationOutcome` — not a shared-vocabulary parallel.
- 2026-05-28 (dev-story): Task 6 — `git diff --check` clean on the three touched files (`docs/adr/memories-link-target.md`, `_bmad-output/planning-artifacts/architecture.md`, `_bmad-output/implementation-artifacts/sprint-status.yaml`) and this story file. No `dotnet build` / `dotnet test` run — spike deliverable is documentation only. No `.g.cs` regeneration. No edit anywhere under `src/Hexalith.Projects.*`. No edit under `Hexalith.Memories/**`. No nested recursive submodule init. The pre-existing `M docs/adr/identifier-boundary.md` in `git status` is a `core.autocrlf`-style line-ending shimmer from a prior session, not a content change made here, and is left untouched.

### Completion Notes List

- **Decision selected:** Option A — a single Project Memory link targets exactly one Memories `Case`. Stored `memoryReferenceId` equals `Case.Id` (ULID-shaped `string`, no Projects-owned VO). Recorded once in bold in the ADR's Decision section; every subsequent ADR section is consistent.
- **ADR path:** `docs/adr/memories-link-target.md` (new file, second ADR under `docs/adr/`). Header matches the [[identifier-boundary]] ADR style.
- **Chosen Memories client read route:** `MemoriesClient.GetCaseAsync(string tenantId, string caseId, CancellationToken ct)` — documented "Stable since Story 10.2" in the working tree. This is the only Memories method Story 2.7's ACL is permitted to call. `GetMemoryUnitAsync`, `HybridSearchAsync`, `SearchAsync`, `TraverseAsync`, `ExportCaseAsync`, `ExportTenantAsync`, and every `[Experimental("HXL001"/"HXL002")]` method are forbidden from the Projects ACL (content-bearing and/or experimental surface).
- **`IProjectMemoryDirectory` ACL surface designed:** validation method `ValidateLinkMemoryReferenceAsync(ProjectId, memoryReferenceId, correlationId, taskId, ct)`, validation-result enum `ProjectMemoryValidationOutcome { Accepted, ValidationFailed, Denied, Archived, Stale, TenantMismatch, Unavailable }`, fail-closed `UnavailableProjectMemoryDirectory` fallback, typed-client lifetime **transient / request-scoped** (per Story 2.4 / 2.5 review fix).
- **Eventual-consistency strategy:** ACL validates only via `GetCaseAsync`; no read-after-write assumption; Story 2.7 tests use deterministic fakes / stubbed `HttpMessageHandler` / fake clocks — `Thread.Sleep`, `Task.Delay`, `SpinWait`, and wall-clock retry loops are forbidden in Story 2.7 tests.
- **Epic 3 allowlist treatment:** an absent / unavailable / archived / unauthorized memory reference is a fail-closed-clean exclusion carrying the existing shared-vocabulary `ReferenceState` (`Unauthorized` / `Unavailable` / `Stale` / `Archived` / `TenantMismatch` / `InvalidReference`); the assembled `ProjectContext` is always `2xx`. Cross-tenant existence is never disclosed (safe-denial 404 collapse).
- **`[Experimental]` containment:** Story 2.7 is expected to ship zero `#pragma warning disable HXL001` / `HXL002` suppressions because the chosen design only calls stable routes. Any future unavoidable suppression must live in one Memories ACL façade file under `src/Hexalith.Projects.Server/Memories/`; aggregate, projections, contracts, OpenAPI, generated `.g.cs`, and tests must remain free of HXL suppressions.
- **Failure mapping:** all error modes from AC 6 (`MemoriesRemoteException` 401/403/404/409/408/503/5xx/400/422, `TENANT_NOT_FOUND`, `CASE_NOT_FOUND`, `MEMORY_UNIT_NOT_FOUND`, `INVALID_MEMORY_UNIT_ID`, `INVALID_RESPONSE`, network failure, no client registered, `CaseStatus != Active`, `MemoryUnitStatus` lifecycle) mapped in the ADR to existing shared-vocabulary values. No new `ReferenceState` / `ProjectLifecycle` / `ProjectReasonCode` enum value introduced.
- **AR-G4 resolution pointer:** added a single sentence under the "Nice-to-Have Gaps → Memories linkage" bullet in `_bmad-output/planning-artifacts/architecture.md` (the file has no literal `AR-G4` label line; that bullet is the AR-G4 mention). No `epics.md` Story 2.6 / 2.7 acceptance criteria edits.
- **HALT items:** none. Every Memories-surface assumption (`GetCaseAsync` stability, HXL001 / HXL002 annotations, `Case` / `MemoryUnit` / `ErrorResponse` shapes) matched the working-tree evidence.
- **Items deferred to Story 2.7 (recorded in ADR Consequences / Out of Scope):** every `.cs` implementation file under `src/Hexalith.Projects.Server/Memories/`; `LinkMemory` / `UnlinkMemory` commands / events / handlers / projections / endpoints / OpenAPI routes / regenerated NSwag client / regenerated idempotency helpers / audit timeline rows; `MemoryReference` model and `ProjectState.MemoryReferences` bounded set; `ProjectReferenceIndexProjection` memory-kind rows; `MaxMemoryReferences` configuration; per-`MemoryUnit` pins (hybrid option deferred); semantic / hybrid search wiring; ingestion orchestration; bulk re-link / repair; `docs/event-catalog.md` entries for `MemoryLinked` / `MemoryUnlinked`; `NoPayloadLeakage` fixture extensions over Memory events; idempotency fingerprint field lists for the Memory write surface; real-Keycloak E2E proofs (Epic 5).
- **Validation:** no `dotnet build` / `dotnet test` was run — story is documentation-only by design (see AC 9–10 and Validation/Testing Requirements). `git diff --check` clean on touched files. `git status` shows only the ADR addition, the one-line architecture pointer, this story file, and the sprint-status flip; no `src/Hexalith.Projects.*` source change, no `.g.cs` change, no Memories submodule edit, no submodule pointer change, no nested recursive submodule init.

### File List

- `docs/adr/memories-link-target.md` (new) — the canonical ADR; ~340 lines; LF / UTF-8 / final newline (re-normalised from CRLF during review per the repo line-ending convention; see `MEMORY.md` build-environment note).
- `_bmad-output/planning-artifacts/architecture.md` (modified) — one-sentence AR-G4 resolution pointer added under the "Nice-to-Have Gaps → Memories linkage" bullet pointing at the new ADR. No other edits.
- `_bmad-output/implementation-artifacts/2-6-memories-linkage-decision-spike.md` (modified) — Tasks/Subtasks checked, Dev Agent Record / Completion Notes / File List / Change Log populated, Status flipped to `review`.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified) — `2-6-memories-linkage-decision-spike` flipped `ready-for-dev` → `in-progress` → `review` over the course of this run.

### Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot (automated review, Claude Opus 4.7) on 2026-05-28
**Outcome:** Approve (with auto-fixes applied — see below)

**Findings and fixes applied automatically during this review:**

- **HIGH (AC 6 gap) — `TenantStatus` not mapped in the failure-to-shared-vocabulary table.** AC 6 explicitly required mapping `TenantStatus` not `Active` (per `Hexalith.Memories.Contracts.V1.TenantStatus { Provisioning, Active, Deleting, Failed, CompensationFailed }`). The original ADR table omitted any `TenantStatus` row. **Fix:** added a `TenantStatus` row to the mapping table in `docs/adr/memories-link-target.md` explaining that the stable Case read route does not expose `TenantStatus` directly — inactive tenants surface either as `404` (`TENANT_NOT_FOUND` → `Denied` / `Unauthorized` via the safe-denial collapse) or as `503` / 5xx during provisioning / teardown → `Unavailable`. Documents that Projects never echoes Memories `TenantStatus` values.
- **HIGH (AC 6 explicitness) — `MEMORY_UNIT_NOT_FOUND` only implicitly covered.** AC 6 listed `MEMORY_UNIT_NOT_FOUND` as a required mapping target. The original 404 row named `CASE_NOT_FOUND` and `TENANT_NOT_FOUND` but not `MEMORY_UNIT_NOT_FOUND`. **Fix:** added `MEMORY_UNIT_NOT_FOUND` to the 404 row in the mapping table. (The chosen Option A ACL surface does not call `GetMemoryUnitAsync`, so the code can never actually receive this — but the taxonomy is now complete per AC 6.)
- **MEDIUM (AC 6 explicitness) — `MemoryUnitStatus` row now annotates "not-yet-indexed / not-yet-searchable" coverage.** AC 6 specifically called out "the `MemoryUnitStatus` lifecycle including not-yet-indexed / not-yet-searchable states." **Fix:** updated the `MemoryUnitStatus` row to call those out explicitly while preserving the "n/a under Option A" rationale (Case-level link does not validate per-unit).
- **MEDIUM (repo-convention drift) — new ADR authored with CRLF line endings; pre-existing `docs/adr/identifier-boundary.md` and `_bmad-output/planning-artifacts/epics.md` showed as fully-modified in `git status` due to CRLF / LF churn.** Per the user's recorded repo convention (`MEMORY.md` → `build-environment.md` → *"HEAD stores `.cs`/`.yaml`/`.md` as LF (no `.gitattributes`, no `autocrlf`). Keep hand-written sources LF too, or `git diff` shows whole-file churn."*), all hand-written `.md` must be LF on disk even though `.editorconfig` says `end_of_line = crlf`. **Fix:** ran `sed -i 's/\r$//'` on the new ADR (`docs/adr/memories-link-target.md`), on `docs/adr/identifier-boundary.md` (back to its HEAD state), and on `_bmad-output/planning-artifacts/epics.md` (back to its HEAD state). `git diff --stat` after the fix shows only the four story-owned files remain modified, with no unrelated whole-file churn. The earlier File List claim that the ADR was "CRLF / UTF-8 / final newline" has been corrected to LF / UTF-8 / final newline above.
- **LOW (no fix needed) — `AR-13`, `AR-9`, `AR-11`, `AR-14`, `AR-18`, `AR-G4` are referenced by tag in the ADR but `_bmad-output/planning-artifacts/architecture.md` does not carry literal `AR-#` section markers.** This matches the existing convention used by `epics.md` (which references `PR-4` / `AR-G4` the same way) and by Story 2.6 itself; no drift introduced by this story.
- **LOW (no fix needed) — Considered options is a subsection of Context (`### Considered options`) rather than a top-level `## Considered options` section.** Task 3's required-section list does not mandate a top-level placement; the ADR keeps options grouped with their motivating context. The Decision section names "Case" once, in bold, on line 67; every subsequent section is consistent.

**AC re-check after fixes:** AC 1 through AC 11 all satisfied. AC 6 is now exhaustively covered (`TenantStatus`, `CaseStatus`, `MemoryUnitStatus`, every named error code, every named HTTP status range). No new shared-vocabulary enum values introduced. No code changes outside the ADR + architecture pointer + this story file + `sprint-status.yaml`. No `src/Hexalith.Projects.*` source edited. No `.g.cs` regenerated. No Memories submodule edit or pointer advance. No nested recursive submodule init. `git diff --check` clean on touched files.

## Change Log

| Date | Version | Description | Author |
| --- | --- | --- | --- |
| 2026-05-28 | 1.0 | Created Story 2.6 spike artifact and set sprint status to ready-for-dev. Spike deliverable is the ADR at `docs/adr/memories-link-target.md` deciding Case vs MemoryUnit (recommended: Case) plus identifier shape, `IProjectMemoryDirectory` ACL surface, eventual-consistency strategy (deterministic, no sleeps), failure-to-shared-vocabulary mapping, Epic 3 allowlist treatment (fail-closed-clean), and `[Experimental]` containment strategy. No Projects source changes, no Memories submodule changes, no `.g.cs` regeneration, no nested recursive submodule init. | Claude Opus 4.7 |
| 2026-05-28 | 1.1 | Dev-story executed. Authored `docs/adr/memories-link-target.md` (Status: Accepted) selecting **Option A — a single Project Memory link targets exactly one Memories `Case`** with `MemoriesClient.GetCaseAsync` (stable since Memories Story 10.2) as the sole permitted ACL read route. ADR covers: identifier shape (`memoryReferenceId = Case.Id`, ULID-shaped `string`; envelope `tenantId` implicit; explicit forbidden-fields list), `IProjectMemoryDirectory` ACL surface (interface signature, `ProjectMemoryValidationOutcome { Accepted, ValidationFailed, Denied, Archived, Stale, TenantMismatch, Unavailable }`, transient / request-scoped lifetime, `UnavailableProjectMemoryDirectory` fail-closed fallback), eventual-consistency strategy (validate-only-via-stable-read, no read-after-write, deterministic test convergence — no `Thread.Sleep` / `Task.Delay` / wall-clock retries), failure-to-shared-vocabulary mapping (`MemoriesRemoteException` 401/403/404/408/409/422/503/5xx, `TENANT_NOT_FOUND`, `CASE_NOT_FOUND`, `INVALID_RESPONSE`, `CaseStatus` transitions, network failure, no-client-registered — all mapped to existing `ReferenceState` / `ProjectLifecycle` values), Epic 3 allowlist fail-closed-clean exclusion treatment, and `[Experimental]` containment (Story 2.7 expected to ship zero HXL001/HXL002 pragma suppressions; any future suppression confined to a single Memories ACL façade file). Added a one-sentence AR-G4 resolution pointer in `_bmad-output/planning-artifacts/architecture.md`. No `src/Hexalith.Projects.*` source change, no `.g.cs` change, no Memories submodule edit or pointer advance, no nested recursive submodule init. No new shared-vocabulary enum values. No `dotnet build` / `dotnet test` (spike is documentation-only). `git diff --check` clean. Status flipped to `review`. | Claude Opus 4.7 |
| 2026-05-28 | 1.2 | Senior developer review (story-automator). Auto-fixes applied: (a) extended the ADR failure-to-shared-vocabulary table to explicitly cover `TenantStatus` not `Active` (Provisioning / Deleting / Failed / CompensationFailed) — AC 6 gap — and named `MEMORY_UNIT_NOT_FOUND` alongside `CASE_NOT_FOUND` / `TENANT_NOT_FOUND` in the 404 row; (b) annotated the `MemoryUnitStatus` row with "not-yet-indexed / not-yet-searchable" coverage per AC 6 wording while keeping the Option A "n/a — Case-level link" rationale; (c) re-normalised `docs/adr/memories-link-target.md`, `docs/adr/identifier-boundary.md`, and `_bmad-output/planning-artifacts/epics.md` from CRLF to LF to match the recorded repo line-ending convention (HEAD stores `.md` as LF; the original ADR's CRLF authoring caused `git diff` whole-file churn on neighbouring files). `git diff --stat` after the fix shows only the four story-owned files modified; no other unrelated churn. All 11 ACs re-checked. Status remains `review` → `done` per the senior review outcome. | Claude Opus 4.7 |
