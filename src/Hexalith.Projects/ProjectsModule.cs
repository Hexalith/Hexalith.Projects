// <copyright file="ProjectsModule.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects;

/// <summary>
/// Placeholder anchor for the Hexalith.Projects domain core module.
/// Establishes the domain namespace and assembly identity. Aggregates, projections,
/// resolution, context, authorization and queries are added by later Epic-1 stories.
/// </summary>
public static class ProjectsModule
{
    /// <summary>
    /// Gets the module name used in logging scopes and registration diagnostics.
    /// </summary>
    public static string Name => "Hexalith.Projects";
}
