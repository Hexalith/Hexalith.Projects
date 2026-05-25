// <copyright file="ProjectsAspire.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Aspire;

/// <summary>
/// Placeholder anchor for the Hexalith.Projects Aspire orchestration extensions.
/// The AppHost topology (eventstore + tenants + projects + workers + ui + Keycloak) and the
/// Dapr component wiring land in Story 1.9.
/// </summary>
public static class ProjectsAspire
{
    /// <summary>
    /// Gets the module name used in orchestration diagnostics.
    /// </summary>
    public static string Name => "Hexalith.Projects.Aspire";
}
