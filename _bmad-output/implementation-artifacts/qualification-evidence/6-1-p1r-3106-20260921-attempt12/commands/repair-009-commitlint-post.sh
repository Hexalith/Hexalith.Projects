#!/usr/bin/env bash
set -euo pipefail
cd /tmp/hexalith-p1r3106-attempt12.8kKst0/builds-rollback
/home/administrator/projects/hexalith/projects/references/Hexalith.Builds/node_modules/.bin/commitlint --config /tmp/hexalith-p1r3106-attempt12.8kKst0/builds-rollback/commitlint.config.mjs --from HEAD~3 --to HEAD --verbose 
