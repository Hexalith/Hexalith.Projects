# Required producer publication sequence

These are commands for a later, separately approved publication. No command in this document authorizes dispatch, environment approval, tagging or publishing during preparation. The final README and decision manifest determine whether prerequisites passed; command availability alone is insufficient.

Approved-source proposal for review: `2186b9a4bc507a116a457c590c80945c2e232cbd`, tree `18d735e56eeb17332e2d2b57a6b116097f42fdec`, source catalog blob `37148298dfd3a4ae35d89dd5f4677e06e89149ad`, catalog SHA-256 `986b463e12daaf1818c261c8d509f38b9803bc2f9f328c48d591d3f0caec8e6a`. Real analyzer proposal is **4.39.0**, minor, from official `v4.38.3` / `d1931b1e803144048d927fff16cb8119afde541e`. A future run must retain these exact identities or stop for a new package of evidence and approval.

## Nonpublishing prerequisites

Each Bash block must run as one complete script with its shown fail-closed shell options; a failed prerequisite stops that block. From a clean detached checkout of the approved source, with the selected lockfile, Node 24, .NET SDK 10.0.403 and PowerShell 7.6:

```bash
set -euo pipefail
test "$(git rev-parse HEAD)" = 2186b9a4bc507a116a457c590c80945c2e232cbd
test -z "$(git status --porcelain)"
npm ci --ignore-scripts
npm audit signatures
```

First refresh official source/tag/feed observations independently and compare them with the retained capsule. Any changed tag/source/configuration or previous-release identity requires a new analysis and approval package; do not edit the frozen capsule. The separately hardened [analysis-only replay](analyzer/analyze-release-reviewed.mjs) replays this immutable recorded proposal into a new absent output directory and validates committed inputs plus seven retained module fingerprints. It does not refresh the official observations. Preserve the original executed program, inputs and result; never overwrite them. Stop if main, the selected source/configuration, previous release or computed version differs from this package. Verify `v4.39.0`, its GitHub release and both NuGet destinations remain unused; an external publication requires reconciliation, never a second publication of the same identity.

The separate npm vulnerability audit currently exits 1 with 38 vulnerable dependency entries, including bundled undici 6.29.0. **Stop before seeking publication approval until a bounded remediation or documented applicability assessment resolves this retained security observation.** Preserve the original audit and bind any assessment/remediation to the exact source/lock and advisories. Signature verification alone cannot close this blocker; this instruction grants no vulnerability-policy exception. Any dependency correction changes the source/lock and requires a newly recorded existing SHA, analysis and qualification.

Run the full existing producer qualifier without skipped source validation, substitute builders or external fixtures. The directory must not exist before the command:

```bash
set -euo pipefail
pwsh -NoProfile -File ./Tools/test-g4-tool-package-contracts.ps1 \
  -Version 4.39.0 \
  -PackageDirectory artifacts/g4-tool-packages/4.39.0-approval \
  -RetainPackageDirectory -RequireControls
```

Require exit 0, `qualification.releaseEligible: true`, `qualification.sourceTree.clean: true`, exact source revision, both tool package/symbol pairs, all tracked fixture byte proofs, embedded-versus-evaluated runtime catalog proof and packaged seed/subscription controls. Validate through the producer's actual `Assert-ReleaseEligibleInventory` helper; keep raw evidence and digests. Any diagnostic-only or CI inventory with skipped source validation remains ineligible. Local package hashes are unpublished observations and cannot fill official publication fields.

The local original builder produced four archives, then failed its loader fixture under Projects' inherited CA1062 policy; no inventory was emitted. The original immutable loader passes in its isolated `/tmp` context against those same archives. The prepared [two-null-guard patch](qualification/corrections/loader-parameter-guards.patch) separately passes the stricter context. This is a context-specific fixture correction, not proof that official default paths fail. It remains unapplied to the original Builds checkout. If adopted, record its actual new source commit and redo source/CI/analyzer qualification; never label the dirty corrected tree as the selected SHA or guess its future revision.

The separately hardened integrity checker binds the original implementation manifest, approved analysis digest, verified inputs, receipt streams, local archives, security counts and blocked state. An integrity pass does **not** grant qualification credit:

```bash
set -euo pipefail
package_root=/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/evidence/6-4-advisory-resolution-20261021/producer-publication-preparation-20261031
python3 "$package_root/qualification/verify-receipts-reviewed.py" "$package_root" \
  --manifest-sha256 e2f627c35e5a27b98b0f82da4f83594b203ae3ca47a1169552eb9cfd17cc1fb6
```

## Exact source and existing push CI

```bash
set -euo pipefail
approved_source=2186b9a4bc507a116a457c590c80945c2e232cbd
test "$(gh api repos/Hexalith/Hexalith.Builds/git/ref/heads/main --jq .object.sha)" = "$approved_source"
REPOSITORY=Hexalith/Hexalith.Builds \
DISPATCH_REF=refs/heads/main DISPATCH_SHA="$approved_source" \
  bash ./Tools/verify-g4-release-source.sh
gh run view 41171553609 --repo Hexalith/Hexalith.Builds \
  --json headSha,event,status,conclusion,jobs
```

Require completed successful exact-source **push** CI. If an existing nonpublishing CI attempt must be rerun, first verify its path is `.github/workflows/ci.yml`, its source is exactly approved and its workflow body remains nonpublishing, then use `gh run rerun 41171553609 --repo Hexalith/Hexalith.Builds`. A new source SHA requires a new qualification and approval package. Do not rerun or dispatch `build-release.yml` as a CI workaround.

## Protected dispatch, only after separate publication approval

An independently initiated run **41172847732** for this exact source was observed in progress at 2026-10-31T05:31Z. This session did not dispatch or approve it. Inspect that existing run first and retain its outcome; **do not dispatch a duplicate** while it is active or after it has published. A completed or failed independent run requires fresh destination/source observations before deciding whether a later dispatch is appropriate.

The workflow accepts a branch and has no version or expected-SHA input. Recheck live main immediately before this command:

```bash
set -euo pipefail
test "$(gh api repos/Hexalith/Hexalith.Builds/git/ref/heads/main --jq .object.sha)" = 2186b9a4bc507a116a457c590c80945c2e232cbd
test "$(gh run list --repo Hexalith/Hexalith.Builds --workflow build-release.yml --limit 100 --json status --jq '[.[] | select(.status != "completed")] | length == 0')" = true
gh workflow run build-release.yml --repo Hexalith/Hexalith.Builds --ref main
gh run list --repo Hexalith/Hexalith.Builds --workflow build-release.yml \
  --event workflow_dispatch --limit 10 \
  --json databaseId,headSha,createdAt,status,conclusion
```

Identify the newly dispatched run, bind its actual run ID, source and creation time, and inspect its pending `production` environment. **Approve only a run whose `headSha` is exactly `2186b9a4bc507a116a457c590c80945c2e232cbd`.** Branch dispatch is not an atomic expected-SHA operation. If the branch races, leave the different-source run unapproved and obtain a new package and approval; never reinterpret this approval as permission for the new tip. The observed production environment requires reviewer `jpiquot`, disallows admin bypass and admits only branch `main`. Recheck those live protections before relying on this sequence. No automatic environment-approval command is supplied.

The protected workflow reruns source validation before and after environment approval, all catalog/release controls, source/package qualification and `npm audit signatures`. NuGet Trusted Publishing uses `NuGet/login` v1.2.0 and configured `vars.NUGET_USER`; `GITHUB_TOKEN` and the short-lived NuGet key stay in the protected job. Its actual lifecycle command is:

```bash
set -euo pipefail
npx --no-install --ignore-scripts semantic-release
```

Do not run that lifecycle locally during preparation. Semantic-release computes the version; it calls `Tools/verify-g4-tool-release.ps1 -Version ${nextRelease.version}` before tagging and `Tools/publish-g4-tool-packages.ps1 -Version ${nextRelease.version}` for publication. Require the observed next version to remain **4.39.0**. The publisher independently validates exact source, complete eligible inventory/evidence, package/symbol identities and bytes, then verifies feed payload equivalence.

## Post-publication verification and downstream boundary

Bind official successful release run/attempt/job, `v4.39.0` tag target, GitHub release and its actual qualification-inventory asset/digest. Verify both actual signed NuGet packages and both symbol artifacts, NuSpec source commit, archive sizes/digests, feed hashes, repository signatures, embedded runtime catalog and extracted Tooling/CLI/catalog identities. Official signed archives may differ from local unsigned archives; compare the producer's signed-payload equivalence and actual contents rather than asserting byte equality of signed and unsigned ZIPs.

Only after that verification may a separately authorized Platform correction bind this producer's exact release identities. Existing Platform binding and gitlink preparation still names the earlier producer/catalog, so requalify it for the newly selected catalog; do not auto-adopt this SHA, copy diagnostic hashes into official fields, overwrite the earlier binding or reuse historical Platform CI. Platform recording/publication and Projects consumer adoption remain separately authorized steps. Story 6.4 remains in progress until its independent acceptance gates pass.
