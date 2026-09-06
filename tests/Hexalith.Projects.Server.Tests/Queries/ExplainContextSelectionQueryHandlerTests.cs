// <copyright file="ExplainContextSelectionQueryHandlerTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Queries;

using System;
using System.Text.Json;
using System.Threading.Tasks;

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
        response.Evaluations.ShouldContain(item => item.ReferenceKind == "folder" && item.FailedCheck is null);
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
}
