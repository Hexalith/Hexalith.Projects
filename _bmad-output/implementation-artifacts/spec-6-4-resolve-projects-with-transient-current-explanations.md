---
title: 'Story 6.4: Transient current Project resolution'
type: 'feature'
created: '2026-10-17'
updated: '2026-10-19'
status: 'draft'
route: 'dispatch'
story_key: '6-4-resolve-projects-with-transient-current-explanations'
baseline_commit: '83781b31e06214ad6da788c6535f9044df06ed5e'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/epic-6-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Supported resolution lacks current owner-authorized metadata and AD-32 explanations.

**Approach:** Add Conversation and attachment resolution using supported evidence, existing scoring and shared snapshots after prerequisite approval. Candidates remain unselected; explanations remain transient.

**Decision (2026-10-17):** The user selected prerequisites first. Retain the original implementation gate; establish approved owner metadata/RTI contracts and exact-current prerequisite qualification before implementing Story 6.4. No local-foundation exception is authorized.

## Boundaries & Constraints

**Always:** Bind authenticated Tenant, actor, workload and delegation; reauthorize before disclosure. Score only current authorized evidence and confirmed Projects. Preserve weights 50/45/35/30/20, threshold 20, distinct reasons and deterministic ordering. Archived requires explicit opt-in and authorization.

**Never:** Retrieve content, messages, paths, URIs or secrets; invent owner/RTI contracts; trust the legacy reverse index; persist inference, selection, tasks, events or repairs; invoke inspection-auditing reads; change legacy routing/generated artifacts. Only platform-owned metadata security-denial audit is permitted.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Current | Coherent authorized evidence | NoMatch, SingleCandidate or MultipleCandidates; shared AD-32 snapshot | No automatic choice |
| Duplicates/permutations | Identical evidence in different order | Identical candidates, scores and reasons | Bounded canonical inputs |
| Protected | Denied, absent or cross-Tenant target | No protected identity or existence detail | Indistinguishable safe 404 |
| Degraded | Required evidence non-current | Minimal Unavailable; safe recovery | No fabricated NoMatch |
| Optional omission | Required evidence current; omission disclosure authorized | Explicit Partial | No protected exclusion identity |
| Query effects | Either resolution query | No domain/read-model/trace mutation | Persisted before/after proof |

**Decision (2026-10-18):** The user selected option 1, approving the separate exact five-file governed-retirement prerequisite and its bounded validator/lineage work. This does not approve Story 6.4 implementation or waive its remaining gates. The original proposal and explicit selection are retained under `evidence/6-4-resume-20261018/`.

**Decision (2026-10-18, option 2):** Target published 3.125.1 for the next finite-enrollment prerequisite. This selects planning scope only; exact execution selection, qualification and Story 6.4 implementation remain separately gated.

</frozen-after-approval>

## Open Questions

- Display names on a qualifying candidate — options: Return `DisplayName` only after inspection authorization and one `ProjectReadAudit` inspection record (callers with inspection rights see the engine's safe name; the query-effects proof allows that single platform audit write and no other mutation) / Omit `DisplayName` on both supported responses (no inspection audit; shadow comparison ignores names; a separate inspection read is required to see a name).

## Code Map

- `src/Hexalith.Projects/Resolution/ProjectResolutionEngine.cs` — reuse `Resolve`. Rank is score descending, then `ProjectId`. `SingleCandidate` does not select. Do not edit.
- `src/Hexalith.Projects/Resolution/ProjectResolutionScoringRules.cs` — weights 50/45/35/30/20 and threshold 20. Do not edit.
- `src/Hexalith.Projects/Resolution/ConversationResolutionEvidenceMapper.cs` and `AttachmentResolutionEvidenceMapper.cs` — reuse `Map`. They emit evidence only. Do not score in the mappers.
- `src/Hexalith.Projects.Contracts/Queries/AdmissionSnapshot.cs` plus `AdmissionResponseState`, `AdmissionComponent`, and `AdmissionRecoveryAction` — the shared AD-32 vocabulary. Reuse; do not fork.
- `src/Hexalith.Projects.Server/Queries/ProjectContextQueryExecutor.cs` — copy bind, two-pass `AuthorizeSupportedReadAsync`, and `safe-denial`. Do not fold resolution into this executor.
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs` — register new handlers beside lines 144–154 and advertise their types in `CreateOperationalIndexMetadata`.
- `src/Hexalith.Projects.Server/ProjectsServerModule.cs` — add two `.v1` constants beside the context query types. Do not change existing constants.
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` lines 216–252 and the legacy `ResolveProjectFrom*Endpoint` types — leave the GET routes and reverse-index read models unchanged.
- `tests/Hexalith.Projects.Tests/Resolution/ProjectResolutionPersistsNothingTests.cs` — engine zero-write proof. Extend handler coverage; do not weaken this test.
- No `ReferenceTrust*` types exist under `src/`, and `module/hexalith-projects.module.json` is absent. Do not add either in this story. Prior gate notes: `evidence/6-4-resume-20261018/preflight-c91c704.json`.

## Tasks & Acceptance

**Gate:** P1R 3.125.1 enrollment and immutable binding are done. They are not owner-metadata/RTI approval or a G-4 runner. Do not add product code, and do not edit `sprint-status.yaml`, until both exist.

**Execution:**
- [ ] `src/Hexalith.Projects.Contracts/Queries/` — add conversation and attachment resolution queries and responses, one type per file, each carrying one `AdmissionSnapshot` and the existing resolution result, with display-name disclosure left to the open question.
- [ ] `src/Hexalith.Projects/Resolution/ProjectResolutionSnapshotComposer.cs` — map current authorized owner/RTI evidence through the existing mappers into engine inputs.
- [ ] `src/Hexalith.Projects.Server/Queries/` — add one executor and both handlers: bind, reauthorize, then compose and `Resolve`. Safe denial before disclosure; minimal `Unavailable` when required evidence is non-current; `Partial` only for an authorized optional omission.
- [ ] `src/Hexalith.Projects.Server/ProjectsServerModule.cs` and `ProjectsServerServiceCollectionExtensions.cs` — register both handlers on the existing `/query` route.
- [ ] `tests/Hexalith.Projects.Tests/Resolution/` and `tests/Hexalith.Projects.Server.Tests/Queries/` — test the frozen matrix, including order independence, safe 404, and zero writes.
- [ ] `tests/Hexalith.Projects.Integration.Tests/` — once G-4 exists, prove persisted before/after equality and shadow equivalence with the legacy routes.

**Acceptance Criteria:**
- Given the gate is met and evidence is current and authorized, when either supported query runs, then the matrix holds and no candidate is selected.
- Given protected, degraded, or reordered input, when either query runs, then that matrix row holds and no reverse-index row is treated as authority.
- Given either query, when persisted state is compared, then no domain event, read model, trace, task, or repair was written.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

Handlers never read `IProjectReferenceIndexReadModel`. The composer accepts only evidence an approved owner/RTI contract marks current and authorized. Archived candidates stay out unless the query sets `IncludeArchived` and the gate allows that read. A disclosure-unsafe exclusion is omitted; non-current required evidence stays `Unavailable` and is never reported as `NoMatch`.

## Verification

**Commands:**
- `python3 tools/planning/validate_production_authority.py --story-id 6.4` — expected: Story 6.4 remains within production authority.
- `dotnet test tests/Hexalith.Projects.Tests/Hexalith.Projects.Tests.csproj --configuration Debug --filter FullyQualifiedName~Resolution` — expected: scoring, determinism, leakage, and zero-write tests pass.
- `dotnet test tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Debug` — expected: safe denial and both new handlers pass once they exist.
- `git diff --check` — expected: no whitespace errors.

**Manual checks (if no CLI):**
- Do not treat `dotnet tool run hexalith-module test --profile reads --filter Story=6.4` as runnable until `module/hexalith-projects.module.json` and the G-4 reads profile exist.
- Confirm `sprint-status.yaml` has no diff from this story.
