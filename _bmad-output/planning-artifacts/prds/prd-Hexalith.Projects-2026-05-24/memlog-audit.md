# Memlog Audit

Audit scope: all 39 current entries in `.memlog.md`, reconciled against the current `prd.md` and `addendum.md`.

| Entry | Entry gist | Disposition | Exact section reference | Mismatch / note |
| ---: | --- | --- | --- | --- |
| 1 | Internal Chatbot platform module providing a durable Tenant-aware AI workspace, not generic project management. | Captured in PRD | `prd.md` §0 **Document Purpose**; §1 **Vision**; §5 **Non-Goals (Explicit)** | None. |
| 2 | Primary user needs continuity across Conversations and resources without rebuilding context. | Captured in PRD | `prd.md` §2.1 **Primary User**; §2.3 **Jobs To Be Done** | None. |
| 3 | Projects owns metadata, setup, lifecycle, and associations; Conversations, Folders, and Memories retain payload and authorization ownership. | Captured in PRD | `prd.md` §3 **Glossary**; §4.2 **Context References**; §5 **Non-Goals (Explicit)** | None. |
| 4 | All Project operations and context assembly are Tenant-scoped and fail closed when authorization or resource evidence is uncertain. | Captured in PRD | `prd.md` §4.2–§4.4; §7 (NFR-1) | None. |
| 5 | A Conversation belongs to exactly one Project; moving it is explicit and auditable. | Captured in PRD | `prd.md` §3 (`Conversation`); §4.2 (FR-6, FR-7); §4.6 (FR-21) | None. |
| 6 | Each v1 Project has one canonical Folder, with same-name creation when absent and explicit replacement only. | Captured in PRD | `prd.md` §3 (`Project Folder`); §4.1 (FR-1); §4.2 (FR-8); §7 (NFR-4) | None; Active visibility is Folder- and read-model-gated. |
| 7 | Lifecycle is exactly Active/Archived; Archived is retained and excluded from automatic resolution by default. | Captured in PRD | `prd.md` §3 (`Project Lifecycle State`); §4.1 (FR-4, FR-5); §4.3 (FR-12) | None. |
| 8 | Name-only creation originally defaulted immediately to Active; inferred creation required confirmation. | Superseded | `.memlog.md` entry 37; `prd.md` §4.1 (FR-1); §4.3 (FR-15); §4.5 (FR-19); §10 | The immediate/default-Active clause is explicitly superseded; the name-only and inferred-confirmation intent remains captured. |
| 9 | Resolution returns NoMatch, SingleCandidate, or MultipleCandidates with reason codes and no silent ambiguous attachment. | Captured in PRD | `prd.md` §3 (`Resolution Result`, `Resolution Reason Code`); §4.3 (FR-12–FR-14) | None. |
| 10 | Project Context carries authorized references/setup/metadata, excludes foreign payloads, and reports stale or excluded resources. | Captured in PRD | `prd.md` §3 (`Project Context`); §4.4 (FR-16–FR-18) | None. |
| 11 | Project Setup controls Conversation behavior/context policy, not provider internals, and rejects unsafe content. | Captured in PRD | `prd.md` §3 (`Project Setup`); §4.1 (FR-3); §4.5 (FR-19, FR-20) | None. |
| 12 | Audit, diagnostics, and operator inspection are Tenant-scoped, authorization-gated, and metadata-only. | Captured in PRD | `prd.md` §4.6 (FR-21, FR-22, FR-24); §7 (NFR-1) | None. |
| 13 | MVP excludes indexing, payload/transcript storage, standalone UI, generic project management, and cross-Tenant sharing. | Captured in PRD | `prd.md` §5 **Non-Goals (Explicit)**; §6.2 **Out of Scope for MVP** | None. |
| 14 | Success covers usable context, useful resolution/new-Project proposals, zero unauthorized leakage, and internal p95 metadata latency under 500 ms. | Captured in PRD | `prd.md` §7 (NFR-3, NFR-5); §8 (SM-1, SM-3, SM-4, SM-7, SM-C1); §10 | None; SM-7 now measures accepted resolution/proposal usefulness directly. |
| 15 | FR-22 remains operator read access; FR-24 owns Safe Diagnostic Export; mutation authority stays with action-specific FRs and a role matrix. | Captured in PRD | `prd.md` §4.6 (FR-22, FR-24); §4.7; §10 | None. |
| 16 | Product roles and operation-specific authority prevent UI surface or service identity from expanding permission. | Captured in PRD | `prd.md` §2.2 **Runtime Roles**; §4.7; §7 (NFR-1) | None. |
| 17 | Durable Task vocabulary, terminality, nonterminal NeedsAttention, lost-response behavior, and cancellation cutoff. | Captured in PRD | `prd.md` §3 (`Durable Task`, `Task Status`, `Read-Model-Confirmed Completion`); §7 (NFR-3, NFR-4); `addendum.md` §§1–2 | None; mechanism detail is correctly routed to the addendum. |
| 18 | Internal-launch scale, page size, metadata latency, and task-admission performance envelope. | Captured in PRD | `prd.md` §4.1 (FR-5); §7 (NFR-5, NFR-6); §8 (SM-4) | None. |
| 19 | Availability, RPO/RTO, task recovery, fail-closed mutation, and idempotent-retry reliability envelope. | Captured in PRD | `prd.md` §7 (NFR-1, NFR-3, NFR-4); §8 (SM-1, SM-2) | None. |
| 20 | Task/idempotency, confirmation, audit, trace, and export retention/replay policy. | Captured in PRD | `prd.md` §3 (`Confirmation Artifact`, `Idempotency Key`, `Resolution Trace`); §4.3 (FR-14); §4.6 (FR-21, FR-24); §7 (NFR-8) | None. |
| 21 | Encryption/key management, server-derived authority, confirmation integrity, fail-closed evidence, and security release proof. | Captured in PRD | `prd.md` §3 (`Confirmation Artifact`); §7 (NFR-1, NFR-2, NFR-11); §8 (SM-3, SM-6) | None. |
| 22 | Per-Tenant throughput/concurrency, dependency timeout/retry, overload guidance, and accepted-task guarantees. | Captured in PRD | `prd.md` §7 (NFR-6, NFR-7); §8 (SM-2) | None. |
| 23 | WCAG 2.2 AA behavior and automated plus authenticated manual accessibility evidence for Chatbot/operator journeys. | Captured in PRD | `prd.md` §2.5 (UJ-3); §4.3 (FR-14, FR-15); §7 (NFR-9); §8 (SM-5) | None. |
| 24 | Safe Diagnostic Export authorization, surfaces, audit, bounds, deterministic truncation, upstream gaps, and non-retention. | Captured in PRD | `prd.md` §4.6 (FR-24); §7 (NFR-6, NFR-8); `addendum.md` §3 | None. |
| 25 | Folderless/invalid-Folder restore requires a bound preview choice; partial Folder creation becomes recoverable NeedsAttention without deleting Folders-owned data. | Captured in PRD | `prd.md` §2.5 (UJ-5); §4.6 (FR-23); `addendum.md` §§1, 5 | None. |
| 26 | Confirmation boundary for consequential/inferred actions; actor-selected additive links remain idempotent tasks without a second confirmation. | Captured in PRD | `prd.md` §4.1 (FR-4); §4.2 (FR-6–FR-11); §4.3 (FR-14, FR-15); §4.6 (FR-23); §6.1 | None. |
| 27 | Pending creation is task-only and hidden from Project reads; authorized parties can poll, cancel, inspect, or reconcile within role and retention limits. | Captured in PRD | `prd.md` §4.1 (FR-1, FR-2, FR-5); §4.6 (FR-22); §7 (NFR-8); `addendum.md` §§1–2 | None. |
| 28 | Consequential/security/mutation/reconciliation/export/receipt audit taxonomy; transient workflow detail remains telemetry; retries do not duplicate audit. | Captured in PRD | `prd.md` §4.4 (FR-17, FR-18); §4.6 (FR-21) | None. |
| 29 | Release gates cover folder integrity, disclosure, recovery, read-model confirmation, and required quality evidence; failed/unknown critical evidence blocks release. | Captured in PRD | `prd.md` §7 (NFR-4, NFR-11); §8 (SM-1–SM-6) | None. |
| 30 | User authorized automatic adoption of remaining recommendations for this drafting update. | Deliberately set aside | No product-section reference; retained only in `.memlog.md`. | None; this is workflow authority, not a product or downstream-design requirement. |
| 31 | Canonical creation requires system-supplied Metadata Classification; legacy unversioned name-only requests remain throughout v1. | Captured in PRD | `prd.md` §3 (`Metadata Classification`); §4.1 (FR-1); §4.5 (FR-19); §7 (NFR-10); `addendum.md` §4.1 | None. |
| 32 | Chatbot owns accessible presentation; Projects owns versioned workflow/context contracts; release requires authenticated compatibility evidence. | Captured in addendum | `addendum.md` §6 **Chatbot Companion Contract**; supported by `prd.md` §0, §2.2, and NFR-9–NFR-11 | None. |
| 33 | Resolution diagnostics recompute current authorized metadata; traces are transient; only confirmed outcomes are audited; missing/stale evidence is explicit. | Captured in PRD | `prd.md` §3 (`Resolution Trace`); §4.3 (FR-12, FR-13); §4.4 (FR-17, FR-18); §4.6 (FR-21) | None. |
| 34 | PRD revision added Folder-gated tasks, confirmations, cross-context recovery, roles, FR-23/FR-24, measurable NFRs, and release metrics while preserving FR-1–FR-22. | Captured in PRD | `prd.md` §4.1–§4.7 (FR-1–FR-24); §7 (NFR-1–NFR-11); §8 | None. |
| 35 | Addendum created for workflow/idempotency, export contract, package boundaries, migration, Chatbot ownership, and verification routing. | Captured in addendum | `addendum.md` §§1–8 | None. |
| 36 | Final reconciliation gaps were closed through SM-7 plus ownership, identity, metadataClass, UI.Contracts, shared-build, Unicode, live-lane, readiness, and evidence additions. | Captured in PRD + addendum | `prd.md` §8 (SM-7); `addendum.md` §1.1, §§4.1–4.3, §7, §8.1 | None. |
| 37 | Immediate/default Active creation is superseded; visibility requires one authorized Folder and read-model-confirmed completion. | Captured in PRD | `prd.md` §4.1 (FR-1); §4.5 (FR-19); §10 | None; this is the governing replacement for entry 8. |
| 38 | July export placement/count is superseded: FR-22 read, FR-23 restore, FR-24 export, 24 total while preserving FR-1–FR-22. | Captured in PRD + addendum | `prd.md` §4.6 (FR-22–FR-24); §10; `addendum.md` §8.2 | None. |
| 39 | Formal traceability closed through the Story 5.13 Contracts release gate and explicit export/count supersession trace. | Captured in addendum | `addendum.md` §4.2 (final paragraph); §8.2 | None. |

## Summary

- Captured in PRD: 32
- Captured in addendum: 3
- Captured in PRD + addendum: 2
- Superseded by a later memlog decision: 1
- Deliberately set aside as workflow-only: 1
- Total current memlog entries: 39
- Substantive mismatches: 0
