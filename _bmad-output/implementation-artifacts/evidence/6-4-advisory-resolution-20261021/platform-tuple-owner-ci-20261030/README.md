# Platform tuple owner CI — 2026-10-30

**The authorized push, workflow registration and exact-candidate CI operation is complete.** Selected candidate **`6a186754102b33c3cfea823efc55e49264efbb84`** pins published Builds/tool **4.38.3**, exact source **`d1931b1e803144048d927fff16cb8119afde541e`**, including Tenants **5.11.1**. [Hosted run 41014652010](https://github.com/Hexalith/Hexalith.Platform/actions/runs/41014652010) passed: complete solution package-only Release build, zero warnings/errors, 14 binding controls, 82/82 required cases and 27 durable restored project graphs.

**This is a passing frozen-candidate preflight, not current-main, runtime, published-tuple or consumer acceptance.** Story 6.4 remains in progress. Artifact inspection exposed a separate host package pin, and remote main independently advanced to a different Builds source/catalog. No accepted published tuple for that current main has been established. [qualification-handoff.json](qualification-handoff.json) and [qualification-next-actions.md](qualification-next-actions.md) contain the exact remaining gates and authorization.

## Authorized operations and changes

The user's “I authorize” followed the concrete request to push the candidate, register the reviewed workflow through the owner process and execute exact-candidate CI. The frozen candidate was pushed unchanged on `fix/story-6-4-builds-4-38-3-candidate-20261030`; no force push or source-history rewrite occurred.

[PR #8](https://github.com/Hexalith/Hexalith.Platform/pull/8) registered only the byte-identical reviewed manual workflow on main. Head `a7f23df00007ddbb4232221ba99e857879d43075`, base `f60f2c7596b1fa3010c91d1d546c71600da24715`, final merge `357d8cfc074d79042dc4395cf7fc2ff51034c74b`. The active ruleset requires review and explicitly grants `jpiquot` an always-allowed owner exception. That configured exception was used for this authorized workflow-only registration, recorded before merge; no rules were changed or GitHub approval fabricated. The registration diff was verified to contain only the workflow. It runs manually with read-only contents permission and no publish/deploy step. GitHub's [manual workflow documentation](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/manually-run-a-workflow?tool=webui) explains its default-branch prerequisite.

Two actual CI failures exposed small local corrections. Commit `0e524c17333ba181ff7a6a2384b680da9760489e` adds the existing ArchiveProcessFixture to the solution configuration map; the Release guard stays intact. Commit `6a186754102b33c3cfea823efc55e49264efbb84` arms the worker test's unchanged 200 ms caller timeout after setup and cancels/observes callers before cleanup. The four-slot, fifth-caller and late-completion assertions remain. Only setup gets a ten-second barrier; no production limit, required suite or dependency version changes. CRLF attributes retain ordinary whitespace checks. Independent review closed all findings; see [review-triage.md](review-triage.md).

[PR #9](https://github.com/Hexalith/Hexalith.Platform/pull/9) closed after other pushes incorporated prior candidate work. The remaining test correction is reviewable in [draft PR #10](https://github.com/Hexalith/Hexalith.Platform/pull/10). This operation did not merge that product correction, publish, deploy or adopt consumer pins.

## Actual CI results

All runs used attempt 1. Workflow head and explicitly checked-out Platform candidate are recorded separately.

| Run / job | Exact Platform checkout | Result |
| --- | --- | --- |
| 41012249519 / 123920527376 | `9f43cefe11bd246ce0de1cd0740261b557c0c7a0` | Graphs/restore/14 controls passed; Release build failed because the unmapped fixture selected Debug. Zero warnings/one error; selected suites not run. |
| 41013494873 / 123924438995 | `0e524c17333ba181ff7a6a2384b680da9760489e` | Release build passed, zero warnings/errors; 79/80 class cases passed, one setup barrier timed out. The remaining two-method group did not run. |
| 41014652010 / 123928122261 | `6a186754102b33c3cfea823efc55e49264efbb84` | Release build passed, zero warnings/errors; 80/80 class cases + 2/2 owner metadata cases, no errors/failures/skips/not-run; 14/14 controls; 27 restored graphs retained. |

The successful run uses workflow head `1e4e447c84109f2d8ba6e7e2357953e5cdbef9a4`, clean selected source, signed archive SHA-256 `6c074186a9bbfb66c558fe5d1090e1e35fdd58577c0396266074bd1cea8919fd` and exact executable source/version. Output Tooling and host bytes match the authenticated archive. Its explicit `candidateAcceptance: false` remains unchanged. [run-41014652010-summary.json](run-41014652010-summary.json) binds the actual receipts and counts.

All three uploaded archives were downloaded through the GitHub connector and matched their official SHA-256 digests; raw archives, extracted output, signatures, inputs, SDK/runtime/runner metadata, commands, XML and asset graphs remain here. Successful artifact 12959311920 has SHA-256 `31e316d0a13050da2377bee2cf72e322fc7ebc836a171a7b20d36c913baf67cf`. Temporary signed download URLs are not retained. Prior failed runs are preserved rather than relabelled.

Local checks also passed: actual SDK assignment changes from removing `Configuration;Platform` to assigning Release/AnyCPU; reviewed Debug build has zero warnings/errors; all 80 class cases pass with two processors; whitespace and pinned commitlint pass. The original isolated two-processor case passed, so the hosted scheduling interleaving is not claimed as locally reproduced. Failed diagnostic approaches are retained and explained in the triage. Local builds remain source diagnostics, not package/runtime acceptance.

## Identity mismatches and concrete remaining proposal

At **2026-10-30T06:37:55.783235Z**, remote main was `e3822304436e69f4caa58df7def79a1c8899cece`, tool 4.38.3, Builds pin **`157b4dacdd90148de76e576396f61dd5859556f3`**. Its official source catalog matches the earlier observed a13d catalog, SHA-256 `fa3df877f1304db565456dd6c79da7d13ad2cff600fea94331ea093523568a10`; published 4.38.3's catalog is `501d72650e3a42afc8fde0345a69208669e7fabdb98b6300f20afca07585d405`. Main selects Memories 3.0.10, newer Azure/HotChocolate and explicit FrontComposer.Contracts.UI 4.7.1. It cannot be identified as released source d193. Recorded official latest-release/feed observations still end at 4.38.3; no matching published identity was verified for that newer source. Main moved independently throughout this operation; recheck exact identities before acting.

The actual restored graph for `TenantRegistry.Host` also differs: its own central package file pins Gateway **3.124.0**, producing seven EventStore 3.124.0 libraries and Commons.UniqueIds 2.30.1. The root catalog's 3.128.4/2.30.3 selections do not override this child file. Tenants.Contracts/Server 5.11.1 are verified in other owner graphs. The preflight checks foreign source fallback and its declared suites; it does not assert uniform package versions or full runtime acceptance.

The [one-line Gateway alignment proposal](tenant-registry-catalog-alignment.UNAPPLIED.patch) changes only that child pin to **3.128.4**. `git apply --check` passed; it remains **unapplied**. Exact authorization needed: apply this dependency change and qualify the newly restored closure and owner-host behavior, or explicitly govern the older-host exception. Keeping main's newer catalog also requires preparation and separately authorized publication of a coherent Builds/tool tuple before Platform adoption; silently rolling back its newer dependencies is not authorized. Then qualify the real integration revision/head/base and generated/direct runtime paths. Platform publication and consumer package/symbol qualification remain separate authorized gates. The detailed plan retains the 46 consumer inventory discrepancies and durable-graph requirements.

## Preservation

Final verification completed **115,458 original file checks**, found **zero differences**, and preserved all thirteen original repository HEAD/index identities. The four analyzer corrections and all previous receipts remain intact. The original Platform checkout stays at 9f43cefe; actual sibling Builds stays at `e909ffd9fe9a6097e36d932010b983d029396968`; neither was substituted for the tested candidate or published Builds source. No nested dependency in the user's umbrella was initialized. CI initializes only the standalone owner's declared direct dependencies, without recursive update. Story/spec/sprint bytes retain in-progress status.

[COMMANDS.md](COMMANDS.md) indexes exact argv/cwd/environment/timing/results. Connector observations are retained separately. [preservation-result.json](preservation-result.json) and `SHA256SUMS.json` bind the preservation and final packet.
