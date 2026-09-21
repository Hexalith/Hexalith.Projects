#!/usr/bin/env bash
set -euo pipefail

workspace=/home/administrator/projects/hexalith/projects
bundle="$workspace/_bmad-output/implementation-artifacts/qualification-evidence/6-1-p1r-3106-20260921-attempt12"
attempt10="$workspace/_bmad-output/implementation-artifacts/qualification-evidence/6-1-p1r-3106-20260921-attempt10"
attempt11="$workspace/_bmad-output/implementation-artifacts/qualification-evidence/6-1-p1r-3106-20260921-attempt11"
builds="$workspace/references/Hexalith.Builds"
eventstore="$workspace/references/Hexalith.EventStore"
contract="$workspace/_bmad-output/specs/spec-6-1-p1r-revalidation-owner-acceptance/qualification-contract.md"
spec="$workspace/_bmad-output/implementation-artifacts/spec-6-1-p1r.md"

test "$(sha256sum "$attempt10/artifact-manifest.sha256" | cut -d' ' -f1)" = f8fdf2c215c8a759596472dd5a5f0b0e6156bb139769fd1b44fa44ba91e50cb5
test "$(sha256sum "$attempt11/artifact-manifest.sha256" | cut -d' ' -f1)" = 8160f13b71756fbbc089f988599c20cf8225af6ce6e76ed582c323918f66c29a
(cd "$attempt10" && sha256sum -c artifact-manifest.sha256)
(cd "$attempt11" && sha256sum -c artifact-manifest.sha256)
test "$(wc -l < "$attempt10/artifact-manifest.sha256")" -eq 140
test "$(wc -l < "$attempt11/artifact-manifest.sha256")" -eq 39

attempt10_rows=(
    history-001-prior-manifests es-001-coordinates bld-align-000-commitlint
    bld-align-001-static-parity bld-align-002-catalog bld-align-003-audit
    es-002-api-blobs es-source-001-restore es-source-002-build
    es-source-003-contracts-test es-source-004-client-test
    es-source-005-domain-test es-source-006-server-test
    es-package-001-restore es-package-002-build es-package-003-contracts-test
    es-package-004-client-test es-package-005-domain-test es-package-006-server-test
    remote-000-effective-cpm remote-001-restore remote-002-retain-verify
    bld-candidate-001-restore bld-candidate-002-build
    bld-candidate-003-module-test bld-candidate-004-evidence-test
    bld-candidate-005-g4
)
for id in "${attempt10_rows[@]}"; do
    test "$(jq -r --arg id "$id" 'select(.id == $id) | .result' "$attempt10/command-ledger.jsonl")" = PASS
done
test "${#attempt10_rows[@]}" -eq 27
test "$(jq -r 'select(.id == "rollback-bld-009-audit") | .result' "$attempt11/command-ledger.jsonl")" = FAIL
test "$(jq -r 'select(.id == "rollback-bld-009-audit") | .log_sha256' "$attempt11/command-ledger.jsonl")" = 378b23a299031df38bc9a5b9a35ab26438a42668de5dd05aa28767c534f013ae
rg -q "Generated-from revision '44bd5ebcb1cac36f25b67ce5ba1a54a2772ca73b' is not an ancestor" "$attempt11/logs/rollback-bld-009-audit.log"

test "$(git -C "$builds" rev-parse fix/p1r-3106-candidate)" = ad52f350a2f0bc47849179ae17b4594dafff5363
test "$(git -C "$builds" rev-parse fix/p1r-3106-rollback-attempt10)" = caf49c58bc18f5226733e7879b4f71e99213fffd
test "$(git -C "$builds" rev-parse fix/p1r-3106-rollback-candidate)" = 7af20f8bafbfe561df6f7705913a0800603090b5
test "$(git -C "$eventstore" rev-parse 'v3.106.0^{commit}')" = 76051c70cbf868c40edc00ca0344fa5bd8879b69
test "$(git -C "$eventstore" rev-parse 'v3.70.1^{commit}')" = f13f9925fdca53efa2ab8c90d396ab106f91bb9c
test "$(sed -n '/<frozen-after-approval /,/<\/frozen-after-approval>/p' "$spec" | sha256sum | cut -d' ' -f1)" = 94a818e011c5b7a393f93fe8d2f3270d169141aefa072c3d9fe979d5f7553413
test "$(rg -c -- 'dotnet test .*--no-build --no-restore .*--max-parallel-test-modules 1' "$contract")" -eq 10
test "$(rg -c -- 'dotnet build .* -m:1' "$contract")" -eq 3
test "$(git -C "$builds" rev-parse HEAD)" = 2fba3497043fe5ffcfe4dc44c51a09eae9b950ab
test -z "$(git -C "$builds" status --porcelain=v1)"
test "$(git -C "$eventstore" rev-parse HEAD)" = ffb6901a5b840af7be030ff246435b4bad00b79c
test -z "$(git -C "$eventstore" status --porcelain=v1)"

sha256sum \
    "$attempt10/artifact-manifest.sha256" \
    "$attempt10/command-ledger.jsonl" \
    "$attempt11/artifact-manifest.sha256" \
    "$attempt11/command-ledger.jsonl" \
    > "$bundle/adopted-prior-attempts.sha256"

printf 'attempt10_adopted_rows=%s\n' "${#attempt10_rows[@]}"
printf 'attempt10_manifest_sha256=%s\n' "$(sha256sum "$attempt10/artifact-manifest.sha256" | cut -d' ' -f1)"
printf 'attempt11_manifest_sha256=%s\n' "$(sha256sum "$attempt11/artifact-manifest.sha256" | cut -d' ' -f1)"
printf 'rollback_head=%s\n' "$(git -C "$builds" rev-parse fix/p1r-3106-rollback-candidate)"
