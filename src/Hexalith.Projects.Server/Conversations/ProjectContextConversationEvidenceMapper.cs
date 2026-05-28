// <copyright file="ProjectContextConversationEvidenceMapper.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Conversations;

using System;
using System.Collections.Generic;

using Hexalith.Projects.Context;
using Hexalith.Projects.Contracts.Queries;

/// <summary>
/// Translates a Projects-shaped <see cref="ProjectConversationsPage"/> read from the Story 2.1
/// conversation directory ACL into the per-candidate
/// <see cref="ProjectContextConversationEvidence"/> shape consumed by Story 3.1's
/// <c>ProjectContextInclusionPolicy.Assemble(...)</c> (Story 3.2 — first HTTP-surfaced consumer of
/// the policy).
/// </summary>
/// <remarks>
/// <para>
/// This is the single allowed boundary mapper for conversation evidence. It is a pure static
/// function: no DI, no HTTP, no wall-clock read (only the typed <c>now</c> parameter), no Dapr.
/// The translator namespace boundary mirrors the precedent set by
/// <see cref="ProjectConversationTranslator"/> for the read ACL.
/// </para>
/// <para>
/// Whitespace-only display labels collapse to <see langword="null"/> on the resulting evidence
/// records, matching the Story 3.1
/// <see cref="ProjectContextConversationEvidence"/> normalisation contract. A null/empty page
/// produces an empty result — never <see langword="null"/>.
/// </para>
/// </remarks>
internal static class ProjectContextConversationEvidenceMapper
{
    /// <summary>
    /// Maps every <see cref="ProjectConversationItem"/> in <paramref name="page"/> to a
    /// <see cref="ProjectContextConversationEvidence"/> row preserving order. The <paramref name="now"/>
    /// instant is the fallback for items that do not carry an observed-at value.
    /// </summary>
    /// <param name="page">The Projects-shaped conversation page from the Story 2.1 ACL.</param>
    /// <param name="now">The typed observation instant the host injected from <c>TimeProvider</c>.</param>
    /// <returns>The mapped evidence list (never <see langword="null"/>; empty when the page is empty).</returns>
    public static IReadOnlyList<ProjectContextConversationEvidence> Map(
        ProjectConversationsPage? page,
        DateTimeOffset now)
    {
        if (page is null || page.Items.Count == 0)
        {
            return Array.Empty<ProjectContextConversationEvidence>();
        }

        List<ProjectContextConversationEvidence> evidence = new(page.Items.Count);
        foreach (ProjectConversationItem item in page.Items)
        {
            if (item is null || item.ConversationId is null || string.IsNullOrWhiteSpace(item.ConversationId.Value))
            {
                continue;
            }

            evidence.Add(new ProjectContextConversationEvidence(
                ConversationId: item.ConversationId.Value,
                DisplayLabel: item.DisplayLabel,
                TrustSignal: item.TrustSignal,
                LastCheckedAt: now));
        }

        return evidence;
    }
}
