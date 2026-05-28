// <copyright file="ProjectResolutionContext.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Resolution;

using System;
using System.Collections.Generic;

/// <summary>
/// Request-level evidence consumed by <see cref="ProjectResolutionEngine"/>. The values are supplied
/// by the host composition; the engine never fetches tenant, clock, correlation, or task metadata.
/// </summary>
/// <param name="AuthoritativeTenantId">Server-derived tenant authority, or <see langword="null"/> when unverifiable.</param>
/// <param name="RequestedTenantId">Optional requested tenant identifier. It is never trusted over <paramref name="AuthoritativeTenantId"/>.</param>
/// <param name="IncludeArchived">Whether archived Projects may qualify as resolution candidates.</param>
/// <param name="Now">The evaluation instant. This is the engine's only clock source.</param>
/// <param name="CorrelationId">Optional correlation id used for structured logging only.</param>
/// <param name="TaskId">Optional task id used for structured logging only.</param>
/// <param name="PresentedInputIds">Optional metadata-only opaque ids describing presented inputs. Never emitted to the wire result.</param>
public sealed record ProjectResolutionContext(
    string? AuthoritativeTenantId,
    string? RequestedTenantId,
    bool IncludeArchived,
    DateTimeOffset Now,
    string? CorrelationId = null,
    string? TaskId = null,
    IReadOnlyList<string>? PresentedInputIds = null)
{
    /// <summary>Gets metadata-only opaque ids for inputs the caller presented to the engine.</summary>
    public IReadOnlyList<string> PresentedInputIds { get; } = PresentedInputIds ?? Array.Empty<string>();

    /// <summary>
    /// Gets an empty canonical context for tests and callers that have no request metadata beyond a
    /// deterministic evaluation instant.
    /// </summary>
    /// <param name="now">The deterministic evaluation instant.</param>
    /// <returns>A request context with archived projects excluded and no tenant authority.</returns>
    public static ProjectResolutionContext Empty(DateTimeOffset now)
        => new(
            AuthoritativeTenantId: null,
            RequestedTenantId: null,
            IncludeArchived: false,
            Now: now,
            CorrelationId: null,
            TaskId: null,
            PresentedInputIds: Array.Empty<string>());
}
