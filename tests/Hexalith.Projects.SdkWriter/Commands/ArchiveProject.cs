// <copyright file="ArchiveProject.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Commands;

/// <summary>SDK wire command matching the routed v1 <c>ArchiveProject</c> short name.</summary>
/// <param name="ArchiveIntent">The explicit archive intent.</param>
/// <param name="RequestSchemaVersion">The wire schema version.</param>
public sealed record ArchiveProject(
    string? ArchiveIntent,
    string? RequestSchemaVersion);
