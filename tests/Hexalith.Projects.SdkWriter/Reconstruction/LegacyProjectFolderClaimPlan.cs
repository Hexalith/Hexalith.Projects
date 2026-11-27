// <copyright file="LegacyProjectFolderClaimPlan.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Reconstruction;

using System.Collections.Generic;

/// <summary>Deterministic output of AD-35 cutover reconstruction from legacy Project histories.</summary>
/// <param name="IncumbentClaims">Unambiguous Active Project incumbents keyed by length-prefixed Folder key.</param>
/// <param name="ClosedFolderKeys">Folder keys closed/quarantined due to conflicts, incomplete history, pre-existing closed claims, or stale active claims.</param>
/// <param name="QuarantinedProjectKeys">Length-prefixed Project keys quarantined as <c>Unavailable</c> with no context.</param>
/// <param name="FreeFolderKeys">Known Folder keys verified free of incumbents, conflicts, and pre-existing claims.</param>
/// <param name="LegacyPendingFolderProjectKeys">Active Projects carrying historical <c>ProjectFolderCreationPending</c> with no bound Folder.</param>
public sealed record LegacyProjectFolderClaimPlan(
    IReadOnlyDictionary<string, string> IncumbentClaims,
    IReadOnlySet<string> ClosedFolderKeys,
    IReadOnlySet<string> QuarantinedProjectKeys,
    IReadOnlySet<string> FreeFolderKeys,
    IReadOnlySet<string> LegacyPendingFolderProjectKeys);
