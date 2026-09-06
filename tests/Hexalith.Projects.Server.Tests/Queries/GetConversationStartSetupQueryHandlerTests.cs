// <copyright file="GetConversationStartSetupQueryHandlerTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Queries;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Server;
using Hexalith.Projects.Server.Projections.ConversationStartSetup;
using Hexalith.Projects.Server.Queries;

using Shouldly;

using Xunit;

/// <summary>Tests the supported Conversation-start DomainService query handler.</summary>
public sealed class GetConversationStartSetupQueryHandlerTests
{
    private const string TenantId = "tenant-a";
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
        GetConversationStartSetupQueryHandler handler = await CreateHandlerAsync(
            Detail(ProjectLifecycle.Active, setup, hasFolder: true)).ConfigureAwait(true);

        QueryResult result = await handler.ExecuteAsync(Query(), TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue();
        ConversationStartSetupResponse response = JsonSerializer.Deserialize<ConversationStartSetupResponse>(result.PayloadBytes!, JsonOptions)!;
        response.Setup!.Goals.ShouldBe(["goal"]);
        response.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Complete);
        response.Snapshot.ProjectVersion.ShouldBe(4);
        response.Snapshot.AsOf.ShouldBe(ObservedAt);
    }

    [Fact]
    public async Task ExecuteAsync_ArchivedProject_ReturnsSafeDenial()
    {
        GetConversationStartSetupQueryHandler handler = await CreateHandlerAsync(
            Detail(ProjectLifecycle.Archived, ProjectSetup.Empty, hasFolder: true)).ConfigureAwait(true);

        QueryResult result = await handler.ExecuteAsync(Query(), TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("safe-denial");
        result.PayloadBytes.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_MissingFolder_ReturnsUnavailableWithoutSetup()
    {
        GetConversationStartSetupQueryHandler handler = await CreateHandlerAsync(
            Detail(ProjectLifecycle.Active, ProjectSetup.Empty, hasFolder: false)).ConfigureAwait(true);

        QueryResult result = await handler.ExecuteAsync(Query(), TestContext.Current.CancellationToken);

        ConversationStartSetupResponse response = JsonSerializer.Deserialize<ConversationStartSetupResponse>(result.PayloadBytes!, JsonOptions)!;
        response.Setup.ShouldBeNull();
        response.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
        response.Snapshot.RecoveryActions.ShouldContain("RefreshContext");
    }

    [Fact]
    public async Task ExecuteAsync_UnknownTenant_ReturnsSafeDenialWithoutReadingProjection()
    {
        InMemoryReadModelStore store = new();
        await store.SaveAsync(
            ConversationStartSetupProjectionHandler.StoreName,
            ConversationStartSetupProjectionHandler.Key(TenantId, ProjectId),
            Detail(ProjectLifecycle.Active, ProjectSetup.Empty, hasFolder: true),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        TenantAccessAuthorizer tenantAccess = new(
            new InMemoryProjectTenantAccessProjectionStore(),
            new FixedUtcClock(ObservedAt.AddMinutes(1)),
            new TenantAccessOptions());
        var handler = new GetConversationStartSetupQueryHandler(store, tenantAccess);

        QueryResult result = await handler.ExecuteAsync(Query(), TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("safe-denial");
    }

    private static async Task<GetConversationStartSetupQueryHandler> CreateHandlerAsync(ProjectDetailItem detail)
    {
        InMemoryReadModelStore store = new();
        await store.SaveAsync(
            ConversationStartSetupProjectionHandler.StoreName,
            ConversationStartSetupProjectionHandler.Key(TenantId, ProjectId),
            detail,
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        TenantAccessAuthorizer tenantAccess = new(
            await SeedTenantAccessStoreAsync().ConfigureAwait(true),
            new FixedUtcClock(ObservedAt.AddMinutes(1)),
            new TenantAccessOptions());
        return new GetConversationStartSetupQueryHandler(store, tenantAccess);
    }

    private static async Task<IProjectTenantAccessProjectionStore> SeedTenantAccessStoreAsync()
    {
        InMemoryProjectTenantAccessProjectionStore store = new();
        ProjectTenantAccessProjection projection = new()
        {
            TenantId = TenantId,
            Enabled = true,
            Watermark = 1,
            ProjectionWatermark = $"{TenantId}:1",
            LastEventTimestamp = ObservedAt,
        };
        projection.Principals["actor-1"] = new ProjectTenantPrincipalEvidence("actor-1", "TenantOwner");
        await store.SaveAsync(projection, TestContext.Current.CancellationToken).ConfigureAwait(true);
        return store;
    }

    private static QueryEnvelope Query()
        => new(TenantId, ProjectsServerModule.DomainName, ProjectId, ProjectsServerModule.GetConversationStartSetupQueryType, [], "corr-1", "actor-1");

    private static ProjectDetailItem Detail(ProjectLifecycle lifecycle, ProjectSetup setup, bool hasFolder)
        => new(
            TenantId,
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
}
