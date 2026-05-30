// <copyright file="ProjectsMcpResourceReaderTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Mcp.Tests;

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
}
