#!/usr/bin/env bash
set -euo pipefail

# This independent Dapr consumer records its production package closure in its own policy.
root=$(cd "$(dirname "$0")/../.." && pwd)
policy="$root/references/Hexalith.McpCli/tools/dependency-policy.json"
version=$(jq -er '.baselinePackages
  | .["Hexalith.EventStore.Client"] as $client
  | select($client == .["Hexalith.EventStore.Contracts"])
  | $client | select(type == "string" and test("^[0-9]+\\.[0-9]+\\.[0-9]+$"))' "$policy")
exec dotnet "$@" "-p:HexalithEventStoreVersion=$version"
