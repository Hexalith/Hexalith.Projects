// <copyright file="TenantAccessOutcomeReferenceStateMapper.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Authorization;

using Hexalith.Projects.Contracts.Ui;

/// <summary>Maps internal tenant-access outcomes to the shared external reference-state vocabulary.</summary>
public static class TenantAccessOutcomeReferenceStateMapper
{
    /// <summary>Maps a tenant-access outcome to the shared <see cref="ReferenceState"/> vocabulary.</summary>
    public static ReferenceState ToReferenceState(TenantAccessOutcome outcome)
        => outcome switch
        {
            TenantAccessOutcome.Allowed => ReferenceState.Included,
            TenantAccessOutcome.TenantMismatch => ReferenceState.TenantMismatch,
            TenantAccessOutcome.StaleProjection => ReferenceState.Stale,
            TenantAccessOutcome.UnavailableProjection
                or TenantAccessOutcome.MalformedEvidence
                or TenantAccessOutcome.ReplayConflict
                or TenantAccessOutcome.DisabledTenant => ReferenceState.Unavailable,
            TenantAccessOutcome.Denied
                or TenantAccessOutcome.MissingAuthoritativeTenant
                or TenantAccessOutcome.UnknownTenant => ReferenceState.Unauthorized,
            _ => ReferenceState.Unauthorized,
        };
}
