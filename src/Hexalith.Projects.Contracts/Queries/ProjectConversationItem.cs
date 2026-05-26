// <copyright file="ProjectConversationItem.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Queries;

using System;

using ConversationId = Hexalith.Conversations.Contracts.Identifiers.ConversationId;
using Hexalith.Projects.Contracts.Identifiers;

/// <summary>
/// Projects-shaped, metadata-only conversation reference for a project.
/// </summary>
/// <param name="ProjectId">The owning project identifier.</param>
/// <param name="ConversationId">The Conversations-owned reference identity.</param>
/// <param name="LifecycleStatus">The safe lifecycle/status token from the conversation summary.</param>
/// <param name="DisplayLabel">An optional safe display label.</param>
/// <param name="TrustSignal">The Projects-owned trust signal for this item.</param>
/// <param name="ProjectSafeLabel">The optional safe project hydration label supplied by Conversations.</param>
/// <param name="ProjectSafeStatus">The optional safe project hydration status supplied by Conversations.</param>
public sealed record ProjectConversationItem(
    ProjectId ProjectId,
    ConversationId ConversationId,
    string LifecycleStatus,
    string? DisplayLabel,
    ProjectConversationTrustSignal TrustSignal,
    string? ProjectSafeLabel,
    string? ProjectSafeStatus)
{
    /// <summary>
    /// Gets the owning project identifier.
    /// </summary>
    public ProjectId ProjectId { get; } = ProjectId ?? throw new ArgumentNullException(nameof(ProjectId));

    /// <summary>
    /// Gets the Conversations-owned reference identity.
    /// </summary>
    public ConversationId ConversationId { get; } = ConversationId ?? throw new ArgumentNullException(nameof(ConversationId));

    /// <summary>
    /// Gets the safe lifecycle/status token.
    /// </summary>
    public string LifecycleStatus { get; } = ValidateRequired(LifecycleStatus, nameof(LifecycleStatus));

    /// <summary>
    /// Gets an optional safe display label.
    /// </summary>
    public string? DisplayLabel { get; } = Normalize(DisplayLabel);

    /// <summary>
    /// Gets the Projects-owned trust signal for this item.
    /// </summary>
    public ProjectConversationTrustSignal TrustSignal { get; } = TrustSignal;

    /// <summary>
    /// Gets the optional safe project hydration label supplied by Conversations.
    /// </summary>
    public string? ProjectSafeLabel { get; } = Normalize(ProjectSafeLabel);

    /// <summary>
    /// Gets the optional safe project hydration status supplied by Conversations.
    /// </summary>
    public string? ProjectSafeStatus { get; } = Normalize(ProjectSafeStatus);

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
