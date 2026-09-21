#!/usr/bin/env bash
set -euo pipefail

worktree=/tmp/hexalith-p1r3106-attempt12.8kKst0/builds-rollback
bundle=/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/qualification-evidence/6-1-p1r-3106-20260921-attempt12

test "$(git -C "$worktree" rev-parse HEAD)" = 1f46bc373d82fb266c8396759cc3a57c62e6ae7c
test -z "$(git -C "$worktree" status --porcelain=v1)"
test "$(jq -r '.generatedFromRevision' "$worktree/Tools/package-version-audit.json")" = 44bd5ebcb1cac36f25b67ce5ba1a54a2772ca73b
if git -C "$worktree" merge-base --is-ancestor 44bd5ebcb1cac36f25b67ce5ba1a54a2772ca73b HEAD; then
    exit 1
fi

{
    printf 'head=%s\n' "$(git -C "$worktree" rev-parse HEAD)"
    printf 'describe=%s\n' "$(git -C "$worktree" describe --tags --always --dirty)"
    printf 'status='; git -C "$worktree" status --porcelain=v1
    printf 'audit_generated_from=%s\n' "$(jq -r '.generatedFromRevision' "$worktree/Tools/package-version-audit.json")"
    printf 'old_audit_revision_is_ancestor=false\n'
} | tee "$bundle/provenance-repair-coordinate.txt"
