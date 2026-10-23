---
title: 'Resolve five deferred advisory policy obligations'
type: 'chore'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five deferred Story 6.4 policy entries still describe the earlier strict resolution design: server-owned action, attachment denial, Conversation membership/session fallback, usable Partial evidence, and mandatory Folder admission. Their ledger state obscures the later approved advisory contract.

**Approach:** Add focused regression coverage and consumer guidance for the current advisory boundaries, then resolve the five entries against the approved October 21 scope reset and October 22 Folder decision. Distinguish implemented safeguards from superseded requirements. Preserve independent context, confirmation, deployment and qualification gates. Validate, independently review, commit and push main as requested.

</frozen-after-approval>

## Implementation Notes

- Baseline: `e29712287a4dc691e24839e454e22a0854a7c55f`, obtained directly from Git; clean main. No intent gaps or new irreversible operation beyond the explicitly requested Git publication. Small root-only test/documentation footprint; no dependency, runtime API or submodule changes.
- Code map: add `tests/Hexalith.Projects.Server.Tests/Queries/ProjectAdvisoryQueryTests.Policy.cs` alongside the existing partial class. Reuse `SupportedProjectReadFixture`, guarded persisted store, owner directory, exact authorization callback and current executor helpers. Update `docs/supported-project-reads.md` and exactly five matching entries in `deferred-work.md`.
- Authority: the approved exception near the start of `ARCHITECTURE-SPINE.md` and `sprint-change-proposal-2026-10-21-simplify-story-6-4.md` permit lagging metadata and explicitly exclude AD-32 context authority. Story 6.4's fourth-pass option 3 (2026-10-22) permits null/Pending-null Folder Conversation suggestions under exact Project Read authority. Do not restore the superseded stricter admission or infer actual membership from a hint.
- Tests will check literal `projects:read` with exact Tenant/actor/resource, denied input versus protected owner outcomes, no implicit session fallback or selected candidate, advisory output without context authority despite absent Setup/lag, and Folderless versus bound-Folder authorization with final revocation controls. Persisted Project/index bytes and ETags must remain unchanged in these read fixtures.
- Verification: build the individual Server.Tests project in local Debug/source and Release/package modes; run the focused advisory class in both modes and the complete package-mode server suite. Check Git whitespace and commitlint. These controlled fixtures establish local regression behavior, not live owner or production qualification.
- Pre-change AppHost baseline: exact root AppHost started and was inspected through Aspire CLI. Security was Running; dependent resources were Waiting. The owned AppHost stopped successfully before builds. Local state capture: `/tmp/five-advisory-policy-aspire-baseline.json`; no ready topology is claimed.

- Implemented 16 unconditional cases in the policy companion. The Release/package build and full server suite pass: 1,178 total, 1,177 passed, zero failures, one existing opt-in Story 6.1 live Dapr persistence skip. Release advisory focus: 135/135 with zero skips. The Debug/source build also passes; both builds have zero warnings/errors. Git whitespace passes; fetch found zero divergence from origin/main.

## Reproducible Verification

```bash
dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Release -p:UseHexalithProjectReferences=false -m:1 -p:NuGetAudit=false > /tmp/five-advisory-policy-release-build.log 2>&1
dotnet tests/Hexalith.Projects.Server.Tests/bin/Release/net10.0/Hexalith.Projects.Server.Tests.dll -class '*ProjectAdvisoryQueryTests' -noColor > /tmp/five-advisory-policy-release-focused.log 2>&1
dotnet tests/Hexalith.Projects.Server.Tests/bin/Release/net10.0/Hexalith.Projects.Server.Tests.dll -noColor > /tmp/five-advisory-policy-release-suite.log 2>&1
dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Debug -m:1 -p:NuGetAudit=false > /tmp/five-advisory-policy-debug-build.log 2>&1
dotnet tests/Hexalith.Projects.Server.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.Tests.dll -class '*ProjectAdvisoryQueryTests' -noColor > /tmp/five-advisory-policy-debug-focused.log 2>&1
git diff --check
```

Temporary logs are local developer verification, not acceptance or qualification evidence. The first build exposed nullable ETag and private-helper ConfigureAwait diagnostics; those were fixed before the passing runs above.

## Review Triage Log

- BH1 — medium, patched: the original action-argument controls did not isolate permission enforcement from scope admission. Four added cases retain projects.read while omitting projects:read or substituting projects:list; both query kinds must safely deny before validator, owner or persisted reads.
- BH2 — medium, patched: the rejected Folder lacked a matching Project in the new mixed-input test. A second persisted Project now binds that exact denied Folder; only the allowed peer may score, and both Project records plus index bytes/ETags are checked unchanged.
- BH3 — medium, patched: closure wording conflated a protected Project target with a protected Conversation. The ledger and consumer mapping now explain collection-target advisory omission versus the independent direct Project safe-denial contract; existing authenticated HTTP tests retain the latter.
- BH4 — medium, patched: owner-rejected was too broad. The guidance and ledger now explicitly name Denied; other non-Accepted Folder outcomes retain incomplete coverage.
- BH5 — low, patched: linked the exact Story 6.4 record containing the approved October 22 fourth-pass option 3 so the Folder exception is independently traceable.

## Final Verification

The corrected tree adds 20 unconditional policy regression cases. Debug/source and Release/package builds both pass with zero warnings/errors. Each focused advisory run passes 139/139 with zero skips. The complete Release/package server suite reports 1,182 total, 1,181 passed, zero failures, one existing opt-in Dapr persistence skip. These final counts supersede the provisional pre-review counts above. All five review corrections were independently verified; no material finding remains and none was deferred. Exactly five existing ledger entries were resolved, with the original evidence retained and superseded policy distinguished from implemented safeguards. Git whitespace, required file endings and commitlint pass. A final nonrecursive fetch found zero main divergence.
