set -euo pipefail
pwsh -NoProfile -File ./Tools/test-authoritative-package-catalog.ps1
test -z "$(git status --porcelain=v1)"
