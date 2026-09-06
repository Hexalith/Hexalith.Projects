// <copyright file="ProjectContextShadowComparison.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Testing.Reads;

/// <summary>Outcome of a legacy-versus-supported Project Context shadow comparison.</summary>
/// <param name="Equivalent">Whether the comparable surface matched after AD-32 normalization.</param>
/// <param name="Divergence">The first unmatched comparable field, or null when equivalent.</param>
public sealed record ProjectContextShadowComparison(bool Equivalent, string? Divergence)
{
    /// <summary>Gets the equivalent comparison outcome.</summary>
    public static ProjectContextShadowComparison Match { get; } = new(true, null);

    /// <summary>Builds a diverged comparison outcome.</summary>
    /// <param name="divergence">The unmatched comparable field.</param>
    /// <returns>The diverged outcome.</returns>
    public static ProjectContextShadowComparison Diverged(string divergence)
        => new(false, divergence);
}
