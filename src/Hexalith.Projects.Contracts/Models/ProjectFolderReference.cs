// <copyright file="ProjectFolderReference.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Models;

using System;

using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Projects-owned, metadata-only reference to the single Project Folder.
/// </summary>
/// <param name="FolderId">The Folders-owned folder identifier, or null while creation is pending.</param>
/// <param name="DisplayName">Safe display metadata intent or confirmed label.</param>
/// <param name="ReferenceState">The shared reference state exposed to Projects reads.</param>
/// <param name="ReasonCode">Optional stable metadata-only reason code for pending/unavailable state.</param>
/// <param name="ObservedAt">The event-carried instant at which this reference state was observed.</param>
public sealed record ProjectFolderReference(
    string? FolderId,
    string? DisplayName,
    ReferenceState ReferenceState,
    string? ReasonCode,
    DateTimeOffset ObservedAt);
