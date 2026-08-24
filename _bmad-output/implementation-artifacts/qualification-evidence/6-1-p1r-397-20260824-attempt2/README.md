# 6.1-P1R `3.97.0` stopped qualification attempt 2

This bundle retains the first results from the second authorized clean
qualification attempt on 2026-08-24. It preserves the prior stopped bundle at
`../6-1-p1r-397-20260824/` byte-for-byte. This bundle is failure evidence only;
it is not a qualification packet, owner acceptance, Architecture authority,
publication authority, or permission to close P1R or any downstream gate.

## Passed rows before the stop

- `es-001-coordinates` passed in a clean detached EventStore worktree at
  `94591f3539ce30372db58e5fdd3ba017ea8c07b8`, described exactly as
  `v3.97.0`. The tag resolves to the same commit, so the selected source and
  package-source identities are equivalent. The retained 14-ID release
  manifest has SHA-256
  `6b0b70b856839d4117bcd969f6a2de0093c477c109cb79f3f2882b1f05effcae`.
- EventStore HEAD `da52e2c85ecc5909fa8ce2547e626f3968c056ef`
  was recorded as the unselected `v3.97.0-5-gda52e2c8` observation. Its
  selected-tag-to-HEAD `src/` diff is empty. The selected, HEAD, and rollback
  dependency gitlinks were retained in the coordinate log.
- `bld-align-001-static-parity`, `bld-align-002-catalog`, and
  `bld-align-003-audit` passed in a clean detached Builds worktree at immutable
  candidate `fb05dd84625abdcd1a62d2664e8557379fd631bb`. The runner,
  schema, active fixtures, serialized evidence, package controls, and coupled
  hashes bind `3.97.0`; `3.88.0` and `3.70.1` remain exact `HXM016`
  negatives; invalid-profile remains `HXM009`; and the deliberate all-`F`
  evidence hash remains invalid. The catalog passed 49 identities and three
  shared versions; the complete audit passed 284 packages, 139 families, and
  one source.
- `es-002-api-blobs` passed. The selected package/source commit and unselected
  HEAD have identical blobs for all seven APIs. The rollback differs only by
  the already recorded additive `QueryCursorScope.AddProjectionWatermark(long?)`
  surface; no compared API was removed or changed.

## First non-passing row

`es-source-001-restore` ran from the clean selected EventStore worktree from
`2026-08-24T17:43:55.766785516Z` to
`2026-08-24T17:43:59.678633800Z`. The exact mandated command exited `1` and is
`FAIL`. Its retained log SHA-256 is
`d44d2b3788179e9d945e2bbf6aafcb081f28f0e4e9fb853c63e23dd100011ccc`.

The isolated worktree correctly retained empty nested-submodule directories in
accordance with the prohibition on nested initialization. Consequently its
import-only `Directory.Packages.props` found none of the three relative
`Hexalith.Builds/Props/Directory.Packages.props` locations. NuGet Central
Package Management then reported widespread `NU1010` errors because package
references had no matching `PackageVersion` declarations, and restore ended
with a NuGet `Object reference not set to an instance of an object` error.

The post-failure state capture confirmed the EventStore worktree remained
clean, still at `94591f3539ce30372db58e5fdd3ba017ea8c07b8`, and still
described as `v3.97.0`.

## Stop boundary and pending action

Per the first-non-passing rule, no later source build/test, package-source
lane, remote 14-package restore, clean Builds restore/build/test/package lane,
rollback commit, or reciprocal rollback execution started. The candidate
Builds commit remains implemented but unqualified. Architecture and sprint
status remain unchanged, and no dependency gitlink was moved by this attempt.

Before another clean attempt, the EventStore Owner, Builds Owner, Solution
Architect, and Test Architect must select and approve a dependency-materialization
or import-routing method that supplies the exact selected EventStore revision's
recorded Builds catalog without initializing nested submodules, changing the
qualified source, or weakening the mandated command. A new attempt must retain
both stopped bundles and restart at coordinate capture.

All four owner decisions remain pending; no acceptance is inferred.
