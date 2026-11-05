namespace Hexalith.Projects.Server.Tests.Queries;

using System.Text.Json;
using System.Text.Json.Nodes;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Server.Projections.ProjectReads;
using Shouldly;
using Xunit;

public sealed partial class SupportedProjectProjectionTests
{
    /// <summary>Every catalogued rejection validates identity and enum content before advancing persisted evidence.</summary>
    /// <param name="fault">The malformed rejection field.</param>
    [Theory]
    [InlineData("tenant")]
    [InlineData("project")]
    [InlineData("reason")]
    public async Task RejectionNoOpWithForeignIdentityOrUndefinedReasonCannotAdvanceSequence(string fault)
    {
        foreach (Type type in ProjectReadProjectionHandler.NoOpEventTypes.Where(type => type != typeof(ProjectChatBotAuthorityRejected)
            && type.Name.EndsWith("Rejected", StringComparison.Ordinal)
            && (fault != "project" || type != typeof(ProjectCreationRejected))))
        {
            SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
            await fixture.SeedAsync("project-a");
            ProjectionEventDto item = SupportedProjectReadFixture.Event(ValidNoOp(type), 3, 102);
            JsonObject content = JsonNode.Parse(item.Payload)!.AsObject();
            if (fault == "tenant") { content["tenantId"] = "foreign-tenant"; }
            else if (fault == "project") { content["projectId"] = JsonSerializer.SerializeToNode(new ProjectId("foreign-project"), SupportedProjectReadFixture.Json); }
            else { content["reason"] = 999; }
            item = item with { Payload = JsonSerializer.SerializeToUtf8Bytes(content) };
            (await fixture.Projection.ProjectAsync(new("tenant-a", "projects", "project-a", [item]), "invalid-rejection", TestContext.Current.CancellationToken))
                .Status.ShouldBe(ProjectionDispatchStatus.Failed);
            ProjectReadModel persisted = fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", "project-a"))!;
            persisted.Sequence.ShouldBe(2);
            persisted.Freshness.ShouldBe(EvidenceFreshnessState.Unavailable);
        }
    }

    /// <summary>Case-insensitive duplicate fields cannot substitute a different persisted value.</summary>
    [Fact]
    public async Task CaseVariantDuplicatePayloadKeysFailContentValidation()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        await fixture.SeedAsync("project-a");
        ProjectionEventDto item = SupportedProjectReadFixture.Request("project-a").Events[0];
        JsonObject content = JsonNode.Parse(item.Payload)!.AsObject();
        content["Name"] = "Conflicting name";
        item = item with { Payload = JsonSerializer.SerializeToUtf8Bytes(content) };
        (await fixture.Projection.ProjectAsync(new("tenant-a", "projects", "project-a", [item]), "duplicate-key", TestContext.Current.CancellationToken))
            .Status.ShouldBe(ProjectionDispatchStatus.Failed);
        ProjectReadModel persisted = fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", "project-a"))!;
        persisted.Sequence.ShouldBe(2);
        persisted.Freshness.ShouldBe(EvidenceFreshnessState.Unavailable);
    }
}
