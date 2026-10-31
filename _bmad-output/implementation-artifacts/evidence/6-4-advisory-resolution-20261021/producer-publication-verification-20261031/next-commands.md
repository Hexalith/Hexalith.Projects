# Nonpublishing verification commands

Publication already succeeded. Do not dispatch or rerun `build-release.yml`, run semantic-release, push packages, or reuse 4.39.0 for corrected source. These commands only read official state or verify inside a new disposable directory. Platform adoption and pin changes require separate work.

## Reproduce the retained publication verification

Run from the Projects workspace. The fetch and detached checkout below affect only the new command-owned temporary repository. They do not use a worktree or initialize any submodule.

```bash
set -euo pipefail
projects_root=/home/administrator/projects/hexalith/projects
packet="$projects_root/_bmad-output/implementation-artifacts/evidence/6-4-advisory-resolution-20261021/producer-publication-verification-20261031"
scratch=$(mktemp -d /tmp/story64-publication-recheck.XXXXXXXX)
git init --quiet "$scratch/source"
git -C "$scratch/source" -c fetch.recurseSubmodules=false fetch --no-tags --no-recurse-submodules \
  https://github.com/Hexalith/Hexalith.Builds.git 2186b9a4bc507a116a457c590c80945c2e232cbd
git -C "$scratch/source" -c submodule.recurse=false checkout --detach 2186b9a4bc507a116a457c590c80945c2e232cbd
test "$(git -C "$scratch/source" rev-parse HEAD)" = 2186b9a4bc507a116a457c590c80945c2e232cbd
python3 "$packet/verify-publication-reviewed.py" --source "$scratch/source" --output "$scratch/publication-verification.json"
python3 "$packet/verification-controls-final.py" --source "$scratch/source" > "$scratch/verification-controls.json"
```

The verifier checks retained official observations, exact signed and symbol bytes, the exact unsigned pre-signing digest without writing an unsigned package, source/fixture identities, the evaluated catalog and all three Tooling copies. It refuses existing report outputs. The seventeen controls mutate only disposable synthetic copies. The original fixture enumeration omits one hidden manual template; the report states that limit explicitly. Raw NuGet registration responses retain HTTP gzip encoding; the verifier decompresses them in memory.

## Refresh current official observations

These reads are independent of the retained observation times. A later main or release must not replace the selected source in the existing binding.

```bash
set -euo pipefail
fresh=$(mktemp -d /tmp/story64-official-refresh.XXXXXXXX)
gh api repos/Hexalith/Hexalith.Builds/actions/runs/41172847732 > "$fresh/release-run.json"
gh api repos/Hexalith/Hexalith.Builds/git/ref/tags/v4.39.0 > "$fresh/tag.json"
gh api repos/Hexalith/Hexalith.Builds/releases/tags/v4.39.0 > "$fresh/release.json"
gh api repos/Hexalith/Hexalith.Builds/actions/runs/41171553609 > "$fresh/ci.json"
gh api repos/Hexalith/Hexalith.Builds/git/ref/heads/main > "$fresh/main.json"
curl --fail --location --silent --show-error \
  https://github.com/Hexalith/Hexalith.Builds/releases/download/v4.39.0/g4-tool-package-inventory.json \
  --output "$fresh/g4-tool-package-inventory.json"
printf '%s  %s\n' f07332c8c005bfa91896034933fd556ae0dfef47c3f759f712f797251c8bf555 "$fresh/g4-tool-package-inventory.json" | sha256sum --check
for package_id in hexalith.builds.module.cli hexalith.builds.evidence.cli; do
  curl --fail --location --silent --show-error \
    "https://api.nuget.org/v3-flatcontainer/$package_id/4.39.0/$package_id.4.39.0.nupkg" \
    --output "$fresh/$package_id.4.39.0.nupkg"
  curl --fail --location --silent --show-error \
    "https://www.nuget.org/api/v2/symbolpackage/$package_id/4.39.0" \
    --output "$fresh/$package_id.4.39.0.snupkg"
  dotnet nuget verify --all "$fresh/$package_id.4.39.0.nupkg"
done
printf '%s  %s\n' \
  111d621a2b4e659ab21b32c340717def3bf7c9e5757b728f8679785396aca192 "$fresh/hexalith.builds.module.cli.4.39.0.nupkg" \
  79d6ffe29718fa7adccb6a49311cbc39ebab558e718e4a7f95c761915d2416f2 "$fresh/hexalith.builds.module.cli.4.39.0.snupkg" \
  8b7d1de983b4f5abf4b05e68e4623766446930c6050976263efa0ea5046c1c93 "$fresh/hexalith.builds.evidence.cli.4.39.0.nupkg" \
  a656d52f286651527a391c24ceef3f4b5cac29268a2c3b80371f5f14893faa70 "$fresh/hexalith.builds.evidence.cli.4.39.0.snupkg" \
  | sha256sum --check
```

## Repeat the existing full local qualification

Use the same `$scratch` and `$packet` from the first block. This is an unsigned, nonpublishing local qualification; its output cannot replace any official artifact binding. It runs the existing source suites, repository fixtures, builder and positive/negative controls without skip flags. Fixture Git operations remain in disposable temporary repositories. The NuGet configuration is installed only under a new child-process HOME.

```bash
set -euo pipefail
test -n "${scratch:-}"
test -d "$scratch/source/.git"
test -n "${packet:-}"
qualifier_home=$(mktemp -d /var/tmp/story64-producer-home.XXXXXXXX)
mkdir -p "$qualifier_home/.nuget/NuGet" "$qualifier_home/dotnet-home"
cp "$scratch/source/test/fixtures/package-consumer/NuGet.Config" "$qualifier_home/.nuget/NuGet/NuGet.Config"
(
  cd "$scratch/source"
  env -u NUGET_PACKAGES HOME="$qualifier_home" DOTNET_CLI_HOME="$qualifier_home/dotnet-home" \
    GIT_OPTIONAL_LOCKS=0 \
    pwsh -NoProfile -File ./Tools/test-g4-tool-package-contracts.ps1 \
    -Version 4.39.0 -RequireControls -RetainPackageDirectory
) > "$scratch/full-qualification.stdout.log" 2> "$scratch/full-qualification.stderr.log"
```

The isolated HOME uses `/var/tmp` because an existing per-user-directory test correctly refuses defaults underneath `Path.GetTempPath()` (`/tmp` here). Leave `NUGET_PACKAGES` unset so fixture-specific NuGet settings can take effect. These environment corrections preserve the original failed attempt and do not change tests or guards.

Do not add `-SkipSourceValidation`, use a substitute builder, initialize real nested submodules, or alter guards when a check fails. Preserve the failure and follow the focused command documented in [local results](local/README.md). The applicability assessment and prepared future-source corrections have their own [security commands](security/README.md). An existing source's publication is a historical fact; future correction recording, analysis, CI and publication identity remain separately gated.
