namespace Hexalith.Projects.Server;

using System.Text.Json.Serialization;

using Hexalith.Projects.Contracts.Models;

/// <summary>The exact payload bound by the existing RestoreProject processor branch.</summary>
/// <param name="RequestSchemaVersion">The existing payload schema version.</param>
/// <param name="RestoreIntent">The explicit restore intent.</param>
internal sealed record RestoreProjectPayload(
    [property: JsonRequired] string? RequestSchemaVersion,
    [property: JsonRequired] string? RestoreIntent);
