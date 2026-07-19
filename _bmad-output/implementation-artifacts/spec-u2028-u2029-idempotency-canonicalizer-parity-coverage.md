---
title: 'Harden U+2028/U+2029 Idempotency Canonicalizer Parity'
type: 'bugfix'
created: '2026-07-19'
status: 'done'
baseline_commit: 'f7602dfdfa1392d7a27eef5b636e2a113bae26a4'
review_loop_iteration: 2
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-14-u2028-u2029-idempotency-parity.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Accepted string metadata containing Unicode LINE SEPARATOR (`U+2028`) or PARAGRAPH SEPARATOR (`U+2029`) hashes differently in server canonicalizers and the generated-client hasher, so an equivalent retry can be treated as a different payload. The same characters are also admitted by the domain envelope-identifier guard even though identifier contexts must not contain line separators.

**Approach:** Make every server fingerprint path that canonicalizes accepted project metadata emit the client’s existing literal `\u2028`/`\u2029` escape bytes, reject the separators in envelope identifiers, and pin direct server/generated-request parity plus non-collision vectors. The repository records no production deployment evidence, so no legacy compatibility path is indicated unless contrary live-state evidence appears.

## Boundaries & Constraints

**Always:** Preserve every existing hash for inputs without U+2028/U+2029; retain accepted separator-bearing metadata; return field-name-only validation diagnostics; exercise the real `ProjectCommandValidator`, proposal-confirmation fingerprint path, and generated `ComputeIdempotencyHash()` helpers; keep production dependency direction unchanged.

**Ask First:** Halt if any live durable idempotency ledger may contain separator-bearing legacy fingerprints, or if passing parity requires a public API/OpenAPI/schema change, generated-artifact change, migration, or legacy-hash fallback.

**Never:** Hand-edit `.g.cs` output; change the already-correct client hasher policy; normalize separators to LF; weaken metadata, tenant, authorization, or identifier validation; claim deployment compatibility from test evidence alone.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Separator parity | Accepted CreateProject or proposal metadata contains U+2028 or U+2029 | Server and real generated request hash identical canonical bytes containing lowercase literal `\u2028` or `\u2029` | N/A |
| Non-collision | Separator, LF, and literal ASCII `\u2028`/`\u2029` variants | All relevant fingerprints remain pairwise distinct | Test fails on any collision |
| Identifier safety | Actor, correlation, task, or idempotency identifier contains either separator | Domain validation rejects with safe field-name-only evidence | No raw identifier value is returned |
| Legacy state | A deployed ledger contains a legacy raw-separator fingerprint | Do not change runtime behavior unconditionally | Halt for an approved bounded compatibility strategy |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidator.cs` -- domain/server validation and canonical fingerprints; its shared `Escape` and envelope guard omit both separators, while CreateProject, UpdateProjectSetup, SetProjectFolder, LinkFileReference, and LinkMemory trim separator-bearing equivalence metadata before hashing.
- `src/Hexalith.Projects.Server/Queries/ProposeNewProjectEndpoint.cs` -- proposal-confirmation root fingerprint has a second server escape routine with the same omission and trims display name, description, and setup metadata before hashing.
- `src/Hexalith.Projects.Client/Idempotency/HexalithIdempotencyHasher.cs` -- authoritative generated-client behavior; already escapes both values and must remain unchanged.
- `src/Hexalith.Projects.Client/Generated/HexalithProjectsIdempotencyHelpers.g.cs` -- real generated helpers used by parity tests; verification-only.
- `tests/Hexalith.Projects.Tests/Aggregates/Project/ProjectCommandValidatorTests.cs` -- pure validator, canonical-persistence, raw-length, and identifier regression coverage across every affected domain command.
- `tests/Hexalith.Projects.Client.Tests/Hexalith.Projects.Client.Tests.csproj` -- test-only domain reference for direct CreateProject parity.
- `tests/Hexalith.Projects.Client.Tests/ClientGenerationTests.cs` -- real cross-assembly parity and non-collision vectors for CreateProject, UpdateProjectSetup, SetProjectFolder, LinkFileReference, and LinkMemory, plus the artifact-staleness gate.
- `tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj` -- test-only client reference for proposal parity.
- `tests/Hexalith.Projects.Server.Tests/Queries/ProposeNewProjectEndpointTests.cs` -- proposal root-ledger fingerprint regression coverage.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidator.cs` -- escape U+2028/U+2029 in every domain fingerprint value and reject them from envelope identifiers; for CreateProject, SetProjectFolder, LinkFileReference, and LinkMemory, hash the accepted raw display metadata when it contains either separator; for UpdateProjectSetup, hash the complete raw setup when any goal or user-instruction item contains either separator. Keep persisted canonical values trimmed and all separator-free fingerprints unchanged.
- [x] `src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidator.cs` -- bound every raw value used by separator-aware hashing to its existing field maximum. When UpdateProjectSetup switches to raw mode, bound every raw goal and user-instruction item, including items without a separator, and reject with the existing safe field name.
- [x] `src/Hexalith.Projects.Server/Queries/ProposeNewProjectEndpoint.cs` -- align proposal escaping and, whenever any fingerprint-equivalence metadata field contains either separator, hash the raw display name, description, and setup metadata exactly as the generated helper does; retain the existing trim/null behavior for separator-free requests and, in raw mode, bound all three raw values to their existing field maxima before hashing.
- [x] `tests/Hexalith.Projects.Tests/Aggregates/Project/ProjectCommandValidatorTests.cs` -- prove embedded/leading/trailing separator handling, separator-only optional display metadata, unchanged canonical persistence, raw-mode length rejection, all envelope-field rejections, pinned hashes, legacy mismatch, and non-collision for every affected domain command.
- [x] `tests/Hexalith.Projects.Client.Tests/Hexalith.Projects.Client.Tests.csproj` and `tests/Hexalith.Projects.Client.Tests/ClientGenerationTests.cs` -- add a test-only domain reference and compare each real domain validator with its matching generated helper for both separators in CreateProject, UpdateProjectSetup goals and user instructions, SetProjectFolder display metadata, LinkFileReference display metadata, and LinkMemory display metadata; cover leading, embedded, and trailing positions, request-scoped raw mode, unaffected baselines, and LF/literal-escape non-collision.
- [x] `tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj` and `tests/Hexalith.Projects.Server.Tests/Queries/ProposeNewProjectEndpointTests.cs` -- add a test-only client reference and capturing-ledger assertions comparing the proposal endpoint with generated `ConfirmNewProjectProposalRequest` when each of display name, description, and setup metadata independently contains leading, embedded, trailing, or separator-only accepted values; prove request-scoped raw mode, raw-length rejection, separator-free regression, and proposal-path non-collision by sending accepted baseline/separator variants through the endpoint while asserting LF and literal-backslash-u metadata remain safely rejected and their generated hashes remain distinct.

**Acceptance Criteria:**
- Given accepted separator-bearing metadata in any position, when the server and matching generated request compute fingerprints, then both return the same pinned `sha256:` value using literal lowercase backslash-u bytes.
- Given a proposal whose fingerprint-equivalence metadata contains a separator, when the server hashes it, then all three metadata equivalence fields use their raw accepted request values so ordinary surrounding whitespace and separator-only optional values remain client-identical; separator-free requests retain their existing trim/null fingerprints.
- Given UpdateProjectSetup, SetProjectFolder, LinkFileReference, or LinkMemory metadata containing either separator, when the real validator and matching generated helper compute fingerprints, then request-scoped raw values produce identical fingerprints while accepted canonical persistence remains unchanged.
- Given separator-aware raw hashing is selected, when any raw equivalence metadata value exceeds its existing field maximum, then validation rejects with field-name-only evidence before escaping or hashing it.
- Given U+2028, U+2029, LF, literal backslash-u text, and an unaffected baseline, when fingerprints are computed, then the separator cases do not collide and the baseline hash is unchanged.
- Given either separator in every envelope identifier field, when domain validation runs, then it rejects safely with no value echo.
- Given a client build, when generated-artifact verification runs, then no `.g.cs` content changes.

## Spec Change Log

- 2026-07-19, review loop 1: Review found that .NET `Trim()` removes leading and trailing U+2028/U+2029 before both direct-create and proposal fingerprints are computed, so embedded-only tests passed while accepted boundary values still disagreed with the generated client. The implementation tasks, acceptance criteria, code map, and design notes now require separator-aware raw fingerprint inputs, boundary and separator-only vectors for every proposal metadata equivalence field, and proposal-path non-collision. Avoid the known-bad state where escaping is correct only after lossy trimming. KEEP: explicit lowercase separator escaping, safe envelope rejection, unchanged separator-free hashes, unchanged persisted canonical metadata, test-only dependency direction, real generated request helpers, pinned hashes, legacy mismatch evidence, and generated-file immutability.
- 2026-07-19, review loop 2: Review found that UpdateProjectSetup, SetProjectFolder, LinkFileReference, and LinkMemory still trimmed accepted separator-bearing metadata before server hashing, and that request-scoped raw mode could hash unbounded whitespace padding because legacy validation checks trimmed length. The code map, tasks, acceptance criteria, and design notes now cover every affected generated mutation helper, request-wide setup raw mode, bounded raw values, and proposal non-collision against accepted and rejected variants. Avoid the known-bad state where CreateProject/proposal pass while other generated helpers diverge or raw escaping amplifies unbounded accepted input. KEEP from loop 1, plus the correct CreateProject/proposal request-scoped raw rule, boundary and separator-only vectors, separator-free trim/null regression pins, and exact verification reporting.
- 2026-07-19, implementation clarification: Proposal metadata's existing backslash block means literal ASCII `\\u2028`/`\\u2029` values are intentionally rejected. The proposal test task now preserves that validation and proves non-collision through safe endpoint rejection plus distinct generated hashes; accepting literal escapes would violate the frozen prohibition against weakening metadata validation.

## Design Notes

The client hasher is the established canonical policy. Test vectors must use the actual generated request methods, not a test reimplementation. The CreateProject embedded U+2028/U+2029 target hashes are `sha256:b8926519ad4db0115ce4d82416caf8ca9fb27fedd087fc90eb0032104f850d78` and `sha256:358216e481d29223f697a3a0023f5fe41ea8904894bd271c592b2fb7dec272bf`.

Fingerprint source selection must be request-scoped. Direct CreateProject, SetProjectFolder, LinkFileReference, and LinkMemory hash their raw display metadata only when that value contains U+2028/U+2029. UpdateProjectSetup hashes the complete raw setup when any goal or user-instruction item contains either separator. Proposal confirmation hashes the raw values of display name, description, and setup metadata when any one of those three equivalence fields contains either separator. Each path otherwise keeps its pre-change trim/null behavior. Persisted canonical metadata remains trimmed. This conditional rule is required both to match the raw generated-client policy for every separator-bearing request and to preserve every existing separator-free server hash.

Before a path hashes raw values, every raw string it will hash must fit that field's existing maximum length, including separator-free sibling fields pulled into request-scoped raw mode. This closes allocation amplification without adding a new schema limit or changing hashes. Separator-only optional display metadata remains accepted within the bound and must be fingerprinted as present even when its persisted canonical value is null.

## Verification

**Commands:**
- `dotnet test tests/Hexalith.Projects.Tests/Hexalith.Projects.Tests.csproj --configuration Release -warnaserror` -- validator/parity lane passes.
- `dotnet test tests/Hexalith.Projects.Client.Tests/Hexalith.Projects.Client.Tests.csproj --configuration Release -warnaserror` -- real generated-helper and staleness tests pass.
- `dotnet test tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Release -warnaserror` -- proposal fingerprint lane passes.
- `dotnet build Hexalith.Projects.slnx --configuration Release -warnaserror` -- solution builds with zero warnings/errors.
- `pwsh ./tests/tools/run-openapi-fingerprint-gate.ps1 && git diff --check && git diff --exit-code -- src/Hexalith.Projects.Client/Generated` -- generated output is current, unchanged, and whitespace-clean.

**Results:** All verification passed after final review patches: domain tests 656/656, client tests 114/114, server tests 556/556, solution build with 0 warnings and 0 errors, OpenAPI fingerprint gate current (99/99 selected client tests), generated artifacts unchanged, and `git diff --check` clean.

## Suggested Review Order

**Domain fingerprint policy**

- Separator-aware routing preserves canonical persistence while matching raw generated-client values.
  [`ProjectCommandValidator.cs:112`](../../src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidator.cs#L112)

- Setup collections are snapshotted once before validation and raw fingerprinting.
  [`ProjectCommandValidator.cs:177`](../../src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidator.cs#L177)

- Shared domain escaping emits explicit lowercase separator escapes.
  [`ProjectCommandValidator.cs:718`](../../src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidator.cs#L718)

**Proposal root fingerprint**

- Request-wide raw bounds prevent separator-triggered allocation amplification.
  [`ProposeNewProjectEndpoint.cs:619`](../../src/Hexalith.Projects.Server/Queries/ProposeNewProjectEndpoint.cs#L619)

- Raw metadata selection preserves separator-free proposal fingerprints.
  [`ProposeNewProjectEndpoint.cs:744`](../../src/Hexalith.Projects.Server/Queries/ProposeNewProjectEndpoint.cs#L744)

**Rejection safety**

- Envelope validation rejects both separators before command execution.
  [`ProjectCommandValidator.cs:937`](../../src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidator.cs#L937)

- Result sanitization prevents rejected identifiers from echoing separators.
  [`ProjectResult.cs:281`](../../src/Hexalith.Projects/Aggregates/Project/ProjectResult.cs#L281)

**Verification and follow-up**

- Real generated helpers cover every affected mutation fingerprint path.
  [`ClientGenerationTests.cs:402`](../../tests/Hexalith.Projects.Client.Tests/ClientGenerationTests.cs#L402)

- Proposal ledger tests pin parity, exact bounds, and non-collision.
  [`ProposeNewProjectEndpointTests.cs:301`](../../tests/Hexalith.Projects.Server.Tests/Queries/ProposeNewProjectEndpointTests.cs#L301)

- Domain tests pin legacy preservation, stable snapshots, and safe rejection.
  [`ProjectCommandValidatorTests.cs:138`](../../tests/Hexalith.Projects.Tests/Aggregates/Project/ProjectCommandValidatorTests.cs#L138)

- Pre-existing proposal collection parity gaps remain explicitly deferred.
  [`deferred-work.md:58`](deferred-work.md#L58)
