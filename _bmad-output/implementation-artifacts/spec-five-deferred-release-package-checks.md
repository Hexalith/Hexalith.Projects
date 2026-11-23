---
title: 'Verify Projects Release package compatibility and preserve dependency audits'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Close five Projects release-packaging follow-ups in the deferred-work register. Verify a package-only Release build of Server.Tests, including the worker guard, and check the resolved Folders.Client and McpCli.Abstractions versions. Verify that the published EventStore and Conversations packages provide the APIs used by the Story 6.7 read cutover. Compare the actual packed dependencies with the five-package manifest, including the four EventStore.Contracts entries. Remove the NuGet audit override from the release pack helper and cover its command and failure handling.

</frozen-after-approval>

## Implementation Notes

Use the Projects dependency revisions selected by the checkout under test. The existing manifest validator and NuGet assets provide the dependency checks. Pack prerelease candidates into a temporary directory. The release workflow owns the preceding audited restore. Record the results against each original register entry.

The build used Projects baseline `43e3b89ffb0122b9deeee2f84c47992deafd78f2`, its committed Builds catalog `ff25c4f89d9a602fc375dc7c3bd280f70e9436f9` and .NET SDK 11.0.100. The Server.Tests assets resolved EventStore 5.4.0, Conversations 2.1.0, Folders.Client 2.0.1, McpCli.Abstractions 3.0.0 and Tenants.Client 8.0.3. The ten archives listed below matched their NuGet.org downloads byte for byte. The EventStore generator archive contains the analyzer used to emit typed gateway queries.

The first real pack identified four mismatches in `tools/release-packages.json`. Contracts, core, Client and Testing all emit Commons.UniqueIds 3.0.2. Their EventStore.Contracts 5.4.0 entries already matched the packages. Updated the four Commons entries and reran the complete gate.

## Verification

- Ran `CI=true dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj -c Release -m:1 -p:UseHexalithProjectReferences=false -p:MinVerVersionOverride=1.0.0`. The build restored successfully with zero warnings and errors. Evaluated properties gave `NuGetAudit=true`, `NuGetAuditMode=all` and `TreatWarningsAsErrors=true`.
- Executed the Release assembly's native xUnit runner. The full package-mode assembly passed 1,547 of 1,547 tests with an isolated Dapr 1.19.1 sidecar and an in-memory state store. This included the production worker guard, 39 read-cutover cases and the HTTP/store-client restart test.
- Ran `pwsh -NoProfile -File tests/tools/run-package-dependency-gate.ps1 -Version 91.92.93-ci.1` with a private candidate directory. The five real nuspecs passed inventory, provenance and dependency validation, and all five isolated consumers restored and compiled.
- Ran `python3 tests/tools/test_release_package_tools.py` (22 passed), `python3 tests/tools/test_release_workflow.py` (15 passed) and `pwsh -NoProfile -File tests/tools/run-ci-workflow-gates.ps1` (passed). The audit regression covers the command for every candidate and a separate first-command failure verifies early termination.

### Reproducing the checks

From the Projects root after the Release build, run the complete Server.Tests assembly with four available local ports and Dapr 1.19.1. The store lives in the sidecar across the host and client restart exercised by the test.

```bash
(
  set -eu
  components=$(mktemp -d)
  printf '%s\n' 'apiVersion: dapr.io/v1alpha1' 'kind: Component' 'metadata:' '  name: statestore' \
    'spec:' '  type: state.in-memory' '  version: v1' '  metadata: []' > "$components/state.yaml"
  daprd --app-id projects-release-check --resources-path "$components" --dapr-listen-addresses 127.0.0.1 \
    --dapr-http-port 35871 --dapr-grpc-port 35872 --dapr-internal-grpc-port 35873 \
    --metrics-listen-address 127.0.0.1 --metrics-port 35874 > "$components/dapr.log" 2>&1 &
  pid=$!
  trap 'kill "$pid" 2>/dev/null || true; wait "$pid" 2>/dev/null || true' EXIT
  export STORY61_DAPR_HTTP_ENDPOINT=http://127.0.0.1:35871 STORY61_DAPR_GRPC_ENDPOINT=http://127.0.0.1:35872
  curl --fail --retry 10 --retry-connrefused "$STORY61_DAPR_HTTP_ENDPOINT/v1.0/healthz/outbound"
  dotnet tests/Hexalith.Projects.Server.Tests/bin/Release/net11.0/Hexalith.Projects.Server.Tests.dll \
    -result-xml "$components/server-tests.xml"
)
```

Compare the restored owner archives to the public feed. Set `NUGET_PACKAGES` to the global packages directory from the Release restore.

```bash
(
  set -euo pipefail
  for coordinate in hexalith.eventstore.{contracts,client,gateway,domainservice,restapi.generators}/5.4.0 \
    hexalith.conversations.{client,contracts}/2.1.0 hexalith.folders.client/2.0.1 \
    hexalith.mcpcli.abstractions/3.0.0 hexalith.tenants.client/8.0.3; do
    file="${coordinate%/*}.${coordinate#*/}.nupkg"
    curl -fsSL "https://api.nuget.org/v3-flatcontainer/$coordinate/$file" | cmp - "$NUGET_PACKAGES/$coordinate/$file"
  done
)
```

## Review Triage Log

- Low, corrected the Release build command to include the project path.
- Medium, recorded the xUnit invocation, isolated state component and required endpoint settings for the restart test.
- Medium, listed the public archive coordinates and supplied a byte comparison. Also verified the published analyzer directly.
- Medium, extended the pack-command regression to all five successful commands and kept a separate first-failure case.
