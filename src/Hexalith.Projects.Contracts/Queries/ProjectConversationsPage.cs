// <copyright file="ProjectConversationsPage.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Queries;

using System;

using Hexalith.Projects.Contracts.Identifiers;

/// <summary>
/// Projects-shaped page of conversation references for a project.
/// </summary>
/// <param name="ProjectId">The owning project identifier.</param>
/// <param name="Items">The visible metadata-only conversation references.</param>
/// <param name="Page">The page metadata after Projects fail-closed filtering.</param>
/// <param name="TrustSignal">The aggregate Projects-owned trust signal for the page.</param>
public sealed record ProjectConversationsPage(
    ProjectId ProjectId,
    IReadOnlyList<ProjectConversationItem> Items,
    ProjectConversationPageMetadata Page,
    ProjectConversationTrustSignal TrustSignal)
{
    /// <summary>
    /// Gets the owning project identifier.
    /// </summary>
    public ProjectId ProjectId { get; } = ProjectId ?? throw new ArgumentNullException(nameof(ProjectId));

    /// <summary>
    /// Gets the visible metadata-only conversation references.
    /// </summary>
    public IReadOnlyList<ProjectConversationItem> Items { get; } = ValidateItems(Items);

    /// <summary>
    /// Gets the page metadata after Projects fail-closed filtering.
    /// </summary>
    public ProjectConversationPageMetadata Page { get; } = Page ?? throw new ArgumentNullException(nameof(Page));

    /// <summary>
    /// Gets the aggregate Projects-owned trust signal for the page.
    /// </summary>
    public ProjectConversationTrustSignal TrustSignal { get; } = TrustSignal;

    /// <summary>
    /// Creates an empty Projects-safe page for a denied or unavailable upstream read.
    /// </summary>
    /// <param name="projectId">The requested project identifier.</param>
    /// <param name="trustSignal">The Projects-owned trust signal.</param>
    /// <returns>An empty page.</returns>
    public static ProjectConversationsPage Empty(ProjectId projectId, ProjectConversationTrustSignal trustSignal)
        => new(projectId, [], new ProjectConversationPageMetadata(0), trustSignal);

    private static IReadOnlyList<ProjectConversationItem> ValidateItems(IReadOnlyList<ProjectConversationItem>? items)
    {
        if (items is null || items.Count == 0)
        {
            return Array.Empty<ProjectConversationItem>();
        }

        return items.Any(static item => item is null)
            ? throw new ArgumentException("Project conversation pages must not contain null items.", nameof(items))
            : items;
    }
}
