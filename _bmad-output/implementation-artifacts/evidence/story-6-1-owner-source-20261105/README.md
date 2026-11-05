# Story 6.1 owner-source repair — 2026-11-05

**Latest parent result — 2026-11-05:** The authorized repair and three-layer review are complete; Story 6.1 remains **in-progress**. At unchanged corrected manifest `970e0d526fd71d5ca3dfba99b53515e662f10c26f56a2c2b7709740d76de4cfc` (17,078 inputs), parent checks pass SDK 25/25, helper 32/32, selected Tenants 10/10, Integration 212/212, Server 1,331/1,331 and launcher 122/122. The separately recorded unselected Tenants build passes 9/9. All passing lanes have zero failures/errors/skips/not-run; Project local totals retain explicit live exclusions. Builds are clean. See [final verification](parent-final/final-verification.json), [exact parent commands](parent-final/results.json), [correction receipts](review-corrections/final-verification.json) and [review triage](../../spec-6-1-list-and-open-projects-through-supported-authenticated-paths-13.md).

The [fresh parent native run](qualification-2/duplicate/run.json) proves the repaired [complete persisted bootstrap binding](qualification-2/duplicate/bootstrap-delivery.json), then fails 0/1 at the same later maintenance reconciliation HTTP 403. It reaches no Project seed/read, later provider or full live regressions. Parent independently rechecks all four owned containers and eight original process identities absent; original cleanup/private removal/external preservation pass. The owned build mapping is removed. All 36 [checkout-visible output copies](retained-output/index.json) validate byte-for-byte. Original source/evidence remains under its actual earlier manifest; [source phases](review-corrections/evidence-phases.json) and normalized patches distinguish both sources.

The [next owner proposal](maintenance-owner-handoff.md) requires explicit approval to add narrow platform maintenance admission; the current intent prohibits new public endpoints. Package-mode NU1109 remains a separately recorded, unrerun obligation. Earlier sections below describe the original implementation run and its historical source.

The initial SDK/helper repair proves the real persisted bootstrap event at source `1849fc6b0f9ab7f644828e23fee2d893f28555eafb37c6e95e712643fb951f60`. That distributed lane then fails at a separate maintenance authorization boundary before Project seed/read. The review corrections below have focused verification only at their distinct source. Story and sprint remain **in-progress**.

## Initial implementation and source

EventStore Client sends one explicit scope header for the two exact `LegacyTenantStorageMap` platform/domain mappings and validates their retained storage Tenant plus exact Domain/AggregateId. Assertion and bound proof receive the original public request. Unsupported pairs, conflicting headers and wrong response identities refuse; ordinary Tenant response identity remains exact.

EventStore Testing supplies separate private owner-source IPC limited to `administrator-bootstrap` / `platform:global-administrators` / `global-administrators` / `global-administrators`. It uses the actual SDK and owner issuer/key inside Tenants. Wrong targets, expired/cancelled calls, incomplete histories, oversized results and SDK failures refuse. Tenants enrolls it only through an explicit Debug/source build and Development selection; production owner-source registrations remain unchanged.

Projects supplies separate exact private custody, an exact resource-bound assertion grant, and Tenants-only helper configuration. The fixture independently checks complete source identity and exactly one original event, preserving original receipt/correlation/causation/actor checks. Cleanup removes the helper custody with all private material.

Tested heads are Projects `fb6e1dfbe55fcade245da251bc5ff0d645a3456d`, EventStore `bbc165a5cd3a1f21822b4b53daa7fd895883f8a5`, and Tenants `8e20524574901d71ad5f4876804c64c3c6c87b88`, plus the complete [initial source changes with relative paths](source-changes/qualification-1-relative/index.json). Full dependency heads and 17,078 input hashes are in [focused/source-checkpoint.json](focused/source-checkpoint.json) and [focused/source-manifest.json](focused/source-manifest.json). Manifest `1849fc6b0f9ab7f644828e23fee2d893f28555eafb37c6e95e712643fb951f60` is unchanged across native and local verification. No pins, branches, checkouts, staging, commits or pushes changed.

## Initial verification

Exact commands, exit codes, mapping environment and original output are retained in `focused/*.command.json` / `focused/*.log`, indexed by [verification-command-index.json](verification-command-index.json). Native XML is the execution authority.

| Check | Passed / total | Result |
| --- | --- | --- |
| SDK workload/stream client | 25 / 25 | Pass |
| Shared owner-source helper | 19 / 19 | Pass |
| Tenants actual owner/catalog runtime | 10 / 10 | Pass |
| Stream controller/privacy | 131 / 131 | Pass |
| Projects evidence/topology | 127 / 127 | Pass |
| Launcher controls | 122 / 122 | Pass |
| Local Integration, live selectors excluded | 212 / 212 | Pass |
| Local Server, live selectors excluded | 1,331 / 1,331 | Pass |
| Fresh native duplicate scenario | 0 / 1 | Later maintenance HTTP 403 |

Passing totals have zero failures/errors/skips/not-run. Relevant Debug/source builds have zero warnings/errors. The first new helper build failed CA1062 for a missing constructor null check; corrected builds pass and initial failures remain retained. All three changed repositories pass `git diff --check`.

The [pre-edit baseline](baseline/run.json) establishes current topology state, the original unconditional SDK guard, native 0/1, exact cleanup and external preservation. Its [sanitized resource state](baseline/pre-edit-resource-state.json) shows the six native applications running before edits.

The fresh command was:

```text
MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 DOTNET_PROCESSOR_COUNT=2 python3 tools/qualification/run_supported_project_reads.py --regressions --timeout 900 --evidence _bmad-output/implementation-artifacts/evidence/story-6-1-owner-source-20261105/qualification-1
```

The [native bootstrap delivery evidence](qualification-1/duplicate/bootstrap-delivery.json) proves actual durable Completed status, HTTP 200 status read, original accepted HTTP 202 submission, original command/correlation, actual persisted `GlobalAdministratorSet`, and target actor. The earlier progress artifact remains false because it was written before the source read; the later delivery artifact records the completed proof.

The [native XML](qualification-1/duplicate/distributed.xml) fails next with `Authenticated bootstrap checkpoint reconciliation returned HTTP 403.` No later provider or full live regressions start. Complete local assemblies were run separately with the excluded live selectors explicitly recorded in their command receipts; they supply no distributed AC7 acceptance.

## Remaining blocker and acceptance limits

The fixture calls `POST api/v1/admin/projections/system/global-administrators/delivery-reconciliation/global-administrators/global-administrators`. EventStore's `AdminTenantAuthorizationFilter` requires a canonical public Tenant with matching grants; `system` is reserved. The action has no approved platform admission, and `AdminProjectionRebuildController.CreateScope` also rejects reserved public Tenant identity. A scope header cannot admit this existing route. No public `system` grant, history/checkpoint rewrite, source-helper maintenance expansion, or production maintenance API change was applied. [qualification-summary.json](qualification-summary.json) records the exact blocker.

| Criterion | Current evidence and limit |
| --- | --- |
| AC1 authorized list/open | Local query/evidence/topology and full local regressions pass; fresh native Project reads are not reached. |
| AC2 safe denial | Local security/privacy controls pass; fresh whole-chain denial/privacy/timing is behind the maintenance 403. Historical receipts remain historical. |
| AC3 honest snapshots | Local projection/query controls pass; fresh native snapshot scenarios are blocked before seeds. |
| AC4 compatibility/routing | Existing comparator and additive-contract local regressions pass; fresh native replay/duplicate matrix is incomplete. |
| AC5 allowed fields | Local response/evidence controls pass; fresh authenticated Project response matrix is blocked. |
| AC6 read-only/history | Exact original bootstrap event is proved without remapping history; fresh Project no-domain-write/restart/replay controls are not reached. |
| AC7 persisted verification | Prior owner source blocker is resolved natively. Remaining duplicate/full-replay/main and full live regressions are blocked by the exact maintenance 403. |

Previously recorded package-mode NU1109 remains separate; no package-mode rerun or acceptance is claimed. Parent workflow review remains pending.

## Cleanup

Both native attempts prove exact AppHost absence, all four captured containers absent, all six sidecars absent, private material removal and preservation of preexisting resources. [Fresh independent cleanup verification](fresh-cleanup-verification.json) rechecks every changed-source container by full ID and confirms no exact AppHost session remains. The local source-mapping workspace is removed after verification; its preparation command and emitted mapping are retained in `focused/`.


## Review corrections and evidence repair

Corrected source is `970e0d526fd71d5ca3dfba99b53515e662f10c26f56a2c2b7709740d76de4cfc`, separately recorded in [review-corrections/source-checkpoint.json](review-corrections/source-checkpoint.json). The initial native bootstrap proof, failed qualification, full local totals above and original command receipts remain historical at their actual original source; they are not new acceptance for these corrections.

- Tenants positive helper cases now compile only under the same Debug/source/`EnablePrivateOwnerSourceReadProof=true` condition as the host. An ordinary unselected Debug/source build compiles a separate test proving that explicitly selecting the helper in Development refuses before registration.
- IPC-dependent helper and Tenants cases use xUnit's platform-selection attributes for Linux. Portable default/refusal and direct complete-history controls remain active.
- Both shared and independent fixture validators require the original unbounded range from zero, no continuation/truncation, contiguous one-based event positions, and exact count/latest/last-sequence agreement. New cases reject upper bounds, omitted tails, gaps, duplicates, reordering, invalid positions and contradictory empty-stream metadata while preserving original command/correlation/actor binding.
- Corrected [owner patches](source-changes/index.json) use repository-relative paths. All three pass [reverse-application checks](source-changes/reverse-application-verification.json) under their actual owner roots without modifying Git state. The [byte-identical initial bundle](source-changes/original-qualification-1/index.json) is preserved separately. Its [relative representation](source-changes/qualification-1-relative/index.json) reconstructs every original file digest and passes [reverse checks](source-changes/qualification-1-relative/reconstruction-verification.json) in an owned temporary tree.
- All 31 original ignored command logs now have byte-identical checkout-visible copies, with [original/copy paths and digests](retained-output/index.json). Narrow packet-only Git attributes preserve those bytes; no ignore rules or staging changed. Initial failed builds and failed native attempts remain explicit.

Focused correction results are **32/32 shared helper**, **10/10 selected Tenants**, **9/9 unselected Tenants**, and **1/1 Projects bootstrap binding**, all with zero failures/errors/skips/not-run. Only the edited-file suites/method ran; no distributed runtime or broad regression run was repeated. All relevant builds and three repository diff checks pass. [Results and native XML](review-corrections/results.json) are separate from the original results.

Exact prerequisite, build and test commands now retain all explicitly configured source-reference, opt-in, dependency-root and MSBuild environment properties. The [fresh preparation command](review-corrections/prepare-source.command.json) and [emitted mapping](review-corrections/prepare-source.json) are followed by separate [selected](review-corrections/selected-environment.json) and [unselected](review-corrections/unselected-environment.json) environments. The command index labels old focused receipts as incomplete historical environments instead of adding unrecorded properties to them. A redundant preparation was interrupted before test execution and is recorded separately; no test result is claimed for it.

At the parent's explicit request, `/tmp/story61-owner-review` remains available for parent verification with the selected environment. The parent owns its removal after full checks. The original `/tmp/story61-owner-build` workspace remains removed. The later maintenance HTTP 403, package-mode conflict and native acceptance limits remain unchanged; [current focused verification](review-corrections/final-verification.json) claims no native acceptance at the corrected source.
