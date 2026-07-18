# Implementation-Readiness Traceability & Release-Evidence Matrix — Hexalith.Projects

> **Human-readable view.** The canonical source is [`implementation-readiness-traceability-matrix.yaml`](./implementation-readiness-traceability-matrix.yaml) (schema `hexalith.readiness-evidence.v1`); this Markdown is mechanically generated over the **same stable row keys** and the YAML wins on any conflict. Regenerate after editing the YAML.

## Status & containment

- **Overall readiness:** READY (2026-07-17 planning-layer rerun; implementation remains controlled by per-story gates)
- **Freeze:** Story 6.1 runtime implementation is blocked until 6.1-P4 accepts P0-P3 and the Story 6.1 spec passes ready-for-development; every other story remains controlled by its applicable entry gates
- **Release block:** production + consequential autonomous MCP mutation + proposed-Project confirmation blocked until row 'release-stakeholder-acceptance' (Story 8.11) passes with dated Jerome + John acceptance
- **Validation gate (target):** `dotnet tool run hexalith-evidence validate _bmad-output/planning-artifacts/implementation-readiness-traceability-matrix.yaml` — tool status **not-available**
- **Rows total:** 63 (FR 24 · NFR 11 · findings 16 · release 12). No row is `passed` pre-READY.

**Status legend:** `pending` = owned + fully specified, not yet executed · `blocked-external` = additionally gated on a G-1…G-6 or explicitly routed external capability · `not-verified` = environment/tool unavailable (never a pass) · `failed` = executed and currently failing · `passed` = real, honest passing evidence (none yet). None of `pending`/`blocked-external`/`not-verified`/`failed` counts as a pass.


## Functional Requirements (24)

| Key | IDs | Description | Primary | Supporting | ADs | Status | Blocker |
|---|---|---|---|---|---|---|---|
| `fr-1` | FR-1 | Create Project — Folder-first, idempotent, metadata-classified; Active only after exactly one authorized Folder is bound and read-model-confirmed | 7.1 | 6.1 | AD-3, AD-5, AD-8, AD-12, AD-22, AD-31 | 🔒 blocked-external | G-1 durable-task engine + G-2 Folders owner contract |
| `fr-2` | FR-2 | Open Project — authorized metadata/lifecycle/setup/reference summary via supported read models | 6.1 | — | AD-3, AD-14, AD-19, AD-20, AD-32, AD-33 | 🔒 blocked-external | 6.1-P0, P2..P4 (P1 EventStore/Builds baseline normalized 2026-07-18): G-4 runner, dual-principal identity, safe denial, global watermark, production auth contract, and accepted gate record |
| `fr-3` | FR-3 | Update Project Setup — durable, additive, idempotent | 7.2 | — | AD-5, AD-15, AD-16 | 🔒 blocked-external | G-1 durable-task engine |
| `fr-4` | FR-4 | Archive Project — Preview + single-use confirmation + idempotent Durable Task | 7.13 | — | AD-4, AD-5, AD-13 | 🔒 blocked-external | G-1 durable-task engine + confirmation record |
| `fr-5` | FR-5 | List Projects — Tenant-scoped, authorization-filtered, cursor-paged | 6.1 | — | AD-14, AD-19, AD-20, AD-27 | 🔒 blocked-external | 6.1-P0, P2..P4 (P1 EventStore/Builds baseline normalized 2026-07-18): G-4 runner, dual-principal identity, safe denial, global watermark, production auth contract, and accepted gate record |
| `fr-6` | FR-6 | Link Conversation — durable single-membership link via Conversations owner + reverse index | 7.3 | — | AD-10, AD-12, AD-14 | 🔒 blocked-external | G-2 Conversations owner contract |
| `fr-7` | FR-7 | Move Conversation Between Projects — Preview + confirmation + durable saga; prior membership removed before new one | 7.4 | — | AD-5, AD-10, AD-12, AD-13 | 🔒 blocked-external | G-1 + G-2 (Conversations owner) |
| `fr-8` | FR-8 | Set / Replace Project Folder — single-Folder invariant preserved; replace via Preview + confirmation (initial Folder via 7.1) | 7.6 | 7.1 | AD-3, AD-11, AD-12 | 🔒 blocked-external | G-2 Folders owner contract |
| `fr-9` | FR-9 | Link File Reference — idempotent additive link; no Folder change; no content copy | 7.7 | — | AD-11, AD-12, AD-15 | 🔒 blocked-external | G-2 Folders owner contract |
| `fr-10` | FR-10 | Link Memory — idempotent metadata-only link; auth delegated to Memories; Case-vs-Unit per pinned G-2 contract | 7.9 | — | AD-11, AD-12, AD-15 | 🔒 blocked-external | G-2 Memories owner contract (Case/Unit granularity) |
| `fr-11` | FR-11 | Unlink Context Reference — Preview + confirmation + durable task for Conversation/File/Memory; Folder replace-only | 7.5 / 7.8 / 7.10 | 7.6 | AD-5, AD-11, AD-13 | 🔒 blocked-external | G-1 confirmation record + G-2 owner contracts |
| `fr-12` | FR-12 | Resolve Project From Conversation — compute-on-demand; NoMatch/Single/Multiple; transient non-persisted trace | 6.4 | — | AD-7, AD-10, AD-14, AD-32 | 🟡 pending | — |
| `fr-13` | FR-13 | Resolve Project From Attachments — from Folder/File refs; fail-closed on stale auth; no raw file content as data | 6.4 | — | AD-7, AD-11, AD-14, AD-32 | 🟡 pending | — |
| `fr-14` | FR-14 | Confirm Ambiguous Project — bound Confirmation Artifact + durable task; no preselection; rejected candidates not linked | 7.11 | 6.4 | AD-5, AD-13, AD-32 | 🔒 blocked-external | G-1 confirmation record |
| `fr-15` | FR-15 | Propose New Project — durable confirmed Folder-first creation from a bound Preview; no Project before confirmation | 7.12 | 7.1, 6.4 | AD-5, AD-8, AD-13, AD-31 | 🔒 blocked-external | G-1 confirmation record + G-2 owners |
| `fr-16` | FR-16 | Get Project Context — allowlist-assembled setup + included refs with exclusion reasons; metadata-only | 6.3 | — | AD-11, AD-14, AD-32 | 🟡 pending | — |
| `fr-17` | FR-17 | Explain Context Selection — current transient inclusion/exclusion evidence; no secrets/payloads; no persisted trace | 6.3 | 6.4 | AD-7, AD-14 | 🟡 pending | — |
| `fr-18` | FR-18 | Refresh Project Context — read-only recompute; surfaces stale/unavailable refs; never mutates | 6.3 | — | AD-7, AD-14, AD-32 | 🟡 pending | — |
| `fr-19` | FR-19 | Validate Setup & Metadata Classification — four-value metadataClass, shared SensitiveMetadataTierValidator, auth-before-parse, 400 rejectedField, name-only compatibility | 7.1 | 6.7 | AD-31, AD-16 | 🔒 blocked-external | G-1; E-9 remediation gate |
| `fr-20` | FR-20 | Retrieve Conversation-Start Setup — bounded start subset with admission truth; excludes audit metadata | 6.2 | — | AD-14, AD-32 | 🟡 pending | — |
| `fr-21` | FR-21 | Record Project Audit Events — metadata-only audit of admissions/mutations/confirmations/reconciliation/export; ≥365-day retention | 8.1 | — | AD-21, AD-26, AD-30 | 🟡 pending | — |
| `fr-22` | FR-22 | Support Operator Read Access — authorized metadata-only read across Web/CLI/MCP + operational surfaces; read grants neither export nor mutation | 6.5 / 6.6 | 8.1, 8.3, 8.4, 8.5 | AD-2, AD-19, AD-20, AD-29 | 🔒 blocked-external | G-3 FrontComposer adapters + G-5 identity |
| `fr-23` | FR-23 | Restore Archived Project — Preview + confirmation + Folder-before-activation ordering; NeedsAttention on partial; no owner-resource deletion | 7.14 | — | AD-3, AD-13, AD-23 | 🔒 blocked-external | G-1 + G-2 Folders owner |
| `fr-24` | FR-24 | Create Safe Diagnostic Export — projects.safe-diagnostic-export.v1; ≤1 MiB / ≤500 refs / ≤100 audit rows; separate permission; no cursor/retention; Chatbot cannot | 8.2 | — | AD-7, AD-19, AD-21, AD-26, AD-27 | 🟡 pending | — |

## Non-Functional Requirements (11)

| Key | IDs | Description | Primary | Supporting | ADs | Status | Blocker |
|---|---|---|---|---|---|---|---|
| `nfr-1` | NFR-1 | Security & privacy — Tenant/actor/action/target/version scoping; fail-closed on stale auth; metadata-only logs/telemetry/errors | 8.8 | 6.1, 6.5, 7.1 | AD-11, AD-13, AD-20 | 🔒 blocked-external | G-5 identity; authenticated live topology; Story 6.1 P2/P3/P4 query-safety and production-auth acceptance |
| `nfr-2` | NFR-2 | Encryption & key management — encryption in transit; platform-managed at rest; KMS rotation/revocation evidence; Projects owns no private keys | 8.11 | 8.6, 8.7 | AD-28 | 🔒 blocked-external | G-5 KMS/secret bindings; deployment environment |
| `nfr-3` | NFR-3 | Availability & recovery — 99.9% monthly; RTO 15 min; task resume or NeedsAttention within 5 min | 8.10 | 8.6 | AD-4, AD-9, AD-28 | 🔒 blocked-external | G-1 durable-task engine |
| `nfr-4` | NFR-4 | Durability & idempotency — RPO 0 committed events; Active never folderless; equivalent-retry-same-task; no silent drop/duplicate | 8.10 | 7.1, 7.15 | AD-4, AD-12 | 🔒 blocked-external | G-1 durable-task engine |
| `nfr-5` | NFR-5 | Performance & scale — 10k Projects/Tenant, 5k refs/Project, 100k audit; reads p95 <500ms (<1s max); admission p95 <500ms warm | 8.9 | 6.1, 6.7 | AD-14, AD-15, AD-27 | 🔒 blocked-external | Story 6.1 P0/P2/P4 runner, watermark, and accepted persisted-read evidence (P1 baseline normalized 2026-07-18); final scale gate remains Story 8.9 |
| `nfr-6` | NFR-6 | Pagination & export bounds — cursor default 50 / cap 200; export caps + 2 concurrent per Tenant | 8.9 | 8.2 | AD-19, AD-21, AD-27 | 🟡 pending | — |
| `nfr-7` | NFR-7 | Back-pressure & dependency control — 100 reads/s (burst 200), 20 mutations/s (burst 40), 1000 nonterminal tasks, 2 exports; 2s/10s timeouts; ≤3 retries/30s; structured overload | 8.9 | — | AD-27 | 🟡 pending | — |
| `nfr-8` | NFR-8 | Retention & transient data — terminal/idempotency ≥30 days; Preview/Confirmation 15-min expiry; audit ≥365 days; traces/exports not persisted | 8.1 | 8.2, 7.2, 7.4 | AD-4, AD-5, AD-21, AD-26 | 🟡 pending | — |
| `nfr-9` | NFR-9 | Accessibility — WCAG 2.2 AA across operator + Chatbot companion; keyboard/focus/AT/200%/320px; automated + authenticated manual evidence | 8.8 | 6.5, 8.3 | AD-34, AD-29 | 🔒 blocked-external | G-3 FrontComposer adapters; SEPARATE cross-repo Chatbot owner + pinned revision (unowned as of 2026-07-16) |
| `nfr-10` | NFR-10 | Compatibility — additive/serialization-tolerant contracts; name-only creation preserved; retirement gated; no event-history rewrite | 6.7 | 6.1, 7.15, 8.7 | AD-6, AD-16, AD-17, AD-22 | 🔒 blocked-external | 6.1-P1 version/source normalization complete (EventStore 3.70.1, 2026-07-18); 6.1-P4 entry-gate acceptance still pending; final cutover gate remains Story 6.7 |
| `nfr-11` | NFR-11 | Release evidence — all critical categories pass; failed/unexplained-skip critical case blocks; unavailable env = not-verified | 8.11 | 8.1, 8.6, 8.7, 8.8, 8.9, 8.10 | AD-25, AD-28, AD-30 | 🔒 blocked-external | G-4 hexalith-evidence tool (AD-30); terminal gate |

## Audit Findings (9 P1 + 7 P2) (16)

| Key | ID | Pri | Description | Primary | Supporting | ADs | Binding | Status | Blocker |
|---|---|---|---|---|---|---|---|---|---|
| `finding-arch-001` | ARCH-001 | P1 | Replace Projects-owned technical runtime with EventStore DomainService (platform-owned hosting/persistence/projections/topology) | 6.7 | 7.15, 8.7 | AD-1, AD-17, AD-24, AD-25 | inferred | 🔒 blocked-external | G-4 composition runner; command cutover completes in 7.15/8.7 |
| `finding-arch-002` | ARCH-002 | P1 | Restore Contracts/domain dependency direction; UI.Contracts as non-packable descriptor boundary | 6.7 | 8.7 | AD-16, AD-24 | explicit-source-old-numbering | 🟡 pending | — |
| `finding-sec-001` | SEC-001 | P1 | Remove production allow-all identity/authz stubs; require complete authentication validation (fail-fast on incomplete config) | 8.6 | 6.5, 6.6, 8.8 | AD-20, AD-28 | inferred | 🔒 blocked-external | G-5 identity bindings |
| `finding-client-001` | CLIENT-001 | P1 | Make shipped UI/CLI authenticate and propagate caller identity; no client supplies authoritative identity | 6.5 | 6.6 | AD-2, AD-20 | inferred | 🔒 blocked-external | G-3 + G-5 |
| `finding-rel-001` | REL-001 | P1 | Make proposal workflow and idempotency durable and recoverable | 7.12 | 7.1, 7.15, 8.10 | AD-4, AD-12 | inferred | 🔒 blocked-external | G-1 durable-task engine |
| `finding-agent-001` | AGENT-001 | P1 | Replace caller assertions with bound server-issued confirmations (Confirmation Artifacts) | 7.11 | 7.12, 8.5 | AD-5, AD-13, AD-29 | inferred | 🔒 blocked-external | G-1 confirmation record |
| `finding-id-001` | ID-001 | P1 | Use canonical ULIDs / framework-owned identifiers everywhere; foreign IDs opaque; legacy IDs readable | 6.7 | — | AD-18 | inferred | 🟡 pending | — |
| `finding-api-001` | API-001 | P1 | Reconcile OpenAPI and runtime validation, pagination, and error behavior | 6.7 | 8.9, 8.3, 8.4, 8.5 | AD-16, AD-19 | inferred | 🟡 pending | — |
| `finding-test-001` | TEST-001 | P1 | Add real persisted-boundary, restart, and critical E2E CI gates | 8.1 | 8.8, 8.10 | AD-30 | inferred-partial | 🔒 blocked-external | G-4 runner + blocking CI |
| `finding-perf-001` | PERF-001 | P2 | Replace unbounded tenant journal rebuilds and serial diagnostic enrichment | 8.9 | 6.3 | AD-14, AD-27 | inferred | 🟡 pending | — |
| `finding-ops-001` | OPS-001 | P2 | Implement truthful health and source-generated observability | 8.6 | — | AD-26, AD-28 | inferred | 🟡 pending | — |
| `finding-mcp-001` | MCP-001 | P2 | Publish accurate MCP schemas, annotations, and task semantics | 8.5 | — | AD-29 | inferred | 🔒 blocked-external | G-3 FrontComposer/MCP adapters |
| `finding-build-001` | BUILD-001 | P2 | Align dependency modes, Dapr runtime, central build policy, and immutable Actions (Builds owns NSwag/Fluxor versions) | 8.7 | — | AD-24, AD-25 | unassigned-in-source | 🔒 blocked-external | G-6 toolchain alignment (Dapr runtime↔SDK; Builds central versions) |
| `finding-ux-001` | UX-001 | P2 | Rebuild operator UI with Fluent V5 components, current tokens, and accessible composition | 8.3 | 8.8 | AD-34 | inferred | 🔒 blocked-external | G-3 FrontComposer; G-6 Fluent UI RC governance |
| `finding-code-001` | CODE-001 | P2 | Enforce one handwritten C# type per file | 8.7 | — | AD-16 | unassigned-in-source | 🟡 pending | — |
| `finding-cli-001` | CLI-001 | P2 | Reject unknown, duplicate, and unsupported CLI options deterministically | 8.4 | 6.6 | AD-19 | inferred | 🟡 pending | — |

## Critical Release-Evidence Categories (NFR-11 / AD-30) (12)

| Key | IDs | Description | Primary | Supporting | ADs | Status | Blocker |
|---|---|---|---|---|---|---|---|
| `release-authenticated-persisted-boundary` | NFR-11.persisted-boundary | Authenticated persisted-boundary evidence over the real supported platform path | 8.1 | 6.7 | AD-30, AD-17 | 🔒 blocked-external | G-4 runner + blocking CI |
| `release-cross-tenant-isolation` | NFR-11.cross-tenant | Cross-Tenant isolation — no surface renders another Tenant's data; denial indistinguishable from absence | 8.8 | 6.1 | AD-30, AD-19, AD-20 | 🔒 blocked-external | G-5 identity; authenticated live topology |
| `release-restart-concurrency` | NFR-11.restart-concurrency | Restart/concurrency — durable convergence across restart, two-instance, concurrency | 8.10 | 7.15 | AD-30, AD-4, AD-9 | 🔒 blocked-external | G-1 durable-task engine |
| `release-duplicate-delivery` | NFR-11.duplicate-delivery | Duplicate-delivery — at-least-once delivery converges without duplicate durable effect | 8.10 | — | AD-30, AD-4, AD-12 | 🔒 blocked-external | G-1 durable-task engine |
| `release-lost-response` | NFR-11.lost-response | Lost-response — equivalent retry after a lost response converges to the original durable truth | 8.10 | — | AD-30, AD-4, AD-5 | 🔒 blocked-external | G-1 durable-task engine |
| `release-accessibility` | NFR-11.accessibility | Accessibility — WCAG 2.2 AA automated + authenticated manual evidence for operator AND Chatbot companion journeys | 8.8 | 8.3, 6.5 | AD-30, AD-34 | 🔒 blocked-external | G-3; SEPARATE cross-repo Chatbot owner (unowned as of 2026-07-16) |
| `release-privacy` | NFR-11.privacy | Privacy — NoPayloadLeakage across every event/DTO/log/audit/surface/export | 8.8 | 6.3, 8.1, 8.2 | AD-30, AD-11, AD-26 | 🔒 blocked-external | authenticated live topology |
| `release-performance` | NFR-11.performance | Performance — authenticated performance at small/median/max supported scale | 8.9 | — | AD-30, AD-27 | 🟡 pending | — |
| `release-deployment` | NFR-11.deployment | Deployment — deployed version/environment + encryption/KMS evidence recorded | 8.11 | 8.7 | AD-30, AD-28 | 🔒 blocked-external | G-5 bindings; deployment environment |
| `release-smoke` | NFR-11.smoke, E-6 | Smoke / live E2E — authenticated live-topology smoke; supersede the failing 19-passed/56-failed run | 8.8 | 8.11 | AD-30 | ❌ failed | current live E2E 19 passed / 56 failed (E-6); must be superseded by a passing authenticated run |
| `release-rollback` | NFR-11.rollback | Rollback — reversible read/command routing + rollback drill reference | 8.11 | 6.7, 7.15 | AD-30, AD-17 | 🟡 pending | — |
| `release-stakeholder-acceptance` | NFR-11.stakeholder-acceptance, E-8 | Stakeholder acceptance — dated residual-risk disposition + terminal acceptance from Jerome and John; TERMINAL release gate | 8.11 | — | AD-30 | ⛔ blocked | release handoff BLOCKED (E-8); requires all critical evidence to pass first |

## Verification commands & evidence artifacts

_Every row's exact verification command and expected evidence artifact (full detail in the YAML)._

| Key | Verification command | Evidence artifact | Est. |
|---|---|---|---|
| `fr-1` | `dotnet tool run hexalith-module test --profile durable --filter Story=7.1` | `evidence/epic7/7.1-create.trx` | XL |
| `fr-2` | `dotnet tool run hexalith-module test --profile reads --filter Story=6.1` | `evidence/epic6/6.1-authorized-reads.trx` | M |
| `fr-3` | `dotnet tool run hexalith-module test --profile durable --filter Story=7.2` | `evidence/epic7/7.2-setup-update.trx` | M |
| `fr-4` | `dotnet tool run hexalith-module test --profile durable --filter Story=7.13` | `evidence/epic7/7.13-archive.trx` | M |
| `fr-5` | `dotnet tool run hexalith-module test --profile reads --filter Story=6.1` | `evidence/epic6/6.1-authorized-reads.trx` | M |
| `fr-6` | `dotnet tool run hexalith-module test --profile durable --filter Story=7.3` | `evidence/epic7/7.3-link-conversation.trx` | M |
| `fr-7` | `dotnet tool run hexalith-module test --profile durable --filter Story=7.4` | `evidence/epic7/7.4-move-conversation.trx` | L |
| `fr-8` | `dotnet tool run hexalith-module test --profile durable --filter Story=7.6` | `evidence/epic7/7.6-replace-folder.trx` | L |
| `fr-9` | `dotnet tool run hexalith-module test --profile durable --filter Story=7.7` | `evidence/epic7/7.7-link-file.trx` | M |
| `fr-10` | `dotnet tool run hexalith-module test --profile durable --filter Story=7.9` | `evidence/epic7/7.9-link-memory.trx` | M |
| `fr-11` | `dotnet tool run hexalith-module test --profile durable --filter Story=7.5\|7.8\|7.10` | `evidence/epic7/unlink-suite.trx` | M |
| `fr-12` | `dotnet tool run hexalith-module test --profile reads --filter Story=6.4` | `evidence/epic6/6.4-resolution-reads.trx` | L |
| `fr-13` | `dotnet tool run hexalith-module test --profile reads --filter Story=6.4` | `evidence/epic6/6.4-resolution-reads.trx` | L |
| `fr-14` | `dotnet tool run hexalith-module test --profile durable --filter Story=7.11` | `evidence/epic7/7.11-confirm-ambiguous.trx` | L |
| `fr-15` | `dotnet tool run hexalith-module test --profile durable --filter Story=7.12` | `evidence/epic7/7.12-confirm-proposed.trx` | L |
| `fr-16` | `dotnet tool run hexalith-module test --profile reads --filter Story=6.3` | `evidence/epic6/6.3-project-context.trx` | L |
| `fr-17` | `dotnet tool run hexalith-module test --profile reads --filter Story=6.3` | `evidence/epic6/6.3-project-context.trx` | L |
| `fr-18` | `dotnet tool run hexalith-module test --profile reads --filter Story=6.3` | `evidence/epic6/6.3-project-context.trx` | L |
| `fr-19` | `dotnet tool run hexalith-module test --profile durable --filter Story=7.1&Trait=metadata-classification` | `evidence/epic7/7.1-metadata-classification.trx` | XL |
| `fr-20` | `dotnet tool run hexalith-module test --profile reads --filter Story=6.2` | `evidence/epic6/6.2-conversation-start-setup.trx` | S |
| `fr-21` | `dotnet tool run hexalith-module test --profile ops --filter Story=8.1` | `evidence/epic8/8.1-task-audit.trx` | M |
| `fr-22` | `dotnet tool run hexalith-module test --profile web-reads,cli-reads --filter Story=6.5\|6.6` | `evidence/epic6/read-surfaces.trx` | L |
| `fr-23` | `dotnet tool run hexalith-module test --profile durable --filter Story=7.14` | `evidence/epic7/7.14-restore.trx` | L |
| `fr-24` | `dotnet tool run hexalith-module test --profile ops --filter Story=8.2` | `evidence/epic8/8.2-safe-export.trx` | L |
| `nfr-1` | `dotnet tool run hexalith-module test --profile e2e-auth --filter Story=8.8&Trait=security` | `evidence/epic8/8.8-security.trx` | XL |
| `nfr-2` | `dotnet tool run hexalith-module test --profile deploy --filter Story=8.11&Trait=encryption` | `evidence/epic8/8.11-encryption-kms.json` | L |
| `nfr-3` | `dotnet tool run hexalith-module test --profile resilience --filter Story=8.10&Trait=availability` | `evidence/epic8/8.10-availability.trx` | L |
| `nfr-4` | `dotnet tool run hexalith-module test --profile resilience --filter Story=8.10&Trait=durability` | `evidence/epic8/8.10-durability.trx` | L |
| `nfr-5` | `dotnet tool run hexalith-module test --profile perf --filter Story=8.9` | `evidence/epic8/8.9-performance.json` | L |
| `nfr-6` | `dotnet tool run hexalith-module test --profile perf --filter Story=8.9&Trait=paging` | `evidence/epic8/8.9-paging.trx` | L |
| `nfr-7` | `dotnet tool run hexalith-module test --profile perf --filter Story=8.9&Trait=backpressure` | `evidence/epic8/8.9-backpressure.trx` | L |
| `nfr-8` | `dotnet tool run hexalith-module test --profile ops --filter Story=8.1&Trait=retention` | `evidence/epic8/8.1-retention.trx` | M |
| `nfr-9` | `dotnet tool run hexalith-module test --profile e2e-auth --filter Story=8.8&Trait=accessibility` | `evidence/epic8/8.8-accessibility.json` | XL |
| `nfr-10` | `dotnet tool run hexalith-module test --profile read-cutover --filter Story=6.7` | `evidence/epic6/6.7-read-cutover.json` | L |
| `nfr-11` | `dotnet tool run hexalith-evidence validate _bmad-output/planning-artifacts/implementation-readiness-traceability-matrix.yaml` | `evidence/epic8/8.11-release-evidence.json` | L |
| `finding-arch-001` | `dotnet tool run hexalith-module test --profile read-cutover --filter Story=6.7` | `evidence/epic6/6.7-read-cutover.json` | L |
| `finding-arch-002` | `dotnet tool run hexalith-module test --profile read-cutover --filter Story=6.7&Trait=contracts` | `evidence/epic6/6.7-contracts.trx` | M |
| `finding-sec-001` | `dotnet tool run hexalith-module test --profile ops --filter Story=8.6&Trait=admission` | `evidence/epic8/8.6-admission.trx` | M |
| `finding-client-001` | `dotnet tool run hexalith-module test --profile web-reads,cli-reads --filter Story=6.5\|6.6&Trait=identity` | `evidence/epic6/authenticated-consumers.trx` | M |
| `finding-rel-001` | `dotnet tool run hexalith-module test --profile durable --filter Story=7.12` | `evidence/epic7/7.12-confirm-proposed.trx` | L |
| `finding-agent-001` | `dotnet tool run hexalith-module test --profile durable --filter Story=7.11` | `evidence/epic7/7.11-confirm-ambiguous.trx` | L |
| `finding-id-001` | `dotnet tool run hexalith-module test --profile read-cutover --filter Story=6.7&Trait=identity` | `evidence/epic6/6.7-ulid-identity.trx` | M |
| `finding-api-001` | `dotnet tool run hexalith-module test --profile read-cutover --filter Story=6.7&Trait=openapi` | `evidence/epic6/6.7-openapi.trx` | M |
| `finding-test-001` | `dotnet tool run hexalith-module test --profile ci-gates --filter Story=8.1` | `evidence/epic8/8.1-persisted-boundary.trx` | L |
| `finding-perf-001` | `dotnet tool run hexalith-module test --profile perf --filter Finding=PERF-001` | `evidence/epic8/8.9-perf-001.json` | M |
| `finding-ops-001` | `dotnet tool run hexalith-module test --profile ops --filter Story=8.6` | `evidence/epic8/8.6-health.trx` | M |
| `finding-mcp-001` | `dotnet tool run hexalith-module test --profile mcp --filter Story=8.5` | `evidence/epic8/8.5-mcp.trx` | M |
| `finding-build-001` | `dotnet tool run hexalith-module test --profile supply-chain --filter Story=8.7` | `evidence/epic8/8.7-build-001.trx` | M |
| `finding-ux-001` | `dotnet tool run hexalith-module test --profile web-ops --filter Story=8.3` | `evidence/epic8/8.3-ux-001.trx` | L |
| `finding-code-001` | `dotnet tool run hexalith-module test --profile supply-chain --filter Finding=CODE-001` | `evidence/epic8/8.7-code-001.trx` | S |
| `finding-cli-001` | `dotnet tool run hexalith-module test --profile cli --filter Story=8.4&Trait=option-validation` | `evidence/epic8/8.4-cli-001.trx` | S |
| `release-authenticated-persisted-boundary` | `dotnet tool run hexalith-module test --profile ci-gates --filter Category=persisted-boundary` | `evidence/release/persisted-boundary.trx` | L |
| `release-cross-tenant-isolation` | `dotnet tool run hexalith-module test --profile e2e-auth --filter Category=cross-tenant` | `evidence/release/cross-tenant-isolation.trx` | L |
| `release-restart-concurrency` | `dotnet tool run hexalith-module test --profile resilience --filter Category=restart-concurrency` | `evidence/release/restart-concurrency.trx` | L |
| `release-duplicate-delivery` | `dotnet tool run hexalith-module test --profile resilience --filter Category=duplicate-delivery` | `evidence/release/duplicate-delivery.trx` | M |
| `release-lost-response` | `dotnet tool run hexalith-module test --profile resilience --filter Category=lost-response` | `evidence/release/lost-response.trx` | M |
| `release-accessibility` | `dotnet tool run hexalith-module test --profile e2e-auth --filter Category=accessibility` | `evidence/release/accessibility.json` | L |
| `release-privacy` | `dotnet tool run hexalith-module test --profile e2e-auth --filter Category=privacy-leakage` | `evidence/release/privacy-leakage.trx` | M |
| `release-performance` | `dotnet tool run hexalith-module test --profile perf --filter Category=performance` | `evidence/release/performance.json` | L |
| `release-deployment` | `dotnet tool run hexalith-module test --profile deploy --filter Category=deployment` | `evidence/release/deployment.json` | L |
| `release-smoke` | `dotnet tool run hexalith-module test --profile e2e-auth --filter Category=smoke` | `evidence/release/live-e2e.trx` | L |
| `release-rollback` | `dotnet tool run hexalith-module test --profile deploy --filter Category=rollback` | `evidence/release/rollback-drill.json` | M |
| `release-stakeholder-acceptance` | `dotnet tool run hexalith-evidence validate _bmad-output/planning-artifacts/implementation-readiness-traceability-matrix.yaml` | `evidence/release/stakeholder-acceptance.md` | M |

## Notes & non-authoritative bindings

- **`fr-23`** — NEW FR (2026-07-15 rebaseline). Restore counterpart to FR-4.
- **`fr-24`** — NEW FR (2026-07-15/16 rebaseline). Release-blocking.
- **`nfr-2`** — Previously unmapped (readiness M2). Owner assigned per SCP §4.6.
- **`nfr-7`** — Previously unmapped (readiness M2). Owner assigned per SCP §4.6.
- **`nfr-8`** — Previously unmapped (readiness M2). Owner assigned per SCP §4.6.
- **`nfr-9`** — Chatbot companion a11y/integration is cross-repository (Hexalith.Chatbot); requires separately approved owner + pinned revision. Its absence blocks Projects release.
- **`nfr-11`** — Terminal release gate. Cannot complete by recording a blocker.
- **`finding-arch-002`** — Readiness addendum §4.2 named OLD 'Story 6.2 (E-4/E-5)' for the Projects.UI.Contracts boundary; under the rebaseline that boundary work is Story 6.7 (contract cutover) + 8.7 (package graph). Confirm at de-placeholdering.
- **`finding-build-001`** — No epic/story explicitly assigned in source; mapped to 8.7 per SCP §4.3 shared-build centralization. Confirm at de-placeholdering.
- **`finding-code-001`** — Audit sequences this in Phase 3 (after package/UI/MCP ownership settles). Mapped to 8.7 (source structure). Confirm at de-placeholdering.
- **`release-smoke`** — HONEST current state is FAILED, not skipped. Do not represent as passing.

---
_Generated from the YAML on 2026-07-16. Do not hand-edit divergently; edit the YAML and regenerate._
