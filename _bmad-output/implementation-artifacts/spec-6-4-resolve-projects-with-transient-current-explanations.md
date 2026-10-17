---
title: 'Story 6.4: Transient current Project resolution'
type: 'feature'
created: '2026-10-17'
status: 'draft'
route: 'dispatch'
story_key: '6-4-resolve-projects-with-transient-current-explanations'
baseline_commit: '71c326813e75e7b3486555b5f22ed377b42064d6'
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

</frozen-after-approval>

## Code Map

- `src/Hexalith.Projects/Resolution/` — engine/mappers; exclusions retain IDs/names, requiring authorization before evaluation.
- `src/Hexalith.Projects.Contracts/Queries/` — shared AdmissionSnapshot, components and recovery.
- `src/Hexalith.Projects.Server/Queries/` — principal binding/authorization; ProjectReadQueryExecutor writes audit and cannot serve resolution.
- `src/Hexalith.Projects.Server/Projections/ProjectReads/` — persisted Project index/detail; no RTI exists.
- `src/Hexalith.Projects.Server/Conversations/` — resolution adapter retrieves full detail including Messages; unsuitable.
- `src/Hexalith.Projects.Server/Folders/` — opaque File refresh is Unavailable; legacy metadata needs paths. Delegated Folder read supplies singleton authorization only.

## Tasks & Acceptance

**Execution, after the applicable prerequisite gates:**

- [ ] `src/Hexalith.Projects.Contracts/Queries/` — add ResolveProjectFromConversationQuery, ResolveProjectFromAttachmentsQuery and ProjectResolutionReadResponse; one type per file, shared vocabulary.
- [ ] `src/Hexalith.Projects/Resolution/ProjectResolutionSnapshotComposer.cs` — map authorized evidence without I/O, clocks, writes or unsafe exclusions.
- [ ] `src/Hexalith.Projects.Server/Queries/` — add both corresponding QueryHandlers using approved contracts, bounded reads and authority rechecks.
- [ ] `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs` — register additively.
- [ ] `tests/Hexalith.Projects.Contracts.Tests/`, `tests/Hexalith.Projects.Tests/Resolution/`, `tests/Hexalith.Projects.Server.Tests/Queries/` — cover contracts, matrix, archive policy, leakage and cancellation.
- [ ] `tests/Hexalith.Projects.Integration.Tests/` — prove persisted zero writes, restart/two-instance behavior and finite shadow equivalence through G-4.

**Acceptance Criteria:**

- Given authenticated queries, when evaluated, then the matrix holds without selection or persisted inference.
- Given full completion, when G-4 runs, then actual `evidence/epic6/6.4-resolution-reads.trx` and `.json` prove persisted boundaries.

## Implementation Notes

- 2026-10-17: Prerequisites-first decision recorded. The first proposed work package is `spec-6-4-prerequisite-p1r-source-readiness.md`; its preflight evidence records five verified package signatures and 224 passing tooling controls without claiming P1R qualification. Story 6.4 remains a draft behind its original gates.

## Spec Change Log

## Review Triage Log

## Design Notes

Scope is resolved: prerequisites first. Irreversibles: none. Footprint after gate acceptance: contracts, composer, handlers, registration and tests. Preserve concurrent changes. P0/P1R/P2/P3/P4/readiness and current runtime qualification remain open; original story/tracker stay blocked. This draft cannot become ready-for-dev until approved callable owner/RTI contracts make its handler tasks actionable.

## Verification

- `python3 tools/planning/validate_production_authority.py --story-id 6.4` — passed; scheduling authority only.
- `dotnet tool run hexalith-module test --profile reads --filter Story=6.4` — exit 1: `Cannot find a tool in the manifest file that has a command named 'hexalith-module'.`
- After implementation: Debug solution restore/build, affected test projects individually, then G-4 qualification.
- `git diff --check` — require clean whitespace.
