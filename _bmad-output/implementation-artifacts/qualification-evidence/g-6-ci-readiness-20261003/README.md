# G-6 CI readiness evidence — 2026-10-03

CI [37111866011](https://github.com/Hexalith/Hexalith.Projects/actions/runs/37111866011),
job [111171338892](https://github.com/Hexalith/Hexalith.Projects/actions/runs/37111866011/job/111171338892),
qualified exact pushed root `387b5af242bc99f48848f3d0265fa3c38575bc03`.
Both runner and validator printed `G6-CURRENT-QUALIFIED`; the job and full CI run passed.
Only after verifying that result did sprint-status.yaml current_checkout fields become qualified.

This directory retains byte-identical local runner outputs and the downloaded CI
artifact, original uploaded ZIP, job log and API metadata. Results still refer
to their original `.g6-current-evidence/<run>/` paths. [SHA256SUMS](SHA256SUMS)
indexes every retained file except this README, SHA256SUMS itself and .gitattributes.
Run `sha256sum -c SHA256SUMS` here. [retention-check.json](retention-check.json)
records independent canonical result, bound receipt and uploaded ZIP checks.

The tuple remains `52c8d36a9db4aae09e7a32f7a62be61ed7eee4e0f33a15099215bd61ff780098`,
with Toolkit `13.6.0-preview.1.261001-0243`. Jérôme Piquot's existing approval
at `2026-10-03T07:15:52Z` and policy SHA-256
`de3c614a23109838e53524c6f033cbc8c86c68edfa5815da608710d59feb7dfb` are unchanged.

## Committed source and fixes

- Builds `688eec9a4333245cc0ff7772115c769094471863`: replace inherited explicitly packed root README items with the owning module README. Preserve content/docs/unpacked copies; missing README remains a packing error.
- Commons `116d26815eb81e35b3c161e1799e5ee12805fc0a`: assign ProjectRoot after shared imports. This override alone still leaves the enclosing Projects README; the shared replacement is also necessary.
- Root implementation `455b26fbea4c914cea643f5c37566e91ed3e599c` and gitlinks `387b5af242bc99f48848f3d0265fa3c38575bc03`: pull exact PostgreSQL digest in CI and release; record the owner restore decision; normalize runner/tool-install build controls and retain selected controls; add real MSBuild/NuGet and workflow regressions.

Every fix was committed and validated with the owning repository's commitlint
before the fresh pre-check. Remotes were checked after local validate exit 0;
Builds and Commons were pushed before root. No material changes occurred between
local proof and the authoritative CI checkout.

## Retained attempts

| Attempt | Result | Evidence |
| --- | --- | --- |
| local 1 | Restore bootstrap stalled; runtime proof never started. Only owned restore/MSBuild processes were stopped. | [bootstrap.json](ci-readiness-local-20261003-1/bootstrap.json) and restore log |
| local 2 | Docker port-forwarding failed before runtime tests; runner failed and validator exited 1. Owned containers removed and shared resources unchanged. | [result.json](ci-readiness-local-20261003-2/result.json), cleanup and validator log |
| local 3 | Fresh non-IDE pre-check qualified; validator exited 0 before any push. | [result.json](ci-readiness-local-20261003-3/result.json) and [validate.log](ci-readiness-local-20261003-3/validate.log) |
| CI 37111866011 attempt 1 | Authoritative exact-source qualification passed. | [result.json](ci-37111866011-1/result.json), [job log](ci-37111866011-job.log) and original ZIP |

Local retries used the same committed root, fresh isolated NuGet caches,
`/bin/bash --noprofile --norc`, a whitelist of ordinary environment variables,
GITHUB_ACTIONS=true and no IDE/CI overrides. Serialized outer restore and disabled
MSBuild node reuse avoided the local bootstrap stall; no qualification requirement
changed. [focused/precheck.sh](focused/precheck.sh) and
[focused/clean-shell-launch.py](focused/clean-shell-launch.py) retain the successful recipe.

Both passing runs had audit zero issues and tupleApproved=true, qualifier 1/1,
support 33/33 across 21 selectors, fixture controls 31/31, strict capture validation,
all 10 AppHosts and McpCli, zero skips and all 18 commands exiting 0. Every cleanup
flag is true. Local shared containers and global Dapr binaries stayed unchanged.
FrontComposer and Parties built successfully with real packing enabled.

Local result SHA-256 is `b508bb380516ff4ed4f4f6cd63ded19eafe8af833dd4439a4f30dfb3161780a4`;
internal artifactSha256 is `948364c81b7b84e17c2c34f7d0e25620c25005c8434f94572818c84498b07536`.
CI result SHA-256 is `bc04ab1d5ec236db3b7fcdc18b41cb89ef355e3f4731af9f69dcd95e35c7b280`;
internal artifactSha256 is `48239d5fdeee8a59a42f4031906ac68b6f8b63d7b87111af449d7b6c5a93ff66`.

The uploaded artifact is
`g6-current-387b5af242bc99f48848f3d0265fa3c38575bc03-37111866011-1`,
id `11270158612`, ZIP digest
`sha256:a85f319814a2dbc8c2761909d6f5af99e6498b4b78e516d1fa1330aff5de3cf4`.
The downloaded ZIP matches that digest; all extracted bytes and result-bound hashes match.

## Focused verification and limits

[focused](focused/) retains the pre-fix clean-shell NU5118 reproduction, evaluated
ProjectRoot, fixed Commons build and package README bytes check (one owned README),
runner/gate tests 25/25, Builds README archive tests 6/6, tuple controls 3/3,
workflow policy and production-authority index checks. Three independent review
lenses were triaged in [the spec](../../spec-g-6-ci-readiness.md); accepted findings
were patched and verified. No new deferred item was added.

Local material fingerprint is `4a2f944a2f4791844b17489135f958a77cf5999c5d3170e74a50f64a9fd9c905`;
CI fingerprint is `4afa46578dacd7e237c497588ba0989c38e6693d5c3db6d2636f7aef9458382c`.
Each run was internally stable and source-bound. Existing working-tree EOL fingerprint
drift remains a separate deferred item; reuse remains disabled. CI's own validator
is the authority for its checkout.

This is checkout-source proof. Attempt-16, the original failed CI attempt,
G-4/G-5, P1R, release readiness and downstream statuses remain unchanged.
The release workflow uses the repaired prerequisites, but no release was dispatched.
Platform app-model startup and test_g6_packet_references.py remain separate deferred work.
