namespace Hexalith.Projects.Contracts.Queries;

using System.ComponentModel;
using System.Text.Json.Serialization;

/// <summary>Reads a bounded page of committed audit metadata for the envelope Project.</summary>
/// <param name="Cursor">The continuation for this exact authorized view.</param>
/// <param name="PageSize">Number of facts to return, up to two hundred.</param>
[Hexalith.EventStore.Contracts.Queries.QueryResultContract(typeof(ProjectAuditPage), "projects", "Hexalith.Projects.Queries.GetProjectAudit.v1")]
public sealed record GetProjectAuditQuery(
    [property: Description("Opaque continuation from the previous page."), JsonPropertyName("cursor")] string? Cursor = null,
    [property: Description("Number of audit facts from 1 through 200."), JsonPropertyName("pageSize")] int PageSize = 50)
{
    /// <summary>The supported query discriminator.</summary>
    public const string QueryType = "Hexalith.Projects.Queries.GetProjectAudit.v1";
}
