// <copyright file="ProjectsMcpCommandServiceTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Mcp.Tests;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.Projects.Client.Generated;
using Hexalith.Projects.Mcp;

using NSubstitute;

using Shouldly;

using Xunit;

public sealed class ProjectsMcpCommandServiceTests
{
    [Fact]
    public async Task Archive_Dispatch_Uses_IdempotencyKey_Internally_But_Returns_Only_Safe_Ids()
    {
        IClient client = Substitute.For<IClient>();
        client.ArchiveProjectAsync(
                "project-1",
                "idem-secret",
                "corr-1",
                "task-1",
                Arg.Any<ArchiveProjectRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new AcceptedCommand
            {
                AcceptedAt = DateTimeOffset.UtcNow,
                CorrelationId = "corr-1",
                TaskId = "task-1",
                Status = AcceptedCommandStatus.Accepted,
            });
        var service = new ProjectsMcpCommandService(client);

        CommandResult result = await service.DispatchAsync(
            new ProjectsMcpMaintenanceCommand
            {
                Action = "archive",
                ProjectId = "project-1",
                Confirmed = true,
                DryRunEvidence = "preview-1",
                IdempotencyKey = "idem-secret",
                CorrelationId = "corr-1",
                CommandId = "task-1",
            },
            CancellationToken.None);

        result.Status.ShouldBe("Accepted");
        result.MessageId.ShouldBe("task-1");
        result.CorrelationId.ShouldBe("corr-1");
        result.ToString().ShouldNotContain("idem-secret");
    }

    [Fact]
    public async Task Mutation_Missing_Confirmation_Fails_Closed()
    {
        var service = new ProjectsMcpCommandService(Substitute.For<IClient>());

        await Should.ThrowAsync<CommandValidationException>(() => service.DispatchAsync(
            new ProjectsMcpMaintenanceCommand
            {
                Action = "archive",
                ProjectId = "project-1",
                DryRunEvidence = "preview-1",
                IdempotencyKey = "idem-secret",
            },
            CancellationToken.None));
    }
}
