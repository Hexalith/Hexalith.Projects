set -euo pipefail
dotnet test tests/Hexalith.EventStore.Client.Tests/Hexalith.EventStore.Client.Tests.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -m:1
