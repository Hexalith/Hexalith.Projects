#!/usr/bin/env bash
set -euo pipefail

worktree=/tmp/hexalith-p1r3106-attempt12.8kKst0/builds-rollback
bundle=/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/qualification-evidence/6-1-p1r-3106-20260921-attempt12
head=7af20f8bafbfe561df6f7705913a0800603090b5
provenance=1f46bc373d82fb266c8396759cc3a57c62e6ae7c

test "$(git -C "$worktree" rev-parse HEAD)" = "$head"
test "$(git -C "$worktree" rev-parse HEAD^)" = "$provenance"
test "$(git -C "$worktree" rev-parse HEAD^^)" = 38bac7906bd8aa3887bdec4dd7c31470a10dd5ed
test "$(git -C "$worktree" rev-parse HEAD^^^)" = ad52f350a2f0bc47849179ae17b4594dafff5363
test -z "$(git -C "$worktree" status --porcelain=v1)"
test "$(git -C "$worktree" diff-tree --no-commit-id --name-only -r HEAD)" = Tools/package-version-audit.json
test "$(jq -r '.generatedFromRevision' "$worktree/Tools/package-version-audit.json")" = "$provenance"
test "$(jq -r '.familyDecisions[] | select(.family == "hexalith-eventstore") | .origin.generatedFromRevision' "$worktree/Tools/package-version-audit.json")" = "$provenance"
git -C "$worktree" merge-base --is-ancestor "$provenance" "$head"

{
    printf 'rollback_head=%s\n' "$head"
    printf 'provenance_parent=%s\n' "$provenance"
    printf 'audit_generated_from=%s\n' "$(jq -r '.generatedFromRevision' "$worktree/Tools/package-version-audit.json")"
    printf 'eventstore_origin_revision=%s\n' "$(jq -r '.familyDecisions[] | select(.family == "hexalith-eventstore") | .origin.generatedFromRevision' "$worktree/Tools/package-version-audit.json")"
    printf 'audit_revision_is_ancestor=true\n'
    printf 'repair_changed_paths=%s\n' "$(git -C "$worktree" diff-tree --no-commit-id --name-only -r HEAD)"
    printf 'status='; git -C "$worktree" status --porcelain=v1
} | tee "$bundle/provenance-repair-result.txt"
