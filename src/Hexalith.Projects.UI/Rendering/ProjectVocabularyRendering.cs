// <copyright file="ProjectVocabularyRendering.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Rendering;

using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Thin UI adapter over the shared Projects vocabulary descriptors.
/// </summary>
public static class ProjectVocabularyRendering
{
    /// <summary>Gets the lifecycle descriptor for a diagnostic DTO lifecycle string.</summary>
    /// <param name="lifecycleState">The lifecycle state from the operator diagnostic DTO.</param>
    /// <returns>The shared descriptor.</returns>
    public static VocabularyDescriptor DescribeLifecycle(string? lifecycleState)
        => ProjectVocabularyDescriptors.Describe(ParseLifecycle(lifecycleState));

    /// <summary>Gets the reference-state descriptor for a diagnostic DTO reference state string.</summary>
    /// <param name="referenceState">The reference state from a reference summary.</param>
    /// <returns>The shared descriptor.</returns>
    public static VocabularyDescriptor DescribeReferenceState(string? referenceState)
        => ProjectVocabularyDescriptors.Describe(ParseReferenceState(referenceState));

    /// <summary>Parses a diagnostic lifecycle string into the shared lifecycle vocabulary.</summary>
    /// <param name="lifecycleState">The lifecycle state from the operator diagnostic DTO.</param>
    /// <returns>The parsed lifecycle value.</returns>
    public static ProjectLifecycle ParseLifecycle(string? lifecycleState)
        => Enum.TryParse(lifecycleState, ignoreCase: true, out ProjectLifecycle lifecycle)
            ? lifecycle
            : ProjectLifecycle.Archived;

    /// <summary>Parses a diagnostic reference-state string into the shared reference vocabulary.</summary>
    /// <param name="referenceState">The reference state from a reference summary.</param>
    /// <returns>The parsed reference-state value.</returns>
    public static ReferenceState ParseReferenceState(string? referenceState)
        => Enum.TryParse(referenceState, ignoreCase: true, out ReferenceState state)
            ? state
            : ReferenceState.Unavailable;
}
