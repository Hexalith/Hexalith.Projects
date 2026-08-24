set -euo pipefail
dotnet build Hexalith.EventStore.slnx --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -m:1
