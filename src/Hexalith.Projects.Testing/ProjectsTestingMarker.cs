// <copyright file="ProjectsTestingMarker.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Testing;

/// <summary>
/// Placeholder anchor for reusable Hexalith.Projects test utilities.
/// Builders and fakes are added by later stories; the scaffold reuses
/// Hexalith.EventStore.Testing / Hexalith.Tenants.Testing rather than inventing new doubles.
/// </summary>
public static class ProjectsTestingMarker
{
    /// <summary>
    /// Gets the module name used in test diagnostics.
    /// </summary>
    public static string Name => "Hexalith.Projects.Testing";
}
