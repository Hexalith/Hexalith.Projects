// <copyright file="ProjectsCliApplicationTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Cli.Tests;

using System.Text.Json;

using Hexalith.Projects.Cli;
using Hexalith.Projects.Client.Generated;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using Xunit;

public sealed class ProjectsCliApplicationTests
{
    [Fact]
    public async Task List_Writes_Json_To_Stdout_And_Keeps_Stderr_Empty()
    {
        IClient client = Substitute.For<IClient>();
        client.ListProjectsAsync(
                Lifecycle.All,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(new ProjectListResponse
            {
                Items =
                {
                    new ProjectListItem
                    {
                        ProjectId = "project-1",
                        Name = "Ops",
                        LifecycleState = ProjectLifecycleState.Active,
                        UpdatedAt = DateTimeOffset.UnixEpoch,
                        Freshness = new FreshnessMetadata
                        {
                            ReadConsistency = ReadConsistencyClass.Eventually_consistent,
                            ObservedAt = DateTimeOffset.UnixEpoch,
                            ProjectionWatermark = "42",
                            TrustState = ProjectionTrustState.Trusted,
                        },
                    },
                },
            });
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var app = new ProjectsCliApplication(client, stdout, stderr);

        int exitCode = await app.RunAsync(["projects", "list"], TestContext.Current.CancellationToken);

        exitCode.ShouldBe(ProjectsCliExitCodes.Success);
        stderr.ToString().ShouldBeEmpty();
        stdout.ToString().ShouldContain("\"projectId\":\"project-1\"");
        stdout.ToString().ShouldContain("\"payloadExcluded\":true");
    }

    [Fact]
    public async Task Archive_Missing_Confirmation_Returns_Validation_And_Only_Stderr()
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var app = new ProjectsCliApplication(Substitute.For<IClient>(), stdout, stderr);

        int exitCode = await app.RunAsync([
            "projects",
            "archive",
            "--project-id",
            "project-1",
            "--idempotency-key",
            "idem-secret",
            "--correlation-id",
            "corr-1",
            "--task-id",
            "task-1",
        ], TestContext.Current.CancellationToken);

        exitCode.ShouldBe(ProjectsCliExitCodes.Validation);
        stdout.ToString().ShouldBeEmpty();
        stderr.ToString().ShouldContain("confirmation_required");
        stderr.ToString().ShouldNotContain("idem-secret");
    }

    [Theory]
    [InlineData(400, ProjectsCliExitCodes.Validation, "validation_error")]
    [InlineData(401, ProjectsCliExitCodes.DenialOrNotFound, "safe_denial")]
    [InlineData(403, ProjectsCliExitCodes.DenialOrNotFound, "safe_denial")]
    [InlineData(404, ProjectsCliExitCodes.DenialOrNotFound, "safe_denial")]
    [InlineData(503, ProjectsCliExitCodes.Unavailable, "data_unavailable")]
    public async Task Describe_Maps_Api_Failure_To_Stable_ExitCode_And_Sanitized_Stderr(
        int status,
        int expectedExit,
        string expectedCode)
    {
        IClient client = Substitute.For<IClient>();
        client.GetProjectAsync(
                "project-1",
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HexalithProjectsApiException(
                "blocked",
                status,
                "{\"problem\":\"secret-problem-detail\"}",
                new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal),
                null!));
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var app = new ProjectsCliApplication(client, stdout, stderr);

        int exitCode = await app.RunAsync(
            ["projects", "describe", "--project-id", "project-1"],
            TestContext.Current.CancellationToken);

        exitCode.ShouldBe(expectedExit);
        stdout.ToString().ShouldBeEmpty();
        stderr.ToString().Trim().ShouldBe(expectedCode);
        stderr.ToString().ShouldNotContain("secret-problem-detail");
    }

    [Fact]
    public async Task Describe_Collapses_CrossTenant_Denial_Without_Sibling_Metadata()
    {
        IClient client = Substitute.For<IClient>();
        client.GetProjectAsync(
                "project-hidden",
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HexalithProjectsApiException(
                "cross tenant project-hidden project-visible hidden descriptor",
                403,
                "{\"projectId\":\"project-visible\",\"name\":\"Sibling Secret\",\"denial\":\"sibling detail\"}",
                new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal),
                null!));
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var app = new ProjectsCliApplication(client, stdout, stderr);

        int exitCode = await app.RunAsync(
            ["projects", "describe", "--project-id", "project-hidden"],
            TestContext.Current.CancellationToken);

        exitCode.ShouldBe(ProjectsCliExitCodes.DenialOrNotFound);
        stdout.ToString().ShouldBeEmpty();
        stderr.ToString().Trim().ShouldBe("safe_denial");
        stderr.ToString().ShouldNotContain("project-visible");
        stderr.ToString().ShouldNotContain("Sibling Secret");
        stderr.ToString().ShouldNotContain("hidden descriptor");
    }

    [Fact]
    public async Task Warnings_And_Dashboard_Expose_Story511_Parity_Fields_And_Partial_Failure_Count()
    {
        IClient client = Substitute.For<IClient>();
        client.ListProjectsAsync(
                Lifecycle.All,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(ProjectList("project-1", "project-2"));
        client.GetProjectOperatorDiagnosticsAsync(
                "project-1",
                25,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(DiagnosticWithWarning("project-1"));
        client.GetProjectOperatorDiagnosticsAsync(
                "project-2",
                25,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HexalithProjectsApiException(
                "unavailable",
                503,
                "{\"problem\":\"secret-problem-detail\"}",
                new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal),
                null!));

        using var warningsStdout = new StringWriter();
        using var warningsStderr = new StringWriter();
        var warningsApp = new ProjectsCliApplication(client, warningsStdout, warningsStderr);

        int warningsExit = await warningsApp.RunAsync(["projects", "warnings"], TestContext.Current.CancellationToken);

        warningsExit.ShouldBe(ProjectsCliExitCodes.Success);
        warningsStderr.ToString().ShouldBeEmpty();
        using JsonDocument warnings = JsonDocument.Parse(warningsStdout.ToString());
        JsonElement warningsRoot = warnings.RootElement;
        warningsRoot.GetProperty("tenantScope").GetString().ShouldBe("server-derived tenant");
        warningsRoot.GetProperty("payloadExcluded").GetBoolean().ShouldBeTrue();
        warningsRoot.GetProperty("diagnosticUnavailable").GetInt32().ShouldBe(1);
        JsonElement firstWarning = warningsRoot.GetProperty("items").EnumerateArray().Single();
        firstWarning.GetProperty("referenceKind").GetString().ShouldBe("memory");
        firstWarning.GetProperty("referenceState").GetString().ShouldBe("stale");
        firstWarning.GetProperty("reasonCode").GetString().ShouldBe("MemoryMatched");
        warningsStdout.ToString().ShouldNotContain("secret-problem-detail");

        using var dashboardStdout = new StringWriter();
        using var dashboardStderr = new StringWriter();
        var dashboardApp = new ProjectsCliApplication(client, dashboardStdout, dashboardStderr);

        int dashboardExit = await dashboardApp.RunAsync(["projects", "dashboard"], TestContext.Current.CancellationToken);

        dashboardExit.ShouldBe(ProjectsCliExitCodes.Success);
        dashboardStderr.ToString().ShouldBeEmpty();
        using JsonDocument dashboard = JsonDocument.Parse(dashboardStdout.ToString());
        JsonElement dashboardRoot = dashboard.RootElement;
        dashboardRoot.GetProperty("totalVisibleProjects").GetInt32().ShouldBe(2);
        dashboardRoot.GetProperty("projectsWithWarnings").GetInt32().ShouldBe(1);
        dashboardRoot.GetProperty("diagnosticUnavailable").GetInt32().ShouldBe(1);
        dashboardRoot.GetProperty("payloadExcluded").GetBoolean().ShouldBeTrue();
        dashboardStdout.ToString().ShouldNotContain("secret-problem-detail");
    }

    private static ProjectListResponse ProjectList(params string[] projectIds)
    {
        var response = new ProjectListResponse();
        foreach (string projectId in projectIds)
        {
            response.Items.Add(new ProjectListItem
            {
                ProjectId = projectId,
                Name = projectId,
                LifecycleState = ProjectLifecycleState.Active,
                UpdatedAt = DateTimeOffset.UnixEpoch,
                Freshness = Freshness(),
            });
        }

        return response;
    }

    private static ProjectOperatorDiagnostic DiagnosticWithWarning(string projectId)
        => new()
        {
            ProjectId = projectId,
            Name = projectId,
            LifecycleState = ProjectLifecycleState.Active,
            Freshness = Freshness(),
            References =
            {
                new ProjectReferenceSummary
                {
                    ReferenceKind = ProjectReferenceSummaryReferenceKind.Memory,
                    ReferenceState = ProjectReferenceSummaryReferenceState.Stale,
                    ReferenceId = "memory-001",
                    ReasonCode = "MemoryMatched",
                    Freshness = Freshness(),
                },
            },
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
