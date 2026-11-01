---
title: 'Wait for an owned Redis SAVE acknowledgement within the original deadline'
type: 'bugfix'
created: '2026-11-01'
status: 'in-review'
route: 'dispatch'
baseline_commit: 'fa978a283b29deb05a9c988ff54e3299de9606fc'
context: []
review_loop_iteration: 0
---

<frozen-after-approval reason="user authorized all actual CI repairs without human validation">

## Intent

Repair the demonstrated published-compatibility runner race when Redis returns the exact transient `ERR Background save already in progress` reply with exit0. Retry that reply within the existing single three-minute command budget, retaining every real command observation, and require a real final OK before copying a dump. Validate the complete ordered retry/acknowledgement sequence without accepting unrelated failures or changing any historical package result.

## Boundaries & Constraints

Preserve exact owned-container reservation, original total deadline/cancellation, nonzero/truncation/error rejection, all command receipts, backup path/hash/byte observations, fresh restore volume and copy-before-start/destruction ordering. Never accept a busy reply as an acknowledgement, select an arbitrary OK while ignoring prior errors, reset the deadline per attempt, disable Redis automatic snapshots or change the pinned provider profile. Do not modify HistoricalMatrixMeasurementContract historical outcomes. No Git operations, nested initialization, live Redis/Docker/backend or local topology. Parent owns commits and hosted verification. The parent already rendered bmad-build; implement this step03 directly without invoking/rendering the skill again. Never load skills under references.

## I/O & Edge-Case Matrix

| Input | Expected |
|---|---|
| Single real owned OK | Existing successful acknowledgement/copy chain |
| Ordered exact busy replies then one OK | Retry under remaining original deadline; retain/validate every receipt, then allow unchanged backup chain |
| Busy-only, unrelated error, nonzero, truncation or cancellation | No successful SAVE or copied-dump qualification |
| Duplicate OK, busy after OK, overlaps, foreign ACK, copy before ACK, renewed deadlines/total span over3min | Evidence validator refuses |

</frozen-after-approval>

## Code Map

- Owner `references/Hexalith.EventStore`, baselinefa. Read its AGENTS and relevant guidance/build/test configuration plus permitted umbrella baseline. Another agent owns Server/ServiceDefaults projection code/tests; parent owns CommandDispatchDiagnosticsTests sentinel. Touch only the published-compatibility runner, backup evidence contract and directly relevant tests. Coordinate any builds with parent; other source/UI builds are serialized.
- `tests/Hexalith.EventStore.PublishedCompatibility.Tests/QualificationRuntime.cs:212`, RequireRedisSaveAsync checks `_containerNames` then performs one RequireAsync docker exec redis-cli SAVE; result.Trim must equal OK. RequireAsync's default finite budget is3minutes. RunAsync records actual commands and accepts a command-specific remaining timeout/cancellation. Keep ownership checks in production, not in a synthetic seam.
- A small internal retry-policy helper/operation+clock seam is acceptable for deterministic no-backend tests; production must still execute its exact owned command through the existing measured runner. Prefer monotonic elapsed time/TimeProvider and one shared deadline; do not introduce a general retry framework.
- `HistoricalMatrixBackupContract.cs:42` currently selects exactly one zero-exit SAVE receipt, so a correct busy→OK retry would currently fail. Validate all scoped SAVE attempts, including failed/nonzero/truncated receipts rather than filtering them away, with exactly one final OK and only preceding exact busy replies. Preserve the remaining owned command chain and byte evidence.
- `HistoricalMatrixSyntheticFixture.Create/Commands/Observations/Files/RewriteSummary/Validate`: ProviderBackup582 creates SAVE593 followed by copy/fresh-volume/create/restore/start/destruction. Reuse this full fixture for positive and negative receipt controls.
- `HistoricalLoopTwoEvidenceTests.ProviderBackupRequiresActualFilesAndOwnedSaveRestoreDestruction`: existing12 mutations; `HistoricalLoopThreeEvidenceTests.FailedRedisSaveCannotQualifyAPreexistingDump`: exit-zero permission error; `QualificationRedisSaveTests.cs`, `QualificationRuntimeLedgerTests.cs` and `HistoricalMatrixMeasurementContractTests.KnownDeficitRejectsEveryUnmeasuredOrNewCause`: reuse applicable controls. The historical expected3.70.1 floor/proof deficits remain unchanged.

## Tasks & Acceptance

- [x] Add exact-busy-only acknowledgement retry under one original3min deadline, preserving real observations and failure/cancellation semantics.
- [x] Extend backup receipt validation to the complete ordered bounded busy→OK sequence while retaining all ownership/backup/restore checks.
- [x] Add deterministic helper/runtime policy and full evidence-chain controls for every matrix row, and run only affected no-backend tests/builds. Parent owns all live/hosted checks.

Given Redis is temporarily saving, when an exact busy reply precedes actual OK within the original budget, then the real dump-copy chain can continue. Given any unrecognized failure or invalid receipt sequence, when validating backup, then no success is granted. Given cancellation or budget expiry, when waiting, then it stops without manufacturing an acknowledgement.

## Implementation Notes

Actual run41352994284 job124986999357 fails Published31282QualificationMatrixAsync, post-upgrade-restore/3.70.1. Raw `/tmp/projects-release-maintenance/event-fa-published-compatibility-failed.log`, lines22524–22558: owned docker exec SAVE started22:10:03.1071848Z/ended03.1499322Z, exit0, stdout `ERR Background save already in progress\n\n`, empty stderr, untruncated. Same provider returns OK at22:10:05.4157463Z in the next scenario. The failed scenario ends before backup copy with95 assertions and no assertion failures. This is a provider coordination race, not new historical compatibility. Parent preserves the failed evidence and reruns actual matrix after reviewed repair.

## Review Triage Log

## Local verification

Debug/source-pinned PublishedCompatibility build passed0warnings/errors. Five affected no-backend classes passed250/250 with zero failures/errors/skips/not-run in330.424s, including real Python-process command ledger checks, exact-busy policy and complete synthetic backup/measurement negatives. Historical expected outcomes, existing live Redis tests and pinned provider profile remain unchanged. Logs /tmp/owned-redis-save-build.log, /tmp/owned-redis-save-tests.log/.xml. Actual hosted Redis SAVE/matrix qualification remains pending.

## Combined review closure

The combined three-layer review is complete. All findings were individually triaged in spec-durable-projection-command-response-2026-11-01.md. No further code changes apply to this scoped repair. Existing local verification remains passing; hosted exact-source qualification is pending.
