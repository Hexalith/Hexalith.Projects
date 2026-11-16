---
title: 'Projects CI and G-6 package compatibility'
type: 'bugfix'
created: '2026-11-16'
status: 'in-progress'
route: 'dispatch'
baseline_commit: '85ff97950a7cb290288d56618ed7da8152f5a2c6'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
---

<frozen-after-approval reason="human-owned intent — current invocation authorizes necessary alignment and qualification; publication requires separate package-owner approval">

## Intent

**Problem:** Exact-source Projects CI reaches Release compilation but cannot resolve QueryResultContractAttribute from EventStore.Contracts 5.1.1. G-6 acquires the catalog and prepares Conversations successfully, then cannot restore the pinned McpCli dependency Projects.Contracts 2.0.0-story66.3 from nuget.org.

**Approach:** Verify current immutable sources, ownership, approved feeds and actual APIs; prefer an existing published compatible artifact. Qualify the smallest dependency correction, including exact release artifacts if publication is unavoidable, and request package-owner approval only for a concrete qualified release proposal. Once prerequisites are published through their owners, deliver the authorized correction and obtain fresh hosted CI/G-6 evidence bound to the delivered source.

## Boundaries & Constraints

**Always:** Preserve the completed CiCd 194bdcb0481632499b7ed9d1e0730a47bfca2437 and Projects 7da7e95b670838de3181e946709e1246695a0e72 fixes, every unrelated pin and concurrent change. Use run-with-temp for disposable verification and retain useful logs externally. Change dependency pins only when demonstrably necessary for these failures. Keep any module-owned CI change behind qualified CiCd narrow delivery; extend its closed support rather than adopting Projects. Treat local package-feed proof as qualification only, never hosted acceptance. Retain and diagnose later failures.

**Never:** Remove metadata, hide package failures with source references, weaken gates/audits, broaden credentials, repeat publication/A8 work, initialize nested submodules, deploy, activate v2, or change Story 1.17 tracking. Do not publish new immutable package coordinates before owner approval. Do not publish changed current source under an earlier candidate identity.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Existing release | Approved nuget.org versions | Inspect actual binaries and provenance before choosing a version | A latest version lacking the API is rejected |
| Missing contracts | Projects annotations plus EventStore.Contracts 5.1.1 | Qualify a package containing the metadata type; preserve annotations | Keep compiler failures visible |
| Frozen enrollment | McpCli 5008de388c5e37257ca78e48d8afe899cf7aaa2a | Restore and build with the exact retained Projects.Contracts story66.3 artifact if compatible | Reject old UI closure and changed package identities |
| New publication | Required coordinate absent | Produce source, artifact hashes, exact coordinates, dependency closure and ordered release proposal | Stop at package-owner approval boundary |
| Subsequent failure | Target blockers cleared in temporary package proof | Preserve full logs and report independent blocker | No gate suppression or unrelated fixes |

</frozen-after-approval>

## Code Map

- `Directory.Packages.props` imports the direct Builds catalog. Its family property selects all EventStore packages at 5.1.1; a post-import PackageVersion Update can target Contracts alone.
- `tools/release-packages.json` records Contracts 5.1.1 in four exact expected dependency closures; change these only alongside the qualified Contracts pin.
- `src/Hexalith.Projects.Contracts/Queries/` carries required QueryResultContract annotations. Preserve all declarations and metadata.
- `references/Hexalith.EventStore` is pinned at 0be0c922883d4ea61b18e843e24472162261d934, the feature introducing QueryResultContractAttribute. Published 5.2.0 comes from earlier 8b6d5bee5217b3f4bbd8fd689ad887aca863961b and also lacks the API.
- `references/Hexalith.McpCli/Directory.Build.props` pins Projects.Contracts 2.0.0-story66.3, EventStore 5.1.0 and Abstractions 2.0.1; its dependency policy checks the exact closure. Preserve enrollment and unrelated pins.
- `nuget.config` approves nuget.org only. A private temporary feed is an explicit qualification input, never a production feed change.
- `evidence/epic6/6.6-cli-read-surface.json` binds retained story66.3 archive SHA256 349f938db77dd83ff45e200997b1cba97147c46b1855ec58fd6e2a4b0572eff5. Reuse its completed evidence; verify fresh compatibility without repeating publication/A8.
- CiCd `tools/deliver_g6_catalog.py` provides closed catalog/ci-gates profiles with exact trees, source rereads and scoped writer. Existing profiles must remain unchanged if later delivery requires extension.

## Tasks & Acceptance

**Execution:**
- [x] `evidence/projects-package-compatibility-20261116/` — record live feed, source/API and provenance evidence for both prerequisites.
- [x] `evidence/projects-package-compatibility-20261116/` — qualify exact EventStore.Contracts release candidate and retained Projects.Contracts candidate, keeping archives and hashes.
- [x] `Directory.Packages.props`, `tools/release-packages.json` — prepare and qualify the minimal Contracts-only pin candidate in a disposable copy; preserve all other pins, metadata and gitlinks.
- [x] `evidence/projects-package-compatibility-20261116/` — retain exact Release build/tests and pinned McpCli restore/build results, including later blockers.
- [x] `evidence/projects-package-compatibility-20261116/` — write a concrete ordered package-owner release proposal; independent review must assess artifact provenance and scope.
- [ ] After owner publication, qualify/deliver the necessary correction under existing ownership and obtain fresh hosted exact-source evidence. Leave pending if approval has not arrived.

**Acceptance Criteria:**
- Given existing published artifacts, when binaries are inspected, then an alignment is accepted only if the actual required API and compatible dependency graph exist.
- Given a proposed correction, when complete before/after sources are compared, then only necessary dependency-pin/manifest entries differ and all completed fixes and concurrent changes survive.
- Given local candidate feeds, when package-mode Release and pinned McpCli qualification run, then target blockers are resolved and every subsequent failure is separately retained.
- Given missing public prerequisites, when work reaches publication, then exact sources, coordinates, artifact hashes, validation limits and owner approval request are available first.
- Given owner-approved publication and delivered source, when fresh hosted CI/G-6 completes, then results are reported from the exact delivered SHA without claiming skipped or failed gates passed.

## Implementation Notes

The current invocation authorizes necessary pin alignment and qualification, and explicitly reserves new publication for package-owner approval. This is a freeform repair, not Story 1.17 or a sprint-status transition. Projects started clean; the dirty CiCd shared checkout is preserved through immutable-source/disposable workspaces. Planning investigation uses the skill-directed independent agents. Agent published_compatibility owns EventStore API/provenance and candidate packing in this evidence directory, including unpublished-candidates/Hexalith.EventStore.Contracts.5.3.0.nupkg; agent mcp_compatibility owns pinned McpCli/historical archive compatibility under the mcp/ subdirectory. The implementation handoff agent owns the combined minimal Projects pin candidate runner and Release qualification under combined/; reuse their artifacts and avoid duplicate investigation. Prepare patches in evidence and disposable copies only until public prerequisites and delivery qualification are available. The coordinator owns the final proposal, review and any later remote delivery. Do not overwrite another agent’s evidence.

Qualification is complete through the publication boundary. Contracts-only tree 61450315ae07ad6f9e34dbca31d815fae336b480 resolves the original 14 errors; 207 Contracts tests pass. Pinned McpCli restore/build/discovery passes using retained archive349f. Full Release remains failed with 13 subsequent Conversations/generator errors, diagnosed without additional pin changes. Package owner approval, publication, narrow delivery and fresh hosted acceptance remain pending. Concurrent Story 6.7 edits appeared in the shared checkout during qualification; the candidate preserves immutable85ff source and does not claim to include uncommitted work. CiCd remote main advanced independently to5da249288476d21095c3c677563974bf90dccbbe; future delivery must requalify current owner source.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Evidence and route |
|---|---|---|
| BH1 required failures can return exit0 | medium | Verified producer catches errors without exit failure and consumer runners lack final exit enforcement. Patch replay status handling; expected baseline/subsequent failures remain explicitly distinguished. |
| BH2 replay overwrites approved archive/receipts | medium | Producer copy2 and save target the existing approval paths. Patch immutable-output refusal and explicit fresh replay directories. |
| BH3 producer checkout is absolute | low | ROOT is hardcoded to this workstation. Direct correction to derive root from script path. |
| BH4 producer cache/public dependency provenance | medium | Producer inherits os.environ without a private NuGet cache; unlike consumers it does not isolate restore. Patch private cache and exact approved feed, then qualify producer again and compare to retained payload. |
| BH5 inspector version is unbound | medium | All replay commands use unversioned dnx. Current tool reports0.27.0+25095d2; pin0.27.0 in replay and retain versioned fresh proof. |
| BH6 producer replay omits enriched receipt fields | medium | Actual helper lacks the retained binary diff, availability, catalog and library enrichment. Patch reproducible qualification output without overwriting original evidence. |
| BH7 expected missing-type output is invalidJSON | low | published-es-generator-511-typed-emitter.json starts Error:. Preserve it as raw log and use a structured JSON receipt. |
| BH8 package comparison lacks entry/nuspec assertions | medium | Current retained/rebuilt packages actually have identical six entry sets and all non-DLL bytes, but comparator does not enforce these. Patch those assertions and run positive/negative payload cases. |
| BH9 additive public profile APIs lack focused tests | medium | Complete source suite executed: all130 Deployment and163 Queries cases passed, covering the new API families. Packaging retained52 failures and2 skips; no complete owner gate pass is claimed. Final exact-package Projects consumer passed207/207. |
| BH10 hash-bound evidence lacks byte attributes | medium | Root attributes cover older Story packets only. Add packet-local -text/-whitespace rules and verify effective attributes on receipts, patches, logs and archives. |
| Edge case layer | false | Reviewer returned an empty findings array after tracing the patch and replay scripts. |
| Verification gap layer | false | Reviewer found no verification gaps in runtime consumers or existing contract tests. |

## Verification

- Inspect actual nuget.org indexes/nuspecs and dotnet-inspect API surfaces; record full source revisions and artifact SHA256 values.
- Package-only Release builds with audits and warnings-as-errors retained; focused contract and enrollment behavior tests.
- Exact pinned McpCli G-6 restore command, then build; inspect dependency policy without modifying it.
- Complete source-tree, pin and gitlink comparison. No automatic publication or release dispatch before approval.

Final approval packet selects fresh public-cache EventStore.Contracts5.3.0 archiveSHA256 1c3bdabc4778467d74b468a47f0413f5f85272539d9969891036c8865f6949c3 from0be. Final exact-package consumer:207/207, zero skips/failures. Public API is identical to the earlier e367 archive, which remains unchanged. Producer complete suite ran1704:1650passed,52Packaging failures,2Packaging skips; all1328 non-Packaging cases, including130Deployment and163Queries, passed. Full producer gate is not accepted; mandatory owner release fixtures/gates remain required. Root complete Release13-error result belongs to the prior artifact qualification and was not rerun for the final archive. Three review layers completed, ten findings addressed for this compatibility scope and20 helper checks passed. The selected two package sources/coordinates/payloads are reviewable; owner approval, mandatory publication gates, actual publication, authorized owner delivery and fresh corrected-source hosted CI/G6 remain outstanding. No local commit or shared product/owner policy change was made.

Final source reread found concurrent Projects cd1fcd62e3180e5b037850cacb1c1756668b2866 and CiCd4d5a41c69b1c57e049a3d3a5c084259d896c6e5b. Private requalification rebases only the two necessary blobs onto current Projects, yielding tree080313f55def2d0972ab5cbaecfbaf5b5d27b7b3; all64 concurrent committed paths, including historical evidence deletions, and all12 gitlinks are preserved. All578 focused production/test/configuration/dependency input entries are identical to the tested85ff base. The207-case exact-artifact result is reused on that evidence; no new complete Release or hosted acceptance is claimed. Shared working-tree/index/branch state remains owned by concurrent work.
