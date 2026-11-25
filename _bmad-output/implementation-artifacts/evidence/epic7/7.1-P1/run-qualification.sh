#!/usr/bin/env bash
# 7.1-P1 creation-contract qualification
# (_bmad-output/implementation-artifacts/spec-7-1-p1-qualify-projects-creation-contracts.md).
#
#   run-qualification.sh pin <sha>
#     Clean local clone at <sha>, root submodules initialized from the local stores without
#     recursion, isolated NUGET_PACKAGES. CI=true package-mode builds of Contracts.Tests (full),
#     Client.Tests (ClientGenerationTests) and Hexalith.Projects.Tests (full); Debug source-mode
#     build of Server.Tests for CreateProjectEndpointTests, ProposeNewProjectEndpointTests and
#     ProjectsDomainProcessorTests (package mode hits the DW-63 CS0246 blocker).
#
#   run-qualification.sh rollback <sha> [<rollback-commit>...]
#     Fresh clean clone at <sha>, one git revert --no-edit of the rollback commits newest first
#     (default: rollback_commits below; explicit commits override it), then the existing create
#     suites with -trait- Package=7.1-P1, the recomputed compatibility fingerprint and a negative
#     control: the Package=7.1-P1 tests must fail exactly on negative_control_expected.
#
# Native xUnit XML, commands.json and summary.json are written to <script dir>/<mode>/.
# Build and test logs stay under the work root (QUALIFICATION_WORK_ROOT, default: a new temp dir).
set -euo pipefail

usage() {
  echo "usage: $0 pin <sha> | rollback <sha> [<rollback-commit>...]" >&2
  exit 2
}

mode=${1:-}
requested=${2:-}
[ "$mode" = pin ] || [ "$mode" = rollback ] || usage
[ -n "$requested" ] || usage

script_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
repo=$(git -C "$script_dir" rev-parse --show-toplevel)
sha=$(git -C "$repo" rev-parse --verify "$requested^{commit}")
out_dir="$script_dir/$mode"
if [ -e "$out_dir" ]; then
  echo "output directory already exists: $out_dir (move it away to keep its receipts, then re-run)" >&2
  exit 2
fi
work=${QUALIFICATION_WORK_ROOT:-$(mktemp -d "${TMPDIR:-/tmp}/p71-qualification.XXXXXX")}
mkdir -p "$work"
clone="$work/clone"
logs="$work/logs"
[ ! -e "$clone" ] || { echo "work root already holds a clone: $clone" >&2; exit 2; }

expected_fingerprint=90feea73d2df1247c40a5aa624245abddc8c66447fabf8ac5df61bfc12d88684
fingerprint_files=(
  src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs
  src/Hexalith.Projects.Client/Generated/HexalithProjectsIdempotencyHelpers.g.cs
  src/Hexalith.Projects.Contracts/Commands/CreateProject.cs
  src/Hexalith.Projects.Contracts/Events/ProjectCreated.cs
  src/Hexalith.Projects.Contracts/Events/ProjectCreationRejected.cs
  src/Hexalith.Projects.Contracts/Events/ProjectFolderCreationPending.cs
  src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml
  src/Hexalith.Projects.Server/CreateProjectPayload.cs
  src/Hexalith.Projects.Server/EventStoreProjectCommandSubmitter.cs
  src/Hexalith.Projects.Server/Queries/ProposeNewProjectEndpoint.cs
  src/Hexalith.Projects.Server/SensitiveMetadataTierValidator.cs
  src/Hexalith.Projects/Aggregates/Project/ProjectAggregate.cs
  src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidator.cs
)
# The 7.1-P1 rollback unit, listed oldest first; the drill reverts them newest first.
rollback_commits=(
  a101e7cdae6918602b9d771c4fe4a18a96ebe551
  b738fc5b1709a65982f8632534561ade459a3290
)
# Fixture cases that must fail, and only these, when the rollback commits are reverted.
negative_control_expected=(
  class-non-string-number
  class-non-string-boolean
  class-non-string-object
  class-non-string-array
  class-non-string-case-variant-key
  class-duplicate-invalid-then-valid
  class-duplicate-same-value
  class-duplicate-case-variant-key
  versioned-name-only
  versioned-null-metadata
  versioned-version-only
  duplicate-legacy-name
  duplicate-request-schema-version
  duplicate-project-metadata
  duplicate-display-name
)
server_classes=(
  Hexalith.Projects.Server.Tests.CreateProjectEndpointTests
  Hexalith.Projects.Server.Tests.Queries.ProposeNewProjectEndpointTests
  Hexalith.Projects.Server.Tests.ProjectsDomainProcessorTests
)

export NUGET_PACKAGES="$work/nuget-packages"
export NUGET_HTTP_CACHE_PATH="$work/nuget-http-cache"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 MSBUILDDISABLENODEREUSE=1
unset CI

mkdir -p "$out_dir" "$logs"
commands_json="$out_dir/commands.json"
echo '[]' >"$commands_json"

# run <label> <cwd> <command...>: runs one command, logs it under $logs and records it in commands.json.
run() {
  local label=$1 cwd=$2 status started finished
  shift 2
  started=$(date -u +%Y-%m-%dT%H:%M:%SZ)
  set +e
  (cd "$cwd" && "$@") >"$logs/$label.log" 2>&1
  status=$?
  set -e
  finished=$(date -u +%Y-%m-%dT%H:%M:%SZ)
  python3 - "$commands_json" "$work" "$repo" "$label" "$cwd" "$status" "$started" "$finished" "$@" <<'PY'
import json
import sys

path, work, repo, label, cwd, status, started, finished, *command = sys.argv[1:]

def portable(value):
    return value.replace(work, "<work>").replace(repo, "<repo>")

data = json.load(open(path))
data.append({
    "purpose": label,
    "cwd": portable(cwd),
    "command": [portable(item) for item in command],
    "exitCode": int(status),
    "startedUtc": started,
    "finishedUtc": finished,
})
open(path, "w").write(json.dumps(data, indent=2) + "\n")
PY
  if [ "$status" -ne 0 ]; then
    echo "FAILED: $label (exit $status); log: $logs/$label.log" >&2
    tail -n 80 "$logs/$label.log" >&2
    exit "$status"
  fi
  echo "ok: $label"
}

# Clean local clone with root submodules only, initialized from the local stores (no network,
# no recursion, unpushed gitlinks resolve locally).
run clone "$work" git clone --no-checkout --quiet "$repo" "$clone"
run checkout "$clone" git -c advice.detachedHead=false checkout --quiet --detach "$sha"
run submodule-init "$clone" git submodule init
submodule_paths=()
while read -r key path; do
  name=${key#submodule.}
  name=${name%.path}
  git -C "$clone" config "submodule.$name.url" "$repo/$path"
  submodule_paths+=("$path")
done < <(git -C "$clone" config -f .gitmodules --get-regexp '^submodule\..*\.path$')
run submodule-update "$clone" git -c protocol.file.allow=always submodule update --quiet -- "${submodule_paths[@]}"

reverted=()
if [ "$mode" = rollback ]; then
  [ "$#" -lt 3 ] || rollback_commits=("${@:3}")
  for ((index = ${#rollback_commits[@]} - 1; index >= 0; index--)); do
    reverted+=("$(git -C "$repo" rev-parse --verify "${rollback_commits[index]}^{commit}")")
  done
  run revert "$clone" git -c user.name="7.1-P1 rollback drill" -c user.email="rollback-drill@invalid" revert --no-edit "${reverted[@]}"
fi

[ -z "$(git -C "$clone" status --porcelain --ignore-submodules=none)" ] || { echo "clone is not clean" >&2; exit 1; }
git -C "$clone" submodule status >"$work/submodule-status.txt"
if grep -qv '^ ' "$work/submodule-status.txt"; then
  echo "a root submodule does not match its gitlink" >&2
  cat "$work/submodule-status.txt" >&2
  exit 1
fi
for path in "${submodule_paths[@]}"; do
  if git -C "$clone/$path" submodule status 2>/dev/null | grep -qv '^-'; then
    echo "nested submodule initialized under $path" >&2
    exit 1
  fi
done
git -C "$clone" ls-tree HEAD -- "${fingerprint_files[@]}" >"$work/fingerprint-tree.txt"
fingerprint=$(sha256sum <"$work/fingerprint-tree.txt" | cut -d' ' -f1)
(cd "$clone" && dotnet --version) >"$work/dotnet-version.txt"

# package_suite <label> <project> [runner filters...]: CI=true package-mode Release build and native run.
package_suite() {
  local label=$1 project=$2
  shift 2
  run "$label-build" "$clone" env CI=true dotnet build "tests/$project/$project.csproj" \
    --configuration Release -m:1 --disable-build-servers -warnaserror -v:m --artifacts-path "$clone/artifacts/$label"
  run "$label-tests" "$clone" env CI=true dotnet "$clone/artifacts/$label/bin/$project/release/$project.dll" \
    -xml "$out_dir/$label.xml" -noColor -noLogo "$@"
}

server_filters=()
for class in "${server_classes[@]}"; do
  server_filters+=(-class "$class")
done

if [ "$mode" = pin ]; then
  package_suite Contracts Hexalith.Projects.Contracts.Tests
  package_suite Client Hexalith.Projects.Client.Tests -class Hexalith.Projects.Client.Tests.ClientGenerationTests
  package_suite Domain Hexalith.Projects.Tests
else
  server_filters+=(-trait- "Package=7.1-P1")
fi

# Debug source mode for Server.Tests, mapped by the Builds module CLI.
run builds-cli-build "$clone" dotnet build references/Hexalith.Builds/src/libraries/Hexalith.Builds.Module.Cli/Hexalith.Builds.Module.Cli.csproj \
  --configuration Debug -m:1 --disable-build-servers --artifacts-path "$work/builds-cli"
run source-mapping "$clone" dotnet "$work/builds-cli/bin/Hexalith.Builds.Module.Cli/debug/Hexalith.Builds.Module.Cli.dll" \
  prepare-build --manifest module/hexalith.module-manifest.v2.json --mode source --workspace "$work/prepared" --output json
mapfile -t mapping_env < <(python3 - "$logs/source-mapping.log" <<'PY'
import json
import sys

text = open(sys.argv[1]).read()
mapping = json.loads(text[text.index("{"):])
if mapping.get("status") != "prepared":
    raise SystemExit("source mapping was not prepared")
for key, value in mapping["buildPreparation"]["environment"].items():
    print(f"{key}={value}")
PY
)
run Server-build "$clone" env "${mapping_env[@]}" dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj \
  --configuration Debug -m:1 --disable-build-servers -warnaserror -v:m --artifacts-path "$clone/artifacts/server"
run Server-tests "$clone" env "${mapping_env[@]}" dotnet "$clone/artifacts/server/bin/Hexalith.Projects.Server.Tests/debug/Hexalith.Projects.Server.Tests.dll" \
  -xml "$out_dir/Server.xml" -noColor -noLogo "${server_filters[@]}"
if [ "$mode" = rollback ]; then
  # Expected to fail on the reverted tree; -ignoreFailures keeps the runner exit code for real errors.
  run negative-control "$clone" env "${mapping_env[@]}" dotnet "$clone/artifacts/server/bin/Hexalith.Projects.Server.Tests/debug/Hexalith.Projects.Server.Tests.dll" \
    -xml "$out_dir/negative-control.xml" -noColor -noLogo -ignoreFailures -trait "Package=7.1-P1"
fi

python3 - "$out_dir" "$mode" "$sha" "$(git -C "$clone" rev-parse HEAD)" "${reverted[*]}" "$fingerprint" "$expected_fingerprint" \
  "$work" "$(cat "$work/dotnet-version.txt")" "$work/fingerprint-tree.txt" "$work/submodule-status.txt" \
  "${negative_control_expected[*]}" <<'PY'
import datetime
import hashlib
import json
import pathlib
import re
import sys
import xml.etree.ElementTree as ElementTree

(out_dir, mode, sha, head, reverted, fingerprint, expected, work, dotnet_version,
 fingerprint_tree, submodule_status, negative_expected) = sys.argv[1:]
out = pathlib.Path(out_dir)
suites = []
failures = []
negative_control = None
if mode == "rollback":
    xml = out / "negative-control.xml"
    data = xml.read_bytes()
    tests = ElementTree.fromstring(data).iter("test")
    results = [(test.get("name", ""), test.get("result")) for test in tests]
    failed = [name for name, result in results if result != "Pass"]
    failed_cases = []
    for name in failed:
        match = re.search(r'caseId: \\?"([^"\\]+)', name)
        failed_cases.append(match.group(1) if match and "CreationContract_FixtureCase_" in name else name)
    expected_cases = negative_expected.split()
    negative_control = {
        "xml": xml.name,
        "sha256": hashlib.sha256(data).hexdigest(),
        "total": len(results),
        "passed": len(results) - len(failed),
        "failed": len(failed),
        "failedCases": failed_cases,
        "expectedFailedCases": expected_cases,
        "matches": sorted(failed_cases) == sorted(expected_cases),
    }
    if not negative_control["matches"]:
        failures.append("negative-control")
for xml in sorted(out.glob("*.xml")):
    if xml.name == "negative-control.xml":
        continue
    data = xml.read_bytes()
    assemblies = ElementTree.fromstring(data).findall("assembly")
    counts = {key: sum(int(item.get(key, "0")) for item in assemblies)
              for key in ("total", "passed", "failed", "skipped", "errors", "not-run")}
    suites.append({"suite": xml.stem, "xml": xml.name, "sha256": hashlib.sha256(data).hexdigest(), **counts})
    if counts["total"] == 0 or counts["failed"] or counts["skipped"] or counts["errors"] or counts["not-run"]:
        failures.append(xml.stem)
summary = {
    "schema": "hexalith.projects.7-1-p1-qualification-run.v1",
    "mode": mode,
    "requestedRevision": sha,
    "checkedOutHead": head,
    "rollbackCommits": reverted.split(),
    "fingerprint": {
        "command": "git ls-tree <rev> -- <13 files> | sha256sum",
        "value": fingerprint,
        "expected": expected,
        "matches": fingerprint == expected,
        "tree": pathlib.Path(fingerprint_tree).read_text().splitlines(),
    },
    "rootSubmodules": pathlib.Path(submodule_status).read_text().splitlines(),
    "nestedSubmodulesInitialized": False,
    "isolatedNugetPackages": True,
    "dotnetSdk": dotnet_version,
    "suites": suites,
    "negativeControl": negative_control,
    "completedUtc": datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
}
(out / "summary.json").write_text(json.dumps(summary, indent=2) + "\n")
print(json.dumps({"fingerprint": fingerprint, "suites": suites}, indent=2))
if failures or fingerprint != expected:
    raise SystemExit(f"qualification failed: suites={failures} fingerprintMatches={fingerprint == expected}")
PY
echo "work root: $work"
