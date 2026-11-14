#if SUPPORTED_PROJECT_READ_DISTRIBUTED
namespace Hexalith.Projects.Integration.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Claims;
using Dapr.Client;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Infrastructure;
using Hexalith.Projects.Projections.TenantAccess;
using Microsoft.Extensions.Configuration;
using Xunit;

/// <summary>Focused controls for the distributed fixture's independent evidence checks.</summary>
public sealed class SupportedProjectReadEvidenceTests
{
    /// <summary>A state read receives the private deadline and cannot return successful convergence after cancellation.</summary>
    [Fact]
    public async Task RecoveryProjectionDeadlineRejectsLateSuccessfulStateRead()
    {
        CancellationToken outer = TestContext.Current.CancellationToken;
        bool receivedPrivateDeadline = false;
        async Task<ProjectTenantAccessProjection> ReadAsync(CancellationToken cancellation)
        {
            receivedPrivateDeadline = cancellation.CanBeCanceled && cancellation != outer;
            TaskCompletionSource<ProjectTenantAccessProjection> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            using CancellationTokenRegistration registration = cancellation.Register(() => completion.TrySetResult(new()
                { TenantId = "tenant", Watermark = 4, SnapshotWatermark = 4, Version = 3 }));
            return await completion.Task.ConfigureAwait(false);
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => SupportedProjectReadDistributedFixture.ObserveRecoveryProjectionAsync(
            ReadAsync, 4, TimeSpan.FromMilliseconds(20), outer));
        Assert.True(receivedPrivateDeadline);
    }

    /// <summary>A matching complete snapshot can converge before its deadline without waiting for expiration.</summary>
    [Fact]
    public async Task RecoveryProjectionAcceptsMatchingSnapshotBeforeDeadline()
    {
        ProjectTenantAccessProjection projection = new() { TenantId = "tenant", Watermark = 4, SnapshotWatermark = 4, Version = 3 };
        int reads = 0;
        ProjectTenantAccessProjection observed = await SupportedProjectReadDistributedFixture.ObserveRecoveryProjectionAsync(cancellation =>
        {
            cancellation.ThrowIfCancellationRequested();
            reads++;
            return Task.FromResult(projection);
        }, 4, TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Same(projection, observed);
        Assert.Equal(1, reads);
    }

    /// <summary>Administrator credentials retain only canonical public grants and finite provisioning permissions.</summary>
    [Fact]
    public void PublicFixtureAdministratorGrantsExcludeHistoricalStorage()
    {
        List<Claim> claims = SupportedProjectReadDistributedFixture.HumanClaims("primary", "foreign", chatbot: false, administrator: true);
        Assert.Equal(["primary"], claims.Where(claim => claim.Type == "eventstore:tenant").Select(claim => claim.Value));
        Assert.DoesNotContain(claims, claim => claim.Value == "system");
        Assert.Equal(new[] { "command:submit", "query:read", "replay:read", "read_metadata", "projects:read", "projects:list",
            "create_folder", "manage_folder_access", "manage_organization_acl", "tenants:create", "tenants:add-user", "tenants:read", "global-administrators:read", ProjectionDeliveryMaintenanceScopeCatalog.Permission },
            claims.Where(claim => claim.Type == "eventstore:permission").Select(claim => claim.Value));
        Assert.DoesNotContain(SupportedProjectReadDistributedFixture.HumanClaims("primary", "foreign"),
            claim => claim.Type == "eventstore:permission" && (claim.Value.StartsWith("tenants:", StringComparison.Ordinal)
                || claim.Value == ProjectionDeliveryMaintenanceScopeCatalog.Permission));
    }

    /// <summary>Positive foreign actors keep one grant, while the deliberate coarse negative control keeps both distinct grants.</summary>
    [Theory]
    [InlineData("primary", false, "primary")]
    [InlineData("primary", true, "primary,foreign")]
    [InlineData("foreign", false, "foreign")]
    [InlineData("foreign", true, "foreign")]
    public void PublicFixtureGrantAugmentationPreservesDistinctScopesAndOriginalActor(string tenant, bool augmented, string expected)
    {
        ArgumentNullException.ThrowIfNull(expected);
        List<Claim> claims = SupportedProjectReadDistributedFixture.HumanClaims(tenant, "foreign", foreignTransportGrant: augmented);
        string[] grants = claims.Where(claim => claim.Type == "eventstore:tenant").Select(claim => claim.Value).ToArray();
        Assert.Equal(expected.Split(','), grants);
        Assert.True(Hexalith.EventStore.Contracts.Identity.TenantCanonicalizer.TryCanonicalizeGrants(grants, out _));
        Assert.Equal(tenant, claims.Single(claim => claim.Type == "tenant_id").Value);
        Assert.Equal(tenant == "foreign" ? "foreign-actor" : "actor-1", claims.Single(claim => claim.Type == "sub").Value);
    }

    /// <summary>Maintenance sends the approved exact URL and one explicit disjoint scope.</summary>
    [Fact]
    public void PublicFixtureMaintenanceUsesExactRouteAndScope()
    {
        const string route = "api/v1/admin/projections/platform/global-administrators/delivery-reconciliation";
        Assert.Equal(route, "api/v1/admin/projections/" + ProjectionDeliveryMaintenanceScopeCatalog.Route);
        using HttpRequestMessage request = new(HttpMethod.Post, "https://fixture/" + route);
        SupportedProjectReadDistributedFixture.BindDeliveryMaintenanceScope(request, route);
        Assert.Equal(["platform:global-administrators-delivery-reconciliation"], request.Headers.GetValues("X-Hexalith-Scope"));
        Assert.Single(request.Headers);
        Assert.Throws<ArgumentException>(() => SupportedProjectReadDistributedFixture.BindDeliveryMaintenanceScope(request, route));
    }

    /// <summary>Foreign routes, verbs and mixed selectors cannot borrow maintenance scope.</summary>
    [Theory]
    [InlineData("legacy")]
    [InlineData("read")]
    [InlineData("tenant")]
    [InlineData("scope")]
    [InlineData("verb")]
    public void PublicFixtureMaintenanceRejectsOtherRoutesAndMixedScopes(string kind)
    {
        string route = "api/v1/admin/projections/" + ProjectionDeliveryMaintenanceScopeCatalog.Route;
        using HttpRequestMessage request = new(kind == "verb" ? HttpMethod.Get : HttpMethod.Post, "https://fixture/");
        if (kind == "legacy") route = "api/v1/admin/projections/system/global-administrators/delivery-reconciliation/global-administrators/global-administrators";
        if (kind == "read") route = "api/v1/streams/read";
        if (kind == "tenant") request.Headers.Add("X-Hexalith-Tenant", "primary");
        if (kind == "scope") request.Headers.Add("X-Hexalith-Scope", "platform:eventstore-admin");
        Assert.Throws<ArgumentException>(() => SupportedProjectReadDistributedFixture.BindDeliveryMaintenanceScope(request, route));
    }

    /// <summary>Reserved, malformed and platform scopes never become public Tenant grants.</summary>
    [Theory]
    [InlineData("system")]
    [InlineData("platform:tenants")]
    [InlineData(" tenant ")]
    [InlineData("")]
    public void PublicFixtureCredentialsRejectInvalidTenantScopes(string tenant)
        => Assert.Throws<ArgumentException>(() => SupportedProjectReadDistributedFixture.HumanClaims(tenant, "foreign"));

    /// <summary>Status requests carry exactly the explicit scope required by the owner contract.</summary>
    [Theory]
    [InlineData("primary", "X-Hexalith-Tenant")]
    [InlineData("platform:tenants", "X-Hexalith-Scope")]
    [InlineData("platform:global-administrators", "X-Hexalith-Scope")]
    public void PublicFixtureStatusBindsOneExplicitScope(string scope, string header)
    {
        using HttpRequestMessage request = new();
        SupportedProjectReadDistributedFixture.BindStatusScope(request, scope);
        Assert.Equal([scope], request.Headers.GetValues(header));
        Assert.Single(request.Headers);
        Assert.Throws<ArgumentException>(() => SupportedProjectReadDistributedFixture.BindStatusScope(request, scope));
    }

    /// <summary>Invalid, reserved and mixed status scopes fail before transport.</summary>
    [Theory]
    [InlineData("system")]
    [InlineData("platform:other")]
    [InlineData(" tenant ")]
    [InlineData("")]
    public void PublicFixtureStatusRejectsInvalidAndMixedScopes(string scope)
    {
        using HttpRequestMessage request = new();
        Assert.Throws<ArgumentException>(() => SupportedProjectReadDistributedFixture.BindStatusScope(request, scope));
        Assert.Empty(request.Headers);
        request.Headers.Add("X-Hexalith-Tenant", "primary");
        Assert.Throws<ArgumentException>(() => SupportedProjectReadDistributedFixture.BindStatusScope(request, "platform:tenants"));
    }

    /// <summary>The complete provider control must finish before the actual matrix file can exist.</summary>
    [Fact]
    public async Task ScenarioReceiptWaitsForDeferredProviderVerification()
    {
        string path = Path.Combine(Path.GetTempPath(), "supported-read-receipt-" + Guid.NewGuid().ToString("N") + ".json");
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously), release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task VerifyAsync()
        {
            entered.SetResult();
            await release.Task.ConfigureAwait(false);
        }
        try
        {
            Task write = SupportedProjectReadScenarioEvidence.WriteAfterProviderVerificationAsync(path, "duplicate",
                SupportedProjectReadScenarioEvidence.Cases("duplicate"), "reads-" + new string('a', 24), VerifyAsync, TestContext.Current.CancellationToken);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.False(File.Exists(path));
            Assert.False(write.IsCompleted);
            release.SetResult();
            await write.ConfigureAwait(true);
            using JsonDocument receipt = JsonDocument.Parse(await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
            Assert.Equal("duplicate", receipt.RootElement.GetProperty("qualificationGroup").GetString());
        }
        finally { release.TrySetResult(); File.Delete(path); }
    }

    /// <summary>Faulted, canceled and late-canceled provider checks never leave a successful matrix receipt.</summary>
    [Theory]
    [InlineData("fault")]
    [InlineData("canceled")]
    [InlineData("late-canceled")]
    public async Task FailedDeferredProviderVerificationCannotPersistScenarioSuccess(string mode)
    {
        string path = Path.Combine(Path.GetTempPath(), "supported-read-receipt-" + Guid.NewGuid().ToString("N") + ".json");
        using CancellationTokenSource cancellation = new();
        Task VerifyAsync()
        {
            if (mode == "fault") return Task.FromException(new InvalidOperationException("Provider parity failed."));
            cancellation.Cancel();
            return mode == "canceled" ? Task.FromCanceled(cancellation.Token) : Task.CompletedTask;
        }
        try
        {
            Task write = SupportedProjectReadScenarioEvidence.WriteAfterProviderVerificationAsync(path, "duplicate",
                SupportedProjectReadScenarioEvidence.Cases("duplicate"), "reads-" + new string('a', 24), VerifyAsync, cancellation.Token);
            if (mode == "fault") await Assert.ThrowsAsync<InvalidOperationException>(() => write);
            else await Assert.ThrowsAnyAsync<OperationCanceledException>(() => write);
            Assert.False(File.Exists(path));
        }
        finally { File.Delete(path); }
    }

    /// <summary>One complete required-key batch preserves all twenty values and their original inventory order.</summary>
    [Fact]
    public async Task RequiredSnapshotUsesOneCompleteBatchAndRetainsEveryByteAndVersion()
    {
        string[] keys = Enumerable.Range(0, 20).Select(index => (index % 5 == 0 ? "named||" : "logical:") + index).ToArray();
        SupportedProjectReadStateSnapshot[] expected = keys.Select((key, index) => new SupportedProjectReadStateSnapshot(key, [(byte)index, 0, 255], (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture))).ToArray();
        int batches = 0;
        SupportedProjectReadStateSnapshot[] actual = await SupportedProjectReadDistributedFixture.ObserveRequiredSnapshotAsync(keys, observed =>
        {
            batches++;
            Assert.Equal(keys, observed);
            return Task.FromResult(expected.Reverse().ToArray());
        }, allowIncomplete: false);
        Assert.Equal(1, batches);
        Assert.Equal(keys, actual.Select(value => value.Key));
        Assert.True(SupportedProjectReadDistributedFixture.SameDomainState(expected, actual));
    }

    /// <summary>Batching cannot conceal a missing, duplicated, foreign or empty required value or version.</summary>
    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("foreign")]
    [InlineData("empty-data")]
    [InlineData("empty-version")]
    public async Task RequiredSnapshotBatchRejectsIncompleteOrSubstitutedObservations(string mode)
    {
        string[] keys = ["logical:key", "named||key"];
        SupportedProjectReadStateSnapshot[] values = [new(keys[0], [1], "1"), new(keys[1], [2], "2")];
        values = mode switch
        {
            "missing" => values[..1], "duplicate" => [values[0], values[0]], "foreign" => [values[0], new("foreign", [2], "2")],
            "empty-data" => [values[0], new(keys[1], [], "2")], _ => [values[0], new(keys[1], [2], string.Empty)],
        };
        await Assert.ThrowsAsync<Xunit.Sdk.TrueException>(() => SupportedProjectReadDistributedFixture.ObserveRequiredSnapshotAsync(keys,
            _ => Task.FromResult(values), allowIncomplete: false));
    }

    /// <summary>Incomplete observations retain their actual absent bytes and version for later failure evidence.</summary>
    [Fact]
    public async Task IncompleteSnapshotNeverInventsPersistedBytesOrVersion()
    {
        SupportedProjectReadStateSnapshot[] observed = await SupportedProjectReadDistributedFixture.ObserveRequiredSnapshotAsync(["logical:missing"],
            _ => Task.FromResult<SupportedProjectReadStateSnapshot[]>([new("logical:missing", [], string.Empty)]), allowIncomplete: true);
        Assert.Empty(observed.Single().Bytes);
        Assert.Empty(observed.Single().ETag);
    }

    /// <summary>Ambiguous required inventories fail before accessing the provider.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RequiredSnapshotRejectsEmptyOrDuplicateInventoriesBeforeRead(bool duplicate)
    {
        int reads = 0;
        await Assert.ThrowsAsync<Xunit.Sdk.TrueException>(() => SupportedProjectReadDistributedFixture.ObserveRequiredSnapshotAsync(
            duplicate ? ["logical:key", "logical:key"] : [], _ =>
            {
                reads++;
                return Task.FromResult(Array.Empty<SupportedProjectReadStateSnapshot>());
            }, allowIncomplete: false));
        Assert.Equal(0, reads);
    }

    /// <summary>Each independent receipt contains only its actual finite cases and never borrows another scenario flag.</summary>
    [Theory]
    [InlineData("duplicate", 1)]
    [InlineData("full-replay", 1)]
    [InlineData("main", 9)]
    public void ScenarioReceiptsNeverClaimAnotherRuntimeOrUnexecutedCase(string scenario, int count)
    {
        string[] cases = SupportedProjectReadScenarioEvidence.Cases(scenario);
        JsonElement result = SupportedProjectReadScenarioEvidence.Create(scenario, cases, "reads-" + new string('a', 24));
        Assert.Equal(2, result.GetProperty("version").GetInt32());
        Assert.Equal(scenario, result.GetProperty("qualificationGroup").GetString());
        Assert.Equal("one-fresh-owned-runtime", result.GetProperty("providerScope").GetString());
        Assert.Equal(count, result.GetProperty("cases").GetArrayLength());
        Assert.Equal(scenario == "duplicate", result.GetProperty("duplicateDeliveryVerified").GetBoolean());
        Assert.Equal(scenario == "full-replay", result.GetProperty("fullReplayVerified").GetBoolean());
        Assert.Equal(scenario == "main", result.GetProperty("projectsProcessRestartVerified").GetBoolean());
        Assert.False(result.GetProperty("productionReadiness").GetBoolean());
        Assert.False(result.GetProperty("acceptedEquivalence").GetBoolean());
        cases[0] = "poisoned";
        Assert.DoesNotContain("poisoned", SupportedProjectReadScenarioEvidence.Cases(scenario));
    }

    /// <summary>Identical scenario rows from two real isolation names retain distinct provider bindings.</summary>
    [Fact]
    public void ScenarioMetadataBindsItsOwnProviderIsolation()
    {
        string[] cases = SupportedProjectReadScenarioEvidence.Cases("duplicate");
        JsonElement first = SupportedProjectReadScenarioEvidence.Create("duplicate", cases, "reads-" + new string('a', 24));
        JsonElement second = SupportedProjectReadScenarioEvidence.Create("duplicate", cases, "reads-" + new string('b', 24));
        Assert.NotEqual(first.GetProperty("providerIsolationSha256").GetString(), second.GetProperty("providerIsolationSha256").GetString());
        Assert.Equal(64, first.GetProperty("providerIsolationSha256").GetString()!.Length);
        Assert.Equal(first.GetProperty("cases").GetRawText(), second.GetProperty("cases").GetRawText());
    }

    /// <summary>An unowned, shared, malformed or path-like isolation cannot produce a group receipt.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("shared")]
    [InlineData("reads-short")]
    [InlineData("reads-../other")]
    public void ScenarioMetadataRejectsUnownedIsolationNames(string isolation)
        => Assert.Throws<InvalidOperationException>(() => SupportedProjectReadScenarioEvidence.Create("duplicate", SupportedProjectReadScenarioEvidence.Cases("duplicate"), isolation));

    /// <summary>Missing, duplicated, mixed or reordered scenario cases refuse before a qualifying receipt exists.</summary>
    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("mixed")]
    [InlineData("reversed")]
    [InlineData("unknown")]
    public void ScenarioReceiptRejectsMissingDuplicatedSubstitutedOrUnknownCases(string mode)
    {
        string[] cases = SupportedProjectReadScenarioEvidence.Cases("main");
        cases = mode switch
        {
            "missing" => cases[..^1],
            "duplicate" => [.. cases, cases[0]],
            "mixed" => [.. cases[..^1], SupportedProjectReadScenarioEvidence.Cases("duplicate")[0]],
            "reversed" => cases.Reverse().ToArray(),
            _ => cases,
        };
        Assert.Throws<InvalidOperationException>(() => SupportedProjectReadScenarioEvidence.Create(mode == "unknown" ? "other" : "main", cases, "reads-" + new string('a', 24)));
    }

    /// <summary>AlreadyCompleted never substitutes for durable proof, and no refusal or malformed outcome qualifies.</summary>
    [Fact]
    public void SeedProjectionCompletionRequiresExactDurableProofForIdempotentOutcome()
    {
        Hexalith.EventStore.Contracts.Projections.ProjectionDispatchResponse Response(Hexalith.EventStore.Contracts.Projections.ProjectionDispatchStatus status)
            => new(Hexalith.EventStore.Contracts.Projections.ProjectionDispatchProtocol.Version,
                [new("project-reads", status, null, null)]);
        var completed = Response(Hexalith.EventStore.Contracts.Projections.ProjectionDispatchStatus.Completed);
        var already = Response(Hexalith.EventStore.Contracts.Projections.ProjectionDispatchStatus.AlreadyCompleted);
        Assert.True(SupportedProjectReadDistributedFixture.ProjectSeedCompletionMatches(completed, false));
        Assert.False(SupportedProjectReadDistributedFixture.ProjectSeedCompletionMatches(already, false));
        Assert.True(SupportedProjectReadDistributedFixture.ProjectSeedCompletionMatches(already, true));
        foreach (var status in Enum.GetValues<Hexalith.EventStore.Contracts.Projections.ProjectionDispatchStatus>()
            .Where(item => item is not (Hexalith.EventStore.Contracts.Projections.ProjectionDispatchStatus.Completed
                or Hexalith.EventStore.Contracts.Projections.ProjectionDispatchStatus.AlreadyCompleted)))
            Assert.False(SupportedProjectReadDistributedFixture.ProjectSeedCompletionMatches(Response(status), true));
        foreach (var malformed in new[]
        {
            completed with { Version = 999 }, completed with { Outcomes = [] }, completed with { Outcomes = [.. completed.Outcomes, .. completed.Outcomes] },
            completed with { Outcomes = [completed.Outcomes[0] with { ProjectionType = "foreign" }] },
        }) Assert.False(SupportedProjectReadDistributedFixture.ProjectSeedCompletionMatches(malformed, true));
        Assert.False(SupportedProjectReadDistributedFixture.ProjectSeedCompletionMatches(null, true));
    }

    /// <summary>The seed mapper retains actual metadata/version/correlation fields and refuses a missing source correlation.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    public void PersistedProjectionMappingPreservesSourceWireMetadata(int? payloadVersion)
    {
        var first = new StreamReadEvent(1, "Event", "{}"u8.ToArray(), "json", 1, "event-1", "correlation", "cause", DateTimeOffset.UtcNow, "actor")
            { GlobalPosition = 10, PayloadVersion = payloadVersion };
        var second = first with { SequenceNumber = 2, GlobalPosition = 11, MessageId = "event-2" };
        var source = new StreamReadPage("tenant", "projects", "project", [first, second], new(0, null, 2, 2, 2, false, null));
        var mapped = SupportedProjectReadDistributedFixture.MapProjectStream(source, "tenant", "project");
        Assert.All(mapped.Events, item => { Assert.Equal(1, item.MetadataVersion); Assert.Equal(payloadVersion, item.StoredPayloadVersion); Assert.Equal("correlation", item.CorrelationId); });
        Assert.Equal(first.GlobalPosition, mapped.Events[0].GlobalPosition);
        Assert.Equal(first.UserId, mapped.Events[0].UserId);
        Assert.Equal(first.MessageId, mapped.Events[0].MessageId);
        Assert.False(SupportedProjectReadDistributedFixture.IsCompleteProjectStream(new("tenant", "projects", "project"),
            source with { Events = [first with { CorrelationId = null }, second] }));
    }

    /// <summary>Native backup quiescence requires the complete unchanged source inventory, bytes and ETags.</summary>
    [Fact]
    public void CutoverInventoryRejectsAddedMissingChangedAndUnversionedState()
    {
        var before = new Dictionary<string, SupportedProjectReadStateSnapshot>
        {
            ["projection-activation:outbox:v1:00"] = new("projection-activation:outbox:v1:00", "{\"items\":[],\"lastRevision\":1}"u8.ToArray(), "1"),
        };
        Assert.True(SupportedProjectReadDistributedFixture.SameCutoverInventory(before, before));
        foreach (var after in new[]
        {
            new Dictionary<string, SupportedProjectReadStateSnapshot>(),
            new Dictionary<string, SupportedProjectReadStateSnapshot>(before) { ["extra"] = new("extra", "{}"u8.ToArray(), "1") },
            new Dictionary<string, SupportedProjectReadStateSnapshot> { [before.First().Key] = before.First().Value with { ETag = "2" } },
            new Dictionary<string, SupportedProjectReadStateSnapshot> { [before.First().Key] = before.First().Value with { Bytes = "{}"u8.ToArray() } },
        }) Assert.False(SupportedProjectReadDistributedFixture.SameCutoverInventory(before, after));
        var unversioned = new Dictionary<string, SupportedProjectReadStateSnapshot> { [before.First().Key] = before.First().Value with { ETag = "" } };
        Assert.False(SupportedProjectReadDistributedFixture.SameCutoverInventory(unversioned, unversioned));
    }

    /// <summary>Seed completion binds the complete supported source to the actual last accepted command.</summary>
    [Fact]
    public void SeedDeliverySourceRefusesWrongCommandScopeOrHead()
    {
        var item = new StreamReadEvent(1, "Event", "{}"u8.ToArray(), "json", 1, "event", "accepted-command", "accepted-command", DateTimeOffset.UtcNow, "actor");
        var source = new StreamReadPage("tenant", "folders", "folder", [item], new(0, null, 1, 1, 1, false, null));
        bool Matches(StreamReadPage value, string command = "accepted-command") => SupportedProjectReadDistributedFixture.SeedSourceMatches(value, "tenant", "folders", "folder", command, "actor");
        Assert.True(Matches(source));
        Assert.False(Matches(source, "different-command"));
        Assert.False(Matches(source, ""));
        foreach (StreamReadPage invalid in new[]
        {
            source with { Tenant = "other" }, source with { Domain = "other" }, source with { AggregateId = "other" },
            source with { Events = [] }, source with { Metadata = source.Metadata with { LatestSequence = 2 } },
            source with { Metadata = source.Metadata with { IsTruncated = true } },
            source with { Metadata = source.Metadata with { NextContinuationToken = new("unexpected") } },
            source with { Events = [item with { CorrelationId = "other" }] }, source with { Events = [item with { CausationId = "other" }] },
            source with { Events = [item with { UserId = "other" }] },
        }) Assert.False(Matches(invalid));
    }

    /// <summary>Only an explicit complete provider response can prove lifecycle absence; missing or conflicting observations refuse.</summary>
    [Fact]
    public void SeedProviderObservationsDistinguishExplicitAbsenceFromMissingProof()
    {
        string[] keys = ["checkpoint", "epoch", "lifecycle"];
        object[] rows = [new { key = keys[0], exists = true, dataHex = "7B7D", version = "1" },
            new { key = keys[1], exists = true, dataHex = "32", version = "2" },
            new { key = keys[2], exists = false, dataHex = false, version = false }];
        var observed = SupportedProjectReadDistributedFixture.ParseSeedDeliveryStates(JsonSerializer.SerializeToUtf8Bytes(rows), keys);
        Assert.Equal("{}"u8.ToArray(), observed["checkpoint"]!.Bytes);
        Assert.Equal("2"u8.ToArray(), observed["epoch"]!.Bytes);
        Assert.Null(observed["lifecycle"]);
        foreach (object[] invalid in new[]
        {
            rows.Take(2).ToArray(), new[] { rows[0], rows[1], rows[1] },
            new object[] { rows[0], rows[1], new { key = "other", exists = false, dataHex = false, version = false } },
            new object[] { rows[0], rows[1], new { key = "lifecycle", exists = false, dataHex = "", version = false } },
            new object[] { rows[0], rows[1], new { key = "lifecycle", dataHex = false, version = false } },
            new object[] { rows[0], rows[1], new { key = "lifecycle", exists = true, dataHex = false, version = false } },
        }) Assert.Throws<Xunit.Sdk.TrueException>(() => SupportedProjectReadDistributedFixture.ParseSeedDeliveryStates(JsonSerializer.SerializeToUtf8Bytes(invalid), keys));
    }

    /// <summary>Head observations require an exact authenticated SDK metadata shape and preserve the independently returned current sequence.</summary>
    [Fact]
    public void OwnerHeadObservationRejectsForeignBodiesRangesCountsAndContinuations()
    {
        var page = new StreamReadPage("tenant", "folders", "folder", [], new(0, 0, null, 42, 0, false, null));
        bool Matches(StreamReadPage value) => SupportedProjectReadDistributedFixture.IsOwnerHeadObservation(value, "tenant", "folder");
        Assert.True(Matches(page));
        Assert.True(Matches(page with { Metadata = page.Metadata with { LatestSequence = 0 } }));
        foreach (var invalid in new[]
        {
            page with { Tenant = "foreign" }, page with { Domain = "foreign" }, page with { AggregateId = "foreign" },
            page with { Metadata = page.Metadata with { FromSequence = 1 } }, page with { Metadata = page.Metadata with { ToSequence = null } },
            page with { Metadata = page.Metadata with { LastSequenceReturned = 1 } }, page with { Metadata = page.Metadata with { LatestSequence = -1 } },
            page with { Metadata = page.Metadata with { EventCount = 1 } }, page with { Metadata = page.Metadata with { IsTruncated = true } },
            page with { Metadata = page.Metadata with { NextContinuationToken = new("unexpected") } },
            page with { Events = [new(1, "unexpected", [], "json", 1, "message", "correlation", "command", DateTimeOffset.UtcNow, "actor")] },
        }) Assert.False(Matches(invalid));
    }

    /// <summary>Seed readiness needs the accepted ACL command, exact retained prefix, current head and projected actor grants.</summary>
    [Fact]
    public async Task OrganizationSeedReadinessRejectsForeignChangedOrIncompleteEvidence()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string[] actions = ["create_folder", "manage_folder_access"];
        Hexalith.Folders.Aggregates.Organization.IOrganizationAclEvent[] typed = [
            new Hexalith.Folders.Aggregates.Organization.OrganizationAclBaselineInitialized("tenant", "tenant", "accepted-command",
                "accepted-command", "accepted-command", new string('A', 64)),
            .. actions.Select(action => new Hexalith.Folders.Aggregates.Organization.OrganizationAclPrincipalGranted("tenant", "tenant",
                Hexalith.Folders.Aggregates.Organization.OrganizationAclPrincipalKind.User, "actor", action,
                "accepted-command", "accepted-command", "accepted-command", new string('A', 64))),
        ];
        StreamReadEvent[] events = typed.Select((value, index) => new StreamReadEvent(index + 1, value.GetType().FullName!,
            JsonSerializer.SerializeToUtf8Bytes(value, value.GetType()), "json", 1, "event-" + index,
            "accepted-command", "accepted-command", now, "actor")).ToArray();
        var source = new StreamReadPage("tenant", "folders", "tenant", events, new(0, null, 3, 3, 3, false, null));
        var state = new Hexalith.EventStore.Testing.Fakes.InMemoryReadModelStore();
        using HttpClient unusedTransport = new();
        var model = new Hexalith.Folders.Authorization.EventStoreOrganizationEffectivePermissionsReadModel(state,
            new Hexalith.Folders.Projections.TenantAccess.FixedUtcClock(now),
            new Hexalith.Folders.Authorization.DaprOrganizationStreamSequenceReader(
                new DaprNativeSourceMetadataReader(unusedTransport, new ConfigurationBuilder().Build(), TimeProvider.System)));
        foreach ((StreamReadEvent item, int index) in events.Select((item, index) => (item, index)))
        {
            string digest = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                [.. System.Text.Encoding.UTF8.GetBytes(item.EventTypeName + ":1:"), .. item.Payload]));
            await model.ApplyAsync("tenant", "tenant", item.MessageId, item.SequenceNumber, typed[index],
                TestContext.Current.CancellationToken, digest);
        }
        var stored = await state.GetAsync<Hexalith.Folders.Authorization.OrganizationPermissionProjection>("statestore",
            "projection:folders:organization-permissions:tenant", TestContext.Current.CancellationToken);
        using DaprClient client = new DaprClientBuilder().Build();
        JsonObject projection = JsonSerializer.SerializeToNode(stored.Value, client.JsonSerializerOptions)!.AsObject();
        bool Matches(JsonObject value, StreamReadPage? stream = null, long head = 3) => SupportedProjectReadDistributedFixture
            .OrganizationAclReadinessMatches(JsonSerializer.SerializeToElement(value), "tenant", "actor", "accepted-command", stream ?? source, head);
        Assert.True(Matches(projection));
        foreach (string field in projection.Select(property => property.Key).ToArray())
        {
            var invalid = (JsonObject)projection.DeepClone(); invalid.Remove(field); Assert.False(Matches(invalid));
        }
        foreach ((string field, JsonNode? value) in new (string, JsonNode?)[]
        {
            ("tenantId", JsonValue.Create("foreign")), ("organizationId", JsonValue.Create("foreign")),
            ("lastSequence", JsonValue.Create(2)), ("isInitialized", JsonValue.Create(false)),
            ("projectedAt", JsonValue.Create(now.AddSeconds(-1))), ("projectedAt", JsonValue.Create(now.AddMinutes(3))),
        })
        {
            var invalid = (JsonObject)projection.DeepClone(); invalid[field] = value; Assert.False(Matches(invalid));
        }
        foreach (string field in new[] { "principalId", "action" })
        {
            var invalid = (JsonObject)projection.DeepClone(); invalid["grants"]![0]![field] = "foreign"; Assert.False(Matches(invalid));
        }
        var changed = (JsonObject)projection.DeepClone(); changed["grants"]![0]!["principalKind"] = 1; Assert.False(Matches(changed));
        changed = (JsonObject)projection.DeepClone(); changed["processedMessages"]!["event-1"] = 1; Assert.False(Matches(changed));
        changed = (JsonObject)projection.DeepClone(); changed["recentMessages"]![0]!["payloadDigest"] = "invalid"; Assert.False(Matches(changed));
        changed = (JsonObject)projection.DeepClone(); changed["recentMessages"]![0]!["payloadDigest"] = new string('B', 64); Assert.False(Matches(changed));
        Assert.False(Matches(projection, head: 2));
        Assert.False(Matches(projection, source with { Tenant = "foreign" }));
        Assert.False(Matches(projection, source with { Metadata = source.Metadata with { IsTruncated = true } }));
        Assert.False(Matches(projection, source with { Metadata = source.Metadata with { EventCount = 2 } }));
        Assert.False(Matches(projection, source with { Events = events.Skip(1).ToArray() }));
        foreach (var invalid in new[]
        {
            events[1] with { CausationId = "other" }, events[1] with { CorrelationId = "other" }, events[1] with { UserId = "other" },
            events[1] with { EventTypeName = "foreign.OrganizationAclPrincipalGranted" },
            events[1] with { Payload = """{"ManagedTenantId":"foreign"}"""u8.ToArray() },
        }) Assert.False(Matches(projection, source with { Events = [events[0], invalid, events[2]] }));
    }

    /// <summary>Creation readiness binds a real aggregate event, actual permission projector and actual Dapr serialization without inventing grants.</summary>
    [Fact]
    public async Task FolderCreationSeedReadinessUsesActualProducerProjectorAndStoreSerialization()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var command = new Hexalith.Folders.Aggregates.Folder.CreateFolder("tenant", "tenant", "folder", "Owned folder label",
            null, null, [], "actor", "accepted-command", "accepted-command", "accepted-command", null);
        var result = Hexalith.Folders.Aggregates.Folder.FolderAggregate.Handle(
            Hexalith.Folders.Aggregates.Folder.FolderState.Empty, command, now);
        var created = Assert.IsType<Hexalith.Folders.Aggregates.Folder.FolderCreated>(Assert.Single(result.Events));
        var item = new StreamReadEvent(1, created.GetType().FullName!, JsonSerializer.SerializeToUtf8Bytes(created),
            "json", 1, "actual-event", "accepted-command", "accepted-command", now, "actor");
        var source = new StreamReadPage("tenant", "folders", "folder", [item], new(0, null, 1, 1, 1, false, null));
        string Digest(StreamReadEvent value) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            [.. System.Text.Encoding.UTF8.GetBytes(value.EventTypeName + ":" + (value.PayloadVersion ?? 1) + ":"), .. value.Payload]));
        var state = new Hexalith.EventStore.Testing.Fakes.InMemoryReadModelStore();
        var clock = new Hexalith.Folders.Projections.TenantAccess.FixedUtcClock(now);
        using HttpClient unusedTransport = new();
        var sequence = new Hexalith.Folders.Authorization.DaprOrganizationStreamSequenceReader(
            new DaprNativeSourceMetadataReader(unusedTransport, new ConfigurationBuilder().Build(), TimeProvider.System));
        var organization = new Hexalith.Folders.Authorization.EventStoreOrganizationEffectivePermissionsReadModel(state, clock, sequence);
        var model = new Hexalith.Folders.Authorization.EventStoreFolderEffectivePermissionsReadModel(state, clock, sequence, organization);
        await model.ApplyAsync("tenant", "folder", item.MessageId, 1, Digest(item), created, TestContext.Current.CancellationToken);
        var stored = await state.GetAsync<Hexalith.Folders.Authorization.FolderPermissionProjection>("statestore",
            "projection:folders:folder-permissions:tenant:folder", TestContext.Current.CancellationToken);
        using DaprClient client = new DaprClientBuilder().Build();
        JsonObject projection = JsonSerializer.SerializeToNode(stored.Value, client.JsonSerializerOptions)!.AsObject();
        bool Matches(JsonObject value, StreamReadPage? stream = null, long head = 1, string accepted = "accepted-command")
            => SupportedProjectReadDistributedFixture.FolderCreationReadinessMatches(JsonSerializer.SerializeToElement(value),
                "tenant", "folder", "actor", accepted, stream ?? source, head);
        Assert.True(Matches(projection));
        Assert.Empty(projection["overrides"]!.AsArray());
        foreach (string field in projection.Select(property => property.Key).ToArray())
        {
            var invalid = (JsonObject)projection.DeepClone(); invalid.Remove(field); Assert.False(Matches(invalid));
        }
        foreach ((string field, JsonNode? value) in new (string, JsonNode?)[]
        {
            ("tenantId", JsonValue.Create("foreign")), ("organizationId", JsonValue.Create("foreign")), ("folderId", JsonValue.Create("foreign")),
            ("lastSequence", JsonValue.Create(0)), ("lastSequence", JsonValue.Create(2)), ("isCreated", JsonValue.Create(false)),
            ("isArchived", JsonValue.Create(true)), ("projectedAt", JsonValue.Create(now.AddSeconds(-1))),
            ("projectedAt", JsonValue.Create(now.AddMinutes(3))),
        })
        {
            var invalid = (JsonObject)projection.DeepClone(); invalid[field] = value; Assert.False(Matches(invalid));
        }
        foreach ((string field, JsonNode? value) in new (string, JsonNode?)[]
        {
            ("messageId", JsonValue.Create("foreign")), ("sequence", JsonValue.Create(2)),
            ("payloadDigest", JsonValue.Create("invalid")), ("payloadDigest", JsonValue.Create(new string('B', 64))),
        })
        {
            var invalid = (JsonObject)projection.DeepClone(); invalid["recentMessages"]![0]![field] = value; Assert.False(Matches(invalid));
        }
        var changed = (JsonObject)projection.DeepClone(); changed["recentMessages"] = new JsonArray(); Assert.False(Matches(changed));
        changed = (JsonObject)projection.DeepClone(); changed["fileIdentities"]!["unexpected"] = new JsonObject(); Assert.False(Matches(changed));
        changed = (JsonObject)projection.DeepClone(); changed["overrides"]!.AsArray().Add(new JsonObject()); Assert.False(Matches(changed));
        Assert.False(Matches(projection, head: 0));
        Assert.False(Matches(projection, head: 2));
        Assert.False(Matches(projection, accepted: "other-command"));
        foreach (var invalid in new[]
        {
            source with { Tenant = "foreign" }, source with { Domain = "foreign" }, source with { AggregateId = "foreign" },
            source with { Metadata = source.Metadata with { IsTruncated = true } },
            source with { Metadata = source.Metadata with { EventCount = 2 } },
            source with { Metadata = source.Metadata with { LatestSequence = 2 } }, source with { Events = [] },
        }) Assert.False(Matches(projection, invalid));
        foreach (var invalid in new[]
        {
            item with { CausationId = "foreign" }, item with { CorrelationId = "foreign" }, item with { UserId = "foreign" },
            item with { EventTypeName = "foreign.FolderCreated" }, item with { Timestamp = default },
            item with { SerializationFormat = "other" }, item with { MetadataVersion = 2 }, item with { PayloadVersion = 2 },
            item with { Payload = [] }, item with { Payload = "{\"ManagedTenantId\":\"tenant\",\"managedTenantId\":\"tenant\"}"u8.ToArray() },
        }) Assert.False(Matches(projection, source with { Events = [invalid] }));
        foreach (var invalid in new[]
        {
            created with { ManagedTenantId = "foreign" }, created with { OrganizationId = "foreign" }, created with { FolderId = "foreign" },
            created with { ActorPrincipalId = "foreign" }, created with { CorrelationId = "foreign" }, created with { TaskId = "foreign" },
            created with { IdempotencyKey = "foreign" }, created with { IdempotencyFingerprint = new string('B', 64) },
            created with { OccurredAt = default }, created with { DisplayName = "changed" }, created with { Tags = null! },
            created with { LifecycleState = (Hexalith.Folders.Aggregates.Folder.FolderLifecycleState)999 },
            created with { RepositoryBindingState = (Hexalith.Folders.Aggregates.Folder.FolderRepositoryBindingState)999 },
        })
        {
            var changedSource = item with { Payload = JsonSerializer.SerializeToUtf8Bytes(invalid) };
            changed = (JsonObject)projection.DeepClone(); changed["recentMessages"]![0]!["payloadDigest"] = Digest(changedSource);
            Assert.False(Matches(changed, source with { Events = [changedSource] }));
        }
    }

    /// <summary>The real handler and store serializer retain the wrapped Projects and direct owner projection layouts.</summary>
    [Fact]
    public async Task TenantAuthorityReadinessUsesActualVersionedProjectsStoreDocument()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        using DaprClient client = new DaprClientBuilder().Build();
        SupportedProjectReadCapturingStateStore capture = new(client.JsonSerializerOptions);
        DaprProjectTenantAccessProjectionStore store = new(new ProjectsStateReadModelTestAdapter(capture));
        ProjectTenantAccessHandler handler = new(store, new FixedUtcClock(now), new TenantAccessOptions());
        await handler.HandleAsync(new(ProjectTenantAccessEventKind.TenantCreated, "tenant", "created", 1, now,
            "correlation"), TestContext.Current.CancellationToken);
        await handler.HandleAsync(new(ProjectTenantAccessEventKind.UserAddedToTenant, "tenant", "owner", 2, now,
            "correlation", "actor", "TenantOwner"), TestContext.Current.CancellationToken);
        StreamReadEvent[] events = [
            new(1, "Hexalith.Tenants.Contracts.Events.TenantCreated", [], "json", 1, "created", "correlation", "command", now, "actor"),
            new(2, "Hexalith.Tenants.Contracts.Events.UserAddedToTenant", [], "json", 1, "owner", "correlation", "command", now, "actor"),
        ];
        var source = new StreamReadPage("system", "tenants", "tenant", events, new(0, null, 2, 2, 2, false, null));
        JsonElement document = capture.Document!.Value;
        bool Matches(JsonElement value, bool wrapped = true) => SupportedProjectReadDistributedFixture
            .TenantAuthorityStateMatches(value, "tenant", "actor", source, wrapped);
        Assert.True(Matches(document));
        Assert.Equal(2, document.GetProperty("version").GetInt64());
        JsonElement direct = document.GetProperty("projection");
        Assert.True(Matches(direct, wrapped: false));
        Assert.False(Matches(direct));
        Assert.False(Matches(document, wrapped: false));
        foreach (string field in new[] { "projection", "version" })
        {
            var invalid = JsonNode.Parse(document.GetRawText())!.AsObject(); invalid.Remove(field);
            Assert.False(Matches(JsonSerializer.SerializeToElement(invalid)));
        }
        foreach (long version in new long[] { -1, 0, 1, 3 })
        {
            var invalid = JsonNode.Parse(document.GetRawText())!.AsObject(); invalid["version"] = version;
            Assert.False(Matches(JsonSerializer.SerializeToElement(invalid)));
        }
        var changed = JsonNode.Parse(document.GetRawText())!.AsObject(); changed["projection"]!["version"] = 3;
        Assert.False(Matches(JsonSerializer.SerializeToElement(changed)));
        changed = JsonNode.Parse(document.GetRawText())!.AsObject(); changed["unexpected"] = true;
        Assert.False(Matches(JsonSerializer.SerializeToElement(changed)));
        changed = JsonNode.Parse(document.GetRawText())!.AsObject(); changed["projection"]!["processedMessages"]!["owner"]!["eventTypeName"] = "different";
        Assert.False(Matches(JsonSerializer.SerializeToElement(changed)));
    }

    /// <summary>A present projection cannot authorize seeding until its exact retained source and actor owner evidence have arrived.</summary>
    [Fact]
    public void TenantAuthorityReadinessRejectsPresentButIncompleteOrConflictingProjection()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        StreamReadEvent[] events = [
            new(1, "Hexalith.Tenants.Contracts.Events.TenantCreated", [], "json", 1, "created", "correlation", "command", now, "actor"),
            new(2, "Hexalith.Tenants.Contracts.Events.UserAddedToTenant", [], "json", 1, "owner", "correlation", "command", now, "actor"),
        ];
        var source = new StreamReadPage("system", "tenants", "tenant", events, new(0, null, 2, 2, 2, false, null));
        JsonObject projection = JsonSerializer.SerializeToNode(new
        {
            tenantId = "tenant", enabled = true, watermark = 2, projectionWatermark = "tenant:2", lastEventTimestamp = now,
            replayConflict = false, malformedEvidence = false,
            principals = new Dictionary<string, object> { ["actor"] = new { principalId = "actor", role = "TenantOwner" } },
            processedMessages = events.ToDictionary(item => item.MessageId, item => new
            {
                messageId = item.MessageId, tenantId = "tenant", eventTypeName = item.EventTypeName.Split('.')[^1],
                sequenceNumber = item.SequenceNumber, timestamp = item.Timestamp,
            }),
        })!.AsObject();
        bool Matches(JsonObject value, StreamReadPage? stream = null) => SupportedProjectReadDistributedFixture
            .TenantAuthorityProjectionMatches(JsonSerializer.SerializeToElement(value), "tenant", "actor", stream ?? source);
        Assert.True(Matches(projection));
        foreach (string field in projection.Select(property => property.Key).ToArray())
        {
            var missing = (JsonObject)projection.DeepClone(); missing.Remove(field); Assert.False(Matches(missing));
        }
        foreach ((string field, JsonNode? value) in new (string, JsonNode?)[]
        {
            ("tenantId", JsonValue.Create("foreign")), ("enabled", JsonValue.Create(false)), ("watermark", JsonValue.Create(1)),
            ("projectionWatermark", JsonValue.Create("tenant:1")), ("lastEventTimestamp", JsonValue.Create(now.AddSeconds(-1))),
            ("replayConflict", JsonValue.Create(true)), ("malformedEvidence", JsonValue.Create(true)),
        })
        {
            var invalid = (JsonObject)projection.DeepClone(); invalid[field] = value; Assert.False(Matches(invalid));
        }
        foreach (string field in new[] { "principalId", "role" })
        {
            var invalid = (JsonObject)projection.DeepClone(); invalid["principals"]!["actor"]![field] = "other"; Assert.False(Matches(invalid));
        }
        foreach (string field in new[] { "messageId", "tenantId", "eventTypeName", "sequenceNumber", "timestamp" })
        {
            var invalid = (JsonObject)projection.DeepClone(); invalid["processedMessages"]!["owner"]!.AsObject().Remove(field); Assert.False(Matches(invalid));
        }
        var presentOnly = (JsonObject)projection.DeepClone(); presentOnly["principals"] = new JsonObject(); Assert.False(Matches(presentOnly));
        var missingEvent = (JsonObject)projection.DeepClone(); missingEvent["processedMessages"]!.AsObject().Remove("owner"); Assert.False(Matches(missingEvent));
        Assert.False(Matches(projection, source with { Tenant = "foreign" }));
        Assert.False(Matches(projection, source with { Metadata = source.Metadata with { IsTruncated = true } }));
        Assert.False(Matches(projection, source with { Events = events.Skip(1).ToArray() }));
        var first = (JsonObject)projection.DeepClone(); first["watermark"] = 1; first["projectionWatermark"] = "tenant:1";
        first["principals"] = new JsonObject(); first["processedMessages"]!.AsObject().Remove("owner");
        Assert.True(SupportedProjectReadDistributedFixture.TenantAuthorityProjectionMatches(
            JsonSerializer.SerializeToElement(first), "tenant", "actor", source, requireOwner: false, throughSequence: 1));
        Assert.False(SupportedProjectReadDistributedFixture.TenantAuthorityProjectionMatches(
            JsonSerializer.SerializeToElement(first), "tenant", "actor", source, throughSequence: 1));
        first["processedMessages"]!["created"]!["messageId"] = "different-original-event";
        Assert.False(SupportedProjectReadDistributedFixture.TenantAuthorityProjectionMatches(
            JsonSerializer.SerializeToElement(first), "tenant", "actor", source, requireOwner: false, throughSequence: 1));
        Assert.False(SupportedProjectReadDistributedFixture.TenantAuthorityProjectionMatches(
            JsonSerializer.SerializeToElement(projection), "tenant", "actor", source, throughSequence: 3));
    }

    /// <summary>Unknown delivery acknowledgements require the same original receipt; negative or conflicting statuses never pass.</summary>
    [Fact]
    public void HostedBootstrapReceiptRetainsUnknownAcknowledgementAndRejectsConflicts()
    {
        string root = new('a', 64);
        JsonObject receipt = new()
        {
            ["AttemptId"] = "original-hosted-attempt", ["RootDigest"] = root, ["MessageId"] = "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            ["CorrelationId"] = "01ARZ3NDEKTSV4RRFFQ69G5FAW", ["PrincipalId"] = "actor-1", ["Phase"] = "attempted", ["HttpStatus"] = null,
        };
        bool Matches(JsonObject value) => SupportedProjectReadDistributedFixture.HostedBootstrapReceiptMatches(JsonSerializer.SerializeToElement(value), root);
        Assert.True(Matches(receipt));
        foreach (string field in receipt.Select(property => property.Key).ToArray())
        {
            var missing = (JsonObject)receipt.DeepClone(); missing.Remove(field); Assert.False(Matches(missing));
        }
        foreach ((string field, JsonNode? value) in new (string, JsonNode?)[]
        {
            ("RootDigest", JsonValue.Create("other")), ("PrincipalId", JsonValue.Create("other-actor")),
            ("MessageId", JsonValue.Create("invalid")), ("CorrelationId", JsonValue.Create("invalid")),
            ("Phase", JsonValue.Create("response-received")), ("HttpStatus", JsonValue.Create(500)), ("HttpStatus", JsonValue.Create(202)),
        })
        {
            var invalid = (JsonObject)receipt.DeepClone(); invalid[field] = value; Assert.False(Matches(invalid));
        }
        receipt["Phase"] = "accepted"; receipt["HttpStatus"] = 202; Assert.True(Matches(receipt));
        foreach (int status in new[] { 200, 400, 401, 403, 409, 500, 503 }) { receipt["HttpStatus"] = status; Assert.False(Matches(receipt)); }
    }

    /// <summary>Only exact source type, command causation, correlation, metadata actor and decoded target/acting actor prove bootstrap.</summary>
    [Fact]
    public void HostedBootstrapEventRequiresExactCommandAndActorBindings()
    {
        byte[] Payload(string tenant = "system", string target = "actor-1", string actor = "actor-1") =>
            JsonSerializer.SerializeToUtf8Bytes(new { TenantId = tenant, UserId = target, ActorUserId = actor, SetAt = DateTimeOffset.UtcNow });
        var value = new StreamReadEvent(1, "Hexalith.Tenants.Contracts.Events.GlobalAdministratorSet", Payload(), "json", 1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAX", "correlation", "command", DateTimeOffset.UtcNow, "actor-1");
        bool Matches(StreamReadEvent item) => SupportedProjectReadDistributedFixture.HostedBootstrapEventMatches(item, "command", "correlation", "actor-1");
        Assert.True(Matches(value));
        var source = new StreamReadPage("system", "global-administrators", "global-administrators", [value], new(0, null, 1, 1, 1, false, null));
        bool SourceMatches(StreamReadPage page) => SupportedProjectReadDistributedFixture.HostedBootstrapSourceMatches(page, "command", "correlation", "actor-1");
        Assert.True(SourceMatches(source));
        var following = value with { SequenceNumber = 2, CausationId = "another-command" };
        var complete = source with { Events = [value, following], Metadata = new(0, null, 2, 2, 2, false, null) };
        Assert.True(SourceMatches(complete));
        foreach (var invalid in new[]
        {
            source with { Tenant = "platform:global-administrators" }, source with { Domain = "tenants" }, source with { AggregateId = "other" },
            source with { Events = [] }, source with { Events = [value, value], Metadata = source.Metadata with { EventCount = 2 } },
            source with { Metadata = source.Metadata with { IsTruncated = true } },
            source with { Metadata = source.Metadata with { NextContinuationToken = new("cursor") } },
            source with { Metadata = source.Metadata with { FromSequence = 1 } },
            source with { Metadata = source.Metadata with { ToSequence = 1 } },
            source with { Metadata = source.Metadata with { LatestSequence = 2 } },
            source with { Metadata = source.Metadata with { LastSequenceReturned = null } },
            source with { Metadata = source.Metadata with { LastSequenceReturned = 2 } },
            source with { Events = [value with { SequenceNumber = 0 }] },
            source with { Events = [value with { SequenceNumber = -1 }] },
            source with { Events = [value with { SequenceNumber = 2 }] },
            complete with { Events = [value, following with { SequenceNumber = 3 }] },
            complete with { Events = [value, following with { SequenceNumber = 1 }] },
            complete with { Events = [following, value] },
        }) Assert.False(SourceMatches(invalid));
        foreach (var invalid in new[]
        {
            value with { CausationId = "other" }, value with { CorrelationId = "other" }, value with { UserId = "other" },
            value with { Payload = Payload(target: "other") }, value with { Payload = Payload(actor: "other") }, value with { Payload = Payload(tenant: "foreign") },
            value with { EventTypeName = "foreign.GlobalAdministratorSet" }, value with { Payload = "invalid"u8.ToArray() },
            value with { Payload = """{"userId":"actor-1","UserId":"other"}"""u8.ToArray() }, value with { MetadataVersion = 2 },
        }) Assert.False(Matches(invalid));
    }

    /// <summary>New domain keys are observed while permitted audit and operational writes stay separate.</summary>
    [Fact]
    public void DomainFamilyEnumerationIncludesNewKeysAndExcludesAuditAndOperationalKeys()
    {
        foreach (string key in new[] { "projects:reads:v1:5:owned:9:new-entry", "projects:tenant-access:owned",
            "projection:folders:new-domain-family:owned:new-folder", "eventstore||AggregateActor||owned:projects:new-project||journal",
            "eventstore||AggregateActor||system:tenants:owned||journal", "projects-conversation-start-setup||owned:projects:new-project",
            "projects-conversation-start-setup||foreign:projects:new-project" })
            Assert.True(SupportedProjectReadDistributedFixture.IsOwnedDomainKey(key, "owned", "foreign"), "Owned domain family was omitted.");
        foreach (string key in new[] { "projects:read-audit:v1:owned", "admin:catalog:owned", "projection:folders:folder-permissions:unrelated:folder",
            "projects:reads:v1:9:unrelated:detail", "projects-conversation-start-setup||unrelated:projects:new-project",
            "projects-conversation-start-setup||owned-other:projects:new-project", "projects-conversation-start-setup||owned:other:new-project" })
            Assert.False(SupportedProjectReadDistributedFixture.IsOwnedDomainKey(key, "owned", "foreign"), "Unrelated or permitted state entered the domain comparison.");
    }

    /// <summary>Real owner/SDK hashed keys remain inside byte, version and complete-keyset invariance checks.</summary>
    [Fact]
    public async Task HashedOwnerViewsAndRealSdkGenerationKeysCannotEscapeDomainSnapshots()
    {
        var backing = new Hexalith.EventStore.Testing.Fakes.InMemoryReadModelStore();
        var recording = new SupportedProjectReadKeyRecordingStore(backing);
        var coordinator = new SharedProjectionEpochCoordinator(recording, backing);
        var scope = Hexalith.Folders.Projections.OperationalStatus.OperationalStatusGeneration.Scope("owned");
        string lifecycle = Hexalith.Folders.Projections.OperationalStatus.OperationalStatusKeys.Lifecycle("owned", "folder");
        string[] logical = [lifecycle,
            Hexalith.Folders.Projections.OperationalStatus.OperationalStatusKeys.Checkpoint("owned", "folder"),
            Hexalith.Folders.Projections.OperationalStatus.OperationalStatusKeys.Workspace("owned", "folder", "workspace"),
            Hexalith.Folders.Projections.OperationalStatus.OperationalStatusKeys.Task("owned", "folder", "task"),
            Hexalith.Folders.Projections.OperationalStatus.OperationalStatusKeys.Receipt("owned", "folder", 1),
            Hexalith.Folders.Projections.OperationalStatus.OperationalStatusKeys.Message("owned", "folder", "message"),
            Hexalith.Folders.Projections.OperationalStatus.OperationalStatusKeys.Evidence(lifecycle)];
        foreach (string key in logical)
            _ = await coordinator.ReadGenerationAsync<JsonObject>(scope, 1, key, TestContext.Current.CancellationToken).ConfigureAwait(true);
        string[] business = logical.Concat(recording.ReadKeys.Where(key => key.StartsWith("shared-generation:", StringComparison.Ordinal)
            || key.StartsWith("shared-cow-pointer:", StringComparison.Ordinal))).Distinct(StringComparer.Ordinal).ToArray();
        Assert.Equal(logical.Length * 3, business.Length);
        foreach (string key in business)
        {
            Assert.DoesNotContain("owned", key, StringComparison.Ordinal);
            Assert.True(SupportedProjectReadDistributedFixture.IsOwnedDomainKey(key, "owned", "foreign"));
        }
        Assert.Contains(recording.ReadKeys, key => key == "shared-epoch:" + scope.ComputeHash());
        foreach (string key in recording.ReadKeys.Where(key => key.StartsWith("shared-epoch:", StringComparison.Ordinal)).Concat(new[]
        {
            "shared-receipt:" + scope.ComputeHash() + ":1:ABC", "shared-checkpoint:" + scope.ComputeHash() + ":ABC",
            "shared-chunk:" + scope.ComputeHash() + ":ABC", "shared-control-index:ABC", "shared-control-index-tombstone:ABC",
        })) Assert.False(SupportedProjectReadDistributedFixture.IsOwnedDomainKey(key, "owned", "foreign"));
        var before = business.Select(key => new SupportedProjectReadStateSnapshot(key, [1], "v1")).ToArray();
        Assert.False(SupportedProjectReadDistributedFixture.SameDomainState(before, [before[0] with { Bytes = [2] }, .. before.Skip(1)]));
        Assert.False(SupportedProjectReadDistributedFixture.SameDomainState(before, [before[0] with { ETag = "v2" }, .. before.Skip(1)]));
        Assert.False(SupportedProjectReadDistributedFixture.SameDomainState(before, before.Skip(1).ToArray()));
        _ = await coordinator.ReadGenerationAsync<JsonObject>(scope, 2, lifecycle, TestContext.Current.CancellationToken).ConfigureAwait(true);
        string added = recording.ReadKeys.First(key => key.StartsWith("shared-generation:", StringComparison.Ordinal) && !business.Contains(key, StringComparer.Ordinal));
        Assert.True(SupportedProjectReadDistributedFixture.IsOwnedDomainKey(added, "owned", "foreign"));
        Assert.False(SupportedProjectReadDistributedFixture.SameDomainState(before, [.. before, new(added, [1], "v1")]));
        Assert.Equal(0, recording.WriteCount);
        Assert.Equal(0, backing.Count);
    }

    /// <summary>Closed audit values, types, hash, count, time and sensitive exclusions are all required.</summary>
    [Fact]
    public void AuditPrivacyRequiresClosedValuesAndSafeMetadata()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Dictionary<string, object> valid = new()
        {
            ["requestSurrogate"] = new string('A', 64), ["outcome"] = "Inspection", ["projectCount"] = 2,
            ["fieldClass"] = "ProjectName", ["recordedAt"] = now,
        };
        bool Safe(Dictionary<string, object> value, params string[] exclusions) => SupportedProjectReadDistributedFixture.IsSafeAudit(
            JsonSerializer.SerializeToElement(value), now.AddMinutes(-1), now, exclusions);
        Assert.True(Safe(valid));
        foreach ((string field, object value) in new (string, object)[]
        {
            ("requestSurrogate", "protected-description"), ("requestSurrogate", new string('a', 64)), ("outcome", "ProtectedAlphabeticDescription"),
            ("fieldClass", "Owned folder label"), ("fieldClass", 4), ("projectCount", -1), ("projectCount", 201), ("projectCount", "2"),
            ("recordedAt", now.AddHours(-1)), ("recordedAt", now.AddHours(1)), ("recordedAt", "actor-1"),
        })
        {
            Dictionary<string, object> invalid = new(valid) { [field] = value };
            Assert.False(Safe(invalid), "Invalid audit metadata was accepted.");
        }
        Assert.False(Safe(valid, new string('A', 64)), "A seeded sensitive value was retained in audit metadata.");
        Dictionary<string, object> extra = new(valid) { ["credential"] = "protected-value" };
        Assert.False(Safe(extra), "An extra audit field was accepted.");
        Dictionary<string, object> denial = new(valid) { ["outcome"] = "SafeDenial", ["fieldClass"] = "None", ["projectCount"] = 0 };
        Assert.True(Safe(denial));
        denial["projectCount"] = 1;
        Assert.False(Safe(denial), "Safe denial retained a disclosure count.");
    }

    /// <summary>Real admission component names remain public while each descriptive response path is omitted.</summary>
    [Fact]
    public void NonDescriptiveFaultControlsValidateContractPathsAndPopulatedCompleteResults()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        AdmissionSnapshot snapshot = new(AdmissionResponseState.Complete, now, 2,
            [new("Project", true, EvidenceFreshnessState.Current, "current"), new("Folder", true, EvidenceFreshnessState.Current, "current")], []);
        GetProjectResponse opened = new("project-a", null, null, ProjectLifecycle.Active, now, now, null, null,
            [new("ProjectFolder", "folder-project-a", null, null, null, ReferenceState.Included, null, now)], snapshot);
        JsonElement open = JsonSerializer.SerializeToElement(opened, JsonSerializerOptions.Web);
        Assert.True(open.GetProperty("snapshot").GetProperty("components")[0].TryGetProperty("name", out _), "Positive control omitted public admission component names.");
        Assert.True(SupportedProjectReadDistributedFixture.IsNonDescriptiveControl(open, open: true));
        foreach (string field in new[] { "name", "description", "setup", "setupMetadata" })
            foreach (string? value in new string?[] { null, "protected-descriptive-value" })
            {
                JsonObject invalid = JsonNode.Parse(open.GetRawText())!.AsObject();
                invalid[field] = value;
                Assert.False(SupportedProjectReadDistributedFixture.IsNonDescriptiveControl(JsonSerializer.SerializeToElement(invalid), open: true), "An open descriptive field was accepted.");
            }
        foreach (string? value in new string?[] { null, "protected-reference-label" })
        {
            JsonObject invalid = JsonNode.Parse(open.GetRawText())!.AsObject();
            invalid["references"]![0]!["displayName"] = value;
            Assert.False(SupportedProjectReadDistributedFixture.IsNonDescriptiveControl(JsonSerializer.SerializeToElement(invalid), open: true), "An open reference display name was accepted.");
        }
        ProjectListPage listed = new([new("project-a", null, ProjectLifecycle.Active, ReferenceState.Included, snapshot),
            new("project-b", null, ProjectLifecycle.Active, ReferenceState.Included, snapshot)], null, false, snapshot);
        JsonElement list = JsonSerializer.SerializeToElement(listed, JsonSerializerOptions.Web);
        Assert.True(list.GetProperty("items")[0].GetProperty("snapshot").GetProperty("components")[0].TryGetProperty("name", out _), "Positive list control omitted public admission component names.");
        Assert.True(SupportedProjectReadDistributedFixture.IsNonDescriptiveControl(list, open: false));
        foreach (int index in new[] { 0, 1 })
            foreach (string? value in new string?[] { null, "protected-project-name" })
            {
                JsonObject invalid = JsonNode.Parse(list.GetRawText())!.AsObject();
                invalid["items"]![index]!["name"] = value;
                Assert.False(SupportedProjectReadDistributedFixture.IsNonDescriptiveControl(JsonSerializer.SerializeToElement(invalid), open: false), "A list row name was accepted.");
            }
        Assert.False(SupportedProjectReadDistributedFixture.IsNonDescriptiveControl(JsonSerializer.SerializeToElement(listed with { Items = [] }, JsonSerializerOptions.Web), open: false), "An empty Complete list control qualified.");
        Assert.False(SupportedProjectReadDistributedFixture.IsNonDescriptiveControl(JsonSerializer.SerializeToElement(opened with { Snapshot = snapshot with { ResponseState = AdmissionResponseState.Unavailable } }, JsonSerializerOptions.Web), open: true), "An unavailable ordinary open control qualified.");
        Assert.False(SupportedProjectReadDistributedFixture.IsNonDescriptiveControl(JsonSerializer.SerializeToElement(listed with { Snapshot = snapshot with { ResponseState = AdmissionResponseState.Unavailable } }, JsonSerializerOptions.Web), open: false), "An unavailable ordinary list control qualified.");
    }

    /// <summary>Restart evidence requires both actual server and launcher identities to change after proven exit.</summary>
    [Fact]
    public void RestartEvidenceRejectsMissingExitAndUnchangedActualProcess()
    {
        object Identity(int pid, int launcher, string started) => new
        {
            resourceName = "projects-owned", pid, startTicks = started, launcherPid = launcher, launcherStartTicks = started,
            binarySha256 = new string('a', 64), assemblySha256 = new string('b', 64),
        };
        JsonObject valid = JsonSerializer.SerializeToNode(new
        {
            resource = "projects", action = "restart", completed = true, oldProcessExited = true, endpointsRediscovered = true,
            before = Identity(101, 201, "100"), after = Identity(102, 202, "200"),
        })!.AsObject();
        bool Accept(JsonObject value) => SupportedProjectReadDistributedFixture.IsRestartProcessEvidence(JsonSerializer.SerializeToElement(value));
        Assert.True(Accept(valid));
        foreach (string field in new[] { "before", "after", "completed", "oldProcessExited", "endpointsRediscovered" })
        {
            JsonObject invalid = valid.DeepClone().AsObject();
            invalid.Remove(field);
            Assert.False(Accept(invalid), "Incomplete restart evidence qualified.");
        }
        foreach ((string pid, string ticks) in new[] { ("pid", "startTicks"), ("launcherPid", "launcherStartTicks") })
        {
            JsonObject invalid = valid.DeepClone().AsObject();
            invalid["after"]![pid] = invalid["before"]![pid]!.DeepClone();
            invalid["after"]![ticks] = invalid["before"]![ticks]!.DeepClone();
            Assert.False(Accept(invalid), "An unchanged process qualified as a restart.");
        }
        foreach (string field in new[] { "pid", "startTicks", "launcherPid", "launcherStartTicks", "binarySha256", "assemblySha256", "resourceName" })
        {
            JsonObject invalid = valid.DeepClone().AsObject();
            invalid["after"]!.AsObject().Remove(field);
            Assert.False(Accept(invalid), "Missing process identity qualified.");
        }
        JsonObject changedBinary = valid.DeepClone().AsObject();
        changedBinary["after"]!["assemblySha256"] = new string('c', 64);
        Assert.False(Accept(changedBinary), "A rebuilt or replaced application qualified as an unchanged restart.");
    }

    /// <summary>Fresh observation timestamps and opaque cursors may change; stable metadata, versions, freshness and order may not.</summary>
    [Fact]
    public void RestartComparisonPreservesStableMetadataVersionsFreshnessAndOrdering()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        AdmissionSnapshot snapshot = new(AdmissionResponseState.Complete, now, 2,
            [new("Project", true, EvidenceFreshnessState.Current, "Current") { LastVerifiedAt = now },
             new("Folder", true, EvidenceFreshnessState.Current, "Current") { LastVerifiedAt = now },
             new("Authorization", true, EvidenceFreshnessState.Current, "Current") { LastVerifiedAt = now }], []);
        AdmissionSnapshot observed = snapshot with { AsOf = now.AddSeconds(1), Components = snapshot.Components.Select(component =>
            component.Name == "Project" ? component : component with { LastVerifiedAt = now.AddSeconds(1) }).ToArray() };
        GetProjectResponse opened = new("project-a", "Synthetic", "Description", ProjectLifecycle.Active, now, now, null, null, [], snapshot);
        Assert.True(SupportedProjectReadDistributedFixture.SameStableReadMetadata(opened, opened with { Snapshot = observed }));
        foreach (GetProjectResponse changed in new[]
        {
            opened with { Name = "Changed" }, opened with { Description = "Changed" }, opened with { UpdatedAt = now.AddSeconds(1) },
            opened with { LifecycleState = ProjectLifecycle.Archived }, opened with { SetupMetadata = "Changed" },
            opened with { Snapshot = snapshot with { ProjectVersion = 3 } },
            opened with { Snapshot = snapshot with { Components = [snapshot.Components[0] with { Freshness = EvidenceFreshnessState.Stale }] } },
            opened with { Snapshot = snapshot with { Components = [snapshot.Components[0] with { LastVerifiedAt = now.AddSeconds(1) }, .. snapshot.Components.Skip(1)] } },
            opened with { Snapshot = snapshot with { AsOf = default } },
            opened with { Snapshot = snapshot with { RecoveryActions = ["Retry"] } },
        }) Assert.False(SupportedProjectReadDistributedFixture.SameStableReadMetadata(opened, changed), "Altered stable metadata qualified.");
        ProjectListPage page = new([new("project-a", "A", ProjectLifecycle.Active, ReferenceState.Included, snapshot),
            new("project-b", "B", ProjectLifecycle.Active, ReferenceState.Included, snapshot)], "opaque-before", true, snapshot);
        Assert.True(SupportedProjectReadDistributedFixture.SameStableReadMetadata(page, page with { Cursor = "opaque-after", Snapshot = observed }));
        foreach (ProjectListPage changed in new[] { page with { Items = page.Items.Reverse().ToArray() }, page with { Items = [page.Items[0]] },
            page with { Cursor = null }, page with { HasMore = false }, page with { Items = [page.Items[0] with { Name = "Changed" }, page.Items[1]] } })
            Assert.False(SupportedProjectReadDistributedFixture.SameStableReadMetadata(page, changed), "Altered paging metadata qualified.");
    }

    /// <summary>Neither primary nor foreign domain bytes, exact watermarks, keys or concurrency identities may change.</summary>
    [Fact]
    public void RestartStateComparisonRejectsAlteredStateInEitherTenant()
    {
        SupportedProjectReadStateSnapshot[] before = [new("primary", "{\"globalPosition\":9}"u8.ToArray(), "one"),
            new("foreign", "{\"globalPosition\":17}"u8.ToArray(), "two")];
        Assert.True(SupportedProjectReadDistributedFixture.SameDomainState(before, before.Reverse().ToArray()));
        Assert.False(SupportedProjectReadDistributedFixture.SameDomainState([], []));
        Assert.False(SupportedProjectReadDistributedFixture.SameDomainState(before, [before[0]]));
        Assert.False(SupportedProjectReadDistributedFixture.SameDomainState(before, [.. before, new("new-key", [1], "three")]));
        for (int index = 0; index < before.Length; index++)
            foreach (SupportedProjectReadStateSnapshot altered in new[] { before[index] with { ETag = "changed" }, before[index] with { Key = "replaced" },
                before[index] with { Bytes = "{\"globalPosition\":18}"u8.ToArray() } })
            {
                SupportedProjectReadStateSnapshot[] after = before.ToArray();
                after[index] = altered;
                Assert.False(SupportedProjectReadDistributedFixture.SameDomainState(before, after), "Altered domain state qualified.");
            }
    }

    /// <summary>Failure evidence survives mismatched, missing, malformed and unreadable state without protected payloads.</summary>
    [Fact]
    public async Task RestartFailurePersistsSanitizedObservationsBeforeAssertionAsync()
    {
        string[] tenants = ["primary-secret", "foreign-secret"];
        SupportedProjectReadStateSnapshot[] before = tenants.SelectMany(tenant => new[]
        {
            Hexalith.Projects.Server.Projections.ProjectReads.ProjectReadModelKeys.Index(tenant),
            Hexalith.Projects.Server.Projections.ProjectReads.ProjectReadModelKeys.Detail(tenant, "project-a"),
            Hexalith.Projects.Server.Projections.ProjectReads.ProjectReadModelKeys.Detail(tenant, "project-b"),
        }).Select((key, index) => new SupportedProjectReadStateSnapshot(key,
            JsonSerializer.SerializeToUtf8Bytes(new { globalPosition = index + 1, description = "protected-secret-payload" }), "secret-etag")).ToArray();
        DirectoryInfo directory = Directory.CreateTempSubdirectory("restart-state-control-");
        try
        {
            foreach (string scenario in new[] { "changed", "missing", "malformed", "unreadable" })
            {
                string path = Path.Combine(directory.FullName, scenario + ".json");
                SupportedProjectReadStateSnapshot[] after = before.ToArray();
                if (scenario == "changed") after[0] = after[0] with { ETag = "changed" };
                if (scenario == "missing") after = after.Skip(1).ToArray();
                if (scenario == "malformed") after[0] = after[0] with { Bytes = "["u8.ToArray() };
                await Assert.ThrowsAnyAsync<Exception>(() => SupportedProjectReadDistributedFixture.CaptureRestartStateEvidenceAsync(path, before,
                    () => scenario == "unreadable" ? Task.FromException<SupportedProjectReadStateSnapshot[]>(new IOException("protected-secret-error")) : Task.FromResult(after), tenants));
                string text = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
                Assert.DoesNotContain("secret", text, StringComparison.Ordinal);
                using JsonDocument evidence = JsonDocument.Parse(text);
                Assert.False(evidence.RootElement.GetProperty("domainKeysBytesEtagsAndWatermarksUnchanged").GetBoolean());
                Assert.Equal(6, evidence.RootElement.GetProperty("before").GetArrayLength());
                Assert.Equal(scenario == "changed" ? "observed" : scenario == "missing" ? "missing" : "unreadable",
                    evidence.RootElement.GetProperty("afterObservation").GetString());
                if (scenario == "missing") Assert.Single(evidence.RootElement.GetProperty("missingKeys").EnumerateArray());
                if (scenario == "unreadable") Assert.Equal(JsonValueKind.Null, evidence.RootElement.GetProperty("after").ValueKind);
            }
        }
        finally { directory.Delete(recursive: true); }
    }

    /// <summary>Transient network/request failures recover through the real predicate and never disclose exception details.</summary>
    [Fact]
    public async Task RecoveryRetriesTransportAndRequestFailuresThenRequiresAuthorizedResponseAsync()
    {
        DirectoryInfo directory = Directory.CreateTempSubdirectory("restart-recovery-control-");
        try
        {
            int calls = 0;
            Task<System.Net.Http.HttpResponseMessage> Query(CancellationToken _) => ++calls switch
            {
                1 => Task.FromException<System.Net.Http.HttpResponseMessage>(new HttpRequestException("protected-secret-token")),
                2 => Task.FromException<System.Net.Http.HttpResponseMessage>(new TaskCanceledException("protected-secret-payload")),
                3 => Task.FromResult(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)),
                _ => Task.FromResult(AuthorizedRecoveryResponse()),
            };
            string path = Path.Combine(directory.FullName, "recovery.log");
            await SupportedProjectReadDistributedFixture.ObserveAuthorizedRecoveryAsync("projects-process", Query, path,
                TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(1), TestContext.Current.CancellationToken);
            string diagnostic = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
            Assert.Equal(4, calls);
            Assert.Contains("transportFailures=2", diagnostic);
            Assert.Contains("authorizedRecovery=True", diagnostic);
            Assert.DoesNotContain("secret", diagnostic, StringComparison.Ordinal);
        }
        finally { directory.Delete(recursive: true); }
    }

    /// <summary>A short test budget bounds persistent failures and a request which ignores cancellation.</summary>
    [Theory]
    [InlineData("transport")]
    [InlineData("request-timeout")]
    [InlineData("unresponsive")]
    [InlineData("server")]
    public async Task RecoveryFailureIsBoundedAndLogsOnlyMetadataAsync(string scenario)
    {
        DirectoryInfo directory = Directory.CreateTempSubdirectory("restart-recovery-control-");
        try
        {
            TaskCompletionSource<System.Net.Http.HttpResponseMessage> unresponsive = new();
            Task<System.Net.Http.HttpResponseMessage> Query(CancellationToken _) => scenario switch
            {
                "transport" => Task.FromException<System.Net.Http.HttpResponseMessage>(new HttpRequestException("protected-secret-token")),
                "request-timeout" => Task.FromException<System.Net.Http.HttpResponseMessage>(new TaskCanceledException("protected-secret-token")),
                "unresponsive" => unresponsive.Task,
                _ => Task.FromResult(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
                    { Content = new StringContent("protected-secret-payload") }),
            };
            string path = Path.Combine(directory.FullName, "recovery.log");
            System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
            await Assert.ThrowsAsync<InvalidOperationException>(() => SupportedProjectReadDistributedFixture.ObserveAuthorizedRecoveryAsync(
                "projects-process", Query, path, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(1), TestContext.Current.CancellationToken));
            Assert.True(timer.Elapsed < TimeSpan.FromSeconds(2), "Recovery exceeded its short control budget.");
            string diagnostic = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
            Assert.Contains("authorizedRecovery=False", diagnostic);
            Assert.DoesNotContain("secret", diagnostic, StringComparison.Ordinal);
        }
        finally { directory.Delete(recursive: true); }
    }

    /// <summary>Caller cancellation is preserved while recording unsuccessful recovery diagnostics.</summary>
    [Fact]
    public async Task RecoveryCancellationIsNotReclassifiedAsSuccessOrDeadlineAsync()
    {
        DirectoryInfo directory = Directory.CreateTempSubdirectory("restart-recovery-control-");
        try
        {
            using CancellationTokenSource cancellation = new(TimeSpan.FromMilliseconds(50));
            string path = Path.Combine(directory.FullName, "recovery.log");
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => SupportedProjectReadDistributedFixture.ObserveAuthorizedRecoveryAsync(
                "projects-process", async token => { await Task.Delay(Timeout.InfiniteTimeSpan, token).ConfigureAwait(false); return AuthorizedRecoveryResponse(); }, path,
                TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(1), cancellation.Token));
            Assert.Contains("authorizedRecovery=False", await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        }
        finally { directory.Delete(recursive: true); }
    }

    /// <summary>Authentication and malformed-request refusals stop immediately rather than retrying until the deadline.</summary>
    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    public async Task RecoveryUnexpectedAuthenticationRefusesImmediatelyAsync(int status)
    {
        DirectoryInfo directory = Directory.CreateTempSubdirectory("restart-recovery-control-");
        try
        {
            int calls = 0;
            string path = Path.Combine(directory.FullName, "recovery.log");
            Task<System.Net.Http.HttpResponseMessage> Query(CancellationToken _)
            {
                calls++;
                return Task.FromResult(new System.Net.Http.HttpResponseMessage((System.Net.HttpStatusCode)status)
                    { Content = new StringContent("protected-secret-payload") });
            }
            await Assert.ThrowsAsync<InvalidOperationException>(() => SupportedProjectReadDistributedFixture.ObserveAuthorizedRecoveryAsync(
                "projects-process", Query, path, TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(1), TestContext.Current.CancellationToken));
            Assert.Equal(1, calls);
            string diagnostic = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
            Assert.Contains("authorizedRecovery=False", diagnostic);
            Assert.DoesNotContain("secret", diagnostic, StringComparison.Ordinal);
        }
        finally { directory.Delete(recursive: true); }
    }

    /// <summary>Cancelling a fixture helper terminates and awaits its child while preserving an unrelated process.</summary>
    [Fact]
    public async Task HelperCancellationTerminatesOnlyItsOwnedProcessTreeAsync()
    {
        DirectoryInfo directory = Directory.CreateTempSubdirectory("restart-process-control-");
        using System.Diagnostics.Process unrelated = new() { StartInfo = new("python3") };
        unrelated.StartInfo.ArgumentList.Add("-c");
        unrelated.StartInfo.ArgumentList.Add("import time;time.sleep(30)");
        unrelated.Start();
        try
        {
            string path = Path.Combine(directory.FullName, "pids");
            const string code = "import os,subprocess,sys,time; child=subprocess.Popen([sys.executable,'-c','import time;time.sleep(30)']);open(sys.argv[1],'w').write(str(os.getpid())+' '+str(child.pid));time.sleep(30)";
            using CancellationTokenSource cancellation = new();
            Task<byte[]> helper = SupportedProjectReadDistributedFixture.RunOwnedProcessAsync("python3", ["-c", code, path], cancellation.Token);
            using CancellationTokenSource readyBudget = new(TimeSpan.FromSeconds(5));
            try
            {
                while (!File.Exists(path)) await Task.Delay(10, readyBudget.Token);
                cancellation.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => helper);
                foreach (string pid in (await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken)).Split(' '))
                {
                    string stat = $"/proc/{pid}/stat";
                    Assert.True(!File.Exists(stat) || File.ReadAllText(stat).Split(')')[1].TrimStart().StartsWith('Z'), "Owned helper remained alive after cancellation.");
                }
                Assert.False(unrelated.HasExited);
            }
            finally
            {
                cancellation.Cancel();
                try { await helper.ConfigureAwait(true); } catch (OperationCanceledException) { }
            }
        }
        finally { unrelated.Kill(); await unrelated.WaitForExitAsync(TestContext.Current.CancellationToken); directory.Delete(recursive: true); }
    }

    /// <summary>The conservative request-start bound cannot accept rejection after the unchanged cursor lifetime.</summary>
    [Fact]
    public void CursorRejectionMustPrecedeItsObservedLifetimeBound()
    {
        DateTimeOffset requested = DateTimeOffset.UtcNow;
        Assert.True(SupportedProjectReadDistributedFixture.CursorIsWithinObservedLifetime(requested, requested.AddMinutes(14)));
        Assert.False(SupportedProjectReadDistributedFixture.CursorIsWithinObservedLifetime(requested, requested.AddMinutes(15)));
        Assert.False(SupportedProjectReadDistributedFixture.CursorIsWithinObservedLifetime(requested, requested.AddMinutes(16)));
        Assert.False(SupportedProjectReadDistributedFixture.CursorIsWithinObservedLifetime(requested, requested.AddSeconds(-1)));
    }

    /// <summary>Replay cannot use an incomplete prefix, a substituted identity or inconsistent source metadata.</summary>
    [Fact]
    public void CompleteReplaySourceRejectsMissingTruncatedAndInconsistentHistory()
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        Hexalith.EventStore.Contracts.Streams.StreamReadEvent first = new(1, "Created", "{}"u8.ToArray(), "json", 1, "first", "correlation", null, timestamp, "actor") { GlobalPosition = 11 };
        Hexalith.EventStore.Contracts.Streams.StreamReadEvent second = first with { SequenceNumber = 2, MessageId = "second", GlobalPosition = 15, Timestamp = timestamp.AddSeconds(1) };
        Hexalith.EventStore.Contracts.Streams.StreamReadRequest request = new("owned", "projects", "project-a");
        Hexalith.EventStore.Contracts.Streams.StreamReadPage source = new("owned", "projects", "project-a", [first, second], new(0, null, 2, 2, 2, false, null));
        Assert.True(SupportedProjectReadDistributedFixture.IsCompleteProjectStream(request, source));
        foreach (Hexalith.EventStore.Contracts.Streams.StreamReadPage invalid in new[]
        {
            source with { Tenant = "foreign" }, source with { Domain = "folders" }, source with { AggregateId = "project-b" },
            source with { Events = [] }, source with { Events = [first] }, source with { Events = [second] },
            source with { Events = [first, second with { SequenceNumber = 3 }] },
            source with { Events = [first, second with { GlobalPosition = 0 }] }, source with { Events = [first, second with { GlobalPosition = 11 }] },
            source with { Events = [first, second with { Timestamp = default }] }, source with { Events = [first, second with { MessageId = first.MessageId }] },
            source with { Metadata = source.Metadata with { FromSequence = 1 } }, source with { Metadata = source.Metadata with { ToSequence = 2 } },
            source with { Metadata = source.Metadata with { LastSequenceReturned = 1 } }, source with { Metadata = source.Metadata with { LatestSequence = 3 } },
            source with { Metadata = source.Metadata with { LatestSequence = 1 } }, source with { Metadata = source.Metadata with { EventCount = 1 } },
            source with { Metadata = source.Metadata with { IsTruncated = true } },
        }) Assert.False(SupportedProjectReadDistributedFixture.IsCompleteProjectStream(request, invalid), "Incomplete or inconsistent source evidence qualified.");
        Assert.False(SupportedProjectReadDistributedFixture.IsCompleteProjectStream(request with { FromSequence = 1 }, source));
    }

    /// <summary>Only the named target detail and tenant index ETags may change; both tenants' bytes and watermarks remain exact.</summary>
    [Fact]
    public void ReplayComparisonRejectsForbiddenWritesAndAlteredWatermarksInBothTenants()
    {
        string[] tenants = ["primary-secret", "foreign-secret"];
        SupportedProjectReadStateSnapshot[] before = ReplayState(tenants);
        string[] expected = [before[0].Key, before[1].Key];
        SupportedProjectReadStateSnapshot[] after = before.Select((item, index) => index < 2 ? item with { ETag = "new-secret-etag" } : item).ToArray();
        bool Valid(SupportedProjectReadStateSnapshot[]? values, string[]? allowed = null) => SupportedProjectReadDistributedFixture.CreateReplayStateEvidence(before, values, tenants, allowed ?? expected, before.Select(item => item.Key).ToArray()).GetProperty("valid").GetBoolean();
        Assert.True(Valid(after));
        Assert.True(SupportedProjectReadDistributedFixture.CreateReplayStateEvidence(before, before, tenants, [], before.Select(item => item.Key).ToArray()).GetProperty("valid").GetBoolean());
        Assert.False(Valid(before), "A rebuild with no ETag change qualified.");
        Assert.False(Valid(null));
        Assert.False(Valid([]));
        Assert.False(Valid(after.Skip(1).ToArray()));
        Assert.False(Valid([.. after, new("new-secret-key", [1], "secret-etag")]));
        Assert.False(Valid([.. after.Skip(1), after[1]]), "Duplicate state observations qualified.");
        Assert.False(Valid(after, [before[0].Key]));
        SupportedProjectReadStateSnapshot[] projectionsOnly = before.Take(6).ToArray();
        Assert.False(SupportedProjectReadDistributedFixture.CreateReplayStateEvidence(projectionsOnly, projectionsOnly, tenants, [],
            before.Select(item => item.Key).ToArray()).GetProperty("valid").GetBoolean(), "Consistently omitted non-projection domain keys qualified.");
        for (int index = 0; index < before.Length; index++)
            foreach (SupportedProjectReadStateSnapshot changed in new[]
            {
                after[index] with { Bytes = "{\"globalPosition\":999,\"description\":\"protected-secret\"}"u8.ToArray() },
                after[index] with { Bytes = "{\"globalPosition\":0}"u8.ToArray() }, after[index] with { Bytes = "["u8.ToArray() },
                after[index] with { Bytes = [] }, after[index] with { ETag = "" }, after[index] with { Key = "replaced-secret-key" },
            })
            {
                SupportedProjectReadStateSnapshot[] invalid = after.ToArray();
                invalid[index] = changed;
                Assert.False(Valid(invalid), "Forbidden state changes or missing watermarks qualified.");
            }
        foreach (int index in Enumerable.Range(2, before.Length - 2))
        {
            SupportedProjectReadStateSnapshot[] invalid = after.ToArray();
            invalid[index] = invalid[index] with { ETag = "unexpected-secret-etag" };
            Assert.False(Valid(invalid), "A sibling or foreign tenant ETag write qualified.");
        }
        Assert.False(SupportedProjectReadDistributedFixture.CreateReplayStateEvidence(before, after, [tenants[0]], expected, before.Select(item => item.Key).ToArray()).GetProperty("valid").GetBoolean());
        Assert.False(SupportedProjectReadDistributedFixture.CreateReplayStateEvidence(before, after, [tenants[0], tenants[0]], expected, before.Select(item => item.Key).ToArray()).GetProperty("valid").GetBoolean());
    }

    /// <summary>Incomplete replay evidence remains explicit and sanitized without retaining keys, ETags, metadata or error bodies.</summary>
    [Fact]
    public void ReplayStateEvidenceRetainsExactWatermarksAndSanitizedFailureObservations()
    {
        string[] tenants = ["primary-secret", "foreign-secret"];
        SupportedProjectReadStateSnapshot[] before = ReplayState(tenants);
        foreach (SupportedProjectReadStateSnapshot[]? after in new SupportedProjectReadStateSnapshot[]?[] { before, null, before.Skip(1).ToArray() })
        {
            JsonElement evidence = SupportedProjectReadDistributedFixture.CreateReplayStateEvidence(before, after, tenants, [], before.Select(item => item.Key).ToArray());
            Assert.DoesNotContain("secret", evidence.GetRawText(), StringComparison.Ordinal);
            Assert.Equal(after == before, evidence.GetProperty("valid").GetBoolean());
            Assert.Equal(6, evidence.GetProperty("beforeWatermarks").GetArrayLength());
            Assert.Equal(6, evidence.GetProperty("afterWatermarks").GetArrayLength());
            Assert.Equal(Enumerable.Range(1, 6).Select(value => (long)value), evidence.GetProperty("beforeWatermarks").EnumerateArray().Select(value => value.GetProperty("globalPosition").GetInt64()));
        }
    }

    /// <summary>A failed cursor page retains AD-32 diagnostics without payloads, identifiers, token bytes or unrecognized metadata.</summary>
    [Fact]
    public void CursorControlDiagnosticsRetainUnavailableStateWithoutProtectedPayloads()
    {
        AdmissionSnapshot snapshot = new(AdmissionResponseState.Unavailable, DateTimeOffset.UtcNow, 0,
            [new("FolderVisibility", false, EvidenceFreshnessState.Unavailable, "FolderUnavailable"),
             new("secret-component", false, EvidenceFreshnessState.Unavailable, "secret-reason")], []);
        ProjectListPage page = new([new("secret-project", "secret-name", ProjectLifecycle.Active, ReferenceState.Included, snapshot)], "secret-token", false, snapshot);
        JsonElement evidence = SupportedProjectReadDistributedFixture.CreateCursorControlEvidence(page);
        Assert.DoesNotContain("secret", evidence.GetRawText(), StringComparison.Ordinal);
        Assert.Equal("Unavailable", evidence.GetProperty("responseState").GetString());
        Assert.False(evidence.GetProperty("hasMore").GetBoolean());
        Assert.True(evidence.GetProperty("cursorPresent").GetBoolean());
        Assert.Equal("FolderVisibility", evidence.GetProperty("components")[0].GetProperty("name").GetString());
        Assert.Equal("FolderUnavailable", evidence.GetProperty("components")[0].GetProperty("reason").GetString());
        Assert.Equal("Other", evidence.GetProperty("components")[1].GetProperty("name").GetString());
        Assert.Equal("Other", evidence.GetProperty("components")[1].GetProperty("reason").GetString());
    }

    /// <summary>Honest empty Unavailable observations are retained until a definitive refusal; they never establish acceptance.</summary>
    [Fact]
    public async Task CursorObserverRetainsUnavailableAttemptsBeforeDefinitiveRefusalAsync()
    {
        List<JsonElement> observations = [];
        int calls = 0;
        await SupportedProjectReadDistributedFixture.ObserveRebuildCursorInvalidationAsync(_ => Task.FromResult(++calls < 3
            ? CursorResponse(AdmissionResponseState.Unavailable)
            : InvalidCursorResponse()),
            (value, _) => { observations.Add(value); return Task.CompletedTask; }, DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        Assert.Equal(3, calls);
        Assert.Equal("query_invalid_page", observations[^1].GetProperty("problem").GetProperty("reasonCode").GetString());
        Assert.Equal("invalid-cursor", observations[^1].GetProperty("problem").GetProperty("reason").GetString());
        Assert.True(observations[^1].GetProperty("observerDeadline").GetDateTimeOffset()
            - observations[^1].GetProperty("observerStartedAt").GetDateTimeOffset() <= TimeSpan.FromSeconds(90));
        Assert.Equal(new[] { 200, 200, 400 }, observations.Select(value => value.GetProperty("httpStatus").GetInt32()));
        Assert.All(observations.Take(2), value => Assert.True(value.GetProperty("honestEmptyUnavailable").GetBoolean()));
        Assert.DoesNotContain("secret", JsonSerializer.Serialize(observations), StringComparison.Ordinal);
    }

    /// <summary>Complete/Partial continuations and populated or cursor-bearing Unavailable pages fail immediately.</summary>
    [Theory]
    [InlineData("complete")]
    [InlineData("partial")]
    [InlineData("populated")]
    [InlineData("cursor")]
    [InlineData("has-more")]
    [InlineData("transport")]
    public async Task CursorObserverNeverRetriesAcceptedOrDishonestResponsesAsync(string scenario)
    {
        List<JsonElement> observations = [];
        int calls = 0;
        Task<System.Net.Http.HttpResponseMessage> Query(CancellationToken _)
        {
            calls++;
            return scenario == "transport" ? Task.FromException<System.Net.Http.HttpResponseMessage>(new HttpRequestException("secret-error"))
                : Task.FromResult(CursorResponse(scenario == "complete" ? AdmissionResponseState.Complete : scenario == "partial" ? AdmissionResponseState.Partial : AdmissionResponseState.Unavailable,
                    scenario == "populated", scenario == "cursor", scenario == "has-more"));
        }
        await Assert.ThrowsAnyAsync<Exception>(() => SupportedProjectReadDistributedFixture.ObserveRebuildCursorInvalidationAsync(Query,
            (value, _) => { observations.Add(value); return Task.CompletedTask; }, DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
        Assert.Equal(1, calls);
        Assert.Single(observations);
        Assert.False(observations[0].GetProperty("honestEmptyUnavailable").GetBoolean());
        Assert.DoesNotContain("secret", JsonSerializer.Serialize(observations), StringComparison.Ordinal);
    }

    /// <summary>Unavailable-only observations cannot pass on deadline or after the original cursor lifetime.</summary>
    [Fact]
    public async Task CursorObserverRequiresRefusalWithinBothBudgetsAsync()
    {
        foreach (bool expired in new[] { false, true })
        {
            List<JsonElement> observations = [];
            await Assert.ThrowsAnyAsync<Exception>(() => SupportedProjectReadDistributedFixture.ObserveRebuildCursorInvalidationAsync(
                _ => Task.FromResult(expired ? InvalidCursorResponse() : CursorResponse(AdmissionResponseState.Unavailable)),
                (value, _) => { observations.Add(value); return Task.CompletedTask; }, DateTimeOffset.UtcNow.AddMinutes(expired ? -16 : 0),
                TimeSpan.FromMilliseconds(30), TestContext.Current.CancellationToken));
            Assert.NotEmpty(observations);
        }
    }

    /// <summary>The new observer propagates caller cancellation and retains the failed attempt without error payloads.</summary>
    [Fact]
    public async Task CursorObserverPreservesCallerCancellationAsync()
    {
        List<JsonElement> observations = [];
        using CancellationTokenSource cancellation = new(TimeSpan.FromMilliseconds(50));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => SupportedProjectReadDistributedFixture.ObserveRebuildCursorInvalidationAsync(
            async token => { await Task.Delay(Timeout.InfiniteTimeSpan, token).ConfigureAwait(false); return CursorResponse(AdmissionResponseState.Unavailable); },
            (value, _) => { observations.Add(value); return Task.CompletedTask; }, DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5), cancellation.Token));
        Assert.Single(observations);
        Assert.Equal(JsonValueKind.Null, observations[0].GetProperty("httpStatus").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, observations[0].GetProperty("failureType").ValueKind);
    }

    /// <summary>A stalled query which ignores cancellation cannot extend the observer's whole-operation budget.</summary>
    [Fact]
    public async Task CursorObserverBoundsAStalledQueryAsync()
    {
        List<JsonElement> observations = [];
        TaskCompletionSource<System.Net.Http.HttpResponseMessage> stalled = new();
        System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
        await Assert.ThrowsAsync<InvalidOperationException>(() => SupportedProjectReadDistributedFixture.ObserveRebuildCursorInvalidationAsync(
            _ => stalled.Task, (value, _) => { observations.Add(value); return Task.CompletedTask; }, DateTimeOffset.UtcNow,
            TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken));
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(2), "The stalled cursor query exceeded its short control budget.");
        Assert.Single(observations);
        Assert.Equal(JsonValueKind.Null, observations[0].GetProperty("httpStatus").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, observations[0].GetProperty("failureType").ValueKind);
    }

    /// <summary>Only the actual safe platform Problem Details classification proves cursor invalidation.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("[")]
    [InlineData("{}")]
    [InlineData("{\"status\":400,\"reasonCode\":\"query_invalid_page\",\"reason\":\"invalid-page\"}")]
    [InlineData("{\"status\":400,\"reasonCode\":\"query_malformed_request\",\"reason\":\"invalid-cursor\"}")]
    [InlineData("{\"status\":\"400\",\"reasonCode\":\"query_invalid_page\",\"reason\":\"invalid-cursor\"}")]
    public async Task CursorObserverRejectsUnclassifiedAndMalformedBadRequestsAsync(string body)
    {
        List<JsonElement> observations = [];
        await Assert.ThrowsAsync<InvalidOperationException>(() => SupportedProjectReadDistributedFixture.ObserveRebuildCursorInvalidationAsync(
            _ => Task.FromResult(InvalidCursorResponse(body)), (value, _) => { observations.Add(value); return Task.CompletedTask; },
            DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
        Assert.Single(observations);
        Assert.Equal(JsonValueKind.Null, observations[0].GetProperty("problem").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, observations[0].GetProperty("failureType").ValueKind);
    }

    /// <summary>Even a valid refusal cannot qualify when asynchronous evidence recording stalls beyond the observer budget.</summary>
    [Fact]
    public async Task CursorObserverBoundsStalledEvidenceRecordingAsync()
    {
        TaskCompletionSource stalled = new();
        System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
        await Assert.ThrowsAsync<InvalidOperationException>(() => SupportedProjectReadDistributedFixture.ObserveRebuildCursorInvalidationAsync(
            _ => Task.FromResult(InvalidCursorResponse()), (_, token) => { Assert.True(token.CanBeCanceled); return stalled.Task; },
            DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken));
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(2), "Stalled evidence recording extended the observer budget.");
    }

    /// <summary>Cancellation or elapsed observer time during recording is rechecked before reporting success.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CursorObserverRejectsLateOrCancelledSuccessAsync(bool cancelled)
    {
        using CancellationTokenSource cancellation = new();
        Task Observe() => SupportedProjectReadDistributedFixture.ObserveRebuildCursorInvalidationAsync(
            _ => Task.FromResult(InvalidCursorResponse()), (_, _) =>
            {
                if (cancelled) cancellation.Cancel();
                else Thread.Sleep(100);
                return Task.CompletedTask;
            }, DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(50), cancellation.Token);
        if (cancelled) await Assert.ThrowsAnyAsync<OperationCanceledException>(Observe);
        else await Assert.ThrowsAsync<InvalidOperationException>(Observe);
    }

    private static System.Net.Http.HttpResponseMessage InvalidCursorResponse(string body = "{\"status\":400,\"reasonCode\":\"query_invalid_page\",\"reason\":\"invalid-cursor\",\"detail\":\"secret-not-retained\"}")
        => new(System.Net.HttpStatusCode.BadRequest) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/problem+json") };

    private static System.Net.Http.HttpResponseMessage CursorResponse(AdmissionResponseState state, bool populated = false, bool cursor = false, bool hasMore = false)
    {
        AdmissionSnapshot snapshot = new(state, DateTimeOffset.UtcNow, 0,
            [new("FolderVisibility", false, EvidenceFreshnessState.Unavailable, "FolderUnavailable")], []);
        ProjectListPage page = new(populated ? [new("secret-project", "secret-name", ProjectLifecycle.Active, ReferenceState.Included, snapshot)] : [],
            cursor ? "secret-cursor" : null, hasMore, snapshot);
        return new(System.Net.HttpStatusCode.OK) { Content = System.Net.Http.Json.JsonContent.Create(
            new Hexalith.EventStore.Contracts.Queries.SubmitQueryResponse("correlation", JsonSerializer.SerializeToElement(page, JsonSerializerOptions.Web))) };
    }

    /// <summary>Batch decoding preserves binary/escaping boundaries and refuses missing, duplicate or malformed provider observations.</summary>
    [Fact]
    public void ProviderBatchDecodingPreservesExactBytesAndVersionsAndRejectsMissingEvidence()
    {
        string[] keys = ["owned||first", "owned||second"];
        byte[] binary = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();
        JsonArray rows = JsonSerializer.SerializeToNode(new[]
        {
            new { key = keys[0], exists = true, dataHex = Convert.ToHexString(binary), version = "9223372036854775807" },
            new { key = keys[1], exists = true, dataHex = "", version = "0002" },
        })!.AsArray();
        SupportedProjectReadStateSnapshot[] Parse(JsonNode value) => SupportedProjectReadDistributedFixture.ParseRawStateBatch(JsonSerializer.SerializeToUtf8Bytes(value), keys);
        SupportedProjectReadStateSnapshot[] actual = Parse(rows);
        Assert.Equal(binary, actual[0].Bytes);
        Assert.Equal("9223372036854775807", actual[0].ETag);
        Assert.Empty(actual[1].Bytes);
        Assert.Equal("0002", actual[1].ETag);
        foreach ((string field, object value) in new (string, object)[]
        {
            ("key", "foreign||key"), ("key", keys[1]), ("exists", false), ("dataHex", false), ("dataHex", "0"),
            ("dataHex", "GG"), ("version", false), ("version", "0"), ("version", "-1"), ("version", "1.5"), ("version", "9223372036854775808"),
        })
        {
            JsonArray invalid = rows.DeepClone().AsArray();
            invalid[0]![field] = JsonSerializer.SerializeToNode(value);
            Assert.ThrowsAny<Exception>(() => Parse(invalid));
        }
        foreach (string field in new[] { "key", "exists", "dataHex", "version" })
        {
            JsonArray invalid = rows.DeepClone().AsArray();
            invalid[0]!.AsObject().Remove(field);
            Assert.ThrowsAny<Exception>(() => Parse(invalid));
        }
        Assert.ThrowsAny<Exception>(() => Parse(new JsonArray(rows[0]!.DeepClone())));
    }

    private static SupportedProjectReadStateSnapshot[] ReplayState(string[] tenants)
        => tenants.SelectMany(tenant => new[]
        {
            Hexalith.Projects.Server.Projections.ProjectReads.ProjectReadModelKeys.Index(tenant),
            Hexalith.Projects.Server.Projections.ProjectReads.ProjectReadModelKeys.Detail(tenant, "project-a"),
            Hexalith.Projects.Server.Projections.ProjectReads.ProjectReadModelKeys.Detail(tenant, "project-b"),
        }).Select((key, index) => new SupportedProjectReadStateSnapshot(key,
            JsonSerializer.SerializeToUtf8Bytes(new { globalPosition = index + 1, description = "protected-secret-payload" }), "secret-etag"))
            .Concat(tenants.Select(tenant => new SupportedProjectReadStateSnapshot("journal:" + tenant, "protected-secret-journal"u8.ToArray(), "secret-etag"))).ToArray();

    private static System.Net.Http.HttpResponseMessage AuthorizedRecoveryResponse()
    {
        AdmissionSnapshot snapshot = new(AdmissionResponseState.Complete, DateTimeOffset.UtcNow, 2, [], []);
        GetProjectResponse opened = new("project-a", "protected-secret-name", null, ProjectLifecycle.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null, [], snapshot);
        var response = new Hexalith.EventStore.Contracts.Queries.SubmitQueryResponse("correlation", JsonSerializer.SerializeToElement(opened, JsonSerializerOptions.Web));
        return new(System.Net.HttpStatusCode.OK) { Content = System.Net.Http.Json.JsonContent.Create(response) };
    }

    /// <summary>Storage access remains bound to the creation ID even if its reusable name is replaced.</summary>
    [Fact]
    public void RedisIdentityRefusesReplacementAndForeignOwnership()
    {
        string expected = new('a', 64);
        Assert.True(SupportedProjectReadDistributedFixture.IsOwnedRedisIdentity(JsonSerializer.Serialize(new { id = expected, owner = "scope" }), expected, "scope"));
        Assert.False(SupportedProjectReadDistributedFixture.IsOwnedRedisIdentity(JsonSerializer.Serialize(new { id = new string('b', 64), owner = "scope" }), expected, "scope"));
        Assert.False(SupportedProjectReadDistributedFixture.IsOwnedRedisIdentity(JsonSerializer.Serialize(new { id = expected, owner = "foreign" }), expected, "scope"));
        Assert.False(SupportedProjectReadDistributedFixture.IsOwnedRedisIdentity("{}", expected, "scope"));
    }
}
#endif
