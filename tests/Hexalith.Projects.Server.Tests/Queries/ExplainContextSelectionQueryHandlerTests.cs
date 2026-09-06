// <copyright file="ExplainContextSelectionQueryHandlerTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Queries;

using System;
using System.Text.Json;
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

/// <summary>Tests the supported Explain Context Selection DomainService query handler.</summary>
public sealed class ExplainContextSelectionQueryHandlerTests
{
    private const string TenantId = "tenant-a";
    private const string ProjectId = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";
    private static readonly DateTimeOffset ObservedAt = new(2026, 9, 6, 8, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ExecuteAsync_CompleteContext_ReturnsDeterministicFolderEvaluation()
    {
        ExplainContextSelectionQueryHandler handler = await CreateHandlerAsync(Detail(hasFolder: true)).ConfigureAwait(true);

        QueryResult result = await handler.ExecuteAsync(Query(), TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue();
        ExplainContextSelectionResponse response = JsonSerializer.Deserialize<ExplainContextSelectionResponse>(result.PayloadBytes!, JsonOptions)!;
        response.Context.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Complete);
        response.Evaluations.ShouldContain(item => item.ReferenceKind == "folder" && item.FailedCheck == null);
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(response));
    }

    [Fact]
    public async Task ExecuteAsync_DeniedTarget_ReturnsSafeDenial()
    {
        ExplainContextSelectionQueryHandler handler = await CreateHandlerAsync(Detail(hasFolder: true)).ConfigureAwait(true);

        QueryResult result = await handler.ExecuteAsync(
            new QueryEnvelope(
                "other-tenant",
                ProjectsServerModule.DomainName,
                ProjectId,
                ProjectsServerModule.ExplainContextSelectionQueryType,
                JsonSerializer.SerializeToUtf8Bytes(new ExplainContextSelectionQuery(ProjectId), JsonOptions),
                "corr-1",
                "actor-1"),
            TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("safe-denial");
    }

    [Fact]
    public async Task ExecuteAsync_MalformedPayload_ReturnsSafeDenial()
    {
        ExplainContextSelectionQueryHandler handler = await CreateHandlerAsync(Detail(hasFolder: true)).ConfigureAwait(true);
        QueryEnvelope query = Query() with { Payload = "{not-json"u8.ToArray() };

        QueryResult result = await handler.ExecuteAsync(query, TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("safe-denial");
    }

    [Fact]
    public async Task ExecuteAsync_ProductionDualPrincipalClaims_ReturnsComplete()
    {
        ExplainContextSelectionQueryHandler handler = await CreateHandlerAsync(Detail(hasFolder: true)).ConfigureAwait(true);
        QueryEnvelope query = Query() with
        {
            Scopes = ["projects.read", "projects.list"],
            Audience = ["hexalith-projects", "hexalith-eventstore"],
        };

        QueryResult result = await handler.ExecuteAsync(query, TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue();
        ExplainContextSelectionResponse response = JsonSerializer.Deserialize<ExplainContextSelectionResponse>(result.PayloadBytes!, JsonOptions)!;
        response.Context.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Complete);
    }

    [Fact]
    public async Task ExecuteAsync_MismatchedAudience_ReturnsSafeDenial()
    {
        ExplainContextSelectionQueryHandler handler = await CreateHandlerAsync(Detail(hasFolder: true)).ConfigureAwait(true);
        QueryEnvelope query = Query() with { Audience = ["other-audience"] };

        QueryResult result = await handler.ExecuteAsync(query, TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("safe-denial");
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
        InMemoryProjectTenantAccessProjectionStore tenantStore = new();
        ProjectTenantAccessProjection projection = new()
        {
            TenantId = TenantId,
            Enabled = true,
            Watermark = 1,
            ProjectionWatermark = $"{TenantId}:1",
            LastEventTimestamp = ObservedAt,
        };
        projection.Principals["actor-1"] = new ProjectTenantPrincipalEvidence("actor-1", "TenantOwner");
        await tenantStore.SaveAsync(projection, TestContext.Current.CancellationToken).ConfigureAwait(true);
        var handler = new ExplainContextSelectionQueryHandler(new ProjectContextQueryExecutor(
            store,
            new TenantAccessAuthorizer(tenantStore, new FixedUtcClock(ObservedAt.AddMinutes(1)), new TenantAccessOptions()),
            new ProjectContextInclusionPolicy()));

        _ = await handler.ExecuteAsync(Query(), TestContext.Current.CancellationToken);

        store.Saves.ShouldBe(0);
        store.TrySaves.ShouldBe(0);
    }

    private static async Task<ExplainContextSelectionQueryHandler> CreateHandlerAsync(ProjectDetailItem detail)
    {
        InMemoryReadModelStore store = new();
        await store.SaveAsync(
            ConversationStartSetupProjectionHandler.StoreName,
            ConversationStartSetupProjectionHandler.Key(TenantId, ProjectId),
            detail,
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        InMemoryProjectTenantAccessProjectionStore tenantStore = new();
        ProjectTenantAccessProjection projection = new()
        {
            TenantId = TenantId,
            Enabled = true,
            Watermark = 1,
            ProjectionWatermark = $"{TenantId}:1",
            LastEventTimestamp = ObservedAt,
        };
        projection.Principals["actor-1"] = new ProjectTenantPrincipalEvidence("actor-1", "TenantOwner");
        await tenantStore.SaveAsync(projection, TestContext.Current.CancellationToken).ConfigureAwait(true);
        return new ExplainContextSelectionQueryHandler(new ProjectContextQueryExecutor(
            store,
            new TenantAccessAuthorizer(tenantStore, new FixedUtcClock(ObservedAt.AddMinutes(1)), new TenantAccessOptions()),
            new ProjectContextInclusionPolicy()));
    }

    private static QueryEnvelope Query()
        => new(
            TenantId,
            ProjectsServerModule.DomainName,
            ProjectId,
            ProjectsServerModule.ExplainContextSelectionQueryType,
            JsonSerializer.SerializeToUtf8Bytes(new ExplainContextSelectionQuery(ProjectId), JsonOptions),
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
}
