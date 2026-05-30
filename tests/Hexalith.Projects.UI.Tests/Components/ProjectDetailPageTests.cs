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
        Services.AddSingleton(Substitute.For<IProjectResolutionTraceSource>());
        Services.AddSingleton(Substitute.For<IProjectAuditTimelineSource>());
        Services.AddSingleton(Substitute.For<IProjectMaintenanceActionSource>());

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
        cut.Find("[data-testid='project-resolution-trace-workbench']").TextContent.ShouldContain("No trace has been run yet");

        cut.Find("[data-testid='project-detail-tab-audit']").Click();
        cut.Find("[data-testid='audit-timeline']").TextContent.ShouldContain("project.created");
        cut.Find("[data-testid='safe-diagnostic-export-preview']").TextContent.ShouldContain("audit-001");

        cut.Find("[data-testid='project-detail-tab-actions']").Click();
        cut.Find("[data-testid='maintenance-action-panel']").TextContent.ShouldContain("Maintenance actions");
        cut.Find("[data-testid='maintenance-action-state']").TextContent.ShouldContain("Preview");
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
        Services.AddSingleton(Substitute.For<IProjectResolutionTraceSource>());
        Services.AddSingleton(Substitute.For<IProjectAuditTimelineSource>());

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
        Services.AddSingleton(Substitute.For<IProjectResolutionTraceSource>());
        Services.AddSingleton(Substitute.For<IProjectAuditTimelineSource>());

        IRenderedComponent<ProjectDiagnostics> cut = Render<ProjectDiagnostics>(parameters => parameters
            .Add(p => p.ProjectId, "project-001"));

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-detail-inspector']").ShouldNotBeNull());
        cut.Find("[data-testid='project-detail-tab-references']").Click();
        cut.Find("[data-testid='project-detail-section-references']").TextContent.ShouldContain("No references");
        cut.Find("[data-testid='project-detail-tab-audit']").Click();
        cut.Find("[data-testid='project-detail-section-audit']").TextContent.ShouldContain("No audit events");
    }

    [Fact]
    public void MaintenancePanelRequiresDryRunAndConfirmationBeforeSubmit()
    {
        IProjectDetailSource source = Substitute.For<IProjectDetailSource>();
        source.GetProjectDetailAsync("project-001", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectDetailLoadResult.FromDetail(Detail())));
        IProjectMaintenanceActionSource maintenance = Substitute.For<IProjectMaintenanceActionSource>();
        maintenance.ExecuteAsync(Arg.Any<ProjectMaintenanceActionExecutionRequest>(), Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectMaintenanceActionExecutionResult.Confirmed("corr-001", "task-001", "audit-archive")));
        Services.AddSingleton(source);
        Services.AddSingleton(Substitute.For<IProjectResolutionTraceSource>());
        Services.AddSingleton(Substitute.For<IProjectAuditTimelineSource>());
        Services.AddSingleton(maintenance);

        IRenderedComponent<ProjectDiagnostics> cut = Render<ProjectDiagnostics>(parameters => parameters
            .Add(p => p.ProjectId, "project-001"));

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-detail-inspector']").ShouldNotBeNull());
        cut.Find("[data-testid='project-detail-tab-actions']").Click();

        cut.Find("[data-testid='maintenance-action-submit']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='maintenance-action-confirm']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='maintenance-action-dry-run-run']").Click();

        // A successful dry-run surfaces the distinct DryRunPassed state and keeps submit disabled until
        // explicit confirmation advances the panel to ConfirmationRequired.
        cut.Find("[data-testid='maintenance-action-state']").TextContent.ShouldContain("DryRunPassed");
        cut.Find("[data-testid='maintenance-action-confirm']").HasAttribute("disabled").ShouldBeFalse();
        cut.Find("[data-testid='maintenance-action-submit']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='maintenance-action-confirm']").Change(true);
        cut.Find("[data-testid='maintenance-action-state']").TextContent.ShouldContain("ConfirmationRequired");
        cut.Find("[data-testid='maintenance-action-submit']").HasAttribute("disabled").ShouldBeFalse();
        cut.Find("[data-testid='maintenance-action-submit']").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='maintenance-action-state']").TextContent.ShouldContain("Succeeded"));
        cut.Find("[data-testid='maintenance-action-feedback']").TextContent.ShouldContain("confirmed");
        cut.Markup.ShouldNotContain("token");
        cut.Markup.ShouldNotContain("ProblemDetails");
    }

    [Fact]
    public async Task MaintenancePanelBlocksRelinkUntilExplicitReplacementTargetExists()
    {
        IProjectDetailSource source = Substitute.For<IProjectDetailSource>();
        source.GetProjectDetailAsync("project-001", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectDetailLoadResult.FromDetail(Detail())));
        IProjectMaintenanceActionSource maintenance = Substitute.For<IProjectMaintenanceActionSource>();
        Services.AddSingleton(source);
        Services.AddSingleton(Substitute.For<IProjectResolutionTraceSource>());
        Services.AddSingleton(Substitute.For<IProjectAuditTimelineSource>());
        Services.AddSingleton(maintenance);

        IRenderedComponent<ProjectDiagnostics> cut = Render<ProjectDiagnostics>(parameters => parameters
            .Add(p => p.ProjectId, "project-001"));

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-detail-inspector']").ShouldNotBeNull());
        cut.Find("[data-testid='project-detail-tab-actions']").Click();
        cut.Find("[data-testid='maintenance-action-select']").Change(ProjectMaintenanceActions.Relink);

        cut.Find("[data-testid='maintenance-action-reference-kind']").TextContent.ShouldContain("Folder");
        cut.Find("[data-testid='maintenance-action-dry-run-run']").Click();

        cut.Find("[data-testid='maintenance-action-state']").TextContent.ShouldContain("DryRunBlocked");
        cut.Find("[data-testid='maintenance-action-feedback']").TextContent.ShouldContain("invalid_reference");
        cut.Find("[data-testid='maintenance-action-submit']").HasAttribute("disabled").ShouldBeTrue();
        await maintenance.DidNotReceive()
            .ExecuteAsync(
                Arg.Any<ProjectMaintenanceActionExecutionRequest>(),
                Arg.Any<IProgress<string>?>(),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    [Fact]
    public void MaintenancePanelEntersDryRunRequiredAndBlocksRestoreOnActiveProjectWithSafeFeedback()
    {
        IProjectDetailSource source = Substitute.For<IProjectDetailSource>();
        source.GetProjectDetailAsync("project-001", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectDetailLoadResult.FromDetail(Detail())));
        Services.AddSingleton(source);
        Services.AddSingleton(Substitute.For<IProjectResolutionTraceSource>());
        Services.AddSingleton(Substitute.For<IProjectAuditTimelineSource>());
        Services.AddSingleton(Substitute.For<IProjectMaintenanceActionSource>());

        IRenderedComponent<ProjectDiagnostics> cut = Render<ProjectDiagnostics>(parameters => parameters
            .Add(p => p.ProjectId, "project-001"));

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-detail-inspector']").ShouldNotBeNull());
        cut.Find("[data-testid='project-detail-tab-actions']").Click();

        // Selecting a new action requires a fresh dry-run before any confirmation.
        cut.Find("[data-testid='maintenance-action-select']").Change(ProjectMaintenanceActions.Restore);
        cut.Find("[data-testid='maintenance-action-state']").TextContent.ShouldContain("DryRunRequired");

        // Restore against an active project is blocked with a safe reason and a disabled submit.
        cut.Find("[data-testid='maintenance-action-dry-run-run']").Click();
        cut.Find("[data-testid='maintenance-action-state']").TextContent.ShouldContain("DryRunBlocked");
        cut.Find("[data-testid='maintenance-action-feedback']").TextContent.ShouldContain("invalid_lifecycle");
        cut.Find("[data-testid='maintenance-action-submit']").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void MaintenancePanelRendersFailedStateAndSafeFeedbackWhenSourceRejects()
    {
        IProjectDetailSource source = Substitute.For<IProjectDetailSource>();
        source.GetProjectDetailAsync("project-001", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectDetailLoadResult.FromDetail(Detail())));
        IProjectMaintenanceActionSource maintenance = Substitute.For<IProjectMaintenanceActionSource>();
        maintenance.ExecuteAsync(Arg.Any<ProjectMaintenanceActionExecutionRequest>(), Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectMaintenanceActionExecutionResult.Rejected("corr-001", "task-001", "idempotency_conflict")));
        Services.AddSingleton(source);
        Services.AddSingleton(Substitute.For<IProjectResolutionTraceSource>());
        Services.AddSingleton(Substitute.For<IProjectAuditTimelineSource>());
        Services.AddSingleton(maintenance);

        IRenderedComponent<ProjectDiagnostics> cut = Render<ProjectDiagnostics>(parameters => parameters
            .Add(p => p.ProjectId, "project-001"));

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-detail-inspector']").ShouldNotBeNull());
        cut.Find("[data-testid='project-detail-tab-actions']").Click();

        cut.Find("[data-testid='maintenance-action-dry-run-run']").Click();
        cut.Find("[data-testid='maintenance-action-confirm']").Change(true);
        cut.Find("[data-testid='maintenance-action-submit']").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='maintenance-action-state']").TextContent.ShouldContain("Failed"));
        cut.Find("[data-testid='maintenance-action-feedback']").TextContent.ShouldContain("idempotency_conflict");
        cut.Markup.ShouldNotContain("ProblemDetails");
        cut.Markup.ShouldNotContain("token");
    }

    [Fact]
    public async Task DetailRendersFullAuditTimelineAndReloadsBoundedLimit()
    {
        IProjectDetailSource source = Substitute.For<IProjectDetailSource>();
        source.GetProjectDetailAsync("project-001", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectDetailLoadResult.FromDetail(Detail())));
        IProjectAuditTimelineSource auditSource = Substitute.For<IProjectAuditTimelineSource>();
        auditSource.GetAuditTimelineAsync("project-001", 50, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectAuditTimelineLoadResult.FromRows(
                [
                    new ProjectOperatorAuditTimelineItem(
                        "audit-050",
                        "project.updated",
                        DateTimeOffset.UnixEpoch.AddMinutes(5),
                        "actor-050",
                        "corr-050",
                        "task-050",
                        "folder",
                        "folder-050",
                        "active",
                        "archived",
                        "project_archived",
                        null,
                        null,
                        50),
                ],
                Freshness())));
        Services.AddSingleton(source);
        Services.AddSingleton(Substitute.For<IProjectResolutionTraceSource>());
        Services.AddSingleton(auditSource);

        IRenderedComponent<ProjectDiagnostics> cut = Render<ProjectDiagnostics>(parameters => parameters
            .Add(p => p.ProjectId, "project-001"));

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-detail-inspector']").ShouldNotBeNull());
        cut.Find("[data-testid='project-detail-tab-audit']").Click();

        cut.FindAll("[data-testid='audit-timeline-entry']").Count.ShouldBe(1);
        cut.Find("[data-testid='audit-timeline-operation']").TextContent.ShouldContain("project.created");
        cut.Find("[data-testid='audit-timeline-state-delta']").TextContent.ShouldContain("none");
        cut.Find("[data-testid='audit-timeline-reference']").TextContent.ShouldContain("folder-001");
        cut.Find("[data-testid='audit-timeline-actor']").TextContent.ShouldContain("actor-001");
        cut.Find("[data-testid='audit-timeline-correlation-id']").TextContent.ShouldContain("corr-001");
        cut.Find("[data-testid='audit-timeline-task-id']").TextContent.ShouldContain("task-001");
        cut.Find("[data-testid='audit-timeline-event-id']").TextContent.ShouldContain("audit-001");
        cut.FindAll("[data-testid='audit-timeline-copy']").Count.ShouldBeGreaterThanOrEqualTo(3);
        cut.Find("[data-testid='safe-diagnostic-export-guarantee']").TextContent.ShouldContain("Payload-bearing data is excluded");
        cut.Find("[data-testid='safe-diagnostic-export-preview']").TextContent.ShouldNotContain("token");
        cut.Find("[data-testid='safe-diagnostic-export-preview']").TextContent.ShouldNotContain("score");

        cut.Find("[data-testid='audit-timeline-limit']").Change("50");
        cut.Find("[data-testid='audit-timeline-reload']").Click();

        await auditSource.Received(1)
            .GetAuditTimelineAsync("project-001", 50, Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
        cut.WaitForAssertion(() => cut.Find("[data-testid='audit-timeline-operation']").TextContent.ShouldContain("project.updated"));
        cut.Find("[data-testid='safe-diagnostic-export-preview']").TextContent.ShouldContain("audit-050");
    }

    [Theory]
    [MemberData(nameof(AuditReloadFeedbackCases))]
    public async Task DetailRendersAuditReloadFeedbackStatesWithoutBlankTimeline(ProjectConsoleFeedback feedback)
    {
        ArgumentNullException.ThrowIfNull(feedback);

        IProjectDetailSource source = Substitute.For<IProjectDetailSource>();
        source.GetProjectDetailAsync("project-001", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectDetailLoadResult.FromDetail(Detail())));
        IProjectAuditTimelineSource auditSource = Substitute.For<IProjectAuditTimelineSource>();
        auditSource.GetAuditTimelineAsync("project-001", 25, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectAuditTimelineLoadResult.FromFeedback(feedback)));
        Services.AddSingleton(source);
        Services.AddSingleton(Substitute.For<IProjectResolutionTraceSource>());
        Services.AddSingleton(auditSource);

        IRenderedComponent<ProjectDiagnostics> cut = Render<ProjectDiagnostics>(parameters => parameters
            .Add(p => p.ProjectId, "project-001"));

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-detail-inspector']").ShouldNotBeNull());
        cut.Find("[data-testid='project-detail-tab-audit']").Click();
        cut.Find("[data-testid='audit-timeline-reload']").Click();

        await auditSource.Received(1)
            .GetAuditTimelineAsync("project-001", 25, Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
        cut.WaitForAssertion(() => cut.Find("[data-testid='audit-timeline-feedback']").TextContent.ShouldContain(feedback.SafeReasonCode));
        cut.FindAll("[data-testid='audit-timeline-entry']").ShouldBeEmpty();
        cut.Find("[data-testid='safe-diagnostic-export-preview']").TextContent.ShouldContain(feedback.SafeReasonCode);
        cut.Markup.ShouldNotContain("secret");
        cut.Markup.ShouldNotContain("token");
        cut.Markup.ShouldNotContain("ProblemDetails");
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
        Services.AddSingleton(Substitute.For<IProjectResolutionTraceSource>());
        Services.AddSingleton(Substitute.For<IProjectAuditTimelineSource>());

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
        Services.AddSingleton(Substitute.For<IProjectResolutionTraceSource>());
        Services.AddSingleton(Substitute.For<IProjectAuditTimelineSource>());

        IRenderedComponent<ProjectDiagnostics> cut = Render<ProjectDiagnostics>(parameters => parameters
            .Add(p => p.ProjectId, "project-001"));

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-feedback-fail-closed']").TextContent.ShouldContain("safe_denial"));
        cut.Markup.ShouldNotContain("secret");
        cut.Markup.ShouldNotContain("token");
    }

    [Fact]
    public void ResolutionTraceWorkbenchRendersConversationTrace()
    {
        IProjectDetailSource detailSource = Substitute.For<IProjectDetailSource>();
        detailSource.GetProjectDetailAsync("project-001", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectDetailLoadResult.FromDetail(Detail())));
        IProjectResolutionTraceSource traceSource = Substitute.For<IProjectResolutionTraceSource>();
        traceSource.LoadTraceAsync(
                Arg.Is<ProjectResolutionTraceRequest>(request =>
                    request.Mode == ProjectResolutionTraceRequest.ConversationMode
                    && request.ConversationId == "conversation-001"),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SingleCandidateTrace()));
        Services.AddSingleton(detailSource);
        Services.AddSingleton(traceSource);
        Services.AddSingleton(Substitute.For<IProjectAuditTimelineSource>());

        IRenderedComponent<ProjectDiagnostics> cut = Render<ProjectDiagnostics>(parameters => parameters
            .Add(p => p.ProjectId, "project-001"));

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-detail-inspector']").ShouldNotBeNull());
        cut.Find("[data-testid='project-detail-tab-resolution']").Click();
        cut.Find("[data-testid='project-resolution-trace-conversation-id']").Change("conversation-001");
        cut.Find("[data-testid='project-resolution-trace-run']").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-resolution-trace-outcome']").TextContent.ShouldContain("Resolved"));
        cut.Find("[data-testid='project-resolution-trace-input-summary']").TextContent.ShouldContain("conversation-001");
        cut.Find("[data-testid='project-resolution-trace-candidate']").TextContent.ShouldContain("project-001");
        cut.FindAll("[data-testid='project-resolution-trace-reason']").Count.ShouldBe(2);

        // AC9: badges must not be color-only — each reason carries a visible text label.
        cut.FindAll("[data-testid='project-resolution-trace-reason']")
            .ShouldAllBe(reason => !string.IsNullOrWhiteSpace(reason.TextContent));
        cut.Find("[data-testid='project-resolution-trace-candidate']").TextContent.ShouldContain("ConversationLinked");

        cut.Markup.ShouldNotContain("transcript");
        cut.Markup.ShouldNotContain("token");
    }

    [Fact]
    public void ResolutionTraceWorkbenchRendersAttachmentTraceAndExclusionEvidence()
    {
        IProjectDetailSource detailSource = Substitute.For<IProjectDetailSource>();
        detailSource.GetProjectDetailAsync("project-001", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectDetailLoadResult.FromDetail(Detail())));
        IProjectResolutionTraceSource traceSource = Substitute.For<IProjectResolutionTraceSource>();
        traceSource.LoadTraceAsync(
                Arg.Is<ProjectResolutionTraceRequest>(request =>
                    request.Mode == ProjectResolutionTraceRequest.AttachmentsMode
                    && request.IncludeArchived),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(MultipleCandidateTraceWithExclusion()));
        Services.AddSingleton(detailSource);
        Services.AddSingleton(traceSource);
        Services.AddSingleton(Substitute.For<IProjectAuditTimelineSource>());

        IRenderedComponent<ProjectDiagnostics> cut = Render<ProjectDiagnostics>(parameters => parameters
            .Add(p => p.ProjectId, "project-001"));

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-detail-inspector']").ShouldNotBeNull());
        cut.Find("[data-testid='project-detail-tab-resolution']").Click();
        cut.Find("[data-testid='project-resolution-trace-mode']").Change(ProjectResolutionTraceRequest.AttachmentsMode);
        cut.Find("[data-testid='project-resolution-trace-folder-id']").Change("folder-001");
        cut.Find("[data-testid='project-resolution-trace-file-id']").Change("file-001");
        cut.Find("[data-testid='project-resolution-trace-include-archived']").Change(true);
        cut.Find("[data-testid='project-resolution-trace-run']").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-resolution-trace-outcome']").TextContent.ShouldContain("MultipleCandidates"));
        cut.Find("[data-testid='project-resolution-trace-candidate-comparison']").TextContent.ShouldContain("project-002");

        // AC9: candidate comparison must use semantic table structure, not a styled div.
        cut.Find("[data-testid='project-resolution-trace-candidate-comparison'] caption").TextContent.ShouldBe("Candidate comparison");
        cut.FindAll("[data-testid='project-resolution-trace-candidate-comparison'] thead th[scope='col']").Count.ShouldBe(4);
        cut.FindAll("[data-testid='project-resolution-trace-candidate-comparison'] tbody tr").Count.ShouldBe(2);
        cut.FindAll("[data-testid='project-resolution-trace-candidate-comparison'] tbody th[scope='row']").Count.ShouldBe(2);

        cut.Find("[data-testid='project-resolution-trace-exclusion']").TextContent.ShouldContain("referenceUnavailable");
        cut.Find("[data-testid='project-detail-section-resolution']").TextContent.ShouldNotContain("Story 5.6");
        cut.Markup.ShouldNotContain("payload");
        cut.Markup.ShouldNotContain("secret");
    }

    [Fact]
    public void ResolutionTraceWorkbenchRendersSafeValidationFeedback()
    {
        IProjectDetailSource detailSource = Substitute.For<IProjectDetailSource>();
        detailSource.GetProjectDetailAsync("project-001", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectDetailLoadResult.FromDetail(Detail())));
        IProjectResolutionTraceSource traceSource = Substitute.For<IProjectResolutionTraceSource>();
        traceSource.LoadTraceAsync(Arg.Any<ProjectResolutionTraceRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectResolutionTraceLoadResult.FromFeedback(
                ProjectConsoleFeedback.Error("conversation_id_required"))));
        Services.AddSingleton(detailSource);
        Services.AddSingleton(traceSource);
        Services.AddSingleton(Substitute.For<IProjectAuditTimelineSource>());

        IRenderedComponent<ProjectDiagnostics> cut = Render<ProjectDiagnostics>(parameters => parameters
            .Add(p => p.ProjectId, "project-001"));

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-detail-inspector']").ShouldNotBeNull());
        cut.Find("[data-testid='project-detail-tab-resolution']").Click();
        cut.Find("[data-testid='project-resolution-trace-run']").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-resolution-trace-feedback']").TextContent.ShouldContain("conversation_id_required"));
        cut.Find("[data-testid='project-resolution-trace-feedback']").TextContent.ShouldNotContain("conversation-001");
    }

    [Fact]
    public void ResolutionTraceWorkbenchRendersNoMatchOutcome()
    {
        IProjectDetailSource detailSource = Substitute.For<IProjectDetailSource>();
        detailSource.GetProjectDetailAsync("project-001", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectDetailLoadResult.FromDetail(Detail())));
        IProjectResolutionTraceSource traceSource = Substitute.For<IProjectResolutionTraceSource>();
        traceSource.LoadTraceAsync(Arg.Any<ProjectResolutionTraceRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(NoMatchTrace()));
        Services.AddSingleton(detailSource);
        Services.AddSingleton(traceSource);
        Services.AddSingleton(Substitute.For<IProjectAuditTimelineSource>());

        IRenderedComponent<ProjectDiagnostics> cut = Render<ProjectDiagnostics>(parameters => parameters
            .Add(p => p.ProjectId, "project-001"));

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-detail-inspector']").ShouldNotBeNull());
        cut.Find("[data-testid='project-detail-tab-resolution']").Click();
        cut.Find("[data-testid='project-resolution-trace-conversation-id']").Change("conversation-001");
        cut.Find("[data-testid='project-resolution-trace-run']").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-resolution-trace-outcome']").TextContent.ShouldContain("NoMatch"));
        cut.Find("[data-testid='project-resolution-trace-input-summary']").TextContent.ShouldContain("conversation-001");
        cut.Find("[data-testid='project-detail-section-resolution']").TextContent.ShouldContain("No candidates returned.");
        cut.FindAll("[data-testid='project-resolution-trace-candidate']").ShouldBeEmpty();
    }

    [Fact]
    public void ResolutionTraceWorkbenchRendersExcludedOutcomeFromArchivedEvidence()
    {
        IProjectDetailSource detailSource = Substitute.For<IProjectDetailSource>();
        detailSource.GetProjectDetailAsync("project-001", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectDetailLoadResult.FromDetail(Detail())));
        IProjectResolutionTraceSource traceSource = Substitute.For<IProjectResolutionTraceSource>();
        traceSource.LoadTraceAsync(Arg.Any<ProjectResolutionTraceRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ExcludedTrace()));
        Services.AddSingleton(detailSource);
        Services.AddSingleton(traceSource);
        Services.AddSingleton(Substitute.For<IProjectAuditTimelineSource>());

        IRenderedComponent<ProjectDiagnostics> cut = Render<ProjectDiagnostics>(parameters => parameters
            .Add(p => p.ProjectId, "project-001"));

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-detail-inspector']").ShouldNotBeNull());
        cut.Find("[data-testid='project-detail-tab-resolution']").Click();
        cut.Find("[data-testid='project-resolution-trace-conversation-id']").Change("conversation-001");
        cut.Find("[data-testid='project-resolution-trace-run']").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-resolution-trace-outcome']").TextContent.ShouldContain("Excluded"));
        cut.Find("[data-testid='project-resolution-trace-exclusion']").TextContent.ShouldContain("project-archived");
        cut.Find("[data-testid='project-resolution-trace-exclusion']").TextContent.ShouldContain("referenceArchived");
    }

    [Fact]
    public void ResolutionTraceWorkbenchRendersFailedClosedOutcomeFromDeniedEvidenceAndKeepsLongIdsReadable()
    {
        IProjectDetailSource detailSource = Substitute.For<IProjectDetailSource>();
        detailSource.GetProjectDetailAsync("project-001", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectDetailLoadResult.FromDetail(Detail())));
        IProjectResolutionTraceSource traceSource = Substitute.For<IProjectResolutionTraceSource>();
        traceSource.LoadTraceAsync(Arg.Any<ProjectResolutionTraceRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(FailedClosedTrace()));
        Services.AddSingleton(detailSource);
        Services.AddSingleton(traceSource);
        Services.AddSingleton(Substitute.For<IProjectAuditTimelineSource>());

        IRenderedComponent<ProjectDiagnostics> cut = Render<ProjectDiagnostics>(parameters => parameters
            .Add(p => p.ProjectId, "project-001"));

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-detail-inspector']").ShouldNotBeNull());
        cut.Find("[data-testid='project-detail-tab-resolution']").Click();
        cut.Find("[data-testid='project-resolution-trace-conversation-id']").Change("conversation-001");
        cut.Find("[data-testid='project-resolution-trace-run']").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='project-resolution-trace-outcome']").TextContent.ShouldContain("FailedClosed"));
        AngleSharp.Dom.IElement excludedProjectId = cut.Find("[data-testid='project-resolution-trace-exclusion'] code");
        excludedProjectId.TextContent.ShouldContain("project-denied-with-a-very-long-opaque-identifier-001");
        string ariaLabel = excludedProjectId.GetAttribute("aria-label").ShouldNotBeNull();
        ariaLabel.ShouldContain("project-denied-with-a-very-long-opaque-identifier-001");
        cut.Find("[data-testid='project-resolution-trace-exclusion']").TextContent.ShouldContain("referenceUnauthorized");
        cut.Markup.ShouldNotContain("ProblemDetails");
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
                    "folder",
                    "folder-001",
                    null,
                    null,
                    "project_created",
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

    public static IEnumerable<object[]> AuditReloadFeedbackCases()
    {
        yield return [ProjectConsoleFeedback.Error("validation_error", "corr-validation")];
        yield return [ProjectConsoleFeedback.FailClosed("safe_denial", "corr-denial")];
        yield return [ProjectConsoleFeedback.Warning("data_unavailable", "corr-unavailable")];
    }

    private static ProjectResolutionTraceLoadResult SingleCandidateTrace()
        => ProjectResolutionTraceLoadResult.FromTrace(
            new ProjectResolutionTraceProjection
            {
                InputMode = ProjectResolutionTraceRequest.ConversationMode,
                PresentedConversationId = "conversation-001",
                IncludeArchived = false,
                ObservedAt = DateTimeOffset.UnixEpoch,
                Result = ResolutionResult.SingleCandidate,
                CandidateCount = 1,
            },
            [
                new ProjectResolutionTraceCandidateProjection
                {
                    Id = "candidate:project-001",
                    ProjectId = "project-001",
                    DisplayName = "Trace Project",
                    Rank = 1,
                    Score = 70,
                    ReasonCodes = "ConversationLinked, MetadataMatched",
                },
            ],
            []);

    private static ProjectResolutionTraceLoadResult MultipleCandidateTraceWithExclusion()
        => ProjectResolutionTraceLoadResult.FromTrace(
            new ProjectResolutionTraceProjection
            {
                InputMode = ProjectResolutionTraceRequest.AttachmentsMode,
                PresentedFolderIds = "folder-001",
                PresentedFileIds = "file-001",
                IncludeArchived = true,
                ObservedAt = DateTimeOffset.UnixEpoch,
                Result = ResolutionResult.MultipleCandidates,
                CandidateCount = 2,
                ExclusionCount = 1,
            },
            [
                new ProjectResolutionTraceCandidateProjection
                {
                    Id = "candidate:project-001",
                    ProjectId = "project-001",
                    Rank = 1,
                    Score = 45,
                    ReasonCodes = "ProjectFolderMatched",
                },
                new ProjectResolutionTraceCandidateProjection
                {
                    Id = "candidate:project-002",
                    ProjectId = "project-002",
                    Rank = 2,
                    Score = 35,
                    ReasonCodes = "FileReferenceMatched",
                },
            ],
            [
                new ProjectResolutionTraceExclusionProjection
                {
                    Id = "exclusion:project-denied",
                    ProjectId = "project-denied",
                    ReferenceState = ReferenceState.Unavailable,
                    ReasonCode = ProjectReasonCode.FileReferenceMatched,
                    Diagnostic = ProjectContextInclusionDiagnostic.ReferenceUnavailable,
                },
            ]);

    private static ProjectResolutionTraceLoadResult NoMatchTrace()
        => ProjectResolutionTraceLoadResult.FromTrace(
            new ProjectResolutionTraceProjection
            {
                InputMode = ProjectResolutionTraceRequest.ConversationMode,
                PresentedConversationId = "conversation-001",
                IncludeArchived = false,
                ObservedAt = DateTimeOffset.UnixEpoch,
                Result = ResolutionResult.NoMatch,
            },
            [],
            []);

    private static ProjectResolutionTraceLoadResult ExcludedTrace()
        => ProjectResolutionTraceLoadResult.FromTrace(
            new ProjectResolutionTraceProjection
            {
                InputMode = ProjectResolutionTraceRequest.ConversationMode,
                PresentedConversationId = "conversation-001",
                IncludeArchived = false,
                ObservedAt = DateTimeOffset.UnixEpoch,
                Result = ResolutionResult.NoMatch,
                ExclusionCount = 1,
            },
            [],
            [
                new ProjectResolutionTraceExclusionProjection
                {
                    Id = "exclusion:project-archived",
                    ProjectId = "project-archived",
                    ReferenceState = ReferenceState.Archived,
                    ReasonCode = ProjectReasonCode.ConversationLinked,
                    Diagnostic = ProjectContextInclusionDiagnostic.ReferenceArchived,
                },
            ]);

    private static ProjectResolutionTraceLoadResult FailedClosedTrace()
        => ProjectResolutionTraceLoadResult.FromTrace(
            new ProjectResolutionTraceProjection
            {
                InputMode = ProjectResolutionTraceRequest.ConversationMode,
                PresentedConversationId = "conversation-001",
                IncludeArchived = false,
                ObservedAt = DateTimeOffset.UnixEpoch,
                Result = ResolutionResult.NoMatch,
                ExclusionCount = 1,
            },
            [],
            [
                new ProjectResolutionTraceExclusionProjection
                {
                    Id = "exclusion:project-denied-with-a-very-long-opaque-identifier-001",
                    ProjectId = "project-denied-with-a-very-long-opaque-identifier-001",
                    ReferenceState = ReferenceState.Unauthorized,
                    ReasonCode = ProjectReasonCode.ConversationLinked,
                    Diagnostic = ProjectContextInclusionDiagnostic.ReferenceUnauthorized,
                },
            ]);
}
