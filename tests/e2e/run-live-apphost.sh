#!/usr/bin/env bash
set -Eeuo pipefail

repository_root=$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)
apphost="$repository_root/src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj"
describe_file=$(mktemp)
start_file=$(mktemp)
started=0

cleanup() {
    result=$?
    if test "$started" = 1; then
        if ! aspire stop --apphost "$apphost" --non-interactive >/dev/null && test "$result" = 0; then
            result=1
        fi
    fi
    rm -f "$describe_file" "$start_file"
    exit "$result"
}
trap cleanup EXIT

: "${KEYCLOAK_CLIENT_ID:?KEYCLOAK_CLIENT_ID is required}"
: "${TEST_USER_USERNAME:?TEST_USER_USERNAME is required}"
: "${TEST_USER_PASSWORD:?TEST_USER_PASSWORD is required}"

export Projects__E2E__LiveFixtures=1
export E2E_LIVE_APPHOST=1
export E2E_RUN_ID="${E2E_RUN_ID:-run-$(date -u +%Y%m%d%H%M%S)-$$}"

cd "$repository_root"
aspire start --apphost "$apphost" --non-interactive --format Json >"$start_file"
started=1

for resource in security eventstore tenants projects projects-workers projects-ui conversations folders memories live-fixtures eventstore-dapr-cli tenants-dapr-cli projects-dapr-cli projects-workers-dapr-cli; do
    aspire wait "$resource" --apphost "$apphost" --non-interactive
done

aspire describe --apphost "$apphost" --format Json --non-interactive >"$describe_file"

endpoint() {
    resource=$1
    name=$2
    jq -er --arg resource "$resource" --arg name "$name" \
        '.resources[] | select(.displayName == $resource) | .urls[] | select(.name == $name) | .url' \
        "$describe_file"
}

export BASE_URL="$(endpoint projects-ui http)"
export API_URL="$(endpoint projects http)"
export EVENTSTORE_API_URL="$(endpoint eventstore http)"
export KEYCLOAK_URL="$(endpoint security http)"
export FIXTURE_API_URL="$(endpoint live-fixtures http)"

cd "$repository_root/tests/e2e"
npm run typecheck
npx playwright test \
    specs/live-apphost-startup.spec.ts \
    specs/projects-authentication.spec.ts \
    --project chromium \
    --workers 2
npx playwright test \
    --project chromium \
    --workers 2
