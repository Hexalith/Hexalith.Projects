namespace Hexalith.Projects.PackageFixtures;

using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;

using Aspire.Hosting;

/// <summary>Executes the prepared native assertion assembly without a compile-time dependency cycle or test skipping.</summary>
internal static class PackageNativeRunner
{
    private static Type? _entry;

    internal static async Task InvokeAsync(string operation, string root, FixtureInputs inputs, FixtureScratch scratch,
        PackageApplicationFactory factory, DistributedApplication application, string scenario, CancellationToken cancellationToken)
    {
        if (operation is not ("InitializeAsync" or "SeedAsync" or "RunAsync")) throw new InvalidOperationException("Unknown native assertion operation.");
        if (_entry is null)
        {
            string assembly = Path.Combine(root, "tests/Hexalith.Projects.Integration.Tests/bin/Release/net11.0/Hexalith.Projects.Integration.Tests.dll");
            if (!File.Exists(assembly)) throw new InvalidOperationException("The shared Release build must prepare the native assertion assembly.");
            AssemblyDependencyResolver resolver = new(assembly);
            AssemblyLoadContext.Default.Resolving += (context, name) => resolver.ResolveAssemblyToPath(name) is { } path
                ? context.LoadFromAssemblyPath(path) : null;
            _entry = AssemblyLoadContext.Default.LoadFromAssemblyPath(assembly).GetType("Hexalith.Projects.Integration.Tests.PackageReadScenarios")
                ?? throw new InvalidOperationException("Prepared native product scenarios are absent.");
        }
        MethodInfo method = _entry.GetMethod(operation, BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("Prepared native product entrypoint is absent.");
        try
        {
            Task execution = (Task)method.Invoke(null, [inputs, scratch, factory, application, scenario, cancellationToken])!;
            await execution.ConfigureAwait(false);
        }
        catch (TargetInvocationException failure) when (failure.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(failure.InnerException).Throw();
            throw;
        }
    }
}
