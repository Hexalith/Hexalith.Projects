// <copyright file="ProjectsServerServiceCollectionExtensions.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using Hexalith.Projects.Contracts.Routing;

using System;
using System.Linq;

using Hexalith.Conversations.Client;
using Hexalith.Folders.Client;
using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.Memories.Client.Rest;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Infrastructure;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Server.Conversations;
using Hexalith.Projects.Server.Folders;
using Hexalith.Projects.Server.Memories;
using Hexalith.Projects.Server.Proposals;
using Hexalith.Projects.Server.Projections.ConversationStartSetup;
using Hexalith.Projects.Server.Projections.ProjectReads;
using Hexalith.Projects.Server.Queries;
using Hexalith.Projects.Server.Authentication;
using Hexalith.Projects.Server.Authorization;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using FoldersClient = Hexalith.Folders.Client.Generated.IClient;

/// <summary>
/// DI registration for the Hexalith.Projects Server command-async + minimal-read slice (Story 1.4).
/// </summary>
public static class ProjectsServerServiceCollectionExtensions
{
    /// <summary>Registers the Projects executable with the canonical EventStore receiver.</summary>
    /// <param name="builder">The Projects host builder.</param>
    /// <returns>The same host builder.</returns>
    public static WebApplicationBuilder AddProjectsDomainService(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (ProjectsAuthenticationServiceCollectionExtensions.IsAnonymousDevelopmentBypass(builder.Configuration, builder.Environment))
        {
            throw new InvalidOperationException("The Projects domain service requires authenticated human callers.");
        }

        builder.Services.AddProjectsServer();
        builder.Services.AddProjectsServerRuntimeInfrastructure();
        builder.Services.AddProjectsAuthentication(builder.Configuration, builder.Environment);
        builder.AddEventStoreDomainService(typeof(ProjectsDomainProcessor).Assembly);
        builder.Services.AddSingleton<IDomainQueryResponsePolicy, ProjectsQueryResponsePolicy>();
        builder.Services.AddSingleton(new LegacyProjectionCatalogRegistration(ProjectsRuntimeCatalogDeclarations.Routes
            .Where(route => route.Kind == "projection" && route.ProjectionType == ProjectsServerModule.ProjectionType).ToArray()));
        builder.AddProjectsDevelopmentCatalog();
        return builder;
    }

    /// <summary>Maps the retained Projects REST surface for authenticated human callers.</summary>
    /// <param name="app">The Projects host.</param>
    /// <returns>The same host.</returns>
    public static WebApplication MapProjectsHumanEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapGroup(string.Empty)
            .RequireAuthorization(EventStoreHttpHostSecurityExtensions.HumanPolicy)
            .MapProjectsDomainServiceEndpoints();
        return app;
    }

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
        services.AddProjectsModule();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<InMemoryProjectDetailReadModel>();
        services.TryAddSingleton<IProjectDetailReadModel>(sp => sp.GetRequiredService<InMemoryProjectDetailReadModel>());
        services.TryAddSingleton<InMemoryProjectListReadModel>();
        services.TryAddSingleton<IProjectListReadModel>(sp => sp.GetRequiredService<InMemoryProjectListReadModel>());
        services.TryAddSingleton<InMemoryProjectReferenceIndexReadModel>();
        services.TryAddSingleton<IProjectReferenceIndexReadModel>(sp => sp.GetRequiredService<InMemoryProjectReferenceIndexReadModel>());
        services.TryAddSingleton<InMemoryProjectAuditTimelineReadModel>();
        services.TryAddSingleton<IProjectAuditTimelineReadModel>(sp => sp.GetRequiredService<InMemoryProjectAuditTimelineReadModel>());
        services.TryAddSingleton<InMemoryProjectReadModelStore>();
        services.TryAddSingleton<IReadModelStore>(sp => sp.GetRequiredService<InMemoryProjectReadModelStore>());
        services.TryAddSingleton<IProjectTenantContextAccessor, HttpContextProjectTenantContextAccessor>();
        services.TryAddSingleton<ProjectAuthorizationGate>();
        services.TryAddSingleton<IProjectProposalConfirmationIdempotencyLedger, InMemoryProjectProposalConfirmationIdempotencyLedger>();
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
        services.TryAddTransient<IProjectConversationResolutionDirectory>(sp =>
        {
            IConversationClient? client = sp.GetService<IConversationClient>();
            return client is null
                ? new UnavailableProjectConversationResolutionDirectory()
                : new ConversationsProjectConversationResolutionDirectory(client);
        });
        services.TryAddTransient<IProjectConversationMembershipDirectory>(sp =>
        {
            IHttpContextAccessor accessor = sp.GetRequiredService<IHttpContextAccessor>();
            return new ProjectConversationMembershipDirectory(sp.GetService<IConversationClient>(), () =>
                (accessor.HttpContext ?? throw new InvalidOperationException("Owner queries require an active request."))
                    .RequestServices.GetRequiredService<DelegatedOwnerQueryClient>(), accessor);
        });
        services.AddHttpClient();
        services.TryAddTransient<DelegatedOwnerQueryClient>(sp => ActivatorUtilities.CreateInstance<DelegatedOwnerQueryClient>(sp));
        services.TryAddTransient<IProjectFolderDirectory>(sp =>
        {
            FoldersClient? client = sp.GetService<FoldersClient>();
            IProjectFolderDirectory direct = client is null ? new UnavailableProjectFolderDirectory() : new FoldersProjectFolderDirectory(client);
            if (sp.GetService<FoldersV1ProjectReadTransport>() is not null)
                direct = new FoldersV1ProjectReadDirectory(sp.GetRequiredService<IHttpClientFactory>().CreateClient(FoldersV1ProjectReadDirectory.ClientName), direct);
            IHttpContextAccessor accessor = sp.GetRequiredService<IHttpContextAccessor>();
            return new DelegatedProjectFolderDirectory(() =>
                (accessor.HttpContext ?? throw new InvalidOperationException("Owner queries require an active request."))
                    .RequestServices.GetRequiredService<DelegatedOwnerQueryClient>(), accessor, direct);
        });
        services.TryAddTransient<IProjectFileReferenceDirectory>(sp =>
        {
            FoldersClient? client = sp.GetService<FoldersClient>();
            return client is null
                ? new UnavailableProjectFileReferenceDirectory()
                : new FoldersProjectFileReferenceDirectory(client);
        });
        services.TryAddTransient<IProjectMemoryDirectory>(sp =>
        {
            MemoriesClient? client = sp.GetService<MemoriesClient>();
            return client is null
                ? new UnavailableProjectMemoryDirectory()
                : new MemoriesProjectMemoryDirectory(client);
        });
        services.TryAddSingleton<IProjectEventStoreAuthorizationValidator, DenyAllProjectEventStoreAuthorizationValidator>();
        services.TryAddSingleton<IProjectDaprPolicyEvidenceProvider, DenyAllProjectDaprPolicyEvidenceProvider>();
        services.TryAddSingleton<IClaimsTransformation, ProjectsClaimsTransformation>();
        services.TryAddTransient<ProjectsDomainProcessor>();
        services.TryAddTransient<IDomainProcessor>(provider => provider.GetRequiredService<ProjectsDomainProcessor>());
        services.AddKeyedTransient<IDomainProcessor>(ProjectsServerModule.DomainName,
            (provider, _) => provider.GetRequiredService<IDomainProcessor>());
        services.AddKeyedTransient<IAsyncAggregateReplay>(ProjectsServerModule.DomainName,
            (provider, _) => provider.GetRequiredService<ProjectsDomainProcessor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDomainProjectionHandler, ProjectProjectionHandler>());
        services.AddOptions<ProjectionDispatchOptions>().BindConfiguration("EventStore:ProjectionDispatch");
        services.AddOptions<DomainProjectionIdentityOptions>().BindConfiguration("EventStore:DomainService")
            .PostConfigure(options =>
            {
                if (string.IsNullOrWhiteSpace(options.AppId)) { options.AppId = ProjectsServerModule.DomainName; }
            });
        services.AddSingleton<IDomainQueryHandler, GetConversationStartSetupQueryHandler>();
        services.AddSingleton<ProjectQueryEnvelopePrincipalBinding>();
        services.AddSingleton<ProjectContextQueryExecutor>();
        services.AddSingleton<IDomainQueryHandler, GetProjectContextQueryHandler>();
        services.AddSingleton<IDomainQueryHandler, ExplainContextSelectionQueryHandler>();
        services.AddDataProtection();
        services.AddEventStoreQueryCursorCodec("Hexalith.Projects.ReadCursor.v1");
        services.AddSingleton<ProjectReadAudit>();
        services.AddSingleton<ProjectReadQueryExecutor>();
        services.AddSingleton<ProjectAdvisoryQueryExecutor>();
        services.TryAddSingleton<IProjectAdvisoryConversationDirectory>(sp =>
        {
            IHttpContextAccessor accessor = sp.GetRequiredService<IHttpContextAccessor>();
            return new DelegatedProjectAdvisoryConversationDirectory(() =>
                (accessor.HttpContext ?? throw new InvalidOperationException("Owner queries require an active request."))
                    .RequestServices.GetRequiredService<DelegatedOwnerQueryClient>());
        });
        services.AddSingleton<IDomainQueryHandler, ResolveProjectFromConversationQueryHandler>();
        services.AddSingleton<IDomainQueryHandler, ResolveProjectFromAttachmentsQueryHandler>();
        services.AddSingleton<IDomainQueryHandler, ListProjectsQueryHandler>();
        services.AddSingleton<IDomainQueryHandler, ListProjectsV2QueryHandler>();
        services.AddSingleton<ProjectOperatorQueryExecutor>();
        services.AddSingleton<IDomainQueryHandler, GetProjectAuditQueryHandler>();
        services.AddSingleton<IDomainQueryHandler, GetProjectTaskStatusQueryHandler>();
        services.AddSingleton<IDomainQueryHandler, GetProjectQueryHandler>();

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
        services.AddEventStoreWorkloadAssertionIssuer();
        string authorityDaprEndpoint = Environment.GetEnvironmentVariable("DAPR_HTTP_ENDPOINT")
            ?? $"http://127.0.0.1:{Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500"}";
        services.AddHttpClient<ProjectChatBotAuthorityGateway>(client => client.BaseAddress = new Uri(
            authorityDaprEndpoint.Replace("localhost", "127.0.0.1", StringComparison.OrdinalIgnoreCase)));
        services.AddTransient<ProjectOwnerBootstrapVerifier>();
        services.AddTransient<IAuthoritativeEventStreamReader>(provider =>
            new AuthoritativeEventStreamReader(provider.GetRequiredService<ProjectChatBotAuthorityGateway>(),
                provider.GetRequiredService<TimeProvider>(), TimeSpan.FromSeconds(10)));
        services.AddTransient<IDomainQueryHandler, ProjectChatBotAuthorityQueryHandler>();
        services.RemoveAll<IReadModelStore>();
        services.RemoveAll<InMemoryProjectReadModelStore>();
        services.AddEventStoreReadModelStore();
        services.AddSingleton<IAsyncDomainProjectionHandler, ConversationStartSetupProjectionHandler>();
        services.AddSingleton<ProjectReadProjectionHandler>();
        services.AddSingleton<IAsyncDomainProjectionHandler>(sp => sp.GetRequiredService<ProjectReadProjectionHandler>());
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
        services.RemoveAll<IProjectReferenceIndexReadModel>();
        services.RemoveAll<InMemoryProjectReferenceIndexReadModel>();
        services.AddSingleton<IProjectReferenceIndexReadModel, DaprProjectReferenceIndexReadModel>();
        services.RemoveAll<IProjectAuditTimelineReadModel>();
        services.RemoveAll<InMemoryProjectAuditTimelineReadModel>();
        services.AddSingleton<IProjectAuditTimelineReadModel, DaprProjectAuditTimelineReadModel>();
        if (!services.Any(static service => service.ServiceType == typeof(IConversationClient)))
        {
            services.TryAddTransient<EventStoreGatewayTokenForwardingHandler>();
            services.AddHexalithConversationsClient(options => options.Endpoint = new Uri("http://conversations"))
                .AddHttpMessageHandler<EventStoreGatewayTokenForwardingHandler>();
        }

        if (!services.Any(static service => service.ServiceType == typeof(FoldersClient)))
        {
            services.TryAddTransient<EventStoreGatewayTokenForwardingHandler>();
            services.AddFoldersClient(options => options.BaseAddress = new Uri("http://folders"))
                .AddHttpMessageHandler<EventStoreGatewayTokenForwardingHandler>();
        }
        // The retained read contract is independent of whether a host pre-registers the generated client.
        services.TryAddTransient<EventStoreGatewayTokenForwardingHandler>();
        services.TryAddSingleton<FoldersV1ProjectReadTransport>();
        services.AddHttpClient(FoldersV1ProjectReadDirectory.ClientName, (sp, http) =>
            http.BaseAddress = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Hexalith.Folders.Client.FoldersClientOptions>>().Value.BaseAddress)
            .AddHttpMessageHandler<EventStoreGatewayTokenForwardingHandler>();

        if (!services.Any(static service => service.ServiceType == typeof(MemoriesClient)))
        {
            services.AddMemoriesClient(options => options.Endpoint = new Uri("http://memories"));
        }

        // Forward the authenticated caller's bearer token onto the EventStore command gateway: the
        // command API is [Authorize] and reads the command UserId from the JWT 'sub'. Without this the
        // persist-then-publish hop is rejected (401) and the create collapses to a safe-denial.
        services.AddHttpContextAccessor();
        services.TryAddTransient<EventStoreGatewayTokenForwardingHandler>();
        _ = services
            .AddEventStoreGatewayClient(options => options.BaseAddress = new Uri("http://eventstore"))
            .AddHttpMessageHandler<EventStoreGatewayTokenForwardingHandler>();
        services.TryAddSingleton<IProjectCommandSubmitter, EventStoreProjectCommandSubmitter>();

        return services;
    }

}
