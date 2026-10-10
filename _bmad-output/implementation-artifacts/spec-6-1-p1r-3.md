---
title: '6.1-P1R Establish a supportable successor qualification path'
type: 'bugfix'
created: '2026-10-10'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/spec-6-1-p1r-remediation.md'
  - '{project-root}/references/Hexalith.EventStore/_bmad-output/specs/spec-6-1-p1r-remediation/qualification-contract.md'
---

<frozen-after-approval reason="human-owned P1R scope and supported envelope; do not modify without a new decision">

## Intent

**Problem:** The owner-selected EventStore 3.119.0 published run has a valid packet but fails technical qualification. Its stale-fence result lacks a proven refusal, registered logical event evolution was not executed, and immutable older packages fail required compatibility directions. Current P1R usability and Story 6.1 remain blocked.

**Approach:** Diagnose the candidate-specific failures with persisted evidence, correct demonstrated source or fixture defects, and obtain an explicit supported-envelope decision for incapable historical directions. Requalify only a separately owner-selected published successor against the selected PostgreSQL/Dapr profile, then present exact evidence for the four owner decisions. Keep the existing 3.119.0 packet and October 1 acceptance sealed.

## Boundaries & Constraints

**Always:** Use the EventStore repository for runtime and harness changes. Preserve command, event and snapshot bytes, retained floor, sequence, Tenant isolation, one writer, actual loaded package identity, measured assertions, source closure and owned cleanup. Keep historical comparison failures visible. With `rollback=null`, enforce the approved Projects AD-17 mutation freeze and forward recovery. Publication, pin movement and acceptance require their separate owner decisions.

**Never:** Turn an opaque exception into a successful refusal by label alone; infer alias execution from registration metadata; rebuild an existing package version; rewrite sealed receipts; silently remove failing comparison directions; claim P1R usable, change Projects pins/status/readiness, or perform publication/deployment from source-only evidence.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Stale fence | Published actor at sequence 12, stale context and forged proof | Both deny with unchanged domain inventory; exact denial or independently bound safe refusal is observable | Opaque transport failure remains nonpassing until its cause and outcome are proved |
| Logical alias | Registered old logical event and supported reader | Actual replay uses the registered alias, preserves the original envelope and rejects an unknown version before application | Missing registration or skipped execution remains nonpassing |
| Historical comparison | 3.70.1/3.110.0 packages lacking newer capability | Record actual losses/refusals, then evaluate only the owner-approved support boundary | No unsupported old operation may execute or appear complete |
| Recovery | Floor 5/head 12/snapshot 9, two Tenants, PostgreSQL/Dapr 1.18.2 | Fresh restore, append 13, restart and replay preserve old hashes, floor and other Tenant | Without capable rollback, freeze mutation and recover forward |

</frozen-after-approval>

## Open Questions

- Scope of this invocation — proceed with the successor technical qualification path above (addresses the blocking P1R evidence), or only close the separate dump-to-copy backup provenance gap in `deferred-work.md` (small receipt-contract fix; P1R remains technically unqualified)?
- Supported historical compatibility envelope — keep every current cross-version direction required (technical qualification remains impossible while immutable old packages fail), or have the Solution Architect explicitly approve a narrower supported set with incapable-route refusal and retained negative observations (the qualification contract and gate would need a reviewed change)?
- Successor publication and recovery — is there an owner-approved exact candidate/Builds tuple and publication scope for a fresh run, with `rollback=null` and AD-17 freeze/forward recovery, or should this work stop after source and fixture repairs while publication and owner selection remain pending?

## Code Map

- `references/Hexalith.EventStore/tools/p1r_published_executor.py` — `mixed_case` records stale/forged outcomes; `evolution_case` currently cannot execute a registered alias; `restore_case` already records successful PostgreSQL restore and cleanup. Preserve raw failed observations.
- `references/Hexalith.EventStore/tools/p1r-published-consumers/host/QualificationCapabilities.cs` — published actor fixture and stale-fence transport. Reproduce the nested Dapr exception before changing runtime semantics.
- `references/Hexalith.EventStore/tools/p1r_published_qualification.py` — `executed_case_inventory`, `required_lanes` and `evaluate` define the strict gate; alter its supported set only after an explicit owner decision.
- `references/Hexalith.EventStore/tools/p1r_qualification_runtime.py` — persisted inventory and restore validator; preserve exact receipt schema and existing seals.
- `references/Hexalith.EventStore/tools/tests/test_p1r_published_executor.py` and `test_p1r_published_qualification.py` — focused regressions for refusal, alias execution, boundary selection, and fail-closed evaluation.
- `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-31190-published-run/README.md` — immutable nonqualifying 3.119.0 baseline. New proof belongs at a unique path.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — Projects authority record; keep current P1R usability false until exact decisions and independent gates pass.

## Tasks & Acceptance

**Execution:**
- [ ] `references/Hexalith.EventStore/tools/p1r-published-consumers/host/QualificationCapabilities.cs` and `tools/p1r_published_executor.py` — reproduce the stale-fence transport response, prove persisted non-mutation, and correct only the demonstrated defect.
- [ ] `references/Hexalith.EventStore/tools/p1r_published_executor.py` and published consumer fixtures — execute a real registered logical alias and unknown-version refusal through the selected package; retain the original envelope and measured checks.
- [ ] `references/Hexalith.EventStore/tools/p1r_published_qualification.py` and tests — encode only the approved supported comparison boundary; preserve the failed historical observations and reject unapproved omissions.
- [ ] `references/Hexalith.EventStore/tools/tests/test_p1r_published_executor.py` and `test_p1r_published_qualification.py` — cover all matrix outcomes, packet binding and fail-closed negatives.
- [ ] `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/` — after exact publication authority, run fresh package and PostgreSQL/Dapr qualification at unique paths, validate independently and record owner decisions without transferring historical acceptance.

**Acceptance Criteria:**
- Given the current 3.119.0 packet, when it is re-read, then its false qualification and historical failed directions remain unchanged.
- Given an approved successor package and support boundary, when all required published lanes run, then every result has measured checks and persisted evidence; missing or incompatible required evidence leaves qualification false.
- Given complete technical proof, when four owners and same-baseline conformance are pending, then P1R usability and Projects readiness remain false.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `python3 -m unittest tools.tests.test_p1r_published_executor tools.tests.test_p1r_published_qualification` from EventStore — focused harness regressions pass.
- `python3 tools/p1r-published-qualification.py validate --help` from EventStore — verify the supported CLI contract before an owner-authorized fresh packet validation.
- `git diff --check` in each changed repository — no whitespace errors.
