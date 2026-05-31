// <copyright file="ProjectNavigationTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Tests.Components;

using Bunit;

using Hexalith.FrontComposer.Testing;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.UI.Components.Layout;
using Hexalith.Projects.UI.Components.Shared;
using Hexalith.Projects.UI.Diagnostics;
using Hexalith.Projects.UI.Rendering;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

using Xunit;

/// <summary>
/// Tests for the Projects-specific shell navigation.
/// </summary>
public sealed class ProjectNavigationTests : FrontComposerTestBase
{
    [Fact]
    public void NavigationRendersProjectNamesInsteadOfProjectionClassNames()
    {
        IProjectInventorySource source = Substitute.For<IProjectInventorySource>();
        source.ListProjectsAsync(null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectInventoryLoadResult.FromRows(
                [
                    Row("project-001", "Customer Portal"),
                    Row("project-002", "Operations Console"),
                ])));
        Services.AddSingleton(source);

        IRenderedComponent<ProjectNavigation> cut = Render<ProjectNavigation>();

        cut.WaitForAssertion(() => cut.FindAll("[data-testid='project-navigation-project']").Count.ShouldBe(2));
        cut.Markup.ShouldContain("Customer Portal");
        cut.Markup.ShouldContain("Operations Console");
        cut.Find("[data-testid='project-navigation-project']").GetAttribute("href").ShouldBe("/projects/project-001");
        cut.Markup.ShouldNotContain(nameof(ProjectInventoryRowProjection));
        cut.Markup.ShouldNotContain(nameof(ProjectWarningQueueItemProjection));
    }

    [Fact]
    public void MainLayoutUsesProjectsNavigationSlot()
    {
        IProjectInventorySource source = Substitute.For<IProjectInventorySource>();
        source.ListProjectsAsync(null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectInventoryLoadResult.FromRows([Row("project-001", "Customer Portal")])));
        Services.AddSingleton(source);

        RenderFragment body = builder => builder.AddMarkupContent(0, "<p>Body</p>");
        IRenderedComponent<MainLayout> cut = Render<MainLayout>(parameters => parameters
            .Add(p => p.Body, body));

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-navigation']").TextContent.ShouldContain("Customer Portal"));
        cut.Markup.ShouldNotContain(nameof(ProjectInventoryRowProjection));
    }

    [Fact]
    public void NavigationDoesNotFallBackToProjectionClassNamesWhenListFails()
    {
        IProjectInventorySource source = Substitute.For<IProjectInventorySource>();
        source.ListProjectsAsync(null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectInventoryLoadResult.FromFeedback(
                ProjectConsoleFeedback.FailClosed("safe_denial", "corr-001"))));
        Services.AddSingleton(source);

        IRenderedComponent<ProjectNavigation> cut = Render<ProjectNavigation>();

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-navigation-feedback']").TextContent.ShouldContain("Project list unavailable"));
        cut.Markup.ShouldNotContain(nameof(ProjectInventoryRowProjection));
        cut.Markup.ShouldNotContain(nameof(ProjectWarningQueueItemProjection));
    }

    private static ProjectInventoryRowProjection Row(string projectId, string name)
        => new()
        {
            Id = projectId,
            ProjectId = projectId,
            Name = name,
            Lifecycle = ProjectLifecycle.Active,
            WarningSummary = ProjectInventoryRowProjection.WarningSummaryUnavailable,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch.AddMinutes(1),
            TenantScope = "server-derived tenant",
            FreshnessTrustState = "trusted",
            ProjectionWatermark = "watermark-001",
        };
}
