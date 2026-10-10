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

<frozen-after-approval reason="human-owned P1R scope and supported envelope">

## Intent

**Problem:** The 3.119.0 published packet is valid but nonqualifying. Stale-fence refusal is unproved, logical alias execution is missing, and immutable old packages fail required comparison directions. P1R remains unusable.

**Approach:** Diagnose candidate-specific results, repair demonstrated defects, decide the supported boundary for old packages, then qualify an owner-selected published successor on PostgreSQL/Dapr 1.18.2. Present independent evidence for four owner decisions.

## Boundaries & Constraints

**Always:** Preserve sealed packets, October 1 acceptance, event bytes, retained floor, sequence, Tenant isolation, one writer, loaded package identity and measured checks. Keep historical failures visible. With `rollback=null`, use Projects AD-17 mutation freeze and forward recovery. Publication, pins and acceptance retain separate owner gates.

**Never:** Count an opaque exception as a proved refusal, infer alias execution from metadata, rebuild an existing version, omit failed directions silently, advance Projects readiness from source evidence, or deploy without authorization.

## I/O & Edge-Case Matrix

| Case | Input | Expected behavior |
| --- | --- | --- |
| Stale fence | Sequence 12; stale context and forged proof | Both deny with unchanged persisted state; opaque transport error stays nonpassing until diagnosed. |
| Logical alias | Registered old event and supported reader | Real alias replay preserves original envelope; unknown version refuses before application. |
| Old packages | 3.70.1 and 3.110.0 | Retain actual losses/refusals; incapable operations fail closed within an approved support boundary. |
| Recovery | Floor 5/head 12/snapshot 9; two Tenants | Restore, append 13 and restart preserve floor, hashes and other Tenant; otherwise freeze mutation. |

</frozen-after-approval>

## Open Questions

- Scope: pursue this blocking successor qualification path, or only the separate dump-to-copy backup provenance gap in `deferred-work.md` (small receipt fix that leaves P1R unqualified)?
- Historical support: require every current cross-version direction (old immutable failures keep qualification false), or approve a narrower supported set with incapable-route refusal and retained negatives (requires Solution Architect decision and gate/contract revision)?
- Candidate: is a successor EventStore/Builds tuple and publication scope approved with `rollback=null`, or should work stop after source/fixture repairs pending that decision?

## Code Map

- `tools/p1r_published_executor.py` — `mixed_case` measures stale/forged refusal; `evolution_case` lacks registered alias proof. Preserve raw outcomes.
- `tools/p1r-published-consumers/host/QualificationCapabilities.cs` — published actor fixture; diagnose nested Dapr error before runtime edits.
- `tools/p1r_published_qualification.py` — `required_lanes`/`evaluate` enforce all directions; change only after owner decision.
- `tools/p1r_qualification_runtime.py` — exact persisted inventory and restore receipts; preserve old schema/seals.
- `tools/tests/test_p1r_published_{executor,qualification}.py` — focused regression suites.
- `_bmad-output/implementation-artifacts/evidence/6-1-p1r-31190-published-run/README.md` — sealed nonqualifying baseline; create new evidence elsewhere.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — leave P1R usability false pending exact decisions.

## Tasks & Acceptance

**Execution:**
- [ ] `tools/p1r-published-consumers/host/QualificationCapabilities.cs` and `tools/p1r_published_executor.py` — reproduce stale-fence transport and persisted state; fix the proved defect.
- [ ] `tools/p1r_published_executor.py` and published consumer fixtures — execute registered alias replay and unknown-version refusal.
- [ ] `tools/p1r_published_qualification.py` — enforce only the approved support boundary while retaining old failures.
- [ ] `tools/tests/test_p1r_published_executor.py` and `test_p1r_published_qualification.py` — cover matrix outcomes, binding and fail-closed behavior.
- [ ] `_bmad-output/implementation-artifacts/evidence/` — after publication authority, run fresh package qualification at a unique path and validate independently.

**Acceptance Criteria:**
- Given the sealed 3.119.0 packet, when re-read, then its false qualification and failures remain unchanged.
- Given approved published inputs, when required lanes run, then each has measured persisted evidence; incompatible required evidence leaves qualification false.
- Given technical proof but pending owners or conformance, when evaluated, then P1R usability and Projects readiness remain false.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `python3 -m unittest discover -s tools/tests -p 'test_p1r_published_*.py'` from EventStore — focused tests pass.
- `git diff --check` in each changed repository — no whitespace errors.
