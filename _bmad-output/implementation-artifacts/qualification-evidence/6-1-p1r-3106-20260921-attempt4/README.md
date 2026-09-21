# 6.1-P1R EventStore 3.106.0 Qualification Attempt 4

This append-only bundle restarts qualification from coordinate capture after
the human explicitly renegotiated only the contract's `dotnet test`
serialization option from `-m:1` to `--max-parallel-test-modules 1`.
Attempts 1–3 remain preserved byte-for-byte.

The selected EventStore coordinate is `v3.106.0` /
`76051c70cbf868c40edc00ca0344fa5bd8879b69`; the aligned Builds candidate is
`5730f5a60c0e106131498126cbee323e24db5f4e`.

This attempt is acceptance evidence only if every required contract row is
`PASS`, the final artifact manifest verifies, and all four named owner roles
accept the same finite record.

Outcome: **stopped / non-qualifying**. Coordinate capture, preservation of all
three prior manifests, candidate commitlint, static parity, authoritative
catalog, complete package audit, the seven-API comparison, source restore,
source build, and the renegotiated Contracts test passed. The next row,
`es-source-004-client-test`, exited `1`: compilation of
`Hexalith.EventStore.Contracts` imported both the restored
`Hexalith.Commons.UniqueIds` 2.30.0 package and the exact-dependency source
assembly (version 1.0.0), producing `CS1704`. The source restore assets record
the package while build evaluation records `HexalithCommonsFromSource=true`.
The post-failure state row confirmed the selected EventStore and all seven
exact dependency worktrees remained clean at their recorded revisions. Per
the stop-first rule, no package-source, remote-restore, Builds runner,
rollback, or acceptance phase ran.
