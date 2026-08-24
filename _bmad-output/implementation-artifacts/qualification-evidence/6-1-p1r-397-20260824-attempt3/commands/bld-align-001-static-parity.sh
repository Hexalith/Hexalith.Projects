set -euo pipefail
test -z "$(git status --porcelain=v1)"
test "$(git rev-parse HEAD)" = fb05dd84625abdcd1a62d2664e8557379fd631bb
git describe --tags --always --dirty
test "$(grep -c '<PackageVersion Include="Hexalith.EventStore' Props/Directory.Packages.props)" -eq 13
test "$(sed -n 's:.*<HexalithEventStoreVersion[^>]*>\([^<]*\)</HexalithEventStoreVersion>.*:\1:p' Props/Directory.Packages.props)" = 3.97.0
test "$(grep -c '3.97.0' src/libraries/Hexalith.Builds.Tooling/Manifest/SupportedPlatformPins.cs)" -eq 1
test "$(grep -c '"const": "3.97.0"' schemas/hexalith.module-manifest.v1.json)" -eq 1
test "$(grep -c '"eventStoreVersion": "3.88.0"' test/fixtures/module/negative/superseded-platform-pin.json)" -eq 1
test "$(jq -r '.ruleIds | join("|")' test/fixtures/module/negative/superseded-platform-pin.expected.json)" = HXM016
test "$(grep -c '"eventStoreVersion": "3.70.1"' test/fixtures/module/negative/tampered-platform-pin.json)" -eq 1
test "$(jq -r '.ruleIds | join("|")' test/fixtures/module/negative/tampered-platform-pin.expected.json)" = HXM016
test "$(jq -r '.ruleIds | join("|")' test/fixtures/module/negative/invalid-profile.expected.json)" = HXM009
test "$(grep -c 'FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF' test/fixtures/evidence/negative/artifact-hash-mismatch.yaml)" -eq 1
expected=$(sha256sum test/fixtures/evidence/positive/evidence/release-passed.json | cut -d' ' -f1)
test "$(grep -R "artifact_sha256: $expected" test/fixtures/evidence/positive/readiness.yaml test/fixtures/evidence/negative/coverage-shortfall.yaml test/fixtures/evidence/negative/outcome-mismatch.yaml test/fixtures/evidence/negative/policy-controls.yaml | wc -l)" -eq 5
find test/fixtures -type f -name '*.json' -print0 | xargs -0 -n1 jq empty
git -c core.whitespace=cr-at-eol diff --check
test -z "$(git status --porcelain=v1)"
