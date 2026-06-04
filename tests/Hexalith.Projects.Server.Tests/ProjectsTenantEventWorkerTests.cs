// <copyright file="ProjectsTenantEventWorkerTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using System.Linq;
using System.Threading.Tasks;

using Hexalith.EventStore.Client.Subscriptions;
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
