// <copyright file="TenantAccessTransientPersistenceException.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.TenantAccess;

using System;

/// <summary>Thrown by projection stores for retryable tenant-access persistence failures.</summary>
public sealed class TenantAccessTransientPersistenceException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="TenantAccessTransientPersistenceException"/> class.</summary>
    public TenantAccessTransientPersistenceException(string tenantId, Exception? innerException = null)
        : base($"Transient tenant-access projection persistence failure for tenant '{tenantId}'.", innerException)
        => TenantId = tenantId;

    /// <summary>Gets the affected tenant id.</summary>
    public string TenantId { get; }
}
