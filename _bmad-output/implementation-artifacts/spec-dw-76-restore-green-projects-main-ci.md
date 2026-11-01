---
title: 'DW-76: Restore Projects main CI'
type: 'bugfix'
created: '2026-11-01'
status: 'in-progress'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Restore Projects main CI after the 353cfd33..ded1e704 submodule sync. Fix the package consumer mismatch in its owning repository and reproduce the PostgreSQL two-sidecar qualifier once. Record an EventStore defect in EventStore's deferred-work ledger. Preserve Epic 6 completion: no story re-verification, new evidence receipts or new gates. Run tests outside the sandbox, serially, with CI=true and an isolated NUGET_PACKAGES. Ask before committing or pushing. Completion requires green CI on main.

</frozen-after-approval>

## Supersession (2026-11-01)

The user's later explicit maintenance/release authorization, recorded in [the current maintenance spec](spec-release-maintenance-2026-11-01.md), supersedes this investigation's pending commit/push approval and manifest-only/G-6-immaterial execution assumptions. That broader operation synchronizes runtime-owning dependencies and requires fresh current-source G-6 qualification plus exact-current-main CI before release. The original intent, investigation, failed runs and narrow-scope conclusions below remain historical evidence; they do not waive current automated checks or require another human approval pause.

## Implementation Notes

- Projects owns tools/release-packages.json. Builds commit 69f07af0aadd4166f75f6b965ebfac9b4fd51de2 raises the shared EventStore catalog from 3.129.0 to 3.130.1; transitive pinning promotes Contracts into four packed dependency graphs. Update those four manifest expectations and validate the existing pack and isolated consumer lane. No public API, catalog rollback or new gate.
- CI run 41298330907 also records an EventStore child-process Contracts assembly load failure and missing older package files in the support build. Reproduce and record upstream ownership in the existing ledgers; keep all diagnostics under /tmp.
- Commit and push approval remains required by the user. Main CI completion stays pending until verified after publication.
- Existing validation passed: serial Release CI solution build (zero warnings/errors), 19 package-tool tests, pack/validate for all five candidates and all five isolated consumer compilations. Commands and outcomes are summarized in DW-76; temporary artifacts remain under /tmp/dw76.
- One actual live case reproduced the hosted Contracts 3.130.1.0 startup failure (1 failed, no skips) using Dapr 1.18.2 and the pinned PostgreSQL image. The preceding support build reproduced both missing 3.129.0 MSB3030 errors. Recorded the defect as EventStore DW-545 without changing runtime or gate code.
- The .NET 10 MTP CLI rejected the requested -m:1 syntax; its /m:1 form reached a handshake failure (exit 5, zero tests). Followed the existing baseline fallback: serial -m:1 project builds then the built xUnit assembly with the exact single-method selector. CI=true and the isolated package cache were retained throughout.
- Main CI remains unverified until the user authorizes committing and pushing. The package-manifest-only change does not affect existing G-6 material inputs; publishing the separate EventStore ledger does not require updating the Projects gitlink. Runtime qualification remains blocked by EventStore DW-545 for material changes or schedules.
- Launcher limitation also affects the attempted Projects.Contracts CI shard: `CI=true NUGET_PACKAGES=/tmp/dw76/nuget dotnet test --project tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj --configuration Release --no-build /m:1 --no-ansi --no-progress` and the retry with `--report-xunit-trx --report-xunit-trx-filename contracts.trx --results-directory /tmp/dw76/contracts-mtp --no-progress` both exited 5 at handshake with zero tests. No Projects .NET shard pass is claimed; the successful results above cover the build and package/consumer checks.

## Review Triage Log

- Low, patched: package-check script names omitted required arguments. Added the exact existing invocations to DW-76.
- Medium, patched: EventStore reproduction omitted the complete override values and control-plane setup. Added those details directly to DW-545; no new receipt or test run.
- Low, patched: verification notes omitted the Projects.Contracts launcher failures. Recorded both zero-test exit-5 attempts separately from successful package checks.
