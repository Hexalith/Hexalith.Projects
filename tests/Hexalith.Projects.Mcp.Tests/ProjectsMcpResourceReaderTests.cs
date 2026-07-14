// <copyright file="ProjectsMcpResourceReaderTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Mcp.Tests;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.Projects.Client.Generated;
using Hexalith.Projects.Mcp;

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
        rows.Single().PayloadExcluded.ShouldBeTrue();
        rows.Single().ShortExplanation.ShouldNotBeNullOrWhiteSpace();
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
                },
            });
        client.GetProjectOperatorDiagnosticsAsync(
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(call => DiagnosticWithWarnings(call.ArgAt<string>(0)));
        var reader = new ProjectsMcpResourceReader(client);

        QueryResult<ProjectsMcpWarningQueueItem> result = await reader.QueryAsync<ProjectsMcpWarningQueueItem>(
            QueryRequest.Create(
                new ProjectionQuery(
                    typeof(ProjectsMcpWarningQueueItem).AssemblyQualifiedName!,
                    Take: 2),
                "tenant-1"),
            TestContext.Current.CancellationToken);

        result.Items.Count.ShouldBe(2);
        result.TotalCount.ShouldBe(2);
    }

    private static ProjectOperatorDiagnostic DiagnosticWithWarnings(string projectId)
        => new()
        {
            ProjectId = projectId,
            Name = projectId,
            LifecycleState = ProjectLifecycleState.Active,
            Freshness = Fresh(),
            References =
            {
                ExcludedReference("ref-1"),
                ExcludedReference("ref-2"),
            },
        };

    private static ProjectReferenceSummary ExcludedReference(string referenceId)
        => new()
        {
            ReferenceKind = ProjectReferenceSummaryReferenceKind.Folder,
            ReferenceState = ProjectReferenceSummaryReferenceState.Excluded,
            ReferenceId = referenceId,
            ReasonCode = "excluded",
            Freshness = Fresh(),
        };

    private static FreshnessMetadata Fresh()
        => new()
        {
            ReadConsistency = ReadConsistencyClass.Eventually_consistent,
            ObservedAt = DateTimeOffset.UnixEpoch,
            ProjectionWatermark = "1",
            TrustState = ProjectionTrustState.Trusted,
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
