# ADR: Memories link target — one Project Memory link = one Memories `Case`

- Status: Accepted
- Date: 2026-05-28
- Story: 2.6 (Memories linkage decision spike)
- Requirement: AR-G4 (Memories Case-vs-Unit), PR-4 (Memories deferred behind this decision)

## Context

Hexalith.Projects must decide, before Story 2.7 writes the `LinkMemory` / `UnlinkMemory`
write-side, **what a single Project Memory link points to inside Hexalith.Memories**. The
Memories identity hierarchy is `Tenant → Case → MemoryUnit`, so a Project Memory link could
in principle target a `Case`, an individual `MemoryUnit`, or both (hybrid). The decision is
load-bearing because three properties of `Hexalith.Memories` are materially different from
Folders, where the analogous reference-target question was trivial:

1. **Experimental write surface.** `MemoriesClient.CreateTenantAsync`, `CreateCaseAsync`,
   `IngestAsync`, and `GetTelemetrySummaryAsync` are annotated `[Experimental("HXL001")]`;
   `ListHandlersAsync` / `GetHandlerMismatchesAsync` are `[Experimental("HXL002")]`. Their
   signatures may shift in upstream phases (per `references/Hexalith.Memories/docs/dev/experimental-apis.md`).
2. **Asynchronous, eventually-consistent ingestion.** Memories ingestion is `202 Accepted` +
   Dapr Workflow + triple-write to RediSearch + Redis Vector + FalkorDB. There is **no
   read-after-write** guarantee; verify-then-repair is the upstream-documented contract
   (`references/Hexalith.Memories/docs/dev/consistency.md`).
3. **Identity hierarchy.** A `Case` groups many `MemoryUnit`s; per-unit reference cardinality
   is unbounded in practice (`Case.MemoryUnitCount`). A Project Memory link does not have a
   one-to-one mapping by construction.

Projects' shared vocabulary and OpenAPI spine already reserve `memory` as a `referenceKind`
value at both the request enum and the `ProjectReferenceSummary.referenceKind` response enum
(`src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml`). The architecture lists
`MemoryLinked` / `MemoryUnlinked` as Projects events (`AR-6`) and `MemoryMatched` as a
`[ProjectionBadge]` reason code (`AR-18`). Story 2.6 must reach a decision that is consistent
with those reserved surfaces and with `PR-4` (Memories writes deferred behind this spike).

This ADR is the canonical record of that decision. It is the sole production deliverable of
Story 2.6 alongside a one-line `AR-G4`-resolution pointer in
`_bmad-output/planning-artifacts/architecture.md`. Story 2.7 is where every implementation
artifact (`LinkMemory` / `UnlinkMemory` commands and events, `MemoryReference` state,
`ProjectReferenceIndexProjection` memory rows, `IProjectMemoryDirectory` implementation,
endpoints, OpenAPI routes, regenerated typed client / idempotency helpers, and
`docs/event-catalog.md` entries) lands.

### Considered options

- **Option A — Case-level link (chosen).** A Project Memory link is exactly one Memories
  `Case`. Stored `memoryReferenceId` equals `Case.Id`. Validation route: `GetCaseAsync`
  (stable since Memories Story 10.2). One Case per project / conversation matches the
  Memories consumer model described in
  `_bmad-output/planning-artifacts/research/technical-hexalith-memories-rag-research-2026-05-24.md`.
- **Option B — MemoryUnit-level link.** A Project Memory link is one or more `MemoryUnit`s.
  Stored shape requires `caseId` + `memoryUnitId`. Validation route: `GetMemoryUnitAsync` —
  which returns a `MemoryUnit` whose schema includes `Content`, `ContentHash`, `SourceUri`,
  and embedding provenance (fields Projects must never receive past the ACL boundary). Each
  Project would carry unbounded per-link rows (Memories' `Case.MemoryUnitCount` is not
  bounded), inflating `ProjectState` and `ProjectReferenceIndexProjection` cardinality and
  raising the eventual-consistency exposure surface (a newly ingested unit may not be
  retrievable for some time after the `IngestAsync` `202`).
- **Option C — Hybrid (Case primary + optional pinned `MemoryUnit`s under the same
  reference).** Combines both validation routes and both identifier shapes. Highest
  implementation cost; pins reopen Options B's eventual-consistency exposure for the
  per-unit subset; no upstream Memories surface today materially benefits from the extra
  granularity at link time.

## Decision

**A single Project Memory link targets exactly one Memories `Case`.**

Rationale:

- The Memories research recommends "one Case per project / conversation" as the consumer
  model.
- `MemoriesClient.GetCaseAsync(tenantId, caseId, ct)` is documented "Stable since Story 10.2"
  and is the only stable read route covering the chosen target. Picking `Case` lets the
  Projects-to-Memories ACL be implemented entirely against the stable surface.
- Case granularity keeps the bounded `ProjectState.MemoryReferences` cardinality consistent
  with the bounded `FileReferences` set Story 2.5 already established, and keeps the
  per-kind `ProjectReferenceIndexProjection` keys disjoint between `folder`, `file`, and
  `memory` (AR-9, AR-3).
- Picking Case side-steps the `[Experimental("HXL001")]` write surface entirely: the ACL
  never calls `CreateTenantAsync`, `CreateCaseAsync`, `IngestAsync`, or
  `GetTelemetrySummaryAsync`, so the experimental signature churn never leaks into Projects.
- The stored identifier reduces to a single opaque string (`Case.Id`, ULID-shaped) and
  satisfies the sibling-identifier-reuse rule in [[identifier-boundary]].

`GetMemoryUnitAsync` returns a `MemoryUnit` schema that includes `Content`, `ContentHash`,
and `SourceUri` — fields the ACL is explicitly forbidden from forwarding to Projects-owned
state, events, projections, logs, audit, or any DTO. Option A is the only option whose
validation route avoids those fields entirely.

## Identifier shape

The Story 2.7 `MemoryReference` descriptor stores **only** the following fields, and the
`MemoryLinked` / `MemoryUnlinked` events emit only these fields as payload:

| Field | Type | Source | Notes |
| --- | --- | --- | --- |
| `memoryReferenceId` | `string` (ULID-shaped) | `Case.Id` | Opaque sibling identifier reused per [[identifier-boundary]]; never wrapped in a Projects-owned VO. Validation accepts any non-whitespace string. |
| `referenceKind` | `string` enum | constant `memory` | Already reserved in the OpenAPI spine; never re-emitted by this ADR. |
| `displayName` | `string?` | `Case.Name` | Optional safe label for surface rendering; not load-bearing for authorization. |
| `lifecycle` | `ProjectLifecycle` | derived from `Case.Status` | `Active → Active`; `Closed`/`Deleting → Archived`. |
| `referenceState` | `ReferenceState` | derived from ACL outcome | Always from the shared vocabulary (`src/Hexalith.Projects.Contracts/Ui/ReferenceState.cs`); see the failure mapping below. |
| `reasonCode` | `ProjectReasonCode?` | constant `MemoryMatched` when included | Already reserved in `src/Hexalith.Projects.Contracts/Ui/ProjectReasonCode.cs`; no new reason code is introduced. |
| `occurredAt` | `DateTimeOffset` | event envelope occurrence time | Owned by EventStore envelope semantics; aggregate `Handle` does not invent wall-clock time. |

The owning `tenantId` is **implicit**: it equals the EventStore envelope tenant of the
Project stream. It is **never** re-emitted into the `MemoryLinked` / `MemoryUnlinked`
payload, and the ACL never accepts a caller-supplied `tenantId` for Memory-link validation —
the envelope tenant is the only trusted source (consistent with the layered tenant-isolation
rule under AR-13, AR-18, and the layered-authz model `1-6`).

The following fields are **explicitly forbidden** in `MemoryReference`, in
`ProjectState.MemoryReferences`, in `MemoryLinked` / `MemoryUnlinked` payloads, in
`ProjectReferenceIndexProjection` rows, in `ProjectDetailProjection` / `ProjectListProjection`
items, in `ProjectAuditTimelineProjection` rows, in logs, and in any operator-visible
diagnostic or surface:

- `MemoryUnit.Content`, `ContentBytes`, `ContentHash`, `SourceUri`, `SourceType`,
  `IngestedBy`, `IngestedAt`, `Metadata`, `EmbeddingProvider`, `EmbeddingModel`,
  `EmbeddingDimensions`, `Classification`, `FailureDetails`.
- Raw `IngestionInput` payloads (text/bytes), embedding vectors, vector dimensions, fusion
  weights, search snippets, traversal payloads.
- Raw upstream `ErrorResponse.Message` / `ErrorResponse.Suggestion` text, raw
  `MemoriesRemoteException.Message`, raw upstream problem-detail bodies.
- Tokens (any), file paths, prompt fragments, conversation transcript fragments.
- The Memories internal tenant identifier as a payload field on Projects events (it is the
  envelope tenant, not a payload).

The identifier shape rule reuses the existing AR-7 / [[identifier-boundary]] decision that
sibling references are held as plain ULID-shaped `string`s. Projects does **not** introduce a
`MemoryId` / `CaseId` Projects-owned value object.

## ACL contract surface

Story 2.7 implements a Projects-owned ACL under `src/Hexalith.Projects.Server/Memories/`
mirroring the Story 2.4 / 2.5 Folders and File-Reference pattern. This ADR fixes the surface
shape so Story 2.7 does not re-litigate it.

```text
src/Hexalith.Projects.Server/Memories/
├─ IProjectMemoryDirectory.cs              # interface (this ADR)
├─ MemoriesProjectMemoryDirectory.cs       # MemoriesClient-backed implementation (Story 2.7)
├─ ProjectMemoryValidationResult.cs        # discriminated outcome (this ADR)
└─ UnavailableProjectMemoryDirectory.cs    # fail-closed fallback (this ADR)
```

The interface signature Story 2.7 will implement:

```csharp
public interface IProjectMemoryDirectory
{
    Task<ProjectMemoryValidationResult> ValidateLinkMemoryReferenceAsync(
        ProjectId projectId,
        string memoryReferenceId,
        string correlationId,
        string taskId,
        CancellationToken cancellationToken = default);
}
```

The validation-result outcome enum mirrors the
`ProjectFileReferenceValidationOutcome` / `ProjectFolderValidationOutcome` taxonomy:

| Outcome | Meaning | Maps to `ReferenceState` (Epic 3 assembly) |
| --- | --- | --- |
| `Accepted` | Memories `GetCaseAsync` returned a `Case` whose `Status` is `Active` and whose tenant matches the Projects envelope tenant. | `Included` (Story 2.7 link is accepted) |
| `ValidationFailed` | The Projects-shaped request is invalid (malformed `memoryReferenceId`, blank correlation, etc.) or Memories returned `INVALID_RESPONSE` (version skew). | `InvalidReference` |
| `Denied` | Memories returned `401` / `403` / `404` / `TENANT_NOT_FOUND` / `CASE_NOT_FOUND`; the safe-denial principle collapses unauthorized and not-found at the boundary so cross-tenant existence is not disclosed. | `Unauthorized` (the boundary intentionally does not distinguish "not found" from "not permitted") |
| `Archived` | `Case.Status` is `Closed` or `Deleting`. | `Archived` |
| `Stale` | The validation evidence is older than the Story 2.7 freshness budget for a mutation. | `Stale` |
| `TenantMismatch` | Documented for taxonomy symmetry with the Folders / File-Reference outcomes; in practice the safe-denial 404 collapse means Memories will almost never surface a distinguishable cross-tenant case (mirrors the Story 2.5 review LOW observation). | `TenantMismatch` |
| `Unavailable` | `408` / `503` / 5xx / network failure / no `IProjectMemoryDirectory` registered. | `Unavailable` |

The validation-result record is symmetric with `ProjectFileReferenceValidationResult` and
carries only a safe correlation identifier — never raw upstream error text:

```csharp
public sealed record ProjectMemoryValidationResult(
    ProjectMemoryValidationOutcome Outcome,
    string? CorrelationId)
{
    public static ProjectMemoryValidationResult Accepted(string? correlationId)
        => new(ProjectMemoryValidationOutcome.Accepted, correlationId);
}
```

`UnavailableProjectMemoryDirectory` is the fail-closed fallback registered by default when
the host does not configure a Memories typed client (mirrors
`UnavailableProjectFolderDirectory` / `UnavailableProjectFileReferenceDirectory`). It always
returns `Unavailable`, never `Accepted`. Story 2.7 must register
`IProjectMemoryDirectory` with `TryAddTransient` (or request-scoped, per the Story 2.4 / 2.5
review fix for typed-client / bearer-handler chains), never as a singleton.

The ACL surface is **metadata-only** by construction:

- It accepts only a `ProjectId` + a `memoryReferenceId` (the Case id) + safe diagnostic
  identifiers.
- It returns only a discriminated outcome + a safe `CorrelationId`. Story 2.7 may extend the
  `Accepted` shape to carry a small `MemoryReferenceMetadata` (safe `displayName` from
  `Case.Name`, `lifecycle` from `Case.Status`, `observedAt`) as long as the additions remain
  drawn from the identifier-shape table above.

## Eventual-consistency strategy

Memories ingestion is asynchronous and eventually consistent; Projects never assumes
read-after-write. The ACL therefore validates **only** through stable Memories read routes,
and Story 2.7 tests assert convergence **deterministically** — never with wall-clock waits.

Rules (binding on Story 2.7):

1. The Projects link command (Story 2.7 `LinkMemory`) validates the Project mutation intent
   first (tenant gate → project-level authz → expected-current guard), exactly as Story 2.3
   / 2.5 do, before any Memories call.
2. The ACL calls **only** `MemoriesClient.GetCaseAsync(tenantId, caseId, ct)` and maps the
   typed result to a `ProjectMemoryValidationResult` per the table above. It never calls
   `IngestAsync`, `CreateCaseAsync`, `CreateTenantAsync`, `GetTelemetrySummaryAsync`,
   `ListHandlersAsync`, `GetHandlerMismatchesAsync`, `HybridSearchAsync`, `SearchAsync`,
   `TraverseAsync`, `GetMemoryUnitAsync`, `ExportCaseAsync`, or `ExportTenantAsync`.
3. The Memories typed-client registration uses **transient / request-scoped** lifetime
   (mirrors Story 2.4 / 2.5 review fix). Singleton capture of `HttpClient` chains with
   bearer handlers is forbidden.
4. At link time, a Case that cannot be found returns `Denied` → the link command rejects
   fail-closed via `ProjectReferenceLinkRejected` carrying a `ReferenceState` of
   `Unauthorized` and a `ProjectReasonCode` consistent with the shared vocabulary. Projects
   does **not** introduce a Memory-specific "pending ingestion" enum value; if Story 2.7
   chooses to persist a memory link before the upstream Case is fully visible (it should
   not, by default), the persisted reference carries the existing `Unavailable` or `Pending`
   `ReferenceState`, never a new enum.
5. At assembly time (Epic 3 `GetProjectContext`), a stored memory link may flip across runs
   between `Included` / `Archived` / `Unavailable` / `Unauthorized` purely because Memories
   moved (e.g. `Case.Status` → `Closed`). The Epic 3 allowlist treats every non-`Included`
   outcome as a fail-closed-clean exclusion (see the next section); the assembled
   `ProjectContext` is still `2xx`.
6. **Test convergence is deterministic.** Story 2.7 Tier-1 tests inject a deterministic fake
   `IProjectMemoryDirectory` (or, for Tier-2 boundary tests, a stubbed
   `HttpMessageHandler` behind `MemoriesClient` per the test pattern in the Memories
   research). The following are **forbidden** in Story 2.7 tests: `Thread.Sleep`,
   `Task.Delay`, `SpinWait`, wall-clock retry loops, polling with real time, and any
   "wait for backend" pattern that depends on real elapsed time. Test time is either
   stepped explicitly (fake clock) or asserted on injected state-transition events.

## Failure-to-shared-vocabulary mapping

Every Memories failure mode the ACL must handle resolves to a value from the existing
shared vocabulary (`src/Hexalith.Projects.Contracts/Ui/ReferenceState.cs` +
`ProjectLifecycle.cs`). No new enum values are introduced by this ADR; if Story 2.7's design
later genuinely requires one, that work is recorded as a Consequence below and decided in a
follow-up ADR — not invented inline.

| Upstream signal | `ProjectMemoryValidationOutcome` | Surfaced `ReferenceState` | Notes |
| --- | --- | --- | --- |
| `GetCaseAsync` 200 + `Case.Status == Active` + tenant matches envelope tenant | `Accepted` | `Included` | Happy path. |
| `GetCaseAsync` 200 + `Case.Status == Closed` | `Archived` | `Archived` | Case lifecycle has ended. |
| `GetCaseAsync` 200 + `Case.Status == Deleting` | `Archived` | `Archived` | Case is being torn down upstream; treated as archived for Projects purposes. |
| `MemoriesRemoteException` 401 | `Denied` | `Unauthorized` | Caller credentials rejected by Memories. |
| `MemoriesRemoteException` 403 | `Denied` | `Unauthorized` | Caller authenticated but not permitted by Memories. |
| `MemoriesRemoteException` 404 (any code, incl. `CASE_NOT_FOUND`, `TENANT_NOT_FOUND`, `MEMORY_UNIT_NOT_FOUND`) | `Denied` | `Unauthorized` | Safe-denial collapse — never distinguishes "not found" from "not permitted" at the boundary. |
| `MemoriesRemoteException` 408 | `Unavailable` | `Unavailable` | Upstream timeout. |
| `MemoriesRemoteException` 503 | `Unavailable` | `Unavailable` | Upstream unavailable. |
| `MemoriesRemoteException` other 5xx | `Unavailable` | `Unavailable` | Upstream server error. |
| `MemoriesRemoteException` 409 | `ValidationFailed` | `InvalidReference` | Memories conflict surfaced to Projects as an invalid-reference; conflict semantics do not exist for the stable read route, but the taxonomy must remain total. |
| `MemoriesRemoteException` 400 / 422 (incl. `INVALID_MEMORY_UNIT_ID`, malformed request) | `ValidationFailed` | `InvalidReference` | Malformed reference identifier or other request-shape failure. |
| `MemoriesRemoteException` with `ErrorResponse.Code == "INVALID_RESPONSE"` | `ValidationFailed` | `InvalidReference` | Version skew between Memories client and server; never echo raw `Message` / `Suggestion`. |
| Network failure / DNS / connection reset | `Unavailable` | `Unavailable` | Treated identically to 5xx. |
| No `IProjectMemoryDirectory` registered | `Unavailable` | `Unavailable` | `UnavailableProjectMemoryDirectory` fallback. |
| Memories `TenantStatus` not `Active` (`Provisioning` / `Deleting` / `Failed` / `CompensationFailed`) | `Unavailable` (or `Denied` if surfaced as 404 by upstream safe-denial) | `Unavailable` (resp. `Unauthorized`) | The stable Case read route does not return `TenantStatus` directly; in practice an inactive tenant either surfaces as `404` (`TENANT_NOT_FOUND`) → `Denied` / `Unauthorized` per the safe-denial collapse above, or as `503` / 5xx during provisioning / teardown → `Unavailable`. Projects never echoes Memories `TenantStatus` values; mapped here for taxonomy totality per AC 6. |
| `MemoryUnitStatus` lifecycle (`Queued` / `Extracting` / `Embedding` / `Indexing` / `Indexed` / `Failed`) — including not-yet-indexed / not-yet-searchable states | n/a (Option A) | n/a (Option A) | Not exposed by the chosen ACL surface — the Case-level link does not validate per-unit, so `MemoryUnit` lifecycle never reaches Projects through `GetCaseAsync`. Listed here per AC 6 for taxonomy totality. If Story 2.7 ever introduces a metadata-only count surface, it must derive lifecycle from `Case.Status`, not from per-unit status. |

The mapping never echoes `MemoryUnit.Content`, `ContentHash`, `SourceUri`, or upstream
`ErrorResponse.Message` / `Suggestion` text into Projects-owned outputs. Story 2.7's
`ProblemDetails` for `ProjectReferenceLinkRejected` carries only the Projects-shaped
`ReferenceState` and a safe correlation identifier — consistent with the Story 1.4 / 2.4 /
2.5 safe-denial precedent.

## Epic 3 allowlist treatment

Per `PR-4` and `AR-9`, the Epic 3 `ProjectContext` assembly policy is **allowlist
inclusion**: a reference is included only when tenant, project, lifecycle, authorization,
and freshness all pass. For Memory references, the rule is:

- A stored `MemoryReference` whose ACL re-validation outcome is not `Accepted` is
  **excluded** from the assembled `ProjectContext` and carries the corresponding
  `ReferenceState` (`Unauthorized` / `Unavailable` / `Stale` / `Archived` / `TenantMismatch`
  / `InvalidReference`) plus a safe reason explanation drawn from the shared
  `ProjectReasonCode` set.
- The assembled `ProjectContext` response is still `2xx` / `success`. An absent or
  unavailable Memories reference **never** propagates as a Projects error / 5xx, and
  **never** returns an existence-revealing partial result. This is identical to how Story
  2.4 / 2.5 already treat unavailable Folder / File references — the Memories case adds no
  new failure semantics, only re-uses the existing fail-closed-clean exclusion vocabulary.
- Cross-tenant existence is never disclosed: a memory whose owning Case belongs to another
  tenant resolves through `Denied` and surfaces as `Unauthorized`, not as
  `TenantMismatch`, at the boundary (the latter outcome is documented for taxonomy
  completeness only — see the Story 2.5 review LOW observation).

This realizes the Epic 2 narrative line "Epic 3's allowlist treats an absent / unavailable
Memories reference as a fail-closed-clean state" verbatim, without inventing new wire
shapes.

## `[Experimental]` surface containment

Story 2.7 is constrained as follows when implementing
`MemoriesProjectMemoryDirectory`:

**Memories methods the ACL is allowed to call:**

- `MemoriesClient.GetCaseAsync(string tenantId, string caseId, CancellationToken ct)` —
  stable since Memories Story 10.2.

**Memories methods the ACL must never call:**

- `CreateTenantAsync`, `CreateCaseAsync`, `IngestAsync`, `GetTelemetrySummaryAsync`
  (`HXL001`).
- `ListHandlersAsync`, `GetHandlerMismatchesAsync` (`HXL002`).
- Content-bearing or search routes: `GetMemoryUnitAsync` (returns `MemoryUnit` with
  `Content` / `ContentHash` / `SourceUri`), `HybridSearchAsync`, `SearchAsync`,
  `TraverseAsync`, `ExportCaseAsync`, `ExportTenantAsync`.

**Pragma containment rule.** Because the chosen design only calls stable routes, Story 2.7
is expected to ship **zero** `#pragma warning disable HXL001` / `HXL002` suppressions. If a
future story genuinely needs an experimental Memories method, the suppression must live in
**exactly one** Memories ACL façade file under `src/Hexalith.Projects.Server/Memories/`
and nowhere else. The following are out of bounds for HXL pragma suppressions in
perpetuity: `src/Hexalith.Projects.Contracts/**`, `src/Hexalith.Projects.Client/**`,
`src/Hexalith.Projects/**` (domain core, including aggregate, projections, validators,
identity helpers), generated `.g.cs` artifacts, the OpenAPI spine and idempotency hasher,
and every test project. This stays consistent with
`references/Hexalith.Memories/docs/dev/experimental-apis.md` (suppression at opt-in call sites only).

## Consequences

What this decision locks in for Story 2.7:

- `MemoryReference` is a bounded set on `ProjectState`, sized analogously to
  `FileReferences` (Story 2.5 set `MaxFileReferences`; Story 2.7 mirrors with
  `MaxMemoryReferences`).
- `LinkMemory` accepts exactly one opaque `memoryReferenceId` (Case id). `UnlinkMemory`
  removes the association from `ProjectState` and `ProjectReferenceIndexProjection`; it
  never calls Memories.
- `MemoryLinked` / `MemoryUnlinked` payloads contain only the identifier-shape fields
  above; nothing more.
- `ProjectReferenceIndexProjection` adds a `memory`-kind row keyed on the bounded reference
  set (per-kind, disjoint from `folder` and `file`).
- The OpenAPI spine adds `/projects/{projectId}/memories` link / unlink routes consistent
  with the existing `referenceKind: memory` enum reservation; the existing enum reservation
  is sufficient and is not re-emitted by Story 2.6.
- The idempotency fingerprint field set for `LinkMemory` / `UnlinkMemory` is the
  identifier-shape subset (excluding `displayName`, which is presentation-only and not part
  of the dedup key).
- The Memories typed client is registered request-scoped / transient at server composition
  time. The `UnavailableProjectMemoryDirectory` fallback is registered as the default when
  no Memories client is configured.
- `docs/event-catalog.md` gains `MemoryLinked` / `MemoryUnlinked` entries with payload
  fields, sensitivity class, and consumers — added by Story 2.7, not by this story.

What is intentionally **deferred**:

- Per-`MemoryUnit` pins on a Project Memory link. Revisit only if measured product evidence
  shows Case granularity is insufficient.
- Semantic / hybrid search wiring (`HybridSearchAsync`, `SearchAsync`, `TraverseAsync`)
  from inside Projects — belongs to a separate feature, not the metadata-only reference
  surface.
- Any ingestion orchestration from Projects. Memories ingestion remains caller-driven and
  fully external to Projects.
- Bulk relink / repair flows.
- Real-Keycloak / E2E Memories proofs (Story 2.7 stays Tier-1 / Tier-2 with synthetic JWTs;
  E2E lives in Epic 5).

If future evidence forces a switch to Option B (per-`MemoryUnit` pins) or Option C
(hybrid), the migration is **additive**: the existing `Case` link is preserved and the
extra pin descriptors are added under a new optional collection, never by re-shaping
`MemoryLinked` / `MemoryUnlinked` payloads (no `V2` events; AR-10 / NFR-6).

## Out of scope

Mirrors the Story 2.5 precedent — none of these are produced by Story 2.6:

- `LinkMemory` / `UnlinkMemory` commands, events, validators, projections, endpoints,
  OpenAPI routes, regenerated typed client / idempotency helpers, audit timeline rows, or
  any executable Projects domain / server code.
- `IProjectMemoryDirectory` implementation, validation-result type files,
  `MemoriesProjectMemoryDirectory`, or `UnavailableProjectMemoryDirectory` `.cs` files —
  Story 2.6 documents the surface; Story 2.7 creates the files.
- Storing `MemoryUnit.Content` / `ContentBytes` / `ContentHash` / `SourceUri` / embeddings
  / vector dimensions / fusion weights / search snippets / raw `IngestionInput` payloads /
  raw `ErrorResponse` body text in any Projects event / state / projection / log / audit
  row.
- Calling Memories `[Experimental("HXL001")]` writes (`CreateTenantAsync`,
  `CreateCaseAsync`, `IngestAsync`, `GetTelemetrySummaryAsync`) or `[Experimental("HXL002")]`
  diagnostic routes from Projects, ever, including in tests.
- Implementing semantic / hybrid search, traversal, or any RAG behavior inside Projects.
- Advancing the `Hexalith.Memories` submodule pointer or editing any file under
  `references/Hexalith.Memories/**`. The ADR cites Memories repo paths as evidence only.
- Performing nested recursive submodule initialization / update.
- Modifying `_bmad-output/planning-artifacts/epics.md` Story 2.6 / 2.7 acceptance criteria
  or any shared-vocabulary enum in `src/Hexalith.Projects.Contracts/Ui/`.

## References

- `_bmad-output/planning-artifacts/architecture.md` — `AR-G4` (Memories Case-vs-Unit),
  `AR-14` (Folders / Memories clients; experimental writes; Tenant → Case → MemoryUnit
  model), `AR-9` (reference index), `AR-11` (ACLs), `AR-18` (shared vocabulary),
  Implementation Sequence step 6.
- `_bmad-output/planning-artifacts/epics.md` — `PR-4` (Memories deferred behind decision
  spike), Story 2.6 (this spike), Story 2.7 (Link / Unlink Memory; consumer of this
  decision).
- `_bmad-output/planning-artifacts/research/technical-hexalith-memories-rag-research-2026-05-24.md`
  — Memories identity model, REST surface, `[Experimental]` write annotations, async ingest
  `202`+poll pattern, fail-closed error handling, RAG retrieval surface; canonical evidence
  base for this decision.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md` —
  `FR-10` (Link Memory), `FR-11` (Unlink Context Reference) consequences.
- `_bmad-output/implementation-artifacts/2-5-link-unlink-file-reference.md` — pattern Story
  2.7 mirrors; per-kind reference-index keys; `Unavailable*Directory` fail-closed
  fallback; typed-client transient registration; deterministic test pattern.
- `_bmad-output/implementation-artifacts/2-4-set-auto-create-project-folder.md` —
  capability-gate discipline; HALT-before-coding precedent.
- [[identifier-boundary]] (`docs/adr/identifier-boundary.md`) — sole prior Projects ADR;
  sibling-identifier reuse rule (Memories ids are plain ULID-shaped strings; no
  Projects-owned `MemoryId` / `CaseId` VO).
- `docs/event-catalog.md` — authoritative event catalog; Story 2.7 adds `MemoryLinked` /
  `MemoryUnlinked` entries (this story does not).
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` — already reserves
  `referenceKind: memory` at both the request enum and the response
  `ProjectReferenceSummary.referenceKind` enum; this ADR does not modify the spine.
- `src/Hexalith.Projects.Contracts/Ui/ReferenceState.cs`,
  `src/Hexalith.Projects.Contracts/Ui/ProjectLifecycle.cs`,
  `src/Hexalith.Projects.Contracts/Ui/ProjectReasonCode.cs` — shared-vocabulary single
  source of truth; no new enum values are introduced by this story.
- `src/Hexalith.Projects.Server/Folders/` — analogue pattern Story 2.7 mirrors under
  `src/Hexalith.Projects.Server/Memories/`.
- `references/Hexalith.Memories/src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` —
  `GetCaseAsync` documented "Stable since Story 10.2"; `CreateTenantAsync`,
  `CreateCaseAsync`, `IngestAsync`, `GetTelemetrySummaryAsync` annotated
  `[Experimental("HXL001")]`; `ListHandlersAsync`, `GetHandlerMismatchesAsync` annotated
  `[Experimental("HXL002")]`.
- `references/Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/Case.cs`,
  `CaseStatus.cs`, `MemoryUnit.cs`, `MemoryUnitStatus.cs`, `ErrorResponse.cs` — Memories
  contract types and error codes the ACL must translate.
- `references/Hexalith.Memories/docs/dev/experimental-apis.md` — authoritative `HXL001` / `HXL002`
  surface and pragma rules.
- `references/Hexalith.Memories/docs/dev/consistency.md` — authoritative eventual-consistency contract
  (no read-after-write; triple-write divergence; verify-then-repair).
