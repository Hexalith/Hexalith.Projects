// <copyright file="MutableProjectCreationAuthorityChecker.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Workflow;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Projects.SdkWriter.Admission;

/// <summary>Thread-safe configurable authority checker for isolated-host and multi-replica tests.</summary>
public sealed class MutableProjectCreationAuthorityChecker : IProjectCreationAuthorityChecker
{
    private readonly ConcurrentDictionary<string, ActorAuthorityStatus> _overrides = new(StringComparer.Ordinal);
    private volatile ActorAuthorityStatus _defaultStatus = ActorAuthorityStatus.Allowed;

    /// <summary>Sets the default status for actors without an explicit override.</summary>
    /// <param name="status">The default status.</param>
    public void SetDefaultStatus(ActorAuthorityStatus status) => _defaultStatus = status;

    /// <summary>Sets an explicit authority status for a specific <c>(tenantId, actorPrincipalId)</c> pair.</summary>
    /// <param name="tenantId">The Tenant identifier.</param>
    /// <param name="actorPrincipalId">The actor principal identifier.</param>
    /// <param name="status">The authority status to return.</param>
    public void SetStatus(string tenantId, string actorPrincipalId, ActorAuthorityStatus status)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(actorPrincipalId);
        _overrides[Key(tenantId, actorPrincipalId)] = status;
    }

    /// <summary>Removes an explicit override so the default status applies.</summary>
    /// <param name="tenantId">The Tenant identifier.</param>
    /// <param name="actorPrincipalId">The actor principal identifier.</param>
    public void ClearStatus(string tenantId, string actorPrincipalId)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(actorPrincipalId);
        _overrides.TryRemove(Key(tenantId, actorPrincipalId), out _);
    }

    /// <inheritdoc/>
    public Task<ActorAuthorityStatus> CheckAsync(
        string tenantId,
        string actorPrincipalId,
        ProjectCreationCallerClass callerClass,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(actorPrincipalId);
        cancellationToken.ThrowIfCancellationRequested();
        if (callerClass is not (ProjectCreationCallerClass.ProjectUser or ProjectCreationCallerClass.ServiceWorkflowCaller))
        {
            return Task.FromResult(ActorAuthorityStatus.Denied);
        }

        ActorAuthorityStatus status = _overrides.TryGetValue(Key(tenantId, actorPrincipalId), out ActorAuthorityStatus explicitStatus)
            ? explicitStatus
            : _defaultStatus;
        return Task.FromResult(status);
    }

    private static string Key(string tenantId, string actorPrincipalId)
        => $"{tenantId.Length}:{tenantId}:{actorPrincipalId.Length}:{actorPrincipalId}";
}
