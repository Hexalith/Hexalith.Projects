namespace Hexalith.Projects.Server.Tests.Queries;

using System.Reflection;
using System.Text;
using System.Text.Json;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.McpCli.Abstractions;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Server.Queries;

using Shouldly;

using Xunit;

/// <summary>Exercises decorated CLI examples against server admission in both source and package lanes.</summary>
public sealed class ProjectMcpCliAdmissionTests
{
    /// <summary>Every enrolled query's declared routing and example must produce an authorized result.</summary>
    /// <param name="queryType">The enrolled Contracts query to submit.</param>
    [Theory]
    [InlineData(typeof(ListProjectsQuery))]
    [InlineData(typeof(GetProjectQuery))]
    [InlineData(typeof(ResolveProjectFromConversationQuery))]
    [InlineData(typeof(ResolveProjectFromAttachmentsQuery))]
    public async Task DecoratedExamplePassesServerAdmission(Type queryType)
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        await fixture.SeedAsync("project-a");
        HexalithQueryAttribute declaration = queryType.GetCustomAttribute<HexalithQueryAttribute>().ShouldNotBeNull();
        declaration.Example.ShouldNotBeNullOrWhiteSpace();
        declaration.Domain.ShouldNotBeNullOrWhiteSpace();
        declaration.WireType.ShouldNotBeNullOrWhiteSpace();
        declaration.ProjectionType.ShouldBeNull();
        declaration.ProjectionActorType.ShouldBeNull();

        QueryEnvelope envelope = fixture.Query(new ListProjectsQuery()) with
        {
            Domain = declaration.Domain,
            AggregateId = declaration.AggregateId ?? "project-a",
            QueryType = declaration.WireType,
            Payload = Encoding.UTF8.GetBytes(declaration.Example),
        };
        ProjectAdvisoryQueryExecutor advisory = new(fixture.Store, fixture.Gate, fixture.Binding,
            fixture.Folders, new AdvisoryTestConversationDirectory(), fixture.Clock);
        CancellationToken token = TestContext.Current.CancellationToken;
        QueryResult result = queryType == typeof(ListProjectsQuery)
            ? await fixture.Executor.ListAsync(envelope, token)
            : queryType == typeof(GetProjectQuery)
                ? await fixture.Executor.GetAsync(envelope, token)
                : queryType == typeof(ResolveProjectFromConversationQuery)
                    ? await advisory.FromConversationAsync(envelope, token)
                    : await advisory.FromAttachmentsAsync(envelope, token);

        result.Success.ShouldBeTrue(result.ErrorMessage);
        result.ErrorMessage.ShouldBeNull();
        result.PayloadBytes.ShouldNotBeNull();
        using JsonDocument payload = JsonDocument.Parse(result.PayloadBytes);
        payload.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);
    }
}
