// <copyright file="ProjectFolderMetadata.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Models;

/// <summary>
/// Bounded, metadata-only Project Folder display metadata owned by Projects.
/// </summary>
/// <param name="DisplayName">The safe folder display label intent. It is not a path or tenant authority.</param>
public sealed record ProjectFolderMetadata(string? DisplayName);
