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

## Story 5.3 Shell / Shared Rendering Contract

| Surface primitive | Source of truth | Web rendering contract | MCP/CLI parity handoff |
|-------------------|-----------------|------------------------|------------------------|
| FrontComposer navigation seed | `ProjectOperatorDiagnosticShellProjection` over `ProjectOperatorDiagnostic` | Generated `RegisterDomain` metadata groups the Projects operational console under the `Projects` bounded context. The seed is shell/navigation metadata only, not the full 5.4 inventory/detail view. | Reuse the same field names and shared vocabulary when command/resource descriptors land. |
| Project Diagnostic Header | `ProjectOperatorDiagnostic` | Shows tenant scope label, project id/name, lifecycle badge, warning count, last-updated timestamp, mode label (`read-only`, `dry-run`, `maintenance`), and copyable project id. | Expose the same header fields as columns/fields; do not add tenant authority from client input. |
| Lifecycle/reference/result/reason badges | `ProjectVocabularyDescriptors` and `[ProjectionBadge]` enum metadata | Badge rendering uses visible labels plus accessible names through `FcStatusBadge`; color is supportive only. | CLI/MCP fields should use the descriptor `Code`/label vocabulary and avoid adapter-local mappings. |
| Empty states | `ProjectEmptyState` | Distinguishes no projects/references/audit, access denied, data unavailable, and filter returned no results through stable categories and visible text. No blank tables. | Preserve the categories as explicit adapter states rather than returning empty rows for denial/unavailable. |
| Feedback | `ProjectConsoleFeedback` | Distinguishes success, warning, error, fail-closed, and loading. Messages expose safe reason codes/correlation ids only. | Return safe reason codes only; never echo raw problem details, prompts, bodies, paths, tokens, proposal bodies, or sibling denial details. |
| Operator diagnostic query | Generated `GetProjectOperatorDiagnosticsAsync(...)` | UI source calls the generated query client with `X-Hexalith-Freshness: eventually_consistent`, no `Idempotency-Key`, and maps 400/404/503 to safe feedback. | MCP/CLI query adapters must preserve the same query semantics. |

Stories 5.4-5.11 consume these primitives and own their specific inventory/detail, reference health,
resolution trace, audit export, warning queue, maintenance mutation, MCP/CLI, and final accessibility
behavior.

## Story 5.4 Inventory / Detail Contract

| Surface | Source of truth | Web rendering contract | MCP/CLI parity handoff |
|---------|-----------------|------------------------|------------------------|
| Project inventory rows | Generated `ListProjectsAsync(lifecycle, correlationId, eventually_consistent, cancellationToken)` over `ProjectListResponse` / `ProjectListItem` | `/` and `/projects` render a DataGrid-style table with `project-inventory-grid`, `project-inventory-row`, `project-inventory-row-link`, lifecycle, safe project id/name, warning summary (`project-inventory-warning-summary`, currently the placeholder "Not available on list row" until additive metadata-only summary fields land), updated timestamp, server-derived tenant scope label, and freshness evidence. External rows still do not carry `tenantId`. | Use the same list query and field names for inventory/list commands. Do not add a duplicate operator inventory endpoint. |
| Inventory filters | Existing `ProjectListItem` fields | Lifecycle filter is query-backed (`project-inventory-filter-lifecycle`); timestamp filter is local over `updatedAt` (`project-inventory-filter-updated`). Warning, reason-code, and reference-type filters (`project-inventory-filter-warning`, `project-inventory-filter-reason-code`, `project-inventory-filter-reference-type`) are disabled because current list rows have no safe summary fields. Empty/filter-empty render `project-inventory-empty`. | CLI/MCP may expose only filters backed by existing list fields or future additive metadata-only summary fields. Warning aggregation/queue is owned by Story 5.8. |
| Project detail inspector | Generated `GetProjectAsync(projectId, correlationId, eventually_consistent, cancellationToken)` plus bounded `GetProjectOperatorDiagnosticsAsync(...)` evidence | `/projects/{ProjectId}` preserves `project-diagnostic-header` and adds read-only sections/tabs: metadata, setup, references, resolution, audit, and actions. Selectors: `project-detail-inspector`, `project-detail-section-{metadata,setup,references,resolution,audit,actions}` (full registry in `tests/e2e/support/page-objects/project-detail.page.ts`). | Detail/read commands should mirror the same section names and safe field groups. Reference health (5.5), resolution traces (5.6), audit export (5.7), the warnings dashboard (5.8), maintenance mutations (5.9), and MCP/CLI surfaces (5.10) remain later-story surfaces. |
| Safe query semantics | Generated client method signatures and server authorization behavior | Sources send `X-Hexalith-Freshness: eventually_consistent`, never send `Idempotency-Key`, map 400/404/503/unexpected failures to `ProjectConsoleFeedback`, and keep base detail recoverable when bounded diagnostics are unavailable. | Preserve safe-denial 404, validation after authorization, retryable 503, and no raw ProblemDetails/body echoing. |
