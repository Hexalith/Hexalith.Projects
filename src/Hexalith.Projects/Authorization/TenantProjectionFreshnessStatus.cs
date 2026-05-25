// <copyright file="TenantProjectionFreshnessStatus.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Authorization;

using System.Text.Json.Serialization;

/// <summary>Freshness classification for the tenant-access projection.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TenantProjectionFreshnessStatus>))]
public enum TenantProjectionFreshnessStatus
{
    Unknown,
    Fresh,
    Stale,
    Future,
    Unavailable,
}
