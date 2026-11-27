// <copyright file="SetProjectFolder.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Commands;

using Hexalith.Projects.Contracts.Models;

/// <summary>SDK wire command matching the routed v1 <c>SetProjectFolder</c> short name.</summary>
/// <param name="RequestSchemaVersion">The wire schema version.</param>
/// <param name="Operation">The operation discriminator.</param>
/// <param name="ProjectId">The Project identifier repeated on the wire payload.</param>
/// <param name="FolderId">The Folder identifier.</param>
/// <param name="FolderMetadata">The validated Folder metadata.</param>
/// <param name="ReplacementConfirmed">Whether replacement was explicitly confirmed.</param>
public sealed record SetProjectFolder(
    string? RequestSchemaVersion,
    string? Operation,
    string? ProjectId,
    string? FolderId,
    ProjectFolderMetadata? FolderMetadata,
    bool ReplacementConfirmed);
