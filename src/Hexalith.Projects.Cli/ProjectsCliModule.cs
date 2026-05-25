// <copyright file="ProjectsCliModule.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Cli;

/// <summary>
/// Placeholder anchor for the Hexalith.Projects CLI adapter.
/// CLI commands call the Admin API over the typed client; they must never reference domain
/// event types or Dapr. Command verbs are added by later stories.
/// </summary>
public static class ProjectsCliModule
{
    /// <summary>
    /// Gets the module name used in CLI diagnostics.
    /// </summary>
    public static string Name => "Hexalith.Projects.Cli";
}
