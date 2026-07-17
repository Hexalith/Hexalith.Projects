---
stepsCompleted: ['step-01-document-discovery', 'step-02-prd-analysis', 'step-03-epic-coverage-validation', 'step-04-ux-alignment', 'step-05-epic-quality-review', 'step-06-final-assessment']
documentsIncluded:
  prd: 'prds/prd-Hexalith.Projects-2026-05-24/prd.md'
  prdAddendum: 'prds/prd-Hexalith.Projects-2026-05-24/addendum.md'
  architecture: 'architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md (sole normative architecture)'
  architectureSuperseded: 'architecture.md (status: superseded 2026-07-16, historical evidence)'
  epics: 'epics.md'
  ux: 'ux-design-specification.md'
---

# Implementation Readiness Assessment Report

**Date:** 2026-07-17
**Project:** Hexalith.Projects

## Document Inventory

### Assessment Set (confirmed by user)

| Type | File | Size | Modified |
| --- | --- | --- | --- |
| PRD | `prds/prd-Hexalith.Projects-2026-05-24/prd.md` | 46 KB | 2026-07-15 |
| PRD Addendum | `prds/prd-Hexalith.Projects-2026-05-24/addendum.md` | 22 KB | 2026-07-15 |
| Architecture | `architecture.md` | 52 KB | 2026-07-16 |
| Epics & Stories | `epics.md` | 173 KB | 2026-07-17 |
| UX Specification | `ux-design-specification.md` | 54 KB | 2026-05-29 |

### Supporting Context (not primary assessment targets)

- `architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md` — architecture spine (source kernel of `architecture.md`, same timestamp) plus 6 review/reconcile files
- `epics-architecture-conformance-checklist-2026-07-16.md` (2026-07-17)
- `implementation-readiness-traceability-matrix.md` + `.yaml` (2026-07-17)
- 12 sprint change proposals (latest: `sprint-change-proposal-2026-07-16-implementation-readiness-rerun.md`)
- Prior readiness reports: 2026-05-24, 2026-07-14 (×2 + pre-rerun), 2026-07-15, 2026-07-16
- Product brief `briefs/brief-Hexalith.Projects-2026-05-24/brief.md` + 6 research documents (2026-05-24)
- `ux-design-directions.html` — visual companion to the UX specification

### Discovery Notes

- **Architecture whole vs spine (corrected during step 4):** `architecture.md` carries frontmatter `status: superseded` (supersededAt 2026-07-16, historicalEvidence: true) and points to `ARCHITECTURE-SPINE.md` as the sole current architecture. The epics were reconciled against the spine (AD-1…AD-34). Resolved: **ARCHITECTURE-SPINE.md is the architecture assessment target**; `architecture.md` is May 2026 historical evidence. No duplicate conflict — supersession is explicit in both files.
- **Epics backup:** `epics.md.pre-reconcile-2026-07-16.bak` ignored (`.bak` backup, not an active document).
- **No missing document types** — PRD, Architecture, Epics, and UX all present.
- **Freshness spread:** UX spec (2026-05-29) predates PRD (2026-07-15), architecture (2026-07-16), and epics (2026-07-17) — UX↔epics alignment drift to be checked in later steps.

## PRD Analysis

**Source:** `prds/prd-Hexalith.Projects-2026-05-24/prd.md` (status: final, updated 2026-07-15) + `addendum.md`.

### Functional Requirements

**FR-1: Create Project** — Chatbot can admit Project creation as an idempotent Durable Task. A Project becomes caller-visible and `Active` only after exactly one authorized Project Folder is verified and bound. Realizes UJ-2. Testable consequences: Project name is the only required user-authored field (canonical requests also carry system-supplied Metadata Classification); supplied Folder is authorized/verified, else same-name Folder creation is requested from Hexalith.Folders; admission returns a pollable Durable Task, not an immediately Active Project; dependency denial/timeout/cancellation/duplicate delivery/lost response/reconciliation never exposes an Active folderless Project; equivalent Idempotency Key retries return the original task, materially different reuse conflicts; terminal success exposes Project identity only after Read-Model-Confirmed Completion; historical unversioned name-only creation remains supported throughout v1; creation never duplicates transcripts, file contents, prompts, secrets, or Memory payloads.

**FR-2: Open Project** — Chatbot can open an authorized Project and receive the metadata, lifecycle state, Project Setup, and references needed to initialize a Conversation. Realizes UJ-1. Consequences: Tenant/actor-visible data only; follows §5 Context Response State / Evidence Freshness State / Recovery Action Code semantics; pre-activation creation tasks are not exposed through open APIs; Archived or unavailable Projects are identified and cannot silently become active Conversation context.

**FR-3: Update Project Setup** — Chatbot can update Project Setup used for Conversation continuity. Consequences: updates are idempotent, durable, observable from the authoritative read model; setup may include goals, user-facing instructions, context preferences, source inclusion/exclusion policy, Conversation-start defaults; setup describes Conversation behavior/context policy, not model-provider internals; updates remain additive and serialization-tolerant and reject secrets, unrestricted paths, and foreign payloads.

**FR-4: Archive Project** — An authorized Project User, Tenant Operator, or Tenant Project Administrator can archive an Active Project through server Preview, single-use confirmation, and an idempotent Durable Task (restore is FR-23). Consequences: lifecycle remains exactly `Active`/`Archived`; confirmation invalidated when actor authority or Project version changes; Archived Projects excluded from Resolution unless explicitly requested; completion not reported until read model confirms `Archived`; existing references remain auditable after archival.

**FR-5: List Projects** — Authorized callers can list visible Active and Archived Projects. Consequences: Tenant-scoped, authorization-filtered, filterable by lifecycle state; each result carries Project identity, name, lifecycle state, current version, Folder availability, and §5 response/freshness/recovery metadata; pre-activation tasks never appear as Projects; cursor pages default 50, cap 200, cursors scoped to the authenticated query.

**FR-6: Link Conversation** — An authorized Project User can link an existing Conversation to a Project. Realizes UJ-1, UJ-3. Consequences: a Conversation belongs to exactly one Project in v1; explicit actor-selected additive link uses an idempotent Durable Task without second confirmation, inferred link requires Preview+confirmation; linking an already-assigned Conversation requires FR-7 (no second membership); authorization failure prevents any protected access or durable effect; link stores stable identity/metadata, never transcript content.

**FR-7: Move Conversation Between Projects** — An authorized Project User or Tenant Project Administrator can move a Conversation through Preview, single-use confirmation, and an idempotent Durable Task. Consequences: Preview binds both Projects, the Conversation, actor, current resource versions; completion yields exactly one membership plus a durable cross-context receipt; failure/duplicate delivery/lost response cannot leave two memberships silently valid; metadata-only audit; fails closed when either Project or the Conversation cannot be authorized.

**FR-8: Set Project Folder** — An authorized Project User can set the single Project Folder; a Project User or Tenant Project Administrator can replace it through Preview and confirmation. Realizes UJ-2. Consequences: every Active Project has exactly one authorized Folder; initial actor-selected binding is idempotent, inferred binding requires confirmation; replacement binds old and new Folder evidence to the Confirmation Artifact and completes only after read-model confirmation; stores Folder identity/metadata, never file contents or unrestricted paths; Hexalith.Folders remains authorization and system-of-record boundary.

**FR-9: Link File Reference** — An authorized Project User can link a File Reference without changing the Project Folder. Consequences: File References are optional and do not replace the Folder; actor-selected additive linking idempotent, inferred linking requires confirmation; stores stable File identity/metadata only; authorization delegated to Hexalith.Folders.

**FR-10: Link Memory** — An authorized Project User can link a Memory. Realizes UJ-1, UJ-3. Consequences: actor-selected additive linking idempotent, inferred linking requires confirmation; stores stable Memory identity/metadata only; authorization delegated to Hexalith.Memories.

**FR-11: Unlink Context Reference** — An authorized Project User or Tenant Project Administrator can unlink a Conversation, File Reference, or Memory through Preview, confirmation, and an idempotent Durable Task; the Project Folder can be replaced but not removed from an Active Project. Consequences: unlinking removes only the association, never deletes the resource; Preview identifies affected reference and current Project version; completion durable, metadata-only audited, read-model-confirmed; fails closed on stale authorization or resource evidence.

**FR-12: Resolve Project From Conversation** — Chatbot can request Candidate Projects for a Conversation with no explicit Project. Realizes UJ-3. Consequences: result is `NoMatch`/`SingleCandidate`/`MultipleCandidates` with current Resolution Reason Codes; only Active, read-model-confirmed Projects considered by default; pre-activation tasks and unauthorized/stale resources cannot become candidates; response follows §5; `Unavailable` and `Denied` never return a selected candidate.

**FR-13: Resolve Project From Attachments** — Chatbot can resolve Candidate Projects from an attached Project Folder or File References. Realizes UJ-2. Consequences: matching uses current authorized Folder/File identity and metadata, not file contents; applicable candidates include `ProjectFolderMatched`/`FileReferenceMatched` reason codes; missing/stale/unavailable authorization evidence fails closed.

**FR-14: Confirm Ambiguous Project** — With multiple candidates, Chatbot presents an accessible, unselected comparison and records the Project User's choice through a Confirmation Artifact and Durable Task. Realizes UJ-3. Consequences: no candidate silently or visually preselected; artifact bound to Tenant, actor, action, Conversation, candidates, normalized request, Preview, current versions; expires after 15 minutes, single-use; stale/expired/replayed/tampered confirmation rejected safely, requires fresh Preview; only Read-Model-Confirmed Completion creates/updates association and audit history; Chatbot supports confirmation, cancellation, retry, expiry/staleness, lost-response recovery, and task status states.

**FR-15: Propose New Project** — When no suitable Project exists, Chatbot can present a proposed Project and admit creation only after the Project User confirms a bound Preview. Realizes UJ-2. Consequences: proposal may suggest name/setup metadata but creates nothing before confirmation; artifact binds initiating Conversation, authorized attachments, Folder plan, normalized request, current evidence; confirmed creation follows FR-1 and exposes no Project before Folder binding and read-model confirmation; non-success follows §5 recovery contract (cancellation → `Cancelled`, terminal failure → `Failed`, expired/stale evidence creates no task).

**FR-16: Get Project Context** — Chatbot can request Project Context for an Active Project. Realizes UJ-1, UJ-4. Consequences: Tenant-scoped, actor-authorized, only for a read-model-confirmed Active Project with exactly one authorized Folder; contains Project Setup and reference metadata, not foreign payloads; follows §5, representing every excluded/stale/rebuilding/unavailable reference as a metadata-only component; `Denied` discloses no protected detail.

**FR-17: Explain Context Selection** — Authorized callers can obtain current metadata explaining why a reference was included or excluded. Realizes UJ-4. Consequences: explanations are current Resolution Traces, not reconstructed history; traces contain no secrets, payloads, prompts, unrestricted paths, raw upstream problems, or unconfirmed-candidate detail; request-scoped, not persisted; only confirmed outcomes enter audit history.

**FR-18: Refresh Project Context** — Chatbot can request a read-only refresh after links, setup, authorization, or resource availability changes. Consequences: recomputes from current authorized Project/Conversation/Folder/File/Memory/version metadata; never mutates state, creates no maintenance audit event; refreshed response follows §5 including new snapshot metadata, component evidence, recovery actions, and binding transition rules for `Partial`/`Unavailable`/`Complete`.

**FR-19: Validate Project Setup** — Projects validates setup and creation admission before accepting durable work. Consequences: name remains the only required user-authored creation field; canonical requests require valid system-supplied Metadata Classification, rejected before command submission when invalid; validation permits supplied authorized Folder or same-name Folder creation but never defaults a caller-visible Project to Active before Folder completion; rejects secrets, unrestricted paths, unsupported references, unsafe control/invisible characters, foreign payloads; failures identify safe field/reason codes without echoing sensitive values.

**FR-20: Retrieve Conversation-Start Setup** — Chatbot can retrieve the subset of Project Setup needed to start or resume a Conversation. Consequences: includes goals, user-facing instructions, context preferences, default source policy; excludes internal audit metadata and unavailable/unauthorized references; bound to one authorized `projectVersion` + `asOf` snapshot; follows §5 — first response admissible only for `Complete`/`Partial`; `Unavailable`/`Denied` blocks first-response admission and returns applicable Recovery Action Codes without re-querying every bounded context.

**FR-21: Record Project Audit Events** — Projects records metadata-only audit events for consequential task admission and outcome, confirmed Project mutations, security-relevant confirmation outcomes, reconciliation, and Safe Diagnostic Export. Consequences: audit covers task admission/terminal outcome; confirmation use/cancellation; rejection of stale/replayed/tampered confirmations; authorization denial; creation, archive, restore, move, relink, Folder replacement, unlink, confirmed resolution, confirmed proposed creation; manual reconciliation; export creation; stable upstream receipt identifiers. Equivalent idempotent retries create no duplicate audit events; intermediate task states/polls/retries/dependency latency/notifications/unused expiry/read-only Resolution Traces remain telemetry, not durable audit; audit contains Tenant, actor, Project/action identity, timestamp, safe reason/outcome codes, affected reference identifiers — never payloads or secrets.

**FR-22: Support Operator Read Access** — Tenant Operators and Tenant Project Administrators can inspect authorized Project metadata, lifecycle state, references, Durable Task status, confirmed resolution outcomes, and audit metadata. Consequences: Tenant-scoped, action-authorized, metadata-only across Web, CLI, MCP; Project Users may inspect only their own permitted task status through Chatbot; pre-activation tasks remain separate from list/open APIs (Operators/Administrators may inspect safe status; Administrators may perform authorized reconciliation); read permission alone grants neither Safe Diagnostic Export nor a mutation.

**FR-23: Restore Archived Project** — An authorized Project User, Tenant Operator, or Tenant Project Administrator can restore an Archived Project through Preview, confirmation, and an idempotent Durable Task (counterpart to FR-4). Realizes UJ-5. Consequences: Preview verifies Tenant, actor, authority, current Project version, exactly one authorized Folder; if prior Folder invalid/missing, Preview requires authorized replacement or same-name Folder creation before confirmation; Project remains Archived until Folder evidence and read-model-confirmed restore complete; if Folder creation succeeds but activation cannot commit, task enters `NeedsAttention` — Projects never auto-deletes a Folders-owned resource; stale/unavailable evidence, replay, cancellation, duplicate delivery, concurrency, lost response cannot expose an invalid Active Project; completion and reconciliation outcomes audited metadata-only.

**FR-24: Create Safe Diagnostic Export** — A separately authorized Tenant Operator or Tenant Project Administrator can create a bounded Safe Diagnostic Export through Web, CLI, or MCP. Consequences: export permission distinct from FR-22 read permission; Chatbot cannot create exports; every attempt/outcome audited metadata-only; complete encoded export (envelope + truncation metadata) ≤ 1 MiB, ≤ 500 reference rows, ≤ 100 audit rows; reference ordering stable/deterministic, audit rows newest-first with stable tie-breaking; truncation reports included/omitted counts and safe reasons without excluded detail; no continuation cursor; upstream unavailability represented safely without raw errors or fabricated completeness; exports never retained.

**Total FRs: 24**

### Non-Functional Requirements

**NFR-1 — Security and privacy:** Every read, write, task, confirmation, audit event, and export is Tenant-, actor-, action-, target-, and current-version-scoped. Trust-bearing mutations fail closed when authorization evidence is stale, unknown, rebuilding, or unavailable. Logs, telemetry, errors, and evidence remain metadata-only.

**NFR-2 — Encryption and key management:** Production traffic uses platform-approved authenticated encryption in transit. Durable Project, task, idempotency, and audit data uses platform-managed encryption at rest. Projects owns no private keys; approved platform KMS/secret-provider rotation and revocation evidence is release-blocking.

**NFR-3 — Availability and recovery:** Authenticated metadata APIs and task admission target 99.9% monthly availability excluding planned maintenance. With required dependencies healthy, service RTO after process/node failure is 15 minutes, and accepted tasks resume or reach truthful `NeedsAttention` within 5 minutes.

**NFR-4 — Durability and idempotency:** A Project event acknowledged as committed has RPO 0 within the configured primary-region durability domain. Active Projects are never folderless. Equivalent retries return the same task; changed requests conflict. Accepted tasks are never silently dropped or duplicated.

**NFR-5 — Performance and scale:** v1 supports 10,000 Projects per Tenant, 5,000 Context References per Project excluding its Folder, and 100,000 retained audit records per Project. Metadata reads target p95 < 500 ms at 1,000 Projects / 500 references, and p95 < 1 s at supported maximum. Durable-task admission targets p95 < 500 ms under authenticated warm steady-state with required dependencies available.

**NFR-6 — Pagination and export bounds:** Cursor pages default to 50 and cap at 200. Safe Diagnostic Export obeys FR-24's per-export global size/row bounds and a per-Tenant limit of two concurrent exports.

**NFR-7 — Back-pressure and dependency control:** Per Tenant: 100 metadata reads/s (burst 200), 20 mutation admissions/s (burst 40), 1,000 nonterminal tasks, 2 concurrent Safe Diagnostic Exports. Interactive dependency timeout defaults 2 s; durable-step timeout 10 s. Idempotent calls retry at most 3 times within 30 s before truthful waiting or intervention status. Overload returns structured retry guidance.

**NFR-8 — Retention and transient data:** Active tasks remain pollable until terminal. Terminal result + scoped idempotency record available ≥ 30 days or the result's lifetime, whichever is longer. Preview/Confirmation Artifacts expire after 15 minutes. Audit metadata retained ≥ 365 days and never less than applicable retained event-history obligations. Resolution Traces and generated exports are not persisted.

**NFR-9 — Accessibility:** Chatbot candidate, confirmation, cancellation, recovery, and task journeys, plus operator read, mutation, and export journeys, conform to WCAG 2.2 AA — keyboard operable, visibly focused, announced to assistive technology, no reliance on color or timing alone, usable at 200% zoom and 320 CSS px width. Verification combines automated evidence with authenticated manual keyboard and screen-reader evidence.

**NFR-10 — Compatibility:** Contracts additive and serialization-tolerant unless a breaking change is explicitly approved. Historical v1 data and unversioned name-only creation remain readable/accepted throughout v1. Retirement requires a major version, migration notice, usage evidence, compatibility tests, and rollback evidence; event history is not rewritten.

**NFR-11 — Release evidence:** Authenticated persisted-boundary, cross-Tenant, restart/concurrency, duplicate-delivery, lost-response, accessibility, privacy, performance, deployment, smoke, rollback, and stakeholder-acceptance evidence must pass. A failed critical case or unexplained critical skip blocks release; unavailable environments remain "not verified," never "passed."

**Total NFRs: 11**

### Additional Requirements and Constraints

- **§5 Observable Context and Recovery Contract** (binding cross-FR semantics): shared logical fields `responseState`, `asOf`, `projectVersion`, `resolutionResult`, `components`, `recoveryActions`; binding consequences for `Complete`/`Partial`/`Unavailable`/`Denied`; refresh/recovery never silently rewrites an earlier response; `Partial`/`Unavailable` → `Complete` only after fresh recomputation; expired/stale confirmation → `RenewPreview`, no task; lost admission → `PollTask` or Idempotency Key retry resolving to the original task; dependency delay → `WaitingForDependency`; human-recoverable → `NeedsAttention` + `ResolveNeedsAttention`; terminal Task Status immutable.
- **Release cut rule (§2.3):** Core user value = FR-1–FR-20, FR-23; release-blocking safety/operations = FR-21, FR-22, FR-24, NFR-1–NFR-11. No approved v1 FR or NFR is deferrable from production release; only §2.4 exclusions are deferrable.
- **Role/authority model (§3.2):** four runtime roles (Project User, Tenant Operator, Tenant Project Administrator, Service/Workflow Caller); surface choice never expands authority; service callers act only with delegated actor authority.
- **Success metrics (§8):** SM-1 through SM-8 with defined measurement contract (eligible resumption, continuity success, context correction), plus counter-metrics SM-C1–SM-C4 (notably context corrections ≤ 5% of eligible resumptions).
- **Addendum constraints (downstream-binding):** platform ownership invariant (§1.1 — EventStore DomainService/platform owns hosting/persistence/read-models; no allow-all identity stubs in production hosts); Preview/Confirmation/idempotency mechanism requirements incl. U+2028/U+2029 Unicode canonicalization parity (§2, §7.3); `projects.safe-diagnostic-export.v1` contract detail (§3); Metadata Classification wire vocabulary `public_metadata`/`tenant_sensitive`/`credential_sensitive`/`secret` with `SensitiveMetadataTierValidator` reuse (§4.1); `Hexalith.Projects.UI.Contracts` non-packable descriptor host boundary (§4.2); Hexalith.Builds central ownership of NSwag.MSBuild 14.7.1 and Fluxor.Blazor.Web 6.9.0 (§4.3); migration/compatibility duties incl. legacy Active-folderless reconciliation (§5); Chatbot companion contract (§6); verification matrix and live-Playwright two-lane contract (§7); evidence/gate index E-1–E-9 (§8).
- **Readiness containment (addendum):** Epics 1–5 are immutable implementation history; the 23 Epic 6–8 placeholders recorded in E-2–E-4 are findings inventory, not schedulable stories, until outcome-based replacements pass implementation-readiness review; corrective order Epic 6 → 7 → 8; corrective development and production release frozen until an E-2-superseding rerun returns `READY`, Story 8.9 release evidence passes, and Jerome/John record a terminal dated disposition.

### PRD Completeness Assessment

The PRD is exceptionally complete and implementation-oriented: 24 FRs each carry explicit testable consequences; 11 NFRs carry quantified envelopes; a shared §5 response/recovery contract removes per-FR ambiguity; §2.3 fixes the release-cut rule; the addendum routes mechanism detail to architecture/UX/API/test artifacts with a verification matrix and evidence-gate index (E-1–E-9). Terminology is consistent (defined glossary, stable status/state vocabularies). One structural caution for later steps: the PRD deliberately freezes corrective development until a readiness rerun returns `READY` — this very assessment is that superseding rerun candidate, so epic/story coverage (steps 3–5) must be judged against the corrective Epic 6–8 rebaseline plus FR/NFR traceability rather than against Epics 1–5 history.

## Epic Coverage Validation

**Source:** `epics.md` (reconciled 2026-07-16; production authority = Epics 6–8, 33 stories: 7/15/11; Epics 1–5 retained as implementation history only).

### Coverage Matrix

Production owner = AC-bearing story in Epics 6–8 (release authority). Historical implementation (Epics 1–5) shown for trace only. Every mapping below was verified against the actual story definitions, not just the epics' own coverage map.

| FR | PRD Requirement | Production Owner (Epics 6–8) | Historical impl. | Status |
| --- | --- | --- | --- | --- |
| FR-1 | Create Project (Folder-first, idempotent Durable Task) | Story 7.1 | 1.4 (superseded) | ✓ Covered |
| FR-2 | Open Project | Story 6.1 | 1.7 | ✓ Covered |
| FR-3 | Update Project Setup | Story 7.2 | 1.8 | ✓ Covered |
| FR-4 | Archive Project | Story 7.13 | 1.8 | ✓ Covered |
| FR-5 | List Projects | Story 6.1 | 1.7 | ✓ Covered |
| FR-6 | Link Conversation | Story 7.3 | 2.3 | ✓ Covered |
| FR-7 | Move Conversation Between Projects | Story 7.4 | 2.3 | ✓ Covered |
| FR-8 | Set / Replace Project Folder | Story 7.6 (initial binding via 7.1) | 2.4 (superseded) | ✓ Covered |
| FR-9 | Link File Reference | Story 7.7 | 2.5 | ✓ Covered |
| FR-10 | Link Memory | Story 7.9 | 2.7 | ✓ Covered |
| FR-11 | Unlink Context Reference | Stories 7.5 / 7.8 / 7.10 (+ 7.6 replace-only rule) | 2.3/2.5/2.7 | ✓ Covered |
| FR-12 | Resolve Project From Conversation | Story 6.4 | 4.2 | ✓ Covered |
| FR-13 | Resolve Project From Attachments | Story 6.4 | 4.3 | ✓ Covered |
| FR-14 | Confirm Ambiguous Project | Story 7.11 (candidate reads 6.4) | 4.4 | ✓ Covered |
| FR-15 | Propose New Project | Story 7.12 | 4.5 | ✓ Covered |
| FR-16 | Get Project Context | Story 6.3 | 3.2 | ✓ Covered |
| FR-17 | Explain Context Selection | Stories 6.3 / 6.4 | 3.3 | ✓ Covered |
| FR-18 | Refresh Project Context | Story 6.3 | 3.4 | ✓ Covered |
| FR-19 | Validate Setup & Metadata Classification | Story 7.1 (contract cutover 6.7) | 1.4/1.8 | ✓ Covered |
| FR-20 | Retrieve Conversation-Start Setup | Story 6.2 | 3.5 | ✓ Covered |
| FR-21 | Record Project Audit Events | Story 8.1 | 5.1 | ✓ Covered |
| FR-22 | Support Operator Read Access | Stories 6.5 / 6.6 + 8.1 / 8.3 / 8.4 / 8.5 | 5.2 | ✓ Covered |
| FR-23 | Restore Archived Project | Story 7.14 (primary owner per SCP §4.6) | — (new) | ✓ Covered |
| FR-24 | Create Safe Diagnostic Export | Story 8.2 (primary owner per SCP §4.6) | — (new) | ✓ Covered |

NFR coverage (validated the same way):

| NFR | Primary owner | Supporting | Status |
| --- | --- | --- | --- |
| NFR-1 Security & privacy | 8.8 | all stories | ✓ Covered |
| NFR-2 Encryption & KMS | 8.11 | 8.6, 8.7 | ✓ Covered |
| NFR-3 Availability & recovery | 8.10 | 8.6 | ✓ Covered |
| NFR-4 Durability & idempotency | 8.10 | Epic 7 | ✓ Covered |
| NFR-5 Performance & scale | 8.9 | 6.7 | ✓ Covered |
| NFR-6 Pagination & export bounds | 8.9 | 8.2 | ✓ Covered |
| NFR-7 Back-pressure & dependency control | 8.9 | — | ✓ Covered |
| NFR-8 Retention & transient data | 8.1, 8.2 | Epic 7 | ✓ Covered |
| NFR-9 Accessibility (incl. Chatbot companion) | 8.8 | 6.5, 8.3 | ✓ Covered |
| NFR-10 Compatibility | 6.7 | 7.15, 8.7 | ✓ Covered |
| NFR-11 Release evidence | 8.11 | all Epic 8 | ✓ Covered |

### Missing Requirements

**None.** All 24 PRD FRs and all 11 NFRs have an AC-bearing production-owner story in Epics 6–8. No FR appears in the epics that is absent from the PRD (the epics use the identical FR-1…24 / NFR-1…11 numbering). Story counts match the declared rebaseline exactly: Epic 6 = 7 stories (6.1–6.7), Epic 7 = 15 stories (7.1–7.15), Epic 8 = 11 stories (8.1–8.11).

### Observations (non-blocking)

1. **Requirements Inventory abbreviation drift:** the epics' own FR inventory (§Requirements Inventory) is abbreviated and in two places retains pre-rebaseline phrasing — e.g. the FR-1 summary line says "Sets lifecycle `Active`" without restating the Folder-first gate, and FR-1 is tagged (UJ-2, UJ-3) vs the PRD's "Realizes UJ-2". The authoritative Story 7.1 ACs correctly enforce the final PRD contract (no folderless-Active window), so this is cosmetic inventory text, not a coverage gap.
2. **Superseded stories are correctly fenced:** Stories 1.4, 2.4, and 5.12 carry explicit "Superseded for production (2026-07-16)" banners pointing to their production owners (7.1, 7.1/7.6/7.15, 8.8/8.11) — the historical/production split is unambiguous.
3. **FR-8 initial binding** is delivered inside Story 7.1 (Folder-first create) while 7.6 owns replacement; the coverage map states this explicitly, so no orphaned sub-requirement.

### Coverage Statistics

- Total PRD FRs: 24
- FRs covered in epics (production owner in Epics 6–8): 24
- FR coverage: **100%**
- Total PRD NFRs: 11; NFRs with primary owner: 11 — **100%**

## UX Alignment Assessment

### UX Document Status

**Found:** `ux-design-specification.md` (complete, 14/14 steps, authored 2026-05-24, last touched 2026-05-29) plus visual companion `ux-design-directions.html`. Scope: administrative/operational UX (CLI + MCP + FrontComposer Web over one diagnostic model); the end-user conversation experience is explicitly delegated to Hexalith.Chatbot.

### UX ↔ PRD Alignment

**Aligned:**

- UX scope matches the PRD product boundary exactly: no standalone end-user UI outside Chatbot and generated/operational surfaces (PRD §2.4); UX targets operators/administrators/agents — the FR-22/FR-24 audience.
- "One operational model, three surfaces" with identical states, reason codes, and redaction across CLI/MCP/Web realizes the PRD rule that surface choice never expands authority (§3.2) and cross-surface parity (addendum §7.2).
- Metadata-only discipline is pervasive in the UX spec (no transcripts, file contents, prompts, secrets, memory payloads) — matches NFR-1 and the PRD's payload boundaries.
- Resolution Trace Workbench (candidates, reason codes, inclusion/exclusion evidence, outcome, no silent selection) realizes FR-12–FR-17 semantics including no-preselection (FR-14).
- Audit-first maintenance (preview → confirm → metadata-only audit evidence) matches FR-4/FR-11/FR-21/FR-23 Preview/confirmation requirements; empty-state, feedback, and fail-closed patterns match §5 `Denied`/`Unavailable` behavior.
- WCAG 2.2 AA strategy (keyboard, focus, non-color-only status, screen reader, reduced motion) matches NFR-9; responsive breakpoints include 320 px — the NFR-9 reflow floor.
- Reference/inclusion state vocabulary (`included`/`excluded`/`unauthorized`/`unavailable`/`stale`/`archived`/`ambiguous`/`tenant_mismatch`/`conflict`/`invalidReference`) is the same shared vocabulary the epics (UX-DR5/AR-18) and spine (AD-32 components, Consistency Conventions) bind.

**Drift (UX spec predates the 2026-07-15/16 rebaseline):**

1. **Confirmation Artifact model absent.** The UX spec's Maintenance Action Panel uses dry-run/preview/confirm states but never names the final PRD's 15-minute single-use Confirmation Artifact, expiry/staleness renewal (`409` + `RenewPreview`), or replay rejection. Reconciled downstream: Story 8.3 ACs bind panel phases to Preview → bound Confirmation Artifact → Durable Task truth.
2. **§5 response snapshot vocabulary absent.** `responseState` (`Complete`/`Partial`/`Unavailable`/`Denied`), Evidence Freshness States, and Recovery Action Codes postdate the UX spec. Reconciled downstream: AD-32 fixes the vocabulary; Stories 6.1–6.5, 8.3 require the snapshot fields on every surface.
3. **FR-24 export bounds absent.** The UX Safe Diagnostic Export component predates the hard contract (≤ 1 MiB, ≤ 500 reference rows, ≤ 100 audit rows, no cursor, no retention, separate permission, 2 concurrent/Tenant). Reconciled downstream: AD-21 + Story 8.2 own the bounds, including truncation-metadata display.
4. **Task Status vocabulary absent.** The PRD's 8-value Task Status set and `NeedsAttention` recovery semantics are not in the UX spec. Reconciled downstream: AD-4 + Stories 8.1/8.3.
5. **Chatbot companion journeys not designed here.** FR-14/FR-15 end-user confirmation UX (candidate comparison, no preselection, expiry recovery, task rendering — addendum §6) is out of the UX spec's declared scope. Ownership is assigned (Story 8.8: separately approved Chatbot owner + pinned revision; AD-34/SM-5 evidence), but no UX artifact for the Chatbot side exists in this repository — tracked as an external dependency, and its absence blocks Projects release per Story 8.8.

### UX ↔ Architecture Alignment

**Aligned (spine supports every load-bearing UX requirement):**

- One-model/three-surfaces → AD-2 (adapter authority), AD-16 (contracts sole authority; generated surfaces), AD-19 (one transport mapping), AD-33 (surface-invariant action matrix).
- MCP read-resources vs mutating-tools separation, no self-confirmation → AD-29 (MCP contained; consequential mutation gated).
- Metadata-only diagnostics + nonpersistent resolution traces → AD-7; bounded export → AD-21; audit channel separation → AD-26.
- FrontComposer-composed Web (no bespoke UI framework) → AD-16/AD-24 (`Projects.UI.Contracts` descriptors; FrontComposer/platform hosts own runtime composition).
- Accessibility as release invariant (keyboard, focus, non-color-only, 200% zoom, 320 px) → AD-34; performance for dense operational grids → AD-27 latency/back-pressure envelopes.
- Safe denial/empty-state distinctions (`denied` vs `absent` vs `unavailable`) → AD-19 (`404` collapse), AD-32 (`Denied` no-disclosure).

**Notes:**

1. **Runtime ownership moved after the UX spec was written.** The UX spec assumed Projects would ship its own generated console projects; AD-24 retires Projects-owned UI/MCP/CLI runtime projects in favor of platform/FrontComposer hosts. UX semantics survive unchanged (descriptors still Projects-owned via `UI.Contracts`); only implementation placement changed. Epics 6.5/6.6/8.3–8.5 already reflect the new ownership.
2. **"Dry-run" vocabulary vs Preview.** The UX spec and UX-DR17/UX-DR19 use "dry-run" as a distinct step; the spine's canonical action-admission classification defines Preview + Confirmation Artifact and no separate dry-run admission class. Story authoring for 8.3/8.4 should map "dry-run" onto server Preview (or explicitly define it as a read-only validation call) so CLI `dry-run`/`preview` and the panel states don't imply a third admission mechanism. Minor vocabulary reconciliation, not a structural conflict.

### Warnings

- ⚠️ **Stale-but-reconciled UX baseline:** the UX spec was not re-issued after the July rebaseline. Every gap found is covered by a downstream binding artifact (AD-4/7/19/21/29/32/33/34 + Stories 6.5, 8.1–8.5, 8.8), and the epics' UX-DR1–UX-DR28 inventory functions as the reconciliation layer. Recommendation: when Epic 8 story files are created, cite AD-32/AD-4/AD-21 vocabularies directly rather than the UX spec's older wording; optionally refresh the UX spec afterward. Not a readiness blocker.
- ⚠️ **External Chatbot companion UX dependency:** FR-14/FR-15/NFR-9 Chatbot-side journeys have no design artifact in this repository; Story 8.8 correctly makes their authenticated evidence release-blocking with a named external owner. This must stay visible in sprint planning as a cross-repository dependency.

## Epic Quality Review

**Standard applied:** create-epics-and-stories best practices (user value, epic independence, no forward dependencies, story sizing, AC quality), evaluated against the production authority **Epics 6–8** (33 stories). Epics 1–5 are frozen implementation history — explicitly non-schedulable, with superseded stories (1.4, 2.4, 5.12) fenced by dated supersession banners — so structural findings there are moot and were not re-litigated.

### Epic Structure Validation

| Check | Epic 6 | Epic 7 | Epic 8 |
| --- | --- | --- | --- |
| User-value framing | ✓ Operators/Chatbot get authorized, truthful reads (FR-2/5/12/13/16-18/20/22) | ✓ Users get durable, recoverable, confirmed decisions (FR-1/3/4/6-11/14/15/19/23) | ✓ Operators get safe surfaces + stakeholders get honest release evidence (FR-21/22/24, NFR-1-11) |
| Independence | ✓ Depends only on its entry gate; explicitly "never Epic 7/8" | ✓ Depends on Epic 6 reads + G-1/G-2 (prior-only) | ✓ Depends on Epics 6–7 + entry gate (prior-only) |
| Named beneficiary per story | ✓ all 7 | ✓ all 15 | ✓ all 11 |
| Traceability per story | ✓ FR/NFR/AD/UJ/finding/evidence row | ✓ | ✓ |

Stories 6.7 (read cutover), 7.15 (legacy reconciliation), and 8.7 (packaging/supply chain) are migration/operations stories rather than direct user features — **acceptable and expected for a brownfield corrective rebaseline** (each has a named beneficiary, observable outcome, and binds directly to NFR-10/NFR-4/NFR-11, which the PRD classifies as release-blocking product requirements, not optional technical work).

### Dependency Analysis

- **No forward dependencies found in Epics 6–8.** Within-epic ordering is prior-only: 6.7 consumes 6.1–6.6; 7.12 reuses the 7.1 create path; 7.15 closes Epic 7; 8.11 is terminal. Story 6.4's note that "confirmation is the durable Epic 7 step" is a scope boundary, not a dependency — 6.4 completes without Epic 7.
- **Corrective order Epic 6 → 7 → 8 is enforced** by entry gates and the containment rules; Epics 1–5 are never a dependency of 6–8 (only immutable event history is).
- **No upfront-schema violation:** event-sourced model with additive per-story event evolution (AD-22); read models are built by the story that needs them; the AD-17 inventory precedes cutover. Contracts-before-code is satisfied by AD-16 + the Epic 6 entry-gate pinned baseline.
- **Brownfield indicators present and strong:** shadow-read-first cutover with reversible routing (6.7), compensating legacy reconciliation (7.15), compatibility/rollback ACs on every applicable story, no event-history rewrite, single-writer cutover (AD-17).

### Story Quality Assessment

- **Format:** all 33 stories use role/want/benefit plus Given/When/Then ACs, and each carries the SCP §4.5 completion contract: traceability, owner roles, entry gate, deterministic fixture, exact verification command, expected evidence artifact, estimate, and a completion boundary. This exceeds the baseline standard.
- **Negative paths:** denial/cross-Tenant, stale/unavailable evidence, duplicate/replay/restart/concurrency/lost-response, and compatibility/rollback ACs are systematically present (Epic 7 shared invariants 1–6 + per-story specifics). ✓
- **Sizing:** estimates S–XL declared relative. Two XL stories (7.1 create workflow; 8.8 parity/isolation/a11y evidence) are large but bounded by explicit completion boundaries.

### Findings

#### 🔴 Critical Violations

None. No technical-milestone epics without requirement backing, no forward dependencies, no epic-sized uncompletable stories, no unfenced duplicate authority.

#### 🟠 Major Issues

1. **Evidence-row key mismatch between epics.md and the AD-30 matrix.** Stories cite row names that do not exist as keys in `implementation-readiness-traceability-matrix.yaml`: `fr-22-web`, `fr-22-cli`, `fr-22-web-ops`, `nfr-10-reads`, `fr-11-conversation`, `fr-11-file`, `fr-11-memory`, `nfr-4-reconcile`, `nfr-3-resilience`, `e2e-live`, `mcp-contract`, `cli-contract`, `supply-chain`, `release-acceptance`. The matrix consolidates to canonical keys (`fr-22`, `nfr-10`, `fr-11`, `nfr-3`, `nfr-4`, `release-smoke`, `release-stakeholder-acceptance`, …) with `primary_story`/`supporting_stories` fields. Traceability itself is complete in both directions (all 63 required rows exist: 24 FR + 11 NFR + 16 findings + 12 release categories), but the epics' claim of "identically keyed" rows is inaccurate for these ~14 references. **Remediation:** at 6.x/7.x/8.x story-file creation, normalize story evidence-row references to the canonical matrix keys (or deliberately expand the matrix with per-surface sub-rows) so `hexalith-evidence validate` has one unambiguous key set. Low effort; should be done before or during story-file creation, not after.
2. **External entry gates dominate the critical path.** 37 of 63 matrix rows are `blocked-external` on G-1…G-6. G-1 (platform Durable Task engine + Confirmation Artifact record) is explicitly *absent from published/clean EventStore 3.67.3 API evidence*, which blocks every Epic 7 story; G-3 requires a FrontComposer 4.0.0-vs-4.0.1 disposition; G-6 requires a Dapr runtime↔SDK tuple decision. This is honestly declared (gates are "prerequisites, not delivered value") and is not a structural violation — but it means a `READY` verdict authorizes story-file creation and sprint planning, **not** immediate Epic 7 implementation. The gates need named platform-side owners and dates in sprint planning.
3. **All verification commands depend on not-yet-existing G-4 tooling.** Every story's exact command (`dotnet tool run hexalith-module test …`) and the AD-30 gate (`hexalith-evidence validate`, `tool_status: not-available`) are target contracts, clearly labeled as such. Risk: if G-4 slips or the delivered tool diverges, all 33 stories' verification commands and evidence paths must be re-authored. Mitigation is already partially in place (matrix records the blocker honestly); keep G-4 first in sprint sequencing.

#### 🟡 Minor Concerns

4. **Story 8.8 bundles four evidence concerns** (cross-surface parity, cross-Tenant isolation/leakage, operator accessibility, cross-repository Chatbot companion evidence) into one XL story with an external dependency (separately approved Chatbot owner + pinned revision). Consider splitting the Chatbot companion evidence into its own story at story-file creation so the external dependency cannot block the repository-local evidence lanes.
5. **BDD formatting deviations:** Story 6.7 AC1 chains two `When` clauses in one criterion; Story 8.1's traceability line contains a dangling empty findings reference ("findings (audit) ;"). Cosmetic.
6. **Requirements Inventory text drift** (from step 3): the epics' abbreviated FR-1 inventory line retains pre-rebaseline phrasing ("Sets lifecycle Active") that the authoritative Story 7.1 supersedes. Cosmetic; correct at next epics touch.
7. **"Dry-run" vs Preview vocabulary** (from step 4): UX-DR17/UX-DR19 and Stories 8.3/8.4 use "dry-run" language while the spine's action-admission classification defines only Preview + Confirmation Artifact. Map "dry-run" onto server Preview (or define it as a read-only validation call) during story-file authoring.
8. **Three matrix rows carry "Confirm at de-placeholdering" notes** (UI-contracts boundary routing to 6.7/8.7; two 8.7 mappings) — resolve those confirmations when the placeholder reconciliation happens.

### Best Practices Compliance Checklist (Epics 6–8)

- [x] Epics deliver user/requirement value (no unbacked technical milestones)
- [x] Epic independence maintained (6 → 7 → 8, prior-only)
- [x] Stories appropriately sized with declared relative estimates and completion boundaries
- [x] No forward dependencies (within or across epics)
- [x] Schema/read models created when needed (event-sourced, additive, AD-22)
- [x] Clear, testable, negative-path-inclusive acceptance criteria
- [x] Traceability to FRs/NFRs/ADs/findings maintained (with the key-naming caveat in Major Issue 1)

## Summary and Recommendations

### Overall Readiness Status

# ✅ READY

The planning layer for the corrective Epics 6–8 rebaseline is complete, internally consistent, and fully traceable. This assessment is the independent rerun contemplated by E-2/the containment rules and **supersedes the 2026-07-16 `NOT_READY` verdict**.

**What READY authorizes (per the plan's own containment):** creation of the 6.x/7.x/8.x story files, sprint-status reconciliation, and entry into corrective implementation *story by story as each entry gate is satisfied*.

**What READY does not change:** production release, consequential autonomous MCP mutation, and proposed-Project confirmation remain blocked until Story 8.11 passes with dated Jerome + John terminal acceptance; no story may start before its G-1…G-6 entry-gate capability is pinned and approved; no sibling repository may be changed without separate repository-local authorization.

### Evidence Base for the Verdict

| Dimension | Result |
| --- | --- |
| Documents | All present and final: PRD (24 FR / 11 NFR, updated 2026-07-15) + addendum, ARCHITECTURE-SPINE (AD-1…34, final 2026-07-16), epics (33-story rebaseline, reconciled 2026-07-16), UX spec (complete, reconciled via epics UX-DR1–28) |
| FR coverage | 24/24 FRs have an AC-bearing production owner in Epics 6–8 (verified against story definitions, not just the coverage map) |
| NFR coverage | 11/11 NFRs have primary + supporting owners |
| Findings & release categories | 16/16 audit findings (9 P1 + 7 P2) and 12/12 critical release categories present as matrix rows |
| AD-30 matrix | 63/63 required rows, canonical YAML, honest fail-closed statuses (37 blocked-external, 24 pending, 1 failed, 1 blocked; zero false `passed`) |
| Epic structure | No critical violations: user-value framing, strict prior-only dependencies, superseded history fenced, brownfield migration/rollback discipline throughout |
| Supersession chain | Clean: architecture.md → SPINE; 23 placeholders → 33 stories (atomic); Story 8.9 gate → Story 8.11 (explicit); Stories 1.4/2.4/5.12 → 7.1/7.6/7.15/8.8/8.11 (banners) |

### Critical Issues Requiring Immediate Action

None block the READY verdict. Three conditions attach to it:

1. **Normalize evidence-row key references (Major Issue 1)** — before or during 6.x/7.x/8.x story-file creation, replace the ~14 non-canonical row names in epics.md story annotations (`fr-22-web`, `e2e-live`, `release-acceptance`, …) with the canonical matrix keys, or deliberately add per-surface sub-rows to the matrix. One key vocabulary must exist before `hexalith-evidence validate` becomes runnable.
2. **G-1…G-6 gates need named platform-side owners and target dates at sprint planning (Major Issue 2)** — G-1 (Durable Task engine, blocks all of Epic 7), G-3 (FrontComposer 4.0.0/4.0.1 disposition), G-4 (composition runner + evidence tool, blocks all verification commands), G-6 (Dapr runtime↔SDK tuple). 37 of 63 evidence rows wait on these. Sequence G-4 first.
3. **Execute the conformance checklist or supersede it** — `epics-architecture-conformance-checklist-2026-07-16.md` is an unfilled template (all verdict checkboxes unticked). Either record its per-story verdicts during story-file creation, or record that this report's independent verification supersedes it. Don't leave an ambiguous half-artifact in the planning set.

### Recommended Next Steps

1. **Record this verdict:** update the AD-30 matrix `containment.overall_readiness` to `READY (2026-07-17 independent rerun; supersedes 2026-07-16 NOT_READY)` and have Jerome/John note the disposition per the E-2 supersession rule.
2. **Run sprint planning** (`bmad-sprint-planning`) to reconcile `sprint-status.yaml` with the 33-story inventory, sequencing G-4 → G-3/G-5 → Epic 6 entry gate → Epic 6 stories, with G-1/G-2 procurement running in parallel for Epic 7.
3. **Create story files in corrective order** (`bmad-create-story`), starting with Story 6.1, applying conditions 1 and 3 above, resolving the "dry-run vs Preview" vocabulary (map dry-run onto server Preview), and considering the Story 8.8 split (repository-local evidence vs Chatbot companion evidence).
4. **Register the cross-repository Chatbot companion dependency** (Story 8.8 / AD-34 / SM-5) with a named Chatbot owner and pinned revision now — it is release-blocking and outside this repository's control.
5. **Optional hygiene:** refresh the UX spec's Confirmation Artifact / response-snapshot / export-bounds vocabulary (or annotate it as reconciled-via-epics), and fix the cosmetic epics inventory drift (FR-1 line, Story 6.7 double-When, Story 8.1 dangling findings reference) at the next epics touch.

### Final Note

This assessment identified **8 issues across 3 categories** (0 critical, 3 major, 5 minor) — none structural. The corrective planning layer is exceptionally rigorous: every requirement, finding, and release category is triple-keyed to an AC-bearing story, an architecture decision, and a fail-closed evidence row, and the containment rules prevented every classic failure mode (placeholder scheduling, false `passed` evidence, silent scope expansion). Address the three attached conditions in the course of sprint planning and story-file creation; they do not require another readiness rerun.

---

**Assessment date:** 2026-07-17
**Assessor:** Independent implementation-readiness rerun (bmad-check-implementation-readiness, executed by Claude for Jerome)
**Verdict:** `READY` — supersedes `implementation-readiness-report-2026-07-16.md` (`NOT_READY`)
**Release containment unchanged:** Story 8.11 + dated Jerome & John terminal acceptance still gate production.
