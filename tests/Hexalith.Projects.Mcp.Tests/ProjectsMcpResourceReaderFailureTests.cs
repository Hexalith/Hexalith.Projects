// <copyright file="ProjectsMcpResourceReaderFailureTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Mcp.Tests;

using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Mcp;
using Hexalith.Projects.Client.Generated;
using Hexalith.Projects.Mcp;

using EvidenceFreshnessStateCode = Hexalith.Projects.Contracts.Models.EvidenceFreshnessStateCode;

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
    public async Task Query_Warnings_And_Dashboard_Count_Unavailable_Diagnostics_And_Preserve_Healthy_Warnings()
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
                25,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(DiagnosticWithExcludedReference("project-1"));
        client.GetProjectOperatorDiagnosticsAsync(
                "project-2",
                25,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .ThrowsAsync(Api(503));
        var reader = new ProjectsMcpResourceReader(client);

        QueryResult<ProjectsMcpWarningQueueItem> warningResult =
            await reader.QueryAsync<ProjectsMcpWarningQueueItem>(
                QueryRequest.Create(
                    new ProjectionQuery(
                        typeof(ProjectsMcpWarningQueueItem).AssemblyQualifiedName!,
                        Take: 25),
                    "tenant-1"),
                TestContext.Current.CancellationToken);
        QueryResult<ProjectsMcpOperationalDashboardItem> dashboardResult =
            await reader.QueryAsync<ProjectsMcpOperationalDashboardItem>(
                QueryRequest.Create(
                    new ProjectionQuery(typeof(ProjectsMcpOperationalDashboardItem).AssemblyQualifiedName!),
                    "tenant-1"),
                TestContext.Current.CancellationToken);

        warningResult.TotalCount.ShouldBe(1);
        ProjectsMcpWarningQueueItem warning = warningResult.Items.ShouldHaveSingleItem();
        warning.ProjectId.ShouldBe("project-1");
        warning.ProjectName.ShouldBe("project-1");
        warning.LifecycleState.ShouldBe("active");
        warning.ReferenceKind.ShouldBe("folder");
        warning.ReferenceId.ShouldBe("ref-1");
        warning.ReferenceState.ShouldBe("excluded");
        warning.ReasonCode.ShouldBe("excluded");
        warning.DiagnosticUnavailable.ShouldBe(1);
        warning.FreshnessTrustState.ShouldBe(EvidenceFreshnessStateCode.Current);
        warning.TenantScope.ShouldBe("server-derived tenant");
        warning.PayloadExcluded.ShouldBeTrue();
        warning.ShortExplanation.ShouldNotBeNullOrWhiteSpace();

        dashboardResult.TotalCount.ShouldBe(1);
        ProjectsMcpOperationalDashboardItem dashboard = dashboardResult.Items.ShouldHaveSingleItem();
        dashboard.TotalVisibleProjects.ShouldBe(2);
        dashboard.ActiveProjects.ShouldBe(2);
        dashboard.ArchivedProjects.ShouldBe(0);
        dashboard.ProjectsWithWarnings.ShouldBe(1);
        dashboard.DiagnosticUnavailable.ShouldBe(1);
        dashboard.TenantScope.ShouldBe("server-derived tenant");
        dashboard.PayloadExcluded.ShouldBeTrue();
        dashboard.ShortExplanation.ShouldNotBeNullOrWhiteSpace();

        string serialized = JsonSerializer.Serialize(new
        {
            Warnings = warningResult.Items,
            Dashboard = dashboardResult.Items,
        });
        serialized.ShouldNotContain("unsafe-exception-detail");
        serialized.ShouldNotContain("secret-problem-detail");
    }

    [Fact]
    public async Task Query_EmptyWarningQueue_StillEmitsScanSummaryWithUnavailableCount()
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
                25,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(DiagnosticWithoutWarnings("project-1"));
        client.GetProjectOperatorDiagnosticsAsync(
                "project-2",
                25,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .ThrowsAsync(Api(503));
        var reader = new ProjectsMcpResourceReader(client);

        QueryResult<ProjectsMcpWarningQueueItem> warningResult =
            await reader.QueryAsync<ProjectsMcpWarningQueueItem>(
                QueryRequest.Create(
                    new ProjectionQuery(typeof(ProjectsMcpWarningQueueItem).AssemblyQualifiedName!),
                    "tenant-1"),
                TestContext.Current.CancellationToken);
        QueryResult<ProjectsMcpWarningScanSummaryItem> summaryResult =
            await reader.QueryAsync<ProjectsMcpWarningScanSummaryItem>(
                QueryRequest.Create(
                    new ProjectionQuery(typeof(ProjectsMcpWarningScanSummaryItem).AssemblyQualifiedName!),
                    "tenant-1"),
                TestContext.Current.CancellationToken);

        warningResult.Items.ShouldBeEmpty();
        warningResult.TotalCount.ShouldBe(0);
        summaryResult.TotalCount.ShouldBe(1);
        ProjectsMcpWarningScanSummaryItem summary = summaryResult.Items.ShouldHaveSingleItem();
        summary.ScannedProjectCount.ShouldBe(2);
        summary.DiagnosticUnavailable.ShouldBe(1);
        summary.ShortExplanation.ShouldNotContain("unsafe-exception-detail");
        summary.ShortExplanation.ShouldNotContain("secret-problem-detail");
        summary.PayloadExcluded.ShouldBeTrue();
    }

    [Fact]
    public async Task Query_DashboardUsesOneInventorySnapshotForAllCountersAndWarningScan()
    {
        IClient client = Substitute.For<IClient>();
        var firstSnapshot = new ProjectListResponse
        {
            Items =
            {
                ListItem("project-1", ProjectLifecycleState.Active),
                ListItem("project-2", ProjectLifecycleState.Archived),
            },
        };
        var secondSnapshot = new ProjectListResponse
        {
            Items =
            {
                ListItem("project-999", ProjectLifecycleState.Active),
            },
        };
        client.ListProjectsAsync(
                Lifecycle.All,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(firstSnapshot, secondSnapshot);
        client.GetProjectOperatorDiagnosticsAsync(
                "project-1",
                25,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(DiagnosticWithExcludedReference("project-1"));
        client.GetProjectOperatorDiagnosticsAsync(
                "project-2",
                25,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(DiagnosticWithoutWarnings("project-2"));
        var reader = new ProjectsMcpResourceReader(client);

        QueryResult<ProjectsMcpOperationalDashboardItem> result =
            await reader.QueryAsync<ProjectsMcpOperationalDashboardItem>(
                QueryRequest.Create(
                    new ProjectionQuery(typeof(ProjectsMcpOperationalDashboardItem).AssemblyQualifiedName!),
                    "tenant-1"),
                TestContext.Current.CancellationToken);

        ProjectsMcpOperationalDashboardItem dashboard = result.Items.ShouldHaveSingleItem();
        dashboard.TotalVisibleProjects.ShouldBe(2);
        dashboard.ActiveProjects.ShouldBe(1);
        dashboard.ArchivedProjects.ShouldBe(1);
        dashboard.ProjectsWithWarnings.ShouldBe(1);
        dashboard.DiagnosticUnavailable.ShouldBe(0);
        await client.Received(1).ListProjectsAsync(
            Lifecycle.All,
            Arg.Any<string>(),
            ReadConsistencyClass.Eventually_consistent,
            Arg.Any<CancellationToken>());
        await client.DidNotReceive().GetProjectOperatorDiagnosticsAsync(
            "project-999",
            Arg.Any<int>(),
            Arg.Any<string>(),
            ReadConsistencyClass.Eventually_consistent,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Query_WarningScanRethrowsCancellationWithoutContinuing()
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
                25,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());
        var reader = new ProjectsMcpResourceReader(client);

        await Should.ThrowAsync<OperationCanceledException>(
            () => reader.QueryAsync<ProjectsMcpWarningScanSummaryItem>(
                QueryRequest.Create(
                    new ProjectionQuery(typeof(ProjectsMcpWarningScanSummaryItem).AssemblyQualifiedName!),
                    "tenant-1"),
                TestContext.Current.CancellationToken));
        await client.DidNotReceive().GetProjectOperatorDiagnosticsAsync(
            "project-2",
            Arg.Any<int>(),
            Arg.Any<string>(),
            ReadConsistencyClass.Eventually_consistent,
            Arg.Any<CancellationToken>());
    }

    private static HexalithProjectsApiException Api(int status)
        => new("unsafe-exception-detail", status, "{\"problem\":\"secret-problem-detail\"}", NoHeaders, null!);

    private static FreshnessMetadata Fresh()
        => new()
        {
            ReadConsistency = ReadConsistencyClass.Eventually_consistent,
            ObservedAt = DateTimeOffset.UnixEpoch,
            ProjectionWatermark = "1",
            TrustState = ProjectionTrustState.Trusted,
        };

    private static ProjectListItem ListItem(
        string id,
        ProjectLifecycleState lifecycleState = ProjectLifecycleState.Active)
        => new()
        {
            ProjectId = id,
            Name = id,
            LifecycleState = lifecycleState,
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

    private static ProjectOperatorDiagnostic DiagnosticWithoutWarnings(string id)
        => new()
        {
            ProjectId = id,
            Name = id,
            LifecycleState = ProjectLifecycleState.Active,
            Freshness = Fresh(),
        };
}
