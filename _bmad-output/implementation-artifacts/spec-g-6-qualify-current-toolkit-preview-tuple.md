---
title: 'G-6 Qualify the Current Toolkit Preview Tuple'
type: 'chore'
created: '2026-10-03'
status: 'in-progress'
approved_at_utc: '2026-10-03T06:51:47Z'
approval_decision: 'Approve and stop'
route: 'dispatch'
baseline_commit: '53c6f29a9b0aaf858f11ecd919b32e676b6a41f1'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-10-02.md'
  - '{project-root}/docs/runbooks/projects-topology.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Current G-6 is `not verified`. The Builds catalog pins Toolkit `13.6.0-preview.1.261001-0243`, but Platform `apphost.cs` still pins `13.6.0-beta.910`, restored consumer graphs are stale, and the tuple/prerelease exception has neither v3 live proof nor a named owner disposition.

**Approach:** Align the Platform pin on committed source, run the v3 isolated PostgreSQL two-sidecar proof on a clean committed checkout, record Jérôme Piquot's named Builds/Platform/FrontComposer-Web disposition in the Builds policy, and obtain a `qualified` result that `g6_current.py validate` accepts; then update current-checkout tracking only.

**Decisions (2026-10-03, user: "do recommended"):**
- *Status authority:* commit locally, fast-forward and push Platform, then push Builds and root to `origin/main`. The CI `g6-current` run on the pushed root SHA is the authority for `current_checkout_status`; tracking cites its run and artifact. Local runs are pre-checks. Push all repositories only after the approval commit, so CI evaluates the approved tuple.
- *Disposition order:* proof first. Run a pending candidate, HALT for the named decision on its result hash, counts and limitations, record it, then run a second time to qualify. Spec approval is not the tuple disposition.
- *Scope:* keep the full spec (~1,850 tokens) as one goal.

## Boundaries & Constraints

**Always:** Follow the approved 2026-10-02 policy and runbook. Run live proof only on a clean committed checkout whose submodule HEADs equal root gitlinks, after restoring the 11 policy consumers as CI does. Validate a result before any further commit. Retain every attempt, including failed or `not verified` ones. Keep the running shared Aspire/Dapr/Redis containers untouched; a changed shared snapshot is a retained failed attempt. Validate each commit message with the owning repository's pinned commitlint. State that checkout-source proof does not qualify published EventStore archives.

**Never:** Infer or pre-fill the named disposition; change any tuple version other than the Platform Toolkit pin; enable reuse; weaken validator, runner, or workflow semantics to obtain a pass; rewrite attempt-16 or other historical records; change EventStore.Aspire pins, G-4/G-5, P1R, Story 6.1/8.11, or downstream statuses; activate Dapr.Workflow; initialize nested submodules; publish packages, deploy, or release.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Pending candidate | Aligned committed source, approval `pending` | All commands pass; status `not verified`; sole issue is pending approval | Present result hash/counts for decision |
| Critical failure | Failed/skipped test, build error, cleanup or shared-snapshot change | Status `failed`; result retained | Fix root cause within boundaries or stop and report |
| Approved run | Approved policy committed, clean checkout | Status `qualified`; `validate` exits 0 | Any issue keeps `not verified`/`failed` |
| Declined | Owner rejects tuple | Policy stays `pending`; tracking stays `not-verified` | Record the decline only |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Platform/apphost.cs:8` -- only Toolkit pin. Local main is 12 commits behind `origin/main`; those commits leave `apphost.cs`, `DaprSelfHostedMtls.cs`, and runtime config unchanged, so fast-forward before committing (decision: keeps Platform pushable). No CI; commitlint config-conventional.
- `references/Hexalith.Builds/Tools/g6-current-policy.json` -- set `approval` (`decision: approved`, `approvedBy: "Jérôme Piquot"`, UTC `approvedAtUtc`, `reference` to this spec's decision record) and `exceptions.communityToolkitAspireDapr: approved-prerelease-exception`. `tupleSha256` stays `52c8d36a…`. Enforced at `g6_current.py:103-135`.
- `references/Hexalith.Builds/Tools/README.md:466-470` -- replace pending/beta.910 wording once approved. Controls: `Tools/test_g6_current.py`.
- `tools/qualification/run_g6_current.py` -- runner. Output goes to gitignored `.g6-current-evidence/<run>`. It needs Docker, network (Dapr CLI, Aspire CLI tool, images) and system `dotnet` 10.0.401, and uses an isolated `NUGET_PACKAGES`. It emits `qualified` only with an approved tuple and zero audit issues.
- `references/Hexalith.Builds/Tools/g6_current.py` -- `audit`/`validate`; validate requires clean root and submodules and exact source equality (`:482-487`).
- `.github/workflows/ci.yml:165-219` -- `g6-current`: restore each `resolvedProjects` entry with `-p:UseHexalithProjectReferences=true`, then `tests/tools/run_g6_ci_gate.py`. Release reruns proof unconditionally.
- `tools/qualification/run_g6_qualification.py:58-71` -- shared snapshot covers every host container.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- `qualification_gates.G-6.current_checkout_*` and `p1r_current_revalidation.g6_failures`; `epic-6-context.md:36`. Update only current-checkout fields.
- Do not change: attempt-16 packets/acceptance, v1/v2 validators and baselines, CI/release logic, EventStore pins.

## Tasks & Acceptance

**Execution:**
- [ ] `references/Hexalith.Platform/apphost.cs` -- fast-forward Platform main to `origin/main`, set Toolkit to `13.6.0-preview.1.261001-0243`, commit; commit the root Platform gitlink -- removes the only direct pin drift.
- [ ] Restore the 11 policy consumers with isolated NuGet cache, then audit -- the only remaining issue must be pending approval.
- [ ] `.g6-current-evidence/<candidate-run>/` -- run the pending candidate; HALT with result hash, counts and limitations for the named decision.
- [ ] `references/Hexalith.Builds/Tools/{g6-current-policy.json,README.md}` -- record the decision verbatim, pass Builds controls, commit; commit the root Builds gitlink.
- [ ] `.g6-current-evidence/<final-run>/` -- run on clean committed source; `validate` must exit 0 before any later commit.
- [ ] `_bmad-output/implementation-artifacts/qualification-evidence/g-6-current-20261003/` -- copy validated result, cleanup, capture and logs with SHA-256 index.
- [ ] Push Platform, Builds, then root; observe CI `g6-current` on the pushed root SHA -- status `qualified` and artifact uploaded.
- [ ] `sprint-status.yaml`, `epic-6-context.md` -- set current-checkout status from the CI result, citing run URL, artifact name and the local evidence index; commit and push (non-material, so CI reports `not required by this change`).

**Acceptance Criteria:**
- Given aligned committed source and an approved policy, when the runner completes, then qualifier 1/1 and support 33/33 pass with zero skips, 10 AppHosts plus McpCli compile, all cleanup flags are true, and `validate` exits 0.
- Given the pushed approved root SHA, when CI `g6-current` runs, then it prints `G6-CURRENT-QUALIFIED` and uploads its result artifact; only then does `current_checkout_status` become `qualified`.
- Given tracking updates, when compared with `HEAD` before work, then attempt-16 files and all downstream statuses are byte-identical.

## Verification

**Commands:**
- `python3 references/Hexalith.Builds/Tools/test_g6_current.py` -- all controls pass.
- `python3 -m unittest tests/tools/test_run_g6_current.py tests/tools/test_run_g6_ci_gate.py -v` -- pass.
- `python3 references/Hexalith.Builds/Tools/g6_current.py validate --workspace . --policy references/Hexalith.Builds/Tools/g6-current-policy.json --evidence .g6-current-evidence/<final-run>/result.json` -- exit 0, `G6-CURRENT-QUALIFIED`.
- `python3 tools/planning/validate_production_authority.py --validate-index` and `git diff --check` -- pass.

## Implementation Notes

## Spec Change Log

## Review Triage Log
