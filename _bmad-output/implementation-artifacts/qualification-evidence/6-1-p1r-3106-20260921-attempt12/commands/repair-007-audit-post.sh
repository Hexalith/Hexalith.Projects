#!/usr/bin/env bash
set -euo pipefail
cd /tmp/hexalith-p1r3106-attempt12.8kKst0/builds-rollback
env NUGET_PACKAGES=/tmp/hexalith-p1r3106-attempt12.8kKst0/caches/repair-nuget DOTNET_CLI_HOME=/tmp/hexalith-p1r3106-attempt12.8kKst0/caches/repair-home TMPDIR=/tmp/hexalith-p1r3106-attempt12.8kKst0/caches/repair-tmp MSBUILDDISABLENODEREUSE=1 pwsh -NoProfile -File ./Tools/validate-package-version-audit.ps1 
