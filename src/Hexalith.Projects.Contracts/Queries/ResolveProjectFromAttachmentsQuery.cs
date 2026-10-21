namespace Hexalith.Projects.Contracts.Queries;

/// <summary>Suggests Projects from at most 32 raw owner references. File mapping is unsupported.</summary>
/// <param name="Attachments">The presented opaque references, counted before deduplication.</param>
/// <param name="IncludeArchived">Whether authorized archived candidates may qualify.</param>
public sealed record ResolveProjectFromAttachmentsQuery(IReadOnlyList<ProjectAdvisoryAttachment> Attachments, bool IncludeArchived = false)
{
    /// <summary>The supported query discriminator.</summary>
    public const string QueryType = "Hexalith.Projects.Queries.ResolveProjectFromAttachments.v1";
}
