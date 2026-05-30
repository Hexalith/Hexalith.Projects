// <copyright file="ProjectDetailPageTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Tests.Components;

using Bunit;

using Hexalith.FrontComposer.Testing;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
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
        cut.Find("[data-testid='project-reference-health-matrix']").TextContent.ShouldContain("folder-001");
        cut.Find("[data-testid='project-reference-owner']").TextContent.ShouldContain("Folders");
        cut.Find("[data-testid='project-reference-safe-actions']").TextContent.ShouldContain("Story 5.9");

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
    public void DetailRendersReferenceHealthMatrixForAllReferenceKindsAndFailureStates()
    {
        IProjectDetailSource source = Substitute.For<IProjectDetailSource>();
        source.GetProjectDetailAsync("project-001", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectDetailLoadResult.FromDetail(
                DetailWithReferenceHealthRows(),
                referenceHealthRows: ReferenceHealthRows())));
        Services.AddSingleton(source);

        IRenderedComponent<ProjectDiagnostics> cut = Render<ProjectDiagnostics>(parameters => parameters
            .Add(p => p.ProjectId, "project-001"));

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-detail-inspector']").ShouldNotBeNull());
        cut.Find("[data-testid='project-detail-tab-references']").Click();

        cut.Find("[data-testid='project-reference-health-matrix']").TextContent.ShouldContain("Reference Health Matrix");
        cut.FindAll("[data-testid='project-reference-health-row']").Count.ShouldBe(7);
        cut.Find("[data-testid='project-detail-section-references']").TextContent.ShouldContain("conversation-001");
        cut.Find("[data-testid='project-detail-section-references']").TextContent.ShouldContain("referenceUnauthorized");
        cut.Find("[data-testid='project-detail-section-references']").TextContent.ShouldContain("ReferenceAuthorization");
        cut.Find("[data-testid='project-detail-section-references']").TextContent.ShouldContain("Stale");
        cut.Find("[data-testid='project-detail-section-references']").TextContent.ShouldContain("Archived");
        cut.Find("[data-testid='project-detail-section-references']").TextContent.ShouldContain("Conflict");
        cut.Find("[data-testid='project-detail-section-references']").TextContent.ShouldContain("Unavailable");
        cut.Find("[data-testid='project-detail-section-references']").TextContent.ShouldContain("Invalid reference");
        cut.Find("[data-testid='project-detail-section-references']").TextContent.ShouldNotContain("transcript");
        cut.Find("[data-testid='project-detail-section-references']").TextContent.ShouldNotContain("token");
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

    private static ProjectOperatorDiagnostic DetailWithReferenceHealthRows()
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
            [
                new ProjectOperatorReferenceSummary(
                    "conversation",
                    "unauthorized",
                    "conversation-001",
                    "Support conversation",
                    "ConversationLinked",
                    Freshness()),
                new ProjectOperatorReferenceSummary(
                    "folder",
                    "included",
                    "folder-001",
                    "Folder",
                    "ProjectFolderMatched",
                    Freshness()),
                new ProjectOperatorReferenceSummary(
                    "file",
                    "unavailable",
                    "file-001",
                    "File",
                    null,
                    Freshness()),
                new ProjectOperatorReferenceSummary(
                    "memory",
                    "invalidReference",
                    "memory-001",
                    "Memory",
                    null,
                    Freshness()),
            ],
            [],
            Freshness());

    private static IReadOnlyList<ProjectReferenceHealthRowProjection> ReferenceHealthRows()
        =>
        [
            UnauthorizedConversationHealthRow(),
            ProjectReferenceHealthRowProjection.FromReferenceSummary(
                "project-001",
                new ProjectOperatorReferenceSummary(
                    "folder",
                    "included",
                    "folder-001",
                    "Folder",
                    "ProjectFolderMatched",
                    Freshness())),
            ProjectReferenceHealthRowProjection.FromReferenceSummary(
                "project-001",
                new ProjectOperatorReferenceSummary(
                    "file",
                    "unavailable",
                    "file-001",
                    "File",
                    null,
                    Freshness())),
            ProjectReferenceHealthRowProjection.FromReferenceSummary(
                "project-001",
                new ProjectOperatorReferenceSummary(
                    "file",
                    "stale",
                    "file-stale-001",
                    "Stale file",
                    null,
                    Freshness())),
            ProjectReferenceHealthRowProjection.FromReferenceSummary(
                "project-001",
                new ProjectOperatorReferenceSummary(
                    "folder",
                    "conflict",
                    "folder-conflict-001",
                    "Conflicting folder",
                    null,
                    Freshness())),
            ProjectReferenceHealthRowProjection.FromReferenceSummary(
                "project-001",
                new ProjectOperatorReferenceSummary(
                    "memory",
                    "invalidReference",
                    "memory-001",
                    "Memory",
                    null,
                    Freshness())),
            ProjectReferenceHealthRowProjection.FromReferenceSummary(
                "project-001",
                new ProjectOperatorReferenceSummary(
                    "memory",
                    "archived",
                    "memory-archived-001",
                    "Archived memory",
                    null,
                    Freshness())),
        ];

    private static ProjectReferenceHealthRowProjection UnauthorizedConversationHealthRow()
    {
        ProjectReferenceHealthRowProjection row = ProjectReferenceHealthRowProjection.FromReferenceSummary(
            "project-001",
            new ProjectOperatorReferenceSummary(
                "conversation",
                "unauthorized",
                "conversation-001",
                "Support conversation",
                "ConversationLinked",
                Freshness()));
        row.InclusionCheck = ProjectContextInclusionCheck.ReferenceAuthorization;
        row.DiagnosticCode = ProjectContextInclusionDiagnostic.ReferenceUnauthorized;
        return row;
    }

    private static ProjectOperatorFreshnessMetadata Freshness()
        => new("eventually_consistent", DateTimeOffset.UnixEpoch, "watermark-001", false, "trusted");
}
