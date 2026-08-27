---
title: 'Align Proposal Fingerprint Collection Parity'
type: 'bugfix'
created: '2026-08-27'
status: 'in-progress'
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
