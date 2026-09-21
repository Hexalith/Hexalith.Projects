#!/usr/bin/env bash
set -euo pipefail

workspace=/home/administrator/projects/hexalith/projects
attempt10="$workspace/_bmad-output/implementation-artifacts/qualification-evidence/6-1-p1r-3106-20260921-attempt10"
builds="$workspace/references/Hexalith.Builds"
candidate=ad52f350a2f0bc47849179ae17b4594dafff5363
evidence="$attempt10/retained-packages/candidate/qualification-evidence"

test "$(git -C "$builds" show "$candidate:test/fixtures/module/negative/superseded-platform-pin.json" | jq -r '.platform.eventStoreVersion')" = 3.88.0
test "$(git -C "$builds" show "$candidate:test/fixtures/module/negative/tampered-platform-pin.json" | jq -r '.platform.eventStoreVersion')" = 3.70.1
test "$(jq -r '.outcome.ruleId' "$evidence/module-negative-superseded-platform-pin-output.json")" = HXM016
test "$(jq -r '.outcome.ruleId' "$evidence/module-negative-tampered-platform-pin-output.json")" = HXM016
test "$(jq -r '.status' "$evidence/packaged-down-output.json")" = completed
test "$(jq -r '.status' "$evidence/packaged-readiness-output.json")" = passed

sha256sum \
    "$evidence/module-negative-superseded-platform-pin-output.json" \
    "$evidence/module-negative-tampered-platform-pin-output.json" \
    "$evidence/packaged-down-output.json" \
    "$evidence/packaged-readiness-output.json"
