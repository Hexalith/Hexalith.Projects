---
title: 'Migrate Projects releases to NuGet Trusted Publishing'
type: 'chore'
created: '2026-09-20'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '1d349dca3a5a3e834c306d679eed6716dc19ef97'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-fix-ci-cd-run-release-verify-nuget-publication.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Projects stable publication still passes a long-lived `NUGET_API_KEY` into Semantic Release, and the key used for `v1.0.0` is expiring. The current publish step runs inside a cross-repository reusable workflow, while NuGet's open reusable-workflow defects can reject that OIDC identity with HTTP 401.

**Approach:** Move the protected release job into the caller-owned Projects workflow, preserve its exact-source, build, freeze, and package safeguards, and obtain a short-lived key from the SHA-pinned official `NuGet/login` action immediately before Semantic Release. Keep `release.config.cjs` consuming `NUGET_API_KEY`, but bind that process variable only to the login step output.

## Boundaries & Constraints

**Always:** Keep release manual, non-cancellable, limited to the current green `main` SHA, protected by the `production` environment and exact `HEXALITH_RELEASE_PUBLISH_ENABLED=true` freeze gate. Use immutable action and Hexalith.Builds SHAs, Release/package-only restore and build, the five-package manifest/count gate, npm signature verification, and the existing semantic-release lifecycle. Configure the future NuGet policy for owner `Hexalith`, repository `Hexalith.Projects`, workflow `release.yml`, environment `production`, and only the five manifest package IDs; supply the NuGet profile name through `NUGET_USER`.

**Never:** Request the NuGet credential inside a reusable workflow; read a long-lived NuGet key; persist, log, or pass the temporary key beyond the Semantic Release step; weaken source freshness, environment approval, publication freeze, package validation, or action pinning. Do not dispatch Release, enable publication, change NuGet.org policies, create/delete secrets, publish packages, or remove the old key during this implementation.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Frozen release | Publish variable is absent or not exactly `true` | Build completes; login and Semantic Release are skipped | Successful frozen no-op with notice |
| Trusted publish | Exact green live `main`; environment approved; policy and `NUGET_USER` match | OIDC exchange yields a masked one-hour key; Semantic Release validates and publishes exactly five packages | Existing package and source gates remain blocking |
| Identity mismatch | Missing username or NuGet policy claim mismatch | Login fails before Semantic Release receives any credential | Stop before package or GitHub release publication |
| Stale source | `main` advances during setup | Source revalidation fails before login | Redispatch only after CI is green for the new tip |

</frozen-after-approval>

## Code Map

- `.github/workflows/release.yml` -- replace the cross-repository reusable release call with the caller-owned protected job; reuse its current pinned checkout/setup/build/freeze/source checks and add pinned `NuGet/login` immediately before publication.
- `release.config.cjs` -- retain the five-package count check and `$NUGET_API_KEY` push interface; the variable becomes process-local short-lived credential plumbing, not a stored secret.
- `tests/tools/run-ci-workflow-gates.ps1` -- update release topology assertions and require caller-owned OIDC login, immutable pins, guarded step ordering, temporary-output binding, and complete absence of `secrets.NUGET_API_KEY`.
- `docs/runbooks/nuget-trusted-publishing.md` -- record the exact NuGet.org/GitHub setup, activation check, failure recovery, and post-success retirement of the old key.

## Tasks & Acceptance

**Execution:**
- [ ] `.github/workflows/release.yml` -- inline the Projects release job from the approved shared contract, remove the reusable-workflow secret boundary, and wire the official login output only to Semantic Release.
- [ ] `tests/tools/run-ci-workflow-gates.ps1` -- replace stale reusable-callee assertions with fail-closed Trusted Publishing and ordering invariants covering every matrix row.
- [ ] `docs/runbooks/nuget-trusted-publishing.md` -- document human-owned policy/username setup and require a successful frozen-to-enabled trusted release before deleting the legacy secret.

**Acceptance Criteria:**
- Given a frozen or stale release, when the workflow runs, then no OIDC exchange or publication step executes.
- Given an approved exact-source release with matching trusted policy, when publication runs, then the only NuGet credential is the masked `NuGet/login` output and all five manifest packages retain existing validation.
- Given workflow policy validation, when any long-lived NuGet secret reference, mutable action reference, reusable publishing call, or login-after-publication ordering is introduced, then the gate fails.
- Given the migration runbook, when an operator configures NuGet.org, then every repository, workflow, environment, username, scope, activation, rollback, and secret-retirement value is explicit.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

NuGet's official setup supports caller-owned GitHub Actions, but `NuGet/login` issues [#6](https://github.com/NuGet/login/issues/6) and [#9](https://github.com/NuGet/login/issues/9) document current reusable-workflow claim mismatches. A caller-owned job is therefore a correctness boundary, not merely a preference. The official action is pinned to the reviewed `v1.2.0` commit rather than its mutable major tag.

## Verification

**Commands:**
- `pwsh -NoProfile -File ./tests/tools/run-ci-workflow-gates.ps1` -- expected: all release topology, credential, ordering, package-count, and immutable-reference invariants pass.
- `npm ci --ignore-scripts && npm audit signatures` -- expected: the exact Semantic Release dependency graph installs and its registry signatures verify.
- `git diff --check` -- expected: no whitespace errors.

**Manual checks:**
- Inspect the workflow to confirm the `production` environment owns the caller job and `NuGet/login` is skipped unless publication is enabled.
- Do not exercise the irreversible publish path in implementation; the first live verification follows the runbook after the NuGet.org policy and `NUGET_USER` are configured.
