namespace Hexalith.Projects.PackageFixtures.AppHost;

using global::Aspire.Hosting;
using global::Aspire.Hosting.ApplicationModel;
using global::Aspire.Hosting.Dotnet;

/// <summary>Loads the explicitly enabled fixture adapters into the original prebuilt application entrypoints.</summary>
internal static class PackageHostingStartupComposition
{
    internal static void Add(string root, string directory, params IResourceBuilder<DotnetProjectResource>[] resources)
    {
        string assembly = Path.Combine(root, "tests/Hexalith.Projects.PackageFixtures/bin/Release/net11.0/Hexalith.Projects.PackageFixtures.dll");
        if (!File.Exists(assembly)) throw new InvalidOperationException("Build the exact Release fixture adapter before startup.");
        foreach (IResourceBuilder<DotnetProjectResource> resource in resources)
            _ = resource.WithEnvironment("DOTNET_STARTUP_HOOKS", assembly)
                .WithEnvironment("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES", "Hexalith.Projects.PackageFixtures")
                .WithEnvironment("HEXALITH_PACKAGE_STARTUP", "1")
                .WithEnvironment("HEXALITH_PACKAGE_ROLE", resource.Resource.Name)
                .WithEnvironment("HEXALITH_PACKAGE_DIRECTORY", directory);
    }
}
