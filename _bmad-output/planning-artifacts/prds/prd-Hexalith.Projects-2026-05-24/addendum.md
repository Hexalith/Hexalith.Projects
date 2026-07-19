# Hexalith.Projects PRD Addendum

## Purpose and Downstream Artifact Routing

This addendum preserves implementation, migration, package-boundary, and verification depth from the July 2026 sprint-change proposals. The observable product contract remains in `prd.md`; downstream ownership is:

- **Architecture:** task/receipt persistence, state transitions, confirmation security, idempotency canonicalization, compensation/reconciliation, safe-export schema, migration/cutover, deployment topology, and upstream version gates.
- **UX:** Chatbot companion journeys and operator task/read/export experiences, including all accessibility behavior.
- **API contracts:** canonical/legacy Create Project shapes, task/preview/confirmation endpoints, cursor scope, Safe Diagnostic Export, structured errors, CLI/MCP mappings, and compatibility policy.
- **Test strategy:** deterministic fixtures, acceptance matrices, performance envelopes, recovery/concurrency suites, security/privacy proof, accessibility evidence, live-AppHost lane, and no-skip release gate.
- **Epics and stories:** repository-local slices and dependency order; proposals do not themselves authorize corrective development or sibling-repository mutation.

## Current Readiness, Release Containment, and Supersession

- Epics 1–5 remain immutable implementation history, not release authorization.
- E-2 through E-4 record 23 Epic 6–8 placeholders as findings inventory, not schedulable stories. Outcome-based replacements must pass implementation-readiness review before sprint planning or story creation.
- Corrective order remains Epic 6, Epic 7, then Epic 8; consequential autonomous MCP mutation and proposal confirmation stay disabled until their gates pass.
- Corrective development and production release remain frozen until an E-2-superseding rerun returns `READY` (recorded 2026-07-17, E-13), Story 8.11 terminal release evidence passes (the materialized epics move this gate from Story 8.9; E-8 records the blocked Epic 5 handoff), and Jerome/John record a terminal, dated disposition.
- Every NFR and P1/P2 finding requires an owning story, deterministic environment/fixture, verification command, pass/fail artifact, and terminal release disposition. The 29 issues, 23 placeholders, live result of 19 passed and 56 failed cases, and blocked handoff remain the readiness trigger; planning approval does not erase them.

E-6 proved a runnable live-AppHost mechanism, not release acceptance: the focused run had 13 passes and 13 failures; the full Chromium run had 19 passes and 56 failures across 75 cases. Blockers were missing deterministic `tenant-a` access-projection seeding and missing prerequisites for the warning-console UI and its static assets. Safe-denial `404` results are valid negative evidence, not authorized FR-22 proof. Story 8.6 remediation must map failures to FR-21, FR-22, NFR-9, and NFR-11, retain the separate Conversations sibling-root correction, name owners, and rerun with deterministic authorized fixtures and required assets.

Supersession trace:

- The final structure supersedes the July 15 proposal's export placement: FR-22 remains operator read access; Safe Diagnostic Export is FR-24.
- The final PRD preserves the pre-rebaseline FR-1–FR-22 and adds FR-23 Restore Archived Project and FR-24 Create Safe Diagnostic Export, for a total of 24 Functional Requirements.
- The materialized 2026-07-16 epics supersede the Story 8.9 terminal release gate: Story 8.11 owns deployment, rollback, and stakeholder-acceptance evidence, and Story 8.9 is repurposed as the bounded performance and back-pressure story.
- The 2026-07-17 independent readiness rerun (E-13) returned `READY`, superseding the E-2/E-11 `NOT_READY` triggers; release containment continues until Story 8.11 terminal acceptance.

## 1. Durable Workflow Design Context

Architecture must define the exact state machine and persistence mechanism behind the PRD's `Pending`, `Running`, `WaitingForDependency`, `NeedsAttention`, `Succeeded`, `Rejected`, `Failed`, and `Cancelled` statuses. It must cover:

- Durable checkpoints, worker ownership, leases, restart recovery, two-instance convergence, duplicate delivery, lost responses, cancellation cut-off, and terminal-state immutability.
- Cross-context receipts and reconciliation for Folder creation/binding, Conversation membership, File/Memory links, archive, and restore.
- The irreversible commit point and the difference between retryable dependency waiting, human reconciliation, terminal rejection, and terminal failure.
- Orphan/reserved Folder recovery without automatic deletion of a resource owned by `Hexalith.Folders`.
- Read-model confirmation is the completion authority; a SignalR notification or request acknowledgement does not establish completion.

The Project lifecycle must not acquire task-like states. Pending creation remains a task concern and is not represented as a third Project lifecycle value.

### 1.1 Platform ownership invariant

- `Hexalith.Projects` owns domain policy, Project contracts, and Project-specific Durable Task transitions.
- Hexalith.EventStore DomainService/platform owns hosting, event persistence/publication, subscriptions, read-model stores, cursors, health, telemetry, and reusable durable-workflow capability.
- The platform AppHost owns distributed topology and dependency wiring.
- FrontComposer/platform hosts own Web, MCP, and CLI runtime composition; domain code does not reimplement those platform capabilities.
- Production-capable hosts must not register allow-all development stubs for identity or authorization. Web, CLI, MCP, Chatbot, and service-to-service paths must carry real credentials and delegated service identity through the approved platform composition.

## 2. Preview, Confirmation, and Idempotency Mechanisms

Downstream design must specify:

- Preview and Confirmation Artifact schemas, signing/key ownership, normalized request material, resource-version binding, 15-minute expiry, single-use enforcement, replay response, and safe renewal after stale evidence.
- Idempotency scope `(Tenant, actor, operation, key)`, request-equivalence canonicalization, retention for at least 30 days and never less than the associated result lifetime (whichever expires later controls), conflict response, and recovery after a successful operation whose response was lost.
- Unicode-safe canonicalization, including parity for U+2028/U+2029 and other control/invisible characters, without broadening what counts as equivalent input.
- Cancellation before the irreversible commit point and a conflict/safe status response after it.
- Metadata-only reason codes that distinguish expired, stale, replayed, tampered, unauthorized, dependency-waiting, and reconciliation-required outcomes without leaking protected detail.

Exact wire fields, cryptographic choices, stores, and algorithms belong in architecture and contract artifacts, not the PRD.

## 3. Safe Diagnostic Export Contract Detail

The architecture/API contract should define a versioned `projects.safe-diagnostic-export.v1` representation consistent across Web, CLI, and MCP adapters. It must preserve:

- A 1 MiB cap on the complete encoded response, including the envelope and truncation metadata.
- Global caps of 500 reference rows and 100 audit rows.
- A per-Tenant limit of two concurrent exports; the size and row caps apply independently to each complete encoded export.
- Deterministic reference ordering and newest-first audit ordering with stable tie-breaking.
- Included/omitted counts and safe truncation reasons without excluded detail.
- No continuation cursor and no server retention of the generated export.
- Safe markers for unavailable components instead of raw upstream problems or fabricated completeness.
- Separate export permission and metadata-only audit of every attempt/outcome.

Exact field ordering, serialization, CLI exit mapping, and MCP schema belong in API/architecture artifacts.

## 4. Contract and Package Boundaries

### 4.1 Create Project metadata classification

Canonical Create Project requests require a valid system-supplied Metadata Classification while preserving Project name as the only user-authored input. Historical unversioned name-only requests remain compatible throughout v1. Contract planning must identify:

- The exact wire vocabulary: `public_metadata`, `tenant_sensitive`, `credential_sensitive`, and `secret`. The `secret` token is a sensitivity label; it never authorizes storage or disclosure of secret content.
- The owning authenticated integration adapter derives and supplies classification from integration policy; Projects validates it before domain submission. The field is not user-authored, and domain behavior must not infer it from user text.
- Canonical and legacy paths are distinguished by the versioned request shape: the canonical shape requires classification; only the historical unversioned name-only shape receives the v1 compatibility treatment.
- Missing, blank, null, non-string, case/whitespace variants, duplicate properties, and unknown values are invalid on the canonical path. A valid label is retained only where safe policy/audit metadata requires it; it is not a payload-storage instruction.
- Authorization precedes protected request parsing. Invalid classification returns `400 ValidationFailure` with `details.rejectedField = projectMetadata.metadataClass`, echoes no rejected value, and invokes no command submitter.
- A server-owned `SensitiveMetadataTierValidator` is reused by direct Create Project and proposal confirmation so the four-value classification policy cannot drift; proposal-confirmation behavior remains unchanged.
- Only the historical request shape uses the compatibility adapter; retirement requires client migration, major-version approval, and rollback criteria, with section 7.1 centralizing the required verification evidence.

### 4.2 Projects UI contract ownership

**Decision:** The approved boundary is a non-packable `Hexalith.Projects.UI.Contracts` descriptor host owned and delivered in the Projects repository. It is a technical package-boundary change, not a Projects feature change.

**Boundary:**

- `Hexalith.Projects.UI.Contracts` depends on the UI-free `Hexalith.Projects.Contracts` kernel and does not make the kernel depend on FrontComposer Shell, Fluxor, Fluent UI, or `Microsoft.AspNetCore.App`.
- `Hexalith.Projects.UI.Contracts` remains excluded from the published package inventory. MCP and CLI remain independent of this descriptor host.
- Logical namespaces and FrontComposer contract versions remain unchanged by default. If the audit finds assembly/public-surface impact, an explicit migration and package-versioning decision is required; a breaking release is not presumed when compatibility can be preserved.

**Delivery and provenance:** The approved Story 5.13 proposal records the provenance of the decision; the authoritative implementation-readiness correction routes current delivery through Story 6.2 (E-4 and E-5). Exact type inventory, assembly scanning, gate commands, and artifact edits remain in that source proposal, the Story 6.2 delivery vehicle, and repository-local architecture/test artifacts.

**Release gate:** Story 6.2 completion and the dependency, non-packability, consumer-parity, and regression evidence centralized in section 7.1 explicitly gate release readiness of the `Hexalith.Projects.Contracts` package.

### 4.3 Shared build centralization

**Decision:** `Hexalith.Builds` is the single version owner for `NSwag.MSBuild` `14.7.1` and `Fluxor.Blazor.Web` `6.9.0`. Projects imports those centrally resolved versions through versionless `PackageReference` entries, preserves `CentralPackageTransitivePinningEnabled`, and must not reintroduce local pins.

**Scope and constraints:** The correction record must name the independently approved scope across Projects, Builds, FrontComposer, and Conversations, including disposition of the adjacent Conversations `Microsoft.Playwright` cleanup. Copy the exact approved versions without an opportunistic upgrade, preserve generated/client/runtime behavior, and roll back on restore or build failure.

**Verification:** Section 7.1 centralizes the acceptance evidence. An implementation claim without that evidence remains unverified.

## 5. Migration and Compatibility

Architecture and implementation planning must cover:

- Inventory and reconciliation of legacy Active folderless Projects and in-flight Folder work before those Projects can appear in lists or participate in resolution or context.
- Additive event evolution and preservation of historical event readability; no event-history rewrite.
- Compatibility adapters, replay comparison, value-slice cutover, routing rollback, and retirement evidence.
- Safe handling of archived Projects whose prior Folder is missing or no longer authorized.
- No unsafe dual writes during migration.

Repository-local upstream work in EventStore, FrontComposer, Conversations, Folders, or Chatbot requires its own approved story and verification; the Projects PRD does not authorize sibling-repository changes by implication.

## 6. Chatbot Companion Contract

`Hexalith.Chatbot` owns end-user presentation. Projects owns the versioned Preview, Confirmation Artifact, Durable Task, Project Resolution, and Project Context contracts. UX and integration artifacts must define:

- Candidate comparison with no preselection.
- Explicit confirm/cancel actions, expiry/staleness recovery, lost-response retry, and task-status rendering.
- Exact wire schemas and surface mappings for the PRD's logical `responseState`, `asOf`, `projectVersion`, component inclusion/freshness/reason metadata, and Recovery Action Codes, without changing the observable transitions defined in the PRD.
- Keyboard/focus behavior, live status announcements, 200% zoom, responsive behavior at 320 CSS pixels, and authenticated screen-reader evidence.
- Safe denial/degraded states that do not infer completion from acknowledgement or SignalR.
- Version compatibility and authenticated integration evidence required before Projects release.

## 7. Verification and Release Evidence

### 7.1 Topic-specific verification matrix

| Topic | Required evidence | Release effect and trace |
| --- | --- | --- |
| Create Project metadata classification | OpenAPI required-field/enum tests; canonical/legacy endpoint matrix; leakage and no-command assertions; shared-validator parity; fingerprint/compatibility gates; warning-free Release build; usage evidence; client migration; major-version approval; rollback criteria | The sprint action remains open until focused evidence is green and exact commands/results are recorded; required before compatibility retirement; contract rules remain in section 4.1; E-9 |
| Projects UI contract ownership | Supported-consumer audit across Web, MCP, CLI, generated output, schemas, snapshots, and package/public-API baselines; equivalent descriptor discovery, navigation/routes, views, state names, reason codes, fields, contract versions, retargeted FrontComposer inspection, UI-lane descriptor tests, dependency/non-packability gates, Tenant isolation, accessibility, and `NoPayloadLeakage` behavior | Story 6.2 completion gates `Hexalith.Projects.Contracts` package release readiness; E-4 and E-5 |
| Shared build centralization | Restore and warning-free Release builds; targeted central-resolution proof; exact changed-file scope; repository approvals; resolved package output; Builds commit; root submodule-pointer state | Required for a verified implementation claim; section 4.3 rollback applies on restore or build failure |

### 7.2 Cross-cutting test and release evidence

Detailed test design belongs in a test strategy and repository-local gates. It must cover:

- Deterministic fixtures with small, median, and maximum cardinality for PRD NFR performance claims.
- Authenticated persisted-boundary, cross-Tenant denial, authorization freshness, encryption/KMS configuration, replay/tamper, privacy, and metadata-only evidence.
- Restart, two-instance convergence, duplicate delivery, concurrency, cancellation, lost response, compensation, reconciliation, and read-model confirmation.
- Web/CLI/MCP parity for authorized facts and safe failure categories without forcing identical presentation.
- Automated accessibility checks plus manual keyboard/screen-reader review using authenticated fixtures.
- Metadata-only outcome measurement for SM-7, SM-8, and SM-C4: rolling-window numerator/denominator accounting, excluded counts by safe reason, context-response states, and correction categories, with no Conversation text, Project name, prompt, path, foreign payload, or secret.
- Deployment, smoke, rollback, compatibility, and stakeholder-acceptance evidence.
- A rule that failed critical cases and unexplained critical skips block release; environment absence remains “not verified.”

### 7.3 Unicode idempotency parity evidence

Idempotency verification must reject U+2028/U+2029 in identifier and envelope fields while preserving deterministic escaping in accepted descriptive metadata. Direct real-server and generated-helper tests must prove byte-for-byte parity, non-collision with LF and literal backslash-`u` text, and stability of unaffected hashes. Generated `.g.cs` files remain generated. Before canonical bytes change, inspect deployed state for persisted legacy fingerprints; if affected entries exist, require a bounded legacy-hash compatibility strategy and deployment gate.

### 7.4 Live Playwright evidence

The live Playwright contract has two independent lanes: deterministic checks that require no AppHost and an explicit live opt-in lane that discovers the ready `projects-ui` endpoint dynamically, rejects guessed/invalid URLs, and uses Aspire-managed teardown. Permanent `test.fixme` declarations become conditional live tests; every retained skip names a concrete missing route, fixture, seed, or product prerequisite. Use only root-declared sibling checkouts, never initialize nested submodules, align the Projects-owned AppHost SDK with the shared Aspire hosting toolchain, and never edit generated tool-cache binaries.

## 8. Evidence and Gate Index

Statuses below describe the cited artifact at its stated revision/date. A later artifact supersedes a row only when it explicitly identifies the earlier evidence and records the new disposition.

| ID | Artifact path | Revision / date | Owner | Current status | Gated requirements or decision |
| --- | --- | --- | --- | --- | --- |
| **E-1** | `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15.md` | Approved 2026-07-15 | Jerome (approval); Product Manager, Architect, UX/Chatbot owner, Product Owner, Test Architect (handoff) | Approved Major rebaseline; planning correction authorized, corrective development/release still contained | Product direction and rebaseline for FR-1–FR-24, NFR-1–NFR-11, artifact order, and the `READY`-before-sprint decision |
| **E-2** | `_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-14.md` | Completed 2026-07-14 | Codex, independent assessor | `NOT_READY`; historical trigger that remains blocking until an explicit rerun supersedes it | Corrective story entry, sprint planning, NFR-11, 29 recorded readiness issues, and the requirement to replace 23 placeholders |
| **E-3** | `_bmad-output/planning-artifacts/epics.md` | Reconciled 2026-07-16 (supersedes the 2026-07-14 corrective addendum) | Product Owner | Epics 1–5 are implementation history; Epics 6–8 (33 AC-bearing stories) are production authority, gated by the `READY` rerun (E-13) | Corrective scope/dependency inventory, Story 6.2, Story 8.6, the Story 8.11 terminal release gate (Story 8.9 repurposed as performance/back-pressure), and release sequencing |
| **E-4** | `_bmad-output/implementation-artifacts/sprint-status.yaml` | Regenerated 2026-07-17 after the `READY` rerun; last updated 2026-07-19 | Product Owner; named action owners in ledger | Epics 1–5 recorded `done` as implementation history; the historical Epic 5 action ledger routes Story 5.13 intent to Story 6.2, live failures to Story 8.6, and the release decision to Story 8.9 as recorded 2026-07-14 (terminal gate now Story 8.11) | Current tracking state, package-boundary delivery route, authenticated E2E remediation, and terminal release evidence |
| **E-5** | `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-14-frontcomposer-contract-boundary.md` | Approved 2026-07-14 | Jerome (decision); Winston, Amelia, Murat (delivery/verification) | Boundary decision approved; original Story 5.13 route superseded by E-1/E-4 Story 6.2 route | `Hexalith.Projects.UI.Contracts` non-packable boundary, FR-22/FR-24 consumer parity, NFR-10, NFR-11, Contracts package release readiness |
| **E-6** | `_bmad-output/implementation-artifacts/spec-5-12-live-apphost-operational-console-verification.md` | Done 2026-07-14; baseline commit `f03a8d6` | Murat / Amelia | Harness and execution complete; the focused live run had 13 passes and 13 failures, and the full live run had 19 passes and 56 failures; remediation routed to Story 8.6 | FR-21, FR-22, NFR-9, NFR-11, authenticated authorized-path and UI/static-asset evidence |
| **E-7** | `_bmad-output/analysis/hexalith-projects-codebase-audit-2026-07-14.md` | Audit 2026-07-14; observed baseline `4fc7fa5`→`b89cb8f` | Product Owner (routing); Jerome / John (risk disposition) | Nine P1 and seven P2 findings require closure or authorized disposition; no production authorization | NFR-1–NFR-11, secure platform boundary, identity, durable confirmation/workflow, machine clients, and production readiness |
| **E-8** | `_bmad-output/implementation-artifacts/epic-5-release-handoff-2026-07-14.md` | Decision dated 2026-07-14 | Jerome / John | `BLOCKED`; deployment not verified and stakeholder acceptance not granted | Terminal release evidence (recorded against Story 8.9 at decision date; gate moved to Story 8.11 by the materialized epics), NFR-11, production deployment/smoke/rollback evidence, residual-risk disposition, and final release authorization |
| **E-9** | `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-14.md` | Approved 2026-07-14 | Jerome (decision); Amelia (delivery) | CreateProject metadata-classification correction approved; implementation remains open until focused contract/server evidence is green and recorded | `projectMetadata.metadataClass` rejection path, shared `SensitiveMetadataTierValidator`, canonical/legacy compatibility, NFR-10, NFR-11 |
| **E-10** | `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-16.md` | Approved 2026-07-16 | Jerome (approval) | Approved Major downstream artifact repair after the final PRD rebaseline; amends the 2026-07-15 proposal; no PRD or addendum change required | Repair scope for downstream planning artifacts (epics, tracking) so they conform to final FR-1–FR-24 / NFR-1–NFR-11 authority |
| **E-11** | `_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-16.md` | Completed 2026-07-16 | Independent readiness rerun | `NOT_READY`; superseded the 2026-07-15 report in the E-2 chain; itself superseded by the 2026-07-17 `READY` rerun (E-13) | Continued corrective containment and the trigger for materializing the corrective planning layer (E-12) |
| **E-12** | `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-16-implementation-readiness-rerun.md` | Approved 2026-07-16 | Jerome (approval) | Approved Moderate corrective-planning materialization; explicitly preserves PRD, addendum, and Architecture Spine | Materialized Epics 6–8 (33 AC-bearing stories) as production authority, `READY`-before-story-creation gating, and the Story 8.11 terminal release gate superseding Story 8.9 |
| **E-13** | `_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-17.md` | Completed 2026-07-17 | Independent readiness rerun | `READY`; the E-2-superseding rerun contemplated by the containment rules; supersedes the 2026-07-16 `NOT_READY` verdict | Authorizes 6.x/7.x/8.x story-file creation and sprint reconciliation only; release, consequential autonomous MCP mutation, and proposal confirmation stay blocked until Story 8.11 terminal acceptance; G-1–G-6 entry gates remain prerequisites per story |
| **E-14** | `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-17.md` | Approved 2026-07-17 | Jerome (approval) | Approved Major externalization of Story 6.1 platform prerequisites; preserves PRD, addendum, and Architecture Spine; no PRD change required | Epic 6 entry gating through the externalized 6.1-P0–6.1-P4 prerequisite ledger (persisted module runner, query security/watermark capability, production identity contract) with owner-approved platform decisions kept outside story scope |
