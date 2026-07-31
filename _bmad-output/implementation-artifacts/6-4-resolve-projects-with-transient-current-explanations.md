---
story_id: 6.4
story_key: 6-4-resolve-projects-with-transient-current-explanations
epic: "Epic 6: Authorized Project Reads on the Supported Platform"
created: 2026-07-31
source_story_status: backlog
status: blocked
blocked_by: [6.1-P0, 6.1-P1, 6.1-P2, 6.1-P3, 6.1-P4, G-2-Conversations, G-2-Folders, G-6]
baseline_commit: 8f070243998f535be224b796c747269e4a42ce13
traceability:
  requirements: [fr-12, fr-13, fr-17]
  nfrs: [nfr-1, nfr-8]
  architecture: [AD-7, AD-10, AD-11, AD-14, AD-32]
  journey: UJ-3
repository_authority: "Hexalith.Projects contracts, current-resolution policy, supported query handlers, and metadata-only composition over owner-approved read evidence"
owners: [Product Owner, Solution Architect, Test Architect]
entry_gate: "The exact-current Epic 6 shared baseline, applicable Conversations/Folders G-2 contracts, G-4 evidence runner, and affected G-6 runtime tuple must be accepted before implementation begins"
release_disposition: "Compute-only read story; Stories 7.11/7.12 own durable confirmation/attachment, Story 6.7 owns read cutover, and Story 8.11 owns terminal release acceptance"
estimate: L
---

# Story 6.4: Resolve Projects with Transient Current Explanations

Status: blocked

<!-- The 2026-07-17 READY verdict authorizes Story 6.x planning. It does not waive the Epic 6 shared gate, applicable sibling-owner gates, or the missing G-4 evidence runner. -->

## Story

As a delegated Chatbot service caller,
I want to resolve Candidate Projects from a Conversation's metadata and from attached Folder/File references, with a request-scoped, current-only explanation,
so that Chatbot can identify the right Project (FR-12, FR-13) without persisted inference history and without silently attaching.

## Acceptance Criteria

1. **Supported, current, metadata-only resolution.** Given a Conversation with no explicit Project, when `ResolveProjectFromConversation` or `ResolveProjectFromAttachments` executes through an authenticated `IDomainQueryHandler`, then it recomputes from the current Tenant-scoped, owner-authorized Project, Conversation, Folder, File, and Reference Trust Index evidence and returns exactly `NoMatch`, `SingleCandidate`, or `MultipleCandidates` with canonical reason codes. It reads metadata only, excludes pre-activation and non-read-model-confirmed Projects, excludes Archived Projects by default, and includes an Archived Project only when the caller explicitly requests that option and current authorization still permits disclosure.
2. **AD-32 snapshot and transient explanation.** Given an authorized resolution result, then it contains the logical AD-32 fields `responseState`, `asOf`, authorized `projectVersion` when disclosable, `resolutionResult`, metadata-only `components`, and `recoveryActions`. Components use `Included|Excluded` and `Current|Stale|Rebuilding|Unavailable`, a safe reason, and last-verified time. Candidate reasons use only `ConversationLinked|ProjectFolderMatched|FileReferenceMatched|MemoryMatched|MetadataMatched`. The explanation reflects only the current request and evidence snapshot; it has no durable trace identifier and is neither persisted nor reconstructable as inference history.
3. **Fail-closed authorization and freshness.** Given missing, denied, stale, rebuilding, unavailable, cross-Tenant, or unverifiable authority for a candidate or attachment, when resolution runs, then that evidence cannot score and no protected candidate identifier, name, owner denial detail, or resource-existence signal is exposed. A whole-query denial or indistinguishable absent protected resource maps to the supported safe `404`. An already-authorized query with missing or non-current required evidence returns honest `Unavailable`; `Partial` is usable only when the required Project, Folder, Setup, and first-response authorization evidence are current and every safe optional omission is explicit. Raw upstream failures are normalized to metadata-only states.
4. **No selection, attachment, or persistence.** Given any `SingleCandidate` or `MultipleCandidates` result, then no candidate is selected, preselected, linked, attached, confirmed, or written. The query performs no event append, Project/domain write, task or Confirmation Artifact creation, maintenance audit, sibling mutation, read-model repair, inference/history write, or Resolution Trace persistence. Platform-owned metadata-only security-denial audit remains permitted and operational telemetry stays nonpersistent. Stories 7.11 and 7.12 own any later durable confirmation and attach/create behavior.
5. **Deterministic policy and bounded inputs.** Given the same authorized evidence snapshot, then repeated execution and every input permutation produce the same candidates, exclusions, reasons, scores, and score-descending/Project-ID-ordinal order. Each distinct included reason contributes its existing weight once (`50/45/35/30/20`), the minimum score remains `20`, duplicates cannot amplify a candidate, and two or more qualifying Projects remain `MultipleCandidates`. Attachment identifiers are canonical, bounded by the approved contract, and never cause content, path, URI, workspace, provider, prompt, transcript, secret, or token retrieval.
6. **Additive compatibility and shadow safety.** Given the legacy resolution routes and supported query path coexist, then Story 6.4 adds the supported path without replacing the default/public route, extending the legacy Dapr projection journal, hand-editing generated artifacts, or changing legacy semantics. A canonical shadow comparator may normalize only the finite, owner-approved legacy-to-AD-32 differences; all other deltas fail. Story 6.7 exclusively owns reversible read-routing cutover and legacy retirement.
7. **Persisted-boundary evidence.** Given the entry gates are accepted and Story 6.4 is complete, when the approved G-4 reads profile runs through the authenticated gateway, platform `POST /query` dispatch, current read stores, and both resolution handlers, then unit, contract, authorization, leakage, current/stale/rebuild/fault, restart/two-instance, shadow, and zero-write scenarios pass. The run produces actual `evidence/epic6/6.4-resolution-reads.trx` and `evidence/epic6/6.4-resolution-reads.json`, including persisted before/after proof that no resolution trace, candidate choice, task, event, or domain/read-model mutation was created.

## Tasks / Subtasks

**Non-implementation entry condition.** Do not start implementation until 6.1-P0 through 6.1-P4 are accepted against the exact current repository/package revisions, the applicable Conversations and Folders G-2 read contracts are pinned with authorization/freshness semantics, the G-4 runner and module manifest can execute the required profile, the affected G-6 runtime/toolchain tuple is approved, and this story passes ready-for-development review. The implementing agent cannot waive or self-approve these conditions. The absence of Story 6.2/6.3 files is not permission to invent their promised shared contracts.

- [ ] Freeze the callable contracts and evidence boundary (AC: 1, 2, 3, 5, 6)
  - [ ] Record the exact EventStore, Builds, Conversations, and Folders revisions; current central package tuple; supported query/store/identity API signatures; owner read batch limits; freshness/watermark meanings; safe-denial behavior; normalization rules; and rollback pins in the accepted Epic 6 gate evidence.
  - [ ] Reconcile the previously accepted EventStore `3.70.1` planning record with the current checkout's centrally pinned `3.86.0` and exact gitlinks. Do not copy stale Architecture Spine package prose or silently upgrade/downgrade a dependency in this story.
  - [ ] Confirm the shared AD-32 response/component/recovery types and current actor-scoped Reference Trust Index seam delivered by the accepted Epic 6 work. Reuse them; do not create parallel vocabulary, a custom owner protocol, or a second trust store.
  - [ ] Freeze finite input bounds and canonicalization for Conversation, Folder, and File identities, including duplicate handling and `includeArchived`. Explicit archive inclusion never bypasses authorization or current read-model confirmation.

- [ ] Add versioned supported query contracts (AC: 1, 2, 4, 5, 6)
  - [ ] Add one public C# type per same-named file under `src/Hexalith.Projects.Contracts/Queries/` for `ResolveProjectFromConversationQuery`, `ResolveProjectFromAttachmentsQuery`, and only those resolution response types not already supplied by the accepted shared AD-32 contract.
  - [ ] Follow `IQueryContract` conventions for stable domain/query/projection names. The payload may carry resource identifiers and explicit query options, but never Tenant, original actor, authenticated workload, delegation authority, permission, or trusted timestamps.
  - [ ] Keep candidate outputs explicitly unselected. Separate candidate-facing reason evidence from operational diagnostics so safe candidate metadata can be returned without leaking authorization failures, raw owner problems, or unconfirmed protected detail.
  - [ ] Keep legacy `ProjectResolution`, OpenAPI YAML, generated `.g.cs` clients, and existing public route DTOs unchanged unless the approved generator workflow requires an additive change. Stop for architecture disposition rather than hand-mapping a duplicate route or editing generated output.

- [ ] Compose current authorized Conversation resolution (AC: 1, 2, 3, 4, 5)
  - [ ] Add `ResolveProjectFromConversationQueryHandler` under `src/Hexalith.Projects/Queries/Handlers/` and dispatch it only through the supported DomainService query seam established by Story 6.1.
  - [ ] Read current Conversation metadata and membership evidence through the pinned Conversations owner contract. Conversations remains the sole authority for Conversation-to-Project membership; never copy membership into the Project aggregate or load a transcript/message body.
  - [ ] Reauthorize the immutable dual-principal query envelope—Tenant, original actor, authenticated workload, delegation, audience, scope/routing metadata, and `eventstore:permission` authorization input—before protected validation or disclosure. Mere scope is not permission.
  - [ ] Convert only authorized, current metadata to existing `ConversationResolutionEvidenceMapper` inputs, enumerate only current authorized Project candidates through the accepted Project read model, and run the existing `ProjectResolutionEngine` once per request.
  - [ ] Normalize candidate-local missing/denied/stale evidence without exposing its protected identity. If mandatory evidence cannot establish a safe result, return `Unavailable` rather than fabricating `NoMatch` or `Complete`.

- [ ] Compose current authorized attachment resolution (AC: 1, 2, 3, 4, 5)
  - [ ] Add `ResolveProjectFromAttachmentsQueryHandler` beside the Conversation handler and use the same snapshot composer/policy vocabulary where shared behavior is exact.
  - [ ] Resolve only canonical Folder/File identities and metadata through the pinned Folders owner batch-read contract and the current actor-scoped Reference Trust Index. Folders owns resource existence, payload, lifecycle, and authorization; Projects owns only metadata references and resolution policy.
  - [ ] Treat legacy `IProjectReferenceIndexReadModel` and `DaprProjectReferenceIndexReadModel` as shadow inputs, not current owner authorization. Do not repeat the legacy behavior that marks every presented attachment `Included` without current owner verification.
  - [ ] Convert only currently authorized reference evidence through `AttachmentResolutionEvidenceMapper`; exclude non-current evidence before scoring and prevent an excluded record from carrying a protected Project identity/display name into the supported response.
  - [ ] Enforce the approved combined input limit before expensive owner work, deduplicate canonically, use owner-supported safe batches, propagate cancellation, and never fan out into unbounded per-reference calls.

- [ ] Preserve and harden the pure deterministic resolution boundary (AC: 1, 2, 4, 5)
  - [ ] Reuse `ProjectResolutionEngine`, `ConversationResolutionEvidenceMapper`, `AttachmentResolutionEvidenceMapper`, `ProjectResolutionContext`, and `ProjectResolutionScoringRules`; do not duplicate scoring in handlers or move sibling I/O into the engine.
  - [ ] Keep scoring weights, threshold, distinct-reason behavior, archive policy, and deterministic ordering unchanged unless an explicit product/architecture amendment changes the canonical heuristic.
  - [ ] Add a Projects-owned pure snapshot composer only if the accepted AD-32 types do not already provide one. It may map already-authorized evidence to response state/components/recovery actions, but may not call a store, owner service, clock, telemetry sink, or writer.
  - [ ] If a currently multi-public-type source file must be changed, split its public types into same-named files in the same change and keep serialization compatibility. Do not expand the existing one-type-per-file violations.

- [ ] Register the supported path without owning platform migration or cutover (AC: 1, 3, 6)
  - [ ] Rely on platform handler discovery where supported. Update `ProjectsServiceCollectionExtensions` only for a Projects-owned composer/policy that is not auto-discovered, preserving every existing registration.
  - [ ] Update `Hexalith.Projects.csproj` only if the accepted 6.1 baseline deliberately requires a centrally versioned EventStore reference not already present. A missing supported host/query seam is prerequisite drift, not scope to absorb silently in 6.4.
  - [ ] Keep `ResolveProjectFromConversationEndpoint`, `ResolveProjectFromAttachmentsEndpoint`, `ProjectsDomainServiceEndpoints`, legacy ACL/client adapters, reverse-index Dapr adapters, Server `Program.cs`, and default route selection unchanged for comparison through Story 6.7.
  - [ ] Add reusable shadow comparison only under the accepted `Hexalith.Projects.Testing/Reads/` and G-4 integration conventions; production handlers must not branch between legacy and supported runtimes.

- [ ] Add focused, production-chain, and persisted-boundary verification (AC: all)
  - [ ] Extend existing Tier-1 engine/mapper tests for exact outcomes/reasons/ranking, archive default and explicit opt-in, threshold boundaries, duplicate signals/attachments, input permutations, malformed IDs, fixed evidence time, cancellation, and no candidate selection.
  - [ ] Add query-contract serialization/golden tests and handler tests for `Complete|Partial|Unavailable`, all component inclusion/freshness states, exact recovery codes, authorized/disclosable `projectVersion`, and current `asOf` derived from the recomputation snapshot rather than an invented persisted trace.
  - [ ] Add the actor × workload × Tenant × delegation/audience/scope/permission matrix. Cover missing/invalid authentication, permission-vs-scope, cross-Tenant, absent/denied equivalence, authorization before protected validation/idempotency handling, stale authorization, disabled Tenant, and unchanged bearer forwarding at the supported gateway.
  - [ ] Add leakage tests over responses, errors, logs, telemetry, and serialized contracts. Forbid transcript/message/prompt text, file bytes/content/path/URI, owner/provider/workspace detail, raw upstream problems, unconfirmed protected candidate detail, Tenant/actor authority data, secrets, and tokens.
  - [ ] Add real supported projection/store/owner-adapter chain cases, including unknown/relevant event handling, stale watermark/provenance, rebuilding/unavailable store, duplicate dispatch, full rebuild, restart, two instances, and owner batch fault. Handler mocks alone do not count.
  - [ ] Implement scenarios `E6.4-U01` exact deterministic outcomes, `E6.4-U02` archive policy, `E6.4-A01` selects and persists nothing, and `E6.4-A02` safe denial/degradation from the Epic 6 test design.
  - [ ] Snapshot persisted end state before and after both queries and assert no Project events, commands, tasks, confirmations, maintenance audit, resolution trace, selected candidate, sibling mutation, or read-model write. Separately allow and validate only platform-owned metadata-only authorization-denial audit.
  - [ ] Run `dotnet restore Hexalith.Projects.slnx`, `dotnet build Hexalith.Projects.slnx --configuration Debug`, and the affected test projects individually. Then run `dotnet tool run hexalith-module test --profile reads --filter Story=6.4` and retain the generated TRX/JSON evidence. Do not fabricate evidence while `.config/dotnet-tools.json` or `module/hexalith-projects.module.json` is absent.
  - [ ] Run `git diff --check`, contract/generator fingerprints when applicable, and the Story 6.4 architecture/conformance checks. New/materially changed Epic 6 read code must satisfy the approved coverage threshold; Story 8.9 retains the terminal NFR-5 load/p95 gate.

## Dev Notes

### Authority, Readiness, and Scope

- `epics.md` is the exact Story 6.4 scope. The final Architecture Spine is normative; the root `architecture.md` is explicitly superseded and is historical evidence only.
- The 2026-07-17 implementation-readiness report is `READY` and authorizes creation of 6.x story files. Its own verdict says no story starts before applicable external and epic entry gates are pinned and approved. The conformance checklist remains useful for row-level disposition but cannot override that dated verdict.
- FR-12 and FR-13 matrix rows bind Story 6.4 to the Epic 6 gate and G-2. FR-17's current-only explanation semantics also bind this story. Record a finite conformance disposition for `fr-12`, `fr-13`, and the 6.4 share of `fr-17` before promotion to `ready-for-dev`.
- Stories 6.2 and 6.3 are not formal forward dependencies and no story files currently exist for them. Story 6.4 must reuse any accepted shared AD-32/read-index seams when they land, but it can complete independently once its own required current read evidence exists. Do not claim predecessor implementation intelligence that does not exist.
- Stories 7.11/7.12 are scope boundaries, not dependencies: 6.4 returns unselected candidates only. Story 6.5 owns Web presentation, 6.6 CLI presentation, 8.5 MCP, 6.7 route cutover, 8.9 release performance evidence, and 8.11 terminal release acceptance.

### Binding Resolution Semantics

- The existing pure engine is the policy authority: included evidence only; weights Conversation `50`, Project Folder `45`, File Reference `35`, Memory `30`, Metadata `20`; minimum `20`; one contribution per distinct reason; deterministic score-descending then Project-ID-ordinal ordering. `SingleCandidate` still selects nothing and two or more qualifying candidates stay ambiguous.
- `includeArchived` is an explicit query option, not authorization. Archived candidates remain subject to current Project/lifecycle, Tenant, and actor/workload authorization evidence. Pre-activation tasks can never become candidates.
- A candidate-facing explanation can contain only currently authorized candidate metadata and canonical reasons. Operational diagnostics describe safe state/freshness/recovery without exposing a candidate that failed authorization. FR-17 prohibits secrets, payloads, prompts, unrestricted paths, raw upstream problems, and unconfirmed-candidate detail.
- `Complete` requires all required evidence current. `Partial` is usable only under the AD-32 minimum-current rule with every optional omission explicit. `Unavailable` blocks use. `Denied` discloses no protected detail. Recovery codes are exactly `None|Retry|RefreshContext|RequestPreview|RenewPreview|PollTask|ResolveNeedsAttention|SelectAlternative|ContactAdministrator`.
- The query may read persisted models, but resolution and explanation are request-scoped recomputations. No candidate score, trace, diagnostic snapshot, or inference history is persisted. Only later confirmed outcomes may enter durable audit history.

### Current Code and Regression Guardrails

- `ProjectResolutionEngine` is already pure, deterministic, clock-free, and I/O-free. Preserve it. Its exclusion shape can carry Project identity/display name, so supported handlers must supply only authorized/disclosable candidates or redact before response mapping.
- `ConversationResolutionEvidenceMapper` and `AttachmentResolutionEvidenceMapper` translate pre-fetched metadata into evidence; they do not own authorization, reads, ranking, or persistence. Keep owner clients and stores in handler composition.
- Legacy `ResolveProjectFromConversationEndpoint` uses live Conversations ACL plus legacy Project list evidence and emits an eventually-consistent shape. Preserve it for a finite shadow comparison; explicitly normalize only approved `NoMatch` versus AD-32 degradation differences.
- Legacy `ResolveProjectFromAttachmentsEndpoint` uses the Projects reverse index and currently marks presented inputs included without current owner authorization. It must remain a regression comparator, never the supported trust source.
- The legacy Projects reverse-index projection represents Projects event/membership evidence, not current Folders owner existence/authorization. The supported path consumes the actor-scoped current Reference Trust Index and owner evidence; it must not extend the custom Dapr projection journal.
- `ProjectResolution.cs` currently contains the legacy response plus multiple public types and lacks AD-32 fields. Prefer a new supported response wrapper and keep the wire-compatible legacy shape intact. If it must be edited, split the public types at the same time.
- `ProjectsServiceCollectionExtensions` already registers `ProjectResolutionEngine`. `Hexalith.Projects.csproj` currently has no EventStore DomainService reference, and the Server is still a bespoke host; those are shared 6.1 baseline concerns, not implicit work for 6.4.

### Platform, Security, and Persistence Rules

- Supported query handlers implement `IDomainQueryHandler` and read only through accepted `IReadModelStore`/batch models produced by named `IAsyncDomainProjectionHandler` implementations and `ReadModelWritePolicy`. `IDomainProjectionHandler` remains replay compatibility only. Never call Dapr state APIs directly or introduce a custom query switch/runtime.
- The immutable `QueryEnvelope` is the authority source: server-derived Tenant, original actor, authenticated workload, delegation, scopes, audience, and permission context. Reauthorize at Projects and each owner. Never trust payload/header substitutes for Tenant or actor.
- AD-19 maps authorized computation to `200` with `Complete|Partial|Unavailable`; protected denial/nonexistence collapse to an indistinguishable safe `404`. Validation ordering must not become an existence oracle.
- Conversations owns Conversation membership. Folders owns Folder/File existence, payload, lifecycle, and authorization. Projects stores stable metadata references and owns resolution policy; it never reads foreign content or mutates a sibling in this story.
- Resolution traces are operational telemetry only. Platform metadata-only durable security audit for a denial is distinct and remains allowed under AD-26. Logs/traces must be bounded, scrubbed, and correlation-safe.

### Current Baseline and Technology Notes

- Baseline commit is `8f070243998f535be224b796c747269e4a42ce13`. `global.json` pins SDK `10.0.302` with latest-patch roll-forward; repository builds target `net10.0`, nullable and implicit usings are enabled, warnings are errors, and language version is latest. Do not change these settings for Story 6.4.
- Current Builds central pins include EventStore `3.86.0`, FrontComposer `4.0.1`, Aspire `13.4.6`, Dapr .NET `1.18.5`, CommunityToolkit Aspire Dapr `13.4.1-beta.686`, OpenTelemetry `1.17.0`, NSwag `14.7.1`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, and NSubstitute `6.0.0`. Central versioning is mandatory; no inline versions.
- Current CI/runtime evidence still names Dapr runtime `1.18.0` while the central .NET SDK package is `1.18.5`; this is an exact-current G-6 reconciliation item. The Architecture Spine's older EventStore/Dapr/NSubstitute values are not permission to downgrade.
- Official .NET support policy lists .NET 10 as active LTS through November 14, 2028. Official Aspire 13.4 guidance is compatible with the pinned family, but the repository's exact patch remains authoritative. Official Dapr access-control guidance reinforces service-to-service allowlists; it does not replace application-level dual-principal reauthorization.

### Recent Git and Previous-Story Intelligence

- The four latest root commits are dependency gitlink updates. They advanced Builds/EventStore/Conversations APIs without implementing Story 6.4; inspect current source and exact pins instead of copying stale story signatures.
- The latest root source commit, `6b72d8a`, hardens authentication/claims transformation. Preserve its `ValidateOnStart`, authenticated-identity-only, production HTTPS/OIDC fail-closed, invalid audience/expiry, permission-versus-scope, cross-Tenant safe-denial, and bearer-forwarding test patterns.
- Story 6.1 is the highest existing Epic 6 implementation story below 6.4 and remains blocked. Reuse its supported-host, immutable identity, AD-32, safe-denial, shadow, and persisted-evidence guardrails after acceptance; do not infer completion from its draft tasks.
- Historical Stories 4.2/4.3 established useful regression behavior and the pure resolution engine, but their legacy adapters are not the supported target. Prior review found that stub-only endpoint tests missed real projection-chain regressions; Story 6.4 therefore requires production mapper/store/projection/owner-adapter execution and persisted end-state assertions.

### Recommended File Map

**New after the gates define exact shared names:**

- `src/Hexalith.Projects.Contracts/Queries/ResolveProjectFromConversationQuery.cs`
- `src/Hexalith.Projects.Contracts/Queries/ResolveProjectFromAttachmentsQuery.cs`
- One-file-per-type supported resolution snapshot/component/recovery contracts only where accepted shared AD-32 contracts are insufficient.
- `src/Hexalith.Projects/Queries/Handlers/ResolveProjectFromConversationQueryHandler.cs`
- `src/Hexalith.Projects/Queries/Handlers/ResolveProjectFromAttachmentsQueryHandler.cs`
- Optionally `src/Hexalith.Projects/Resolution/ProjectResolutionSnapshotComposer.cs` as a pure shared mapper.
- Contract/serialization tests and handler tests under the matching `tests/Hexalith.Projects.Contracts.Tests/` and `tests/Hexalith.Projects.Tests/Queries/` conventions.
- Persisted/gateway Story 6.4 scenarios and a reusable zero-write/shadow fixture under the accepted G-4 and `Hexalith.Projects.Testing/Reads/` conventions.

**Update only when required by accepted prerequisites:**

- `src/Hexalith.Projects/ProjectsServiceCollectionExtensions.cs` for a Projects-owned helper not auto-discovered.
- `src/Hexalith.Projects/Hexalith.Projects.csproj` for an accepted centrally versioned supported-query dependency.
- The delivered Reference Trust Index read contract only additively if its approved contract lacks required resolution metadata; stop rather than inventing an owner protocol.

**Expected unchanged:**

- `ProjectResolutionEngine.cs`, both evidence mappers, legacy Server resolution endpoints and ACL/client adapters, legacy Dapr/reference-index projections, `ProjectsDomainServiceEndpoints.cs`, the default Server route/host composition, OpenAPI YAML, generated clients, UI, CLI, MCP, aggregate events, and unrelated projections.

### Verification and Evidence Contract

- Required command after the gates exist: `dotnet tool run hexalith-module test --profile reads --filter Story=6.4`.
- Required evidence: `evidence/epic6/6.4-resolution-reads.trx` and `evidence/epic6/6.4-resolution-reads.json`. The epics require both; retain both even where the current traceability row abbreviates the artifact list.
- Evidence must identify baseline revisions, fixture, immutable caller/delegation, query kind/options, current-evidence watermarks, result/reasons, response/component states, recovery actions, zero-write before/after comparison, and actual pass/fail. It must not contain protected identifiers from denied evidence.
- G-4 completion cannot be claimed in this checkout: `.config/dotnet-tools.json` and `module/hexalith-projects.module.json` are absent. Record the exact blocker; never hand-author a passing TRX/JSON result.
- NFR-5 still informs bounded batch/fan-out and measurement design, but Story 8.9 owns the terminal 10k-project, 500-reference, p95/max release-performance proof. Do not overclaim release performance from unit timings.

### Hard Stops

- Stop if the exact-current 6.1-P0 through P4 baseline, G-2 Conversations/Folders semantics, or required G-4/G-6 evidence capability is unaccepted or ambiguous.
- Stop if original actor and authenticated workload cannot both be reauthorized, if scope is treated as permission, or if gateway/owner denial can disclose a protected resource differently from safe `404`.
- Stop if a stale/unavailable owner record can score, an unauthorized candidate identity/name can enter a response, or raw Conversation/File content is required.
- Stop if implementation requires a direct Dapr state call, custom journal/query runtime, unbounded owner fan-out, fabricated timestamp/watermark, duplicate scoring policy, or hand-edited generated artifact.
- Stop if any resolution selects or persists a candidate/trace, emits a Project event, creates a task/confirmation, mutates a sibling, repairs a read model, or changes default routing/legacy retirement.
- Stop if actual persisted evidence cannot be generated. Report the command and missing capability instead of substituting mocks or historical green counts.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Epic-6-Authorized-Project-Reads-on-the-Supported-Platform]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-6.4-Resolve-Projects-with-transient-current-explanations]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md#5-Observable-Context-and-Recovery-Contract]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md#Functional-Requirements]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md#AD-7-Diagnostics-are-current-bounded-and-authority-neutral]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md#AD-10-Conversations-owns-Conversation-to-Project-membership]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md#AD-11-Projects-owns-references-not-foreign-resources-or-authority]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md#AD-14-Query-trust-is-incremental-and-rebuildable]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md#AD-32-One-response-snapshot-governs-context-usability]
- [Source: _bmad-output/planning-artifacts/implementation-readiness-report-2026-07-17.md#Readiness-Determination]
- [Source: _bmad-output/planning-artifacts/implementation-readiness-traceability-matrix.yaml#fr-12-fr-13-and-fr-17]
- [Source: _bmad-output/test-artifacts/test-design-epic-6.md#Story-6.4]
- [Source: docs/resolution-scoring-heuristic.md#Resolution-Scoring-Heuristic]
- [Source: references/Hexalith.AI.Tools/hexalith-state-instructions.md]
- [Official .NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy)
- [Official Aspire 13.4 release notes](https://aspire.dev/whats-new/aspire-13-4/)
- [Official Dapr service invocation access control](https://docs.dapr.io/operations/configuration/invoke-allowlist/)

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Story-context analysis performed against baseline `8f070243998f535be224b796c747269e4a42ce13` on 2026-07-31.
- Current implementation and platform API inventory performed read-only; no dependency or submodule revisions changed.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story remains blocked because planning-level READY does not satisfy the exact-current Epic 6 shared gate, applicable G-2 owner contracts, missing G-4 evidence tooling, or G-6 tuple disposition.
- Requirements, Architecture Spine, current resolution/read code, Git history, prior story intelligence, current package pins, official technology guidance, and the Epic 6 test design were reconciled into implementation guardrails.

### File List

- `_bmad-output/implementation-artifacts/6-4-resolve-projects-with-transient-current-explanations.md` (new)

## Change Log

- 2026-07-31: Created the comprehensive Story 6.4 implementation guide at `blocked`, pending accepted shared and external entry gates.
