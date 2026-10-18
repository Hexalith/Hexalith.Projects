---
title: 'Story 6.4 prerequisite: approved exact-file historical retirement'
type: 'chore'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
owner_repository: 'references/Hexalith.EventStore'
owner_baseline_commit: '37bdc90a72b9f0ea547852e903334cc8cc541641'
context: []
---

<frozen-after-approval reason="user selected option 1 approving the reviewable five-file proposal">

## Intent

**Problem:** Seven historical secret-scan findings remain in five immutable EventStore artifacts, and their full-suite/source-review claims cannot remain authoritative after retirement.

**Approach:** Execute only the five exact path/SHA-256 retirements approved by the user's option-1 reply, as bound by the unchanged proposal and separate owner-decision record under `evidence/6-4-resume-20261018/`. Preserve every original byte and historical receipt/index. Add a finite non-authoritative tombstone/lineage overlay, independent content-bound approval validation, registry enrollment, focused adversarial ingestion tests and measured verification. Preserve the two established approvals and reject re-pinning, unknown paths, modified or absent bytes, missing/invalid approval and adjacent credentials. No broader scanning exclusion or fixture recognition is authorized. Do not publish, change package/source pins, commit, push, accept P1R or implement/unblock Story 6.4.

</frozen-after-approval>

## Implementation Notes

- User approval is the explicit option-1 selection in this session, recorded verbatim with date and proposal hash; no personal name or separate four-role acceptance is inferred.
- Intent gaps: none. Irreversibles: none; originals and prior approvals remain intact. Footprint: existing secret-guard retirement validation/registry, one focused test partial, existing preservation assertions, architecture/current-authority notice, and a new exact-byte decision/overlay/evidence directory. Story key remains unset.
- Reuse `IsRetiredEvidenceCapture`, `WithEvidenceRepository` and canonical `ScanTrackedFile`/decoder. Existing two hash approvals and unapproved inherited OTLP entries remain unchanged. New enrollment is exact-path only and independently pins the user-authorized decision plus its proposal, selection and withdrawal overlay. Historical probe tests will continue verifying raw/renamed detection while canonical ingestion validates retirement.
- Baseline EventStore source is the full commit above. Parent root's existing planning edits are preserved. Build/test runs use local Debug source references, the owning Server test project and direct xUnit class/method filters; no solution-level test run. Retain actual strict tracked-content results separately from focused tests and hosted CI/P1R qualification.

- Implemented finite retirement validation in the existing reader with a dedicated test partial. The code pins owner decision `22d6ca077b3d4d78b6a317a4191d7fac696520096ec0908da9b29fe3f196926d`, which binds the untouched proposal, explicit user selection and additive authority overlay. Registry enrollment preserves all four prior records; the two inherited OTLP entries remain unusable.
- Added the overlay for all 46 direct and 27 indirect references, including withdrawn Server full-suite and Story 3.6 reviewed-source authority. Added current notices outside frozen/historical content. All five original hashes also match their exact Git blobs.
- Baseline: started the exact isolated EventStore AppHost through the Aspire orchestration workflow, observed every listed resource Running/Healthy, then stopped only that owned AppHost before editing. Initial Debug build and subsequent focused builds passed with zero warnings/errors.
- Red controls: all five acceptance cases failed before validator enrollment. Focused verification passed 38/38 with zero failures/errors/skips. The complete SecretsProtectionTests class, including the strict tracked-content gate, is running separately. Hosted CI/P1R qualification is not claimed.
- Explicit scope forbids commit/push/publication. This overrides the generic oneshot commit instruction; no Git recording operation is authorized by the user's retirement selection.

- Independent Blind Hunter review completed. Patched every confirmed finding: all 83 overlay-bound originals/references now survive actual Windows checkout filters (56 exact new Git attribute rules); decision membership is checked per tuple; contradictory duplicate registry properties fail; canonical renamed-original controls and exact retained URI lines are asserted; 83 historical files and 255 RFC 6901 locators have repeatable integrity checks; five earlier diagnostics were added to lineage; affected claims have explicit dispositions; copied selection/proposal origin is explicit without rewriting the original human selection.
- Reviewed Debug build passed with zero warnings/errors; final focused verification passed 49/49 cases with zero failures/errors/skips. The first complete run was deliberately terminated for the confirmed review corrections and remains recorded as interrupted (exit 143), never passing. The corrected complete class is running. Final decision digest is `e63af04857d0b4d72e5234f17e9014aae971efcab7c7ee83bbcc45bd56679d7d`; earlier local digests in these notes are historical implementation attempts.

## Review Triage Log

| Finding | Verdict | Evidence / resolution |
| --- | --- | --- |
| XML checkout normalization | medium, patched | Actual CRLF checkout filters changed both XML hashes; exact preservation rules now cover both originals and all 54 additionally affected references. All 83 hashes match. |
| Decision tuple membership | low, patched | The current dictionaries already matched the five owner tuples; the reader now explicitly checks each registry tuple against the independently pinned decision. |
| Duplicate registry properties | medium, patched | A real canonical-ingestion reproduction accepted contradictory status properties. Root and selected-entry uniqueness now rejects them; controls cover eight property forms for all five files. |
| Referenced history integrity | medium, patched | A persistent test now verifies all 83 original/reference file hashes, preserving all historical bytes. |
| Ambiguous reference locators | low, patched | Replaced descriptive field paths with 255 resolving RFC 6901 pointers and explicit key/value targets; an actual test resolves every pointer. |
| Prior diagnostic inventory | low, patched | Added five preserved scan-diagnostic references, yielding 46 direct and 32 indirect rows. They confer no qualification authority. |
| Renamed original ingestion | medium, patched | All five exact original byte sequences are detected through canonical ingestion under unapproved paths, with original finding counts. |
| Specific URI finding identity | low, patched | Raw-preserved Debug and Release tests now assert exact lines 96 and 98. |
| Machine-readable withdrawal | low, patched | Every lineage row declares affected-claim disposition, reference locations and preserved independent scope. |
| Selection origin | low, patched | Decision bindings identify Hexalith.Projects provenance and standalone local copies; original selection bytes remain intact. |

No findings deferred. Follow-up review confirmed the corrections; its additional reference-checkout case was reproduced and fixed under the same preservation root cause.


## Completion Verification

- The complete tracked-content gate passed in 706.842 seconds. Its containing full-class attempt ran 940 cases: 938 passed and two malformed random fixtures failed; the actual native XML and failures remain recorded. Both a new neighboring-path fixture and an inherited runtime-source fixture emitted unquoted Base64, which can be a valid C# identifier. Corrected the fixtures to quote generated literal values and added a deterministic identifier/literal boundary control. No detector/retirement policy changed.
- Final Debug source build passed with zero warnings/errors. All 940 remaining guard cases passed after correction (zero failures/errors/skips); the already-passing expensive tracked-content gate was not repeated. Canonical ingestion separately passed on the corrected sources, final exact Git attributes and all new delivery files. This combination records the actual verification scopes rather than labeling the earlier full-class failure green.
- All 83 immutable original/reference hashes survive actual Windows checkout filters. All 22,916 unrelated tracked files, all four prior registry entries, original Project Story 6.4/sprint state, fixed P1R acceptance and package/source pins are preserved.
- Actual commands, source/assembly hashes, successful/failed/interrupted outcomes, byte-preservation/checkout controls and the strict native test result are retained under `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-4-historical-retirement-20261018/`. Native XML remains at the exact recorded temporary paths with hashes; no passing TRX or G-4 evidence was fabricated.
- Local retirement prerequisite complete. Hosted CI/P1R execution and owner/package/release acceptance remain separate. Project Story 6.4 stays blocked; no commit, push, dependency update or publication occurred.
