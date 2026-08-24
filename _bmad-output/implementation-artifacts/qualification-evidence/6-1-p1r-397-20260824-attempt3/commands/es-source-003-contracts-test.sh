set -euo pipefail
dotnet test tests/Hexalith.EventStore.Contracts.Tests/Hexalith.EventStore.Contracts.Tests.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -m:1
