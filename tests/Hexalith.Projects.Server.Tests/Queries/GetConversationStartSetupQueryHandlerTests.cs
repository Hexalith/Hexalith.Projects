// <copyright file="GetConversationStartSetupQueryHandlerTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Queries;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Server;
using Hexalith.Projects.Server.Queries;

using Shouldly;

using Xunit;

/// <summary>Tests the supported Conversation-start DomainService query handler.</summary>
public sealed class GetConversationStartSetupQueryHandlerTests
{
    private const string ProjectId = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";
    private static readonly DateTimeOffset ObservedAt = new(2026, 9, 5, 8, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ExecuteAsync_ActiveProject_ReturnsBoundedSetupAndCompleteSnapshot()
    {
        ProjectSetup setup = new(
            ["goal"],
            ["instruction"],
            [ProjectContextSourceKind.Conversation],
            [ProjectContextSourceKind.FileReference],
            new ConversationStartDefaults(LinkedSourcePolicy.AuthorizedReferences));
        var handler = new GetConversationStartSetupQueryHandler(new StubReadModel(Detail(ProjectLifecycle.Active, setup, hasFolder: true)));

        QueryResult result = await handler.ExecuteAsync(Query(), CancellationToken.None);

        result.Success.ShouldBeTrue();
        ConversationStartSetupResponse response = JsonSerializer.Deserialize<ConversationStartSetupResponse>(result.PayloadBytes!, JsonOptions)!;
        response.Setup!.Goals.ShouldBe(["goal"]);
        response.Snapshot.ResponseState.ShouldBe(ConversationStartResponseState.Complete);
        response.Snapshot.ProjectVersion.ShouldBe(4);
        response.Snapshot.AsOf.ShouldBe(ObservedAt);
    }

    [Fact]
    public async Task ExecuteAsync_ArchivedProject_ReturnsSafeDenial()
    {
        var handler = new GetConversationStartSetupQueryHandler(new StubReadModel(Detail(ProjectLifecycle.Archived, ProjectSetup.Empty, hasFolder: true)));

        QueryResult result = await handler.ExecuteAsync(Query(), CancellationToken.None);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("safe-denial");
        result.PayloadBytes.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_MissingFolder_ReturnsUnavailableWithoutSetup()
    {
        var handler = new GetConversationStartSetupQueryHandler(new StubReadModel(Detail(ProjectLifecycle.Active, ProjectSetup.Empty, hasFolder: false)));

        QueryResult result = await handler.ExecuteAsync(Query(), CancellationToken.None);

        ConversationStartSetupResponse response = JsonSerializer.Deserialize<ConversationStartSetupResponse>(result.PayloadBytes!, JsonOptions)!;
        response.Setup.ShouldBeNull();
        response.Snapshot.ResponseState.ShouldBe(ConversationStartResponseState.Unavailable);
        response.Snapshot.RecoveryActions.ShouldContain("RefreshContext");
    }

    private static QueryEnvelope Query()
        => new("tenant-a", ProjectsServerModule.DomainName, ProjectId, ProjectsServerModule.GetConversationStartSetupQueryType, [], "corr-1", "actor-1");

    private static ProjectDetailItem Detail(ProjectLifecycle lifecycle, ProjectSetup setup, bool hasFolder)
        => new(
            "tenant-a",
            ProjectId,
            "Project",
            null,
            null,
            setup,
            hasFolder ? new ProjectFolderReference("folder-1", "Folder", ReferenceState.Included, null, ObservedAt) : null,
            [],
            [],
            lifecycle,
            ObservedAt,
            ObservedAt,
            4);

    private sealed class StubReadModel(ProjectDetailItem? detail) : IProjectDetailReadModel
    {
        public Task<ProjectDetailItem?> GetAsync(string authoritativeTenantId, string projectId, CancellationToken cancellationToken = default)
            => Task.FromResult(detail is not null
                && detail.TenantId == authoritativeTenantId
                && detail.ProjectId == projectId
                ? detail
                : null);
    }
}
