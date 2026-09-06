---
title: 'Retrieve assembled Project Context through supported read models'
type: 'feature'
created: '2026-08-24'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 3
baseline_commit: '5a37f9e4ba9cd7f35afae212398db9f945d4d475'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-state-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The Story 3.x context routes assemble from a Projects-owned Dapr journal, truncate Conversation evidence to one page, and refresh through incomplete live fan-out. They cannot prove the supported AD-32 Project, Folder, Setup, authorization, version, component, recovery, and Reference Trust Index truth required for Chatbot grounding.

**Approach:** Add one cohesive, read-only DomainService slice for context retrieval, refresh, and explanation over EventStore-managed persisted Project and Reference Trust Index models. Reuse the supported query and projection pattern already used by Conversation-start, preserve the pure allowlist policy and legacy routes for shadow comparison, and expose no sibling payload or durable diagnostic trace.

**Decision:** Implement on the current local EventStore and `/query` seams now, following Story 6.2. Focused tests may pass; G-4 and Story 6.1-chain evidence stay non-qualifying until those gates exist.

**Decision:** When other required evidence is current and Setup is null, treat Setup as current empty values (Story 6.2 current-empty). `Complete` and `Partial` remain allowed.

**Decision:** Extract shared AD-32 snapshot types from the Story 6.2 Conversation-start records, one type per file, and reuse them for context. Keep the existing Conversation-start wire shape.

**Decision:** Keep Get, Refresh, Explain, and Reference Trust Index in this story. Do not implement Refresh owner batches or RTI ingestion until approved G-2 and RTI artifacts exist. Until then, Get and Explain run on Project-owned persisted detail and the allowlist; missing owner-backed trust outcomes are explicit optional `Partial` omissions, not invented inclusions and not required-evidence `Unavailable`. After RTI is accepted, missing or non-current trust-index evidence is required `Unavailable`.

## Boundaries & Constraints

**Always:** Derive Tenant, original actor, authenticated workload, delegation, scopes, and audience from the immutable `QueryEnvelope`; resolve expected action, target, and version server-side and require exact matches. Use named `IAsyncDomainProjectionHandler` projections, `IReadModelStore`/`IReadModelBatchStore`, and `ReadModelWritePolicy`. Require an Active Project with exactly one authorized Folder. Include a reference only after Tenant, Project, lifecycle, authorization, and freshness checks pass; make every omission explicit. Use one AD-32 snapshot vocabulary (`responseState`, `asOf`, authorized `projectVersion` when disclosable, metadata-only `components`, closed recovery actions). Preserve deterministic ordinal ordering. Keep Get and Explain zero-write with zero sibling owner calls. Keep Refresh bounded, read-only, and zero-write. Keep explanation current, request-scoped, and nonpersistent. Reauthorize after persisted or owner reads and before returning `Complete`, `Partial`, or `Unavailable`.

**Never:** Write or revert `sprint-status.yaml`. Trust payloads or custom headers for authority. Add direct Dapr state access, another journal or query runtime, a second AD-32 vocabulary, an unbounded per-reference fan-out, a parallel trust store, or an invented per-actor authorization fingerprint. Persist explanation or selection traces. Mutate Projects or siblings during refresh. Expose Tenant or actor authority, claims, tokens, prompts, transcripts, paths, file or memory content, secrets, raw owner errors, or unconfirmed-candidate detail. Hand-edit generated files. Switch or retire legacy routing before Story 6.7. Invent unapproved Conversations, Folders, or Memories G-2 batch-read contracts, an unapproved Reference Trust Index schema, or a substitute G-4 module manifest. Relabel `ProjectReferenceIndex` as the Reference Trust Index. Treat Story 6.1 list/open contracts as landed in this checkout.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Complete context | Authorized Active Project; current Project, exactly one Folder, current-empty or current Setup, authorization, and all selected reference evidence (including accepted trust-index currentness when RTI exists) | Metadata-only setup and ordered included/excluded references with AD-32 `Complete`, `asOf`, authorized version, components, and `None` recovery | No error expected |
| Partial context | Required evidence current; optional reference denied, stale, rebuilding, unavailable, excluded, or missing owner-backed trust while RTI is unapproved | Usable `Partial`; every omission has closed state/reason/last-verified evidence and applicable recovery | No raw owner detail; never silently drop a candidate |
| Required evidence non-current | Project, Folder, authorization missing/non-current; or Setup non-current for a reason other than current-empty; or accepted RTI missing/non-current after it exists | `Unavailable`; context use blocked and only applicable recovery actions returned | No fabricated data, timestamp, version, or completeness |
| Protected target | Archived, absent, denied, cross-Tenant, or unverifiable Project | No protected context or explanation | Observationally identical safe `404` |
| Refresh | Current owner batch evidence differs from persisted trust evidence | New snapshot reflects current safe metadata and provenance | No command, event, task, audit, repair, or sibling mutation |
| Explain | Current assembled evidence includes included and excluded candidates | Deterministic per-reference explanation with no persisted identity | No secrets, payloads, raw upstream problems, or durable trace |
| Replay or fault | Duplicate dispatch, rebuild, restart, store fault, owner fault, or oversized reference set | Deterministic persisted convergence or honest `Partial`/`Unavailable`; bounded work up to 5,000 references | Preserve cancellation and fail closed without leakage |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Projects.Server/Queries/GetProjectContextEndpoint.cs:52` -- legacy Get; `ProjectContextConversationsPageSize` 100 at 50; authorizes at 73-81; Conversation first page at 104-110; no ACL recheck after lookup. Preserve for shadow; do not extend into the supported handler.
- `src/Hexalith.Projects.Server/Queries/RefreshProjectContextEndpoint.cs:65` -- legacy Refresh; live Folder/Memory/Conversation `Task.WhenAll` fan-out at 121-153; File evidence stays on the projection. Preserve; do not copy live fan-out into the supported path.
- `src/Hexalith.Projects.Server/Queries/GetProjectContextExplanationEndpoint.cs:52` -- legacy Explain; HTTP-bound reassembly. Preserve.
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs:134` -- legacy REST map for `GET .../context`, `.../context/explain`, `.../context/refresh`. Keep until Story 6.7.
- `src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs:41` -- pure allowlist to reuse. Success still hardcodes `Assembled` at 183. Stale-Tenant allowance at 228 must not override AD-32 required-evidence rules.
- `src/Hexalith.Projects/Context/ProjectContextInclusionOrder.cs:34` -- TenantAuthority → ProjectVisibility → ProjectLifecycle → ReferenceAuthorization → ReferenceLifecycle → ReferenceFreshness → ReferenceKindAllowlist. Reuse; do not fork.
- `src/Hexalith.Projects.Contracts/Models/ProjectContext.cs:47` -- legacy DTO; no AD-32 snapshot fields. Keep additive compatibility; do not treat it as supported truth.
- `src/Hexalith.Projects.Contracts/Queries/GetConversationStartSetupQuery.cs` -- Story 6.2 singleton query (`ProjectId`). Reuse the query shape; do not edit unless extracting shared snapshot types.
- `src/Hexalith.Projects.Contracts/Queries/ConversationStartSetupResponse.cs:16` -- story-local AD-32 types (`ConversationStartResponseState`, `ConversationStartComponent`, `ConversationStartAdmissionSnapshot`, `ConversationStartSetupResponse`) in one file. Pattern to copy; no shared AD-32 types exist elsewhere.
- `src/Hexalith.Projects.Server/Queries/GetConversationStartSetupQueryHandler.cs:25` -- supported handler: envelope `TenantId`/`UserId`/`OriginalActorId`/`EntityId??AggregateId` at 45-51; `TenantAccessAuthorizer.AuthorizeDiagnosticReadAsync` before store read at 55-63; Active-only at 69-72; null Setup → `ConversationStartSetup.Empty` at 86-90; `Complete` vs `Unavailable` only (Partial declared, never set); `QueryResult.Failure("safe-denial")`. Copy this shape; do not use body/header identity.
- `src/Hexalith.Projects.Server/Projections/ConversationStartSetup/ConversationStartSetupProjectionHandler.cs` -- `IAsyncDomainProjectionHandler`; `StoreName` `projects-conversation-start-setup` at 26; key `{tenantId}:projects:{projectId}` at 45; `Seed` plus `Apply` at 88-120; `ReadModelWritePolicy.UpdateAsync`. Copy this fold; do not `Rebuild` from an event slice.
- `src/Hexalith.Projects.Server/InMemoryProjectReadModelStore.cs` and `ProjectsServerServiceCollectionExtensions.cs:69` -- fake-then-swap: `AddProjectsServer` registers in-memory `IReadModelStore` at 69-70 and `GetConversationStartSetupQueryHandler` at 123; `AddProjectsServerRuntimeInfrastructure` removes it and installs EventStore at 138-141, then registers the projection. Register 6.3 handlers the same way.
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs:208` -- hand-rolled `POST /query` via `DomainQueryDispatcher`; failure → `Results.NotFound`. Add 6.3 query types here. Do not migrate `AddEventStoreDomainService` in this story (DW-63).
- `src/Hexalith.Projects.Server/Program.cs:15` -- `AddProjectsServer` plus `AddProjectsServerRuntimeInfrastructure`; no SDK host.
- `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailProjection.cs:28` -- deterministic Project fold to reuse. `ProjectDetailProjection.Seed` already exists for incremental apply.
- `src/Hexalith.Projects/Projections/ProjectReferenceIndex/ProjectReferenceIndexProjection.cs:21` -- Project membership/reverse index only. Not the Reference Trust Index. No `ReferenceTrust*` types in `src/`.
- `src/Hexalith.Projects.Infrastructure/DaprProjectProjectionStore.cs:23` -- forbidden journal/rebuild-on-read store. Preserve for legacy shadow only.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Queries/QueryEnvelope.cs` -- TenantId, UserId, OriginalActorId, AuthenticatedWorkloadId, IsDelegated, DelegationId, Scopes, Audience. No Action/Target/Version members.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/IDomainQueryHandler.cs` and `IAsyncDomainProjectionHandler.cs` -- required handler seams.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/IReadModelStore.cs`, `IReadModelBatchStore.cs`, `IReadModelBulkStore.cs`, and `ReadModelWritePolicy.cs` -- only permitted persisted read-model path. Batch/bulk stores are unused in Projects `src/` today. Owner G-2 is not `IReadModelBulkStore`.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Queries/SafeDenialQueryRouter.cs` -- SDK opt-in denial router. Unused until SDK host; keep 6.2 `"safe-denial"` mapping on `/query`.
- `src/Hexalith.Projects.Testing/Context/ProjectContextEvidenceBuilder.cs:22` and `tests/Hexalith.Projects.Tests/Context/` -- reuse decision-matrix fixtures; extend rather than fork. No `Testing/Reads/` tree or `ProjectContextShadowComparator` yet.
- `_bmad-output/test-artifacts/test-design-epic-6.md:180` -- required E6.3-U01/U02/U03, A01/A02/A03 and P1 E6.3-A04; plus E6-X01 privacy.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- DW-63 SDK host migration, DW-66 Partial admission, DW-67 Folder Unauthorized collapse; all open, gated to 6.7. Do not close them here.
- `module/hexalith-projects.module.json` -- required G-4 manifest; absent. Do not author a substitute.

## Tasks & Acceptance

**Execution:**
- [ ] `src/Hexalith.Projects.Contracts/Queries/` shared AD-32 snapshot types (one type per file) plus `GetProjectContextQuery.cs`, `RefreshProjectContextQuery.cs`, and `ExplainContextSelectionQuery.cs` -- extract the Story 6.2 snapshot vocabulary into shared types without changing Conversation-start JSON, then add additive singleton context queries. Rationale: one snapshot family for Chatbot; keep the legacy `ProjectContext` DTO.
- [ ] `src/Hexalith.Projects.Server/Queries/GetProjectContextQueryHandler.cs` and `ExplainContextSelectionQueryHandler.cs` -- follow `GetConversationStartSetupQueryHandler`: envelope identity, `TenantAccessAuthorizer` before lookup, exact query-target equality, Active plus one-Folder rule, current-empty Setup, shared inclusion policy, `QueryResult` or `"safe-denial"`. Until RTI exists, treat missing owner-backed trust as explicit `Partial` omissions. Rationale: implement Get and Explain on local seams now.
- [ ] `src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs` -- adapt assembly to AD-32 usability without duplicating decisions in handlers; keep stale-Tenant allowance from overriding required-evidence `Unavailable`. Rationale: preserve the pure allowlist.
- [ ] `src/Hexalith.Projects.Server/Queries/RefreshProjectContextQueryHandler.cs` and `ProjectContextOwnerRefreshService.cs` -- Refresh-only counted G-2 owner batches; match by opaque identity; zero persisted writes. Skip until G-2 is approved. Rationale: Refresh must not copy legacy live fan-out.
- [ ] `src/Hexalith.Projects.Server/Projections/` Reference Trust Index handler, item, and backfill types (one type per file) -- implement only the approved Tenant-scoped schema and bounded producer; atomic checkpoint/index writes through `IReadModelBatchStore`; no per-actor fingerprint. Skip until the RTI schema is approved. Rationale: ingestion is the only writer; do not invent a schema.
- [ ] `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs` -- register Get and Explain handlers on the existing fake-then-swap `/query` composition; keep legacy REST routes; register Refresh and RTI only when those tasks un-skip. Rationale: match Story 6.2 without a host migration.
- [ ] `src/Hexalith.Projects.Testing/Reads/ProjectContextShadowComparator.cs` -- compare legacy and supported Get/Explain on a frozen representable corpus now; add Refresh when that handler exists; do not normalize known legacy deficits. Rationale: E6.3-A04.
- [ ] `tests/Hexalith.Projects.Contracts.Tests/`, `tests/Hexalith.Projects.Tests/Context/`, `tests/Hexalith.Projects.Tests/Queries/`, `tests/Hexalith.Projects.Server.Tests/Queries/` -- cover the I/O matrix, E6.3-U01/U03, A01/A02/A03, zero-write Get/Explain, leakage, safe-404, and current-empty Setup; add U02/Refresh when un-skipped. Rationale: focused proof before G-4.

**Acceptance Criteria:**
- Given current authorized evidence, when Get, Refresh, or Explain runs through the supported `/query` handler, then the result matches the matrix, uses one AD-32 snapshot vocabulary, and exposes metadata only.
- Given any candidate reference, when assembly evaluates it, then Tenant, Project, lifecycle, authorization, and freshness checks run in order and the candidate is either included or explicitly excluded.
- Given missing, denied, Archived, or cross-Tenant Project authority, when any Story 6.3 query runs, then the caller-observable response is the canonical safe `404`.
- Given refresh or explanation, when persisted state is compared before and after, then no Project event, task, audit, selection trace, or sibling mutation was written.
- Given duplicate delivery, rebuild, restart, store or owner fault, or more than 5,000 candidates, when the supported models run, then state converges or the query returns honest `Partial`/`Unavailable` without truncation.
- Given current required evidence and an authoritative empty trust index, when assembly runs, then it returns `Complete` with empty collections and does not invent a candidate.
- Given current required evidence and an intentional optional allowlist exclusion, when assembly runs, then the result is `Partial`.
- Given legacy and supported routes coexist, when the frozen comparable corpus runs, then Get, Refresh, and Explain match after the approved AD-32 normalization, and routing stays legacy until Story 6.7.

## Implementation Notes

## Spec Change Log

### 2026-08-24 — Review repair 1
- Trigger: the first review pass found ambiguous projection/query write boundaries and underspecified authority, evidence, ordering, owner-batch, failure, shadow, and verification rules.
- Amended: the post-gate execution and acceptance instructions now distinguish projection ingestion from zero-write queries, include File References, bind authorization per request, define deterministic fail-closed edge behavior and measurable approved limits, constrain shadow normalization, clarify manifest ownership, and add an implementation decision table plus revision preflight.
- Known-bad state avoided: inventing an actor-reusable trust decision, silently truncating or mis-associating owner evidence, treating mixed-version truth as `Complete`, persisting refresh/explanation side effects, or manufacturing a G-4 pass.
- KEEP: preserve the single shared AD-32 vocabulary, supported EventStore read-model seams, safe denial, metadata-only responses, legacy shadow routing, operator-owned prerequisites, and untouched `sprint-status.yaml`.

### 2026-08-24 — Review repair 2
- Trigger: the second review pass found unresolved authority-source distinctions, normal-read authorization semantics, shadow-corpus comparability, evidence-cutoff meaning, ingestion entry points, aggregate limits, and revision preflight.
- Amended: the post-gate plan now separates envelope claims from server expectations, defines exact-scope persisted authorization, freezes comparable shadow inputs, defines authoritative `asOf`, revalidates all query paths, maps aggregate overflow, names ingestion/backfill artifacts and transitions, expands all-operation bounds, requires a checkout allowlist, and splits operator actions into independently completable imperative actions.
- Known-bad state avoided: trusting caller-selected action/scope, comparing changing or known-non-equivalent legacy evidence, leaking after mid-read revocation, accepting same-version mutation, exceeding output bounds, or accepting evidence whose approved inputs cannot be identified and matched.
- KEEP: retain every Review repair 1 safeguard, do not edit the intent contract, do not manufacture absent schemas/pins/tooling, and finalize operator-owned prerequisites as `awaiting-operator` without touching `sprint-status.yaml`.

### 2026-08-24 — Review repair 3
- Trigger: the third review pass exposed an invented unbounded actor-fingerprint model plus remaining ambiguity in gate conjunction, target binding, omission disclosure, empty-index proof, ingestion atomicity, serialization limits, telemetry retention, and evidence checkout phases.
- Amended: all operator actions are now a conjunctive readiness gate; authorization outcomes require an approved bounded producer/applicability model; query identity binds exactly to the envelope target; empty indexes require completeness markers; checkpoints/backfills/tombstones are bounded and atomic; actual serialized bytes govern response limits; evidence provenance has explicit pre-run, generated-artifact, commit, and final-clean phases.
- Known-bad state avoided: unbounded per-principal trust state, authorization evidence with no producer, topology leakage, uninitialized state labeled empty, backfill/live-update races, underestimated responses, and dirty-checkout evidence accepted as clean.
- KEEP: retain Review repairs 1 and 2, current-only AD-32 truth, supported stores, safe denial, bounded zero-write queries, frozen shadow comparison, operator-owned gates, and the untouched orchestrator status file.

## Review Triage Log

### 2026-08-24 — Review pass 1
- intent_gap: 0
- bad_spec: 12: (high 9, medium 3, low 0)
- patch: 0
- defer: 0
- reject: 4: (high 0, medium 2, low 2)
- addressed_findings:
  - `[high]` `[bad_spec]` Separated EventStore projection ingestion writes from the zero-write Get, Refresh, and Explain query path; required approved ingestion, backfill, tombstone, gap, duplicate, stale-delivery, and rebuild rules.
  - `[high]` `[bad_spec]` Partitioned pre-authority safe denial from post-authority required-evidence `Unavailable`, including zero/multiple Folder, corrupt model, and mixed-version cases.
  - `[high]` `[bad_spec]` Defined protected-candidate redaction, never-verified provenance, unknown-kind, duplicate-identity, and deterministic exclusion behavior.
  - `[high]` `[bad_spec]` Added File References to G-2 and prohibited persisted/reused positive actor authorization in favor of per-request envelope authorization.
  - `[high]` `[bad_spec]` Required one captured `asOf`, injected time, approved freshness boundaries, non-regressing owner versions, and coherence checks across owner batches.
  - `[high]` `[bad_spec]` Defined fail-closed behavior above 5,000 references and made batch, concurrency, timeout, retry, metadata-size, response-size, and cancellation bounds measurable from approved pins.
  - `[medium]` `[bad_spec]` Specified stable type/ordinal/identity ordering and duplicate rejection for included and excluded decisions.
  - `[high]` `[bad_spec]` Bound parsed query identity and fixed operation authority to the envelope, with safe denial on malformed, missing, mismatched, or revoked authority.
  - `[high]` `[bad_spec]` Added canonical handling for required versus optional faults, owner/store faults, partial batches, cancellation, and `QueryResult`/HTTP outcomes.
  - `[medium]` `[bad_spec]` Fixed Explain to the same persisted evidence and assembly decision set as Get at one request `asOf`, without live owner fan-out.
  - `[high]` `[bad_spec]` Constrained shadow normalization and the full safe-404 observable surface so comparison cannot hide authorization, freshness, omission, ordering, header, body, cache, log, or trace differences.
  - `[medium]` `[bad_spec]` Clarified that the operator supplies the approved manifest and added exact baseline/submodule preflight evidence before canonical verification.

### 2026-08-24 — Review pass 2
- intent_gap: 0
- bad_spec: 15: (high 10, medium 5, low 0)
- patch: 0
- defer: 0
- reject: 4: (high 0, medium 2, low 2)
- addressed_findings:
  - `[high]` `[bad_spec]` Clarified that Folders G-2 owns Folder and File Reference evidence and that all aggregate-response and freshness limits must be approved before implementation.
  - `[high]` `[bad_spec]` Distinguished immutable envelope-presented claims from server-owned expected action, audience, scope, target, and version values and required exact matching.
  - `[high]` `[bad_spec]` Restricted normal Get/Explain to exact-scope indexed authorization evidence and Refresh to counted actor-scoped G-2 owner batches.
  - `[high]` `[bad_spec]` Required final authority-version revalidation after persisted or owner reads on every operation.
  - `[medium]` `[bad_spec]` Defined `asOf` as the authoritative evidence cutoff and kept request observation time separate.
  - `[high]` `[bad_spec]` Limited shadow equivalence to frozen, commonly representable inputs and required known legacy truncation/File-refresh deficits to be exercised separately rather than normalized away.
  - `[medium]` `[bad_spec]` Defined Explain as current-at-request and required only same-snapshot test comparisons, removing any implied prior-Get correlation token.
  - `[medium]` `[bad_spec]` Distinguished forbidden durable selection traces from redacted operational telemetry and normalized nondeterministic telemetry fields for safe-denial comparison.
  - `[high]` `[bad_spec]` Named owner-change subscription/checkpoint and backfill artifacts and added tests for deletion, gap, out-of-order, duplicate, restart, and rebuild transitions.
  - `[high]` `[bad_spec]` Defined authoritative empty-index and aggregate-response-overflow outcomes and prohibited same-version/different-evidence acceptance.
  - `[medium]` `[bad_spec]` Made canonical persisted opaque identity, not ad hoc text normalization, the final deterministic sort key.
  - `[high]` `[bad_spec]` Limited opaque exclusion topology to Project-owned ordinals already authorized for disclosure.
  - `[high]` `[bad_spec]` Required canonical prerequisite artifact identifiers/hashes and a porcelain checkout allowlist before evidence acceptance.
  - `[high]` `[bad_spec]` Removed the integration-environment-blocker alternative once G-4 is accepted and extended bounds to Get and Explain.
  - `[medium]` `[bad_spec]` Split bundled operator prerequisites into independently completable imperative actions.

### 2026-08-24 — Review pass 3
- intent_gap: 0
- bad_spec: 14: (high 10, medium 4, low 0)
- patch: 0
- defer: 0
- reject: 5: (high 0, medium 2, low 3)
- addressed_findings:
  - `[high]` `[bad_spec]` Made all frontmatter operator actions a conjunctive expansion of the `Block If` gates, including performance/freshness pins, ingestion rules, hashes, and prerequisite-record availability.
  - `[high]` `[bad_spec]` Reasserted exact parsed-query-to-envelope-target equality and explained that envelope-derived authority means reading presented immutable claims before matching server-owned expectations.
  - `[high]` `[bad_spec]` Removed the invented per-actor fingerprint and required an approved bounded authorization-outcome applicability key, producer, expiry/retention, and deletion model before implementation.
  - `[high]` `[bad_spec]` Made last-verified and Project-ordinal disclosure conditional on approved Project-owned topology disclosure and mapped unavailable topology to required-evidence `Unavailable` after final authority revalidation.
  - `[medium]` `[bad_spec]` Unified unknown-kind handling as schema-invalid required trust evidence and defined an authoritative empty index through completeness/checkpoint markers.
  - `[high]` `[bad_spec]` Defined atomic checkpoint/index writes, fenced paged backfill, live/backfill ordering, resumable cancellation, and bounded tombstone retention/compaction/re-creation.
  - `[high]` `[bad_spec]` Required actual final-serializer byte measurement and G-4-pinned performance methodology rather than estimated payload/allocation bounds.
  - `[medium]` `[bad_spec]` Classified intentional optional policy exclusions as `Partial`, reserving `Complete` for no optional omission.
  - `[high]` `[bad_spec]` Added stale-Tenant policy to the independently tested legacy deficits and required final authority revalidation to precede all evidence-state responses.
  - `[medium]` `[bad_spec]` Expanded safe-denial equivalence to the accepted full response policy and bounded timing oracle while distinguishing semantics from nondeterministic telemetry identifiers.
  - `[high]` `[bad_spec]` Applied the no-selection-trace rule to retained/exported telemetry, not merely in-process lifetime.
  - `[high]` `[bad_spec]` Added a canonical prerequisite-record frontmatter reference populated by the operator and consumed by the named G-4 runner for executable hash matching.
  - `[high]` `[bad_spec]` Split checkout validation into pre-run clean implementation, post-run evidence allowlist, evidence commit, and final clean phases.
  - `[medium]` `[bad_spec]` Gave review passes stable numeric identities while retaining the workflow-required aggregate rejection counts.

### 2026-08-25 — Review pass 4
- intent_gap: 0
- bad_spec: 0
- patch: 6: (high 2, medium 4, low 0)
- defer: 0
- reject: 20: (high 0, medium 8, low 12)
- addressed_findings:
  - `[high]` `[patch]` Corrected batch-fault acceptance so required evidence fails `Unavailable` while optional candidate faults remain explicit `Partial` exclusions.
  - `[high]` `[patch]` Added zero-write Get coverage and authorization-outcome producer/applicability failure cases to the mandatory test task.
  - `[medium]` `[patch]` Added bounded live-change buffering/spill and safe-denial timing-equivalence to the operator-approved performance limits.
  - `[medium]` `[patch]` Required one schema-selected canonical opaque identity representation and comparer rather than allowing byte/string implementation choice.
  - `[medium]` `[patch]` Distinguished forbidden selection-trace identities from ordinary operational request/span identifiers.
  - `[medium]` `[patch]` Required the G-4 profile to prove prerequisite tampering fails closed and to enforce pre/post evidence checkout allowlists.

## Design Notes

Get, Refresh, and Explain stay one story: one authority boundary, one allowlist, one snapshot contract, one fixture corpus, and a zero-write query invariant. Get and Explain read persisted current evidence only. Refresh substitutes a bounded owner-batch snapshot without updating the index. Explain returns the same assembly evaluations with no persisted trace identity.

Checkout facts (2026-09-06): Story 6.1 list/open handlers did not land. Story 6.2 did land a supported `/query` handler, incremental Conversation-start projection, fake-then-swap `IReadModelStore`, and story-local AD-32 types in `ConversationStartSetupResponse.cs`; null Setup becomes `ConversationStartSetup.Empty` and still reports Setup current; `Partial` is never produced (DW-66) and Folder Unauthorized collapses to Unavailable (DW-67). No Conversations/Folders/Memories G-2 batch-read contracts, no Reference Trust Index types, no `IReadModelBatchStore` usage, and no `module/hexalith-projects.module.json` exist in this checkout. `QueryEnvelope` has no Action/Target/Version members. SDK host migration remains DW-63.

Agent decisions (not user-visible): follow the Story 6.2 host and DI pattern; use `Seed` plus `Apply` rather than slice `Rebuild`; keep `"safe-denial"` on the hand-rolled `/query` rather than `SafeDenialQueryRouter`; put new handlers next to `GetConversationStartSetupQueryHandler`; one C# type per new file; extract shared snapshot types under `src/Hexalith.Projects.Contracts/` without changing Conversation-start JSON; do not edit `sprint-status.yaml`. Operator accepted the full-spec size (review logs retained).

### Post-gate implementation decisions

"Derive from `QueryEnvelope`" means read immutable presented caller context and validate it against server-owned expectations. It never lets the caller choose expected action, audience, scope, target, or version policy. Qualifying G-4 evidence still requires an operator `prerequisite_record`; focused tests are not that evidence.

| Boundary | Required decision |
|---|---|
| Write ownership | Only asynchronous projection ingestion may write the approved read model and its partition checkpoint, atomically through the supported batch store under the accepted schema. Get, Refresh, and Explain perform no EventStore command, domain event, read-model/checkpoint write/delete, task, audit, repair, sibling mutation, or selection-diagnostic persistence. Zero-write assertions bracket query dispatch separately from projection-delivery tests. |
| Authority and target | Treat Tenant, original actor, workload, delegation, presented scopes/audience, and target in `QueryEnvelope` as immutable caller context, never expected policy. Canonically parse the query Project identity and require exact value equality with the envelope target, not merely target-shape compatibility. Resolve the fixed expected action, audience, scope, target shape, and authority version server-side and require exact matches. Missing, malformed, mismatched, denied, cross-Tenant, Archived, or not-yet-authorized existence returns canonical safe `404`. Revalidate authority after all persisted/owner reads and before evaluating or returning any `Unavailable`/`Partial`/`Complete` content. |
| Required evidence | After Project authority is established, missing, stale, rebuilding, corrupt, version-incoherent, zero-Folder, or multiple-Folder Project/Folder/authorization evidence yields canonical `Unavailable`. Null Setup is current-empty, not missing. Until RTI exists, missing owner-backed trust is an explicit optional `Partial` omission, not required `Unavailable` and not an invented inclusion. After RTI is accepted, missing or non-current trust-index evidence is required `Unavailable`. Never label mixed or regressed evidence `Complete`. |
| Candidate disclosure | Emit identity/type metadata only for a Project-linked candidate whose sibling authority is confirmed for the current envelope. For denied or unconfirmed candidates, emit one deterministic opaque exclusion per Project ordinal only after the Projects-owned link topology and that ordinal are authorized for disclosure. Unknown kinds and duplicate identity/ordinal make required trust evidence `Unavailable`. |
| Authorization persistence | The Tenant-scoped index may retain only approved non-payload owner provenance and authorization outcome. Get/Explain make zero owner calls. Refresh obtains current actor-scoped outcomes only through counted G-2 owner batches but remains zero-write. Do not invent per-actor fingerprints. |
| Snapshot and freshness | Derive one authoritative evidence cutoff `asOf` from coherent Project/index or owner watermarks; never substitute local request-observation time. Reject regressing watermarks and same-version/different-evidence input. |
| Bounds and owner batches | Reject more than 5,000 candidates or an over-limit serialized response as canonical minimal `Unavailable`; never truncate. Get/Explain issue zero owner calls. Match owner responses by opaque identity, never array position. |
| Determinism | Order by `ProjectContextInclusionOrder`, then persisted Project ordinal, then the schema's one opaque identity comparer. Reject conflicting duplicates. |
| Explain | Use the same persisted snapshot and assembly policy as Get at one evidence `asOf`. Do not live-refresh owners. Do not require a prior-Get token. |
| Shadow corpus | Compare only the frozen representable intersection (legacy 100-Conversation page, identical File evidence, no stale-Tenant allowance). Exercise Conversation overflow, changed File evidence, and stale-Tenant as known legacy deficits; never normalize them away. |
| Telemetry | No selection-trace identities or payload-bearing diagnostics. Ordinary request/span IDs are allowed through existing redaction. |
| Index completeness | After RTI exists, zero candidates are authoritative `Complete` empty only when completeness/checkpoint/watermark/rebuild markers prove a fully initialized index; otherwise required trust evidence is `Unavailable`. Until RTI exists, assemble from Project-owned persisted detail and the allowlist; do not invent RTI completeness markers. |
| Failure mapping | Before authority, only canonical safe denial. After authority: required failure is `Unavailable`, optional failure is explicit `Partial`, cancellation propagates. |
| Verification provenance | Qualifying evidence needs an operator `prerequisite_record` and G-4 runner. Focused unit/server tests are not that evidence. |

## Verification

**Commands:**
- `python3 tools/planning/validate_production_authority.py --story-id 6.3` -- expected: Story 6.3 remains within production authority.
- `dotnet restore Hexalith.Projects.slnx` -- expected: restore succeeds with central dependency ownership.
- `dotnet build Hexalith.Projects.slnx --configuration Debug` -- expected: zero warnings and errors.
- `dotnet test tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj --configuration Debug` -- expected: additive supported and legacy contracts pass.
- `dotnet test tests/Hexalith.Projects.Tests/Hexalith.Projects.Tests.csproj --configuration Debug` -- expected: policy, projection, handler, replay, leakage, and zero-write cases pass.
- `dotnet test tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Debug` -- expected: supported composition, safe denial, shadow, and legacy regressions pass.
- `git diff --check` -- expected: no whitespace errors.

**Manual checks (if no CLI):**
- Confirm `sprint-status.yaml` has no diff from this story.
- Do not treat `dotnet tool run hexalith-module test --profile reads --filter Story=6.3` as runnable until the operator supplies `module/hexalith-projects.module.json` and the G-4 reads profile.
