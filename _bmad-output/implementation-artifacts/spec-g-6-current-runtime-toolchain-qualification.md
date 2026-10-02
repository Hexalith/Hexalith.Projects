---
title: 'G-6 Current Runtime and Toolchain Qualification'
type: 'feature'
created: '2026-10-01'
status: 'done'
baseline_commit: '4d8dcf65803792f7def3b10ed21227329536154b'
approved_at_utc: '2026-10-01T08:29:35Z'
approval_decision: 'Approve and stop'
approval_scope: 'spec, proposed tuple, and qualification run'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** G-6 fails seven checks after catalog/revision changes. Historical G-6 and separate P1R acceptance do not qualify this toolchain.

**Approach:** Qualify .NET SDK `10.0.401`, Aspire SDK/CLI/hosting `13.6.0`, Toolkit `13.6.0-beta.910`, Dapr CLI/runtime/.NET `1.18.0`/`1.18.2`/`1.18.10`, Fluent UI `5.0.0`, NSubstitute `6.2.0`, and Fluxor `6.11.0`. Align Aspire pins; capture isolated PostgreSQL two-sidecar stop/restart proof and a hashed review packet.

**Approval:** Spec approval authorizes this tuple/run as Builds, Platform, and FrontComposer/Web owner, including Toolkit prerelease and non-support-table-listed Dapr exceptions. Final acceptance separately requires a named decision on the packet hash.

## Boundaries & Constraints

**Always:** Preserve existing changes/history. Bind source, versions, outcomes, limitations, and cleanup. Distinguish checkout proof from published EventStore `3.110.0` archives. Attempt 16 retains its Debug/source and Release/packages evidence, committed closure, and exact-gitlink acceptance. Current G-6 uses a Debug/source runtime qualifier and builds selected source-mode consumers plus the package-mode Platform AppHost on the CI and release checkouts. Dirty source remains pending. For future G-6 applicability, use the approved 2026-10-02 material-input and exact-release-source policy below.

**Never:** Stage/commit/push, publish/deploy, initialize nested submodules, mutate domain data, interrupt shared Dapr/Redis, infer downstream approval, or activate Dapr.Workflow. Inventory Platform Works/mTLS `1.18.3` as an unselected, unqualified preview.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Pin audit | Current root-declared consumers | Exact tuple or explicit exclusion | Missing/drifting consumer fails |
| Isolated lifecycle | Two distinct sidecars and disposable PostgreSQL | One execution; identical survivor/restart replay and authority | Missing persistence, duplicate execution, or skips fail |
| Packet mutation | Altered source, outcome, approval, or limitation | Deterministic rejection | No partial acceptance |

</frozen-after-approval>

## Approved 2026-10-02 G-6 applicability amendment

Jérôme approved the [2026-10-02 Sprint Change Proposal](../planning-artifacts/sprint-change-proposal-2026-10-02.md) after reviewing its complete change set. This supersedes the original requirement to recapture and reaccept the entire 13-repository, 12,244-file packet after every gitlink movement. It does not change the bytes or scope of the accepted attempt-16 packet, approve the newer `13.6.0-preview.1.261001-0243` Toolkit version, or grant downstream readiness or release authority.

Current G-6 applicability requires an approved effective runtime/toolchain tuple and passing isolated PostgreSQL two-sidecar restart proof for a reviewed fingerprint of material runtime inputs. The tested root SHA and gitlinks remain provenance; an unrelated gitlink advance alone does not revoke tuple approval or matching runtime proof. A controlled version, exception, tested runtime path, fixture, or critical result change requires a new qualifying run. A new tuple or exception also requires a named Builds, Platform, and FrontComposer/Web owner disposition. A dirty local checkout cannot claim current qualification.

The qualifier must retain actual result counts, environment and resolved package versions, source/package distinction, limitations, and exact owned-resource cleanup. Failed, skipped, or unavailable critical evidence is `failed` or `not verified`, never `qualified`. Exact-source release qualification reruns the focused proof on the current release SHA and checks the resulting artifact against that SHA and the approved tuple. Historical source-mode proof does not qualify published EventStore archives. G-4/G-5, P1R, AD-30, Story 6.1, Story 8.11, Dapr.Workflow, deployment, and publication retain their separate gates.

The initial v3 implementation disables non-release proof reuse until Platform's file-based transitive restore graph is covered. CI audits every checkout, runs live qualification for material changes, runtime-owning gitlinks, uncertain change ranges, and schedules, and reports unrelated changes as `not required by this change` without claiming G-6 has passed. Release always reruns the live proof.

## Code Map

- Builds `Tools` contracts and `schemas/hexalith.runtime-toolchain-evidence.v1.json`: preserve dirty stable-Fluent work and historical requirements/hashes.
- Root/sibling AppHosts, Builds Module.AppHost, Platform file-based directives, and McpCli transitive Dapr consumers need coverage.
- EventStore `Oq8PostgresqlFixture` owns binary/output, control-plane, and Redis selection. Reuse `DockerPublishedPortResolver` and the existing OQ8 matrix.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-10-01.json` -- add authorized tuple, exceptions, rollback, and consumer inventory.
- [x] `src/Hexalith.Projects.AppHost/*.csproj`, `references/*/src/**/*AppHost/*.csproj`, `references/Hexalith.Platform/apphost.cs`, and FrontComposer `.github/workflows/quality.yml` -- align Aspire/Toolkit; bind Platform EventStore to P1R `3.110.0`.
- [x] Builds `Tools/{validate-runtime-toolchain-evidence.py,test-runtime-toolchain-evidence-validator.py}` and schema -- audit file-based directives, Aspire.Hosting, Dapr packages, and EventStore binding; hash closure, AppHost outcomes, resolved packages, audit, and cleanup. Version new requirements while preserving history.
- [x] `references/Hexalith.EventStore/tests/Hexalith.EventStore.Server.LiveSidecar.Tests/Fixtures/Oq8PostgresqlFixture.cs` and fixture tests -- add validated binary/configuration/container-name/Redis overrides; use disposable loopback resources and exact-ID teardown.
- [x] `tools/qualification/run_g6_qualification.py` -- require approved inputs; orchestrate isolated tools/cache, fresh builds, exact selectors, sanitization, failed attempts, and unchanged shared-resource checks.
- [x] `.github/workflows/ci.yml`, `tests/tools/{run-ci-workflow-gates.ps1,test_g6_packet_references.py}` -- select new packet/baseline; align Builds execution with root gitlink; preserve gate isolation.
- [x] `_bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain-20261001/` -- retain actual commands/counts/versions/hashes/limitations/rollback; failures remain failures.
- [x] `_bmad-output/implementation-artifacts/{sprint-status.yaml,6-1-p1r-current-exact-baseline-candidate.md}`, the existing context spine, `docs/runbooks/projects-topology.md`, and `tests/e2e/README.md` -- coordinate candidate truth through the scheduling guard; preserve concurrent changes/downstream states.

**Acceptance Criteria:**
- Given approved inputs, when consumers restore/audit, then pins match or have explicit unqualified exclusions.
- Given isolated resources, when qualifier/support run, then 1 qualification and 33 cases across 21 selectors pass without failures/skips; shared resources remain unchanged.
- Given a candidate, when validators/mutations run, then hashes/outcomes reconcile and stale, unapproved, secret-bearing, or contradictory evidence fails.
- Given dirty source or pending acceptance, when gates run, then prerequisite usability remains false and downstream states remain intact.

## Implementation Notes

Implemented the approved v2 baseline, pin inventory, current evidence contract/schema, complete OQ8 semantics and hash/authority mutation controls, isolated runner and fixture overrides, CI selection, retained failed-attempt packets and coordinated planning/docs. Historical v1 baselines and acceptance remain preserved. The fixture now uses exact-ID teardown, loopback PostgreSQL publication and a unique validated Dapr namespace for every owned sidecar launch.

Previously selected, now superseded attempt 3 packet SHA-256 is `724939520f2d8850073fd0091ffb7f0ada88e87231bb6ff457063bacb562a6bd`. Actual results: 1 failed qualifier, 33 passed deterministic support cases across 21 selectors, 21 passed fixture controls, zero skips. Five AppHosts pass and five fail; Platform and the separate McpCli consumer pass after routine harness/configuration corrections. Namespace-isolated `sample/process` invocation still returns HTTP 404, producing HTTP 500 at concurrent writer admission. Strict observation validation fails. Projects/Memories/Tenants preserve their missing nested Polymorphic source gate; FrontComposer preserves missing CounterFixture EventStore references, and FrontComposer/Parties preserve missing UI types. Shared resource snapshots remain unchanged, four owned containers are removed, owned process groups stop and scratch is removed.

Superseded attempt 3 post-capture verification: a source-only closure correction allows later metadata-only root evidence/acceptance commits, requiring an ancestor captured root revision and identical committed bound source/gitlinks at both revisions. Exact submodule HEAD/gitlinks remain required. At that point, a full accepted positive control and 40 mutation controls passed. Attempt 3 was preserved, with no rewritten source bindings, and became additionally stale against those validator/runner/test bytes. Its gate exited 1 on the mutation-test artifact hash plus EventStore checkout HEAD `96a6041c5d63a933a58f1df09f7fad123c9ada41` versus then-root gitlink `6dededdecd62dd6dc6d1f15810108d860ec70c8f`. No named G-6 acceptance was inferred.

Resumed technical remediation is complete within the frozen boundary. FrontComposer now resolves initialized module dependencies from the permitted umbrella siblings, and Parties probes its direct FrontComposer sibling. Memories removes only the obsolete PolymorphicSerializations requirement: no current source/project dependency uses it. Its `CheckSubmodules` target and all actual dependency requirements remain enforced. A positive MSBuild guard check passes; an isolated synthetic missing required dependency still fails with the exact missing-submodule error. No nested checkout was initialized and no guard was disabled.

The HTTP failure occurred on a correct POST through each assigned Dapr endpoint before the intended Sample handler. Pinned mDNS lookup ignores `NAMESPACE`; the namespace alone did not isolate discovery from shared application IDs. Every isolated sidecar/restart now selects SQLite v1 discovery against one fixture-private registry, while `NAMESPACE` continues to isolate actors/scheduler and the PostgreSQL domain store, application IDs and OQ8 matrix remain unchanged. Evidence binds the registry path identity hash separately from the generated configuration byte hash, observes registry creation after readiness, and proves fixture scratch removal. SQLite name resolution is Alpha and qualifies only disposable fixture infrastructure. Safe routing diagnostics retain method, authority/path, status and whitelisted Dapr error code; four controls prove response preservation and exclusion of protected body/query content. Failed Docker launch ownership now records an exact CID before raising; CID/no-CID controls prove cleanup never targets shared names or containers.

Published EventStore.Aspire `3.110.0` brought older transitive Keycloak/Redis hosting versions into Builds Module.AppHost and Platform package consumption. Explicit consumer declarations now select the already approved catalog versions (`13.6.0` Redis; `13.6.0-preview.1.26479.8` Keycloak). Keycloak runtime remains unqualified. File-based inventory applies the same exact exclusion crosswalk as XML; positive and negative parser controls reject older or unauthorized preview pins. Attempts 4–6 remain preserved: attempt 4 retained the runtime/build failures; attempts 5 and 6 passed actual commands but correctly failed package reconciliation before these final corrections.

Superseded attempt 7 packet SHA-256 is `edd626a36f50a53114f3abdfe0c6b5cf725a75dad9b46801f679882342f717fa`. All 22 recorded commands pass: 1 qualifier, 33 support cases across 21 selectors, 27 fixture controls, all 10 AppHosts, McpCli, two container-cleanup controls, 52 current evidence controls, 90 historical packet scenarios, 48 baseline-drift controls, 165 authority controls, and the root workflow contract. All test lanes have zero failures/skips. Strict OQ8 and current candidate validation pass. Eight sidecar launches/restarts share the private resolver bindings; shared resource snapshots match, all four exact owned containers are removed, process groups stop and both scratch areas are removed. The 11,887-file source manifest matches current bytes. Root capture revision is `bb364eb86e2008068617d4f95fb32c2661a8a08f`; EventStore checkout/gitlink is `08b9cfbce38fd34b9d791c63c848a8e685d72f36`, with 22 commits and 44 changed source paths from published `v3.110.0` (40 added, 4 modified). Checkout proof does not qualify those published archives.

The status-aware gate truthfully remains blocked only by the pre-existing Parties checkout `8c49383e94d16162ef30d102f4c3e536cda9790f` versus root gitlink `60b9836ea23151c5319dd06fd3deb80122f7abc3`. Neither was changed. Dirty committed closure is incomplete, and named final acceptance is ungranted. The packet remains pending with `technicalValidity=true`, `closure.committed=false`, null acceptance and `usableAsPrerequisite=false`; accepted-only validation rejects it.

Superseded attempt 14 packet SHA-256 is `9caa2699bc6dca0c2366e0f9eaa088a9ffa9ce9ec4d27f448b31b8c53a87cfaa`. All 22 recorded commands pass: 1 qualifier, 33 support cases across 21 selectors, 31 fixture controls, all 10 AppHosts, McpCli, 12 runner controls, 103 current evidence controls plus an accepted metadata-commit positive control, 90 historical packet scenarios, 48 baseline controls, 165 authority controls, and the root workflow contract. Strict OQ8 capture and independent candidate validation pass with zero failures/skips. Pre/post source snapshots match; the 12,246-file source manifest digest is `656f5a357bbb4ddb370c4d6d7777ddb9152ad43b1cb6138bcabc52a64a374401`. Shared snapshots match, four exact owned containers are removed, all recorded owned groups have disappeared, and both scratch areas are removed. Eight sidecar launches/restarts share the fixture-private discovery bindings.

Review corrections bind exact commands, controlled effective versions, mandatory resolved dependencies, canonical policy paths, actual runner errors, JSON types and complete cleanup roles. Runtime JSON/scripts and tracked deletions are in source closure. PostgreSQL retains an exact CID on failure/timeout. Safe failed/skipped support results are projected after the unchanged strict capture validator returns, and stale fixture binaries are never executed. Immutable Builds-local fixtures have pinned provenance; the final standalone Builds CI command passes 103 current controls without Projects/EventStore siblings. Attempts 8–12 retain infrastructure or harness failures, and attempt 13 truthfully fails its shared-snapshot check after a separate Aspire session restarted shared resources. Every earlier packet/receipt remains unchanged.

Remaining acceptance work:

- [x] Resolve the observed runtime/domain-service HTTP 404/500 failure without weakening the matrix; capture 1 qualifier and 33 support passes with zero failures/skips.
- [x] Build every required AppHost with current resolved packages using permitted source prerequisites; no nested dependencies were initialized or guards bypassed.
- [x] Recapture final committed implementation/source and exact root gitlinks; validate the complete new packet and cleanup.
- [x] Obtain a separate named decision on the final reviewed packet hash before prerequisite usability can become true.

## Committed-closure authorization

2026-10-01: The user instructed “do” in response to the scoped closure prompt. This authorizes staging and local Conventional Commits of reviewed G-6 changes, exact root gitlinks and the Builds CI SHA, followed by a fresh committed-source capture and evidence-only commits. Unrelated work is preserved and excluded using a clean isolated checkout when necessary. Push, publication, deployment, nested submodule initialization, downstream approval and Dapr.Workflow activation remain excluded. The new packet stays pending and unusable until a separate explicit decision by Jérôme Piquot on its reviewed hash. The original frozen intent, recorded approval and baseline commit remain historical and unchanged.

## Spec Change Log

2026-10-02: Jérôme approved the material-input and exact-release-source applicability amendment above. The attempt-16 approval, packet, tests, and historical exact-gitlink interpretation remain immutable for their recorded source. New Toolkit preview qualification and its separate owner disposition are pending.

2026-10-01: Earlier attempt 3 implementation/evidence notes added; frozen approved intent unchanged. At that point, the spec remained in-progress because live qualification and final committed acceptance were incomplete.

2026-10-01: Resumed source-prerequisite, private discovery, exact transitive pin and evidence-control corrections completed. Attempt 7 is technically valid and retained; committed exact-gitlink closure and named acceptance remain incomplete. Frozen approved intent is unchanged.

2026-10-01: All 24 review/verification findings were patched and verified. Fresh attempt 14 passes the technical acceptance criteria. Frozen intent and spec baseline commit remain unchanged; committed exact-gitlink closure and named packet acceptance remain pending.

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B1 | high | patch | Runner captures source only after builds/tests; there is no initial source snapshot or stability comparison, so a concurrent code edit can be attributed to prior binaries. |
| B2 | high | patch | The source predicate includes selected JSON names but excludes realm/launch/Aspire JSON and shell scripts; those runtime/test inputs can change without changing the claimed closure. |
| B3 | high | patch | StartPostgresAsync assigns ownership only after RunProcessAsync returns; nonzero launch or timeout throws before assignment, preventing exact-ID cleanup. |
| B4 | medium | patch | The failure fallback calls passing-only sanitize_support_ctrf; a failed/skipped case raises before actual support counts are retained, and scratch cleanup removes the native report. |
| B5 | high | patch | Required commands are checked for purpose/exit only except McpCli; substituted commands can retain passing hashes and fixtures without matching the required builds/selectors. |
| B6 | medium | patch | XML inventory reads Version and catalog values without VersionOverride; an explicit override can be reported as the approved catalog pin. |
| B7 | high | patch | Resolved rows require only nonempty packages, then validate only listed entries; omitting controlled dependencies can evade version verification. |
| B8 | medium | patch | Policy artifact kinds have no canonical-path crosswalk; a valid hash of an unrelated log can be labeled as the current validator/schema/tool. |
| B9 | high | patch | Cleanup compares sets without expected resource coverage; two empty sets pass despite requiring owned PostgreSQL, placement, scheduler and Redis. |
| B10 | medium | patch | Technical validity does not read attempts.errors; retained runner exceptions can coexist with a passing claim. |
| E1 | high | patch | A missing tracked root source is omitted by tracked_files; root_source_dirty checks only surviving entries and can report clean committed closure. The reviewer reproduced the missing src/Removed.cs path. |
| E2 | high | patch | Independent path tracing confirms the same absence of a pre-execution source snapshot as B1; final source bytes need not be the tested bytes. |
| E3 | high | patch | Independent tracing confirms the PostgreSQL ownership gap in B3: the process helper throws before the exact ID field is assigned. |
| E4 | medium | patch | Fixture override tests run unconditionally after a failed fresh qualifier build; an existing Debug assembly can supply stale control results even though the overall qualification fails. |
| E5 | medium | patch | Packet-reference tests normalize accepted packets for reviewed hashes but unconditionally assert packet usability false; valid named accepted state is therefore rejected by that gate. |
| E6 | medium | patch | Independent XML tracing confirms the ignored VersionOverride described in B6; audit versions can disagree with effective package declarations. |
| V1 | medium | patch | Pre-verified gap: current controls have only a root binding, and removing the committed-gitlink loop still passes all controls. Add captured/current commit and index gitlink distinction cases. |
| V2 | medium | patch | Pre-verified gap: no test calls resolved(); fabricated rows cannot detect omitted controlled/transitive packages. Add actual temporary project-assets projection assertions. |
| V3 | high | patch | Builds standalone Python CI checks out without submodules and invokes the new controls, whose fixtures are outside Builds; the standalone reproduction fails with missing external EventStore validator. |
| P1 | medium | patch | Python equality allows integer 1/0 to satisfy boolean technicalValidity/closure flags and boolean values to satisfy integer aggregate test counts; exact JSON types and mutation controls are required to reject those contradictory schema values. |
| R1 | medium | patch | Full verification preflight rejected the existing Hexalith.Commons catalog property because strict controlled-version parsing was applied to every PackageVersion. Restrict the check to controlled package families and retain an unrelated-property regression. No capture or runtime resources were created. |
| R2 | medium | patch | Attempt 9 passes the qualifier and support selectors, but prewriting deterministic-support.json violates the unchanged strict validator's fresh-directory contract. Retain failed support safely after strict validation returns; preserve attempt 9 as a failed capture and verify through a fresh run. |
| R3 | medium | patch | Attempt 9 builds every AppHost and McpCli, then required_packages rejects FrontComposer's compound Debug/source group containing only ProjectReferences. Evaluate conditions only on ancestors of controlled PackageReferences, retain rejection of unresolved relevant conditions, and check every actual required consumer. |
| R4 | medium | patch | Attempt 10 passes all 22 commands, but cleanup sets ownedProcessesStopped=false whenever it sends SIGKILL without checking disappearance afterward; an independent scan found no remaining members of all recorded groups. Prove final disappearance with a bounded check, retain false for persistent/unverifiable groups, and report genuinely failed cleanup as a runner error/technical failure. |

Grouped patches: B1/E2 source stability; B2/E1 complete/deleted source closure; B3/E3 PostgreSQL launch ownership; B4 failed-result retention; B5 command binding; B6/E6 effective pin audit; B7/V2 resolved package coverage and projection controls; B8 canonical policy artifacts; B9 resource-role coverage; B10 runner errors; E4 stale fixture prevention; E5 accepted-state reference gate; V1 committed gitlink controls; V3 standalone Builds fixtures; P1 exact JSON flag/count types. All smallest fixes stay within existing private qualification/test behavior and add no public runtime surface. Frozen intent and previously captured packet bytes remain unchanged.

## Verification

- `python3 references/Hexalith.Builds/Tools/test-runtime-toolchain-evidence-validator.py` -- controls pass; retain emitted counts.
- Existing root gate/gitlink fixtures and updated packet-reference tests -- pass.
- `pwsh -NoProfile -File tests/tools/run-ci-workflow-gates.ps1` -- pass.
- Per-project builds/tests, exact OQ8/support commands from EventStore `integration.yml`, and strict capture validation -- zero skips; retain outcomes.
- Candidate/accepted-only gates -- distinguish technical validity from closure/acceptance.
- `python3 tools/planning/validate_production_authority.py --validate-index` and whitespace checks -- pass before/after planning edits.

Superseded attempt 3 verification outcomes (2026-10-01): then-current 40 mutation controls plus accepted metadata-commit positive control; historical 30 scenarios across three baselines, 48 baseline-drift controls, 165 authority controls and two historical hash pins passed. Root gate/gitlink fixtures passed 19 controls; packet-reference tests passed 5 controls after coordinated updates; workflow gates and scheduling guard passed. Whitespace checks passed in every changed repository. Candidate validation/gate and accepted-only validation exited 1 for the retained failed/stale/pending packet. Those historical commands/results remain in `qualification-evidence/g-6-runtime-toolchain-20261001/post-capture-implementation-verification.md`.

Superseded attempt 7 verification: all 22 commands and all three matrix rows pass, including exact pin/exclusion controls, unchanged OQ8 lifecycle assertions and 52 current mutations. Independent candidate validation exits 0; the root candidate gate exits 1 solely on the preserved Parties checkout/root-gitlink mismatch; accepted-only validation exits 1 because the packet is pending. The root gate/gitlink/reference/cleanup suite passes 26 tests, and production-authority controls pass 21. Guarded sprint candidate and active-index validation both pass with downstream states preserved.

Final attempt 14 verification: independent candidate validation exits 0; the root candidate gate exits 1 solely on the preserved Parties checkout/root-gitlink mismatch; accepted-only validation exits 1 because acceptance is pending. Root gate/gitlink/reference/runner controls pass 38 tests, production-authority controls pass 21, and the focused freshly built Memories source guard passes 1 test. Standalone Builds CI passes 103 current controls and all historical controls. Scheduling-index validation passes before edits, on the staged metadata candidate and after replacement; whitespace checks pass in all ten dirty repositories. Of 11,880 pre-resume source paths, 11,864 retain identical bytes and 16 intentional implementation paths differ; no original path is missing. Final evidence and verification notes are retained under `qualification-evidence/g-6-runtime-toolchain-20261001/attempt-14/`.

At the end of the attempt-14 phase, Build workflow status `done` recorded completion of the authorized implementation, review patches and technical qualification. It supplies no immutable G-6 acceptance: committed exact-gitlink closure and the separate named decision remain pending. The frozen `Never: Stage/commit/push` constraint overrides the workflow's generic local-commit instruction; no commit or push was created. G-6 is a qualification gate, so no epic-story development status is advanced.

## Committed-source closure result

Attempt 16, reviewed SHA-256 `b7f940e512e17bcad98b021e4a7d4b9acce4f0c9fba4d3dc23f9d160245be139`, captured committed root `b26129c35604689a3a0a13e8af545bab7586e77e` and all 12 exact gitlinks in clean isolated checkout `/home/administrator/projects/hexalith/g6-committed-closure-20261001`. All 22 commands pass with the unchanged qualification/support matrix and zero failures/skips; cleanup and pre/post source checks pass. Builds/root CI execution is `51af786cf156d2a3396dbd49f5e4898222e55c12`; EventStore is `8096455e4f23f2912998e36738058b8e3d961be6`; Parties is `937cb2a343aaa74963db9bb867a2c3a01ff48677`. `technicalValidity=true`, `closure.committed=true`, acceptance null and prerequisite usability false. Attempt 15 retains the Docker scheduler port failure; all earlier packet/receipt bytes are unchanged. Original unrelated Builds G-4 work is preserved and excluded. The frozen intent/hash and original spec baseline remain unchanged. Jérôme Piquot subsequently accepted attempt 16 at `2026-10-01T14:58:12Z` on this reviewed hash. The accepted packet grants G-6 usability for its recorded exact-source scope; no push or downstream approval occurred.

## Final named acceptance

The user stated “I Jérôme Piquot accept attempt 16” after receiving the reviewed packet SHA-256 `b7f940e512e17bcad98b021e4a7d4b9acce4f0c9fba4d3dc23f9d160245be139` and its limitations. The decision is recorded at `2026-10-01T14:58:12Z` in `_bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain-20261001/attempt-16/acceptance.json` and the packet's exact named acceptance fields. The reviewed pending bytes are preserved as `reviewed-packet.json`; technical evidence, frozen intent, spec baseline and source bindings are unchanged. Status is accepted and G-6 prerequisite usability true. Accepted-only and status-aware gates pass in the retained clean checkout. Current P1R usability, downstream readiness, G-4/G-5, package archives, rollback, release, deployment and Dapr.Workflow retain their separate limits.
