// <copyright file="RestoreProject.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Commands;

/// <summary>SDK wire command matching the routed v1 <c>RestoreProject</c> short name.</summary>
/// <param name="RequestSchemaVersion">The wire schema version.</param>
/// <param name="RestoreIntent">The explicit restore intent.</param>
public sealed record RestoreProject(
    string? RequestSchemaVersion,
    string? RestoreIntent);
