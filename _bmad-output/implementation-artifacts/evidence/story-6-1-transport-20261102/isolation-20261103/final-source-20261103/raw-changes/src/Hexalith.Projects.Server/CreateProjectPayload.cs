namespace Hexalith.Projects.Server;

using System.Text.Json.Serialization;

using Hexalith.Projects.Contracts.Models;

/// <summary>The exact payload bound by the existing CreateProject processor branch.</summary>
/// <param name="Name">The caller-supplied Project name.</param>
/// <param name="Description">The optional Project description.</param>
/// <param name="SetupMetadata">The existing opaque Setup metadata reference.</param>
internal sealed record CreateProjectPayload(
    [property: JsonRequired] string? Name,
    string? Description,
    string? SetupMetadata);
