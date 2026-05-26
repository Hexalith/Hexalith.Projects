// <copyright file="ProjectsServerServiceCollectionExtensions.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using System;
using System.Linq;

using Hexalith.Conversations.Client;
using Hexalith.Folders.Client;
using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Client.Registration;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Infrastructure;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Server.Conversations;
using Hexalith.Projects.Server.Folders;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using FoldersClient = Hexalith.Folders.Client.Generated.IClient;

/// <summary>
/// DI registration for the Hexalith.Projects Server command-async + minimal-read slice (Story 1.4).
/// </summary>
public static class ProjectsServerServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Projects domain processor, the in-memory detail read model, and a
    /// <see cref="TimeProvider"/> for the command-async + minimal-read endpoints. The production
    /// <see cref="IProjectCommandSubmitter"/> (EventStore gateway-backed) and the gateway client are
    /// registered by <see cref="AddProjectsServerRuntimeInfrastructure"/>; a caller may register a fake submitter
    /// for Tier-2 tests.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddProjectsServer(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();
        services.AddProjectsTenantAccess();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<InMemoryProjectDetailReadModel>();
        services.TryAddSingleton<IProjectDetailReadModel>(sp => sp.GetRequiredService<InMemoryProjectDetailReadModel>());
        services.TryAddSingleton<InMemoryProjectListReadModel>();
        services.TryAddSingleton<IProjectListReadModel>(sp => sp.GetRequiredService<InMemoryProjectListReadModel>());
        services.TryAddSingleton<IProjectTenantContextAccessor, HttpContextProjectTenantContextAccessor>();
        services.TryAddSingleton<ProjectAuthorizationGate>();
        services.TryAddSingleton<IActorPartyResolver, DeterministicActorPartyResolver>();
        services.TryAddTransient<IProjectConversationDirectory>(sp =>
        {
            IConversationClient? client = sp.GetService<IConversationClient>();
            return client is null
                ? new UnavailableProjectConversationDirectory()
                : new ConversationsProjectConversationDirectory(client);
        });
        services.TryAddTransient<IProjectConversationAssignmentDirectory>(sp =>
        {
            IConversationClient? client = sp.GetService<IConversationClient>();
            return client is null
                ? new UnavailableProjectConversationAssignmentDirectory()
                : new ConversationsProjectConversationAssignmentDirectory(
                    client,
                    sp.GetRequiredService<IActorPartyResolver>());
        });
        services.TryAddTransient<IProjectFolderDirectory>(sp =>
        {
            FoldersClient? client = sp.GetService<FoldersClient>();
            return client is null
                ? new UnavailableProjectFolderDirectory()
                : new FoldersProjectFolderDirectory(client);
        });
        services.TryAddSingleton<IProjectEventStoreAuthorizationValidator, DenyAllProjectEventStoreAuthorizationValidator>();
        services.TryAddSingleton<IProjectDaprPolicyEvidenceProvider, DenyAllProjectDaprPolicyEvidenceProvider>();
        services.TryAddSingleton<IClaimsTransformation, ProjectsClaimsTransformation>();
        services.TryAddSingleton<IDomainProcessor, ProjectsDomainProcessor>();

        return services;
    }

    /// <summary>
    /// Replaces pre-runtime fakes with Dapr/EventStore-backed runtime infrastructure.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddProjectsServerRuntimeInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddProjectsDaprInfrastructure();
        services.RemoveAll<IProjectTenantAccessProjectionStore>();
        services.AddSingleton<IProjectTenantAccessProjectionStore, DaprProjectTenantAccessProjectionStore>();
        services.RemoveAll<IProjectEventStoreAuthorizationValidator>();
        services.AddSingleton<IProjectEventStoreAuthorizationValidator, AllowingProjectEventStoreAuthorizationValidator>();
        services.RemoveAll<IProjectDaprPolicyEvidenceProvider>();
        services.AddSingleton<IProjectDaprPolicyEvidenceProvider, AllowingProjectDaprPolicyEvidenceProvider>();
        services.RemoveAll<IProjectListReadModel>();
        services.RemoveAll<InMemoryProjectListReadModel>();
        services.AddSingleton<IProjectListReadModel, DaprProjectListReadModel>();
        services.RemoveAll<IProjectDetailReadModel>();
        services.RemoveAll<InMemoryProjectDetailReadModel>();
        services.AddSingleton<IProjectDetailReadModel, DaprProjectDetailReadModel>();
        if (!services.Any(static service => service.ServiceType == typeof(IConversationClient)))
        {
            services.AddHexalithConversationsClient(options => options.Endpoint = new Uri("http://conversations"));
        }

        if (!services.Any(static service => service.ServiceType == typeof(FoldersClient)))
        {
            services.AddFoldersClient(options => options.BaseAddress = new Uri("http://folders"));
        }

        services.AddEventStoreGatewayClient(options => options.BaseAddress = new Uri("http://eventstore"));
        services.TryAddSingleton<IProjectCommandSubmitter, EventStoreProjectCommandSubmitter>();

        return services;
    }

    /// <summary>Maps the Projects Server endpoints (the command-async POST + minimal-read GET).</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The same route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapProjectsServerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapProjectsDomainServiceEndpoints();
        return endpoints;
    }
}
