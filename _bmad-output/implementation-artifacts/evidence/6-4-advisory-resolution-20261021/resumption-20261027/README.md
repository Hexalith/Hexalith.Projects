# Story 6.4 resumption — 2026-10-27

Story 6.4 remains **in-progress**. Its advisory source implementation is present; Conversations 1.3.1 publication, official package verification and the consumer pins are unfinished. The current release prerequisite is failing upstream CI, before the separately required release approval.

Local Projects source is `83c6bef90bbac3bb74f41cdb77fe7e84d53c82e0`; local Conversations source is `8039d2abcf41bcd2c01fa3dbe618cbec2f4f6da3`. Read-only GitHub queries observed newer Conversations main `536bf80d5fd5c2a64c2f64815edc67074fd5a6ea`. No fetch, checkout, Git mutation, remote write, dependency update or runtime source edit was performed.

## Fresh local verification

| Lane | Passed | Evidence |
| --- | ---: | --- |
| Projects advisory, legacy resolution and scoped registration | 215 | [Projects XML](projects-focused.xml) |
| Conversations advisory, strict metadata and registration | 116 | [Conversations XML](conversations-focused.xml) |
| Production-authority planning guard | 37 | [exact commands/results](local-verification.json) |
| **Total** | **368** | Zero failures or skips |

Both source Debug test-project builds passed with zero warnings/errors. Story 6.4 authority and the complete production-authority index both pass. Parent independently parsed both XML files, checked their hashes and all 331 passing cases, inspected both build summaries and independently compared all 39 retained source hashes. Exact argv, working directories, exit codes and private build-log paths are in [local-verification.json](local-verification.json).

The frozen block retains SHA-256 `78e8a29557f949b064db4111f8d487ff4c4d15f3f7d57284e72905f797b9a7df`. Only 28 of the 39 files in the old source manifest still match; [source-audit.json](source-audit.json) names all eleven later changes. The core advisory executor, owner advisory handler and metadata model retain the reviewed hashes. All 65 historical matrix references and 256 historical patch references still resolve to passing recorded XML. Those receipts remain historical: full current suites, a fresh signed gateway/Dapr/Redis integration, live AppHost/browser tests and deployment acceptance were not rerun. This focused local result does not establish successful CI on newer live main.

## Current publication blocker

[Exact-source CI run 40499324347](https://github.com/Hexalith/Hexalith.Conversations/actions/runs/40499324347) failed on current main. [Job statuses](ci-jobs.json), [run metadata](ci-run.json) and [specific diagnostics](ci-diagnostics.json) identify:

- Missing Platform tool-selected source mapping prevents restore, portable conformance and Admin Web builds.
- The dependency-mode tooling regression fails for local Debug selection.
- The minimal-module fixture has inconsistent locked dependencies for EventStore ServiceDefaults and Aspire (`NU1004`).
- Windows source checkout fails on long Epic 16 fixture paths.

From `references/Hexalith.Conversations`, this read-only command exits **1**:

```bash
python3 scripts/verify-release-source.py --publish-enabled true --repository Hexalith/Hexalith.Conversations --dispatch-ref refs/heads/main --dispatch-sha 536bf80d5fd5c2a64c2f64815edc67074fd5a6ea --workflow ci.yml --release-version 1.3.1
```

Result: `Release-source verification failed: EXACT_CI_ABSENT: newest push CI run/attempt for the current main SHA is not successful`. [Exact command/result](release-gate.json).

The official destination verifier exits **0** for absent 1.3.1 destinations: [release-destinations.json](release-destinations.json). Builds and all four `tools/release-packages.json` Conversations pins remain at 1.3.0. No release was dispatched, and no installed 1.3.1 acceptance is claimed.

## Next work

Resume the producer-owned Conversations enrollment and CI remediation, then rerun the guard against the newest live-main SHA. Story 6.4's frozen scope excludes undertaking a host/platform migration here. Once current-source CI succeeds, obtain the separately required publication go-ahead, verify official package/symbol/tag/source state, and prepare the 1.3.1 consumer pin and package-mode validation. Existing approval records cover completed commits/pushes and do not supply the pending publication approval. Keep this story in-progress until delivery is verified.

Files remain unsupported/incomplete. Existing unlabeled Conversation rows benefit after reprojection or their next update; queries do not repair state.
