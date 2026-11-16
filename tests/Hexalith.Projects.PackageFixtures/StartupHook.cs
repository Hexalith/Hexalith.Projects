using System.Reflection;
using System.Runtime.Loader;

/// <summary>Resolves only the fixture's prebuilt dependencies before ASP.NET Core loads its explicit hosting startup.</summary>
public static class StartupHook
{
    /// <summary>Runs only for explicitly enrolled private fixture processes.</summary>
    public static void Initialize()
    {
        if (Environment.GetEnvironmentVariable("HEXALITH_PACKAGE_STARTUP") != "1")
            throw new InvalidOperationException("Package hosting startup requires its explicit private launch.");
        AssemblyDependencyResolver resolver = new(typeof(StartupHook).Assembly.Location);
        AssemblyLoadContext.Default.Resolving += (context, name) => resolver.ResolveAssemblyToPath(name) is { } path
            ? context.LoadFromAssemblyPath(path) : null;
    }
}
