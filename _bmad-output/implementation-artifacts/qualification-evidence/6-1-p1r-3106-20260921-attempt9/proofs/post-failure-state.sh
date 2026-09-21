#!/usr/bin/env bash
set -euo pipefail

attempt_root=/tmp/hexalith-p1r-3106-attempt9.xF8z3V
bundle=/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/qualification-evidence/6-1-p1r-3106-20260921-attempt9

df -h /tmp /home/administrator/projects/hexalith/projects | tee "$bundle/disk-state.txt"

test "$(git -C "$attempt_root/builds-candidate" rev-parse HEAD)" = 5730f5a60c0e106131498126cbee323e24db5f4e
test -z "$(git -C "$attempt_root/builds-candidate" status --porcelain=v1)"

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
