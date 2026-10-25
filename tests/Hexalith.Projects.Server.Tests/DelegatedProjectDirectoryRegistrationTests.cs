namespace Hexalith.Projects.Server.Tests;

using System.Security.Claims;
using Dapr.Client;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Server.Folders;
using Hexalith.Projects.Server.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using Xunit;

/// <summary>Verifies captured owner directories resolve transport from each active request scope.</summary>
public sealed class DelegatedProjectDirectoryRegistrationTests
{
    /// <summary>Preserves SDK and host registrations in either composition order under scope validation.</summary>
    /// <param name="sdkFirst">Whether the SDK transport is enrolled before Projects.</param>
    /// <param name="hostClient">Whether the host supplies its own scoped transport factory.</param>
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task BothDirectoriesUseTheActiveRequestServices(bool sdkFirst, bool hostClient)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        List<IServiceProvider> transportScopes = [];
        builder.Services.AddScoped<DaprClient>(scope =>
        {
            transportScopes.Add(scope);
            return new DaprClientBuilder().Build();
        });
        ServiceDescriptor hostDapr = builder.Services.Single(service => service.ServiceType == typeof(DaprClient));
        if (hostClient)
            builder.Services.AddScoped(scope => ActivatorUtilities.CreateInstance<DelegatedOwnerQueryClient>(scope));
        ServiceDescriptor? hostTransport = builder.Services.SingleOrDefault(service => service.ServiceType == typeof(DelegatedOwnerQueryClient));
        if (sdkFirst) builder.Services.AddEventStoreDelegatedOwnerQueries();
        builder.Services.AddProjectsServer();
        if (!sdkFirst) builder.Services.AddEventStoreDelegatedOwnerQueries();
        builder.Services.Single(service => service.ServiceType == typeof(DaprClient)).ShouldBeSameAs(hostDapr);
        var transport = builder.Services.Single(service => service.ServiceType == typeof(DelegatedOwnerQueryClient));
        transport.Lifetime.ShouldBe(sdkFirst || hostClient ? ServiceLifetime.Scoped : ServiceLifetime.Transient);
        if (hostTransport is not null) transport.ShouldBeSameAs(hostTransport);

        IProjectAdvisoryConversationDirectory? conversations = null;
        IProjectFolderDirectory? folders = null;
        builder.Services.RemoveAll<IDomainQueryHandler>();
        builder.Services.AddSingleton<IDomainQueryHandler>(new DelegatedDirectoryTestQueryHandler(async token =>
        {
            var folder = await folders!.RefreshFolderReferenceAsync(new ProjectId("project-a"), "folder-a", "correlation", token).ConfigureAwait(false);
            folder.Outcome.ShouldBe(ProjectFolderValidationOutcome.Unavailable);
            int folderTransportCount = transportScopes.Count;
            var conversation = await conversations!.ReadAsync("tenant-a", "conversation-a", token).ConfigureAwait(false);
            conversation.ErrorMessage.ShouldBe("actor-exception");
            return QueryResult.FromPayload(System.Text.Json.JsonSerializer.SerializeToElement(new { folderTransportCount }));
        }));
        await using ServiceProvider provider = builder.Services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        // Resolve from the root once, as the singleton executors do, and reuse across two requests.
        conversations = provider.GetRequiredService<IProjectAdvisoryConversationDirectory>();
        folders = provider.GetRequiredService<IProjectFolderDirectory>();
        IHttpContextAccessor accessor = provider.GetRequiredService<IHttpContextAccessor>();
        for (int request = 0; request < 2; request++)
        {
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            QueryEnvelope query = new("tenant-a", "projects", "projects", DelegatedDirectoryTestQueryHandler.Route,
                "{}"u8.ToArray(), "correlation", "actor-a")
            {
                OriginalActorId = "actor-a", AuthenticatedWorkloadId = "operator-web",
                CallerAuthority = new("authenticated-caller", [], []),
            };
            List<Claim> claims =
            [
                new(EventStoreWorkloadAuthenticationDefaults.WorkloadClaimType, "eventstore"),
                new(EventStoreWorkloadAuthenticationDefaults.OperationClaimType, EventStoreWorkloadOperations.DomainServiceQuery),
                .. QueryCallerAuthorityBinding.Bind(query).Select(pair => new Claim(pair.Key, pair.Value)),
            ];
            accessor.HttpContext = new DefaultHttpContext
            {
                RequestServices = scope.ServiceProvider,
                User = new(new ClaimsIdentity(claims, "Workload")),
            };
            accessor.HttpContext.Request.Headers[EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName] = "verified-fixture-assertion";
            try
            {
                var result = await DomainQueryDispatcher.ExecuteAsync(scope.ServiceProvider, query, TestContext.Current.CancellationToken);
                result.Success.ShouldBeTrue(result.ErrorMessage);
                result.GetPayload().GetProperty("folderTransportCount").GetInt32().ShouldBe(request + 1);
                transportScopes.Count.ShouldBe(request + 1);
                transportScopes[request].ShouldBeSameAs(scope.ServiceProvider);
            }
            finally { accessor.HttpContext = null; }
        }
        transportScopes[0].ShouldNotBeSameAs(transportScopes[1]);
    }
}
