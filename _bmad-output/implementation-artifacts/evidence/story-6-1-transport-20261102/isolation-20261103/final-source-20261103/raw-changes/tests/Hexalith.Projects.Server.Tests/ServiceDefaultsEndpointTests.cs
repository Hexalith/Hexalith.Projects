// <copyright file="ServiceDefaultsEndpointTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.Projects.Authorization;

using Microsoft.AspNetCore.Authorization;
using Hexalith.Projects.Server;
using Hexalith.Projects.Server.Conversations;
using Hexalith.Projects.ServiceDefaults;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

/// <summary>
/// Tests the shared ServiceDefaults endpoint mapping without requiring live Dapr sidecars.
/// </summary>
public sealed class ServiceDefaultsEndpointTests
{
    /// <summary>Verifies health, liveness, and readiness endpoints are mapped separately.</summary>
    [Fact]
    public void MapDefaultEndpointsShouldMapHealthAliveAndReadyRoutes()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddServiceDefaults();

        WebApplication app = builder.Build();
        app.MapDefaultEndpoints();

        string[] routePatterns = [.. ((IEndpointRouteBuilder)app)
            .DataSources
            .SelectMany(static dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(static endpoint => endpoint.RoutePattern.RawText ?? string.Empty)];

        routePatterns.ShouldContain("/health");
        routePatterns.ShouldContain("/alive");
        routePatterns.ShouldContain("/ready");
    }

    /// <summary>Classifies only the three metadata-safe probes under the shared fail-closed HTTP inventory.</summary>
    [Fact]
    public async Task HealthProbesPassTheSharedHttpSecurityInventory()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddServiceDefaults();
        builder.Services.AddEventStoreHttpHostSecurity();
        using WebApplication app = builder.Build();
        app.MapDefaultEndpoints();
        var endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).ToArray();
        (await EventStoreHttpEndpointInventory.ValidateAsync(endpoints, app.Services)).ShouldBeEmpty();
        endpoints.Length.ShouldBe(3);
        foreach (RouteEndpoint endpoint in endpoints.OfType<RouteEndpoint>())
        {
            EventStoreHttpEndpointInventory.IsProbe(endpoint.RoutePattern.RawText).ShouldBeTrue();
            endpoint.Metadata.GetMetadata<IAllowAnonymous>().ShouldNotBeNull();
            endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().ShouldBeEmpty();
        }
    }

    /// <summary>Verifies default health checks include liveness and readiness tags.</summary>
    [Fact]
    public void AddDefaultHealthChecksShouldRegisterTaggedLivenessAndReadinessChecks()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.AddDefaultHealthChecks();

        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckServiceOptions> options =
            provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckServiceOptions>>();

        options.Value.Registrations.ShouldContain(registration => registration.Tags.Contains("live"));
        options.Value.Registrations.ShouldContain(registration => registration.Tags.Contains("ready"));
    }

    /// <summary>Verifies runtime composition replaces fail-closed defaults with local runtime evidence providers.</summary>
    [Fact]
    public void AddProjectsServerRuntimeInfrastructureShouldReplaceFailClosedAuthorizationEvidence()
    {
        ServiceCollection services = new();

        services.AddProjectsServer();
        services.AddProjectsServerRuntimeInfrastructure();

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IProjectEventStoreAuthorizationValidator>()
            .ShouldBeOfType<AllowingProjectEventStoreAuthorizationValidator>();
        provider.GetRequiredService<IProjectDaprPolicyEvidenceProvider>()
            .ShouldBeOfType<AllowingProjectDaprPolicyEvidenceProvider>();
        services.Single(static service => service.ServiceType == typeof(IProjectAuditTimelineReadModel))
            .ImplementationType.ShouldBe(typeof(DaprProjectAuditTimelineReadModel));
    }

    /// <summary>Verifies the ACL directory does not capture transient typed HTTP clients in a singleton.</summary>
    [Fact]
    public void AddProjectsServerShouldRegisterConversationDirectoryAsTransient()
    {
        ServiceCollection services = new();

        services.AddProjectsServer();

        ServiceDescriptor descriptor = services.Single(static service => service.ServiceType == typeof(IProjectConversationDirectory));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }
}
