// <copyright file="ConversationTenantAccessRegistrationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Commons.UniqueIds;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Client.Handlers;
using Hexalith.Tenants.Client.Projections;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hexalith.Conversations.Server.Tests.TenantAccess;

/// <summary>
/// Verifies Tenants integration registration remains server-boundary scoped.
/// </summary>
public sealed class ConversationTenantAccessRegistrationTest
{
    /// <summary>
    /// The registration adds Tenants projection services, the Conversations projection signal, and the access boundary.
    /// </summary>
    [Fact]
    public void AddConversationTenantAccessShouldRegisterTenantsProjectionAndAccessService()
    {
        ServiceCollection services = new();
        services.AddLogging();

        services.AddConversationTenantAccess();

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IConversationTenantAccessService>()
            .ShouldBeOfType<ConversationTenantAccessService>();
        provider.GetRequiredService<ITenantProjectionStore>().ShouldNotBeNull();
        provider.GetRequiredService<TenantProjectionEventHandler>().ShouldNotBeNull();

        // F12: the projection signal is registered explicitly so freshness/poisoning checks
        // remain a first-class dependency, not a silent cast against the store.
        provider.GetRequiredService<IConversationTenantProjectionSignal>()
            .ShouldBeOfType<DefaultConversationTenantProjectionSignal>();
    }

    /// <summary>
    /// Calling the shared registration twice keeps the Conversations facade registrations singular.
    /// </summary>
    [Fact]
    public void AddConversationTenantAccessShouldKeepFacadeRegistrationsSingularWhenCalledTwice()
    {
        ServiceCollection services = new();
        services.AddLogging();

        services.AddConversationTenantAccess();
        services.AddConversationTenantAccess();

        services.Count(static descriptor => descriptor.ServiceType == typeof(IConversationTenantAccessService))
            .ShouldBe(1);
        services.Count(static descriptor => descriptor.ServiceType == typeof(IConversationTenantProjectionSignal))
            .ShouldBe(1);
    }

    /// <summary>Configured Tenants registration consumes injected delivery names and its SDK processor updates isolated state.</summary>
    /// <returns>A task for the consumer-state assertions.</returns>
    [Fact]
    public async Task EnrolledTenantConsumerUsesConfiguredDeliveryAndUpdatesItsProjection()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddConversationTenantAccess(EnrolledConfiguration());
        using ServiceProvider provider = services.BuildServiceProvider();
        EventStoreDomainEventsOptions options = provider.GetRequiredService<IOptions<EventStoreDomainEventsOptions>>().Value;
        options.PubSubName.ShouldBe("enrolled-tenant-events");
        options.TopicName.ShouldBe("enrolled-tenant-topic");
        options.SubscriptionRoute.ShouldBe("/tenants/events");
        TenantCreated created = new("tenant-configured", "Configured tenant", null, DateTimeOffset.UtcNow);
        // The producer's ordinary raw-envelope contract uses a sortable message ULID and system/tenants scope.
        EventStoreDomainEventEnvelope envelope = new(UniqueIdHelper.GenerateSortableUniqueStringId(), created.TenantId, "system", typeof(TenantCreated).FullName!,
            1, created.CreatedAt, UniqueIdHelper.GenerateSortableUniqueStringId(), "json", JsonSerializer.SerializeToUtf8Bytes(created, created.GetType()))
        {
            Domain = "tenants",
            UserId = "enrollment-consumer",
        };
        EventIdentityValidator.ValidateSubscription(envelope.TenantId, envelope.Domain, envelope.AggregateId, envelope.AggregateType,
            envelope.EventTypeName, envelope.MessageId, envelope.CorrelationId, envelope.CausationId, envelope.SequenceNumber);
        (await provider.GetRequiredService<EventStoreDomainEventProcessor>().ProcessAsync(envelope, TestContext.Current.CancellationToken))
            .ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        (await provider.GetRequiredService<ITenantProjectionStore>().GetAsync(created.TenantId, TestContext.Current.CancellationToken))!
            .Name.ShouldBe(created.Name);
    }

    /// <summary>Missing enrolled pub/sub, topic, or declarative configuration refuses before delivery registration.</summary>
    /// <param name="key">The missing input.</param>
    /// <param name="value">The absent or invalid value.</param>
    [Theory]
    [InlineData("Tenants:PubSubName", null)]
    [InlineData("Tenants:PubSubName", " ")]
    [InlineData("Tenants:TopicName", null)]
    [InlineData("Tenants:TopicName", " ")]
    [InlineData("Dapr:DeclarativeSubscriptions", null)]
    [InlineData("Dapr:DeclarativeSubscriptions", " ")]
    [InlineData("Dapr:DeclarativeSubscriptions", "false")]
    public void MissingEnrolledDeliveryInputsFailBeforeConsumerRegistration(string key, string? value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ServiceCollection services = new();
        Should.Throw<InvalidOperationException>(() => services.AddConversationTenantAccess(EnrolledConfiguration(key, value)))
            .Message.ShouldContain(key.Split(':')[0]);
        services.ShouldNotContain(descriptor => descriptor.ServiceType == typeof(EventStoreDomainEventProcessor));
    }

    private static IConfiguration EnrolledConfiguration(string? key = null, string? value = null)
    {
        Dictionary<string, string?> values = new()
        {
            ["Tenants:PubSubName"] = "enrolled-tenant-events",
            ["Tenants:TopicName"] = "enrolled-tenant-topic",
            ["Dapr:DeclarativeSubscriptions"] = "true",
        };
        if (key is not null) values[key] = value;
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    /// <summary>
    /// F20: omitting <see cref="ConversationTenantAccessServiceCollectionExtensions.AddConversationTenantAccess"/>
    /// must leave the access service unresolvable so protected routes cannot fall back to a permissive default.
    /// </summary>
    [Fact]
    public void OmittingRegistrationShouldLeaveAccessServiceUnresolvable()
    {
        ServiceCollection services = new();
        services.AddLogging();

        // Intentionally NOT calling AddConversationTenantAccess.
        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<IConversationTenantAccessService>().ShouldBeNull();
        provider.GetService<IConversationTenantProjectionSignal>().ShouldBeNull();

        // Requesting the access service via GetRequiredService must throw, never fall back.
        Should.Throw<InvalidOperationException>(() =>
            provider.GetRequiredService<IConversationTenantAccessService>());
    }
}
