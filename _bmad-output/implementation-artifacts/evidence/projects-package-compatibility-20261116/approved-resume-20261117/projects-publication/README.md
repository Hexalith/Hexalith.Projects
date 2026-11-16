# Approved exact Contracts release: route and remaining gates

The approved archive remains available as committed Projects blob
`3fc916b6fc7676e0327181e1e217f715e1645174`, 80,379 bytes, at
`_bmad-output/implementation-artifacts/evidence/epic6/6.6/review-pass2/candidate/Hexalith.Projects.Contracts.2.0.0-story66.3.nupkg`.
Its SHA256 is still
`349f938db77dd83ff45e200997b1cba97147c46b1855ec58fd6e2a4b0572eff5`.
All six payload hashes match the approved proposal. Existing source binding and
McpCli restore/build/discovery evidence are reused; no broader qualification was
repeated.

Current Projects `cdacfb2a2c9d1eb23f2dbd59b53f42b945e80f37` has five workflows.
Only `release.yml` publishes packages. It has no dispatch inputs, requires green
push CI on exact current main and fresh G-6 before NuGet login, then invokes
Semantic Release for all five packages. It cannot publish this retained single
historical prerelease. The current main CI run
[43967083172](https://github.com/Hexalith/Hexalith.Projects/actions/runs/43967083172)
failed. No release dispatch was attempted.

The smallest feasible extension belongs to CiCd: add one closed compatibility
profile inside existing Projects `release.yml`, delivered through the qualified
CiCd owner mechanism. Preserve the normal release path. The new profile must
fetch the single immutable blob, verify its approved coordinate/source/hash and
six payloads, bind the applicable owner release proofs, then use the existing
pinned NuGet login and one exact-file push. Preserve the existing
`release-production` mutex and Projects/release.yml/production OIDC identity;
create no feed, credential scope, arbitrary package selector or repack path.
The required release gates must pass before credential exchange and upload.

NuGet.org automatically repository-signs uploaded packages. The original
unsigned archive therefore need not receive a temporary author signature.
Public verification must require every approved payload byte unchanged, allowing
only the authenticated `.signature.p7s` addition. Self-signed validation
certificates cannot be used for publication. If the package already requires an
author signer, use its existing registered signer; do not change that policy.
These boundaries follow the official [signature reference](https://learn.microsoft.com/en-us/nuget/reference/signed-packages-reference),
[signing requirements](https://learn.microsoft.com/en-us/nuget/create-packages/sign-a-package)
and [trusted publishing protocol](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing).

B1 concerns inventory approval capacity, not an X.509 certificate. Current
authenticated GitHub reads identify `jpiquot`, account 6775094, as Jérôme Piquot,
an active Hexalith organization administrator and administrator of Projects and
McpCli. This verifies the named session approver's role/capacity alongside the
current explicit package-owner approval. No separate external signature is
invented and no legacy retirement/completeness approval is claimed.

B4 and V1 remain substantive requirements. Existing publication preparation
evidence does not qualify the native McpCli Projects list plan: its vector still
expects an empty Unavailable result without a discriminating fixture, and the
pinned native host lacks the Projects service, committed routes and signed read
grants. The existing package product matrix and latest publication repair
receipts explicitly report incomplete native scenarios; three passing initial
read/security rows were followed by actual process restart failures, and the
latest graph later stopped at the SDK package prerequisite. Completed A8
configuration work cannot substitute for these runtime results. Their receipts
were read and reused; no A8 audit or native rerun was performed.

The current repository publication variable is `true`, `NUGET_USER` is
`jpiquot`, repository secrets are absent, and the production environment has no
protection rules or deployment branch policy. These are observations only; no
setting was changed. The authenticated NuGet policy's current active state and
required-signer setting have not been established by this read-only task.

`current-route-observation.json`, `inventory-approver-capacity.json` and
`route-and-gates.json` retain exact source, artifact, role and route findings.
No publication, dispatch, workflow edit, tracking change, deployment or v2
activation was performed.
