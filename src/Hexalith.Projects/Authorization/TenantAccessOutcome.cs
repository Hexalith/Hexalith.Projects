// <copyright file="TenantAccessOutcome.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Authorization;

using System.Text.Json.Serialization;

/// <summary>Internal tenant-access authorization outcomes.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TenantAccessOutcome>))]
public enum TenantAccessOutcome
{
    Allowed,
    Denied,
    StaleProjection,
    UnavailableProjection,
    UnknownTenant,
    DisabledTenant,
    MalformedEvidence,
    TenantMismatch,
    MissingAuthoritativeTenant,
    ReplayConflict,
}
