# Story 6.1 bootstrap authority follow-up — 2026-11-05

Story 6.1 remains **in-progress**. The fixture bootstrap token now includes the exact `global-administrators:bootstrap` permission required by the current Tenants owner. Its signature/credential regression failed before the change and passes afterward; all **119 launcher controls pass**. Existing uncommitted G1 patches are preserved.

## Current verification

| Lane | Result |
| --- | --- |
| Focused issuer before repair | 0 passed / 1 failed |
| Focused issuer after repair | 1 passed / 0 failed |
| Launcher controls | 119 passed / 0 failed |
| Fresh distributed Integration build | 0 warnings / 0 errors |
| Fresh native duplicate method | 0 passed / 1 failed / 0 skipped/errors/not-run |
| Package-mode Server.Tests Release build | Restore failed: 3 NU1109 errors, 0 warnings |

The distributed invocation was:

```bash
MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 DOTNET_PROCESSOR_COUNT=2 python3 tools/qualification/run_supported_project_reads.py --regressions --timeout 900 --evidence _bmad-output/implementation-artifacts/evidence/story-6-1-bootstrap-authority-20261105/acceptance
```

Bootstrap now creates its original receipt; the native test fails at **`Hosted bootstrap receipt binding failed.`** before Project reads. No later group or full regression ran. The removed private receipt's exact HTTP status was not retained, so no status is inferred as an observed result. `acceptance/duplicate/distributed.xml`, `distributed.log`, `build-35.log` and `run.json` retain the actual failure. No acceptance assertion was weakened.

The package invocation was:

```bash
CI=true MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 DOTNET_PROCESSOR_COUNT=2 dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj -c Release -m:1 -p:NuGetAudit=false
```

`build-package.log` confirms Tenants.Client 7.0.0 requires EventStore Client/Contracts >=4.0.4 while Projects selects 3.133.0. No dependency override was applied. The passing mapped Debug build is the focused fallback evidence; it does not certify package compilation.

## Why replan is required

Further ordinary-source investigation identified public fixture scope mismatches. The launcher bootstrap JWT and the distributed fixture's Administrator JWTs include the reserved storage grant `eventstore:tenant=system`. Current `TenantCanonicalizer` rejects that grant before `LegacyTenantStorageScopeAdapter` can map an explicit platform operation. `SeedAsync` still submits Tenants commands using public Tenant `system`, and status reads omit the now-required explicit scope headers. Current owner declarations require `platform:tenants` or `platform:global-administrators` with exact finite operation permissions. These are public credential/request issues; persisted events, keys and historical Tenant assertions must retain `system`. The CLR command-name alias remains supported and is not a defect.

Global-administrator source proof also needs the existing exact bound-workload SDK transport; the human stream endpoint's Tenants-only platform gate must not be bypassed or widened locally. Missing owner capability must remain an explicit blocker.

[Independent review and complete triage](review-triage.md) finished G2–G4 and confirmed a denial-observer export-completion race. Its correction needs a delayed-export control and a suppressed-root control. These fixture/observer changes extend the initial two-file repair, so the Build workflow returned the spec to draft before further coding.

## Source and cleanup

`summary.json` records full Projects/direct dependency commits, the frozen manifest hash, actual totals and final source equality. The frozen source still matches the current code. `commands.json` records exact commands and outcomes.

The startup-only baseline was intentionally stopped after resource inspection; it is recorded as interrupted and supplies no acceptance. Both baseline and failed acceptance cleanup removed their owned AppHost, six sidecars, four containers and private material while preserving unrelated resources. A fresh final check confirms all four acceptance containers are absent and `aspire ps` returns no session. Historical packets remain unchanged.

No sibling source, package pin, public route, production enrollment, Git staging, commit, push or submodule revision changed. The follow-up plan is `../../spec-6-1-list-and-open-projects-through-supported-authenticated-paths-12.md`.
