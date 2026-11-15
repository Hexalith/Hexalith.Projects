namespace Hexalith.Projects.Contracts.Queries;

using System.ComponentModel;
using Hexalith.McpCli.Abstractions;

using System.Text.Json.Serialization;

/// <summary>Lists authorized Projects; authority comes exclusively from the authenticated envelope.</summary>
/// <param name="LifecycleState">Optional Active or Archived lifecycle filter.</param>
/// <param name="Cursor">The opaque continuation issued for this exact authorized view.</param>
/// <param name="PageSize">The requested number of rows, from one through 200.</param>
/// <param name="InspectDescriptiveMetadata">Requests separately authorized descriptive metadata.</param>
/// <param name="IncludeQuarantined">When true, returns only the separately audited Administrator quarantine inventory of unbound Active Projects; ordinary Projects are excluded.</param>
[method: JsonConstructor]
[HexalithQuery("List authorized Projects with one snapshot. Use payload paging: default 50, maximum 200; continue with the opaque cursor and unchanged filters and authority. Descriptive metadata requires a separate permission and durable audit.",
    Name = "list", Domain = "projects", WireType = QueryType, AggregateId = "projects",
    Example = """{"pageSize":50}""")]
public sealed record ListProjectsQuery(
    [property: Description("Optional Active or Archived filter; omitted includes both."), JsonPropertyName("lifecycleState")]
    string? LifecycleState = null,
    [property: Description("Opaque continuation for the same filters, tenant, actor and page size; omitted starts a new snapshot."), JsonPropertyName("cursor")]
    string? Cursor = null,
    [property: Description("Payload page size from 1 through 200; defaults to 50."), JsonPropertyName("pageSize")]
    int PageSize = 50,
    [property: Description("Request descriptive metadata; permission and durable inspection audit are required."), JsonPropertyName("inspectDescriptiveMetadata")]
    bool InspectDescriptiveMetadata = false,
    [property: Description("Request only the separately authorized and audited Administrator quarantine inventory."), JsonPropertyName("includeQuarantined")]
    bool IncludeQuarantined = false)
{
    /// <summary>Preserves the original constructor signature for existing consumers.</summary>
    /// <param name="LifecycleState">The lifecycle filter.</param>
    /// <param name="Cursor">The opaque continuation.</param>
    /// <param name="PageSize">The requested page size.</param>
    /// <param name="InspectDescriptiveMetadata">The descriptive inspection selection.</param>
    public ListProjectsQuery(string? LifecycleState, string? Cursor, int PageSize, bool InspectDescriptiveMetadata)
        : this(LifecycleState, Cursor, PageSize, InspectDescriptiveMetadata, false) { }

    /// <summary>Preserves the original positional deconstruction signature.</summary>
    /// <param name="LifecycleState">The lifecycle filter.</param>
    /// <param name="Cursor">The opaque continuation.</param>
    /// <param name="PageSize">The requested page size.</param>
    /// <param name="InspectDescriptiveMetadata">The descriptive inspection selection.</param>
    public void Deconstruct(out string? LifecycleState, out string? Cursor, out int PageSize, out bool InspectDescriptiveMetadata)
    {
        LifecycleState = this.LifecycleState;
        Cursor = this.Cursor;
        PageSize = this.PageSize;
        InspectDescriptiveMetadata = this.InspectDescriptiveMetadata;
    }

    /// <summary>The supported query discriminator.</summary>
    public const string QueryType = "Hexalith.Projects.Queries.ListProjects.v1";

    /// <summary>The independent list policy, preserving conversation paging defaults.</summary>
    public const int MaximumPageSize = 200;
}
