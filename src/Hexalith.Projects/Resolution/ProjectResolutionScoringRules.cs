// <copyright file="ProjectResolutionScoringRules.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Resolution;

using System.Collections.Generic;

using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Single source of truth for Story 4.1 resolution reason-code ordering, weights, and thresholds.
/// Mirrors <c>docs/resolution-scoring-heuristic.md</c>.
/// </summary>
public static class ProjectResolutionScoringRules
{
    /// <summary>Gets deterministic reason-code weights in ranking order.</summary>
    public static IReadOnlyList<ProjectResolutionReasonWeight> Weights { get; } =
    [
        new(ProjectReasonCode.ConversationLinked, 50),
        new(ProjectReasonCode.ProjectFolderMatched, 45),
        new(ProjectReasonCode.FileReferenceMatched, 35),
        new(ProjectReasonCode.MemoryMatched, 30),
        new(ProjectReasonCode.MetadataMatched, 20),
    ];

    /// <summary>Gets the minimum score a candidate needs to qualify.</summary>
    public static int MinimumQualifyingScore => 20;

    /// <summary>Returns the configured weight for <paramref name="reasonCode"/>.</summary>
    /// <param name="reasonCode">The shared reason code.</param>
    /// <returns>The score contribution.</returns>
    public static int WeightFor(ProjectReasonCode reasonCode)
    {
        foreach (ProjectResolutionReasonWeight weight in Weights)
        {
            if (weight.ReasonCode == reasonCode)
            {
                return weight.Weight;
            }
        }

        return 0;
    }
}

/// <summary>One reason-code weight row from the resolution scoring heuristic.</summary>
/// <param name="ReasonCode">The reason code.</param>
/// <param name="Weight">The numeric score contribution.</param>
public readonly record struct ProjectResolutionReasonWeight(ProjectReasonCode ReasonCode, int Weight);
