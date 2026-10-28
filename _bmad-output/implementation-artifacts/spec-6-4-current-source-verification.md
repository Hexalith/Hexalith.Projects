---
title: 'Story 6.4: Current-source verification preparation'
type: 'chore'
created: '2026-10-28'
status: 'done'
route: 'oneshot'
baseline_commit: '0e45105476902fcfce1f8dbd8416d7234bb16b41'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="User explicitly authorized isolated upstream source/configuration preparation and testing on 2026-10-28">

## Intent

Investigate HXW004 between Commons checkout `3edfbdafc7d4fa915286dce8a37dc4cc7c06085a` and umbrella gitlink `1663028fb6d9f46a0bf17919edeb21bb99be1587`. Establish supported consistent current-source verification inputs after verifying owner identities. Rerun public source preparation and the narrowest relevant Story 6.4 builds/tests using its actual generated mapping. Preserve all current working trees, indexes and user changes. Keep Platform release/package identity gates intact. Do not commit, push, publish, deploy, initialize nested submodules, select unqualified dependency versions or fabricate declarations or mappings. Record commands, identities, results, limitations and the next remaining blocker. Do not claim Story completion.

</frozen-after-approval>

## Implementation Notes

This is bounded source-verification preparation, not a change to advisory behavior or Story acceptance. The existing advisory spec and sprint status stay untouched. The user's explicit preservation and isolated-workspace authorization resolves the workflow's dirty-tree concern; no additional permission is needed. No irreversible operation or unresolved intent choice is required.

Investigation: Builds `Workspace/SubmoduleInitializer.cs` explicitly supports stage-zero gitlinks ahead of HEAD and refuses an existing checkout mismatch. `WorkspaceRootResolver.cs` derives the active module from the declaration's owner and uses only root-declared references. `PlatformIdentityResolver.cs` separately uses committed Platform identity; source mode warns on qualification problems while package mode refuses them. No resolver change is warranted. Commons' two identities differ only in shared instruction files.

Prepare an isolated clone of Projects at its existing canonical HEAD, with actual current clean owner source revisions, verified individually before selection. Use the qualified Builds CLI/catalog release source `16ff70a2321ad576faaac3fe53088c2f1bdacae3`, rather than either the dirty catalog's version changes or a moving Builds branch. Preserve the original Platform candidate, but select its committed source for this source-only check. Align only the isolated root index to selected actual checkouts; retain its unmodified HEAD, with no fixture commit. Materialize direct submodules only; verify parent ownership and no nested initialization. Keep a full input inventory explaining deliberate differences from current owner HEADs and committed umbrella pins.

Use Conversations' existing manifest and accepted CLI 4.38.1 for `prepare-build --mode source`. Retain the actual output mapping and imports byte-for-byte, and pass the exact emitted environment into individual Debug builds. Build Contracts.Tests and run the existing mapping boundary class with a clean child environment, then build Server.Tests and run the six existing advisory/strict/registration/projection/recovery classes. If current-source dependencies fail, record the exact failure and use focused existing lanes without suppressing analyzers or overriding dependencies. Recheck package refusal and Projects root mapping with actual public inputs. Projects has no supported root declaration or active-root override; do not invent one to force the Conversations mapping onto Projects.

Evidence is confined to `evidence/6-4-advisory-resolution-20261021/source-verification-20261028/` plus this preparation spec. Capture exact argument arrays, working directories, explicit environments, UTC times, outputs and exits, full canonical Git identities, source/mapping hashes and original-state preservation. Review this new scope with the configured Blind Hunter subagent before finalizing; keep any remaining source, package, root-mapping, runtime and acceptance limitations explicit.


Preparation results: original HXW004 reproduced, then resolved in `/tmp/story64-source-verification-20261028/projects` using real verified owner commits and isolated stage-zero gitlinks only. No source correction or fabricated composition commit was needed. Actual producer mapping is retained unchanged. Its catalog remains Platform-owned `51a71cb1cfe860d5fc135eb3b8521fd80c413810`; the qualified `16ff70a2321ad576faaac3fe53088c2f1bdacae3` checkout provides tooling source, not a replacement for that committed catalog. Raw versus materialized catalog bytes differ only by the producer's omitted UTF-8 BOM.

Two mapped Conversations Debug builds pass with zero warnings/errors. Ten existing mapping cases and 243 existing owner cases pass without skips. The same narrow lanes were repeated after review to capture effective environment, SDK 10.0.402/MSBuild identity and an unchanged 499-file executable closure before/after tests; there are 253 unique cases, not 506. Source preparation still warns HXW008/HXW011/HXW009; public package preparation refuses them. Both actual-mapped Projects test-project builds refuse HXW006 before compilation because no supported Projects root mapping route exists. That is the next current-source blocker. No Story completion or package/runtime qualification follows.

Original thirteen-repository HEAD/ref/index/user-file/nested-marker preservation passes. The prior advisory spec, frozen intent and sprint status remain untouched. The receipt binds source identities, exact captured commands/environments/streams/exits, source and generated-input hashes, XML and limitations. Initial workflow-render and failed BOM-assertion harness outer process streams/timing were not captured; `outer-invocations.json` explicitly records that limitation without reconstructing missing metadata. All later final harness commands and nested results are captured.

## Review Triage Log

1. Low, patched: disable bytecode before importing the live audit helper; cache-free read-only control passes.
2. Medium, patched: verify isolated root HEAD/index, child HEADs/parent ownership and complete status; index-drift control fails as required.
3. Medium, patched: inventory imported configuration across selected trees, including Builds `Props/Environment.Build.props`.
4. Medium, patched: reject unexpected tracked/staged/untracked files; newly added source control fails as required.
5. Medium, patched: hash the complete 499-file executable output closure before and after focused execution; changed production DLL control fails.
6. Medium, patched: record sanitized effective build/tool environment allowlist for final repeated lanes; retain earlier capture limitations explicitly.
7. Medium, patched: capture resolved executable and actual .NET SDK/MSBuild information from the owner directory.
8. Low, patched: identify exact initial outer invocations and unavailable raw streams/timestamps in `outer-invocations.json`; final nested harness receipts are complete.
9. Low, patched: describe twelve direct references total, including active Conversations.
10. Medium, patched: refuse optimized Python before assertions; optimization control fails as required.

Full findings, evidence and patch triage are in the receipt's `review-triage.md`. No review item was deferred. These are evidence-tooling corrections; production source and producer-generated mappings remain unchanged.

The original reviewer independently confirmed all ten findings closed and reran the live read-only audit successfully, including optimized-execution rejection and no evidence or bytecode writes. This preparatory task is complete within the authorized scope; Story 6.4 remains in progress. No commit was made because the user prohibited commits.
