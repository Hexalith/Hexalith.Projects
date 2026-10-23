---
title: 'Complete five deferred qualification verification items'
type: 'chore'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five open deferred entries leave qualification verification incomplete: release exact-source preflight execution controls, complete atomic P1R candidate commands, stale historical G-6 packet reference checks, independent generated-artifact CI gates, and immutable placement/scheduler launch coverage.

**Approach:** Complete these five root-owned verification obligations with hermetic fixtures and precise command documentation. Replace historical-versus-current reference assertions with isolated historical provenance fixtures and run them in the existing workflow fixture lane. Exercise the real release shell, CI policy gate and current runner launch path, including negative controls. Preserve historical evidence, package pins, acceptance and sprint state. Verify, independently review, commit and push to main as requested.

</frozen-after-approval>

## Implementation Notes

- Investigation found the release shell harness and independent CI layout already exist; extend their executable negative controls rather than alter deployment policy. The P1R parser has no acceptance-record flag: it reads the fixed relative record under the supplied workspace root.
- Footprint: four existing test modules, existing CI fixture invocation and matching policy expectation, project context, deferred ledger and this spec. No new product API, dependency, topology change or live qualification. All work before the explicitly requested push is reversible; intent has no unresolved choices.

- Rebased packet reference checks on the retained attempt-16 packet/README rather than active sprint/owner documents or current Git index. Wired the suite into the existing blocking historical-fixture step and synchronized its exact PowerShell policy expectation.
- Release preflight fixtures now execute isolated source/event/state failures and malformed API responses. Removed unused job-response fixtures whose old tests only repeated aggregate run failure. The protected workflow still performs its own fresh qualifier.
- Verified the P1R command copied from project context against a separate temporary candidate directory containing all companion files; the real validator returned `PASS: production-authority index is [6, 7, 8]`. No acceptance or index file changed.
- Initial focused fixture command passed 56 tests before removal of two redundant release cases; the final release module separately passed 14 tests. Historical packet and current selection/launch modules passed 20 tests. The full workflow policy mutation suite is being checked separately.

- Independent review identified six verification gaps. Patched them with exact generated-artifact step contracts, broader G-6 wrapper detection, repository-specific archive rows, archive state checks, accepted-file byte hashing, and an argument-aware live-SHA API stub. The six review issues required no product API or dependency changes.
- Full workflow policy mutation suite passed 20/20 in 422.866 seconds; the subsequently added duplicate-E2E rejection test passed independently. After review corrections, archived packet and release shell modules passed 23/23. The final policy positive control and six conditional/extra-command mutations run separately against the final gate.

## Review Triage Log

- BH1 — medium, patch: a generated-artifact step-level condition could suppress the command even though its job was unconditional. Both generated-artifact steps now require the exact unconditional blocking step text; fixtures inject a G-6-dependent condition in each step.
- BH2 — medium, patch: the forbidden-command list missed the current G-6 wrappers. Added both current entry points and exact run-block checks; fixtures insert a current G-6 command with explicit exit propagation into both steps.
- BH3 — medium, patch: global hash membership admitted repository-label swaps. Archive checks now compare each named repository's complete revision/gitlink table row; a swapped-binding negative control fails.
- BH4 — medium, patch: reviewed-pending normalization hid contradictory accepted-state fields. Archive reference validation separately requires consistent status, usability, decision and reviewed hash, with contradictory-state controls.
- BH5 — medium, patch: the reviewed-pending hash did not bind the README's advertised accepted-file digest. The archived-file check now hashes actual normalized-LF bytes, with acceptance metadata and raw-byte drift controls.
- BH6 — medium, patch: the release live-SHA stub ignored the jq selector. It now validates the actual API/selector arguments, and a mutated selector is rejected. No review finding was deferred.

- Final review-correction policy run passed its positive checkout and all six generated-artifact condition/command mutations (2/2 test methods, 49.573 seconds). The independent reviewer spot-checked all six fixes and confirmed no unresolved findings. `git diff --check` and commitlint passed. Baseline revision is `e70a73fc28d5e3f4457472206f622e835e994f54`; five ledger entries are resolved, with no story acceptance, package pin or submodule pointer change.
