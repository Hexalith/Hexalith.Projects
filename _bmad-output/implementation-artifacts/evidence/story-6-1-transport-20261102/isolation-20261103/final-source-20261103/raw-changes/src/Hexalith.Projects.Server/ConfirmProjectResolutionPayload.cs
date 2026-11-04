namespace Hexalith.Projects.Server;

using System.Text.Json.Serialization;

using Hexalith.Projects.Contracts.Models;

/// <summary>The exact payload bound by the existing ConfirmProjectResolution processor branch.</summary>
/// <param name="RequestSchemaVersion">The existing payload schema version.</param>
/// <param name="Operation">The existing operation discriminator.</param>
/// <param name="ProjectId">The opaque Project identity repeated by the existing payload.</param>
/// <param name="ConversationId">The opaque Conversation identity.</param>
/// <param name="SourceProjectId">The optional original Project identity.</param>
/// <param name="ResolutionResult">The existing resolution confirmation discriminator.</param>
internal sealed record ConfirmProjectResolutionPayload(
    string? RequestSchemaVersion,
    string? Operation,
    string? ProjectId,
    string? ConversationId,
    string? SourceProjectId,
    string? ResolutionResult);
