using System.Text.Json;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.Conversations.Server.Tests.ResolutionMetadata;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.Server.Tests.Queries;

public sealed partial class ConversationAdvisoryMetadataQueryHandlerTest
{
    [Fact]
    public async Task CommittedMetadataWithoutNativeProofUsesTwoReadChecksAndLeavesBytesUnchanged()
    {
        var f = new ResolutionMetadataFixture();
        await f.ProjectAsync();
        string key = ConversationProjectionReadModelKeys.ResolutionMetadataKey(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation);
        f.Store.SeedRaw("statestore", key, f.Metadata with { NativeAdmission = null, NativeSourceProof = null });
        byte[] before = f.Store.SnapshotRaw("statestore", key)!;
        int count = f.Store.Count;
        var result = Payload(await f.DispatchAdvisoryAsync());
        result.Metadata!.ProjectId.ShouldBe("project-original");
        result.Metadata.SafeLabel.ShouldBe("Scoring label"); result.Incomplete.ShouldBeFalse();
        f.Access.Checks.ShouldBe(2);
        f.Store.SnapshotRaw("statestore", key).ShouldBe(before); f.Store.Count.ShouldBe(count);
        f.Guard.ForbiddenAttempts.ShouldBe(0);
    }

    [Fact]
    public async Task UnclassifiedLabelIsOmittedWithoutLosingAuthorizedLinkHint()
    {
        var f = new ResolutionMetadataFixture(); await f.ProjectAsync();
        string key = ConversationProjectionReadModelKeys.ResolutionMetadataKey(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation);
        f.Store.SeedRaw("statestore", key, f.Metadata with { LabelClassification = null, LabelPurpose = null });
        var result = Payload(await f.DispatchAdvisoryAsync());
        result.Incomplete.ShouldBeTrue(); result.Metadata!.SafeLabel.ShouldBeNull(); result.Metadata.ProjectId.ShouldBe("project-original");
    }

    [Fact]
    public async Task DeniedMissingAndForeignInputsShareSafeDenialAndInitialDenialAvoidsReads()
    {
        var missing = new ResolutionMetadataFixture();
        var absent = await missing.DispatchAdvisoryAsync();
        var denied = new ResolutionMetadataFixture(); await denied.ProjectAsync(); denied.Access.Denied = true;
        var rejected = await denied.DispatchAdvisoryAsync();
        rejected.ErrorMessage.ShouldBe(absent.ErrorMessage); rejected.Success.ShouldBeFalse(); denied.Guard.Reads.ShouldBeEmpty();
        denied.Access.Denied = false; denied.Access.AllowedTenant = ResolutionMetadataFixture.Tenant.Value;
        var foreign = await denied.DispatchAdvisoryAsync(ResolutionMetadataFixture.Query() with
            { QueryType = GetConversationAdvisoryMetadataQuery.QueryType, TenantId = "foreign" });
        foreign.ErrorMessage.ShouldBe(absent.ErrorMessage);
        denied.Guard.ReadKeys.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("missing-etag")]
    [InlineData("dispatch")]
    [InlineData("lifecycle")]
    [InlineData("tenant")]
    [InlineData("resource")]
    [InlineData("pending")]
    [InlineData("schema")]
    [InlineData("Rebuilding")]
    [InlineData("Unavailable")]
    [InlineData("Forbidden")]
    public async Task MalformedOrPendingMetadataWithholdsObservations(string mutation)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        var f = new ResolutionMetadataFixture(); await f.ProjectAsync();
        string key = ConversationProjectionReadModelKeys.ResolutionMetadataKey(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation);
        var value = f.Metadata;
        if (mutation == "missing-etag") f.Guard.EmptyMetadataETag = true;
        if (mutation == "dispatch") value = value with { DispatchId = " dispatch " };
        if (mutation == "lifecycle") value = value with { Metadata = value.Metadata with { LifecycleState = "Purged" } };
        if (mutation == "tenant") value = value with { TenantId = new TenantId("foreign") };
        if (mutation == "resource") value = value with { Metadata = value.Metadata with { ConversationId = "other" } };
        if (mutation == "schema") value = value with { Freshness = new(new(2), value.Freshness.ProjectionCursor, value.Freshness.LastAppliedEventPosition, value.Freshness.LastAppliedEventTimestamp, value.Freshness.ProjectionGeneratedAt, value.Freshness.LagDuration, value.Freshness.IsStale, value.Freshness.FreshnessState, value.Freshness.ReasonCode) };
        if (mutation is "Rebuilding" or "Unavailable" or "Forbidden")
        {
            var old = value.Freshness;
            value = value with { Freshness = new(old.ProjectionContractSchemaVersion, old.ProjectionCursor, old.LastAppliedEventPosition,
                old.LastAppliedEventTimestamp, old.ProjectionGeneratedAt, old.LagDuration, false, ProjectionTrustState.Parse(mutation),
                ProjectionFreshnessReasonCode.Parse(mutation.ToLowerInvariant())) };
        }
        if (mutation == "pending") f.Store.SeedRaw("statestore", ConversationProjectionReadModelKeys.ResolutionProofKey(
            ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation), new ConversationResolutionMetadataProof(null));
        f.Store.SeedRaw("statestore", key, value);
        var result = Payload(await f.DispatchAdvisoryAsync()); result.Metadata.ShouldBeNull(); result.Incomplete.ShouldBeTrue();
    }

    [Fact]
    public async Task RevocationBeforeDisclosureWithholdsTheReadMetadata()
    {
        var f = new ResolutionMetadataFixture(); await f.ProjectAsync(); f.Access.OnCheck = n => f.Access.Denied = n == 2;
        (await f.DispatchAdvisoryAsync()).ErrorMessage.ShouldBe("safe-denial"); f.Access.Checks.ShouldBe(2);
    }

    [Fact]
    public async Task MissingVerifiedCallerAndTamperedWorkloadReadNoMetadata()
    {
        var f = new ResolutionMetadataFixture(); await f.ProjectAsync();
        (await f.DispatchAdvisoryAsync(verified: false)).Success.ShouldBeFalse();
        (await f.DispatchAdvisoryAsync(ResolutionMetadataFixture.Query() with
            { QueryType = GetConversationAdvisoryMetadataQuery.QueryType, AuthenticatedWorkloadId = "untrusted" })).Success.ShouldBeFalse();
        f.Access.Checks.ShouldBe(0); f.Guard.Reads.ShouldBeEmpty();
    }

    [Fact]
    public async Task CancellationPropagates()
    {
        var f = new ResolutionMetadataFixture(); using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => f.DispatchAdvisoryAsync(cancellationToken: cancelled.Token));
    }

    [Fact]
    public async Task DefaultNonsignalStorePermissionDisclosesCommittedMetadata()
    {
        var f = new ResolutionMetadataFixture(); await f.ProjectAsync();
        var services = new ServiceCollection(); services.AddLogging(); services.AddConversationTenantAccess();
        await using var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<ITenantProjectionStore>();
        (store is IConversationTenantProjectionSignal).ShouldBeFalse();
        await store.SaveAsync(new TenantLocalState
        {
            TenantId = ResolutionMetadataFixture.Tenant.Value, Status = TenantStatus.Active,
            Members = new() { ["actor"] = TenantRole.TenantReader },
        }, TestContext.Current.CancellationToken);
        var access = provider.GetRequiredService<IConversationTenantAccessService>();
        var decision = await access.CheckAccessAsync(ConversationTenantAccessRequirement.Read,
            ResolutionMetadataFixture.Tenant, "actor", cancellationToken: TestContext.Current.CancellationToken);
        decision.IsAllowed.ShouldBeTrue(); decision.ProjectionVersion.ShouldBeNull(); decision.ProjectionWatermark.ShouldBeNull();
        var result = Payload(await f.DispatchAdvisoryAsync(access: access));
        result.Metadata.ShouldNotBeNull(); result.Incomplete.ShouldBeFalse();
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("negative")]
    [InlineData("blank")]
    [InlineData("oversized")]
    public async Task OrdinaryFinalPermissionDoesNotRequireProjectionEvidence(string mutation)
    {
        var f = new ResolutionMetadataFixture(); await f.ProjectAsync();
        f.Access.OnCheck = n =>
        {
            if (n != 2) return;
            if (mutation == "missing") f.Access.MissingDecisionEvidence = true;
            if (mutation == "negative") f.Access.DecisionVersionOverride = -1;
            if (mutation == "blank") f.Access.DecisionWatermarkOverride = " ";
            if (mutation == "oversized") f.Access.DecisionWatermarkOverride = new string('w', 1025);
        };
        var result = Payload(await f.DispatchAdvisoryAsync());
        result.Metadata.ShouldNotBeNull(); result.Incomplete.ShouldBeFalse(); f.Access.Checks.ShouldBe(2);
    }

    [Fact]
    public async Task KnownErasedNativeSourceWithholdsMetadata()
    {
        var f = new ResolutionMetadataFixture(); await f.ProjectAsync();
        string key = ConversationProjectionReadModelKeys.ResolutionMetadataKey(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation);
        f.Store.SeedRaw("statestore", key, f.Metadata with { NativeSourceProof = f.Metadata.NativeSourceProof! with { Erased = true } });
        var result = Payload(await f.DispatchAdvisoryAsync());
        result.Metadata.ShouldBeNull(); result.Incomplete.ShouldBeTrue();
    }

    [Theory]
    [InlineData("null-current")]
    [InlineData("completed-replacement")]
    [InlineData("pending-current")]
    [InlineData("barrier")]
    public async Task FenceChangedDuringAdvisoryReadWithholdsMetadata(string mutation)
    {
        var f = new ResolutionMetadataFixture(); await f.ProjectAsync();
        string proofKey = ConversationProjectionReadModelKeys.ResolutionProofKey(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation);
        var proof = f.Proof;
        f.Guard.AfterRead = (type, _) =>
        {
            if (type == typeof(ConversationResolutionMetadataReadModel))
                f.Store.SeedRaw("statestore", proofKey, mutation switch
                {
                    "completed-replacement" => proof with { Current = proof.Current! with { DispatchId = "replacement" } },
                    "pending-current" => proof with { Current = proof.Current! with { IsPending = true } },
                    "barrier" => proof with { Barrier = new(99, "barrier") },
                    _ => new ConversationResolutionMetadataProof(null),
                });
            return Task.CompletedTask;
        };
        var result = Payload(await f.DispatchAdvisoryAsync());
        result.Metadata.ShouldBeNull(); result.Incomplete.ShouldBeTrue();
        f.Guard.Reads.ShouldContain(typeof(ConversationResolutionMetadataReadModel));
        f.Guard.ReadKeys.Count(key => key == proofKey).ShouldBe(2);
    }

    [Fact]
    public async Task ExistingSummaryWithoutIsolatedMetadataIsIncompleteWhileAbsentSummaryIsDenied()
    {
        var f = new ResolutionMetadataFixture(); await f.ProjectAsync();
        await f.EraseAsync(ConversationProjectionReadModelKeys.ResolutionMetadataKey(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation));
        var result = Payload(await f.DispatchAdvisoryAsync());
        result.Metadata.ShouldBeNull(); result.Incomplete.ShouldBeTrue();
        f.Guard.ReadKeys.ShouldContain(ConversationProjectionReadModelKeys.TenantIndexKey(ResolutionMetadataFixture.Tenant));
        f.Guard.ReadKeys.ShouldNotContain(ConversationProjectionReadModelKeys.ConversationKey(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation));
        var absent = new ResolutionMetadataFixture();
        (await absent.DispatchAdvisoryAsync()).ErrorMessage.ShouldBe("safe-denial");
    }

    [Fact]
    public async Task RetryableOwnerDenialReturnsUnavailableWithoutMetadataReads()
    {
        var f = new ResolutionMetadataFixture(); await f.ProjectAsync(); f.Access.RetryableDenial = true;
        var result = Payload(await f.DispatchAdvisoryAsync());
        result.Metadata.ShouldBeNull(); result.Incomplete.ShouldBeTrue(); f.Guard.Reads.ShouldBeEmpty();
    }

    private static ConversationAdvisoryMetadataResult Payload(QueryResult result)
    {
        result.Success.ShouldBeTrue(); result.ProjectionType.ShouldBeNull(); return result.GetPayload().Deserialize<ConversationAdvisoryMetadataResult>(JsonSerializerOptions.Web)!;
    }
}
