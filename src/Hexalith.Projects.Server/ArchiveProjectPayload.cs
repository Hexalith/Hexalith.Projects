namespace Hexalith.Projects.Server;

using System.Text.Json.Serialization;

using Hexalith.Projects.Contracts.Models;

/// <summary>The exact payload bound by the existing ArchiveProject processor branch.</summary>
/// <param name="ArchiveIntent">The explicit archive intent.</param>
/// <param name="RequestSchemaVersion">The existing payload schema version.</param>
internal sealed record ArchiveProjectPayload(
    [property: JsonRequired] string? ArchiveIntent,
    [property: JsonRequired] string? RequestSchemaVersion);
