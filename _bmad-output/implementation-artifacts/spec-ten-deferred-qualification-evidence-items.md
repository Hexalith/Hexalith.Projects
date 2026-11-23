---
title: 'Verify retained P1R qualification evidence and reconcile nine isolation findings'
type: 'chore'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Resolve ten Projects deferred-work entries about P1R qualification evidence. For DW-71, provide a runnable check that discovers the retained SHA-256 manifests, verifies each referenced artifact is a tracked regular file in the same revision, and hashes its exact bytes. For the nine isolation findings from the recovery review (B1–B8 and V1), verify the historical EventStore fix and record the evidence beside each original finding. Report any missing historical artifacts as findings.

</frozen-after-approval>

## Implementation Notes

The verifier resolves Projects HEAD once, then reads manifests and artifact bytes from that commit. Git checkout filters, local edits and untracked files cannot affect the result. The command treats each path as relative to the manifest's directory and prints every failing entry.

## Verification

- At Projects `bfa4dd5d869b1d2fe75140dddfee1164a19eae52`, the verifier checked 270 entries. All 215 in the 3.102 bundle matched. In the three 3.97 bundles, 4/10, 11/18 and 17/27 matched. The command exited 1 and named all 23 missing logs. DW-70 tracks their recovery.
- All 27 historical isolation checks passed in an isolated EventStore checkout at `2817c2cc510adf39a45994d4b9d2e8dbdf48b8a0`. To reproduce, run `python3 -m unittest discover -s tools/tests -p 'test_p1r_isolation_hardening.py' -v` from the root of that revision. The test file matches the SHA-256 in the retained source inventory. [Historical suite](https://github.com/Hexalith/Hexalith.EventStore/blob/2817c2cc510adf39a45994d4b9d2e8dbdf48b8a0/tools/tests/test_p1r_isolation_hardening.py) · [Source inventory](evidence/6-4-p1r-31251-immutable-binding-20261018/final/sources-sha256.json) · [Retained 27-test output](evidence/6-4-p1r-31251-immutable-binding-20261018/final/test_p1r_isolation_hardening.log)
- For B1–B6, the tests vary the container creation, image inspection, SDK launch, input hash, ownership, process records, measured executables and paired absence observations through the importer. B7 and B8 exercise early rejection of remote Docker endpoints and the shared startup deadline. V1 runs finalization through the real caller and checks that removing it breaks the positive case.
- EventStore fixed these nine findings in its tooling-hardening change and retired the controller and its workflow on October 23. The nine closures apply to that historical implementation. [Owner repair](https://github.com/Hexalith/Hexalith.EventStore/blob/9590106d411347d992ee29b812feacf56bed2c30/_bmad-output/implementation-artifacts/spec-6-1-p1r-tooling-hardening.md) · [Retirement](https://github.com/Hexalith/Hexalith.EventStore/commit/6a0b6441f6d78a3007678a1d0ec336e9cb8ff961)

- The verifier CLI regression suite passed five tests covering exact committed content, separate bundle roots, missing and malformed entries, duplicate paths, symlinks, escaped filenames, and absence of any tracked manifest. Run `python3 tests/tools/test_verify_retained_manifests.py -v`. The 51 existing production-authority tests and `python3 tools/planning/validate_production_authority.py --validate-index` also passed.
- The repository's `aspire run --non-interactive` baseline exited 6. The configured historical AppHost references a missing `src/Hexalith.Platform.Composition/Hexalith.Platform.Composition.csproj` under its evidence directory. The verifier runs directly with Python and Git.

## Review Triage Log

- Medium, fixed directory-only paths such as `trace.bin/` and `trace.bin/.` being accepted as files. The CLI suite checks both spellings.
- Medium, changed manifest parsing to split on LF and accept CRLF. A literal Unicode line separator inside a tracked filename now verifies.
- Medium, added GNU sha256sum filename escapes for backslash, CR and LF. The regression suite also checks an invalid escape.
- Low, linked the retained output listing all 27 historical tests beside the replay revision and test source hash.
