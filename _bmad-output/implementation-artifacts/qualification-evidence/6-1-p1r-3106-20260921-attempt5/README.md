# 6.1-P1R EventStore 3.106.0 Qualification Attempt 5

This append-only bundle restarts qualification from coordinate capture after
the human explicitly renegotiated all ten `dotnet test` rows to add
`--no-build`, retaining `--no-restore` and
`--max-parallel-test-modules 1`. Attempts 1–4 remain preserved byte-for-byte.

The selected EventStore coordinate is `v3.106.0` /
`76051c70cbf868c40edc00ca0344fa5bd8879b69`; the aligned Builds candidate is
`5730f5a60c0e106131498126cbee323e24db5f4e`.

This attempt is acceptance evidence only if every required contract row is
`PASS`, the final artifact manifest verifies, and all four named owner roles
accept the same finite record.

Outcome: **stopped / non-qualifying**. Preservation of attempts 1–4,
coordinate capture, candidate commitlint/static/catalog/audit, the seven-API
comparison, source restore, and source build passed. The first test row,
`es-source-003-contracts-test`, executed 2,023 tests with the renegotiated
`--no-build --no-restore --max-parallel-test-modules 1` command: 2,022 passed
and one failed. The failing
`ReleaseSourcePreflightFailsClosedAndEmitsSelectedSuccessfulPushProof` case
received process exit `126` instead of `0`; the row exited `2`. The
post-failure state row confirmed the selected EventStore and all seven exact
dependency worktrees remained clean. Per the explicit stop-first instruction,
no later source, package-source, remote-restore, Builds runner, rollback, or
acceptance row ran.
