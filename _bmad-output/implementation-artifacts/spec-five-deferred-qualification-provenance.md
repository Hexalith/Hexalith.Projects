---
title: 'Verify the provenance of five historical qualification findings'
type: 'chore'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Give reviewers verifiable answers to five outstanding questions about the qualification evidence. Identify the checkout that produced the September OQ8 observations. Bind the Builds P0 proposal and continuation to portable owner references. Trace the submodule changes around the October compatibility replay. Verify which commands the retired restore tool recorded and how its isolation wrapper handled remote Docker controllers.

</frozen-after-approval>

## Implementation Notes

Use committed artifact bytes and full owner revisions. Add the OQ8 provenance beside the original packet, link the P0 handoff to immutable Builds records, and retain the Git comparison and historical test results here. Record each finding against its original ledger entry. These are archival corrections with no change to release or qualification decisions.


## Verification

### September OQ8 capture

The live PostgreSQL qualifier and its 21 support selectors used Release test binaries built from EventStore `5b9494835fcba6152789653add8f84bee5666035`. The retained command log identifies both builds. The Projects Release build in the same packet used the public 3.109.0 packages. The new record beside the observations binds the original packet, command log and observations by SHA-256. Run `python3 tests/tools/test_g6_packet_references.py` from the Projects root to check the record against the Git blobs. [Test binary provenance](qualification-evidence/g-6-runtime-toolchain-20260929/eventstore/test-binary-provenance.json) · [Original command log](qualification-evidence/g-6-runtime-toolchain-20260929/commands.json)

### October Builds handoff

The handoff now links the proposal, approval and continuation at Builds `92fd7a37b632f713833705c91ff35a373af997cc`. The SHA-256 of the original proposal agrees with both `approval.json#/proposal/sha256` and `continuation-state.json#/approvedProposalSha256`. To check a record from a Builds clone, substitute the path from its link in `git show 92fd7a37b632f713833705c91ff35a373af997cc:<path> | sha256sum`. The approval applies to the October preparation target. [Handoff and hashes](6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md#approved-preparation-continuation--2026-10-20)

### October compatibility replay and submodule changes

The historical comparison selected Projects `cbcf54fa4d7a8c17bbfc3f9fb555ac0a85c179c0`, EventStore `2c58ffda41759e895ace4b9625c9bd931a217672` and Builds `688eec9a4333245cc0ff7772115c769094471863`. Projects first updated only EventStore to `aaaa627dcbe26a6dd22f7f2b199893123ba934a3`. The next update moved EventStore again and advanced seven other repositories. Run `git diff --raw --no-abbrev cbcf54fa4d7a8c17bbfc3f9fb555ac0a85c179c0 1e1e121c78abf1a7785b7b070b729920b53169ea -- references` to reproduce the eight pointer changes. [Comparison contract](6-1-p1r-compatibility-scenarios.md#bound-coordinates) · [First update](https://github.com/Hexalith/Hexalith.Projects/commit/0cba151ecdcdbc6453d80a5fb23b2d95e62d2eae) · [Second update](https://github.com/Hexalith/Hexalith.Projects/commit/1e1e121c78abf1a7785b7b070b729920b53169ea)

For each owner, the old revision was the merge base. The counts cover each owner's whole revision range. Only the fixed sources in the comparison contract were selected for the replay.

| Repository | Prior gitlink | Recorded gitlink | Commits ahead |
| --- | --- | --- | ---: |
| Hexalith.Builds | `688eec9a` | `90f3836d` | [7](https://github.com/Hexalith/Hexalith.Builds/compare/688eec9a4333245cc0ff7772115c769094471863...90f3836dd7482db35c2c187a50999b99215919b0) |
| Hexalith.Conversations | `8f2594db` | `90c1fc89` | [22](https://github.com/Hexalith/Hexalith.Conversations/compare/8f2594db2f6e29e6eb98e2fea90467d615674dd0...90c1fc8942a390bfd558b5d53767c0fd7bc74e61) |
| Hexalith.EventStore | `2c58ffda` | `ff7f07d1` | [38](https://github.com/Hexalith/Hexalith.EventStore/compare/2c58ffda41759e895ace4b9625c9bd931a217672...ff7f07d1ff12b94c7b53646581ca3843b2c09548) |
| Hexalith.Folders | `92da4b01` | `b4aeb455` | [5](https://github.com/Hexalith/Hexalith.Folders/compare/92da4b01352448cb06296776097e2da4383d9340...b4aeb4551e8c2743a8cc3d9cdea15af2b3a16f23) |
| Hexalith.FrontComposer | `bf40099f` | `74f7569e` | [31](https://github.com/Hexalith/Hexalith.FrontComposer/compare/bf40099f81fcaeac324b7b4377513ac7d49cead4...74f7569e1c260506b959c12a140d116cdeeb6839) |
| Hexalith.Memories | `42995692` | `ec3601a5` | [14](https://github.com/Hexalith/Hexalith.Memories/compare/42995692634af9ba35982ec0e1aedec4f5576e71...ec3601a532a6779c2c677c9b74e8bba642a8a651) |
| Hexalith.Parties | `06714c16` | `b3794a4d` | [12](https://github.com/Hexalith/Hexalith.Parties/compare/06714c166c090200ac87373b11d8243aa11b2126...b3794a4dcbe2fff3e9ea5c420a3c4695e2d1a8ca) |
| Hexalith.Tenants | `bfc10cb6` | `50fc6257` | [27](https://github.com/Hexalith/Hexalith.Tenants/compare/bfc10cb62dff6784c4b4bcabadfd537ef74fd77a...50fc625753626a0bc7868f16c46c7d94b1daaf0f) |

Three EventStore commits in that range touched the verifier or its evidence directory. They were `aaaa627dcbe26a6dd22f7f2b199893123ba934a3`, `2540001d92ec4c9b93de0a6ef61450bccaf51efd` and `e968467c7db5b5685894fa2b85cfa7ec14512a7c`. Across the final 25 commits, the verifier script retained Git blob `830079c42295e24ab7fece91eaa95c2aaccb949c` and the verification directory retained tree `6ea2e9c9f4b9a833e0622c282f952144048de122`. The owner comparisons establish the origin of those updates. The replay attests to its bound inputs. [Final EventStore range](https://github.com/Hexalith/Hexalith.EventStore/compare/e968467c7db5b5685894fa2b85cfa7ec14512a7c...ff7f07d1ff12b94c7b53646581ca3843b2c09548)

From an EventStore clone containing the two revisions, print the tree and script identities at each endpoint. The first output on each iteration is the directory tree, followed by the runner blob.

```sh
fixture=_bmad-output/implementation-artifacts/evidence/6-1-p1r-3110/verification
for revision in e968467c7db5b5685894fa2b85cfa7ec14512a7c ff7f07d1ff12b94c7b53646581ca3843b2c09548
do
    git rev-parse "$revision:$fixture" "$revision:$fixture/run_verification.py"
done
```

### Retired EventStore restore and isolation tools

The restore fixture exercises both 3.125.0 and 3.125.1. The create-database step identifies the restored container's start command. The backup step identifies the PostgreSQL dump and the dump byte digest. Both steps preserve the recorded command's timing, working directory and exit status. The ledger's separate request to link the subsequent archive copy remains open. [Retained executor results](evidence/6-4-p1r-31251-immutable-binding-20261018/final/test_p1r_published_executor.log)

The isolation fixture verifies that remote controller addresses, including TLS and SSH endpoints, are refused before Docker is called. Its retained failure record excludes the supplied credential. [Retained isolation results](evidence/6-4-p1r-31251-immutable-binding-20261018/final/test_p1r_isolation_hardening.log)

Both tests passed in a fresh replay of the `tools` directory from EventStore `2817c2cc510adf39a45994d4b9d2e8dbdf48b8a0`. Nine of the 15 files in the retained final source inventory match that commit. The other six include the executor and isolation wrapper. The retained suite results therefore describe the October working tree. This independent two-test replay describes the named commit. To repeat from its root, run the following command with local fixtures.

```sh
PYTHONPATH=tools:tools/tests python3 -m unittest -v \
  test_p1r_published_executor.MeasurementTests.testRestoreReceiptAttributesBackupAndDatabaseCreationToTheirCommands \
  test_p1r_isolation_hardening.LocalStartupTests.testRemoteEndpointsRefuseBeforeProvisioningAndDoNotRetainCredentials
```

## Review triage

- Low, corrected the reference from the original remote-controller finding to recovered-wrapper finding B7. B8 covers the startup deadline.
- Low, added a direct capture link beside the OQ8 ledger entry. The September README is itself hash-bound in a later preservation inventory, so the new navigation lives in the ledger. [Preservation inventory](evidence/6-4-advisory-resolution-20261021/platform-builds-tuple-qualification-20261029/preservation-before.json)
- Medium, made the origin of the entry assemblies and the separate Projects package consumption machine-readable. The regression now checks both against the original records.
- Low, added the exact EventStore paths and an object lookup command for reproducing the final 25-commit comparison.

## Checkout validation

The Projects archive, gitlink and planning-authority suites passed 72 tests together. Both focused EventStore tests passed at the recorded replay revision. All three immutable P0 artifacts matched their documented hashes. The production-authority index passed separately.

With the direct submodules fetched at the start of this task, the Release solution built with 36 warnings and zero errors. Two broader G-6 preflight tests failed on both the synchronized checkout and the original Projects revision `d6862037512f3a36029b5748419812f729845c4b` with its prior submodule pins.

| Check | Observed failure |
| --- | --- |
| `python3 tests/tools/test_g6_qualification_runner.py QualificationEnvironmentTests.test_hostile_caller_cannot_disable_real_msbuild_packing_or_ci_properties` | The test expects the Commons root README. MSBuild packs the project-level README. |
| `python3 tests/tools/test_g6_deferred_contracts.py DeferredG6ContractTests.test_packet_obligations_reach_real_candidate_and_accepted_validation` | The archived Builds fixture for `tools/validate-oq8-platform-evidence.py` hashes to `018f5cc078d2fabd1deea5c3db72feebb4e2480398c32d36b46a1d227ce0e01a`. Its inventory expects `87c7e5289fad892d3008ad70923c3be63b7c2fb0ac5492392e65b686812ad9c7`. |

The configured `aspire run --non-interactive` baseline exited 6. Its historical AppHost refers to a missing `Hexalith.Platform.Composition.csproj`.
