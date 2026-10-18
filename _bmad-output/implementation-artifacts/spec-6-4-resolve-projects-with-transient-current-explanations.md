---
title: 'Story 6.4: Transient current Project resolution'
type: 'feature'
created: '2026-10-17'
updated: '2026-10-18'
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

**Decision (2026-10-18):** The user selected option 1, approving the separate exact five-file governed-retirement prerequisite and its bounded validator/lineage work. This does not approve Story 6.4 implementation or waive its remaining gates. The original proposal and explicit selection are retained under `evidence/6-4-resume-20261018/`.

</frozen-after-approval>

## Code Map

- `src/Hexalith.Projects.Contracts/Queries/Admission*.cs` — reuse shared AD-32 vocabulary.
- `src/Hexalith.Projects/Resolution/` — reuse scoring; authorize before exclusions expose identities.
- `src/Hexalith.Projects.Server/Queries/ProjectReadQueryExecutor.cs` — writes inspection audit; unsuitable for resolution.
- `src/Hexalith.Projects.Server/Queries/ProjectContextQueryExecutor.cs` — explicitly lacks owner-backed RTI.
- `src/Hexalith.Projects.Server/Conversations/` — legacy resolution retrieves Messages.
- `src/Hexalith.Projects.Server/Folders/` — singleton Folder authorization lacks metadata/watermarks; opaque File recheck is Unavailable.
- `references/Hexalith.EventStore/docs/architecture/evidence-secret-scanning.md` — replacement/retirement policy; the preserved proposal records five verified hashes; the separately approved retirement prerequisite owns enrollment and authority withdrawal.

## Tasks & Acceptance

**Execution, after prerequisite acceptance:**

- [ ] `src/Hexalith.Projects.Contracts/Queries/` — add both resolution queries and response using shared vocabulary.
- [ ] `src/Hexalith.Projects/Resolution/ProjectResolutionSnapshotComposer.cs` — compose authorized snapshots without I/O or writes.
- [ ] `src/Hexalith.Projects.Server/Queries/` — add both handlers using approved bounded owner/RTI contracts.
- [ ] `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs` — register additively.
- [ ] `tests/Hexalith.Projects.Contracts.Tests/`, `tests/Hexalith.Projects.Tests/Resolution/`, `tests/Hexalith.Projects.Server.Tests/Queries/` — verify the frozen matrix.
- [ ] `tests/Hexalith.Projects.Integration.Tests/` — prove persisted zero writes, restart/two-instance behavior and shadow equivalence through G-4.

**Acceptance:** Given accepted entry gates, when both handlers execute, then the frozen matrix holds and actual `evidence/epic6/6.4-resolution-reads.trx` and `.json` prove persisted boundaries.

## Implementation Notes

- 2026-10-17: Prerequisites-first decision recorded. The first proposed work package is `spec-6-4-prerequisite-p1r-source-readiness.md`; its preflight evidence records five verified package signatures and 224 passing tooling controls without claiming P1R qualification. Story 6.4 remains a draft behind its original gates.

- 2026-10-18: Resumed planning at Projects `3d3b3fab63afe63090e6314e2c2b6e66bcd0ec31`; retained reviewed EventStore source hashes match checkout. Seven historical findings in five files remain; no fresh full scan is claimed. Independent source inspection confirms missing owner metadata/RTI contracts. Local 6.1–6.3 exceptions do not authorize 6.4. Retained command outputs and an unapproved exact-file proposal in the linked JSON; runtime, pins, acceptance and original story/tracker remain unchanged.

## Spec Change Log

## Review Triage Log

## Design Notes

The owner approved the separate five-file retirement prerequisite; its implementation and verification are tracked independently. Irreversibles: none in planning; publication remains separate. Footprint: this draft and planning JSON only. P0/current P1R/P2/P3/P4, G-2/RTI, G-4, current G-6 and independent readiness remain open. The spec cannot become ready-for-dev.

## Verification

- `python3 tools/planning/validate_production_authority.py --story-id 6.4` — passed; scheduling only.
- `dotnet tool run hexalith-module test --profile reads --filter Story=6.4` — exit 1: `Cannot find a tool in the manifest file that has a command named 'hexalith-module'.` Both runner manifests remain absent.
- `git diff --check`, CRLF, frozen-intent, five-hash and protected-file checks — passed. Only this draft and the planning JSON changed; EventStore remains clean.

- 2026-10-18: User selected option 1. The separate `spec-6-4-prerequisite-governed-retirement.md` owns the finite approved retirement work; this story remains draft/blocked pending its other original entry gates.

- 2026-10-18 completion: the separately approved five-file retirement prerequisite is locally complete. Strict tracked-content scan plus all 940 remaining guard cases pass, with all historical bytes and four prior registry records preserved. This supplies no approved owner metadata/RTI seam, G-4 runner, exact-source hosted CI/P1R qualification or remaining entry-gate acceptance. Story 6.4 remains draft/blocked.
