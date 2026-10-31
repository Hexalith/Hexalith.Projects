# Builds producer publication approval package

**Publication is blocked in this preparation.** The selected existing producer is `2186b9a4bc507a116a457c590c80945c2e232cbd`; the real release analyzer proposes **4.39.0** and exact-source push CI passes. No release-eligible 4.39.0 qualification inventory or official selected-version artifacts have been verified. Canonical Story 6.4 remains **in-progress**, `candidateAcceptance:false`.

The concrete [approval manifest](approval-package.json), [required release commands](publication-commands.md), [implementation results](implementation.md), [source/release input manifest](source-inputs.json) and [actual analyzer result](analyzer/analysis-result.json) form the approval package. This package does not authorize publication.

## Exact selection and version

| Input | Bound value |
| --- | --- |
| Official source | `2186b9a4bc507a116a457c590c80945c2e232cbd` |
| Complete source tree | `18d735e56eeb17332e2d2b57a6b116097f42fdec` |
| Catalog Git blob | `37148298dfd3a4ae35d89dd5f4677e06e89149ad` |
| Catalog SHA-256, exact Git bytes | `986b463e12daaf1818c261c8d509f38b9803bc2f9f328c48d591d3f0caec8e6a` |
| Real analyzer proposal | 4.39.0, minor; 95 commits |
| Previous official release | `v4.38.3`, `d1931b1e803144048d927fff16cb8119afde541e` |
| Analyzer toolchain | Node 24.21.0, semantic-release 25.0.9, commit-analyzer 13.0.1 |
| Hosted push CI | [41171553609](https://github.com/Hexalith/Hexalith.Builds/actions/runs/41171553609), attempt 1, completed/success |

This selection explicitly supersedes historical candidate `6cc96b4ff23c40c77651e1e4f85ed3170c25c7a4`, whose CI 41102168937 remains cancelled. The selected SHA is fifteen commits later and was verified as exact live main. The real release guard requires that live tip; the moving branch is never substituted beneath the captured identity. A later tip requires reconciliation and new qualification.

The catalog preserves all prior selections except three explicit upstream increases: FrontComposer **4.7.1→4.7.2**, AngleSharp **1.8.6→1.9.0**, JsonSchema.Net **9.5.0→9.6.0**. Commons 2.30.7, EventStore 3.128.4, Conversations 1.3.1, Tenants 5.11.1 and Memories 3.0.10 remain selected. [Reconciliation](catalog-and-pin-reconciliation.json) also records that formerly unavailable Platform pin `cc0db51b800be1d0d5badc3780395cfb5953d0f5` is now a locally available official ancestor after targeted object fetching. Its historical fetch128/API422 evidence remains unchanged.

The analyzer executes the pinned real history/commit/version modules and actual configured default analyzer. All 182 observed official stable tags were independently verified against local objects and ancestry. No full semantic-release lifecycle, push verification, release hook, tag or publication was executed for version analysis.

## Actual qualification outcomes

| Gate | Actual result and limit |
| --- | --- |
| Exact live source/push CI guard | Initial exit 1 while CI was incomplete; later exit 0. Both [initial](source-guard-initial.json) and [passing](source-guard-green-ci.json) receipts remain. |
| Hosted source suites | 2,821 passed, zero failed, **17 skipped**. [Per-suite summary and raw log](observations/hosted-source-test-summary.json). Skips confer no passing or live coverage. |
| Hosted package controls | Pass at diagnostic **0.0.0-ci.573**, explicitly `-SkipSourceValidation`; its ephemeral inventory is not release eligible and was not uploaded. [Actual log](observations/hosted-contracts-job.log). Publisher messages within that job come from test doubles, not actual CI publication. |
| Local selected-source restore/build | Release solution restore/build pass, zero warnings/errors. Existing safe source tests pass 191 Evidence, 46 catalog and one integration case; twelve live cases are skipped. These overlap hosted source evidence and are not added to its total. |
| npm provenance and vulnerability observations | 495 registry signatures and 130 attestations verify. Separate `npm audit --json` exits **1**, reporting 37 moderate and one high vulnerable dependency entry. The locked high entry is bundled `npm/node_modules/undici` **6.29.0**, with multiple advisory occurrences. [Actual audit](analyzer/npm-vulnerability-audit.stdout.log). Signature success is not a vulnerability pass; remediation/applicability assessment remains open. |
| Local official 4.39.0 builder | Four real unsigned/unpublished package/symbol archives produced; exit **1** at the loader fixture under inherited Projects CA1062 policy. No inventory emitted. |
| Loader fallback and correction | Original exact loader/fixture pass in isolated `/tmp`; two local null guards separately pass the stricter context against the unchanged original archives. [Three-lane comparison](qualification/loader-context-comparison.json), [prepared patch](qualification/corrections/loader-parameter-guards.patch). No universal default-path defect or clean-source qualification is inferred from this correction. |
| Full source/package qualifier | **Not run** pending the requested clarification about disposable fixture Git operations. No skip flag, substitute builder, external qualification fixture or forged eligibility clears this gate. |
| Protected publication and official artifacts | Not performed or approved by this session. Official selected-version inventory, signed package/symbol identities and embedded-versus-evaluated catalog qualification remain unverified. |

Exact local argv/cwd/public environment/UTC/exits and raw outputs are retained under `qualification/`; the implementation receipt identifies each control that ran or remained unexecuted. The complete original builder failure remains separate from focused successes. Local archive hashes cannot fill official publication fields.

The npm chain is semantic-release 25.0.9 → installed `@semantic-release/npm` 13.2.0 → npm 11.21.1 → bundled undici 6.29.0. The selected configuration does not invoke the npm publication plugin; no exploitability assessment is claimed. Upstream identifies **6.29.1** as the fixed 6.x version for the [TLS-option validation advisory](https://github.com/nodejs/undici/security/advisories/GHSA-2m5v-pch6-74hx) and [content-encoding denial of service advisory](https://github.com/nodejs/undici/security/advisories/GHSA-xjj6-f33q-929c). No lockfile, dependency, vulnerability exception or existing release gate was changed. The repository's Trivy image policy does not itself govern this npm audit, so no new policy exemption or automatic policy verdict is invented.

## Independent release activity

After CI succeeded, GitHub actor `jpiquot` independently initiated [release run 41172847732](https://github.com/Hexalith/Hexalith.Builds/actions/runs/41172847732) for exactly the selected SHA at 2026-10-31T05:30:19Z. It was observed **in progress**; this session did not dispatch or approve it. Earlier release/tag/NuGet observations still end at 4.38.3 and 4.39.0 destinations returned 404 at their recorded times. An active run is not published qualification. **Do not dispatch a duplicate**; inspect this existing run and refresh source/destinations before any later decision.

The publication command handoff requires complete exact-source qualification, unused destinations, no active duplicate, explicit publication approval and exact run-head verification before protected environment approval. Production requires reviewer `jpiquot`, forbids admin bypass and admits only main. Branch dispatch has no expected-SHA input, so a raced different-source run must remain unapproved. Trusted Publishing and semantic-release stay inside the protected workflow.

## Preservation and scope deviation

The original Builds checkout remains `1160a3a7ddc0f4c6927456b5735a2963261016a0`; original Platform remains `a185bfaa5541f8de675fb82e4f553e1e49931201` with its existing dirty preparation. No consumer pin or Platform producer binding was changed. The earlier binding still names the earlier producer/catalog and cannot accept this newly selected catalog without separately authorized reconciliation after official publication. The original story prefix/frozen scope and historical evidence remain preserved; original repository HEAD/index/ref and byte audits are retained in the preservation receipts and compressed baseline.

**Deviation:** `test-g4-tool-package-contract-gate.ps1` indirectly invoked the artifact-validator suite, which created four disposable fixture commits before its transitive Git writes were caught. The fixture root was removed by its existing cleanup. Exact ephemeral commit IDs were not captured before deletion. [Deviation receipt](qualification/scope-deviation.json) retains the command, actual 63-scenario result, cleanup and response. Further unverified suites were stopped. [Independent post-deviation audit](preservation-post-fixture-deviation.json) verifies all thirteen original repositories' HEAD/index/refs and all other original files unchanged; the selected checkout was also clean before its separately prepared two-line correction. No no-fixture-commits claim is made.

Story 6.4 stays in progress. Full producer eligibility, verified official publication, any separately approved recording of the optional fixture correction, coherent Platform binding/qualification/publication and consumer adoption remain distinct actions. Historical owner/runtime/policy and consumer package blockers are not rerun or cleared here.


## Final review follow-up

Independent review identified fail-closed replay and command-documentation gaps. Original executed scripts, results and all 116 implementation evidence files remain immutable; separate reviewed replay/checker programs carry corrections. Every release-command Bash block now stops on a failed prerequisite, and the unresolved npm observation is an explicit prerequisite before requesting publication approval. The current cleanup audit finds no matching owned capture/control process; the historical interrupted child's final exit remains unknown and is not retroactively claimed.

Redundant expanded package and generated bin/obj trees are stored losslessly in [expanded-loader-payloads.tar.gz](expanded-loader-payloads.tar.gz), with every original file hash and reconstruction command in [the storage manifest](expanded-loader-payload-storage.json). All original package/symbol archives and unique proof files remain directly accessible. Large source/API observations use [documented gzip storage](compressed-input-storage.json); their original source blob/digests bind decompressed bytes.


The reviewed helper controls pass **20** positive/negative cases, including changed config/lock/modules, manifest/input/result tampering, overwrite refusal and child failure propagation. [Results](qualification/replay-review-controls/focused-controls.json), [correction fingerprints](qualification/replay-review-controls/review-corrections.json), [reviewed integrity checker](qualification/verify-receipts-reviewed.py). The hardened analysis replay reproduces 4.39.0 against the same exact clean SHA; it is a replay of the immutable capsule, not a current tag/feed refresh. Three process-double controls verify the exact documented dispatch block refuses changed source and active release while calling only a simulated dispatch on the eligible lane. All original 116 implementation files remain byte-identical.


Delivery audit: planning guard **37** passes; both final production-authority validators pass. Tracked root/correction whitespace checks are clean; the new scoped-spec no-index check reports only the normal new-file difference with no whitespace diagnostics. Parent independently verifies all 116 original implementation files, 22 source inputs, four archive identities, twenty helper-control stream digests and the exact three-case dispatch block. [Delivery preservation](preservation-delivery.json), [parent integrity](parent-validation/reviewed-integrity-check.json), [review correction audit](parent-validation/review-correction-audit.json). Final read-only observations still find the same official main, latest release/feed 4.38.3, no v4.39.0 tag, and the independent release in progress. Full qualifier execution, npm applicability/remediation and official publication binding remain open; no permission has been inferred from elapsed time.


Synthetic replay-control input copies, including deliberate tampering and duplicate hardlinked archives, are [losslessly stored](qualification/replay-review-controls/synthetic-inputs.tar.gz) with [file hashes/reconstruction instructions](qualification/replay-review-controls/synthetic-input-storage.json). Reconstruct them before replaying the captured test program; they are never producer acceptance evidence. Actual control outputs and the immutable original capsule remain readable at their original paths.
