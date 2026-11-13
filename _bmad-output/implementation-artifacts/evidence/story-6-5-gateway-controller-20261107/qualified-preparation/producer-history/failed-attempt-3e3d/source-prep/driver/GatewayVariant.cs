namespace Story65.GatewayControllerProof;

using System.Reflection;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Server.Pipeline.Queries;
using Hexalith.EventStore.Server.Queries;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

internal sealed class GatewayVariant
{
    internal readonly IsolatedGatewayContext Context;
    internal readonly Assembly Assembly;
    internal readonly Type ControllerType;
    internal readonly MethodInfo Action;
    internal readonly object AdmissionKey;
    internal const string PrevalidatedKey = "__EventStorePrevalidatedAuthorizationContext";
    internal GatewayVariant(string name, string path)
    {
        Context = new(name); Assembly = Context.LoadFromAssemblyPath(Path.GetFullPath(path));
        ControllerType = Assembly.GetType("Hexalith.EventStore.Controllers.QueriesController", throwOnError: true)!;
        Action = ControllerType.GetMethod("SubmitBoundOwnerAuthorityAsync", BindingFlags.Instance | BindingFlags.Public)!;
        AdmissionKey = ControllerType.GetField("BoundOwnerCatalogAdmissionKey", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        Check.Require(ControllerType.BaseType == typeof(ControllerBase) && Action.ReturnType == typeof(Task<IActionResult>), "Controller base/Task result is not shared.");
        foreach (var type in new[] { typeof(BoundOwnerQueryRequest), typeof(SubmitQuery), typeof(IRouteCatalogAuthority), typeof(IMediator), typeof(ControllerBase), typeof(WorkloadAssertionValidator) })
            Check.Require(ReferenceEquals(Context.LoadFromAssemblyName(type.Assembly.GetName()), type.Assembly), "A dependency Assembly was cloned.");
    }
    internal ControllerBase Create(IRouteCatalogAuthority authority, IMediator mediator, IETagService tags,
        IConfiguration configuration, WorkloadAssertionValidator validator, IOptionsMonitor<WorkloadAuthenticationOptions> options, HttpContext http)
    {
        object tenant = Activator.CreateInstance(Assembly.GetType("Hexalith.EventStore.Authorization.ClaimsTenantValidator", true)!)!;
        object rbac = Activator.CreateInstance(Assembly.GetType("Hexalith.EventStore.Authorization.ClaimsRbacValidator", true)!)!;
        Type loggerType = typeof(NullLogger<>).MakeGenericType(ControllerType);
        object logger = loggerType.GetField("Instance", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        object[] arguments = [authority, mediator, tags, tenant, rbac, logger, null!, configuration, validator, options];
        var controller = (ControllerBase)ControllerType.GetConstructors().Single().Invoke(arguments);
        controller.ControllerContext = new() { HttpContext = http };
        http.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new ControllerActionDescriptor
        { ControllerTypeInfo = ControllerType.GetTypeInfo(), MethodInfo = Action, ActionName = Action.Name }), "synthetic-bound-owner-action"));
        return controller;
    }
    internal Task<IActionResult> Invoke(ControllerBase controller, BoundOwnerQueryRequest request, CancellationToken token)
        => (Task<IActionResult>)Action.Invoke(controller, [request, token])!;
    internal bool HasAdmission(HttpContext http, SubmitQuery request, CommittedRouteCatalogSnapshot snapshot)
        => (bool)ControllerType.GetMethod("HasVerifiedBoundOwnerCatalogAdmission", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [http, request, snapshot])!;
    internal static void CompareIdentities(GatewayVariant ordinary, GatewayVariant instrumented)
    {
        Check.Require(!ReferenceEquals(ordinary.Assembly, instrumented.Assembly) && !ReferenceEquals(ordinary.ControllerType, instrumented.ControllerType), "Gateway variants share an instance.");
        Check.Require(ordinary.Assembly.FullName == instrumented.Assembly.FullName && ordinary.ControllerType.FullName == instrumented.ControllerType.FullName, "Gateway public identity changed.");
        Check.Require(!System.Runtime.Loader.AssemblyLoadContext.Default.Assemblies.Any(a => a.GetName().Name == "Hexalith.EventStore.Gateway"), "Gateway entered the default context.");
    }
}
