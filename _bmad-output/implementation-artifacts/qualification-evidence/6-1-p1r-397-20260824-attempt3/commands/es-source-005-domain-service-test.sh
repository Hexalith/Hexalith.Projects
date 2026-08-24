set -euo pipefail
dotnet test tests/Hexalith.EventStore.DomainService.Tests/Hexalith.EventStore.DomainService.Tests.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -m:1
