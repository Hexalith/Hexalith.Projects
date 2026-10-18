---
title: 'Story 6.4 prerequisite: keep P1R sentinel alive through verification'
type: 'bugfix'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '771f55e131912f9182098a00d3d4e16e8cf4f61e'
owner_repository: 'references/Hexalith.EventStore'
owner_baseline_commit: '269b20d58236f07228bc2afd54807b94bd96f37b'
context:
  - '_bmad-output/implementation-artifacts/spec-6-4-resolve-projects-with-transient-current-explanations.md'
---

<frozen-after-approval reason="existing prerequisites-first decision; bounded reversible tooling repair">

## Intent

**Problem:** P1R preparation creates a shared sentinel with a fixed 30-second sleep. Repeated source verification can outlive it and falsely fail preservation checks. Live EventStore main `ab8fa115c486875be3a22860a273d5d68a26c749` fails the external-SIGINT test; a controlled 31-second verification delay reproduces expiry in the shared published-runtime process controls.

**Approach:** Replace the fixed lifetime with a shared parent-owned lifetime in `tools/p1r_qualification.py` and `tools/p1r_qualification_runtime.py`; add focused regressions in their existing test suites. The sentinel must survive verification latency, exit on parent disconnect, and remain subject to existing explicit termination, identity checks and bounded cleanup. Preserve strict failed/interrupted receipts, source binding, required controls and non-qualification semantics. Work only on tooling and focused tests; retain historical evidence, package pins, fixed acceptance, rollback policy, Story 6.4 and sprint states. CI's separate secrets-scan failure, publication, package qualification, owner contracts and production resolution remain outside this small repair.

</frozen-after-approval>

## Implementation Notes

- Resumed Story 6.4 under its recorded prerequisites-first decision. No intent gaps or irreversible action; this bounded tooling fix uses the oneshot path. Story 6.4's original draft and frozen intent remain authoritative and blocked.
- Read-only recheck: both live EventStore CI 39104649105 and P1R preparation 39104648854 failed; all five official package indexes stop at 3.124.0 and omit proposed 3.125.0. CI separately reports `SecretsProtectionTests.TrackedReusableContent_DoesNotContainUsableSecrets` failing with 2,812 violations; no secret values were copied here.
- Diagnostic before repair: real shared process controls with one injected 31-second source-verification pause took 32.94 seconds; sentinel before=true, after=false, released=true; all six rows failed. This demonstrates lifetime expiry only and is not published-package qualification.
- Approved callable metadata-only Conversations/Folders batches, actor-scoped RTI, `.config/dotnet-tools.json`, `module/hexalith-projects.module.json` and exact Story 6.1 entry-gate evidence remain unavailable. Story 6.3 explicitly deferred Refresh and RTI and grants no exception for 6.4.

- Reused `start_shared_sentinel` in both entry points. The sentinel blocks on its parent's private pipe and exits on EOF, so slow source verification cannot expire it and abrupt parent exit cannot strand it. Existing explicit signal termination, two-second waits, identity validation and receipt schemas are unchanged.
- Added real 31-second latency regressions for both process-control entry points, each validating all 38 existing process assertions. Added an abrupt-parent-exit test with bounded polling, kill and reap; no unbounded Popen context-manager wait remains.
- Final affected suites passed: preparation 37/37 and published-qualification tooling 62/62. An initial new-test error passed timestamp strings where the existing validator requires parsed UTC datetimes; corrected through `validate_times` and reran the full affected suite successfully.
- Retained the exact pre-fix diagnostic command/output, baseline source hashes, final local test results and source patch in `evidence/6-4-prerequisite-p1r-sentinel-lifecycle-20261018/`. These are local diagnostic/tooling records, not G-4 or published-family qualification evidence.
- At the implementation handoff, no commit or push was performed because the user had not yet requested those Git mutations. All existing package/source pins, historical qualification packets, fixed acceptance, original Story 6.4 draft and sprint statuses remain unchanged.

- Subsequent user authorization: "push all changes and give me next step prompt". Committed the four owner files with validated Conventional Commit text and intact commit hooks; rebased cleanly onto live EventStore main `ab8fa115c486875be3a22860a273d5d68a26c749`, producing `794369b30c2f3615a7f1d70a5beb1935f6470b36`. The three upstream commits do not overlap the changed files. After rebasing, preparation 37/37 and published-tooling 62/62 passed again. EventStore push succeeded; the separately requested Projects commit records its new gitlink and evidence. Package versions, fixed acceptance, original Story 6.4 draft and sprint status remain unchanged. Remote CI remains pending.

## Review Triage Log

- **medium / patched:** The original new disconnect test used Popen's context manager, whose exit can wait indefinitely after a timeout. Replaced it with an abrupt-owner-exit test and bounded WNOHANG polling plus kill/reap cleanup.
- **medium / patched:** Closing stdin in a live owner did not prove abrupt parent death. The disposable owner now calls `os._exit(0)`; the actual sentinel is adopted by a subreaper and must exit with status zero within two seconds. Final preparation suite passed.
- **low / patched:** The pre-fix timing and failures lacked a retained reproduction command. Added `diagnostic-before.json` with the exact command, stdout, owner revision and pre-edit source hashes; it explicitly distinguishes injected latency from actual CI timing.

## Verification

- `python3 -m unittest discover -s tools/tests -p test_p1r_qualification.py` (EventStore) — 37 passed in 55.506s.
- `python3 -m unittest discover -s tools/tests -p test_p1r_published_qualification.py` (EventStore) — 62 passed in 44.371s.
- `python3 tools/planning/validate_production_authority.py --story-id 6.4` (Projects) — passed; scheduling authority only.
- `git diff --check` — passed in both repositories; exact changed-file allowlist and protected story/sprint/acceptance checks passed.

- `dotnet tool run hexalith-module test --profile reads --filter Story=6.4` (Projects) — exit 1: `Cannot find a tool in the manifest file that has a command named 'hexalith-module'.`

Story 6.4 remains blocked. Next prerequisites include EventStore's independent failing secrets scan, exact published successor selection/qualification, approved owner metadata/RTI contracts, and the existing P0/P2/P3/P4/G-4/G-6/readiness decisions.
