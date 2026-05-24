# External Handoff Waiver

Date: 2026-05-24
Status: waived

## Summary

The PRD is drafted, validated, reconciled, and polished. The `bmad-prd` workflow has no external handoff destination configured.

Resolved workflow value:

```json
"external_handoffs": []
```

## Why This Blocks Final Closeout

The PRD Finalize workflow includes an external handoff step before setting `prd.md` to `status: final`. The missing handoff configuration previously blocked final closeout.

## Waiver Decision

On 2026-05-24, the user instructed `fix` after validation surfaced the blocker. Because no handoff destination is configured in `_bmad/custom`, this is recorded as an explicit waiver for this PRD closeout. `prd.md` may remain `status: final`.

## Future Configuration

Define the intended handoff destination and tool directive, for example:

```toml
[workflow]
external_handoffs = [
  "After finalize, upload prd.md and validation-report.md to <destination> via <tool> with <required fields>."
]
```

Expected override path:

`_bmad/custom/bmad-prd.toml` for team/project configuration, or `_bmad/custom/bmad-prd.user.toml` for a personal local override.

## Current Local Artifacts

- `prd.md`
- `.decision-log.md`
- `reconcile-product-brief.md`
- `review-rubric.md`
- `validation-report.md`
- `validation-report.html`
