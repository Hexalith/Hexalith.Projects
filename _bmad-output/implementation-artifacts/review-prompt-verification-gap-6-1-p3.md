# External review prompt: verification-gap reviewer

Invoke the `bmad-review-verification-gap` skill on the complete workspace diff
for P3.

Baseline commit: `d4a69ad9a640294e849444a60d7ddfbd0468f91a`

Compare the implementation against the approved spec:

```bash
sed -n '1,360p' _bmad-output/implementation-artifacts/spec-6-1-p3-approve-production-identity-authentication-contract.md
git diff d4a69ad9a640294e849444a60d7ddfbd0468f91a
```

Also inspect the untracked files named in the adversarial prompt. Identify
changed behavior that could regress without the current Server (568/568) and
Integration (19/19) lanes detecting it. Report missing, weak, or misleading
verification only; do not modify files.
