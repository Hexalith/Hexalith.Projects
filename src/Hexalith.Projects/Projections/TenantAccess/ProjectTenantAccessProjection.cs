// <copyright file="ProjectTenantAccessProjection.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.TenantAccess;

using System;
using System.Collections.Generic;

/// <summary>
/// Local, metadata-only tenant access projection fed by Hexalith.Tenants events.
/// </summary>
public sealed class ProjectTenantAccessProjection
{
    /// <summary>Gets the managed tenant identifier.</summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the tenant is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets projected principal membership evidence keyed by principal id.</summary>
    public Dictionary<string, ProjectTenantPrincipalEvidence> Principals { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Gets project-scoped tenant configuration keys currently set.</summary>
    public HashSet<string> ConfigurationKeys { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Gets project-scoped tenant configuration keys explicitly removed.</summary>
    public HashSet<string> RemovedConfigurationKeys { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Gets processed message evidence keyed by Tenants event message id.</summary>
    public Dictionary<string, ProjectTenantEventEvidence> ProcessedMessages { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Gets or sets the highest applied Tenants event sequence.</summary>
    public long Watermark { get; set; }

    /// <summary>Gets or sets the display-safe projection watermark.</summary>
    public string ProjectionWatermark { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp of the most recent applied event.</summary>
    public DateTimeOffset? LastEventTimestamp { get; set; }

    /// <summary>Gets or sets a value indicating whether a same-message replay carried divergent evidence.</summary>
    public bool ReplayConflict { get; set; }

    /// <summary>Gets or sets a value indicating whether malformed evidence was observed.</summary>
    public bool MalformedEvidence { get; set; }

    /// <summary>Gets or sets the optimistic-concurrency token used by the projection store.</summary>
    public long Version { get; set; }

    /// <summary>Creates a deep copy of this projection.</summary>
    /// <returns>A detached copy.</returns>
    public ProjectTenantAccessProjection Clone()
        => new()
        {
            TenantId = TenantId,
            Enabled = Enabled,
            Principals = new Dictionary<string, ProjectTenantPrincipalEvidence>(Principals, StringComparer.Ordinal),
            ConfigurationKeys = new HashSet<string>(ConfigurationKeys, StringComparer.Ordinal),
            RemovedConfigurationKeys = new HashSet<string>(RemovedConfigurationKeys, StringComparer.Ordinal),
            ProcessedMessages = new Dictionary<string, ProjectTenantEventEvidence>(ProcessedMessages, StringComparer.Ordinal),
            Watermark = Watermark,
            ProjectionWatermark = ProjectionWatermark,
            LastEventTimestamp = LastEventTimestamp,
            ReplayConflict = ReplayConflict,
            MalformedEvidence = MalformedEvidence,
            Version = Version,
        };
}
