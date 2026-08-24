set -euo pipefail
printf 'eventstore_head=%s\n' "$(git rev-parse HEAD)"
printf 'eventstore_describe=%s\n' "$(git describe --tags --always --dirty)"
printf 'eventstore_status_begin\n'
git status --porcelain=v1
printf 'eventstore_status_end\n'
printf 'recorded_submodules_begin\n'
git submodule status
printf 'recorded_submodules_end\n'
printf 'nested_builds_catalog_present=%s\n' "$(test -f references/Hexalith.Builds/Props/Directory.Packages.props && printf true || printf false)"
printf 'sibling_builds_head=%s\n' "$(git -C ../references/Hexalith.Builds rev-parse HEAD)"
printf 'sibling_builds_status_begin\n'
git -C ../references/Hexalith.Builds status --porcelain=v1
printf 'sibling_builds_status_end\n'
test -z "$(git status --porcelain=v1)"
test "$(git rev-parse HEAD)" = 94591f3539ce30372db58e5fdd3ba017ea8c07b8
test -z "$(git -C ../references/Hexalith.Builds status --porcelain=v1)"
test "$(git -C ../references/Hexalith.Builds rev-parse HEAD)" = fb05dd84625abdcd1a62d2664e8557379fd631bb
