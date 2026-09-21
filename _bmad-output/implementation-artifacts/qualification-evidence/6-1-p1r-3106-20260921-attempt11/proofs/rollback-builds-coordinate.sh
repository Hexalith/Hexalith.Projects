#!/usr/bin/env bash
set -euo pipefail

workspace=/home/administrator/projects/hexalith/projects
rollback=/tmp/hexalith-p1r-3106-attempt11.LhzYSJ/builds-rollback
bundle="$workspace/_bmad-output/implementation-artifacts/qualification-evidence/6-1-p1r-3106-20260921-attempt11"
builds="$workspace/references/Hexalith.Builds"

test -z "$(git -C "$rollback" status --porcelain=v1)"
test "$(git -C "$rollback" rev-parse HEAD)" = 1f46bc373d82fb266c8396759cc3a57c62e6ae7c
test "$(git -C "$rollback" rev-parse HEAD^)" = 38bac7906bd8aa3887bdec4dd7c31470a10dd5ed
test "$(git -C "$rollback" rev-parse HEAD^^)" = ad52f350a2f0bc47849179ae17b4594dafff5363
test "$(git -C "$rollback" rev-parse 'HEAD~2^{tree}')" = "$(git -C "$builds" rev-parse 'ad52f350^{tree}')"
test "$(git -C "$rollback" rev-parse 'HEAD^{tree}')" = "$(git -C "$builds" rev-parse 'caf49c5^{tree}')"
test "$(git -C "$rollback" rev-parse 'HEAD^^{tree}')" != "$(git -C "$rollback" rev-parse 'HEAD^{tree}')"
test "$(git -C "$rollback" rev-parse 'HEAD^{tree}')" = d53a53615774e127ebddc0688e9ad459e2de017c
test "$(git -C "$rollback" rev-parse 'HEAD^^{tree}')" = b404b1624b239446bfbc4a99127f8a342a062e8a
test "$(git -C "$builds" rev-parse fix/p1r-3106-rollback-attempt10)" = caf49c58bc18f5226733e7879b4f71e99213fffd
test "$(git -C "$builds" rev-parse 'fix/p1r-3106-rollback-attempt10^{tree}')" = d53a53615774e127ebddc0688e9ad459e2de017c
test "$(git -C "$builds" rev-parse 'fix/p1r-3106-rollback-attempt10^^{tree}')" = b404b1624b239446bfbc4a99127f8a342a062e8a

for revision in HEAD HEAD^; do
    git -C "$rollback" log -1 --format='%B' "$revision" |
        awk 'length($0) > 199 { exit 1 }'
done

{
    printf 'status='; git -C "$rollback" status --porcelain=v1
    printf 'head='; git -C "$rollback" rev-parse HEAD
    printf 'describe='; git -C "$rollback" describe --tags --always --dirty
    printf 'commit1='; git -C "$rollback" rev-parse HEAD^
    printf 'commit1_tree='; git -C "$rollback" rev-parse 'HEAD^^{tree}'
    printf 'commit2_tree='; git -C "$rollback" rev-parse 'HEAD^{tree}'
    printf 'failed_branch='; git -C "$builds" rev-parse fix/p1r-3106-rollback-attempt10
    git -C "$rollback" log -2 --format='%H%n%P%n%aI%n%B%n---'
} | tee "$bundle/rollback-builds-coordinate.txt"
