---
stepsCompleted:
  - 1
  - 2
  - 3
  - 4
  - 5
  - 6
status: complete
overallReadiness: NOT_READY
assessor: 'Implementation Readiness workflow (independent rerun, 2026-07-16)'
supersedes: '_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-15.md'
date: '2026-07-16'
project: 'Hexalith.Projects'
inputDocuments:
  - _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md
  - _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/addendum.md
  - _bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md
  - _bmad-output/planning-artifacts/epics.md
  - _bmad-output/planning-artifacts/ux-design-specification.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-06.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-14.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-14-frontcomposer-contract-boundary.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-14-implementation-readiness-correction.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-14-live-apphost.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-14-u2028-u2029-idempotency-parity.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-16.md
---

# Implementation Readiness Assessment Report

**Date:** 2026-07-16
**Project:** Hexalith.Projects

## 1. Document Inventory

### Baseline Documents (Authoritative)

| Type | Path | Status | Last Updated |
|------|------|--------|--------------|
| PRD | `prds/prd-Hexalith.Projects-2026-05-24/prd.md` (+ `addendum.md`) | final | 2026-07-15 |
| Architecture | `architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md` | final | 2026-07-16 |
| Epics & Stories | `epics.md` | complete | 2026-07-14 |
| UX Design | `ux-design-specification.md` (+ `ux-design-directions.html`) | — | 2026-05-29 |

### Course-Correction Inputs (Traceability)

- `sprint-change-proposal-2026-07-06.md`
- `sprint-change-proposal-2026-07-14.md`
- `sprint-change-proposal-2026-07-14-frontcomposer-contract-boundary.md`
- `sprint-change-proposal-2026-07-14-implementation-readiness-correction.md`
- `sprint-change-proposal-2026-07-14-live-apphost.md`
- `sprint-change-proposal-2026-07-14-u2028-u2029-idempotency-parity.md`
- `sprint-change-proposal-2026-07-15.md`
- `sprint-change-proposal-2026-07-16.md`

### Discovery Notes

- **Architecture duplicate auto-resolved:** root `architecture.md` declares `status: superseded`, `supersededBy` the ARCHITECTURE-SPINE.md, retained as `historicalEvidence`. The spine is the source of truth.
- **Traceability flag:** `epics.md` frontmatter lists the *superseded* `architecture.md` as an input document, not the newer spine. To be verified during requirements traceability analysis.
- All four required document types are present. No missing documents.

## 2. PRD Analysis

Source: `prd.md` (status: final, updated 2026-07-15) + `addendum.md`.

### Functional Requirements (24 total)

**§6.1 Project Workspace Management**
- **FR-1 — Create Project:** Idempotent Durable Task; Project becomes caller-visible/`Active` only after exactly one authorized Project Folder is verified and bound. Name is only user-authored field; canonical request carries system-supplied Metadata Classification; historical unversioned name-only creation supported throughout v1. (UJ-2)
- **FR-2 — Open Project:** Return metadata, lifecycle, Project Setup, references to initialize a Conversation; §5 response/freshness/recovery semantics; pre-activation tasks not exposed. (UJ-1)
- **FR-3 — Update Project Setup:** Idempotent, durable, read-model-observable; additive/serialization-tolerant; rejects secrets/paths/foreign payloads.
- **FR-4 — Archive Project:** Server Preview + single-use confirmation + idempotent Durable Task; lifecycle only `Active`/`Archived`; completion on read-model `Archived`. (pairs FR-23)
- **FR-5 — List Projects:** Tenant-scoped, authorization-filtered, lifecycle-filterable; pages default 50, cap 200; cursors scoped to authenticated query.

**§6.2 Context References**
- **FR-6 — Link Conversation:** One Project per Conversation in v1; actor-selected additive link idempotent, inferred link needs Preview+confirmation. (UJ-1, UJ-3)
- **FR-7 — Move Conversation Between Projects:** Preview + single-use confirmation + idempotent Durable Task; exactly one membership; fails closed.
- **FR-8 — Set Project Folder:** Single Folder invariant; initial actor-selected binding idempotent; replacement via Preview+confirmation; Folders stays system-of-record. (UJ-2)
- **FR-9 — Link File Reference:** Optional, does not replace Folder; additive idempotent / inferred needs confirmation.
- **FR-10 — Link Memory:** Identity/metadata only; authorization delegated to Memories. (UJ-1, UJ-3)
- **FR-11 — Unlink Context Reference:** Conversation/File/Memory via Preview+confirmation+Durable Task; Folder replaceable but not removable from Active Project; removes association only, never the resource.

**§6.3 Project Resolution**
- **FR-12 — Resolve Project From Conversation:** `NoMatch`/`SingleCandidate`/`MultipleCandidates` with current reason codes; only Active read-model-confirmed Projects. (UJ-3)
- **FR-13 — Resolve Project From Attachments:** Match on current Folder/File identity+metadata (not contents); fails closed on stale evidence. (UJ-2)
- **FR-14 — Confirm Ambiguous Project:** No preselection; artifact bound to Tenant/actor/action/Conversation/candidates/versions, 15-min, single-use; stale/expired/replayed/tampered rejected safely. (UJ-3)
- **FR-15 — Propose New Project:** Creates nothing before confirmation; confirmed creation follows FR-1; §5 recovery on non-success. (UJ-2)

**§6.4 Project Context Assembly**
- **FR-16 — Get Project Context:** Tenant-scoped, actor-authorized; only for read-model-confirmed Active Project with exactly one Folder; reference metadata not payloads. (UJ-1, UJ-4)
- **FR-17 — Explain Context Selection:** Current Resolution Traces only, not reconstructed history; no secrets/payloads/unconfirmed-candidate detail; not persisted. (UJ-4)
- **FR-18 — Refresh Project Context:** Read-only recompute; never mutates; no maintenance audit event; §5 transition rules for Partial/Unavailable/Complete.

**§6.5 Project Setup Quality**
- **FR-19 — Validate Project Setup:** Name only required user field; valid Metadata Classification required on canonical path; rejects secrets/unrestricted paths/unsafe control chars/foreign payloads with safe reason codes.
- **FR-20 — Retrieve Conversation-Start Setup:** Goals, instructions, context preferences, default source policy bound to one `projectVersion`/`asOf`; first-response admission only for Complete/Partial.

**§6.6 Audit and Operations**
- **FR-21 — Record Project Audit Events:** Metadata-only audit of task admission/outcome, confirmed mutations, security-relevant confirmation outcomes, reconciliation, Safe Diagnostic Export; idempotent retries no duplicate audit. *(Release-blocking)*
- **FR-22 — Support Operator Read Access:** Tenant Operators/Administrators inspect metadata/lifecycle/references/task status/audit across Web/CLI/MCP; read alone grants no export/mutation. *(Release-blocking)*
- **FR-23 — Restore Archived Project:** Preview verifies Tenant/actor/authority/version/exactly-one-Folder; missing Folder requires replacement or same-name creation; `NeedsAttention` if activation cannot commit; never auto-deletes Folder. (UJ-5)
- **FR-24 — Create Safe Diagnostic Export:** Separate export permission (Chatbot cannot); ≤1 MiB encoded, ≤500 reference rows, ≤100 audit rows; deterministic ordering; no retention; every attempt audited. *(Release-blocking)*

### Non-Functional Requirements (11 total)

- **NFR-1 — Security & privacy:** Tenant/actor/action/target/version scoping; fail-closed on stale auth; metadata-only logs/telemetry/errors. *(Release-blocking)*
- **NFR-2 — Encryption & key management:** Authenticated encryption in transit; platform-managed at rest; Projects owns no keys; KMS rotation/revocation evidence release-blocking. *(Release-blocking)*
- **NFR-3 — Availability & recovery:** 99.9% monthly; RTO 15 min; accepted tasks resume or reach truthful `NeedsAttention` within 5 min. *(Release-blocking)*
- **NFR-4 — Durability & idempotency:** RPO 0 in primary-region durability domain; Active Projects never folderless; equivalent retries same task, changed requests conflict. *(Release-blocking)*
- **NFR-5 — Performance & scale:** 10,000 Projects/Tenant, 5,000 refs/Project (excl. Folder), 100,000 audit records/Project; reads p95 <500 ms at 1,000 Projects/500 refs and <1 s at max; task admission p95 <500 ms warm. *(Release-blocking)*
- **NFR-6 — Pagination & export bounds:** Pages default 50 / cap 200; export obeys FR-24 caps + 2 concurrent exports/Tenant. *(Release-blocking)*
- **NFR-7 — Back-pressure & dependency control:** Per Tenant: 100 reads/s (burst 200), 20 mutations/s (burst 40), 1,000 nonterminal tasks, 2 concurrent exports; interactive timeout 2 s, durable-step 10 s; ≤3 retries in 30 s; structured overload guidance. *(Release-blocking)*
- **NFR-8 — Retention & transient data:** Terminal result + idempotency record ≥30 days or result lifetime; Preview/Confirmation Artifacts expire 15 min; audit ≥365 days; Resolution Traces/exports not persisted. *(Release-blocking)*
- **NFR-9 — Accessibility:** WCAG 2.2 AA across Chatbot + operator journeys; keyboard, focus, AT announcements, no color/timing-only, 200% zoom, 320 CSS px; automated + authenticated manual evidence. *(Release-blocking)*
- **NFR-10 — Compatibility:** Additive/serialization-tolerant contracts; historical v1 data + unversioned name-only creation preserved; retirement gated by major version + migration/usage/compat/rollback evidence. *(Release-blocking)*
- **NFR-11 — Release evidence:** Authenticated persisted-boundary, cross-Tenant, restart/concurrency, duplicate-delivery, lost-response, accessibility, privacy, performance, deployment, smoke, rollback, stakeholder-acceptance evidence must pass; failed critical case or unexplained critical skip blocks release; unavailable env = "not verified". *(Release-blocking)*

### Additional Requirements & Constraints

- **Release classification (§2.3):** Core user value = FR-1–FR-20 + FR-23; Release-blocking = FR-21, FR-22, FR-24, NFR-1–NFR-11. No approved v1 FR/NFR is deferrable; a core-only build is internal evidence, not an authorized release.
- **Observable Context & Recovery Contract (§5):** Shared logical fields `responseState` (Complete/Partial/Unavailable/Denied), `asOf`, `projectVersion`, `resolutionResult`, `components`, `recoveryActions`; binding state-transition rules. Applies to open/list/resolution/context/Conversation-start/proposal-recovery.
- **Platform ownership invariant (addendum §1.1):** Projects owns domain policy + Durable Task transitions; EventStore/DomainService platform owns hosting, persistence/publication, subscriptions, read models, cursors, health, telemetry; AppHost owns topology; FrontComposer/platform hosts own Web/MCP/CLI. Production-capable hosts must not register allow-all identity/authz dev stubs.
- **Metadata Classification (addendum §4.1):** Wire vocabulary `public_metadata`/`tenant_sensitive`/`credential_sensitive`/`secret`; supplied by authenticated integration adapter, validated pre-submission; invalid → `400 ValidationFailure` with `details.rejectedField = projectMetadata.metadataClass`, no value echo; shared `SensitiveMetadataTierValidator` reused by direct create + proposal confirmation. (E-9)
- **Projects.UI.Contracts boundary (addendum §4.2):** Non-packable descriptor host in Projects repo; kernel `Hexalith.Projects.Contracts` stays UI-free; delivered via Story 6.2; gates Contracts package release readiness. (E-4/E-5)
- **Shared build centralization (addendum §4.3):** `Hexalith.Builds` is sole version owner of `NSwag.MSBuild` 14.7.1 and `Fluxor.Blazor.Web` 6.9.0; Projects imports versionless; no local re-pinning.
- **Unicode idempotency parity (addendum §7.3):** Reject U+2028/U+2029 in identifier/envelope fields; byte-for-byte parity between real-server and generated helpers; inspect deployed fingerprints before canonical byte change.
- **Migration/compatibility (addendum §5):** Inventory + reconcile legacy Active folderless Projects before list/resolution/context; additive event evolution, no history rewrite; no unsafe dual writes; sibling-repo work needs its own approved story.
- **Success metrics:** SM-1..SM-8 + counter-metrics SM-C1..SM-C4 (metadata-only measurement, rolling 30-day windows).
- **Evidence & gate index (addendum §8):** E-1..E-9 gate requirements/decisions — notably **E-2 = prior readiness report `NOT_READY`** (blocking until an explicit rerun supersedes it), **E-3** epics corrective addendum (23 placeholders), **E-4** sprint-status, **E-6** live-AppHost (19 pass/56 fail), **E-7** codebase audit (9 P1 + 7 P2), **E-8** release handoff `BLOCKED`.

### PRD Completeness Assessment (initial)

- **Strengths:** Every FR carries testable "Consequences"; NFRs are quantified with concrete envelopes; success metrics tie back to FR/NFR IDs; §5 gives a single observable-state contract; glossary is precise; §2.3 makes release-blocking scope explicit; addendum routes each mechanism to its owning downstream artifact.
- **Clarity:** Requirements are stable-ID'd, non-overlapping, and consistently reference UJ-1..UJ-5. No obvious internal contradiction between FR statements.
- **Open dependency on downstream artifacts:** The PRD deliberately defers wire schemas, state machines, and crypto to architecture/API — so readiness hinges on whether the **architecture spine + epics** actually cover all 24 FR / 11 NFR and the addendum constraints. This is the core traceability question for the next steps.
- **Watch items to carry into traceability:** (1) epics.md cites the *superseded* architecture.md as input; (2) 23 Epic 6–8 placeholders must now be real outcome-based stories; (3) release-blocking FR-21/22/24 + NFR-1..11 must each have an owning story + verification per addendum §7.

## 3. Epic Coverage Validation

Source: `epics.md` (status: complete, updated 2026-07-14).

### Structural finding (root cause)

`epics.md` has two strata:
- **Epics 1–5** — fully decomposed with detailed Given/When/Then acceptance criteria. Written against an **earlier 22-FR PRD**. Its "Requirements Inventory" and "FR Coverage Map" stop at **FR-22** and assert *"All 22 FRs mapped."* Its NFR inventory is a bespoke **NFR-1–9** that does **not** correspond to the current PRD's NFR-1–11.
- **Epics 6–8 "Approved Corrective Epic Addendum" (2026-07-14)** — **23 stories (6.1–6.7, 7.1–7.7, 8.1–8.9) with titles + one-paragraph scope but NO acceptance criteria.** These are the "23 placeholders" the PRD addendum (§ Readiness) states *"must pass implementation-readiness review before sprint planning or story creation."*

**Consequence:** the top-of-file coverage map is **stale relative to the current 24-FR / 11-NFR PRD.** FR-23 and FR-24 are absent from both the Requirements Inventory and the FR Coverage Map; the PRD's NFR-1–11 are not re-mapped.

### FR Coverage Matrix (current PRD FR-1..FR-24 → epics)

| FR | PRD requirement | Epic/story coverage | Status |
|----|-----------------|---------------------|--------|
| FR-1 | Create Project (idempotent Durable Task; Folder-bound) | E1 S1.4 (create) + **E7 S7.3** (durable Folder task) | ✓ (durable path in placeholder E7) |
| FR-2 | Open Project | E1 S1.7 | ✓ |
| FR-3 | Update Project Setup | E1 S1.8 | ✓ |
| FR-4 | Archive Project (Preview + single-use confirm + Durable Task) | E1 S1.8 (command) + **E7 S7.6** (durable preview/confirm) | ✓ (Preview/confirm in placeholder E7) |
| FR-5 | List Projects | E1 S1.7 | ✓ |
| FR-6 | Link Conversation | E2 S2.3 + **E7 S7.4** | ✓ |
| FR-7 | Move Conversation | E2 S2.3 + **E7 S7.4** | ✓ |
| FR-8 | Set Project Folder | E2 S2.4 + **E7 S7.3** | ✓ |
| FR-9 | Link File Reference | E2 S2.5 | ✓ |
| FR-10 | Link Memory | E2 S2.6/2.7 | ✓ |
| FR-11 | Unlink Context Reference | E2 S2.3/2.5/2.7 + **E7 S7.6** | ✓ |
| FR-12 | Resolve From Conversation | E4 S4.2 | ✓ |
| FR-13 | Resolve From Attachments | E4 S4.3 | ✓ |
| FR-14 | Confirm Ambiguous Project (15-min single-use artifact) | E4 S4.4 + **E7 S7.2/7.5** (artifact binding) | ✓ (binding in placeholder E7) |
| FR-15 | Propose New Project | E4 S4.5 + **E7 S7.5** | ✓ |
| FR-16 | Get Project Context | E3 S3.2 | ✓ |
| FR-17 | Explain Context Selection | E3 S3.3 | ✓ |
| FR-18 | Refresh Project Context | E3 S3.4 | ✓ |
| FR-19 | Validate Project Setup (Metadata Classification) | E1 S1.4/1.8 (setup validation) | ⚠️ **Partial** — Metadata Classification / `SensitiveMetadataTierValidator` / `400 projectMetadata.metadataClass` (PRD addendum §4.1, gate E-9) is **not assigned to any story with ACs** |
| FR-20 | Retrieve Conversation-Start Setup | E3 S3.5 | ✓ |
| FR-21 | Record Project Audit Events | E5 S5.1 + **E7 S7.4** | ✓ |
| FR-22 | Support Operator Read Access | E5 S5.2 + **E8 S8.6** | ✓ |
| FR-23 | Restore Archived Project | E5 S5.9 (restore action) + **E7 S7.6** (durable); **E7 gate cites FR-23** | ⚠️ **Partial** — **absent from Requirements Inventory & FR Coverage Map**; no dedicated story with ACs |
| FR-24 | Create Safe Diagnostic Export (bounded, separate perm) | E5 S5.7 (export view/UX-DR18) | ⚠️ **Partial** — **absent from inventory & coverage map**; release-blocking bounds (1 MiB / 500 / 100 rows, 2 concurrent, separate permission) not formally storied with ACs |

### Missing / Under-covered Requirements

- **FR-23 Restore Archived Project (release-blocking-adjacent, realizes UJ-5):** Not inventoried; only implicit via a maintenance action (S5.9) and a placeholder durable story (E7 S7.6). No acceptance criteria bind Preview→confirm→Folder-verification→`NeedsAttention`-on-partial semantics.
- **FR-24 Create Safe Diagnostic Export (release-blocking):** Not inventoried; only S5.7 delivers an export UI. The quantitative contract (`projects.safe-diagnostic-export.v1`, ≤1 MiB, ≤500 reference rows, ≤100 audit rows, 2 concurrent/Tenant, export-permission ≠ read-permission, no retention) has no owning story with ACs.
- **FR-19 canonical Metadata Classification (gate E-9):** The four-value vocabulary + shared validator + `400 ValidationFailure` leakage-safe rejection path is required by the current PRD but unmapped in the story set.
- **NFR re-mapping (carried to Step 5):** epics NFR-1–9 ≠ PRD NFR-1–11. No story explicitly owns **PRD NFR-2 (encryption/KMS rotation)**, **PRD NFR-7 (back-pressure/rate limits/timeouts)**, or the **PRD NFR-8 retention envelopes** (30-day results, 365-day audit, 15-min artifacts). Availability/RTO targets of **PRD NFR-3** (99.9%, RTO 15 min, 5-min task resume) are only obliquely in E8 S8.7/8.8.

### Coverage Statistics

- **Total current-PRD FRs:** 24
- **FRs in the epics FR Coverage Map:** 22 (FR-1–FR-22) → **91.7% formally mapped**; FR-23 & FR-24 unmapped.
- **FRs with a traceable implementation path (incl. placeholder Epics 6–8):** 24/24 have *some* path, but **21 have detailed-AC stories**; FR-19 (classification aspect), FR-23, and FR-24 lack dedicated AC-bearing stories.
- **Corrective stories still at placeholder (title-only) status:** 23 (all of Epics 6–8).
- **Verdict for this step:** Coverage is **substantially present but not traceably complete against the current PRD** — the coverage map must be regenerated to 24 FRs / 11 NFRs, and FR-19-classification, FR-23, FR-24 need owning stories with acceptance criteria.

## 4. UX Alignment Assessment

Sources: `ux-design-specification.md` (+ `ux-design-directions.html`); cross-checked against PRD §3/§7/addendum §6 and `ARCHITECTURE-SPINE.md`.

### UX Document Status

**Found.** A complete, mature UX specification (1,031 lines) covering vision, platform strategy, design system, journeys, components, patterns, responsive strategy, and accessibility. It is the source of the epics' UX-DR1–UX-DR28.

### UX ↔ PRD Alignment

- **Scope boundary is explicit and correct:** the UX spec scopes UX to the **administrative/operational surfaces (CLI + MCP + FrontComposer Web)** and explicitly states the **end-user Chatbot conversation experience belongs to `Hexalith.Chatbot` and is outside this module's UX scope.** This matches PRD §3.1/§3.2 and addendum §6 (Chatbot Companion Contract). ✓
- **Accessibility:** UX targets **WCAG 2.2 AA** with keyboard, visible focus, non-color-only status, screen-reader tables/timelines, focus trapping, reduced-motion, 320px reflow — matches PRD **NFR-9** and SM-5. ✓
- **Operator journeys:** UX covers inspect / resolution-trace / reference-health / audit-timeline / safe-maintenance (archive/restore/relink/unlink/reevaluate) / Safe Diagnostic Export — supports **FR-4, FR-22, FR-23 (restore), FR-24 (export)**. ✓ (Note: UX predates the FR-23/FR-24 renumbering but already contains restore + a Safe Diagnostic Export component, so it is substantively compatible.)
- **Metadata-only invariant:** UX forbids exposing transcripts/file contents/prompts/secrets/memory payloads on every surface — matches PRD NFR-1 privacy + `NoPayloadLeakage`. ✓

### UX ↔ Architecture Alignment

The architecture spine **fully supports** the UX requirements — strong alignment throughout:

| UX requirement | Spine support |
|----------------|---------------|
| One operational model, three surfaces (CLI/MCP/Web) | AD-19 (one observable transport mapping), AD-2, AD-29 (MCP contained adapter) |
| FrontComposer-generated Web, no bespoke UI | AD-16, AD-24 (`Projects.UI.Contracts` descriptors) |
| Shared vocabulary / reason codes rendered identically | AD-16, AD-32, Consistency Conventions |
| Metadata-only / no payload leakage | AD-7, AD-11, logging convention |
| Safe Diagnostic Export (bounded, safe) | AD-21 |
| Five-state maintenance/command lifecycle + safe denial | AD-4, AD-19 |
| WCAG 2.2 AA, 200% zoom, 320px reflow | AD-34 (accessible completion = release invariant) |
| Role-specific action authority, surface-invariant | AD-33 |

No contradiction found between the UX spec and the architecture spine.

### Alignment Issues / Warnings

- **⚠️ Chatbot companion journey UX + accessibility is unowned in the epics.** The PRD **NFR-9 explicitly lists "Chatbot candidate, confirmation, cancellation, recovery, and task journeys"** as in-scope for WCAG 2.2 AA, and SM-5 validates FR-14/FR-15 accessibility. Correctly, this module's UX spec defers that experience to `Hexalith.Chatbot`, and the spine accounts for it (AD-34 binds "Chatbot **and** operator presentation owners"; AD-29; Deferred table defers Chatbot interaction design to UX/API). **But `epics.md` accessibility stories (5.11, 8.6) cover only the operator surfaces** — there is **no owning story ensuring the cross-repository Chatbot companion accessibility/integration evidence exists before Projects release**, which addendum §6 requires ("authenticated integration evidence required before Projects release"). Carry to gap analysis.
- **⚠️ UX spec is not versioned against the FR-23/FR-24 renumbering.** Content is compatible (restore + export already present), but the spec has not been re-stamped to the current 24-FR PRD. Low-risk documentation currency issue.
- **✓ No architectural support gaps.** Every UX capability has an owning architecture decision; the architecture does not under-serve the UX.

## 5. Epic Quality Review

Scope: 8 epics / 44 stories in `epics.md`, reviewed against create-epics-and-stories standards (user value, independence, dependency direction, story sizing, acceptance-criteria quality, greenfield/brownfield fit, traceability).

### Two-stratum structure (the central quality fact)

- **Epics 1–5 (Stories 1.1–5.12)** — completed, fully-decomposed, detailed Given/When/Then acceptance criteria. **Implementation history, not release authorization** (per addendum + Epic-6/7/8 preamble).
- **Epics 6–8 (Stories 6.1–6.7, 7.1–7.7, 8.1–8.9)** — the Approved Corrective Addendum. **23 stories = title + one paragraph of scope, with NO acceptance criteria.** These are the "23 placeholders" the addendum says *"must pass implementation-readiness review before sprint planning or story creation."*

### 🔴 Critical Violations

- **C1 — All 23 corrective stories (Epics 6–8) lack acceptance criteria and are not implementation-ready.** They have no Given/When/Then, no testable outcomes, no per-story verification command/fixture/artifact. By the rubric ("Stories must be independently completable / clear acceptance criteria"), none of Epics 6–8 is a schedulable story. This is the primary readiness blocker, and it is *self-declared* by the planning artifacts (findings inventory, not stories).
- **C2 — Release-completeness is a latent cross-epic dependency that the per-epic "Standalone: Yes" labels obscure.** Epics 1–5 each claim standalone delivery, but production-correct behavior of FR-1 (durable Folder-first creation), FR-4/FR-23 (archive/restore as bound tasks), FR-14/FR-15 (bound confirmation artifacts), and FR-21 (durable receipts) is only completed in **Epic 7**, and all release evidence is only in **Epic 8**. So Epics 1–5 are shippable *as internal evidence only* — the "Standalone: Yes" claim is true for build-time but misleading for release without the containment caveat. (The document does state the containment elsewhere; the per-epic labels should carry it.)

### 🟠 Major Issues

- **M1 — Epics/architecture release-gate desync (Story 8.9 vs 8.11).** The architecture spine AD-30 requires the machine-checkable evidence source `implementation-readiness-traceability-matrix.yaml` (schema `hexalith.readiness-evidence.v1`) and cites **Story 8.11** for deployment/rollback + Jerome/John dated acceptance. `epics.md` Epic 8 ends at **Story 8.9** ("Record deployment and stakeholder acceptance") and does not mention the evidence-matrix artifact. The story set is not re-synced to the current spine.
- **M2 — Stale requirements traceability at the top of `epics.md`.** Requirements Inventory + FR Coverage Map cover **FR-1–22 / NFR-1–9** and assert "All 22 FRs mapped," but the current PRD is **FR-1–24 / NFR-1–11**. FR-23, FR-24, and the FR-19 Metadata-Classification path (AD-31/E-9) have no formal mapping or AC-bearing story. PRD NFR-2 (encryption/KMS), NFR-7 (back-pressure), NFR-8 (retention envelopes) are not re-mapped to stories.
- **M3 — `epics.md` predates and is unreconciled with the current architecture spine.** Its `inputDocuments` cite the **superseded** `architecture.md`; the `ARCHITECTURE-SPINE.md` (2026-07-15/-16) is newer and richer (34 ADs, Epic-6/7/8 binding, evidence-matrix model). The epics were not regenerated against the spine, so story→AD traceability is implicit at best.
- **M4 — Epics 6–8 are technical/architectural epics** (platform migration, durable workflows, release evidence) rather than user-outcome epics. Defensible in a brownfield corrective context (the outcome is "safe, supportable production"), but each corrective story should be reframed with an explicit operational/user outcome and acceptance criteria when it is de-placeholdered.

### 🟡 Minor Concerns

- **m1 — Technical/enabler stories without direct user value in Epics 1–5:** 1.2 (vocabulary/taxonomy), 1.3 (OpenAPI spine), 1.5 (rebuild/idempotency), 1.9 (Aspire/Dapr topology), 2.2 (upstream Conversations capability), 2.6 (Memories spike), 3.1 (assembly policy), 4.1 (resolution engine). Common and justified as foundational slices/spikes; called out for completeness.
- **m2 — "Greenfield" label is now stale.** The overview calls the module greenfield, but Epics 6–8 treat it as **brownfield** with existing event history requiring shadow-read cutover (AD-6/AD-17). Framing should be updated.
- **m3 — Some Epic 1–5 ACs are compound** (multiple assertions under one Then), slightly reducing per-criterion testability; acceptable but splittable.
- **m4 — UX-DR set + UX spec not re-stamped** to the FR-23/FR-24 renumbering (carried from Step 4).

### ✅ Strengths (best-practice compliance that holds)

- **Greenfield scaffold story present:** Story 1.1 is the "set up module from established sibling shape" starter story (matches the starter-template requirement).
- **Clean dependency direction in Epics 1–5:** no forward dependencies; enablers precede consumers; the deferred FR-1 auto-folder is a one-directional deferral (Epic 1 works without it); cross-module prerequisites (AR-G1–G4 / PR-1–4) are sequenced as separate submodule-first stories.
- **Strong acceptance criteria in Epics 1–5:** consistent Given/When/Then, with mandatory **negative-path** coverage (cross-tenant isolation, `NoPayloadLeakage`, idempotency dedup, projection rebuild/replay).
- **Read models created per-epic as needed** (list/detail in E1, reference index in E2, audit in E5) — no upfront over-provisioning.
- **Cross-cutting foundational slices FS-1..FS-8** defined once and CI-enforced rather than reinvented per epic.

### Best-Practices Compliance Checklist

| Check | Epics 1–5 | Epics 6–8 |
|-------|-----------|-----------|
| Epic delivers user value | ✓ (mostly) | ⚠️ technical/corrective (M4) |
| Epic can function independently | ✓ build-time / ⚠️ release-time (C2) | N/A — corrective overlay |
| Stories appropriately sized | ✓ | 🔴 placeholders (C1) |
| No forward dependencies | ✓ | ⚠️ re-open Epics 1–5 by design (C2) |
| Read models created when needed | ✓ | n/a |
| Clear acceptance criteria | ✓ | 🔴 absent (C1) |
| Traceability to FRs maintained | ⚠️ stale to FR-22 (M2) | partial (FR gates cited, no ACs) |

## 6. Current Implementation State (ground truth, 2026-07-16)

Verified against `sprint-status.yaml`, the Story 6.1 dev-auto result, and the filesystem:

- **Epics 1–5: `done`** — implementation history; every story completed with a spec file.
- **Epics 6–8: `backlog`** — all 23 corrective stories remain placeholders; **no Story 6.x/7.x/8.x spec file has been created.**
- **Story 6.1 development is `blocked`** by design. The dev automation halted with: *"no superseding independent implementation-readiness rerun has returned READY"* and correctly refused to synthesize a story spec from placeholder text (per the approved 2026-07-15 proposal). **The governance gate is working as intended.**
- **The AD-30 canonical evidence source `implementation-readiness-traceability-matrix.yaml` (schema `hexalith.readiness-evidence.v1`) does NOT exist.**
- **Prior independent verdicts are `NOT_READY`:** E-2 (2026-07-14) and the 2026-07-15 rerun. Their triggers are still open: 29 recorded readiness issues, 23 placeholders, live E2E **19 passed / 56 failed** (E-6), release handoff **BLOCKED** (E-8), and 9 P1 + 7 P2 audit findings (E-7).
- **Rebaseline progress:** PRD ✅ final (2026-07-15); Architecture spine ✅ final (2026-07-16, was an empty template on 2026-07-15); **Epics 6–8 rewrite ❌ not started** (epics.md unchanged since 2026-07-14 22:33); post-rebaseline READY rerun ❌ none.

## 7. Summary and Recommendations

### Overall Readiness Status

## 🔴 NOT READY

The **baseline is strong and largely ready** (PRD, Architecture, UX, and Epics 1–5 are high quality and mutually aligned), but the **release-blocking corrective scope is not implementation-ready**: Epics 6–8 are 23 acceptance-criteria-less placeholders, the mandated machine-checkable evidence matrix is absent, and the epics have not been reconciled to the current 24-FR PRD or the current architecture spine. This is not planning sloppiness — it is a *deliberately gated, not-yet-executed* rewrite step. This assessment (an independent rerun) **confirms containment must remain in place** and corrective development must not start.

### What IS ready (credit where due)

- **PRD** — final, complete, internally consistent; 24 FRs with testable consequences, 11 quantified NFRs, an explicit §5 observable-state contract, release classification, and an evidence-gate index.
- **Architecture spine** — final and comprehensive; 34 ADOPTED decisions binding FR-1–24 / NFR-1–11, a capability→AD map covering every FR/NFR, explicit cutover/migration model, and a machine-checkable release-acceptance model (AD-30).
- **UX spec** — complete, correctly scoped to operator surfaces, and **fully supported by the architecture** (no architectural support gaps).
- **Epics 1–5** — done, with strong BDD acceptance criteria (including mandatory negative-path coverage) and clean dependency hygiene.
- **Governance** — the dev automation correctly self-blocked rather than fabricating stories; prior NOT_READY verdicts are being respected.

### Critical Issues Requiring Immediate Action (release blockers)

1. **[C1] Epics 6–8 are 23 placeholders with no acceptance criteria.** The entire release-blocking corrective scope (platform boundary/identity, durable workflows, production evidence) is not schedulable. → Rewrite into outcome-based, AC-bearing, independently verifiable stories.
2. **[AD-30] The canonical evidence matrix does not exist.** `implementation-readiness-traceability-matrix.yaml` (schema `hexalith.readiness-evidence.v1`) is required as the release-acceptance source of truth. → Author it, one row per requirement/finding with AD, UX journey, story, owner, pinned revision, environment/fixture, command, artifact, status, and release disposition.
3. **[M2] Requirements traceability is stale.** epics.md maps FR-1–22 / NFR-1–9; the current PRD is FR-1–24 / NFR-1–11. **FR-23 (Restore), FR-24 (Safe Diagnostic Export), and the FR-19 Metadata-Classification path (AD-31/E-9) have no AC-bearing story;** PRD NFR-2/NFR-7/NFR-8 are not re-mapped. → Regenerate the coverage map to the 24-FR / 11-NFR PRD and give these an owning story.
4. **[M1] Epics/architecture release-gate desync.** The spine (AD-30) references **Story 8.11** + the evidence matrix; epics.md Epic 8 ends at **Story 8.9**. → Reconcile the Epic 8 story set and gates to the spine.
5. **[M3] Epics predate/are unreconciled with the current architecture spine** and cite the *superseded* `architecture.md`. → Regenerate Epics 6–8 against `ARCHITECTURE-SPINE.md`, adding story→AD traceability.
6. **[Step 4 warning] Chatbot companion accessibility/integration evidence is unowned in stories.** PRD NFR-9/SM-5 require accessible Chatbot candidate/confirmation/proposal/recovery journeys (cross-repo, owned by Hexalith.Chatbot). → Add an owning story/gate for the cross-repository authenticated integration + a11y evidence required before Projects release.
7. **[Open triggers] Prior NOT_READY findings are unresolved:** live E2E 19/56 (Story 8.6), release handoff BLOCKED (Story 8.9/8.11), 9 P1 + 7 P2 audit findings (E-7). → Each needs an owning story + verification evidence before READY.

### Recommended Next Steps (the unblock sequence)

1. **Rewrite Epics 6–8** from the approved corrective proposals into outcome-based executable stories with full Given/When/Then acceptance criteria, entry/verification/rollback gates, and explicit FR/NFR/AD/UX traceability — **without weakening** the proposals' security/durability/compatibility/release gates.
2. **Author the evidence matrix** `implementation-readiness-traceability-matrix.yaml` and wire the `hexalith-evidence validate` gate (AD-30); ensure every FR-1–24, NFR-1–11, P1/P2 finding, and release case has a row with a named owner, pinned revision, command, and artifact.
3. **Regenerate the epics traceability** (FR-1–24 / NFR-1–11) and add dedicated AC-bearing stories for **FR-23, FR-24, FR-19 Metadata-Classification (AD-31)**, plus explicit ownership for **NFR-2 (encryption/KMS), NFR-7 (back-pressure), NFR-8 (retention)** and the **Chatbot companion a11y** cross-repo evidence.
4. **Reconcile Epic 8 to the spine** (Story 8.9 → 8.11 + deployment/rollback/dated-acceptance gate) and refresh stale framing (greenfield→brownfield; superseded architecture input pointer; UX FR-23/24 re-stamp).
5. **Re-run implementation readiness in a fresh context** once 1–4 are done; **only a `READY` verdict** authorizes de-placeholdering into `ready-for-dev`, sprint planning, and corrective development. Production, autonomous consequential MCP mutation, and proposal-confirmation remain blocked until Story 8.11 + Jerome/John dated acceptance.

### Final Note

This assessment reviewed 4 baseline documents + 8 sprint-change proposals across 6 analysis dimensions and identified **2 critical violations, 4 major issues, 4 minor concerns, and 7 release-blocking action items**, against a genuinely strong and well-aligned PRD/architecture/UX baseline. The single root cause is that the **corrective planning layer (Epics 6–8 de-placeholdering + evidence matrix) — the last step of the approved rebaseline sequence — has not yet been executed.** The verdict is **NOT READY**; containment is correct and must hold. Complete the five next steps, then rerun this workflow. These findings are the actionable punch-list to reach `READY`.

---

**Assessment date:** 2026-07-16 · **Assessor:** Implementation Readiness workflow (independent rerun) · **Supersedes:** implementation-readiness-report-2026-07-15.md (also NOT_READY)

