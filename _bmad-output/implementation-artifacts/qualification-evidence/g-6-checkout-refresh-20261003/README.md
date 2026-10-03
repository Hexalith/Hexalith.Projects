# G-6 current checkout refresh — 2026-10-03

[CI 37118950623](https://github.com/Hexalith/Hexalith.Projects/actions/runs/37118950623)
and [G-6 job 111191288139](https://github.com/Hexalith/Hexalith.Projects/actions/runs/37118950623/job/111191288139)
passed on root `0f03582b3457a6d9212d60e2f9146a6043af5f7e` after the
Conversations, Folders, Memories and Parties source gitlinks advanced. Both
runner and validator printed `G6-CURRENT-QUALIFIED`. The recorded qualification
applies to this source only; proof reuse remains disabled.

The existing tuple approval is unchanged: Jérôme Piquot at
`2026-10-03T07:15:52Z`, tuple SHA-256
`52c8d36a9db4aae09e7a32f7a62be61ed7eee4e0f33a15099215bd61ff780098`.
The [owner decision](../../spec-g-6-qualify-current-toolkit-preview-tuple.md#implementation-notes)
records its scope and prerelease exception.
The audit reports zero issues and 7,026 material files. Qualifier 1/1 and support
33/33 passed with zero critical skips; all 18 commands, ten AppHost builds and
McpCli succeeded. All five cleanup flags are true.

## Retained evidence

The uploaded artifact is
`g6-current-0f03582b3457a6d9212d60e2f9146a6043af5f7e-37118950623-1`,
id `11273215715`. The original ZIP SHA-256 matches GitHub's uploaded digest:
`02a2eaef4d00642f90df2bf34975417f8a5265aa2c2c5992860200cdf0830108`.
Extracted files in [ci-37118950623-1](ci-37118950623-1/) preserve original bytes.
Result SHA-256 is
`113ffa2acb1861ef74a88b77e5a933d7d2762753e69563d1a42e1206bec0b0d9`;
its canonical internal artifactSha256 is
`e6bb620b1835670829ac81ad5765d5c29c0fff6815ff11ebe912891d3e2362ef`.
The original API metadata, job log, ZIP, capture documents and command/cleanup
receipts are retained. [SHA256SUMS](SHA256SUMS) indexes every file except itself;
run `sha256sum -c SHA256SUMS` from this directory.

## Independent validation

A disposable clean checkout reproduced the exact root and all 12 root-declared
gitlinks, with `core.autocrlf=false` and no nested submodule initialization.
Existing local ignored project.assets.json files supplied restored controlled
package graphs; their summaries matched CI's graph. The reconstructed source
fingerprint matched CI exactly:
`d510a72c9e685a9a9ca6207f11b701e5654d9838730f0b648556701a8dde15c0`.
The original artifact was placed under its original workspace-relative path,
`.g6-current-evidence/ci-37118950623-1/`, without editing result or receipts.

The unmodified Builds validator checked the approved tuple, material fingerprint,
resolved controlled graphs, exact source/gitlinks, canonical result digest,
all bound captures/logs, strict EventStore OQ8 proof and isolated cleanup.
It exited 0 and printed `G6-CURRENT-QUALIFIED`; see
[clean-checkout-validate.log](clean-checkout-validate.log) and
[retention-check.json](retention-check.json). No runtime test was rerun locally.
The review follow-up [replay receipt](replay/receipt.json) retains exact preparation
and validator commands, timestamps, working directory, validator revision/hash,
copied graph-input paths/hashes, exit code and temporary-checkout cleanup.
[replay/audit.json](replay/audit.json) retains the matching local audit.
[replay.py](replay.py) checks ZIP/API binding, all extracted members and the
canonical result digest, then clones only the recorded root-declared repositories,
copies ignored local restored graphs, restores original evidence paths, and runs
the unmodified validator. That validator rechecks every bound receipt hash.
From the Projects workspace root, with current controlled graphs already restored:

```bash
python3 _bmad-output/implementation-artifacts/qualification-evidence/g-6-checkout-refresh-20261003/replay.py --source-workspace "$PWD" --receipt-directory /var/tmp/g6-replay-new-receipt
```

Use a new receipt directory; the recipe never overwrites an existing one.
To replay on a clean checkout of that exact source with restored graphs and
the artifact in its original path:

```bash
python3 references/Hexalith.Builds/Tools/g6_current.py validate --workspace . --policy references/Hexalith.Builds/Tools/g6-current-policy.json --evidence .g6-current-evidence/ci-37118950623-1/result.json
```

Only G-6 current-checkout tracking advances. Prior accepted packets, readiness
evidence and tuple approval remain unchanged. This source proof does not qualify
published EventStore archives, G-4/G-5, P1R, downstream work or release.
EOL fingerprint reproducibility, Platform app-model startup and the obsolete
packet-reference test remain in their [existing deferred ledger](../../deferred-work.md).
The retained Platform build warns ASPIRE010 because AspireUseCliBundle=false;
the build passed, while Platform app-model startup remains unproved under that
existing deferred item. Local investigation controls and historical-preservation
checks are retained separately in [investigation](investigation/).
