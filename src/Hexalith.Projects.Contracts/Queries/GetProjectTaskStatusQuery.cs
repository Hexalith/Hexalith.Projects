namespace Hexalith.Projects.Contracts.Queries;

/// <summary>Reads current status for one opaque Durable Task identified by the envelope target.</summary>
[Hexalith.EventStore.Contracts.Queries.QueryResultContract(typeof(ProjectTaskStatusResponse), "projects", "Hexalith.Projects.Queries.GetProjectTaskStatus.v1")]
public sealed record GetProjectTaskStatusQuery
{
    /// <summary>The supported query discriminator.</summary>
    public const string QueryType = "Hexalith.Projects.Queries.GetProjectTaskStatus.v1";
}
