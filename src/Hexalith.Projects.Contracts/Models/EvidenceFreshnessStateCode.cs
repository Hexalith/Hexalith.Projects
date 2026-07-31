// <copyright file="EvidenceFreshnessStateCode.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Models;

/// <summary>
/// Converts producer-local freshness values into the canonical reference-health machine and display vocabulary.
/// </summary>
public static class EvidenceFreshnessStateCode
{
    /// <summary>The canonical machine code for current evidence.</summary>
    public const string Current = "current";

    /// <summary>The canonical machine code for stale evidence.</summary>
    public const string Stale = "stale";

    /// <summary>The canonical machine code for rebuilding evidence.</summary>
    public const string Rebuilding = "rebuilding";

    /// <summary>The canonical machine code for unavailable or unrecognized evidence.</summary>
    public const string Unavailable = "unavailable";

    /// <summary>
    /// Normalizes a producer-local freshness value to one of the four canonical machine codes.
    /// </summary>
    /// <param name="value">The producer-local value.</param>
    /// <returns><c>current</c>, <c>stale</c>, <c>rebuilding</c>, or <c>unavailable</c>.</returns>
    public static string Normalize(string? value)
        => Parse(value) switch
        {
            EvidenceFreshnessState.Current => Current,
            EvidenceFreshnessState.Stale => Stale,
            EvidenceFreshnessState.Rebuilding => Rebuilding,
            _ => Unavailable,
        };

    /// <summary>
    /// Gets the canonical human-readable label for a producer-local or canonical freshness value.
    /// </summary>
    /// <param name="value">The producer-local or canonical value.</param>
    /// <returns><c>Current</c>, <c>Stale</c>, <c>Rebuilding</c>, or <c>Unavailable</c>.</returns>
    public static string ToLabel(string? value)
        => Parse(value).ToString();

    private static EvidenceFreshnessState Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return EvidenceFreshnessState.Unavailable;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "trusted" or "fresh" or Current => EvidenceFreshnessState.Current,
            Stale or "mixedgeneration" => EvidenceFreshnessState.Stale,
            Rebuilding => EvidenceFreshnessState.Rebuilding,
            Unavailable or "unknown" or "forbidden" or "redacted" => EvidenceFreshnessState.Unavailable,
            _ => EvidenceFreshnessState.Unavailable,
        };
    }
}
