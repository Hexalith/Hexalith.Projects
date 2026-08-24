set -euo pipefail
git status --porcelain=v1
git rev-parse HEAD
git describe --tags --always --dirty
test -z "$(git status --porcelain=v1)"
