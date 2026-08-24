set -euo pipefail
dotnet restore Hexalith.EventStore.slnx -p:UseHexalithProjectReferences=true --force --no-cache --disable-parallel --verbosity minimal
