// <copyright file="ProjectRenderingPrimitiveTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Tests.Rendering;

using Bunit;

using Hexalith.FrontComposer.Testing;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Testing.Leakage;
using Hexalith.Projects.UI.Components.Shared;
using Hexalith.Projects.UI.Rendering;

using Shouldly;

using Xunit;

/// <summary>
/// Tests for shared empty-state and feedback primitives.
/// </summary>
public sealed class ProjectRenderingPrimitiveTests : FrontComposerTestBase
{
    [Fact]
    public void ProjectionAndFeedbackModelsSerializeMetadataOnly()
    {
        var projection = new ProjectOperatorDiagnosticShellProjection
        {
            Id = "project-001",
            ProjectId = "project-001",
            Name = "Console Project",
            Lifecycle = ProjectLifecycle.Active,
            WarningCount = 1,
            LastUpdated = DateTimeOffset.UnixEpoch,
            Mode = ProjectConsoleModes.ReadOnly,
            FreshnessTrustState = "trusted",
        };

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(projection));
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(
            ProjectConsoleFeedback.FailClosed("safe_denial", "corr-001")));
    }

    [Theory]
    [InlineData(ProjectEmptyState.None)]
    [InlineData(ProjectEmptyState.Denied)]
    [InlineData(ProjectEmptyState.Unavailable)]
    [InlineData(ProjectEmptyState.Filtered)]
    public void EmptyStatesExposeDistinctCategories(string category)
    {
        ProjectEmptyState state = category switch
        {
            ProjectEmptyState.Denied => ProjectEmptyState.AccessDenied(),
            ProjectEmptyState.Unavailable => ProjectEmptyState.DataUnavailable(),
            ProjectEmptyState.Filtered => ProjectEmptyState.FilterReturnedNoResults(),
            _ => ProjectEmptyState.NoProjects(),
        };

        IRenderedComponent<ProjectEmptyStateView> cut = Render<ProjectEmptyStateView>(parameters => parameters
            .Add(p => p.State, state)
            .Add(p => p.ProjectionType, typeof(ProjectOperatorDiagnosticShellProjection)));

        cut.Find($"[data-testid='project-empty-{category}']").TextContent.ShouldContain(state.SecondaryText);
    }

    [Theory]
    [MemberData(nameof(RequiredEmptyStates))]
    public void EmptyStateFactoriesExposeRequiredStoryExamples(ProjectEmptyState state, string expectedText)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(expectedText);

        IRenderedComponent<ProjectEmptyStateView> cut = Render<ProjectEmptyStateView>(parameters => parameters
            .Add(p => p.State, state)
            .Add(p => p.ProjectionType, typeof(ProjectOperatorDiagnosticShellProjection)));

        cut.Find($"[data-testid='project-empty-{state.Category}']").TextContent.ShouldContain(expectedText);
        (cut.Find(".project-empty-state").GetAttribute("aria-label") ?? string.Empty).ShouldBe(state.AriaLabel);
    }

    [Theory]
    [InlineData(ProjectConsoleFeedback.SuccessCategory)]
    [InlineData(ProjectConsoleFeedback.WarningCategory)]
    [InlineData(ProjectConsoleFeedback.ErrorCategory)]
    [InlineData(ProjectConsoleFeedback.FailClosedCategory)]
    [InlineData(ProjectConsoleFeedback.LoadingCategory)]
    public void FeedbackCategoriesRenderSafeReasonCodes(string category)
    {
        ProjectConsoleFeedback feedback = category switch
        {
            ProjectConsoleFeedback.SuccessCategory => ProjectConsoleFeedback.Success("operation_completed", "corr-001"),
            ProjectConsoleFeedback.WarningCategory => ProjectConsoleFeedback.Warning("stale", "corr-001"),
            ProjectConsoleFeedback.ErrorCategory => ProjectConsoleFeedback.Error("validation_error", "corr-001"),
            ProjectConsoleFeedback.FailClosedCategory => ProjectConsoleFeedback.FailClosed("safe_denial", "corr-001"),
            _ => ProjectConsoleFeedback.Loading("operator_diagnostics_loading"),
        };

        IRenderedComponent<ProjectFeedbackView> cut = Render<ProjectFeedbackView>(parameters => parameters
            .Add(p => p.Feedback, feedback));

        cut.Find($"[data-testid='project-feedback-{category}']").TextContent.ShouldContain(feedback.SafeReasonCode);
        cut.Markup.ShouldNotContain("secret");
        cut.Markup.ShouldNotContain("token");
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(feedback));
    }

    [Theory]
    [InlineData("Bearer token raw transcript body")]
    [InlineData("secret:/unrestricted/path/private_key")]
    public void FeedbackReasonCodesAreSanitizedBeforeRendering(string unsafeReasonCode)
    {
        ProjectConsoleFeedback feedback = ProjectConsoleFeedback.Error(unsafeReasonCode, "corr-001");

        IRenderedComponent<ProjectFeedbackView> cut = Render<ProjectFeedbackView>(parameters => parameters
            .Add(p => p.Feedback, feedback));

        feedback.SafeReasonCode.ShouldBe("unsafe_reason_code");
        cut.Markup.ShouldContain("unsafe_reason_code");
        cut.Markup.ShouldNotContain("Bearer");
        cut.Markup.ShouldNotContain("token");
        cut.Markup.ShouldNotContain("transcript");
        cut.Markup.ShouldNotContain("private_key");
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(feedback));
    }

    [Theory]
    [MemberData(nameof(StatusDescriptors))]
    public void StatusBadgesRenderVisibleLabelsAndAccessibleNames(VocabularyDescriptor descriptor, string columnHeader)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(columnHeader);

        IRenderedComponent<ProjectStatusBadge> cut = Render<ProjectStatusBadge>(parameters => parameters
            .Add(p => p.Descriptor, descriptor)
            .Add(p => p.ColumnHeader, columnHeader));

        var badge = cut.Find("[data-testid='fc-status-badge']");
        badge.TextContent.ShouldContain(descriptor.DisplayLabel);
        string ariaLabel = badge.GetAttribute("aria-label") ?? string.Empty;
        ariaLabel.ShouldContain(columnHeader);
        ariaLabel.ShouldContain(descriptor.DisplayLabel);
    }

    public static TheoryData<ProjectEmptyState, string> RequiredEmptyStates()
        => new()
        {
            { ProjectEmptyState.NoProjects(), "No projects" },
            { ProjectEmptyState.NoReferences(), "No references" },
            { ProjectEmptyState.NoAudit(), "No audit events" },
            { ProjectEmptyState.AccessDenied(), "Access denied" },
            { ProjectEmptyState.DataUnavailable(), "temporarily unavailable" },
            { ProjectEmptyState.FilterReturnedNoResults(), "filter returned no results" },
        };

    public static TheoryData<VocabularyDescriptor, string> StatusDescriptors()
        => new()
        {
            { ProjectVocabularyDescriptors.Describe(ProjectLifecycle.Active), "Lifecycle" },
            { ProjectVocabularyDescriptors.Describe(ReferenceState.Included), "Reference state" },
            { ProjectVocabularyDescriptors.Describe(ResolutionResult.MultipleCandidates), "Resolution result" },
            { ProjectVocabularyDescriptors.Describe(ProjectReasonCode.ConversationLinked), "Reason code" },
        };
}
