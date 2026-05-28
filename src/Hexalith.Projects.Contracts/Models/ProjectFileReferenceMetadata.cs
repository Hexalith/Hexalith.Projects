// <copyright file="ProjectFileReferenceMetadata.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Models;

/// <summary>
/// Bounded, metadata-only display metadata for an optional Project File Reference owned by Projects.
/// </summary>
/// <remarks>
/// It is comparison/display intent only. It is never a path, content body, byte range, provider payload,
/// tenant authority, or raw Folders authorization detail. Folders-owned metadata is the authority for
/// whether the reference is currently usable; this label only labels the reference for Projects reads.
/// </remarks>
/// <param name="DisplayName">The safe file display label intent.</param>
public sealed record ProjectFileReferenceMetadata(string? DisplayName);
