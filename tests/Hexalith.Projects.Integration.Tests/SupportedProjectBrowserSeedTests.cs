#if SUPPORTED_PROJECT_READ_DISTRIBUTED
namespace Hexalith.Projects.Integration.Tests;

using Xunit;

/// <summary>Seeds real persisted owners using the launcher's actual Keycloak credentials.</summary>
public sealed class SupportedProjectBrowserSeedTests
{
    /// <summary>Completes setup before the isolated browser phase observes reads and separate audits.</summary>
    [Fact]
    public async Task SeedRealOidcBrowserFixture()
    {
        using SupportedProjectReadDistributedFixture fixture = new();
        fixture.RequireScenario("main");
        await fixture.SeedAsync();
    }
}
#endif
