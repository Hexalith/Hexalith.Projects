# Qualification Evidence

This directory retains historical evidence that is outside the retired EventStore
3.106 P1R qualification flow. Older P1R roots and the accepted G-6
runtime/toolchain packet remain historical inputs; they do not accept the current
P1R baseline.

## Check the retained manifests

From the Projects repository, run the following command to check every retained
`artifact-manifest.sha256` under this directory.

```bash
python3 tools/qualification/verify_retained_manifests.py
```

The command reads the manifests and artifact bytes from the same Git commit
(`HEAD`). It reports verified entries per bundle and exits nonzero for missing
files, malformed entries, or digest mismatches. The three EventStore 3.97
bundles include their original command logs (DW-70). Their manifest entry
counts are 10 for [the first bundle](6-1-p1r-397-20260824/artifact-manifest.sha256),
18 for [attempt 2](6-1-p1r-397-20260824-attempt2/artifact-manifest.sha256), and
27 for [attempt 3](6-1-p1r-397-20260824-attempt3/artifact-manifest.sha256).

## Package retention

Record the package ID, normalized version, byte length, SHA-256 digest, and
immutable download coordinate for each archive used to qualify a candidate.
Keep this inventory with the result even after the archive expires.

- For published packages, use the exact registry package coordinate and verify
  its downloaded bytes against the inventory. Treat symbol packages separately
  because a download location for the matching `.nupkg` does not establish
  availability of its `.snupkg`.
- Keep unpublished qualification archives in an owner-controlled artifact store
  with a 90-day retention period. Before accepting one as a current or rollback
  input, move it to durable storage and record that coordinate. Retain it while
  any acceptance or rollback record requires it, and for 90 days after the
  last such record is superseded.
- Review expired archives before pruning. Prune only when no active decision
  requires the bytes and the checked-in inventory records the retired status
  or a verified replacement coordinate. Local build and download copies are
  disposable after the inventory and required durable copies are verified.
- Use Git for inventories and verification records. The eight historical
  package blobs already tracked here are retained with their original
  manifests. Keep new archives in the registry or artifact store. Keep the
  historical blobs in ordinary Git, where the manifest verifier reads and
  hashes their contents directly. Git LFS pointers change those blob bytes and
  are outside this evidence format.

## Current P1R Boundary

The selected coordinates are:

- EventStore `3.106.0`, tag `v3.106.0`, revision
  `76051c70cbf868c40edc00ca0344fa5bd8879b69`.
- Builds revision `ad52f350a2f0bc47849179ae17b4594dafff5363`.

The rollback coordinates are:

- EventStore `3.70.1`, tag `v3.70.1`, revision
  `f13f9925fdca53efa2ab8c90d396ab106f91bb9c`.
- Builds revision `7af20f8bafbfe561df6f7705913a0800603090b5`.

P1R is accepted through the fixed record at
`_bmad-output/implementation-artifacts/6-1-p1r-acceptance.json` under schema
`hexalith.projects.p1r-acceptance.v1`.

That JSON object must contain exactly `schema`, `selected`, `rollback`,
`timestamp_utc`, and `decisions`. The decisions mapping must contain exactly the
EventStore Owner, Builds Owner, Solution Architect, and Test Architect. Every
decision must be `accept`, name a non-empty approver, name the fixed record path,
and reference `#/selected`.

Run the gate with:

```bash
python3 tools/planning/validate_production_authority.py --validate-index
```

The accepted record closes only P1R, P0 Stage 1, DW-35, and DW-68. All later
production-readiness and Epic 7/8 work remains open or blocked.
