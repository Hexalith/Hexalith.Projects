using Hexalith.EventStore.Testing.CatalogInvocation;
using Hexalith.EventStore.Testing.OwnerSourceRead;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.ServiceDefaults.Authentication;

namespace Hexalith.Projects.PackageFixtures;

/// <summary>Uses the actual Tenants issuer and owner-bound key only for the selected original SDK request.</summary>
internal sealed class PackageOwnerCredentials(IWorkloadAssertionIssuer issuer, string key, TimeProvider clock)
    : IEventStoreGatewayWorkloadAssertionSource, IEventStoreGatewayStreamReadProofSource
{
    /// <inheritdoc/>
    public ValueTask<string?> IssueAsync(string operation, string tenant, CancellationToken cancellationToken)
        => ValueTask.FromResult<string?>(null);

    /// <inheritdoc/>
    public ValueTask<string?> IssueAsync(string operation, string tenant, string domain, CancellationToken cancellationToken)
        => operation == EventStoreGatewayWorkloadOperations.StreamRead && tenant == PackageOwnerSelection.Scope
            && domain == PackageOwnerSelection.Administrator
            ? issuer.IssueAsync(new("eventstore", operation, new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [EventStoreWorkloadAuthenticationDefaults.TenantBindingClaimType] = tenant,
                [EventStoreWorkloadAuthenticationDefaults.DomainBindingClaimType] = domain,
            }), cancellationToken)
            : ValueTask.FromResult<string?>(null);

    /// <inheritdoc/>
    public ValueTask<string?> IssueAsync(StreamReadRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<string?>(request.Tenant == PackageOwnerSelection.Scope
            && request.Domain == PackageOwnerSelection.Administrator && request.AggregateId == PackageOwnerSelection.Administrator
            ? BoundStreamReadProof.Issue(request, "tenants", key, clock.GetUtcNow()) : null);
    }
}
