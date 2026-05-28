// <copyright file="ProjectFileReference.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Models;

using System;

using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Projects-owned, metadata-only reference to an optional Project File Reference (FR-9, FR-11). File
/// references are a bounded optional set; they supplement Project Context and never replace the single
/// Project Folder.
/// </summary>
/// <remarks>
/// Stores only stable opaque identifiers and bounded safe display metadata: the Projects-owned opaque
/// <see cref="FileReferenceId"/>, the owning Folders <see cref="FolderId"/>, and a safe
/// <see cref="DisplayName"/>. It never stores file contents, byte ranges, raw or workspace paths, diffs,
/// provider payloads, tokens, secrets, or raw Folders authorization details (per
/// <c>docs/payload-taxonomy.md</c>; prefer opaque reference id plus safe display metadata over path-like
/// fields).
/// </remarks>
/// <param name="FileReferenceId">The Projects-owned opaque, stable file-reference identifier.</param>
/// <param name="FolderId">The owning Folders-owned folder identifier the file lives under, or null.</param>
/// <param name="DisplayName">Safe display metadata intent for the file reference, or null.</param>
/// <param name="ReferenceState">The shared reference state exposed to Projects reads.</param>
/// <param name="ReasonCode">Optional stable metadata-only reason code for non-included states.</param>
/// <param name="ObservedAt">The event-carried instant at which this reference state was observed.</param>
public sealed record ProjectFileReference(
    string FileReferenceId,
    string? FolderId,
    string? DisplayName,
    ReferenceState ReferenceState,
    string? ReasonCode,
    DateTimeOffset ObservedAt);
