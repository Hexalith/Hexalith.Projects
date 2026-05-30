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

    /// <summary>Parses a diagnostic lifecycle string into the shared lifecycle vocabulary.</summary>
    /// <param name="lifecycleState">The lifecycle state from the operator diagnostic DTO.</param>
    /// <returns>The parsed lifecycle value.</returns>
    public static ProjectLifecycle ParseLifecycle(string? lifecycleState)
        => Enum.TryParse(lifecycleState, ignoreCase: true, out ProjectLifecycle lifecycle)
            ? lifecycle
            : ProjectLifecycle.Archived;
}

