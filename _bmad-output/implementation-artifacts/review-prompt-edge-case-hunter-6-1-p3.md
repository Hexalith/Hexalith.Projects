# External review prompt: edge-case hunter

Invoke the `bmad-review-edge-case-hunter` skill on the complete workspace diff
for P3.

Baseline commit: `d4a69ad9a640294e849444a60d7ddfbd0468f91a`

Inspect:

```bash
git diff d4a69ad9a640294e849444a60d7ddfbd0468f91a
```

Include the untracked implementation, documentation, and test files listed in
`review-prompt-adversarial-general-6-1-p3.md`. Walk every environment/config,
token-claim, bypass, startup, and safe-denial branch. Report only unhandled
edge cases, naming the affected file and the concrete missing behavior.
