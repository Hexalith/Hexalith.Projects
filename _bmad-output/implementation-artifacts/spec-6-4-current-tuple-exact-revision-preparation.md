---
title: 'Story 6.4 current tuple exact-revision preparation'
type: 'bugfix'
created: '2026-10-30'
status: 'done'
route: 'dispatch'
baseline_commit: '5724c2400c102c466247890c05bbbdabb898e7a4'
story_key: '6-4-resolve-projects-with-transient-current-explanations'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/evidence/6-4-advisory-resolution-20261021/current-tuple-preparation-20261030/README.md'
---

<frozen-after-approval reason="User explicitly authorized current tuple preparation, dirty-work preservation, read-only official verification and targeted nonrecursive fetching; ask only for unresolved decisions">

## Intent

Prepare Story 6.4's current Platform/Builds/tool tuple for exact-revision qualification while preserving the newer catalog. No compatible published tuple is currently verified: prepare the concrete official Builds producer candidate and minimal Platform configuration/CI corrections, then verify actual restored dependencies and relevant owner-host behavior. Deliver exact reviewable changes, honest focused results/blockers and a precise recording/publication sequence. Main Story 6.4 stays in progress until its independent acceptance gates pass.

## Boundaries & Constraints

**Always:** Preserve existing dirty work, historical evidence and original story prefix/frontmatter/frozen block. Reconcile historical findings to current full revision identifiers. Keep source diagnostics, local Debug checks, hosted preflight, runtime/policy and publication/adoption acceptance distinct. Read baseline and owner guidance/configuration before edits; never load skills in references. Read-only GitHub/NuGet verification and targeted nonrecursive official-object/tag fetching override workflow no-remote restrictions for this task. Execute sequentially. Adapt within this scope if source details differ and document why.

**Never:** Stage, commit (including fixture commits), push, branch, create tags, publish, deploy, dispatch CI, initialize nested submodules, suppress guards or invent publication versions/hashes. Do not downgrade catalogs, substitute current source beneath 4.38.3, manufacture a clean recorded owner or accept old runtime evidence for new inputs. Do not repeat unrelated full source suites.

</frozen-after-approval>

## Code Map

- Canonical `spec-6-4-resolve-projects-with-transient-current-explanations.md`, its last checkpoint and `evidence/6-4-advisory-resolution-20261021/platform-tuple-owner-ci-20261030/qualification-next-actions.md` retain historical truth. Append only new facts; story/sprint remain in progress.
- Evidence `current-tuple-preparation-20261030/` contains initial thirteen-repository states, dirty diffs, original story/tracking bytes, official observations and exact catalogs. Add implementation/validation receipts without replacing original results.
- Local Platform `a185bfaa5541f8de675fb82e4f553e1e49931201` pins unavailable official Builds object `cc0db51b800be1d0d5badc3780395cfb5953d0f5` (fetch128/API422). Local Builds `1160a3a7ddc0f4c6927456b5735a2963261016a0` catalog equals official fetched producer candidate `6cc96b4ff23c40c77651e1e4f85ed3170c25c7a4`, including newer security/provenance fixes. Source catalog SHA256 `abe630443a9f095a1bce171c4b10314a714a8bba9f3bde8980a5c40ef46ef172`; checkout CRLF hash differs. Remote Platform `a57383fd34faec3a6ff891b5c63f0a9f535ed699` is a separate observed identity. Detect independent advancement without overwriting it.
- Builds `.github/workflows/build-release.yml`, package.json, `Tools/verify-g4-release-source.sh`, package/release contract scripts own qualification/version computation and protected publication. Existing candidate needs no speculative dependency changes. Latest observed exact-source CI41102168937 pending. Official latest release/feed is4.38.3/d1931b1e803144048d927fff16cb8119afde541e.
- Platform child `src/Hexalith.Platform.TenantRegistry.Host/Directory.Packages.props` independently pins Gateway3.128.4, omitting root transitive pinning. Import `$(MSBuildThisFileDirectory)../../Directory.Packages.props` to inherit the real selected catalog, preserving Gateway and bringing actual Commons closure to2.30.7. Verify evaluated versions and actual assets rather than assumptions.
- Platform `eng/prepare_builds_artifacts.py`, `eng/run_tuple_ci.py`, `.github/workflows/tuple-ci.yml` hardcode historical4.38.3 identities. Centralize exact candidate/publication bindings; absent official version/archive/tooling/catalog binding must fail closed. Keep signature, clean-recorded-source, pin/catalog, extracted-byte, foreign-project, Release/package and suite-count guards plus candidateAcceptance:false. Add focused meaningful negative controls without Git mutation.
- Platform `.config/dotnet-tools.json`, `hexalith-platform.json`, README and Builds gitlink must eventually align with the verified producer release. Until its real version is established, retain existing bootstrap tool/range explicitly unqualified, prepare the exact gitlink correction as an UNAPPLIED patch, and document remaining publication-binding edits. apphost.cs SDK13.6.4 and helper13.6.2-beta.959 already match; solution fixture/worker fixes already landed.

## Tasks & Acceptance

- [x] Prepare producer source/catalog/release-input manifest and required qualification sequence; refresh only useful official source CI observations. Given no compatible release, when prepared, then exact official source is concrete and publication fields remain honestly unavailable.
- [x] Apply minimal Platform child CPM and CI/binding/documentation corrections; retain exact diffs and proposed gitlink patch. Given absent/mismatched publication identity, when preflight runs, then it refuses without weaker guards or source fallback.
- [x] Evaluate the selected catalog through MSBuild, restore/build actual TenantRegistry Debug, retain actual assets and complete Hexalith expected/actual comparison. Given current restored closure, when compared, then every mismatch is resolved within catalog alignment or explicitly blocked.
- [x] Run existing relevant host seed tests, CI binding controls, pin checks and actual command-owned local host HTTP controls where feasible. Reuse the earlier `tenant-registry-gateway-alignment-20261030/run-local-host.py` harness only with new output locations. Given unavailable services, when controls run, then precise readiness/authorization limits and cleanup remain explicit. Keep secrets/private runtime logs outside evidence.
- [x] Audit original byte/Git preservation, focused results, and actual package-preparation guards where feasible; append current story checkpoint and update only obsolete sprint comment. Given review, when handed off, then exact recording/publication/adoption order and all unmet gates are clear, main story remains in progress, and no unauthorized Git mutation occurred.

## Implementation Notes

No unresolved user decision blocks reversible preparation. User's explicit scope removes redundant intermediate approval gates. Repository-specific validation may require focused fallbacks; no fixture operation may create commits. Baseline recorded by implementation before changes; existing canonical baselines remain immutable.

Implementation prepared and parent-audited: [receipt](evidence/6-4-advisory-resolution-20261021/current-tuple-preparation-20261030/implementation/README.md). Ten binding tests, fifteen MSBuild controls and ten actual host HTTP controls pass; actual TenantRegistry package graph matches the selected catalog. Five existing Custody errors block the full and narrow seed-test builds; no seed cases are claimed executed. Producer CI is cancelled, publication bindings unavailable, package guards remain closed. Main story stays in progress. Parent inspected complete owner/root patches and raw passing/failing results; historical evidence and all thirteen HEAD/index/ref identities are preserved.


## Review Triage Log

All three independent review layers reported before triage. Every finding is listed before grouping.

| Finding | Verdict / route | Evidence and disposition |
| --- | --- | --- |
| BH1: SemVer boundary handling | medium / patch | Current regex accepts numeric prerelease 01 and rejects permitted repeated hyphens. Correct identifier validation and cover both boundaries. |
| BH2: publication can exceed fixed range ceiling | medium / patch | A completed binding can accept major 5 with a range excluding itself. Require the supported 4.x publication family and test refusal. |
| BH3: Gateway override test is nondiscriminating | medium / patch | Parent fixture already supplies 3.128.4. Give it a different version to prove the retained owner Update wins exactly once. |
| BH4: valid/dirty catalog checkout coverage absent | medium / patch | Existing catalog tests all stop before Git diff. Exercise valid catalog/path, CRLF-normalized checkout and modified-checkout rejection without fixture commits. |
| BH5: catalog mismatch diagnostics discarded | low / patch | CalledProcessError hides captured Git diff output from terminal receipt. Preserve the public catalog command/exit/output in the raised diagnostic. |
| BH6: qualification inventory identity absent from binding | medium / patch | Required manual qualification has no mandatory inventory locator/digest in the owner binding. Add unavailable required fields and document the actual producer proof to bind; do not duplicate producer qualification logic. |
| BH7: no embedded-versus-source catalog proof | false | Mandatory producer package controls already generate expected catalog and compare installed embedded resource byte-for-byte (Tools/test-g4-tool-package-contracts.ps1:983 and :1051). Publication remains unavailable until that actual qualification is verified; BH6 will bind its inventory evidence. The runtime resource is not the complete NuGet package catalog, so direct full-catalog equality would be incorrect. |
| BH8: recorded artifact orchestration control absent | medium / patch | Archive success mocks owner pins and omits require-recorded. Add controls showing recorded pin/catalog refusal occurs before signature/executable invocation or output creation. |
| BH9: subscription only checks status/count | medium / patch | Fresh probe records count but does not assert subscription payload. Add expected pubsub/topic/route assertions in a copied follow-up harness and retain a new result. Original execution evidence stays immutable. |
| BH10: health503 reason not asserted | medium / patch | Existing probe cannot distinguish expected placement/writer unavailability from another failure. Retain sanitized names/statuses and require only expected failures in the copied follow-up harness. |
| EC1: major5 accepted despite ceiling | medium / patch | Same demonstrated invalid range as BH2; retain individual verdict and apply one correction. |
| VG1: success/CRLF/dirty catalog coverage | medium / patch | Preverified mutation demonstration leaves current tests passing after valid catalogs are unconditionally rejected. Same missing coverage as BH4. |
| VG2: retained Gateway override coverage | medium / patch | Preverified fixture mutation removes Update without failure. Same missing coverage as BH3. |

Surviving groups: version validation, discriminating catalog/orchestration and Gateway controls, retained catalog diagnostics, qualification-inventory binding, and stronger fresh HTTP content/health evidence. Existing historical receipts and the original source/runtime blockers remain unchanged.


## Review Resolution

All surviving findings are corrected. Final targeted controls pass fifteen tuple-binding cases and fifteen MSBuild cases. The copied fresh HTTP probe passes ten controls with exact subscription and named health assertions, retains expected placement/writer unavailability, and stops all owned processes successfully. [Follow-up evidence](evidence/6-4-advisory-resolution-20261021/current-tuple-preparation-20261030/implementation/review-follow-up-20261030/README.md) preserves all 118 original implementation evidence files and their earlier inputs/results. Parent inspected the fixes, including Builds’ tracked CRLF catalog attribute, so the real-checkout test is portable to the initialized CI dependency. No finding was deferred and no publication/runtime/seed acceptance is inferred. This scoped preparation is done; canonical Story 6.4 and sprint status remain in-progress under the user's explicit instruction. No commit or sprint promotion is performed; that instruction overrides the generic presentation step.

Parent final audit: planning guard37, both authority checks and whitespace pass; final preflight still rejects unavailable publication. The recording sequence explicitly defers the umbrella pointer until separately approved consumer adoption after Platform publication. Exact final patches, commands, integrity checks and approval sequence are in [the final handoff](evidence/6-4-advisory-resolution-20261021/current-tuple-preparation-20261030/parent-final/README.md).
