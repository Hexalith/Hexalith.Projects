// <copyright file="ProjectsMcpResourceReaderTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Mcp.Tests;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.Projects.Client.Generated;
using Hexalith.Projects.Mcp;

using EvidenceFreshnessStateCode = Hexalith.Projects.Contracts.Models.EvidenceFreshnessStateCode;

using NSubstitute;

using Shouldly;

using Xunit;

public sealed class ProjectsMcpResourceReaderTests
{
    [Fact]
    public async Task Inventory_Uses_Eventual_Freshness_And_Returns_Safe_Metadata()
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
                    new ProjectListItem
                    {
                        ProjectId = "project-1",
                        Name = "Ops",
                        LifecycleState = ProjectLifecycleState.Active,
                        UpdatedAt = DateTimeOffset.UnixEpoch,
                        Freshness = new FreshnessMetadata
                        {
                            ReadConsistency = ReadConsistencyClass.Eventually_consistent,
                            ObservedAt = DateTimeOffset.UnixEpoch,
                            ProjectionWatermark = "42",
                            TrustState = ProjectionTrustState.Trusted,
                        },
                    },
                },
            });

        var reader = new ProjectsMcpResourceReader(client);
        IReadOnlyList<ProjectsMcpInventoryItem> rows = await reader.ReadInventoryAsync(25, CancellationToken.None);

        rows.Single().ProjectId.ShouldBe("project-1");
        rows.Single().TenantScope.ShouldBe("server-derived tenant");
        rows.Single().FreshnessTrustState.ShouldBe("trusted");
        rows.Single().PayloadExcluded.ShouldBeTrue();
        rows.Single().ShortExplanation.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(ProjectionTrustState.Trusted, EvidenceFreshnessStateCode.Current)]
    [InlineData(ProjectionTrustState.Stale, EvidenceFreshnessStateCode.Stale)]
    [InlineData(ProjectionTrustState.Unavailable, EvidenceFreshnessStateCode.Unavailable)]
    public async Task ReferenceHealth_Normalizes_Producer_Freshness(
        ProjectionTrustState trustState,
        string expectedFreshness)
    {
        IClient client = Substitute.For<IClient>();
        client.GetProjectOperatorDiagnosticsAsync(
                "project-1",
                25,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(DiagnosticWithWarnings("project-1", trustState));
        var reader = new ProjectsMcpResourceReader(client);

        IReadOnlyList<ProjectsMcpReferenceHealthItem> rows = await reader.ReadReferenceHealthAsync(
            "project-1",
            25,
            TestContext.Current.CancellationToken);

        rows.ShouldAllBe(row => row.FreshnessTrustState == expectedFreshness);
    }

    [Fact]
    public async Task Query_Inventory_Bounds_Results_To_Canonical_Take()
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
                    ListItem("project-3"),
                },
            });
        var reader = new ProjectsMcpResourceReader(client);

        QueryResult<ProjectsMcpInventoryItem> result = await reader.QueryAsync<ProjectsMcpInventoryItem>(
            QueryRequest.Create(
                new ProjectionQuery(
                    typeof(ProjectsMcpInventoryItem).AssemblyQualifiedName!,
                    Take: 2),
                "tenant-1"),
            TestContext.Current.CancellationToken);

        result.Items.Count.ShouldBe(2);
        result.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task Query_WarningQueue_Bounds_Results_To_Canonical_Take()
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
                    ListItem("project-3"),
                },
            });
        client.GetProjectOperatorDiagnosticsAsync(
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<string>(0) == "project-2"
                ? throw new InvalidOperationException("unsafe diagnostic failure detail")
                : DiagnosticWithWarnings(call.ArgAt<string>(0)));
        var reader = new ProjectsMcpResourceReader(client);

        QueryResult<ProjectsMcpWarningQueueItem> result = await reader.QueryAsync<ProjectsMcpWarningQueueItem>(
            QueryRequest.Create(
                new ProjectionQuery(
                    typeof(ProjectsMcpWarningQueueItem).AssemblyQualifiedName!,
                    Take: 1),
                "tenant-1"),
            TestContext.Current.CancellationToken);

        result.Items.Count.ShouldBe(1);
        result.TotalCount.ShouldBe(4);
        result.Items.ShouldAllBe(item => item.FreshnessTrustState == EvidenceFreshnessStateCode.Current);
        result.Items.ShouldAllBe(item => item.DiagnosticUnavailable == 1);
        await client.Received(1).GetProjectOperatorDiagnosticsAsync(
            "project-1",
            25,
            Arg.Any<string>(),
            ReadConsistencyClass.Eventually_consistent,
            Arg.Any<CancellationToken>());
        await client.Received(1).GetProjectOperatorDiagnosticsAsync(
            "project-2",
            25,
            Arg.Any<string>(),
            ReadConsistencyClass.Eventually_consistent,
            Arg.Any<CancellationToken>());
        await client.Received(1).GetProjectOperatorDiagnosticsAsync(
            "project-3",
            25,
            Arg.Any<string>(),
            ReadConsistencyClass.Eventually_consistent,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WarningResourcesDiagnoseOrdinalFirstWindowAndDashboardKeepsFullInventoryTotals()
    {
        IClient client = Substitute.For<IClient>();
        ProjectListItem[] visibleProjects = Enumerable.Range(1, 30)
            .Reverse()
            .Select(static index => ListItem($"project-{index:000}"))
            .ToArray();
        var list = new ProjectListResponse();
        foreach (ProjectListItem project in visibleProjects)
        {
            list.Items.Add(project);
        }

        client.ListProjectsAsync(
                Lifecycle.All,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(list);
        var diagnosedProjectIds = new List<string>();
        client.GetProjectOperatorDiagnosticsAsync(
                Arg.Any<string>(),
                25,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                string projectId = call.ArgAt<string>(0);
                diagnosedProjectIds.Add(projectId);
                return DiagnosticWithWarnings(projectId);
            });
        var reader = new ProjectsMcpResourceReader(client);

        QueryResult<ProjectsMcpWarningScanSummaryItem> result =
            await reader.QueryAsync<ProjectsMcpWarningScanSummaryItem>(
                QueryRequest.Create(
                    new ProjectionQuery(typeof(ProjectsMcpWarningScanSummaryItem).AssemblyQualifiedName!),
                    "tenant-1"),
                TestContext.Current.CancellationToken);

        string[] expectedScan = Enumerable.Range(1, 25)
            .Select(static index => $"project-{index:000}")
            .ToArray();
        diagnosedProjectIds.ShouldBe(expectedScan, ignoreOrder: false);
        result.TotalCount.ShouldBe(1);
        ProjectsMcpWarningScanSummaryItem summary = result.Items.ShouldHaveSingleItem();
        summary.ScannedProjectCount.ShouldBe(25);
        summary.DiagnosticUnavailable.ShouldBe(0);
        summary.TenantScope.ShouldBe("server-derived tenant");
        summary.PayloadExcluded.ShouldBeTrue();

        diagnosedProjectIds.Clear();
        QueryResult<ProjectsMcpOperationalDashboardItem> dashboardResult =
            await reader.QueryAsync<ProjectsMcpOperationalDashboardItem>(
                QueryRequest.Create(
                    new ProjectionQuery(typeof(ProjectsMcpOperationalDashboardItem).AssemblyQualifiedName!),
                    "tenant-1"),
                TestContext.Current.CancellationToken);

        diagnosedProjectIds.ShouldBe(expectedScan, ignoreOrder: false);
        ProjectsMcpOperationalDashboardItem dashboard = dashboardResult.Items.ShouldHaveSingleItem();
        dashboard.TotalVisibleProjects.ShouldBe(30);
        dashboard.ActiveProjects.ShouldBe(30);
        dashboard.ArchivedProjects.ShouldBe(0);
        dashboard.ProjectsWithWarnings.ShouldBe(25);
        dashboard.DiagnosticUnavailable.ShouldBe(0);
    }

    private static ProjectOperatorDiagnostic DiagnosticWithWarnings(
        string projectId,
        ProjectionTrustState trustState = ProjectionTrustState.Trusted)
        => new()
        {
            ProjectId = projectId,
            Name = projectId,
            LifecycleState = ProjectLifecycleState.Active,
            Freshness = Fresh(),
            References =
            {
                ExcludedReference("ref-1", trustState),
                ExcludedReference("ref-2", trustState),
            },
        };

    private static ProjectReferenceSummary ExcludedReference(
        string referenceId,
        ProjectionTrustState trustState = ProjectionTrustState.Trusted)
        => new()
        {
            ReferenceKind = ProjectReferenceSummaryReferenceKind.Folder,
            ReferenceState = ProjectReferenceSummaryReferenceState.Excluded,
            ReferenceId = referenceId,
            ReasonCode = "excluded",
            Freshness = Fresh(trustState),
        };

    private static FreshnessMetadata Fresh(ProjectionTrustState trustState = ProjectionTrustState.Trusted)
        => new()
        {
            ReadConsistency = ReadConsistencyClass.Eventually_consistent,
            ObservedAt = DateTimeOffset.UnixEpoch,
            ProjectionWatermark = "1",
            TrustState = trustState,
        };

    private static ProjectListItem ListItem(string projectId)
        => new()
        {
            ProjectId = projectId,
            Name = projectId,
            LifecycleState = ProjectLifecycleState.Active,
            UpdatedAt = DateTimeOffset.UnixEpoch,
            Freshness = Fresh(),
        };
}
