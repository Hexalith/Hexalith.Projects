// <copyright file="ProjectsClientModule.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Client;

/// <summary>
/// Placeholder anchor for the consumer-facing Hexalith.Projects client.
/// The generated typed HTTP client and idempotency helpers land in Story 1.3.
/// This client surface must never reference domain event types or Dapr.
/// </summary>
public static class ProjectsClientModule
{
    /// <summary>
    /// Gets the module name used in client diagnostics and registration.
    /// </summary>
    public static string Name => "Hexalith.Projects.Client";
}
