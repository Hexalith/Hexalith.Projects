// <copyright file="ProjectsServerModule.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

/// <summary>
/// Placeholder anchor for the Hexalith.Projects server host behavior.
/// Authentication, authorization, ACL adapters and tenant-access wiring land in later
/// Epic-1 stories. Only <c>Server/Acl/*</c> may reference sibling module clients.
/// </summary>
public static class ProjectsServerModule
{
    /// <summary>
    /// Gets the module name used in server diagnostics and registration.
    /// </summary>
    public static string Name => "Hexalith.Projects.Server";
}
