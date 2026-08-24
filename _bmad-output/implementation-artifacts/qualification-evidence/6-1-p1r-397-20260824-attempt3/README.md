# 6.1-P1R `3.97.0` stopped qualification attempt 3

This bundle retains the first results from the third authorized clean
qualification attempt on 2026-08-24. It verifies and preserves both earlier
stopped bundles byte-for-byte. This bundle is failure evidence only; it is not
a qualification packet, owner acceptance, Architecture authority, publication
authority, or permission to close P1R or any downstream gate.

## Dependency materialization used

The approved route used repository-supported sibling materialization. Each
clean detached EventStore worktree lived at `<lane>/EventStore`; a separate
clean detached Builds worktree lived at `<lane>/references/Hexalith.Builds`.
The EventStore import search therefore resolved the exact candidate catalog at
`fb05dd84625abdcd1a62d2664e8557379fd631bb` without initializing any
nested submodule, editing EventStore source, moving a gitlink, changing the
qualified EventStore revision, or weakening a mandated command.

The post-failure state capture proves the EventStore worktree stayed clean at
`94591f3539ce30372db58e5fdd3ba017ea8c07b8`, the sibling Builds worktree
stayed clean at `fb05dd84625abdcd1a62d2664e8557379fd631bb`, and every
recorded EventStore nested submodule remained uninitialized.

## Passed rows before the stop

- `history-001-preserve` verified every file in both earlier stopped bundles
  against their manifests and verified the append-only Builds record ancestry.
  Their manifest SHA-256 values remain
  `1c535eac91ae9433a21a20ab1e948e3951a2f62184ac7af137316dfcf0fb0265`
  and
  `fbe5596f7dc1dd5d3a3bbc3b3ba02f9ff8b7e9da284b9b890fe5baccfe9f25d4`.
- `es-001-coordinates-and-routing` passed in the clean selected EventStore
  worktree, described exactly as `v3.97.0`. The package tag resolves to the
  same commit. The exact sibling Builds candidate/catalog routing passed; the
  catalog SHA-256 is
  `c51967e25c5a8770fcae6469f335b38775904c5c56b8a4fc4fb67605a1cb84bb`.
  The retained 14-ID release manifest has SHA-256
  `6b0b70b856839d4117bcd969f6a2de0093c477c109cb79f3f2882b1f05effcae`.
  Unselected EventStore HEAD remained
  `da52e2c85ecc5909fa8ce2547e626f3968c056ef`, with no `src/` diff from
  the selected tag. Exact dependency gitlinks were retained.
- `bld-align-001-static-parity`, `bld-align-002-catalog`, and
  `bld-align-003-audit` passed at the immutable candidate. The runner/schema/
  fixture/evidence/hash contract binds `3.97.0`; `3.88.0` and `3.70.1`
  remain exact `HXM016` negatives; the catalog passed 49 identities and three
  shared versions; and the full audit passed 284 packages, 139 families, and
  one source.
- `es-002-api-blobs` passed. Selected package/source and unselected HEAD blobs
  are identical for all seven APIs. Rollback differs only by the already
  recorded additive `QueryCursorScope.AddProjectionWatermark(long?)` surface.
- `es-source-001-restore` passed the exact mandated Debug source-mode restore
  with the isolated lane cache. This proves the sibling catalog route resolved
  the attempt-2 restore blocker.
- `es-source-002-build` passed the exact mandated serialized Debug source-mode
  build with zero warnings and zero errors.

## First non-passing row

`es-source-003-contracts-test` ran from
`2026-08-24T18:22:22.543299647Z` to
`2026-08-24T18:23:51.038975740Z`. The exact mandated command exited `1` and
is `FAIL`. Its retained log SHA-256 is
`a85c4b7a94f7277000665059b6677f017dceea4836a09d98565bbaa1037130b5`.
The result was 29 failed, 1503 passed, zero skipped, and 1532 total tests.

The earliest failures show that the sibling catalog route is sufficient for
MSBuild import and compilation but not for repository-governance tests that
directly read nested dependency worktrees or pinned dependency Git bytes:

- `Hexalith.Tenants` project content was absent at the recorded nested path;
- the shared LLM instruction file was absent at its recorded nested path;
- `Hexalith.Builds/Github/dapr-init/action.yml` was absent at the recorded
  nested path, causing the live-sidecar guardrail cases to fail;
- a Builds tool path was not available in pinned commit
  `a53166539bf4441d5e33d04281b14c2d59e950c3`; and
- publication-authority fixtures could not resolve Git tree
  `a07078ad74d3727bc5a6b6d85d47d56a6e5c9fec`.

Additional OCI/evidence mutation assertions then failed closed. The retained
log is the authority for all 29 exact names, messages, and stack traces; no
failure was filtered, retried, or relabeled.

## Stop boundary and pending action

Per the first-non-passing rule, the remaining source tests, package-source
lane, remote 14-package restore, clean Builds restore/build/test/package lane,
official G-4 controls, rollback commit, and reciprocal rollback execution did
not start. The candidate Builds commit remains implemented but unqualified.
Architecture and sprint status remain unchanged, no dependency gitlink was
moved, and no publication or push occurred.

Any future attempt requires new owner authorization. It must preserve all three
stopped bundles and restart at coordinate capture. The owners must choose an
exact dependency-object/materialization route that satisfies the selected
source revision's repository-governance tests while honoring the prohibition
on nested submodule initialization and source/gitlink changes.

All four owner decisions remain pending; no acceptance is inferred.
