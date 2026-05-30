// <copyright file="ProjectWarningsDashboardSourceTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Tests.Diagnostics;

using Hexalith.Projects.Client.Generated;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.UI.Diagnostics;
using Hexalith.Projects.UI.Rendering;

using NSubstitute;

using Shouldly;

using Xunit;

using ContractDiagnostic = Hexalith.Projects.Contracts.Models.ProjectOperatorDiagnostic;
using ContractReferenceSummary = Hexalith.Projects.Contracts.Models.ProjectOperatorReferenceSummary;
using GeneratedDiagnostic = Hexalith.Projects.Client.Generated.ProjectOperatorDiagnostic;

/// <summary>
/// Tests for the bounded generated-client warnings dashboard source.
/// </summary>
public sealed class ProjectWarningsDashboardSourceTests
{
    [Fact]
    public async Task SourceUsesVisibleProjectListAndBoundedOperatorDiagnostics()
    {
        IClient client = Substitute.For<IClient>();
        client.ListProjectsAsync(
                Lifecycle.Active,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ListResponse(
                ListItem("project-001", ProjectLifecycleState.Active),
                ListItem("project-002", ProjectLifecycleState.Archived))));
        client.GetProjectOperatorDiagnosticsAsync(
                "project-001",
                25,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Diagnostic(
                "project-001",
                ProjectLifecycleState.Active,
                Reference(ProjectReferenceSummaryReferenceKind.Memory, ProjectReferenceSummaryReferenceState.Stale, "memory-001", "MemoryMatched"))));
        client.GetProjectOperatorDiagnosticsAsync(
                "project-002",
                25,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Diagnostic(
                "project-002",
                ProjectLifecycleState.Archived,
                Reference(ProjectReferenceSummaryReferenceKind.File, ProjectReferenceSummaryReferenceState.Included, "file-001", "FileReferenceMatched"))));

        var source = new ProjectWarningsDashboardSource(client);
        ProjectWarningsDashboardLoadResult result = await source
            .LoadAsync(ProjectLifecycle.Active, CancellationToken.None)
            .ConfigureAwait(true);

        result.Feedback.ShouldBeNull();
        result.InventoryRows.Count.ShouldBe(2);
        result.QueueItems.ShouldHaveSingleItem();
        result.QueueItems[0].State.ShouldBe(ReferenceState.Stale);
        result.QueueItems[0].ReasonCode.ShouldBe(ProjectReasonCode.MemoryMatched);
        result.QueueItems[0].ReferenceKind.ShouldBe("memory");
        result.Dashboard.TotalVisibleProjects.ShouldBe(2);
        result.Dashboard.ProjectsWithWarnings.ShouldBe(1);
        result.Dashboard.StaleReferences.ShouldBe(1);
        await client.Received(1).ListProjectsAsync(
            Lifecycle.Active,
            Arg.Is<string>(s => !string.IsNullOrWhiteSpace(s)),
            ReadConsistencyClass.Eventually_consistent,
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await client.Received(1).GetProjectOperatorDiagnosticsAsync(
            "project-001",
            25,
            Arg.Is<string>(s => !string.IsNullOrWhiteSpace(s)),
            ReadConsistencyClass.Eventually_consistent,
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task SourcePreservesLoadedRowsWhenOneDiagnosticEnrichmentFails()
    {
        IClient client = Substitute.For<IClient>();
        client.ListProjectsAsync(
                null,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ListResponse(
                ListItem("project-001", ProjectLifecycleState.Active),
                ListItem("project-002", ProjectLifecycleState.Active))));
        client.GetProjectOperatorDiagnosticsAsync(
                "project-001",
                25,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Diagnostic(
                "project-001",
                ProjectLifecycleState.Active,
                Reference(ProjectReferenceSummaryReferenceKind.Folder, ProjectReferenceSummaryReferenceState.Conflict, "folder-001", null))));
        client.GetProjectOperatorDiagnosticsAsync(
                "project-002",
                25,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns<Task<GeneratedDiagnostic>>(_ => throw ApiException(503));

        var source = new ProjectWarningsDashboardSource(client);
        ProjectWarningsDashboardLoadResult result = await source
            .LoadAsync(null, CancellationToken.None)
            .ConfigureAwait(true);

        result.Feedback.ShouldBeNull();
        result.QueueItems.Count.ShouldBe(2);
        result.QueueItems.ShouldContain(item => item.State == ReferenceState.Conflict);
        ProjectWarningQueueItemProjection unavailable = result.QueueItems.Single(item => item.ProjectId == "project-002");
        unavailable.State.ShouldBe(ReferenceState.Unavailable);
        unavailable.SourceSection.ShouldContain("data_unavailable");
        unavailable.SafeActionAvailabilityLabel.ShouldContain("Story 5.9");
        result.Dashboard.DiagnosticUnavailable.ShouldBe(1);
        result.Dashboard.Conflicts.ShouldBe(1);
    }

    [Theory]
    [InlineData(400, ProjectConsoleFeedback.ErrorCategory, "validation_error")]
    [InlineData(401, ProjectConsoleFeedback.FailClosedCategory, "safe_denial")]
    [InlineData(403, ProjectConsoleFeedback.FailClosedCategory, "safe_denial")]
    [InlineData(404, ProjectConsoleFeedback.FailClosedCategory, "safe_denial")]
    [InlineData(503, ProjectConsoleFeedback.WarningCategory, "data_unavailable")]
    [InlineData(500, ProjectConsoleFeedback.ErrorCategory, "warnings_dashboard_query_failed")]
    public async Task SourceMapsBaseListFailuresToSafeFeedback(int statusCode, string category, string reasonCode)
    {
        IClient client = Substitute.For<IClient>();
        client.ListProjectsAsync(
                null,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns<Task<ProjectListResponse>>(_ => throw ApiException(statusCode));

        var source = new ProjectWarningsDashboardSource(client);
        ProjectWarningsDashboardLoadResult result = await source
            .LoadAsync(null, CancellationToken.None)
            .ConfigureAwait(true);

        result.InventoryRows.ShouldBeEmpty();
        result.QueueItems.ShouldBeEmpty();
        result.Feedback.ShouldNotBeNull();
        result.Feedback.Category.ShouldBe(category);
        result.Feedback.SafeReasonCode.ShouldBe(reasonCode);
        result.Feedback.Message.ShouldNotContain("secret");
        result.Feedback.Message.ShouldNotContain("token");
    }

    [Fact]
    public void MapperSurfacesUnknownStateAsSafeUnavailableWarning()
    {
        ProjectInventoryRowProjection project = InventoryRow("project-001", ProjectLifecycle.Active);
        var diagnostic = new ContractDiagnostic(
            "project-001",
            "Console Project",
            null,
            "active",
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddMinutes(1),
            null,
            null,
            new ProjectOperatorContextActivation(true, null),
            [
                new ContractReferenceSummary(
                    "memory",
                    "__new_state__",
                    "memory-001",
                    "Memory",
                    "__new_reason__",
                    Freshness()),
            ],
            [],
            Freshness());

        IReadOnlyList<ProjectWarningQueueItemProjection> items =
            ProjectWarningsDashboardMapper.BuildQueueItems(project, diagnostic);

        ProjectWarningQueueItemProjection item = items.ShouldHaveSingleItem();
        item.State.ShouldBe(ReferenceState.Unavailable);
        item.ReasonCode.ShouldBeNull();
        item.SourceSection.ShouldContain("unknown-state");
        item.SourceSection.ShouldContain("unknown-reason");
    }

    private static ProjectListResponse ListResponse(params ProjectListItem[] items)
        => new()
        {
            Items = items,
            Freshness = GeneratedFreshness(),
        };

    private static ProjectListItem ListItem(string projectId, ProjectLifecycleState lifecycle)
        => new()
        {
            ProjectId = projectId,
            Name = $"Project {projectId}",
            LifecycleState = lifecycle,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch.AddMinutes(1),
            Freshness = GeneratedFreshness(),
        };

    private static GeneratedDiagnostic Diagnostic(
        string projectId,
        ProjectLifecycleState lifecycle,
        params ProjectReferenceSummary[] references)
        => new()
        {
            ProjectId = projectId,
            Name = $"Project {projectId}",
            LifecycleState = lifecycle,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch.AddMinutes(1),
            References = references.ToList(),
            Freshness = GeneratedFreshness(),
        };

    private static ProjectReferenceSummary Reference(
        ProjectReferenceSummaryReferenceKind kind,
        ProjectReferenceSummaryReferenceState state,
        string referenceId,
        string? reasonCode)
        => new()
        {
            ReferenceKind = kind,
            ReferenceState = state,
            ReferenceId = referenceId,
            DisplayName = referenceId,
            ReasonCode = reasonCode,
            Freshness = GeneratedFreshness(),
        };

    private static FreshnessMetadata GeneratedFreshness()
        => new()
        {
            ReadConsistency = ReadConsistencyClass.Eventually_consistent,
            ObservedAt = DateTimeOffset.UnixEpoch,
            ProjectionWatermark = "watermark-001",
            Stale = false,
            TrustState = ProjectionTrustState.Trusted,
        };

    private static ProjectInventoryRowProjection InventoryRow(string projectId, ProjectLifecycle lifecycle)
        => new()
        {
            Id = projectId,
            ProjectId = projectId,
            Name = "Console Project",
            Lifecycle = lifecycle,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch.AddMinutes(1),
            FreshnessTrustState = "trusted",
            ProjectionWatermark = "watermark-001",
        };

    private static ProjectOperatorFreshnessMetadata Freshness()
        => new("eventually_consistent", DateTimeOffset.UnixEpoch, "watermark-001", false, "trusted");

    private static HexalithProjectsApiException ApiException(int statusCode)
        => new(
            "unsafe response hidden",
            statusCode,
            "secret token transcript body",
            new Dictionary<string, IEnumerable<string>>(),
            null!);
}
