namespace Hexalith.Projects.Integration.Tests;

using global::Aspire.Hosting;

/// <summary>Metadata with no output directory for the no-rebuild failure control.</summary>
public sealed class MissingPackageFixtureMetadata : IProjectMetadata
{
    /// <summary>Gets a project path whose Release output does not exist.</summary>
    public string ProjectPath => Path.Combine(AppContext.BaseDirectory, "missing-package-output", "Fixture.csproj");
}
