#!/usr/bin/env bash
# Source this file from Bash to prepare a Projects checkout for local Aspire commands.

projects_reference_roots() {
    local repository_root="$1" module_name
    export HexalithCommonsRoot="${HexalithCommonsRoot:-$repository_root/references/Hexalith.Commons}"
    for module_name in EventStore Tenants Folders Conversations Memories FrontComposer Parties McpCli Builds Platform; do
        export "Hexalith${module_name}Root=$repository_root/references/Hexalith.$module_name"
    done
    export HexalithTenantsBasePath="$repository_root/references/Hexalith.Tenants/src"
}

projects_prepare_source() {
    local repository_root="$1" workspace="$2" mapping_assignment
    local mapping_file="$workspace/preparation.json"
    local mapper="$repository_root/references/Hexalith.Builds/src/libraries/Hexalith.Builds.Module.Cli/Hexalith.Builds.Module.Cli.csproj"
    projects_reference_roots "$repository_root"
    unset CustomBeforeDirectoryBuildProps CustomAfterMicrosoftCommonProps CustomBeforeDirectoryBuildTargets
    unset CustomAfterDirectoryBuildTargets AfterMicrosoftNETSdkTargets HEXALITH_SOURCE_MAPPING_HASH
    export Configuration=Debug UseHexalithProjectReferences=true UseNuGetDeps=false

    # Keep compiler and restore messages out of the producer's JSON.
    dotnet build "$mapper" --configuration Debug --nologo >&2 || return 1
    if ! (cd "$repository_root" && dotnet run --no-build --project "$mapper" -- prepare-build \
        --manifest module/hexalith.module-manifest.v2.json --mode source --workspace "$workspace/prepared" \
        --output json) >"$mapping_file"; then
        jq '.diagnostics[]? | {ruleId, phase, category, message}' "$mapping_file" >&2
        return 1
    fi
    jq -e '.status == "prepared" and .buildPreparation.environment.Configuration == "Debug" and
        (.buildPreparation.environment | keys | sort) ==
        (["AfterMicrosoftNETSdkTargets", "Configuration", "CustomAfterDirectoryBuildTargets",
          "CustomAfterMicrosoftCommonProps", "CustomBeforeDirectoryBuildProps",
          "CustomBeforeDirectoryBuildTargets", "HEXALITH_SOURCE_MAPPING_HASH"] | sort) and
        all(.buildPreparation.environment[]; type == "string" and all(explode[]; . >= 32))' \
        "$mapping_file" >/dev/null || { echo 'Projects source preparation did not return a valid Debug mapping.' >&2; return 1; }
    while IFS= read -r -d '' mapping_assignment; do
        export "$mapping_assignment"
    done < <(jq -j '.buildPreparation.environment | to_entries[] | .key + "=" + .value + "\u0000"' "$mapping_file")
}
