# External Handoff Blocker

Date: 2026-05-24
Status: blocked

## Summary

The PRD is drafted, validated, reconciled, and polished, but final closeout is blocked because the `bmad-prd` workflow has no external handoff destination configured.

Resolved workflow value:

```json
"external_handoffs": []
```

## Why This Blocks Final Closeout

The PRD Finalize workflow includes an external handoff step before setting `prd.md` to `status: final`. The user clarified that skipping this step is not intentional, so the PRD must remain in `draft` until the missing handoff is configured, executed, or explicitly waived.

## Required Decision

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
