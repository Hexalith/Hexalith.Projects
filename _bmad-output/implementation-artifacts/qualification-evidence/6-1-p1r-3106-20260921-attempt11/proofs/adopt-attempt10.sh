#!/usr/bin/env bash
set -euo pipefail

workspace=/home/administrator/projects/hexalith/projects
attempt10="$workspace/_bmad-output/implementation-artifacts/qualification-evidence/6-1-p1r-3106-20260921-attempt10"
attempt11="$workspace/_bmad-output/implementation-artifacts/qualification-evidence/6-1-p1r-3106-20260921-attempt11"
builds="$workspace/references/Hexalith.Builds"
eventstore="$workspace/references/Hexalith.EventStore"
contract="$workspace/_bmad-output/specs/spec-6-1-p1r-revalidation-owner-acceptance/qualification-contract.md"
spec="$workspace/_bmad-output/implementation-artifacts/spec-6-1-p1r.md"

test "$(sha256sum "$attempt10/artifact-manifest.sha256" | cut -d' ' -f1)" = f8fdf2c215c8a759596472dd5a5f0b0e6156bb139769fd1b44fa44ba91e50cb5
(
    cd "$attempt10"
    sha256sum -c artifact-manifest.sha256
)
test "$(wc -l < "$attempt10/artifact-manifest.sha256")" -eq 140

adopted_rows=(
    history-001-prior-manifests
    es-001-coordinates
    bld-align-000-commitlint
    bld-align-001-static-parity
    bld-align-002-catalog
    bld-align-003-audit
    es-002-api-blobs
    es-source-001-restore
    es-source-002-build
    es-source-003-contracts-test
    es-source-004-client-test
    es-source-005-domain-test
    es-source-006-server-test
    es-package-001-restore
    es-package-002-build
    es-package-003-contracts-test
    es-package-004-client-test
    es-package-005-domain-test
    es-package-006-server-test
    remote-000-effective-cpm
    remote-001-restore
    remote-002-retain-verify
    bld-candidate-001-restore
    bld-candidate-002-build
    bld-candidate-003-module-test
    bld-candidate-004-evidence-test
    bld-candidate-005-g4
)
for id in "${adopted_rows[@]}"; do
    test "$(jq -r --arg id "$id" 'select(.id == $id) | .result' "$attempt10/command-ledger.jsonl")" = PASS
done
test "${#adopted_rows[@]}" -eq 27

inventory="$attempt10/retained-packages/candidate/g4-tool-package-inventory.json"
test "$(jq -r '.version' "$inventory")" = 0.0.0-p1r3106-attempt10.1
test "$(jq -r '.qualification.sourceTree.revision' "$inventory")" = ad52f350a2f0bc47849179ae17b4594dafff5363
test "$(jq -r '.qualification.sourceTree.clean' "$inventory")" = true
test "$(jq -r '.qualification.sourceValidation.result' "$inventory")" = passed
test "$(jq -r '.qualification.controls.result' "$inventory")" = passed
test "$(jq -r '.qualification.releaseEligible' "$inventory")" = true
test "$(jq '.packages | length' "$inventory")" -eq 2
test "$(jq '.qualificationEvidence | length' "$inventory")" -eq 53

test "$(git -C "$builds" rev-parse fix/p1r-3106-candidate)" = ad52f350a2f0bc47849179ae17b4594dafff5363
test "$(git -C "$builds" rev-parse fix/p1r-3106-rollback-attempt10)" = caf49c58bc18f5226733e7879b4f71e99213fffd
test "$(git -C "$eventstore" rev-parse 'v3.106.0^{commit}')" = 76051c70cbf868c40edc00ca0344fa5bd8879b69
test "$(git -C "$eventstore" rev-parse 'v3.70.1^{commit}')" = f13f9925fdca53efa2ab8c90d396ab106f91bb9c
test "$(sed -n '/<frozen-after-approval /,/<\/frozen-after-approval>/p' "$spec" | sha256sum | cut -d' ' -f1)" = 94a818e011c5b7a393f93fe8d2f3270d169141aefa072c3d9fe979d5f7553413
test "$(rg -c -- 'dotnet test .*--no-build --no-restore .*--max-parallel-test-modules 1' "$contract")" -eq 10
test "$(rg -c -- 'dotnet build .* -m:1' "$contract")" -eq 3

sha256sum \
    "$attempt10/artifact-manifest.sha256" \
    "$attempt10/command-ledger.jsonl" \
    "$attempt10/eventstore-release-packages-v3.106.0.json" \
    "$attempt10/seven-api-blobs.tsv" \
    "$attempt10/remote-package-inventory.tsv" \
    "$inventory" \
    > "$attempt11/adopted-attempt10.sha256"

printf 'adopted_rows=%s\n' "${#adopted_rows[@]}"
printf 'attempt10_manifest_sha256=%s\n' "$(sha256sum "$attempt10/artifact-manifest.sha256" | cut -d' ' -f1)"
printf 'contract_sha256=%s\n' "$(sha256sum "$contract" | cut -d' ' -f1)"
printf 'selected_eventstore=%s\n' "$(git -C "$eventstore" rev-parse 'v3.106.0^{commit}')"
printf 'candidate_builds=%s\n' "$(git -C "$builds" rev-parse fix/p1r-3106-candidate)"
printf 'preserved_failed_rollback=%s\n' "$(git -C "$builds" rev-parse fix/p1r-3106-rollback-attempt10)"
