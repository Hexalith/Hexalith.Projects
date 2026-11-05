namespace Hexalith.Projects.Contracts.Queries;

using System.Text.Json.Serialization;

/// <summary>Opens one Project bound to the envelope target, without paging.</summary>
/// <param name="InspectDescriptiveMetadata">Requests separately authorized descriptive metadata.</param>
/// <param name="InspectQuarantined">Requests the narrowly scoped Administrator inspection of an unbound Active Project.</param>
[method: JsonConstructor]
public sealed record GetProjectQuery(bool InspectDescriptiveMetadata = false, bool InspectQuarantined = false)
{
    /// <summary>Preserves the original constructor signature for existing consumers.</summary>
    /// <param name="InspectDescriptiveMetadata">Requests separately authorized descriptive metadata.</param>
    public GetProjectQuery(bool InspectDescriptiveMetadata) : this(InspectDescriptiveMetadata, false) { }

    /// <summary>Preserves the original positional deconstruction signature.</summary>
    /// <param name="InspectDescriptiveMetadata">The descriptive inspection selection.</param>
    public void Deconstruct(out bool InspectDescriptiveMetadata) => InspectDescriptiveMetadata = this.InspectDescriptiveMetadata;

    /// <summary>The supported query discriminator.</summary>
    public const string QueryType = "Hexalith.Projects.Queries.GetProject.v1";
}
