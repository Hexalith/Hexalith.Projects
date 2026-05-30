// <copyright file="ProjectCreationProposal.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Models;

using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Metadata-only preview of a possible new Project derived from current resolution evidence.
/// </summary>
/// <param name="ResolutionResult">The required resolution outcome. Proposal previews are only emitted for <see cref="ResolutionResult.NoMatch"/>.</param>
/// <param name="SuggestedName">The safe, bounded Project display name suggestion.</param>
/// <param name="Description">Optional safe metadata-only description.</param>
/// <param name="SetupMetadata">Optional safe metadata-only setup reference.</param>
/// <param name="ConversationId">The initiating Conversation identifier.</param>
/// <param name="FolderId">Optional Project Folder identifier presented with the proposal.</param>
/// <param name="FileReferenceIds">Optional bounded File Reference identifiers presented with the proposal.</param>
/// <param name="ObservedAt">The evaluation instant.</param>
/// <param name="Freshness">The read freshness class.</param>
/// <param name="Warnings">Safe, closed diagnostic warnings.</param>
public sealed record ProjectCreationProposal(
    ResolutionResult ResolutionResult,
    string SuggestedName,
    string? Description,
    string? SetupMetadata,
    string ConversationId,
    string? FolderId,
    IReadOnlyList<string> FileReferenceIds,
    DateTimeOffset ObservedAt,
    string Freshness,
    IReadOnlyList<string> Warnings)
{
    /// <summary>Gets the safe Project display name suggestion.</summary>
    public string SuggestedName { get; } = ValidateRequired(SuggestedName, nameof(SuggestedName));

    /// <summary>Gets the initiating Conversation identifier.</summary>
    public string ConversationId { get; } = ValidateRequired(ConversationId, nameof(ConversationId));

    /// <summary>Gets the optional bounded File Reference identifiers.</summary>
    public IReadOnlyList<string> FileReferenceIds { get; } = FileReferenceIds ?? Array.Empty<string>();

    /// <summary>Gets safe, closed diagnostic warnings.</summary>
    public IReadOnlyList<string> Warnings { get; } = Warnings ?? Array.Empty<string>();

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }
}
