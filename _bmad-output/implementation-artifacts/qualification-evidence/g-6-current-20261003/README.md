# Current G-6 qualification — 2026-10-03 (local pre-check)

This directory keeps both v3 runs from
`spec-g-6-qualify-current-toolkit-preview-tuple.md`. They are byte-identical
copies of the gitignored `.g6-current-evidence/<run>/` directories. Each
`result.json` receipt still names its original `.g6-current-evidence/...`
path. [SHA256SUMS](SHA256SUMS) indexes every retained file; check it with
`sha256sum -c SHA256SUMS` from this directory.

These local runs are pre-checks. The CI `g6-current` run on the pushed root
SHA decides `current_checkout_status`. This is checkout-source proof: it does
not qualify published EventStore archives, G-4/G-5, P1R, release readiness, or
any downstream status.

## Runs

| Run | Root source | Policy decision | Status | `result.json` SHA-256 | `artifactSha256` |
| --- | --- | --- | --- | --- | --- |
| [candidate-20261003-1](candidate-20261003-1/result.json) | `accaee77fcda44fc0df7627a1d6120e08f8a604c` | pending | `not verified` | `399befab8b1035496f8b60bf59652e6e24bf55189a260f1b4ccd4d5faf97c26e` | `03648b68affe1fbac1d04316ae38008a6e76fbaf1a89596c8887b226b94ea89d` |
| [final-20261003-1](final-20261003-1/result.json) | `dac6ef2c235b3480c84ce6f57bc41172ca2c09b5` | approved | `qualified` | `dc834ba4479516e55fb950e7f6288376c39eb791c552616e07d590d91c6ef34f` | `aca6684d9fcf44baee38276fe731472434df85b2608c4bd76aa9401d52b69a27` |

The two runs had the same outcomes:

- Qualifier: 1/1 passed.
- Support: 33/33 passed across 21 selectors.
- Fixture controls: 31/31 passed.
- Strict OQ8 capture validation passed.
- Ten AppHosts compiled: nine from source, and the Platform file host in package mode. The McpCli transitive Dapr consumer also compiled.
- All 18 commands exited `0`, with zero skips.
- Every cleanup flag is true.
- The 15-container shared snapshot was unchanged.
- The four owned containers (placement, scheduler, redis, postgresql) were removed.

**Candidate run.** The only audit issue was the pending named approval.
`g6_current.py validate` exited `1` with `current effective tuple or resolved
graph is not approved`. The candidate run is kept as the result the owner
decided on.

**Owner decision.** Jérôme Piquot (Builds/Platform/FrontComposer-Web) answered
verbatim "Approve" at `2026-10-03T07:15:52Z`. The record is in the spec's
Implementation Notes. Builds `c16249a6a0c88b903e14c7f6612632e2c70d4e94` records
it in `Tools/g6-current-policy.json` (file SHA-256
`de3c614a23109838e53524c6f033cbc8c86c68edfa5815da608710d59feb7dfb`). The tuple
hash `52c8d36a9db4aae09e7a32f7a62be61ed7eee4e0f33a15099215bd61ff780098` is
unchanged.

**Final run.** The audit had zero issues and `tupleApproved=true`. The material
fingerprint was
`3ad7d5d5034e78db6ef85202af59ebce2a5799e75d2c317357ba8f7dc3f8bf3b` (7,021 files),
the same before and after the run. Every root and submodule checkout was clean,
and each submodule HEAD equalled its root gitlink.
[final-validate.log](final-validate.log) records `G6-CURRENT-QUALIFIED` and exit
`0` from:

```bash
python3 references/Hexalith.Builds/Tools/g6_current.py validate --workspace . \
  --policy references/Hexalith.Builds/Tools/g6-current-policy.json \
  --evidence .g6-current-evidence/final-20261003-1/result.json
```

## Tuple and environment

The tuple:

- .NET SDK `10.0.401`
- Aspire SDK/CLI `13.6.0`
- CommunityToolkit.Aspire.Hosting.Dapr `13.6.0-preview.1.261001-0243` (approved prerelease exception)
- Dapr CLI `1.18.0`, runtime `1.18.2`, .NET packages `1.18.10` (approved explicit support-table exception)
- Fluent UI `5.0.0`
- NSubstitute `6.2.0`
- Fluxor `6.11.0`

The runs used Debug source mode with an isolated NuGet cache and tools, in
execution scope `ci`. The Builds execution SHA was
`51af786cf156d2a3396dbd49f5e4898222e55c12`.

## Limitations

- The live qualifier exercises Dapr runtime 1.18.2 and the Dapr .NET packages.
  The Toolkit preview is covered only by restore, the resolved-graph audit, and
  compiling all ten AppHosts. No AppHost was started.
- The Platform file host was compiled against its NuGet directives, including
  EventStore.Aspire `3.110.0`. That is package consumption, not checkout-source
  proof.
- The SQLite name resolver is Alpha. It is selected only for this disposable
  fixture.
- The following remain unselected or unqualified: the Keycloak/Kubernetes
  `13.6.0-preview.1.26479.8` exclusions, Works/mTLS `1.18.3`, and Dapr.Workflow.
- `reuseEnabled` is false, so each qualified result must match its exact
  source.

## Final-run source

| Path | Revision |
| --- | --- |
| references/Hexalith.AI.Tools | `3f194e17174994d308ec84af9ee2b5aa68674d0d` |
| references/Hexalith.EventStore | `2c58ffda41759e895ace4b9625c9bd931a217672` |
| references/Hexalith.Tenants | `11e65e37f0fbf6649642a512052eebd37d50166d` |
| references/Hexalith.FrontComposer | `bf40099f81fcaeac324b7b4377513ac7d49cead4` |
| references/Hexalith.Conversations | `35121cab6bfc963fb2a8d411a51800f6ef5a2a70` |
| references/Hexalith.Folders | `ebefb2836d5debe6735b60d070d805cee55168c5` |
| references/Hexalith.Parties | `937cb2a343aaa74963db9bb867a2c3a01ff48677` |
| references/Hexalith.Commons | `c13dc6679aa91144b6d541078f3f20019d79c2eb` |
| references/Hexalith.Builds | `c16249a6a0c88b903e14c7f6612632e2c70d4e94` |
| references/Hexalith.Memories | `ece4edc4c9a37a62b34d3b7c8aa901fc363c038c` |
| references/Hexalith.Platform | `73421308fd42fecf034751a2f3d5bf821caf3c8d` |
| references/Hexalith.McpCli | `9b0b6e7a082bf93b669a883e48672c26c6f0630d` |
