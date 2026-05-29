# Resolution Scoring Heuristic

> **Owner:** Epic 4 Story 4.1 (Resolution engine, compute-on-demand).
> **Policy file:** [`src/Hexalith.Projects/Resolution/ProjectResolutionEngine.cs`](../src/Hexalith.Projects/Resolution/ProjectResolutionEngine.cs).
> **Single source of truth.** Stories 4.2 (`Resolve From Conversation`) and 4.3 (`Resolve From Attachments`) pre-fetch evidence and call this engine. They do not duplicate these scoring, ranking, or outcome rules.

The engine is a pure function: it evaluates pre-fetched, Projects-shaped candidate evidence and emits a metadata-only [`ProjectResolution`](../src/Hexalith.Projects.Contracts/Models/ProjectResolution.cs). It performs no reads, writes, network calls, ACL calls, event emission, projection updates, or trace persistence.

Each cell records: qualifying-match rule / score contribution / confidence band / result outcome. The right-most column names the Tier-1 test fixture under `tests/Hexalith.Projects.Tests/Resolution/` that pins doc-to-code agreement.

## Qualifying-Match Rule

| Input evidence state | Qualifying match rule | Surfaced exclusion evidence | Test fixture |
| -------------------- | --------------------- | --------------------------- | ------------ |
| `ReferenceState.Included` | Contributes its distinct `ProjectReasonCode` weight to the candidate score. | n/a | `ProjectResolutionScoringMatrixTests` |
| Any other `ReferenceState` | Does not contribute to score. | A `ResolutionExclusion` row with the candidate `ProjectId`, surfaced state, reason code, and closed diagnostic. | `ProjectResolutionScoringMatrixTests` |
| Missing authoritative tenant | No candidate can qualify. | Each candidate is excluded with `ReferenceState.TenantMismatch` and `tenantMismatch`. | `ProjectResolutionEngineTests` |
| `RequestedTenantId != AuthoritativeTenantId` | No candidate can qualify. | Each candidate is excluded with `ReferenceState.TenantMismatch` and `tenantMismatch`. | `ProjectResolutionEngineTests` |
| `ProjectLifecycle.Archived` and `IncludeArchived == false` | Candidate cannot qualify. | Candidate is excluded with `ReferenceState.Archived` and `projectArchived`. | `ProjectResolutionEngineTests` |
| `ProjectLifecycle.Archived` and `IncludeArchived == true` | Included signals qualify normally. | Non-included signals still surface as exclusions. | `ProjectResolutionEngineTests` |

## Per-Reason-Code Weights

Weights are declared once in [`ProjectResolutionScoringRules.Weights`](../src/Hexalith.Projects/Resolution/ProjectResolutionScoringRules.cs). A candidate receives each reason-code weight at most once, even if multiple references produce the same reason code.

| Reason code | Weight | Rationale | Test fixture |
| ----------- | ------ | --------- | ------------ |
| `ConversationLinked` | 50 | Direct conversation linkage is a strong project intent signal. | `ProjectResolutionScoringMatrixTests` |
| `ProjectFolderMatched` | 45 | The project folder is the primary project-owned context anchor. | `ProjectResolutionScoringMatrixTests` |
| `FileReferenceMatched` | 35 | File references are concrete but optional context anchors. | `ProjectResolutionScoringMatrixTests` |
| `MemoryMatched` | 30 | Memory references are concrete but may be broader than a file or folder anchor. | `ProjectResolutionScoringMatrixTests` |
| `MetadataMatched` | 20 | Metadata is useful but intentionally the weakest signal. | `ProjectResolutionScoringMatrixTests` |

## Confidence Bands

Confidence bands are documented thresholds, not wire enum values. The wire result carries only a numeric `Score`, a `Rank`, and shared-vocabulary reason codes.

A candidate **qualifies** whenever its score is at or above the minimum qualifying score (`20`). The band is **informational only** — it describes confidence strength and contributes to score-descending rank ordering. It does **not** decide the top-level outcome: whether the result is `SingleCandidate` or `MultipleCandidates` is determined solely by how many candidates qualify (see the Single-vs-Multiple Threshold section), never by the band.

| Candidate score | Band | Result impact | Test fixture |
| --------------- | ---- | ------------- | ------------ |
| 0 | none | Below the minimum qualifying score; the candidate cannot qualify. | `ProjectResolutionScoringMatrixTests` |
| 20-34 | low | At or above the minimum; the candidate qualifies. Ranks below higher bands. | `ProjectResolutionScoringMatrixTests` |
| 35-49 | medium | At or above the minimum; the candidate qualifies. Outranks `low`. | `ProjectResolutionScoringMatrixTests` |
| 50-79 | high | At or above the minimum; the candidate qualifies. Outranks `medium`. | `ProjectResolutionScoringMatrixTests` |
| 80+ | strong | At or above the minimum; the candidate qualifies. Highest rank band. | `ProjectResolutionScoringMatrixTests` |

## Single-vs-Multiple Threshold

The engine biases toward `MultipleCandidates` when ambiguity exists. It never silently collapses two qualifying candidates into a single attach.

| Qualifying candidates after fail-closed filtering | Result | Candidate ordering | Test fixture |
| ------------------------------------------------- | ------ | ------------------ | ------------ |
| 0 | `NoMatch` | Empty candidates list; exclusion rows explain failed-closed inputs. | `ProjectResolutionEngineTests` |
| 1 | `SingleCandidate` | Candidate rank is `1`. | `ProjectResolutionEngineTests` |
| 2+ | `MultipleCandidates` | Ranked by score descending, then `ProjectId` Ordinal ascending. | `ProjectResolutionEngineTests` |

## Trace Mapping

The future Resolution Trace view can render its five visual states without persisted trace history:

| Trace state | Engine evidence |
| ----------- | --------------- |
| `Resolved` | Top-level `ResolutionResult.SingleCandidate`. |
| `NoMatch` | Top-level `ResolutionResult.NoMatch`. |
| `MultipleCandidates` | Top-level `ResolutionResult.MultipleCandidates`. |
| `Excluded` | `ResolutionExclusion` rows for archived or policy-excluded candidates/references. |
| `FailedClosed` | `ResolutionExclusion` rows for unverifiable tenant, authorization, freshness, conflict, invalid, pending, stale, unavailable, or ambiguous evidence. |

## Conventions

- Positive match signals use only [`ProjectReasonCode`](../src/Hexalith.Projects.Contracts/Ui/ProjectReasonCode.cs).
- Result outcomes use only [`ResolutionResult`](../src/Hexalith.Projects.Contracts/Ui/ResolutionResult.cs).
- Exclusion states use only [`ReferenceState`](../src/Hexalith.Projects.Contracts/Ui/ReferenceState.cs).
- Safe diagnostics reuse [`ProjectContextInclusionDiagnostic`](../src/Hexalith.Projects.Contracts/Ui/ProjectContextInclusionDiagnostic.cs); no free-form upstream message, path, prompt, transcript, body, token, or secret is emitted.
