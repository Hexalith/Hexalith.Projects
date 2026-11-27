// <copyright file="UpdateProjectSetup.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Commands;

using Hexalith.Projects.Contracts.Models;

/// <summary>SDK wire command matching the routed v1 <c>UpdateProjectSetup</c> short name.</summary>
/// <param name="RequestSchemaVersion">The wire schema version.</param>
/// <param name="Setup">The typed Project Setup.</param>
public sealed record UpdateProjectSetup(
    string? RequestSchemaVersion,
    ProjectSetup? Setup);
