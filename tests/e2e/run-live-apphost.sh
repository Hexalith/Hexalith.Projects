#!/usr/bin/env bash
# Managed live AppHost E2E lane. This runner is the only lifecycle owner: it enables the explicit
# fixture profile, starts the exact Projects AppHost, waits for every required resource, describes
# the resource graph exactly once, exports the dynamically assigned endpoints, runs the startup/auth
# smoke and the full Chromium suite with two workers and zero skips, and always stops that AppHost.
set -Eeuo pipefail

repository_root=$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)
apphost="$repository_root/src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj"
resource_timeout_seconds="${E2E_RESOURCE_TIMEOUT_SECONDS:-600}"
describe_file=$(mktemp)
start_file=$(mktemp)
source_workspace=$(mktemp -d /tmp/projects-live-fixtures-source-XXXXXXXX)
mapping_file="$source_workspace/preparation.json"
browser_session_file="$repository_root/tests/e2e/.auth/projects-ui-browser-session.json"
started=0

cleanup() {
    result=$?
    if test "$started" = 1; then
        # Exact-AppHost teardown runs on success, failure, and interruption alike.
        if ! aspire stop --apphost "$apphost" --non-interactive >/dev/null 2>&1 && test "$result" = 0; then
            result=1
        fi
        # The browser storage state (UI and Keycloak-origin HttpOnly cookies) never outlives the run that
        # created it; a run that refused to start never owned it and leaves it alone.
        rm -f "$browser_session_file"
    fi
    # The captured graph and start output stay local and are never published as artifacts.
    rm -f "$describe_file" "$start_file"
    # This exact temporary directory was allocated by this invocation only.
    rm -rf -- "$source_workspace"
    exit "$result"
}
trap cleanup EXIT

require_command() {
    command -v "$1" >/dev/null 2>&1 || { echo "[run-live-apphost] $1 is required." >&2; exit 2; }
}

for command_name in aspire dotnet jq npm npx python3; do
    require_command "$command_name"
done

if test "${E2E_SUPPORTED_READS_ONLY:-0}" = 1; then
    cd "$repository_root"
    rm -f "$describe_file" "$start_file"
    rm -rf -- "$source_workspace"
    exec python3 tools/qualification/run_supported_project_reads.py --browser --timeout "$resource_timeout_seconds" \
        --evidence "${E2E_SUPPORTED_EVIDENCE:-/tmp/projects-supported-browser-$(date -u +%Y%m%d%H%M%S)-$$}"
fi

: "${KEYCLOAK_CLIENT_ID:?KEYCLOAK_CLIENT_ID is required}"
: "${TEST_USER_USERNAME:?TEST_USER_USERNAME is required}"
: "${TEST_USER_PASSWORD:?TEST_USER_PASSWORD is required}"
case "$resource_timeout_seconds" in
    '' | *[!0-9]*) echo "[run-live-apphost] E2E_RESOURCE_TIMEOUT_SECONDS must be a positive integer." >&2; exit 2 ;;
esac
if [[ "$resource_timeout_seconds" =~ ^0+$ ]]; then
    echo "[run-live-apphost] E2E_RESOURCE_TIMEOUT_SECONDS must be a positive integer." >&2
    exit 2
fi

run_id="${E2E_RUN_ID:-run-$(date -u +%Y%m%d%H%M%S)-$$}"
if ! [[ "$run_id" =~ ^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$ ]]; then
    echo "[run-live-apphost] E2E_RUN_ID must be 1-64 characters of [A-Za-z0-9._-]." >&2
    exit 2
fi

export Projects__E2E__LiveFixtures=1
export E2E_LIVE_APPHOST=1
export KEYCLOAK_REALM="${KEYCLOAK_REALM:-hexalith}"
# Sibling source builds resolve Commons through the root-declared checkout, never a nested submodule.
export HexalithCommonsRoot="${HexalithCommonsRoot:-$repository_root/references/Hexalith.Commons}"
for module_name in EventStore Tenants Folders Conversations Memories FrontComposer Parties McpCli Builds Platform; do
    export "Hexalith${module_name}Root=$repository_root/references/Hexalith.$module_name"
done
export HexalithTenantsBasePath="$repository_root/references/Hexalith.Tenants/src"
# Ports are always assigned by Aspire and read from the single describe capture below.
unset BASE_URL API_URL EVENTSTORE_API_URL KEYCLOAK_URL FIXTURE_API_URL TEST_TENANT_ID TEST_PRINCIPAL_ID

cd "$repository_root"
if test "${CI:-false}" != true; then
    # Reuse the supported Projects qualifier's Platform-owned source preparation. Local execution
    # always uses Debug and source references; an unavailable source graph remains a prerequisite.
    unset CustomBeforeDirectoryBuildProps CustomAfterMicrosoftCommonProps CustomBeforeDirectoryBuildTargets
    unset CustomAfterDirectoryBuildTargets AfterMicrosoftNETSdkTargets HEXALITH_SOURCE_MAPPING_HASH
    export Configuration=Debug UseHexalithProjectReferences=true UseNuGetDeps=false
    if ! dotnet run --project "$HexalithBuildsRoot/src/libraries/Hexalith.Builds.Module.Cli/Hexalith.Builds.Module.Cli.csproj" \
        -- prepare-build --manifest module/hexalith.module-manifest.v2.json \
        --mode source --workspace "$source_workspace/prepared" --output json >"$mapping_file"; then
        echo '[run-live-apphost] supported Debug source preparation failed.' >&2
        if jq -e . "$mapping_file" >/dev/null 2>&1; then
            jq '{status, outcome, diagnostics: [.diagnostics[]? | {ruleId, phase, category, message}]}' "$mapping_file" >&2
        else
            tail -n 20 "$mapping_file" >&2
        fi
        exit 2
    fi
    jq -e '.status == "prepared" and .buildPreparation.environment.Configuration == "Debug" and
        (.buildPreparation.environment | keys | sort) ==
        (["AfterMicrosoftNETSdkTargets", "Configuration", "CustomAfterDirectoryBuildTargets",
          "CustomAfterMicrosoftCommonProps", "CustomBeforeDirectoryBuildProps",
          "CustomBeforeDirectoryBuildTargets", "HEXALITH_SOURCE_MAPPING_HASH"] | sort) and
        all(.buildPreparation.environment[]; type == "string" and all(explode[]; . >= 32))' \
        "$mapping_file" >/dev/null || { echo '[run-live-apphost] supported Debug source preparation failed.' >&2; exit 2; }
    while IFS= read -r -d '' mapping_assignment; do
        export "$mapping_assignment"
    done < <(jq -j '.buildPreparation.environment | to_entries[] | .key + "=" + .value + "\u0000"' "$mapping_file")
fi
# Never adopt an AppHost this runner did not start: a running instance would be stopped by this run's
# teardown and could mask a broken start. `aspire describe` exits 0 with empty JSON when nothing runs,
# so ownership is decided on the described resources, not the exit status.
if aspire describe --apphost "$apphost" --format Json --non-interactive 2>/dev/null | jq -e '(.resources // []) | length > 0' >/dev/null 2>&1; then
    echo "[run-live-apphost] the Projects AppHost is already running; stop it before starting the managed lane." >&2
    exit 2
fi
# Mark the AppHost as owned before starting it, so a partial start is still stopped.
started=1
aspire start --apphost "$apphost" --non-interactive --format Json >"$start_file"

for resource in security eventstore tenants projects projects-workers projects-ui conversations folders memories live-fixtures eventstore-dapr-cli tenants-dapr-cli projects-dapr-cli projects-workers-dapr-cli; do
    aspire wait "$resource" --apphost "$apphost" --timeout "$resource_timeout_seconds" --non-interactive
done

aspire describe --apphost "$apphost" --format Json --non-interactive >"$describe_file"

endpoint() {
    jq -er --arg resource "$1" --arg name "$2" \
        '[.resources[] | select(.displayName == $resource) | .urls[] | select(.name == $name) | .url] | first // empty' \
        "$describe_file" || { echo "[run-live-apphost] endpoint '$2' of resource '$1' was not described." >&2; return 1; }
}

BASE_URL="$(endpoint projects-ui http)"
API_URL="$(endpoint projects http)"
EVENTSTORE_API_URL="$(endpoint eventstore http)"
KEYCLOAK_URL="$(endpoint security http)"
FIXTURE_API_URL="$(endpoint live-fixtures http)"
export BASE_URL API_URL EVENTSTORE_API_URL KEYCLOAK_URL FIXTURE_API_URL

cd "$repository_root/tests/e2e"
npm run typecheck

# Each phase owns a distinct run identity, so fixture graphs, Project IDs, and request identities
# can never collide between the smoke and the full suite.
E2E_RUN_ID="$run_id-smoke" npx playwright test \
    specs/live-apphost-startup.spec.ts \
    specs/projects-authentication.spec.ts \
    --project chromium \
    --workers 2
E2E_RUN_ID="$run_id-full" npx playwright test \
    --project chromium \
    --workers 2

cd "$repository_root"
# The ordinary comparison lane owns no resources during the separate actual-owner/OIDC proof.
aspire stop --apphost "$apphost" --non-interactive
started=0
rm -f "$browser_session_file"
python3 tools/qualification/run_supported_project_reads.py --browser --timeout "$resource_timeout_seconds" \
    --evidence "${E2E_SUPPORTED_EVIDENCE:-/tmp/projects-supported-browser-$(date -u +%Y%m%d%H%M%S)-$$}"
