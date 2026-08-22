# 6.1-P1R Builds `3.90.0` Candidate Evidence

This directory is a durable umbrella copy of the final Builds runner
qualification evidence captured on 2026-08-04. It is **candidate evidence, not
P1R or owner acceptance**. Architecture remains bound to `3.70.1`; the
immutable qualifying Builds revision, reciprocal rollback, final EventStore
qualification, and all four owner decisions remain pending.

The source record is
[`references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p1r-eventstore-source-architecture-runner-revalidation-record.md`](../../../../references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p1r-eventstore-source-architecture-runner-revalidation-record.md).

## Captured lane

- Command version: `0.0.0-p1r-revalidate-390.8`
- Started: `2026-08-04T16:41:14.498514549Z`
- Ended: `2026-08-04T16:43:42.062479348Z`
- Exit/result: `0` / `PASS`
- Builds working tree base: `a53166539bf4441d5e33d04281b14c2d59e950c3`
- EventStore source/package revision: `7854f8e51ce9b852bb6c3cac6012670122e93792`
- EventStore tag/package version: `v3.90.0` / `3.90.0`

## Durable files

| File | SHA-256 | Meaning |
| --- | --- | --- |
| `g4-qualification.log` | `2603caa8aa18a410cdf5d2e456ee7c787e45729df86101785b8d8b726267a78e` | Exact final command output |
| `g4-tool-package-inventory.json` | `20d21a89db7baf174c81dc8c089ed688b1d96085e66cad16c5d8ac3043b869d4` | Success-only package/evidence inventory |
| `module-run.json` | `725ef49d0415a7d75b55a80f90e2d895db0ac57c5d0a4f805c757a07c3263a42` | Passing packaged evidence with EventStore `3.90.0` |
| `unavailable.json` | `00cdf6bbd400e78c2eddac128058ab9fbcf9e329cd3eb2d75111e2daaae7faa1` | Non-passing packaged evidence with EventStore `3.90.0`, exit `2`, and `HXR002` |

The four package artifacts remain in the retained qualification directory
`/tmp/hexalith-builds-p1r-390-final.YhznX7/packages`. Their exact names, sizes,
and SHA-256 values are recorded in the inventory and source record. They are
disposable candidate packages and are not publication artifacts.
