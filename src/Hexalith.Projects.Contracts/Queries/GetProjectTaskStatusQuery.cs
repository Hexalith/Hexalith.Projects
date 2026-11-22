namespace Hexalith.Projects.Contracts.Queries;

using Hexalith.McpCli.Abstractions;

/// <summary>Reads current status for one opaque Durable Task identified by the envelope target.</summary>
[HexalithQuery("Inspect one Durable Task using --aggregate-id. Task status requires the platform transition authority.",
    Name = "task-status", Domain = "projects", WireType = QueryType, Example = """{}""")]
[Hexalith.EventStore.Contracts.Queries.QueryResultContract(typeof(ProjectTaskStatusResponse), "projects", "Hexalith.Projects.Queries.GetProjectTaskStatus.v1")]
public sealed record GetProjectTaskStatusQuery
{
    /// <summary>The supported query discriminator.</summary>
    public const string QueryType = "Hexalith.Projects.Queries.GetProjectTaskStatus.v1";
}
