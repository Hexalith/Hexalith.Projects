#if SUPPORTED_PROJECT_READ_DISTRIBUTED
namespace Hexalith.Projects.Integration.Tests;

using Xunit;

/// <summary>Initializes an owned compatibility backend using real authenticated owner contracts.</summary>
public sealed class IsolatedLiveFixtureSeedTests
{
    /// <summary>Creates fresh source authority without modifying a shared tenant or projection.</summary>
    [Fact]
    public async Task SeedOwnedCompatibilityFixture()
    {
        using SupportedProjectReadDistributedFixture fixture = new();
        await fixture.SeedIsolatedLiveFixturesAsync();
    }
}
#endif
