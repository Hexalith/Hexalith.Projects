// <copyright file="ProjectsWorkersModule.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Workers;

/// <summary>
/// Placeholder anchor for the Hexalith.Projects background workers.
/// Tenant-scoped subscribers and the worker host topology land in Story 1.9
/// (Aspire / Dapr / workers operational skeleton).
/// </summary>
public static class ProjectsWorkersModule
{
    /// <summary>
    /// Gets the module name used in worker diagnostics and registration.
    /// </summary>
    public static string Name => "Hexalith.Projects.Workers";
}
