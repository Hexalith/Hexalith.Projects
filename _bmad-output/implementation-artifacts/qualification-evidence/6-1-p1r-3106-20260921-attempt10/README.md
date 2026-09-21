# 6.1-P1R EventStore 3.106.0 Qualification Attempt 10

This append-only bundle restarts from coordinate capture with corrected Builds
candidate `ad52f350a2f0bc47849179ae17b4594dafff5363`. Candidate
`5730f5a60c0e106131498126cbee323e24db5f4e` is superseded only because its
retained module-run artifacts embedded the stale pre-alignment manifest hash.
Attempts 1–9, the unchanged qualification contract, EventStore source/package
coordinate, frozen intent, and planning state are preserved.

Outcome: **stopped / non-qualifying**. Coordinate capture, candidate
commitlint/static/catalog/audit, seven-API comparison, both complete EventStore
source and package-source matrices, the corrected external-consumer CPM proof,
official-only remote restore, and all 14 selected package checks passed. The
corrected Builds candidate then passed restore, Release build, Module tests,
Evidence tests, and the official packaged G-4 lane with required controls.
The retained G-4 inventory records unique disposable version
`0.0.0-p1r3106-attempt10.1`, two CLI packages plus both symbol packages, 53
qualification-evidence artifacts, source validation and controls executed, and
`releaseEligible=true` for the disposable candidate worktree only.

The two local rollback commits were created as
`44bd5ebcb1cac36f25b67ce5ba1a54a2772ca73b` (runner/schema/fixtures/evidence
and hashes bound to `3.70.1`, with `3.106.0` as the reciprocal negative) and
`caf49c58bc18f5226733e7879b4f71e99213fffd` (incremental EventStore-family
audit refresh). The audit refresh itself passed and validated 299 packages,
145 families, and one source. The first rollback qualification row,
`rollback-bld-000-commitlint`, then exited `1`: each commit body was authored
as one line longer than the repository's 200-character maximum. That exact
first failure is retained; the commits were not amended or rewritten, and no
rollback static/build/test/G-4 or EventStore `v3.70.1` row ran.

The external consumer was created outside every repository and ancestor build
configuration. `effective-msbuild-properties.json` proves central package
management was not inherited; `remote-consumer/`,
`remote-consumer-files.sha256`, and `remote-package-inventory.tsv` preserve the
exact consumer/config/assets bytes and empty-cache remote result. Candidate
packages and all packaged-command evidence are under
`retained-packages/candidate/`. `rollback-commit-metadata.txt`,
`disk-state.txt`, and the passing post-failure state row retain the exact local
rollback identities and clean state of both Builds worktrees, both EventStore
lanes, all 14 dependency worktrees, and both live repositories. Architecture,
sprint status, deferred work, downstream gates, and all four owner decisions
remain unchanged and pending.
