# Operator Diagnostic Parity Matrix

Story 5.2 defines the backend operator diagnostic model that later Web, MCP, and CLI surfaces must
preserve without renaming states, reason codes, identifiers, tenant-isolation behavior, or redaction
rules.

## Story 5.2 Baseline

| Field group | REST source | Later Web/MCP/CLI parity requirement |
|-------------|-------------|--------------------------------------|
| Project identity | `projectId` | Render/pass through as an opaque Project id only. Never derive tenant authority from it. |
| Safe metadata | `name`, `description`, `createdAt`, `updatedAt` | Display as metadata. Do not treat description as a payload expansion surface. |
| Lifecycle | `lifecycleState` | Preserve `active` / `archived` spellings and archived metadata-read behavior. |
| Setup preferences | `setupMetadata`, `projectSetup` | Show bounded setup preferences only. Do not add setup body export or raw prompt views. |
| Context activation | `contextActivation.enabled`, `contextActivation.blockedReasonCode` | Preserve safe blocked reason codes so denied/archived states do not render as empty panels. |
| References | `references[].referenceKind`, `referenceState`, `referenceId`, `displayName`, `reasonCode`, `freshness` | Preserve reference kind/state/reason vocabulary and tenant-filtered rows. Do not expose paths, file contents, memory payloads, transcripts, or sibling denial details. |
| Audit timeline | `auditTimeline[]` | Preserve timestamp, actor principal, operation type, safe state delta, affected reference, correlation id, task id, audit event id, reason code, and safe resolution identifiers. Do not expose idempotency keys, command bodies, candidate scores/ranks, rejected ids, proposal bodies, raw prompts, or payloads. |
| Freshness | `freshness` plus `X-Hexalith-Freshness` | Preserve `eventually_consistent`, projection watermark, stale flag, and trust state. Do not invent wall-clock freshness. |
| Error boundary | 404 safe denial, 400 validation after authorization, 503 retryable projection unavailable | Preserve no-existence-disclosure behavior across adapters. Unauthorized probes must not receive validation hints. |

## Reuse Notes

- Tenant inventory remains `ListProjects`; Story 5.2 does not create a duplicate tenant inventory DTO.
- Project diagnostics are `GET /api/v1/projects/{projectId}/operator-diagnostics` with `auditLimit`
  default 25 and max 100.
- Rendering/export/maintenance workflows are owned by later Epic 5 stories; this matrix only fixes the
  shared safe model and parity contract.
