// <copyright file="ConversationStartSetupProjectionHandlerTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Projections.ConversationStartSetup;

using System;
using System.Text.Json;

using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Server.Projections.ConversationStartSetup;

using Shouldly;

using Xunit;

/// <summary>Tests the named persisted Conversation-start projection handler.</summary>
public sealed class ConversationStartSetupProjectionHandlerTests
{
    private const string TenantId = "tenant-a";
    private const string ProjectId = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";
    private static readonly DateTimeOffset CreatedAt = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAt = new(2026, 9, 5, 8, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ProjectAsync_IncrementalSliceOverPriorPersistedState_FoldsOntoExistingItem()
    {
        InMemoryReadModelStore store = new();
        string key = ConversationStartSetupProjectionHandler.Key(TenantId, ProjectId);
        ProjectDetailItem seed = new(
            TenantId,
            ProjectId,
            "Project",
            null,
            null,
            null,
            null,
            [],
            [],
            ProjectLifecycle.Active,
            CreatedAt,
            CreatedAt,
            1);
        await store.SaveAsync(ConversationStartSetupProjectionHandler.StoreName, key, seed, TestContext.Current.CancellationToken).ConfigureAwait(true);
        ConversationStartSetupProjectionHandler handler = new(store);

        ProjectSetup setup = new(["goal"], ["instruction"], [], [], null);
        var setupUpdated = new ProjectSetupUpdated(TenantId, ProjectId, setup, "actor-1", "corr-1", "task-1", "idem-1", "fp-1", UpdatedAt);
        ProjectionRequest request = new(
            TenantId,
            "projects",
            ProjectId,
            [EventDto(setupUpdated, sequenceNumber: 2)]);

        DomainProjectionHandlerResult result = await handler.ProjectAsync(request, "dispatch-1", TestContext.Current.CancellationToken);

        result.Status.ShouldBe(ProjectionDispatchStatus.Completed);
        ProjectDetailItem? persisted = store.Snapshot<ProjectDetailItem>(ConversationStartSetupProjectionHandler.StoreName, key);
        persisted.ShouldNotBeNull();
        persisted.Name.ShouldBe("Project");
        persisted.CreatedAt.ShouldBe(CreatedAt);
        persisted.Setup.ShouldNotBeNull();
        persisted.Setup!.Goals.ShouldBe(["goal"]);
        persisted.Sequence.ShouldBe(2);
    }

    [Fact]
    public async Task ProjectAsync_MalformedEventPayload_ReturnsFailedInsteadOfThrowing()
    {
        InMemoryReadModelStore store = new();
        ConversationStartSetupProjectionHandler handler = new(store);
        ProjectionEventDto malformed = new(
            typeof(ProjectCreated).FullName!,
            [0x7B],
            "json",
            1,
            UpdatedAt,
            "corr-1");
        ProjectionRequest request = new(TenantId, "projects", ProjectId, [malformed]);

        DomainProjectionHandlerResult result = await handler.ProjectAsync(request, "dispatch-1", TestContext.Current.CancellationToken);

        result.Status.ShouldBe(ProjectionDispatchStatus.Failed);
        result.ReasonCode.ShouldBe("invalid-event-payload");
    }

    private static ProjectionEventDto EventDto(IProjectEvent projectEvent, long sequenceNumber)
        => new(
            projectEvent.GetType().FullName!,
            JsonSerializer.SerializeToUtf8Bytes(projectEvent, projectEvent.GetType(), JsonOptions),
            "json",
            sequenceNumber,
            UpdatedAt,
            "corr-1");
}
