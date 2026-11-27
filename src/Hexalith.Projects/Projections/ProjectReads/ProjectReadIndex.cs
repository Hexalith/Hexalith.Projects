// <copyright file="ProjectReadIndex.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.ProjectReads;

using System;
using System.Collections.Generic;

/// <summary>The tenant-scoped supported enumeration index, atomically maintained with detail.</summary>
/// <param name="TenantId">The owning tenant.</param>
/// <param name="ProjectIds">The sorted opaque identities of created Projects.</param>
/// <param name="GlobalPosition">The highest exact global position applied to this index.</param>
/// <param name="LastVerifiedAt">The persisted observation associated with that position.</param>
public sealed record ProjectReadIndex(
    string TenantId,
    IReadOnlyList<string> ProjectIds,
    long GlobalPosition,
    DateTimeOffset LastVerifiedAt)
{
    /// <summary>Gets source aggregates whose latest delivery could not be interpreted completely.</summary>
    public IReadOnlyList<string> UnavailableProjectIds { get; init; } = [];
}
