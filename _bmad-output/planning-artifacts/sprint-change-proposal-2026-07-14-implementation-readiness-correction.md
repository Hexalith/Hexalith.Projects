---
title: "Sprint Change Proposal: Implementation Readiness Correction"
status: approved
created: 2026-07-14
approved: 2026-07-14
approved_by: Jerome
project: Hexalith.Projects
prepared_for: Jerome
scope: major
mode: incremental
trigger_report: _bmad-output/planning-artifacts/implementation-readiness-report-2026-07-14.md
---

# Sprint Change Proposal: Implementation Readiness Correction

## 1. Issue Summary

### Trigger

Story 5.12 supplied executable live-topology evidence after Epics 1–4 and most of Epic 5 had
already been implemented. The subsequent implementation-readiness assessment found that the
planning set was not ready despite complete functional-requirement traceability. The Epic 5
release handoff independently remained blocked, and the production-readiness audit identified
nine P1 blockers.

This is not a missing-vision problem. It is a planning, architecture, sequencing, and release-
evidence problem discovered after substantial implementation had already occurred.

### Problem Statement

The current planning baseline no longer describes an executable, platform-compliant, releasable
system. It contains all of the following conflicts:

- Project creation can produce an Active Project without the PRD-mandated Folder.
- Completed stories depended on later or unverified external capabilities.
- Architecture still assigns hosting, persistence, projections, Dapr plumbing, topology, health,
  telemetry, and UI runtime to Projects even though current platform rules assign them to
  EventStore DomainService, the platform AppHost, and FrontComposer.
- Epic 5 introduced restore, mutating re-evaluation, historical resolution-case assumptions, and
  export/dashboard behavior without complete PRD and architecture contracts.
- UI and CLI lack a production caller-identity path; authorization composition contains
  development allow implementations.
- Proposal confirmation and other cross-context operations are not durable across restart,
  multiple instances, lost responses, or partial failure.
- Consequential confirmation is caller-authored rather than server-issued and bound to the
  reviewed action and state.
- Real persisted-boundary, authenticated E2E, restart/concurrency, and production deployment
  evidence is not green.

### Evidence

- Functional coverage: 22 of 22 PRD requirements mapped.
- Readiness findings: 4 critical violations, 11 major issues, 5 minor concerns, and 7 UX gaps.
- Story 5.12 live evidence: 19 passed and 56 failed product cases.
- Production-readiness audit: 0 verified P0 findings, 9 P1 blockers, and 7 P2 findings.
- Release handoff: production deployment unverified and stakeholder acceptance not granted.
- Sprint tracking: Stories and retrospectives for Epics 1–4 are done while each epic remains
  marked in-progress; Story 5.12 is done in its specification but review in sprint tracking.

### Issue Classification

Primary classification: failed or outdated implementation approach requiring replanning.

Secondary classification: planning-artifact drift and previously undefined product behavior.

## 2. Impact Analysis

### Epic Impact

#### Epics 1–4

The completed domain and feature implementation is preserved as historical evidence. Completed
story files are not reopened or rewritten. The epics are marked done, with a note that obsolete
architecture and release-readiness assumptions are superseded by this proposal.

#### Epic 5

Epic 5 cannot close under its original product and release claims. It is reconciled as an
implementation baseline:

- Story 5.12 is done as verification-enablement work; its failures become corrective backlog.
- Restore is formalized as a requirement but remains production-disabled until corrected.
- Re-evaluation becomes read-only Refresh diagnostics.
- Resolution traces remain transient; historical resolution-case browsing is removed from MVP.
- The approved Story 5.13 contract-boundary decision moves into the Epic 6 target-architecture
  sequence.
- Once these scope corrections are recorded, Epic 5 is done as implementation history, not as
  production-release authorization.

#### New corrective epics

- Epic 6: Supported Platform Boundary and Secure Identity.
- Epic 7: Durable Cross-Context and Agent-Safe Workflows.
- Epic 8: Production Conformance and Release Evidence.

Required order: Epic 5 scope reconciliation, Epic 6, Epic 7, Epic 8, then release handoff.

### Story Impact

Completed story records remain unchanged. Planning descriptions for Stories 5.6, 5.9, 5.10,
and 5.12 receive supersession or clarification notes. Story 5.13 is removed from Epic 5 backlog
and its approved intent is absorbed by Story 6.2.

Twenty-one corrective stories are added across Epics 6–8. No story may pass through a later
story's surface or an unverified upstream dependency.

### Artifact Conflicts

| Artifact | Conflict | Required correction |
| --- | --- | --- |
| PRD | Folder lifecycle, restore, re-evaluate, trace, export, Chatbot UX, and NFR gaps | Revise requirements and accepted decisions |
| Architecture | Obsolete hybrid scaffold and ownership model | Replace with DomainService/platform target and staged migration |
| UX | Historical traces, mutating reevaluate, caller confirmation, hosting drift, vague budgets | Reclassify interactions and define bounded contracts |
| Epics | Historical status drift, undefined Epic 5 scope, no corrective implementation vehicle | Close historical epics and add Epics 6–8 |
| Sprint status | Incorrect epic/story statuses and colliding Story 5.12/5.13 proposals | Reconcile statuses and route existing work |
| OpenAPI/contracts | Runtime validation, pagination, error, identity, confirmation, and task drift | Correct under Stories 6.2 and 7.2 |
| Event/projection catalogs | Pending-folder, restore, audit receipt, and SDK projection ownership drift | Update through owning stories |
| CI and tests | Fake or omitted persisted-boundary and critical E2E gates | Make real lanes blocking in Epic 8 |
| Release handoff | No deployment or stakeholder acceptance evidence | Keep blocked through Story 8.9 |

### Technical Impact

The correction requires a staged architecture migration, public-contract compatibility handling,
real authentication, durable cross-context workflows, server-bound confirmation artifacts,
legacy pending-Folder reconciliation, platform-hosted Web/MCP/CLI composition, and real release
evidence. Event history is preserved. Rollback changes routing and adapters, never committed events.

### MVP Impact

All existing FR-1 through FR-22 outcomes remain. FR-23 formalizes restoration of an Archived
Project. Safe export is defined as a bounded consequence of FR-22. No generic project-management,
payload storage, persisted inference history, or standalone end-user Projects UI is added.

## 3. Recommended Approach

### Selected Path

Hybrid correction: clarify the MVP and revise all planning artifacts, preserve valid domain work,
migrate onto supported platform seams, replace unsafe machine workflows, and prove release
readiness through executable gates.

### Alternatives Considered

#### Direct adjustment only

Not viable alone. Narrow fixes cannot correct platform ownership, authentication, durability,
confirmation, and live evidence.

#### Rollback

Not recommended. Aggregate Handle/Apply behavior, validation, deterministic resolution/context
policy, metadata-only modeling, generated-client reproducibility, and broad focused tests remain
valuable. Staged migration is safer than discarding them.

#### MVP reduction only

Insufficient. Product clarification resolves ambiguous Epic 5 scope but does not repair runtime,
security, durability, or release evidence.

### Effort and Schedule Impact

Classification: Major.

Effort: High. Risk: High but controlled through staged cutover and rollback routing.

Indicative critical path for one senior cross-functional team:

- Planning and architecture correction: approximately 1 week.
- Epic 6: approximately 5–8 weeks.
- Epic 7: approximately 4–6 weeks.
- Epic 8: approximately 3–5 weeks, excluding remediation discovered by live gates.

Initial range: 13–20 engineering weeks. This is not a release commitment. Re-estimate after
Story 6.1 establishes exact platform seams, external versions, consumers, and migration inventory.

## 4. Detailed Change Proposals

### 4.1 PRD Changes

#### Project creation and mandatory Folder

OLD:

> Creating a Project records durable Project metadata and sets Project Lifecycle State to Active.
> Creating a Project can create or attach a Project Folder with the same name as the Project when
> no folder is supplied.

NEW:

> Create Project is admitted as a durable, pollable creation task. A Project is recorded and
> becomes caller-visible with lifecycle Active only after exactly one authorized Project Folder
> has been confirmed. A supplied Folder is verified and bound; otherwise the task requests a
> same-name Folder through Hexalith.Folders. Dependency unavailability leaves the task pending,
> blocked, or failed and never creates an Active folderless Project. Equivalent retries return the
> same task; same-key different-request retries conflict. Partial outcomes reconcile or compensate
> without rewriting event history.

This preserves Project name as the only required user input while removing the degraded Active
plus ProjectFolderCreationPending contract.

#### FR-23: Restore Archived Project

Add:

> Authorized operators or Chatbot workflows can restore an Archived Project. Restoration changes
> Archived to Active only after exactly one authorized Project Folder and current tenant/resource
> authorization evidence are verified. Missing, stale, unavailable, or unauthorized evidence
> fails closed. Restoration is idempotent, explicitly confirmed, and recorded as metadata-only
> audit evidence. The restored Project is not treated as active context until projection-confirmed.

Project lifecycle remains Active or Archived; pre-creation task state is not Project lifecycle.

#### Re-evaluation and trace semantics

Add:

> Re-evaluation is read-only recomputation using current authorized inputs. It does not mutate
> Project state, persist candidate scores, or create a maintenance audit event. Resolution traces
> are transient request results. MVP has no persisted resolution-case store or historical case-ID
> browsing. Only an explicitly confirmed choice is persisted.

#### Safe Diagnostic Export

Add to FR-22:

> Authorized operators can export an already-authorized diagnostic view using schema
> projects.safe-diagnostic-export.v1. Output has deterministic field ordering, ISO-8601 timestamps,
> explicit truncation metadata, a maximum encoded size of 1 MiB, at most 500 reference rows, and
> at most 100 audit rows. Projects does not retain the export after response completion. Setup
> text, transcripts, file or memory contents, prompts, secrets, tokens, unrestricted paths, raw
> upstream problems, and unconfirmed candidate details are excluded.

#### NFR additions

- Cursor pagination defaults to 50 and caps at 200 items.
- Committed Project events have RPO 0.
- Durable tasks recover after restart and converge across multiple instances.
- Trust-bearing mutations deny stale, unknown, rebuilding, or unavailable authorization evidence.
- Dependency timeouts are bounded and configurable; retries are limited to idempotent operations.
- Data uses platform-managed encryption in transit and at rest.
- Audit retention is explicit deployment policy and cannot silently undercut retained event history.
- Web operational surfaces conform to WCAG 2.2 AA.
- Performance evidence covers declared small, median, and maximum supported cardinalities.

#### Accepted decisions

- Mandatory Folder is satisfied before Active.
- Pre-creation state belongs to a durable task.
- Restore is included through FR-23.
- Re-evaluation is read-only.
- Resolution traces remain transient.
- Safe export is a bounded FR-22 read capability.
- Chatbot owns accessible candidate/proposal confirmation, cancellation, and recovery UX through a
  companion integration contract.

### 4.2 Architecture Changes

#### Status and ownership

OLD:

> READY WITH MINOR GAPS. Selected starter: Hybrid Hexalith module scaffold. Projects owns Server,
> Workers, Infrastructure, ServiceDefaults, Aspire, and AppHost.

NEW:

> TARGET ARCHITECTURE APPROVED — IMPLEMENTATION MIGRATION REQUIRED. Hexalith.Projects is a domain
> module built on Hexalith.EventStore.DomainService. Production release remains blocked until
> migration, compatibility, security, durability, and verification gates complete.

Target ownership:

| Concern | Owner |
| --- | --- |
| Commands, events, aggregate, validators, resolution/context policy | Hexalith.Projects |
| Domain query and projection handlers | Projects using EventStore SDK seams |
| Hosting, persistence, publication, subscriptions, cursors, health, telemetry | EventStore DomainService |
| Dapr components and distributed topology | Platform AppHost |
| Web/MCP/CLI runtime composition | FrontComposer/platform host |
| Stable domain and wire contracts | Hexalith.Projects.Contracts |
| Presentation descriptors | Separate presentation adapter |
| Durable workflow/task/confirmation infrastructure | Shared EventStore/platform capability |
| Project-specific workflow transitions | Hexalith.Projects |

#### Dependency direction

Contracts contains identifiers, commands, events, rejection events, query contracts, and stable
DTOs only. It has no Fluent, Fluxor, FrontComposer Shell, ASP.NET hosting, MCP, CLI, or UI dependency.

Platform host and FrontComposer adapters depend on a Projects presentation adapter, which depends
on application/domain handlers and Contracts. No dependency points inward from Contracts to a
presentation or host package.

#### EventStore and read models

Replace custom journals, stores, worker subscriptions, and cursors with
IAsyncDomainProjectionHandler, IReadModelStore or IReadModelBatchStore, IDomainQueryHandler, and
platform cursor/scope abstractions. Use supported DomainService endpoint mapping and host
composition. Replay existing events and compare deterministic old/new outputs before cutover.

#### Durable workflows

CreateProject becomes a durable task that binds tenant, actor, normalized request hash,
idempotency key, and task identity; verifies or creates a Folder; waits for authorized evidence;
then submits Project creation and reconciles partial outcomes. The same platform capability owns
proposal confirmation and cross-context Conversation/Folder/File operations.

Conversation assignment/move emits an idempotent, metadata-only Projects audit receipt after the
Conversations-owned mutation is confirmed; ownership does not move to Projects.

#### Security and confirmation

- Production requires complete JWT and service-identity configuration.
- Development allow stubs cannot resolve outside explicit development/test hosts.
- Tenant and actor authority are server-derived.
- UI and CLI use platform credential providers.
- Consequential operations use expiring, single-use server confirmation artifacts bound to tenant,
  actor, action, targets, request hash, state/version, and preview.
- A state change after preview invalidates confirmation.

#### Identity and API

New Project, message, correlation, task, causation, and confirmation IDs use platform ULID
generation. Legacy identifiers remain readable during a documented compatibility window. Lists are
cursor-bounded. Domain command/query contracts are canonical; OpenAPI is generated from or
mechanically validated against them. Existing generated consumers receive a versioned adapter.

#### Migration and rollback

1. Inventory routes, public APIs, state keys, events, cursors, consumers, and pending records.
2. Validate or add generic platform workflow and confirmation seams.
3. Introduce clean Contracts and presentation boundaries with compatibility shims.
4. Replay and compare SDK read models.
5. Reconcile folderless or pending Projects through compensating workflows.
6. Cut reads to SDK models.
7. Cut commands and workflows to DomainService.
8. Migrate authenticated Web/MCP/CLI composition.
9. Remove Projects-owned runtime/topology plumbing.
10. Retain routing rollback until all release gates pass.

Rollback never rewrites event history and does not dual-write commands without proven deduplication.

### 4.3 UX Changes

#### Surface boundary

Projects supplies domain descriptors and safe view models to the platform-hosted FrontComposer
console. Hosting, authentication, shell composition, MCP registration, CLI composition, theme,
telemetry, and topology remain platform responsibilities.

#### Resolution trace

Replace historical resolution-case navigation with transient recomputation from current authorized
Conversation, Folder, File, Project, or correlation inputs. Closing the view discards the trace.

#### Action classification

Read-only diagnostics: list, describe, inspect, trace, validate, audit, diagnostic export, Refresh
diagnostics, and a temporary reevaluate compatibility alias.

Consequential mutations: archive, restore, relink, unlink, confirm resolution, and confirm proposed
Project creation.

Reevaluate is removed from mutating MCP and maintenance lifecycles. Restore is visible only for an
Archived Project and remains disabled until current authorization, exactly one authorized Folder,
server preview, and confirmation are available.

#### Maintenance interaction

Clients request a server preview and display the returned safe consequences, blockers,
state/version, expiry, and confirmation artifact. Clients do not recompute preview truth or submit
a Boolean/free-form confirmation. Task states are Pending, Running, WaitingForDependency,
Succeeded, Rejected, Failed, or NeedsAttention. Acknowledgement is not completion.

#### Export, dashboard, and Chatbot UX

Safe export follows the PRD schema and bounds. Dashboard metrics are explicitly defined for Active
and Archived Projects, Folder-workflow tasks, reference-health states, failed tasks, projection
freshness, and diagnostic-unavailable count. Unknown data renders Unavailable, never zero.

The Chatbot companion contract covers safe candidate comparison, explicit confirmation and
cancellation, no preselected ambiguous candidate, accessibility, stale confirmation recovery,
lost-response retry, task status, and safe denial.

#### FrontComposer and verification governance

Use the current Contracts/Contracts.UI separation, platform-hosted composition, Fluent UI Blazor V5,
Fluent 2 tokens, approved layout/accordion patterns, stable accessible names, and stable test IDs.
Baseline accessibility, tenant-isolation, and leakage criteria apply per story. Production acceptance
cannot pass with failed critical cases or unowned skips.

### 4.4 Epic and Story Changes

#### Historical closure

Epics 1–4 are marked done and retain their completed story records. Add supersession notes for
obsolete architecture and release assumptions.

Epic 5 is reconciled as an implementation baseline. Story 5.12 is done as harness work, its live
failures move to Epic 8, and Story 5.13 intent moves to Story 6.2. Epic 5 then becomes done while
release remains blocked.

#### Epic 6: Supported Platform Boundary and Secure Identity

1. 6.1 Pin platform capabilities and migration baseline.
2. 6.2 Restore Contracts, presentation, identity, and API boundaries.
3. 6.3 Enforce secure platform admission and authorization evidence.
4. 6.4 Migrate read models and queries to DomainService.
5. 6.5 Migrate command hosting and platform topology.
6. 6.6 Authenticate FrontComposer UI and CLI consumers.
7. 6.7 Execute compatibility cutover and retire legacy runtime.

Completion closes ARCH-001, ARCH-002, SEC-001, CLIENT-001, ID-001, and API-001.

#### Epic 7: Durable Cross-Context and Agent-Safe Workflows

1. 7.1 Provide shared durable workflow, task, and confirmation seams.
2. 7.2 Bind server-issued previews and confirmations.
3. 7.3 Enforce the mandatory Folder through durable Project creation.
4. 7.4 Make Conversation assignment and moves durable and auditable.
5. 7.5 Make proposed-Project confirmation durable and recoverable.
6. 7.6 Migrate archive, restore, relink, and unlink to bound tasks.
7. 7.7 Reconcile legacy pending-Folder and in-flight records.

Completion closes REL-001 and AGENT-001 and provides restart-safe evidence for FR-1, FR-6 through
FR-8, FR-14, FR-15, FR-21, and FR-23.

#### Epic 8: Production Conformance and Release Evidence

1. 8.1 Establish real persisted-boundary fixtures and CI.
2. 8.2 Implement truthful health, telemetry, and generated logging.
3. 8.3 Conform MCP and CLI machine contracts.
4. 8.4 Rebuild operator UI conformance.
5. 8.5 Align build, packaging, supply chain, and source structure.
6. 8.6 Activate authenticated critical E2E and tenant-isolation gates.
7. 8.7 Prove restart, retry, concurrency, and reconciliation.
8. 8.8 Bound read models and verify performance objectives.
9. 8.9 Record deployment and stakeholder acceptance.

Completion closes TEST-001 and all required P2 compliance/correctness findings or records an
authorized disposition. Release handoff can proceed only after Story 8.9.

### 4.5 Tracking and Release Changes

Set Epics 1–5 and Story 5.12 to done. Remove Story 5.13 from Epic 5 backlog. Add all Epic 6–8
entries as backlog with optional retrospectives.

Route existing July 14 items as follows:

| Existing item | Corrective owner |
| --- | --- |
| CreateProject metadataClass enforcement | Story 6.2 |
| U+2028/U+2029 idempotency parity | Story 6.2 |
| FrontComposer descriptor boundary | Story 6.2 |
| Freshness vocabulary drift | Stories 6.2 and 8.3 |
| Warning/dashboard partial-failure parity | Stories 8.3 and 8.4 |
| Story 5.12 tenant/UI/static failures | Story 8.6 |
| Production deployment and acceptance | Story 8.9 |

Preserve completed story files, retrospectives, the readiness report, Story 5.12 evidence, the
production audit, and the current blocked handoff. Add routing/supersession notes rather than
rewriting history.

Append to the release handoff:

> No production release, consequential autonomous MCP operation, or proposal-confirmation
> enablement is authorized before Story 8.9. Failed or skipped critical verification cannot be
> accepted as release evidence.

## 5. Implementation Handoff

### Scope Classification

Major: fundamental product-artifact correction, target-architecture migration, public-contract and
runtime changes, new corrective epics, and a new release-evidence program.

### Recipients and Responsibilities

#### Product Manager

- Update and validate the PRD, including FR-23 and NFR bounds.
- Confirm the MVP and Chatbot-owned interaction contract.

#### Solution Architect

- Produce the revised target architecture.
- Validate platform seams and upstream ownership.
- Define migration, compatibility, cutover, rollback, and legacy-data reconciliation.

#### Product Owner

- Materialize Epics 6–8 and their dependency/entry gates.
- Reconcile sprint tracking and route existing approved proposals.

#### Developer

- Create story files only after PRD, architecture, and epic changes are approved.
- Implement in sequence without weakening gates or rewriting event history.

#### Test Architect

- Produce a system-level test strategy for Epics 6–8.
- Own persisted-boundary, security, resilience, accessibility, performance, and release gates.

#### Release Owners: Jerome and John

- Preserve the blocked release decision.
- Record deployment evidence, residual-risk disposition, and final dated acceptance only after
  Story 8.9 passes.

### Immediate Containment

Until the corrective epics complete:

- Do not authorize production release.
- Do not enable consequential autonomous MCP tools or proposal confirmation.
- Do not treat Story 5.12 failures as passing evidence.
- Do not implement more Epic 5 scope.
- Do not modify sibling platform modules without their own approved stories and repository-local
  validation.

### Success Criteria

- Projects runs through supported DomainService/platform ownership.
- Contracts has no presentation/hosting dependencies.
- No production authorization stub exists.
- Web, CLI, and MCP use real caller identity.
- Project creation never exposes an Active folderless Project.
- Cross-context workflows and confirmations are durable and restart-safe.
- Real persisted-boundary, cross-tenant, authenticated E2E, restart/concurrency, accessibility,
  privacy, and performance gates pass.
- Deployment version, environment, health/smoke result, rollback reference, and residual risks are
  recorded.
- Jerome and John provide explicit dated acceptance.

## 6. Change Navigation Checklist Status

- [x] Trigger and evidence understood.
- [x] Epic impact assessed.
- [x] PRD, architecture, UX, and secondary artifact conflicts assessed.
- [x] Direct adjustment, rollback, and MVP review alternatives evaluated.
- [x] Hybrid path selected.
- [x] Detailed artifact edits reviewed incrementally.
- [x] Major-scope handoff plan defined.
- [x] Complete proposal reviewed.
- [x] Explicit implementation approval granted by Jerome on 2026-07-14.
- [x] Sprint-status reconciliation applied.
- [x] Major-scope PM/Architect handoff recorded.

## 7. Approval and Routing

Jerome approved this Sprint Change Proposal for implementation on 2026-07-14.

Change scope: Major.

Primary route: Product Manager and Solution Architect.

Secondary route: Product Owner, Developer, Test Architect, and release owners Jerome and John.

The approval authorizes planning-artifact correction and the sequenced handoff. It does not
authorize production release, enable consequential autonomous operations, or bypass the entry,
verification, and acceptance gates defined by Epics 6–8.
