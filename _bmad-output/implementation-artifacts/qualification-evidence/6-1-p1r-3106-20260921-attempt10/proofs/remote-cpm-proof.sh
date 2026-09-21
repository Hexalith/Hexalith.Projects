#!/usr/bin/env bash
set -euo pipefail

remote_consumer=/tmp/hexalith-p1r-3106-attempt10.1ai9Yh/remote-consumer
evidence_json=/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/qualification-evidence/6-1-p1r-3106-20260921-attempt10/effective-msbuild-properties.json

env DOTNET_CLI_HOME=/tmp/hexalith-p1r-3106-attempt10.1ai9Yh/caches/remote-home TMPDIR=/tmp/hexalith-p1r-3106-attempt10.1ai9Yh/caches/remote-tmp dotnet msbuild RemoteConsumer.csproj -nologo -getProperty:ManagePackageVersionsCentrally -getProperty:MSBuildProjectFullPath | tee "$evidence_json"
jq -e --arg project "$remote_consumer/RemoteConsumer.csproj" '(.Properties.ManagePackageVersionsCentrally // "") != "true" and .Properties.MSBuildProjectFullPath == $project' "$evidence_json" >/dev/null
