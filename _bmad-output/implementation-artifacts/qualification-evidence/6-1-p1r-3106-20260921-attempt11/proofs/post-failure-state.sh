#!/usr/bin/env bash
set -euo pipefail

workspace=/home/administrator/projects/hexalith/projects
attempt_root=/tmp/hexalith-p1r-3106-attempt11.LhzYSJ
bundle="$workspace/_bmad-output/implementation-artifacts/qualification-evidence/6-1-p1r-3106-20260921-attempt11"
builds="$workspace/references/Hexalith.Builds"
eventstore="$workspace/references/Hexalith.EventStore"
rollback="$attempt_root/builds-rollback"

df -h /tmp "$workspace" | tee "$bundle/disk-state.txt"

test "$(git -C "$builds" rev-parse HEAD)" = 2fba3497043fe5ffcfe4dc44c51a09eae9b950ab
test -z "$(git -C "$builds" status --porcelain=v1)"
test "$(git -C "$eventstore" rev-parse HEAD)" = ffb6901a5b840af7be030ff246435b4bad00b79c
test -z "$(git -C "$eventstore" status --porcelain=v1)"

test "$(git -C "$rollback" rev-parse HEAD)" = 1f46bc373d82fb266c8396759cc3a57c62e6ae7c
test "$(git -C "$rollback" rev-parse HEAD^)" = 38bac7906bd8aa3887bdec4dd7c31470a10dd5ed
test "$(git -C "$rollback" rev-parse HEAD^^)" = ad52f350a2f0bc47849179ae17b4594dafff5363
test "$(git -C "$rollback" rev-parse 'HEAD^{tree}')" = d53a53615774e127ebddc0688e9ad459e2de017c
test "$(git -C "$rollback" rev-parse 'HEAD^^{tree}')" = b404b1624b239446bfbc4a99127f8a342a062e8a
test -z "$(git -C "$rollback" status --porcelain=v1)"

test "$(git -C "$builds" rev-parse fix/p1r-3106-rollback-attempt10)" = caf49c58bc18f5226733e7879b4f71e99213fffd
test "$(git -C "$builds" rev-parse 'fix/p1r-3106-rollback-attempt10^{tree}')" = d53a53615774e127ebddc0688e9ad459e2de017c
test "$(jq -r '.generatedFromRevision' "$rollback/Tools/package-version-audit.json")" = 44bd5ebcb1cac36f25b67ce5ba1a54a2772ca73b
if git -C "$rollback" merge-base --is-ancestor 44bd5ebcb1cac36f25b67ce5ba1a54a2772ca73b HEAD; then
    exit 1
fi

{
    printf 'rollback_head=%s\n' "$(git -C "$rollback" rev-parse HEAD)"
    printf 'rollback_commit1=%s\n' "$(git -C "$rollback" rev-parse HEAD^)"
    printf 'audit_generated_from=%s\n' "$(jq -r '.generatedFromRevision' "$rollback/Tools/package-version-audit.json")"
    printf 'audit_generated_from_is_ancestor=false\n'
    printf 'rollback_status='; git -C "$rollback" status --porcelain=v1
    printf 'rollback_describe='; git -C "$rollback" describe --tags --always --dirty
} | tee "$bundle/audit-provenance-state.txt"
