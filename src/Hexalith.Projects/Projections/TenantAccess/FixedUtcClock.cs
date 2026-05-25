// <copyright file="FixedUtcClock.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.TenantAccess;

using System;

/// <summary>Fixed UTC clock used by deterministic tests.</summary>
/// <param name="utcNow">The fixed UTC instant.</param>
public sealed class FixedUtcClock(DateTimeOffset utcNow) : IUtcClock
{
    /// <inheritdoc/>
    public DateTimeOffset UtcNow { get; } = utcNow;
}
