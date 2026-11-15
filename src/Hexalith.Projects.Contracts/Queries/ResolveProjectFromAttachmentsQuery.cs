namespace Hexalith.Projects.Contracts.Queries;

using System.ComponentModel;
using System.Text.Json.Serialization;
using Hexalith.McpCli.Abstractions;

/// <summary>Suggests Projects from at most 32 raw owner references. File mapping is unsupported.</summary>
/// <param name="Attachments">The presented opaque references, counted before deduplication.</param>
/// <param name="IncludeArchived">Whether authorized archived candidates may qualify.</param>
[HexalithQuery("Trace unselected advisory Project candidates from at most 32 opaque owner references, counted before deduplication. File mapping is unsupported. Coverage and freshness never grant selection or confirmation authority.",
    Name = "trace-attachments", Domain = "projects", WireType = QueryType, AggregateId = "projects",
    Example = """{"attachments":[{"referenceKind":"folder","referenceId":"folder-a"}]}""")]
public sealed record ResolveProjectFromAttachmentsQuery(
    [property: Description("At most 32 opaque folder or file owner references, counted before deduplication; file mapping is unsupported."), JsonPropertyName("attachments")]
    IReadOnlyList<ProjectAdvisoryAttachment> Attachments,
    [property: Description("Include authorized Archived candidates; defaults to false."), JsonPropertyName("includeArchived")]
    bool IncludeArchived = false)
{
    /// <summary>The supported query discriminator.</summary>
    public const string QueryType = "Hexalith.Projects.Queries.ResolveProjectFromAttachments.v1";
}
