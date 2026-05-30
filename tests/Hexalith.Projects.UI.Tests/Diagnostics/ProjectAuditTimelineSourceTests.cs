// <copyright file="ProjectAuditTimelineSourceTests.cs" company="Hexalith">
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
/// Tests for bounded audit timeline reloads over the existing operator diagnostic endpoint.
/// </summary>
public sealed class ProjectAuditTimelineSourceTests
{
    [Theory]
    [InlineData(null, 25)]
    [InlineData(0, 25)]
    [InlineData(50, 50)]
    [InlineData(500, 100)]
    public async Task SourceUsesOperatorDiagnosticsWithBoundedLimitAndEventualFreshness(int? requestedLimit, int expectedLimit)
    {
        IClient client = Substitute.For<IClient>();
        client.GetProjectOperatorDiagnosticsAsync(
                "project-001",
                expectedLimit,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateDiagnostic()));

        var source = new ProjectAuditTimelineSource(client);
        ProjectAuditTimelineLoadResult result = await source
            .GetAuditTimelineAsync("project-001", requestedLimit, CancellationToken.None)
            .ConfigureAwait(true);

        result.Feedback.ShouldBeNull();
        result.Rows.ShouldHaveSingleItem();
        result.Freshness.ShouldNotBeNull();
        await client.Received(1).GetProjectOperatorDiagnosticsAsync(
            "project-001",
            expectedLimit,
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
    [InlineData(500, ProjectConsoleFeedback.ErrorCategory, "audit_timeline_query_failed")]
    public async Task SourceMapsFailuresToSafeFeedback(int statusCode, string category, string reasonCode)
    {
        IClient client = Substitute.For<IClient>();
        client.GetProjectOperatorDiagnosticsAsync(
                "project-001",
                25,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns<Task<ProjectOperatorDiagnostic>>(_ => throw new HexalithProjectsApiException(
                "unsafe response hidden",
                statusCode,
                "secret token transcript body",
                new Dictionary<string, IEnumerable<string>>(),
                null!));

        var source = new ProjectAuditTimelineSource(client);
        ProjectAuditTimelineLoadResult result = await source
            .GetAuditTimelineAsync("project-001", 25, CancellationToken.None)
            .ConfigureAwait(true);

        result.Rows.ShouldBeEmpty();
        result.Feedback.ShouldNotBeNull();
        result.Feedback.Category.ShouldBe(category);
        result.Feedback.SafeReasonCode.ShouldBe(reasonCode);
        result.Feedback.Message.ShouldNotContain("secret");
        result.Feedback.Message.ShouldNotContain("token");
        result.Feedback.Message.ShouldNotContain("transcript");
    }

    [Fact]
    public async Task SourceMapsTransportFailuresToSafeFeedbackWithoutLeakingExceptionText()
    {
        IClient client = Substitute.For<IClient>();
        client.GetProjectOperatorDiagnosticsAsync(
                "project-001",
                25,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns<Task<ProjectOperatorDiagnostic>>(_ => throw new InvalidOperationException(
                "secret token transcript raw ProblemDetails body"));

        var source = new ProjectAuditTimelineSource(client);
        ProjectAuditTimelineLoadResult result = await source
            .GetAuditTimelineAsync("project-001", 25, CancellationToken.None)
            .ConfigureAwait(true);

        result.Rows.ShouldBeEmpty();
        result.Feedback.ShouldNotBeNull();
        result.Feedback.Category.ShouldBe(ProjectConsoleFeedback.ErrorCategory);
        result.Feedback.SafeReasonCode.ShouldBe("audit_timeline_query_failed");
        result.Feedback.Message.ShouldNotContain("secret");
        result.Feedback.Message.ShouldNotContain("token");
        result.Feedback.Message.ShouldNotContain("transcript");
        result.Feedback.Message.ShouldNotContain("ProblemDetails");
    }

    private static ProjectOperatorDiagnostic CreateDiagnostic()
        => new()
        {
            ProjectId = "project-001",
            Name = "Detail Project",
            LifecycleState = ProjectLifecycleState.Active,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch,
            ContextActivation = new ContextActivation { Enabled = true },
            References = [],
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
            Freshness = new FreshnessMetadata
            {
                ReadConsistency = ReadConsistencyClass.Eventually_consistent,
                ObservedAt = DateTimeOffset.UnixEpoch,
                Stale = false,
                TrustState = ProjectionTrustState.Trusted,
            },
        };
}
