# Qualification Evidence

This directory retains historical evidence that is outside the retired EventStore
3.106 P1R qualification flow. Older P1R roots and the accepted G-6
runtime/toolchain packet remain historical inputs; they do not accept the current
P1R baseline.

## Current P1R Boundary

The selected coordinates are:

- EventStore `3.106.0`, tag `v3.106.0`, revision
  `76051c70cbf868c40edc00ca0344fa5bd8879b69`.
- Builds revision `ad52f350a2f0bc47849179ae17b4594dafff5363`.

The rollback coordinates are:

- EventStore `3.70.1`, tag `v3.70.1`, revision
  `f13f9925fdca53efa2ab8c90d396ab106f91bb9c`.
- Builds revision `7af20f8bafbfe561df6f7705913a0800603090b5`.

P1R is open. No current acceptance record exists. The only accepted closure
mechanism is the optional fixed record at
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

When the record is absent, the open planning state is valid. When a valid record
exists, only P1R, P0 Stage 1, DW-35, and DW-68 may transition to `done`; all
later production-readiness and Epic 7/8 work remains open or blocked.
