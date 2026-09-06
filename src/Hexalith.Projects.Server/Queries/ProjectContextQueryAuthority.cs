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
    public static IReadOnlyList<string> ExpectedAudience { get; } = ["hexalith-projects"];

    /// <summary>
    /// Returns whether presented envelope collections are omitted or contain every server-owned expectation.
    /// </summary>
    /// <param name="presented">The immutable presented collection, or null when omitted by a Story 6.2 caller.</param>
    /// <param name="expected">The server-owned expected collection.</param>
    /// <returns>
    /// <see langword="true"/> when the presented values are omitted, or every expected value appears in
    /// the presented collection. Extra DualPrincipal entries are allowed.
    /// </returns>
    public static bool MatchesPresented(IReadOnlyList<string>? presented, IReadOnlyList<string> expected)
    {
        ArgumentNullException.ThrowIfNull(expected);
        if (presented is null || presented.Count == 0)
        {
            return true;
        }

        foreach (string required in expected)
        {
            bool found = false;
            for (int index = 0; index < presented.Count; index++)
            {
                if (string.Equals(presented[index], required, StringComparison.Ordinal))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }
}
