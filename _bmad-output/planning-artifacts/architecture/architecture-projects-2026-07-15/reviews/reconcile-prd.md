---
review: prd-reconciliation
artifact: ../ARCHITECTURE-SPINE.md
authority:
  - _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md
  - _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/addendum.md
result: fail
date: 2026-07-16
---

# PRD Reconciliation — Hexalith.Projects Architecture Spine

## Verdict

**FAIL — targeted reconciliation is required before finalization.** The draft has strong structural coverage of Folder-gated visibility, bounded-context ownership, durable work, confirmation/idempotency, migration, diagnostics, platform extraction, and release containment. It nevertheless omits several PRD facts whose exact shape must constrain independently built contracts, adapters, projections, evidence, and platform modules.

`Pass` below means the spine assigns a non-divergent architecture owner/rule while the PRD remains the observable product authority; it does not require the spine to repeat every acceptance bullet. `Partial` or `Fail` means an architecture-owned mechanism, exact vocabulary/bound, or cross-module invariant from the PRD package did not land sufficiently.

## Actionable Findings

### R-PRD-1 — Critical — Canonical creation Metadata Classification contract did not land

**Source:** `prd.md` §§2.1, FR-1, FR-19; `addendum.md` §4.1 and §7.1.

AD-8, AD-16, and the FR-1/FR-19 capability rows do not bind the canonical Create Project shape to its required server-supplied Metadata Classification. The spine omits the exact vocabulary `public_metadata`, `tenant_sensitive`, `credential_sensitive`, `secret`; the rule that this field is integration-policy-derived rather than inferred from user text; authorization-before-protected-parsing; the exact safe rejection contract `400 ValidationFailure` with `details.rejectedField = projectMetadata.metadataClass`; no command submission on failure; and reuse of one `SensitiveMetadataTierValidator` by direct creation and proposal confirmation.

**Required correction:** Add one contract/canonicalization invariant or extend AD-16 and the creation map with all of the above. Explicitly state that `secret` is only a label and never permission to store secret content.

### R-PRD-2 — High — Role/action authority matrix is not architecture-binding

**Source:** `prd.md` §§3.2 and 6.1–6.6.

AD-2 and AD-20 define authority ownership but not the allowed operation matrix. Independent Web, CLI, MCP, and Chatbot adapters could therefore grant different actions. The missing distinctions include:

- only Project Users confirm ambiguous resolution or proposed creation;
- Project Users, Tenant Operators, and Tenant Project Administrators may archive/restore, subject to action authorization;
- only Project Users perform additive Conversation/File/Memory links and initial Folder setting;
- Project Users and Tenant Project Administrators may move/unlink/replace Folder; Tenant Operators may not;
- Tenant Operators and Tenant Project Administrators may inspect pre-activation safe task status, but only Tenant Project Administrators reconcile;
- Project Users see only their own permitted task status through Chatbot;
- Safe Diagnostic Export is separately authorized for Tenant Operators/Administrators and unavailable to Chatbot;
- a delegated service/workflow caller inherits the actor's authority and never gains confirmation authority.

**Required correction:** Add a compact role/operation convention or matrix governed by AD-2/AD-20 and consumed unchanged by every adapter.

### R-PRD-3 — High — Observable response and resolution semantics are referenced but incompletely fixed

**Source:** `prd.md` glossary, §5, FR-2, FR-5, FR-12–FR-20; `addendum.md` §6.

The consistency table says to use exact PRD vocabularies but omits two shared vocabularies that adapters and read models must not independently invent: inclusion status (`Included`, `Excluded`) and Resolution Reason Codes (`ConversationLinked`, `ProjectFolderMatched`, `FileReferenceMatched`, `MemoryMatched`, `MetadataMatched`). More importantly, no rule binds the §5 field/transition semantics across list, open, resolution, context, Conversation-start, and proposal recovery:

- the logical fields `responseState`, `asOf`, `projectVersion`, conditional `resolutionResult`, `components`, and `recoveryActions`;
- `Partial` is usable only when Project, Folder, Setup, and first-response authorization evidence are `Current` and every optional omission is represented;
- `Unavailable` and `Denied` block Conversation first-response admission;
- refresh is a fresh recomputation with a new snapshot and never rewrites an earlier response;
- `Denied` discloses no protected detail;
- `NoMatch`, `SingleCandidate`, and `MultipleCandidates` never select a candidate for an `Unavailable` or `Denied` response.

AD-19's HTTP mapping can coexist with logical `Denied`, but should state whether the safe `404` body preserves or suppresses that logical code so generated clients do not diverge.

**Required correction:** Extend the response-vocabulary convention and add a compact response-state invariant that fixes these shared field and transition semantics without duplicating wire casing decisions.

### R-PRD-4 — High — NFR-5 performance and capacity envelope is materially incomplete

**Source:** `prd.md` NFR-5 and SM-4.

AD-15/AD-27 preserve the 5,000-reference cap, but the spine omits:

- 10,000 Projects per Tenant;
- 100,000 retained audit records per Project;
- metadata read p95 under 500 ms at 1,000 Projects and 500 references;
- metadata read p95 under 1 second at the supported maximum;
- Durable Task admission p95 under 500 ms under authenticated warm steady state with required dependencies available.

These are not story-level details: they determine projection shape, index strategy, load fixtures, and whether live fan-out is permissible.

**Required correction:** Amend AD-14/AD-15/AD-27 or add a performance convention containing the complete NFR-5 data shapes and thresholds.

### R-PRD-5 — High — Durable audit whitelist/exclusions are not precise enough

**Source:** `prd.md` FR-21; `addendum.md` §§1 and 7.2.

AD-26 says platform task/confirmation/reconciliation/export-security events project audit truth, but that broad wording can accidentally persist operational task traffic that FR-21 explicitly excludes. The spine does not bind the required metadata-only audit inventory (admission and terminal outcome; confirmation use/cancellation/stale/replay/tamper; authorization denial; confirmed mutations/outcomes; manual reconciliation; export attempt/outcome; stable upstream receipt IDs) or the exclusion inventory (intermediate states, polls, retries, dependency latency, notifications, unused expiry, and read-only Resolution Traces). It also does not explicitly prevent equivalent idempotent retries from duplicating audit events.

**Required correction:** Amend AD-26 with a whitelist/exclusion rule and stable deduplication identity. Keep operational telemetry and durable audit separate.

### R-PRD-6 — High — Outcome-measurement semantics are too thin to produce SM-7/SM-8/SM-C4 consistently

**Source:** `prd.md` §8; `addendum.md` §7.2.

AD-26 chooses the analytics channel and rolling 30-day store but omits the measurement contract that different Projects/Chatbot/platform teams could calculate incompatibly: eligible-resolution and eligible-resumption denominators, the continuity-success and context-correction definitions, 15-minute acceptance window, the 90% thresholds for SM-7/SM-8, the 5% ceiling for SM-C4, exclusions versus denominator-retained degraded/abandoned outcomes, required report counts, and `insufficient volume` rather than 100% when no eligible resumptions exist.

**Required correction:** Add a measurement convention tied to AD-26 that adopts the PRD definitions and thresholds verbatim and makes deterministic fixture parity part of AD-30 evidence.

### R-PRD-7 — Medium — Setup update, refresh, and first-response mutation boundaries need explicit landing

**Source:** `prd.md` FR-3, FR-18, FR-20.

The capability map assigns these features but no invariant explicitly requires Project Setup updates to be idempotent, durable, and observable from the authoritative read model; context refresh to be read-only and produce no maintenance audit event; or Chatbot first-response admission to accept only `Complete`/`Partial` and block on `Unavailable`/`Denied` without live fan-out to every bounded context.

**Required correction:** Add these three transition boundaries to AD-14/AD-19 or the response convention. This can be one concise rule.

### R-PRD-8 — High — Historical name-only creation compatibility is under-specified

**Source:** `prd.md` §§2.1, FR-1, NFR-10; `addendum.md` §§4.1, 5, and 7.1.

AD-6/AD-16/Deferred cover compatibility generally but do not bind the special v1 rule: only the historical unversioned name-only request shape receives compatibility treatment; canonical creation requires classification; historical v1 data and the legacy request remain readable/accepted throughout v1; retirement requires a major version, migration notice, usage evidence, compatibility tests, and rollback evidence. Without this, adapters could silently broaden the legacy path or retire it early.

**Required correction:** Extend AD-16 or AD-17 with the exact compatibility boundary and retirement gate.

### R-PRD-9 — Medium — Task status meanings and retention have two unresolved edges

**Source:** `prd.md` §5, FR-15, NFR-8; `addendum.md` §§1–2.

AD-4 correctly fixes the eight statuses and terminality, but does not define `Rejected` as a known domain/owner denial versus `Failed` as a known non-retryable technical failure. It also does not require active tasks to remain pollable until terminal, or define the post-irreversible-cancellation response as a conflict/safe current-status result. These distinctions affect platform task implementations and generated adapters.

**Required correction:** Add those three semantics to AD-4/AD-19 while retaining `NeedsAttention` as nonterminal and terminal-state immutability.

## Functional Requirement Coverage

| Requirement | Result | Architecture landing / gap |
| --- | --- | --- |
| FR-1 Create Project | Partial | Folder-first, task truth, visibility, idempotency, recovery: AD-3/4/5/8/12/13/22. Missing canonical classification and exact legacy path: R-PRD-1, R-PRD-8. |
| FR-2 Open Project | Partial | AD-3/14/19/20. Shared §5 response-state field/transition semantics need R-PRD-3. |
| FR-3 Update Project Setup | Partial | AD-15/16 and capability map assign ownership; authoritative read-model/idempotent mutation boundary needs R-PRD-7. |
| FR-4 Archive Project | Pass | AD-3/4/5/12/13/15/19 cover Preview, confirmation, task, lifecycle, authority, and completion. |
| FR-5 List Projects | Partial | AD-3/14/19/20/27 cover visibility, filtering boundary, cursor, and authorization; shared response semantics need R-PRD-3. |
| FR-6 Link Conversation | Pass | AD-4/9/10/12/14 preserve actor-selected no-second-confirmation workflow and Conversations authority. |
| FR-7 Move Conversation | Pass | AD-4/5/10/12/13 cover bound confirmation, single owner truth, receipt, replay, and recovery. |
| FR-8 Set Project Folder | Pass | AD-3/5/8/11–15/23 cover single Folder, replacement confirmation, owner authority, and read-model completion. |
| FR-9 Link File Reference | Pass | AD-11/12/15 preserve reference-only storage, Folders authority, and idempotent orchestration. |
| FR-10 Link Memory | Pass | AD-11/12/15 preserve reference-only storage, Memories authority, and idempotent orchestration. |
| FR-11 Unlink Context Reference | Pass | AD-5/10–15 preserve confirmation/task, association-only removal, and owner authority. |
| FR-12 Resolve from Conversation | Partial | AD-7/10/14/19 preserve current authorized resolution; exact reasons/response constraints need R-PRD-3. |
| FR-13 Resolve from Attachments | Partial | AD-7/11/14/19 preserve metadata-only current owner evidence; exact reasons/response constraints need R-PRD-3. |
| FR-14 Confirm Ambiguous Project | Partial | AD-5/10/13/14/19/29 preserve no silent mutation and confirmed task path; role matrix and exact response vocabulary need R-PRD-2/3. |
| FR-15 Propose New Project | Partial | AD-5/8/12/13/19 preserve Preview, Folder-first task, and recovery; classification validator parity and role constraint need R-PRD-1/2. |
| FR-16 Get Project Context | Partial | AD-3/7/11/14/19/20 preserve active-only, metadata-only, current authority; component/state semantics need R-PRD-3. |
| FR-17 Explain Context Selection | Pass | AD-7 and AD-26 fix current request-scoped nonpersistent traces and confirmed-outcome-only audit boundary. |
| FR-18 Refresh Project Context | Partial | AD-7/11/14 assign recomputation; read-only/no-maintenance-audit and snapshot transitions need R-PRD-3/7. |
| FR-19 Validate Project Setup | Fail | General contract validation is assigned, but the canonical Metadata Classification contract and rejection path are missing: R-PRD-1. |
| FR-20 Conversation-Start Setup | Partial | AD-3/14/19/20 assign snapshot query; first-response admission boundary needs R-PRD-3/7. |
| FR-21 Record Audit Events | Partial | AD-21/26/30 establish ownership, retention, metadata boundary; exact inventory/exclusions/deduplication need R-PRD-5. |
| FR-22 Operator Read Access | Partial | AD-2/14/19/20/29 assign safe platform adapters; exact role/task/reconciliation permissions need R-PRD-2. |
| FR-23 Restore Archived Project | Pass | AD-3–5/8/12/13/23 preserve Folder validity while Archived, task truth, recovery, and read-model-confirmed activation. |
| FR-24 Safe Diagnostic Export | Pass | AD-7/19/21/26/27 preserve separate authorization, synchronous non-retention, 1 MiB/500/100 bounds, two leases, deterministic truncation, safe gaps, and audit. |

## Non-Functional Requirement Coverage

| Requirement | Result | Architecture landing / gap |
| --- | --- | --- |
| NFR-1 Security/privacy | Pass | AD-2/7/11/13/19/20/26/28/29 bind Tenant/actor/action scope, fail-closed evidence, safe disclosure, and metadata-only channels. |
| NFR-2 Encryption/key management | Pass | AD-20/28 and G-5 bind platform identity, encryption, KMS/secrets, rotation/revocation, and fail-fast configuration. |
| NFR-3 Availability/recovery | Pass | AD-4/9/12/27/28 preserve 99.9%, 15-minute RTO, five-minute accepted-task recovery/NeedsAttention, and dependency behavior. |
| NFR-4 Durability/idempotency | Pass | AD-3–5/8/12/13/15/17/22/23/28 preserve RPO 0, no Active folderless Project, one writer, replay safety, and no silent task loss/duplication. |
| NFR-5 Performance/scale | Fail | Only the 5,000-reference cap lands. Project/audit capacities and all p95 data-shape targets are absent: R-PRD-4. |
| NFR-6 Pagination/export bounds | Pass | AD-21/27 and conventions preserve default 50/max 200 pages, full export bounds, and two concurrent exports. |
| NFR-7 Back-pressure/dependency control | Pass | AD-27 preserves 100/200 reads, 20/40 admissions, 1,000 tasks, two exports, 2s/10s timeouts, and three retries within 30s. |
| NFR-8 Retention/transience | Partial | AD-5/7/21/26 preserve 15-minute confirmations, 30-day/lifetime idempotency, 365-day audit, and nonpersistent traces/exports; active-task polling edge needs R-PRD-9. |
| NFR-9 Accessibility | Pass | AD-2/29/30 assign Chatbot/FrontComposer ownership and fail-closed evidence; the PRD remains binding for WCAG 2.2 AA, keyboard/focus/announcement, 200%/320px, and authenticated manual evidence. |
| NFR-10 Compatibility | Partial | AD-6/16/17/18/22 preserve additive contracts/events, legacy identifiers, adapters, staged cutover, no rewrite; exact unversioned name-only boundary/retirement gate needs R-PRD-8. |
| NFR-11 Release evidence | Pass | AD-25/28/30 and G-1–G-6 preserve persisted/authenticated/local lanes, repository/version gates, no false pass, `READY` containment, rollback, and terminal release disposition. |

## Strongly Reconciled Areas

- The final 24-FR structure is preserved: FR-22 is operator read, FR-23 is restore, and FR-24 owns Safe Diagnostic Export.
- Exactly-one-Folder visibility and Folder-first creation are stronger and more implementation-convergent than the minimum PRD wording while remaining compatible.
- Project lifecycle and Durable Task status remain separate with the exact eight task status tokens.
- Confirmation, idempotency, lost-response recovery, owner receipts, forward recovery, and `NeedsAttention` are coherently assigned across Projects and platform modules.
- Conversations/Folders/Memories remain systems of record; Projects stores associations/rebuildable indexes without foreign payload ownership.
- Safe Diagnostic Export carries every product bound and the non-retention/separate-authorization invariant.
- Additive event evolution, shadow reads, single-writer cutover, and no history rewrite satisfy the migration package.
- `Projects.UI.Contracts`, shared-build pins, technical-layer extraction, local run/test parity, MCP containment, and readiness/release freeze all have explicit owners and gates.

## Finalization Condition

Resolve R-PRD-1 through R-PRD-9, then rerun this reconciliation. No PRD-level open product question is exposed; all findings are deterministic amendments to the existing adopted direction.

## Recheck — 2026-07-16

**PASS — the updated spine resolves R-PRD-1 through R-PRD-9.** This disposition supersedes the initial `result: fail` for the current updated draft.

| Finding | Recheck disposition |
| --- | --- |
| R-PRD-1 | Resolved by AD-31: exact classification vocabulary, integration ownership, authorization-before-parsing, safe validation response/no submission, shared validator, and `secret` constraint. |
| R-PRD-2 | Resolved by AD-33's surface-invariant role/action matrix and delegated-caller rule. |
| R-PRD-3 | Resolved by AD-32 plus AD-19: logical fields, inclusion/freshness/resolution/recovery vocabularies, usability transitions, first-response gate, refresh snapshot, and safe `404` disclosure are fixed. |
| R-PRD-4 | Resolved by AD-27: 10,000 Projects/Tenant, 5,000 references/Project, 100,000 audit records/Project, both metadata-read p95 shapes, and task-admission p95 are fixed. |
| R-PRD-5 | Resolved by AD-26's durable-audit whitelist, operational-telemetry exclusions, receipt capture, and idempotent deduplication identity. |
| R-PRD-6 | Resolved by AD-26 and the Outcome measurement convention, which normatively adopts the PRD's eligible-resolution/resumption, continuity-success, and correction definitions, thresholds, denominator treatment, reporting fields, insufficient-volume rule, and deterministic fixture evidence. |
| R-PRD-7 | Resolved by AD-32: Setup mutation completion, read-only refresh/no maintenance audit, and first-response admission boundaries are explicit. |
| R-PRD-8 | Resolved by AD-31's exact historical unversioned name-only scope and major-version/migration/usage/test/rollback retirement gate. |
| R-PRD-9 | Resolved by AD-4 and AD-19: rejection/failure meanings, active-task polling, immutable terminal states, irreversible checkpoint, and post-checkpoint cancellation conflict are explicit. |

No remaining PRD reconciliation blocker was found.
