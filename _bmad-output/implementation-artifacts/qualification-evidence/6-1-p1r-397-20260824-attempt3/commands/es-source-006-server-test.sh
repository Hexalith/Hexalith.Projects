set -euo pipefail
dotnet test tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -m:1
