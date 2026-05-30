// <copyright file="ProjectInventorySourceTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Tests.Diagnostics;

using Hexalith.Projects.Client.Generated;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.UI.Diagnostics;
using Hexalith.Projects.UI.Rendering;

using NSubstitute;

using Shouldly;

using Xunit;

/// <summary>
/// Tests for the generated-client backed inventory source.
/// </summary>
public sealed class ProjectInventorySourceTests
{
    [Fact]
    public async Task SourceUsesListProjectsWithEventualFreshnessAndLifecycleFilter()
    {
        IClient client = Substitute.For<IClient>();
        client.ListProjectsAsync(
                Lifecycle.Active,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateResponse()));

        var source = new ProjectInventorySource(client);
        ProjectInventoryLoadResult result = await source
            .ListProjectsAsync(ProjectLifecycle.Active, CancellationToken.None)
            .ConfigureAwait(true);

        result.Feedback.ShouldBeNull();
        result.Rows.ShouldHaveSingleItem();
        result.Rows[0].ProjectId.ShouldBe("project-001");
        result.Rows[0].Name.ShouldBe("Inventory Project");
        result.Rows[0].Lifecycle.ShouldBe(ProjectLifecycle.Active);
        result.Rows[0].TenantScope.ShouldBe("server-derived tenant");
        result.Rows[0].FreshnessTrustState.ShouldBe("trusted");
        await client.Received(1).ListProjectsAsync(
            Lifecycle.Active,
            Arg.Is<string>(s => !string.IsNullOrWhiteSpace(s)),
            ReadConsistencyClass.Eventually_consistent,
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Theory]
    [InlineData(400, ProjectConsoleFeedback.ErrorCategory, "validation_error")]
    [InlineData(404, ProjectConsoleFeedback.FailClosedCategory, "safe_denial")]
    [InlineData(503, ProjectConsoleFeedback.WarningCategory, "data_unavailable")]
    [InlineData(500, ProjectConsoleFeedback.ErrorCategory, "inventory_query_failed")]
    public async Task SourceMapsFailuresToSafeFeedback(int statusCode, string category, string reasonCode)
    {
        IClient client = Substitute.For<IClient>();
        client.ListProjectsAsync(
                null,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns<Task<ProjectListResponse>>(_ => throw new HexalithProjectsApiException(
                "unsafe response hidden",
                statusCode,
                "secret token transcript body",
                new Dictionary<string, IEnumerable<string>>(),
                null!));

        var source = new ProjectInventorySource(client);
        ProjectInventoryLoadResult result = await source
            .ListProjectsAsync(null, CancellationToken.None)
            .ConfigureAwait(true);

        result.Rows.ShouldBeEmpty();
        result.Feedback.ShouldNotBeNull();
        result.Feedback.Category.ShouldBe(category);
        result.Feedback.SafeReasonCode.ShouldBe(reasonCode);
        result.Feedback.Message.ShouldNotContain("secret");
        result.Feedback.Message.ShouldNotContain("token");
        result.Feedback.Message.ShouldNotContain("transcript");
    }

    [Fact]
    public async Task SourceMapsTransportFailuresToSafeFeedbackWithoutCrashing()
    {
        IClient client = Substitute.For<IClient>();
        client.ListProjectsAsync(
                null,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns<Task<ProjectListResponse>>(_ => throw new System.Net.Http.HttpRequestException("connection refused 10.0.0.1 secret"));

        var source = new ProjectInventorySource(client);
        ProjectInventoryLoadResult result = await source
            .ListProjectsAsync(null, CancellationToken.None)
            .ConfigureAwait(true);

        result.Rows.ShouldBeEmpty();
        result.Feedback.ShouldNotBeNull();
        result.Feedback.Category.ShouldBe(ProjectConsoleFeedback.ErrorCategory);
        result.Feedback.SafeReasonCode.ShouldBe("inventory_query_failed");
        result.Feedback.Message.ShouldNotContain("secret");
        result.Feedback.Message.ShouldNotContain("10.0.0.1");
    }

    private static ProjectListResponse CreateResponse()
        => new()
        {
            Items =
            [
                new ProjectListItem
                {
                    ProjectId = "project-001",
                    Name = "Inventory Project",
                    LifecycleState = ProjectLifecycleState.Active,
                    CreatedAt = DateTimeOffset.UnixEpoch,
                    UpdatedAt = DateTimeOffset.UnixEpoch.AddMinutes(1),
                    Freshness = Freshness(),
                },
            ],
            Freshness = Freshness(),
        };

    private static FreshnessMetadata Freshness()
        => new()
        {
            ReadConsistency = ReadConsistencyClass.Eventually_consistent,
            ObservedAt = DateTimeOffset.UnixEpoch,
            ProjectionWatermark = "watermark-001",
            Stale = false,
            TrustState = ProjectionTrustState.Trusted,
        };
}
