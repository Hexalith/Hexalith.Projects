# NuGet Trusted Publishing migration

This runbook activates the caller-owned Projects release workflow after the
workflow change has merged and exact-source CI is green. Implementation and
review must not enable publication, dispatch Release, change the NuGet.org
policy, create or delete secrets, or publish packages.

NuGet Trusted Publishing exchanges a GitHub OIDC token for a masked API key
that is valid for one hour. The release workflow requests that key immediately
before Semantic Release and exposes it only as the `NUGET_API_KEY` environment
variable of that process. See the official
[NuGet Trusted Publishing documentation](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing)
and the pinned [`NuGet/login` v1.2.0 source](https://github.com/NuGet/login/commit/8d196754b4036150537f80ac539e15c2f1028841).

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

Grant publication of new versions only for these five exact package ID
patterns. Add an exact scope entry for each ID; do not use `*`,
`Hexalith.*`, or `Hexalith.Projects.*`:

- `Hexalith.Projects.Contracts`
- `Hexalith.Projects`
- `Hexalith.Projects.Client`
- `Hexalith.Projects.Testing`
- `Hexalith.Projects.ServiceDefaults`

Do not grant a new-package scope. A future package must first be added to
`tools/release-packages.json`, the independent count gate, this policy, and the
runbook in one reviewed change.

## Resolve and record the release version

Run the locked Semantic Release graph from the exact green `main` SHA before
enablement. The dry run must resolve one version; no-release output is not an
activation candidate. Store the preview, source SHA, and resolved version in
the durable repository evidence directory:

```bash
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

1. With publication still frozen, merge the migration and wait for successful
   push CI on the current `main` SHA.
2. Dispatch **Release** from that exact `main` tip and approve `production`.
   The package-only restore and Release build must complete, the freeze notice
   must say that NuGet login and Semantic Release were skipped, and the run must
   finish successfully without requesting an OIDC credential.
3. Wait until a reviewed, releasable Conventional Commit exists on a new green
   `main` tip. Complete the dry-run/version record above and confirm the intended
   version is absent for all five package IDs.
4. Prove no older Release is queued, waiting, pending, requested, or running.
   Preserve the query result, require an empty array, and only then enable:

   ```bash
   ACTIVE_RELEASES="$(gh run list --repo Hexalith/Hexalith.Projects --workflow release.yml --limit 100 --json status,url --jq '[.[] | select(.status == "queued" or .status == "in_progress" or .status == "waiting" or .status == "pending" or .status == "requested")]')"
   printf '%s\n' "$ACTIVE_RELEASES" | tee "$EVIDENCE_DIR/active-release-runs.json"
   test "$(printf '%s' "$ACTIVE_RELEASES" | jq 'length')" -eq 0
   gh variable set HEXALITH_RELEASE_PUBLISH_ENABLED --repo Hexalith/Hexalith.Projects --body true
   ```

5. Dispatch **Release** once from the exact green tip and approve `production`.
6. In the job log, confirm source revalidation succeeded before `NuGet/login`,
   the login step succeeded without displaying its output, and Semantic Release
   published exactly five packages. Never print, copy, download, or persist the
   temporary key.
7. After the run reaches a terminal state, immediately refreeze, including after
   failure:

   ```bash
   gh variable set HEXALITH_RELEASE_PUBLISH_ENABLED --repo Hexalith/Hexalith.Projects --body false
   ```

8. Record and verify the exact GitHub release and all five public packages. Set
   `RELEASE_RUN_ID` to the completed run ID, then run:

   ```bash
   export RELEASE_RUN_ID='<completed-run-id>'
   gh run view "$RELEASE_RUN_ID" --repo Hexalith/Hexalith.Projects --json databaseId,headSha,status,conclusion,url >"$EVIDENCE_DIR/release-run.json"
   jq -e --arg sha "$RELEASE_SHA" '.headSha == $sha and .status == "completed" and .conclusion == "success"' "$EVIDENCE_DIR/release-run.json"
   gh release view "v${RELEASE_VERSION}" --repo Hexalith/Hexalith.Projects --json tagName,url,assets >"$EVIDENCE_DIR/github-release.json"

   export PACKAGE_DIRECTORY="$EVIDENCE_DIR/packages"
   mkdir -p "$PACKAGE_DIRECTORY"
   PACKAGE_IDS=(
     Hexalith.Projects.Contracts
     Hexalith.Projects
     Hexalith.Projects.Client
     Hexalith.Projects.Testing
     Hexalith.Projects.ServiceDefaults
   )
   for PACKAGE_ID in "${PACKAGE_IDS[@]}"; do
     LOWER_ID="$(printf '%s' "$PACKAGE_ID" | tr '[:upper:]' '[:lower:]')"
     INDEX_PATH="$EVIDENCE_DIR/${PACKAGE_ID}.index.json"
     curl -fsS "https://api.nuget.org/v3-flatcontainer/${LOWER_ID}/index.json" -o "$INDEX_PATH"
     jq -e --arg version "$RELEASE_VERSION" '.versions | index($version) != null' "$INDEX_PATH"
     curl -fsS "https://api.nuget.org/v3-flatcontainer/${LOWER_ID}/${RELEASE_VERSION}/${LOWER_ID}.${RELEASE_VERSION}.nupkg" -o "$PACKAGE_DIRECTORY/${PACKAGE_ID}.${RELEASE_VERSION}.nupkg"
   done
   test "$(find "$PACKAGE_DIRECTORY" -maxdepth 1 -name '*.nupkg' | wc -l)" -eq 5
   pwsh ./tests/tools/run-package-dependency-gate.ps1 -Version "$RELEASE_VERSION" -PackageDirectory "$PACKAGE_DIRECTORY" -SkipPack | tee "$EVIDENCE_DIR/package-manifest-validation.log"
   for PACKAGE in "$PACKAGE_DIRECTORY"/*.nupkg; do
     dotnet nuget verify --all "$PACKAGE" | tee -a "$EVIDENCE_DIR/nuget-signature-verification.log"
   done
   ```

9. Reopen the policy in NuGet.org. It must be active with no ownership warning.
   If NuGet.org created it in a temporary activation window, confirm the
   successful publish changed it to **permanently active** before retiring the
   legacy key.

Commit or attach the complete `$EVIDENCE_DIR` through the repository's normal
reviewed evidence process. It contains the workflow run URL/SHA, predicted and
published version, five NuGet indexes and packages, manifest/signature results,
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
- **Failure after publication begins:** preserve the run log and query all five
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
release succeeded, all five packages and the GitHub release were verified, the
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
