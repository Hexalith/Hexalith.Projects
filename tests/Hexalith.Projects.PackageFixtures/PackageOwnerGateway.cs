using Hexalith.EventStore.Testing.CatalogInvocation;
using Hexalith.EventStore.Testing.OwnerSourceRead;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.ServiceDefaults.Authentication;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Hexalith.Projects.PackageFixtures;

/// <summary>Independent SDK client within the real owner; production owner-source registrations are untouched.</summary>
internal sealed class PackageOwnerGateway
{
    private readonly EventStoreGatewayClient _client;

    /// <summary>Constructs the SDK with the actual owner issuer, bound key and catalog-fenced HTTP client.</summary>
    internal PackageOwnerGateway(HttpClient http, IWorkloadAssertionIssuer issuer, IConfiguration configuration, TimeProvider clock)
    {
        if (configuration["Authentication:WorkloadIssuer:Workload"] != "tenants"
            || configuration["Authentication:BoundStreamRead:PrivateKey"] is not { Length: > 0 } key)
            throw new InvalidOperationException("Private owner source requires the actual Tenants identity and bound key.");
        var credentials = new PackageOwnerCredentials(issuer, key, clock);
        _client = new(http, Options.Create(new EventStoreGatewayClientOptions
        {
            UseWorkloadTransport = true, MaxStreamReadResponseBytes = 4 * 1024 * 1024,
        }), credentials, credentials);
    }

    /// <summary>Dispatches the unchanged exact public source request through the SDK.</summary>
    internal Task<StreamReadPage> ReadAsync(StreamReadRequest request, CancellationToken cancellationToken)
        => _client.ReadWorkloadStreamAsync(request, cancellationToken);
}
