# Preparation review triage — 2026-10-28

Blind Hunter: N = min(floor(sqrt(63.384) + 1), 10) = 8. All eight findings were checked against current source/evidence before action; no finding was dropped. The review concerned the preparation candidate and evidence, not Story completion. No additional review layer is configured for this oneshot route.

| Finding | Verdict and disposition | Evidence / correction |
| --- | --- | --- |
| 1. Contradictory Aspire setup 13.6.1 / candidate 13.6.3 / AppHost 13.6.4 | medium, patch | Actual embedded catalog requires 13.6.3; `apphost.cs` still pins 13.6.4. README now distinguishes both and retains the integration blocker. No AppHost or dependency pin changed. |
| 2. Obsolete Dapr setup 1.18.0 / 1.18.2 | medium, patch | Candidate embedded catalog requires CLI 1.18.2 / runtime 1.18.7. Prerequisites, official download/checksum URLs, directories and environment examples now agree. Read-only official release asset queries confirm the URLs. |
| 3. Old executables do not all enforce range | low, patch | Restored the qualification for tools before 4.31.0 and the instruction to use the exact local manifest. |
| 4. Audit rebased hashes and overwrote receipts | medium, patch | Final verifier reads immutable `reviewed-source-baseline.json` and `official-tool-identity.json`, compares source/frozen/prefix hashes and emits only stdout. Initial verifier/observations remain separately retained. |
| 5. Weak qualification inventory binding | medium, patch | Final verifier compares official asset SHA-256/size, exact clean source revision, releaseEligible=true, empty ineligibilityReasons, all three passed result fields and exact-source successful CI/release runs. |
| 6. Audit depended on original absolute paths/cache | medium, patch | Workspace derives from receipt location. Actual ZIP verification accepts `--archive`; no original cache is needed. Offline source/retained-evidence mode explicitly reports that payload bytes were not freshly reverified. |
| 7. Hardcoded exits and collapsed diagnostic causes | medium, patch | Actual `commands.json` exits and public JSON are checked; an exact Counter binds four messages/fields, including missing-tag and dirty-checkout HXW011 separately. Source Commons SHA/path mismatch is checked separately. |
| 8. Any failed pin test counted as expected | medium, patch | Final verifier checks exact test type/method, one failure, missing `v4.38.1^{commit}` and unknown-revision cause. It separately checks the real committed owner mismatch using the permitted sibling Builds checkout. No acceptance credit is awarded. |

Parent review fixes were checked by the read-only audit with the fresh official signed ZIP, Platform whitespace check and unchanged source hashes. Product test results remain 126 passing checks, one owner-pin failure and three separately blocked broad-build analyzer errors; no skipped/failing gate was converted into a pass.

The original reviewer independently confirmed closure of all eight findings and reran the final signed-archive audit with exit 0. Parent positive/offline and three tampering controls also passed; the auditor changed no input evidence.
