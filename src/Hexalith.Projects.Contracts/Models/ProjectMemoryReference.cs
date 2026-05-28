// <copyright file="ProjectMemoryReference.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Models;

using System;

using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Projects-owned, metadata-only reference to a Hexalith.Memories Case (FR-10, FR-11). Memory
/// references are a bounded optional set; they supplement Project Context and never replace the
/// single Project Folder.
/// </summary>
/// <remarks>
/// Stores only the opaque Memories case identifier (a ULID-shaped sibling identifier) and bounded
/// safe display metadata. It never stores any <c>MemoryUnit.Content</c>, <c>ContentBytes</c>,
/// <c>ContentHash</c>, <c>SourceUri</c>, <c>SourceType</c>, <c>IngestedBy</c>, <c>Metadata</c>,
/// <c>EmbeddingProvider</c>, <c>EmbeddingModel</c>, <c>EmbeddingDimensions</c>, <c>Classification</c>,
/// raw <c>ErrorResponse.Message</c>, <c>Suggestion</c>, tokens, or paths. The owning Memories tenant
/// is implicit (envelope tenant); it is not echoed onto the reference.
/// </remarks>
/// <param name="MemoryReferenceId">The opaque Memories case identifier.</param>
/// <param name="DisplayName">Safe display metadata intent for the memory reference, or null.</param>
/// <param name="ReferenceState">The shared reference state exposed to Projects reads.</param>
/// <param name="ReasonCode">Optional stable metadata-only reason code for non-included states.</param>
/// <param name="ObservedAt">The event-carried instant at which this reference state was observed.</param>
public sealed record ProjectMemoryReference(
    string MemoryReferenceId,
    string? DisplayName,
    ReferenceState ReferenceState,
    string? ReasonCode,
    DateTimeOffset ObservedAt);
