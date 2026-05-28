// <copyright file="ProjectMemoryReferenceMetadata.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Models;

/// <summary>
/// Bounded, metadata-only display metadata for a Project Memory Reference owned by Projects.
/// </summary>
/// <remarks>
/// It is comparison/display intent only. It is never a Memories tenant authority, MemoryUnit content,
/// content hash, source URI, embedding payload, raw upstream error text, or token. Hexalith.Memories
/// owned metadata is the authority for whether the reference is currently usable; this label only
/// labels the reference for Projects reads.
/// </remarks>
/// <param name="DisplayName">The safe memory display label intent.</param>
public sealed record ProjectMemoryReferenceMetadata(string? DisplayName);
