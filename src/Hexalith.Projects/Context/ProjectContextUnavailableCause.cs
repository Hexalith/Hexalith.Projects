// <copyright file="ProjectContextUnavailableCause.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Context;

/// <summary>Closed internal causes used to select applicable AD-32 recovery actions.</summary>
public enum ProjectContextUnavailableCause
{
    /// <summary>Required persisted evidence is missing or stale.</summary>
    MissingOrStaleRequiredContext,

    /// <summary>Required evidence is still materializing.</summary>
    MaterializationInProgress,

    /// <summary>An identity-free persisted-store fault occurred after authority was established.</summary>
    StoreFault,

    /// <summary>Evidence is corrupt, duplicated, overflowing, or authorization-uncertain.</summary>
    CorruptionOrAuthorizationUncertainty,

    /// <summary>An archived or ambiguous required reference needs an alternative selection.</summary>
    AlternativeRequired,
}
