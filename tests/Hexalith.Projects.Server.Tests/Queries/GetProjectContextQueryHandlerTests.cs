// <copyright file="GetProjectContextQueryHandlerTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Queries;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Context;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Server;
using Hexalith.Projects.Server.Projections.ConversationStartSetup;
using Hexalith.Projects.Server.Queries;
using Hexalith.Projects.Testing.Leakage;

using Shouldly;

using Xunit;

/// <summary>Tests the supported Get Project Context DomainService query handler.</summary>
public sealed class GetProjectContextQueryHandlerTests
{
    private const string TenantId = "tenant-a";
    private const string ProjectId = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";
    private static readonly DateTimeOffset ObservedAt = new(2026, 9, 6, 8, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ExecuteAsync_ActiveProjectWithFolder_ReturnsCompleteCurrentEmptySetup()
    {
        GetProjectContextQueryHandler handler = await CreateHandlerAsync(Detail(hasFolder: true)).ConfigureAwait(true);

        QueryResult result = await handler.ExecuteAsync(Query(), TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue();
        ProjectContextReadResponse response = Deserialize(result);
        response.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Complete);
        response.Setup.ShouldBe(ProjectSetup.Empty);
        response.ProjectFolder.ShouldNotBeNull();
        response.Snapshot.AsOf.ShouldBe(ObservedAt);
        response.Snapshot.ProjectVersion.ShouldBe(4);
        response.Snapshot.RecoveryActions.ShouldBe([AdmissionRecoveryAction.None]);
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(response));
    }

    [Fact]
    public async Task ExecuteAsync_UnauthorizedFile_ReturnsPartial()
    {
        ProjectDetailItem detail = Detail(hasFolder: true) with
        {
            FileReferences =
            [
                new ProjectFileReference("file-1", "folder-1", "contract", ReferenceState.Unauthorized, null, ObservedAt),
            ],
        };
        GetProjectContextQueryHandler handler = await CreateHandlerAsync(detail).ConfigureAwait(true);

        ProjectContextReadResponse response = Deserialize(await handler.ExecuteAsync(Query(), TestContext.Current.CancellationToken));

        response.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Partial);
        response.FileReferences.ShouldBeEmpty();
        response.Excluded.ShouldContain(item => item.ReferenceId == "file-1" && item.ReferenceState == ReferenceState.Unauthorized);
    }

    [Fact]
    public async Task ExecuteAsync_MissingFolder_ReturnsUnavailable()
    {
        GetProjectContextQueryHandler handler = await CreateHandlerAsync(Detail(hasFolder: false)).ConfigureAwait(true);

        ProjectContextReadResponse response = Deserialize(await handler.ExecuteAsync(Query(), TestContext.Current.CancellationToken));

        response.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
        response.Setup.ShouldBeNull();
        response.ProjectFolder.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ArchivedProject_ReturnsSafeDenial()
    {
        GetProjectContextQueryHandler handler = await CreateHandlerAsync(
            Detail(hasFolder: true) with { Lifecycle = ProjectLifecycle.Archived }).ConfigureAwait(true);

        QueryResult result = await handler.ExecuteAsync(Query(), TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("safe-denial");
        result.PayloadBytes.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_UnknownTenant_ReturnsSafeDenialWithoutReadingProjection()
    {
        InMemoryReadModelStore store = new();
        await store.SaveAsync(
            ConversationStartSetupProjectionHandler.StoreName,
            ConversationStartSetupProjectionHandler.Key(TenantId, ProjectId),
            Detail(hasFolder: true),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        var handler = new GetProjectContextQueryHandler(new ProjectContextQueryExecutor(
            store,
            new TenantAccessAuthorizer(
                new InMemoryProjectTenantAccessProjectionStore(),
                new FixedUtcClock(ObservedAt.AddMinutes(1)),
                new TenantAccessOptions()),
            new ProjectContextInclusionPolicy()));

        QueryResult result = await handler.ExecuteAsync(Query(), TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("safe-denial");
    }

    [Fact]
    public async Task ExecuteAsync_TargetMismatch_ReturnsSafeDenial()
    {
        GetProjectContextQueryHandler handler = await CreateHandlerAsync(Detail(hasFolder: true)).ConfigureAwait(true);

        QueryResult result = await handler.ExecuteAsync(
            Query(projectId: "other-project"),
            TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("safe-denial");
    }

    [Fact]
    public async Task ExecuteAsync_StaleTenant_ReturnsUnavailable()
    {
        InMemoryReadModelStore store = new();
        await store.SaveAsync(
            ConversationStartSetupProjectionHandler.StoreName,
            ConversationStartSetupProjectionHandler.Key(TenantId, ProjectId),
            Detail(hasFolder: true),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        InMemoryProjectTenantAccessProjectionStore tenantStore = await SeedTenantAccessStoreAsync().ConfigureAwait(true);
        ProjectTenantAccessProjection projection = await tenantStore.GetAsync(TenantId, TestContext.Current.CancellationToken).ConfigureAwait(true)
            ?? throw new InvalidOperationException("tenant access");
        projection.LastEventTimestamp = ObservedAt;
        await tenantStore.SaveAsync(projection, TestContext.Current.CancellationToken).ConfigureAwait(true);
        var handler = new GetProjectContextQueryHandler(new ProjectContextQueryExecutor(
            store,
            new TenantAccessAuthorizer(tenantStore, new FixedUtcClock(ObservedAt.AddMinutes(10)), new TenantAccessOptions()),
            new ProjectContextInclusionPolicy()));

        ProjectContextReadResponse response = Deserialize(await handler.ExecuteAsync(Query(), TestContext.Current.CancellationToken));

        response.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
    }

    [Fact]
    public async Task ExecuteAsync_ZeroWrite_DoesNotSaveReadModel()
    {
        InMemoryReadModelStore inner = new();
        await inner.SaveAsync(
            ConversationStartSetupProjectionHandler.StoreName,
            ConversationStartSetupProjectionHandler.Key(TenantId, ProjectId),
            Detail(hasFolder: true),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        CountingReadModelStore store = new(inner);
        var handler = new GetProjectContextQueryHandler(new ProjectContextQueryExecutor(
            store,
            new TenantAccessAuthorizer(
                await SeedTenantAccessStoreAsync().ConfigureAwait(true),
                new FixedUtcClock(ObservedAt.AddMinutes(1)),
                new TenantAccessOptions()),
            new ProjectContextInclusionPolicy()));

        _ = await handler.ExecuteAsync(Query(), TestContext.Current.CancellationToken);

        store.Saves.ShouldBe(0);
        store.TrySaves.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_StoreFaultAfterAuthority_ReturnsUnavailable()
    {
        var handler = new GetProjectContextQueryHandler(new ProjectContextQueryExecutor(
            new ThrowingReadModelStore(),
            new TenantAccessAuthorizer(
                await SeedTenantAccessStoreAsync().ConfigureAwait(true),
                new FixedUtcClock(ObservedAt.AddMinutes(1)),
                new TenantAccessOptions()),
            new ProjectContextInclusionPolicy()));

        ProjectContextReadResponse response = Deserialize(await handler.ExecuteAsync(Query(), TestContext.Current.CancellationToken));

        response.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
        response.Setup.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_Cancellation_Propagates()
    {
        GetProjectContextQueryHandler handler = await CreateHandlerAsync(Detail(hasFolder: true)).ConfigureAwait(true);
        using CancellationTokenSource cts = new();
        await cts.CancelAsync().ConfigureAwait(true);

        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.ExecuteAsync(Query(), cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_MismatchedScopes_ReturnsSafeDenial()
    {
        GetProjectContextQueryHandler handler = await CreateHandlerAsync(Detail(hasFolder: true)).ConfigureAwait(true);
        QueryEnvelope query = Query() with { Scopes = ["other.scope"] };

        QueryResult result = await handler.ExecuteAsync(query, TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("safe-denial");
    }

    private static async Task<GetProjectContextQueryHandler> CreateHandlerAsync(ProjectDetailItem detail)
    {
        InMemoryReadModelStore store = new();
        await store.SaveAsync(
            ConversationStartSetupProjectionHandler.StoreName,
            ConversationStartSetupProjectionHandler.Key(TenantId, ProjectId),
            detail,
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        return new GetProjectContextQueryHandler(new ProjectContextQueryExecutor(
            store,
            new TenantAccessAuthorizer(
                await SeedTenantAccessStoreAsync().ConfigureAwait(true),
                new FixedUtcClock(ObservedAt.AddMinutes(1)),
                new TenantAccessOptions()),
            new ProjectContextInclusionPolicy()));
    }

    private static async Task<InMemoryProjectTenantAccessProjectionStore> SeedTenantAccessStoreAsync()
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

    private static QueryEnvelope Query(string projectId = ProjectId)
        => new(
            TenantId,
            ProjectsServerModule.DomainName,
            ProjectId,
            ProjectsServerModule.GetProjectContextQueryType,
            JsonSerializer.SerializeToUtf8Bytes(new GetProjectContextQuery(projectId), JsonOptions),
            "corr-1",
            "actor-1");

    private static ProjectDetailItem Detail(bool hasFolder)
        => new(
            TenantId,
            ProjectId,
            "Project",
            null,
            null,
            Setup: null,
            hasFolder ? new ProjectFolderReference("folder-1", "Folder", ReferenceState.Included, null, ObservedAt) : null,
            [],
            [],
            ProjectLifecycle.Active,
            ObservedAt,
            ObservedAt,
            4);

    private static ProjectContextReadResponse Deserialize(QueryResult result)
    {
        result.Success.ShouldBeTrue();
        return JsonSerializer.Deserialize<ProjectContextReadResponse>(result.PayloadBytes!, JsonOptions)!;
    }

    private sealed class CountingReadModelStore(IReadModelStore inner) : IReadModelStore
    {
        public int Saves { get; private set; }

        public int TrySaves { get; private set; }

        public Task<ReadModelEntry<TValue>> GetAsync<TValue>(string storeName, string key, CancellationToken cancellationToken = default)
            where TValue : class
            => inner.GetAsync<TValue>(storeName, key, cancellationToken);

        public Task SaveAsync<TValue>(string storeName, string key, TValue value, CancellationToken cancellationToken = default)
            where TValue : class
        {
            Saves++;
            return inner.SaveAsync(storeName, key, value, cancellationToken);
        }

        public Task<bool> TrySaveAsync<TValue>(string storeName, string key, TValue value, string etag, CancellationToken cancellationToken = default)
            where TValue : class
        {
            TrySaves++;
            return inner.TrySaveAsync(storeName, key, value, etag, cancellationToken);
        }
    }

    private sealed class ThrowingReadModelStore : IReadModelStore
    {
        public Task<ReadModelEntry<TValue>> GetAsync<TValue>(string storeName, string key, CancellationToken cancellationToken = default)
            where TValue : class
            => Task.FromException<ReadModelEntry<TValue>>(new InvalidOperationException("store-fault"));

        public Task SaveAsync<TValue>(string storeName, string key, TValue value, CancellationToken cancellationToken = default)
            where TValue : class
            => Task.CompletedTask;

        public Task<bool> TrySaveAsync<TValue>(string storeName, string key, TValue value, string etag, CancellationToken cancellationToken = default)
            where TValue : class
            => Task.FromResult(false);
    }
}
