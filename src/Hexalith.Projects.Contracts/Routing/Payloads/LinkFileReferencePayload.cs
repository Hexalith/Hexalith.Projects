namespace Hexalith.Projects.Server;

using Hexalith.Projects.Contracts.Models;

/// <summary>The exact payload bound by the existing LinkFileReference processor branch.</summary>
/// <param name="RequestSchemaVersion">The existing payload schema version.</param>
/// <param name="Operation">The existing operation discriminator.</param>
/// <param name="ProjectId">The opaque Project identity repeated by the existing payload.</param>
/// <param name="FileReferenceId">The opaque File reference identity.</param>
/// <param name="FolderId">The opaque Folder identity.</param>
/// <param name="FileMetadata">The validated Projects-owned File metadata.</param>
internal sealed record LinkFileReferencePayload(
    string? RequestSchemaVersion,
    string? Operation,
    string? ProjectId,
    string? FileReferenceId,
    string? FolderId,
    ProjectFileReferenceMetadata? FileMetadata);
