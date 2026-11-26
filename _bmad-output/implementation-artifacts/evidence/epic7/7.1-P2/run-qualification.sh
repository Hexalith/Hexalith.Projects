#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 3 ]]; then
    echo "Usage: bash $0 REPLAY_INPUTS FOLDERS_CHECKOUT RESULTS_PARENT" >&2
    exit 2
fi

projects=$(git -C "$(dirname -- "$0")" rev-parse --show-toplevel)
inputs=$(realpath -- "$1")
folders=$(realpath -- "$2")
results_parent=$(realpath -- "$3")
g1="$inputs/7-1-p2-g1-approved-vySfbsep"
g2="$inputs/7-1-p2-folders-successor-publication-Mv9itxKM"
(cd "$inputs" && sha256sum --check --status replay-inputs.sha256)

results=$(mktemp -d "$results_parent/7-1-p2-replay-XXXXXXXX")
printf 'Retaining replay results in %s\n' "$results"
run-with-temp bash "$g1/replay.sh" "$projects" "$g1" "$results/g1"
run-with-temp bash "$g2/replay/replay-deployment.sh" "$projects" "$folders" "$g2/published" "$results/g2"
