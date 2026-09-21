#!/usr/bin/env bash
set -euo pipefail

workspace=/home/administrator/projects/hexalith/projects
attempt_root=/tmp/hexalith-p1r-3106-attempt10.1ai9Yh
bundle="$workspace/_bmad-output/implementation-artifacts/qualification-evidence/6-1-p1r-3106-20260921-attempt10"

df -h /tmp "$workspace" | tee "$bundle/disk-state.txt"

test "$(git -C "$workspace/references/Hexalith.Builds" rev-parse HEAD)" = 2fba3497043fe5ffcfe4dc44c51a09eae9b950ab
test -z "$(git -C "$workspace/references/Hexalith.Builds" status --porcelain=v1)"
test "$(git -C "$workspace/references/Hexalith.EventStore" rev-parse HEAD)" = ffb6901a5b840af7be030ff246435b4bad00b79c
test -z "$(git -C "$workspace/references/Hexalith.EventStore" status --porcelain=v1)"

test "$(git -C "$attempt_root/builds-candidate" rev-parse HEAD)" = ad52f350a2f0bc47849179ae17b4594dafff5363
test -z "$(git -C "$attempt_root/builds-candidate" status --porcelain=v1)"

test "$(git -C "$attempt_root/builds-rollback" rev-parse HEAD)" = caf49c58bc18f5226733e7879b4f71e99213fffd
test "$(git -C "$attempt_root/builds-rollback" rev-parse HEAD^)" = 44bd5ebcb1cac36f25b67ce5ba1a54a2772ca73b
test "$(git -C "$attempt_root/builds-rollback" rev-parse HEAD^^)" = ad52f350a2f0bc47849179ae17b4594dafff5363
test -z "$(git -C "$attempt_root/builds-rollback" status --porcelain=v1)"

for lane in selected package; do
    eventstore="$attempt_root/$lane/EventStore"
    test "$(git -C "$eventstore" rev-parse HEAD)" = 76051c70cbf868c40edc00ca0344fa5bd8879b69
    test -z "$(git -C "$eventstore" status --porcelain=v1)"

    for pair in \
        "Hexalith.AI.Tools 5f93d2ec8239494852c97032c819cb1689939e36" \
        "Hexalith.Builds aee01323579adeda952bf41b2f3e0e61785075c6" \
        "Hexalith.Commons 19d7d4d6b21160557b7449f55a0ad0f55e6d7dc6" \
        "Hexalith.FrontComposer 0122c53afb5ac2993ca2e4d6a6e828600c960687" \
        "Hexalith.Memories 5829db422522b79c0d164df85adde53a996ba114" \
        "Hexalith.PolymorphicSerializations 8aeed1d27c9a050bc4bec6d89051aa00de306a69" \
        "Hexalith.Tenants c150d5b1af4f911ff3b2a7ab7901d94838be09d7"
    do
        set -- $pair
        dependency="$eventstore/references/$1"
        test "$(git -C "$dependency" rev-parse HEAD)" = "$2"
        test -z "$(git -C "$dependency" status --porcelain=v1)"
    done
done

git -C "$attempt_root/builds-rollback" log -2 --format='%H%n%P%n%an <%ae>%n%aI%n%B%n---' |
    tee "$bundle/rollback-commit-metadata.txt"
