# 6.1-P1R EventStore 3.106.0 Qualification Attempt 3

This append-only bundle restarts qualification from coordinate capture after
attempt 2 stopped on an incorrectly escaped static-parity regex.

The selected EventStore coordinate is `v3.106.0` /
`76051c70cbf868c40edc00ca0344fa5bd8879b69`; the aligned Builds candidate is
`5730f5a60c0e106131498126cbee323e24db5f4e`.

The attempt is not acceptance evidence unless every required contract row is
`PASS`, the final artifact manifest verifies, and all four named owner roles
accept the same finite record.

Outcome: **stopped / non-qualifying**. Coordinate capture, candidate
commitlint, static parity, authoritative catalog, complete package audit,
seven-API comparison, EventStore source restore, and EventStore source build
passed. The first test row, `es-source-003-contracts-test`, exited `5` with
zero tests because the contract-mandated `-m:1` argument is not supported by
the repository's pinned Microsoft.Testing.Platform `dotnet test` command.
The post-failure state row confirmed the EventStore and all seven exact
dependency worktrees remained clean at their recorded revisions. Per the
stop-first rule, no later source tests, package-mode lane, remote restore,
Builds runner lane, rollback revision, or acceptance decision ran.

The bundle is sealed by `artifact-manifest.sha256`; its verification output is
retained in `manifest-verification.txt`.
