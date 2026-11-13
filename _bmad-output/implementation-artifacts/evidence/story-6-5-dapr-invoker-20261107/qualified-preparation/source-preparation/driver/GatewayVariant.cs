namespace Story65.DaprInvokerProof;

using System.Reflection;
using Dapr.Client;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Server.DomainServices;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

internal sealed class GatewayVariant
{
    internal readonly IsolatedGatewayContext Context;
    internal readonly Assembly Assembly;
    internal readonly Type InvokerType;
    internal readonly MethodInfo Method;
    internal GatewayVariant(string name, string path)
    {
        Context = new(name); Assembly = Context.LoadFromAssemblyPath(Path.GetFullPath(path));
        InvokerType = Assembly.GetType("Hexalith.EventStore.Queries.DaprDomainQueryInvoker", true)!;
        Method = InvokerType.GetMethod("InvokeAsync", BindingFlags.Public | BindingFlags.Instance)!;
        Check.Require(Method.ReturnType == typeof(Task<QueryResult>), "Invoker Task result is not shared.");
        Check.Require(InvokerType.GetInterfaces().Single(i => i.FullName == "Hexalith.EventStore.Queries.IDomainQueryInvoker").Assembly == Assembly,
            "Gateway-owned invoker interface identity changed.");
        foreach (Type type in new[] { typeof(QueryResult), typeof(QueryEnvelope), typeof(IRouteCatalogAuthority), typeof(IDomainServiceResolver),
            typeof(DaprClient), typeof(IHttpClientFactory), typeof(ILogger<>), typeof(QueryCallerAuthorityBinding), typeof(RouteCatalogUnavailableException) })
            Check.Require(ReferenceEquals(Context.LoadFromAssemblyName(type.Assembly.GetName()), type.Assembly), "A shared dependency Assembly was cloned.");
        Check.Require(typeof(NullLogger<>).MakeGenericType(InvokerType).GetField("Instance", BindingFlags.Public | BindingFlags.Static)!.GetValue(null) is not null,
            "Actual variant NullLogger.Instance field was unavailable.");
    }
    internal object Create(IRouteCatalogAuthority? authority, DaprClient dapr, IHttpClientFactory clients, IDomainServiceResolver resolver, FixtureState state)
    {
        object logger = Activator.CreateInstance(typeof(ProbeLogger<>).MakeGenericType(InvokerType), state)!;
        return authority is null
            ? InvokerType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single().Invoke([dapr, clients, resolver, logger, null])
            : InvokerType.GetConstructors(BindingFlags.Instance | BindingFlags.Public).Single().Invoke([authority, dapr, clients, resolver, logger]);
    }
    internal Task<QueryResult> Invoke(object invoker, QueryEnvelope query, CancellationToken token)
        => (Task<QueryResult>)Method.Invoke(invoker, [query, token])!;
    internal static void CompareIdentities(GatewayVariant ordinary, GatewayVariant candidate)
    {
        Check.Require(!ReferenceEquals(ordinary.Assembly, candidate.Assembly) && !ReferenceEquals(ordinary.InvokerType, candidate.InvokerType), "Gateway variants share an instance.");
        Check.Require(ordinary.Assembly.FullName == candidate.Assembly.FullName && ordinary.InvokerType.FullName == candidate.InvokerType.FullName, "Gateway public identity changed.");
        Check.Require(!System.Runtime.Loader.AssemblyLoadContext.Default.Assemblies.Any(a => a.GetName().Name == "Hexalith.EventStore.Gateway"), "Gateway entered Default context.");
    }
}
