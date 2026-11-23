namespace Hexalith.Projects.PackageFixtures.AppHost;

using global::Aspire.Hosting;
using global::Aspire.Hosting.ApplicationModel;
using global::Aspire.Hosting.Dotnet;

internal static class PrebuiltProjectExtensions
{
    internal static IResourceBuilder<DotnetProjectResource> AddPrebuiltProject(
        this IDistributedApplicationBuilder builder, string name, string projectPath, bool withHttps = false)
        => builder.AddPrebuiltProject(name, PrebuiltMetadata.ForProject(projectPath), withHttps);

    internal static IResourceBuilder<DotnetProjectResource> AddPrebuiltProject(
        this IDistributedApplicationBuilder builder, string name, PrebuiltMetadata metadata, bool withHttps = false)
    {
        string assembly = metadata.ResolveReleaseAssembly();
        var project = new DotnetProjectResource(name, metadata.BuildWorkingDirectory);
        var resource = builder.AddResource(project).WithAnnotation(metadata).WithArgs(assembly)
            .WithOtlpExporter().WithHttpEndpoint();
        var httpPort = resource.GetEndpoint("http").Property(EndpointProperty.TargetPort);
        if (!withHttps)
            return resource.WithEnvironment("ASPNETCORE_URLS", ReferenceExpression.Create($"http://127.0.0.1:{httpPort}"));

        resource.WithHttpsEndpoint();
        var https = resource.GetEndpoint("https");
        var httpsPort = https.Property(EndpointProperty.TargetPort);
        return resource.WithEnvironment("ASPNETCORE_URLS", ReferenceExpression.Create($"http://127.0.0.1:{httpPort};https://127.0.0.1:{httpsPort}"))
            .WithEnvironment("ASPNETCORE_HTTPS_PORT", https.Property(EndpointProperty.Port));
    }
}
