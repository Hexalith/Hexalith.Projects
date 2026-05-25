// <copyright file="ProjectsMcpModule.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Mcp;

/// <summary>
/// Placeholder anchor for the Hexalith.Projects MCP adapter.
/// MCP tools translate intent to commands/queries over the typed client; they must never
/// reference domain event types or Dapr. Tooling is added by later stories.
/// </summary>
public static class ProjectsMcpModule
{
    /// <summary>
    /// Gets the module name used in MCP diagnostics.
    /// </summary>
    public static string Name => "Hexalith.Projects.Mcp";
}
