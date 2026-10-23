namespace Hexalith.Projects.Server.Tests.Queries;

using Hexalith.EventStore.ServiceDefaults.Authentication;

/// <summary>Records the outbound workload audience without contacting an identity provider.</summary>
/// <param name="assertion">The configured assertion, or null to simulate unavailable credentials.</param>
internal sealed class OwnerQueryTestAssertionIssuer(string? assertion) : IWorkloadAssertionIssuer
{
    /// <summary>Gets every requested receiving audience and operation.</summary>
    public List<WorkloadAssertionRequest> Requests { get; } = [];

    /// <inheritdoc/>
    public bool CanBindResources => true;

    /// <inheritdoc/>
    public ValueTask<string?> IssueAsync(WorkloadAssertionRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Requests.Add(request);
        return ValueTask.FromResult(assertion);
    }
}
