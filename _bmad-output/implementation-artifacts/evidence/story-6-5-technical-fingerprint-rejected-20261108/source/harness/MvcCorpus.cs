using Hexalith.EventStore.Admin.Server.Authorization;
using Hexalith.EventStore.Admin.Server.Configuration;
using Hexalith.EventStore.Contracts.Authorization;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.Controllers;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
namespace Story65.TechnicalFingerprint.Harness;

/// <summary>Real MVC endpoint factory/descriptor graph, without starting a server or executing actions.</summary>
internal sealed class MvcCorpus : IAsyncDisposable
{
    private readonly WebApplication _app;
    private MvcCorpus(WebApplication app, RouteEndpoint[] marked, RouteEndpoint[] eligible, Dictionary<string, int> refused, bool queryMarked, bool queryEligible, int serviceFilters)
    { _app = app; AllMarkedEndpoints = marked; EligibleEndpoints = eligible; RefusalLabels = refused; OwnerQueryMarked = queryMarked; OwnerQueryEligible = queryEligible; ServiceFilterTypesResolved = serviceFilters; }
    internal IServiceProvider Services => _app.Services;
    internal IReadOnlyList<RouteEndpoint> AllMarkedEndpoints { get; }
    internal IReadOnlyList<RouteEndpoint> EligibleEndpoints { get; }
    internal int MarkedTechnicalEndpoints => AllMarkedEndpoints.Count;
    internal int EligibleTechnicalEndpoints => EligibleEndpoints.Count;
    internal int RefusedTechnicalEndpoints => MarkedTechnicalEndpoints - EligibleTechnicalEndpoints;
    internal int ConstrainedEligibleEndpoints => EligibleEndpoints.Count(e => e.RoutePattern.ParameterPolicies.Any(p => p.Value.Count != 0));
    internal IReadOnlyDictionary<string, CorpusAssemblyCounts> AssemblyCounts => new[] { "Hexalith.EventStore.Gateway", "Hexalith.EventStore.Admin.Server" }
        .ToDictionary(name => name, name => new CorpusAssemblyCounts(
            AllMarkedEndpoints.Count(e => OwnerAssembly(e) == name), EligibleEndpoints.Count(e => OwnerAssembly(e) == name),
            AllMarkedEndpoints.Count(e => OwnerAssembly(e) == name) - EligibleEndpoints.Count(e => OwnerAssembly(e) == name),
            EligibleEndpoints.Count(e => OwnerAssembly(e) == name && e.RoutePattern.ParameterPolicies.Any(p => p.Value.Count != 0))), StringComparer.Ordinal);
    private static string? OwnerAssembly(RouteEndpoint endpoint) => endpoint.Metadata.GetMetadata<ControllerActionDescriptor>()!.ControllerTypeInfo.Assembly.GetName().Name;
    internal bool ApplicationStarted => _app.Lifetime.ApplicationStarted.IsCancellationRequested;
    internal IReadOnlyDictionary<string, int> RefusalLabels { get; }
    internal bool OwnerQueryMarked { get; }
    internal bool OwnerQueryEligible { get; }
    internal int ServiceFilterTypesResolved { get; }
    internal static MvcCorpus Create() => Create(null);
    internal static MvcCorpus Create(Action<IServiceCollection>? customize)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.Services.AddAdminApi(builder.Configuration);
        // Constructor-only dependencies from TechnicalHttpCatalogFixture; no action/transport is executed.
        builder.Services.AddSingleton<Dapr.Client.DaprClient>(Substitute.For<Dapr.Client.DaprClient>());
        builder.Services.AddSingleton(Substitute.For<Dapr.Actors.Client.IActorProxyFactory>());
        builder.Services.AddSingleton<Hexalith.EventStore.Services.DaprSnapshotPolicyRepository>();
        builder.Services.AddSingleton<Microsoft.Extensions.Options.IOptions<Hexalith.EventStore.Server.Commands.CommandStatusOptions>>(
            Microsoft.Extensions.Options.Options.Create(new Hexalith.EventStore.Server.Commands.CommandStatusOptions { StateStoreName = "statestore" }));
        builder.Services.AddSingleton(Substitute.For<Hexalith.EventStore.Authorization.ITenantValidator>());
        builder.Services.AddSingleton(Substitute.For<Hexalith.EventStore.Authorization.IRbacValidator>());
        builder.Services.AddSingleton(Substitute.For<Hexalith.EventStore.Commands.ICommandActivityReader>());
        builder.Services.AddSingleton(Substitute.For<Hexalith.EventStore.Admin.Abstractions.Services.ITenantCommandService>());
        builder.Services.AddSingleton(Substitute.For<Hexalith.EventStore.Admin.Abstractions.Services.ITenantQueryService>());
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddEventStoreHttpHostSecurity();
        builder.Services.PostConfigure<AuthorizationOptions>(options =>
        {
            foreach (string name in new[] { AdminAuthorizationPolicies.ReadOnly, AdminAuthorizationPolicies.Operator, AdminAuthorizationPolicies.Admin })
                options.AddPolicy(name, new AuthorizationPolicyBuilder(options.GetPolicy(name)!).Combine(EventStoreHttpHostSecurityExtensions.CreateHumanPolicy()).Build());
        });
        builder.Services.AddEventStoreWorkloadPolicies("DaprInternal", [EventStoreOperationIdentifiers.GatewayCommandSubmit,
            EventStoreOperationIdentifiers.GatewayCommandStatus, EventStoreOperationIdentifiers.GatewayStreamRead, EventStoreOperationIdentifiers.TrustedEffect,
            EventStoreWorkloadOperations.GatewayOwnerAuthorityQuery]);
        builder.Services.AddSingleton(Substitute.For<IRouteCatalogAuthority>());
        builder.Services.AddScoped<GeneratedRouteCatalogFilter>();
        builder.Services.AddScoped<Hexalith.EventStore.Authorization.AdminTenantAuthorizationFilter>();
        builder.Services.AddControllers(options => options.Filters.AddService<GeneratedRouteCatalogFilter>())
            .AddApplicationPart(typeof(CommandStatusController).Assembly).AddApplicationPart(typeof(Hexalith.EventStore.Admin.Server.Controllers.AdminTenantsController).Assembly)
            .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(new TechnicalFeatureProvider()));
        customize?.Invoke(builder.Services);
        var app = builder.Build();
        try
        {
            app.MapControllers();
            var all = ((IEndpointRouteBuilder)app).DataSources.SelectMany(s => s.Endpoints).OfType<RouteEndpoint>().ToArray();
            var marked = all
                .Where(e => e.Metadata.GetMetadata<ControllerActionDescriptor>() is { } a
                    && (a.ControllerTypeInfo.Assembly.GetName().Name is "Hexalith.EventStore.Gateway" or "Hexalith.EventStore.Admin.Server")
                    && TechnicalHttpActionObservation.IsMarked(a)).ToArray();
            var filterTypes = marked.SelectMany(e => e.Metadata.GetMetadata<ControllerActionDescriptor>()!.FilterDescriptors)
                .Select(d => d.Filter).OfType<ServiceFilterAttribute>().Select(f => f.ServiceType).Distinct().ToArray();
            using (var scope = app.Services.CreateScope())
                foreach (var type in filterTypes) Require.That(scope.ServiceProvider.GetRequiredService(type).GetType() == type, "declared-service-filter-real-constructor-resolved");
            List<RouteEndpoint> eligible = []; Dictionary<string, int> refused = new(StringComparer.Ordinal);
            using var images = RouteCatalogImageDigestScope.Enter();
            var pass = new TechnicalHttpObservationPass(app.Services);
            foreach (var endpoint in marked)
            {
                try
                {
                    var description = TechnicalHttpActionObservation.Describe(endpoint, app.Services, pass, required: true);
                    Require.That(description is not null, "marked-describe-required-result"); eligible.Add(endpoint);
                }
                catch (RouteCatalogUnavailableException error) when (error.Message.StartsWith("technical-http-", StringComparison.Ordinal)
                    && error.Message.All(c => c is >= 'a' and <= 'z' or '-'))
                { refused[error.Message] = refused.GetValueOrDefault(error.Message) + 1; }
            }
            pass.Complete();
            var query = all.Single(e => e.Metadata.GetMetadata<ControllerActionDescriptor>()?.MethodInfo.Name == "SubmitBoundOwnerAuthorityAsync");
            var queryAction = query.Metadata.GetMetadata<ControllerActionDescriptor>()!;
            bool queryMarked = TechnicalHttpActionObservation.IsMarked(queryAction);
            var queryPass = new TechnicalHttpObservationPass(app.Services);
            bool queryEligible = TechnicalHttpActionObservation.Describe(query, app.Services, queryPass) is not null; queryPass.Complete();
            Require.That(!queryMarked && !queryEligible && TechnicalHttpActionObservation.IsCoreAction(queryAction), "owner-query-actual-unmarked-core-not-promoted");
            Require.That(marked.Length > 0 && eligible.Count > 0 && !app.Lifetime.ApplicationStarted.IsCancellationRequested, "no-start-nonempty-real-mvc-corpus");
            return new(app, marked, eligible.ToArray(), refused, queryMarked, queryEligible, filterTypes.Length);
        }
        catch { app.DisposeAsync().AsTask().GetAwaiter().GetResult(); throw; }
    }
    public ValueTask DisposeAsync() => _app.DisposeAsync();
}
