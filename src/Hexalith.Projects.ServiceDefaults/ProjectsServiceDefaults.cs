// <copyright file="ProjectsServiceDefaults.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.ServiceDefaults;

/// <summary>
/// Placeholder anchor for the Hexalith.Projects shared service defaults.
/// Telemetry, health checks, resilience and service-discovery wiring align with the
/// ServiceDefaults pattern and land with the Aspire topology in Story 1.9.
/// </summary>
public static class ProjectsServiceDefaults
{
    /// <summary>
    /// Gets the module name used in host diagnostics.
    /// </summary>
    public static string Name => "Hexalith.Projects.ServiceDefaults";
}
