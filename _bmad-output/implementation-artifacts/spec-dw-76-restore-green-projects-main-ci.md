---
title: 'DW-76: Restore Projects main CI'
type: 'bugfix'
created: '2026-11-01'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Restore Projects main CI after the 353cfd33..ded1e704 submodule sync. Fix the package consumer mismatch in its owning repository and reproduce the PostgreSQL two-sidecar qualifier once. Record an EventStore defect in EventStore's deferred-work ledger. Preserve Epic 6 completion: no story re-verification, new evidence receipts or new gates. Run tests outside the sandbox, serially, with CI=true and an isolated NUGET_PACKAGES. Ask before committing or pushing. Completion requires green CI on main.

**Scope decision (2026-11-13):** The user selected closure of the original November 1 incident using the verified November 2 results. Update this spec and Projects DW-76 tracking only. Today's separate CI failures and closure of EventStore DW-545 are outside this operation.

</frozen-after-approval>

## Supersession (2026-11-01)

The user's later explicit maintenance/release authorization, recorded in [the current maintenance spec](spec-release-maintenance-2026-11-01.md), supersedes this investigation's pending commit/push approval and manifest-only/G-6-immaterial execution assumptions. That broader operation synchronizes runtime-owning dependencies and requires fresh current-source G-6 qualification plus exact-current-main CI before release. The original intent, investigation, failed runs and narrow-scope conclusions below remain historical evidence; they do not waive current automated checks or require another human approval pause.

## Implementation Notes

### Historical investigation (2026-11-01)

- Projects owns tools/release-packages.json. Builds commit 69f07af0aadd4166f75f6b965ebfac9b4fd51de2 raises the shared EventStore catalog from 3.129.0 to 3.130.1; transitive pinning promotes Contracts into four packed dependency graphs. Update those four manifest expectations and validate the existing pack and isolated consumer lane. No public API, catalog rollback or new gate.
- CI run 41298330907 also records an EventStore child-process Contracts assembly load failure and missing older package files in the support build. Reproduce and record upstream ownership in the existing ledgers; keep all diagnostics under /tmp.
- Commit and push approval remains required by the user. Main CI completion stays pending until verified after publication.
- Existing validation passed: serial Release CI solution build (zero warnings/errors), 19 package-tool tests, pack/validate for all five candidates and all five isolated consumer compilations. Commands and outcomes are summarized in DW-76; temporary artifacts remain under /tmp/dw76.
- One actual live case reproduced the hosted Contracts 3.130.1.0 startup failure (1 failed, no skips) using Dapr 1.18.2 and the pinned PostgreSQL image. The preceding support build reproduced both missing 3.129.0 MSB3030 errors. Recorded the defect as EventStore DW-545 without changing runtime or gate code.
- The .NET 10 MTP CLI rejected the requested -m:1 syntax; its /m:1 form reached a handshake failure (exit 5, zero tests). Followed the existing baseline fallback: serial -m:1 project builds then the built xUnit assembly with the exact single-method selector. CI=true and the isolated package cache were retained throughout.
- Main CI remains unverified until the user authorizes committing and pushing. The package-manifest-only change does not affect existing G-6 material inputs; publishing the separate EventStore ledger does not require updating the Projects gitlink. Runtime qualification remains blocked by EventStore DW-545 for material changes or schedules.
- Launcher limitation also affects the attempted Projects.Contracts CI shard: `CI=true NUGET_PACKAGES=/tmp/dw76/nuget dotnet test --project tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj --configuration Release --no-build /m:1 --no-ansi --no-progress` and the retry with `--report-xunit-trx --report-xunit-trx-filename contracts.trx --results-directory /tmp/dw76/contracts-mtp --no-progress` both exited 5 at handshake with zero tests. No Projects .NET shard pass is claimed; the successful results above cover the build and package/consumer checks.

### Resumption investigation (2026-11-13)

- The completed [release-maintenance operation](spec-release-maintenance-2026-11-01.md#final-public-release-closure-2026-11-02) repaired the original failure. GitHub independently confirms [main CI 41376917554](https://github.com/Hexalith/Hexalith.Projects/actions/runs/41376917554) succeeded at `e6eb06a87f1a924c8815306683c6299c41d5c3d4` on November 2: workflow policy, package build/tests, generated artifacts, P1R replay and G-6 all passed. That commit is an ancestor of local main; its manifest has four EventStore.Contracts 3.132.0 expectations.
- Existing [PostgreSQL test results](release-evidence/nuget-trusted-publishing-1.2.3-e6eb06a87f1a924c8815306683c6299c41d5c3d4/g6-test-results.json) record the real two-sidecar qualifier passing (1 test, 1 passed, zero failed/skipped). The adjacent `exact-source-ci.json` and `g6-result.json` retain the successful CI and qualification. Those results apply to the November 2 source, not today's checkout; no new qualifier run or receipt was created.
- The scope question was resolved by the user's choice to close original DW-76. Today's separate failures remain outside this closure: [CI 43510207557](https://github.com/Hexalith/Hexalith.Projects/actions/runs/43510207557) could not fetch an EventStore revision, and [CI 43403553316](https://github.com/Hexalith/Hexalith.Projects/actions/runs/43403553316) reported net10.0/net11.0 package incompatibilities and a missing Builds fixture. No current-main success is claimed.
- This spec and [DW-76](deferred-work.md#dw-76-restore-green-projects-main-ci-after-the-2026-11-01-submodule-sync) are reconciled to done using that bounded historical completion. The original failed investigation is preserved, and [EventStore DW-545](../../references/Hexalith.EventStore/_bmad-output/implementation-artifacts/deferred-work.md#dw-545-keep-one-eventstore-assembly-identity-through-the-postgresql-qualifier-and-support-source-builds) remains independently tracked. This operation changes only the two Projects tracking documents and leaves them uncommitted.
- Closure validation: read-only assertions confirm the five successful CI jobs, matching full source identity, qualified G-6 and PostgreSQL 1/1; all local links resolve, historical notes and receipts are unchanged, and unrelated ledger entries are byte-identical. `git diff --check` passes. No application tests were rerun for this tracking-only correction.

## Review Triage Log

- Low, patched: package-check script names omitted required arguments. Added the exact existing invocations to DW-76.
- Medium, patched: EventStore reproduction omitted the complete override values and control-plane setup. Added those details directly to DW-545; no new receipt or test run.
- Low, patched: verification notes omitted the Projects.Contracts launcher failures. Recorded both zero-test exit-5 attempts separately from successful package checks.
- Closure review 1 — low, patched: the spec remained in-progress while the ledger was done during review; finalized the spec to done so the completed tracking agrees.
- Closure review 2 — low, patched: the closure named DW-545 without a navigable owner reference; added a direct link to the existing EventStore ledger entry, without changing that entry or claiming upstream closure.
