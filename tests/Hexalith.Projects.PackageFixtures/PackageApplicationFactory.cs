namespace Hexalith.Projects.PackageFixtures;

using Aspire.Hosting;
using Aspire.Hosting.Testing;

/// <summary>Captures the application through the published protected callback for health waits and teardown.</summary>
internal sealed class PackageApplicationFactory(Type entryPoint, string[] arguments)
    : DistributedApplicationFactory(entryPoint, arguments)
{
    internal DistributedApplication? Application { get; private set; }

    /// <inheritdoc/>
    protected override void OnBuilt(DistributedApplication application)
    {
        base.OnBuilt(application);
        Application = application;
    }
}
