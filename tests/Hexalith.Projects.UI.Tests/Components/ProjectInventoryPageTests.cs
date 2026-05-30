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
        IProjectInventorySource source = Substitute.For<IProjectInventorySource>();
        source.ListProjectsAsync(null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectInventoryLoadResult.FromRows([Row()])));
        Services.AddSingleton(source);

        IRenderedComponent<Home> cut = Render<Home>();

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-inventory-grid']").ShouldNotBeNull());
        cut.Find("[data-testid='project-inventory-filter-lifecycle']").ShouldNotBeNull();
        cut.Find("[data-testid='project-inventory-filter-warning']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='project-inventory-filter-reference-type']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='project-inventory-row']").TextContent.ShouldContain("Inventory Project");
        cut.Find("[data-testid='project-inventory-row-link']").GetAttribute("href").ShouldBe("/projects/project-001");
        cut.Markup.ShouldContain("server-derived tenant");
        cut.Markup.ShouldContain("Unavailable on list row");
    }

    [Fact]
    public void InventoryRendersSafeFeedbackForDeniedLoad()
    {
        IProjectInventorySource source = Substitute.For<IProjectInventorySource>();
        source.ListProjectsAsync(null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectInventoryLoadResult.FromFeedback(
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
        IProjectInventorySource source = Substitute.For<IProjectInventorySource>();
        source.ListProjectsAsync(null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectInventoryLoadResult.FromRows([])));
        Services.AddSingleton(source);

        IRenderedComponent<Home> cut = Render<Home>();

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='project-inventory-empty']").TextContent.ShouldContain("No projects"));
        cut.FindAll("[data-testid='project-inventory-row']").ShouldBeEmpty();
        cut.FindAll("[data-testid='project-inventory-grid']").ShouldBeEmpty();
    }

    private static ProjectInventoryRowProjection Row()
        => new()
        {
            Id = "project-001",
            ProjectId = "project-001",
            Name = "Inventory Project",
            Lifecycle = ProjectLifecycle.Active,
            WarningSummary = "Unavailable on list row",
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch.AddMinutes(1),
            TenantScope = "server-derived tenant",
            FreshnessTrustState = "trusted",
            ProjectionWatermark = "watermark-001",
        };
}
