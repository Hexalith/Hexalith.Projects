set -euo pipefail
apis=(
    src/Hexalith.EventStore.DomainService/IAsyncDomainProjectionHandler.cs
    src/Hexalith.EventStore.Client/Projections/IReadModelStore.cs
    src/Hexalith.EventStore.Client/Projections/IReadModelBatchStore.cs
    src/Hexalith.EventStore.Client/Projections/ReadModelWritePolicy.cs
    src/Hexalith.EventStore.DomainService/IDomainQueryHandler.cs
    src/Hexalith.EventStore.Client/Queries/IQueryCursorCodec.cs
    src/Hexalith.EventStore.Client/Queries/QueryCursorScope.cs
)
revisions=(
    f13f9925fdca53efa2ab8c90d396ab106f91bb9c
    v3.88.0
    94591f3539ce30372db58e5fdd3ba017ea8c07b8
    da52e2c85ecc5909fa8ce2547e626f3968c056ef
)
for revision in "${revisions[@]}"; do
    printf 'revision %s\n' "$revision"
    for api in "${apis[@]}"; do
        printf '%s  %s\n' "$(git rev-parse "$revision:$api")" "$api"
    done
done
git diff --name-status f13f9925fdca53efa2ab8c90d396ab106f91bb9c..94591f3539ce30372db58e5fdd3ba017ea8c07b8 -- "${apis[@]}"
test "$(git diff --name-only f13f9925fdca53efa2ab8c90d396ab106f91bb9c..94591f3539ce30372db58e5fdd3ba017ea8c07b8 -- "${apis[@]}")" = src/Hexalith.EventStore.Client/Queries/QueryCursorScope.cs
git diff f13f9925fdca53efa2ab8c90d396ab106f91bb9c..94591f3539ce30372db58e5fdd3ba017ea8c07b8 -- src/Hexalith.EventStore.Client/Queries/QueryCursorScope.cs
test -z "$(git diff --name-only 94591f3539ce30372db58e5fdd3ba017ea8c07b8..da52e2c85ecc5909fa8ce2547e626f3968c056ef -- "${apis[@]}")"
test -z "$(git status --porcelain=v1)"
