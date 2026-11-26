# NuGet Trusted Publishing migration

This runbook activates the caller-owned Projects release workflow after the
workflow change has merged and exact-source CI meets the release preflight.
Implementation and review must not enable publication, dispatch Release, change
the NuGet.org policy, create or delete secrets, or publish packages.

Release requires a successful push CI run at the exact current `main` commit.
The protected release job reruns the isolated G-6 qualifier on that checkout
and validates its current-source result before NuGet login. There is no stale
G-6 exception. Historical attempt-16 remains evidence for its recorded source
and tuple only. CI audits the current tuple on every event and runs the live
qualifier when material runtime paths change, a runtime-owning submodule moves,
the change range is uncertain, or a scheduled run is due. Older proof reuse is
disabled until material-input closure is reviewed.

NuGet Trusted Publishing exchanges a GitHub OIDC token for a masked API key
that is valid for one hour. The release workflow requests that key immediately
before Semantic Release and exposes it only as the `NUGET_API_KEY` environment
variable of that process. See the official
[NuGet Trusted Publishing documentation](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing)
and the pinned [`NuGet/login` v1.2.0 source](https://github.com/NuGet/login/commit/8d196754b4036150537f80ac539e15c2f1028841).

## Builds identities

The release workflow runs the initialization action from its explicit immutable
Builds checkout under `.hexalith/builds-execution`. Release preflight checks
that checkout's HEAD against the approved execution SHA. The Projects
`references/Hexalith.Builds` gitlink supplies the package catalog and
qualification tools to the exact Projects source checkout. These coordinates
are independently pinned and audited.

At Projects `b4a2818340a6cb9f54911f65e49f04d10124f280`, the approved workflow
execution SHA is `6ad5854aa14a9950e1fbfc32d7e27953a1c4b82b` and the source
gitlink is `bf8a28b4bbcec619e8d9764d335f786f2bafb69e`. The release execution
checkout is explicitly exempt from matching the source gitlink. The
current-source G-6 receipt records both identities and validates each for its
own purpose.

## Required GitHub configuration

Keep the existing `production` environment and confirm all of these controls
before creating the NuGet.org policy:

- Required reviewers authorize the release job.
- Deployment branches allow only `main`.
- The repository Actions variable `HEXALITH_RELEASE_PUBLISH_ENABLED` exists with
  the explicit value `false`. Absence is fail-closed in the workflow, but is not
  an acceptable operating default because an organization variable may also
  exist. Establish the repository override before policy activation:

  ```bash
  gh variable set HEXALITH_RELEASE_PUBLISH_ENABLED --repo Hexalith/Hexalith.Projects --body false
  ```

- The `production` environment variable `NUGET_USER` is the NuGet.org profile
  username of the person who creates the trusted policy. Use the profile name,
  not an email address and not the `Hexalith` organization name. The same
  person must remain an active member of the policy-owning organization.

Do not add `NUGET_API_KEY` to the workflow. Leave the legacy secret in place,
unused, until the first enabled trusted release has been verified and the
retirement checklist below is complete.

## Create the NuGet.org policy

Sign in to NuGet.org as the profile recorded in `NUGET_USER`, open **Trusted
Publishing**, and add a GitHub Actions policy with these exact identity values:

| Field | Value |
| --- | --- |
| Policy owner | Organization `Hexalith` |
| Repository owner | `Hexalith` |
| Repository | `Hexalith.Projects` |
| Workflow file | `release.yml` (file name only) |
| Environment | `production` |

Grant publication of new versions only for these six exact package ID
patterns. Add an exact scope entry for each ID; do not use `*`,
`Hexalith.*`, or `Hexalith.Projects.*`:

- `Hexalith.Projects.Contracts`
- `Hexalith.Projects.EventStoreExtensions`
- `Hexalith.Projects`
- `Hexalith.Projects.Client`
- `Hexalith.Projects.Testing`
- `Hexalith.Projects.ServiceDefaults`

Keep publication frozen until the maintainer authorizes the first publication
of `Hexalith.Projects.EventStoreExtensions`. For that release, grant temporary
permission to create the new package using the exact pattern
`Hexalith.Projects.EventStoreExtensions`. Keep the other IDs scoped to publishing
new versions. Semantic Release pushes the extension first and stops if creation
is refused. As soon as the first package exists, including after a partial
release, remove its creation permission and retain only the exact-ID scope for
publishing new versions. Remove the temporary creation permission if the attempt
ends before creating the package, too.

A future package must first be added to `tools/release-packages.json`, the
independent count gate, this policy, and the runbook in one reviewed change.

## Resolve and record the release version

Run the locked Semantic Release graph from the exact eligible `main` SHA before
enablement. The dry run must resolve one version; no-release output is not an
activation candidate. Store the preview, source SHA, and resolved version in
the durable repository evidence directory:

```bash
set -euo pipefail
export RELEASE_SHA="$(git rev-parse HEAD)"
test "$RELEASE_SHA" = "$(gh api repos/Hexalith/Hexalith.Projects/git/ref/heads/main --jq .object.sha)"
test -z "$(git status --porcelain=v1)"

PREVIEW_LOG="$(mktemp)"
npm ci --ignore-scripts
npm audit signatures
NO_COLOR=1 GITHUB_TOKEN="$(gh auth token)" npm exec --no -- semantic-release --dry-run 2>&1 | tee "$PREVIEW_LOG"
export RELEASE_VERSION="$(sed -nE 's/.*next release version is ([0-9A-Za-z.+-]+).*/\1/p' "$PREVIEW_LOG" | tail -n 1)"
test -n "$RELEASE_VERSION"

export EVIDENCE_DIR="_bmad-output/implementation-artifacts/release-evidence/nuget-trusted-publishing-${RELEASE_VERSION}-${RELEASE_SHA}"
mkdir -p "$EVIDENCE_DIR"
mv "$PREVIEW_LOG" "$EVIDENCE_DIR/semantic-release-dry-run.log"
printf '%s\n' "$RELEASE_SHA" >"$EVIDENCE_DIR/release-sha.txt"
printf '%s\n' "$RELEASE_VERSION" >"$EVIDENCE_DIR/release-version.txt"
```

## Activate and verify

1. With publication still frozen, merge the migration and wait for exact-source
   CI that meets the release preflight on the current `main` SHA.
2. Dispatch **Release** from that exact `main` tip and approve `production`.
   The package-only restore and Release build must complete, the freeze notice
   must say that NuGet login and Semantic Release were skipped, and the run must
   finish successfully without requesting an OIDC credential.
3. Wait until a reviewed, releasable Conventional Commit exists on a new eligible
   `main` tip. Complete the dry-run/version record above and confirm the intended
   version is absent for all six package IDs.
4. Prove no older Release is queued, waiting, pending, requested, or running.
   Preserve the query result, require an empty array, and only then enable:

   ```bash
   ACTIVE_RELEASES="$(gh run list --repo Hexalith/Hexalith.Projects --workflow release.yml --limit 100 --json status,url --jq '[.[] | select(.status == "queued" or .status == "in_progress" or .status == "waiting" or .status == "pending" or .status == "requested")]')"
   printf '%s\n' "$ACTIVE_RELEASES" | tee "$EVIDENCE_DIR/active-release-runs.json"
   test "$(printf '%s' "$ACTIVE_RELEASES" | jq 'length')" -eq 0
   gh variable set HEXALITH_RELEASE_PUBLISH_ENABLED --repo Hexalith/Hexalith.Projects --body true
   ```

5. Dispatch **Release** once from the exact source tip and complete any
   `production` environment review configured for the repository. Confirm the
   fresh G-6 result names that source SHA, the approved tuple, passing critical
   tests, and successful isolated-resource cleanup before publication.
6. In the job log, confirm source revalidation succeeded before `NuGet/login`,
   the login step succeeded without displaying its output, and Semantic Release
   published exactly six packages. Never print, copy, download, or persist the
   temporary key.
7. After the run reaches a terminal state, immediately refreeze, including after
   failure:

   ```bash
   gh variable set HEXALITH_RELEASE_PUBLISH_ENABLED --repo Hexalith/Hexalith.Projects --body false
   ```

8. Verify the completed manual release from the reviewed source checkout.
   Set `RELEASE_RUN_ID` to the successful dispatch run ID. The verifier checks
   the successful publishing attempt and tag commit, downloads the exact six
   release assets from both registries, validates their package manifest and source
   metadata, compares every payload entry, and checks the NuGet signatures
   with `dotnet nuget verify --all`.

   ```bash
   export RELEASE_RUN_ID='<completed-run-id>'
   PUBLICATION_EVIDENCE="$EVIDENCE_DIR/publication"
   python3 scripts/verify-release-publication.py \
     --version "$RELEASE_VERSION" --source "$RELEASE_SHA" --run-id "$RELEASE_RUN_ID" \
     --evidence "$PUBLICATION_EVIDENCE"
   ```

   A successful run writes `publication/publication-verified.json` with the
   GitHub identities, both package hashes, and signature results. If the
   registries are still converging, keep the incomplete output for diagnosis
   and set `PUBLICATION_EVIDENCE` to a new output path before retrying.

   Run the package consumer gate against the verified NuGet downloads.

   ```bash
   PACKAGE_DIRECTORY="$PUBLICATION_EVIDENCE/nuget"
   pwsh ./tests/tools/run-package-dependency-gate.ps1 -Version "$RELEASE_VERSION" -PackageDirectory "$PACKAGE_DIRECTORY" -SkipPack
   ```

9. Reopen the policy in NuGet.org. It must be active with no ownership warning.
   If NuGet.org created it in a temporary activation window, confirm the
   successful publish changed it to **permanently active** before retiring the
   legacy key.

Commit or attach the complete `$EVIDENCE_DIR` through the repository's normal
reviewed evidence process. It contains the workflow run URL/SHA, predicted and
published version, six public packages from each registry, manifest/signature results,
GitHub release metadata, active-run exclusion, and dry-run record. Also record
the final repository variable value `false` and the policy activation status in
`$EVIDENCE_DIR/operator-checks.md`.

## Failure recovery

- **Missing or wrong `NUGET_USER`:** the login step fails before Semantic
  Release receives `NUGET_API_KEY`. Refreeze, correct the `production`
  environment variable to the policy creator's NuGet.org profile name, obtain
  a new green `main` tip if configuration changed in source, and redispatch.
- **HTTP 401 or claim mismatch:** refreeze and compare the policy owner,
  repository owner, repository, workflow file, and environment with the exact
  table above. Also confirm the policy creator is still an active `Hexalith`
  member. Do not fall back to the legacy API key.
- **Stale source:** the source-revalidation step fails before `NuGet/login`.
  Wait for push CI to succeed on the new live `main` tip, then redispatch from
  that tip.
- **Temporary policy inactive:** restart its activation window in NuGet.org,
  reconfirm every identity and scope, and perform one reviewed enabled release
  before the window expires.
- **Failure after publication begins:** preserve the run log and query all six
  NuGet destinations plus the GitHub tag/release. Do not blind-rerun an occupied
  immutable version. Use a separately reviewed partial-publication recovery
  plan based on the observed official state.

## Rollback

The immediate rollback is always to set
`HEXALITH_RELEASE_PUBLISH_ENABLED=false`; this prevents another credential
exchange or publication while preserving diagnostics. A source rollback must
be a reviewed change on `main`, must restore the exact-source, environment,
package, and immutable-action gates, and must pass CI before any dispatch.

Before the trusted path has one verified successful release, the unused legacy
secret remains available only as rollback custody. Re-enabling secret-based
publication requires an explicit, separately reviewed workflow rollback and a
valid scoped key; never inject the old secret into the trusted workflow. After
legacy-secret retirement, rollback means repairing or replacing the trusted
policy. It does not mean recreating a broad long-lived key without a new human
security decision.

## Retire the legacy credential

Retire the old credential only after all activation checks pass: the enabled
release succeeded, all six packages and the GitHub release were verified, the
repository was refrozen, and the NuGet.org policy is active or permanently
active as applicable.

1. Delete the `NUGET_API_KEY` secret from the `Hexalith.Projects` repository (or
   remove this repository's access if the name resolves from an organization
   secret). Do not delete a shared organization secret until every other
   consumer is identified and migrated.
2. Revoke the corresponding legacy API key in the NuGet.org account. If other
   repositories still use that same key, coordinate their rotation first.
3. Confirm repository Actions secrets no longer expose `NUGET_API_KEY` and that
   `.github/workflows/release.yml` still has no `secrets.NUGET_API_KEY`
   reference.
4. Leave `NUGET_USER`, the trusted policy, the `production` protection, and the
   default frozen publication variable in place for future releases.
