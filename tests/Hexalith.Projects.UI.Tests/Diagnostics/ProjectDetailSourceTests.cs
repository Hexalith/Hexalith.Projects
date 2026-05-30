// <copyright file="ProjectDetailSourceTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Tests.Diagnostics;

using Hexalith.Projects.Client.Generated;
using Hexalith.Projects.UI.Diagnostics;
using Hexalith.Projects.UI.Rendering;

using NSubstitute;

using Shouldly;

using Xunit;

/// <summary>
/// Tests for the generated-client backed detail source.
/// </summary>
public sealed class ProjectDetailSourceTests
{
    [Fact]
    public async Task SourceUsesGetProjectAndDiagnosticsWithEventualFreshness()
    {
        IClient client = Substitute.For<IClient>();
        client.GetProjectAsync(
                "project-001",
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateProject()));
        client.GetProjectOperatorDiagnosticsAsync(
                "project-001",
                25,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateDiagnostic()));
        client.GetProjectContextExplanationAsync(
                "project-001",
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateExplanation()));
        client.ListProjectConversationsAsync(
                "project-001",
                100,
                Arg.Any<string>(),
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateConversations()));

        var source = new ProjectDetailSource(client);
        ProjectDetailLoadResult result = await source
            .GetProjectDetailAsync("project-001", CancellationToken.None)
            .ConfigureAwait(true);

        result.Feedback.ShouldBeNull();
        result.Detail.ShouldNotBeNull();
        result.Detail.ProjectId.ShouldBe("project-001");
        result.Detail.ProjectSetup.ShouldNotBeNull();
        result.Detail.References.ShouldHaveSingleItem();
        result.ReferenceHealthRows.Count.ShouldBe(2);
        result.ReferenceHealthRows.Any(row => row.ReferenceKind == "conversation" && row.ReferenceId == "conversation-001").ShouldBeTrue();
        result.Detail.AuditTimeline.ShouldHaveSingleItem();
        await client.Received(1).GetProjectAsync(
            "project-001",
            Arg.Is<string>(s => !string.IsNullOrWhiteSpace(s)),
            ReadConsistencyClass.Eventually_consistent,
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await client.Received(1).GetProjectOperatorDiagnosticsAsync(
            "project-001",
            25,
            Arg.Is<string>(s => !string.IsNullOrWhiteSpace(s)),
            ReadConsistencyClass.Eventually_consistent,
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await client.Received(1).GetProjectContextExplanationAsync(
            "project-001",
            Arg.Is<string>(s => !string.IsNullOrWhiteSpace(s)),
            ReadConsistencyClass.Eventually_consistent,
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await client.Received(1).ListProjectConversationsAsync(
            "project-001",
            100,
            Arg.Any<string>(),
            Arg.Is<string>(s => !string.IsNullOrWhiteSpace(s)),
            ReadConsistencyClass.Eventually_consistent,
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Theory]
    [InlineData(400, ProjectConsoleFeedback.ErrorCategory, "validation_error")]
    [InlineData(401, ProjectConsoleFeedback.FailClosedCategory, "safe_denial")]
    [InlineData(403, ProjectConsoleFeedback.FailClosedCategory, "safe_denial")]
    [InlineData(404, ProjectConsoleFeedback.FailClosedCategory, "safe_denial")]
    [InlineData(503, ProjectConsoleFeedback.WarningCategory, "data_unavailable")]
    [InlineData(500, ProjectConsoleFeedback.ErrorCategory, "detail_query_failed")]
    public async Task SourceMapsBaseDetailFailuresToSafeFeedback(int statusCode, string category, string reasonCode)
    {
        IClient client = Substitute.For<IClient>();
        client.GetProjectAsync(
                "project-001",
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns<Task<Project>>(_ => throw new HexalithProjectsApiException(
                "unsafe response hidden",
                statusCode,
                "secret token transcript body",
                new Dictionary<string, IEnumerable<string>>(),
                null!));

        var source = new ProjectDetailSource(client);
        ProjectDetailLoadResult result = await source
            .GetProjectDetailAsync("project-001", CancellationToken.None)
            .ConfigureAwait(true);

        result.Detail.ShouldBeNull();
        result.Feedback.ShouldNotBeNull();
        result.Feedback.Category.ShouldBe(category);
        result.Feedback.SafeReasonCode.ShouldBe(reasonCode);
        result.Feedback.Message.ShouldNotContain("secret");
        result.Feedback.Message.ShouldNotContain("token");
        result.Feedback.Message.ShouldNotContain("transcript");

        // A failed base detail must short-circuit before the bounded diagnostics call (independently
        // recoverable contract): the secondary query must never fire when base detail fails.
        await client.DidNotReceive()
            .GetProjectOperatorDiagnosticsAsync(
                Arg.Any<string>(),
                Arg.Any<int?>(),
                Arg.Any<string>(),
                Arg.Any<ReadConsistencyClass?>(),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task SourceCollapsesCrossTenantDenialWithoutRenderingSiblingMetadata()
    {
        IClient client = Substitute.For<IClient>();
        client.GetProjectAsync(
                "project-hidden",
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns<Task<Project>>(_ => throw new HexalithProjectsApiException(
                "cross tenant project-hidden project-visible sibling-secret hidden-descriptor",
                403,
                "{\"projectId\":\"project-visible\",\"name\":\"Sibling Secret\",\"denial\":\"sibling detail\"}",
                new Dictionary<string, IEnumerable<string>>(),
                null!));

        var source = new ProjectDetailSource(client);
        ProjectDetailLoadResult result = await source
            .GetProjectDetailAsync("project-hidden", CancellationToken.None)
            .ConfigureAwait(true);

        result.Detail.ShouldBeNull();
        result.Feedback.ShouldNotBeNull();
        result.Feedback.Category.ShouldBe(ProjectConsoleFeedback.FailClosedCategory);
        result.Feedback.SafeReasonCode.ShouldBe("safe_denial");
        string rendered = string.Join(
            ' ',
            result.Feedback.Message,
            result.Feedback.SafeReasonCode,
            result.Feedback.CorrelationId);
        rendered.ShouldNotContain("project-visible");
        rendered.ShouldNotContain("Sibling Secret");
        rendered.ShouldNotContain("hidden-descriptor");
        await client.DidNotReceive()
            .GetProjectOperatorDiagnosticsAsync(
                Arg.Any<string>(),
                Arg.Any<int?>(),
                Arg.Any<string>(),
                Arg.Any<ReadConsistencyClass?>(),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task SourceMapsTransportFailuresToSafeFeedbackWithoutCrashing()
    {
        IClient client = Substitute.For<IClient>();
        client.GetProjectAsync(
                "project-001",
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns<Task<Project>>(_ => throw new System.Net.Http.HttpRequestException("connection refused 10.0.0.1 secret"));

        var source = new ProjectDetailSource(client);
        ProjectDetailLoadResult result = await source
            .GetProjectDetailAsync("project-001", CancellationToken.None)
            .ConfigureAwait(true);

        result.Detail.ShouldBeNull();
        result.Feedback.ShouldNotBeNull();
        result.Feedback.Category.ShouldBe(ProjectConsoleFeedback.ErrorCategory);
        result.Feedback.SafeReasonCode.ShouldBe("detail_query_failed");
        result.Feedback.Message.ShouldNotContain("secret");
        result.Feedback.Message.ShouldNotContain("10.0.0.1");
    }

    [Fact]
    public async Task SourceKeepsBaseDetailWhenDiagnosticsAreUnavailable()
    {
        IClient client = Substitute.For<IClient>();
        client.GetProjectAsync(
                "project-001",
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateProject()));
        client.GetProjectOperatorDiagnosticsAsync(
                "project-001",
                25,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns<Task<ProjectOperatorDiagnostic>>(_ => throw new HexalithProjectsApiException(
                "unsafe response hidden",
                503,
                "secret token transcript body",
                new Dictionary<string, IEnumerable<string>>(),
                null!));
        client.GetProjectContextExplanationAsync(
                "project-001",
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateExplanation()));
        client.ListProjectConversationsAsync(
                "project-001",
                100,
                Arg.Any<string>(),
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateConversations()));

        var source = new ProjectDetailSource(client);
        ProjectDetailLoadResult result = await source
            .GetProjectDetailAsync("project-001", CancellationToken.None)
            .ConfigureAwait(true);

        result.Detail.ShouldNotBeNull();
        result.Detail.ProjectId.ShouldBe("project-001");
        result.DiagnosticFeedback.ShouldNotBeNull();
        result.DiagnosticFeedback.Category.ShouldBe(ProjectConsoleFeedback.WarningCategory);
        result.DiagnosticFeedback.SafeReasonCode.ShouldBe("data_unavailable");
        result.ReferenceHealthRows.Count.ShouldBe(2);
    }

    [Fact]
    public async Task SourceMapsReferenceHealthFailuresToSafeNonBlockingFeedback()
    {
        IClient client = Substitute.For<IClient>();
        client.GetProjectAsync(
                "project-001",
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateProject()));
        client.GetProjectOperatorDiagnosticsAsync(
                "project-001",
                25,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateDiagnostic()));
        client.GetProjectContextExplanationAsync(
                "project-001",
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns<Task<ProjectContextExplanation>>(_ => throw new HexalithProjectsApiException(
                "unsafe response hidden",
                400,
                "secret token transcript body",
                new Dictionary<string, IEnumerable<string>>(),
                null!));
        client.ListProjectConversationsAsync(
                "project-001",
                100,
                Arg.Any<string>(),
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns<Task<ProjectConversationsPage>>(_ => throw new System.Net.Http.HttpRequestException("secret upstream body"));

        var source = new ProjectDetailSource(client);
        ProjectDetailLoadResult result = await source
            .GetProjectDetailAsync("project-001", CancellationToken.None)
            .ConfigureAwait(true);

        result.Detail.ShouldNotBeNull();
        result.ReferenceHealthRows.ShouldHaveSingleItem();
        result.DiagnosticFeedback.ShouldNotBeNull();
        result.DiagnosticFeedback.SafeReasonCode.ShouldBe("validation_error");
        result.DiagnosticFeedback.Message.ShouldNotContain("secret");
        result.DiagnosticFeedback.Message.ShouldNotContain("transcript");
    }

    private static Project CreateProject()
        => new()
        {
            ProjectId = "project-001",
            Name = "Detail Project",
            Description = "Safe metadata",
            LifecycleState = ProjectLifecycleState.Active,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch.AddMinutes(1),
            SetupMetadata = "safe-setup",
            ProjectSetup = new ProjectSetup
            {
                Goals = ["keep continuity"],
                UserInstructions = ["use safe references"],
                PreferredSourceKinds = [ProjectContextSourceKind.Conversation],
                ExcludedSourceKinds = [ProjectContextSourceKind.FileReference],
                ConversationStartDefaults = new ConversationStartDefaults
                {
                    LinkedSourcePolicy = LinkedSourcePolicy.AuthorizedReferences,
                },
            },
            ContextActivation = new ContextActivation { Enabled = true },
            References = [CreateReference()],
            Freshness = Freshness(),
        };

    private static ProjectOperatorDiagnostic CreateDiagnostic()
        => new()
        {
            ProjectId = "project-001",
            Name = "Detail Project",
            Description = "Safe metadata",
            LifecycleState = ProjectLifecycleState.Active,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch.AddMinutes(1),
            ContextActivation = new ContextActivation { Enabled = true },
            References = [CreateReference()],
            AuditTimeline =
            [
                new ProjectOperatorAuditTimelineItem
                {
                    AuditEventId = "audit-001",
                    OperationType = "project.created",
                    OccurredAt = DateTimeOffset.UnixEpoch,
                    ActorPrincipalId = "actor-001",
                    CorrelationId = "corr-001",
                    TaskId = "task-001",
                    ProjectionSequence = 1,
                },
            ],
            Freshness = Freshness(),
        };

    private static ProjectReferenceSummary CreateReference()
        => new()
        {
            ReferenceKind = ProjectReferenceSummaryReferenceKind.Folder,
            ReferenceState = ProjectReferenceSummaryReferenceState.Included,
            ReferenceId = "folder-001",
            DisplayName = "Folder",
            Freshness = Freshness(),
        };

    private static ProjectContextExplanation CreateExplanation()
        => new()
        {
            Context = new ProjectContext
            {
                ProjectId = "project-001",
                Lifecycle = ProjectContextLifecycle.Active,
                AssemblyOutcome = ProjectContextAssemblyOutcome.Assembled,
                ObservedAt = DateTimeOffset.UnixEpoch,
                Freshness = ProjectContextFreshness.Fresh,
            },
            Evaluations =
            [
                new ProjectContextEvaluation
                {
                    ReferenceKind = ProjectContextEvaluationReferenceKind.Folder,
                    ReferenceId = "folder-001",
                    ResultState = ProjectContextEvaluationResultState.Included,
                    FailedCheck = null,
                    ReasonCode = ProjectContextEvaluationReasonCode.ProjectFolderMatched,
                    ObservedAt = DateTimeOffset.UnixEpoch.AddMinutes(2),
                },
            ],
        };

    private static ProjectConversationsPage CreateConversations()
        => new()
        {
            ProjectId = "project-001",
            TrustSignal = ProjectConversationTrustSignal.Current,
            Items =
            [
                new ProjectConversationItem
                {
                    ProjectId = "project-001",
                    ConversationId = "conversation-001",
                    DisplayLabel = "Support conversation",
                    LifecycleStatus = "Active",
                    ProjectSafeLabel = "Detail Project",
                    ProjectSafeStatus = "Active",
                    TrustSignal = ProjectConversationTrustSignal.Current,
                },
            ],
            Page = new ProjectConversationPageMetadata { ReturnedCount = 1 },
        };

    private static FreshnessMetadata Freshness()
        => new()
        {
            ReadConsistency = ReadConsistencyClass.Eventually_consistent,
            ObservedAt = DateTimeOffset.UnixEpoch,
            ProjectionWatermark = "watermark-001",
            Stale = false,
            TrustState = ProjectionTrustState.Trusted,
        };
}
