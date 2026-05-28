# Context-Assembly Fail-Closed Decision Matrix

> **Owner:** Epic 3 Story 3.1 (Context-assembly policy & allowlist).
> **Policy file:** [`src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs`](../src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs).
> **Single source of truth.** Stories 3.2 (`GetProjectContext`), 3.3 (`ExplainContextSelection`), 3.4 (`RefreshProjectContext`), and 3.5 (`GetConversationStartSetup`) consume this matrix verbatim. Subsequent Epic 3 stories may **extend** rows additively but must not edit Story 3.1's cell semantics.

The policy is a pure function: it evaluates the seven inclusion checks declared by
[`ProjectContextInclusionOrder.Sequence`](../src/Hexalith.Projects/Context/ProjectContextInclusionOrder.cs)
in order and emits the per-candidate evaluation trace alongside the assembled
[`ProjectContext`](../src/Hexalith.Projects.Contracts/Models/ProjectContext.cs).

Each cell records: surfaced `ReferenceState` / `FailedCheck` or `n/a` / outer assembly outcome.
The right-most column names the Tier-1 test fixture (under
`tests/Hexalith.Projects.Tests/Context/`) that exercises the cell.

| Evidence state         | GetProjectContext (3.2)                       | RefreshProjectContext (3.4)                  | ExplainContextSelection (3.3)                 | GetConversationStartSetup (3.5)              | Test fixture                                                  |
| ---------------------- | --------------------------------------------- | -------------------------------------------- | --------------------------------------------- | -------------------------------------------- | ------------------------------------------------------------- |
| missing                | n/a / TenantAuthority / Unauthorized          | n/a / TenantAuthority / Unauthorized         | n/a / TenantAuthority / Unauthorized          | n/a / TenantAuthority / Unauthorized         | `ProjectContextInclusionPolicyTenantAuthorityTests`           |
| stale                  | n/a / n/a / Assembled (Freshness=Stale)       | n/a / n/a / Assembled (Freshness=Stale)      | n/a / n/a / Assembled (Freshness=Stale)       | n/a / n/a / Assembled (Freshness=Stale)      | `ProjectContextInclusionPolicyTenantAuthorityTests`           |
| unauthorized           | Unauthorized / ReferenceAuthorization / Assembled | Unauthorized / ReferenceAuthorization / Assembled | Unauthorized / ReferenceAuthorization / Assembled | Unauthorized / ReferenceAuthorization / Assembled | `ProjectContextInclusionPolicyMemoriesCandidateTests`     |
| unavailable            | Unavailable / ReferenceFreshness / Assembled  | Unavailable / ReferenceFreshness / Assembled | Unavailable / ReferenceFreshness / Assembled  | Unavailable / ReferenceFreshness / Assembled | `ProjectContextInclusionPolicyFileReferenceCandidateTests`    |
| forbidden              | Unauthorized / ReferenceAuthorization / Assembled | Unauthorized / ReferenceAuthorization / Assembled | Unauthorized / ReferenceAuthorization / Assembled | Unauthorized / ReferenceAuthorization / Assembled | `ProjectContextInclusionPolicyConversationCandidateTests` |
| redacted               | Excluded / ReferenceFreshness / Assembled     | Excluded / ReferenceFreshness / Assembled    | Excluded / ReferenceFreshness / Assembled     | Excluded / ReferenceFreshness / Assembled    | `ProjectContextInclusionPolicyConversationCandidateTests`     |
| conflict               | Conflict / ReferenceLifecycle / Assembled     | Conflict / ReferenceLifecycle / Assembled    | Conflict / ReferenceLifecycle / Assembled     | Conflict / ReferenceLifecycle / Assembled    | `ProjectContextInclusionPolicyTests`                          |
| invalidReference       | InvalidReference / ReferenceKindAllowlist / Assembled | InvalidReference / ReferenceKindAllowlist / Assembled | InvalidReference / ReferenceKindAllowlist / Assembled | InvalidReference / ReferenceKindAllowlist / Assembled | `ProjectContextInclusionPolicyNonAllowlistedKindTests`     |
| archived               | Archived / ReferenceLifecycle / Assembled     | Archived / ReferenceLifecycle / Assembled    | Archived / ReferenceLifecycle / Assembled     | Archived / ReferenceLifecycle / Assembled    | `ProjectContextInclusionPolicyLifecycleTests`                 |
| ambiguous              | Ambiguous / ReferenceLifecycle / Assembled    | Ambiguous / ReferenceLifecycle / Assembled   | Ambiguous / ReferenceLifecycle / Assembled    | Ambiguous / ReferenceLifecycle / Assembled   | `ProjectContextInclusionPolicyTests`                          |

## Outer-outcome overrides

These rows collapse the entire assembly before any per-reference check runs. They are the
existence-safe / safe-denial guards. Subsequent Epic 3 stories must not weaken them.

| Outer evidence state                                   | All operations: outer outcome             | Diagnostic (closed vocab)              | Test fixture                                              |
| ------------------------------------------------------ | ----------------------------------------- | -------------------------------------- | --------------------------------------------------------- |
| Missing authoritative tenant                           | `Unauthorized` (assembly-level)           | n/a (assembly empty)                   | `ProjectContextInclusionPolicyTenantAuthorityTests`       |
| `RequestedTenantId != AuthoritativeTenantId`           | `Unauthorized` (assembly-level)           | n/a (assembly empty)                   | `ProjectContextInclusionPolicyTenantAuthorityTests`       |
| Tenant-access denied / disabled / unknown / malformed  | `Unauthorized` (assembly-level)           | n/a (assembly empty)                   | `ProjectContextInclusionPolicyTenantAuthorityTests`       |
| Tenant-access unavailable / rebuilding / replay-conflict | `Unauthorized` (assembly-level)         | n/a (assembly empty)                   | `ProjectContextInclusionPolicyTenantAuthorityTests`       |
| Project not visible (`Detail = null`)                  | `ProjectUnavailable` (safe-denial 404)    | n/a (assembly empty)                   | `ProjectContextInclusionPolicyProjectVisibilityTests`     |
| Project belongs to different tenant (cross-tenant)     | `ProjectUnavailable` (safe-denial 404)    | n/a (assembly empty)                   | `ProjectContextInclusionPolicyProjectVisibilityTests`     |
| Project `Lifecycle == Archived`                        | `Assembled` (every reference excluded)    | `projectArchived` (per reference row)  | `ProjectContextInclusionPolicyLifecycleTests`             |

## Memories-specific rows (Story 2.6 ADR §Epic 3 allowlist treatment)

| Evidence                                          | Surfaced `ReferenceState`            | `FailedCheck`              | Diagnostic                | Outer outcome   |
| ------------------------------------------------- | ------------------------------------ | -------------------------- | ------------------------- | --------------- |
| `Included`                                        | `Included` (+ `MemoryMatched`)       | n/a                        | n/a                       | `Assembled`     |
| `Archived` (Memories `Case.Status` = `Closed`/`Deleting`) | `Archived`                  | `ReferenceLifecycle`       | `referenceArchived`       | `Assembled`     |
| `Unauthorized`                                    | `Unauthorized`                       | `ReferenceAuthorization`   | `referenceUnauthorized`   | `Assembled`     |
| `Unavailable` (`UnavailableProjectMemoryDirectory`) | `Unavailable`                      | `ReferenceFreshness`       | `referenceUnavailable`    | `Assembled`     |
| `InvalidReference` (malformed identifier)         | `InvalidReference`                   | `ReferenceKindAllowlist`   | `referenceInvalidIdentifier` | `Assembled`  |
| `TenantMismatch` (Memories ACL recheck)           | `Unauthorized` (boundary collapse)   | `ReferenceAuthorization`   | `tenantMismatch`          | `Assembled`     |

## Project Folder rows (Story 2.4)

| Evidence                                          | Surfaced `ReferenceState`   | `FailedCheck`              | Diagnostic                | Outer outcome   |
| ------------------------------------------------- | --------------------------- | -------------------------- | ------------------------- | --------------- |
| Active + `Included`                               | `Included` (+ `ProjectFolderMatched`) | n/a              | n/a                       | `Assembled`     |
| Pending (`ProjectFolderCreationPending` degraded path) | `Pending`              | `ReferenceFreshness`       | `projectFolderPending`    | `Assembled`     |
| Archived                                          | `Archived`                  | `ReferenceLifecycle`       | `referenceArchived`       | `Assembled`     |
| Unavailable                                       | `Unavailable`               | `ReferenceFreshness`       | `referenceUnavailable`    | `Assembled`     |
| `TenantMismatch` (Folders ACL recheck)            | `Unauthorized` (boundary collapse) | `ReferenceAuthorization` | `tenantMismatch`     | `Assembled`     |

There is exactly **one** Project Folder reference in the assembled result — never a list.

## File reference rows (Story 2.5)

| Evidence                                          | Surfaced `ReferenceState`            | `FailedCheck`              | Diagnostic                | Outer outcome   |
| ------------------------------------------------- | ------------------------------------ | -------------------------- | ------------------------- | --------------- |
| `Included`                                        | `Included` (+ `FileReferenceMatched`) | n/a                       | n/a                       | `Assembled`     |
| `Archived`                                        | `Archived`                           | `ReferenceLifecycle`       | `referenceArchived`       | `Assembled`     |
| `Stale`                                           | `Stale`                              | `ReferenceFreshness`       | `referenceStale`          | `Assembled`     |
| `Unavailable`                                     | `Unavailable`                        | `ReferenceFreshness`       | `referenceUnavailable`    | `Assembled`     |
| `Unauthorized`                                    | `Unauthorized`                       | `ReferenceAuthorization`   | `referenceUnauthorized`   | `Assembled`     |
| `InvalidReference`                                | `InvalidReference`                   | `ReferenceKindAllowlist`   | `referenceInvalidIdentifier` | `Assembled`  |

## Conversation rows (Story 2.1)

| `ProjectConversationTrustSignal` | Surfaced `ReferenceState`         | `FailedCheck`              | Diagnostic                | Outer outcome   |
| -------------------------------- | --------------------------------- | -------------------------- | ------------------------- | --------------- |
| `Current`                        | `Included` (+ `ConversationLinked`) | n/a                      | n/a                       | `Assembled`     |
| `Stale`                          | `Stale`                           | `ReferenceFreshness`       | `referenceStale`          | `Assembled`     |
| `MixedGeneration`                | `Stale`                           | `ReferenceFreshness`       | `referenceStale`          | `Assembled`     |
| `Rebuilding`                     | `Unavailable`                     | `ReferenceFreshness`       | `referenceUnavailable`    | `Assembled`     |
| `Unavailable`                    | `Unavailable`                     | `ReferenceFreshness`       | `referenceUnavailable`    | `Assembled`     |
| `Forbidden`                      | `Unauthorized`                    | `ReferenceAuthorization`   | `referenceUnauthorized`   | `Assembled`     |
| `Redacted`                       | `Excluded`                        | `ReferenceFreshness`       | `referenceRedacted`       | `Assembled`     |

## Non-allowlisted reference kind (final check)

`ReferenceKind` is one of `"folder"`, `"file"`, `"memory"`, `"conversation"`. Anything else is
**excluded** with `ReferenceState = InvalidReference`, `FailedCheck = ReferenceKindAllowlist`,
`Diagnostic = "referenceKindNotAllowlisted"`, and a structured-log warning entry via
`ILogger<ProjectContextInclusionPolicy>` (LogLevel.Warning). The policy never lets such a
candidate fall through to the `ProjectContext` per-kind lists.

## Read-only vs trust-bearing operations

All four Epic 3 operations on this matrix are **read-only**, so a stale tenant-access projection
is allowed and downgrades the assembly `Freshness` to `Stale` rather than collapsing the
assembly. A future trust-bearing operation must collapse to `Unauthorized` in the stale row.

## Conventions

- Outcomes use the assembly-level
  [`ProjectContextAssemblyOutcome`](../src/Hexalith.Projects.Contracts/Ui/ProjectContextAssemblyOutcome.cs)
  enum (`Assembled` | `ProjectUnavailable` | `Unauthorized`).
- `FailedCheck` values come from
  [`ProjectContextInclusionCheck`](../src/Hexalith.Projects.Contracts/Ui/ProjectContextInclusionCheck.cs).
- Diagnostic strings are members of the closed
  [`ProjectContextInclusionDiagnostic`](../src/Hexalith.Projects.Contracts/Ui/ProjectContextInclusionDiagnostic.cs)
  vocabulary; no free-form text appears in the `Diagnostic` field.
- Cross-tenant existence is always surfaced as `ProjectUnavailable`, never `Unauthorized`, at
  the boundary (safe-denial 404 contract, reused from Story 1.4).
