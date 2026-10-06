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
started=0

cleanup() {
    result=$?
    if test "$started" = 1; then
        # Exact-AppHost teardown runs on success, failure, and interruption alike.
        if ! aspire stop --apphost "$apphost" --non-interactive >/dev/null 2>&1 && test "$result" = 0; then
            result=1
        fi
    fi
    # The captured graph and start output stay local and are never published as artifacts.
    rm -f "$describe_file" "$start_file"
    exit "$result"
}
trap cleanup EXIT

require_command() {
    command -v "$1" >/dev/null 2>&1 || { echo "[run-live-apphost] $1 is required." >&2; exit 2; }
}

for command_name in aspire jq npm npx; do
    require_command "$command_name"
done

: "${KEYCLOAK_CLIENT_ID:?KEYCLOAK_CLIENT_ID is required}"
: "${TEST_USER_USERNAME:?TEST_USER_USERNAME is required}"
: "${TEST_USER_PASSWORD:?TEST_USER_PASSWORD is required}"
case "$resource_timeout_seconds" in
    '' | *[!0-9]*) echo "[run-live-apphost] E2E_RESOURCE_TIMEOUT_SECONDS must be a positive integer." >&2; exit 2 ;;
esac

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
# Ports are always assigned by Aspire and read from the single describe capture below.
unset BASE_URL API_URL EVENTSTORE_API_URL KEYCLOAK_URL FIXTURE_API_URL TEST_TENANT_ID TEST_PRINCIPAL_ID

cd "$repository_root"
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
