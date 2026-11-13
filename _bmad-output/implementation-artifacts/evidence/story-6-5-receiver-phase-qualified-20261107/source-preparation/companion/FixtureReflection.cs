using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Hexalith.PrivateReceiverFixture;

/// <summary>Reuses the exact compiled owner fixtures within the same coherent Default context.</summary>
internal static class FixtureReflection
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    /// <summary>Creates an existing internal fixture with its exact constructor arguments.</summary>
    internal static object Create(string shortName, params object?[] arguments)
    {
        Assembly tests = AppDomain.CurrentDomain.GetAssemblies().Single(assembly => assembly.GetName().Name == "Hexalith.EventStore.Server.Tests");
        Type type = tests.GetType("Hexalith.EventStore.Server.Tests.Routing." + shortName, throwOnError: true)!;
        try { return Activator.CreateInstance(type, Members, binder: null, args: arguments, culture: null)!; }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
    }

    /// <summary>Reads one fixed fixture property.</summary>
    internal static T Property<T>(object instance, string name)
        => (T)instance.GetType().GetProperty(name, Members)!.GetValue(instance)!;

    /// <summary>Reads one fixed fixture field.</summary>
    internal static T Field<T>(object instance, string name)
        => (T)instance.GetType().GetField(name, Members)!.GetValue(instance)!;

    /// <summary>Writes a fixed synthetic fixture callback property.</summary>
    internal static void Set(object instance, string name, object value)
        => instance.GetType().GetProperty(name, Members)!.SetValue(instance, value);

    /// <summary>Invokes an existing method using literal argument names and its actual optional defaults.</summary>
    internal static object? Invoke(object instance, string name, IReadOnlyDictionary<string, object?>? arguments = null)
    {
        MethodInfo method = instance.GetType().GetMethod(name, Members)!;
        object?[] values = method.GetParameters().Select(parameter => arguments?.TryGetValue(parameter.Name!, out object? value) == true
            ? value : parameter.HasDefaultValue ? parameter.DefaultValue : throw new FixtureFailure("missing-fixed-fixture-argument")).ToArray();
        try { return method.Invoke(instance, values); }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
    }

    /// <summary>Disposes an existing fixture through its shared framework interface.</summary>
    internal static async Task DisposeAsync(object? fixture)
    {
        if (fixture is IAsyncDisposable disposable) await disposable.DisposeAsync().ConfigureAwait(false);
    }
}
