// <copyright file="EvidenceFreshnessState.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Canonical verification state for evidence exposed through reference-health boundaries.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<EvidenceFreshnessState>))]
public enum EvidenceFreshnessState
{
    /// <summary>The evidence is current and may be trusted for its approved purpose.</summary>
    Current = 1,

    /// <summary>The evidence is stale and must be treated as degraded.</summary>
    Stale = 2,

    /// <summary>The evidence source is rebuilding and is not currently usable.</summary>
    Rebuilding = 3,

    /// <summary>The evidence is unavailable or cannot be interpreted safely.</summary>
    Unavailable = 4,
}
