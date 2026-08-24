---
title: 'Retrieve assembled Project Context through supported read models'
type: 'feature'
created: '2026-08-24'
status: 'awaiting-operator'
review_loop_iteration: 3
followup_review_recommended: true
baseline_revision: '5e7ac08bd6faa623cf4005f8449b786dbce07c2a'
prerequisite_record: null
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-state-instructions.md'
warnings: [oversized]
operator_actions:
  - 'Accept the exact-current Story 6.1 prerequisite chain through P4.'
  - 'Record immutable platform and identity pins, authorized same-baseline Solution Architect sign-off, executable rollback, clean-checkout evidence, and a superseding independent READY result for the accepted Story 6.1 chain.'
  - 'Land and publish the accepted Story 6.1 shared AD-32 contracts, persisted Project detail read models, dual-principal authorization seam, supported DomainService query composition, and safe-denial route names for Story 6.3 to reuse.'
  - 'Accept and pin the Conversations G-2 bounded batch-read, actor-scoped authorization, owner-version, watermark, freshness, failure, revision, and rollback contract.'
  - 'Accept and pin the Folders G-2 bounded batch-read contract covering Folder and File Reference evidence, actor-scoped authorization, owner version, watermark, freshness, failure, revision, and rollback.'
  - 'Accept and pin the Memories G-2 bounded batch-read, actor-scoped authorization, owner-version, watermark, freshness, failure, revision, and rollback contract.'
  - 'Approve the Reference Trust Index schema, authorization-outcome applicability and bounded producer model, ingestion and backfill sources, atomic checkpointing, tombstone retention, replay rules, and bounded owner-batch protocol.'
  - 'Approve the batch-size, owner-call, concurrency, timeout, retry, live-change buffering or spill, metadata-field, aggregate-response-size, allocation, freshness, and safe-denial timing-equivalence limits for all three Story 6.3 operations.'
  - 'Resolve the canonical null-Setup semantics before implementation.'
  - 'Create one canonical prerequisite record containing repository paths, immutable revisions, and content hashes for every accepted Story 6.1, G-2, Reference Trust Index, performance, and G-4 artifact; set `prerequisite_record` to its repository-relative path and append that path to `context`.'
  - 'Provide the approved G-4 module manifest and reads profile capable of producing persisted Story 6.3 TRX, JSON, shadow-equivalence, zero-write, and NoPayloadLeakage evidence from a clean checkout.'
deferred: []
---

<intent-contract>

## Intent

**Problem:** The Story 3.x context routes assemble from a Projects-owned Dapr journal, truncate Conversation evidence to one page, and refresh through incomplete live fan-out. They cannot prove the supported AD-32 Project, Folder, Setup, authorization, version, component, recovery, and Reference Trust Index truth required for Chatbot grounding.

**Approach:** Add one cohesive, read-only DomainService slice for context retrieval, refresh, and explanation over EventStore-managed persisted Project and Reference Trust Index models. Reuse the accepted Story 6.1 contracts and authorization seam, preserve the pure allowlist policy and legacy routes for shadow comparison, and expose no sibling payload or durable diagnostic trace.

## Boundaries & Constraints

**Always:** Derive Tenant, original actor, authenticated workload, delegation, scopes, audience, action, target, and version authority from the immutable `QueryEnvelope` and the accepted Story 6.1 authorization seam. Use named `IAsyncDomainProjectionHandler` projections, `IReadModelStore`/`IReadModelBatchStore`, and `ReadModelWritePolicy`; require an Active Project with exactly one authorized Folder; include a reference only after Tenant, Project, lifecycle, authorization, and freshness checks pass; make every omission explicit; use the shared AD-32 snapshot and closed state/reason/recovery vocabularies; preserve deterministic ordinal ordering; keep refresh bounded, read-only, and zero-write; keep explanation current, request-scoped, and nonpersistent.

**Block If:** Pause source implementation and finalize `awaiting-operator` if the accepted Story 6.1 shared contracts/read models/authorization/composition/safe-denial seams are unavailable; the applicable Conversations/Folders/Memories G-2 batch-read contracts or Reference Trust Index schema remain unapproved; null Setup has no canonical meaning; or the approved G-4 reads profile cannot produce persisted evidence. Never use `blocked` for these operator-owned prerequisites.

**Never:** Write or revert `sprint-status.yaml`; trust payloads or custom headers for authority; add direct Dapr state access, another journal/query runtime, a second AD-32 vocabulary, an unbounded per-reference fan-out, or a parallel trust store; persist explanation/selection traces; mutate Projects or siblings during refresh; expose Tenant/actor authority, claims, tokens, prompts, transcripts, paths, file/memory content, secrets, raw owner errors, or unconfirmed-candidate detail; hand-edit generated files; switch or retire legacy routing before Story 6.7.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Complete context | Authorized Active Project; current Project, exactly one Folder, Setup, authorization, and all selected reference evidence | Metadata-only setup and ordered included/excluded references with AD-32 `Complete`, `asOf`, authorized version, components, and `None` recovery | No error expected |
| Partial context | Required evidence current; optional reference denied, stale, rebuilding, unavailable, or excluded | Usable `Partial`; every omission has closed state/reason/last-verified evidence and applicable recovery | No raw owner detail; never silently drop a candidate |
| Required evidence non-current | Project, Folder, Setup, authorization, or trust-index evidence missing/non-current | `Unavailable`; context use blocked and only applicable recovery actions returned | No fabricated data, timestamp, version, or completeness |
| Protected target | Archived, absent, denied, cross-Tenant, or unverifiable Project | No protected context or explanation | Observationally identical safe `404` |
| Refresh | Current owner batch evidence differs from persisted trust evidence | New snapshot reflects current safe metadata and provenance | No command, event, task, audit, repair, or sibling mutation |
| Explain | Current assembled evidence includes included and excluded candidates | Deterministic per-reference explanation with no persisted identity | No secrets, payloads, raw upstream problems, or durable trace |
| Replay or fault | Duplicate dispatch, rebuild, restart, store fault, owner fault, or oversized reference set | Deterministic persisted convergence or honest `Partial`/`Unavailable`; bounded work up to 5,000 references | Preserve cancellation and fail closed without leakage |

</intent-contract>

## Code Map

- `src/Hexalith.Projects.Server/Queries/GetProjectContextEndpoint.cs:52` -- legacy Get/shadow input; authorizes before lookup but reads one 100-row Conversation page and legacy detail evidence.
- `src/Hexalith.Projects.Server/Queries/RefreshProjectContextEndpoint.cs:47` -- legacy refresh/shadow input; omits File revalidation and uses live Folder/Memory/Conversation fan-out at lines 121-153.
- `src/Hexalith.Projects.Server/Queries/GetProjectContextExplanationEndpoint.cs:52` -- legacy Explain/shadow input; reassembles on HTTP-bound services and returns transient evaluations.
- `src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs:59` -- pure deterministic allowlist policy to preserve/refactor; its legacy stale-Tenant allowance at lines 222-230 must not override AD-32 required-evidence rules.
- `src/Hexalith.Projects.Contracts/Models/ProjectContext.cs:47` -- legacy compatible DTO; lacks shared AD-32 state, version, components, and recovery fields.
- `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailProjection.cs:41` -- deterministic Project fold to reuse in the accepted incremental detail projection.
- `src/Hexalith.Projects/Projections/ProjectReferenceIndex/ProjectReferenceIndexProjection.cs:28` -- Project membership/reverse index only; do not mislabel it as the owner-backed Reference Trust Index.
- `src/Hexalith.Projects.Infrastructure/DaprProjectProjectionStore.cs:23` -- forbidden legacy append-journal/rebuild-on-read store; preserve only for shadow compatibility.
- `src/Hexalith.Projects.Server/Program.cs:13` -- currently lacks accepted `AddEventStoreDomainService`/`UseEventStoreDomainService` composition.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/IDomainQueryHandler.cs:18` and `IAsyncDomainProjectionHandler.cs:8` -- supported query/projection discovery seams.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/IReadModelStore.cs:23`, `IReadModelBatchStore.cs:24`, and `ReadModelWritePolicy.cs:28` -- only permitted persisted read-model path.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Queries/QueryEnvelope.cs:158` -- immutable dual-principal authority source.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Queries/SafeDenialQueryRouter.cs:23` -- opt-in canonical denial/nonexistence unification.
- `tests/Hexalith.Projects.Tests/Context/` and `src/Hexalith.Projects.Testing/Context/ProjectContextEvidenceBuilder.cs:22` -- reusable decision-matrix, determinism, leakage, and evidence fixtures.
- `_bmad-output/test-artifacts/test-design-epic-6.md:180` -- required E6.3-U01/U02/U03, A01/A02/A03/A04 and E6-X01 evidence catalog.
- `module/hexalith-projects.module.json` -- required G-4 manifest; absent in this checkout, so canonical persisted evidence cannot yet run.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.Projects.Contracts/Queries/GetProjectContextQuery.cs`, `RefreshProjectContextQuery.cs`, and `ExplainContextSelectionQuery.cs` -- define additive singleton query contracts that target one Project and reuse the accepted shared AD-32 response types.
- `src/Hexalith.Projects/Projections/ReferenceTrustIndex/ReferenceTrustIndexItem.cs`, `ReferenceTrustIndexProjectionHandler.cs`, `ReferenceTrustIndexOwnerChangeHandler.cs`, and `ReferenceTrustIndexBackfillService.cs` -- implement the approved Tenant-scoped safe-metadata schema, owner-change subscription/checkpoint, and explicit bounded-page backfill entry point. Consume only approved Project-link and owner-change inputs; atomically advance partition checkpoint/index state through the supported batch store; fence concurrent backfills; order live/backfill handoff; handle tombstones, gaps, stale/out-of-order delivery, same-version/different-evidence conflicts, duplicates, cancellation, restart, retention/compaction, re-creation, and rebuilds deterministically. Persist an authorization outcome only with the approved bounded applicability key and producer; never invent an actor fingerprint or unbounded per-principal state.
- `src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs` and `src/Hexalith.Projects/Queries/ProjectContextAssemblyService.cs` -- adapt the pure ordered policy to accepted detail/trust evidence and AD-32 usability without duplicating decisions across handlers.
- `src/Hexalith.Projects/Queries/ProjectContextOwnerRefreshService.cs` -- use only the accepted bounded Conversations, Folders, File References, and Memories G-2 batch protocols, validate that every owner response corresponds exactly to requested opaque identities, merge current safe evidence into one request snapshot, and perform zero persisted/domain/sibling writes.
- `src/Hexalith.Projects/Queries/Handlers/GetProjectContextQueryHandler.cs`, `RefreshProjectContextQueryHandler.cs`, and `ExplainContextSelectionQueryHandler.cs` -- reauthorize the full envelope, read only supported persisted models, invoke the shared assembly/refresh services, and return canonical `QueryResult`/safe-denial outcomes.
- `src/Hexalith.Projects.Server/Program.cs`, `src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj`, and `src/Hexalith.Projects/Hexalith.Projects.csproj` -- consume the accepted central DomainService composition and register the domain assembly while retaining legacy routes for shadow reads.
- `src/Hexalith.Projects.Testing/Reads/ProjectContextShadowComparator.cs` -- compare legacy and supported Get/Refresh/Explain using one finite normalization that may adapt legacy field names, status vocabulary, and timestamp precision only. Compare Project/Folder/Setup decisions, every confirmed reference decision, freshness, authorization outcome, and ordering; validate supported-only AD-32 provenance/recovery separately; reject every other delta without logging protected values.
- `tests/Hexalith.Projects.Contracts.Tests/`, `tests/Hexalith.Projects.Tests/Context/`, `tests/Hexalith.Projects.Tests/Queries/`, and `tests/Hexalith.Projects.Tests/Projections/` -- cover contract evolution, the full matrix, duplicate/rebuild determinism, bounded 500/5,000-reference behavior, transient explanation, zero-write Get/Refresh/Explain, and accepted authorization-outcome producer absence, applicability-key collision, expiry boundary, cardinality exhaustion, deletion, and compaction safety.
- `tests/Hexalith.Projects.Server.Tests/Queries/` and `tests/Hexalith.Projects.Integration.Tests/` -- prove dual-principal reauthorization, exact safe-404 equivalence, real gateway-to-`/query` composition, persisted end state/restart/fault behavior, shadow equivalence, and leakage across response/log/trace surfaces.
- The operator-provided `module/hexalith-projects.module.json` reads profile and `evidence/epic6/` -- consume the approved manifest without authoring a substitute, then retain real bound TRX/JSON plus NoPayloadLeakage, state-hash, zero-write, revision, and command evidence; never hand-author a pass.

**Acceptance Criteria:**
- Given current authorized evidence, when Get, Refresh, or Explain runs through the supported gateway and handler, then the result matches the matrix, uses one shared AD-32 snapshot vocabulary, and exposes metadata only.
- Given any candidate reference, when assembly evaluates it, then Tenant, Project, lifecycle, authorization, and freshness checks run in order and the candidate is either included or explicitly excluded with deterministic shared vocabulary.
- Given missing, denied, Archived, or cross-Tenant Project authority, when any Story 6.3 query runs, then the caller-observable response is the exact canonical safe `404` and reveals no protected target fact.
- Given refresh or explanation, when persisted state is compared before and after, then no Project event, task, audit, selection trace, maintenance record, repair, or sibling mutation was written.
- Given duplicate delivery, rebuild, restart, store/owner fault, 500 references, or 5,000 references, when the supported models and queries run, then state converges deterministically, work remains bounded, and degraded truth is never labeled `Complete`.
- Given current required evidence and an authoritative empty Reference Trust Index, when any operation assembles context, then it returns `Complete` with empty included/excluded collections and does not invent a candidate.
- Given current required evidence and any intentional optional allowlist exclusion, when assembly runs, then the result is `Partial` and the safe omission is explicit; `Complete` is reserved for no optional omission.
- Given 5,001 or more candidates, aggregate output above the approved response-size limit, an unknown kind, a duplicate identity or ordinal, corrupt persisted evidence, a required incomplete/mismatched owner batch, an older required owner watermark, same-version/different-evidence required owner data, or a required version change during assembly, when a query runs, then it returns the canonical minimal `Unavailable` result and applicable shared recovery action without truncation, association by position, state regression, or a fabricated context snapshot; the same faults affecting optional candidates yield explicit `Partial` exclusions.
- Given the approved G-2 performance limits, when Get, Refresh, or Explain processes any allowed corpus, then total work, allocation, serialized response size, owner-call count (zero for normal Get/Explain), batch size, concurrency, timeout, and retry budget remain at or below those pinned limits; cancellation propagates and a partially failed batch cannot silently disappear.
- Given owner change, deletion, backfill, checkpoint restart, gap, stale/out-of-order delivery, duplicate delivery, or full rebuild, when Reference Trust Index ingestion runs, then its persisted end state and tombstones converge to the same approved state without accepting same-version/different-evidence input.
- Given legacy and supported routes coexist, when the canonical corpus runs, then Get, Refresh, and Explain are semantically equivalent after only the approved AD-32 normalization and routing remains legacy until Story 6.7.

## Spec Change Log

### 2026-08-24 — Review repair 1
- Trigger: the first review pass found ambiguous projection/query write boundaries and underspecified authority, evidence, ordering, owner-batch, failure, shadow, and verification rules.
- Amended: the post-gate execution and acceptance instructions now distinguish projection ingestion from zero-write queries, include File References, bind authorization per request, define deterministic fail-closed edge behavior and measurable approved limits, constrain shadow normalization, clarify manifest ownership, and add an implementation decision table plus revision preflight.
- Known-bad state avoided: inventing an actor-reusable trust decision, silently truncating or mis-associating owner evidence, treating mixed-version truth as `Complete`, persisting refresh/explanation side effects, or manufacturing a G-4 pass.
- KEEP: preserve the single shared AD-32 vocabulary, supported EventStore read-model seams, safe denial, metadata-only responses, legacy shadow routing, operator-owned prerequisites, and untouched `sprint-status.yaml`.

### 2026-08-24 — Review repair 2
- Trigger: the second review pass found unresolved authority-source distinctions, normal-read authorization semantics, shadow-corpus comparability, evidence-cutoff meaning, ingestion entry points, aggregate limits, and revision preflight.
- Amended: the post-gate plan now separates envelope claims from server expectations, defines exact-scope persisted authorization, freezes comparable shadow inputs, defines authoritative `asOf`, revalidates all query paths, maps aggregate overflow, names ingestion/backfill artifacts and transitions, expands all-operation bounds, requires a checkout allowlist, and splits operator actions into independently recordable steps.
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

The three operations remain one story because they share a single authority boundary, persisted detail/trust evidence model, allowlist policy, AD-32 response contract, fixture corpus, and zero-write invariant. Get consumes persisted current evidence; Refresh substitutes a bounded current owner-batch snapshot without updating the index; Explain returns the same assembly evaluations without assigning or persisting a trace identity.

### Post-gate implementation decisions

Every non-empty `operator_actions` item is conjunctive. `Block If` references to accepted Story 6.1 seams, applicable G-2 contracts, the Reference Trust Index schema/protocol, null Setup, and G-4 include all corresponding frontmatter subrequirements; implementation remains paused until `prerequisite_record` names an accepted, hash-verifiable artifact set and every item is recorded complete. In the intent contract, "derive ... from `QueryEnvelope`" means read immutable presented caller context from the envelope and validate it against server-owned expectations; it never permits the caller to choose expected action, audience, scope, target, or version policy.

| Boundary | Required decision |
|---|---|
| Write ownership | Only asynchronous projection ingestion may write the approved read model and its partition checkpoint, atomically through the supported batch store under the accepted schema. Get, Refresh, and Explain perform no EventStore command, domain event, read-model/checkpoint write/delete, task, audit, repair, sibling mutation, or selection-diagnostic persistence. Zero-write assertions bracket query dispatch separately from projection-delivery tests. |
| Authority and target | Treat Tenant, original actor, workload, delegation, presented scopes/audience, and target in `QueryEnvelope` as immutable caller context, never expected policy. Canonically parse the query Project identity and require exact value equality with the envelope target, not merely target-shape compatibility. Resolve the fixed expected action, audience, scope, target shape, and authority version server-side through the accepted Story 6.1 seam and require exact matches. Missing, malformed, mismatched, denied, cross-Tenant, Archived, or not-yet-authorized existence returns that seam's canonical safe `404`; no Project or sibling lookup result may alter the accepted full response policy, including status, media type, body, deterministic headers, cache behavior, or bounded timing equivalence. Revalidate authority after all persisted/owner reads and before evaluating or returning any `Unavailable`/`Partial`/`Complete` content. Compare redacted logs/traces after normalizing only request/span IDs, timestamps, and timing; prohibit protected-value or semantic differences. |
| Required evidence | After Project authority is established, missing, stale, rebuilding, corrupt, version-incoherent, zero-Folder, or multiple-Folder Project/Folder/Setup/authorization/trust evidence yields canonical `Unavailable`, not `Partial` or safe denial. Optional-owner failure yields `Partial`; cancellation propagates through the accepted transport mapping. Never label mixed or regressed evidence `Complete`. |
| Candidate disclosure | Emit identity/type metadata only for a Project-linked candidate whose sibling authority is confirmed for the current envelope. For denied or unconfirmed candidates, emit one deterministic opaque exclusion per Project ordinal only after the Projects-owned link topology and that ordinal are authorized for disclosure; otherwise required topology evidence is `Unavailable` after final authority revalidation. Expose no owner identity/metadata, and expose `lastVerified` only when the accepted schema declares it safe for this disclosure state; otherwise use null. Unknown kinds are schema-invalid required trust evidence and yield `Unavailable`. Duplicate identity/ordinal or other schema-invalid index data also makes required trust evidence `Unavailable`. |
| Authorization persistence | The Tenant-scoped index may retain only the approved non-payload owner provenance and authorization outcome. The prerequisite schema must name its bounded applicability key, authoritative producer, encoding/collision behavior if hashed, freshness/expiry, cardinality bound, and deletion/compaction rules. Get/Explain make zero owner calls and include a candidate only when that accepted outcome is current and applicable to the immutable envelope; otherwise exclude/fail closed. Refresh obtains current actor-scoped outcomes only through counted G-2 owner batches but remains zero-write. Do not invent per-actor fingerprints or an unapproved mechanism for populating outcomes. |
| Snapshot and freshness | Derive one authoritative evidence cutoff `asOf` from the coherent accepted Project/index or owner watermarks; keep local request-observation time separate and never substitute it for evidence time. Apply the exact inclusive/exclusive freshness threshold and owner clock/watermark rules pinned by G-2 using injected `TimeProvider`. Reject regressing watermarks and same-version/different-evidence input; if owner batches cannot prove a coherent cutoff or authority/version changes mid-request, map required evidence to `Unavailable` and optional evidence to explicit `Partial` exclusions. Equal refresh evidence preserves decisions, evidence `asOf`, and provenance; only a separately named request-observation field may change if the approved contract provides one. |
| Bounds and owner batches | Reject more than 5,000 candidates or an actual complete response above the approved byte limit as canonical minimal `Unavailable` with the shared limit recovery action; never truncate or partially emit. Measure the full final serializer output, including envelope, escapes, components, exclusions, and recovery, under pinned serializer options before sending it. Apply G-4-pinned warm-up, runtime, GC, tracing, allocation, total-work, and output methodology to all operations. Refresh additionally uses pinned per-owner batch size, maximum calls, concurrency, timeout, and retry limits; Get/Explain issue zero owner calls. Match responses by opaque identity, never array position; missing, duplicate, unrequested, late, or partially failed items become explicit safe failures under the required/optional rule. |
| Determinism | Order by `ProjectContextInclusionOrder`, then persisted Project ordinal, then the one canonical opaque identity representation and ordinal comparer selected by the approved schema; perform no alternative byte/string choice or locale, case, Unicode, or display-text normalization during assembly. Apply the same comparer to inclusions and exclusions. Reject conflicting duplicates rather than choosing first/last. Projection ingestion accepts only approved monotonic owner versions, handles gaps/rebuild markers, rejects same-version mutation, and removes or tombstones deleted Project links according to the approved schema. |
| Explain | Read the current persisted Project detail and trust snapshot and invoke the same assembly policy as Get at one evidence `asOf`; do not live-refresh owners and do not require a prior-Get token. In tests, freeze projection state and require Explain and Get over the same snapshot to produce identical decisions. The production result remains current-at-request, request-scoped, and unpersisted. |
| Shadow corpus | Run legacy and supported operations against one frozen corpus and common owner cutoff that stays within the legacy 100-Conversation representable intersection, has identical persisted/live File evidence, and does not exercise the legacy stale-Tenant allowance. Compare all common decisions under the finite normalization. Exercise Conversation overflow, changed File evidence, and stale-Tenant authorization as explicit known legacy-deficit cases whose supported truth must pass independently; never normalize those deltas away or use them to cut routing over. |
| Telemetry | Prohibit domain/read-model selection traces, selection-trace identities, and payload-bearing diagnostics regardless of in-process, exported, or platform retention. Permit ordinary operational request/span identifiers and logs/traces only through existing redaction, with no candidate identity before disclosure is authorized. Safe-denial comparison normalizes nondeterministic request/span IDs, timestamps, and duration while requiring the accepted response/timing oracle, identical semantic events, levels, error classes, and leakage results. |
| Index completeness and ingestion | Treat zero candidates as authoritative only when the approved partition completeness marker, checkpoint, owner watermark, and rebuild state prove a fully initialized/current index; otherwise required trust evidence is `Unavailable`. Backfill uses approved bounded pages and a resumable cursor, fences concurrent runs, and orders its snapshot watermark with buffered/live owner changes so older data cannot overwrite newer evidence. Checkpoint and item writes are atomic. Tombstone retention, watermark-safe compaction, and re-creation semantics follow pinned bounds and are covered at retention boundaries. |
| Failure mapping | Before authority is proven, use only canonical safe denial. After authority, use the accepted AD-32/`QueryResult` mapping: required-evidence failure is `Unavailable`, optional-evidence failure is explicit `Partial`, transport cancellation propagates, and unexpected infrastructure faults use the accepted non-leaking retryable failure without a fabricated context snapshot. |
| Verification provenance | Before implementation, `prerequisite_record` must name the operator-created canonical record and that path must also be in `context`. The approved G-4 runner loads it and rejects any root/submodule revision or content-hash mismatch. Run evidence from a clean committed implementation checkout; after the runner, allow only its named evidence artifacts; commit those reviewed artifacts; finalization requires a clean checkout. Record implementation/evidence commits, `baseline_revision`, root and submodule revisions, prerequisite and manifest/profile revisions, commands, and artifact hashes. |

## Verification

**Commands:**
- `python3 tools/planning/validate_production_authority.py --story-id 6.3` -- expected: Story 6.3 remains within production authority.
- `git rev-parse HEAD` plus `git rev-parse 5e7ac08bd6faa623cf4005f8449b786dbce07c2a^{commit}` and `git submodule status -- references/Hexalith.EventStore references/Hexalith.Conversations references/Hexalith.Folders references/Hexalith.Memories` -- expected: this inspection records current values; the approved G-4 runner later loads `prerequisite_record` and fails on any exact revision/content-hash mismatch rather than relying on printed values.
- `git status --porcelain=v2 --untracked-files=all` -- expected by phase: current operator handoff shows only this reviewed spec; post-gate evidence starts from an empty committed implementation checkout, ends with only named evidence artifacts, and finalization is empty after their reviewed commit.
- `dotnet restore Hexalith.Projects.slnx` -- expected: restore succeeds with central dependency ownership.
- `dotnet build Hexalith.Projects.slnx --configuration Debug` -- expected: zero warnings and errors.
- `dotnet test tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj --configuration Debug` -- expected: additive supported and legacy contracts pass.
- `dotnet test tests/Hexalith.Projects.Tests/Hexalith.Projects.Tests.csproj --configuration Debug` -- expected: policy, projection, handler, replay, leakage, bounded-scale, and zero-write cases pass.
- `dotnet test tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Debug` -- expected: supported composition, safe denial, shadow, and legacy regressions pass.
- `dotnet test tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj --configuration Debug` -- expected: persisted gateway, restart, fault, and two-instance scenarios pass; after G-4 acceptance, an unavailable environment is not passing evidence.
- `dotnet tool run hexalith-module test --profile reads --filter Story=6.3` -- expected: approved G-4 runner emits passing bound `evidence/epic6/6.3-project-context.{trx,json}` and NoPayloadLeakage evidence, rejects a tampered prerequisite revision/hash, starts from the committed-clean implementation state, and allows only named evidence artifacts afterward.
- `git diff --check` -- expected: no whitespace errors.

## Auto Run Result

Status: awaiting-operator

Summary: Completed the agent-safe Story 6.3 deliverable: a reviewed post-gate implementation specification for supported Get, Refresh, and Explain context reads. Source implementation correctly did not begin because every listed prerequisite is conjunctive and unresolved.

Files changed:
- `_bmad-output/implementation-artifacts/spec-6-3-retrieve-assembled-project-context-through-supported-read-models.md` -- records the intent, edge matrix, supported code/test map, post-gate implementation invariants, verification plan, review repairs, and operator handoff.

Review findings: four passes completed. The final pass applied 6 patches (high 2, medium 4, low 0), deferred 0 items, and rejected 20 findings as transient workflow-state observations, already-covered constraints, or requests to invent operator-owned contracts. Follow-up review recommendation is `true` because the pass contained high-severity patches; the medium/low score is `3 × 4 + 0 = 12`.

Verification performed:
- Production-authority guard passed for Story 6.3.
- Root and Conversations/EventStore/Folders/Memories revisions were recorded against baseline `5e7ac08bd6faa623cf4005f8449b786dbce07c2a`.
- Restore passed; Debug build passed with 0 warnings and 0 errors.
- Contracts 188/188, domain 656/656, server 582/582, and integration 20/20 tests passed (1,446 total, 0 skipped).
- YAML frontmatter parsed with one `deferred` list and 11 non-empty imperative `operator_actions`; `git diff --check` passed.
- `sprint-status.yaml` has no diff.
- Canonical `dotnet tool run hexalith-module test --profile reads --filter Story=6.3` did not run because the manifest has no `hexalith-module` command; this is the recorded G-4 operator prerequisite, not passing Story 6.3 evidence.
- Matrix audit cannot pass before the supported implementation and G-4 profile exist; existing suite health is not represented as Story 6.3 acceptance verification.

Residual risk: no Story 6.3 runtime behavior, shadow equivalence, zero-write proof, persisted evidence, or NoPayloadLeakage artifact exists yet. Those claims remain unverified until all `operator_actions` are completed and the post-gate plan is implemented from the accepted prerequisite record.
