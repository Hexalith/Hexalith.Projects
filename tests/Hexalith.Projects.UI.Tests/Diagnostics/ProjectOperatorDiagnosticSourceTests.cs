// <copyright file="ProjectOperatorDiagnosticSourceTests.cs" company="Hexalith">
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
/// Tests for the generated-client diagnostic source.
/// </summary>
public sealed class ProjectOperatorDiagnosticSourceTests
{
    [Fact]
    public async Task SourceUsesGeneratedQueryClientWithEventualFreshness()
    {
        IClient client = Substitute.For<IClient>();
        ProjectOperatorDiagnostic diagnostic = CreateGeneratedDiagnostic();
        client.GetProjectOperatorDiagnosticsAsync(
                "project-001",
                25,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(diagnostic));

        var source = new ProjectOperatorDiagnosticSource(client);
        ProjectDiagnosticLoadResult result = await source
            .GetProjectDiagnosticsAsync("project-001", CancellationToken.None)
            .ConfigureAwait(true);

        result.Diagnostic.ShouldNotBeNull();
        result.Diagnostic.ProjectId.ShouldBe(diagnostic.ProjectId);
        result.Feedback.ShouldBeNull();
        await client.Received(1).GetProjectOperatorDiagnosticsAsync(
            "project-001",
            25,
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
    [InlineData(500, ProjectConsoleFeedback.ErrorCategory, "diagnostic_query_failed")]
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

        var source = new ProjectOperatorDiagnosticSource(client);
        ProjectDiagnosticLoadResult result = await source
            .GetProjectDiagnosticsAsync("project-001", CancellationToken.None)
            .ConfigureAwait(true);

        result.Diagnostic.ShouldBeNull();
        result.Feedback.ShouldNotBeNull();
        result.Feedback.Category.ShouldBe(category);
        result.Feedback.SafeReasonCode.ShouldBe(reasonCode);
        result.Feedback.Message.ShouldNotContain("secret");
        result.Feedback.Message.ShouldNotContain("token");
        result.Feedback.Message.ShouldNotContain("transcript");
    }

    private static ProjectOperatorDiagnostic CreateGeneratedDiagnostic()
        => new()
        {
            ProjectId = "project-001",
            Name = "Console Project",
            Description = null,
            LifecycleState = ProjectLifecycleState.Active,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch,
            SetupMetadata = null,
            ProjectSetup = null,
            ContextActivation = new ContextActivation { Enabled = true },
            References = [],
            AuditTimeline = [],
            Freshness = new FreshnessMetadata
            {
                ReadConsistency = ReadConsistencyClass.Eventually_consistent,
                ObservedAt = DateTimeOffset.UnixEpoch,
                ProjectionWatermark = null,
                Stale = false,
                TrustState = ProjectionTrustState.Trusted,
            },
        };
}
