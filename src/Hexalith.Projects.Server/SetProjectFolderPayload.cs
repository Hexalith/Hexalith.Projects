namespace Hexalith.Projects.Server;

using Hexalith.Projects.Contracts.Models;

/// <summary>The exact payload bound by the existing SetProjectFolder processor branch.</summary>
/// <param name="RequestSchemaVersion">The existing payload schema version.</param>
/// <param name="Operation">The existing operation discriminator.</param>
/// <param name="ProjectId">The opaque Project identity repeated by the existing payload.</param>
/// <param name="FolderId">The opaque Folder identity.</param>
/// <param name="FolderMetadata">The validated Projects-owned Folder metadata.</param>
/// <param name="ReplacementConfirmed">Whether replacing the Folder was explicitly confirmed.</param>
internal sealed record SetProjectFolderPayload(
    string? RequestSchemaVersion,
    string? Operation,
    string? ProjectId,
    string? FolderId,
    ProjectFolderMetadata? FolderMetadata,
    bool ReplacementConfirmed);
