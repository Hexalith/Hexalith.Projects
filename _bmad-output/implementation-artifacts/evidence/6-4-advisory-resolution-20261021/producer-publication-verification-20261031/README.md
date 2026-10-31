# Builds producer qualification and publication binding

**Builds 4.39.0 was published successfully from the selected source.** Independent [release run 41172847732](https://github.com/Hexalith/Hexalith.Builds/actions/runs/41172847732), attempt 1, completed successfully at 2026-10-31T06:35:02Z. The [official v4.39.0 tag](https://github.com/Hexalith/Hexalith.Builds/releases/tag/v4.39.0) resolves to `2186b9a4bc507a116a457c590c80945c2e232cbd`, tree `18d735e56eeb17332e2d2b57a6b116097f42fdec`. This session performed read-only verification and local nonpublishing checks; it dispatched or reran no workflow and approved or published nothing.

Story 6.4 remains **in-progress**, `candidateAcceptance:false`. The [verified producer binding](verified-producer-binding.json) is an evidence-only file. Platform's existing binding, source selection, tool pin and consumer pins remain unchanged. Security acceptance remains blocked as described below.

This package continues the [historical preparation](../producer-publication-preparation-20261031/README.md), recorded by Projects `8fa68d0c8b8a3d53b4d70e5456dc11084811a9f8`. Historical active-run, absent-feed, local loader failure and fixture-deviation observations retain their original meanings and bytes. The analyzer's recorded 4.39.0 proposal is now confirmed by actual publication; no new version analysis or future version is invented.

## Verified official evidence

The [executable verification](verify-publication-reviewed.py) and [passing acceptance report](publication-verification-final-proof.json) check source, CI, tag/release, inventory, signed packages, symbols, repository identities and extracted Tooling/catalog bytes. [Command receipt](parent-final-v2-verifier.command.json) and [seventeen positive/negative controls](parent-final-distinct-controls.stdout.log) retain actual results. The checker hashes the exact pre-signing ZIP envelope in memory and compares it with the inventory; it never writes or publishes an unsigned replacement.

| Identity | Verified value |
| --- | --- |
| Selected source / clean qualified tree | `2186b9a4bc507a116a457c590c80945c2e232cbd` / `18d735e56eeb17332e2d2b57a6b116097f42fdec` |
| Exact-source push CI | [41171553609](https://github.com/Hexalith/Hexalith.Builds/actions/runs/41171553609), attempt 1, completed/success |
| Official release | [v4.39.0](https://github.com/Hexalith/Hexalith.Builds/releases/tag/v4.39.0), release ID 429766617 |
| Official inventory asset SHA-256 | `f07332c8c005bfa91896034933fd556ae0dfef47c3f759f712f797251c8bf555` |
| Source catalog blob / exact Git-byte SHA-256 | `37148298dfd3a4ae35d89dd5f4677e06e89149ad` / `986b463e12daaf1818c261c8d509f38b9803bc2f9f328c48d591d3f0caec8e6a` |
| Embedded catalog, equal to fresh evaluated catalog | `b14406f169e1800f30a021aa79d83fb0230514532c0259f59617f72371a092d0` |
| Extracted Tooling DLL, all three copies | `2c006033f77e21db812c9b245a4eb3bc52107cc602f44e997ba2d17c46179dda` |

The [downloaded inventory](downloads/g4-tool-package-inventory.json) matches the digest and length returned by GitHub's release asset API. It records official package build, executed source validation and required controls all passed, a clean selected source and `releaseEligible:true`, with no ineligibility reasons. Its 55 evidence rows cover all 55 names required by the selected publisher. The [release log](observations/release-job.log) proves the actual 4.39.0 pre-tag qualifier completed before the tag and package pushes; the earlier `0.0.0-ci.460` run is a separate diagnostic lane.

The existing qualifier enumerates fixtures without `-Force`: its 292 listed files all match selected Git checkout bytes, including 24 tracked line-ending conversions. One of 293 tracked files is omitted: the hidden manual `package-consumer/.config/dotnet-tools.json` template. The qualifier generates its own version-bound manifest and does not consume that template. This is a stated coverage limit, not a policy exception or a complete 293-file claim. An unapplied [future-source correction](local/corrections/include-hidden-fixtures.patch) and focused regression evidence are retained. The original 55 control-output files were not attached to the release; their hashes remain official inventory assertions, with hosted execution and independent local execution recorded separately.

## Packages and symbols

| Artifact | Official downloaded SHA-256 |
| --- | --- |
| Module.Cli 4.39.0 signed nupkg | `111d621a2b4e659ab21b32c340717def3bf7c9e5757b728f8679785396aca192` |
| Module.Cli 4.39.0 snupkg | `79d6ffe29718fa7adccb6a49311cbc39ebab558e718e4a7f95c761915d2416f2` |
| Evidence.Cli 4.39.0 signed nupkg | `8b7d1de983b4f5abf4b05e68e4623766446930c6050976263efa0ea5046c1c93` |
| Evidence.Cli 4.39.0 snupkg | `a656d52f286651527a391c24ceef3f4b5cac29268a2c3b80371f5f14893faa70` |

Both primary packages pass `dotnet nuget verify --all`: repository signer NuGet.org, owner **Hexalith**, certificate SHA-256 `1f4b311d9acc115c8dc8018b5a49e00fce6da8e2855f9f014ca6f34570bc482d`. Their signed hashes match the successful official publisher's remote observations. Removing only the signing envelope in memory reproduces the inventory's exact unsigned archive digests and sizes. Both symbol archives match their official inventory digests/sizes exactly, declare `SymbolsPackage`, and contain PDB bytes identical to the corresponding primary packages. Symbols are not claimed repository-signed.

All four nuspecs identify the exact package/version and official `https://github.com/Hexalith/Hexalith.Builds` repository at the selected commit. Tooling's assembly identity is `4.39.0.0` and informational version is `4.39.0+2186b9a4bc507a116a457c590c80945c2e232cbd`. CLI root copies in both tools and the Module AppHost copy have identical hashes and embedded catalog bytes. Fresh catalog evaluation preserves EventStore 3.128.4, FrontComposer 4.7.2, Dapr SDK 1.19.0, Aspire 13.6.4 and Toolkit 13.6.2-beta.959. Source catalog AngleSharp 1.9.0, JsonSchema.Net 9.6.0 and all other newer selections are preserved.

The existing loader regression also passes against the actual signed Module package: validation and simulated runtime child both exit 0, load the same contract, create the command-owned Aspire store and stop before any resources start. [Actual receipt](official-loader/loader-result.json), [command](observations/official-loader.command.json). The earlier inherited-output CA1062 failure remains historical evidence; its optional null-guard correction is untouched and never attributed to published source.

## Qualification, security and remaining gates

The successful hosted real-version qualifier ran **2,821 passed, zero failed, 17 skipped** source cases: Evidence 191; Module 2,629 with five skips; integration one with twelve live skips. [Per-lane summaries](observations/hosted-qualification-summaries.json) distinguish this run from diagnostic and earlier CI executions. Repeated runs are overlapping coverage; skips confer no passing/live coverage. The [full local qualification receipt](local/README.md) records three blocked full attempts. The last uses an explicit two-processor runtime limit and fails one unchanged five-second readiness case: Evidence 191 passed; Module 2,628 passed, one failed, five skipped. Integration and package stages were not reached, and no local release-eligible inventory was emitted. The earlier cache/per-user-directory failures and the second run's port/file-lock failures remain separate; focused passes do not replace full qualification.

The [npm applicability assessment](security/README.md) covers all eight unique advisories and all 38 affected dependency paths. The exact-lock audit still exits 1 with 37 moderate and one high entry; signatures verify separately. Actual GitHub fetching uses fixed external undici 7.30.1, and hosted Node 24.22.1 embeds fixed 7.30.1, while vulnerable bundled npm leaves remain on disk. Glob/minimatch input reachability is not fully cleared. Security acceptance remains false. A proposed fail-closed audit patch is prepared and tested locally, and an ineffective bundled-overrides experiment is explicitly rejected. No repository policy exception, remediated-lock claim, successor SHA/version or repackaging under 4.39.0 is invented.

Initial main observation was two commits ahead at `ff1c2952fa6aec113975bbe78b4d2487d6ec0b3d`; the final official refresh observes `a22b55f0a4f6d2aaa3a5ccc505896064ac595364`. The published binding remains selected-source exact. [Final source/run/tag/feed observations](observations/final-main.json) retain their own capture times; CI and release still succeed at attempt 1 and both feeds still end at 4.39.0. A future corrected source needs its own recording, real analysis, exact-source CI and separately authorized publication. This package does not adopt the producer, accept a Platform tuple, clear owner/policy/runtime prerequisites or accept consumer package/symbol coverage.

[Machine-readable results and blockers](qualification-package.json) and [exact next commands](next-commands.md) reproduce retained verification, refresh official state and repeat existing nonpublishing qualification in disposable repositories. No publication command is required or authorized. Real repository HEAD/index/ref/config identities and original bytes are audited by [the preservation checker](audit-preservation-reviewed.py); original snapshots are losslessly compressed with [storage hashes](preservation-storage.json). The canonical checkpoint is append-only and the sprint remains in progress.


## Independent review follow-up

Three review lenses completed. All twelve original findings and one follow-up label finding are [triaged](review-triage.json), with targeted corrections and no deferred item. The current publication replay checks the delivered binding, re-evaluates catalog inputs directly from selected Git bytes in disposable scratch, and freshly verifies both exact signed archives, recording input digests and full outputs. Seventeen publication controls pass under Python optimization, including altered bindings, stale evaluation and corrupted signature bytes despite an old success receipt. Five preservation controls detect same-commit HEAD attachment changes and unauthorized new paths/status changes. The reviewed command recorder protects every prior output component and retains launch failures; original executed helpers remain capture-only history.

The [reviewed preservation result](preservation-final-reviewed.json) compares the original thirteen commit/index/named-ref/config snapshots and original tracked/nonignored files, checks exact task path/status allowances, and monitors symbolic HEAD during review. The initial snapshot did not record symbolic HEAD for every repository, so no retroactive attachment proof is claimed. Ignored build/cache files are outside that file audit. The [original detached-checkout supplement](local/original-checkout-historical-supplement.json) confirms the protected dirty file/patch, 22 canonical source/guidance inputs, ten historical checkout hashes and available historical HEAD/status/submodule evidence. Its historical raw index/ref/symbolic state was not captured; new read-only monitoring is labelled separately.

The security package now has tested, portable [scratch reconstruction](security/reconstruct-and-replay-reviewed.sh), with installation auditing enabled and explicit nonrecursive fetching. Both proposed guards still refuse the affected dependency tree. No security clearance, broad local pass, source correction under 4.39.0, consumer adoption or publication action follows from these helper fixes.

The [complete packet SHA-256 manifest](artifact-manifest.json) covers retained files except the manifest itself. [Final receipt audit](receipt-audit-final.json) verifies 41 stream-bound command receipts while preserving nonzero outcomes; four earlier observation receipts lack stream bindings and remain explicitly labelled. [Final document checks](document-checks-final.json) verify links and Bash syntax.
