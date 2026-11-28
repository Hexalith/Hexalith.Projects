namespace Hexalith.Projects.Contracts.Queries;

using System.ComponentModel;
using System.Text.Json.Serialization;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.McpCli.Abstractions;

/// <summary>Lists authorized Projects using EventStore envelope pagination.</summary>
/// <param name="LifecycleState">Optional Active or Archived lifecycle filter.</param>
/// <param name="InspectDescriptiveMetadata">Requests separately authorized descriptive metadata.</param>
/// <param name="IncludeQuarantined">Requests only the separately audited Administrator inventory of unbound Active Projects.</param>
[HexalithQuery("List authorized Projects with one snapshot. Use envelope page size and cursor, keeping filters and authority unchanged between pages.",
    Name = "list", Domain = "projects", WireType = QueryType, AggregateId = "projects", Example = "{}")]
[QueryResultContract(typeof(ProjectListDocument), "projects", QueryType)]
public sealed record ListProjectsV2Query(
    [property: Description("Optional Active or Archived filter. Omission includes both."), JsonPropertyName("lifecycleState")]
    string? LifecycleState = null,
    [property: Description("Request descriptive metadata. Permission and durable audit are required."), JsonPropertyName("inspectDescriptiveMetadata")]
    bool InspectDescriptiveMetadata = false,
    [property: Description("Request only the separately authorized and audited Administrator quarantine inventory."), JsonPropertyName("includeQuarantined")]
    bool IncludeQuarantined = false)
{
    /// <summary>The query discriminator for the envelope-paged contract.</summary>
    public const string QueryType = "Hexalith.Projects.Queries.ListProjects.v2";
}
