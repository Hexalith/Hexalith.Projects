namespace Hexalith.Projects.Contracts.Queries;

/// <summary>Opens one Project bound to the envelope target, without paging.</summary>
/// <param name="InspectDescriptiveMetadata">Requests separately authorized descriptive metadata.</param>
public sealed record GetProjectQuery(bool InspectDescriptiveMetadata = false)
{
    /// <summary>The supported query discriminator.</summary>
    public const string QueryType = "Hexalith.Projects.Queries.GetProject.v1";
}
