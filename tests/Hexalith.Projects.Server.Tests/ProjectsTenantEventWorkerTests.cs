// <copyright file="ProjectsTenantEventWorkerTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using System.Linq;
using System.Threading.Tasks;

using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Projects.Infrastructure;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Workers;
using Hexalith.Projects.Workers.Tenants.TenantEventHandlers;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shouldly;

using Xunit;

/// <summary>
/// Tier-2 worker-boundary tests for the Story 1.6 Tenants event subscription wiring.
/// </summary>
public sealed class ProjectsTenantEventWorkerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void WorkersModuleShouldExposeStableTenantSubscriptionMetadata()
    {
        ProjectsWorkersModule.Name.ShouldBe("Hexalith.Projects.Workers");
        ProjectsWorkersModule.AppId.ShouldBe("projects-workers");
        ProjectsWorkersModule.TenantEventsRoute.ShouldBe("/tenants/events");
        ProjectsWorkersModule.TenantEventsPubSubName.ShouldBe("pubsub");
        ProjectsWorkersModule.TenantEventsTopicName.ShouldBe("system.tenants.events");
    }

    [Fact]
    public void AddProjectsTenantEventWorkersShouldRegisterSupportedTenantHandlersAndOptions()
    {
        ServiceCollection services = CreateServiceCollection();
        services.AddProjectsTenantEventWorkers();

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ProjectTenantAccessHandler>().ShouldNotBeNull();
        provider.GetRequiredService<IProjectTenantAccessProjectionStore>().ShouldNotBeNull();
        provider.GetRequiredService<IOptions<ProjectTenantEventOptions>>().Value.ProjectionWriter.ShouldBe(ProjectTenantEventProjectionWriter.Workers);
        provider.GetRequiredService<IOptions<EventStoreDomainEventsOptions>>().Value.PubSubName.ShouldBe(ProjectsWorkersModule.TenantEventsPubSubName);
        provider.GetRequiredService<IOptions<EventStoreDomainEventsOptions>>().Value.TopicName.ShouldBe(ProjectsWorkersModule.TenantEventsTopicName);
        provider.GetRequiredService<IOptions<EventStoreDomainEventsOptions>>().Value.SubscriptionRoute.ShouldBe(ProjectsWorkersModule.TenantEventsRoute);

        provider.GetServices<IEventStoreDomainEventHandler<TenantAccessRefreshed>>().OfType<ProjectsTenantEventHandler>().ShouldHaveSingleItem();
        provider.GetServices<IEventStoreDomainEventHandler<TenantCreated>>().OfType<ProjectsTenantEventHandler>().ShouldHaveSingleItem();
        provider.GetServices<IEventStoreDomainEventHandler<TenantUpdated>>().OfType<ProjectsTenantEventHandler>().ShouldHaveSingleItem();
        provider.GetServices<IEventStoreDomainEventHandler<TenantDisabled>>().OfType<ProjectsTenantEventHandler>().ShouldHaveSingleItem();
        provider.GetServices<IEventStoreDomainEventHandler<TenantEnabled>>().OfType<ProjectsTenantEventHandler>().ShouldHaveSingleItem();
        provider.GetServices<IEventStoreDomainEventHandler<UserAddedToTenant>>().OfType<ProjectsTenantEventHandler>().ShouldHaveSingleItem();
        provider.GetServices<IEventStoreDomainEventHandler<UserRemovedFromTenant>>().OfType<ProjectsTenantEventHandler>().ShouldHaveSingleItem();
        provider.GetServices<IEventStoreDomainEventHandler<UserRoleChanged>>().OfType<ProjectsTenantEventHandler>().ShouldHaveSingleItem();
        provider.GetServices<IEventStoreDomainEventHandler<TenantConfigurationSet>>().OfType<ProjectsTenantEventHandler>().ShouldHaveSingleItem();
        provider.GetServices<IEventStoreDomainEventHandler<TenantConfigurationRemoved>>().OfType<ProjectsTenantEventHandler>().ShouldHaveSingleItem();
    }

    [Fact]
    public async Task WorkerHandlerShouldProjectLifecycleMembershipAndProjectsConfiguration()
    {
        InMemoryProjectTenantAccessProjectionStore store = new();
        ProjectsTenantEventHandler subject = CreateWorkerHandler(store);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await subject.HandleAsync(new TenantCreated("tenant-a", "Tenant A", null, Now), Context(1), cancellationToken);
        await subject.HandleAsync(new UserAddedToTenant("tenant-a", "user-a", TenantRole.TenantReader), Context(2), cancellationToken);
        await subject.HandleAsync(new UserRoleChanged("tenant-a", "user-a", TenantRole.TenantReader, TenantRole.TenantOwner), Context(3), cancellationToken);
        await subject.HandleAsync(new TenantConfigurationSet("tenant-a", "billing.secret", "secret-value"), Context(4), cancellationToken);
        await subject.HandleAsync(new TenantConfigurationSet("tenant-a", "projects.create.enabled", "secret-value"), Context(5), cancellationToken);
        await subject.HandleAsync(new TenantConfigurationRemoved("tenant-a", "projects.create.enabled"), Context(6), cancellationToken);
        await subject.HandleAsync(new TenantDisabled("tenant-a", Now), Context(7), cancellationToken);

        ProjectTenantAccessProjection? projection = await store.GetAsync("tenant-a", cancellationToken);

        projection.ShouldNotBeNull();
        projection.Enabled.ShouldBeFalse();
        projection.Principals["user-a"].Role.ShouldBe(nameof(TenantRole.TenantOwner));
        projection.ConfigurationKeys.ShouldNotContain("billing.secret");
        projection.ConfigurationKeys.ShouldNotContain("projects.create.enabled");
        projection.RemovedConfigurationKeys.ShouldContain("projects.create.enabled");
        projection.ProcessedMessages.Values.Select(static evidence => evidence.PayloadFingerprint).ShouldNotContain("secret-value");
    }

    [Theory]
    [InlineData(TenantStatus.Active)]
    [InlineData(TenantStatus.Disabled)]
    public async Task WorkerRefreshProjectsExactBoundedSnapshotWithoutConfigurationValues(TenantStatus status)
    {
        InMemoryProjectTenantAccessProjectionStore store = new();
        ProjectsTenantEventHandler subject = CreateWorkerHandler(store);
        TenantAccessRefreshed snapshot = new("tenant-a", status,
            new Dictionary<string, TenantRole> { ["owner"] = TenantRole.TenantOwner, ["reader"] = TenantRole.TenantReader },
            ["projects.allowed", "billing.secret"]);
        await subject.HandleAsync(snapshot, Context(50, "system") with { AggregateId = "tenant-a" }, TestContext.Current.CancellationToken);
        ProjectTenantAccessProjection saved = (await store.GetAsync("tenant-a", TestContext.Current.CancellationToken)).ShouldNotBeNull();
        saved.Enabled.ShouldBe(status == TenantStatus.Active);
        saved.Principals["reader"].Role.ShouldBe("TenantReader");
        saved.ConfigurationKeys.ShouldBe(["projects.allowed"]);
        saved.SnapshotWatermark.ShouldBe(50);
        saved.ProcessedMessages.Values.ShouldHaveSingleItem().PayloadFingerprint.Length.ShouldBe(64);
    }

    [Theory]
    [InlineData("status")]
    [InlineData("membership")]
    [InlineData("role")]
    [InlineData("configuration")]
    public async Task RealRefreshPayloadConflictsPersistAndDenyAccess(string change)
    {
        InMemoryReadModelStore backend = new();
        DaprProjectTenantAccessProjectionStore store = new(backend);
        ProjectsTenantEventHandler subject = CreateWorkerHandler(store);
        TenantAccessRefreshed original = RefreshPayload();
        EventStoreDomainEventContext context = Context(50, "system") with { AggregateId = "tenant-a" };
        await subject.HandleAsync(original, context, TestContext.Current.CancellationToken);
        TenantAccessRefreshed divergent = change switch
        {
            "status" => original with { Status = TenantStatus.Disabled },
            "membership" => original with { Members = new Dictionary<string, TenantRole>(original.Members) { ["intruder"] = TenantRole.TenantReader } },
            "role" => original with { Members = new Dictionary<string, TenantRole>(original.Members) { ["reader"] = TenantRole.TenantOwner } },
            _ => original with { ConfigurationKeys = ["projects.allowed", "projects.changed"] },
        };
        await subject.HandleAsync(divergent, context, TestContext.Current.CancellationToken);

        ProjectTenantAccessProjection saved = (await new DaprProjectTenantAccessProjectionStore(backend)
            .GetAsync("tenant-a", TestContext.Current.CancellationToken)).ShouldNotBeNull();
        saved.ReplayConflict.ShouldBeTrue();
        saved.Enabled.ShouldBeTrue();
        saved.Principals.ShouldNotContainKey("intruder");
        saved.Principals["reader"].Role.ShouldBe("TenantReader");
        saved.ConfigurationKeys.SetEquals(original.ConfigurationKeys).ShouldBeTrue();
        saved.Watermark.ShouldBe(50);
        TenantAccessAuthorizer authorizer = new(new DaprProjectTenantAccessProjectionStore(backend), new FixedUtcClock(Now.AddMinutes(1)), new TenantAccessOptions());
        (await authorizer.AuthorizeMutationAsync(new("tenant-a", "owner", "tenant-a"), TestContext.Current.CancellationToken))
            .Outcome.ShouldBe(TenantAccessOutcome.ReplayConflict);
    }

    [Fact]
    public async Task EquivalentRefreshCollectionOrderingKeepsFingerprintAndLogicalVersionStable()
    {
        InMemoryReadModelStore backend = new();
        ProjectsTenantEventHandler subject = CreateWorkerHandler(new DaprProjectTenantAccessProjectionStore(backend));
        TenantAccessRefreshed original = RefreshPayload();
        EventStoreDomainEventContext context = Context(50, "system") with { AggregateId = "tenant-a" };
        await subject.HandleAsync(original, context, TestContext.Current.CancellationToken);
        ProjectTenantAccessProjection before = (await new DaprProjectTenantAccessProjectionStore(backend)
            .GetAsync("tenant-a", TestContext.Current.CancellationToken)).ShouldNotBeNull();
        TenantAccessRefreshed reordered = original with
        {
            Members = original.Members.Reverse().ToDictionary(member => member.Key, member => member.Value, StringComparer.Ordinal),
            ConfigurationKeys = original.ConfigurationKeys.Reverse().ToArray(),
        };
        await subject.HandleAsync(reordered, context, TestContext.Current.CancellationToken);
        ProjectTenantAccessProjection after = (await new DaprProjectTenantAccessProjectionStore(backend)
            .GetAsync("tenant-a", TestContext.Current.CancellationToken)).ShouldNotBeNull();
        after.Version.ShouldBe(before.Version);
        after.ProcessedMessages.ShouldHaveSingleItem().Value.PayloadFingerprint.ShouldBe(before.ProcessedMessages.Single().Value.PayloadFingerprint);
        after.ReplayConflict.ShouldBeFalse();
        after.MalformedEvidence.ShouldBeFalse();
        TenantAccessAuthorizer authorizer = new(new DaprProjectTenantAccessProjectionStore(backend), new FixedUtcClock(Now.AddMinutes(1)), new TenantAccessOptions());
        (await authorizer.AuthorizeMutationAsync(new("tenant-a", "owner", "tenant-a"), TestContext.Current.CancellationToken)).IsAllowed.ShouldBeTrue();
    }

    [Theory]
    [InlineData("oversized-entries")]
    [InlineData("oversized-bytes")]
    [InlineData("unknown-status")]
    [InlineData("invalid-role")]
    [InlineData("repeated-configuration-key")]
    public async Task MalformedRefreshPayloadCannotPersistPartialValidAccess(string shape)
    {
        InMemoryReadModelStore backend = new();
        ProjectsTenantEventHandler subject = CreateWorkerHandler(new DaprProjectTenantAccessProjectionStore(backend));
        TenantAccessRefreshed valid = RefreshPayload();
        TenantAccessRefreshed invalid = shape switch
        {
            "oversized-entries" => valid with { Members = Enumerable.Range(0, ProjectionSnapshotLimits.Default.MaxEntries + 1)
                .ToDictionary(index => "reader-" + index, _ => TenantRole.TenantReader, StringComparer.Ordinal) },
            "oversized-bytes" => valid with { Members = new Dictionary<string, TenantRole> { [new string('中', ProjectionSnapshotLimits.Default.MaxSerializedBytes)] = TenantRole.TenantReader } },
            "unknown-status" => valid with { Status = (TenantStatus)999 },
            "invalid-role" => valid with { Members = new Dictionary<string, TenantRole>(valid.Members) { ["reader"] = (TenantRole)999 } },
            _ => valid with { ConfigurationKeys = ["projects.allowed", "projects.allowed"] },
        };
        Exception? failure = await Record.ExceptionAsync(() => subject.HandleAsync(invalid,
            Context(50, "system") with { AggregateId = "tenant-a" }, TestContext.Current.CancellationToken));
        if (shape is "oversized-entries" or "oversized-bytes") failure.ShouldBeOfType<ProjectionSnapshotLimitExceededException>();
        else if (shape == "unknown-status") failure.ShouldBeOfType<InvalidOperationException>();
        else failure.ShouldBeNull();

        ProjectTenantAccessProjection? saved = await new DaprProjectTenantAccessProjectionStore(backend)
            .GetAsync("tenant-a", TestContext.Current.CancellationToken);
        if (saved is not null)
        {
            saved.Enabled.ShouldBeFalse();
            saved.Principals.ShouldBeEmpty();
            saved.ConfigurationKeys.ShouldBeEmpty();
            saved.Watermark.ShouldBe(0);
            saved.SnapshotWatermark.ShouldBe(0);
            saved.MalformedEvidence.ShouldBeTrue();
        }

        TenantAccessAuthorizer authorizer = new(new DaprProjectTenantAccessProjectionStore(backend), new FixedUtcClock(Now.AddMinutes(1)), new TenantAccessOptions());
        (await authorizer.AuthorizeMutationAsync(new("tenant-a", "owner", "tenant-a"), TestContext.Current.CancellationToken)).IsAllowed.ShouldBeFalse();
    }

    private static TenantAccessRefreshed RefreshPayload()
        => new("tenant-a", TenantStatus.Active, new Dictionary<string, TenantRole>
            { ["owner"] = TenantRole.TenantOwner, ["reader"] = TenantRole.TenantReader }, ["projects.allowed", "projects.second"]);

    [Fact]
    public async Task ProjectionWriterOptionShouldDisableTheNonOwningHostBeforeMutation()
    {
        InMemoryProjectTenantAccessProjectionStore store = new();
        ProjectsTenantEventHandler disabledWorker = CreateWorkerHandler(store, ProjectTenantEventProjectionWriter.Server);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await disabledWorker.HandleAsync(new TenantCreated("tenant-a", "Tenant A", null, Now), Context(1), cancellationToken);

        (await store.GetAsync("tenant-a", cancellationToken)).ShouldBeNull();
    }

    private static ProjectsTenantEventHandler CreateWorkerHandler(
        IProjectTenantAccessProjectionStore store,
        ProjectTenantEventProjectionWriter writer = ProjectTenantEventProjectionWriter.Workers)
        => new(
            new ProjectTenantAccessHandler(store, new FixedUtcClock(Now.AddMinutes(1)), new TenantAccessOptions()),
            new ProjectsTenantAccessEventMapper(),
            Options.Create(new ProjectTenantEventOptions { ProjectionWriter = writer }));

    private static EventStoreDomainEventContext Context(long sequence, string tenantId = "tenant-a")
        => new(tenantId, tenantId, $"01J00000000000000000000{sequence:D3}", sequence, Now, $"corr-{sequence}");

    private static ServiceCollection CreateServiceCollection()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();
        return services;
    }
}
