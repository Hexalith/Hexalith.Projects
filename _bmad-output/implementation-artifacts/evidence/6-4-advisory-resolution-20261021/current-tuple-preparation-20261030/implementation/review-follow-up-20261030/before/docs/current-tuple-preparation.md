# Current tuple preparation

The source candidate is official Builds `6cc96b4ff23c40c77651e1e4f85ed3170c25c7a4`.
Its catalog blob is `5d7eeadf561eed182fcb03c2b1fd97565dd52852`, SHA-256
`abe630443a9f095a1bce171c4b10314a714a8bba9f3bde8980a5c40ef46ef172`.
It preserves the selected newer dependencies. This is preparation, with
`candidateAcceptance:false`; no compatible published tuple is verified.

The existing bootstrap tool 4.38.3 and range are retained but unqualified for
this catalog. Its published source is a different revision. The current
recorded Builds gitlink is unavailable from the official remote. An unapplied
candidate gitlink patch is retained in the Projects Story 6.4 evidence; do not
record it until the exact producer source has passed qualification and its
actual publication has been verified. Live upstream heads can advance
independently; new source requires a fresh exact-input decision.

## Qualification and recording order

1. Qualify the exact official producer source. Require successful push CI for
   that full SHA, and require it still to be the intended official release
   branch tip. The observed run 41102168937 was cancelled; it is no success.
   `Tools/verify-g4-release-source.sh` checks live branch identity and exact-SHA
   successful push CI both before and after protected release approval. A newer
   main cannot silently replace this candidate. Reconcile any replacement,
   catalog and release inputs before continuing.
2. In the producer's protected hosted release environment, run its existing
   catalog, dependency audit, provenance, release-contract and package controls.
   `Tools/test-g4-tool-package-contracts.ps1 -RequireControls` qualifies actual
   Release packages and all required source/package controls. Some fixture
   harnesses create Git commits, so they were not run during this no-Git-mutation
   preparation. Local Debug host results are separate evidence.
3. Compute the proposed release through the real configured semantic-release
   analyzer/dry-run for that exact source and history. A proposal is not a
   publication. Obtain separate approval for protected producer publication.
   `verify-g4-tool-release.ps1` qualifies the actual computed version before
   tagging; `publish-g4-tool-packages.ps1` publishes and verifies all three real
   archives and their inventory/symbols. This preparation authorizes none of
   those mutations or CI dispatches.
4. Verify the official release/tag/full source, signed archives, qualification
   inventory, embedded tool version/source, Tooling bytes and source catalog.
   Fill every `publication` field in `eng/builds-tuple.json` with those observed
   identities. Its `sourceRevision` and `catalogSha256` must match `producer`.
   Keep `candidateAcceptance:false`. The binding is deliberately incomplete
   until the real version and hashes exist.
5. Align `.config/dotnet-tools.json`, the range floor in `hexalith-platform.json`
   and README with that verified release, and obtain approval to record the
   exact owner configuration/gitlink. Preserve the selected catalog and direct
   AppHost compatibility. Record the Builds source before the Platform owner
   revision and then the umbrella pointer; the preparation patch is unapplied.
6. Run fresh hosted `tuple-ci.yml` against the full recorded Platform revision.
   Retain signature, pin/catalog, clean-source, extracted-byte, foreign-reference,
   package-only Release, suite-count and output-identity checks. Passing this
   preflight still leaves authenticated candidate/policy/merge-result decisions,
   ready real services and generated/direct-host runtime gates open.
7. After independent acceptance, obtain separate Platform publication approval
   and verify its official identity. Obtain separate consumer adoption approval;
   align Projects' exact tuple, run actual package preparation and complete
   expected/actual restored package and symbol qualification. Unsupported File
   mapping and existing runtime readiness limitations remain explicit.

No local source checkout, old runtime receipt, source-equivalent catalog or
bootstrap range can substitute for any missing publication or acceptance gate.
