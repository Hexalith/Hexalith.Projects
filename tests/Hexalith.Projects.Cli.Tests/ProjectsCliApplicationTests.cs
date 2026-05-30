// <copyright file="ProjectsCliApplicationTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Cli.Tests;

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
}
