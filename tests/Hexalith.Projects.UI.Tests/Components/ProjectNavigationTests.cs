// <copyright file="ProjectNavigationTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Tests.Components;

using System.Security.Claims;
using Bunit;
using Bunit.TestDoubles;

using Hexalith.FrontComposer.Testing;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.UI.Components.Layout;
using Hexalith.Projects.UI.Components.Shared;
using Hexalith.Projects.UI.Diagnostics;
using Hexalith.Projects.UI.Rendering;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Hexalith.Projects.UI.Components.Pages;
using Hexalith.Projects.Contracts.Queries;
using Microsoft.FluentUI.AspNetCore.Components;
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
    private readonly BunitAuthorizationContext _authorization;

    public ProjectNavigationTests()
    {
        _authorization = AddAuthorization();
    }

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
        cut.Find("[data-testid='project-navigation-project']").GetAttribute("href").ShouldBe("/inspect/projects/project-001");
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

        Services.GetRequiredService<NavigationManager>().NavigateTo("/inspect/projects");
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

    [Theory]
    [InlineData("tenant-b", "session-b")]
    [InlineData("tenant-a", "session-b")]
    public async Task CascadingAuthorityChangeRecreatesShellAndClearsDescriptiveMetadata(string tenant, string session)
    {
        IProjectInventorySource inventory = Substitute.For<IProjectInventorySource>();
        inventory.ListProjectsAsync(null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectInventoryLoadResult.FromRows([Row("prior-project", "Prior descriptive name")])));
        IProjectWarningsDashboardSource warnings = Substitute.For<IProjectWarningsDashboardSource>();
        List<ListProjectsQuery> observed = [];
        TaskCompletionSource<ProjectWarningsDashboardLoadResult> next = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bool changed = false;
        warnings.LoadPageAsync(Arg.Do<ListProjectsQuery>(observed.Add), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                if (changed) return next.Task;
                ProjectInventoryLoadResult page = ProjectInventoryLoadResult.FromRows(
                    [Row("prior-project", call.Arg<ListProjectsQuery>().InspectDescriptiveMetadata ? "Prior inspected detail" : string.Empty)]);
                return Task.FromResult(ProjectWarningsDashboardLoadResult.FromRows(page.Rows, [], new()) with { Inventory = page });
            });
        Services.AddSingleton(inventory);
        Services.AddSingleton(warnings);
        _authorization.SetClaims(new Claim("sub", "user-1"), new Claim("eventstore:current-tenant", "tenant-a"),
            new Claim("hexalith:frontcomposer:token-session", "session-a"));
        _authorization.SetAuthorized("user-1");
        Services.GetRequiredService<NavigationManager>().NavigateTo("/inspect/projects");
        RenderFragment body = builder => { builder.OpenComponent<Home>(0); builder.CloseComponent(); };
        IRenderedComponent<CascadingAuthenticationState> cut = Render<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent<MainLayout>(layout => layout.Add(p => p.Body, body)));
        cut.WaitForAssertion(() => cut.FindComponent<ProjectNavigation>().Markup.ShouldContain("Prior descriptive name"));
        await cut.InvokeAsync(() => cut.FindComponent<Home>().FindComponents<FluentCheckbox>()[0].Instance.ValueChanged.InvokeAsync(true));
        cut.WaitForAssertion(() => cut.FindComponent<Home>().Markup.ShouldContain("Prior inspected detail"));
        ProjectNavigation prior = cut.FindComponent<ProjectNavigation>().Instance;
        inventory.ListProjectsAsync(null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectInventoryLoadResult.FromRows([Row("current-project", string.Empty)])));
        changed = true;
        int before = observed.Count;
        await cut.InvokeAsync(() =>
        {
            _authorization.SetClaims(new Claim("sub", "user-1"), new Claim("eventstore:current-tenant", tenant),
                new Claim("hexalith:frontcomposer:token-session", session));
            _authorization.SetAuthorized("user-1");
        });
        cut.WaitForAssertion(() => cut.FindComponent<ProjectNavigation>().Instance.ShouldNotBeSameAs(prior));
        cut.Markup.ShouldNotContain("Prior descriptive name");
        cut.Markup.ShouldNotContain("Prior inspected detail");
        observed.Skip(before).ShouldNotBeEmpty();
        observed.Skip(before).All(query => query.Cursor is null && !query.InspectDescriptiveMetadata && !query.IncludeQuarantined).ShouldBeTrue();
        cut.FindComponent<Home>().FindComponents<FluentCheckbox>()[0].Instance.Value.ShouldBeFalse();
        next.SetResult(ProjectWarningsDashboardLoadResult.FromRows([], [], new()));
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
