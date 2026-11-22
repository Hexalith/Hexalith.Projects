namespace Hexalith.Projects.Server.Queries;

using Hexalith.EventStore.Contracts.Queries;

/// <summary>Adapter to the platform's durable transition authority for an authenticated task poll.</summary>
public interface IProjectTaskStatusAuthority
{
    /// <summary>Checks the bound Tenant, original actor, action and exact task before reading committed status.</summary>
    /// <param name="caller">The authenticated query envelope, including its exact task target and delegation.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>Null for either an inaccessible or missing task. Dependency failure must be reported as unavailable.</returns>
    Task<ProjectAuthorizedTaskObservation?> ReadAuthorizedAsync(QueryEnvelope caller, CancellationToken cancellationToken);
}
