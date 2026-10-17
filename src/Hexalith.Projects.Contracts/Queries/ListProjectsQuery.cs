namespace Hexalith.Projects.Contracts.Queries;

/// <summary>Lists authorized Projects; authority comes exclusively from the authenticated envelope.</summary>
/// <param name="LifecycleState">Optional Active or Archived lifecycle filter.</param>
/// <param name="Cursor">The opaque continuation issued for this exact authorized view.</param>
/// <param name="PageSize">The requested number of rows, from one through 200.</param>
/// <param name="InspectDescriptiveMetadata">Requests separately authorized descriptive metadata.</param>
public sealed record ListProjectsQuery(
    string? LifecycleState = null,
    string? Cursor = null,
    int PageSize = 50,
    bool InspectDescriptiveMetadata = false)
{
    /// <summary>The supported query discriminator.</summary>
    public const string QueryType = "Hexalith.Projects.Queries.ListProjects.v1";

    /// <summary>The independent list policy, preserving conversation paging defaults.</summary>
    public const int MaximumPageSize = 200;
}
