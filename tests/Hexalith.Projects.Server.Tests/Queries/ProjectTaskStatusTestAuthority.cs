namespace Hexalith.Projects.Server.Tests.Queries;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Projects.Server.Queries;

internal sealed class ProjectTaskStatusTestAuthority(Func<QueryEnvelope, ProjectAuthorizedTaskObservation?> read) : IProjectTaskStatusAuthority
{
    public int Calls { get; private set; }

    public Task<ProjectAuthorizedTaskObservation?> ReadAuthorizedAsync(QueryEnvelope caller, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult(read(caller));
    }
}
