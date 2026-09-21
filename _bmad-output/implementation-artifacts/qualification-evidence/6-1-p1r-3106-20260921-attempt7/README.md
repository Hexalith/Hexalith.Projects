# 6.1-P1R EventStore 3.106.0 Qualification Attempt 7

This append-only bundle corrects attempt 6's consumer-harness violation by
creating the disposable remote consumer outside every repository and ancestor
build configuration. The qualification contract, production source, candidate,
and attempts 1–6 are unchanged.

The selected EventStore coordinate is `v3.106.0` /
`76051c70cbf868c40edc00ca0344fa5bd8879b69`; the aligned Builds candidate is
`5730f5a60c0e106131498126cbee323e24db5f4e`.

Outcome: **stopped / non-qualifying**. Preservation, coordinate, candidate,
API, and every source/package-source restore/build/test row passed. The new
pre-restore `remote-000-effective-cpm` proof then exited `2` before invoking
MSBuild because its generated shell command contained an unmatched quote.
The exact command and log are retained; no correction or rerun occurred, and
remote restore, Builds runner, rollback, and acceptance did not run. The
external consumer and NuGet configuration bytes are copied here. The
post-failure clean-state check passed for both EventStore lanes, all fourteen
dependency worktrees, and the Builds candidate.
