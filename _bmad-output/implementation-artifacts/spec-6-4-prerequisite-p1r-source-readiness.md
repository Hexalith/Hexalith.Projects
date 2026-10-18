---
title: 'Story 6.4 prerequisite: P1R current-source successor readiness'
type: 'chore'
created: '2026-10-17'
status: 'done'
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

- [x] `tools/p1r-published-consumers/Directory.Build.props`, `host/Host.csproj`, `domain/Domain.csproj` — add only 3.125.0 to existing finite conditions; preserve older/source behavior.
- [x] `tools/p1r_published_executor.py`, `tools/p1r_check_witnesses.py` — enroll that version in seed/fixture/case/writer guards; bind restore to the selected candidate.
- [x] `tools/tests/test_p1r_published_executor.py` — exercise actual strict validation, seed, tampering and unrelated-version refusal; retain regressions.
- [x] `tools/p1r-published-consumers/README.md` — document enrollment and current protected release boundary.
- [x] Existing Contracts/Client/DomainService/Server and LiveSidecar test projects — build Debug with source references; run focused authority, recovery and snapshot checks, then isolated live source fixtures. Disclose runtime/backend limits and all failures/skips.
- [x] New owner `evidence/6-4-prerequisite-p1r-source-readiness-20261017/` — retain compiler evaluations, results, source/tool hashes, cleanup/preservation, independent review and concrete successor handoff.

**Acceptance Criteria:**

- Given proposed/source evidence, when strict validation runs, then intact inputs pass and missing/substituted inputs fail without widening production policy.
- Given source checks, when reported, then actual effects and failures remain separate from package qualification.
- Given the handoff, when reviewed, then publication, pins, acceptance and 6.4 states remain unchanged.

## Implementation Notes

- Enrolled proposed 3.125.0 in the finite consumer compiler conditions, snapshot seed and witness guards, restore writer binding, and final isolation import checks. Unenrolled successors now fail before fixture state reset or writer startup.
- Kept the work in EventStore tooling and its Counter fixture. Published packages, production source, package pins, and Story 6.4 handlers were not changed.
- Owner evidence: [EventStore source-readiness record](../../references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-4-prerequisite-p1r-source-readiness-20261017/README.md). The source checks ran with Dapr 1.18.7/Redis; pinned Dapr 1.18.2/PostgreSQL published qualification remains open. The first LiveSidecar attempt hit the host inotify limit, and an isolation run overlapped a source edit; both failures and passing reruns are retained.
- Independent review found and closed a source-seed allowlist gap. Exact pre/post preservation of shared Dapr image IDs and start times was not proven, so private P1R cleanup qualification remains unverified.

## Spec Change Log

## Review Triage Log

- **B01 low / patch:** The evidence README says 3.125.0 `compiles`, while its 12 recorded commands evaluate MSBuild properties and Compile items only. Corrected the claim to describe project evaluation; no published package build ran.
- **B02 false:** The matrix describes the strict validator's conditional behavior when genuine evidence is supplied. The executed synthetic-receipt controls exercise that validator, and the evidence explicitly says no genuine published 3.125.0 seed or qualification was run.
- **B03 low / rejected:** The NuGet index response bodies and HTTP statuses were not retained, so the dated absence observation is not independently replayable. The version remains unselected and the handoff requires a fresh signed-archive check before publication; adding another network capture to this local source package would not change a gate.
- **B04 low / rejected:** Test logs and compiler evaluations have no separate SHA-256 manifest. The spec requires source/tool hashes and retained results, both present; a second mutable evidence manifest would add process without affecting this uncommitted source-readiness decision.
- **B05 low / rejected:** The prior Projects preflight release-analysis record lacks the analyzer command/version/output. It predates this implementation, proposes an unselected version, and the handoff requires fresh reachable-history analysis before any selection.
- **B06 medium / defer:** The pre-existing main 6.4 draft omits the fixed action in its future handler acceptance, although Epic 6 requires action-specific authorization. The current prerequisite intent expressly forbids 6.4 handler implementation.
- **B07 medium / defer:** The main 6.4 draft does not distinguish an unauthorized attachment as no match from a denied Project target's safe 404. The draft remains blocked and is outside this EventStore tooling package.
- **B08 medium / defer:** The main 6.4 draft omits the Conversation member/explicitly-opened Project fallback rule present in Epic 6. It needs future 6.4 planning after owner contracts are approved.
- **B09 medium / defer:** The main 6.4 draft does not enumerate Project, Folder, Setup and first-response authority as required current inputs for usable Partial. The prerequisite package cannot amend future resolution policy.
- **B10 medium / defer:** The main 6.4 draft omits the exactly-one-authorized-Folder and folderless-quarantine candidate checks stated in Epic 6. No resolution handler is implemented in this package.
- **B11 medium / defer:** The main 6.4 draft abbreviates AD-32 as a shared snapshot without listing every field in its future acceptance. The original blocked story lists the fields; reconcile them when the draft is made ready.
- **B12 medium / defer:** The main 6.4 draft omits the Epic 6 latency targets from future resolution acceptance. Performance gates belong to the later supported read implementation.
- **V01 medium / patch:** The 3.125.0 Host/Domain/Probe compiler conditions have only retained one-off evaluations; existing executable registration tests use `UseCurrentSource=true`. Added a narrow automated MSBuild evaluation test for proposed and unenrolled versions; its full registration suite passed 2/2 after review.

## Design Notes

No intent gaps or irreversibles. Footprint: tooling/project files, tests, README and evidence. Installed release analysis reports minor over nine commits, suggesting 3.125.0; no package is selected/published. Package qualification, owner disposition, G-2/RTI, G-4, P2/P3/P4 and G-6 remain subsequent work.

## Verification

- Final executor suite: 128 passed; qualification: 61 passed; isolation: 31 + 24 passed; snapshot registration: 1 passed. Twelve Host/Domain compiler evaluations passed, including proposed 3.125.0 and unenrolled 3.126.0.
- Five individual Debug source test projects built with zero warnings/errors. Focused Contracts, Client, DomainService and Server suites passed; P1R LiveSidecar 2/2 and isolated Client recovery 2/2 passed. Post-review, 17 sequential checks passed, including the full affected Python suites, five rebuilt projects and seven focused .NET runs; retained results are in [the EventStore post-review results](../../references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-4-prerequisite-p1r-source-readiness-20261017/postreview-20261017/results.json).
- Matrix audit: compiler evaluations and seed tests cover finite and unenrolled versions; snapshot witness and tampering tests cover strict provenance; focused source/live tests cover authority, recovery and restart; package-scope/tag-source tests cover the publication boundary. All covering suites ran and passed in the retained final logs.
- `git diff --check` passed in both repositories; the ten recorded EventStore source/tool SHA-256 values match the current files. Original 6.4 and sprint statuses remain blocked.

Preflight evidence: `_bmad-output/implementation-artifacts/evidence/6-4-prerequisite-preflight-20261017/published-family.json`.

- Five 3.124.0 signatures passed; current source changes 67 production files beyond their source revision.
- Repeat affected executor/qualification/isolation suites after edits.
- Evaluate consumer DefineConstants/Compile for historical/proposed/source/unenrolled inputs without restoring unpublished packages.
- Build test projects individually in Debug; run focused xUnit classes with single-dash selectors. Missing prerequisites remain nonpassing.
- Run `git diff --check` and protected-history/pin/acceptance/sprint preservation checks.
