---
title: '6.1-P1R Revalidate the current EventStore and Builds baseline'
type: 'chore'
created: '2026-09-26'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '3a121eb678b000bf88816beff75b0230194c3f32'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/6-1-p1r-acceptance.json'
  - '{project-root}/_bmad-output/specs/spec-6-1-p1r-revalidation-owner-acceptance/qualification-contract.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The accepted P1R record selects EventStore 3.106.0 and Builds `ad52f350a2f0bc47849179ae17b4594dafff5363`, while Builds P0 now runs and qualifies against EventStore 3.108.1. The old acceptance does not authorize the current exact tuple.

**Approach:** Follow the recorded 2026-09-25 owner direction to retain 3.108.1, verify the current owner-repository coordinates and qualification boundary, and prepare an exact new P1R decision packet. Preserve the 3.106.0 acceptance and all earlier evidence; record remaining P1R and G-6 owner decisions without claiming them.

## Boundaries & Constraints

**Always:** Use canonical Git revisions, distinguish the 3.108.1 release tag from the newer EventStore checkout, keep P0 Stage 6 and Story 6.1 blocked, and retain the existing rollback tuple until an owner changes it. Validate the current P1R guard and record the current G-6 result separately.

**Never:** Reinterpret the 3.106.0 acceptance as acceptance of 3.108.1; forge four-role or G-6 approvals; rewrite existing evidence; start Story 6.1 implementation; advance P0, P2, P3, P4, readiness, or Story 6.1 sprint status.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Accepted historical tuple | 3.106.0 record and guard | Remain valid as historical acceptance | Do not overwrite the record |
| Current 3.108.1 tuple | EventStore tag plus Builds catalog/runner/schema | One reviewable exact candidate with separate source and package coordinates | Any mismatch stays pending |
| G-6 drift | Existing packet against current repositories | Record exact validator result and changed bindings | Do not reuse stale approval |

</frozen-after-approval>

## Code Map

- `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/` -- owner source record for the immutable 3.108.1 tag and current checkout distinction.
- `references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md` -- current Stage 5 and Stage 6 evidence; append the revalidation result without changing accepted P1R frontmatter.
- `_bmad-output/implementation-artifacts/6-1-p1r-acceptance.json` and `tools/planning/validate_production_authority.py` -- historical acceptance and its exact guard; preserve.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- Story 6.1 remains blocked; record the new candidate as pending without falsely reopening historical acceptance.
- `_bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain/` -- historical packet; do not reseal it for a changed source tree.

## Tasks & Acceptance

**Execution:**
- [x] Record exact EventStore tag, current checkout, and release manifest in the EventStore owner repository.
- [x] Verify Builds current revision, catalog, runner, schema, and focused tests; append the result in the Builds owner story.
- [x] Write the Projects exact-baseline candidate and owner decision list, including G-6 drift and verification commands.
- [x] Keep sprint and Story 6.1 implementation blocked; validate the existing guard and repository diffs.

**Acceptance Criteria:**
- Given the 3.106.0 acceptance, when the current candidate is reviewed, then its historical JSON and guard still validate without claiming 3.108.1 approval.
- Given EventStore `v3.108.1` and current Builds, when the owner packet is read, then exact revisions and every unresolved owner decision are explicit.
- Given the incomplete prerequisite chain, when sprint status is inspected, then Story 6.1 remains `blocked` and implementation has not started.

## Implementation Notes

The request itself authorizes revalidation and preparation of reviewable owner decisions. It does not authorize recording new owner acceptance.

- EventStore, Builds, and Projects owner-repository records now identify the proposed `3.108.1` tuple and the historical `3.106.0` acceptance separately.
- The production-authority guard and all 18 focused guard tests pass. The retained G-6 validator exits `1` on the changed CommunityToolkit Dapr pin; the pending packet records this exact blocker.
- The existing user edit to the Story 6.1 spec was preserved. No Story 6.1 implementation or sprint-status transition occurred.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| BH-01 | medium | patch | The pre-existing Story 6.1 edit sets `ready-for-dev` while its frozen intent and the sprint rule require the complete chain and independent `READY`; restore only its frontmatter status to `draft`. |
| BH-02 | medium | patch | G-6 remains historically `accepted` while the current packet fails; mark the historical scope and current candidate applicability explicitly in the sprint record. |
| BH-03 | medium | patch | P1R `done` and `unblocks` describe the 3.106.0 decision, while the new 3.108.1 tuple is pending; mark that limit on the action and P0 dependency. |
| BH-04 | medium | patch | Architecture Spine still binds EventStore 3.70.1 at its Stack table; the owner packet must name the exact conformance/rebinding decision. |
| BH-05 | medium | patch | The manifest hash proves source inventory only; published 3.108.1 package provenance and hashes remain a required Test Architect input. |
| BH-06 | medium | patch | The EventStore owner record compares only the manifest despite 43 source paths changing after the tag; record scoped API comparison and the remaining compatibility assessment. |
| BH-07 | low | patch | The P1R companion `SPEC.md` also fixes 3.106.0; include it in the eventual coordinated acceptance transition. |
| EC-01 | medium | patch | This independently found the same Story 6.1 status conflict as BH-01; the frozen text itself calls the story draft and blocked. |

## Verification

**Commands:**
- `python3 tools/planning/validate_production_authority.py --validate-index` -- expected: historical acceptance remains internally valid.
- `python3 references/Hexalith.Builds/Tools/validate-runtime-toolchain-evidence.py --workspace . --baseline references/Hexalith.Builds/Tools/runtime-toolchain-baseline.json --packet _bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain/packet.json` -- expected: report actual current result; drift means fresh G-6 qualification is required.
- `git diff --check` in each modified owner repository -- expected: no whitespace errors.
