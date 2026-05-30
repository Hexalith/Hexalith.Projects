// <copyright file="ProjectDiagnosticHeaderTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Tests.Components;

using Bunit;

using Hexalith.FrontComposer.Testing;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.UI.Components.Shared;
using Hexalith.Projects.UI.Rendering;

using Shouldly;

using Xunit;

/// <summary>
/// bUnit contracts for the Story 5.3 diagnostic header primitive.
/// </summary>
public sealed class ProjectDiagnosticHeaderTests : FrontComposerTestBase
{
    [Fact]
    public void HeaderRendersIdentityBadgesWarningsTimestampAndMode()
    {
        ProjectOperatorDiagnostic diagnostic = CreateDiagnostic();

        IRenderedComponent<ProjectDiagnosticHeader> cut = Render<ProjectDiagnosticHeader>(parameters => parameters
            .Add(p => p.Diagnostic, diagnostic)
            .Add(p => p.TenantScope, "tenant:acme")
            .Add(p => p.Mode, ProjectConsoleModes.DryRun));

        cut.Find("[data-testid='project-diagnostic-header']").TextContent.ShouldContain("tenant:acme");
        cut.Find("[data-testid='project-detail-name']").TextContent.ShouldContain("Console Project");
        cut.Find("[data-testid='project-lifecycle-badge']").TextContent.ShouldContain("Active");
        cut.Find("[data-testid='project-warning-count']").TextContent.ShouldContain("2");
        cut.Find("[data-testid='project-last-updated'] time").GetAttribute("datetime").ShouldBe("2026-05-30T10:00:00.0000000+00:00");
        cut.Find("[data-testid='project-mode-indicator']").TextContent.ShouldContain("dry-run");
    }

    [Fact]
    public void HeaderUsesVisibleAndAccessibleLifecycleBadge()
    {
        IRenderedComponent<ProjectDiagnosticHeader> cut = Render<ProjectDiagnosticHeader>(parameters => parameters
            .Add(p => p.Diagnostic, CreateDiagnostic())
            .Add(p => p.TenantScope, "tenant:acme"));

        var badge = cut.Find("[data-testid='fc-status-badge']");
        badge.TextContent.ShouldContain(ProjectVocabularyDescriptors.Describe(ProjectLifecycle.Active).DisplayLabel);
        string ariaLabel = badge.GetAttribute("aria-label") ?? string.Empty;
        ariaLabel.ShouldContain("Lifecycle");
        ariaLabel.ShouldContain("Active");
        badge.GetAttribute("data-fc-badge-slot").ShouldBe("Success");
    }

    [Fact]
    public void HeaderMarksProjectIdAsCopyableWithoutHidingTheValue()
    {
        IRenderedComponent<ProjectDiagnosticHeader> cut = Render<ProjectDiagnosticHeader>(parameters => parameters
            .Add(p => p.Diagnostic, CreateDiagnostic())
            .Add(p => p.TenantScope, "tenant:acme"));

        var copy = cut.Find("[data-testid='project-copy-project-id']");
        copy.GetAttribute("data-copy-value").ShouldBe("project-001");
        (copy.GetAttribute("aria-label") ?? string.Empty).ShouldContain("project-001");
        cut.Markup.ShouldContain("project-001");
    }

    [Fact]
    public void HeaderMarksTenantScopeAsCopyableWithoutHidingTheValue()
    {
        IRenderedComponent<ProjectDiagnosticHeader> cut = Render<ProjectDiagnosticHeader>(parameters => parameters
            .Add(p => p.Diagnostic, CreateDiagnostic())
            .Add(p => p.TenantScope, "tenant:acme"));

        var copy = cut.Find("[data-testid='project-copy-tenant-scope']");
        copy.GetAttribute("data-copy-value").ShouldBe("tenant:acme");
        (copy.GetAttribute("aria-label") ?? string.Empty).ShouldContain("tenant:acme");
        cut.Markup.ShouldContain("tenant:acme");
    }

    [Theory]
    [InlineData(ProjectConsoleModes.ReadOnly)]
    [InlineData(ProjectConsoleModes.DryRun)]
    [InlineData(ProjectConsoleModes.Maintenance)]
    public void HeaderRendersEveryConsoleModeIndicator(string mode)
    {
        IRenderedComponent<ProjectDiagnosticHeader> cut = Render<ProjectDiagnosticHeader>(parameters => parameters
            .Add(p => p.Diagnostic, CreateDiagnostic())
            .Add(p => p.TenantScope, "tenant:acme")
            .Add(p => p.Mode, mode));

        var indicator = cut.Find("[data-testid='project-mode-indicator']");
        indicator.TextContent.ShouldContain(mode);
        (indicator.QuerySelector("span")?.GetAttribute("aria-label") ?? string.Empty).ShouldContain(mode);
    }

    [Fact]
    public void HeaderKeepsLongProjectIdVisibleAndCopyable()
    {
        const string LongProjectId = "project-tenant-acme-region-eu-west-operations-console-00000000000000000001";
        ProjectOperatorDiagnostic diagnostic = CreateDiagnostic(projectId: LongProjectId);

        IRenderedComponent<ProjectDiagnosticHeader> cut = Render<ProjectDiagnosticHeader>(parameters => parameters
            .Add(p => p.Diagnostic, diagnostic)
            .Add(p => p.TenantScope, "tenant:acme"));

        cut.Markup.ShouldContain(LongProjectId);
        cut.Find("[data-testid='project-copy-project-id']").GetAttribute("data-copy-value").ShouldBe(LongProjectId);
    }

    private static ProjectOperatorDiagnostic CreateDiagnostic(string projectId = "project-001")
        => new(
            projectId,
            "Console Project",
            "Safe metadata description",
            "active",
            new DateTimeOffset(2026, 5, 30, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 30, 10, 0, 0, TimeSpan.Zero),
            null,
            null,
            new ProjectOperatorContextActivation(true, null),
            [
                new ProjectOperatorReferenceSummary("folder", "included", "folder-001", "Folder", null, Freshness()),
                new ProjectOperatorReferenceSummary("file", "unavailable", "file-001", "File", null, Freshness()),
                new ProjectOperatorReferenceSummary("memory", "stale", "memory-001", "Memory", null, Freshness()),
            ],
            [],
            Freshness());

    private static ProjectOperatorFreshnessMetadata Freshness()
        => new("eventually_consistent", DateTimeOffset.UnixEpoch, "watermark-001", false, "trusted");
}
