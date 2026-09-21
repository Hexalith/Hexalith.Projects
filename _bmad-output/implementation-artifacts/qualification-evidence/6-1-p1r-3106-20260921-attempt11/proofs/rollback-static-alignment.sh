#!/usr/bin/env bash
set -euo pipefail

test -z "$(git status --porcelain=v1)"
test "$(git rev-parse HEAD)" = 1f46bc373d82fb266c8396759cc3a57c62e6ae7c
test "$(rg -c '<PackageVersion Include="Hexalith.EventStore' Props/Directory.Packages.props)" -eq 13
rg -q 'HexalithEventStoreVersion.*3.70.1' Props/Directory.Packages.props
rg -q 'EventStoreVersion = "3.70.1"' src/libraries/Hexalith.Builds.Tooling/Manifest/SupportedPlatformPins.cs
rg -q '"const": "3.70.1"' schemas/hexalith.module-manifest.v1.json
rg -q '"eventStoreVersion": "3.70.1"' test/fixtures/module/positive/hexalith.module-manifest.v1.json
test "$(sha256sum test/fixtures/module/positive/hexalith.module-manifest.v1.json | cut -d' ' -f1)" = 4c724f80507230f9fde3c15352283dee6cbe48207752e6bf47313f7bdfff015b
test "$(rg -il '4C724F80507230F9FDE3C15352283DEE6CBE48207752E6BF47313F7BDFFF015B' test/fixtures/evidence --glob '*.json' | wc -l)" -eq 10
! rg -qi 'E43682D7E5B7A300767C82F5AA87F8838A9513A0CFE012869E91BCCA1FA03813' test/fixtures/evidence --glob '*.json'
test "$(jq -r '.platform.eventStoreVersion' test/fixtures/module/negative/superseded-platform-pin.json)" = 3.88.0
test "$(jq -r '.platform.eventStoreVersion' test/fixtures/module/negative/tampered-platform-pin.json)" = 3.106.0
test "$(jq -r '.ruleIds | join(",")' test/fixtures/module/negative/superseded-platform-pin.expected.json)" = HXM016
test "$(jq -r '.ruleIds | join(",")' test/fixtures/module/negative/tampered-platform-pin.expected.json)" = HXM016
test "$(jq -r '.ruleIds | join(",")' test/fixtures/module/negative/invalid-profile.expected.json)" = HXM009
rg -q 'artifact_sha256: FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF' test/fixtures/evidence/negative/artifact-hash-mismatch.yaml
test "$(jq '[.packages[] | select(.family == "hexalith-eventstore")] | length' Tools/package-version-audit.json)" -eq 13
test "$(jq -r '[.packages[] | select(.family == "hexalith-eventstore") | .selectedVersion] | unique | join(",")' Tools/package-version-audit.json)" = 3.70.1
! rg -q '3\.106\.0' Tools schemas src/libraries/Hexalith.Builds.Tooling/Manifest test \
    --glob '!Tools/package-version-audit.json' \
    --glob '!Tools/README.md' \
    --glob '!test/fixtures/module/negative/tampered-platform-pin.json' \
    --glob '!**/bin/**' \
    --glob '!**/obj/**'
git diff --check
