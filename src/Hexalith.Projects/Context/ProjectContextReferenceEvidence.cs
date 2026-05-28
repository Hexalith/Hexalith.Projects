// <copyright file="ProjectContextReferenceEvidence.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Context;

using System;
using System.Collections.Generic;

using Hexalith.Projects.Contracts.Models;

/// <summary>
/// Typed input shape that gathers the four per-kind candidate-reference collections
/// <c>ProjectContextInclusionPolicy</c> evaluates (Story 3.1). Story 3.2's host composes this from
/// the four Story 2.x ACL adapters at query time; the policy itself never fetches anything.
/// </summary>
/// <remarks>
/// All collections default to empty (never <see langword="null"/>) so the policy can iterate
/// without nil-guarding. The collections remain per-kind disjoint, preserving the Story 2.5 /
/// Story 2.7 per-kind disjoint-lane invariant.
/// </remarks>
/// <param name="ProjectFolder">The single optional Project Folder reference, or <see langword="null"/>.</param>
/// <param name="FileReferences">The bounded optional file references from <c>ProjectDetailItem</c>.</param>
/// <param name="MemoryReferences">The bounded optional memory references from <c>ProjectDetailItem</c>.</param>
/// <param name="Conversations">The Projects-shaped conversation evidence from the Story 2.1 read ACL.</param>
public sealed record ProjectContextReferenceEvidence(
    ProjectFolderReference? ProjectFolder,
    IReadOnlyList<ProjectFileReference> FileReferences,
    IReadOnlyList<ProjectMemoryReference> MemoryReferences,
    IReadOnlyList<ProjectContextConversationEvidence> Conversations)
{
    /// <summary>Gets the bounded optional file references.</summary>
    public IReadOnlyList<ProjectFileReference> FileReferences { get; } = FileReferences ?? Array.Empty<ProjectFileReference>();

    /// <summary>Gets the bounded optional memory references.</summary>
    public IReadOnlyList<ProjectMemoryReference> MemoryReferences { get; } = MemoryReferences ?? Array.Empty<ProjectMemoryReference>();

    /// <summary>Gets the Projects-shaped conversation evidence.</summary>
    public IReadOnlyList<ProjectContextConversationEvidence> Conversations { get; } = Conversations ?? Array.Empty<ProjectContextConversationEvidence>();

    /// <summary>Gets an empty evidence input (no folder, no file/memory references, no conversations).</summary>
    public static ProjectContextReferenceEvidence Empty { get; } = new(
        ProjectFolder: null,
        FileReferences: Array.Empty<ProjectFileReference>(),
        MemoryReferences: Array.Empty<ProjectMemoryReference>(),
        Conversations: Array.Empty<ProjectContextConversationEvidence>());
}
