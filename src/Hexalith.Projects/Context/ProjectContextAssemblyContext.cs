// <copyright file="ProjectContextAssemblyContext.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Context;

using System;

/// <summary>
/// The request-level inputs <c>ProjectContextInclusionPolicy.Assemble(...)</c> needs to evaluate the
/// AR-9 inclusion policy (Story 3.1). All fields are tenant/correlation metadata plus the typed
/// <see cref="Now"/> clock value; nothing else is read from the environment.
/// </summary>
/// <remarks>
/// <para>
/// Identifier fields are intentionally nullable at construction so that callers can express
/// "missing tenant authority" or "no project id presented" inputs — the policy itself collapses
/// such requests to the safe-denial outcomes defined in
/// <c>docs/context-assembly-decision-matrix.md</c>. There is no eager argument validation here.
/// </para>
/// <para>
/// <see cref="Now"/> is the single time source the policy consults. The policy never reads any
/// wall-clock or stopwatch source directly; callers (Story 3.2+ hosts) inject a clock and pass
/// the typed instant into this record.
/// </para>
/// </remarks>
/// <param name="AuthoritativeTenantId">The authoritative tenant identifier (from the EventStore claim-transform), or null.</param>
/// <param name="RequestedTenantId">The tenant the caller requested context for, or null.</param>
/// <param name="ProjectId">The project the caller requested context for, or null.</param>
/// <param name="OperationKind">The Epic 3 operation driving the read/trust-bearing distinction.</param>
/// <param name="CorrelationId">The Story 1.1 correlation id (logged for traceability), or null.</param>
/// <param name="TaskId">The Story 1.1 task id (logged for traceability), or null.</param>
/// <param name="Now">The typed assembly observation instant.</param>
public sealed record ProjectContextAssemblyContext(
    string? AuthoritativeTenantId,
    string? RequestedTenantId,
    string? ProjectId,
    ProjectContextOperationKind OperationKind,
    string? CorrelationId,
    string? TaskId,
    DateTimeOffset Now);
