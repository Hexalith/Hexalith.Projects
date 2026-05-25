// <copyright file="TenantAccessOptions.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Authorization;

using System;

/// <summary>Freshness and retry options for tenant-access authorization and projection handling.</summary>
public sealed class TenantAccessOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Projects:TenantAccess";

    /// <summary>Gets or sets the maximum projection age allowed for mutations.</summary>
    public TimeSpan MutationFreshnessBudget { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Gets or sets the maximum projection age allowed for diagnostic reads.</summary>
    public TimeSpan DiagnosticStalenessBudget { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Gets or sets producer clock skew tolerated before evidence is marked malformed.</summary>
    public TimeSpan ClockSkewTolerance { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Gets or sets the number of read-modify-write attempts before surfacing a conflict.</summary>
    public int ConcurrencyRetryAttempts { get; set; } = 3;
}
