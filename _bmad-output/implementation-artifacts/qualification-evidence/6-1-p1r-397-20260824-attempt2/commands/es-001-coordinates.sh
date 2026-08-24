set -euo pipefail
test -z "$(git status --porcelain=v1)"
git rev-parse HEAD
git describe --tags --always --dirty
git remote get-url origin
git rev-parse "v3.97.0^{commit}"
git show v3.97.0:tools/release-packages.json > /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/qualification-evidence/6-1-p1r-397-20260824-attempt2/eventstore-release-packages-v3.97.0.json
sha256sum /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/qualification-evidence/6-1-p1r-397-20260824-attempt2/eventstore-release-packages-v3.97.0.json
jq -r '.packages[].id' /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/qualification-evidence/6-1-p1r-397-20260824-attempt2/eventstore-release-packages-v3.97.0.json
test "$(jq '.packages | length' /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/qualification-evidence/6-1-p1r-397-20260824-attempt2/eventstore-release-packages-v3.97.0.json)" -eq 14
printf '%s\n' '--- unselected HEAD ---'
git rev-parse da52e2c85ecc5909fa8ce2547e626f3968c056ef
git describe --tags --always da52e2c85ecc5909fa8ce2547e626f3968c056ef
printf '%s\n' '--- package..HEAD src diff ---'
git diff --name-status 94591f3539ce30372db58e5fdd3ba017ea8c07b8..da52e2c85ecc5909fa8ce2547e626f3968c056ef -- src
test -z "$(git diff --name-only 94591f3539ce30372db58e5fdd3ba017ea8c07b8..da52e2c85ecc5909fa8ce2547e626f3968c056ef -- src)"
printf '%s\n' '--- v3.97 dependency gitlinks ---'
git ls-tree 94591f3539ce30372db58e5fdd3ba017ea8c07b8:references
printf '%s\n' '--- HEAD dependency gitlinks ---'
git ls-tree da52e2c85ecc5909fa8ce2547e626f3968c056ef:references
printf '%s\n' '--- v3.70.1 dependency gitlinks ---'
git ls-tree f13f9925fdca53efa2ab8c90d396ab106f91bb9c:references
test -z "$(git status --porcelain=v1)"
