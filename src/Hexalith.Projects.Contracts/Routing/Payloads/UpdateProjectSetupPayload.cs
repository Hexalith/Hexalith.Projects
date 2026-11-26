namespace Hexalith.Projects.Server;

using Hexalith.Projects.Contracts.Models;

/// <summary>The exact payload bound by the existing UpdateProjectSetup processor branch.</summary>
/// <param name="RequestSchemaVersion">The existing payload schema version.</param>
/// <param name="Setup">The typed Project Setup.</param>
internal sealed record UpdateProjectSetupPayload(
    string? RequestSchemaVersion,
    ProjectSetup? Setup);
