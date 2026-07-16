---
title: Architecture Spine Reconciliation — July 16 Course Correction and Readiness
status: pass
reviewed: 2026-07-16
reviewer: reconcile-change
subject: ../ARCHITECTURE-SPINE.md
inputs:
  - ../../../sprint-change-proposal-2026-07-16.md
  - ../../../implementation-readiness-report-2026-07-15.md
---

# Architecture Spine Reconciliation — Course Correction and Readiness

## Verdict

**FAIL — the draft is directionally strong but is not yet eligible to become the sole final architecture authority.**

The spine successfully lands the central correction: a domain-centric EventStore DomainService module with platform-owned technical layers, a non-packable `Hexalith.Projects.UI.Contracts` boundary, Folder-first activation, platform Durable Tasks and Confirmation Artifacts, staged single-writer migration, repository-local run/test parity, bounded FR-24 export, MCP containment, and fail-closed release evidence.

Finalization is blocked by visible historical supersession, incomplete readiness/release containment, and a machine-checkable evidence contract that is currently asserted only in prose. Six additional material gaps must be closed so downstream UX, story, test, and readiness work cannot choose incompatible mappings.

## Mandatory Correction Checklist

| Required correction | Draft evidence | Result | Required action |
| --- | --- | --- | --- |
| FR-1–FR-24 and NFR-1–NFR-11 are the governed baseline | Frontmatter `binds`; capability map | Pass | None |
| Domain-centric EventStore DomainService; platform-owned runtime and presentation | Paradigm; AD-1, AD-2, AD-24, AD-28 | Pass | None |
| Projects owns policy, contracts, handlers, and Project workflow meaning | AD-2, AD-9, structural seed | Pass | None |
| Stable contracts live in `Projects.Contracts` | AD-16 | Pass | Close the contract-specific gaps in M-5 |
| FrontComposer descriptors live in non-packable `Projects.UI.Contracts` | AD-2, AD-16, AD-24 | Pass | Close the dependency prohibition in M-5 |
| Platform owns hosting, persistence, publication, read models, cursors, health, telemetry, and generic durable workflow | AD-1, AD-9, AD-14, AD-28 | Pass | None |
| Platform AppHost owns topology/Dapr; FrontComposer/platform hosts own Web, CLI, MCP and credentials | AD-2, AD-25, AD-28, AD-29 | Pass | None |
| Chatbot owns end-user candidate/proposal presentation without gaining Project authority | AD-2, AD-29 | Pass | None |
| Technical projects move to technical modules without losing repository-local build/run/debug/test | AD-24, AD-25, G-4 | Pass | None; this directly preserves the user's final constraint |
| Exactly one authorized Folder before visibility or `Active`; read-model-confirmed completion | AD-3, AD-8, AD-22, AD-23 | Pass | None |
| Durable task statuses, durable checkpoints/leases/receipts, immutable terminal truth | AD-4, AD-9, AD-12, state diagram | Partial | Complete worker fencing, restart/two-instance, duplicate-delivery, and transition rules under M-6 |
| `202` and SignalR are never completion | AD-4, AD-19 | Pass | None |
| Opaque, 15-minute, bound, atomically single-use Confirmation Artifact | AD-5, AD-13 | Pass | None |
| Idempotency scope, equivalent retry, changed-request conflict, and retention | AD-5, AD-13, AD-19 | Pass | None |
| Normative response/freshness/task/recovery mapping and first-response admission | AD-19 and convention refer to PRD vocabularies | Fail | Add the missing architecture mapping under M-1 |
| Compatibility adapters, shadow replay comparison, value-slice read cutover, routing rollback, one writer | AD-17 and cutover diagram | Pass | None |
| Comprehensive legacy inventory before migration | No binding inventory rule | Fail | Add the inventory gate under M-2 |
| Legacy folderless/pending/partial records use compensating Durable Tasks | AD-22 says reconciliation; AD-8 covers one partial creation case | Partial | Make all three legacy classes and Durable Task treatment explicit under M-2 |
| No history rewrite and no unsafe dual writes | AD-6, AD-17, AD-22 | Pass | None |
| Every sibling change has separate repository-local authority | AD-6 and G-1–G-6 | Partial | Add named owner and root-declared-checkout rule under M-3 |
| Resolution Traces are current, request-scoped, and nonpersistent | AD-7 | Pass | None |
| FR-24 schema, separate authority, complete-response bounds, ordering, truncation, unavailability, concurrency, audit, and non-retention | AD-21, AD-27 | Partial | Add omitted count/reason/tie-break/Chatbot details under M-4 |
| Semantic parity without authority expansion across Web, CLI, MCP, and Chatbot | AD-7, AD-19, AD-20, AD-29 | Pass | None |
| Consequential autonomous MCP mutation cannot bypass human confirmation or release gates | AD-29, AD-30 | Pass | Tighten exact terminal gate wording under B-2 |
| Machine-checkable evidence ownership and no critical false pass | AD-30 | Fail | Define the canonical artifact/schema/validator/owner under B-3 |
| Old May architecture is visibly superseded; July spine is sole normative authority | Old `architecture.md` still has `status: 'complete'`; spine is `status: draft` | Fail | Complete B-1 |
| Corrective implementation and sprint progression remain blocked until the required readiness result | AD-30 blocks corrective development only | Partial | Complete B-2 |

## Blocking Findings

### B-1 — Historical supersession and sole-authority state are not established

The approved proposal requires the May architecture to remain only as visibly superseded historical evidence and the July spine to become the sole normative architecture. The old `_bmad-output/planning-artifacts/architecture.md` still declares `status: 'complete'` and contains no visible `superseded_by` pointer. The new spine still declares `status: draft`.

**Required fix:**

- Mark the old architecture `status: superseded`, preserve it as historical evidence, and add a direct `superseded_by` reference to this spine plus the supersession date.
- After all reconciliation and reviewer-gate fixes pass, set this spine to `status: final`.
- Add the July 15 readiness assessment to the spine's `sources`; it is the trigger evidence for the correction and must not disappear from authority traceability.

### B-2 — Readiness, sprint, and terminal release containment are not exact enough

AD-30 correctly blocks corrective development until a superseding `READY` assessment and blocks production, consequential autonomous MCP mutation, and proposed-Project confirmation pending terminal evidence. It does not bind all approved sequencing and authority conditions:

- no readiness-approved implementation story may become `ready-for-dev` or be created for execution before the independent readiness rerun;
- sprint planning/reconciliation occurs only after that rerun returns exactly `READY`;
- the placeholder-to-replacement inventory is reconciled atomically and is not itself implementation authorization;
- production release requires Story 8.11 deployment/rollback evidence and dated acceptance from the named release owners, **Jerome and John**;
- recording a blocker or unavailable environment cannot complete Story 8.11 or any critical release case.

**Required fix:** amend AD-30 or add one stable containment convention encoding the full sequence and named terminal authorities. Preserve the existing blocks on production, consequential autonomous MCP mutation, and proposed-Project confirmation until the terminal release conditions pass.

### B-3 — The evidence map is described, not yet machine-checkable

AD-30 lists several fields, but it does not identify a canonical artifact, serialization/schema contract, row identity, validation command, validation owner, or the exact story-entry/release predicates. A prose statement that evidence “maps” items cannot itself prevent a false pass.

The architecture contract must require the later `implementation-readiness-traceability-matrix` to carry at least:

- requirement and finding identifiers, architecture decision, UX journey, story, repository and named owner;
- repository version/revision, dependencies and external entry gates;
- environment/fixture, exact verification command, named evidence artifact, estimate, status, and release disposition;
- terminal disposition for every FR, NFR, P1/P2 finding, and required release-evidence category;
- a stable machine-readable row key and schema/version;
- automated validation that rejects duplicate/missing keys, unresolved `TBD`, missing approval/version/command/artifact, failed critical evidence, unexplained critical skips, and false `passed` values for unavailable environments.

**Required fix:** define the canonical format/path, schema/version, owner, validator command or owned validation capability, and the `ready-for-dev`, readiness, and release gate predicates. The matrix can remain a downstream artifact; its architecture contract cannot remain implicit.

## Major Findings

### M-1 — Observable response, freshness, recovery, and first-response admission are not mapped

AD-19 defines HTTP/task transport semantics, while the conventions defer state vocabularies to the PRD. The approved correction requires a normative mapping among Context Response State, Evidence Freshness State, Durable Task status, Recovery Action Code, and read-model-confirmed completion. The spine does not state:

- the required `responseState`, `asOf`, authorized `projectVersion`, optional `resolutionResult`, component evidence, and recovery actions on applicable read responses;
- how stale, rebuilding, unavailable, denied, or excluded component evidence determines `Complete`, `Partial`, `Unavailable`, or safe `404` behavior;
- that Chatbot may admit a first response only for `Complete` or `Partial`, while `Unavailable` or `Denied` blocks admission;
- which recovery actions are legal for expired/stale confirmation, projection lag/rebuild, dependency unavailability, and `NeedsAttention`;
- FR-3 Update Project Setup under Durable Task and read-model-confirmed completion governance. AD-4 omits FR-3 from `Binds`, and the capability map assigns FR-3 only to aggregate/contracts rules.

**Required fix:** add a compact mapping table or binding rule; add FR-3 to the appropriate task/completion decisions.

### M-2 — Migration lacks its required inventory and complete legacy-repair contract

AD-17 strongly defines shadow-read cutover and single-writer fencing, but it does not require the approved pre-cutover inventory of existing events, state keys, read models, cursors, routes, clients, identities, in-flight work, and consumers. AD-22 sends historical null-Folder state to reconciliation without explicitly requiring compensating Durable Tasks for legacy Active-folderless Projects, pending-Folder work, and other partial records.

**Required fix:** make the inventory a migration entry gate; enumerate those legacy classes; require idempotent compensating Durable Tasks, safe receipts/status recovery, and no automatic deletion of owner resources; require terminal disposition before affected routes switch.

### M-3 — Repository-local authority omits the named owner and checkout constraint

AD-6 requires approval, pin, entry criteria, evidence, and rollback for sibling changes, which is the right boundary. The approved repository contract also requires a named repository owner and permits only root-declared submodule checkouts as implementation evidence. Neither is binding in the spine.

**Required fix:** add named owner/scope to every G-1–G-6 dependency record (or require those fields in the canonical evidence map), state that Projects planning never authorizes sibling mutation, and bind validation to root-declared repository/submodule checkouts only.

### M-4 — FR-24 omits required truncation disclosure and exclusion details

AD-21 captures the schema name, distinct authorization, synchronous snapshot, 1 MiB/500/100 bounds, deterministic ordering, newest-first audit rows, safe unavailable markers, two per-Tenant leases, attempt/outcome audit, and no retained bytes/cursor/task. It does not explicitly require:

- stable tie-breaking for audit ordering and a named deterministic reference-order contract;
- included and omitted counts plus safe truncation reason codes, with no excluded detail;
- the size limit to include the envelope and all truncation metadata;
- Chatbot to be unable to create exports and FR-22 read permission not to imply export permission.

**Required fix:** add those four clauses to AD-21. Keep Web/CLI/MCP as equivalent adapters over the same server query.

### M-5 — Unicode parity and contract/package controls are incomplete

The serialization convention covers U+2028/U+2029 rejection and deterministic escaping, but readiness AR-5 also requires no collision with LF or literal backslash-`u` text, stable hashes for unaffected inputs, generated-file protection, deployed fingerprint inspection, and a bounded legacy-hash strategy if persisted values were affected.

The contract boundary also does not explicitly bind:

- canonical Metadata Classification to exactly `public_metadata`, `tenant_sensitive`, `credential_sensitive`, or `secret`, validated before command submission with metadata-only field diagnostics;
- the UI-free Contracts kernel prohibition against FrontComposer Shell, Fluxor, Fluent UI, and `Microsoft.AspNetCore.App` dependencies;
- `Hexalith.Builds` as the sole approved version owner for NSwag and Fluxor.

**Required fix:** add these rules to the serialization/contracts conventions or a focused AD. Do not hand-edit generated artifacts to satisfy parity.

### M-6 — Durable Task transition integrity is not fully specified

AD-4/AD-9 correctly place generic execution in the platform and workflow meaning in Projects, and the diagram establishes the primary status graph. The approved task contract also requires explicit worker ownership/fencing, lease-expiry behavior, restart and two-instance convergence, duplicate delivery, lost-response recovery, and cancellation cutoff for every nonterminal state. Those properties appear partially across AD-12 and G-1 but are not a single binding transition-integrity rule.

**Required fix:** require platform-enforced fenced worker ownership, one durable transition authority, deterministic duplicate-delivery convergence, checkpoint resume after restart/lease expiry, authoritative query before unknown-response retry, and cancellation only before the workflow's recorded irreversible checkpoint. Make any omitted state transition illegal rather than adapter-defined.

## Confirmed Strengths

- AD-24 and AD-25 precisely preserve the user's boundary: technical layers move to technical modules, while this repository retains a runnable and testable module through a platform development composition runner and parity gates.
- AD-3, AD-8, AD-22, and AD-23 close both historical folderless-activation defects without rewriting committed history or deleting Folders-owned resources.
- AD-10 and AD-11 keep Conversations, Folders, and Memories authoritative while permitting Projects to own intent, orchestration, references, and rebuildable projections.
- AD-12 and AD-13 give cross-context work durable receipts, expected versions, atomic confirmation consumption/admission, forward recovery, and safe `NeedsAttention` handling.
- AD-17 establishes the required shadow-read, reversible read-cutover, fenced single-writer migration rather than a flag-day rewrite or dual write.
- AD-28 makes Projects provider-agnostic while retaining concrete availability, RTO, task recovery, encryption, and primary-region durability obligations.
- AD-29 correctly prevents MCP from confirming end-user choices, bypassing Preview/Confirmation/Task admission, or expanding actor permissions.

## Pass Conditions

This reconciliation becomes **PASS** only when all of the following are true:

1. B-1 through B-3 and M-1 through M-6 are resolved in the spine or its explicitly bound machine contract.
2. The old architecture is visibly superseded and points to the final spine.
3. Deterministic spine lint and the required architecture reviewer gate pass after the fixes.
4. The spine status changes to `final` only after those reviews pass.

This pass would approve the architecture substrate only. It would **not** authorize implementation, sprint planning, sibling-repository mutation, production release, consequential autonomous MCP mutation, or proposed-Project confirmation; those remain governed by the approved downstream sequence and terminal evidence gates.

## Recheck

**PASS — no remaining content blockers (2026-07-16).**

- **B-1:** The readiness report is now a source. Changing the spine to `final` and visibly superseding the May architecture remain intentional reviewer-gate closeout actions, not architecture-content gaps.
- **B-2:** AD-30 now binds atomic placeholder reconciliation, independent exact-`READY` assessment before executable story creation/`ready-for-dev`/sprint reconciliation, Story 8.11 deployment and rollback evidence, named Jerome/John acceptance, and no completion on blockers or unavailable environments.
- **B-3:** AD-30 plus the repository runner contract now define the canonical YAML path, schema, stable row identity, complete fields, Builds-owned validator capability and command, rejection conditions, and readiness/release predicates.
- **M-1:** AD-4, AD-19, and AD-32 now bind FR-3 durable completion, response fields, freshness-to-usability consequences, safe denial, first-response admission, refresh behavior, task transport, and the canonical recovery vocabulary.
- **M-2:** AD-17 and AD-22 now require the complete pre-cutover inventory and idempotent compensating Durable Tasks with receipts, status recovery, terminal disposition, and owner-resource preservation.
- **M-3:** AD-6 now requires separate repository-local approval, named owner, pinned revision, evidence and rollback, and accepts only root-declared checkouts.
- **M-4:** AD-21 and AD-33 now close export permission, Chatbot exclusion, complete-response sizing, stable sorting/tie-break, included/omitted counts, safe truncation reasons, unavailability, concurrency, audit, and non-retention.
- **M-5:** AD-16, AD-31, and the canonicalization convention now close UI-free package dependencies, Builds version authority, exact metadata classes and validation, Unicode collision/hash/generated-file/fingerprint/legacy-hash behavior.
- **M-6:** AD-4's fenced worker ownership and closed transition graph, together with AD-12's receipt/query-before-retry saga, now bind restart, lease expiry, two-instance and duplicate-delivery convergence, irreversible-checkpoint cancellation, unknown outcomes, and terminal immutability.

The reviewer gate may now proceed. Finalization still must perform the two B-1 closeout edits and pass deterministic lint/review; this PASS does not relax any readiness, repository, implementation, MCP, confirmation, or production-release containment.
