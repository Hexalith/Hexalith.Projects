# Current tuple preparation result — 2026-10-30

Story 6.4 remains in progress. This packet prepares exact source and owner
changes; `candidateAcceptance:false` remains mandatory. All commands retain
argv, owner cwd, UTC times, exit, stdout and stderr in their matching receipts.
Original investigation and historical receipts remain unchanged.

## Exact inputs and producer blocker

- Projects remains `5724c2400c102c466247890c05bbbdabb898e7a4`.
- Local Platform is `a185bfaa5541f8de675fb82e4f553e1e49931201`; its recorded
  Builds pin remains unavailable object `cc0db51b800be1d0d5badc3780395cfb5953d0f5`.
- Local Builds remains `1160a3a7ddc0f4c6927456b5735a2963261016a0`. Its catalog
  equals official candidate `6cc96b4ff23c40c77651e1e4f85ed3170c25c7a4`, including
  Commons 2.30.7 and the newer security/provenance selections. Source catalog blob
  `5d7eeadf561eed182fcb03c2b1fd97565dd52852` has SHA-256
  `abe630443a9f095a1bce171c4b10314a714a8bba9f3bde8980a5c40ef46ef172`.
  Checkout CRLF bytes remain a different identity, as recorded in the original
  packet. Local Debug evidence does not relabel 1160a3a as 6cc96b4.
- [producer-release-inputs.json](producer-release-inputs.json) binds the candidate
  tree and exact catalog, workflow, package/lockfile and release gate script
  blobs/bytes. `producer-source/` retains those actual official source bytes.
  Version, release tag, signed archive, Tooling and inventory publication values
  remain unavailable. No proposed version was computed or invented.
- [Exact-source CI 41102168937](https://github.com/Hexalith/Hexalith.Builds/actions/runs/41102168937)
  is now completed/cancelled, not successful. Independent official main tips at
  observation were Builds `76aafc85122fb7ae4b2668ed4895e4bc213c404a` and Platform
  `7efec1ad0b45b413c9ed85f5f565ebbeaae26c65`; neither replaced this source or the
  local checkout. Latest official release/feed still ends at 4.38.3, source
  `d1931b1e803144048d927fff16cb8119afde541e`. See the four `*-refresh` receipts.

## Reviewable owner changes

TenantRegistry's child CPM file imports the owner root, inheriting the selected
catalog and transitive pinning. Its retained Gateway 3.128.4 entry uses `Update`
after the import, avoiding duplicate central entries. Existing build/source
mapping policy remains intact.

`eng/builds-tuple.json` centralizes exact source/catalog and eventual publication
bindings. All publication fields deliberately remain null. `eng/builds_tuple.py`,
the artifact preparation, CI runner and workflow use that binding. Incomplete or
mismatched version/source/tag/catalog/archive/Tooling values refuse execution.
The existing signature, clean recorded owner, exact pin/catalog, extracted-byte,
foreign-reference, Release/package, output-identity and required suite-count
guards remain active; the runner still emits a terminal failed receipt and
never grants candidate acceptance. A positive synthetic archive control catches
version/source validation mistakes without calling any signature verifier or
claiming synthetic bytes are published artifacts.

Bootstrap tool 4.38.3 and its range remain unchanged and explicitly unqualified
for the newer catalog. README and `docs/current-tuple-preparation.md` record this
and the complete qualification/recording/publication/adoption sequence.
[builds-gitlink.UNAPPLIED.patch](builds-gitlink.UNAPPLIED.patch) proposes the exact
candidate source correction only. `git apply --check --index` passes; the patch
was not applied and the index/gitlink remain unchanged.

## Fresh focused results

| Control | Result and receipt |
| --- | --- |
| Strict tuple binding | 10 unittest cases pass: [final output](tuple-binding-controls-final.stderr) |
| MSBuild owner/source selection | 15 tests pass, including actual child CPM inheritance: [output](host-builds-controls.stderr) |
| Incomplete actual artifact preparation | Exit 1 before artifact directory creation/download: [command](actual-incomplete-artifacts.command.json) |
| Incomplete actual preflight | Exit 1; failed terminal receipt and candidateAcceptance false; final receipt uses the final source bytes |
| AppHost pins vs exact candidate catalog | Exit 0: [command](platform-pin-alignment.command.json) |
| Actual TenantRegistry offline restore / Debug build | Both exit 0; zero build warnings/errors: [restore](tenant-restore.command.json), [build](tenant-build.command.json) |
| Actual owner seed project | Restore passes; Debug build fails with five current Custody errors: [build](seed-tests-build.command.json) |
| Narrow existing-seed fallback | Minimal temporary harness links unchanged GatewaySeedSubscriptionTests and actual seed hosts; build reaches the same Custody dependency blocker: [build](seed-focus-build.command.json) |
| Command-owned TenantRegistry HTTP | 10 controls pass, with precise readiness/auth limits and cleanup: [receipt](local-host.json) |
| Actual public package preparation | Exit 2: two HXW011 findings, HXW009 and HXW007: [command](public-package-guard.command.json) |

[tenant-catalog-evaluation.stdout](tenant-catalog-evaluation.stdout) records real
MSBuild values: root transitive pinning true, Commons 2.30.7, EventStore 3.128.4.
[tenant-project.assets.json](tenant-project.assets.json) is the actual restored
assets file. [tenant-package-comparison.json](tenant-package-comparison.json)
compares every actual Hexalith package and every evaluated Hexalith catalog
entry: seven EventStore packages at 3.128.4 and Commons.UniqueIds 2.30.7, no source
project libraries, zero mismatches. Unused catalog entries are marked absent,
not claimed restored. All eight copied runtime assemblies match package bytes
in [the copy identity receipt](tenant-runtime-copy-identities.json); this does
not establish a complete loaded-runtime acceptance proof.

The current seed build errors are missing `ReadAdmittedOriginalAsync`,
`ReadCurrentOriginalAsync` and `DeletionBatchCapabilityIdentity.IssueOperationId`,
plus two CA1062 errors in DeletionCapabilitySigningActor. No Custody code,
package version or analyzer was changed to conceal them. All four existing seed
test cases remain unexecuted for current inputs. The source Builds fallback was
only a local Debug diagnostic, not an exact producer artifact or release proof.
The temporary harness project and its location are retained; no fixture commits
were created.

Fresh host HTTP results: `/alive` 200, `/ready` and `/health` 503,
`/dapr/subscribe` 200 with one subscription, four authentication denials 401 and
two authenticated authorization denials 403. Dapr outbound health is 204. Actor
placement and writer protocol readiness remain unavailable; no successful
authorized owner query, ready full service graph or complete generated/direct
runtime acceptance is claimed. Host, Dapr and Redis all stopped with exit 0.
Secrets and full private logs stay in the private temporary runtime directory,
outside repository evidence. No runtime prerequisites were installed.

The actual public package guard names Platform's absent local SemVer tag and
working changes (HXW011 twice), its unchanged unavailable pin versus umbrella
Builds 1160a3a (HXW009), and the missing pinned catalog object (HXW007). This is
not a passing package-preparation or consumer-adoption receipt.

## Remaining sequence and preservation

Follow the owner preparation document: reconcile exact official producer source
and successful push CI/live-tip identity; run protected producer contracts and
actual package controls; compute the version with its real release analyzer;
obtain separate producer publication approval; verify official tag/source,
three signed archives and inventory/catalog/tool bytes; fill strict publication
bindings and align real owner tool/range/docs; obtain owner recording approval
before applying/recording its gitlink; then run fresh exact-revision hosted
preflight and independent policy/merge-result/generated/direct runtime gates.
Platform publication and consumer adoption each require separate authority and
complete package/symbol qualification. No current source is repackaged as 4.38.3.

`baseline.json` records all thirteen initial HEAD/index/ref identities and hashes
of original tracked and untracked files, including existing evidence. The final
preservation receipt distinguishes exactly the six modified owner files, four
new owner files, the append-only original story and its one changed sprint
comment. All other original bytes and Git identities must remain unchanged.
No stage, commit (including fixture commits), branch, tag, push, dependency
checkout/update, nested initialization, CI dispatch, publication or deployment
was performed. No unrelated source/store suite was repeated.

Final checks: Story 6.4 and index authority validators and root/Platform
whitespace all pass. [The completed preservation audit](preservation-final.json)
passes for all thirteen HEAD/index/ref identities, all other original tracked
and untracked bytes, the complete original story prefix and the single sprint
comment change. Final code is retained in [the owner patch](platform-working.patch)
and [the root documentation patch](projects-working.patch), including new owner
files. The [final failed preflight receipt](actual-preflight-final/tuple-ci-result.json)
binds the final source; its failure is the unavailable official publication.
