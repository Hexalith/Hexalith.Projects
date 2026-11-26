namespace Hexalith.Projects.Server;

using Hexalith.Projects.Contracts.Models;

/// <summary>The exact payload bound by the existing LinkMemory processor branch.</summary>
/// <param name="RequestSchemaVersion">The existing payload schema version.</param>
/// <param name="Operation">The existing operation discriminator.</param>
/// <param name="ProjectId">The opaque Project identity repeated by the existing payload.</param>
/// <param name="MemoryReferenceId">The opaque Memory reference identity.</param>
/// <param name="MemoryMetadata">The validated Projects-owned Memory metadata.</param>
internal sealed record LinkMemoryPayload(
    string? RequestSchemaVersion,
    string? Operation,
    string? ProjectId,
    string? MemoryReferenceId,
    ProjectMemoryReferenceMetadata? MemoryMetadata);
