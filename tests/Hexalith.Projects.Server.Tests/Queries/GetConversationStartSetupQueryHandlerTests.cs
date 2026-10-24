// <copyright file="GetConversationStartSetupQueryHandlerTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Queries;

using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.EventStore.Client.Projections;
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
        response.Snapshot.ResponseState.ShouldBe(ConversationStartResponseState.Complete);
        response.Snapshot.ProjectVersion.ShouldBe(4);
        response.Snapshot.AsOf.ShouldBe(ObservedAt);
        response.Snapshot.Components.Select(static component => component.Name).ShouldBe(
            ["Project", "Folder", "Setup", "FirstResponseAuthorization"]);
        response.Snapshot.RecoveryActions.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_OptionalContextOmission_PreservesCompleteStory62Snapshot()
    {
        ProjectSetup setup = new(
            [],
            [],
            [],
            [ProjectContextSourceKind.Memory],
            null);
        ProjectDetailItem detail = Detail(ProjectLifecycle.Active, setup, hasFolder: true) with
        {
            MemoryReferences =
            [
                new ProjectMemoryReference("memory-1", "Memory", ReferenceState.Included, null, ObservedAt),
            ],
        };
        GetConversationStartSetupQueryHandler handler = await CreateHandlerAsync(detail).ConfigureAwait(true);

        ConversationStartSetupResponse response = JsonSerializer.Deserialize<ConversationStartSetupResponse>(
            (await handler.ExecuteAsync(Query(), TestContext.Current.CancellationToken)).PayloadBytes!,
            JsonOptions)!;

        response.Setup.ShouldNotBeNull();
        response.Snapshot.ResponseState.ShouldBe(ConversationStartResponseState.Complete);
        response.Snapshot.Components.Select(static component => component.Name).ShouldBe(
            ["Project", "Folder", "Setup", "FirstResponseAuthorization"]);
        response.Snapshot.RecoveryActions.ShouldBeEmpty();
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
        response.Snapshot.ResponseState.ShouldBe(ConversationStartResponseState.Unavailable);
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
        var handler = new GetConversationStartSetupQueryHandler(
            ProjectContextQueryTestFactory.Create(store, tenantAccess));

        QueryResult result = await handler.ExecuteAsync(Query(), TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("safe-denial");
    }

    [Fact]
    public async Task ExecuteAsync_MismatchedCallbackPrincipal_ReturnsSafeDenial()
    {
        GetConversationStartSetupQueryHandler handler = await CreateHandlerAsync(
            Detail(ProjectLifecycle.Active, ProjectSetup.Empty, hasFolder: true)).ConfigureAwait(true);

        QueryResult result = await handler.ExecuteAsync(
            Query() with { OriginalActorId = "other-actor" },
            TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("safe-denial");
        result.PayloadBytes.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ContradictoryAggregateAndEntityTargets_ReturnsSafeDenial()
    {
        GetConversationStartSetupQueryHandler handler = await CreateHandlerAsync(
            Detail(ProjectLifecycle.Active, ProjectSetup.Empty, hasFolder: true)).ConfigureAwait(true);

        QueryResult result = await handler.ExecuteAsync(
            Query() with { EntityId = "other-project" },
            TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("safe-denial");
        result.PayloadBytes.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_MismatchedPersistedIdentity_ReturnsSafeDenial()
    {
        GetConversationStartSetupQueryHandler handler = await CreateHandlerAsync(
            Detail(ProjectLifecycle.Active, ProjectSetup.Empty, hasFolder: true) with { ProjectId = "other-project" }).ConfigureAwait(true);

        QueryResult result = await handler.ExecuteAsync(Query(), TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("safe-denial");
        result.PayloadBytes.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_InvalidPersistedDetail_ReturnsMinimalUnavailable()
    {
        GetConversationStartSetupQueryHandler handler = await CreateHandlerAsync(
            Detail(ProjectLifecycle.Active, ProjectSetup.Empty, hasFolder: true) with { Sequence = 0 }).ConfigureAwait(true);

        QueryResult result = await handler.ExecuteAsync(Query(), TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue();
        ConversationStartSetupResponse response = JsonSerializer.Deserialize<ConversationStartSetupResponse>(result.PayloadBytes!, JsonOptions)!;
        response.Setup.ShouldBeNull();
        response.Snapshot.ResponseState.ShouldBe(ConversationStartResponseState.Unavailable);
        response.Snapshot.ProjectVersion.ShouldBe(0);
    }

    [Theory]
    [InlineData("actor")]
    [InlineData("user")]
    [InlineData("tenant")]
    [InlineData("workload")]
    [InlineData("delegated")]
    [InlineData("delegation")]
    [InlineData("scope")]
    [InlineData("audience")]
    public async Task ExecuteAsync_ChangedImmutableAuthorityReturnsSafeDenial(string field)
    {
        GetConversationStartSetupQueryHandler handler = await CreateHandlerAsync(
            Detail(ProjectLifecycle.Active, ProjectSetup.Empty, hasFolder: true)).ConfigureAwait(true);
        QueryEnvelope query = field switch
        {
            "actor" => Query() with { OriginalActorId = "foreign" },
            "user" => Query() with { UserId = "foreign" },
            "tenant" => Query() with { TenantId = "foreign" },
            "workload" => Query() with { AuthenticatedWorkloadId = "foreign" },
            "delegated" => Query() with { IsDelegated = false },
            "delegation" => Query() with { DelegationId = "foreign" },
            "scope" => Query() with { Scopes = ["projects.list"] },
            _ => Query() with { Audience = ["foreign"] },
        };

        QueryResult denied = await handler.ExecuteAsync(query, TestContext.Current.CancellationToken);
        QueryResult allowed = await handler.ExecuteAsync(Query(), TestContext.Current.CancellationToken);

        allowed.Success.ShouldBeTrue();
        denied.Success.ShouldBeFalse();
        denied.ErrorMessage.ShouldBe("safe-denial");
        denied.PayloadBytes.ShouldBeNull();
    }

    [Theory]
    [InlineData(ReferenceState.Pending, "PollTask")]
    [InlineData(ReferenceState.Stale, "RefreshContext")]
    [InlineData(ReferenceState.Unavailable, "RefreshContext")]
    [InlineData(ReferenceState.Archived, "SelectAlternative")]
    [InlineData(ReferenceState.Ambiguous, "SelectAlternative")]
    [InlineData(ReferenceState.Excluded, "ContactAdministrator")]
    [InlineData(ReferenceState.Unauthorized, "ContactAdministrator")]
    [InlineData(ReferenceState.TenantMismatch, "ContactAdministrator")]
    [InlineData(ReferenceState.Conflict, "ContactAdministrator")]
    [InlineData(ReferenceState.InvalidReference, "ContactAdministrator")]
    public async Task ExecuteAsync_FolderStateHasSpecificSafeRecovery(ReferenceState state, string recovery)
    {
        ProjectDetailItem detail = Detail(ProjectLifecycle.Active, ProjectSetup.Empty, hasFolder: true);
        detail = detail with
        {
            ProjectFolder = detail.ProjectFolder! with
            {
                ReferenceState = state,
                DisplayName = "Protected Folder Canary",
                ReasonCode = "protected_folder_reason_canary",
            },
        };
        GetConversationStartSetupQueryHandler handler = await CreateHandlerAsync(detail).ConfigureAwait(true);

        QueryResult result = await handler.ExecuteAsync(Query(), TestContext.Current.CancellationToken);
        result.Success.ShouldBeTrue();
        ConversationStartSetupResponse response = JsonSerializer.Deserialize<ConversationStartSetupResponse>(
            result.PayloadBytes!, JsonOptions)!;

        response.Setup.ShouldBeNull();
        response.Snapshot.ResponseState.ShouldBe(ConversationStartResponseState.Unavailable);
        response.Snapshot.RecoveryActions.ShouldBe([recovery]);
        response.Snapshot.Components.Select(static component => component.Name).ShouldBe(
            ["Project", "Folder", "Setup", "FirstResponseAuthorization"]);
        response.Snapshot.Components.Single(static component => component.Name == "Folder").ShouldBe(
            new ConversationStartComponent("Folder", false, EvidenceFreshnessState.Unavailable, "missing"));
        string body = System.Text.Encoding.UTF8.GetString(result.PayloadBytes!);
        body.ShouldNotContain("folder-1");
        body.ShouldNotContain("Protected Folder Canary");
        body.ShouldNotContain("protected_folder_reason_canary");
    }

    [Theory]
    [InlineData("complete")]
    [InlineData("unavailable")]
    [InlineData("archived")]
    [InlineData("foreign-tenant")]
    [InlineData("wrong-workload")]
    public async Task ExecuteAsync_PreservesPersistedBytesETagsAndKeyCount(string outcome)
    {
        InMemoryReadModelStore store = new();
        InMemoryProjectTenantAccessProjectionStore tenants = await SeedTenantAccessStoreAsync().ConfigureAwait(true);
        ProjectDetailItem detail = Detail(
            outcome == "archived" ? ProjectLifecycle.Archived : ProjectLifecycle.Active,
            ProjectSetup.Empty,
            hasFolder: outcome != "unavailable");
        GetConversationStartSetupQueryHandler handler = await CreateHandlerAsync(detail, store, tenants: tenants).ConfigureAwait(true);
        const string StoreName = ConversationStartSetupProjectionHandler.StoreName;
        string key = ConversationStartSetupProjectionHandler.Key(TenantId, ProjectId);
        string sentinelKey = ConversationStartSetupProjectionHandler.Key("foreign-tenant", ProjectId);
        await store.SaveAsync(StoreName, sentinelKey, detail with { TenantId = "foreign-tenant" },
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        byte[] before = store.SnapshotRaw(StoreName, key)!;
        byte[] sentinelBefore = store.SnapshotRaw(StoreName, sentinelKey)!;
        string? etag = (await store.GetAsync<ProjectDetailItem>(StoreName, key,
            TestContext.Current.CancellationToken).ConfigureAwait(true)).ETag;
        string? sentinelETag = (await store.GetAsync<ProjectDetailItem>(StoreName, sentinelKey,
            TestContext.Current.CancellationToken).ConfigureAwait(true)).ETag;
        int count = store.Count;
        string tenantBefore = JsonSerializer.Serialize(
            await tenants.GetAsync(TenantId, TestContext.Current.CancellationToken).ConfigureAwait(true), JsonOptions);
        QueryEnvelope query = outcome switch
        {
            "foreign-tenant" => Query() with { TenantId = "foreign-tenant" },
            "wrong-workload" => Query() with { AuthenticatedWorkloadId = "foreign-workload" },
            _ => Query(),
        };

        for (int attempt = 0; attempt < 2; attempt++)
        {
            QueryResult result = await handler.ExecuteAsync(query, TestContext.Current.CancellationToken);
            if (outcome is "complete" or "unavailable")
            {
                result.Success.ShouldBeTrue();
                ConversationStartSetupResponse response = JsonSerializer.Deserialize<ConversationStartSetupResponse>(
                    result.PayloadBytes!, JsonOptions)!;
                response.Snapshot.ResponseState.ShouldBe(outcome == "complete"
                    ? ConversationStartResponseState.Complete : ConversationStartResponseState.Unavailable);
            }
            else
            {
                result.Success.ShouldBeFalse();
                result.ErrorMessage.ShouldBe("safe-denial");
                result.PayloadBytes.ShouldBeNull();
            }

            store.Count.ShouldBe(count);
            JsonSerializer.Serialize(await tenants.GetAsync(TenantId,
                TestContext.Current.CancellationToken).ConfigureAwait(true), JsonOptions).ShouldBe(tenantBefore);
            store.SnapshotRaw(StoreName, key).ShouldBe(before);
            store.SnapshotRaw(StoreName, sentinelKey).ShouldBe(sentinelBefore);
            (await store.GetAsync<ProjectDetailItem>(StoreName, key,
                TestContext.Current.CancellationToken).ConfigureAwait(true)).ETag.ShouldBe(etag);
            (await store.GetAsync<ProjectDetailItem>(StoreName, sentinelKey,
                TestContext.Current.CancellationToken).ConfigureAwait(true)).ETag.ShouldBe(sentinelETag);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_MissingPrimaryReadPermissionCannotBeFilledBySupplementaryIdentity(bool supplementary)
    {
        InMemoryReadModelStore store = new();
        AdvisoryGuardedReadStore guarded = new(store);
        GetConversationStartSetupQueryHandler handler = await CreateHandlerAsync(
            Detail(ProjectLifecycle.Active, ProjectSetup.Empty, hasFolder: true), store,
            readModelStore: guarded,
            configurePrincipal: principal =>
            {
                ClaimsIdentity primary = (ClaimsIdentity)principal.Identity!;
                foreach (Claim claim in primary.FindAll("eventstore:permission").ToArray()) primary.RemoveClaim(claim);
                if (supplementary)
                    principal.AddIdentity(new ClaimsIdentity([new Claim("eventstore:permission", "projects:read")]));
            }).ConfigureAwait(true);
        string key = ConversationStartSetupProjectionHandler.Key(TenantId, ProjectId);
        byte[] before = store.SnapshotRaw(ConversationStartSetupProjectionHandler.StoreName, key)!;

        QueryResult result = await handler.ExecuteAsync(Query(), TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("safe-denial");
        result.PayloadBytes.ShouldBeNull();
        guarded.Reads.ShouldBeEmpty();
        store.SnapshotRaw(ConversationStartSetupProjectionHandler.StoreName, key).ShouldBe(before);
    }

    [Fact]
    public async Task ExecuteAsync_FinalAuthorityRevocationDeniesWithoutPersisting()
    {
        int calls = 0;
        ProjectReadTestAuthorization validator = new()
        {
            Decision = request =>
            {
                request.ActionToken.ShouldBe("projects:read");
                request.ProjectId.ShouldBe(ProjectId);
                request.TenantId.ShouldBe(TenantId);
                request.PrincipalId.ShouldBe("actor-1");
                return ++calls == 1
                    ? EventStoreAuthorizationValidationResult.Allowed("stable")
                    : EventStoreAuthorizationValidationResult.Denied();
            },
        };
        InMemoryReadModelStore store = new();
        GetConversationStartSetupQueryHandler handler = await CreateHandlerAsync(
            Detail(ProjectLifecycle.Active, ProjectSetup.Empty, hasFolder: true), store,
            validator: validator).ConfigureAwait(true);
        const string StoreName = ConversationStartSetupProjectionHandler.StoreName;
        string key = ConversationStartSetupProjectionHandler.Key(TenantId, ProjectId);
        byte[] before = store.SnapshotRaw(StoreName, key)!;
        string? etag = (await store.GetAsync<ProjectDetailItem>(StoreName, key,
            TestContext.Current.CancellationToken).ConfigureAwait(true)).ETag;
        int count = store.Count;

        QueryResult result = await handler.ExecuteAsync(Query(), TestContext.Current.CancellationToken);

        calls.ShouldBe(2);
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("safe-denial");
        result.PayloadBytes.ShouldBeNull();
        store.Count.ShouldBe(count);
        store.SnapshotRaw(StoreName, key).ShouldBe(before);
        (await store.GetAsync<ProjectDetailItem>(StoreName, key,
            TestContext.Current.CancellationToken).ConfigureAwait(true)).ETag.ShouldBe(etag);
    }

    private static async Task<GetConversationStartSetupQueryHandler> CreateHandlerAsync(
        ProjectDetailItem detail,
        InMemoryReadModelStore? store = null,
        InMemoryProjectTenantAccessProjectionStore? tenants = null,
        IProjectEventStoreAuthorizationValidator? validator = null,
        Action<ClaimsPrincipal>? configurePrincipal = null,
        IReadModelStore? readModelStore = null)
    {
        store ??= new();
        await store.SaveAsync(
            ConversationStartSetupProjectionHandler.StoreName,
            ConversationStartSetupProjectionHandler.Key(TenantId, ProjectId),
            detail,
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        TenantAccessAuthorizer tenantAccess = new(
            tenants ?? await SeedTenantAccessStoreAsync().ConfigureAwait(true),
            new FixedUtcClock(ObservedAt.AddMinutes(1)),
            new TenantAccessOptions());
        return new GetConversationStartSetupQueryHandler(
            ProjectContextQueryTestFactory.Create(readModelStore ?? store, tenantAccess,
                eventStoreAuthorizationValidator: validator, configurePrincipal: configurePrincipal));
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

    private static QueryEnvelope Query()
        => new(TenantId, ProjectsServerModule.DomainName, ProjectId, ProjectsServerModule.GetConversationStartSetupQueryType, [], "corr-1", "actor-1")
        {
            OriginalActorId = "actor-1",
            AuthenticatedWorkloadId = "projects-callback",
            IsDelegated = true,
            DelegationId = "delegation-1",
            Scopes = ["projects.read", "projects.list"],
            Audience = ["hexalith-projects", "hexalith-eventstore"],
        };

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
