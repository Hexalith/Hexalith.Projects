// <copyright file="CreateProject.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Commands;

/// <summary>SDK wire command matching the routed v1 <c>CreateProject</c> short name.</summary>
/// <param name="Name">The caller-supplied Project name.</param>
/// <param name="Description">Optional safe description.</param>
/// <param name="SetupMetadata">Optional safe setup-metadata reference.</param>
public sealed record CreateProject(
    string? Name,
    string? Description = null,
    string? SetupMetadata = null);
