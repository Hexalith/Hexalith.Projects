namespace Hexalith.Projects.Contracts.Queries;

using System.Text.Json.Serialization;

/// <summary>Lists authorized Projects; authority comes exclusively from the authenticated envelope.</summary>
/// <param name="LifecycleState">Optional Active or Archived lifecycle filter.</param>
/// <param name="Cursor">The opaque continuation issued for this exact authorized view.</param>
/// <param name="PageSize">The requested number of rows, from one through 200.</param>
/// <param name="InspectDescriptiveMetadata">Requests separately authorized descriptive metadata.</param>
/// <param name="IncludeQuarantined">Selects the separately audited Administrator inventory of unbound Active Projects.</param>
[method: JsonConstructor]
public sealed record ListProjectsQuery(
    string? LifecycleState = null,
    string? Cursor = null,
    int PageSize = 50,
    bool InspectDescriptiveMetadata = false,
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
