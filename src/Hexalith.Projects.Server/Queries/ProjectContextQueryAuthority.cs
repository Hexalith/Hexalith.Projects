// <copyright file="ProjectContextQueryAuthority.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Queries;

using System;
using System.Collections.Generic;

/// <summary>Server-owned expected authority values for supported Project Context queries.</summary>
internal static class ProjectContextQueryAuthority
{
    /// <summary>Gets the expected OAuth scopes when the envelope presents scopes.</summary>
    public static IReadOnlyList<string> ExpectedScopes { get; } = ["projects.read"];

    /// <summary>Gets the expected audience values when the envelope presents audience.</summary>
    public static IReadOnlyList<string> ExpectedAudience { get; } = ["projects"];

    /// <summary>
    /// Returns whether presented envelope collections are absent or exactly equal to the server-owned expectation.
    /// </summary>
    /// <param name="presented">The immutable presented collection, or null when omitted by a legacy caller.</param>
    /// <param name="expected">The server-owned expected collection.</param>
    /// <returns><see langword="true"/> when the presented values are omitted or an exact match.</returns>
    public static bool MatchesPresented(IReadOnlyList<string>? presented, IReadOnlyList<string> expected)
    {
        ArgumentNullException.ThrowIfNull(expected);
        if (presented is null || presented.Count == 0)
        {
            return true;
        }

        if (presented.Count != expected.Count)
        {
            return false;
        }

        for (int index = 0; index < expected.Count; index++)
        {
            if (!string.Equals(presented[index], expected[index], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
