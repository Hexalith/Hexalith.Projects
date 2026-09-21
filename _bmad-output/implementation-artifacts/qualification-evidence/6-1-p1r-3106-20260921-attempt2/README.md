# 6.1-P1R EventStore 3.106.0 Qualification Attempt 2

This append-only bundle restarts qualification from coordinate capture after
attempt 1 stopped on an incorrect harness assertion about `git describe`.

The selected EventStore coordinate is `v3.106.0` /
`76051c70cbf868c40edc00ca0344fa5bd8879b69`; the aligned Builds candidate is
`5730f5a60c0e106131498126cbee323e24db5f4e`.

The attempt is not acceptance evidence unless every required contract row is
`PASS`, the final artifact manifest verifies, and all four named owner roles
accept the same finite record.

Outcome: **stopped / non-qualifying**. Coordinate capture and post-commit
commitlint passed. `bld-align-001-static-parity` failed because the harness
lost the literal-dollar escape in its catalog regex; no later row ran.

The bundle is sealed by `artifact-manifest.sha256`; its verification output is
retained in `manifest-verification.txt`.
