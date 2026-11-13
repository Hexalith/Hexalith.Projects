using System.Reflection;
using System.Runtime.ExceptionServices;
namespace Hexalith.PrivateDomainReceiverFixture;

/// <summary>Reuses only the existing compiled catalog setup, before any business measurement window.</summary>
internal static class FixtureReflection
{
    internal static object Catalog(bool schemaThree)
    {
        Type type = Assembly.Load("Hexalith.EventStore.Server.Tests").GetType("Hexalith.EventStore.Server.Tests.Routing.RouteCatalogTestFixture", true)!;
        try { return type.GetConstructors().Single().Invoke([null, null, schemaThree, null]); }
        catch (TargetInvocationException error) when (error.InnerException is not null) { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
    }
    internal static T Property<T>(object target, string name) => (T)target.GetType().GetProperty(name)!.GetValue(target)!;
    internal static T Invoke<T>(object target, string name)
    {
        MethodInfo method = target.GetType().GetMethod(name)!;
        try { return (T)method.Invoke(target, method.GetParameters().Select(p => p.DefaultValue).ToArray())!; }
        catch (TargetInvocationException error) when (error.InnerException is not null) { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
    }
}
