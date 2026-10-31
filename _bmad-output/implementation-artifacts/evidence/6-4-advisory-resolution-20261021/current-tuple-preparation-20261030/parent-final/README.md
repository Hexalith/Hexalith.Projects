# Reviewable current tuple candidate

Preparation is complete; canonical Story 6.4 remains **in-progress** and `candidateAcceptance:false`.

The complete final [Platform patch](platform-final.patch) includes all six modified and four new owner files. [Root story/tracking patch](projects-final.patch), [scoped preparation spec](../../../../spec-6-4-current-tuple-exact-revision-preparation.md), [initial reconciliation](../README.md), [implementation results](../implementation/README.md) and [review corrections](../implementation/review-follow-up-20261030/README.md) retain the supporting inputs and exact commands.

No compatible published tuple preserving the newer catalog was verified. Concrete existing official producer source is `6cc96b4ff23c40c77651e1e4f85ed3170c25c7a4`, catalog blob `5d7eeadf561eed182fcb03c2b1fd97565dd52852`, SHA-256 `abe630443a9f095a1bce171c4b10314a714a8bba9f3bde8980a5c40ef46ef172`. Local Builds `1160a3a7ddc0f4c6927456b5735a2963261016a0` has that catalog, but is not relabeled as the producer candidate. Local Platform `a185bfaa5541f8de675fb82e4f553e1e49931201` still records unavailable pin `cc0db51b800be1d0d5badc3780395cfb5953d0f5`. Remote advancement and cancelled producer CI are separate recorded observations.

## Final validation

- Fifteen tuple-binding tests and fifteen actual MSBuild selection controls pass after review corrections.
- Actual TenantRegistry offline restore and Debug build pass with zero warnings/errors. Retained actual assets contain seven EventStore packages at 3.128.4 and Commons.UniqueIds 2.30.7, no source projects and no Hexalith catalog differences. All eight copied package assemblies match package bytes.
- Ten strengthened real HTTP controls pass: exact subscription and eight/nine named readiness/health checks; only expected placement/writer checks are unhealthy. Readiness remains 503. Authentication/authorization denials remain 401/403. No successful authorized query or ready service graph is claimed. All owned processes stop successfully.
- Parent planning guard passes 37 cases; both authority validators and whitespace pass. Parent independently verifies 116 original and 32 follow-up evidence hashes, eight actual package identities, fresh HTTP outcomes/cleanup, original story prefix and all thirteen repository HEAD/index/ref identities.
- Final actual preflight refuses missing publication, including inventory identities. This is an expected retained failure, not hosted qualification. Original and post-documentation preflight receipts stay bound to their own source digests.

## Remaining blockers and exact commands

Five existing Custody errors (three absent EventStore APIs and two CA1062 errors) block both full and narrow actual-host seed test builds; no current seed cases ran. Package preparation still refuses HXW011 twice, HXW009 and HXW007. Producer exact-source CI was cancelled, official publication identities are unavailable, and full runtime/policy/merge-result/consumer qualification remains open.

- `dotnet build tests/Hexalith.Platform.Composition.Tests/Hexalith.Platform.Composition.Tests.csproj --no-restore -c Debug -m:1 -p:UseHexalithProjectReferences=false` — exit 1. [Raw command](../implementation/seed-tests-build.command.json).
- `dotnet build /tmp/story64-seed-focus-dm2pjp10/GatewaySeedFocus.csproj --no-restore -c Debug -m:1 -p:UseHexalithProjectReferences=false` — exit 1. [Raw command](../implementation/seed-focus-build.command.json).
- `dotnet tool run hexalith-module -- prepare-build --manifest /home/administrator/projects/hexalith/projects/module/hexalith.module-manifest.v2.json --mode package --workspace /tmp/story64-current-package-qk2n6pnz/package-output --output json` — exit 2. [Raw command](../implementation/public-package-guard.command.json).

## Recording and publication order requiring separate authority

1. Reconcile the selected official producer full SHA with the intended live release head and unchanged/newer selected catalog; require successful exact-source push CI. Rerunning or dispatching CI is a remote write requiring approval. The cancelled run is no success, and a new main must not silently replace the captured candidate.
2. Qualify producer source and actual packages through its existing protected release controls; compute the version using its real semantic-release analyzer. Obtain separate protected producer publication approval. No version, tag or archive hash has been invented.
3. Verify official release/tag/source, all actual signed package/symbol artifacts, inventory digest, embedded runtime catalog proof, tool version/source and archive/Tooling/catalog hashes. Fill the strictly required publication fields and align Platform's exact tool/range/documentation. No source is repackaged as 4.38.3.
4. Obtain approval to stage/commit/push the actual Platform configuration and real gitlink correction. The producer commit already exists; no duplicate Builds commit is needed. The proposed gitlink patch is unapplied. Do not adopt the umbrella pointer at this stage.
5. With separate authority for hosted execution, run exact recorded Platform preflight, all remaining owner suites, authenticated policy and real merge-result decisions, plus ready isolated generated/direct-host runtime gates. Local Debug and historical hosted results do not satisfy these gates.
6. After acceptance, obtain separate Platform publication approval and verify its official identity.
7. Obtain separate consumer adoption approval before updating umbrella/Projects tuple pointers and exact tool selection. Run actual public package preparation and complete expected-versus-actual package/symbol qualification, resolving the retained consumer inventory differences. Keep Story 6.4 in progress until its acceptance gates pass.

No Git index/ref/branch/tag/checkout mutation, nested initialization, remote write, CI dispatch, publication or deployment occurred. Existing dirty work and historical inputs/results remain preserved. A parent-only audit summary assembly initially mishandled workspace-relative repository keys; its failed assembly is retained and corrected without rerunning the successful commands.
