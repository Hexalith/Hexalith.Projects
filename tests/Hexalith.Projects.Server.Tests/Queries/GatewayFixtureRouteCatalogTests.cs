#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using System.Text.Json;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.Folders.Contracts;
using Hexalith.Projects.Contracts.Queries;
using Shouldly;
using Xunit;

/// <summary>Verifies that fixture operation schemas describe the actual owner contracts.</summary>
public sealed class GatewayFixtureRouteCatalogTests
{
    /// <summary>Exports each of the six request/result contracts without substituting transport envelopes.</summary>
    [Fact]
    public async Task OperationsDescribeTheSixActualQueryAndResultPairs()
    {
        var catalog = new GatewayFixtureRouteCatalog("tenant-a", includeAdvisoryQueries: true);
        var lease = await catalog.AcquireAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        await using var configuredLease = lease.ConfigureAwait(true);
        RouteCatalogEnvelope envelope = lease.Snapshot.Envelope;
        (string Route, Type Request, Type Result)[] expected =
        [
            ("projects-list", typeof(ListProjectsQuery), typeof(ProjectListPage)),
            ("projects-open", typeof(GetProjectQuery), typeof(GetProjectResponse)),
            ("projects-suggest-conversation", typeof(ResolveProjectFromConversationQuery), typeof(ProjectAdvisoryResolutionResponse)),
            ("projects-suggest-attachments", typeof(ResolveProjectFromAttachmentsQuery), typeof(ProjectAdvisoryResolutionResponse)),
            ("folders-owner-read", typeof(FolderOwnerReadQuery), typeof(FolderOwnerReadResult)),
            ("conversations-advisory-metadata", typeof(GetConversationAdvisoryMetadataQuery), typeof(ConversationAdvisoryMetadataResult)),
        ];
        envelope.Operations!.Count.ShouldBe(expected.Length);
        foreach (var pair in expected)
        {
            RouteContractDeclaration declaration = envelope.Declarations.Single(item => item.RouteId == pair.Route);
            RouteOperationContract actual = envelope.Operations.Single(item => item.RouteId == pair.Route);
            RouteOperationContract exported = RouteOperationSchema.Create(declaration, pair.Request, pair.Result,
                actual.Ingress, actual.Eligibility, JsonSerializerOptions.Web);
            JsonElement.DeepEquals(actual.RequestSchema, exported.RequestSchema).ShouldBeTrue();
            JsonElement.DeepEquals(actual.ResultSchema, exported.ResultSchema).ShouldBeTrue();
            actual.Digest.ShouldBe(exported.Digest);
            envelope.Routes.Single(item => item.RouteId == pair.Route).OperationContractDigest.ShouldBe(actual.Digest);
        }
    }
}
#endif
