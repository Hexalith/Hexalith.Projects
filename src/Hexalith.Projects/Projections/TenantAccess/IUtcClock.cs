// <copyright file="IUtcClock.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.TenantAccess;

using System;

/// <summary>UTC clock abstraction for deterministic freshness tests.</summary>
public interface IUtcClock
{
    /// <summary>Gets the current UTC instant.</summary>
    DateTimeOffset UtcNow { get; }
}
