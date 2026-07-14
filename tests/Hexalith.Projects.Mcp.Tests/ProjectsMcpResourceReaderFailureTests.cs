// <copyright file="ProjectsMcpResourceReaderFailureTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Mcp.Tests;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Mcp;
using Hexalith.Projects.Client.Generated;
using Hexalith.Projects.Mcp;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using Xunit;

public sealed class ProjectsMcpResourceReaderFailureTests
{
    private static readonly IReadOnlyDictionary<string, IEnumerable<string>> NoHeaders =
        new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal);

    [Theory]
    [InlineData(400, FrontComposerMcpFailureCategory.ValidationFailed)]
    [InlineData(401, FrontComposerMcpFailureCategory.UnknownResource)]
    [InlineData(403, FrontComposerMcpFailureCategory.UnknownResource)]
    [InlineData(404, FrontComposerMcpFailureCategory.UnknownResource)]
    [InlineData(503, FrontComposerMcpFailureCategory.DownstreamFailed)]
    public async Task Query_Maps_Api_Failure_To_Safe_Category_Without_Leaking_Body(
        int status,
        FrontComposerMcpFailureCategory expected)
    {
        IClient client = Substitute.For<IClient>();
        client.ListProjectsAsync(
                Lifecycle.All,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .ThrowsAsync(Api(status));
        var reader = new ProjectsMcpResourceReader(client);

        FrontComposerMcpException ex = await Should.ThrowAsync<FrontComposerMcpException>(
            () => reader.QueryAsync<ProjectsMcpInventoryItem>(
                QueryRequest.Create(
                    new ProjectionQuery(typeof(ProjectsMcpInventoryItem).AssemblyQualifiedName!),
                    "tenant-1"),
                TestContext.Current.CancellationToken));

        ex.Category.ShouldBe(expected);
        ex.Message.ShouldNotContain("secret-problem-detail");
    }

    [Fact]
    public async Task Query_Collapses_CrossTenant_Denial_To_UnknownResource_Without_Sibling_Metadata()
    {
        IClient client = Substitute.For<IClient>();
        client.ListProjectsAsync(
                Lifecycle.All,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HexalithProjectsApiException(
                "cross tenant project-hidden project-visible hidden descriptor",
                403,
                "{\"projectId\":\"project-visible\",\"name\":\"Sibling Secret\",\"denial\":\"sibling detail\"}",
                NoHeaders,
                null!));
        var reader = new ProjectsMcpResourceReader(client);

        FrontComposerMcpException ex = await Should.ThrowAsync<FrontComposerMcpException>(
            () => reader.QueryAsync<ProjectsMcpInventoryItem>(
                QueryRequest.Create(
                    new ProjectionQuery(typeof(ProjectsMcpInventoryItem).AssemblyQualifiedName!),
                    "tenant-b"),
                TestContext.Current.CancellationToken));

        ex.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownResource);
        ex.Message.ShouldNotContain("project-visible");
        ex.Message.ShouldNotContain("Sibling Secret");
        ex.Message.ShouldNotContain("hidden descriptor");
    }

    [Fact]
    public async Task Query_Rethrows_Cancellation_For_FrontComposer_To_Map()
    {
        IClient client = Substitute.For<IClient>();
        client.ListProjectsAsync(
                Lifecycle.All,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());
        var reader = new ProjectsMcpResourceReader(client);

        await Should.ThrowAsync<OperationCanceledException>(
            () => reader.QueryAsync<ProjectsMcpInventoryItem>(
                QueryRequest.Create(
                    new ProjectionQuery(typeof(ProjectsMcpInventoryItem).AssemblyQualifiedName!),
                    "tenant-1"),
                CancellationToken.None));
    }

    [Fact]
    public async Task Dashboard_Counts_Unavailable_Diagnostics_And_Preserves_Healthy_Warnings()
    {
        IClient client = Substitute.For<IClient>();
        client.ListProjectsAsync(
                Lifecycle.All,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(new ProjectListResponse
            {
                Items =
                {
                    ListItem("project-1"),
                    ListItem("project-2"),
                },
            });
        client.GetProjectOperatorDiagnosticsAsync(
                "project-1",
                Arg.Any<int>(),
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(DiagnosticWithExcludedReference("project-1"));
        client.GetProjectOperatorDiagnosticsAsync(
                "project-2",
                Arg.Any<int>(),
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .ThrowsAsync(Api(503));
        var reader = new ProjectsMcpResourceReader(client);

        ProjectsMcpOperationalDashboardItem dashboard =
            (await reader.ReadOperationalDashboardAsync(TestContext.Current.CancellationToken)).Single();
        IReadOnlyList<ProjectsMcpWarningQueueItem> warnings =
            await reader.ReadWarningQueueAsync(25, TestContext.Current.CancellationToken);

        dashboard.TotalVisibleProjects.ShouldBe(2);
        dashboard.ProjectsWithWarnings.ShouldBe(1);
        dashboard.DiagnosticUnavailable.ShouldBe(1);
        ProjectsMcpWarningQueueItem warning = warnings.ShouldHaveSingleItem();
        warning.ProjectId.ShouldBe("project-1");
        warning.DiagnosticUnavailable.ShouldBe(1);
    }

    private static HexalithProjectsApiException Api(int status)
        => new("blocked", status, "{\"problem\":\"secret-problem-detail\"}", NoHeaders, null!);

    private static FreshnessMetadata Fresh()
        => new()
        {
            ReadConsistency = ReadConsistencyClass.Eventually_consistent,
            ObservedAt = DateTimeOffset.UnixEpoch,
            ProjectionWatermark = "1",
            TrustState = ProjectionTrustState.Trusted,
        };

    private static ProjectListItem ListItem(string id)
        => new()
        {
            ProjectId = id,
            Name = id,
            LifecycleState = ProjectLifecycleState.Active,
            UpdatedAt = DateTimeOffset.UnixEpoch,
            Freshness = Fresh(),
        };

    private static ProjectOperatorDiagnostic DiagnosticWithExcludedReference(string id)
        => new()
        {
            ProjectId = id,
            Name = id,
            LifecycleState = ProjectLifecycleState.Active,
            Freshness = Fresh(),
            References =
            {
                new ProjectReferenceSummary
                {
                    ReferenceKind = ProjectReferenceSummaryReferenceKind.Folder,
                    ReferenceState = ProjectReferenceSummaryReferenceState.Excluded,
                    ReferenceId = "ref-1",
                    ReasonCode = "excluded",
                    Freshness = Fresh(),
                },
            },
        };
}
