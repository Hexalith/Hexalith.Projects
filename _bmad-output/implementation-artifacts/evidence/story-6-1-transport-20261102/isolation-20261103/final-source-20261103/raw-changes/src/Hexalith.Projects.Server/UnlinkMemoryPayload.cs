namespace Hexalith.Projects.Server;

using System.Text.Json.Serialization;

using Hexalith.Projects.Contracts.Models;

/// <summary>The exact payload bound by the existing UnlinkMemory processor branch.</summary>
/// <param name="RequestSchemaVersion">The existing payload schema version.</param>
/// <param name="Operation">The existing operation discriminator.</param>
/// <param name="UnlinkIntent">The explicit unlink intent.</param>
/// <param name="ProjectId">The opaque Project identity repeated by the existing payload.</param>
/// <param name="MemoryReferenceId">The opaque Memory reference identity.</param>
internal sealed record UnlinkMemoryPayload(
    string? RequestSchemaVersion,
    string? Operation,
    string? UnlinkIntent,
    string? ProjectId,
    string? MemoryReferenceId);
