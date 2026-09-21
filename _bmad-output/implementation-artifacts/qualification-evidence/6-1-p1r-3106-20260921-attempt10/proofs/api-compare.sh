#!/usr/bin/env bash
set -euo pipefail
rollback=f13f9925fdca53efa2ab8c90d396ab106f91bb9c
superseded=4843b492dff7c16a4bc74db67509263f969c78c6
selected=76051c70cbf868c40edc00ca0344fa5bd8879b69
output=/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/qualification-evidence/6-1-p1r-3106-20260921-attempt10/seven-api-blobs.tsv
paths=(src/Hexalith.EventStore.DomainService/IAsyncDomainProjectionHandler.cs src/Hexalith.EventStore.Client/Projections/IReadModelStore.cs src/Hexalith.EventStore.Client/Projections/IReadModelBatchStore.cs src/Hexalith.EventStore.Client/Projections/ReadModelWritePolicy.cs src/Hexalith.EventStore.DomainService/IDomainQueryHandler.cs src/Hexalith.EventStore.Client/Queries/IQueryCursorCodec.cs src/Hexalith.EventStore.Client/Queries/QueryCursorScope.cs)
printf 'path\trollback\tv3.88.0\tselected\tpackage-source\n' > "$output"
for path in "${paths[@]}"; do rollback_blob=$(git rev-parse "$rollback:$path"); superseded_blob=$(git rev-parse "$superseded:$path"); selected_blob=$(git rev-parse "$selected:$path"); package_blob=$(git rev-parse "v3.106.0:$path"); test "$selected_blob" = "$package_blob"; printf '%s\t%s\t%s\t%s\t%s\n' "$path" "$rollback_blob" "$superseded_blob" "$selected_blob" "$package_blob" >> "$output"; done
for index in 1 2 3 4 5 6; do test "$(awk -F '\t' -v row=$((index + 1)) 'NR == row { print $2 }' "$output")" = "$(awk -F '\t' -v row=$((index + 1)) 'NR == row { print $4 }' "$output")"; done
cat "$output"
