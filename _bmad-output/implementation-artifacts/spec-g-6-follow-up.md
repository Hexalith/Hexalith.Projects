---
title: 'G-6 Follow-up Build Scope'
type: 'chore'
created: '2026-10-03'
status: 'draft'
route: 'dispatch'
baseline_commit: '0ef362eec3ec8e89cbfbcf96c479f1a8426c8e15'
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/projects-topology.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-g-6-refresh-current-checkout-qualification.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The invocation requests G-6, but all five existing G-6 build specs are done. Latest proof names `0f03582b3457a6d9212d60e2f9146a6043af5f7e`; HEAD only adds evidence/tracking. Three deferred improvements remain, with none selected.

**Approach:** Resolve the requested follow-up, then prepare its implementation and checks while preserving the qualification and tuple.

## Boundaries & Constraints

**Always:** Preserve the approved tuple, named decision, historical evidence and exact-source enforcement. Keep reuse disabled. Work in the owning repository. Report historical proof separately from current applicability; material changes need fresh proof before qualification.

**Never:** Change versions, approvals, G-4/G-5, P1R or downstream states; weaken gates; initialize nested submodules; interrupt shared resources; enable Workflow; publish or deploy. No new commit, push or dispatch is authorized by this draft.

## I/O & Edge-Case Matrix

| Input / State | Expected Behavior | Failure Handling |
| --- | --- | --- |
| Approved audit, no live proof | Report pins independently of qualification | Audit cannot qualify a checkout |
| Different material bytes or source | Reject prior-result applicability | Retain original evidence |

</frozen-after-approval>

## Open Questions

1. Which outcome does this new G-6 invocation request? Options: confirm the completed G-6 scope with no implementation; obtain fresh proof for exact HEAD; fix material-fingerprint reproducibility; exercise the Platform app model; or retire/rebase the obsolete historical packet-reference tests. Select one deliverable. Fingerprint reproducibility is the recommended implementation follow-up; fresh proof does not resolve EOL differences.

## Code Map

- Existing five `spec-g-6-*.md` files — all done; the tuple, CI-readiness and checkout-refresh specs explicitly defer the three improvements.
- `_bmad-output/implementation-artifacts/sprint-status.yaml:307` — current qualification names `0f03582`; preserve historical acceptance and other prerequisites.
- `qualification-evidence/g-6-checkout-refresh-20261003/` under implementation artifacts — successful CI `37118950623`, 48-file checksum index and exact-source validator replay.
- `references/Hexalith.Builds/Tools/g6_current.py::tracked_material` — hashes working-tree bytes; clean EOL conversion changes fingerprints. `validate` also enforces exact root/gitlinks with reuse disabled.
- `references/Hexalith.Builds/Tools/test_g6_current.py` — three controls for material changes, capture tampering and stale-source rejection; extend here if fingerprint semantics are selected.
- `tools/qualification/run_g6_current.py` and `run_g6_qualification.py` — isolated runtime proof and consumer builds; Platform is compiled without exercising its app model.
- `tests/tools/test_g6_packet_references.py` — obsolete historical assertions excluded from current CI; preserve genuine provenance checks if retiring/rebasing it.
- `_bmad-output/implementation-artifacts/deferred-work.md:917` — three open follow-ups and resolved CI/release/runner readiness items.

## Tasks & Acceptance

**Execution:**
- [x] Existing G-6 specs, sprint status and retained evidence — identify completed work and independent remaining scopes.
- [x] `references/Hexalith.Builds/Tools/g6_current.py` — run current audit and strict prior-result validation; distinguish pin readiness from current proof.
- [x] Existing runner/gate tests and retained `SHA256SUMS` — verify current controls and evidence integrity without launching shared infrastructure.
- [ ] This spec — record the selected outcome and replace investigation tasks with exact implementation actions/checks before approval.

**Acceptance Criteria:**
- Given completed proof, when reporting readiness, then cite its source and CI without qualifying different HEADs.
- Given a selected follow-up, when approved, then its tasks name files and tests that detect the substantiated defect.
- Given historical records, when work finishes, then their bytes, decisions and downstream states remain unchanged.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

- `python3 references/Hexalith.Builds/Tools/g6_current.py audit --workspace . --policy references/Hexalith.Builds/Tools/g6-current-policy.json --out /tmp/g6-build-20261003-audit.json` exited 0: tupleApproved=true, zero issues, 7,026 material files; root `0ef362eec3ec8e89cbfbcf96c479f1a8426c8e15`.
- Strict validation command: `PYTHONDONTWRITEBYTECODE=1 python3 references/Hexalith.Builds/Tools/g6_current.py validate --workspace . --policy references/Hexalith.Builds/Tools/g6-current-policy.json --evidence _bmad-output/implementation-artifacts/qualification-evidence/g-6-checkout-refresh-20261003/ci-37118950623-1/result.json` exited 1: `G6-CURRENT-NOT-VERIFIED: current audit materialInputs differs from evidence`.
- `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests/tools/test_g6_qualification_runner.py tests/tools/test_run_g6_current.py tests/tools/test_run_g6_ci_gate.py -q` — passed 25/25.
- `PYTHONDONTWRITEBYTECODE=1 python3 references/Hexalith.Builds/Tools/test_g6_current.py` — passed 3/3.
- `sha256sum -c SHA256SUMS` from the retained refresh evidence directory — all 48 entries passed.
- Latest GitHub CI is successful on the recorded `0f03582` source. No new runtime proof, Git mutation or dependency update has been performed.
