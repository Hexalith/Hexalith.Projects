# 6.1-P1R EventStore 3.106.0 Qualification Attempt 1

This append-only bundle retains the first results for the selected EventStore
`v3.106.0` / `76051c70cbf868c40edc00ca0344fa5bd8879b69` coordinate and Builds
candidate `5730f5a60c0e106131498126cbee323e24db5f4e`.

The attempt is not acceptance evidence unless every required contract row is
`PASS`, the final artifact manifest verifies, and all four named owner roles
accept the same finite record.

Outcome: **stopped / non-qualifying**. Coordinate capture and post-commit
commitlint passed. `bld-align-001-static-parity` failed because the harness
expected an incorrect abbreviated `git describe` value; no later row ran.

The bundle is sealed by `artifact-manifest.sha256`; its verification output is
retained in `manifest-verification.txt`.
