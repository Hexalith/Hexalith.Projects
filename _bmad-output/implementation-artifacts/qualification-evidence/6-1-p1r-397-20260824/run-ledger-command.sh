#!/usr/bin/env bash

set -u

if [ "$#" -lt 7 ]; then
    printf 'usage: %s <id> <repository> <revision> <cwd> <environment> <artifacts> <command>\n' "$0" >&2
    exit 64
fi

command_id=$1
repository=$2
revision=$3
command_cwd=$4
environment_overrides=$5
artifacts=$6
command_text=$7

bundle_root=$(cd "$(dirname "$0")" && pwd)
log_directory="$bundle_root/logs"
ledger_path="$bundle_root/command-ledger.jsonl"
mkdir -p "$log_directory"
log_path="$log_directory/$command_id.log"

started_utc=$(date -u +'%Y-%m-%dT%H:%M:%S.%NZ')
{
    printf 'id: %s\n' "$command_id"
    printf 'repository: %s\n' "$repository"
    printf 'revision: %s\n' "$revision"
    printf 'cwd: %s\n' "$command_cwd"
    printf 'started_utc: %s\n' "$started_utc"
    printf 'environment_overrides: %s\n' "$environment_overrides"
    printf 'command: %s\n\n' "$command_text"
} > "$log_path"

(cd "$command_cwd" && bash -lc "$command_text") >> "$log_path" 2>&1
exit_code=$?
ended_utc=$(date -u +'%Y-%m-%dT%H:%M:%S.%NZ')
result=PASS
if [ "$exit_code" -ne 0 ]; then
    result=FAIL
fi

{
    printf '\nended_utc: %s\n' "$ended_utc"
    printf 'exit: %s\n' "$exit_code"
    printf 'result: %s\n' "$result"
} >> "$log_path"

log_sha256=$(sha256sum "$log_path" | cut -d' ' -f1)
relative_log="logs/$command_id.log"

jq -nc \
    --arg id "$command_id" \
    --arg repository "$repository" \
    --arg revision "$revision" \
    --arg cwd "$command_cwd" \
    --arg started_utc "$started_utc" \
    --arg ended_utc "$ended_utc" \
    --arg command "$command_text" \
    --arg environment_overrides "$environment_overrides" \
    --argjson exit "$exit_code" \
    --arg result "$result" \
    --arg log "$relative_log" \
    --arg log_sha256 "$log_sha256" \
    --arg artifacts "$artifacts" \
    '{id:$id,repository:$repository,revision:$revision,cwd:$cwd,started_utc:$started_utc,ended_utc:$ended_utc,command:$command,environment_overrides:$environment_overrides,exit:$exit,result:$result,log:$log,log_sha256:$log_sha256,artifacts:$artifacts,notes:""}' \
    >> "$ledger_path"

cat "$log_path"
exit "$exit_code"
