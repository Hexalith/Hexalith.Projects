// <copyright file="TenantAccessConcurrencyException.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.TenantAccess;

using System;

/// <summary>Thrown when an optimistic-concurrency tenant-access projection save loses the race.</summary>
public sealed class TenantAccessConcurrencyException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="TenantAccessConcurrencyException"/> class.</summary>
    public TenantAccessConcurrencyException(string tenantId, long expectedVersion, long actualVersion)
        : base($"Optimistic concurrency conflict for tenant '{tenantId}': expected version {expectedVersion}, store has {actualVersion}.")
    {
        TenantId = tenantId;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }

    /// <summary>Gets the affected tenant id.</summary>
    public string TenantId { get; }

    /// <summary>Gets the projection version expected by the caller.</summary>
    public long ExpectedVersion { get; }

    /// <summary>Gets the current version in the store.</summary>
    public long ActualVersion { get; }
}
