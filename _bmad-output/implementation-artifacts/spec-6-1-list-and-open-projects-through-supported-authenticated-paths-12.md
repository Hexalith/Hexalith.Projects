---
title: 'Story 6.1: Finish supported-read verification with explicit bootstrap authority'
type: 'bugfix'
created: '2026-11-05'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
story_key: '6-1-list-and-open-projects-through-supported-authenticated-paths'
baseline_commit: 'a6c415f38053cbdb257b12d196be0bfa70aadc2d'
context:
  - 'AGENTS.md'
  - 'references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '_bmad-output/implementation-artifacts/evidence/story-6-1-bootstrap-authority-20261105/README.md'
  - '_bmad-output/implementation-artifacts/evidence/story-6-1-bootstrap-authority-20261105/review-triage.md'
  - '_bmad-output/implementation-artifacts/epic-6-context.md'
  - '_bmad-output/implementation-artifacts/6-1-list-and-open-projects-through-supported-authenticated-paths.md'
  - '_bmad-output/implementation-artifacts/evidence/story-6-1-review-patches-20261105/README.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Finish Story 6.1's supported authenticated Project list/open work, preserving the existing uncommitted review fixes authorized by the user. Restore the isolated Development fixture's bootstrap against the current Tenants authority contract, complete relevant verification and review, and record the actual acceptance result against the story's seven criteria.

Give only the fixture's existing exact `actor-1` / `supported-read-bootstrap` password flow the explicit `global-administrators:bootstrap` permission. Preserve credential rejection, real JWT/workload/owner authorization, privacy policy, persistence assertions, cleanup and legacy routing. Do not alter dependency pins, sibling sources, production enrollment or other stories. Keep Story 6.1 in progress if current required acceptance remains unproved; historical receipts cannot establish fresh acceptance.

**Approved scope extension — 2026-11-05:** The user approved continuing with fixture public-scope/credential alignment and the confirmed denial-observer race repair. Remove reserved storage grants from public credentials; use the current owner-declared platform scopes, exact finite permissions and explicit status headers. Use the existing bound-workload SDK for global-administrator source proof; do not bypass owner authorization or alter persisted `system` history. Repair completion ordering and test blocked export and suppressed-root completion. Existing prohibitions on sibling edits, dependency changes and Git mutations remain.

</frozen-after-approval>

## Implementation Notes

- Investigation: all fourteen G1 review patches are already implemented. Retained local evidence reports Integration 196/196 and Server 1,328/1,328 with live exclusions; projection/read/catalog controls 51/7/12, evidence controls 83 and launcher controls 119. Preserve these source-bound receipts and original failed attempts.
- Root cause: `tools/qualification/run_supported_project_reads.py`, `issuer_server`, grants only `command:submit` and `replay:read`. The current ordinary owner source `references/Hexalith.Tenants/src/Hexalith.Tenants/Bootstrap/TenantBootstrapBoundCommand.cs`, `CreateAsync`, requires `global-administrators:bootstrap` before request/receipt creation. Missing permission returns null, matching the distributed failure before Project reads. The retained catalog exception alone does not establish another bootstrap cause.
- Small implementation footprint: add that permission in the issuer and assert the exact permission inventory in `tests/tools/test_supported_project_reads_launcher.py`, `SupportedReadDelegatedIssuerTests.test_https_password_endpoint_issues_only_exact_bootstrap_human_and_rs256_signature`. No public interface changes, intent gaps or irreversible operations are required. This meets the oneshot routing criteria.
- Verify the focused issuer test and the full launcher controls, then run `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 DOTNET_PROCESSOR_COUNT=2 python3 tools/qualification/run_supported_project_reads.py --regressions --timeout 900` with a fresh evidence directory. Preserve native XML, commands, tested full commits, manifest, scenario matrix and owned cleanup. Inspect runtime before any code changes as required by the baseline; use the existing isolated fixture lifecycle.
- Independent review must cover current patches and the outstanding G2 Server/Contracts test, G3 Integration test and G4 qualification-launcher changes against historical baseline `47a1ffd02cb3e1cde80bd7697ac1b46fdefda8cc`. Retain the story's approved decisions and triage concrete findings within scope.
- Separate known blocker: package-mode Server.Tests Release restore reports NU1109 because Tenants.Client 7.0.0 requires EventStore Client/Contracts >=4.0.4 while Projects selects 3.133.0. Record its exact command/output; no version overrides or weakened gate. Do not label that package build passing.
- Record this run in a new evidence packet and append factual results to the story. Synchronize sprint tracking only with actual story acceptance. No Git staging, commits, pushes, branch changes or submodule updates are authorized.

- Replan trigger: the issuer repair passes 119 controls, but fresh acceptance reaches a rejected bootstrap receipt. Current public fixture credentials carry reserved `system` grants, seed requests use storage scope, and status requests lack explicit scope headers. G2 review also confirms an export-completion race. These changes exceed the planned issuer/control edit. Full results and ten finding dispositions are in `evidence/story-6-1-bootstrap-authority-20261105/README.md` and `review-triage.md`. Package restore remains NU1109; no further coding occurred after identifying the expanded scope.

## Code Map

- `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadDistributedFixture.cs`: align HumanToken/SeedAsync/CommandAsync/StreamAsync/AwaitHostedBootstrap public credentials, exact platform permissions and explicit status scopes with owner contracts; retain historical storage `system`. Use the existing bound-workload SDK for global-administrator source proof; stop for missing owner capability.
- `tests/Hexalith.Projects.Server.Tests/Queries/ProjectReadDenialObserver.cs` and `SupportedProjectGatewayHttpTests*.cs`: enroll completion after the actual tracer provider; prove a blocked exporter cannot produce an early empty snapshot and a suppressed root still completes empty. ForceFlush alone is insufficient.
- Existing issuer/control paths above: remove reserved storage grants from the platform-only bootstrap JWT; assert exact finite permissions and absent reserved grants. Run focused controls before fresh AC7, retaining strict receipt and no-write checks.

## Tasks & Acceptance

- [x] `tools/qualification/run_supported_project_reads.py` and `tests/tools/test_supported_project_reads_launcher.py`: align the finite platform bootstrap credential and prove signed exact permissions, absent reserved grants and unchanged credential rejection.
- [ ] `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadDistributedFixture.cs` and `SupportedProjectReadEvidenceTests.cs`: align public commands/status/source reads with actual owner contracts; cover invalid/mixed scopes and preserve receipt, causation, persisted source and no-write checks.
- [x] `tests/Hexalith.Projects.Server.Tests/Queries/ProjectReadDenialObserver.cs` and `SupportedProjectGatewayHttpTests*.cs`: install the completion gate after real provider initialization; run bounded blocked-export and suppressed-root controls, retaining existing observer controls.
- [x] Retain fresh focused and distributed evidence under a new destination in `_bmad-output/implementation-artifacts/evidence/`; report exact commands, source commits/manifest, XML, totals and cleanup. Update the story factually without claiming acceptance if a required gate remains blocked.

Given platform provisioning, when the real owner receives a public request, then the explicit scope and exactly required human permission agree and no public grant uses reserved storage `system`; persisted proof retains historical identities.

Given a blocked exporter, when `ActivityAsync` awaits completion, then it remains pending until export finishes and subsequently contains that span; given a suppressed root, then completion returns an empty export set without hanging.

Given a fresh supported-read qualification, when all required lanes finish, then actual results establish acceptance or the exact missing owner capability remains a documented blocker; no assertion, privacy policy or build gate is weakened.

### Approved implementation continuation

This is an active bmad-build dispatch: implement from this spec without re-entering the Build workflow. Preserve existing changes and original failed packets. The previous pre-edit baseline was already inspected through the owned launcher with healthy application resources; both startup and acceptance cleanup are recorded. Use current local Debug/source mappings, individual test assemblies and the existing isolated launcher; no concurrent qualification or Release/dependency workaround. Add focused tests before a fresh runtime attempt. If another owner capability is missing, finish the independent authorized repairs and record a concrete owner handoff instead of editing siblings. Append implementation notes and leave story acceptance honest.


## Continuation outcome — 2026-11-05

Public fixture credential/command/status alignment and the observer completion repair are implemented. The fixture/source task remains unchecked because the current owner SDK explicitly does not support the required `platform:global-administrators` workload stream read. No custom transport, response remapping, reserved public `system` read, sibling edit or dependency change was used. The fixture stops explicitly at that missing capability; original receipt, source-event, causation/correlation, persisted-state and no-write assertions remain intact.

The [new evidence packet](evidence/story-6-1-public-scope-observer-20261105/README.md) retains exact commands, source commits, manifests, XML, pass/fail totals and cleanup. Signed issuer passes 1/1 and launcher controls 119/119; mapped Debug builds have zero warnings/errors. Local Integration passes 208/208 (including 95 evidence controls and 12 new scope/grant cases); final focused gateway controls pass 8/8 and final local Server passes 1,330/1,330, without failures/errors/skips/not-run. Existing live selectors are excluded from the local totals.

One fresh native duplicate scenario verified the original hosted receipt and a real `Completed` status through HTTP 200 with explicit `platform:global-administrators`, then failed at the exact owner-capability guard: 0 passed, 1 failed, 0 errors/skipped/not-run. Persisted bootstrap source proof is explicitly false and no Project reads, later providers or full runtime regressions ran. The requested full coordinator was not started after confirming this owner capability gap. The concrete [owner handoff](evidence/story-6-1-public-scope-observer-20261105/owner-handoff.md) covers the shared SDK repair.

Native source manifest `41df764d7b1a222455c81f74258f5b819021a87604fd0fdb5928f8aa30c723e0` matched current source at cleanup. All four owned containers, six sidecars, AppHost and private material were removed; the five preexisting containers were preserved. After cleanup, one Server-test-only control was strengthened to install the actual provider/completion gate. Its final local source manifest is `a56fe2d12ab5c1077217ebb02e1f67a9c879e3e73fce6e06098dd622b609099b`; the exact sole-path difference and passing final Server assembly are retained. No native/final source equality or current AC7 acceptance is claimed.

The known package-mode NU1109 blocker remains recorded at its exact prior command/output, without a version override or a passing package-build claim. Story and sprint remain **in-progress**. Parent workflow review follows this implementation handoff; owner source proof and complete fresh acceptance remain incomplete.


## Review Triage Log

2026-11-05 dispatch review: Blind Hunter, Edge Case Hunter and Verification Gap ran; none was skipped. Edge and Verification Gap returned no findings. Edge used the complete embedded deletion-check instructions where a standalone reference was absent. Full unified snapshot includes untracked artifacts; no Git staging occurred.

| Finding | Verdict and evidence | Route |
| --- | --- | --- |
| Ignored command-output logs are missing from the reviewable diff. | **medium.** `.gitignore:92` excludes `*.log`; `git check-ignore` confirms current issuer/launcher/build outputs and retained package failure are absent from the snapshot even though receipts reference them. Preserve byte-identical checkout-visible copies plus a mapping, without staging or changing ignore policy. | patch |
| Progress receipt attributes the SDK blocker before projection readiness. | **low.** The progress JSON sets `sourceProofBlockedByOwnerSdk=true` before `WaitForStateAsync`; a timeout/cancellation there would give premature execution attribution. Remove that early causal flag; keep unverified source proof explicit and the actual SDK failure in native results. | patch |
| Completion would race under a batch processor. | **false.** Every current caller constructs `SimpleActivityExportProcessor`, including the actual gateway and controls; no batch-backed observer configuration is reachable. The explicit synchronous composition and blocked-export control are the contract. A hypothetical future replacement is not a present defect. | reject |
| Actual HTTP gateway exporter wiring lacks a positive export control. | **low.** Synthetic-provider controls and actual HTTP denial-empty assertions do not prove that the actual gateway's exporter/source registration is live. A normal HTTP observation on that same gateway can provide the missing positive control with existing helpers. | patch |
| SendHumanAsync header branches lack captured-request coverage. | **low.** True; later provisioning is explicitly unverified because the owner source-read capability blocks the native run. The typed branches are visible, callers use exact owner constants, and status/grant controls pass. No wrong request or premature acceptance was demonstrated. Additional fixture transport scaffolding exceeds a direct correction. | reject |
| Local scope controls do not prove gateway negative authorization. | **low.** True; those tests claim local helper validation, not endpoint refusal. Native positive platform status is proved, and production status authorization remains owner SDK behavior. No authentication rule changed or passing endpoint-negative claim was made. Additional owner HTTP matrices exceed the bounded correction. | reject |
| Folder replacement test does not promote its rebuild plan. | **low.** Unchanged from the earlier reviewed gap: it tests shared fold readiness; separate SDK/replay lanes own promotion. No promotion implementation changes here. Repeating promotion for one data shape adds coverage without a demonstrated defect. | reject |
| Folder replacement regression uses one projection delivery. | **low.** True; production receives later replacement against persisted linked detail. Split the existing regression into persisted prefix and replacement deliveries while retaining full-history rebuild readiness. This is a direct correction using the same assertions. | patch |
| No clock-skew permutation in the replacement regression. | **low.** Unchanged from prior triage. The approved null-only fold and required replacement regression are present; no timestamp policy changes or distinct new failure was demonstrated. More data permutations exceed a direct correction. | reject |
| No package-only worker-security selector test. | **low.** The removed test is intentionally source-only; the package branch explicitly refuses selection. No package private lane is supported, and package restore is already blocked with no passing claim. A second conditional control duplicates the existing refusal rather than repairing a demonstrated defect. | reject |

No intent-gap, bad-spec or deferred entry survived this review. The documented EventStore capability and NU1109 remain existing blockers, not waived acceptance gates.


### Review correction closure and parent verification — 2026-11-05

Review rows 1, 2, 4 and 8 are resolved: 17 byte-identical checkout-visible outputs and a mapping replace ignored-only evidence references; the early progress receipt omits causal attribution before the SDK boundary; the actual HTTP gateway provider has a passing positive export control; and Folder replacement runs after a separately persisted linked prefix. The exact retained-output directory alone has a raw-output attribute exception. Original native packets remain untouched. The other six findings retain their recorded rejected verdicts; none was deferred.

The parent's full relevant local checks pass 208/208 Integration and 1,331/1,331 Server, with no failures/errors/skips/not-run. Live selectors remain explicitly excluded. [Commands, native XML, hashes and exact source binding](evidence/story-6-1-public-scope-observer-20261105/parent-verification/results.json) show unchanged manifest `0454152151df5bf58fa74343bc59a0d660bb4c4df56dc96cdc25ddf3ad82943d` during both runs. All retained-output digests and checkout visibility validate, and the owned mapping workspace was removed after testing. The tracked story's existing LF convention is preserved.

Workflow review of the implemented repairs is complete. The unchecked source-proof task cannot be completed without the documented EventStore capability, which the approved intent forbids replacing or repairing here. Therefore this run stops before feature completion: spec/story/sprint remain in-progress, no commit/review/done transition or Git mutation is performed, and the earlier native failure/package blocker are not waived. The [owner handoff](evidence/story-6-1-public-scope-observer-20261105/owner-handoff.md) is the concrete next dependency repair.
