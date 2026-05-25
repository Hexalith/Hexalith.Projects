// <copyright file="ProjectsServerModule.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

/// <summary>
/// Server host anchor and shared constants for Hexalith.Projects (Story 1.4 tracer bullet). The full
/// <c>TenantAccessProjection</c> / layered claim-transform authorization chain is Story 1.6; this
/// story wires a minimal tenant-context guard sufficient to prove fail-closed-on-missing-tenant + the
/// command-async 202 + safe-denial 404 mapping. Only <c>Server</c> takes Dapr/EventStore-runtime
/// dependencies; the domain core stays pure.
/// </summary>
public static class ProjectsServerModule
{
    /// <summary>Gets the module name used in server diagnostics and registration.</summary>
    public static string Name => "Hexalith.Projects.Server";

    /// <summary>The canonical EventStore domain segment for project aggregates.</summary>
    public const string DomainName = "projects";

    /// <summary>The fully qualified <c>CreateProject</c> command type discriminator on the wire.</summary>
    public const string CreateProjectCommandType = "Hexalith.Projects.Commands.CreateProject";

    /// <summary>The aggregate-callback route the EventStore command pipeline invokes.</summary>
    public const string ProcessRoute = "/process";

    /// <summary>The reserved platform tenant; never a valid project-data tenant context.</summary>
    public const string ReservedSystemTenant = "system";

    /// <summary>Maximum length for canonical envelope identifiers (idempotency key, correlation id, task id, project id).</summary>
    public const int MaxCanonicalIdentifierLength = 128;
}
