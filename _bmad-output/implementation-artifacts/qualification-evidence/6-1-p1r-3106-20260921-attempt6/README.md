# 6.1-P1R EventStore 3.106.0 Qualification Attempt 6

This append-only bundle is the contract-permitted clean serialized rerun after
attempt 5 retained a one-test contention failure that subsequently passed ten
independent isolated reruns. The qualification contract is unchanged from
attempt 5, and attempts 1–5 remain preserved byte-for-byte.

The selected EventStore coordinate is `v3.106.0` /
`76051c70cbf868c40edc00ca0344fa5bd8879b69`; the aligned Builds candidate is
`5730f5a60c0e106131498126cbee323e24db5f4e`.

This attempt is acceptance evidence only if every required contract row is
`PASS`, the final artifact manifest verifies, and all four named owner roles
accept the same finite record.

Outcome: **stopped / non-qualifying**. Preservation of attempts 1–5,
coordinate capture, candidate commitlint/static/catalog/audit, the seven-API
comparison, and every selected source and package-source restore/build/test
row passed. In particular, the clean Contracts rerun passed all 2,023 tests
and supersedes attempt 5's retained contention result under the contract's
clean-rerun rule.

The next required row, `remote-001-restore`, exited `1` with `NU1008` before
remote package resolution. The retained consumer lived below the umbrella
tree and inherited its central package management, which rejected inline
versions on the 13 `PackageReference` items. The 14th DotnetTool package was
represented as an exact `PackageDownload`. Per the explicit stop-first
instruction, the consumer was not corrected or rerun, and no Builds runner,
rollback, or acceptance row ran. The post-failure state check passed for both
EventStore lanes, their fourteen exact dependency worktrees, and the Builds
candidate.
