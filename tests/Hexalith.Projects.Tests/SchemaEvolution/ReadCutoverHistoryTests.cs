namespace Hexalith.Projects.Tests.SchemaEvolution;

using System.Text.Json;

using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Shouldly;
using Xunit;

public sealed class ReadCutoverHistoryTests
{
    [Theory]
    [InlineData("opaque-historical-project")]
    [InlineData("01HZ9K8YQ3W6V2N4R7T5P0X1AB")]
    public void HistoricalPendingEventAndOpaqueIdentityRemainReadable(string projectId)
    {
        string json = JsonSerializer.Serialize(new
        {
            tenantId = "tenant-a", projectId, displayNameIntent = "Historical", reasonCode = "pending", retryable = true,
            actorPrincipalId = "opaque-actor", correlationId = "historical-correlation", taskId = "historical-task",
            idempotencyKey = "historical-key", idempotencyFingerprint = "historical-fingerprint", occurredAt = DateTimeOffset.UnixEpoch,
        });
        ProjectFolderCreationPending value = JsonSerializer.Deserialize<ProjectFolderCreationPending>(json, JsonSerializerOptions.Web)!;
        value.ProjectId.ShouldBe(projectId);
        new ProjectId(projectId).Value.ShouldBe(projectId);
        value.OccurredAt.ShouldBe(DateTimeOffset.UnixEpoch);
        JsonSerializer.Deserialize<ProjectFolderCreationPending>(JsonSerializer.Serialize(value, JsonSerializerOptions.Web), JsonSerializerOptions.Web).ShouldBe(value);
    }
}
