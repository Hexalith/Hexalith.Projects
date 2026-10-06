---
title: 'Align Proposal Fingerprint Collection Parity'
type: 'bugfix'
created: '2026-08-27'
status: 'done'
baseline_revision: '5680229db1a53f34727f4b48776787e2d0791300'
review_loop_iteration: 0
followup_review_recommended: false
context: []
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** Proposal confirmation accepts `fileReferenceIds` independent of caller order and treats `null` as an empty collection, but the generated `ConfirmNewProjectProposalRequest.ComputeIdempotencyHash()` hashes the raw nullable collection. Equivalent accepted requests can therefore disagree with the server fingerprint.

**Approach:** Declare the collection's canonicalization policy in the OpenAPI generator input, teach the idempotency-helper generator to emit ordinal sorting with null-to-empty normalization, regenerate the helper, and pin direct client/server parity for reversed, null, and empty collections.

## Boundaries & Constraints

**Always:** Preserve the existing server acceptance rules, ordinal comparer, duplicate rejection, field order, canonical JSON representation, and hashes for already canonical non-null arrays. Drive the client assertion through the real generated helper and the server assertion through the proposal-confirmation endpoint and capturing ledger.

**Block If:** Halt if the correction requires changing the public request schema, accepting duplicate/invalid IDs, changing collection semantics for another operation, or adding a compatibility path for deployed fingerprints without explicit evidence and authority.

**Never:** Hand-edit generated `.g.cs` output, change the general hasher to sort every array, weaken proposal validation, edit the deferred-work ledger or bundle intent, or treat `fileReferences` and `fileReferenceIds` as order-sensitive.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Reversed IDs | Same valid IDs in reverse ordinal order | Generated helper and server ledger produce the sorted-array fingerprint | No error expected |
| Null IDs | `fileReferenceIds: null` with no file references | Same fingerprint as an empty array on both surfaces | No error expected |
| Empty IDs | `fileReferenceIds: []` with no file references | Same fingerprint as null on both surfaces | No error expected |

</intent-contract>

## Code Map

- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml:1816` and `:4102` -- `ConfirmNewProjectProposal` declares `file_reference_ids` equivalence and the array schema; add the machine-readable field canonicalization input without changing wire shape.
- `src/Hexalith.Projects.Client/Generation/Program.cs:63` and `:158` -- builds field models from operation/schema metadata and emits each `IdempotencyField`; validate and translate the collection policy here.
- `src/Hexalith.Projects.Client/Generated/HexalithProjectsIdempotencyHelpers.g.cs:216` -- generated output only; regeneration must make `file_reference_ids` ordinal-sorted and null-to-empty before hashing.
- `src/Hexalith.Projects.Server/Queries/ProposeNewProjectEndpoint.cs:588`, `:700`, and `:737` -- validation already normalizes declared IDs with `SortedSet(StringComparer.Ordinal)` and hashes an ordered empty-or-populated array; preserve this as the server contract.
- `tests/Hexalith.Projects.Contracts.Tests/OpenApi/OpenApiContractSpineTests.cs:603` -- contract-spine assertions for proposal equivalence metadata and its collection policy.
- `tests/Hexalith.Projects.Client.Tests/ClientGenerationTests.cs:677` -- real generated-helper tests and artifact-currentness gate; add reversed-order and null/empty equivalence assertions.
- `tests/Hexalith.Projects.Server.Tests/Queries/ProposeNewProjectEndpointTests.cs:301` and `:902` -- endpoint/ledger parity fixtures; extend request and generated-request builders for ordered, reversed, null, and empty IDs.
- `_bmad-output/implementation-artifacts/deferred-work.md` and `.bmad-loop/runs/20260827-214032-0a36/bundles/proposal-fingerprint-collection-parity/intent.md` -- read-only orchestration evidence; do not modify.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` and `tests/Hexalith.Projects.Contracts.Tests/OpenApi/OpenApiContractSpineTests.cs` -- declare and pin a field-scoped ordinal-sort/null-to-empty idempotency canonicalization policy for proposal `fileReferenceIds`, retaining the existing schema.
- `src/Hexalith.Projects.Client/Generation/Program.cs` -- parse and fail closed on the supported collection policy, then emit normalization only for the annotated field; preserve ordinary array order elsewhere.
- `src/Hexalith.Projects.Client/Generated/HexalithProjectsIdempotencyHelpers.g.cs` -- regenerate from the spine and generator; do not edit manually.
- `tests/Hexalith.Projects.Client.Tests/ClientGenerationTests.cs` -- prove reversed arrays hash like sorted arrays and null hashes like empty through `ConfirmNewProjectProposalRequest.ComputeIdempotencyHash()`.
- `tests/Hexalith.Projects.Server.Tests/Queries/ProposeNewProjectEndpointTests.cs` -- compare the accepted endpoint ledger fingerprint directly with the generated helper for reversed order, null, and empty arrays.

**Acceptance Criteria:**
- Given two valid proposal-confirmation requests differing only in `fileReferenceIds` order, when the generated helper and server compute fingerprints, then all fingerprints equal the ordinal-sorted canonical fingerprint.
- Given otherwise equivalent accepted proposals with `fileReferenceIds` null and empty, when either surface computes the fingerprint, then both forms hash the same canonical empty JSON array.
- Given an unannotated array equivalence field, when helpers are generated, then its caller order remains unchanged rather than acquiring global collection sorting.
- Given the updated spine and generator, when generated-artifact verification runs, then checked-in outputs are current and contain no manual drift.

### Review Findings

Code review of commit `005c2d5` (baseline `5680229`) on 2026-10-06. Layers: Blind Hunter, Edge Case Hunter, Verification Gap, Acceptance Auditor. Acceptance Auditor reported: generator re-run is byte-identical to the checked-in helper (AC4); Contracts 194/194, ClientGeneration 101/101, ProposeNewProjectEndpoint 74/74 pass in Release.

- [x] [Review][Patch] (medium) Generator silently drops the `x-hexalith-idempotency-collection-canonicalization` annotation anywhere except a top-level body property — nested dotted fields (`bodyParts.Length > 1`), operation-parameter fields (early return at `:182`), annotations on a `$ref` target, annotated properties no equivalence list names, and a `oneOf` branch that redeclares the property last all bypass the policy check with no error, recreating the client/server drift this spec fixes. Fix: record every annotated node consumed by `ApplyCollectionCanonicalization`, scan the spine for all occurrences of the extension, and throw naming the location of any unconsumed one. [src/Hexalith.Projects.Client/Generation/Program.cs:202]
- [x] [Review][Patch] (medium) `ApplyCollectionCanonicalization` has no direct test: `UnannotatedArrayEquivalenceFieldPreservesCallerOrder` uses nested `project_setup.goals`, which never enters the function, so "sort every top-level array" or "infer from `uniqueItems`" would leave output and tests green; the unsupported-policy and non-array throw paths are never executed. Fix: move the policy decision (and the unconsumed-annotation check) into `Hexalith.Projects.Client.Generation.Shared` as `internal` (already `InternalsVisibleTo` `Hexalith.Projects.Client.Tests`, `IsPackable=false`) and unit-test with YAML fixtures: unannotated top-level array (incl. `uniqueItems: true`) unchanged; unsupported policy throws; annotated non-array throws. [tests/Hexalith.Projects.Client.Tests/ClientGenerationTests.cs:755]
- [x] [Review][Patch] (low) Client helper test only asserts `null == empty`; it never pins either to `field=file_reference_ids;present=true;value=j:[]` as AC2 states. Add an `ExpectedHash(...)` assertion with `j:[]`. [tests/Hexalith.Projects.Client.Tests/ClientGenerationTests.cs:751]
- [x] [Review][Patch] (low) Server reversed-order test reverses only `fileReferenceIds` while sending `fileReferences` sorted; the server fingerprint is built from `fileReferences` (`ProposeNewProjectEndpoint.cs:739`), so its ordinal sort is never exercised. Send `ConfirmFileReferencesJson(reversedIds)` as well. [tests/Hexalith.Projects.Server.Tests/Queries/ProposeNewProjectEndpointTests.cs:356]
- [x] [Review][Patch] (low) Test IDs (`file-alpha`/`file-bravo`, `file-001`/`file-002`) sort identically under ordinal and culture-sensitive comparison, so a regression to culture sorting would pass AC1. Use mixed-case IDs that order differently (e.g. `file-B…` vs `file-a…`, valid for the server's `^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$`). [tests/Hexalith.Projects.Server.Tests/Queries/ProposeNewProjectEndpointTests.cs:348]

Patches applied 2026-10-06 (uncommitted):
- Policy and an unused-annotation check moved to `src/Hexalith.Projects.Client/Generation/Shared/IdempotencyCollectionCanonicalization.cs`. `Program.cs` now calls `Apply` per top-level field and `EnsureAllDeclarationsApplied(root)` after building helpers.
- `Hexalith.Projects.Client.csproj` adds `Generation/Shared` sources to the helper-generation target's `Inputs`, so an edit to the moved logic still triggers regeneration.
- New `tests/Hexalith.Projects.Client.Tests/IdempotencyCollectionCanonicalizationTests.cs` (6 tests).
- The client helper test now uses `file-Bravo`/`file-alpha` and pins `j:[]`. The server reversed test sends both arrays reversed, using the same mixed-case IDs.
- Evidence:
  - The regenerated helper is byte-identical to the checked-in file, and a spine copy with a nested `goals` annotation makes the generator throw without writing output.
  - Tests: ClientGeneration + canonicalization 107/107; ProposeNewProjectEndpoint 74/74; Contracts 194/194.
  - `run-openapi-fingerprint-gate.ps1` passed, and `git diff --check` is clean.
  - Blocked: `dotnet build Hexalith.Projects.slnx -c Release -warnaserror` fails with 9 analyzer errors (CA1062 and others) in `references/Hexalith.Conversations/src/Hexalith.Conversations.Client/{ConversationClient,IConversationClient}.cs`. That submodule is at a clean committed pointer `e6611b2`, and none of the errors are in files touched here.

**Rejected:**
- `low` — Annotated array with non-string items (int/enum/uuid) emits `Array.Empty<string>()`/`StringComparer.Ordinal`: fails loudly at compile time; no such annotation exists; guard adds complexity. (edge-case-hunter)
- `low` — Annotated property without a scalar `type` (`$ref` sibling / `type: [array, null]`) throws a generic `RequiredScalar` error lacking operation context: still a loud failure on an unreachable input. (edge-case-hunter)
- `low` — Item type unvalidated; enum items would "sort by member name": false for the enum part (`OrderBy(StringComparer.Ordinal)` on an enum key does not compile); the rest is the same loud failure. (blind-hunter)
- `low` — Item type unvalidated (Acceptance Auditor variant): same loud compile failure, unreachable today. (acceptance-auditor)
- `false`/`low` — Spine description "Sorted opaque ids…" contradicts the change: false — it describes the canonical equivalence form, now enforced on both surfaces, and imposes no caller obligation. The `required` non-nullable schema vs. server accepting explicit `null` is pre-existing server leniency named in this spec's Problem; the generated client never sends `null` (`NullValueHandling.Ignore`), and making the schema nullable is a Block-If schema change. (blind-hunter)
- `low` — Generated-request fixtures keep `FileReferences = [FileIdValue]` regardless of IDs: `fileReferences` is not hashed, builders were parameterized as the Code Map asked; unlikely to mislead and reshaping fixtures adds logic. (blind-hunter)
- `low` — Client-side requests in the tests would not pass server validation (same fixture mismatch): same reasoning. (acceptance-auditor)
- `false` — Absent `fileReferenceIds` (what the generated client sends for `null`) is untested: absent and literal `null` both bind to `null` on `ConfirmNewProjectProposalHttpRequest.FileReferenceIds` (no `JsonRequired`) and take the same `input is null` branch. (blind-hunter)
- `false` — Annotation not tied to server / `HelperSchemaVersion` unchanged / extension undocumented: `HelperSchemaVersion` versions signature shape by design and `GeneratedHelpersSha256` did change; removing the annotation fails the reversed parity test; no `x-hexalith-*` extension is documented outside the spine, so this matches convention. (blind-hunter)
- `low` — Commit also bumps `references/Hexalith.EventStore`, `Hexalith.FrontComposer`, `Hexalith.Tenants`: out of spec scope, but superseded by later deliberate submodule commits on `main`; nothing to patch. (acceptance-auditor)

## Spec Change Log

## Review Triage Log

## Design Notes

The canonicalization is field-scoped because array order may be meaningful for other operations. The server's accepted semantics are authoritative for this endpoint: normalize absence to `[]`, sort with `StringComparer.Ordinal`, and hash the resulting JSON array. The generator input must carry that intent explicitly instead of inferring it from `uniqueItems` or descriptive prose.

## Verification

**Commands:**
- `dotnet test tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj --configuration Release -warnaserror` -- proposal spine policy is valid.
- `dotnet test tests/Hexalith.Projects.Client.Tests/Hexalith.Projects.Client.Tests.csproj --configuration Release -warnaserror --filter 'FullyQualifiedName~Hexalith.Projects.Client.Tests.ClientGenerationTests'` -- generated helper parity and currentness pass.
- `dotnet test tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Release -warnaserror --filter 'FullyQualifiedName~Hexalith.Projects.Server.Tests.Queries.ProposeNewProjectEndpointTests'` -- endpoint/ledger parity passes.
- `dotnet build Hexalith.Projects.slnx --configuration Release -warnaserror` -- solution builds with zero warnings and errors.
- `pwsh ./tests/tools/run-openapi-fingerprint-gate.ps1` -- generated inputs and outputs are synchronized.
- `git diff --check` -- changed files are whitespace-clean.
