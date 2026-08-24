set -euo pipefail
pwsh -NoProfile -File ./Tools/validate-package-version-audit.ps1
test -z "$(git status --porcelain=v1)"
