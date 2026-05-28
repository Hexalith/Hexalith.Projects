# Story 1.2: Shared vocabulary, identifiers & payload taxonomy

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **Projects platform engineer**,
I want **the single shared state/reason-code vocabulary, the `ProjectId` identifier, the rejection-event taxonomy, the payload-classification allowlist, and the canonical identity-derivation helper defined once**,
so that **every aggregate, projection, ACL, query, audit record, and generated surface uses the same semantics and no later story reinvents them — preventing vocabulary drift, parallel enums/magic strings, ID drift, and payload leakage**.

This is **Implementation Sequence step 2** ("Contracts-before-code", AR-2): it defines identifiers + shared enums + rejection taxonomy + payload taxonomy + identity-derivation **before** any aggregate/projection/OpenAPI/surface code (Stories 1.3 / 1.4+). It builds directly into the empty-but-compiling skeleton scaffolded in Story 1.1. It is **contracts + one pure helper + Tier-1 tests + a docs/machine allowlist** — no command pipeline, no Dapr, no projections, no aggregate logic, no OpenAPI spine.

## Acceptance Criteria

1. **`ProjectId` opaque validated identifier (AR-7).** Under `src/Hexalith.Projects.Contracts/Identifiers/`, `ProjectId` is a `sealed record` value object that: performs **eager boundary validation** (throws on null/empty/whitespace/invalid value at construction — no deferred validation, no silently-valid empty instance); carries a **custom `System.Text.Json` converter** that serializes/deserializes the opaque string value (registered via `[JsonConverter(typeof(...))]` on the type); and exposes the underlying value as a read-only property. **AND** for sibling references the story does **not** mint parallel VOs — it documents (in Dev Notes / an ADR stub) that sibling identities reuse the owning context's identifier representation (Conversations/Folders/Memories currently expose IDs as **plain `string`/ULID** in their Contracts/OpenAPI, so Projects holds sibling reference IDs as `string` reference descriptors, never as a new `ConversationId`/`FolderId` VO invented inside Projects).

2. **Single `[ProjectionBadge]`-annotated shared vocabulary (AR-18, UX-DR5).** Under `src/Hexalith.Projects.Contracts/Ui/`, exactly one enum set exists — no parallel enums, no magic strings — covering: **lifecycle** (`Active`, `Archived`); **reference/inclusion states** (`Included`, `Excluded`, `Unauthorized`, `Unavailable`, `Stale`, `Archived`, `Ambiguous`, `TenantMismatch`, `Conflict`, `InvalidReference`); **resolution results** (`NoMatch`, `SingleCandidate`, `MultipleCandidates`); and **reason codes** (`ConversationLinked`, `ProjectFolderMatched`, `FileReferenceMatched`, `MemoryMatched`, `MetadataMatched`). Each enum member carries a `[ProjectionBadge(BadgeSlot.X)]` attribute (from `Hexalith.FrontComposer.Contracts.Attributes`) mapping it to a severity slot. **AND** each state has a stable **code** (the enum member name — name-based JSON via `JsonStringEnumConverter<T>`, never integer ordinal on the wire), a **display label**, an **accessible name**, and a **severity mapping** — supplied through a lookup/metadata mechanism that the dev agent chooses to be netstandard2.0-safe and FrontComposer-consumable (e.g. attributes + a static descriptor table, or `[Display]`-style metadata) and that is unit-testable.

3. **Rejection-event taxonomy (AR-6, FS-4).** An `IRejectionEvent` taxonomy is defined for the project rejection events: the rejection event contracts named in AR-6 — `ProjectCreationRejected`, `ProjectSetupUpdateRejected`, `ProjectArchiveRejected`, `ProjectReferenceLinkRejected`, `ProjectReferenceUnlinkRejected`, `ProjectResolutionConfirmationRejected` — exist under `src/Hexalith.Projects.Contracts/Events/` as past-tense, metadata-only records that implement `Hexalith.EventStore.Contracts.Events.IRejectionEvent`, each carrying a reason code **from the shared reason-code/reference-state vocabulary in AC-2** (no rejection-local enum, no free-text reason string as the canonical signal). Each rejection event is metadata-only (no setup body, no payload, no echoed sensitive value). (The matching success events and commands are **not** in scope here — they land with their command stories 1.4/1.7/1.8; only the rejection taxonomy + reason-code wiring is defined now so command stories test rejections as ACs, not as a trailing story.)

4. **Payload-classification allowlist / taxonomy (FS-1, NFR-2).** A payload-classification allowlist is authored in **both** a human-readable `docs/` form (e.g. `docs/payload-taxonomy.md`) **and** a machine-usable form in code (e.g. a static `PayloadClassification`/`SafeFieldAllowlist` descriptor under `Contracts`, or a structured data file the FS-2 harness can load). It enumerates: **reference-only / safe fields** (opaque IDs, ETags/versions, tenant ID, reference kind/owner-context, timestamps, lifecycle/inclusion/resolution states, reason codes, correlation/causation/audit IDs) **vs forbidden sibling-owned content** (conversation transcript text, file contents, memory bodies, raw prompts, secrets, raw tokens, full command bodies, unrestricted/local file paths, and folder names where they carry sensitive content). It is explicitly marked as the **single source of truth for the FS-2 `NoPayloadLeakage` harness** (built in Story 1.4) and is referenced by ID/path from the rejection events and shared models so leakage tests can assert against it.

5. **Canonical identity-derivation helper (AR-4, FS-3).** A pure identity-derivation helper is implemented (Contracts or domain-core, netstandard2.0-safe, no Dapr/network) that takes the canonical identity `{tenant}:projects:{projectId}` — domain segment sourced from the existing `ProjectsContractMetadata.DomainName` (`"projects"`) — and **derives every downstream key from it only**: actor IDs, state-store keys, projection keys, pub/sub topic names, SignalR group names, and log scopes. **AND** Tier-1 conformance tests assert that each derived key/topic/group/scope is a deterministic function of `(tenant, projectId)` **only** — never of a payload field, HTTP header, query parameter, or any non-canonical input — and that the same `(tenant, projectId)` always yields identical derived values (and different tenants/projects never collide).

6. **No compiler setting weakened; boundaries preserved; build + Tier-1 lane green.** All touched projects keep `net10.0`/`netstandard2.0`-safe targets, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true` — no relaxation, no `NoWarn`/`#pragma`/`SuppressMessage`. `Contracts` stays low-dependency (its only new external references are `Hexalith.EventStore.Contracts` for `IRejectionEvent`/`IEventPayload` and `Hexalith.FrontComposer.Contracts` for `[ProjectionBadge]`/`BadgeSlot` — both netstandard2.0-safe; **no Dapr, no HTTP, no EventStore.Server, no Redis/broker**). `dotnet build Hexalith.Projects.slnx` and the filtered Tier-1 + Contracts test lane pass green with zero warnings.

## Tasks / Subtasks

- [x] **Task 1 — Wire the two contract dependencies (central package management)** (AC: 1, 2, 3, 6)
  - [x] Add `Hexalith.FrontComposer.Contracts` reference to `src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj` using the sibling root-detection pattern (`$(HexalithFrontComposerRoot)`) established in Story 1.1's root `Directory.Build.props` — a `<ProjectReference>` to `...\Hexalith.FrontComposer\src\Hexalith.FrontComposer.Contracts\Hexalith.FrontComposer.Contracts.csproj` (it is a published package id `Hexalith.FrontComposer.Contracts`, multi-targets `net10.0;netstandard2.0`). This brings `[ProjectionBadge]` + `BadgeSlot`. Mirror the sample reference style in `Hexalith.FrontComposer/samples/Counter/Counter.Specimens.Domain`.
  - [x] Add `Hexalith.EventStore.Contracts` reference to `Contracts.csproj` (via `$(HexalithEventStoreRoot)`) for `IRejectionEvent` / `IEventPayload`.
  - [x] If either reference must be expressed as a `PackageReference` instead of a `ProjectReference` in the umbrella build, add the version to the root `Directory.Packages.props` **reusing the exact pinned version from the sibling** — never inline, never invented. Prefer `ProjectReference` (consistent with Story 1.1's self-contained `submodules:false` build) unless a project reference does not resolve. (Both wired as `ProjectReference`; no PackageReference needed.)
  - [x] Confirm `Contracts` still compiles netstandard2.0-safe and warnings-clean after adding references.

- [x] **Task 2 — `ProjectId` identifier value object** (AC: 1)
  - [x] Create `src/Hexalith.Projects.Contracts/Identifiers/ProjectId.cs`: a `sealed record ProjectId` with eager validation in the constructor/factory (throw `ArgumentException`/`ArgumentNullException` on null/empty/whitespace; define and enforce the value-shape rule — accept any non-whitespace string per EventStore `AggregateIdentity` rules; **do not** use `Guid.TryParse` to validate — Projects/EventStore identifiers are ULID-shaped strings, see EventStore retro rule R2-A7).
  - [x] Create `src/Hexalith.Projects.Contracts/Identifiers/ProjectIdJsonConverter.cs`: a `System.Text.Json.Serialization.JsonConverter<ProjectId>` that reads/writes the opaque string value; annotate `ProjectId` with `[JsonConverter(typeof(ProjectIdJsonConverter))]`.
  - [x] Do **not** create `ConversationId`/`FolderId`/`FileId`/`MemoryId` VOs. Add a short ADR stub `docs/adr/identifier-boundary.md` (or a Dev-Note-referenced section) recording AR-7: sibling references are held as `string` (ULID) reference IDs in reference descriptors, reusing the owning context's representation; Projects owns only `ProjectId`.

- [x] **Task 3 — Shared `[ProjectionBadge]` vocabulary enums** (AC: 2)
  - [x] Under `src/Hexalith.Projects.Contracts/Ui/`, create the four shared enums (one file each or grouped): `ProjectLifecycle` (`Active`, `Archived`); `ReferenceState` (the inclusion/reference states list in AC-2); `ResolutionResult` (`NoMatch`, `SingleCandidate`, `MultipleCandidates`); `ProjectReasonCode` (`ConversationLinked`, `ProjectFolderMatched`, `FileReferenceMatched`, `MemoryMatched`, `MetadataMatched`). Use PascalCase member names; the **enum member name is the stable wire code**.
  - [x] Annotate each member with `[ProjectionBadge(BadgeSlot.X)]` (slot per severity: `Active`/`Included`→`Success`; `Stale`/`Ambiguous`/`Archived`→`Warning`; `Unauthorized`/`Unavailable`/`TenantMismatch`/`Conflict`/`InvalidReference`/`Excluded`→`Danger` or `Neutral`/`Danger` as appropriate; explanatory→`Info`). Follow the `SpecimenBadgeState` example in `Hexalith.FrontComposer/samples/Counter/.../SpecimenStatusProjection.cs`.
  - [x] Apply `[JsonConverter(typeof(JsonStringEnumConverter<TEnum>))]` to each enum so the wire shape is the **name**, not the ordinal (mirror `Hexalith.Folders/src/Hexalith.Folders/Aggregates/Folder/FolderResultCode.cs`).
  - [x] Provide the stable **code + display label + accessible name + severity** metadata in a netstandard2.0-safe, FrontComposer-consumable, unit-testable way (attribute-driven descriptor table or `[Display]`-style metadata + a static lookup). Ensure the lookup covers every member of every enum (a Tier-1 test enforces total coverage).

- [x] **Task 4 — Rejection-event taxonomy** (AC: 3)
  - [x] Under `src/Hexalith.Projects.Contracts/Events/`, create the six rejection event records from AR-6 (`ProjectCreationRejected`, `ProjectSetupUpdateRejected`, `ProjectArchiveRejected`, `ProjectReferenceLinkRejected`, `ProjectReferenceUnlinkRejected`, `ProjectResolutionConfirmationRejected`) as `sealed record`s implementing `IRejectionEvent`. Names are past-tense, no `Event` suffix (per project naming rules).
  - [x] Each rejection carries the canonical reason via a member typed as the shared `ProjectReasonCode`/`ReferenceState` vocabulary (AC-2) — **not** a rejection-local enum or a free-text reason as the canonical signal. Carry only metadata-safe fields (e.g. `ProjectId`/tenant where applicable, reason code, optional safe `field` name that names a rejected field **without echoing its value**, correlation ID). No setup body, no secrets, no payload. (Reason typed as the shared `ReferenceState` enum — it carries the rejection-relevant codes `Unauthorized`/`TenantMismatch`/`Conflict`/`InvalidReference`/etc.; `ProjectReasonCode` holds positive match reasons.)
  - [x] Add XML doc to each rejection event noting its sensitivity class is metadata-only and that command stories (1.4/1.7/1.8) will emit them via `Handle`. Do **not** add success events or commands here.

- [x] **Task 5 — Payload-classification allowlist (docs + machine form)** (AC: 4)
  - [x] Author `docs/payload-taxonomy.md`: a table of **safe / reference-only fields** vs **forbidden sibling-owned content**, with the rationale that ProjectContext assembly (Epic 3) and every `NoPayloadLeakage` test derive from it. Explicitly list forbidden content: conversation transcript text, file contents, memory bodies, raw prompts, secrets, raw tokens, full command bodies, unrestricted/local paths, sensitive folder names.
  - [x] Provide a machine-usable form under `Contracts` (e.g. `Models/PayloadClassification.cs` exposing the safe-field allowlist + forbidden-field denylist as static collections, netstandard2.0-safe) so the FS-2 harness (Story 1.4) can assert against it programmatically. Reference the doc by relative path in XML doc.
  - [x] State the source-of-truth relationship in both forms: "FS-2 `NoPayloadLeakage` harness (Story 1.4) is built against this allowlist."

- [x] **Task 6 — Canonical identity-derivation helper** (AC: 5)
  - [x] Implement a pure, netstandard2.0-safe helper (choose location: `Contracts/Identifiers/ProjectIdentity.cs` if Contracts-only, or domain-core `src/Hexalith.Projects/...` if it needs domain types — prefer Contracts so projections/ACLs/workers can all consume it without a domain dependency) that, given `(tenant, ProjectId)`, builds the canonical aggregate global id `{tenant}:projects:{projectId}` using `ProjectsContractMetadata.DomainName` for the middle segment, and derives: actor id, state-store key, each projection key, pub/sub topic name(s), SignalR group name, and log scope — each as a deterministic string function of `(tenant, projectId)` only. (Placed in `Contracts/Identifiers/ProjectIdentity.cs`.)
  - [x] No Dapr/network/EventStore.Server dependency in the helper. If an EventStore identity primitive (e.g. an `AggregateKey`/`{tenant}:{domain}:{aggregateId}` builder) already exists in `Hexalith.EventStore.Contracts`, prefer wrapping/reusing it over re-implementing the format — verify before authoring (none was found by name in EventStore.Contracts during story prep; confirm and reuse if present). (FOUND and REUSED: `Hexalith.EventStore.Contracts.Identity.AggregateIdentity` derives actor id / state-store keys / pub-sub topic from `{tenant}:{domain}:{aggregateId}`; `ProjectIdentity` wraps it and layers projection-key / SignalR-group / log-scope.)
  - [x] Validate inputs (reject empty/whitespace tenant; require a valid `ProjectId`).

- [x] **Task 7 — Tier-1 tests + green lane** (AC: 1, 2, 3, 5, 6)
  - [x] In `tests/Hexalith.Projects.Contracts.Tests` (xUnit v3 + Shouldly): `ProjectId` eager-validation tests (throws on null/empty/whitespace; accepts valid; equality/record semantics) and JSON round-trip tests (serializes as opaque string, deserializes back, rejects malformed). Vocabulary tests: every enum serializes/deserializes by **name**; the descriptor lookup returns code+label+accessible-name+severity for **every** member of **every** enum (total-coverage assertion); no duplicate codes across the reason-code/state space where uniqueness is required. Rejection-event tests: each implements `IRejectionEvent`; reason member is the shared enum type; metadata-only shape.
  - [x] In `tests/Hexalith.Projects.Tests` (Tier-1) or `Contracts.Tests` (wherever the helper lives): identity-derivation conformance tests — every derived key/topic/group/scope is produced solely from `(tenant, projectId)`; determinism (same inputs → identical outputs); isolation (different tenant or different projectId → different derived values, no collision); rejects invalid inputs. Keep all tests **pure** (no Dapr/Aspire/network/containers/browser). (Helper lives in Contracts, so conformance tests live in `Contracts.Tests/Identifiers/ProjectIdentityTests.cs`.)
  - [x] Run `dotnet build Hexalith.Projects.slnx` (0 warnings) and the filtered Tier-1 + Contracts lane (green). Add new package versions, if any, only via central `Directory.Packages.props`. (Build 0W/0E; full `dotnet test Hexalith.Projects.slnx` green: 106 passed. Bumped `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.0→10.0.3 centrally to resolve the FluentUI-transitive NU1605 downgrade.)

## Dev Notes

### What Story 1.1 left you (verified scaffold reality)
Story 1.1 (status `done`) scaffolded the module **in-place at the umbrella root** (this repo IS the Projects module; it is not a submodule). The relevant landing zones already exist as **empty, `.gitkeep`-only** folders — fill them, do not recreate them:
- `src/Hexalith.Projects.Contracts/Identifiers/` ← `ProjectId` + converter (Task 2)
- `src/Hexalith.Projects.Contracts/Ui/` ← shared `[ProjectionBadge]` enums (Task 3)
- `src/Hexalith.Projects.Contracts/Events/` ← rejection events (Task 4)
- `src/Hexalith.Projects.Contracts/Models/` ← payload-classification machine form (Task 5)
- core `src/Hexalith.Projects/` partial-class folders exist (`Aggregates/Project`, `Projections`, etc.) — **do not** add aggregate/projection logic in this story.

`src/Hexalith.Projects.Contracts/ProjectsContractMetadata.cs` already exists and exposes `public static string DomainName => "projects";` — **this is the authoritative domain segment** for the identity helper (Task 6). Do not hardcode `"projects"` elsewhere; derive from `ProjectsContractMetadata.DomainName`.

`Directory.Packages.props` is **self-contained** (Story 1.1 copied versions verbatim from `Hexalith.Folders`). It currently pins only test packages + `Microsoft.Extensions.DependencyInjection.Abstractions 10.0.0`. Sibling references in Story 1.1 csprojs were intentionally **NONE** (self-contained `submodules:false` build). This story introduces the **first** sibling references (`Hexalith.EventStore.Contracts`, `Hexalith.FrontComposer.Contracts`) — use the `$(Hexalith*Root)` root-detection properties Story 1.1 already defined in the root `Directory.Build.props`. Keep the CI build resolvable (siblings present at umbrella root).

### Concrete sibling patterns to copy (do NOT reinvent)
- **`[ProjectionBadge]` attribute** lives at `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Contracts/Attributes/ProjectionBadgeAttribute.cs` — `[AttributeUsage(AttributeTargets.Field)]`, ctor takes a `BadgeSlot`. `BadgeSlot` (`Hexalith.FrontComposer.Contracts/Attributes/BadgeSlot.cs`) has members: `Neutral, Info, Success, Warning, Danger, Accent`. The Contracts assembly is `net10.0;netstandard2.0` and is a real published package (`Hexalith.FrontComposer.Contracts`).
- **Working `[ProjectionBadge]` enum example:** `Hexalith.FrontComposer/samples/Counter/Counter.Specimens.Domain/SpecimenStatusProjection.cs` (`SpecimenBadgeState` enum — each member annotated `[ProjectionBadge(BadgeSlot.X)]`; the projection class itself uses `[Projection]`/`[BoundedContext]`). Mirror the enum annotation style; **do not** add `[Projection]` projection classes in this story (those land with the projection/surface stories).
- **Name-based-JSON enum example:** `Hexalith.Folders/src/Hexalith.Folders/Aggregates/Folder/FolderResultCode.cs` — `[JsonConverter(typeof(JsonStringEnumConverter<FolderResultCode>))]` with the explicit comment "Wire shape must serialize the enum NAME ... keeps the contract stable when members are inserted, renamed, or renumbered." Apply the same to all four Projects vocabulary enums (NFR-6 schema tolerance: additive, no ordinal coupling).
- **`IRejectionEvent`** is at `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Events/IRejectionEvent.cs` — `public interface IRejectionEvent : IEventPayload;` (marker, no members). `IEventPayload` is the base marker for all event payloads (`.../Events/IEventPayload.cs`). Rejection events implement `IRejectionEvent`; metadata-only.

### Identifier reality check — AR-7 nuance (prevents an ID-drift mistake)
AR-7 says "reuse the owning context's Contracts identifier types (e.g. `ConversationId`, Folders `FolderId`/`FileId`, Memories ids) rather than minting parallel VOs." **Verified caveat:** the sibling Contracts/OpenAPI surfaces (`Hexalith.Folders`, `Hexalith.Conversations`) currently expose their IDs as **plain `string`/ULID**, not as dedicated `FolderId`/`ConversationId` value objects in their Contracts packages (grep found `FolderId` only in the Folders OpenAPI yaml as a string schema; `ConversationId` appears in tests, not as a Contracts VO). So the **correct application of AR-7 in this story** is: Projects mints exactly **one** VO — `ProjectId` — and represents all sibling references as `string` (ULID) reference IDs inside reference descriptors, reusing the sibling's own representation. Do **not** invent `ConversationId`/`FolderId`/`FileId`/`MemoryId` VOs inside Projects (that would itself be the ID-drift AR-7 warns against). Record this decision in `docs/adr/identifier-boundary.md`.

### Architecture compliance (guardrails — enforced fully here for the vocabulary, partially for the rest)
- **One vocabulary, no parallels** [architecture.md#Pattern Enforcement, AR-18, UX-DR5]: "Use the shared state/reason-code enums; never introduce parallel vocabularies or magic strings." This story IS that single source. A later story inventing a `ProjectStatus` enum that duplicates lifecycle is the explicit forbidden anti-pattern (architecture.md#Anti-patterns). Make the shared enums the only place these states/codes are spelled.
- **Metadata-only everywhere** [NFR-2, architecture.md#Anti-patterns]: events/logs/DTOs/audit are metadata-only. Rejection events here must carry no setup body, folder path, secret, token, or correlation token value beyond safe IDs. The payload taxonomy (Task 5) is the boundary definition the FS-2 harness enforces.
- **Identity discipline** [AR-4, FS-3, architecture.md#L444]: "Derive all keys/topics/groups/log scopes from canonical `{tenant}:projects:{projectId}`." The helper is the only place this format is built; everything downstream calls it. Tenant authority is from claims (Story 1.6) — but the *identity string format* is fixed here.
- **`netstandard2.0`-safe Contracts** [Story 1.1 Dev Notes, architecture.md#Decisions Provided by Starter]: contract types feed FrontComposer source generators, so keep `ProjectId`, the enums, the descriptor lookup, the payload classification, and the identity helper (if placed in Contracts) netstandard2.0-compatible. Both new sibling refs (`EventStore.Contracts`, `FrontComposer.Contracts`) multi-target including netstandard2.0, so this is achievable.
- **Boundary direction** [project-context.md#Code Quality]: `Contracts` stays low-dependency. The only acceptable new dependencies are the two netstandard2.0-safe `*.Contracts` siblings above. No Dapr, no HTTP, no `EventStore.Server`, no Redis/Postgres/Cosmos/broker — adding any of those to `Contracts` is a hard violation.

### Library / framework requirements
- **.NET 10** / `netstandard2.0`-safe Contracts; nullable + implicit usings + warnings-as-errors; `LangVersion latest`; `sealed` records preferred. Central Package Management only — **no inline `Version=`**; reuse sibling-pinned versions verbatim.
- **`System.Text.Json`** only (no Newtonsoft) — matches EventStore/Tenants/Parties/Folders contract convention. Use `JsonConverter<T>` for `ProjectId` and `JsonStringEnumConverter<TEnum>` for the vocabulary enums.
- **xUnit v3 + Shouldly** for tests (match EventStore/Tenants/Folders; versions already pinned: `xunit.v3` 3.2.2, `Shouldly` 4.3.0). Do **not** use xUnit v2 (that is the Parties pattern — not Projects' pattern).
- **Do not** add/upgrade Fluent UI, Dapr, Aspire, Roslyn (`Microsoft.CodeAnalysis.CSharp` pinned `4.12.0` ecosystem-wide), Fluxor, or the SDK. Reference only the two `*.Contracts` siblings + what is already pinned.
- **Identifier validation rule (EventStore retro R2-A7):** never validate `ProjectId`/aggregate IDs with `Guid.TryParse` — they are ULID-shaped strings; accept any non-whitespace string per `AggregateIdentity` rules (use `Ulid.TryParse` only if you genuinely require ULID shape, otherwise non-whitespace string validation).

### File / structure requirements (target — fills the Story 1.1 skeleton)
```text
src/Hexalith.Projects.Contracts/
├── ProjectsContractMetadata.cs                 # EXISTS — DomainName => "projects" (reuse)
├── Identifiers/
│   ├── ProjectId.cs                            # NEW — sealed record VO, eager validation, [JsonConverter]
│   ├── ProjectIdJsonConverter.cs               # NEW — JsonConverter<ProjectId>
│   └── ProjectIdentity.cs                       # NEW (if Contracts) — canonical {tenant}:projects:{projectId} derivation
├── Ui/
│   ├── ProjectLifecycle.cs                     # NEW — Active, Archived ([ProjectionBadge], name-JSON)
│   ├── ReferenceState.cs                       # NEW — Included..InvalidReference
│   ├── ResolutionResult.cs                     # NEW — NoMatch, SingleCandidate, MultipleCandidates
│   ├── ProjectReasonCode.cs                    # NEW — ConversationLinked..MetadataMatched
│   └── ProjectVocabularyDescriptors.cs         # NEW — code+label+accessibleName+severity lookup
├── Events/
│   ├── ProjectCreationRejected.cs              # NEW — : IRejectionEvent, reason from shared enum
│   ├── ProjectSetupUpdateRejected.cs           # NEW
│   ├── ProjectArchiveRejected.cs               # NEW
│   ├── ProjectReferenceLinkRejected.cs         # NEW
│   ├── ProjectReferenceUnlinkRejected.cs       # NEW
│   └── ProjectResolutionConfirmationRejected.cs# NEW
└── Models/
    └── PayloadClassification.cs                # NEW — machine-usable safe/forbidden field allowlist
docs/
├── payload-taxonomy.md                         # NEW — human-readable allowlist (FS-1)
└── adr/identifier-boundary.md                  # NEW — AR-7 decision (ProjectId only; siblings as string)
tests/Hexalith.Projects.Contracts.Tests/        # ProjectId, vocabulary, rejection, descriptor tests
tests/Hexalith.Projects.Tests/                  # identity-derivation conformance (if helper in domain-core)
```
- File-scoped namespaces under `Hexalith.Projects.*` matching folder path. 4-space indent, **CRLF**, UTF-8, final newline (Story 1.1's review found and fixed an LF violation — write CRLF from the start). Private fields `_camelCase`; interfaces `I`-prefixed; `Async` suffix on async methods; prefer `sealed`.
- Command records: imperative, no `Command` suffix. Event records: past tense, no `Event` suffix (the six rejections follow this — `*Rejected`).

### Testing requirements
- Keep `Contracts.Tests` and Tier-1 `Hexalith.Projects.Tests` **pure and fast** — no Dapr/Aspire/network/containers/browser (project-context.md#Testing Rules).
- Use Shouldly assertions; use `Hexalith.EventStore.Testing` / `Hexalith.Tenants.Testing` fakes/builders **before inventing doubles** — though this story is pure contracts + one pure helper, it likely needs no external doubles at all.
- **Mandatory negative-path coverage** (project-context.md): vocabulary/identity/payload behavior needs negative tests, not just happy path — `ProjectId` rejects bad input; identity helper rejects empty tenant; enums reject ordinal-only round-trips (assert name-based JSON); descriptor lookup is total (no member without metadata).
- **Schema-evolution intent** (NFR-6, FS-5): name-based enum JSON + additive records means a future member insert/rename does not break the wire — a Tier-1 test asserting name-based serialization protects this. (The frozen golden-file corpus itself lands in Story 1.4 against the first real success event; here, just enforce name-based JSON so the corpus stays stable.)
- Determinism: identity-derivation tests must be deterministic (no wall-clock, no random) — same `(tenant, projectId)` → identical derived strings.

### Project Structure Notes
- **Alignment:** Targets match architecture.md#Complete Project Directory Structure exactly (`Identifiers/`=`ProjectId (+ JSON converter)`; `Ui/`=`SHARED state/reason-code enums ([ProjectionBadge])`; `Events/`=`... (+ rejections)`; `Models/`=safe DTOs). No structural deviation expected.
- **Decision the dev agent must make + document:** where the identity-derivation helper lives (Contracts vs domain-core). **Recommendation:** Contracts (netstandard2.0-safe) so projections, ACLs, and Workers can all derive keys without taking a domain dependency, and so the FrontComposer-facing surface can reference it. Document the choice in the Dev Agent Record.
- **Decision the dev agent must make + document:** whether the two sibling contracts come in as `ProjectReference` (preferred, matches Story 1.1's self-contained build) or `PackageReference` (only if project refs don't resolve in the umbrella/CI build) — note which and why.
- **Variance to flag:** AR-7's literal "`ConversationId`/`FolderId` VO reuse" vs the sibling reality (string/ULID IDs) — resolved as "`ProjectId` only; siblings as `string`" with an ADR. Flag in Dev Agent Record so a reviewer expecting sibling VOs isn't surprised.
- **Submodule discipline:** root-level submodules only; never `--recursive`; never modify sibling submodule pointers. All new files live under the umbrella-root Projects-module paths (`src/`, `docs/`, `tests/`).

### References
- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.2: Shared vocabulary, identifiers & payload taxonomy] — the four BDD acceptance criteria (ProjectId VO; shared `[ProjectionBadge]` enums + rejection taxonomy; payload allowlist; identity-derivation helper) this story implements.
- [Source: _bmad-output/planning-artifacts/epics.md#Additional Requirements] — AR-2 (contracts-before-code), AR-4 (canonical identity `{tenant}:projects:{projectId}`, user-facing tenant as envelope tenant), AR-6 (event/rejection list + event-catalog rule), AR-7 (`ProjectId` VO; reuse sibling identifiers), AR-18 (single `[ProjectionBadge]` vocabulary, no parallel enums/magic strings).
- [Source: _bmad-output/planning-artifacts/epics.md#Cross-Cutting Foundational Slices (Epic 1)] — FS-1 (payload-classification taxonomy & allowlist — first content story of Epic 1), FS-3 (canonical identity-derivation helper with Tier-1 conformance tests), FS-4 (shared rejection-event & reason-code vocabulary defined once).
- [Source: _bmad-output/planning-artifacts/epics.md#UX Design Requirements] — UX-DR5 (status & reason-code pattern: stable code + display label + accessible name + severity mapping, declared once as `[ProjectionBadge]` shared enums).
- [Source: _bmad-output/planning-artifacts/architecture.md#Pattern Enforcement / Anti-patterns (L440-472)] — single vocabulary rule; derive all keys from canonical identity; metadata-only; forbidden parallel `ProjectStatus` enum; no payload/path/token logging.
- [Source: _bmad-output/planning-artifacts/architecture.md#Complete Project Directory Structure (L497-519)] — Contracts subfolder map (`Identifiers/`, `Ui/`, `Events/`, `Models/`).
- [Source: _bmad-output/project-context.md#Language-Specific Rules / Framework-Specific Rules / Critical Don't-Miss Rules] — past-tense events / `IRejectionEvent`; no `V2`; `System.Text.Json`; EventStore identity model; metadata-only logging; additive serialization-tolerant contracts; central package management.
- [Source: _bmad-output/implementation-artifacts/1-1-module-scaffold-build-ci-wiring.md] — scaffold reality: empty `.gitkeep` folders, self-contained `Directory.Packages.props`, `$(Hexalith*Root)` root-detection in root `Directory.Build.props`, NONE sibling refs yet, CRLF requirement (LF→CRLF review fix), `ProjectsContractMetadata.DomainName`.
- [Source: src/Hexalith.Projects.Contracts/ProjectsContractMetadata.cs] — existing `DomainName => "projects"` to reuse for the identity helper.
- [Source: Hexalith.FrontComposer/src/Hexalith.FrontComposer.Contracts/Attributes/ProjectionBadgeAttribute.cs] · [.../Attributes/BadgeSlot.cs] — the attribute + slot enum to apply.
- [Source: Hexalith.FrontComposer/samples/Counter/Counter.Specimens.Domain/SpecimenStatusProjection.cs] — working `[ProjectionBadge(BadgeSlot.X)]` enum annotation example + ProjectReference style.
- [Source: Hexalith.Folders/src/Hexalith.Folders/Aggregates/Folder/FolderResultCode.cs] — name-based `JsonStringEnumConverter<T>` pattern + rationale (members insertable/renamable without wire break).
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Events/IRejectionEvent.cs] · [.../Events/IEventPayload.cs] — interfaces the rejection events implement.
- [Source: Hexalith.EventStore/CLAUDE.md#ID validation rule (R2-A7)] — ULID, not GUID: never `Guid.TryParse` aggregate/identifier fields.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.7 (1M context) — claude-opus-4-7[1m]

### Debug Log References

- `dotnet build src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj` → 0W/0E (both sibling `ProjectReference`s resolve via `$(Hexalith*Root)`).
- `dotnet build Hexalith.Projects.slnx` initially FAILED with NU1605: `Hexalith.Projects` (net10.0 app graph) → `Contracts` → `FrontComposer.Contracts` → `Microsoft.FluentUI.AspNetCore.Components` (pinned RC) → `Microsoft.Extensions.Hosting.Abstractions 10.0.3` → `DI.Abstractions (>= 10.0.3)`, downgraded by the direct `DI.Abstractions` 10.0.0 pin. Resolved by bumping the central pin to 10.0.3 (non-protected Microsoft.Extensions abstraction; within the ecosystem 10.0.x line — sibling Folders runs Hosting.Abstractions at 10.0.5). Re-build → 0W/0E.
- `dotnet test Hexalith.Projects.slnx --no-build` → 106 passed / 0 failed / 0 skipped (Contracts.Tests 101, Tests 2, Server.Tests 2, Integration.Tests 1).

### Completion Notes List

- **Sibling Contracts wiring (Task 1):** Both `Hexalith.EventStore.Contracts` and `Hexalith.FrontComposer.Contracts` added to `Hexalith.Projects.Contracts.csproj` as `ProjectReference`s using the `$(HexalithEventStoreRoot)` / `$(HexalithFrontComposerRoot)` root-detection properties from the umbrella-root `Directory.Build.props` (Story 1.1). No `PackageReference` was needed — project refs resolve cleanly. These are the first sibling references in the module.
- **`ProjectId` (Task 2):** `sealed record` with eager constructor validation (throws `ArgumentNullException` on null, `ArgumentException` on empty/whitespace), opaque `Value` property, `[JsonConverter(typeof(ProjectIdJsonConverter))]`. The converter serializes as a plain JSON string and re-validates on read (rejects non-string tokens / empty values with `JsonException`). No `Guid.TryParse` — ULID-shaped non-whitespace string rule per EventStore R2-A7.
- **AR-7 boundary decision:** Only `ProjectId` is minted; sibling references are held as `string` (ULID). Recorded in `docs/adr/identifier-boundary.md`. **Variance flagged for reviewer:** AR-7's literal "reuse `ConversationId`/`FolderId` VOs" is not achievable — those VOs do not exist in the sibling Contracts packages (they expose IDs as plain `string`/ULID), so importing/recreating them would itself be the ID-drift AR-7 warns against.
- **Shared vocabulary (Task 3):** Four enums under `Ui/` — `ProjectLifecycle`, `ReferenceState`, `ResolutionResult`, `ProjectReasonCode` — each `[JsonConverter(typeof(JsonStringEnumConverter<TEnum>))]` (name-based wire) and each member `[ProjectionBadge(BadgeSlot.X)]`. `ProjectVocabularyDescriptors` is the single lookup supplying code + display label + accessible name + severity for every member; severity is read from the `[ProjectionBadge]` attribute via reflection (declared once on the enum), labels/accessible-names from static tables. `BuildDescriptors` throws if any member lacks metadata, and a Tier-1 test asserts total coverage.
- **Rejection taxonomy (Task 4):** Six `sealed record`s implementing `Hexalith.EventStore.Contracts.Events.IRejectionEvent`, past-tense `*Rejected` names, metadata-only (ProjectId/tenant/`ReferenceState` reason/optional safe field NAME/correlation id). Canonical reason is the shared `ReferenceState` enum (carries the rejection-relevant codes), not a rejection-local enum or free-text. XML docs mark sensitivity class metadata-only and defer emission to command stories 1.4/1.7/1.8.
- **Payload taxonomy (Task 5):** `docs/payload-taxonomy.md` (human) + `Models/PayloadClassification.cs` (machine: `SafeFields` allowlist + `ForbiddenContent` denylist, `IsSafe`/`IsForbidden`, `TaxonomyDocumentPath`, `SourceOfTruthStatement`). Both forms state the FS-2 `NoPayloadLeakage` (Story 1.4) source-of-truth relationship.
- **Identity helper (Task 6):** `Contracts/Identifiers/ProjectIdentity.cs` — placed in Contracts (netstandard2.0-safe) so projections/ACLs/workers consume it without a domain dependency. **Reuses** `Hexalith.EventStore.Contracts.Identity.AggregateIdentity` (an existing primitive found during implementation) for actor id / state-store keys / pub-sub topic from `{tenant}:projects:{projectId}` (domain from `ProjectsContractMetadata.DomainName`), and layers Projects-specific `ProjectionKey(name)` / `SignalRGroup` / `LogScope`. All derivations are deterministic functions of `(tenant, projectId)` only. Note: `AggregateIdentity` lowercases the tenant; `PubSubTopic` is `{tenant}.projects.events` (tenant-scoped by design, not project-scoped).
- **Tests (Task 7):** 99 new Tier-1 tests in `Contracts.Tests` (ProjectId, vocabulary name-based JSON + total descriptor coverage + uniqueness, rejection taxonomy, identity-derivation conformance/determinism/isolation/negative inputs, payload classification). All pure (no Dapr/Aspire/network/containers/browser). Added `ArgumentNullException.ThrowIfNull` guards in theory methods to satisfy CA1062 under warnings-as-errors.
- **Line endings:** All new files written/normalized to CRLF + UTF-8 (no BOM) + final newline, per `.editorconfig` and the Story 1.1 review fix.

### File List

- `Directory.Packages.props` (modified — bumped `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.0 → 10.0.3 to resolve transitive NU1605)
- `src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj` (modified — added two sibling `ProjectReference`s)
- `src/Hexalith.Projects.Contracts/Identifiers/ProjectId.cs` (new)
- `src/Hexalith.Projects.Contracts/Identifiers/ProjectIdJsonConverter.cs` (new)
- `src/Hexalith.Projects.Contracts/Identifiers/ProjectIdentity.cs` (new)
- `src/Hexalith.Projects.Contracts/Ui/ProjectLifecycle.cs` (new)
- `src/Hexalith.Projects.Contracts/Ui/ReferenceState.cs` (new)
- `src/Hexalith.Projects.Contracts/Ui/ResolutionResult.cs` (new)
- `src/Hexalith.Projects.Contracts/Ui/ProjectReasonCode.cs` (new)
- `src/Hexalith.Projects.Contracts/Ui/VocabularyDescriptor.cs` (new)
- `src/Hexalith.Projects.Contracts/Ui/ProjectVocabularyDescriptors.cs` (new)
- `src/Hexalith.Projects.Contracts/Events/ProjectCreationRejected.cs` (new)
- `src/Hexalith.Projects.Contracts/Events/ProjectSetupUpdateRejected.cs` (new)
- `src/Hexalith.Projects.Contracts/Events/ProjectArchiveRejected.cs` (new)
- `src/Hexalith.Projects.Contracts/Events/ProjectReferenceLinkRejected.cs` (new)
- `src/Hexalith.Projects.Contracts/Events/ProjectReferenceUnlinkRejected.cs` (new)
- `src/Hexalith.Projects.Contracts/Events/ProjectResolutionConfirmationRejected.cs` (new)
- `src/Hexalith.Projects.Contracts/Models/PayloadClassification.cs` (new)
- `docs/payload-taxonomy.md` (new)
- `docs/adr/identifier-boundary.md` (new)
- `tests/Hexalith.Projects.Contracts.Tests/Identifiers/ProjectIdTests.cs` (new)
- `tests/Hexalith.Projects.Contracts.Tests/Identifiers/ProjectIdentityTests.cs` (new)
- `tests/Hexalith.Projects.Contracts.Tests/Ui/ProjectVocabularyTests.cs` (new)
- `tests/Hexalith.Projects.Contracts.Tests/Events/RejectionEventTaxonomyTests.cs` (new)
- `tests/Hexalith.Projects.Contracts.Tests/Models/PayloadClassificationTests.cs` (new)

## Senior Developer Review (AI)

- Reviewer: Jérôme Piquot (automated story-automator-review)
- Date: 2026-05-25
- Outcome: **Approve** (0 CRITICAL, 0 HIGH, 0 MEDIUM; 2 non-blocking LOW observations)

### AC verification (all satisfied on disk)

1. **AC-1 ProjectId VO + JSON converter** — `Identifiers/ProjectId.cs` is a `sealed record` with eager constructor validation (`ArgumentNullException` on null, `ArgumentException` on empty/whitespace), opaque read-only `Value`, `[JsonConverter(typeof(ProjectIdJsonConverter))]`. Converter serializes a plain string, re-validates on read, rejects non-string/object tokens with `JsonException`. No `Guid.TryParse` (R2-A7). ADR `docs/adr/identifier-boundary.md` records the "ProjectId only; siblings as string" decision. SATISFIED.
2. **AC-2 single `[ProjectionBadge]` vocabulary** — Exactly four enums under `Ui/` (`ProjectLifecycle`, `ReferenceState`, `ResolutionResult`, `ProjectReasonCode`); `ReferenceState` carries all 10 required members; every member has `[ProjectionBadge(BadgeSlot.X)]` and each enum has `[JsonConverter(typeof(JsonStringEnumConverter<T>))]` (name-based wire). `ProjectVocabularyDescriptors` supplies code+label+accessibleName+severity for every member, severity read from the attribute via reflection; `BuildDescriptors` throws on any uncovered member and a Tier-1 test asserts total coverage. netstandard2.0-safe. SATISFIED.
3. **AC-3 rejection taxonomy** — Six `sealed record`s under `Events/` implementing `Hexalith.EventStore.Contracts.Events.IRejectionEvent`, past-tense `*Rejected`, metadata-only (ProjectId/tenant/`ReferenceState` reason/optional safe field NAME/correlation id). Canonical reason typed as the shared `ReferenceState` enum (carries `Unauthorized`/`TenantMismatch`/`Conflict`/`InvalidReference`), not a rejection-local enum or free-text. SATISFIED.
4. **AC-4 payload allowlist (docs + machine)** — `docs/payload-taxonomy.md` (human, with safe/forbidden tables + rationale) and `Models/PayloadClassification.cs` (machine: `SafeFields`/`ForbiddenContent` + `IsSafe`/`IsForbidden` + `TaxonomyDocumentPath` + `SourceOfTruthStatement`). Both state the FS-2 `NoPayloadLeakage` source-of-truth relationship. SATISFIED.
5. **AC-5 canonical identity helper + conformance tests** — `Identifiers/ProjectIdentity.cs` wraps EventStore `AggregateIdentity` (domain from `ProjectsContractMetadata.DomainName`) and layers projection-key / SignalR-group / log-scope, all deterministic functions of `(tenant, projectId)` only. Conformance tests cover determinism, tenant/project isolation, and invalid-input rejection. SATISFIED.
6. **AC-6 no compiler weakening; boundaries; green lane** — No `NoWarn`/`#pragma`/`SuppressMessage`/nullable-disable anywhere in Contracts; no inline `Version=` in any csproj (central management intact). Contracts' only new refs are the two netstandard2.0-safe `*.Contracts` siblings. Build 0W/0E; 106 tests pass. SATISFIED.

### Flagged-item assessment

- **(a) Sibling `ProjectReference`s** — `Projects.Contracts → EventStore.Contracts + FrontComposer.Contracts` via `$(Hexalith*Root)` root-detection in the umbrella-root `Directory.Build.props` (probes both umbrella-root and submodule layouts via `Exists()`; no recursive init; no sibling pointer changes). Both targets are Contracts-tier (marker interfaces + attributes/enums), no Dapr/HTTP/Server/Redis/broker — boundary direction (Contracts staying low-dependency) is respected. The FluentUI transitive pull from `FrontComposer.Contracts` is **net10.0-only** (guarded `Condition="'$(TargetFramework)' == 'net10.0'"`) and is the canonical home of `[ProjectionBadge]`/`BadgeSlot` the story mandates — the dev had no lower-dependency alternative. ACCEPTED.
- **(b) `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.0 → 10.0.3** — Reproduced the NU1605 by reverting to 10.0.0: `Hexalith.Projects → Contracts → FrontComposer.Contracts → FluentUI (RC) → Hosting.Abstractions 10.0.3 → DI.Abstractions (>= 10.0.3)` downgraded by the direct 10.0.0 pin. Bump is justified, minimal (exactly the transitive floor `>= 10.0.3`), within the 10.0.x patch line, and **not** a protected pin (DI.Abstractions is a base Microsoft.Extensions abstraction, not FluentUI/Dapr/Aspire/xUnit/Roslyn/SDK). ACCEPTED.

### LOW observations (non-blocking, no change made)

- LOW-1: The Dev Notes target tree shows the descriptor lookup as a single `ProjectVocabularyDescriptors.cs`, but the implementation correctly split the `VocabularyDescriptor` record into its own file (one-type-per-file, StyleCop-friendly) — accurately reflected in the File List. Cosmetic structure variance only.
- LOW-2: `ProjectIdentity` validates the tenant for null/whitespace before delegating to `AggregateIdentity`'s stricter canonical regex; a non-empty but non-canonical tenant still throws `ArgumentException` (just from the inner primitive). Behavior is correct; no change warranted.

### Verification commands

- `dotnet build Hexalith.Projects.slnx` → **0 Warning(s) / 0 Error(s)**.
- `dotnet test Hexalith.Projects.slnx --no-build` → **106 passed / 0 failed / 0 skipped** (Contracts.Tests 101, Tests 2, Server.Tests 2, Integration.Tests 1).
- NU1605 reproduction (revert→rebuild→restore) confirmed the DI.Abstractions bump is necessary and minimal.
- No sibling submodule files modified during review.

## Change Log

| Date | Change |
| --- | --- |
| 2026-05-25 | Implemented Story 1.2: `ProjectId` VO + JSON converter; single `[ProjectionBadge]` shared vocabulary (4 enums) + descriptor lookup; 6 `IRejectionEvent` rejection records; payload-classification allowlist (docs + machine form); canonical identity-derivation helper (`ProjectIdentity` wrapping EventStore `AggregateIdentity`); 99 Tier-1 tests. Wired first sibling Contracts `ProjectReference`s (EventStore.Contracts, FrontComposer.Contracts). Bumped `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.0→10.0.3 centrally. Build 0W/0E; full test suite 106 passed. Status → review. |
| 2026-05-25 | Senior Developer Review (AI, story-automator-review): adversarial review of all 6 ACs + 7 tasks against files on disk; both flagged items (sibling `ProjectReference` boundary; DI.Abstractions 10.0.3 bump) verified and accepted (NU1605 reproduced). 0 CRITICAL / 0 HIGH / 0 MEDIUM, 2 non-blocking LOW. Build 0W/0E; 106 tests pass; no sibling submodules modified. Outcome Approve → Status `done`. |
