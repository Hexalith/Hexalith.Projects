// <copyright file="SystemUtcClock.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.TenantAccess;

using System;

/// <summary>System UTC clock.</summary>
public sealed class SystemUtcClock : IUtcClock
{
    /// <inheritdoc/>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
