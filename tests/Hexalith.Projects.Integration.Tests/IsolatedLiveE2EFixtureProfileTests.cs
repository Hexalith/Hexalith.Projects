// <copyright file="IsolatedLiveE2EFixtureProfileTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Integration.Tests;

using System.Text.Json;

using Hexalith.Projects.AppHost;

using Microsoft.Extensions.Configuration;

using Shouldly;

using Xunit;

/// <summary>Exercises admission independently of Aspire startup or shared infrastructure.</summary>
public sealed class IsolatedLiveE2EFixtureProfileTests : IDisposable
{
    private const string Prefix = "Projects:IsolatedLiveFixtures:";
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "isolated-profile-" + Guid.NewGuid().ToString("N"));
    private readonly Dictionary<string, string?> _values;

    /// <summary>Creates only private disposable trust and discovery inputs.</summary>
    public IsolatedLiveE2EFixtureProfileTests()
    {
        _ = Directory.CreateDirectory(_directory);
        _values = new()
        {
            ["EnableKeycloak"] = "false",
            ["KeycloakPersistent"] = "false",
            [Prefix + "IsolationId"] = "fixtures-0123456789abcdef01234567",
            [Prefix + "UserIssuer"] = "https://127.0.0.1:12345/realms/fixture",
            ["Dapr:RedisHost"] = "127.0.0.1:12346",
        };
        foreach (string key in new[] { "CertificatePath", "TrustBundle", "DiscoveryConfiguration" })
        {
            string path = Path.Combine(_directory, key + ".json");
            File.WriteAllText(path, key == "DiscoveryConfiguration"
                ? JsonSerializer.Serialize(new { spec = new { nameResolution = new { component = "sqlite", version = "v1",
                    configuration = new { connectionString = Path.Combine(_directory, "discovery.sqlite") } } } })
                : "private-test-input");
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            _values[Prefix + key] = path;
        }
        foreach (string key in new[] { "CertificatePassword", "CatalogDigestKey", "EventStoreSecret", "ProjectsSecret", "TenantsSecret",
            "OwnerPrivateKey", "OwnerPublicKey", "TenantOwnerPrivateKey", "TenantOwnerPublicKey", "ReceiptKey", "BootstrapPassword",
            "Tenant", "ForeignTenant", "Browser:PrimaryActor", "Browser:ClientSecret" })
            _values[Prefix + key] = "private-test-value";
    }

    /// <summary>Accepts a fresh private profile with the SDK's exact explicit disable value.</summary>
    [Theory]
    [InlineData("false")]
    [InlineData("FALSE")]
    public void ShouldAcceptExplicitPrivateConfiguration(string security)
    {
        _values["EnableKeycloak"] = security;
        IsolatedLiveE2EFixtureProfile.Validate(Configuration());
    }

    /// <summary>Rejects absent, enabled, malformed and SDK-incompatible padded disable values.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("true")]
    [InlineData(" false ")]
    [InlineData("invalid")]
    public void ShouldRequireExactExplicitOrdinarySecurityDisable(string? security)
    {
        _values["EnableKeycloak"] = security;
        Should.Throw<InvalidOperationException>(() => IsolatedLiveE2EFixtureProfile.Validate(Configuration()));
    }

    /// <summary>Rejects shared Redis and mixed security/owner profiles.</summary>
    [Theory]
    [InlineData("Dapr:RedisHost", "127.0.0.1:6379")]
    [InlineData("Dapr:RedisHost", "192.0.2.1:12346")]
    [InlineData("Projects:SupportedReads:Enabled", "true")]
    [InlineData("KeycloakPersistent", "true")]
    public void ShouldRejectSharedOrMixedConfiguration(string key, string value)
    {
        _values[key] = value;
        Should.Throw<InvalidOperationException>(() => IsolatedLiveE2EFixtureProfile.Validate(Configuration()));
    }

    /// <summary>Rejects a discovery registry which already has an owner.</summary>
    [Fact]
    public void ShouldRejectReusedDiscovery()
    {
        File.WriteAllText(Path.Combine(_directory, "discovery.sqlite"), "already-owned");
        Should.Throw<InvalidOperationException>(() => IsolatedLiveE2EFixtureProfile.Validate(Configuration()));
    }

    /// <inheritdoc />
    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(_values).Build();
}
