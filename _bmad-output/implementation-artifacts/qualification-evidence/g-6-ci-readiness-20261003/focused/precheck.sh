set -euo pipefail
qualification_output=.g6-current-evidence/ci-readiness-local-20261003-3
mkdir -p "$qualification_output"
export PYTHONDONTWRITEBYTECODE=1
export DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1
export MSBUILDDISABLENODEREUSE=1
qualification_nuget="$(mktemp -d /var/tmp/hexalith-g6-precheck-nuget-XXXXXX)"
export NUGET_PACKAGES="$qualification_nuget"
trap 'rm -rf -- "$qualification_nuget"' EXIT
docker pull postgres@sha256:a02db8cac496f15b094798a38254f14d6e00741f709360e5e00bb6668ea31636 > "$qualification_output/postgresql-pull.log" 2>&1
while IFS= read -r project; do
  env -u CI dotnet restore "$project" -p:UseHexalithProjectReferences=true -m:1 >> "$qualification_output/restore.log" 2>&1
done < <(jq -r '.resolvedProjects[]' references/Hexalith.Builds/Tools/g6-current-policy.json)
python3 tools/qualification/run_g6_current.py --policy references/Hexalith.Builds/Tools/g6-current-policy.json --output "$qualification_output" > "$qualification_output/run.log" 2>&1
python3 references/Hexalith.Builds/Tools/g6_current.py validate --workspace . --policy references/Hexalith.Builds/Tools/g6-current-policy.json --evidence "$qualification_output/result.json" > "$qualification_output/validate.log" 2>&1
cat "$qualification_output/validate.log"
