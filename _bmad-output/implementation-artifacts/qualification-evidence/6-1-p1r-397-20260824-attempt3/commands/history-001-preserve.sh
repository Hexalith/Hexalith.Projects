set -euo pipefail
(
    cd _bmad-output/implementation-artifacts/qualification-evidence/6-1-p1r-397-20260824
    sha256sum -c artifact-manifest.sha256
)
(
    cd _bmad-output/implementation-artifacts/qualification-evidence/6-1-p1r-397-20260824-attempt2
    sha256sum -c artifact-manifest.sha256
)
git -C references/Hexalith.Builds merge-base --is-ancestor fb82c0caedfa4a98b413164dc630e122b50b2970 0942bcb351c82dd46c0aa6c02b503992eff90fdf
test -z "$(git -C references/Hexalith.Builds show --format= --name-only 0942bcb351c82dd46c0aa6c02b503992eff90fdf | grep -v '^_bmad-output/implementation-artifacts/6-1-p1r-eventstore-source-architecture-runner-revalidation-record.md$')"
