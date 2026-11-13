namespace Story65.OutboundAssertionProof;

using System.Reflection;
using System.Threading.Tasks.Sources;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

internal sealed class ServerVariant
{
    internal readonly IsolatedServerContext Context;
    internal readonly Assembly Assembly;
    internal readonly Type HandlerType, LoggerType;
    private readonly MethodInfo send;
    internal ServerVariant(string name, string path)
    {
        Context = new(name); Assembly = Context.LoadFromAssemblyPath(Path.GetFullPath(path));
        HandlerType = Assembly.GetType("Hexalith.EventStore.Server.DomainServices.DomainServiceWorkloadAssertionHandler", true)!;
        LoggerType = typeof(ILogger<>).MakeGenericType(HandlerType);
        send = HandlerType.GetMethod("SendAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Check.Require(HandlerType.IsSealed && HandlerType.BaseType == typeof(DelegatingHandler)
            && send.ReturnType == typeof(Task<HttpResponseMessage>), "Actual Server handler identity/base/return changed.");
        foreach (Type type in new[] { typeof(HttpMessageHandler), typeof(IServiceProvider), typeof(IWorkloadAssertionIssuer),
            typeof(EventStoreDomainServiceRoute), typeof(ILogger<>), typeof(IValueTaskSource<>) })
            Check.Require(ReferenceEquals(Context.LoadFromAssemblyName(type.Assembly.GetName()), type.Assembly), "A nonServer dependency was cloned.");
        Check.Require(typeof(NullLogger<>).MakeGenericType(HandlerType).GetField("Instance", BindingFlags.Public | BindingFlags.Static)!.GetValue(null) is not null,
            "Actual typed NullLogger.Instance field was unavailable.");
    }
    internal HttpMessageHandler Create(FixtureState state)
    {
        object logger = Activator.CreateInstance(typeof(ProbeLogger<>).MakeGenericType(HandlerType), state)!;
        var provider = new ProbeProvider(state, LoggerType, logger);
        return (HttpMessageHandler)Activator.CreateInstance(HandlerType, provider)!;
    }
    internal Task<HttpResponseMessage> InvokeNull(HttpMessageHandler handler, CancellationToken token)
        => (Task<HttpResponseMessage>)send.Invoke(handler, [null, token])!;
    internal static void CompareIdentities(ServerVariant ordinary, ServerVariant candidate)
    {
        Check.Require(!ReferenceEquals(ordinary.Assembly, candidate.Assembly) && !ReferenceEquals(ordinary.HandlerType, candidate.HandlerType), "Server variants share an instance.");
        Check.Require(ordinary.Assembly.FullName == candidate.Assembly.FullName && ordinary.HandlerType.FullName == candidate.HandlerType.FullName, "Server public identity changed.");
        Check.Require(!System.Runtime.Loader.AssemblyLoadContext.Default.Assemblies.Any(a => a.GetName().Name == "Hexalith.EventStore.Server"), "Server entered Default context.");
    }
}
