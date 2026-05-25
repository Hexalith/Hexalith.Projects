// <copyright file="IProjectCommand.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Commands;

using Hexalith.Projects.Contracts.Identifiers;

/// <summary>
/// Marker contract shared by every Projects command (AR-5). Mirrors the Folders
/// <c>IFolderCommand</c> convention: a command carries the canonical aggregate identity plus the
/// cross-cutting envelope identifiers (actor, correlation, task, idempotency) the EventStore
/// command pipeline threads through to the emitted events.
/// </summary>
/// <remarks>
/// Lives in <c>Contracts</c> (not domain-core) because the Server endpoint and the generated
/// client bind the command shape, while the success/rejection events remain metadata-only
/// regardless of where the markers live. Tenant is an envelope <see cref="string"/> (the
/// user-facing managed tenant, never a payload-controlled authority); the project identity is the
/// validated <see cref="ProjectId"/> value object, never a raw string. Netstandard2.0-safe.
/// </remarks>
public interface IProjectCommand
{
    /// <summary>Gets the managed tenant identifier (the EventStore envelope tenant).</summary>
    string TenantId { get; }

    /// <summary>Gets the project identifier this command targets.</summary>
    ProjectId ProjectId { get; }

    /// <summary>Gets the authenticated actor principal identifier.</summary>
    string ActorPrincipalId { get; }

    /// <summary>Gets the correlation identifier for request tracing.</summary>
    string CorrelationId { get; }

    /// <summary>Gets the task identifier for task-scoped operations.</summary>
    string TaskId { get; }

    /// <summary>Gets the idempotency key (same key + same payload = replay; same key + different payload = conflict).</summary>
    string IdempotencyKey { get; }

    /// <summary>Gets the command type discriminator (imperative name, no <c>Command</c> suffix).</summary>
    string CommandType { get; }
}
