---
title: 'Migrate Projects releases to NuGet Trusted Publishing'
type: 'chore'
created: '2026-09-20'
status: 'done'
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
- [x] `.github/workflows/release.yml` -- inline the Projects release job from the approved shared contract, remove the reusable-workflow secret boundary, and wire the official login output only to Semantic Release.
- [x] `tests/tools/run-ci-workflow-gates.ps1` -- replace stale reusable-callee assertions with fail-closed Trusted Publishing and ordering invariants covering every matrix row.
- [x] `docs/runbooks/nuget-trusted-publishing.md` -- document human-owned policy/username setup and require a successful frozen-to-enabled trusted release before deleting the legacy secret.

**Acceptance Criteria:**
- Given a frozen or stale release, when the workflow runs, then no OIDC exchange or publication step executes.
- Given an approved exact-source release with matching trusted policy, when publication runs, then the only NuGet credential is the masked `NuGet/login` output and all five manifest packages retain existing validation.
- Given workflow policy validation, when any long-lived NuGet secret reference, mutable action reference, reusable publishing call, or login-after-publication ordering is introduced, then the gate fails.
- Given the migration runbook, when an operator configures NuGet.org, then every repository, workflow, environment, username, scope, activation, rollback, and secret-retirement value is explicit.

## Implementation Notes

- Replaced the reusable `domain-release.yml` call with a caller-owned `production` job that preserves exact-source admission, package-only restore/build, the exact freeze gate, the five-package manifest/count gate, npm signature verification, and Semantic Release.
- Pinned `NuGet/login` v1.2.0 to commit `8d196754b4036150537f80ac539e15c2f1028841`; its short-lived output is referenced only by the Semantic Release step's process-local `NUGET_API_KEY` environment variable.
- Added fail-closed topology, immutable-reference, step-order, credential-source, manifest-inventory, and runbook-policy assertions. No release dispatch, publication enablement, policy/secret change, or package publication was performed.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| BH-1 | medium | patch | `vars.HEXALITH_RELEASE_PUBLISH_ENABLED` is an effective configuration value, so an organization-level `true` is visible when the repository value is absent; the new runbook incorrectly presents repository-variable absence as sufficient to freeze. Require an explicit repository-scoped verdict. |
| BH-2 | false | reject | Job-level `id-token: write` makes token requests possible, but every current pre-login action/script was traced and none requests an OIDC token or calls the NuGet exchange endpoint. Capability alone does not make the frozen/stale exchange outcome occur. |
| BH-3 | medium | patch | The gate counts two guard expressions anywhere in the job and does not prove they belong to the named NuGet login and Semantic Release steps, so moving the guards can leave either step unconditional while the gate passes. |
| BH-4 | medium | patch | The literal `secrets.NUGET_API_KEY` prohibition misses bracket-form access such as `secrets['NUGET_API_KEY']`; that exact bypass can restore the legacy credential while the current gate remains green. |
| BH-5 | medium | patch | Only the `release` job and Hexalith.Builds release paths are checked for job-level reuse; a second pinned external reusable publishing job is accepted by the current immutable-reference scan. |
| BH-6 | low | reject | No extra permission exists in the implementation, and the approved scope does not require an exact permission-map equality assertion. Adding a general permission parser for a hypothetical future edit is disproportionate here. |
| BH-7 | false | reject | The three `@main` calls are pre-existing non-release policy workflows, while the immutable Hexalith.Builds constraint in this story applies to the release execution path. The change narrows the existing exception and introduces no new mutable release reference. |
| BH-8 | medium | patch | Runbook validation uses global substrings, so required values can survive only in warnings/recovery prose after the actual policy table or scope list becomes unsafe. Validate the structured table rows and exact package bullets. |
| BH-9 | low | patch | The activation procedure asks for an “intended version” without a reproducible resolution command. This is a direct documentation omission and can be corrected without changing release behavior. |
| BH-10 | medium | patch | Workflow concurrency serializes runs but does not prevent an already queued dispatch from entering while the repository-global flag remains enabled after a prior run. The activation procedure needs an explicit active/queued-run check. |
| BH-11 | medium | patch | The runbook names neither the manifest validation command nor exact NuGet verification commands/evidence location, leaving post-publication proof non-reproducible despite the spec requiring explicit activation and retirement evidence. |
| BH-12 | medium | defer | The three gitlink changes are real in the baseline-to-tree diff, but commit `ffa922d0f7a278da3bb02ee09769564df02d5f01` already contained them when this implementation began. They are not caused by the Trusted Publishing implementation and must be audited as prior dependency work. |
| EC-1 | medium | patch | Same verified effective-variable scope defect as BH-1: repository absence does not override an organization-level `true`, while the runbook says absence is frozen. |
| EC-2 | false | reject | Same refutation as BH-2: no current pre-gate step requests or exchanges an OIDC token, and the reviewer demonstrated only job-wide capability, not the claimed frozen-run exchange. |
| EC-3 | false | reject | Source is revalidated immediately before login as specified. A later `main` advance does not make the just-completed boundary check false, and an additional non-atomic check cannot eliminate the proposed race. |
| EC-4 | medium | patch | Same verified bracket-form legacy-secret bypass as BH-4. |
| EC-5 | medium | patch | The immutable-reference scanner matches `uses:` but not valid YAML shorthand `- uses:`, allowing a mutable external action reference to evade validation. |
| EC-6 | medium | patch | Same verified second-job reusable-publishing bypass as BH-5. |
| EC-7 | medium | patch | Ordering is based on step-name strings while action/run assertions are global, so decoy named steps can satisfy ordering without associating NuGet/login and semantic-release with those blocks. |
| EC-8 | low | patch | The shared-workflow presence check counts raw text, so an exact reference retained only in a comment satisfies it. Requiring an actual `uses:` line is a direct correction. |
| EC-9 | medium | patch | The base package ID `Hexalith.Projects` is matched as a substring of repository prose and prefixed IDs, so removing its exact scope bullet does not fail the gate. |
| EC-10 | medium | patch | Same verified named-step guard association defect as BH-3. |
| VG-1 | medium | patch | Pre-verified demonstration replaced the false output with a comment plus `publish-enabled=true`; the complete gate still passed. An executable extracted-shell test is required for non-true and exact-true inputs. |
| VG-2 | medium | patch | Pre-verified demonstration changed the stale-source mismatch to `exit 0`; the complete gate still passed. An executable extracted-shell test must assert malformed and mismatched SHAs fail and exact/frozen cases succeed. |
| VG-O1 | medium | defer | The baseline Folders gitlink exposes a four-argument cancellation overload, while `FoldersProjectFolderDirectory` still calls it with five arguments; the source-reference Debug build fails with CS1501. This gitlink and failure pre-date the Trusted Publishing implementation. |
| PA-1 | medium | patch | Final audit found the proposed repository-variable REST lookup requires the separate `Variables: read` repository permission, which workflow `GITHUB_TOKEN` permissions cannot grant; every enabled release would remain frozen. Restored the effective `vars` binding and exact shell comparison while retaining the mandatory repository-level override in the runbook. |

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
