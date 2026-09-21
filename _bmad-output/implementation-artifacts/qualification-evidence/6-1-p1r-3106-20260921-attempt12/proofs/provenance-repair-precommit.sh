#!/usr/bin/env bash
set -euo pipefail

worktree=/tmp/hexalith-p1r3106-attempt12.8kKst0/builds-rollback
message=/tmp/hexalith-p1r3106-attempt12.8kKst0/messages/provenance-repair.txt
commitlint=/home/administrator/projects/hexalith/projects/references/Hexalith.Builds/node_modules/.bin/commitlint

test "$(git -C "$worktree" rev-parse HEAD)" = 1f46bc373d82fb266c8396759cc3a57c62e6ae7c
test "$(git -C "$worktree" status --porcelain=v1)" = ' M Tools/package-version-audit.json'
test "$(git -C "$worktree" diff --name-only)" = Tools/package-version-audit.json
test "$(jq -r '.generatedFromRevision' "$worktree/Tools/package-version-audit.json")" = 1f46bc373d82fb266c8396759cc3a57c62e6ae7c
git -C "$worktree" merge-base --is-ancestor 1f46bc373d82fb266c8396759cc3a57c62e6ae7c HEAD
git -C "$worktree" diff --check
"$commitlint" --config "$worktree/commitlint.config.mjs" --verbose < "$message"
