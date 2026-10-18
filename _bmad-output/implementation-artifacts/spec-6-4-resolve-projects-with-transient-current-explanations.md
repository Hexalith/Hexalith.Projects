---
title: 'Story 6.4: Transient current Project resolution'
type: 'feature'
created: '2026-10-17'
updated: '2026-10-18'
status: 'draft'
route: 'dispatch'
story_key: '6-4-resolve-projects-with-transient-current-explanations'
baseline_commit: '25f9fdab6106f5bcbf06aaabe8ca351f543bb52e'
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

## Code Map

- `src/Hexalith.Projects.Contracts/Queries/Admission*.cs` — shared AD-32 vocabulary.
- `src/Hexalith.Projects/Resolution/` — engine/mappers; filter protected exclusions first.
- `evidence/6-4-resume-20261018/current-preflight-25f9fda.json` — findings and checks.
- `spec-6-4-prerequisite-p1r-31251-enrollment.md` — selected prerequisite plan.

## Tasks & Acceptance

**After accepted prerequisites:**

- [ ] `src/Hexalith.Projects.Contracts/Queries/` — add both resolution contracts using shared vocabulary.
- [ ] `src/Hexalith.Projects/Resolution/ProjectResolutionSnapshotComposer.cs` — compose authorized snapshots purely.
- [ ] `src/Hexalith.Projects.Server/Queries/` — add both handlers over approved owner/RTI contracts.
- [ ] `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs` — register additively.
- [ ] `tests/Hexalith.Projects.Contracts.Tests/`, `tests/Hexalith.Projects.Tests/Resolution/`, `tests/Hexalith.Projects.Server.Tests/Queries/` — test the frozen matrix.
- [ ] `tests/Hexalith.Projects.Integration.Tests/` — prove zero writes, restart/two-instance behavior and shadow equivalence through G-4.

**Acceptance:** Given accepted gates, when both handlers run, then the matrix holds and actual `evidence/epic6/6.4-resolution-reads.{trx,json}` proves persisted boundaries.

## Implementation Notes

- 2026-10-17: Prerequisites-first decision recorded. The first proposed work package is `spec-6-4-prerequisite-p1r-source-readiness.md`; its preflight evidence records five verified package signatures and 224 passing tooling controls without claiming P1R qualification. Story 6.4 remains a draft behind its original gates.

- 2026-10-18: Resumed planning at Projects `3d3b3fab63afe63090e6314e2c2b6e66bcd0ec31`; retained reviewed EventStore source hashes match checkout. Seven historical findings in five files remain; no fresh full scan is claimed. Independent source inspection confirms missing owner metadata/RTI contracts. Local 6.1–6.3 exceptions do not authorize 6.4. Retained command outputs and an unapproved exact-file proposal in the linked JSON; runtime, pins, acceptance and original story/tracker remain unchanged.
- 2026-10-18 recheck: Projects `3faac93ac718b16629923ac824e9f463759c5c17`, EventStore `47e234ccb535a71e7dd4a039fd0e2281083083ae`. Current-source P1R preparation and Commitlint pass; CI is pending. All five indexes omit proposed 3.125.0. Approved retirement is pushed; owner batches/RTI and G-4 remain absent. The earlier publication proposal needs refreshed coordinates before an owner decision. Detailed evidence and prior verification are retained in the Code Map JSON.

- 2026-10-18 live recheck: both 3.125.0 and 3.125.1 are published in all five indexes; tag/nuspec sources match. Both releases differ from the proposed source. Local owner/RTI and G-4 gaps remain. The preflight retains observations; selection/qualification/acceptance remain pending.

## Spec Change Log

## Review Triage Log

## Design Notes

No intent gap: 3.125.1 selected for planning. Story readiness remains blocked by its original prerequisites.

## Verification

- `python3 tools/planning/validate_production_authority.py --story-id 6.4` — PASS; scheduling only.
- `dotnet tool run hexalith-module test --profile reads --filter Story=6.4` — exit 1: `Cannot find a tool in the manifest file that has a command named 'hexalith-module'.`
- Check frozen intent, protected hashes, CRLF and `git diff --check`.
