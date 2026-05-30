// <copyright file="ProjectDetailPageTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Tests.Components;

using Bunit;

using Hexalith.FrontComposer.Testing;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.UI.Components.Pages;
using Hexalith.Projects.UI.Diagnostics;
using Hexalith.Projects.UI.Rendering;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

using Xunit;

/// <summary>
/// bUnit tests for the read-only Project detail inspector.
/// </summary>
public sealed class ProjectDetailPageTests : FrontComposerTestBase
{
    [Fact]
    public void DetailRendersHeaderSectionsSetupReferencesAndAudit()
    {
        IProjectDetailSource source = Substitute.For<IProjectDetailSource>();
        source.GetProjectDetailAsync("project-001", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectDetailLoadResult.FromDetail(Detail())));
        Services.AddSingleton(source);

        IRenderedComponent<ProjectDiagnostics> cut = Render<ProjectDiagnostics>(parameters => parameters
            .Add(p => p.ProjectId, "project-001"));

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-detail-inspector']").ShouldNotBeNull());
        cut.Find("[data-testid='project-diagnostic-header']").TextContent.ShouldContain("Detail Project");

        // Metadata section (default) exposes freshness evidence.
        cut.Find("[data-testid='project-detail-freshness']").TextContent.ShouldContain("trusted");

        cut.Find("[data-testid='project-detail-tab-setup']").Click();
        cut.Find("[data-testid='project-detail-section-setup']").TextContent.ShouldContain("keep continuity");
        cut.Find("[data-testid='project-detail-context-activation']").TextContent.ShouldContain("Enabled");

        cut.Find("[data-testid='project-detail-tab-references']").Click();
        cut.Find("[data-testid='project-detail-reference-summary']").TextContent.ShouldContain("folder-001");

        cut.Find("[data-testid='project-detail-tab-resolution']").Click();
        cut.Find("[data-testid='project-detail-section-resolution']").TextContent.ShouldContain("Story 5.6");

        cut.Find("[data-testid='project-detail-tab-audit']").Click();
        cut.Find("[data-testid='project-detail-audit-summary']").TextContent.ShouldContain("project.created");

        cut.Find("[data-testid='project-detail-tab-actions']").Click();
        cut.Find("[data-testid='project-detail-section-actions']").TextContent.ShouldContain("Story 5.9");
    }

    [Fact]
    public void DetailRendersNonBlockingDiagnosticFeedbackWhileShowingInspector()
    {
        IProjectDetailSource source = Substitute.For<IProjectDetailSource>();
        source.GetProjectDetailAsync("project-001", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectDetailLoadResult.FromDetail(
                Detail(),
                ProjectConsoleFeedback.Warning("data_unavailable", "corr-001"))));
        Services.AddSingleton(source);

        IRenderedComponent<ProjectDiagnostics> cut = Render<ProjectDiagnostics>(parameters => parameters
            .Add(p => p.ProjectId, "project-001"));

        // Base detail still renders; the bounded diagnostics failure degrades to non-blocking feedback.
        cut.WaitForAssertion(() => cut.Find("[data-testid='project-detail-inspector']").ShouldNotBeNull());
        cut.Find("[data-testid='project-feedback-warning']").TextContent.ShouldContain("data_unavailable");
    }

    [Fact]
    public void DetailRendersEmptyReferenceAndAuditStates()
    {
        IProjectDetailSource source = Substitute.For<IProjectDetailSource>();
        source.GetProjectDetailAsync("project-001", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectDetailLoadResult.FromDetail(DetailWithoutReferencesOrAudit())));
        Services.AddSingleton(source);

        IRenderedComponent<ProjectDiagnostics> cut = Render<ProjectDiagnostics>(parameters => parameters
            .Add(p => p.ProjectId, "project-001"));

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-detail-inspector']").ShouldNotBeNull());
        cut.Find("[data-testid='project-detail-tab-references']").Click();
        cut.Find("[data-testid='project-detail-section-references']").TextContent.ShouldContain("No references");
        cut.Find("[data-testid='project-detail-tab-audit']").Click();
        cut.Find("[data-testid='project-detail-section-audit']").TextContent.ShouldContain("No audit events");
    }

    [Fact]
    public void DetailRendersSafeFeedbackWhenBaseDetailIsDenied()
    {
        IProjectDetailSource source = Substitute.For<IProjectDetailSource>();
        source.GetProjectDetailAsync("project-001", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectDetailLoadResult.FromFeedback(
                ProjectConsoleFeedback.FailClosed("safe_denial", "corr-001"))));
        Services.AddSingleton(source);

        IRenderedComponent<ProjectDiagnostics> cut = Render<ProjectDiagnostics>(parameters => parameters
            .Add(p => p.ProjectId, "project-001"));

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-feedback-fail-closed']").TextContent.ShouldContain("safe_denial"));
        cut.Markup.ShouldNotContain("secret");
        cut.Markup.ShouldNotContain("token");
    }

    private static ProjectOperatorDiagnostic Detail()
        => new(
            "project-001",
            "Detail Project",
            "Safe metadata",
            "active",
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddMinutes(1),
            "safe-setup",
            new ProjectSetup(
                ["keep continuity"],
                ["use safe references"],
                [ProjectContextSourceKind.Conversation],
                [ProjectContextSourceKind.FileReference],
                new ConversationStartDefaults(LinkedSourcePolicy.AuthorizedReferences)),
            new ProjectOperatorContextActivation(true, null),
            [
                new ProjectOperatorReferenceSummary(
                    "folder",
                    "included",
                    "folder-001",
                    "Folder",
                    null,
                    Freshness()),
            ],
            [
                new ProjectOperatorAuditTimelineItem(
                    "audit-001",
                    "project.created",
                    DateTimeOffset.UnixEpoch,
                    "actor-001",
                    "corr-001",
                    "task-001",
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    1),
            ],
            Freshness());

    private static ProjectOperatorDiagnostic DetailWithoutReferencesOrAudit()
        => new(
            "project-001",
            "Detail Project",
            "Safe metadata",
            "active",
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddMinutes(1),
            "safe-setup",
            null,
            new ProjectOperatorContextActivation(true, null),
            [],
            [],
            Freshness());

    private static ProjectOperatorFreshnessMetadata Freshness()
        => new("eventually_consistent", DateTimeOffset.UnixEpoch, "watermark-001", false, "trusted");
}
