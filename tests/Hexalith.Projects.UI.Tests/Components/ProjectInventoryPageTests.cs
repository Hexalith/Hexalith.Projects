// <copyright file="ProjectInventoryPageTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Tests.Components;

using Bunit;

using Hexalith.FrontComposer.Testing;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.UI.Components.Pages;
using Hexalith.Projects.UI.Diagnostics;
using Hexalith.Projects.UI.Rendering;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

using Xunit;

/// <summary>
/// bUnit tests for the Projects inventory entry point.
/// </summary>
public sealed class ProjectInventoryPageTests : FrontComposerTestBase
{
    [Fact]
    public void InventoryRendersRowsFiltersSelectorsAndDetailLinks()
    {
        IProjectWarningsDashboardSource source = Substitute.For<IProjectWarningsDashboardSource>();
        source.LoadAsync(null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectWarningsDashboardLoadResult.FromRows(
                [Row()],
                [Warning()],
                Dashboard())));
        Services.AddSingleton(source);

        IRenderedComponent<Home> cut = Render<Home>();

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-inventory-grid']").ShouldNotBeNull());
        cut.Find("[data-testid='project-inventory-filter-lifecycle']").ShouldNotBeNull();
        cut.Find("[data-testid='project-warning-filter-state']").ShouldNotBeNull();
        cut.Find("[data-testid='project-warning-filter-reference-type']").ShouldNotBeNull();
        cut.Find("[data-testid='project-inventory-row']").TextContent.ShouldContain("Inventory Project");
        cut.Find("[data-testid='project-inventory-row-link']").GetAttribute("href").ShouldBe("/projects/project-001");
        cut.Markup.ShouldContain("server-derived tenant");
        cut.Markup.ShouldContain(ProjectInventoryRowProjection.WarningSummaryUnavailable);
        cut.Find("[data-testid='project-warnings-dashboard']").TextContent.ShouldContain("Warning projects");
        cut.Find("[data-testid='project-warnings-queue']").TextContent.ShouldContain("Warnings queue");
        cut.Find("[data-testid='project-warning-row']").TextContent.ShouldContain("memory-001");
        cut.Find("[data-testid='project-warning-safe-action']").TextContent.ShouldContain("Handled by Story 5.9");
    }

    [Fact]
    public void InventoryRendersSafeFeedbackForDeniedLoad()
    {
        IProjectWarningsDashboardSource source = Substitute.For<IProjectWarningsDashboardSource>();
        source.LoadAsync(null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectWarningsDashboardLoadResult.FromFeedback(
                ProjectConsoleFeedback.FailClosed("safe_denial", "corr-001"))));
        Services.AddSingleton(source);

        IRenderedComponent<Home> cut = Render<Home>();

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-feedback-fail-closed']").TextContent.ShouldContain("safe_denial"));
        cut.Markup.ShouldNotContain("secret");
        cut.Markup.ShouldNotContain("token");
    }

    [Fact]
    public void InventoryRendersEmptyStateWhenNoProjects()
    {
        IProjectWarningsDashboardSource source = Substitute.For<IProjectWarningsDashboardSource>();
        source.LoadAsync(null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectWarningsDashboardLoadResult.FromRows([], [], Dashboard())));
        Services.AddSingleton(source);

        IRenderedComponent<Home> cut = Render<Home>();

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='project-inventory-empty']").TextContent.ShouldContain("No projects"));
        cut.Find("[data-testid='project-warning-empty']").TextContent.ShouldContain("No warnings");
        cut.FindAll("[data-testid='project-inventory-row']").ShouldBeEmpty();
        cut.FindAll("[data-testid='project-inventory-grid']").ShouldBeEmpty();
    }

    [Fact]
    public void WarningsQueueFiltersByStateReasonAndReferenceType()
    {
        IProjectWarningsDashboardSource source = Substitute.For<IProjectWarningsDashboardSource>();
        source.LoadAsync(null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectWarningsDashboardLoadResult.FromRows(
                [Row()],
                [
                    Warning("memory-001", ReferenceState.Stale, ProjectReasonCode.MemoryMatched, "memory"),
                    Warning("file-001", ReferenceState.Conflict, ProjectReasonCode.FileReferenceMatched, "file"),
                ],
                Dashboard())));
        Services.AddSingleton(source);

        IRenderedComponent<Home> cut = Render<Home>();

        cut.WaitForAssertion(() => cut.FindAll("[data-testid='project-warning-row']").Count.ShouldBe(2));
        cut.Find("[data-testid='project-warning-filter-state']").Change(ReferenceState.Stale.ToString());
        cut.FindAll("[data-testid='project-warning-row']").Count.ShouldBe(1);
        cut.Find("[data-testid='project-warning-row']").TextContent.ShouldContain("memory-001");

        cut.Find("[data-testid='project-warning-filter-reason']").Change(ProjectReasonCode.MemoryMatched.ToString());
        cut.FindAll("[data-testid='project-warning-row']").Count.ShouldBe(1);

        cut.Find("[data-testid='project-warning-filter-reference-type']").Change("file");
        cut.Find("[data-testid='project-warning-empty']").TextContent.ShouldContain("Filter returned no results");
    }

    [Fact]
    public void GroupedDashboardTileDrillInKeepsEveryCountedState()
    {
        IProjectWarningsDashboardSource source = Substitute.For<IProjectWarningsDashboardSource>();
        source.LoadAsync(null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectWarningsDashboardLoadResult.FromRows(
                [Row()],
                [
                    Warning("memory-001", ReferenceState.Unauthorized, null, "memory"),
                    Warning("file-001", ReferenceState.Unavailable, null, "file"),
                    Warning("folder-001", ReferenceState.Stale, ProjectReasonCode.ProjectFolderMatched, "folder"),
                ],
                Dashboard())));
        Services.AddSingleton(source);

        IRenderedComponent<Home> cut = Render<Home>();

        cut.WaitForAssertion(() => cut.FindAll("[data-testid='project-warning-row']").Count.ShouldBe(3));

        cut.FindAll("[data-testid='project-dashboard-tile']")
            .Single(tile => tile.TextContent.Contains("Denied/unavailable"))
            .Click();

        // The "Denied/unavailable" tile counts Unauthorized + Unavailable, so the drill-in must keep both
        // rows (and drop the unrelated Stale row) rather than silently hiding part of its own count.
        cut.FindAll("[data-testid='project-warning-row']").Count.ShouldBe(2);
        string queue = cut.Find("[data-testid='project-warnings-queue']").TextContent;
        queue.ShouldContain("memory-001");
        queue.ShouldContain("file-001");
        queue.ShouldNotContain("folder-001");
    }

    [Fact]
    public async Task LifecycleDashboardTileReloadsWithSelectedLifecycle()
    {
        IProjectWarningsDashboardSource source = Substitute.For<IProjectWarningsDashboardSource>();
        source.LoadAsync(Arg.Any<ProjectLifecycle?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectWarningsDashboardLoadResult.FromRows([Row()], [Warning()], Dashboard())));
        Services.AddSingleton(source);

        IRenderedComponent<Home> cut = Render<Home>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='project-warnings-dashboard']").ShouldNotBeNull());

        cut.FindAll("[data-testid='project-dashboard-tile']")
            .Single(tile => tile.TextContent.Contains("Archived"))
            .Click();

        // Lifecycle is a server-side query parameter, so the tile must re-query with the chosen lifecycle
        // rather than only updating the dropdown value while leaving the loaded rows untouched.
        await source.Received(1).LoadAsync(ProjectLifecycle.Archived, Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    private static ProjectInventoryRowProjection Row()
        => new()
        {
            Id = "project-001",
            ProjectId = "project-001",
            Name = "Inventory Project",
            Lifecycle = ProjectLifecycle.Active,
            WarningSummary = ProjectInventoryRowProjection.WarningSummaryUnavailable,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch.AddMinutes(1),
            TenantScope = "server-derived tenant",
            FreshnessTrustState = "trusted",
            ProjectionWatermark = "watermark-001",
        };

    private static ProjectWarningQueueItemProjection Warning(
        string referenceId = "memory-001",
        ReferenceState state = ReferenceState.Stale,
        ProjectReasonCode? reason = ProjectReasonCode.MemoryMatched,
        string referenceKind = "memory")
        => new()
        {
            Id = $"project-001:{referenceKind}:{referenceId}",
            ProjectId = "project-001",
            ProjectName = "Inventory Project",
            Lifecycle = ProjectLifecycle.Active,
            State = state,
            ReasonCode = reason,
            ReferenceKind = referenceKind,
            ReferenceId = referenceId,
            OwnerContext = "Memories",
            LastObservedAt = DateTimeOffset.UnixEpoch.AddMinutes(2),
            FreshnessTrustState = "trusted",
            ProjectionWatermark = "watermark-001",
            SourceSection = "operator-diagnostics.references",
        };

    private static ProjectOperationalDashboardProjection Dashboard()
        => new()
        {
            TotalVisibleProjects = 1,
            ActiveProjects = 1,
            ProjectsWithWarnings = 1,
            StaleReferences = 1,
            TenantScope = "server-derived tenant",
            LastObservedWarningAt = DateTimeOffset.UnixEpoch.AddMinutes(2),
        };
}
