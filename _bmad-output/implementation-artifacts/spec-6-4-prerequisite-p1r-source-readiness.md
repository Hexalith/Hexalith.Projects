---
title: 'Story 6.4 prerequisite: P1R current-source successor readiness'
type: 'chore'
created: '2026-10-17'
status: 'ready-for-dev'
approved_on: '2026-10-17'
approval_decision: 'approve and stop'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '71c326813e75e7b3486555b5f22ed377b42064d6'
owner_repository: 'references/Hexalith.EventStore'
owner_baseline_commit: 'bb986902d3c1b2f3b1c209fe8eb0ffa0292153d9'
builds_observed_revision: 'b7a9c9ec9f57e098b408d3b940929ef4b160985b'
context:
  - '_bmad-output/implementation-artifacts/spec-6-4-resolve-projects-with-transient-current-explanations.md'
---

<frozen-after-approval reason="human-owned prerequisite scope — approval required before implementation">

## Intent

**Problem:** Projects uses authority/recovery changes absent from published EventStore 3.124.0. P1R tooling enrolls only older successors; old receipts cannot qualify current source.

**Approach:** Enroll proposed 3.125.0 in the existing qualification fixture, verify strict controls/current source, and prepare its publication/qualification handoff. This first prerequisite package does not complete P1R or 6.4.

## Boundaries & Constraints

**Always:** Work in EventStore; preserve existing work, history, finite enrollment, genuine snapshots, private-resource ownership, retained floor and Tenant isolation. Keep rollback=null with drain/freeze/forward recovery. Approval covers tooling/local verification only. Recheck source/version analysis before dependent work.

Preserve the existing Reminder/evolution lanes and pinned Dapr 1.18.2 package profile.

**Never:** Implement 6.4, invent owner/RTI contracts, stage/commit/push/publish, move pins, fabricate acceptance, rewrite packets, weaken checks, admit arbitrary versions or bypass tag/source equality. Keep production/story gates blocked; do not restore obsolete release-comment authority.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Finite enrollment | Proposed 3.125.0 or source consumer | Existing capability/evolution/folded-snapshot compiler inputs and genuine seed/witness checks | No production snapshot-policy expansion |
| Other successor | Unenrolled version | No candidate-specific compilation or genuine seed | Fail closed |
| Genuine evidence | Nine commits, manual snapshot, three tail commits | Real strict validator accepts intact evidence from the correct writer | No diagnostic synthesis |
| Tampering | Missing/substituted source, binary, version, provider or timing proof | Strict rejection | Rehashing cannot bypass checks |
| Current source | Authority/recovery, retained replay and restart cases | Actual expected effects and preserved state/history | Failures remain nonpassing |
| Publication boundary | Source checks pass; package absent | Readiness handoff only | No qualification or acceptance claim |

</frozen-after-approval>

## Code Map

Paths below are relative to `references/Hexalith.EventStore`.

- `tools/p1r-published-consumers/` — finite Counter fixture; separate source/package graphs.
- `tools/p1r_published_executor.py`, `tools/p1r_check_witnesses.py` — seed, retained fixture and strict writer/provenance checks.
- `tools/p1r_isolation.py` — private resources and cleanup; reuse unchanged.
- `_bmad-output/specs/spec-6-1-p1r-remediation/qualification-contract.md` — retain the existing scenario obligations.
- Contracts `QueryCallerAuthorityTests`, Client pending-recovery tests and Server snapshot tests — current capability/retained-state checks.
- LiveSidecar `P1RRemediationPersistenceTests` — source-only append/restart proof; not package rollback or PostgreSQL qualification.

## Tasks & Acceptance

**Execution:**

- [ ] `tools/p1r-published-consumers/Directory.Build.props`, `host/Host.csproj`, `domain/Domain.csproj` — add only 3.125.0 to existing finite conditions; preserve older/source behavior.
- [ ] `tools/p1r_published_executor.py`, `tools/p1r_check_witnesses.py` — enroll that version in seed/fixture/case/writer guards; bind restore to the selected candidate.
- [ ] `tools/tests/test_p1r_published_executor.py` — exercise actual strict validation, seed, tampering and unrelated-version refusal; retain regressions.
- [ ] `tools/p1r-published-consumers/README.md` — document enrollment and current protected release boundary.
- [ ] Existing Contracts/Client/DomainService/Server and LiveSidecar test projects — build Debug with source references; run focused authority, recovery and snapshot checks, then isolated live source fixtures. Disclose runtime/backend limits and all failures/skips.
- [ ] New owner `evidence/6-4-prerequisite-p1r-source-readiness-20261017/` — retain compiler evaluations, results, source/tool hashes, cleanup/preservation, independent review and concrete successor handoff.

**Acceptance Criteria:**

- Given proposed/source evidence, when strict validation runs, then intact inputs pass and missing/substituted inputs fail without widening production policy.
- Given source checks, when reported, then actual effects and failures remain separate from package qualification.
- Given the handoff, when reviewed, then publication, pins, acceptance and 6.4 states remain unchanged.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

No intent gaps or irreversibles. Footprint: tooling/project files, tests, README and evidence. Installed release analysis reports minor over nine commits, suggesting 3.125.0; no package is selected/published. Package qualification, owner disposition, G-2/RTI, G-4, P2/P3/P4 and G-6 remain subsequent work.

## Verification

Preflight evidence: `_bmad-output/implementation-artifacts/evidence/6-4-prerequisite-preflight-20261017/published-family.json`.

- Five 3.124.0 signatures passed; current source changes 67 production files beyond their source revision.
- Repeat affected executor/qualification/isolation suites after edits.
- Evaluate consumer DefineConstants/Compile for historical/proposed/source/unenrolled inputs without restoring unpublished packages.
- Build test projects individually in Debug; run focused xUnit classes with single-dash selectors. Missing prerequisites remain nonpassing.
- Run `git diff --check` and protected-history/pin/acceptance/sprint preservation checks.
